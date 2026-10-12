using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>W044's approved private faces. These mappings are presentation, never rule authority.</summary>
    public static class PveEnemyArtwork
    {
        /// <summary>
        /// Enemy cards reuse the five ordinary card colours as a presentation-only category key.
        /// The definition remains <see cref="CardProgressionProfile.NotStated"/>, so existing-mode
        /// effects that inspect printed colour never treat these PvE cards as coloured cards.
        /// </summary>
        public static CardColor DisplayColor(PveEnemyType type, PveOrangeCard card)
        {
            if (type == null) throw new System.ArgumentNullException(nameof(type));
            if (card == null) throw new System.ArgumentNullException(nameof(card));
            if (card.Skill != PveSkillKind.None) return CardColor.Silver;
            if (!card.AttackValue.HasValue && card.MoveValue.HasValue) return CardColor.Green;
            if (card.AttackValue.HasValue)
            {
                var strongest = type.OrangeCards.Where(value => value.AttackValue.HasValue)
                    .Max(value => value.AttackValue.Value);
                return card.AttackValue.Value == strongest ? CardColor.Red : CardColor.Gold;
            }
            return CardColor.Silver;
        }

        public static CardColor DisplayColor(PvePurpleCard card)
        {
            if (card == null) throw new System.ArgumentNullException(nameof(card));
            return CardColor.Blue;
        }

        private static readonly string[] Faces =
        {
            "melee|A|advance|Advance|9|0|0|2|None|False|Move:Minion:1",
            "melee|B|slash|Slash|7|3|0|2|None|False|Attack:Minion:1,Attack:Hero:1,Move:Minion:1",
            "melee|C|heavy-blow|Heavy Blow|4|5|0|0|None|False|Attack:Minion:1,Attack:Hero:1",
            "melee|X|shield-up|Shield Up|9|0|0|0|ImmuneThisTurn|False|",
            "ranged|A|take-position|Take Position|8|0|0|2|None|False|Move:Hero:2",
            "ranged|B|shoot|Shoot|6|3|3|1|None|False|Attack:Hero:1,Attack:Minion:1,Move:Hero:2",
            "ranged|C|power-shot|Power Shot|3|5|3|0|None|False|Attack:Hero:1,Attack:Minion:1",
            "ranged|X|aim|Aim|1|0|0|0|RangeNextTurn|False|",
            "heavy|A|lumber|Lumber|6|0|0|2|None|False|Move:Hero:1",
            "heavy|B|smash|Smash|5|6|0|1|None|False|Attack:Hero:1,Attack:Minion:1,Move:Minion:1",
            "heavy|C|hurl|Hurl|3|2|2|1|None|True|Attack:Hero:1,Attack:Minion:1,Move:Unit:1",
            "heavy|X|hook|Hook|1|0|0|0|HookClosestBlueUnit|False|",
            "heavy|P|thick-hide|Thick Hide|1|3|1",
            "heavy|Q|brace|Brace|1|5|0"
        };

        public static IReadOnlyDictionary<CardDefinitionReference, string> ResourcesFor(PveEnemyCatalog catalog)
        {
            var result = new Dictionary<CardDefinitionReference, string>();
            foreach (var text in Faces)
            {
                var fields = text.Split('|');
                var type = catalog.Types.FirstOrDefault(value => value.Id == fields[0]);
                if (type == null) continue;
                var orange = type.OrangeCards.FirstOrDefault(value => value.Key == fields[1]);
                var purple = type.PurpleCards.FirstOrDefault(value => value.Key == fields[1]);
                // Tuned cards must not show the old baked name or printed numbers.
                var matches = orange != null
                    ? orange.Name == fields[3] && orange.Initiative == int.Parse(fields[4]) &&
                      (orange.AttackValue ?? 0) == int.Parse(fields[5]) && orange.AttackRange == int.Parse(fields[6]) &&
                      (orange.MoveValue ?? 0) == int.Parse(fields[7]) && orange.Skill.ToString() == fields[8] &&
                      !orange.IsIdle && orange.RepeatOnDifferentTarget.ToString() == fields[9] &&
                      string.Join(",", orange.Options.Select(option => option.Kind + ":" + option.Target + ":" + option.StopDistance)) == fields[10]
                    : purple != null && purple.Name == fields[3] && purple.Initiative == int.Parse(fields[4]) &&
                      purple.Defense == int.Parse(fields[5]) && purple.Counterattack == int.Parse(fields[6]);
                if (!matches) continue;
                for (var copy = 1; copy <= (orange?.Copies ?? 1); copy++)
                {
                    var id = orange != null ? PveCardFactory.OrangeId(type, orange, copy) : PveCardFactory.PurpleId(type, purple);
                    result.Add(new CardDefinitionReference(id, PveCardFactory.ContentRevision),
                        "Private/PveCards/" + fields[0] + "-" + fields[2]);
                }
            }
            return result;
        }

        public static void Configure(PveEnemyCatalog catalog) => ConfiguredCardArtwork.SetModeResources(ResourcesFor(catalog));
    }
}
