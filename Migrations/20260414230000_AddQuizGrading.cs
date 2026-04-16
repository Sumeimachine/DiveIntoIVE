using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiveIntoIVE.Migrations
{
    public partial class AddQuizGrading : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGraded",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGraded",
                table: "Quizzes");
        }
    }
}
