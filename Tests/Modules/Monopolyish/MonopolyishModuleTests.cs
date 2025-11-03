using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.Modules;
using TableCore.Modules.Monopolyish;

namespace TableCore.Tests.Modules.Monopolyish
{
    [TestFixture]
    public class MonopolyishModuleTests
    {
        [Test]
        public void MonopolyishModule_ImplementsGameModuleContract()
        {
            Assert.That(typeof(IGameModule).IsAssignableFrom(typeof(MonopolyishModule)), Is.True);
        }

        [Test]
        public void ModuleDescriptor_IsDiscoverableByLoader()
        {
            var repoRoot = GetRepositoryRoot();
            var modulesPath = Path.Combine(repoRoot, "Modules");
            Assert.That(Directory.Exists(modulesPath), Is.True, "Modules directory missing from repository.");

            var loader = new ModuleLoader();
            var descriptors = loader.LoadModules(modulesPath);

            Assert.That(descriptors, Has.Some.Matches<ModuleDescriptor>(descriptor =>
                string.Equals(descriptor.ModuleId, "com.tablecore.monopolyish", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(descriptor.EntryScenePath, "MonopolyishModule.tscn", StringComparison.OrdinalIgnoreCase)));
        }

        private static string GetRepositoryRoot()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            return Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        }

        private sealed class StubModuleServices : IModuleServices
        {
            public IReadOnlyList<PlayerProfile> GetPlayers() => Array.Empty<PlayerProfile>();
            public TurnManager GetTurnManager() => new();
            public DiceService GetDiceService() => new();
            public CurrencyBank GetBank() => new();
            public CardService GetCardService() => new();
            public IHUDService GetHUDService() => new StubHudService();
            public AnimationService GetAnimationService() => new AnimationService(null);
            public SessionState GetSessionState() => new();
            public void ReturnToLobby()
            {
            }
        }

        private sealed class StubHudService : IHUDService
        {
            public IPlayerHUD CreatePlayerHUD(PlayerProfile player) => new StubPlayerHud();
            public void UpdateFunds(Guid playerId, int newAmount) { }
            public void UpdateHand(Guid playerId, IReadOnlyList<CardData> cards) { }
            public void SetPrompt(Guid playerId, string message) { }
        }

        private sealed class StubPlayerHud : IPlayerHUD
        {
            public Guid PlayerId => Guid.Empty;
            public Control GetRootControl() => default!;
            public void AddControl(Node controlNode) { }
        }
    }
}
