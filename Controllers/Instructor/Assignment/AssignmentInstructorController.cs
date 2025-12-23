using BTL_QuanLyLopHocTrucTuyen.Data;
using BTL_QuanLyLopHocTrucTuyen.Models;
using BTL_QuanLyLopHocTrucTuyen.Services;
using BTL_QuanLyLopHocTrucTuyen.Repositories;
using Microsoft.AspNetCore.Mvc;
using BTL_QuanLyLopHocTrucTuyen.Core.Controllers;
using Microsoft.EntityFrameworkCore;

namespace BTL_QuanLyLopHocTrucTuyen.Controllers
{
    [Route("Instructor/[action]")]
    public class AssignmentInstructorController : BaseInstructorController
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly SupabaseStorageService _supabaseStorage;
        private readonly ApplicationDbContext _context;

        public AssignmentInstructorController(
            ApplicationDbContext context,
            IAssignmentRepository assignmentRepository,
            ILessonRepository lessonRepository,
            SupabaseStorageService supabaseStorage)
        {
            _context = context;
            _assignmentRepository = assignmentRepository;
            _lessonRepository = lessonRepository;
            _supabaseStorage = supabaseStorage;
        }

        /* =====================================================
           📋 DANH SÁCH BÀI TẬP TRONG KHÓA HỌC HIỆN TẠI
        ===================================================== */
        [HttpGet]
        public async Task<IActionResult> Assignment()
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;

            // 🔹 Lấy toàn bộ bài tập thuộc các bài học trong khóa học này
            var assignments = await _context.Assignments
                .Include(a => a.Lesson)
                .Where(a => a.Lesson != null && a.Lesson.CourseId == courseId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();


            ViewBag.CourseName = GetCurrentCourseName();
            return View("~/Views/Instructor/AssignmentInstructor/Assignment.cshtml", assignments);
        }

        /* =====================================================
           ➕ THÊM BÀI TẬP
        ===================================================== */
        [HttpGet]
        public async Task<IActionResult> AddAssignment(Guid? lessonId)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;

            // 🔹 Lọc chỉ lấy bài học của khóa học hiện tại
            var lessons = (await _lessonRepository.FindAsync())
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.Title)
                .ToList();

            ViewBag.Lessons = lessons;
            ViewBag.CourseName = GetCurrentCourseName();

            var assignment = new Assignment { LessonId = lessonId ?? Guid.Empty };
            return View("~/Views/Instructor/AssignmentInstructor/AddAssignment.cshtml", assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssignment([FromForm] Assignment assignment)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;

            if (!ModelState.IsValid)
            {
                ViewBag.Lessons = (await _lessonRepository.FindAsync())
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.Title)
                    .ToList();
                return View("~/Views/Instructor/AssignmentInstructor/AddAssignment.cshtml", assignment);
            }

            assignment.Id = Guid.NewGuid();
            assignment.CreatedAt = DateTime.Now;

            try
            {
                // ✅ Nếu có file upload → upload lên Supabase
                if (assignment.UploadFile != null && assignment.UploadFile.Length > 0)
                {
                    var url = await _supabaseStorage.UploadFileAsync(assignment.UploadFile, "assignments");
                    assignment.UploadedFileUrl = url;
                    assignment.UploadedFileName = assignment.UploadFile.FileName;
                }

                await _assignmentRepository.AddAsync(assignment);
                TempData["SuccessMessage"] = "✅ Thêm bài tập thành công!";
                return RedirectToAction(nameof(Assignment));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi thêm bài tập: {ex.Message}");

                ViewBag.Lessons = (await _lessonRepository.FindAsync())
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.Title)
                    .ToList();

                return View("~/Views/Instructor/AssignmentInstructor/AddAssignment.cshtml", assignment);
            }
        }

        /* =====================================================
           ✏️ CHỈNH SỬA BÀI TẬP
        ===================================================== */
        [HttpGet]
        public async Task<IActionResult> EditAssignment(Guid id)
        {
            var redirect = EnsureCourseSelected();
            if (redirect != null) return redirect;

            var courseId = GetCurrentCourseId()!.Value;
            var assignment = await _context.Assignments
                .Include(a => a.Lesson)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null || assignment.Lesson?.CourseId != courseId)
                return NotFound();

            ViewBag.Lessons = (await _lessonRepository.FindAsync())
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.Title)
                .ToList();

            ViewBag.CourseName = GetCurrentCourseName();
            return View("~/Views/Instructor/AssignmentInstructor/EditAssignment.cshtml", assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment([FromForm] Assignment assignment)
        {
            var existing = await _assignmentRepository.FindByIdAsync(assignment.Id);
            if (existing == null)
                return NotFound();

            try
            {
                existing.Title = assignment.Title;
                existing.Description = assignment.Description;
                existing.MaxScore = assignment.MaxScore;
                existing.Type = assignment.Type;
                existing.AvailableFrom = assignment.AvailableFrom;
                existing.AvailableUntil = assignment.AvailableUntil;
                existing.LessonId = assignment.LessonId;
                existing.ExternalFileUrl = assignment.ExternalFileUrl;

                // ✅ Nếu có file mới → xóa cũ, upload lại
                if (assignment.UploadFile != null && assignment.UploadFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.UploadedFileUrl))
                        await _supabaseStorage.DeleteFileAsync(existing.UploadedFileUrl);

                    var newUrl = await _supabaseStorage.UploadFileAsync(assignment.UploadFile, "assignments");
                    existing.UploadedFileUrl = newUrl;
                    existing.UploadedFileName = assignment.UploadFile.FileName;
                }

                await _assignmentRepository.UpdateAsync(existing);

                TempData["SuccessMessage"] = "✅ Cập nhật bài tập thành công!";
                return RedirectToAction(nameof(Assignment));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi EditAssignment: {ex.Message}");
                return View("~/Views/Instructor/AssignmentInstructor/EditAssignment.cshtml", assignment);
            }
        }

        /* =====================================================
           🗑️ XÓA BÀI TẬP
        ===================================================== */
        [HttpDelete]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            var assignment = await _assignmentRepository.FindByIdAsync(id);
            if (assignment == null)
                return Json(new { success = false, message = "Không tìm thấy bài tập để xóa!" });

            try
            {
                if (!string.IsNullOrEmpty(assignment.UploadedFileUrl))
                    await _supabaseStorage.DeleteFileAsync(assignment.UploadedFileUrl);

                await _assignmentRepository.DeleteByIdAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /* =====================================================
           🌍 CÔNG KHAI / ẨN
        ===================================================== */
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TogglePublicAssignment(Guid id)
        {
            var assignment = await _assignmentRepository.FindByIdAsync(id);
            if (assignment == null)
                return Json(new { success = false, message = "Không tìm thấy bài tập." });

            try
            {
                assignment.bDisabled = !assignment.bDisabled;
                await _assignmentRepository.UpdateAsync(assignment);
                return Json(new { success = true, isPublic = assignment.bDisabled });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
