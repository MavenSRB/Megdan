using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>The private Megdan package's selectable roster; content registration stays complete.</summary>
    public static class MegdanSelectionRoster
    {
        public static IReadOnlyList<RegisteredHero> SixHeroes { get; } = System.Array.AsReadOnly(
            RegisteredHeroRoster.ForThisBuild.Where(hero =>
                MegdanHeroPresentationCatalog.TryForContent(hero.ContentId, out _)).ToArray());

        public static IReadOnlyList<RegisteredHero> Current =>
#if MEGDAN_SIX_HERO_PACKAGE
            SixHeroes;
#else
            RegisteredHeroRoster.ForThisBuild;
#endif
    }
}
