using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ensa");

            migrationBuilder.CreateTable(
                name: "Activity",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentActivityId = table.Column<int>(type: "int", nullable: true),
                    ActivityCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ActivityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActivityGroupId = table.Column<int>(type: "int", nullable: true),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    DefaultActivity = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCount = table.Column<int>(type: "int", nullable: false),
                    DefaultStartMonthOffset = table.Column<int>(type: "int", nullable: false),
                    DefaultElementCondition = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    RelatedTable = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RelationId = table.Column<int>(type: "int", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityDuty",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    DutyCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DutyId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDuty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityGroup",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityPeriod",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityPeriod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityReport",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReportStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityReportLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityReportId = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Value1 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Value2 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Value3 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityReportLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Archive",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleType = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LineId = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ModuleDescription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PreviousAddDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousAddedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archive", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignedSpecialist",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StaffRole = table.Column<int>(type: "int", nullable: false),
                    MonthlyWorkDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Sid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OhsProfApproval = table.Column<bool>(type: "bit", nullable: false),
                    OhsProfApprovalGuid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedSpecialist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignedSpecialistDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedSpecialistId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedSpecialistDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bank",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AccountNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ImageDocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bank", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashRegister",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashRegisterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    HeadquarterCashRegister = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegister", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashTransaction",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashRegisterId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    OperationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceModule = table.Column<int>(type: "int", nullable: false),
                    SourceRecordId = table.Column<int>(type: "int", nullable: true),
                    ExitItemId = table.Column<int>(type: "int", nullable: true),
                    OperationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Certificate",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CertificateCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "City",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PlateCodeCode = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Sid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SsiNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TaxTaxOffice = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EmployerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmployerMobilePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BusinessActivity = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OccupationCodeId = table.Column<int>(type: "int", nullable: true),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    OrganizationTypeVerified = table.Column<bool>(type: "bit", nullable: false),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: true),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: true),
                    IsOrganizationRecord = table.Column<bool>(type: "bit", nullable: false),
                    IsSubcontractor = table.Column<bool>(type: "bit", nullable: true),
                    SolutionPartner = table.Column<bool>(type: "bit", nullable: false),
                    WorkplaceType = table.Column<int>(type: "int", nullable: false),
                    HeadquarterCompanyId = table.Column<int>(type: "int", nullable: true),
                    BranchNo = table.Column<int>(type: "int", nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BranchContact = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BranchContactGsm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GroupCorporateId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InvoiceAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    QuarterId = table.Column<int>(type: "int", nullable: true),
                    NeighborhoodId = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Gsm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Cc = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WebUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AuthorizedPerson = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorizedPersonPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AuthorizedPersonEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FinanceOwner = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FinanceOwnerGsm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    RegionCode = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    VisitSpecialist = table.Column<int>(type: "int", nullable: true),
                    VisitPhysician = table.Column<int>(type: "int", nullable: true),
                    OhsKatipWorkerCount = table.Column<int>(type: "int", nullable: true),
                    FirstMonthProgramIncluded = table.Column<bool>(type: "bit", nullable: false),
                    UserLimit = table.Column<bool>(type: "bit", nullable: true),
                    PasswordSent = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyFeeOfficial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MonthlyFeeTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SpecialistFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PhysicianFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InvoiceAmountKh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GrContractAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PayableDigit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuoteVatIncluded = table.Column<bool>(type: "bit", nullable: false),
                    ShowUnofficialAmount = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WarningNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NoteRecordedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LogoDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyActivity",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    ActivityCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyActivity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyCheck",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CheckMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ControlItemDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCheck", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyCheckLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyControlItemId = table.Column<int>(type: "int", nullable: false),
                    ControlItemId = table.Column<int>(type: "int", nullable: false),
                    ControlItemStatus = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCheckLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyComplianceSummary",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsSafetyTrainingNoneCount = table.Column<int>(type: "int", nullable: true),
                    IsSafetyTrainingMissingCount = table.Column<int>(type: "int", nullable: true),
                    IsHealthTrainingNoneCount = table.Column<int>(type: "int", nullable: true),
                    IsHealthTrainingMissingCount = table.Column<int>(type: "int", nullable: true),
                    PreEmploymentHealthExaminationMissingCount = table.Column<int>(type: "int", nullable: true),
                    EquipmentExaminationMissingCount = table.Column<int>(type: "int", nullable: true),
                    CalculatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyComplianceSummary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmployee",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BirthLocation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    EducationLevel = table.Column<int>(type: "int", nullable: false),
                    MaritalStatus = table.Column<int>(type: "int", nullable: false),
                    ChildCount = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Gsm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HomeAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmergencyPerson = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmergencyPersonPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Duty = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OccupationCodeId = table.Column<int>(type: "int", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AssignedDepartmentId = table.Column<int>(type: "int", nullable: true),
                    AssignedDepartmentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreEmploymentExamination = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreEmploymentExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreEmploymentNextExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreEmploymentExaminationPerformedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PreEmploymentExaminationDocumentId = table.Column<int>(type: "int", nullable: true),
                    WorkMethodCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    WorkEnvironmentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    WorkEquipmentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmployee", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmployeeDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrainingId = table.Column<int>(type: "int", nullable: true),
                    TrainingPlanLineId = table.Column<int>(type: "int", nullable: true),
                    WorkPlanLineId = table.Column<int>(type: "int", nullable: true),
                    CertificateId = table.Column<int>(type: "int", nullable: true),
                    OtherCertificateName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TeamDocumentType = table.Column<int>(type: "int", nullable: false),
                    GroupCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IbysStatus = table.Column<int>(type: "int", nullable: false),
                    IbysSubmissionAttempt = table.Column<int>(type: "int", nullable: true),
                    IbysStatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IbysMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IbysNotificationNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IbysQueryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmployeeDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmployeeDuty",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    DutyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmployeeDuty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmployeeDutyDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeDutyId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmployeeDutyDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyLedgerEntry",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LedgerEntryType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OfficialAccount = table.Column<bool>(type: "bit", nullable: false),
                    SourceModule = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyLedgerEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyModule",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyModule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyStandardDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StandardDocumentId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyStandardDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyTag",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TagCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyTrainingProgressMode",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TransitionMode = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTrainingProgressMode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractTemplate",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    RevisionNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SpecialistUserId = table.Column<int>(type: "int", nullable: true),
                    PhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    ControlItemListId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    WorkPlanId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlItem",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ControlItemName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    PeriodUnit = table.Column<int>(type: "int", nullable: false),
                    PeriodValue = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlMeasure",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentifiedHazardId = table.Column<int>(type: "int", nullable: false),
                    Measure = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerCompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlMeasure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorrectiveAction",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Finding = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FindingDocumentId = table.Column<int>(type: "int", nullable: true),
                    ResultDocumentId = table.Column<int>(type: "int", nullable: true),
                    RiskCategory = table.Column<int>(type: "int", nullable: false),
                    OperationResult = table.Column<int>(type: "int", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OwnerCompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    FindingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FieldObservationLineId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectiveAction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkplaceDepartmentId = table.Column<int>(type: "int", nullable: false),
                    DocumentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    ExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExaminationPerformedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    WorkPlanLineId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "District",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    IlCode = table.Column<int>(type: "int", nullable: true),
                    DistrictCode = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_District", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Document",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentCategoryId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    DocumentName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerRecordId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Document", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentCategory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReportingArticleGroup = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Duty",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DutyCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DutyName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DutyLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Duty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSettings",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(704)", maxLength: 704, nullable: false),
                    Pop3Server = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    SslUse = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyActionPlan",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PreparedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RegistrationNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TeamsChief = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmergencyTeam = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WorkerRepresentative = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SupportStaff = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmployerOrDeputy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OccupationalSafetySpecialist = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    WorkplacePhysician = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProtectionEmployee = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EvacuationPlanDocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyActionPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyPlanSection",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmergencyActionPlanId = table.Column<int>(type: "int", nullable: false),
                    SectionType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyPlanSection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyTeamMember",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmergencyActionPlanId = table.Column<int>(type: "int", nullable: false),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    StaffRole = table.Column<int>(type: "int", nullable: false),
                    TeamType = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyTeamMember", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeExamAnswer",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ExamQuestionId = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeTrainingProgressId = table.Column<int>(type: "int", nullable: false),
                    TestType = table.Column<int>(type: "int", nullable: false),
                    CevaplanmaDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeExamAnswer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFamilyHistory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Relation = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFamilyHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeHealthInfo",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    BloodType = table.Column<int>(type: "int", nullable: false),
                    AllergyDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChronicIllnessDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeHealthInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeImmunization",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ImmunizationType = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeImmunization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTrainingLog",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    TrainingTopicId = table.Column<int>(type: "int", nullable: true),
                    Page = table.Column<int>(type: "int", nullable: true),
                    ElapsedDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    RemainingDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ExamId = table.Column<int>(type: "int", nullable: true),
                    ExamNote = table.Column<int>(type: "int", nullable: true),
                    EmployeeTrainingProgressId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTrainingLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTrainingProgress",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    TrainingTopicId = table.Column<int>(type: "int", nullable: true),
                    FirstTestCompleted = table.Column<bool>(type: "bit", nullable: false),
                    FirstTestNote = table.Column<int>(type: "int", nullable: true),
                    LatestTestCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LatestTestNote = table.Column<int>(type: "int", nullable: true),
                    ElapsedDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    ActivePage = table.Column<int>(type: "int", nullable: false),
                    TrainingSpecialistUserId = table.Column<int>(type: "int", nullable: true),
                    TrainingPhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTrainingProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkHistory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkSector = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PerformedJob = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EPrescription",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EPrescriptionCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ProtocolNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PatientNationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PatientCompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 21356, nullable: true),
                    DescriptionType = table.Column<int>(type: "int", nullable: false),
                    Cancelled = table.Column<bool>(type: "bit", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WarningMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EPrescription", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EPrescriptionDiagnosis",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EPrescriptionId = table.Column<int>(type: "int", nullable: false),
                    Icd10Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Icd10Id = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EPrescriptionDiagnosis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EPrescriptionMedication",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EPrescriptionId = table.Column<int>(type: "int", nullable: false),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    MedicationBarcode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UsageMethodId = table.Column<int>(type: "int", nullable: false),
                    UsageDoseUnitId = table.Column<int>(type: "int", nullable: false),
                    UsagePeriodUnitId = table.Column<int>(type: "int", nullable: false),
                    Box = table.Column<int>(type: "int", nullable: false),
                    Dose = table.Column<int>(type: "int", nullable: false),
                    DoseFraction = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    Period = table.Column<int>(type: "int", nullable: false),
                    MedicationDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: true),
                    MedicationDescriptionType = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EPrescriptionMedication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EquipmentType = table.Column<int>(type: "int", nullable: false),
                    ExaminationReport = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExaminationReportDocumentId = table.Column<int>(type: "int", nullable: true),
                    ExaminationPerformedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    Deletable = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    EquipmentDocumentTypeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExaminationPerformedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    WorkPlanLineId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentDocumentType",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentDocumentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ESignatureLicense",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    License = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESignatureLicense", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exam",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamAnswer",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamQuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAnswer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestion",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ParentExpenseCategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldObservationLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldObservationReportId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NonConformity = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Measures = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OwnerCompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    RiskCategory = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldObservationLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldObservationReport",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldObservationReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Form",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultForm = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Form", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormCategory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hazard",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HazardCategoryId = table.Column<int>(type: "int", nullable: false),
                    HazardTag = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RiskTag = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Measure = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Likelihood = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Severity = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Frequency = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hazard", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HazardCategory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsHazardSource = table.Column<bool>(type: "bit", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HazardCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysChildReferenceValue",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReferenceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ParentReferenceCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IbysRootReferenceValueId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysChildReferenceValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysEquipmentTopCategory",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCategoryName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysEquipmentTopCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysIsco08OccupationCode",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysIsco08OccupationCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysQuery",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    QueryType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    IbysMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IbysVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TimeStamp = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    XmlData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysQuery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysRootReferenceValue",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReferenceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysRootReferenceValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysServedWorkplace",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<int>(type: "int", nullable: false),
                    ServiceStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IbysNotificationNo = table.Column<string>(type: "nvarchar(192)", maxLength: 192, nullable: true),
                    XmlData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysServedWorkplace", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysWorkArrangement",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysWorkArrangement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysWorkEnvironment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnvironmentCode = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TypeCode = table.Column<int>(type: "int", nullable: false),
                    IbysWorkEnvironmentTypeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysWorkEnvironment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysWorkEnvironmentType",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeCode = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysWorkEnvironmentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IbysWorkEquipment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbysWorkEquipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Icd10",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ParentIcd10Id = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Icd10", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Icon",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LibraryCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IconCssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExtraFeature = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Icon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IconLibrary",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IconLibrary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdentifiedHazard",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    HazardCategoryId = table.Column<int>(type: "int", nullable: true),
                    HazardId = table.Column<int>(type: "int", nullable: true),
                    HazardTag = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActivityDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerPerson = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RiskTag = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Measure = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Likelihood = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Severity = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Frequency = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResidualLikelihood = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    ResidualSeverity = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    ResidualFrequency = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    ResidualRiskScore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    ResidualComment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentifiedHazard", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incident",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    IncidentType = table.Column<int>(type: "int", nullable: false),
                    AccidentType = table.Column<int>(type: "int", nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Expression = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    UnitSupervisorId = table.Column<int>(type: "int", nullable: true),
                    SupervisorFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LostWorkDays = table.Column<int>(type: "int", nullable: true),
                    IsPerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SsiNotificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentPerson",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    PersonType = table.Column<int>(type: "int", nullable: false),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentPerson", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    SourceModule = table.Column<int>(type: "int", nullable: false),
                    OfficeId = table.Column<int>(type: "int", nullable: true),
                    AccountCurrentName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InvoiceDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InWords = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GeneralTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ServiceItemId = table.Column<int>(type: "int", nullable: true),
                    LineDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Count = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<int>(type: "int", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossWithVatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTemplate",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleType = table.Column<int>(type: "int", nullable: false),
                    OnValue = table.Column<bool>(type: "bit", nullable: false),
                    DesignName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Design = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPrimaryDesign = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineNo = table.Column<int>(type: "int", nullable: true),
                    PageName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LogLevel = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mail",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sender = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentFormat = table.Column<int>(type: "int", nullable: false),
                    MailPriority = table.Column<int>(type: "int", nullable: false),
                    MailType = table.Column<int>(type: "int", nullable: false),
                    MailStatus = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailAttachment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MailId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAttachment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExamComplaint",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalExaminationFormId = table.Column<int>(type: "int", nullable: false),
                    ComplaintType = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExamComplaint", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExamHabit",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalExaminationFormId = table.Column<int>(type: "int", nullable: false),
                    HabitType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DailyQuantity = table.Column<int>(type: "int", nullable: true),
                    DurationYear = table.Column<int>(type: "int", nullable: true),
                    CessationYearBefore = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExamHabit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExamImmunization",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalExaminationFormId = table.Column<int>(type: "int", nullable: false),
                    ImmunizationType = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExamImmunization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExaminationForm",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    ExaminationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    HeightCm = table.Column<int>(type: "int", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    BodyMassIndex = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    BloodPressureSystolic = table.Column<int>(type: "int", nullable: true),
                    BloodPressureDiastolic = table.Column<int>(type: "int", nullable: true),
                    PulseRate = table.Column<int>(type: "int", nullable: true),
                    ChronicIllnessDeclaration = table.Column<string>(type: "nvarchar(max)", maxLength: 21356, nullable: true),
                    Opinion = table.Column<int>(type: "int", nullable: false),
                    OpinionDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 21356, nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", maxLength: 21356, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    IbysStatus = table.Column<int>(type: "int", nullable: false),
                    IbysQueryId = table.Column<int>(type: "int", nullable: true),
                    IbysStatusCode = table.Column<int>(type: "int", nullable: true),
                    IbysStatusMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IbysGroupCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IbysOccupationCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IbysWorkEnvironmentCodes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IbysWorkArrangementCodes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IbysWorkEquipmentCodes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExaminationForm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExamLabTest",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalExaminationFormId = table.Column<int>(type: "int", nullable: false),
                    LabTestType = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExamLabTest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExamPhysicalFinding",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalExaminationFormId = table.Column<int>(type: "int", nullable: false),
                    System = table.Column<int>(type: "int", nullable: false),
                    Finding = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10688, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExamPhysicalFinding", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalExamWorkCondition",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalExaminationFormId = table.Column<int>(type: "int", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    Suitable = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalExamWorkCondition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medication",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    GeneratorCompanyName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AtcCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AtcName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OutpatientReimbursementCondition = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    InpatientReimbursementCondition = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PrescriptionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeactivationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationDoseUnit",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    CodeTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationDoseUnit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationFrequencyUnit",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    CodeTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationFrequencyUnit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationRoute",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    CodeTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationRoute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Menu",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MenuTypeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserTypeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: true),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuElement",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    ParentMenuElementId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IconCssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssStyle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UrlParameters = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuElement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItem",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProjectCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Description1 = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Description2 = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LongDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    QueryStringKeys = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExtraAttributes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IconCssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssClass2 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssStyle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ModuleId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuNode",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    ParentMenuNodeId = table.Column<int>(type: "int", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IconCssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CssClass2 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuNode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuPage",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    MenuCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SettlementCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuPage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuType",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProjectCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RecipientId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplate",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageTemplateTypeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplateType",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplateType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Module",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParentModuleId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleArchive",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ModuleCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleArchive", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleArchiveItem",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleArchiveId = table.Column<int>(type: "int", nullable: false),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleArchiveItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Neighborhood",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NeighborhoodName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Neighborhood", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSubscriber",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscriber", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberSequence",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScopeId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LatestNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OccupationCode",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NaceCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupationCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Office",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    AuthorizedPerson = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    HeadquarterOffice = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Office", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficeExpense",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseTag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeExpense", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OhsReport",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    ModuleArchiveDetailId = table.Column<int>(type: "int", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StaffRole = table.Column<int>(type: "int", nullable: false),
                    DutyType = table.Column<int>(type: "int", nullable: false),
                    TotalMonthlyFazlaOvertimeDuration = table.Column<int>(type: "int", nullable: false),
                    TotalMinutes = table.Column<int>(type: "int", nullable: false),
                    UsedMonthlyMinutes = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OhsReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OhsReportHazardClassBreakdown",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OhsReportId = table.Column<int>(type: "int", nullable: false),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    CompanyCount = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OhsReportHazardClassBreakdown", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictApplications",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonWebKeySet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostLogoutRedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictScopes",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descriptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resources = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictScopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: false),
                    TaxTaxOffice = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    WebUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AuthorizedFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorizedPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AuthorizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LogoDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubscriptionEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaximumUserCount = table.Column<int>(type: "int", nullable: true),
                    MaximumCompanyCount = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationContract",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AuthorizedNationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AuthorizedName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorizedLastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ContractDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserCount = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: true),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Paid = table.Column<bool>(type: "bit", nullable: false),
                    SalesRepId = table.Column<int>(type: "int", nullable: true),
                    ReferenceCompanyId = table.Column<int>(type: "int", nullable: true),
                    AssignmentLogId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContractStatus = table.Column<int>(type: "int", nullable: false),
                    ContractNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContractStatusDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccountClosingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationContract", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationType",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationTypePermission",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationTypePermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parameter",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NotificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceiptDocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Penalty",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreeNodeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LawArticle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PenaltyArticle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LawArticleReferencedOffence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MultiplierCalculate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penalty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PenaltyAmount",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PenaltyId = table.Column<int>(type: "int", nullable: false),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    EmployeeCountRange = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidityYear = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyAmount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PenaltySurvey",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FacilityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FacilityOwner = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FacilityOwnerDuty = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FacilityOwnerGsm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmployerNameLastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    NeighborhoodId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InvoiceAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TaxTaxOffice = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    WorkerCount = table.Column<int>(type: "int", nullable: true),
                    SsiRegistrationNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    LogoDocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltySurvey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PenaltySurveyLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PenaltySurveyId = table.Column<int>(type: "int", nullable: false),
                    PenaltyId = table.Column<int>(type: "int", nullable: false),
                    SurveyAnswer = table.Column<bool>(type: "bit", nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MultiplierCalculate = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltySurveyLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Period",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PeriodValue = table.Column<int>(type: "int", nullable: false),
                    PeriodUnit = table.Column<int>(type: "int", nullable: false),
                    PeriodExpression = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Period", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentPermissionId = table.Column<int>(type: "int", nullable: true),
                    PermissionType = table.Column<int>(type: "int", nullable: false),
                    PermissionTarget = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PermissionDescription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RedMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PermissionRestrictionMode = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRestriction",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    UserTypeId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRestriction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionScope",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LinkType = table.Column<int>(type: "int", nullable: false),
                    LinkTargetId = table.Column<int>(type: "int", nullable: true),
                    LinkTargetCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PermissionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionScope", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Person",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FatherName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    NeighborhoodId = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProspectOrganization",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OrganizationTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsIndividual = table.Column<bool>(type: "bit", nullable: false),
                    IsOhsProvider = table.Column<bool>(type: "bit", nullable: false),
                    PhysicianExists = table.Column<bool>(type: "bit", nullable: false),
                    SpecialistCount = table.Column<int>(type: "int", nullable: true),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GrossWithVatPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Paid = table.Column<bool>(type: "bit", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    MailSent = table.Column<bool>(type: "bit", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: true),
                    SalesRepId = table.Column<int>(type: "int", nullable: true),
                    ReferenceCompanyId = table.Column<int>(type: "int", nullable: true),
                    AssignmentLogId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContractStatus = table.Column<int>(type: "int", nullable: false),
                    ContractNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContractStatusDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProspectOrganization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentControlMeasure",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    Measure = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentControlMeasure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentExposedGroup",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    Group = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentExposedGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentHistoryRecord",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    RecordType = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentHistoryRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentImprovementAction",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    Recommendation = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentImprovementAction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentParticipant",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    ParticipantType = table.Column<int>(type: "int", nullable: false),
                    CompanyEmployeeId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentParticipant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentProtectedGroup",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskAssessmentReportId = table.Column<int>(type: "int", nullable: false),
                    Group = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentProtectedGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessmentReport",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    WorkplaceTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BusinessActivity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkplaceAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    WorkplaceTelefonu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    WorkplaceDepartments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MachinesVeEquipments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HazardousArticles = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WasteOperations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PerformedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Employer = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SpecialistUserId = table.Column<int>(type: "int", nullable: true),
                    SpecialistFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    PhysicianFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    WorkerCount = table.Column<int>(type: "int", nullable: false),
                    ReportMethod = table.Column<int>(type: "int", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsStatic = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteOrigin",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteOrigin", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteOriginDistance",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginId = table.Column<int>(type: "int", nullable: true),
                    CityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteOriginDistance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesRep",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SalesRepType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRep", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepScreenField",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayedName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Show = table.Column<bool>(type: "bit", nullable: false),
                    ScreenType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    InTableShow = table.Column<bool>(type: "bit", nullable: false),
                    InPopupShow = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepScreenField", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItem",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefaultValue = table.Column<int>(type: "int", nullable: false),
                    VatRate = table.Column<int>(type: "int", nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotReport",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffCostBaseline",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StaffRole = table.Column<int>(type: "int", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SsiAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WorkedDayCount = table.Column<int>(type: "int", nullable: true),
                    OhsKatipMinutes = table.Column<int>(type: "int", nullable: false),
                    OhsKatipUsedMinutes = table.Column<int>(type: "int", nullable: false),
                    IncludesMeal = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffCostBaseline", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardDocument",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StandardDocumentName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StandardDocumentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlan",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanPermission",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicket",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Topic = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OpenedByUserId = table.Column<int>(type: "int", nullable: false),
                    ResponderUserId = table.Column<int>(type: "int", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketMessage",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupportTicketId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    FieldUserId = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSetting",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SettingType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Encrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Training",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TrainingCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TrainingGroupId = table.Column<int>(type: "int", nullable: true),
                    TrainingType = table.Column<int>(type: "int", nullable: false),
                    TopicGroup = table.Column<int>(type: "int", nullable: false),
                    MandatoryTraining = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IbysTrainingCode = table.Column<int>(type: "int", nullable: true),
                    IncludedInDefaultPlan = table.Column<bool>(type: "bit", nullable: false),
                    DefaultTraining = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCount = table.Column<int>(type: "int", nullable: false),
                    DefaultStartMonthOffset = table.Column<int>(type: "int", nullable: false),
                    DefaultElementCondition = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Training", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDuration",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDuration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingExam",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingExam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingGroup",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingGroupName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TrainingGroupCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPlan",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RevisionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SpecialistUserId = table.Column<int>(type: "int", nullable: true),
                    PhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Transferred = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPlanLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingPlanId = table.Column<int>(type: "int", nullable: false),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: true),
                    PerformedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PreviousLineId = table.Column<int>(type: "int", nullable: true),
                    ForApprovalSenderUserId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    ForApprovalSendingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstructorNationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InstructorTitle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InstructorFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InstructorUserId = table.Column<int>(type: "int", nullable: true),
                    TrainingLocation = table.Column<int>(type: "int", nullable: true),
                    TrainingType = table.Column<int>(type: "int", nullable: true),
                    IbysStatus = table.Column<int>(type: "int", nullable: false),
                    IbysQueryId = table.Column<int>(type: "int", nullable: true),
                    IbysStatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IbysMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlanLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingTopic",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    TopicTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PresentationAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PresentationPageCount = table.Column<int>(type: "int", nullable: false),
                    TopicOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTopic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingTopicDuration",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingTopicId = table.Column<int>(type: "int", nullable: false),
                    HazardClass = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTopicDuration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tree",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TreeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tree", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreeNode",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TreeId = table.Column<int>(type: "int", nullable: true),
                    TreeNodeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ParentTreeNodeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ParentTreeNodeId = table.Column<int>(type: "int", nullable: true),
                    TreeNodeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MainItem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeNode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    Gsm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhotoDocumentId = table.Column<int>(type: "int", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    StaffRole = table.Column<int>(type: "int", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PartTime = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyWorkDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: true),
                    OfficeAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    PermissionGroupId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OrganizationAdmin = table.Column<bool>(type: "bit", nullable: false),
                    SystemAdministrator = table.Column<bool>(type: "bit", nullable: false),
                    ContractApproved = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    MedulaUserName = table.Column<string>(type: "nvarchar(704)", maxLength: 704, nullable: true),
                    MedulaPassword = table.Column<string>(type: "nvarchar(704)", maxLength: 704, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMenuOverride",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMenuOverride", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserOffice",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    MonthlyWorkDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOffice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPermission",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    Authorized = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserType",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StaffRole = table.Column<int>(type: "int", nullable: false),
                    IconCssClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTypePermission",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserTypeId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypePermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visit",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: true),
                    End = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ScheduledWeek = table.Column<int>(type: "int", nullable: true),
                    ScheduledMonth = table.Column<int>(type: "int", nullable: true),
                    RegionCode = table.Column<int>(type: "int", nullable: true),
                    OtherCompanyDistanceKm = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkplaceDepartment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Deletable = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkplaceDepartment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkPlan",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RevisionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SpecialistUserId = table.Column<int>(type: "int", nullable: true),
                    PhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    ControlItemListId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Transferred = table.Column<bool>(type: "bit", nullable: false),
                    PreviousPlanId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkPlanLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkPlanId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    PerformedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PreviousLineId = table.Column<int>(type: "int", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ForApprovalSenderUserId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    ForApprovalSendingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    InstructorNationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InstructorUserId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPlanLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YearEndReviewLine",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YearEndReviewReportId = table.Column<int>(type: "int", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Work = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PersonVeTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RepeatCount = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UsedMethod = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultVeComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ParentLineId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YearEndReviewLine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YearEndReviewReport",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    MaleWorker = table.Column<int>(type: "int", nullable: true),
                    FemaleWorker = table.Column<int>(type: "int", nullable: true),
                    ChildWorker = table.Column<int>(type: "int", nullable: true),
                    YoungWorker = table.Column<int>(type: "int", nullable: true),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SpecialistUserId = table.Column<int>(type: "int", nullable: true),
                    SpecialistFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PhysicianUserId = table.Column<int>(type: "int", nullable: true),
                    PhysicianFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeputyFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YearEndReviewReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictAuthorizations",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "ensa",
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleClaim",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaim_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "ensa",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaim",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaim_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "ensa",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogin",
                schema: "ensa",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogin", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogin_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "ensa",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                schema: "ensa",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "ensa",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "ensa",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserToken",
                schema: "ensa",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserToken", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserToken_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "ensa",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictTokens",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: true),
                    AuthorizationId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedemptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "ensa",
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalSchema: "ensa",
                        principalTable: "OpenIddictAuthorizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_ActivityGroupId",
                schema: "ensa",
                table: "Activity",
                column: "ActivityGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_ParentActivityId",
                schema: "ensa",
                table: "Activity",
                column: "ParentActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_PeriodId",
                schema: "ensa",
                table: "Activity",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_RelatedTable_RelationId",
                schema: "ensa",
                table: "Activity",
                columns: new[] { "RelatedTable", "RelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_TenantId_ActivityCode",
                schema: "ensa",
                table: "Activity",
                columns: new[] { "TenantId", "ActivityCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_TenantId_IsActive",
                schema: "ensa",
                table: "Activity",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDuty_ActivityId",
                schema: "ensa",
                table: "ActivityDuty",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDuty_DutyId",
                schema: "ensa",
                table: "ActivityDuty",
                column: "DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGroup_IsActive",
                schema: "ensa",
                table: "ActivityGroup",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPeriod_ActivityId_PeriodId",
                schema: "ensa",
                table: "ActivityPeriod",
                columns: new[] { "ActivityId", "PeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPeriod_PeriodId",
                schema: "ensa",
                table: "ActivityPeriod",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReport_TenantId_CompanyId_ReportStart",
                schema: "ensa",
                table: "ActivityReport",
                columns: new[] { "TenantId", "CompanyId", "ReportStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReport_TenantId_ReportType",
                schema: "ensa",
                table: "ActivityReport",
                columns: new[] { "TenantId", "ReportType" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityReportLine_ActivityReportId_OrderNo",
                schema: "ensa",
                table: "ActivityReportLine",
                columns: new[] { "ActivityReportId", "OrderNo" });

            migrationBuilder.CreateIndex(
                name: "IX_Archive_CompanyId_Year_Month",
                schema: "ensa",
                table: "Archive",
                columns: new[] { "CompanyId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Archive_DocumentId",
                schema: "ensa",
                table: "Archive",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Archive_ModuleType_ModuleId_LineId",
                schema: "ensa",
                table: "Archive",
                columns: new[] { "ModuleType", "ModuleId", "LineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Archive_TenantId_CompanyId",
                schema: "ensa",
                table: "Archive",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignedSpecialist_CompanyId",
                schema: "ensa",
                table: "AssignedSpecialist",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedSpecialist_CompanyId_UserId_StaffRole",
                schema: "ensa",
                table: "AssignedSpecialist",
                columns: new[] { "CompanyId", "UserId", "StaffRole" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedSpecialist_UserId_IsActive",
                schema: "ensa",
                table: "AssignedSpecialist",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignedSpecialistDocument_AssignedSpecialistId_IsActive",
                schema: "ensa",
                table: "AssignedSpecialistDocument",
                columns: new[] { "AssignedSpecialistId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignedSpecialistDocument_DocumentId",
                schema: "ensa",
                table: "AssignedSpecialistDocument",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Bank_ImageDocumentId",
                schema: "ensa",
                table: "Bank",
                column: "ImageDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Bank_TenantId_IsActive",
                schema: "ensa",
                table: "Bank",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegister_OfficeId_IsActive",
                schema: "ensa",
                table: "CashRegister",
                columns: new[] { "OfficeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegister_TenantId_HeadquarterCashRegister",
                schema: "ensa",
                table: "CashRegister",
                columns: new[] { "TenantId", "HeadquarterCashRegister" });

            migrationBuilder.CreateIndex(
                name: "IX_CashTransaction_CashRegisterId_OperationDate",
                schema: "ensa",
                table: "CashTransaction",
                columns: new[] { "CashRegisterId", "OperationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashTransaction_ExitItemId",
                schema: "ensa",
                table: "CashTransaction",
                column: "ExitItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransaction_PaymentMethodId",
                schema: "ensa",
                table: "CashTransaction",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransaction_SourceModule_SourceRecordId",
                schema: "ensa",
                table: "CashTransaction",
                columns: new[] { "SourceModule", "SourceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificate_CertificateCode",
                schema: "ensa",
                table: "Certificate",
                column: "CertificateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_CityName",
                schema: "ensa",
                table: "City",
                column: "CityName");

            migrationBuilder.CreateIndex(
                name: "IX_City_PlateCodeCode",
                schema: "ensa",
                table: "City",
                column: "PlateCodeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Company_CityId",
                schema: "ensa",
                table: "Company",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CompanyName",
                schema: "ensa",
                table: "Company",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Company_DistrictId",
                schema: "ensa",
                table: "Company",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_GroupCorporateId",
                schema: "ensa",
                table: "Company",
                column: "GroupCorporateId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_HeadquarterCompanyId",
                schema: "ensa",
                table: "Company",
                column: "HeadquarterCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_LogoDocumentId",
                schema: "ensa",
                table: "Company",
                column: "LogoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_NeighborhoodId",
                schema: "ensa",
                table: "Company",
                column: "NeighborhoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_OccupationCodeId",
                schema: "ensa",
                table: "Company",
                column: "OccupationCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_OfficeId",
                schema: "ensa",
                table: "Company",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_OrganizationTypeId",
                schema: "ensa",
                table: "Company",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_QuarterId",
                schema: "ensa",
                table: "Company",
                column: "QuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_SubscriptionPlanId",
                schema: "ensa",
                table: "Company",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_TenantId_IsActive_IsOrganizationRecord",
                schema: "ensa",
                table: "Company",
                columns: new[] { "TenantId", "IsActive", "IsOrganizationRecord" });

            migrationBuilder.CreateIndex(
                name: "IX_Company_TenantId_OfficeId",
                schema: "ensa",
                table: "Company",
                columns: new[] { "TenantId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Company_TenantId_SsiNumber",
                schema: "ensa",
                table: "Company",
                columns: new[] { "TenantId", "SsiNumber" },
                unique: true,
                filter: "[SsiNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyActivity_ActivityId",
                schema: "ensa",
                table: "CompanyActivity",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyActivity_CompanyId_ActivityId",
                schema: "ensa",
                table: "CompanyActivity",
                columns: new[] { "CompanyId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheck_CompanyId_CheckMonth",
                schema: "ensa",
                table: "CompanyCheck",
                columns: new[] { "CompanyId", "CheckMonth" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheck_DocumentId",
                schema: "ensa",
                table: "CompanyCheck",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckLine_CompanyControlItemId_ControlItemId",
                schema: "ensa",
                table: "CompanyCheckLine",
                columns: new[] { "CompanyControlItemId", "ControlItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckLine_ControlItemId",
                schema: "ensa",
                table: "CompanyCheckLine",
                column: "ControlItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyComplianceSummary_CompanyId",
                schema: "ensa",
                table: "CompanyComplianceSummary",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_AssignedDepartmentId",
                schema: "ensa",
                table: "CompanyEmployee",
                column: "AssignedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_CompanyId_IsActive",
                schema: "ensa",
                table: "CompanyEmployee",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_OccupationCodeId",
                schema: "ensa",
                table: "CompanyEmployee",
                column: "OccupationCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_PreEmploymentExaminationDocumentId",
                schema: "ensa",
                table: "CompanyEmployee",
                column: "PreEmploymentExaminationDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_TenantId_CompanyId_NationalId",
                schema: "ensa",
                table: "CompanyEmployee",
                columns: new[] { "TenantId", "CompanyId", "NationalId" },
                unique: true,
                filter: "[NationalId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_TenantId_NationalId_IsActive",
                schema: "ensa",
                table: "CompanyEmployee",
                columns: new[] { "TenantId", "NationalId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployee_UserId",
                schema: "ensa",
                table: "CompanyEmployee",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_CertificateId",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_CompanyEmployeeId_TrainingId_DocumentDate",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                columns: new[] { "CompanyEmployeeId", "TrainingId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_DocumentId",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_GroupCode",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "GroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_IbysQueryId",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "IbysQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_IbysStatus",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "IbysStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_TrainingId",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_TrainingPlanLineId",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "TrainingPlanLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDocument_WorkPlanLineId",
                schema: "ensa",
                table: "CompanyEmployeeDocument",
                column: "WorkPlanLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDuty_CompanyEmployeeId_IsActive",
                schema: "ensa",
                table: "CompanyEmployeeDuty",
                columns: new[] { "CompanyEmployeeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDuty_DutyId",
                schema: "ensa",
                table: "CompanyEmployeeDuty",
                column: "DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDutyDocument_CompanyEmployeeDutyId_IsActive",
                schema: "ensa",
                table: "CompanyEmployeeDutyDocument",
                columns: new[] { "CompanyEmployeeDutyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeDutyDocument_DocumentId",
                schema: "ensa",
                table: "CompanyEmployeeDutyDocument",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLedgerEntry_CompanyId",
                schema: "ensa",
                table: "CompanyLedgerEntry",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLedgerEntry_SourceModule_OperationId",
                schema: "ensa",
                table: "CompanyLedgerEntry",
                columns: new[] { "SourceModule", "OperationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLedgerEntry_TenantId_CompanyId_Date",
                schema: "ensa",
                table: "CompanyLedgerEntry",
                columns: new[] { "TenantId", "CompanyId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModule_CompanyId",
                schema: "ensa",
                table: "CompanyModule",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModule_ModuleId",
                schema: "ensa",
                table: "CompanyModule",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModule_TenantId_CompanyId_ModuleId",
                schema: "ensa",
                table: "CompanyModule",
                columns: new[] { "TenantId", "CompanyId", "ModuleId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStandardDocument_CompanyId_StandardDocumentId",
                schema: "ensa",
                table: "CompanyStandardDocument",
                columns: new[] { "CompanyId", "StandardDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStandardDocument_DocumentId",
                schema: "ensa",
                table: "CompanyStandardDocument",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStandardDocument_StandardDocumentId",
                schema: "ensa",
                table: "CompanyStandardDocument",
                column: "StandardDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTag_CompanyId_TagCode",
                schema: "ensa",
                table: "CompanyTag",
                columns: new[] { "CompanyId", "TagCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTrainingProgressMode_CompanyId",
                schema: "ensa",
                table: "CompanyTrainingProgressMode",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTrainingProgressMode_UserId",
                schema: "ensa",
                table: "CompanyTrainingProgressMode",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_ApproverUserId",
                schema: "ensa",
                table: "ContractTemplate",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_CompanyId",
                schema: "ensa",
                table: "ContractTemplate",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_ControlItemListId",
                schema: "ensa",
                table: "ContractTemplate",
                column: "ControlItemListId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_PhysicianUserId",
                schema: "ensa",
                table: "ContractTemplate",
                column: "PhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_SpecialistUserId",
                schema: "ensa",
                table: "ContractTemplate",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplate_WorkPlanId",
                schema: "ensa",
                table: "ContractTemplate",
                column: "WorkPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlItem_PeriodId",
                schema: "ensa",
                table: "ControlItem",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlItem_TenantId_IsActive_SortOrder",
                schema: "ensa",
                table: "ControlItem",
                columns: new[] { "TenantId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlMeasure_IdentifiedHazardId",
                schema: "ensa",
                table: "ControlMeasure",
                column: "IdentifiedHazardId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlMeasure_OwnerCompanyEmployeeId",
                schema: "ensa",
                table: "ControlMeasure",
                column: "OwnerCompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlMeasure_TenantId_IsCompleted_DeadlineDate",
                schema: "ensa",
                table: "ControlMeasure",
                columns: new[] { "TenantId", "IsCompleted", "DeadlineDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlMeasure_TenantId_IsDeleted",
                schema: "ensa",
                table: "ControlMeasure",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_CompanyId",
                schema: "ensa",
                table: "CorrectiveAction",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_DeadlineDate",
                schema: "ensa",
                table: "CorrectiveAction",
                column: "DeadlineDate",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_FieldObservationLineId",
                schema: "ensa",
                table: "CorrectiveAction",
                column: "FieldObservationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_FindingDocumentId",
                schema: "ensa",
                table: "CorrectiveAction",
                column: "FindingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_OwnerCompanyEmployeeId",
                schema: "ensa",
                table: "CorrectiveAction",
                column: "OwnerCompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_ResultDocumentId",
                schema: "ensa",
                table: "CorrectiveAction",
                column: "ResultDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_TenantId_CompanyId_OperationResult",
                schema: "ensa",
                table: "CorrectiveAction",
                columns: new[] { "TenantId", "CompanyId", "OperationResult" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveAction_TenantId_IsDeleted",
                schema: "ensa",
                table: "CorrectiveAction",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentDocument_ActivityId",
                schema: "ensa",
                table: "DepartmentDocument",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentDocument_DocumentId",
                schema: "ensa",
                table: "DepartmentDocument",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentDocument_TenantId_ValidityDate",
                schema: "ensa",
                table: "DepartmentDocument",
                columns: new[] { "TenantId", "ValidityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentDocument_WorkplaceDepartmentId",
                schema: "ensa",
                table: "DepartmentDocument",
                column: "WorkplaceDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentDocument_WorkPlanLineId",
                schema: "ensa",
                table: "DepartmentDocument",
                column: "WorkPlanLineId");

            migrationBuilder.CreateIndex(
                name: "IX_District_CityId",
                schema: "ensa",
                table: "District",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_District_CityId_DistrictName",
                schema: "ensa",
                table: "District",
                columns: new[] { "CityId", "DistrictName" });

            migrationBuilder.CreateIndex(
                name: "IX_Document_DocumentCategoryId",
                schema: "ensa",
                table: "Document",
                column: "DocumentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_OwnerType_OwnerRecordId",
                schema: "ensa",
                table: "Document",
                columns: new[] { "OwnerType", "OwnerRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_Document_Sha256",
                schema: "ensa",
                table: "Document",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_Document_StorageName",
                schema: "ensa",
                table: "Document",
                column: "StorageName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Document_TenantId_CompanyId",
                schema: "ensa",
                table: "Document",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategory_ReportingArticleGroup",
                schema: "ensa",
                table: "DocumentCategory",
                column: "ReportingArticleGroup");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategory_TenantId_CategoryCode",
                schema: "ensa",
                table: "DocumentCategory",
                columns: new[] { "TenantId", "CategoryCode" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Duty_DutyCode",
                schema: "ensa",
                table: "Duty",
                column: "DutyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSettings_TenantId_IsActive",
                schema: "ensa",
                table: "EmailSettings",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyActionPlan_CompanyId",
                schema: "ensa",
                table: "EmergencyActionPlan",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyActionPlan_DocumentId",
                schema: "ensa",
                table: "EmergencyActionPlan",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyActionPlan_EvacuationPlanDocumentId",
                schema: "ensa",
                table: "EmergencyActionPlan",
                column: "EvacuationPlanDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyActionPlan_TenantId_CompanyId_ValidityDate",
                schema: "ensa",
                table: "EmergencyActionPlan",
                columns: new[] { "TenantId", "CompanyId", "ValidityDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyActionPlan_TenantId_IsDeleted",
                schema: "ensa",
                table: "EmergencyActionPlan",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPlanSection_EmergencyActionPlanId_OrderNo",
                schema: "ensa",
                table: "EmergencyPlanSection",
                columns: new[] { "EmergencyActionPlanId", "OrderNo" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPlanSection_TenantId_EmergencyActionPlanId_SectionType",
                schema: "ensa",
                table: "EmergencyPlanSection",
                columns: new[] { "TenantId", "EmergencyActionPlanId", "SectionType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPlanSection_TenantId_IsDeleted",
                schema: "ensa",
                table: "EmergencyPlanSection",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyTeamMember_CompanyEmployeeId",
                schema: "ensa",
                table: "EmergencyTeamMember",
                column: "CompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyTeamMember_EmergencyActionPlanId_TeamType",
                schema: "ensa",
                table: "EmergencyTeamMember",
                columns: new[] { "EmergencyActionPlanId", "TeamType" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyTeamMember_TenantId_IsDeleted",
                schema: "ensa",
                table: "EmergencyTeamMember",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExamAnswer_CompanyEmployeeId_ExamQuestionId",
                schema: "ensa",
                table: "EmployeeExamAnswer",
                columns: new[] { "CompanyEmployeeId", "ExamQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExamAnswer_EmployeeTrainingProgressId",
                schema: "ensa",
                table: "EmployeeExamAnswer",
                column: "EmployeeTrainingProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExamAnswer_ExamQuestionId",
                schema: "ensa",
                table: "EmployeeExamAnswer",
                column: "ExamQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFamilyHistory_CompanyEmployeeId",
                schema: "ensa",
                table: "EmployeeFamilyHistory",
                column: "CompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFamilyHistory_CompanyEmployeeId_Relation",
                schema: "ensa",
                table: "EmployeeFamilyHistory",
                columns: new[] { "CompanyEmployeeId", "Relation" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHealthInfo_CompanyEmployeeId",
                schema: "ensa",
                table: "EmployeeHealthInfo",
                column: "CompanyEmployeeId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImmunization_CompanyEmployeeId",
                schema: "ensa",
                table: "EmployeeImmunization",
                column: "CompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingLog_CompanyEmployeeId_CreationTime",
                schema: "ensa",
                table: "EmployeeTrainingLog",
                columns: new[] { "CompanyEmployeeId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingLog_EmployeeTrainingProgressId",
                schema: "ensa",
                table: "EmployeeTrainingLog",
                column: "EmployeeTrainingProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingLog_ExamId",
                schema: "ensa",
                table: "EmployeeTrainingLog",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingLog_Operation",
                schema: "ensa",
                table: "EmployeeTrainingLog",
                column: "Operation");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingLog_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingLog",
                column: "TrainingTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_CompanyEmployeeId_TrainingId_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                columns: new[] { "CompanyEmployeeId", "TrainingId", "TrainingTopicId" },
                unique: true,
                filter: "[TrainingTopicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_TrainingId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_TrainingPhysicianUserId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                column: "TrainingPhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_TrainingSpecialistUserId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                column: "TrainingSpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                column: "TrainingTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkHistory_CompanyEmployeeId_OrderNo",
                schema: "ensa",
                table: "EmployeeWorkHistory",
                columns: new[] { "CompanyEmployeeId", "OrderNo" });

            migrationBuilder.CreateIndex(
                name: "IX_EPrescription_PatientCompanyEmployeeId",
                schema: "ensa",
                table: "EPrescription",
                column: "PatientCompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescription_TenantId_EPrescriptionCode",
                schema: "ensa",
                table: "EPrescription",
                columns: new[] { "TenantId", "EPrescriptionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_EPrescription_TenantId_IsDeleted",
                schema: "ensa",
                table: "EPrescription",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EPrescription_TenantId_PatientNationalId",
                schema: "ensa",
                table: "EPrescription",
                columns: new[] { "TenantId", "PatientNationalId" });

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionDiagnosis_EPrescriptionId",
                schema: "ensa",
                table: "EPrescriptionDiagnosis",
                column: "EPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionDiagnosis_Icd10Id",
                schema: "ensa",
                table: "EPrescriptionDiagnosis",
                column: "Icd10Id");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionDiagnosis_TenantId_IsDeleted",
                schema: "ensa",
                table: "EPrescriptionDiagnosis",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionMedication_EPrescriptionId",
                schema: "ensa",
                table: "EPrescriptionMedication",
                column: "EPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionMedication_MedicationId",
                schema: "ensa",
                table: "EPrescriptionMedication",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionMedication_TenantId_IsDeleted",
                schema: "ensa",
                table: "EPrescriptionMedication",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionMedication_UsageDoseUnitId",
                schema: "ensa",
                table: "EPrescriptionMedication",
                column: "UsageDoseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionMedication_UsageMethodId",
                schema: "ensa",
                table: "EPrescriptionMedication",
                column: "UsageMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_EPrescriptionMedication_UsagePeriodUnitId",
                schema: "ensa",
                table: "EPrescriptionMedication",
                column: "UsagePeriodUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_CompanyId",
                schema: "ensa",
                table: "Equipment",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_ExaminationReportDocumentId",
                schema: "ensa",
                table: "Equipment",
                column: "ExaminationReportDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_NextExaminationDate",
                schema: "ensa",
                table: "Equipment",
                column: "NextExaminationDate",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_PeriodId",
                schema: "ensa",
                table: "Equipment",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_TenantId_CompanyId_EquipmentType",
                schema: "ensa",
                table: "Equipment",
                columns: new[] { "TenantId", "CompanyId", "EquipmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_TenantId_IsDeleted",
                schema: "ensa",
                table: "Equipment",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_ActivityId",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_CompanyId",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_DocumentId",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_EquipmentDocumentTypeId",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "EquipmentDocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_EquipmentId",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_TenantId_IsDeleted",
                schema: "ensa",
                table: "EquipmentDocument",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_ValidityDate",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "ValidityDate");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocument_WorkPlanLineId",
                schema: "ensa",
                table: "EquipmentDocument",
                column: "WorkPlanLineId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocumentType_TenantId_IsActive_SortOrder",
                schema: "ensa",
                table: "EquipmentDocumentType",
                columns: new[] { "TenantId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocumentType_TenantId_IsDeleted",
                schema: "ensa",
                table: "EquipmentDocumentType",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ESignatureLicense_IsActive_ValidityDate",
                schema: "ensa",
                table: "ESignatureLicense",
                columns: new[] { "IsActive", "ValidityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Exam_TenantId_IsActive",
                schema: "ensa",
                table: "Exam",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswer_ExamQuestionId",
                schema: "ensa",
                table: "ExamAnswer",
                column: "ExamQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestion_ExamId",
                schema: "ensa",
                table: "ExamQuestion",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_ParentExpenseCategoryId",
                schema: "ensa",
                table: "ExpenseCategory",
                column: "ParentExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_TenantId_IsActive",
                schema: "ensa",
                table: "ExpenseCategory",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationLine_DeadlineDate",
                schema: "ensa",
                table: "FieldObservationLine",
                column: "DeadlineDate");

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationLine_DocumentId",
                schema: "ensa",
                table: "FieldObservationLine",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationLine_FieldObservationReportId",
                schema: "ensa",
                table: "FieldObservationLine",
                column: "FieldObservationReportId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationLine_OwnerCompanyEmployeeId",
                schema: "ensa",
                table: "FieldObservationLine",
                column: "OwnerCompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationLine_TenantId_IsDeleted",
                schema: "ensa",
                table: "FieldObservationLine",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationReport_CompanyId",
                schema: "ensa",
                table: "FieldObservationReport",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationReport_DepartmentId",
                schema: "ensa",
                table: "FieldObservationReport",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationReport_TenantId_CompanyId_Date",
                schema: "ensa",
                table: "FieldObservationReport",
                columns: new[] { "TenantId", "CompanyId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldObservationReport_TenantId_IsDeleted",
                schema: "ensa",
                table: "FieldObservationReport",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Form_CategoryId_IsActive",
                schema: "ensa",
                table: "Form",
                columns: new[] { "CategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Form_DocumentId",
                schema: "ensa",
                table: "Form",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FormCategory_TenantId_CategoryName",
                schema: "ensa",
                table: "FormCategory",
                columns: new[] { "TenantId", "CategoryName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Hazard_HazardCategoryId_IsActive",
                schema: "ensa",
                table: "Hazard",
                columns: new[] { "HazardCategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Hazard_TenantId",
                schema: "ensa",
                table: "Hazard",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HazardCategory_TenantId",
                schema: "ensa",
                table: "HazardCategory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HazardCategory_TenantId_IsHazardSource_SortOrder",
                schema: "ensa",
                table: "HazardCategory",
                columns: new[] { "TenantId", "IsHazardSource", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_IbysChildReferenceValue_Code_ParentReferenceCode",
                schema: "ensa",
                table: "IbysChildReferenceValue",
                columns: new[] { "Code", "ParentReferenceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysChildReferenceValue_IbysRootReferenceValueId",
                schema: "ensa",
                table: "IbysChildReferenceValue",
                column: "IbysRootReferenceValueId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysChildReferenceValue_ParentReferenceCode",
                schema: "ensa",
                table: "IbysChildReferenceValue",
                column: "ParentReferenceCode");

            migrationBuilder.CreateIndex(
                name: "IX_IbysEquipmentTopCategory_ParentCategoryName",
                schema: "ensa",
                table: "IbysEquipmentTopCategory",
                column: "ParentCategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysIsco08OccupationCode_Code",
                schema: "ensa",
                table: "IbysIsco08OccupationCode",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysQuery_CompanyEmployeeId",
                schema: "ensa",
                table: "IbysQuery",
                column: "CompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysQuery_CompanyId",
                schema: "ensa",
                table: "IbysQuery",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysQuery_GroupId",
                schema: "ensa",
                table: "IbysQuery",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysQuery_QueryNo",
                schema: "ensa",
                table: "IbysQuery",
                column: "QueryNo");

            migrationBuilder.CreateIndex(
                name: "IX_IbysQuery_TenantId_IsDeleted",
                schema: "ensa",
                table: "IbysQuery",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_IbysQuery_TenantId_QueryType_Status",
                schema: "ensa",
                table: "IbysQuery",
                columns: new[] { "TenantId", "QueryType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IbysRootReferenceValue_Code",
                schema: "ensa",
                table: "IbysRootReferenceValue",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysServedWorkplace_ApproverUserId",
                schema: "ensa",
                table: "IbysServedWorkplace",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysServedWorkplace_CompanyId",
                schema: "ensa",
                table: "IbysServedWorkplace",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysServedWorkplace_IbysNotificationNo",
                schema: "ensa",
                table: "IbysServedWorkplace",
                column: "IbysNotificationNo");

            migrationBuilder.CreateIndex(
                name: "IX_IbysServedWorkplace_TenantId_CompanyId",
                schema: "ensa",
                table: "IbysServedWorkplace",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_IbysServedWorkplace_TenantId_IsDeleted",
                schema: "ensa",
                table: "IbysServedWorkplace",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkArrangement_Code",
                schema: "ensa",
                table: "IbysWorkArrangement",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkArrangement_Type",
                schema: "ensa",
                table: "IbysWorkArrangement",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkEnvironment_EnvironmentCode",
                schema: "ensa",
                table: "IbysWorkEnvironment",
                column: "EnvironmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkEnvironment_IbysWorkEnvironmentTypeId",
                schema: "ensa",
                table: "IbysWorkEnvironment",
                column: "IbysWorkEnvironmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkEnvironment_TypeCode",
                schema: "ensa",
                table: "IbysWorkEnvironment",
                column: "TypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkEnvironmentType_TypeCode",
                schema: "ensa",
                table: "IbysWorkEnvironmentType",
                column: "TypeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkEquipment_Code",
                schema: "ensa",
                table: "IbysWorkEquipment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IbysWorkEquipment_ParentCategoryId",
                schema: "ensa",
                table: "IbysWorkEquipment",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Icd10_Code",
                schema: "ensa",
                table: "Icd10",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Icd10_ParentCode",
                schema: "ensa",
                table: "Icd10",
                column: "ParentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Icd10_ParentIcd10Id",
                schema: "ensa",
                table: "Icd10",
                column: "ParentIcd10Id");

            migrationBuilder.CreateIndex(
                name: "IX_Icon_LibraryCode_IconCssClass",
                schema: "ensa",
                table: "Icon",
                columns: new[] { "LibraryCode", "IconCssClass" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IconLibrary_Code",
                schema: "ensa",
                table: "IconLibrary",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IconLibrary_IsActive_SortOrder",
                schema: "ensa",
                table: "IconLibrary",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_DeadlineDate",
                schema: "ensa",
                table: "IdentifiedHazard",
                column: "DeadlineDate",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_DocumentId",
                schema: "ensa",
                table: "IdentifiedHazard",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_HazardCategoryId",
                schema: "ensa",
                table: "IdentifiedHazard",
                column: "HazardCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_HazardId",
                schema: "ensa",
                table: "IdentifiedHazard",
                column: "HazardId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_RiskAssessmentReportId",
                schema: "ensa",
                table: "IdentifiedHazard",
                column: "RiskAssessmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_SourceType_SourceId",
                schema: "ensa",
                table: "IdentifiedHazard",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_TenantId_IsDeleted",
                schema: "ensa",
                table: "IdentifiedHazard",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentifiedHazard_TenantId_RiskScore",
                schema: "ensa",
                table: "IdentifiedHazard",
                columns: new[] { "TenantId", "RiskScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_CompanyId",
                schema: "ensa",
                table: "Incident",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_DepartmentId",
                schema: "ensa",
                table: "Incident",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_DocumentId",
                schema: "ensa",
                table: "Incident",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_IncidentType_SsiNotificationDate",
                schema: "ensa",
                table: "Incident",
                columns: new[] { "IncidentType", "SsiNotificationDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_TenantId_CompanyId_IncidentDate",
                schema: "ensa",
                table: "Incident",
                columns: new[] { "TenantId", "CompanyId", "IncidentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_TenantId_IsDeleted",
                schema: "ensa",
                table: "Incident",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_UnitSupervisorId",
                schema: "ensa",
                table: "Incident",
                column: "UnitSupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPerson_CompanyEmployeeId",
                schema: "ensa",
                table: "IncidentPerson",
                column: "CompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPerson_IncidentId_PersonType",
                schema: "ensa",
                table: "IncidentPerson",
                columns: new[] { "IncidentId", "PersonType" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPerson_TenantId_IsDeleted",
                schema: "ensa",
                table: "IncidentPerson",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OfficeId",
                schema: "ensa",
                table: "Invoice",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_TenantId_CompanyId_InvoiceDate",
                schema: "ensa",
                table: "Invoice",
                columns: new[] { "TenantId", "CompanyId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_TenantId_InvoiceDate",
                schema: "ensa",
                table: "Invoice",
                columns: new[] { "TenantId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_TenantId_InvoiceNo",
                schema: "ensa",
                table: "Invoice",
                columns: new[] { "TenantId", "InvoiceNo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLine_CompanyId",
                schema: "ensa",
                table: "InvoiceLine",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLine_InvoiceId_OrderNo",
                schema: "ensa",
                table: "InvoiceLine",
                columns: new[] { "InvoiceId", "OrderNo" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLine_ServiceItemId",
                schema: "ensa",
                table: "InvoiceLine",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTemplate_TenantId_ModuleType_OnValue",
                schema: "ensa",
                table: "InvoiceTemplate",
                columns: new[] { "TenantId", "ModuleType", "OnValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Log_TenantId_CreationTime",
                schema: "ensa",
                table: "Log",
                columns: new[] { "TenantId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Log_UserId",
                schema: "ensa",
                table: "Log",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Mail_MailStatus_AttemptCount",
                schema: "ensa",
                table: "Mail",
                columns: new[] { "MailStatus", "AttemptCount" },
                filter: "[MailStatus] IN (0, 1, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_Mail_TenantId_SubmissionDate",
                schema: "ensa",
                table: "Mail",
                columns: new[] { "TenantId", "SubmissionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MailAttachment_DocumentId",
                schema: "ensa",
                table: "MailAttachment",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_MailAttachment_MailId_OrderNo",
                schema: "ensa",
                table: "MailAttachment",
                columns: new[] { "MailId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamComplaint_MedicalExaminationFormId",
                schema: "ensa",
                table: "MedicalExamComplaint",
                column: "MedicalExaminationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamComplaint_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExamComplaint",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamComplaint_TenantId_MedicalExaminationFormId_ComplaintType",
                schema: "ensa",
                table: "MedicalExamComplaint",
                columns: new[] { "TenantId", "MedicalExaminationFormId", "ComplaintType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamHabit_MedicalExaminationFormId",
                schema: "ensa",
                table: "MedicalExamHabit",
                column: "MedicalExaminationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamHabit_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExamHabit",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamHabit_TenantId_MedicalExaminationFormId_HabitType",
                schema: "ensa",
                table: "MedicalExamHabit",
                columns: new[] { "TenantId", "MedicalExaminationFormId", "HabitType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamImmunization_MedicalExaminationFormId",
                schema: "ensa",
                table: "MedicalExamImmunization",
                column: "MedicalExaminationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamImmunization_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExamImmunization",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamImmunization_TenantId_MedicalExaminationFormId_ImmunizationType",
                schema: "ensa",
                table: "MedicalExamImmunization",
                columns: new[] { "TenantId", "MedicalExaminationFormId", "ImmunizationType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_DocumentId",
                schema: "ensa",
                table: "MedicalExaminationForm",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_IbysQueryId",
                schema: "ensa",
                table: "MedicalExaminationForm",
                column: "IbysQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_IbysStatus",
                schema: "ensa",
                table: "MedicalExaminationForm",
                column: "IbysStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_PhysicianUserId",
                schema: "ensa",
                table: "MedicalExaminationForm",
                column: "PhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_TenantId_CompanyEmployeeId_ExaminationDate",
                schema: "ensa",
                table: "MedicalExaminationForm",
                columns: new[] { "TenantId", "CompanyEmployeeId", "ExaminationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_TenantId_CompanyId_ValidityDate",
                schema: "ensa",
                table: "MedicalExaminationForm",
                columns: new[] { "TenantId", "CompanyId", "ValidityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExaminationForm_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExaminationForm",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamLabTest_MedicalExaminationFormId",
                schema: "ensa",
                table: "MedicalExamLabTest",
                column: "MedicalExaminationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamLabTest_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExamLabTest",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamLabTest_TenantId_MedicalExaminationFormId_LabTestType",
                schema: "ensa",
                table: "MedicalExamLabTest",
                columns: new[] { "TenantId", "MedicalExaminationFormId", "LabTestType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamPhysicalFinding_MedicalExaminationFormId",
                schema: "ensa",
                table: "MedicalExamPhysicalFinding",
                column: "MedicalExaminationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamPhysicalFinding_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExamPhysicalFinding",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamPhysicalFinding_TenantId_MedicalExaminationFormId_System",
                schema: "ensa",
                table: "MedicalExamPhysicalFinding",
                columns: new[] { "TenantId", "MedicalExaminationFormId", "System" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamWorkCondition_MedicalExaminationFormId",
                schema: "ensa",
                table: "MedicalExamWorkCondition",
                column: "MedicalExaminationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamWorkCondition_TenantId_IsDeleted",
                schema: "ensa",
                table: "MedicalExamWorkCondition",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalExamWorkCondition_TenantId_MedicalExaminationFormId_ConditionType",
                schema: "ensa",
                table: "MedicalExamWorkCondition",
                columns: new[] { "TenantId", "MedicalExaminationFormId", "ConditionType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_AtcCode",
                schema: "ensa",
                table: "Medication",
                column: "AtcCode");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_Barcode",
                schema: "ensa",
                table: "Medication",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_MedicationName",
                schema: "ensa",
                table: "Medication",
                column: "MedicationName");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDoseUnit_Code",
                schema: "ensa",
                table: "MedicationDoseUnit",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationFrequencyUnit_Code",
                schema: "ensa",
                table: "MedicationFrequencyUnit",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationRoute_Code",
                schema: "ensa",
                table: "MedicationRoute",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_MenuTypeCode_UserTypeCode_OrganizationTypeId_SubscriptionPlanId",
                schema: "ensa",
                table: "Menu",
                columns: new[] { "MenuTypeCode", "UserTypeCode", "OrganizationTypeId", "SubscriptionPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_Menu_OrganizationTypeId",
                schema: "ensa",
                table: "Menu",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_SubscriptionPlanId",
                schema: "ensa",
                table: "Menu",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuElement_MenuId_ParentMenuElementId_SortOrder",
                schema: "ensa",
                table: "MenuElement",
                columns: new[] { "MenuId", "ParentMenuElementId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuElement_ParentMenuElementId",
                schema: "ensa",
                table: "MenuElement",
                column: "ParentMenuElementId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_Code",
                schema: "ensa",
                table: "MenuItem",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_ModuleId",
                schema: "ensa",
                table: "MenuItem",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_ProjectCode_IsActive_SortOrder",
                schema: "ensa",
                table: "MenuItem",
                columns: new[] { "ProjectCode", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuNode_MenuId_ParentMenuNodeId_SortOrder",
                schema: "ensa",
                table: "MenuNode",
                columns: new[] { "MenuId", "ParentMenuNodeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuNode_MenuItemId",
                schema: "ensa",
                table: "MenuNode",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuNode_ParentMenuNodeId",
                schema: "ensa",
                table: "MenuNode",
                column: "ParentMenuNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuPage_MenuCode",
                schema: "ensa",
                table: "MenuPage",
                column: "MenuCode");

            migrationBuilder.CreateIndex(
                name: "IX_MenuPage_PageUrl",
                schema: "ensa",
                table: "MenuPage",
                column: "PageUrl");

            migrationBuilder.CreateIndex(
                name: "IX_MenuPage_ProjectCode_SettlementCode_IsActive",
                schema: "ensa",
                table: "MenuPage",
                columns: new[] { "ProjectCode", "SettlementCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuType_Code",
                schema: "ensa",
                table: "MenuType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuType_ProjectCode_SortOrder",
                schema: "ensa",
                table: "MenuType",
                columns: new[] { "ProjectCode", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Message_CompanyId",
                schema: "ensa",
                table: "Message",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_RecipientId_IsRead",
                schema: "ensa",
                table: "Message",
                columns: new[] { "RecipientId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderId",
                schema: "ensa",
                table: "Message",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplate_Code",
                schema: "ensa",
                table: "MessageTemplate",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplate_MessageTemplateTypeId",
                schema: "ensa",
                table: "MessageTemplate",
                column: "MessageTemplateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplateType_Code",
                schema: "ensa",
                table: "MessageTemplateType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Module_ParentModuleId_IsActive_SortOrder",
                schema: "ensa",
                table: "Module",
                columns: new[] { "ParentModuleId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleArchive_TenantId_ModuleCode",
                schema: "ensa",
                table: "ModuleArchive",
                columns: new[] { "TenantId", "ModuleCode" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleArchiveItem_DocumentId",
                schema: "ensa",
                table: "ModuleArchiveItem",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleArchiveItem_ModuleArchiveId_OfficeId",
                schema: "ensa",
                table: "ModuleArchiveItem",
                columns: new[] { "ModuleArchiveId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleArchiveItem_OfficeId",
                schema: "ensa",
                table: "ModuleArchiveItem",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Neighborhood_DistrictId",
                schema: "ensa",
                table: "Neighborhood",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Neighborhood_DistrictId_NeighborhoodName",
                schema: "ensa",
                table: "Neighborhood",
                columns: new[] { "DistrictId", "NeighborhoodName" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriber_Email",
                schema: "ensa",
                table: "NewsletterSubscriber",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequence_ScopeId",
                schema: "ensa",
                table: "NumberSequence",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequence_TenantId_ScopeId_Type",
                schema: "ensa",
                table: "NumberSequence",
                columns: new[] { "TenantId", "ScopeId", "Type" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OccupationCode_NaceCode",
                schema: "ensa",
                table: "OccupationCode",
                column: "NaceCode");

            migrationBuilder.CreateIndex(
                name: "IX_Office_CityId",
                schema: "ensa",
                table: "Office",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Office_CompanyId",
                schema: "ensa",
                table: "Office",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Office_DistrictId",
                schema: "ensa",
                table: "Office",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Office_TenantId_HeadquarterOffice",
                schema: "ensa",
                table: "Office",
                column: "TenantId",
                unique: true,
                filter: "[HeadquarterOffice] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Office_TenantId_IsDeleted",
                schema: "ensa",
                table: "Office",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficeExpense_OfficeId",
                schema: "ensa",
                table: "OfficeExpense",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeExpense_TenantId_ExpenseDate",
                schema: "ensa",
                table: "OfficeExpense",
                columns: new[] { "TenantId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OhsReport_ModuleArchiveDetailId",
                schema: "ensa",
                table: "OhsReport",
                column: "ModuleArchiveDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OhsReport_NationalId",
                schema: "ensa",
                table: "OhsReport",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_OhsReport_TenantId_OfficeId",
                schema: "ensa",
                table: "OhsReport",
                columns: new[] { "TenantId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_OhsReportHazardClassBreakdown_OhsReportId_HazardClass",
                schema: "ensa",
                table: "OhsReportHazardClassBreakdown",
                columns: new[] { "OhsReportId", "HazardClass" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictApplications_ClientId",
                schema: "ensa",
                table: "OpenIddictApplications",
                column: "ClientId",
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type",
                schema: "ensa",
                table: "OpenIddictAuthorizations",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictScopes_Name",
                schema: "ensa",
                table: "OpenIddictScopes",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type",
                schema: "ensa",
                table: "OpenIddictTokens",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_AuthorizationId",
                schema: "ensa",
                table: "OpenIddictTokens",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ReferenceId",
                schema: "ensa",
                table: "OpenIddictTokens",
                column: "ReferenceId",
                unique: true,
                filter: "[ReferenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_CityId",
                schema: "ensa",
                table: "Organization",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Code",
                schema: "ensa",
                table: "Organization",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_DistrictId",
                schema: "ensa",
                table: "Organization",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_LogoDocumentId",
                schema: "ensa",
                table: "Organization",
                column: "LogoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_OrganizationTypeId",
                schema: "ensa",
                table: "Organization",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_SubscriptionPlanId",
                schema: "ensa",
                table: "Organization",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_AssignmentLogId",
                schema: "ensa",
                table: "OrganizationContract",
                column: "AssignmentLogId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_ContractStatus_IsActive",
                schema: "ensa",
                table: "OrganizationContract",
                columns: new[] { "ContractStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_OrganizationId",
                schema: "ensa",
                table: "OrganizationContract",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_OrganizationTypeId",
                schema: "ensa",
                table: "OrganizationContract",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_ReferenceCompanyId",
                schema: "ensa",
                table: "OrganizationContract",
                column: "ReferenceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_SalesRepId",
                schema: "ensa",
                table: "OrganizationContract",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationContract_SubscriptionPlanId",
                schema: "ensa",
                table: "OrganizationContract",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationType_Code",
                schema: "ensa",
                table: "OrganizationType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationType_IsActive_SortOrder",
                schema: "ensa",
                table: "OrganizationType",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationTypePermission_OrganizationTypeId_PermissionId",
                schema: "ensa",
                table: "OrganizationTypePermission",
                columns: new[] { "OrganizationTypeId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationTypePermission_PermissionId",
                schema: "ensa",
                table: "OrganizationTypePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameter_TenantId_Code",
                schema: "ensa",
                table: "Parameter",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_BankId",
                schema: "ensa",
                table: "Payment",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ReceiptDocumentId",
                schema: "ensa",
                table: "Payment",
                column: "ReceiptDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TenantId_Status_NotificationDate",
                schema: "ensa",
                table: "Payment",
                columns: new[] { "TenantId", "Status", "NotificationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_Name",
                schema: "ensa",
                table: "PaymentMethod",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Penalty_IsActive",
                schema: "ensa",
                table: "Penalty",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Penalty_TreeNodeCode",
                schema: "ensa",
                table: "Penalty",
                column: "TreeNodeCode");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyAmount_PenaltyId_HazardClass_EmployeeCountRange_ValidityYear",
                schema: "ensa",
                table: "PenaltyAmount",
                columns: new[] { "PenaltyId", "HazardClass", "EmployeeCountRange", "ValidityYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyAmount_ValidityYear",
                schema: "ensa",
                table: "PenaltyAmount",
                column: "ValidityYear");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurvey_CityId",
                schema: "ensa",
                table: "PenaltySurvey",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurvey_DistrictId",
                schema: "ensa",
                table: "PenaltySurvey",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurvey_LogoDocumentId",
                schema: "ensa",
                table: "PenaltySurvey",
                column: "LogoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurvey_NeighborhoodId",
                schema: "ensa",
                table: "PenaltySurvey",
                column: "NeighborhoodId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurvey_TenantId_CompanyTitle",
                schema: "ensa",
                table: "PenaltySurvey",
                columns: new[] { "TenantId", "CompanyTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurveyLine_PenaltyId",
                schema: "ensa",
                table: "PenaltySurveyLine",
                column: "PenaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltySurveyLine_PenaltySurveyId",
                schema: "ensa",
                table: "PenaltySurveyLine",
                column: "PenaltySurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Period_PeriodUnit_PeriodValue",
                schema: "ensa",
                table: "Period",
                columns: new[] { "PeriodUnit", "PeriodValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Permission_ParentPermissionId",
                schema: "ensa",
                table: "Permission",
                column: "ParentPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_PermissionTarget",
                schema: "ensa",
                table: "Permission",
                column: "PermissionTarget",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permission_PermissionType_SortOrder",
                schema: "ensa",
                table: "Permission",
                columns: new[] { "PermissionType", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRestriction_PermissionId",
                schema: "ensa",
                table: "PermissionRestriction",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRestriction_TenantId_PermissionId_UserTypeId",
                schema: "ensa",
                table: "PermissionRestriction",
                columns: new[] { "TenantId", "PermissionId", "UserTypeId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRestriction_UserTypeId",
                schema: "ensa",
                table: "PermissionRestriction",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionScope_LinkType_LinkTargetCode",
                schema: "ensa",
                table: "PermissionScope",
                columns: new[] { "LinkType", "LinkTargetCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionScope_LinkType_LinkTargetId",
                schema: "ensa",
                table: "PermissionScope",
                columns: new[] { "LinkType", "LinkTargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionScope_PermissionId",
                schema: "ensa",
                table: "PermissionScope",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_CityId",
                schema: "ensa",
                table: "Person",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_DistrictId",
                schema: "ensa",
                table: "Person",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_NeighborhoodId",
                schema: "ensa",
                table: "Person",
                column: "NeighborhoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_TenantId_NationalId",
                schema: "ensa",
                table: "Person",
                columns: new[] { "TenantId", "NationalId" },
                unique: true,
                filter: "[NationalId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectOrganization_AssignmentLogId",
                schema: "ensa",
                table: "ProspectOrganization",
                column: "AssignmentLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectOrganization_ContractStatus_IsActive",
                schema: "ensa",
                table: "ProspectOrganization",
                columns: new[] { "ContractStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProspectOrganization_OrganizationId",
                schema: "ensa",
                table: "ProspectOrganization",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectOrganization_ReferenceCompanyId",
                schema: "ensa",
                table: "ProspectOrganization",
                column: "ReferenceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectOrganization_SalesRepId",
                schema: "ensa",
                table: "ProspectOrganization",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectOrganization_SubscriptionPlanId",
                schema: "ensa",
                table: "ProspectOrganization",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentControlMeasure_RiskAssessmentReportId",
                schema: "ensa",
                table: "RiskAssessmentControlMeasure",
                column: "RiskAssessmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentControlMeasure_TenantId_RiskAssessmentReportId_Measure",
                schema: "ensa",
                table: "RiskAssessmentControlMeasure",
                columns: new[] { "TenantId", "RiskAssessmentReportId", "Measure" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentExposedGroup_RiskAssessmentReportId",
                schema: "ensa",
                table: "RiskAssessmentExposedGroup",
                column: "RiskAssessmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentExposedGroup_TenantId_RiskAssessmentReportId_Group",
                schema: "ensa",
                table: "RiskAssessmentExposedGroup",
                columns: new[] { "TenantId", "RiskAssessmentReportId", "Group" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentHistoryRecord_RiskAssessmentReportId_RecordType_Date",
                schema: "ensa",
                table: "RiskAssessmentHistoryRecord",
                columns: new[] { "RiskAssessmentReportId", "RecordType", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentHistoryRecord_TenantId_IsDeleted",
                schema: "ensa",
                table: "RiskAssessmentHistoryRecord",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentImprovementAction_RiskAssessmentReportId",
                schema: "ensa",
                table: "RiskAssessmentImprovementAction",
                column: "RiskAssessmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentImprovementAction_TenantId_RiskAssessmentReportId_Recommendation",
                schema: "ensa",
                table: "RiskAssessmentImprovementAction",
                columns: new[] { "TenantId", "RiskAssessmentReportId", "Recommendation" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentParticipant_CompanyEmployeeId",
                schema: "ensa",
                table: "RiskAssessmentParticipant",
                column: "CompanyEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentParticipant_RiskAssessmentReportId_ParticipantType",
                schema: "ensa",
                table: "RiskAssessmentParticipant",
                columns: new[] { "RiskAssessmentReportId", "ParticipantType" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentProtectedGroup_RiskAssessmentReportId",
                schema: "ensa",
                table: "RiskAssessmentProtectedGroup",
                column: "RiskAssessmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentProtectedGroup_TenantId_RiskAssessmentReportId_Group",
                schema: "ensa",
                table: "RiskAssessmentProtectedGroup",
                columns: new[] { "TenantId", "RiskAssessmentReportId", "Group" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentReport_CompanyId",
                schema: "ensa",
                table: "RiskAssessmentReport",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentReport_PhysicianUserId",
                schema: "ensa",
                table: "RiskAssessmentReport",
                column: "PhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentReport_SpecialistUserId",
                schema: "ensa",
                table: "RiskAssessmentReport",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentReport_TenantId_ApprovalStatus",
                schema: "ensa",
                table: "RiskAssessmentReport",
                columns: new[] { "TenantId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentReport_TenantId_CompanyId_ValidityDate",
                schema: "ensa",
                table: "RiskAssessmentReport",
                columns: new[] { "TenantId", "CompanyId", "ValidityDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentReport_TenantId_IsDeleted",
                schema: "ensa",
                table: "RiskAssessmentReport",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId",
                schema: "ensa",
                table: "Role",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId_NormalizedName",
                schema: "ensa",
                table: "Role",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaim_RoleId",
                schema: "ensa",
                table: "RoleClaim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOrigin_CityId",
                schema: "ensa",
                table: "RouteOrigin",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOrigin_DistrictId",
                schema: "ensa",
                table: "RouteOrigin",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOriginDistance_CompanyId",
                schema: "ensa",
                table: "RouteOriginDistance",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOriginDistance_OriginId_CompanyId",
                schema: "ensa",
                table: "RouteOriginDistance",
                columns: new[] { "OriginId", "CompanyId" },
                unique: true,
                filter: "[OriginId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRep_SalesRepType_IsActive",
                schema: "ensa",
                table: "SalesRep",
                columns: new[] { "SalesRepType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRep_UserId",
                schema: "ensa",
                table: "SalesRep",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepScreenField_ScreenType_FieldName",
                schema: "ensa",
                table: "SalesRepScreenField",
                columns: new[] { "ScreenType", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepScreenField_ScreenType_SortOrder",
                schema: "ensa",
                table: "SalesRepScreenField",
                columns: new[] { "ScreenType", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItem_TenantId_CardType_IsActive",
                schema: "ensa",
                table: "ServiceItem",
                columns: new[] { "TenantId", "CardType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItem_TenantId_Code",
                schema: "ensa",
                table: "ServiceItem",
                columns: new[] { "TenantId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotReport_OfficeId",
                schema: "ensa",
                table: "SnapshotReport",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotReport_ReportType_OfficeId_ReportDate",
                schema: "ensa",
                table: "SnapshotReport",
                columns: new[] { "ReportType", "OfficeId", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffCostBaseline_OfficeId",
                schema: "ensa",
                table: "StaffCostBaseline",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffCostBaseline_TenantId_IsDeleted",
                schema: "ensa",
                table: "StaffCostBaseline",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffCostBaseline_UserId",
                schema: "ensa",
                table: "StaffCostBaseline",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StandardDocument_StandardDocumentCode",
                schema: "ensa",
                table: "StandardDocument",
                column: "StandardDocumentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlan_Code",
                schema: "ensa",
                table: "SubscriptionPlan",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlan_IsActive_SortOrder",
                schema: "ensa",
                table: "SubscriptionPlan",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPermission_PermissionId",
                schema: "ensa",
                table: "SubscriptionPlanPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPermission_SubscriptionPlanId_PermissionId",
                schema: "ensa",
                table: "SubscriptionPlanPermission",
                columns: new[] { "SubscriptionPlanId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_ClosedByUserId",
                schema: "ensa",
                table: "SupportTicket",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_OpenedByUserId",
                schema: "ensa",
                table: "SupportTicket",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_ResponderUserId",
                schema: "ensa",
                table: "SupportTicket",
                column: "ResponderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_TenantId_Status",
                schema: "ensa",
                table: "SupportTicket",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessage_FieldUserId_IsRead",
                schema: "ensa",
                table: "SupportTicketMessage",
                columns: new[] { "FieldUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessage_SenderUserId",
                schema: "ensa",
                table: "SupportTicketMessage",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessage_SupportTicketId",
                schema: "ensa",
                table: "SupportTicketMessage",
                column: "SupportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_SettingName",
                schema: "ensa",
                table: "SystemSetting",
                column: "SettingName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Training_TenantId_IncludedInDefaultPlan",
                schema: "ensa",
                table: "Training",
                columns: new[] { "TenantId", "IncludedInDefaultPlan" });

            migrationBuilder.CreateIndex(
                name: "IX_Training_TenantId_IsActive",
                schema: "ensa",
                table: "Training",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Training_TenantId_TrainingCode",
                schema: "ensa",
                table: "Training",
                columns: new[] { "TenantId", "TrainingCode" },
                unique: true,
                filter: "[TrainingCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Training_TrainingGroupId",
                schema: "ensa",
                table: "Training",
                column: "TrainingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDuration_TrainingId_HazardClass",
                schema: "ensa",
                table: "TrainingDuration",
                columns: new[] { "TrainingId", "HazardClass" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingExam_ExamId",
                schema: "ensa",
                table: "TrainingExam",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingExam_TrainingId_ExamId",
                schema: "ensa",
                table: "TrainingExam",
                columns: new[] { "TrainingId", "ExamId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingGroup_OrderNo",
                schema: "ensa",
                table: "TrainingGroup",
                column: "OrderNo");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingGroup_TrainingGroupCode",
                schema: "ensa",
                table: "TrainingGroup",
                column: "TrainingGroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlan_ApproverUserId",
                schema: "ensa",
                table: "TrainingPlan",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlan_CompanyId",
                schema: "ensa",
                table: "TrainingPlan",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlan_PhysicianUserId",
                schema: "ensa",
                table: "TrainingPlan",
                column: "PhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlan_SpecialistUserId",
                schema: "ensa",
                table: "TrainingPlan",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlan_TenantId_CompanyId_StartDate",
                schema: "ensa",
                table: "TrainingPlan",
                columns: new[] { "TenantId", "CompanyId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlan_TenantId_Transferred",
                schema: "ensa",
                table: "TrainingPlan",
                columns: new[] { "TenantId", "Transferred" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_ApproverUserId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_CompanyId_TrainingId_Status",
                schema: "ensa",
                table: "TrainingPlanLine",
                columns: new[] { "CompanyId", "TrainingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_DocumentId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_ForApprovalSenderUserId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "ForApprovalSenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_IbysQueryId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "IbysQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_IbysStatus",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "IbysStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_InstructorUserId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_PreviousLineId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "PreviousLineId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_TrainingId",
                schema: "ensa",
                table: "TrainingPlanLine",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanLine_TrainingPlanId_Year_Month",
                schema: "ensa",
                table: "TrainingPlanLine",
                columns: new[] { "TrainingPlanId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTopic_TrainingId_TopicOrder",
                schema: "ensa",
                table: "TrainingTopic",
                columns: new[] { "TrainingId", "TopicOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTopicDuration_TrainingTopicId_HazardClass",
                schema: "ensa",
                table: "TrainingTopicDuration",
                columns: new[] { "TrainingTopicId", "HazardClass" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tree_TreeCode",
                schema: "ensa",
                table: "Tree",
                column: "TreeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreeNode_ParentTreeNodeId",
                schema: "ensa",
                table: "TreeNode",
                column: "ParentTreeNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TreeNode_TreeCode_TreeNodeCode",
                schema: "ensa",
                table: "TreeNode",
                columns: new[] { "TreeCode", "TreeNodeCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TreeNode_TreeId",
                schema: "ensa",
                table: "TreeNode",
                column: "TreeId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "ensa",
                table: "User",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_User_CityId",
                schema: "ensa",
                table: "User",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_User_CompanyId",
                schema: "ensa",
                table: "User",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_User_DistrictId",
                schema: "ensa",
                table: "User",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_User_OfficeId",
                schema: "ensa",
                table: "User",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_User_PermissionGroupId",
                schema: "ensa",
                table: "User",
                column: "PermissionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_User_PhotoDocumentId",
                schema: "ensa",
                table: "User",
                column: "PhotoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_IsDeleted",
                schema: "ensa",
                table: "User",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_NationalId",
                schema: "ensa",
                table: "User",
                columns: new[] { "TenantId", "NationalId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [NationalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_NormalizedEmail",
                schema: "ensa",
                table: "User",
                columns: new[] { "TenantId", "NormalizedEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_NormalizedUserName",
                schema: "ensa",
                table: "User",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaim_UserId",
                schema: "ensa",
                table: "UserClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogin_UserId",
                schema: "ensa",
                table: "UserLogin",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMenuOverride_MenuItemId",
                schema: "ensa",
                table: "UserMenuOverride",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMenuOverride_TenantId_UserId_MenuItemId",
                schema: "ensa",
                table: "UserMenuOverride",
                columns: new[] { "TenantId", "UserId", "MenuItemId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMenuOverride_UserId",
                schema: "ensa",
                table: "UserMenuOverride",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOffice_OfficeId",
                schema: "ensa",
                table: "UserOffice",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOffice_TenantId_UserId_OfficeId",
                schema: "ensa",
                table: "UserOffice",
                columns: new[] { "TenantId", "UserId", "OfficeId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserOffice_UserId",
                schema: "ensa",
                table: "UserOffice",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_PermissionId",
                schema: "ensa",
                table: "UserPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_TenantId_UserId_PermissionId",
                schema: "ensa",
                table: "UserPermission",
                columns: new[] { "TenantId", "UserId", "PermissionId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_UserId",
                schema: "ensa",
                table: "UserPermission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                schema: "ensa",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserType_Code",
                schema: "ensa",
                table: "UserType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserType_IsActive_SortOrder",
                schema: "ensa",
                table: "UserType",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UserType_StaffRole",
                schema: "ensa",
                table: "UserType",
                column: "StaffRole");

            migrationBuilder.CreateIndex(
                name: "IX_UserTypePermission_PermissionId",
                schema: "ensa",
                table: "UserTypePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTypePermission_UserTypeId_PermissionId",
                schema: "ensa",
                table: "UserTypePermission",
                columns: new[] { "UserTypeId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visit_TenantId_CompanyId_VisitDate",
                schema: "ensa",
                table: "Visit",
                columns: new[] { "TenantId", "CompanyId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Visit_UserId_Start_End",
                schema: "ensa",
                table: "Visit",
                columns: new[] { "UserId", "Start", "End" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkplaceDepartment_CompanyId_DepartmentName",
                schema: "ensa",
                table: "WorkplaceDepartment",
                columns: new[] { "CompanyId", "DepartmentName" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_ApproverUserId",
                schema: "ensa",
                table: "WorkPlan",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_CompanyId_StartDate",
                schema: "ensa",
                table: "WorkPlan",
                columns: new[] { "CompanyId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_ControlItemListId",
                schema: "ensa",
                table: "WorkPlan",
                column: "ControlItemListId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_PhysicianUserId",
                schema: "ensa",
                table: "WorkPlan",
                column: "PhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_PreviousPlanId",
                schema: "ensa",
                table: "WorkPlan",
                column: "PreviousPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_SpecialistUserId",
                schema: "ensa",
                table: "WorkPlan",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlan_TenantId_Transferred",
                schema: "ensa",
                table: "WorkPlan",
                columns: new[] { "TenantId", "Transferred" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_ActivityId_ApprovalStatus",
                schema: "ensa",
                table: "WorkPlanLine",
                columns: new[] { "ActivityId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_ApproverUserId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_CompanyId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_DocumentId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_ForApprovalSenderUserId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "ForApprovalSenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_InstructorUserId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_PeriodId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_PreviousLineId",
                schema: "ensa",
                table: "WorkPlanLine",
                column: "PreviousLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanLine_WorkPlanId_Year_Month",
                schema: "ensa",
                table: "WorkPlanLine",
                columns: new[] { "WorkPlanId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_YearEndReviewLine_ParentLineId",
                schema: "ensa",
                table: "YearEndReviewLine",
                column: "ParentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_YearEndReviewLine_YearEndReviewReportId_ParentLineId_OrderNo",
                schema: "ensa",
                table: "YearEndReviewLine",
                columns: new[] { "YearEndReviewReportId", "ParentLineId", "OrderNo" });

            migrationBuilder.CreateIndex(
                name: "IX_YearEndReviewReport_PhysicianUserId",
                schema: "ensa",
                table: "YearEndReviewReport",
                column: "PhysicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_YearEndReviewReport_SpecialistUserId",
                schema: "ensa",
                table: "YearEndReviewReport",
                column: "SpecialistUserId");

            migrationBuilder.CreateIndex(
                name: "IX_YearEndReviewReport_TenantId_CompanyId",
                schema: "ensa",
                table: "YearEndReviewReport",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_YearEndReviewReport_TenantId_ReportDate",
                schema: "ensa",
                table: "YearEndReviewReport",
                columns: new[] { "TenantId", "ReportDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activity",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ActivityDuty",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ActivityGroup",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ActivityPeriod",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ActivityReport",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ActivityReportLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Archive",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "AssignedSpecialist",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "AssignedSpecialistDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Bank",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CashRegister",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CashTransaction",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Certificate",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "City",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Company",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyActivity",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyCheck",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyCheckLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyComplianceSummary",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyEmployee",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyEmployeeDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyEmployeeDuty",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyEmployeeDutyDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyLedgerEntry",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyModule",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyStandardDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyTag",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CompanyTrainingProgressMode",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ContractTemplate",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ControlItem",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ControlMeasure",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "CorrectiveAction",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "DepartmentDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "District",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Document",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "DocumentCategory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Duty",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmailSettings",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmergencyActionPlan",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmergencyPlanSection",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmergencyTeamMember",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeExamAnswer",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeFamilyHistory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeHealthInfo",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeImmunization",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeTrainingLog",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeTrainingProgress",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EmployeeWorkHistory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EPrescription",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EPrescriptionDiagnosis",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EPrescriptionMedication",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Equipment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EquipmentDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "EquipmentDocumentType",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ESignatureLicense",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Exam",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ExamAnswer",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ExamQuestion",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ExpenseCategory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "FieldObservationLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "FieldObservationReport",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Form",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "FormCategory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Hazard",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "HazardCategory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysChildReferenceValue",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysEquipmentTopCategory",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysIsco08OccupationCode",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysQuery",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysRootReferenceValue",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysServedWorkplace",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysWorkArrangement",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysWorkEnvironment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysWorkEnvironmentType",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IbysWorkEquipment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Icd10",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Icon",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IconLibrary",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IdentifiedHazard",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Incident",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "IncidentPerson",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Invoice",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "InvoiceLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "InvoiceTemplate",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Log",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Mail",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MailAttachment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExamComplaint",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExamHabit",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExamImmunization",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExaminationForm",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExamLabTest",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExamPhysicalFinding",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicalExamWorkCondition",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Medication",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicationDoseUnit",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicationFrequencyUnit",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MedicationRoute",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Menu",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MenuElement",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MenuItem",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MenuNode",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MenuPage",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MenuType",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Message",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MessageTemplate",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "MessageTemplateType",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Module",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ModuleArchive",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ModuleArchiveItem",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Neighborhood",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "NewsletterSubscriber",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "NumberSequence",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OccupationCode",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Office",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OfficeExpense",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OhsReport",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OhsReportHazardClassBreakdown",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OpenIddictScopes",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OpenIddictTokens",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Organization",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OrganizationContract",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OrganizationType",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OrganizationTypePermission",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Parameter",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "PaymentMethod",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Penalty",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "PenaltyAmount",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "PenaltySurvey",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "PenaltySurveyLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Period",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Permission",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "PermissionRestriction",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "PermissionScope",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Person",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ProspectOrganization",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentControlMeasure",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentExposedGroup",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentHistoryRecord",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentImprovementAction",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentParticipant",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentProtectedGroup",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RiskAssessmentReport",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RoleClaim",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RouteOrigin",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "RouteOriginDistance",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SalesRep",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SalesRepScreenField",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "ServiceItem",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SnapshotReport",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "StaffCostBaseline",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "StandardDocument",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SubscriptionPlan",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanPermission",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SupportTicket",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SupportTicketMessage",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "SystemSetting",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Training",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingDuration",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingExam",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingGroup",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingPlan",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingPlanLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingTopic",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TrainingTopicDuration",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Tree",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "TreeNode",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserClaim",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserLogin",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserMenuOverride",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserOffice",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserPermission",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserRole",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserToken",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserType",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserTypePermission",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Visit",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "WorkplaceDepartment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "WorkPlan",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "WorkPlanLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "YearEndReviewLine",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "YearEndReviewReport",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OpenIddictAuthorizations",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "User",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "OpenIddictApplications",
                schema: "ensa");
        }
    }
}
