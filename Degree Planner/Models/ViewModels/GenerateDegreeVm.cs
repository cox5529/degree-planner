﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Degree_Planner.Models.ViewModels {
    public class GenerateDegreeVm {

        public User User { get; set; }

        public IList<SelectListItem> Options { get; set; }

        [Required(ErrorMessage = "You must select a degree."), Display(Name = "Degree")]
        public int? DegreeID { get; set; }

    }
}
