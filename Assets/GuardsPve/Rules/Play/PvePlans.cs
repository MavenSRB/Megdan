using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Pve.Play
{
    /// <summary>
    /// PVE-D07/D13. Each enemy's public four-turn plan for a round: one of its type's listed plans,
    /// chosen by <see cref="PveSeed.PlanIndex"/> from the setup seed. Derived on every read, so it is
    /// never stored and replays, undoes and restarts with the match.
    /// </summary>
    public static class PvePlans
    {
        public static IReadOnlyList<string> PlanFor(PveComposedEncounter encounter, PveEnemyRecord enemy, int round)
        {
            if (encounter == null) throw new ArgumentNullException(nameof(encounter));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            var plans = enemy.Type.Plans;
            return plans[PveSeed.PlanIndex(encounter.Setup.Seed, round, enemy.HeroId.Value, plans.Count)];
        }

        /// <summary>The orange card the enemy's plan names for one turn (1-4).</summary>
        public static PveOrangeCard PlannedCard(PveComposedEncounter encounter, PveEnemyRecord enemy, int round, int turn)
        {
            var plan = PlanFor(encounter, enemy, round);
            return turn >= 1 && turn <= plan.Count ? enemy.Type.FindOrange(plan[turn - 1]) : null;
        }
    }

    /// <summary>The PvE card behind an enemy's card instance.</summary>
    public sealed class PveCardIndex
    {
        private readonly Dictionary<CardDefinitionId, (PveEnemyType Type, PveOrangeCard Orange, PvePurpleCard Purple)> byDefinition =
            new Dictionary<CardDefinitionId, (PveEnemyType, PveOrangeCard, PvePurpleCard)>();

        public PveCardIndex(PveEnemyCatalog catalog)
        {
            foreach (var type in catalog.Types)
            {
                foreach (var card in type.OrangeCards) byDefinition[PveCardFactory.OrangeId(type, card)] = (type, card, null);
                foreach (var card in type.PurpleCards) byDefinition[PveCardFactory.PurpleId(type, card)] = (type, null, card);
            }
        }

        public PveOrangeCard Orange(CardDefinitionId id) => byDefinition.TryGetValue(id, out var found) ? found.Orange : null;

        public PvePurpleCard Purple(CardDefinitionId id) => byDefinition.TryGetValue(id, out var found) ? found.Purple : null;
    }

    /// <summary>PVE-D10/D12/D20. What a tie the player settles for an enemy is about.</summary>
    public enum PvePromptKind
    {
        None = 0,
        EnemyAttackTarget = 1,
        EnemyMovementDestination = 2,
        EnemySpawnSpace = 3,
        EnemyInitiativeOrder = 4
    }

    /// <summary>
    /// A decision the rules give the player on an enemy's behalf. Exactly the listed commands answer
    /// it; the encounter session refuses any other command while it stands.
    /// </summary>
    public sealed class PvePrompt
    {
        public PvePrompt(PvePromptKind kind, HeroId enemy, string text, IEnumerable<DirectorCommand> answers)
        {
            Kind = kind;
            Enemy = enemy;
            Text = text;
            Answers = Array.AsReadOnly((answers ?? Enumerable.Empty<DirectorCommand>()).ToArray());
        }

        public PvePromptKind Kind { get; }
        public HeroId Enemy { get; }
        public string Text { get; }
        public IReadOnlyList<DirectorCommand> Answers { get; }
    }
}
