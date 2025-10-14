using System.Data.Entity;

namespace Clinic.Models
{
    // ĐỔI: từ IdentityDbContext<ApplicationUser> -> DbContext
    public class ClinicDbContext : DbContext
    {
        public ClinicDbContext() : base("name=ClinicDb") { }
        public static ClinicDbContext Create() => new ClinicDbContext();

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<WorkingHour> WorkingHours { get; set; }
        public DbSet<PatientProfile> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Nếu dùng decimal cho Fee (EF6)
            modelBuilder.Entity<Service>()
                        .Property(x => x.Fee)
                        .HasPrecision(18, 2);
        }
    }
}
