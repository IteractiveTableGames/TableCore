using System.Collections.Generic;

namespace TableCore.Core
{
    public class SessionState
    {
        public List<PlayerProfile> PlayerProfiles { get; set; } = new List<PlayerProfile>();
        public ModuleDescriptor? SelectedModule { get; set; }
        public Dictionary<string, object> SessionOptions { get; set; } = new Dictionary<string, object>();
    }
}