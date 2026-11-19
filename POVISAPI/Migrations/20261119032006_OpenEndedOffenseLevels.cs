using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POVISAPI.Migrations
{
    /// <inheritdoc />
    public partial class OpenEndedOffenseLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Offense levels used to be stored as text ("First".."Fourth"); they are now plain numbers so a section
            // can have any number of escalating levels. Convert existing rows.
            foreach (var table in new[] { "Violations", "OrdinancePenalties" })
            {
                migrationBuilder.Sql($@"
                    ALTER TABLE ""{table}"" ALTER COLUMN ""OffenseLevel"" TYPE integer
                    USING (CASE ""OffenseLevel""
                        WHEN 'First' THEN 1 WHEN 'Second' THEN 2 WHEN 'Third' THEN 3 WHEN 'Fourth' THEN 4
                        ELSE 1 END);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Levels above 4 cannot be represented by the old text values and collapse to "Fourth".
            foreach (var table in new[] { "Violations", "OrdinancePenalties" })
            {
                migrationBuilder.Sql($@"
                    ALTER TABLE ""{table}"" ALTER COLUMN ""OffenseLevel"" TYPE character varying(30)
                    USING (CASE WHEN ""OffenseLevel"" = 1 THEN 'First' WHEN ""OffenseLevel"" = 2 THEN 'Second'
                                WHEN ""OffenseLevel"" = 3 THEN 'Third' ELSE 'Fourth' END);");
            }
        }
    }
}
