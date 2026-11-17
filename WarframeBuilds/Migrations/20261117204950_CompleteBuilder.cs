using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class CompleteBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the original slot rows before repairing the old nullable composite key.
            // Duplicate slot configurations are archived, never silently discarded.
            migrationBuilder.Sql("""
                CREATE TABLE "LegacyBuildModsBackup" AS SELECT * FROM "BuildMods";
                WITH ranked AS (SELECT ctid, row_number() OVER(PARTITION BY "BuildId", "SlotIndex" ORDER BY "ModId" DESC) AS n FROM "BuildMods")
                DELETE FROM "BuildMods" WHERE ctid IN (SELECT ctid FROM ranked WHERE n > 1);
                WITH numbered AS (SELECT ctid, row_number() OVER(ORDER BY "BuildId", "SlotIndex") AS n FROM "BuildMods")
                UPDATE "BuildMods" b SET "Id"=n.n FROM numbered n WHERE b.ctid=n.ctid;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_BuildMods_Mods_ModId",
                table: "BuildMods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BuildMods",
                table: "BuildMods");

            migrationBuilder.AddColumn<double>(
                name: "BaseDamage",
                table: "Weapons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CriticalChance",
                table: "Weapons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CriticalMultiplier",
                table: "Weapons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FireRate",
                table: "Weapons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ImageName",
                table: "Weapons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MagazineSize",
                table: "Weapons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ReloadTime",
                table: "Weapons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StatusChance",
                table: "Weapons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "UniqueName",
                table: "Weapons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WeaponType",
                table: "Weapons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Compatibility",
                table: "Mods",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsExilus",
                table: "Mods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LevelStatsJson",
                table: "Mods",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Mods",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniqueName",
                table: "Mods",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Guides",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Builds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "HasReactor",
                table: "Builds",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Builds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "WeaponId",
                table: "Builds",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BuildMods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ModId",
                table: "BuildMods",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('"BuildMods"', 'Id'), COALESCE(MAX("Id"),0)+1, false) FROM "BuildMods";
                UPDATE "Builds" SET "CreatedAt"=CURRENT_TIMESTAMP, "UpdatedAt"=CURRENT_TIMESTAMP;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BuildMods",
                table: "BuildMods",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BuildComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildId = table.Column<int>(type: "integer", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildComments_Builds_BuildId",
                        column: x => x.BuildId,
                        principalTable: "Builds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Builds_UserId",
                table: "Builds",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Builds_WeaponId",
                table: "Builds",
                column: "WeaponId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Build_Target",
                table: "Builds",
                sql: "\"WarframeId\" IS NULL OR \"WeaponId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BuildMods_BuildId_SlotIndex",
                table: "BuildMods",
                columns: new[] { "BuildId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildComments_BuildId",
                table: "BuildComments",
                column: "BuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_BuildMods_Mods_ModId",
                table: "BuildMods",
                column: "ModId",
                principalTable: "Mods",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Builds_Weapons_WeaponId",
                table: "Builds",
                column: "WeaponId",
                principalTable: "Weapons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BuildMods_Mods_ModId",
                table: "BuildMods");

            migrationBuilder.DropForeignKey(
                name: "FK_Builds_Weapons_WeaponId",
                table: "Builds");

            migrationBuilder.DropTable(
                name: "BuildComments");

            migrationBuilder.DropIndex(
                name: "IX_Builds_UserId",
                table: "Builds");

            migrationBuilder.DropIndex(
                name: "IX_Builds_WeaponId",
                table: "Builds");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Build_Target",
                table: "Builds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BuildMods",
                table: "BuildMods");

            migrationBuilder.DropIndex(
                name: "IX_BuildMods_BuildId_SlotIndex",
                table: "BuildMods");

            migrationBuilder.DropColumn(
                name: "BaseDamage",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "CriticalChance",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "CriticalMultiplier",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "FireRate",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "ImageName",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "MagazineSize",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "ReloadTime",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "StatusChance",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "UniqueName",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "WeaponType",
                table: "Weapons");

            migrationBuilder.DropColumn(
                name: "Compatibility",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "IsExilus",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "LevelStatsJson",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "UniqueName",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Guides");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "HasReactor",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "WeaponId",
                table: "Builds");

            migrationBuilder.AlterColumn<int>(
                name: "ModId",
                table: "BuildMods",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BuildMods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BuildMods",
                table: "BuildMods",
                columns: new[] { "BuildId", "ModId", "SlotIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_BuildMods_Mods_ModId",
                table: "BuildMods",
                column: "ModId",
                principalTable: "Mods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
