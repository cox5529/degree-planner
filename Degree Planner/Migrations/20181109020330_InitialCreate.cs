using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DegreePlanner.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_groups",
                columns: table => new
                {
                    CourseGroupID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_groups", x => x.CourseGroupID);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    CourseID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Department = table.Column<string>(nullable: true),
                    CatalogNumber = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.CourseID);
                });

            migrationBuilder.CreateTable(
                name: "degree_plans",
                columns: table => new
                {
                    DegreePlanID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_degree_plans", x => x.DegreePlanID);
                });

            migrationBuilder.CreateTable(
                name: "degrees",
                columns: table => new
                {
                    DegreeID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_degrees", x => x.DegreeID);
                });

            migrationBuilder.CreateTable(
                name: "courses_x_course_groups",
                columns: table => new
                {
                    CourseCourseGroupLinkID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseID = table.Column<int>(nullable: false),
                    CourseGroupID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses_x_course_groups", x => x.CourseCourseGroupLinkID);
                    table.ForeignKey(
                        name: "FK_courses_x_course_groups_course_groups_CourseGroupID",
                        column: x => x.CourseGroupID,
                        principalTable: "course_groups",
                        principalColumn: "CourseGroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_courses_x_course_groups_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prerequisites",
                columns: table => new
                {
                    PrerequisiteLinkID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseID = table.Column<int>(nullable: false),
                    PrerequisiteID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prerequisites", x => x.PrerequisiteLinkID);
                    table.ForeignKey(
                        name: "FK_prerequisites_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prerequisites_courses_PrerequisiteID",
                        column: x => x.PrerequisiteID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "semesters",
                columns: table => new
                {
                    SemesterID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DegreePlanID = table.Column<int>(nullable: false),
                    Index = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semesters", x => x.SemesterID);
                    table.ForeignKey(
                        name: "FK_semesters_degree_plans_DegreePlanID",
                        column: x => x.DegreePlanID,
                        principalTable: "degree_plans",
                        principalColumn: "DegreePlanID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    UserID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    DegreePlanID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_users_degree_plans_DegreePlanID",
                        column: x => x.DegreePlanID,
                        principalTable: "degree_plans",
                        principalColumn: "DegreePlanID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "degree_elements",
                columns: table => new
                {
                    DegreeElementID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Hours = table.Column<int>(nullable: false),
                    CanOverlap = table.Column<bool>(nullable: false),
                    CourseGroupID = table.Column<int>(nullable: false),
                    DegreeID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_degree_elements", x => x.DegreeElementID);
                    table.ForeignKey(
                        name: "FK_degree_elements_course_groups_CourseGroupID",
                        column: x => x.CourseGroupID,
                        principalTable: "course_groups",
                        principalColumn: "CourseGroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_degree_elements_degrees_DegreeID",
                        column: x => x.DegreeID,
                        principalTable: "degrees",
                        principalColumn: "DegreeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "semesters_x_courses",
                columns: table => new
                {
                    SemesterCourseLinkID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SemesterID = table.Column<int>(nullable: false),
                    CourseID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semesters_x_courses", x => x.SemesterCourseLinkID);
                    table.ForeignKey(
                        name: "FK_semesters_x_courses_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_semesters_x_courses_semesters_SemesterID",
                        column: x => x.SemesterID,
                        principalTable: "semesters",
                        principalColumn: "SemesterID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "courses_x_users",
                columns: table => new
                {
                    CourseUserLinkID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseID = table.Column<int>(nullable: false),
                    UserID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses_x_users", x => x.CourseUserLinkID);
                    table.ForeignKey(
                        name: "FK_courses_x_users_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_courses_x_users_users_UserID",
                        column: x => x.UserID,
                        principalTable: "users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users_x_degrees",
                columns: table => new
                {
                    UserDegreeLinkID = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DegreeID = table.Column<int>(nullable: false),
                    UserID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_x_degrees", x => x.UserDegreeLinkID);
                    table.ForeignKey(
                        name: "FK_users_x_degrees_degrees_DegreeID",
                        column: x => x.DegreeID,
                        principalTable: "degrees",
                        principalColumn: "DegreeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_users_x_degrees_users_UserID",
                        column: x => x.UserID,
                        principalTable: "users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_courses_x_course_groups_CourseGroupID",
                table: "courses_x_course_groups",
                column: "CourseGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_courses_x_course_groups_CourseID",
                table: "courses_x_course_groups",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_courses_x_users_CourseID",
                table: "courses_x_users",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_courses_x_users_UserID",
                table: "courses_x_users",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_degree_elements_CourseGroupID",
                table: "degree_elements",
                column: "CourseGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_degree_elements_DegreeID",
                table: "degree_elements",
                column: "DegreeID");

            migrationBuilder.CreateIndex(
                name: "IX_prerequisites_CourseID",
                table: "prerequisites",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_prerequisites_PrerequisiteID",
                table: "prerequisites",
                column: "PrerequisiteID");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_DegreePlanID",
                table: "semesters",
                column: "DegreePlanID");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_x_courses_CourseID",
                table: "semesters_x_courses",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_x_courses_SemesterID",
                table: "semesters_x_courses",
                column: "SemesterID");

            migrationBuilder.CreateIndex(
                name: "IX_users_DegreePlanID",
                table: "users",
                column: "DegreePlanID");

            migrationBuilder.CreateIndex(
                name: "IX_users_x_degrees_DegreeID",
                table: "users_x_degrees",
                column: "DegreeID");

            migrationBuilder.CreateIndex(
                name: "IX_users_x_degrees_UserID",
                table: "users_x_degrees",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "courses_x_course_groups");

            migrationBuilder.DropTable(
                name: "courses_x_users");

            migrationBuilder.DropTable(
                name: "degree_elements");

            migrationBuilder.DropTable(
                name: "prerequisites");

            migrationBuilder.DropTable(
                name: "semesters_x_courses");

            migrationBuilder.DropTable(
                name: "users_x_degrees");

            migrationBuilder.DropTable(
                name: "course_groups");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "semesters");

            migrationBuilder.DropTable(
                name: "degrees");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "degree_plans");
        }
    }
}
