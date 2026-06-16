using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class Airport
{
    public int Id { get; set; }

    [MaxLength(12)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Country { get; set; } = "Vietnam";

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public bool IsActive { get; set; } = true;
}
