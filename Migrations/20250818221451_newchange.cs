using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class newchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "EvaluationTestEmails",
                schema: "public",
                table: "Evaluation",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "EvaluationTestEmails",
                schema: "public",
                table: "Evaluation",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
