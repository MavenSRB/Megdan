using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Dodger's printed card text, composed as typed effect definitions. Milestone 8.35z28.
    ///
    /// <para>It sits beside <see cref="DodgerCardContent"/> for the reason the six earlier heroes' pairs
    /// do: that file is the transcription and this one is the composition. A reader auditing what the
    /// cards say reads one; a reader auditing what the engine does with it reads the other. Nothing here
    /// parses printed text, names a hero in a rule, or reads a card by name at runtime.</para>
    ///
    /// <para><b>Eleven of her nineteen faces compose here, and eight compose nothing.</b> Rusty Razor is
    /// p.20's Handicap card and carries the standing named deferral. Seven more are parked on the user's
    /// open decision-list questions - D-02/D-03 (the Ultimate), D-04 (Darkest Ritual), D-06
    /// (Middlefinger of Death), D-07 (the Necromancy pair) and D-08/D-09 (Weakness and Enfeeblement) -
    /// and each names its question through <c>PrintedTextComposition.DeferredUntil</c> on the card. A
    /// deferred composition and a composed body are mutually exclusive, so a parked face parks whole
    /// rather than shipping half a sentence.</para>
    ///
    /// <para><b>Three of her faces print a <c>+</c> and compose nothing here.</b> Shield of Decay,
    /// Vampiric Shield and Aegis of Doom raise their own printed Defense, and a value contribution is
    /// not a step: nobody chooses, nothing moves on the board, and the clause is read when p.14 computes
    /// the Defense Total. Those declarations are on the action in <see cref="DodgerCardContent"/>.</para>
    ///
    /// <para><b>What is new here is where the board condition sits in each sentence.</b> The identical
    /// printed phrase - "an empty spawn point in the battle zone" - is a <em>gate</em> on Dread Razor's
    /// second bullet and on the two composed rituals, and a <em>candidate narrowing</em> on Death Trap,
    /// where it qualifies the enemy hero rather than the actor. One derivation answers all four, and it
    /// is reached through two capability declarations that differ only in where their reach comes from:
    /// p.9's printed Radius icon, which p.16's item raises, or p.19's innermost ring, which the sentence
    /// writes down.</para>
    /// </summary>
    public static class DodgerEffectContent
    {
        private const string SelectedCountOutput = "selectedCount";
        private const string FirstSpaceOutput = "firstSpace";
        private const string SecondSpaceOutput = "secondSpace";
        private const string MovedOutput = "moved";
        private const string FlagParameter = "flag";
        private const string GateValueOutput = "value";

        /// <summary>
        /// The number every one of her counting faces prints. It is written once here rather than at
        /// each call so a reader can see that all four say "2 or more" and none says anything else;
        /// the two Gold cards ask about one point and declare that separately, because "adjacent to an
        /// empty spawn point" is a different printed number rather than a variation on this one.
        /// </summary>
        private const int PrintedSpawnPointThreshold = 2;

        // -----------------------------------------------------------------------------------------
        // p.14 step 1: the attacker's "Choose one —" targeting, and step 7's "After the attack:"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Attack of Dodger's applies, or <c>null</c> where it prints none or owes
        /// its composition. Paired with <see cref="AttackTextTimingFor"/>, which names which of p.14's
        /// moments that is.
        ///
        /// <para>Four of her seven Attacks compose. Two are modal targeting sentences read at step 1 -
        /// Littlefinger of Death and Finger of Death, whose printings are identical - and Dread Razor is
        /// the third, differing only in what its second bullet is gated on. Burning Skull and Blazing
        /// Skull compose a step-7 rider instead. Middlefinger of Death is parked on D-06 and Rusty Razor
        /// is the boxed Handicap.</para>
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(DodgerCardContent.DreadRazor))
            {
                return CreateSpawnPointGatedModal(cardId, contentRevision);
            }

            if (cardId.Equals(DodgerCardContent.LittlefingerOfDeath) ||
                cardId.Equals(DodgerCardContent.FingerOfDeath))
            {
                return CreateDiscardPileModal(cardId, contentRevision);
            }

            if (cardId.Equals(DodgerCardContent.BurningSkull))
            {
                return CreateShoveRider(cardId, contentRevision, maximumMinions: 1);
            }

            if (cardId.Equals(DodgerCardContent.BlazingSkull))
            {
                return CreateShoveRider(cardId, contentRevision, maximumMinions: 2);
            }

            // Rusty Razor composes nothing, exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike and Xargatha's
            // Desperate Cleave compose nothing, by the same 2026-08-21 user decision: p.6 returns the
            // Handicap card to the box and the six-player pitch never brings it back. What it would
            // need when a milestone deals it is one call to CreateSpawnPointGatedModal, because it
            // prints the identical gated modal - the only difference between the two faces is that both
            // of its bullets name a hero where Dread Razor's name any unit, and that is on the card
            // rather than here.
            //
            // Middlefinger of Death composes nothing for a different reason and it is not a deferral of
            // convenience: p.10's "Choose one or both (on different targets)" is a shape no verified
            // face had printed, and what one Attack action carrying two targets does at p.14's nine
            // numbered steps is open question D-06.
            return null;
        }

        /// <summary>
        /// Which of p.14's moments <see cref="AttackTextFor"/>'s body is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Her three composed modal Attacks apply at step 1, because their bullets <em>are</em>
        /// the targeting sentence and p.14 announces the Attack Total against a target that already
        /// exists. The two Skull cards print "After the attack:" and apply at step 7. No face of hers
        /// prints text at both moments.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(DodgerCardContent.DreadRazor) ||
                cardId.Equals(DodgerCardContent.LittlefingerOfDeath) ||
                cardId.Equals(DodgerCardContent.FingerOfDeath))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            return cardId.Equals(DodgerCardContent.BurningSkull) ||
                cardId.Equals(DodgerCardContent.BlazingSkull)
                    ? CardTextTiming.AfterTheAttack
                    : CardTextTiming.None;
        }

        // -----------------------------------------------------------------------------------------
        // p.12's Skill primaries
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Skill of Dodger's performs, or <c>null</c> where it owes its
        /// composition. Three of her eight Skills compose: the Silver basic and the two rituals whose
        /// whole sentence is a gated coin gain.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(DodgerCardContent.DeathTrap))
            {
                return CreateDeathTrap(cardId, contentRevision);
            }

            if (cardId.Equals(DodgerCardContent.DarkRitual))
            {
                return CreateRitual(cardId, contentRevision, coins: 1);
            }

            if (cardId.Equals(DodgerCardContent.DarkerRitual))
            {
                return CreateRitual(cardId, contentRevision, coins: 2);
            }

            if (cardId.Equals(DodgerCardContent.DarkestRitual))
            {
                return CreateDarkestRitual(cardId, contentRevision);
            }

            if (cardId.Equals(DodgerCardContent.Weakness))
            {
                // p.12: "When you perform a Skill action, simply apply the card text." The whole of
                // this card's text is the `This turn` drain, so its effect is the one node that raises
                // it and there is nothing to sequence it with - Arien's Spell Break exactly.
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    DodgerActiveEffectContent.WeakenEnemyHeroesInRadiusV1, "weakness");
            }

            if (cardId.Equals(DodgerCardContent.Enfeeblement))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    DodgerActiveEffectContent.EnfeebleEnemyHeroesInRadiusV1, "enfeeblement");
            }

            if (cardId.Equals(DodgerCardContent.Necromancy))
            {
                return CreateRespawn(cardId, contentRevision, readsPrintedRadius: false);
            }

            if (cardId.Equals(DodgerCardContent.Necromastery))
            {
                return CreateRespawn(cardId, contentRevision, readsPrintedRadius: true);
            }

            // Weakness, Enfeeblement, Necromancy, Necromastery and Darkest Ritual are parked on D-08,
            // D-09, D-07 and D-04 respectively, and say so on their own actions.
            return null;
        }

        /// <summary>
        /// "Choose one — • Target a unit adjacent to you. • If you are adjacent to an empty spawn point
        /// in the battlezone, target a unit in range." - Dread Razor, and what Rusty Razor would reuse.
        ///
        /// <para><b>The modal is p.10's and it is a node kind, not a branch.</b>
        /// <see cref="EffectNodeKind.Choose"/> is a chooser-owned selection among children and p.10
        /// gives an unassigned decision to the card's controller, so the actor picks a bullet and the
        /// other is ignored - the page's own word for what happens to the one not chosen.</para>
        ///
        /// <para><b>Both bullets end by naming the target, and neither performs the Attack.</b> The card
        /// declares <c>AttackTargetBinding.ByCardText</c>, so the action reaches p.14's step 1 with no
        /// target and no Attack Total; each arm binds one through <c>set-attack-target@1</c>. That is
        /// Brogan's shape exactly, and the reason it is reused rather than restated.</para>
        ///
        /// <para><b>The gate is the user's RULING 3 in composed form.</b> "If X, do Y" whose condition
        /// is false simply skips - it is a condition and not a failable mandatory step - so a Dodger
        /// standing nowhere near an empty minion spawn point takes this arm and binds nothing. p.10 then
        /// stops the Attack for want of a target, which is the identical outcome Brogan's unpayable
        /// range bullet reaches and which ledger question 11 approved: the absence of a bound target
        /// <em>is</em> the stop, and nothing needs to refuse anything.</para>
        ///
        /// <para><b>The adjacency is a written number and not the card's icon.</b> Dread Razor prints
        /// Range 2 and no Radius at all, so there is no printed disk for the clause to mean; "adjacent"
        /// is p.19's innermost ring, which p.16's items never widen. That is why the gate names
        /// <c>empty-minion-spawn-points-adjacent@1</c> and the rituals below name its printed-Radius
        /// twin.</para>
        /// </summary>
        private static EffectDefinition CreateSpawnPointGatedModal(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("razor-mode");
            var adjacentArm = new EffectNodeId("razor-adjacent");
            var adjacentTarget = new EffectNodeId("razor-adjacent-target");
            var adjacentBind = new EffectNodeId("razor-adjacent-bind");
            var rangeArm = new EffectNodeId("razor-range");
            var spawnPoints = new EffectNodeId("razor-spawn-points");
            var gate = new EffectNodeId("razor-gate");
            var rangeSequence = new EffectNodeId("razor-range-sequence");
            var rangeTarget = new EffectNodeId("razor-range-target");
            var rangeBind = new EffectNodeId("razor-range-bind");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentArm, rangeArm },
                        chooser: EffectChooserRole.Actor,
                        // p.10's "Choose one" exactly: one bullet is taken and the other is ignored.
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        adjacentArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentTarget, adjacentBind }),
                    new EffectNodeDefinition(
                        adjacentTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: AdjacentEnemyUnit(),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    BindAttackTargetFromRosterSelection(adjacentBind, adjacentTarget),
                    new EffectNodeDefinition(
                        rangeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { spawnPoints, gate }),
                    new EffectNodeDefinition(
                        spawnPoints, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EmptyMinionSpawnPointsAdjacentV1,
                        // "an empty spawn point" - one is enough, which is the capability's own default,
                        // declared here rather than left implicit so the card's number is on the card.
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.MinimumPresentParameter,
                                EffectValue.Integer(1))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.CountOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    Gate(gate, spawnPoints, rangeSequence),
                    new EffectNodeDefinition(
                        rangeSequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { rangeTarget, rangeBind }),
                    new EffectNodeDefinition(
                        rangeTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        // "target a unit in range" - p.19's units, inside p.9's printed Range with
                        // p.16's items composed onto it. It is the unit-valued printed-Range selection
                        // because the sentence names a unit and the node that consumes it binds the
                        // Attack's target, which is a piece and not a space.
                        SyntheticTargetingCapabilities.RangeUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion)
                        }),
                    BindAttackTargetFromPrintedIconSelection(rangeBind, rangeTarget)
                });
        }

        /// <summary>
        /// "Choose one — • Target a unit adjacent to you. • Target a hero in range who has one or more
        /// cards in the discard." - Littlefinger of Death and Finger of Death, whose printings are
        /// identical and which differ only in Initiative, printed Range and item icon.
        ///
        /// <para><b>The second bullet's clause is a narrowing and not a gate</b>, which is what
        /// separates this shape from Dread Razor's. "who has" qualifies the hero being picked, so a hero
        /// with an empty discard pile is not offered - rather than being offered and then refused, and
        /// rather than the whole bullet being switched off by a board question. That is the reading
        /// milestone 8.21 settled for Brogan's "who has played an attack card this turn" and milestone
        /// 8.22 for Sabina's "adjacent to your friendly minion", and it is the same grammar.</para>
        ///
        /// <para><b>The bullet narrows the kind as well as the pile.</b> The first says "a unit" and the
        /// second says "a hero", so the second arm is the printed-Range <em>hero</em> selection while
        /// the first is the card's own adjacent-unit reach. A bullet offering minions would be a card
        /// this one does not print - and a minion has no hand and no discard pile for the clause to be
        /// about.</para>
        ///
        /// <para>Because p.10 returns every card to its owner's hand at the end of a round, the pile the
        /// clause reads is the current round's: heroes who have defended, or who have been made to
        /// discard. Nothing here has to know that; it falls out of reading live card state.</para>
        /// </summary>
        private static EffectDefinition CreateDiscardPileModal(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("finger-mode");
            var adjacentArm = new EffectNodeId("finger-adjacent");
            var adjacentTarget = new EffectNodeId("finger-adjacent-target");
            var adjacentBind = new EffectNodeId("finger-adjacent-bind");
            var rangeArm = new EffectNodeId("finger-range");
            var rangeTarget = new EffectNodeId("finger-range-target");
            var rangeBind = new EffectNodeId("finger-range-bind");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentArm, rangeArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        adjacentArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentTarget, adjacentBind }),
                    new EffectNodeDefinition(
                        adjacentTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: AdjacentEnemyUnit(),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    BindAttackTargetFromRosterSelection(adjacentBind, adjacentTarget),
                    new EffectNodeDefinition(
                        rangeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { rangeTarget, rangeBind }),
                    new EffectNodeDefinition(
                        rangeTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            // "other than the attacker" is the Range hero arm's own declared narrowing
                            // and neither of these faces prints it. Declared false rather than left to a
                            // default, because the schema requires it and a card that means no should
                            // say so.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            // "who has one or more cards in the discard".
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.HasCardInDiscardParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        rangeBind, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticContinuationCapabilities.SetAttackTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.HeroParameter, rangeTarget,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                                EffectValueKind.Minion)
                        })
                });
        }

        /// <summary>
        /// "Target a unit in range. After the attack: Move up to N minion(s) adjacent to you 1 space to
        /// a space not adjacent to you." - Burning Skull at one and Blazing Skull at two.
        ///
        /// <para><b>The first sentence composes nothing.</b> "Target a unit in range" is p.13's own
        /// reading of a Ranged Attack and the action's own targeting already does it, which is why
        /// neither of these faces binds its target by card text where the three modal Attacks do.</para>
        ///
        /// <para><b>"minion" is unqualified and stays that way.</b> p.19's Friendly entry makes "a
        /// friendly unit" mean another unit on your team, so a bare "minion" is any minion - hers as
        /// readily as the enemy's. The absence of a side-word is the answer, exactly as it is for Wasp's
        /// Lift Up, and the selection therefore names <see cref="SourceTeamRelation.AnyTeam"/>. Nothing
        /// on either face overrides a Heavy minion's p.13 immunity, so it stands.</para>
        ///
        /// <para><b>"up to N" is the selection's own count</b>, so one node asks for between zero and N
        /// of the minions standing beside her and a player naming none is a legal answer rather than a
        /// failure. Blazing Skull's second move is a node of its own rather than a repeat, because the
        /// two are one sentence with one count - a repeat would ask a second question and the card asks
        /// one. That is Sabina's Bullet Hell shape, reused for the identical grammar.</para>
        ///
        /// <para><b>Each move is <c>if able</c>, and that carries two cases at once.</b> A chooser who
        /// named fewer minions than the card allowed leaves the later node nothing bound, and that is
        /// the answer they gave rather than a step that failed. A named minion boxed in with no
        /// non-adjacent space to reach is the same silence: the "up to" over the minions makes the whole
        /// clause optional, so a minion that cannot be shoved is not a mandatory step that stops the
        /// card. A mandatory reading would stop the text on both, which would be a different card.</para>
        ///
        /// <para>The destination is <see cref="DirectMoveDestinationPolicy.NotAdjacentToActor"/>, minted
        /// for this sentence: none of the five earlier policies says it, and
        /// <see cref="DirectMoveDestinationPolicy.Unrestricted"/> would let the minion shuffle sideways
        /// to another space still beside her - the outcome the clause exists to forbid.</para>
        /// </summary>
        private static EffectDefinition CreateShoveRider(
            CardDefinitionId cardId, string contentRevision, int maximumMinions)
        {
            var root = new EffectNodeId("shove-root");
            var select = new EffectNodeId("shove-select");
            var moveFirst = new EffectNodeId("shove-move");
            var moveSecond = new EffectNodeId("shove-move-second");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: maximumMinions > 1
                        ? new[] { select, moveFirst, moveSecond }
                        : new[] { select, moveFirst }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticRosterCapabilities.ChooseTargetSetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter,
                            RosterQueryOrigin.Actor),
                        // "adjacent to you", which p.19 measures as one space from her own.
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter,
                            RosterUnitKind.Minion),
                        // "minion", with no side-word: p.19 makes "friendly" the word that would have
                        // narrowed it, and neither face prints one.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.AnyTeam),
                        // This text is applied inside an Attack, and p.18 asks immunity about the
                        // activity being performed rather than about the node doing it.
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Attack),
                        EffectInputBinding.FromLiteral("minimum", EffectValue.Integer(0)),
                        EffectInputBinding.FromLiteral(
                            "maximum", EffectValue.Integer(maximumMinions))
                    },
                    outputs: SelectionOutputs(maximumMinions)),
                ShoveNode(moveFirst, select, FirstSpaceOutput)
            };

            if (maximumMinions > 1)
            {
                nodes.Add(ShoveNode(moveSecond, select, SecondSpaceOutput));
            }

            return new EffectDefinition(
                AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>One of the shoves the selection above named, by the space it published.</summary>
        private static EffectNodeDefinition ShoveNode(
            EffectNodeId nodeId, EffectNodeId selectionNodeId, string spaceOutput) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", selectionNodeId, spaceOutput, EffectValueKind.Coordinate),
                    // "1 space", exactly: the "up to" in this sentence is over the minions and not over
                    // the distance, so `if able` leaves the number exact - which is p.11's own
                    // separation of "the distance is optional" from "the move itself is optional".
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.NotAdjacentToActor)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                });

        /// <summary>What the shove selection publishes, which is one space per minion it may name.</summary>
        private static EffectOutputBinding[] SelectionOutputs(int maximumMinions) =>
            maximumMinions > 1
                ? new[]
                {
                    new EffectOutputBinding(SelectedCountOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(FirstSpaceOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(SecondSpaceOutput, EffectValueKind.Coordinate)
                }
                : new[]
                {
                    new EffectOutputBinding(SelectedCountOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(FirstSpaceOutput, EffectValueKind.Coordinate)
                };

        /// <summary>
        /// "An enemy hero in radius who is adjacent to an empty spawn point in the battle zone discards
        /// a card, if able." - Death Trap, her Silver basic and her only basic Skill.
        ///
        /// <para><b>The sentence has two bounds measured from two different places</b>, which is the
        /// whole reason its clause is a narrowing rather than a wider disk: p.9's Radius 4 is measured
        /// from Dodger, and the adjacency to a qualifying spawn point is measured from each candidate.
        /// Moving the selection's origin could not express it, and neither could a gate - a gate would
        /// ask about Dodger's own surroundings, which this sentence never mentions.</para>
        ///
        /// <para><b>Which spawn points qualify is the same derivation her other eight faces read</b> -
        /// minion points only per the user's D-01 ruling, in the current Battle Zone, empty. Note which
        /// piece the Battle Zone is asked about: the spawn point, never the hero and never Dodger. That
        /// is the designer's own 2026-07-25 ruling read at its natural scope - the clause qualifies the
        /// spawn point and nothing else.</para>
        ///
        /// <para><b>The discard is p.17's <c>if able</c>, and the card prints it.</b> Revision 1 of the
        /// discard is exactly that contract: a hero with no card to give gives none, the step declines,
        /// and nothing downstream is told - which is right here, because the sentence ends there. There
        /// is no second arm, so nothing needs to know that the first could not happen.</para>
        ///
        /// <para><b>Naming nobody is impossible rather than a decline.</b> p.10 makes this one mandatory
        /// step and a step with nobody to perform it is one that cannot complete; the printed
        /// <c>if able</c> is about the discard, and it is declared on the discard node where the page
        /// puts it. That is the identical reading Wasp's radius discard already carries.</para>
        /// </summary>
        private static EffectDefinition CreateDeathTrap(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("death-trap-root");
            var select = new EffectNodeId("death-trap-select");
            var discard = new EffectNodeId("death-trap-discard");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, discard }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            // The enclosing action is this card's own Skill primary, and p.18 declares
                            // immunity per activity.
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
                            // "enemy", from the card's perspective, which is the acting hero's.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            // "and not adjacent to you" is the Radius hero arm's own declared narrowing
                            // and this face does not print it: an adjacent enemy hero standing beside a
                            // qualifying spawn point is squarely inside "in radius".
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            // "who is adjacent to an empty spawn point in the battle zone".
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities
                                    .AdjacentToEmptyMinionSpawnPointParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter, select,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
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
        /// "If there are 2 or more empty spawn points in radius in the battle zone, gain N coins." -
        /// Dark Ritual at one coin and Darker Ritual at two.
        ///
        /// <para><b>The gate is the whole sentence's condition and the gain is its whole consequence.</b>
        /// Nobody is targeted and nothing on the board moves, so no immunity question arises: p.18 makes
        /// immunity a rule about being targeted, and counting spawn points is not targeting. The count
        /// is the capability's own answer measured against the number the card prints, stated as a bound
        /// rather than a comparison this file writes - the same shape <c>presence-count@1</c> has
        /// carried since milestone 8.22.</para>
        ///
        /// <para><b>The disk is p.9's printed Radius icon</b>, which both faces print as 3 and which
        /// p.16's Radius item widens. That is the declaration's whole difference from Dread Razor's: the
        /// Gold card's adjacency is a number its sentence writes down, and an item never raises one of
        /// those.</para>
        ///
        /// <para><b>A false condition skips and the card is done</b> - the user's RULING 3, and the
        /// natural shape of a conditional node with no else-child. A ritual performed in a full Battle
        /// Zone is a Skill that legally does nothing, not a step that stops the action.</para>
        ///
        /// <para>The recipient is the actor: "gain N coins" names no one else, and p.16 gives coins to
        /// the hero whose card it is.</para>
        /// </summary>
        private static EffectDefinition CreateRitual(
            CardDefinitionId cardId, string contentRevision, int coins)
        {
            var root = new EffectNodeId("ritual-root");
            var spawnPoints = new EffectNodeId("ritual-spawn-points");
            var gate = new EffectNodeId("ritual-gate");
            var gain = new EffectNodeId("ritual-gain");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { spawnPoints, gate }),
                    new EffectNodeDefinition(
                        spawnPoints, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.MinimumPresentParameter,
                                EffectValue.Integer(PrintedSpawnPointThreshold))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.CountOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    Gate(gate, spawnPoints, gain),
                    new EffectNodeDefinition(
                        gain, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
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
                });
        }

        /// <summary>
        /// One node that raises a persistent text and nothing else, which is what both of her
        /// "<b>This turn:</b>" Skills are. The rule itself is declared on the Active-effect definition
        /// in <see cref="DodgerActiveEffectContent"/>, so nothing about the drain or the Repeat lock is
        /// restated here.
        /// </summary>
        private static EffectDefinition CreateActiveEffect(
            EffectDefinitionId effectId,
            string contentRevision,
            EffectCapabilityReference capability,
            string nodePrefix)
        {
            var root = new EffectNodeId(nodePrefix + "-root");
            return new EffectDefinition(
                effectId,
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        capability,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "Respawn a friendly minion in an empty friendly spawn point [adjacent to you / in radius] in
        /// the battle zone." - Necromancy at p.19's innermost ring, Necromastery at its printed Radius 1.
        ///
        /// <para><b>The whole sentence is one node, because the card asks one question.</b> The user's
        /// 2026-09-06 ruling on D-07 makes the chosen Spawn point decide what comes back - p.8's own
        /// respawn already says "one minion of the <em>matching type</em>" - so the actor names a space
        /// and never a minion. A composition that asked for a type as well would be offering a choice
        /// the card does not print.</para>
        ///
        /// <para><b>Heavies are excluded by p.19's arithmetic and by nothing written here.</b> The
        /// ruling records the user's derivation rather than an added restriction: p.13's immunity means
        /// a Heavy dies only as the last minion of its side in the Zone, and that death is p.8's
        /// Lane-Push cause, which moves the Battle Zone and respawns the whole wave - so the page's
        /// count never favours a Heavy. <c>MinionRespawnEligibilityQuery</c> states the count as printed
        /// and <c>DodgerNecromancyTests</c> proves the consequence; a hard-coded exclusion would have
        /// made that proof tautological.</para>
        ///
        /// <para><b>Mandatory, because the face prints no "you may" and no "if able".</b> A Dodger with
        /// no eligible empty friendly point in reach cannot perform the step, which p.10 makes a stop -
        /// and the designer's 2026-07-24 answer in the official channel confirms that reading for a
        /// mandatory step that has no legal subject. The offer is withdrawn rather than refused
        /// afterwards, because the handler answers <c>ImpossibleMandatory</c> when it anticipates.</para>
        ///
        /// <para>"friendly" is p.19's word about sides and it is read from the actor's team, which is
        /// why the reach and the side are the only two things this definition supplies.</para>
        /// </summary>
        private static EffectDefinition CreateRespawn(
            CardDefinitionId cardId, string contentRevision, bool readsPrintedRadius)
        {
            var root = new EffectNodeId("necromancy-root");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        readsPrintedRadius
                            ? SyntheticPlacementCapabilities.RespawnFriendlyMinionInRadiusV1
                            : SyntheticPlacementCapabilities.RespawnFriendlyMinionAdjacentV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticPlacementCapabilities.RespawnedOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticPlacementCapabilities.RespawnedAtOutputName,
                                EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "If there are 2 or more empty spawn points in radius in the battle zone, gain 2 coins; If
        /// you have Ultimate, gain +1 [Attack] Attack item." - Darkest Ritual, her Green Tier III.
        ///
        /// <para><b>Two clauses, two conditions, one after the other - the user's 2026-09-06 ruling on
        /// D-04.</b> The semicolon does not subordinate the second to the first, so the item clause is
        /// gated on having the Ultimate and on nothing else. The composition says that by putting the
        /// two gates side by side in one sequence rather than nesting the second inside the first
        /// branch: a Dodger with her Ultimate up in a crowded Battle Zone gains the item and no coins,
        /// which is exactly what the two independent sentences describe.</para>
        ///
        /// <para><b>Why that matters more than its wording suggests.</b> p.16: "Once you unlock your
        /// Ultimate, you cannot gain any additional levels, but can still gain coins." Items are
        /// otherwise gained only at a level up, so after the Ultimate this clause is the only route to a
        /// new item in the game, and it is the whole of her late-game scaling.</para>
        ///
        /// <para><b>The item is unbounded and permanent</b> - the user's ruling on D-05, and also the
        /// card's silence, which the standing ruling reads as absence rather than an unprinted limit.
        /// Nothing here counts performances or expires anything; p.16 already allows several items under
        /// one slot, and the slot is the dashboard's answer to the printed Attack icon rather than
        /// something this definition names.</para>
        ///
        /// <para>Both gates are <see cref="EffectNodeKind.Conditional"/> with no else-child, which is
        /// the user's RULING 3: a false condition skips and the card continues. A Darkest Ritual
        /// performed by a hero with no Ultimate in a full Battle Zone is a Skill that legally does
        /// nothing.</para>
        /// </summary>
        private static EffectDefinition CreateDarkestRitual(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("darkest-root");
            var spawnPoints = new EffectNodeId("darkest-spawn-points");
            var coinGate = new EffectNodeId("darkest-coin-gate");
            var gain = new EffectNodeId("darkest-gain");
            var ultimate = new EffectNodeId("darkest-ultimate");
            var itemGate = new EffectNodeId("darkest-item-gate");
            var item = new EffectNodeId("darkest-item");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { spawnPoints, coinGate, ultimate, itemGate }),

                    // First clause. The identical count her other counting faces read, at the printed
                    // Radius 3 this face carries.
                    new EffectNodeDefinition(
                        spawnPoints, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.MinimumPresentParameter,
                                EffectValue.Integer(PrintedSpawnPointThreshold))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.CountOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    Gate(coinGate, spawnPoints, gain),
                    new EffectNodeDefinition(
                        gain, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.GainResourceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral("amount", EffectValue.Integer(2))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding("amount", EffectValueKind.Integer)
                        }),

                    // Second clause, gated on its own condition and nothing else.
                    new EffectNodeDefinition(
                        ultimate, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.UltimateUnlockedV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    Gate(itemGate, ultimate, item),
                    new EffectNodeDefinition(
                        item, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.GainItemFromTheBoxV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                            // "+1 [Attack] Attack item" - the icon the card prints, which p.16 then
                            // matches to the dashboard's slot 1.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.ItemIconParameter,
                                PrintedValueChannel.Attack)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.GrantedItemOrderOutput,
                                EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "Target a unit adjacent to you" as a selection, which is what the adjacent bullet of each of
        /// her modal Ranged Attacks is: the card's own reach rather than p.13's subtype reach, because
        /// the bullet says adjacent and the icon says two or three.
        /// </summary>
        private static IEnumerable<EffectInputBinding> AdjacentEnemyUnit() => new[]
        {
            EffectInputBinding.FromClosedVocabulary(
                SyntheticRosterCapabilities.OriginParameter,
                RosterQueryOrigin.Actor),
            EffectInputBinding.FromLiteral(
                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
            EffectInputBinding.FromClosedVocabulary(
                SyntheticRosterCapabilities.UnitKindParameter,
                RosterUnitKind.Unit),
            EffectInputBinding.FromClosedVocabulary(
                SyntheticRosterCapabilities.RelationParameter,
                SourceTeamRelation.OpposingSource),
            EffectInputBinding.FromClosedVocabulary(
                "activity", RuleActivity.Attack)
        };

        /// <summary>
        /// One printed condition read as a branch with no else-child, which is what "If X, do Y" is: the
        /// user's RULING 3 says a false condition simply skips and the card continues, so the absence of
        /// a second arm is the composition of that ruling rather than an omission.
        /// </summary>
        private static EffectNodeDefinition Gate(
            EffectNodeId nodeId, EffectNodeId conditionNodeId, EffectNodeId thenNodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { thenNodeId },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        FlagParameter, conditionNodeId,
                        SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(GateValueOutput, EffectValueKind.Boolean)
                });

        /// <summary>
        /// The node that names the enclosing Attack's target from a unit an earlier roster selection
        /// chose. It performs no Attack: the capability publishes a typed fact and the Attack's own
        /// authority reads it.
        /// </summary>
        private static EffectNodeDefinition BindAttackTargetFromRosterSelection(
            EffectNodeId nodeId, EffectNodeId selectionNodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, selectionNodeId,
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, selectionNodeId,
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                },
                outputs: BoundTargetOutputs());

        /// <summary>The same binding from a printed-icon unit selection, whose outputs are named differently.</summary>
        private static EffectNodeDefinition BindAttackTargetFromPrintedIconSelection(
            EffectNodeId nodeId, EffectNodeId selectionNodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, selectionNodeId,
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, selectionNodeId,
                        SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        EffectValueKind.Minion)
                },
                outputs: BoundTargetOutputs());

        private static EffectOutputBinding[] BoundTargetOutputs() => new[]
        {
            new EffectOutputBinding(
                SyntheticContinuationCapabilities.BoundHeroTargetOutput, EffectValueKind.Hero),
            new EffectOutputBinding(
                SyntheticContinuationCapabilities.BoundMinionTargetOutput, EffectValueKind.Minion)
        };

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-attack-text");

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-skill");
    }
}
