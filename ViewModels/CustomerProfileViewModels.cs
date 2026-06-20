using System.ComponentModel.DataAnnotations;

namespace LuxuryCar.ViewModels
{
    public class CustomerProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(80)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(80)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [Display(Name = "Country code")]
        public string? PhoneCountryCode { get; set; }

        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public class CustomerChangePasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a new password.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}