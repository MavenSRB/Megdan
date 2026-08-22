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
        /// <para>It is deliberately indifferent about which. A candidate that names a space is answered
        /// on the board; one that names a hero by identity rather than by coordinate still carries a
        /// control, because the board is addressed by space and that candidate does not name one. What
        /// must never happen - and is what this pins - is a candidate on none of the three, which is a
        /// question the authority asks and the player cannot answer.</para>
        /// </summary>
        [Test]
        public void EveryLegalEffectChoiceCandidate_IsAnswerableOnTheBoardOrInThePrompt()
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

            board.Refresh();
            var model = view.Model;
            var prompt = RoundActionPresenter.CreateViewModel(session.Projection);

            var onTheBoard = new HashSet<HexCoordinate>(model.EffectChoiceDestinations);
            onTheBoard.UnionWith(model.EffectChoiceTargetSpaces);
            Assert.That(
                model.EffectChoiceDestinations.Intersect(model.EffectChoiceTargetSpaces), Is.Empty,
                "No space is both lit and marked - the board says the two in different vocabularies.");

            var answeredByAControl = new HashSet<DirectorCommand>(
                prompt.EffectChoices.Select(value => value.Command)
                    .Concat(prompt.EffectChoiceTargets.Select(value => value.Command))
                    .Concat(prompt.EffectChoiceDestinations.Select(value => value.Command)));

            foreach (var invocation in choice.SelectInvocations)
            {
                var command = DirectorCommand.AnswerEffectChoice(invocation);
                var namesASpace =
                    invocation.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    onTheBoard.Contains(invocation.Candidate.Value.Coordinate);
                Assert.That(namesASpace || answeredByAControl.Contains(command), Is.True,
                    $"The candidate on option node '{invocation.Candidate.OptionNodeId.Value}' can be " +
                    "answered on the board or from the prompt. On neither, it is a question with no " +
                    "way to answer it.");
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
