using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.StagedHeroBoard;
using static GuardsDigital.Tests.EditMode.TigerclawBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.23. Tigerclaw's printed set resolved through the ordinary pipeline, on boards built
    /// to ask each printed clause the question it is about.
    ///
    /// <para>What each card composes - which node, which capability, which literal - is
    /// <see cref="TigerclawVerifiedContentTests"/>'s and is not repeated. This file asks what a player
    /// is offered and what the board looks like afterwards. <see cref="StagedHeroBoard"/> owns the
    /// board, unchanged from the hero it was generalised out of.</para>
    /// </summary>
    public sealed class TigerclawPrintedSetTests
    {
        // -----------------------------------------------------------------------------------------
        // "Before the attack: Move 2 spaces in a straight line through an enemy unit; target that unit"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Blink Strike offers only the units it can cross in a straight two-space line whose end space
        /// is empty, and walks him to that end space.
        /// </summary>
        [Test]
        public void BlinkStrike_OffersOnlyACrossableEnemy_AndLandsBeyondIt()
        {
            var fixture = Create(
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: new Dictionary<string, HexCoordinate>());
            var crossed = fixture.MinionAt(FirstAdjacent);

            fixture.StartPrimary(TigerclawCardContent.BlinkStrike);

            Assert.That(fixture.Choice, Is.Not.Null,
                "The line is one question: which enemy to cross, which decides where he lands.");
            Assert.That(fixture.CandidateMinions(fixture.Choice), Is.EqualTo(new[] { crossed }),
                "Only an enemy standing one space away in a straight line, with the space beyond it " +
                "empty and playable, can be crossed.");

            fixture.Select(crossed);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(TwoAway),
                "p.18's direct Move takes him to the space the line published - two in a straight " +
                "line - rather than anywhere his Movement value could reach.");
        }

        /// <summary>
        /// A board with no such line lets him spend the card and attack nobody, which is what the
        /// card's own parenthesis restates: "If you cannot make this move, you cannot attack."
        ///
        /// <para>p.10 stops an action at a mandatory step it cannot complete, and every step of this
        /// text is mandatory - so the Attack, which is what comes after, never starts. The action is
        /// still offered: p.12 lets a player Hold instead, and what greys an offer today is a rule
        /// proving there is nothing to target rather than a card text proving there is nothing to do.
        /// That the offer is drawn and does nothing is recorded as an observation of this milestone
        /// rather than asserted around.</para>
        /// </summary>
        [Test]
        public void BlinkStrike_WithNoCrossableEnemy_AttacksNobody()
        {
            var fixture = Create(banishedMinions: MinionsBesideHim());

            fixture.StartPrimary(TigerclawCardContent.BlinkStrike);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.NodeApplied("blink-move"), Is.False,
                "No enemy stands one space away in a straight line, so the line names nobody and the " +
                "Move it publishes never runs.");
            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(HeroAt), "He is where he began.");
            Assert.That(
                fixture.RuleEventTypes().Any(value => value == "Action.AttackStarted"), Is.False,
                "\"If you cannot make this move, you cannot attack.\" - p.10 skips what stands after " +
                "an impossible mandatory step, and the Attack is exactly that.");
        }

        /// <summary>
        /// The space beyond the crossed unit has to be empty. A unit standing there is what separates a
        /// line he may cross from one he may not, and no other rule of the card changes.
        /// </summary>
        [Test]
        public void BlinkStrike_RefusesALineWhoseEndSpaceIsOccupied()
        {
            var fixture = Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = FirstAdjacent,
                    ["red-ranged-01"] = TwoAway
                });

            fixture.StartPrimary(TigerclawCardContent.BlinkStrike);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(HeroAt),
                "He moves *through* the unit and stops beyond it; a space with something in it is not " +
                "a place p.18's Move may end, so no line exists and the step cannot be completed.");
            Assert.That(
                fixture.RuleEventTypes().Any(value => value == "Action.AttackStarted"), Is.False,
                "An enemy is standing beside him and is still not attacked, because what this card " +
                "targets is the unit its line crossed rather than whatever is in reach.");
        }

        // -----------------------------------------------------------------------------------------
        // "Before the attack: You may move 1 space ... After the attack: If you did not move ..."
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Combat Reflexes offers its step-1 move, and declining it opens the step-7 move.
        ///
        /// <para>p.14 runs the two six numbered steps apart - the target named, the Defense answered
        /// and the attack itself all stand between them - so this is one face asking twice, from two
        /// separate applications of its own printed text.</para>
        /// </summary>
        [Test]
        public void CombatReflexes_DecliningTheFirstMove_OpensTheSecond()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.FirstSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent));

            fixture.PlayAttackAt(
                TigerclawCardContent.CombatReflexes, fixture.MinionAt(FirstAdjacent));
            Assert.That(fixture.Choice?.NodeId.Value, Is.EqualTo("before-move"),
                "\"Before the attack: You may move 1 space.\" - p.14 asks it once the target has been " +
                "named, because naming one is what its step 2 is.");
            fixture.Decline();

            Assert.That(fixture.Choice?.NodeId.Value, Is.EqualTo("reflexes-move"),
                "\"If you did not move before the attack, you may move 1 space.\" - he did not, so " +
                "the second move is offered.");
            fixture.Select(SecondAdjacent);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.NodeApplied("before-move"), Is.False, "The first move was declined.");
            Assert.That(fixture.NodeApplied("reflexes-move"), Is.True);
            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(SecondAdjacent),
                "and the second one moved him, one space, after the attack had already removed what " +
                "it targeted.");
        }

        /// <summary>
        /// Taking the step-1 move closes the step-7 one, which is the whole of what its second clause
        /// says and the only place in verified content where one of p.14's moments reads the other.
        /// </summary>
        [Test]
        public void CombatReflexes_TakingTheFirstMove_ClosesTheSecond()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.FirstSibling,
                minionPlacements: At("red-melee-01", FirstAdjacent));

            fixture.PlayAttackAt(
                TigerclawCardContent.CombatReflexes, fixture.MinionAt(FirstAdjacent));
            Assert.That(fixture.Choice, Is.Not.Null, "\"Before the attack: You may move 1 space.\"");
            fixture.Select(SecondAdjacent);
            fixture.Confirm();

            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(SecondAdjacent),
                "He took the step-1 move.");
            Assert.That(fixture.Choice, Is.Null,
                "\"If you did not move before the attack\" is false, so the step-7 move never runs - " +
                "the condition is a branch nobody is asked about rather than a second decision.");
            Assert.That(fixture.NodeApplied("reflexes-move"), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // "If a friendly unit is adjacent to the target, +2 Attack"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The number is read where a player reads it - off the Defense preview the attacked hero is
        /// answering - because what the case is about is the total this Attack resolves with.
        /// </summary>
        [Test]
        public void Backstab_RaisesItsAttackOnlyWhenAFriendlyUnitStandsBesideTheTarget()
        {
            var alone = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: At("primitive-hero", FirstAdjacent));
            alone.PlayAttackAt(
                TigerclawCardContent.Backstab, alone.Enemy, defendSuccessfully: false);

            var supported = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                minionPlacements: At("blue-melee-01", FourthAdjacent),
                heroPlacements: At("primitive-hero", FirstAdjacent));
            supported.PlayAttackAt(
                TigerclawCardContent.Backstab, supported.Enemy, defendSuccessfully: false);

            Assert.That(alone.AttackTotal(), Is.EqualTo(5),
                "Backstab's printed Attack, with no rule standing on it.");
            Assert.That(supported.AttackTotal(), Is.EqualTo(alone.AttackTotal() + 2),
                "p.14's printed `+` is a conditional addition to the Attack Total, and the condition " +
                "is a friendly unit beside the TARGET rather than beside him - here his own Melee " +
                "minion, on a space next to the hero he is attacking.");
        }

        /// <summary>
        /// The parenthesised reminder's own sentence: "A 'friendly unit' is another hero or a minion on
        /// your team." Tigerclaw himself is not one, so standing beside his own target raises nothing.
        /// </summary>
        [Test]
        public void Backstab_DoesNotCountTigerclawHimselfAsTheFriendlyUnit()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: At("primitive-hero", FirstAdjacent),
                banishedMinions: HisOwnMinions());

            fixture.PlayAttackAt(
                TigerclawCardContent.Backstab, fixture.Enemy, defendSuccessfully: false);

            Assert.That(fixture.AttackTotal(), Is.EqualTo(5),
                "Not one unit of his own team is on the board, and he is adjacent to his own target " +
                "by construction - it is a melee Attack - so a query that counted him would raise " +
                "every Backstab ever played.");
        }

        // -----------------------------------------------------------------------------------------
        // "the target cannot perform a primary action to defend"
        // -----------------------------------------------------------------------------------------

        [Test]
        public void BackstabWithABallista_ForbidsAPrimaryDefense_AndLeavesTheSecondaryOne()
        {
            var action = TigerclawCardContent
                .Definition(TigerclawCardContent.BackstabWithABallista)
                .ActionDefinitions
                .Single(candidate => candidate.Id.Equals(TigerclawCardContent.PrimaryRangedAttack));

            var block = TigerclawCardContent.Definition(TigerclawCardContent.Dodge)
                .ActionDefinitions
                .Single(candidate => candidate.Id.Equals(TigerclawCardContent.PrimaryBlock));
            var secondary = TigerclawCardContent.Definition(TigerclawCardContent.HitAndRun)
                .ActionDefinitions
                .Single(candidate => candidate.Origin == ActionDefinitionOrigin.Reaction);

            Assert.That(action.DefenseOfferRestriction.Forbids(block), Is.True,
                "A Block is a printed primary Defense, which is exactly what the clause names.");
            Assert.That(action.DefenseOfferRestriction.Forbids(secondary), Is.False,
                "p.14's ordinary secondary Defense remains legal; the clause restricts the origin " +
                "rather than the card or the value.");
        }

        // -----------------------------------------------------------------------------------------
        // "Take 1 coin from an enemy hero adjacent to you; if you do, you may move"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// With no enemy hero beside him the take is a mandatory step that cannot be completed, so
        /// rulebook p.10 stops the action there - which is what "if you do" means on this face.
        /// </summary>
        [Test]
        public void LightFingered_WithNobodyToStealFrom_StopsAtThatStep()
        {
            var fixture = Create(heroPlacements: At("primitive-hero", SecondThreeAway));

            fixture.StartPrimary(TigerclawCardContent.LightFingered);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.NodeApplied("theft-take"), Is.False,
                "Nobody adjacent is an enemy hero, so the selection names nobody and the take never " +
                "runs.");
            Assert.That(fixture.NodeApplied("theft-follow-up-move"), Is.False,
                "p.10 skips what stands after a mandatory step that cannot be completed, and the " +
                "movement behind the semicolon is exactly that.");
        }

        /// <summary>
        /// An enemy hero standing beside him with an empty pile is the other way the same step fails,
        /// and it is the one the transfer contract decides rather than the selection.
        ///
        /// <para>"Take 1 coin" names a minimum of one, and p.15 makes coins a resource a hero spends
        /// down to nothing - so an empty pile is an ordinary state rather than an edge case. The
        /// transfer refuses rather than moving zero, which is what keeps "took a coin" and "took
        /// nothing" different outcomes.</para>
        ///
        /// <para><b>The positive arm is proved at the capability tier</b>, by
        /// <c>RosterStateOperationCapabilityTests</c>, which drives <c>transfer-resource@1</c> over a
        /// funded ledger directly. No staged session can seed a starting balance - coins are earned
        /// only through p.13's Defeat, and nothing in the setup carries one - so a fixture that claimed
        /// to move a coin here would have had to invent a production seam for it.</para>
        /// </summary>
        [Test]
        public void LightFingered_AgainstAnEmptyPile_TakesNothingAndSkipsWhatFollows()
        {
            var fixture = Create(heroPlacements: At("primitive-hero", FirstAdjacent));
            Assert.That(fixture.Coins(fixture.Enemy), Is.Zero,
                "p.6 starts every hero at nothing, which is the state this case is about.");

            fixture.StartPrimary(TigerclawCardContent.LightFingered);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.Coins(fixture.Hero), Is.Zero, "Nothing was minted.");
            Assert.That(fixture.Coins(fixture.Enemy), Is.Zero, "Nothing was taken.");
            Assert.That(fixture.NodeApplied("theft-follow-up-move"), Is.False,
                "and the movement the semicolon guards is skipped with it.");
        }

        /// <summary>
        /// Master Thief's "1 or 2 coins" is the first verified transfer whose amount is a choice, and
        /// the two amounts are the card's own. What bounds them is declared rather than asked here,
        /// because a board with no funded pile never reaches the question.
        /// </summary>
        [Test]
        public void MasterThief_DeclaresTheTwoAmountsItsFacePrints()
        {
            var take = TigerclawEffectContent
                .SkillEffectFor(
                    TigerclawCardContent.MasterThief, TigerclawCardContent.ContentRevision)
                .Nodes
                .Single(node => node.Id.Value == "theft-take");

            Assert.That(Literal(take, "minimum"), Is.EqualTo(1));
            Assert.That(Literal(take, "maximum"), Is.EqualTo(2),
                "\"Take 1 or 2 coins\" - and Light-Fingered and Pick Pocket print one, which is the " +
                "same declaration with the two numbers equal.");

            foreach (var oneCoin in new[]
                     {
                         TigerclawCardContent.LightFingered,
                         TigerclawCardContent.PickPocket
                     })
            {
                var node = TigerclawEffectContent
                    .SkillEffectFor(oneCoin, TigerclawCardContent.ContentRevision)
                    .Nodes
                    .Single(candidate => candidate.Id.Value == "theft-take");
                Assert.That(Literal(node, "minimum"), Is.EqualTo(1));
                Assert.That(Literal(node, "maximum"), Is.EqualTo(1));
            }
        }

        // -----------------------------------------------------------------------------------------
        // "Give a hero in range a Poison marker"
        // -----------------------------------------------------------------------------------------

        [Test]
        public void PoisonedDagger_GivesTheMarker_AndTheHolderLosesThreeValues()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: At("primitive-hero", TwoAway));

            fixture.StartPrimary(TigerclawCardContent.PoisonedDagger);
            Assert.That(fixture.CandidateHeroes(fixture.Choice), Is.EqualTo(new[] { fixture.Enemy }),
                "\"a hero in range\" - and p.10 excludes Tigerclaw himself, because an action affects " +
                "its own hero only where the text says so.");
            fixture.SelectFirst();
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.MarkersHeldBy(fixture.Enemy), Has.Count.EqualTo(1));
            Assert.That(
                fixture.ValueModifierFor(fixture.Enemy, PrintedValueChannel.Attack), Is.EqualTo(-1));
            Assert.That(
                fixture.ValueModifierFor(fixture.Enemy, PrintedValueChannel.Defense), Is.EqualTo(-1));
            Assert.That(
                fixture.ValueModifierFor(fixture.Enemy, PrintedValueChannel.Initiative),
                Is.EqualTo(-1),
                "The face names three channels at one strength; the marker carries all three because " +
                "the rule is the marker's rather than the card's.");
        }

        [Test]
        public void PoisonedDart_IsTheSameSentenceAtTwice_TheStrength()
        {
            var fixture = Create(
                tier: 3,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: At("primitive-hero", TwoAway));

            fixture.StartPrimary(TigerclawCardContent.PoisonedDart);
            fixture.SelectFirst();
            fixture.AnswerEveryStandingChoice();

            Assert.That(
                fixture.ValueModifierFor(fixture.Enemy, PrintedValueChannel.Attack), Is.EqualTo(-2));
            Assert.That(
                fixture.MarkersHeldBy(fixture.Enemy).Single().Definition.Id,
                Is.EqualTo(TigerclawComponentContent.PoisonTwo),
                "The Tier III card gives the second strength, and the two are never both out from " +
                "his own deck because p.15's Level Up happens after p.8 has returned every marker.");
        }

        // -----------------------------------------------------------------------------------------
        // "If you are adjacent to terrain, place yourself into a space in radius"
        // -----------------------------------------------------------------------------------------

        [Test]
        public void BlendIntoShadows_AwayFromTerrain_PlacesNobodyAndRaisesNoImmunity()
        {
            var fixture = Create();

            fixture.StartPrimary(TigerclawCardContent.BlendIntoShadows);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(HeroAt),
                "The staged space is not beside terrain, so the presence question is false and the " +
                "whole branch behind it is skipped.");
            Assert.That(fixture.NodeApplied("blend-place"), Is.False);
            Assert.That(fixture.NodeApplied("blend-shadow"), Is.False,
                "No Place, no immunity - and the immunity is skipped by the condition rather than by " +
                "a second question.");
        }

        // -----------------------------------------------------------------------------------------
        // Absences recorded rather than tested around
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// No face of his gives a Poison marker to a friendly hero on this board, and that is a fact
        /// about the staging rather than about the cards.
        ///
        /// <para>Poisoned Dagger and Poisoned Dart print "a hero in range", which admits both sides;
        /// the staged one-versus-one has exactly one other hero and they are the enemy. Recording the
        /// gap is what the standing ruling asks - what a hero's faces do not say is absent, and what a
        /// board cannot reach is unreached rather than wrong - and milestone 8.25's six-player
        /// configuration is where a friendly hero stands beside him.</para>
        /// </summary>
        [Test]
        public void TheFriendlyHalfOfTheMarkerSelection_IsUnreachableOnAOneVersusOneBoard()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: At("primitive-hero", TwoAway));

            fixture.StartPrimary(TigerclawCardContent.PoisonedDagger);

            Assert.That(fixture.CandidateHeroes(fixture.Choice), Has.Length.EqualTo(1),
                "One other hero is on this board. The selection's team relation admits either side, " +
                "which TigerclawVerifiedContentTests pins on the declaration; what a one-versus-one " +
                "staging cannot do is put a friendly hero in range.");
        }

        private static int Literal(EffectNodeDefinition node, string parameterName) =>
            node.Inputs
                .Single(input => input.ParameterName == parameterName)
                .Literal.IntegerValue;

        private static IReadOnlyCollection<string> MinionsBesideHim() => new[]
        {
            "red-melee-01", "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-ranged-01", "blue-heavy-01"
        };

        /// <summary>
        /// Every minion on Tigerclaw's own side. He takes the Blue seat in this staged one-versus-one,
        /// as Sabina and Brogan do, whichever colour his pitch roster prints.
        /// </summary>
        private static IReadOnlyCollection<string> HisOwnMinions() => new[]
        {
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };
    }
}
