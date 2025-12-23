using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BTL_QuanLyLopHocTrucTuyen.Core.Models;
using BTL_QuanLyLopHocTrucTuyen.Models.Enums;

namespace BTL_QuanLyLopHocTrucTuyen.Models
{
    [Table("Lessons")]
    public class Lesson : Entity
    {
        [Required(ErrorMessage = "Tên bài học là bắt buộc")]
        [MaxLength(200, ErrorMessage = "Tên bài học không được vượt quá 200 ký tự")]
        [Display(Name = "Tên bài học")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        [Display(Name = "Mô tả bài học")]
        public string? Content { get; set; }

        [Url(ErrorMessage = "Đường dẫn video không hợp lệ")]
        [Display(Name = "Liên kết video")]
        public string? VideoUrl { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Thời gian bắt đầu")]
        [Required(ErrorMessage = "Vui lòng nhập thời gian bắt đầu")]
        public DateTime BeginTime { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Thời gian kết thúc")]
        [Required(ErrorMessage = "Vui lòng nhập thời gian kết thúc")]
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1);

        //[Required(ErrorMessage = "Bài học phải thuộc về một khóa học")]
        [ForeignKey(nameof(Course))]
        public Guid? CourseId { get; set; }   // Cho phép null

        [JsonIgnore]
        public Course? Course { get; set; } // Quan hệ có thể null


        [Display(Name = "Trạng thái lịch học")]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Planned;

        [JsonIgnore]
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        [JsonIgnore]
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

        [Required(ErrorMessage = "Vui lòng nhập VerifyKey")]
        [StringLength(50, MinimumLength = 10, ErrorMessage = "VerifyKey phải có đúng 10 ký tự")]
        [RegularExpression(@"^[0-9][A-Za-z0-9]{9}$", ErrorMessage = "VerifyKey phải bắt đầu bằng số và có 10 ký tự")]
        public string VerifyKey { get; set; }
    }
}
