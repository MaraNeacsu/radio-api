namespace WebAPI.Models
{
    public class Contributor
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

       
        public string StageName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? PhotoUrl { get; set; }

       
        public decimal HourlyRate { get; set; } = 750m;

       
        public string? UserId { get; set; }
        public AplicationUser? User { get; set; }

     
        public ICollection<ScheduleEvent> HostedEvents { get; set; } = new List<ScheduleEvent>();
        public ICollection<ScheduleEvent> GuestEvents { get; set; } = new List<ScheduleEvent>();
        public ICollection<PaymentRecord> Payments { get; set; } = new List<PaymentRecord>();
    }
}
