using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38d. Bullet Time's second sentence - "you cannot target the same enemy hero twice
    /// in the same turn this way" - and the ability-granted action path it is enforced on.
    ///
    /// <para><b>Two things are proved here and they are separable.</b> The first is that a PERMANENT
    /// ABILITY can grant an action at all: every borrow in this build until now was asked for by card
    /// text from inside a resolution that was suspended and resumed around the instance, and a trigger
    /// has no such frame, so Bullet Time would have chosen a card and granted nothing. The second is
    /// the clause itself, which is a rule about what THE GRANT may reach and deliberately not a rule
    /// about the hero: her own Attack on the same enemy in the same turn is untouched.</para>
    /// </summary>
    [TestFixture]
    public sealed class GrantedTargetExclusionTests
    {
        private const string Grant = "bullet-time-after-basic-card";

        private static readonly HeroId FirstEnemy = new HeroId("granted-enemy-one");
        private static readonly HeroId SecondEnemy = new HeroId("granted-enemy-two");

        /// <summary>The two enemy heroes the staged board puts in Snipe's printed band.</summary>
        private static readonly HeroId FirstTarget =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static readonly HeroId SecondTarget =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        // -------------------------------------------------------------------------------------------
        // The keys: how the accumulation is written down, and how it shares one set with 8.19a
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnExcludedHero_ReadsBackAsTheTargetThatGrantMayNoLongerTake()
        {
            var keys = new[] { GrantedTargetExclusionQuery.ExcludedTargetKey(Grant, FirstEnemy) };

            Assert.That(
                GrantedTargetExclusionQuery.ExcludedTargets(Grant, keys),
                Is.EqualTo(new[] { AttackTarget.Hero(FirstEnemy) }));
        }

        [Test]
        public void AnotherGrantsExclusions_AreNotThisGrants()
        {
            var keys = new[]
            {
                GrantedTargetExclusionQuery.ExcludedTargetKey("some-other-ability", FirstEnemy),
                GrantedTargetExclusionQuery.ExcludedTargetKey(Grant, SecondEnemy)
            };

            Assert.That(
                GrantedTargetExclusionQuery.ExcludedTargets(Grant, keys),
                Is.EqualTo(new[] { AttackTarget.Hero(SecondEnemy) }),
                "The window is one grant's, so two granting abilities accumulate apart.");
        }

        [Test]
        public void WithNothingAccumulated_TheGrantExcludesNothing()
        {
            Assert.That(
                GrantedTargetExclusionQuery.ExcludedTargets(Grant, Array.Empty<string>()),
                Is.Empty);
            Assert.That(
                GrantedTargetExclusionQuery.ExcludedTargets(null, new[] { "anything" }),
                Is.Empty,
                "No grant running is not the same question as a grant that has reached nobody.");
        }

        [Test]
        public void A819aTriggerIdSharingTheSet_IsNotReadAsAnExclusion()
        {
            var keys = new[] { "living-tsunami-before-attack", Grant, "bullet-time" };

            Assert.That(GrantedTargetExclusionQuery.ExcludedTargets(Grant, keys), Is.Empty);
            Assert.That(GrantedTargetExclusionQuery.RunningGrant(keys), Is.Null);
        }

        [Test]
        public void TheRunningGrant_IsWhicheverAbilityWroteItsKey()
        {
            var keys = new List<string>
            {
                "living-tsunami-before-attack",
                GrantedTargetExclusionQuery.RunningGrantKey(Grant)
            };

            Assert.That(GrantedTargetExclusionQuery.RunningGrant(keys), Is.EqualTo(Grant));
            keys.RemoveAll(key => key.StartsWith(GrantedTargetExclusionQuery.RunningGrantPrefix));
            Assert.That(GrantedTargetExclusionQuery.RunningGrant(keys), Is.Null);
        }

        [Test]
        public void AnAbilityIdThatCouldBeMistakenForAKey_IsRefusedWhereItIsMinted()
        {
            Assert.Throws<ArgumentException>(
                () => GrantedTargetExclusionQuery.RunningGrantKey("granted-target:forged"));
            Assert.Throws<ArgumentException>(
                () => GrantedTargetExclusionQuery.ExcludedTargetKey("a:b", FirstEnemy));
            Assert.Throws<ArgumentException>(
                () => GrantedTargetExclusionQuery.ExcludedTargetKey(Grant, default));
        }
        // -------------------------------------------------------------------------------------------
        // The declaration: which printed ability narrows what it grants
        // -------------------------------------------------------------------------------------------

        [Test]
        public void BulletTime_IsTheOneVerifiedAbilityThatNarrowsWhatItGrants()
        {
            var narrowing = new List<string>();
            foreach (var type in typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal)))
            {
                var all = type.GetProperty("AllDefinitions", BindingFlags.Public | BindingFlags.Static);
                if (all == null)
                {
                    continue;
                }

                foreach (var card in (IEnumerable<CardDefinition>)all.GetValue(null))
                {
                    if (card.UltimateRule != null &&
                        card.UltimateRule.GrantedTargetExclusion != GrantedTargetExclusion.None)
                    {
                        narrowing.Add(card.UltimateRule.RuleId);
                    }
                }
            }

            Assert.That(
                narrowing, Is.EqualTo(new[] { Grant }),
                "Every other permanent ability in the build declares None, which is what makes the " +
                "clause a property of Bullet Time rather than of granting.");
        }

        [Test]
        public void TheGrantIsNamedByTheUnlockedUltimate_AndOnlyWhileItIsUnlocked()
        {
            var owner = new HeroId("granted-owner");
            var definition = SwiftCardContent.Definition(SwiftCardContent.BulletTime);
            var card = CardInstance.CreateInDeck(
                new CardInstanceId("granted-ultimate"), definition.Reference, owner);
            var definitions = new[] { definition };
            var instances = new[] { card };

            Assert.That(
                GrantedTargetExclusionQuery.GrantSourceIdFor(
                    UltimateUnlockQuery.Derive(definitions, instances), owner),
                Is.Null,
                "Rulebook p.16: an Ultimate still in the deck grants nothing and names nothing.");

            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(card), Is.True);
            var unlocked = UltimateUnlockQuery.Derive(definitions, instances);

            Assert.That(GrantedTargetExclusionQuery.GrantSourceIdFor(unlocked, owner), Is.EqualTo(Grant));
            Assert.That(GrantedTargetExclusionQuery.Excludes(unlocked, owner), Is.True);
            Assert.That(
                GrantedTargetExclusionQuery.Excludes(unlocked, new HeroId("someone-else")), Is.False,
                "It is her ability, so it narrows her grants and nobody else's.");
        }

        [Test]
        public void TheTurnScopedKeys_SurviveTheTurnsOwnRestoreState()
        {
            var keys = new[]
            {
                "living-tsunami-before-attack",
                GrantedTargetExclusionQuery.RunningGrantKey(Grant),
                GrantedTargetExclusionQuery.ExcludedTargetKey(Grant, FirstEnemy)
            };
            var state = new TurnResolutionRestoreState(
                true, false, new TieBreakerState(TeamSide.Red),
                new InitiativeResolutionRestoreState(null, default), 1, 1, keys);

            Assert.That(state.ConsumedTurnScopedKeys, Is.EqualTo(keys));
            Assert.That(
                GrantedTargetExclusionQuery.ExcludedTargets(Grant, state.ConsumedTurnScopedKeys),
                Is.EqualTo(new[] { AttackTarget.Hero(FirstEnemy) }),
                "One serialized run of strings carries both kinds, which is why the clause moved no " +
                "snapshot schema.");
        }

        // -------------------------------------------------------------------------------------------
        // The narrowing, at the offer that reads it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The enforcement point itself: an Attack instance born with the grant's accumulated heroes
        /// does not offer them, and refuses a payload that names one anyway.
        ///
        /// <para><b>Why this is asked of the started instance rather than driven through Bullet Time's
        /// own grant.</b> Swift's only two basic cards, Reload! and Bounce, print SKILL primaries - so
        /// the action p.16's ability hands back is never itself an Attack, and the Attack the clause can
        /// reach is the one Reload!'s own text borrows from inside the granted instance. What is common
        /// to both, and what the clause is actually about, is that an instance carries what the grant
        /// has already reached into its target offer; that is one question and it is asked here, of the
        /// same backend entry point <c>TryStartOwedBorrowedAction</c> calls.</para>
        /// </summary>
        [TestCase(false, TestName = "AnInstanceTheGrantHasReachedNobodyWith_OffersEveryLegalEnemy")]
        [TestCase(true, TestName = "AnInstanceCarryingAReachedHero_NeitherOffersNorAcceptsThem")]
        public void TheGrantedInstance_IsNarrowedByWhatTheGrantHasAlreadyReached(bool alreadyReached)
        {
            var board = new AttackBoard();
            var excluded = alreadyReached
                ? GrantedTargetExclusionQuery.ExcludedTargets(
                    Grant,
                    new[] { GrantedTargetExclusionQuery.ExcludedTargetKey(Grant, FirstTarget) })
                : null;

            var started = board.StartTheAttack(excluded);

            Assert.That(started.Accepted, Is.True);
            var choice = board.CardActions.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.LegalTargetHeroIds, Does.Contain(SecondTarget),
                "The clause names one hero, so the rest of the board is untouched either way.");

            if (!alreadyReached)
            {
                Assert.That(choice.LegalTargetHeroIds, Does.Contain(FirstTarget),
                    "An ordinary instance offers every legal enemy, which is what the clause narrows " +
                    "and therefore what it must be measured against.");
                return;
            }

            Assert.That(choice.LegalTargetHeroIds, Does.Not.Contain(FirstTarget),
                "\"you cannot target the same enemy hero twice in the same turn this way\".");
            Assert.That(
                board.CardActions.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        choice.ResolutionId, choice.ChoiceId, AttackTarget.Hero(FirstTarget)),
                    board.Definitions, board.Instances, board.Participants,
                    RuleModificationSet.Empty).Accepted,
                Is.False,
                "A payload naming the excluded hero is refused, not merely left out of the offer.");
        }

        [Test]
        public void TheNarrowingIsTheGrantsAlone_AndAnotherGrantsAccumulationDoesNotReachIt()
        {
            var board = new AttackBoard();
            var excluded = GrantedTargetExclusionQuery.ExcludedTargets(
                Grant,
                new[]
                {
                    GrantedTargetExclusionQuery.ExcludedTargetKey("another-ability", FirstTarget)
                });

            Assert.That(board.StartTheAttack(excluded).Accepted, Is.True);
            Assert.That(
                board.CardActions.PendingAttackTargetChoice.LegalTargetHeroIds,
                Does.Contain(FirstTarget),
                "A hero another ability reached is not a hero THIS grant reached.");
        }

        /// <summary>
        /// The smallest board on which Snipe's printed narrowing - "at maximum range, and in a straight
        /// line" - admits exactly two enemy heroes, so an exclusion has something to take out and
        /// something to leave behind.
        /// </summary>
        private sealed class AttackBoard
        {
            private readonly CardInstance snipe;

            public AttackBoard()
            {
                var configuration =
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
                Participants = configuration.Participants;
                var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                var definition = SwiftCardContent.Definition(SwiftCardContent.Snipe);
                snipe = CardInstance.CreateInHand(
                    new CardInstanceId("granted-snipe"), definition.Reference, actor);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(snipe), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(snipe), Is.True);

                Definitions = configuration.CardDefinitions.Concat(new[] { definition })
                    .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
                Instances = configuration.CardInstances.Concat(new[] { snipe }).ToArray();

                var coordinates = new List<HexCoordinate>();
                for (var q = -5; q <= 5; q++)
                for (var r = -5; r <= 5; r++)
                {
                    if (Math.Abs(q + r) <= 5) coordinates.Add(new HexCoordinate(q, r));
                }

                var board = new AdjacentMoveAuthority(
                    coordinates,
                    new[]
                    {
                        new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(FirstTarget, new HexCoordinate(4, 0)),
                        new HeroOccupancy(SecondTarget, new HexCoordinate(0, 4))
                    },
                    Enumerable.Empty<ObstacleOccupancy>(),
                    Enumerable.Empty<QuickGameMinionSetupState>());

                CardActions = new CardActionResolutionAuthority(
                    new MatchContentCatalogs(
                        SyntheticEffectCapabilities.CreateCatalog(),
                        SyntheticComponentDefinitions.CreateCatalog(),
                        TriggeredEffectCatalog.Empty),
                    board);
            }

            public CardActionResolutionAuthority CardActions { get; }

            public MatchParticipants Participants { get; }

            public IReadOnlyList<CardDefinition> Definitions { get; }

            public IReadOnlyList<CardInstance> Instances { get; }

            public AttackActionResolutionResult StartTheAttack(
                IReadOnlyList<AttackTarget> excludedTargets) =>
                CardActions.StartTurnAttackResolution(
                    new ActionResolutionId("granted-instance"),
                    ActionOfferQuery
                        .GetAvailableTurnActionOffers(
                            Definitions, Instances, snipe.OwnerId, snipe.Id)
                        .Single(value => value.ActionDefinitionId.Equals(
                            SwiftCardContent.PrimaryRangedAttack)),
                    Definitions,
                    Instances,
                    Participants,
                    RuleModificationSet.Empty,
                    excludedTargets);
        }

        // -------------------------------------------------------------------------------------------
        // The grant itself, driven through a turn
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.16's ability actually granting an action, which is the engine path this milestone
        /// had to build rather than reuse: every borrow in the build until now was asked for by CARD
        /// TEXT from inside a resolution that is suspended and resumed around the instance, and a
        /// trigger has already run to completion when it asks - so a permanent ability could choose a
        /// card and grant nothing at all.
        ///
        /// <para>Bounce is the basic card here because Bullet Time fires on a BASIC card resolving and
        /// Swift has exactly two - Reload! and Bounce - which is also the measured reason the clause's
        /// own target arm is proved at the offer rather than through this drive.</para>
        /// </summary>
        [Test]
        public void BulletTime_PerformsThePrimaryOfTheBasicCardItJustResolved()
        {
            var fixture = new BulletTimeTurn();

            fixture.Drive();

            Assert.That(fixture.BorrowOffered, Is.True,
                fixture.Trace() + " | The ability offered the primary of the card just resolved.");
            Assert.That(fixture.BorrowCandidates, Does.Contain(fixture.BounceInstanceId),
                "The pool is `your rightmost resolved card`, and that is the card the trigger fired " +
                "after.");
            Assert.That(fixture.GrantWasRunning, Is.True,
                "The grant names itself for as long as the instance it produced is standing, which is " +
                "what a target answered by a later command is measured against.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(2),
                "Her own primary, then the instance the ability granted - two performances of one " +
                "card's printed Move, which is exactly what p.16's sentence buys.");
            Assert.That(
                GrantedTargetExclusionQuery.RunningGrant(fixture.TurnScopedKeys()), Is.Null,
                "The grant stops running when its instance ends, at the same boundary a card-text " +
                "borrow hands its parent frame back.");
        }

        /// <summary>One of Swift's turns, staged so p.16's ability can fire on a basic card.</summary>
        private sealed class BulletTimeTurn
        {
            private readonly MatchConfiguration configuration;
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] instances;
            private readonly CardInstance bounce;
            private readonly CardActionResolutionAuthority cardActions;
            private readonly List<string> trace = new List<string>();
            private readonly List<CardInstanceId> borrowCandidates = new List<CardInstanceId>();
            private TurnResolutionAuthority turn;

            public BulletTimeTurn()
            {
                configuration =
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
                var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                var bounceDefinition = SwiftCardContent.Definition(SwiftCardContent.Bounce);
                var ultimateDefinition = SwiftCardContent.Definition(SwiftCardContent.BulletTime);
                bounce = CardInstance.CreateInHand(
                    new CardInstanceId("bullet-bounce"), bounceDefinition.Reference, actor);
                var ultimate = CardInstance.CreateInDeck(
                    new CardInstanceId("bullet-ultimate"), ultimateDefinition.Reference, actor);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);

                definitions = configuration.CardDefinitions
                    .Concat(new[] { bounceDefinition, ultimateDefinition })
                    .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
                instances = configuration.CardInstances.Concat(new[] { bounce, ultimate }).ToArray();

                var coordinates = new List<HexCoordinate>();
                for (var q = -5; q <= 5; q++)
                for (var r = -5; r <= 5; r++)
                {
                    if (Math.Abs(q + r) <= 5) coordinates.Add(new HexCoordinate(q, r));
                }

                var board = new AdjacentMoveAuthority(
                    coordinates,
                    new[]
                    {
                        new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(FirstTarget, new HexCoordinate(4, 0)),
                        new HeroOccupancy(SecondTarget, new HexCoordinate(0, 4))
                    },
                    Enumerable.Empty<ObstacleOccupancy>(),
                    Enumerable.Empty<QuickGameMinionSetupState>());

                cardActions = new CardActionResolutionAuthority(
                    new MatchContentCatalogs(
                        SyntheticEffectCapabilities.CreateCatalog(
                            SyntheticPlacementCapabilities.DeferredHandlers()
                                .Concat(SyntheticRosterCapabilities.DeferredHandlers())),
                        SyntheticComponentDefinitions.CreateCatalog(),
                        new TriggeredEffectCatalog(
                            SwiftEffectContent.TriggeredEffects(SwiftCardContent.ContentRevision))),
                    board);

                var committed = new List<CardInstance> { bounce };
                foreach (var owner in new[] { FirstTarget, SecondTarget })
                {
                    committed.Add(configuration.CardInstances.First(value =>
                        value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Hand));
                }

                foreach (var card in committed)
                {
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                }

                typeof(CardActionResolutionAuthority)
                    .GetMethod("BindRosterState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(cardActions, new object[]
                    {
                        EffectRosterState.Of(
                            new[]
                            {
                                new RoundTurnSlot(
                                    1, 1,
                                    committed.Select(card =>
                                        new TurnCardAssignment(card.OwnerId, card.Id, false)),
                                    instances,
                                    configuration.Participants)
                            },
                            new RoundHeroDefeatHistory(configuration.Participants),
                            new HeroCoinLedger(configuration.Participants))
                    });

                turn = new TurnResolutionAuthority(
                    definitions, instances, configuration.Participants,
                    new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
                Assert.That(turn.TryStart().Accepted, Is.True);
            }

            public CardInstanceId BounceInstanceId => bounce.Id;

            public bool BorrowOffered { get; private set; }

            public bool GrantWasRunning { get; private set; }

            public int PerformedMoves { get; private set; }

            public IReadOnlyList<CardInstanceId> BorrowCandidates => borrowCandidates;

            public string Trace() => string.Join(",", trace);

            public IReadOnlyList<string> TurnScopedKeys() =>
                ((TurnResolutionRestoreState)typeof(TurnResolutionAuthority)
                    .GetMethod("CaptureRestoreState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(turn, null)).ConsumedTurnScopedKeys;

            public void Drive()
            {
                var started = false;
                for (var step = 0; step < 80 && !turn.IsEnded; step++)
                {
                    if (!started && turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        trace.Add("tie");
                        Assert.That(turn.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(bounce.Id)
                                ? bounce.Id
                                : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    if (!started && turn.PendingTurnRelease != null)
                    {
                        trace.Add("release");
                        Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (turn.PendingEffectChoice != null)
                    {
                        var effect = turn.PendingEffectChoice;
                        trace.Add("choice:" + effect.NodeId.Value);
                        if (effect.NodeId.Value == "bullet-borrow")
                        {
                            BorrowOffered = true;
                            borrowCandidates.AddRange(effect.Candidates
                                .Where(candidate =>
                                    candidate.Value.Kind == EffectValueKind.CardInstance)
                                .Select(candidate => candidate.Value.CardInstanceId));
                        }

                        // One line choice is visited twice - once to select the destination and once
                        // to confirm it - so the instance is counted at its first visit rather than at
                        // every one.
                        if (effect.NodeId.Value == "bounce-line" &&
                            effect.SelectedCandidates.Count == 0)
                        {
                            PerformedMoves++;
                        }

                        Assert.That(turn.TryAnswerEffectChoice(
                            effect.SelectInvocations.Count > 0
                                ? effect.SelectInvocations[0]
                                : effect.ConfirmInvocation).Accepted, Is.True);
                        GrantWasRunning |= GrantedTargetExclusionQuery
                            .RunningGrant(TurnScopedKeys()) != null;
                        continue;
                    }

                    if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        if (!turn.SelectedActor.CardInstanceId.Equals(bounce.Id))
                        {
                            trace.Add("hold");
                            Assert.That(turn.TryPerformHold(
                                turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                            continue;
                        }

                        trace.Add("start");
                        var offer = ActionOfferQuery
                            .GetAvailableTurnActionOffers(
                                definitions, instances, bounce.OwnerId, bounce.Id)
                            .Single(value => value.ActionDefinitionId.Equals(
                                SwiftCardContent.PrimaryBasicSkill));
                        Assert.That(turn.TryStartSkill(
                            new ActionResolutionId("bullet-own"), offer).Accepted, Is.True);
                        started = true;
                        continue;
                    }

                    trace.Add("break:ended=" + turn.IsEnded);
                    break;
                }
            }
        }
    }
}
