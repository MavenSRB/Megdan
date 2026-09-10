using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class CardActionResolutionAuthority
    {
        private readonly MovementCardActionStrategyBackend movementBackend;
        private readonly AttackDefenseCardActionStrategyBackend attackDefenseBackend;
        private readonly FastTravelCardActionStrategyBackend fastTravelBackend;
        private readonly ClearCardActionStrategyBackend clearBackend;
        private readonly SkillCardActionStrategyBackend skillBackend;
        private readonly List<PendingResolution> borrowedSkillParents = new List<PendingResolution>();
        public bool HasBorrowedSkillParent => borrowedSkillParents.Count > 0;
        private readonly TriggeredEffectResolutionAuthority triggeredEffects;
        private readonly MatchComponents components;
        private readonly AdjacentMoveAuthority board;
        private EffectRosterState rosterState = EffectRosterState.Absent;

        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority)
            : this(catalogs, adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, null)
        {
        }

        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions)
            : this(catalogs, adjacentMoveAuthority, movementActionStartRestrictions, null)
        {
        }

        /// <summary>
        /// The configured match's shape: default movement restrictions, plus the marker register the
        /// round authority also holds. The default evaluator is internal to this assembly, so the
        /// application layer composes a match through this overload rather than naming it.
        /// </summary>
        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers)
            : this(catalogs, adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, markers)
        {
        }

        /// <summary>
        /// Milestone 7.4. A dispatcher composed without a round authority, stating the two states
        /// rulebook p.13's hero-Defeat consequences read and write.
        ///
        /// A configured match never uses this: there the round owns both, is composed after this
        /// dispatcher, and binds them down. This exists for the compositions that have no round at all,
        /// and it takes them at construction rather than through a setter so such a composition states
        /// what a Defeat is priced against once and cannot silently exchange it later. A dispatcher
        /// built through any other overload and never bound refuses to resolve a hero Defeat rather
        /// than pricing one against Levels and counters nobody records.
        /// </summary>
        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers,
            GuardsDigital.Core.Rules.Progression.HeroProgressionState heroLevels,
            GuardsDigital.Core.Rules.Consequences.TeamLifeState teamLives)
            : this(catalogs, adjacentMoveAuthority, UnrestrictedMovementActionStartEvaluator.Instance, markers)
        {
            if (heroLevels == null) throw new ArgumentNullException(nameof(heroLevels));
            if (teamLives == null) throw new ArgumentNullException(nameof(teamLives));
            BindDefeatConsequenceState(heroLevels, teamLives);
        }

        public CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator movementActionStartRestrictions,
            MarkerAttachmentRegister markers)
            : this(
                catalogs ?? throw new ArgumentNullException(nameof(catalogs)),
                new MovementCardActionStrategyBackend(
                    adjacentMoveAuthority,
                    movementActionStartRestrictions ?? throw new ArgumentNullException(
                        nameof(movementActionStartRestrictions))),
                new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, null, markers),
                new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                new SkillCardActionStrategyBackend(catalogs.Capabilities),
                CreateComponents(catalogs, adjacentMoveAuthority, markers),
                adjacentMoveAuthority)
        {
        }

        private PendingDisplacedMinionCorrection pendingCorrection;
        private PendingRepeatedAction pendingRepeat;

        /// <summary>
        /// Milestone 8.23. The completed basic action a standing after-action ability may repeat, and
        /// the repeat it has already asked for. They are two fields rather than one with a flag
        /// because they are two states of the match: a decision that has not been answered, and an
        /// instance that is owed and has not started.
        /// </summary>
        private PendingAfterActionRepeat repeatSubject;

        private PendingAfterActionRepeat owedAfterActionRepeat;

        /// <summary>
        /// The unit a repeated Attack may not take again, when the ability that asked printed the
        /// exclusion. Milestone 8.23; Tigerclaw's Cloak and Daggers is what prints it.
        /// </summary>
        private AttackTarget owedAfterActionRepeatExclusion;
        private PendingImmediateLanePushRequest pendingLanePush;
        private PendingAttackCardTextResolution pendingCardText;

        /// <summary>
        /// Milestone 8.22. The rest of rulebook p.14, owed while an after-push trigger that the card
        /// text set off is standing. Null whenever the page is not stopped that way.
        /// </summary>
        private PendingAttackCardTextRemainder pendingCardTextAwaitingInterrupt;
        private PendingAttackResolution owedAttackRepeat;
        private AttackTarget owedAttackRepeatExclusion;

        // Milestone 8.35z50. The card a completed card text asked to borrow a primary action from, and
        // the hero who will perform it. Ephemeral by design and deliberately NOT persisted: a borrow is
        // owed only across the boundary between the frame that asked and the instance that answers,
        // which is the same window `owedAttackRepeat` above lives in.
        private CardInstanceId owedBorrowedActionCardId;
        private HeroId owedBorrowedActionActorId;

        /// <summary>
        /// Milestone 8.19a's outer pre-action continuation, or null. It is held beside the card-action
        /// continuation rather than as one of its kinds because the two genuinely coexist: work the
        /// actor placed after a Movement waits while that Movement runs with a continuation of its own.
        /// </summary>
        private PendingPreActionResolution pendingPreAction;

        /// <summary>
        /// Milestone 8.19c1's after-push trigger frame, or null. It is held beside the card-action
        /// continuation for the reason the pre-action one is, and the reason is sharper: the effect it
        /// interrupted is still standing in its own backend, at the node it had reached.
        /// </summary>
        private GuardsDigital.Core.Rules.EffectResolution.PendingEffectInterrupt pendingEffectInterrupt;
        private readonly List<ResolutionStep> triggeredResolutionSteps = new List<ResolutionStep>();

        private CardActionResolutionAuthority(
            MatchContentCatalogs catalogs,
            MovementCardActionStrategyBackend movementBackend,
            AttackDefenseCardActionStrategyBackend attackDefenseBackend,
            FastTravelCardActionStrategyBackend fastTravelBackend,
            ClearCardActionStrategyBackend clearBackend,
            SkillCardActionStrategyBackend skillBackend,
            MatchComponents components,
            AdjacentMoveAuthority board)
        {
            ContentCatalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
            this.movementBackend = movementBackend ?? throw new ArgumentNullException(nameof(movementBackend));
            this.attackDefenseBackend = attackDefenseBackend ?? throw new ArgumentNullException(nameof(attackDefenseBackend));
            this.fastTravelBackend = fastTravelBackend ?? throw new ArgumentNullException(nameof(fastTravelBackend));
            this.clearBackend = clearBackend ?? throw new ArgumentNullException(nameof(clearBackend));
            this.skillBackend = skillBackend ?? throw new ArgumentNullException(nameof(skillBackend));
            this.components = components ?? throw new ArgumentNullException(nameof(components));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            triggeredEffects = new TriggeredEffectResolutionAuthority(
                catalogs.TriggeredEffects, catalogs.Capabilities, components);
        }

        /// <summary>
        /// The component collaborators a Skill's capabilities may reach. The board doubles as the token
        /// placement port, because tokens are board occupants and the board owns the one occupancy index
        /// milestone 6.1r established, and since milestone 6.4 it is also the displacement port, since
        /// milestone 6.5 the unit Spawn port, and since milestone 6.6 the exchange port, for the same
        /// reason: every one of them mutates the
        /// same occupancy, and that is the same authority's business. They stay named ports on one
        /// object rather than one wide one, because placing from a supply, displacing something already
        /// placed, and taking a miniature out of the supply are different rules questions. The marker
        /// register arrives from the caller rather than being created here whenever a live match is
        /// composing, so that the register this dispatcher hands to capabilities and the register the
        /// round authority persists are the same object.
        /// </summary>
        private static MatchComponents CreateComponents(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            new MatchComponents(
                catalogs.Components,
                adjacentMoveAuthority,
                markers ?? new MarkerAttachmentRegister(),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                adjacentMoveAuthority);

        /// <summary>
        /// The same collaborators, for the snapshot reader that has to re-ask a paused capability what
        /// it offered. Exposed rather than duplicated so a restored choice is derived against exactly
        /// the composition a live one was.
        /// </summary>
        internal static MatchComponents CreateRestoredComponents(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            MarkerAttachmentRegister markers) =>
            CreateComponents(catalogs, adjacentMoveAuthority, markers);

        /// <summary>
        /// Milestone 8.3. The trusted content this dispatcher was composed with, exposed so the write
        /// path can record what a match actually ran on instead of asking the build a second time. It
        /// is the same object every backend and every capability here holds.
        /// </summary>
        public MatchContentCatalogs ContentCatalogs { get; }

        public PendingCardActionResolution PendingResolution => movementBackend.PendingResolution;

        public PendingMovementChoice PendingChoice => movementBackend.PendingChoice;

        public PendingAttackResolution PendingAttackResolution => attackDefenseBackend.PendingResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => attackDefenseBackend.PendingTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => attackDefenseBackend.PendingDefenseChoice;

        public HeroCoinLedger CoinLedger => attackDefenseBackend.CoinLedger;

        /// <summary>
        /// Milestone 7.5. The match's one coin ledger, created on first ask. The round authority asks
        /// when it composes itself, because p.15's level purchase now spends from the same balances
        /// p.13's Defeat fills and two ledgers would be two answers to what a hero can afford.
        /// </summary>
        internal HeroCoinLedger EnsureCoinLedger(MatchParticipants participants) =>
            attackDefenseBackend.EnsureCoinLedger(participants);

        /// <summary>
        /// Milestone 8.35z17. The same single creation point, given the balances a staged
        /// situation starts from. The seed is consulted only when no ledger exists yet, so this can
        /// never replace balances a match has already moved - and calling it with a null seed is
        /// the overload above, byte for byte.
        /// </summary>
        internal HeroCoinLedger EnsureCoinLedger(
            MatchParticipants participants, HeroCoinLedgerRestoreState seed) =>
            attackDefenseBackend.EnsureCoinLedger(participants, seed);

        /// <summary>
        /// Milestone 7.2. Hands the backends the match's item state, which the round authority owns.
        /// It is a binding rather than a constructor argument because the dispatcher is composed before
        /// the round that owns the state, and the one thing that must not happen is a second copy: a
        /// Defense total derived from items other than the ones the match records would be a number no
        /// restore or replay could reproduce.
        /// </summary>
        internal void BindHeroItems(GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
        {
            attackDefenseBackend.BindHeroItems(heroItems);

            // Milestone 7.2a. The Movement backend joins it, so an item raises the Movement Total that
            // gates the action and bounds its reachability through the same state and the same query.
            movementBackend.BindHeroItems(heroItems);

            // Milestone 7.2b. The Skill backend joins them, so a capability reading its card's printed
            // Radius composes the same items into the same total every other channel does.
            skillBackend.BindHeroItems(heroItems);
        }

        /// <summary>
        /// Milestone 7.4. Hands the Attack backend the two states rulebook p.13's hero-Defeat
        /// consequences read and write: every hero's Level and every team's Life counters. Only that
        /// backend needs them, because a hero Defeat can only be produced by an Attack - p.19's Remove
        /// is explicitly not a Defeat and "Do not collect any coins" - so binding them more widely would
        /// hand two other backends state no rule of theirs reads.
        /// </summary>
        internal void BindDefeatConsequenceState(
            GuardsDigital.Core.Rules.Progression.HeroProgressionState levels,
            GuardsDigital.Core.Rules.Consequences.TeamLifeState lives)
        {
            attackDefenseBackend.BindDefeatConsequenceState(levels, lives);
        }

        /// <summary>
        /// Hands the Attack backend the round's typed hero-Defeat history. The round owns and persists
        /// it; the backend is merely the one p.13 path that can append a completed outcome.
        /// </summary>
        internal void BindHeroDefeatHistory(
            GuardsDigital.Core.Rules.Consequences.RoundHeroDefeatHistory history)
        {
            attackDefenseBackend.BindHeroDefeatHistory(history);
        }

        /// <summary>
        /// Binds the round's live roster authorities to every effect-entry altitude. The dispatcher is
        /// composed before the round, so this follows the same one-authority binding used for items,
        /// Defeat history and coin accounting.
        /// </summary>
        internal void BindRosterState(EffectRosterState state)
        {
            rosterState = state ?? throw new ArgumentNullException(nameof(state));
            attackDefenseBackend.BindRosterState(rosterState);
            skillBackend.BindRosterState(rosterState);
            triggeredEffects.BindRosterState(rosterState);
        }

        internal AdjacentMoveAuthority Board => board;

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            triggeredEffects.PendingResolution ?? skillBackend.PendingResolution;

        internal bool RemovalPreventionInterruptsSkill => skillBackend.PendingResolution != null &&
            attackDefenseBackend.PendingRemovalPrevention?.Origin == RemovalPreventionOrigin.EffectInvokedMinionDefeat &&
            skillBackend.PendingResolution.ResolutionId.Value == attackDefenseBackend.PendingRemovalPrevention.Resolution.ResolutionId.Value;

        public bool IsPendingTriggeredEffect => triggeredEffects.PendingResolution != null;

        /// <summary>
        /// The question an effect frame is waiting on, whichever frame is holding it. Milestone 8.10
        /// added the second source: card text applied inside rulebook p.14 pauses on the same frame a
        /// Skill does, so a reader that already knew how to draw and answer one needs nothing new to
        /// draw and answer the other, and a match can only ever be holding one of the two.
        /// </summary>
        public GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice PendingEffectChoice =>
            pendingCardText?.Frame.PendingChoice ??
            pendingPreAction?.Frame?.PendingChoice ??
            pendingEffectInterrupt?.Frame.PendingChoice ??
            triggeredEffects.PendingResolution?.PendingChoice ??
            skillBackend.PendingChoice;

        /// <summary>
        /// Milestone 8.19a's outer pre-action continuation, or null. A reader that already knew how to
        /// draw and answer a paused effect frame needs nothing new for this one - which is the point:
        /// the new pause altitudes reach the player through the same projection every other choice does.
        /// </summary>
        public PendingPreActionResolution PendingPreActionResolution => pendingPreAction;

        /// <summary>
        /// Milestone 8.19c1's after-push trigger frame, or null. Like the pre-action continuation, a
        /// reader that can already draw a paused effect frame needs nothing new for it.
        /// </summary>
        public GuardsDigital.Core.Rules.EffectResolution.PendingEffectInterrupt PendingEffectInterrupt =>
            pendingEffectInterrupt;

        /// <summary>
        /// Milestone 6.5c: what each space-valued candidate of the pending effect choice currently
        /// means on the board. This dispatcher answers it because it is the one component holding both
        /// the paused frame and the component composition capabilities execute against, so the preview
        /// a reader sees is derived from exactly the state the next command would resolve against.
        /// Empty whenever no effect choice is pending or none of its candidates name a space.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
            DescribePendingEffectChoice(
                IEnumerable<CardDefinition> definitions,
                RuleModificationSet modifications)
        {
            var pending = pendingCardText?.Frame ?? pendingPreAction?.Frame ??
                triggeredEffects.PendingResolution ??
                skillBackend.PendingResolution;
            var choice = pending?.PendingChoice;
            if (pending == null || choice == null)
            {
                return Array.Empty<
                    GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>();
            }

            var materialized = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));

            // Milestone 8.20a. Which catalog holds the frame's effect is a question about the effect,
            // not about which backend happens to be holding the pause. Keying it on the triggered
            // backend left Arien's Living Tsunami - a p.16 Ultimate whose "before performing an Attack
            // action" trigger stops the game as a PRE-ACTION frame while its effect lives in the
            // triggered catalog - describing no candidates at all, so its one-space step reached the
            // player as four anonymous `OPTION #n` controls instead of four lit spaces on the board.
            var effectRoot =
                ContentCatalogs.TriggeredEffects.TryGet(pending.EffectDefinitionReference, out var triggered)
                    ? triggered
                    : SkillCardActionStrategyBackend.FindEffectRoot(materialized, pending);
            return GuardsDigital.Core.Rules.EffectResolution.EffectChoicePreviewQuery.Describe(
                effectRoot,
                choice,
                pending.ActorId,
                components,
                modifications);
        }

        /// <summary>
        /// Milestone 6.4a's paused after-action correction, or <c>null</c>. It is dispatcher-owned
        /// rather than backend-owned because the correction belongs to the action boundary itself:
        /// every displacement-capable operation the Skill backend hosts - and every later one - reaches
        /// it through the same completion path, which is the reuse the roadmap requires.
        /// </summary>
        public PendingDisplacedMinionCorrection PendingDisplacedMinionCorrection => pendingCorrection;

        /// <summary>
        /// Milestone 6.7's owed second action instance, or <c>null</c>. Non-null only while the
        /// correction between the two instances is paused on a team choice: a correction that needs no
        /// choice starts the instance inside the same command, so there is nothing to hold.
        /// </summary>
        public PendingRepeatedAction PendingRepeatedAction => pendingRepeat;

        /// <summary>
        /// Milestone 6.9's mid-turn Lane-Push request, or <c>null</c>. It is dispatcher-owned for the
        /// reason the correction is: p.8's push is a consequence of the action boundary rather than of
        /// any one backend, and both operations the rulebook allows to cause one - p.19's standalone
        /// Remove inside a Skill and p.13's Defeat inside an Attack - reach this object.
        ///
        /// Nothing here consumes it. The turn refuses to hand Initiative on while it is set, which is
        /// the whole of this milestone's stop; milestones 6.10 and 6.11 own the transition that clears
        /// it.
        /// </summary>
        public PendingImmediateLanePushRequest PendingImmediateLanePush => pendingLanePush;

        /// <summary>
        /// Milestone 8.10's Attack stopped inside rulebook p.14's card text, or <c>null</c>. Non-null
        /// only while the text applied at one of the page's moments is waiting on an answer; text that
        /// asks nothing finishes inside the command that reached it, exactly as it did before.
        /// </summary>
        public PendingAttackCardTextResolution PendingCardTextResolution => pendingCardText;

        /// <summary>
        /// Whether rulebook p.14 is stopped part-way through - because its own text asked a question,
        /// or because milestone 8.22 let a Push inside that text set off a trigger that did. Either
        /// way the steps below the one being applied are not carried anywhere: they are regenerated
        /// from the continuation.s own identity when the answer arrives.
        /// </summary>
        public bool IsCardTextPageStopped =>
            pendingCardText != null || pendingCardTextAwaitingInterrupt != null;

        /// <summary>The remainder owed while a card-text push trigger stands, or null.</summary>
        public PendingAttackCardTextRemainder PendingCardTextRemainder =>
            pendingCardTextAwaitingInterrupt;

        internal IReadOnlyList<ActiveEffectLifecycleEvent> ApplyTriggeredActionEnded(
            ActiveEffectLifecycle timing,
            ActiveEffectCheckpointContext checkpoint,
            TieBreakerState tieBreaker,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IEnumerable<ContinuousTriggeredEffectSource> continuousSources = null,
            PerformedBasicAction basicAction = default,
            AttackTarget completedAttackTarget = default)
        {
            // Milestone 8.23. The subject is held before the trigger runs, because the trigger may open
            // a decision and the action it is about has already finished by then. It is cleared again
            // the moment the trigger's frame completes without asking for it, which is the ordinary
            // case and every case before this milestone.
            repeatSubject = PendingAfterActionRepeat.For(
                basicAction, RepeatResolutionIdFor(basicAction), completedAttackTarget);
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var events = triggeredEffects.ApplyCheckpoint(
                timing, checkpoint, tieBreaker, materializedDefinitions, materializedInstances,
                participants, modifications, skillBackend.HeroItems, continuousSources,
                // Milestone 8.25e. The ability asks this before it offers, and asks it with the
                // binding its own node declares - the exclusion Cloak and Daggers prints is part of
                // what the instance it offers would have to satisfy. The board is the one this
                // checkpoint is standing on, because it is the board the second instance would start
                // from.
                binding => OwedRepeatCanBePerformed(
                    binding, materializedDefinitions, materializedInstances, participants,
                    modifications, checkpoint.RoundIndex, checkpoint.TurnIndex, timing));
            SettleAfterActionRepeat(triggeredEffects.LastRepeatRequested);
            return events;
        }

        /// <summary>
        /// Milestone 8.25e. Whether the repeat this checkpoint's held subject stands for could be
        /// performed, asked before it is offered rather than discovered after it is taken.
        ///
        /// <para><b>The defect this closes, in one sentence: the offer admitted strictly more than the
        /// answer could perform.</b> The ability decided from
        /// <see cref="PerformedBasicAction.CanBeRepeated"/> alone - a fact about the action that
        /// ended - while whether a second instance can start is a fact about which kinds this
        /// dispatcher covers and about the board as it now stands. Both halves are answered here, and
        /// each kind is answered by the resolver that would start it rather than by a rule restated in
        /// this method.</para>
        ///
        /// <para>p.12's Hold is the one kind that is always performable: performing a Hold is doing
        /// nothing, and doing nothing needs nothing from the board. p.11's Fast Travel and p.13's
        /// Clear are the two the page will not repeat at all, which
        /// <see cref="PerformedBasicAction.KindCanBeRepeated"/> states and the subject above has
        /// already applied.</para>
        /// </summary>
        private bool OwedRepeatCanBePerformed(
            RepeatTargetBinding binding,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex,
            ActiveEffectLifecycle timing)
        {
            var owed = repeatSubject;
            if (owed == null)
            {
                return false;
            }

            // Milestone 8.35z28. Dodger's Enfeeblement, "cannot repeat actions" - the user's
            // 2026-09-06 ruling on D-09 scopes that to p.10's Repeat keyword alone. It is asked here
            // rather than at the moment the answer arrives so the offer is never made, which is the
            // offer-not-reject discipline milestone 6.8 set for the Fast-Travel restriction and 8.35l
            // re-learned the hard way.
            if (RepeatRestrictionQuery.IsRestrictedFromRepeating(
                    owed.ActorId, modifications ?? RuleModificationSet.Empty))
            {
                return false;
            }

            if (owed.Kind == ActionKind.Hold)
            {
                // p.12: "When you perform a Hold action you simply do nothing." There is no board
                // condition on doing nothing, so a second one can always be performed - and it is an
                // action performed, so it raises p.8's checkpoints of its own. It is answered before
                // the lookup below because a Hold is a general-rule action: its definition is p.12's
                // own, and the card it is performed on prints no action for it to be found under.
                return true;
            }

            var card = instances.FirstOrDefault(
                candidate => candidate.Id.Equals(owed.SourceCardInstanceId));
            var definition = card == null
                ? null
                : definitions.FirstOrDefault(
                    candidate => candidate.Reference.Equals(card.DefinitionReference));
            var action = definition?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(owed.ActionDefinitionId));
            if (action == null)
            {
                return false;
            }

            switch (owed.Kind)
            {
                case ActionKind.Attack:
                    return attackDefenseBackend.CanStartRepeatedAttack(
                        RepeatedAttackSubjectFor(owed, definition),
                        owed.ResolutionId,
                        ExclusionFor(binding, owed),
                        definitions, instances, participants, modifications);

                case ActionKind.Skill:
                    // The Skill resolver refuses only an action it cannot find an effect for; a Skill
                    // whose text finds nothing on the board still runs and simply applies nothing,
                    // which is the action performed rather than an offer that should not have been
                    // made.
                    return action.EffectRoot != null;

                case ActionKind.Movement:
                    return movementBackend.CanStartRepeatedMovement(
                        RepeatedOfferFor(owed, definition, action), definitions, instances,
                        modifications);

                default:
                    return false;
            }
        }

        /// <summary>
        /// The unit an owed repeat may not take, read off the ability that asked for it. Milestone
        /// 8.25e extracted it so the offer and the instance apply the one rule: the offer has to know
        /// what the instance it is offering would be barred from, or it is not offering that instance.
        /// </summary>
        private static AttackTarget ExclusionFor(
            RepeatTargetBinding binding, PendingAfterActionRepeat subject) =>
            (binding == RepeatTargetBinding.DifferentTarget ||
                binding == RepeatTargetBinding.DifferentEnemyHero ||
                binding == RepeatTargetBinding.DifferentEnemyMinion) &&
            subject.Kind == ActionKind.Attack
                ? subject.CompletedAttackTarget
                : default;

        /// <summary>The completed instance a repeated Attack is rebuilt from. Milestone 8.25e.</summary>
        private static PendingAttackResolution RepeatedAttackSubjectFor(
            PendingAfterActionRepeat owed, CardDefinition definition) =>
            new PendingAttackResolution(
                owed.ResolutionId,
                owed.SourceCardInstanceId,
                definition.Reference,
                owed.ActionDefinitionId,
                owed.ActorId,
                ActionInvocationContext.Repeated,
                new ActionResolutionStepId("attack-target"),
                AttackResolutionPhase.ChooseTarget,
                default(AttackTarget),
                null);

        /// <summary>The offer a repeated instance runs under. Milestone 8.25e.</summary>
        private static ActionOffer RepeatedOfferFor(
            PendingAfterActionRepeat owed, CardDefinition definition, ActionDefinition action) =>
            new ActionOffer(
                owed.SourceCardInstanceId,
                definition.Reference,
                owed.ActionDefinitionId,
                owed.ActorId,
                ActionInvocationContext.Repeated,
                true,
                action.AttackRange,
                action.SourceCardStateTransition);

        /// <summary>
        /// The identity the repeated instance runs under, derived from the action it repeats rather
        /// than minted from a counter, so a replay of the same accepted commands produces the same one.
        /// </summary>
        private static ActionResolutionId RepeatResolutionIdFor(PerformedBasicAction basicAction) =>
            basicAction.IsDefined
                ? new ActionResolutionId(
                    $"after-action-repeat:{basicAction.SourceCardInstanceId.Value}:" +
                    $"{basicAction.ActionDefinitionId.Value}")
                : default;

        /// <summary>
        /// Milestone 8.23. Keeps the held subject exactly while the ability that may repeat it is
        /// standing, and turns it into an owed repeat when the ability asked for one.
        ///
        /// <para>A trigger still holding a question keeps the subject: the decision is what the subject
        /// exists for. A trigger that finished without requesting drops it, which is both a decline and
        /// the case of an ability that never mentioned a repeat at all.</para>
        /// </summary>
        private void SettleAfterActionRepeat(RepeatRequest? requested)
        {
            if (triggeredEffects.PendingResolution != null)
            {
                return;
            }

            if (requested.HasValue && requested.Value.IsRequested && repeatSubject != null)
            {
                owedAfterActionRepeat = repeatSubject;

                // Tigerclaw's Cloak and Daggers prints "if you repeat an attack action, you cannot
                // target the same unit", which arrives as the requesting node's own declared binding.
                // It is read off the ability rather than assumed, so an ability printing no such clause
                // repeats freely - and it applies to an Attack alone, because no other action kind the
                // page repeats has a target to exclude. Since milestone 8.25e the offer reads it
                // through the same helper, because an offer that did not know it would be offering a
                // different instance than the one the answer performs.
                owedAfterActionRepeatExclusion =
                    ExclusionFor(requested.Value.Binding, repeatSubject);
            }

            repeatSubject = null;
        }

        /// <summary>Whether a permanent after-action ability has asked for a repeat that has not started.</summary>
        public bool HasOwedAfterActionRepeat => owedAfterActionRepeat != null;

        /// <summary>The owed repeat, or null. Milestone 8.23.</summary>
        public PendingAfterActionRepeat OwedAfterActionRepeat => owedAfterActionRepeat;

        /// <summary>
        /// Milestone 8.25e. Hands the owed repeat to the caller that will perform it and clears it
        /// here, for the one kind no resolver in this dispatcher holds: p.12's Hold is raised by the
        /// turn authority, so its second instance is performed there too.
        ///
        /// <para>It clears exactly as <see cref="StartOwedAfterActionRepeat"/> does, so a repeat is
        /// consumed once whichever of the two performs it.</para>
        /// </summary>
        public PendingAfterActionRepeat TakeOwedAfterActionRepeat()
        {
            var owed = owedAfterActionRepeat ??
                throw new InvalidOperationException("No after-action repeat is owed.");
            owedAfterActionRepeat = null;
            owedAfterActionRepeatExclusion = default;
            return owed;
        }

        /// <summary>The subject a standing after-action ability may still ask to repeat, or null.</summary>
        public PendingAfterActionRepeat AfterActionRepeatSubject => repeatSubject;

        /// <summary>
        /// Milestone 8.25a. Settles the held subject from wherever the trigger frame it belongs to
        /// finished, rather than only from the effect choice that is the commonest way to finish one.
        ///
        /// <para>The subject exists for exactly as long as the ability that may repeat the action is
        /// standing, and milestone 8.21's removal-prevention answer is the other command that can
        /// finish such an ability: a triggered effect that asked for a Defeat stops at p.19's
        /// prevention offer and completes when it is answered. Left unsettled there, the subject
        /// outlives its trigger - and, now that the turn holds its card while one is held, would hold
        /// that card forever. It is a no-op while the frame is still standing, which is what makes it
        /// safe to call at any boundary that might have finished one.</para>
        /// </summary>
        public void SettleStandingAfterActionRepeat() =>
            SettleAfterActionRepeat(triggeredEffects.LastRepeatRequested);

        internal EffectResolutionStepResult AnswerTriggeredEffectChoice(
            EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var answered = CompleteTriggeredConsequences(
                triggeredEffects.Answer(
                    command, timing, materializedDefinitions, materializedInstances, participants,
                    modifications, roundIndex, turnIndex, skillBackend.HeroItems),
                participants,
                modifications,
                materializedDefinitions,
                materializedInstances);

            // Milestone 8.23. The answer is where a `may` repeat is decided, so it is where the held
            // subject becomes an owed instance or is dropped.
            SettleAfterActionRepeat(triggeredEffects.LastRepeatRequested);
            return answered;
        }

        internal IReadOnlyList<ResolutionStep> ConsumeTriggeredResolutionSteps()
        {
            var result = triggeredResolutionSteps.ToArray();
            triggeredResolutionSteps.Clear();
            return Array.AsReadOnly(result);
        }

        private EffectResolutionStepResult CompleteTriggeredConsequences(
            EffectResolutionStepResult step,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            if (!step.Accepted || !step.IsComplete || step.RequestedMinionDefeats.Count == 0)
                return step;

            var source = triggeredEffects.LastCompletedResolution ??
                throw new InvalidOperationException("A completed triggered Defeat lost its source identity.");
            foreach (var minionId in step.RequestedMinionDefeats)
            {
                var defeat = attackDefenseBackend.ResolveEffectInvokedMinionDefeat(
                    new ActionResolutionId(source.ResolutionId.Value),
                    source.SourceCardInstanceId,
                    source.SourceDefinitionReference,
                    source.ActionDefinitionId,
                    source.ActorId,
                    minionId,
                    participants,
                    modifications,
                    definitions,
                    instances,
                    out var eventIdentity);

                // Milestone 8.19c1. A prevention source is offering and its controller has not
                // answered. The Defeat's remaining facts and p.8's cause are owed to that answer, which
                // is why they are produced there rather than here; a further Defeat this effect asked
                // for waits behind it, because a second removal stage cannot stand inside the first.
                if (defeat == null)
                {
                    triggeredResolutionSteps.Add(ResolutionStep.FromFact(
                        CardActionResolutionEvent.RemovalPreventionRequested(
                            eventIdentity, attackDefenseBackend.PendingRemovalPrevention)));
                    return step;
                }

                triggeredResolutionSteps.AddRange(defeat.Facts.Select(fact =>
                    ResolutionStep.FromFact(CardActionResolutionEvent.MinionDefeatFact(
                        eventIdentity, defeat, fact))));
                RecordImmediateLanePush(
                    defeat.LanePush.Cause,
                    new ActionResolutionId(source.ResolutionId.Value),
                    participants,
                    modifications);
            }

            return step;
        }

        /// <summary>
        /// Milestone 8.10. Whether an Attack's own card text asked for that Attack to happen again -
        /// Thunder Boomerang's "may repeat once on a different target" - and the instance that asked
        /// has finished the page.
        ///
        /// <para>It is not persisted, and does not need to be: the request is recorded and consumed
        /// inside one command, between the card text completing and the turn deciding what to do with a
        /// finished Attack. Every pause the repeated instance then opens is the ordinary Attack's own,
        /// which already crosses the wire.</para>
        /// </summary>
        public bool HasOwedAttackRepeat => owedAttackRepeat != null;

        /// <summary>
        /// The modifications in effect right now, derived from the supplied timing state and this
        /// authority's board. Built per command rather than cached: an Active effect can expire, a
        /// token can be removed, and milestone 7.3's hero can leave the board between two commands, and
        /// a cached set would answer for the wrong moment.
        ///
        /// The unlocked Ultimates arrive from the caller rather than being derived here, because they
        /// are read off the match's card instances and this authority holds none; the callers that do
        /// pass what they hold.
        ///
        /// Milestone 8.9's items go the other way, and for the mirror-image reason: the match's one
        /// item state is already bound into this authority's backends, so taking it from a caller
        /// would be the second copy <see cref="BindHeroItems"/> exists to prevent.
        /// </summary>
        public RuleModificationSet CurrentModifications(
            ActiveEffectLifecycle timing,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates,
            RuleObject actingObject) =>
            RuleModificationQuery.Build(
                timing, board, participants, tieBreaker, ultimates, actingObject, skillBackend.HeroItems,
                // Milestone 8.23. The marker register goes the same way milestone 8.9's items do and
                // for the same reason: this authority already holds the one register a live match
                // gives markers into, so taking it from a caller would be the second copy
                // CreateComponents exists to prevent.
                components.Markers);

        public bool HasPendingResolution =>
            movementBackend.HasPendingResolution || attackDefenseBackend.HasPendingResolution ||
            skillBackend.HasPendingResolution || triggeredEffects.PendingResolution != null ||
            pendingCorrection != null || pendingRepeat != null ||
            // Milestone 8.19a. A pre-action continuation counts as pending exactly while it is holding
            // a question. One with no frame is work the actor has placed and the turn has not entered
            // yet - and in the `after the action` case it is deliberately parked behind the very action
            // that is about to start, so counting it would make the action refuse itself.
            pendingCardText != null || pendingPreAction?.Frame != null ||
            // Milestone 8.19c1. A standing after-push trigger holds the whole action: the effect that
            // pushed is waiting at the node it reached, and nothing else may be commanded until the
            // trigger finishes. A Defeat standing at its removal stage holds it for the same reason -
            // the miniature is still on the board and the page is not finished with it.
            pendingEffectInterrupt != null ||
            attackDefenseBackend.PendingRemovalPrevention != null;

        public CardActionResolutionResult StartTurnSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Skill,
                true);
            if (prepared.RejectionReason.HasValue)
            {
                return CardActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingResolution,
                    PendingChoice);
            }

            var boundary = new ActionBoundaryContext(
                prepared.Definitions, prepared.Instances, participants, timing, tieBreaker,
                roundIndex, turnIndex);
            var result = skillBackend.StartTurnSkill(
                resolutionId, offer, prepared.Definitions, prepared.Instances, participants,
                timing, modifications, components, roundIndex, turnIndex, out var repeat,
                out var lanePushCause, out var appliedPushes, out var requestedDefeats,
                out var borrowRequested);
            RecordOwedBorrowedAction(borrowRequested, offer.ActorId);
            result = WithSkillRequestedHeroDefeats(
                result, requestedDefeats, resolutionId, offer.SourceCardInstanceId,
                offer.ActionDefinitionId, offer.ActorId, boundary);
            result = RunSkillConsequences(
                result, appliedPushes, boundary, ref repeat, ref lanePushCause);
            return CloseActionBoundary(
                result, repeat, lanePushCause, resolutionId, offer.SourceCardInstanceId,
                offer.ActionDefinitionId, offer.ActorId, boundary);
        }

        /// <summary>
        /// Milestone 6.5af: whether starting this offer's Skill could apply anything. The components
        /// this authority owns are what make the question answerable at all, which is why it is asked
        /// here rather than in the card-state-only offer query.
        /// </summary>
        public bool CanSkillApplyAnything(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex) =>
            CanSkillApplyAnything(
                offer, definitions, instances, participants, timing, modifications,
                roundIndex, turnIndex, out _);

        /// <summary>
        /// Milestone 8.35l: the same question, keeping the standing modification that answered it.
        /// </summary>
        public bool CanSkillApplyAnything(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex,
            out AppliedRuleModification blocking) =>
            skillBackend.CanApplyAnything(
                offer, definitions, instances, participants, timing, modifications, components,
                roundIndex, turnIndex, out blocking);

        /// <summary>
        /// Milestone 7.8a1: whether an Attack offer has a legal target, asked of the same backend that
        /// builds the target set when the Attack starts. The counterpart of
        /// <see cref="CanSkillApplyAnything"/>, and true for anything that is not an Attack.
        /// </summary>
        public bool CanAttackFindTarget(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            attackDefenseBackend.CanFindLegalTarget(offer, definitions, participants, modifications);

        /// <summary>
        /// Milestone 8.29d: the complete availability question for a live turn. Ordinary Attacks keep
        /// using the target backend's exact candidate query; an Attack whose mandatory card text binds
        /// its target instead probes that typed effect path without starting or mutating a frame.
        /// </summary>
        public bool CanAttackFindTarget(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex) =>
            CanAttackFindTarget(
                offer, definitions, instances, participants, timing, modifications,
                roundIndex, turnIndex, out _);

        /// <summary>
        /// Milestone 8.35z42: the same answer, plus the standing modification behind a refusal
        /// where the walk met one.
        ///
        /// <para>The channel milestone 8.35l opened for a Skill, on the other kind of offer that greys.
        /// It is <c>null</c> for every Attack refused by the ordinary target backend and for every
        /// card-text Attack whose text is empty for a reason no standing rule explains - a board with
        /// nobody in reach greys exactly as it did and says nothing new.</para>
        /// </summary>
        public bool CanAttackFindTarget(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex,
            out AppliedRuleModification blocking)
        {
            blocking = null;
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            if (!ActionOfferQuery.TryFindActionDefinition(
                    offer, materializedDefinitions, out var action) ||
                action.Kind != ActionKind.Attack ||
                !(action.AttackTargetPolicy?.TargetIsBoundByCardText ?? false))
            {
                return attackDefenseBackend.CanFindLegalTarget(
                    offer, materializedDefinitions, participants, modifications);
            }

            var cardText = action.CardTextAt(CardTextTiming.BeforeTheAttack);
            if (cardText == null)
            {
                return false;
            }

            // Milestone 8.35z27. A modal Attack's text binds through a plan rather than a
            // unit, so the walk below can say only that a binding is reachable. What it may reach is
            // this: every bullet's printed sentence is literal content on its own node, so the same
            // target query each sequence will use can be asked of each sentence now. An Attack whose
            // every bullet reaches nobody is greyed here, which is 8.35z15's rule - judge the offer
            // against what the answer could perform - applied to the modal shape.
            var declaredBullets = RequestedAttackSequenceQuery.Declared(cardText);
            if (declaredBullets.Count > 0 &&
                !attackDefenseBackend.CanAnyDeclaredBulletFindATarget(
                    offer, declaredBullets, materializedDefinitions, participants, modifications,
                    instances))
            {
                return false;
            }

            var context = new EffectResolutionContext(
                cardText,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                ActionKind.Attack,
                components: components,
                heroItems: skillBackend.HeroItems,
                rosterState: rosterState);
            return EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding(
                EffectResolutionSession.BuildStartFrame(
                    new EffectResolutionId(
                        $"probe-attack-target-{offer.SourceCardInstanceId.Value}-" +
                        offer.ActionDefinitionId.Value),
                    offer.SourceCardInstanceId,
                    offer.SourceDefinitionReference,
                    offer.ActionDefinitionId,
                    offer.ActorId,
                    offer.ActorId,
                    offer.InvocationContext,
                    context),
                context,
                out blocking);
        }

        public CardActionResolutionResult AnswerEffectChoice(
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            // Read the owning action's identity before the answer: a completing answer clears the
            // frame, and the correction facts still have to name the action they close.
            var frame = skillBackend.PendingResolution;
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var result = skillBackend.AnswerEffectChoice(
                command, materializedDefinitions, materializedInstances, participants, timing,
                modifications, components, roundIndex, turnIndex, out var repeat, out var lanePushCause,
                out var appliedPushes, out var requestedDefeats, out var skillBorrow);
            if (frame == null)
            {
                return result;
            }

            // Milestone 8.35z50. Angry Roar and his two borrowing Greens all pause HERE - the borrow
            // node asks which card, and this is where that answer arrives - so this is the site that
            // actually produces a borrow for every face in this build that prints one.
            RecordOwedBorrowedAction(skillBorrow, frame.ActorId);

            var boundary = new ActionBoundaryContext(
                materializedDefinitions, materializedInstances, participants, timing, tieBreaker,
                roundIndex, turnIndex);
            result = WithSkillRequestedHeroDefeats(
                result, requestedDefeats, new ActionResolutionId(frame.ResolutionId.Value),
                frame.SourceCardInstanceId, frame.ActionDefinitionId, frame.ActorId, boundary);
            result = RunSkillConsequences(
                result, appliedPushes, boundary, ref repeat, ref lanePushCause);
            return CloseActionBoundary(
                result, repeat, lanePushCause, new ActionResolutionId(frame.ResolutionId.Value),
                frame.SourceCardInstanceId, frame.ActionDefinitionId, frame.ActorId, boundary);
        }

        /// <summary>
        /// Answers the paused after-action correction. Everything is validated against a fresh
        /// derivation from the live board rather than the stored candidates - the same discipline the
        /// Minion Battle answer applies - so a stale or tampered answer is refused and the pending
        /// choice survives untouched.
        /// </summary>
        public CardActionResolutionResult AnswerDisplacedMinionCorrection(
            DisplacedMinionCorrectionCommand command,
            ActionBoundaryContext boundary)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var participants = boundary.Participants ??
                throw new ArgumentException("A correction answer requires the match participants.", nameof(boundary));
            var tieBreaker = boundary.TieBreaker;
            if (pendingCorrection == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingCorrection, null, null);
            }

            if (!command.ResolutionId.Equals(pendingCorrection.ResolutionId) ||
                !command.ChoiceId.Equals(pendingCorrection.ChoiceId) ||
                command.ChoosingSide != pendingCorrection.ChoosingSide)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.StaleCorrectionChoice, null, null);
            }

            var pending = pendingCorrection;
            var steps = new List<ResolutionStep>();
            if (pending.Stage == DisplacedMinionCorrectionStage.SelectSubject)
            {
                if (command.Kind != DisplacedMinionCorrectionCommandKind.SelectSubject)
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                var derived = DisplacedMinionCorrectionPlanner.DeriveNext(
                    board, participants, tieBreaker, pending.ResolutionId,
                    pending.SourceCardInstanceId, pending.ActionDefinitionId, pending.ActorId);
                if (derived.Kind != DisplacedMinionCorrectionStepKind.Choice ||
                    derived.Pending.Stage != DisplacedMinionCorrectionStage.SelectSubject ||
                    !derived.Pending.SubjectCandidates.Any(subject =>
                        subject.MinionId.Equals(command.MinionId)))
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                var subject = board.Minions.Single(minion => minion.MinionId.Equals(command.MinionId));
                var zoneCoordinates = BattleZoneQuery.FindCurrentBattleZoneCoordinates(board);
                var destinationStep = DisplacedMinionCorrectionPlanner.DeriveDestinationStep(
                    board, participants, pending.BattleZoneId, zoneCoordinates, subject,
                    pending.ResolutionId, pending.SourceCardInstanceId,
                    pending.ActionDefinitionId, pending.ActorId);
                if (destinationStep.Kind == DisplacedMinionCorrectionStepKind.Choice)
                {
                    pendingCorrection = destinationStep.Pending;
                    steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                        CardActionResolutionEventKind.DisplacedMinionCorrectionChoiceRequested,
                        pending.ResolutionId, pending.SourceCardInstanceId, pending.ActionDefinitionId,
                        pending.ActorId, destinationStep.Pending.ChoiceId.Value)));
                    return CardActionResolutionResult.Accept(false, null, null, steps);
                }

                CommitCorrection(steps, pending, destinationStep);
            }
            else
            {
                if (command.Kind != DisplacedMinionCorrectionCommandKind.SelectDestination ||
                    !command.MinionId.Equals(pending.SubjectMinionId))
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                var subject = board.Minions.SingleOrDefault(minion =>
                    minion.MinionId.Equals(pending.SubjectMinionId));
                var zoneCoordinates = BattleZoneQuery.FindCurrentBattleZoneCoordinates(board);
                var candidates = subject.MinionId.IsDefined &&
                    !BattleZoneQuery.IsInCurrentBattleZone(board, subject.OccupiedSpace.SpaceId)
                        ? DisplacedMinionCorrectionPlanner.DeriveCandidates(
                            board, zoneCoordinates, subject, out var kind)
                        : Array.Empty<DisplacedMinionCorrectionCandidate>();
                var chosen = candidates.Where(candidate =>
                    candidate.Destination.Equals(command.Destination)).ToArray();
                if (chosen.Length != 1)
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.IllegalCorrectionChoice, null, null);
                }

                CommitCorrection(
                    steps, pending,
                    DisplacedMinionCorrectionStep.Apply(subject, pending.Kind, chosen[0]));
            }

            pendingCorrection = null;
            var complete = AppendCorrectionSteps(
                steps, pending.ResolutionId, pending.SourceCardInstanceId, pending.ActionDefinitionId,
                pending.ActorId, participants, tieBreaker, true);
            return complete
                ? StartOwedRepeat(steps, boundary)
                : CardActionResolutionResult.Accept(false, null, null, steps);
        }

        /// <summary>
        /// Milestone 6.7: closes one action instance's boundary, and starts the next instance when this
        /// one asked for it.
        ///
        /// The order is the acceptance check: the instance's own effect nodes finish, p.18's
        /// after-action correction runs to completion, and only then does a repeated instance begin.
        /// Nothing starts while the correction is paused - the request is held instead, and the pause
        /// is what the next command answers.
        ///
        /// A still-resolving action is returned untouched: an instance that has not completed cannot
        /// have requested anything, because the request is read from the completing frame.
        /// </summary>
        private CardActionResolutionResult CloseActionBoundary(
            CardActionResolutionResult result,
            PendingRepeatedAction repeat,
            LanePushCauseKind lanePushCause,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionBoundaryContext boundary)
        {
            if (!result.Accepted || !result.IsComplete)
            {
                return result;
            }

            // Milestone 6.9: p.8 pushes the Lane immediately, so the request is minted here - before
            // p.18's after-action correction runs - rather than once the boundary is finally clean. A
            // minion the correction returns to the Battle Zone was not standing there when the last one
            // was removed, and reading the board after it would answer a question about a later moment.
            RecordImmediateLanePush(
                lanePushCause, resolutionId, boundary.Participants,
                CurrentModifications(
                    boundary.Timing,
                    boundary.Participants,
                    boundary.TieBreaker,
                    GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        boundary.Definitions, boundary.Instances),
                    RuleObject.ForHero(actorId)));

            pendingRepeat = repeat;
            var steps = new List<ResolutionStep>(result.Steps);
            var complete = AppendCorrectionSteps(
                steps, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                boundary.Participants, boundary.TieBreaker, false);
            if (!complete)
            {
                return CardActionResolutionResult.Accept(false, null, null, steps);
            }

            if (pendingRepeat == null)
            {
                return steps.Count == result.Steps.Count
                    ? result
                    : CardActionResolutionResult.Accept(true, null, null, steps);
            }

            return StartOwedRepeat(steps, boundary);
        }

        /// <summary>
        /// Starts the instance a completed action asked for, once its boundary is clean, and closes
        /// that instance's own boundary in turn.
        ///
        /// The recursion is bounded by the rule itself rather than by a guard here: each instance
        /// carries how many repeats its chain has already run, and the Repeat node it reaches is
        /// eligible only while that count is below the maximum the node declares - so the chain ends by
        /// the last instance reporting no request, exactly as milestone 6.7's single repeat did when
        /// the maximum was always one.
        /// </summary>
        private CardActionResolutionResult StartOwedRepeat(
            List<ResolutionStep> steps,
            ActionBoundaryContext boundary)
        {
            var repeat = pendingRepeat;
            if (repeat == null)
            {
                return CardActionResolutionResult.Accept(true, null, null, steps);
            }

            pendingRepeat = null;
            var repeated = skillBackend.StartRepeatedSkill(
                repeat.ResolutionId,
                repeat,
                boundary.Definitions,
                boundary.Instances,
                boundary.Participants,
                boundary.Timing,
                CurrentModifications(
                    boundary.Timing,
                    boundary.Participants,
                    boundary.TieBreaker,
                    GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        boundary.Definitions, boundary.Instances),
                    RuleObject.ForHero(repeat.ActorId)),
                components,
                boundary.RoundIndex,
                boundary.TurnIndex,
                out var furtherRepeat,
                out var repeatedLanePushCause,
                out var repeatedPushes,
                out var repeatedDefeats,
                out _);
            if (!repeated.Accepted)
            {
                throw new InvalidOperationException(
                    "A repeated action instance could not start from the definition its own action " +
                    "just resolved from; the trusted definitions and the recorded repeat disagree.");
            }

            repeated = WithSkillRequestedHeroDefeats(
                repeated, repeatedDefeats, repeat.ResolutionId, repeat.SourceCardInstanceId,
                repeat.ActionDefinitionId, repeat.ActorId, boundary);
            repeated = RunSkillConsequences(
                repeated, repeatedPushes, boundary, ref furtherRepeat, ref repeatedLanePushCause);
            steps.AddRange(repeated.Steps);
            return CloseActionBoundary(
                CardActionResolutionResult.Accept(repeated.IsComplete, null, null, steps),
                furtherRepeat,
                repeatedLanePushCause,
                repeat.ResolutionId,
                repeat.SourceCardInstanceId,
                repeat.ActionDefinitionId,
                repeat.ActorId,
                boundary);
        }

        /// <summary>
        /// Applies corrections until the board is clean, a team must decide, or only uncorrectable
        /// strays remain. Every iteration re-derives from the live board, because an earlier correction
        /// can free or block a later one's shortest path.
        /// </summary>
        private bool AppendCorrectionSteps(
            List<ResolutionStep> steps,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            bool correctionAlreadyStarted)
        {
            var emitted = correctionAlreadyStarted;
            while (true)
            {
                var step = DisplacedMinionCorrectionPlanner.DeriveNext(
                    board, participants, tieBreaker, resolutionId, sourceCardInstanceId,
                    actionDefinitionId, actorId);
                switch (step.Kind)
                {
                    case DisplacedMinionCorrectionStepKind.Apply:
                        EmitStartedOnce(steps, ref emitted, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
                        CommitCorrection(steps, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId, step);
                        continue;

                    case DisplacedMinionCorrectionStepKind.Choice:
                        EmitStartedOnce(steps, ref emitted, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
                        pendingCorrection = step.Pending;
                        steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                            CardActionResolutionEventKind.DisplacedMinionCorrectionChoiceRequested,
                            resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                            step.Pending.ChoiceId.Value)));
                        return false;

                    default:
                        if (step.LeftInPlace.Count > 0)
                        {
                            EmitStartedOnce(steps, ref emitted, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
                            foreach (var stranded in step.LeftInPlace)
                            {
                                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                                    CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace,
                                    resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                                    $"correction-left-{stranded.MinionId.Value}",
                                    new DisplacedMinionCorrectionEvent(
                                        stranded.MinionId, DisplacedMinionCorrectionKind.None,
                                        stranded.Coordinate, stranded.Coordinate))));
                            }
                        }

                        if (emitted)
                        {
                            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                                CardActionResolutionEventKind.DisplacedMinionCorrectionCompleted,
                                resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                                "correction-complete")));
                        }

                        return true;
                }
            }
        }

        private void EmitStartedOnce(
            List<ResolutionStep> steps,
            ref bool emitted,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId)
        {
            if (emitted)
            {
                return;
            }

            emitted = true;
            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                CardActionResolutionEventKind.DisplacedMinionCorrectionStarted,
                resolutionId, sourceCardInstanceId, actionDefinitionId, actorId, "correction-start")));
        }

        private void CommitCorrection(
            List<ResolutionStep> steps,
            PendingDisplacedMinionCorrection pending,
            DisplacedMinionCorrectionStep step) =>
            CommitCorrection(
                steps, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.ActionDefinitionId, pending.ActorId, step);

        private void CommitCorrection(
            List<ResolutionStep> steps,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            DisplacedMinionCorrectionStep step)
        {
            var origin = step.Subject.Coordinate;
            var subject = BoardObjectReference.ForMinion(step.Subject.MinionId);

            // p.18 gives the correction two different operations, so this commits through two: a
            // shortest-path return is a relocation, and the no-path fallback is a Place. Since
            // milestone 6.5 the fallback is the shared general Place rather than a minimum path of its
            // own, which is what makes the eligible piece kind, the destination question, and the
            // different-space requirement one implementation with every other Place.
            var accepted = step.CorrectionKind == DisplacedMinionCorrectionKind.Place
                ? PlaceOperation.PlaceFromBoard(
                    DisplacedMinionCorrectionPlanner.FallbackPlacePolicy,
                    board,
                    board,
                    subject,
                    origin,
                    step.Destination.Destination,
                    BattleZoneQuery.FindCurrentBattleZoneCoordinates(board),
                    // The planner derived this destination with no modifications in effect, for the
                    // reason recorded there; the commit asks the same question the same way, or the
                    // two would disagree about the same state.
                    RuleModificationSet.Empty).Accepted
                : board.CommitValidatedDisplacement(subject, step.Destination.Destination).Accepted;
            if (!accepted)
            {
                throw new InvalidOperationException(
                    "A derived correction destination could not be committed; the planner and the " +
                    "board disagree about the same state.");
            }

            var kind = step.CorrectionKind == DisplacedMinionCorrectionKind.Place
                ? CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced
                : CardActionResolutionEventKind.DisplacedMinionCorrectionMoved;
            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.DisplacedMinionCorrection(
                kind, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                $"correction-{step.Subject.MinionId.Value}",
                new DisplacedMinionCorrectionEvent(
                    step.Subject.MinionId, step.CorrectionKind, origin, step.Destination.Destination))));
        }

        public CardActionResolutionRestoreState CaptureRestoreState() =>
            // Milestone 8.19a. The outer pre-action continuation is captured beside whichever
            // continuation the action itself left, never instead of it: work the actor placed after a
            // Movement is parked while that Movement is still mid-route, and a capture that chose
            // between them would lose one of the two.
            // Milestone 8.19c1. Its two pauses are captured the same way and for the same reason: an
            // after-push trigger stands inside a continuation that is still there, and a Defeat at its
            // removal stage stands beside whatever the Defeat's own path left behind.
            CaptureContinuation()
                .WithPreAction(pendingPreAction)
                .WithInterruptions(
                    pendingEffectInterrupt, attackDefenseBackend.PendingRemovalPrevention)
                // Milestone 8.22. The rest of p.14, owed while a Push inside its card text has set off
                // a trigger that is standing. It rides whatever continuation the capture found, for
                // the reason the Lane-Push request does.
                .WithCardTextRemainder(pendingCardTextAwaitingInterrupt)
                .WithAfterActionRepeatSubject(repeatSubject)
                // Milestone 8.35z21. A repeat instance that has been decided and not yet started, which
                // p.8's sidebar Place can now stand in the middle of; see the restore state's own
                // account of why this stopped being a within-command fact.
                .WithOwedRepeats(
                    owedAttackRepeat,
                    owedAttackRepeatExclusion,
                    owedAfterActionRepeat,
                    owedAfterActionRepeatExclusion)
                // Milestone 8.35z27. A modal Attack part-way through its plan. It rides
                // whatever continuation the capture found, for the reason the owed repeats above do -
                // it is a fact about the action the pause is inside rather than a kind of pause - and
                // it is applied last so it forwards everything the builders before it set.
                .WithAttackSequenceChain(attackDefenseBackend.PendingSequenceChain)
                .WithBorrowedSkillParents(borrowedSkillParents);

        private CardActionResolutionRestoreState CaptureContinuation()
        {
            var ledger = CoinLedger == null
                ? HeroCoinLedgerRestoreStateHolder.Absent()
                : HeroCoinLedgerRestoreStateHolder.Present(CoinLedger.CaptureRestoreState());
            // Milestone 8.10 first, and deliberately: at p.14's step 4 the attack's own resolution is
            // still pending underneath the paused text, so asking about the attack before asking about
            // the text would capture the attack alone and lose the frame that is holding the page.
            if (pendingCardText != null)
            {
                return CardActionResolutionRestoreState.WithPendingCardText(
                    pendingCardText, ledger, pendingLanePush);
            }

            if (PendingResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingMovement(
                    PendingResolution, ledger, pendingLanePush);
            }

            if (PendingAttackResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingAttack(
                    PendingAttackResolution, ledger, pendingLanePush);
            }

            if (PendingSkillResolution != null)
            {
                return CardActionResolutionRestoreState.WithPendingSkill(
                    PendingSkillResolution, ledger, pendingLanePush);
            }

            return pendingCorrection != null
                ? CardActionResolutionRestoreState.WithPendingCorrection(
                    pendingCorrection, ledger, pendingRepeat, pendingLanePush)
                : CardActionResolutionRestoreState.WithoutPendingAction(ledger, pendingLanePush);
        }

        /// <summary>
        /// Restores the single card-action authority described by <paramref name="state"/>.
        /// </summary>
        /// <param name="verifiedMovementChoice">
        /// An independently supplied pending movement choice to cross-check, or
        /// <c>null</c> to accept the one derived from the resolution.
        /// </param>
        /// <param name="verifiedTargetChoice">
        /// An independently supplied Attack target choice to cross-check, or <c>null</c>.
        /// </param>
        /// <param name="verifiedDefenseChoice">
        /// An independently supplied Defense response choice to cross-check, or <c>null</c>.
        /// </param>
        /// <param name="markers">
        /// The marker register the round is restoring, so the dispatcher's capabilities and the round's
        /// persisted state share one object. Required rather than optional: a caller that supplied its
        /// own fresh register here would restore a match whose markers exist for the snapshot and not
        /// for the effects that can return them.
        /// </param>
        public static CardActionResolutionAuthority Restore(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            CardActionResolutionRestoreState state,
            PendingMovementChoice verifiedMovementChoice,
            PendingAttackTargetChoice verifiedTargetChoice,
            PendingDefenseResponseChoice verifiedDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            GuardsDigital.Core.Rules.Progression.HeroProgressionState heroLevels = null,
            GuardsDigital.Core.Rules.Consequences.TeamLifeState teamLives = null,
            EffectRosterState rosterState = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Milestone 6.9. Restored outside the continuation switch because a mid-turn Lane-Push
            // request is not one of the continuations: it accompanies whichever one the action left
            // behind, including none at all.
            var restoredAuthority = RestoreContinuation(
                catalogs, adjacentMoveAuthority, state, verifiedMovementChoice, verifiedTargetChoice,
                verifiedDefenseChoice, definitions, instances, participants, markers, modifications,
                heroItems, rosterState);
            restoredAuthority.pendingLanePush = state.PendingImmediateLanePush;
            restoredAuthority.pendingPreAction = state.PendingPreAction;
            restoredAuthority.repeatSubject = state.AfterActionRepeatSubject;

            // Milestone 8.35z21, outside the continuation switch for the same reason as everything above
            // it: a decided-and-unstarted repeat instance is a fact about the action boundary rather
            // than a continuation of its own.
            restoredAuthority.owedAttackRepeat = state.OwedAttackRepeat;
            restoredAuthority.owedAttackRepeatExclusion = state.OwedAttackRepeatExclusion;
            restoredAuthority.owedAfterActionRepeat = state.OwedAfterActionRepeat;
            restoredAuthority.owedAfterActionRepeatExclusion = state.OwedAfterActionRepeatExclusion;

            // Milestone 8.19c1. Restored outside the continuation switch for the reason the Lane-Push
            // request is: neither is a continuation, and both stand beside whichever one the action
            // left behind.
            restoredAuthority.pendingEffectInterrupt = state.PendingEffectInterrupt;
            restoredAuthority.attackDefenseBackend.RestorePendingRemovalPrevention(
                state.PendingRemovalPrevention);

            // Milestone 8.35z27, and it has to be here rather than only inside the Attack
            // arm of the switch: p.14's minion arm can end a sequence's command with a Defeat standing
            // at its removal stage, which leaves a modal Attack holding a live plan and NO pending
            // resolution at all - the continuation kind is then None and the Attack arm is never
            // reached. Restoring it twice on the Attack arm is harmless; restoring it nowhere for that
            // state would lose the whole rest of the action.
            restoredAuthority.attackDefenseBackend.RestoreSequenceChain(state.AttackSequenceChain);
            restoredAuthority.borrowedSkillParents.AddRange(state.BorrowedSkillParents);

            // Milestone 8.22, same line of reasoning again: the rest of p.14 owed while a card-text
            // Push has set off a trigger is not a continuation either, and the continuation it stands
            // beside is usually none at all.
            restoredAuthority.pendingCardTextAwaitingInterrupt = state.PendingCardTextRemainder;

            // Every continuation, not only the Attack one that needed the items to verify itself: the
            // restored dispatcher answers the next command, and that command may be the Defense whose
            // total the items belong in.
            restoredAuthority.BindHeroItems(heroItems);

            // Milestone 7.4. Same reason, one milestone later: the restored dispatcher answers the next
            // command, and that command may be the Attack outcome whose Defeat p.13 prices. A round
            // restoring a whole match rebinds both from its own restored state a few lines later, so
            // this argument is for the compositions that restore a dispatcher alone.
            if (heroLevels != null && teamLives != null)
            {
                restoredAuthority.BindDefeatConsequenceState(heroLevels, teamLives);
            }

            if (rosterState != null)
            {
                restoredAuthority.BindRosterState(rosterState);
            }

            return restoredAuthority;
        }

        private static CardActionResolutionAuthority RestoreContinuation(
            MatchContentCatalogs catalogs,
            AdjacentMoveAuthority adjacentMoveAuthority,
            CardActionResolutionRestoreState state,
            PendingMovementChoice verifiedMovementChoice,
            PendingAttackTargetChoice verifiedTargetChoice,
            PendingDefenseResponseChoice verifiedDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectRosterState rosterState)
        {
            var coinLedger = state.CoinLedger.IsPresent
                ? HeroCoinLedger.Restore(participants, state.CoinLedger.State)
                : null;
            var components = CreateComponents(catalogs, adjacentMoveAuthority, markers);

            CardActionResolutionAuthority Compose(
                MovementCardActionStrategyBackend movement = null,
                AttackDefenseCardActionStrategyBackend attack = null,
                SkillCardActionStrategyBackend skill = null) =>
                new CardActionResolutionAuthority(
                    catalogs,
                    movement ?? new MovementCardActionStrategyBackend(adjacentMoveAuthority),
                    attack ?? new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers),
                    new FastTravelCardActionStrategyBackend(adjacentMoveAuthority),
                    new ClearCardActionStrategyBackend(adjacentMoveAuthority),
                    skill ?? new SkillCardActionStrategyBackend(catalogs.Capabilities),
                    components,
                    adjacentMoveAuthority);

            switch (state.ContinuationKind)
            {
                case CardActionContinuationKind.Movement:
                    return Compose(
                        movement: MovementCardActionStrategyBackend.Restore(
                            adjacentMoveAuthority,
                            new MovementBackendRestoreState(state.PendingMovementResolution),
                            verifiedMovementChoice,
                            modifications));

                case CardActionContinuationKind.Attack:
                    return Compose(
                        attack: AttackDefenseCardActionStrategyBackend.RestoreAttack(
                            adjacentMoveAuthority,
                            new AttackDefenseBackendRestoreState(
                                state.PendingAttackResolution, state.AttackSequenceChain),
                            verifiedTargetChoice,
                            verifiedDefenseChoice,
                            definitions,
                            instances,
                            participants,
                            coinLedger,
                            markers,
                            modifications,
                            heroItems,
                            rosterState));

                case CardActionContinuationKind.Skill:
                    var isTriggered = catalogs.TriggeredEffects.TryGet(
                        state.PendingSkillResolution.EffectDefinitionReference, out _);
                    var restoredEffect = Compose(
                        skill: isTriggered
                            ? null
                            : SkillCardActionStrategyBackend.Restore(
                                catalogs.Capabilities,
                                new SkillBackendRestoreState(state.PendingSkillResolution)));
                    if (isTriggered)
                        restoredEffect.triggeredEffects.RestorePending(state.PendingSkillResolution);
                    return restoredEffect;

                case CardActionContinuationKind.Correction:
                    var restored = Compose();
                    restored.pendingCorrection = state.PendingCorrection;
                    restored.pendingRepeat = state.PendingRepeat;
                    return restored;

                // Milestone 8.10. Nothing about the attack underneath is rebuilt here: what the page
                // has left to do is regenerated from the continuation's own identity when the question
                // is answered, so a restored pause owns exactly what the live one owned.
                case CardActionContinuationKind.AttackCardText:
                    // Milestone 8.19a. The attack underneath is restored at the two moments it is still
                    // in flight, and at neither of them is anything about it rebuilt: the payload
                    // already carries the resolution, and the rest of rulebook p.14 is produced from it
                    // when the question is answered. Step 7's attack is finished by the time its text
                    // runs - the page's own last step - so restoring one there would put a completed
                    // attack back into the pending slot.
                    var attackUnderTheText =
                        state.PendingCardText.Moment == CardTextTiming.AfterTheAttack
                            ? null
                            : state.PendingCardText.Attack;
                    var restoredCardText = Compose(
                        attack: attackUnderTheText == null
                            ? null
                            : AttackDefenseCardActionStrategyBackend.RestoreAttack(
                                adjacentMoveAuthority,
                                new AttackDefenseBackendRestoreState(
                                    attackUnderTheText, state.AttackSequenceChain),
                                null,
                                null,
                                definitions,
                                instances,
                                participants,
                                coinLedger,
                                markers,
                                modifications,
                                heroItems,
                                rosterState));
                    restoredCardText.pendingCardText = state.PendingCardText;
                    return restoredCardText;

                default:
                    return Compose();
            }
        }

        public CardActionResolutionResult StartMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Movement,
                false);
            if (prepared.RejectionReason.HasValue)
            {
                return CardActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingResolution,
                    PendingChoice);
            }

            return movementBackend.StartMovement(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances,
                    modifications);
        }

        public CardActionResolutionResult StartTurnMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Movement,
                true);
            if (prepared.RejectionReason.HasValue)
            {
                return CardActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingResolution,
                    PendingChoice);
            }

            return movementBackend.StartTurnMovement(
                    resolutionId,
                    offer,
                    prepared.Definitions,
                    prepared.Instances,
                    modifications,
                    IsAuthorizedBorrowedOffer(offer, prepared.Definitions));
        }

        public CardActionResolutionResult AnswerMovementChoice(
            MovementChoiceCommand command, RuleModificationSet modifications) =>
            movementBackend.AnswerMovement(command, modifications);

        public CardActionResolutionResult PerformTurnFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            RuleModificationSet modifications)
        {
            if (HasPendingResolution)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    PendingResolution,
                    PendingChoice);
            }

            return fastTravelBackend.PerformTurnFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                definitions,
                instances,
                participants,
                minions,
                modifications);
        }

        /// <summary>
        /// Milestone 8.8b: whether this Clear could remove anything, asked of the same backend that
        /// validates the selection when the Clear runs. The counterpart of
        /// <see cref="CanAttackFindTarget"/> for p.13's other replacement.
        /// </summary>
        public bool CanClearRemoveAnything(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications) =>
            clearBackend.CanRemoveAnything(
                replacementOffer,
                replacedAttackActionDefinitionId,
                definitions,
                instances,
                modifications);

        public CardActionResolutionResult PerformTurnClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            if (HasPendingResolution)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    PendingResolution,
                    PendingChoice);
            }

            return clearBackend.PerformTurnClear(
                resolutionId,
                replacementOffer,
                replacedAttackActionDefinitionId,
                selectedTokenIds,
                definitions,
                instances,
                modifications);
        }

        public AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Attack,
                false);
            if (prepared.RejectionReason.HasValue)
            {
                return AttackActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingAttackResolution,
                    PendingAttackTargetChoice,
                    PendingDefenseChoice);
            }

            return attackDefenseBackend.StartAttack(
                resolutionId,
                offer,
                prepared.Definitions,
                prepared.Instances,
                participants,
                modifications);
        }

        public AttackActionResolutionResult StartTurnAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var prepared = PrepareStart(
                resolutionId,
                offer,
                definitions,
                instances,
                ActionKind.Attack,
                true);
            if (prepared.RejectionReason.HasValue)
            {
                return AttackActionResolutionResult.Reject(
                    prepared.RejectionReason.Value,
                    PendingAttackResolution,
                    PendingAttackTargetChoice,
                    PendingDefenseChoice);
            }

            return attackDefenseBackend.StartTurnAttack(
                resolutionId,
                offer,
                prepared.Definitions,
                prepared.Instances,
                participants,
                modifications,
                IsAuthorizedBorrowedOffer(offer, prepared.Definitions));
        }


        /// <summary>
        /// Applies one card's printed text at the moment rulebook p.14 reached, and returns the ordered
        /// facts it produced.
        ///
        /// <para>Milestone 8.5, closing the 7.9a review's F-CARD-01. The Attack backend owns p.14's
        /// order and says where each of the page's three card-text steps falls; this is what actually
        /// composes one, because the effect catalog, the component ports and the item state live here
        /// rather than in a backend that resolves numbers. The enclosing action's kind goes into the
        /// resolution context, so a capability that asks p.18's immunity asks about the Attack it is
        /// running inside rather than about a Skill (F-BOARD-03).</para>
        ///
        /// <para><b>Card text applied inside an Attack may pause, at two of p.14's three moments.</b>
        /// Milestone 8.10. What the page leaves after step 4 and after step 7 is a function of the
        /// attack's own identity, so a question standing at either is held in a
        /// <see cref="PendingAttackCardTextResolution"/> and the rest of the page is rebuilt from that
        /// identity when it is answered. What follows step 1 is not such a function - on the minion arm
        /// it contains the facts of a Defeat this backend has already applied - so text that could ask
        /// there is still refused at configuration by
        /// <see cref="GuardsDigital.Core.Effects.EffectDefinitionValidator"/>, and the throw below is
        /// the case that rule failed to catch. It stays a content fault rather than a rules
        /// outcome.</para>
        /// </summary>
        public IReadOnlyList<ResolutionStep> ApplyCardText(
            ResolutionStep step,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            if (step.Kind != ResolutionStepKind.CardText)
            {
                throw new ArgumentException(
                    "Only a card-text step applies card text.", nameof(step));
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = (instances ??
                throw new ArgumentNullException(nameof(instances))).ToArray();
            var card = materializedInstances
                .Single(candidate => candidate.Id.Equals(step.CheckpointSourceCardInstanceId));
            var definition = materializedDefinitions.Single(candidate =>
                candidate.Reference.Equals(card.DefinitionReference));
            var action = definition.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(step.CardTextActionDefinitionId));

            // Milestone 8.23. The body applied here is the one this action prints for THIS moment; an
            // Attack may print two, six numbered steps apart.
            var context = new EffectResolutionContext(
                action.CardTextAt(step.CardTextTiming),
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                action.Kind,
                // Milestone 8.23. The defender's own card text at rulebook p.14's step 4 is the one
                // card-text moment with a hero on the other side of it, and that hero is the attacker.
                // `EffectRoleKind.OpposingHero` has named exactly this since milestone 8.22 - its
                // validator says so in as many words - and nothing supplied it here, so a Block's
                // rider naming "the attacker" resolved to nobody and reported itself impossible.
                // Tigerclaw's Parry and Riposte are the first content to print one.
                //
                // The attacker's own two moments supply none. At step 1 the Attack has no target yet,
                // and at step 7 what it took may be a minion - so there is no opposing *hero* to name,
                // and a text about the target reaches it through the enclosing facts instead.
                opposingHeroId: step.CardTextTiming == CardTextTiming.DefenseAction
                    ? EnclosingAttackOf(
                        step.CardTextRemainderResolution, materializedDefinitions, board).ActorId
                    : default,
                components: components,
                heroItems: skillBackend.HeroItems,
                // Milestone 8.10, widened at 8.11. What this text may ask about the Attack it is
                // printed inside: who is attacking, what the Attack took, and p.13.s subtype. Every
                // field is undefined at p.14.s step 1, because the page has not chosen a target yet.
                enclosingAttack: EnclosingAttackOf(
                    step.CardTextRemainderResolution, materializedDefinitions, board),
                rosterState: rosterState,
                // Milestone 8.22, finding `F-ATK-02`. p.14's card text can displace a unit - Sabina's
                // Point Blank Shot pushes what its Attack targeted - and a source watching p.18's
                // Pushes is entitled to that moment wherever the Push happened. The Skill altitude has
                // watched since 8.19c1; this one did not, so an after-push trigger could never fire
                // from an Attack's own rider, which is the only push her set prints.
                watchesAppliedPushes: WatchesAppliedPushes(materializedDefinitions, instances),
                // Milestone 8.25d. Rulebook p.10: "You cannot repeat a repeated action!" A
                // `RepeatAction` node reached inside p.14 asks to repeat the attack this text is
                // running inside - `RecordOwedAttackRepeat` owes the request to that attack and to no
                // other, whichever of the page's three moments asked - so an attack that is already a
                // repeated instance has nothing left this page may repeat, and the node is skipped in
                // silence exactly as a chain that has run its declared maximum is.
                //
                // It is stated here because this is where the enclosing attack is known. Before this
                // milestone the enclosing attack could not say it was a repeat at all: the second
                // instance copied the first's printed context, so Cleave's step 7 asked again inside
                // its own second instance and a third instance started - measured, not assumed.
                //
                // Milestone 8.35z46 makes the bar per clause where the card prints a
                // chain, and leaves this sentence exactly as it was where it prints one - see
                // CardTextRepeatIsBarredByPageTen.
                repeatIsUnavailable: CardTextRepeatIsBarredByPageTen(
                    step.CardTextRemainderResolution, materializedDefinitions),
                // Milestone 8.35z15. The other half of the same gate, and the half this path never
                // had: p.10's repeat is an offer, so the node asks whether the instance it would
                // offer can start at all - with the binding it declares, against the board this
                // moment is standing on. See CardTextRepeatCanBePerformed.
                repeatCanBePerformed: binding => CardTextRepeatCanBePerformed(
                    binding,
                    step.CardTextRemainderResolution,
                    materializedDefinitions,
                    materializedInstances,
                    participants,
                    modifications));
            var result = EffectResolutionSession.Start(
                new EffectResolutionId(
                    $"card-text-{card.Id.Value}-{action.Id.Value}-{step.CardTextTiming}"),
                card.Id,
                card.DefinitionReference,
                action.Id,
                step.CardTextActorId,
                step.CardTextActorId,
                ActionInvocationContext.PrintedPrimary,
                context);
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"Card text on '{definition.Id}' action '{action.Id}' was refused at resolution " +
                    $"({result.RejectionReason}); the effect-definition validator admits only text a " +
                    "match can apply.");
            }

            if (result.Frame?.PendingChoice != null)
            {
                if (step.CardTextRemainderResolution == null)
                {
                    throw new InvalidOperationException(
                        $"Card text on '{definition.Id}' action '{action.Id}' asked a question at " +
                        $"rulebook p.14's {step.CardTextTiming}, whose remaining steps no identity can " +
                        "reproduce. The effect-definition validator refuses such a composition at " +
                        "configuration; this build loaded one it should have rejected.");
                }

                // Milestone 8.10. The page stops here. What is kept is the frame that asked, the moment
                // it asked at, and the attack the rest of the page belongs to - never the steps
                // themselves, which are regenerated from that identity when the answer arrives.
                pendingCardText = new PendingAttackCardTextResolution(
                    result.Frame,
                    step.CardTextTiming,
                    step.CardTextRemainderResolution,
                    step.CardTextRemainderOutcome);
            }

            var steps = result.Events
                .Select(value => CardActionResolutionEvent.CardTextApplied(
                    card.Id, action.Id, step.CardTextActorId, step.CardTextTiming, value))
                .Select(ResolutionStep.FromFact)
                .ToList();
            AppendRequestedHeroDefeats(
                result, step.CardTextRemainderResolution, materializedDefinitions, instances,
                participants, modifications, steps);
            RecordOwedAttackRepeat(result, step.CardTextRemainderResolution);
            // Milestone 8.35z50. A borrow asked for by card text is recorded at the same moment a
            // card-text repeat is, and read from the same completed result. The actor is the hero whose
            // text asked - the borrowing hero performs the borrowed primary, never its owner.
            RecordOwedBorrowedAction(result, step.CardTextActorId);

            // Milestone 8.22, finding `F-ATK-02`. The two things this altitude used to drop, both of
            // which the Skill altitude has carried since 6.9 and 8.19c1: rulebook p.8's recorded cause,
            // and the sources watching the Pushes this text applied. Shootout and Bullet Hell remove
            // minions from inside an Attack's text and can take a side's last one out of the Battle
            // Zone; Point Blank Shot pushes from there and Big Sodding Gun is watching.
            if (step.CardTextRemainderResolution != null)
            {
                RecordImmediateLanePush(
                    result.LanePushCause,
                    step.CardTextRemainderResolution.ResolutionId,
                    participants,
                    modifications);
            }

            RunCardTextPushTriggers(
                result, step.CardTextActorId, step.CheckpointSourceCardInstanceId,
                step.CardTextActionDefinitionId, step.CardTextTiming,
                step.CardTextRemainderResolution, step.CardTextRemainderOutcome,
                materializedDefinitions, instances, participants, timing, tieBreaker,
                roundIndex, turnIndex, steps);

            // Milestone 8.19a. Rulebook p.14's step 1 is the one moment whose remainder is the rest of
            // the page rather than a list of facts, so when the text finishes without asking anything
            // the page is resumed here - through the same generator the answer path uses.
            if (step.CardTextTiming == CardTextTiming.BeforeTheAttack && pendingCardText == null)
            {
                steps.AddRange(ResumeAttackAfterBeforeText(
                    result.BoundAttackTarget, result.RequestedAttackSequences,
                    materializedDefinitions, instances, participants, modifications));
            }

            return Array.AsReadOnly(steps.ToArray());
        }

        /// <summary>
        /// Milestone 8.19a. Hands the Attack back everything rulebook p.14 does after its step 1, once
        /// that step's text has finished.
        ///
        /// <para>The one thing carried across is the unit the text named, read off the frame's own
        /// completed outcomes by <see cref="RequestedAttackTargetQuery"/> - the transport milestone
        /// 8.10's Defeat request and milestone 6.9's Lane-Push cause already use, and the reason a
        /// binding survives however many questions the text stopped on. The backend re-derives that
        /// unit's legality against the board the text has just changed; nothing here decides it.</para>
        ///
        /// <para><b>The steps it produces may contain this action's step 7, and never a second step
        /// 1.</b> Milestone 8.19a wrote this refusal against every card-text step, because an action
        /// declared one moment then and this one's was step 1; it named the future action that could
        /// declare two, so that its second would be caught here rather than silently skipped.
        /// Milestone 8.23 is that action - Combat Reflexes and Leaping Strike print "Before the
        /// attack:" and "After the attack:" on one face - so the remainder of a step-1 pause now
        /// legitimately carries step 7 and the refusal narrows to what stayed impossible. p.14 has one
        /// step 1, and a remainder carrying another would apply the same sentence twice.</para>
        /// </summary>
        private IReadOnlyList<ResolutionStep> ResumeAttackAfterBeforeText(
            AttackTarget boundTarget,
            AttackSequencePlan requestedSequences,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var resolutionId = attackDefenseBackend.PendingResolution?.ResolutionId ?? default;

            // Milestone 8.35z27. A step-1 text that named a PLAN rather than a unit is a
            // modal Attack, and what follows it is not one p.14 but one per chosen bullet. It is
            // decided here, at the one place step 1's remainder is produced, so the two shapes cannot
            // be reached by different routes: an ordinary card-text Attack binds a target and runs the
            // page once, a modal one opens the chain and runs the page per bullet.
            if (requestedSequences != null)
            {
                var opened = attackDefenseBackend.OpenAttackSequences(
                    Array.Empty<ResolutionStep>(), requestedSequences, definitions, instances,
                    participants, modifications);
                if (!opened.Accepted)
                {
                    throw new InvalidOperationException(
                        "A modal Attack's step-1 text named a plan the Attack it is printed inside " +
                        $"could not open ({opened.RejectionReason}).");
                }

                RecordAttackLanePush(opened, resolutionId, participants, modifications);
                return opened.Steps;
            }

            var continued = attackDefenseBackend.ContinueAfterBeforeAttackText(
                Array.Empty<ResolutionStep>(), definitions, instances, participants, modifications,
                boundTarget);
            if (!continued.Accepted)
            {
                throw new InvalidOperationException(
                    "Rulebook p.14's step 1 finished on an Attack that could no longer be resumed " +
                    $"({continued.RejectionReason}); the page is produced from the resolution the " +
                    "step was applied against, which nothing between them may clear.");
            }

            if (continued.Steps.Any(produced =>
                produced.Kind == ResolutionStepKind.CardText &&
                produced.CardTextTiming == CardTextTiming.BeforeTheAttack))
            {
                throw new InvalidOperationException(
                    "Rulebook p.14 numbers one step 1, so the remainder of a step-1 pause cannot " +
                    "contain another; an action's second moment is its step 7.");
            }

            RecordAttackLanePush(continued, resolutionId, participants, modifications);
            return continued.Steps;
        }

        /// <summary>
        /// Milestone 8.10. Runs rulebook p.13's Defeat list for every hero this card text asked it to be
        /// run about, in the order the frame recorded them, and splices its facts into the same stream.
        ///
        /// <para>The requests are read only from a step that <em>completed</em>, which is the same rule
        /// milestone 6.9's Lane-Push cause is read under and for the same reason: a frame carries its
        /// outcomes cumulatively, so a request seen while the text is still paused would be seen again
        /// on the next step and the page would be run twice about one hero. Electroblast's defeat is
        /// the last node of its rider, so completion is exactly where it belongs.</para>
        ///
        /// <para>It is the Attack's own resolver that runs the list - not a second copy of p.13 - which
        /// is why the attack this text is printed inside has to be the one identity handed over.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.10. Remembers that this Attack's card text asked for the Attack again, and what
        /// the repeated instance may not take.
        ///
        /// <para>The exclusion is read from the declaration the request carries rather than from the
        /// effect nodes, which this dispatcher does not inspect: a repeat that declared nothing chooses
        /// freely by p.10, and only <c>DifferentTarget</c> - which is what Thunder Boomerang prints -
        /// takes the finished instance's target out of the set.</para>
        /// </summary>
        /// <remarks>
        /// Milestone 8.35i made it `internal`. The snapshot restore path has to rebuild the SAME facts
        /// when it re-asks a capability that paused inside an Attack, and a second builder there would
        /// be exactly the "second place the same fact could be wrong" this method already refuses for
        /// the printed subtype.
        /// </remarks>
        /// <summary>
        /// What card text applied inside rulebook p.14 may ask about the Attack it is running inside.
        /// Milestone 8.10 for the hero the Attack took; milestone 8.11 for the attacker and p.13's
        /// printed subtype, which Wasp's three Block cards are conditioned on.
        ///
        /// <para>The subtype is read from the trusted definition the resolution names rather than
        /// stored on the resolution, for the reason every other printed reading is: the definition is
        /// the content the match was configured with, and a copy on a continuation would be a second
        /// place the same fact could be wrong.</para>
        ///
        /// <para><b>Milestone 8.35z50 made it `public`, and the widening is deliberately the SMALL
        /// one.</b> That milestone's `attack-target-survived@1` reads `TargetWasRemoved` live off
        /// board occupancy, and the cases that pin that reading live in the Edit Mode test assembly -
        /// which is a separate assembly under Unity, where `internal` is invisible and the one-assembly
        /// dotnet harness and compile probe both report a false green. Core grants no
        /// `InternalsVisibleTo` to that assembly and deliberately should not: that would open every
        /// internal in the layer to reach one pure query. This method is a pure derivation over
        /// trusted inputs - it stores nothing, mutates nothing, and returns a value type computed from
        /// the definition set and the board - so exposing it exposes a reading rather than a seam.
        /// The alternative considered and rejected was moving the cases to the harness-only tree,
        /// which would have kept the evidence but hidden it from the tier that actually compiles the
        /// way the shipped game does.</para>
        /// </summary>
        public static GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts EnclosingAttackOf(
            PendingAttackResolution attack,
            IReadOnlyList<CardDefinition> definitions,
            AdjacentMoveAuthority board = null)
        {
            if (attack == null)
            {
                return GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts.None;
            }

            var action = definitions
                .FirstOrDefault(definition => definition.Reference.Equals(attack.SourceDefinitionReference))
                ?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(attack.ActionDefinitionId));

            // Milestone 8.19a added the minion the Attack took and the space its target stood on
            // before p.14's consequences. Both come off the resolution rather than off the board: a
            // step-7 rider asks about a hero the page has already removed, and a text that binds its
            // own target has to be able to see which unit it bound.
            // Milestone 8.35z33 adds the card the defender answered p.14's step 3 with, for Whisper's
            // Swift Justice - "that hero performs a movement action on the card they defended with".
            // It is the offer the Defense window accepted, so it is a projection of state this
            // resolution already holds rather than anything new to persist; where the attack was not
            // defended, or has not reached that step, there is no offer and the field is undefined,
            // which is the "if able" the face prints.
            return new GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts(
                attack.ActorId,
                attack.TargetHeroId,
                attack.TargetMinionId,
                action?.AttackRange ?? AttackRangeKind.None,
                attack.TargetSpaceBeforeConsequences,
                attack.ActorSpaceAtActionStart,
                attack.DefenseOffer?.SourceCardInstanceId ?? default,
                TargetWasRemoved(attack, board));
        }

        /// <summary>
        /// Milestone 8.35z50. Whether this Attack's own target is currently off the board -
        /// rulebook p.14 step 6's answer, read live rather than stored. See
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts.TargetWasRemoved"/>
        /// for why a live read is exact here and costs nothing to restore.
        /// </summary>
        private static bool TargetWasRemoved(PendingAttackResolution attack, AdjacentMoveAuthority board)
        {
            if (board == null)
            {
                // Safe direction: an Attack backend under test with no board wired reports "not
                // removed", which is the reading that composes less rather than more - exactly
                // TargetSurvivedFactIsOwed's own stated direction for the shortfall this replaces.
                return false;
            }

            if (attack.TargetHeroId.IsDefined)
            {
                return !board.Occupancies.Any(
                    occupancy => occupancy.HeroId.Equals(attack.TargetHeroId));
            }

            if (attack.TargetMinionId.IsDefined)
            {
                return !board.Minions.Any(
                    minion => minion.MinionId.Equals(attack.TargetMinionId));
            }

            return false;
        }

        private void RecordOwedAttackRepeat(
            EffectResolutionStepResult result,
            PendingAttackResolution attack)
        {
            if (!result.IsComplete || !result.RepeatRequested.IsRequested || attack == null)
            {
                return;
            }

            owedAttackRepeat = attack;
            owedAttackRepeatExclusion =
                CardTextRepeatExclusionFor(result.RepeatRequested.Binding, attack);
        }

        /// <summary>
        /// Milestone 8.35z50. Whether a completed card text asked to perform a borrowed card's printed
        /// primary, and which card - held here until the caller starts it, on exactly the terms
        /// <see cref="RecordOwedAttackRepeat"/> holds a card-text repeat.
        ///
        /// <para>It is recorded rather than started inline for the reason
        /// <c>SyntheticBorrowedActionCapabilities</c> states about its own handler: a borrowed primary
        /// is a whole action instance with its own resolution, its own pauses and its own card-text
        /// seams, and starting one from inside the frame that asked for it would nest a resolution
        /// inside a resolution. The frame finishes; then the borrow begins.</para>
        /// </summary>
        private void RecordOwedBorrowedAction(EffectResolutionStepResult result, HeroId actorId)
        {
            if (result == null || !result.IsComplete)
            {
                return;
            }

            RecordOwedBorrowedAction(result.BorrowRequested, actorId);
        }

        /// <summary>
        /// The same recording, from a request a backend surfaced through an out-parameter rather than
        /// from a whole step result - which is how the SKILL path reports one, and the path that
        /// matters: every face in this build that prints a borrow prints it on a Skill, so a producer
        /// wired only to the Attack card-text path would be a mechanism with no live caller.
        /// </summary>
        private void RecordOwedBorrowedAction(BorrowRequest borrowRequested, HeroId actorId)
        {
            if (!borrowRequested.IsRequested || !actorId.IsDefined)
            {
                return;
            }

            owedBorrowedActionCardId = borrowRequested.BorrowedCardId;
            owedBorrowedActionActorId = actorId;
        }

        /// <summary>Whether a card text has asked for a borrowed primary that has not started.</summary>
        public bool HasOwedBorrowedAction => owedBorrowedActionCardId.IsDefined;

        /// <summary>
        /// The unit a card-text repeat of this Attack may not take, read off the binding the asking
        /// node declares.
        ///
        /// <para>Milestone 8.24. Xargatha's "on a different enemy hero" excludes the target this
        /// instance took exactly as "on a different target" does; what it adds is a narrowing of the
        /// kinds left, and that half is derived from the card when the repeated instance builds its
        /// candidates rather than carried here.</para>
        ///
        /// <para><b>Milestone 8.35z15 extracted it, and the extraction is the point.</b> The offer has
        /// to be judged against the exclusion the instance it offers would carry, or it is not judging
        /// that instance - which is milestone 8.25e's <see cref="ExclusionFor"/> argument on the
        /// after-action path, made once more here so the card-text offer and the card-text answer read
        /// one statement rather than two copies that can drift.</para>
        /// </summary>
        private static AttackTarget CardTextRepeatExclusionFor(
            RepeatTargetBinding binding, PendingAttackResolution attack) =>
            binding == RepeatTargetBinding.DifferentTarget ||
            binding == RepeatTargetBinding.DifferentEnemyHero ||
            // Milestone 8.35z46. RULING 38 says the minion is a different unit, so the
            // clause excludes what its asking instance took exactly as the two above do. In the one
            // chain that reaches it the exclusion is vacuous - the instance before it took a hero -
            // and it is carried because the ruling says so rather than because a board makes it show.
            binding == RepeatTargetBinding.DifferentEnemyMinion
                ? attack.Target
                : default;

        /// <summary>
        /// Milestone 8.35z15: whether the repeat a card text printed inside rulebook p.14 is about to
        /// offer could actually be performed, asked before the offer is made rather than discovered
        /// after it is taken.
        ///
        /// <para><b>The defect this closes.</b> Milestone 8.25e made p.10's repeat an honest offer on
        /// the after-action-ability path - "you may perform the entire action again" is an offer, so
        /// whether a second instance can start is asked of the board as it then stands - and
        /// <c>Rules/RULE_SYSTEMS.md</c> row 48 has recorded that ruling ever since. The card-text path
        /// never asked it. So Xargatha's Cleave, whose repeat admits enemy heroes and nothing else,
        /// opened p.10's confirm-or-decline against a board whose only adjacent enemy hero was the one
        /// the printed instance had just attacked - a live report, and the fourth instance of the
        /// offer-admits-more-than-the-answer-can-perform class after milestones 8.29d, 8.29e and
        /// 8.35z5.</para>
        ///
        /// <para><b>The board it judges against is the one this moment is standing on, and that is a
        /// recorded ruling rather than a choice made here.</b> <c>Rules/RULE_SYSTEMS.md</c> row 48
        /// says the question is "asked of the board as it then stands" - not of the board the second
        /// instance will eventually start from, which no moment can read. So both of p.14's moments
        /// that reach a repeat are judged the same way, and the residual p.10 keeps for itself - a
        /// board that changes between the offer and the instance - stays exactly where
        /// <see cref="AttackDefenseCardActionStrategyBackend.CanStartRepeatedAttack"/> already
        /// documents it, outside this question's reach.</para>
        ///
        /// <para><b>What makes the question answerable is the target, not the moment.</b> Every
        /// binding here excludes the Attack's own target, so a repeat reached before the page has
        /// chosen one would have nothing to measure its exclusion against - and this returns true
        /// there, which is <see cref="EffectOptionFeasibilityQuery"/>'s own standing direction
        /// restated: refuse a repeat that can be proved to have nowhere to go, never one that merely
        /// cannot be read. Arien's Violent Torrent is why this was measured rather than assumed. Its
        /// repeat is printed at p.14's step 1, so a moment-shaped gate would have excused it - but the
        /// node is reached with the target already chosen, and driven on a board holding one adjacent
        /// minion it opened p.10's confirm, took the confirmation, and started nothing. It is the
        /// second card of the class, not an exception to it.</para>
        ///
        /// <para><b>What it deliberately does not ask.</b> Whether the Attack was defended. p.10 says
        /// "After you perform this action, you may perform the entire action again" and conditions
        /// nothing on the outcome, so a successfully defended Attack was still performed and still
        /// offers its repeat. The live report was a defended Attack, and the empty subject set is the
        /// whole of what is refused here.</para>
        /// </summary>
        private bool CardTextRepeatCanBePerformed(
            RepeatTargetBinding binding,
            PendingAttackResolution enclosing,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            // Milestone 8.35z46. On a card that prints a CHAIN of repeat clauses, p.10's
            // bar is answered per clause rather than per instance, and this is where that answer is
            // given: an instance may ask the clause its own chain position names, and no other. A card
            // printing one clause answers true here and keeps the walk it had.
            if (!CardTextRepeatChain.ClauseIsReachable(
                    ActionOf(enclosing, definitions),
                    enclosing?.ResolutionId ?? default,
                    binding))
            {
                return false;
            }

            if (enclosing == null || !enclosing.Target.IsDefined)
            {
                return true;
            }

            // The same Enfeeblement restriction the owed-repeat gate above asks, on the other of the
            // two paths p.10's Repeat reaches - a repeat printed inside the card's own text rather than
            // owed after the action. Both are "repeat actions" in the sentence's words, so one clause
            // cannot answer only one of them. Milestone 8.35z28.
            if (RepeatRestrictionQuery.IsRestrictedFromRepeating(
                    enclosing.ActorId, modifications ?? RuleModificationSet.Empty))
            {
                return false;
            }

            return attackDefenseBackend.CanStartRepeatedAttack(
                enclosing,
                CardTextRepeatResolutionIdFor(enclosing),
                CardTextRepeatExclusionFor(binding, enclosing),
                definitions,
                instances,
                participants,
                modifications,
                // The text asking this is a step of the Attack it offers to repeat, so that Attack
                // being under way is the subject of the question rather than an obstacle to it.
                theAskingInstanceIsStillUnderWay: true);
        }

        /// <summary>
        /// Milestone 8.25d. The identity a card-text repeat's second instance runs under, derived from
        /// the instance that asked for it rather than minted from a counter, so a replay of the same
        /// accepted commands produces the same one - the rule
        /// <see cref="RepeatResolutionIdFor"/> already follows for the after-action path.
        ///
        /// <para><b>Why it is not the completed instance's own.</b> Rulebook p.13's Defeat outcome is
        /// identified as <c>&lt;resolution&gt;:hero-defeat</c> or <c>&lt;resolution&gt;:minion-defeat</c>
        /// and a Defeat outcome pays out once, so two instances sharing one identity cannot both defeat
        /// the same kind of unit: the second one's reward is refused and rulebook p.14 stops on a fault
        /// the player did nothing to cause. p.10's bar on repeating a repeat makes <c>:repeat</c> unique
        /// without a counter - there is exactly one repeated instance per printed one.</para>
        /// </summary>
        private static ActionResolutionId CardTextRepeatResolutionIdFor(
            PendingAttackResolution completed) =>
            CardTextRepeatChain.Mint(completed.ResolutionId);

        /// <summary>
        /// Milestone 8.35z46. Rulebook p.10's "You cannot repeat a repeated action!" for
        /// card text, asked of the card rather than of the instance alone.
        ///
        /// <para>Milestone 8.25d stated it as a single boolean about the enclosing attack, and for
        /// every face in this build but one that is the whole answer. Silverarrow's Rain of Arrows
        /// prints a CHAIN - "repeat once on a different hero; if you do, repeat once on a minion" -
        /// and a boolean cannot let its second clause run while barring its first. So a chain answers
        /// the bar per clause, in <see cref="CardTextRepeatChain.ClauseIsReachable"/>, and a
        /// single-clause card keeps this exact sentence.</para>
        /// </summary>
        private static bool CardTextRepeatIsBarredByPageTen(
            PendingAttackResolution enclosing, IReadOnlyList<CardDefinition> definitions) =>
            RepeatedActionQuery.IsRepeatedInstance(
                enclosing?.InvocationContext ?? ActionInvocationContext.PrintedPrimary) &&
            !CardTextRepeatChain.PrintsAChain(ActionOf(enclosing, definitions));

        /// <summary>
        /// The action definition one pending attack is performing, or <c>null</c> outside an attack.
        /// It is the same two-step lookup <see cref="EnclosingAttackOf"/> makes, named once because the
        /// chain gate and the narrowing both need it.
        /// </summary>
        private static ActionDefinition ActionOf(
            PendingAttackResolution attack, IReadOnlyList<CardDefinition> definitions) =>
            attack == null || definitions == null
                ? null
                : definitions
                    .FirstOrDefault(definition =>
                        definition.Reference.Equals(attack.SourceDefinitionReference))
                    ?.ActionDefinitions
                    .FirstOrDefault(candidate => candidate.Id.Equals(attack.ActionDefinitionId));

        /// <summary>
        /// Milestone 8.10. Starts the Attack instance the finished one asked for, once that instance's
        /// page is closed.
        ///
        /// <para>The repeated instance is an ordinary Attack from here on: it opens its own target
        /// choice, takes its own Defense response, and runs p.13's consequences and p.14's order for
        /// itself. It may also refuse to start at all - a board with nobody left the card admits is a
        /// repeat that simply does not happen, because p.10 offers a repeat rather than owing one - and
        /// that is reported as a completed action rather than as a rejected command.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.35z27. Whether a modal Attack still owes rulebook p.14 for a bullet
        /// its actor chose.
        ///
        /// <para>It is asked <em>before</em> everything the end of an action owes, and that ordering is
        /// the contract: Ruling 5 makes "choose one, or both" a single action, so p.8's mid-turn Lane
        /// Push, the card's resolution and Initiative's advance all wait for the plan to be spent. It
        /// is deliberately NOT an owed repeat and is not asked beside one - see
        /// <see cref="AttackSequencePlan"/> for why sharing the label would hand a modal card every
        /// rule that reads it.</para>
        /// </summary>
        public bool HasOwedAttackSequence => attackDefenseBackend.HasOwedAttackSequence;

        /// <summary>
        /// Milestone 8.35z27. Starts the next complete p.14 sequence a modal Attack owes,
        /// against the board as it now stands.
        ///
        /// <para>Nothing about the action is reconstructed: the sequences share the action's card, its
        /// actor, its printed invocation context and its <c>ActionStarted</c>, and what each brings of
        /// its own is a resolution identity - p.13's Defeat outcome pays once per identity - and its
        /// bullet's targeting sentence.</para>
        /// </summary>
        public CardActionResolutionResult StartOwedAttackSequence(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var chain = attackDefenseBackend.PendingSequenceChain;
            if (chain == null || !chain.HasNextSequence)
            {
                return CardActionResolutionResult.Accept(
                    true, null, null, Array.Empty<ResolutionStep>());
            }

            var nextResolutionId = chain.NextSequenceResolutionId;
            var started = attackDefenseBackend.StartNextAttackSequence(
                Array.Empty<ResolutionStep>(), definitions, instances, participants, modifications);
            if (!started.Accepted)
            {
                throw new InvalidOperationException(
                    "A modal Attack could not start the sequence its own plan named " +
                    $"({started.RejectionReason}); the plan is settled when the modal is answered and " +
                    "nothing between the sequences may unsettle it.");
            }

            RecordAttackLanePush(started, nextResolutionId, participants, modifications);
            return CardActionResolutionResult.Accept(
                started.PendingResolution == null, null, null, started.Steps);
        }

        /// <summary>
        /// Milestone 8.35z50. Starts the borrowed card's printed primary action once
        /// <c>borrow-primary-action@1</c>'s card choice has been answered - the authority
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.BorrowedActionQuery"/>'s doc comment
        /// names as the missing half, and the direct twin of
        /// <see cref="StartOwedAfterActionRepeat"/>.
        ///
        /// <para><b>Everything the contract promises falls out of ONE decision</b> - that the offer
        /// built here names the BORROWED card as its source. Its values, Range, Radius and printed text
        /// are the borrowed card's because every value query and every card-text seam in this engine
        /// reads them off the offer's own source definition; its supertype is the borrowed card's
        /// because `PerformedBasicAction` resolves that same definition, so borrowing a basic card's
        /// primary is a basic action performed and feeds every trigger that reads one (RULING 36).
        /// Nothing is copied, so nothing can be copied wrongly.</para>
        ///
        /// <para><b>The borrowed card does not change state</b>, and that is structural rather than
        /// guarded: a printed primary declares no <c>SourceCardStateTransition</c> - only a secondary
        /// Defense does - and this path never enters the turn's commitment path, which is the only
        /// other thing that moves a card. <see cref="BorrowedPrimaryActionDeclaration.Refuses"/> states
        /// it as a rule, and is asked here before the offer is built rather than after.</para>
        ///
        /// <para><b>The instance is stamped <see cref="ActionInvocationContext.TextGranted"/></b>, which
        /// is what makes it invisible to every repeat reader in the engine without any of them being
        /// touched: the whole repeat mechanism keys on <see cref="ActionInvocationContext.Repeated"/>
        /// and this is not that (RULING 22). The turn still spends one action in total, because this is
        /// started from inside the borrowing card's own effect resolution - the turn's action was spent
        /// committing that card, exactly as p.14's step-1 move inside an Attack is part of the
        /// Attack.</para>
        /// </summary>
        internal ActionOffer TakeOwedBorrowedAction(
            IEnumerable<CardDefinition> definitions, IEnumerable<CardInstance> instances,
            out ActionResolutionId resolutionId)
        {
            if (!HasOwedBorrowedAction)
                throw new InvalidOperationException("No borrowed primary is owed.");
            var actor = owedBorrowedActionActorId;
            var card = instances.Single(value => value.Id.Equals(owedBorrowedActionCardId));
            var definition = definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
            if (BorrowedPrimaryActionDeclaration.Refuses(definition) != BorrowRefusal.None)
                throw new InvalidOperationException("The selected card has no borrowable primary.");
            var parent = skillBackend.SuspendForBorrow();
            borrowedSkillParents.Add(parent);
            owedBorrowedActionCardId = default;
            owedBorrowedActionActorId = default;
            var primary = definition.ActionDefinitions.Single(value => value.Origin == ActionDefinitionOrigin.PrintedPrimary);
            resolutionId = new ActionResolutionId(parent.ResolutionId.Value + ":borrow:" +
                parent.CompletedOutcomes.Count + ":" + card.Id.Value);
            return new ActionOffer(card.Id, definition.Reference, primary.Id, actor,
                ActionInvocationContext.TextGranted, true, primary.AttackRange, primary.SourceCardStateTransition);
        }

        private bool IsAuthorizedBorrowedOffer(ActionOffer offer, IReadOnlyList<CardDefinition> definitions)
        {
            if (offer.InvocationContext != ActionInvocationContext.TextGranted || !HasBorrowedSkillParent)
                return false;
            var parent = borrowedSkillParents[borrowedSkillParents.Count - 1];
            var root = SkillCardActionStrategyBackend.FindEffectRoot(definitions, parent);
            var request = root == null ? default : BorrowedActionQuery.Requested(root, parent.CompletedOutcomes);
            return request.IsRequested && request.BorrowedCardId.Equals(offer.SourceCardInstanceId) &&
                parent.ActorId.Equals(offer.ActorId) &&
                ActionOfferQuery.FindActionDefinition(offer, definitions).Origin == ActionDefinitionOrigin.PrintedPrimary;
        }

        internal CardActionResolutionResult ResumeBorrowingSkill(ActionBoundaryContext boundary)
        {
            if (!HasBorrowedSkillParent)
                throw new InvalidOperationException("No borrowing Skill is suspended.");
            var parent = borrowedSkillParents[borrowedSkillParents.Count - 1];
            borrowedSkillParents.RemoveAt(borrowedSkillParents.Count - 1);
            skillBackend.RestoreBorrowParent(parent);
            var result = skillBackend.ContinueAfterInterrupt(
                boundary.Definitions, boundary.Instances, boundary.Participants, boundary.Timing,
                CurrentModificationsFor(boundary, parent.ActorId), components, boundary.RoundIndex,
                boundary.TurnIndex, out var repeat, out var cause, out var pushes,
                out var defeats, out var borrow);
            RecordOwedBorrowedAction(borrow, parent.ActorId);
            var id = new ActionResolutionId(parent.ResolutionId.Value);
            result = WithSkillRequestedHeroDefeats(result, defeats, id, parent.SourceCardInstanceId,
                parent.ActionDefinitionId, parent.ActorId, boundary);
            result = RunSkillConsequences(result, pushes, boundary, ref repeat, ref cause);
            return CloseActionBoundary(result, repeat, cause, id, parent.SourceCardInstanceId,
                parent.ActionDefinitionId, parent.ActorId, boundary);
        }

        /// <summary>
        /// Milestone 8.35z50. The identity a borrowed instance resolves under.
        ///
        /// <para>It is minted from the borrowed card and the borrowing hero rather than inherited from
        /// the frame that asked, because the borrowed action is a DIFFERENT action instance - rulebook
        /// p.13's Defeat outcome pays once per identity, and sharing one with the borrowing card's own
        /// resolution is exactly the collision milestone 8.25d recorded for two repeat instances.</para>
        /// </summary>
        private static ActionResolutionId BorrowedResolutionIdFor(
            CardInstanceId borrowedCardId, HeroId actorId) =>
            new ActionResolutionId($"borrowed:{actorId.Value}:{borrowedCardId.Value}");

        public CardActionResolutionResult StartOwedAttackRepeat(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var attack = owedAttackRepeat;
            var excluded = owedAttackRepeatExclusion;
            owedAttackRepeat = null;
            owedAttackRepeatExclusion = default;
            if (attack == null)
            {
                return CardActionResolutionResult.Accept(
                    true, null, null, Array.Empty<ResolutionStep>());
            }

            var repeated = attackDefenseBackend.StartRepeatedAttack(
                attack, CardTextRepeatResolutionIdFor(attack), excluded, definitions, instances,
                participants, modifications);
            return repeated.Accepted
                ? CardActionResolutionResult.Accept(false, null, null, repeated.Steps)
                : CardActionResolutionResult.Accept(true, null, null, Array.Empty<ResolutionStep>());
        }

        /// <summary>
        /// Milestone 8.23. Starts the instance a permanent after-action ability asked for.
        ///
        /// <para>Rulebook p.10: "After you perform this action, you may perform the entire action again
        /// (with the same range, restrictions etc.)". So this does not reconstruct anything the first
        /// instance did - it starts the same action from the same card, under
        /// <see cref="ActionInvocationContext.Repeated"/>, and every rule that applied to the first
        /// applies again from the board as it now stands.</para>
        ///
        /// <para>Which resolver starts it is decided by the kind, and by nothing else. p.9 makes every
        /// action on a basic card basic, so the set is wider than the printed icons: a Movement and a
        /// Hold performed on a basic card are repeatable alongside the printed Attack and Skill, and
        /// each is started by the resolver that started the first instance. Nothing here reads a card
        /// name or an ability.</para>
        ///
        /// <para><b>Milestone 8.25e: nothing arrives here that cannot start.</b> This used to report
        /// "nothing happened" on four arms - the two kinds it had no case for, and any of the other
        /// three whose backend refused the second instance - so an ability could offer a repeat, have
        /// the player take it, and change nothing at all. The question is now asked before the offer,
        /// by <see cref="OwedRepeatCanBePerformed"/>, so a repeat that cannot be performed is never
        /// owed; p.11's Fast Travel and p.13's Clear are refused by
        /// <see cref="PerformedBasicAction.KindCanBeRepeated"/> before that, and p.12's Hold is
        /// performed by the turn authority, which is the only place that raises a Hold.</para>
        ///
        /// <para>A backend that refuses anyway means the offer and the answer have disagreed, which is
        /// the defect itself rather than a state to shrug at, so it faults and the command transaction
        /// rolls the answer back.</para>
        /// </summary>
        public CardActionResolutionResult StartOwedAfterActionRepeat(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            var owed = owedAfterActionRepeat;
            owedAfterActionRepeat = null;
            var nothingHappened = CardActionResolutionResult.Accept(
                true, null, null, Array.Empty<ResolutionStep>());
            if (owed == null)
            {
                return nothingHappened;
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var card = materializedInstances.FirstOrDefault(
                candidate => candidate.Id.Equals(owed.SourceCardInstanceId));
            var definition = card == null
                ? null
                : materializedDefinitions.FirstOrDefault(
                    candidate => candidate.Reference.Equals(card.DefinitionReference));
            var action = definition?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(owed.ActionDefinitionId));
            if (action == null)
            {
                // A payload naming a card or an action this build does not supply produces no repeat
                // rather than a reconstructed one, which is the same refusal every other continuation
                // here makes against a tampered or stale record.
                return nothingHappened;
            }

            var repeatedOffer = new ActionOffer(
                owed.SourceCardInstanceId,
                definition.Reference,
                owed.ActionDefinitionId,
                owed.ActorId,
                ActionInvocationContext.Repeated,
                true,
                action.AttackRange,
                action.SourceCardStateTransition);

            switch (owed.Kind)
            {
                case ActionKind.Attack:
                    var repeatedAttack = attackDefenseBackend.StartRepeatedAttack(
                        new PendingAttackResolution(
                            owed.ResolutionId,
                            owed.SourceCardInstanceId,
                            definition.Reference,
                            owed.ActionDefinitionId,
                            owed.ActorId,
                            ActionInvocationContext.Repeated,
                            new ActionResolutionStepId("attack-target"),
                            AttackResolutionPhase.ChooseTarget,
                            default(AttackTarget),
                            null),
                        // Milestone 8.25d: the identity minted above, unchanged. This path already
                        // gives its instance one of its own, which is why only the card-text path
                        // needed a derivation added.
                        owed.ResolutionId,
                        // p.10's exclusion belongs to card text that names it. Cloak and Daggers prints
                        // its own - "if you repeat an attack action, you cannot target the same unit" -
                        // and that arrives as the request's binding rather than as a rule of this
                        // dispatcher, which is why it is read off the ability and not assumed here.
                        owedAfterActionRepeatExclusion,
                        materializedDefinitions,
                        materializedInstances,
                        participants,
                        modifications);
                    owedAfterActionRepeatExclusion = default;
                    return repeatedAttack.Accepted
                        ? CardActionResolutionResult.Accept(false, null, null, repeatedAttack.Steps)
                        : throw RefusedAfterItWasOffered(owed, repeatedAttack.RejectionReason);

                case ActionKind.Skill:
                    if (action.EffectRoot == null)
                    {
                        throw RefusedAfterItWasOffered(
                            owed, CardActionResolutionRejectionReason.UnavailableOffer);
                    }

                    var repeatedSkill = skillBackend.StartRepeatedSkill(
                        owed.ResolutionId,
                        PendingRepeatedAction.Mint(
                            owed.ResolutionId,
                            owed.SourceCardInstanceId,
                            definition.Reference,
                            owed.ActionDefinitionId,
                            action.EffectRoot.Reference,
                            owed.ActorId,
                            owed.ActorId,
                            RepeatContinuation.Next(1)),
                        materializedDefinitions,
                        materializedInstances,
                        participants,
                        timing,
                        modifications,
                        components,
                        roundIndex,
                        turnIndex,
                        out _,
                        out _,
                        out _,
                        out _,
                        out _);
                    if (!repeatedSkill.Accepted)
                    {
                        throw RefusedAfterItWasOffered(owed, repeatedSkill.RejectionReason);
                    }

                    return CardActionResolutionResult.Accept(
                        repeatedSkill.IsComplete, null, null, repeatedSkill.Steps);

                case ActionKind.Movement:
                    var repeatedMovement = movementBackend.StartRepeatedMovement(
                        owed.ResolutionId, repeatedOffer, materializedDefinitions,
                        materializedInstances, modifications);
                    return repeatedMovement.Accepted
                        ? CardActionResolutionResult.Accept(
                            repeatedMovement.IsComplete, null, null, repeatedMovement.Steps)
                        : throw RefusedAfterItWasOffered(owed, repeatedMovement.RejectionReason);

                default:
                    // p.12's Hold is the one basic action no resolver here holds - it is raised by the
                    // turn authority, which is where its second instance is performed - so an owed Hold
                    // never reaches this dispatcher at all. p.11's Fast Travel and p.13's Clear never
                    // become an owed repeat, because p.10 refuses a repeated instance that is a
                    // replacement and PerformedBasicAction.KindCanBeRepeated states that before the
                    // ability offers. Either one arriving here means that rule was bypassed.
                    throw RefusedAfterItWasOffered(
                        owed, CardActionResolutionRejectionReason.UnsupportedActionKind);
            }
        }

        /// <summary>
        /// Milestone 8.25e. The offer and the answer have disagreed about the same instance, which is
        /// the defect this milestone closes rather than a state to report as a completed action.
        ///
        /// <para>Before this milestone every one of these arms returned "nothing happened", so a player
        /// could be offered rulebook p.10's repeat, take it, and watch the turn move on with the action
        /// unperformed. A seeded driver that only checks that offered commands are accepted passes over
        /// that in silence, which is why it stood for two milestones. Faulting rolls the answer back
        /// through the command transaction and names the kind and the reason.</para>
        /// </summary>
        private static InvalidOperationException RefusedAfterItWasOffered(
            PendingAfterActionRepeat owed, CardActionResolutionRejectionReason reason) =>
            new InvalidOperationException(
                $"An after-action repeat of a {owed.Kind} was offered and taken, and the resolver " +
                $"that would start its second instance refused it as {reason}. Rulebook p.10's " +
                "repeat is offered only where it can be performed, so the offer and the answer have " +
                "disagreed about the same instance.");

        private void AppendRequestedHeroDefeats(
            EffectResolutionStepResult result,
            PendingAttackResolution attack,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> steps) =>
            AppendRequestedHeroDefeats(
                result,
                EffectInvokedDefeatSource.FromAttack(
                    attack ?? throw new ArgumentNullException(nameof(attack))),
                definitions, instances, participants, modifications, steps);

        /// <summary>
        /// Milestone 8.22. The same list, run about whichever action asked for it.
        ///
        /// <para>It was the Attack's until now because every printed text that could escalate into
        /// p.13 was Attack text. Sabina's Covering Fire prints "discards a card, or is defeated" on a
        /// <em>Skill</em>, and a Skill that asked for a Defeat previously changed nothing at all: this
        /// method was reachable only from the two card-text paths.</para>
        /// </summary>
        private void AppendRequestedHeroDefeats(
            EffectResolutionStepResult result,
            EffectInvokedDefeatSource source,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> steps)
        {
            if (!result.IsComplete || result.RequestedHeroDefeats.Count == 0)
            {
                return;
            }

            var materializedInstances = instances as IReadOnlyList<CardInstance> ?? instances.ToArray();
            foreach (var heroId in result.RequestedHeroDefeats)
            {
                var defeat = attackDefenseBackend.ResolveEffectInvokedHeroDefeat(
                    source, heroId, definitions, materializedInstances, participants, modifications,
                    steps);
                if (defeat == null)
                {
                    throw new InvalidOperationException(
                        $"Card text asked for hero '{heroId.Value}' to be defeated, and this " +
                        "composition cannot price a Defeat. A card that can reach p.13's list is " +
                        "dealt only into a match that binds the Levels and Life counters it spends.");
                }
            }
        }
        /// <summary>
        /// Milestone 8.10. Answers the question rulebook p.14's card text stopped on, and - when the
        /// text has finished - hands back the rest of the page.
        ///
        /// <para>The frame resumes through the same <c>EffectResolutionSession.Answer</c> a paused
        /// Skill resumes through, and the effect itself is resolved from the trusted card definitions
        /// rather than from anything the continuation carried, which is why the continuation carries
        /// identity alone. Text that asks a second question simply pauses again: the continuation is
        /// replaced and no steps are produced, so Wasp's riders - which choose a hero and then let that
        /// hero choose a card - stop twice inside one attack without either half being special.</para>
        ///
        /// <para>The steps returned when it finishes are
        /// <see cref="AttackDefenseCardActionStrategyBackend.StepsAfterCardText"/>'s, the same
        /// generator that emits them when nothing paused at all.</para>
        /// </summary>
        public CardActionResolutionResult AnswerCardTextChoice(
            EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (pendingCardText == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingResolution, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = (instances ??
                throw new ArgumentNullException(nameof(instances))).ToArray();
            var effectRoot = SkillCardActionStrategyBackend.FindEffectRoot(
                materializedDefinitions, pendingCardText.Frame);
            if (effectRoot == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer, null, null);
            }

            var context = new EffectResolutionContext(
                effectRoot,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                ActionKind.Attack,
                // Milestone 8.23. The resume half of the same supply the start half makes: p.14's
                // step 4 is the defender's card text and the hero on the other side of it is the
                // attacker. Without it a rider naming "the attacker" resolved to nobody on the way
                // back, so Parry and Riposte opened their discard and then refused every answer to
                // it - the question was askable and unanswerable.
                opposingHeroId: pendingCardText.Moment == CardTextTiming.DefenseAction
                    ? EnclosingAttackOf(pendingCardText.Attack, materializedDefinitions, board).ActorId
                    : default,
                components: components,
                heroItems: skillBackend.HeroItems,
                enclosingAttack: EnclosingAttackOf(pendingCardText.Attack, materializedDefinitions, board),
                rosterState: rosterState,
                // Milestone 8.25d, the resume half of the same bound the start half states: a frame
                // that comes back to a `RepeatAction` node after answering an earlier question is
                // still running inside the same attack, and p.10 bars a repeat of a repeat wherever
                // the node is reached from.
                // Milestone 8.35z46, the resume half of the per-clause bar.
                repeatIsUnavailable: CardTextRepeatIsBarredByPageTen(
                    pendingCardText.Attack, materializedDefinitions),
                // Milestone 8.35z15, the resume half of the same question the start half asks: a
                // frame that comes back to a `RepeatAction` node after answering an earlier question
                // is offered that repeat here, so it is judged here too - and from the board as it
                // now stands, which is a later board than the one the start half read.
                repeatCanBePerformed: binding => CardTextRepeatCanBePerformed(
                    binding,
                    pendingCardText.Attack,
                    materializedDefinitions,
                    materializedInstances,
                    participants,
                    modifications));
            var step = EffectResolutionSession.Answer(pendingCardText.Frame, command, context);
            if (!step.Accepted)
            {
                switch (step.RejectionReason)
                {
                    case EffectResolutionRejectionReason.StaleChoice:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.StaleChoice, null, null);
                    case EffectResolutionRejectionReason.WrongChooser:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.WrongChooser, null, null);
                    default:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.InvalidChoice, null, null);
                }
            }

            var paused = pendingCardText;
            var facts = step.Events
                .Select(value => CardActionResolutionEvent.CardTextApplied(
                    paused.Frame.SourceCardInstanceId,
                    paused.Frame.ActionDefinitionId,
                    paused.Frame.ActorId,
                    paused.Moment,
                    value))
                .Select(ResolutionStep.FromFact)
                .ToList();

            if (step.Frame?.PendingChoice != null)
            {
                pendingCardText = new PendingAttackCardTextResolution(
                    step.Frame, paused.Moment, paused.Attack, paused.Outcome);
                return CardActionResolutionResult.Accept(false, null, null, facts);
            }

            pendingCardText = null;
            AppendRequestedHeroDefeats(
                step, paused.Attack, materializedDefinitions, instances, participants, modifications,
                facts);
            RecordOwedAttackRepeat(step, paused.Attack);

            // Milestone 8.22, finding `F-ATK-02`, the resume half. A card text that asked a question
            // does its work here rather than where it started, so p.8's recorded cause and the Pushes
            // it applied are both produced on this pass - Bullet Hell removes the minions it named
            // only once the player has named them. Recording them where the text began would have
            // caught the faces that ask nothing and missed every face that asks.
            RecordImmediateLanePush(
                step.LanePushCause, paused.Attack.ResolutionId, participants, modifications);
            if (RunCardTextPushTriggers(
                    step, paused.Frame.ActorId, paused.Frame.SourceCardInstanceId,
                    paused.Frame.ActionDefinitionId, paused.Moment, paused.Attack, paused.Outcome,
                    materializedDefinitions, instances, participants, timing, tieBreaker,
                    roundIndex, turnIndex, facts))
            {
                return CardActionResolutionResult.Accept(false, null, null, facts);
            }

            // Milestone 8.19a. Step 1's remainder is the rest of rulebook p.14 rather than a list of
            // facts, so it is produced against live state - including the unit this text named, if it
            // named one. Steps 4 and 7 keep the static generator they have had since 8.10.
            facts.AddRange(paused.Moment == CardTextTiming.BeforeTheAttack
                ? ResumeAttackAfterBeforeText(
                    step.BoundAttackTarget, step.RequestedAttackSequences, materializedDefinitions,
                    instances, participants, modifications)
                : AttackDefenseCardActionStrategyBackend.StepsAfterCardText(
                    paused.Moment, paused.Attack, paused.Outcome,
                    theActionEndsHere: !attackDefenseBackend.SequenceIsOwedAfter(paused.Attack)));
            return CardActionResolutionResult.Accept(true, null, null, facts);
        }

        /// <summary>
        /// Milestone 8.19a. Runs a rule source's `before-action-trigger@1` effect, holding the action it
        /// precedes exactly as the turn validated it.
        ///
        /// <para>Nothing about the held action is started here and nothing about it is re-derived. The
        /// offer is carried because that is the whole point of the altitude: Living Tsunami moves its
        /// hero one space before an Attack, and the offer the turn built <em>before</em> that move is
        /// the one p.10 gave the player.</para>
        /// </summary>
        public CardActionResolutionResult StartBeforeActionTrigger(
            GuardsDigital.Core.Rules.Progression.BeforeActionTriggerSource source,
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (pendingPreAction != null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending, null, null);
            }

            if (!ContentCatalogs.TriggeredEffects.TryGet(source.TriggeredEffect, out var effect))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var context = new EffectResolutionContext(
                effect,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                ActionKind.Skill,
                components: components,
                heroItems: skillBackend.HeroItems,
                rosterState: rosterState);
            var result = EffectResolutionSession.Start(
                new EffectResolutionId(
                    $"before-action-{source.SourceId}-{heldResolutionId.Value}"),
                source.SourceCardInstanceId,
                source.SourceDefinitionReference,
                source.SourceActionDefinitionId,
                source.ControllerId,
                source.ControllerId,
                ActionInvocationContext.PrintedPrimary,
                context);
            if (!result.Accepted)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.InvalidChoice, null, null);
            }

            if (result.Frame?.PendingChoice != null)
            {
                pendingPreAction = new PendingPreActionResolution(
                    result.Frame,
                    PreActionContinuationKind.BeforeActionTrigger,
                    PreActionPlacement.BeforeTheAction,
                    heldResolutionId,
                    heldOffer);
            }

            return CardActionResolutionResult.Accept(
                pendingPreAction == null,
                null,
                null,
                PreActionFacts(result, source.SourceCardInstanceId, source.SourceActionDefinitionId,
                    source.ControllerId, CardTextTiming.None));
        }

        /// <summary>
        /// Milestone 8.19a. Asks the actor which side of this primary action its printed text belongs
        /// to, holding the action while the question stands.
        ///
        /// <para>The definition is entered at its ordering node rather than at its root, and the work
        /// it places is entered later at the node beside it. One definition, two entries, because what
        /// stands between them is a whole action with a continuation of its own - which is exactly what
        /// the shape rule in <c>EffectDefinitionValidator</c> makes the content declare.</para>
        /// </summary>
        public CardActionResolutionResult StartAroundActionText(
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            if (pendingPreAction != null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var action = ActionOfferQuery.FindActionDefinition(heldOffer, materializedDefinitions);
            if (action.CardTextTiming != CardTextTiming.AroundTheAction || action.EffectRoot == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext, null, null);
            }

            return StartAroundActionEntry(
                AroundActionShape.OrderingNode(action.EffectRoot),
                isWorkEntry: false,
                PreActionPlacement.Undecided,
                heldResolutionId,
                heldOffer,
                action,
                materializedDefinitions,
                instances,
                participants,
                timing,
                modifications,
                roundIndex,
                turnIndex,
                heldActionHasStarted: false);
        }

        /// <summary>
        /// Milestone 8.20. Applies a Movement card's printed text at rulebook p.11's default moment -
        /// "Unless it says otherwise, the card text comes into effect before movement" - holding the
        /// Movement while the text runs.
        ///
        /// <para>It enters the definition at its own root rather than at an ordering node, because
        /// there is nothing to order: the page decided the side, so the content declares no placement
        /// question and carries none. What it shares with milestone 8.19a's placed work is everything
        /// else - the held offer and resolution id, the pause the text may open, and the continuation
        /// that starts the Movement once it finishes - which is why it enters through the same seam
        /// rather than through a second one that could drift from it.</para>
        ///
        /// <para>The placement is stated as <see cref="PreActionPlacement.BeforeTheAction"/> and never
        /// asked, so nothing owes work afterwards: a finished text leaves no continuation standing and
        /// the Movement starts.</para>
        /// </summary>
        public CardActionResolutionResult StartBeforeMovementText(
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            if (pendingPreAction != null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var action = ActionOfferQuery.FindActionDefinition(heldOffer, materializedDefinitions);
            if (action.CardTextTiming != CardTextTiming.BeforeTheMovement || action.EffectRoot == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext, null, null);
            }

            return StartAroundActionEntry(
                action.EffectRoot.RootNodeId,
                isWorkEntry: true,
                PreActionPlacement.BeforeTheAction,
                heldResolutionId,
                heldOffer,
                action,
                materializedDefinitions,
                instances,
                participants,
                timing,
                modifications,
                roundIndex,
                turnIndex,
                heldActionHasStarted: false);
        }

        /// <summary>
        /// Milestone 8.35z31. Holds a Movement card's text on the far side of its own walk,
        /// where the card says it belongs.
        ///
        /// <para>p.11's default puts a Movement card's text before the walk and
        /// <see cref="StartBeforeMovementText"/> runs it there; the page's own "unless it says
        /// otherwise" is what this is. Ursafar's three swap faces print "after movement", and a swap is
        /// about the space the walk left him on, so there is nothing to run yet - what this does is
        /// park the continuation that owes the work, exactly as an actor-placed
        /// <c>AfterTheAction</c> answer parks one. The Movement then starts underneath it and the owed
        /// work enters through <see cref="StartOwedAroundActionWork"/> when the walk completes, which is
        /// the same seam and not a second one.</para>
        ///
        /// <para>No frame is created here and no effect node is entered, which is why the returned
        /// result carries no steps: a text that has not begun has produced no facts to report.</para>
        /// </summary>
        public CardActionResolutionResult HoldAfterMovementText(
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            IEnumerable<CardDefinition> definitions)
        {
            if (pendingPreAction != null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var action = ActionOfferQuery.FindActionDefinition(heldOffer, materializedDefinitions);
            if (action.CardTextTiming != CardTextTiming.AfterTheMovement || action.EffectRoot == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext, null, null);
            }

            pendingPreAction = new PendingPreActionResolution(
                null,
                PreActionContinuationKind.AroundPrimaryAction,
                PreActionPlacement.AfterTheAction,
                heldResolutionId,
                heldOffer);
            return CardActionResolutionResult.Accept(
                true, null, null, Array.Empty<ResolutionStep>());
        }

        /// <summary>
        /// Milestone 8.19a. Enters the work the actor placed after the primary action, once that action
        /// has finished. The continuation carried the side and the identity; the work itself is read
        /// from the trusted definition the held offer names, never from anything the payload said.
        /// </summary>
        public CardActionResolutionResult StartOwedAroundActionWork(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            var owed = pendingPreAction;
            if (owed == null || !owed.OwesWork)
            {
                return CardActionResolutionResult.Accept(
                    true, null, null, Array.Empty<ResolutionStep>());
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var action = ActionOfferQuery.FindActionDefinition(
                owed.HeldOffer, materializedDefinitions);
            pendingPreAction = null;

            // Milestone 8.35z31. Two shapes reach this seam and they enter at different nodes.
            // 8.19a's ordering question puts the work in the root sequence's second child, because the
            // first child is the question; a card that states its own side never asks one, so its root
            // IS the work - the same reading StartBeforeMovementText takes of the other side.
            var entryNodeId = action.CardTextTiming == CardTextTiming.AfterTheMovement
                ? action.EffectRoot.RootNodeId
                : AroundActionShape.WorkNode(action.EffectRoot);
            return StartAroundActionEntry(
                entryNodeId,
                isWorkEntry: true,
                owed.Placement,
                owed.HeldResolutionId,
                owed.HeldOffer,
                action,
                materializedDefinitions,
                instances,
                participants,
                timing,
                modifications,
                roundIndex,
                turnIndex,
                owed.HeldActionHasStarted);
        }

        private CardActionResolutionResult StartAroundActionEntry(
            GuardsDigital.Core.Effects.EffectNodeId entryNodeId,
            bool isWorkEntry,
            PreActionPlacement placement,
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            ActionDefinition action,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex,
            bool heldActionHasStarted)
        {
            var context = new EffectResolutionContext(
                action.EffectRoot,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(definitions, instances),
                action.Kind,
                components: components,
                heroItems: skillBackend.HeroItems,
                rosterState: rosterState);
            var result = EffectResolutionSession.Start(
                new EffectResolutionId(
                    $"around-action-{heldOffer.SourceCardInstanceId.Value}-{entryNodeId.Value}"),
                heldOffer.SourceCardInstanceId,
                heldOffer.SourceDefinitionReference,
                action.Id,
                heldOffer.ActorId,
                heldOffer.ActorId,
                ActionInvocationContext.PrintedPrimary,
                context,
                entryNodeId: entryNodeId);
            if (!result.Accepted)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.InvalidChoice, null, null);
            }

            pendingPreAction = ContinuationAfterAroundEntry(
                result, isWorkEntry, placement, heldResolutionId, heldOffer, heldActionHasStarted);
            return CardActionResolutionResult.Accept(
                pendingPreAction == null || pendingPreAction.OwesWork,
                null,
                null,
                PreActionFacts(
                    result, heldOffer.SourceCardInstanceId, action.Id, heldOffer.ActorId,
                    CardTextTiming.AroundTheAction));
        }

        /// <summary>
        /// The continuation an around-action entry leaves behind. Three shapes and no fourth: a frame
        /// still standing on a question, a decided side whose work has not been entered, and nothing at
        /// all once that work is done.
        /// </summary>
        private static PendingPreActionResolution ContinuationAfterAroundEntry(
            EffectResolutionStepResult result,
            bool isWorkEntry,
            PreActionPlacement placement,
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            bool heldActionHasStarted)
        {
            var decided = placement == PreActionPlacement.Undecided
                ? result.AroundActionPlacement
                : placement;
            if (!result.IsComplete)
            {
                return new PendingPreActionResolution(
                    result.Frame, PreActionContinuationKind.AroundPrimaryAction, decided,
                    heldResolutionId, heldOffer, heldActionHasStarted);
            }

            if (isWorkEntry)
            {
                return null;
            }

            if (decided == PreActionPlacement.Undecided)
            {
                throw new InvalidOperationException(
                    "Text applied around a primary action places itself on one side of it; a finished " +
                    "ordering question that placed neither is a composition the definition validator " +
                    "refuses at content load.");
            }

            return new PendingPreActionResolution(
                null, PreActionContinuationKind.AroundPrimaryAction, decided, heldResolutionId,
                heldOffer, heldActionHasStarted);
        }

        /// <summary>
        /// Milestone 8.19a. Answers the question a pre-action frame stopped on, and reports whether the
        /// frame has finished. What happens next belongs to the turn: a finished trigger's held action
        /// starts, and a finished ordering question either runs its work now or waits for the action.
        /// </summary>
        public CardActionResolutionResult AnswerPreActionChoice(
            EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var paused = pendingPreAction;
            if (paused?.Frame == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingResolution, null, null);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var effectRoot = SkillCardActionStrategyBackend.FindEffectRoot(
                materializedDefinitions, paused.Frame);
            if (effectRoot == null &&
                !ContentCatalogs.TriggeredEffects.TryGet(
                    paused.Frame.EffectDefinitionReference, out effectRoot))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer, null, null);
            }

            var enclosingKind = paused.Kind == PreActionContinuationKind.AroundPrimaryAction
                ? ActionOfferQuery.FindActionDefinition(paused.HeldOffer, materializedDefinitions).Kind
                : ActionKind.Skill;
            var context = new EffectResolutionContext(
                effectRoot,
                ContentCatalogs.Capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications,
                EffectCardState.Of(materializedDefinitions, instances),
                enclosingKind,
                components: components,
                heroItems: skillBackend.HeroItems,
                rosterState: rosterState);
            var step = EffectResolutionSession.Answer(paused.Frame, command, context);
            if (!step.Accepted)
            {
                switch (step.RejectionReason)
                {
                    case EffectResolutionRejectionReason.StaleChoice:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.StaleChoice, null, null);
                    case EffectResolutionRejectionReason.WrongChooser:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.WrongChooser, null, null);
                    default:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.InvalidChoice, null, null);
                }
            }

            var timingLabel = paused.Kind == PreActionContinuationKind.AroundPrimaryAction
                ? CardTextTiming.AroundTheAction
                : CardTextTiming.None;
            var facts = PreActionFacts(
                step, paused.Frame.SourceCardInstanceId, paused.Frame.ActionDefinitionId,
                paused.Frame.ActorId, timingLabel);

            if (!step.IsComplete)
            {
                pendingPreAction = paused.WithFrame(step.Frame);
                return CardActionResolutionResult.Accept(false, null, null, facts);
            }

            // A finished trigger leaves nothing standing: the turn starts the action it held. A
            // finished ordering question leaves the side it chose, with the work still owed; a finished
            // piece of work leaves nothing.
            pendingPreAction = ContinuationAfterAroundEntry(
                step,
                isWorkEntry: paused.Kind != PreActionContinuationKind.AroundPrimaryAction ||
                    paused.Placement != PreActionPlacement.Undecided,
                paused.Placement,
                paused.HeldResolutionId,
                paused.HeldOffer,
                paused.HeldActionHasStarted);
            return CardActionResolutionResult.Accept(true, null, null, facts);
        }

        /// <summary>
        /// Milestone 8.19c1. Runs whatever declared an `after-push-trigger@1` for the objects a Skill's
        /// Push has just displaced, or settles its requested minion Defeat through the existing
        /// reward/removal-prevention authority, then carries the Skill on when nothing stops it.
        ///
        /// <para>The loop is the whole shape of the moment. A Push stops the effect at the node it
        /// finished; every source concerned by an object it displaced runs in turn; and when none of
        /// them is left standing the effect carries on from that exact node - which may itself push
        /// again, which is why this repeats rather than running once. The bound is the effect's own:
        /// each pass advances the walk, and a definition that cycles is refused at content load.</para>
        ///
        /// <para>Nothing is derived here that the query cannot answer: which sources apply, and which
        /// of the displaced objects each of them concerns, is <c>AfterPushTriggerQuery</c>'s, asked
        /// against this authority's board and the caller's participants.</para>
        /// </summary>
        private CardActionResolutionResult RunSkillConsequences(
            CardActionResolutionResult result,
            IReadOnlyList<RuleObject> appliedPushes,
            ActionBoundaryContext boundary,
            ref PendingRepeatedAction repeat,
            ref LanePushCauseKind lanePushCause)
        {
            if (!result.Accepted || board == null ||
                ((appliedPushes == null || appliedPushes.Count == 0) && skillBackend.StagedMinionDefeats.Count == 0))
            {
                return result;
            }

            var steps = new List<ResolutionStep>(result.Steps);
            var pushes = appliedPushes ?? Array.Empty<RuleObject>();
            var complete = result.IsComplete;
            while (pushes.Count > 0 || skillBackend.StagedMinionDefeats.Count > 0)
            {
                var interrupted = skillBackend.PendingResolution;
                if (interrupted == null || interrupted.PendingChoice != null)
                {
                    // Nothing is standing to interrupt. A Push whose effect has already finished has
                    // no node to come back to, so the moment simply passes.
                    break;
                }

                var minionDefeats = skillBackend.StagedMinionDefeats;
                foreach (var minionId in minionDefeats)
                {
                    var modifications = CurrentModificationsFor(boundary, interrupted.ActorId);
                    var defeat = attackDefenseBackend.ResolveEffectInvokedMinionDefeat(
                        new ActionResolutionId(interrupted.ResolutionId.Value), interrupted.SourceCardInstanceId,
                        interrupted.SourceDefinitionReference, interrupted.ActionDefinitionId,
                        interrupted.ActorId, minionId, boundary.Participants, modifications,
                        boundary.Definitions, boundary.Instances, out var identity,
                        outcomeDiscriminator: interrupted.CompletedOutcomes.Last().NodeId.Value);
                    if (defeat == null)
                    {
                        steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.RemovalPreventionRequested(
                            identity, attackDefenseBackend.PendingRemovalPrevention)));
                        return CardActionResolutionResult.Accept(false, null, null, steps);
                    }
                    steps.AddRange(defeat.Facts.Select(fact => ResolutionStep.FromFact(
                        CardActionResolutionEvent.MinionDefeatFact(identity, defeat, fact))));
                    if (defeat.LanePush.Cause != LanePushCauseKind.None) lanePushCause = defeat.LanePush.Cause;
                }

                if (minionDefeats.Count > 0 || !TryStartAfterPushTrigger(interrupted, pushes, boundary, steps))
                {
                    var continued = skillBackend.ContinueAfterInterrupt(
                        boundary.Definitions, boundary.Instances, boundary.Participants,
                        boundary.Timing, CurrentModificationsFor(boundary, interrupted.ActorId),
                        components, boundary.RoundIndex, boundary.TurnIndex,
                        out var furtherRepeat, out var furtherCause, out var furtherPushes,
                        out var furtherDefeats, out _);
                    if (!continued.Accepted)
                    {
                        return continued;
                    }

                    steps.AddRange(continued.Steps);
                    AppendSkillRequestedHeroDefeats(
                        furtherDefeats, new ActionResolutionId(interrupted.ResolutionId.Value),
                        interrupted.SourceCardInstanceId, interrupted.ActionDefinitionId,
                        interrupted.ActorId, boundary, steps);
                    complete = continued.IsComplete;
                    if (furtherRepeat != null) repeat = furtherRepeat;
                    if (furtherCause != LanePushCauseKind.None) lanePushCause = furtherCause;
                    pushes = furtherPushes;
                    continue;
                }

                return CardActionResolutionResult.Accept(false, null, null, steps);
            }

            return CardActionResolutionResult.Accept(complete, null, null, steps);
        }

        /// <summary>
        /// Starts the first after-push trigger owed for these displaced objects, and reports whether it
        /// is still standing when this returns. A trigger that finished in one pass leaves nothing and
        /// answers <c>false</c>, exactly as no applicable source does.
        /// </summary>
        private bool TryStartAfterPushTrigger(
            GuardsDigital.Core.Rules.EffectResolution.PendingResolution interrupted,
            IReadOnlyList<RuleObject> pushes,
            ActionBoundaryContext boundary,
            List<ResolutionStep> steps) =>
            TryStartAfterPushTrigger(
                interrupted.ActorId, interrupted.ResolutionId, pushes, boundary, steps);

        /// <summary>
        /// The same, named by the hero whose Pushes these were and by the resolution a standing
        /// trigger would be interrupting. Milestone 8.22 states both, because an Attack.s card text
        /// pushes with no Skill resolution behind it.
        /// </summary>
        private bool TryStartAfterPushTrigger(
            HeroId pushingHeroId,
            GuardsDigital.Core.Rules.EffectResolution.EffectResolutionId interruptedResolutionId,
            IReadOnlyList<RuleObject> pushes,
            ActionBoundaryContext boundary,
            List<ResolutionStep> steps)
        {
            var sources = GuardsDigital.Core.Rules.Progression.AfterPushTriggerQuery.Derive(
                GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                    boundary.Definitions, boundary.Instances),
                board,
                boundary.Participants,
                pushingHeroId,
                pushes);
            for (var index = 0; index < sources.Count; index++)
            {
                var owed = sources.Skip(index + 1).Select(source => source.PushedObject).ToArray();
                if (RunOneAfterPushTrigger(
                        sources[index], interruptedResolutionId, owed, boundary, steps))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// One source's effect, run for one displaced object. Returns whether it paused; a trigger that
        /// resolved in a single pass has already published its facts and left nothing standing.
        /// </summary>
        private bool RunOneAfterPushTrigger(
            GuardsDigital.Core.Rules.Progression.AfterPushTriggerSource source,
            GuardsDigital.Core.Rules.EffectResolution.EffectResolutionId interruptedResolutionId,
            IReadOnlyList<RuleObject> remainingSubjects,
            ActionBoundaryContext boundary,
            List<ResolutionStep> steps)
        {
            if (!ContentCatalogs.TriggeredEffects.TryGet(source.TriggeredEffect, out var effect))
            {
                throw new InvalidOperationException(
                    $"After-push trigger effect '{source.TriggeredEffect}' is not configured.");
            }

            var step = EffectResolutionSession.Start(
                new GuardsDigital.Core.Rules.EffectResolution.EffectResolutionId(
                    source.ResolutionIdValue),
                source.SourceCardInstanceId,
                source.SourceDefinitionReference,
                source.SourceActionDefinitionId,
                source.ControllerId,
                source.ControllerId,
                ActionInvocationContext.PrintedPrimary,
                AfterPushTriggerContext(effect, source, boundary));
            if (!step.Accepted)
            {
                throw new InvalidOperationException(
                    "A configured after-push trigger was refused by its own effect frame.");
            }

            steps.AddRange(PreActionFacts(
                step, source.SourceCardInstanceId, source.SourceActionDefinitionId,
                source.ControllerId, CardTextTiming.None));
            if (step.Frame?.PendingChoice == null)
            {
                // Milestone 8.22. A trigger that finished in one pass has published its request here
                // and nowhere else: the answer path honours the ones that paused, and until this a
                // trigger whose subject had nothing to give simply escalated into nothing. Sabina's
                // Big Sodding Gun reaches it whenever the pushed hero is holding no cards.
                AppendSkillRequestedHeroDefeats(
                    step.RequestedHeroDefeats,
                    new ActionResolutionId(source.ResolutionIdValue),
                    source.SourceCardInstanceId,
                    source.SourceActionDefinitionId,
                    source.ControllerId,
                    boundary,
                    steps);
                return false;
            }

            pendingEffectInterrupt =
                new GuardsDigital.Core.Rules.EffectResolution.PendingEffectInterrupt(
                    step.Frame,
                    GuardsDigital.Core.Rules.EffectResolution.EffectInterruptKind.AfterPushTrigger,
                    source.PushedObject,
                    interruptedResolutionId,
                    remainingSubjects);
            return true;
        }

        /// <summary>
        /// The frame context an after-push trigger runs under. Its opposing hero is the pushed unit
        /// when that unit is a hero, because "that hero" is the whole subject of the printed rider and
        /// the frame's own vocabulary for the other side of a moment is this slot.
        /// </summary>
        private EffectResolutionContext AfterPushTriggerContext(
            EffectDefinition effect,
            GuardsDigital.Core.Rules.Progression.AfterPushTriggerSource source,
            ActionBoundaryContext boundary) =>
            new EffectResolutionContext(
                effect,
                ContentCatalogs.Capabilities,
                boundary.Participants,
                boundary.Timing,
                boundary.RoundIndex,
                boundary.TurnIndex,
                CurrentModificationsFor(boundary, source.ControllerId),
                EffectCardState.Of(boundary.Definitions, boundary.Instances),
                ActionKind.Skill,
                opposingHeroId: IntrinsicKindQuery.Of(source.PushedObject) == RuleObjectKind.Hero
                    ? source.PushedObject.HeroId
                    : default,
                components: components,
                heroItems: skillBackend.HeroItems,
                rosterState: rosterState);

        /// <summary>
        /// The live rules for one acting hero, derived from a boundary context. The unlocked Ultimates
        /// are read from the same cards the boundary carries, so a trigger and the effect it
        /// interrupted are judged against one moment.
        /// </summary>
        private RuleModificationSet CurrentModificationsFor(
            ActionBoundaryContext boundary,
            HeroId actingHeroId) =>
            CurrentModifications(
                boundary.Timing,
                boundary.Participants,
                boundary.TieBreaker,
                GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                    boundary.Definitions, boundary.Instances),
                RuleObject.ForHero(actingHeroId));

        /// <summary>
        /// Milestone 8.19c1. Answers the question a standing after-push trigger asked, and carries the
        /// effect it interrupted on once nothing is left standing.
        /// </summary>
        public CardActionResolutionResult AnswerEffectInterruptChoice(
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command,
            ActionBoundaryContext boundary)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var standing = pendingEffectInterrupt;
            if (standing == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingResolution, null, null);
            }

            if (!ContentCatalogs.TriggeredEffects.TryGet(
                    standing.Frame.EffectDefinitionReference, out var effect))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer, null, null);
            }

            var context = new EffectResolutionContext(
                effect,
                ContentCatalogs.Capabilities,
                boundary.Participants,
                boundary.Timing,
                boundary.RoundIndex,
                boundary.TurnIndex,
                CurrentModificationsFor(boundary, standing.Frame.ActorId),
                EffectCardState.Of(boundary.Definitions, boundary.Instances),
                ActionKind.Skill,
                opposingHeroId: IntrinsicKindQuery.Of(standing.Subject) == RuleObjectKind.Hero
                    ? standing.Subject.HeroId
                    : default,
                components: components,
                heroItems: skillBackend.HeroItems,
                rosterState: rosterState);
            var step = EffectResolutionSession.Answer(standing.Frame, command, context);
            if (!step.Accepted)
            {
                switch (step.RejectionReason)
                {
                    case EffectResolutionRejectionReason.StaleChoice:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.StaleChoice, null, null);
                    case EffectResolutionRejectionReason.WrongChooser:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.WrongChooser, null, null);
                    default:
                        return CardActionResolutionResult.Reject(
                            CardActionResolutionRejectionReason.InvalidChoice, null, null);
                }
            }

            var steps = new List<ResolutionStep>(PreActionFacts(
                step, standing.Frame.SourceCardInstanceId, standing.Frame.ActionDefinitionId,
                standing.Frame.ActorId, CardTextTiming.None));
            if (!step.IsComplete)
            {
                pendingEffectInterrupt = standing.WithFrame(step.Frame);
                return CardActionResolutionResult.Accept(false, null, null, steps);
            }

            pendingEffectInterrupt = null;
            AppendRequestedHeroDefeatsFromInterrupt(step, standing.Frame, boundary, steps);

            // Milestone 8.22, finding `F-ATK-02`. The trigger was set off by a Push inside rulebook
            // p.14's card text, so what it interrupted is the rest of the page. It is handed back
            // through the same generator that emits it when nothing paused at all, which is what keeps
            // a page stopped by a trigger and a page never stopped producing the one same order.
            var owedRemainder = pendingCardTextAwaitingInterrupt;
            if (owedRemainder != null)
            {
                pendingCardTextAwaitingInterrupt = null;
                steps.AddRange(
                    owedRemainder.Moment == CardTextTiming.BeforeTheAttack
                        ? ResumeAttackAfterBeforeText(
                            default, null, boundary.Definitions, boundary.Instances,
                            boundary.Participants,
                            CurrentModificationsFor(boundary, owedRemainder.Attack.ActorId))
                        : AttackDefenseCardActionStrategyBackend.StepsAfterCardText(
                            owedRemainder.Moment, owedRemainder.Attack, owedRemainder.Outcome,
                            theActionEndsHere: !attackDefenseBackend.SequenceIsOwedAfter(
                                owedRemainder.Attack)));
                return CardActionResolutionResult.Accept(true, null, null, steps);
            }

            var interrupted = skillBackend.PendingResolution;
            if (interrupted == null ||
                !interrupted.ResolutionId.Equals(standing.InterruptedResolutionId))
            {
                // The effect this trigger interrupted is no longer standing. Nothing is invented in its
                // place: the trigger's own work is reported and the action is left exactly as the rest
                // of the engine finds it.
                return CardActionResolutionResult.Accept(true, null, null, steps);
            }

            var repeat = default(PendingRepeatedAction);
            var lanePushCause = LanePushCauseKind.None;
            var owed = standing.RemainingSubjects;
            if (owed.Count > 0 && TryStartAfterPushTrigger(interrupted, owed, boundary, steps))
            {
                return CardActionResolutionResult.Accept(false, null, null, steps);
            }

            var continued = skillBackend.ContinueAfterInterrupt(
                boundary.Definitions, boundary.Instances, boundary.Participants, boundary.Timing,
                CurrentModificationsFor(boundary, interrupted.ActorId), components,
                boundary.RoundIndex, boundary.TurnIndex,
                out repeat, out lanePushCause, out var furtherPushes, out var furtherDefeats, out _);
            if (!continued.Accepted)
            {
                return continued;
            }

            steps.AddRange(continued.Steps);
            AppendSkillRequestedHeroDefeats(
                furtherDefeats, new ActionResolutionId(interrupted.ResolutionId.Value),
                interrupted.SourceCardInstanceId, interrupted.ActionDefinitionId,
                interrupted.ActorId, boundary, steps);
            var afterPushes = RunSkillConsequences(
                CardActionResolutionResult.Accept(
                    continued.IsComplete, null, null, Array.Empty<ResolutionStep>()),
                furtherPushes, boundary, ref repeat, ref lanePushCause);
            steps.AddRange(afterPushes.Steps);
            return CloseActionBoundary(
                CardActionResolutionResult.Accept(afterPushes.IsComplete, null, null, steps),
                repeat,
                lanePushCause,
                new ActionResolutionId(interrupted.ResolutionId.Value),
                interrupted.SourceCardInstanceId,
                interrupted.ActionDefinitionId,
                interrupted.ActorId,
                boundary);
        }

        /// <summary>
        /// Rulebook p.13's Defeat list, run for any hero a finished after-push trigger asked about.
        /// It is the same request every other effect frame publishes, honoured through the same backend
        /// - Sabina's rider escalates to a Defeat when the pushed hero has no card to give.
        /// </summary>
        /// <summary>
        /// Milestone 8.22. Rulebook p.13's list, run for any hero a finished <em>Skill</em> asked
        /// about.
        ///
        /// <para>Sabina's Covering Fire prints "An enemy hero in radius adjacent to your friendly
        /// minion discards a card, or is defeated" - the same construction Wasp's Electroblast and
        /// Brogan's Counterattack print, and the first printing of it on a Skill. Until this, the
        /// request a Skill's frame published was collected by the effect session and honoured by
        /// nobody: p.13 was reachable only from the two card-text paths and from an interrupt with a
        /// Skill standing behind it.</para>
        /// </summary>
        /// <summary>
        /// The same list, spliced into a Skill's own result so p.13's facts land in the stream the
        /// action produced rather than beside it.
        /// </summary>
        private CardActionResolutionResult WithSkillRequestedHeroDefeats(
            CardActionResolutionResult result,
            IReadOnlyList<HeroId> requestedHeroDefeats,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionBoundaryContext boundary)
        {
            if (!result.Accepted || requestedHeroDefeats == null || requestedHeroDefeats.Count == 0)
            {
                return result;
            }

            var steps = new List<ResolutionStep>(result.Steps);
            AppendSkillRequestedHeroDefeats(
                requestedHeroDefeats, resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                boundary, steps);
            return CardActionResolutionResult.Accept(result.IsComplete, null, null, steps);
        }

        /// <summary>
        /// Milestone 8.22, finding `F-ATK-02`. Runs the after-push triggers owed for the Pushes an
        /// Attack's card text applied.
        ///
        /// <para><b>Why it is not <see cref="RunSkillConsequences"/>.</b> That one interrupts a
        /// standing Skill and carries it on from the node it left; there is no Skill here and the text
        /// that pushed has already finished, so what a trigger interrupts at this altitude is the rest
        /// of rulebook p.14 rather than an effect. The trigger's own machinery is otherwise the same
        /// one - the same query, the same frame, the same
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.PendingEffectInterrupt"/> - because a
        /// second copy is how the two altitudes would come to disagree about when a Push is a Push.
        /// </para>
        ///
        /// <para>A trigger that pauses stops the page: <see cref="pendingCardTextAwaitingInterrupt"/>
        /// holds the remainder, the executor sees that the page is stopped, and the remainder is
        /// regenerated by the same generator that emits it when nothing paused at all.</para>
        /// </summary>
        /// <returns>Whether a trigger is standing, which stops the page.</returns>
        private bool RunCardTextPushTriggers(
            EffectResolutionStepResult result,
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            CardTextTiming moment,
            PendingAttackResolution remainderAttack,
            AttackResolutionOutcome remainderOutcome,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex,
            List<ResolutionStep> steps)
        {
            if (pendingCardText != null || board == null ||
                result.AppliedPushes == null || result.AppliedPushes.Count == 0)
            {
                return false;
            }

            var boundary = new ActionBoundaryContext(
                definitions, instances, participants, timing, tieBreaker, roundIndex, turnIndex);
            if (!TryStartAfterPushTrigger(
                    actorId,
                    new GuardsDigital.Core.Rules.EffectResolution.EffectResolutionId(
                        $"card-text-{sourceCardInstanceId.Value}-" +
                        $"{actionDefinitionId.Value}-{moment}"),
                    result.AppliedPushes,
                    boundary,
                    steps))
            {
                return false;
            }

            // The trigger is standing. p.14 stops here and the rest of the page is regenerated when it
            // finishes - which is exactly what milestone 8.10 does when the text itself pauses.
            pendingCardTextAwaitingInterrupt = remainderAttack == null
                ? null
                : new PendingAttackCardTextRemainder(moment, remainderAttack, remainderOutcome);
            return pendingCardTextAwaitingInterrupt != null;
        }

        /// <summary>
        /// Whether any unlocked Ultimate is watching rulebook p.18's Pushes. The effect session only
        /// reports the objects a step displaced when something is listening, so this is asked wherever
        /// an effect is started - the same derivation the Skill altitude makes.
        /// </summary>
        private static bool WatchesAppliedPushes(
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(definitions, instances)
                .Ultimates.Any(ultimate => ultimate.Rule.AfterPush.IsDeclared);

        private void AppendSkillRequestedHeroDefeats(
            IReadOnlyList<HeroId> requestedHeroDefeats,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionBoundaryContext boundary,
            ICollection<ResolutionStep> steps)
        {
            if (requestedHeroDefeats == null || requestedHeroDefeats.Count == 0)
            {
                return;
            }

            var source = EffectInvokedDefeatSource.FromSkill(
                resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
            var modifications = CurrentModificationsFor(boundary, actorId);
            foreach (var heroId in requestedHeroDefeats)
            {
                var defeat = attackDefenseBackend.ResolveEffectInvokedHeroDefeat(
                    source, heroId, boundary.Definitions, boundary.Instances, boundary.Participants,
                    modifications, steps);
                if (defeat == null)
                {
                    throw new InvalidOperationException(
                        $"A Skill asked for hero '{heroId.Value}' to be defeated, and this " +
                        "composition cannot price a Defeat. A card that can reach p.13's list is " +
                        "dealt only into a match that binds the Levels and Life counters it spends.");
                }
            }
        }

        /// <remarks>
        /// Milestone 8.22 identifies the Defeat by the <em>trigger's</em> own frame rather than by the
        /// effect it interrupted. Two things were wrong with reading the interrupted one: it made the
        /// events name a card that had asked for nothing, and it returned without running p.13 at all
        /// when nothing was standing to interrupt - which is every trigger fired from an Attack's card
        /// text, because that altitude has no Skill resolution behind it.
        /// </remarks>
        private void AppendRequestedHeroDefeatsFromInterrupt(
            EffectResolutionStepResult step,
            GuardsDigital.Core.Rules.EffectResolution.PendingResolution trigger,
            ActionBoundaryContext boundary,
            List<ResolutionStep> steps)
        {
            if (step.RequestedHeroDefeats.Count == 0 || trigger == null)
            {
                return;
            }

            AppendRequestedHeroDefeats(
                step,
                EffectInvokedDefeatSource.FromSkill(
                    new ActionResolutionId(trigger.ResolutionId.Value),
                    trigger.SourceCardInstanceId,
                    trigger.ActionDefinitionId,
                    trigger.ActorId),
                boundary.Definitions,
                boundary.Instances,
                boundary.Participants,
                CurrentModificationsFor(boundary, trigger.ActorId),
                steps);
        }

        /// <summary>
        /// Milestone 8.19c1's Defeat standing at its removal stage, or <c>null</c>.
        /// </summary>
        public PendingRemovalPreventionDecision PendingRemovalPrevention =>
            attackDefenseBackend.PendingRemovalPrevention;

        /// <summary>
        /// Milestone 8.19c1. Which sources are still offering to stop the standing removal, and what
        /// each would cost, derived against live state. Empty when no decision is standing.
        ///
        /// <para>It is derived rather than stored for the reason the answer re-derives: a reader that
        /// drew a stale list would offer a card the hand no longer holds.</para>
        ///
        /// <para><b>Milestone 8.37, checked rather than assumed.</b> This is the third of
        /// the three sites that ask <see cref="RemovalStageDecision.OfferedPreventions"/>, and it needs no
        /// change: <see cref="RemovalPreventionQuery.Offered"/> already skips every prevention whose price
        /// is not a card, so a mandatory source such as a totem's save was never in this list to begin
        /// with. The wiring that matters is upstream, at the site that decides whether a pause opens at
        /// all - a mandatory prevention resolves the removal before this description could ever be asked
        /// for it.</para>
        /// </summary>
        public IReadOnlyList<RemovalPreventionOption> DescribeRemovalPreventionOptions(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var standing = attackDefenseBackend.PendingRemovalPrevention;
            if (standing == null || board == null)
            {
                return Array.Empty<RemovalPreventionOption>();
            }

            return new RemovalStageDecision(
                    standing.Activity,
                    RuleObject.ForHero(standing.Resolution.ActorId),
                    board,
                    board,
                    participants,
                    modifications,
                    GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.Defeat)
                .OfferedPreventions(
                    GuardsDigital.Core.Components.BoardObjectReference.ForMinion(
                        standing.SubjectMinionId),
                    definitions,
                    instances)
                .Where(offer => offer.DecidingHeroId.Equals(standing.DecidingHeroId))
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.19c1. Answers the standing removal-prevention decision, then finishes the Defeat
        /// and whatever its origin still owes.
        ///
        /// <para>The two origins owe different remainders and both are produced from the identity the
        /// pause carries: rulebook p.14's minion arm owes the rest of the page, and an effect-invoked
        /// Defeat owes its own facts and p.8's recorded cause. Neither carries a list of steps across
        /// the wire, which is milestone 8.10's rule for every pause in this engine.</para>
        /// </summary>
        public CardActionResolutionResult AnswerRemovalPrevention(
            RemovalPreventionCommand command,
            ActionBoundaryContext boundary,
            RuleModificationSet modifications)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var defeat = attackDefenseBackend.AnswerRemovalPrevention(
                command, boundary.Participants, modifications, boundary.Definitions,
                boundary.Instances, out var answered, out var paidCard, out var rejection);
            if (defeat == null)
            {
                return CardActionResolutionResult.Reject(rejection, null, null);
            }

            var steps = new List<ResolutionStep>();
            if (paidCard.IsDefined)
            {
                steps.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.RemovalPreventionPaid(
                        answered.Resolution, answered, paidCard)));
            }

            steps.AddRange(defeat.Facts.Select(fact => ResolutionStep.FromFact(
                CardActionResolutionEvent.MinionDefeatFact(answered.Resolution, defeat, fact))));

            if (answered.Origin == RemovalPreventionOrigin.EffectInvokedMinionDefeat)
            {
                var skill = skillBackend.PendingResolution;
                if (skill != null && skill.ResolutionId.Value == answered.Resolution.ResolutionId.Value)
                {
                    var continued = skillBackend.ContinueAfterInterrupt(
                        boundary.Definitions, boundary.Instances, boundary.Participants, boundary.Timing,
                        CurrentModificationsFor(boundary, skill.ActorId), components,
                        boundary.RoundIndex, boundary.TurnIndex, out var repeat, out var cause,
                        out var pushes, out var heroDefeats, out var borrow);
                    if (!continued.Accepted) return continued;
                    RecordOwedBorrowedAction(borrow, skill.ActorId);
                    if (defeat.LanePush.Cause != LanePushCauseKind.None) cause = defeat.LanePush.Cause;
                    steps.AddRange(continued.Steps);
                    var resumed = WithSkillRequestedHeroDefeats(
                        CardActionResolutionResult.Accept(continued.IsComplete, null, null, steps),
                        heroDefeats, new ActionResolutionId(skill.ResolutionId.Value),
                        skill.SourceCardInstanceId, skill.ActionDefinitionId, skill.ActorId, boundary);
                    resumed = RunSkillConsequences(resumed, pushes, boundary, ref repeat, ref cause);
                    return CloseActionBoundary(resumed, repeat, cause, new ActionResolutionId(skill.ResolutionId.Value),
                        skill.SourceCardInstanceId, skill.ActionDefinitionId, skill.ActorId, boundary);
                }
                RecordImmediateLanePush(
                    defeat.LanePush.Cause,
                    new ActionResolutionId(answered.Resolution.ResolutionId.Value),
                    boundary.Participants,
                    modifications);
                triggeredResolutionSteps.AddRange(steps);
                return CardActionResolutionResult.Accept(
                    true, null, null, Array.Empty<ResolutionStep>());
            }

            steps.AddRange(attackDefenseBackend.StepsAfterMinionDefeat(
                answered.Resolution, boundary.Definitions));
            return CloseActionBoundary(
                CardActionResolutionResult.Accept(true, null, null, steps),
                null,
                defeat.LanePush.Cause,
                new ActionResolutionId(answered.Resolution.ResolutionId.Value),
                answered.Resolution.SourceCardInstanceId,
                answered.Resolution.ActionDefinitionId,
                answered.Resolution.ActorId,
                boundary);
        }

        /// <summary>Forgets a pre-action continuation whose held action the turn has now started.</summary>
        public void ClearPreActionContinuation() => pendingPreAction = null;

        /// <summary>Records that the held action has started and the placed work is still owed.</summary>
        public void ParkPreActionContinuation() => pendingPreAction = pendingPreAction?.Parked();

        private static IReadOnlyList<ResolutionStep> PreActionFacts(
            EffectResolutionStepResult result,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            CardTextTiming timing) =>
            Array.AsReadOnly(result.Events
                .Select(value => CardActionResolutionEvent.CardTextApplied(
                    sourceCardInstanceId, actionDefinitionId, actorId, timing, value))
                .Select(ResolutionStep.FromFact)
                .ToArray());

        public AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            // Read the identity before the answer: an answer that resolves the Attack clears the
            // pending resolution, and the request has to name the action instance that caused it.
            var resolutionId = attackDefenseBackend.PendingResolution?.ResolutionId ?? default;
            var result = attackDefenseBackend.AnswerAttackTarget(
                command, definitions, instances, participants, modifications);
            return RecordAttackLanePush(result, resolutionId, participants, modifications);
        }

        public AttackActionResolutionResult AnswerDefenseResponse(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            attackDefenseBackend.AnswerDefense(
                command, definitions, instances, participants, modifications);

        public AttackActionResolutionResult ResolveAttackOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var resolutionId = attackDefenseBackend.PendingResolution?.ResolutionId ?? default;
            var result = attackDefenseBackend.ResolveOutcome(
                definitions, instances, participants, modifications);
            return RecordAttackLanePush(result, resolutionId, participants, modifications);
        }

        /// <summary>
        /// Milestone 6.9: records the cause a completed Attack's Defeat stage reported.
        ///
        /// p.13's Defeat is not an effect node, so it cannot publish its answer into an effect frame the
        /// way p.19's Remove does; it carries a whole <c>LanePushQualification</c> on its resolution
        /// instead, and this is where that record becomes p.8's request. An Attack has no after-action
        /// correction boundary of its own - nothing it does displaces a minion - so the completion of
        /// its outcome is the boundary.
        ///
        /// Milestone 7.9c, finding F-BOARD-01: both Defeat arms are read. Only the minion arm was, so a
        /// hero Defeat's qualification was computed by the shared removal stage, recorded on the
        /// resolution, and consumed by nothing - and a hero that p.17 makes count as a minion is exactly
        /// the case where p.8's sentence applies to the hero arm. Each arm is asked separately rather
        /// than by picking one: an Attack resolves against a single target, so at most one is present,
        /// and <see cref="RecordImmediateLanePush"/> keeps the first request either way.
        /// </summary>
        private AttackActionResolutionResult RecordAttackLanePush(
            AttackActionResolutionResult result,
            ActionResolutionId resolutionId,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (!result.Accepted)
            {
                return result;
            }

            if (result.MinionDefeat != null)
            {
                RecordImmediateLanePush(
                    result.MinionDefeat.LanePush.Cause, resolutionId, participants, modifications);
            }

            if (result.HeroDefeat != null)
            {
                RecordImmediateLanePush(
                    result.HeroDefeat.LanePush.Cause, resolutionId, participants, modifications);
            }

            return result;
        }

        /// <summary>
        /// Forgets the mid-turn request p.8's transition has just finished (milestone 6.11).
        ///
        /// Consuming it is the round's business rather than this authority's - the transition is a
        /// round-scoped mechanism and the request is only held here because the action that caused it
        /// was - so this says nothing about when, and only the checkpoint that ran the push calls it.
        /// A call with nothing held is a no-op rather than a failure, because the end-of-round
        /// checkpoint clears its own sibling request through the same path.
        /// </summary>
        internal void ConsumeImmediateLanePush()
        {
            pendingLanePush = null;
        }

        /// <summary>
        /// Milestone 8.35z21. The held request re-read from the board as it stands now, or <c>null</c>
        /// when p.8's winner sentence no longer has a subject here.
        ///
        /// <para>p.8 names the winner by looking - "The team with one or more minions still remaining
        /// in the Battle Zone wins the push" - and the designer's 2026-06-18 ruling puts the push at
        /// the end of the action, so the look belongs there rather than where the cause was recorded.
        /// Between the two, p.14 step 7's "After the attack:" text runs and can move, place or remove
        /// minions in the zone, so the two answers can genuinely differ.</para>
        ///
        /// <para>The <em>cause</em> is not re-derived and could not be: nothing about an emptied zone
        /// distinguishes p.8's two sentences, which is the division of labour milestone 6.6
        /// established and <see cref="ImmediateLanePushQuery"/> keeps. So this asks the same query the
        /// record asked, with the same cause, on today's board - which also means it mints the same
        /// <c>LanePushRequestId</c>, and a replay of the same commands still names the same push.</para>
        /// </summary>
        public PendingImmediateLanePushRequest DeriveImmediateLanePushAtEndOfAction(
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var held = pendingLanePush;
            return held == null
                ? null
                : ImmediateLanePushQuery.From(
                    held.Cause, held.CauseResolutionId, board, participants, modifications);
        }

        /// <summary>
        /// The one place a mid-turn Lane-Push request is produced.
        ///
        /// A conformance rule holds it to being the only one, in the shape milestone 6.6 used for p.8's
        /// question itself: moving, pushing, swapping, placing, spawning and replacing record no cause,
        /// so they cannot reach this, and nothing else can build a request.
        ///
        /// <para>A request already held is never replaced, and since milestone 8.35z21 that is exactly
        /// one request per <em>action instance</em>. The turn authority drains it at the end of the
        /// instance that recorded it, so a second cause reaching this can only be a second cause inside
        /// the same instance - and p.8 describes one push per emptied side rather than one per piece
        /// taken, which is why the first is kept. Before 8.35z21 the same guard read as one request per
        /// card resolution, and its stated reason - "the turn is stopped from the moment the first one
        /// is recorded" - was false across a repeat: the stop is <c>GetCommonRejection</c>, which only
        /// bites while <c>HasPendingResolution</c> is false, and a running repeat instance makes it
        /// true. A qualifying cause in the second instance was therefore dropped where the ruling owes
        /// a second push. It is not dropped now, because the first request is gone before the second
        /// instance starts.</para>
        /// </summary>
        private void RecordImmediateLanePush(
            LanePushCauseKind cause,
            ActionResolutionId resolutionId,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (cause == LanePushCauseKind.None || pendingLanePush != null || !resolutionId.IsDefined)
            {
                return;
            }

            pendingLanePush = ImmediateLanePushQuery.From(
                cause, resolutionId, board, participants, modifications);
        }

        private PreparedStart PrepareStart(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            ActionKind expectedKind,
            bool turnOnly)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Starting a card action resolution requires a resolution ID.", nameof(resolutionId));
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            if (HasPendingResolution)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    materializedDefinitions,
                    materializedInstances);
            }

            // Milestone 8.35z50 admits `TextGranted` here too, and this is the SHARED funnel every
            // Attack, Movement and Skill start passes through - so widening it is what lets a borrowed
            // primary of any kind begin. The offer names the borrowed card, which is what makes the
            // borrowed action the borrowed card's own in every respect (RULING 36).
            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary &&
                offer.InvocationContext != ActionInvocationContext.TextGranted)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext,
                    materializedDefinitions,
                    materializedInstances);
            }

            var authoritativeOffers = turnOnly
                ? ActionOfferQuery.GetAvailableTurnActionOffers(
                    materializedDefinitions,
                    materializedInstances,
                    offer.ActorId,
                    offer.SourceCardInstanceId)
                : ActionOfferQuery.GetAvailableOffers(
                    materializedDefinitions,
                    materializedInstances,
                    offer.ActorId,
                    ActionOfferWindow.NormalPlay);
            if (!offer.IsAvailable || (!authoritativeOffers.Contains(offer) &&
                !IsAuthorizedBorrowedOffer(offer, materializedDefinitions)))
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer,
                    materializedDefinitions,
                    materializedInstances);
            }

            if (ActionOfferQuery.FindActionDefinition(offer, materializedDefinitions).Kind != expectedKind)
            {
                return PreparedStart.Reject(
                    CardActionResolutionRejectionReason.UnsupportedActionKind,
                    materializedDefinitions,
                    materializedInstances);
            }

            return PreparedStart.Accept(materializedDefinitions, materializedInstances);
        }

        private readonly struct PreparedStart
        {
            private PreparedStart(
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                CardActionResolutionRejectionReason? rejectionReason)
            {
                Definitions = definitions;
                Instances = instances;
                RejectionReason = rejectionReason;
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }

            internal IReadOnlyList<CardInstance> Instances { get; }

            internal CardActionResolutionRejectionReason? RejectionReason { get; }

            internal static PreparedStart Accept(
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances) =>
                new PreparedStart(definitions, instances, null);

            internal static PreparedStart Reject(
                CardActionResolutionRejectionReason reason,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances) =>
                new PreparedStart(definitions, instances, reason);
        }
    }
}
