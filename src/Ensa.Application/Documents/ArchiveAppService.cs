using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Documents;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Documents;

/// <summary>
/// Module-scoped document archive: which record, of which module, for which period a stored
/// file belongs to.
/// <para>
/// Archive entries share the <see cref="EnsaPermissions.Document"/> permissions because an
/// archive row is a filing card for a document - anyone allowed to see the underlying files is
/// allowed to see how they are filed, and nobody else is.
/// </para>
/// </summary>
public class ArchiveAppService(
    IServiceProvider serviceProvider,
    IArchiveRepository archiveRepository)
    : EnsaAppService(serviceProvider), IArchiveAppService
{
    /// <inheritdoc />
    public async Task<ArchiveDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        var archive = await archiveRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Archive), id);

        return ObjectMapper.Map<Archive, ArchiveDto>(archive);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<ArchiveListDto>> GetListAsync(
        GetArchiveListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        ValidateMonth(input.Month);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await archiveRepository.GetCountAsync(predicate, cancellationToken);

        var records = await archiveRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Archive>, List<ArchiveListDto>>(records);

        return new PagedResultDto<ArchiveListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<ArchiveListDto>> GetByModuleAsync(
        DocumentOwnerType moduleType,
        int moduleId,
        int? month = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        ValidateMonth(month);

        var records = await archiveRepository.GetByModuleAsync(
            moduleType,
            moduleId,
            month,
            year,
            cancellationToken);

        var items = ObjectMapper.Map<List<Archive>, List<ArchiveListDto>>(records);

        return new ListResultDto<ArchiveListDto>(items);
    }

    /// <inheritdoc />
    public async Task<ArchiveDto> CreateAsync(
        CreateArchiveDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Create);

        ValidateMonth(input.Month);

        var archive = ObjectMapper.Map<CreateArchiveDto, Archive>(input);

        // Filing a document without saying which period it covers makes the monthly and annual
        // activity reports unable to pick it up, so the current period is filled in by default.
        archive.Year ??= Clock.Now.Year;
        archive.Month ??= Clock.Now.Month;

        await archiveRepository.InsertAsync(archive, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Archive entry created: {ArchiveId} - Module={ModuleType}/{ModuleId}",
            archive.Id, archive.ModuleType, archive.ModuleId);

        return ObjectMapper.Map<Archive, ArchiveDto>(archive);
    }

    /// <inheritdoc />
    public async Task<ArchiveDto> UpdateAsync(
        int id,
        UpdateArchiveDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Update);

        ValidateMonth(input.Month);

        var archive = await archiveRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Archive), id);

        ObjectMapper.Map(input, archive);

        await archiveRepository.UpdateAsync(archive, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Archive, ArchiveDto>(archive);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Delete);

        var archive = await archiveRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Archive), id);

        await archiveRepository.DeleteAsync(archive, autoSave: true, cancellationToken);

        Logger.LogInformation("Archive entry deleted: {ArchiveId}", id);
    }

    // ----------------------------------------------------------- internals

    /// <summary>
    /// Guards the month on query inputs too, not just on write payloads: a filter of
    /// <c>Month = 13</c> would otherwise return an empty page and look like missing data.
    /// </summary>
    private static void ValidateMonth(int? month)
    {
        if (month is null or (>= 1 and <= 12))
        {
            return;
        }

        throw new BusinessException(
                "The month must be between 1 and 12.",
                "Ensa:Archive:InvalidMonth")
            .WithData("Month", month);
    }

    private static Expression<Func<Archive, bool>> BuildFilter(GetArchiveListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var moduleType = input.ModuleType;
        var moduleId = input.ModuleId;
        var companyId = input.CompanyId;
        var month = input.Month;
        var year = input.Year;

        return a =>
            (search == null
             || (a.Description != null && a.Description.Contains(search))
             || (a.ModuleDescription != null && a.ModuleDescription.Contains(search)))
            && (moduleType == null || a.ModuleType == moduleType)
            && (moduleId == null || a.ModuleId == moduleId)
            && (companyId == null || a.CompanyId == companyId)
            && (month == null || a.Month == month)
            && (year == null || a.Year == year);
    }
}
