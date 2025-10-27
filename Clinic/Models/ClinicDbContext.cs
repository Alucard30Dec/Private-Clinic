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

        // CHỈ GIỮ LẠI MỘT DbSet CHO Patient
        public DbSet<Patient> Patients { get; set; }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }

        public DbSet<AppointmentReview> AppointmentReviews { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Nếu bảng trong DB đang tên "Patients", map DbSet Patients về đúng bảng đó
            modelBuilder.Entity<Patient>().ToTable("Patients");

            modelBuilder.Entity<Service>()
                        .Property(x => x.Fee)
                        .HasPrecision(18, 2);
        }
    }
}
