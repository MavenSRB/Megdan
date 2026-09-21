using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// What a live Active effect looks like on the board: which hero pieces wear the tray's Active
    /// violet, and which spaces its disk tints in the same colour.
    ///
    /// <para><b>Milestone 8.35z63, from the user's own direction:</b> "when a hero has an
    /// active effect, we need to show it. Same purple color we use in hero tray to show active cards
    /// should higlight the effect target. For example. If tigerclaw has effect of his silver he should
    /// have pruple higlight. If it is an active effect in radius, we need to paint the hexes in that
    /// radius that same color."</para>
    ///
    /// <para><b>It widens no projection and reads no rules state.</b> Every input is something the
    /// read model already carries and every seat already holds:
    /// <see cref="IMatchReadModel.ActiveEffectSourceCardInstanceIds"/> is the card-level live-effect
    /// fact the trays have drawn <c>ACTIVE</c> from since milestone 8.5b, <see cref="CardInstance"/>
    /// names its owner, and the definition the owner's card carries names, in content, every Active
    /// effect that card can create. Nothing here consults <see cref="ActiveEffectInstance"/>: an
    /// instance is authority-side state, and a member of it would have to reach a networked seat over
    /// the wire, which this milestone is forbidden to touch and does not need to.</para>
    ///
    /// <para><b>Every shipped scope is anchored on the controller, which is why the derivation
    /// closes.</b> All thirty-eight <see cref="ActiveEffectDefinition"/>s this build registers - the
    /// ten <c>*ActiveEffectContent</c> families and the four synthetic ones - declare either
    /// <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> or one of the four moving-disk
    /// scopes, all of which p.17 draws around the controller's own current space. Not one declares
    /// <see cref="ActiveEffectAffectedScopeKind.Hero"/>,
    /// <see cref="ActiveEffectAffectedScopeKind.ControllerTeam"/> or
    /// <see cref="ActiveEffectAffectedScopeKind.Global"/>. That is asserted rather than assumed:
    /// <c>ActiveEffectVisibilityTests</c> walks this build's own capability catalog and fails the
    /// moment content ships a scope this layer cannot place, so the gap becomes a red test rather
    /// than a hero silently rendering nothing. <see cref="RenderedScopes"/> is that list.</para>
    ///
    /// <para><b>The disk is where it is now, not where it was created.</b> p.17: "an Active effect's
    /// Radius moves with its hero." The anchor is read fresh from
    /// <see cref="IMatchReadModel.Heroes"/> on every derivation, so the aura walks with its carrier
    /// and a restore, a replay or an Undo redraws it with no state of this type's own. A controller
    /// who is off the board anchors no disk and contributes nothing, which is the reading
    /// <see cref="RuleModificationQuery"/> already takes of the same situation.</para>
    ///
    /// <para><b>Deliberately Unity-free.</b> This file is compiled by
    /// <c>Tools/DotnetTests</c> as well as by the Presentation assembly, which is what lets the whole
    /// derivation - including the exact hex set of every disk - be proven offline in the ordinary
    /// harness rather than only under a Unity launch. A <c>UnityEngine</c> reference added here would
    /// break that build, and that is the point of it.</para>
    /// </summary>
    public static class ActiveEffectVisibility
    {
        /// <summary>
        /// The affected scopes this layer knows how to place on a board, and therefore the complete
        /// set a shipped definition may declare.
        ///
        /// <para><b>Milestone 8.71h added <see cref="ActiveEffectAffectedScopeKind.Hero"/>, and the
        /// paragraph this replaces predicted the day exactly.</b> It read "names a hero the DEFINITION
        /// does not know - <see cref="ActiveEffectInstance.AffectedHeroId"/> carries it, and no
        /// projection does - so a guess here would be presentation inventing who an effect is on", and
        /// Hanu the Trickster's three Blue journeys are the first content to declare it: "Swap with an
        /// enemy hero in radius. This turn: That hero is immune. End of turn: Swap with that hero
        /// regardless of radius and immunity." That reading still stands and nothing below guesses the
        /// affected hero. What this scope draws is the CONTROLLER's own ring - the hero whose card is
        /// standing the effect - because that is the one participant every seat can name from the
        /// facts it already holds, and because the alternative was a live effect that rendered
        /// NOTHING on every seat, which is the exact silent failure this list exists to refuse.</para>
        ///
        /// <para>It draws no disk, and that is the content rather than a limitation: "regardless of
        /// radius" is stated structurally by declaring none, so there is no circle to paint. A seat
        /// that wants to see WHO is immune reads the tray, and putting
        /// <see cref="ActiveEffectInstance.AffectedHeroId"/> on the wire so the ring could sit on the
        /// affected piece is a wire revision a later milestone may decide to spend - milestone 8.71h
        /// deliberately did not, because it is a dealing and moves neither the schema nor the
        /// wire.</para>
        ///
        /// <para>The two still-absent members are absent because no shipped definition declares them:
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerTeam"/> and
        /// <see cref="ActiveEffectAffectedScopeKind.Global"/> are derivable but would be speculative
        /// code for a sentence no card prints; the fixture turns red the day one does.</para>
        /// </summary>
        public static readonly IReadOnlyList<ActiveEffectAffectedScopeKind> RenderedScopes =
            Array.AsReadOnly(new[]
            {
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectAffectedScopeKind.Hero,
                ActiveEffectAffectedScopeKind.UnitsInRadius,
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectAffectedScopeKind.MinionsInRadius,
                ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius,
                ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius
            });

        /// <summary>
        /// The board treatment every live Active effect this reader can see adds up to, against the
        /// capability catalog the caller was composed with.
        ///
        /// <para><b>The catalog is TAKEN, never asked for, and that is a rule rather than a
        /// preference.</b> <c>EffectSeamConformanceTests.NoProductionCompositionSite_NamesACapability
        /// CatalogItWasNotGiven</c> forbids a production type naming
        /// <c>MatchContentCatalogs.ForThisBuild</c>: only the catalog itself and
        /// <c>MatchConfiguration</c> may declare what this build registers, so that a composed match
        /// cannot end up reading semantics some other composition never agreed to. The first draft of
        /// this type conjured its own and cached it; the offline harness caught it the moment this
        /// file joined the audited assembly. A null catalog therefore draws nothing rather than
        /// reaching for one.</para>
        /// </summary>
        public static ActiveEffectVisibilityModel Derive(
            IMatchReadModel projection,
            EffectCapabilityCatalog capabilities)
        {
            var sources = projection?.ActiveEffectSourceCardInstanceIds;
            if (sources == null || sources.Count == 0)
            {
                return ActiveEffectVisibilityModel.Empty;
            }

            return Derive(
                projection.ActiveEffectSourceCardInstanceIds,
                projection.Cards,
                projection.Definitions,
                projection.Heroes,
                projection.HeroProgression,
                capabilities);
        }

        /// <summary>
        /// The derivation itself, over the six facts it actually uses. Stated this way rather than
        /// only over <see cref="IMatchReadModel"/> so a fixture can put one card, one hero and one
        /// catalog in front of it without standing up a whole match.
        /// </summary>
        public static ActiveEffectVisibilityModel Derive(
            IReadOnlyList<CardInstanceId> activeEffectSourceCardInstanceIds,
            IReadOnlyList<CardInstance> cards,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<HeroOccupancy> heroes,
            IReadOnlyList<HeroProgressionProjection> heroProgression,
            EffectCapabilityCatalog capabilities)
        {
            if (activeEffectSourceCardInstanceIds == null || activeEffectSourceCardInstanceIds.Count == 0 ||
                cards == null || definitions == null || heroes == null || capabilities == null)
            {
                return ActiveEffectVisibilityModel.Empty;
            }

            var markedHeroes = new List<HeroId>();
            var auras = new List<ActiveEffectAuraViewModel>();
            var markedSet = new HashSet<HeroId>();
            var auraKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var cardInstanceId in activeEffectSourceCardInstanceIds)
            {
                // A seat that may not name this card simply draws nothing for it. That is rulebook
                // p.4's open-information boundary doing its job, not a hole: a card whose owner's hand
                // is private to another seat is one this reader was never shown.
                var card = cards.FirstOrDefault(value => value != null && value.Id.Equals(cardInstanceId));
                if (card == null)
                {
                    continue;
                }

                var controller = card.OwnerId;
                var occupancy = heroes.FirstOrDefault(value => value.HeroId.Equals(controller));
                if (!occupancy.HeroId.IsDefined)
                {
                    // Off the board: p.17's disk has no anchor and p.16's supply has no hero. The same
                    // reading RuleModificationQuery takes - the effect reaches nobody.
                    continue;
                }

                var definition = definitions.FirstOrDefault(value =>
                    value != null && value.Id.Equals(card.DefinitionReference.DefinitionId));
                if (definition == null)
                {
                    continue;
                }

                // Milestone 8.61h. The projection carries a COUNT of Radius items; what each one adds
                // is p.16's +1 or the multiple Min's "This round: Double your item bonuses" states,
                // read from the same live-effect facts this loop is already walking. Composed here so
                // the disk a seat draws and the disk the authority measures move together.
                var radiusItemBonus = RadiusItemBonus(heroProgression, controller) *
                    ItemBonusPerItem(
                        controller, activeEffectSourceCardInstanceIds, cards, definitions,
                        capabilities);
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action == null)
                    {
                        continue;
                    }

                    foreach (var declared in DeclaredActiveEffects(action, capabilities))
                    {
                        Place(
                            declared,
                            action.Id,
                            card.Id,
                            controller,
                            occupancy.Coordinate,
                            radiusItemBonus,
                            markedHeroes,
                            markedSet,
                            auras,
                            auraKeys);
                    }
                }
            }

            if (markedHeroes.Count == 0 && auras.Count == 0)
            {
                return ActiveEffectVisibilityModel.Empty;
            }

            return new ActiveEffectVisibilityModel(markedHeroes, auras);
        }

        /// <summary>
        /// Milestone 8.61h. What one of <paramref name="heroId"/>'s rulebook p.16 items adds to a
        /// matching icon right now, as a seat can read it - <c>ItemBonusQuery.BonusPerItem</c>, or the
        /// multiple Min the Dragonmonk's "This round: Double your item bonuses" states while it
        /// stands.
        ///
        /// <para><b>It widens no projection and reaches no authority state, for the reason this whole
        /// type does not.</b> <see cref="IMatchReadModel.ActiveEffectSourceCardInstanceIds"/> is the
        /// card-level live-effect fact every seat already holds, a <see cref="CardInstance"/> names its
        /// owner, and the definition that card carries names, in content, every Active effect it can
        /// create - which is exactly the derivation <see cref="Derive"/> already performs for the
        /// disks. The authority's own reading is <c>ItemBonusMultiplierQuery</c>'s and is untouched;
        /// this is the same declaration read from the seat's side, so the badge and the Attack total
        /// cannot disagree about whether the doubling is up.</para>
        ///
        /// <para><b>A composition naming no catalog reads rulebook p.16 unmodified</b>, exactly as one
        /// draws no aura - <c>EffectSeamConformanceTests</c> forbids a production type reaching for
        /// <c>MatchContentCatalogs.ForThisBuild</c> on its own, so a null catalog is the honest "this
        /// reader was told nothing" rather than a guess.</para>
        /// </summary>
        public static int ItemBonusPerItem(
            HeroId heroId,
            IReadOnlyList<CardInstanceId> activeEffectSourceCardInstanceIds,
            IReadOnlyList<CardInstance> cards,
            IReadOnlyList<CardDefinition> definitions,
            EffectCapabilityCatalog capabilities)
        {
            var largest = ItemBonusMultiplierQuery.PrintedMultiplier;
            if (!heroId.IsDefined || activeEffectSourceCardInstanceIds == null ||
                cards == null || definitions == null || capabilities == null)
            {
                return ItemBonusQuery.BonusPerItem * largest;
            }

            foreach (var cardInstanceId in activeEffectSourceCardInstanceIds)
            {
                var card = cards.FirstOrDefault(value =>
                    value != null && value.Id.Equals(cardInstanceId));
                if (card == null || !card.OwnerId.Equals(heroId))
                {
                    continue;
                }

                var definition = definitions.FirstOrDefault(value =>
                    value != null && value.Id.Equals(card.DefinitionReference.DefinitionId));
                if (definition == null)
                {
                    continue;
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    foreach (var declared in DeclaredActiveEffects(action, capabilities))
                    {
                        // "Your item bonuses" is a sentence about the hero whose card it is, so only
                        // the controller-scoped declaration is read - the same structural narrowing
                        // the authority's own query applies.
                        if (declared.AffectedScope != ActiveEffectAffectedScopeKind.ControllerHero)
                        {
                            continue;
                        }

                        foreach (var modification in declared.Modifications)
                        {
                            if (modification != null &&
                                modification.Kind == RuleModificationKind.ItemBonusMultiplier &&
                                modification.Amount > largest)
                            {
                                largest = modification.Amount;
                            }
                        }
                    }
                }
            }

            return ItemBonusQuery.BonusPerItem * largest;
        }

        /// <summary>The same reading over a whole projection. Milestone 8.61h.</summary>
        public static int ItemBonusPerItem(
            IMatchReadModel projection, HeroId heroId, EffectCapabilityCatalog capabilities) =>
            projection == null
                ? ItemBonusQuery.BonusPerItem * ItemBonusMultiplierQuery.PrintedMultiplier
                : ItemBonusPerItem(
                    heroId,
                    projection.ActiveEffectSourceCardInstanceIds,
                    projection.Cards,
                    projection.Definitions,
                    capabilities);

        /// <summary>
        /// Every Active effect the two printed bodies of one action can create, in declaration order
        /// and each named once.
        ///
        /// <para>An action carries at most two effect roots - <see cref="ActionDefinition.EffectRoot"/>
        /// and <see cref="ActionDefinition.SecondCardTextRoot"/>, which is the whole of what p.14 gives
        /// one card - and a root's nodes are a flat list, so no graph walk is involved. A capability
        /// that creates an Active effect declares exactly one, by
        /// <see cref="ActiveEffectCreationHandler"/>'s own rule ("one registration per definition,
        /// never one with a mode selector"), so a node names its effect precisely rather than a family
        /// of them.</para>
        /// </summary>
        public static IEnumerable<ActiveEffectDefinition> DeclaredActiveEffects(
            ActionDefinition action,
            EffectCapabilityCatalog capabilities)
        {
            if (action == null || capabilities == null)
            {
                yield break;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in new[] { action.EffectRoot, action.SecondCardTextRoot })
            {
                if (root == null)
                {
                    continue;
                }

                foreach (var node in root.Nodes)
                {
                    if (node == null || !node.Capability.IsDefined ||
                        !capabilities.TryLoad(node.Capability, out var handler) || handler == null)
                    {
                        continue;
                    }

                    foreach (var declared in handler.ActiveEffectDefinitions)
                    {
                        if (declared != null && seen.Add(declared.DefinitionId))
                        {
                            yield return declared;
                        }
                    }
                }
            }
        }

        private static void Place(
            ActiveEffectDefinition declared,
            ActionDefinitionId actionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId controller,
            HexCoordinate anchor,
            int radiusItemBonus,
            List<HeroId> markedHeroes,
            HashSet<HeroId> markedSet,
            List<ActiveEffectAuraViewModel> auras,
            HashSet<string> auraKeys)
        {
            if (MarksItsController(declared.AffectedScope) && markedSet.Add(controller))
            {
                markedHeroes.Add(controller);
            }

            if (!DrawsADisk(declared))
            {
                return;
            }

            var total = ResolveRadius(declared.Radius, radiusItemBonus);

            // One disk per carrier per radius. Two definitions of the same size on the same hero draw
            // the same circle, and drawing it twice would say nothing a player can see.
            if (!auraKeys.Add(controller.Value + "/" + total))
            {
                return;
            }

            auras.Add(new ActiveEffectAuraViewModel(
                controller,
                anchor,
                total,
                RadiusQuery.Spaces(
                    anchor,
                    RadiusTotal(declared.Radius, sourceCardInstanceId, actionDefinitionId, total))));
        }

        /// <summary>
        /// Whether this scope puts its rules on the hero the disk is drawn around - which is what the
        /// piece's own purple ring says.
        ///
        /// <para>The four pure disk scopes are deliberately absent. p.19 keeps self and another
        /// distinct, and <see cref="ActiveEffectAffectedScopeKind.UnitsInRadius"/> and its two
        /// narrowings exclude the controller by construction, so ringing Wasp's own token for Magnetic
        /// Dagger would say the pin is on Wasp. The disk still paints his space, because a disk
        /// contains its centre.</para>
        /// </summary>
        /// <remarks>
        /// <b>Milestone 8.71h.</b> <see cref="ActiveEffectAffectedScopeKind.Hero"/> is the one member
        /// here whose rules are NOT on the hero this marks, and the ring it draws says the weaker of
        /// the two things a ring can say: "this hero's card is standing a live effect", rather than
        /// "these rules are on this hero". It is the strongest statement a seat can make from facts it
        /// holds - the affected hero lives on <see cref="ActiveEffectInstance.AffectedHeroId"/> and
        /// reaches no projection - and the alternative was a live effect drawing nothing at all. See
        /// <see cref="RenderedScopes"/>' own remarks for the wire revision that would move the ring
        /// onto the affected piece.
        /// </remarks>
        public static bool MarksItsController(ActiveEffectAffectedScopeKind scope) =>
            scope == ActiveEffectAffectedScopeKind.ControllerHero ||
            scope == ActiveEffectAffectedScopeKind.Hero ||
            scope == ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius ||
            scope == ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius;

        /// <summary>
        /// Whether this definition paints a disk of spaces around its controller - and it is asked of
        /// the whole DEFINITION, not of its scope alone.
        ///
        /// <para><b>The second arm is the one the first sweep of this milestone got wrong, and Wasp's
        /// Static Barrier is why.</b> Its affected scope is
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> - the rules it carries are hers -
        /// and yet the card reads "spaces in radius count as obstacles", which is a disk of SPACES and
        /// exactly the thing the user asked to see painted. The radius reaches those spaces through
        /// <see cref="ActiveEffectDefinition.ObstacleOverlays"/> rather than through the affected-unit
        /// scope, so a scope-only reading drew her barrier as a ring around Wasp and left the barrier
        /// itself invisible.</para>
        ///
        /// <para>This is not a presentation judgement either: <c>ActiveEffectDefinition.ValidateDisk</c>
        /// already states the same union in Core - <c>needsADisk = scopeDrawsADisk ||
        /// declaredOverlayCount > 0</c> - and refuses a definition that needs a disk and declares no
        /// radius. What is drawn here is what the rules already say has a disk.</para>
        /// </summary>
        public static bool DrawsADisk(ActiveEffectDefinition declared) =>
            declared != null && declared.Radius.IsDefined &&
            (DrawsADisk(declared.AffectedScope) || declared.ObstacleOverlays.Count > 0);

        /// <summary>Whether this scope's own membership is decided by a disk around its controller.</summary>
        public static bool DrawsADisk(ActiveEffectAffectedScopeKind scope) =>
            scope == ActiveEffectAffectedScopeKind.UnitsInRadius ||
            scope == ActiveEffectAffectedScopeKind.HeroesInRadius ||
            scope == ActiveEffectAffectedScopeKind.MinionsInRadius ||
            scope == ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius ||
            scope == ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius;

        /// <summary>
        /// The disk's number as the rules resolve it, reproduced from the read model.
        ///
        /// <para><see cref="DeclaredRadius.Resolve"/> is the authority's own statement of this and is
        /// not reachable here, because it composes p.16's items through <c>HeroItemState</c> and no
        /// projection carries one. What every projection does carry is
        /// <see cref="HeroProgressionProjection.ItemBonuses"/> - the per-channel count of items a hero
        /// holds - and p.16's rule is exactly <c>ItemBonusQuery.BonusPerItem</c> (+1) per matching
        /// item, applied to a PRINTED icon and never to a number the sentence writes down. So the two
        /// arms here are the two arms there, and the same rule decides both.</para>
        ///
        /// <para>Milestone 8.61h: <paramref name="radiusItemBonus"/> is the POINTS those items add
        /// rather than how many there are, because what one item adds is no longer always one - see
        /// <see cref="ItemBonusPerItem(HeroId, IReadOnlyList{CardInstanceId}, IReadOnlyList{CardInstance}, IReadOnlyList{CardDefinition}, EffectCapabilityCatalog)"/>.
        /// The two were the same number until a card printed "Double your item bonuses".</para>
        /// </summary>
        public static int ResolveRadius(DeclaredRadius radius, int radiusItemBonus) =>
            radius.Source == DeclaredRadiusSource.PrintedIcon
                ? radius.Value + Math.Max(0, radiusItemBonus)
                : radius.Value;

        private static int RadiusItemBonus(
            IReadOnlyList<HeroProgressionProjection> heroProgression,
            HeroId controller)
        {
            if (heroProgression == null)
            {
                return 0;
            }

            var row = heroProgression.FirstOrDefault(value =>
                value != null && value.HeroId.Equals(controller));
            if (row == null)
            {
                return 0;
            }

            return row.ItemBonuses
                .Where(entry => entry.Key == PrintedValueChannel.Radius)
                .Sum(entry => entry.Value);
        }

        /// <summary>
        /// The composed Radius total in the shape <see cref="RadiusQuery"/> insists on, with its parts
        /// still attributable: the declaration's own contribution, plus one item contribution per
        /// point p.16 adds, exactly as <c>ItemBonusQuery.Contributions</c> builds them.
        /// </summary>
        private static ValueResult RadiusTotal(
            DeclaredRadius radius,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            int total)
        {
            var declaredKind = radius.Source == DeclaredRadiusSource.PrintedIcon
                ? ValueContributionKind.PrintedIcon
                : ValueContributionKind.CardTextModifier;
            var contributions = new List<ValueContribution>
            {
                new ValueContribution(
                    declaredKind,
                    sourceCardInstanceId,
                    sourceActionDefinitionId,
                    PrintedValueChannel.Radius,
                    radius.Value)
            };
            for (var added = radius.Value; added < total; added++)
            {
                // p.16's own shape: an item contribution identifies its ITEM card and never an action
                // definition - Core refuses one that does, which is how this line was written the first
                // time and caught on its first offline run. Which item card raised the icon is the one
                // part of it no projection carries (<see cref="HeroProgressionProjection.ItemBonuses"/>
                // is a per-icon COUNT), so the contribution names the card whose disk it widens. That
                // stand-in reaches nothing: this result is handed straight to
                // <see cref="RadiusQuery.Spaces"/> for its total and is never published, compared, or
                // read by a rules query - the authority's own composition, with the real item cards, is
                // <see cref="DeclaredRadius.Resolve"/>'s and is untouched.
                contributions.Add(new ValueContribution(
                    ValueContributionKind.ItemModifier,
                    sourceCardInstanceId,
                    PrintedValueChannel.Radius,
                    ItemBonusQuery.BonusPerItem));
            }

            return new ValueResult(PrintedValueChannel.Radius, contributions);
        }
    }

    /// <summary>
    /// One reading of every live Active effect a viewer can see, as two sets a board can draw.
    ///
    /// <para>It carries no colour, no material and no geometry. What violet means is
    /// <c>MatchHudTheme.ActiveEffectAccent</c>'s to say - the one token milestone 8.5b minted for the
    /// tray - and how a ring or a tint is built is the board view's; this is only which pieces and
    /// which spaces are in it.</para>
    /// </summary>
    public sealed class ActiveEffectVisibilityModel
    {
        public static ActiveEffectVisibilityModel Empty { get; } = new ActiveEffectVisibilityModel(
            Array.Empty<HeroId>(), Array.Empty<ActiveEffectAuraViewModel>());

        public ActiveEffectVisibilityModel(
            IEnumerable<HeroId> markedHeroes,
            IEnumerable<ActiveEffectAuraViewModel> auras)
        {
            MarkedHeroes = Array.AsReadOnly((markedHeroes ??
                Enumerable.Empty<HeroId>()).Distinct().ToArray());
            Auras = Array.AsReadOnly((auras ?? Enumerable.Empty<ActiveEffectAuraViewModel>())
                .Where(value => value != null).ToArray());
            AuraSpaces = Array.AsReadOnly(Auras
                .SelectMany(value => value.Spaces).Distinct().ToArray());
        }

        /// <summary>
        /// The historical shape, kept for every caller and fixture that only needs the spaces: one
        /// disk around one carrier.
        /// </summary>
        public ActiveEffectVisibilityModel(
            IEnumerable<HeroId> markedHeroes,
            IEnumerable<HexCoordinate> auraSpaces)
            : this(
                markedHeroes,
                auraSpaces == null || !auraSpaces.Any()
                    ? Array.Empty<ActiveEffectAuraViewModel>()
                    : new[] { ActiveEffectAuraViewModel.Anonymous(auraSpaces) })
        {
        }

        /// <summary>
        /// Each live disk on its own, so the board can follow one while its carrier is being slid.
        /// Milestone 8.35z63.
        /// </summary>
        public IReadOnlyList<ActiveEffectAuraViewModel> Auras { get; }

        /// <summary>
        /// The hero pieces an effect is standing ON, in the order the source cards were reported. Each
        /// one wears the Active violet on its own token.
        /// </summary>
        public IReadOnlyList<HeroId> MarkedHeroes { get; }

        /// <summary>
        /// Every space inside a live disk, in <see cref="RadiusQuery.Spaces"/>' canonical axial order
        /// per disk and deduplicated across overlapping ones.
        ///
        /// <para>Unfiltered by the board on purpose, which is the contract
        /// <see cref="RadiusQuery.Spaces"/> itself states: "a caller that wants spaces filters by the
        /// board it has." The board view draws the tiles it owns and silently has none for the rest,
        /// so a disk hanging over the edge of the map needs no special case anywhere.</para>
        /// </summary>
        public IReadOnlyList<HexCoordinate> AuraSpaces { get; }

        public bool IsEmpty => MarkedHeroes.Count == 0 && AuraSpaces.Count == 0;

        /// <summary>
        /// The disk covering one space, or null - which is what the board asks when it needs to know
        /// whose motion a tinted tile should follow. First disk wins where two overlap.
        /// </summary>
        public ActiveEffectAuraViewModel AuraCovering(HexCoordinate space)
        {
            for (var index = 0; index < Auras.Count; index++)
            {
                if (Auras[index].Covers(space))
                {
                    return Auras[index];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// One live Active-effect disk: whose it is, where it is centred, and the spaces it covers.
    ///
    /// <para><b>Milestone 8.35z63, for the user's follow-up ("Does it fly with
    /// animation?").</b> The flattened space set the first pass carried was enough to PAINT a disk and
    /// not enough to MOVE one: a tinted tile had no way to know which hero's slide it belonged to. The
    /// carrier travels with the disk now, so the board can offset one disk by one piece's live
    /// displacement without knowing anything about timing.</para>
    ///
    /// <para><see cref="Center"/> and <see cref="Spaces"/> are the SETTLED, hex-derived facts and stay
    /// that way for the whole slide - the glide is a presentation offset applied to them, never a
    /// re-derivation. What the rules consider affected is the authority's, unchanged, and is not what
    /// this describes.</para>
    /// </summary>
    public sealed class ActiveEffectAuraViewModel
    {
        public ActiveEffectAuraViewModel(
            HeroId carrier,
            HexCoordinate center,
            int radius,
            IEnumerable<HexCoordinate> spaces)
        {
            Carrier = carrier;
            Center = center;
            Radius = radius;
            Spaces = Array.AsReadOnly((spaces ?? Enumerable.Empty<HexCoordinate>()).ToArray());
            covered = new HashSet<HexCoordinate>(Spaces);
        }

        private readonly HashSet<HexCoordinate> covered;

        /// <summary>
        /// A disk with no carrier and no meaningful centre, for a fixture that states spaces directly.
        /// It never glides, because nothing on the board is its anchor.
        /// </summary>
        public static ActiveEffectAuraViewModel Anonymous(IEnumerable<HexCoordinate> spaces) =>
            new ActiveEffectAuraViewModel(default, default, 0, spaces);

        /// <summary>The hero this disk is drawn around, and whose slide it follows.</summary>
        public HeroId Carrier { get; }

        /// <summary>The hex the settled disk is centred on - p.17's anchor, read fresh every rebind.</summary>
        public HexCoordinate Center { get; }

        /// <summary>The composed Radius total this disk was drawn at.</summary>
        public int Radius { get; }

        public IReadOnlyList<HexCoordinate> Spaces { get; }

        public bool Covers(HexCoordinate space) => covered.Contains(space);
    }
}
