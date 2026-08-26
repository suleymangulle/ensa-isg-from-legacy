namespace Ensa.Domain.Health;

/// <summary>
/// SKRS code list of medication administration routes (oral, intramuscular, intravenous, ...).
/// <para>Legacy equivalent: <c>SKRS_MedicationRoute_T</c>.</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class MedicationRoute : SkrsReferenceEntity;
