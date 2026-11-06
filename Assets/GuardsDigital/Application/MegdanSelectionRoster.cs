using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>The private Megdan package's selectable roster; content registration stays complete.</summary>
    public static class MegdanSelectionRoster
    {
        public static readonly bool IsDemo =
#if MEGDAN_SIX_HERO_PACKAGE
            true;
#else
            false;
#endif
        public static IReadOnlyList<RegisteredHero> DemoOriginals { get; } = System.Array.AsReadOnly(
            new[] { "misa", "rowenna", "tali" }.Select(id => RegisteredHeroRoster.ForThisBuild.Single(h => h.ContentId.Value == id)).ToArray());
        public static IReadOnlyList<RegisteredHero> DemoHeroes { get; } = System.Array.AsReadOnly(
            DemoOriginals.Concat(DemoOriginals.Select(h => new RegisteredHero(
                h.HeroIdValue + "-demo-blue", h.ContentId, h.DisplayName, h.Complexity, h.Roles))).ToArray());

        public static bool AllowsDemoTeam(RegisteredHero hero, TeamSide side) =>
            (side == TeamSide.Blue) == hero.HeroIdValue.EndsWith("-demo-blue", System.StringComparison.Ordinal);

        public static IReadOnlyList<RegisteredHero> ForSeats(IEnumerable<TeamSide> sides)
        {
            if (!IsDemo) return RegisteredHeroRoster.ForThisBuild;
            return sides.Contains(TeamSide.Red) ? DemoHeroes : DemoOriginals;
        }

        public static bool TryResolve(string id, out RegisteredHero hero, bool allowDemo = false)
        {
            hero = IsDemo || allowDemo ? DemoHeroes.FirstOrDefault(h => h.HeroIdValue == id) : default;
            return hero.ContentId.IsDefined || RegisteredHeroRoster.TryFind(id, out hero);
        }
        public static IReadOnlyList<RegisteredHero> SixHeroes { get; } = System.Array.AsReadOnly(
            RegisteredHeroRoster.ForThisBuild.Where(hero =>
                MegdanHeroPresentationCatalog.TryForContent(hero.ContentId, out _)).ToArray());

        public static IReadOnlyList<RegisteredHero> Current =>
#if MEGDAN_SIX_HERO_PACKAGE
            DemoHeroes;
#else
            RegisteredHeroRoster.ForThisBuild;
#endif
    }
}
