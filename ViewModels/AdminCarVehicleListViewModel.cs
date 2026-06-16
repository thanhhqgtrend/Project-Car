using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class AdminCarVehicleListViewModel
{
    public List<CarVehicleType> Vehicles { get; set; } = [];
    public string Search { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
}
