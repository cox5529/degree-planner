using Microsoft.EntityFrameworkCore.Migrations;

namespace DegreePlanner.Migrations
{
    public partial class DegreePlanParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxHours",
                table: "degree_plans",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinHours",
                table: "degree_plans",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinSemesters",
                table: "degree_plans",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxHours",
                table: "degree_plans");

            migrationBuilder.DropColumn(
                name: "MinHours",
                table: "degree_plans");

            migrationBuilder.DropColumn(
                name: "MinSemesters",
                table: "degree_plans");
        }
    }
}
