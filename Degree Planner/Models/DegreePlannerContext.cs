using Degree_Planner.Constants;
using Microsoft.EntityFrameworkCore;

namespace Degree_Planner.Models {
    public class DegreePlannerContext : DbContext {

        public DbSet<Course> Courses { get; set; }

        public DegreePlannerContext() : base() { }
        public DegreePlannerContext(DbContextOptions<DegreePlannerContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseMySql(ConnectionStrings.DEGREE_PLANNER);
        }
    }

    public class Course {
        public long CourseID { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int CatalogNumber { get; set; }
    }
}
