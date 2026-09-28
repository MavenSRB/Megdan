using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114g. The staged Wuk match his Play Mode journeys load, composed the way
    /// <c>MatchSceneCompositionRoot</c> composes it (staged 1v1 board, engaged restaging, then the
    /// Director sandbox staging), and driven with real <see cref="DirectorCommand"/>s.
    /// </summary>
    internal sealed class WukStagedSession
    {
        public static readonly MinionId StagedMinion = new MinionId("wuk-journey-minion");

        private WukStagedSession(MatchSession session, HeroId wuk, HeroId wasp, HeroId arien)
        {
            Session = session;
            Wuk = wuk;
            Wasp = wasp;
            Arien = arien;
        }

        public MatchSession Session { get; }
        public HeroId Wuk { get; }
        public HeroId Arien { get; }
        public HeroId Wasp { get; }
        public MatchSessionProjection Projection => Session.Projection;

        /// <summary>
        /// Wuk on <paramref name="wukAt"/> holding <paramref name="hand"/>, Wasp on
        /// <paramref name="waspAt"/>, optionally one of Wuk's own melee minions, and - on the ally
        /// board - Arien on <paramref name="arienAt"/> holding one card so the rest of his set is in
        /// his discard.
        /// </summary>
        public static WukStagedSession Create(
            IReadOnlyList<CardDefinitionId> hand, HexCoordinate wukAt, HexCoordinate waspAt,
            HexCoordinate? wuksMinionAt = null, HexCoordinate? arienAt = null)
        {
            var withAlly = arienAt.HasValue;
            var configuration = withAlly
                ? MatchConfiguration.CreateStagedWukWithAllyForInternalTest(TeamSide.Red)
                : MatchConfiguration.CreateStagedWukForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                withAlly
                    ? QuickGameSetupConfiguration.CreateStagedWaspVersusWukWithAllyOneVsTwo()
                    : QuickGameSetupConfiguration.CreateStagedWaspVersusWukOneVsOne(),
                configuration.Participants, map, configuration.TieBreakerState);
            if (!withAlly) setup = EngagedMirrorStaging.Restage(map, setup, out _);
            var wuk = setup.Heroes.Single(hero => hero.ContentId.Value == "wuk");
            var wasp = setup.Heroes.Single(hero => hero.ContentId.Value == "wasp");
            var occupied = new List<HexCoordinate> { wukAt, waspAt };
            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(wuk.HeroId, wukAt, hand: hand),
                new SandboxHeroStaging(wasp.HeroId, waspAt, hand: new[]
                {
                    WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                    WaspCardContent.Shock, WaspCardContent.LiftUp
                })
            };
            var arien = default(HeroId);
            if (withAlly)
            {
                arien = setup.Heroes.Single(hero => hero.ContentId.Value == "arien").HeroId;
                heroes.Add(new SandboxHeroStaging(arien, arienAt.Value,
                    hand: new[] { ArienCardContent.DealtDefinitions.First().Id }));
                occupied.Add(arienAt.Value);
            }

            if (wuksMinionAt.HasValue) occupied.Add(wuksMinionAt.Value);
            var description = new SandboxStagingDescription(
                heroes: heroes,
                spawnedMinions: wuksMinionAt.HasValue
                    ? new[]
                    {
                        new SandboxMinionStaging(StagedMinion, wuk.TeamSide,
                            StandardMinionType.Melee, wuksMinionAt.Value)
                    }
                    : null,
                removedMinions: setup.Minions.Where(minion => occupied.Contains(minion.Coordinate))
                    .Select(minion => minion.MinionId));
            var state = SandboxMatchComposition.Compose(
                description, map, setup, configuration, new[] { map });
            return new WukStagedSession(new MatchSession(state), wuk.HeroId, wasp.HeroId, arien);
        }

        public void Accept(DirectorCommand command) => ConfiguredMatchFixtures.Accept(Session, command);

        public CardInstance Card(HeroId owner, CardDefinitionId face) =>
            Projection.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        /// <summary>Commits <paramref name="face"/> for <paramref name="actor"/> and the lowest-Initiative card for the other.</summary>
        public void CommitSo(HeroId actor, CardDefinitionId face)
        {
            foreach (var hero in Projection.Participants.Heroes.Select(value => value.Id).ToArray())
            {
                var card = hero.Equals(actor)
                    ? Card(hero, face)
                    : Projection.Cards.Where(value => value.OwnerId.Equals(hero) &&
                            value.State.Location == CardLocation.Hand)
                        .OrderBy(value => Projection.Definitions.Single(definition =>
                            definition.Reference.Equals(value.DefinitionReference)).Initiative.Value)
                        .FirstOrDefault();

                // A seat with nothing left in hand passes (p.7), as Arien does after his one card.
                Accept(card == null ? DirectorCommand.Pass(hero) : DirectorCommand.CommitCard(hero, card.Id));
            }
        }

        /// <summary>Ends other seats' turns until <paramref name="hero"/> is the acting seat.</summary>
        public void UntilActs(HeroId hero)
        {
            for (var step = 0; step < 12; step++)
            {
                var projection = Projection;
                if (projection.PendingInitiativeChoice != null)
                {
                    Accept(DirectorCommand.ChooseTiedActor(projection.PendingInitiativeChoice.OwningSide,
                        projection.PendingInitiativeChoice.CandidateCardInstanceIds[0]));
                    continue;
                }

                if (projection.PendingTurnRelease == null && projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(hero))
                {
                    return;
                }

                EndTurn();
            }

            Assert.Fail(hero.Value + " never became the acting seat.");
        }

        /// <summary>Ends the current turn the way the presenter's END TURN does.</summary>
        public void EndTurn()
        {
            var projection = Projection;
            if (projection.PendingTurnRelease != null)
            {
                Accept(DirectorCommand.ReleaseTurn());
                return;
            }

            var movement = projection.PendingMovementChoice;
            if (movement != null && movement.CanStop)
            {
                Accept(DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId));
                return;
            }

            Accept(DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
        }

        /// <summary>Ends turns until p.7 asks for the next commitments.</summary>
        public void UntilCommitments()
        {
            for (var step = 0; step < 12 &&
                Projection.Phase != RoundResolutionPhase.AwaitingCommitments; step++)
            {
                EndTurn();
            }

            Assert.That(Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
        }

        /// <summary>Starts the acting card's action of <paramref name="kind"/> and printed <paramref name="origin"/>.</summary>
        public void Start(ActionKind kind, ActionDefinitionOrigin origin)
        {
            var offer = Projection.AvailableTurnActions.Single(candidate =>
            {
                var action = Projection.Definitions
                    .Single(definition => definition.Reference.Equals(candidate.SourceDefinitionReference))
                    .ActionDefinitions.Single(value => value.Id.Equals(candidate.ActionDefinitionId));
                return candidate.IsAvailable && action.Kind == kind && action.Origin == origin;
            });
            Accept(DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        public void TargetHero(HeroId hero)
        {
            var choice = Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null);
            Accept(DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, hero));
        }

        public void TargetMinion(MinionId minion)
        {
            var choice = Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null);
            Accept(DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, minion));
        }

        public void Select(Func<EffectValue, bool> which)
        {
            var choice = Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Accept(DirectorCommand.AnswerEffectChoice(
                choice.SelectInvocations.First(value => which(value.Candidate.Value))));
            var after = Projection.PendingEffectChoice;
            if (after != null && after.NodeId.Equals(choice.NodeId) && after.ConfirmInvocation != null &&
                after.SelectedCandidates.Count >= after.MaximumSelections)
            {
                Accept(DirectorCommand.AnswerEffectChoice(after.ConfirmInvocation));
            }
        }

        public HexCoordinate[] Trees() => Projection.PlacedTokens
            .Where(token => token.Definition.Id.Equals(WukComponentContent.TreeId))
            .Select(token => token.Coordinate).ToArray();
    }
}
