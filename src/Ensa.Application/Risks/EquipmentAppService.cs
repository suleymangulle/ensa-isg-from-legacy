using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Lookups;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Risks;

/// <summary>
/// Work equipment (legacy: Cihaz) application service.
/// <para>
/// The permission group is <c>EnsaPermissions.Equipment.*</c> — the legacy "Cihaz" naming was
/// already retired in the permission catalogue.
/// </para>
/// </summary>
public class EquipmentAppService(
    IServiceProvider serviceProvider,
    IEquipmentRepository equipmentRepository,
    IRepository<EquipmentDocument> equipmentDocumentRepository,
    IReadOnlyRepository<Company> companyRepository,
    IReadOnlyRepository<Period> periodRepository)
    : EnsaAppService(serviceProvider), IEquipmentAppService
{
    /// <inheritdoc />
    public async Task<EquipmentDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Equipment.Default);

        var equipment = await equipmentRepository.FindAsync(id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Equipment), id);

        return MapEquipment(equipment);
    }

    /// <inheritdoc />
    public async Task<EquipmentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Equipment.Default);

        // One repository call joins company, the inspection report file and every attached
        // document with its own file and type definition.
        var navigation = await equipmentRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Equipment), id);

        return new EquipmentNavigationDto
        {
            Equipment = MapEquipment(navigation.Equipment),
            Company = RiskLookupHelper.Lookup(navigation.Company?.Id, navigation.Company?.CompanyName),
            ExaminationReportDocument = RiskLookupHelper.Lookup(
                navigation.ExaminationReportDocument?.Id,
                navigation.ExaminationReportDocument?.DocumentName),
            Documents =
            [
                .. navigation.Documents.Select(d => new EquipmentDocumentNavigationDto
                {
                    Document = ObjectMapper.Map<EquipmentDocument, EquipmentDocumentDto>(d.Document),
                    File = RiskLookupHelper.Lookup(d.File?.Id, d.File?.DocumentName),
                    DocumentType = RiskLookupHelper.Lookup(d.DocumentType?.Id, d.DocumentType?.DocumentName)
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<EquipmentListDto>> GetListAsync(
        GetEquipmentListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Equipment.Default);

        var reference = Clock.Now.Date;
        var predicate = BuildFilter(input, reference);
        var sorting = NormalizeSorting(input.Sorting, "NextExaminationDate DESC");

        var total = await equipmentRepository.GetCountAsync(predicate, cancellationToken);

        var records = await equipmentRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = await MapListAsync(records, reference, cancellationToken);

        return new PagedResultDto<EquipmentListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<EquipmentDto> CreateAsync(
        CreateEquipmentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Equipment.Create);

        var equipment = ObjectMapper.Map<CreateEquipmentDto, Equipment>(input);
        equipment.NextExaminationDate = await CalculateNextExaminationDateAsync(
            input.ExaminationDate, input.PeriodId, cancellationToken);

        equipment = await equipmentRepository.InsertAsync(equipment, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Equipment created: {EquipmentId} — {EquipmentName} (Company: {CompanyId})",
            equipment.Id, equipment.EquipmentName, equipment.CompanyId);

        return MapEquipment(equipment);
    }

    /// <inheritdoc />
    public async Task<EquipmentDto> UpdateAsync(
        int id,
        UpdateEquipmentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Equipment.Update);

        var equipment = await equipmentRepository.FindAsync(id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Equipment), id);

        ObjectMapper.Map(input, equipment);
        equipment.NextExaminationDate = await CalculateNextExaminationDateAsync(
            input.ExaminationDate, input.PeriodId, cancellationToken);

        equipment = await equipmentRepository.UpdateAsync(equipment, autoSave: true, cancellationToken);

        return MapEquipment(equipment);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Equipment.Delete);

        var equipment = await equipmentRepository.FindAsync(id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Equipment), id);

        if (!equipment.IsDeletable)
        {
            throw new BusinessException(
                    "This equipment record was created automatically and cannot be deleted.",
                    "Ensa:Equipment:NotDeletable")
                .WithData("EquipmentName", equipment.EquipmentName);
        }

        var documents = await equipmentRepository.GetDocumentsAsync(id, cancellationToken);

        await equipmentDocumentRepository.DeleteManyAsync(documents, autoSave: false, cancellationToken);
        await equipmentRepository.DeleteAsync(equipment, autoSave: true, cancellationToken);

        Logger.LogInformation("Equipment deleted: {EquipmentId}", id);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<EquipmentListDto>> GetOverdueInspectionsAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Equipment.Default);

        var reference = Clock.Now.Date;

        // Never-inspected equipment counts as overdue: it is the most common real finding.
        var records = await equipmentRepository.GetExaminationOverdueAsync(
            reference,
            companyId,
            includeNeverExamined: true,
            cancellationToken);

        var items = await MapListAsync(records, reference, cancellationToken);

        return new ListResultDto<EquipmentListDto>(items);
    }

    // --------------------------------------------------------------- Documents

    /// <inheritdoc />
    public async Task<ListResultDto<EquipmentDocumentDto>> GetDocumentsAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Equipment.Default);

        _ = await equipmentRepository.FindAsync(equipmentId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Equipment), equipmentId);

        var documents = await equipmentRepository.GetDocumentsAsync(equipmentId, cancellationToken);

        return new ListResultDto<EquipmentDocumentDto>(
            ObjectMapper.Map<List<EquipmentDocument>, List<EquipmentDocumentDto>>(documents));
    }

    /// <inheritdoc />
    public async Task<EquipmentDocumentDto> AddDocumentAsync(
        int equipmentId,
        CreateEquipmentDocumentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Equipment.Update);

        var equipment = await equipmentRepository.FindAsync(equipmentId, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Equipment), equipmentId);

        var document = ObjectMapper.Map<CreateEquipmentDocumentDto, EquipmentDocument>(input);
        document.EquipmentId = equipmentId;

        // CompanyId is denormalized on the document; it always follows the parent equipment.
        document.CompanyId = equipment.CompanyId;

        document = await equipmentDocumentRepository.InsertAsync(document, autoSave: true, cancellationToken);

        return ObjectMapper.Map<EquipmentDocument, EquipmentDocumentDto>(document);
    }

    /// <inheritdoc />
    public async Task RemoveDocumentAsync(
        int equipmentId,
        int equipmentDocumentId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Equipment.Update);

        var document = await equipmentDocumentRepository.FindAsync(equipmentDocumentId, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(EquipmentDocument), equipmentDocumentId);

        if (document.EquipmentId != equipmentId)
        {
            throw new EntityNotFoundException(typeof(EquipmentDocument), equipmentDocumentId);
        }

        await equipmentDocumentRepository.DeleteAsync(document, autoSave: true, cancellationToken);
    }

    // ----------------------------------------------------------------- Helpers

    /// <summary>
    /// Derives the next inspection due date from the last inspection plus the selected period.
    /// <para>
    /// The domain layer has no <c>EquipmentManager</c>, so the arithmetic lives here; it is kept
    /// in one place so <c>CreateAsync</c> and <c>UpdateAsync</c> cannot drift apart.
    /// </para>
    /// </summary>
    private async Task<DateTime?> CalculateNextExaminationDateAsync(
        DateTime? examinationDate,
        int? periodId,
        CancellationToken cancellationToken)
    {
        if (examinationDate is not { } performed || periodId is not { } id)
        {
            return null;
        }

        var period = await periodRepository.FindAsync(id, cancellationToken);
        if (period is null || period.PeriodValue <= 0)
        {
            return null;
        }

        return period.PeriodUnit switch
        {
            PeriodUnit.Day => performed.Date.AddDays(period.PeriodValue),
            PeriodUnit.Week => performed.Date.AddDays(period.PeriodValue * 7),
            PeriodUnit.Month => performed.Date.AddMonths(period.PeriodValue),
            PeriodUnit.Year => performed.Date.AddYears(period.PeriodValue),
            _ => null
        };
    }

    private EquipmentDto MapEquipment(Equipment equipment)
    {
        var dto = ObjectMapper.Map<Equipment, EquipmentDto>(equipment);
        dto.IsInspectionOverdue = IsInspectionOverdue(equipment, Clock.Now.Date);
        return dto;
    }

    /// <summary>
    /// Maps a page of equipment and fills <c>CompanyName</c> with one batched company query
    /// instead of a lookup per row.
    /// </summary>
    private async Task<List<EquipmentListDto>> MapListAsync(
        List<Equipment> records,
        DateTime reference,
        CancellationToken cancellationToken)
    {
        var items = ObjectMapper.Map<List<Equipment>, List<EquipmentListDto>>(records);

        var companyNames = await RiskLookupHelper.LoadCompanyNamesAsync(
            companyRepository,
            RiskLookupHelper.DistinctIds(records, e => e.CompanyId),
            cancellationToken);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].CompanyName = companyNames.GetValueOrDefault(items[i].CompanyId);
            items[i].IsInspectionOverdue = IsInspectionOverdue(records[i], reference);
            items[i].RemainingDays = records[i].NextExaminationDate is { } due
                ? (int)(due.Date - reference).TotalDays
                : null;
        }

        return items;
    }

    /// <summary>Never inspected, or the due date has passed.</summary>
    private static bool IsInspectionOverdue(Equipment equipment, DateTime reference)
        => equipment.ExaminationDate is null
           || equipment.NextExaminationDate is null
           || equipment.NextExaminationDate.Value.Date < reference;

    private static Expression<Func<Equipment, bool>>? BuildFilter(
        GetEquipmentListInput input,
        DateTime reference)
    {
        var filter = new RiskFilter<Equipment>();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            filter.Add(e =>
                e.EquipmentName.Contains(search)
                || (e.ExaminationReport != null && e.ExaminationReport.Contains(search))
                || (e.ExaminationPerformedBy != null && e.ExaminationPerformedBy.Contains(search)));
        }

        filter.AddIf(input.CompanyId is { }, e => e.CompanyId == input.CompanyId!.Value);
        filter.AddIf(input.EquipmentType is { }, e => e.EquipmentType == input.EquipmentType!.Value);
        filter.AddIf(input.PeriodId is { }, e => e.PeriodId == input.PeriodId!.Value);

        filter.AddIf(
            input.OnlyOverdueInspection,
            e => e.ExaminationDate == null
                 || e.NextExaminationDate == null
                 || e.NextExaminationDate < reference);

        return filter.Build();
    }
}
