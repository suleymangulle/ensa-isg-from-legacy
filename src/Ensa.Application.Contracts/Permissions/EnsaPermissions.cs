using System.Reflection;

namespace Ensa.Application.Contracts.Permissions;

/// <summary>
/// Every permission name in the application.
/// <para>
/// The rule: each permission name has the form <c>Ensa.{Module}[.{Operation}]</c>.
/// <c>Default</c> means permission to view and list the module.
/// </para>
/// <para>
/// These constants are used both as <c>AuthorizationPolicy</c> names and as the
/// <c>ensa:permission</c> claim values carried in the token.
/// </para>
/// </summary>
public static class EnsaPermissions
{
    /// <summary>Root prefix shared by every permission name.</summary>
    public const string GroupName = "Ensa";

    // ---------------------------------------------------------------- System

    /// <summary>Organization (tenant) management — host administrators only.</summary>
    public static class Tenant
    {
        public const string Default = GroupName + ".Tenant";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Office
    {
        public const string Default = GroupName + ".Office";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class User
    {
        public const string Default = GroupName + ".User";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    public static class Role
    {
        public const string Default = GroupName + ".Role";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>Permission definitions and user/role permission assignments.</summary>
    public static class Permission
    {
        public const string Default = GroupName + ".Permission";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Menu
    {
        public const string Default = GroupName + ".Menu";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    // -------------------------------------------------------------- Business

    public static class Company
    {
        public const string Default = GroupName + ".Company";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class CompanyEmployee
    {
        public const string Default = GroupName + ".CompanyEmployee";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    public static class WorkplaceDepartment
    {
        public const string Default = GroupName + ".WorkplaceDepartment";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Document
    {
        public const string Default = GroupName + ".Document";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Form
    {
        public const string Default = GroupName + ".Form";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    // ------------------------------------------------------------------ OHS

    public static class Training
    {
        public const string Default = GroupName + ".Training";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    public static class TrainingPlan
    {
        public const string Default = GroupName + ".TrainingPlan";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class WorkPlan
    {
        public const string Default = GroupName + ".WorkPlan";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class Activity
    {
        public const string Default = GroupName + ".Activity";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class RiskAssessment
    {
        public const string Default = GroupName + ".RiskAssessment";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    /// <summary>Corrective and preventive action.</summary>
    public static class CorrectiveAction
    {
        public const string Default = GroupName + ".CorrectiveAction";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class FieldObservation
    {
        public const string Default = GroupName + ".FieldObservation";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    /// <summary>Workplace accidents and near-miss incidents.</summary>
    public static class Incident
    {
        public const string Default = GroupName + ".Incident";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class EmergencyPlan
    {
        public const string Default = GroupName + ".EmergencyPlan";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class Equipment
    {
        public const string Default = GroupName + ".Equipment";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    // --------------------------------------------------------------- Health

    public static class MedicalExamination
    {
        public const string Default = GroupName + ".MedicalExamination";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class EPrescription
    {
        public const string Default = GroupName + ".EPrescription";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    /// <summary>ISG-KATIP / IBYS integration.</summary>
    public static class Ibys
    {
        public const string Default = GroupName + ".Ibys";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    // ---------------------------------------------------------------- Finance

    public static class Invoice
    {
        public const string Default = GroupName + ".Invoice";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class CashRegister
    {
        public const string Default = GroupName + ".CashRegister";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    /// <summary>Administrative fine definitions and records.</summary>
    public static class Penalty
    {
        public const string Default = GroupName + ".Penalty";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    // ---------------------------------------------------------- Communication

    public static class Visit
    {
        public const string Default = GroupName + ".Visit";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    public static class Mail
    {
        public const string Default = GroupName + ".Mail";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    public static class Message
    {
        public const string Default = GroupName + ".Message";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class SupportTicket
    {
        public const string Default = GroupName + ".SupportTicket";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
        public const string Approve = Default + ".Approve";
    }

    // --------------------------------------------------- Reports / reference

    public static class Report
    {
        public const string Default = GroupName + ".Report";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    /// <summary>Shared reference tables (city, occupation code, period, hazard and so on).</summary>
    public static class Lookups
    {
        public const string Default = GroupName + ".Lookups";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Export = Default + ".Export";
    }

    // ------------------------------------------------------------ Reflection

    private static readonly Lazy<string[]> AllPermissions = new(
        static () =>
        {
            var target = new List<string>(256);
            Collect(typeof(EnsaPermissions), target);
            return [.. target.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)];
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Returns every permission constant declared in this class, nested classes included.
    /// Used to register the authorization policies and to build the permission screen.
    /// </summary>
    public static IEnumerable<string> GetAll() => AllPermissions.Value;

    /// <summary>Checks whether the given permission is defined.</summary>
    public static bool IsDefined(string permission)
        => Array.BinarySearch(AllPermissions.Value, permission, StringComparer.Ordinal) >= 0;

    private static void Collect(Type type, List<string> target)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            // Only `public const string` fields.
            if (field is { IsLiteral: true, IsInitOnly: false }
                && field.FieldType == typeof(string)
                && field.GetRawConstantValue() is string value
                && value.Length > GroupName.Length
                && value.StartsWith(GroupName + ".", StringComparison.Ordinal))
            {
                target.Add(value);
            }
        }

        foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
        {
            Collect(nested, target);
        }
    }
}

/// <summary>
/// Ensa-specific claim types. They are written into the access token under these names and read
/// back under the same names by <c>ICurrentUser</c> and <c>ICurrentTenant</c>.
/// </summary>
public static class EnsaClaimTypes
{
    /// <summary>Id of the organization (tenant) the user belongs to. Absent means the host context.</summary>
    public const string TenantId = "ensa:tenantId";

    /// <summary>
    /// The client workplace the user belongs to. Absent for the provider's own staff, who are not
    /// restricted to one workplace.
    /// </summary>
    public const string CompanyId = "ensa:companyId";

    /// <summary>A permission name held by the user; the claim may appear multiple times.</summary>
    public const string Permission = "ensa:permission";
}

/// <summary>Fixed role names recognized by the system.</summary>
public static class EnsaRoles
{
    /// <summary>Host administrator. Holds every permission and may switch tenants.</summary>
    public const string SystemAdministrator = Ensa.Domain.Shared.EnsaRoleNames.SystemAdministrator;

    /// <summary>Organization (tenant) administrator.</summary>
    public const string OrganizationAdministrator = Ensa.Domain.Shared.EnsaRoleNames.OrganizationAdministrator;
}

/// <summary>OpenIddict scope names.</summary>
public static class EnsaScopes
{
    /// <summary>Scope that grants access to the Ensa API.</summary>
    public const string Api = "ensa";
}

/// <summary>HTTP headers used by the API, including the tenant override header for host administrators.</summary>
public static class EnsaHttpHeaders
{
    public const string TenantId = "X-Ensa-TenantId";
    public const string CorrelationId = "X-Correlation-ID";
}
