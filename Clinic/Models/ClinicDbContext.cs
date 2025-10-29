using System.Data.Entity;

namespace Clinic.Models
{
    public class ClinicDbContext : DbContext
    {
        public ClinicDbContext() : base("name=ClinicDb") { }
        public static ClinicDbContext Create() => new ClinicDbContext();

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<WorkingHour> WorkingHours { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }
        public DbSet<AppointmentReview> AppointmentReviews { get; set; }

        // *** THÊM DbSet CHO SPECIALTY ***
        public DbSet<Specialty> Specialties { get; set; }
        // *** KẾT THÚC THÊM ***

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<Service>().Property(x => x.Fee).HasPrecision(18, 2);

            // *** (Tùy chọn) Cấu hình quan hệ Doctor-Specialty nếu cần ***
            // modelBuilder.Entity<Doctor>()
            //    .HasRequired(d => d.Specialty) // Bác sĩ bắt buộc phải có chuyên khoa
            //    .WithMany() // Một chuyên khoa có nhiều bác sĩ (nếu không cần List<Doctor> trong Specialty)
            //    .HasForeignKey(d => d.SpecialtyId)
            //    .WillCascadeOnDelete(false); // Không xóa bác sĩ khi xóa chuyên khoa (chỉ set null hoặc báo lỗi tùy logic)

            // *** Cấu hình Query Filter cho Soft Delete (nếu dùng EF Core, EF6 không hỗ trợ trực tiếp) ***
            // Trong EF6, bạn cần thêm .Where(x => x.IsVisible) vào các truy vấn LINQ thủ công.
        }
    }
}
