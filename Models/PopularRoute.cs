using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class PopularRoute
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(80)]
    public string PriceLabel { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(320)]
    public string BookingUrl { get; set; } = "/booking/search";

    [MaxLength(320)]
    public string OriginAddress { get; set; } = string.Empty;

    [MaxLength(320)]
    public string DestinationAddress { get; set; } = string.Empty;
    [MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string BodyHtml { get; set; } = string.Empty;

    [MaxLength(320)]
    public string HighlightOne { get; set; } = string.Empty;

    [MaxLength(320)]
    public string HighlightTwo { get; set; } = string.Empty;

    [MaxLength(320)]
    public string HighlightThree { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MediaAssetId { get; set; }

    public MediaAsset? Image { get; set; }
}