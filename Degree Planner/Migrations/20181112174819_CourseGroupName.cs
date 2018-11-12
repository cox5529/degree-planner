using Microsoft.EntityFrameworkCore.Migrations;

namespace DegreePlanner.Migrations
{
    public partial class CourseGroupName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "course_groups",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "course_groups");
        }
    }
}
