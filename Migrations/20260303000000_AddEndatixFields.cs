using System;
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
            migrationBuilder.AddColumn<string>(
                name: "EndatixSubmissionId",
                table: "EvaluationParticipants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SurveyLastSavedAt",
                table: "EvaluationParticipants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurveyVersion",
                table: "EvaluationParticipants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurveyTemplateKey",
                table: "EvaluationParticipants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipants_EndatixSubmissionId",
                table: "EvaluationParticipants",
                column: "EndatixSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvaluationParticipants_EndatixSubmissionId",
                table: "EvaluationParticipants");

            migrationBuilder.DropColumn(
                name: "EndatixSubmissionId",
                table: "EvaluationParticipants");

            migrationBuilder.DropColumn(
                name: "SurveyLastSavedAt",
                table: "EvaluationParticipants");

            migrationBuilder.DropColumn(
                name: "SurveyVersion",
                table: "EvaluationParticipants");

            migrationBuilder.DropColumn(
                name: "SurveyTemplateKey",
                table: "EvaluationParticipants");
        }
    }
}
