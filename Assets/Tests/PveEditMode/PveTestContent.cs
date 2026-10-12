using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// The first slice's placeholder enemies and the PVE-D17 layout, stated in code so the
    /// engine-free tests do not depend on a JSON reader. The shipped JSON is checked against the
    /// same layout by the Unity-side content test.
    /// </summary>
    internal static class PveTestContent
    {
        internal static BoardMapDefinition OfficialMap() =>
            OfficialBoardMapCatalog.Load().Load(new MapDefinitionReference(new MapId("official-single-lane"), 1));

        internal static PveEnemyCatalog Catalog() => new PveEnemyCatalog("placeholder-test", new[]
        {
            new PveEnemyType("raider", "Raider", 1,
                new[]
                {
                    new PveOrangeCard("A", "Advance", 9, 1, null, 0, 2, new[]
                    {
                        new PveCardOption(PveOptionKind.Move, PveTargetKind.Hero)
                    }),
                    new PveOrangeCard("B", "Slash", 7, 2, 3, 0, 2, new[]
                    {
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero),
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Minion),
                        new PveCardOption(PveOptionKind.Move, PveTargetKind.Hero)
                    }),
                    new PveOrangeCard("C", "Heavy Blow", 4, 1, 5, 0, null, new[]
                    {
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero),
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Minion)
                    })
                },
                new[] { new PvePurpleCard("P", "Parry", 1, 4) },
                new[]
                {
                    new[] { "A", "B", "C", "B" },
                    new[] { "A", "C", "B", "B" },
                    new[] { "A", "B", "B", "C" }
                }),
            new PveEnemyType("brute", "Brute", 1,
                new[]
                {
                    new PveOrangeCard("A", "Lumber", 6, 1, null, 0, 2, new[]
                    {
                        new PveCardOption(PveOptionKind.Move, PveTargetKind.Hero)
                    }),
                    new PveOrangeCard("B", "Smash", 5, 1, 6, 0, 1, new[]
                    {
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero),
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Minion),
                        new PveCardOption(PveOptionKind.Move, PveTargetKind.Minion)
                    }),
                    new PveOrangeCard("C", "Hurl", 3, 1, 4, 2, 1, new[]
                    {
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero),
                        new PveCardOption(PveOptionKind.Attack, PveTargetKind.Minion),
                        new PveCardOption(PveOptionKind.Move, PveTargetKind.Hero, 2)
                    }),
                    new PveOrangeCard("X", "Dozing Off", 1, 1, null, 0, null, null, isIdle: true)
                },
                new[]
                {
                    new PvePurpleCard("P", "Thick Hide", 1, 3),
                    new PvePurpleCard("Q", "Brace", 1, 5)
                },
                new[]
                {
                    new[] { "A", "B", "X", "C" },
                    new[] { "A", "X", "B", "C" },
                    new[] { "A", "C", "X", "B" }
                })
        });

        /// <summary>The placeholder types with every minion option removed, so the creeps outlive a passive player.</summary>
        internal static PveEnemyCatalog HeroHuntingCatalog() => new PveEnemyCatalog("placeholder-test",
            Catalog().Types.Select(type => new PveEnemyType(type.Id, type.DisplayName, type.Level,
                type.OrangeCards.Select(card =>
                {
                    var options = card.Options.Where(option => option.Target == PveTargetKind.Hero).ToArray();
                    return new PveOrangeCard(card.Key, card.Name, card.Initiative, card.Copies,
                        options.Any(option => option.Kind == PveOptionKind.Attack) ? card.AttackValue : null,
                        card.AttackRange,
                        options.Any(option => option.Kind == PveOptionKind.Move) ? card.MoveValue : null,
                        options, card.IsIdle);
                }),
                type.PurpleCards, type.Plans)));

        internal static PveEncounterDefinition Encounter() => new PveEncounterDefinition(
            "official-single-lane",
            1,
            new[] { "-3,2", "-5,4", "-3,4" }.Select(value =>
                new PveCreepPlacement(Hex(value), StandardMinionType.Melee)),
            new[] { "-4,2", "-2,2", "-5,3", "-2,3", "-4,4" }.Select(Hex),
            new[] { "-1,-2", "1,-1", "-7,2", "0,4", "-9,7", "-7,10" }
                .Select(value => new PveSpawn(1, Hex(value), "raider"))
                .Concat(new[] { "0,-4", "-6,0", "0,6" }
                    .Select(value => new PveSpawn(2, Hex(value), "brute"))));

        internal static HexCoordinate Hex(string value)
        {
            var parts = value.Split(',');
            return new HexCoordinate(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }
}
