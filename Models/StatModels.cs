namespace DiveIntoIVE.Models
{
    public class YoutubeStat
    {
        public string Song { get; set; } = "";
        public DateTime Date { get; set; }
        public int Views { get; set; }
    }

    public class SpotifyStat
    {
        public string Song { get; set; } = "";
        public DateTime Date { get; set; }
        public int Streams { get; set; }
    }

    public class SocialStat
    {
        public string Platform { get; set; } = "";
        public DateTime Date { get; set; }
        public int Followers { get; set; }
    }
}
