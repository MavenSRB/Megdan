using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// The bounded synthetic content milestone 8.19c proves its persistent source families against:
    /// multi-activity immunity qualified by actor, a `Next Turn` window, additive effective-type
    /// overlays, dynamic values, and values qualified to the action class they name.
    ///
    /// <para>Each declaration is a roster <em>shape</em> on content that is nobody's card, exactly as
    /// milestone 8.19a's four continuation shapes are. The numbers are the smallest that make the rule
    /// observable and none of them is an official value; the milestone that composes the printed card
    /// supplies those. What is under test here is that a trusted definition can say the thing at all,
    /// that a rule reads it, and that it ends when its source does.</para>
    ///
    /// <para>They are separate registrations rather than one capability with a mode selector, for the
    /// reason milestone 6.8 gave the first five: <see cref="IEffectCapabilityHandler.ActiveEffectDefinitions"/>
    /// means "every definition this capability can create", and a selector would make that list a lie or
    /// smuggle the choice through an untyped payload.</para>
    /// </summary>
    public static class SyntheticPersistentSourceCapabilities
    {
        /// <summary>Brogan's Bulwark shape: four repositioning activities, from enemy heroes only.</summary>
        public static EffectCapabilityId GuardAlliesInRadius { get; } =
            new EffectCapabilityId("synthetic-guard-allies-in-radius");

        /// <summary>Tigerclaw's Blend Into Shadows shape: next turn, immune to enemy Attack.</summary>
        public static EffectCapabilityId ShadowedNextTurn { get; } =
            new EffectCapabilityId("synthetic-shadowed-next-turn");

        /// <summary>Xargatha's Stone Gaze shape: next turn, enemy heroes also count as terrain.</summary>
        public static EffectCapabilityId PetrifyEnemiesInRadius { get; } =
            new EffectCapabilityId("synthetic-petrify-enemies-in-radius");

        /// <summary>Xargatha's Metamorphosis shape, as an Active effect: value per adjacent enemy.</summary>
        public static EffectCapabilityId ScaleWithAdjacentEnemies { get; } =
            new EffectCapabilityId("synthetic-scale-with-adjacent-enemies");

        /// <summary>Sabina's Big Sodding Gun shape: two channels, on a basic Attack only.</summary>
        public static EffectCapabilityId EmpowerBasicAttack { get; } =
            new EffectCapabilityId("synthetic-empower-basic-attack");

        public static EffectCapabilityReference GuardAlliesInRadiusV1 =>
            new EffectCapabilityReference(GuardAlliesInRadius, 1);

        public static EffectCapabilityReference ShadowedNextTurnV1 =>
            new EffectCapabilityReference(ShadowedNextTurn, 1);

        public static EffectCapabilityReference PetrifyEnemiesInRadiusV1 =>
            new EffectCapabilityReference(PetrifyEnemiesInRadius, 1);

        public static EffectCapabilityReference ScaleWithAdjacentEnemiesV1 =>
            new EffectCapabilityReference(ScaleWithAdjacentEnemies, 1);

        public static EffectCapabilityReference EmpowerBasicAttackV1 =>
            new EffectCapabilityReference(EmpowerBasicAttack, 1);

        /// <summary>
        /// p.17's three cancellations for an Active effect on your card, as milestone 6.8 recorded
        /// them: the effect stops applying, the controller is defeated, or the card changes state at
        /// the end of the round. Resolving the source card is deliberately not one of them.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] GeneralCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// `This Turn: friendly units in radius cannot be moved, pushed, swapped or placed by enemy
        /// heroes` - Brogan's Bulwark shape, and the first declaration in this build that names more
        /// than one activity and the actors it protects against.
        ///
        /// The scope is the disk rather than the controller, because that is what makes the two halves
        /// separable: the units protected are decided by the board at the moment a rule is asked, and
        /// the actors barred are decided by the declaration. A hero on the source's own side may still
        /// reposition them, which is the qualification the card prints and the one an unqualified
        /// immunity would silently drop.
        /// </summary>
        public static ActiveEffectDefinition GuardAlliesDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-guard-allies-effect",
            ActiveEffectAffectedScopeKind.UnitsInRadius,
            ActiveEffectDurationKind.ThisTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            GeneralCancellations,
            false,
            "SyntheticGuardAllies",
            "synthetic-guard-allies",
            Array.Empty<TimingCheckpoint>(),
            new[]
            {
                RuleModification.ForImmunity(
                    new[]
                    {
                        RuleActivity.DirectMove,
                        RuleActivity.ObjectPush,
                        RuleActivity.Swap,
                        RuleActivity.Place
                    },
                    RestrictedActorScope.Of(RestrictedActorRelation.HeroesOpposingSource))
            },
            radius: DeclaredRadius.WrittenInText(2),
            affectedUnitRelation: SourceTeamRelation.AlliedWithSource);

        /// <summary>
        /// `Next Turn: you are immune to enemy heroes' Attack actions` - Tigerclaw's Blend Into Shadows
        /// shape. One activity and a named actor class, so the qualification is proven on the narrow
        /// declaration as well as the wide one, and on the delayed window rather than the immediate one.
        /// </summary>
        public static ActiveEffectDefinition ShadowedDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-shadowed-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            GeneralCancellations,
            true,
            "SyntheticShadowed",
            "synthetic-shadowed",
            Array.Empty<TimingCheckpoint>(),
            new[]
            {
                RuleModification.ForImmunity(
                    new[] { RuleActivity.Attack },
                    RestrictedActorScope.Of(RestrictedActorRelation.HeroesOpposingSource))
            });

        /// <summary>
        /// `Next Turn: enemy heroes in radius count as both heroes and terrain, and cannot perform
        /// movement actions` - Xargatha's Stone Gaze shape.
        ///
        /// Both declarations ride one effect because the card prints one sentence, and the overlay is
        /// the half that is new: the affected heroes keep every hero rule - they are still on their
        /// team, still occupy their space as heroes, still take their turns - and gain one answer they
        /// did not have. The restriction beside it is milestone 6.8's ordinary family, and it is here so
        /// the two are proven to end together.
        /// </summary>
        public static ActiveEffectDefinition PetrifyDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-petrify-effect",
            ActiveEffectAffectedScopeKind.UnitsInRadius,
            ActiveEffectDurationKind.NextTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            GeneralCancellations,
            true,
            "SyntheticPetrify",
            "synthetic-petrify",
            Array.Empty<TimingCheckpoint>(),
            new[]
            {
                RuleModification.ForEffectiveTypeOverlay(EffectiveUnitKind.Terrain),
                RuleModification.ForRestriction(RuleActivity.Movement)
            },
            radius: DeclaredRadius.WrittenInText(2),
            affectedUnitRelation: SourceTeamRelation.OpposingSource);

        /// <summary>
        /// `This Round: +1 Movement and +1 Initiative for each enemy unit adjacent to you` - Xargatha's
        /// Metamorphosis shape, declared here on an Active effect so both of its channels can be
        /// carried by one source. The Ultimate form of the same declaration is the synthetic hand's.
        ///
        /// The count is not in the declaration and cannot be: the enemies walk in and out while the
        /// ability stands unchanged, so what the card states is the amount per match and the live set
        /// states the total.
        /// </summary>
        public static ActiveEffectDefinition ScaleWithAdjacentEnemiesDefinition { get; } =
            new ActiveEffectDefinition(
                "synthetic-scale-with-adjacent-enemies-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                GeneralCancellations,
                false,
                "SyntheticScaleWithAdjacentEnemies",
                "synthetic-scale-with-adjacent-enemies",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForDerivedValue(
                        PrintedValueChannel.Movement,
                        SourceValueQueryKind.EnemyUnitsAdjacentToAffectedObject,
                        1,
                        RuleValueApplicability.Always),
                    RuleModification.ForDerivedValue(
                        PrintedValueChannel.Initiative,
                        SourceValueQueryKind.EnemyUnitsAdjacentToAffectedObject,
                        1,
                        RuleValueApplicability.Always)
                });

        /// <summary>
        /// `This Round: your basic attack has +2 Range and +2 Attack` - Sabina's Big Sodding Gun shape.
        ///
        /// The qualification is the point. The same hero's Skill Range, her non-basic attacks and every
        /// other card's totals are untouched, so a consumer that admitted the declaration by channel
        /// alone would hand two extra Attack to cards the sentence does not name.
        /// </summary>
        public static ActiveEffectDefinition EmpowerBasicAttackDefinition { get; } =
            new ActiveEffectDefinition(
                "synthetic-empower-basic-attack-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                GeneralCancellations,
                false,
                "SyntheticEmpowerBasicAttack",
                "synthetic-empower-basic-attack",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForValue(
                        PrintedValueChannel.Attack, 2,
                        RuleValueApplicability.ControllerBasicAttackAction),
                    RuleModification.ForValue(
                        PrintedValueChannel.Range, 2,
                        RuleValueApplicability.ControllerBasicAttackAction)
                });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(GuardAlliesInRadiusV1, GuardAlliesDefinition),
            new ActiveEffectCreationHandler(ShadowedNextTurnV1, ShadowedDefinition),
            new ActiveEffectCreationHandler(PetrifyEnemiesInRadiusV1, PetrifyDefinition),
            new ActiveEffectCreationHandler(ScaleWithAdjacentEnemiesV1, ScaleWithAdjacentEnemiesDefinition),
            new ActiveEffectCreationHandler(EmpowerBasicAttackV1, EmpowerBasicAttackDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            GuardAlliesDefinition,
            ShadowedDefinition,
            PetrifyDefinition,
            ScaleWithAdjacentEnemiesDefinition,
            EmpowerBasicAttackDefinition
        });
    }
}
