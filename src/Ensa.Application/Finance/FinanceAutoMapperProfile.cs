using AutoMapper;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Domain.Finance;

namespace Ensa.Application.Finance;

/// <summary>
/// Mappings for the Finance module (invoices, cash registers, statutory fines).
/// <para>
/// Rules, following <c>CompanyAutoMapperProfile</c>:
/// <list type="bullet">
/// <item>Audit fields carry the same names on the base DTOs, so they map automatically on reads.</item>
/// <item>Input DTO to entity mappings <b>ignore</b> <c>Id</c>, <c>TenantId</c> and every audit
/// field — those are set by the interceptor, never by a request payload.</item>
/// <item>Derived monetary figures are ignored on the way in: they are computed by
/// <c>IInvoiceManager</c> from the lines, so accepting them from a client would let a caller
/// state a total that its own lines do not add up to.</item>
/// <item>Navigation DTOs are not mapped here; the app service projects them by hand.</item>
/// </list>
/// </para>
/// </summary>
public class FinanceAutoMapperProfile : Profile
{
    public FinanceAutoMapperProfile()
    {
        ConfigureInvoice();
        ConfigureCashRegister();
        ConfigurePenalty();
    }

    private void ConfigureInvoice()
    {
        CreateMap<Invoice, InvoiceDto>();
        CreateMap<Invoice, InvoiceListDto>();
        CreateMap<InvoiceLine, InvoiceLineDto>();

        CreateMap<CreateInvoiceDto, Invoice>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Set by the service after uniqueness validation or number generation.
            .ForMember(d => d.InvoiceNo, o => o.Ignore())
            // Derived from the lines by IInvoiceManager — never from the payload.
            .ForMember(d => d.Total, o => o.Ignore())
            .ForMember(d => d.VatTotal, o => o.Ignore())
            .ForMember(d => d.GeneralTotal, o => o.Ignore())
            .ForMember(d => d.InWords, o => o.Ignore());

        CreateMap<UpdateInvoiceDto, Invoice>()
            .IncludeBase<CreateInvoiceDto, Invoice>();

        CreateMap<CreateInvoiceLineDto, InvoiceLine>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Owner is taken from the route, not the body.
            .ForMember(d => d.InvoiceId, o => o.Ignore())
            // Computed by IInvoiceManager.CalculateLineTotals.
            .ForMember(d => d.TotalAmount, o => o.Ignore())
            .ForMember(d => d.VatAmount, o => o.Ignore())
            .ForMember(d => d.GrossWithVatAmount, o => o.Ignore());

        CreateMap<UpdateInvoiceLineDto, InvoiceLine>()
            .IncludeBase<CreateInvoiceLineDto, InvoiceLine>();
    }

    private void ConfigureCashRegister()
    {
        CreateMap<CashRegister, CashRegisterDto>();
        CreateMap<CashRegister, CashRegisterListDto>();
        CreateMap<CashTransaction, CashTransactionDto>();

        CreateMap<CreateCashRegisterDto, CashRegister>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore());

        CreateMap<UpdateCashRegisterDto, CashRegister>()
            .IncludeBase<CreateCashRegisterDto, CashRegister>()
            // Only the update input carries the active flag.
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        CreateMap<CreateCashTransactionDto, CashTransaction>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            // A new movement is always live; voiding is an explicit operation.
            .ForMember(d => d.IsActive, o => o.Ignore())
            // Defaulted to "now" by the service when the payload leaves it out.
            .ForMember(d => d.OperationDate, o => o.Ignore());
    }

    private void ConfigurePenalty()
    {
        CreateMap<Penalty, PenaltyDto>();
        CreateMap<Penalty, PenaltyListDto>();
        CreateMap<PenaltyAmount, PenaltyAmountDto>();
        CreateMap<PenaltySurvey, PenaltySurveyDto>();
        CreateMap<PenaltySurvey, PenaltySurveyListDto>();
        CreateMap<PenaltySurveyLine, PenaltySurveyLineDto>();

        // Penalty is host data: FullAuditedEntity, so there is no TenantId to ignore.
        CreateMap<CreatePenaltyDto, Penalty>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore());

        CreateMap<UpdatePenaltyDto, Penalty>()
            .IncludeBase<CreatePenaltyDto, Penalty>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        CreateMap<CreatePenaltyAmountDto, PenaltyAmount>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.PenaltyId, o => o.Ignore());

        CreateMap<UpdatePenaltyAmountDto, PenaltyAmount>()
            .IncludeBase<CreatePenaltyAmountDto, PenaltyAmount>();

        CreateMap<CreatePenaltySurveyDto, PenaltySurvey>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore());

        CreateMap<UpdatePenaltySurveyDto, PenaltySurvey>()
            .IncludeBase<CreatePenaltySurveyDto, PenaltySurvey>();

        // CreatePenaltySurveyLineDto has no entity mapping on purpose: the amount and the
        // multiplier are resolved from the fine catalogue, so the line is built explicitly in
        // PenaltySurveyAppService rather than mapped from the request.
    }
}
