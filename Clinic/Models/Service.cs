// Models/Service.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Service
{
    public int Id { get; set; }
    [Required] public string Name { get; set; }

    [Column(TypeName = "decimal")]          // <-- chỉ 'decimal'
    public decimal Fee { get; set; }

    public int DurationMinutes { get; set; }
}
