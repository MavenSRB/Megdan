using System;
using System.Collections.Generic;
using System.Linq;
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
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.82. Swift's two minion-commanding faces, driven: "Move [up to two] enemy minion[s]
    /// in radius up to 3 spaces to [a space / spaces] in radius." - Mark for Death and Hunting Season.
    ///
    /// <para><b>The defect this file closes, in one sentence: the minion could never go anywhere.</b>
    /// The move declared milestone 8.22's <c>WithinActorRadius</c> and bound it NO number at all, and
    /// <c>DirectMoveDestinations.Admit</c> reads a radius policy with no radius as admitting nothing -
    /// "an undeclared bound is not an absent bound". So the node had no destination for any minion on
    /// any board; the selection above it declares that move as its CONSUMER, a consumer declaration
    /// narrows the offer to candidates the consumer can act on, and the whole face therefore offered
    /// the player nobody. It was reported from a live playtest in exactly those words - "Swift is not
    /// being offered blue minion to move" - and the host log shows the Skill used with no minion Move
    /// row after it.</para>
    ///
    /// <para><b>The leash is the card's own icon, not a number.</b> p.9: "if the card text mentions
    /// Range/Radius, it refers to this value", and p.16 raises that value wherever the hero prints it.
    /// The second "in radius" is the SAME icon as the first, so a declared literal 4 would have been
    /// the wrong disk the moment she held Super-Shotgun's +1 Radius item - which she did on the night
    /// this was reported, which is the second half of what she asked for ("Swift has +1 radius as
    /// well"). Milestone 8.82 mints
    /// <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/> for it and both faces
    /// declare it.</para>
    ///
    /// <para><b>The same node's distance was exact and the card prints "up to".</b> It declared
    /// <see cref="EffectFailurePolicy.IfAble"/>, which p.11 reads as "you must move the exact number of
    /// spaces", so even with the leash bound the only destinations would have been the ones exactly
    /// three steps out. "Move an enemy minion in radius <b>up to 3 spaces</b>" is p.11's optional
    /// distance, which is <see cref="EffectFailurePolicy.UpTo"/> - the reading Xargatha's Siren's Call
    /// and every other printed "up to" already takes. It is corrected here because it is the same
    /// clause of the same sentence on the same node.</para>
    ///
    /// <para>The drive is the real effect definition walked by
    /// <see cref="EffectResolutionSession"/> over a board built for the question, rather than the
    /// staged engagement: what this file measures is which spaces a disk admits, and a board holding
    /// other pieces would answer that for reasons belonging to the staging.</para>
    /// </summary>
    public sealed class SwiftMarkForDeathTests
    {
        private static readonly MapId Map = new MapId("swift-mark-for-death-map");
        private static readonly HeroId Swift = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly TeamId EnemyTeam = new TeamId(StagedBoardParticipants.BlueTeamIdValue);

        private static readonly HexCoordinate SwiftAt = new HexCoordinate(0, 0);

        /// <summary>The playable disk, wide enough that no case is answered by the map's edge.</summary>
        private static readonly IReadOnlyList<HexCoordinate> Playable = Disk(SwiftAt, 9);

        /// <summary>Mark for Death and Hunting Season both print Radius 4.</summary>
        private const int PrintedRadius = 4;

        /// <summary>"Move an enemy minion in radius up to 3 spaces."</summary>
        private const int PrintedDistance = 3;

        // -------------------------------------------------------------------------------------------
        // The whole milestone
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE PLAYTEST REPORT, as a case. An enemy minion three spaces from Swift is a legal subject
        /// and the move it is chosen for has somewhere to put it.
        ///
        /// <para>Both assertions fail on the shipped composition, and the first one is the sentence
        /// the player typed: the selection offers nobody, because its consumer can act on nobody.</para>
        /// </summary>
        [Test]
        public void MarkForDeath_OffersTheEnemyMinionInRadius_AndReallyMovesIt()
        {
            var minionAt = new HexCoordinate(3, 0);
            var drive = Drive(SwiftCardContent.MarkForDeath, new[] { minionAt });

            Assert.That(drive.OfferedMinions, Has.Count.EqualTo(1),
                "\"Move an enemy minion in radius\" - a minion three spaces away is inside her " +
                "printed Radius 4, so the selection must offer it. It offered nobody, because the " +
                "move it declares as its consumer could act on nobody.");
            Assert.That(drive.OfferedDestinations, Is.Not.Empty,
                "and the move it was chosen for must have somewhere to put it.");
            Assert.That(drive.SpaceOfTheMovedMinion, Is.Not.EqualTo(minionAt),
                "The minion really leaves the space it stood on.");
        }

        /// <summary>
        /// The leash itself: every space inside her printed Radius disk that the minion can walk to
        /// within the printed distance, and not one space outside it.
        ///
        /// <para>The expectation is computed from p.9's two measures rather than written out, so a
        /// case that passed by naming a short list cannot hide a disk of the wrong size.</para>
        /// </summary>
        [Test]
        public void TheMovesDestinations_AreHerPrintedRadiusDiskAndNothingOutsideIt()
        {
            var minionAt = new HexCoordinate(3, 0);
            var drive = Drive(SwiftCardContent.MarkForDeath, new[] { minionAt });

            Assert.That(
                drive.OfferedDestinations.Where(space =>
                    HexDistanceQuery.Calculate(SwiftAt, space) > PrintedRadius),
                Is.Empty,
                "\"to a space in radius\" - nothing outside p.9's disk, measured inclusively from " +
                "where she stands when the node runs.");
            Assert.That(
                drive.OfferedDestinations,
                Is.EquivalentTo(ExpectedDestinations(minionAt, PrintedRadius, occupied: minionAt)),
                "and everything inside it the minion can reach at `up to 3 spaces`, including the " +
                "space it already stands on - p.11 applies a card's text \"even if you choose to " +
                "move zero spaces\".");
        }

        /// <summary>
        /// p.16, and the half of the report a literal could never have answered. Her Super-Shotgun
        /// grants +1 Radius, so the disk BOTH halves of the sentence measure is 5 - a minion five
        /// spaces away is offered, and it may be put down five spaces away.
        /// </summary>
        [Test]
        public void APlusOneRadiusItem_WidensTheOfferAndTheLeashTogether_ToFive()
        {
            var minionAt = new HexCoordinate(5, 0);
            var unarmed = Drive(SwiftCardContent.MarkForDeath, new[] { minionAt });
            Assert.That(unarmed.OfferedMinions, Is.Empty,
                "Five spaces is outside a printed Radius 4, so without the item there is nothing to " +
                "offer at all - which is what makes the next assertion about the item.");

            var drive = Drive(
                SwiftCardContent.MarkForDeath, new[] { minionAt }, radiusItems: 1);

            Assert.That(drive.OfferedMinions, Has.Count.EqualTo(1),
                "p.16 raises the printed icon by one, so the selection's disk is 5 and the minion " +
                "is inside it.");
            Assert.That(
                drive.OfferedDestinations,
                Is.EquivalentTo(ExpectedDestinations(minionAt, PrintedRadius + 1, occupied: minionAt)),
                "THE POINT: the second \"in radius\" is the same icon as the first, so the leash is " +
                "5 as well. A declared literal 4 would have left the offer wider than the move.");
            Assert.That(
                drive.OfferedDestinations.Any(space =>
                    HexDistanceQuery.Calculate(SwiftAt, space) == PrintedRadius + 1),
                Is.True,
                "and a space on the item's own ring is one of the answers.");

            var chosen = drive.OfferedDestinations.First(space =>
                HexDistanceQuery.Calculate(SwiftAt, space) == PrintedRadius + 1);
            var moved = Drive(
                SwiftCardContent.MarkForDeath, new[] { minionAt }, radiusItems: 1,
                destination: chosen);
            Assert.That(moved.SpaceOfTheMovedMinion, Is.EqualTo(chosen),
                "and naming it really puts the minion down five spaces from her.");
        }

        /// <summary>
        /// Hunting Season is the same sentence with two promises behind it, and it carries the same
        /// leash - the disk is drawn from ITS printed Radius icon rather than from Mark for Death's.
        /// </summary>
        [Test]
        public void HuntingSeason_MovesAMinionUnderTheSameLeash_AndStandsBothOfItsPromises()
        {
            var minionAt = new HexCoordinate(3, 0);
            var drive = Drive(
                SwiftCardContent.HuntingSeason,
                new[] { minionAt, new HexCoordinate(-2, 0) },
                subject: minionAt);

            Assert.That(drive.OfferedMinions, Has.Count.EqualTo(2),
                "Both enemy minions stand inside her printed Radius 4.");
            Assert.That(
                drive.OfferedDestinations,
                Is.EquivalentTo(ExpectedDestinations(
                    minionAt, PrintedRadius, occupied: new[] { minionAt, new HexCoordinate(-2, 0) })),
                "and the minion she names is kept inside the same disk Mark for Death draws.");
            Assert.That(drive.SpaceOfTheMovedMinion, Is.Not.EqualTo(minionAt));
            Assert.That(drive.PromisesStanding, Is.EqualTo(2),
                "\"The first two times ... gain 1 coin\" is two promises, and the move running is " +
                "what lets the walk reach them at all.");
        }

        /// <summary>
        /// The reading that keeps the new member honest: p.16 raises a printed icon and never invents
        /// one, so a card carrying no Radius has no such bound at all and the policy admits nothing.
        /// A destination set produced from a missing icon would be the widest possible reading of the
        /// one clause this member exists to narrow.
        /// </summary>
        [Test]
        public void ACardPrintingNoRadius_AdmitsNoDestinationUnderThePrintedPolicy()
        {
            var minionAt = new HexCoordinate(1, 0);
            var board = CreateBoard(new[] { minionAt });
            var handler = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticDisplacementCapabilities.DirectMoveV3);

            Assert.That(
                handler.Anticipate(NoIconContext(board, minionAt, EffectFailurePolicy.Mandatory,
                    DirectMoveDestinationPolicy.WithinActorPrintedRadius)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "No printed Radius is no disk, so the policy admits nothing and a step that must " +
                "travel has nowhere to go.");

            Assert.That(
                handler.Anticipate(NoIconContext(board, minionAt, EffectFailurePolicy.Mandatory,
                    DirectMoveDestinationPolicy.Unrestricted)),
                Is.Not.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and the same board under a policy that draws no disk is perfectly movable, so the " +
                "refusal above is the icon's absence rather than the board's shape.");

            // Under an optional distance the mover's own space is still one of the answers, because
            // standing still is admitted by the WORDING and not by the geometry - the handler adds it
            // beside whatever the policy admitted rather than through it. What the missing icon takes
            // away is every space the piece could travel to, which is the whole of this bound.
            var offered = handler.Execute(NoIconContext(board, minionAt, EffectFailurePolicy.UpTo,
                DirectMoveDestinationPolicy.WithinActorPrintedRadius));
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EqualTo(new[] { minionAt }),
                "p.11 applies a card's text \"even if you choose to move zero spaces\", and an " +
                "absent icon leaves that as the only answer rather than widening the move to the " +
                "whole board.");
        }

        /// <summary>
        /// The new member is classified exactly as milestone 8.22's declared one is, everywhere it is
        /// asked about. The two draw one disk from one centre and differ only in where the number
        /// comes from, so an actor-relative question that answered them differently would be a
        /// disagreement this vocabulary cannot express.
        /// </summary>
        [Test]
        public void ThePrintedRadiusPolicy_IsActorRelativeExactlyAsTheDeclaredOneIs()
        {
            Assert.That(
                DirectMoveDestinations.NeedsActorPosition(
                    DirectMoveDestinationPolicy.WithinActorPrintedRadius),
                Is.EqualTo(DirectMoveDestinations.NeedsActorPosition(
                    DirectMoveDestinationPolicy.WithinActorRadius)));

            var reachable = Disk(new HexCoordinate(2, 0), 3)
                .Where(space => !space.Equals(SwiftAt))
                .ToArray();
            Assert.That(
                DirectMoveDestinations.Admit(
                    DirectMoveDestinationPolicy.WithinActorPrintedRadius,
                    SwiftAt, new HexCoordinate(2, 0), reachable, actorRadius: PrintedRadius),
                Is.EquivalentTo(DirectMoveDestinations.Admit(
                    DirectMoveDestinationPolicy.WithinActorRadius,
                    SwiftAt, new HexCoordinate(2, 0), reachable, actorRadius: PrintedRadius)),
                "One disk, one filter: what differs is who worked out the number.");
            Assert.That(
                DirectMoveDestinations.Admit(
                    DirectMoveDestinationPolicy.WithinActorPrintedRadius,
                    SwiftAt, new HexCoordinate(2, 0), reachable),
                Is.Empty,
                "and a printed policy handed no total admits nothing, exactly as an undeclared " +
                "literal does.");
        }

        /// <summary>
        /// The correction read off the composition rather than inferred from the drives above: both
        /// faces point at the icon, neither writes a number down, and the distance is the optional one
        /// the card prints.
        /// </summary>
        [Test]
        public void BothFaces_DeclareThePrintedRadiusPolicy_BindNoLiteralRadius_AndPrintAnOptionalDistance()
        {
            foreach (var cardId in new[]
                { SwiftCardContent.MarkForDeath, SwiftCardContent.HuntingSeason })
            {
                var move = MoveNode(cardId);
                Assert.That(
                    (DirectMoveDestinationPolicy)move.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName)
                        .Literal.IntegerValue,
                    Is.EqualTo(DirectMoveDestinationPolicy.WithinActorPrintedRadius),
                    cardId + " says \"to a space in radius\", which is the icon.");
                Assert.That(
                    move.Inputs.Any(input => input.ParameterName ==
                        SyntheticDisplacementCapabilities.ActorRadiusParameterName),
                    Is.False,
                    cardId + " must write no number down: a literal would stop following the item.");
                Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    cardId + " prints \"up to 3 spaces\", which p.11 makes the distance itself the " +
                    "decision - an exact reading would offer only the third ring.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The drive
        // -------------------------------------------------------------------------------------------

        private sealed class DriveResult
        {
            internal readonly List<MinionId> OfferedMinions = new List<MinionId>();
            internal readonly List<HexCoordinate> OfferedDestinations = new List<HexCoordinate>();
            internal MinionId ChosenMinion;
            internal HexCoordinate SpaceOfTheMovedMinion;
            internal int PromisesStanding;
        }

        /// <summary>
        /// Swift at the origin with the named enemy minions standing where the case puts them, her
        /// named face resolved, and every question answered - the first minion offered, and either the
        /// named destination or the first one that really travels.
        /// </summary>
        private static DriveResult Drive(
            CardDefinitionId cardId,
            IReadOnlyList<HexCoordinate> minionSpaces,
            int radiusItems = 0,
            HexCoordinate? destination = null,
            HexCoordinate? subject = null)
        {
            var definition = SwiftCardContent.Definition(cardId);
            var action = definition.ActionDefinitions.Single(
                candidate => candidate.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var participants = StagedBoardParticipants.Create();
            var board = CreateBoard(minionSpaces);
            var card = CardInstance.CreateInHand(
                new CardInstanceId("swift-mark-probe"), definition.Reference, Swift);
            var timing = new ActiveEffectLifecycle();

            var build = MatchContentCatalogs.ForThisBuild();
            var capabilities = new EffectCapabilityCatalog(
                build.Capabilities.Registered.Select(build.Capabilities.Load)
                    .Concat(SwiftActiveEffectContent.Handlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));

            var context = new EffectResolutionContext(
                action.EffectRoot,
                capabilities,
                participants,
                timing,
                1,
                1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { definition }, new[] { card }),
                action.Kind,
                default,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board),
                Items(participants, radiusItems));

            var result = new DriveResult();
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("swift-mark-probe-resolution"),
                card.Id,
                definition.Reference,
                action.Id,
                Swift,
                Swift,
                ActionInvocationContext.PrintedPrimary,
                context);

            for (var guard = 0; guard < 24; guard++)
            {
                var choice = step.Frame?.PendingChoice;
                if (choice == null)
                {
                    break;
                }

                var minions = choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                    .ToArray();
                var spaces = choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .ToArray();

                EffectChoiceCommand answer;
                if (minions.Length > 0)
                {
                    foreach (var candidate in minions)
                    {
                        if (!result.OfferedMinions.Contains(candidate.Value.MinionId))
                        {
                            result.OfferedMinions.Add(candidate.Value.MinionId);
                        }
                    }

                    // The minion the case names, found by where it stands, and otherwise the first
                    // one offered. A drive that took the first on a two-minion board would measure
                    // the disk around whichever minion the offer happened to list first.
                    var named = subject.HasValue
                        ? choice.SelectInvocations.Single(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                            board.Minions.Single(minion =>
                                    minion.MinionId.Equals(invocation.Candidate.Value.MinionId))
                                .Coordinate.Equals(subject.Value))
                        : choice.SelectInvocations.First(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Minion);
                    result.ChosenMinion = named.Candidate.Value.MinionId;
                    answer = named;
                }
                else if (spaces.Length > 0)
                {
                    foreach (var candidate in spaces)
                    {
                        result.OfferedDestinations.Add(candidate.Value.Coordinate);
                    }

                    // The named space where the case names one, and otherwise the first destination
                    // that is not the minion standing still - a stand-still answer would leave every
                    // "did it move" assertion true of a board nothing happened on.
                    var wanted = destination.HasValue
                        ? choice.SelectInvocations.Single(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            invocation.Candidate.Value.Coordinate.Equals(destination.Value))
                        : choice.SelectInvocations.FirstOrDefault(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            !invocation.Candidate.Value.Coordinate.Equals(
                                board.Minions.Single(minion =>
                                    minion.MinionId.Equals(result.ChosenMinion)).Coordinate));
                    Assert.That(wanted, Is.Not.Null,
                        "The move offered nowhere but the space the minion already stands on.");
                    answer = wanted;
                }
                else if (choice.ConfirmInvocation != null)
                {
                    answer = choice.ConfirmInvocation;
                }
                else
                {
                    answer = choice.SelectInvocations.FirstOrDefault();
                    Assert.That(answer, Is.Not.Null, "A standing question must be answerable.");
                }

                var answered = EffectResolutionSession.Answer(step.Frame, answer, context);
                Assert.That(answered.Accepted, Is.True, "Every answer this drive gives is a legal one.");
                step = answered;

                if (step.Frame?.PendingChoice?.ConfirmInvocation != null &&
                    step.Frame.PendingChoice.SelectedCandidates.Count > 0)
                {
                    step = EffectResolutionSession.Answer(
                        step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
                    Assert.That(step.Accepted, Is.True);
                }
            }

            result.SpaceOfTheMovedMinion = result.OfferedMinions.Count == 0
                ? default
                : board.Minions
                    .Single(minion => minion.MinionId.Equals(result.ChosenMinion))
                    .Coordinate;
            result.PromisesStanding = timing.Effects.Count(
                effect => effect.Phase != ActiveEffectPhase.Ended);
            return result;
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// What the leash must admit, derived from p.9's disk and p.11's reachability rather than
        /// written out: every playable space within the printed distance of the minion that also lies
        /// inside the acting hero's disk, plus the minion's own space, minus whatever is occupied.
        ///
        /// <para>Walk cost and hex distance agree on this board and only on this board: the only
        /// occupied spaces are the hero's and the minions', none of which lies strictly between a
        /// minion and anywhere within three steps of it, so no detour is ever forced. That is the
        /// whole reason the board is a featureless disk.</para>
        /// </summary>
        private static IReadOnlyList<HexCoordinate> ExpectedDestinations(
            HexCoordinate minionAt, int radius, params HexCoordinate[] occupied) =>
            Playable
                .Where(space =>
                    HexDistanceQuery.Calculate(minionAt, space) <= PrintedDistance &&
                    HexDistanceQuery.Calculate(SwiftAt, space) <= radius &&
                    !space.Equals(SwiftAt) &&
                    (space.Equals(minionAt) || !occupied.Contains(space)))
                .ToArray();

        private static AdjacentMoveAuthority CreateBoard(IReadOnlyList<HexCoordinate> minionSpaces) =>
            new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, Playable),
                new[] { new HeroOccupancy(Swift, SwiftAt) },
                Array.Empty<ObstacleOccupancy>(),
                minionSpaces.Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"swift-mark-probe-minion-{index:00}"),
                        EnemyTeam,
                        TeamSide.Blue,
                        index % 2 == 0 ? StandardMinionType.Melee : StandardMinionType.Ranged,
                        default,
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                        at))
                    .ToArray());

        /// <summary>
        /// p.16's items as state: the named number of +1 Radius items under the dashboard slot that
        /// accepts that icon, which is the slot Swift's Super-Shotgun grants one under.
        /// </summary>
        private static HeroItemState Items(MatchParticipants participants, int radiusItems)
        {
            if (radiusItems <= 0)
            {
                return new HeroItemState(participants);
            }

            Assert.That(
                HeroDashboard.RulebookLayout.TryFindSlotAccepting(
                    PrintedValueChannel.Radius, out var slot),
                Is.True,
                "p.16's layout accepts a Radius item.");
            return HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(Enumerable.Range(0, radiusItems)
                    .Select(index => new HeroItem(
                        Swift,
                        new CardInstanceId($"swift-radius-item-{index}"),
                        slot.Position,
                        PrintedValueChannel.Radius,
                        index + 1))
                    .ToArray()));
        }

        /// <summary>
        /// The same Move on a card carrying no printed icon at all, which is the one case the printed
        /// policy answers differently from every board question.
        /// </summary>
        private static EffectCapabilityContext NoIconContext(
            AdjacentMoveAuthority board,
            HexCoordinate moverAt,
            EffectFailurePolicy failurePolicy,
            DirectMoveDestinationPolicy policy) =>
            new EffectCapabilityContext(
                new EffectNodeId("swift-mark-probe-no-icon"),
                failurePolicy,
                Swift,
                Swift,
                NoIconInstance.Id,
                NoIconCard.Reference,
                new ActionDefinitionId("swift-mark-probe-no-icon-action"),
                new Dictionary<string, EffectValue>
                {
                    ["mover"] = EffectValue.Space(moverAt),
                    ["distance"] = EffectValue.Integer(PrintedDistance),
                    [SyntheticDisplacementCapabilities.DestinationPolicyParameterName] =
                        EffectValue.Integer((int)policy)
                },
                1,
                1,
                StagedBoardParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { NoIconCard }, new[] { NoIconInstance }),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board));

        /// <summary>
        /// A card printing a Range icon and no Radius one - the shape p.16's own sentence is about:
        /// "if an Action card has no matching icon, the item has no effect on that particular Action
        /// card", and an icon the card does not carry is not one an item can invent.
        /// </summary>
        private static readonly CardDefinition NoIconCard = new CardDefinition(
            new CardDefinitionId("swift-mark-probe-no-icon-definition"),
            "probe-rev-1",
            "Probe Without A Radius",
            new InitiativeValue(4),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("swift-mark-probe-no-icon-action"),
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    null,
                    PrintedTextComposition.DeferredUntil(
                        "never - this is milestone 8.82's no-Radius probe, not content"))
            });

        private static readonly CardInstance NoIconInstance = CardInstance.CreateInHand(
            new CardInstanceId("swift-mark-probe-no-icon-card"), NoIconCard.Reference, Swift);

        private static EffectNodeDefinition MoveNode(CardDefinitionId cardId) =>
            SwiftCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot.Nodes
                .Single(node => node.Capability.Equals(
                    SyntheticDisplacementCapabilities.DirectMoveV3));

        private static IReadOnlyList<HexCoordinate> Disk(HexCoordinate centre, int radius)
        {
            var spaces = new List<HexCoordinate>();
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius);
                    dr <= Math.Min(radius, -dq + radius);
                    dr++)
                {
                    spaces.Add(new HexCoordinate(centre.Q + dq, centre.R + dr));
                }
            }

            return spaces;
        }
    }
}
