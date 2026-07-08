using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your corporate email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid corporate email address.")]
        [Display(Name = "Corporate Email Address")]
        public string Email { get; set; } = string.Empty;
    }
}