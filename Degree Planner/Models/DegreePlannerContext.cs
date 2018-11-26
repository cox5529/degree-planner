using Degree_Planner.Constants;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace Degree_Planner.Models {
    public class DegreePlannerContext : DbContext {

        public DbSet<Course> Courses { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<DegreePlan> DegreePlans { get; set; }
        public DbSet<Degree> Degrees { get; set; }
        public DbSet<DegreeElement> DegreeElements { get; set; }
        public DbSet<CourseGroup> CourseGroups { get; set; }
        public DbSet<UserDegreeLink> UserDegreeLinks { get; set; }
        public DbSet<PrerequisiteLink> PrerequisiteLinks { get; set; }
        public DbSet<CourseCourseGroupLink> CourseCourseGroupLinks { get; set; }
        public DbSet<SemesterCourseLink> SemesterCourseLinks { get; set; }
        public DbSet<CourseUserLink> CourseUserLinks { get; set; }

        public DegreePlannerContext() : base() { }
        public DegreePlannerContext(DbContextOptions<DegreePlannerContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseMySql(ConnectionStrings.PRODUCTION);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.PrerequisiteLinks)
                .WithOne(pl => pl.Course)
                .HasForeignKey(pl => pl.CourseID);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.PostrequisiteLinks)
                .WithOne(pl => pl.Prerequisite)
                .HasForeignKey(pl => pl.PrerequisiteID);
        }
    }

    [Table("degree_plans")]
    public class DegreePlan {
        public int DegreePlanID { get; set; }
        public virtual ICollection<Semester> Semesters { get; set; }
    }

    [Table("semesters")]
    public class Semester {
        public int SemesterID { get; set; }
        public int DegreePlanID { get; set; }
        public int Index { get; set; }

        public virtual ICollection<SemesterCourseLink> SemesterCourseLinks { get; set; }

        [ForeignKey("DegreePlanID")]
        public virtual DegreePlan DegreePlan { get; set; }
        [NotMapped]
        public virtual IEnumerable<Course> Courses => SemesterCourseLinks.Select(scl => scl.Course);
    }

    [Table("users")]
    public class User {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }

        public int? DegreePlanID { get; set; }

        public ICollection<UserDegreeLink> UserDegreeLinks { get; set; }
        public ICollection<CourseUserLink> CourseUserLinks { get; set; }

        [ForeignKey("DegreePlanID")]
        public DegreePlan DegreePlan { get; set; }
        [NotMapped]
        public virtual IEnumerable<Course> Courses => CourseUserLinks.Select(cul => cul.Course);
        [NotMapped]
        public virtual IEnumerable<Degree> Degrees => UserDegreeLinks.Select(udl => udl.Degree);
    }

    [Table("degrees")]
    public class Degree {
        public int DegreeID { get; set; }
        public string Name { get; set; }

        public virtual ICollection<UserDegreeLink> UserDegreeLinks { get; set; }
        public virtual ICollection<DegreeElement> Requirements { get; set; }

        [NotMapped]
        public virtual IEnumerable<User> Users => UserDegreeLinks.Select(udl => udl.User);
    }

    [Table("degree_elements")]
    public class DegreeElement {
        public int DegreeElementID { get; set; }
        public int Hours { get; set; }
        public bool CanOverlap { get; set; }

        public int CourseGroupID { get; set; }
        public int DegreeID { get; set; }
        
        public virtual CourseGroup Members { get; set; }
        [JsonIgnore]
        public virtual Degree Degree { get; set; }
    }

    [Table("course_groups")]
    public class CourseGroup {
        public int CourseGroupID { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public virtual ICollection<CourseCourseGroupLink> CourseCourseGroupLinks { get; set; }
        [JsonIgnore]
        public virtual ICollection<DegreeElement> DegreeElements { get; set; }

        [NotMapped]
        public virtual IEnumerable<Course> Courses => CourseCourseGroupLinks.Select(ccgl => ccgl.Course);
    }

    [Table("courses")]
    public class Course {
        public int CourseID { get; set; }
        public int Hours { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string CatalogNumber { get; set; }

        [JsonIgnore]
        public virtual ICollection<PrerequisiteLink> PrerequisiteLinks { get; set; }
        [JsonIgnore]
        public virtual ICollection<PrerequisiteLink> PostrequisiteLinks { get; set; }
        [JsonIgnore]
        public virtual ICollection<CourseCourseGroupLink> CourseCourseGroupLinks { get; set; }
        [JsonIgnore]
        public virtual ICollection<SemesterCourseLink> SemesterCourseLinks { get; set; }
        [JsonIgnore]
        public virtual ICollection<CourseUserLink> CourseUserLinks { get; set; }

        [NotMapped, JsonIgnore]
        public virtual IEnumerable<User> Users => CourseUserLinks.Select(cul => cul.User);
        [NotMapped, JsonIgnore]
        public virtual IEnumerable<Semester> Semesters => SemesterCourseLinks.Select(scl => scl.Semester);
        [NotMapped, JsonIgnore]
        public virtual IEnumerable<CourseGroup> CourseGroups => CourseCourseGroupLinks.Select(ccgl => ccgl.CourseGroup);
        [NotMapped, JsonIgnore]
        public virtual IEnumerable<Course> Prerequisites => PrerequisiteLinks.Where(pl => pl.CourseID == CourseID).Select(pl => pl.Prerequisite);
        [NotMapped, JsonIgnore]
        public virtual IEnumerable<Course> Postrequisites => PostrequisiteLinks.Where(pl => pl.PrerequisiteID == CourseID).Select(pl => pl.Course);
    }

    #region many-to-many links

    [Table("users_x_degrees")]
    public class UserDegreeLink {
        public int UserDegreeLinkID { get; set; }
        
        public int DegreeID { get; set; }
        public int UserID { get; set; }
        
        public virtual Degree Degree { get; set; }
        public virtual User User { get; set; }
    }

    [Table("courses_x_users")]
    public class CourseUserLink {
        public int CourseUserLinkID { get; set; }

        public int CourseID { get; set; }
        public int UserID { get; set; }

        public virtual Course Course { get; set; }
        public virtual User User { get; set; }
    }

    [Table("semesters_x_courses")]
    public class SemesterCourseLink {
        public int SemesterCourseLinkID { get; set; }

        public int SemesterID { get; set; }
        public int CourseID { get; set; }

        public virtual Semester Semester { get; set; }
        public virtual Course Course { get; set; }
    }

    [Table("prerequisites")]
    public class PrerequisiteLink {
        public int PrerequisiteLinkID { get; set; }

        public int CourseID { get; set; }
        public int PrerequisiteID { get; set; }

        public virtual Course Course { get; set; }
        public virtual Course Prerequisite { get; set; }
    }

    [Table("courses_x_course_groups")]
    public class CourseCourseGroupLink {
        public int CourseCourseGroupLinkID { get; set; }

        public int CourseID { get; set; }
        public int CourseGroupID { get; set; }

        public virtual Course Course { get; set; }
        public virtual CourseGroup CourseGroup { get; set; }
    }

    #endregion
}
