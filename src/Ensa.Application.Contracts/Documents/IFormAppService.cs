using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents.Dtos;

namespace Ensa.Application.Contracts.Documents;

/// <summary>
/// Downloadable form and template definitions.
/// <para>
/// A form is a metadata record pointing at a row in the central document store; the file
/// itself is transferred through the storage layer described in
/// <see cref="IDocumentAppService"/>.
/// </para>
/// </summary>
public interface IFormAppService : IApplicationService
{
    Task<FormDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<FormListDto>> GetListAsync(
        GetFormListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-downs.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<FormDto> CreateAsync(CreateFormDto input, CancellationToken cancellationToken = default);

    Task<FormDto> UpdateAsync(int id, UpdateFormDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
