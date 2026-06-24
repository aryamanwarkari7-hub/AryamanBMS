using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class CityModel
    {
        public int Id { get; set; }

        [Required]
        public int StateId { get; set; }

        [Required]
        [StringLength(150)]
        public string CityName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public StateModel? State { get; set; }

        public ICollection<PincodeModel> Pincodes { get; set; }
            = new List<PincodeModel>();
    }
}