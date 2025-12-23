using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BTL_QuanLyLopHocTrucTuyen.Data;
using BTL_QuanLyLopHocTrucTuyen.Models;
using BTL_QuanLyLopHocTrucTuyen.Repositories;
using BTL_QuanLyLopHocTrucTuyen.Core.Controllers;
using BTL_QuanLyLopHocTrucTuyen.Authorizations;
using BTL_QuanLyLopHocTrucTuyen.Models.Enums;

namespace BTL_QuanLyLopHocTrucTuyen.Controllers
{
    [Route("Instructor/[action]")]
    public class LessonInstructorController : BaseInstructorController
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly ICourseRepository _courseRepository;

        public LessonInstructorController(ILessonRepository lessonRepository, ICourseRepository courseRepository)
        {
            _lessonRepository = lessonRepository;
            _courseRepository = courseRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Lesson()
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;

            var lessons = (await _lessonRepository.FindAsync())
                .Where(l => l.CourseId == courseId)
                .OrderByDescending(l => l.BeginTime)
                .ToList();

            var course = await _courseRepository.FindByIdAsync(courseId);
            ViewBag.CourseName = course?.Name ?? "Khóa học không xác định";
            ViewBag.CourseId = courseId;

            if (User.Identity?.IsAuthenticated == true)
            {
                string lastVisitTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

                Response.Cookies.Append(
                    "LastVisit_Lesson",         
                    lastVisitTime,              
                    new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(7), 
                        HttpOnly = true,                   
                        Secure = false                    
                    }
                );
            }

            return View("~/Views/Instructor/LessonInstructor/Lesson.cshtml", lessons);
        }


        // ✅ Chi tiết bài học
        [HttpGet]
        public async Task<IActionResult> DetailLesson(Guid id)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;

            var lesson = await _lessonRepository.FindByIdAsync(id);
            if (lesson == null || lesson.CourseId != courseId)
                return NotFound();

            var allLessons = (await _lessonRepository.FindAsync())
                .Where(l => l.CourseId == courseId)
                .OrderByDescending(l => l.BeginTime)
                .ToList();

            ViewBag.AllLessons = allLessons;
            ViewBag.CourseId = courseId;
            ViewBag.CourseName = (await _courseRepository.FindByIdAsync(courseId))?.Name;

            return View("~/Views/Instructor/LessonInstructor/DetailLesson.cshtml", lesson);
        }

        // ✅ Giao diện thêm bài học
        [HttpGet]
        public async Task<IActionResult> AddLesson()
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;
            var course = await _courseRepository.FindByIdAsync(courseId);

            ViewBag.CourseId = courseId;
            ViewBag.CourseName = course?.Name;

            return View("~/Views/Instructor/LessonInstructor/AddLesson.cshtml");
        }

        // ✅ Xử lý thêm bài học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLesson([FromForm] Lesson lesson)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var courseId = GetCurrentCourseId()!.Value;

            // ✅ Lấy ID giảng viên từ khóa học hiện tại
            var currentCourse = await _courseRepository.FindByIdAsync(courseId);
            if (currentCourse == null)
                return Json(new { success = false, message = "❌ Không tìm thấy khóa học hiện tại!" });

            var instructorId = currentCourse.InstructorId;

            // ✅ Lấy tất cả khóa học mà giảng viên này đang dạy
            var instructorCourses = await _courseRepository.FindAsync();
            var courseIdsOfInstructor = instructorCourses
                .Where(c => c.InstructorId == instructorId)
                .Select(c => c.Id)
                .ToList();

            // ✅ Kiểm tra trùng giờ trong toàn bộ các bài học của các khóa học đó
            var existingLesson = (await _lessonRepository.FindAsync())
                .FirstOrDefault(l =>
                    l.CourseId.HasValue && courseIdsOfInstructor.Contains(l.CourseId.Value) &&
                    (
                        // Trùng thời gian bắt đầu hoặc trong khoảng giao nhau
                        (lesson.BeginTime >= l.BeginTime && lesson.BeginTime < l.EndTime) ||
                        (lesson.EndTime > l.BeginTime && lesson.EndTime <= l.EndTime) ||
                        (lesson.BeginTime <= l.BeginTime && lesson.EndTime >= l.EndTime)
                    ) &&
                    l.Id != lesson.Id
                );

            if (existingLesson != null)
            {
                // ⚠️ Nếu trùng với bài học khác của cùng giảng viên
                return Json(new
                {
                    success = false,
                    message = $"⚠️ Giờ học này trùng với bài học \"{existingLesson.Title}\" " +
                            $"({existingLesson.BeginTime:HH:mm dd/MM/yyyy} - {existingLesson.EndTime:HH:mm}) " +
                            $"thuộc khóa học \"{existingLesson.Course?.Name}\"."
                });
            }

            // ✅ Nếu không trùng, thêm mới
            lesson.Id = Guid.NewGuid();
            lesson.CourseId = courseId;
            lesson.Status = Models.Enums.ScheduleStatus.Planned;

            await _lessonRepository.AddAsync(lesson);

            return Json(new { success = true });
        }

        // ✅ Chỉnh sửa bài học
        [HttpGet]
        public async Task<IActionResult> EditLesson(Guid id)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;

            var lesson = await _lessonRepository.FindByIdAsync(id);
            if (lesson == null || lesson.CourseId != courseId)
                return NotFound();

            ViewBag.CourseId = courseId;
            return View("~/Views/Instructor/LessonInstructor/EditLesson.cshtml", lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson([FromForm] Lesson lesson)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
                return View("~/Views/Instructor/LessonInstructor/EditLesson.cshtml", lesson);

            var existingLesson = await _lessonRepository.FindByIdAsync(lesson.Id);
            if (existingLesson == null)
                return NotFound();

            existingLesson.Title = lesson.Title;
            existingLesson.Content = lesson.Content;
            existingLesson.VideoUrl = lesson.VideoUrl;
            existingLesson.BeginTime = lesson.BeginTime;
            existingLesson.EndTime = lesson.EndTime;

            await _lessonRepository.UpdateAsync(existingLesson);

            TempData["SuccessMessage"] = "✅ Cập nhật bài học thành công!";
            return RedirectToAction(nameof(Lesson));
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteLesson(Guid id)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var lesson = await _lessonRepository.FindByIdAsync(id);
            if (lesson == null)
                return Json(new { success = false, message = "Không tìm thấy bài học!" });

            await _lessonRepository.DeleteByIdAsync(id);
            return Json(new { success = true });
        }
        // NewLesson
        [HttpGet]
        public async Task<IActionResult> NewLesson()
        {
            var lessons = await _lessonRepository.FindAsync();

            foreach (var lesson in lessons)
            {
                if (!string.IsNullOrEmpty(lesson.VerifyKey))
                {
                    lesson.VerifyKey = DecodeBase64(lesson.VerifyKey);
                }
            }

            var allLessons = lessons
                .OrderByDescending(l => l.BeginTime)
                .ToList();

            return View("~/Views/Instructor/LessonInstructor/NewLesson.cshtml", allLessons);
        }
        [HttpGet]
        public IActionResult AddNewLesson()
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            return View("~/Views/Instructor/LessonInstructor/AddNewLesson.cshtml");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNewLesson([FromForm] Lesson lesson)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            // ✅ Kiểm tra hợp lệ Model (đặc biệt là VerifyKey)
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var courseId = GetCurrentCourseId()!.Value;
            var currentCourse = await _courseRepository.FindByIdAsync(courseId);
            if (currentCourse == null)
                return Json(new { success = false, message = "❌ Không tìm thấy khóa học hiện tại!" });

            // 🧠 Mã hóa VerifyKey trước khi lưu (Base64 hoặc custom)
            lesson.VerifyKey = EncodeBase64(lesson.VerifyKey);

            // 🔹 Thiết lập thông tin khác
            lesson.Id = Guid.NewGuid();
            lesson.CourseId = courseId;
            lesson.Status = Models.Enums.ScheduleStatus.Planned;

            await _lessonRepository.AddAsync(lesson);

            return Json(new { success = true });
        }
        private string EncodeBase64(string plainText)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        private string DecodeBase64(string base64Text)
        {
            var bytes = Convert.FromBase64String(base64Text);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        //viewcookie
        [HttpGet]
        public IActionResult ViewCookie()
        {
            string lastVisit = Request.Cookies["LastVisit_Lesson"];

            if (string.IsNullOrEmpty(lastVisit))
                ViewBag.CookieMessage = "Chưa có cookie nào được lưu.";
            else
                ViewBag.CookieMessage = $"Lần truy cập cuối cùng vào trang Lesson: {lastVisit}";

            return View("~/Views/Instructor/LessonInstructor/ViewCookie.cshtml");
        }



    }
}
