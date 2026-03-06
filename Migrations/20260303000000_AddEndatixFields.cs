using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddEndatixFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op.
            // The original migration draft targeted incorrect table/column names.
            // SurveyJS schema changes are applied by 20260304065955_AddSurveyJSSupport.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op.
        }
    }
}
