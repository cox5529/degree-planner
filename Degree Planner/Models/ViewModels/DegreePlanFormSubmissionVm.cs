using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Degree_Planner.Models.ViewModels
{
    public class DegreePlanFormSubmissionVm {

		public int DegreeID { get; set; }

		public int MaxHoursPerSemester { get; set; }

		public IList<DegreePlanDegreeElementVm> DegreeElements { get; set; }

    }

	public class DegreePlanDegreeElementVm {

		public int DegreeElementID { get; set; }

		public IList<CourseData> Courses { get; set; }

	}

	public class CourseData {

		public int CourseID { get; set; }

		public string Department { get; set; }

		public string CatalogNumber { get; set; }

		public string Name { get; set; }
	}

	public class DegreePlanData {

		public IList<SemesterData> Semesters { get; set; }

		public DegreePlanData DeepClone() {
			DegreePlanData data = new DegreePlanData() {
				Semesters = new List<SemesterData>()
			};
			foreach (var semester in Semesters) {
				var s = new SemesterData() {
					CourseIDs = new HashSet<int>()
				};
				foreach (var id in semester.CourseIDs) {
					s.CourseIDs.Add(id);
				}
				data.Semesters.Add(s);
			}

			return data;
		}

	}

	public class SemesterData {

		public HashSet<int> CourseIDs { get; set; }

	}
}
