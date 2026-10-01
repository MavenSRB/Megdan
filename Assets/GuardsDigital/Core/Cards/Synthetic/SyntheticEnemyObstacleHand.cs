using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>
    /// Milestone 9.32c. Test-only Attacks in the shape of Takahide's Spinning Blade and Blade Helix:
    /// "Target a unit adjacent to you. After the attack: This turn: Empty spaces adjacent to you [in
    /// radius] count as obstacles for the enemy units." Fixture content, not a verified card or a
    /// rule-source interpretation; no Takahide face is registered.
    ///
    /// <para>"Adjacent to you" is a disk the text writes down - one space - and "in radius" is the
    /// printed Radius icon with Radius items on it (R1). Both are drawn around the hero's current
    /// space (p.17) and bar only the enemy's units (TK-Q3).</para>
    /// </summary>
    public static class SyntheticEnemyObstacleHand
    {
        public const string ContentRevision = "9.32c-rev-1";
        public const int AttackValue = 3;
        public const int HelixRadius = 2;

        public static readonly CardDefinitionId BladeId = new CardDefinitionId("synthetic-red-blade");
        public static readonly CardDefinitionId HelixId = new CardDefinitionId("synthetic-red-helix");
        public static readonly ActionDefinitionId PrimaryId = new ActionDefinitionId("primary-attack");

        public static EffectCapabilityReference GrantBladeV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("synthetic-grant-blade-obstacles"), 1);

        public static EffectCapabilityReference GrantHelixV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("synthetic-grant-helix-obstacles"), 1);

        /// <summary>"This turn: Empty spaces adjacent to you count as obstacles for the enemy units."</summary>
        public static ActiveEffectDefinition BladeDefinition { get; } =
            Obstacles("synthetic-blade-obstacles", DeclaredRadius.WrittenInText(1));

        /// <summary>"This turn: Empty spaces in radius count as obstacles for the enemy units."</summary>
        public static ActiveEffectDefinition HelixDefinition { get; } =
            Obstacles("synthetic-helix-obstacles", DeclaredRadius.PrintedIcon(HelixRadius));

        /// <summary>Blade and Helix.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        /// <summary>The two Active effects' creation steps, which a fixture's catalog carries.</summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActiveEffectCreationHandler(GrantBladeV1, BladeDefinition),
                new ActiveEffectCreationHandler(GrantHelixV1, HelixDefinition)
            };

        private static ActiveEffectDefinition Obstacles(string id, DeclaredRadius radius) =>
            new ActiveEffectDefinition(
                id,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false,
                "SyntheticEnemyObstacles",
                id,
                Array.Empty<TimingCheckpoint>(),
                null,
                new[] { ConditionalObstacleOverlay.DeclareForUnits(SourceTeamRelation.OpposingSource) },
                radius);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Attack(BladeId, "TEST - Blade", GrantBladeV1, 4),
                Attack(HelixId, "TEST - Helix", GrantHelixV1, HelixRadius)
            }));

        private static CardDefinition Attack(
            CardDefinitionId id, string name, EffectCapabilityReference grant, int printedRadius) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(11),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        PrimaryId, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, AttackValue),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, printedRadius)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        new EffectDefinition(
                            new EffectDefinitionId(id.Value + "-text"), ContentRevision, new EffectNodeId("this-turn"),
                            new[]
                            {
                                new EffectNodeDefinition(
                                    new EffectNodeId("this-turn"), EffectNodeKind.Capability,
                                    EffectFailurePolicy.Mandatory, grant,
                                    outputs: new[]
                                    {
                                        new EffectOutputBinding(
                                            ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                                    })
                            }),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack)
                });
    }
}
