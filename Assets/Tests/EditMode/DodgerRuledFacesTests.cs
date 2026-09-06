using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The six Dodger faces the user's 2026-09-06 rulings unparked, checked against the rulings
    /// themselves rather than against the shape of the code that implements them. Milestone
    /// 8.35z28.
    ///
    /// <para>Each case names the ruling it is evidence for, because that is what these assertions are
    /// for: the compositions were written from a decision list, and a reader auditing whether the build
    /// matches the decisions should not have to re-derive which clause each case is about.</para>
    /// </summary>
    [TestFixture]
    public sealed class DodgerRuledFacesTests
    {
        private static readonly HeroId Dodger = new HeroId("dodger");

        // -----------------------------------------------------------------------------------------
        // RULING 9 and RULING 10 - Tide of Darkness, the master switch behind nine faces
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The Ultimate composes p.16's permanent ability, and its whole content is the one rule the
        /// printed sentence states. It is checked as a rule rather than as a body because that is what
        /// distinguishes it from every other face of hers: it starts no step and asks nobody anything.
        /// </summary>
        [Test]
        public void TideOfDarkness_ComposesOnePermanentRuleAndNoAction()
        {
            var ultimate = DodgerCardContent.Definition(DodgerCardContent.TideOfDarkness);

            Assert.That(ultimate.UltimateRule, Is.Not.Null);
            Assert.That(
                ultimate.UltimateRule.TextComposition.IsAwaited, Is.False,
                "The user's rulings on D-02 and D-03 answered it, so it owes no composition.");
            Assert.That(
                ultimate.UltimateRule.Modifications.Select(value => value.Kind),
                Is.EqualTo(new[]
                {
                    RuleModificationKind.AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone
                }),
                "One printed sentence, one declaration.");
            Assert.That(
                ultimate.ActionDefinitions, Is.Empty,
                "It prints no Initiative, no icons and no actions at all.");
        }

        /// <summary>
        /// RULING 10, measured. Without the Ultimate the count is the map's own empty minion Spawn
        /// points; with it up, "2 or more empty spawn points in radius in the battle zone" reads "2 or
        /// more empty spaces in radius" - so the number rises to the empty spaces in the disk, and the
        /// hero Spawn point RULING 4 excludes stops mattering because every space now carries a granted
        /// point anyway.
        /// </summary>
        [Test]
        public void TheUltimate_TurnsTheCountIntoEmptySpacesInRadius()
        {
            var board = DodgerSpawnPointBoard.CreateBoard(Dodger, DodgerSpawnPointBoard.Origin);

            var printedOnly = EmptyMinionSpawnPointQuery.CountWithin(
                board, board, DodgerSpawnPointBoard.Origin, 2, SpawnPointOverlay.None);
            var withUltimate = EmptyMinionSpawnPointQuery.CountWithin(
                board, board, DodgerSpawnPointBoard.Origin, 2,
                SpawnPointOverlay.AllSpacesFriendlyTo(TeamSide.Red));

            Assert.That(printedOnly, Is.EqualTo(4),
                "RULING 4's measured number: the four empty minion Spawn points of the Battle Zone, " +
                "with the hero Spawn point one space away excluded and the Throne one out of zone.");

            var emptySpacesInRadius = DodgerSpawnPointBoard.BattleZoneCoordinates
                .Concat(DodgerSpawnPointBoard.ThroneCoordinates)
                .Where(coordinate => HexDistanceQuery.Calculate(
                    DodgerSpawnPointBoard.Origin, coordinate) <= 2)
                .Count(coordinate => board.IsEmptyPlayableSpace(coordinate));

            Assert.That(withUltimate, Is.EqualTo(emptySpacesInRadius),
                "With the Ultimate up the qualifying set is exactly the empty spaces in the disk - " +
                "the card supplies the Spawn point and the board supplies the emptiness (p.19).");
            Assert.That(withUltimate, Is.GreaterThan(printedOnly),
                "The Tier IV payoff is that the count goes up, which is what turns nine faces on.");
        }

        /// <summary>
        /// The Ultimate reaches spaces the real Battle Zone does not, which is the other half of what
        /// "all spaces count as if they were in the battle zone" says. Measured on the Throne space the
        /// phase-one fixture put outside the zone on purpose.
        /// </summary>
        [Test]
        public void TheUltimate_ReachesSpacesOutsideTheRealBattleZone()
        {
            var board = DodgerSpawnPointBoard.CreateBoard(
                Dodger, DodgerSpawnPointBoard.ThroneStandingSpace);

            Assert.That(
                EmptyMinionSpawnPointQuery.AnyWithin(
                    board, board, DodgerSpawnPointBoard.ThroneMinionSpawn, 0,
                    SpawnPointOverlay.None),
                Is.False,
                "The Throne Zone's own minion Spawn point is out of the Battle Zone, so it never " +
                "qualifies on the printed board - which is what makes it the right probe.");
            Assert.That(
                EmptyMinionSpawnPointQuery.AnyWithin(
                    board, board, DodgerSpawnPointBoard.ThroneMinionSpawn, 0,
                    SpawnPointOverlay.AllSpacesFriendlyTo(TeamSide.Red)),
                Is.True,
                "With the Ultimate up every space counts as in the Battle Zone, so it does.");
        }

        /// <summary>
        /// The overlay is read off the acting hero's own declared rule and nothing else, which is the
        /// predicate every consumer of the count asks. A hero with no such rule gets the plain board.
        /// </summary>
        [Test]
        public void TheOverlay_IsReadFromTheActingHerosOwnUltimate()
        {
            Assert.That(
                SpawnPointOverlay.ForActor(RuleModificationSet.Empty, null, Dodger).IsActive,
                Is.False,
                "No participants and no modifications is the honest 'off', not a throw.");
            Assert.That(
                SpawnPointOverlay.AllSpacesFriendlyTo(TeamSide.None).IsActive, Is.False,
                "A grant whose whole content is the word 'friendly' cannot be friendly to nobody.");
            Assert.That(
                SpawnPointOverlay.AllSpacesFriendlyTo(TeamSide.Blue).FriendlySide,
                Is.EqualTo(TeamSide.Blue));
        }

        // -----------------------------------------------------------------------------------------
        // RULING 7 and RULING 11 - Darkest Ritual
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// RULING 7, structurally. The two clauses are two conditions in one sequence rather than one
        /// nested inside the other's branch - so the item is gained on having the Ultimate whatever the
        /// spawn-point count says.
        /// </summary>
        [Test]
        public void DarkestRitual_GatesItsTwoClausesIndependently()
        {
            var skill = DodgerCardContent
                .Definition(DodgerCardContent.DarkestRitual)
                .ActionDefinitions
                .Single(action => action.EffectRoot != null);

            var root = skill.EffectRoot.Nodes
                .Single(node => node.Id.Equals(skill.EffectRoot.RootNodeId));

            Assert.That(root.ChildNodeIds.Count, Is.EqualTo(4),
                "Count, gate, Ultimate predicate, gate - four children in one sequence is what makes " +
                "the second clause independent of the first.");

            var gates = skill.EffectRoot.Nodes
                .Where(node => node.Kind == EffectNodeKind.Conditional)
                .ToArray();
            Assert.That(gates.Length, Is.EqualTo(2), "One gate per printed `If`.");
            foreach (var gate in gates)
            {
                Assert.That(gate.ChildNodeIds.Count, Is.EqualTo(1),
                    "RULING 3: an `If X` with no else-child, so a false condition simply skips.");
            }

            var gated = gates.SelectMany(gate => gate.ChildNodeIds).ToArray();
            Assert.That(
                gated.Any(id => id.Value.Contains("gain")) &&
                gated.Any(id => id.Value.Contains("item")),
                Is.True,
                "One gate carries the coins and the other carries the item; neither carries both.");
        }

        /// <summary>
        /// RULING 11, measured. Every performance grants another permanent item, they stack under one
        /// slot, and nothing counts or expires them.
        /// </summary>
        [Test]
        public void DarkestRitualsItem_IsUnboundedAndStacksInTheAttackSlot()
        {
            var configuration = MatchConfiguration.CreateForShowcaseFixture();
            var owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var items = new HeroItemState(configuration.Participants);

            for (var granted = 1; granted <= 5; granted++)
            {
                var placement = items.TryGrantItemFromTheBox(owner, PrintedValueChannel.Attack);

                Assert.That(placement.IsPlaced, Is.True,
                    "p.16 allows several items under one slot and the face prints no limit, so the " +
                    "fifth grant is as legal as the first.");
                Assert.That(placement.Item.Icon, Is.EqualTo(PrintedValueChannel.Attack));
                Assert.That(placement.Item.Order, Is.EqualTo(granted),
                    "Each grant is its own item rather than a repeat of the last.");
                Assert.That(placement.Item.Slot.Index, Is.EqualTo(1),
                    "p.16's matching-icon rule puts an Attack item under slot 1, and the dashboard " +
                    "decides that rather than the card.");
            }

            Assert.That(items.GetItems(owner).Count, Is.EqualTo(5));
            Assert.That(
                items.GetItems(owner).Select(item => item.SourceCardInstanceId.Value).Distinct().Count(),
                Is.EqualTo(5),
                "Each spare card from the box is its own identity, so the restore path's one-card-one-" +
                "item rule holds and a reload rebuilds all five.");
        }

        // -----------------------------------------------------------------------------------------
        // RULING 12 - the Necromancy pair, and the derived Heavy exclusion
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// RULING 12's derivation, proved rather than hard-coded. p.19's count is stated as printed and
        /// asked of every type; a Heavy is ineligible while its miniature stands, and the ruling's whole
        /// point is that a board where it does not stand is not reachable - p.13's immunity means a
        /// Heavy dies only as the last minion of its side, and that death is p.8's Lane-Push cause,
        /// which respawns the wave.
        ///
        /// <para>What this case can prove offline is the arithmetic half: with the wave standing, no
        /// type is eligible; remove a melee and only melee becomes eligible; the Heavy never does
        /// unless its own miniature is off the board, which is the state the ruling says the rules do
        /// not leave you in.</para>
        /// </summary>
        [Test]
        public void PageNineteensCount_NeverFavoursAHeavyWhileItsMiniatureStands()
        {
            var full = DodgerSpawnPointBoard.CreateBoard(
                Dodger,
                DodgerSpawnPointBoard.Origin,
                DodgerSpawnPointBoard.MeleeSpawnNear,
                DodgerSpawnPointBoard.RangedSpawnNear);

            Assert.That(
                MinionRespawnEligibilityQuery.IsTypeEligible(
                    full, TeamSide.Red, StandardMinionType.Heavy, SpawnPointOverlay.None),
                Is.False,
                "Red prints no Heavy Spawn point in this Zone at all, so the count cannot favour one.");

            // The occupier's TYPE is the question here, so it is named rather than defaulted - a Heavy
            // point occupied by the fixture's default Melee miniature would leave the Heavy count
            // reading zero miniatures and answer the opposite of what this case asks.
            var withHeavyStanding = DodgerSpawnPointBoard.CreateBoardWithTypedOccupiers(
                Dodger,
                DodgerSpawnPointBoard.Origin,
                (DodgerSpawnPointBoard.HeavySpawnFar, TeamSide.Blue, StandardMinionType.Heavy));

            Assert.That(
                MinionRespawnEligibilityQuery.IsTypeEligible(
                    withHeavyStanding, TeamSide.Blue, StandardMinionType.Heavy,
                    SpawnPointOverlay.None),
                Is.False,
                "One Blue Heavy Spawn point and one Blue Heavy miniature standing on it: p.19 wants " +
                "strictly MORE points than miniatures, so nothing comes back. This is the state the " +
                "user's derivation says the rules keep you in - p.13's immunity means the Heavy is " +
                "the last of its side to fall, and that fall is p.8's Lane-Push cause.");

            Assert.That(
                MinionRespawnEligibilityQuery.IsTypeEligible(
                    full, TeamSide.Blue, StandardMinionType.Melee, SpawnPointOverlay.None),
                Is.True,
                "The Blue melee point in the Zone is empty and no Blue melee miniature stands, which " +
                "is exactly p.19's 'a minion must first be removed, before it can be respawned'.");
        }

        /// <summary>
        /// A Respawn destination is a printed friendly point of an eligible type in reach, and a point
        /// the Ultimate granted is not one - it names no type, so p.19's count has nothing to compare
        /// and the card's own "the chosen point decides what comes back" has nothing to read.
        /// </summary>
        [Test]
        public void AGrantedSpawnPoint_CountsButHostsNoRespawn()
        {
            var board = DodgerSpawnPointBoard.CreateBoard(Dodger, DodgerSpawnPointBoard.Origin);
            var overlay = SpawnPointOverlay.AllSpacesFriendlyTo(TeamSide.Red);

            var destinations = MinionRespawnEligibilityQuery.FindRespawnDestinationsWithin(
                board, board, TeamSide.Red, DodgerSpawnPointBoard.Origin, 2, overlay);

            Assert.That(destinations, Is.Not.Empty, "Her printed Red points are still destinations.");
            Assert.That(
                destinations.All(fact => fact.IsPrinted), Is.True,
                "Every destination is a point the map prints; the Ultimate's typeless grants are not " +
                "destinations, which is the printed silence recorded rather than a chosen type.");
            Assert.That(
                destinations.All(fact => fact.MinionType != StandardMinionType.None), Is.True,
                "A destination always knows what comes back, which is RULING 12's whole shape.");
            Assert.That(
                destinations.All(fact => fact.TeamSide == TeamSide.Red), Is.True,
                "'friendly' is p.19's word about sides.");
        }

        // -----------------------------------------------------------------------------------------
        // RULING 13 and RULING 14 - Weakness and Enfeeblement
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// RULING 13. Both faces declare a signed contribution on the Attack channel - which is what
        /// puts them on the Attack Total beside p.16's items rather than on the printed value - and
        /// both scope it to the enemy heroes in their own printed disk.
        /// </summary>
        [Test]
        public void WeaknessAndEnfeeblement_DrainTheAttackTotalByTheirPrintedNumbers()
        {
            var cases = new[]
            {
                (Definition: DodgerActiveEffectContent.WeaknessDefinition, Amount: -4),
                (Definition: DodgerActiveEffectContent.EnfeeblementDefinition, Amount: -6)
            };

            foreach (var (definition, amount) in cases)
            {
                Assert.That(
                    definition.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.HeroesInRadius),
                    "'Enemy heroes in radius' is p.19's heroes alone.");
                Assert.That(
                    definition.AffectedUnitRelation, Is.EqualTo(SourceTeamRelation.OpposingSource),
                    "'Enemy', from the card's own perspective.");
                Assert.That(
                    definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn),
                    "p.17's printed 'This turn:' keyword, on the lifecycle Arien's Spell Break, " +
                    "Slippery Ground and Deluge already carry for the identical printed shape.");
                Assert.That(
                    definition.Radius.Value, Is.EqualTo(4),
                    "Both faces print Radius 4.");
                Assert.That(
                    definition.Radius.Source, Is.EqualTo(DeclaredRadiusSource.PrintedIcon),
                    "The sentence says 'in radius' and the face prints the icon, so p.16's Radius " +
                    "item raises it - where Arien's Slippery Ground, whose sentence writes its own " +
                    "number down, is WrittenInText and is never raised.");

                var drain = definition.Modifications
                    .Single(value => value.Kind == RuleModificationKind.ValueModifier);
                Assert.That(drain.Channel, Is.EqualTo(PrintedValueChannel.Attack));
                Assert.That(drain.Amount, Is.EqualTo(amount));
                Assert.That(
                    drain.ValueApplicability, Is.EqualTo(RuleValueApplicability.Always),
                    "Neither face qualifies which Attack it drains, so the declaration qualifies none.");
            }
        }

        /// <summary>
        /// RULING 13's second half, which needed no code: there is no floor at zero in the Attack
        /// arithmetic, so Weakness's reminder - "they can still attack, even with a negative attack
        /// value" - is already true. Asserted through the value model itself rather than by reading the
        /// arithmetic, so a clamp added later fails here.
        /// </summary>
        [Test]
        public void TheAttackTotal_HasNoFloorAtZero()
        {
            var printed = new ValueResult(
                PrintedValueChannel.Attack,
                new[]
                {
                    new ValueContribution(
                        ValueContributionKind.PrintedIcon,
                        new CardInstanceId("probe-card"),
                        new ActionDefinitionId("probe-action"),
                        PrintedValueChannel.Attack,
                        2),
                    new ValueContribution(
                        ValueContributionKind.CardTextModifier,
                        new CardInstanceId("probe-card"),
                        new ActionDefinitionId("probe-action"),
                        PrintedValueChannel.Attack,
                        -6)
                });

            Assert.That(printed.Total, Is.EqualTo(-4),
                "A printed 2 under Enfeeblement's -6 is -4, and the total says so rather than zero.");
        }

        /// <summary>
        /// RULING 14. Enfeeblement's second clause names rulebook p.10's Repeat and nothing wider, so
        /// Fast Travel and the Clear that replaces an Attack stay available - and Weakness, which
        /// prints no such clause, declares no restriction at all.
        /// </summary>
        [Test]
        public void Enfeeblement_ForbidsOnlyThePageTenRepeat()
        {
            var restrictions = DodgerActiveEffectContent.EnfeeblementDefinition.Modifications
                .Where(value => value.Kind == RuleModificationKind.Restriction)
                .ToArray();

            Assert.That(restrictions.Length, Is.EqualTo(1), "One printed clause, one restriction.");
            Assert.That(
                restrictions[0].Activities, Is.EqualTo(new[] { RuleActivity.Repeat }),
                "Only p.10's Repeat keyword - replacement actions remain available.");
            Assert.That(
                restrictions[0].Activities, Has.No.Member(RuleActivity.FastTravel),
                "Fast Travel is a replacement action, which the page keeps distinct from a repeat.");
            Assert.That(
                restrictions[0].Activities, Has.No.Member(RuleActivity.Clear),
                "So is the Clear that substitutes for an Attack.");

            Assert.That(
                DodgerActiveEffectContent.WeaknessDefinition.Modifications
                    .Any(value => value.Kind == RuleModificationKind.Restriction),
                Is.False,
                "Weakness prints the drain and nothing else.");
        }

        /// <summary>
        /// p.10's Repeat is an operation and not an action, which is why it needed a thirteenth
        /// activity rather than reusing one. Asserted through the one bridge between the two
        /// vocabularies: no <see cref="ActionKind"/> maps to it, so nothing a hero performs is
        /// accidentally covered by Enfeeblement's clause.
        /// </summary>
        [Test]
        public void TheRepeatActivity_MapsFromNoActionKind()
        {
            foreach (var actionKind in new[]
            {
                ActionKind.Movement, ActionKind.Attack, ActionKind.Skill, ActionKind.Defense,
                ActionKind.Hold, ActionKind.FastTravel, ActionKind.Clear
            })
            {
                Assert.That(
                    RuleActivityQuery.ForAction(actionKind),
                    Is.Not.EqualTo(RuleActivity.Repeat),
                    actionKind + " is an action; a repeat is p.10's control-flow modifier on one, " +
                    "which is why the restriction cannot be reached by performing anything.");
            }
        }
    }
}
