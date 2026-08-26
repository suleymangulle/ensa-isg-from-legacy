using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;

namespace Ensa.Application.Contracts.Risks;

/// <summary>Work equipment (legacy: Cihaz) application service.</summary>
public interface IEquipmentAppService : IApplicationService
{
    Task<EquipmentDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail projection: equipment, company, inspection report and attached documents.</summary>
    Task<EquipmentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<EquipmentListDto>> GetListAsync(
        GetEquipmentListInput input,
        CancellationToken cancellationToken = default);

    Task<EquipmentDto> CreateAsync(CreateEquipmentDto input, CancellationToken cancellationToken = default);

    Task<EquipmentDto> UpdateAsync(int id, UpdateEquipmentDto input, CancellationToken cancellationToken = default);

    /// <summary>Refuses to delete equipment flagged as not deletable.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Equipment whose periodic inspection is missing or past due.</summary>
    Task<ListResultDto<EquipmentListDto>> GetOverdueInspectionsAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default);

    // --------------------------------------------------------------- Documents

    Task<ListResultDto<EquipmentDocumentDto>> GetDocumentsAsync(
        int equipmentId,
        CancellationToken cancellationToken = default);

    Task<EquipmentDocumentDto> AddDocumentAsync(
        int equipmentId,
        CreateEquipmentDocumentDto input,
        CancellationToken cancellationToken = default);

    Task RemoveDocumentAsync(
        int equipmentId,
        int equipmentDocumentId,
        CancellationToken cancellationToken = default);
}
