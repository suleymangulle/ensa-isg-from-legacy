using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Documents;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Documents;

/// <summary>
/// Downloadable form and template definitions.
/// <para>
/// A form record is metadata pointing at a row in the central document store; the file itself
/// moves through the storage layer described in <see cref="DocumentAppService"/>.
/// </para>
/// </summary>
public class FormAppService(
    IServiceProvider serviceProvider,
    IRepository<Form> formRepository)
    : EnsaAppService(serviceProvider), IFormAppService
{
    /// <summary>Maximum number of records returned by the drop-down endpoint.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<FormDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Form.Default);

        var form = await formRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Form), id);

        return ObjectMapper.Map<Form, FormDto>(form);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<FormListDto>> GetListAsync(
        GetFormListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Form.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "FormName ASC");

        var total = await formRepository.GetCountAsync(predicate, cancellationToken);

        var records = await formRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Form>, List<FormListDto>>(records);

        return new PagedResultDto<FormListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Form.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await formRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "FormName ASC",
            predicate: f => f.IsActive && (search == null || f.FormName.Contains(search)),
            cancellationToken);

        var result = records
            .Select(f => new LookupDto
            {
                Id = f.Id,
                DisplayName = f.FormName,
                IsActive = f.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<FormDto> CreateAsync(
        CreateFormDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Form.Create);

        var form = ObjectMapper.Map<CreateFormDto, Form>(input);

        await formRepository.InsertAsync(form, autoSave: true, cancellationToken);

        if (form.DefaultForm)
        {
            await DemoteOtherDefaultsAsync(form.CategoryId, form.Id, cancellationToken);
        }

        Logger.LogInformation("Form created: {FormId} - {FormName}", form.Id, form.FormName);

        return ObjectMapper.Map<Form, FormDto>(form);
    }

    /// <inheritdoc />
    public async Task<FormDto> UpdateAsync(
        int id,
        UpdateFormDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Form.Update);

        var form = await formRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Form), id);

        ObjectMapper.Map(input, form);

        await formRepository.UpdateAsync(form, autoSave: true, cancellationToken);

        if (form.DefaultForm)
        {
            await DemoteOtherDefaultsAsync(form.CategoryId, form.Id, cancellationToken);
        }

        return ObjectMapper.Map<Form, FormDto>(form);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Form.Delete);

        var form = await formRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Form), id);

        await formRepository.DeleteAsync(form, autoSave: true, cancellationToken);

        Logger.LogInformation("Form deleted: {FormId}", id);
    }

    // ----------------------------------------------------------- internals

    /// <summary>
    /// Keeps at most one featured form per category: promoting a form demotes whichever form
    /// previously held the flag. Without this the category picker would have to choose between
    /// several "default" forms arbitrarily.
    /// </summary>
    private async Task DemoteOtherDefaultsAsync(
        int categoryId,
        int keepFormId,
        CancellationToken cancellationToken)
    {
        var others = await formRepository.GetListAsync(
            f => f.CategoryId == categoryId && f.DefaultForm && f.Id != keepFormId,
            cancellationToken);

        if (others.Count == 0)
        {
            return;
        }

        foreach (var other in others)
        {
            other.DefaultForm = false;
        }

        await formRepository.UpdateManyAsync(others, autoSave: true, cancellationToken);
    }

    private static Expression<Func<Form, bool>> BuildFilter(GetFormListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var categoryId = input.CategoryId;
        var isActive = input.IsActive;
        var defaultForm = input.DefaultForm;

        return f =>
            (search == null || f.FormName.Contains(search))
            && (categoryId == null || f.CategoryId == categoryId)
            && (isActive == null || f.IsActive == isActive)
            && (defaultForm == null || f.DefaultForm == defaultForm);
    }
}
