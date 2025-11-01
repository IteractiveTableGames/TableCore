using System;
using System.Collections.Generic;

namespace TableCore.Core
{
    public class ModuleDescriptor
    {
        public string ModuleId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 4;
        public string ModulePath { get; set; } = string.Empty;
        public string? IconPath { get; set; }
        public string? EntryScenePath { get; set; }

        public Dictionary<string, object> Capabilities { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool SupportsPlayerCount(int playerCount)
        {
            return playerCount >= MinPlayers && playerCount <= Math.Max(MinPlayers, MaxPlayers);
        }
    }
}
