using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public static class PlayerAccent
    {
        private static readonly Color[] Colours = {
            new Color32(255,59,48,255), new Color32(255,149,0,255), new Color32(255,214,10,255),
            new Color32(47,128,255,255), new Color32(32,215,255,255), new Color32(32,201,151,255)
        };
        public static Color For(MatchParticipants participants, HeroId hero) =>
            Colours[PlayerVisualIdentity.Index(participants, hero)];
    }
}
