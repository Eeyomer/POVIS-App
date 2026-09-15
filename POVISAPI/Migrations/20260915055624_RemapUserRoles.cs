using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemapUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old "Admin" role becomes MIS Personnel (system/account administration).
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = 'MISPersonnel' WHERE \"Role\" = 'Admin';");

            // Old "Staff" role becomes Apprehending Officer, the closest match to the
            // previous general field-user role.
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = 'ApprehendingOfficer' WHERE \"Role\" = 'Staff';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = 'Admin' WHERE \"Role\" = 'MISPersonnel';");

            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = 'Staff' WHERE \"Role\" = 'ApprehendingOfficer';");
        }
    }
}
