using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Degree_Planner.Models.ViewModels {
    public class FileUploadVm {

        public string Name { get; set; }

        [Required]
        public IFormFile File { get; set; }

    }
}
