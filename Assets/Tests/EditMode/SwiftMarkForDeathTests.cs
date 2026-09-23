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
            var other = new HexCoordinate(-2, 0);
            var drive = Drive(
                SwiftCardContent.HuntingSeason,
                new[] { minionAt, other },
                subject: minionAt,
                passesTaken: 1);

            Assert.That(drive.MinionOffers[0].Count, Is.EqualTo(2),
                "Both enemy minions stand inside her printed Radius 4.");
            Assert.That(
                drive.DestinationOffers[0],
                Is.EquivalentTo(ExpectedDestinations(
                    minionAt, PrintedRadius, occupied: new[] { minionAt, other })),
                "and the minion she names is kept inside the same disk Mark for Death draws.");
            Assert.That(drive.SpaceOfTheMovedMinion, Is.Not.EqualTo(minionAt));
            Assert.That(drive.PromisesStanding, Is.EqualTo(2),
                "\"The first two times ... gain 1 coin\" is two promises, and the move running is " +
                "what lets the walk reach them at all.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.83: "Move UP TO TWO enemy minions in radius ... to SPACES in radius"
        //
        // THE COUNTS BELOW ARE READ AS `.Count` RATHER THAN THROUGH `Has.Count`, and that is a fact
        // about the Unity tier rather than a style: the Editor's NUnit resolves `Has.Count` by
        // reflecting for a public `Count` property, and an ARRAY has `Length` with `ICollection.Count`
        // implemented explicitly - so every one of these read green in the cross-platform harness and
        // threw `Property Count was not found` on the first Unity run. Measured at 8.83, on three
        // cases, and recorded here so the next fixture that fans a selection into arrays does not pay
        // a Unity run to learn it.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE SECOND ROUTED FINDING, as a case. Hunting Season prints "up to TWO enemy minions" and
        /// composed ONE subject and ONE move until milestone 8.83 - only the bounty count doubled - so
        /// the face moved exactly as many minions as its Tier II sibling, and the word "two" in the
        /// first half of its own sentence did nothing at all.
        ///
        /// <para>It fails on the shipped composition at the first assertion: there was only ever one
        /// selection to open.</para>
        /// </summary>
        [Test]
        public void HuntingSeason_MovesTwoDifferentMinions_AndReallyMovesBothOfThem()
        {
            var first = new HexCoordinate(3, 0);
            var second = new HexCoordinate(-2, 0);
            var drive = Drive(SwiftCardContent.HuntingSeason, new[] { first, second });

            Assert.That(drive.MinionOffers.Count, Is.EqualTo(2),
                "\"Move up to two enemy minions in radius\" asks twice. It asked once.");
            Assert.That(drive.ChosenMinions.Count, Is.EqualTo(2),
                "and both answers name a minion.");
            Assert.That(drive.ChosenMinions[0], Is.Not.EqualTo(drive.ChosenMinions[1]),
                "TWO minions, not one twice.");
            for (var index = 0; index < 2; index++)
            {
                Assert.That(drive.SpacesAfter[index], Is.Not.EqualTo(drive.SpacesBefore[index]),
                    $"and the minion named by pass {index} really left the space it stood on - the " +
                    "assertion milestone 8.82 learned to make, because a drive that only answers " +
                    "cannot tell a move from a step that never ran.");
                Assert.That(
                    HexDistanceQuery.Calculate(SwiftAt, drive.SpacesAfter[index]),
                    Is.LessThanOrEqualTo(PrintedRadius),
                    "\"to SPACES in radius\" - the plural is the same disk, twice.");
            }

            Assert.That(drive.PromisesStanding, Is.EqualTo(2),
                "and both promises still stand behind the two moves.");
        }

        /// <summary>
        /// What makes "two minions" two minions. The second selection declares milestone 8.69c's
        /// <c>excludeMinion</c> against the first selection's own chosen minion, so the minion already
        /// moved is not among the candidates the second pass offers.
        /// </summary>
        [Test]
        public void HuntingSeasonsSecondSelection_ExcludesTheMinionTheFirstOneNamed()
        {
            var first = new HexCoordinate(3, 0);
            var second = new HexCoordinate(-2, 0);
            var third = new HexCoordinate(0, 3);
            var drive = Drive(SwiftCardContent.HuntingSeason, new[] { first, second, third });

            Assert.That(drive.MinionOffers[0].Count, Is.EqualTo(3),
                "All three stand inside her printed Radius 4, so the first pass offers all three.");
            Assert.That(drive.MinionOffers[1].Count, Is.EqualTo(2),
                "and the second pass offers one fewer.");
            Assert.That(drive.MinionOffers[1].Contains(drive.ChosenMinions[0]), Is.False,
                "\"up to two enemy minionS\" - the one already named is out of its own disk for the " +
                "second half of the sentence, which is the roster's `excludeMinion` clause and not a " +
                "new one.");
            Assert.That(drive.MinionOffers[1],
                Is.EquivalentTo(drive.MinionOffers[0]
                    .Where(id => !id.Equals(drive.ChosenMinions[0]))),
                "and nothing ELSE is taken out: the exclusion names one minion, not a ring.");
        }

        /// <summary>
        /// "UP TO two" is a floor of zero at both passes. Taking one and walking away from the second
        /// is a legal answer, and so is walking away from both - which is why each pass is an
        /// <see cref="EffectNodeKind.Optional"/> carrying <see cref="EffectFailurePolicy.UpTo"/>
        /// rather than a mandatory step.
        /// </summary>
        [Test]
        public void HuntingSeason_MayMoveOneMinionOrNone_AndBothPromisesStandEitherWay()
        {
            var first = new HexCoordinate(3, 0);
            var second = new HexCoordinate(-2, 0);

            var one = Drive(SwiftCardContent.HuntingSeason, new[] { first, second }, passesTaken: 1);
            Assert.That(one.ChosenMinions.Count, Is.EqualTo(1),
                "Declining the second pass is legal and the first move stands.");
            Assert.That(one.SpacesAfter[0], Is.Not.EqualTo(one.SpacesBefore[0]));
            Assert.That(one.PromisesStanding, Is.EqualTo(2),
                "\"Next turn: the first two times...\" is behind the moves and does not depend on " +
                "how many of them were taken.");

            var none = Drive(SwiftCardContent.HuntingSeason, new[] { first, second }, passesTaken: 0);
            Assert.That(none.ChosenMinions, Is.Empty,
                "and \"up to two\" includes none, so declining the first pass is legal too.");
            Assert.That(none.PromisesStanding, Is.EqualTo(2),
                "with both promises still standing - the moves are optional and the bounty is not.");
        }

        /// <summary>
        /// The composition read off both faces: Mark for Death keeps exactly one subject-and-move pair
        /// and Hunting Season composes two, the second nested inside the first so that its exclusion
        /// binding reads a node guaranteed to have run.
        ///
        /// <para>The nesting is not a style. <c>EffectDefinitionValidator</c> refuses a binding whose
        /// producer is not guaranteed to have executed, and two optional passes standing as siblings
        /// under the root are exactly that - which is what it said when this was first composed
        /// flat.</para>
        /// </summary>
        [Test]
        public void MarkForDeathComposesOnePair_AndHuntingSeasonComposesTwoNestedOnes()
        {
            Assert.That(MoveNodes(SwiftCardContent.MarkForDeath).Count, Is.EqualTo(1),
                "\"Move AN enemy minion in radius\" is one minion and one move.");

            var hunt = SkillRoot(SwiftCardContent.HuntingSeason);
            Assert.That(MoveNodes(SwiftCardContent.HuntingSeason).Count, Is.EqualTo(2),
                "\"Move up to TWO enemy minions in radius\" is two.");

            var passes = hunt.Nodes
                .Where(node => node.Kind == EffectNodeKind.Optional)
                .ToArray();
            Assert.That(passes, Has.Length.EqualTo(2));
            Assert.That(passes.Select(node => node.FailurePolicy),
                Is.All.EqualTo(EffectFailurePolicy.UpTo),
                "\"up to two\" prints no \"may\", so each pass is p.10's optional COUNT.");

            var firstBody = hunt.Nodes.Single(node => node.Id.Value == "hunt-body-0");
            Assert.That(firstBody.ChildNodeIds.Select(id => id.Value),
                Does.Contain("hunt-pass-1"),
                "The second pass stands INSIDE the first, which is what makes its `excludeMinion` " +
                "binding read a producer the validator can guarantee ran.");

            var secondSubject = hunt.Nodes.Single(node => node.Id.Value == "hunt-subject-1");
            var exclusion = secondSubject.Inputs.Single(input =>
                input.ParameterName ==
                SyntheticTargetingCapabilities.ExcludedMinionParameterName);
            Assert.That(exclusion.SourceNodeId.Value, Is.EqualTo("hunt-subject-0"));
            Assert.That(exclusion.SourceOutputName,
                Is.EqualTo(SyntheticTargetingCapabilities.ChosenMinionOutputName));

            Assert.That(
                SkillRoot(SwiftCardContent.MarkForDeath).Nodes
                    .Any(node => node.Inputs != null && node.Inputs.Any(input =>
                        input.ParameterName ==
                        SyntheticTargetingCapabilities.ExcludedMinionParameterName)),
                Is.False,
                "and Mark for Death declares no exclusion at all: there is no second pass to " +
                "exclude anybody from.");
        }

        /// <summary>
        /// THE THIRD CORRECTION THE 8.83 SWEEP FOUND ON HER, and the one nobody reported. Her three
        /// jump Skills print "Push ... UP TO 1 / 2 spaces" and their push declared
        /// <see cref="EffectFailurePolicy.May"/>, which <c>AdmittedDistances.ForDeclared</c> reads as
        /// the declared number and nothing less - the floor beside it is consulted under `up to`
        /// alone.
        ///
        /// <para>On Drop Trooper it was worse than a distance: <c>OfferTargets</c> sets its minimum to
        /// its maximum under any policy but `up to`, so "Push UP TO TWO enemy units adjacent to you"
        /// forced BOTH of them whenever two stood beside her.</para>
        /// </summary>
        [Test]
        public void HerThreeJumpSkills_PushAnOptionalDistance_AndDropTroopersCountIsOptionalToo()
        {
            foreach (var row in new[]
            {
                new { Card = SwiftCardContent.SteamJump, Distance = 1, Targets = 1 },
                new { Card = SwiftCardContent.AssaultJump, Distance = 2, Targets = 1 },
                new { Card = SwiftCardContent.DropTrooper, Distance = 2, Targets = 2 }
            })
            {
                var push = SkillRoot(row.Card).Nodes.Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.ObjectPushV3));

                Assert.That(push.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    row.Card + " prints \"up to " + row.Distance + " space(s)\", which is p.11's " +
                    "optional distance and not p.10's optional step.");

                var admitted = AdmittedDistances.ForDeclared(
                    push.FailurePolicy,
                    push.Inputs.Single(input => input.ParameterName == "distance")
                        .Literal.IntegerValue,
                    push.Inputs.Single(input => input.ParameterName ==
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName)
                        .Literal.IntegerValue);
                Assert.That(admitted.Enumerate(), Is.EqualTo(Enumerable.Range(0, row.Distance + 1)),
                    "so the pushed unit may travel none of the spaces, all of them, or any number " +
                    "between - measured through the production statement rather than restated.");
                Assert.That(
                    push.Inputs.Single(input => input.ParameterName ==
                            SyntheticDisplacementCapabilities.MaximumTargetsParameterName)
                        .Literal.IntegerValue,
                    Is.EqualTo(row.Targets),
                    row.Card + " names its own count, and under `up to` that count's floor is zero " +
                    "- which is what stops Drop Trooper forcing both units.");
            }
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
            foreach (var move in MoveNodes(cardId))
            {
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
        }

        // -------------------------------------------------------------------------------------------
        // The drive
        // -------------------------------------------------------------------------------------------

        private sealed class DriveResult
        {
            internal readonly List<MinionId> OfferedMinions = new List<MinionId>();
            internal readonly List<HexCoordinate> OfferedDestinations = new List<HexCoordinate>();

            /// <summary>One entry per minion selection the walk opened, in the order it opened them.</summary>
            internal readonly List<IReadOnlyList<MinionId>> MinionOffers =
                new List<IReadOnlyList<MinionId>>();

            /// <summary>One entry per minion selection the walk opened, holding the offered destinations.</summary>
            internal readonly List<IReadOnlyList<HexCoordinate>> DestinationOffers =
                new List<IReadOnlyList<HexCoordinate>>();

            internal readonly List<MinionId> ChosenMinions = new List<MinionId>();
            internal readonly List<HexCoordinate> SpacesBefore = new List<HexCoordinate>();
            internal readonly List<HexCoordinate> SpacesAfter = new List<HexCoordinate>();

            /// <summary>How many optional passes the walk stopped to ask about.</summary>
            internal int PassDecisions;

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
            HexCoordinate? subject = null,
            int passesTaken = int.MaxValue)
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

            for (var guard = 0; guard < 48; guard++)
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
                    result.MinionOffers.Add(
                        minions.Select(candidate => candidate.Value.MinionId).ToArray());
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
                    // The minion the case names where it is still on offer - a later pass of the
                    // same sentence has taken it out of its own disk, which is the whole point of
                    // `excludeMinion` - and otherwise the first one offered.
                    var minionInvocations = choice.SelectInvocations
                        .Where(invocation => invocation.Candidate.Value.Kind == EffectValueKind.Minion)
                        .ToArray();
                    var named = (subject.HasValue
                        ? minionInvocations.FirstOrDefault(invocation =>
                            board.Minions.Single(minion =>
                                    minion.MinionId.Equals(invocation.Candidate.Value.MinionId))
                                .Coordinate.Equals(subject.Value))
                        : null) ?? minionInvocations.First();
                    if (result.ChosenMinions.Count == 0)
                    {
                        result.ChosenMinion = named.Candidate.Value.MinionId;
                    }

                    result.ChosenMinions.Add(named.Candidate.Value.MinionId);
                    result.SpacesBefore.Add(board.Minions
                        .Single(minion => minion.MinionId.Equals(named.Candidate.Value.MinionId))
                        .Coordinate);
                    answer = named;
                }
                else if (spaces.Length > 0)
                {
                    result.DestinationOffers.Add(
                        spaces.Select(candidate => candidate.Value.Coordinate).ToArray());
                    foreach (var candidate in spaces)
                    {
                        result.OfferedDestinations.Add(candidate.Value.Coordinate);
                    }

                    // The named space where the case names one, and otherwise the first destination
                    // that is not the minion standing still - a stand-still answer would leave every
                    // "did it move" assertion true of a board nothing happened on.
                    var mover = result.ChosenMinions[result.ChosenMinions.Count - 1];
                    var wanted = destination.HasValue
                        ? choice.SelectInvocations.Single(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            invocation.Candidate.Value.Coordinate.Equals(destination.Value))
                        : choice.SelectInvocations.FirstOrDefault(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            !invocation.Candidate.Value.Coordinate.Equals(
                                board.Minions.Single(minion =>
                                    minion.MinionId.Equals(mover)).Coordinate));
                    Assert.That(wanted, Is.Not.Null,
                        "The move offered nowhere but the space the minion already stands on.");
                    answer = wanted;
                }
                else if (choice.ConfirmInvocation != null)
                {
                    // An optional pass of "Move up to two enemy minions": taking it is Confirm and
                    // walking away from it is Decline, and a case that names a count declines from
                    // that count on.
                    if (choice.DeclineInvocation != null)
                    {
                        result.PassDecisions++;
                        answer = result.PassDecisions > passesTaken
                            ? choice.DeclineInvocation
                            : choice.ConfirmInvocation;
                    }
                    else
                    {
                        answer = choice.ConfirmInvocation;
                    }
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

            result.SpaceOfTheMovedMinion = result.ChosenMinions.Count == 0
                ? default
                : board.Minions
                    .Single(minion => minion.MinionId.Equals(result.ChosenMinion))
                    .Coordinate;
            result.SpacesAfter.AddRange(result.ChosenMinions.Select(id =>
                board.Minions.Single(minion => minion.MinionId.Equals(id)).Coordinate));
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

        /// <summary>
        /// Every direct Move one of her marking faces composes - ONE on Mark for Death and TWO on
        /// Hunting Season since milestone 8.83, which is why this is a list and was a `Single`.
        /// </summary>
        private static IReadOnlyList<EffectNodeDefinition> MoveNodes(CardDefinitionId cardId) =>
            SwiftCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot.Nodes
                .Where(node => node.Capability.Equals(
                    SyntheticDisplacementCapabilities.DirectMoveV3))
                .ToArray();

        private static EffectDefinition SkillRoot(CardDefinitionId cardId) =>
            SwiftCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot;

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
