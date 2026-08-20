using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Checks one configuration's heroes against the cards those heroes can reach: rulebook p.16's
    /// correspondence between an Item icon and a dashboard slot, asked of the content rather than of a
    /// placement.
    ///
    /// The placement refuses one card at a time, and that refusal is a rules outcome - a hero can be
    /// handed a card no slot accepts and must be told so. This check is the other half, and it is about
    /// content instead: p.15's upgrade step is not optional, so a hero who can reach a Tier II or Tier
    /// III card whose icon no slot of theirs accepts is a hero the rules can force to set aside a card
    /// they can never place. That is content stated wrongly rather than a legal position, which is why
    /// it is refused at setup, next to the upgrade relationships
    /// <see cref="CardProgressionValidator"/> checks over the same definition set.
    ///
    /// Reachability is the closure of the cards a hero owns over each definition's own stated next-Tier
    /// candidates, which is the same relationship p.15 sends the player down and the same one the
    /// configuration's restore walks. It is deliberately not "every Tier II and Tier III definition in
    /// the set": a definition another hero's tree leads to is not one this hero can be made to set
    /// aside, and refusing on it would make one hero's content decide another hero's dashboard.
    /// </summary>
    public static class HeroItemSlotSetupValidator
    {
        public static void Validate(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            var definitionsById = new Dictionary<CardDefinitionId, CardDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Card definitions must not contain null entries.", nameof(definitions));
                }

                if (!definitionsById.ContainsKey(definition.Id))
                {
                    definitionsById.Add(definition.Id, definition);
                }
            }

            var ownedByHero = new Dictionary<HeroId, List<CardDefinitionId>>();
            foreach (var instance in instances)
            {
                if (instance == null)
                {
                    throw new ArgumentException(
                        "Card instances must not contain null entries.", nameof(instances));
                }

                if (!ownedByHero.TryGetValue(instance.OwnerId, out var owned))
                {
                    owned = new List<CardDefinitionId>();
                    ownedByHero.Add(instance.OwnerId, owned);
                }

                owned.Add(instance.DefinitionReference.DefinitionId);
            }

            foreach (var hero in participants.Heroes)
            {
                if (!ownedByHero.TryGetValue(hero.Id, out var owned))
                {
                    continue;
                }

                var ownedIds = new HashSet<CardDefinitionId>(owned);
                foreach (var reachable in Reachable(owned, definitionsById))
                {
                    // Milestone 7.9d, finding F-PROG-02. p.15's Level Up is mandatory - "Leveling up is
                    // not optional" - and it is carried out by searching the levelling hero's own deck
                    // for the card the upgrade names. A hero whose deck holds no instance of a named
                    // candidate cannot complete that purchase, and the rules engine's answer to a
                    // purchase it cannot complete is to make none: no coins are spent, no Level is
                    // raised, no outcome is recorded, and because the round then sees a hero that did
                    // not level, p.8 pays that hero a Pity Coin as well. Nothing is refused and nothing
                    // is logged, so the only visible symptom is a hero that quietly never levels.
                    //
                    // The synthetic showcase content cannot expose this: it generates every upgrade
                    // target mechanically, so its decks are complete by construction. Verified hero
                    // decks are transcribed by hand, one instance at a time, which is where a missing
                    // or mis-owned card comes from - and this is the composition seam that already
                    // walks exactly the definitions it would have to be missing from.
                    foreach (var candidateId in reachable.Progression.NextTierCandidateIds)
                    {
                        if (ownedIds.Contains(candidateId))
                        {
                            continue;
                        }

                        throw new ArgumentException(
                            $"Hero '{hero.Id}' can reach card definition '{reachable.Id}', which names " +
                            $"next-Tier candidate '{candidateId}' that the hero owns no instance of. " +
                            "p.15's mandatory Level Up searches the levelling hero's own deck for the " +
                            "card it upgrades to, so a candidate with no instance there is a purchase " +
                            "that silently cannot happen.",
                            nameof(instances));
                    }

                    var icon = reachable.Progression.ItemIcon;
                    if (!reachable.Progression.HasItemIcon || hero.Dashboard.Accepts(icon))
                    {
                        continue;
                    }

                    throw new ArgumentException(
                        $"Hero '{hero.Id}' can reach card definition '{reachable.Id}', which prints a " +
                        $"{icon} Item icon that no slot on that hero's dashboard accepts (rulebook " +
                        "p.16).",
                        nameof(participants));
                }
            }
        }

        private static IEnumerable<CardDefinition> Reachable(
            IEnumerable<CardDefinitionId> ownedDefinitionIds,
            IReadOnlyDictionary<CardDefinitionId, CardDefinition> definitionsById)
        {
            var seen = new HashSet<CardDefinitionId>();
            var pending = new Queue<CardDefinitionId>(ownedDefinitionIds);
            while (pending.Count > 0)
            {
                var next = pending.Dequeue();
                if (!seen.Add(next) || !definitionsById.TryGetValue(next, out var definition))
                {
                    continue;
                }

                yield return definition;
                foreach (var candidateId in definition.Progression.NextTierCandidateIds)
                {
                    pending.Enqueue(candidateId);
                }
            }
        }
    }
}
