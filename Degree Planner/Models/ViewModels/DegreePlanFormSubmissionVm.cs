using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Degree_Planner.Models.ViewModels
{
    public class DegreePlanFormSubmissionVm {

		public int DegreeID { get; set; }

        public int MinHoursPerSemester { get; set; }

		public int MaxHoursPerSemester { get; set; }

        public int MinSemesters { get; set; }

		public IList<CourseData> Courses { get; set; }

    }

	public class CourseData {

		public int CourseID { get; set; }

		public string Department { get; set; }

		public string CatalogNumber { get; set; }

		public int Hours { get; set; }
	}
}
