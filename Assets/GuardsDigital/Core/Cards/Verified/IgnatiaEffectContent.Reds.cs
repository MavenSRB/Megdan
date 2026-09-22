using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's Red shard bodies - milestone 8.75c. Her Red line is seven Ranged Attacks whose
    /// target is named in card text and picked by the Tie Breaker coin; the action reaches p.14's step 1
    /// with no target (<c>AttackTargetBinding.ByCardText</c>, declared on the action once a body exists),
    /// and each coin arm SELECTS a subject and binds it through <c>set-attack-target@1</c>, exactly as
    /// Dodger's modal Attacks do. Two of the seven are Basics and compose in the sibling
    /// <c>IgnatiaEffectContent.Basics.cs</c>; the five here are the Reds.
    ///
    /// <para><b>Two lines fork off Playing with Fire.</b> The STRAIGHT-LINE line (Playing with Fire ->
    /// Erratic Fireblast, and the Tier III Loosely-Aimed Firebolts) prints ☀ "a unit in range not in a
    /// straight line" / ☾ "a unit in range in a straight line", the two subjects minted in
    /// <see cref="IgnatiaAttackTargetingContent"/>. Loosely-Aimed adds "may repeat once on a different
    /// hero" to its moon arm. The ADJACENT/MAX-RANGE line (Crack of Doom, and the Tier III Imminent
    /// Eruption) prints ☀ "a unit adjacent to you" / ☾ "a unit at maximum range"; Imminent Eruption adds
    /// "may repeat once on a minion" to its sun arm.</para>
    ///
    /// <para>"adjacent to you" is Dodger's own adjacent-unit reach - <c>choose-target@2</c> at a written
    /// distance of one - and needs no minting; the three range subjects the printed-icon family cannot
    /// express are minted beside this file. The repeats are <see cref="EffectNodeKind.RepeatAction"/>
    /// nodes at <c>maximumRepeats: 1</c>, Brynn's Split Attack shape - a mandatory repeat is refused by
    /// the node validator, so "may repeat" is <see cref="EffectFailurePolicy.May"/> with the Actor as
    /// chooser.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        static partial void RedBodyImpl(
            CardDefinitionId cardId, string revision, ref EffectDefinition body)
        {
            if (cardId.Equals(IgnatiaCardContent.PlayingWithFire) ||
                cardId.Equals(IgnatiaCardContent.ErraticFireblast))
            {
                // ☀ a unit in range not in a straight line / ☾ a unit in range in a straight line.
                body = StraightLineAttack(cardId, revision, moonRepeatsOnDifferentHero: false);
            }
            else if (cardId.Equals(IgnatiaCardContent.LooselyAimedFirebolts))
            {
                // The same, and the moon arm "may repeat once on a different hero".
                body = StraightLineAttack(cardId, revision, moonRepeatsOnDifferentHero: true);
            }
            else if (cardId.Equals(IgnatiaCardContent.CrackOfDoom))
            {
                // ☀ a unit adjacent to you / ☾ a unit at maximum range.
                body = AdjacentOrMaxRangeAttack(cardId, revision, sunRepeatsOnMinion: false);
            }
            else if (cardId.Equals(IgnatiaCardContent.ImminentEruption))
            {
                // The same, and the sun arm "may repeat once on a minion".
                body = AdjacentOrMaxRangeAttack(cardId, revision, sunRepeatsOnMinion: true);
            }
        }

        /// <summary>
        /// One coin-branched straight-line Attack. The sun arm targets a unit in range NOT in a straight
        /// line, the moon arm a unit in range IN a straight line; when
        /// <paramref name="moonRepeatsOnDifferentHero"/> the moon arm follows its binding with a
        /// may-repeat on a different target.
        /// </summary>
        private static EffectDefinition StraightLineAttack(
            CardDefinitionId cardId, string revision, bool moonRepeatsOnDifferentHero)
        {
            var sunArm = new EffectNodeId("ignatia-attack-sun");
            var sunSelect = new EffectNodeId("ignatia-attack-sun-select");
            var sunBind = new EffectNodeId("ignatia-attack-sun-bind");
            var moonArm = new EffectNodeId("ignatia-attack-moon");
            var moonSelect = new EffectNodeId("ignatia-attack-moon-select");
            var moonBind = new EffectNodeId("ignatia-attack-moon-bind");
            var moonRepeat = new EffectNodeId("ignatia-attack-moon-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    sunArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { sunSelect, sunBind }),
                MintedUnitSelect(sunSelect, IgnatiaAttackTargetingContent.UnitNotInStraightLineV1),
                BindUnitAttackTarget(sunBind, sunSelect),

                new EffectNodeDefinition(
                    moonArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: moonRepeatsOnDifferentHero
                        ? new[] { moonSelect, moonBind, moonRepeat }
                        : new[] { moonSelect, moonBind }),
                MintedUnitSelect(moonSelect, IgnatiaAttackTargetingContent.UnitInStraightLineV1),
                BindUnitAttackTarget(moonBind, moonSelect)
            };

            if (moonRepeatsOnDifferentHero)
            {
                // "May repeat once on a different hero." DifferentEnemyHero, not DifferentTarget:
                // the printed phrase names a HERO, and 8.24's binding narrows the repeat to a hero
                // other than the one this instance took, exactly as Silverarrow's identical phrase does.
                nodes.Add(MayRepeatOnce(moonRepeat, RepeatTargetBinding.DifferentEnemyHero));
            }

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        /// <summary>
        /// One coin-branched adjacent/maximum-range Attack. The sun arm targets a unit adjacent to the
        /// actor, the moon arm a unit at maximum range; when <paramref name="sunRepeatsOnMinion"/> the
        /// sun arm follows its binding with a may-repeat on a minion.
        /// </summary>
        private static EffectDefinition AdjacentOrMaxRangeAttack(
            CardDefinitionId cardId, string revision, bool sunRepeatsOnMinion)
        {
            var sunArm = new EffectNodeId("ignatia-attack-sun");
            var sunSelect = new EffectNodeId("ignatia-attack-sun-select");
            var sunBind = new EffectNodeId("ignatia-attack-sun-bind");
            var sunRepeat = new EffectNodeId("ignatia-attack-sun-repeat");
            var moonArm = new EffectNodeId("ignatia-attack-moon");
            var moonSelect = new EffectNodeId("ignatia-attack-moon-select");
            var moonBind = new EffectNodeId("ignatia-attack-moon-bind");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    sunArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: sunRepeatsOnMinion
                        ? new[] { sunSelect, sunBind, sunRepeat }
                        : new[] { sunSelect, sunBind }),
                AdjacentUnitSelect(sunSelect),
                BindUnitAttackTarget(sunBind, sunSelect),

                new EffectNodeDefinition(
                    moonArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { moonSelect, moonBind }),
                MintedUnitSelect(moonSelect, IgnatiaAttackTargetingContent.UnitAtMaximumRangeV1),
                BindUnitAttackTarget(moonBind, moonSelect)
            };

            if (sunRepeatsOnMinion)
            {
                // "May repeat once on a minion." DifferentEnemyMinion is the enum member whose own
                // remarks read "on a minion" - Silverarrow's Rain of Arrows' second clause.
                nodes.Add(MayRepeatOnce(sunRepeat, RepeatTargetBinding.DifferentEnemyMinion));
            }

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        // -----------------------------------------------------------------------------------------
        // Shared node builders, used by the Reds here and the Basics beside them (one partial class).
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One of the minted range-unit selections - in/out of a straight line, or at maximum range -
        /// naming enemy units and publishing whichever kind is chosen.
        /// </summary>
        private static EffectNodeDefinition MintedUnitSelect(
            EffectNodeId nodeId, EffectCapabilityReference capability) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                capability,
                inputs: EnemyRangeInputs(),
                outputs: new[]
                {
                    new EffectOutputBinding(
                        IgnatiaAttackTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero),
                    new EffectOutputBinding(
                        IgnatiaAttackTargetingContent.ChosenMinionOutputName, EffectValueKind.Minion)
                });

        /// <summary>
        /// The minted range-HERO selection ("a hero in range, not adjacent to you"), naming enemy heroes
        /// and publishing the chosen one.
        /// </summary>
        private static EffectNodeDefinition MintedHeroSelect(
            EffectNodeId nodeId, EffectCapabilityReference capability) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                capability,
                inputs: EnemyRangeInputs(),
                outputs: new[]
                {
                    new EffectOutputBinding(
                        IgnatiaAttackTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)
                });

        /// <summary>The activity and side every one of her card-text targets is chosen under.</summary>
        private static EffectInputBinding[] EnemyRangeInputs() => new[]
        {
            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
            EffectInputBinding.FromClosedVocabulary(
                IgnatiaAttackTargetingContent.TargetRelationParameterName,
                SourceTeamRelation.OpposingSource)
        };

        /// <summary>
        /// "a hero in range" - the registered printed-Range hero selection, enemy, publishing the chosen
        /// hero. "other than the attacker" is that arm's own required narrowing and no face of hers
        /// prints it, so it is declared false.
        /// </summary>
        private static EffectNodeDefinition RangeHeroSelect(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RangeHeroTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
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
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                });

        /// <summary>
        /// "a unit / a minion / a hero adjacent to you" - Dodger's own adjacent reach, the roster
        /// selection at a written distance of one, enemy. <paramref name="kind"/> chooses which of
        /// p.19's kinds it offers, and the outputs declared match: a unit publishes both identities, a
        /// minion or a hero only its own.
        /// </summary>
        private static EffectNodeDefinition AdjacentUnitSelect(EffectNodeId nodeId) =>
            AdjacentSelect(nodeId, RosterUnitKind.Unit,
                new EffectOutputBinding(
                    IgnatiaAttackTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero),
                new EffectOutputBinding(
                    IgnatiaAttackTargetingContent.ChosenMinionOutputName, EffectValueKind.Minion));

        private static EffectNodeDefinition AdjacentSelect(
            EffectNodeId nodeId, RosterUnitKind kind, params EffectOutputBinding[] outputs) =>
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
                        SyntheticRosterCapabilities.UnitKindParameter, kind),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack)
                },
                outputs: outputs);

        /// <summary>
        /// Binds the enclosing Attack's target from a UNIT selection, which publishes whichever of the
        /// two identities was named - set-attack-target binds both and one resolves.
        /// </summary>
        private static EffectNodeDefinition BindUnitAttackTarget(
            EffectNodeId nodeId, EffectNodeId selectNode) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, selectNode,
                        IgnatiaAttackTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, selectNode,
                        IgnatiaAttackTargetingContent.ChosenMinionOutputName, EffectValueKind.Minion)
                },
                outputs: BoundAttackTargetOutputs());

        /// <summary>Binds the enclosing Attack's target from a HERO selection, which publishes one hero.</summary>
        private static EffectNodeDefinition BindHeroAttackTarget(
            EffectNodeId nodeId, EffectNodeId selectNode) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, selectNode,
                        IgnatiaAttackTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)
                },
                outputs: BoundAttackTargetOutputs());

        /// <summary>Binds the enclosing Attack's target from a MINION selection, which publishes one minion.</summary>
        private static EffectNodeDefinition BindMinionAttackTarget(
            EffectNodeId nodeId, EffectNodeId selectNode) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, selectNode,
                        IgnatiaAttackTargetingContent.ChosenMinionOutputName, EffectValueKind.Minion)
                },
                outputs: BoundAttackTargetOutputs());

        private static EffectOutputBinding[] BoundAttackTargetOutputs() => new[]
        {
            new EffectOutputBinding(
                SyntheticContinuationCapabilities.BoundHeroTargetOutput, EffectValueKind.Hero),
            new EffectOutputBinding(
                SyntheticContinuationCapabilities.BoundMinionTargetOutput, EffectValueKind.Minion)
        };

        /// <summary>
        /// "May repeat once" on the enclosing Attack, with the binding the card prints about the repeated
        /// instance. A mandatory repeat is refused by <see cref="EffectNodeDefinition"/> ("a mandatory
        /// repeat is unbounded"), so a printed "may" is <see cref="EffectFailurePolicy.May"/> with the
        /// Actor as chooser - Brynn's Split Attack shape.
        /// </summary>
        private static EffectNodeDefinition MayRepeatOnce(
            EffectNodeId nodeId, RepeatTargetBinding binding) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                chooser: EffectChooserRole.Actor,
                maximumRepeats: 1,
                repeatTargetBinding: binding);
    }
}
