using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Documents.Navigations;

/// <summary>
/// Combined view of a document with its category and a company summary.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class DocumentNavigation : NavigationEntity<Document>
{
    /// <summary>Shortcut to the root record (the same object as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public Document Document
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>The category the document belongs to.</summary>
    public DocumentCategory? Category { get; set; }

    /// <summary>
    /// Summary name of the related company. The <c>Company</c> entity lives in another module
    /// (<c>Companies</c>), so the looked-up text is held here INSTEAD of a direct reference.
    /// </summary>
    public string? CompanyName { get; set; }
}
