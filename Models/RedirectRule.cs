using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class RedirectRule
{
    public int Id { get; set; }

    [MaxLength(240)]
    public string SourcePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string TargetUrl { get; set; } = string.Empty;

    public int StatusCode { get; set; } = 301;

    public bool IsActive { get; set; } = true;

    [MaxLength(240)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAtUtc { get; set; }
}
