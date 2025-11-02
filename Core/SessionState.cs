using System.Collections.Generic;

namespace TableCore.Core
{
    public class SessionState
    {
        public List<PlayerProfile> PlayerProfiles { get; set; } = new List<PlayerProfile>();
        public ModuleDescriptor? SelectedModule { get; set; }
        public Dictionary<string, object> SessionOptions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Produces a snapshot of the current session, cloning players and module metadata.
        /// </summary>
        public SessionState Clone()
        {
            var snapshot = new SessionState
            {
                PlayerProfiles = new List<PlayerProfile>(),
                SessionOptions = new Dictionary<string, object>()
            };

            foreach (var profile in PlayerProfiles)
            {
                if (profile != null)
                {
                    snapshot.PlayerProfiles.Add(profile.Clone());
                }
            }

            snapshot.SelectedModule = SelectedModule?.Clone();

            foreach (var option in SessionOptions)
            {
                snapshot.SessionOptions[option.Key] = option.Value;
            }

            return snapshot;
        }
    }
}
