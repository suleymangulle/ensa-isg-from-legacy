using AutoMapper;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Domain.Reports;

namespace Ensa.Application.Reports;

/// <summary>
/// Mappings for the Reports module (activity reports, year-end review reports, OHS reports).
/// <para>
/// Rules, following <c>CompanyAutoMapperProfile</c>:
/// <list type="bullet">
/// <item>Audit fields carry the same names on the base DTOs, so they map automatically on reads.</item>
/// <item>Input DTO to entity mappings <b>ignore</b> <c>Id</c>, <c>TenantId</c> and every audit
/// field.</item>
/// <item><c>OhsReport</c> has no input mapping at all: it is produced by the reporting engine and
/// exposed read-only.</item>
/// <item>Navigation DTOs are not mapped here; the app service projects them by hand.</item>
/// </list>
/// </para>
/// </summary>
public class ReportAutoMapperProfile : Profile
{
    public ReportAutoMapperProfile()
    {
        ConfigureActivityReport();
        ConfigureYearEndReviewReport();
        ConfigureOhsReport();
    }

    private void ConfigureActivityReport()
    {
        CreateMap<ActivityReport, ActivityReportDto>();
        CreateMap<ActivityReport, ActivityReportListDto>();
        CreateMap<ActivityReportLine, ActivityReportLineDto>();

        CreateMap<CreateActivityReportDto, ActivityReport>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateActivityReportDto, ActivityReport>()
            .IncludeBase<CreateActivityReportDto, ActivityReport>();

        CreateMap<CreateActivityReportLineDto, ActivityReportLine>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            // Owner comes from the route, not the body.
            .ForMember(d => d.ActivityReportId, o => o.Ignore());

        CreateMap<UpdateActivityReportLineDto, ActivityReportLine>()
            .IncludeBase<CreateActivityReportLineDto, ActivityReportLine>();
    }

    private void ConfigureYearEndReviewReport()
    {
        CreateMap<YearEndReviewReport, YearEndReviewReportDto>();
        CreateMap<YearEndReviewReport, YearEndReviewReportListDto>();
        CreateMap<YearEndReviewLine, YearEndReviewLineDto>();

        CreateMap<CreateYearEndReviewReportDto, YearEndReviewReport>()
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

        CreateMap<UpdateYearEndReviewReportDto, YearEndReviewReport>()
            .IncludeBase<CreateYearEndReviewReportDto, YearEndReviewReport>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        CreateMap<CreateYearEndReviewLineDto, YearEndReviewLine>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            .ForMember(d => d.YearEndReviewReportId, o => o.Ignore())
            // Applied by the service only after the tree invariants have been checked.
            .ForMember(d => d.ParentLineId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore());

        CreateMap<UpdateYearEndReviewLineDto, YearEndReviewLine>()
            .IncludeBase<CreateYearEndReviewLineDto, YearEndReviewLine>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));
    }

    private void ConfigureOhsReport()
    {
        CreateMap<OhsReport, OhsReportDto>();
        CreateMap<OhsReport, OhsReportListDto>();

        // No input mappings: OhsReport and its hazard-class breakdown are produced by the
        // reporting engine and exposed read-only.
    }
}
