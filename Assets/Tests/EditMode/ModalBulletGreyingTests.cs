using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.70. A modal Attack bullet whose own printed sentence names no legal unit on the
    /// board as it stands is GREYED at the modal choice, keeping its label and its place and carrying
    /// no way to select it - the user's ruling on BR-Q19h, "I think we should gray it out if no legal
    /// targets", which reverses milestone 8.35z27's reading for the choice and for the choice alone.
    ///
    /// <para><b>Every face on the <c>attack-sequence@1</c> contract is measured here, and there are
    /// seven rather than the roadmap's six.</b> Brynn's Familiar Ground, Bain's Hand Crossbow and
    /// Hunter-Seeker, Dodger's Middlefinger of Death, Whisper's Swift Justice - and Tali's Spirit Wolf
    /// and Spirit Bear, which compose the same modal and which the roadmap block does not name. Each is
    /// measured on three boards: one bullet empty, both empty, and both live. Bain's "Bounty Hunter" is
    /// his epithet rather than a card, and Brynn's Home Turf composes no body at all, so neither is a
    /// consumer.</para>
    ///
    /// <para><b>Why the derivation is asked twice in each case.</b> The offer half is read off the
    /// pause the resolver actually publishes - <c>PendingEffectChoice.UnavailableCandidates</c>, which
    /// is what the decision window draws and what decides whether a Select invocation exists at all -
    /// and the authority half is read off
    /// <see cref="AttackSequenceBulletSubjectQuery"/>, the one owner both the choice and the chosen
    /// bullet's own p.14 sequence go through. Asserting both on one board is what proves they agree by
    /// construction rather than by coincidence.</para>
    ///
    /// <para><b>ONE SHAPE ON THIS CONTRACT IS DELIBERATELY NOT GREYED, and it is recorded rather than
    /// fixed.</b> Tali's ranged bullet is composed INSIDE a <see cref="EffectNodeKind.Conditional"/>
    /// ("if this card is not in the discard"), and the option-feasibility walk has never let a
    /// Conditional report <c>Blocked</c> - "an unevaluated predicate could have taken the harmless
    /// path" - so an arm behind one stays offered however empty its bullet is. That is a property of
    /// the shared walk rather than of this contract, it predates this milestone, and changing it would
    /// reach every conditional branch of every card in the build. The case below measures the position
    /// so a later milestone finds it stated.</para>
    /// </summary>
    public sealed class ModalBulletGreyingTests
    {
        // -------------------------------------------------------------------------------------------
        // Brynn - Familiar Ground: "a unit adjacent to you" / "a hero in range adjacent to 3+ obstacles"
        // -------------------------------------------------------------------------------------------

        [Test]
        public void FamiliarGround_GreysTheObstacleBullet_WhereNoHeroInRangeIsBoxedIn()
        {
            var board = ModalBoard.ForBrynnFamiliarGround(boxTheEnemyIn: false, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.EqualTo(new[]
                {
                    BrynnEffectContent.FamiliarGroundObstacleBulletNode
                }),
                "\"Target a hero in range who is adjacent to 3 or more obstacles\" names nobody on a " +
                "board where the only enemy hero stands in the open, so the bullet is greyed rather " +
                "than offered, taken and spent on nothing.");
            Assert.That(board.OfferedBullets(), Is.EqualTo(new[]
                {
                    BrynnEffectContent.FamiliarGroundAdjacentBulletNode
                }),
                "and the bullet that does name somebody is still the answer the player gives.");
        }

        [Test]
        public void FamiliarGround_GreysTheAdjacentBullet_WhereNothingStandsInHisRing()
        {
            var board = ModalBoard.ForBrynnFamiliarGround(boxTheEnemyIn: true, standTheEnemyBeside: false);

            Assert.That(board.GreyedBullets(), Is.EqualTo(new[]
                {
                    BrynnEffectContent.FamiliarGroundAdjacentBulletNode
                }),
                "p.19's innermost ring is empty, so \"Target a unit adjacent to you\" names nobody.");
            Assert.That(board.OfferedBullets(), Is.EqualTo(new[]
                {
                    BrynnEffectContent.FamiliarGroundObstacleBulletNode
                }),
                "and the boxed-in hero keeps the narrowed bullet offered - the exact inverse of the " +
                "case above, on one face, which is what makes each grey about its own sentence.");
        }

        [Test]
        public void FamiliarGround_GreysBothBullets_WhereNeitherSentenceNamesAnybody()
        {
            var board = ModalBoard.ForBrynnFamiliarGround(
                boxTheEnemyIn: false, standTheEnemyBeside: false);

            Assert.That(board.GreyedBullets(), Has.Length.EqualTo(2),
                "Both sentences name nobody, so the modal stands with everything greyed - which is " +
                "milestone 6.5ae's stop, and the position milestone 8.69g already greys the whole " +
                "printed primary on.");
            Assert.That(board.Choice().StopInvocation, Is.Not.Null,
                "and p.10's stop is the one command left, so the player closes a decision that has " +
                "nothing in it rather than the resolver closing it for them.");
            Assert.That(board.Choice().SelectInvocations, Is.Empty);
        }

        [Test]
        public void FamiliarGround_OffersBothBullets_WhereBothSentencesNameSomebody()
        {
            var board = ModalBoard.ForBrynnFamiliarGround(boxTheEnemyIn: true, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.Empty);
            Assert.That(board.OfferedBullets(), Has.Length.EqualTo(2),
                "Nothing is greyed while both sentences name a unit, so this milestone takes no " +
                "answer away that the board supports.");
        }

        [Test]
        public void FamiliarGround_ReadsOverTheTopsOverlayLikeTheSequenceDoes()
        {
            var board = ModalBoard.ForBrynnFamiliarGround(
                boxTheEnemyIn: false,
                standTheEnemyBeside: false,
                modifications: OverTheTop());

            Assert.That(board.GreyedBullets(), Is.EqualTo(new[]
                {
                    BrynnEffectContent.FamiliarGroundAdjacentBulletNode
                }),
                "His Ultimate declares that enemy heroes COUNT AS adjacent to three or more obstacles " +
                "(p.17), and the greying asks the one owner of that phrase - so the same board that " +
                "greys the narrowed bullet without the overlay offers it with one. The adjacent " +
                "bullet is untouched, because nothing stands in his ring either way.");
        }

        [Test]
        public void FamiliarGround_CountsImmunityAsNoLegalUnit()
        {
            var board = ModalBoard.ForBrynnFamiliarGround(
                boxTheEnemyIn: true,
                standTheEnemyBeside: true,
                modifications: ImmuneToAttacks(ModalBoard.EnemyId));

            Assert.That(board.GreyedBullets(), Has.Length.EqualTo(2),
                "p.18's immunity is read through the same TargetQuery the sequence reads it through, " +
                "so a hero nothing may attack is not a legal unit for either sentence - and a board " +
                "whose only candidate is immune greys exactly as an empty one does.");
        }

        // -------------------------------------------------------------------------------------------
        // Bain - Hand Crossbow (1..1) and Hunter-Seeker (1..2): the marker narrowing
        // -------------------------------------------------------------------------------------------

        [TestCase(true, TestName = "HandCrossbow_GreysTheMarkerBullet_WithNoBountyOut")]
        [TestCase(false, TestName = "HunterSeeker_GreysTheMarkerBullet_WithNoBountyOut")]
        public void TheMarkerBullet_IsGreyedWithNoBountyOut(bool oneBulletOnly)
        {
            var board = ModalBoard.ForBainMarkerModal(
                oneBulletOnly, armTheBounty: false, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.EqualTo(new[] { "crossbow-marker-bullet" }),
                "\"Target a hero in range with a bounty marker\" names nobody while no bounty is out.");
            Assert.That(board.OfferedBullets(), Is.EqualTo(new[] { "crossbow-adjacent-bullet" }));
        }

        [Test]
        public void HunterSeeker_KeepsItsOneOrBothCeiling_WithOnlyOneLiveArm()
        {
            var board = ModalBoard.ForBainMarkerModal(
                oneBulletOnly: false, armTheBounty: false, standTheEnemyBeside: true);
            var choice = board.Choice();

            Assert.That(choice.MaximumSelections, Is.EqualTo(2),
                "\"Choose one, or both\" is the card's printed cardinality and a greyed bullet is not " +
                "a change to what the card says - the ceiling is content and the grey is a board " +
                "reading.");
            Assert.That(choice.SelectInvocations, Has.Count.EqualTo(1),
                "but only the live arm may actually be named, so the one answer the board supports is " +
                "the one the player can give.");
            Assert.That(choice.StopInvocation, Is.Null,
                "and there is no stop, because a decision with an answer left in it is a decision.");
        }

        [Test]
        public void TheMarkerBullet_IsOfferedOnceTheBountyIsOnAHeroInRange()
        {
            var board = ModalBoard.ForBainMarkerModal(
                oneBulletOnly: false, armTheBounty: true, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.Empty);
            Assert.That(board.Choice().SelectInvocations, Has.Count.EqualTo(2),
                "One marker is the whole difference between this board and the one above.");
        }

        [Test]
        public void TheMarkerModal_GreysBothBullets_WithNobodyInReachAtAll()
        {
            var board = ModalBoard.ForBainMarkerModal(
                oneBulletOnly: true, armTheBounty: false, standTheEnemyBeside: false);

            Assert.That(board.GreyedBullets(), Has.Length.EqualTo(2));
            Assert.That(board.Choice().StopInvocation, Is.Not.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Dodger - Middlefinger of Death: the discard narrowing
        // -------------------------------------------------------------------------------------------

        [Test]
        public void MiddlefingerOfDeath_GreysItsPileBullet_WhileEveryDiscardInRangeIsEmpty()
        {
            var board = ModalBoard.ForMiddlefinger(fillTheEnemyPile: false, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.EqualTo(new[] { "middlefinger-discard" }),
                "\"Target a hero in range WHO HAS one or more cards in the discard\" names nobody " +
                "while nobody has discarded anything - the same difference milestone 8.35z45 measured " +
                "as an empty candidate set, now read one step earlier.");
            Assert.That(board.OfferedBullets(), Is.EqualTo(new[] { "middlefinger-adjacent" }));
        }

        [Test]
        public void MiddlefingerOfDeath_OffersBothBullets_OnceAHeroInRangeHasAPile()
        {
            var board = ModalBoard.ForMiddlefinger(fillTheEnemyPile: true, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.Empty);
            Assert.That(board.Choice().MaximumSelections, Is.EqualTo(2));
            Assert.That(board.Choice().SelectInvocations, Has.Count.EqualTo(2),
                "and \"one, or both, on different targets\" really is answerable both ways on a board " +
                "that supports both sentences.");
        }

        [Test]
        public void MiddlefingerOfDeath_GreysBothBullets_WithNobodyInReach()
        {
            var board = ModalBoard.ForMiddlefinger(fillTheEnemyPile: true, standTheEnemyBeside: false);

            Assert.That(board.GreyedBullets(), Has.Length.EqualTo(2),
                "A pile nobody can be reached to attack is not a subject: the narrowing narrows what " +
                "p.13 already admitted, and p.13 admits nobody here.");
        }

        // -------------------------------------------------------------------------------------------
        // Whisper - Swift Justice: the EMPTY-discard narrowing, the negation of Dodger's
        // -------------------------------------------------------------------------------------------

        [Test]
        public void SwiftJustice_GreysItsRangedBullet_OnceEveryHeroInRangeHasAPile()
        {
            var board = ModalBoard.ForSwiftJustice(fillTheEnemyPile: true, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.EqualTo(new[] { "modal-ranged-hero" }),
                "\"Target a hero in range with an EMPTY discard\" is the exact negation of " +
                "Middlefinger's clause, and the board that keeps his bullet offered is the board that " +
                "greys hers.");
            Assert.That(board.OfferedBullets(), Is.EqualTo(new[] { "modal-adjacent-unit" }));
        }

        [Test]
        public void SwiftJustice_OffersBothBullets_WhileTheHeroInRangeHasDiscardedNothing()
        {
            var board = ModalBoard.ForSwiftJustice(fillTheEnemyPile: false, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.Empty);
            Assert.That(board.Choice().SelectInvocations, Has.Count.EqualTo(2));
        }

        [Test]
        public void SwiftJustice_GreysBothBullets_WithNobodyInReach()
        {
            var board = ModalBoard.ForSwiftJustice(fillTheEnemyPile: false, standTheEnemyBeside: false);

            Assert.That(board.GreyedBullets(), Has.Length.EqualTo(2));
        }

        // -------------------------------------------------------------------------------------------
        // Tali - Spirit Wolf and Spirit Bear, and the one arm a Conditional keeps offered
        // -------------------------------------------------------------------------------------------

        [TestCase(true, TestName = "SpiritWolf_GreysItsAdjacentBullet_WithNothingInHerRing")]
        [TestCase(false, TestName = "SpiritBear_GreysItsAdjacentBullet_WithNothingInHerRing")]
        public void TheSpiritModal_GreysItsAdjacentBullet_WithNothingInHerRing(bool oneBulletOnly)
        {
            var board = ModalBoard.ForSpiritModal(oneBulletOnly, standTheEnemyBeside: false);

            // Milestone 9.6. The walk used to leave an arm behind a Conditional offered however empty
            // its bullet was, because it never evaluated the predicate. "If this card is not in the
            // discard" is now read - the card is not - so the ranged arm is judged by its bullet, and
            // with the enemy five spaces off (past every printed Range) it names nobody either.
            Assert.That(board.GreyedBullets(),
                Is.EquivalentTo(new[] { "spirit-adjacent-hero", "spirit-not-in-discard-gate" }),
                "\"Target a hero adjacent to you\" names nobody with the enemy five spaces off, and " +
                "neither does \"target a unit in range\".");
            Assert.That(board.OfferedBullets(), Is.Empty);
        }

        [Test]
        public void TheSpiritModal_OffersBothArms_WithAHeroBesideHer()
        {
            var board = ModalBoard.ForSpiritModal(oneBulletOnly: true, standTheEnemyBeside: true);

            Assert.That(board.GreyedBullets(), Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // The answer, and the authority the offer is read from
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnAnswerNamingAGreyedBullet_IsRefused_AndLeavesThePauseStanding()
        {
            var board = ModalBoard.ForMiddlefinger(fillTheEnemyPile: false, standTheEnemyBeside: true);
            var paused = board.Start();
            var greyed = paused.Frame.PendingChoice.UnavailableCandidates.Single();

            var answered = EffectResolutionSession.Answer(
                paused.Frame,
                new EffectChoiceCommand(
                    paused.Frame.PendingChoice.ResolutionId,
                    paused.Frame.PendingChoice.ChoiceId,
                    EffectChoiceCommandKind.Select,
                    greyed,
                    paused.Frame.PendingChoice.ChooserId),
                board.Context());

            Assert.That(answered.Accepted, Is.False,
                "A greyed bullet carries no Select invocation, so naming it is an answer the " +
                "resolution refuses outright rather than one it quietly accepts.");
            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates, Has.Count.EqualTo(1),
                "and the decision is still there to be made.");
        }

        [Test]
        public void TheOfferAndTheAuthority_AnswerTheSameSetOnOneBoard()
        {
            foreach (var board in new[]
            {
                ModalBoard.ForBrynnFamiliarGround(boxTheEnemyIn: false, standTheEnemyBeside: true),
                ModalBoard.ForBrynnFamiliarGround(boxTheEnemyIn: true, standTheEnemyBeside: false),
                ModalBoard.ForBainMarkerModal(false, armTheBounty: false, standTheEnemyBeside: true),
                ModalBoard.ForMiddlefinger(fillTheEnemyPile: false, standTheEnemyBeside: true),
                ModalBoard.ForSwiftJustice(fillTheEnemyPile: true, standTheEnemyBeside: true)
            })
            {
                Assert.That(board.GreyedBullets(), Is.EqualTo(board.BulletsTheAuthorityRefuses()),
                    "The greyed set the pause publishes - which is what the decision window draws and " +
                    "what decides whether an answer exists - is the set the one owner of the question " +
                    "refuses. They agree because neither holds a derivation of its own.");
            }
        }

        [Test]
        public void TheWholePrimary_IsStillGreyedOnlyWhenEveryBulletIsEmpty()
        {
            var oneLive = ModalBoard.ForMiddlefinger(
                fillTheEnemyPile: false, standTheEnemyBeside: true);
            var noneLive = ModalBoard.ForMiddlefinger(
                fillTheEnemyPile: true, standTheEnemyBeside: false);

            Assert.That(oneLive.BulletsTheAuthorityRefuses(), Has.Length.EqualTo(1));
            Assert.That(noneLive.BulletsTheAuthorityRefuses(), Has.Length.EqualTo(2),
                "\"Any bullet\" is exactly \"each bullet, and at least one\", so the action tier and " +
                "the bullet tier are one reading counted twice - which is why milestone 8.35z27's " +
                "whole-primary grey needed no second rule when this one arrived.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static RuleModificationSet OverTheTop() =>
            Standing(
                RuleModification.ForEnemyHeroesCountAsAdjacentToThreeOrMoreObstacles(),
                ModalBoard.EnemyId,
                "over-the-top-probe");

        private static RuleModificationSet ImmuneToAttacks(HeroId hero) =>
            Standing(RuleModification.ForImmunity(RuleActivity.All), hero, "immunity-probe");

        private static RuleModificationSet Standing(
            RuleModification modification, HeroId affected, string id) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromContinuousSource(
                        modification,
                        RuleObject.ForHero(affected),
                        new ContinuousRuleSource(
                            id, ContinuousRuleSourceKind.Ultimate, affected, "probe",
                            UltimateRuleDefinition.ApplicabilityKind,
                            new CardInstanceId($"{id}-card")),
                        TeamSide.Blue,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>
        /// One featureless disk, one acting hero holding one real printed face, and the units its two
        /// bullets are about. Everything a bullet's sentence can read - the ring, the Range, the marker
        /// register, the discard piles and the standing rules - is a knob here, so each case moves one
        /// of them and nothing else.
        /// </summary>
        private sealed class ModalBoard
        {
            public const string ActorIdValue = StagedBoardParticipants.RedHeroIdValue;

            public static HeroId EnemyId { get; } =
                new HeroId(StagedBoardParticipants.BlueHeroIdValue);

            private static readonly CardInstanceId FaceInstance =
                new CardInstanceId("modal-greying-face");

            private readonly CardDefinition card;
            private readonly ActionDefinition action;
            private readonly EffectDefinition modal;
            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly MarkerAttachmentRegister markers = new MarkerAttachmentRegister();
            private readonly MatchComponents components;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly RuleModificationSet modifications;
            private readonly HeroItemState heroItems;

            private ModalBoard(
                CardDefinition card,
                IReadOnlyList<HeroOccupancy> occupancies,
                bool fillTheEnemyPile,
                bool armTheBounty,
                RuleModificationSet modifications)
            {
                this.card = card;
                this.modifications = modifications ?? RuleModificationSet.Empty;
                participants = StagedBoardParticipants.Create();
                Actor = new HeroId(ActorIdValue);
                action = card.ActionDefinitions.Single(value => value.Kind == ActionKind.Attack);
                modal = action.CardTextAt(CardTextTiming.BeforeTheAttack);
                Assert.That(modal, Is.Not.Null,
                    $"'{card.Reference.DefinitionId.Value}' prints its modal as before-the-attack text.");

                board = new AdjacentMoveAuthority(FeaturelessDisk(6), occupancies);
                var catalogs = MatchContentCatalogs.ForThisBuild();
                components = new MatchComponents(
                    catalogs.Components, board, markers, board, board, board, board);

                var pile = WhisperCardContent.Definition(WhisperCardContent.BloodFrenzy);
                var face = CardInstance.CreateInHand(FaceInstance, card.Reference, Actor);
                var enemyCard = CardInstance.CreateInHand(
                    new CardInstanceId("modal-greying-enemy-card"), pile.Reference, EnemyId);
                if (fillTheEnemyPile)
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(enemyCard), Is.True,
                        "Fixture precondition: the hero in range really does have a pile.");
                }

                definitions = new[] { card, pile }
                    .GroupBy(value => value.Reference)
                    .Select(group => group.First())
                    .ToArray();
                instances = new[] { face, enemyCard };
                heroItems = new HeroItemState(participants);

                if (armTheBounty)
                {
                    markers.Give(new MarkerAttachment(
                        new MarkerInstanceId("modal-greying-bounty"),
                        BainComponentContent.BountyDefinition,
                        Actor,
                        EnemyId,
                        FaceInstance,
                        action.Id));
                }
            }

            public HeroId Actor { get; }

            /// <summary>
            /// Brynn's Familiar Ground, on a radius-6 disk.
            ///
            /// <para><paramref name="boxTheEnemyIn"/> stands the enemy hero on the map's own edge,
            /// where three of his six neighbours are off the map - and p.18 counts the void past the
            /// edge as an obstacle, which is milestone 8.69h's measured "the water ring alone boxes in
            /// a hero standing on the edge". <paramref name="standTheEnemyBeside"/> puts Brynn in that
            /// hero's ring, which is the other bullet's whole sentence. The two knobs are independent,
            /// so each case moves one of them.</para>
            /// </summary>
            public static ModalBoard ForBrynnFamiliarGround(
                bool boxTheEnemyIn,
                bool standTheEnemyBeside,
                RuleModificationSet modifications = null)
            {
                var enemySpace = boxTheEnemyIn
                    ? new HexCoordinate(6, 0)
                    : new HexCoordinate(3, 0);
                var brynnSpace = standTheEnemyBeside
                    ? new HexCoordinate(enemySpace.Q - 1, enemySpace.R)
                    : new HexCoordinate(enemySpace.Q - 2, enemySpace.R);
                return new ModalBoard(
                    BrynnCardContent.Definition(BrynnCardContent.FamiliarGround),
                    new[]
                    {
                        new HeroOccupancy(new HeroId(ActorIdValue), brynnSpace),
                        new HeroOccupancy(EnemyId, enemySpace)
                    },
                    fillTheEnemyPile: false,
                    armTheBounty: false,
                    modifications: modifications);
            }

            public static ModalBoard ForBainMarkerModal(
                bool oneBulletOnly, bool armTheBounty, bool standTheEnemyBeside) =>
                Simple(
                    BainCardContent.Definition(
                        oneBulletOnly ? BainCardContent.HandCrossbow : BainCardContent.HunterSeeker),
                    standTheEnemyBeside,
                    fillTheEnemyPile: false,
                    armTheBounty: armTheBounty);

            public static ModalBoard ForMiddlefinger(
                bool fillTheEnemyPile, bool standTheEnemyBeside) =>
                Simple(
                    DodgerCardContent.Definition(DodgerCardContent.MiddlefingerOfDeath),
                    standTheEnemyBeside,
                    fillTheEnemyPile,
                    armTheBounty: false);

            public static ModalBoard ForSwiftJustice(
                bool fillTheEnemyPile, bool standTheEnemyBeside) =>
                Simple(
                    WhisperCardContent.Definition(WhisperCardContent.SwiftJustice),
                    standTheEnemyBeside,
                    fillTheEnemyPile,
                    armTheBounty: false);

            public static ModalBoard ForSpiritModal(bool oneBulletOnly, bool standTheEnemyBeside) =>
                Simple(
                    TaliCardContent.Definition(
                        oneBulletOnly ? TaliCardContent.SpiritWolf : TaliCardContent.SpiritBear),
                    standTheEnemyBeside,
                    fillTheEnemyPile: false,
                    armTheBounty: false);

            /// <summary>
            /// The one enemy hero either in the actor's own ring, or five spaces off - which is past
            /// every printed Range in this build, so "not beside" is also "not in range" and the board
            /// that greys one bullet for its clause greys both for p.13.
            /// </summary>
            private static ModalBoard Simple(
                CardDefinition card,
                bool standTheEnemyBeside,
                bool fillTheEnemyPile,
                bool armTheBounty) =>
                new ModalBoard(
                    card,
                    new[]
                    {
                        new HeroOccupancy(new HeroId(ActorIdValue), new HexCoordinate(0, 0)),
                        new HeroOccupancy(
                            EnemyId,
                            standTheEnemyBeside
                                ? new HexCoordinate(1, 0)
                                : new HexCoordinate(5, 0))
                    },
                    fillTheEnemyPile,
                    armTheBounty,
                    modifications: null);

            public EffectResolutionContext Context() =>
                new EffectResolutionContext(
                    modal,
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    new ActiveEffectLifecycle(),
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Attack,
                    components: components,
                    heroItems: heroItems,
                    rosterState: EffectRosterState.Absent);

            public EffectResolutionStepResult Start()
            {
                var paused = EffectResolutionSession.Start(
                    new EffectResolutionId("modal-greying-probe"),
                    FaceInstance,
                    card.Reference,
                    action.Id,
                    Actor,
                    Actor,
                    ActionInvocationContext.PrintedPrimary,
                    Context());
                Assert.That(paused.Frame.PendingChoice, Is.Not.Null,
                    "Fixture precondition: the modal really is what the text pauses on.");
                return paused;
            }

            public PendingEffectChoice Choice() => Start().Frame.PendingChoice;

            public string[] GreyedBullets() => Choice().UnavailableCandidates
                .Select(candidate => candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            public string[] OfferedBullets() => Choice().SelectInvocations
                .Select(invocation => invocation.Candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            /// <summary>
            /// The same question put to the one owner rather than to the pause: every declared bullet
            /// of this face whose own sentence names nobody right now.
            /// </summary>
            public string[] BulletsTheAuthorityRefuses()
            {
                var offer = new ActionOffer(
                    FaceInstance,
                    card.Reference,
                    action.Id,
                    Actor,
                    ActionInvocationContext.PrintedPrimary,
                    isAvailable: true,
                    attackRange: action.AttackRange,
                    sourceCardStateTransition: default);
                return RequestedAttackSequenceQuery.Declared(modal)
                    .Where(bullet => !AttackSequenceBulletSubjectQuery.HasALegalSubject(
                        Actor, offer, bullet, definitions, instances, participants, board, board,
                        markers, heroItems, EffectRosterState.Absent, modifications))
                    .Select(bullet => bullet.BulletId)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
            }

            private static HexCoordinate[] FeaturelessDisk(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates.ToArray();
            }
        }
    }
}
