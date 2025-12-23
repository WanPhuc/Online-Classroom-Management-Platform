using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BTL_QuanLyLopHocTrucTuyen.Data;
using BTL_QuanLyLopHocTrucTuyen.Models;

namespace BTL_QuanLyLopHocTrucTuyen.Controllers
{
    [Route("Instructor/[action]")]
    public class ManageLessonController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ManageLessonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 View chính chứa layout CRUD
        [Authorize(Roles = "Admin,User")]
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Instructor/ManageLesson/Index.cshtml");
        }

        // 🔹 Hiển thị danh sách bài học (AJAX)
        [HttpGet]
        public IActionResult GetLessons()
        {
            var data = _context.Lessons
                .Select(l => new { l.Id, l.Title, l.Content, l.VerifyKey })
                .ToList();

            return PartialView("~/Views/Instructor/ManageLesson/_LessonList.cshtml", data);
        }

        // 🔹 Thêm bài học (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddLesson([FromForm] Lesson lesson)
        {
            if (!ModelState.IsValid) return BadRequest("Dữ liệu không hợp lệ.");
            lesson.Id = Guid.NewGuid();
            _context.Lessons.Add(lesson);
            _context.SaveChanges();
            return Ok();
        }

        // 🔹 Xóa bài học (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteLesson(Guid id)
        {
            var lesson = _context.Lessons.Find(id);
            if (lesson == null) return NotFound();
            _context.Lessons.Remove(lesson);
            _context.SaveChanges();
            return Ok();
        }
    }
}
