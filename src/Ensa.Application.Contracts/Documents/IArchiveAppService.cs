using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Documents;

/// <summary>
/// Module-scoped document archive: which record, of which module, for which period a given
/// file belongs to.
/// </summary>
public interface IArchiveAppService : IApplicationService
{
    Task<ArchiveDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ArchiveListDto>> GetListAsync(
        GetArchiveListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive entries of one module record, optionally narrowed to a month and year.
    /// </summary>
    Task<ListResultDto<ArchiveListDto>> GetByModuleAsync(
        DocumentOwnerType moduleType,
        int moduleId,
        int? month = null,
        int? year = null,
        CancellationToken cancellationToken = default);

    Task<ArchiveDto> CreateAsync(CreateArchiveDto input, CancellationToken cancellationToken = default);

    Task<ArchiveDto> UpdateAsync(int id, UpdateArchiveDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
