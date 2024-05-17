using System.ComponentModel.DataAnnotations;

namespace Pustok.ViewModels
{
    public class MemberLoginViewModel
    {
        [MaxLength(25)]
        [MinLength(5)]
        [Required]
        [EmailAddress]
        public string Email { get; set; }
      
        [MaxLength(25)]
        [MinLength(8)]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
     
    }
}
