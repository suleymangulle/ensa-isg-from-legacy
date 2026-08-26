namespace Ensa.Application.Contracts.Common;

/// <summary>Marker for application services — used by the DI scan (ABP: <c>IApplicationService</c>).</summary>
public interface IApplicationService;

/// <summary>The standard CRUD contract (ABP: <c>ICrudAppService</c>).</summary>
public interface ICrudAppService<TGetOutputDto, TKey, in TGetListInput, in TCreateInput, in TUpdateInput>
    : IApplicationService
{
    Task<TGetOutputDto> GetAsync(TKey id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<TGetOutputDto>> GetListAsync(TGetListInput input, CancellationToken cancellationToken = default);

    Task<TGetOutputDto> CreateAsync(TCreateInput input, CancellationToken cancellationToken = default);

    Task<TGetOutputDto> UpdateAsync(TKey id, TUpdateInput input, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}

/// <summary>CRUD contract for services whose list view uses a separate DTO.</summary>
public interface ICrudAppService<TGetOutputDto, TGetListOutputDto, TKey, in TGetListInput, in TCreateInput, in TUpdateInput>
    : IApplicationService
{
    Task<TGetOutputDto> GetAsync(TKey id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<TGetListOutputDto>> GetListAsync(TGetListInput input, CancellationToken cancellationToken = default);

    Task<TGetOutputDto> CreateAsync(TCreateInput input, CancellationToken cancellationToken = default);

    Task<TGetOutputDto> UpdateAsync(TKey id, TUpdateInput input, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}

/// <summary>For services that expose a lookup list.</summary>
public interface ILookupAppService<TKey> : IApplicationService
{
    Task<ListResultDto<LookupDto<TKey>>> GetLookupAsync(string? filter = null, CancellationToken cancellationToken = default);
}
