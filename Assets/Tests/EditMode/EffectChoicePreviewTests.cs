using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5c. The authority-computed reading of a pending effect choice: which board object
    /// each space-valued candidate names, and where the operation waiting on that choice would leave
    /// it. Presentation may not derive either, so both have to arrive in the projection.
    ///
    /// It runs on the same official-map geography milestone 6.5a proved the Skill on, because the two
    /// interesting Push outcomes are free there: from `q-2_r-5` the Blue hero at `q-1_r-5` is pushed
    /// exactly one space, since `q1_r-5` is a declared terrain cell; and from `q-1_r-5` a Blue hero at
    /// `q0_r-5` cannot move at all, which is p.18's zero-space push - a legal result, not a refusal.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class EffectChoicePreviewTests
    {
        private static readonly CardInstanceId NeedleCardId = new CardInstanceId("red-needle-card");

        private static readonly ActionDefinitionId NeedleSkill =
            new ActionDefinitionId("primary-push-place-skill");

        private static readonly EffectNodeId PushBranch = new EffectNodeId("needle-push-branch");
        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("needle-place-branch");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate BeyondBlue = new HexCoordinate(0, -5);

        [Test]
        public void NoPendingEffectChoice_PreviewsNothing()
        {
            var session = CreateNeedleSession(RedAt, BlueAt);

            Assert.That(session.Projection.PendingEffectChoicePreview, Is.Empty,
                "A preview exists only for a decision that is actually waiting.");
        }

        [Test]
        public void AModeChoice_PreviewsNothing()
        {
            var session = StartNeedleSkill();

            Assert.That(session.Projection.PendingEffectChoice.Candidates
                    .All(candidate => candidate.Kind == EffectChoiceCandidateKind.Option),
                Is.True);
            Assert.That(session.Projection.PendingEffectChoicePreview, Is.Empty,
                "An option names a mode, not a space, so there is nothing on the board to read.");
        }

        [Test]
        public void APushTargetChoice_NamesEachTargetAndWhereThePushWouldLeaveIt()
        {
            var session = StartNeedleSkill();
            ChooseBranch(session, PushBranch);

            var choice = session.Projection.PendingEffectChoice;
            var preview = session.Projection.PendingEffectChoicePreview;

            Assert.That(preview.Select(value => value.Coordinate),
                Is.EqualTo(choice.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Every candidate is described, in the projection's own candidate order.");
            Assert.That(preview.All(value => value.NamesObject), Is.True,
                "Every target of `choose another adjacent unit` stands on the space it names.");

            var blue = preview.Single(value => value.Coordinate.Equals(BlueAt));
            Assert.That(blue.Occupant.Kind, Is.EqualTo(BoardObjectKind.Hero));
            Assert.That(blue.Occupant.HeroId, Is.EqualTo(BlueHero(session)));
            Assert.That(blue.ResultingCoordinate, Is.EqualTo(BeyondBlue),
                "A push of 3 travels exactly one space here, because the space beyond is terrain.");
        }

        [Test]
        public void APushThatCannotMoveItsTarget_PreviewsTheTargetsOwnSpace()
        {
            // p.18's zero-space push. Saying nothing here would read as "this target cannot be
            // chosen", which is the opposite of what the rule allows.
            var session = StartNeedleSkill(BlueAt, BeyondBlue);
            ChooseBranch(session, PushBranch);

            var blue = session.Projection.PendingEffectChoicePreview
                .Single(value => value.Coordinate.Equals(BeyondBlue));

            Assert.That(blue.Occupant.HeroId, Is.EqualTo(BlueHero(session)));
            Assert.That(blue.ResultingCoordinate, Is.EqualTo(BeyondBlue));
        }

        [Test]
        public void APlaceDestinationChoice_NamesNoObjectAndPredictsNothing()
        {
            var session = StartNeedleSkill();
            ChooseBranch(session, PlaceBranch);

            var preview = session.Projection.PendingEffectChoicePreview;

            Assert.That(preview, Is.Not.Empty);
            Assert.That(preview.Any(value => value.NamesObject), Is.False,
                "`a different empty space` offers only empty spaces.");
            Assert.That(preview.All(value => value.ResultingCoordinate == null), Is.True,
                "Nothing consumes this choice with an operation that relocates what stands there.");
            Assert.That(preview.Any(value => value.Coordinate.Equals(RedAt)), Is.False,
                "The actor's own space is not among them, so nothing is previewed for it.");
        }

        [Test]
        public void ThePreviewIsRebuiltFromCurrentStateRatherThanRemembered()
        {
            var session = StartNeedleSkill();
            ChooseBranch(session, PushBranch);
            var before = session.Projection.PendingEffectChoicePreview
                .Single(value => value.Coordinate.Equals(BlueAt));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PendingEffectChoicePreview, Is.Empty,
                "Undo returns to the mode choice, whose candidates name no space.");

            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation)).Accepted, Is.True);
            var after = session.Projection.PendingEffectChoicePreview
                .Single(value => value.Coordinate.Equals(BlueAt));

            Assert.That(after.ResultingCoordinate, Is.EqualTo(before.ResultingCoordinate),
                "Re-derived from the same board, the same answer comes back.");
        }

        [Test]
        public void ThePreviewDescribesNothingWhileACorrectionIsPending()
        {
            // A correction pauses after the Skill's own decisions are finished, so no effect choice
            // stands and the preview has nothing to describe. The two prompts never overlap.
            var session = StartNeedleSkill(new HexCoordinate(0, -1), new HexCoordinate(-3, 10));
            ChooseBranch(session, PushBranch);
            // The corpus fixture's correction geography: red-melee-01 stands adjacent at (-1,-1),
            // where a push directly away carries it out of the Battle Zone.
            var minionSpace = session.Projection.PendingEffectChoicePreview
                .Single(value => value.Occupant.Kind == BoardObjectKind.Minion &&
                    value.Occupant.MinionId.Equals(new MinionId("red-melee-01")))
                .Coordinate;
            SelectValue(session, minionSpace);

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.PendingEffectChoicePreview, Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.11c: a candidate that names its object by identity rather than by coordinate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Reflect Projectiles' rider names the attacker by `HeroId`. Until milestone 8.11c the
        /// preview covered coordinate-valued candidates only, so such a candidate meant nothing on the
        /// board and could only be answered from the prompt. It is previewed on the space occupancy
        /// currently puts that hero on, which is what lets the board mark the piece and take the click.
        /// </summary>
        [Test]
        public void AHeroNamedByIdentity_IsPreviewedWhereOccupancyPutsItAndPredictsNothing()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            ConfiguredDefenseCardTests.BlockTheRangedAttack(
                session, WaspCardContent.ReflectProjectiles);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "The rider asks its question, or this asserts nothing.");
            var heroCandidates = choice.Candidates
                .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Hero)
                .ToArray();
            Assert.That(heroCandidates, Is.Not.Empty,
                "and it names a hero by identity, which is the shape this case is about.");

            foreach (var candidate in heroCandidates)
            {
                var occupancy = session.Projection.Heroes
                    .Single(value => value.HeroId.Equals(candidate.Value.HeroId));
                var preview = session.Projection.PendingEffectChoicePreview
                    .Single(value => value.CandidateValue.Equals(candidate.Value));

                Assert.That(preview.Coordinate, Is.EqualTo(occupancy.Coordinate),
                    "The space is read from authoritative occupancy, not carried by the candidate.");
                Assert.That(preview.NamesObject, Is.True);
                Assert.That(preview.Occupant.Kind, Is.EqualTo(BoardObjectKind.Hero));
                Assert.That(preview.Occupant.HeroId, Is.EqualTo(candidate.Value.HeroId),
                    "and the piece standing there is the hero the candidate names.");
                Assert.That(preview.ResultingCoordinate, Is.Null,
                    "A discard moves nobody, so there is no resulting space to predict - the " +
                    "resulting reading belongs to displacement alone.");
            }
        }

        /// <summary>
        /// The totality claim, as a failure rather than as a comment. `TryGetObjectAt` answers with at
        /// most one object per space, so two candidates on one space would be two answers to one
        /// click. Collapsing them into a single mark would silently drop an offer the authority made,
        /// so the query refuses instead.
        /// </summary>
        [Test]
        public void TwoCandidatesResolvingToOneSpace_FailLoudlyRatherThanCollapsing()
        {
            var actor = new HeroId("preview-actor");
            var other = new HeroId("preview-other");
            var otherAt = new HexCoordinate(1, 0);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("preview-invariant"),
                1,
                Enumerable.Range(-2, 9).Select(q => new HexCoordinate(q, 0)).ToArray());
            var board = new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(actor, new HexCoordinate(0, 0)), new HeroOccupancy(other, otherAt) },
                Array.Empty<ObstacleOccupancy>());

            // The same piece named twice: once by the space it stands on, once by its own identity.
            var doubled = new PendingEffectChoice(
                new EffectResolutionId("preview-resolution"),
                new EffectChoiceId("preview-choice"),
                new EffectNodeId("preview-node"),
                actor,
                EffectChoiceKind.SelectTarget,
                new[]
                {
                    EffectChoiceCandidate.Value_(EffectValue.Space(otherAt)),
                    EffectChoiceCandidate.Value_(EffectValue.Hero(other))
                },
                1,
                1);

            // A definition with nothing that consumes the choice: the invariant is about the candidates
            // alone, so there is deliberately no Push here to preview. It is built outside the assert
            // so that a construction fault reads as one rather than as the throw under test.
            var definition = new EffectDefinition(
                new EffectDefinitionId("preview-definition"),
                "preview-rev-1",
                new EffectNodeId("preview-node"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("preview-node"),
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities
                            .ChooseTargetV3)
                });
            var components = new MatchComponents(
                GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions.CreateCatalog(),
                null,
                new MarkerAttachmentRegister(),
                board,
                null,
                null);

            Assert.That(
                () => EffectChoicePreviewQuery.Describe(
                    definition,
                    doubled,
                    actor,
                    components,
                    GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty),
                Throws.InvalidOperationException.With.Message.Contains(otherAt.ToString()),
                "Two candidates on one space is a violated invariant, and it says which space.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers - the same fixture shape milestone 6.5a proved the Skill with.
        // -------------------------------------------------------------------------------------------

        private static HeroId BlueHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        private static MatchSession StartNeedleSkill(
            HexCoordinate? redAt = null, HexCoordinate? blueAt = null)
        {
            var session = CreateNeedleSession(redAt ?? RedAt, blueAt ?? BlueAt);
            CommitCards(session, "needle", "longbow");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(NeedleCardId));
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(NeedleSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static void CommitCards(
            MatchSession session, string redCardFragment, string blueCardFragment)
        {
            foreach (var pair in new[]
            {
                new { Side = TeamSide.Red, Fragment = redCardFragment },
                new { Side = TeamSide.Blue, Fragment = blueCardFragment }
            })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == pair.Side &&
                    candidate.Id.Value.Contains(pair.Fragment) &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }
        }

        private static void ChooseBranch(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(branch))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.Value.Coordinate.Equals(coordinate))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateNeedleSession(HexCoordinate redAt, HexCoordinate blueAt)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redAt),
                    new HeroOccupancy(blueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
