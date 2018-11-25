using System.Collections;
using System.Collections.Generic;

namespace Degree_Planner.Models.ViewModels {
    public class SelectCoursesVm {
        public int IdPrefix { get; set; }
        public DegreeElement DegreeElement { get; set; }
        public IList<int> CoursesTaken { get; set; }
    }
}
