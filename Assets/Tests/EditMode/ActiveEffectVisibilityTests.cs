using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z63. What the board says while the rules are still doing something.
    ///
    /// <para><b>These run offline, in the ordinary dotnet harness.</b> The derivation under test is
    /// deliberately Unity-free and is compiled by <c>Tools/DotnetTests</c> alongside Core and
    /// Application, so the one question that actually needs proving - <em>exactly</em> which hexes a
    /// disk covers, derived from the effect's own declared radius rather than from a hand-written
    /// list - is answered here rather than only under a Unity launch. The drawing itself (the violet
    /// ring on a piece, the violet tint on a tile, and that both read the tray's own token) is a
    /// Unity question and is proven in <c>ActiveEffectBoardTreatmentTests</c>.</para>
    /// </summary>
    public sealed class ActiveEffectVisibilityTests
    {
        private static readonly HeroId Carrier = new HeroId("carrier-hero");
        private static readonly HeroId Bystander = new HeroId("bystander-hero");
        private static readonly HexCoordinate Anchor = new HexCoordinate(3, -2);

        /// <summary>
        /// THE SWEEP, made structural. Every Active-effect definition this build registers declares a
        /// scope the board knows how to place.
        ///
        /// <para>This is the check that keeps the visual honest as content grows. A hero shipped with
        /// <see cref="ActiveEffectAffectedScopeKind.Hero"/> would name an affected hero that lives only
        /// on the live instance and reaches no projection - so it would render nothing, silently, on
        /// every seat. That is exactly the failure mode a hand-maintained list of "effects we drew"
        /// cannot catch, and this walks the catalog instead.</para>
        /// </summary>
        [Test]
        public void EveryRegisteredActiveEffectDeclaresAScopeTheBoardCanDraw()
        {
            var registered = RegisteredActiveEffects().ToArray();

            Assert.That(registered, Is.Not.Empty,
                "No Active-effect definition resolved, so this rule would be blind.");
            var unplaceable = registered
                .Where(declared => !ActiveEffectVisibility.RenderedScopes.Contains(declared.AffectedScope))
                .Select(declared => $"{declared.DefinitionId} declares {declared.AffectedScope}")
                .ToArray();

            Assert.That(unplaceable, Is.Empty,
                "An Active-effect definition declares an affected scope the board cannot place, so it " +
                "would render nothing on every seat. Either the scope joins " +
                "ActiveEffectVisibility.RenderedScopes with a derivation that reaches it from a " +
                "projection, or the content is wrong:\n" + string.Join("\n", unplaceable));
        }

        /// <summary>
        /// Every scope this build actually ships is placed as either a ring, a disk, or both - and
        /// none of them is placed as nothing.
        /// </summary>
        [Test]
        public void EveryRegisteredActiveEffectResolvesToAtLeastOneTreatment()
        {
            var silent = RegisteredActiveEffects()
                .Where(declared =>
                    !ActiveEffectVisibility.MarksItsController(declared.AffectedScope) &&
                    !ActiveEffectVisibility.DrawsADisk(declared))
                .Select(declared =>
                    $"{declared.DefinitionId} ({declared.AffectedScope}, {declared.Radius})")
                .ToArray();

            Assert.That(silent, Is.Empty,
                "An Active effect would be live on the board and draw nothing:\n" +
                string.Join("\n", silent));
        }

        /// <summary>
        /// The user's own example: "If tigerclaw has effect of his silver he should have pruple
        /// higlight." Blend Into Shadows declares
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> and no radius, so it is a ring on
        /// his piece and not one painted hex.
        /// </summary>
        [Test]
        public void TigerclawsSilverMarksHisOwnPieceAndPaintsNoHexes()
        {
            var card = CardDeclaring(TigerclawActiveEffectContent.ShadowedDefinition.DefinitionId);

            var model = Derive(card, live: true);

            Assert.That(model.MarkedHeroes, Is.EqualTo(new[] { Carrier }));
            Assert.That(model.AuraSpaces, Is.Empty);
        }

        /// <summary>
        /// Wasp's Static Barrier is BOTH, and it is the case that caught this milestone's first
        /// reading. "<b>This turn:</b> While an enemy hero is performing an action, spaces outside of
        /// radius count as obstacles" - the rules it carries belong to Wasp, so its affected scope is
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/>, but the barrier itself is a disk
        /// of SPACES reached through <see cref="ActiveEffectDefinition.ObstacleOverlays"/>. A
        /// scope-only reading ringed Wasp and painted nothing, which is precisely the "active effect in
        /// radius" the user asked to see.
        /// </summary>
        [Test]
        public void WaspsStaticBarrierRingsHerAndPaintsTheDiskItsOverlayCovers()
        {
            var declared = WaspActiveEffectContent.StaticBarrierDefinition;
            var card = CardDeclaring(declared.DefinitionId);

            Assert.That(card, Is.Not.Null, "No dealt card creates Static Barrier.");
            Assert.That(declared.AffectedScope,
                Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
            Assert.That(declared.ObstacleOverlays, Is.Not.Empty);

            var model = Derive(card, live: true);

            Assert.That(model.MarkedHeroes, Is.EqualTo(new[] { Carrier }));
            Assert.That(model.AuraSpaces, Is.EquivalentTo(ExpectedDisk(declared.Radius.Value)));
        }

        /// <summary>
        /// Milestone 8.71h. Hanu the Trickster's Unexpected Journey is the first content in this
        /// build to declare <see cref="ActiveEffectAffectedScopeKind.Hero"/>, and the dealing is where
        /// the two sweeps above went red: the scope was on neither list, so three of his faces would
        /// have stood a live effect and drawn NOTHING on every seat.
        ///
        /// <para><b>What it draws, and the boundary of the claim.</b> A ring on the CONTROLLER - the
        /// hero whose card is standing the effect - and no painted hex. The ring says "this hero's
        /// card is doing something", which is weaker than the "these rules are on this hero" every
        /// other marked scope means, and deliberately so: the affected hero lives on
        /// <c>ActiveEffectInstance.AffectedHeroId</c> and reaches no projection, so a ring on the
        /// immune enemy would be presentation inventing who an effect is on. The absence of a disk is
        /// the CONTENT rather than a limitation - "regardless of radius" is stated by declaring
        /// none.</para>
        /// </summary>
        [Test]
        public void AHeroScopedJourneyRingsItsControllerAndPaintsNoHexes()
        {
            var declared = HanuActiveEffectContent.UnexpectedJourneyDefinition;
            var card = CardDeclaring(declared.DefinitionId);

            Assert.That(card, Is.Not.Null, "No dealt card creates his Unexpected Journey.");
            Assert.That(declared.AffectedScope, Is.EqualTo(ActiveEffectAffectedScopeKind.Hero));
            Assert.That(declared.Radius.IsDefined, Is.False,
                "and it measures no disk at all, which is the 'regardless of radius' half of the " +
                "printed sentence stated structurally.");

            var model = Derive(card, live: true);

            Assert.That(model.MarkedHeroes, Is.EqualTo(new[] { Carrier }),
                "The ring sits on the hero whose CARD stands the effect, which is the only " +
                "participant a seat can name from the facts it already holds.");
            Assert.That(model.AuraSpaces, Is.Empty,
                "and nothing is painted, because the definition declares no radius to paint.");
        }

        /// <summary>
        /// NebKher's Global board partition and token-kind overlay now ring their controller as a
        /// visible source, without inventing a disk. ControllerTeam remains unrendered.
        /// </summary>
        [Test]
        public void TheWideningReachesTheHeroScopeAndNoOtherUnrenderedScope()
        {
            Assert.That(
                ActiveEffectVisibility.RenderedScopes,
                Has.Member(ActiveEffectAffectedScopeKind.Hero));
            Assert.That(
                ActiveEffectVisibility.MarksItsController(ActiveEffectAffectedScopeKind.Hero),
                Is.True);
            Assert.That(
                ActiveEffectVisibility.DrawsADisk(ActiveEffectAffectedScopeKind.Hero), Is.False,
                "It is a ring and never a disk: a Hero-scoped occurrence is about a NAMED hero " +
                "rather than about a circle, so there is no membership for a disk to decide.");

            foreach (var scope in new[]
            {
                ActiveEffectAffectedScopeKind.Global,
                ActiveEffectAffectedScopeKind.TokensOfKindWhileControllerActs
            })
            {
                Assert.That(ActiveEffectVisibility.RenderedScopes, Has.Member(scope));
                Assert.That(ActiveEffectVisibility.MarksItsController(scope), Is.True);
                Assert.That(ActiveEffectVisibility.DrawsADisk(scope), Is.False);
            }
            Assert.That(ActiveEffectVisibility.RenderedScopes,
                Has.No.Member(ActiveEffectAffectedScopeKind.ControllerTeam));
        }

        [TestCase("nebkher-crack-reality-effect")]
        [TestCase("nebkher-shift-reality-effect")]
        [TestCase("nebkher-illusions-count-as-minions-effect")]
        public void NebKherBoardWideAndIllusionEffects_MarkTheirControllerWithoutInventingADisk(
            string effectId)
        {
            var card = CardDeclaring(effectId);
            Assert.That(card, Is.Not.Null, effectId);
            var model = Derive(card, live: true);
            Assert.That(model.MarkedHeroes, Is.EqualTo(new[] { Carrier }), effectId);
            Assert.That(model.AuraSpaces, Is.Empty, effectId);
        }

        /// <summary>Nothing live means nothing drawn, on the same card and the same board.</summary>
        [Test]
        public void ACardWhoseEffectIsNotLiveCarriesNoTreatment()
        {
            var card = CardDeclaring(TigerclawActiveEffectContent.ShadowedDefinition.DefinitionId);

            var model = Derive(card, live: false);

            Assert.That(model.IsEmpty, Is.True);
            Assert.That(model.MarkedHeroes, Is.Empty);
            Assert.That(model.AuraSpaces, Is.Empty);
        }

        /// <summary>
        /// A disk covers exactly the spaces its own content declares, and the expected set is derived
        /// from <see cref="RadiusQuery.Spaces"/> against the definition's own
        /// <see cref="DeclaredRadius"/> - never hand-listed. Every shipped radius definition is put
        /// through it, so a new one joins this case by existing.
        /// </summary>
        [Test]
        public void EveryRadiusEffectPaintsExactlyTheDiskItsOwnContentDeclares()
        {
            var radiusEffects = RegisteredActiveEffects()
                .Where(declared => ActiveEffectVisibility.DrawsADisk(declared))
                .ToArray();

            Assert.That(radiusEffects, Is.Not.Empty, "No disk-scoped effect resolved.");
            foreach (var declared in radiusEffects)
            {
                var card = CardDeclaring(declared.DefinitionId);
                if (card == null)
                {
                    // A synthetic capability nothing in the dealt roster reaches. Its geometry is
                    // covered by the anchored-disk case below; there is no card to stand it on.
                    continue;
                }

                var model = Derive(card, live: true);
                var expected = ExpectedDisk(declared.Radius.Value);

                Assert.That(model.AuraSpaces, Is.EquivalentTo(expected),
                    $"{declared.DefinitionId} ({declared.Radius}) painted the wrong disk.");
            }
        }

        /// <summary>
        /// p.17: "an Active effect's Radius moves with its hero." Nothing is stored, so the same live
        /// effect draws its disk around wherever its carrier is standing at the moment the board is
        /// rebuilt.
        /// </summary>
        [Test]
        public void TheDiskIsAnchoredWhereItsCarrierStandsNow()
        {
            var declared = FirstDiskEffectWithACard();
            var card = CardDeclaring(declared.DefinitionId);
            var moved = new HexCoordinate(Anchor.Q - 4, Anchor.R + 1);

            var before = Derive(card, live: true);
            var after = Derive(card, live: true, anchor: moved);

            Assert.That(before.AuraSpaces, Is.EquivalentTo(ExpectedDisk(declared.Radius.Value)));
            Assert.That(after.AuraSpaces,
                Is.EquivalentTo(ExpectedDisk(declared.Radius.Value, moved)));
            Assert.That(after.AuraSpaces, Is.Not.EquivalentTo(before.AuraSpaces));
        }

        /// <summary>
        /// THE GLIDE'S OWN GEOMETRY, and the reason the shape milestone 8.35z63 chose settles
        /// with no snap.
        ///
        /// <para>The board draws the SETTLED destination disk and offsets it backwards by its
        /// carrier's live displacement, easing to zero as the slide ends. That reads as the disk
        /// travelling from where the piece was to where it is going only if a disk translated by one
        /// hex step IS the disk around the stepped-to hex - which is a property of the lattice, not an
        /// approximation, and is what this measures. Every radius the roster ships is put through every
        /// one of p.9's six directions and a two-step diagonal.</para>
        /// </summary>
        [Test]
        public void ADiskTranslatedByAHexStepIsExactlyTheDiskAroundTheSteppedToHex()
        {
            var radii = RegisteredActiveEffects()
                .Where(declared => ActiveEffectVisibility.DrawsADisk(declared))
                .Select(declared => declared.Radius.Value)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();

            Assert.That(radii, Is.Not.Empty, "No shipped disk radius resolved.");
            var steps = Anchor.GetNeighbors()
                .Select(neighbour => new HexCoordinate(neighbour.Q - Anchor.Q, neighbour.R - Anchor.R))
                .Concat(new[] { new HexCoordinate(2, -1), new HexCoordinate(-3, 2) })
                .ToArray();

            foreach (var radius in radii)
            {
                var settled = ExpectedDisk(radius);
                foreach (var step in steps)
                {
                    var translated = settled
                        .Select(space => new HexCoordinate(space.Q + step.Q, space.R + step.R))
                        .ToArray();
                    var destination = new HexCoordinate(Anchor.Q + step.Q, Anchor.R + step.R);

                    Assert.That(translated, Is.EquivalentTo(ExpectedDisk(radius, destination)),
                        $"A radius {radius} disk stepped by ({step.Q},{step.R}) must be the disk at " +
                        "its destination, or the glide would have to snap when the slide ends.");
                }
            }
        }

        /// <summary>
        /// Each disk names the hero it is drawn around and the hex it is settled on, which is the whole
        /// of what a glide needs: whose motion to follow, and what to return to.
        /// </summary>
        [Test]
        public void EachDiskNamesItsCarrierAndItsSettledCentre()
        {
            var declared = FirstDiskEffectWithACard();
            var card = CardDeclaring(declared.DefinitionId);

            var model = Derive(card, live: true);

            Assert.That(model.Auras, Has.Count.EqualTo(1));
            Assert.That(model.Auras[0].Carrier, Is.EqualTo(Carrier));
            Assert.That(model.Auras[0].Center, Is.EqualTo(Anchor));
            Assert.That(model.Auras[0].Radius, Is.EqualTo(declared.Radius.Value));
            Assert.That(model.Auras[0].Spaces,
                Is.EquivalentTo(ExpectedDisk(declared.Radius.Value)));

            // The flattened union every space-only reader uses is exactly the disks' own spaces.
            Assert.That(model.AuraSpaces, Is.EquivalentTo(model.Auras[0].Spaces));
            Assert.That(model.AuraCovering(Anchor), Is.SameAs(model.Auras[0]));
            Assert.That(
                model.AuraCovering(new HexCoordinate(Anchor.Q + 40, Anchor.R)), Is.Null);
        }

        /// <summary>
        /// The settled centre moves with its carrier between rebinds, so what the glide eases TOWARD
        /// is always the hero's current hex rather than the one the effect was created on.
        /// </summary>
        [Test]
        public void TheSettledCentreFollowsItsCarrierBetweenRebinds()
        {
            var declared = FirstDiskEffectWithACard();
            var card = CardDeclaring(declared.DefinitionId);
            var moved = new HexCoordinate(Anchor.Q - 4, Anchor.R + 1);

            Assert.That(Derive(card, live: true).Auras[0].Center, Is.EqualTo(Anchor));
            Assert.That(Derive(card, live: true, anchor: moved).Auras[0].Center, Is.EqualTo(moved));
        }

        /// <summary>
        /// A carrier who is not on the board anchors no disk at all - the same reading
        /// <c>RuleModificationQuery</c> takes of a controller who has left it.
        /// </summary>
        [Test]
        public void ACarrierOffTheBoardAnchorsNothing()
        {
            var declared = FirstDiskEffectWithACard();
            var card = CardDeclaring(declared.DefinitionId);

            var model = ActiveEffectVisibility.Derive(
                new[] { card.Id },
                new[] { card },
                Dealt(),
                Array.Empty<HeroOccupancy>(),
                Array.Empty<HeroProgressionProjection>(),
                Catalogs.Capabilities);

            Assert.That(model.IsEmpty, Is.True);
        }

        /// <summary>
        /// p.16: "Each item increases the value of the matching icon ... by +1", and "Items never
        /// affect values in the card text." A printed Radius icon's disk widens with its carrier's
        /// items; a radius the sentence writes down does not move.
        /// </summary>
        [Test]
        public void AnItemWidensAPrintedRadiusDiskAndNeverAWrittenOne()
        {
            var printed = DeclaredRadius.PrintedIcon(2);
            var written = DeclaredRadius.WrittenInText(2);

            Assert.That(ActiveEffectVisibility.ResolveRadius(printed, 0), Is.EqualTo(2));
            Assert.That(ActiveEffectVisibility.ResolveRadius(printed, 1), Is.EqualTo(3));
            Assert.That(ActiveEffectVisibility.ResolveRadius(written, 1), Is.EqualTo(2));
        }

        /// <summary>
        /// The item bonus reaches the painted disk itself and not only the arithmetic, through the
        /// projected per-icon count every seat already carries.
        /// </summary>
        [Test]
        public void AProjectedRadiusItemWidensThePaintedDisk()
        {
            var declared = RegisteredActiveEffects()
                .Where(value =>
                    ActiveEffectVisibility.DrawsADisk(value) &&
                    value.Radius.Source == DeclaredRadiusSource.PrintedIcon &&
                    CardDeclaring(value.DefinitionId) != null)
                .OrderBy(value => value.DefinitionId, StringComparer.Ordinal)
                .FirstOrDefault();

            Assert.That(declared, Is.Not.Null,
                "No dealt card declares a printed-icon disk, so this rule would be blind.");
            var card = CardDeclaring(declared.DefinitionId);
            var withItem = new[]
            {
                new HeroProgressionProjection(Carrier, 1, 0, false, new[]
                {
                    new KeyValuePair<PrintedValueChannel, int>(PrintedValueChannel.Radius, 1)
                })
            };

            var model = ActiveEffectVisibility.Derive(
                new[] { card.Id },
                new[] { card },
                Dealt(),
                new[] { new HeroOccupancy(Carrier, Anchor) },
                withItem,
                Catalogs.Capabilities);

            Assert.That(model.AuraSpaces,
                Is.EquivalentTo(ExpectedDisk(declared.Radius.Value + 1)));
        }

        /// <summary>A hero holding no live effect is named by neither set.</summary>
        [Test]
        public void AHeroWithNoLiveEffectIsNeitherRingedNorEnclosedByItsOwnDisk()
        {
            var card = CardDeclaring(TigerclawActiveEffectContent.ShadowedDefinition.DefinitionId);

            var model = ActiveEffectVisibility.Derive(
                new[] { card.Id },
                new[] { card },
                Dealt(),
                new[]
                {
                    new HeroOccupancy(Carrier, Anchor),
                    new HeroOccupancy(Bystander, new HexCoordinate(Anchor.Q + 1, Anchor.R))
                },
                Array.Empty<HeroProgressionProjection>(),
                Catalogs.Capabilities);

            Assert.That(model.MarkedHeroes.Contains(Bystander), Is.False);
            Assert.That(model.MarkedHeroes.Contains(Carrier), Is.True);
        }

        /// <summary>
        /// A card this reader may not name draws nothing rather than throwing. That is rulebook p.4's
        /// open-information boundary reaching presentation intact: a seat is shown the source card ids
        /// the whole table can see, and one it was not shown is simply absent from
        /// <c>IMatchReadModel.Cards</c>.
        /// </summary>
        [Test]
        public void ASourceCardThisReaderCannotNameDrawsNothing()
        {
            var model = ActiveEffectVisibility.Derive(
                new[] { new CardInstanceId("a-card-this-seat-was-never-shown") },
                Array.Empty<CardInstance>(),
                Dealt(),
                new[] { new HeroOccupancy(Carrier, Anchor) },
                Array.Empty<HeroProgressionProjection>(),
                Catalogs.Capabilities);

            Assert.That(model.IsEmpty, Is.True);
        }

        /// <summary>
        /// Every dealt card that can create an Active effect draws something when it is live. This is
        /// the roster-wide sweep the milestone owes, taken over content rather than over a list.
        /// </summary>
        [Test]
        public void EveryDealtCardThatCanCreateAnActiveEffectDrawsSomethingWhenItIsLive()
        {
            var sources = Dealt()
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    ActiveEffectVisibility.DeclaredActiveEffects(action, Catalogs.Capabilities).Any()))
                .ToArray();

            Assert.That(sources, Is.Not.Empty, "No dealt card declares an Active effect.");
            var silent = new List<string>();
            foreach (var definition in sources)
            {
                var card = CardInstance.CreateInHand(
                    new CardInstanceId("live-" + definition.Id.Value), definition.Reference, Carrier);
                var model = Derive(card, live: true);
                if (model.IsEmpty)
                {
                    silent.Add(definition.Id.Value);
                }
            }

            Assert.That(silent, Is.Empty,
                "A dealt card sourcing a live Active effect drew nothing:\n" +
                string.Join("\n", silent));
        }

        /// <summary>
        /// The sweep, printed. Not a rule - the two rules above are - but the reproducible way to read
        /// the whole table at once: every Active-effect definition this build registers, the cards that
        /// can create it, and which of the two treatments it gets.
        ///
        /// <para>Explicit, so it never joins the ordinary count. Run it with
        /// <c>--filter FullyQualifiedName~ActiveEffectSweep</c>.</para>
        /// </summary>
        [Test]
        [Explicit]
        public void ActiveEffectSweep_PrintTheTable()
        {
            var rows = RegisteredActiveEffects()
                .OrderBy(declared => declared.DefinitionId, StringComparer.Ordinal)
                .Select(declared =>
                {
                    var cards = Dealt()
                        .Where(definition => definition.ActionDefinitions.Any(action =>
                            ActiveEffectVisibility.DeclaredActiveEffects(action, Catalogs.Capabilities)
                                .Any(candidate => string.Equals(
                                    candidate.DefinitionId, declared.DefinitionId, StringComparison.Ordinal))))
                        .Select(definition => definition.Id.Value)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    var ring = ActiveEffectVisibility.MarksItsController(declared.AffectedScope);
                    var disk = ActiveEffectVisibility.DrawsADisk(declared);
                    var treatment = ring && disk ? "RING + DISK" : ring ? "RING" : disk ? "DISK" : "NONE";
                    return string.Join("\t", new[]
                    {
                        declared.DefinitionId,
                        declared.AffectedScope.ToString(),
                        declared.Radius.ToString(),
                        treatment,
                        cards.Length == 0 ? "(no dealt card)" : string.Join(" ", cards)
                    });
                })
                .ToArray();

            TestContext.Out.WriteLine("effect\tscope\tradius\ttreatment\tdealt cards");
            foreach (var row in rows)
            {
                TestContext.Out.WriteLine(row);
            }

            Assert.That(rows, Is.Not.Empty);
        }

        private static MatchContentCatalogs catalogs;

        /// <summary>
        /// This build's own trusted content. A FIXTURE may name it - <c>EffectSeamConformanceTests</c>
        /// audits production types and excludes the test namespace - and production may not: a
        /// composed match hands its own catalog down instead. See
        /// <see cref="ActiveEffectVisibility.Derive(IMatchReadModel, EffectCapabilityCatalog)"/>.
        /// </summary>
        private static MatchContentCatalogs Catalogs =>
            catalogs ?? (catalogs = MatchContentCatalogs.ForThisBuild());

        private static IEnumerable<ActiveEffectDefinition> RegisteredActiveEffects() =>
            Catalogs.Capabilities.Registered
                .Select(reference => Catalogs.Capabilities.Load(reference))
                .SelectMany(handler => handler.ActiveEffectDefinitions)
                .GroupBy(declared => declared.DefinitionId, StringComparer.Ordinal)
                .Select(group => group.First());

        private static ActiveEffectDefinition FirstDiskEffectWithACard() =>
            RegisteredActiveEffects()
                .Where(declared =>
                    ActiveEffectVisibility.DrawsADisk(declared) &&
                    CardDeclaring(declared.DefinitionId) != null)
                .OrderBy(declared => declared.DefinitionId, StringComparer.Ordinal)
                .First();

        private static ActiveEffectVisibilityModel Derive(
            CardInstance card, bool live, HexCoordinate? anchor = null) =>
            ActiveEffectVisibility.Derive(
                live ? new[] { card.Id } : Array.Empty<CardInstanceId>(),
                new[] { card },
                Dealt(),
                new[] { new HeroOccupancy(Carrier, anchor ?? Anchor) },
                Array.Empty<HeroProgressionProjection>(),
                Catalogs.Capabilities);

        /// <summary>
        /// The expected disk, derived rather than listed: the one Core statement of rulebook p.9's
        /// inclusive disk, asked with the same Radius total the derivation composed.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> ExpectedDisk(int radius, HexCoordinate? origin = null) =>
            RadiusQuery.Spaces(
                origin ?? Anchor,
                new ValueResult(
                    PrintedValueChannel.Radius,
                    new[]
                    {
                        new ValueContribution(
                            ValueContributionKind.PrintedIcon,
                            new CardInstanceId("expected"),
                            new ActionDefinitionId("expected"),
                            PrintedValueChannel.Radius,
                            radius)
                    }));

        /// <summary>
        /// One card instance owned by <see cref="Carrier"/> whose definition declares the named Active
        /// effect, or null when no dealt card does.
        /// </summary>
        private static CardInstance CardDeclaring(string activeEffectDefinitionId)
        {
            var definition = Dealt().FirstOrDefault(candidate =>
                candidate.ActionDefinitions.Any(action =>
                    ActiveEffectVisibility.DeclaredActiveEffects(action, Catalogs.Capabilities)
                        .Any(declared => string.Equals(
                            declared.DefinitionId, activeEffectDefinitionId, StringComparison.Ordinal))));
            return definition == null
                ? null
                : CardInstance.CreateInHand(
                    new CardInstanceId("instance-of-" + definition.Id.Value),
                    definition.Reference,
                    Carrier);
        }

        private static IReadOnlyList<CardDefinition> dealt;

        /// <summary>
        /// Every definition a configured match can load, as
        /// <c>VerifiedContentIdentityTests.EveryDealtDefinition</c> composes the same union: the
        /// official six-player line-up plus every staged configuration no official line-up deals.
        /// </summary>
        private static IReadOnlyList<CardDefinition> Dealt() =>
            dealt ?? (dealt =
                MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red).CardDefinitions
                    .Concat(MatchConfiguration.CreateStagedDodgerForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    .Concat(MatchConfiguration.CreateStagedMisaForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    .Concat(MatchConfiguration.CreateStagedSilverarrowForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    .Concat(MatchConfiguration.CreateStagedUrsafarForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    .Concat(MatchConfiguration.CreateStagedWhisperForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    // Milestone 8.71h. Hanu's staged configuration, because his three Blue journeys
                    // are the only content in this build that declares the Hero scope and the two
                    // sweeps above are asked of every definition a configured match can load.
                    .Concat(MatchConfiguration.CreateStagedHanuForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    .Concat(MatchConfiguration.CreateStagedNebKherForInternalTest(TeamSide.Red)
                        .CardDefinitions)
                    .GroupBy(definition => definition.Id.Value, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToArray());
    }
}
