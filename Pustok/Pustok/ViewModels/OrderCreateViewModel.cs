using System.ComponentModel.DataAnnotations;

namespace Pustok.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [MaxLength(250)]
        public string Address { get; set; }
        [Required]
        [MaxLength(30)]
        public string Phone { get; set; }
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
