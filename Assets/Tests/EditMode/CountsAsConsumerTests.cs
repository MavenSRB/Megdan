using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8g: the `Counts As` effective type's first production consumer.
    ///
    /// The consuming rule is the p.13 Heavy-immunity accompaniment clause: "As long as there are any
    /// friendly non-heavy minions in the Battle Zone, the heavy minion is Immune". Under p.17's
    /// replacement doctrine - "all rules and qualities of that object are replaced by the rules and
    /// qualities of the object type(s) it now counts as" - a hero counting as a minion is a friendly
    /// non-heavy minion for that membership question, so its presence in the starting Battle Zone
    /// protects the Heavy.
    ///
    /// The fixture is a configured official-map match: the Blue team's lone Heavy stands in the
    /// starting Battle Zone with no other Blue minion anywhere, the Red hero stands adjacent, and the
    /// Blue hero stands elsewhere in the zone holding a synthetic test card whose Skill binds the
    /// registered <c>CountAsMinionV1</c> capability. Playing that Skill is the only difference
    /// between the two match halves, and the enemy's legal attack-target list is where the outcome
    /// changes. The card is test-local content; converting a hand card is milestone 6.8b's job.
    /// </summary>
    public sealed class CountsAsConsumerTests
    {
        private static readonly CardInstanceId CountsAsCardId =
            new CardInstanceId("counts-as-consumer-card");

        private static readonly ActionDefinitionId CountsAsSkill =
            new ActionDefinitionId("counts-as-consumer-skill");

        private static readonly MinionId BlueHeavy = new MinionId("counts-as-blue-heavy");

        private static readonly HexCoordinate RedHeroAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate HeavyAt = new HexCoordinate(0, -1);
        private static readonly HexCoordinate BlueHeroAt = new HexCoordinate(2, -1);

        /// <summary>
        /// Milestone 6.8g's failing observation: before the accompaniment clause consulted the
        /// effective question, the declaration changed nothing and the Heavy stayed targetable.
        /// </summary>
        [Test]
        public void AHeroCountingAsAMinion_ProtectsItsHeavyFromTheEnemyAttack()
        {
            var session = CreateCountsAsSession();
            CommitBothCards(session);

            StartCountsAsSkill(session);
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "The single-capability Skill resolves in one step.");
            Assert.That(
                Modifications(session).CountsAs(
                    RuleObject.ForHero(BlueHeroId(session)), EffectiveUnitKind.Minion),
                Is.True,
                "The fixture must actually hold the declaration, or the case proves nothing.");

            StartRedAttack(session);
            var choice = session.Projection.PendingAttackTargetChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.LegalTargets, Has.No.Member(AttackTarget.Minion(BlueHeavy)),
                "p.13: the Heavy is Immune while a friendly non-heavy minion is in the Battle Zone, " +
                "and p.17 makes the counting hero exactly that, so the declaration changes the " +
                "match outcome rather than only a query answer.");
        }

        [Test]
        public void StandingHeavyImmunity_ExcludesTheProtectedHeavyFromEveryActivityTargetQuery()
        {
            var session = CreateCountsAsSession();
            CommitBothCards(session);
            StartCountsAsSkill(session);

            var board = RestoreBoard(session);
            var participants = session.Projection.Participants;
            var modifications = Modifications(session);
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            foreach (var activity in new[]
            {
                RuleActivity.Attack,
                RuleActivity.Movement,
                RuleActivity.Skill,
                RuleActivity.ObjectPush
            })
            {
                Assert.That(TargetQuery.FindTargetsWithin(
                        RedHeroAt,
                        1,
                        false,
                        RuleObject.ForHero(redHero),
                        activity,
                        board,
                        participants,
                        modifications).Select(target => target.MinionId),
                    Has.No.Member(BlueHeavy),
                    $"Standing Heavy immunity must exclude the target for {activity}, not only Attack.");
                Assert.That(ImmunityQuery.IsImmuneTo(
                        RuleObject.ForMinion(BlueHeavy),
                        activity,
                        RuleObject.ForHero(redHero),
                        board,
                        participants,
                        modifications),
                    Is.True,
                    $"The shared final-application query must protect the Heavy from {activity}.");
            }
        }

        [Test]
        public void WithoutTheDeclaration_TheLoneHeavyStaysTargetable()
        {
            var session = CreateCountsAsSession();
            CommitBothCards(session);

            Accept(session, DirectorCommand.PerformHold(CountsAsCardId));

            StartRedAttack(session);
            var choice = session.Projection.PendingAttackTargetChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.LegalTargets, Has.Member(AttackTarget.Minion(BlueHeavy)),
                "Without the declaration the Heavy has no accompanying non-heavy minion in the zone " +
                "and is targetable - the exact result the pre-6.8g implementation produced in both " +
                "halves.");
        }

        [Test]
        public void TheDeclaration_LeavesIntrinsicRepresentationAndOccupancyUntouched()
        {
            var session = CreateCountsAsSession();
            CommitBothCards(session);
            StartCountsAsSkill(session);

            var board = RestoreBoard(session);
            var blueHero = BlueHeroId(session);

            Assert.That(board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(blueHero)),
                Is.True,
                "The declaring hero remains a hero occupant; Counts As replaces rules qualities, " +
                "never identity, ownership, or occupancy kind.");
            Assert.That(board.Minions.Single().MinionId, Is.EqualTo(BlueHeavy),
                "No minion was created; the board's minion register still holds exactly the Heavy.");
            Assert.That(
                IntrinsicKindQuery.IsMinionMiniature(RuleObject.ForHero(blueHero)),
                Is.False,
                "The intrinsic question still answers hero; only the effective question changes.");
        }

        [Test]
        public void ZonePresence_CountsTheDeclaringHeroOnlyWithTheDeclaration()
        {
            var session = CreateCountsAsSession();
            CommitBothCards(session);

            var zone = new ZoneId("starting-battle-zone");
            var before = UnitPresenceQuery.FindUnitsInZone(
                zone, EffectiveUnitKind.Minion, RestoreBoard(session), Modifications(session));
            Assert.That(before, Is.EqualTo(new[] { RuleObject.ForMinion(BlueHeavy) }),
                "Without the declaration the zone's effective minions are exactly the Heavy.");

            StartCountsAsSkill(session);

            var after = UnitPresenceQuery.FindUnitsInZone(
                zone, EffectiveUnitKind.Minion, RestoreBoard(session), Modifications(session));
            Assert.That(after, Is.EqualTo(new[]
                {
                    RuleObject.ForHero(BlueHeroId(session)),
                    RuleObject.ForMinion(BlueHeavy)
                }),
                "With the declaration the zone-scoped presence question counts the hero as a " +
                "minion, in canonical identity order.");

            var heroesInZone = UnitPresenceQuery.FindUnitsInZone(
                zone, EffectiveUnitKind.Hero, RestoreBoard(session), Modifications(session));
            Assert.That(heroesInZone, Has.No.Member(RuleObject.ForHero(BlueHeroId(session))),
                "The same hero is no longer effectively a hero while it counts as a minion (p.17 " +
                "replaces, never duplicates).");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static HeroId BlueHeroId(MatchSession session) =>
            session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;

        private static void CommitBothCards(MatchSession session)
        {
            // Red Lancer's printed Attack is Ranged with Range 3, so the Blue hero at distance 2
            // stays a legal target either way and the pending choice opens in both match halves;
            // only the Heavy's membership changes.
            var countsAs = session.Projection.Cards.Single(card => card.Id.Equals(CountsAsCardId));
            var lancer = session.Projection.Cards.Single(card => card.Id.Value == "red-lancer-card");
            Accept(session, DirectorCommand.CommitCard(countsAs.OwnerId, countsAs.Id));
            Accept(session, DirectorCommand.CommitCard(lancer.OwnerId, lancer.Id));

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "Initiative 30 against a placeholder card cannot tie.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(CountsAsCardId),
                "The declaring hero acts first, so the effect is live when the enemy attacks.");
        }

        private static void StartCountsAsSkill(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(CountsAsSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));

            // The declaration executed on the first step; declining the trailing `may` completes
            // the Skill without touching it.
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.DeclineInvocation));
        }

        private static void StartRedAttack(MatchSession session)
        {
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
        }

        private static ActionKind ActionKindOf(MatchSessionProjection projection, ActionOffer offer) =>
            projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind;

        private static RuleModificationSet Modifications(MatchSession session)
        {
            var restored = Restore(session);
            return RuleModificationQuery.Build(
                restored.Round.Timing,
                restored.Board,
                restored.Participants,
                restored.Round.TieBreakerState, UnlockedUltimateSet.None);
        }

        private static GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority RestoreBoard(
            MatchSession session) => Restore(session).Board;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// The Skill that makes the acting hero count as a minion for the round: one mandatory
        /// capability node binding the registered <c>CountAsMinionV1</c>. Initiative 30 outranks
        /// every placeholder card, so the declaring hero always acts first.
        /// </summary>
        private static CardDefinition CreateCountsAsDefinition()
        {
            // The 6.2a skill card's proven shape: the declaration executes on the first step, and a
            // trailing `may` step opens the choice that keeps a pending frame alive. The shape was
            // originally forced - a Skill whose root completed at its own start step read its
            // identity from a null frame and faulted - and milestone 6.8j closed that seam, proving
            // it with its own start-completing fixture. This fixture deliberately keeps the shape
            // anyway: it is the one every configured Skill uses, and changing it would move this
            // milestone's evidence for a reason unrelated to `Counts As`.
            var root = new EffectNodeId("counts-as-root");
            var declare = new EffectNodeId("counts-as-declare");
            var optional = new EffectNodeId("counts-as-may-note");
            var note = new EffectNodeId("counts-as-note");
            var effect = new EffectDefinition(
                new EffectDefinitionId("counts-as-consumer-effect"),
                "phase-6-8g-test-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { declare, optional }),
                    new EffectNodeDefinition(
                        declare, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticModificationCapabilities.CountAsMinionV1,
                        outputs: new[] { new EffectOutputBinding("affected", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(
                        optional, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { note },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        note, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1,
                        outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) })
                });

            return new CardDefinition(
                new CardDefinitionId("counts-as-consumer-test"),
                "phase-6-8g-test-rev-1",
                "TEST - Counts As Consumer",
                new InitiativeValue(30),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        CountsAsSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        /// <summary>
        /// Official map; Red hero adjacent to the Blue team's lone Heavy in the starting Battle
        /// Zone; the Blue hero elsewhere in the same zone with the counts-as card added to its hand.
        /// </summary>
        private static MatchSession CreateCountsAsSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);

            var countsAsDefinition = CreateCountsAsDefinition();
            var countsAsInstance = CardInstance.CreateInHand(
                CountsAsCardId, countsAsDefinition.Reference, blueHeroId);
            var definitions = configuration.CardDefinitions.Concat(new[] { countsAsDefinition });
            var instances = configuration.CardInstances.Concat(new[] { countsAsInstance });

            var heavySetup = new QuickGameMinionSetupState(
                BlueHeavy,
                blueTeam.Id,
                TeamSide.Blue,
                StandardMinionType.Heavy,
                new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-minion-01")),
                new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r-1")),
                HeavyAt);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-throne")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-throne"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-counts-as"),
                        "Red Counts As",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-hero-01")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        RedHeroAt),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-counts-as"),
                        "Blue Counts As",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-hero-01")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q2_r-1")),
                        BlueHeroAt)
                },
                new[] { heavySetup });
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                definitions,
                instances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, RedHeroAt),
                    new HeroOccupancy(blueHeroId, BlueHeroAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
