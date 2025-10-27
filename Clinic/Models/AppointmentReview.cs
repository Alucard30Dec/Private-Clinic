using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần thiết cho ForeignKey

namespace Clinic.Models
{
    // Lớp này định nghĩa cấu trúc của bảng AppointmentReviews trong CSDL
    public class AppointmentReview
    {
        // Khóa chính, tự động tăng
        public int Id { get; set; }

        // Khóa ngoại, liên kết đến bảng Appointments
        // Required đảm bảo mỗi đánh giá phải thuộc về một lịch hẹn cụ thể
        [Required]
        [ForeignKey("Appointment")] // Chỉ rõ cột này liên kết với thuộc tính Appointment ở dưới
        public int AppointmentId { get; set; }

        // Số sao đánh giá (ví dụ: 1 đến 5)
        [Required(ErrorMessage = "Vui lòng chọn đánh giá.")] // Bắt buộc phải có
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")] // Giới hạn giá trị
        public int Rating { get; set; }

        // Nội dung bình luận (tùy chọn)
        [DataType(DataType.MultilineText)] // Giúp hiển thị ô nhập liệu nhiều dòng
        [StringLength(1000, ErrorMessage = "Nội dung đánh giá quá dài.")] // Giới hạn độ dài
        public string Comments { get; set; }

        // Ngày giờ đánh giá được tạo (lấy giờ UTC)
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        // Cờ để Admin duyệt (mặc định là chưa duyệt)
        public bool IsApproved { get; set; } = false;

        // Thuộc tính điều hướng (Navigation Property)
        // Giúp Entity Framework tự động liên kết đến đối tượng Appointment tương ứng
        // khi bạn truy vấn dữ liệu (ví dụ: dùng .Include())
        public virtual Appointment Appointment { get; set; }
    }

    // ViewModel dùng cho Form tạo đánh giá (tách biệt để linh hoạt hơn)
    public class ReviewCreateViewModel
    {
        // Chỉ cần ID lịch hẹn để biết đánh giá này cho ai
        public int AppointmentId { get; set; }

        // Hiển thị thông tin lịch hẹn trên Form cho người dùng biết
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; } // Giờ hẹn (Local)

        // Các trường nhập liệu giống như trong Model chính
        [Required(ErrorMessage = "Vui lòng chọn đánh giá.")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; } = 5; // Mặc định 5 sao cho tiện

        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Nội dung đánh giá quá dài.")]
        [Display(Name = "Nội dung đánh giá (tùy chọn)")] // Tên hiển thị trên Form
        public string Comments { get; set; }
    }
}