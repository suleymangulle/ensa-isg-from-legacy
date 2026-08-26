using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS work environment code. These codes are used in the
/// <c>Health.MedicalExaminationForm.IbysWorkEnvironmentCodes</c> column of the examination form.
/// <para>Legacy equivalent: <c>IBYSWorkOrtamlari_T</c>.</para>
/// <para>
/// The link to the parent grouping still goes through <see cref="TypeCode"/> as it did in the
/// legacy system (see <see cref="IbysWorkEnvironmentType.TypeCode"/>); the
/// <see cref="IbysWorkEnvironmentTypeId"/> FK was added on top of it to make joins easier.
/// There are NO navigation properties.
/// </para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysWorkEnvironment : AuditedEntity, IActivatable
{
    /// <summary>IBYS work environment code. (Legacy: <c>OrtamKodu</c>)</summary>
    public int EnvironmentCode { get; set; }

    /// <summary>Name of the work environment. (Legacy: <c>Ortam</c>)</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Code of the environment type this record belongs to. (Legacy: <c>TurKodu</c>)</summary>
    public int TypeCode { get; set; }

    /// <summary>
    /// NORMALISATION (new column): FK of the environment type record;
    /// it is populated during seeding by matching on <see cref="TypeCode"/>.
    /// </summary>
    public int? IbysWorkEnvironmentTypeId { get; set; }

    /// <summary>Whether the code is still in use. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
