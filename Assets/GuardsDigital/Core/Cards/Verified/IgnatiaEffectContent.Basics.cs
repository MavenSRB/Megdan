using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's Basic Attack bodies - milestone 8.75a. Her two Gold Attacks name their own
    /// target in card text and let the Tie Breaker coin pick which; the action reaches p.14's step 1 with
    /// no target (<c>AttackTargetBinding.ByCardText</c>) and each coin arm SELECTS a subject and binds it
    /// through <c>set-attack-target@1</c>, the same shape the Reds beside them use.
    ///
    /// <list type="bullet">
    /// <item><description>Chaos Bolt (Gold basic): ☀ a minion adjacent to you / ☾ a hero in range.
    /// Both subjects are already expressible - the adjacent minion is Dodger's roster reach at a written
    /// distance of one, and "a hero in range" is the registered printed-Range hero selection - so
    /// nothing is minted for it.</description></item>
    /// <item><description>Unreliable Jolt (the boxed Handicap): ☀ a hero adjacent to you / ☾ a hero in
    /// range, not adjacent to you. The moon subject is
    /// <see cref="IgnatiaAttackTargetingContent.HeroInRangeNotAdjacentV1"/>, minted beside this file
    /// because the registered Range hero arm carries no <c>excludeAdjacent</c>.</description></item>
    /// </list>
    ///
    /// <para>The node builders and the coin-branch root are the ones the Reds partial declares - both
    /// are the one <see cref="IgnatiaEffectContent"/> partial class.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        static partial void BasicBodyImpl(
            CardDefinitionId cardId, string revision, ref EffectDefinition body)
        {
            if (cardId.Equals(IgnatiaCardContent.ChaosBolt))
            {
                // ☀ a minion adjacent to you / ☾ a hero in range.
                body = ChaosBoltBody(cardId, revision);
            }
            else if (cardId.Equals(IgnatiaCardContent.UnreliableJolt))
            {
                // ☀ a hero adjacent to you / ☾ a hero in range, not adjacent to you.
                body = UnreliableJoltBody(cardId, revision);
            }
            else if (cardId.Equals(IgnatiaCardContent.Equilibrium))
            {
                // Silver: not coin-branched itself - it creates the round-long marker that FREES the
                // coin choice for every later primary action.
                body = EquilibriumBody(cardId, revision);
            }
        }

        /// <summary>
        /// "This round: each time you perform or repeat a primary action you may apply either ☀ or ☾
        /// card text, regardless of the Tie Breaker coin." One capability node creating the round-scoped
        /// marker <see cref="IgnatiaComponentContent.FreeCoinChoiceDefinition"/> on the acting hero.
        /// </summary>
        private static EffectDefinition EquilibriumBody(CardDefinitionId cardId, string revision)
        {
            var root = new EffectNodeId("ignatia-equilibrium-root");
            return new EffectDefinition(
                PrimaryEffectId(cardId),
                revision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        IgnatiaComponentContent.EquilibriumV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        private static EffectDefinition ChaosBoltBody(CardDefinitionId cardId, string revision)
        {
            var sunArm = new EffectNodeId("ignatia-attack-sun");
            var sunSelect = new EffectNodeId("ignatia-attack-sun-select");
            var sunBind = new EffectNodeId("ignatia-attack-sun-bind");
            var moonArm = new EffectNodeId("ignatia-attack-moon");
            var moonSelect = new EffectNodeId("ignatia-attack-moon-select");
            var moonBind = new EffectNodeId("ignatia-attack-moon-bind");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    sunArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { sunSelect, sunBind }),
                // "a minion adjacent to you" - the roster reach at distance one, minions only.
                AdjacentSelect(sunSelect, RosterUnitKind.Minion,
                    new EffectOutputBinding(
                        IgnatiaAttackTargetingContent.ChosenMinionOutputName, EffectValueKind.Minion)),
                BindMinionAttackTarget(sunBind, sunSelect),

                new EffectNodeDefinition(
                    moonArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { moonSelect, moonBind }),
                // "a hero in range" - the registered printed-Range hero selection.
                RangeHeroSelect(moonSelect),
                BindHeroAttackTarget(moonBind, moonSelect)
            };

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        private static EffectDefinition UnreliableJoltBody(CardDefinitionId cardId, string revision)
        {
            var sunArm = new EffectNodeId("ignatia-attack-sun");
            var sunSelect = new EffectNodeId("ignatia-attack-sun-select");
            var sunBind = new EffectNodeId("ignatia-attack-sun-bind");
            var moonArm = new EffectNodeId("ignatia-attack-moon");
            var moonSelect = new EffectNodeId("ignatia-attack-moon-select");
            var moonBind = new EffectNodeId("ignatia-attack-moon-bind");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    sunArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { sunSelect, sunBind }),
                // "a hero adjacent to you" - the roster reach at distance one, heroes only.
                AdjacentSelect(sunSelect, RosterUnitKind.Hero,
                    new EffectOutputBinding(
                        IgnatiaAttackTargetingContent.ChosenHeroOutputName, EffectValueKind.Hero)),
                BindHeroAttackTarget(sunBind, sunSelect),

                new EffectNodeDefinition(
                    moonArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { moonSelect, moonBind }),
                // "a hero in range, not adjacent to you" - the minted range-hero-not-adjacent selection.
                MintedHeroSelect(moonSelect, IgnatiaAttackTargetingContent.HeroInRangeNotAdjacentV1),
                BindHeroAttackTarget(moonBind, moonSelect)
            };

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }
    }
}
