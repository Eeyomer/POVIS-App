using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddViolationRecordedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecordedByUserId",
                table: "Violations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordedByUserId",
                table: "Violations");
        }
    }
}
