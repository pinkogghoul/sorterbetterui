using System.Collections.Generic;

namespace LogSummaryApp.Models
{
    public class LogSummary
    {
        public int TotalAccounts { get; set; }
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
        public Dictionary<string, CategoryCount> SkinCounts { get; set; } = new Dictionary<string, CategoryCount>();
        public Dictionary<string, CategoryCount> VbucksCounts { get; set; } = new Dictionary<string, CategoryCount>();
        public Dictionary<string, CategoryCount> SkinrangeCounts { get; set; } = new Dictionary<string, CategoryCount>();
    }
}
