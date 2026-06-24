using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class PincodeModel
    {
        public int Id { get; set; }

        [Required]
        public int CityId { get; set; }

        [Required]
        [StringLength(6)]
        public string Pincode { get; set; } = string.Empty;

        [StringLength(150)]
        public string? AreaName { get; set; }

        public bool IsActive { get; set; } = true;

        public CityModel? City { get; set; }
    }
}