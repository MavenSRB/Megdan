using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Derives the permanent triggered sources applicable to one completed basic action. The source
    /// remains card-defined; this query owns p.16's shared on-board lifetime and the trigger fact's
    /// actor/kind match.
    /// </summary>
    public static class UltimateTriggeredEffectSourceQuery
    {
        public static IReadOnlyList<ContinuousTriggeredEffectSource> Derive(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            PerformedBasicAction basicAction,
            PerformedPrimaryAction primaryAction = default,
            PerformedMovementAction movementAction = default)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (!basicAction.IsDefined && !primaryAction.IsDefined && !movementAction.IsDefined)
                return Array.Empty<ContinuousTriggeredEffectSource>();

            return ultimates.Ultimates
                .Where(ultimate =>
                    Applies(ultimate.Rule.TriggerCondition, basicAction, primaryAction, movementAction) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _) &&
                    ultimate.OwnerId.Equals(
                        ActorOf(ultimate.Rule.TriggerCondition, basicAction, primaryAction, movementAction)))
                .Select((ultimate, index) => ultimate.TriggeredSource(participants, index, basicAction))
                .Where(source => source != null)
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.39f1. The derived sources whose turn bound has not already been spent, with the
        /// keys of the ones being armed written into <paramref name="consumedTurnScopedKeys"/>.
        ///
        /// <para><b>"The first time each turn" is a frequency on the trigger, and this is where it is
        /// enforced</b> - the same place and the same way milestone 8.19a enforces it for a
        /// once-per-turn BEFORE-action trigger: a shared per-turn key set that the turn clears and the
        /// snapshot already carries, rather than a new counter anybody has to remember to reset. A turn
        /// performs as many primary actions as its text grants - the printed one, and every one a grant
        /// produces - and Legend of the Skies fires on the first.</para>
        ///
        /// <para><b>Only a condition whose printed sentence bounds it is keyed.</b> Wasp's "each time
        /// after you perform a basic skill", Tigerclaw's, Misa's, Tali's and Swift's print no such
        /// limit or are naturally once a turn, so they are returned untouched and nothing about them is
        /// written into the turn - which is what keeps this from changing five shipped Ultimates.</para>
        ///
        /// <para><b>It is also what stops Cutter's clause looping.</b> The Ultimate grants a borrowed
        /// primary; a borrowed primary IS the performance of a primary action, so the completed grant
        /// comes back through here - and finds the source already spent.</para>
        /// </summary>
        public static IReadOnlyList<ContinuousTriggeredEffectSource> ArmUnspent(
            UnlockedUltimateSet ultimates,
            IReadOnlyList<ContinuousTriggeredEffectSource> derived,
            ICollection<string> consumedTurnScopedKeys)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (derived == null) throw new ArgumentNullException(nameof(derived));
            if (consumedTurnScopedKeys == null)
                throw new ArgumentNullException(nameof(consumedTurnScopedKeys));
            if (derived.Count == 0) return derived;

            var armed = new List<ContinuousTriggeredEffectSource>(derived.Count);
            foreach (var source in derived)
            {
                var key = OncePerTurnKeyFor(ultimates, source);
                if (key == null)
                {
                    armed.Add(source);
                    continue;
                }

                if (consumedTurnScopedKeys.Contains(key)) continue;

                consumedTurnScopedKeys.Add(key);
                armed.Add(source);
            }

            return armed;
        }

        /// <summary>
        /// The turn-scoped key bounding one derived source, or <c>null</c> where the source's own
        /// condition prints no bound. Milestone 8.39f1.
        ///
        /// <para>The key is the source's own stable id - the card instance beside the dashboard plus
        /// the ability's rule id, which a restore reproduces identically - under a prefix of this
        /// milestone's own, so a spent after-action trigger cannot be mistaken for milestone 8.19a's
        /// spent before-action trigger on the same Ultimate, nor for milestone 8.38d's granted-target
        /// exclusions, which share the set.</para>
        /// </summary>
        public static string OncePerTurnKeyFor(
            UnlockedUltimateSet ultimates, ContinuousTriggeredEffectSource source)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (source == null) return null;

            foreach (var ultimate in ultimates.Ultimates)
            {
                if (ultimate.Source.SourceId != source.SourceId) continue;
                return ultimate.Rule.TriggerCondition ==
                    UltimateTriggerCondition.AfterControllerPerformsPrimaryAction
                    ? OncePerTurnKeyPrefix + source.SourceId
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Milestone 8.39f1's own namespace inside the turn's shared consumed-key set.
        /// </summary>
        public const string OncePerTurnKeyPrefix = "after-primary-action:";

        /// <summary>
        /// Milestone 8.39f1. Which of the two completed-action facts one condition reads, and whether
        /// that fact met it.
        ///
        /// <para>The five conditions written before that milestone read
        /// <see cref="PerformedBasicAction"/> and are matched by <see cref="Matches"/> exactly as they
        /// were - an undefined basic fact meets none of them, which is what keeps a non-basic card
        /// reaching no Ultimate that was written before this one. The sixth reads
        /// <see cref="PerformedPrimaryAction"/> instead, and is the only condition in this file that
        /// can be met while the basic fact is absent.</para>
        /// </summary>
        private static bool Applies(
            UltimateTriggerCondition condition,
            PerformedBasicAction basicAction,
            PerformedPrimaryAction primaryAction,
            PerformedMovementAction movementAction) =>
            condition == UltimateTriggerCondition.AfterControllerPerformsMovementAction ? movementAction.IsDefined :
            condition == UltimateTriggerCondition.AfterControllerPerformsPrimaryAction
                ? primaryAction.IsDefined
                : basicAction.IsDefined && Matches(condition, basicAction);

        /// <summary>
        /// The hero whose action it was, read off whichever fact the condition consulted. p.16 gives an
        /// Ultimate to one hero and every one of these sentences says "you", so a source is derived
        /// only for the hero who acted - and asking the fact the condition did not read would compare
        /// against a default id and match nobody.
        /// </summary>
        private static HeroId ActorOf(
            UltimateTriggerCondition condition,
            PerformedBasicAction basicAction,
            PerformedPrimaryAction primaryAction,
            PerformedMovementAction movementAction) =>
            condition == UltimateTriggerCondition.AfterControllerPerformsMovementAction ? movementAction.ActorId :
            condition == UltimateTriggerCondition.AfterControllerPerformsPrimaryAction
                ? primaryAction.ActorId
                : basicAction.ActorId;

        /// <summary>
        /// Whether one declared condition is met by the kind of basic action that just completed.
        ///
        /// An explicit switch rather than a comparison, for the reason
        /// <see cref="GuardsDigital.Core.Rules.Modifications.RuleActivityQuery.ForAction"/> is a switch:
        /// milestone 8.19c added a second condition beside the first, and the two are narrower and
        /// wider readings of the same page. A card that says "basic skill" must keep meaning that after
        /// a card that says "basic action" arrives, and the only way to guarantee it is to state both
        /// answers rather than to test one and let the other fall through.
        /// </summary>
        private static bool Matches(
            UltimateTriggerCondition condition, PerformedBasicAction performed)
        {
            switch (condition)
            {
                case UltimateTriggerCondition.AfterControllerPerformsBasicSkill:
                    return performed.Kind == ActionKind.Skill;
                case UltimateTriggerCondition.AfterControllerPerformsBasicAction:
                    return true;

                // Milestone 8.35z29. The one condition here that reads what the action did rather
                // than which kind it was: a basic Skill of Misa's that moved her five spaces is not a
                // basic Skill of Misa's that placed her, and only the second is what her Ultimate's
                // second paragraph watches.
                case UltimateTriggerCondition.AfterControllerPlacesSelf:
                    return performed.PlacedSelf;

                // Milestone 8.37. Tali's Reign of Winter: a second condition reading a
                // CONSEQUENCE of the completed action, exactly as AfterControllerPlacesSelf does - here
                // an Attack that actually removed a minion, not merely one that was performed.
                case UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack:
                    return performed.Kind == ActionKind.Attack &&
                        performed.DefeatedMinionWithBasicAttack;

                // Milestone 8.38c2. Swift's Bullet Time: the CARD was resolved, which happens once a
                // turn, rather than an action having been performed, which happens as often as text
                // grants one. The kind is not asked at all - a basic card resolved by a Hold is still
                // resolved, and the primary this Ultimate then grants is the card's own.
                case UltimateTriggerCondition.AfterControllerResolvesBasicCard:
                    return performed.WasCardResolution;

                // Milestone 8.39f1. Cutter's Legend of the Skies reads a fact this method is not
                // given - a primary action that need not have come off a basic card - and is answered
                // by Applies above before it reaches here. Stated rather than left to the default, so
                // the switch is exhaustive and a reader is not left wondering whether it was missed.
                case UltimateTriggerCondition.AfterControllerPerformsPrimaryAction:
                    return false;

                // Milestone 8.40h. Bain's A Complicated Profession reads a moment INSIDE a running
                // effect - a marker having been given - so no completed action can meet it and this
                // query must never build a source for it. Stated rather than left to the default for
                // the reason the line above is: the switch stays exhaustive, and the negative is a
                // decision rather than an omission. `AfterMarkerGivenTriggerQuery` is what derives it,
                // off the give's own report; `ContinuousTriggeredEffectSource` would refuse the
                // checkpoint such a rule names, which is the second guard keeping the two apart.
                case UltimateTriggerCondition.AfterControllerGivesMarker:
                case UltimateTriggerCondition.AfterOwnedResolvedCardDiscarded:
                    return false;
                default:
                    return false;
            }
        }
    }
}
