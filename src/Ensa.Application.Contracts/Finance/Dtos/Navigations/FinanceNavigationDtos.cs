using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Finance.Dtos.Navigations;

/// <summary>
/// Everything the invoice detail / print screen needs in a single call: the header, the
/// workplace it was issued to, the issuing office and the lines with their service-item names.
/// <para>
/// Class-typed properties are forbidden on ordinary DTOs, so combined reads live in a
/// <see cref="NavigationDto"/> derivative (see docs/ARCHITECTURE.md section 4).
/// </para>
/// </summary>
public class InvoiceNavigationDto : NavigationDto
{
    public InvoiceDto Invoice { get; set; } = null!;

    public LookupDto? Company { get; set; }

    public LookupDto? Office { get; set; }

    /// <summary>Invoice lines in <c>OrderNo</c> order, each carrying its service-item name.</summary>
    public List<InvoiceLineNavigationDto> Lines { get; set; } = [];
}

/// <summary>An invoice line together with the name of the service item it was priced from.</summary>
public class InvoiceLineNavigationDto : NavigationDto
{
    public InvoiceLineDto Line { get; set; } = null!;

    /// <summary>The service card the line was created from, when one was used.</summary>
    public LookupDto? ServiceItem { get; set; }
}

/// <summary>
/// Cash register detail screen: the register, its office, the balance at query time and the
/// most recent movements.
/// </summary>
public class CashRegisterNavigationDto : NavigationDto
{
    public CashRegisterDto CashRegister { get; set; } = null!;

    public LookupDto? Office { get; set; }

    /// <summary>Balance at the moment of the query (entries minus exits, voided rows excluded).</summary>
    public decimal Balance { get; set; }

    /// <summary>The most recent movements, newest first.</summary>
    public List<CashTransactionDto> LatestTransactions { get; set; } = [];
}

/// <summary>
/// Statutory fine detail screen: the article plus its amount matrix.
/// <para>
/// The brief calls for the amounts to be exposed as a list on the DTO. Because a list of child
/// records is a combined read, it is carried here rather than on <see cref="PenaltyDto"/>, which
/// keeps <see cref="PenaltyDto"/> free of class-typed properties.
/// </para>
/// </summary>
public class PenaltyNavigationDto : NavigationDto
{
    public PenaltyDto Penalty { get; set; } = null!;

    /// <summary>Hazard class x head-count band x year amount matrix, newest year first.</summary>
    public List<PenaltyAmountDto> Amounts { get; set; } = [];
}
