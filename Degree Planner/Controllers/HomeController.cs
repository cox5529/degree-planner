using Degree_Planner.Models;
using Degree_Planner.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Degree_Planner.Controllers {

    public class HomeController : Controller {

        public const string USERNAME = "username";

	    public ISession Session => HttpContext.Session;

	    public IActionResult Index() {
            return View(new UserLoginVm());
        }

        [HttpPost]
        public IActionResult Index(UserLoginVm vm) {
            if(ModelState.IsValid) {
                vm.Password = GetPasswordHash(vm.Password);

                using(var context = new DegreePlannerContext()) {
                    User user = context.Users.FirstOrDefault(u => u.Username == vm.Username && u.Password == vm.Password);
                    if(user != null) {
                        HttpContext.Session.SetInt32(USERNAME, user.UserID);
                        return RedirectToAction("Index", "Planner");
                    } else {
                        vm.Message = "Incorrect username or password";
                    }
                }
            }
            return View(vm);
        }

        public IActionResult CreateAccount() {
            return View(new CreateAccountVm());
        }

        [HttpPost]
        public IActionResult CreateAccount(CreateAccountVm vm) {
            if(ModelState.IsValid) {
                User user = new User() {
                    Username = vm.Username,
                    Password = GetPasswordHash(vm.Password)
                };

                using(var context = new DegreePlannerContext()) {
                    if(context.Users.Any(u => u.Username == vm.Username)) {
                        ModelState.AddModelError("Username", "Username already exists");
                        return View(vm);
                    }

                    context.Users.Add(user);
                    context.SaveChanges();

                    user = context.Users.FirstOrDefault(u => u.Username == user.Username && u.Password == user.Password);
                }
                HttpContext.Session.SetInt32(USERNAME, user.UserID);
                return RedirectToAction("Index", "Planner");
            }
            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetPasswordHash(string input) {
            input = "saltysalt." + input + ".saltysalt";

            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach(var b in hash) {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
