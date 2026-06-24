using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class StateModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string StateName { get; set; } = string.Empty;

        [StringLength(10)]
        public string? StateCode { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<CityModel> Cities { get; set; }
            = new List<CityModel>();
    }
}