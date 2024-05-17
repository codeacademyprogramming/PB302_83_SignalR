using System.ComponentModel.DataAnnotations;

namespace Pustok.ViewModels
{
    public class ForgetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        [MinLength(3)]
        public string Email { get; set; }
    }
}
