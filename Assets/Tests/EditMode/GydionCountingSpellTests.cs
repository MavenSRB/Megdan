using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30e: Gydion's spell definitions, the counting spells and the Familiar. The four
    /// composed spells are cast through 9.30c's path on a staged round - the caster (Red) owns all
    /// twenty-two spells in his book, with <c>removed</c> staged faceup by 9.30b's own transition - and
    /// their printed outcomes are measured on the board, the coins and the book (9.13). Nothing is
    /// registered: the fixture's catalog carries Gydion's deferred handlers and his Familiar.
    /// </summary>
    public sealed class GydionCountingSpellTests
    {
        // -------------------------------------------------------------------------------------------
        // The content: twenty-two spells against the shard's Spells table
        // -------------------------------------------------------------------------------------------

        private sealed class Row
        {
            public Row(CardDefinitionId id, string name, CardColor color, CardTier tier, ActionKind kind,
                AttackRangeKind range, string owedTo, params (PrintedValueChannel Channel, int Amount)[] values)
            {
                Id = id; Name = name; Color = color; Tier = tier; Kind = kind; Range = range; OwedTo = owedTo;
                Values = values;
            }

            public CardDefinitionId Id { get; }
            public string Name { get; }
            public CardColor Color { get; }
            public CardTier Tier { get; }
            public ActionKind Kind { get; }
            public AttackRangeKind Range { get; }
            public string OwedTo { get; }
            public (PrintedValueChannel Channel, int Amount)[] Values { get; }
        }

        private const PrintedValueChannel A = PrintedValueChannel.Attack;
        private const PrintedValueChannel R = PrintedValueChannel.Range;
        private const PrintedValueChannel Rad = PrintedValueChannel.Radius;
        private const PrintedValueChannel M = PrintedValueChannel.Movement;

        /// <summary>HERO_GYDION.md's Spells table, row by row, as the faces print it.</summary>
        private static readonly Row[] ShardTable =
        {
            new Row(GydionCardContent.MagicMissile, "Magic Missile", CardColor.Gold, CardTier.None,
                ActionKind.Attack, AttackRangeKind.Ranged, null, (A, 1), (R, 3)),
            new Row(GydionCardContent.ShockingGrasp, "Shocking Grasp", CardColor.Gold, CardTier.None,
                ActionKind.Attack, AttackRangeKind.NonRanged, null, (A, 3)),
            new Row(GydionCardContent.ExpeditiousRetreat, "Expeditious Retreat", CardColor.Gold, CardTier.None,
                ActionKind.Movement, AttackRangeKind.None, null, (M, 5)),
            new Row(GydionCardContent.BurningHands, "Burning Hands", CardColor.Red, CardTier.I,
                ActionKind.Attack, AttackRangeKind.NonRanged, null, (A, 5)),
            new Row(GydionCardContent.Fireball, "Fireball", CardColor.Red, CardTier.II,
                ActionKind.Attack, AttackRangeKind.Ranged, null, (A, 5), (R, 3)),
            new Row(GydionCardContent.Sunburst, "Sunburst", CardColor.Red, CardTier.III,
                ActionKind.Attack, AttackRangeKind.Ranged, null, (A, 0), (R, 0)),
            new Row(GydionCardContent.VampiricTouch, "Vampiric Touch", CardColor.Red, CardTier.II,
                ActionKind.Attack, AttackRangeKind.NonRanged, null, (A, 5)),
            new Row(GydionCardContent.CreateUndead, "Create Undead", CardColor.Red, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null, (R, 3)),
            new Row(GydionCardContent.EnergyDrain, "Energy Drain", CardColor.Red, CardTier.III,
                ActionKind.Skill, AttackRangeKind.None, null, (R, 3)),
            new Row(GydionCardContent.Suggestion, "Suggestion", CardColor.Blue, CardTier.I,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.DominatePerson, "Dominate Person", CardColor.Blue, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.PowerWordKill, "Power Word Kill", CardColor.Blue, CardTier.III,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.MidasTouch, "Midas' Touch", CardColor.Blue, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null),
            new Row(GydionCardContent.Disintegrate, "Disintegrate", CardColor.Blue, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null),
            new Row(GydionCardContent.Polymorph, "Polymorph", CardColor.Blue, CardTier.III,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.Shield, "Shield", CardColor.Green, CardTier.I,
                ActionKind.Skill, AttackRangeKind.None, null),
            new Row(GydionCardContent.Banishment, "Banishment", CardColor.Green, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.Invulnerability, "Invulnerability", CardColor.Green, CardTier.III,
                ActionKind.Skill, AttackRangeKind.None, null),
            new Row(GydionCardContent.FindFamiliar, "Find Familiar", CardColor.Green, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.DimensionDoor, "Dimension Door", CardColor.Green, CardTier.II,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 0)),
            new Row(GydionCardContent.CloudKill, "Cloud Kill", CardColor.Green, CardTier.III,
                ActionKind.Skill, AttackRangeKind.None, null, (Rad, 3)),
            new Row(GydionCardContent.Wish, "Wish", CardColor.Purple, CardTier.IV,
                ActionKind.Skill, AttackRangeKind.None, null)
        };

        [Test]
        public void EverySpellDefinition_PrintsItsRowOfTheShardsSpellsTable()
        {
            var spells = GydionCardContent.SpellDefinitions;
            Assert.That(spells.Select(spell => spell.Id), Is.EqualTo(ShardTable.Select(row => row.Id)),
                "twenty-two spells, in the shard's order (GY-Q1: every one is in the book)");
            Assert.That(spells.Select(spell => spell.Id.Value), Is.Unique);

            foreach (var (spell, row) in spells.Zip(ShardTable, (spell, row) => (spell, row)))
            {
                var at = row.Name + ": ";
                Assert.That(spell.DisplayName, Is.EqualTo(row.Name), at + "printed name");
                Assert.That(spell.Id.Value, Does.StartWith("gydion-"), at + "hero-content id style");
                Assert.That(spell.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));
                Assert.That(spell.Progression.IsSpell, Is.True, at + "a spell (9.30b)");
                Assert.That(spell.Progression.Color, Is.EqualTo(row.Color), at + "colour");
                Assert.That(spell.Progression.Tier, Is.EqualTo(row.Tier), at + "Tier");
                Assert.That(spell.Progression.Classification, Is.EqualTo(row.Tier == CardTier.None
                    ? CardClassification.Basic : CardClassification.NonBasic), at + "classification");
                Assert.That(spell.Initiative.IsDefined, Is.False, at + "a spell prints no Initiative");
                Assert.That(spell.SourceContent?.PrintedText, Is.Not.Empty, at + "transcribed words");

                var primary = spell.ActionDefinitions.Single();
                Assert.That(primary.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
                Assert.That(primary.Kind, Is.EqualTo(row.Kind), at + "printed primary type");
                Assert.That(primary.AttackRange, Is.EqualTo(row.Range), at + "Ranged or not");
                Assert.That(primary.BaseValues.Select(value => (value.Channel, value.Amount)),
                    Is.EquivalentTo(row.Values), at + "printed values and icons");

                if (row.OwedTo == null)
                {
                    Assert.That(primary.TextComposition.IsAwaited, Is.False, at + "composed (9.30d, 9.30e)");
                }
                else
                {
                    Assert.That(primary.TextComposition, Is.EqualTo(PrintedTextComposition.AwaitedAt(row.OwedTo)),
                        at + "its text is owed to the block that composes it");
                    Assert.That(primary.EffectRoot, Is.Null, at + "and none of it runs meanwhile");
                    Assert.That(BorrowedPrimaryActionDeclaration.Refuses(spell),
                        Is.EqualTo(BorrowRefusal.PrintedTextNotComposed), at + "so it cannot be cast short");
                }
            }

            Assert.That(GydionCardContent.InBookV1(GydionCardContent.Sunburst).ToString(),
                Does.Contain("gydion-sunburst-in-book"), "each spell's naming step is keyed on its id");
            Assert.That(GydionComponentContent.Handlers().Select(handler => handler.Reference),
                Is.SupersetOf(spells.Select(spell => GydionCardContent.InBookV1(spell.Id))));
        }

        [Test]
        public void TheZeroPlusIcons_AreRaisedByTheirOwnText_PerOtherSpellRemoved()
        {
            var sunburst = GydionCardContent.Definition(GydionCardContent.Sunburst).ActionDefinitions.Single();
            Assert.That(sunburst.RosterValueContributions, Is.EquivalentTo(new[]
            {
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack, RosterValueQueryKind.OtherSpellsRemovedFromSpellbook, 1),
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Range, RosterValueQueryKind.OtherSpellsRemovedFromSpellbook, 1)
            }), "Sunburst's A 0+ and R 0+");
            Assert.That(sunburst.AttackTargetPolicy?.Reach, Is.EqualTo(AttackTargetReach.ExactlyPrintedRange),
                "\"Target a unit at maximum range\" (RULING 1)");

            var door = GydionCardContent.Definition(GydionCardContent.DimensionDoor).ActionDefinitions.Single();
            Assert.That(door.RosterValueContributions, Is.EqualTo(new[]
            {
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Radius, RosterValueQueryKind.OtherSpellsRemovedFromSpellbook, 1)
            }), "Dimension Door's r 0+");

            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Radius, RosterValueQueryKind.EnemyUnitsAdjacentToActor, 1),
                Throws.ArgumentException, "a Radius is raised by the spell count alone");
            Assert.That(() => new ActionDefinition(new ActionDefinitionId("zero-range-no-text"),
                    ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(A, 1), new PrintedValueDefinition(R, 0) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged, default),
                Throws.ArgumentException, "a printed 0 Range with no text raising it is still refused");
        }

        // -------------------------------------------------------------------------------------------
        // The count
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheCount_ExcludesTheCastingSpell_AndReadsTheSpellsOwnersBook()
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.Sunburst),
                removed: new[] { GydionCardContent.MagicMissile, GydionCardContent.Shield });
            var sunburst = round.Spell(GydionCardContent.Sunburst);
            Assert.That(SpellbookQuery.OtherRemoved(round.Cards, sunburst.Id), Is.EqualTo(2),
                "in the book: every removed spell is another");

            Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(sunburst), Is.True);
            Assert.That(SpellbookQuery.Removed(round.Cards, GydionRound.Caster), Has.Count.EqualTo(3));
            Assert.That(SpellbookQuery.OtherRemoved(round.Cards, sunburst.Id), Is.EqualTo(2),
                "cast and removed: it is not one of its own \"other spell cards\"");
            Assert.That(SpellbookQuery.OtherRemoved(round.Cards, round.Spell(GydionCardContent.Shield).Id),
                Is.EqualTo(2), "any removed spell excludes only itself");
            Assert.That(SpellbookQuery.OtherRemoved(round.Cards, GydionRound.Played), Is.EqualTo(3),
                "a card outside the book counts its owner's whole removed pile");
            Assert.That(SpellbookQuery.OtherRemoved(round.Cards, new CardInstanceId("nowhere")), Is.EqualTo(0));
        }

        // -------------------------------------------------------------------------------------------
        // Sunburst
        // -------------------------------------------------------------------------------------------

        [Test]
        public void Sunburst_WithNothingElseRemoved_HasRangeZero_IsStillCast_AndReachesNobody_GyQ12()
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.Sunburst),
                enemyAt: new HexCoordinate(1, 0));
            Assert.That(round.IsOffered, Is.True,
                "GY-Q12 (user): an in-book spell is always castable - the removal changes the counts");
            round.StartCasting();
            Assert.That(round.Spell(GydionCardContent.Sunburst).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "the spell is cast and removed faceup");
            Assert.That(round.Turn.PendingAttackTargetChoice, Is.Null,
                "Range 0 + 0 others reaches no unit, not even the adjacent enemy");
            Assert.That(round.Turn.PendingDefenseChoice, Is.Null);
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
            Assert.That(round.Card(GydionRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Sunburst_WithNOthersRemoved_TargetsAtExactlyRangeN_WithAttackN(int others)
        {
            var removed = Others(GydionCardContent.Sunburst, others);
            var near = new GydionRound(GydionRound.CastingCard(GydionCardContent.Sunburst), removed,
                enemyAt: new HexCoordinate(others == 1 ? 2 : others - 1, 0));
            near.StartCasting();
            Assert.That(near.Turn.PendingAttackTargetChoice, Is.Null, others +
                " others: an enemy at any other distance is not at maximum range, so the cast reaches nobody");

            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.Sunburst), removed,
                enemyAt: new HexCoordinate(others, 0));
            round.StartCasting();
            Assert.That(round.Spell(GydionCardContent.Sunburst).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "cast and removed faceup");
            var target = round.Turn.PendingAttackTargetChoice;
            Assert.That(target?.LegalTargetHeroIds, Is.EqualTo(new[] { GydionRound.Enemy }),
                "the enemy at exactly the Range Total (0 + " + others + ") is the target");
            round.TargetEnemy();
            Assert.That(round.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(others),
                "Attack 0 + 1 per other spell removed - Sunburst itself, removed as it is cast, not counted");
        }

        [Test]
        public void Sunburst_AddsHisItems_AsPage16AppliesThemToItsPrintedIcons()
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.Sunburst),
                Others(GydionCardContent.Sunburst, 1), enemyAt: new HexCoordinate(2, 0),
                items: new[] { PrintedValueChannel.Range, PrintedValueChannel.Attack });
            round.StartCasting();
            Assert.That(round.Turn.PendingAttackTargetChoice?.LegalTargetHeroIds, Is.EqualTo(new[] { GydionRound.Enemy }),
                "Range 0 + 1 other + the Range item = 2");
            round.TargetEnemy();
            Assert.That(round.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(2),
                "Attack 0 + 1 other + the Attack item");
        }

        /// <summary>
        /// Lesson 27: the targets the Attack offers and the total it resolves with read one count, on
        /// every staged book - and the probe of the cast Attack (9.6's question, asked before the cast
        /// with the spell still in the book) reads that count too.
        /// </summary>
        [Test]
        public void SunburstsProbeTargetOfferAndTotal_ReadOneCount_OnEveryStagedBook()
        {
            for (var others = 0; others <= 4; others++)
            {
                for (var distance = 1; distance <= 5; distance++)
                {
                    var position = others + " others, enemy at " + distance;
                    var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.Sunburst),
                        Others(GydionCardContent.Sunburst, others), enemyAt: new HexCoordinate(distance, 0));
                    Assert.That(round.IsOffered, Is.True, position + ": castable while in the book (GY-Q12)");
                    Assert.That(round.SpellsPrimaryCanAct(GydionCardContent.Sunburst), Is.EqualTo(distance == others),
                        position + ": the probe, before the cast, reaches the enemy exactly at the Range the count makes");

                    round.StartCasting();
                    var target = round.Turn.PendingAttackTargetChoice;
                    Assert.That(target != null, Is.EqualTo(distance == others),
                        position + ": and so does the cast Attack's target offer, after the spell is removed");
                    if (target == null)
                    {
                        continue;
                    }

                    Assert.That(target.LegalTargetHeroIds, Is.EqualTo(new[] { GydionRound.Enemy }), position);
                    round.TargetEnemy();
                    Assert.That(round.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(others),
                        position + ": and the Attack Total");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // Dimension Door
        // -------------------------------------------------------------------------------------------

        [Test]
        public void DimensionDoor_WithNothingElseRemoved_HasRadiusZero_IsCastAndPlacesNothing_UnlessAnItemRaisesIt()
        {
            var bare = new GydionRound(GydionRound.CastingCard(GydionCardContent.DimensionDoor));
            Assert.That(bare.IsOffered, Is.True, "GY-Q12: castable while in the book");
            Assert.That(bare.SpellsPrimaryCanAct(GydionCardContent.DimensionDoor), Is.False,
                "Radius 0 + 0 others: no space at maximum radius");
            bare.StartCasting();
            Assert.That(bare.Spell(GydionCardContent.DimensionDoor).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "the spell is cast and removed faceup");
            Assert.That(bare.Turn.PendingEffectChoice, Is.Null, "and asks no destination");
            Assert.That(bare.Board.TryGetHeroCoordinate(GydionRound.Caster, out var stayed) &&
                stayed.Equals(GydionRound.CasterAt), Is.True, "nobody moves");

            var item = new GydionRound(GydionRound.CastingCard(GydionCardContent.DimensionDoor),
                items: new[] { PrintedValueChannel.Radius });
            Assert.That(item.SpellsPrimaryCanAct(GydionCardContent.DimensionDoor), Is.True,
                "the Radius item raises the printed 0 (R1)");
            item.StartCasting();
            Assert.That(item.OfferedSpaces().ToArray(), Has.Length.EqualTo(6), "the ring at Radius 0 + 1");
            Assert.That(item.OfferedSpaces().Select(Distance), Is.All.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void DimensionDoor_WithNOthersRemoved_PlacesHimAtExactlyRadiusN(int others)
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.DimensionDoor),
                Others(GydionCardContent.DimensionDoor, others));
            Assert.That(round.SpellsPrimaryCanAct(GydionCardContent.DimensionDoor), Is.True,
                "the probe, before the cast, reads the same Radius");
            round.StartCasting();
            var offered = round.OfferedSpaces();
            Assert.That(offered, Is.Not.Empty);
            Assert.That(offered.Select(Distance), Is.All.EqualTo(others),
                "\"a space at maximum radius\": exactly Radius 0 + " + others);
            Assert.That(offered.ToArray(), Has.Length.EqualTo(6 * others), "the whole ring, every space of it empty");

            var to = offered.OrderBy(space => space.Q).ThenBy(space => space.R).First();
            round.PickSpace(to);
            Assert.That(round.Board.TryGetHeroCoordinate(GydionRound.Caster, out var at) && at.Equals(to), Is.True,
                "\"Place yourself\"");
            Assert.That(round.Spell(GydionCardContent.DimensionDoor).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook));
        }

        // -------------------------------------------------------------------------------------------
        // Midas' Touch
        // -------------------------------------------------------------------------------------------

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(5, 2)]
        public void MidasTouch_GainsOneCoinForEveryTwoOthersRemoved_RoundedDown(int others, int coins)
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.MidasTouch),
                Others(GydionCardContent.MidasTouch, others));
            var before = round.Coins(GydionRound.Caster);
            Assert.That(round.IsOffered, Is.True, others + " others: castable while in the book (GY-Q12)");
            Assert.That(round.SpellsPrimaryCanAct(GydionCardContent.MidasTouch), Is.EqualTo(coins > 0),
                others + " others: the probe reads the gain the resolution makes - 0 is no gain at all");

            round.StartCasting();
            Assert.That(round.Coins(GydionRound.Caster) - before, Is.EqualTo(coins),
                others + " others removed: floor(" + others + " / 2)");
            Assert.That(round.Spell(GydionCardContent.MidasTouch).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "cast and removed faceup either way");
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
            Assert.That(round.Card(GydionRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void MidasTouch_GainingNothing_IsSelectableBesideAnotherSpell_AndIsCast()
        {
            var round = new GydionRound(
                GydionRound.ChooseAndCastCard(GydionCardContent.MidasTouch, GydionCardContent.FindFamiliar));
            Assert.That(round.IsOffered, Is.True);
            round.StartCasting();
            Assert.That(round.Choice.UnavailableCandidates, Is.Empty, "both spells are in the book (GY-Q12)");
            var before = round.Coins(GydionRound.Caster);
            round.ChooseArm(GydionRound.CastArm(GydionCardContent.MidasTouch));
            Assert.That(round.Spell(GydionCardContent.MidasTouch).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "the spell is cast");
            Assert.That(round.Coins(GydionRound.Caster), Is.EqualTo(before), "and gains nothing");
            Assert.That(round.Spell(GydionCardContent.FindFamiliar).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Find Familiar
        // -------------------------------------------------------------------------------------------

        [Test]
        public void FindFamiliar_PlacesTheOneFamiliarInRadius_AnObstacleHeOwns()
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.FindFamiliar));
            Assert.That(round.IsOffered, Is.True);
            round.StartCasting();
            var offered = round.OfferedSpaces();
            Assert.That(offered, Is.Not.Empty);
            Assert.That(offered.Select(Distance), Is.All.InRange(1, 3), "in radius: Radius 3");
            Assert.That(offered.ToArray(), Has.Length.EqualTo(36), "every empty space within 3 (the enemy is far)");

            var to = new HexCoordinate(2, 0);
            round.PickSpace(to);
            var familiar = round.Familiars().Single();
            Assert.That(familiar.Coordinate, Is.EqualTo(to));
            Assert.That(familiar.ControllerHeroId, Is.EqualTo(GydionRound.Caster));
            Assert.That(familiar.Definition.SupplyCount, Is.EqualTo(1), "GY-Q5: one Familiar");
            Assert.That(ObstacleQuery.IsObstacle(round.Board, to, RuleModificationSet.Empty), Is.True,
                "p.19: all tokens are obstacles");

            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.FindFamiliarRemovalChoice),
                "then \"You may remove up to three other spell cards\"");
            round.Confirm();
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
            Assert.That(round.Spell(GydionCardContent.FindFamiliar).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(SpellbookQuery.Removed(round.Cards, GydionRound.Caster), Has.Count.EqualTo(1),
                "removing none is a real answer (W2/W5)");
        }

        [Test]
        public void FindFamiliar_WithTheFamiliarOnTheBoard_MovesTheSameOneToken()
        {
            var staged = new HexCoordinate(-3, 0);
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.FindFamiliar), familiarAt: staged);
            var before = round.Familiars().Single();
            round.StartCasting();
            var offered = round.OfferedSpaces();
            Assert.That(offered, Has.No.Member(staged), "it moves to a different, empty space");
            Assert.That(offered.Select(Distance), Is.All.InRange(1, 3), "in radius of Gydion");

            var to = new HexCoordinate(0, 2);
            round.PickSpace(to);
            var after = round.Familiars().Single();
            Assert.That(after.Id, Is.EqualTo(before.Id), "GY-Q5: placing it again MOVES the one Familiar");
            Assert.That(after.Coordinate, Is.EqualTo(to));
            Assert.That(round.Board.PlacedTokens, Has.Count.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void FindFamiliar_RemovesUpToThreeOtherSpellsFaceup_WithoutCastingThem(int count)
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.FindFamiliar),
                removed: new[] { GydionCardContent.Wish });
            round.StartCasting();
            round.PickSpace(new HexCoordinate(1, 0));

            var choice = round.Choice;
            Assert.That(choice.NodeId, Is.EqualTo(GydionCardContent.FindFamiliarRemovalChoice));
            Assert.That(choice.MinimumSelections, Is.EqualTo(0));
            Assert.That(choice.MaximumSelections, Is.EqualTo(3));
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(GydionCardContent.FindFamiliarRemovableSpells.Select(GydionCardContent.FindFamiliarRemovalArm)),
                "one option per OTHER spell, by name - Find Familiar itself is not one");
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { GydionCardContent.FindFamiliarRemovalArm(GydionCardContent.Wish) }),
                "a spell already removed is greyed");
            Assert.That(choice.Candidates.Select(candidate => candidate.Kind),
                Is.All.EqualTo(EffectChoiceCandidateKind.Option), "spells are named, never offered as book cards");

            var picks = new[] { GydionCardContent.Sunburst, GydionCardContent.Shield, GydionCardContent.Fireball }
                .Take(count).ToArray();
            foreach (var spell in picks)
            {
                round.SelectArm(GydionCardContent.FindFamiliarRemovalArm(spell));
            }

            if (count == 3)
            {
                Assert.That(round.Choice.SelectInvocations, Is.Empty, "up to three, and no more");
            }

            round.Confirm();
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
            foreach (var spell in picks)
            {
                Assert.That(round.Spell(spell).State, Is.EqualTo(new CardStateFacts(
                        CardLocation.RemovedFromSpellbook, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)),
                    spell.Value + ": removed from the book faceup");
            }

            Assert.That(SpellbookQuery.Removed(round.Cards, GydionRound.Caster).Select(card =>
                    card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(picks.Concat(new[] { GydionCardContent.Wish, GydionCardContent.FindFamiliar })));
            Assert.That(round.Turn.PendingAttackTargetChoice, Is.Null,
                "a removal is not a cast: no action is performed off a removed spell");
            Assert.That(round.Turn.PendingMovementChoice, Is.Null);
            Assert.That(round.Card(GydionRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void FindFamiliar_StillRemoves_WhenNoSpaceInRadiusCanTakeTheFamiliar()
        {
            var walls = GydionRound.Disk(3).Where(space => !space.Equals(GydionRound.CasterAt)).ToArray();
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.FindFamiliar), obstacles: walls);
            Assert.That(round.SpellsPrimaryCanAct(GydionCardContent.FindFamiliar), Is.True,
                "the removal can still act");
            round.StartCasting();
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.FindFamiliarRemovalChoice),
                "no empty space in radius: nothing is placed, and the removal is still asked");
            Assert.That(round.Familiars(), Is.Empty);
            round.SelectArm(GydionCardContent.FindFamiliarRemovalArm(GydionCardContent.MagicMissile));
            round.Confirm();
            Assert.That(round.Spell(GydionCardContent.MagicMissile).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook));
        }

        [Test]
        public void TheFamiliar_IsRemovedAtRoundEnd_AndSurvivesGydionsDefeat()
        {
            var round = new GydionRound(GydionRound.CastingCard(GydionCardContent.FindFamiliar),
                enemyAt: new HexCoordinate(-1, 0), enemyStrikes: true);
            round.StartCasting();
            round.PickSpace(new HexCoordinate(2, 0));
            round.Confirm();
            var familiar = round.Familiars().Single();

            round.EnemyDefeatsTheCaster();
            Assert.That(round.Board.Occupancies.Any(value => value.HeroId.Equals(GydionRound.Caster)), Is.False,
                "Gydion is defeated and off the board");
            Assert.That(round.Familiars().Single().Id, Is.EqualTo(familiar.Id),
                "p.19: \"Tokens are not removed when the hero who placed them is defeated\" (GY-Q5)");

            Assert.That(ComponentCleanupRules.TokensRemovedAtEndOfRound(round.Board.PlacedTokens).Select(token => token.Id),
                Is.EqualTo(new[] { familiar.Id }),
                "p.19: \"Unless the card text says otherwise, tokens are removed at the end of round\"");
        }

        [Test]
        public void FindFamiliarsRemovalOptions_HaveLabelsDerivedFromTheSpellsTheyName()
        {
            var catalogs = GydionRound.CreateCatalogs();
            var findFamiliar = GydionCardContent.Definition(GydionCardContent.FindFamiliar);
            var labels = GydionCardContent.CountingSpellDecisionLabels().ToArray();
            Assert.That(DecisionLabelRequirementQuery.For(findFamiliar, catalogs).Select(row => row.NodeId),
                Is.EquivalentTo(labels.Select(label => label.NodeId)),
                "the controls Find Familiar raises are exactly the labelled options");
            Assert.That(labels.Single(label => label.NodeId.Equals(
                    GydionCardContent.FindFamiliarRemovalArm(GydionCardContent.MidasTouch))).Label.Chosen,
                Is.EqualTo("Remove Midas' Touch"), "the printed name, read off the definition");
        }

        #if UNITY_EDITOR
        /// <summary>
        /// The Familiar's private face (element 5831117, the white owl holding a book) loads by its
        /// definition id and wears no caption; without it the disc keeps its name.
        /// </summary>
        [Test]
        public void TheFamiliarsPreparedFace_LoadsAndWearsNoCaption()
        {
            Assert.That(GuardsDigital.Presentation.Board.PlacedTokenArtwork.TryLoadTexture(
                    GydionComponentContent.FamiliarId, out _), Is.True,
                "run Tools/Prepare-TokenArtwork.ps1 -Hero gydion in this checkout");
            Assert.That(GuardsDigital.Presentation.Board.PlacedTokenArtwork.Caption(
                GydionComponentContent.Familiar, true), Is.Empty);
            Assert.That(GuardsDigital.Presentation.Board.PlacedTokenArtwork.Caption(
                GydionComponentContent.Familiar, false), Is.EqualTo("FAMILIAR"));
        }
        #endif

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>The first <paramref name="count"/> spells in book order other than <paramref name="cast"/>.</summary>
        private static CardDefinitionId[] Others(CardDefinitionId cast, int count) =>
            GydionCardContent.SpellIds.Where(id => !id.Equals(cast)).Take(count).ToArray();

        private static int Distance(HexCoordinate space) =>
            HexDistanceQuery.Calculate(GydionRound.CasterAt, space);

        /// <summary>
        /// One round on a disk: the caster (Red) owns Gydion's twenty-two spells, <c>removed</c> staged
        /// faceup by 9.30b's transition, and has committed a casting card; the enemy (Blue) has committed
        /// a card he can only Hold (or a strong Attack) and holds a Wall.
        /// </summary>
        internal sealed class GydionRound
        {
            internal static readonly HeroId Caster = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly CardInstanceId Played = new CardInstanceId("gydion-played");
            internal static readonly CardInstanceId EnemyPlayed = new CardInstanceId("gydion-enemy-played");
            internal static readonly CardInstanceId EnemyWall = new CardInstanceId("gydion-enemy-wall");
            internal static readonly HexCoordinate CasterAt = new HexCoordinate(0, 0);
            internal static readonly ActionDefinitionId CastingPrimary = new ActionDefinitionId("primary-skill");

            private readonly CardActionResolutionAuthority cardActions;
            private readonly MatchParticipants participants = StagedBoardParticipants.Create();
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();

            internal GydionRound(
                CardDefinition played,
                IEnumerable<CardDefinitionId> removed = null,
                HexCoordinate? enemyAt = null,
                IEnumerable<PrintedValueChannel> items = null,
                IEnumerable<HexCoordinate> obstacles = null,
                HexCoordinate? familiarAt = null,
                bool enemyStrikes = false,
                int boardRadius = 5)
            {
                var catalogs = CreateCatalogs();
                Definitions = GydionCardContent.SpellDefinitions
                    .Concat(new[] { played, EnemyHold, EnemyStrike, EnemyWallDefinition })
                    .ToArray();

                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Played, played.Reference, Caster),
                    CardInstance.CreateInHand(EnemyPlayed, (enemyStrikes ? EnemyStrike : EnemyHold).Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, EnemyWallDefinition.Reference, Enemy)
                };
                var spells = GydionCardContent.SpellDefinitions
                    .Select(spell => CardInstance.CreateInSpellbook(
                        new CardInstanceId("gydion-book-" + spell.Id.Value), spell.Reference, Caster))
                    .ToArray();
                foreach (var spell in removed ?? Enumerable.Empty<CardDefinitionId>())
                {
                    Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(
                            spells.Single(card => card.DefinitionReference.DefinitionId.Equals(spell))), Is.True,
                        "the staged removal is 9.30b's own transition");
                }

                cards.AddRange(spells);
                Cards = cards.ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("gydion-counting"), 1, Disk(boardRadius)),
                    new[]
                    {
                        new HeroOccupancy(Caster, CasterAt),
                        new HeroOccupancy(Enemy, enemyAt ?? new HexCoordinate(boardRadius, 0))
                    },
                    (obstacles ?? Enumerable.Empty<HexCoordinate>())
                        .Select((at, index) => new ObstacleOccupancy(new ObstacleId($"gydion-rock-{index:00}"), at))
                        .ToArray());
                if (familiarAt.HasValue)
                {
                    Board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId("gydion-earlier-familiar"), GydionComponentContent.Familiar,
                        Caster, Caster, new CardInstanceId("gydion-earlier-cast"), familiarAt.Value));
                }

                var markers = new MarkerAttachmentRegister();
                cardActions = new CardActionResolutionAuthority(catalogs, Board, markers);
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(cardActions, Board, timing, markers,
                        null, null, null, new TeamLifeState(participants, 4), null, null));
                foreach (var item in items ?? Enumerable.Empty<PrintedValueChannel>())
                {
                    Assert.That(Round.HeroItems.TryGrantItemFromTheBox(Caster, item).IsPlaced, Is.True);
                }

                Accept(Round.TryCommitCard(Caster, Played));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            /// <summary>
            /// The build's own catalogs: since Gydion's dealing (9.30i) they register his naming steps,
            /// the Familiar and its token, the spellbook's contracts and Dimension Door's Place.
            /// </summary>
            internal static MatchContentCatalogs CreateCatalogs() => MatchContentCatalogs.ForThisBuild();

            internal static IReadOnlyList<HexCoordinate> Disk(int radius) =>
                (from q in Enumerable.Range(-radius, 2 * radius + 1)
                 from r in Enumerable.Range(-radius, 2 * radius + 1)
                 where Math.Abs(q + r) <= radius
                 select new HexCoordinate(q, r)).ToArray();

            /// <summary>The bullet a casting card casts <paramref name="spell"/> with.</summary>
            internal static EffectNodeId CastArm(CardDefinitionId spell) => new EffectNodeId("cast-" + spell.Value);

            /// <summary>"Cast the X spell in the spellbook" - 9.30c's single cast bullet as the root.</summary>
            internal static CardDefinition CastingCard(CardDefinitionId spell) =>
                Caster_(new CardDefinitionId("test-cast-" + spell.Value), "TEST - Cast " + spell.Value,
                    CastArm(spell), SyntheticSpellbookCapabilities.CastArm(CastArm(spell), GydionCardContent.InBookV1(spell)));

            /// <summary>"Choose and cast one spell in the spellbook" over <paramref name="spells"/>.</summary>
            internal static CardDefinition ChooseAndCastCard(params CardDefinitionId[] spells)
            {
                var root = new EffectNodeId("choose-and-cast-one");
                return Caster_(new CardDefinitionId("test-choose-and-cast"), "TEST - Choose and cast", root,
                    new[]
                    {
                        new EffectNodeDefinition(root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                            childNodeIds: spells.Select(CastArm), chooser: EffectChooserRole.Actor,
                            minimumSelections: 1, maximumSelections: 1)
                    }.Concat(spells.SelectMany(spell =>
                        SyntheticSpellbookCapabilities.CastArm(CastArm(spell), GydionCardContent.InBookV1(spell)))));
            }

            private static CardDefinition Caster_(
                CardDefinitionId id, string name, EffectNodeId root, IEnumerable<EffectNodeDefinition> nodes) =>
                new CardDefinition(
                    id, "rev-1", name, new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.BasicCard(CardColor.Silver),
                    new[]
                    {
                        new ActionDefinition(CastingPrimary, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            Array.Empty<PrintedValueDefinition>(),
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default,
                            new EffectDefinition(new EffectDefinitionId(id.Value + "-skill"), "rev-1", root, nodes))
                    });

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;

            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal CardInstance Spell(CardDefinitionId spell) =>
                Cards.Single(card => card.OwnerId.Equals(Caster) && card.DefinitionReference.DefinitionId.Equals(spell));

            internal int Coins(HeroId hero) => cardActions.CoinLedger == null ? 0 : cardActions.CoinLedger.GetCoins(hero);

            internal IReadOnlyList<PlacedToken> Familiars() =>
                Board.PlacedTokens.Where(token => token.Definition.Id.Equals(GydionComponentContent.FamiliarId)).ToArray();

            internal bool IsOffered => OfferOn(Played).IsAvailable;

            /// <summary>
            /// 9.6's question of <paramref name="spell"/>'s printed primary, asked before any cast with
            /// the spell still in the book - the offer the borrow would mint, judged by the rules the
            /// turn applies to its own offers (6.5af's Skill, 7.8a1's Attack), exactly as
            /// <c>TurnResolutionAuthority.BorrowedPrimaryCanAct</c> asks it.
            /// </summary>
            internal bool SpellsPrimaryCanAct(CardDefinitionId spell)
            {
                var card = Spell(spell);
                var definition = Definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
                var primary = definition.ActionDefinitions.Single(value =>
                    value.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var offer = new ActionOffer(card.Id, definition.Reference, primary.Id, Caster,
                    ActionInvocationContext.TextGranted, true, primary.AttackRange, primary.SourceCardStateTransition);
                return primary.Kind == ActionKind.Attack
                    ? cardActions.CanAttackFindTarget(offer, Definitions, Cards, participants, timing,
                        RuleModificationSet.Empty, 1, Round.TurnIndex)
                    : cardActions.CanSkillApplyAnything(offer, Definitions, Cards, participants, timing,
                        RuleModificationSet.Empty, 1, Round.TurnIndex, out _);
            }

            internal void StartCasting() =>
                Accept(Round.TryStartSkill(new ActionResolutionId("cast-" + Played.Value), OfferOn(Played)));

            internal IReadOnlyList<HexCoordinate> OfferedSpaces()
            {
                Assert.That(Choice, Is.Not.Null, "a destination is asked");
                return Choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate)
                    .ToArray();
            }

            internal void PickSpace(HexCoordinate space)
            {
                Assert.That(Choice, Is.Not.Null, "a destination is asked");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    invocation.Candidate.Value.Coordinate.Equals(space))));
            }

            internal void ChooseArm(EffectNodeId arm) => SelectArm(arm);

            internal void SelectArm(EffectNodeId arm)
            {
                Assert.That(Choice, Is.Not.Null, "a choice stands");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(arm))));
            }

            internal IReadOnlyList<RoundResolutionEvent> Confirm()
            {
                Assert.That(Choice?.ConfirmInvocation, Is.Not.Null, "the choice can be confirmed");
                var mark = events.Count;
                Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));
                return events.Skip(mark).ToArray();
            }

            internal void TargetEnemy()
            {
                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "the cast Attack asks for its target");
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
            }

            /// <summary>The caster's turn ends; the enemy's Attack 20 then defeats him undefended.</summary>
            internal void EnemyDefeatsTheCaster()
            {
                ReleaseIfAsked();
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
                var strike = Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(EnemyPlayed) && offer.ActionDefinitionId.Equals(EnemyStrikeId));
                Accept(Round.TryStartAttack(new ActionResolutionId("enemy-strike"), strike));
                var target = Turn.PendingAttackTargetChoice;
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Caster)));
                var defense = Turn.PendingDefenseChoice;
                if (defense != null)
                {
                    Accept(Round.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.Decline(defense.ResolutionId, defense.ChoiceId)));
                }
            }

            private void ReleaseIfAsked()
            {
                if (Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            private ActionOffer OfferOn(CardInstanceId card) =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(card) && offer.ActionDefinitionId.Equals(CastingPrimary));

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }

            private static readonly ActionDefinitionId EnemyStrikeId = new ActionDefinitionId("primary-attack");

            private static readonly CardDefinition EnemyHold = new CardDefinition(
                new CardDefinitionId("gydion-test-enemy-hold"), "rev-1", "TEST - Hold",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            private static readonly CardDefinition EnemyStrike = new CardDefinition(
                new CardDefinitionId("gydion-test-enemy-strike"), "rev-1", "TEST - Strike",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(EnemyStrikeId, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 20) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default)
                });

            private static readonly CardDefinition EnemyWallDefinition = new CardDefinition(
                new CardDefinitionId("gydion-test-enemy-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });
        }
    }
}
