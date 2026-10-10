using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public static class PlayerAccent
    {
        private const int SeatsPerSidePalette = 3;

        private static readonly Color[] Colours = {
            new Color32(255,59,48,255), new Color32(255,149,0,255), new Color32(255,214,10,255),
            new Color32(47,128,255,255), new Color32(32,215,255,255), new Color32(32,201,151,255)
        };

        // W-021. A side fielding more than three seats - the separate PvE executable's five heroes and
        // its enemies - continues in its own side's hues. The first three seats of each side keep
        // exactly the colours above, so every existing match draws what it always has; only a fourth
        // or later seat reads these, cycling within its own side.
        private static readonly Color[] FurtherRedColours = {
            new Color32(255,45,146,255), new Color32(196,92,52,255), new Color32(255,112,163,255),
            new Color32(214,130,40,255), new Color32(178,48,90,255)
        };

        private static readonly Color[] FurtherBlueColours = {
            new Color32(94,92,230,255), new Color32(160,120,255,255), new Color32(90,200,250,255),
            new Color32(52,170,110,255), new Color32(120,150,220,255)
        };

        public static Color For(MatchParticipants participants, HeroId hero)
        {
            var index = PlayerVisualIdentity.Index(participants, hero);
            var side = participants.GetTeamForHero(hero).Side;
            var position = index - (side == TeamSide.Blue ? SeatsPerSidePalette : 0);
            if (position < SeatsPerSidePalette)
            {
                return Colours[index];
            }

            var further = side == TeamSide.Blue ? FurtherBlueColours : FurtherRedColours;
            return further[(position - SeatsPerSidePalette) % further.Length];
        }
    }
}
