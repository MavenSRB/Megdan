using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Tigerclaw's printed card text, composed as typed effect definitions. Milestone 8.23.
    ///
    /// <para>Every sentence below is transcribed in <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c> and mapped in
    /// the roster capability matrix. What this file adds is the composition: which nodes each sentence
    /// becomes, in the order the face prints them, over contracts that already exist. Nothing here
    /// parses printed text, names a hero in a rule, or reads a card by name at runtime.</para>
    ///
    /// <para><b>Two of his faces print card text at two of rulebook p.14's moments</b>, which no earlier
    /// verified face does: Combat Reflexes and Leaping Strike print "Before the attack:" and "After the
    /// attack:" on the same card. Those are two applications of card text six numbered steps apart with
    /// the defender's whole response in between, so each is its own definition, and
    /// <see cref="ActionDefinition.SecondCardTextRoot"/> is what lets one action declare both.</para>
    /// </summary>
    public static class TigerclawEffectContent
    {
        /// <summary>The Ultimate's rule identity, named here because the card content references it.</summary>
        public const string CloakAndDaggersRuleId = "tigerclaw-cloak-and-daggers";

        private const string ActorSpaceOutput = "space";
        private const string DestinationOutput = "destination";
        private const string PresentOutput = "present";
        private const string PlacedOutput = "placed";
        private const string MovedOutput = "moved";
        private const string DidNotMoveOutput = "didNotMove";
        private const string TransferAmountOutput = "amount";

        // -----------------------------------------------------------------------------------------
        // p.14 step 1: the attacker's "Before the attack:" text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Attack of Tigerclaw's applies first, or <c>null</c> where it prints
        /// none at its first moment. Paired with <see cref="AttackTextTimingFor"/>, which names which
        /// of p.14's moments that is.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(TigerclawCardContent.BlinkStrike))
            {
                return CreateMoveThroughAndTarget(cardId, contentRevision, RosterUnitKind.Unit);
            }

            // Nimble Strike composes nothing, exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward and Sabina's Bang compose nothing: p.6 returns the Handicap
            // card to the box and the six-player pitch never brings it back, so its
            // `PrintedTextComposition.DeferredUntil` and a composed body are mutually exclusive - an
            // action carrying its effect root owes no composition. Its printed values and words ship
            // in the card content all the same, which is what makes the deferral a decision about
            // composition rather than a gap in the inventory. What it would need when a milestone
            // deals it is one call to CreateMoveThroughAndTarget with RosterUnitKind.Hero, because the
            // Handicap card crosses an enemy hero where the ordinary Gold card crosses any enemy unit.

            if (cardId.Equals(TigerclawCardContent.CombatReflexes) ||
                cardId.Equals(TigerclawCardContent.LeapingStrike))
            {
                return CreateOptionalStep(cardId, contentRevision, "before", 1);
            }

            if (cardId.Equals(TigerclawCardContent.HitAndRun))
            {
                // Its only text is at step 7, so its first moment is that one.
                return CreateOptionalStep(cardId, contentRevision, "after", 1);
            }

            return null;
        }

        /// <summary>
        /// Which of p.14's moments <see cref="AttackTextFor"/>'s body is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(TigerclawCardContent.BlinkStrike) ||
                cardId.Equals(TigerclawCardContent.CombatReflexes) ||
                cardId.Equals(TigerclawCardContent.LeapingStrike))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            return cardId.Equals(TigerclawCardContent.HitAndRun)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;
        }

        // -----------------------------------------------------------------------------------------
        // p.14 step 7: the attacker's "After the attack:" text, where the same face printed step 1's
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The second body a printed Attack applies, or <c>null</c> for the seventeen faces that print
        /// one body or none.
        ///
        /// <para>Only Combat Reflexes and Leaping Strike carry one. Hit and Run prints a single "After
        /// the attack:" clause and declares it as its first body, because a card printing one moment
        /// has no second to name - the pair is not "before and after", it is "the moments this face
        /// prints".</para>
        /// </summary>
        public static EffectDefinition SecondAttackTextFor(
            CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(TigerclawCardContent.LeapingStrike))
            {
                // "After the attack: You may move 1 space." - unconditional, which is the whole of what
                // separates its step 7 from Combat Reflexes'.
                return CreateOptionalStep(cardId, contentRevision, "after", 1);
            }

            if (cardId.Equals(TigerclawCardContent.CombatReflexes))
            {
                return CreateUnmovedFollowUpStep(cardId, contentRevision);
            }

            return null;
        }

        /// <summary>Which moment the second body is applied at, or None where there is none.</summary>
        public static CardTextTiming SecondAttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(TigerclawCardContent.CombatReflexes) ||
            cardId.Equals(TigerclawCardContent.LeapingStrike)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.14 step 4: the defender's own Defense action text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The rider one of his five Blocks applies at p.14's step 4, or <c>null</c> for the Block that
        /// prints none.
        ///
        /// <para>Dodge is that Block: "Block a ranged attack." is the whole card, and its condition is
        /// the Block's own resolution rather than text. A definition that gave it an empty effect would
        /// be claiming a step the face does not print.</para>
        /// </summary>
        public static EffectDefinition DefenseTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(TigerclawCardContent.Sidestep))
            {
                return CreateOptionalStep(cardId, contentRevision, "sidestep", 1);
            }

            if (cardId.Equals(TigerclawCardContent.Evade))
            {
                return CreateEvade(cardId, contentRevision);
            }

            if (cardId.Equals(TigerclawCardContent.Parry))
            {
                return CreateAttackerDiscard(cardId, contentRevision, escalatesToDefeat: false);
            }

            if (cardId.Equals(TigerclawCardContent.Riposte))
            {
                return CreateAttackerDiscard(cardId, contentRevision, escalatesToDefeat: true);
            }

            return null;
        }

        /// <summary>
        /// The moment a Block's rider is applied at, or None for the Block that prints none. It is
        /// asked per card rather than derived from "this is a Block", because Dodge is a Block with no
        /// text and a declared timing beside a null body would be a contradiction the action
        /// definition would have to resolve by guessing.
        /// </summary>
        public static CardTextTiming DefenseTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(TigerclawCardContent.Sidestep) ||
            cardId.Equals(TigerclawCardContent.Parry) ||
            cardId.Equals(TigerclawCardContent.Evade) ||
            cardId.Equals(TigerclawCardContent.Riposte)
                ? CardTextTiming.DefenseAction
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.12's Skills: the effect is the action
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(TigerclawCardContent.BlendIntoShadows))
            {
                return CreateBlendIntoShadows(cardId, contentRevision);
            }

            if (cardId.Equals(TigerclawCardContent.LightFingered))
            {
                // "You may move 1 space" - an optional step of exactly one, so standing still is one of
                // the answers and moving two is not.
                return CreateTheft(
                    cardId, contentRevision,
                    openingIsOptional: true, openingDistance: 1,
                    minimumCoins: 1, maximumCoins: 1,
                    followUpIsOptional: true, followUpDistance: 1);
            }

            if (cardId.Equals(TigerclawCardContent.PickPocket))
            {
                // "Move up to 2 spaces" - p.17's `up to`, which is a different non-mandatory keyword
                // from `you may`: the step happens and its distance may be zero.
                return CreateTheft(
                    cardId, contentRevision,
                    openingIsOptional: false, openingDistance: 2,
                    minimumCoins: 1, maximumCoins: 1,
                    followUpIsOptional: true, followUpDistance: 1);
            }

            if (cardId.Equals(TigerclawCardContent.MasterThief))
            {
                // "Take 1 or 2 coins" - the first verified transfer whose amount is a choice, and
                // "you may move up to 2 spaces" on the arm that follows it.
                return CreateTheft(
                    cardId, contentRevision,
                    openingIsOptional: false, openingDistance: 2,
                    minimumCoins: 1, maximumCoins: 2,
                    followUpIsOptional: true, followUpDistance: 2);
            }

            if (cardId.Equals(TigerclawCardContent.PoisonedDagger))
            {
                return CreatePoisoning(
                    cardId, contentRevision, TigerclawComponentContent.GivePoisonOneV1);
            }

            if (cardId.Equals(TigerclawCardContent.PoisonedDart))
            {
                return CreatePoisoning(
                    cardId, contentRevision, TigerclawComponentContent.GivePoisonTwoV1);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // p.16's permanent ability
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "After you perform a basic action you may repeat it once; if you repeat an attack action,
        /// you cannot target the same unit." - Cloak and Daggers.
        ///
        /// <para><b>One node, and everything the sentence says is on it.</b> "you may" is the failure
        /// policy and the chooser; "once" is the declared maximum; "if you repeat an attack action, you
        /// cannot target the same unit" is
        /// <see cref="RepeatTargetBinding.DifferentTarget"/>, which milestone 8.10 minted for Thunder
        /// Boomerang and which applies to an Attack alone because no other action kind the page repeats
        /// has a target to exclude.</para>
        ///
        /// <para><b>What is not here is the whole of what milestone 8.23 built.</b> Which action is
        /// repeated, that p.10 forbids repeating a repeat, and that "basic action" is p.9's supertype
        /// rather than a card list are all facts about the moment this ability fires - carried by the
        /// trigger's own occurrence and by the subject the authority holds - so the definition says
        /// what the card says and nothing about how the engine finds it.</para>
        /// </summary>
        public static EffectDefinition CloakAndDaggersTriggeredEffect(string contentRevision)
        {
            var repeat = new EffectNodeId("cloak-and-daggers-repeat");
            return new EffectDefinition(
                new EffectDefinitionId("tigerclaw-cloak-and-daggers-effect"),
                contentRevision,
                repeat,
                new[]
                {
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }

        // -----------------------------------------------------------------------------------------
        // Compositions
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Before the attack: Move 2 spaces in a straight line through an enemy unit; target that
        /// unit." - Blink Strike, and Nimble Strike with `hero` for `unit`.
        ///
        /// <para><b>One question, not three.</b> The face reads as a move and then a target, but there
        /// is only ever one decision: a straight two-space line whose middle space holds a qualifying
        /// enemy and whose end space is empty determines both where he lands and whom he attacks. That
        /// is what <c>choose-through-target-line@1</c> asks - it offers the crossable units and
        /// publishes the destination beside the one chosen - and asking twice would let a player pick a
        /// line and then a unit that is not on it.</para>
        ///
        /// <para><b>Every step is mandatory, and the printed reminder is why that matters.</b> Blink
        /// Strike prints "(If you cannot make this move, you cannot attack).", which is p.10's own
        /// rule for a mandatory step restated rather than an exception - so the reminder composes to
        /// nothing and the policy carries the meaning. A board with no such line stops the action at
        /// this step, and p.10 skips the rest, which is the Attack.</para>
        ///
        /// <para>The Move is p.18's direct Move to a named space rather than a Movement action: it
        /// spends no Movement value, it is not the card's Movement icon, and its destination is the one
        /// the line published.</para>
        ///
        /// <para><b>The crossing is declared on that Move.</b> "Through an enemy unit" is a sentence
        /// about this movement, not a permission Tigerclaw carries, so it is bound as the Move's own
        /// <c>movesThroughObstacles</c> rather than as a granted
        /// <c>MoveThroughObstacles</c>: the unit he crossed stops him again on his very next Move.
        /// The line above has already refused any route whose end space is occupied, which is p.18's
        /// other sentence - a Move may cross an obstacle and may never rest on one - so the
        /// declaration widens exactly the step it names and nothing else.</para>
        /// </summary>
        private static EffectDefinition CreateMoveThroughAndTarget(
            CardDefinitionId cardId, string contentRevision, RosterUnitKind crossed)
        {
            var root = new EffectNodeId("blink-root");
            var line = new EffectNodeId("blink-line");
            var actorSpace = new EffectNodeId("blink-actor-space");
            var move = new EffectNodeId("blink-move");
            var retarget = new EffectNodeId("blink-retarget");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { line, actorSpace, move, retarget }),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseThroughTargetLineV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                EffectValue.Integer((int)crossed))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(DestinationOutput, EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer((int)DirectMoveDestinationPolicy.NamedSpace)),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, DestinationOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities
                                    .MovesThroughObstaclesParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        retarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticContinuationCapabilities.SetAttackTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.HeroParameter,
                                line, SyntheticRosterCapabilities.ChosenHeroOutput,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.MinionParameter,
                                line, SyntheticRosterCapabilities.ChosenMinionOutput,
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
                        })
                });
        }

        /// <summary>
        /// "You may move 1 space." - Hit and Run's step 7, Combat Reflexes' and Leaping Strike's step
        /// 1, Leaping Strike's step 7, and Sidestep's Defense rider. One sentence, five faces.
        ///
        /// <para>p.17's <c>you may</c> makes the step declinable, and one space is exactly one: the
        /// distance is not "up to", so a hero who takes it moves and a hero who does not declines. That
        /// is the axis milestone 8.8b turned on and it is what separates this from Pick Pocket's "Move
        /// up to 2 spaces".</para>
        /// </summary>
        private static EffectDefinition CreateOptionalStep(
            CardDefinitionId cardId, string contentRevision, string prefix, int distance)
        {
            var root = new EffectNodeId($"{prefix}-root");
            var actorSpace = new EffectNodeId($"{prefix}-actor-space");
            var move = new EffectNodeId($"{prefix}-move");

            return new EffectDefinition(
                prefix == "sidestep" ? DefenseTextEffectId(cardId)
                    : prefix == "after" && SecondAttackTextTimingFor(cardId) != CardTextTiming.None
                        ? SecondAttackTextEffectId(cardId)
                        : AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move }),
                    ActorSpaceNode(actorSpace),
                    OptionalStepNode(move, actorSpace, distance)
                });
        }

        /// <summary>
        /// "After the attack: If you did not move before the attack, you may move 1 space." - Combat
        /// Reflexes' step 7, and the only sentence in the verified roster that asks at one of p.14's
        /// moments about what happened at another.
        ///
        /// <para><b>The condition is the card's own and is read from the Attack.</b> The frame that
        /// offered the step-1 move completed six numbered steps ago and is gone;
        /// <c>actor-moved-this-action@1</c> answers by comparing where he stands with where this Attack
        /// recorded him standing when it began, which is the fact the Attack itself carries.</para>
        ///
        /// <para><b>Two non-mandatory keywords in one sentence, and neither swallows the other.</b> The
        /// condition is a branch nobody is asked about, and the step behind it is a `may` the actor
        /// answers - so a hero who moved is never offered the second move at all, and a hero who did
        /// not is offered it and may still decline.</para>
        /// </summary>
        private static EffectDefinition CreateUnmovedFollowUpStep(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("reflexes-root");
            var moved = new EffectNodeId("reflexes-moved");
            var gate = new EffectNodeId("reflexes-gate");
            var sequence = new EffectNodeId("reflexes-sequence");
            var actorSpace = new EffectNodeId("reflexes-actor-space");
            var move = new EffectNodeId("reflexes-move");

            return new EffectDefinition(
                SecondAttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { moved, gate }),
                    new EffectNodeDefinition(
                        moved, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ActorMovedThisActionV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Boolean),
                            new EffectOutputBinding(DidNotMoveOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { sequence },
                        inputs: new[]
                        {
                            // The card prints the negative, and the reading it is handed is the
                            // negative: nothing here inverts a boolean, because the capability
                            // publishes both views of its one answer.
                            EffectInputBinding.FromNodeOutput(
                                "flag", moved, DidNotMoveOutput, EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move }),
                    ActorSpaceNode(actorSpace),
                    OptionalStepNode(move, actorSpace, 1)
                });
        }

        /// <summary>
        /// "If you are adjacent to terrain, place yourself into a space in radius; if you do, <b>Next
        /// turn:</b> You are immune to enemy attack actions." - Blend Into Shadows.
        ///
        /// <para><b>Two clauses, and only the first is a condition.</b> "If you are adjacent to
        /// terrain" is a presence question asked before anything happens, and it gates the rest. "If
        /// you do" is not a second question at all - it is rulebook p.10's own rule for a mandatory
        /// step restated, exactly as Blink Strike's parenthesis restates it: the Place prints neither
        /// "you may" nor "up to" nor "if able", so a hero with no empty space in radius stops the
        /// action at that step and p.10 skips what follows, which is the immunity. Composing it as a
        /// boolean gate would have been a second statement of a rule the engine already applies.</para>
        ///
        /// <para><b>The Place measures by the printed icon.</b> The face carries Radius 2 and the text
        /// says "in radius", so the reach is <c>synthetic-place-in-radius@1</c> rather than a number
        /// written here - which is what lets p.16's +1 Radius item widen it, as the shard records that
        /// item doing for this exact card.</para>
        ///
        /// <para>p.19's Place is not a Movement: it spends no Movement value, ignores what is in
        /// between, and is refused by nothing a walk would be refused by.</para>
        /// </summary>
        private static EffectDefinition CreateBlendIntoShadows(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("blend-root");
            var terrain = new EffectNodeId("blend-terrain");
            var gate = new EffectNodeId("blend-gate");
            var sequence = new EffectNodeId("blend-sequence");
            var actorSpace = new EffectNodeId("blend-actor-space");
            var place = new EffectNodeId("blend-place");
            var shadow = new EffectNodeId("blend-shadow");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { terrain, gate }),
                    new EffectNodeDefinition(
                        terrain, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.AdjacentTerrainPresenceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { sequence },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", terrain, PresentOutput, EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place, shadow }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRadiusV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer((int)PlaceEligiblePieceKind.UnitOrToken)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                EffectValue.Integer((int)PlaceDestinationAnchor.ActingHero)),
                            // The face refuses no Spawn point, so this declaration refuses none.
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                EffectValue.Integer((int)PlaceSpawnPointExclusion.None))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate)
                        }),
                    // "if you do" - the step after a mandatory Place, reached only when that Place
                    // applied and only inside the branch the terrain question opened. It names no
                    // input: what it creates is definition content on the Active effect itself, and
                    // whose effect it is comes from the frame's own actor.
                    new EffectNodeDefinition(
                        shadow, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        TigerclawActiveEffectContent.ShadowedNextTurnV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "[Move] Take 1 coin from an enemy hero adjacent to you; if you do, you may move." -
        /// Light-Fingered, Pick Pocket and Master Thief, which differ in three numbers and one keyword.
        ///
        /// <para><b>These are the first verified cards that take a resource from another hero</b>
        /// rather than gaining one from the supply, which is why <c>transfer-resource@1</c> exists and
        /// why it debits and credits together: a transfer that could half-apply would mint or destroy
        /// coins, and p.15 spends them.</para>
        ///
        /// <para><b>The selection carries the adjacency and the minimum.</b> "an enemy hero adjacent to
        /// you" is who may be picked, so a hero with no coins is not offered and then refused - the
        /// same reading milestone 8.20b settled for a restriction naming its actors.</para>
        ///
        /// <para><b>"if you do" is rulebook p.10's mandatory-step rule and not a second question.</b>
        /// "Take 1 coin from an enemy hero adjacent to you" prints none of p.10's three non-mandatory
        /// keywords, so it is mandatory - and a step that cannot be completed stops the action there
        /// and skips what follows, which is exactly the movement behind the semicolon. The opening
        /// step already taken stands, which is the same page's other half.</para>
        /// </summary>
        private static EffectDefinition CreateTheft(
            CardDefinitionId cardId,
            string contentRevision,
            bool openingIsOptional,
            int openingDistance,
            int minimumCoins,
            int maximumCoins,
            bool followUpIsOptional,
            int followUpDistance)
        {
            var root = new EffectNodeId("theft-root");
            var openingSpace = new EffectNodeId("theft-opening-space");
            var opening = new EffectNodeId("theft-opening-move");
            var victim = new EffectNodeId("theft-victim");
            var take = new EffectNodeId("theft-take");
            var followUpSpace = new EffectNodeId("theft-follow-up-space");
            var followUp = new EffectNodeId("theft-follow-up-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[]
                        {
                            openingSpace, opening, victim, take, followUpSpace, followUp
                        }),
                    ActorSpaceNode(openingSpace),
                    openingIsOptional
                        ? OptionalStepNode(opening, openingSpace, openingDistance)
                        : UpToStepNode(opening, openingSpace, openingDistance),
                    new EffectNodeDefinition(
                        victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.OriginParameter,
                                EffectValue.Integer((int)RosterQueryOrigin.Actor)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                EffectValue.Integer((int)RosterUnitKind.Hero)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.RelationParameter,
                                EffectValue.Integer((int)SourceTeamRelation.OpposingSource)),
                            EffectInputBinding.FromLiteral(
                                "activity", EffectValue.Integer((int)RuleActivity.Skill))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        take, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.TransferResourceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "source", victim, SyntheticRosterCapabilities.ChosenHeroOutput,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral(
                                "minimum", EffectValue.Integer(minimumCoins)),
                            EffectInputBinding.FromLiteral(
                                "maximum", EffectValue.Integer(maximumCoins))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("source", EffectValueKind.Hero),
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding(TransferAmountOutput, EffectValueKind.Integer)
                        }),
                    ActorSpaceNode(followUpSpace),
                    followUpIsOptional
                        ? OptionalStepNode(followUp, followUpSpace, followUpDistance)
                        : UpToStepNode(followUp, followUpSpace, followUpDistance)
                });
        }

        /// <summary>
        /// "Give a hero in range a [Poison] Poison marker." - Poisoned Dagger and Poisoned Dart, whose
        /// only difference is which of the two marker strengths they hand over.
        ///
        /// <para><b>"a hero", not "an enemy hero".</b> p.10 lets a card affect its own hero only when
        /// it says so, and neither face does, so the selection admits both sides and excludes
        /// Tigerclaw. That is what <c>synthetic-range-hero-target@1</c> with an any-team relation
        /// already means: the printed-icon selections never offer the actor.</para>
        ///
        /// <para>The give asks nothing - the range is the selection's question - and a supply with
        /// nothing left in it stops the Skill at that step under p.10, which is the whole of what p.18's
        /// finite supply costs.</para>
        /// </summary>
        private static EffectDefinition CreatePoisoning(
            CardDefinitionId cardId, string contentRevision, EffectCapabilityReference marker)
        {
            var root = new EffectNodeId("poison-root");
            var target = new EffectNodeId("poison-target");
            var give = new EffectNodeId("poison-give");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, give }),
                    new EffectNodeDefinition(
                        target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "activity", EffectValue.Integer((int)RuleActivity.Skill)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                EffectValue.Integer((int)SourceTeamRelation.AnyTeam)),
                            // "other than the attacker" is a Block's clause and neither of these
                            // prints it; there is no attacker here at all.
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
                    new EffectNodeDefinition(
                        give, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        marker,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "holder", target,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("holder", EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "Block a ranged attack. You may move 1 space. You may retrieve your resolved or discarded
        /// basic skill card." - Evade, which is Sidestep's rider plus one clause.
        ///
        /// <para>The retrieve names a card class rather than a card: p.11's Retrieve takes a card back
        /// into hand "depending on the card text", and this text says which of his own cards qualify -
        /// a basic Skill, in either of the two states the sentence names. Blend Into Shadows is the one
        /// card of his that is one, which is a fact about his set rather than a narrowing this
        /// declaration makes.</para>
        /// </summary>
        private static EffectDefinition CreateEvade(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("evade-root");
            var actorSpace = new EffectNodeId("evade-actor-space");
            var move = new EffectNodeId("evade-move");
            var retrieve = new EffectNodeId("evade-retrieve");

            return new EffectDefinition(
                DefenseTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move, retrieve }),
                    ActorSpaceNode(actorSpace),
                    OptionalStepNode(move, actorSpace, 1),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral(
                                "kind",
                                EffectValue.Integer(
                                    (int)RosterCardRetrievalKind.OwnedResolvedOrDiscardedBasicSkill))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// "The attacker discards a card, if able." - Parry; and "The attacker discards a card, or is
        /// defeated." - Riposte.
        ///
        /// <para><b>The attacker is named, not chosen.</b> p.14's step 4 is the defender's own text and
        /// the hero on the other side of it is the attacker, which
        /// <see cref="EffectRoleKind.OpposingHero"/> names - so nothing is selected and no prompt with
        /// one candidate is invented. Milestone 8.23 is what supplies that role at this moment;
        /// milestone 8.22 declared it.</para>
        ///
        /// <para><b>The two endings are ledger question 13's two answers</b>, settled from source at
        /// milestone 8.9. Parry prints <c>if able</c>, which p.17 makes the step non-mandatory, so an
        /// attacker with an empty hand gives nothing and the Block stands. Riposte prints "or is
        /// defeated", which is one mandatory step with two ways to complete rather than a choice either
        /// player makes - so the discard reports whether it could happen and one conditional arm runs
        /// p.13's Defeat list where it could not.</para>
        /// </summary>
        private static EffectDefinition CreateAttackerDiscard(
            CardDefinitionId cardId, string contentRevision, bool escalatesToDefeat)
        {
            var root = new EffectNodeId("riposte-root");
            var discard = new EffectNodeId("riposte-discard");
            var escalation = new EffectNodeId("riposte-escalation");
            var defeat = new EffectNodeId("riposte-defeat");

            var attacker = EffectInputBinding.FromRole(
                SyntheticCardStateCapabilities.HeroParameter,
                EffectRoleKind.OpposingHero,
                EffectValueKind.Hero);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: escalatesToDefeat
                        ? new[] { discard, escalation }
                        : new[] { discard }),
                escalatesToDefeat
                    ? new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[] { attacker },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        })
                    : new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[] { attacker })
            };

            if (escalatesToDefeat)
            {
                nodes.Add(new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
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
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticConsequenceCapabilities.HeroParameter,
                            EffectRoleKind.OpposingHero,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(
                DefenseTextEffectId(cardId), contentRevision, root, nodes);
        }

        // -----------------------------------------------------------------------------------------
        // Shared nodes and identities
        // -----------------------------------------------------------------------------------------

        /// <summary>Where the acting hero is standing, which every direct Move of his moves from.</summary>
        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        /// <summary>p.17's `you may`: a step the actor answers, at exactly the printed distance.</summary>
        private static EffectNodeDefinition OptionalStepNode(
            EffectNodeId nodeId, EffectNodeId actorSpace, int distance) =>
            MoveNode(nodeId, actorSpace, distance, EffectFailurePolicy.May);

        /// <summary>p.17's `up to`: a step that happens, whose distance may be nothing at all.</summary>
        private static EffectNodeDefinition UpToStepNode(
            EffectNodeId nodeId, EffectNodeId actorSpace, int distance) =>
            MoveNode(nodeId, actorSpace, distance, EffectFailurePolicy.UpTo);

        private static EffectNodeDefinition MoveNode(
            EffectNodeId nodeId,
            EffectNodeId actorSpace,
            int distance,
            EffectFailurePolicy policy) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, policy,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        EffectValue.Integer((int)DirectMoveDestinationPolicy.Unrestricted))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                });

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-attack-text");

        private static EffectDefinitionId SecondAttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-after-attack-text");

        private static EffectDefinitionId DefenseTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-defense-text");

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-skill");
    }
}
