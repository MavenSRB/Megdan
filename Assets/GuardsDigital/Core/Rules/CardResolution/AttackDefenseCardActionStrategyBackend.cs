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
        /// instance carries that the first did not is the target it may not take.</para>
        /// </summary>
        internal AttackActionResolutionResult StartRepeatedAttack(
            PendingAttackResolution completed,
            AttackTarget excludedTarget,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            if (pendingAttackResolution != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ResolutionAlreadyPending);
            }

            var repeated = new PendingAttackResolution(
                completed.ResolutionId,
                completed.SourceCardInstanceId,
                completed.SourceDefinitionReference,
                completed.ActionDefinitionId,
                completed.ActorId,
                completed.InvocationContext,
                new ActionResolutionStepId("attack-target"),
                AttackResolutionPhase.ChooseTarget,
                default(AttackTarget),
                null,
                excludedTarget: excludedTarget);
            var offer = FindPendingAttackOffer(
                repeated, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            if (!adjacentMoveAuthority.Occupancies.Any(
                occupancy => occupancy.HeroId.Equals(repeated.ActorId)))
            {
                return RejectAttack(CardActionResolutionRejectionReason.ActorUnavailable);
            }

            pendingAttackResolution = repeated;
            pendingAttackTargetChoice = CreateAttackTargetChoice(
                repeated, offer.Value, materializedDefinitions, participants, modifications);
            pendingDefenseChoice = null;

            if (pendingAttackTargetChoice.LegalTargets.Count == 0)
            {
                // p.10's repeat is offered, not owed: an instance with nobody left to take simply does
                // not happen, and the action that asked for it is already complete.
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
                return ResolveMinionTarget(
                    command.TargetMinionId, participants, modifications, materializedDefinitions);
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
                attackValue,
                excludedTarget: pendingAttackResolution.ExcludedTarget);
            var defenseOffers = FindDefenseOffers(
                materializedDefinitions, materializedInstances, command.TargetHeroId, modifications);
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

            var openingSteps = new List<ResolutionStep>
            {
                ResolutionStep.FromFact(
                    CardActionResolutionEvent.AttackTargetValidated(pendingAttackResolution)),
                ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack)
            };

            // p.14 step 1: "Attacker applies 'Before the attack' text, if any." p.13 puts it "at the
            // very beginning", which is before the Attack Total is announced and therefore before the
            // defender has anything to answer - and p.17's own note keeps it applied whether or not the
            // attack later succeeds.
            openingSteps.AddRange(CardTextAt(
                CardTextTiming.BeforeTheAttack,
                pendingAttackResolution,
                pendingAttackResolution.ActorId,
                materializedDefinitions));
            openingSteps.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.AttackStarted(pendingAttackResolution)));
            openingSteps.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.AttackTotalCalculated(pendingAttackResolution)));
            openingSteps.Add(ResolutionStep.AtCheckpoint(
                TimingCheckpoint.DefenseResponse, pendingAttackResolution.TargetHeroId));
            openingSteps.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.DefenseResponseChoiceRequested(pendingAttackResolution)));

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                null,
                pendingDefenseChoice,
                openingSteps);
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
        private static IEnumerable<ResolutionStep> CardTextAt(
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
        private static IEnumerable<ResolutionStep> CardTextAt(
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
            if (action == null || action.CardTextTiming != timing)
            {
                return Array.Empty<ResolutionStep>();
            }

            // Milestone 8.10. Only the two moments whose remainder is a function of the attack's
            // identity can carry one, so only they can hold a question; step 1 hands over nothing and
            // is refused at configuration instead.
            var carriesARemainder =
                timing == CardTextTiming.AfterTheAttack || timing == CardTextTiming.DefenseAction;

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
        internal static IReadOnlyList<ResolutionStep> StepsAfterCardText(
            CardTextTiming moment,
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            if (resolution == null) throw new ArgumentNullException(nameof(resolution));

            switch (moment)
            {
                // p.14 step 7 is the last thing the page does, so what stands after it is the outcome
                // this attack reached and the end of the action that carried it.
                case CardTextTiming.AfterTheAttack:
                    return Array.AsReadOnly(new[]
                    {
                        ResolutionStep.FromFact(
                            CardActionResolutionEvent.AttackResolutionCompleted(resolution, outcome)),
                        ResolutionStep.AtActionEnded(
                            resolution.SourceCardInstanceId,
                            resolution.ActionDefinitionId,
                            resolution.ActorId,
                            ActionKind.Attack)
                    });

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
                        "produce from the attack's identity alone.");
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
            IReadOnlyList<CardDefinition> definitions)
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
                excludedTarget: pendingAttackResolution.ExcludedTarget);
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
                ResolutionStep.AtCheckpoint(TimingCheckpoint.BeforeAttack)
            };

            // p.14 step 1, on the arm p.13 says has no steps 3 to 5: "Minions cannot defend. When you
            // target an enemy minion with an attack, it is simply defeated." The two card-text moments
            // that survive are the attacker's own, and they keep the page's positions around the
            // defeat that replaces the comparison.
            events.AddRange(CardTextAt(
                CardTextTiming.BeforeTheAttack, resolution, resolution.ActorId, definitions));
            events.Add(ResolutionStep.FromFact(CardActionResolutionEvent.AttackStarted(resolution)));
            events.AddRange(defeat.Facts.Select(fact => ResolutionStep.FromFact(
                CardActionResolutionEvent.MinionDefeatFact(resolution, defeat, fact))));
            events.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.AfterAttack));
            events.AddRange(CardTextAt(
                CardTextTiming.AfterTheAttack, resolution, resolution.ActorId, definitions,
                AttackResolutionOutcome.AttackSuccessful));
            events.AddRange(StepsAfterCardText(
                CardTextTiming.AfterTheAttack, resolution, AttackResolutionOutcome.AttackSuccessful));

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

            var currentDefenseOffers = FindDefenseOffers(
                materializedDefinitions, materializedInstances,
                pendingAttackResolution.TargetHeroId, modifications);
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

            // Milestone 8.11. p.14 gives a Defense card two shapes and this is where they part. A
            // numeric Defense calculates a total and enters the page's step 5 comparison; a Block
            // prints no value at all, and its condition decides the attack outright - "If the Block
            // condition is fulfilled, you successfully defend, regardless of the attack value. If not,
            // you are defeated." Calculating a total for one would be inventing a number the card does
            // not print, which is the sentinel-Defense mistake `PrintedDefenseResolution` exists to
            // refuse.
            var blockResult = EvaluateBlock(defenseOffer, materializedDefinitions);
            var defenseValue = blockResult == DefenseBlockResult.None
                ? DefenseValueQuery.Calculate(
                    defenseOffer,
                    materializedDefinitions,
                    pendingAttackResolution.TargetHeroId,
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
            if (!CardLifecycleAuthority.TryDiscardFromHand(defenseCard))
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
                    completedResolution,
                    completedResolution.TargetHeroId,
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
                CardTextTiming.AfterTheAttack, completedResolution, outcome));

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
            HeroId defeatedHeroId,
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
            PendingAttackResolution attack,
            HeroId defeatedHeroId,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            ICollection<ResolutionStep> events) =>
            ResolveHeroDefeat(
                attack ?? throw new ArgumentNullException(nameof(attack)),
                defeatedHeroId,
                definitions,
                instances,
                participants,
                modifications,
                events);

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
                    var expectedDefenseOffers = FindDefenseOffers(
                        materializedDefinitions, materializedInstances,
                        pendingResolution.TargetHeroId, modifications);
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
        private DefenseBlockResult EvaluateBlock(
            ActionOffer defenseOffer,
            IReadOnlyList<CardDefinition> definitions)
        {
            var action = ActionOfferQuery.FindActionDefinition(defenseOffer, definitions);
            if (action.DefenseResolution != PrintedDefenseResolution.Block)
            {
                return DefenseBlockResult.None;
            }

            var attack = definitions
                .FirstOrDefault(definition =>
                    definition.Reference.Equals(pendingAttackResolution.SourceDefinitionReference))
                ?.ActionDefinitions
                .FirstOrDefault(candidate =>
                    candidate.Id.Equals(pendingAttackResolution.ActionDefinitionId));
            return attack != null && attack.AttackRange == AttackRangeKind.Ranged
                ? DefenseBlockResult.Blocked
                : DefenseBlockResult.ConditionUnfulfilled;
        }

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
                pendingAttackResolution.TargetHeroId,
                pendingAttackResolution.AttackValue,
                defenseOffer,
                defenseValue,
                pendingAttackResolution.ExcludedTarget,
                blockResult);
        }

        private PendingAttackTargetChoice CreateAttackTargetChoice(
            PendingAttackResolution resolution,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            // Milestone 8.10. "on a different target" is the repeated instance's own narrowing, taken
            // out of the same legal set the first instance chose from rather than out of a second
            // query, so everything else the page decides about a target - reach, the printed policy,
            // p.18's immunity - is decided once and the exclusion is the only difference.
            var legal = FindLegalAttackTargets(
                resolution.ActorId, offer, definitions, participants, modifications);
            return new PendingAttackTargetChoice(
                resolution.ResolutionId,
                resolution.CurrentStepId,
                resolution.ActorId,
                resolution.ExcludedTarget.IsDefined
                    ? legal.Where(target => !target.Equals(resolution.ExcludedTarget)).ToArray()
                    : legal);
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

        /// <summary>
        /// The attacker's Range value, composed the same way (milestone 7.2a). Milestone 8.6 moved the
        /// composition itself into <see cref="RangeValueQuery"/> when Wasp's telekinetic Skills became
        /// a second reader of the same printed icon; this stays as the local name the resolution's
        /// three re-derivation sites call.
        /// </summary>
        private ValueResult RangeValue(ActionOffer offer, IReadOnlyList<CardDefinition> definitions) =>
            RangeValueQuery.Calculate(offer, definitions, heroItems);

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

            // Milestone 8.5. The policy is the card's own, not the classification's: p.13 states what
            // an Attack may target as a rule about the card, and a card that narrows it - a hero-only
            // Attack - says so in its definition. A definition that narrows nothing supplies the page's
            // default, so every Attack in this build reaches exactly what it reached before.
            var action = ActionOfferQuery.FindActionDefinition(offer, definitions);
            return AttackTargetQuery.FindLegalEnemyTargets(
                attackerId,
                action.AttackTargetPolicy ?? PrintedAttackTargetPolicy.ForClassification(offer.AttackRange),
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
        private static ActionOffer[] FindDefenseOffers(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            HeroId defenderId,
            RuleModificationSet modifications) =>
            DefenseOfferQuery.Offers(definitions, instances, defenderId, modifications).ToArray();

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
            var isBlock = action.DefenseResolution == PrintedDefenseResolution.Block;
            if (!instance.OwnerId.Equals(resolution.TargetHeroId) ||
                !instance.DefinitionReference.Equals(offer.SourceDefinitionReference) ||
                instance.State.Location != CardLocation.DiscardPile ||
                action.Kind != ActionKind.Defense ||
                // Milestone 8.11. p.14 prints a Block as the card's own primary and performs it only in
                // reaction, so the origin a Block carries is the one the face prints and the origin
                // every other Defense carries is the reaction it is printed as. Both are Defense
                // actions discarded from hand, which is what the rest of this check holds.
                (isBlock
                    ? action.Origin != ActionDefinitionOrigin.PrintedPrimary
                    : action.Origin != ActionDefinitionOrigin.Reaction) ||
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
                participants,
                board,
                modifications,
                heroItems);
            return PrintedValuesMatch(resolution.DefenseValue, expectedDefenseValue);
        }
    }
}
