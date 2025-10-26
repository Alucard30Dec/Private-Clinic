using System;
using System.Collections.Generic;
using System.Linq;

namespace Clinic.Models
{
    public class AppointmentService
    {
        private readonly ClinicDbContext _db;
        public AppointmentService(ClinicDbContext db) { _db = db; }

        public bool IsSlotAvailable(int doctorId, DateTime startUtc, DateTime endUtc)
        {
            return !_db.Appointments.Any(a =>
                a.DoctorId == doctorId &&
                a.Status != AppointmentStatus.Canceled &&
                a.StartTime < endUtc && startUtc < a.EndTime
            );
        }

        public Appointment Create(int doctorId, int serviceId, int PatientId,
                                  DateTime startUtc, DateTime endUtc, string notes)
        {
            var appt = new Appointment
            {
                DoctorId = doctorId,
                ServiceId = serviceId,
                PatientId = PatientId,
                StartTime = startUtc,
                EndTime = endUtc,
                Status = AppointmentStatus.Confirmed, // Code gốc của bạn đã tự gán Confirmed
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };
            _db.Appointments.Add(appt);
            _db.SaveChanges();
            return appt;
        }

        // === SỬA LỖI LOGIC THỜI GIAN THỰC TẾ TRONG HÀM NÀY ===
        public List<DateTime> SuggestSlots(int doctorId, DateTime fromLocal, int days = 7, int minutes = 30)
        {
            var list = new List<DateTime>();
            var toLocal = fromLocal.Date.AddDays(days);

            // Lấy thời gian hiện tại (ví dụ: 4:46 PM)
            var now = fromLocal;

            for (var day = fromLocal.Date; day < toLocal; day = day.AddDays(1))
            {
                var s = day.AddHours(9); // Bắt đầu từ 9h sáng
                var e = day.AddHours(17); // Kết thúc lúc 5h chiều (17:00)

                for (var t = s; t.AddMinutes(minutes) <= e; t = t.AddMinutes(minutes))
                {
                    
                    if (t < now)
                    {
                        continue; // Bỏ qua khung giờ quá khứ
                    }
                    // === KẾT THÚC SỬA ĐỔI ===

                    var startUtc = t.ToUniversalTime();
                    var endUtc = startUtc.AddMinutes(minutes);

                    if (IsSlotAvailable(doctorId, startUtc, endUtc))
                        list.Add(t);
                }
            }
            return list;
        }
    }
}