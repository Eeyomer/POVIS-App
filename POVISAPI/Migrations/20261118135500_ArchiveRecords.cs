using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Violations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Violations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "SeminarRecords",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "SeminarRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Payments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "FiledCases",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "FiledCases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "CounselingRecords",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "CounselingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "CommunityServiceRecords",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "CommunityServiceRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "SeminarRecords");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "SeminarRecords");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "FiledCases");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "FiledCases");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "CounselingRecords");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "CounselingRecords");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "CommunityServiceRecords");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "CommunityServiceRecords");
        }
    }
}
