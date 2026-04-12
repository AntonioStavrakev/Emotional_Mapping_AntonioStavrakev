using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emotional_Mapping.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MakeEmotionalPointDistrictIdNullable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EmotionalPoints_Districts_DistrictId",
            table: "EmotionalPoints");

        // Null out any rows with the default empty GUID (invalid FK value)
        migrationBuilder.Sql(
            "UPDATE \"EmotionalPoints\" SET \"DistrictId\" = NULL WHERE \"DistrictId\" = '00000000-0000-0000-0000-000000000000';");

        migrationBuilder.AlterColumn<Guid>(
            name: "DistrictId",
            table: "EmotionalPoints",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddForeignKey(
            name: "FK_EmotionalPoints_Districts_DistrictId",
            table: "EmotionalPoints",
            column: "DistrictId",
            principalTable: "Districts",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_EmotionalPoints_Districts_DistrictId",
            table: "EmotionalPoints");

        migrationBuilder.AlterColumn<Guid>(
            name: "DistrictId",
            table: "EmotionalPoints",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_EmotionalPoints_Districts_DistrictId",
            table: "EmotionalPoints",
            column: "DistrictId",
            principalTable: "Districts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
