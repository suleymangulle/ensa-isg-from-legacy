using AutoMapper;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Domain.Common;
using Ensa.Domain.Health;

namespace Ensa.Application.Health;

/// <summary>
/// Mappings for the health module (medical examination forms, e-prescriptions and the
/// read-only SKRS reference catalogues).
/// <para>
/// Rules, as established by <c>CompanyAutoMapperProfile</c>:
/// <list type="bullet">
/// <item>Audit fields carry the same names on the base DTOs, so they map automatically.</item>
/// <item>Input DTO to entity mappings <b>ignore</b> <c>Id</c>, <c>TenantId</c> and every audit
/// field — those are decided by the save interceptor, not by the caller.</item>
/// <item>Navigation DTOs are not mapped here; the application service projects them by hand.</item>
/// <item>Fields the caller must not be able to set (IBYS outcome, service results,
/// cancellation state, derived BMI) are ignored on the input mappings as well.</item>
/// </list>
/// </para>
/// </summary>
public class HealthAutoMapperProfile : Profile
{
    public HealthAutoMapperProfile()
    {
        // ------------------------------------------- Medical examination form

        CreateMap<MedicalExaminationForm, MedicalExaminationFormDto>();

        // The list row deliberately carries no clinical field; the display names are
        // resolved with batched lookups in the application service.
        CreateMap<MedicalExaminationForm, MedicalExaminationFormListDto>()
            .ForMember(d => d.EmployeeFullName, o => o.Ignore())
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.PhysicianFullName, o => o.Ignore());

        CreateMap<CreateMedicalExaminationFormDto, MedicalExaminationForm>()
            .IgnoreSystemFields()
            // Derived by IHealthSurveillanceManager.CalculateBmi — never taken from input.
            .ForMember(d => d.BodyMassIndex, o => o.Ignore())
            // Owned by the IBYS submission flow — never taken from input.
            .ForMember(d => d.IbysStatus, o => o.Ignore())
            .ForMember(d => d.IbysQueryId, o => o.Ignore())
            .ForMember(d => d.IbysStatusCode, o => o.Ignore())
            .ForMember(d => d.IbysStatusMessage, o => o.Ignore())
            .ForMember(d => d.IbysGroupCode, o => o.Ignore());

        CreateMap<UpdateMedicalExaminationFormDto, MedicalExaminationForm>()
            .IncludeBase<CreateMedicalExaminationFormDto, MedicalExaminationForm>()
            // The group code is the one IBYS field an operator may set while batching.
            .ForMember(d => d.IbysGroupCode, o => o.MapFrom(s => s.IbysGroupCode));

        // ------------------------------------------------- Child collections

        CreateMap<MedicalExamComplaint, MedicalExamComplaintDto>();
        CreateMap<SaveMedicalExamComplaintDto, MedicalExamComplaint>()
            .IgnoreSystemFields()
            .ForMember(d => d.MedicalExaminationFormId, o => o.Ignore());

        CreateMap<MedicalExamPhysicalFinding, MedicalExamPhysicalFindingDto>();
        CreateMap<SaveMedicalExamPhysicalFindingDto, MedicalExamPhysicalFinding>()
            .IgnoreSystemFields()
            .ForMember(d => d.MedicalExaminationFormId, o => o.Ignore());

        CreateMap<MedicalExamLabTest, MedicalExamLabTestDto>();
        CreateMap<SaveMedicalExamLabTestDto, MedicalExamLabTest>()
            .IgnoreSystemFields()
            .ForMember(d => d.MedicalExaminationFormId, o => o.Ignore());

        CreateMap<MedicalExamHabit, MedicalExamHabitDto>();
        CreateMap<SaveMedicalExamHabitDto, MedicalExamHabit>()
            .IgnoreSystemFields()
            .ForMember(d => d.MedicalExaminationFormId, o => o.Ignore());

        CreateMap<MedicalExamWorkCondition, MedicalExamWorkConditionDto>();
        CreateMap<SaveMedicalExamWorkConditionDto, MedicalExamWorkCondition>()
            .IgnoreSystemFields()
            .ForMember(d => d.MedicalExaminationFormId, o => o.Ignore());

        CreateMap<MedicalExamImmunization, MedicalExamImmunizationDto>();
        CreateMap<SaveMedicalExamImmunizationDto, MedicalExamImmunization>()
            .IgnoreSystemFields()
            .ForMember(d => d.MedicalExaminationFormId, o => o.Ignore());

        // ------------------------------------------------------ E-prescription

        CreateMap<EPrescription, EPrescriptionDto>();

        CreateMap<EPrescription, EPrescriptionListDto>()
            .ForMember(d => d.PatientFullName, o => o.Ignore());

        CreateMap<CreateEPrescriptionDto, EPrescription>()
            .IgnoreSystemFields()
            // Everything below is written by the e-prescription service round trip.
            .ForMember(d => d.EPrescriptionCode, o => o.Ignore())
            .ForMember(d => d.Cancelled, o => o.Ignore())
            .ForMember(d => d.SubmissionDate, o => o.Ignore())
            .ForMember(d => d.ResultCode, o => o.Ignore())
            .ForMember(d => d.ResultMessage, o => o.Ignore())
            .ForMember(d => d.WarningMessage, o => o.Ignore());

        CreateMap<UpdateEPrescriptionDto, EPrescription>()
            .IncludeBase<CreateEPrescriptionDto, EPrescription>();

        CreateMap<EPrescriptionMedication, EPrescriptionMedicationDto>();
        CreateMap<SaveEPrescriptionMedicationDto, EPrescriptionMedication>()
            .IgnoreSystemFields()
            .ForMember(d => d.EPrescriptionId, o => o.Ignore());

        CreateMap<EPrescriptionDiagnosis, EPrescriptionDiagnosisDto>();
        CreateMap<SaveEPrescriptionDiagnosisDto, EPrescriptionDiagnosis>()
            .IgnoreSystemFields()
            .ForMember(d => d.EPrescriptionId, o => o.Ignore());

        // ------------------------------------------- SKRS reference catalogues

        CreateMap<Icd10, Icd10LookupDto>();
        CreateMap<Medication, MedicationLookupDto>();
    }
}

/// <summary>
/// Shared ignore list for the health module's input mappings.
/// </summary>
internal static class HealthMappingExtensions
{
    /// <summary>
    /// Ignores <c>Id</c>, <c>TenantId</c> and every audit field on a fully audited,
    /// tenant-scoped entity. These are set by the save interceptor.
    /// </summary>
    public static IMappingExpression<TSource, TDestination> IgnoreSystemFields<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> map)
        where TDestination : FullAuditedTenantEntity
        => map
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());
}
