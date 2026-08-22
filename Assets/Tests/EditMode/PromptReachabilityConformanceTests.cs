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
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.11a, at the user's request, and written because one of these fell through.
    ///
    /// <para>Milestone 8.11 shipped rulebook p.14's Block. Every case it wrote drove the authority
    /// directly - offer the Block, choose it, check what happened - and every one passed. None of them
    /// ever built the prompt a defender is actually shown. That prompt looked for the offer's printed
    /// Defense icon, a Block has none, and it threw where it was drawn: the authority asked the
    /// question, the screen asked nothing, and the match stopped. It took playing the game to find it.
    /// </para>
    ///
    /// <para>The gap was not the Block. The gap was that nothing checked <b>the authority's offers
    /// against the surfaces a player answers them on</b>. These cases check exactly that, from the
    /// authority's own legal sets, so a future choice that presentation cannot draw fails here rather
    /// than in play. They deliberately assert reachability rather than appearance: what a control says
    /// is a judgement, but whether an offered answer can be given at all is not.</para>
    /// </summary>
    public sealed class PromptReachabilityConformanceTests
    {
        /// <summary>
        /// The case that fell through, generalised: every Defense the authority offers must produce
        /// exactly one control, whatever kind of Defense it is. Wasp's hand holds both kinds at once -
        /// a numeric secondary and a printed Block - so this covers the shape that worked and the shape
        /// that did not from a single pending choice.
        /// </summary>
        [Test]
        public void EveryLegalDefenseOffer_ProducesExactlyOneControlIncludingABlock()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.ThunderBoomerang, WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session, defender);

            var choice = session.Projection.PendingDefenseChoice;
            Assert.That(choice, Is.Not.Null, "The scenario reaches p.14 step 3, or this asserts nothing.");
            Assert.That(choice.LegalDefenseOffers, Is.Not.Empty);
            Assert.That(
                choice.LegalDefenseOffers.Any(offer => IsBlock(session, offer)), Is.True,
                "and one of them is a Block, which is the shape that was unreachable.");
            Assert.That(
                choice.LegalDefenseOffers.Any(offer => !IsBlock(session, offer)), Is.True,
                "and one is an ordinary numeric Defense, so both shapes are covered at once.");

            RoundActionViewModel model = null;
            Assert.That(
                () => model = RoundActionPresenter.CreateViewModel(session.Projection),
                Throws.Nothing,
                "Building the prompt must not throw. It threw here, and a thrown prompt draws nothing.");

            Assert.That(model.DefenseChoices, Has.Count.EqualTo(
                    choice.LegalDefenseOffers.Count + (choice.CanDecline ? 1 : 0)),
                "One control per offer the authority allows, plus declining when it is allowed.");
            Assert.That(
                model.DefenseChoices.All(value => !string.IsNullOrWhiteSpace(value.Label)), Is.True,
                "and each says something, because a blank control is an answer nobody can find.");
            Assert.That(
                model.DefenseChoices.Select(value => value.Label).Distinct().Count(),
                Is.EqualTo(model.DefenseChoices.Count),
                "and says something different, because two identical controls are one answer lost.");
        }

        /// <summary>
        /// The general form of the same rule, and the one that matters most now that targeting moved
        /// onto the board: <b>every candidate the authority offers must be answerable somewhere</b> -
        /// by clicking a marked piece, by clicking a lit space, or by a control in the prompt.
        ///
        /// <para>Milestone 8.11a left this indifferent about which, because a candidate naming a hero
        /// by identity rather than by coordinate still carried a control: the board is addressed by
        /// space, and such a candidate names none. Milestone 8.11c closes exactly that, so the rule
        /// tightens for the candidates it closed - a candidate that names a board object is answered
        /// <b>on the board</b>, and its prompt control is gone. Everything else keeps the weaker rule,
        /// because a mode, a Spawn drawn from the supply, and a confirm still have no board surface at
        /// all.</para>
        ///
        /// <para>Which candidates name an object is read here from authoritative occupancy directly,
        /// never from the preview the production path builds. That is deliberate: reading it from the
        /// preview would make the case agree with whatever the preview happens to cover, which is the
        /// very thing 8.11c changes.</para>
        /// </summary>
        [Test]
        public void EveryLegalEffectChoiceCandidateNamingAnObject_IsAnsweredOnTheBoard()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            var view = new CapturingBoardInteractionView();
            var board = new BoardInteractionPresenter(session, view);
            ConfiguredDefenseCardTests.BlockTheRangedAttack(
                session, WaspCardContent.ReflectProjectiles);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "The rider asks a question, or this asserts nothing.");
            Assert.That(choice.SelectInvocations, Is.Not.Empty);
            Assert.That(
                choice.SelectInvocations.Any(value =>
                    value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Candidate.Value.Kind == EffectValueKind.Hero),
                Is.True,
                "Reflect Projectiles' rider names the attacker by HeroId, which is the shape milestone " +
                "8.11c closes. Without one offered here this case asserts nothing about it.");

            board.Refresh();
            var model = view.Model;
            var prompt = RoundActionPresenter.CreateViewModel(session.Projection);

            var onTheBoard = new HashSet<HexCoordinate>(model.EffectChoiceDestinations);
            onTheBoard.UnionWith(model.EffectChoiceTargetSpaces);
            Assert.That(
                model.EffectChoiceDestinations.Intersect(model.EffectChoiceTargetSpaces), Is.Empty,
                "No space is both lit and marked - the board says the two in different vocabularies.");

            var promptControls = new HashSet<DirectorCommand>(
                prompt.EffectChoices.Select(value => value.Command));
            var answeredByAControl = new HashSet<DirectorCommand>(promptControls
                .Concat(prompt.EffectChoiceTargets.Select(value => value.Command))
                .Concat(prompt.EffectChoiceDestinations.Select(value => value.Command)));

            foreach (var invocation in choice.SelectInvocations)
            {
                var candidate = invocation.Candidate;
                var command = DirectorCommand.AnswerEffectChoice(invocation);
                var occupied = OccupiedSpaceOf(session.Projection, candidate);
                if (occupied.HasValue)
                {
                    Assert.That(onTheBoard, Contains.Item(occupied.Value),
                        $"The candidate on option node '{candidate.OptionNodeId.Value}' names the piece " +
                        $"standing on {occupied.Value}, so that piece is marked and a click on it " +
                        "answers the choice.");
                    Assert.That(promptControls, Has.No.Member(command),
                        $"and the candidate on option node '{candidate.OptionNodeId.Value}' keeps no " +
                        "prompt control, because two ways to say one thing is one of them redundant.");
                    continue;
                }

                var namesASpace =
                    candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Coordinate &&
                    onTheBoard.Contains(candidate.Value.Coordinate);
                Assert.That(namesASpace || answeredByAControl.Contains(command), Is.True,
                    $"The candidate on option node '{candidate.OptionNodeId.Value}' can be " +
                    "answered on the board or from the prompt. On neither, it is a question with no " +
                    "way to answer it.");
            }
        }

        /// <summary>
        /// The space a candidate's own value puts a board object on, or null when it names none.
        ///
        /// Read straight from projected occupancy, by value kind, so this case decides for itself what
        /// counts as naming an object rather than inheriting the production path's answer.
        /// </summary>
        private static HexCoordinate? OccupiedSpaceOf(
            MatchSessionProjection projection, EffectChoiceCandidate candidate)
        {
            if (candidate.Kind != EffectChoiceCandidateKind.Value)
            {
                return null;
            }

            switch (candidate.Value.Kind)
            {
                case EffectValueKind.Hero:
                    var hero = projection.Heroes.FirstOrDefault(value =>
                        value.HeroId.Equals(candidate.Value.HeroId));
                    return hero.HeroId.IsDefined ? hero.Coordinate : (HexCoordinate?)null;
                case EffectValueKind.Minion:
                    var minion = projection.Minions.FirstOrDefault(value =>
                        value.MinionId.Equals(candidate.Value.MinionId));
                    return minion?.Coordinate;
                case EffectValueKind.Coordinate:
                    var space = candidate.Value.Coordinate;
                    var occupied =
                        projection.Heroes.Any(value => value.Coordinate.Equals(space)) ||
                        projection.Minions.Any(value => value.Coordinate.Equals(space)) ||
                        projection.PlacedTokens.Any(value => value.Coordinate.Equals(space));
                    return occupied ? space : (HexCoordinate?)null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// And for the Minion Battle, which moved onto the board at the same time. Here the rule cuts
        /// both ways: every legal selection must be markable, and a candidate the authority withholds -
        /// rulebook p.8 removes Heavy minions last - must not be, or the board offers what the rules
        /// refuse.
        /// </summary>
        [Test]
        public void TheMinionBattle_MarksEveryLegalSelectionAndNothingTheRulesWithhold()
        {
            var session = MinionChoicePresentationTests.CreatePendingRemovalSession();
            var view = new CapturingBoardInteractionView();
            var board = new BoardInteractionPresenter(session, view);
            var choice = session.Projection.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null);

            board.Refresh();
            var model = view.Model;
            var marked = model.MinionChoiceCues.Select(value => value.MinionId).ToArray();

            Assert.That(marked, Is.EquivalentTo(
                    choice.LegalSelectMinionIds.Concat(choice.SelectedMinionIds).Distinct()),
                "Exactly the selections the authority allows, plus the ones already made.");
            foreach (var withheld in choice.Candidates
                .Select(candidate => candidate.MinionId)
                .Where(id => id.IsDefined)
                .Except(choice.LegalSelectMinionIds)
                .Except(choice.SelectedMinionIds))
            {
                Assert.That(marked, Has.No.Member(withheld),
                    $"'{withheld.Value}' is a candidate the rules currently withhold, so the board " +
                    "must not offer it.");
            }
        }

        private static bool IsBlock(IMatchSession session, ActionOffer offer) =>
            GuardsDigital.Core.Rules.CardResolution.DefenseBlockQuery.IsBlock(
                offer, session.Projection.Definitions);


        private sealed class CapturingBoardInteractionView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }

            public event Action<HeroId> HeroSubmitted;
            public event Action<MinionId> MinionSubmitted;
            public event Action<HexCoordinate> DestinationSubmitted;

            public void Render(BoardInteractionViewModel model) => Model = model;
        }
    }
}
