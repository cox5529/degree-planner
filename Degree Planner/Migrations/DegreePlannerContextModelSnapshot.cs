﻿// <auto-generated />
using System;
using Degree_Planner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DegreePlanner.Migrations
{
    [DbContext(typeof(DegreePlannerContext))]
    partial class DegreePlannerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Degree_Planner.Models.Course", b =>
                {
                    b.Property<int>("CourseID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CatalogNumber");

                    b.Property<string>("Department");

                    b.Property<int>("Hours");

                    b.Property<string>("Name");

                    b.HasKey("CourseID");

                    b.ToTable("courses");
                });

            modelBuilder.Entity("Degree_Planner.Models.CourseCourseGroupLink", b =>
                {
                    b.Property<int>("CourseCourseGroupLinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CourseGroupID");

                    b.Property<int>("CourseID");

                    b.HasKey("CourseCourseGroupLinkID");

                    b.HasIndex("CourseGroupID");

                    b.HasIndex("CourseID");

                    b.ToTable("courses_x_course_groups");
                });

            modelBuilder.Entity("Degree_Planner.Models.CourseGroup", b =>
                {
                    b.Property<int>("CourseGroupID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("CourseGroupID");

                    b.ToTable("course_groups");
                });

            modelBuilder.Entity("Degree_Planner.Models.CourseUserLink", b =>
                {
                    b.Property<int>("CourseUserLinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CourseID");

                    b.Property<int>("UserID");

                    b.HasKey("CourseUserLinkID");

                    b.HasIndex("CourseID");

                    b.HasIndex("UserID");

                    b.ToTable("courses_x_users");
                });

            modelBuilder.Entity("Degree_Planner.Models.Degree", b =>
                {
                    b.Property<int>("DegreeID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("DegreeID");

                    b.ToTable("degrees");
                });

            modelBuilder.Entity("Degree_Planner.Models.DegreeElement", b =>
                {
                    b.Property<int>("DegreeElementID")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("CanOverlap");

                    b.Property<int>("CourseGroupID");

                    b.Property<int>("DegreeID");

                    b.Property<int>("Hours");

                    b.HasKey("DegreeElementID");

                    b.HasIndex("CourseGroupID");

                    b.HasIndex("DegreeID");

                    b.ToTable("degree_elements");
                });

            modelBuilder.Entity("Degree_Planner.Models.DegreePlan", b =>
                {
                    b.Property<int>("DegreePlanID")
                        .ValueGeneratedOnAdd();

                    b.HasKey("DegreePlanID");

                    b.ToTable("degree_plans");
                });

            modelBuilder.Entity("Degree_Planner.Models.PrerequisiteLink", b =>
                {
                    b.Property<int>("PrerequisiteLinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CourseID");

                    b.Property<int>("PrerequisiteID");

                    b.HasKey("PrerequisiteLinkID");

                    b.HasIndex("CourseID");

                    b.HasIndex("PrerequisiteID");

                    b.ToTable("prerequisites");
                });

            modelBuilder.Entity("Degree_Planner.Models.Semester", b =>
                {
                    b.Property<int>("SemesterID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DegreePlanID");

                    b.Property<int>("Index");

                    b.HasKey("SemesterID");

                    b.HasIndex("DegreePlanID");

                    b.ToTable("semesters");
                });

            modelBuilder.Entity("Degree_Planner.Models.SemesterCourseLink", b =>
                {
                    b.Property<int>("SemesterCourseLinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CourseID");

                    b.Property<int>("SemesterID");

                    b.HasKey("SemesterCourseLinkID");

                    b.HasIndex("CourseID");

                    b.HasIndex("SemesterID");

                    b.ToTable("semesters_x_courses");
                });

            modelBuilder.Entity("Degree_Planner.Models.User", b =>
                {
                    b.Property<int>("UserID")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("DegreePlanID");

                    b.Property<bool>("IsAdmin");

                    b.Property<string>("Password");

                    b.Property<string>("Username");

                    b.HasKey("UserID");

                    b.HasIndex("DegreePlanID");

                    b.ToTable("users");
                });

            modelBuilder.Entity("Degree_Planner.Models.UserDegreeLink", b =>
                {
                    b.Property<int>("UserDegreeLinkID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DegreeID");

                    b.Property<int>("UserID");

                    b.HasKey("UserDegreeLinkID");

                    b.HasIndex("DegreeID");

                    b.HasIndex("UserID");

                    b.ToTable("users_x_degrees");
                });

            modelBuilder.Entity("Degree_Planner.Models.CourseCourseGroupLink", b =>
                {
                    b.HasOne("Degree_Planner.Models.CourseGroup", "CourseGroup")
                        .WithMany("CourseCourseGroupLinks")
                        .HasForeignKey("CourseGroupID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Degree_Planner.Models.Course", "Course")
                        .WithMany("CourseCourseGroupLinks")
                        .HasForeignKey("CourseID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Degree_Planner.Models.CourseUserLink", b =>
                {
                    b.HasOne("Degree_Planner.Models.Course", "Course")
                        .WithMany("CourseUserLinks")
                        .HasForeignKey("CourseID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Degree_Planner.Models.User", "User")
                        .WithMany("CourseUserLinks")
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Degree_Planner.Models.DegreeElement", b =>
                {
                    b.HasOne("Degree_Planner.Models.CourseGroup", "Members")
                        .WithMany("DegreeElements")
                        .HasForeignKey("CourseGroupID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Degree_Planner.Models.Degree", "Degree")
                        .WithMany("Requirements")
                        .HasForeignKey("DegreeID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Degree_Planner.Models.PrerequisiteLink", b =>
                {
                    b.HasOne("Degree_Planner.Models.Course", "Course")
                        .WithMany("PostrequisiteLinks")
                        .HasForeignKey("CourseID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Degree_Planner.Models.Course", "Prerequisite")
                        .WithMany("PrerequisiteLinks")
                        .HasForeignKey("PrerequisiteID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Degree_Planner.Models.Semester", b =>
                {
                    b.HasOne("Degree_Planner.Models.DegreePlan", "DegreePlan")
                        .WithMany("Semesters")
                        .HasForeignKey("DegreePlanID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Degree_Planner.Models.SemesterCourseLink", b =>
                {
                    b.HasOne("Degree_Planner.Models.Course", "Course")
                        .WithMany("SemesterCourseLinks")
                        .HasForeignKey("CourseID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Degree_Planner.Models.Semester", "Semester")
                        .WithMany("SemesterCourseLinks")
                        .HasForeignKey("SemesterID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Degree_Planner.Models.User", b =>
                {
                    b.HasOne("Degree_Planner.Models.DegreePlan", "DegreePlan")
                        .WithMany()
                        .HasForeignKey("DegreePlanID");
                });

            modelBuilder.Entity("Degree_Planner.Models.UserDegreeLink", b =>
                {
                    b.HasOne("Degree_Planner.Models.Degree", "Degree")
                        .WithMany("UserDegreeLinks")
                        .HasForeignKey("DegreeID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Degree_Planner.Models.User", "User")
                        .WithMany("UserDegreeLinks")
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
