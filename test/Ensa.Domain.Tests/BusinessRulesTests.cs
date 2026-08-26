using Ensa.Domain.Trainings;
using Ensa.Domain.Finance;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Health;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Tests;

/// <summary>
/// Verifies the regulation-driven business rules implemented by the domain services.
/// These tests need no database — they target the pure calculation methods only.
/// </summary>
public class NationalIdValidationTests
{
    [Theory]
    // Synthetic numbers that satisfy the algorithm but belong to no real person.
    [InlineData("10000000146")]
    [InlineData("11111111110")]
    public void Accepts_valid_numbers(string nationalId)
        => Assert.True(CompanyEmployeeManager.IsValidNationalId(nationalId));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890")]      // 10 digits
    [InlineData("123456789012")]    // 12 digits
    [InlineData("01234567890")]     // the first digit cannot be 0
    [InlineData("1234567890a")]     // non-numeric character
    [InlineData("12345678901")]     // checksum does not match
    public void Rejects_invalid_numbers(string? nationalId)
        => Assert.False(CompanyEmployeeManager.IsValidNationalId(nationalId));
}

public class RiskScoreTests
{
    private readonly RiskAssessmentManager _manager = new(null!);

    [Theory]
    // L-Matrix: score = likelihood × severity
    [InlineData(RiskAssessmentMethod.LMatrixFiveByFive, 5, 5, 0, 25)]
    [InlineData(RiskAssessmentMethod.LMatrixFiveByFive, 3, 4, 0, 12)]
    [InlineData(RiskAssessmentMethod.LMatrixThreeByThree, 3, 3, 0, 9)]
    // Fine-Kinney: score = likelihood × frequency × severity
    [InlineData(RiskAssessmentMethod.FineKinney, 6, 15, 6, 540)]
    [InlineData(RiskAssessmentMethod.FineKinney, 1, 1, 1, 1)]
    public void Calculates_the_correct_score_per_method(
        RiskAssessmentMethod method, int likelihood, int severity, int frequency, int expected)
    {
        // xUnit's InlineData cannot convert `int` to `decimal?`, so a frequency of 0
        // encodes "not applicable".
        decimal? frequencyValue = frequency > 0 ? frequency : null;

        var score = _manager.Calculate(likelihood, severity, frequencyValue, method);
        Assert.Equal(expected, score);
    }

    [Theory]
    // Law no. 6331: the risk assessment renewal period depends on the hazard class.
    [InlineData(HazardClass.VeryHazardous, 2)]
    [InlineData(HazardClass.Hazardous, 4)]
    [InlineData(HazardClass.LowHazard, 6)]
    public void Derives_the_validity_date_from_the_hazard_class(HazardClass hazardClass, int expectedYears)
    {
        var performed = new DateTime(2026, 1, 15);

        var validity = _manager.CalculateValidUntilDate(performed, hazardClass);

        Assert.Equal(performed.AddYears(expectedYears), validity);
    }
}

public class TrainingPeriodTests
{
    // Only the pure interval calculations are exercised here; the repositories are never touched.
    private readonly TrainingPlanningManager _manager =
        new(null!, new FixedClock(new DateTime(2026, 8, 26, 9, 0, 0)), null!);

    [Theory]
    // Law no. 6331: the OHS training renewal interval.
    [InlineData(HazardClass.VeryHazardous, 1)]
    [InlineData(HazardClass.Hazardous, 2)]
    [InlineData(HazardClass.LowHazard, 3)]
    public void Derives_the_next_training_date_from_the_hazard_class(HazardClass hazardClass, int expectedYears)
    {
        var latestTraining = new DateTime(2026, 3, 10);

        var next = _manager.CalculateNextTrainingDate(latestTraining, hazardClass);

        Assert.Equal(latestTraining.AddYears(expectedYears), next);
    }
}

public class HealthSurveillanceTests
{
    private readonly HealthSurveillanceManager _manager =
        new(null!, new FixedClock(new DateTime(2026, 8, 26, 9, 0, 0)));

    [Theory]
    // Law no. 6331: the periodic examination interval.
    [InlineData(HazardClass.VeryHazardous, 1)]
    [InlineData(HazardClass.Hazardous, 3)]
    [InlineData(HazardClass.LowHazard, 5)]
    public void Derives_the_next_examination_date_from_the_hazard_class(HazardClass hazardClass, int expectedYears)
    {
        var examination = new DateTime(2026, 6, 1);

        var next = _manager.CalculateNextExaminationDate(examination, hazardClass);

        Assert.Equal(examination.AddYears(expectedYears), next);
    }

    [Theory]
    [InlineData(180, 81.0, 25.00)]
    [InlineData(170, 70.0, 24.22)]
    public void Calculates_bmi(int heightCm, double weightKg, decimal expected)
    {
        var bmi = _manager.CalculateBmi(heightCm, (decimal)weightKg);

        Assert.Equal(expected, Math.Round(bmi, 2));
    }
}

public class InvoiceCalculationTests
{
    // Both dependencies are only touched by the async members; every test here exercises the
    // pure calculation and formatting logic.
    private readonly InvoiceManager _manager = new(null!, null!);

    [Fact]
    public void Calculates_line_totals_including_vat()
    {
        var line = new InvoiceLine
        {
            Count = 2,
            UnitPrice = 100m,
            VatRate = 20
        };

        _manager.CalculateLineTotals(line);

        Assert.Equal(200m, line.TotalAmount);
        Assert.Equal(40m, line.VatAmount);
        Assert.Equal(240m, line.GrossWithVatAmount);
    }

    [Fact]
    public void Calculates_invoice_totals_from_its_lines()
    {
        var invoice = new Invoice();
        var lines = new List<InvoiceLine>
        {
            new() { Count = 1, UnitPrice = 1000m, VatRate = 20 },
            new() { Count = 3, UnitPrice = 50m, VatRate = 10 }
        };

        foreach (var line in lines)
        {
            _manager.CalculateLineTotals(line);
        }

        _manager.CalculateInvoiceTotals(invoice, lines);

        Assert.Equal(1150m, invoice.Total);        // 1000 + 150
        Assert.Equal(215m, invoice.VatTotal);      // 200 + 15
        Assert.Equal(1365m, invoice.GeneralTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1234.56)]
    [InlineData(1000000)]
    public void Spells_an_amount_out_and_never_returns_empty(decimal amount)
    {
        var words = _manager.AmountToWords(amount);

        Assert.False(string.IsNullOrWhiteSpace(words));
    }

    /// <summary>
    /// Pins the exact Turkish wording, because this string is printed on the invoice.
    /// A bulk rename once translated the digit table itself - "iki" became "two", "alti"
    /// became "childi", "dokuz" became "nine" and "elli" became "fifty" - and the previous
    /// not-empty assertion happily passed while invoices printed nonsense. Every digit that
    /// was corrupted back then is covered here.
    /// </summary>
    [Theory]
    [InlineData(0, "Sıfır Türk Lirası")]
    [InlineData(2, "İki Türk Lirası")]
    [InlineData(6, "Altı Türk Lirası")]
    [InlineData(9, "Dokuz Türk Lirası")]
    [InlineData(50, "Elli Türk Lirası")]
    [InlineData(1000, "Bin Türk Lirası")]
    [InlineData(269.56, "İki yüz altmış dokuz Türk Lirası, Elli altı Kuruş")]
    public void Spells_an_amount_out_in_Turkish(decimal amount, string expected)
    {
        Assert.Equal(expected, _manager.AmountToWords(amount));
    }
}
