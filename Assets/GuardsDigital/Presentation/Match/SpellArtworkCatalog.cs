using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 9.30h. The spell half of the tracked card artwork catalog: one row per spell
    /// definition naming its private front, and one row per spell BACK naming the back the table sees
    /// of a facedown spell.
    ///
    /// <para><b>It lives in the same tracked JSON</b> (<c>UI/ConfiguredCardArtworkCatalog</c>, arrays
    /// <c>spells</c> and <c>spellBacks</c>) beside the action-card rows, and follows their contract:
    /// the source path is inventory, never runtime authority; the prepared resource is ignored by Git
    /// and absent from a clean checkout, where the generated face is drawn instead; and the rows are a
    /// bijection with the spell definitions this build composes, so a content-revision move fails
    /// loudly here rather than silently losing a picture (milestone 8.35l's trap). The action-card
    /// reader ignores these arrays and this reader ignores <c>entries</c>.</para>
    ///
    /// <para><b>Addresses.</b> A front is prepared to <c>Private/Spells/&lt;definition id&gt;</c> and
    /// a back to <c>Private/Spells/Backs/&lt;Tier&gt;</c> (I, II, III, IV), by
    /// <c>Tools/Prepare-SpellArtwork.ps1</c>. Every source is a verified 372x260 landscape scan under
    /// <c>cards/other-cards/</c>.</para>
    /// </summary>
    public sealed class SpellArtworkCatalog
    {
        private const int CurrentFormatVersion = 1;

        /// <summary>The ignored Resources folder every prepared spell picture is written under.</summary>
        public const string ResourceFolder = "Private/Spells";

        private static readonly Regex SpellSource = new Regex(
            @"^cards/other-cards/spells-tt(?:[2-9]|1[0-9]|2[0-2])?__\d+__(front|back)\.png$",
            RegexOptions.CultureInvariant);

        private readonly IReadOnlyDictionary<CardDefinitionReference, string> fronts;
        private readonly IReadOnlyDictionary<CardTier, string> backs;

        private SpellArtworkCatalog(
            IReadOnlyList<SpellArtworkEntry> frontEntries,
            IReadOnlyList<SpellBackArtworkEntry> backEntries)
        {
            Fronts = frontEntries;
            Backs = backEntries;
            fronts = frontEntries.ToDictionary(entry => entry.Reference, entry => entry.ResourcePath);
            backs = backEntries.ToDictionary(entry => entry.Back, entry => entry.ResourcePath);
        }

        public IReadOnlyList<SpellArtworkEntry> Fronts { get; }

        public IReadOnlyList<SpellBackArtworkEntry> Backs { get; }

        public bool TryGetFront(CardDefinitionReference reference, out string resourcePath) =>
            fronts.TryGetValue(reference, out resourcePath);

        public bool TryGetBack(CardTier back, out string resourcePath) =>
            backs.TryGetValue(back, out resourcePath);

        /// <summary>The resource a front of <paramref name="definitionId"/> is prepared to.</summary>
        public static string FrontResourcePath(CardDefinitionId definitionId) =>
            ResourceFolder + "/" + definitionId.Value;

        /// <summary>The resource the <paramref name="back"/> back is prepared to.</summary>
        public static string BackResourcePath(CardTier back) => ResourceFolder + "/Backs/" + back;

        /// <summary>
        /// Parses the tracked JSON's spell arrays and proves them against
        /// <paramref name="configuredSpells"/>: every spell exactly once at its current revision, every
        /// source a distinct verified spell scan, and the four backs I-IV exactly once each.
        /// </summary>
        public static SpellArtworkCatalog ParseAndValidate(
            string json, IEnumerable<CardDefinitionReference> configuredSpells)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("The configured card artwork catalog is empty.");
            }

            if (configuredSpells == null) throw new ArgumentNullException(nameof(configuredSpells));

            CatalogJson parsed;
            try
            {
                parsed = JsonUtility.FromJson<CatalogJson>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"The configured card artwork catalog is not valid JSON ({exception.GetType().Name}).");
            }

            if (parsed == null || parsed.version != CurrentFormatVersion ||
                parsed.spells == null || parsed.spellBacks == null)
            {
                throw new InvalidOperationException(
                    $"The configured card artwork catalog must be format version {CurrentFormatVersion} " +
                    "and contain spells and spellBacks arrays.");
            }

            var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var frontEntries = new List<SpellArtworkEntry>();
            foreach (var row in parsed.spells)
            {
                var definitionId = row?.definitionId ?? string.Empty;
                var revision = row?.contentRevision ?? string.Empty;
                var source = row?.sourcePath ?? string.Empty;
                var reference = new CardDefinitionReference(new CardDefinitionId(definitionId), revision);
                RequireSource(source, "front", $"Spell artwork entry '{definitionId}@{revision}'", sources);
                frontEntries.Add(new SpellArtworkEntry(reference, source, FrontResourcePath(reference.DefinitionId)));
            }

            var configured = configuredSpells.ToArray();
            var expected = new HashSet<CardDefinitionReference>(configured);
            var stated = new HashSet<CardDefinitionReference>();
            foreach (var entry in frontEntries)
            {
                if (!stated.Add(entry.Reference))
                {
                    throw new InvalidOperationException(
                        $"Spell artwork entry '{entry.Reference}' appears twice.");
                }

                if (!expected.Contains(entry.Reference))
                {
                    throw new InvalidOperationException(
                        $"Spell artwork entry '{entry.Reference}' is not a spell definition this build " +
                        "composes at that revision.");
                }
            }

            var missing = configured.FirstOrDefault(reference => !stated.Contains(reference));
            if (missing.IsDefined)
            {
                throw new InvalidOperationException(
                    $"The card artwork catalog omits spell '{missing}'; there is no stable source path for it.");
            }

            var backEntries = new List<SpellBackArtworkEntry>();
            foreach (var row in parsed.spellBacks)
            {
                var back = (CardTier)(row?.back ?? 0);
                var source = row?.sourcePath ?? string.Empty;
                if (back == CardTier.None || !Enum.IsDefined(typeof(CardTier), back))
                {
                    throw new InvalidOperationException(
                        $"Spell back artwork row '{row?.back}' names no back; a spell back is Tier I to IV.");
                }

                if (backEntries.Any(entry => entry.Back == back))
                {
                    throw new InvalidOperationException($"Spell back '{back}' appears twice.");
                }

                RequireSource(source, "back", $"Spell back artwork '{back}'", sources);
                backEntries.Add(new SpellBackArtworkEntry(back, source, BackResourcePath(back)));
            }

            if (backEntries.Count != 4)
            {
                throw new InvalidOperationException(
                    $"The card artwork catalog names {backEntries.Count} spell backs; the table shows four (I, II, III, IV).");
            }

            return new SpellArtworkCatalog(
                Array.AsReadOnly(frontEntries.ToArray()),
                Array.AsReadOnly(backEntries.OrderBy(entry => entry.Back).ToArray()));
        }

        private static void RequireSource(
            string source, string side, string what, HashSet<string> sources)
        {
            if (string.IsNullOrWhiteSpace(source) || Path.IsPathRooted(source) || source.Contains("\\") ||
                source.Split('/').Any(segment => segment == ".."))
            {
                throw new InvalidOperationException($"{what} must use a normalized corpus-relative source path.");
            }

            var match = SpellSource.Match(source);
            if (!match.Success || match.Groups[1].Value != side)
            {
                throw new InvalidOperationException(
                    $"{what} from source '{source}' is not one of the spell {side} scans under cards/other-cards/.");
            }

            if (!sources.Add(source))
            {
                throw new InvalidOperationException($"{what} reuses source '{source}'.");
            }
        }

        [Serializable]
        private sealed class CatalogJson
        {
            public int version;
            public SpellEntryJson[] spells;
            public SpellBackJson[] spellBacks;
        }

        [Serializable]
        private sealed class SpellEntryJson
        {
            public string definitionId;
            public string contentRevision;
            public string sourcePath;
        }

        [Serializable]
        private sealed class SpellBackJson
        {
            public int back;
            public string sourcePath;
        }
    }

    /// <summary>Milestone 9.30h. One spell front's tracked address.</summary>
    public sealed class SpellArtworkEntry
    {
        internal SpellArtworkEntry(CardDefinitionReference reference, string sourcePath, string resourcePath)
        {
            Reference = reference;
            SourcePath = sourcePath;
            ResourcePath = resourcePath;
        }

        public CardDefinitionReference Reference { get; }

        /// <summary>Path relative to the ignored private Tabletopia corpus.</summary>
        public string SourcePath { get; }

        /// <summary>The ignored Resources address prepared for this spell.</summary>
        public string ResourcePath { get; }
    }

    /// <summary>Milestone 9.30h. One spell back's tracked address.</summary>
    public sealed class SpellBackArtworkEntry
    {
        internal SpellBackArtworkEntry(CardTier back, string sourcePath, string resourcePath)
        {
            Back = back;
            SourcePath = sourcePath;
            ResourcePath = resourcePath;
        }

        public CardTier Back { get; }

        public string SourcePath { get; }

        public string ResourcePath { get; }
    }

    /// <summary>
    /// Milestone 9.30h. Loads the tracked spell rows against every spell definition this build
    /// composes, and resolves a prepared picture - or reports that the generated face is drawn
    /// instead, exactly as <see cref="ConfiguredCardArtwork"/> does for an action card.
    /// </summary>
    public static class ConfiguredSpellArtwork
    {
        /// <summary>
        /// Every spell definition this build composes - Gydion's twenty-two (9.30e). A spellbook hero
        /// joins here with his spells, as a hero's dealt fronts join <c>ConfiguredCardArtworkCatalog</c>.
        /// </summary>
        public static IReadOnlyList<CardDefinitionReference> ConfiguredReferences { get; } = Array.AsReadOnly(
            GydionCardContent.SpellDefinitions.Select(definition => definition.Reference).ToArray());

        private static readonly Lazy<SpellArtworkCatalog> Loaded =
            new Lazy<SpellArtworkCatalog>(Load);

        private static Func<string, Texture2D> textureLoaderOverride;

        public static SpellArtworkCatalog Catalog => Loaded.Value;

        /// <summary>A bounded texture provider, so a test proves both the prepared and clean paths.</summary>
        public static IDisposable UseTextureLoader(Func<string, Texture2D> textureLoader)
        {
            if (textureLoader == null) throw new ArgumentNullException(nameof(textureLoader));
            var previous = textureLoaderOverride;
            textureLoaderOverride = textureLoader;
            return new Scope(() => textureLoaderOverride = previous);
        }

        public static bool TryLoadFront(CardDefinitionReference reference, out Texture2D texture)
        {
            texture = null;
            try
            {
                return Catalog.TryGetFront(reference, out var path) && TryLoad(path, out texture);
            }
            catch (Exception)
            {
                // An invalid tracked catalog draws the generated face; the catalog test says why.
                return false;
            }
        }

        public static bool TryLoadBack(CardTier back, out Texture2D texture)
        {
            texture = null;
            try
            {
                return Catalog.TryGetBack(back, out var path) && TryLoad(path, out texture);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryLoad(string resourcePath, out Texture2D texture)
        {
            try
            {
                texture = textureLoaderOverride != null
                    ? textureLoaderOverride(resourcePath)
                    : Resources.Load<Texture2D>(resourcePath);
            }
            catch (Exception)
            {
                texture = null;
            }

            return texture != null;
        }

        private static SpellArtworkCatalog Load()
        {
            var source = Resources.Load<TextAsset>(ConfiguredCardArtworkCatalog.ResourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"The tracked configured card artwork catalog resource '{ConfiguredCardArtworkCatalog.ResourcePath}' is absent.");
            }

            return SpellArtworkCatalog.ParseAndValidate(source.text, ConfiguredReferences);
        }

        private sealed class Scope : IDisposable
        {
            private Action restore;

            public Scope(Action restore) => this.restore = restore;

            public void Dispose()
            {
                restore?.Invoke();
                restore = null;
            }
        }
    }
}
