using System;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Minions
{
    public static class StandardMinionArtCatalog
    {
        public static string GetResourcePath(TeamSide teamSide, StandardMinionType minionType)
        {
            var team = teamSide == TeamSide.Red
                ? "red"
                : teamSide == TeamSide.Blue
                    ? "blue"
                    : throw new ArgumentOutOfRangeException(nameof(teamSide));
            string type;
            switch (minionType)
            {
                case StandardMinionType.Melee: type = "melee"; break;
                case StandardMinionType.Ranged: type = "ranged"; break;
                case StandardMinionType.Heavy: type = "heavy"; break;
                default: throw new ArgumentOutOfRangeException(nameof(minionType));
            }

            return $"Minions/{team}-{type}";
        }

        public static string GetStandeeResourcePath(TeamSide teamSide, StandardMinionType minionType)
        {
            var flatPath = GetResourcePath(teamSide, minionType);
            return "Private/Megdan/MinionStandees/" + flatPath.Substring("Minions/".Length);
        }

        public static Sprite Load(TeamSide teamSide, StandardMinionType minionType)
        {
            var resourcePath = GetResourcePath(teamSide, minionType);
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null) throw new InvalidOperationException($"Standard minion art resource '{resourcePath}' is unavailable.");
            return sprite;
        }
    }
}
