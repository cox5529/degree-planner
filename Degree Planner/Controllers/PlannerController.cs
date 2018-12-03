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
            if(!Authenticate())
                return RedirectToAction("Index", "Home");

            // Otherwise, display a page
            var user = GetCurrentlyLoggedInUser();
            using(var context = new DegreePlannerContext()) {
                user = context.Users.Include(u => u.DegreePlan).FirstOrDefault(u => u.UserID == user.UserID);
            }

            if(user.IsAdmin) {
                return RedirectToAction("AdministerUsers", "Planner");
            }

            return RedirectToAction("ViewDegreePlan", "Planner");
        }

        public IActionResult UploadCoursesTaken() {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");

            return View(new Course());
        }

        public IActionResult GenerateSchedules() {
            if(!Authenticate())
                return Json("");

            var user = GetCurrentlyLoggedInUser();

            var vm = new GenerateDegreeVm();
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

        public IActionResult ViewDegreePlan() {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            User user = GetCurrentlyLoggedInUser();

            using(var context = new DegreePlannerContext()) {
                var plan = context.DegreePlans
                    .Include(dp => dp.Semesters)
                    .ThenInclude(s => s.SemesterCourseLinks)
                    .ThenInclude(scl => scl.Course)
                    .Include(dp => dp.User)
                    .FirstOrDefault(dp => dp.User.UserID == user.UserID);

                if(plan == null) {
                    return RedirectToAction("GenerateSchedules", "Planner");
                }

                foreach(var semester in plan.Semesters) {
                    semester.SemesterCourseLinks = semester.SemesterCourseLinks
                        .OrderBy(scl => scl.Course.Department)
                        .ThenBy(scl => scl.Course.CatalogNumber)
                        .ToList();
                }

                return View(plan);
            }
        }

        [HttpPost]
        public IActionResult ViewDegreePlan(DegreePlanFormSubmissionVm data) {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            User user = GetCurrentlyLoggedInUser();

            // only doing front end validation for now.
            //TODO fix later.
            using(var context = new DegreePlannerContext()) {
                //Figure out what courses i need to take to graduate
                IList<Course> coursesTaken = context.Users
                    .Include(u => u.CourseUserLinks)
                    .ThenInclude(cul => cul.Course)
                    .ThenInclude(c => c.PrerequisiteLinks)
                    .ThenInclude(pl => pl.Prerequisite)
                    .FirstOrDefault(u => u.UserID == user.UserID)
                    ?.Courses
                    .ToList();

                IList<Course> coursesToTake = new List<Course>();
                foreach(var courseData in data.Courses) {
                    if(coursesTaken.All(c => c.CourseID != courseData.CourseID) && coursesToTake.All(c => c.CourseID != courseData.CourseID)) {
                        var course = CreateOrFetchCourse(context, courseData.Department, courseData.CatalogNumber,
                            out _, true);
                        if(course.Hours == -1) {
                            course.Hours = courseData.Hours;
                        }
                        coursesToTake.Add(course);
                    }
                }

                var degreeRequiredCourses = context.Degrees
                    .Include(d => d.Requirements)
                    .ThenInclude(de => de.Members)
                    .ThenInclude(cg => cg.CourseCourseGroupLinks)
                    .ThenInclude(ccgl => ccgl.Course)
                    .ThenInclude(c => c.PrerequisiteLinks)
                    .ThenInclude(pl => pl.Prerequisite)
                    .FirstOrDefault(d => d.DegreeID == data.DegreeID)
                    ?.Requirements
                    .Where(de => de.Hours == de.Members.Courses.Sum(c => c.Hours))
                    .SelectMany(de => de.Members.Courses);

                foreach(var course in degreeRequiredCourses) {
                    if(coursesTaken.All(c => c.CourseID != course.CourseID) && coursesToTake.All(c => c.CourseID != course.CourseID)) {
                        coursesToTake.Add(course);
                    }
                }

                for(var i = 0; i < coursesToTake.Count; i++) {
                    foreach(var prereq in coursesToTake[i].Prerequisites) {
                        if(coursesTaken.All(c => c.CourseID != prereq.CourseID) &&
                            coursesToTake.All(c => c.CourseID != prereq.CourseID)) {
                            Course fullPrereqData = context.Courses
                                .Include(c => c.PrerequisiteLinks)
                                .ThenInclude(pl => pl.Prerequisite)
                                .FirstOrDefault(p => p.CourseID == prereq.CourseID);
                            coursesToTake.Add(fullPrereqData);
                        }
                    }
                }

                //Build an adjacency matrix
                int n = coursesToTake.Count;
                bool[,] matrix = new bool[n, n];
                for(var i = 0; i < n; i++) {
                    for(var j = 0; j < n; j++) {
                        if(coursesToTake[i].Prerequisites.Any(c => c.CourseID == coursesToTake[j].CourseID)) {
                            matrix[i, j] = true;
                        } else {
                            matrix[i, j] = false; // matrix[i, j] = true IF course j is a prerequisite of course i
                        }
                    }
                }

                int[] hours = new int[n];
                int[] years = new int[n];
                for(int i = 0; i < n; i++) {
                    hours[i] = coursesToTake[i].Hours;
                    years[i] = int.Parse(coursesToTake[i].CatalogNumber[0] + "");
                }

                // Use topological sort to show the minimum semester for a course
                var planData = GeneratePlan(matrix, hours, years, data.MaxHoursPerSemester, data.MinHoursPerSemester, data.MinSemesters, n);

                // convert the graph data to actual courses
                DegreePlan plan = new DegreePlan() {
                    Semesters = new List<Semester>(),
                    MinHours = data.MinHoursPerSemester,
                    MinSemesters = data.MinSemesters,
                    MaxHours = data.MaxHoursPerSemester
                };

                int index = 0;
                Course free = context.Courses.FirstOrDefault(c => c.Department == "UARK" && c.CatalogNumber == "000E");
                foreach(var semesterData in planData) {
                    var courses = new List<SemesterCourseLink>();
                    var semester = new Semester() {
                        Index = index
                    };
                    foreach(int courseData in semesterData) {
                        if(courseData != -1) {
                            courses.Add(new SemesterCourseLink() {
                                Course = coursesToTake[courseData],
                                Semester = semester
                            });
                        } else {
                            courses.Add(new SemesterCourseLink() {
                                Course = free,
                                Semester = semester
                            });
                        }
                    }

                    index++;

                    semester.SemesterCourseLinks = courses;
                    plan.Semesters.Add(semester);
                }

                var oldPlan = context.DegreePlans.Where(dp => dp.UserID == user.UserID);
                if(oldPlan.Count() != 0) {
                    context.DegreePlans.RemoveRange(oldPlan);
                }
                context.SaveChanges();

                plan.UserID = user.UserID;
                context.DegreePlans.Add(plan);
                context.SaveChanges();

                return RedirectToAction("ViewDegreePlan", "Planner");
            }
        }

        private static IList<IList<int>> GeneratePlan(bool[,] matrix, int[] hours, int[] years, int maxHoursPerSemester, int minHoursPerSemester, int minSemesters, int n) {
            int[] levels = BuildLevels(matrix, n);
            int semesters = levels.Max() + 1;

            IList<IList<int>> plan = new List<IList<int>>();
            for(int i = 0; i < semesters; i++) {
                plan.Add(new List<int>());
                for(int j = 0; j < n; j++) {
                    if(levels[j] == i) {
                        plan[i].Add(j);
                    }
                }
            }
            // Courses are placed at the earliest semesters they can be taken.
            // Now, move them so the plan matches the given parameters
            // You will never be able to move anything to an earlier semester.
            for(int i = 0; i < plan.Count; i++) {
                int count = CountHours(plan[i], hours);
                List<int> toMove = new List<int>();
                while(count > maxHoursPerSemester) {
                    // pick a course to move. prefer the one with the shortest postreq chain
                    int shortest = 100;
                    int shortestIndex = 0;
                    for(int j = 0; j < plan[i].Count; j++) {
                        if(!toMove.Contains(plan[i][j])) {
                            int len = GetLongestTrail(matrix, n, plan[i][j]) + GetLongestPath(matrix, n, plan[i][j]);
                            if(len < shortest) {
                                shortest = len;
                                shortestIndex = j;
                            } else if(len == shortest && years[plan[i][shortestIndex]] < years[plan[i][j]]) {
                                shortest = len;
                                shortestIndex = j;
                            }
                        }
                    }
                    toMove.Add(plan[i][shortestIndex]);

                    count -= hours[plan[i][shortestIndex]];
                }
                if(toMove.Count > 0 && count < maxHoursPerSemester) {
                    for(int j = 0; j < toMove.Count; j++) {
                        if(hours[toMove[j]] + count <= maxHoursPerSemester) {
                            count += hours[toMove[j]];
                            toMove.RemoveAt(j);
                            j--;
                        }
                    }
                }
                foreach(int course in toMove) {
                    MoveTrailBack(plan, matrix, n, i, course);
                }
                if(count < minHoursPerSemester) {
                    // if there aren't enough hours in a semester, add a free slot.
                    plan[i].Add(-1);
                }
            }

            while(plan.Count < minSemesters) {
                plan.Add(new List<int>() { -1 });
            }

            return plan;
        }

        private static void MoveTrailBack(IList<IList<int>> plan, bool[,] matrix, int n, int semester, int course) {
            plan[semester].Remove(course);
            if(plan.Count <= semester + 1) {
                plan.Add(new List<int>());
            }
            plan[semester + 1].Add(course);

            // move all of the postrequisites of course back as well
            for(int i = 0; i < n; i++) {
                if(matrix[i, course]) {
                    MoveTrailBack(plan, matrix, n, semester + 1, i);
                }
            }
        }

        private static int CountHours(IList<int> semester, int[] hours) {
            int total = 0;
            foreach(int course in semester) {
                total += hours[course];
            }

            return total;
        }

        private static int[] BuildLevels(bool[,] matrix, int n) {
            int[] r = new int[n];
            for(int i = 0; i < n; i++) {
                r[i] = 0;
            }

            Queue<int> nodes = new Queue<int>();
            for(int i = 0; i < n; i++) {
                if(GetPrerequisites(matrix, i, n).Count == 0) {
                    nodes.Enqueue(i);
                }
            }

            while(nodes.Count > 0) {
                int node = nodes.Dequeue();

                for(int i = 0; i < n; i++) {
                    if(matrix[i, node]) {
                        if(r[node] + 1 > r[i]) {
                            r[i] = r[node] + 1;
                            nodes.Enqueue(i);
                        }
                    }
                }
            }

            return r;
        }

        private static IList<int> GetPrerequisites(bool[,] matrix, int i, int n) {
            IList<int> prereqs = new List<int>();
            for(int a = 0; a < n; a++) {
                if(matrix[i, a]) {
                    prereqs.Add(a);
                }
            }
            return prereqs;
        }

        /// <summary>
        /// Determines the longest "postrequisite" trail from this course
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="n"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static int GetLongestTrail(bool[,] matrix, int n, int i) {
            int max = 0;
            for(int a = 0; a < n; a++) {
                if(matrix[a, i]) {
                    int val = GetLongestTrail(matrix, n, a);
                    if(val > max) {
                        max = val;
                    }
                }
            }
            return max + 1;
        }

        /// <summary>
        /// Determines the longest prerequisite path to this course
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="n"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static int GetLongestPath(bool[,] matrix, int n, int i) {
            int max = 0;
            for(int a = 0; a < n; a++) {
                if(matrix[i, a]) {
                    int val = GetLongestPath(matrix, n, a);
                    if(val > max) {
                        max = val;
                    }
                }
            }
            return max + 1;
        }

        [HttpPost]
        public JsonResult CanAddElective(int degreeElementId, int degreeId, string department, string catalog) {
            using(var context = new DegreePlannerContext()) {
                var degreeElement = context.DegreeElements
                    .Include(de => de.Members)
                    .ThenInclude(cg => cg.CourseCourseGroupLinks)
                    .ThenInclude(ccgl => ccgl.Course)
                    .FirstOrDefault(de => de.DegreeElementID == degreeElementId);

                if(degreeElement == null)
                    return Json(new { allow = false, taken = false, requirements = true, name = "", courseID = -1 });
                var course = CreateOrFetchCourse(context, department, catalog, out var generated);
                if(generated)
                    return Json(new { allow = false, taken = false, requirements = true, name = "", courseID = -1 });

                var level = int.Parse(catalog[0] + "");

                bool? inOtherElement = context.Degrees
                    .Include(d => d.Requirements)
                    .ThenInclude(de => de.Members)
                    .ThenInclude(cg => cg.CourseCourseGroupLinks)
                    .ThenInclude(ccgl => ccgl.Course)
                    .FirstOrDefault(d => d.DegreeID == degreeId)
                    ?.Requirements
                    .Where(r => r.Members.Courses.Sum(c => c.Hours) == r.Hours)
                    .All(de => !de.Members.Courses.Any(c => c.Department == department && c.CatalogNumber == catalog));

                if(inOtherElement == null || !inOtherElement.Value) {
                    return Json(new { allow = false, taken = true, requirements = true, name = "", courseID = -1 });
                }

                foreach(var elective in degreeElement.Members.Courses.Where(c => c.CatalogNumber.EndsWith("E"))) {
                    var requiredLevel = int.Parse(elective.CatalogNumber[0] + "");
                    if(elective.Department == "UARK" && requiredLevel <= level)
                        return Json(new { allow = true, taken = false, requirements = true, name = course.Name, courseID = course.CourseID });
                    if(elective.Department == department && elective.CatalogNumber[3] == 'E' &&
                       requiredLevel <= level)
                        return Json(new { allow = true, taken = false, requirements = true, name = course.Name, courseID = course.CourseID });
                }

                return Json(new { allow = false, taken = false, requirements = false, name = "" });
            }
        }

        public IActionResult SelectCourses(int degreeId) {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
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

                if(degree == null)
                    return RedirectToAction("Index", "Planner");

                vm = degree.Requirements
                    .Select(de => new SelectCoursesVm() {
                        DegreeElement = de,
                        CoursesTaken = new List<int>()
                    })
                    .ToList();

                IList<Course> coursesTaken = context.Users
                    .Include(u => u.CourseUserLinks)
                    .ThenInclude(cul => cul.Course)
                    .FirstOrDefault(u => u.UserID == user.UserID)
                    .Courses.ToList();

                IDictionary<SelectCoursesVm, int> hours = vm.ToDictionary(c => c, c => 0);

                // remove all fixed courses
                foreach(var requirementList in vm.Where(sc => sc.DegreeElement.Members.Courses.Sum(c => c.Hours) == sc.DegreeElement.Hours)) {
                    int h = hours[requirementList];
                    foreach(var course in requirementList.DegreeElement.Members.Courses.Where(c => !c.CatalogNumber.EndsWith("E"))) {
                        Course taken = coursesTaken.FirstOrDefault(c =>
                            c.Department == course.Department && c.CatalogNumber == course.CatalogNumber);

                        if(taken != null && h < requirementList.DegreeElement.Hours) {
                            requirementList.CoursesTaken.Add(taken.CourseID);
                            coursesTaken.Remove(taken);
                            h += taken.Hours;
                        }
                    }
                    hours[requirementList] = h;
                }

                foreach(var requirementList in vm.Where(sc => sc.DegreeElement.Members.Courses.Sum(c => c.Hours) != sc.DegreeElement.Hours)) {
                    int h = hours[requirementList];
                    foreach(var course in requirementList.DegreeElement.Members.Courses.Where(c => !c.CatalogNumber.EndsWith("E"))) {
                        Course taken = coursesTaken.FirstOrDefault(c =>
                            c.Department == course.Department && c.CatalogNumber == course.CatalogNumber);

                        if(taken != null && h < requirementList.DegreeElement.Hours) {
                            requirementList.CoursesTaken.Add(taken.CourseID);
                            coursesTaken.Remove(taken);
                            h += taken.Hours;
                        }
                    }
                    hours[requirementList] = h;
                }

                // remove all electives with fixed departments
                foreach(var requirementList in vm) {
                    int h = hours[requirementList];
                    IList<Course> toAppend = new List<Course>();
                    foreach(var course in requirementList.DegreeElement.Members.Courses.Where(c => c.CatalogNumber.EndsWith("E") && c.Department != "UARK")) {
                        IEnumerable<Course> taken = coursesTaken.Where(c =>
                            c.Department == course.Department && c.CatalogNumber[0] >= course.CatalogNumber[0])
                            .ToList();
                        foreach(var takenCourse in taken) {
                            if(h < requirementList.DegreeElement.Hours) {
                                toAppend.Add(takenCourse);
                                requirementList.CoursesTaken.Add(takenCourse.CourseID);
                                coursesTaken.Remove(takenCourse);
                                h += takenCourse.Hours;
                            } else {
                                break;
                            }
                        }
                    }
                    hours[requirementList] = h;

                    foreach(var course in toAppend) {
                        requirementList.DegreeElement.Members.CourseCourseGroupLinks.Add(new CourseCourseGroupLink() {
                            Course = course
                        });
                    }
                }

                // remove all other electives
                foreach(var requirementList in vm) {
                    int h = hours[requirementList];
                    IList<Course> toAppend = new List<Course>();
                    foreach(var course in requirementList.DegreeElement.Members.Courses.Where(c => c.CatalogNumber.EndsWith("E") && c.Department == "UARK")) {
                        IEnumerable<Course> taken = coursesTaken.Where(c => c.CatalogNumber[0] >= course.CatalogNumber[0])
                            .ToList();
                        foreach(var takenCourse in taken) {
                            if(h < requirementList.DegreeElement.Hours) {
                                toAppend.Add(takenCourse);
                                requirementList.CoursesTaken.Add(takenCourse.CourseID);
                                coursesTaken.Remove(takenCourse);
                                h += takenCourse.Hours;
                            } else {
                                break;
                            }
                        }
                    }
                    hours[requirementList] = h;

                    foreach(var course in toAppend) {
                        requirementList.DegreeElement.Members.CourseCourseGroupLinks.Add(new CourseCourseGroupLink() {
                            Course = course
                        });
                    }
                }

                vm = vm.Where(de => de.DegreeElement.Members.Courses.Sum(c => c.Hours) != de.DegreeElement.Hours)
                    .ToList();

                ViewBag.DegreeId = degreeId;

                var prefix = 0;
                foreach(var list in vm) {
                    list.IdPrefix = prefix;
                    prefix++;
                }
            }

            return View(vm);
        }

        [HttpPost]
        public IActionResult GenerateSchedules(GenerateDegreeVm vm) {
            if(!Authenticate())
                return Json("");
            var user = GetCurrentlyLoggedInUser();

            if(ModelState.IsValid) {
                return RedirectToAction("SelectCourses", "Planner", new { degreeId = vm.DegreeID });
            }
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

        [HttpPost]
        public JsonResult AddCourseTaken(string department, string catalog) {
            if(!Authenticate())
                return Json("");
            var user = GetCurrentlyLoggedInUser();

            if(string.IsNullOrEmpty(department) || string.IsNullOrEmpty(catalog))
                return Json("false");

            using(var context = new DegreePlannerContext()) {
                var hasCourseAlready = context.Users
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
        public JsonResult RemoveCourseTaken(string department, string catalog) {
            if(!Authenticate())
                return Json("");
            var user = GetCurrentlyLoggedInUser();

            if(string.IsNullOrEmpty(department) || string.IsNullOrEmpty(catalog))
                return Json("false");

            using(var context = new DegreePlannerContext()) {
                var hasCourseAlready = context.Users
                    .Include(u => u.CourseUserLinks)
                    .ThenInclude(cul => cul.Course)
                    .FirstOrDefault(u => u.UserID == user.UserID)
                    ?.Courses
                    .Any(c => c.Department == department && c.CatalogNumber == catalog);

                if(hasCourseAlready == null || !hasCourseAlready.Value)
                    return GetCoursesTaken();

                context.Users.Include(u => u.CourseUserLinks).ThenInclude(cul => cul.Course)
                    .FirstOrDefault(u => u.UserID == user.UserID)
                    ?.CourseUserLinks.Remove(
                        context.CourseUserLinks.FirstOrDefault(c =>
                            c.UserID == user.UserID && c.Course.Department == department &&
                            c.Course.CatalogNumber == catalog));
                context.SaveChanges();
            }

            return GetCoursesTaken();
        }

        [HttpPost]
        public JsonResult GetCoursesTaken() {
            if(!Authenticate())
                return Json("");
            var user = GetCurrentlyLoggedInUser();

            using(var context = new DegreePlannerContext()) {
                var courses = context.Users
                     .Include(c => c.CourseUserLinks)
                     .ThenInclude(cul => cul.Course)
                     .FirstOrDefault(u => u.UserID == user.UserID)
                     .Courses
                     .OrderBy(c => c.Department)
                     .ThenBy(c => c.CatalogNumber)
                     .Select(c => new { c.Name, c.Department, c.CatalogNumber })
                     .ToList();

                return Json(courses);
            }
        }

        [HttpPost]
        public JsonResult GetElectiveCourses(string department, string catalogNumber, int page) {
            if(!Authenticate()) {
                return Json("");
            }

            using(var context = new DegreePlannerContext()) {
                var courses = context.Courses.Where(c => !c.CatalogNumber.EndsWith("E"));

                if(department != "UARK") {
                    courses = courses.Where(c => c.Department == department);
                }

                if(catalogNumber.EndsWith("E")) {
                    courses = courses.Where(c => c.CatalogNumber[0] >= catalogNumber[0]);
                }

                int start = page * 25;
                courses = courses.OrderBy(c => c.Department)
                    .ThenBy(c => c.CatalogNumber)
                    .Skip(start)
                    .Take(25);

                return Json(courses.ToList());
            }
        }

        public IActionResult UploadDegree() {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            return View(new FileUploadVm());
        }

        [HttpPost]
        public IActionResult UploadDegree(FileUploadVm vm) {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            if(!ModelState.IsValid)
                return View(vm);

            var degree = new Degree {
                Name = vm.Name,
                Requirements = new List<DegreeElement>()
            };
            using(var context = new DegreePlannerContext())
            using(var stream = vm.File.OpenReadStream())
            using(var reader = new StreamReader(stream)) {
                context.Degrees.Add(degree);
                context.SaveChanges();

                string line;
                while((line = reader.ReadLine()) != null) {
                    var data = line.Split(',');

                    var element = new DegreeElement();
                    element.Hours = int.Parse(data[0]);
                    var courseGroupName = data[1];
                    var courseGroup = context.CourseGroups.FirstOrDefault(cg => cg.Name == courseGroupName);
                    if(data.Length == 2) {
                        if(courseGroup == null)
                            courseGroup = new CourseGroup() {
                                Name = courseGroupName
                            };
                    } else {
                        if(courseGroup == null) {
                            courseGroup = new CourseGroup() {
                                Name = courseGroupName,
                                CourseCourseGroupLinks = new List<CourseCourseGroupLink>()
                            };

                            for(var i = 2; i < data.Length; i++) {
                                var link = new CourseCourseGroupLink();
                                var department = data[i].Substring(0, 4);
                                var number = data[i].Substring(4);
                                var course = CreateOrFetchCourse(context, department, number, out var gen);

                                link.Course = course;
                                courseGroup.CourseCourseGroupLinks.Add(link);
                            }
                        }
                    }
                    element.Members = courseGroup;
                    element.Degree = degree;
                    context.DegreeElements.Add(element);
                    context.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Planner");
        }

        public IActionResult UploadCourseGroup() {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            return View(new FileUploadVm());
        }

        [HttpPost]
        public IActionResult UploadCourseGroup(FileUploadVm vm) {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            if(!ModelState.IsValid)
                return View(vm);

            using(var context = new DegreePlannerContext())
            using(var stream = vm.File.OpenReadStream())
            using(var reader = new StreamReader(stream)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    var data = line.Split(',');

                    var name = data[0];
                    var courseGroup = new CourseGroup() {
                        Name = name,
                        CourseCourseGroupLinks = new List<CourseCourseGroupLink>()
                    };
                    for(var i = 1; i < data.Length; i++) {
                        var link = new CourseCourseGroupLink();
                        var department = data[i].Substring(0, 4);
                        var number = data[i].Substring(4);
                        var course = CreateOrFetchCourse(context, department, number, out var gen);

                        link.Course = course;

                        courseGroup.CourseCourseGroupLinks.Add(link);
                    }

                    context.CourseGroups.Add(courseGroup);
                    context.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Planner");
        }

        public IActionResult UploadCourseList() {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            return View(new FileUploadVm());
        }

        [HttpPost]
        public IActionResult UploadCourseList(FileUploadVm vm) {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            if(!ModelState.IsValid)
                return View(vm);

            using(var context = new DegreePlannerContext())
            using(var stream = vm.File.OpenReadStream())
            using(var reader = new StreamReader(stream)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    var data = line.Split(',');

                    var department = data[0].Substring(0, 4);
                    var catalogNumber = data[0].Substring(4);

                    var course = CreateOrFetchCourse(context, department, catalogNumber, out var gen, true);
                    var start = 1;
                    var name = data[1];
                    if(name[0] == '"') {
                        start = 2;
                        while(!name.EndsWith("\"")) {
                            name += data[start];
                            start++;
                        }
                        course.Name = name.Substring(1, name.Length - 2);
                    }

                    if(gen)
                        context.Courses.Add(course);
                    else
                        context.Courses.Update(course);

                    var save = gen;

                    for(var i = start; i < data.Length; i++) {
                        var prereqDepartment = data[i].Substring(0, 4);
                        var prereqCatalog = data[i].Substring(4);

                        var prereq = CreateOrFetchCourse(context, prereqDepartment, prereqCatalog, out var genPrereq, true);
                        if(genPrereq || gen) {
                            var link = new PrerequisiteLink() {
                                Prerequisite = prereq,
                                Course = course
                            };
                            save = true;
                            if(genPrereq)
                                context.Courses.Add(prereq);
                            context.PrerequisiteLinks.Add(link);
                        } else if(!context.PrerequisiteLinks.Any(p => p.CourseID == course.CourseID && p.PrerequisiteID == prereq.CourseID)) {
                            var link = new PrerequisiteLink() {
                                Prerequisite = prereq,
                                Course = course
                            };

                            context.PrerequisiteLinks.Add(link);
                            save = true;
                        }
                    }

                    if(save)
                        context.SaveChanges();
                }

                context.SaveChanges();
            }
            return RedirectToAction("Index", "Planner");
        }

        public IActionResult AdministerUsers() {
            if(!Authenticate())
                return RedirectToAction("Index", "Home");
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return RedirectToAction("Index", "Planner");

            using(var context = new DegreePlannerContext()) {
                var users = context.Users.Where(u => u.UserID != user.UserID).ToList();

                return View(users);
            }
        }

        [HttpPost]
        public JsonResult ToggleAdmin(int userID, bool isAdmin) {
            if(!Authenticate())
                return Json(false);
            var user = GetCurrentlyLoggedInUser();
            if(!user.IsAdmin)
                return Json(false);

            using(var context = new DegreePlannerContext()) {
                var toEdit = context.Users.FirstOrDefault(u => u.UserID == userID);
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
                    .ThenInclude(pl => pl.Prerequisite)
                    .FirstOrDefault(c => c.Department == department && c.CatalogNumber == catalog);
            } else {
                course = context.Courses.FirstOrDefault(c => c.Department == department && c.CatalogNumber == catalog);
            }

            generated = course == null;
            if(course != null)
                return course;

            var variable = !int.TryParse(catalog[3] + "", out var hours);
            if(variable)
                hours = -1;

            return new Course() {
                Department = department,
                CatalogNumber = catalog,
                PrerequisiteLinks = new List<PrerequisiteLink>(),
                Hours = hours
            };
        }

        private User GetCurrentlyLoggedInUser() {
            var userId = HttpContext.Session.GetInt32(HomeController.USERNAME);
            if(userId == null)
                return null;
            using(var context = new DegreePlannerContext()) {
                return context.Users.FirstOrDefault(u => u.UserID == userId);
            }
        }

        private bool Authenticate() {
            var userId = HttpContext.Session.GetInt32(HomeController.USERNAME);
            if(userId == null)
                return false;
            using(var context = new DegreePlannerContext()) {
                return context.Users.Any(u => u.UserID == userId);
            }
        }
    }
}