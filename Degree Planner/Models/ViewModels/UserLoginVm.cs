using System.ComponentModel.DataAnnotations;

namespace Degree_Planner.Models.ViewModels {
    public class UserLoginVm {

        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        public string Message { get; set; }

    }
}
