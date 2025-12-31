namespace WebAPI.Models
{
    public class ScheduleEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Music";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string? Host { get; set; }
        public string? Guest { get; set; }
        public string? Studio { get; set; }

      
        public int? HostContributorId { get; set; }
        public Contributor? HostContributor { get; set; }

        public int? GuestContributorId { get; set; }
        public Contributor? GuestContributor { get; set; }
    }
}
