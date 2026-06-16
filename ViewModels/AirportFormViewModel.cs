using System.ComponentModel.DataAnnotations;
using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AirportFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(12)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Country { get; set; } = "Vietnam";

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public static AirportFormViewModel FromEntity(Airport airport) => new()
    {
        Id = airport.Id,
        Code = airport.Code,
        Name = airport.Name,
        City = airport.City,
        Country = airport.Country,
        Latitude = airport.Latitude,
        Longitude = airport.Longitude,
        IsActive = airport.IsActive
    };

    public void ApplyTo(Airport airport)
    {
        airport.Code = Code.Trim().ToUpperInvariant();
        airport.Name = Name.Trim();
        airport.City = City.Trim();
        airport.Country = Country.Trim();
        airport.Latitude = Latitude;
        airport.Longitude = Longitude;
        airport.IsActive = IsActive;
    }
}
