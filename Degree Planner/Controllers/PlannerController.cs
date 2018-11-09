using Degree_Planner.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Degree_Planner.Controllers {
    public class PlannerController : Controller {


        public IActionResult Index() {
            // If user is not logged in, send the user back to the main landing page
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            // Otherwise, display a page
            User user = GetCurrentlyLoggedInUser();
            return View(user);
        }

        private User GetCurrentlyLoggedInUser() {
            int? userId = HttpContext.Session.GetInt32(HomeController.USERNAME);
            if(userId == null) {
                return null;
            }
            using(var context = new DegreePlannerContext()) {
                return context.Users.FirstOrDefault(u => u.UserID == userId);
            }
        }

        private bool Authenticate() {
            int? userId = HttpContext.Session.GetInt32(HomeController.USERNAME);
            if(userId == null) {
                return false;
            }
            using(var context = new DegreePlannerContext()) {
                return context.Users.Any(u => u.UserID == userId);
            }
        }
    }
}