using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Private_Clinic.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string Name { get; set; }       // Tên bác sĩ
        public string Specialty { get; set; }  // Chuyên khoa
        public string Avatar { get; set; }     // (tùy chọn) link ảnh
    }
}