using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Tenancy;

namespace Ensa.Domain.Finance.Navigations;

/// <summary>
/// Combined view of an <see cref="Invoice"/> with its company, office and lines, including
/// service item names.
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>IInvoiceRepository.GetWithNavigationAsync</c> populates it through an <c>IQueryable</c> join
/// and projection.
/// </para>
/// </summary>
[NotMapped]
public class InvoiceNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public Invoice Invoice { get; set; } = null!;

    public Company? Company { get; set; }

    public Office? Office { get; set; }

    public List<InvoiceLineNavigation> Lines { get; set; } = [];
}

/// <summary>
/// Combined view of an <see cref="InvoiceLine"/> and its service item, for printing and viewing
/// invoices.
/// </summary>
[NotMapped]
public class InvoiceLineNavigation : NavigationEntity
{
    public InvoiceLine InvoiceLine { get; set; } = null!;

    public ServiceItem? ServiceItem { get; set; }
}
