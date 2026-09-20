using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's two "This round: Double your item bonuses" faces, as the p.17 Active
    /// effects they create and the capabilities that create them - milestone 8.61h.
    ///
    /// <para><b>What the sentence is, in engine terms.</b> Rulebook p.16 states the item bonus as a
    /// rule - "Each item increases the value of the matching icon on each of your Action cards by +1"
    /// - and this card changes that rule's number for the rest of the round. It is therefore a
    /// <see cref="RuleModificationKind.ItemBonusMultiplier"/> declaration on a
    /// <see cref="ActiveEffectDurationKind.ThisRound"/> effect scoped to the controller, and it is
    /// nothing else: no value contribution, no channel, no per-card exception. Every consumer of
    /// p.16's bonus reads the multiplier through <c>ItemBonusMultiplierQuery</c>, so the Attack,
    /// Defense, Movement, Range, Radius and Initiative totals, p.7's turn order, an Active effect's
    /// declared Radius and the hero rail's badge all move together without any of them naming this
    /// card.</para>
    ///
    /// <para><b>Two definitions rather than one, because p.10 counts instances per CARD.</b> "Only one
    /// instance of an active effect per card can be active" - the lifecycle suppresses a duplicate by
    /// matching uniqueness key and controller, so a shared key would let Perfect Self create nothing
    /// while Inner Strength's effect stood. The two share one <c>EffectKind</c>, because being under
    /// the doubling is one state however it was granted, and differ in their uniqueness key, which is
    /// the same split Ursafar's twelve enrage grants take.</para>
    ///
    /// <para><b>They can never both stand, and nothing here relies on that.</b> p.15 replaces the
    /// lower Tier when a card is upgraded, so Inner Strength (Green II) and Perfect Self (Green III)
    /// are never in one deck at once. Two declarations would take the LARGEST rather than multiply -
    /// see <see cref="RuleModificationKind.ItemBonusMultiplier"/> - so even the impossible case is a
    /// doubling rather than a quadrupling, and no stacking rule is built for a card nobody
    /// prints.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <c>MatchContentCatalogs</c> names no Min content, so no
    /// configured match can deal these cards and nothing reachable can start these capabilities. That
    /// is milestone 7.9's gate rather than an oversight - the registered capability list is
    /// serialized, and registering a contract before a dealt card can reach it moves the pinned list,
    /// every snapshot's bytes and every corpus digest while the content never moved. Milestone 8.61j
    /// is the block that flips it; until then <see cref="DisciplineDeferredHandlers"/> is what a
    /// fixture composes onto a bounded catalog, and <c>MinItemAndFlurryTests</c> asserts the absence
    /// from both sides.</para>
    /// </summary>
    public static partial class MinComponentContent
    {
        /// <summary>
        /// p.17's cancellation set, less the one the duration already covers. "At the end of the
        /// round" is the <see cref="ActiveEffectDurationKind.ThisRound"/> expiry; the three that remain
        /// are the page's others - the source card changing state, the controller being defeated, and
        /// p.8's end-of-round retrieval, which is a card state change with a checkpoint of its own.
        ///
        /// <para>Resolving the card is deliberately NOT one of them: p.17 has the player turn the card
        /// on its side after performing the action precisely to signal the effect is still running,
        /// and <c>TurnResolutionAuthority</c> does not raise the state-changed checkpoint for an action
        /// completing. Discarding the card, and retrieval at the round's end, still do.</para>
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] DisciplineCancellations =
        {
            ActiveEffectCancellationCondition.SourceCardStateChanged,
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// p.16's own arithmetic, doubled - the whole of what "Double your item bonuses" states, and
        /// the only number either face prints.
        /// </summary>
        public const int DoubledItemBonusMultiplier = 2;

        /// <summary>The capability Inner Strength's Skill performs.</summary>
        public static EffectCapabilityId InnerStrengthDoubleItemBonuses { get; } =
            new EffectCapabilityId("min-inner-strength-double-item-bonuses");

        /// <summary>The capability Perfect Self's first bullet performs.</summary>
        public static EffectCapabilityId PerfectSelfDoubleItemBonuses { get; } =
            new EffectCapabilityId("min-perfect-self-double-item-bonuses");

        public static EffectCapabilityReference InnerStrengthDoubleItemBonusesV1 =>
            new EffectCapabilityReference(InnerStrengthDoubleItemBonuses, 1);

        public static EffectCapabilityReference PerfectSelfDoubleItemBonusesV1 =>
            new EffectCapabilityReference(PerfectSelfDoubleItemBonuses, 1);

        /// <summary>
        /// Inner Strength's "<b>This round:</b> Double your item bonuses."
        ///
        /// <para>A Skill's effect is the action (p.12), so the effect is created and activated at
        /// <see cref="TimingCheckpoint.ActionStarted"/> - the shape Arien's Spell Break takes, and the
        /// one that makes the doubling true for the rest of this very turn rather than only from the
        /// next one. It expires at <see cref="TimingCheckpoint.RoundEnd"/>, which is what p.19's "This
        /// round" says and where p.8 puts the boundary.</para>
        /// </summary>
        public static ActiveEffectDefinition InnerStrengthDoubledItemsDefinition { get; } =
            DoubledItemsDefinition(
                "min-inner-strength-doubled-items", "min-inner-strength-doubled-items");

        /// <summary>
        /// Perfect Self's first bullet - the identical sentence, as its own definition for the reason
        /// the type remarks give: p.10 counts one instance of an active effect per card, so the two
        /// faces need two uniqueness keys.
        /// </summary>
        public static ActiveEffectDefinition PerfectSelfDoubledItemsDefinition { get; } =
            DoubledItemsDefinition(
                "min-perfect-self-doubled-items", "min-perfect-self-doubled-items");

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> DisciplineDefinitions { get; } =
            Array.AsReadOnly(new[]
            {
                InnerStrengthDoubledItemsDefinition,
                PerfectSelfDoubledItemsDefinition
            });

        /// <summary>
        /// Everything this file declares, registered or not, for the offline content fixtures that
        /// compose catalogs of their own.
        ///
        /// <para>The member is named for this block rather than <c>Handlers</c>, because five parallel
        /// partials of <see cref="MinComponentContent"/> are being written at once and one shared name
        /// would be a compile error rather than a merge. Milestone 8.61i folds them into the set's own
        /// pair.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DisciplineHandlers() =>
            DisciplineRegisteredHandlers().Concat(DisciplineDeferredHandlers());

        /// <summary>
        /// The two Active-effect creations her two doubling faces name, registered at milestone 8.61j
        /// because Inner Strength and Perfect Self are cards a configured match now puts in a hand.
        /// Both run on <see cref="ActiveEffectCreationHandler"/>, the type Arien's five persistent
        /// texts proved at milestone 6.8, so neither owed a showcase consumer.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DisciplineRegisteredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActiveEffectCreationHandler(
                    InnerStrengthDoubleItemBonusesV1, InnerStrengthDoubledItemsDefinition),
                new ActiveEffectCreationHandler(
                    PerfectSelfDoubleItemBonusesV1, PerfectSelfDoubledItemsDefinition)
            };

        /// <summary>
        /// Empty, from milestone 8.61j on. The member exists so the fixture that asserts the gate
        /// from both sides asks a question rather than failing to compile, and so a "registered or
        /// deferred" sweep keeps its shape.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DisciplineDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The one sentence both faces print, at the uniqueness key each of them owns. One factory,
        /// because the sentence is identical on both and a reader comparing them should find nothing
        /// to compare.
        /// </summary>
        private static ActiveEffectDefinition DoubledItemsDefinition(
            string definitionId, string uniquenessKey) =>
            new ActiveEffectDefinition(
                definitionId,

                // "YOUR item bonuses" - the hero whose card it is, and nobody else. A disk scope would
                // have been a card that doubled a team-mate's items, which this one does not print.
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                DisciplineCancellations,
                false,

                // One kind for both faces: being under the doubling is one state however it was
                // granted. The uniqueness key beside it is per-card, because p.10 counts instances
                // per card.
                DoubledItemsEffectKind,
                uniquenessKey,
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForItemBonusMultiplier(DoubledItemBonusMultiplier) });

        /// <summary>
        /// The typed kind every reading of the doubling matches on - one string for both faces, for the
        /// reason the factory above states.
        /// </summary>
        public const string DoubledItemsEffectKind = "MinDoubledItemBonuses";
    }
}
