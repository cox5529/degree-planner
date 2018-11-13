using Degree_Planner.Models;
using Degree_Planner.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                                Course course = context.Courses.FirstOrDefault(c => c.Department == department && c.CatalogNumber == number);

                                if(course == null) {
                                    course = new Course() {
                                        Department = department,
                                        CatalogNumber = number
                                    };
                                }
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
                        Course course = context.Courses.FirstOrDefault(c => c.Department == department && c.CatalogNumber == number);

                        if(course == null) {
                            course = new Course() {
                                Department = department,
                                CatalogNumber = number
                            };
                        }
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

                    Course course = context.Courses.FirstOrDefault(c => c.Department == department && c.CatalogNumber == catalogNumber);
                    bool exists = (course == null);
                    if(!exists) {
                        course = new Course() {
                            Department = department,
                            CatalogNumber = catalogNumber,
                            PrerequisiteLinks = new List<PrerequisiteLink>()
                        };
                    }
                    course.Name = data[1];

                    for(int i = 2; i < data.Length; i++) {
                        string prereqDepartment = data[i].Substring(0, 4);
                        string prereqCatalog = data[i].Substring(4);

                        Course prereq = context.Courses.FirstOrDefault(c => c.Department == prereqDepartment && c.CatalogNumber == prereqCatalog);
                        if(prereq != null) {
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

                    if(!exists) {
                        context.Courses.Add(course);
                    } else {
                        context.Courses.Update(course);
                    }
                }

                context.SaveChanges();
            }
            return RedirectToAction("Index", "Planner");
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