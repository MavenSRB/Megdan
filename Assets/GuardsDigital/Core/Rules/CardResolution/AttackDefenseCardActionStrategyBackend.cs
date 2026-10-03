using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    internal sealed class AttackDefenseCardActionStrategyBackend
    {
        private readonly AdjacentMoveAuthority adjacentMoveAuthority;
        private PendingAttackResolution pendingAttackResolution;
        private PendingAttackTargetChoice pendingAttackTargetChoice;
        private PendingDefenseResponseChoice pendingDefenseChoice;

        /// <summary>
        /// Milestone 8.35z27. The modal Attack action now running more than one complete
        /// rulebook p.14 sequence, or <c>null</c> - which is every Attack in this build outside a card
        /// printing "choose one, or both".
        ///
        /// <para>It is held beside <see cref="pendingAttackResolution"/> rather than above it because
        /// the two answer different questions and outlive each other in both directions: a sequence's
        /// resolution is null between the outcome of one and the target choice of the next, and the
        /// chain is null for every ordinary Attack. What the chain adds to the resolution is the two
        /// facts p.14 cannot re-derive - which bullet's sentence this sequence targets by, and which
        /// units the earlier sequences bound.</para>
        /// </summary>
        private PendingAttackSequenceChain pendingSequenceChain;

        private HeroCoinLedger coinLedger;
        private EffectRosterState rosterState = EffectRosterState.Absent;

        /// <summary>
        /// Milestone 8.19c1: a Defeat standing at its removal stage while the hero controlling a
        /// prevention source decides whether to pay for it, or <c>null</c>.
        ///
        /// It is held here rather than beside the attack continuation because both of this build's
        /// minion Defeats reach it and only one of them is inside an Attack: the effect-invoked Defeat
        /// has no pending attack at all, and a continuation kept on that slot would have had nowhere to
        /// live for half the cases the rule covers.
        /// </summary>
        private PendingRemovalPreventionDecision pendingRemovalPrevention;

        /// <summary>
        /// Milestone 6.12: the same register the round authority holds, so rulebook p.13's "Any markers
        /// they had are returned" happens where the Defeat is recorded rather than at some later
        /// checkpoint that would have to rediscover which hero was defeated. Absent outside a live match,
        /// where there is no attachment to return.
        /// </summary>
        private readonly MarkerAttachmentRegister markers;

        /// <summary>
        /// Milestone 7.2: the same item state the round authority holds, for the same reason
        /// <see cref="markers"/> is the same register - a Defense value that consulted a different copy
        /// of a hero's items than the one the match records would be a total nothing could reproduce.
        /// Absent outside a live match, where no hero has any.
        ///
        /// Milestone 7.2a: the Attack Total and a Ranged Attack's Range Total now read it too. Both
        /// were already computed here from the same printed query the Defense total uses, so composing
        /// them is a field this backend already held rather than any new derivation - which is why
        /// neither the attack resolution's stored value nor any resolver signature widened.
        /// </summary>
        private GuardsDigital.Core.Rules.Progression.HeroItemState heroItems;

        /// <summary>
        /// The match's Active-effect lifecycle, or <c>null</c> in a composition that holds none.
        /// Milestone 8.38c1; bound after construction by <see cref="BindActiveEffects"/>.
        /// </summary>
        private ActiveEffectLifecycle activeEffects;

        /// <summary>
        /// Milestone 8.61b. The same capability catalog the dispatcher composed every backend against,
        /// bound after construction by <see cref="BindCapabilities"/> because this backend is built
        /// inside the dispatcher's own constructor argument list.
        ///
        /// <para>It is read at exactly one place - p.14's Block condition, for Min the Dragonmonk's
        /// three faces whose condition is the precondition of the swap their Defense text performs. The
        /// token that swap names is declared by the capability the text consumes, so the catalog is what
        /// says which piece "her Smoke bomb" is; nothing else here asks it anything. Absent outside a
        /// live match, where no card text can run either.</para>
        /// </summary>
        private GuardsDigital.Core.Effects.EffectCapabilityCatalog capabilities;

        /// <summary>
        /// Milestone 7.4: the same per-hero Levels the round authority holds. p.13 prices a hero Defeat
        /// at "coins equal to that hero's level", so the reward cannot be derived without them, and a
        /// second copy would price a Defeat against a Level the match does not record.
        /// </summary>
        private GuardsDigital.Core.Rules.Progression.HeroProgressionState heroLevels;

        /// <summary>
        /// Milestone 7.4: the same Life counters the round authority holds. p.13's spend happens inside
        /// the Attack that caused the Defeat - p.14's numbered order puts the removal at step 6, before
        /// the Attack card's "After the attack" text at step 7 - so the counters are spent here rather
        /// than at a later checkpoint that would have to rediscover who was defeated.
        /// </summary>
        private TeamLifeState teamLives;

        /// <summary>
        /// Milestone 8.19a1: the round-owned typed history of completed hero Defeats. It is bound beside
        /// the consequence state because this backend is composed before the round that owns all three.
        /// Absent only for dispatcher-only fixtures with no round context.
        /// </summary>
        private RoundHeroDefeatHistory heroDefeatHistory;

        internal AttackDefenseCardActionStrategyBackend(AdjacentMoveAuthority adjacentMoveAuthority)
            : this(adjacentMoveAuthority, null, null)
        {
        }

        internal AttackDefenseCardActionStrategyBackend(
            AdjacentMoveAuthority adjacentMoveAuthority,
            HeroCoinLedger coinLedger)
            : this(adjacentMoveAuthority, coinLedger, null)
        {
        }

        internal AttackDefenseCardActionStrategyBackend(
            AdjacentMoveAuthority adjacentMoveAuthority,
            HeroCoinLedger coinLedger,
            MarkerAttachmentRegister markers)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
            this.coinLedger = coinLedger;
            this.markers = markers;
        }

        internal PendingAttackResolution PendingResolution => pendingAttackResolution;

        internal PendingAttackTargetChoice PendingTargetChoice => pendingAttackTargetChoice;

        internal PendingDefenseResponseChoice PendingDefenseChoice => pendingDefenseChoice;

        internal void SuspendDefenseChoice()
        {
            if (pendingAttackResolution?.Phase != AttackResolutionPhase.DefenseResponse ||
                pendingDefenseChoice == null)
                throw new InvalidOperationException("Only an open Defense choice can be interrupted.");
            pendingDefenseChoice = null;
        }

        internal PendingDefenseResponseChoice ReopenDefenseChoice(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            RuleModificationSet modifications,
            MatchParticipants participants)
        {
            if (pendingAttackResolution?.Phase != AttackResolutionPhase.DefenseResponse ||
                pendingDefenseChoice != null)
                throw new InvalidOperationException("The interrupted Defense choice must be absent.");
            var attack = FindPendingAttackOffer(
                pendingAttackResolution, definitions, instances);
            if (!attack.HasValue)
                throw new InvalidOperationException("The interrupted Attack source is unavailable.");
            var offers = FindDefenseOffers(definitions, instances,
                pendingAttackResolution.TargetHeroId, modifications, attack.Value, participants);
            pendingDefenseChoice = new PendingDefenseResponseChoice(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.CurrentStepId,
                pendingAttackResolution.TargetHeroId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.AttackTotal,
                offers, true);
            return pendingDefenseChoice;
        }

        internal AttackActionResolutionResult FinishAfterDefenderRemovedBeforeDefense()
        {
            if (pendingAttackResolution?.Phase != AttackResolutionPhase.DefenseResponse ||
                // Milestone 9.21a: the attacked BODY, the same question the turn authority asks.
                adjacentMoveAuthority.TryGetHeroBodyCoordinate(
                    pendingAttackResolution.TargetHeroId, pendingAttackResolution.Target.HeroBody, out _))
                throw new InvalidOperationException(
                    "Only a Defense window whose target was removed can end without a response.");
            return StopAttackWithoutATarget(
                pendingAttackResolution, new List<ResolutionStep>());
        }

        internal bool HasPendingResolution => pendingAttackResolution != null;

        /// <summary>
        /// Milestone 8.35z27. The modal Attack chain this backend is part-way through, or
        /// <c>null</c>.
        /// </summary>
        internal PendingAttackSequenceChain PendingSequenceChain => pendingSequenceChain;

        /// <summary>
        /// Whether a modal Attack still owes rulebook p.14 for a bullet its actor chose.
        ///
        /// <para>This is the whole of what makes the contract one action. Everything the end of an
        /// action owes - p.8's mid-turn Lane Push, the card's resolution, Initiative's advance,
        /// p.10's repeat offers - waits on this reading false, and nothing between two sequences may
        /// read the action as over.</para>
        /// </summary>
        internal bool HasOwedAttackSequence => pendingSequenceChain?.HasNextSequence == true;

        internal HeroCoinLedger CoinLedger => coinLedger;

        /// <summary>
        /// Milestone 7.5. The match's coin ledger, created here if a Defeat has not already created it.
        ///
        /// It exists because coins stopped being a resource only this backend touches. p.15's level
        /// purchase spends them, so the round authority asks for the ledger when it composes itself, and
        /// a second one created later by a Defeat would be a balance no restore could reproduce. The
        /// creation stays in this type rather than moving to the round for the reason
        /// <see cref="CoinLedger"/> lives here at all: the Defeat that fills it is this backend's.
        /// </summary>
        internal HeroCoinLedger EnsureCoinLedger(MatchParticipants participants) =>
            EnsureCoinLedger(participants, null);

        /// <summary>
        /// Milestone 8.35z17. The overload above with the balances a staged situation begins
        /// from. The seed is read only on the creation this method would have made anyway, so a
        /// ledger that already exists is returned untouched and no staged number can overwrite a
        /// balance p.13 or p.15 has moved.
        /// </summary>
        internal HeroCoinLedger EnsureCoinLedger(
            MatchParticipants participants, HeroCoinLedgerRestoreState seed)
        {
            if (coinLedger == null)
            {
                if (participants == null) throw new ArgumentNullException(nameof(participants));
                coinLedger = seed != null
                    ? HeroCoinLedger.Restore(participants, seed)
                    : new HeroCoinLedger(participants);
            }

            return coinLedger;
        }

        internal GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems => heroItems;

        /// <summary>
        /// Hands this backend the match's item state. It arrives after construction because the round
        /// authority is what owns the state and the dispatcher it composes with is built before it; the
        /// alternative was a second <see cref="GuardsDigital.Core.Rules.Progression.HeroItemState"/>
        /// created here, which is the one outcome that must not happen.
        /// </summary>
        internal void BindHeroItems(GuardsDigital.Core.Rules.Progression.HeroItemState items)
        {
            heroItems = items;
        }

        /// <summary>
        /// Hands this backend the match's Active-effect lifecycle, on exactly the terms
        /// <see cref="BindHeroItems"/> is handed the item state and for the same reason: the round
        /// authority owns it and is built after the dispatcher this backend belongs to.
        ///
        /// <para>Milestone 8.38c1 needs it because a standing bounty is spent BY a Defeat - see
        /// <see cref="MinionDefeatBountyQuery"/> - and p.13's Defeat is resolved here. A composition
        /// that never binds one simply pays no bounties, which is every board in this build before
        /// Swift's two Green faces exist.</para>
        /// </summary>
        internal void BindActiveEffects(ActiveEffectLifecycle effects)
        {
            activeEffects = effects;
        }

        /// <summary>
        /// Hands this backend the dispatcher's own capability catalog, on the same terms
        /// <see cref="BindActiveEffects"/> is handed the lifecycle. Milestone 8.61b.
        ///
        /// <para>It is the dispatcher's own object rather than a second lookup, which is what keeps
        /// p.14's Block condition and the swap it is the precondition of reading one declaration: the
        /// preview is built from the configuration's catalog and the dispatcher is composed from it, so
        /// the two cannot name different pieces.</para>
        /// </summary>
        internal void BindCapabilities(GuardsDigital.Core.Effects.EffectCapabilityCatalog catalog)
        {
            capabilities = catalog;
        }

        /// <summary>
        /// Pays whatever standing bounties this Defeat spends. Milestone 8.38c1.
        ///
        /// <para>Called from both of p.13's minion-Defeat paths - the Attack's and the effect-invoked
        /// one - immediately after the page's own reward, which is the moment the Defeat is complete
        /// and before the removal stage that may decide the miniature stays. That ordering is p.13's
        /// own: its Bolster example prevents a removal and collects the coins in the same breath, so a
        /// bounty on "an enemy minion is defeated" is owed by a Defeat whose removal is prevented
        /// exactly as much as by one whose removal happens.</para>
        ///
        /// <para>The round and turn come from the bound roster state's newest slot, because a Defeat is
        /// resolved inside a turn and the lifecycle records where a cancellation happened. A backend
        /// with no roster state bound pays nothing, which is the same named absence every other
        /// optional dependency here uses.</para>
        /// </summary>
        private void SpendMinionDefeatBounties(
            GuardsDigital.Core.Units.Minions.MinionOccupancy defeatedMinion,
            MatchParticipants participants,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            if (activeEffects == null || rosterState == null || !rosterState.IsAvailable)
            {
                return;
            }

            var slot = rosterState.TurnSlots
                .OrderByDescending(candidate => candidate.RoundIndex)
                .ThenByDescending(candidate => candidate.TurnIndex)
                .FirstOrDefault();
            if (slot == null)
            {
                return;
            }

            MinionDefeatBountyQuery.Spend(
                activeEffects,
                coinLedger,
                adjacentMoveAuthority,
                participants,
                heroItems,
                definitions,
                instances,
                defeatedMinion,
                slot.RoundIndex,
                slot.TurnIndex);
        }

        /// <summary>
        /// Binds the round's live history/resource state used by roster-derived Attack values. It is
        /// the same reference effect frames receive, so restore and live resolution rederive from one
        /// authority without a snapshot field.
        /// </summary>
        internal void BindRosterState(EffectRosterState state)
        {
            rosterState = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// Milestone 7.4. Hands this backend the two states rulebook p.13's hero-Defeat consequences
        /// read and write - every hero's Level, which prices the Defeat, and every team's Life counters,
        /// which the defeated hero's team spends. It is a binding for exactly the reason
        /// <see cref="BindHeroItems"/> is: both are owned by the round authority, which is composed
        /// after this dispatcher, and a second copy of either would produce a coin total or a counter
        /// count that no restore or replay could reproduce.
        ///
        /// Absent outside a live match. A composition that binds neither still records the Defeat, the
        /// removal and the marker returns; what it cannot do is price the reward, so the resolver
        /// refuses the outcome rather than inventing a Level of one.
        /// </summary>
        internal void BindDefeatConsequenceState(
            GuardsDigital.Core.Rules.Progression.HeroProgressionState levels,
            TeamLifeState lives)
        {
            heroLevels = levels;
            teamLives = lives;
        }

        internal void BindHeroDefeatHistory(RoundHeroDefeatHistory history)
        {
            heroDefeatHistory = history ?? throw new ArgumentNullException(nameof(history));
        }

        public AttackActionResolutionResult StartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                ActionOfferWindow.NormalPlay);
            return StartAttackResolution(
                resolutionId,
                offer,
                materializedDefinitions,
                materializedInstances,
                participants,
                authoritativeOffers,
                modifications);
        }


        public AttackActionResolutionResult StartTurnAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            bool authorizedTextGrant = false,
            IReadOnlyList<AttackTarget> excludedTargets = null)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = authorizedTextGrant
                ? new[] { offer }
                : ActionOfferQuery.GetAvailableTurnActionOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                offer.SourceCardInstanceId);
            return StartAttackResolution(
                resolutionId,
                offer,
                materializedDefinitions,
                materializedInstances,
                participants,
                authoritativeOffers,
                modifications,
                excludedTargets);
        }

        /// <summary>
        /// Milestone 8.10. A second instance of an Attack that has just finished, asked for by that
        /// Attack's own card text: Thunder Boomerang prints "If you targeted a hero may repeat once on
        /// a different target."
        ///
        /// <para><b>Why it is not <see cref="StartTurnAttack"/>.</b> That entry point validates the
        /// offer against what the hero may take right now, and by the time a repeat is owed the answer
        /// is no - the card has been played and its action performed, which is precisely why p.10 has a
        /// repeat at all. The identity therefore comes from the instance that just completed, exactly
        /// as milestone 6.7's repeated Skill takes its identity from the frame that asked, and the
        /// offer is rebuilt from that identity rather than re-chosen.</para>
        ///
        /// <para>Everything after this point is the ordinary Attack: the same target choice, the same
        /// Defense response, the same p.13 consequences and the same p.14 order. What the repeated
        /// instance carries that the first did not is the target it may not take - and, since
        /// milestone 8.25d, <see cref="ActionInvocationContext.Repeated"/>.</para>
        ///
        /// <para><b>Milestone 8.25d: why the context is minted here rather than copied.</b> This built
        /// its second instance with <c>completed.InvocationContext</c>, which is the printed one the
        /// first instance ran under, so a card-text Attack repeat reported itself as printed
        /// everywhere downstream: Xargatha's "on a different enemy hero" narrowing never fired, and the
        /// <see cref="TimingCheckpoint.ActionEnded"/> this instance raises reported a printed context,
        /// which is what p.10's "You cannot repeat a repeated action!" is enforced by. It is the same
        /// value milestone 8.23's after-action arm has always passed in, so the two paths that start a
        /// repeated Attack now agree - and <see cref="PendingAttackResolution"/> has admitted the
        /// context since milestone 8.25a.</para>
        ///
        /// <para><b>Milestone 8.25d: the second instance runs under an identity of its own.</b>
        /// Everything else it inherits from the completed one, but not this: rulebook p.13's Defeat
        /// outcome is identified as <c>&lt;resolution&gt;:hero-defeat</c> or
        /// <c>&lt;resolution&gt;:minion-defeat</c>, and <see cref="HeroCoinLedger"/> refuses to pay a
        /// Defeat outcome twice - so two instances sharing one identity could not both defeat the same
        /// kind of unit. The caller names the identity because the two callers mint it differently:
        /// milestone 8.23's after-action path already mints <c>after-action-repeat:...</c> for its
        /// instance, and milestone 8.10's card-text path derives one from the instance that asked.
        /// Measured on the official six-player board: Cleave's printed instance defeated one adjacent
        /// enemy hero, its repeat took the other, and declining that Defense threw
        /// <c>OutcomeAlreadyAwarded</c>.</para>
        /// </summary>
        internal AttackActionResolutionResult StartRepeatedAttack(
            PendingAttackResolution completed,
            ActionResolutionId repeatedResolutionId,
            AttackTarget excludedTarget,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (!repeatedResolutionId.IsDefined)
            {
                throw new ArgumentException(
                    "A repeated Attack instance runs under a resolution identity of its own.",
                    nameof(repeatedResolutionId));
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var refusal = PlanRepeatedAttack(
                completed, repeatedResolutionId, excludedTarget, materializedDefinitions,
                materializedInstances, participants, modifications,
                out var repeated, out var plannedChoice, out var targetIsBoundByCardText);
            if (refusal != CardActionResolutionRejectionReason.None)
            {
                return RejectAttack(refusal);
            }

            if (targetIsBoundByCardText)
            {
                var applying = new PendingAttackResolution(
                    repeatedResolutionId,
                    completed.SourceCardInstanceId,
                    completed.SourceDefinitionReference,
                    completed.ActionDefinitionId,
                    completed.ActorId,
                    ActionInvocationContext.Repeated,
                    new ActionResolutionStepId("before-attack-text"),
                    AttackResolutionPhase.ApplyBeforeAttackText,
                    default(AttackTarget),
                    null,
                    excludedTarget: excludedTarget,
                    actorSpaceAtActionStart: ActorSpaceOf(completed.ActorId),
                    excludedTargets: repeated.ExcludedTargets);
                pendingAttackResolution = applying;
                pendingAttackTargetChoice = null;
                pendingDefenseChoice = null;
                return AttackActionResolutionResult.Accept(
                    applying,
                    null,
                    null,
                    new List<ResolutionStep>
                    {
                        ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                        ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack)
                    }
                    .Concat(CardTextAt(
                        CardTextTiming.BeforeTheAttack, applying, applying.ActorId,
                        materializedDefinitions))
                    .ToArray());
            }

            pendingAttackResolution = repeated;
            pendingAttackTargetChoice = plannedChoice;
            pendingDefenseChoice = null;

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                pendingAttackTargetChoice,
                null,
                new[]
                {
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackTargetChoiceRequested(pendingAttackResolution))
                });
        }

        /// <summary>
        /// Milestone 8.25e. Whether a repeated instance of this completed Attack could start from the
        /// board as it now stands, without starting one.
        ///
        /// <para>Rulebook p.10's repeat is an offer - "you <em>may</em> perform the entire action
        /// again" - so an ability that offers one has to know whether there is an instance to offer.
        /// The answer is the same judgement <see cref="StartRepeatedAttack"/> makes, asked from
        /// <see cref="PlanRepeatedAttack"/> rather than restated here, for the reason
        /// <c>CanClearRemoveAnything</c> exists: one statement of the judgement, asked in both places,
        /// so the offer and the answer cannot drift apart.</para>
        ///
        /// <para>What it cannot answer is whether the instance will <em>finish</em>. An Attack whose
        /// text names its own target starts at p.14's step 1 and the text names a unit there; a board
        /// that leaves it nothing to name stops the action at that mandatory step, which is p.10's own
        /// Mandatory and Optional Steps rather than an offer that should not have been made. Deciding
        /// that here would mean running the card text speculatively at the checkpoint.</para>
        /// </summary>
        /// <param name="theAskingInstanceIsStillUnderWay">
        /// Milestone 8.35z15. Whether the instance being asked about is the one currently occupying
        /// <see cref="pendingAttackResolution"/> - which is the ordinary case when card text printed
        /// inside rulebook p.14 asks this, because that text is a step of the very Attack it offers to
        /// repeat.
        ///
        /// <para><b>Why the guard has to be told.</b> <see cref="PlanRepeatedAttack"/> refuses while a
        /// resolution is pending, and that guard exists so <see cref="StartRepeatedAttack"/> cannot
        /// clobber a live one. Asked as a question rather than as a start, the pending resolution is
        /// not a blocker but the subject: Arien's Violent Torrent reaches its repeat at p.14's step 1,
        /// with its own Attack still under way, and judging it against that guard would have refused
        /// every one of its repeats - which is measurably worse than the defect being closed. Only the
        /// named instance is excused; any other pending resolution still refuses.</para>
        /// </param>
        /// <param name="cardTextCanBindATarget">
        /// Milestone 8.75d. Whether the printed subject of an Attack whose target is bound by CARD
        /// TEXT could name anybody in the instance this question is about, asked of the planned
        /// instance so the probe reads the exclusions and the clause narrowing that instance carries.
        ///
        /// <para><b>Why it is a caller's delegate rather than a judgement made here.</b> Answering it
        /// means walking the card's own typed effect body against the registered capability catalog,
        /// which this backend holds none of; the card-action authority does, and already asks exactly
        /// this question for a first instance in <c>CanAttackFindTarget</c>. Passing it in means one
        /// statement of the walk, asked from both places.</para>
        ///
        /// <para><b>Null keeps the reading this method had</b>, which is what the after-action repeat
        /// path deliberately passes: Ignatia's Chaos Incarnate FLIPS the Tie Breaker coin between the
        /// offer and the instance, so her live printed line at the moment of the offer is not the one
        /// the repeated instance will run, and probing the wrong line would refuse a repeat the card
        /// grants. The card-text path has no such step between the two.</para>
        /// </param>
        internal bool CanStartRepeatedAttack(
            PendingAttackResolution completed,
            ActionResolutionId repeatedResolutionId,
            AttackTarget excludedTarget,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            bool theAskingInstanceIsStillUnderWay = false,
            Func<PendingAttackResolution, bool> cardTextCanBindATarget = null) =>
            completed != null &&
            repeatedResolutionId.IsDefined &&
            definitions != null &&
            instances != null &&
            PlanRepeatedAttack(
                completed, repeatedResolutionId, excludedTarget, definitions.ToArray(),
                instances.ToArray(), participants, modifications,
                out _, out _, out _, theAskingInstanceIsStillUnderWay, cardTextCanBindATarget) ==
                CardActionResolutionRejectionReason.None;

        /// <summary>
        /// Everything <see cref="StartRepeatedAttack"/> decides before it commits anything: the
        /// instance it would run, the target question it would ask, and whether the card's own text
        /// names the target instead. Nothing here writes a field, so the query above can ask it.
        /// </summary>
        private static IReadOnlyList<AttackTarget> RepeatExclusions(
            PendingAttackResolution completed, AttackTarget excludedTarget,
            IReadOnlyList<CardDefinition> definitions)
        {
            // Milestone 8.38d. What an ABILITY-GRANTED instance was born excluding is not the repeat
            // clause's business and does not reset with it: it is the grant saying which enemy heroes
            // it has already reached this turn, and a repeat of a granted Attack is still that grant's
            // instance. It is carried first so both branches below keep it.
            var granted = completed.InvocationContext == ActionInvocationContext.TextGranted
                ? completed.ExcludedTargets
                : (IReadOnlyList<AttackTarget>)Array.Empty<AttackTarget>();
            if (!excludedTarget.IsDefined) return granted;
            var action = definitions.Single(value => value.Reference.Equals(completed.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(completed.ActionDefinitionId));
            var clause = CardTextRepeatChain.ProducingClause(action, CardTextRepeatChain.Mint(completed.ResolutionId));
            // Accumulation belongs to a multi-repeat "different targets" clause. Separate
            // one-repeat clauses keep their own binding; an unbound repeat inherits no exclusion.
            return clause != null && clause.MaximumRepeats > 1
                ? completed.ExcludedTargets.Concat(new[] { excludedTarget }).Distinct().ToArray()
                : granted.Concat(new[] { excludedTarget }).Distinct().ToArray();
        }

        private CardActionResolutionRejectionReason PlanRepeatedAttack(
            PendingAttackResolution completed,
            ActionResolutionId repeatedResolutionId,
            AttackTarget excludedTarget,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            IReadOnlyList<CardInstance> materializedInstances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            out PendingAttackResolution planned,
            out PendingAttackTargetChoice plannedChoice,
            out bool targetIsBoundByCardText,
            bool theAskingInstanceIsStillUnderWay = false,
            Func<PendingAttackResolution, bool> cardTextCanBindATarget = null)
        {
            planned = null;
            plannedChoice = null;
            targetIsBoundByCardText = false;
            if (pendingAttackResolution != null &&
                !(theAskingInstanceIsStillUnderWay &&
                    pendingAttackResolution.ResolutionId.Equals(completed.ResolutionId)))
            {
                return CardActionResolutionRejectionReason.ResolutionAlreadyPending;
            }

            planned = new PendingAttackResolution(
                repeatedResolutionId,
                completed.SourceCardInstanceId,
                completed.SourceDefinitionReference,
                completed.ActionDefinitionId,
                completed.ActorId,
                ActionInvocationContext.Repeated,
                new ActionResolutionStepId("attack-target"),
                AttackResolutionPhase.ChooseTarget,
                default(AttackTarget),
                null,
                excludedTarget: excludedTarget,
                actorSpaceAtActionStart: ActorSpaceOf(completed.ActorId),
                excludedTargets: RepeatExclusions(completed, excludedTarget, materializedDefinitions));
            var offer = FindPendingAttackOffer(
                planned, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                return CardActionResolutionRejectionReason.UnavailableOffer;
            }

            var actorId = planned.ActorId;
            if (!adjacentMoveAuthority.Occupancies.Any(
                occupancy => occupancy.HeroId.Equals(actorId)))
            {
                return CardActionResolutionRejectionReason.ActorUnavailable;
            }

            // Milestone 8.19a. A repeated instance of an Attack whose text names its target does the
            // same thing the printed instance did: it starts at p.14's step 1 with nothing bound, and
            // the text names a unit again - under the exclusion this instance carries.
            var repeatedAction = ActionOfferQuery.FindActionDefinition(
                offer.Value, materializedDefinitions);
            var repeatedPolicy = repeatedAction.AttackTargetPolicy ??
                PrintedAttackTargetPolicy.ForClassification(offer.Value.AttackRange);
            if (repeatedPolicy.TargetIsBoundByCardText)
            {
                targetIsBoundByCardText = true;

                // Milestone 8.75d. This method's own remarks said what it could not answer - "an
                // Attack whose text names its own target starts at p.14's step 1 and the text names a
                // unit there; deciding that here would mean running the card text speculatively at
                // the checkpoint". Running it speculatively is exactly what
                // `CanAttackFindTarget` has done for a FIRST instance since milestone 8.29d, so the
                // question has an owner; what it needed was the planned instance, because the clause
                // that produced it is half of what its printed subject may name. A caller that hands
                // over no probe keeps the reading this method had.
                return cardTextCanBindATarget == null || cardTextCanBindATarget(planned)
                    ? CardActionResolutionRejectionReason.None
                    : CardActionResolutionRejectionReason.TargetUnavailable;
            }

            plannedChoice = CreateAttackTargetChoice(
                planned, offer.Value, materializedDefinitions, participants, modifications,
                materializedInstances);
            return plannedChoice.LegalTargets.Count == 0
                // p.10's repeat is offered, not owed: an instance with nobody left to take simply does
                // not happen, and since milestone 8.25e it is not offered either.
                ? CardActionResolutionRejectionReason.TargetUnavailable
                : CardActionResolutionRejectionReason.None;
        }

        private AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            IReadOnlyList<CardInstance> materializedInstances,
            MatchParticipants participants,
            IReadOnlyList<ActionOffer> authoritativeOffers,
            RuleModificationSet modifications,
            IReadOnlyList<AttackTarget> excludedTargets = null)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Starting an Attack resolution requires a resolution ID.", nameof(resolutionId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ResolutionAlreadyPending);
            }

            // Milestone 8.35z50 admits `TextGranted`, the stamp a borrowed primary carries. The offer
            // reaching here names the BORROWED card as its source, so every value, Range, Radius and
            // printed-text seam below reads that card's own definition and nothing had to be copied.
            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary &&
                offer.InvocationContext != ActionInvocationContext.TextGranted)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedInvocationContext);
            }

            if (!offer.IsAvailable || !authoritativeOffers.Contains(offer))
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var actionDefinition = ActionOfferQuery.FindActionDefinition(offer, materializedDefinitions);
            if (actionDefinition.Kind != ActionKind.Attack)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedActionKind);
            }

            if (offer.AttackRange != AttackRangeKind.NonRanged &&
                offer.AttackRange != AttackRangeKind.Ranged)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedAttackRange);
            }

            try
            {
                participants.GetHero(offer.ActorId);
            }
            catch (KeyNotFoundException)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ActorUnavailable);
            }

            if (!adjacentMoveAuthority.Occupancies.Any(occupancy => occupancy.HeroId.Equals(offer.ActorId)))
            {
                return RejectAttack(CardActionResolutionRejectionReason.ActorUnavailable);
            }

            // Milestone 8.19a, `attack-target@2`. A card whose own text names its target has nothing to
            // choose here: Brogan's Mad Dash charges two spaces and attacks what it ends beside, so the
            // set the page would offer now is the set before the charge and offers units the charge
            // cannot reach. Such an Attack starts straight into p.14's step 1 with no target and no
            // total, and its text hands one over.
            var targetPolicy = actionDefinition.AttackTargetPolicy ??
                PrintedAttackTargetPolicy.ForClassification(offer.AttackRange);
            if (targetPolicy.TargetIsBoundByCardText)
            {
                var applying = new PendingAttackResolution(
                    resolutionId,
                    offer.SourceCardInstanceId,
                    offer.SourceDefinitionReference,
                    offer.ActionDefinitionId,
                    offer.ActorId,
                    offer.InvocationContext,
                    new ActionResolutionStepId("before-attack-text"),
                    AttackResolutionPhase.ApplyBeforeAttackText,
                    default(AttackTarget),
                    null,
                    actorSpaceAtActionStart: ActorSpaceOf(offer.ActorId),
                    excludedTargets: excludedTargets);
                pendingAttackResolution = applying;
                pendingAttackTargetChoice = null;
                pendingDefenseChoice = null;
                return AttackActionResolutionResult.Accept(
                    applying,
                    null,
                    null,
                    new List<ResolutionStep>
                    {
                        ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                        ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack)
                    }
                    .Concat(CardTextAt(
                        CardTextTiming.BeforeTheAttack, applying, applying.ActorId,
                        materializedDefinitions))
                    .ToArray());
            }

            pendingAttackResolution = new PendingAttackResolution(
                resolutionId,
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                offer.ActorId,
                offer.InvocationContext,
                new ActionResolutionStepId("attack-target"),
                AttackResolutionPhase.ChooseTarget,
                default(AttackTarget),
                null,
                actorSpaceAtActionStart: ActorSpaceOf(offer.ActorId),
                // Milestone 8.38d. The narrowing an ABILITY-GRANTED instance is born with: the enemy
                // heroes that grant has already reached this turn. Seeded rather than accumulated -
                // the store is the turn scoped key set, and this is only how one instance reads it,
                // so the offer and the bind check below cannot disagree about what is excluded.
                excludedTargets: excludedTargets);
            pendingAttackTargetChoice = CreateAttackTargetChoice(
                pendingAttackResolution,
                offer,
                materializedDefinitions,
                participants,
                modifications,
                materializedInstances);
            pendingDefenseChoice = null;

            if (pendingAttackTargetChoice.LegalTargets.Count == 0)
            {
                pendingAttackResolution = null;
                pendingAttackTargetChoice = null;
                return RejectAttack(CardActionResolutionRejectionReason.TargetUnavailable);
            }

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                pendingAttackTargetChoice,
                null,
                new[]
                {
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackTargetChoiceRequested(pendingAttackResolution))
                });
        }


        public AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution == null || pendingAttackTargetChoice == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.ChooseTarget)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            if (!command.ResolutionId.Equals(pendingAttackTargetChoice.ResolutionId) ||
                !command.ChoiceId.Equals(pendingAttackTargetChoice.ChoiceId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.StaleChoice);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var offer = FindPendingAttackOffer(
                pendingAttackResolution,
                materializedDefinitions,
                materializedInstances);
            if (!offer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var authoritativeChoice = CreateAttackTargetChoice(
                pendingAttackResolution,
                offer.Value,
                materializedDefinitions,
                participants,
                modifications,
                materializedInstances);
            if (!AttackTargetChoicesMatch(authoritativeChoice, pendingAttackTargetChoice))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            if (!authoritativeChoice.LegalTargets.Contains(command.Target))
            {
                return RejectAttack(CardActionResolutionRejectionReason.TargetUnavailable);
            }

            // Milestone 8.19a. The page reaches its first card-text moment here and stops being a
            // single command. What is recorded before it does is the target and the space that target
            // is standing on, because p.14's steps 6 and 7 can take the piece off the board and a rider
            // printed at either would then be asking about a unit that is nowhere.
            var applying = new PendingAttackResolution(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.InvocationContext,
                new ActionResolutionStepId("before-attack-text"),
                AttackResolutionPhase.ApplyBeforeAttackText,
                command.Target,
                null,
                excludedTarget: pendingAttackResolution.ExcludedTarget,
                targetSpaceBeforeConsequences: TargetSpaceOf(command.Target),
                actorSpaceAtActionStart: pendingAttackResolution.ActorSpaceAtActionStart,
                excludedTargets: pendingAttackResolution.ExcludedTargets);

            pendingAttackResolution = applying;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;

            // Milestone 8.35z27. A sequence of a modal Attack records the unit it bound on
            // the chain: Ruling 19's "(on different targets)" asks a later sequence a question the
            // board stops being able to answer the moment this one is over, because a target that
            // successfully defended is still standing and looks like a unit nobody has taken.
            var isASequenceOfAModalAttack =
                pendingSequenceChain != null && pendingSequenceChain.Owns(applying.ResolutionId);
            if (isASequenceOfAModalAttack)
            {
                pendingSequenceChain = pendingSequenceChain.Binding(command.Target);
            }

            var openingSteps = new List<ResolutionStep>
            {
                ResolutionStep.FromFact(CardActionResolutionEvent.AttackTargetValidated(applying))
            };

            // Milestone 8.35z27. The two checkpoints below and the text under them belong
            // to the ACTION, and Ruling 5 makes a whole choose-both modal one action: they were raised
            // once when the modal opened, and a sequence re-raising them would give a before-action
            // trigger a second open, "you performed an Attack action" a second subject, and the card's
            // own step-1 text - which for a modal card IS the modal - a second run. What is per
            // sequence is everything p.14 does from the Attack Total down, which is below this.
            if (!isASequenceOfAModalAttack)
            {
                openingSteps.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted));
                openingSteps.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack));
            }

            // p.14 step 1: "Attacker applies 'Before the attack' text, if any." p.13 puts it "at the
            // very beginning", which is before the Attack Total is announced and therefore before the
            // defender has anything to answer - and p.17's own note keeps it applied whether or not the
            // attack later succeeds.
            var beforeText = isASequenceOfAModalAttack
                ? Array.Empty<ResolutionStep>()
                : CardTextAt(
                    CardTextTiming.BeforeTheAttack, applying, applying.ActorId,
                    materializedDefinitions)
                    .ToArray();
            if (beforeText.Length > 0)
            {
                // The page stops here and the authority applies the text. Whether it asks anything is
                // not this backend's business: either way the rest of the page is produced by
                // ContinueAfterBeforeAttackText, from this resolution, once the text is done.
                openingSteps.AddRange(beforeText);
                return AttackActionResolutionResult.Accept(applying, null, null, openingSteps);
            }

            return ContinueAfterBeforeAttackText(
                openingSteps, materializedDefinitions, materializedInstances, participants,
                modifications);
        }

        /// <summary>
        /// Milestone 8.19a. Everything rulebook p.14 does after its step 1, produced from the
        /// resolution that step is being applied against.
        ///
        /// <para><b>Why it is one method called from two places.</b> Before this milestone the whole
        /// page from the target's validation to the Defense request was emitted inside the command that
        /// answered the target, so a question standing at step 1 had nothing to hold the rest in - and a
        /// composition that could ask one was refused at content load. Two of the roster's printed
        /// shapes ask one, so the page has to be able to stop. It stops by this method not being called
        /// yet: the card-action authority calls it when the text finishes, and
        /// <see cref="AnswerAttackTarget"/> calls it directly when there is no text at all. One
        /// generator, so a paused page and an unpaused one cannot produce different orders - which is
        /// the same discipline <see cref="StepsAfterCardText"/> holds for steps 4 and 7.</para>
        ///
        /// <para><b>The bound target.</b> An Attack whose policy says its text names its target arrives
        /// here with none, and the text hands one over. It is re-derived against the live legal set
        /// rather than trusted, because the text has just moved pieces: a charge that ended beside
        /// nobody, or beside a unit p.18 makes immune, names nothing this Attack may take. That is not
        /// a rejected command - the move happened and p.10 keeps it - so the action stops with no
        /// outcome, exactly as a mandatory step that cannot be carried out stops one.</para>
        /// </summary>
        internal AttackActionResolutionResult ContinueAfterBeforeAttackText(
            IReadOnlyList<ResolutionStep> precedingSteps,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            AttackTarget boundTarget = default)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (pendingAttackResolution == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.ApplyBeforeAttackText)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var offer = FindPendingAttackOffer(
                pendingAttackResolution, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var steps = new List<ResolutionStep>(
                precedingSteps ?? Array.Empty<ResolutionStep>());
            var resolution = pendingAttackResolution;
            if (boundTarget.IsDefined)
            {
                // Milestone 8.75d. The clause that produced a REPEATED instance narrows what that
                // instance may take, and until this milestone this validation asked only the
                // unnarrowed legal set and the exclusion list beside it. So a card-text subject
                // could hand over a unit of the kind the clause forbids - Imminent Eruption's "may
                // repeat once on a minion" would have accepted a hero - and the Attack resolved
                // against it. The narrowing is the same method rulebook p.13's own target question
                // applies, asked here so the two answers stay one answer.
                var legal = NarrowedByTheProducingClause(
                    resolution,
                    FindLegalAttackTargets(
                        resolution.ActorId, offer.Value, materializedDefinitions, participants,
                        modifications, cardInstances: materializedInstances),
                    materializedDefinitions);
                if (!legal.Contains(boundTarget) ||
                    resolution.ExcludedTargets.Any(excluded => excluded.IsSameUnit(boundTarget)))
                {
                    return StopAttackWithoutATarget(resolution, steps);
                }

                resolution = new PendingAttackResolution(
                    resolution.ResolutionId,
                    resolution.SourceCardInstanceId,
                    resolution.SourceDefinitionReference,
                    resolution.ActionDefinitionId,
                    resolution.ActorId,
                    resolution.InvocationContext,
                    resolution.CurrentStepId,
                    AttackResolutionPhase.ApplyBeforeAttackText,
                    boundTarget,
                    null,
                    excludedTarget: resolution.ExcludedTarget,
                    targetSpaceBeforeConsequences: TargetSpaceOf(boundTarget),
                    actorSpaceAtActionStart: resolution.ActorSpaceAtActionStart,
                    excludedTargets: resolution.ExcludedTargets);
                pendingAttackResolution = resolution;
                steps.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.AttackTargetValidated(resolution)));
            }

            if (!resolution.Target.IsDefined)
            {
                return StopAttackWithoutATarget(resolution, steps);
            }

            if (resolution.Target.Kind == AttackTargetKind.Minion)
            {
                return ResolveMinionTarget(
                    resolution.TargetMinionId, participants, modifications, materializedDefinitions,
                    materializedInstances,
                    steps);
            }

            var attackValue = CalculateAttackValue(
                offer.Value,
                resolution.Target,
                materializedDefinitions,
                materializedInstances,
                participants,
                modifications,
                resolution.ActorSpaceAtActionStart);
            var defenseResolution = new PendingAttackResolution(
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.SourceDefinitionReference,
                resolution.ActionDefinitionId,
                resolution.ActorId,
                resolution.InvocationContext,
                new ActionResolutionStepId("defense-response"),
                AttackResolutionPhase.DefenseResponse,
                resolution.Target,
                attackValue,
                excludedTarget: resolution.ExcludedTarget,
                targetSpaceBeforeConsequences: resolution.TargetSpaceBeforeConsequences,
                actorSpaceAtActionStart: resolution.ActorSpaceAtActionStart,
                excludedTargets: resolution.ExcludedTargets);
            var defenseOffers = FindDefenseOffers(
                materializedDefinitions, materializedInstances, defenseResolution.TargetHeroId,
                modifications, offer.Value, participants);
            pendingAttackResolution = defenseResolution;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = new PendingDefenseResponseChoice(
                defenseResolution.ResolutionId,
                defenseResolution.CurrentStepId,
                defenseResolution.TargetHeroId,
                defenseResolution.ActorId,
                // Milestone 8.80. The attacking card travels WITH the question, because the frame
                // it used to be read from reaches the acting seat alone.
                defenseResolution.SourceCardInstanceId,
                defenseResolution.SourceDefinitionReference,
                defenseResolution.ActionDefinitionId,
                defenseResolution.AttackTotal,
                defenseOffers,
                true);

            steps.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.AttackStarted(defenseResolution)));
            steps.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.AttackTotalCalculated(defenseResolution)));
            steps.Add(ResolutionStep.AtCheckpoint(
                TimingCheckpoint.DefenseResponse, defenseResolution.TargetHeroId));
            steps.Add(ResolutionStep.AtDefenseChoice(defenseResolution));

            return AttackActionResolutionResult.Accept(
                defenseResolution, null, pendingDefenseChoice, steps);
        }

        /// <summary>
        /// Milestone 8.35z27. Opens the modal Attack contract on the action standing at
        /// rulebook p.14's step 1, and starts the first of its sequences.
        ///
        /// <para><b>Where it is called from.</b> The card-action authority calls it instead of
        /// <see cref="ContinueAfterBeforeAttackText"/> when the step-1 frame named a plan rather than a
        /// target. That is the whole of the entry: an Attack declaring
        /// <see cref="Cards.AttackTargetBinding.ByCardText"/> reaches step 1 with no target, its text
        /// is the modal, and the modal answers with bullets rather than a unit.</para>
        ///
        /// <para><b>What it deliberately does not emit.</b> Any checkpoint belonging to the action
        /// rather than to a sequence. <c>ActionStarted</c> was raised when the action started and is
        /// raised once, because Ruling 5 makes the whole modal one action and every reader of that
        /// checkpoint - a before-action trigger's open/close pair, "you performed an Attack action",
        /// p.10's repeat identity - would otherwise see two.</para>
        /// </summary>
        internal AttackActionResolutionResult OpenAttackSequences(
            IReadOnlyList<ResolutionStep> precedingSteps,
            AttackSequencePlan plan,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (pendingAttackResolution == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.ApplyBeforeAttackText)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            if (pendingSequenceChain != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ResolutionAlreadyPending);
            }

            // The modal's own entry resolution stops being the pending one here: it never had a target,
            // never announces an Attack Total, and never reaches p.14's step 2. What it is from now on
            // is the chain's record of whose action this is, which is where it is kept.
            pendingSequenceChain = new PendingAttackSequenceChain(
                pendingAttackResolution, plan, 0, Array.Empty<AttackTarget>());
            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
            return StartNextAttackSequence(
                precedingSteps, definitions, instances, participants, modifications);
        }

        /// <summary>
        /// Milestone 8.35z27. Starts the next complete rulebook p.14 sequence a modal
        /// Attack owes, binding its target against the board <em>as it now stands</em>.
        ///
        /// <para><b>The board is re-read, and that is the contract's central claim.</b> Ruling 19 puts
        /// each bullet's targeting at its own sequence's start, so a unit the first sequence defeated
        /// is not a candidate for the second, a unit the first sequence pushed may have left the
        /// second's reach, and - with no printed "(on different targets)" - a unit the first sequence
        /// merely failed to defeat is a legal target again.</para>
        ///
        /// <para><b>A sequence with nothing to take does not happen.</b> p.10 stops a mandatory step
        /// that cannot be carried out and keeps everything already done, which Ruling 45 restates for
        /// the targeting step. So this records the stop and moves the chain on rather than refusing
        /// the command: the earlier sequences stand, and the action ends when the plan is spent.</para>
        /// </summary>
        internal AttackActionResolutionResult StartNextAttackSequence(
            IReadOnlyList<ResolutionStep> precedingSteps,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (pendingSequenceChain == null || !pendingSequenceChain.HasNextSequence)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            if (pendingAttackResolution != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ResolutionAlreadyPending);
            }

            var actionIdentity = pendingSequenceChain.ActionIdentity;
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var steps = new List<ResolutionStep>(
                precedingSteps ?? Array.Empty<ResolutionStep>());

            while (pendingSequenceChain.HasNextSequence)
            {
                var sequence = new PendingAttackResolution(
                    pendingSequenceChain.NextSequenceResolutionId,
                    actionIdentity.SourceCardInstanceId,
                    actionIdentity.SourceDefinitionReference,
                    actionIdentity.ActionDefinitionId,
                    actionIdentity.ActorId,
                    actionIdentity.InvocationContext,
                    new ActionResolutionStepId("attack-target"),
                    AttackResolutionPhase.ChooseTarget,
                    default(AttackTarget),
                    null,
                    actorSpaceAtActionStart: actionIdentity.ActorSpaceAtActionStart);
                pendingSequenceChain = pendingSequenceChain.Starting();

                var offer = FindPendingAttackOffer(
                    sequence, materializedDefinitions, materializedInstances);
                if (!offer.HasValue)
                {
                    pendingAttackResolution = null;
                    pendingAttackTargetChoice = null;
                    pendingDefenseChoice = null;
                    pendingSequenceChain = null;
                    return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
                }

                pendingAttackResolution = sequence;
                pendingAttackTargetChoice = CreateAttackTargetChoice(
                    sequence, offer.Value, materializedDefinitions, participants, modifications,
                    materializedInstances);
                pendingDefenseChoice = null;
                if (pendingAttackTargetChoice.LegalTargets.Count > 0)
                {
                    steps.Add(ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackTargetChoiceRequested(sequence)));
                    return AttackActionResolutionResult.Accept(
                        sequence, pendingAttackTargetChoice, null, steps);
                }

                // Nothing this bullet's own sentence admits is left. The sequence stops where p.10
                // stops a mandatory step, the chain moves on, and the next bullet - if the plan names
                // one - is asked against this same board.
                steps.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.AttackResolutionCompleted(
                        sequence, AttackResolutionOutcome.None)));
                pendingAttackResolution = null;
                pendingAttackTargetChoice = null;
            }

            steps.AddRange(EndOfModalAttackSteps(actionIdentity));
            pendingSequenceChain = null;
            return AttackActionResolutionResult.Accept(
                null, null, null, steps, AttackResolutionOutcome.None);
        }

        /// <summary>
        /// The one <c>ActionEnded</c> a modal Attack raises: the action's own, once its whole plan is
        /// spent. Ruling 5 - "you performed an Attack action" is one fact about one action, recorded
        /// with the action's printed identity.
        /// </summary>
        private static IReadOnlyList<ResolutionStep> EndOfModalAttackSteps(
            PendingAttackResolution actionIdentity) =>
            new[]
            {
                ResolutionStep.AtActionEnded(
                    actionIdentity.SourceCardInstanceId,
                    actionIdentity.ActionDefinitionId,
                    actionIdentity.ActorId,
                    ActionKind.Attack,
                    actionIdentity.InvocationContext,
                    default(AttackTarget))
            };

        /// <summary>
        /// An Attack whose "Before the attack:" text named no unit this Attack may take. p.10 stops the
        /// remainder of the action and keeps everything the text already did, so the action ends with
        /// no outcome rather than being rolled back or refused.
        /// </summary>
        private AttackActionResolutionResult StopAttackWithoutATarget(
            PendingAttackResolution resolution,
            List<ResolutionStep> steps)
        {
            steps.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.AttackResolutionCompleted(
                    resolution, AttackResolutionOutcome.None)));

            // Milestone 8.35z27. A sequence of a modal Attack that reached no target ends
            // that sequence and not the action: Ruling 5's single action is over when its plan is,
            // and the bullets still owed are asked against this same board.
            if (!SequenceIsOwedAfter(resolution))
            {
                steps.Add(ResolutionStep.AtActionEnded(
                    resolution.SourceCardInstanceId,
                    resolution.ActionDefinitionId,
                    resolution.ActorId,
                    ActionKind.Attack,
                    resolution.InvocationContext,
                    resolution.Target));
            }

            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
            ClearSpentSequenceChain();
            return AttackActionResolutionResult.Accept(
                null, null, null, steps, AttackResolutionOutcome.None);
        }

        /// <summary>
        /// Where a unit is standing right now, captured when an Attack binds it as its target.
        /// Milestone 8.19a: the one fact about a target that the board stops answering once p.14's
        /// consequences have run.
        /// </summary>
        /// <summary>
        /// Where the attacking hero is standing right now, captured when an Attack instance starts.
        /// Milestone 8.23: Tigerclaw's Combat Reflexes asks at p.14's step 7 whether he moved before
        /// the attack, and the only way to answer it at step 7 is to have recorded where he was.
        /// </summary>
        // Milestone 9.21a: the acting body's space (RZ-Q4), not whichever row is listed first.
        private HexCoordinate? ActorSpaceOf(HeroId actorId) =>
            adjacentMoveAuthority.TryGetHeroCoordinate(actorId, out var actorAt)
                ? actorAt
                : (HexCoordinate?)null;

        private HexCoordinate? TargetSpaceOf(AttackTarget target)
        {
            if (target.Kind == AttackTargetKind.Hero)
            {
                // Milestone 9.21a: the space of the BODY the Attack targets (RZ-Q3).
                return adjacentMoveAuthority.TryGetHeroBodyCoordinate(
                        target.HeroId, target.HeroBody, out var targetAt)
                    ? targetAt
                    : (HexCoordinate?)null;
            }

            if (target.Kind == AttackTargetKind.Minion)
            {
                var minion = adjacentMoveAuthority.Minions
                    .FirstOrDefault(candidate => candidate.MinionId.Equals(target.MinionId));
                return minion == null ? (HexCoordinate?)null : minion.Coordinate;
            }

            return null;
        }

        /// <summary>
        /// The card-text step rulebook p.14 applies at this moment, or nothing at all.
        ///
        /// <para>Milestone 8.5, opening the seam the 7.9a review recorded as F-CARD-01. Three of p.14's
        /// nine numbered steps are card text - the attacker's at 1 and 7 and the defender's at 4 - and
        /// this is where each is offered, in the exact position of the stream the page puts it. What the
        /// text does is not decided here: the step names the card, the action and the hero, and the
        /// authority that holds the effect catalog runs the composition.</para>
        ///
        /// <para>p.13 is the reason the policy is read rather than assumed: "Most Attacks are Primary
        /// actions. When you perform a primary action, apply the card text." An action that suppresses
        /// its source card's text performs none, which is what keeps a printed secondary - and the
        /// replacements p.13 gives a Movement or an Attack - from applying the card's rider.</para>
        /// </summary>
        private IEnumerable<ResolutionStep> CardTextAt(
            CardTextTiming timing,
            PendingAttackResolution resolution,
            HeroId actorId,
            IReadOnlyList<CardDefinition> definitions,
            AttackResolutionOutcome remainderOutcome = AttackResolutionOutcome.None) =>
            CardTextAt(
                timing,
                resolution,
                actorId,
                definitions,
                resolution.SourceDefinitionReference,
                resolution.ActionDefinitionId,
                resolution.SourceCardInstanceId,
                remainderOutcome);

        /// <summary>
        /// The same, for text on a card that is not the Attack's own - p.14's step 4 applies the
        /// defender's card, which the attacker's resolution does not name.
        /// </summary>
        private IEnumerable<ResolutionStep> CardTextAt(
            CardTextTiming timing,
            PendingAttackResolution resolution,
            HeroId actorId,
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            AttackResolutionOutcome remainderOutcome = AttackResolutionOutcome.None)
        {
            var action = FindCardTextAction(sourceDefinitionReference, actionDefinitionId, definitions);
            var bullet = SequenceBulletFor(resolution);
            if (timing == CardTextTiming.AfterTheAttack && bullet.IsDefined && !bullet.AppliesAfterAttackText)
                return Array.Empty<ResolutionStep>();
            // Milestone 8.23. An Attack may print text at both of p.14's attacker moments, so the
            // question is whether this action has text for THIS moment rather than whether its one
            // declared moment happens to be this one.
            if (action == null || action.CardTextAt(timing) == null)
            {
                return Array.Empty<ResolutionStep>();
            }

            // Milestone 8.10 could carry a remainder at two of p.14's three moments; milestone 8.19a
            // carries one at all three. Step 1's remainder is not a list of facts the way the other
            // two are - it is the rest of the page, produced by
            // <see cref="ContinueAfterBeforeAttackText"/> against live state - but what has to be
            // handed over is the same thing either way: the attack the remainder belongs to.
            const bool carriesARemainder = true;

            return new[]
            {
                ResolutionStep.ApplyCardText(
                    sourceCardInstanceId,
                    action.Id,
                    actorId,
                    timing,
                    carriesARemainder ? resolution : null,
                    remainderOutcome)
            };
        }

        /// <summary>
        /// The steps rulebook p.14 leaves after one of its card-text moments, produced from the
        /// attack's own identity rather than from anything a run remembered. Milestone 8.10.
        ///
        /// <para><b>Why it is a generator rather than a list.</b> Card text applied inside an Attack
        /// may now ask the player a question, and the steps standing after it cannot be carried across
        /// that pause: they are facts about an attack, not identity, and this engine persists identity
        /// alone. So they are generated here from the pending resolution the pause does persist - and
        /// the same generator is what emits them when nothing pauses at all. There is one of them, so
        /// the paused and unpaused orders cannot drift apart.</para>
        ///
        /// <para><b>Why only two of the page's three moments.</b> What follows step 4 and what follows
        /// step 7 are each a function of the resolution: the Defense total and its comparison, or the
        /// outcome and the end of the action. What follows step 1 is not - on the minion arm it
        /// contains the facts of a Defeat this backend has already computed and applied, which no
        /// identity can reproduce. A card whose text could pause at step 1 is therefore still refused
        /// at configuration by <see cref="GuardsDigital.Core.Effects.EffectDefinitionValidator"/>, and
        /// that refusal now names the moment rather than the whole Attack.</para>
        /// </summary>
        /// <param name="theActionEndsHere">
        /// Milestone 8.35z27. Whether p.14 finishing here also finishes the action that
        /// carried it. False for every sequence of a modal Attack but the last: Ruling 5 makes
        /// "choose one, or both" a single action, so <c>ActionEnded</c> - the checkpoint "you performed
        /// an Attack action" is read from, and the one a before-action trigger closes on - is raised
        /// once, when the plan is spent. The sequence's own outcome is still recorded either way,
        /// because each sequence really does reach one.
        /// </param>
        internal static IReadOnlyList<ResolutionStep> StepsAfterCardText(
            CardTextTiming moment,
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome,
            bool theActionEndsHere = true)
        {
            if (resolution == null) throw new ArgumentNullException(nameof(resolution));

            switch (moment)
            {
                // p.14 step 7 is the last thing the page does, so what stands after it is the outcome
                // this attack reached and the end of the action that carried it.
                case CardTextTiming.AfterTheAttack:
                    var completed = ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackResolutionCompleted(resolution, outcome));
                    // Milestone 8.37. Stated here, from identity this method already
                    // holds, rather than inferred later by re-reading a produced fact's kind - see
                    // ResolutionStep.CompletedDefeatedMinionWithBasicAttack's own remarks.
                    var defeatedMinionWithBasicAttack =
                        resolution.Target.Kind == AttackTargetKind.Minion &&
                        outcome == AttackResolutionOutcome.AttackSuccessful;
                    return theActionEndsHere
                        ? Array.AsReadOnly(new[]
                        {
                            completed,
                            ResolutionStep.AtActionEnded(
                                resolution.SourceCardInstanceId,
                                resolution.ActionDefinitionId,
                                resolution.ActorId,
                                ActionKind.Attack,
                                resolution.InvocationContext,
                                resolution.Target,
                                defeatedMinionWithBasicAttack: defeatedMinionWithBasicAttack)
                        })
                        : Array.AsReadOnly(new[] { completed });

                // p.14 step 4 sits inside the defender's response, so what stands after it is step 5's
                // total, the end of the response, and the comparison step 6 opens.
                case CardTextTiming.DefenseAction:
                    return Array.AsReadOnly(new[]
                    {
                        ResolutionStep.FromFact(
                            CardActionResolutionEvent.DefenseTotalCalculated(resolution)),
                        ResolutionStep.FromFact(
                            CardActionResolutionEvent.DefenseResponseCompleted(resolution)),
                        ResolutionStep.AtCheckpoint(
                            TimingCheckpoint.AttackResultComparison, resolution.TargetHeroId)
                    });

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(moment), moment,
                        "Only rulebook p.14's steps 4 and 7 leave a remainder this backend can " +
                        "produce from the attack's identity alone; step 1's remainder is the rest of " +
                        "the page and is produced by ContinueAfterBeforeAttackText against live state.");
            }
        }

        /// <summary>
        /// One card's action, when it carries card text this build may apply. Null covers every ordinary
        /// case: the action prints no text at all, its composition is still owed to an 8.6-8.13 slice,
        /// or p.13 suppresses it.
        /// </summary>
        private static ActionDefinition FindCardTextAction(
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IReadOnlyList<CardDefinition> definitions)
        {
            var definition = definitions.FirstOrDefault(candidate =>
                candidate.Reference.Equals(sourceDefinitionReference));
            var action = definition?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(actionDefinitionId));
            return action?.EffectRoot != null &&
                action.CardTextTiming != CardTextTiming.None &&
                action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies
                ? action
                : null;
        }

        /// <summary>
        /// Unit-type question (6.8g ledger): intrinsic, recorded as open rules question 8 in
        /// RULES_LEDGER.md. p.13 prices defeats by intrinsic identity - melee/ranged 2 coins, heavy
        /// 4, heroes by level - and no clause prices a defeated object that merely counts as a
        /// minion, so the reward keeps reading the defeated miniature until settled.
        /// </summary>
        private AttackActionResolutionResult ResolveMinionTarget(
            MinionId targetMinionId,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            List<ResolutionStep> precedingSteps)
        {
            var target = adjacentMoveAuthority.Minions.Single(minion => minion.MinionId.Equals(targetMinionId));
            var resolution = new PendingAttackResolution(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.InvocationContext,
                new ActionResolutionStepId("minion-defeat"),
                AttackResolutionPhase.ResolveMinionDefeat,
                AttackTarget.Minion(targetMinionId),
                null,
                excludedTarget: pendingAttackResolution.ExcludedTarget,
                targetSpaceBeforeConsequences:
                    pendingAttackResolution.TargetSpaceBeforeConsequences ??
                    TargetSpaceOf(AttackTarget.Minion(targetMinionId)),
                actorSpaceAtActionStart: pendingAttackResolution.ActorSpaceAtActionStart,
                excludedTargets: pendingAttackResolution.ExcludedTargets);
            // Finding `F-ER2-09`: p.13's coin values are read from the declared standard-minion content
            // rather than restated here as a Heavy-versus-other literal. The number is unchanged.
            var coins = StandardMinionProfileCatalog.Rulebook.Get(target.MinionType).DefeatCoins;
            var outcome = new DefeatOutcome(
                new DefeatOutcomeId($"{resolution.ResolutionId.Value}:minion-defeat"),
                new DefeatTargetId(targetMinionId.Value),
                target.TeamId,
                resolution.ActorId,
                coins,
                0);
            if (coinLedger == null) coinLedger = new HeroCoinLedger(participants);
            if (DefeatReplacementQuery.TryApply(
                BoardObjectReference.ForMinion(targetMinionId), RuleActivity.Attack,
                RuleObject.ForHero(resolution.ActorId), adjacentMoveAuthority, participants, modifications))
            {
                var replacedSteps = precedingSteps ?? new List<ResolutionStep>();
                replacedSteps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.AttackStarted(resolution)));
                replacedSteps.AddRange(StepsAfterMinionDefeat(resolution, definitions));
                pendingAttackResolution = null;
                pendingAttackTargetChoice = null;
                pendingDefenseChoice = null;
                ClearSpentSequenceChain();
                return AttackActionResolutionResult.Accept(
                    null, null, null, replacedSteps, AttackResolutionOutcome.AttackSuccessful,
                    MinionDefeatResolution.Replaced());
            }

            var reward = coinLedger.Award(outcome);
            if (!reward.Accepted)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            SpendMinionDefeatBounties(target, participants, definitions, instances);

            var removalAttempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
            var defeat = ResolveRemovalStage(
                targetMinionId, outcome, reward, removalAttempt, participants, modifications,
                resolution.ActorId, RuleActivity.Attack,
                RemovalPreventionOrigin.AttackMinionDefeat, resolution, definitions, instances);

            // Milestone 8.19c1. A source has offered to stop the removal and its controller has not
            // answered. Everything rulebook p.13 does before the removal has happened - the Defeat is
            // recorded and the coins are paid - and the rest of p.14 is owed to the answer, produced
            // from this resolution exactly as the page's card-text pause produces its own remainder.
            if (defeat == null)
            {
                var paused = precedingSteps ?? new List<ResolutionStep>();
                paused.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.AttackStarted(resolution)));
                paused.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.RemovalPreventionRequested(
                        resolution, pendingRemovalPrevention)));
                pendingAttackResolution = null;
                pendingAttackTargetChoice = null;
                pendingDefenseChoice = null;
                return AttackActionResolutionResult.Accept(
                    null, null, null, paused, AttackResolutionOutcome.None, null);
            }

            // Milestone 8.19a. p.14's step 1 and the two checkpoints around it are already in
            // <paramref name="precedingSteps"/>: this arm is now reached from
            // <see cref="ContinueAfterBeforeAttackText"/>, which is on the far side of that moment.
            // That is what let the page start pausing at step 1 - the Defeat below is applied after
            // the text rather than before it, so a question standing in the text stands before any of
            // it has happened.
            //
            // p.14 step 1 sits on the arm p.13 says has no steps 3 to 5: "Minions cannot defend. When
            // you target an enemy minion with an attack, it is simply defeated." The two card-text
            // moments that survive are the attacker's own, and they keep the page's positions around
            // the defeat that replaces the comparison.
            var events = precedingSteps ?? new List<ResolutionStep>();
            events.Add(ResolutionStep.FromFact(CardActionResolutionEvent.AttackStarted(resolution)));
            events.AddRange(defeat.Facts.Select(fact => ResolutionStep.FromFact(
                CardActionResolutionEvent.MinionDefeatFact(resolution, defeat, fact))));
            events.AddRange(StepsAfterMinionDefeat(resolution, definitions));

            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
            ClearSpentSequenceChain();
            return AttackActionResolutionResult.Accept(
                null, null, null, events,
                AttackResolutionOutcome.AttackSuccessful, defeat);
        }

        /// <summary>
        /// p.13's removal stage, decided by milestone 6.6's shared <see cref="RemovalStageDecision"/>
        /// rather than by this path reading the rules for itself.
        ///
        /// Before milestone 6.6b this method's answer was a constant: the removal was committed and the
        /// result written down as removed, so `DefeatRemovalResultKind.RemovalPrevented` had no producer
        /// while the standalone Remove reported it against live state. Two paths take a minion off the
        /// board and they disagreed about whether a removal can be stopped at all, which is worse than
        /// the rule being missing - a minion protected from a Skill's Remove still died to an Attack.
        ///
        /// The decision is asked before anything is committed, so a prevented removal touches neither
        /// occupancy nor the reserve, and p.8's question is asked only afterwards, because the rule is
        /// about what remains rather than about what was attempted. The reward has already been awarded
        /// by the time this runs, which is p.13's own ordering: its Bolster example prevents the removal
        /// and collects the coins in the same breath.
        ///
        /// No source can currently answer "prevented" for a legal Attack target - the only prevention
        /// the rules can express is p.18's immunity, and an immune minion is filtered out of the legal
        /// targets and revalidated immediately before this point. What the sharing guarantees is that
        /// the general prevent-removal family, when it arrives with verified card text, is honoured by
        /// both removal paths rather than by whichever one remembered to ask.
        /// </summary>
        private MinionDefeatResolution ResolveRemovalStage(
            MinionId targetMinionId,
            DefeatOutcome outcome,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            MatchParticipants participants,
            RuleModificationSet modifications,
            HeroId actorId,
            RuleActivity activity,
            RemovalPreventionOrigin preventionOrigin = RemovalPreventionOrigin.None,
            PendingAttackResolution preventionResolution = null,
            IReadOnlyList<CardDefinition> definitions = null,
            IReadOnlyList<CardInstance> instances = null)
        {
            var subject = BoardObjectReference.ForMinion(targetMinionId);
            var removalStage = new RemovalStageDecision(
                activity,
                RuleObject.ForHero(actorId),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                participants,
                modifications,
                LanePushCauseKind.Defeat);

            // A mandatory removal prevention (not a would-be Defeat replacement)
            // has nobody to ask and nothing to decide, so it is asked and applied before the offered,
            // card-priced family ever gets a chance to open a pause. Asked unconditionally: unlike the
            // offered family below, it needs no identity to rebuild a remainder from, because nobody is
            // ever asked to answer it.
            if (removalStage.ApplyAutomaticPrevention(subject))
            {
                return CompleteRemovalStage(
                    targetMinionId, outcome, reward, removalAttempt, removalStage, prevented: true);
            }

            // Milestone 8.19c1. The removal stage's second question, asked of the same shared decision
            // the first one is asked of, so both p.13 Defeat paths reach it or neither does. A caller
            // that supplies no identity to rebuild a remainder from cannot hold a pause, so it is not
            // offered one - and no such caller exists: both of this build's minion Defeats supply it.
            if (preventionOrigin != RemovalPreventionOrigin.None && preventionResolution != null &&
                definitions != null && instances != null)
            {
                var offers = removalStage.OfferedPreventions(subject, definitions, instances);
                if (offers.Count > 0)
                {
                    pendingRemovalPrevention = new PendingRemovalPreventionDecision(
                        preventionOrigin,
                        preventionResolution,
                        targetMinionId,
                        outcome,
                        reward.Operations,
                        offers[0].DecidingHeroId,
                        activity);
                    return null;
                }
            }

            return CompleteRemovalStage(
                targetMinionId, outcome, reward, removalAttempt, removalStage,
                removalStage.IsPrevented(subject));
        }

        /// <summary>
        /// Milestone 8.19c1's Defeat standing at its removal stage, or <c>null</c>. Non-null only while
        /// a prevention source's controller is deciding.
        /// </summary>
        internal PendingRemovalPreventionDecision PendingRemovalPrevention => pendingRemovalPrevention;

        /// <summary>Milestone 8.19c1: restores a decision that was standing when the match was saved.</summary>
        /// <summary>
        /// Milestone 8.35z27. Puts a persisted modal Attack plan back, for every
        /// continuation kind. A <c>null</c> chain is the ordinary case and clears nothing that was not
        /// already clear, because a restored backend starts with none.
        /// </summary>
        internal void RestoreSequenceChain(PendingAttackSequenceChain chain)
        {
            if (chain != null)
            {
                pendingSequenceChain = chain;
            }
        }

        /// <summary>
        /// Milestone 8.118. Holds the further minion Defeats an effect's text asked for behind the
        /// removal-prevention question the Defeat before them stopped on.
        /// </summary>
        internal void HoldMinionDefeatsBehindPendingPrevention(IEnumerable<MinionId> owed)
        {
            if (pendingRemovalPrevention == null)
            {
                throw new InvalidOperationException(
                    "Defeats are held behind a removal-prevention question only while one stands.");
            }

            pendingRemovalPrevention = pendingRemovalPrevention.HoldingMinionDefeats(owed);
        }

        internal void RestorePendingRemovalPrevention(PendingRemovalPreventionDecision decision)
        {
            pendingRemovalPrevention = decision;
        }

        /// <summary>
        /// Milestone 8.19c1. Answers a standing removal-prevention decision and finishes the Defeat.
        ///
        /// <para>Everything the answer names is checked against a fresh derivation rather than against
        /// anything the pause carried - which sources are still offering, and which cards could still
        /// pay them - for the reason milestone 6.8l re-derives a paused selection's eligibility: what
        /// was true when the question opened is not what its answer applies to. A refused answer leaves
        /// the decision exactly as it was, so it is still there to be made.</para>
        ///
        /// <para>Declining is always legal, and a paid prevention produces exactly what p.18's immunity
        /// produces, because both go through the one <see cref="CompleteRemovalStage"/>.</para>
        /// </summary>
        internal MinionDefeatResolution AnswerRemovalPrevention(
            RemovalPreventionCommand command,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            out PendingRemovalPreventionDecision answered,
            out CardInstanceId paidCardInstanceId,
            out CardActionResolutionRejectionReason rejection)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            answered = pendingRemovalPrevention;
            paidCardInstanceId = default;
            rejection = CardActionResolutionRejectionReason.None;
            if (answered == null)
            {
                rejection = CardActionResolutionRejectionReason.NoPendingResolution;
                return null;
            }

            if (!string.Equals(command.RequestId, answered.RequestId, StringComparison.Ordinal))
            {
                rejection = CardActionResolutionRejectionReason.StaleChoice;
                return null;
            }

            if (!command.DecidingHeroId.Equals(answered.DecidingHeroId))
            {
                rejection = CardActionResolutionRejectionReason.WrongChooser;
                return null;
            }

            var removalStage = new RemovalStageDecision(
                answered.Activity,
                RuleObject.ForHero(answered.Resolution.ActorId),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                participants,
                modifications,
                LanePushCauseKind.Defeat);
            var subject = BoardObjectReference.ForMinion(answered.SubjectMinionId);
            var prevented = false;
            if (command.Accepted)
            {
                var decidingHeroId = answered.DecidingHeroId;
                var option = removalStage
                    .OfferedPreventions(subject, definitions, instances)
                    .FirstOrDefault(offer =>
                        string.Equals(offer.SourceId, command.SourceId, StringComparison.Ordinal) &&
                        offer.DecidingHeroId.Equals(decidingHeroId) &&
                        offer.PayableCards.Contains(command.PaymentCardInstanceId));
                if (option == null)
                {
                    rejection = CardActionResolutionRejectionReason.InvalidChoice;
                    return null;
                }

                var payment = instances.Single(card => card.Id.Equals(command.PaymentCardInstanceId));
                if (!CardLifecycleAuthority.TryDiscardFromHand(payment))
                {
                    rejection = CardActionResolutionRejectionReason.InvalidChoice;
                    return null;
                }

                paidCardInstanceId = payment.Id;
                prevented = true;
            }

            pendingRemovalPrevention = null;
            return CompleteRemovalStage(
                answered.SubjectMinionId,
                answered.Defeat,
                answered.Reward,
                new DefeatRemovalAttempt(answered.Defeat.Id, answered.Defeat.TargetId),
                removalStage,
                // Milestone 8.37. A mandatory prevention is asked here too, ahead of
                // nothing since there is no pause to open over it - it can only ever ADD a reason the
                // piece stays, never remove one a payment already bought.
                prevented || removalStage.IsPrevented(subject) ||
                removalStage.ApplyAutomaticPrevention(subject));
        }

        /// <summary>
        /// Milestone 8.19c1. The rest of rulebook p.14 after a minion Defeat, produced from the
        /// resolution's own identity rather than from a list of steps a pause would have had to carry.
        /// It is the same generator whether or not anything stopped at the removal stage.
        /// </summary>
        internal IReadOnlyList<ResolutionStep> StepsAfterMinionDefeat(
            PendingAttackResolution resolution,
            IReadOnlyList<CardDefinition> definitions)
        {
            var steps = new List<ResolutionStep>
            {
                ResolutionStep.AtCheckpoint(TimingCheckpoint.AfterAttack)
            };
            steps.AddRange(CardTextAt(
                CardTextTiming.AfterTheAttack, resolution, resolution.ActorId, definitions,
                AttackResolutionOutcome.AttackSuccessful));
            steps.AddRange(StepsAfterCardText(
                CardTextTiming.AfterTheAttack, resolution, AttackResolutionOutcome.AttackSuccessful,
                theActionEndsHere: !SequenceIsOwedAfter(resolution)));
            return steps;
        }

        /// <summary>
        /// Milestone 8.35z27. Whether this resolution is a sequence of a modal Attack whose
        /// plan still names another. It is the one question every p.14 completion point asks before
        /// raising <c>ActionEnded</c>, so the six of them cannot disagree about when the action ends.
        /// </summary>
        internal bool SequenceIsOwedAfter(PendingAttackResolution resolution) =>
            pendingSequenceChain != null &&
            resolution != null &&
            pendingSequenceChain.Owns(resolution.ResolutionId) &&
            pendingSequenceChain.HasNextSequence;

        /// <summary>
        /// Forgets a modal Attack's plan once its last sequence has finished. A spent chain is not
        /// wrong to hold, but it would cross the wire and be written into every snapshot for the rest
        /// of the match, which is a fact about an action that is over.
        /// </summary>
        private void ClearSpentSequenceChain()
        {
            if (pendingSequenceChain != null && !pendingSequenceChain.HasNextSequence)
            {
                pendingSequenceChain = null;
            }
        }

        /// <summary>
        /// The removal stage once the two questions before it have been answered: it commits, or it
        /// does not, and either way it reports p.8's cause from where the miniature stood.
        ///
        /// <para>Extracted at milestone 8.19c1 so that a prevention paid for by a card produces exactly
        /// what p.18's immunity produces. The two reasons a removal stops are different rules and are
        /// decided in different places; what happens once one of them has decided is one thing, written
        /// once, and neither reason can drift into producing a different Defeat than the other.</para>
        /// </summary>
        private MinionDefeatResolution CompleteRemovalStage(
            MinionId targetMinionId,
            DefeatOutcome outcome,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            RemovalStageDecision removalStage,
            bool prevented)
        {
            var subject = BoardObjectReference.ForMinion(targetMinionId);
            var origin = adjacentMoveAuthority.Minions
                .Single(minion => minion.MinionId.Equals(targetMinionId)).Coordinate;
            if (prevented)
            {
                // Milestone 6.9: p.8's first sentence is answered by the Defeat, so a prevented removal
                // still records the cause. The stage is asked from where the miniature still stands,
                // and it discounts that miniature itself, which is why "the last remaining enemy
                // minion" can have been defeated while its piece is still on the board.
                return MinionDefeatResolution.Prevented(
                    outcome,
                    reward,
                    removalAttempt,
                    new DefeatRemovalResult(
                        outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                    removalStage.QualifyLanePush(subject, origin));
            }

            var removed = adjacentMoveAuthority.CommitValidatedMinionRemoval(targetMinionId);
            return MinionDefeatResolution.Removed(
                outcome,
                reward,
                removalAttempt,
                new DefeatRemovalResult(outcome.Id, outcome.TargetId, DefeatRemovalResultKind.Removed),
                removed,
                removalStage.QualifyLanePush(subject, origin));
        }

        internal MinionDefeatResolution ResolveEffectInvokedMinionDefeat(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId sourceAbilityId,
            HeroId actorId,
            MinionId targetMinionId,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            out PendingAttackResolution eventIdentity,
            string outcomeDiscriminator = null)
        {
            var target = adjacentMoveAuthority.Minions
                .Single(minion => minion.MinionId.Equals(targetMinionId));
            eventIdentity = new PendingAttackResolution(
                resolutionId,
                sourceCardInstanceId,
                sourceDefinitionReference,
                sourceAbilityId,
                actorId,
                ActionInvocationContext.PrintedPrimary,
                new ActionResolutionStepId("triggered-minion-defeat"),
                AttackResolutionPhase.ResolveMinionDefeat,
                AttackTarget.Minion(targetMinionId),
                null);

            var coins = StandardMinionProfileCatalog.Rulebook.Get(target.MinionType).DefeatCoins;
            var outcome = new DefeatOutcome(
                new DefeatOutcomeId($"{resolutionId.Value}:minion-defeat" +
                    (outcomeDiscriminator == null ? string.Empty : ":" + outcomeDiscriminator)),
                new DefeatTargetId(targetMinionId.Value),
                target.TeamId,
                actorId,
                coins,
                0);
            if (coinLedger == null) coinLedger = new HeroCoinLedger(participants);

            // Milestone 9.2, ruling W4. The rulebook's Defeat entry: "You can never defeat a friendly
            // unit. No exceptions." A selection that offered one is a composition defect, refused
            // loudly here rather than paid nothing (8.114b1 had admitted it for WU-Q4, since replaced).
            if (participants.GetTeamForHero(actorId).Id.Equals(target.TeamId))
                throw new InvalidOperationException(
                    "Card text asked to defeat a friendly minion; the rulebook forbids defeating a " +
                    "friendly unit with no exceptions (ruling W4).");

            if (DefeatReplacementQuery.TryApply(
                BoardObjectReference.ForMinion(targetMinionId), RuleActivity.Skill,
                RuleObject.ForHero(actorId), adjacentMoveAuthority, participants, modifications))
                return MinionDefeatResolution.Replaced();

            var reward = coinLedger.Award(outcome);
            if (!reward.Accepted)
                throw new InvalidOperationException("A triggered minion Defeat could not award its declared reward.");

            SpendMinionDefeatBounties(target, participants, definitions, instances);

            var attempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);

            // Milestone 8.19c1. The same identity, the same shared removal stage, and therefore the
            // same prevention question the Attack's own minion arm asks. This is what "or neither does"
            // means structurally: the offer is asked once, inside the decision both paths go through,
            // and this path can hold the pause because it supplies the identity its remainder is
            // rebuilt from. A null answer here is that pause; the caller reports it.
            return ResolveRemovalStage(
                targetMinionId, outcome, reward, attempt, participants, modifications,
                actorId, RuleActivity.Skill,
                RemovalPreventionOrigin.EffectInvokedMinionDefeat, eventIdentity, definitions,
                instances);
        }


        public AttackActionResolutionResult AnswerDefense(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution == null || pendingDefenseChoice == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.DefenseResponse)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            if (!command.ResolutionId.Equals(pendingDefenseChoice.ResolutionId) ||
                !command.ChoiceId.Equals(pendingDefenseChoice.ChoiceId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.StaleChoice);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var attackOffer = FindPendingAttackOffer(
                pendingAttackResolution,
                materializedDefinitions,
                materializedInstances);
            if (!attackOffer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var expectedAttackValue = CalculateAttackValue(
                attackOffer.Value,
                pendingAttackResolution.Target,
                materializedDefinitions,
                materializedInstances,
                participants,
                modifications,
                pendingAttackResolution.ActorSpaceAtActionStart);

            // Milestone 8.35z24. What p.14's step 3 needs is a target still STANDING, not a
            // target still reachable - the same narrowing milestone 8.23 made at step 6 in
            // <see cref="ResolveOutcome"/>, stated there at length and wired into that one site only.
            //
            // Step 6's case was a Defense text that moved the DEFENDER. This is the mirror of it, and
            // it needs no card text on the defender's side at all: rulebook p.14 names the target at
            // step 2, and the attacker's own "Before the attack:" body is applied AFTER that (see
            // ContinueAfterBeforeAttackText, which validates a text-BOUND target against the post-move
            // board and leaves an ordinarily-chosen one alone). Tigerclaw's Combat Reflexes and Leaping
            // Strike - "Before the attack: You may move 1 space. Target a unit adjacent to you." - are
            // the verified faces that reach it: the attacker legally steps out of contact between
            // naming his target and the defender answering, and re-deriving his reach here refused the
            // page its own step 3.
            //
            // It refused the WHOLE pause rather than one card, because this guard stands above both of
            // p.14's answers: the defender was offered Defense cards the preview scored as
            // SuccessfullyDefended, and Decline with them, and every one of the three was rejected
            // InvalidChoice. A pause no answer can leave is not an infeasible offer, it is a dead end,
            // and a deterministic driver that re-picks the same answer never leaves it.
            //
            // The narrowing is presence rather than legality for that reason, and it is exactly as
            // narrow as step 6's: the resolution id, the step id, the printed Attack value and the
            // whole Defense choice are all still compared below, so an answer that is not the one this
            // pause asked for is still refused.
            if (!adjacentMoveAuthority.Occupancies.Any(occupancy =>
                    occupancy.HeroId.Equals(pendingAttackResolution.TargetHeroId)) ||
                !PrintedValuesMatch(pendingAttackResolution.AttackValue, expectedAttackValue))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var currentDefenseOffers = FindDefenseOffers(
                materializedDefinitions, materializedInstances,
                pendingAttackResolution.TargetHeroId, modifications, attackOffer.Value, participants);
            if (!DefenseChoicesMatch(pendingAttackResolution, currentDefenseOffers, pendingDefenseChoice))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            if (command.ChoiceKind == DefenseResponseChoiceKind.Decline)
            {
                if (!pendingDefenseChoice.CanDecline || command.SelectedCardInstanceIds.Count != 0)
                {
                    return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
                }

                var resumedWithoutDefense = CreateOutcomeComparisonResolution(null, null);
                pendingAttackResolution = resumedWithoutDefense;
                pendingDefenseChoice = null;
                return AttackActionResolutionResult.Accept(
                    pendingAttackResolution,
                    null,
                    null,
                    new[]
                    {
                        ResolutionStep.FromFact(
                            CardActionResolutionEvent.DefenseDeclined(pendingAttackResolution)),
                        ResolutionStep.FromFact(
                            CardActionResolutionEvent.DefenseResponseCompleted(pendingAttackResolution)),
                        ResolutionStep.AtCheckpoint(
                            TimingCheckpoint.AttackResultComparison, pendingAttackResolution.TargetHeroId)
                    });
            }

            if (command.ChoiceKind != DefenseResponseChoiceKind.SelectCards ||
                command.SelectedCardInstanceIds.Count != 1 ||
                !command.SelectedCardInstanceIds[0].IsDefined)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var selectedCardId = command.SelectedCardInstanceIds[0];
            var matchingOffers = currentDefenseOffers
                .Where(candidate => candidate.SourceCardInstanceId.Equals(selectedCardId))
                .ToArray();
            var matchingInstances = materializedInstances
                .Where(candidate => candidate != null && candidate.Id.Equals(selectedCardId))
                .ToArray();
            if (matchingOffers.Length != 1 || matchingInstances.Length != 1 ||
                selectedCardId.Equals(pendingAttackResolution.SourceCardInstanceId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var defenseOffer = matchingOffers[0];
            var defenseCard = matchingInstances[0];
            var transition = defenseOffer.SourceCardStateTransition;

            // Milestone 8.112c. A resolved card a live grant lets its owner defend with "as if it was
            // in your hand" rests in the turn area; every other Defense card is in hand.
            var paidFromTurnArea = DefenseOfferQuery.IsSubstitute(
                defenseCard, materializedInstances, pendingAttackResolution.TargetHeroId,
                modifications);
            if (!defenseCard.OwnerId.Equals(pendingAttackResolution.TargetHeroId) ||
                !defenseCard.DefinitionReference.Equals(defenseOffer.SourceDefinitionReference) ||
                (!paidFromTurnArea && defenseCard.State.Location != transition.RequiredLocation) ||
                transition.RequiredLocation != CardLocation.Hand ||
                transition.DestinationLocation != CardLocation.DiscardPile)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            // Milestone 8.11. p.14 gives a Defense card two shapes and this is where they part. A
            // numeric Defense calculates a total and enters the page's step 5 comparison; a Block
            // prints no value at all, and its condition decides the attack outright - "If the Block
            // condition is fulfilled, you successfully defend, regardless of the attack value. If not,
            // you are defeated." Calculating a total for one would be inventing a number the card does
            // not print, which is the sentinel-Defense mistake `PrintedDefenseResolution` exists to
            // refuse.
            var blockResult = EvaluateBlock(defenseOffer, materializedDefinitions, modifications);
            var defenseValue = blockResult == DefenseBlockResult.None
                ? DefenseValueQuery.Calculate(
                    defenseOffer,
                    materializedDefinitions,
                    pendingAttackResolution.TargetHeroId,
                    pendingAttackResolution.Target.HeroBody,
                    participants,
                    adjacentMoveAuthority,
                    modifications,
                    heroItems)
                : null;
            var resumedWithDefense = CreateOutcomeComparisonResolution(
                defenseOffer, defenseValue, blockResult);

            // Routed through the lifecycle authority rather than transitioning the card directly.
            // The guards above already pin this transition to Hand -> DiscardPile, which is exactly
            // what TryDiscardFromHand performs, so the discard semantics are unchanged - but
            // the authority additionally requires the card to be unplayed with no resolution status,
            // which the duplicated guard here never checked. See milestone 6.1f / finding F613-06.
            if (!(paidFromTurnArea
                    ? CardLifecycleAuthority.TryDiscardResolvedCard(defenseCard)
                    : CardLifecycleAuthority.TryDiscardFromHand(defenseCard)))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            pendingAttackResolution = resumedWithDefense;
            pendingDefenseChoice = null;
            var defenseSteps = new List<ResolutionStep>
            {
                ResolutionStep.FromFact(
                    CardActionResolutionEvent.DefenseCardDiscarded(pendingAttackResolution))
            };

            // p.14 step 4: "Defender applies Defense action text, if any." It sits between step 3's
            // discard and step 5's Defense total, which is why it is emitted here and not beside the
            // attacker's own text: the defender's card is chosen by then, and p.10 gives the choices
            // inside it to the defending hero rather than to the attacker.
            defenseSteps.AddRange(CardTextAt(
                CardTextTiming.DefenseAction,
                pendingAttackResolution,
                pendingAttackResolution.TargetHeroId,
                materializedDefinitions,
                defenseOffer.SourceDefinitionReference,
                defenseOffer.ActionDefinitionId,
                defenseCard.Id));
            defenseSteps.AddRange(StepsAfterCardText(
                CardTextTiming.DefenseAction,
                pendingAttackResolution,
                AttackResolutionOutcome.None));

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                null,
                null,
                defenseSteps);
        }


        public AttackActionResolutionResult ResolveOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.CompareOutcome ||
                pendingAttackTargetChoice != null ||
                pendingDefenseChoice != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var attackOffer = FindPendingAttackOffer(
                pendingAttackResolution,
                materializedDefinitions,
                materializedInstances);
            if (!attackOffer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var expectedAttackValue = CalculateAttackValue(
                attackOffer.Value,
                pendingAttackResolution.Target,
                materializedDefinitions,
                materializedInstances,
                participants,
                modifications,
                pendingAttackResolution.ActorSpaceAtActionStart);
            // Milestone 8.23. What p.14's comparison needs is a target still STANDING, not a target
            // still reachable. The page names its target at step 2 and never names it again; step 4
            // is the defender's own card text, and Tigerclaw's Sidestep and Evade are the first
            // verified faces whose Defense text MOVES the defender - "Block a ranged attack. You may
            // move 1 space." Re-deriving the attacker's reach afterwards refused the page its own
            // step 6, because the defender had legitimately stepped out of contact between being
            // named and being compared, and the whole command was rolled back as an engine fault.
            //
            // The narrowing is presence rather than legality for that reason, and it is narrow: the
            // step id, the printed Attack value and the completed Defense are all still compared, so
            // a resolution that is not the one that was validated is still refused here.
            if (!pendingAttackResolution.CurrentStepId.Equals(
                    new ActionResolutionStepId("attack-outcome-comparison")) ||
                !adjacentMoveAuthority.Occupancies.Any(occupancy =>
                    occupancy.HeroId.Equals(pendingAttackResolution.TargetHeroId)) ||
                !PrintedValuesMatch(pendingAttackResolution.AttackValue, expectedAttackValue) ||
                !CompletedDefenseMatches(
                    pendingAttackResolution,
                    materializedDefinitions,
                    materializedInstances,
                    participants,
                    adjacentMoveAuthority,
                    modifications,
                    heroItems))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var completedResolution = pendingAttackResolution;
            // Milestone 8.11. p.14 step 6 reads what step 3 decided: a fulfilled Block defends
            // regardless of the totals, an unfulfilled one defeats the defender, and everything else
            // is the page.s numeric comparison exactly as before.
            var comparison = AttackOutcomeComparison.Create(
                completedResolution.AttackTotal,
                completedResolution.DefenseTotal,
                completedResolution.BlockResult);
            var outcome = comparison.Outcome;
            HeroDefeatResolution heroDefeat = null;
            var events = new List<ResolutionStep>
            {
                ResolutionStep.FromFact(
                    CardActionResolutionEvent.AttackOutcomeDetermined(completedResolution, outcome))
            };
            if (outcome == AttackResolutionOutcome.AttackSuccessful)
            {
                heroDefeat = ResolveHeroDefeat(
                    EffectInvokedDefeatSource.FromAttack(completedResolution),
                    completedResolution.TargetHeroId,
                    materializedDefinitions,
                    materializedInstances,
                    participants,
                    modifications,
                    events,
                    defeatedBody: completedResolution.Target.HeroBody);
                if (heroDefeat == null)
                {
                    return RejectAttack(
                        CardActionResolutionRejectionReason.DefeatConsequenceStateUnavailable);
                }
            }

            events.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.AfterAttack));

            // p.14 step 7: "Apply 'After the attack:' text on the Attack card, if any" - after step 6
            // has checked the attack and removed the defending hero if it succeeded, which is why this
            // sits below the Defeat stage above rather than beside the outcome. p.13 says the same
            // thing in words: the text is applied "at the very end, after the target either defends, or
            // is defeated and removed".
            events.AddRange(CardTextAt(
                CardTextTiming.AfterTheAttack,
                completedResolution,
                completedResolution.ActorId,
                materializedDefinitions,
                outcome));
            events.AddRange(StepsAfterCardText(
                CardTextTiming.AfterTheAttack, completedResolution, outcome,
                theActionEndsHere: !SequenceIsOwedAfter(completedResolution)));

            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
            ClearSpentSequenceChain();
            return AttackActionResolutionResult.Accept(
                null,
                null,
                null,
                events,
                outcome,
                outcomeComparison: comparison,
                heroDefeat: heroDefeat);
        }


        /// <summary>
        /// Rulebook p.13's "When a Hero is Defeated" list, performed in the page's printed order and
        /// reported as it happens. Returns <c>null</c>, having changed nothing, when this composition
        /// cannot price the Defeat.
        ///
        /// Where it runs is p.14's, not a choice made here. That page prints the numbered
        /// <em>Attack/Defense Detailed Order</em>, whose step 6 is "Check if the attack was successful;
        /// if it was, remove the defending hero" and whose step 7 is "Apply 'After the attack:' text on
        /// the Attack card" - so the whole list belongs inside the Attack action and ahead of the
        /// after-attack checkpoint, which is exactly where the caller raises it.
        ///
        /// Every derivation the list needs is read and every refusal is taken before the first mutation,
        /// so a composition that cannot finish the list never starts it: the hero is still standing, its
        /// card is still unresolved, its team still has its counters, and the command is refused whole.
        /// </summary>
        /// <param name="assistCoinMultiplier">
        /// Milestone 8.57c. What THIS Defeat multiplies p.13's assist coins by - Rowenna's Glorious
        /// Triumph, "your friendly heroes gain triple assist coins". p.14's step 6 and every Defeat
        /// composed before her pass the page's own schedule, so the number they read is unchanged.
        /// It is an argument rather than a rule because the clause is true of one Defeat and a
        /// modification set, derived once at the command boundary, cannot say "this one".
        /// </param>
        private HeroDefeatResolution ResolveHeroDefeat(
            EffectInvokedDefeatSource completedResolution,
            HeroId defeatedHeroId,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> events,
            int assistCoinMultiplier =
                GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities
                    .UnmultipliedAssistCoins,
            int defeatedBody = 0)
        {
            if (heroLevels == null || teamLives == null)
            {
                return null;
            }

            var defeatedTeamId = participants.GetTeamForHero(defeatedHeroId).Id;

            // p.13's two readings of the defeated hero, taken before anything moves. The Level is
            // authoritative state; the lowest Tier is read from the cards the hero is playing with -
            // hand, turn area and discard pile - whatever it has played, discarded or is still holding
            // when it is defeated. Milestone 8.49: the whole-pool reading this replaced also counted
            // the Tier I cards p.15's upgrades RETURN TO THE DECK, so every hero read as Tier I at
            // every Level and every Defeat cost one Life counter; `HeroDefeatCostQuery` states the
            // scope once for this backend and for the rail's worth badge.
            var defeatedLevel = heroLevels.GetLevel(defeatedHeroId);
            var hasTier = HeroDefeatCostQuery.TryGetLowestCardTier(
                defeatedHeroId,
                definitions,
                instances,
                out var lowestTier);
            var defeatCoins = HeroDefeatRewardSchedule.DefeatCoins(defeatedLevel);
            // Milestone 8.57c. The ASSIST column with this one Defeat's printed multiplier composed
            // into it, read here for the reason 8.35z31's additional Life counter is read here: p.13's
            // numbers are produced once and spent once, so a clause that changes one of them changes
            // it where it is produced rather than paying a second time afterwards. The defeating
            // hero's own coins above are a different sentence and do not move.
            var assistCoins = hasTier
                ? HeroDefeatRewardSchedule.AssistCoins(lowestTier, assistCoinMultiplier)
                : 0;
            // Milestone 8.35z31. p.13's own number, plus whatever the attacking hero has
            // declared it adds - Ursafar's Tear, and nothing else in the roster. It is composed into
            // the count here rather than spent again later because the ruling that admits it says the
            // charge is computed once and paid once; a hero with no such declaration reads exactly the
            // number the page prints, which is what makes this change invisible to every other face.
            //
            // Milestone 8.40a adds the second subject, and the addition is one argument because
            // there is only one place where p.13's cost is produced. Bain's bounty marker prints the
            // mirror of Tear's sentence - "a hero with a bounty marker spends 1 additional [Life] life
            // counter when defeated" - which is a rule about the DEFEATED HOLDER, and a held marker's
            // property already affects the holder. It is a SECOND declaration family rather than the
            // first one read about a second object, because a bounty-wearing hero who defeats somebody
            // must not charge their victim: see RuleModificationKind's two members.
            //
            // THE READ IS TAKEN HERE, BEFORE p.13's OWN MARKER RETURN BELOW, and the order is the
            // printed one rather than a convenience: the page prices the Defeat and then returns the
            // defeated hero's markers, so the bounty a defeat removes still charges for the defeat
            // that removed it. The set itself is derived at the command boundary before this list
            // starts, which makes the ordering structural rather than positional.
            var additionalLifeCounters = modifications == null
                ? 0
                : modifications.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(completedResolution.ActorId),
                    RuleObject.ForHero(defeatedHeroId));
            var lifeCounters = hasTier
                ? HeroDefeatRewardSchedule.LifeCounters(lowestTier, additionalLifeCounters)
                : 0;

            // Milestone 9.13, the hero arm of 8.118's minion fix. One Attack can defeat two heroes -
            // its target, then a second hero its "After the attack" text defeats (Wasp's Electroblast
            // on an empty-handed hero) - and both Defeats were named "<resolution>:hero-defeat", so
            // p.13's second reward was refused as already awarded. The first Defeat keeps the name
            // every existing record carries; a later one in the same resolution adds the defeated
            // hero, so each Defeat is paid once.
            var heroDefeatId = new DefeatOutcomeId($"{completedResolution.ResolutionId.Value}:hero-defeat");
            if (coinLedger != null && coinLedger.AwardedOutcomeIds.Contains(heroDefeatId))
            {
                heroDefeatId = new DefeatOutcomeId($"{heroDefeatId.Value}:{defeatedHeroId.Value}");
            }

            var outcome = new DefeatOutcome(
                heroDefeatId,
                new DefeatTargetId(defeatedHeroId.Value),
                defeatedTeamId,
                completedResolution.ActorId,
                defeatCoins,
                assistCoins);
            if (coinLedger == null) coinLedger = new HeroCoinLedger(participants);

            // p.13, first line: "Remove the defeated hero from the board." The prevention question is
            // asked through milestone 6.6's shared decision rather than answered here, so the hero path
            // and the minion path cannot disagree about whether a removal can be stopped - the exact
            // disagreement 6.6b closed. p.8's cause is asked from where the miniature still stands.
            var subject = BoardObjectReference.ForHero(defeatedHeroId);
            var removalStage = new RemovalStageDecision(
                completedResolution.Activity,
                RuleObject.ForHero(completedResolution.ActorId),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                participants,
                modifications,
                LanePushCauseKind.Defeat);
            var removalAttempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
            var prevented = removalStage.IsPrevented(subject);
            // Milestone 9.21a. The space p.8's cause is asked from is the defeated BODY's - the one the
            // Attack targeted - or the hero's acting body for a Defeat no Attack aimed (an effect-invoked
            // Defeat names the hero, not a body). A hero with one body reads its one space either way.
            var origin = default(HexCoordinate);
            if (defeatedBody < HeroOccupancy.FirstBody ||
                !adjacentMoveAuthority.TryGetHeroBodyCoordinate(defeatedHeroId, defeatedBody, out origin))
            {
                adjacentMoveAuthority.TryGetHeroCoordinate(defeatedHeroId, out origin);
            }

            // p.13's removal, RZ-Q3: a Defeat is the hero's, so every body leaves the board at once.
            IReadOnlyList<HeroOccupancy> removedBodies = Array.Empty<HeroOccupancy>();
            if (!prevented)
            {
                removedBodies = adjacentMoveAuthority.CommitValidatedHeroRemoval(defeatedHeroId);
            }

            var lanePush = removalStage.QualifyLanePush(subject, origin);

            // p.13, second line: "If the defeated hero had an unresolved card, it is resolved, without
            // performing an action." The Initiative order reads the card instances themselves, so a card
            // resolved here is simply never offered again - there is no separate acknowledgement to
            // make, and making one would require the defeated hero to be the selected actor, which it
            // never is: p.13 forbids attacking a friendly hero, so the target is always someone else.
            var unresolvedCard = instances.FirstOrDefault(candidate =>
                candidate != null &&
                candidate.OwnerId.Equals(defeatedHeroId) &&
                candidate.State.Location == CardLocation.TurnArea &&
                candidate.State.HasBeenPlayed &&
                candidate.State.ResolutionStatus == CardResolutionStatus.Unresolved);
            var resolvedCardId = default(CardInstanceId);
            if (unresolvedCard != null && CardLifecycleAuthority.TryResolveAfterAction(unresolvedCard))
            {
                resolvedCardId = unresolvedCard.Id;
            }

            // p.13, third line, second half: "Any markers they had are returned." Milestone 6.12's
            // per-attachment returns, unchanged. p.19's "tokens are not removed when the hero who placed
            // them is defeated" is why nothing here touches a token. The attachments are returned now
            // and reported below, so the mutations stay in the page's order while the stream does too.
            var returnedMarkers = ComponentCleanupRules.MarkersReturnedOnDefeat(
                    markers?.Attachments ?? Array.Empty<MarkerAttachment>(),
                    defeatedHeroId)
                .Select(attachment => markers.Return(attachment.Id))
                .ToArray();

            // p.13, fourth line: the coins. The accounting refuses a second award for the same outcome
            // id, so a re-execution inside one resolution collects nothing twice.
            var reward = coinLedger.Award(outcome);
            if (!reward.Accepted)
            {
                throw new InvalidOperationException(
                    "A prevalidated hero Defeat could not award its p.13 reward: " + reward.RejectionReason + ".");
            }

            // p.13, fifth line: the Life counters, and the game end when the last one goes.
            var lifeSpend = default(TeamLifeSpend);
            var hasLifeSpend = false;
            if (lifeCounters > 0)
            {
                var spend = teamLives.TrySpend(defeatedTeamId, lifeCounters);
                if (!spend.Accepted)
                {
                    throw new InvalidOperationException(
                        "A prevalidated hero Defeat could not spend its p.13 Life counters: " +
                        spend.RejectionReason + ".");
                }

                lifeSpend = spend.Spend;
                hasLifeSpend = true;
            }

            var resolved = prevented
                ? HeroDefeatResolution.Prevented(
                    outcome, reward, removalAttempt,
                    new DefeatRemovalResult(
                        outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                    resolvedCardId, lifeSpend, hasLifeSpend, lanePush)
                : HeroDefeatResolution.Removed(
                    outcome, reward, removalAttempt,
                    new DefeatRemovalResult(
                        outcome.Id, outcome.TargetId, DefeatRemovalResultKind.Removed),
                    removedBodies, resolvedCardId, lifeSpend, hasLifeSpend, lanePush);

            // This is the only completed hero-Defeat seam. Record after every p.13 mutation has
            // succeeded and before publishing its facts, so a refused or incomplete Attack cannot
            // trip a later round-history condition. A prevented removal still records the Defeat:
            // HeroDefeatResolution explicitly treats that as a complete Defeat.
            heroDefeatHistory?.Record(resolved.Defeat);

            // The stream, in p.13's printed order - the same order the mutations above ran in. The
            // cancellation checkpoint and the marker returns belong to the page's third line and are
            // spliced in at exactly that point, between the resolved card and the coins.
            foreach (var fact in resolved.Facts)
            {
                if (fact == HeroDefeatFactKind.RewardAwarded)
                {
                    // p.13, third line, first half: "Defeated hero's actions with Active effects are
                    // canceled." p.17 states the same cancellation from the effect's own side - it ends
                    // when "you are defeated" - and the lifecycle has carried that condition since
                    // milestone 6.1q with nothing raising it. This is what raises it: the checkpoint
                    // names the defeated hero, and the turn authority applies it to every effect that
                    // declared the condition.
                    events.Add(ResolutionStep.AtCheckpoint(
                        TimingCheckpoint.ControllerHeroDefeated, defeatedHeroId));
                    foreach (var attachment in returnedMarkers)
                    {
                        events.Add(ResolutionStep.FromFact(
                            CardActionResolutionEvent.MarkerReturnedOnHeroDefeat(
                                completedResolution, defeatedHeroId, attachment)));
                    }
                }

                events.Add(fact == HeroDefeatFactKind.DefeatRecorded
                    ? ResolutionStep.FromFact(
                        CardActionResolutionEvent.HeroDefeatRecorded(completedResolution, defeatedHeroId, resolved))
                    : ResolutionStep.FromFact(
                        CardActionResolutionEvent.HeroDefeatFact(completedResolution, defeatedHeroId, resolved, fact)));
            }

            return resolved;
        }

        /// <summary>
        /// Milestone 8.10. Rulebook p.13's "When a Hero is Defeated" list run against a hero this
        /// Attack did not target, which is what Electroblast's rider escalates to when the hero it
        /// chose has no card to give: "An enemy hero in radius and not adjacent to you discards a card,
        /// or is defeated."
        ///
        /// <para>It is the same list, in the same order, through the same method - not a second one.
        /// Everything p.13 prices and spends is authority state this backend already holds, and a
        /// second copy of the chain is exactly the disagreement milestone 6.6b closed between the hero
        /// and minion removal paths. What the caller supplies is only which hero the page is being run
        /// about; the acting hero, the activity and the action identity are the attack's own, because
        /// the rider is printed on the card performing it.</para>
        ///
        /// <para><c>null</c>, having changed nothing, when this composition cannot price a Defeat -
        /// the same answer the Attack path gets, for the same reason.</para>
        /// </summary>
        internal HeroDefeatResolution ResolveEffectInvokedHeroDefeat(
            EffectInvokedDefeatSource source,
            HeroId defeatedHeroId,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> events,
            int assistCoinMultiplier =
                GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities
                    .UnmultipliedAssistCoins) =>
            ResolveHeroDefeat(
                source,
                defeatedHeroId,
                definitions,
                instances,
                participants,
                modifications,
                events,
                assistCoinMultiplier);

        internal AttackDefenseBackendRestoreState CaptureRestoreState() =>
            new AttackDefenseBackendRestoreState(pendingAttackResolution, pendingSequenceChain);

        /// <param name="verifiedTargetChoice">
        /// An independently supplied Attack target choice to cross-check against the
        /// derived one, or <c>null</c> to accept the derived choice. Supplying one for a
        /// phase that has no target choice is rejected.
        /// </param>
        /// <param name="verifiedDefenseChoice">
        /// An independently supplied Defense response choice to cross-check against the
        /// authoritative Defense offers, or <c>null</c> to accept the derived choice.
        /// Supplying one for a phase that has no Defense choice is rejected.
        /// </param>
        internal static AttackDefenseCardActionStrategyBackend RestoreAttack(
            AdjacentMoveAuthority adjacentMoveAuthority,
            AttackDefenseBackendRestoreState state,
            PendingAttackTargetChoice verifiedTargetChoice,
            PendingDefenseResponseChoice verifiedDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            HeroCoinLedger coinLedger,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            EffectRosterState rosterState = null,
            bool defenseInterrupted = false)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var pendingResolution = state.PendingResolution;
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var backend = new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority, coinLedger, markers);

            // Bound before the verification below, because a paused Defense comparison is re-derived
            // against the same items the live one used; a restore that verified it without them would
            // refuse a legitimate payload as soon as a hero owns one.
            backend.BindHeroItems(heroItems);
            if (rosterState != null)
            {
                backend.BindRosterState(rosterState);
            }

            // Milestone 8.35z27, and it has to be bound before the derivations below run:
            // a sequence of a modal Attack derives its target set from its own bullet's sentence and
            // subtracts the units earlier sequences took, and both of those live here. A restore that
            // rebuilt the choice without the chain would offer the action's printed set instead of the
            // bullet's, which is the same class of defect milestone 8.35z21 fixed one level up - a
            // restored decision offering what the live one had already excluded.
            backend.pendingSequenceChain = state.SequenceChain;
            var offer = FindPendingAttackOffer(pendingResolution, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                throw new ArgumentException("Pending Attack resolution no longer references an available offer.", nameof(state));
            }

            PendingAttackTargetChoice targetChoice = null;
            PendingDefenseResponseChoice defenseChoice = null;
            if (pendingResolution.Phase == AttackResolutionPhase.ChooseTarget)
            {
                var expectedChoice = backend.CreateAttackTargetChoice(
                    pendingResolution,
                    offer.Value,
                    materializedDefinitions,
                    participants,
                    modifications,
                    materializedInstances);
                if (verifiedDefenseChoice != null ||
                    (verifiedTargetChoice != null &&
                        !AttackTargetChoicesMatch(expectedChoice, verifiedTargetChoice)))
                {
                    throw new ArgumentException("Pending Attack target choice does not match authoritative state.", nameof(verifiedTargetChoice));
                }

                targetChoice = verifiedTargetChoice ?? expectedChoice;
            }
            else if (pendingResolution.Phase == AttackResolutionPhase.ApplyBeforeAttackText)
            {
                // Milestone 8.19a. Rulebook p.14's step 1 has no choice of its own and no Attack Total
                // yet, so what is cross-checked here is what the page has decided by then and nothing
                // more: that the target the attack bound - if it has bound one at all, which an Attack
                // whose text names its target has not - is still a unit this Attack may take.
                if (verifiedTargetChoice != null || verifiedDefenseChoice != null)
                {
                    throw new ArgumentException(
                        "Rulebook p.14's step 1 holds no target or Defense choice of its own.",
                        nameof(verifiedTargetChoice));
                }

                if (pendingResolution.Target.IsDefined &&
                    !backend.FindLegalAttackTargets(
                        pendingResolution.ActorId, offer.Value, materializedDefinitions, participants,
                        modifications, backend.SequenceBulletFor(pendingResolution),
                        materializedInstances)
                        .Contains(pendingResolution.Target))
                {
                    throw new ArgumentException(
                        "Pending Attack target is no longer legal.", nameof(pendingResolution));
                }
            }
            else
            {
                // Milestone 8.35z24. The same narrowing the live paths make, for the same
                // reason and at the same two phases this branch covers - DefenseResponse and
                // CompareOutcome. A capture taken while p.14 stands at either, after the attacker's own
                // "Before the attack:" body or the defender's step-4 rider has moved somebody out of
                // contact, is a legitimate payload of a legitimate position; asking it for a still
                // REACHABLE target made it unreadable by the very session that wrote it, which is
                // milestone 8.23's step-6 finding arriving through the restore door instead.
                if (!backend.adjacentMoveAuthority.Occupancies.Any(occupancy =>
                    occupancy.HeroId.Equals(pendingResolution.TargetHeroId)))
                {
                    throw new ArgumentException("Pending Attack target is no longer on the board.", nameof(pendingResolution));
                }

                var expectedAttackValue = backend.CalculateAttackValue(
                    offer.Value,
                    pendingResolution.Target,
                    materializedDefinitions,
                    materializedInstances,
                    participants,
                    modifications,
                    pendingResolution.ActorSpaceAtActionStart);
                if (verifiedTargetChoice != null ||
                    !PrintedValuesMatch(pendingResolution.AttackValue, expectedAttackValue))
                {
                    throw new ArgumentException("Pending Attack continuation does not match authoritative state.", nameof(state));
                }

                if (pendingResolution.Phase == AttackResolutionPhase.DefenseResponse)
                {
                    if (defenseInterrupted && verifiedDefenseChoice != null)
                        throw new ArgumentException(
                            "An interrupted Defense cannot also offer a card.",
                            nameof(verifiedDefenseChoice));
                    var expectedDefenseOffers = backend.FindDefenseOffers(
                        materializedDefinitions, materializedInstances,
                        pendingResolution.TargetHeroId, modifications, offer.Value, participants);
                    if (verifiedDefenseChoice != null &&
                        !DefenseChoicesMatch(pendingResolution, expectedDefenseOffers, verifiedDefenseChoice))
                    {
                        throw new ArgumentException("Pending Defense response choice does not match authoritative Attack state.", nameof(verifiedDefenseChoice));
                    }

                    defenseChoice = defenseInterrupted ? null : verifiedDefenseChoice ?? new PendingDefenseResponseChoice(
                        pendingResolution.ResolutionId,
                        pendingResolution.CurrentStepId,
                        pendingResolution.TargetHeroId,
                        pendingResolution.ActorId,
                        // Milestone 8.80, and the restore door says the same three as the live one.
                        pendingResolution.SourceCardInstanceId,
                        pendingResolution.SourceDefinitionReference,
                        pendingResolution.ActionDefinitionId,
                        pendingResolution.AttackTotal,
                        expectedDefenseOffers,
                        true);
                }
                else if (pendingResolution.Phase == AttackResolutionPhase.CompareOutcome)
                {
                    if (verifiedDefenseChoice != null ||
                        !pendingResolution.CurrentStepId.Equals(new ActionResolutionStepId("attack-outcome-comparison")) ||
                        !CompletedDefenseMatches(
                            pendingResolution,
                            materializedDefinitions,
                            materializedInstances,
                            participants,
                            adjacentMoveAuthority,
                            modifications,
                            heroItems))
                    {
                        throw new ArgumentException("Completed Defense response does not match authoritative Attack state.", nameof(state));
                    }
                }
                else
                {
                    throw new ArgumentException("Pending Attack resolution has an unsupported phase.", nameof(state));
                }
            }

            backend.pendingAttackResolution = pendingResolution;
            backend.pendingAttackTargetChoice = targetChoice;
            backend.pendingDefenseChoice = defenseChoice;
            return backend;
        }

        private AttackActionResolutionResult RejectAttack(CardActionResolutionRejectionReason reason)
        {
            return AttackActionResolutionResult.Reject(
                reason,
                pendingAttackResolution,
                pendingAttackTargetChoice,
                pendingDefenseChoice);
        }

        /// <summary>
        /// Milestone 8.11. Rulebook p.14's Block condition, evaluated at the page's step 3 where the
        /// defender's card is revealed, or <see cref="DefenseBlockResult.None"/> when the card the
        /// defender revealed is an ordinary numeric Defense.
        ///
        /// <para><b>The condition is the card's, and every Block in this build prints the same one.</b>
        /// Wasp's three Green cards all read "Block a ranged attack", so what is asked is p.13's
        /// printed subtype of the attack being answered - a typed fact the attack already carries,
        /// rather than a sentence parsed at runtime. A Block card printing a different condition would
        /// declare it; none does yet, and inventing the vocabulary for one before a face needs it is
        /// what milestone 8.3 refused when it declared the resolution shape and left the evaluation
        /// here.</para>
        ///
        /// <para>The answer is decided once, when the card is revealed, and carried to the comparison
        /// rather than re-derived there: the attack's subtype cannot change in between, but a rule that
        /// asked twice could still be made to disagree by a future modification, and p.14 asks it
        /// once.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.11a moved the condition itself into <see cref="DefenseBlockQuery"/>, because the
        /// defence preview a defender chooses from has to reach the same verdict this does. What is
        /// left here is naming the attack being answered.
        ///
        /// <para>Milestone 8.40g adds the second thing a condition may be answered from: the marker
        /// register this backend has held since milestone 6.12, so that p.13's "any markers they had
        /// are returned" happens where the Defeat is recorded. Reading it here needed no widened
        /// signature and no component port, exactly as 8.40c's bullet narrowing did not - and it is
        /// the same object the round authority hands the projection the preview is built from, which
        /// is what makes the two verdicts one fact rather than two derivations.</para>
        /// </summary>
        /// <remarks>
        /// Milestone 8.57e adds the third thing a condition may be answered from: the attacker, and the
        /// live set that may have changed what rulebook p.13 makes of his Attack - Rowenna's Fabled
        /// Lance. Both travel from this resolution and this call, which is the same object the round
        /// authority hands the projection the preview is built from, so the two verdicts stay one fact.
        /// </remarks>
        private DefenseBlockResult EvaluateBlock(
            ActionOffer defenseOffer,
            IReadOnlyList<CardDefinition> definitions,
            RuleModificationSet modifications) =>
            DefenseBlockQuery.Evaluate(
                defenseOffer,
                definitions,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                markers,
                pendingAttackResolution.ActorId,
                modifications,
                adjacentMoveAuthority,
                heroItems,
                capabilities,
                rosterState.IsAvailable
                    ? rosterState.TurnSlots.LastOrDefault()?.TurnIndex ?? 0
                    : 0);

        /// <summary>
        /// Milestone 8.22, finding `F-ATK-01`: the pre-consequence target space is carried across
        /// p.14's outcome comparison.
        ///
        /// <para>Milestone 8.19a records where the target was standing before steps 6 and 7 could take
        /// the piece off the board, and every other rebuild of the resolution passes it along. This
        /// one - the hero arm's rebuild, once the Defense has been answered - did not, and the
        /// parameter defaults to "no space recorded". Everything downstream of it therefore believed
        /// the attack had no target space: <c>attacked-target-space@1</c> reported
        /// <c>ImpossibleMandatory</c> and <c>pre-consequence-adjacency@1</c> answered no, so on a
        /// <em>hero</em> target Point Blank Shot pushed nobody and Shootout and Bullet Hell removed
        /// nothing - all three silently, because a mandatory step that cannot run simply ends the
        /// text.</para>
        ///
        /// <para>It was invisible until Sabina, because she is the first printed content to read
        /// either capability, and invisible on a minion target either way, because the minion arm
        /// never comes through here.</para>
        /// </summary>
        private PendingAttackResolution CreateOutcomeComparisonResolution(
            ActionOffer? defenseOffer,
            ValueResult defenseValue,
            DefenseBlockResult blockResult = DefenseBlockResult.None)
        {
            return new PendingAttackResolution(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.InvocationContext,
                new ActionResolutionStepId("attack-outcome-comparison"),
                AttackResolutionPhase.CompareOutcome,
                pendingAttackResolution.Target,
                pendingAttackResolution.AttackValue,
                defenseOffer,
                defenseValue,
                pendingAttackResolution.ExcludedTarget,
                blockResult,
                pendingAttackResolution.TargetSpaceBeforeConsequences,
                actorSpaceAtActionStart: pendingAttackResolution.ActorSpaceAtActionStart,
                excludedTargets: pendingAttackResolution.ExcludedTargets);
        }

        private PendingAttackTargetChoice CreateAttackTargetChoice(
            PendingAttackResolution resolution,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IEnumerable<CardInstance> cardInstances = null)
        {
            // Milestone 8.10. "on a different target" is the repeated instance's own narrowing, taken
            // out of the same legal set the first instance chose from rather than out of a second
            // query, so everything else the page decides about a target - reach, the printed policy,
            // p.18's immunity - is decided once and the exclusion is the only difference.
            var legal = FindLegalAttackTargets(
                resolution.ActorId, offer, definitions, participants, modifications,
                SequenceBulletFor(resolution), cardInstances);
            if (resolution.ExcludedTargets.Count > 0)
            {
                legal = legal.Where(target =>
                    !resolution.ExcludedTargets.Any(excluded => excluded.IsSameUnit(target))).ToArray();
            }

            // Milestone 8.35z27. Ruling 19's "(on different targets)" is the card's own
            // narrowing across the bullets of one modal action, and it is taken out of the same legal
            // set every other sequence chooses from - the discipline milestone 8.10 set for the
            // repeat's exclusion, restated for a wider one. An unconstrained card excludes nothing,
            // which is the rider: the same unit may be both targets while it still satisfies each
            // bullet's own sentence.
            if (pendingSequenceChain != null && pendingSequenceChain.Owns(resolution.ResolutionId))
            {
                var alreadyTaken = pendingSequenceChain.ExcludedTargets;
                if (alreadyTaken.Count > 0)
                {
                    legal = legal.Where(target =>
                        !alreadyTaken.Any(taken => taken.IsSameUnit(target))).ToArray();
                }
            }

            legal = NarrowedByTheProducingClause(resolution, legal, definitions);

            return new PendingAttackTargetChoice(
                resolution.ResolutionId,
                resolution.CurrentStepId,
                resolution.ActorId,
                legal);
        }

        /// <summary>
        /// What the clause that produced a REPEATED instance lets it take, out of the legal set every
        /// instance of this Attack chooses from. Every other instance passes through unchanged.
        ///
        /// <para>Milestone 8.24. "on a different enemy hero" is the repeat clause narrowing what is
        /// left of the set, and Xargatha's Cleave and Rapid Thrusts are what print it. It is read off
        /// the trusted definition rather than carried on the resolution: a repeated instance already
        /// knows it is one and which action it repeats, so the card can be asked again - which is why
        /// the clause moves no snapshot field, and why a payload naming a minion cannot put one back
        /// into the set. Milestone 8.35z46 widened the derivation to a card that prints a CHAIN of
        /// clauses, where the card alone cannot say which one produced this instance; nothing about a
        /// single-clause card changed.</para>
        ///
        /// <para><b>Milestone 8.75d made it a method, because a second caller needed the identical
        /// reading.</b> An Attack whose target is bound by card text reaches p.14's step 1 with
        /// nothing bound and hands a unit over from its own printed subject, and
        /// <see cref="ContinueAfterBeforeAttackText"/> validates that unit against the legal set -
        /// which, until this milestone, was the UNNARROWED one. So Imminent Eruption's "may repeat
        /// once on a minion" would have accepted a hero at the bind. One method, asked at both
        /// sites, is what keeps p.13's answer and the card text's answer the same answer.</para>
        /// </summary>
        private IReadOnlyList<AttackTarget> NarrowedByTheProducingClause(
            PendingAttackResolution resolution,
            IReadOnlyList<AttackTarget> legal,
            IReadOnlyList<CardDefinition> definitions)
        {
            if (resolution.InvocationContext != ActionInvocationContext.Repeated)
            {
                return legal;
            }

            var admitted = RepeatAdmitsOnly(resolution, definitions);
            if (admitted.HasValue)
            {
                legal = legal.Where(target => target.Kind == admitted.Value).ToArray();
            }

            // Milestone 8.69b. "on a different unit adjacent to you" - Brynn the Seeker's Split
            // Attack. It is the SAME derivation the kind narrowing above makes, of the same
            // trusted definition, and it narrows the other half of what a target is: p.19's
            // innermost ring around the attacker, in place of the printed Range 2 every other
            // instance of that action reaches. Applied here rather than inside
            // FindLegalAttackTargets for milestone 8.35z45's reason - the query answers p.13's
            // question and this is one card's own extra word narrowing the answer - and applied to
            // the one set both the offer and the answer read, so CanStartRepeatedAttack refuses a
            // repeat with an empty ring rather than offering one and discovering it after.
            if (RepeatReachesOnlyUnitsAdjacentToTheActor(resolution, definitions))
            {
                var actorAt = ActorSpaceOf(resolution.ActorId);
                legal = actorAt.HasValue
                    ? legal.Where(target =>
                        {
                            var at = TargetSpaceOf(target);
                            return at.HasValue &&
                                HexDistanceQuery.Calculate(actorAt.Value, at.Value) == 1;
                        })
                        .ToArray()
                    // An attacker standing nowhere has no ring, so the clause admits nobody. It
                    // is unreachable through a started Attack, whose actor occupancy is checked
                    // before the instance is planned, and is answered rather than left to a
                    // distance measured from nothing.
                    : Array.Empty<AttackTarget>();
            }

            return legal;
        }

        /// <summary>
        /// Milestone 8.75d. The same clause, stated in the vocabulary a card-text target SELECTION can
        /// read - see <see cref="GuardsDigital.Core.Rules.EffectResolution.RepeatedAttackSubjectNarrowing"/>.
        ///
        /// <para><b>One derivation, two readers.</b> The kind comes from
        /// <see cref="RepeatAdmitsOnly"/>, which is the method p.13's own target question uses, so the
        /// offer a printed subject makes and the set that question would accept cannot come to
        /// disagree. The exclusions come off the resolution the instance is already carrying.</para>
        ///
        /// <para><b>What it deliberately leaves out is the REACH narrowing</b>
        /// (<see cref="GuardsDigital.Core.Effects.RepeatTargetBinding.DifferentUnitAdjacentToActor"/>).
        /// Measured over all twenty-two heroes' composed content at this milestone: the only action
        /// definitions that both bind their target by card text and declare a repeat clause are
        /// Ignatia's Loosely-Aimed Firebolts and Imminent Eruption, and neither declares that member -
        /// Brynn's Split Attack, which does, binds its target through p.13's own question. A card-text
        /// attack declaring it would be a new shape, and
        /// <c>RepeatedSubjectNarrowingReachesEveryCardTextRepeatTests</c> is the guard that makes one
        /// arrive as a red test rather than as a silently unnarrowed offer.</para>
        /// </summary>
        internal static GuardsDigital.Core.Rules.EffectResolution.RepeatedAttackSubjectNarrowing
            RepeatedSubjectNarrowingOf(
                PendingAttackResolution resolution, IReadOnlyList<CardDefinition> definitions)
        {
            if (resolution == null || definitions == null ||
                resolution.InvocationContext != ActionInvocationContext.Repeated)
            {
                return GuardsDigital.Core.Rules.EffectResolution.RepeatedAttackSubjectNarrowing.None;
            }

            var admitted = RepeatAdmitsOnly(resolution, definitions);
            return new GuardsDigital.Core.Rules.EffectResolution.RepeatedAttackSubjectNarrowing(
                admitted.HasValue
                    ? (admitted.Value == AttackTargetKind.Hero
                        ? BoardObjectKind.Hero
                        : BoardObjectKind.Minion)
                    : BoardObjectKind.None,
                resolution.ExcludedTargets.Select(AsBoardObject));
        }

        /// <summary>p.13's target as p.19's board object - the identity a selection offers.</summary>
        private static BoardObjectReference AsBoardObject(AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? BoardObjectReference.ForHero(target.HeroId)
                : target.Kind == AttackTargetKind.Minion
                    ? BoardObjectReference.ForMinion(target.MinionId)
                    : default;

        /// <summary>
        /// The one repeat clause a single-clause card declares, read off the action this instance is
        /// actually performing - both of p.14's card-text moments, because a face may print its repeat
        /// at either and the page does not care which one asked. A card that declares no repeat
        /// declares <see cref="RepeatTargetBinding.Unbound"/>, which narrows nothing.
        /// </summary>
        /// <remarks>
        /// <para><b>Milestone 8.75d widened this from "is it DifferentEnemyHero" to "which binding is
        /// it", and that closed a measured defect rather than tidying a name.</b> Its predecessor
        /// asked one yes/no question, because when milestone 8.24 wrote it
        /// <see cref="RepeatTargetBinding.DifferentEnemyHero"/> was the only member that narrowed a
        /// KIND. Milestone 8.35z46 then added <see cref="RepeatTargetBinding.DifferentEnemyMinion"/>
        /// for Silverarrow's Rain of Arrows - a card that prints a CHAIN, so it is answered by
        /// <see cref="CardTextRepeatChain.ProducingClause"/> and never reaches here - and the
        /// single-clause fallback was left asking about heroes alone. Ignatia the Mad's Imminent
        /// Eruption is the first face in the corpus to print "may repeat once on a minion" as its
        /// only clause, and it fell straight through: p.13's own target question narrowed the repeated
        /// instance to nothing at all, so "on a MINION" admitted heroes. Reading the declared binding
        /// answers both members from one place and leaves every other card in the corpus exactly where
        /// it was (measured: every other single-clause Attack declares
        /// <see cref="RepeatTargetBinding.DifferentTarget"/>,
        /// <see cref="RepeatTargetBinding.DifferentUnitAdjacentToActor"/> or
        /// <see cref="RepeatTargetBinding.DifferentEnemyHero"/>, and the first two narrow no kind
        /// here either way).</para>
        /// </remarks>
        private static RepeatTargetBinding SingleDeclaredRepeatBinding(
            PendingAttackResolution resolution, IReadOnlyList<CardDefinition> definitions)
        {
            var action = RepeatedActionDefinition(resolution, definitions);
            if (action == null)
            {
                return RepeatTargetBinding.Unbound;
            }

            var fromRoot = DeclaredRepeatBinding(action.EffectRoot);
            return fromRoot != RepeatTargetBinding.Unbound
                ? fromRoot
                : DeclaredRepeatBinding(action.SecondCardTextRoot);
        }

        /// <summary>
        /// Milestone 8.35z46. The single unit kind a repeated instance may take, or
        /// <c>null</c> where its card narrows nothing and p.19's ordinary set stands.
        ///
        /// <para><b>Why this is not <see cref="SingleDeclaredRepeatBinding"/> with a second arm.</b>
        /// That method asks the card, which is deliberate - the clause moves no snapshot field and a
        /// tampered payload cannot widen the set back - and it is exactly why it cannot tell two
        /// clauses on one card apart. Silverarrow's Rain of Arrows prints two, narrowed to different
        /// kinds, so which one produced THIS instance decides what it may take. That is answered by
        /// the instance's own position in the chain, read from the identity the chain minted - see
        /// <see cref="CardTextRepeatChain"/> - which is restorable state and moves no schema
        /// version.</para>
        ///
        /// <para>A card printing one clause never reaches the chain branch, so its narrowing is the
        /// one it had before this milestone, computed by the same method.</para>
        ///
        /// <para><b>One case falls through deliberately.</b> Milestone 8.23's after-action ability
        /// mints its instance's identity with a <c>after-action-repeat:</c> PREFIX rather than the
        /// chain's suffix, so a chain card repeated that way reports position nought and no producing
        /// clause. It then takes the same card-derived answer every single-clause card takes, which is
        /// the behaviour that path already had - p.10's "perform the entire action again" inheriting a
        /// card-text clause's narrowing is a question this milestone does not open, because changing
        /// it would move Xargatha's Cleave as well.</para>
        /// </summary>
        private static AttackTargetKind? RepeatAdmitsOnly(
            PendingAttackResolution resolution, IReadOnlyList<CardDefinition> definitions)
        {
            var action = RepeatedActionDefinition(resolution, definitions);
            var producing = CardTextRepeatChain.ProducingClause(action, resolution.ResolutionId);
            if (producing != null)
            {
                switch (producing.RepeatTargetBinding)
                {
                    case RepeatTargetBinding.DifferentEnemyHero:
                        return AttackTargetKind.Hero;
                    case RepeatTargetBinding.DifferentEnemyMinion:
                        return AttackTargetKind.Minion;
                    default:
                        return null;
                }
            }

            // Milestone 8.75d. The single-clause fallback reads WHICH binding the card declares
            // rather than asking whether it is the one member that existed when this fallback was
            // written; see SingleDeclaredRepeatBinding for the defect that closed.
            switch (SingleDeclaredRepeatBinding(resolution, definitions))
            {
                case RepeatTargetBinding.DifferentEnemyHero:
                    return AttackTargetKind.Hero;
                case RepeatTargetBinding.DifferentEnemyMinion:
                    return AttackTargetKind.Minion;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Milestone 8.69b. Whether the clause that produced this repeated instance narrowed it to
        /// p.19's ring around the attacker - Split Attack's "on a different unit adjacent to you".
        ///
        /// <para>It is <see cref="RepeatAdmitsOnly"/>'s twin and asks the definition the same two ways
        /// for the same two reasons: a card printing a CHAIN of clauses is answered by the instance's
        /// own position in it, and a card printing one is answered by the card. What it narrows is the
        /// reach rather than the unit kind, so the two compose - a face declaring both would get both,
        /// and no face in the corpus declares either twice.</para>
        /// </summary>
        private static bool RepeatReachesOnlyUnitsAdjacentToTheActor(
            PendingAttackResolution resolution, IReadOnlyList<CardDefinition> definitions)
        {
            var action = RepeatedActionDefinition(resolution, definitions);
            var producing = CardTextRepeatChain.ProducingClause(action, resolution.ResolutionId);
            if (producing != null)
            {
                return producing.RepeatTargetBinding ==
                    RepeatTargetBinding.DifferentUnitAdjacentToActor;
            }

            return action != null &&
                (DeclaresAdjacentUnitRepeat(action.EffectRoot) ||
                    DeclaresAdjacentUnitRepeat(action.SecondCardTextRoot));
        }

        private static bool DeclaresAdjacentUnitRepeat(EffectDefinition effect) =>
            effect != null &&
            effect.Nodes.Any(node =>
                node.Kind == EffectNodeKind.RepeatAction &&
                node.RepeatTargetBinding == RepeatTargetBinding.DifferentUnitAdjacentToActor);

        private static ActionDefinition RepeatedActionDefinition(
            PendingAttackResolution resolution, IReadOnlyList<CardDefinition> definitions)
        {
            var card = definitions?.FirstOrDefault(candidate =>
                candidate.Id.Equals(resolution.SourceDefinitionReference.DefinitionId));
            return card?.ActionDefinitions?.FirstOrDefault(candidate =>
                candidate.Id.Equals(resolution.ActionDefinitionId));
        }

        /// <summary>
        /// The binding the first <see cref="EffectNodeKind.RepeatAction"/> node of one body declares,
        /// or <see cref="RepeatTargetBinding.Unbound"/> where the body prints no repeat at all. The
        /// definition's own node order is the printed order, which is the assumption
        /// <see cref="CardTextRepeatChain.ClausesOf"/> and <c>RepeatedActionQuery.Requested</c> both
        /// already make.
        /// </summary>
        private static RepeatTargetBinding DeclaredRepeatBinding(EffectDefinition effect)
        {
            if (effect == null)
            {
                return RepeatTargetBinding.Unbound;
            }

            foreach (var node in effect.Nodes)
            {
                if (node.Kind == EffectNodeKind.RepeatAction &&
                    node.RepeatTargetBinding != RepeatTargetBinding.Unbound)
                {
                    return node.RepeatTargetBinding;
                }
            }

            return RepeatTargetBinding.Unbound;
        }

        /// <summary>
        /// The attacker's Attack value: the printed icon with rulebook p.16's item bonuses composed
        /// onto it (milestone 7.2a).
        ///
        /// It is one helper rather than the composition repeated at each site because the sites are not
        /// alternatives - one computes the value the resolution stores, and the others re-derive it to
        /// check that the stored one still matches authoritative state. A composition present in some
        /// of them and absent in others would refuse a continuation for having the total the page
        /// requires.
        ///
        /// The bonus is asked of the printed result, exactly as the Defense total asks it, so p.16's
        /// "if an Action card has no matching icon, the item has no effect" stays a question about what
        /// the card prints.
        /// </summary>
        private ValueResult CalculateAttackValue(
            ActionOffer offer,
            AttackTarget target,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            // Milestone 8.39g2. Threaded from the caller's own resolution rather than read off the
            // backend's field, because three of the four callers are RE-DERIVATION sites verifying a
            // stored total and each must ask the question against the resolution it is verifying.
            HexCoordinate? actorSpaceAtActionStart = null)
        {
            var composed = AttackValueQuery.Calculate(offer, definitions, heroItems, modifications);
            var action = ActionOfferQuery.FindActionDefinition(offer, definitions);
            var current = FindCurrentTurnSlot(offer);
            var roster = RosterDynamicValueQuery.Contributions(
                action,
                offer,
                target,
                PrintedValueChannel.Attack,
                definitions,
                instances,
                participants,
                adjacentMoveAuthority,
                modifications,
                rosterState,
                current?.RoundIndex ?? 0,
                current?.TurnIndex ?? 0,
                actorSpaceAtActionStart,
                // Milestone 8.69a. The same authority, handed on as p.18's classification port, so a
                // declaration reading the obstacles around the bound target asks the one owner of that
                // question rather than re-deriving it from occupancy.
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                markers);
            // Milestone 8.19c moved the printed/item/source composition into AttackValueQuery, so what
            // is left here is p.14's card-text contribution alone - the half that needs the bound
            // target and this turn's own history. The composition still happens once, at every one of
            // this method's callers, so the value the resolution stores and the values its
            // re-derivations check remain one total.
            return roster.Count == 0
                ? composed
                : new ValueResult(
                    PrintedValueChannel.Attack,
                    composed.Contributions.Concat(roster));
        }

        /// <summary>
        /// The attacker's Range value, composed the same way (milestone 7.2a). Milestone 8.6 moved the
        /// composition itself into <see cref="RangeValueQuery"/> when Wasp's telekinetic Skills became
        /// a second reader of the same printed icon; this stays as the local name the resolution's
        /// three re-derivation sites call.
        /// </summary>
        private ValueResult RangeValue(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications,
            // Milestone 9.30e. The cards a card-counting Range reads (Sunburst's other spells removed).
            IEnumerable<CardInstance> cardInstances)
        {
            // Milestone 8.70. The derivation moved to `AttackSequenceBulletSubjectQuery` when a second
            // caller appeared - a modal Attack's bullet asking "in range" at the modal choice, one
            // whole action before its own sequence asks it - and this method delegates rather than
            // keeping a copy, so the two moments cannot come to disagree about one Range Total.
            return AttackSequenceBulletSubjectQuery.RangeValue(
                offer,
                definitions,
                participants,
                adjacentMoveAuthority,
                heroItems,
                rosterState,
                modifications,
                cardInstances);
        }

        private RoundTurnSlot FindCurrentTurnSlot(ActionOffer offer)
        {
            if (!rosterState.IsAvailable)
            {
                return null;
            }

            return rosterState.TurnSlots
                .Where(slot => slot != null && slot.Assignments.Any(assignment =>
                    assignment.HeroId.Equals(offer.ActorId) &&
                    assignment.CardInstanceId.HasValue &&
                    assignment.CardInstanceId.Value.Equals(offer.SourceCardInstanceId)))
                .OrderByDescending(slot => slot.RoundIndex)
                .ThenByDescending(slot => slot.TurnIndex)
                .FirstOrDefault();
        }

        /// <summary>
        /// One printed channel of this attack with p.16's items composed onto it - the local name the
        /// attack's own re-derivations reach for a channel that has no promoted query of its own.
        ///
        /// <para>Milestone 8.61h makes <paramref name="modifications"/> a required argument rather than
        /// a convenience: the page's "+1 per matching item" is what Min's "This round: Double your item
        /// bonuses" changes, and a composition here that could not see the live set would answer this
        /// attack's total differently from the five promoted queries beside it.</para>
        /// </summary>
        private ValueResult ComposeItems(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            PrintedValueChannel channel,
            RuleModificationSet modifications)
        {
            var printed = PrintedValueQuery.CalculatePrintedValue(offer, definitions, channel);
            return heroItems == null
                ? printed
                : ItemBonusQuery.Apply(
                    printed, heroItems, offer.ActorId, modifications ?? RuleModificationSet.Empty);
        }

        /// <summary>
        /// Milestone 7.8a1: whether this Attack has anything it could legally target right now.
        ///
        /// It asks <see cref="FindLegalAttackTargets"/>, which is the method
        /// <see cref="StartAttackResolution"/> builds its candidates with, so the offer set and the
        /// start cannot disagree about the same position. Nothing is stored and no pending state is
        /// touched: this is the same set, counted instead of kept.
        ///
        /// An offer that is not an Attack answers <c>true</c>, exactly as
        /// <c>SkillCardActionStrategyBackend.CanApplyAnything</c> answers <c>true</c> for anything that
        /// is not a Skill. So does an Attack whose action definition this build cannot resolve, or whose
        /// Range kind the start would refuse for its own reason: this judgement refuses an Attack proven
        /// to have no target, and never one that could not be judged.
        /// </summary>
        public bool CanFindLegalTarget(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications,
            // Milestone 9.9. A validation fork with the attacker standing where a before-action step
            // could leave him, or null for the live board.
            IBoardDisplacementAuthority onBoard = null,
            // Milestone 9.30e. The cards, so a Range its own text counts from them (Sunburst) is read
            // at the offer exactly as the Attack's start reads it.
            IEnumerable<CardInstance> instances = null)
        {
            var board = onBoard ?? adjacentMoveAuthority;
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (!ActionOfferQuery.TryFindActionDefinition(offer, materializedDefinitions, out var action) ||
                action.Kind != ActionKind.Attack ||
                (offer.AttackRange != AttackRangeKind.NonRanged &&
                    offer.AttackRange != AttackRangeKind.Ranged) ||
                !offer.ActorId.IsDefined ||
                // Milestone 8.19a. An Attack whose text names its target has no target set at the
                // moment this narrow target-set query is asked, and the set it would have had is the
                // wrong one - the text moves the attacker first. The full card-action authority probes
                // that typed continuation separately; this backend therefore leaves it undecided here.
                (action.AttackTargetPolicy?.TargetIsBoundByCardText ?? false) ||
                !board.Occupancies.Any(occupancy =>
                    occupancy.HeroId.Equals(offer.ActorId)))
            {
                return true;
            }

            return FindLegalAttackTargets(
                offer.ActorId, offer, materializedDefinitions, participants, modifications,
                cardInstances: instances, board: board).Count > 0;
        }

        /// <summary>
        /// Milestone 8.35z27. The bullet this resolution targets by, or an undefined one where the
        /// action's own printed policy is the answer - which is every Attack in this build outside a
        /// modal one.
        ///
        /// <para>Milestone 8.35z45 widened it from the bullet's POLICY to the whole bullet.
        /// The two travelled together already; what forced the change is that a bullet now carries a
        /// second printed reading - the predicate it states about its candidate - and handing the target
        /// derivation half a bullet would have been a place for the sentence and its qualifier to come
        /// apart.</para>
        ///
        /// <para>It is asked of the chain rather than carried on the resolution because the chain is
        /// what a snapshot persists for exactly this reason: the sentence belongs to the bullet, and
        /// the bullet is what the modal chose. A copy on the resolution would be a second place the
        /// same fact could be wrong, which is the rule every other printed reading here follows.</para>
        /// </summary>
        private AttackSequenceBullet SequenceBulletFor(PendingAttackResolution resolution)
        {
            if (resolution == null || pendingSequenceChain == null ||
                !pendingSequenceChain.Owns(resolution.ResolutionId))
            {
                return default;
            }

            return pendingSequenceChain.RunningBullet;
        }

        /// <summary>
        /// Milestone 8.35z27. Whether any bullet of a modal Attack has a unit its own
        /// printed sentence admits, on the board as it stands right now.
        ///
        /// <para>It is the offer-feasibility half of the contract, and it is asked of the same query
        /// that will build each sequence's target set - which is milestone 8.35z15's rule restated:
        /// judge the offer against what the answer would actually be able to perform. An Attack whose
        /// every bullet reaches nobody is greyed exactly as an ordinary Attack with no target is,
        /// rather than being offered, taken, and spent on nothing.</para>
        ///
        /// <para>What it deliberately does not ask is whether the SECOND sequence will find anything.
        /// That board does not exist yet - the first sequence has not run - and refusing a mode on a
        /// reading of a board nobody has reached is milestone 8.35p's measured mistake inverted.</para>
        /// </summary>
        internal bool CanAnyDeclaredBulletFindATarget(
            ActionOffer offer,
            IReadOnlyList<AttackSequenceBullet> bullets,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IEnumerable<CardInstance> cardInstances)
        {
            if (bullets == null || bullets.Count == 0)
            {
                return false;
            }

            if (!offer.ActorId.IsDefined ||
                !adjacentMoveAuthority.Occupancies.Any(occupancy =>
                    occupancy.HeroId.Equals(offer.ActorId)))
            {
                return true;
            }

            // Milestone 8.35z45. The bullet, not the bullet's policy: an Attack whose only
            // reachable bullet prints a predicate no candidate satisfies has nothing it could legally
            // target, and is greyed rather than offered, taken and spent on nothing. That is milestone
            // 8.35z15's rule applied one level in - judge the offer against what the answer would
            // actually be able to perform.
            //
            // Milestone 8.70 made the per-bullet half of this a named question of its own, because the
            // modal choice now asks it too: "any" is exactly "each, and at least one", so the whole
            // primary and the individual bullet are one reading counted twice rather than two rules
            // that could drift apart.
            return bullets.Any(bullet => CanThisDeclaredBulletFindATarget(
                offer, bullet, definitions, participants, modifications, cardInstances));
        }

        /// <summary>
        /// Milestone 8.70. Whether ONE declared bullet of a modal Attack has a unit its own printed
        /// sentence admits, on the board as it stands right now - the question the modal choice asks of
        /// each bullet before it offers one, settling BR-Q19h at the user's direction ("I think we
        /// should gray it out if no legal targets").
        ///
        /// <para>It is the same derivation <see cref="CanAnyDeclaredBulletFindATarget"/> counts over
        /// and the same one the chosen bullet's own p.14 sequence runs, asked of
        /// <see cref="AttackSequenceBulletSubjectQuery"/>, which owns it. The capability handler that
        /// stands on the bullet's node while the modal is being offered asks that owner directly rather
        /// than reaching this backend, because the offer walk has no authority in scope - what makes
        /// the two agree is that neither of them holds a derivation of its own.</para>
        /// </summary>
        internal bool CanThisDeclaredBulletFindATarget(
            ActionOffer offer,
            AttackSequenceBullet bullet,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IEnumerable<CardInstance> cardInstances) =>
            AttackSequenceBulletSubjectQuery.HasALegalSubject(
                offer.ActorId,
                offer,
                bullet,
                definitions,
                cardInstances,
                participants,
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                markers,
                heroItems,
                rosterState,
                modifications);

        private IReadOnlyList<AttackTarget> FindLegalAttackTargets(
            HeroId attackerId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications,
            AttackSequenceBullet sequenceBullet = default,
            IEnumerable<CardInstance> cardInstances = null,
            IBoardDisplacementAuthority board = null)
        {
            // Milestone 8.35z27. A modal Attack's bullets each print their own targeting
            // sentence - Middlefinger of Death's are "a unit adjacent to you" and "a hero in range
            // who has one or more cards in the discard" - so the sequence running this bullet reaches
            // what that bullet says and not what the action's own icons would say. Everything else
            // about the set is unchanged: the same Range Total, the same p.18 immunities, the same
            // query. Only the printed sentence differs, which is exactly the division milestone 8.5
            // drew between the classification and the card's own words.
            //
            // Milestone 8.70 moved that derivation to `AttackSequenceBulletSubjectQuery`, unchanged,
            // when the modal choice became a second caller: the bullet the sequence runs and the
            // bullet the choice offers are now proved legal by one method rather than by two copies
            // of it. The reach and the Range Total below are that query's as well, so nothing about
            // the number this branch would have used moves - it is simply asked one call further in.
            if (sequenceBullet.IsDefined)
            {
                return AttackSequenceBulletSubjectQuery.FindLegalSubjects(
                    attackerId,
                    offer,
                    sequenceBullet,
                    definitions,
                    cardInstances,
                    participants,
                    adjacentMoveAuthority,
                    adjacentMoveAuthority,
                    markers,
                    heroItems,
                    rosterState,
                    modifications);
            }

            // Milestone 7.2a. A Ranged Attack's Range Total decides which enemies it may target, so a
            // +1 Range item reaches one space further - the page's own example of an item that matters
            // only to a card printing that icon. A non-Ranged Attack has no Range icon and is adjacency
            // by rule rather than by a Range of one, so nothing is composed onto it.
            //
            // Milestone 8.57e. The classification the RULES read, which is the card's own unless a
            // standing declaration says otherwise - Rowenna's Fabled Lance, "All your attack actions
            // gain the "Ranged" subtype". It is asked here rather than off the offer because the offer
            // is a card-and-action address whose equality every authorization path compares; what
            // changed is what p.13 makes of that action right now, and this is where p.13's reach is
            // decided. The same query answers the defence preview and the Block, so the three cannot
            // disagree about one attack's subtype.
            var attackRange = RangedAttackOverlayQuery.Classify(
                offer.AttackRange, offer.ActorId, modifications);
            var rangeTotal = attackRange == AttackRangeKind.Ranged
                ? RangeValue(offer, definitions, participants, modifications, cardInstances).Total
                : 1;

            // Milestone 8.5. The policy is the card's own, not the classification's: p.13 states what
            // an Attack may target as a rule about the card, and a card that narrows it - a hero-only
            // Attack - says so in its definition. A definition that narrows nothing supplies the page's
            // default, so every Attack in this build reaches exactly what it reached before.
            var action = ActionOfferQuery.FindActionDefinition(offer, definitions);
            return AttackTargetQuery.FindLegalEnemyTargets(
                attackerId,
                // Milestone 8.57e. The card's own sentence, then the standing declaration that
                // replaces its adjacency clause with p.13's ceiling. Both halves arrive at the one
                // query the offer, the target choice and the resolution all read, which is milestone
                // 8.10's rule applied to a rule that is not printed on the attacking card.
                RangedAttackOverlayQuery.Overlay(
                    action.AttackTargetPolicy ??
                        PrintedAttackTargetPolicy.ForClassification(offer.AttackRange),
                    offer.AttackRange,
                    offer.ActorId,
                    modifications),
                rangeTotal,
                participants,
                board ?? adjacentMoveAuthority,
                modifications,
                // Milestone 9.30f. The attacking card's own classification, which an immunity scoped
                // to basic or non-basic attacks reads (a cast spell's is the spell's, GY-Q3).
                AttackTargetQuery.AttackClassificationOf(offer, definitions));
        }

        private static ActionOffer? FindPendingAttackOffer(
            PendingAttackResolution resolution,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            var sourceCard = instances.SingleOrDefault(candidate =>
                candidate.Id.Equals(resolution.SourceCardInstanceId));
            // Milestone 8.36f3. A borrowed primary was admitted by the enclosing authority before
            // this backend created the pending resolution. It cannot be rediscovered through either
            // ordinary offer window: the borrowed card remains in its owner's discard, by ruling 36.
            // Rebuild that already-authorized continuation from the exact identity held on the
            // resolution, while still requiring the source instance, its definition, and its printed
            // primary to agree. No new TextGranted action can enter through here; this method only
            // continues or restores a pending Attack that StartAttackResolution already accepted.
            if (resolution.InvocationContext == ActionInvocationContext.TextGranted &&
                sourceCard != null &&
                sourceCard.DefinitionReference.Equals(resolution.SourceDefinitionReference))
            {
                var definition = definitions.SingleOrDefault(candidate =>
                    candidate.Reference.Equals(resolution.SourceDefinitionReference));
                var action = definition?.ActionDefinitions.SingleOrDefault(candidate =>
                    candidate.Id.Equals(resolution.ActionDefinitionId));
                if (action != null && action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.Kind == ActionKind.Attack)
                {
                    return new ActionOffer(
                        sourceCard.Id,
                        definition.Reference,
                        action.Id,
                        resolution.ActorId,
                        ActionInvocationContext.TextGranted,
                        true,
                        action.AttackRange,
                        action.SourceCardStateTransition);
                }

                return null;
            }

            var offers = sourceCard != null &&
                sourceCard.State.Location == CardLocation.TurnArea &&
                sourceCard.State.HasBeenPlayed &&
                sourceCard.State.ResolutionStatus == CardResolutionStatus.Unresolved &&
                sourceCard.State.FaceOrientation == CardFaceOrientation.FaceUp
                    ? ActionOfferQuery.GetAvailableTurnActionOffers(
                        definitions,
                        instances,
                        resolution.ActorId,
                        resolution.SourceCardInstanceId)
                    : ActionOfferQuery.GetAvailableOffers(
                        definitions,
                        instances,
                        resolution.ActorId,
                        ActionOfferWindow.NormalPlay);
            return offers
                .Cast<ActionOffer?>()
                .SingleOrDefault(candidate =>
                    candidate.Value.SourceCardInstanceId.Equals(resolution.SourceCardInstanceId) &&
                    candidate.Value.SourceDefinitionReference.Equals(resolution.SourceDefinitionReference) &&
                    candidate.Value.ActionDefinitionId.Equals(resolution.ActionDefinitionId) &&
                    // Milestone 8.25a. A repeated instance is offered under the context its card
                    // prints, because the card prints one action and the repeat is that action again.
                    // The action definition is what identifies it here; the context on the resolution
                    // says which instance this is, which is a fact about the resolution rather than
                    // about the offer, so it is not what the offer is looked up by.
                    (resolution.InvocationContext == ActionInvocationContext.Repeated ||
                        candidate.Value.InvocationContext == resolution.InvocationContext));
        }

        private static bool AttackTargetChoicesMatch(
            PendingAttackTargetChoice expected,
            PendingAttackTargetChoice actual)
        {
            return expected.ResolutionId.Equals(actual.ResolutionId) &&
                expected.ChoiceId.Equals(actual.ChoiceId) &&
                expected.ChooserId.Equals(actual.ChooserId) &&
                expected.LegalTargets.SequenceEqual(actual.LegalTargets);
        }

        /// <summary>
        /// The Defense cards p.14 lets this defender answer with, derived in one place.
        ///
        /// Three sites asked this question with the same four lines - the live opening, the
        /// re-derivation that validates an answer, and the restore that cross-checks a saved choice -
        /// and milestone 8.7 needed all three to agree about one more thing, so they became one call.
        /// That the restore path passes the same modifications the live one did is what keeps a
        /// restriction from changing a defender's legal answers across a save.
        ///
        /// The restriction itself is p.14's own reading: a hero barred from Defense is not offered
        /// Defense cards, which is the treatment every other action kind gets from the offer set.
        /// Declining stays available regardless, because p.14 already lets a defender decline - so a
        /// restriction narrows what may be played and never strands the attack.
        /// </summary>
        /// <summary>
        /// Milestone 8.11 moved this rule to <see cref="DefenseOfferQuery"/>, which both test tiers can
        /// name and which now also asks milestone 8.4.s honesty question of a Defense.
        /// </summary>
        private ActionOffer[] FindDefenseOffers(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            HeroId defenderId,
            RuleModificationSet modifications,
            ActionOffer incomingAttack,
            MatchParticipants participants) =>
            DefenseOfferQuery.Offers(
                definitions,
                instances,
                defenderId,
                modifications,
                ActionOfferQuery.FindActionDefinition(incomingAttack, definitions),
                incomingAttack.ActorId, participants, adjacentMoveAuthority).ToArray();

        private static bool DefenseChoicesMatch(
            PendingAttackResolution resolution,
            IReadOnlyList<ActionOffer> expectedOffers,
            PendingDefenseResponseChoice actual)
        {
            return actual.ResolutionId.Equals(resolution.ResolutionId) &&
                actual.ChoiceId.Equals(resolution.CurrentStepId) &&
                actual.ChooserId.Equals(resolution.TargetHeroId) &&
                actual.AttackerId.Equals(resolution.ActorId) &&
                // Milestone 8.80. A restored choice states the same attacking card as the frame it
                // is being restored beside, or it is not this attack's question.
                actual.SourceCardInstanceId.Equals(resolution.SourceCardInstanceId) &&
                actual.SourceDefinitionReference.Equals(resolution.SourceDefinitionReference) &&
                actual.ActionDefinitionId.Equals(resolution.ActionDefinitionId) &&
                actual.AttackTotal == resolution.AttackTotal &&
                actual.CanDecline &&
                actual.LegalDefenseOffers.SequenceEqual(expectedOffers);
        }

        private static bool PrintedValuesMatch(ValueResult expected, ValueResult actual)
        {
            if (expected == null || actual == null ||
                expected.Channel != actual.Channel ||
                expected.Total != actual.Total ||
                expected.Contributions.Count != actual.Contributions.Count)
            {
                return false;
            }

            for (var index = 0; index < expected.Contributions.Count; index++)
            {
                if (!expected.Contributions[index].Equals(actual.Contributions[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompletedDefenseMatches(
            PendingAttackResolution resolution,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
        {
            if (!resolution.DefenseOffer.HasValue)
            {
                return resolution.DefenseValue == null;
            }

            var offer = resolution.DefenseOffer.Value;
            var matchingInstances = instances
                .Where(candidate => candidate != null && candidate.Id.Equals(offer.SourceCardInstanceId))
                .ToArray();
            if (matchingInstances.Length != 1)
            {
                return false;
            }

            var instance = matchingInstances[0];
            var action = ActionOfferQuery.FindActionDefinition(offer, definitions);
            var isBlock = action.DefenseResolution == PrintedDefenseResolution.Block;
            if (!instance.OwnerId.Equals(resolution.TargetHeroId) ||
                !instance.DefinitionReference.Equals(offer.SourceDefinitionReference) ||
                instance.State.Location != CardLocation.DiscardPile ||
                action.Kind != ActionKind.Defense ||
                // Milestone 8.11. p.14 prints a Block as the card's own primary and performs it only in
                // reaction, so the origin a Block carries is the one the face prints and the origin
                // every other Defense carries is the reaction it is printed as. Both are Defense
                // actions discarded from hand, which is what the rest of this check holds.
                //
                // Milestone 8.20a. The pair became a trio: p.14's Primary Defense Value is printed in
                // the same primary slot as a Block and settles by the page's numeric comparison like a
                // secondary, so keying the origin on the Block resolution refused Arien's three Duelist
                // cards here after accepting them everywhere earlier - the defender's own click was
                // taken and the Attack could then not resume. What the origin actually records is which
                // slot the face prints the Defense in, and p.14 has exactly two.
                //
                // It asks the offer query's own predicate rather than restating it, because this is
                // the site that failed to follow that predicate twice: milestone 8.11 taught it about
                // the Block by hand and 8.20 widened the predicate without it. One statement, four
                // askers, is the shape this codebase keeps a shared judgement in.
                !(ActionOfferQuery.IsPrintedPrimaryDefense(action) ||
                    action.Origin == ActionDefinitionOrigin.Reaction) ||
                !offer.SourceCardStateTransition.Equals(action.SourceCardStateTransition) ||
                offer.SourceCardStateTransition.RequiredLocation != CardLocation.Hand ||
                offer.SourceCardStateTransition.DestinationLocation != CardLocation.DiscardPile)
            {
                return false;
            }

            if (isBlock)
            {
                // A Block prints no Defense value, so there is no total to re-derive and the recorded
                // resolution must carry none. What it must carry instead is the condition's answer,
                // decided when the card was revealed at the page's step 3.
                return resolution.DefenseValue == null &&
                    resolution.BlockResult != DefenseBlockResult.None;
            }

            var expectedDefenseValue = DefenseValueQuery.Calculate(
                offer,
                definitions,
                resolution.TargetHeroId,
                resolution.Target.HeroBody,
                participants,
                board,
                modifications,
                heroItems);
            return PrintedValuesMatch(resolution.DefenseValue, expectedDefenseValue);
        }
    }
}
