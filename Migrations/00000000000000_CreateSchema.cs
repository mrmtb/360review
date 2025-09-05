using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class _00000000000000_CreateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: true),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    NormalizedName = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "public",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Client",
                schema: "public",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ClientName = table.Column<string>(type: "text", nullable: false),
                    ClientLogoLocation = table.Column<string>(type: "text", nullable: false),
                    ClientHelpContact = table.Column<string>(type: "text", nullable: false),
                    ClientIsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ClientEmailDomain = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: false),
                    Xml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

//            migrationBuilder.CreateTable(
//                name: "EligibleRaters",
//                schema: "public",
//                columns: table => new
//                {
//                    EvaluationRateeId = table.Column<Guid>(type: "uuid", nullable: false),
//                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
//                    ParticipantNameFirst = table.Column<string>(type: "text", nullable: false),
//                    ParticipantNameLast = table.Column<string>(type: "text", nullable: false),
//                    ParticipantEmail = table.Column<string>(type: "text", nullable: false),
//                    ParticipantHasRegistered = table.Column<bool>(type: "boolean", nullable: false)
//                },
//                constraints: table =>
//                {
//                });

            migrationBuilder.CreateTable(
                name: "EmailQueue",
                schema: "public",
                columns: table => new
                {
                    EmailQueueId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationRateeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailActor = table.Column<string>(type: "text", nullable: false),
                    EmailQueuedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailTest = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailFrom = table.Column<string>(type: "text", nullable: false),
                    EmailTo = table.Column<string>(type: "text", nullable: false),
                    EmailSubject = table.Column<string>(type: "text", nullable: false),
                    EmailBodyHTML = table.Column<string>(type: "text", nullable: false),
                    EmailBodyText = table.Column<string>(type: "text", nullable: false),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailSentDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueue", x => x.EmailQueueId);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationParticipantStatus",
                schema: "public",
                columns: table => new
                {
                    EvaluationParticipantStatusId = table.Column<int>(type: "integer", nullable: false),
                    EvaluationParticipantStatusName = table.Column<string>(type: "text", nullable: false),
                    OnReminderNewStatus = table.Column<int>(type: "integer", nullable: false),
                    OnCompletionNewStatus = table.Column<int>(type: "integer", nullable: false),
                    OnOptOutNewStatus = table.Column<int>(type: "integer", nullable: false),
                    EvaluationParticipantStatusIsActive = table.Column<int>(type: "integer", nullable: false),
                    EvaluationParticipantStatusIsComplete = table.Column<int>(type: "integer", nullable: false),
                    EvaluationParticipantStatusDecoration = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationParticipantStatus", x => x.EvaluationParticipantStatusId);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRateeStatus",
                schema: "public",
                columns: table => new
                {
                    EvaluationRateeStatusId = table.Column<int>(type: "integer", nullable: false),
                    EvaluationRateeStatusName = table.Column<string>(type: "text", nullable: false),
                    OnReminderNewStatus = table.Column<int>(type: "integer", nullable: false),
                    OnCompletionNewStatus = table.Column<int>(type: "integer", nullable: false),
                    OnOptOutNewStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRateeStatus", x => x.EvaluationRateeStatusId);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantRole",
                schema: "public",
                columns: table => new
                {
                    ParticipantRoleId = table.Column<int>(type: "integer", nullable: false),
                    ParticipantRoleName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantRole", x => x.ParticipantRoleId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRole",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: true, defaultValueSql: "gen_random_uuid()"),
                    LoginProvider = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRole_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplate",
                schema: "public",
                columns: table => new
                {
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailTemplateName = table.Column<string>(type: "text", nullable: false),
                    EmailFromName = table.Column<string>(type: "text", nullable: false),
                    EmailSubject = table.Column<string>(type: "text", nullable: false),
                    EmailTemplateContent = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplate", x => x.EmailTemplateId);
                    table.ForeignKey(
                        name: "FK_EmailTemplate_Client_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "public",
                        principalTable: "Client",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evaluation",
                schema: "public",
                columns: table => new
                {
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EvaluationActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EvaluationName = table.Column<string>(type: "text", nullable: false),
                    EvaluationFolder = table.Column<string>(type: "text", nullable: false),
                    EvaluationHelpContact = table.Column<string>(type: "text", nullable: true),
                    EvaluationMaxRaters = table.Column<int>(type: "integer", nullable: false),
                    EvaluationOpenDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationRateeDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationRaterDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationClientContact = table.Column<string>(type: "text", nullable: true),
                    EvaluationTestEmails = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluation", x => x.EvaluationId);
                    table.ForeignKey(
                        name: "FK_Client_Evaluation_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "public",
                        principalTable: "Client",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participant",
                schema: "public",
                columns: table => new
                {
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ParticipantNameFirst = table.Column<string>(type: "text", nullable: false),
                    ParticipantNameLast = table.Column<string>(type: "text", nullable: false),
                    ParticipantEmail = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantHasRegistered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ParticipantIsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participant", x => x.ParticipantId);
                    table.ForeignKey(
                        name: "FK_Participant_Client_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "public",
                        principalTable: "Client",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRatee",
                schema: "public",
                columns: table => new
                {
                    EvaluationRateeId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationRateeStatusId = table.Column<int>(type: "integer", nullable: false),
                    EvaluationRateeDueDateOverride = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationRaterDueDateOverride = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationRateeDateAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRatee", x => x.EvaluationRateeId);
                    table.ForeignKey(
                        name: "FK_EvaluationRatee_EvaluationRateeStatus",
                        column: x => x.EvaluationRateeStatusId,
                        principalSchema: "public",
                        principalTable: "EvaluationRateeStatus",
                        principalColumn: "EvaluationRateeStatusId");
                    table.ForeignKey(
                        name: "FK_EvaluationRatee_Evaluation_EvaluationId",
                        column: x => x.EvaluationId,
                        principalSchema: "public",
                        principalTable: "Evaluation",
                        principalColumn: "EvaluationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationRatee_Participant_ParticipantId",
                        column: x => x.ParticipantId,
                        principalSchema: "public",
                        principalTable: "Participant",
                        principalColumn: "ParticipantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationParticipant",
                schema: "public",
                columns: table => new
                {
                    EvaluationRateeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantRoleId = table.Column<int>(type: "integer", nullable: true),
                    EvaluationParticipantSurveyPassword = table.Column<string>(type: "text", nullable: false),
                    EvaluationParticipantDateLastEmailed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluationParticipantStatusId = table.Column<int>(type: "integer", nullable: true),
                    EvaluationParticipantDateAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EvaluationParticipantSessionId = table.Column<string>(type: "text", nullable: true),
                    EvaluationParticipantDateCompleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationParticipant", x => new { x.EvaluationRateeId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_EvaluationParticipant_EvaluationParticipantStatus",
                        column: x => x.EvaluationParticipantStatusId,
                        principalSchema: "public",
                        principalTable: "EvaluationParticipantStatus",
                        principalColumn: "EvaluationParticipantStatusId");
                    table.ForeignKey(
                        name: "FK_EvaluationParticipant_EvaluationRatee",
                        column: x => x.EvaluationRateeId,
                        principalSchema: "public",
                        principalTable: "EvaluationRatee",
                        principalColumn: "EvaluationRateeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationParticipant_ParticipantRole_ParticipantRoleId",
                        column: x => x.ParticipantRoleId,
                        principalSchema: "public",
                        principalTable: "ParticipantRole",
                        principalColumn: "ParticipantRoleId");
                    table.ForeignKey(
                        name: "FK_EvaluationParticipant_Participant_ParticipantId",
                        column: x => x.ParticipantId,
                        principalSchema: "public",
                        principalTable: "Participant",
                        principalColumn: "ParticipantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "public",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplate_ClientId",
                schema: "public",
                table: "EmailTemplate",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_ClientId",
                schema: "public",
                table: "Evaluation",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipant_EvaluationParticipantStatusId",
                schema: "public",
                table: "EvaluationParticipant",
                column: "EvaluationParticipantStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipant_ParticipantId",
                schema: "public",
                table: "EvaluationParticipant",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipant_ParticipantRoleId",
                schema: "public",
                table: "EvaluationParticipant",
                column: "ParticipantRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRatee_EvaluationRateeStatusId",
                schema: "public",
                table: "EvaluationRatee",
                column: "EvaluationRateeStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRatee_ParticipantId",
                schema: "public",
                table: "EvaluationRatee",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "UK_EvaluationRatee_EvaluationId_ParticipantId",
                schema: "public",
                table: "EvaluationRatee",
                columns: new[] { "EvaluationId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participant_ClientId",
                schema: "public",
                table: "Participant",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "UK_Participant_ParticipantEmail",
                schema: "public",
                table: "Participant",
                column: "ParticipantEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetUserRole",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys",
                schema: "public");

//            migrationBuilder.DropTable(
//                name: "EligibleRaters",
//                schema: "public");

            migrationBuilder.DropTable(
                name: "EmailQueue",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EmailTemplate",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationParticipant",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationParticipantStatus",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationRatee",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ParticipantRole",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationRateeStatus",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Evaluation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Participant",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Client",
                schema: "public");
        }
    }
}
