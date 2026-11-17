using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorNameToBuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Builds_Warframes_WarframeId",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "Upvotes",
                table: "Builds");

            migrationBuilder.AlterColumn<int>(
                name: "WarframeId",
                table: "Builds",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Builds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Builds_Warframes_WarframeId",
                table: "Builds",
                column: "WarframeId",
                principalTable: "Warframes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Builds_Warframes_WarframeId",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Builds");

            migrationBuilder.AlterColumn<int>(
                name: "WarframeId",
                table: "Builds",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Builds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Upvotes",
                table: "Builds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Builds_Warframes_WarframeId",
                table: "Builds",
                column: "WarframeId",
                principalTable: "Warframes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
