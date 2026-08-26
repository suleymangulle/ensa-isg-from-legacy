using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Risks;

/// <summary>
/// EF Core implementation of <see cref="IEquipmentRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class EquipmentRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Equipment>(context, dataFilter), IEquipmentRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the files of the documents and the document type definitions are fetched
    /// in bulk with <c>Contains</c> — one query each rather than one per document — and matched up in
    /// memory. The total query count is at most 6 regardless of the number of documents.
    /// </remarks>
    public async Task<EquipmentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var equipment = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (equipment is null)
        {
            return null;
        }

        var navigation = new EquipmentNavigation { Equipment = equipment };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == equipment.CompanyId, cancellationToken);

        var equipmentDocuments = await Context.Set<EquipmentDocument>()
            .AsNoTracking()
            .Where(e => e.EquipmentId == id)
            .OrderByDescending(e => e.ExaminationDate)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        // The examination report file plus the document files in a SINGLE query.
        var documentIds = equipmentDocuments.ConvertAll(e => e.DocumentId);
        if (equipment.ExaminationReportDocumentId is { } reportDocumentId)
        {
            documentIds.Add(reportDocumentId);
        }

        List<Document> files = documentIds.Count == 0
            ? []
            : await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

        navigation.ExaminationReportDocument = files.Find(d => d.Id == equipment.ExaminationReportDocumentId);

        var documentTypeIds = equipmentDocuments
            .Where(e => e.EquipmentDocumentTypeId.HasValue)
            .Select(e => e.EquipmentDocumentTypeId!.Value)
            .Distinct()
            .ToList();

        List<EquipmentDocumentType> documentTypes = documentTypeIds.Count == 0
            ? []
            : await Context.Set<EquipmentDocumentType>()
                .AsNoTracking()
                .Where(t => documentTypeIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

        navigation.Documents = equipmentDocuments.ConvertAll(document => new EquipmentDocumentNavigation
        {
            Document = document,
            File = files.Find(d => d.Id == document.DocumentId),
            DocumentType = documentTypes.Find(t => t.Id == document.EquipmentDocumentTypeId)
        });

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<Equipment>> GetExaminationOverdueAsync(
        DateTime reference,
        int? companyId = null,
        bool includeNeverExamined = true,
        CancellationToken cancellationToken = default)
    {
        var cutoff = reference.Date;

        var query = GetReadOnlyQueryable();

        query = includeNeverExamined
            ? query.Where(e => (e.NextExaminationDate != null && e.NextExaminationDate < cutoff)
                               || e.ExaminationDate == null)
            : query.Where(e => e.NextExaminationDate != null && e.NextExaminationDate < cutoff);

        if (companyId is { } value)
        {
            query = query.Where(e => e.CompanyId == value);
        }

        return query
            .OrderBy(e => e.NextExaminationDate)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Equipment>> GetExaminationUpcomingAsync(
        DateTime reference,
        int dayCount,
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        // Half-open range: [reference, reference + dayCount). No function is applied to the date column.
        var lowerBound = reference.Date;
        var upperBound = lowerBound.AddDays(dayCount);

        var query = GetReadOnlyQueryable()
            .Where(e => e.NextExaminationDate != null
                        && e.NextExaminationDate >= lowerBound
                        && e.NextExaminationDate < upperBound);

        if (companyId is { } value)
        {
            query = query.Where(e => e.CompanyId == value);
        }

        return query
            .OrderBy(e => e.NextExaminationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Equipment>> GetListByCompanyAsync(
        int companyId,
        EquipmentType? equipmentType = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(e => e.CompanyId == companyId);

        if (equipmentType is { } type)
        {
            query = query.Where(e => e.EquipmentType == type);
        }

        return query
            .OrderBy(e => e.EquipmentName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<EquipmentDocument>> GetDocumentsAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
        => Context.Set<EquipmentDocument>()
            .AsNoTracking()
            .Where(e => e.EquipmentId == equipmentId)
            .OrderByDescending(e => e.ExaminationDate)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
}
