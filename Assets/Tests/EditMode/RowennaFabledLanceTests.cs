using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.57e. Rowenna the Vanguard's Ultimate, Fabled Lance: "All your attack actions gain
    /// the &quot;Ranged&quot; subtype, target a unit in range, and count as having a printed Range
    /// value of 2."
    ///
    /// <para><b>What the printed sentence changes, and where each half is proved.</b> Rulebook p.13
    /// gives a Ranged Attack two consequences - a "Block a ranged attack" answers it, and it reaches
    /// "units based on the Range value on the card" - and p.9 says what that value is. So the cases
    /// below are the subtype (the Block, both arms, both directions), the reach (every one of her six
    /// Attack faces, at one, two and three spaces), and the number (the Range Total, with and without
    /// p.16's Range item). Her Ultimate is unlocked through p.16's own condition rather than by moving
    /// a card, so the whole chain is the production one from the unlock to the target set.</para>
    ///
    /// <para><b>Why these cases read the production queries rather than drive a configured match.</b>
    /// Nothing of Rowenna's is registered - milestone 7.9's gate holds her content out of
    /// <see cref="MatchContentCatalogs"/> until 8.57g deals her - so no composed match can put one of
    /// these cards in a hand and take p.14's order from a played Attack. That is the same boundary
    /// 8.57a through 8.57d each recorded, and it is why the offer/resolver/preview agreement is proved
    /// STRUCTURALLY here, by the IL rule in
    /// <see cref="TheOfferTheResolverAndTheDefencePreview_EachReadTheOneOverlayQuery"/>: all three
    /// production seams reach one query, so a board cannot make two of them disagree. 8.57g's dealt
    /// match runs the same three end to end.</para>
    ///
    /// <para><b>The Blocks are another hero's real printed faces.</b> Tigerclaw is the roster's one
    /// hero printing both arms of p.14's condition - Dodge blocks a ranged attack, Parry a non-ranged
    /// one - so "a Ranged Block now answers her and a non-Ranged one no longer does" is asked of
    /// verified content rather than of a fixture card written to agree.</para>
    /// </summary>
    public sealed class RowennaFabledLanceTests
    {
        private static readonly HeroId Rowenna = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static readonly HexCoordinate RowennaAt = new HexCoordinate(0, 0);

        /// <summary>One space away: legal for every Attack she prints, before and after the unlock.</summary>
        private static readonly HexCoordinate Adjacent = new HexCoordinate(1, 0);

        /// <summary>Two spaces: the reach Fabled Lance's own printed Range 2 buys her.</summary>
        private static readonly HexCoordinate TwoAway = new HexCoordinate(2, 0);

        /// <summary>Three spaces: outside her Range 2 and inside it once a Range item is earned.</summary>
        private static readonly HexCoordinate ThreeAway = new HexCoordinate(3, 0);

        /// <summary>Her six Attack faces - the Gold basic and the five Reds.</summary>
        private static IEnumerable<CardDefinitionId> AttackFaces() => new[]
        {
            RowennaCardContent.CodeOfChivalry,
            RowennaCardContent.TokenOfGratitude,
            RowennaCardContent.FairShare,
            RowennaCardContent.FeatOfBravery,
            RowennaCardContent.ParagonOfGrace,
            RowennaCardContent.ParagonOfValor
        };

        // -------------------------------------------------------------------------------------------
        // The unlock, through rulebook p.16's own condition
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.16 unlocks the Ultimate "once you upgrade all your cards to Tier III", and until then
        /// nothing about her attacks has changed. The refusal is the page's own, asked of the same
        /// query a level purchase asks.
        /// </summary>
        [Test]
        public void TheAbility_ArrivesOnlyWhenPageSixteensOwnConditionIsMet()
        {
            var world = World.Create();

            Assert.That(
                world.TryUnlock().RejectionReason,
                Is.EqualTo(UltimateUnlockRejectionReason.UpgradeIncomplete),
                "Her hand still holds a Tier I, so p.16's condition is not met and the card stays " +
                "in the deck.");
            Assert.That(world.Modifications().AttackActionsCountAsRangedAtPrintedRange(
                    RuleObject.ForHero(Rowenna)),
                Is.Zero,
                "and nothing declares a printed Range for her attacks while the card is unlocked.");

            world.UpgradeHandToTierThree();
            Assert.That(world.TryUnlock().IsUnlocked, Is.True,
                "Every non-Basic card in hand is Tier III, which is the whole of p.16's condition.");
            Assert.That(world.UltimateCard().State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "p.16 puts the card faceup beside the dashboard, and the unlock is that move.");
            Assert.That(world.Modifications().AttackActionsCountAsRangedAtPrintedRange(
                    RuleObject.ForHero(Rowenna)),
                Is.EqualTo(RowennaEffectContent.FabledLancePrintedRange),
                "and the printed sentence's own number is what the live set now carries.");
        }

        /// <summary>
        /// Before the unlock every Attack face of hers prints "Target a unit adjacent to you" and means
        /// it. This is the control the rest of the fixture is read against: the same boards, the same
        /// faces, the rule absent.
        /// </summary>
        [Test]
        public void BeforeTheUnlock_EveryAttackFaceReachesOnlyTheUnitBesideHer()
        {
            var world = World.Create();

            foreach (var face in AttackFaces())
            {
                Assert.That(world.TargetsOf(face, Adjacent), Is.EqualTo(new[] { Enemy }),
                    $"{face} prints 'Target a unit adjacent to you', which p.13 reads as adjacency.");
                Assert.That(world.TargetsOf(face, TwoAway), Is.Empty,
                    $"{face} reaches nobody two spaces away before the Ultimate is unlocked.");
                Assert.That(world.ClassificationOf(face), Is.EqualTo(AttackRangeKind.NonRanged),
                    $"{face} is printed non-Ranged and nothing has said otherwise.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // "target a unit in range" - the reach
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The printed clause on every face it names. Two spaces is legal, three is not, and the unit
        /// standing beside her is still a legal target - p.13's reach is a ceiling, so widening it
        /// takes nothing away.
        /// </summary>
        [Test]
        public void AfterTheUnlock_EveryAttackFaceReachesTwoSpaces_AndNotThree()
        {
            var world = World.Create();
            world.Unlock();

            foreach (var face in AttackFaces())
            {
                Assert.That(world.ClassificationOf(face), Is.EqualTo(AttackRangeKind.Ranged),
                    $"{face} 'gains the \"Ranged\" subtype' - the first of the sentence's three clauses.");
                Assert.That(world.RangeTotalOf(face), Is.EqualTo(2),
                    $"{face} 'counts as having a printed Range value of 2' - the third clause.");
                Assert.That(world.TargetsOf(face, Adjacent), Is.EqualTo(new[] { Enemy }),
                    $"{face} still reaches the unit beside her: p.13's 'in range' is a ceiling.");
                Assert.That(world.TargetsOf(face, TwoAway), Is.EqualTo(new[] { Enemy }),
                    $"{face} now reaches two spaces - the second clause, 'target a unit in range'.");
                Assert.That(world.TargetsOf(face, ThreeAway), Is.Empty,
                    $"{face} reaches no further than the number the card prints.");
            }
        }

        /// <summary>
        /// p.16's Range item, and the reason the printed clause is an ICON rather than a bonus. The
        /// page is explicit that "if an Action card has no matching icon, the item has no effect on
        /// that particular Action card", and her Attack faces print no Range icon at all - so an
        /// override composed as a value modifier would have left her item matching nothing and her
        /// reach frozen at two. Paragon of Grace's printed item is a Range item, which is why this is
        /// the natural case rather than an invented one.
        /// </summary>
        [Test]
        public void WithARangeItem_HerAttacksReachThree()
        {
            var world = World.Create();
            world.Unlock();
            world.EarnARangeItem();

            foreach (var face in AttackFaces())
            {
                Assert.That(world.RangeTotalOf(face), Is.EqualTo(3),
                    $"{face}: p.16 raises the icon the Ultimate supplies, by one, exactly once.");
                Assert.That(world.TargetsOf(face, ThreeAway), Is.EqualTo(new[] { Enemy }),
                    $"{face} reaches three spaces with one Range item, which is the whole point of " +
                    "the clause saying the attacks count as HAVING a printed value.");
            }
        }

        /// <summary>
        /// p.13 lets an Attack take "enemy heroes and minions" and no others, and nothing in this
        /// sentence touches that half: a friendly minion two spaces away is inside the new reach and is
        /// still not a legal target.
        /// </summary>
        [Test]
        public void AFriendlyUnitIsNeverATarget_HoweverFarTheReachNowGoes()
        {
            var world = World.Create();
            world.Unlock();

            var targets = world.TargetsOfWithFriendlyMinion(
                RowennaCardContent.CodeOfChivalry, TwoAway, TwoAway);
            Assert.That(targets, Is.Empty,
                "The widened reach admits enemies; p.13's own 'enemy heroes and minions' is not " +
                "something this sentence says anything about.");
        }

        /// <summary>
        /// An enemy MINION inside the new reach is a legal target, which is the positive half of the
        /// case above: "target a unit" is rulebook p.19's type-insensitive union and her faces print
        /// that word rather than "hero".
        /// </summary>
        [Test]
        public void AnEnemyMinionTwoSpacesAway_IsALegalTarget()
        {
            var world = World.Create();
            world.Unlock();

            var targets = world.MinionTargetsOf(RowennaCardContent.CodeOfChivalry, TwoAway);
            Assert.That(targets, Is.EqualTo(new[] { World.EnemyMinion }),
                "Her faces print 'a unit', and p.19's unit is the union of both registers.");
        }

        // -------------------------------------------------------------------------------------------
        // The subtype - rulebook p.14's Block condition, both arms and both directions
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The consequence a defender feels. Tigerclaw's Dodge prints "Block a ranged attack" and his
        /// Parry prints the other arm; before the unlock her Code of Chivalry is answered by Parry and
        /// not by Dodge, and after it the two swap. Both arms and both directions, because a rule that
        /// only widened one of them would let a Block answer every attack there is.
        /// </summary>
        [Test]
        public void ARangedBlockAnswersHerAttack_AndANonRangedBlockNoLongerDoes()
        {
            var world = World.Create();

            Assert.That(
                world.BlockVerdict(TigerclawCardContent.Dodge, RowennaCardContent.CodeOfChivalry),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "Before the unlock her Attack is printed non-Ranged, so a ranged-attack Block does " +
                "not answer it.");
            Assert.That(
                world.BlockVerdict(TigerclawCardContent.Parry, RowennaCardContent.CodeOfChivalry),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "and the non-ranged arm does.");

            world.Unlock();

            Assert.That(
                world.BlockVerdict(TigerclawCardContent.Dodge, RowennaCardContent.CodeOfChivalry),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "'All your attack actions gain the \"Ranged\" subtype' is exactly what p.14's ranged " +
                "arm asks about.");
            Assert.That(
                world.BlockVerdict(TigerclawCardContent.Parry, RowennaCardContent.CodeOfChivalry),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "and the complement stops answering her, which is what makes the first half a rule " +
                "rather than a bonus.");
        }

        /// <summary>
        /// Every one of her Attack faces, not only the one the case above names: the printed sentence
        /// says "all your attack actions".
        /// </summary>
        [Test]
        public void TheRangedArmAnswersEveryAttackFaceSheHas()
        {
            var world = World.Create();
            world.Unlock();

            foreach (var face in AttackFaces())
            {
                Assert.That(world.BlockVerdict(TigerclawCardContent.Dodge, face),
                    Is.EqualTo(DefenseBlockResult.Blocked), $"{face} is one of 'all your attack actions'.");
                Assert.That(world.BlockVerdict(TigerclawCardContent.Parry, face),
                    Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                    $"{face} is no longer a non-ranged attack for p.14's purposes.");
            }
        }

        /// <summary>
        /// "All your attack actions" counts the instances the rules produce as well as the one a card
        /// play starts. Paragon of Grace prints "If they do, repeat once on a different target" and
        /// Paragon of Valor the same, so a repeated instance is one of her attack actions; so is one
        /// another card's text grants. Neither is a different sentence, and the declaration is read off
        /// the ACTOR and the ACTION rather than off how the instance came to be - which is what these
        /// two contexts prove, because they are the only thing that differs between the three offers.
        /// </summary>
        [Test]
        public void ARepeatedOrGrantedInstanceOfHerAttackIsRangedToo()
        {
            var world = World.Create();
            world.Unlock();

            foreach (var context in new[]
            {
                ActionInvocationContext.Repeated, ActionInvocationContext.TextGranted
            })
            {
                Assert.That(
                    world.TargetsOf(RowennaCardContent.ParagonOfGrace, TwoAway, context),
                    Is.EqualTo(new[] { Enemy }),
                    $"A {context} instance of her Attack reaches the same two spaces the printed " +
                    "one does.");
                Assert.That(
                    world.BlockVerdict(TigerclawCardContent.Dodge, RowennaCardContent.ParagonOfGrace),
                    Is.EqualTo(DefenseBlockResult.Blocked),
                    "and p.14's ranged arm answers it, because the subtype is the action's rather " +
                    "than the instance's.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // What the sentence deliberately does not reach
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "attack actions" and nothing else. Her Blue Skills print their own Range icon - Stand Guard
        /// a 3, Protect the Weak and Defend the Innocent a 4 - and the override must not touch them, or
        /// a p.16 upgrade would silently SHORTEN the card that earned it.
        /// </summary>
        [Test]
        public void HerSkillsKeepTheirOwnPrintedRange()
        {
            var world = World.Create();
            world.Unlock();

            Assert.That(world.SkillRangeTotalOf(RowennaCardContent.StandGuard), Is.EqualTo(3));
            Assert.That(world.SkillRangeTotalOf(RowennaCardContent.ProtectTheWeak), Is.EqualTo(4));
            Assert.That(world.SkillRangeTotalOf(RowennaCardContent.DefendTheInnocent), Is.EqualTo(4));
            Assert.That(world.SkillRangeTotalOf(RowennaCardContent.ThrowTheGauntlet), Is.EqualTo(4),
                "Her Silver basic is a Skill too - 'BASIC SKILL - RANGED' - and the clause names " +
                "attack actions.");
        }

        /// <summary>
        /// p.9 keeps Range and Radius apart and so does this. Her Attack faces' riders read their own
        /// printed Radius disk - "a friendly hero in radius" - and the Ultimate says nothing about it,
        /// so Token of Gratitude's 1 and Fair Share's 3 are unmoved.
        /// </summary>
        [Test]
        public void TheRidersRadiusIsUntouched()
        {
            var world = World.Create();
            var before = AttackFaces().Select(world.RadiusTotalOf).ToArray();
            world.Unlock();

            Assert.That(AttackFaces().Select(world.RadiusTotalOf).ToArray(), Is.EqualTo(before),
                "Radius and Range are two icons, and this sentence names one of them.");
            Assert.That(world.RadiusTotalOf(RowennaCardContent.TokenOfGratitude), Is.EqualTo(1));
            Assert.That(world.RadiusTotalOf(RowennaCardContent.FairShare), Is.EqualTo(3));
        }

        /// <summary>
        /// The control case, and the one that says the rule is about a hero rather than about the
        /// build: the other hero's Attack on the same board, while her Ultimate stands, is classified
        /// and reaches exactly what it did.
        /// </summary>
        [Test]
        public void NoOtherHerosAttackIsReclassified()
        {
            var world = World.Create();
            world.Unlock();

            Assert.That(world.EnemyClassification(), Is.EqualTo(AttackRangeKind.NonRanged),
                "p.16 gives the ability to 'you', and the live set carries it on her alone.");
            Assert.That(world.EnemyRangeTotal(), Is.Zero,
                "His copy of the same printed face still prints no Range icon at all.");
            Assert.That(world.EnemyTargets(Adjacent), Is.EqualTo(new[] { Rowenna }));
            Assert.That(world.EnemyTargets(TwoAway), Is.Empty,
                "and his adjacency is untouched by a declaration that is not about him.");
            Assert.That(
                world.BlockVerdictAgainstTheEnemysAttack(TigerclawCardContent.Parry),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "so a non-ranged Block still answers HIS attack while it no longer answers hers.");
        }

        // -------------------------------------------------------------------------------------------
        // Derived, never stored
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.16 states the whole lifetime in one sentence - the ability is "always in effect (as long
        /// as your hero is on the board)" - and nothing about this rule is kept anywhere. The evidence
        /// is that the same card position derives the same rule twice, that moving the card away takes
        /// it with it, and that the hero leaving the board suppresses it without deleting anything.
        /// That is the same reading the unlocked state already survives a snapshot restore under
        /// (<c>UltimateContinuousSourceTests</c>): the restore replays the card's position and this
        /// rule is derived from it again rather than carried beside it.
        /// </summary>
        [Test]
        public void TheRuleIsDerivedFromTheUnlockedCard_AndNeverStoredBesideIt()
        {
            var world = World.Create();
            world.Unlock();
            var rowenna = RuleObject.ForHero(Rowenna);

            Assert.That(
                world.Modifications().AttackActionsCountAsRangedAtPrintedRange(rowenna),
                Is.EqualTo(world.Modifications().AttackActionsCountAsRangedAtPrintedRange(rowenna)),
                "Two derivations from one card position are one answer, which is what 'nothing is " +
                "stored' means in practice.");

            Assert.That(
                world.ModificationsWithHeroOffTheBoard()
                    .AttackActionsCountAsRangedAtPrintedRange(rowenna),
                Is.Zero,
                "p.16 applies the ability 'as long as your hero is on the board'.");
            Assert.That(world.UltimateCard().State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "and the suppression deletes nothing: the card is still beside the dashboard.");
            Assert.That(
                world.Modifications().AttackActionsCountAsRangedAtPrintedRange(rowenna),
                Is.EqualTo(RowennaEffectContent.FabledLancePrintedRange),
                "so the same set is derived again the moment she is back on it.");
        }

        /// <summary>
        /// The projection carries what the reading needs, which is what "the classification is
        /// published" means here: an owner's card instances and the match's definitions are both
        /// projected, so the same two queries the authority asks answer from projected state alone. No
        /// second record of the classification exists to drift from the first.
        /// </summary>
        [Test]
        public void ThePublishedCardsAndDefinitions_AnswerTheSameClassification()
        {
            var world = World.Create();
            world.Unlock();

            Assert.That(
                world.ClassificationFromProjectedStateAlone(RowennaCardContent.CodeOfChivalry),
                Is.EqualTo(AttackRangeKind.Ranged),
                "UltimateUnlockQuery.Derive reads the card's projected location and the overlay " +
                "reads the rule it supplies, so a reader holding only the projection reaches the " +
                "authority's own answer.");
        }

        // -------------------------------------------------------------------------------------------
        // One query, three seams - milestone 8.11a's rule applied to a rule that is not printed on the
        // attacking card
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The structural half of the offer/resolution/preview agreement. Milestone 8.11a's finding was
        /// that a preview computing its own verdict predicted a Block the Attack then refused; this
        /// declaration is the same hazard one level out, because the classification it changes is read
        /// in three places. Each of the three is asserted to reach the single query, so a board cannot
        /// make two of them disagree.
        /// </summary>
        [Test]
        public void TheOfferTheResolverAndTheDefencePreview_EachReadTheOneOverlayQuery()
        {
            var classify = ILConformanceScanner.Method(
                typeof(RangedAttackOverlayQuery), nameof(RangedAttackOverlayQuery.Classify));
            var overlay = ILConformanceScanner.Method(
                typeof(RangedAttackOverlayQuery), nameof(RangedAttackOverlayQuery.Overlay));
            var overlayPrintedRange = ILConformanceScanner.Method(
                typeof(RangedAttackOverlayQuery),
                nameof(RangedAttackOverlayQuery.OverlayPrintedRange));
            var blockEvaluate = ILConformanceScanner.Method(
                typeof(DefenseBlockQuery), nameof(DefenseBlockQuery.Evaluate));

            // One literal, not a concatenation: the living-document validator's reflected-member
            // check reads the string a test hands `GetType`, and a split literal reads as a type
            // named "GuardsDigital.Core.Rules.CardResolution." that no source declares.
            var backend = typeof(RuleModificationSet).Assembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend",
                throwOnError: true);

            Assert.That(ILConformanceScanner.References(new[] { backend }, classify), Is.True,
                "The Attack's own reach is chosen from the overlaid classification, or an offer " +
                "still measures adjacency while the Block calls the same attack Ranged.");
            Assert.That(ILConformanceScanner.References(new[] { backend }, overlay), Is.True,
                "and the target policy is overlaid at the one place the offer, the target choice " +
                "and the resolution all build their candidate set from.");
            Assert.That(
                ILConformanceScanner.References(new[] { typeof(RangeValueQuery) }, overlayPrintedRange),
                Is.True,
                "p.9's Range Total is the only place the printed icon is decided, so the override " +
                "lives there and p.16's item composes onto it.");

            Assert.That(
                ILConformanceScanner.References(new[] { typeof(DefenseBlockQuery) }, classify), Is.True,
                "p.14's Block condition asks the same classification question, rather than reading " +
                "the attacking card and reaching its own verdict.");
            Assert.That(ILConformanceScanner.References(new[] { backend }, blockEvaluate), Is.True,
                "The resolver answers a Block through that query...");
            Assert.That(
                ILConformanceScanner.References(new[] { typeof(MatchSession) }, blockEvaluate), Is.True,
                "...and so does the defence preview a defender chooses from, which is milestone " +
                "8.11a's whole rule.");
        }

        /// <summary>
        /// Milestone 8.53's scoreboard counts complete rulebook p.14 orders and asks nothing about a
        /// subtype, so nothing this declaration changes can move a column. Asserted rather than
        /// assumed: a counter that started reading the classification would count her attacks
        /// differently from everybody else's for a reason no printed text carries.
        /// </summary>
        [Test]
        public void TheScoreboardCountsOrdersRatherThanSubtypes()
        {
            Assert.That(
                ILConformanceScanner.References(
                    new[] { typeof(MatchScoreboard) },
                    ILConformanceScanner.Method(
                        typeof(RangedAttackOverlayQuery), nameof(RangedAttackOverlayQuery.Classify))),
                Is.False,
                "p.14's order is what an Attack column counts; the subtype decides who may be " +
                "targeted and what a Block does, and neither is a tally.");
        }

        // -------------------------------------------------------------------------------------------
        // The declaration's own refusals
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The family states a printed icon, and p.9's icons are positive. A declaration of zero would
        /// be an Attack called Ranged with no space to reach, which is neither the sentence nor p.13's
        /// adjacency - so it is refused where it is written rather than discovered by an offer that
        /// greys itself.
        /// </summary>
        [Test]
        public void TheDeclarationRefusesARangeNoPrintedIconCouldCarry()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RuleModification.ForAttackActionsCountAsRangedAtPrintedRange(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RuleModification.ForAttackActionsCountAsRangedAtPrintedRange(-1));
        }

        /// <summary>
        /// The card's printed icon and its ability's declared amount are one number read twice, and the
        /// shard's table is what both are transcribed from: <c>Ultimate / R 2</c>.
        /// </summary>
        [Test]
        public void TheCardPrintsTheSameRangeItsAbilityDeclares()
        {
            var card = RowennaCardContent.Definition(RowennaCardContent.FabledLance);

            Assert.That(card.Progression.IsUltimate, Is.True);
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.IV));
            Assert.That(card.ActionDefinitions, Is.Empty,
                "p.16's card prints an ability rather than an action, so she can neither play it " +
                "nor discard it to defend.");

            var rule = card.UltimateRule;
            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.RuleKind, Is.EqualTo(RowennaEffectContent.FabledLanceRuleKind));
            Assert.That(rule.PrintedValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(
                rule.PrintedValues.Single().Amount,
                Is.EqualTo(RowennaEffectContent.FabledLancePrintedRange));
            Assert.That(
                rule.Modifications.Single().Kind,
                Is.EqualTo(RuleModificationKind.AttackActionsCountAsRangedAtPrintedRange));
            Assert.That(
                rule.Modifications.Single().Amount,
                Is.EqualTo(RowennaEffectContent.FabledLancePrintedRange),
                "The icon and the rule are one printed number; two of them is how they disagree.");
        }

        // -------------------------------------------------------------------------------------------
        // The board these cases are asked on
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rowenna and one enemy hero on a synthetic hex board, her whole composed set in card
        /// instances, and Tigerclaw's two Block faces in the enemy's hand. Everything a case asks is a
        /// pure query over exactly these, so no case depends on anything being registered.
        /// </summary>
        private sealed class World
        {
            internal static readonly MinionId EnemyMinion = new MinionId("fabled-lance-enemy-minion");
            private static readonly MinionId FriendlyMinion =
                new MinionId("fabled-lance-friendly-minion");

            private static readonly MapId MapId = new MapId("rowenna-fabled-lance-map");
            private static readonly ZoneId Zone = new ZoneId("rowenna-fabled-lance-zone");

            private readonly MatchParticipants participants = StagedBoardParticipants.Create();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly BoardMapDefinition map;
            private HeroItemState heroItems;

            private World()
            {
                definitions = RowennaCardContent.AllDefinitions
                    .Concat(TigerclawCardContent.AllDefinitions)
                    .ToArray();
                map = BuildMap();
                heroItems = new HeroItemState(participants);

                // Her hand at p.6's shape: the two basics plus one coloured card per ladder. The three
                // coloured ones start at Tier I so that p.16's condition is genuinely unmet until this
                // fixture upgrades them, which is what makes the unlock a condition rather than a move.
                foreach (var cardId in new[]
                {
                    RowennaCardContent.CodeOfChivalry,
                    RowennaCardContent.ThrowTheGauntlet,
                    RowennaCardContent.TokenOfGratitude,
                    RowennaCardContent.StandGuard,
                    RowennaCardContent.CloseQuarters
                })
                {
                    instances.Add(CardInstance.CreateInHand(
                        Instance(Rowenna, cardId), Reference(cardId), Rowenna));
                }

                // p.6 returns everything else to the deck, the Tier IV card included. The faces the
                // cases build offers from live here; an offer names a card and an action rather than a
                // position, so nothing below depends on where these sit.
                foreach (var cardId in RowennaCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .Where(cardId => !instances.Any(card =>
                        card.OwnerId.Equals(Rowenna) &&
                        card.DefinitionReference.DefinitionId.Equals(cardId))))
                {
                    instances.Add(CardInstance.CreateInDeck(
                        Instance(Rowenna, cardId), Reference(cardId), Rowenna));
                }

                // The enemy seat holds the two Blocks that print both arms of p.14's condition, and a
                // copy of her Gold basic for the control case - the same printed face performed by
                // somebody the declaration is not about.
                foreach (var cardId in new[]
                {
                    TigerclawCardContent.Dodge,
                    TigerclawCardContent.Parry,
                    RowennaCardContent.CodeOfChivalry
                })
                {
                    instances.Add(CardInstance.CreateInHand(
                        Instance(Enemy, cardId), Reference(cardId), Enemy));
                }
            }

            internal static World Create() => new World();

            // ----- p.16's unlock -----

            internal CardInstance UltimateCard() => instances.Single(card =>
                card.OwnerId.Equals(Rowenna) &&
                card.DefinitionReference.DefinitionId.Equals(RowennaCardContent.FabledLance));

            internal UltimateUnlockResult TryUnlock() => UltimateUnlockQuery.TryUnlock(
                UltimateCard(),
                UltimateUnlockQuery.UpgradeCompletionReading(Rowenna),
                definitions,
                instances);

            /// <summary>
            /// p.15's Upgrade a Card step run to its end for her, following each card's own printed
            /// upgrade candidates rather than an id written here.
            /// </summary>
            internal void UpgradeHandToTierThree()
            {
                foreach (var card in instances
                    .Where(card => card.OwnerId.Equals(Rowenna) &&
                        card.State.Location == CardLocation.Hand)
                    .ToArray())
                {
                    var definition = DefinitionOf(card);
                    if (!definition.Progression.IsNonBasic)
                    {
                        continue;
                    }

                    while (definition.Progression.Tier != CardTier.III)
                    {
                        definition = CardCharacteristicQuery
                            .FindUpgradeCandidates(definition.Id, definitions)
                            .First();
                    }

                    instances[instances.IndexOf(card)] = new CardInstance(
                        card.Id, definition.Reference, card.OwnerId, card.State);
                }
            }

            internal void Unlock()
            {
                UpgradeHandToTierThree();
                Assert.That(TryUnlock().IsUnlocked, Is.True,
                    "Every case past this point is about what an unlocked Ultimate does.");
            }

            internal void EarnARangeItem() =>
                Assert.That(
                    heroItems.TryGrantItemFromTheBox(Rowenna, PrintedValueChannel.Range).IsPlaced,
                    Is.True,
                    "Paragon of Grace's printed item is a Range item, and p.16's fourth dashboard " +
                    "slot accepts one.");

            // ----- the live set -----

            internal RuleModificationSet Modifications() => Build(Board(Adjacent));

            internal RuleModificationSet ModificationsWithHeroOffTheBoard() =>
                Build(new AdjacentMoveAuthority(
                    map,
                    new[] { new HeroOccupancy(Enemy, Adjacent) },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>()));

            private RuleModificationSet Build(AdjacentMoveAuthority board) =>
                RuleModificationQuery.Build(
                    null,
                    board,
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    UltimateUnlockQuery.Derive(definitions, instances),
                    RuleObject.None,
                    heroItems,
                    null);

            // ----- the three readings, composed exactly as the backend composes them -----

            internal AttackRangeKind ClassificationOf(CardDefinitionId cardId) =>
                RangedAttackOverlayQuery.Classify(
                    AttackOffer(Rowenna, cardId).AttackRange, Rowenna, Modifications());

            internal int RangeTotalOf(CardDefinitionId cardId) => RangeValueQuery
                .Calculate(AttackOffer(Rowenna, cardId), definitions, heroItems, Modifications())
                .Total;

            internal int SkillRangeTotalOf(CardDefinitionId cardId) => RangeValueQuery
                .Calculate(SkillOffer(cardId), definitions, heroItems, Modifications())
                .Total;

            internal int RadiusTotalOf(CardDefinitionId cardId) => RadiusValueQuery
                .Calculate(AttackOffer(Rowenna, cardId), definitions, heroItems)
                .Total;

            internal IReadOnlyList<HeroId> TargetsOf(
                CardDefinitionId cardId,
                HexCoordinate enemyAt,
                ActionInvocationContext context = ActionInvocationContext.PrintedPrimary) =>
                LegalTargets(Rowenna, cardId, Board(enemyAt), context)
                    .Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId)
                    .ToArray();

            internal IReadOnlyList<MinionId> MinionTargetsOf(
                CardDefinitionId cardId, HexCoordinate minionAt) =>
                LegalTargets(
                        Rowenna,
                        cardId,
                        Board(FarAway, Minion(EnemyMinion, TeamSide.Red, minionAt)))
                    .Where(target => target.Kind == AttackTargetKind.Minion)
                    .Select(target => target.MinionId)
                    .ToArray();

            internal IReadOnlyList<MinionId> TargetsOfWithFriendlyMinion(
                CardDefinitionId cardId, HexCoordinate enemyAt, HexCoordinate friendlyAt) =>
                LegalTargets(
                        Rowenna,
                        cardId,
                        Board(FarAway, Minion(FriendlyMinion, TeamSide.Blue, friendlyAt)))
                    .Where(target => target.Kind == AttackTargetKind.Minion)
                    .Select(target => target.MinionId)
                    .ToArray();

            internal AttackRangeKind EnemyClassification() =>
                RangedAttackOverlayQuery.Classify(
                    AttackOffer(Enemy, RowennaCardContent.CodeOfChivalry).AttackRange,
                    Enemy,
                    Modifications());

            internal int EnemyRangeTotal() => RangeValueQuery
                .Calculate(
                    AttackOffer(Enemy, RowennaCardContent.CodeOfChivalry),
                    definitions,
                    heroItems,
                    Modifications())
                .Total;

            internal IReadOnlyList<HeroId> EnemyTargets(HexCoordinate rowennaAt)
            {
                var board = new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Enemy, RowennaAt),
                        new HeroOccupancy(Rowenna, rowennaAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                return LegalTargets(Enemy, RowennaCardContent.CodeOfChivalry, board)
                    .Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId)
                    .ToArray();
            }

            internal DefenseBlockResult BlockVerdict(
                CardDefinitionId blockCardId, CardDefinitionId attackCardId) =>
                Verdict(blockCardId, attackCardId, Rowenna);

            internal DefenseBlockResult BlockVerdictAgainstTheEnemysAttack(
                CardDefinitionId blockCardId) =>
                Verdict(blockCardId, RowennaCardContent.CodeOfChivalry, Enemy);

            /// <summary>
            /// The same classification reached from nothing but what a projection publishes: the
            /// match's definitions and the owner's card instances. No third record exists.
            /// </summary>
            internal AttackRangeKind ClassificationFromProjectedStateAlone(CardDefinitionId cardId)
            {
                var projectedDefinitions = definitions.ToArray();
                var projectedCards = instances.ToArray();
                var modifications = RuleModificationQuery.Build(
                    null,
                    Board(Adjacent),
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    UltimateUnlockQuery.Derive(projectedDefinitions, projectedCards),
                    RuleObject.None,
                    null,
                    null);
                return RangedAttackOverlayQuery.Classify(
                    AttackOffer(Rowenna, cardId).AttackRange, Rowenna, modifications);
            }

            // ----- composition -----

            private DefenseBlockResult Verdict(
                CardDefinitionId blockCardId, CardDefinitionId attackCardId, HeroId attackerId) =>
                DefenseBlockQuery.Evaluate(
                    new ActionOffer(
                        Instance(Enemy, blockCardId),
                        Reference(blockCardId),
                        TigerclawCardContent.PrimaryBlock,
                        attackerId.Equals(Rowenna) ? Enemy : Rowenna,
                        ActionInvocationContext.PrintedPrimary,
                        true),
                    definitions,
                    Reference(attackCardId),
                    RowennaCardContent.PrimaryAttack,
                    new GuardsDigital.Core.Components.MarkerAttachmentRegister(),
                    attackerId,
                    Modifications());

            /// <summary>
            /// The backend's own composition: the card's printed policy, then the overlay that
            /// replaces its adjacency clause, then p.13's ceiling read off the composed Range Total.
            /// </summary>
            private IReadOnlyList<AttackTarget> LegalTargets(
                HeroId attackerId,
                CardDefinitionId cardId,
                AdjacentMoveAuthority board,
                ActionInvocationContext context = ActionInvocationContext.PrintedPrimary)
            {
                var modifications = Build(board);
                var offer = AttackOffer(attackerId, cardId, context);
                var action = definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(candidate => candidate.Id.Equals(offer.ActionDefinitionId));
                var classification = RangedAttackOverlayQuery.Classify(
                    offer.AttackRange, attackerId, modifications);
                var rangeTotal = classification == AttackRangeKind.Ranged
                    ? RangeValueQuery.Calculate(offer, definitions, heroItems, modifications).Total
                    : 1;
                return AttackTargetQuery.FindLegalEnemyTargets(
                    attackerId,
                    RangedAttackOverlayQuery.Overlay(
                        action.AttackTargetPolicy ??
                            PrintedAttackTargetPolicy.ForClassification(offer.AttackRange),
                        offer.AttackRange,
                        attackerId,
                        modifications),
                    rangeTotal,
                    participants,
                    board,
                    modifications);
            }

            private ActionOffer AttackOffer(
                HeroId actorId,
                CardDefinitionId cardId,
                ActionInvocationContext context = ActionInvocationContext.PrintedPrimary) =>
                new ActionOffer(
                    Instance(actorId, cardId),
                    Reference(cardId),
                    RowennaCardContent.PrimaryAttack,
                    actorId,
                    context,
                    true,
                    AttackRangeKind.NonRanged,
                    default);

            private ActionOffer SkillOffer(CardDefinitionId cardId) =>
                new ActionOffer(
                    Instance(Rowenna, cardId),
                    Reference(cardId),
                    cardId.Equals(RowennaCardContent.ThrowTheGauntlet)
                        ? RowennaCardContent.PrimaryBasicSkill
                        : RowennaCardContent.PrimarySkill,
                    Rowenna,
                    ActionInvocationContext.PrintedPrimary,
                    true);

            private AdjacentMoveAuthority Board(
                HexCoordinate enemyAt, params QuickGameMinionSetupState[] minions) =>
                new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Enemy, enemyAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    minions);

            /// <summary>Five spaces off, where the enemy hero is in nobody's way.</summary>
            private static readonly HexCoordinate FarAway = new HexCoordinate(5, 0);

            private QuickGameMinionSetupState Minion(
                MinionId minionId, TeamSide side, HexCoordinate at) =>
                new QuickGameMinionSetupState(
                    minionId,
                    new TeamId(side == TeamSide.Red
                        ? StagedBoardParticipants.RedTeamIdValue
                        : StagedBoardParticipants.BlueTeamIdValue),
                    side,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(
                        MapId, new SpawnPointId("fabled-lance-spawn")),
                    new ConfiguredSpaceReference(MapId, SpaceIdAt(at)),
                    at);

            private CardDefinition DefinitionOf(CardInstance card) => definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference));

            private CardDefinitionReference Reference(CardDefinitionId cardId) => definitions
                .Single(definition => definition.Id.Equals(cardId))
                .Reference;

            private static CardInstanceId Instance(HeroId ownerId, CardDefinitionId cardId) =>
                new CardInstanceId($"{ownerId.Value}-{cardId.Value}");

            private static SpaceId SpaceIdAt(HexCoordinate at) =>
                new SpaceId($"q{at.Q}_r{at.R}");

            private static BoardMapDefinition BuildMap()
            {
                var spaces = new List<BoardMapSpace>();
                for (var q = -6; q <= 6; q++)
                {
                    for (var r = -6; r <= 6; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, RowennaAt) > 6)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            SpaceIdAt(coordinate), coordinate, BoardMapSpaceKind.Playable, Zone));
                    }
                }

                return new BoardMapDefinition(
                    MapId, 1, "Rowenna Fabled Lance Map",
                    spaces,
                    new[] { new BoardMapZone(Zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());
            }
        }
    }
}
