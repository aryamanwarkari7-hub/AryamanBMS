using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your new password.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Add this missing property so Identity can validate the security token from the email link!
        public string Token { get; set; } = string.Empty;
    }
}