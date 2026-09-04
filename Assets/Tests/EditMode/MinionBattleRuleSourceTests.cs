using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19c's isolated Minion-Battle rule: Brogan's One Man Army shape, "During minion
    /// battle you count as a heavy minion; if you would be removed, lose the push instead."
    ///
    /// <para>Both sentences are proven at the end of a real round rather than against the planner
    /// alone, because both are about p.8's battle: the first changes which side has more minions and
    /// the second replaces what the battle does to a unit it selected. The fixture leaves the rule's own
    /// team with no minion miniature in the Battle Zone at all, so the hero standing there is the whole
    /// of that side's strength - which makes the first sentence observable as the difference between a
    /// battle happening and no battle happening, and the second observable as a removal that did not
    /// occur.</para>
    ///
    /// <para>Nothing here names a hero, a card, or a display name, and neither does the code it drives.
    /// The declaration names a rule identity, the catalog answers with the handler, and the battle asks
    /// the handler - which is what the roster audit meant by the one controlled isolated handler.</para>
    /// </summary>
    public sealed class MinionBattleRuleSourceTests
    {
        [Test]
        public void AnUnlockedRule_PutsItsHeroIntoTheBattleAsHeavyStrength()
        {
            var session = CreateSession(unlockRule: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.MinionBattlePushLost),
                Is.True,
                "With no minion of its own left in the zone, the rule's hero is that side's whole " +
                "strength. A battle that reported nothing would mean the declaration had never " +
                "reached the count p.8 makes.");
        }

        [Test]
        public void AHeroTheBattleSelects_StaysOnTheBoardAndItsTeamLosesThePush()
        {
            var session = CreateSession(unlockRule: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);
            var heroesBefore = session.Projection.Heroes.Select(value => value.HeroId).ToArray();

            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);

            Assert.That(
                session.Projection.Heroes.Select(value => value.HeroId), Is.EquivalentTo(heroesBefore),
                "'Lose the push instead' replaces the removal: the miniature does not leave the board.");

            var push = session.Projection.PendingLanePushRequest;
            Assert.That(push, Is.Not.Null,
                "The substitution owes p.8's push by itself, whatever else the battle did.");
            Assert.That(push.LosingSide, Is.EqualTo(TeamSide.Red),
                "The push goes against the team whose unit the battle selected - the rule's own side.");
            Assert.That(push.WinningSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void TheSubstitution_AwardsNoCoinsAndMovesNoTeamLife()
        {
            var session = CreateSession(unlockRule: true, insideBattleZone: true);
            AdvanceToEndOfRound(session);
            var coinsBefore = CoinsByHero(session);
            var livesBefore = LivesBySide(session);

            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);

            Assert.That(CoinsByHero(session), Is.EqualTo(coinsBefore),
                "A Minion-Battle removal awards no coins, and the substitution that replaces one " +
                "awards none either. This is not p.13's Defeat.");
            Assert.That(LivesBySide(session), Is.EqualTo(livesBefore),
                "No Team Life moves: p.8 takes Life at the Throne and p.13 at a hero's Defeat, and " +
                "this is neither.");
        }

        [Test]
        public void WithoutTheUnlockedRule_TheSameBoardHasNoBattleToLose()
        {
            var session = CreateSession(unlockRule: false, insideBattleZone: true);
            AdvanceToEndOfRound(session);

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.MinionBattlePushLost ||
                    value.Kind == RoundResolutionEventKind.MinionBattleMinionRemoved),
                Is.False,
                "A hero who does not count as a minion is not in p.8's count, so a side with no " +
                "minion miniature in the zone has nothing to remove. This is the negative arm of the " +
                "first sentence: the declaration is what puts the hero into the battle at all.");
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null);
        }

        [Test]
        public void AHeroOutsideTheBattleZone_SuppliesNothingAndSubstitutesNothing()
        {
            var session = CreateSession(unlockRule: true, insideBattleZone: false);
            AdvanceToEndOfRound(session);

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(
                result.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.MinionBattlePushLost),
                Is.False,
                "p.8 counts the minions in the Battle Zone. A hero standing elsewhere is not one of " +
                "them, which is the same answer the page gives a minion standing elsewhere.");
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null);
        }

        // -----------------------------------------------------------------------------------------
        // The declaration and its catalog
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheCatalog_AnswersByRuleIdentityAndRefusesAnUnregisteredOne()
        {
            Assert.That(
                MinionBattleRuleCatalog.Rulebook.References,
                Is.EqualTo(new[] { OneManArmyMinionBattleHandler.RuleReference }),
                "This build implements exactly one Minion-Battle rule, and the battle reaches it by " +
                "identity rather than by any name on a card.");

            Assert.That(
                MinionBattleRuleCatalog.Rulebook.TryGet(
                    OneManArmyMinionBattleHandler.RuleReference, out var handler),
                Is.True);
            Assert.That(handler, Is.InstanceOf<OneManArmyMinionBattleHandler>());

            Assert.That(
                () => MinionBattleRuleCatalog.Rulebook.Get(
                    new MinionBattleRuleReference("no-such-minion-battle-rule", 1)),
                Throws.InvalidOperationException,
                "A definition naming a rule nothing registers is a content error the catalog reports, " +
                "rather than a battle that quietly runs without the rule the card printed.");
        }

        [Test]
        public void ADeclarationThatChangesNothing_IsRefused()
        {
            Assert.That(
                () => new MinionBattleRuleDeclaration(
                    OneManArmyMinionBattleHandler.RuleReference,
                    MinionBattleStrength.None,
                    MinionBattleRemovalSubstitution.None),
                Throws.ArgumentException,
                "A rule that adds no strength and substitutes no removal changes nothing about the " +
                "battle, so it is not a rule.");
        }

        [Test]
        public void TheHandler_AnswersBothSentencesFromItsOwnDeclarationAndOnlyInsideTheZone()
        {
            var declaration = new MinionBattleRuleDeclaration(
                OneManArmyMinionBattleHandler.RuleReference,
                MinionBattleStrength.HeavyMinion,
                MinionBattleRemovalSubstitution.PushLost);
            var handler = new OneManArmyMinionBattleHandler();
            var inside = new MinionBattleParticipant(
                declaration, new HeroId("probe-hero"), TeamSide.Red, true);
            var outside = new MinionBattleParticipant(
                declaration, new HeroId("probe-hero"), TeamSide.Red, false);

            Assert.That(handler.Contribute(inside), Is.EqualTo(MinionBattleStrength.HeavyMinion));
            Assert.That(
                handler.SubstituteRemoval(inside),
                Is.EqualTo(MinionBattleRemovalSubstitution.PushLost));

            Assert.That(handler.Contribute(outside), Is.EqualTo(MinionBattleStrength.None),
                "Both sentences are conditioned on the one fact p.8 conditions them on.");
            Assert.That(
                handler.SubstituteRemoval(outside),
                Is.EqualTo(MinionBattleRemovalSubstitution.None));
        }

        [Test]
        public void AnUltimateDeclaringAMinionBattleRule_DeclaresNothingElse()
        {
            Assert.That(
                () => UltimateRuleDefinition.MinionBattleAbility(
                    "probe-rule", "ProbeKind", null, Array.Empty<PrintedValueDefinition>()),
                Throws.ArgumentNullException,
                "The rule is the whole of this Ultimate's printed content.");
        }

        // -----------------------------------------------------------------------------------------
        // Fixture
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A match whose Red hand carries the Minion-Battle Ultimate, with every Red minion kept out of
        /// the Battle Zone so the hero standing in it is that side's whole strength.
        /// </summary>
        private static MatchSession CreateSession(bool unlockRule, bool insideBattleZone)
        {
            var configuration =
                MatchConfiguration.CreateForMinionBattleRuleFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var redHero = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red)
                .Id;

            if (unlockRule)
            {
                // p.16 records the unlock by moving the card, and the set is derived from that
                // position. It has to happen on the configuration's own instances: a session's
                // projection hands out copies, so unlocking there would unlock nothing.
                var ultimate = configuration.CardInstances.Single(card =>
                    card.OwnerId.Equals(redHero) &&
                    configuration.CardDefinitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .UltimateRule?.MinionBattleRule != null);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
            }

            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                fullSetup.Minions.Where(value => value.TeamSide != TeamSide.Red));

            var battleZone = BattleZoneQuery.FindStartingBattleZoneCoordinates(map);
            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var heroes = setup.Heroes
                .Select(hero => new HeroOccupancy(
                    hero.HeroId,
                    hero.HeroId.Equals(redHero) && insideBattleZone
                        ? battleZone.First(space =>
                            !occupied.Contains(space) &&
                            !setup.Heroes.Any(other =>
                                !other.HeroId.Equals(redHero) && other.Coordinate.Equals(space)))
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
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, so this loop presses the release exactly as that player does.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Assert.That(session.Execute(DirectorCommand.ReleaseTurn()).Accepted, Is.True);
                    continue;
                }

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
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side)
        {
            var heroId = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards.Where(value =>
                value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand);
        }

        private static int Initiative(MatchSession session, CardInstance card) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Initiative.Value;
    }
}
