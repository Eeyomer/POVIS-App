using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMaritalStatusUserPhotoViolationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaritalStatus",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Photo",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ViolationEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImageData = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViolationEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViolationEvidence_Violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "Violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ViolationEvidence_ViolationId",
                table: "ViolationEvidence",
                column: "ViolationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ViolationEvidence");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Users");
        }
    }
}
