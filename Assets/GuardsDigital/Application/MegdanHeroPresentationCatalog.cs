using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>
    /// W-161's temporary, private presentation mapping. Content and hero identifiers remain the
    /// Guards identifiers; only portrait selection and player-facing words are adapted here.
    /// </summary>
    public sealed class MegdanHeroPresentation
    {
        public MegdanHeroPresentation(
            string contentId,
            string portraitFile,
            string originalCardArtFile,
            string displayNameOverride = null)
        {
            ContentId = new HeroContentId(contentId);
            HeroIdValue = "hero-" + contentId;
            PortraitFile = portraitFile ?? throw new ArgumentNullException(nameof(portraitFile));
            OriginalCardArtFile = originalCardArtFile ??
                throw new ArgumentNullException(nameof(originalCardArtFile));
            DisplayNameOverride = displayNameOverride;
        }

        public HeroContentId ContentId { get; }
        public string HeroIdValue { get; }
        public string PortraitFile { get; }
        public string OriginalCardArtFile { get; }
        public string DisplayNameOverride { get; }
        public string CardArtResourcePath => "Private/Megdan/HeroPortraits/" + ContentId.Value;

        public string DisplayName(string fallback) =>
            string.IsNullOrWhiteSpace(DisplayNameOverride) ? fallback : DisplayNameOverride;
    }

    public static class MegdanHeroPresentationCatalog
    {
        private static readonly MegdanHeroPresentation[] Declared =
        {
            new MegdanHeroPresentation("brogan", "ram-warrior-portrait-v1.png",
                "codex-clipboard-65b29d30-6149-452d-b91c-17c1af29df33.jpg"),
            new MegdanHeroPresentation("misa", "cat-duelist-portrait-v1.png",
                "codex-clipboard-20569685-3938-48b7-bfa2-0acc719a0233.jpg", "Riki"),
            new MegdanHeroPresentation("rowenna", "knight-portrait-v1.png",
                "codex-clipboard-4bd36c08-4288-4ee7-a515-b8d5eca211da.jpg"),
            new MegdanHeroPresentation("tigerclaw", "goblin-portrait-v1.png",
                "codex-clipboard-9d90c5f0-fcb0-45fe-9956-c462b4710d69.jpg"),
            new MegdanHeroPresentation("gydion", "grandmother-cat-portrait-v1.png",
                "codex-clipboard-452ad7ee-c4b9-4fe2-a3a1-abf1918d984c.jpg"),
            new MegdanHeroPresentation("tali", "frog-wizard-portrait-v1.png",
                "codex-clipboard-d52c7fbd-f0a1-4f9f-b4ba-3db329d053f5.jpg")
        };

        private static readonly IReadOnlyDictionary<CardDefinitionReference, MegdanHeroPresentation>
            CardOwners = BuildCardOwners();

        public static IReadOnlyList<MegdanHeroPresentation> Entries => Declared;

        public static bool TryForContent(HeroContentId contentId, out MegdanHeroPresentation entry)
        {
            entry = contentId.IsDefined
                ? Declared.FirstOrDefault(candidate => candidate.ContentId.Equals(contentId))
                : null;
            return entry != null;
        }

        public static bool TryForHeroId(string heroIdValue, out MegdanHeroPresentation entry)
        {
            entry = Declared.FirstOrDefault(candidate =>
                string.Equals(candidate.HeroIdValue, heroIdValue, StringComparison.Ordinal));
            return entry != null;
        }

        public static bool TryForCard(CardDefinitionReference reference, out MegdanHeroPresentation entry)
        {
            return CardOwners.TryGetValue(reference, out entry);
        }

        public static string DisplayName(HeroContentId contentId, string fallback) =>
            TryForContent(contentId, out var entry) ? entry.DisplayName(fallback) : fallback;

        public static string LobbyIdentifier(string value)
        {
            if (TryForHeroId(value, out var entry) && !string.IsNullOrWhiteSpace(entry.DisplayNameOverride))
            {
                return entry.DisplayNameOverride.ToUpperInvariant();
            }

            return (value ?? string.Empty).ToUpperInvariant();
        }

        private static IReadOnlyDictionary<CardDefinitionReference, MegdanHeroPresentation>
            BuildCardOwners()
        {
            var owners = new Dictionary<CardDefinitionReference, MegdanHeroPresentation>();
            AddCards(owners, "brogan", BroganCardContent.DealtDefinitions);
            AddCards(owners, "misa", MisaCardContent.DealtDefinitions);
            AddCards(owners, "rowenna", RowennaCardContent.DealtDefinitions);
            AddCards(owners, "tigerclaw", TigerclawCardContent.DealtDefinitions);
            AddCards(owners, "gydion", GydionCardContent.DealtDefinitions);
            AddCards(owners, "tali", TaliCardContent.DealtDefinitions);
            return owners;
        }

        private static void AddCards(
            IDictionary<CardDefinitionReference, MegdanHeroPresentation> owners,
            string contentId,
            IEnumerable<CardDefinition> definitions)
        {
            var owner = Declared.Single(entry => entry.ContentId.Equals(new HeroContentId(contentId)));
            foreach (var definition in definitions)
            {
                owners.Add(definition.Reference, owner);
            }
        }
    }
}
