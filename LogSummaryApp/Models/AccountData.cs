using System.Collections.Generic;

namespace LogSummaryApp.Models
{
    public class AccountData
    {
        public HashSet<string> FoundItems { get; set; } = new HashSet<string>();
        public int VBucks { get; set; } = 0;
        public bool SeenActive { get; set; } = false;
        public bool SeenInactive { get; set; } = false;
        public List<int> DeclaredSkinCounts { get; set; } = new List<int>();
        public string Status { get; set; } = "active";
    }
}
