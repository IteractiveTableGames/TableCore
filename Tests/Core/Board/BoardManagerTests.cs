using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.Board;

namespace TableCore.Tests.Core.Board
{
    [TestFixture]
    public class BoardManagerTests
    {
        [Test]
        public void PlaceToken_UsesLocalConverterForWorldPositions()
        {
            var boardOffset = new Vector2(400, 300);
            var positions = new Dictionary<TokenController, Vector2>();
            var manager = CreateManager(positions, world => world - boardOffset);

            var token = CreateToken();
            var playerId = Guid.NewGuid();
            var worldPosition = boardOffset + new Vector2(25, -10);

            manager.PlaceToken(playerId, token, new BoardLocation(string.Empty, worldPosition));

            Assert.That(positions[token], Is.EqualTo(new Vector2(25, -10)));
        }

        [Test]
        public async Task MoveToken_AppliesConverterToEachStep()
        {
            var boardOffset = new Vector2(-120, 85);
            var positions = new Dictionary<TokenController, Vector2>();
            var manager = CreateManager(positions, world => world - boardOffset);

            var token = CreateToken();
            var playerId = Guid.NewGuid();
            manager.PlaceToken(playerId, token, new BoardLocation(string.Empty, boardOffset));

            var path = new BoardPath(new[]
            {
                new BoardLocation(string.Empty, boardOffset + new Vector2(10, 0)),
                new BoardLocation(string.Empty, boardOffset + new Vector2(20, 5))
            });

            await manager.MoveToken(playerId, token, path);

            Assert.That(positions[token], Is.EqualTo(new Vector2(20, 5)));
        }

        [Test]
        public void PlaceToken_DistributesTokensSharingTile()
        {
            var positions = new Dictionary<TokenController, Vector2>();
            var manager = CreateManager(positions, world => world);

            var playerOne = Guid.NewGuid();
            var playerTwo = Guid.NewGuid();
            var sharedLocation = new BoardLocation("Markers/Start");

            var tokenOne = CreateToken();
            var tokenTwo = CreateToken();

            manager.PlaceToken(playerOne, tokenOne, sharedLocation);
            manager.PlaceToken(playerTwo, tokenTwo, sharedLocation);

            var distance = positions[tokenOne].DistanceTo(positions[tokenTwo]);
            var midpoint = (positions[tokenOne] + positions[tokenTwo]) / 2f;

            Assert.That(distance, Is.GreaterThanOrEqualTo(30f));
            Assert.That(midpoint.X, Is.EqualTo(0f).Within(0.001f));
            Assert.That(midpoint.Y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public async Task MoveToken_ReleasesSpaceOnPreviousTile()
        {
            var positions = new Dictionary<TokenController, Vector2>();
            var manager = CreateManager(positions, world => world);

            var playerOne = Guid.NewGuid();
            var playerTwo = Guid.NewGuid();
            var sharedLocation = new BoardLocation("Markers/Start");
            var destination = new BoardLocation("Markers/End", new Vector2(100, 0));

            var tokenOne = CreateToken();
            var tokenTwo = CreateToken();

            manager.PlaceToken(playerOne, tokenOne, sharedLocation);
            manager.PlaceToken(playerTwo, tokenTwo, sharedLocation);

            await manager.MoveToken(playerOne, tokenOne, new BoardPath(new[] { destination }));

            Assert.That(positions[tokenTwo], Is.EqualTo(Vector2.Zero));
            Assert.That(positions[tokenOne], Is.EqualTo(destination.Offset));
        }

        private static BoardManager CreateManager(
            IDictionary<TokenController, Vector2> positions,
            Func<Vector2, Vector2> toLocal)
        {
            Vector2 GetPosition(TokenController token)
            {
                return positions.TryGetValue(token, out var value) ? value : Vector2.Zero;
            }

            return new BoardManager(
                new AnimationService(null),
                (_, world) => toLocal(world),
                GetPosition,
                (token, value) => positions[token] = value,
                (token, _, to, _) =>
                {
                    positions[token] = to;
                    return Task.CompletedTask;
                });
        }

        private static TokenController CreateToken()
        {
            return (TokenController)RuntimeHelpers.GetUninitializedObject(typeof(TokenController));
        }
    }
}
