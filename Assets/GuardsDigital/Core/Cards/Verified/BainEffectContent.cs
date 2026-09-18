using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The effect bodies Bain the Bounty Hunter's printed card text composes to - milestone 8.40i.
    ///
    /// <para><b>Every contract these bodies name was built before them, which is what the eight
    /// lettered blocks of this series were for.</b> Nothing here mints a capability: the marker and its
    /// defeat cost are 8.40a's, the transfer and the definition-bound return are 8.40b's, the
    /// marker-presence predicate and its two selection narrowings are 8.40c's, the unit-and-terrain
    /// clear line is 8.40d's, the pull's terrain narrowing and its actor-side approach space are
    /// 8.40e's, the hidden choice, the reveal, the public guess domain, the hand-size narrowing and the
    /// failure modal are 8.40f's, the board-conditioned Block is 8.40g's and the after-a-marker-given
    /// trigger is 8.40h's.</para>
    ///
    /// <para><b>THREE MAPPING ROWS NAMED THE WRONG CONTRACT AND ARE CORRECTED IN HIS SHARD BESIDE THIS
    /// FILE.</b> Composition is where this project expects such things to surface, and this is the
    /// ninth time in two series that it has. (1) "If they do" is <c>synthetic-is-flagged@1</c> over a
    /// fact <c>retrieve-card@1</c> did not publish: that capability's <c>flag</c> parameter is a
    /// BOOLEAN and the retrieval published a card instance and a hero, so the row's binding is refused
    /// by the definition validator. The capability gained the count form of its own fact -
    /// <c>didRetrieve</c>, published only on success, exactly as <c>discard-owned-card@1</c> already
    /// carries <c>didDiscard</c> - which is an added OUTPUT and therefore moves no digest and no
    /// earlier node. (2) Hand Crossbow's bullet cannot be a
    /// printed-Range hero selection narrowed by <c>holdsTheActorsMarker</c>: that narrowing lives on
    /// <c>choose-target@2</c>, whose reach is an integer a definition writes down, and writing 3 there
    /// would make a +1 Range item raise every other reach on his sheet and not that bullet - which is
    /// the one item BA-15 says his Blue Tier III exists to grant. The bullet that reads the printed
    /// Range AND carries the marker narrowing is <c>attack-sequence@1</c>'s, so Hand Crossbow composes
    /// as a modal over two bullets exactly as Hunter-Seeker does and differs from it in the modal's own
    /// cardinality, which is what the two printed sentences differ in. (3) Close Call's transfer needs
    /// no <c>synthetic-is-flagged@1</c> either: the predicate publishes a Boolean itself, so it IS the
    /// <c>Conditional</c>'s capability, and the transfer's <c>holder</c> is the hero the marker moves
    /// TO - the defender, who is the frame's own actor - rather than the hero it moves from, which the
    /// handler derives.</para>
    ///
    /// <para><b>Six of his declarations are deliberately unregistered as this file is written</b> - his
    /// four marker operations (<see cref="BainComponentContent"/>), the hidden card choice and the
    /// reveal (<see cref="SyntheticHiddenChoiceCapabilities"/>) - held out of the build's registration
    /// list by milestone 7.9's deferral gate until a dealt card can start them. Composing faces that
    /// name them is exactly what the gate waits for; milestone 8.40j is what registers them and adds
    /// the showcase consumers the gate's second half requires of a new handler TYPE.</para>
    /// </summary>
    public static class BainEffectContent
    {
        /// <summary>
        /// Milestone 8.46a. What his own decision controls say - the consequence in the printed text's
        /// own words, never the mechanism. <see cref="DecisionLabelCatalog"/> aggregates it and
        /// <c>DecisionLabelCoverageTests</c> proves it complete against the resolver's own pause rule.
        ///
        /// <para>We're Not Done Yet!'s repeat is one of the five nodes in this build that is BOTH an
        /// arm of a <c>Choose</c> and a node opening rulebook p.10's confirm-or-decline - "otherwise
        /// may repeat once or gain 2 coins" prints the repeat as a bullet and as a <c>may</c>. One
        /// declaration answers both, and what the repeat repeats is the guess, so that is what it
        /// says.</para>
        ///
        /// <para>Another One!'s row keys a node in the End-of-turn body its Active effect names by
        /// reference to the card that prints the clause, which is what the pause carries.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionStep(BainCardContent.DeadOrAlive, "bounty-may",
                "Give an enemy hero in radius a bounty marker", "Give no bounty marker");

            yield return DecisionArm(BainCardContent.HandCrossbow, "crossbow-marker-bullet",
                "Target a hero in range with a bounty marker");
            yield return DecisionArm(BainCardContent.HandCrossbow, "crossbow-adjacent-bullet",
                "Target a unit adjacent to you");
            yield return DecisionArm(BainCardContent.HunterSeeker, "crossbow-marker-bullet",
                "Target a hero in range with a bounty marker");
            yield return DecisionArm(BainCardContent.HunterSeeker, "crossbow-adjacent-bullet",
                "Target a unit adjacent to you");

            yield return DecisionStep(BainCardContent.WereNotDoneYet, "guess-repeat",
                "Guess again on another card", "Stop here");
            yield return DecisionArm(BainCardContent.WereNotDoneYet, "guess-coin-bullet",
                "Gain 2 coins");

            yield return DecisionStep(BainCardContent.AnotherOne, "another-one-repeat-optional",
                "Have a hero retrieve a discarded card again", "Stop here");
        }
        // -----------------------------------------------------------------------------------------
        // p.14's Attack text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of his six Attack faces applies at the p.14 moment
        /// <see cref="AttackTextTimingFor"/> names, or <c>null</c> for a face whose whole printed text
        /// is its targeting sentence - which is what his three plain crossbows print.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(BainCardContent.DeadOrAlive))
            {
                return CreateBountyGive(cardId, contentRevision);
            }

            if (cardId.Equals(BainCardContent.HandCrossbow))
            {
                return CreateMarkerModal(cardId, contentRevision, mayTakeBoth: false);
            }

            if (cardId.Equals(BainCardContent.HunterSeeker))
            {
                return CreateMarkerModal(cardId, contentRevision, mayTakeBoth: true);
            }

            return null;
        }

        /// <summary>
        /// Which p.14 moment one of his Attack faces applies its text at.
        ///
        /// <para>His Gold basic prints "After the attack:" in as many words. His two modal Reds print a
        /// targeting sentence, which is step 1 - the action reaches it with no target and the modal
        /// answers with bullets, which is Dodger's four modal Attacks' own timing.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(BainCardContent.DeadOrAlive))
            {
                return CardTextTiming.AfterTheAttack;
            }

            if (cardId.Equals(BainCardContent.HandCrossbow) ||
                cardId.Equals(BainCardContent.HunterSeeker))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            return CardTextTiming.None;
        }

        // -----------------------------------------------------------------------------------------
        // p.12: the Skills
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of his Skill faces resolves as, or <c>null</c> for the one face that carries a
        /// composition debt: p.20's Handicap card, which p.6 returns to the box.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(BainCardContent.GetOverHere))
            {
                return CreatePull(cardId, contentRevision);
            }

            // The three guessing faces print Radius 2, 3 and 3, and the disk is NOT a number any of
            // these bodies carries: `synthetic-radius-hero-target@1` reads the acting card's own
            // printed Radius with p.16's items composed onto it, which is what a card-written distance
            // could not do. What separates the three here is the reward and the failure branch.
            if (cardId.Equals(BainCardContent.AGameOfChance))
            {
                return CreateGuess(cardId, contentRevision, coins: 1, failureIsModal: false);
            }

            if (cardId.Equals(BainCardContent.DeadMansHand))
            {
                return CreateGuess(cardId, contentRevision, coins: 2, failureIsModal: false);
            }

            if (cardId.Equals(BainCardContent.WereNotDoneYet))
            {
                return CreateGuess(cardId, contentRevision, coins: 2, failureIsModal: true);
            }

            if (cardId.Equals(BainCardContent.DrinkingBuddies))
            {
                return CreateSharedRetrieval(cardId, contentRevision, armsEndOfTurnRepeat: false);
            }

            if (cardId.Equals(BainCardContent.AnotherOne))
            {
                return CreateSharedRetrieval(cardId, contentRevision, armsEndOfTurnRepeat: true);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // p.14 step 4: the Defense text his two consequential Blocks print
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of his three Green Blocks applies at p.14's step 4, or <c>null</c> for the one
        /// that prints a condition and no consequence at all.
        ///
        /// <para><b>Perfect Getaway is the control and that is the point of having three.</b> It proves
        /// the condition alone, Close Call proves the transfer arm, and Narrow Escape proves the return
        /// arm.</para>
        /// </summary>
        public static EffectDefinition DefenseTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(BainCardContent.CloseCall))
            {
                return CreateBlockRider(cardId, contentRevision, transfersTheMarker: true);
            }

            if (cardId.Equals(BainCardContent.NarrowEscape))
            {
                return CreateBlockRider(cardId, contentRevision, transfersTheMarker: false);
            }

            return null;
        }

        /// <summary>
        /// Which of p.14's moments a Block's composed text is applied at. The page numbers the
        /// defender's own text step 4 - "Defender applies Defense action text, if any" - and that is the
        /// only moment a Defense action has, which milestone 8.40g settled against Wasp's two shipped
        /// riders rather than by reading the word "and".
        /// </summary>
        public static CardTextTiming DefenseTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(BainCardContent.CloseCall) || cardId.Equals(BainCardContent.NarrowEscape)
                ? CardTextTiming.DefenseAction
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.11: the Movement text his other two Greens print
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition MovementTextFor(
            CardDefinitionId cardId, string contentRevision) =>
            cardId.Equals(BainCardContent.VantagePoint) || cardId.Equals(BainCardContent.HighGround)
                ? CreateStride(cardId, contentRevision)
                : null;

        /// <summary>
        /// Which side of the walk a Movement face's text belongs to, decided by the face's own words.
        ///
        /// <para>p.11 states the default and allows the exception in one sentence: "Unless it says
        /// otherwise, the card text comes into effect before movement." Neither of his says otherwise,
        /// and both would be nonsense if they did - a permission granted after the walk permits
        /// nothing, and a raise applied after it raises an allowance already spent.</para>
        /// </summary>
        public static CardTextTiming MovementTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(BainCardContent.VantagePoint) || cardId.Equals(BainCardContent.HighGround)
                ? CardTextTiming.BeforeTheMovement
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // The triggered effects
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// p.16's permanent ability: "After you give a hero the [bounty] bounty marker that hero
        /// discards a card, if able."
        ///
        /// <para><b>The whole first half of the sentence is a declaration rather than a node.</b>
        /// "After you give a hero the bounty marker" is
        /// <see cref="UltimateTriggerCondition.AfterControllerGivesMarker"/> watching
        /// <see cref="TimingCheckpoint.MarkerGivenToHero"/>, both minted at milestone 8.40h, and the
        /// "you" is the whole of what it narrows: a transfer, a return and a give another hero
        /// performed all reach nothing, which is BA-D5.</para>
        ///
        /// <para><b>The recipient is a ROLE and not a selection.</b> The give already picked the hero
        /// out and the trigger's frame carries them as
        /// <see cref="EffectRoleKind.OpposingHero"/> - the same binding Sabina's Big Sodding Gun reads
        /// its pushed hero through - so offering a one-candidate prompt here would invent a decision
        /// the card does not print.</para>
        ///
        /// <para><b>"if able" is the skipping kind</b>, which is revision 1's whole contract: a
        /// recipient holding nothing gives nothing up and the ability is over.</para>
        /// </summary>
        public static EffectDefinition AComplicatedProfessionTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("profession-root");
            var discard = new EffectNodeId("profession-discard");

            return new EffectDefinition(
                new EffectDefinitionId(BainCardContent.AComplicatedProfession.Value + "-ultimate"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { discard }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                SyntheticCardStateCapabilities.HeroParameter,
                                EffectRoleKind.OpposingHero,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// "<b>End of turn:</b> May repeat once." - Another One!'s second sentence, run by the Active
        /// effect its first sentence armed.
        ///
        /// <para><b>It is a hand-written duplicate of the retrieval graph and that is the cost of the
        /// keyword</b>, not an oversight: <c>EffectNodeKind.RepeatAction</c> re-runs an enclosing
        /// ACTION and there is none at <see cref="TimingCheckpoint.TurnEnded"/>. Tali's Blizzard pays
        /// the same four costs for the same reason. The duplicate is built by the same private helper
        /// the Skill's own body is, so the two cannot drift.</para>
        ///
        /// <para><b>The repeat's own "May" is the Optional above it</b>, answered by the card's
        /// controller when the turn ends, and the clause repeats the whole printed sentence - the
        /// selection, the other hero's retrieval and his own - because "repeat once" names no part of
        /// it.</para>
        /// </summary>
        public static EffectDefinition AnotherOneRepeatTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("another-one-repeat-root");
            var optional = new EffectNodeId("another-one-repeat-optional");
            var sequence = new EffectNodeId("another-one-repeat-sequence");

            return new EffectDefinition(
                new EffectDefinitionId(BainCardContent.AnotherOne.Value + "-end-of-turn-repeat"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { optional }),
                    new EffectNodeDefinition(
                        optional, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { sequence },
                        chooser: EffectChooserRole.Actor)
                }
                .Concat(new[]
                {
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: SharedRetrievalClauseIds(RepeatPrefix))
                })
                .Concat(SharedRetrievalBody(RepeatPrefix))
                .ToArray());
        }

        /// <summary>
        /// Every triggered effect Bain's content declares, for a fixture or a composition root that
        /// configures them: p.16's permanent ability, and the End-of-turn repeat Another One! arms.
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects(string contentRevision) =>
            Array.AsReadOnly(new[]
            {
                AComplicatedProfessionTriggeredEffect(contentRevision),
                AnotherOneRepeatTriggeredEffect(contentRevision)
            });

        // -----------------------------------------------------------------------------------------
        // The bodies
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "After the attack: You may give an enemy hero in radius a [bounty] bounty marker; a hero
        /// with a bounty marker spends 1 additional [life] life counter when defeated." - Dead or
        /// Alive, his Gold basic.
        ///
        /// <para><b>The second sentence is composed NOWHERE, and that is the marker's own design.</b>
        /// It is a property of the attachment - <c>BainComponentContent.BountyDefinition</c> declares
        /// <c>RuleModification.ForAdditionalDefeatLifeCostWhenDefeated()</c> - so it is true while the
        /// marker is held, by whoever holds it, and stops when p.8 or p.13 returns it, without any node
        /// of any card saying so. BA-13 records why it is a twelfth rule family rather than Ursafar's
        /// Tear read twice: his charges the hero he DEFEATED, and a marker charges its holder whoever
        /// that is.</para>
        ///
        /// <para><b>The "may" is the clause's and not the selection's.</b> p.10 gives an unassigned
        /// decision to the card's controller, and an <see cref="EffectNodeKind.Optional"/> above the
        /// pair is what lets him decline without first being made to name a hero - and what silently
        /// skips the whole clause on a board with no enemy hero in radius, which is milestone 8.29e's
        /// rule rather than this card's.</para>
        ///
        /// <para><b>The give is the LAST step of its clause, and that is a bound rather than a
        /// preference.</b> Milestone 8.40h's trigger stands at the card-text altitude and stops the
        /// page there, so a clause that gave and then did something else would need a
        /// continue-after-interrupt neither that moment nor the after-push moment has. Both of his
        /// giving sentences end on the give, so it costs him nothing.</para>
        ///
        /// <para><b>And it declares BA-D1's move.</b> His supply is one, so from the second give
        /// onwards the marker is already on the board: <c>movesWhenSupplyIsEmpty</c> is what makes that
        /// give MOVE it rather than report itself impossible, which is what happens at a physical
        /// table and what keeps his own Gold basic from silently losing its printed clause after one
        /// use.</para>
        /// </summary>
        private static EffectDefinition CreateBountyGive(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("bounty-root");
            var may = new EffectNodeId("bounty-may");
            var sequence = new EffectNodeId("bounty-sequence");
            var select = new EffectNodeId("bounty-select");
            var give = new EffectNodeId("bounty-give");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { may }),
                    new EffectNodeDefinition(
                        may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { sequence },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, give }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            // The text runs inside an Attack, so p.18's immunity is asked about the
                            // Attack rather than about a Skill this card does not perform.
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),

                            // The face prints no "not adjacent to you", so nothing is taken out of
                            // p.9's disk - and the unit he just attacked is as legal a recipient as
                            // any other, which is what "an enemy hero in radius" says.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(give))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        give, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        BainComponentContent.GiveBountyV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticComponentCapabilities.MarkerGivingHandler.HolderParameterName,
                                select,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral(
                                SyntheticComponentCapabilities.MarkerGivingHandler
                                    .MovesWhenSupplyIsEmptyParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("holder", EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "Choose one[, or both, on different targets] — • Target a hero in range with a [bounty]
        /// bounty marker. • Target a unit adjacent to you." - Hand Crossbow at one bullet and
        /// Hunter-Seeker at one or both.
        ///
        /// <para><b>Both compose as <c>attack-sequence@1</c> bullets, and the audit row that sent the
        /// Tier II face to <c>set-attack-target@1</c> was wrong for a measurable reason.</b> The marker
        /// narrowing exists in exactly two places: on <c>choose-target@2</c>, whose reach is an integer
        /// the definition writes down, and on the attack-sequence BULLET, whose reach is
        /// <see cref="AttackTargetReach.PrintedRange"/> - the card's own Range icon with p.16's items
        /// composed onto it. Hand Crossbow prints "in range", and BA-15 records that his Blue Tier III
        /// item exists to raise exactly the Range his Red and Silver faces print, so a bullet carrying
        /// a written 3 would have made that item raise everything except the face it was printed
        /// for.</para>
        ///
        /// <para><b>What separates the two faces is the modal's cardinality and nothing else</b>, which
        /// is what their printed sentences differ in: "Choose one" is 1..1 and "Choose one, or both" is
        /// 1..2, which is p.10's Modal Actions read literally. BA-D4 follows Dodger's D-06 ruling -
        /// each chosen bullet is a complete p.14 order with its own targeting, its own Defense window
        /// and its own Attack Total - and the whole of it is ONE action.</para>
        ///
        /// <para><b>"(on different targets)" is printed on one face and not the other</b>, so
        /// Hunter-Seeker declares it on both of its bullets and Hand Crossbow declares it false on
        /// both: RULING 19's rider is that a card printing no such parenthetical may point its bullets
        /// at one unit, and a face that can only ever take one bullet is saying nothing either
        /// way.</para>
        /// </summary>
        private static EffectDefinition CreateMarkerModal(
            CardDefinitionId cardId, string contentRevision, bool mayTakeBoth)
        {
            var modal = new EffectNodeId("crossbow-modal");
            var markerBullet = new EffectNodeId("crossbow-marker-bullet");
            var adjacentBullet = new EffectNodeId("crossbow-adjacent-bullet");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { markerBullet, adjacentBullet },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: mayTakeBoth ? 2 : 1),

                    // "Target a hero in range with a [bounty] bounty marker." - heroes only, because
                    // that is the noun the bullet prints and because a minion wears no marker of his.
                    BulletNode(
                        markerBullet,
                        AttackTargetableUnits.HeroesOnly,
                        AttackTargetReach.PrintedRange,
                        AttackSequenceSubjectNarrowing.HoldsTheActorsMarker,
                        mayTakeBoth),

                    // "Target a unit adjacent to you." - p.19's units at p.19's innermost ring, and no
                    // narrowing at all, which the bullet says by printing nothing.
                    BulletNode(
                        adjacentBullet,
                        AttackTargetableUnits.HeroesAndMinions,
                        AttackTargetReach.AdjacentOnly,
                        AttackSequenceSubjectNarrowing.None,
                        mayTakeBoth)
                });
        }

        /// <summary>
        /// "Target a unit or a token in a straight line and in range with no obstacles between you and
        /// the target; move the target towards you in a straight line, until you are adjacent." - Get
        /// over here!, his Silver basic.
        ///
        /// <para><b>The selection is the obstacle walk with its terrain narrowing switched on</b>, and
        /// the two halves of that sentence are deliberately different sets: "no obstacles" is p.18's
        /// full list, so a terrain space or a token still STOPS the walk, while "a unit or a token" is
        /// what may be NAMED. A file whose first obstacle is terrain therefore offers nothing rather
        /// than offering what stands behind it, which is BA-03's whole point - a token blocks this pull
        /// and does not block a crossbow.</para>
        ///
        /// <para><b>The Move moves the TARGET and not the actor, which is the mirror of Cutter's
        /// Grappling Bolt and why 8.40e published a second approach space.</b> The mover is the space
        /// the selection named and the destination is
        /// <c>actorApproachSpace</c> - the space on that same file that leaves the target adjacent to
        /// Bain. Re-deriving the line here would be a second source of truth about one step.</para>
        ///
        /// <para><b>The Move is "if able" for one position only</b>, and it is a real one: a target
        /// already adjacent is published its own space, and a Move to where its mover already stands
        /// has no destination to offer. That is a step not taken rather than a Skill that stops - which
        /// is the printed sentence's own reading of "until you are adjacent".</para>
        ///
        /// <para>The Move's declared distance is a budget rather than a count, because the destination
        /// is named; six is chosen so it cannot under-serve the longest approach p.16 can produce from
        /// a printed Range of four.</para>
        /// </summary>
        private static EffectDefinition CreatePull(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("pull-root");
            var target = new EffectNodeId("pull-target");
            var move = new EffectNodeId("pull-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, move }),
                    new EffectNodeDefinition(
                        target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeObstacleTargetV1,
                        inputs: new[]
                        {
                            // A Skill's own activity, since there is no enclosing Attack to inherit one
                            // from - and the face overrides no immunity, so p.18's keyword stands.
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTerrainParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                                EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ActorApproachSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", target,
                                SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(6)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                target,
                                SyntheticTargetingCapabilities.ActorApproachSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "An enemy hero in radius with two or more cards in hand chooses one of those cards; Guess
        /// the card's color, then reveal the card; If you guessed correctly, discard that card;
        /// otherwise you gain N coin(s)[ or may repeat once]." - A Game of Chance at Radius 2 and one
        /// coin, Dead Man's Hand at Radius 3 and two, We're Not Done Yet! at Radius 3 with the modal.
        ///
        /// <para><b>Five nodes in the printed order, and the order is the rule.</b> The selection names
        /// a hero; that hero picks one of their own hand cards and KEEPS it; Bain names a colour
        /// against a domain public information admits; the card is turned face up for every seat and
        /// the verdict is published on the same node; and the verdict branches. A composition that
        /// revealed before it guessed would make the guess free, which is what the printed semicolons
        /// order.</para>
        ///
        /// <para><b>"with two or more cards in hand" narrows the SELECTION rather than gating the
        /// clause</b>, which is milestone 8.40f's <c>minimumCardsInHand</c>: a hero holding one card is
        /// not one to offer and then refuse. It discloses nothing, because every seat has been shown
        /// every hero's hand COUNT since milestone 8.30a.</para>
        ///
        /// <para><b>The guess domain never reads a hand</b> (BA-D2). <c>restrictToColorsOwnedBy</c>
        /// binds the chosen hero, and what the capability derives from them is their printed deck minus
        /// the colours the OPEN zones fully account for, minus Purple - so the offer is identical
        /// whichever card was hidden.</para>
        ///
        /// <para><b>The discard names its card rather than asking for one</b>, through
        /// <c>restrictToCard</c>: "discard THAT card" has already named the one legal answer, and a
        /// prompt here would let the chooser spend a different card after the guess had earned this
        /// one. Revision 1 is the right revision for a sentence that ENDS there - revision 2 exists for
        /// one that does not, which is Cutter's push rider rather than this.</para>
        ///
        /// <para><b>The failure modal is one <c>Choose</c> over a repeat and a grant</b>, reached only
        /// from the else-branch, and BA-Q2 is settled by the printed sentence: "otherwise may repeat
        /// once" names no hero and no exclusion, so a second run re-asks every question the first one
        /// asked. The repeat bullet is greyed where no second instance could run, which milestone
        /// 8.40f built and this face is the only consumer of.</para>
        /// </summary>
        private static EffectDefinition CreateGuess(
            CardDefinitionId cardId,
            string contentRevision,
            int coins,
            bool failureIsModal)
        {
            var root = new EffectNodeId("guess-root");
            var select = new EffectNodeId("guess-select");
            var hide = new EffectNodeId("guess-hide");
            var guess = new EffectNodeId("guess-name-colour");
            var reveal = new EffectNodeId("guess-reveal");
            var branch = new EffectNodeId("guess-branch");
            var discard = new EffectNodeId("guess-discard");
            var modal = new EffectNodeId("guess-modal");
            var repeat = new EffectNodeId("guess-repeat");
            var coinBullet = new EffectNodeId("guess-coin-bullet");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { select, hide, guess, reveal, branch }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
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
                            SyntheticTargetingCapabilities.MinimumCardsInHandParameterName,
                            EffectValue.Integer(2)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(hide))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }),
                new EffectNodeDefinition(
                    hide, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticHiddenChoiceCapabilities.HiddenCardChoiceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticHiddenChoiceCapabilities.HiddenChoiceHeroParameter,
                            select,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticHiddenChoiceCapabilities.ChosenCardOutput,
                            EffectValueKind.CardInstance)
                    }),
                new EffectNodeDefinition(
                    guess, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticBorrowedActionCapabilities.NameColorV1,
                    inputs: new[]
                    {
                        // "GUESS the card's color" - the naming is his, which is what "you" means on a
                        // card, and the domain is the chooser's own public deck.
                        EffectInputBinding.FromRole(
                            SyntheticBorrowedActionCapabilities.NameColorChooserParameter,
                            EffectRoleKind.Actor,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticBorrowedActionCapabilities.RestrictToColorsOwnedByParameter,
                            select,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticBorrowedActionCapabilities.NamedColorOutput,
                            EffectValueKind.Integer)
                    }),
                new EffectNodeDefinition(
                    reveal, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticHiddenChoiceCapabilities.RevealChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticHiddenChoiceCapabilities.RevealCardParameter,
                            hide,
                            SyntheticHiddenChoiceCapabilities.ChosenCardOutput,
                            EffectValueKind.CardInstance),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticHiddenChoiceCapabilities.NamedColorParameter,
                            guess,
                            SyntheticBorrowedActionCapabilities.NamedColorOutput,
                            EffectValueKind.Integer,
                            EffectIntegerDomain.All<CardColor>())
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticHiddenChoiceCapabilities.RevealedColorOutput,
                            EffectValueKind.Integer),
                        new EffectOutputBinding(
                            SyntheticHiddenChoiceCapabilities.MatchedOutput,
                            EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    branch, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { discard, failureIsModal ? modal : coinBullet },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", reveal,
                            SyntheticHiddenChoiceCapabilities.MatchedOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter,
                            select,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.RestrictToCardParameter,
                            hide,
                            SyntheticHiddenChoiceCapabilities.ChosenCardOutput,
                            EffectValueKind.CardInstance)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    }),
                new EffectNodeDefinition(
                    coinBullet, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    })
            };

            if (failureIsModal)
            {
                nodes.Add(new EffectNodeDefinition(
                    modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { repeat, coinBullet },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1));
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "You may have a hero in radius retrieve a discarded card. If they do, you may also retrieve
        /// a discarded card[. End of turn: May repeat once]." - Drinking Buddies at Tier II and Another
        /// One! at Tier III, which print BYTE-IDENTICAL first sentences at the SAME Radius (BA-12).
        ///
        /// <para><b>BA-Q1 IS SETTLED HERE AND PROVED ON A BOARD WHERE THE READINGS DIFFER: the first
        /// hero is any hero on the board EXCEPT Bain, enemies included.</b> Two printed facts decide
        /// it. "a hero" is unqualified, and this card set's convention is that an unqualified hero is
        /// every hero - p.19 needed a special word for "friendly" and this face does not use it, which
        /// is RULING 8's structure applied to a different card. And the second sentence says "you may
        /// <b>also</b> retrieve", which is a word that means nothing unless the first retrieval was
        /// somebody else's; a reading that let Bain pick himself would make the whole card "retrieve
        /// two cards" and the word "also" redundant. So the selection names
        /// <see cref="SourceTeamRelation.AnyTeam"/> with <c>includeSelf</c> false, and p.21's Healer
        /// role - "Healers help their allies by letting them retrieve" - is what the first half is
        /// for.</para>
        ///
        /// <para><b>"If they do" is a <c>Conditional</c> over <c>synthetic-is-flagged@1</c>, and the
        /// audit row named the right contract over the wrong FACT.</b> That row read the flag off
        /// <c>retrieved</c>, which is a card instance; the capability's <c>flag</c> parameter is a
        /// Boolean and the definition validator refuses the binding outright. Silverarrow's Fae Healing
        /// needs no Boolean because its consequence is about the hero who retrieved - the binding IS
        /// the condition there - and Bain's second sentence is about HIMSELF, so nothing in the
        /// dependent step can be bound from the first one. Milestone 8.40i therefore added the count
        /// form of the fact, <c>didRetrieve</c>, exactly as <c>discard-owned-card@1</c> already carries
        /// <c>didDiscard</c> for the identical grammar, and a decline publishes nothing - which an
        /// unbound flag reads as false.</para>
        ///
        /// <para><b>An <see cref="EffectNodeKind.Optional"/> gated by a hero-consuming node was
        /// measured and refused.</b> The walk that decides whether to OFFER an optional branch treats a
        /// node whose required input is not yet bound as undecidable and therefore performable, so a
        /// hero who declined still opened "you may also retrieve" and the step behind it then refused
        /// itself. A wasted decision is not a condition, and a mandatory gate is worse: it stops the
        /// resolution, and Another One! has one more thing to do afterwards - arming its End-of-turn
        /// effect, which its printed sentence does unconditionally.</para>
        ///
        /// <para><b>One bounded fidelity note, inherited rather than introduced.</b> The printed "may"
        /// is Bain's, and a <see cref="EffectFailurePolicy.May"/> on the retrieval opens p.10's
        /// confirm-or-decline on the card prompt the RETRIEVING hero answers - so the hero is named
        /// before the offer can be declined. Silverarrow's Fae Healing records the same wrinkle from
        /// the other side, and directing an enclosing policy's decline to a different seat is
        /// vocabulary this build does not have.</para>
        /// </summary>
        private static EffectDefinition CreateSharedRetrieval(
            CardDefinitionId cardId, string contentRevision, bool armsEndOfTurnRepeat)
        {
            var root = new EffectNodeId("buddies-root");
            var arm = new EffectNodeId("buddies-arm-repeat");

            var children = armsEndOfTurnRepeat
                ? SharedRetrievalClauseIds(SkillPrefix).Concat(new[] { arm }).ToArray()
                : SharedRetrievalClauseIds(SkillPrefix);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children)
            };
            nodes.AddRange(SharedRetrievalBody(SkillPrefix));

            if (armsEndOfTurnRepeat)
            {
                // Another One!'s second sentence is armed here and asked at the turn's end, exactly as
                // Tali's Blizzard arms hers as the last step of its own Skill.
                nodes.Add(new EffectNodeDefinition(
                    arm, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    BainActiveEffectContent.AnotherOneEndOfTurnRepeatV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>The Skill's own node prefix, and the End-of-turn repeat's.</summary>
        private const string SkillPrefix = "buddies";

        private const string RepeatPrefix = "another-one-repeat";

        /// <summary>
        /// The three steps the printed sentence orders, as the ids a parent lists: name a hero, have
        /// them retrieve, and then - only if they did - retrieve one yourself.
        /// </summary>
        private static EffectNodeId[] SharedRetrievalClauseIds(string prefix) => new[]
        {
            new EffectNodeId(prefix + "-select"),
            new EffectNodeId(prefix + "-retrieve"),
            new EffectNodeId(prefix + "-also")
        };

        /// <summary>
        /// The retrieval clause's own nodes, under a parent the caller owns - the Skill's
        /// <c>Sequence</c>, or the <c>Optional</c> the End-of-turn repeat runs it under. Written once
        /// so the printed sentence and its own repeat cannot drift apart.
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> SharedRetrievalBody(string prefix)
        {
            var select = new EffectNodeId(prefix + "-select");
            var retrieve = new EffectNodeId(prefix + "-retrieve");
            var also = new EffectNodeId(prefix + "-also");
            var retrieveSelf = new EffectNodeId(prefix + "-also-retrieve");

            return new[]
            {
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),

                        // BA-Q1: "a hero" with no side-word is every hero, and the "also" in the second
                        // sentence is what takes Bain himself out of the first.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AnyTeam),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(retrieve))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }),
                new EffectNodeDefinition(
                    retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticRosterCapabilities.RetrieveCardV1,
                    inputs: new[]
                    {
                        // The card comes from that hero's own discard pile and they choose it, which is
                        // p.10's rule that a step's choice belongs to whoever performs it.
                        EffectInputBinding.FromNodeOutput(
                            "owner", select,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(
                            "chooser", select,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("retrieved", EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.DidRetrieveOutputName,
                            EffectValueKind.Boolean)
                    }),

                // "If they do" - the fact the retrieval publishes ONLY on success, read in the shape a
                // Conditional takes. A decline publishes nothing, and an unbound flag is false, which
                // is exactly what the printed "if" asks for.
                new EffectNodeDefinition(
                    also, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { retrieveSelf },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", retrieve,
                            SyntheticRosterCapabilities.DidRetrieveOutputName,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    retrieveSelf, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticRosterCapabilities.RetrieveCardV1,
                    inputs: new[]
                    {
                        // "you may ALSO retrieve a discarded card" - p.10 is explicit that "you can
                        // never retrieve another hero's cards", and the card names no other subject.
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
            };
        }

        /// <summary>
        /// "If a hero in play has a [bounty] bounty marker, block the attack and that hero gives the
        /// marker to you / and retrieve the marker." - Close Call's Defense text and Narrow Escape's.
        ///
        /// <para><b>The condition is declared TWICE and both readings are the same question.</b> The
        /// Block's own resolution reads <c>PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker</c>
        /// off the action, and this text reads the board again through 8.40c's predicate - which is
        /// exactly how Wasp's two shipped "if you do" riders work, because p.14 applies the defender's
        /// text at step 4 whatever step 6 will decide. Milestone 8.40g measured that and refused to
        /// make step 4 itself conditional, which would have changed six shipped faces to buy
        /// nothing.</para>
        ///
        /// <para><b>The predicate IS the <c>Conditional</c>'s capability, and the audit row that put
        /// <c>synthetic-is-flagged@1</c> between them was describing a different problem.</b>
        /// <c>bain-bounty-marker-held@1</c> publishes a Boolean itself, so a branch that needs nothing
        /// else from it can hang directly off it; 8.40c's rule - a Conditional's branch cannot bind
        /// that Conditional's own output - bites only where the branch names "that hero", and neither
        /// of these two does.</para>
        ///
        /// <para><b>"That hero gives the marker to you" binds the DESTINATION and derives the
        /// source.</b> <c>MarkerTransferHandler</c>'s <c>holder</c> is the hero the marker moves TO,
        /// which is the defender - the frame's own actor - and the hero it moves FROM is whoever is
        /// wearing the one attachment, which the handler reads for itself. That is also why it is a
        /// transfer rather than a return-then-give: a give would mint a second marker identity and
        /// would fire his own Ultimate on a clause that says another hero gave it (BA-D5).</para>
        ///
        /// <para><b>"Retrieve the marker" is the rulebook's word for returning one to the supply</b>,
        /// and Narrow Escape gave none - so it is milestone 8.40b's definition-bound reading of
        /// <c>ReturnMarkerHandler</c>: the one attachment of THAT definition, wherever it sits.</para>
        /// </summary>
        private static EffectDefinition CreateBlockRider(
            CardDefinitionId cardId, string contentRevision, bool transfersTheMarker)
        {
            var root = new EffectNodeId("block-root");
            var consequence = new EffectNodeId("block-consequence");

            return new EffectDefinition(
                DefenseTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        BainComponentContent.BountyHeldV1,
                        childNodeIds: new[] { consequence },
                        outputs: new[]
                        {
                            // A predicate that publishes its answer has to have the node declare it, or
                            // a restore re-deriving the recorded outcome finds an output the node does
                            // not admit and refuses the payload.
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean)
                        }),
                    transfersTheMarker
                        ? new EffectNodeDefinition(
                            consequence, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            BainComponentContent.TransferBountyV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromRole(
                                    SyntheticComponentCapabilities.MarkerTransferHandler
                                        .HolderParameterName,
                                    EffectRoleKind.Actor,
                                    EffectValueKind.Hero)
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding("holder", EffectValueKind.Hero),
                                new EffectOutputBinding(
                                    SyntheticComponentCapabilities.MarkerTransferHandler
                                        .PreviousHolderOutputName,
                                    EffectValueKind.Hero)
                            })
                        : new EffectNodeDefinition(
                            consequence, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            BainComponentContent.ReturnBountyV1,
                            outputs: new[]
                            {
                                new EffectOutputBinding("returned", EffectValueKind.Hero)
                            })
                });
        }

        /// <summary>
        /// "Ignore obstacles. If a hero in play has a [bounty] bounty marker, +N [movement] movement." -
        /// Vantage Point at one and High Ground at two.
        ///
        /// <para><b>Two sentences, two Active effects, and the gate belongs to one of them.</b> The
        /// permission is unconditional and the raise is not, so a single effect carrying both
        /// modifications would take the obstacle permission away on a board with no marker out - a rule
        /// neither face prints. Both are created before the walk, which is p.11's default and the only
        /// reading under which either does anything.</para>
        ///
        /// <para><b>The raise is an Active effect rather than a roster contribution</b> because
        /// <c>RosterValueContributionDefinition</c>'s constructor refuses the Movement channel outright,
        /// which milestone 8.40 measured. See <see cref="BainActiveEffectContent"/> for BA-Q3's scope
        /// answer and why the turn is the shortest window this build can express.</para>
        /// </summary>
        private static EffectDefinition CreateStride(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("stride-root");
            var permission = new EffectNodeId("stride-ignore-obstacles");
            var gate = new EffectNodeId("stride-gate");
            var raise = new EffectNodeId("stride-raise");

            return new EffectDefinition(
                MovementTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { permission, gate }),
                    new EffectNodeDefinition(
                        permission, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        BainActiveEffectContent.IgnoreObstaclesFor(cardId),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        BainComponentContent.BountyHeldV1,
                        childNodeIds: new[] { raise },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        raise, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        BainActiveEffectContent.MovementRaiseFor(cardId),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        // -----------------------------------------------------------------------------------------
        // Shared nodes
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One bullet of a modal Attack, as an <c>attack-sequence@1</c> leaf. The leaf performs
        /// nothing: it publishes the bullet's printed targeting sentence, and the card-action authority
        /// runs a complete p.14 per chosen bullet against the board as that sequence finds it.
        /// </summary>
        private static EffectNodeDefinition BulletNode(
            EffectNodeId nodeId,
            AttackTargetableUnits targetableUnits,
            AttackTargetReach reach,
            AttackSequenceSubjectNarrowing subjectNarrowing,
            bool distinctTargets)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.TargetableUnitsParameter, targetableUnits),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.ReachParameter, reach),

                // Neither modal prints a line clause at all, which is BA-03's correction to an audit
                // row that had counted five clear-line Reds where the deck prints three.
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.LineParameter, AttackTargetLine.AnyLine),
                EffectInputBinding.FromLiteral(
                    SyntheticSequenceCapabilities.DistinctTargetsParameter,
                    EffectValue.Boolean(distinctTargets))
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

            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSequenceCapabilities.AttackSequenceV1,
                inputs: inputs,
                outputs: outputs);
        }

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");

        private static EffectDefinitionId DefenseTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-defense-text");

        private static EffectDefinitionId MovementTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-movement-text");
    }
}
