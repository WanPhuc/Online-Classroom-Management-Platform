using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using BTL_QuanLyLopHocTrucTuyen.Core.Models;

namespace BTL_QuanLyLopHocTrucTuyen.Models
{
    public class Assignment : Entity
    {
        // ===== 🧩 THÔNG TIN CƠ BẢN =====
        [Required(ErrorMessage = "Tên bài tập là bắt buộc")]
        [MaxLength(200, ErrorMessage = "Tên bài tập không được vượt quá 200 ký tự")]
        [Display(Name = "Tên bài tập")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // ===== 📎 FILE NGOÀI (Drive / PDF / Link) =====
        [Url(ErrorMessage = "Đường dẫn liên kết ngoài không hợp lệ")]
        [Display(Name = "Liên kết ngoài (Google Drive, PDF, v.v.)")]
        public string? ExternalFileUrl { get; set; }

        // ===== 💾 FILE NỘI BỘ (Upload từ máy) =====
        [Display(Name = "Tên tệp tải lên")]
        [MaxLength(255)]
        public string? UploadedFileName { get; set; }

        [Display(Name = "Đường dẫn tệp nội bộ")]
        public string? UploadedFileUrl { get; set; }

        [NotMapped]
        [Display(Name = "Tệp tải lên (tùy chọn)")]
        public IFormFile? UploadFile { get; set; }

        // ===== 🧮 ĐIỂM & PHÂN LOẠI =====
        [Range(1, 100, ErrorMessage = "Điểm tối đa phải nằm trong khoảng 1 đến 100")]
        [Display(Name = "Điểm tối đa")]
        public int MaxScore { get; set; } = 10;

        [Required(ErrorMessage = "Loại bài tập là bắt buộc")]
        [MaxLength(50, ErrorMessage = "Loại bài tập không được vượt quá 50 ký tự")]
        [Display(Name = "Loại bài tập")]
        public string Type { get; set; } = "Bài tập";

        // ===== ⏰ THỜI GIAN =====
        [DataType(DataType.DateTime)]
        [Display(Name = "Bắt đầu từ")]
        public DateTime? AvailableFrom { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Hạn nộp")]
        public DateTime? AvailableUntil { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ===== 🌍 TRẠNG THÁI =====
        [Display(Name = "Công khai cho sinh viên")]
        public bool bDisabled { get; set; } = false;

        [NotMapped]
        [Display(Name = "Đã hết hạn")]
        public bool IsExpired => AvailableUntil.HasValue && AvailableUntil.Value < DateTime.Now;

        // ===== 🔗 LIÊN KẾT =====
        [Required(ErrorMessage = "Bài học là bắt buộc")]
        [ForeignKey("Lesson")]
        [Display(Name = "Bài học")]
        public Guid LessonId { get; set; }

        [JsonIgnore]
        public Lesson? Lesson { get; set; }

        [JsonIgnore]
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public DateTime DueDate { get; set; }

    }
}
