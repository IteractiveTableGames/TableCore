using Godot;

namespace TableCore.Core.Input
{
    public static class InputRoutingHelper
    {
        public static int? ResolvePlayerIndex(SessionState? session, Vector2? position)
        {
            if (session?.PlayerProfiles == null || position == null)
            {
                return null;
            }

            for (var index = 0; index < session.PlayerProfiles.Count; index++)
            {
                var player = session.PlayerProfiles[index];

                if (player?.Seat?.ScreenRegion.HasPoint(position.Value) == true)
                {
                    return index;
                }
            }

            return null;
        }
    }
}
