using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Whisper's printed card text, composed as typed effect definitions. Milestone
    /// <c>8.35z33</c>.
    ///
    /// <para>Every sentence below is transcribed in <see cref="WhisperCardContent"/>. What this file
    /// adds is the composition: which nodes each sentence becomes, in the order the face prints them,
    /// over contracts that already exist wherever one does. Nothing here parses printed text, names a
    /// hero in a rule, or reads a card by name at runtime.</para>
    ///
    /// <para><b>Two of her sentences hand a decision to somebody who is not the actor</b>, and they do
    /// it in two different ways. Lesser Evil and Greater Good put a <em>modal</em> to an enemy hero
    /// Whisper picked, which is <see cref="EffectChooserRole.BoundHero"/>; Swift Justice makes its
    /// target <em>perform</em> a move, which is <c>defended-card-forced-move@1</c>. Rulebook p.10
    /// sanctions the first in as many words and the 2026-08-19 designer answer settles the second: the
    /// hero's own player steers.</para>
    ///
    /// <para><b>Her Red line's three step-1 movers compose the seam milestone 8.19a built and
    /// milestone 8.35z25 corrected</b> - move, then an actor-origin adjacency selection, then
    /// <c>set-attack-target@1</c> - because all three print the move BEFORE "Target a unit adjacent to
    /// you." Under the 2026-09-06 rider the move is a card-text direct Move and not a Movement action:
    /// movement-action restrictions do not block it and movement-action buffs do not widen it.</para>
    /// </summary>
    public static class WhisperEffectContent
    {
        /// <summary>
        /// Milestone 8.46a. What her own decision controls say - the consequence in the printed text's
        /// own words, never the mechanism. <see cref="DecisionLabelCatalog"/> aggregates it and
        /// <c>DecisionLabelCoverageTests</c> proves it complete against the resolver's own pause rule.
        ///
        /// <para><b>Lesser Evil and Greater Good are read by the ENEMY hero, not by her.</b> "An enemy
        /// hero in range chooses one" makes that hero the chooser, so its two arms are written about
        /// the board rather than to "you": the card's own "you" is Whisper, and a control that kept it
        /// would tell the pressing player they were about to defeat a minion of their own. Naming her
        /// is what keeps the second bullet true for whoever is reading it.</para>
        ///
        /// <para>Lesser Evil's second bullet is a printed bullet and a <c>may</c> at once - it prints
        /// "You may defeat a minion adjacent to you" where Greater Good prints an unconditional "up to
        /// 3" - so it carries declining words and Greater Good's does not. Death Seeker's retrieval is
        /// the same double role.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionArm(WhisperCardContent.SwiftJustice, "modal-ranged-hero",
                "Target a hero in range with an empty discard");
            yield return DecisionArm(WhisperCardContent.SwiftJustice, "modal-adjacent-unit",
                "Target a unit adjacent to you");

            yield return DecisionArm(WhisperCardContent.DeathSeeker, "seeker-immunity",
                "This turn: You are immune to enemy actions");
            yield return DecisionStep(WhisperCardContent.DeathSeeker, "seeker-retrieve-may",
                "Retrieve a discarded card", "Leave my discard as it is");

            yield return DecisionArm(WhisperCardContent.LesserEvil, "modal-price",
                "Discard a card, if able");
            yield return DecisionStep(WhisperCardContent.LesserEvil, "modal-slaughter",
                "Defeat a minion adjacent to Whisper", "No minion is defeated");
            yield return DecisionArm(WhisperCardContent.GreaterGood, "modal-price",
                "Discard a card, or be defeated");
            yield return DecisionArm(WhisperCardContent.GreaterGood, "modal-slaughter",
                "Defeat up to 3 minions adjacent to Whisper");

            // Milestone 9.1, ruling W2: "repeat UP TO five times" is hers to stop.
            yield return DecisionStep(WhisperCardContent.BloodFrenzy, "after-repeat",
                "Attack a different target", "Stop here");
        }
        /// <summary>The Ultimate's rule identity, named here because the card content references it.</summary>
        public const string GrimReaperRuleId = "whisper-grim-reaper";

        private const string ActorSpaceOutput = "space";
        private const string MovedOutput = "moved";
        private const string PlacedOutput = "placed";

        /// <summary>
        /// "When performing basic actions you may choose one, or both." - Grim Reaper, as p.16's
        /// permanent ability.
        ///
        /// <para><b>One declaration, because the card states one rule.</b> It is not a value, a
        /// permission, an immunity or a trigger: it changes how many bullets one of rulebook p.10's
        /// modal nodes admits, which is what <see cref="RuleModificationKind.ChoiceCardinality"/>
        /// names and the first kind in this build to modify control flow rather than a rules
        /// value.</para>
        ///
        /// <para><b>"or both" is two, and two is what is declared.</b> A card printing a different
        /// number would declare that number; nothing here is a general "raise every modal", and the
        /// query bounds the answer by the options a node actually prints, so a one-bullet text cannot
        /// be widened into a second bullet that does not exist.</para>
        ///
        /// <para><b>"basic actions" is the scope, and p.9 defines it by the card.</b> "All actions on
        /// a basic card are basic actions", so this reaches Swift Justice's Attack and Death Seeker's
        /// Skill - her only two dealt basic cards - and reaches no Red, Blue or Green face of hers at
        /// all. That is <see cref="RuleValueApplicability.ControllerBasicAction"/>. It also does not
        /// reach Lesser Evil or Greater Good even though those print modals, and for two independent
        /// reasons: they are not basic cards, and their modal is not one Whisper is choosing.</para>
        ///
        /// <para><b>What "both" means differs by card, and neither meaning is stated here.</b> On
        /// Death Seeker the two bullets are two effects and both apply. On Swift Justice they are two
        /// targeting sentences, and the 2026-09-06 ruling makes "both" TWO FULL rulebook p.14
        /// sequences inside one action - which is the modal-Attack contract, entered by the card
        /// declaring <c>attack-sequence@1</c> leaves. Whisper's Gold card is composed that way
        /// whatever the Ultimate is doing, so the Ultimate only ever changes a number.</para>
        /// </summary>
        public static IReadOnlyList<RuleModification> GrimReaperModifications { get; } =
            Array.AsReadOnly(new[]
            {
                RuleModification.ForChoiceCardinality(
                    2, RuleValueApplicability.ControllerBasicAction)
            });

        // -----------------------------------------------------------------------------------------
        // p.14's step 1 and step 7: the attacker's own card text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The first body one printed Attack of Whisper's applies, or <c>null</c> where it prints none.
        /// Paired with <see cref="AttackTextTimingFor"/>, which names which of p.14's moments it is.
        ///
        /// <para>Six of her seven Attacks compose something. Vile Accusation is the boxed Handicap
        /// card and composes nothing, exactly as Wasp's Crackling Dagger, Arien's Ceremonial Blade,
        /// Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike and Xargatha's Desperate
        /// Cleave compose nothing, by the same 2026-08-21 user decision. What it would need when a
        /// milestone deals it is one call to <see cref="CreateModalAttackSequences"/> with its second
        /// bullet narrowed to heroes, because that is the only place its text differs from the Gold
        /// card's.</para>
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(WhisperCardContent.SwiftJustice))
            {
                return CreateModalAttackSequences(
                    cardId, contentRevision, AttackTargetableUnits.HeroesAndMinions);
            }

            if (cardId.Equals(WhisperCardContent.CrimsonTrail))
            {
                return CreateGatedStepThenTarget(cardId, contentRevision);
            }

            if (cardId.Equals(WhisperCardContent.SanguinePath))
            {
                return CreateCountedStepThenTarget(cardId, contentRevision, maximumSpaces: 2);
            }

            if (cardId.Equals(WhisperCardContent.BloodPilgrimage))
            {
                return CreateCountedStepThenTarget(cardId, contentRevision, maximumSpaces: 3);
            }

            if (cardId.Equals(WhisperCardContent.BloodFury))
            {
                return CreateDiscardGatedRepeat(cardId, contentRevision, maximumRepeats: 1);
            }

            if (cardId.Equals(WhisperCardContent.BloodFrenzy))
            {
                return CreateDiscardGatedRepeat(cardId, contentRevision, maximumRepeats: 5);
            }

            return null;
        }

        /// <summary>
        /// Which of p.14's moments <see cref="AttackTextFor"/>'s body is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Blood Fury and Blood Frenzy print one moment and it is step 7, so that is their first
        /// body: a card printing one moment has no second to name, which is the reading Tigerclaw's Hit
        /// and Run already takes. The three Red movers and the Gold card print step 1.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(WhisperCardContent.SwiftJustice) ||
                cardId.Equals(WhisperCardContent.CrimsonTrail) ||
                cardId.Equals(WhisperCardContent.SanguinePath) ||
                cardId.Equals(WhisperCardContent.BloodPilgrimage))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            if (cardId.Equals(WhisperCardContent.BloodFury) ||
                cardId.Equals(WhisperCardContent.BloodFrenzy))
            {
                return CardTextTiming.AfterTheAttack;
            }

            return CardTextTiming.None;
        }

        /// <summary>
        /// The second body a printed Attack applies, or <c>null</c> for the faces that print one or
        /// none. Only Swift Justice carries one: its first modal bullet prints "After the attack:"
        /// inside itself, which makes Whisper the first verified hero to print a p.14 timing keyword
        /// inside a modal bullet.
        /// </summary>
        public static EffectDefinition SecondAttackTextFor(
            CardDefinitionId cardId, string contentRevision) =>
            cardId.Equals(WhisperCardContent.SwiftJustice)
                ? CreateDefendedCardForcedMove(cardId, contentRevision)
                : null;

        public static CardTextTiming SecondAttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(WhisperCardContent.SwiftJustice)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.12's Skill primaries
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(WhisperCardContent.DeathSeeker))
            {
                return CreateDeathSeeker(cardId, contentRevision);
            }

            if (cardId.Equals(WhisperCardContent.SeedsOfFear))
            {
                // "Move that unit 1 space." - an exact distance, which is what separates this face
                // from its two upgrades. p.10's "up to" is absent, so the move is not optional in its
                // length: a unit with nowhere to go one space away cannot satisfy the step.
                return CreateSpawnPointDisplacement(
                    cardId, contentRevision, spaces: 1, upTo: false);
            }

            if (cardId.Equals(WhisperCardContent.SproutsOfPanic))
            {
                return CreateSpawnPointDisplacement(cardId, contentRevision, spaces: 2, upTo: true);
            }

            if (cardId.Equals(WhisperCardContent.BloomingNightmare))
            {
                return CreateSpawnPointDisplacement(cardId, contentRevision, spaces: 3, upTo: true);
            }

            if (cardId.Equals(WhisperCardContent.LesserEvil))
            {
                return CreateOpponentChosenModal(cardId, contentRevision, escalatesToDefeat: false);
            }

            if (cardId.Equals(WhisperCardContent.GreaterGood))
            {
                return CreateOpponentChosenModal(cardId, contentRevision, escalatesToDefeat: true);
            }

            if (cardId.Equals(WhisperCardContent.ShadowStep) ||
                cardId.Equals(WhisperCardContent.ShadowWalk))
            {
                return CreateSpawnPointPlace(
                    cardId, contentRevision,
                    PlaceSpawnPointExclusion.SpacesThatAreNotAnEmptyMinionSpawnPointInTheBattleZone);
            }

            if (cardId.Equals(WhisperCardContent.CreepingShadow))
            {
                return CreateSpawnPointPlace(
                    cardId, contentRevision,
                    PlaceSpawnPointExclusion.SpacesThatAreNeitherSuchAPointNorAdjacentToOne);
            }

            if (cardId.Equals(WhisperCardContent.CruelTwist))
            {
                return CreateSwapThenStep(cardId, contentRevision, spaces: 2);
            }

            if (cardId.Equals(WhisperCardContent.SealedFate))
            {
                return CreateSwapThenStep(cardId, contentRevision, spaces: 3);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // Compositions
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Choose one — • Target a hero in range with an empty discard; ... • Target a unit adjacent
        /// to you." - Swift Justice's step-1 text, as the modal-Attack contract.
        ///
        /// <para><b>Each bullet performs nothing.</b> It publishes its own printed targeting sentence,
        /// and the card-action authority runs one complete p.14 order per chosen bullet - own
        /// targeting against the board as it then stands, own Defense window, own Attack Total, own
        /// consequences - with the whole modal being ONE action for every external purpose. That is
        /// the contract milestone <c>8.35z27</c> built and this is its first printed
        /// consumer.</para>
        ///
        /// <para><b>The printed maximum is one and the Ultimate is what raises it.</b> Grim Reaper
        /// declares a choice cardinality of two scoped to her basic cards, so the same definition
        /// offers one bullet before the Ultimate is unlocked and two after it, with nothing about the
        /// card having changed. The minimum stays one either way: a modal Attack that chose nothing
        /// would be an Attack that never targeted anything.</para>
        ///
        /// <para><b>Nothing constrains the two sequences to different units</b>, because the face
        /// prints no "(on different targets)" clause - the 2026-09-06 rider, in the user's own words:
        /// "if he does not move, it can be the SAME target as well". So both bullets may point at one
        /// unit provided it still satisfies each bullet's own sentence when that bullet resolves.</para>
        ///
        /// <para><b>BULLET 1'S "WITH AN EMPTY DISCARD" IS NOW COMPOSED, milestone
        /// 8.35z53.</b> "with an empty discard" is a predicate about the candidate, and
        /// <see cref="PrintedAttackTargetPolicy"/> states which units, how far and which line - it
        /// cannot state a predicate. The multi-sequence contract recorded that as its one open extension
        /// point, sized for Dodger's Middlefinger of Death; milestone 8.35z45 took the extension point
        /// as <see cref="AttackSequenceSubjectNarrowing"/>, and this bullet is its second printed
        /// consumer, reading the exact negation of Middlefinger's own clause -
        /// <see cref="AttackSequenceSubjectNarrowing.HasAnEmptyDiscard"/> - so the two members cannot
        /// come to disagree about what one hero's discard pile holds. Composing it costs no schema:
        /// the field it travels through was already widened at Middlefinger's dealing, and a bullet
        /// declaring a member of an already-open domain moves no shared declaration and no other
        /// shipped hero's content identity.</para>
        ///
        /// <para><b>THE RIDER'S OWN GATING IS A SEPARATE, STILL-OPEN GAP.</b> The step-7 rider below
        /// (<see cref="CreateDefendedCardForcedMove"/>) fires whenever EITHER bullet's sequence
        /// completes, because <c>SecondAttackTextFor</c> is asked once per completed sequence and does
        /// not yet read which bullet ran. The printed "After the attack:" clause is embedded inside
        /// bullet 1's own sentence alone, so a hero taken by bullet 2 (adjacent unit, which admits
        /// heroes too) who then defends would still trigger the rider today - reachable now that bullet
        /// 1 is properly narrowed rather than only when it over-offered. The fix reads
        /// <c>SequenceBulletFor(resolution).SubjectNarrowing == AttackSequenceSubjectNarrowing
        /// .HasAnEmptyDiscard</c> at the rider's own gate, which is dealing-time work and is not
        /// composed here - flagged for the executor rather than folded into this prep's scope.</para>
        /// </summary>
        private static EffectDefinition CreateModalAttackSequences(
            CardDefinitionId cardId,
            string contentRevision,
            AttackTargetableUnits adjacentBulletUnits)
        {
            var modal = new EffectNodeId("modal");
            var rangedHero = new EffectNodeId("modal-ranged-hero");
            var adjacentUnit = new EffectNodeId("modal-adjacent-unit");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { rangedHero, adjacentUnit },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),

                    // "Target a hero in range with an empty discard ..." - the printed Range icon,
                    // heroes only, and the pile clause as the bullet's own subject narrowing. Vile
                    // Accusation shares this exact bullet unchanged (its printed text differs only on
                    // the second bullet), which is why the narrowing lives here rather than being
                    // duplicated at her own composition when she is dealt.
                    ModalBulletNode(
                        rangedHero, AttackTargetableUnits.HeroesOnly, AttackTargetReach.PrintedRange,
                        AttackSequenceSubjectNarrowing.HasAnEmptyDiscard),

                    // "Target a unit adjacent to you." - p.19's units at p.19's innermost ring. Vile
                    // Accusation narrows this one bullet to heroes and nothing else about the face.
                    ModalBulletNode(
                        adjacentUnit, adjacentBulletUnits, AttackTargetReach.AdjacentOnly,
                        AttackSequenceSubjectNarrowing.None, appliesAfterAttackText: false)
                });
        }

        /// <summary>
        /// One bullet of Swift Justice's (and, when she is dealt, Vile Accusation's) modal, as an
        /// <c>attack-sequence@1</c> leaf. Widened at milestone 8.35z53 to accept the
        /// candidate predicate <see cref="AttackSequenceSubjectNarrowing"/> declares, mirroring
        /// Middlefinger of Death's own <c>ModalBulletNode</c> exactly: the parameter and its output
        /// binding are added to the node ONLY where the bullet actually narrows, so a bullet declaring
        /// <see cref="AttackSequenceSubjectNarrowing.None"/> - every bullet composed before this one -
        /// is unchanged down to the byte.
        /// </summary>
        private static EffectNodeDefinition ModalBulletNode(
            EffectNodeId nodeId,
            AttackTargetableUnits targetableUnits,
            AttackTargetReach reach,
            AttackSequenceSubjectNarrowing subjectNarrowing,
            bool appliesAfterAttackText = true)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.TargetableUnitsParameter, targetableUnits),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.ReachParameter, reach),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.LineParameter, AttackTargetLine.AnyLine),

                // The face prints no "(on different targets)", so the contract adds none.
                EffectInputBinding.FromLiteral(
                    SyntheticSequenceCapabilities.DistinctTargetsParameter,
                    EffectValue.Boolean(false))
            };
            var outputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.TargetableUnitsOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.ReachOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.LineOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.DistinctTargetsOutput, EffectValueKind.Boolean)
            };

            if (subjectNarrowing != AttackSequenceSubjectNarrowing.None)
            {
                inputs.Add(EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.SubjectNarrowingParameter, subjectNarrowing));
                outputs.Add(new EffectOutputBinding(
                    SyntheticSequenceCapabilities.SubjectNarrowingOutput, EffectValueKind.Integer));
            }

            if (!appliesAfterAttackText)
            {
                inputs.Add(EffectInputBinding.FromLiteral(SyntheticSequenceCapabilities.AppliesAfterAttackTextParameter, EffectValue.Boolean(false)));
                outputs.Add(new EffectOutputBinding(SyntheticSequenceCapabilities.AppliesAfterAttackTextOutput, EffectValueKind.Boolean));
            }
            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSequenceCapabilities.AttackSequenceV1,
                inputs: inputs,
                outputs: outputs);
        }

        /// <summary>
        /// "After the attack: If able, that hero performs a movement action on the card they defended
        /// with, moving full distance in a straight line." - Swift Justice's step-7 rider.
        ///
        /// <para>One node, because everything the sentence says is one capability's - see
        /// <see cref="SyntheticRosterCapabilities.DefendedCardForcedMoveV1"/> for the printed reading
        /// of each clause. What is here is the failure policy: "If able" is
        /// <see cref="EffectFailurePolicy.IfAble"/>, which opens no decision of the actor's and skips
        /// silently where the sentence cannot be carried out - a defender the page removed at step 6,
        /// one who declined to defend, one whose card prints no Movement value, one hemmed in on every
        /// side.</para>
        /// </summary>
        private static EffectDefinition CreateDefendedCardForcedMove(
            CardDefinitionId cardId, string contentRevision)
        {
            var rider = new EffectNodeId("rider");

            return new EffectDefinition(
                SecondAttackTextEffectId(cardId),
                contentRevision,
                rider,
                new[]
                {
                    new EffectNodeDefinition(
                        rider, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticRosterCapabilities.DefendedCardForcedMoveV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.MovedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Before the attack: You may move up to 1 space if there is an empty minion spawn point in
        /// radius in the battle zone. Target a unit adjacent to you." - Crimson Trail.
        ///
        /// <para><b>The order is the printed order and it decides the target.</b> The move comes
        /// first, so "a unit adjacent to you" is judged from the space the move left her in - the seam
        /// Brogan's charges own and milestone 8.35z25 corrected Tigerclaw's two faces onto. The Attack
        /// therefore declares that its target is bound by its own card text, and p.14's opening
        /// question, which is asked before any text runs, does not ask it.</para>
        ///
        /// <para><b>The condition gates the move and nothing else.</b> "if there is an empty minion
        /// spawn point in radius in the battle zone" is a property of the board, not of the
        /// destination: it decides whether she may step at all, and where she steps is unconstrained.
        /// So a false condition leaves the targeting sentence untouched and the Attack proceeds from
        /// where she stands, which is what a card that prints its condition on one clause says.</para>
        ///
        /// <para><b>Stranding is offered and is the player's rope.</b> Every space one step away is a
        /// legal move, including one that leaves nobody adjacent; taking it stops the Attack at the
        /// mandatory targeting step, the card is spent and the move stands. That is the 2026-09-06
        /// ruling's own answer to the stranding case rather than an oversight.</para>
        /// </summary>
        private static EffectDefinition CreateGatedStepThenTarget(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("before-root");
            var actorSpace = new EffectNodeId("before-actor-space");
            var gate = new EffectNodeId("before-gate");
            var move = new EffectNodeId("before-move");
            var victim = new EffectNodeId("before-target");
            var retarget = new EffectNodeId("before-retarget");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, gate, victim, retarget }),
                    ActorSpaceNode(actorSpace),

                    // The condition has a then-branch and no else-branch: an unsatisfied "if" is a
                    // step that simply does not happen, and the card carries on.
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1,
                        childNodeIds: new[] { move },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.CountOutput, EffectValueKind.Integer)
                        }),
                    StepNode(move, actorSpace, distance: 1),
                    AdjacentUnitTargetNode(victim),
                    BindAttackTargetNode(retarget, victim)
                });
        }

        /// <summary>
        /// "Before the attack: You may move up to 1 space for every empty minion spawn point in radius
        /// in the battle zone up to a maximum of 2 / 3 spaces. Target a unit adjacent to you." -
        /// Sanguine Path and Blood Pilgrimage, whose sentences differ by one number.
        ///
        /// <para><b>The board supplies the budget and the card supplies its ceiling.</b> Both halves
        /// are one sentence and yield one number, so the count capability publishes the lesser of the
        /// two and the move binds its distance from it - rather than the move being given a printed
        /// number and a clamp being applied somewhere else. A board with no qualifying Spawn point
        /// yields zero, and an up-to move of zero spaces moves nowhere without needing a condition to
        /// say so, which is why these two faces print no "if there is" where Crimson Trail does.</para>
        ///
        /// <para>Everything else is Crimson Trail's: the printed order, the actor-origin targeting
        /// after the move, and the same stranding answer.</para>
        /// </summary>
        private static EffectDefinition CreateCountedStepThenTarget(
            CardDefinitionId cardId, string contentRevision, int maximumSpaces)
        {
            var root = new EffectNodeId("before-root");
            var actorSpace = new EffectNodeId("before-actor-space");
            var count = new EffectNodeId("before-count");
            var move = new EffectNodeId("before-move");
            var victim = new EffectNodeId("before-target");
            var retarget = new EffectNodeId("before-retarget");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, count, move, victim, retarget }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        count, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.MaximumCountedParameter,
                                EffectValue.Integer(maximumSpaces))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.CountOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                "distance", count, SyntheticRosterCapabilities.CountOutput,
                                EffectValueKind.Integer),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        }),
                    AdjacentUnitTargetNode(victim),
                    BindAttackTargetNode(retarget, victim)
                });
        }

        /// <summary>
        /// "After the attack: If an enemy hero in radius has a card in the discard, repeat once / up to
        /// five times on different targets." - Blood Fury and Blood Frenzy.
        ///
        /// <para><b>The condition is re-checked before each instance, and that is consequential.</b>
        /// p.10 makes each repeat a separate instance of the action, so this text runs again inside it
        /// and asks the board again - which the 2026-09-06 ruling states as a rule: a hero who
        /// discards <em>to defend</em> one instance of the chain arms the instances after it. Nothing
        /// here arranges that; it falls out of the condition being a live query inside a body that
        /// runs per instance.</para>
        ///
        /// <para><b>Neither face prints "may", so neither repeat is declinable.</b> It is
        /// <see cref="EffectFailurePolicy.IfAble"/>: mandatory where a legal target exists and skipped
        /// where none does, which is also what makes running out of targets the ordinary end of a
        /// chain rather than a stop.</para>
        ///
        /// <para><b>MEASURED ENGINE GAP on Blood Frenzy, asserted by test rather than assumed.</b> The
        /// 2026-09-06 ruling reads "on different targets" as PAIRWISE distinct - no unit attacked
        /// twice anywhere in the chain. The build's repeat exclusion is a single target, reset at each
        /// instance, so it excludes only the unit the <em>previous</em> instance took. At Blood Fury's
        /// maximum of one the two readings coincide exactly and the card is correct. At Blood Frenzy's
        /// five they diverge: the third instance may re-take the first's target. Every
        /// <c>DifferentTarget</c> repeat shipped before this hero has a maximum of one - Wasp's Center
        /// of Mass reaches two but binds <c>SameTarget</c> - so Blood Frenzy is the first printed face
        /// in the roster where the distinction exists at all. Closing it means an accumulating
        /// exclusion set where a single target is held today, and that state is persisted, so it is a
        /// schema-bumping change and not phase-one work.</para>
        /// </summary>
        private static EffectDefinition CreateDiscardGatedRepeat(
            CardDefinitionId cardId, string contentRevision, int maximumRepeats)
        {
            var root = new EffectNodeId("after-root");
            var gate = new EffectNodeId("after-gate");
            var repeat = new EffectNodeId("after-repeat");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                        childNodeIds: new[] { repeat },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    // Milestone 9.1, rulings W1 and W2 (Rules/PRINTED_WORDING.md). Blood Fury's
                    // "repeat once" is automatic; Blood Frenzy's "repeat UP TO five times" is the
                    // player's to stop after any instance.
                    maximumRepeats > 1
                        ? new EffectNodeDefinition(
                            repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                            chooser: EffectChooserRole.Actor,
                            maximumRepeats: maximumRepeats,
                            repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                        : new EffectNodeDefinition(
                            repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                            maximumRepeats: maximumRepeats,
                            repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }

        /// <summary>
        /// "If an enemy hero in radius has a card in the discard, choose one — • This turn: You are
        /// immune to enemy actions. • You may retrieve a discarded card." - Death Seeker.
        ///
        /// <para><b>The gate is why this card cannot be played on the first turn of round 1</b>, and
        /// the 2026-09-06 ruling accepts that as product behaviour: no hero has discarded anything
        /// yet, so the condition is false for every seat and rule 14 refuses an action whose effect can
        /// be proven empty. The greying already names its blocker, and no special case is made
        /// here.</para>
        ///
        /// <para><b>The modal is Whisper's own</b> - "choose one" with no other hero named - so its
        /// chooser is the actor, and Grim Reaper is what raises its maximum from one to two. Under the
        /// 2026-09-06 ruling "both" on this card is the plain reading: both effect bullets apply.</para>
        ///
        /// <para><b>"You may retrieve" is the bullet's own optionality</b> and stays inside the bullet
        /// rather than being read as the modal's: choosing that bullet and then declining is a
        /// different thing from never choosing it, and only the second is what an unchosen bullet
        /// means.</para>
        /// </summary>
        private static EffectDefinition CreateDeathSeeker(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("seeker-root");
            var gate = new EffectNodeId("seeker-gate");
            var modal = new EffectNodeId("seeker-modal");
            var immunity = new EffectNodeId("seeker-immunity");
            var retrieveMay = new EffectNodeId("seeker-retrieve-may");
            var retrieve = new EffectNodeId("seeker-retrieve");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                        childNodeIds: new[] { modal },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { immunity, retrieveMay },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        immunity, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        WhisperActiveEffectContent.DeathSeekerImmunityV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        retrieveMay, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { retrieve },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// "An enemy hero in range chooses one — • That hero discards a card[, if able / , or is
        /// defeated]. • You [may defeat a minion / defeat up to 3 minions] adjacent to you." - Lesser
        /// Evil and Greater Good.
        ///
        /// <para><b>The opponent chooses the bullet, which is what makes these two faces new.</b>
        /// p.10 permits it - "You make all the choices in your card's text, unless the card text
        /// instructs the other hero to make a choice" - and the 2026-09-06 ruling extends that from
        /// choosing a card, which the shipped roster already does, to choosing a bullet. Whisper picks
        /// <em>which</em> enemy hero; that hero picks the bullet; and the second bullet's effect then
        /// happens on Whisper's side of the board, performed by her.</para>
        ///
        /// <para><b>Bullet 2's optionality stays Whisper's.</b> Lesser Evil prints "You <em>may</em>
        /// defeat a minion", so having had the bullet chosen for her she still elects whether to
        /// defeat; Greater Good prints "You defeat up to 3 minions", where the election is gone but
        /// the count is hers and may be zero. The Tier III printing dropping the "may" is deliberate
        /// and both readings are the words as printed.</para>
        ///
        /// <para><b>The minions are any minions, friendly included.</b> Neither face prints "enemy",
        /// and Whisper's own vocabulary prints it five times elsewhere, so its absence is the
        /// definition - the 2026-09-06 ruling. That is what makes the opponent's choice interesting:
        /// facing Greater Good with only Whisper's own minions beside her, an enemy may pick that
        /// bullet deliberately. Nothing is forced on her, because "up to" lets the count be zero.</para>
        ///
        /// <para><b>Neither bullet is greyed for having no effect.</b> The 2026-09-06 two-tier ruling
        /// splits an option that CANNOT be performed from one that WOULD DO NOTHING: the first is
        /// greyed with its reason, the second is offered and choosing it is a legal no-op. These
        /// opponent-chosen nodes are the canonical case - the enemy's escape hatch - which is what the
        /// designer's own reading of Lesser Evil's reminder text says: it holds no rules text, and the
        /// card would function identically without it, so Greater Good behaves the same way.</para>
        /// </summary>
        private static EffectDefinition CreateOpponentChosenModal(
            CardDefinitionId cardId, string contentRevision, bool escalatesToDefeat)
        {
            var root = new EffectNodeId("modal-root");
            var victim = new EffectNodeId("modal-victim");
            var modal = new EffectNodeId("modal-choice");
            var price = new EffectNodeId("modal-price");
            var discard = new EffectNodeId("modal-discard");
            var escalation = new EffectNodeId("modal-escalation");
            var defeatHero = new EffectNodeId("modal-defeat-hero");
            var slaughter = new EffectNodeId("modal-slaughter");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { victim, modal }),

                // "An enemy hero in range" - Whisper's own selection, at the card's printed Range.
                new EffectNodeDefinition(
                    victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeHeroTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                            EffectValue.Boolean(false))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }),

                // "chooses one" - the modal whose chooser is the hero the node above named.
                new EffectNodeDefinition(
                    modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { price, slaughter },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            EffectNodeDefinition.ChooserParameterName, victim,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    chooser: EffectChooserRole.BoundHero,
                    minimumSelections: 1,
                    maximumSelections: 1),

                // Bullet 1. Lesser Evil prints "if able", so its discard is skipped where the hero
                // holds nothing; Greater Good prints "or is defeated", which is one mandatory step
                // with two ways to complete - the reading ledger question 13 settled on Arien's
                // identical sentence, and this is that composition unchanged.
                new EffectNodeDefinition(
                    price, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: escalatesToDefeat
                        ? new[] { discard, escalation }
                        : new[] { discard }),
                new EffectNodeDefinition(
                    discard,
                    EffectNodeKind.Capability,
                    escalatesToDefeat ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, victim,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    }),

                // Bullet 2. "adjacent to you" is p.19's innermost ring measured from Whisper, and the
                // side is every side, because the face names none.
                MinionSlaughterNode(slaughter, escalatesToDefeat)
            };

            if (escalatesToDefeat)
            {
                nodes.Add(new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeatHero },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard,
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    defeatHero, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, victim,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            EffectValueKind.Hero)
                    }));
            }

            nodes.AddRange(MinionSlaughterChildren(slaughter, escalatesToDefeat));

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// Bullet 2's own root: Lesser Evil's declinable single Defeat, or Greater Good's bounded
        /// three.
        /// </summary>
        private static EffectNodeDefinition MinionSlaughterNode(
            EffectNodeId nodeId, bool bounded) =>
            bounded
                ? new EffectNodeDefinition(
                    nodeId, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[]
                    {
                        Child(nodeId, "set"), Child(nodeId, "first"),
                        Child(nodeId, "second"), Child(nodeId, "third")
                    })
                : new EffectNodeDefinition(
                    nodeId, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { Child(nodeId, "one") },
                    chooser: EffectChooserRole.Actor);

        private static IEnumerable<EffectNodeDefinition> MinionSlaughterChildren(
            EffectNodeId nodeId, bool bounded)
        {
            if (!bounded)
            {
                // "You may defeat a minion adjacent to you." - one selection and one Defeat, inside
                // the `may` above. Every side of the board, because the face prints no adjective.
                var one = Child(nodeId, "one");
                yield return new EffectNodeDefinition(
                    one, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { Child(one, "pick"), Child(one, "kill") });
                yield return AdjacentMinionSelectionNode(Child(one, "pick"));
                yield return DefeatMinionNode(
                    Child(one, "kill"), Child(one, "pick"),
                    SyntheticRosterCapabilities.ChosenMinionOutput,
                    EffectFailurePolicy.Mandatory);
                yield break;
            }

            // "You defeat up to 3 minions adjacent to you." - one selection naming up to three, and
            // three Defeats each reading one slot. The slots a selection of fewer than three leaves
            // unfilled are skipped by their own node's `if able`, which is the shape Sabina's Bullet
            // Hell already has one slot narrower.
            var set = Child(nodeId, "set");
            yield return new EffectNodeDefinition(
                set, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetTrioV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.AnyTeam),

                    // "up to 3", floored at nothing: the count is Whisper's and zero is an answer.
                    EffectInputBinding.FromLiteral("minimum", EffectValue.Integer(0)),
                    EffectInputBinding.FromLiteral("maximum", EffectValue.Integer(3))
                },
                outputs: new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("secondMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("thirdMinion", EffectValueKind.Minion)
                });
            yield return DefeatMinionNode(
                Child(nodeId, "first"), set, "firstMinion", EffectFailurePolicy.IfAble);
            yield return DefeatMinionNode(
                Child(nodeId, "second"), set, "secondMinion", EffectFailurePolicy.IfAble);
            yield return DefeatMinionNode(
                Child(nodeId, "third"), set, "thirdMinion", EffectFailurePolicy.IfAble);
        }

        /// <summary>
        /// "Target an enemy unit in radius occupying a spawn point. Move that unit N / up to N spaces."
        /// - Seeds of Fear, Sprouts of Panic and Blooming Nightmare.
        ///
        /// <para><b>Two printed clauses and each is one node.</b> The selection carries the reach
        /// (p.9's Radius disk, which p.16's item widens), the side, and the narrowing "occupying a
        /// spawn point"; the Move carries how far. Nothing about the destination is narrowed - the
        /// sentence says only how many spaces - so the unit may be moved anywhere its reachability
        /// admits, on or off a Spawn point.</para>
        ///
        /// <para><b>"a spawn point" here is p.19's broad term and not the Red line's narrow one.</b>
        /// These three faces print neither "minion" nor "in the battle zone", where seven other faces
        /// of hers print both, so the qualifiers are not read across. Emptiness cannot be part of it
        /// either: the unit being selected is standing on the point.</para>
        ///
        /// <para><b>Seeds of Fear moves exactly one space and its upgrades move "up to".</b> That is
        /// the only difference in the family besides the disk, and it is the difference between a
        /// mandatory distance and a budget - so a unit hemmed in cannot satisfy Seeds of Fear's step
        /// while it satisfies the other two by not moving.</para>
        /// </summary>
        private static EffectDefinition CreateSpawnPointDisplacement(
            CardDefinitionId cardId, string contentRevision, int spaces, bool upTo)
        {
            var root = new EffectNodeId("spawn-root");
            var victim = new EffectNodeId("spawn-target");
            var move = new EffectNodeId("spawn-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { victim, move }),
                    new EffectNodeDefinition(
                        victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.OccupiesSpawnPointParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability,
                        upTo ? EffectFailurePolicy.UpTo : EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", victim,
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(spaces)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Place yourself into an empty minion spawn point in range in the battle zone[, or into a
        /// space in range adjacent to such spawn point]." - Shadow Step, Shadow Walk and Creeping
        /// Shadow.
        ///
        /// <para><b>The actor is the piece being placed</b>, which is the shape
        /// <c>synthetic-place-in-range@1</c> already has - Arien's three Green cards place him the
        /// same way - so nothing new is minted for the placement itself. What is new is the
        /// destination clause, and it is the inverse of Arien's: his three carve Spawn-point spaces
        /// <em>out</em> of the disk, hers admit only those spaces. Both are one filter over one derived
        /// candidate set, which is what keeps the offer and the commit from being able to
        /// disagree.</para>
        ///
        /// <para><b>The Place is mandatory: no face prints "may".</b> A hero with no legal destination
        /// stops the Skill at that step, which is p.10's own answer for an impossible mandatory step
        /// and leaves everything before it standing. Under rule 14 the action is refused before it is
        /// offered rather than taken and wasted.</para>
        ///
        /// <para><b>She need not be in the Battle Zone to reach into it.</b> "in the battle zone"
        /// qualifies the spawn point and never the hero, and the Range disk is measured from wherever
        /// she stands - the designer's own 2026-07-25 answer, built into the derivation rather than
        /// restated here. Shadow Walk's Range 4 is what makes that a deliberate tool rather than a
        /// technicality.</para>
        /// </summary>
        private static EffectDefinition CreateSpawnPointPlace(
            CardDefinitionId cardId, string contentRevision, PlaceSpawnPointExclusion exclusion)
        {
            var root = new EffectNodeId("step-root");
            var actorSpace = new EffectNodeId("step-actor-space");
            var place = new EffectNodeId("step-place");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRangeV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                PlaceEligiblePieceKind.GamePiece),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                PlaceDestinationAnchor.ActingHero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                exclusion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Swap with a unit adjacent to you, or with an enemy hero in range with a card in the
        /// discard. You may then move up to 2 / 3 spaces." - Cruel Twist and Sealed Fate.
        ///
        /// <para><b>The disjunction is one selection and not two.</b> The face offers one choice among
        /// a union of two differently reached sets, so composing it as a modal would invent a question
        /// - "which kind of swap?" - that the card never asks. That union is
        /// <c>choose-swap-subject@1</c>, whose two arms are declared here because these two faces print
        /// both; the adjacency arm is p.19's innermost ring and names no side, and the hero arm is the
        /// card's printed Range narrowed to enemies holding a discarded card.</para>
        ///
        /// <para><b>"You may then move" is a separate step and its own decision.</b> "then" puts it
        /// after the Swap, and "may ... up to N" makes both whether and how far Whisper's - so it is an
        /// up-to move from wherever the Swap left her, which is the space her subject was standing in.
        /// p.19 makes a Swap not movement, so nothing about the exchange spends this budget.</para>
        /// </summary>
        private static EffectDefinition CreateSwapThenStep(
            CardDefinitionId cardId, string contentRevision, int spaces)
        {
            var root = new EffectNodeId("twist-root");
            var subject = new EffectNodeId("twist-subject");
            var swap = new EffectNodeId("twist-swap");
            var move = new EffectNodeId("twist-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { subject, swap, move }),
                    new EffectNodeDefinition(
                        subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseSwapSubjectV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.AdjacentUnitKindParameter,
                                RosterUnitKind.Unit),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RangedHeroRelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.RangedHeroRequiresDiscardParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenSpaceOutput,
                                EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapWithActorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", subject,
                                SyntheticRosterCapabilities.ChosenSpaceOutput,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                        }),

                    // The move starts from where the Swap put her, which the exchange publishes rather
                    // than the board being asked again - the same reading Arien's Ebb and Flow takes of
                    // the pair of spaces a Swap exchanges.
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", swap, "actorAt", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(spaces)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        // -----------------------------------------------------------------------------------------
        // Shared nodes
        // -----------------------------------------------------------------------------------------

        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        /// <summary>
        /// "You may move up to 1 space" as an unconstrained up-to Move of the actor. Every space the
        /// Movement action's own reachability admits is a legal destination, because the sentence
        /// demands nothing of the space at all.
        /// </summary>
        private static EffectNodeDefinition StepNode(
            EffectNodeId nodeId, EffectNodeId actorSpace, int distance) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                });

        /// <summary>
        /// "Target a unit adjacent to you." - p.13's own reach for a non-Ranged Attack, measured from
        /// the actor rather than from a remembered space, which is what makes the question read the
        /// board the move just produced.
        ///
        /// <para>The activity is left undeclared, exactly as Brogan's charges and Tigerclaw's two
        /// corrected faces leave it, so the enclosing Attack answers p.18's immunity question: the
        /// selection must ask the same question p.14 would have asked, or moving the targeting into
        /// card text would quietly re-scope who may be targeted.</para>
        /// </summary>
        private static EffectNodeDefinition AdjacentUnitTargetNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Unit),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                });

        private static EffectNodeDefinition BindAttackTargetNode(
            EffectNodeId nodeId, EffectNodeId source) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter,
                        source, SyntheticRosterCapabilities.ChosenHeroOutput,
                        EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter,
                        source, SyntheticRosterCapabilities.ChosenMinionOutput,
                        EffectValueKind.Minion)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                        EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                        EffectValueKind.Minion)
                });

        /// <summary>
        /// "a minion adjacent to you" - every side of the board, because the face prints no adjective
        /// and Whisper's own vocabulary prints "enemy" five times elsewhere.
        /// </summary>
        private static EffectNodeDefinition AdjacentMinionSelectionNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.AnyTeam)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion),
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                });

        private static EffectNodeDefinition DefeatMinionNode(
            EffectNodeId nodeId,
            EffectNodeId source,
            string outputName,
            EffectFailurePolicy policy) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, policy,
                SyntheticConsequenceCapabilities.DefeatMinionV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticConsequenceCapabilities.MinionParameter,
                        source, outputName, EffectValueKind.Minion)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                        EffectValueKind.Minion)
                });

        private static EffectNodeId Child(EffectNodeId parent, string suffix) =>
            new EffectNodeId($"{parent.Value}-{suffix}");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-attack-text");

        private static EffectDefinitionId SecondAttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-after-attack-text");

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-skill");
    }
}
