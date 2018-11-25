using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Degree_Planner.Models.ViewModels {
    public class GenerateDegreeVm {

        public User User { get; set; }

        public IList<SelectListItem> Options { get; set; }

        public IList<DegreePlan> Results { get; set; }

        [Required]
        public int DegreeID { get; set; }

    }
}
