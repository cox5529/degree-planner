using System.ComponentModel.DataAnnotations;

namespace Degree_Planner.Models.ViewModels {
    public class UserLoginVm {

        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required, Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
        public bool AGREE {get; set; }
        public string Message { get; set; }

    }
}
