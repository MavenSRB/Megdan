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
    /// One tracked presentation-only address from immutable card identity to a private source front
    /// and the ignored Unity resource prepared from it.
    ///
    /// <para>The source path is inventory, never runtime authority. Rules and view-model content still
    /// come from the definition reference; this entry only says which picture may illustrate that
    /// already-known identity.</para>
    /// </summary>
    public sealed class CardArtworkCatalogEntry
    {
        public CardArtworkCatalogEntry(CardDefinitionReference reference, string sourcePath)
        {
            Reference = reference;
            SourcePath = sourcePath ?? string.Empty;
        }

        public CardDefinitionReference Reference { get; }

        /// <summary>Path relative to the ignored private Tabletopia corpus.</summary>
        public string SourcePath { get; }

        /// <summary>The configured hero content identity derived from the stable definition ID.</summary>
        public string ContentSetId { get; internal set; }

        /// <summary>The ignored Resources address prepared for this definition.</summary>
        public string ResourcePath { get; internal set; }
    }

    /// <summary>
    /// The validated presentation catalog used by both local preparation and runtime lookup. It holds
    /// one entry per dealt definition of every hero this build deals - 180 across ten heroes since
    /// milestone 8.35z50. The count is stated here without being pinned here: the pin lives in
    /// `ConfiguredCardArtworkCatalogTests`, and a second copy of it would be a second place to be
    /// wrong. (This line read 144 for two dealings after it stopped being true, which is why it now
    /// says where the authority is.)
    /// </summary>
    public sealed class CardArtworkCatalog
    {
        private const int CurrentFormatVersion = 1;

        private static readonly IReadOnlyDictionary<string, string> HeroFolders =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "wasp", "Wasp" },
                { "xargatha", "Xargatha" },
                { "tigerclaw", "Tigerclaw" },
                { "arien", "Arien" },
                { "brogan", "Brogan" },
                { "sabina", "Sabina" },
                { "dodger", "Dodger" },
                { "misa", "Misa" },
                // Milestone 8.35z46. The extraction directory spells her name in camel case,
                // `SilverArrow` - that extraction's identity, not this project's (SL-10 records that
                // both printed faces spell it `Silverarrow`, one word, single capital).
                { "silverarrow", "SilverArrow" },
                // Milestone 8.35z50. His extraction directory spells the name plainly, unlike
                // Silverarrow's above. THE OMISSION OF THIS ONE LINE IS WORTH RECORDING: his dealing
                // appended all eighteen artwork rows to the catalog JSON and validated that file, and
                // every offline tier stayed green, because this map lives in PRESENTATION - which the
                // Unity-free harness does not compile at all and the one-assembly compile probe can
                // only compile, never run. The first Unity Edit Mode run failed sixteen cases on it.
                // A dealing owes BOTH halves: the rows, and the hero this map can address them to.
                { "ursafar", "Ursafar" },
                // Milestone 8.35z53. Her extraction directory spells the name plainly, as his does.
                // This line is the FOURTH consecutive dealing to be told by the comment above rather
                // than by a check: it is written here at the same moment as the eighteen rows, because
                // the rows alone are what every offline tier can see.
                { "whisper", "Whisper" },
                // Milestone 8.37, the FIFTH consecutive dealing told by the comment above rather than
                // by a check - and the last, because Tali is the twelfth and final hero of the arc.
                // Written here at the same moment as the eighteen rows, for the reason those four
                // paragraphs give: the rows alone are what every offline tier can see, and this map
                // is what turns them into a front on a card.
                { "tali", "Tali" },
                // Milestone 8.38e, the SIXTH consecutive dealing told by the comment above rather than
                // by a check - and the first after the arc Tali's line called the last of. Her
                // extraction directory spells the name plainly. Written here at the same moment as the
                // eighteen rows, for the reason those five paragraphs give: the rows alone are what
                // every offline tier can see, and this map is what turns them into a front on a card.
                { "swift", "Swift" },
                // Milestone 8.39i, the SEVENTH consecutive dealing told by the comment above rather
                // than by a check. Her extraction directory spells the name plainly, like Swift's and
                // Tali's before her. Written here at the same moment as the eighteen rows, for the
                // reason those six paragraphs give: the rows alone are what every offline tier can
                // see, and this map is what turns them into a front on a card.
                { "cutter", "Cutter" },
                // Milestone 8.40j, the EIGHTH consecutive dealing told by the comment above rather
                // than by a check. His extraction directory spells the name plainly, like Cutter's,
                // Swift's and Tali's before him. Written here at the same moment as the eighteen
                // rows, for the reason those seven paragraphs give: the rows alone are what every
                // offline tier can see, and this map is what turns them into a front on a card.
                { "bain", "Bain" },
                { "garrus", "Garrus" },
                { "widget", "Widget" },
                // Milestone 8.57g, the ELEVENTH consecutive dealing told by the comment above rather
                // than by a check. Her extraction directory spells the name plainly - which is worth
                // saying out loud for her, because her STANDEE does not: the bundled manifest carries
                // her circular faces under `Knighttt` and nothing under "Rowenna" at all, so the card
                // corpus and the token corpus disagree about her name and only one of them is this
                // map's business. Written here at the same moment as the eighteen rows, for the
                // reason those eight paragraphs give: the rows alone are what every offline tier can
                // see, and this map is what turns them into a front on a card.
                { "rowenna", "Rowenna" },
                // Milestone 8.61j, the TWELFTH consecutive dealing told by the comment above rather
                // than by a check. Her extraction directory spells the name plainly, and unlike
                // Rowenna's her STANDEE agrees with it: the bundled manifest carries her circular
                // faces under `Min` too, so for the first time since Cutter the card corpus and the
                // token corpus name a hero the same way. Written here at the same moment as the
                // eighteen rows, for the reason those paragraphs give: the rows alone are what every
                // offline tier can see, and this map is what turns them into a front on a card.
                { "min", "Min" },
                // Milestone 8.69g, the THIRTEENTH consecutive dealing told by the comment above rather
                // than by a check. His extraction directory spells the name plainly and his STANDEE
                // agrees with it - the bundled manifest carries his circular faces under `Brynn` too -
                // so for the second dealing running the card corpus and the token corpus name a hero
                // the same way. What does NOT agree is the Tabletopia card GROUP, which calls him
                // "Brynn the Tracker" where the dashboard prints "the Seeker"; the dashboard is the
                // printed authority and the display name follows it, while this map follows the
                // DIRECTORY. Written here at the same moment as the eighteen rows, for the reason
                // those paragraphs give: the rows alone are what every offline tier can see, and this
                // map is what turns them into a front on a card.
                { "brynn", "Brynn" },
                // Milestone 8.71h, the FOURTEENTH consecutive dealing told by the comment above
                // rather than by a check - and the first in three dealings where every source agrees
                // at once. His extraction directory spells the name plainly, the bundled manifest
                // carries his standee under `Hanu`, the dashboard prints "Hanu the Trickster" and the
                // Tabletopia card GROUP prints it too, where Brynn's said "the Tracker" and Min's
                // said something else again. Written here at the same moment as the eighteen rows,
                // for the reason those paragraphs give: the rows alone are what every offline tier
                // can see, and this map is what turns them into a front on a card.
                //
                // The manifest's agreement is only about the NAME: it holds a SECOND element also
                // called `Hanu` (8597495) that draws a different, ape-faced hero, so the standee this
                // dealing prepared is 8597492, chosen by crop rather than by the name - Min's MI-Q6
                // lesson, and the reason this comment does not claim the two corpora agree about the
                // PICTURE.
                { "hanu", "Hanu" },
                // Milestone 8.75b prep, the FIFTEENTH consecutive dealing told by the comment above
                // rather than by a check, and the second running where every source agrees at once:
                // her extraction directory spells the name plainly, the bundled manifest carries her
                // standee under `Ignatia` - exactly ONE element of that name, where Hanu's line above
                // had to disown a second - and the dashboard prints "Ignatia the Mad". Written here
                // at the same moment as the eighteen rows, for the reason those paragraphs give: the
                // rows alone are what every offline tier can see, and this map is what turns them
                // into a front on a card.
                //
                // This row is written by the ARTWORK half of her dealing, ahead of the dealing
                // itself, which is the first time this map has moved before its hero had a roster
                // row. Nothing here depends on registration - the catalog names DEALT definitions,
                // and `IgnatiaCardContent.DealtDefinitions` is a printed set whether or not
                // `MatchContentCatalogs` knows about it.
                { "ignatia", "Ignatia" }
            };

        private readonly IReadOnlyDictionary<CardDefinitionReference, CardArtworkCatalogEntry> byReference;

        public CardArtworkCatalog(IEnumerable<CardArtworkCatalogEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var materialized = entries.ToArray();
            var references = new Dictionary<CardDefinitionReference, CardArtworkCatalogEntry>();
            var sources = new Dictionary<string, CardArtworkCatalogEntry>(StringComparer.OrdinalIgnoreCase);
            var resources = new Dictionary<string, CardArtworkCatalogEntry>(StringComparer.Ordinal);

            foreach (var entry in materialized)
            {
                if (entry == null)
                {
                    throw new InvalidOperationException("The card artwork catalog contains a null entry.");
                }

                ValidateAndAddress(entry);
                if (references.TryGetValue(entry.Reference, out var sameReference))
                {
                    throw EntryFailure(
                        entry,
                        $"duplicates definition '{sameReference.Reference}' from source " +
                        $"'{sameReference.SourcePath}'");
                }

                if (sources.TryGetValue(entry.SourcePath, out var sameSource))
                {
                    throw EntryFailure(
                        entry,
                        $"reuses source '{entry.SourcePath}' already mapped to " +
                        $"'{sameSource.Reference}'");
                }

                if (resources.TryGetValue(entry.ResourcePath, out var sameResource))
                {
                    throw EntryFailure(
                        entry,
                        $"reuses prepared resource '{entry.ResourcePath}' already mapped to " +
                        $"'{sameResource.Reference}'");
                }

                references.Add(entry.Reference, entry);
                sources.Add(entry.SourcePath, entry);
                resources.Add(entry.ResourcePath, entry);
            }

            Entries = Array.AsReadOnly(materialized);
            byReference = references;
        }

        public IReadOnlyList<CardArtworkCatalogEntry> Entries { get; }

        public bool TryGet(CardDefinitionReference reference, out CardArtworkCatalogEntry entry) =>
            byReference.TryGetValue(reference, out entry);

        /// <summary>
        /// Proves that the catalog and the configured dealt-definition set are the same set.
        /// </summary>
        public void ValidateBijection(IEnumerable<CardDefinitionReference> configuredReferences)
        {
            if (configuredReferences == null)
            {
                throw new ArgumentNullException(nameof(configuredReferences));
            }

            var configured = configuredReferences.ToArray();
            var duplicateConfigured = configured
                .GroupBy(reference => reference)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateConfigured != null)
            {
                throw new InvalidOperationException(
                    $"The configured card set itself duplicates '{duplicateConfigured.Key}'.");
            }

            var expected = new HashSet<CardDefinitionReference>(configured);
            foreach (var entry in Entries)
            {
                if (expected.Contains(entry.Reference)) continue;

                var current = configured.FirstOrDefault(reference =>
                    reference.DefinitionId.Equals(entry.Reference.DefinitionId));
                var reason = current.IsDefined
                    ? $"uses revision '{entry.Reference.ContentRevision}' while the configured " +
                      $"definition uses '{current.ContentRevision}'"
                    : "is not a configured dealt definition of any hero this build deals";
                throw EntryFailure(entry, reason);
            }

            foreach (var reference in configured)
            {
                if (!byReference.ContainsKey(reference))
                {
                    throw new InvalidOperationException(
                        $"The card artwork catalog omits configured definition '{reference}'; " +
                        "there is no stable source path for it.");
                }
            }

            if (Entries.Count != configured.Length)
            {
                throw new InvalidOperationException(
                    $"The configured card artwork bijection expected {configured.Length} entries but " +
                    $"the catalog contains {Entries.Count}.");
            }
        }

        public static CardArtworkCatalog Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("The configured card artwork catalog is empty.");
            }

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

            if (parsed == null || parsed.version != CurrentFormatVersion || parsed.entries == null)
            {
                throw new InvalidOperationException(
                    $"The configured card artwork catalog must be format version {CurrentFormatVersion} " +
                    "and contain an entries array.");
            }

            var entries = new List<CardArtworkCatalogEntry>(parsed.entries.Length);
            foreach (var row in parsed.entries)
            {
                var definitionId = row?.definitionId ?? string.Empty;
                var revision = row?.contentRevision ?? string.Empty;
                var source = row?.sourcePath ?? string.Empty;
                try
                {
                    entries.Add(new CardArtworkCatalogEntry(
                        new CardDefinitionReference(new CardDefinitionId(definitionId), revision),
                        source));
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Card artwork entry '{definitionId}@{revision}' from source '{source}' is " +
                        $"invalid ({exception.GetType().Name}).");
                }
            }

            return new CardArtworkCatalog(entries);
        }

        private static void ValidateAndAddress(CardArtworkCatalogEntry entry)
        {
            if (!entry.Reference.IsDefined)
            {
                throw EntryFailure(entry, "has no defined card identity");
            }

            var definitionId = entry.Reference.DefinitionId.Value;
            var hero = HeroFolders.Keys.FirstOrDefault(candidate =>
                definitionId.StartsWith(candidate + "-", StringComparison.Ordinal));
            if (hero == null)
            {
                throw EntryFailure(entry, "names an unknown configured hero");
            }

            if (string.IsNullOrWhiteSpace(entry.SourcePath) ||
                Path.IsPathRooted(entry.SourcePath) ||
                entry.SourcePath.Contains("\\") ||
                entry.SourcePath.Split('/').Any(segment => segment == ".."))
            {
                throw EntryFailure(entry, "must use a normalized corpus-relative source path");
            }

            var prefix = $"active-hero-cards/{HeroFolders[hero]}/";
            if (!entry.SourcePath.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw EntryFailure(
                    entry,
                    $"does not point into configured hero '{HeroFolders[hero]}'s action-card folder");
            }

            var fileName = entry.SourcePath.Substring(prefix.Length);
            var playableFront = new Regex(
                $"^{Regex.Escape(hero)}(?:[2-9]|1[0-8])?__\\d+__front\\.png$",
                RegexOptions.CultureInvariant);
            if (!playableFront.IsMatch(fileName))
            {
                throw EntryFailure(
                    entry,
                    "is not one of that hero's eighteen playable __front.png sources " +
                    "(dashboard, back-face, Handicap, and unrelated files are excluded)");
            }

            var localDefinitionId = definitionId.Substring(hero.Length + 1);
            if (localDefinitionId.Length == 0)
            {
                throw EntryFailure(entry, "has no hero-local definition identity");
            }

            entry.ContentSetId = hero;
            entry.ResourcePath = $"Private/Cards/{hero}/{localDefinitionId}";
        }

        private static InvalidOperationException EntryFailure(
            CardArtworkCatalogEntry entry,
            string reason) =>
            new InvalidOperationException(
                $"Card artwork entry '{entry.Reference}' from source '{entry.SourcePath}' {reason}.");

        [Serializable]
        private sealed class CatalogJson
        {
            public int version;
            public CatalogEntryJson[] entries;
        }

        [Serializable]
        private sealed class CatalogEntryJson
        {
            public string definitionId;
            public string contentRevision;
            public string sourcePath;
        }
    }

    /// <summary>
    /// Loads and pins the one tracked catalog against every dealt definition of every hero this build
    /// deals - TEN heroes and 180 cards since milestone 8.35z50 dealt Ursafar the Savage.
    /// </summary>
    public static class ConfiguredCardArtworkCatalog
    {
        public const string ResourcePath = "UI/ConfiguredCardArtworkCatalog";

        private static readonly IReadOnlyList<CardDefinitionReference> Configured = Array.AsReadOnly(
            WaspCardContent.DealtDefinitions
                .Concat(XargathaCardContent.DealtDefinitions)
                .Concat(TigerclawCardContent.DealtDefinitions)
                .Concat(ArienCardContent.DealtDefinitions)
                .Concat(BroganCardContent.DealtDefinitions)
                .Concat(SabinaCardContent.DealtDefinitions)
                // Milestone 8.35z34. Dodger's eighteen, joining at the milestone that deals her -
                // which is the only thing this list has ever been a statement about. Milestone 8.35l's
                // trap is that a content-revision bump owes rows here and a green harness is
                // structurally blind to the omission, so her own content file records the same debt
                // from the other side and neither can move alone.
                .Concat(DodgerCardContent.DealtDefinitions)
                // Milestone 8.35z38. Misa's eighteen, on exactly the terms Dodger's row above states.
                // Her rows are the second proof of the same shape rather than a repetition of it: her
                // content revision was re-based in this same change, so an omission here would have
                // been a revision MISMATCH rather than an absence - the loud half of milestone 8.35l's
                // trap rather than the silent half.
                .Concat(MisaCardContent.DealtDefinitions)
                // Milestone 8.35z46. Silverarrow's eighteen, on exactly the terms Dodger's and Misa's
                // rows above state. Keep the Distance is p.20's Handicap card and is not among her
                // DealtDefinitions, exactly as Dodger's Rusty Razor and Misa's Leaf on the Wind are not
                // among theirs.
                .Concat(SilverarrowCardContent.DealtDefinitions)
                // Milestone 8.35z50. Ursafar's eighteen, on exactly the terms the three rows above
                // state. Jaws That Bite is p.20's Handicap card and is not among his DealtDefinitions,
                // exactly as Dodger's Rusty Razor, Misa's Leaf on the Wind and Silverarrow's Keep the
                // Distance are not among theirs.
                .Concat(UrsafarCardContent.DealtDefinitions)
                // Milestone 8.35z53. Whisper's eighteen, on exactly the terms the rows above state.
                // Vile Accusation is p.20's Handicap card and is not among her DealtDefinitions,
                // exactly as Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the
                // Distance and Ursafar's Jaws That Bite are not among theirs.
                .Concat(WhisperCardContent.DealtDefinitions)
                // Milestone 8.37. Tali's eighteen, on exactly the terms the rows above state. Ice
                // Shard is p.20's Handicap card and is not among her DealtDefinitions, exactly as
                // Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance,
                // Ursafar's Jaws That Bite and Whisper's Vile Accusation are not among theirs.
                .Concat(TaliCardContent.DealtDefinitions)
                // Milestone 8.38e. Swift's eighteen, on exactly the terms the rows above state. Backup
                // is p.20's Handicap card and is not among her DealtDefinitions, exactly as Dodger's
                // Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance, Ursafar's
                // Jaws That Bite, Whisper's Vile Accusation and Tali's Ice Shard are not among theirs.
                .Concat(SwiftCardContent.DealtDefinitions)
                // Milestone 8.39i. Cutter's eighteen, on exactly the terms the rows above state. Few
                // More Steps is p.20's Handicap card and is not among her DealtDefinitions, exactly as
                // Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance,
                // Ursafar's Jaws That Bite, Whisper's Vile Accusation, Tali's Ice Shard and Swift's
                // Backup are not among theirs.
                .Concat(CutterCardContent.DealtDefinitions)
                // Milestone 8.40j. Bain's eighteen, on exactly the terms the rows above state. Side
                // Quest is p.20's Handicap card and is not among his DealtDefinitions, exactly as
                // Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance,
                // Ursafar's Jaws That Bite, Whisper's Vile Accusation, Tali's Ice Shard, Swift's
                // Backup and Cutter's Few More Steps are not among theirs.
                .Concat(BainCardContent.DealtDefinitions)
                .Concat(GarrusCardContent.DealtDefinitions)
                .Concat(WidgetCardContent.DealtDefinitions)
                // Milestone 8.57g. Rowenna's eighteen, on exactly the terms the rows above state.
                // Earn the Spurs is p.20's Handicap card and is not among her DealtDefinitions,
                // exactly as Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the
                // Distance, Ursafar's Jaws That Bite, Whisper's Vile Accusation, Tali's Ice Shard,
                // Swift's Backup, Cutter's Few More Steps and Bain's Side Quest are not among theirs.
                .Concat(RowennaCardContent.DealtDefinitions)
                // Milestone 8.61j. Min's eighteen, on exactly the terms the rows above state. Chop
                // Down is p.20's Handicap card and is not among her DealtDefinitions, exactly as
                // Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance,
                // Ursafar's Jaws That Bite, Whisper's Vile Accusation, Tali's Ice Shard, Swift's
                // Backup, Cutter's Few More Steps, Bain's Side Quest and Rowenna's Earn the Spurs
                // are not among theirs.
                .Concat(MinCardContent.DealtDefinitions)
                // Milestone 8.69g. Brynn's eighteen, on exactly the terms the rows above state. Home
                // Turf is p.20's Handicap card and is not among his DealtDefinitions, exactly as
                // Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance,
                // Ursafar's Jaws That Bite, Whisper's Vile Accusation, Tali's Ice Shard, Swift's
                // Backup, Cutter's Few More Steps, Bain's Side Quest, Rowenna's Earn the Spurs and
                // Min's Chop Down are not among theirs.
                .Concat(BrynnCardContent.DealtDefinitions)
                // Milestone 8.71h. Hanu's eighteen, on exactly the terms the rows above state. Fight
                // or Flight is p.20's Handicap card and is not among his DealtDefinitions, exactly as
                // Dodger's Rusty Razor, Misa's Leaf on the Wind, Silverarrow's Keep the Distance,
                // Ursafar's Jaws That Bite, Whisper's Vile Accusation, Tali's Ice Shard, Swift's
                // Backup, Cutter's Few More Steps, Bain's Side Quest, Rowenna's Earn the Spurs, Min's
                // Chop Down and Brynn's Home Turf are not among theirs.
                .Concat(HanuCardContent.DealtDefinitions)
                // Milestone 8.75b prep. Ignatia's eighteen, on exactly the terms the rows above
                // state. Unreliable Jolt is p.20's Handicap card and is not among her
                // DealtDefinitions, exactly as Dodger's Rusty Razor, Misa's Leaf on the Wind,
                // Silverarrow's Keep the Distance, Ursafar's Jaws That Bite, Whisper's Vile
                // Accusation, Tali's Ice Shard, Swift's Backup, Cutter's Few More Steps, Bain's Side
                // Quest, Rowenna's Earn the Spurs, Min's Chop Down, Brynn's Home Turf and Hanu's
                // Fight or Flight are not among theirs.
                .Concat(IgnatiaCardContent.DealtDefinitions)
                .Select(definition => definition.Reference)
                .ToArray());

        private static readonly Lazy<CardArtworkCatalog> Loaded =
            new Lazy<CardArtworkCatalog>(LoadAndValidate);

        public static IReadOnlyList<CardDefinitionReference> ConfiguredReferences => Configured;

        public static CardArtworkCatalog Catalog => Loaded.Value;

        public static CardArtworkCatalog ParseAndValidate(string json)
        {
            var catalog = CardArtworkCatalog.Parse(json);
            catalog.ValidateBijection(Configured);
            return catalog;
        }

        private static CardArtworkCatalog LoadAndValidate()
        {
            var source = Resources.Load<TextAsset>(ResourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"The tracked configured card artwork catalog resource '{ResourcePath}' is absent.");
            }

            return ParseAndValidate(source.text);
        }
    }
}
