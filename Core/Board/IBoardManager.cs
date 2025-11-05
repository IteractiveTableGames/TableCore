using System;
using System.Threading.Tasks;

namespace TableCore.Core.Board
{
    /// <summary>
    /// Abstraction for placing and animating tokens on a module-defined board.
    /// </summary>
    public interface IBoardManager
    {
        void SetBoardRoot(Godot.Node2D boardRoot);

        void PlaceToken(Guid playerId, TokenController token, BoardLocation location);

        Task MoveToken(Guid playerId, TokenController token, BoardPath path);

        BoardLocation? GetLocation(TokenController token);

        Godot.Vector2 GetWorldPosition(BoardLocation location);
    }
}
