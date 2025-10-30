using System;
using System.Collections.Generic;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class CurrencyBankTests
    {
        [Test]
        public void GetBalance_ReturnsZeroForUnknownPlayer()
        {
            var bank = new CurrencyBank();

            Assert.That(bank.GetBalance(Guid.NewGuid()), Is.EqualTo(0));
        }

        [Test]
        public void Add_UpdatesBalance_AndRaisesEvent()
        {
            var bank = new CurrencyBank();
            var player = Guid.NewGuid();
            var events = new List<(Guid Player, int Balance)>();
            bank.BalanceChanged += (id, balance) => events.Add((id, balance));

            var balance = bank.Add(player, 200);

            Assert.That(balance, Is.EqualTo(200));
            Assert.That(bank.GetBalance(player), Is.EqualTo(200));
            Assert.That(events, Is.EqualTo(new List<(Guid, int)> { (player, 200) }));
        }

        [Test]
        public void SetBalance_OverridesExistingBalance()
        {
            var bank = new CurrencyBank();
            var player = Guid.NewGuid();

            bank.Add(player, 100);
            bank.SetBalance(player, 350);

            Assert.That(bank.GetBalance(player), Is.EqualTo(350));
        }

        [Test]
        public void Transfer_SucceedsWhenFundsAvailable()
        {
            var bank = new CurrencyBank();
            var from = Guid.NewGuid();
            var to = Guid.NewGuid();
            var observedEvents = new List<(Guid Player, int Balance)>();
            bank.BalanceChanged += (id, balance) => observedEvents.Add((id, balance));

            bank.SetBalance(from, 500);
            bank.SetBalance(to, 75);

            var transferSucceeded = bank.Transfer(from, to, 125);

            Assert.That(transferSucceeded, Is.True);
            Assert.That(bank.GetBalance(from), Is.EqualTo(375));
            Assert.That(bank.GetBalance(to), Is.EqualTo(200));
            Assert.That(observedEvents, Does.Contain((from, 375)));
            Assert.That(observedEvents, Does.Contain((to, 200)));
        }

        [Test]
        public void Transfer_FailsWhenInsufficientFunds()
        {
            var bank = new CurrencyBank();
            var from = Guid.NewGuid();
            var to = Guid.NewGuid();

            bank.SetBalance(from, 50);

            var transferSucceeded = bank.Transfer(from, to, 100);

            Assert.That(transferSucceeded, Is.False);
            Assert.That(bank.GetBalance(from), Is.EqualTo(50));
            Assert.That(bank.GetBalance(to), Is.EqualTo(0));
        }

        [Test]
        public void Transfer_WithNegativeAmount_Throws()
        {
            var bank = new CurrencyBank();

            Assert.That(() => bank.Transfer(Guid.NewGuid(), Guid.NewGuid(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Transfer_SamePlayer_Ignored()
        {
            var bank = new CurrencyBank();
            var player = Guid.NewGuid();
            bank.SetBalance(player, 100);

            var succeeded = bank.Transfer(player, player, 50);

            Assert.That(succeeded, Is.True);
            Assert.That(bank.GetBalance(player), Is.EqualTo(100));
        }

        [Test]
        public void Reset_ClearsAllBalances()
        {
            var bank = new CurrencyBank();
            bank.Add(Guid.NewGuid(), 10);
            bank.Add(Guid.NewGuid(), 15);

            bank.Reset();

            Assert.That(bank.GetBalance(Guid.NewGuid()), Is.EqualTo(0));
        }
    }
}
