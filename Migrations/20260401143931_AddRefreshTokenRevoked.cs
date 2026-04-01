using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiveIntoIVE.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRevoked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RefreshTokenRevoked",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshTokenRevoked",
                table: "Users");
        }
    }
}
