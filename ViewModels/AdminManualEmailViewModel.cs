using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.ViewModels;

public class AdminManualEmailViewModel
{
    [Required, StringLength(220)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(8000)]
    public string BodyHtml { get; set; } = string.Empty;
}
