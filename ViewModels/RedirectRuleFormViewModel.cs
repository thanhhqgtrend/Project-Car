using System.ComponentModel.DataAnnotations;
using LuxuryCar.Models;

namespace LuxuryCar.ViewModels;

public class RedirectRuleFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(240)]
    public string SourcePath { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string TargetUrl { get; set; } = string.Empty;

    [Range(301, 302)]
    public int StatusCode { get; set; } = 301;

    public bool IsActive { get; set; } = true;

    [StringLength(240)]
    public string Notes { get; set; } = string.Empty;

    public static RedirectRuleFormViewModel FromEntity(RedirectRule rule) =>
        new()
        {
            Id = rule.Id,
            SourcePath = rule.SourcePath,
            TargetUrl = rule.TargetUrl,
            StatusCode = rule.StatusCode,
            IsActive = rule.IsActive,
            Notes = rule.Notes
        };

    public void ApplyTo(RedirectRule rule)
    {
        rule.SourcePath = SourcePath;
        rule.TargetUrl = TargetUrl;
        rule.StatusCode = StatusCode is 302 ? 302 : 301;
        rule.IsActive = IsActive;
        rule.Notes = Notes?.Trim() ?? string.Empty;
        rule.UpdatedAtUtc = DateTime.UtcNow;
    }
}
