using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Lookups.Dtos;

namespace Ensa.Application.Contracts.Lookups;

/// <summary>
/// Per-organization key/value system settings.
/// <para>
/// Unlike the reference tables behind <see cref="ILookupAppService"/>, parameters belong to a
/// tenant and are writable, so this is a full CRUD service.
/// </para>
/// </summary>
public interface IParameterAppService : IApplicationService
{
    Task<ParameterDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ParameterListDto>> GetListAsync(
        GetParameterListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single parameter value by its code - the shape application code needs, without
    /// paging or entity metadata. Returns <c>Exists = false</c> rather than throwing when the
    /// code is not defined, so callers can fall back to a default.
    /// </summary>
    Task<ParameterValueDto> GetValueAsync(string code, CancellationToken cancellationToken = default);

    Task<ParameterDto> CreateAsync(CreateParameterDto input, CancellationToken cancellationToken = default);

    Task<ParameterDto> UpdateAsync(int id, UpdateParameterDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
