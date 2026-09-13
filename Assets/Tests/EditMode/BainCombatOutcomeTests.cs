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
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40j. The ENCLOSING COMBAT OUTCOME milestone 8.40i could not reach, driven on the
    /// configured match this dealing composes.
    ///
    /// <para><b>Why it could not be reached before, stated rather than implied.</b> 8.40i's acceptance
    /// check asked its Attack faces to cover "the enclosing combat outcome arms", and its completion
    /// correction recorded that they do not: a complete rulebook p.14 for one of his Attacks needs a
    /// configured match HOLDING his cards - a commitment, a turn, an offered action, a target, the
    /// defender's window and a resolved outcome - and a composition milestone has no such match. This
    /// milestone composes one, so the check lands here.</para>
    ///
    /// <para><b>What is driven.</b> His three plain crossbows - Light Crossbow, Heavy Crossbow and
    /// Arbalest, which is every face carrying milestone 8.40d's unit-and-terrain clear line - and one
    /// bullet of Hunter-Seeker, his one-or-both modal. Each runs the whole page: the action is taken
    /// from the offers a real turn publishes, the target is chosen from the choice the resolver
    /// opens, the defender answers p.14's step 3 window, and the outcome is read off the resolved
    /// attack rather than off the effect frame. Milestone 8.40k drives both bullets of Hunter-Seeker
    /// and the rest of his set through real controls; what this owes is the page, not the pointer.
    /// </para>
    ///
    /// <para><b>The board is the staged one his scenes compose, with ONE deliberate move, and the
    /// reason is the measurement in <see cref="CreateAdjacentBainSession"/>'s own remarks.</b> The
    /// engaged staging leaves Bain and the enemy hero two spaces apart around the engaged minion, and
    /// on that position every Attack face of his offers MINIONS ONLY - a page against a minion never
    /// opens p.14's step 3 window and never records a step 6 outcome, so it could not answer this
    /// check however many times it ran. He is moved to a free playable neighbour of the enemy hero,
    /// computed from the board rather than written down, and everything else is the staging's.</para>
    /// </summary>
    public sealed class BainCombatOutcomeTests
    {
        [Test]
        public void HisThreePlainCrossbows_RunACompletePageFourteenAndResolveAnOutcome()
        {
            // Light Crossbow is his printed Tier I and sits in rulebook p.6's starting hand; Heavy
            // Crossbow is the first Tier II sibling and Arbalest its Tier III successor, so each
            // needs the staged progression that actually deals it.
            foreach (var (card, tier) in new[]
            {
                (BainCardContent.LightCrossbow, 1),
                (BainCardContent.HeavyCrossbow, 2),
                (BainCardContent.Arbalest, 3)
            })
            {
                var session = CreateAdjacentBainSession(
                    tier, BainUpgradeBranch.FirstSibling, out var bainId, out var enemyId);
                var outcome = RunOnePageFourteen(session, bainId, enemyId, card, null);

                Assert.That(outcome, Is.Not.EqualTo(AttackResolutionOutcome.None),
                    card.Value + " must resolve p.14 to a declared outcome rather than stopping " +
                    "inside its own targeting declaration.");
                Assert.That(session.Projection.PendingAttackResolution, Is.Null,
                    card.Value + "'s attack must be finished, not still standing.");
                Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null);
            }
        }

        [Test]
        public void HunterSeekersAdjacentBullet_RunsACompletePageFourteenAndResolvesAnOutcome()
        {
            var session = CreateAdjacentBainSession(
                3, BainUpgradeBranch.SecondSibling, out var bainId, out var enemyId);
            var outcome = RunOnePageFourteen(
                session, bainId, enemyId, BainCardContent.HunterSeeker, "crossbow-adjacent-bullet");

            Assert.That(outcome, Is.Not.EqualTo(AttackResolutionOutcome.None),
                "A chosen bullet of \"Choose one, or both\" is a complete p.14 order of its own - " +
                "BA-D4 - so taking one and stopping still has to reach an outcome.");
            Assert.That(session.Projection.PendingAttackResolution, Is.Null,
                "and the modal does not leave an attack standing once the chosen bullets are done.");
        }

        /// <summary>
        /// Commits the named card, takes its printed Attack out of the turn's own offers, answers
        /// whatever the resolver asks - the modal's bullets, the target, the defender's window - and
        /// returns the outcome p.14 step 6 recorded.
        /// </summary>
        private static AttackResolutionOutcome RunOnePageFourteen(
            MatchSession session,
            HeroId bainId,
            HeroId enemyId,
            CardDefinitionId definitionId,
            string bulletNodeId)
        {
            var card = session.Projection.Cards.Single(value =>
                value.OwnerId.Equals(bainId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            foreach (var seat in session.Projection.Setup.Heroes.Select(hero => hero.HeroId))
            {
                var committed = seat.Equals(bainId)
                    ? card
                    : session.Projection.Cards
                        .Where(value => value.OwnerId.Equals(seat) &&
                            value.State.Location == CardLocation.Hand)
                        .OrderBy(value => session.Projection.Definitions
                            .Single(definition =>
                                definition.Reference.Equals(value.DefinitionReference))
                            .Initiative.Value)
                        .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                        .First();
                Accept(session, DirectorCommand.CommitCard(seat, committed.Id));
            }

            // p.7 resolves the higher printed Initiative first, and the other seat yields its lowest
            // card, so the turn opening on him is a fact this fixture can assert rather than hope for.
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(bainId),
                definitionId.Value + " must open the turn for this page to be his.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            for (var step = 0; step < 16; step++)
            {
                var choice = session.Projection.PendingEffectChoice;
                if (choice != null)
                {
                    // The modal's bullets, when the face prints one. Exactly ONE is taken - the one
                    // the caller names - which is what makes the case above about a single complete
                    // order rather than about whichever arm happened to come first.
                    var select = choice.SelectInvocations.FirstOrDefault(invocation =>
                        bulletNodeId == null ||
                        invocation.Candidate.OptionNodeId.Value == bulletNodeId);
                    if (select != null && choice.SelectedCandidates.Count == 0)
                    {
                        Accept(session, DirectorCommand.AnswerEffectChoice(select));
                        continue;
                    }

                    if (choice.ConfirmInvocation != null &&
                        choice.SelectedCandidates.Count >= choice.MinimumSelections)
                    {
                        Accept(session, DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation));
                        continue;
                    }

                    if (choice.DeclineInvocation != null)
                    {
                        Accept(session, DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation));
                        continue;
                    }
                }

                var targets = session.Projection.PendingAttackTargetChoice;
                if (targets != null)
                {
                    Assert.That(targets.LegalTargetHeroIds, Has.Member(enemyId),
                        definitionId.Value + " must be able to name the enemy HERO, because a page " +
                        "against a minion never opens p.14's step 3 window and never records a " +
                        "step 6 outcome. Minions offered: " +
                        string.Join(", ", targets.LegalTargetMinionIds.Select(value => value.Value)));
                    Accept(session, DirectorCommand.ChooseAttackTarget(
                        targets.ResolutionId, targets.ChoiceId, enemyId));
                    continue;
                }

                var defense = session.Projection.PendingDefenseChoice;
                if (defense != null)
                {
                    Assert.That(defense.CanDecline, Is.True,
                        "p.14 step 3 lets a defender answer with nothing, which is how this page " +
                        "reaches step 6 without a second card's text in the way.");
                    Accept(session, DirectorCommand.DeclineDefense(
                        defense.ResolutionId, defense.ChoiceId));
                    continue;
                }

                break;
            }

            // Step 6 and step 9 are read off the rule-event log rather than off the effect frame,
            // because what this case is about is the ATTACK finishing: `AttackSuccessful` and
            // `AttackSuccessfullyDefended` are the only two kinds `AttackOutcomeDetermined` can mint
            // (it refuses any other), and `AttackResolutionCompleted` is p.14's own end of the page.
            var log = session.Projection.DiagnosticEvents.Select(entry => entry.EventType).ToArray();
            Assert.That(log, Has.Member("Action.AttackResolutionCompleted"),
                definitionId.Value + " never reached p.14's end. Log: " + string.Join(", ", log));
            var determined = log.Count(value =>
                value == "Action.AttackSuccessful" ||
                value == "Action.AttackSuccessfullyDefended");
            Assert.That(determined, Is.GreaterThanOrEqualTo(1),
                definitionId.Value + " reached no step-6 outcome. Log: " + string.Join(", ", log));
            return log.Contains("Action.AttackSuccessful")
                ? AttackResolutionOutcome.AttackSuccessful
                : AttackResolutionOutcome.SuccessfullyDefended;
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// The staged Wasp-versus-Bain board with Bain standing beside the enemy hero, which is the
        /// one position every case here needs and the one the scene staging deliberately does not
        /// produce.
        ///
        /// <para><b>Measured rather than assumed, and worth writing down for milestone 8.40k.</b> The
        /// engaged staging leaves Bain at (-3,1) and the enemy Wasp at (-2,2), two spaces apart
        /// around the engaged Blue melee minion at (-3,2) - and on that board his three plain
        /// crossbows offer MINIONS ONLY: `red-melee-01` and `red-ranged-01`, never the hero, because
        /// the two-space gap between the heroes is bent rather than straight and his clause prints
        /// "in a straight line". A page against a minion removes it at step 2 and never opens step
        /// 3's Defense window, so it can never record a step 6 outcome. Moving him to a free playable
        /// neighbour of the enemy is the smallest change that gives every one of his Attack faces a
        /// hero to shoot: adjacent is in range for a Range 2, 3 or 4 icon and trivially a straight
        /// line with nothing between.</para>
        /// </summary>
        private static MatchSession CreateAdjacentBainSession(
            int tier, BainUpgradeBranch branch, out HeroId bainId, out HeroId enemyId)
        {
            var configuration =
                tier == 1 ? MatchConfiguration.CreateStagedBainForInternalTest(TeamSide.Red) :
                tier == 2 ? MatchConfiguration.CreateStagedBainTierTwoForInternalTest(
                    TeamSide.Red, branch) :
                MatchConfiguration.CreateStagedBainTierThreeForInternalTest(TeamSide.Red, branch);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusBainOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState),
                out _);

            var bain = setup.Heroes
                .Single(hero => hero.ContentId.Value == BainCardContent.ContentSetId);
            var enemy = setup.Heroes.Single(hero => !hero.HeroId.Equals(bain.HeroId));
            bainId = bain.HeroId;
            enemyId = enemy.HeroId;

            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate))
            {
                enemy.Coordinate
            };
            var beside = enemy.Coordinate.GetNeighbors()
                .Where(coordinate =>
                    !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Coordinate.Equals(coordinate) &&
                        space.Kind == BoardMapSpaceKind.Playable))
                .OrderBy(coordinate => coordinate.Q).ThenBy(coordinate => coordinate.R)
                .ToArray();
            Assert.That(beside, Is.Not.Empty,
                "The staged enemy hero must have a free playable neighbour for Bain to stand on.");
            var heroes = setup.Heroes
                .Select(hero => hero.HeroId.Equals(bain.HeroId)
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint,
                        new ConfiguredSpaceReference(
                            map.Id,
                            map.Spaces.Single(space => space.Coordinate.Equals(beside[0])).Id),
                        beside[0])
                    : hero)
                .ToArray();
            setup = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes,
                setup.Minions);

            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
