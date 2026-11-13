using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyVersionAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftJson",
                schema: "public",
                table: "SurveyDefinitionVersion",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastEditedAt",
                schema: "public",
                table: "SurveyDefinitionVersion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEditedBy",
                schema: "public",
                table: "SurveyDefinitionVersion",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedBy",
                schema: "public",
                table: "SurveyDefinitionVersion",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedJson",
                schema: "public",
                table: "SurveyDefinitionVersion",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftJson",
                schema: "public",
                table: "SurveyDefinitionVersion");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                schema: "public",
                table: "SurveyDefinitionVersion");

            migrationBuilder.DropColumn(
                name: "LastEditedBy",
                schema: "public",
                table: "SurveyDefinitionVersion");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                schema: "public",
                table: "SurveyDefinitionVersion");

            migrationBuilder.DropColumn(
                name: "PublishedJson",
                schema: "public",
                table: "SurveyDefinitionVersion");
        }
    }
}
