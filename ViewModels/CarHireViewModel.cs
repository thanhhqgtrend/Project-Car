using LuxuryCar.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace LuxuryCar.ViewModels;

public class CarHireViewModel
{
    public DateTime StartDate { get; set; } = DateTime.Now.AddDays(1).Date;
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(2).Date;
    public string PickupAddress { get; set; } = string.Empty;
    public int? CarVehicleTypeId { get; set; }
    public int PassengerCount { get; set; } = 1;

    public List<CarVehicleType> Vehicles { get; set; } = new();
    public List<SelectListItem> VehicleOptions { get; set; } = new();

    public int TotalDays => Math.Max(1, (EndDate.Date - StartDate.Date).Days);
    public decimal DailyRate => SelectedVehicle?.DailyRateUsd ?? 0;
    public decimal TotalPrice => DailyRate * TotalDays;

    public CarVehicleType? SelectedVehicle { get; set; }
    public string GeoapifyApiKey { get; set; } = string.Empty;
}