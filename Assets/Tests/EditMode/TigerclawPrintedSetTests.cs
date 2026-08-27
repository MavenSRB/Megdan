using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
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
        // The Blocks that print a rider, answered as p.14's step 4
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Parry is offered as a whole face against the attack its printed condition names, its rider
        /// takes a card off the attacker, and Tigerclaw is left standing.
        ///
        /// <para><b>The rider names somebody on the other side of the moment, which is the first time
        /// verified content does.</b> "The attacker discards a card, if able" is the defender's own
        /// card text at p.14's step 4, and the hero it names is the one taking the turn - so the
        /// resolution the answer resumes through has to know who that was. It did not: the frame
        /// opened its discard and then refused every answer to it, because the chooser resolved to
        /// nobody. The question was askable and unanswerable.</para>
        ///
        /// <para>The round trip is here rather than in a fixture of its own for the same reason: the
        /// snapshot reader rebuilds this frame's candidates on the way back and had no attacker to
        /// rebuild them for, so the payload holding the standing question could not be read at
        /// all - which surfaced as a failed rollback rather than as a failed restore.</para>
        /// </summary>
        [Test]
        public void Parry_BlocksTheNonRangedAttack_AndItsRiderTakesTheAttackersCard()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: At("blue-test-hero", FirstAdjacent));

            fixture.TakeTheEnemysAttack(
                WaspCardContent.Electrocute, TigerclawCardContent.BlendIntoShadows);
            var attackersHand = fixture.Hand(fixture.Enemy).Count;
            fixture.DefendWith(TigerclawCardContent.Parry);

            Assert.That(fixture.Choice, Is.Not.Null,
                "\"The attacker discards a card, if able\" is p.14's step 4, and which card is the " +
                "attacker's own decision.");
            fixture.RoundTripThroughASnapshot();
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.Hand(fixture.Enemy).Count, Is.EqualTo(attackersHand - 1),
                "The card leaves the attacker's hand, not the defender's.");
            Assert.That(fixture.IsOnBoard(fixture.Hero), Is.True,
                "and the Block succeeded, because Electrocute is not a ranged attack and Parry is " +
                "printed against exactly that.");
        }

        /// <summary>
        /// Sidestep's rider moves the defender out of contact, and p.14 still reaches its own step 6.
        ///
        /// <para><b>This is the case that found the outcome comparison re-deriving its target.</b> The
        /// page names its target at step 2 and never names it again; step 4 is the defender's card
        /// text, and this is the first verified face whose Defense text moves the defender. Asking
        /// again whether the attacker could reach them refused the page its own comparison, and the
        /// whole command was rolled back as an engine fault - so a printed step a player took made the
        /// match unresolvable rather than making them harder to hit.</para>
        ///
        /// <para>He is defeated all the same, and that is the content rather than the defect: Sidestep
        /// blocks a <em>ranged</em> attack, Electrocute is not one, and p.14 defeats a defender whose
        /// Block condition is not met however far they stepped.</para>
        /// </summary>
        [Test]
        public void Sidestep_StepsAwayOnItsOwnRider_AndPageFourteenStillCompares()
        {
            var fixture = Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.FirstSibling,
                heroPlacements: At("blue-test-hero", FirstAdjacent));

            fixture.TakeTheEnemysAttack(
                WaspCardContent.Electrocute, TigerclawCardContent.BlendIntoShadows);
            fixture.DefendWith(TigerclawCardContent.Sidestep);

            Assert.That(fixture.Choice, Is.Not.Null, "\"You may move 1 space.\"");
            var offered = fixture.CandidateSpaces(fixture.Choice);
            Assert.That(offered, Is.Not.Empty);
            var away = offered.First(space =>
                HexDistanceQuery.Calculate(space, fixture.HeroSpace(fixture.Enemy)) > 1);
            fixture.Select(away);
            fixture.AnswerEveryStandingChoice();

            Assert.That(
                fixture.RuleEventTypes().Any(value => value == "Action.AttackResolutionCompleted"),
                Is.True,
                "p.14 reaches its own end with the defender standing somewhere else, rather than " +
                "refusing the comparison it already validated.");
            Assert.That(fixture.IsOnBoard(fixture.Hero), Is.False,
                "and the comparison it reaches is a Defeat: Sidestep is printed against a ranged " +
                "attack and this one is not, so stepping away changes where he was standing rather " +
                "than whether the Block succeeded.");
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

            Assert.That(fixture.MarkersHeldBy(fixture.Enemy).Count, Is.EqualTo(1),
                "One marker, from the one supply his face names.");
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

        /// <summary>
        /// Standing beside terrain, the same face offers the disk its own icon prints and places him
        /// into the space he names.
        ///
        /// <para><b>This is the arm that found milestone 8.20's Place measuring the wrong icon.</b>
        /// The printed-Radius Place is milestone 8.23's, and the reach behind it was reading
        /// <c>RangeValueQuery</c> - the only printed arm that existed when it was written. Blend Into
        /// Shadows prints a Radius icon and no Range icon, so its reach came back zero and the Place
        /// reported itself impossible at every space on the board; the condition arm above passed
        /// throughout, because a branch that is skipped never asks.</para>
        /// </summary>
        [Test]
        public void BlendIntoShadows_BesideTerrain_OffersItsPrintedRadiusAndPlacesHimInIt()
        {
            var fixture = Create(heroPlacements: At("blue-test-hero", AdjacentAgainstTerrain));

            fixture.StartPrimary(TigerclawCardContent.BlendIntoShadows);

            Assert.That(fixture.Choice, Is.Not.Null,
                "\"place yourself into a space in radius\" is a question, and standing beside the " +
                "terrain at (-3, 3) is what makes the condition in front of it true.");
            var offered = fixture.CandidateSpaces(fixture.Choice);
            Assert.That(offered, Is.Not.Empty);
            Assert.That(
                offered.All(space =>
                    HexDistanceQuery.Calculate(space, AdjacentAgainstTerrain) <= 2),
                Is.True,
                "The disk is the printed Radius 2 measured from him, and every space in it is his " +
                "own icon's rather than some other channel's.");
            Assert.That(
                offered.Any(space =>
                    HexDistanceQuery.Calculate(space, AdjacentAgainstTerrain) == 2),
                Is.True,
                "and it reaches the whole of that 2 - a disk one space short would be the Range " +
                "icon he does not print.");

            var destination = offered.First(space =>
                HexDistanceQuery.Calculate(space, AdjacentAgainstTerrain) == 2);
            fixture.Select(destination);
            fixture.AnswerEveryStandingChoice();

            Assert.That(fixture.HeroSpace(fixture.Hero), Is.EqualTo(destination),
                "p.18's Place puts him into the space he named.");
            Assert.That(fixture.NodeApplied("blend-place"), Is.True);
            Assert.That(fixture.NodeApplied("blend-shadow"), Is.True,
                "and the immunity behind the semicolon runs, because the Place it depends on did.");
        }


        // -----------------------------------------------------------------------------------------
        // "After you perform a basic action you may repeat it once"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// p.16's unlocked Ultimate stops the game after a basic action, and BOTH answers to it are
        /// accepted.
        ///
        /// <para><b>Neither was.</b> Cloak and Daggers fires after the action it repeats has ended -
        /// which is what makes p.10's Repeat reachable from outside that action at all - so the
        /// actor's card is already resolved and the next actor already selected while its decision
        /// stands. The answer advanced Initiative a second time, that advance was refused, and the
        /// aggregate rolled the whole answer back as an atomicity violation: the decision was drawn on
        /// the surface and no answer to it could be given. Milestone 8.21 met the identical shape on
        /// Brogan's removal-prevention prompt and guarded that command; this is the other command that
        /// finishes a trigger of this shape.</para>
        ///
        /// <para><b>Milestone 8.25a states what each answer then does</b>, which this case could not:
        /// until that milestone, taking the repeat owed an instance nothing started, so both answers
        /// looked alike from here and the assertion below could only say that the decision closed.
        /// Declining ends the action and hands Initiative on; taking it starts the second instance
        /// there and then, on the same card, under the same hero, with his card still unresolved -
        /// which is the position rulebook p.10 describes. Blink Strike's own line then names a unit
        /// again, under the exclusion the card prints, so what stands after a taken repeat is that
        /// line rather than nothing.</para>
        /// </summary>
        [Test]
        public void CloakAndDaggers_OffersItsRepeatAfterABasicAction_AndBothAnswersAreAccepted()
        {
            foreach (var takeIt in new[] { true, false })
            {
                var fixture = Create(
                    tier: 3,
                    branch: TigerclawUpgradeBranch.FirstSibling,
                    minionPlacements: At("red-melee-01", FirstAdjacent),
                    heroPlacements: new Dictionary<string, HexCoordinate>());

                fixture.StartPrimary(TigerclawCardContent.BlinkStrike);
                fixture.SelectFirst();
                fixture.Confirm();

                Assert.That(fixture.Choice, Is.Not.Null,
                    "The unlocked Ultimate stops the game after the basic action it watched.");
                Assert.That(fixture.Choice.NodeId.Value, Is.EqualTo("cloak-and-daggers-repeat"));
                Assert.That(fixture.Choice.ConfirmInvocation, Is.Not.Null);
                Assert.That(fixture.Choice.DeclineInvocation, Is.Not.Null,
                    "\"you may repeat it once\" - p.17's `you may` is an offer, so the decline is an " +
                    "answer a player has to be able to give by name.");

                // Milestone 8.25a. The decision stands inside his own action, so he is still the hero
                // Initiative has selected and the card he took the action on is still unresolved.
                // Before that milestone both had already been given up, which is why the instance the
                // answer owed had nothing to run on.
                Assert.That(fixture.Session.Projection.SelectedActor.ActorId, Is.EqualTo(fixture.Hero),
                    "The ability asks about his own action, so it asks while it is still his turn.");

                if (takeIt)
                {
                    fixture.Confirm();

                    Assert.That(fixture.Choice, Is.Not.Null,
                        "Taking the repeat starts the second instance rather than owing one: p.10's " +
                        "repeated action is that action performed again, and Blink Strike's line is " +
                        "the first thing it asks.");
                    Assert.That(fixture.Choice.NodeId.Value, Is.EqualTo("blink-line"));
                    Assert.That(fixture.Choice.ChooserId, Is.EqualTo(fixture.Hero),
                        "and it is his question, asked while Initiative still has him selected - " +
                        "which is what makes it answerable at all.");
                    Assert.That(
                        fixture.Session.Projection.SelectedActor.ActorId, Is.EqualTo(fixture.Hero));
                }
                else
                {
                    fixture.Decline();

                    Assert.That(fixture.Choice, Is.Null,
                        "Declining closes the decision rather than being rolled back under it, and " +
                        "the action it was about is over.");
                    Assert.That(fixture.Session.Projection.SelectedActor.ActorId,
                        Is.Not.EqualTo(fixture.Hero),
                        "so the turn goes on in Initiative order, which it could not do while the " +
                        "ability was still standing.");
                }
            }
        }
        /// <summary>
        /// Milestone 8.25e. p.12's Hold is a basic action when it is performed on a basic card, so the
        /// Ultimate offers to repeat it - and taking that offer performs a second Hold.
        /// </summary>
        /// <remarks>
        /// Taking it did nothing at all. The dispatcher that starts an owed repeat switched on Attack,
        /// Skill and Movement and reported "nothing happened" for every other kind, and its comment for
        /// that arm said the second instance "is started there", in the turn authority, where nothing
        /// started it. So the ability offered, the player confirmed, the command was accepted, and the
        /// turn moved on exactly as a decline would have.
        ///
        /// <para>Two facts make the second instance real rather than ceremonial, and both are asserted
        /// here. The first is that the action is <em>held</em> across the decision: before this
        /// milestone the three general-rule paths resolved their card and acknowledged Initiative
        /// before raising the checkpoint the ability fires at, so the decision stood over a finished
        /// turn - nobody selected, card resolved - and an instance started from it would have had
        /// nothing to run on. The second is that performing a Hold raises p.8's checkpoints and records
        /// p.9's supertype again, which is what a second instance of an action that does nothing
        /// consists of.</para>
        /// </remarks>
        [Test]
        public void CloakAndDaggers_RepeatsAHoldByPerformingASecondOne()
        {
            var fixture = Create(
                tier: 3,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: new Dictionary<string, HexCoordinate>());

            // Commits every seat and drains to his own turn without starting an action, so the Hold
            // below is the one action performed on the card.
            fixture.IsPrimaryAvailable(TigerclawCardContent.BlinkStrike);
            var selected = fixture.Session.Projection.SelectedActor;
            fixture.Accept(DirectorCommand.PerformHold(selected.CardInstanceId));

            Assert.That(fixture.Choice, Is.Not.Null);
            Assert.That(fixture.Choice.NodeId.Value, Is.EqualTo("cloak-and-daggers-repeat"),
                "p.9 makes every action on a basic card basic, and Blink Strike is Gold, so his Hold " +
                "on it is a basic action the Ultimate watches.");
            Assert.That(fixture.Session.Projection.SelectedActor.ActorId, Is.EqualTo(fixture.Hero),
                "and the action is held open across the decision, exactly as an Attack, a Skill and a " +
                "Movement have been since milestone 8.25a - which is what gives the second instance a " +
                "hero and an unresolved card to be performed on.");

            var before = HoldsPerformed(fixture);
            fixture.Confirm();

            Assert.That(HoldsPerformed(fixture), Is.EqualTo(before + 1),
                "p.10: \"you may perform the entire action again\". Performing a Hold is doing " +
                "nothing, and doing nothing twice is two actions performed rather than one.");
            Assert.That(fixture.Choice, Is.Null,
                "The repeated instance is itself a repeat, so p.10's \"You cannot repeat a repeated " +
                "action!\" closes the ability rather than asking again.");
            Assert.That(fixture.Session.Projection.SelectedActor.ActorId, Is.Not.EqualTo(fixture.Hero),
                "and the held action then finishes: his card resolves and the turn goes on.");
        }

        /// <summary>
        /// Milestone 8.25e. p.11's Fast Travel is a basic action on a basic card and the Ultimate does
        /// not offer to repeat it, because the only instance that repeat could produce is a second
        /// replacement.
        /// </summary>
        /// <remarks>
        /// It was offered, and taking it did nothing - the same dropped arm the Hold above fell into.
        /// The reading is recorded in the <see href="../../../Docs/AI/Rules/RULE_SYSTEMS.md">rule-system
        /// ledger</see>: p.11 performs a Fast Travel "instead of your entire Movement action" and caps
        /// it at "one Fast Travel action, regardless of the movement value on the card", and p.10's own
        /// bullet reads "A repeated instance of an action cannot be replaced with a different action
        /// (Such as Clear or Fast Travel)". p.13's Clear is the same sentence about an Attack.
        ///
        /// <para>The board is his own with every minion banished and Wasp put two zones away, because
        /// p.11 refuses a Fast Travel out of or into a zone holding an enemy hero or minion and the
        /// staged board deliberately holds both.</para>
        /// </remarks>
        [Test]
        public void CloakAndDaggers_DoesNotOfferToRepeatAFastTravel()
        {
            var fixture = Create(
                tier: 3,
                banishedMinions: new[]
                {
                    "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
                    "red-ranged-01", "red-heavy-01",
                    "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
                    "blue-ranged-01", "blue-heavy-01"
                },
                heroPlacements: At("primitive-hero", new HexCoordinate(-7, 1)));

            fixture.IsPrimaryAvailable(TigerclawCardContent.BlinkStrike);
            var option = fixture.Session.Projection.AvailableFastTravelOptions.Single();
            fixture.Accept(DirectorCommand.PerformFastTravel(
                option.ReplacedMovementActionDefinitionId, option.LegalDestinations[0]));

            Assert.That(fixture.Choice, Is.Null,
                "A repeat the page cannot make an instance of is one the ability does not offer. It " +
                "used to be offered and dropped, which is the one outcome a `may` must never produce.");
            Assert.That(fixture.Session.Projection.SelectedActor.ActorId, Is.Not.EqualTo(fixture.Hero),
                "and with nothing standing, his card resolves and the turn goes on.");
        }

        /// <summary>
        /// Milestone 8.25e. A repeated Attack that starts and then cannot complete the first mandatory
        /// step of its own text finishes the action it repeated, rather than leaving the turn open.
        /// </summary>
        /// <remarks>
        /// This is the case milestone 8.25e's block reported from Play Mode: taking the repeat "returns
        /// the game to choose an action or End Turn with nothing having happened". Blink Strike's text
        /// names its own target - a straight two-space line through an enemy unit - so the second
        /// instance starts at p.14's step 1 and the text looks for a line. On a board with nothing left
        /// to cross there is none, and p.10's Mandatory and Optional Steps stops the action at that
        /// step, which is the page working rather than an offer that should not have been made.
        ///
        /// <para>What was wrong is what happened next. The turn decided whether it was waiting from
        /// what the resolver reported <em>before</em> the steps were applied, so it accepted the answer
        /// and returned while the started instance had already died inside its text: the hero stayed
        /// selected on an unresolved card and the match offered him a second action on it, which p.7
        /// forbids. It is decided from what actually stands afterwards now.</para>
        /// </remarks>
        [Test]
        public void CloakAndDaggers_RepeatThatStopsAtItsOwnMandatoryStep_StillFinishesTheAction()
        {
            var fixture = Create(
                tier: 3,
                minionPlacements: At("red-melee-01", FirstAdjacent),
                banishedMinions: new[]
                {
                    "red-melee-02", "red-melee-03", "red-melee-04",
                    "red-ranged-01", "red-heavy-01",
                    "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
                    "blue-ranged-01", "blue-heavy-01"
                },
                heroPlacements: At("primitive-hero", new HexCoordinate(-7, 1)));

            fixture.StartPrimary(TigerclawCardContent.BlinkStrike);
            fixture.SelectFirst();
            fixture.Confirm();
            Assert.That(fixture.Choice, Is.Not.Null);
            Assert.That(fixture.Choice.NodeId.Value, Is.EqualTo("cloak-and-daggers-repeat"));
            Assert.That(fixture.MinionIds(), Is.Empty,
                "The one unit he could cross was the one his first instance defeated, so the second " +
                "has no line at all.");

            fixture.Confirm();

            Assert.That(fixture.Choice, Is.Null);
            Assert.That(fixture.Session.Projection.SelectedActor.ActorId, Is.Not.EqualTo(fixture.Hero),
                "The repeated instance stopped at its own first mandatory step, and the action it " +
                "repeated is then over: his card resolves and Initiative moves on. It used to leave " +
                "him selected on an unresolved card, able to start a second action on it.");
        }

        /// <summary>How many Holds this match has recorded, in its authoritative rule-event log.</summary>
        private static int HoldsPerformed(StagedHeroBoard fixture) =>
            fixture.Session.Projection.DiagnosticEvents
                .Count(entry => entry.EventType == "Turn.HoldActionPerformed");

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
