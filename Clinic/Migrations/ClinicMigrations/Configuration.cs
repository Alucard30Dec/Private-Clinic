namespace Clinic.Migrations.ClinicMigrations
{
    using Clinic.Models; // Ensure Models namespace is included
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity; // Needed for PasswordHasher
    using Microsoft.AspNet.Identity.EntityFramework; // Needed for UserStore etc. (if linking)

    internal sealed class Configuration : DbMigrationsConfiguration<Clinic.Models.ClinicDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Migrations\ClinicMigrations";
            // ContextKey = "Clinic.Models.ClinicDbContext"; // Optional: Explicitly set ContextKey if needed
        }

        protected override void Seed(Clinic.Models.ClinicDbContext context)
        {
            // This method will be called after migrating to the latest version,
            // or every time Update-Database is run if AutomaticMigrationsEnabled is true.

            // Use the DbSet<T>.AddOrUpdate() helper extension method
            // to avoid creating duplicate seed data.

            // ==================================
            // 1. Seed Specialties
            // ==================================
            // Using Name as the identifier property for AddOrUpdate
            context.Specialties.AddOrUpdate(
                s => s.Name, // Identifier: Update if Name matches, otherwise Insert
                new Specialty { Name = "Nội tổng quát", IsVisible = true },
                new Specialty { Name = "Nhi", IsVisible = true },
                new Specialty { Name = "Tai Mũi Họng", IsVisible = true },
                new Specialty { Name = "Hô hấp", IsVisible = true },
                new Specialty { Name = "Tiêu hóa", IsVisible = true },
                new Specialty { Name = "Thận - Tiết niệu", IsVisible = true },
                new Specialty { Name = "Cơ xương khớp", IsVisible = true },
                new Specialty { Name = "Nội tiết", IsVisible = true },
                new Specialty { Name = "Tim mạch", IsVisible = true },
                new Specialty { Name = "Da liễu", IsVisible = true },
                new Specialty { Name = "Ngoại tổng quát", IsVisible = true },
                new Specialty { Name = "Chấn thương chỉnh hình", IsVisible = true },
                new Specialty { Name = "Sản phụ khoa", IsVisible = true },
                new Specialty { Name = "Mắt", IsVisible = true },
                new Specialty { Name = "Thần kinh", IsVisible = true },
                new Specialty { Name = "Chưa xác định", IsVisible = true } // Default/Fallback
            );
            context.SaveChanges(); // Save specialties to get their IDs

            // ==================================
            // 2. Seed Services
            // ==================================
            context.Services.AddOrUpdate(
                s => s.Name, // Use Name as identifier
                new Service { Name = "Khám Dịch vụ", Fee = 200000, DurationMinutes = 30, ExamType = ExamType.Service, IsVisible = true },
                new Service { Name = "Khám BHYT", Fee = 50000, DurationMinutes = 20, ExamType = ExamType.HealthInsurance, IsVisible = true },
                new Service { Name = "Tái khám Dịch vụ", Fee = 150000, DurationMinutes = 20, ExamType = ExamType.Service, IsVisible = true },
                new Service { Name = "Tái khám BHYT", Fee = 30000, DurationMinutes = 15, ExamType = ExamType.HealthInsurance, IsVisible = true }
            );
            context.SaveChanges();

            // ==================================
            // 3. Seed Doctors
            // ==================================
            // Get Specialty IDs first
            var noiTongQuatId = context.Specialties.FirstOrDefault(s => s.Name == "Nội tổng quát")?.Id ?? 0;
            var nhiId = context.Specialties.FirstOrDefault(s => s.Name == "Nhi")?.Id ?? 0;
            var tmhId = context.Specialties.FirstOrDefault(s => s.Name == "Tai Mũi Họng")?.Id ?? 0;
            var hoHapId = context.Specialties.FirstOrDefault(s => s.Name == "Hô hấp")?.Id ?? 0;
            var tieuHoaId = context.Specialties.FirstOrDefault(s => s.Name == "Tiêu hóa")?.Id ?? 0;
            var thanTietNieuId = context.Specialties.FirstOrDefault(s => s.Name == "Thận - Tiết niệu")?.Id ?? 0;
            var cxkId = context.Specialties.FirstOrDefault(s => s.Name == "Cơ xương khớp")?.Id ?? 0;
            var noiTietId = context.Specialties.FirstOrDefault(s => s.Name == "Nội tiết")?.Id ?? 0;
            var timMachId = context.Specialties.FirstOrDefault(s => s.Name == "Tim mạch")?.Id ?? 0;
            var daLieuId = context.Specialties.FirstOrDefault(s => s.Name == "Da liễu")?.Id ?? 0;
            var ngoaiTQId = context.Specialties.FirstOrDefault(s => s.Name == "Ngoại tổng quát")?.Id ?? 0;
            var ctchId = context.Specialties.FirstOrDefault(s => s.Name == "Chấn thương chỉnh hình")?.Id ?? 0;
            var sanPhuKhoaId = context.Specialties.FirstOrDefault(s => s.Name == "Sản phụ khoa")?.Id ?? 0;
            var matId = context.Specialties.FirstOrDefault(s => s.Name == "Mắt")?.Id ?? 0;
            var thanKinhId = context.Specialties.FirstOrDefault(s => s.Name == "Thần kinh")?.Id ?? 0;


            // Use Email as identifier for AddOrUpdate (assuming Email is unique for doctors)
            // IMPORTANT: Linking UserId depends on Identity users being seeded first.
            // This might require running Identity seed first or handling potential null UserIds.
            // For simplicity, we'll try to find the UserId based on Email.
            string adminUserId = null;
            string receptUserId = null;
            string doctor1UserId = null;
            string doctor2UserId = null;
            // ... (Add variables for other doctor UserIds)
            string patient1UserId = null;
            string patient2UserId = null;

            try
            {
                // Use a separate context for Identity if necessary, or assume it runs first
                using (var identityContext = new ApplicationDbContext())
                {
                    adminUserId = identityContext.Users.FirstOrDefault(u => u.Email == "admin@clinic.local")?.Id;
                    receptUserId = identityContext.Users.FirstOrDefault(u => u.Email == "recept01@clinic.local")?.Id;
                    doctor1UserId = identityContext.Users.FirstOrDefault(u => u.Email == "quan@clinic.vn")?.Id;
                    doctor2UserId = identityContext.Users.FirstOrDefault(u => u.Email == "trang@clinic.vn")?.Id;
                    // ... (Find other doctor UserIds)
                    patient1UserId = identityContext.Users.FirstOrDefault(u => u.Email == "patient01@clinic.local")?.Id;
                    patient2UserId = identityContext.Users.FirstOrDefault(u => u.Email == "patient02@clinic.local")?.Id;
                }
            }
            catch (Exception ex)
            {
                // Log error if Identity DB access fails during Clinic seeding
                System.Diagnostics.Debug.WriteLine($"Error accessing Identity DB during Clinic seed: {ex.Message}");
                // Seeding will continue without UserIds linked
            }

            var passwordHasher = new PasswordHasher();
            string defaultPasswordHash = passwordHasher.HashPassword("12345678"); // Match Identity Seed

            context.Doctors.AddOrUpdate(
                d => d.Email, // Identifier
                new Doctor
                {
                    Name = "Dr. An",
                    SpecialtyId = noiTongQuatId,
                    IsVisible = true,
                    Email = "an@clinic.vn",
                    PhoneNumber = "0901001001",
                    Gender = "Nam",
                    DateOfBirth = new DateTime(1980, 4, 12),
                    YearsOfExperience = 12,
                    Bio = "Bác sĩ Nội tổng quát, nhiều năm kinh nghiệm.",
                    PhotoUrl = "/Resources/images/doctors/an.jpg",
                    Password = defaultPasswordHash,
                    NationalId = "123456789011",
                    Address = "123 Nguyen Van Linh, Q7",
                    UserId = context.Doctors.Any(doc => doc.Email == "an@clinic.vn") ? context.Doctors.First(doc => doc.Email == "an@clinic.vn").UserId : null // Preserve existing UserId or set null
                },
                 new Doctor
                 {
                     Name = "Dr. Bình",
                     SpecialtyId = nhiId,
                     IsVisible = true,
                     Email = "binh@clinic.vn",
                     PhoneNumber = "0902002002",
                     Gender = "Nam",
                     DateOfBirth = new DateTime(1985, 9, 8),
                     YearsOfExperience = 9,
                     Bio = "Bác sĩ Nhi khoa tận tâm.",
                     PhotoUrl = "/Resources/images/doctors/binh.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789012",
                     Address = "456 Le Loi, Q1",
                     UserId = context.Doctors.Any(doc => doc.Email == "binh@clinic.vn") ? context.Doctors.First(doc => doc.Email == "binh@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Châu",
                     SpecialtyId = tmhId,
                     IsVisible = true,
                     Email = "chau@clinic.vn",
                     PhoneNumber = "0903003003",
                     Gender = "Nữ",
                     DateOfBirth = new DateTime(1982, 2, 20),
                     YearsOfExperience = 10,
                     Bio = "Chuyên gia Tai Mũi Họng.",
                     PhotoUrl = "/Resources/images/doctors/chau.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789013",
                     Address = "789 Tran Hung Dao, Q5",
                     UserId = context.Doctors.Any(doc => doc.Email == "chau@clinic.vn") ? context.Doctors.First(doc => doc.Email == "chau@clinic.vn").UserId : null
                 },
                new Doctor
                {
                    Name = "Dr. Quân",
                    SpecialtyId = hoHapId,
                    IsVisible = true,
                    Email = "quan@clinic.vn",
                    PhoneNumber = "0904000004",
                    Gender = "Nam",
                    YearsOfExperience = 8,
                    Bio = "Chuyên hô hấp, COPD, hen",
                    PhotoUrl = "/Resources/images/doctors/quan.jpg",
                    Password = defaultPasswordHash,
                    NationalId = "123456789014",
                    Address = "101 Vo Van Tan, Q3",
                    UserId = doctor1UserId // Link if found
                },
                new Doctor
                {
                    Name = "Dr. Trang",
                    SpecialtyId = tieuHoaId,
                    IsVisible = true,
                    Email = "trang@clinic.vn",
                    PhoneNumber = "0905000005",
                    Gender = "Nữ",
                    YearsOfExperience = 7,
                    Bio = "Nội soi tiêu hoá, HP, IBS",
                    PhotoUrl = "/Resources/images/doctors/trang.jpg",
                    Password = defaultPasswordHash,
                    NationalId = "123456789015",
                    Address = "202 Nguyen Thi Minh Khai, Q1",
                    UserId = doctor2UserId // Link if found
                },
                 new Doctor
                 {
                     Name = "Dr. Vân",
                     SpecialtyId = thanTietNieuId,
                     IsVisible = true,
                     Email = "van@clinic.vn",
                     PhoneNumber = "0906000006",
                     Gender = "Nữ",
                     YearsOfExperience = 10,
                     Bio = "Sỏi thận, rối loạn đường tiểu",
                     PhotoUrl = "/Resources/images/doctors/van.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789016",
                     Address = "303 Hai Ba Trung, Q3",
                     UserId = context.Doctors.Any(doc => doc.Email == "van@clinic.vn") ? context.Doctors.First(doc => doc.Email == "van@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Hùng",
                     SpecialtyId = cxkId,
                     IsVisible = true,
                     Email = "hung@clinic.vn",
                     PhoneNumber = "0907000007",
                     Gender = "Nam",
                     YearsOfExperience = 12,
                     Bio = "Viêm khớp, loãng xương, phục hồi chức năng",
                     PhotoUrl = "/Resources/images/doctors/hung.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789017",
                     Address = "404 Dien Bien Phu, Binh Thanh",
                     UserId = context.Doctors.Any(doc => doc.Email == "hung@clinic.vn") ? context.Doctors.First(doc => doc.Email == "hung@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Hà",
                     SpecialtyId = noiTietId,
                     IsVisible = true,
                     Email = "ha@clinic.vn",
                     PhoneNumber = "0908000008",
                     Gender = "Nữ",
                     YearsOfExperience = 9,
                     Bio = "Đái tháo đường, tuyến giáp",
                     PhotoUrl = "/Resources/images/doctors/ha.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789018",
                     Address = "505 Cach Mang Thang Tam, Q10",
                     UserId = context.Doctors.Any(doc => doc.Email == "ha@clinic.vn") ? context.Doctors.First(doc => doc.Email == "ha@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Minh",
                     SpecialtyId = timMachId,
                     IsVisible = true,
                     Email = "minh_tm@clinic.vn",
                     PhoneNumber = "0909000009",
                     Gender = "Nam", // Changed email slightly
                     YearsOfExperience = 11,
                     Bio = "Tăng huyết áp, mạch vành",
                     PhotoUrl = "/Resources/images/doctors/minh_tm.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789019",
                     Address = "606 Ly Thuong Kiet, Tan Binh",
                     UserId = context.Doctors.Any(doc => doc.Email == "minh_tm@clinic.vn") ? context.Doctors.First(doc => doc.Email == "minh_tm@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Lan",
                     SpecialtyId = daLieuId,
                     IsVisible = true,
                     Email = "lan@clinic.vn",
                     PhoneNumber = "0910000010",
                     Gender = "Nữ",
                     YearsOfExperience = 6,
                     Bio = "Mụn, nám, viêm da, dị ứng",
                     PhotoUrl = "/Resources/images/doctors/lan.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789020",
                     Address = "707 Le Van Sy, Q3",
                     UserId = context.Doctors.Any(doc => doc.Email == "lan@clinic.vn") ? context.Doctors.First(doc => doc.Email == "lan@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Phong",
                     SpecialtyId = tmhId,
                     IsVisible = true,
                     Email = "phong@clinic.vn",
                     PhoneNumber = "0911000011",
                     Gender = "Nam",
                     YearsOfExperience = 13,
                     Bio = "Viêm xoang, viêm tai giữa, amidan",
                     PhotoUrl = "/Resources/images/doctors/phong.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789021",
                     Address = "808 Nguyen Kiem, Phu Nhuan",
                     UserId = context.Doctors.Any(doc => doc.Email == "phong@clinic.vn") ? context.Doctors.First(doc => doc.Email == "phong@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Thảo",
                     SpecialtyId = nhiId,
                     IsVisible = true,
                     Email = "thao@clinic.vn",
                     PhoneNumber = "0912000012",
                     Gender = "Nữ",
                     YearsOfExperience = 5,
                     Bio = "Sốt virus, dinh dưỡng trẻ em",
                     PhotoUrl = "/Resources/images/doctors/thao.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789022",
                     Address = "909 Hoang Van Thu, Tan Binh",
                     UserId = context.Doctors.Any(doc => doc.Email == "thao@clinic.vn") ? context.Doctors.First(doc => doc.Email == "thao@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Khánh",
                     SpecialtyId = ngoaiTQId,
                     IsVisible = true,
                     Email = "khanh_ntq@clinic.vn",
                     PhoneNumber = "0913000013",
                     Gender = "Nam", // Changed email
                     YearsOfExperience = 15,
                     Bio = "Thoát vị, dạ dày, túi mật",
                     PhotoUrl = "/Resources/images/doctors/khanh_ntq.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789023",
                     Address = "111 Pasteur, Q1",
                     UserId = context.Doctors.Any(doc => doc.Email == "khanh_ntq@clinic.vn") ? context.Doctors.First(doc => doc.Email == "khanh_ntq@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Dũng",
                     SpecialtyId = ctchId,
                     IsVisible = true,
                     Email = "dung_ctch@clinic.vn",
                     PhoneNumber = "0914000014",
                     Gender = "Nam", // Changed email
                     YearsOfExperience = 14,
                     Bio = "Gãy xương, dây chằng, khớp gối",
                     PhotoUrl = "/Resources/images/doctors/dung_ctch.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789024",
                     Address = "222 Vo Thi Sau, Q3",
                     UserId = context.Doctors.Any(doc => doc.Email == "dung_ctch@clinic.vn") ? context.Doctors.First(doc => doc.Email == "dung_ctch@clinic.vn").UserId : null
                 },
                 new Doctor
                 {
                     Name = "Dr. Linh",
                     SpecialtyId = sanPhuKhoaId,
                     IsVisible = true,
                     Email = "linh@clinic.vn",
                     PhoneNumber = "0915000015",
                     Gender = "Nữ",
                     YearsOfExperience = 9,
                     Bio = "Sản khoa, phụ khoa, kế hoạch hoá gia đình",
                     PhotoUrl = "/Resources/images/doctors/linh.jpg",
                     Password = defaultPasswordHash,
                     NationalId = "123456789025",
                     Address = "333 Ly Tu Trong, Q1",
                     UserId = context.Doctors.Any(doc => doc.Email == "linh@clinic.vn") ? context.Doctors.First(doc => doc.Email == "linh@clinic.vn").UserId : null
                 }
            );
            context.SaveChanges(); // Save doctors to get IDs

            // ==================================
            // 4. Seed Patients
            // ==================================
            context.Patients.AddOrUpdate(
                p => p.Email, // Use Email as identifier (if available and unique)
                              // Or use p => p.PhoneNumber if that's more reliable
                new Patient
                {
                    FullName = "Nguyễn Văn A",
                    Email = "patient01@clinic.local",
                    PhoneNumber = "0987654321",
                    UserId = patient1UserId,
                    Gender = "Nam",
                    DateOfBirth = new DateTime(1990, 1, 15),
                    Address = "10 Downing Street, London",
                    NationalId = "012345678901",
                    BloodType = "O+",
                    MedicalHistory = "Tiền sử khỏe mạnh",
                    Allergies = "Không",
                    EmergencyContactName = "Trần Thị B",
                    EmergencyContactRelationship = "Vợ",
                    EmergencyContactPhone = "0987111222",
                    CreatedAt = DateTime.UtcNow
                },
                new Patient
                {
                    FullName = "Trần Thị B",
                    Email = "patient02@clinic.local",
                    PhoneNumber = "0912345678",
                    UserId = patient2UserId,
                    Gender = "Nữ",
                    DateOfBirth = new DateTime(1992, 5, 20),
                    Address = "1600 Pennsylvania Ave NW, Washington",
                    NationalId = "098765432109",
                    BloodType = "A+",
                    MedicalHistory = "Hen suyễn",
                    Allergies = "Phấn hoa",
                    EmergencyContactName = "Nguyễn Văn A",
                    EmergencyContactRelationship = "Chồng",
                    EmergencyContactPhone = "0987654321",
                    CreatedAt = DateTime.UtcNow
                },
                 new Patient // Walk-in patient (no UserId, maybe no email)
                 {
                     FullName = "Lê Văn C (Vãng lai)",
                     Email = null,
                     PhoneNumber = "0905555111",
                     UserId = null,
                     Gender = "Nam",
                     DateOfBirth = new DateTime(1985, 11, 1),
                     Address = "Unknown",
                     NationalId = "112233445566",
                     BloodType = "B-",
                     MedicalHistory = "Viêm gan B",
                     Allergies = "Hải sản",
                     EmergencyContactName = "Phạm Thị D",
                     EmergencyContactRelationship = "Chị gái",
                     EmergencyContactPhone = "0905555222",
                     CreatedAt = DateTime.UtcNow
                 }
            );
            context.SaveChanges(); // Save patients to get IDs


            // ==================================
            // 5. Seed Working Hours
            // ==================================
            // Clear existing for seeded doctors before adding new ones
            var seededDoctorEmails = new[] {
                "an@clinic.vn", "binh@clinic.vn", "chau@clinic.vn", "quan@clinic.vn", "trang@clinic.vn",
                "van@clinic.vn", "hung@clinic.vn", "ha@clinic.vn", "minh_tm@clinic.vn", "lan@clinic.vn",
                "phong@clinic.vn", "thao@clinic.vn", "khanh_ntq@clinic.vn", "dung_ctch@clinic.vn", "linh@clinic.vn"
             };
            var seededDoctorIds = context.Doctors
                                        .Where(d => seededDoctorEmails.Contains(d.Email))
                                        .Select(d => d.Id)
                                        .ToList();

            if (seededDoctorIds.Any())
            {
                // Efficiently delete using ExecuteSqlCommand (be careful!)
                // context.Database.ExecuteSqlCommand("DELETE FROM WorkingHours WHERE DoctorId IN ({0})", string.Join(",", seededDoctorIds));
                // Or safer way: Load and RemoveRange
                var existingShifts = context.WorkingHours.Where(wh => seededDoctorIds.Contains(wh.DoctorId)).ToList();
                if (existingShifts.Any())
                {
                    context.WorkingHours.RemoveRange(existingShifts);
                    context.SaveChanges(); // Save deletion
                }
            }


            var shiftsToAdd = new System.Collections.Generic.List<WorkingHour>();
            foreach (int docId in seededDoctorIds)
            {
                // Thứ 2 đến Thứ 6 (DayOfWeek 1 đến 5)
                for (int dowInt = 1; dowInt <= 5; dowInt++)
                {
                    var dow = (DayOfWeek)dowInt;
                    // Ca sáng: 08:00 - 11:30
                    shiftsToAdd.Add(new WorkingHour { DoctorId = docId, DayOfWeek = dow, Start = new TimeSpan(8, 0, 0), End = new TimeSpan(11, 30, 0) });
                    // Ca chiều: 13:00 - 17:00
                    shiftsToAdd.Add(new WorkingHour { DoctorId = docId, DayOfWeek = dow, Start = new TimeSpan(13, 0, 0), End = new TimeSpan(17, 0, 0) });
                }
                // Thứ 7 (DayOfWeek = 6)
                shiftsToAdd.Add(new WorkingHour { DoctorId = docId, DayOfWeek = DayOfWeek.Saturday, Start = new TimeSpan(8, 0, 0), End = new TimeSpan(11, 30, 0) });
            }
            context.WorkingHours.AddRange(shiftsToAdd);
            context.SaveChanges();


            // ==================================
            // 6. Seed Appointments (Optional Example)
            // ==================================
            // Get some IDs (ensure these doctors/patients/services were seeded above)
            int drAnId = context.Doctors.FirstOrDefault(d => d.Email == "an@clinic.vn")?.Id ?? 0;
            int drTrangId = context.Doctors.FirstOrDefault(d => d.Email == "trang@clinic.vn")?.Id ?? 0;
            int patientAId = context.Patients.FirstOrDefault(p => p.Email == "patient01@clinic.local")?.Id ?? 0;
            int patientBId = context.Patients.FirstOrDefault(p => p.Email == "patient02@clinic.local")?.Id ?? 0;
            int khamDVId = context.Services.FirstOrDefault(s => s.Name == "Khám Dịch vụ")?.Id ?? 0;
            int khamBHYTId = context.Services.FirstOrDefault(s => s.Name == "Khám BHYT")?.Id ?? 0;

            // Only seed if necessary IDs were found
            if (drAnId > 0 && patientAId > 0 && khamDVId > 0)
            {
                context.Appointments.AddOrUpdate(
                    a => new { a.DoctorId, a.PatientId, a.StartTime }, // Composite identifier
                                                                       // Example Past Appointment
                    new Appointment
                    {
                        DoctorId = drAnId,
                        PatientId = patientAId,
                        ServiceId = khamDVId,
                        ExamType = ExamType.Service,
                        StartTime = DateTime.UtcNow.AddDays(-5).Date.AddHours(9), // 9 AM UTC, 5 days ago
                        EndTime = DateTime.UtcNow.AddDays(-5).Date.AddHours(9).AddMinutes(30),
                        Status = AppointmentStatus.Completed,
                        Notes = "Bệnh nhân A khám tổng quát.",
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
                    }
                 );
            }
            if (drTrangId > 0 && patientBId > 0 && khamBHYTId > 0)
            {
                context.Appointments.AddOrUpdate(
                    a => new { a.DoctorId, a.PatientId, a.StartTime },
                   // Example Future Appointment
                   new Appointment
                   {
                       DoctorId = drTrangId,
                       PatientId = patientBId,
                       ServiceId = khamBHYTId,
                       ExamType = ExamType.HealthInsurance,
                       StartTime = DateTime.UtcNow.AddDays(2).Date.AddHours(14), // 2 PM UTC, 2 days from now
                       EndTime = DateTime.UtcNow.AddDays(2).Date.AddHours(14).AddMinutes(20),
                       Status = AppointmentStatus.Confirmed,
                       Notes = "Bệnh nhân B khám BHYT.",
                       CreatedAt = DateTime.UtcNow.AddDays(-1)
                   }
                );
            }
            context.SaveChanges(); // Final save
        }
    }
}
