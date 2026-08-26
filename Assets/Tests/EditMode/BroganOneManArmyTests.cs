using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.21. Brogan's One Man Army, driven as the printed card rather than as the synthetic
    /// shape milestone 8.19c proved the rule on.
    ///
    /// <para>That milestone built <c>one-man-army-minion-battle@1</c> and proved both of its sentences
    /// against a fixture Ultimate; <see cref="MinionBattleRuleSourceTests"/> still owns the rule. What
    /// these cases add is that the printed Purple card actually reaches it: p.16's unlock puts the card
    /// beside the dashboard, the derived unlock set carries its declaration, and rulebook p.8's battle
    /// asks the registered handler for it - with no hero name, card name or content id read anywhere on
    /// the path.</para>
    ///
    /// <para>The second sentence is the user's 2026-08-23 decision on B-02: "lose the push instead"
    /// means exactly what losing the last minion means, so what a battle reports is p.8's ordinary Lane
    /// Push and Brogan is not removed. Its two most consequential negatives - no Defeat reward and no
    /// Life spent - are asserted here as well, because the substitution replaces a removal rather than
    /// performing a Defeat.</para>
    /// </summary>
    public sealed class BroganOneManArmyTests
    {
        [Test]
        public void TheUnlockedUltimate_PutsBroganIntoTheBattleAsHeavyStrength()
        {
            var session = CreateSession(unlockUltimate: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.MinionBattlePushLost),
                Is.True,
                "With no minion of his own left in the zone, Brogan is his side's whole strength - " +
                "\"During minion battle you count as a heavy minion\". A battle reporting nothing " +
                "would mean the printed declaration never reached p.8's count.");
        }

        [Test]
        public void TheBattleLeavesBroganOnTheBoard_AndHisTeamLosesThePushInstead()
        {
            var session = CreateSession(unlockUltimate: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);
            var before = session.Projection.Heroes.Select(value => value.HeroId).ToArray();

            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);

            Assert.That(
                session.Projection.Heroes.Select(value => value.HeroId).ToArray(),
                Is.EquivalentTo(before),
                "\"if you would be removed, lose the push instead\" - the card substitutes the push " +
                "for the removal, so the miniature is still standing where it was.");
        }

        [Test]
        public void TheSubstitution_PaysNoRewardAndSpendsNoLife()
        {
            var session = CreateSession(unlockUltimate: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);
            var coinsBefore = CoinsByHero(session);
            var livesBefore = LivesBySide(session);

            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);

            Assert.That(CoinsByHero(session), Is.EqualTo(coinsBefore),
                "p.13's reward belongs to a Defeat, and this is a removal replaced by a push.");
            Assert.That(LivesBySide(session), Is.EqualTo(livesBefore),
                "and a Life is spent by a hero's Defeat, which has not happened either.");
        }

        [Test]
        public void WithoutTheUnlock_TheSameBoardHasNoBattleToLose()
        {
            var session = CreateSession(unlockUltimate: false, insideBattleZone: true);
            AdvanceToEndOfRound(session);

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.MinionBattlePushLost),
                Is.False,
                "p.16 makes the ability the unlocked card's. A Brogan whose Ultimate is still in the " +
                "deck counts as nothing in the battle, which is what keeps the rule a property of the " +
                "card rather than of the hero.");
        }

        [Test]
        public void BroganOutsideTheBattleZone_SuppliesNothingAndSubstitutesNothing()
        {
            var session = CreateSession(unlockUltimate: true, insideBattleZone: false);
            AdvanceToEndOfRound(session);

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.MinionBattlePushLost),
                Is.False,
                "Both sentences begin \"During minion battle\", and p.8 counts the minions in the " +
                "Battle Zone. A hero standing elsewhere is not one of them.");
        }

        /// <summary>
        /// The round carries on afterwards, and the state it carries on from survives the boundary the
        /// snapshot draws. It is asserted through the same capture/restore the aggregate uses rather
        /// than by reading fields, because what must survive is the position rather than any one value.
        /// </summary>
        [Test]
        public void TheRoundContinuesAfterTheSubstitution_AndTheResultingPositionRestores()
        {
            var session = CreateSession(unlockUltimate: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);

            var payload = session.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            var restored = CreateSession(unlockUltimate: true, insideBattleZone: true);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True,
                "A position this rule produced is one the snapshot boundary carries, or the " +
                "substitution would be a state no saved match could come back to.");
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "and it comes back byte-identical, which is what makes the carry a restore rather " +
                "than a re-derivation.");
        }

        /// <summary>
        /// The one rule this Ultimate declares is the registered one, named by identity. Nothing on the
        /// path from the printed card to p.8's battle reads a hero name, a card name or a content id.
        /// </summary>
        [Test]
        public void ThePrintedUltimate_ReachesTheBattleThroughTheDerivedUnlockSetAlone()
        {
            var hand = BroganCardContent.CreateHand(new HeroId("one-man-army-hero"), "blue");
            var ultimate = hand.AllInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(BroganCardContent.OneManArmy));

            Assert.That(
                GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery
                    .Derive(hand.Definitions, hand.AllInstances)
                    .Ultimates,
                Is.Empty,
                "p.6 deals the Ultimate to the deck, and a card in the deck grants nothing.");

            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                "p.16 unlocks it by moving the card beside the dashboard.");

            var unlocked = GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery
                .Derive(hand.Definitions, hand.AllInstances);
            Assert.That(
                unlocked.Ultimates
                    .Select(value => value.Rule.MinionBattleRule.Reference.ToString())
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    GuardsDigital.Core.Rules.Consequences.OneManArmyMinionBattleHandler
                        .RuleReference.ToString()
                }),
                "The unlock set carries the rule identity the card declared, and the catalog is what " +
                "answers with the handler.");
        }

        private static MatchSession CreateSession(bool unlockUltimate, bool insideBattleZone)
        {
            var configuration =
                MatchConfiguration.CreateStagedBroganForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateStagedWaspVersusBroganOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var brogan = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

            if (unlockUltimate)
            {
                // p.16 records the unlock by moving the card, and the set is derived from that
                // position. It has to happen on the configuration's own instances: a session's
                // projection hands out copies, so unlocking there would unlock nothing.
                var ultimate = configuration.CardInstances.Single(card =>
                    card.OwnerId.Equals(brogan) &&
                    card.DefinitionReference.DefinitionId.Equals(BroganCardContent.OneManArmy));
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
            }

            // Brogan's own side keeps no minion in the zone, so he is the whole of its strength and
            // the battle has to reach him to report anything at all.
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                fullSetup.Minions.Where(value => value.TeamSide != TeamSide.Blue));

            var battleZone = BattleZoneQuery.FindStartingBattleZoneCoordinates(map);
            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var heroes = setup.Heroes
                .Select(hero => new HeroOccupancy(
                    hero.HeroId,
                    hero.HeroId.Equals(brogan) && insideBattleZone
                        ? battleZone.First(space =>
                            !occupied.Contains(space) &&
                            !setup.Heroes.Any(other =>
                                !other.HeroId.Equals(brogan) && other.Coordinate.Equals(space)))
                        : hero.Coordinate))
                .ToArray();
            Assert.That(
                heroes.Select(hero => hero.Coordinate).Distinct().Count(), Is.EqualTo(heroes.Length),
                "The fixture must leave every hero on a space of its own, or the board refuses the " +
                "scenario before any rule is asked.");

            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                heroes,
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static IReadOnlyList<string> CoinsByHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .OrderBy(hero => hero.Id.Value, StringComparer.Ordinal)
                .Select(hero => hero.Id.Value + ":" + session.Projection.HeroProgression
                    .Single(value => value.HeroId.Equals(hero.Id)).Coins)
                .ToArray();

        private static IReadOnlyList<string> LivesBySide(MatchSession session) =>
            session.Projection.TeamLifeCounters
                .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                .Select(entry => entry.Key.Value + ":" + entry.Value)
                .ToArray();

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Assert.That(session.Execute(DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }
        }

        private static void CommitUnequalCards(MatchSession session)
        {
            var red = EligibleCards(session, TeamSide.Red)
                .OrderBy(value => Initiative(session, value)).First();
            var blue = EligibleCards(session, TeamSide.Blue)
                .OrderByDescending(value => Initiative(session, value)).First();
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "The two committed cards print different Initiative values, so no tie opens.");
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards.Where(card =>
                card.State.Location == CardLocation.Hand &&
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side);

        private static int Initiative(MatchSession session, CardInstance card) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Initiative.Value;
    }
}
