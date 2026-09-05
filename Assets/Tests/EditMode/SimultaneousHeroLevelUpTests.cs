using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35u. Rulebook p.15 levels every hero at the same time, and this is the fixture that
    /// says so.
    ///
    /// <para>The page's own word is "simultaneously", and the code has said since milestone 7.5 that its
    /// hero-id ordering was "a serialization of the page's simultaneity rather than a rule about who goes
    /// first". This milestone removed the serialization; what follows is what that has to mean in
    /// practice - every hero that bought a level is asked at once, each pause is owned by its own seat,
    /// a hero with nothing to level is still never asked, and the round does not move on until the LAST
    /// answer lands.</para>
    ///
    /// <para><b>The multi-hero state is rules-produced, not staged.</b> p.8's Collect a Pity Coin gives
    /// every hero that did not level exactly one coin, and p.15's table prices the first level at one -
    /// so the round after a round nobody could level in is a round in which everybody must. That is the
    /// natural shape of this game, and it is why simultaneous leveling is the ordinary case rather than
    /// an edge one.</para>
    /// </summary>
    public sealed class SimultaneousHeroLevelUpTests
    {
        [Test]
        public void EveryHeroThatBoughtALevel_IsAskedItsUpgradeAtTheSameTime()
        {
            var session = CreateSessionWhereEveryHeroCanLevel();
            var pending = PauseTheLevelUpStage(session);

            Assert.That(pending.IsConcurrent, Is.True,
                "p.15 levels heroes simultaneously; a pause holding one item is the serialization " +
                "milestone 8.35u removed.");
            Assert.That(pending.OpenItems.Count, Is.EqualTo(2),
                "Both heroes collected a pity coin last round and p.15 prices the first level at one.");

            var heroes = pending.OpenItems
                .Select(open => HeroLevelUpItem.Read(open.ItemId).HeroId)
                .ToArray();
            Assert.That(heroes.Distinct().Count(), Is.EqualTo(heroes.Length),
                "One item per hero, never one per level: a hero finishes each level before buying the " +
                "next, so it holds one open question at a time.");

            foreach (var open in pending.OpenItems)
            {
                var item = HeroLevelUpItem.Read(open.ItemId);
                Assert.That(item.Kind, Is.EqualTo(HeroLevelUpItemKind.Upgrade));
                Assert.That(open.Options, Is.Not.Empty,
                    "An open item with nothing to choose is not a question.");
                Assert.That(open.ChoosingSide,
                    Is.EqualTo(session.Projection.Participants.GetTeamForHero(item.HeroId).Side));

                // Every option names cards this hero owns. That is what makes another hero's option
                // list a statement about another hero's HAND, and so owner-private.
                foreach (var optionId in open.Options)
                {
                    var option = HeroLevelUpOption.Read(optionId);
                    Assert.That(OwnerOf(session, option.UpgradedCardInstanceId), Is.EqualTo(item.HeroId));
                    Assert.That(OwnerOf(session, option.TakenCardInstanceId), Is.EqualTo(item.HeroId));
                }
            }

            // The two heroes are on opposite sides here, which is the case a team-scoped reading of
            // "the chooser" cannot represent at all: there is no one side the authority is waiting on.
            Assert.That(pending.OpenItems.Select(open => open.ChoosingSide).Distinct().Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void AnsweringOneHerosUpgrade_LeavesTheOtherStandingAndTheRoundWhereItIs()
        {
            var session = CreateSessionWhereEveryHeroCanLevel();
            var pending = PauseTheLevelUpStage(session);
            var answeredHero = HeroLevelUpItem.Read(pending.OpenItems[0].ItemId).HeroId;
            var waitingHero = HeroLevelUpItem.Read(pending.OpenItems[1].ItemId).HeroId;

            Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                FirstInvocationFor(pending, answeredHero)));

            var afterOne = session.Projection.PendingSystemTransitionStep;
            Assert.That(afterOne, Is.Not.Null,
                "p.15 has the heroes wait for each other. The transition advances when the SET empties, " +
                "never when the first answer lands - which is also why a disconnected player cannot be " +
                "skipped: nothing about their absence empties the set.");
            Assert.That(OpenHeroes(afterOne), Has.Member(waitingHero));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.LevelUpAndUpgrade),
                "A stage that stopped on a chooser stays where it is until every answer arrives.");

            Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                FirstInvocationFor(afterOne, waitingHero)));
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null,
                "The LAST answer is what lets the stage complete.");
        }

        [Test]
        public void TheOutstandingSet_IsReproducedWhicheverOrderItIsAnsweredIn()
        {
            // R4. Every fixture written before this milestone answered in hero-id order, because that
            // is the order the old serialization produced - so an order-dependent re-derivation would
            // have passed all of them. This one deliberately answers backwards.
            var forwards = CreateSessionWhereEveryHeroCanLevel();
            var forwardsPending = PauseTheLevelUpStage(forwards);
            var first = HeroLevelUpItem.Read(forwardsPending.OpenItems[0].ItemId).HeroId;
            var second = HeroLevelUpItem.Read(forwardsPending.OpenItems[1].ItemId).HeroId;

            var backwards = CreateSessionWhereEveryHeroCanLevel();
            var backwardsPending = PauseTheLevelUpStage(backwards);

            // The same two answers, in opposite orders. Choosing by OPTION rather than by index is
            // what makes the two runs the same decision: an index would be a different choice.
            var forwardsFirstOption = OptionFor(forwardsPending, first);
            var forwardsSecondOption = OptionFor(forwardsPending, second);

            AnswerByOption(forwards, first, forwardsFirstOption);
            var midway = OpenHeroes(forwards.Projection.PendingSystemTransitionStep);
            AnswerByOption(forwards, second, forwardsSecondOption);

            AnswerByOption(backwards, second, forwardsSecondOption);
            Assert.That(OpenHeroes(backwards.Projection.PendingSystemTransitionStep),
                Is.Not.EqualTo(midway),
                "The two runs really did answer in different orders; if they had not, this fixture " +
                "would be asserting nothing.");
            AnswerByOption(backwards, first, forwardsFirstOption);

            Assert.That(backwards.Projection.PendingSystemTransitionStep, Is.Null);
            Assert.That(forwards.Projection.PendingSystemTransitionStep, Is.Null);
            Assert.That(LevelsAndCoins(backwards), Is.EqualTo(LevelsAndCoins(forwards)),
                "Nothing one hero's purchase does can change what another's offers - the cards, the " +
                "coins and the Level are all that hero's own - so the order two independent answers " +
                "arrive in cannot change where the match ends up.");
        }

        [Test]
        public void APartiallyAnsweredSet_RestoresToTheSameRemainingItemsAndSpendsNothingDoingIt()
        {
            var session = CreateSessionWhereEveryHeroCanLevel();
            var pending = PauseTheLevelUpStage(session);
            var answeredHero = HeroLevelUpItem.Read(pending.OpenItems[0].ItemId).HeroId;
            Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                FirstInvocationFor(pending, answeredHero)));

            var partially = session.Projection.AuthoritativeSnapshot;
            var remaining = session.Projection.PendingSystemTransitionStep;
            Assert.That(remaining, Is.Not.Null);

            var restored = ConfiguredMatchFixtures.CreateRestoreTarget();
            Assert.That(restored.RestoreSnapshot(partially).Accepted, Is.True);

            // THE INVARIANT THIS MILESTONE CREATED, asserted rather than argued. Restore re-runs the
            // level-up rules to verify the persisted pause, and those rules can now reach the purchase
            // loop - which spends coins. It is a pure query in exactly the state a pause can exist in,
            // because a pause is published only once no hero without an outstanding purchase can buy
            // anything. A byte-identical payload is that argument, checked.
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(partially),
                "Verifying a restored pause must not buy a level, upgrade a card or spend a coin.");

            var restoredPending = restored.Projection.PendingSystemTransitionStep;
            Assert.That(restoredPending, Is.Not.Null);
            Assert.That(OpenHeroes(restoredPending), Is.EqualTo(OpenHeroes(remaining)),
                "CompletedItemIds and OpenItems together are the whole of a partly answered set.");
            Assert.That(restoredPending.CompletedItemIds.Select(value => value.Value),
                Is.EqualTo(remaining.CompletedItemIds.Select(value => value.Value)));

            // And the restored match finishes the set through the same commands.
            foreach (var open in restoredPending.OpenItems.ToArray())
            {
                var live = restored.Projection.PendingSystemTransitionStep;
                Accept(restored, DirectorCommand.AnswerSystemTransitionStep(
                    live.Invocations.First(invocation => invocation.ItemId.Equals(open.ItemId))));
            }

            Assert.That(restored.Projection.PendingSystemTransitionStep, Is.Null);
        }

        [Test]
        public void TheDirectorIsOfferedEveryOpenItemAtOnceAndAnswersEachDirectly()
        {
            var session = CreateSessionWhereEveryHeroCanLevel();
            var pending = PauseTheLevelUpStage(session);

            var contract = session.Projection.DecisionContract;
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.FamilyId, Is.EqualTo(PendingDecisionFamilies.SystemTransition));

            foreach (var open in pending.OpenItems)
            {
                Assert.That(
                    contract.Commands.Any(command =>
                        command.SystemTransitionChoice.ItemId.Equals(open.ItemId)),
                    Is.True,
                    "The Director holds every seat's authority, so every open item's answers are on " +
                    "its own contract.");
            }

            // Milestone 8.35u keeps the Director's SURFACE serialization: it describes one open item at
            // a time, in the rules' own order, and answering it brings up the next. Simultaneity is a
            // seat-surface property - exactly the position p.7's commitments already take.
            var described = session.Projection.PendingSystemTransitionDescriptions;
            Assert.That(described.Count, Is.EqualTo(pending.OpenItems.Count));
            Assert.That(session.Projection.PendingSystemTransitionDescription.ItemId,
                Is.EqualTo(pending.OpenItems[0].ItemId));
            Assert.That(described.Select(value => value.ItemId.Value),
                Is.EqualTo(pending.OpenItems.Select(open => open.ItemId.Value)));

            foreach (var open in pending.OpenItems.ToArray())
            {
                var live = session.Projection.PendingSystemTransitionStep;
                Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                    live.Invocations.First(invocation => invocation.ItemId.Equals(open.ItemId))));
            }

            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);
        }

        [Test]
        public void TheSingleItemAccessors_RefuseAConcurrentPauseRatherThanNamingItsFirstItem()
        {
            // R7's audit line. A shared accessor whose invariant just changed has to be found
            // deliberately, and a silently wrong first element is the failure mode that survives to a
            // playtest - so these throw. Invocations does not, because every command on it names the
            // item it answers, which is what lets the Director's surface work unchanged.
            var session = CreateSessionWhereEveryHeroCanLevel();
            var pending = PauseTheLevelUpStage(session);

            Assert.That(() => pending.ItemId, Throws.InstanceOf<InvalidOperationException>());
            Assert.That(() => pending.Options, Throws.InstanceOf<InvalidOperationException>());
            Assert.That(() => pending.ChoosingSide, Throws.InstanceOf<InvalidOperationException>());
            Assert.That(pending.Invocations.Select(invocation => invocation.ItemId).Distinct().Count(),
                Is.EqualTo(pending.OpenItems.Count));
        }

        [Test]
        public void AnAnswerNamingAnItemNobodyIsBeingAskedAbout_IsRefusedAsStale()
        {
            var session = CreateSessionWhereEveryHeroCanLevel();
            var pending = PauseTheLevelUpStage(session);
            var answeredHero = HeroLevelUpItem.Read(pending.OpenItems[0].ItemId).HeroId;
            var answered = FirstInvocationFor(pending, answeredHero);
            Accept(session, DirectorCommand.AnswerSystemTransitionStep(answered));

            // StaleItem keeps its exact meaning - "no pause stands for that item" - and now covers one
            // more way to be stale: an item of this step somebody has already answered.
            var repeated = session.Execute(DirectorCommand.AnswerSystemTransitionStep(answered));
            Assert.That(repeated.Accepted, Is.False,
                "An item answered once is not open, so answering it twice is not about this pause.");
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Not.Null,
                "Every refusal leaves the continuation exactly as it was.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A configured match at the start of the round in which p.8's pity coins have made every hero
        /// able to afford p.15's first level. Nothing is staged: one ordinary round earns nobody a coin,
        /// its Collect a Pity Coin step grants one to each hero, and the next round's Level Up stage is
        /// then a question for all of them.
        /// </summary>
        private static MatchSession CreateSessionWhereEveryHeroCanLevel()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            ConfiguredMatchFixtures.PlayOneOrdinaryRound(session);
            ConfiguredMatchFixtures.FinishEndOfRound(session);
            ConfiguredMatchFixtures.PlayOneOrdinaryRound(session);
            return session;
        }

        private static PendingSystemTransitionStep PauseTheLevelUpStage(MatchSession session)
        {
            ConfiguredMatchFixtures.AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            Accept(session, DirectorCommand.AdvanceEndOfRound());
            var pending = session.Projection.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null,
                "The pity coins collected last round are what every hero levels on now.");
            Assert.That(pending.DefinitionId, Is.EqualTo(HeroLevelUpSystemTransition.DefinitionId));
            return pending;
        }

        private static MatchCommandResult Accept(MatchSession session, DirectorCommand command) =>
            ConfiguredMatchFixtures.Accept(session, command);

        private static HeroId[] OpenHeroes(PendingSystemTransitionStep pending) =>
            pending == null
                ? Array.Empty<HeroId>()
                : pending.OpenItems.Select(open => HeroLevelUpItem.Read(open.ItemId).HeroId).ToArray();

        private static SystemTransitionChoiceCommand FirstInvocationFor(
            PendingSystemTransitionStep pending, HeroId heroId) =>
            pending.Invocations.First(invocation =>
                HeroLevelUpItem.Read(invocation.ItemId).HeroId.Equals(heroId));

        private static SystemTransitionOptionId OptionFor(
            PendingSystemTransitionStep pending, HeroId heroId) =>
            FirstInvocationFor(pending, heroId).OptionId;

        private static void AnswerByOption(
            MatchSession session, HeroId heroId, SystemTransitionOptionId optionId)
        {
            var pending = session.Projection.PendingSystemTransitionStep;
            var invocation = pending.Invocations.Single(candidate =>
                candidate.OptionId.Equals(optionId) &&
                HeroLevelUpItem.Read(candidate.ItemId).HeroId.Equals(heroId));
            Accept(session, DirectorCommand.AnswerSystemTransitionStep(invocation));
        }

        private static HeroId OwnerOf(MatchSession session, CardInstanceId cardInstanceId) =>
            session.Projection.Cards.Single(card => card.Id.Equals(cardInstanceId)).OwnerId;

        /// <summary>
        /// Every hero's Level and coin balance, as one comparable string - the whole of what p.15's
        /// leveling is allowed to have changed.
        /// </summary>
        private static string LevelsAndCoins(MatchSession session) => string.Join(
            "|",
            session.Projection.HeroProgression
                .OrderBy(row => row.HeroId.Value, StringComparer.Ordinal)
                .Select(row => $"{row.HeroId.Value}:{row.Level}:{row.Coins}"));
    }
}
