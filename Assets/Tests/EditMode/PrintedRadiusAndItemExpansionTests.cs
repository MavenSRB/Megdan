using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.2b: rulebook p.9's printed Radius icon, the one total that composes it with p.16's
    /// items, the inclusive disk it scopes, and the card that reaches the board through both.
    ///
    /// Organised by acceptance check. The distinction the whole milestone turns on is between two
    /// numbers that look alike in a definition and are not alike at all: the printed icon, which p.16
    /// raises, and an integer written into an effect's own parameters, which it never does. Several
    /// cases below exist only to keep those two apart.
    /// </summary>
    public sealed class PrintedRadiusAndItemExpansionTests
    {
        private static readonly HeroId RedHeroId =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HeroId BlueHeroId =
            new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        private const int PrintedRadius = 2;

        // ----- Check 1: a printed Radius, and one total that composes it with items -----

        [Test]
        public void ACardStatesAPrintedRadius_AndOneTotalComposesItWithItemsThroughTheOneQuery()
        {
            var fixture = new RadiusFixture();
            var card = fixture.RadiusCard;
            var action = fixture.RadiusAction;

            var printed = fixture.RadiusTotal(items: null);
            Assert.That(printed.Channel, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(printed.Total, Is.EqualTo(PrintedRadius));
            Assert.That(printed.Contributions, Has.Count.EqualTo(1));
            Assert.That(printed.Contributions[0].Kind, Is.EqualTo(ValueContributionKind.PrintedIcon));
            Assert.That(printed.Contributions[0].SourceCardInstanceId, Is.EqualTo(card.Id));
            Assert.That(printed.Contributions[0].SourceActionDefinitionId, Is.EqualTo(action));

            // One item, +1, and the composed result is exactly what the shared bonus query produces -
            // there is no second composition anywhere.
            var items = fixture.ItemsFor(RedHeroId, PrintedValueChannel.Radius);
            var composed = fixture.RadiusTotal(items);
            Assert.That(composed.Total, Is.EqualTo(PrintedRadius + 1));
            Assert.That(
                composed.Contributions,
                Is.EqualTo(ItemBonusQuery.Apply(printed, items, RedHeroId).Contributions));

            var itemContributions = composed.Contributions
                .Where(value => value.Kind == ValueContributionKind.ItemModifier)
                .ToArray();
            Assert.That(itemContributions, Has.Length.EqualTo(1));
            Assert.That(itemContributions[0].Amount, Is.EqualTo(ItemBonusQuery.BonusPerItem));
            Assert.That(itemContributions[0].Channel, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(
                itemContributions[0].SourceCardInstanceId, Is.Not.EqualTo(card.Id),
                "An item contribution names the card that became the item, not the card being played.");

            // A second Radius item adds a second contribution of one each rather than one of two.
            var doubled = fixture.RadiusTotal(fixture.ItemsFor(
                RedHeroId, PrintedValueChannel.Radius, PrintedValueChannel.Radius));
            Assert.That(doubled.Total, Is.EqualTo(PrintedRadius + 2));
        }

        [Test]
        public void ASkillPrintsARadiusAndNothingElse_AndOnlyAPrimaryActionPrintsOne()
        {
            var fixture = new RadiusFixture();
            var definition = fixture.DefinitionOf(fixture.RadiusCard);
            var action = definition.ActionDefinitions.Single(value => value.Id.Equals(fixture.RadiusAction));

            Assert.That(action.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(action.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(action.BaseValues, Has.Count.EqualTo(1));
            Assert.That(action.BaseValues[0].Channel, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(
                action.AttackRange, Is.EqualTo(AttackRangeKind.None),
                "p.9: all actions other than Ranged primaries are non-Ranged, including those with a " +
                "Radius icon.");

            // A Skill still carries no other printed value, the Radius is a primary action's icon, and
            // a printed icon states a positive value.
            Assert.Throws<ArgumentException>(() => SkillAction(
                ActionDefinitionOrigin.PrintedPrimary,
                new PrintedValueDefinition(PrintedValueChannel.Attack, 2)));
            Assert.Throws<ArgumentException>(() => SkillAction(
                ActionDefinitionOrigin.PrintedSecondary,
                new PrintedValueDefinition(PrintedValueChannel.Radius, 2)));
            Assert.Throws<ArgumentException>(() => SkillAction(
                ActionDefinitionOrigin.PrintedPrimary,
                new PrintedValueDefinition(PrintedValueChannel.Radius, 0)));
            Assert.DoesNotThrow(() => SkillAction(
                ActionDefinitionOrigin.PrintedPrimary,
                new PrintedValueDefinition(PrintedValueChannel.Radius, 1)));
        }

        // ----- Check 2: no printed icon gains nothing, and card text is never raised -----

        [Test]
        public void ACardPrintingNoRadiusIcon_GainsNoRadiusFromAnItem()
        {
            var fixture = new RadiusFixture();
            var items = fixture.ItemsFor(RedHeroId, PrintedValueChannel.Radius);

            // An ordinary card of the same hand prints no Radius at all. p.16 raises a printed icon and
            // never invents one, so the total stays zero rather than becoming one.
            var ordinary = fixture.OrdinarySkillTotal(items);
            Assert.That(ordinary.Total, Is.Zero);
            Assert.That(ordinary.Contributions, Is.Empty);

            // And an item of another icon leaves the printed Radius exactly as printed.
            Assert.That(
                fixture.RadiusTotal(fixture.ItemsFor(RedHeroId, PrintedValueChannel.Attack)).Total,
                Is.EqualTo(PrintedRadius));
        }

        [Test]
        public void AFixedNumberInEffectText_IsNotRaisedByAnItem()
        {
            // The card's Push distance is a literal in its own effect parameters - card text by p.16's
            // reading - while its reach is the printed icon. A Radius item widens the second and leaves
            // the first alone, which is the whole distinction this milestone exists to make
            // representable.
            var withItem = new RadiusFixture(defenderDistance: PrintedRadius + 1);
            withItem.PlaceItems(RedHeroId, PrintedValueChannel.Radius, 1);
            withItem.StartRadiusSkill();
            var before = withItem.CoordinateOf(BlueHeroId);
            withItem.SelectTarget(before);

            var after = withItem.CoordinateOf(BlueHeroId);
            Assert.That(
                HexDistanceQuery.Calculate(before, after), Is.EqualTo(1),
                "The pushed unit moves the one space the card's text names, whatever the hero owns.");
        }

        // ----- Check 3: the named query derives the inclusive disk by rulebook distance -----

        [Test]
        public void TheRadiusQuery_DerivesTheInclusiveDiskFromTheTotalAndNothingElse()
        {
            var origin = new HexCoordinate(0, 0);
            var two = RadiusTotalOf(2);
            var spaces = RadiusQuery.Spaces(origin, two);

            // An inclusive disk of radius 2 is nineteen spaces, its own centre among them.
            Assert.That(spaces, Has.Count.EqualTo(19));
            Assert.That(spaces, Contains.Item(origin));
            Assert.That(
                spaces.All(space => HexDistanceQuery.Calculate(origin, space) <= 2), Is.True);
            Assert.That(
                spaces.Count(space => HexDistanceQuery.Calculate(origin, space) == 2), Is.EqualTo(12),
                "The outermost ring is included; p.9 reaches targets 'up to' the value.");
            Assert.That(RadiusQuery.Contains(origin, two, new HexCoordinate(2, 0)), Is.True);
            Assert.That(RadiusQuery.Contains(origin, two, new HexCoordinate(3, 0)), Is.False);

            // p.9's own smallest value: a Radius of 1 reaches adjacent targets only.
            Assert.That(RadiusQuery.Spaces(origin, RadiusTotalOf(1)), Has.Count.EqualTo(7));

            // The disk is derived from a Radius total and from nothing else: a Range total of the same
            // size, or a card-text integer dressed as one, is refused rather than silently accepted.
            var range = new ValueResult(PrintedValueChannel.Range, new[]
            {
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("ranged-card"),
                    new ActionDefinitionId("ranged-action"),
                    PrintedValueChannel.Range,
                    2)
            });
            Assert.Throws<ArgumentException>(() => RadiusQuery.Spaces(origin, range));
            Assert.Throws<ArgumentNullException>(() => RadiusQuery.Spaces(origin, null));
        }

        [Test]
        public void TheDisk_IsRulebookDistanceRatherThanReachability()
        {
            // p.9: "There are no 'line of sight' rules. You can target units through obstacles, as long
            // as they are in range/radius." An obstacle between the origin and a space changes neither
            // the disk nor what may be targeted inside it, which a movement-path answer could not say.
            var fixture = new RadiusFixture(
                defenderDistance: PrintedRadius,
                obstacleAt: new HexCoordinate(1, 0));

            Assert.That(
                RadiusQuery.Contains(
                    fixture.CoordinateOf(RedHeroId),
                    fixture.RadiusTotal(items: null),
                    fixture.CoordinateOf(BlueHeroId)),
                Is.True);
            Assert.That(
                fixture.TargetsInRadius(items: null).Contains(fixture.CoordinateOf(BlueHeroId)),
                Is.True,
                "An obstacle on the way blocks a route and not a Radius.");
        }

        // ----- Check 4: targeting and presence answer the same disk differently -----

        [Test]
        public void TargetingFiltersImmunity_WhilePresenceOverTheSameDiskCountsIt()
        {
            var fixture = new RadiusFixture(defenderDistance: PrintedRadius);
            var origin = fixture.CoordinateOf(RedHeroId);
            var total = fixture.RadiusTotal(items: null);
            var immune = new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForImmunity(RuleActivity.ObjectPush),
                        RuleObject.ForHero(BlueHeroId),
                        new ActiveEffectId("radius-fixture-immunity"),
                        BlueHeroId,
                        TeamSide.Blue,
                        0)
                },
                new TieBreakerState(TeamSide.Red));

            // p.18: "if a unit is immune, you cannot target that unit."
            Assert.That(
                fixture.TargetsInRadius(items: null, modifications: immune), Is.Empty);

            // p.18: "effects that check for unit presence ... will count immune units." The same disk,
            // the same unit, the opposite answer - which is the rule rather than an inconsistency.
            Assert.That(
                RadiusQuery.UnitsPresent(origin, total, EffectiveUnitKind.Hero, fixture.Board, immune)
                    .Select(unit => unit.HeroId),
                Contains.Item(BlueHeroId));

            // Without the immunity the same targeting question finds it.
            Assert.That(
                fixture.TargetsInRadius(items: null).Contains(fixture.CoordinateOf(BlueHeroId)),
                Is.True);
        }

        // ----- Check 5: the complete eligible set, deterministic and not a mutation order -----

        [Test]
        public void TheEligibleSet_IsCompleteAndDeterministicWithoutBeingAResolutionOrder()
        {
            var fixture = new RadiusFixture(defenderDistance: PrintedRadius);
            var origin = fixture.CoordinateOf(RedHeroId);
            var total = fixture.RadiusTotal(items: null);

            var everyObject = RadiusQuery.Targets(
                origin,
                total,
                includeSelf: true,
                RuleObject.ForHero(RedHeroId),
                RuleActivity.ObjectPush,
                fixture.Board,
                fixture.Participants,
                RuleModificationSet.Empty);

            // Complete: every board object standing inside the disk is in the set, derived by asking
            // the disk rather than by stopping when the caller has enough.
            var standingInside = new[] { RedHeroId, BlueHeroId }
                .Where(hero => RadiusQuery.Contains(origin, total, fixture.CoordinateOf(hero)))
                .ToArray();
            Assert.That(everyObject, Has.Count.EqualTo(standingInside.Length));

            // Deterministic: the same state answers identically, and the query mutates nothing, so an
            // effect that affects every unit in Radius may read this set without the reading itself
            // deciding what happens first.
            var again = RadiusQuery.Targets(
                origin,
                total,
                includeSelf: true,
                RuleObject.ForHero(RedHeroId),
                RuleActivity.ObjectPush,
                fixture.Board,
                fixture.Participants,
                RuleModificationSet.Empty);
            Assert.That(again, Is.EqualTo(everyObject));
            Assert.That(fixture.CoordinateOf(BlueHeroId), Is.EqualTo(fixture.DefenderOrigin));

            // p.19 keeps self and another distinct, and the caller states which it means.
            var others = RadiusQuery.Targets(
                origin,
                total,
                includeSelf: false,
                RuleObject.ForHero(RedHeroId),
                RuleActivity.ObjectPush,
                fixture.Board,
                fixture.Participants,
                RuleModificationSet.Empty);
            Assert.That(others, Has.Count.EqualTo(everyObject.Count - 1));
        }

        // ----- Check 6: the synthetic consumer, end to end, at the boundary -----

        [Test]
        public void AMatchingItem_AdmitsAnObjectExactlyOneStepBeyondThePrintedRadius()
        {
            // Just beyond the printed disk: the Skill has nothing to choose, so its mandatory selection
            // cannot apply anything and the action is refused rather than reaching further.
            var withoutItem = new RadiusFixture(defenderDistance: PrintedRadius + 1);
            Assert.That(withoutItem.TargetsInRadius(items: null), Is.Empty);
            Assert.That(withoutItem.StartRadiusSkill().Accepted, Is.False);

            // The same board with one Radius item: the defender is inside the disk, the selection
            // offers exactly that space, and answering it pushes them.
            var withItem = new RadiusFixture(defenderDistance: PrintedRadius + 1);
            withItem.PlaceItems(RedHeroId, PrintedValueChannel.Radius, 1);
            Assert.That(withItem.StartRadiusSkill().Accepted, Is.True);
            var choice = withItem.PendingChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { withItem.DefenderOrigin }));
            withItem.SelectTarget(withItem.DefenderOrigin);
            Assert.That(withItem.CoordinateOf(BlueHeroId), Is.Not.EqualTo(withItem.DefenderOrigin));

            // One step and no further: two beyond the printed value stays out of reach with the item.
            var stillOutOfReach = new RadiusFixture(defenderDistance: PrintedRadius + 2);
            stillOutOfReach.PlaceItems(RedHeroId, PrintedValueChannel.Radius, 1);
            Assert.That(stillOutOfReach.StartRadiusSkill().Accepted, Is.False);

            // A nonmatching item widens nothing.
            var wrongIcon = new RadiusFixture(defenderDistance: PrintedRadius + 1);
            wrongIcon.PlaceItems(RedHeroId, PrintedValueChannel.Movement, 1);
            Assert.That(wrongIcon.StartRadiusSkill().Accepted, Is.False);
        }

        // ----- Check 7: what this milestone actually changed, durably -----

        [Test]
        public void APausedRadiusSelection_SurvivesSnapshotRestoreWithTheSameItemWidenedDisk()
        {
            var fixture = new RadiusFixture(defenderDistance: PrintedRadius + 1);
            fixture.PlaceItems(RedHeroId, PrintedValueChannel.Radius, 1);
            Assert.That(fixture.StartRadiusSkill().Accepted, Is.True);
            Assert.That(fixture.PendingChoice, Is.Not.Null);

            var payload = AuthoritativeSnapshotSerializer.Serialize(fixture.Round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Definitions,
                new[] { fixture.Board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());

            // The paused choice is re-derived on the way back rather than carried, so the restored
            // candidate set is the item-widened one the player was offered. Without the items reaching
            // restore's re-derivation this is the assertion that fails: the disk would come back a ring
            // smaller and the payload would be refused for offering a candidate it no longer derives.
            var restoredChoice = restored.CardActions.PendingEffectChoice;
            Assert.That(restoredChoice, Is.Not.Null);
            Assert.That(
                restoredChoice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { fixture.DefenderOrigin }));
            Assert.That(
                restored.Round.HeroItems.GetItems(RedHeroId), Has.Count.EqualTo(1));
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "Restore-and-recapture reproduces the payload byte for byte.");
        }

        [Test]
        public void TheCapabilityRegistration_IsThisMilestonesOnlyCompatibilityCause()
        {
            var catalog = GuardsDigital.Core.Effects.Synthetic.SyntheticEffectCapabilities.CreateCatalog();
            Assert.That(
                catalog.Registered.Any(reference =>
                    reference.CapabilityId.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities.RadiusTarget)),
                Is.True);

            // This milestone's own two Radius cards are dealt by their own configuration alone, which is
            // why 7.2b's corpus moved for the registration alone.
            var ordinary = PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture();
            Assert.That(
                ordinary.CardDefinitions.Any(definition =>
                    definition.Id.Value.Contains("shockwave")),
                Is.False);

            // Milestone 7.2c then printed the icon on one configured card, deliberately: Red Needle's
            // text has named a Radius since 6.5a, and p.9 says such text refers to the printed value.
            // It is the only configured card that prints one, and the count is asserted rather than the
            // absence, so a second one arriving silently is a failure rather than a surprise.
            var printed = ordinary.CardDefinitions
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.BaseValues.Any(value => value.Channel == PrintedValueChannel.Radius)))
                .Select(definition => definition.Id.Value)
                .ToArray();
            Assert.That(printed, Is.EqualTo(new[] { "placeholder-red-needle" }));
        }

        // ----- Helpers -----

        private static ValueResult RadiusTotalOf(int amount) =>
            new ValueResult(PrintedValueChannel.Radius, new[]
            {
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("radius-card"),
                    new ActionDefinitionId("radius-action"),
                    PrintedValueChannel.Radius,
                    amount)
            });

        /// <summary>
        /// A Skill action built around the configured card's own effect, so these cases vary the two
        /// things they are about - the origin and the printed value - and nothing else.
        /// </summary>
        private static ActionDefinition SkillAction(
            ActionDefinitionOrigin origin,
            PrintedValueDefinition printedValue)
        {
            var effect = PlaceholderOneVsOneTestConfiguration
                .CreateForRadiusFixture(TeamSide.Red)
                .CardDefinitions
                .Single(definition => definition.Id.Value.Contains("red-shockwave"))
                .ActionDefinitions
                .Single(action => action.Kind == ActionKind.Skill)
                .EffectRoot;

            return new ActionDefinition(
                new ActionDefinitionId("fixture-skill"),
                ActionKind.Skill,
                origin,
                new[] { printedValue },
                new ActionDefinitionPolicy(
                    origin == ActionDefinitionOrigin.PrintedPrimary
                        ? CardTextApplicationPolicy.Applies
                        : CardTextApplicationPolicy.Suppressed,
                    false,
                    true),
                AttackRangeKind.None,
                default,
                effect);
        }

        /// <summary>
        /// The printed-Radius configuration on a hand-built board, with the round that owns the items
        /// and the dispatcher bound to it.
        ///
        /// Items are placed through the round's own state rather than injected, because the binding is
        /// half of what these tests are about: the disk a capability offers must be derived from the
        /// items the match records.
        /// </summary>
        private sealed class RadiusFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;
            private readonly List<CardInstance> instances;
            private readonly List<CardDefinition> definitions;

            public RadiusFixture(int defenderDistance = 1, HexCoordinate? obstacleAt = null)
            {
                configuration = PlaceholderOneVsOneTestConfiguration.CreateForRadiusFixture(TeamSide.Red);
                definitions = configuration.CardDefinitions.ToList();
                definitions.Add(RadiusItemDefinition);
                instances = configuration.CardInstances.ToList();
                DefenderOrigin = new HexCoordinate(defenderDistance, 0);
                Map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                Board = new AdjacentMoveAuthority(
                    Coordinates(6),
                    new[]
                    {
                        new HeroOccupancy(RedHeroId, new HexCoordinate(0, 0)),
                        new HeroOccupancy(BlueHeroId, DefenderOrigin)
                    },
                    obstacleAt.HasValue
                        ? new[] { new ObstacleOccupancy(new ObstacleId("radius-blocker"), obstacleAt.Value) }
                        : Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                CardActions = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), Board);
                Round = new RoundResolutionAuthority(
                    definitions,
                    instances,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    1,
                    RoundResolutionComposition.ForCardActionsOnly(CardActions));
            }

            public AdjacentMoveAuthority Board { get; }

            public BoardMapDefinition Map { get; }

            public CardActionResolutionAuthority CardActions { get; }

            public RoundResolutionAuthority Round { get; }

            public HexCoordinate DefenderOrigin { get; }

            public MatchParticipants Participants => configuration.Participants;

            public IReadOnlyList<CardDefinition> Definitions => definitions;

            public CardInstance RadiusCard => instances.Single(instance =>
                instance.DefinitionReference.DefinitionId.Value.Contains("shockwave") &&
                instance.OwnerId.Equals(RedHeroId));

            public ActionDefinitionId RadiusAction => new ActionDefinitionId("primary-radius-skill");

            public PendingEffectChoice PendingChoice => CardActions.PendingEffectChoice;

            /// <summary>A Tier III fixture card printing p.16's Radius Item icon.</summary>
            private static CardDefinition RadiusItemDefinition { get; } = ItemDefinition(
                "fixture-radius-item", PrintedValueChannel.Radius);

            private static CardDefinition ItemDefinition(string id, PrintedValueChannel icon) =>
                new CardDefinition(
                    new CardDefinitionId(id),
                    "phase-7-2b-fixture-rev-1",
                    $"TEST - {icon} Item",
                    new InitiativeValue(1),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.III, icon));

            public CardDefinition DefinitionOf(CardInstance instance) =>
                definitions.Single(definition =>
                    definition.Reference.Equals(instance.DefinitionReference));

            public HexCoordinate CoordinateOf(HeroId heroId)
            {
                Assert.That(Board.TryGetHeroCoordinate(heroId, out var coordinate), Is.True);
                return coordinate;
            }

            /// <summary>The Radius Total of the printed-Radius card, composed with the items given.</summary>
            public ValueResult RadiusTotal(HeroItemState items) =>
                RadiusValueQuery.Calculate(
                    RadiusCard.Id,
                    RadiusCard.DefinitionReference,
                    RadiusAction,
                    definitions,
                    RedHeroId,
                    items);

            /// <summary>The same total asked of an ordinary card, which prints no Radius icon.</summary>
            public ValueResult OrdinarySkillTotal(HeroItemState items)
            {
                var ordinary = instances.First(instance =>
                    instance.OwnerId.Equals(RedHeroId) &&
                    DefinitionOf(instance).ActionDefinitions.Any(action =>
                        action.Kind == ActionKind.Movement));
                var action = DefinitionOf(ordinary).ActionDefinitions
                    .First(value => value.Kind == ActionKind.Movement);
                return RadiusValueQuery.Calculate(
                    ordinary.Id, ordinary.DefinitionReference, action.Id, definitions, RedHeroId, items);
            }

            public HeroItemState ItemsFor(HeroId owner, params PrintedValueChannel[] icons) =>
                HeroItemState.Restore(
                    configuration.Participants,
                    new HeroItemRestoreState(icons.Select((icon, index) =>
                    {
                        Assert.That(
                            HeroDashboard.RulebookLayout.TryFindSlotAccepting(icon, out var slot),
                            Is.True);
                        return new HeroItem(
                            owner, new CardInstanceId($"radius-fixture-item-{index}"),
                            slot.Position, icon, index + 1);
                    })));

            /// <summary>Places real items into the round's own state, as milestone 7.5's purchase will.</summary>
            public void PlaceItems(HeroId owner, PrintedValueChannel icon, int count)
            {
                for (var index = 0; index < count; index++)
                {
                    var definition = icon == PrintedValueChannel.Radius
                        ? RadiusItemDefinition
                        : definitions.First(candidate => candidate.Progression.ItemIcon == icon);
                    var card = new CardInstance(
                        new CardInstanceId($"{definition.Id.Value}-set-aside-{index}"),
                        definition.Reference,
                        owner,
                        new CardStateFacts(
                            CardLocation.SetAside,
                            false,
                            CardResolutionStatus.None,
                            CardFaceOrientation.FaceDown));
                    if (!definitions.Contains(definition))
                    {
                        definitions.Add(definition);
                    }

                    instances.Add(card);
                    var placed = Round.HeroItems.TryPlaceSetAsideCard(card, definitions);
                    Assert.That(placed.IsPlaced, Is.True, placed.RejectionReason.ToString());
                }
            }

            /// <summary>The spaces the printed-Radius selection would offer, through the shared query.</summary>
            public IReadOnlyList<HexCoordinate> TargetsInRadius(
                HeroItemState items, RuleModificationSet modifications = null) =>
                RadiusQuery.Targets(
                        CoordinateOf(RedHeroId),
                        RadiusTotal(items),
                        includeSelf: false,
                        RuleObject.ForHero(RedHeroId),
                        RuleActivity.ObjectPush,
                        Board,
                        configuration.Participants,
                        modifications ?? RuleModificationSet.Empty)
                    .Select(target =>
                    {
                        Assert.That(Board.TryGetObjectCoordinate(target, out var at), Is.True);
                        return at;
                    })
                    .ToArray();

            /// <summary>
            /// Commits both heroes' cards and starts the printed-Radius Skill through the live round
            /// path, so the disk the player is offered is the one the bound item state produces.
            /// </summary>
            public RoundResolutionResult StartRadiusSkill()
            {
                var blueCard = instances.First(instance =>
                    instance.OwnerId.Equals(BlueHeroId) && instance.State.Location == CardLocation.Hand);
                Assert.That(Round.TryCommitCard(RedHeroId, RadiusCard.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(BlueHeroId, blueCard.Id).Accepted, Is.True);
                Assert.That(
                    Round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(RedHeroId),
                    "The Radius card's Initiative is the highest dealt, so its hero acts first.");

                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, instances, RedHeroId, RadiusCard.Id)
                    .Single(candidate => candidate.ActionDefinitionId.Equals(RadiusAction));
                return Round.TryStartSkill(new ActionResolutionId("radius-skill"), offer);
            }

            /// <summary>
            /// Names a candidate and confirms it where the authority still asks for a confirm, which is
            /// the same shape every other value selection in this build answers with.
            /// </summary>
            public void SelectTarget(HexCoordinate coordinate)
            {
                var choice = PendingChoice;
                Assert.That(choice, Is.Not.Null);
                var invocation = choice.SelectInvocations.Single(candidate =>
                    candidate.Candidate.Value.Coordinate.Equals(coordinate));
                Assert.That(Round.TryAnswerEffectChoice(invocation).Accepted, Is.True);

                var confirm = PendingChoice?.ConfirmInvocation;
                if (confirm != null && PendingChoice.NodeId.Equals(choice.NodeId))
                {
                    Assert.That(Round.TryAnswerEffectChoice(confirm).Accepted, Is.True);
                }
            }

            private static IReadOnlyList<HexCoordinate> Coordinates(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates;
            }
        }
    }
}
