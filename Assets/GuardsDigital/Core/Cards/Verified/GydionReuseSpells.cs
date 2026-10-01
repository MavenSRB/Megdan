using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30d. Gydion's twelve spells whose printed text composes on contracts the engine
    /// already has: Magic Missile, Shocking Grasp, Expeditious Retreat, Burning Hands, Fireball,
    /// Vampiric Touch, Create Undead, Dominate Person, Power Word Kill, Disintegrate, Polymorph and
    /// Banishment. Each is the spell's own printed primary, performed by the hero casting it through
    /// 9.30c's borrow path, with the spell's values and the caster's items (GY-Q3); under GY-Q12 a cast
    /// is playable whenever its spell is in the book, even when the primary then does nothing.
    ///
    /// <para><b>What the composition found missing, and added as vocabulary rather than as a
    /// selection</b> (8.88's rule: extend the family): Fireball's "not adjacent ... to your friendly
    /// unit" (<see cref="AttackTargetableUnits.UnitsNotAdjacentToAFriendlyUnitOfTheAttacker"/>);
    /// Create Undead's Range channel on Dodger's Respawn
    /// (<see cref="SyntheticPlacementCapabilities.RespawnFriendlyMinionInRangeV1"/>); Disintegrate's
    /// "non-heavy" on the written-distance object selection (<c>admittedMinionClass</c> on
    /// <c>synthetic-choose-target@4</c>); Polymorph's "a token ... or an enemy minion" union
    /// (<see cref="PrintedIconObjectKind.MinionsAndTokens"/>); and Expeditious Retreat's "Move only in a
    /// straight line" (<see cref="RuleModificationKind.MovementActionOnlyInAStraightLine"/>).</para>
    ///
    /// <para>Registered with the dealing (9.30i): <see cref="ReuseSpellHandlers"/> holds the one
    /// capability these spells name that is theirs alone, and
    /// <see cref="ReuseSpellDecisionLabels"/> the one confirm-or-decline they raise.</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        /// <summary>The twelve spells this block composes, in book order.</summary>
        public static IReadOnlyList<CardDefinitionId> ReuseSpellIds => new[]
        {
            MagicMissile, ShockingGrasp, ExpeditiousRetreat, BurningHands, Fireball, VampiricTouch,
            CreateUndead, DominatePerson, PowerWordKill, Disintegrate, Polymorph, Banishment
        };

        // ----------------------------------------------------------------------------------------
        // Gold, basic
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Magic Missile (Gold basic, Basic Attack - Ranged, Attack 1, Range 3): "Target a unit in
        /// range and not adjacent to you." Swift's Prepared Shot reach without its straight line
        /// (<see cref="AttackTargetReach.PrintedRangeBeyondAdjacent"/>): the printed Range ceiling with
        /// p.19's innermost ring taken out. The sentence is the Attack's own target set, so the offer,
        /// the target choice and the resolution read one answer; there is no other text.
        /// </summary>
        private static ActionDefinition MagicMissilePrimary() =>
            new ActionDefinition(
                PrimaryRangedAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Attack, 1), Value(PrintedValueChannel.Range, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.Ranged, default,
                attackTargetPolicy: PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                    .BeyondAdjacentWithinThePrintedRange());

        /// <summary>The space the target stood on, read after Shocking Grasp's attack.</summary>
        public static EffectNodeId ShockingGraspTargetSpace { get; } = new EffectNodeId("shocking-grasp-target-space");

        /// <summary>"Move the target up to 1 space" - the node the destination is asked at.</summary>
        public static EffectNodeId ShockingGraspMove { get; } = new EffectNodeId("shocking-grasp-move");

        /// <summary>
        /// Shocking Grasp (Gold basic, Basic Attack, Attack 3): "Target a unit adjacent to you. After
        /// the attack: Move the target up to 1 space."
        ///
        /// <para>"Target a unit adjacent to you" is p.13's own sentence for a non-Ranged Attack and
        /// narrows nothing. The after-attack Move is Misa's Thunder Shot shape - the attacked space
        /// (<c>attacked-target-space@1</c>), then a direct Move of what stands there - with no "may"
        /// and p.11's "up to": <see cref="EffectFailurePolicy.UpTo"/>, so 0 is a real answer (W5/W9).
        /// Gydion is the one performing the text, so he steers it. A target the attack defeated is no
        /// longer there, and the Move finds nothing to move: a step not taken, not a failure.</para>
        /// </summary>
        private static ActionDefinition ShockingGraspPrimary()
        {
            var root = new EffectNodeId("shocking-grasp-root");
            var text = new EffectDefinition(
                new EffectDefinitionId(ShockingGrasp.Value + "-attack-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { ShockingGraspTargetSpace, ShockingGraspMove }),
                    new EffectNodeDefinition(ShockingGraspTargetSpace, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[] { new EffectOutputBinding(SpaceOutput, EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(ShockingGraspMove, EffectNodeKind.Capability,
                        EffectFailurePolicy.UpTo, SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", ShockingGraspTargetSpace, SpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[] { new EffectOutputBinding(ReuseMovedOutput, EffectValueKind.Coordinate) })
                });

            return new ActionDefinition(
                PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Attack, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged, default, text,
                cardTextTiming: CardTextTiming.AfterTheAttack);
        }

        /// <summary>"Move only in a straight line" - the This Turn effect Expeditious Retreat raises before its walk.</summary>
        public static EffectCapabilityReference ExpeditiousRetreatStraightLineV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("gydion-expeditious-retreat-straight-line"), 1);

        /// <summary>
        /// The walk's restriction, as Wuk's Trample grants his walk's permission: a This Turn effect on
        /// the hero performing it, raised by p.11's default "before movement" text, read on the Movement
        /// action's own walk (<see cref="RuleModificationKind.MovementActionOnlyInAStraightLine"/>).
        /// </summary>
        public static ActiveEffectDefinition ExpeditiousRetreatStraightLine { get; } = new ActiveEffectDefinition(
            "gydion-expeditious-retreat-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[]
            {
                ActiveEffectCancellationCondition.ControllerHeroDefeated,
                ActiveEffectCancellationCondition.CardsRetrieved
            },
            false,
            "GydionStraightLineWalk",
            "gydion-expeditious-retreat",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForMovementActionOnlyInAStraightLine() });

        /// <summary>
        /// Expeditious Retreat (Gold basic, Basic Movement, Movement 5): "Move only in a straight line."
        ///
        /// <para>A Movement action (GY-Q3): p.11's walk of up to the Movement Total, the spell's 5 with
        /// the caster's Movement item. Its text is p.11's default "before movement" moment, and what it
        /// says is about the walk itself, so it raises the restriction the walk reads - every leg one
        /// p.19 straight line from where it started, never through an obstacle. Moving 0 is still the
        /// Movement action (p.11's "up to").</para>
        /// </summary>
        private static ActionDefinition ExpeditiousRetreatPrimary()
        {
            var root = new EffectNodeId("expeditious-retreat-straight-line");
            return new ActionDefinition(
                PrimaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Movement, 5) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(ExpeditiousRetreat.Value + "-before-movement"), ContentRevision, root,
                    new[]
                    {
                        new EffectNodeDefinition(root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            ExpeditiousRetreatStraightLineV1,
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                            })
                    }),
                cardTextTiming: CardTextTiming.BeforeTheMovement);
        }

        // ----------------------------------------------------------------------------------------
        // Red (flame)
        // ----------------------------------------------------------------------------------------

        /// <summary>Burning Hands' "Up to 1 enemy hero adjacent to the target" - the hero it hits.</summary>
        public static EffectNodeId BurningHandsHero { get; } = new EffectNodeId("burning-hands-hero");

        /// <summary>
        /// Burning Hands (Red I, Attack, Attack 5): "Target a unit adjacent to you. Before the attack:
        /// Up to 1 enemy hero adjacent to the target discards a card, if able."
        ///
        /// <para>W3 (user, 2026-09-28): "Up to N enemy heroes ... discard" hits every such hero present,
        /// up to N - an <see cref="EffectFailurePolicy.IfAble"/> Optional with no chooser, Min's Death
        /// Grenade shape; where two stand there, Gydion picks which. The hero is Arien's Noble Blade
        /// selection read off the target's space (<see cref="RosterQueryOrigin.EnclosingTargetBeforeConsequences"/>,
        /// reach 1, the target itself excluded - nothing is adjacent to itself). "If able" is the
        /// skipping kind (Cutter's Bombardment): a hero holding nothing gives nothing, and the discarded
        /// card is that hero's own choice (p.10). The selection declares no consumer, for Bombardment's
        /// and the Death Grenade's reason: the hero is hit whether or not he holds a card.</para>
        /// </summary>
        private static ActionDefinition BurningHandsPrimary()
        {
            var root = new EffectNodeId("burning-hands-up-to-one");
            var hit = new EffectNodeId("burning-hands-hit");
            var discard = new EffectNodeId("burning-hands-discard");
            var text = new EffectDefinition(
                new EffectDefinitionId(BurningHands.Value + "-attack-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                        childNodeIds: new[] { hit }),
                    new EffectNodeDefinition(hit, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { BurningHandsHero, discard }),
                    new EffectNodeDefinition(BurningHandsHero, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.EnclosingTargetBeforeConsequences),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.ExcludeTargetParameter, EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter, BurningHandsHero,
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                        })
                });

            return new ActionDefinition(
                PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Attack, 5) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged, default, text,
                cardTextTiming: CardTextTiming.BeforeTheAttack);
        }

        /// <summary>
        /// Fireball (Red II, Attack - Ranged, Attack 5, Range 3): "Target a unit in range, not adjacent
        /// to you or to your friendly unit."
        ///
        /// <para>Not adjacent to Gydion AND not adjacent to any unit friendly to him: Magic Missile's
        /// reach (<see cref="AttackTargetReach.PrintedRangeBeyondAdjacent"/>) with the friendly half as
        /// the Attack's own noun (<see cref="AttackTargetableUnits.UnitsNotAdjacentToAFriendlyUnitOfTheAttacker"/>,
        /// Silverarrow's precedent for a neighbourhood sentence on a single Attack). p.17: a friendly
        /// unit is another hero or minion on his team, never himself.</para>
        /// </summary>
        private static ActionDefinition FireballPrimary() =>
            new ActionDefinition(
                PrimaryRangedAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Attack, 5), Value(PrintedValueChannel.Range, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.Ranged, default,
                attackTargetPolicy: PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                    .BeyondAdjacentWithinThePrintedRange()
                    .NotAdjacentToAFriendlyUnitOfTheAttacker());

        // ----------------------------------------------------------------------------------------
        // Red (skull)
        // ----------------------------------------------------------------------------------------

        /// <summary>Vampiric Touch's "You may retrieve a discarded card" - p.10's confirm-or-decline.</summary>
        public static EffectNodeId VampiricTouchMayRetrieve { get; } = new EffectNodeId("vampiric-touch-may-retrieve");

        /// <summary>
        /// Vampiric Touch (Red II, Attack, Attack 5): "Target a unit adjacent to you. After the attack:
        /// You may retrieve a discarded card."
        ///
        /// <para>Garrus's Hold Ground shape at p.14's step 7: a <c>may</c> Optional around
        /// <c>retrieve-card@1</c> from the actor's own discard pile, the actor choosing (p.10's retrieve:
        /// "one of your cards"). A spell is never in a discard pile - it lies in the book or removed
        /// faceup (9.30b) - so the card retrieved is one of his action cards. With an empty pile the
        /// question is not asked (8.29e).</para>
        /// </summary>
        private static ActionDefinition VampiricTouchPrimary()
        {
            var retrieve = new EffectNodeId("vampiric-touch-retrieve");
            var text = new EffectDefinition(
                new EffectDefinitionId(VampiricTouch.Value + "-attack-text"), ContentRevision, VampiricTouchMayRetrieve,
                new[]
                {
                    new EffectNodeDefinition(VampiricTouchMayRetrieve, EffectNodeKind.Optional,
                        EffectFailurePolicy.May, childNodeIds: new[] { retrieve }, chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary("kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) })
                });

            return new ActionDefinition(
                PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Attack, 5) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged, default, text,
                cardTextTiming: CardTextTiming.AfterTheAttack);
        }

        /// <summary>
        /// Create Undead (Red II, Skill - Ranged, Range 3): "Respawn a friendly minion in an empty
        /// friendly spawn point in range in the battle zone."
        ///
        /// <para>Dodger's Necromastery sentence with "in range" for "in radius": p.19's Respawn into an
        /// empty friendly minion spawn point in the Battle Zone, within the printed Range with p.16's
        /// Range item (<see cref="SyntheticPlacementCapabilities.RespawnFriendlyMinionInRangeV1"/>). The
        /// point decides what comes back (D-07) and p.19's count decides whether anything may.</para>
        /// </summary>
        private static ActionDefinition CreateUndeadPrimary()
        {
            var respawn = new EffectNodeId("create-undead-respawn");
            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Range, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(CreateUndead.Value + "-skill"), ContentRevision, respawn,
                    new[]
                    {
                        new EffectNodeDefinition(respawn, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticPlacementCapabilities.RespawnFriendlyMinionInRangeV1,
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticPlacementCapabilities.RespawnedOutputName, EffectValueKind.Minion),
                                new EffectOutputBinding(
                                    SyntheticPlacementCapabilities.RespawnedAtOutputName, EffectValueKind.Coordinate)
                            })
                    }));
        }

        // ----------------------------------------------------------------------------------------
        // Blue (eye)
        // ----------------------------------------------------------------------------------------

        /// <summary>Dominate Person's "Target an enemy hero in radius".</summary>
        public static EffectNodeId DominatePersonHero { get; } = new EffectNodeId("dominate-person-hero");

        /// <summary>Dominate Person's "an enemy minion in radius adjacent to the target".</summary>
        public static EffectNodeId DominatePersonMinion { get; } = new EffectNodeId("dominate-person-minion");

        /// <summary>
        /// Dominate Person (Blue II, Skill, Radius 3): "Target an enemy hero in radius. Defeat an enemy
        /// minion in radius adjacent to the target."
        ///
        /// <para><b>GY-Q8 (user, 2026-10-01): only an enemy hero with an enemy minion in radius adjacent
        /// to it is offered</b> - 6.8l's rule, a selection offers what its step can act on. The hero
        /// selection names the minion selection as its consumer; that selection reads the hero's space
        /// (<c>adjacentToSpace</c>), so a hero beside no such minion is not a candidate. Both are the
        /// printed Radius with p.16's item (R1), both "Target" (p.19), so a unit immune to Gydion's Skill
        /// is not offered. The Defeat is p.13's ordinary one by Gydion (<c>synthetic-defeat-minion</c>):
        /// he collects its coins.</para>
        /// </summary>
        private static ActionDefinition DominatePersonPrimary()
        {
            var root = new EffectNodeId("dominate-person-root");
            var defeat = new EffectNodeId("dominate-person-defeat");
            var text = new EffectDefinition(
                new EffectDefinitionId(DominatePerson.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { DominatePersonHero, DominatePersonMinion, defeat }),
                    new EffectNodeDefinition(DominatePersonHero, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(DominatePersonMinion))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(DominatePersonMinion, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.AdjacentToSpaceParameterName, DominatePersonHero,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(defeat))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatMinionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.MinionParameter, DominatePersonMinion,
                                SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedMinionOutput, EffectValueKind.Minion)
                        })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text);
        }

        /// <summary>
        /// Power Word Kill (Blue III, Skill, Radius 3): "Defeat an enemy hero in radius with no cards in
        /// hand."
        ///
        /// <para>Rowenna's Accept Surrender on the printed Radius: "with no cards in hand" narrows who
        /// may be chosen (<c>requiresEmptyHand</c>, <c>HeroHandQuery</c>), so a hero holding a card is
        /// never offered and then refused. The Defeat is p.13's ordinary one by Gydion
        /// (<c>synthetic-defeat-hero@1</c>): the defeated hero's team spends Lives by its lowest Tier,
        /// Gydion collects the Defeat coins and his team-mates the assists.</para>
        /// </summary>
        private static ActionDefinition PowerWordKillPrimary()
        {
            var root = new EffectNodeId("power-word-kill-root");
            var victim = new EffectNodeId("power-word-kill-victim");
            var defeat = new EffectNodeId("power-word-kill-defeat");
            var text = new EffectDefinition(
                new EffectDefinitionId(PowerWordKill.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { victim, defeat }),
                    new EffectNodeDefinition(victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.RequiresEmptyHandParameterName, EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatHeroV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.HeroParameter, victim,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                        })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text);
        }

        // ----------------------------------------------------------------------------------------
        // Blue (ring)
        // ----------------------------------------------------------------------------------------

        /// <summary>Disintegrate's "a token or an enemy non-heavy minion adjacent to you".</summary>
        public static EffectNodeId DisintegrateSubject { get; } = new EffectNodeId("disintegrate-subject");

        /// <summary>
        /// Disintegrate (Blue II, Skill, no icon): "Remove a token or an enemy non-heavy minion adjacent
        /// to you."
        ///
        /// <para>One selection over the union (the reason <c>choose-swap-subject@1</c> records): "adjacent
        /// to you" is a number the sentence writes down, so it is the written-distance object selection
        /// at 1, Mrak's Boulder Rush grammar - "an enemy unit or a token" is
        /// <see cref="SourceTeamRelation.OpposingSourceOrUnaligned"/> - narrowed to "non-heavy"
        /// (<see cref="RosterMinionClass.RangedOrMelee"/>). Its consumer is p.19's Remove of a game piece
        /// (a minion or a token, never a hero), so an enemy hero standing beside him is not offered.
        /// A Remove is not a Defeat: no coins (p.19). A removed minion rejoins the reserve.</para>
        /// </summary>
        private static ActionDefinition DisintegratePrimary()
        {
            var root = new EffectNodeId("disintegrate-root");
            var remove = new EffectNodeId("disintegrate-remove");
            var text = new EffectDefinition(
                new EffectDefinitionId(Disintegrate.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { DisintegrateSubject, remove }),
                    new EffectNodeDefinition(DisintegrateSubject, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSourceOrUnaligned),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.AdmittedMinionClassParameterName,
                                RosterMinionClass.RangedOrMelee),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(remove))
                        },
                        outputs: new[] { new EffectOutputBinding(ReuseChosenOutput, EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.RemoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", DisintegrateSubject, ReuseChosenOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                RemoveEligiblePieceKind.GamePiece)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                            new EffectOutputBinding(RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                        })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text);
        }

        /// <summary>Polymorph's "an enemy hero in radius".</summary>
        public static EffectNodeId PolymorphHero { get; } = new EffectNodeId("polymorph-hero");

        /// <summary>Polymorph's "a token in radius or ... an enemy minion in radius".</summary>
        public static EffectNodeId PolymorphSubject { get; } = new EffectNodeId("polymorph-subject");

        /// <summary>
        /// Polymorph (Blue III, Skill, Radius 3): "Swap an enemy hero in radius with a token in radius
        /// or with an enemy minion in radius."
        ///
        /// <para>p.19's Swap of two named spaces (<c>synthetic-swap@2</c>, any game piece): the enemy
        /// hero first, then one selection over the union "a token ... or an enemy minion" on the printed
        /// Radius (R1) - <see cref="PrintedIconObjectKind.MinionsAndTokens"/> with
        /// <see cref="SourceTeamRelation.OpposingSourceOrUnaligned"/>, a token being on no side. Gydion
        /// chooses both. p.18's immunity is asked of each - by the selections and again by the Swap - so
        /// a Heavy minion its p.13 escort protects is not offered. The hero selection names the second
        /// as its consumer (6.8l), so a hero with nothing in radius to trade places with is not
        /// offered.</para>
        /// </summary>
        private static ActionDefinition PolymorphPrimary()
        {
            var root = new EffectNodeId("polymorph-root");
            var swap = new EffectNodeId("polymorph-swap");
            var text = new EffectDefinition(
                new EffectDefinitionId(Polymorph.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { PolymorphHero, PolymorphSubject, swap }),
                    new EffectNodeDefinition(PolymorphHero, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(PolymorphSubject))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(PolymorphSubject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            // p.19's Swap exchanges TWO objects, so the second is never the hero named
                            // first. The clause is what lets the hero selection name this one as its
                            // consumer (6.8l): a hero with no token or enemy minion in radius to trade
                            // places with is not offered.
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.ExcludedHeroParameterName, PolymorphHero,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSourceOrUnaligned),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                                PrintedIconObjectKind.MinionsAndTokens),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(swap))
                        },
                        outputs: new[] { new EffectOutputBinding(ReuseChosenOutput, EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "first", PolymorphHero, SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                "second", PolymorphSubject, ReuseChosenOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.GamePiece)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                        })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text);
        }

        // ----------------------------------------------------------------------------------------
        // Green (hand)
        // ----------------------------------------------------------------------------------------

        /// <summary>Banishment's "a unit or a token adjacent to you".</summary>
        public static EffectNodeId BanishmentSubject { get; } = new EffectNodeId("banishment-subject");

        /// <summary>"into a space in radius" - the node the destination is asked at.</summary>
        public static EffectNodeId BanishmentPlace { get; } = new EffectNodeId("banishment-place");

        /// <summary>
        /// Banishment (Green II, Skill, Radius 3): "Place a unit or a token adjacent to you into a space
        /// in radius."
        ///
        /// <para>Wasp's Telekinesis turned round: the piece is the one adjacent ("adjacent to you" is a
        /// written number, so the written-distance object selection at 1, either side - the sentence
        /// names none) and the destination is the printed Radius measured from Gydion (R1,
        /// <c>synthetic-place-in-radius@1</c>), an empty space (p.18). The selection's consumer is the
        /// Place of a unit or token, so nothing else standing beside him is offered, and p.18's immunity
        /// is asked of the piece - a Heavy minion its p.13 escort protects stays put.</para>
        /// </summary>
        private static ActionDefinition BanishmentPrimary()
        {
            var root = new EffectNodeId("banishment-root");
            var text = new EffectDefinition(
                new EffectDefinitionId(Banishment.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { BanishmentSubject, BanishmentPlace }),
                    new EffectNodeDefinition(BanishmentSubject, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(BanishmentPlace))
                        },
                        outputs: new[] { new EffectOutputBinding(ReuseChosenOutput, EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(BanishmentPlace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRadiusV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", BanishmentSubject, ReuseChosenOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                PlaceEligiblePieceKind.UnitOrToken),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                PlaceDestinationAnchor.ActingHero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                PlaceSpawnPointExclusion.None)
                        },
                        outputs: new[] { new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate) })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text);
        }

        // ----------------------------------------------------------------------------------------
        // What 9.30i aggregates with the dealing
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// The capability these twelve name that is theirs alone: Expeditious Retreat's straight-line
        /// walk (an Active effect over the registered <see cref="ActiveEffectCreationHandler"/> type),
        /// registered with the dealing (9.30i). Create Undead's Respawn registers from
        /// <see cref="SyntheticPlacementCapabilities.RegisteredHandlers"/>.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ReuseSpellHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActiveEffectCreationHandler(ExpeditiousRetreatStraightLineV1, ExpeditiousRetreatStraightLine)
            };

        /// <summary>
        /// The one confirm-or-decline these twelve raise - Vampiric Touch's "You may retrieve a
        /// discarded card" - in the words the roster's other "you may retrieve a discarded card" steps
        /// say (Garrus's, Ursafar's, Whisper's, Xargatha's). 9.30i aggregates it into
        /// <see cref="DecisionLabelCatalog"/> with the dealing.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> ReuseSpellDecisionLabels() =>
            new[]
            {
                DecisionLabelDeclaration.DecisionStep(
                    VampiricTouch, VampiricTouchMayRetrieve.Value,
                    "Retrieve a discarded card", "Leave my discard as it is")
            };

        private const string ReuseChosenOutput = "chosen";
        private const string ReuseMovedOutput = "moved";
    }
}
