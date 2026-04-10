namespace DiveIntoIVE.Models
{
    public class MemberProfile
    {
        public int Id { get; set; }
        public string MemberKey { get; set; } = "";
        public string Name { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
        public string Tagline { get; set; } = "";
        public string Bio { get; set; } = "";
        public string Accent { get; set; } = "#9F7AEA";
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
