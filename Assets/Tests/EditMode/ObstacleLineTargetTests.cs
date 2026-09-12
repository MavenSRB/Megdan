using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39b. The obstacle line Cutter's Silver basic prints, proved as vocabulary before
    /// any of her content exists: "Target an obstacle in range and in a straight line, with no
    /// obstacles between you; ignore immunity. Move in a straight line towards that obstacle until you
    /// are adjacent."
    ///
    /// <para><b>The rulebook settled two questions this block was scoped to litigate.</b> p.18 defines
    /// an obstacle as "Units, terrain (including printed on the board), tokens ... There can never be
    /// more than one obstacle in any one space. Ever. No exceptions." - so an obstacle is named
    /// one-to-one by its space, which is why this selection publishes a coordinate and why no enum
    /// gained a member. And p.18's Immune keyword reads "you cannot target that unit ... unless the
    /// card text says otherwise", so "ignore immunity" is doing real work rather than being boilerplate
    /// (her shard's CU-Q2): an obstacle may BE a unit, and a unit may be immune.</para>
    ///
    /// <para><b>The clear line is a printed narrowing of a default that admits everything.</b> p.9:
    /// "There are no 'line of sight' rules. You can target units through obstacles, as long as they are
    /// in range/radius." So the clause is the card's own restriction, and its arithmetic consequence is
    /// that each file offers at most one candidate.</para>
    /// </summary>
    [TestFixture]
    public sealed class ObstacleLineTargetTests
    {
        private const int PrintedRange = 5;

        private static readonly ActionDefinitionId BoltAction = new ActionDefinitionId("probe-bolt");
        private static readonly CardDefinitionId BoltCard = new CardDefinitionId("probe-bolt-card");
        private static readonly CardInstanceId BoltInstance =
            new CardInstanceId("probe-bolt-instance");

        private static HexCoordinate East(int steps) =>
            GrappleLineBoard.At(HexDirection.East, steps);

        // -------------------------------------------------------------------------------------------
        // What the declaration is, and where it sits
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate, both halves. It is a NEW handler type and no dealt card starts it
        /// until milestone 8.39i deals Cutter, so it is deferred rather than registered - a capability
        /// registered before a dealt card can start it moves pinned bytes.
        /// </summary>
        [Test]
        public void TheObstacleSelection_IsBuiltAndDeferredRatherThanRegistered()
        {
            Assert.That(
                SyntheticTargetingCapabilities.DeferredHandlers().Select(handler => handler.Reference),
                Does.Contain(SyntheticTargetingCapabilities.RangeObstacleTargetV1),
                "Grappling Bolt is the only face in the corpus that targets an obstacle, and she is " +
                "not dealt yet.");
            Assert.That(
                SyntheticTargetingCapabilities.RegisteredHandlers()
                    .Any(handler => handler.Reference.Equals(
                        SyntheticTargetingCapabilities.RangeObstacleTargetV1)),
                Is.False,
                "and registering it before then would move pinned bytes.");
        }

        /// <summary>
        /// It declares p.18's unqualified immunity arm, which is the reason the printed clause names.
        /// A selection that could not say so would have to offer an immune unit and refuse it later.
        /// </summary>
        [Test]
        public void TheObstacleSelection_DeclaresTheUnqualifiedImmunityOverride()
        {
            Assert.That(
                ((IEffectImmunityOverrideCapabilityHandler)Handler())
                    .SupportedImmunityOverrideActivities,
                Is.EquivalentTo(new[] { RuleActivity.All }));
        }

        // -------------------------------------------------------------------------------------------
        // "an obstacle in range and in a straight line, with no obstacles between you"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The clause's arithmetic: the first obstacle on a file is the only one that file can ever
        /// offer, because a second has the first between it and the actor. Asserted against p.9's
        /// stated default, which would have offered both.
        /// </summary>
        [Test]
        public void EachFile_OffersItsFirstObstacleAndNothingBehindIt()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyMinionsAt: new[] { East(1), East(3) }, withTerrainRing: false);

            Assert.That(Candidates(board), Is.EqualTo(new[] { East(1) }),
                "p.9 would have offered both - 'You can target units through obstacles, as long as " +
                "they are in range' - and this card prints the clause that does not.");
        }

        /// <summary>
        /// Terrain is an obstacle by p.19's own definition, so a hero standing on an otherwise empty
        /// board grapples to the ring around it - one candidate per file and no more.
        /// </summary>
        [Test]
        public void AnEmptyBoard_OffersTheTerrainRing_OnePerFile()
        {
            var candidates = Candidates(GrappleLineBoard.CreateBoard());

            Assert.That(candidates.Count, Is.EqualTo(6),
                "Six files, one first obstacle each; the printed Range of 5 reaches the ring at 5.");
            Assert.That(
                candidates.All(at => GrappleLineBoard.Terrain.Contains(at)), Is.True,
                "and every one of them is terrain, which p.19 makes an obstacle 'including the water " +
                "spaces surrounding the board'.");
        }

        /// <summary>
        /// Off the map is the absence of a space rather than an obstacle - the reading
        /// <c>choose-line-destination@1</c> already records - so a file that runs out of board offers
        /// nothing at all rather than offering its edge.
        /// </summary>
        [Test]
        public void AFileThatRunsOffTheMap_OffersNothing()
        {
            var board = GrappleLineBoard.CreateBoard(withTerrainRing: false);

            Assert.That(Candidates(board), Is.Empty,
                "Playable space ends at 4, the printed Range is 5, and with no terrain ring there is " +
                "nothing on any file to grapple to.");
        }

        /// <summary>
        /// An obstacle past the printed Range is out of reach, which is p.9's bound doing its ordinary
        /// work on a selection that reads the icon rather than an integer.
        /// </summary>
        [Test]
        public void AnObstacleBeyondThePrintedRange_IsNotOffered()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyMinionsAt: new[] { East(1) }, withTerrainRing: false);

            Assert.That(Candidates(board, printedRange: 0), Is.Empty,
                "A card printing no Range icon has no such bound and p.16 does not invent one.");
            Assert.That(Candidates(board, printedRange: 1), Is.EqualTo(new[] { East(1) }),
                "and a Range of exactly one reaches an obstacle exactly one away.");
        }

        // -------------------------------------------------------------------------------------------
        // "Move in a straight line towards that obstacle until you are adjacent"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The approach space is one step back along the file the selection walked, published beside
        /// the target so the Move that follows reads the same line rather than re-deriving it.
        /// </summary>
        [Test]
        public void TheApproachSpace_IsOneStepBackAlongTheSameFile()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyMinionsAt: new[] { East(3) }, withTerrainRing: false);

            Assert.That(Approach(board, East(3)), Is.EqualTo(East(2)));
        }

        /// <summary>
        /// An obstacle already adjacent publishes the actor's own space: "until you are adjacent" is
        /// satisfied where she stands, so the Move it feeds is a move of no spaces rather than an
        /// impossible step. The card bars neither the target nor the Move, so neither does this.
        /// </summary>
        [Test]
        public void AnObstacleAlreadyAdjacent_PublishesTheActorsOwnSpaceAsTheApproach()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyMinionsAt: new[] { East(1) }, withTerrainRing: false);

            Assert.That(Candidates(board), Is.EqualTo(new[] { East(1) }));
            Assert.That(Approach(board, East(1)), Is.EqualTo(GrappleLineBoard.Origin));
        }

        /// <summary>
        /// <b>Arrival is guaranteed on this card, which is the opposite of what this block expected to
        /// find.</b> "Until you are adjacent" does not promise arrival in general - p.19 stops a move
        /// at the first thing that stops it - but the same sentence prints "with no obstacles between
        /// you", and p.18's obstacles include units. A line the clause admits therefore has nothing on
        /// it to stop her, so every space between the actor and the approach is empty by construction.
        /// </summary>
        [Test]
        public void TheClearLineClause_LeavesNothingOnThePathToStopHer()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(2) },
                enemyMinionsAt: new[] { East(4) },
                withTerrainRing: false);

            var approach = Approach(board, East(2));
            Assert.That(approach, Is.EqualTo(East(1)));

            var between = Enumerable.Range(1, 1).Select(East).ToArray();
            Assert.That(
                between.All(at => !ObstacleQuery.IsObstacle(board, at, RuleModificationSet.Empty)),
                Is.True,
                "Every space the approach crosses is empty, because a space that was not would have " +
                "been the first obstacle on this file and the candidate instead.");
        }

        // -------------------------------------------------------------------------------------------
        // "ignore immunity"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// An immune unit is not offered to a node that does not print the clause - and still occupies
        /// its file, so nothing behind it is offered either. That second half is the one worth
        /// asserting: immunity is a rule about being targeted, not about standing somewhere.
        /// </summary>
        [Test]
        public void AnImmuneUnit_IsNotOfferedWithoutTheClauseAndStillBlocksItsFile()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(1) },
                enemyMinionsAt: new[] { East(3) },
                withTerrainRing: false);
            var immunity = ImmunityFor(GrappleLineBoard.Enemies[0], RuleActivity.All);

            Assert.That(Candidates(board, modifications: immunity), Is.Empty,
                "The immune hero cannot be targeted, and the minion behind him is not offered in his " +
                "place - he is an obstacle whether or not he can be chosen.");
        }

        /// <summary>
        /// The clause is what changes that answer, and it is declared rather than assumed: the same
        /// board, the same immunity, one <see cref="ImmunityOverrideReason.PrintedUnqualifiedImmunity"/>
        /// declaration, and the immune hero becomes the candidate.
        /// </summary>
        [Test]
        public void TheUnqualifiedOverride_ReachesAnImmuneUnitTheClauselessNodeCannot()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(1) }, withTerrainRing: false);
            var immunity = ImmunityFor(GrappleLineBoard.Enemies[0], RuleActivity.All);

            Assert.That(
                Candidates(board, modifications: immunity, overrides: UnqualifiedOverride()),
                Is.EqualTo(new[] { East(1) }),
                "p.18: 'you cannot target that unit ... unless the card text says otherwise.'");
        }

        /// <summary>
        /// The Heavy-minion reason is unchanged, which is what keeps Sabina's and Xargatha's existing
        /// declarations byte-identical in behaviour: it bypasses p.22's standing rule and never a
        /// card-declared immunity, exactly as its own remarks have always said.
        /// </summary>
        [Test]
        public void TheHeavyMinionReason_StillBypassesOnlyTheStandingRule()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(1) }, withTerrainRing: false);
            var immunity = ImmunityFor(GrappleLineBoard.Enemies[0], RuleActivity.All);
            var heavyOnly = new[]
            {
                new ImmunityOverrideDeclaration(
                    RuleActivity.All, ImmunityOverrideReason.PrintedHeavyMinionImmunity)
            };

            Assert.That(Candidates(board, modifications: immunity, overrides: heavyOnly), Is.Empty,
                "A card naming p.22's rule bypasses p.22's rule and nothing else.");
        }

        // -------------------------------------------------------------------------------------------
        // One query, three readings
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The set the offer opens with is the set the answer is checked against, so an obstacle
        /// behind the first one cannot be submitted even though it is an obstacle in range.
        /// </summary>
        [Test]
        public void TheOfferTheAnswerAndTheResolution_ReadTheOneQuery()
        {
            var board = GrappleLineBoard.CreateBoard(
                enemyMinionsAt: new[] { East(1), East(3) }, withTerrainRing: false);
            var handler = Handler();
            var context = Context(board);

            Assert.That(handler.Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                handler.Resume(context, new[] { EffectValue.Space(East(1)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                handler.Resume(context, new[] { EffectValue.Space(East(3)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "An obstacle in range that the clear-line clause excluded is refused by the same " +
                "query that left it out of the offer.");
        }

        /// <summary>An answer with nothing to answer reports an impossible step rather than throwing.</summary>
        [Test]
        public void AnEmptyOffer_IsAnImpossibleStep()
        {
            var board = GrappleLineBoard.CreateBoard(withTerrainRing: false);

            Assert.That(Handler().Anticipate(Context(board)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(Handler().Execute(Context(board)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler() =>
            SyntheticTargetingCapabilities.Handlers()
                .Single(handler =>
                    handler.Reference.Equals(SyntheticTargetingCapabilities.RangeObstacleTargetV1));

        private static IReadOnlyList<HexCoordinate> Candidates(
            AdjacentMoveAuthority board,
            int printedRange = PrintedRange,
            RuleModificationSet modifications = null,
            IReadOnlyList<ImmunityOverrideDeclaration> overrides = null)
        {
            var opened = Handler().Execute(Context(board, printedRange, modifications, overrides));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HexCoordinate>()
                : opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
        }

        private static HexCoordinate Approach(AdjacentMoveAuthority board, HexCoordinate obstacle)
        {
            var applied = Handler().Resume(
                Context(board), new[] { EffectValue.Space(obstacle) });
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            return applied.Outcomes
                .Single(fact => fact.OutputName ==
                    SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName)
                .Value.Coordinate;
        }

        private static IReadOnlyList<ImmunityOverrideDeclaration> UnqualifiedOverride() =>
            new[]
            {
                new ImmunityOverrideDeclaration(
                    RuleActivity.All, ImmunityOverrideReason.PrintedUnqualifiedImmunity)
            };

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            int printedRange = PrintedRange,
            RuleModificationSet modifications = null,
            IReadOnlyList<ImmunityOverrideDeclaration> overrides = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("probe-bolt-node"),
                EffectFailurePolicy.Mandatory,
                GrappleLineBoard.Actor,
                GrappleLineBoard.Actor,
                BoltInstance,
                new CardDefinitionReference(BoltCard, "probe-rev-1"),
                BoltAction,
                new Dictionary<string, EffectValue>
                {
                    ["activity"] = EffectValue.Integer((int)RuleActivity.All)
                },
                1,
                1,
                GrappleLineBoard.Participants(),
                null,
                modifications ?? RuleModificationSet.Empty,
                EffectCardState.Of(
                    new[] { BoltDefinition(printedRange) }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                immunityOverrides: overrides);

        /// <summary>
        /// A card printing the Range this selection reads its reach from. A probe definition rather
        /// than Cutter's, because she has no content until milestone 8.39h and the clause is not hers
        /// alone - a later hero printing "target an obstacle in range" takes the same declaration.
        /// </summary>
        private static CardDefinition BoltDefinition(int printedRange) =>
            new CardDefinition(
                BoltCard,
                "probe-rev-1",
                "Probe Bolt",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        BoltAction,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        printedRange > 0
                            ? new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Range, printedRange)
                            }
                            : Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        null,
                        // The capability under test is driven directly; this definition exists only to
                        // print the Range its reach is read from, which is milestone 8.38b's own
                        // reason for a probe card that carries no root.
                        PrintedTextComposition.DeferredUntil(
                            "never - this is milestone 8.39b's Range-printing probe, not content"))
                });

        /// <summary>
        /// A live modification set carrying one immunity, built the way the match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled, on
        /// <c>TargetQueryTests</c>' terms.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"grapple-line-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"GrappleLine{activity}Immunity",
                $"grapple-line-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"grapple-line-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("grapple-line-immunity-source"),
                new ActionDefinitionId("grapple-line-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                StagedBoardParticipants.Create(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, null,
                null);
            Assert.That(
                set.IsImmuneTo(
                    RuleObject.ForHero(heroId), activity, RuleObject.ForHero(GrappleLineBoard.Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the cases above prove nothing.");
            return set;
        }
    }
}
