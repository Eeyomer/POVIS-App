using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class CleanMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Violators");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Violators");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SeminarRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SeminarRecords");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OrdinanceSections");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "OrdinanceSections");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Ordinances");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Ordinances");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OrdinancePenalties");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "OrdinancePenalties");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FiledCases");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FiledCases");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CounselingRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CounselingRecords");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CommunityServiceRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CommunityServiceRecords");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Barangays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Violators",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Violators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Violations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Violations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SeminarRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SeminarRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Payments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OrdinanceSections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "OrdinanceSections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Ordinances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Ordinances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OrdinancePenalties",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "OrdinancePenalties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FiledCases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "FiledCases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CounselingRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CounselingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CommunityServiceRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CommunityServiceRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Barangays",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Barangays",
                type: "text",
                nullable: true);
        }
    }
}
