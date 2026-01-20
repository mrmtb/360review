using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityUserPasskey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetPasskeys",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignCount = table.Column<long>(type: "bigint", nullable: false),
                    Transports = table.Column<string[]>(type: "text[]", nullable: true),
                    IsUserVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsBackupEligible = table.Column<bool>(type: "boolean", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "boolean", nullable: false),
                    AttestationObject = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClientDataJson = table.Column<byte[]>(type: "bytea", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetPasskeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserPasskeys",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    DataId = table.Column<byte[]>(type: "bytea", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserPasskeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserPasskeys_AspNetPasskeys_DataId",
                        column: x => x.DataId,
                        principalSchema: "public",
                        principalTable: "AspNetPasskeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserPasskeys_DataId",
                schema: "public",
                table: "AspNetUserPasskeys",
                column: "DataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserPasskeys",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AspNetPasskeys",
                schema: "public");
        }
    }
}
