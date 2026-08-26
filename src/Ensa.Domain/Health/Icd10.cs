using Ensa.Domain.Common;

namespace Ensa.Domain.Health;

/// <summary>
/// SKRS ICD-10 diagnosis code reference.
/// <para>Legacy equivalent: <c>SKRS_ICD10_T</c> (PK <c>ICD10Id</c> → <c>Id</c>).</para>
/// <para>
/// Host reference table — does NOT implement <c>IMultiTenant</c>; it is seeded by
/// <c>DbMigrator</c>. The hierarchy is built through the <see cref="ParentIcd10Id"/>
/// self-referencing FK; there are NO navigation properties.
/// </para>
/// </summary>
public class Icd10 : AuditedEntity, IActivatable
{
    /// <summary>ICD-10 diagnosis name. (Legacy: <c>ICD10_ADi</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ICD-10 code (e.g. "J45.9"). (Legacy: <c>ICD10_Kodu</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>ICD-10 code of the parent level. (Legacy: <c>ICD10_UstKodu</c>)</summary>
    public string? ParentCode { get; set; }

    /// <summary>Record of the parent level (self-referencing FK). (Legacy: <c>ICD10_UstIdNo</c>)</summary>
    public int? ParentIcd10Id { get; set; }

    /// <summary>Level within the hierarchy. (Legacy: <c>ICD10_Seviye</c>)</summary>
    public int? Level { get; set; }

    /// <summary>Whether the code is still in use. (Legacy: <c>Aktif</c> <c>bool?</c>)</summary>
    public bool IsActive { get; set; } = true;
}
