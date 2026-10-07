using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public static class PlayerAccent
    {
        private static readonly Color[] Colours = {
            new Color32(255,118,24,255), new Color32(255,213,48,255), new Color32(238,66,196,255),
            new Color32(35,211,238,255), new Color32(91,226,89,255), new Color32(169,111,255,255)
        };
        public static Color For(MatchParticipants participants, HeroId hero) =>
            Colours[PlayerVisualIdentity.Index(participants, hero)];
    }
}
