using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POVISAPI.Data;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261122090000_AddPaymentIncentive")]
    public partial class AddPaymentIncentive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IncentiveRate",
                table: "Payments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IncentiveAmount",
                table: "Payments",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Payments recorded before the rate became configurable were reported at the fixed 10%; freeze that.
            migrationBuilder.Sql(@"UPDATE ""Payments"" SET ""IncentiveRate"" = 10, ""IncentiveAmount"" = ROUND(""Amount"" * 0.10, 2);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IncentiveAmount", table: "Payments");
            migrationBuilder.DropColumn(name: "IncentiveRate", table: "Payments");
        }
    }
}
