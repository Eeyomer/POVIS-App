using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBarangay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BarangayId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BarangayId",
                table: "Users",
                column: "BarangayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Barangays_BarangayId",
                table: "Users",
                column: "BarangayId",
                principalTable: "Barangays",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Barangays_BarangayId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BarangayId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BarangayId",
                table: "Users");
        }
    }
}
