using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Classifies every column that is a candidate for removal from <c>User</c>, and refuses to bless
/// one it cannot prove.
/// <para>
/// <b>Why a step and not a checklist.</b> The identity contract allows a column to be dropped only
/// once it is <c>MOVED_AND_VERIFIED</c> or <c>CONFIRMED_UNUSED</c>, and it says plainly that a
/// column is not dead merely because it currently holds nothing. A list in a document cannot
/// enforce that; this compares the source against the destination, row by row, and reports a
/// verdict per column.
/// </para>
/// <para>
/// It writes nothing. Run it immediately before the migration that drops anything, and read the
/// verdicts rather than the summary line.
/// </para>
/// </summary>
public sealed class UserColumnClassifyStep : IMigrationStep
{
    public int Order => 9200;

    public string Name => "classify-user-columns";

    public string Description => "Proves each User column was moved, before anything is dropped";

    private sealed record Candidate(string Column, string Destination, string Sql);

    /// <summary>
    /// Each entry counts the rows where the source and the destination <b>disagree</b>. Zero is the
    /// only passing answer.
    /// </summary>
    private static readonly Candidate[] Candidates =
    [
        // ---- UserProfile: a straight 1-1 copy, so any difference at all is a failure.
        Profile("Name", "ISNULL(u.Name,'') <> ISNULL(p.Name,'')"),
        Profile("LastName", "ISNULL(u.LastName,'') <> ISNULL(p.LastName,'')"),
        Profile("NationalId", "ISNULL(u.NationalId,'') <> ISNULL(p.NationalId,'')"),
        Profile("Address", "ISNULL(u.Address,'') <> ISNULL(p.Address,'')"),
        Profile("CityId", "ISNULL(u.CityId,-1) <> ISNULL(p.CityId,-1)"),
        Profile("DistrictId", "ISNULL(u.DistrictId,-1) <> ISNULL(p.DistrictId,-1)"),
        Profile("PhotoDocumentId", "ISNULL(u.PhotoDocumentId,-1) <> ISNULL(p.PhotoDocumentId,-1)"),
        Profile("Color", "ISNULL(u.Color,'') <> ISNULL(p.Color,'')"),
        Profile("IsActive", "u.IsActive <> p.IsActive"),
        Profile("ContractApproved", "u.ContractApproved <> p.ContractApproved"),

        // ---- UserEmployment
        Employment("HireDate", "ISNULL(u.HireDate,'1900-01-01') <> ISNULL(e.HireDate,'1900-01-01')"),
        Employment("TerminationDate", "ISNULL(u.TerminationDate,'1900-01-01') <> ISNULL(e.TerminationDate,'1900-01-01')"),
        Employment("GrossSalary", "ISNULL(u.GrossSalary,-1) <> ISNULL(e.GrossSalary,-1)"),
        Employment("PartTime", "u.PartTime <> e.PartTime"),

        // ---- UserMedulaCredential, only for the users that have one.
        Medula("MedulaUserName", "ISNULL(u.MedulaUserName,'') <> ISNULL(m.MedulaUserName,'')"),
        Medula("MedulaPassword", "ISNULL(u.MedulaPassword,'') <> ISNULL(m.MedulaPassword,'')"),
        Medula("BranchCode", "ISNULL(u.BranchCode,'') <> ISNULL(m.BranchCode,'')"),
    ];

    private static Candidate Profile(string column, string disagreement) => new(
        column, "UserProfile",
        $"SELECT COUNT(*) FROM ensa.[User] u JOIN ensa.UserProfile p ON p.UserId = u.Id WHERE {disagreement}");

    private static Candidate Employment(string column, string disagreement) => new(
        column, "UserEmployment",
        $"SELECT COUNT(*) FROM ensa.[User] u JOIN ensa.UserEmployment e ON e.UserId = u.Id WHERE {disagreement}");

    private static Candidate Medula(string column, string disagreement) => new(
        column, "UserMedulaCredential",
        $"SELECT COUNT(*) FROM ensa.[User] u JOIN ensa.UserMedulaCredential m ON m.UserId = u.Id WHERE {disagreement}");

    /// <summary>
    /// The ones whose destination has a different shape, so "compare the two columns" does not
    /// express the question.
    /// </summary>
    private static readonly Candidate[] Shaped =
    [
        // MustChangePassword: the profile may legitimately be true where the user row was false,
        // because resetting a password now sets it there. A user who has it set on the account and
        // NOT on the profile would be the loss.
        new("MustChangePassword", "UserProfile",
            "SELECT COUNT(*) FROM ensa.[User] u JOIN ensa.UserProfile p ON p.UserId = u.Id "
            + "WHERE u.MustChangePassword = 1 AND p.MustChangePassword = 0"),

        // StaffRole: the type carries the same enum, so the question is whether the link leads
        // back to it.
        new("StaffRole", "UserEmployment.UserTypeId -> UserType.StaffRole",
            "SELECT COUNT(*) FROM ensa.[User] u JOIN ensa.UserEmployment e ON e.UserId = u.Id "
            + "LEFT JOIN ensa.UserType t ON t.Id = e.UserTypeId "
            + "WHERE u.StaffRole <> 0 AND ISNULL(t.StaffRole, -1) <> u.StaffRole"),

        // OfficeId is one office among the assignments now, so it must appear among them.
        new("OfficeId", "UserOffice",
            "SELECT COUNT(*) FROM ensa.[User] u WHERE u.OfficeId IS NOT NULL AND NOT EXISTS ("
            + "SELECT 1 FROM ensa.UserOffice o WHERE o.UserId = u.Id AND o.OfficeId = u.OfficeId)"),

        new("MonthlyWorkDurationMinutes", "UserOffice",
            "SELECT COUNT(*) FROM ensa.[User] u WHERE u.MonthlyWorkDurationMinutes IS NOT NULL "
            + "AND u.MonthlyWorkDurationMinutes > 0 AND NOT EXISTS ("
            + "SELECT 1 FROM ensa.UserOffice o WHERE o.UserId = u.Id)"),

        // The three administrator flags are role assignments now.
        Role("SystemAdministrator", "SystemAdministrator"),
        Role("OrganizationAdmin", "OrganizationAdministrator"),
        Role("OfficeAdmin", "OfficeAdministrator"),

        // Gsm was folded into Identity's own PhoneNumber. A row still holding a number here that
        // is not there would be the loss.
        new("Gsm", "User.PhoneNumber",
            "SELECT COUNT(*) FROM ensa.[User] WHERE Gsm IS NOT NULL AND LEN(Gsm) > 0 "
            + "AND ISNULL(PhoneNumber,'') <> Gsm"),
    ];

    private static Candidate Role(string column, string roleName) => new(
        column, $"UserRole -> {roleName}",
        $"SELECT COUNT(*) FROM ensa.[User] u WHERE u.[{column}] = 1 AND NOT EXISTS ("
        + "SELECT 1 FROM ensa.UserRole ur JOIN ensa.Role r ON r.Id = ur.RoleId "
        + $"WHERE ur.UserId = u.Id AND r.Name = '{roleName}')");

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);

        var verified = 0;
        var failed = new List<string>();

        foreach (var candidate in Candidates.Concat(Shaped))
        {
            await using var command = new SqlCommand(candidate.Sql, connection) { CommandTimeout = 600 };
            var mismatches = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);

            if (mismatches == 0)
            {
                verified++;
                context.Logger.LogInformation(
                    "    MOVED_AND_VERIFIED  {Column,-28} -> {Destination}",
                    candidate.Column, candidate.Destination);
            }
            else
            {
                failed.Add($"{candidate.Column} ({mismatches})");
                context.Logger.LogError(
                    "    NOT VERIFIED        {Column,-28} -> {Destination}: {Count} row(s) disagree",
                    candidate.Column, candidate.Destination, mismatches);
            }
        }

        var note = $"{verified} verified, {failed.Count} not";

        if (failed.Count > 0)
        {
            context.Logger.LogError("    DO NOT DROP ANYTHING: {Failed}", string.Join(", ", failed));
            return new StepResult(verified + failed.Count, 0, failed.Count, "FAILED: " + note);
        }

        context.Logger.LogInformation(
            "    every candidate column is MOVED_AND_VERIFIED. PermissionGroupId is deliberately "
            + "absent: it is still referenced by code and has not been classified.");

        return new StepResult(verified, 0, 0, note);
    }
}
