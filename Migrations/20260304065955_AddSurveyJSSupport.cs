using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyJSSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SurveyLastSavedAt",
                schema: "public",
                table: "EvaluationParticipant",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurveyTemplateKey",
                schema: "public",
                table: "EvaluationParticipant",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SurveyTemplateVersion",
                schema: "public",
                table: "EvaluationParticipant",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EvaluationParticipantResponse",
                schema: "public",
                columns: table => new
                {
                    EvaluationParticipantResponseId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EvaluationRateeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaterParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationParticipantSurveyPassword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SurveyTemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SurveyTemplateVersion = table.Column<int>(type: "integer", nullable: false),
                    DraftJson = table.Column<string>(type: "jsonb", nullable: true),
                    FinalJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastSavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationParticipantResponse", x => x.EvaluationParticipantResponseId);
                    table.ForeignKey(
                        name: "FK_EvaluationParticipantResponse_EvaluationRatee_EvaluationRat~",
                        column: x => x.EvaluationRateeId,
                        principalSchema: "public",
                        principalTable: "EvaluationRatee",
                        principalColumn: "EvaluationRateeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationParticipantResponse_Participant_RaterParticipantId",
                        column: x => x.RaterParticipantId,
                        principalSchema: "public",
                        principalTable: "Participant",
                        principalColumn: "ParticipantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipantResponse_RateeId",
                schema: "public",
                table: "EvaluationParticipantResponse",
                column: "EvaluationRateeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipantResponse_RaterParticipantId",
                schema: "public",
                table: "EvaluationParticipantResponse",
                column: "RaterParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipantResponse_Template",
                schema: "public",
                table: "EvaluationParticipantResponse",
                columns: new[] { "SurveyTemplateKey", "SurveyTemplateVersion" });

            migrationBuilder.CreateIndex(
                name: "UK_EvaluationParticipantResponse_Password",
                schema: "public",
                table: "EvaluationParticipantResponse",
                column: "EvaluationParticipantSurveyPassword",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationParticipantResponse",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "SurveyLastSavedAt",
                schema: "public",
                table: "EvaluationParticipant");

            migrationBuilder.DropColumn(
                name: "SurveyTemplateKey",
                schema: "public",
                table: "EvaluationParticipant");

            migrationBuilder.DropColumn(
                name: "SurveyTemplateVersion",
                schema: "public",
                table: "EvaluationParticipant");
        }
    }
}
