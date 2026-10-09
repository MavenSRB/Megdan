using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardValuePreviewTests
    {
        [Test]
        public void ExperimentalOptionsSelectEveryCardAndShieldCategoryIndependently()
        {
            var options = new ExperimentalMatchOptions(
                heroShieldItems: true, heroShieldActiveEffects: false, heroShieldCardEffects: true,
                cardModifiersItems: false, cardModifiersMinions: true,
                cardModifiersActiveEffects: false, cardModifiersCardEffects: true);
            Assert.That(MatchSession.CardValueSources(options), Is.EqualTo(
                CardValuePreviewSources.Minions | CardValuePreviewSources.CardEffects));
            Assert.That(MatchSession.IncludeHeroShieldSupplement(
                options, ValueContributionKind.ItemModifier), Is.True);
            Assert.That(MatchSession.IncludeHeroShieldSupplement(
                options, ValueContributionKind.ActiveEffectModifier), Is.False);
            Assert.That(MatchSession.IncludeHeroShieldSupplement(
                options, ValueContributionKind.CardTextModifier), Is.True);
            Assert.That(MatchSession.IncludeHeroShieldSupplement(
                options, ValueContributionKind.ContinuousSourceModifier), Is.True);
            Assert.That(MatchSession.IncludeHeroShieldSupplement(
                options, ValueContributionKind.MinionDefenseModifier), Is.False,
                "minions are unconditional base rows, not optional supplements");
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        public void DefenseCardsCanShowItemsWithoutEnemyMinionModifiers(int itemCount, int expected)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var map = new MapId("synthetic-coordinate-board");
            var minion = new QuickGameMinionSetupState(new MinionId("enemy-creep"),
                new TeamId("blue"), TeamSide.Blue, StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(map, new SpawnPointId("spawn")),
                new ConfiguredSpaceReference(map, new SpaceId("q1_r0")), new HexCoordinate(1, 0));
            var coordinates = Enumerable.Range(-3, 7).SelectMany(q => Enumerable.Range(-3, 7)
                .Where(r => Math.Abs(q + r) <= 3).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(coordinates,
                new[] { new HeroOccupancy(owner, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(2, 0)) },
                Array.Empty<ObstacleOccupancy>(), new[] { minion });
            Assert.That(HeroDashboard.RulebookLayout.TryFindSlotAccepting(
                PrintedValueChannel.Defense, out var slot), Is.True);
            var items = HeroItemState.Restore(configuration.Participants, new HeroItemRestoreState(
                Enumerable.Range(0, itemCount).Select(index => new HeroItem(owner,
                    new CardInstanceId("item-" + index), slot.Position, PrintedValueChannel.Defense, index + 1))));
            var readings = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                configuration.CardInstances, configuration.Participants, board, items,
                RuleModificationSet.Empty, EffectRosterState.Absent, 1, 1, null, null,
                CardValuePreviewSources.Items);
            var card = configuration.CardInstances.First(c => c.OwnerId.Equals(owner) &&
                configuration.CardDefinitions.Single(d => d.Reference.Equals(c.DefinitionReference))
                    .ActionDefinitions.Any(a => a.Kind == ActionKind.Defense &&
                        a.MinionDefenseContributions == MinionDefenseContributionPolicy.IncludeAll &&
                        a.BaseValues.Any(v => v.Channel == PrintedValueChannel.Defense)));
            Assert.That(readings.SingleOrDefault(r => r.Owner.Equals(owner) &&
                r.Definition.Equals(card.DefinitionReference) && r.Channel == PrintedValueChannel.Defense)?.Bonus ?? 0,
                Is.EqualTo(expected));
            var minionOnly = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                configuration.CardInstances, configuration.Participants, board, items,
                RuleModificationSet.Empty, EffectRosterState.Absent, 1, 1, null, null,
                CardValuePreviewSources.Minions);
            Assert.That(minionOnly.Single(r => r.Owner.Equals(owner) &&
                r.Definition.Equals(card.DefinitionReference) && r.Channel == PrintedValueChannel.Defense).Bonus,
                Is.EqualTo(-1));
        }

        [Test]
        public void TargetedBodyAndDefenseWindowDoNotLeakMinionTotalsOntoCardsAndUndoRefreshes()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _, out var blue, out _);
            var original = Describe(session.Projection);
            var choice = session.Projection.PendingAttackTargetChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, blue, 2));
            var sawMinionDifference = false;
            foreach (var defense in session.Projection.PendingDefensePreview.Choices
                .Where(c => c.CardInstanceId.HasValue && c.DefenseTotal.HasValue))
            {
                var card = session.Projection.Cards.Single(c => c.Id.Equals(defense.CardInstanceId.Value));
                var definition = session.Projection.Definitions.Single(d => d.Reference.Equals(card.DefinitionReference));
                var printed = definition.ActionDefinitions.SelectMany(a => a.BaseValues)
                    .Single(v => v.Channel == PrintedValueChannel.Defense).Amount;
                var displayed = CardValueReadModel.ForCard(
                        session.Projection, card.OwnerId, card.DefinitionReference)
                    .SingleOrDefault(v => v.Channel == PrintedValueChannel.Defense)?.Bonus ?? 0;
                Assert.That(displayed, Is.Zero,
                    "The staged defender has no Defense item or explicit rule modifier.");
                sawMinionDifference |= defense.DefenseTotal.Value - printed != displayed;
            }
            Assert.That(sawMinionDifference, Is.True,
                "The authoritative Defense total still includes the targeted body's minion modifier.");
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(Describe(session.Projection), Is.EqualTo(original));
        }

        [Test]
        public void EverySeatAndWireCarryOnlyAlreadyVisibleFaces()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _, out var defender, out _);
            var pending = session.Projection.PendingAttackTargetChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, defender, 2));
            var host = new MatchHost(session);
            Assert.That(host.Projection.CardValues, Is.Empty,
                "The staged nonzero minion modifier is not a card-face modifier; populated wire encoding is covered by CopyCompletenessTests.");
            foreach (var seat in host.Seats.Seats)
            {
                var local = host.ProjectionForSeat(seat.Id);
                Assert.That(local.CardValues, Is.Not.Null);
                Assert.That(local.CardValues.All(value => local.Cards.Any(card =>
                    card.OwnerId.Equals(value.Owner) && card.DefinitionReference.Equals(value.Definition))), Is.True);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(NetworkSeatProjectionCodec.Encode(local),
                    out var record), Is.True);
                var remote = new NetworkMatchReadModel(host.Projection, MatchContentCatalogs.ForThisBuild(), record, false);
                Assert.That(Describe(remote), Is.EqualTo(Describe(local)));
            }
        }

        [Test]
        public void EveryConfiguredHeroProducesUniqueTypedReadings()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            foreach (var hero in roster)
            {
                var lineup = new[] { hero }.Concat(roster.Where(h => !h.ContentId.Equals(hero.ContentId)).Take(5)).ToArray();
                var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                    MatchFormat.ThreeVersusThreeQuick, new HeroLineup(lineup.Take(3), lineup.Skip(3)), TeamSide.Red);
                var coordinates = Enumerable.Range(-10, 21).SelectMany(q => Enumerable.Range(-10, 21)
                    .Where(r => Math.Abs(q + r) <= 10).Select(r => new HexCoordinate(q, r))).ToArray();
                var board = new AdjacentMoveAuthority(coordinates, configuration.Participants.Heroes
                    .Select((h, i) => new HeroOccupancy(h.Id, new HexCoordinate(i - 3, 0))),
                    Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
                var items = HeroItemState.Restore(configuration.Participants,
                    new HeroItemRestoreState(Array.Empty<HeroItem>()));
                var values = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                    configuration.CardInstances, configuration.Participants, board, items,
                    RuleModificationSet.Empty, EffectRosterState.Absent, 1, 1,
                    new GuardsDigital.Core.Components.MarkerAttachmentRegister(), null);
                Assert.That(values.Select(v => (v.Owner, v.Definition, v.Channel)).Distinct().Count(),
                    Is.EqualTo(values.Count), hero.ContentId.ToString());
            }
        }

        [Test]
        public void EveryChannelShowsItemsAndActiveEffectsAndRefreshesWhenRemoved()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var coordinates = Enumerable.Range(-5, 11).SelectMany(q => Enumerable.Range(-5, 11)
                .Where(r => Math.Abs(q + r) <= 5).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(coordinates, configuration.Participants.Heroes
                .Select((h, i) => new HeroOccupancy(h.Id, new HexCoordinate(i * 3, 0))),
                Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            var emptyItems = HeroItemState.Restore(configuration.Participants,
                new HeroItemRestoreState(Array.Empty<HeroItem>()));
            foreach (var channel in Enum.GetValues(typeof(PrintedValueChannel)).Cast<PrintedValueChannel>()
                .Where(c => c != PrintedValueChannel.None))
            {
                Assert.That(HeroDashboard.RulebookLayout.TryFindSlotAccepting(channel, out var slot), Is.True);
                var items = HeroItemState.Restore(configuration.Participants, new HeroItemRestoreState(
                    new[] { new HeroItem(owner, new CardInstanceId("item"), slot.Position, channel, 1) }));
                var mods = new RuleModificationSet(new[]
                    {
                        AppliedRuleModification.FromActiveEffect(
                            RuleModification.ForValue(channel, 2), RuleObject.ForHero(owner),
                            new ActiveEffectId("effect"), owner, TeamSide.Red, 1),
                        AppliedRuleModification.FromContinuousSource(
                            RuleModification.ForValue(channel, 4), RuleObject.ForHero(owner),
                            new ContinuousRuleSource("card-effect", ContinuousRuleSourceKind.Ultimate,
                                owner, "test-value", "hero", new CardInstanceId("source-card")),
                            TeamSide.Red, 2)
                    },
                    Array.Empty<AppliedObstacleOverlay>(), configuration.TieBreakerState);
                var itemValues = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                    configuration.CardInstances, configuration.Participants, board, items, mods,
                    EffectRosterState.Absent, 1, 1, null, null, CardValuePreviewSources.Items);
                Assert.That(itemValues.Any(v => v.Owner.Equals(owner) && v.Channel == channel && v.Bonus == 1),
                    Is.True, "item: " + channel);
                var effectValues = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                    configuration.CardInstances, configuration.Participants, board, items, mods,
                    EffectRosterState.Absent, 1, 1, null, null, CardValuePreviewSources.ActiveEffects);
                Assert.That(effectValues.Any(v => v.Owner.Equals(owner) && v.Channel == channel && v.Bonus == 2),
                    Is.True, "Active effect: " + channel);
                var cardEffectValues = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                    configuration.CardInstances, configuration.Participants, board, items, mods,
                    EffectRosterState.Absent, 1, 1, null, null, CardValuePreviewSources.CardEffects);
                Assert.That(cardEffectValues.Any(v => v.Owner.Equals(owner) && v.Channel == channel && v.Bonus == 4),
                    Is.True, "card effect: " + channel);
                var values = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                    configuration.CardInstances, configuration.Participants, board, items, mods,
                    EffectRosterState.Absent, 1, 1, null, null,
                    CardValuePreviewSources.Items | CardValuePreviewSources.ActiveEffects |
                    CardValuePreviewSources.CardEffects);
                Assert.That(values.Any(v => v.Owner.Equals(owner) && v.Channel == channel && v.Bonus == 7),
                    Is.True, "combined: " + channel);
                var cleared = CardValuePreviewQuery.Read(configuration.CardDefinitions,
                    configuration.CardInstances, configuration.Participants, board, emptyItems,
                    RuleModificationSet.Empty, EffectRosterState.Absent, 1, 1, null, null);
                Assert.That(cleared.Where(v => v.Owner.Equals(owner) && v.Channel == channel), Is.Empty,
                    "Removing items/effects must remove the old modifier: " + channel);
            }
        }

        private static string Describe(ICardValueReadModel read) => string.Join("|",
            read.CardValues.Select(v => v.Owner + "/" + v.Definition + "/" + v.Channel + "=" + v.Bonus));
    }
}
