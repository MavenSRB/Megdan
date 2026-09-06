using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Dodger the Warlock's two persistent texts, at milestone
    /// 8.35z28: the Attack drain her Blue Tier II Skill lays over the enemy heroes beside her,
    /// and the same drain deepened with a lock on rulebook p.10's Repeat on her Tier III.
    ///
    /// <para><b>These are declarations, not handlers</b>, exactly as the five earlier heroes' are.
    /// Both register through <see cref="ActiveEffectCreationHandler"/> - the one capability shape whose
    /// whole content is an Active-effect definition - and everything either card does is stated in the
    /// definition. No card name is read at runtime and nothing here asks the board a question.</para>
    ///
    /// <para><b>Why they are Active effects.</b> Both faces print p.17's "<b>This turn:</b>" keyword,
    /// and both scope their sentence to "Enemy heroes in radius" - which is the printed shape Arien's
    /// Spell Break, Slippery Ground and Deluge already carry. The lifecycle is theirs unchanged and
    /// deliberately so: four faces across two heroes print one sentence structure, and a hero who gave
    /// it a second reading would be the two-locally-correct-readings defect rather than a new card.</para>
    ///
    /// <para><b>Both disks are printed icons.</b> Each face prints Radius 4 and the sentence says "in
    /// radius", which p.9 makes the icon's own value - so p.16's matching Radius item raises both, and
    /// neither is <see cref="DeclaredRadius.WrittenInText"/>.</para>
    ///
    /// <para><b>What the numbers reduce, and what they do not stop at.</b> The user's 2026-09-06 ruling
    /// on D-08 settles the layer: the -4 and -6 are signed contributions to the <em>Attack Total</em>,
    /// beside p.16's items and p.14's minion modifiers, rather than reductions of the printed value on
    /// the enemy's card. And the same ruling records what Weakness's own reminder says outright -
    /// "<em>( They can still attack, even with a negative attack value. )</em>" - that there is no floor
    /// at zero. Nothing had to be built for that half: the Attack arithmetic carries no clamp anywhere,
    /// which was measured rather than assumed at this milestone.</para>
    /// </summary>
    public static class DodgerActiveEffectContent
    {
        /// <summary>
        /// Rulebook p.17's cancellations for an Active effect on your own card, as the six earlier
        /// heroes declare them: you are defeated, or the card changes state at the end of the round.
        /// Resolving the source card is deliberately absent - p.17 turns the card on its side
        /// <em>after</em> the action precisely to signal the effect is still running - and each
        /// duration's expiration checkpoint ends it normally.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId WeakenEnemyHeroesInRadius { get; } =
            new EffectCapabilityId("dodger-weaken-enemy-heroes-in-radius");

        public static EffectCapabilityId EnfeebleEnemyHeroesInRadius { get; } =
            new EffectCapabilityId("dodger-enfeeble-enemy-heroes-in-radius");

        public static EffectCapabilityReference WeakenEnemyHeroesInRadiusV1 =>
            new EffectCapabilityReference(WeakenEnemyHeroesInRadius, 1);

        public static EffectCapabilityReference EnfeebleEnemyHeroesInRadiusV1 =>
            new EffectCapabilityReference(EnfeebleEnemyHeroesInRadius, 1);

        /// <summary>
        /// Weakness's Skill: "<b>This turn:</b> Enemy heroes in radius have -4 [Attack] Attack.
        /// <em>( They can still attack, even with a negative attack value. )</em>"
        ///
        /// <para><b>Two printed clauses, one declaration and one reminder.</b> "Enemy heroes in radius"
        /// is the affected scope - <see cref="ActiveEffectAffectedScopeKind.HeroesInRadius"/> opposing
        /// the source. "-4 Attack" is the modification. The parenthesised sentence is p.9's reminder
        /// text and composes nothing: it clarifies a consequence of the first two rather than adding a
        /// rule, and the engine already had no floor for it to remove.</para>
        ///
        /// <para><b>The asymmetry with Enfeeblement is recorded and means nothing.</b> This face prints
        /// the no-floor reminder and Enfeeblement does not, despite its -6 making a negative total far
        /// likelier. Under the standing ruling that reminder text is clarification only, so nothing is
        /// read into the omission and both cards reach the same arithmetic.</para>
        ///
        /// <para>A Skill's effect is the action (p.12), so it is created and activated at
        /// <see cref="TimingCheckpoint.ActionStarted"/>.</para>
        /// </summary>
        public static ActiveEffectDefinition WeaknessDefinition { get; } =
            new ActiveEffectDefinition(
                "dodger-weakness-effect",
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "DodgerWeakness",
                "dodger-weakness",
                Array.Empty<TimingCheckpoint>(),
                new[] { DrainedAttack(4) },
                null,
                DeclaredRadius.PrintedIcon(4),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// Enfeeblement's Skill: "<b>This turn:</b> Enemy heroes in radius have -6 [Attack] Attack and
        /// cannot repeat actions."
        ///
        /// <para><b>Weakness's sentence with a deeper number and a second clause.</b> The scope, the
        /// disk and the lifecycle are identical; what this face adds is the restriction.</para>
        ///
        /// <para><b>"cannot repeat actions" is rulebook p.10's Repeat and nothing wider</b> - the user's
        /// 2026-09-06 ruling on D-09. p.10 defines Repeat as a named keyword ("After you perform this
        /// action, you may perform the entire action again"), and the page keeps replacement actions
        /// deliberately distinct from it, so a hero under this effect may still Fast Travel and may
        /// still take the Clear that substitutes for an Attack. <see cref="RuleActivity.Repeat"/> is
        /// that keyword named, and the two gates inside <c>CardActionResolutionAuthority</c> consult it
        /// so the repeat is never <em>offered</em> rather than refused after the answer arrives.</para>
        ///
        /// <para><b>The actors are unqualified for the reason Arien's Spell Break records.</b> The
        /// sentence says who is affected - enemy heroes - and the scope is what says it; the
        /// restriction is on those heroes repeating their own actions, which is
        /// <see cref="RestrictedActorScope.AnyActor"/>'s reading of an affected object acting for
        /// itself. Nobody else could repeat another hero's action, so a qualification would narrow
        /// nothing and read as though it might.</para>
        /// </summary>
        public static ActiveEffectDefinition EnfeeblementDefinition { get; } =
            new ActiveEffectDefinition(
                "dodger-enfeeblement-effect",
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "DodgerEnfeeblement",
                "dodger-enfeeblement",
                Array.Empty<TimingCheckpoint>(),
                new[] { DrainedAttack(6), RuleModification.ForRestriction(RuleActivity.Repeat) },
                null,
                DeclaredRadius.PrintedIcon(4),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// "-N [Attack] Attack", stated once because both faces print the same clause with a different
        /// number and a second copy could drift from the first.
        ///
        /// <para>It is a plain signed value contribution, which is what puts it on the Attack Total
        /// beside p.16's items rather than on the printed value of whatever card the enemy plays - the
        /// user's D-08 ruling, and the only shape in this vocabulary that can carry a negative amount
        /// at all: a roster card-text contribution is required to be positive, and this is not one.</para>
        ///
        /// <para><see cref="RuleValueApplicability.Always"/> because neither face qualifies which Attack
        /// it drains. Arien's duelling immunity narrows by actor and Sabina's Ultimate narrows to a
        /// basic Attack; these two say "have -N Attack" with no qualification at all, so the
        /// declaration says none.</para>
        /// </summary>
        private static RuleModification DrainedAttack(int amount) =>
            RuleModification.ForValue(PrintedValueChannel.Attack, -amount);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(WeakenEnemyHeroesInRadiusV1, WeaknessDefinition),
            new ActiveEffectCreationHandler(EnfeebleEnemyHeroesInRadiusV1, EnfeeblementDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            WeaknessDefinition,
            EnfeeblementDefinition
        });
    }
}
