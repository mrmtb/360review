using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyVersioningFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipantResponse",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedSurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipant",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSurveyDefinitionVersionId",
                schema: "public",
                table: "Evaluation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SurveyDefinition",
                schema: "public",
                columns: table => new
                {
                    SurveyDefinitionId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyDefinitionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SurveyDefinitionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyDefinition", x => x.SurveyDefinitionId);
                    table.ForeignKey(
                        name: "FK_SurveyDefinition_Client_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "public",
                        principalTable: "Client",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyDefinitionVersion",
                schema: "public",
                columns: table => new
                {
                    SurveyDefinitionVersionId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SurveyDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TemplatePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyDefinitionVersion", x => x.SurveyDefinitionVersionId);
                    table.ForeignKey(
                        name: "FK_SurveyDefinitionVersion_SurveyDefinition_SurveyDefinitionId",
                        column: x => x.SurveyDefinitionId,
                        principalSchema: "public",
                        principalTable: "SurveyDefinition",
                        principalColumn: "SurveyDefinitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipantResponse_SurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipantResponse",
                column: "SurveyDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipant_AssignedSurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipant",
                column: "AssignedSurveyDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_CurrentSurveyDefinitionVersionId",
                schema: "public",
                table: "Evaluation",
                column: "CurrentSurveyDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "UK_SurveyDefinition_ClientId_SurveyDefinitionKey",
                schema: "public",
                table: "SurveyDefinition",
                columns: new[] { "ClientId", "SurveyDefinitionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UK_SurveyDefinitionVersion_SurveyDefinitionId_VersionNumber",
                schema: "public",
                table: "SurveyDefinitionVersion",
                columns: new[] { "SurveyDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.Sql(
                """
                WITH seeded_definitions AS (
                    INSERT INTO public."SurveyDefinition" (
                        "SurveyDefinitionId",
                        "ClientId",
                        "SurveyDefinitionKey",
                        "SurveyDefinitionName",
                        "IsActive",
                        "CreatedAt"
                    )
                    SELECT
                        gen_random_uuid(),
                        c."ClientId",
                        'leadership-competency',
                        'Leadership Competency',
                        TRUE,
                        NOW()
                    FROM public."Client" c
                    RETURNING "SurveyDefinitionId", "ClientId"
                ),
                seeded_versions AS (
                    INSERT INTO public."SurveyDefinitionVersion" (
                        "SurveyDefinitionVersionId",
                        "SurveyDefinitionId",
                        "VersionNumber",
                        "TemplateKey",
                        "TemplatePath",
                        "IsPublished",
                        "CreatedAt",
                        "PublishedAt"
                    )
                    SELECT
                        gen_random_uuid(),
                        sd."SurveyDefinitionId",
                        1,
                        'leadership-competency-v1',
                        'Surveys/leadership-competency-v1.json',
                        TRUE,
                        NOW(),
                        NOW()
                    FROM seeded_definitions sd
                    RETURNING "SurveyDefinitionVersionId", "SurveyDefinitionId"
                )
                UPDATE public."Evaluation" e
                SET "CurrentSurveyDefinitionVersionId" = sv."SurveyDefinitionVersionId"
                FROM seeded_versions sv
                INNER JOIN seeded_definitions sd
                    ON sd."SurveyDefinitionId" = sv."SurveyDefinitionId"
                WHERE e."ClientId" = sd."ClientId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE public."EvaluationParticipantResponse" epr
                SET
                    "SurveyDefinitionVersionId" = e."CurrentSurveyDefinitionVersionId",
                    "SurveyTemplateKey" = COALESCE(NULLIF(epr."SurveyTemplateKey", ''), 'leadership-competency-v1'),
                    "SurveyTemplateVersion" = CASE
                        WHEN epr."SurveyTemplateVersion" IS NULL OR epr."SurveyTemplateVersion" = 0 THEN 1
                        ELSE epr."SurveyTemplateVersion"
                    END
                FROM public."EvaluationRatee" er
                INNER JOIN public."Evaluation" e
                    ON e."EvaluationId" = er."EvaluationId"
                WHERE er."EvaluationRateeId" = epr."EvaluationRateeId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE public."EvaluationParticipant" ep
                SET
                    "AssignedSurveyDefinitionVersionId" = e."CurrentSurveyDefinitionVersionId",
                    "SurveyTemplateKey" = COALESCE(ep."SurveyTemplateKey", 'leadership-competency-v1'),
                    "SurveyTemplateVersion" = COALESCE(ep."SurveyTemplateVersion", 1)
                FROM public."EvaluationRatee" er
                INNER JOIN public."Evaluation" e
                    ON e."EvaluationId" = er."EvaluationId"
                WHERE er."EvaluationRateeId" = ep."EvaluationRateeId"
                  AND EXISTS (
                      SELECT 1
                      FROM public."EvaluationParticipantResponse" epr
                      WHERE epr."EvaluationParticipantSurveyPassword" = ep."EvaluationParticipantSurveyPassword"
                  );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluation_SurveyDefinitionVersion_CurrentSurveyDefinitionVersionId",
                schema: "public",
                table: "Evaluation",
                column: "CurrentSurveyDefinitionVersionId",
                principalSchema: "public",
                principalTable: "SurveyDefinitionVersion",
                principalColumn: "SurveyDefinitionVersionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationParticipant_SurveyDefinitionVersion_AssignedSurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipant",
                column: "AssignedSurveyDefinitionVersionId",
                principalSchema: "public",
                principalTable: "SurveyDefinitionVersion",
                principalColumn: "SurveyDefinitionVersionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationParticipantResponse_SurveyDefinitionVersion_Surve~",
                schema: "public",
                table: "EvaluationParticipantResponse",
                column: "SurveyDefinitionVersionId",
                principalSchema: "public",
                principalTable: "SurveyDefinitionVersion",
                principalColumn: "SurveyDefinitionVersionId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluation_SurveyDefinitionVersion_CurrentSurveyDefinitionVersionId",
                schema: "public",
                table: "Evaluation");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationParticipant_SurveyDefinitionVersion_AssignedSurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipant");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationParticipantResponse_SurveyDefinitionVersion_Surve~",
                schema: "public",
                table: "EvaluationParticipantResponse");

            migrationBuilder.DropTable(
                name: "SurveyDefinitionVersion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SurveyDefinition",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationParticipantResponse_SurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipantResponse");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationParticipant_AssignedSurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipant");

            migrationBuilder.DropIndex(
                name: "IX_Evaluation_CurrentSurveyDefinitionVersionId",
                schema: "public",
                table: "Evaluation");

            migrationBuilder.DropColumn(
                name: "SurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipantResponse");

            migrationBuilder.DropColumn(
                name: "AssignedSurveyDefinitionVersionId",
                schema: "public",
                table: "EvaluationParticipant");

            migrationBuilder.DropColumn(
                name: "CurrentSurveyDefinitionVersionId",
                schema: "public",
                table: "Evaluation");
        }
    }
}
