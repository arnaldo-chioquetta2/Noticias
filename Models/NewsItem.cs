using System;

namespace NewsImpactRanker.WinForms.Models
{
    public class NewsItem
    {
        public double ImpactScore { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public string ImpactReason { get; set; }
        public string Status { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string RawText { get; set; }
        public string TextHash { get; set; }
    }
}
