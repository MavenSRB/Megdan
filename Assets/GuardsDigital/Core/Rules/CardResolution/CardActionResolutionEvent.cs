using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The displaced-minion correction detail a correction fact carries (milestone 6.4a): which
    /// miniature, how it was corrected, and where from and to. A left-in-place outcome records the one
    /// coordinate twice, because nothing moved and the fact says so.
    /// </summary>
    public readonly struct DisplacedMinionCorrectionEvent : IEquatable<DisplacedMinionCorrectionEvent>
    {
        public DisplacedMinionCorrectionEvent(
            MinionId minionId,
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionKind kind,
            HexCoordinate origin,
            HexCoordinate destination)
        {
            MinionId = minionId;
            CorrectionKind = kind;
            Origin = origin;
            Destination = destination;
        }

        public MinionId MinionId { get; }
        public GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionKind CorrectionKind { get; }
        public HexCoordinate Origin { get; }
        public HexCoordinate Destination { get; }

        public bool Equals(DisplacedMinionCorrectionEvent other) =>
            MinionId.Equals(other.MinionId) && CorrectionKind == other.CorrectionKind &&
            Origin.Equals(other.Origin) && Destination.Equals(other.Destination);

        public override bool Equals(object obj) =>
            obj is DisplacedMinionCorrectionEvent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = MinionId.GetHashCode();
                hash = (hash * 397) ^ (int)CorrectionKind;
                hash = (hash * 397) ^ Origin.GetHashCode();
                return (hash * 397) ^ Destination.GetHashCode();
            }
        }
    }

    public readonly struct CardActionResolutionEvent : IEquatable<CardActionResolutionEvent>
    {
        private CardActionResolutionEvent(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionDefinitionId replacedActionDefinitionId,
            HeroId actorId,
            HeroId targetHeroId,
            ActionResolutionStepId stepId,
            int remainingAllowance,
            int attackTotal,
            CardInstanceId defenseCardInstanceId,
            int defenseTotal,
            AttackResolutionOutcome attackOutcome,
            AdjacentMoveEvent movementEvent,
            FastTravelEvent fastTravelEvent,
            MinionDefeatFactKind minionDefeatFactKind = MinionDefeatFactKind.None,
            MinionId targetMinionId = default,
            DefeatOutcomeId defeatOutcomeId = default,
            TokenInstanceId targetTokenInstanceId = default,
            TokenDefinitionReference targetTokenDefinitionReference = default,
            HexCoordinate targetTokenCoordinate = default,
            DisplacedMinionCorrectionEvent correctionEvent = default,
            MarkerInstanceId returnedMarkerInstanceId = default,
            MarkerDefinitionReference returnedMarkerDefinitionReference = default,
            HeroDefeatFactKind heroDefeatFactKind = HeroDefeatFactKind.None,
            CardInstanceId resolvedCardInstanceId = default,
            int lifeCountersSpent = 0,
            int remainingLifeCounters = 0,
            HeroId blockingHeroId = default,
            CardInstanceId blockingCardInstanceId = default)
        {
            if (kind == CardActionResolutionEventKind.None)
            {
                throw new ArgumentException("Card action resolution event requires a kind.", nameof(kind));
            }

            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a resolution ID.", nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (!actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires an action definition ID.", nameof(actionDefinitionId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires an actor ID.", nameof(actorId));
            }

            if (!stepId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a step ID.", nameof(stepId));
            }

            Kind = kind;
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ReplacedActionDefinitionId = replacedActionDefinitionId;
            ActorId = actorId;
            TargetHeroId = targetHeroId;
            StepId = stepId;
            RemainingAllowance = remainingAllowance;
            AttackTotal = attackTotal;
            DefenseCardInstanceId = defenseCardInstanceId;
            DefenseTotal = defenseTotal;
            AttackOutcome = attackOutcome;
            MovementEvent = movementEvent;
            FastTravelEvent = fastTravelEvent;
            MinionDefeatFactKind = minionDefeatFactKind;
            TargetMinionId = targetMinionId;
            DefeatOutcomeId = defeatOutcomeId;
            TargetTokenInstanceId = targetTokenInstanceId;
            TargetTokenDefinitionReference = targetTokenDefinitionReference;
            TargetTokenCoordinate = targetTokenCoordinate;
            CorrectionEvent = correctionEvent;
            ReturnedMarkerInstanceId = returnedMarkerInstanceId;
            ReturnedMarkerDefinitionReference = returnedMarkerDefinitionReference;
            HeroDefeatFactKind = heroDefeatFactKind;
            ResolvedCardInstanceId = resolvedCardInstanceId;
            LifeCountersSpent = lifeCountersSpent;
            RemainingLifeCounters = remainingLifeCounters;
            BlockingHeroId = blockingHeroId;
            BlockingCardInstanceId = blockingCardInstanceId;
        }

        public CardActionResolutionEventKind Kind { get; }

        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public ActionDefinitionId ReplacedActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public HeroId TargetHeroId { get; }

        public ActionResolutionStepId StepId { get; }

        public int RemainingAllowance { get; }

        public int AttackTotal { get; }

        public CardInstanceId DefenseCardInstanceId { get; }

        public int DefenseTotal { get; }

        public AttackResolutionOutcome AttackOutcome { get; }

        public AdjacentMoveEvent MovementEvent { get; }

        public FastTravelEvent FastTravelEvent { get; }
        public MinionDefeatFactKind MinionDefeatFactKind { get; }
        public MinionId TargetMinionId { get; }
        public DefeatOutcomeId DefeatOutcomeId { get; }
        public TokenInstanceId TargetTokenInstanceId { get; }
        public TokenDefinitionReference TargetTokenDefinitionReference { get; }
        public HexCoordinate TargetTokenCoordinate { get; }

        /// <summary>Milestone 6.12: the marker p.13's defeat return took back, or <c>default</c>.</summary>
        public MarkerInstanceId ReturnedMarkerInstanceId { get; }

        public MarkerDefinitionReference ReturnedMarkerDefinitionReference { get; }
        public DisplacedMinionCorrectionEvent CorrectionEvent { get; }

        /// <summary>Milestone 7.4: which line of p.13's hero-Defeat list this fact is.</summary>
        public HeroDefeatFactKind HeroDefeatFactKind { get; }

        /// <summary>
        /// Milestone 7.4: the defeated hero's unresolved card that p.13 resolved without an action,
        /// or <c>default</c> on every other fact.
        /// </summary>
        public CardInstanceId ResolvedCardInstanceId { get; }

        /// <summary>Milestone 7.4: the Life counters p.13 actually took, or zero.</summary>
        public int LifeCountersSpent { get; }

        /// <summary>Milestone 7.4: what that team has left afterwards, or zero.</summary>
        public int RemainingLifeCounters { get; }

        /// <summary>
        /// Milestone 8.35z14 (the blocker-identity channel). On a <see cref="CardActionResolutionEventKind.SkillEffectNodeStopped"/>
        /// or a <see cref="CardActionResolutionEventKind.CardTextApplied"/> fact whose own node stopped,
        /// the hero controlling the standing rule modification that refused the step - or the undefined
        /// hero where none could be named, exactly as milestone 8.35l already leaves an offer's own
        /// reason unstated rather than guessed.
        /// </summary>
        public HeroId BlockingHeroId { get; }

        /// <summary>
        /// Milestone 8.35z14. The card the blocking modification's source came off, beside
        /// <see cref="BlockingHeroId"/>.
        /// </summary>
        public CardInstanceId BlockingCardInstanceId { get; }

        public static CardActionResolutionEvent MovementActionStarted(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementActionStarted, resolution, default);
        }

        public static CardActionResolutionEvent MovementChoiceRequested(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementChoiceRequested, resolution, default);
        }

        public static CardActionResolutionEvent MovementStepCommitted(
            PendingCardActionResolution resolution,
            AdjacentMoveEvent movementEvent)
        {
            return FromResolution(CardActionResolutionEventKind.MovementStepCommitted, resolution, movementEvent);
        }

        public static CardActionResolutionEvent MovementActionCompleted(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementActionCompleted, resolution, default);
        }

        public static CardActionResolutionEvent FastTravelActionStarted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent) =>
            FromFastTravel(
                CardActionResolutionEventKind.FastTravelActionStarted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                fastTravelEvent,
                "fast-travel-start");

        public static CardActionResolutionEvent FastTravelCommitted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent) =>
            FromFastTravel(
                CardActionResolutionEventKind.FastTravelCommitted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                fastTravelEvent,
                "fast-travel-commit");

        public static CardActionResolutionEvent FastTravelActionCompleted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent) =>
            FromFastTravel(
                CardActionResolutionEventKind.FastTravelActionCompleted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                fastTravelEvent,
                "fast-travel-complete");

        public static CardActionResolutionEvent ClearActionStarted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId) =>
            FromClear(
                CardActionResolutionEventKind.ClearActionStarted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                null,
                "clear-start");

        public static CardActionResolutionEvent TokenCleared(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            PlacedToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return FromClear(
                CardActionResolutionEventKind.TokenCleared,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                token,
                $"clear-token-{token.Id.Value}");
        }

        public static CardActionResolutionEvent ClearActionCompleted(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId) =>
            FromClear(
                CardActionResolutionEventKind.ClearActionCompleted,
                resolutionId,
                offer,
                replacedActionDefinitionId,
                null,
                "clear-complete");

        public static CardActionResolutionEvent AttackTargetChoiceRequested(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTargetChoiceRequested, resolution);
        }

        public static CardActionResolutionEvent AttackStarted(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackStarted, resolution);
        }

        /// <summary>
        /// Milestone 8.19c1. A Defeat is standing at its removal stage while the hero controlling a
        /// prevention source decides. The fact names the miniature still on the board and the Defeat it
        /// belongs to, so a reader can place the pause without inferring it from what stopped arriving.
        /// </summary>
        public static CardActionResolutionEvent RemovalPreventionRequested(
            PendingAttackResolution resolution,
            PendingRemovalPreventionDecision decision)
        {
            if (resolution == null) throw new ArgumentNullException(nameof(resolution));
            if (decision == null) throw new ArgumentNullException(nameof(decision));

            return new CardActionResolutionEvent(
                CardActionResolutionEventKind.RemovalPreventionRequested,
                resolution.ResolutionId, resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId, default, resolution.ActorId, decision.DecidingHeroId,
                resolution.CurrentStepId, 0, resolution.AttackTotal, default, 0,
                AttackResolutionOutcome.None, default, default,
                MinionDefeatFactKind.RemovalAttempted, decision.SubjectMinionId, decision.Defeat.Id);
        }

        /// <summary>
        /// Milestone 8.19c1. The card one hero discarded to stop a removal. The discarded card rides
        /// the Defense-card slot, which is the event's one place for a card a hero paid with.
        /// </summary>
        public static CardActionResolutionEvent RemovalPreventionPaid(
            PendingAttackResolution resolution,
            PendingRemovalPreventionDecision decision,
            CardInstanceId paymentCardInstanceId)
        {
            if (resolution == null) throw new ArgumentNullException(nameof(resolution));
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (!paymentCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "A paid prevention names the card that paid.", nameof(paymentCardInstanceId));
            }

            return new CardActionResolutionEvent(
                CardActionResolutionEventKind.RemovalPreventionPaid,
                resolution.ResolutionId, resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId, default, resolution.ActorId, decision.DecidingHeroId,
                resolution.CurrentStepId, 0, resolution.AttackTotal, paymentCardInstanceId, 0,
                AttackResolutionOutcome.None, default, default,
                MinionDefeatFactKind.RemovalPrevented, decision.SubjectMinionId, decision.Defeat.Id);
        }

        public static CardActionResolutionEvent AttackTargetValidated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTargetValidated, resolution);
        }

        public static CardActionResolutionEvent AttackTotalCalculated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTotalCalculated, resolution);
        }

        public static CardActionResolutionEvent DefenseResponseChoiceRequested(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseResponseChoiceRequested, resolution);
        }

        public static CardActionResolutionEvent DefenseCardDiscarded(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseCardDiscarded, resolution);
        }

        public static CardActionResolutionEvent DefenseTotalCalculated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseTotalCalculated, resolution);
        }

        public static CardActionResolutionEvent DefenseDeclined(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseDeclined, resolution);
        }

        public static CardActionResolutionEvent DefenseResponseCompleted(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseResponseCompleted, resolution);
        }

        public static CardActionResolutionEvent AttackOutcomeDetermined(
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            if (outcome != AttackResolutionOutcome.SuccessfullyDefended &&
                outcome != AttackResolutionOutcome.AttackSuccessful)
            {
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Attack outcome must be final.");
            }

            var kind = outcome == AttackResolutionOutcome.SuccessfullyDefended
                ? CardActionResolutionEventKind.AttackSuccessfullyDefended
                : CardActionResolutionEventKind.AttackSuccessful;
            return FromAttackResolution(kind, resolution, outcome);
        }

        public static CardActionResolutionEvent AttackResolutionCompleted(
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            return FromAttackResolution(
                CardActionResolutionEventKind.AttackResolutionCompleted,
                resolution,
                outcome);
        }

        /// <summary>
        /// Milestone 6.12. Rulebook p.13, "When a Hero is Defeated": "Any markers they had are
        /// returned." One event per attachment that ended, naming the marker and the holder it left, so
        /// the return is as readable after the fact as the Defeat that caused it.
        ///
        /// It reports only the marker half of that line. The same page's effect cancellation, the hero's
        /// removal, the Life counters, and the rewards remain their own later milestones' work, and this
        /// event deliberately says nothing about them.
        /// </summary>
        public static CardActionResolutionEvent MarkerReturnedOnHeroDefeat(
            PendingAttackResolution resolution,
            HeroId defeatedHeroId,
            MarkerAttachment attachment) =>
            MarkerReturnedOnHeroDefeat(
                EffectInvokedDefeatSource.FromAttack(
                    resolution ?? throw new ArgumentNullException(nameof(resolution))),
                defeatedHeroId,
                attachment);

        /// <summary>
        /// Milestone 8.22. The same return, reported about whichever action asked for the Defeat -
        /// which is a Skill for Sabina's Covering Fire and an Attack for every earlier printing.
        /// </summary>
        public static CardActionResolutionEvent MarkerReturnedOnHeroDefeat(
            EffectInvokedDefeatSource resolution,
            HeroId defeatedHeroId,
            MarkerAttachment attachment)
        {
            if (!resolution.IsDefined)
            {
                throw new ArgumentException(
                    "A p.13 defeat return names the action it is being run about.", nameof(resolution));
            }

            if (attachment == null) throw new ArgumentNullException(nameof(attachment));
            if (!defeatedHeroId.IsDefined ||
                !attachment.HolderHeroId.Equals(defeatedHeroId))
            {
                throw new ArgumentException(
                    "A p.13 defeat return reports a marker the defeated hero held.", nameof(attachment));
            }

            return new CardActionResolutionEvent(
                CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                defeatedHeroId,
                new ActionResolutionStepId($"defeat-return-marker-{attachment.Id.Value}"),
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                default,
                returnedMarkerInstanceId: attachment.Id,
                returnedMarkerDefinitionReference: attachment.Definition.Reference);
        }

        /// <summary>
        /// <paramref name="defeatedHeroId"/> was the Attack's own hero target until milestone 8.10, and
        /// is now stated because it is no longer always that hero: Electroblast's rider escalates into
        /// rulebook p.13's list against a hero the Attack did not target and may never have been able
        /// to. What the guard still holds is the pairing - the event and the resolution it reports have
        /// to be about the same hero - which is the whole of what reading it off the Attack was for.
        /// </summary>
        public static CardActionResolutionEvent HeroDefeatRecorded(
            PendingAttackResolution resolution,
            HeroId defeatedHeroId,
            HeroDefeatResolution defeat) =>
            HeroDefeatRecorded(
                EffectInvokedDefeatSource.FromAttack(
                    resolution ?? throw new ArgumentNullException(nameof(resolution))),
                defeatedHeroId,
                defeat);

        /// <summary>
        /// Milestone 8.22. The same record, about whichever action asked for the Defeat. An Attack
        /// carries its total, its Defense card and its outcome; a Skill carries their named absence,
        /// because no Attack happened and a zero would read as one that did.
        /// </summary>
        public static CardActionResolutionEvent HeroDefeatRecorded(
            EffectInvokedDefeatSource resolution,
            HeroId defeatedHeroId,
            HeroDefeatResolution defeat)
        {
            if (!resolution.IsDefined || defeat == null ||
                !defeatedHeroId.IsDefined ||
                !string.Equals(defeat.Defeat.TargetId.Value, defeatedHeroId.Value, StringComparison.Ordinal))
            {
                throw new ArgumentException("Hero Defeat event requires the defeated hero it reports.");
            }

            return new CardActionResolutionEvent(
                CardActionResolutionEventKind.HeroDefeatRecorded,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                defeatedHeroId,
                resolution.CurrentStepId,
                0,
                resolution.AttackTotal,
                resolution.DefenseCardInstanceId,
                resolution.DefenseTotal,
                resolution.Outcome,
                default,
                default,
                MinionDefeatFactKind.None,
                default,
                defeat.Defeat.Id);
        }

        public static CardActionResolutionEvent MinionDefeatFact(
            PendingAttackResolution resolution,
            MinionDefeatResolution defeat,
            MinionDefeatFactKind factKind)
        {
            if (resolution == null || defeat == null ||
                !defeat.Facts.Contains(factKind) || !resolution.TargetMinionId.IsDefined)
            {
                throw new ArgumentException("Minion Defeat event requires one recorded typed fact.");
            }

            CardActionResolutionEventKind eventKind;
            switch (factKind)
            {
                case MinionDefeatFactKind.DefeatRecorded: eventKind = CardActionResolutionEventKind.MinionDefeatRecorded; break;
                case MinionDefeatFactKind.RewardAwarded: eventKind = CardActionResolutionEventKind.MinionRewardAwarded; break;
                case MinionDefeatFactKind.RemovalAttempted: eventKind = CardActionResolutionEventKind.MinionRemovalAttempted; break;
                case MinionDefeatFactKind.RemovalCompleted: eventKind = CardActionResolutionEventKind.MinionRemovalCompleted; break;
                case MinionDefeatFactKind.RemovalPrevented: eventKind = CardActionResolutionEventKind.MinionRemovalPrevented; break;
                default: throw new ArgumentOutOfRangeException(nameof(factKind));
            }

            return new CardActionResolutionEvent(
                eventKind, resolution.ResolutionId, resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId, default, resolution.ActorId, default,
                resolution.CurrentStepId, 0, resolution.AttackTotal, default, 0,
                AttackResolutionOutcome.AttackSuccessful, default, default,
                factKind, resolution.TargetMinionId, defeat.Defeat.Id);
        }

        /// <summary>
        /// Milestone 7.4. One line of rulebook p.13's "When a Hero is Defeated" list, reported in the
        /// page's own order from the staged resolution that performed it.
        ///
        /// <see cref="HeroDefeatFactKind.DefeatRecorded"/> is deliberately not routed here: it has been
        /// <see cref="HeroDefeatRecorded"/> since milestone 5.8, carries the Attack totals that make the
        /// Defeat readable, and rewriting it would have changed an event the projections already read.
        /// </summary>
        public static CardActionResolutionEvent HeroDefeatFact(
            PendingAttackResolution resolution,
            HeroId defeatedHeroId,
            HeroDefeatResolution defeat,
            HeroDefeatFactKind factKind) =>
            HeroDefeatFact(
                EffectInvokedDefeatSource.FromAttack(
                    resolution ?? throw new ArgumentNullException(nameof(resolution))),
                defeatedHeroId,
                defeat,
                factKind);

        /// <summary>
        /// Milestone 8.22. The same line of the list, about whichever action asked for the Defeat.
        /// </summary>
        public static CardActionResolutionEvent HeroDefeatFact(
            EffectInvokedDefeatSource resolution,
            HeroId defeatedHeroId,
            HeroDefeatResolution defeat,
            HeroDefeatFactKind factKind)
        {
            if (!resolution.IsDefined || defeat == null ||
                !defeat.Facts.Contains(factKind) || !defeatedHeroId.IsDefined)
            {
                throw new ArgumentException("Hero Defeat event requires one recorded typed fact.");
            }

            CardActionResolutionEventKind eventKind;
            switch (factKind)
            {
                case HeroDefeatFactKind.RemovalAttempted: eventKind = CardActionResolutionEventKind.HeroRemovalAttempted; break;
                case HeroDefeatFactKind.RemovalCompleted: eventKind = CardActionResolutionEventKind.HeroRemovalCompleted; break;
                case HeroDefeatFactKind.RemovalPrevented: eventKind = CardActionResolutionEventKind.HeroRemovalPrevented; break;
                case HeroDefeatFactKind.UnresolvedCardResolved: eventKind = CardActionResolutionEventKind.HeroUnresolvedCardResolved; break;
                case HeroDefeatFactKind.RewardAwarded: eventKind = CardActionResolutionEventKind.HeroDefeatRewardAwarded; break;
                case HeroDefeatFactKind.LifeSpent: eventKind = CardActionResolutionEventKind.TeamLifeSpent; break;
                case HeroDefeatFactKind.TeamEliminated: eventKind = CardActionResolutionEventKind.TeamEliminated; break;
                default: throw new ArgumentOutOfRangeException(nameof(factKind));
            }

            var spent = factKind == HeroDefeatFactKind.LifeSpent || factKind == HeroDefeatFactKind.TeamEliminated
                ? defeat.LifeSpend.Spent
                : 0;
            var remaining = factKind == HeroDefeatFactKind.LifeSpent || factKind == HeroDefeatFactKind.TeamEliminated
                ? defeat.LifeSpend.CountersAfter
                : 0;
            return new CardActionResolutionEvent(
                eventKind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                defeatedHeroId,
                new ActionResolutionStepId($"hero-defeat-{FactStepSuffix(factKind)}"),
                0,
                resolution.AttackTotal,
                // The comparison that produced the Defeat, carried on every line of its list exactly as
                // HeroDefeatRecorded carries it, so a reader of any one fact can see what caused it.
                resolution.DefenseCardInstanceId,
                resolution.DefenseTotal,
                resolution.Outcome,
                default,
                default,
                MinionDefeatFactKind.None,
                default,
                defeat.Defeat.Id,
                default,
                default,
                default,
                default,
                default,
                default,
                factKind,
                factKind == HeroDefeatFactKind.UnresolvedCardResolved
                    ? defeat.ResolvedUnresolvedCardId
                    : default,
                spent,
                remaining);
        }

        private static string FactStepSuffix(HeroDefeatFactKind factKind)
        {
            switch (factKind)
            {
                case HeroDefeatFactKind.RemovalAttempted: return "removal-attempted";
                case HeroDefeatFactKind.RemovalCompleted: return "removal-completed";
                case HeroDefeatFactKind.RemovalPrevented: return "removal-prevented";
                case HeroDefeatFactKind.UnresolvedCardResolved: return "unresolved-card-resolved";
                case HeroDefeatFactKind.RewardAwarded: return "reward-awarded";
                case HeroDefeatFactKind.LifeSpent: return "life-spent";
                case HeroDefeatFactKind.TeamEliminated: return "team-eliminated";
                default: throw new ArgumentOutOfRangeException(nameof(factKind));
            }
        }

        public bool Equals(CardActionResolutionEvent other)
        {
            return Kind == other.Kind &&
                ResolutionId.Equals(other.ResolutionId) &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) &&
                ReplacedActionDefinitionId.Equals(other.ReplacedActionDefinitionId) &&
                ActorId.Equals(other.ActorId) &&
                TargetHeroId.Equals(other.TargetHeroId) &&
                StepId.Equals(other.StepId) &&
                RemainingAllowance == other.RemainingAllowance &&
                AttackTotal == other.AttackTotal &&
                DefenseCardInstanceId.Equals(other.DefenseCardInstanceId) &&
                DefenseTotal == other.DefenseTotal &&
                AttackOutcome == other.AttackOutcome &&
                MovementEvent.Equals(other.MovementEvent) &&
                FastTravelEvent.Equals(other.FastTravelEvent) &&
                MinionDefeatFactKind == other.MinionDefeatFactKind &&
                TargetMinionId.Equals(other.TargetMinionId) &&
                DefeatOutcomeId.Equals(other.DefeatOutcomeId) &&
                TargetTokenInstanceId.Equals(other.TargetTokenInstanceId) &&
                TargetTokenDefinitionReference.Equals(other.TargetTokenDefinitionReference) &&
                TargetTokenCoordinate.Equals(other.TargetTokenCoordinate) &&
                CorrectionEvent.Equals(other.CorrectionEvent) &&
                HeroDefeatFactKind == other.HeroDefeatFactKind &&
                ResolvedCardInstanceId.Equals(other.ResolvedCardInstanceId) &&
                LifeCountersSpent == other.LifeCountersSpent &&
                RemainingLifeCounters == other.RemainingLifeCounters &&
                BlockingHeroId.Equals(other.BlockingHeroId) &&
                BlockingCardInstanceId.Equals(other.BlockingCardInstanceId);
        }

        public override bool Equals(object obj)
        {
            return obj is CardActionResolutionEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ ResolutionId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ReplacedActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetHeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ StepId.GetHashCode();
                hashCode = (hashCode * 397) ^ RemainingAllowance;
                hashCode = (hashCode * 397) ^ AttackTotal;
                hashCode = (hashCode * 397) ^ DefenseCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ DefenseTotal;
                hashCode = (hashCode * 397) ^ (int)AttackOutcome;
                hashCode = (hashCode * 397) ^ MovementEvent.GetHashCode();
                hashCode = (hashCode * 397) ^ FastTravelEvent.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)MinionDefeatFactKind;
                hashCode = (hashCode * 397) ^ TargetMinionId.GetHashCode();
                hashCode = (hashCode * 397) ^ DefeatOutcomeId.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetTokenInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetTokenDefinitionReference.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetTokenCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrectionEvent.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)HeroDefeatFactKind;
                hashCode = (hashCode * 397) ^ ResolvedCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ LifeCountersSpent;
                hashCode = (hashCode * 397) ^ RemainingLifeCounters;
                hashCode = (hashCode * 397) ^ BlockingHeroId.GetHashCode();
                return (hashCode * 397) ^ BlockingCardInstanceId.GetHashCode();
            }
        }

        private static CardActionResolutionEvent FromResolution(
            CardActionResolutionEventKind kind,
            PendingCardActionResolution resolution,
            AdjacentMoveEvent movementEvent)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            return new CardActionResolutionEvent(
                kind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                default,
                resolution.CurrentStepId,
                resolution.RemainingAllowance,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                movementEvent,
                default);
        }

        /// <summary>
        /// Milestone 6.2a. Skill facts name the effect node they concern through the ordinary step
        /// identity, and say what happened through the event kind rather than by packing an outcome
        /// into a field that means something else.
        /// </summary>
        public static CardActionResolutionEvent SkillActionStarted(
            ActionResolutionId resolutionId,
            ActionOffer offer) =>
            SkillActionStarted(
                resolutionId, offer.SourceCardInstanceId, offer.ActionDefinitionId, offer.ActorId);

        /// <summary>
        /// The same fact, named by identity rather than by an offer. Milestone 6.7's repeated instance
        /// is created by a rule and never has an offer, and fabricating one that claimed to be
        /// available would have stated something untrue on the wire.
        /// </summary>
        public static CardActionResolutionEvent SkillActionStarted(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId) =>
            Skill(
                CardActionResolutionEventKind.SkillActionStarted,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId("skill-start"));

        public static CardActionResolutionEvent SkillEffectNode(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            string nodeId,
            HeroId blockingHeroId = default,
            CardInstanceId blockingCardInstanceId = default)
        {
            if (kind != CardActionResolutionEventKind.SkillEffectNodeApplied &&
                kind != CardActionResolutionEventKind.SkillEffectNodeDeclined &&
                kind != CardActionResolutionEventKind.SkillEffectNodeStopped)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Not a Skill effect-node fact kind.");
            }

            return Skill(
                kind,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId(nodeId),
                blockingHeroId,
                blockingCardInstanceId);
        }

        /// <summary>
        /// Milestone 8.5: one node of a printed Attack's or Defense's card text, applied at one of
        /// rulebook p.14's numbered moments.
        ///
        /// It reuses the Skill fact shape because what it reports is the same thing - a node of an
        /// effect definition resolved against a source card by an actor - and differs only in what
        /// surrounds it. The step id carries the moment and the node together, so the ordered log
        /// places the text inside p.14's order without a reader inferring anything from position.
        /// </summary>
        public static CardActionResolutionEvent CardTextApplied(
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            CardTextTiming timing,
            EffectResolutionEvent node) =>
            Skill(
                CardActionResolutionEventKind.CardTextApplied,
                new ActionResolutionId(
                    $"card-text-{sourceCardInstanceId.Value}-{actionDefinitionId.Value}"),
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId($"{timing}:{node.NodeId.Value}:{node.ResultKind}"),
                // Milestone 8.35z14. `node` is the same transient fact `EffectResolutionSession` already
                // attaches the blocking modification to, so this is read rather than re-derived.
                node.Blocking?.ControllerHeroId ?? default,
                node.Blocking?.SourceCardInstanceId ?? default);

        public static CardActionResolutionEvent SkillChoiceRequested(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            string choiceId) =>
            Skill(
                CardActionResolutionEventKind.SkillChoiceRequested,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId(choiceId));

        public static CardActionResolutionEvent SkillActionCompleted(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId) =>
            Skill(
                CardActionResolutionEventKind.SkillActionCompleted,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                new ActionResolutionStepId("skill-complete"));

        private static CardActionResolutionEvent Skill(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionResolutionStepId stepId,
            HeroId blockingHeroId = default,
            CardInstanceId blockingCardInstanceId = default) =>
            new CardActionResolutionEvent(
                kind,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                default,
                actorId,
                default,
                stepId,
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                default,
                blockingHeroId: blockingHeroId,
                blockingCardInstanceId: blockingCardInstanceId);

        /// <summary>
        /// Milestone 6.4a. Correction facts belong to the action whose boundary triggered them, so they
        /// carry that action's full identity; the step names the phase or the subject, and the typed
        /// correction detail rides its own field the way movement and Fast Travel details do.
        /// </summary>
        public static CardActionResolutionEvent DisplacedMinionCorrection(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            string stepId,
            DisplacedMinionCorrectionEvent correction = default)
        {
            if (kind != CardActionResolutionEventKind.DisplacedMinionCorrectionStarted &&
                kind != CardActionResolutionEventKind.DisplacedMinionCorrectionChoiceRequested &&
                kind != CardActionResolutionEventKind.DisplacedMinionCorrectionMoved &&
                kind != CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced &&
                kind != CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace &&
                kind != CardActionResolutionEventKind.DisplacedMinionCorrectionCompleted)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Not a displaced-minion correction fact kind.");
            }

            return new CardActionResolutionEvent(
                kind,
                resolutionId,
                sourceCardInstanceId,
                actionDefinitionId,
                default,
                actorId,
                default,
                new ActionResolutionStepId(stepId),
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                default,
                MinionDefeatFactKind.None,
                correction.MinionId,
                default,
                default,
                default,
                default,
                correction);
        }

        private static CardActionResolutionEvent FromAttackResolution(
            CardActionResolutionEventKind kind,
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome = AttackResolutionOutcome.None)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            return new CardActionResolutionEvent(
                kind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                default,
                resolution.ActorId,
                resolution.TargetHeroId,
                resolution.CurrentStepId,
                0,
                resolution.AttackTotal,
                resolution.DefenseOffer.HasValue
                    ? resolution.DefenseOffer.Value.SourceCardInstanceId
                    : default,
                resolution.DefenseTotal ?? 0,
                outcome,
                default,
                default,
                // An attack names ONE defender, and PendingAttackResolution.Target already holds
                // whichever it is: the hero arm rides TargetHeroId above, and this is the minion arm.
                // Omitting it left every minion attack - the commonest attack in a match - reaching
                // the rule-event log and the seat-public MatchLog with an attacker and no defender,
                // so RuleEventLogProjection wrote no `minion=` reference and any reader downstream
                // (MatchLogProjection's Optional("minion"), and through it the kinetic cues) could
                // only decline to name a target it was never told. Both arms are defaulted when the
                // resolution has no target yet, so a targetless step is unchanged.
                targetMinionId: resolution.TargetMinionId);
        }

        private static CardActionResolutionEvent FromFastTravel(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            FastTravelEvent fastTravelEvent,
            string stepId)
        {
            if (offer.InvocationContext != ActionInvocationContext.Replacement ||
                !replacedActionDefinitionId.IsDefined ||
                !fastTravelEvent.HeroId.Equals(offer.ActorId))
            {
                throw new ArgumentException("A Fast Travel event requires its replacement offer, replaced action, and actor move.");
            }

            return new CardActionResolutionEvent(
                kind,
                resolutionId,
                offer.SourceCardInstanceId,
                offer.ActionDefinitionId,
                replacedActionDefinitionId,
                offer.ActorId,
                default,
                new ActionResolutionStepId(stepId),
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                fastTravelEvent);
        }

        private static CardActionResolutionEvent FromClear(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            ActionOffer offer,
            ActionDefinitionId replacedActionDefinitionId,
            PlacedToken token,
            string stepId)
        {
            if (offer.InvocationContext != ActionInvocationContext.Replacement ||
                !offer.ActionDefinitionId.Equals(GeneralRuleActionOfferQuery.ClearDefinition.Id) ||
                !replacedActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("A Clear event requires its replacement offer and replaced Attack action.");
            }

            return new CardActionResolutionEvent(
                kind,
                resolutionId,
                offer.SourceCardInstanceId,
                offer.ActionDefinitionId,
                replacedActionDefinitionId,
                offer.ActorId,
                default,
                new ActionResolutionStepId(stepId),
                0,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                default,
                default,
                MinionDefeatFactKind.None,
                default,
                default,
                token?.Id ?? default,
                token?.Definition.Reference ?? default,
                token?.Coordinate ?? default);
        }
    }
}
