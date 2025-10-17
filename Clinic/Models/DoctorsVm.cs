using System;
using System.Collections.Generic;

namespace Clinic.Models
{
    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public IEnumerable<T> Items { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }

    public class DoctorsFilterVm
    {
        public string Query { get; set; }           // tìm theo tên
        public string Specialty { get; set; }       // lọc theo khoa (string)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 6;
    }

    public class DoctorsIndexVm
    {
        public DoctorsFilterVm Filter { get; set; }
        public IEnumerable<string> Specialties { get; set; }  // danh sách khoa duy nhất
        public PagedResult<Doctor> Result { get; set; }
    }
}
