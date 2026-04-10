using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiveIntoIVE.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyCurrencySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyBalance",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDailyRewardClaimedAtUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastDailyRewardClaimedAtUtc",
                table: "Users");
        }
    }
}
