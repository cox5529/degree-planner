using System.ComponentModel.DataAnnotations;

namespace Degree_Planner.Models.ViewModels {
    public class CreateAccountVm {

        [Required, StringLength(64, MinimumLength = 3)]
        public string Username { get; set; }
        [Required, StringLength(64, MinimumLength = 6)]
        public string Password { get; set; }
        [Required, Compare("Password", ErrorMessage = "You must re-enter your password correctly")]
        public string ConfirmPassword { get; set; }

    }
}
