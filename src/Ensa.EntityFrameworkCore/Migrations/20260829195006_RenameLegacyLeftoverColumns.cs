using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// Renames columns whose names were carried over from the legacy schema unchanged: Turkish
    /// leftovers (<c>WorkplaceTelefonu</c>, <c>MachinesVeEquipments</c>, <c>PersonVeTitle</c>),
    /// a doubled word (<c>TaxTaxOffice</c>), names that described the wrong concept
    /// (<c>OrganizationTypeVerified</c>, <c>IsPerDate</c>, <c>BranchCode</c>, <c>PayableDigit</c>)
    /// and boolean columns that did not read as booleans (<c>Completed</c>, <c>Paid</c>, ...).
    ///
    /// <para>
    /// <b>Renames only — no column is dropped or added, so no value moves and nothing is lost.</b>
    /// The operations were written by hand from the property-level mapping: the scaffolder pairs a
    /// table's dropped and added columns positionally, which produced pairs such as
    /// <c>PasswordSent -&gt; IsHazardClassVerified</c> and would have swapped two flags' data. Each
    /// pair below names the column it actually belongs to, and <c>Down</c> is its exact inverse.
    /// </para>
    ///
    /// <para>
    /// The head-office index is dropped and recreated rather than renamed because its filter
    /// predicate names the renamed column; an index carries no data of its own.
    /// </para>
    /// </summary>
    public partial class RenameLegacyLeftoverColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Office_TenantId_HeadquarterOffice",
                schema: "ensa",
                table: "Office");

            migrationBuilder.RenameColumn(
                name: "HeadquarterCashRegister",
                schema: "ensa",
                table: "CashRegister",
                newName: "IsHeadquarterCashRegister");

            migrationBuilder.RenameIndex(
                name: "IX_CashRegister_TenantId_HeadquarterCashRegister",
                schema: "ensa",
                table: "CashRegister",
                newName: "IX_CashRegister_TenantId_IsHeadquarterCashRegister");

            migrationBuilder.RenameColumn(
                name: "TaxTaxOffice",
                schema: "ensa",
                table: "Company",
                newName: "TaxOffice");

            migrationBuilder.RenameColumn(
                name: "OrganizationTypeVerified",
                schema: "ensa",
                table: "Company",
                newName: "IsHazardClassVerified");

            migrationBuilder.RenameColumn(
                name: "SolutionPartner",
                schema: "ensa",
                table: "Company",
                newName: "IsSolutionPartner");

            migrationBuilder.RenameColumn(
                name: "PasswordSent",
                schema: "ensa",
                table: "Company",
                newName: "AreEmployeePasswordsSent");

            migrationBuilder.RenameColumn(
                name: "QuoteVatIncluded",
                schema: "ensa",
                table: "Company",
                newName: "IsQuoteVatIncluded");

            migrationBuilder.RenameColumn(
                name: "UserLimit",
                schema: "ensa",
                table: "Company",
                newName: "IsDistanceLearningUserLimitEnabled");

            migrationBuilder.RenameColumn(
                name: "VisitSpecialist",
                schema: "ensa",
                table: "Company",
                newName: "SpecialistVisitMinutes");

            migrationBuilder.RenameColumn(
                name: "VisitPhysician",
                schema: "ensa",
                table: "Company",
                newName: "PhysicianVisitMinutes");

            migrationBuilder.RenameColumn(
                name: "InvoiceAmountKh",
                schema: "ensa",
                table: "Company",
                newName: "UnofficialInvoiceAmount");

            migrationBuilder.RenameColumn(
                name: "GrContractAmount",
                schema: "ensa",
                table: "Company",
                newName: "GroupContractAmount");

            migrationBuilder.RenameColumn(
                name: "PayableDigit",
                schema: "ensa",
                table: "Company",
                newName: "ExpectedPaymentAmount");

            migrationBuilder.RenameColumn(
                name: "SslUse",
                schema: "ensa",
                table: "EmailSettings",
                newName: "UseSsl");

            migrationBuilder.RenameColumn(
                name: "Deletable",
                schema: "ensa",
                table: "Equipment",
                newName: "IsDeletable");

            migrationBuilder.RenameColumn(
                name: "IsPerDate",
                schema: "ensa",
                table: "Incident",
                newName: "ReturnToWorkDate");

            migrationBuilder.RenameColumn(
                name: "HeadquarterOffice",
                schema: "ensa",
                table: "Office",
                newName: "IsHeadquarterOffice");

            migrationBuilder.RenameColumn(
                name: "TotalMonthlyFazlaOvertimeDuration",
                schema: "ensa",
                table: "OhsReport",
                newName: "TotalMonthlyOvertimeDuration");

            migrationBuilder.RenameColumn(
                name: "TaxTaxOffice",
                schema: "ensa",
                table: "Organization",
                newName: "TaxOffice");

            migrationBuilder.RenameColumn(
                name: "Paid",
                schema: "ensa",
                table: "OrganizationContract",
                newName: "IsPaid");

            migrationBuilder.RenameColumn(
                name: "TaxTaxOffice",
                schema: "ensa",
                table: "PenaltySurvey",
                newName: "TaxOffice");

            migrationBuilder.RenameColumn(
                name: "Paid",
                schema: "ensa",
                table: "ProspectOrganization",
                newName: "IsPaid");

            migrationBuilder.RenameColumn(
                name: "MailSent",
                schema: "ensa",
                table: "ProspectOrganization",
                newName: "IsMailSent");

            migrationBuilder.RenameColumn(
                name: "PhysicianExists",
                schema: "ensa",
                table: "ProspectOrganization",
                newName: "HasPhysician");

            migrationBuilder.RenameColumn(
                name: "WorkplaceTelefonu",
                schema: "ensa",
                table: "RiskAssessmentReport",
                newName: "WorkplacePhoneNumber");

            migrationBuilder.RenameColumn(
                name: "MachinesVeEquipments",
                schema: "ensa",
                table: "RiskAssessmentReport",
                newName: "MachineryAndEquipment");

            migrationBuilder.RenameColumn(
                name: "Encrypted",
                schema: "ensa",
                table: "SystemSetting",
                newName: "IsEncrypted");

            migrationBuilder.RenameColumn(
                name: "Transferred",
                schema: "ensa",
                table: "TrainingPlan",
                newName: "IsTransferred");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingPlan_TenantId_Transferred",
                schema: "ensa",
                table: "TrainingPlan",
                newName: "IX_TrainingPlan_TenantId_IsTransferred");

            migrationBuilder.RenameColumn(
                name: "MainItem",
                schema: "ensa",
                table: "TreeNode",
                newName: "IsMainItem");

            migrationBuilder.RenameColumn(
                name: "BranchCode",
                schema: "ensa",
                table: "UserMedulaCredential",
                newName: "MedicalSpecialtyCode");

            migrationBuilder.RenameColumn(
                name: "Authorized",
                schema: "ensa",
                table: "UserPermission",
                newName: "IsAuthorized");

            migrationBuilder.RenameColumn(
                name: "ContractApproved",
                schema: "ensa",
                table: "UserProfile",
                newName: "IsContractApproved");

            migrationBuilder.RenameColumn(
                name: "Completed",
                schema: "ensa",
                table: "Visit",
                newName: "IsCompleted");

            migrationBuilder.RenameColumn(
                name: "Transferred",
                schema: "ensa",
                table: "WorkPlan",
                newName: "IsTransferred");

            migrationBuilder.RenameIndex(
                name: "IX_WorkPlan_TenantId_Transferred",
                schema: "ensa",
                table: "WorkPlan",
                newName: "IX_WorkPlan_TenantId_IsTransferred");

            migrationBuilder.RenameColumn(
                name: "Deletable",
                schema: "ensa",
                table: "WorkplaceDepartment",
                newName: "IsDeletable");

            migrationBuilder.RenameColumn(
                name: "PersonVeTitle",
                schema: "ensa",
                table: "YearEndReviewLine",
                newName: "PersonAndTitle");

            migrationBuilder.RenameColumn(
                name: "ResultVeComment",
                schema: "ensa",
                table: "YearEndReviewLine",
                newName: "ResultAndComment");

            migrationBuilder.CreateIndex(
                name: "IX_Office_TenantId_IsHeadquarterOffice",
                schema: "ensa",
                table: "Office",
                column: "TenantId",
                unique: true,
                filter: "[IsHeadquarterOffice] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Office_TenantId_IsHeadquarterOffice",
                schema: "ensa",
                table: "Office");

            migrationBuilder.RenameColumn(
                name: "ResultAndComment",
                schema: "ensa",
                table: "YearEndReviewLine",
                newName: "ResultVeComment");

            migrationBuilder.RenameColumn(
                name: "PersonAndTitle",
                schema: "ensa",
                table: "YearEndReviewLine",
                newName: "PersonVeTitle");

            migrationBuilder.RenameColumn(
                name: "IsDeletable",
                schema: "ensa",
                table: "WorkplaceDepartment",
                newName: "Deletable");

            migrationBuilder.RenameIndex(
                name: "IX_WorkPlan_TenantId_IsTransferred",
                schema: "ensa",
                table: "WorkPlan",
                newName: "IX_WorkPlan_TenantId_Transferred");

            migrationBuilder.RenameColumn(
                name: "IsTransferred",
                schema: "ensa",
                table: "WorkPlan",
                newName: "Transferred");

            migrationBuilder.RenameColumn(
                name: "IsCompleted",
                schema: "ensa",
                table: "Visit",
                newName: "Completed");

            migrationBuilder.RenameColumn(
                name: "IsContractApproved",
                schema: "ensa",
                table: "UserProfile",
                newName: "ContractApproved");

            migrationBuilder.RenameColumn(
                name: "IsAuthorized",
                schema: "ensa",
                table: "UserPermission",
                newName: "Authorized");

            migrationBuilder.RenameColumn(
                name: "MedicalSpecialtyCode",
                schema: "ensa",
                table: "UserMedulaCredential",
                newName: "BranchCode");

            migrationBuilder.RenameColumn(
                name: "IsMainItem",
                schema: "ensa",
                table: "TreeNode",
                newName: "MainItem");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingPlan_TenantId_IsTransferred",
                schema: "ensa",
                table: "TrainingPlan",
                newName: "IX_TrainingPlan_TenantId_Transferred");

            migrationBuilder.RenameColumn(
                name: "IsTransferred",
                schema: "ensa",
                table: "TrainingPlan",
                newName: "Transferred");

            migrationBuilder.RenameColumn(
                name: "IsEncrypted",
                schema: "ensa",
                table: "SystemSetting",
                newName: "Encrypted");

            migrationBuilder.RenameColumn(
                name: "MachineryAndEquipment",
                schema: "ensa",
                table: "RiskAssessmentReport",
                newName: "MachinesVeEquipments");

            migrationBuilder.RenameColumn(
                name: "WorkplacePhoneNumber",
                schema: "ensa",
                table: "RiskAssessmentReport",
                newName: "WorkplaceTelefonu");

            migrationBuilder.RenameColumn(
                name: "HasPhysician",
                schema: "ensa",
                table: "ProspectOrganization",
                newName: "PhysicianExists");

            migrationBuilder.RenameColumn(
                name: "IsMailSent",
                schema: "ensa",
                table: "ProspectOrganization",
                newName: "MailSent");

            migrationBuilder.RenameColumn(
                name: "IsPaid",
                schema: "ensa",
                table: "ProspectOrganization",
                newName: "Paid");

            migrationBuilder.RenameColumn(
                name: "TaxOffice",
                schema: "ensa",
                table: "PenaltySurvey",
                newName: "TaxTaxOffice");

            migrationBuilder.RenameColumn(
                name: "IsPaid",
                schema: "ensa",
                table: "OrganizationContract",
                newName: "Paid");

            migrationBuilder.RenameColumn(
                name: "TaxOffice",
                schema: "ensa",
                table: "Organization",
                newName: "TaxTaxOffice");

            migrationBuilder.RenameColumn(
                name: "TotalMonthlyOvertimeDuration",
                schema: "ensa",
                table: "OhsReport",
                newName: "TotalMonthlyFazlaOvertimeDuration");

            migrationBuilder.RenameColumn(
                name: "IsHeadquarterOffice",
                schema: "ensa",
                table: "Office",
                newName: "HeadquarterOffice");

            migrationBuilder.RenameColumn(
                name: "ReturnToWorkDate",
                schema: "ensa",
                table: "Incident",
                newName: "IsPerDate");

            migrationBuilder.RenameColumn(
                name: "IsDeletable",
                schema: "ensa",
                table: "Equipment",
                newName: "Deletable");

            migrationBuilder.RenameColumn(
                name: "UseSsl",
                schema: "ensa",
                table: "EmailSettings",
                newName: "SslUse");

            migrationBuilder.RenameColumn(
                name: "ExpectedPaymentAmount",
                schema: "ensa",
                table: "Company",
                newName: "PayableDigit");

            migrationBuilder.RenameColumn(
                name: "GroupContractAmount",
                schema: "ensa",
                table: "Company",
                newName: "GrContractAmount");

            migrationBuilder.RenameColumn(
                name: "UnofficialInvoiceAmount",
                schema: "ensa",
                table: "Company",
                newName: "InvoiceAmountKh");

            migrationBuilder.RenameColumn(
                name: "PhysicianVisitMinutes",
                schema: "ensa",
                table: "Company",
                newName: "VisitPhysician");

            migrationBuilder.RenameColumn(
                name: "SpecialistVisitMinutes",
                schema: "ensa",
                table: "Company",
                newName: "VisitSpecialist");

            migrationBuilder.RenameColumn(
                name: "IsDistanceLearningUserLimitEnabled",
                schema: "ensa",
                table: "Company",
                newName: "UserLimit");

            migrationBuilder.RenameColumn(
                name: "IsQuoteVatIncluded",
                schema: "ensa",
                table: "Company",
                newName: "QuoteVatIncluded");

            migrationBuilder.RenameColumn(
                name: "AreEmployeePasswordsSent",
                schema: "ensa",
                table: "Company",
                newName: "PasswordSent");

            migrationBuilder.RenameColumn(
                name: "IsSolutionPartner",
                schema: "ensa",
                table: "Company",
                newName: "SolutionPartner");

            migrationBuilder.RenameColumn(
                name: "IsHazardClassVerified",
                schema: "ensa",
                table: "Company",
                newName: "OrganizationTypeVerified");

            migrationBuilder.RenameColumn(
                name: "TaxOffice",
                schema: "ensa",
                table: "Company",
                newName: "TaxTaxOffice");

            migrationBuilder.RenameIndex(
                name: "IX_CashRegister_TenantId_IsHeadquarterCashRegister",
                schema: "ensa",
                table: "CashRegister",
                newName: "IX_CashRegister_TenantId_HeadquarterCashRegister");

            migrationBuilder.RenameColumn(
                name: "IsHeadquarterCashRegister",
                schema: "ensa",
                table: "CashRegister",
                newName: "HeadquarterCashRegister");

            migrationBuilder.CreateIndex(
                name: "IX_Office_TenantId_HeadquarterOffice",
                schema: "ensa",
                table: "Office",
                column: "TenantId",
                unique: true,
                filter: "[HeadquarterOffice] = 1 AND [IsDeleted] = 0");
        }
    }
}
