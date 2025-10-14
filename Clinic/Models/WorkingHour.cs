using System;

namespace Clinic.Models
{
    public class WorkingHour
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public DayOfWeek DayOfWeek { get; set; } // Mon=1 … Sun=0
        public TimeSpan Start { get; set; }      // 08:00
        public TimeSpan End { get; set; }        // 17:00
    }
}
