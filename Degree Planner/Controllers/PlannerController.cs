using Degree_Planner.Models;
using Degree_Planner.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
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

        public IActionResult UploadCoursesTaken() {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }

            return View(new Course());
        }

        [HttpPost]
        public JsonResult AddCourseTaken(string department, string catalog) {
            if(!Authenticate()) {
                return Json("");
            }
            User user = GetCurrentlyLoggedInUser();

            using(var context = new DegreePlannerContext()) {
                bool? hasCourseAlready = context.Users
                    .Include(u => u.CourseUserLinks)
                    .ThenInclude(cul => cul.Course)
                    .FirstOrDefault(u => u.UserID == user.UserID)
                    ?.Courses
                    .Any(c => c.Department == department && c.CatalogNumber == catalog);

                if(hasCourseAlready != null && !hasCourseAlready.Value) {
                    Course course = CreateOrFetchCourse(context, department, catalog, out bool generated);

                    CourseUserLink link = new CourseUserLink() {
                        User = user,
                        Course = course
                    };

                    context.CourseUserLinks.Add(link);
                    context.SaveChanges();
                }
            }

            return GetCoursesTaken();
        }

        [HttpPost]
        public JsonResult GetCoursesTaken() {
            if(!Authenticate()) {
                return Json("");
            }
            User user = GetCurrentlyLoggedInUser();

            using(var context = new DegreePlannerContext()) {
                var courses = context.Users
                     .Include(c => c.CourseUserLinks)
                     .ThenInclude(cul => cul.Course)
                     .FirstOrDefault(u => u.UserID == user.UserID)
                     .Courses
                     .OrderBy(c => c.Department)
                     .ThenBy(c => c.CatalogNumber)
                     .Select(c => new { c.Department, c.CatalogNumber })
                     .ToList();

                return Json(courses);
            }
        }

        public IActionResult UploadDegree() {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            return View(new FileUploadVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UploadDegree(FileUploadVm vm) {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            if(!ModelState.IsValid) {
                return View(vm);
            }

            Degree degree = new Degree {
                Name = vm.Name,
                Requirements = new List<DegreeElement>()
            };
            using(var context = new DegreePlannerContext())
            using(var stream = vm.File.OpenReadStream())
            using(var reader = new StreamReader(stream)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    string[] data = line.Split(',');

                    DegreeElement element = new DegreeElement();
                    element.Hours = int.Parse(data[0]);
                    string courseGroupName = data[1];
                    CourseGroup courseGroup = context.CourseGroups.FirstOrDefault(cg => cg.Name == courseGroupName);
                    if(data.Length == 2) {
                        if(courseGroup == null) {
                            courseGroup = new CourseGroup() {
                                Name = courseGroupName
                            };
                        }
                    } else {
                        if(courseGroup == null) {
                            courseGroup = new CourseGroup() {
                                Name = courseGroupName,
                                CourseCourseGroupLinks = new List<CourseCourseGroupLink>()
                            };

                            for(int i = 2; i < data.Length; i++) {
                                CourseCourseGroupLink link = new CourseCourseGroupLink();
                                string department = data[i].Substring(0, 4);
                                string number = data[i].Substring(4);
                                Course course = CreateOrFetchCourse(context, department, number, out bool gen);

                                link.Course = course;
                                courseGroup.CourseCourseGroupLinks.Add(link);
                            }
                        }
                    }
                    element.Members = courseGroup;
                    degree.Requirements.Add(element);
                }

                context.Degrees.Add(degree);
                context.SaveChanges();
            }

            return RedirectToAction("Index", "Planner");
        }

        public IActionResult UploadCourseGroup() {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            return View(new FileUploadVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UploadCourseGroup(FileUploadVm vm) {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            if(!ModelState.IsValid) {
                return View(vm);
            }

            using(var context = new DegreePlannerContext())
            using(var stream = vm.File.OpenReadStream())
            using(var reader = new StreamReader(stream)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    string[] data = line.Split(',');

                    string name = data[0];
                    CourseGroup courseGroup = new CourseGroup() {
                        Name = name,
                        CourseCourseGroupLinks = new List<CourseCourseGroupLink>()
                    };
                    for(int i = 1; i < data.Length; i++) {
                        CourseCourseGroupLink link = new CourseCourseGroupLink();
                        string department = data[i].Substring(0, 4);
                        string number = data[i].Substring(4);
                        Course course = CreateOrFetchCourse(context, department, number, out bool gen);

                        link.Course = course;

                        courseGroup.CourseCourseGroupLinks.Add(link);
                    }

                    context.CourseGroups.Add(courseGroup);
                }
                context.SaveChanges();
            }

            return RedirectToAction("Index", "Planner");
        }

        public IActionResult UploadCourseList() {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            return View(new FileUploadVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UploadCourseList(FileUploadVm vm) {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            if(!ModelState.IsValid) {
                return View(vm);
            }

            using(var context = new DegreePlannerContext())
            using(var stream = vm.File.OpenReadStream())
            using(var reader = new StreamReader(stream)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    string[] data = line.Split(',');

                    string department = data[0].Substring(0, 4);
                    string catalogNumber = data[0].Substring(4);

                    Course course = CreateOrFetchCourse(context, department, catalogNumber, out bool gen);
                    if(gen) {
                        course.PrerequisiteLinks = new List<PrerequisiteLink>();
                    }
                    course.Name = data[1];

                    for(int i = 2; i < data.Length; i++) {
                        string prereqDepartment = data[i].Substring(0, 4);
                        string prereqCatalog = data[i].Substring(4);

                        Course prereq = CreateOrFetchCourse(context, prereqDepartment, prereqCatalog, out bool genPrereq);
                        if(gen) {
                            prereq = new Course() {
                                Department = prereqDepartment,
                                CatalogNumber = prereqCatalog
                            };
                            context.Courses.Add(prereq);
                        }

                        if(!course.Prerequisites.Any(p => p.Department == prereqDepartment && p.CatalogNumber == prereqCatalog)) {
                            PrerequisiteLink link = new PrerequisiteLink() {
                                Prerequisite = prereq,
                                Course = course
                            };

                            course.PrerequisiteLinks.Add(link);
                        }
                    }

                    if(gen) {
                        context.Courses.Add(course);
                    } else {
                        context.Courses.Update(course);
                    }
                }

                context.SaveChanges();
            }
            return RedirectToAction("Index", "Planner");
        }

        public IActionResult AdministerUsers() {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return RedirectToAction("Index", "Planner");
            }

            using(var context = new DegreePlannerContext()) {
                List<User> users = context.Users.Where(u => u.UserID != user.UserID).ToList();

                return View(users);
            }
        }

        [HttpPost]
        public JsonResult ToggleAdmin(int userID, bool isAdmin) {
            if(!Authenticate()) {
                return Json(false);
            }
            User user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin) {
                return Json(false);
            }

            using(var context = new DegreePlannerContext()) {
                User toEdit = context.Users.FirstOrDefault(u => u.UserID == userID);
                toEdit.IsAdmin = isAdmin;
                context.SaveChanges();

                return Json(true);
            }
        }

        private Course CreateOrFetchCourse(DegreePlannerContext context, string department, string catalog, out bool generated) {
            Course course = context.Courses.FirstOrDefault(c => c.Department == department && c.CatalogNumber == catalog);
            generated = course == null;
            if(course != null) {
                return course;
            }

            return new Course() {
                Department = department,
                CatalogNumber = catalog
            };
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