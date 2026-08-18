using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Checks the upgrade relationships one definition set states against the rules that constrain
    /// them. A single definition can only check itself; whether the two cards it names are the same
    /// colour and one Tier higher is a question about the set, which is why it is asked here.
    ///
    /// What it enforces, and where each part comes from - all of it p.15's Upgrade a Card step:
    ///
    /// <list type="bullet">
    /// <item>Every named candidate resolves to a definition in the same set. "Find two Action cards in
    /// your deck" is an instruction to find cards that exist; a dangling id is content that cannot be
    /// upgraded.</item>
    /// <item>Every candidate has the same colour as the card naming it - "that have the same color as
    /// the chosen card".</item>
    /// <item>Every candidate is exactly one Tier higher - "but are one Tier higher".</item>
    /// <item>Every candidate is non-Basic, because a Basic card "is never upgraded" and so is never
    /// something another card upgrades into either.</item>
    /// <item>A Tier I or Tier II card names exactly two, and a Tier III card names none. The count is
    /// the page's own ("Find two Action cards"), and the Tier III case is where its
    /// "where applicable" limit falls: the only card above Tier III is the Ultimate, which p.16 makes a
    /// Tier IV card unlocked instead of an upgrade, and which milestone 7.3 owns.</item>
    /// </list>
    ///
    /// Colour constrains progression here and never determines it. Nothing in this type derives a
    /// candidate from a colour; it reads the candidates the content states and checks the colour
    /// afterwards, which is the difference acceptance check five asks for.
    /// </summary>
    public static class CardProgressionValidator
    {
        public static void Validate(IEnumerable<CardDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var byId = new Dictionary<CardDefinitionId, CardDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Card definitions must not contain null entries.", nameof(definitions));
                }

                if (!byId.ContainsKey(definition.Id))
                {
                    byId.Add(definition.Id, definition);
                }
            }

            foreach (var definition in byId.Values)
            {
                Validate(definition, byId, nameof(definitions));
            }
        }

        private static void Validate(
            CardDefinition definition,
            IReadOnlyDictionary<CardDefinitionId, CardDefinition> byId,
            string parameterName)
        {
            var progression = definition.Progression;
            if (!progression.IsNonBasic)
            {
                // A Basic card and a definition stating no progression both reach here with no
                // candidates, because the profile refuses to give either any.
                return;
            }

            var expectedCount = progression.Tier == CardTier.III ? 0 : 2;
            if (progression.NextTierCandidateIds.Count != expectedCount)
            {
                throw new ArgumentException(
                    $"Card definition '{definition.Id}' is Tier {progression.Tier} and states " +
                    $"{progression.NextTierCandidateIds.Count} next-Tier candidates; rulebook p.15 " +
                    $"requires {expectedCount}.",
                    parameterName);
            }

            foreach (var candidateId in progression.NextTierCandidateIds)
            {
                if (candidateId.Equals(definition.Id))
                {
                    throw new ArgumentException(
                        $"Card definition '{definition.Id}' names itself as a next-Tier candidate.",
                        parameterName);
                }

                if (!byId.TryGetValue(candidateId, out var candidate))
                {
                    throw new ArgumentException(
                        $"Card definition '{definition.Id}' names next-Tier candidate " +
                        $"'{candidateId}', which this content set does not supply.",
                        parameterName);
                }

                var candidateProgression = candidate.Progression;
                if (!candidateProgression.IsNonBasic)
                {
                    throw new ArgumentException(
                        $"Card definition '{definition.Id}' names '{candidateId}' as a next-Tier " +
                        "candidate, but a Basic card is never upgraded into (rulebook p.15).",
                        parameterName);
                }

                if (candidateProgression.Color != progression.Color)
                {
                    throw new ArgumentException(
                        $"Card definition '{definition.Id}' is {progression.Color} and names " +
                        $"'{candidateId}', which is {candidateProgression.Color}; an upgrade candidate " +
                        "has the same colour as the card it upgrades (rulebook p.15).",
                        parameterName);
                }

                if ((int)candidateProgression.Tier != (int)progression.Tier + 1)
                {
                    throw new ArgumentException(
                        $"Card definition '{definition.Id}' is Tier {progression.Tier} and names " +
                        $"'{candidateId}', which is Tier {candidateProgression.Tier}; an upgrade " +
                        "candidate is exactly one Tier higher (rulebook p.15).",
                        parameterName);
                }
            }
        }
    }
}
