using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
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
        private HeroCoinLedger coinLedger;

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

        internal bool HasPendingResolution => pendingAttackResolution != null;

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
        internal HeroCoinLedger EnsureCoinLedger(MatchParticipants participants)
        {
            if (coinLedger == null)
            {
                coinLedger = new HeroCoinLedger(
                    participants ?? throw new ArgumentNullException(nameof(participants)));
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
            RuleModificationSet modifications)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableTurnActionOffers(
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
                modifications);
        }

        private AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            IReadOnlyList<CardInstance> materializedInstances,
            MatchParticipants participants,
            IReadOnlyList<ActionOffer> authoritativeOffers,
            RuleModificationSet modifications)
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

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary)
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
                null);
            pendingAttackTargetChoice = CreateAttackTargetChoice(
                pendingAttackResolution,
                offer,
                materializedDefinitions,
                participants,
                modifications);
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
                modifications);
            if (!AttackTargetChoicesMatch(authoritativeChoice, pendingAttackTargetChoice))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            if (!authoritativeChoice.LegalTargets.Contains(command.Target))
            {
                return RejectAttack(CardActionResolutionRejectionReason.TargetUnavailable);
            }

            if (command.Target.Kind == AttackTargetKind.Minion)
            {
                return ResolveMinionTarget(command.TargetMinionId, participants, modifications);
            }

            var attackValue = CalculateAttackValue(offer.Value, materializedDefinitions);
            var defenseResolution = new PendingAttackResolution(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.InvocationContext,
                new ActionResolutionStepId("defense-response"),
                AttackResolutionPhase.DefenseResponse,
                command.TargetHeroId,
                attackValue);
            var defenseOffers = ActionOfferQuery.GetAvailableOffers(
                materializedDefinitions,
                materializedInstances,
                command.TargetHeroId,
                ActionOfferWindow.Reaction)
                .Where(candidate => ActionOfferQuery.FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                .ToArray();
            var defenseChoice = new PendingDefenseResponseChoice(
                defenseResolution.ResolutionId,
                defenseResolution.CurrentStepId,
                command.TargetHeroId,
                defenseResolution.ActorId,
                defenseResolution.AttackTotal,
                defenseOffers,
                true);

            pendingAttackResolution = defenseResolution;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = defenseChoice;

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                null,
                pendingDefenseChoice,
                new[]
                {
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackTargetValidated(pendingAttackResolution)),
                    ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                    ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack),
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackStarted(pendingAttackResolution)),
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.AttackTotalCalculated(pendingAttackResolution)),
                    ResolutionStep.AtCheckpoint(
                        TimingCheckpoint.DefenseResponse, pendingAttackResolution.TargetHeroId),
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.DefenseResponseChoiceRequested(pendingAttackResolution))
                });
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
            RuleModificationSet modifications)
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
                null);
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
            var reward = coinLedger.Award(outcome);
            if (!reward.Accepted)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var removalAttempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
            var defeat = ResolveRemovalStage(
                targetMinionId, outcome, reward, removalAttempt, participants, modifications);
            var events = new List<ResolutionStep>
            {
                ResolutionStep.FromFact(CardActionResolutionEvent.AttackTargetValidated(resolution)),
                ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack),
                ResolutionStep.FromFact(CardActionResolutionEvent.AttackStarted(resolution))
            };
            events.AddRange(defeat.Facts.Select(fact => ResolutionStep.FromFact(
                CardActionResolutionEvent.MinionDefeatFact(resolution, defeat, fact))));
            events.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.AfterAttack));
            events.Add(ResolutionStep.FromFact(CardActionResolutionEvent.AttackResolutionCompleted(
                resolution, AttackResolutionOutcome.AttackSuccessful)));
            events.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded));

            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
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
            RuleModificationSet modifications)
        {
            var subject = BoardObjectReference.ForMinion(targetMinionId);
            var removalStage = new RemovalStageDecision(
                RuleActivity.Attack,
                RuleObject.ForHero(pendingAttackResolution.ActorId),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                participants,
                modifications,
                LanePushCauseKind.Defeat);

            var origin = adjacentMoveAuthority.Minions
                .Single(minion => minion.MinionId.Equals(targetMinionId)).Coordinate;
            if (removalStage.IsPrevented(subject))
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

            var legalTargets = FindLegalAttackTargets(
                pendingAttackResolution.ActorId,
                attackOffer.Value,
                materializedDefinitions,
                participants,
                modifications);
            var expectedAttackValue = CalculateAttackValue(attackOffer.Value, materializedDefinitions);
            if (!legalTargets.Contains(AttackTarget.Hero(pendingAttackResolution.TargetHeroId)) ||
                !PrintedValuesMatch(pendingAttackResolution.AttackValue, expectedAttackValue))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var currentDefenseOffers = ActionOfferQuery.GetAvailableOffers(
                    materializedDefinitions,
                    materializedInstances,
                    pendingAttackResolution.TargetHeroId,
                    ActionOfferWindow.Reaction)
                .Where(candidate => ActionOfferQuery.FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                .ToArray();
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
            if (!defenseCard.OwnerId.Equals(pendingAttackResolution.TargetHeroId) ||
                !defenseCard.DefinitionReference.Equals(defenseOffer.SourceDefinitionReference) ||
                defenseCard.State.Location != transition.RequiredLocation ||
                transition.RequiredLocation != CardLocation.Hand ||
                transition.DestinationLocation != CardLocation.DiscardPile)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var defenseValue = DefenseValueQuery.Calculate(
                defenseOffer,
                materializedDefinitions,
                pendingAttackResolution.TargetHeroId,
                participants,
                adjacentMoveAuthority,
                modifications,
                heroItems);
            var resumedWithDefense = CreateOutcomeComparisonResolution(defenseOffer, defenseValue);

            // Routed through the lifecycle authority rather than transitioning the card directly.
            // The guards above already pin this transition to Hand -> DiscardPile, which is exactly
            // what TryDiscardFromHand performs, so the discard semantics are unchanged - but
            // the authority additionally requires the card to be unplayed with no resolution status,
            // which the duplicated guard here never checked. See milestone 6.1f / finding F613-06.
            if (!CardLifecycleAuthority.TryDiscardFromHand(defenseCard))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            pendingAttackResolution = resumedWithDefense;
            pendingDefenseChoice = null;
            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                null,
                null,
                new[]
                {
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.DefenseCardDiscarded(pendingAttackResolution)),
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.DefenseTotalCalculated(pendingAttackResolution)),
                    ResolutionStep.FromFact(
                        CardActionResolutionEvent.DefenseResponseCompleted(pendingAttackResolution)),
                    ResolutionStep.AtCheckpoint(
                        TimingCheckpoint.AttackResultComparison, pendingAttackResolution.TargetHeroId)
                });
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

            var legalTargets = FindLegalAttackTargets(
                pendingAttackResolution.ActorId,
                attackOffer.Value,
                materializedDefinitions,
                participants,
                modifications);
            var expectedAttackValue = CalculateAttackValue(attackOffer.Value, materializedDefinitions);
            if (!pendingAttackResolution.CurrentStepId.Equals(
                    new ActionResolutionStepId("attack-outcome-comparison")) ||
                !legalTargets.Contains(AttackTarget.Hero(pendingAttackResolution.TargetHeroId)) ||
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
            var comparison = AttackOutcomeComparison.Create(
                completedResolution.AttackTotal,
                completedResolution.DefenseTotal,
                DefenseBlockResult.None);
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
                    completedResolution,
                    materializedDefinitions,
                    materializedInstances,
                    participants,
                    modifications,
                    events);
                if (heroDefeat == null)
                {
                    return RejectAttack(
                        CardActionResolutionRejectionReason.DefeatConsequenceStateUnavailable);
                }
            }

            events.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.AfterAttack));
            events.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.AttackResolutionCompleted(completedResolution, outcome)));
            events.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded));

            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
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
        private HeroDefeatResolution ResolveHeroDefeat(
            PendingAttackResolution completedResolution,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> events)
        {
            if (heroLevels == null || teamLives == null)
            {
                return null;
            }

            var defeatedHeroId = completedResolution.TargetHeroId;
            var defeatedTeamId = participants.GetTeamForHero(defeatedHeroId).Id;

            // p.13's two readings of the defeated hero, taken before anything moves. The Level is
            // authoritative state; the lowest Tier is read from the hero's own cards, over every
            // location they are in. p.15 asks the same question of the hand alone because it is
            // choosing a card there to upgrade; p.13 attaches no location to "that hero's lowest card
            // Tier", and only the whole-pool reading gives its table's answer whatever the hero has
            // played, discarded or is still holding when it is defeated.
            var defeatedLevel = heroLevels.GetLevel(defeatedHeroId);
            var hasTier = CardCharacteristicQuery.TryGetLowestNonBasicTier(
                new CardCharacteristicCriteria(ownerId: defeatedHeroId),
                definitions,
                instances,
                out var lowestTier);
            var defeatCoins = HeroDefeatRewardSchedule.DefeatCoins(defeatedLevel);
            var assistCoins = hasTier ? HeroDefeatRewardSchedule.AssistCoins(lowestTier) : 0;
            var lifeCounters = hasTier ? HeroDefeatRewardSchedule.LifeCounters(lowestTier) : 0;

            var outcome = new DefeatOutcome(
                new DefeatOutcomeId($"{completedResolution.ResolutionId.Value}:hero-defeat"),
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
                RuleActivity.Attack,
                RuleObject.ForHero(completedResolution.ActorId),
                adjacentMoveAuthority,
                adjacentMoveAuthority,
                participants,
                modifications,
                LanePushCauseKind.Defeat);
            var removalAttempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
            var prevented = removalStage.IsPrevented(subject);
            var origin = adjacentMoveAuthority.Occupancies
                .Where(occupancy => occupancy.HeroId.Equals(defeatedHeroId))
                .Select(occupancy => occupancy.Coordinate)
                .FirstOrDefault();
            var removedHero = default(HeroOccupancy);
            if (!prevented)
            {
                removedHero = adjacentMoveAuthority.CommitValidatedHeroRemoval(defeatedHeroId);
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
                    removedHero, resolvedCardId, lifeSpend, hasLifeSpend, lanePush);

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
                                completedResolution, attachment)));
                    }
                }

                events.Add(fact == HeroDefeatFactKind.DefeatRecorded
                    ? ResolutionStep.FromFact(
                        CardActionResolutionEvent.HeroDefeatRecorded(completedResolution, resolved))
                    : ResolutionStep.FromFact(
                        CardActionResolutionEvent.HeroDefeatFact(completedResolution, resolved, fact)));
            }

            return resolved;
        }

        internal AttackDefenseBackendRestoreState CaptureRestoreState() =>
            new AttackDefenseBackendRestoreState(pendingAttackResolution);

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
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null)
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
                    modifications);
                if (verifiedDefenseChoice != null ||
                    (verifiedTargetChoice != null &&
                        !AttackTargetChoicesMatch(expectedChoice, verifiedTargetChoice)))
                {
                    throw new ArgumentException("Pending Attack target choice does not match authoritative state.", nameof(verifiedTargetChoice));
                }

                targetChoice = verifiedTargetChoice ?? expectedChoice;
            }
            else
            {
                var legalTargets = backend.FindLegalAttackTargets(
                    pendingResolution.ActorId,
                    offer.Value,
                    materializedDefinitions,
                    participants,
                    modifications);
                if (!legalTargets.Contains(AttackTarget.Hero(pendingResolution.TargetHeroId)))
                {
                    throw new ArgumentException("Pending Attack target is no longer legal.", nameof(pendingResolution));
                }

                var expectedAttackValue = backend.CalculateAttackValue(offer.Value, materializedDefinitions);
                if (verifiedTargetChoice != null ||
                    !PrintedValuesMatch(pendingResolution.AttackValue, expectedAttackValue))
                {
                    throw new ArgumentException("Pending Attack continuation does not match authoritative state.", nameof(state));
                }

                if (pendingResolution.Phase == AttackResolutionPhase.DefenseResponse)
                {
                    var expectedDefenseOffers = ActionOfferQuery.GetAvailableOffers(
                        materializedDefinitions,
                        materializedInstances,
                        pendingResolution.TargetHeroId,
                        ActionOfferWindow.Reaction)
                        .Where(candidate => ActionOfferQuery.FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                        .ToArray();
                    if (verifiedDefenseChoice != null &&
                        !DefenseChoicesMatch(pendingResolution, expectedDefenseOffers, verifiedDefenseChoice))
                    {
                        throw new ArgumentException("Pending Defense response choice does not match authoritative Attack state.", nameof(verifiedDefenseChoice));
                    }

                    defenseChoice = verifiedDefenseChoice ?? new PendingDefenseResponseChoice(
                        pendingResolution.ResolutionId,
                        pendingResolution.CurrentStepId,
                        pendingResolution.TargetHeroId,
                        pendingResolution.ActorId,
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

        private PendingAttackResolution CreateOutcomeComparisonResolution(
            ActionOffer? defenseOffer,
            ValueResult defenseValue)
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
                pendingAttackResolution.TargetHeroId,
                pendingAttackResolution.AttackValue,
                defenseOffer,
                defenseValue);
        }

        private PendingAttackTargetChoice CreateAttackTargetChoice(
            PendingAttackResolution resolution,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            return new PendingAttackTargetChoice(
                resolution.ResolutionId,
                resolution.CurrentStepId,
                resolution.ActorId,
                FindLegalAttackTargets(
                    resolution.ActorId,
                    offer,
                    definitions,
                    participants,
                    modifications));
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
            IReadOnlyList<CardDefinition> definitions) =>
            ComposeItems(offer, definitions, PrintedValueChannel.Attack);

        /// <summary>The attacker's Range value, composed the same way (milestone 7.2a).</summary>
        private ValueResult RangeValue(ActionOffer offer, IReadOnlyList<CardDefinition> definitions) =>
            ComposeItems(offer, definitions, PrintedValueChannel.Range);

        private ValueResult ComposeItems(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            PrintedValueChannel channel)
        {
            var printed = PrintedValueQuery.CalculatePrintedValue(offer, definitions, channel);
            return heroItems == null
                ? printed
                : ItemBonusQuery.Apply(printed, heroItems, offer.ActorId);
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
            RuleModificationSet modifications)
        {
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (!ActionOfferQuery.TryFindActionDefinition(offer, materializedDefinitions, out var action) ||
                action.Kind != ActionKind.Attack ||
                (offer.AttackRange != AttackRangeKind.NonRanged &&
                    offer.AttackRange != AttackRangeKind.Ranged) ||
                !offer.ActorId.IsDefined ||
                !adjacentMoveAuthority.Occupancies.Any(occupancy =>
                    occupancy.HeroId.Equals(offer.ActorId)))
            {
                return true;
            }

            return FindLegalAttackTargets(
                offer.ActorId, offer, materializedDefinitions, participants, modifications).Count > 0;
        }

        private IReadOnlyList<AttackTarget> FindLegalAttackTargets(
            HeroId attackerId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            // Milestone 7.2a. A Ranged Attack's Range Total decides which enemies it may target, so a
            // +1 Range item reaches one space further - the page's own example of an item that matters
            // only to a card printing that icon. A non-Ranged Attack has no Range icon and is adjacency
            // by rule rather than by a Range of one, so nothing is composed onto it.
            var rangeTotal = offer.AttackRange == AttackRangeKind.Ranged
                ? RangeValue(offer, definitions).Total
                : 1;
            return AttackTargetQuery.FindLegalEnemyTargets(
                attackerId,
                offer.AttackRange,
                rangeTotal,
                participants,
                adjacentMoveAuthority,
                modifications);
        }

        private static ActionOffer? FindPendingAttackOffer(
            PendingAttackResolution resolution,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            var sourceCard = instances.SingleOrDefault(candidate =>
                candidate.Id.Equals(resolution.SourceCardInstanceId));
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
                    candidate.Value.InvocationContext == resolution.InvocationContext);
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

        private static bool DefenseChoicesMatch(
            PendingAttackResolution resolution,
            IReadOnlyList<ActionOffer> expectedOffers,
            PendingDefenseResponseChoice actual)
        {
            return actual.ResolutionId.Equals(resolution.ResolutionId) &&
                actual.ChoiceId.Equals(resolution.CurrentStepId) &&
                actual.ChooserId.Equals(resolution.TargetHeroId) &&
                actual.AttackerId.Equals(resolution.ActorId) &&
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
            if (!instance.OwnerId.Equals(resolution.TargetHeroId) ||
                !instance.DefinitionReference.Equals(offer.SourceDefinitionReference) ||
                instance.State.Location != CardLocation.DiscardPile ||
                action.Kind != ActionKind.Defense ||
                action.Origin != ActionDefinitionOrigin.Reaction ||
                !offer.SourceCardStateTransition.Equals(action.SourceCardStateTransition) ||
                offer.SourceCardStateTransition.RequiredLocation != CardLocation.Hand ||
                offer.SourceCardStateTransition.DestinationLocation != CardLocation.DiscardPile)
            {
                return false;
            }

            var expectedDefenseValue = DefenseValueQuery.Calculate(
                offer,
                definitions,
                resolution.TargetHeroId,
                participants,
                board,
                modifications,
                heroItems);
            return PrintedValuesMatch(resolution.DefenseValue, expectedDefenseValue);
        }
    }
}
