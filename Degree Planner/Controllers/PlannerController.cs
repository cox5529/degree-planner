using Degree_Planner.Models;
using Degree_Planner.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public IActionResult GenerateSchedules() {
            if(!Authenticate()) {
                return Json("");
            }

            User user = GetCurrentlyLoggedInUser();

            GenerateDegreeVm vm = new GenerateDegreeVm();
            using(var context = new DegreePlannerContext()) {
                vm.Options = context.Degrees
                    .Select(d => new SelectListItem() {
                        Text = d.Name,
                        Value = d.DegreeID + ""
                    }).ToList();
                vm.User = user;
            }

            return View(vm);
        }

        public IActionResult SelectCourses(int degreeId) {
            if(!Authenticate()) {
                return RedirectToAction("Index", "Home");
            }
            User user = GetCurrentlyLoggedInUser();

            IList<SelectCoursesVm> vm;
            using(var context = new DegreePlannerContext()) {
                var degree = context.Degrees
                    .Include(d => d.Requirements)
                    .ThenInclude(de => de.Members)
                    .ThenInclude(cg => cg.CourseCourseGroupLinks)
                    .ThenInclude(ccgl => ccgl.Course)
                    .ThenInclude(c => c.CourseUserLinks)
                    .FirstOrDefault(d => d.DegreeID == degreeId);

                if(degree == null) {
                    return RedirectToAction("Index", "Planner");
                }

                vm = degree.Requirements
                    .Where(de => de.Members.Courses.Sum(c => c.Hours) != de.Hours)
                    .Select(de => new SelectCoursesVm() {
                        CoursesTaken = de.Members.Courses
                            .Where(c => c.CourseUserLinks.Any(cul => cul.UserID == user.UserID))
                            .Select(c => c.CourseID)
                            .ToList(),
                        DegreeElement = de
                    })
                    .ToList();

                int prefix = 0;
                foreach (var list in vm) {
                    list.IdPrefix = prefix;
                    prefix++;
                }
            }

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult GenerateSchedules(GenerateDegreeVm vm) {
            if(!Authenticate()) {
                return Json("");
            }
            User user = GetCurrentlyLoggedInUser();

            if(ModelState.IsValid) {
                return RedirectToAction("SelectCourses", "Planner", new {degreeId = vm.DegreeID});
            }
            return View(vm);
        }

        [HttpPost]
        public JsonResult AddCourseTaken(string department, string catalog) {
            if(!Authenticate()) {
                return Json("");
            }
            User user = GetCurrentlyLoggedInUser();

            if(string.IsNullOrEmpty(department) || string.IsNullOrEmpty(catalog)) {
                return Json("false");
            }

            using(var context = new DegreePlannerContext()) {
                bool? hasCourseAlready = context.Users
                    .Include(u => u.CourseUserLinks)
                    .ThenInclude(cul => cul.Course)
                    .FirstOrDefault(u => u.UserID == user.UserID)
                    ?.Courses
                    .Any(c => c.Department == department && c.CatalogNumber == catalog);

                if(hasCourseAlready == null || hasCourseAlready.Value)
                    return GetCoursesTaken();
                var course = CreateOrFetchCourse(context, department, catalog, out var generated);

                if(!generated) {
                    var link = new CourseUserLink() {
                        User = user,
                        Course = course
                    };

                    context.CourseUserLinks.Add(link);
                    context.SaveChanges();
                } else {
                    return Json("false");
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

                    Course course = CreateOrFetchCourse(context, department, catalogNumber, out bool gen, true);
                    int start = 1;
                    string name = data[1];
                    if(name.Length < 5 || !char.IsDigit(name[4])) {
                        start = 2;
                        course.Name = name;
                    }

                    for(int i = start; i < data.Length; i++) {
                        string prereqDepartment = data[i].Substring(0, 4);
                        string prereqCatalog = data[i].Substring(4);

                        Course prereq = CreateOrFetchCourse(context, prereqDepartment, prereqCatalog, out bool genPrereq, true);
                        if(genPrereq || gen) {
                            PrerequisiteLink link = new PrerequisiteLink() {
                                Prerequisite = prereq,
                                Course = course
                            };

                            context.PrerequisiteLinks.Add(link);
                            if(genPrereq) {
                                context.Courses.Add(prereq);
                            }
                        } else if(!context.PrerequisiteLinks.Any(p => p.CourseID == course.CourseID && p.PrerequisiteID == prereq.CourseID)) {
                            PrerequisiteLink link = new PrerequisiteLink() {
                                Prerequisite = prereq,
                                Course = course
                            };

                            context.PrerequisiteLinks.Add(link);
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

        private Course CreateOrFetchCourse(DegreePlannerContext context, string department, string catalog, out bool generated, bool includePrerequisite = false) {
            if(string.IsNullOrEmpty(department) || string.IsNullOrEmpty(catalog)) {
                generated = false;
                return null;
            }

            department = department.ToUpper();
            catalog = catalog.ToUpper();
            Course course;

            if(includePrerequisite) {
                course = context.Courses
                    .Include(c => c.PrerequisiteLinks)
                    .FirstOrDefault(c => c.Department == department && c.CatalogNumber == catalog);
            } else {
                course = context.Courses.FirstOrDefault(c => c.Department == department && c.CatalogNumber == catalog);
            }
            generated = course == null;
            if(course != null) {
                return course;
            }

            bool variable = !int.TryParse(catalog[3] + "", out int hours);
            if (variable) {
                hours = -1;
            }

            return new Course() {
                Department = department,
                CatalogNumber = catalog,
                PrerequisiteLinks = new List<PrerequisiteLink>(),
                Hours = hours
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