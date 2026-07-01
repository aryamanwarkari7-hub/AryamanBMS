using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class GeneratePtSnapshotInput
    {
        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Range(2000, 2100)]
        public int Year { get; set; }
    }
}