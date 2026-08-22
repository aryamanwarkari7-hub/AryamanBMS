using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class CountryModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string CountryName { get; set; } = string.Empty;

        [Required]
        [StringLength(2)]
        public string Iso2Code { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string Iso3Code { get; set; } = string.Empty;

        [StringLength(3)]
        public string? DefaultCurrencyCode { get; set; }

        [StringLength(10)]
        public string? PhoneCode { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}