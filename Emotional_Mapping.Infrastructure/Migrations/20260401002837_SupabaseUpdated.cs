using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emotional_Mapping.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupabaseUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapRecommendations_GeneratedMaps_GeneratedMapId1",
                table: "MapRecommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_Places_Cities_CityId1",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_CityId1",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_MapRecommendations_GeneratedMapId1",
                table: "MapRecommendations");

            migrationBuilder.DropColumn(
                name: "CityId1",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "GeneratedMapId1",
                table: "MapRecommendations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CityId1",
                table: "Places",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GeneratedMapId1",
                table: "MapRecommendations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Places_CityId1",
                table: "Places",
                column: "CityId1");

            migrationBuilder.CreateIndex(
                name: "IX_MapRecommendations_GeneratedMapId1",
                table: "MapRecommendations",
                column: "GeneratedMapId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MapRecommendations_GeneratedMaps_GeneratedMapId1",
                table: "MapRecommendations",
                column: "GeneratedMapId1",
                principalTable: "GeneratedMaps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Places_Cities_CityId1",
                table: "Places",
                column: "CityId1",
                principalTable: "Cities",
                principalColumn: "Id");
        }
    }
}
