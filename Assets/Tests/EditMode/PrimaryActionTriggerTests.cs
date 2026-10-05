using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39f1. The trigger Cutter's Legend of the Skies fires on, proved as vocabulary before
    /// her content exists: "The first time each turn after you perform a primary action, you may
    /// perform the primary action of a card in the previous turn slot."
    ///
    /// <para><b>NO EXISTING CONDITION COULD REACH IT, AND THE REASON IS A FACT RATHER THAN A
    /// READING.</b> All five conditions written before this milestone are matched against a
    /// <see cref="PerformedBasicAction"/>, and <c>BasicActionQuery.ForCompletedAction</c> returns the
    /// named absence unless the source card's classification is <see cref="CardClassification.Basic"/>
    /// - p.9's Gold and Silver alone. So Tigerclaw's "any basic action" is not a wider version of
    /// Cutter's sentence that merely says "basic": it is structurally unable to see the cards she plays
    /// most, and a Tier IV Ultimate is unlocked exactly when most of a deck is upgraded. This fixture's
    /// central case is therefore a completed action off a card that is NOT basic.</para>
    ///
    /// <para><b>So the sixth condition reads its own fact</b>,
    /// <see cref="PerformedPrimaryAction"/>, produced at the same single site.
    /// <see cref="PerformedBasicAction"/> keeps meaning exactly what it meant, which is asserted here
    /// rather than assumed.</para>
    ///
    /// <para>The borrow POOL this Ultimate then reads is milestone 8.39f's and is not re-proved here.
    /// What these cases own is when the clause fires.</para>
    /// </summary>
    [TestFixture]
    public sealed class PrimaryActionTriggerTests
    {
        private static readonly HeroId Actor = new HeroId("cutter-trigger-actor");
        private static readonly HeroId Other = new HeroId("cutter-trigger-other");

        private static readonly TeamId RedTeam = new TeamId("cutter-trigger-red");
        private static readonly TeamId BlueTeam = new TeamId("cutter-trigger-blue");

        private static readonly MapId Map = new MapId("cutter-trigger-board");
        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate OtherAt = new HexCoordinate(2, 0);

        // -------------------------------------------------------------------------------------------
        // Which invocations perform a primary action
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every invocation context is classified, and the case enumerates the enum rather than listing
        /// the ones it happens to know - so a context added later fails here instead of falling
        /// silently into "not a primary action". Milestone 8.38c2's own shape for the same reason.
        /// </summary>
        [Test]
        public void EveryInvocationContext_IsClassifiedAsAPrimaryActionOrNot()
        {
            var contexts = Enum.GetValues(typeof(ActionInvocationContext))
                .Cast<ActionInvocationContext>()
                .ToArray();

            Assert.That(contexts.Length, Is.EqualTo(9),
                "If a context has been added, classify it in PrimaryActionQuery.");

            Assert.That(
                contexts.Where(PrimaryActionQuery.IsPerformingAPrimaryAction),
                Is.EquivalentTo(new[]
                {
                    ActionInvocationContext.PrintedPrimary,
                    ActionInvocationContext.TextGranted
                }),
                "The printed primary is the clause's own subject, and a granted primary IS a primary " +
                "action being performed - which is what 'perform the primary action of a card' names.");
        }

        /// <summary>
        /// The five that are not, named individually because each is a different reason and a reader
        /// should not have to infer them from the set above.
        /// </summary>
        [Test]
        public void TheSecondariesTheReplacementTheReactionAndTheRepeat_AreNotPrimaryActions()
        {
            Assert.That(
                PrimaryActionQuery.IsPerformingAPrimaryAction(
                    ActionInvocationContext.PrintedSecondary), Is.False,
                "The secondary band is not the primary one.");
            Assert.That(
                PrimaryActionQuery.IsPerformingAPrimaryAction(
                    ActionInvocationContext.GeneralRuleSecondary), Is.False,
                "and neither is p.11's general-rule secondary.");
            Assert.That(
                PrimaryActionQuery.IsPerformingAPrimaryAction(
                    ActionInvocationContext.Replacement), Is.False,
                "p.7's Fast Travel and p.13's Clear are replacements rather than the printed primary " +
                "the sentence names - which is the reading BeforeActionTriggerQuery already records " +
                "for the same sentence shape at the other end of the action.");
            Assert.That(
                PrimaryActionQuery.IsPerformingAPrimaryAction(
                    ActionInvocationContext.Reaction), Is.False,
                "A p.14 Defense is spent on somebody else's turn, and every printed-primary Block is " +
                "classified as a Reaction before it ever reaches here.");
            Assert.That(
                PrimaryActionQuery.IsPerformingAPrimaryAction(
                    ActionInvocationContext.Repeated), Is.False,
                "A repeat REPLACES the original context rather than qualifying it, so a completed " +
                "repeat cannot say whether what it repeated was the printed primary. Answering yes " +
                "would be inventing that.");
        }

        /// <summary>
        /// An undeclared context is refused rather than treated as "not a primary action", so the
        /// classification cannot rot silently when the enum grows.
        /// </summary>
        [Test]
        public void AnUndeclaredContext_IsRefusedRatherThanTreatedAsNotAPrimaryAction()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PrimaryActionQuery.IsPerformingAPrimaryAction((ActionInvocationContext)99));
        }

        /// <summary>
        /// The fact carries the action it was produced for, and is the named absence where the
        /// invocation performed no primary.
        /// </summary>
        [Test]
        public void TheFact_IsDefinedForAPrimaryAndAbsentOtherwise()
        {
            var primary = PrimaryActionQuery.ForCompletedAction(
                Actor, new CardInstanceId("cutter-trigger-card"),
                new ActionDefinitionId("cutter-trigger-action"), ActionKind.Attack,
                ActionInvocationContext.PrintedPrimary);

            Assert.That(primary.IsDefined, Is.True);
            Assert.That(primary.ActorId, Is.EqualTo(Actor));
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack));

            Assert.That(
                PrimaryActionQuery.ForCompletedAction(
                    Actor, new CardInstanceId("cutter-trigger-card"),
                    new ActionDefinitionId("cutter-trigger-action"), ActionKind.Attack,
                    ActionInvocationContext.PrintedSecondary).IsDefined,
                Is.False);
        }

        /// <summary>
        /// The fact consults no card definition, which is the whole difference from p.9's basic
        /// supertype: the primary/secondary split is decided by which band the action came from, and
        /// the invocation context already states it.
        /// </summary>
        [Test]
        public void TheFact_IsIndifferentToWhetherTheCardIsBasic()
        {
            foreach (var kind in new[] { ActionKind.Skill, ActionKind.Attack, ActionKind.Movement })
            {
                Assert.That(
                    PrimaryActionQuery.ForCompletedAction(
                        Actor, new CardInstanceId("cutter-trigger-card"),
                        new ActionDefinitionId("cutter-trigger-action"), kind,
                        ActionInvocationContext.PrintedPrimary).IsDefined,
                    Is.True,
                    kind.ToString());
            }
        }

        // -------------------------------------------------------------------------------------------
        // What the condition sees
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE CASE THIS MILESTONE EXISTS FOR. A primary action off a card that is not basic reaches
        /// the new condition, and reaches none of the five written before it - because the fact those
        /// five read is the named absence for such a card.
        /// </summary>
        [Test]
        public void APrimaryActionOffANonBasicCard_ReachesTheNewConditionAndNoOlderOne()
        {
            var fixture = Fixture.Create();

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.PrimaryActionUltimates(), fixture.Board, fixture.Participants,
                    PerformedBasicAction.None, fixture.PrimaryActionBy(Actor)),
                Is.Not.Empty,
                "Legend of the Skies fires after a primary action, whatever card it came off.");

            foreach (var older in new[]
            {
                fixture.BasicActionUltimates(),
                fixture.BasicSkillUltimates()
            })
            {
                Assert.That(
                    UltimateTriggeredEffectSourceQuery.Derive(
                        older, fixture.Board, fixture.Participants,
                        PerformedBasicAction.None, fixture.PrimaryActionBy(Actor)),
                    Is.Empty,
                    "An Ultimate written against p.9's basic supertype sees nothing here, because " +
                    "BasicActionQuery returns the named absence for a card that is not basic - which " +
                    "is why this milestone could not widen an existing condition.");
            }
        }

        /// <summary>
        /// The five existing conditions are unchanged, asserted on the boards that distinguish them
        /// rather than assumed. A basic action still reaches the two that ask for one, and the narrow
        /// basic-Skill reading still refuses a basic Movement.
        /// </summary>
        [Test]
        public void TheOlderConditions_AreUnchangedByTheSixth()
        {
            var fixture = Fixture.Create();

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.BasicActionUltimates(), fixture.Board, fixture.Participants,
                    fixture.BasicActionBy(Actor, ActionKind.Movement)),
                Is.Not.Empty,
                "Tigerclaw's Cloak and Daggers still reaches every basic action.");

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.BasicSkillUltimates(), fixture.Board, fixture.Participants,
                    fixture.BasicActionBy(Actor, ActionKind.Skill)),
                Is.Not.Empty,
                "Wasp's High Voltage still reaches a basic Skill.");

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.BasicSkillUltimates(), fixture.Board, fixture.Participants,
                    fixture.BasicActionBy(Actor, ActionKind.Movement)),
                Is.Empty,
                "and still refuses a basic Movement, which is the distinction milestone 8.19c added a " +
                "second condition rather than loosen.");
        }

        /// <summary>
        /// The new condition is the controller's own, which p.16 requires and every one of these
        /// sentences says: "after YOU perform".
        /// </summary>
        [Test]
        public void TheNewCondition_SuppliesNothingForAnotherHerosPrimaryAction()
        {
            var fixture = Fixture.Create();

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.PrimaryActionUltimates(), fixture.Board, fixture.Participants,
                    PerformedBasicAction.None, fixture.PrimaryActionBy(Other)),
                Is.Empty);
        }

        /// <summary>
        /// A basic action that performed no primary - a secondary spent off a basic card - reaches the
        /// older conditions and not this one, which is the two questions being genuinely different
        /// rather than one implying the other.
        /// </summary>
        [Test]
        public void ABasicActionThatWasNotAPrimary_ReachesTheOlderConditionsAndNotTheNew()
        {
            var fixture = Fixture.Create();
            var basic = fixture.BasicActionBy(Actor, ActionKind.Skill);

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.BasicActionUltimates(), fixture.Board, fixture.Participants, basic),
                Is.Not.Empty,
                "The premise: a basic action reaches the older reading.");
            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.PrimaryActionUltimates(), fixture.Board, fixture.Participants,
                    basic, PerformedPrimaryAction.None),
                Is.Empty,
                "and a completed action that performed no primary reaches this one not at all.");
        }

        /// <summary>
        /// Both facts absent is no trigger at all, rather than an exception or an unfiltered set. A
        /// query that cannot see what happened cannot answer a question about it.
        /// </summary>
        [Test]
        public void WithNeitherFactDefined_NoSourceIsDerived()
        {
            var fixture = Fixture.Create();

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.PrimaryActionUltimates(), fixture.Board, fixture.Participants,
                    PerformedBasicAction.None, PerformedPrimaryAction.None),
                Is.Empty);
        }

        /// <summary>
        /// The sixth condition exists and is distinct from the five before it, so a content author
        /// naming it cannot silently get Cloak and Daggers' reading.
        ///
        /// <para>Milestone 8.40h extended the count rather than renumbering it, on this fixture's own
        /// precedent: a seventh condition arrived - Bain's "after you give a hero the bounty marker" -
        /// and <c>MarkerGivenTriggerTests</c> owns what it means. It is the first that reads neither of
        /// the two completed-action facts, which is why nothing else here moved.</para>
        ///
        /// <para>Milestone 8.61h extended it once more: a tenth condition arrived - Min's "each time
        /// after you perform an attack action" - which reads a THIRD completed-action fact of its own,
        /// <c>PerformedAttackAction</c>, and is proved by <c>MinItemAndFlurryTests</c>. It asks the
        /// action's KIND where this one asks its BAND, so neither is the other's wider reading and
        /// nothing else here moved.</para>
        /// </summary>
        [Test]
        public void TheSixthTriggerCondition_IsDistinctFromTheFiveBeforeIt()
        {
            var conditions = Enum.GetValues(typeof(UltimateTriggerCondition))
                .Cast<UltimateTriggerCondition>()
                .Where(condition => condition != UltimateTriggerCondition.None)
                .ToArray();

            Assert.That(
                conditions.Length, Is.EqualTo(13),
                "Min adds the distinct Attack-action condition beside Widget's Movement one; Wuk " +
                "(8.114e) adds the any-card resolution beside Swift's basic-card one; Razzle (9.21h) " +
                "adds the basic-Attack condition beside Wasp's basic-Skill one; NebKher (9.52) " +
                "adds the actual-laugh condition proved by NebKherLaughterFoundationTests.");
            Assert.That(
                conditions,
                Does.Contain(UltimateTriggerCondition.AfterControllerPerformsPrimaryAction));
            Assert.That(
                UltimateTriggerCondition.AfterControllerPerformsPrimaryAction,
                Is.Not.EqualTo(UltimateTriggerCondition.AfterControllerPerformsBasicAction));
        }

        // -------------------------------------------------------------------------------------------
        // "The first time each turn"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE BOUND THE CARD PRINTS. A turn performs as many primary actions as its text grants, and
        /// this clause fires on the first: the second arrives to find the source already spent.
        /// </summary>
        [Test]
        public void TheClauseFiresOnTheFirstPrimaryActionOfATurnAndNotTheSecond()
        {
            var fixture = Fixture.Create();
            var ultimates = fixture.PrimaryActionUltimates();
            var turn = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(fixture.ArmFor(ultimates, turn), Is.Not.Empty,
                "The first primary action of the turn fires it.");
            Assert.That(fixture.ArmFor(ultimates, turn), Is.Empty,
                "and the second does not, which is what 'the first time each turn' prints.");
            Assert.That(fixture.ArmFor(ultimates, turn), Is.Empty,
                "nor any after that.");
        }

        /// <summary>
        /// The bound is a fact about the TURN, so the next turn's key set fires it again. The set is
        /// the one the turn already clears at its boundary and the snapshot already carries, which is
        /// why this milestone persists nothing of its own.
        /// </summary>
        [Test]
        public void TheBoundIsPerTurn_SoTheNextTurnFiresItAgain()
        {
            var fixture = Fixture.Create();
            var ultimates = fixture.PrimaryActionUltimates();

            var thisTurn = new HashSet<string>(StringComparer.Ordinal);
            Assert.That(fixture.ArmFor(ultimates, thisTurn), Is.Not.Empty);
            Assert.That(fixture.ArmFor(ultimates, thisTurn), Is.Empty);

            Assert.That(
                fixture.ArmFor(ultimates, new HashSet<string>(StringComparer.Ordinal)), Is.Not.Empty,
                "A fresh turn's key set is a fresh firing.");
        }

        /// <summary>
        /// THE LOOP THE BOUND CLOSES, stated as its own case because it is the reason the clause needs
        /// a bound at all. The Ultimate grants a borrowed primary; a borrowed primary IS the
        /// performance of a primary action, so the completed grant comes back through the same
        /// derivation - and finds the source spent.
        /// </summary>
        [Test]
        public void TheGrantedPrimaryDoesNotReArmTheClauseThatGrantedIt()
        {
            Assert.That(
                PrimaryActionQuery.IsPerformingAPrimaryAction(
                    BorrowedPrimaryActionDeclaration.InvocationContext),
                Is.True,
                "The premise: a borrow performs a primary action, and the clause that says so is the " +
                "borrow's own declared invocation context.");

            var fixture = Fixture.Create();
            var ultimates = fixture.PrimaryActionUltimates();
            var turn = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(fixture.ArmFor(ultimates, turn), Is.Not.Empty,
                "The printed primary fires the clause, which grants the borrow.");
            Assert.That(fixture.ArmFor(ultimates, turn), Is.Empty,
                "and the borrowed primary that firing produced does not fire it again.");
        }

        /// <summary>
        /// Only a condition whose printed sentence bounds it is keyed into the turn. The five written
        /// before this milestone are returned untouched and write nothing, which is what keeps a
        /// shared key set from changing five shipped Ultimates.
        /// </summary>
        [Test]
        public void AnUnboundedConditionIsNeverKeyedIntoTheTurn()
        {
            var fixture = Fixture.Create();
            var ultimates = fixture.BasicActionUltimates();
            var turn = new HashSet<string>(StringComparer.Ordinal);

            var derived = UltimateTriggeredEffectSourceQuery.Derive(
                ultimates, fixture.Board, fixture.Participants,
                fixture.BasicActionBy(Actor, ActionKind.Skill));
            Assert.That(derived, Is.Not.Empty, "The premise.");

            Assert.That(
                UltimateTriggeredEffectSourceQuery.OncePerTurnKeyFor(ultimates, derived[0]),
                Is.Null,
                "Tigerclaw's condition prints no limit, so it has no key.");
            Assert.That(
                UltimateTriggeredEffectSourceQuery.ArmUnspent(ultimates, derived, turn),
                Is.EqualTo(derived));
            Assert.That(
                UltimateTriggeredEffectSourceQuery.ArmUnspent(ultimates, derived, turn),
                Is.EqualTo(derived),
                "and fires every time, however often the turn reaches it.");
            Assert.That(turn, Is.Empty, "having written nothing into the turn at all.");
        }

        /// <summary>
        /// The key is namespaced, so a spent after-action trigger cannot be mistaken for milestone
        /// 8.19a's spent BEFORE-action trigger on the same Ultimate - both live in the one set the turn
        /// clears, keyed by the same stable source id.
        /// </summary>
        [Test]
        public void TheKeyIsNamespacedAgainstTheOtherTurnScopedKeysItSharesASetWith()
        {
            var fixture = Fixture.Create();
            var ultimates = fixture.PrimaryActionUltimates();
            var derived = UltimateTriggeredEffectSourceQuery.Derive(
                ultimates, fixture.Board, fixture.Participants,
                PerformedBasicAction.None, fixture.PrimaryActionBy(Actor));

            var key = UltimateTriggeredEffectSourceQuery.OncePerTurnKeyFor(ultimates, derived[0]);

            Assert.That(key, Is.Not.Null);
            Assert.That(
                key,
                Is.EqualTo(UltimateTriggeredEffectSourceQuery.OncePerTurnKeyPrefix +
                    derived[0].SourceId));
            Assert.That(key, Is.Not.EqualTo(derived[0].SourceId),
                "The bare source id is milestone 8.19a's key for the same Ultimate's before-action " +
                "trigger; sharing it would have made one spend the other.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private sealed class Fixture
        {
            private Fixture(AdjacentMoveAuthority board, MatchParticipants participants)
            {
                Board = board;
                Participants = participants;
            }

            public AdjacentMoveAuthority Board { get; }

            public MatchParticipants Participants { get; }

            public static Fixture Create()
            {
                var participants = BuildParticipants();
                var spaces = Disk(3)
                    .Select(at => new BoardMapSpace(
                        new SpaceId($"q{at.Q}_r{at.R}"), at, BoardMapSpaceKind.Playable,
                        new ZoneId("cutter-trigger-zone")))
                    .ToArray();
                var map = new BoardMapDefinition(
                    Map, 1, "Cutter Trigger Board", spaces,
                    new[]
                    {
                        new BoardMapZone(
                            new ZoneId("cutter-trigger-zone"), "Cutter Trigger Zone", "lane", null,
                            false, true)
                    },
                    Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());

                return new Fixture(
                    new AdjacentMoveAuthority(
                        map,
                        new[]
                        {
                            new HeroOccupancy(Actor, ActorAt),
                            new HeroOccupancy(Other, OtherAt)
                        },
                        Array.Empty<ObstacleOccupancy>(),
                        Array.Empty<QuickGameMinionSetupState>()),
                    participants);
            }

            /// <summary>
            /// A completed primary action. It names no card definition at all, which is the point: the
            /// fact is produced from the invocation context rather than from p.9's classification, so a
            /// case can state "a primary action off a card that is not basic" without staging a deck.
            /// </summary>
            public PerformedPrimaryAction PrimaryActionBy(HeroId actor) =>
                PrimaryActionQuery.ForCompletedAction(
                    actor,
                    new CardInstanceId($"cutter-trigger-{actor.Value}-card"),
                    new ActionDefinitionId($"cutter-trigger-{actor.Value}-action"),
                    ActionKind.Skill,
                    ActionInvocationContext.PrintedPrimary);

            /// <summary>A completed BASIC action, for the cases that assert the older readings.</summary>
            public PerformedBasicAction BasicActionBy(HeroId actor, ActionKind kind) =>
                PerformedBasicAction.Of(
                    actor,
                    new CardInstanceId($"cutter-trigger-{actor.Value}-basic"),
                    new ActionDefinitionId($"cutter-trigger-{actor.Value}-basic-action"),
                    kind);

            /// <summary>
            /// One completed primary action of the actor's, derived and then put through the turn's
            /// bound - which is what a turn does for every primary action it performs.
            /// </summary>
            public IReadOnlyList<ContinuousTriggeredEffectSource> ArmFor(
                UnlockedUltimateSet ultimates, ICollection<string> consumedTurnScopedKeys) =>
                UltimateTriggeredEffectSourceQuery.ArmUnspent(
                    ultimates,
                    UltimateTriggeredEffectSourceQuery.Derive(
                        ultimates, Board, Participants,
                        PerformedBasicAction.None, PrimaryActionBy(Actor)),
                    consumedTurnScopedKeys);

            public UnlockedUltimateSet PrimaryActionUltimates() =>
                UnlockFor(SyntheticShowcaseHand.CreateRedWithAfterPrimaryActionUltimate(Actor));

            public UnlockedUltimateSet BasicActionUltimates() =>
                UnlockFor(SyntheticShowcaseHand.CreateRedWithAfterBasicActionUltimate(Actor));

            public UnlockedUltimateSet BasicSkillUltimates() =>
                UnlockFor(SyntheticShowcaseHand.CreateRedWithAfterBasicSkillUltimate(Actor));

            private static UnlockedUltimateSet UnlockFor(ConfiguredHand hand)
            {
                var ultimate = hand.AllInstances.Single(card =>
                    hand.Definitions.Single(definition =>
                            definition.Reference.Equals(card.DefinitionReference))
                        .UltimateRule != null);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                    "p.16 unlocks the Ultimate by moving the card beside the dashboard; the set is " +
                    "derived from that position and from nothing else.");
                return UltimateUnlockQuery.Derive(hand.Definitions, hand.AllInstances);
            }

            private static MatchParticipants BuildParticipants()
            {
                var actorPlayer = new PlayerId("cutter-trigger-actor-player");
                var otherPlayer = new PlayerId("cutter-trigger-other-player");
                return new MatchParticipants(
                    new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                    new[] { new Player(actorPlayer, RedTeam), new Player(otherPlayer, BlueTeam) },
                    new[]
                    {
                        new Hero(Actor, actorPlayer, HeroDashboard.RulebookLayout),
                        new Hero(Other, otherPlayer, HeroDashboard.RulebookLayout)
                    });
            }

            private static IEnumerable<HexCoordinate> Disk(int radius)
            {
                for (var dq = -radius; dq <= radius; dq++)
                {
                    for (var dr = Math.Max(-radius, -dq - radius);
                        dr <= Math.Min(radius, -dq + radius);
                        dr++)
                    {
                        yield return new HexCoordinate(dq, dr);
                    }
                }
            }
        }
    }
}
