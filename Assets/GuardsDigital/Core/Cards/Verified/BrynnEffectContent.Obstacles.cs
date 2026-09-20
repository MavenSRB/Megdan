using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's effect bodies for the clauses milestone 8.69a owns: Familiar Ground's modal,
    /// Over the Top's permanent ability, and the two reusable sub-graphs the rest of his series builds
    /// its conditions out of.
    ///
    /// <para><b>Two of this block's four faces compose no effect at all, and that is the composition
    /// rather than a gap.</b> High Ground and Elevated Ambush print "Target a unit adjacent to you. If
    /// you target a hero adjacent to 3 or more obstacles, +2 [Attack] attack." The first sentence is
    /// rulebook p.13's own reading of a non-Ranged Attack, which every Attack definition already
    /// carries; the second is a value on the Attack total, which this build expresses as a
    /// <c>RosterValueContributionDefinition</c> on the ACTION - Tigerclaw's Backstab shape, recorded as
    /// BR-Q6. Card text that said either would be a second statement of a rule the definition already
    /// makes.</para>
    ///
    /// <para><b>What the wave blocks compose on.</b> 8.69b, 8.69c, 8.69d and 8.69e each own faces whose
    /// text conditions on the same printed phrase, so the two predicate nodes and the gate below are
    /// declared here, once, rather than in four partials. They are node FACTORIES rather than whole
    /// definitions, because what surrounds them is each block's own printed sentence.</para>
    /// </summary>
    public static partial class BrynnEffectContent
    {
        /// <summary>The node id Over the Top's permanent ability is minted from.</summary>
        public const string OverTheTopRuleId = "brynn-over-the-top";

        /// <summary>
        /// The rule kind <c>UnlockedUltimate</c> builds its continuous source from - the name every
        /// reader of a standing ability matches on.
        /// </summary>
        public const string OverTheTopRuleKind = "BrynnOverTheTop";

        /// <summary>
        /// "While you are performing actions all enemy heroes in play count as adjacent to 3 or more
        /// obstacles." - his Purple Tier IV, whole, and the only thing that card contains.
        ///
        /// <para><b>One declaration, not one per enemy hero.</b> Who the enemies are is a fact about
        /// the board rather than about the card, so
        /// <see cref="RuleModificationQuery"/> expands this into one applied modification per enemy
        /// hero standing on the board while he acts - the same expansion an Active effect's
        /// team-scoped declaration already gets, and for the same reason: that derivation holds the
        /// board and the participants.</para>
        ///
        /// <para><b>"While you are performing actions" is the declaration's own applicability</b>
        /// (<see cref="RuleValueApplicability.ControllerAction"/>) and not a condition any consumer
        /// re-asks. BR-Q2 reads it as every action he is the acting hero of - primary, secondary and
        /// p.14's "perform a Defense action" - and never while another hero acts; p.17's Counts As
        /// changes nothing else about the heroes it reaches, and minions are untouched because the
        /// sentence names heroes.</para>
        /// </summary>
        public static IReadOnlyList<RuleModification> OverTheTopModifications { get; } =
            Array.AsReadOnly(new[]
            {
                RuleModification.ForEnemyHeroesCountAsAdjacentToThreeOrMoreObstacles()
            });

        // -----------------------------------------------------------------------------------------
        // Familiar Ground
        // -----------------------------------------------------------------------------------------

        /// <summary>The modal's own node id, so a paused chain names what it is owed.</summary>
        public const string FamiliarGroundModalNode = "familiar-ground-modal";

        /// <summary>"Target a unit adjacent to you." - bullet 1.</summary>
        public const string FamiliarGroundAdjacentBulletNode = "familiar-ground-adjacent-bullet";

        /// <summary>
        /// "Target a hero in range who is adjacent to 3 or more obstacles." - bullet 2.
        /// </summary>
        public const string FamiliarGroundObstacleBulletNode = "familiar-ground-obstacle-bullet";

        /// <summary>
        /// "Choose one — • Target a unit adjacent to you. • Target a hero in range who is adjacent to 3
        /// or more obstacles. ( You, other heroes, minions, tokens and terrain are obstacles )."
        ///
        /// <para><b>It is p.10's modal, composed as Bain's Hand Crossbow is</b> - <c>Choose</c> 1..1
        /// over two <c>attack-sequence@1</c> bullets, each publishing its own printed targeting
        /// sentence, with the card-action authority running a complete p.14 for the one bullet the
        /// actor took. BR-Q3 is the reading: one bullet is taken and the other ignored, and the second
        /// bullet's narrowing is a property of the CANDIDATE read when that bullet's own sequence
        /// derives its target set - so a hero who is boxed in is not offered, rather than offered and
        /// then refused.</para>
        ///
        /// <para><b>Bullet 2 reaches heroes only and bullet 1 reaches p.19's units</b>, because those
        /// are the nouns the two sentences print. The reach differs the same way: bullet 1 is p.19's
        /// innermost ring and bullet 2 is the card's own printed Range icon with p.16's items composed
        /// onto it, which is what makes his Range item matter to the face that prints one.</para>
        ///
        /// <para><b>"(on different targets)" is not printed</b>, and a face that can only ever take one
        /// bullet is saying nothing either way - so both bullets declare it false, exactly as Hand
        /// Crossbow's two do.</para>
        ///
        /// <para>The parenthetical reminder is a restatement of p.18 and carries no rule of its own;
        /// it is transcribed on the card and composed nowhere.</para>
        /// </summary>
        public static EffectDefinition FamiliarGround(CardDefinitionId cardId, string contentRevision)
        {
            var modal = new EffectNodeId(FamiliarGroundModalNode);
            var adjacentBullet = new EffectNodeId(FamiliarGroundAdjacentBulletNode);
            var obstacleBullet = new EffectNodeId(FamiliarGroundObstacleBulletNode);

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentBullet, obstacleBullet },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),

                    BulletNode(
                        adjacentBullet,
                        AttackTargetableUnits.HeroesAndMinions,
                        AttackTargetReach.AdjacentOnly,
                        AttackSequenceSubjectNarrowing.None),

                    BulletNode(
                        obstacleBullet,
                        AttackTargetableUnits.HeroesOnly,
                        AttackTargetReach.PrintedRange,
                        AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles)
                });
        }

        // -----------------------------------------------------------------------------------------
        // The sub-graphs the rest of the series composes on
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "If an enemy hero in radius is adjacent to 3 or more obstacles" as one node - Mountain
        /// Guide's and Expedition Leader's second sentence, and True Grit's and Die Hard's.
        ///
        /// <para>It publishes <c>present</c> and applies nothing, so
        /// <see cref="GateOnPredicate"/> can hang the conditional sentence off it. The disk it measures
        /// is the performing card's own printed Radius with items - see the handler.</para>
        /// </summary>
        public static EffectNodeDefinition EnemyHeroInRadiusAdjacentToObstaclesNode(
            EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstaclesV1,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        BrynnComponentContent.PresentOutputName, EffectValueKind.Boolean)
                });

        /// <summary>
        /// "a hero who is adjacent to 3 or more obstacles" asked about a hero an earlier node bound -
        /// the gate Split Attack, Split Throw and Peak Precision read off the Attack target the first
        /// instance chose.
        ///
        /// <para>The subject arrives as a node output rather than as a literal, because the hero is
        /// whoever the action targeted; <paramref name="subjectOutputName"/> is the name the binding
        /// node published them under.</para>
        /// </summary>
        public static EffectNodeDefinition HeroAdjacentToObstaclesNode(
            EffectNodeId nodeId, EffectNodeId subjectNodeId, string subjectOutputName) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                BrynnComponentContent.HeroAdjacentToObstaclesV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        BrynnComponentContent.SubjectParameterName,
                        subjectNodeId,
                        subjectOutputName,
                        EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        BrynnComponentContent.PresentOutputName, EffectValueKind.Boolean)
                });

        /// <summary>
        /// The printed "If ..." over either predicate: a <see cref="EffectNodeKind.Conditional"/> whose
        /// flag is the predicate node's own <c>present</c> output, running
        /// <paramref name="childNodeIds"/> when it holds and nothing when it does not.
        ///
        /// <para><c>synthetic-is-flagged@1</c> is the shared gate every conditional sentence in this
        /// build reads, and it declares no negation - so a card that printed the negative would negate
        /// at the predicate rather than here, which is where this build puts one.</para>
        /// </summary>
        public static EffectNodeDefinition GateOnPredicate(
            EffectNodeId nodeId, EffectNodeId predicateNodeId, params EffectNodeId[] childNodeIds) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: childNodeIds,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "flag",
                        predicateNodeId,
                        BrynnComponentContent.PresentOutputName,
                        EffectValueKind.Boolean)
                },
                outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });

        // -----------------------------------------------------------------------------------------
        // Shared nodes and identities
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
            AttackSequenceSubjectNarrowing subjectNarrowing)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.TargetableUnitsParameter, targetableUnits),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.ReachParameter, reach),

                // Neither bullet prints a line clause, so neither declares one.
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.LineParameter, AttackTargetLine.AnyLine),

                // "Choose one" can take one bullet only, so the card says nothing either way about
                // two bullets sharing a unit - RULING 19's rider, read the way Hand Crossbow reads it.
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

            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSequenceCapabilities.AttackSequenceV1,
                inputs: inputs,
                outputs: outputs);
        }

        /// <summary>The conventional effect identity for one face's Attack card text.</summary>
        public static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");

        /// <summary>The conventional effect identity for one face's Skill card text.</summary>
        public static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        /// <summary>The conventional effect identity for one face's Movement card text.</summary>
        public static EffectDefinitionId MovementTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-movement-text");
    }
}
