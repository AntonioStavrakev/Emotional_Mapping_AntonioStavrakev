using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emotional_Mapping.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCreditPacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiCreditPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PackageCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TotalCredits = table.Column<int>(type: "integer", nullable: false),
                    RemainingCredits = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StripeSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCreditPacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiCreditPacks_ExpiresAtUtc",
                table: "AiCreditPacks",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiCreditPacks_StripeSessionId",
                table: "AiCreditPacks",
                column: "StripeSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiCreditPacks_UserId",
                table: "AiCreditPacks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCreditPacks");
        }
    }
}
