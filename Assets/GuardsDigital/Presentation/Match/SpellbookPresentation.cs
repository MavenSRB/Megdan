using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 9.30h. One spell's whole printed face, as every spell surface draws it - the spell
    /// choice (approved mockup A) and the SPELLBOOK drawer (mockups B and C).
    ///
    /// <para><b>A face is a DEFINITION, never a card.</b> A spell face is drawn from the definition a
    /// choice names or a projected faceup card carries, so drawing one can never be the route by which
    /// a facedown spell reaches a seat: what a seat may not see, its projection does not name, and a
    /// back (<see cref="SpellBackStackViewModel"/>) is all that is left to draw.</para>
    /// </summary>
    public sealed class SpellCardViewModel
    {
        public SpellCardViewModel(CardDefinition definition, bool isGreyed = false, string caption = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!definition.Progression.IsSpell)
            {
                throw new ArgumentException("A spell face draws a spell definition.", nameof(definition));
            }

            DefinitionReference = definition.Reference;
            DisplayName = definition.DisplayName;
            Color = definition.Progression.Color;
            Tier = definition.Progression.Tier;
            Back = SpellbookBackCount.BackOf(definition.Progression);
            Face = CurrentRoundCards.ReadFace(definition);
            IsGreyed = isGreyed;
            Caption = string.IsNullOrWhiteSpace(caption) ? DisplayName.ToUpperInvariant() : caption;
            TierLabel = Tier == CardTier.None ? "BASIC" : Tier.ToString();
            AssistiveLabel = DisplayName + " - " + MatchHudTheme.ColorName(Color) + " " +
                (Tier == CardTier.None ? "basic" : "Tier " + Tier) + " spell - " + Face.ValueLine +
                (string.IsNullOrEmpty(Face.PrintedText) ? string.Empty : " - " + Face.PrintedText) +
                (isGreyed ? " - " + Caption : string.Empty);
        }

        public CardDefinitionReference DefinitionReference { get; }
        public string DisplayName { get; }
        public CardColor Color { get; }
        public CardTier Tier { get; }

        /// <summary>The back this spell shows facedown - <see cref="SpellbookBackCount.BackOf"/>.</summary>
        public CardTier Back { get; }

        /// <summary>`BASIC`, or the printed Tier numeral.</summary>
        public string TierLabel { get; }

        /// <summary>The printed regions, read by the one face reader every card goes through.</summary>
        public TrayCardFaceViewModel Face { get; }

        /// <summary>Drawn greyed: a spell not in the book, or an option that cannot be taken now.</summary>
        public bool IsGreyed { get; }

        /// <summary>The words under the face: the spell's name, `REMOVED`, or its counted values.</summary>
        public string Caption { get; }

        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// Milestone 9.30h. One stack of facedown spells showing the same back, with how many - the other
    /// seats' view of a book (approved mockup C: four stacks, `x3`, `x7`, `x6`, `x1`).
    /// </summary>
    public sealed class SpellBackStackViewModel
    {
        public SpellBackStackViewModel(CardTier back, int count)
        {
            if (back == CardTier.None) throw new ArgumentOutOfRangeException(nameof(back), back, "A spell back is Tier I to IV.");
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), count, "A drawn stack holds a spell.");
            Back = back;
            Count = count;
            CountLabel = "x" + count.ToString(CultureInfo.InvariantCulture);
            AssistiveLabel = count.ToString(CultureInfo.InvariantCulture) +
                (count == 1 ? " facedown spell" : " facedown spells") + " with the " +
                (back == CardTier.IV ? "Tier IV (Wish)" : "Tier " + back) + " back";
        }

        public CardTier Back { get; }
        public int Count { get; }
        public string CountLabel { get; }
        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// Milestone 9.30h. One hero's spellbook as one seat may see it: the plaque's chip
    /// (approved mockup D, "SPELLBOOK 17 | 5") and the SPELLBOOK drawer it opens (mockups B and C).
    ///
    /// <para><b>Privacy comes from the projection.</b> What the seat's projection NAMES in the book is
    /// drawn as a face - which on a seat is only ever its own hero's book - and what it only COUNTS is
    /// drawn as backs, from the per-back counts every seat is handed
    /// (<see cref="SeatHandSummary.SpellbookBacks"/>). Nothing here decides who may see a spell.</para>
    /// </summary>
    public sealed class SpellbookViewModel
    {
        public SpellbookViewModel(
            HeroId heroId,
            string heroName,
            int inBookCount,
            IReadOnlyList<SpellCardViewModel> inBookFaces,
            IReadOnlyList<SpellBackStackViewModel> inBookBacks,
            IReadOnlyList<SpellCardViewModel> removedFaces)
        {
            HeroId = heroId;
            InBookCount = inBookCount;
            InBookFaces = inBookFaces;
            InBookBacks = inBookBacks;
            RemovedFaces = removedFaces;
            RemovedCount = removedFaces.Count;
            ReadsTheBook = inBookCount > 0 && inBookFaces.Count == inBookCount;
            var name = (heroName ?? heroId.Value).ToUpperInvariant();
            ChipLabel = "SPELLBOOK " + InBookCount.ToString(CultureInfo.InvariantCulture) + " | " +
                RemovedCount.ToString(CultureInfo.InvariantCulture);
            ChipAssistiveLabel = "Spellbook: " + InBookCount.ToString(CultureInfo.InvariantCulture) +
                " in the book, " + RemovedCount.ToString(CultureInfo.InvariantCulture) +
                " removed faceup. Opens the spellbook.";
            Eyebrow = "SPELLBOOK";
            // The two headers the approved mockups print: the owner's (B) and every other seat's (C).
            Title = ReadsTheBook ? name : name + "'S SPELLBOOK";
            Subtitle = ReadsTheBook
                ? "Spells currently in the book. Removed spells are faceup for everyone."
                : "Facedown - this spellbook is hidden from you.";
            InBookLabel = "IN THE BOOK - " + InBookCount.ToString(CultureInfo.InvariantCulture);
            RemovedLabel = "REMOVED, FACEUP - " + RemovedCount.ToString(CultureInfo.InvariantCulture);
        }

        public HeroId HeroId { get; }

        /// <summary>The facedown book's size: <see cref="SeatHandSummary.SpellbookCount"/>.</summary>
        public int InBookCount { get; }

        /// <summary>The faceup removed pile's size.</summary>
        public int RemovedCount { get; }

        /// <summary>Whether this seat's projection names every spell in the book (its owner's seat).</summary>
        public bool ReadsTheBook { get; }

        /// <summary>The book's faces this seat's projection names, in content order.</summary>
        public IReadOnlyList<SpellCardViewModel> InBookFaces { get; }

        /// <summary>The book's spells this seat only counts, as stacks of backs.</summary>
        public IReadOnlyList<SpellBackStackViewModel> InBookBacks { get; }

        /// <summary>The removed pile, faceup for every seat, in content order.</summary>
        public IReadOnlyList<SpellCardViewModel> RemovedFaces { get; }

        public string ChipLabel { get; }
        public string ChipAssistiveLabel { get; }
        public string Eyebrow { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string InBookLabel { get; }
        public string RemovedLabel { get; }

        /// <summary>`CLOSE` - the drawer's only control; it changes no match state.</summary>
        public string CloseLabel => "CLOSE";
    }

    /// <summary>
    /// Milestone 9.30h. Reads a hero's spellbook, and a spell choice's options, off one seat's
    /// projection - the derivations behind approved mockups A to D. Pure functions of the projection:
    /// no lifetime, no command, nothing a restore, a replay or an Undo has to rebuild.
    /// </summary>
    public static class SpellbookPresentation
    {
        /// <summary>
        /// Milestone 9.30k. The spell faces a projected action card declares it can cast, in the
        /// effect graph's printed order. This reads only typed cast arms; it never recognizes Gydion,
        /// a casting card, or printed wording by name. Removed spells are public faceup cards, so every
        /// seat can apply the same faded treatment without learning a facedown spell instance.
        /// </summary>
        public static IReadOnlyList<SpellCardViewModel> CastableSpells(
            IMatchReadModel projection,
            HeroId owner,
            CardDefinition source)
        {
            if (projection == null || source == null || !owner.IsDefined)
            {
                return Array.Empty<SpellCardViewModel>();
            }

            var result = new List<SpellCardViewModel>();
            var seen = new HashSet<CardDefinitionReference>();
            foreach (var effect in source.ActionDefinitions
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(value => value != null))
            {
                foreach (var node in effect.Nodes)
                {
                    if (!SyntheticSpellbookCapabilities.TryReadSpellArm(
                            new[] { effect }, node.Id, projection.Definitions,
                            out var spell, out var kind) ||
                        kind != SyntheticSpellbookCapabilities.SpellArmKind.Cast ||
                        !seen.Add(spell.Reference))
                    {
                        continue;
                    }

                    var removed = projection.Cards.Any(card =>
                        card.OwnerId.Equals(owner) &&
                        card.DefinitionReference.Equals(spell.Reference) &&
                        card.State.Location == CardLocation.RemovedFromSpellbook);
                    result.Add(new SpellCardViewModel(
                        spell,
                        removed,
                        removed
                            ? spell.DisplayName.ToUpperInvariant() + " - REMOVED"
                            : spell.DisplayName.ToUpperInvariant()));
                }
            }

            return Array.AsReadOnly(result.ToArray());
        }

        /// <summary>
        /// Milestone 9.34. The spell face of the projected card <paramref name="cardInstanceId"/>, or
        /// <c>null</c> when that card is not a spell (or this projection does not carry it). A surface
        /// showing a card that may be a cast spell - a spell attack's card in the spotlight or in the
        /// defender's window - asks this first, because the hero-card builder knows no spell artwork and
        /// draws a generated hero card for one.
        /// </summary>
        public static SpellCardViewModel FaceOfCard(IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (projection?.Cards == null || projection.Definitions == null || !cardInstanceId.IsDefined)
            {
                return null;
            }

            var instance = projection.Cards.FirstOrDefault(card => card != null && card.Id.Equals(cardInstanceId));
            if (instance == null)
            {
                return null;
            }

            var definition = projection.Definitions.FirstOrDefault(value =>
                value != null && value.Reference.Equals(instance.DefinitionReference));
            return definition != null && definition.Progression.IsSpell
                ? new SpellCardViewModel(definition)
                : null;
        }

        /// <summary>The words a spell multi-pick is confirmed with (the user, 2026-10-01: "then CONFIRM").</summary>
        public const string ConfirmLabel = "CONFIRM";

        /// <summary>The window's eyebrow over a spell choice (mockup A).</summary>
        public const string SpellChoiceEyebrow = "SPELL CHOICE";

        /// <summary>The window's question over a cast choice (mockup A).</summary>
        public const string CastQuestion = "Choose and cast one spell in the spellbook.";

        /// <summary>
        /// <paramref name="heroId"/>'s spellbook as this projection shows it, or null for a hero who
        /// owns no spell at all - the chip is drawn only for a hero with a book.
        /// </summary>
        public static SpellbookViewModel ForHero(IMatchReadModel projection, HeroId heroId, string heroDisplayName)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var summary = (projection.HandSummaries ?? Array.Empty<SeatHandSummary>())
                .FirstOrDefault(value => value.HeroId.Equals(heroId));
            var inBook = summary?.SpellbookCount ?? 0;
            var order = DefinitionOrder(projection);
            var owned = projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) && SpellbookQuery.IsInASpellbook(card.State.Location))
                .ToArray();
            var removed = Faces(projection, order, owned
                .Where(card => card.State.Location == CardLocation.RemovedFromSpellbook &&
                    card.State.FaceOrientation == CardFaceOrientation.FaceUp));
            if (inBook == 0 && removed.Count == 0)
            {
                return null;
            }

            var namedInBook = Faces(projection, order, owned
                .Where(card => card.State.Location == CardLocation.Spellbook));
            var inspection = (projection as IExperimentalReadModel)?.CardInspections
                .FirstOrDefault(value => value.Hero.Equals(heroId));
            if (inspection != null)
                namedInBook = Array.AsReadOnly(inspection.Spellbook
                    .Select(reference => projection.Definitions.FirstOrDefault(value => value.Reference.Equals(reference)))
                    .Where(definition => definition != null && definition.Progression.IsSpell)
                    .OrderBy(definition => order.TryGetValue(definition.Reference, out var index) ? index : int.MaxValue)
                    .Select(definition => new SpellCardViewModel(definition)).ToArray());
            var backs = (summary?.SpellbookBacks ?? Array.Empty<SpellbookBackCount>())
                .Select(back => new
                {
                    back.Back,
                    Count = back.Count - namedInBook.Count(face => face.Back == back.Back)
                })
                .Where(back => back.Count > 0)
                .Select(back => new SpellBackStackViewModel(back.Back, back.Count))
                .ToArray();
            return new SpellbookViewModel(
                heroId, ShortName(heroDisplayName), inBook, namedInBook, Array.AsReadOnly(backs), removed);
        }

        /// <summary>
        /// The spell face an effect-choice option names, or null when the option names no spell -
        /// read through <see cref="SyntheticSpellbookCapabilities.TryReadSpellArm"/> off the public effect
        /// graphs of the resolving card and of the spells themselves (a Choose may be printed on a
        /// casting card, or on a spell being cast: Find Familiar's removal, Wish's choice).
        /// </summary>
        /// <param name="taken">Whether the option can be taken now (it carries a select or deselect).</param>
        public static SpellCardViewModel OptionFace(
            IMatchReadModel projection,
            CardDefinitionId sourceDefinitionId,
            EffectChoiceCandidate candidate,
            bool taken,
            out SyntheticSpellbookCapabilities.SpellArmKind kind)
        {
            kind = SyntheticSpellbookCapabilities.SpellArmKind.None;
            if (projection == null || candidate.Kind != EffectChoiceCandidateKind.Option)
            {
                return null;
            }

            var source = projection.Definitions.FirstOrDefault(value => value.Id.Equals(sourceDefinitionId));
            var effects = (source == null ? Enumerable.Empty<CardDefinition>() : new[] { source })
                .Concat(projection.Definitions.Where(value => value.Progression.IsSpell))
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(effect => effect != null);
            if (!SyntheticSpellbookCapabilities.TryReadSpellArm(
                    effects, candidate.OptionNodeId, projection.Definitions, out var spell, out kind))
            {
                return null;
            }

            var name = spell.DisplayName.ToUpperInvariant();
            var visible = projection.Cards
                .Where(card => card.DefinitionReference.DefinitionId.Equals(spell.Id))
                .ToArray();
            // The book a cast or removal arm reads is the ACTOR's (its naming step binds the owner to
            // the actor, 9.30c): the running frame's actor where this seat holds the frame, the public
            // selected actor where it does not (a network seat, Hanu's delegated seat).
            var owner = projection.PendingSkillResolution?.ActorId ?? default;
            if (!owner.IsDefined && projection.SelectedActor.IsDefined)
            {
                owner = projection.SelectedActor.ActorId;
            }

            if (!owner.IsDefined)
            {
                owner = visible.Select(card => card.OwnerId).FirstOrDefault(id => id.IsDefined);
            }

            var isRemoved = visible.Any(card => card.OwnerId.Equals(owner) &&
                card.State.Location == CardLocation.RemovedFromSpellbook);
            if (!taken)
            {
                return new SpellCardViewModel(spell, true, isRemoved ? name + " - REMOVED" : name);
            }

            // A CAST is captioned with what the spell would count now; a removal casts nothing, so
            // its option is the spell's name alone.
            return new SpellCardViewModel(
                spell, false,
                kind == SyntheticSpellbookCapabilities.SpellArmKind.Cast && owner.IsDefined
                    ? CountedCaption(projection, spell, owner, name)
                    : name);
        }

        /// <summary>
        /// Milestone 9.30j. Whether every option <paramref name="choice"/> offers names a spell - a cast
        /// or faceup-removal arm read off the public effect graphs of the match's definitions, the reading
        /// <see cref="OptionFace"/> draws faces by - so the window draws the whole choice as spell faces.
        /// Find Familiar's "remove up to three other spell cards" is the one such choice that picks
        /// several, and it is reviewed as a whole before CONFIRM (<c>EffectChoiceCompletion</c>).
        /// </summary>
        public static bool IsSpellPick(IMatchReadModel projection, PendingEffectChoice choice)
        {
            if (projection == null || choice == null || choice.Candidates.Count == 0 ||
                choice.Candidates.Any(candidate => candidate.Kind != EffectChoiceCandidateKind.Option))
            {
                return false;
            }

            var effects = projection.Definitions
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(effect => effect != null)
                .ToArray();
            return choice.Candidates.All(candidate => SyntheticSpellbookCapabilities.TryReadSpellArm(
                effects, candidate.OptionNodeId, projection.Definitions, out _, out _));
        }

        /// <summary>
        /// Milestone 9.30j. <paramref name="spell"/>'s face under other words - a spell option in a choice
        /// that also offers a worded option, captioned with the arm's own declared words.
        /// </summary>
        public static SpellCardViewModel Recaptioned(
            IMatchReadModel projection, SpellCardViewModel spell, string caption)
        {
            var definition = projection?.Definitions.FirstOrDefault(value =>
                value.Reference.Equals(spell.DefinitionReference));
            return definition == null ? spell : new SpellCardViewModel(definition, spell.IsGreyed, caption);
        }

        /// <summary>
        /// The option nodes of the <c>Choose</c> <paramref name="chooseNode"/>, in the order the card
        /// prints them, read off the same public effect graphs <see cref="OptionFace"/> reads; empty
        /// when no graph declares it.
        /// </summary>
        public static List<EffectNodeId> PrintedOptionOrder(
            IMatchReadModel projection, CardDefinitionId sourceDefinitionId, EffectNodeId chooseNode)
        {
            if (projection == null || !chooseNode.IsDefined)
            {
                return new List<EffectNodeId>();
            }

            var source = projection.Definitions.FirstOrDefault(value => value.Id.Equals(sourceDefinitionId));
            var choose = (source == null ? Enumerable.Empty<CardDefinition>() : new[] { source })
                .Concat(projection.Definitions.Where(value => value.Progression.IsSpell))
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(effect => effect != null)
                .SelectMany(effect => effect.Nodes)
                .FirstOrDefault(node => node.Id.Equals(chooseNode) && node.Kind == EffectNodeKind.Choose);
            return choose == null ? new List<EffectNodeId>() : choose.ChildNodeIds.ToList();
        }

        /// <summary>
        /// "SUNBURST - 5 OUT: ATTACK 5, RANGE 5" (mockup A): a counting spell's caption, the values the
        /// cast will use (<see cref="SpellCastReading"/>: printed icon, the caster's items, the count over
        /// the public removed pile). A spell that counts nothing is
        /// captioned with its name alone.
        /// </summary>
        private static string CountedCaption(
            IMatchReadModel projection, CardDefinition spell, HeroId owner, string name)
        {
            // The values are the rules' reading of the cast now - printed icon, his items, the count -
            // and are only formatted here; no number is added on this side.
            var cast = SpellCastReading.Read(
                projection, spell, owner, ActiveEffectVisibility.ItemBonusPerItem(projection, owner, null));
            if (cast == null)
            {
                return name;
            }

            var parts = cast.Values
                .Select(value => MatchHudTheme.ChannelName(value.Key).ToUpperInvariant() + " " +
                    value.Value.ToString(CultureInfo.InvariantCulture))
                .ToList();
            if (cast.Coins.HasValue)
            {
                parts.Add(cast.Coins.Value.ToString(CultureInfo.InvariantCulture) +
                    (cast.Coins.Value == 1 ? " COIN" : " COINS"));
            }

            return name + " - " + cast.OtherRemoved.ToString(CultureInfo.InvariantCulture) + " OUT" +
                (parts.Count == 0 ? string.Empty : ": " + string.Join(", ", parts));
        }

        private static IReadOnlyList<SpellCardViewModel> Faces(
            IMatchReadModel projection, IReadOnlyDictionary<CardDefinitionReference, int> order, IEnumerable<CardInstance> cards) =>
            Array.AsReadOnly(cards
                .Select(card => projection.Definitions.FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference)))
                .Where(definition => definition != null && definition.Progression.IsSpell)
                .OrderBy(definition => order.TryGetValue(definition.Reference, out var index) ? index : int.MaxValue)
                .ThenBy(definition => definition.Id.Value, StringComparer.Ordinal)
                .Select(definition => new SpellCardViewModel(definition))
                .ToArray());

        /// <summary>The content order the projection lists definitions in - a book's printed order.</summary>
        private static IReadOnlyDictionary<CardDefinitionReference, int> DefinitionOrder(IMatchReadModel projection)
        {
            var order = new Dictionary<CardDefinitionReference, int>();
            for (var index = 0; index < projection.Definitions.Count; index++)
            {
                if (!order.ContainsKey(projection.Definitions[index].Reference))
                {
                    order.Add(projection.Definitions[index].Reference, index);
                }
            }

            return order;
        }

        /// <summary>"Gydion" for "Gydion the Archwizard": the mockups' plaque and drawer name.</summary>
        internal static string ShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }

            var epithet = displayName.IndexOf(" the ", StringComparison.Ordinal);
            return epithet > 0 ? displayName.Substring(0, epithet) : displayName;
        }
    }
}
