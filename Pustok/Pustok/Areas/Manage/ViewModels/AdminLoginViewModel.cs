using System.ComponentModel.DataAnnotations;

namespace Pustok.Areas.Manage.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required]
        [MinLength(5)]
        [MaxLength(25)]
        public string UserName { get; set; }
        [Required]
        [MinLength(8)]
        [MaxLength(25)]
        [DataType(DataType.Password)]
        public string Password { get; set; }  
        public bool RememberMe { get; set; }
    }
}
