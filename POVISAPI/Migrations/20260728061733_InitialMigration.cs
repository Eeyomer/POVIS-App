using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Barangays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barangays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ordinances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdinanceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Series = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsExecutiveOrder = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordinances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Violators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Suffix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Gender = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BarangayId = table.Column<Guid>(type: "uuid", nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PresentedValidId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Violators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Violators_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrdinanceSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdinanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdinanceSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdinanceSections_Ordinances_OrdinanceId",
                        column: x => x.OrdinanceId,
                        principalTable: "Ordinances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdinancePenalties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdinanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdinanceSectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OffenseLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OffenderType = table.Column<int>(type: "integer", nullable: true),
                    FineAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SeminarDays = table.Column<int>(type: "integer", nullable: true),
                    CommunityServiceHours = table.Column<int>(type: "integer", nullable: true),
                    CommunityServiceDays = table.Column<int>(type: "integer", nullable: true),
                    RequiresCounseling = table.Column<bool>(type: "boolean", nullable: false),
                    HasImprisonment = table.Column<bool>(type: "boolean", nullable: false),
                    ImprisonmentTerm = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BusinessPermitAction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsConjunctive = table.Column<bool>(type: "boolean", nullable: false),
                    PenaltyText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdinancePenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdinancePenalties_OrdinanceSections_OrdinanceSectionId",
                        column: x => x.OrdinanceSectionId,
                        principalTable: "OrdinanceSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdinancePenalties_Ordinances_OrdinanceId",
                        column: x => x.OrdinanceId,
                        principalTable: "Ordinances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Violations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OvrNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DateOfSubmission = table.Column<DateOnly>(type: "date", nullable: false),
                    Division = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AgeAtApprehension = table.Column<int>(type: "integer", nullable: true),
                    DateOfOccurrence = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeOfOccurrence = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    PlaceOfOccurrence = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BarangayOfOccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Witness = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ApprehendingOfficer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    OrdinanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdinanceSectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OffenseLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsWarningTicket = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remarks2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Violations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Violations_Barangays_BarangayOfOccurrenceId",
                        column: x => x.BarangayOfOccurrenceId,
                        principalTable: "Barangays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Violations_OrdinanceSections_OrdinanceSectionId",
                        column: x => x.OrdinanceSectionId,
                        principalTable: "OrdinanceSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Violations_Ordinances_OrdinanceId",
                        column: x => x.OrdinanceId,
                        principalTable: "Ordinances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Violations_Violators_ViolatorId",
                        column: x => x.ViolatorId,
                        principalTable: "Violators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityServiceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredHours = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    RequiredDays = table.Column<int>(type: "integer", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DateCompleted = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignedLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityServiceRecords_Violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "Violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CounselingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterventionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConductedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DateCompleted = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ParentOrGuardianPresent = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounselingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CounselingRecords_Violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "Violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiledCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateFiled = table.Column<DateOnly>(type: "date", nullable: false),
                    NpsDocketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateResolved = table.Column<DateOnly>(type: "date", nullable: true),
                    ResolutionRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiledCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiledCases_Violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "Violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateOfPayment = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaidAt = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "Violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeminarRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeminarType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RequiredDays = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateCompleted = table.Column<DateOnly>(type: "date", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeminarRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeminarRecords_Violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "Violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Barangays_Name",
                table: "Barangays",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityServiceRecords_ViolationId",
                table: "CommunityServiceRecords",
                column: "ViolationId");

            migrationBuilder.CreateIndex(
                name: "IX_CounselingRecords_ViolationId",
                table: "CounselingRecords",
                column: "ViolationId");

            migrationBuilder.CreateIndex(
                name: "IX_FiledCases_DateFiled",
                table: "FiledCases",
                column: "DateFiled");

            migrationBuilder.CreateIndex(
                name: "IX_FiledCases_ViolationId",
                table: "FiledCases",
                column: "ViolationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdinancePenalties_OrdinanceId_OrdinanceSectionId_OffenseLe~",
                table: "OrdinancePenalties",
                columns: new[] { "OrdinanceId", "OrdinanceSectionId", "OffenseLevel", "OffenderType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdinancePenalties_OrdinanceSectionId",
                table: "OrdinancePenalties",
                column: "OrdinanceSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordinances_OrdinanceNumber_Series",
                table: "Ordinances",
                columns: new[] { "OrdinanceNumber", "Series" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdinanceSections_OrdinanceId_SectionCode",
                table: "OrdinanceSections",
                columns: new[] { "OrdinanceId", "SectionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DateOfPayment",
                table: "Payments",
                column: "DateOfPayment");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ViolationId",
                table: "Payments",
                column: "ViolationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeminarRecords_ViolationId",
                table: "SeminarRecords",
                column: "ViolationId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_BarangayOfOccurrenceId",
                table: "Violations",
                column: "BarangayOfOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_DateOfOccurrence",
                table: "Violations",
                column: "DateOfOccurrence");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_OrdinanceId",
                table: "Violations",
                column: "OrdinanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_OrdinanceSectionId",
                table: "Violations",
                column: "OrdinanceSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_OvrNumber_OrdinanceId",
                table: "Violations",
                columns: new[] { "OvrNumber", "OrdinanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Status",
                table: "Violations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_ViolatorId",
                table: "Violations",
                column: "ViolatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Violators_BarangayId",
                table: "Violators",
                column: "BarangayId");

            migrationBuilder.CreateIndex(
                name: "IX_Violators_LastName_FirstName",
                table: "Violators",
                columns: new[] { "LastName", "FirstName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityServiceRecords");

            migrationBuilder.DropTable(
                name: "CounselingRecords");

            migrationBuilder.DropTable(
                name: "FiledCases");

            migrationBuilder.DropTable(
                name: "OrdinancePenalties");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "SeminarRecords");

            migrationBuilder.DropTable(
                name: "Violations");

            migrationBuilder.DropTable(
                name: "OrdinanceSections");

            migrationBuilder.DropTable(
                name: "Violators");

            migrationBuilder.DropTable(
                name: "Ordinances");

            migrationBuilder.DropTable(
                name: "Barangays");
        }
    }
}
