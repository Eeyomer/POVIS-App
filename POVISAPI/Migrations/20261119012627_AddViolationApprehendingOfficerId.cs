using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddViolationApprehendingOfficerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprehendingOfficerId",
                table: "Violations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Violations_ApprehendingOfficerId",
                table: "Violations",
                column: "ApprehendingOfficerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Violations_Users_ApprehendingOfficerId",
                table: "Violations",
                column: "ApprehendingOfficerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Link existing violations to the registered officer with the same deputation number.
            migrationBuilder.Sql(@"
                UPDATE ""Violations"" v
                SET ""ApprehendingOfficerId"" = u.""Id"", ""RecordedByUserId"" = COALESCE(v.""RecordedByUserId"", u.""Id"")
                FROM ""Users"" u
                WHERE u.""Role"" = 'ApprehendingOfficer'
                  AND u.""DeputationNumber"" IS NOT NULL
                  AND v.""ApprehendingOfficerDeputationNumber"" = u.""DeputationNumber"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Violations_Users_ApprehendingOfficerId",
                table: "Violations");

            migrationBuilder.DropIndex(
                name: "IX_Violations_ApprehendingOfficerId",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "ApprehendingOfficerId",
                table: "Violations");
        }
    }
}
