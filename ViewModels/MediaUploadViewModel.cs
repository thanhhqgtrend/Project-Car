using System.ComponentModel.DataAnnotations;
using System.Web;

namespace LuxuryCar.ViewModels;

public class MediaUploadViewModel
{
    [Required]
    public HttpPostedFileBase? File { get; set; }

    [StringLength(220)]
    public string AltText { get; set; } = string.Empty;
}
