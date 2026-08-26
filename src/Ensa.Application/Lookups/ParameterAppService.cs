using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Lookups;
using Ensa.Application.Contracts.Lookups.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Lookups;

/// <summary>
/// Per-organization key/value system settings.
/// <para>
/// Parameters share the reference-module permissions (<see cref="EnsaPermissions.Lookups"/>)
/// because they are the writable half of the same "definitions" area of the application, and
/// the tenant filter on <c>Parameter</c> already keeps one organization out of another.
/// </para>
/// </summary>
public class ParameterAppService(
    IServiceProvider serviceProvider,
    IParameterRepository parameterRepository)
    : EnsaAppService(serviceProvider), IParameterAppService
{
    /// <inheritdoc />
    public async Task<ParameterDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var parameter = await parameterRepository.FindAsync(id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Parameter), id);

        return ObjectMapper.Map<Parameter, ParameterDto>(parameter);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<ParameterListDto>> GetListAsync(
        GetParameterListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "Code ASC");

        var total = await parameterRepository.GetCountAsync(predicate, cancellationToken);

        var records = await parameterRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Parameter>, List<ParameterListDto>>(records);

        return new PagedResultDto<ParameterListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ParameterValueDto> GetValueAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var trimmed = code.Trim();

        // The repository already applies the tenant filter, so one organization can never
        // read the setting of another through a guessed code.
        var value = await parameterRepository.GetValueAsync(trimmed, cancellationToken);

        return new ParameterValueDto
        {
            Code = trimmed,
            Value = value,
            Exists = value is not null
        };
    }

    /// <inheritdoc />
    public async Task<ParameterDto> CreateAsync(
        CreateParameterDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Lookups.Create);

        var code = input.Code.Trim();

        var exists = await parameterRepository.AnyAsync(p => p.Code == code, cancellationToken);
        if (exists)
        {
            throw new BusinessException(
                    "A parameter with this code already exists.",
                    "Ensa:Parameter:CodeAlreadyExists")
                .WithData("Code", code);
        }

        var parameter = ObjectMapper.Map<CreateParameterDto, Parameter>(input);
        parameter.Code = code;

        // This module has no domain manager, so the service persists directly.
        await parameterRepository.InsertAsync(parameter, autoSave: true, cancellationToken);

        Logger.LogInformation("Parameter created: {ParameterId} - {Code}", parameter.Id, parameter.Code);

        return ObjectMapper.Map<Parameter, ParameterDto>(parameter);
    }

    /// <inheritdoc />
    public async Task<ParameterDto> UpdateAsync(
        int id,
        UpdateParameterDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Lookups.Update);

        var parameter = await parameterRepository.FindAsync(id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Parameter), id);

        // The mapper leaves Code untouched on purpose: application code looks parameters up by
        // code, so renaming one would silently change behaviour elsewhere.
        ObjectMapper.Map(input, parameter);

        await parameterRepository.UpdateAsync(parameter, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Parameter, ParameterDto>(parameter);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Delete);

        var parameter = await parameterRepository.FindAsync(id, cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(Parameter), id);

        await parameterRepository.DeleteAsync(parameter, autoSave: true, cancellationToken);

        Logger.LogInformation("Parameter deleted: {ParameterId}", id);
    }

    // ----------------------------------------------------------- internals

    private static Expression<Func<Parameter, bool>> BuildFilter(GetParameterListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var isActive = input.IsActive;

        return p =>
            (search == null
             || p.Code.Contains(search)
             || p.Name.Contains(search)
             || p.Value.Contains(search))
            && (isActive == null || p.IsActive == isActive);
    }
}
