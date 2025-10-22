using System;
using System.ComponentModel.DataAnnotations;

namespace CMCSApp.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required, StringLength(100)]
        public string LecturerName { get; set; }

        [Required]
        public int HoursWorked { get; set; }

        [Required, DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        // stored relative to wwwroot/uploads
        public string DocumentFileName { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}