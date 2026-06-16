using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.Models;

public class AppSetting
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
