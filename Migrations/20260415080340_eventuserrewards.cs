using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiveIntoIVE.Migrations
{
    /// <inheritdoc />
    public partial class eventuserrewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserEventRewardClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventRewardId = table.Column<int>(type: "int", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventRewardClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventRewardClaims_EventRewards_EventRewardId",
                        column: x => x.EventRewardId,
                        principalTable: "EventRewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventRewardClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEventRewardClaims_EventRewardId",
                table: "UserEventRewardClaims",
                column: "EventRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventRewardClaims_UserId_EventRewardId",
                table: "UserEventRewardClaims",
                columns: new[] { "UserId", "EventRewardId" },
                unique: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEventRewardClaims");

            migrationBuilder.DropTable(
                name: "EventRewards");
        }
    }
}
