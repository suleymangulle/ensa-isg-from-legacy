using Ensa.Domain.Lookups;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Finance;

/// <summary>
/// Domain service responsible for computing invoice totals, enforcing invoice-number uniqueness
/// and spelling the amount out in words.
/// </summary>
public interface IInvoiceManager : IDomainService
{
    /// <summary>
    /// Computes a single invoice line's <c>TotalAmount</c>/<c>VatAmount</c>/<c>GrossWithVatAmount</c>
    /// from its <c>Count</c>, <c>UnitPrice</c> and <c>VatRate</c>, and writes them back to the line.
    /// Rounding: 2 decimal places, <see cref="MidpointRounding.AwayFromZero"/>.
    /// </summary>
    void CalculateLineTotals(InvoiceLine line);

    /// <summary>
    /// Computes the invoice's <c>Total</c> (excluding VAT), <c>VatTotal</c> and <c>GeneralTotal</c>
    /// (including VAT) from the given lines, writes them to <paramref name="invoice"/>, and then
    /// fills <c>InWords</c> through <see cref="AmountToWords"/>. Every line is refreshed with
    /// <see cref="CalculateLineTotals"/> first.
    /// </summary>
    void CalculateInvoiceTotals(Invoice invoice, IReadOnlyCollection<InvoiceLine> lines);

    /// <summary>
    /// Verifies that the invoice number is unique within the active tenant, excluding
    /// <paramref name="exceptInvoiceId"/>. Throws <see cref="BusinessException"/> when it is
    /// already in use.
    /// </summary>
    Task ValidateInvoiceNoUniqueAsync(
        string invoiceNo,
        int? exceptInvoiceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next invoice number for an office and year, keeping the legacy
    /// "office code + year + sequence" format.
    /// <para>
    /// The sequence comes from the atomic counter in <c>INumberSequenceRepository</c>, not from
    /// "read the highest number and add one" — that older approach handed the same number to two
    /// concurrent callers, and the unique index then rejected whichever invoice saved second.
    /// </para>
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync(
        int? officeId,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Spells a Turkish Lira amount out in Turkish ("... Türk Lirası, ... Kuruş").
    /// A pure function; it needs no data access.
    /// </summary>
    string AmountToWords(decimal amount);
}

/// <inheritdoc cref="IInvoiceManager"/>
public class InvoiceManager : DomainService, IInvoiceManager
{
    // Turkish number words. These are DATA, not identifiers - the bulk English rename
    // corrupted them once ("iki"->"two", "alti"->"childi", "dokuz"->"nine", "elli"->"fifty"),
    // which silently printed English words onto invoices. Do not run a translator over them.
    private static readonly string[] Ones =
        ["", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz"];

    private static readonly string[] Tens =
        ["", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan"];

    private static readonly string[] Scales = ["", "bin", "milyon", "milyar", "trilyon"];

    /// <summary>Counter type prefix; the calendar year is appended to it.</summary>
    private const string InvoiceSequenceTypePrefix = "INVOICE-";

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly INumberSequenceRepository _numberSequenceRepository;

    public InvoiceManager(
        IInvoiceRepository invoiceRepository,
        INumberSequenceRepository numberSequenceRepository)
    {
        _invoiceRepository = invoiceRepository;
        _numberSequenceRepository = numberSequenceRepository;
    }

    public void CalculateLineTotals(InvoiceLine line)
    {
        line.TotalAmount = Round(line.Count * line.UnitPrice);
        line.VatAmount = Round(line.TotalAmount * line.VatRate / 100m);
        line.GrossWithVatAmount = Round(line.TotalAmount + line.VatAmount);
    }

    public void CalculateInvoiceTotals(Invoice invoice, IReadOnlyCollection<InvoiceLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new BusinessException("An invoice must contain at least one line.", "Ensa:Invoice:AtLeastOneLineRequired");
        }

        decimal total = 0m;
        decimal vatTotal = 0m;

        foreach (var line in lines)
        {
            CalculateLineTotals(line);
            total += line.TotalAmount;
            vatTotal += line.VatAmount;
        }

        invoice.Total = Round(total);
        invoice.VatTotal = Round(vatTotal);
        invoice.GeneralTotal = Round(invoice.Total + invoice.VatTotal);
        invoice.InWords = AmountToWords(invoice.GeneralTotal);
    }

    public async Task ValidateInvoiceNoUniqueAsync(
        string invoiceNo,
        int? exceptInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
        {
            throw new BusinessException("Invoice number cannot be empty.", "Ensa:Invoice:NumberEmpty");
        }

        var alreadyUsed = await _invoiceRepository.InvoiceNumberExistsAsync(invoiceNo, exceptInvoiceId, cancellationToken);
        if (alreadyUsed)
        {
            throw new BusinessException(
                $"Invoice '{invoiceNo}' is already registered.",
                "Ensa:Invoice:NumberAlreadyUsed");
        }
    }

    public async Task<string> GenerateInvoiceNumberAsync(
        int? officeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        // The counter row is (tenant, office, "INVOICE-{year}"), so each office restarts its own
        // series every year - which is what the printed format already implied.
        var orderNo = await _numberSequenceRepository.GetNextNumberAsync(
            officeId ?? 0,
            $"{InvoiceSequenceTypePrefix}{year}",
            cancellationToken);

        var officePart = officeId?.ToString("D2") ?? "00";
        return $"{officePart}-{year}-{orderNo:D6}";
    }

    public string AmountToWords(decimal amount)
    {
        if (amount < 0)
        {
            throw new BusinessException("A negative amount cannot be spelled out.", "Ensa:Invoice:NegativeAmount");
        }

        var wholePart = (long)Math.Truncate(amount);
        var kurus = (int)Math.Round((amount - wholePart) * 100m, 0, MidpointRounding.AwayFromZero);
        if (kurus == 100)
        {
            // Rounding overflow (e.g. 12.995 -> 13.00) — carry one into the whole part.
            wholePart += 1;
            kurus = 0;
        }

        var wholeText = NumberToWords(wholePart);
        var text = $"{LargeCaseStart(wholeText)} Türk Lirası";

        if (kurus > 0)
        {
            var kurusText = NumberToWords(kurus);
            text += $", {LargeCaseStart(kurusText)} Kuruş";
        }

        return text;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Spells a 0-999 group out in Turkish, without appending a scale suffix.</summary>
    private static string ThreeDigitsToWords(int n)
    {
        var hundredsDigit = n / 100;
        var remaining = n % 100;
        var tensDigit = remaining / 10;
        var onesDigit = remaining % 10;

        var parts = new List<string>();

        if (hundredsDigit > 0)
        {
            // Turkish says "yüz", never "bir yüz".
            if (hundredsDigit > 1)
            {
                parts.Add(Ones[hundredsDigit]);
            }

            parts.Add("yüz");
        }

        if (tensDigit > 0)
        {
            parts.Add(Tens[tensDigit]);
        }

        if (onesDigit > 0)
        {
            parts.Add(Ones[onesDigit]);
        }

        return string.Join(' ', parts);
    }

    private static string NumberToWords(long n)
    {
        if (n == 0)
        {
            return "sıfır";
        }

        // Split the number into groups of three, least significant group first.
        var groups = new List<int>();
        var remaining = n;
        while (remaining > 0)
        {
            groups.Add((int)(remaining % 1000));
            remaining /= 1000;
        }

        var parts = new List<string>();

        for (var i = groups.Count - 1; i >= 0; i--)
        {
            var group = groups[i];
            if (group == 0)
            {
                continue;
            }

            var groupText = ThreeDigitsToWords(group);

            if (i == 1 && group == 1)
            {
                // Turkish says "bin", never "bir bin" — from "milyon" upwards the "bir" is kept.
                parts.Add(Scales[i]);
            }
            else if (i > 0)
            {
                parts.Add($"{groupText} {Scales[i]}");
            }
            else
            {
                parts.Add(groupText);
            }
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// Upper-cases the first letter, applying the Turkish 'i' → 'İ' mapping correctly and
    /// independently of the current culture.
    /// </summary>
    private static string LargeCaseStart(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var firstChar = text[0] == 'i' ? 'İ' : char.ToUpperInvariant(text[0]);
        return firstChar + text[1..];
    }
}
