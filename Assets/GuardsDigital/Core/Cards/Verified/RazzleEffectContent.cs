using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Razzle the Ringmaster's card text. Milestone 9.21d starts it with Stunt Doubles' after-the-attack
    /// spawn, which the boxed Stunted Doubles prints word for word.
    ///
    /// <para>Nothing here is registered until 9.21j deals him: the handlers are composed into a staged
    /// fixture's own catalog.</para>
    /// </summary>
    public static partial class RazzleEffectContent
    {
        /// <summary>
        /// "Spawn up to 3 more of you in radius": one body of the actor from his supply into an empty
        /// space within the action's printed Radius (with Radius items) of the acting body.
        /// </summary>
        public static EffectCapabilityReference SpawnBodyV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("razzle-spawn-body"), 1);

        /// <summary>The most bodies one Stunt Doubles spawn prints ("up to 3 more of you").</summary>
        public const int StuntDoublesPrintedSpawnCount = 3;

        /// <summary>The spawn's nodes, in order, so a paused spawn names which body it is asking about.</summary>
        public static string SpawnNode(int index) => $"stunt-doubles-spawn-{index}";

        /// <summary>
        /// Razzle's capabilities so far: the spawn of one body (9.21d), and the removal of his own
        /// bodies with the "more than one of you" gate (9.21e).
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new[]
            {
                SyntheticHeroBodyCapabilities.SpawnActorBodyHandler(SpawnBodyV1),
                SyntheticHeroBodyCapabilities.RemoveActorBodiesHandler(RemoveBodiesV1),
                SyntheticHeroBodyCapabilities.ActorHasSeveralBodiesHandler(SeveralBodiesV1)
            };

        /// <summary>
        /// "After the attack: Spawn up to 3 more of you in radius." Three optional spawns of one body
        /// each - Awaken!'s one-piece-per-node composition, without its short-supply step: p.19's Spawn
        /// takes only from the supply (RZ-Q1), so a spawn that finds no body left, or no empty space
        /// in radius, is an empty optional step (W5), and a full board of bodies spawns nothing. The
        /// rest of the sentence composes to no node: "each of you is the same hero, except when
        /// actions are performed" is the many-bodies model (9.21a-9.21c) and "If defeated, remove all
        /// of you" is its Defeat (RZ-Q3, 9.21a).
        /// </summary>
        public static EffectDefinition SpawnAfterTheAttack(CardDefinitionId card, string contentRevision)
        {
            var root = new EffectNodeId("stunt-doubles-root");
            var spawns = Enumerable.Range(1, StuntDoublesPrintedSpawnCount)
                .Select(index => new EffectNodeId(SpawnNode(index)))
                .ToArray();
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: spawns)
            };
            nodes.AddRange(spawns.Select(spawn => new EffectNodeDefinition(
                spawn, EffectNodeKind.Capability, EffectFailurePolicy.May, SpawnBodyV1,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticHeroBodyCapabilities.SpawnedOutput, EffectValueKind.Coordinate)
                })));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), contentRevision, root, nodes);
        }
    }
}
