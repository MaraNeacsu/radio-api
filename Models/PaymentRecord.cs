using System.Text.Json.Serialization;

namespace WebAPI.Models
{
    public class PaymentRecord
    {
        public int Id { get; set; }

        public int ContributorId { get; set; }

        [JsonIgnore]              
        public Contributor? Contributor { get; set; }

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
    }
}
