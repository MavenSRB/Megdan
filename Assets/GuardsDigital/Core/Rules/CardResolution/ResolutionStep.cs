using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum ResolutionStepKind
    {
        None = 0,

        /// <summary>An ordered rule fact the action produced.</summary>
        Fact = 1,

        /// <summary>A timing checkpoint the resolver raises at this exact position in the action.</summary>
        Checkpoint = 2,

        /// <summary>
        /// Milestone 8.5. Rulebook p.14 applies a card's printed text at three numbered moments
        /// inside an Attack, and this is the resolver saying it has reached one of them. The step
        /// carries the card and action whose text applies and nothing about what the text does: the
        /// backend knows p.14's order and owns it, and the authority that holds the effect catalog,
        /// the components and the timing service is what runs the composition.
        /// </summary>
        CardText = 3
    }

    /// <summary>
    /// Milestone 6.1ap, finding <c>F-ER2-05</c>. One entry in the ordered stream a card action emits.
    ///
    /// Before this, a resolver emitted only facts and <c>TurnResolutionAuthority.ApplyActionTiming</c>
    /// reconstructed the timing afterwards by reading each fact's kind. Interleaving checkpoints into
    /// the same stream lets the resolver say when a checkpoint opens, at the point it opens, and
    /// leaves the turn authority nothing to infer.
    ///
    /// The step deliberately carries no action-family discriminator. Every call site already knows
    /// statically which family it is resolving, so it supplies the adapter; adding a family here would
    /// only recreate a switch in the layer this milestone is clearing.
    /// </summary>
    public readonly struct ResolutionStep
    {
        private ResolutionStep(
            ResolutionStepKind kind,
            CardActionResolutionEvent fact,
            TimingCheckpoint checkpoint,
            HeroId checkpointAffectedHeroId,
            CardInstanceId checkpointSourceCardInstanceId,
            ActionDefinitionId cardTextActionDefinitionId = default,
            HeroId cardTextActorId = default,
            CardTextTiming cardTextTiming = CardTextTiming.None,
            ActionDefinitionId completedActionDefinitionId = default,
            HeroId completedActorId = default,
            ActionKind completedActionKind = ActionKind.None,
            PendingAttackResolution cardTextRemainderResolution = null,
            AttackResolutionOutcome cardTextRemainderOutcome = AttackResolutionOutcome.None)
        {
            CardTextRemainderResolution = cardTextRemainderResolution;
            CardTextRemainderOutcome = cardTextRemainderOutcome;
            Kind = kind;
            Fact = fact;
            Checkpoint = checkpoint;
            CheckpointAffectedHeroId = checkpointAffectedHeroId;
            CheckpointSourceCardInstanceId = checkpointSourceCardInstanceId;
            CardTextActionDefinitionId = cardTextActionDefinitionId;
            CardTextActorId = cardTextActorId;
            CardTextTiming = cardTextTiming;
            CompletedActionDefinitionId = completedActionDefinitionId;
            CompletedActorId = completedActorId;
            CompletedActionKind = completedActionKind;
        }

        public ResolutionStepKind Kind { get; }

        /// <summary>Defined only when <see cref="Kind"/> is <see cref="ResolutionStepKind.Fact"/>.</summary>
        public CardActionResolutionEvent Fact { get; }

        /// <summary>Defined only when <see cref="Kind"/> is <see cref="ResolutionStepKind.Checkpoint"/>.</summary>
        public TimingCheckpoint Checkpoint { get; }

        /// <summary>
        /// The hero a checkpoint concerns when it is not the acting hero - the Defense-response
        /// checkpoint names the defender. Left undefined to mean "the current actor".
        /// </summary>
        public HeroId CheckpointAffectedHeroId { get; }

        public CardInstanceId CheckpointSourceCardInstanceId { get; }

        public static ResolutionStep FromFact(CardActionResolutionEvent fact) =>
            new ResolutionStep(ResolutionStepKind.Fact, fact, TimingCheckpoint.None, default, default);

        public static ResolutionStep AtCheckpoint(
            TimingCheckpoint checkpoint,
            HeroId checkpointAffectedHeroId = default,
            CardInstanceId checkpointSourceCardInstanceId = default)
        {
            if (checkpoint == TimingCheckpoint.None)
            {
                throw new ArgumentException(
                    "A resolution checkpoint step requires a defined checkpoint.", nameof(checkpoint));
            }

            if (checkpoint == TimingCheckpoint.ActionEnded)
            {
                // Milestone 8.9. The completion checkpoint is the one place rulebook p.9's supertype is
                // recorded, and it can only be recorded by naming the action that ended. Refusing the
                // plain factory here is what makes "every action performed on a basic card records the
                // fact" true by construction rather than by every resolver remembering to: a new action
                // family cannot reach this checkpoint without saying what it just finished.
                throw new ArgumentException(
                    "Use AtActionEnded: the completion checkpoint names the action it completes.",
                    nameof(checkpoint));
            }

            return new ResolutionStep(
                ResolutionStepKind.Checkpoint,
                default,
                checkpoint,
                checkpointAffectedHeroId,
                checkpointSourceCardInstanceId);
        }

        /// <summary>
        /// Milestone 8.9. The action this resolver just finished, at the checkpoint that finishes it.
        ///
        /// It carries the three identities rulebook p.9's supertype needs and nothing it could infer:
        /// the card, because that is what decides whether the action was basic; the hero, because a
        /// trigger asks whose action it was; and the kind, because "a basic <em>skill</em>" narrows by
        /// it and a replacement action - p.7's Fast Travel, p.13's Clear - has no definition on the
        /// card to look one up on. Every call site knows its kind statically, which is why it is stated
        /// rather than resolved.
        /// </summary>
        public static ResolutionStep AtActionEnded(
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionKind kind)
        {
            if (!sourceCardInstanceId.IsDefined || !actionDefinitionId.IsDefined || !actorId.IsDefined)
            {
                throw new ArgumentException(
                    "A completion step names the card, the action and the hero whose action ended.",
                    nameof(sourceCardInstanceId));
            }

            if (kind == ActionKind.None)
            {
                throw new ArgumentException(
                    "A completion step names the kind of action that ended.", nameof(kind));
            }

            return new ResolutionStep(
                ResolutionStepKind.Checkpoint,
                default,
                TimingCheckpoint.ActionEnded,
                default,
                sourceCardInstanceId,
                completedActionDefinitionId: actionDefinitionId,
                completedActorId: actorId,
                completedActionKind: kind);
        }

        /// <summary>
        /// Milestone 8.5. One of rulebook p.14's numbered card-text moments, at the exact position in
        /// the stream the page puts it: step 1 for the attacker's "Before the attack" text, step 4 for
        /// the defender's Defense action text, step 7 for the attacker's "After the attack:" text.
        ///
        /// <paramref name="actorId"/> is whose card it is, which is the attacker at steps 1 and 7 and
        /// the defender at step 4; p.10 gives every choice inside a card's text to that hero.
        ///
        /// <para>Milestone 8.10 added the last two arguments, which are what this moment's remainder is
        /// a function of. Card text may now ask a question, and the steps standing after it cannot
        /// cross that pause on the wire - they are facts about an attack rather than identity - so what
        /// crosses instead is the resolution and outcome
        /// <see cref="AttackDefenseCardActionStrategyBackend.StepsAfterCardText"/> rebuilds them from.
        /// A step that carries none is a moment whose remainder is not a function of identity, which
        /// today is p.14's step 1, and text that paused there would be a content fault rather than a
        /// rules outcome. They never reach the wire themselves: a step is an in-memory instruction to
        /// the materializer, and it is the continuation the pause creates that is persisted.</para>
        /// </summary>
        public static ResolutionStep ApplyCardText(
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            CardTextTiming timing,
            PendingAttackResolution remainderResolution = null,
            AttackResolutionOutcome remainderOutcome = AttackResolutionOutcome.None)
        {
            if (!sourceCardInstanceId.IsDefined || !actionDefinitionId.IsDefined || !actorId.IsDefined)
            {
                throw new ArgumentException(
                    "A card-text step names the card, the action and the hero whose text applies.",
                    nameof(sourceCardInstanceId));
            }

            if (timing == CardTextTiming.None)
            {
                throw new ArgumentException(
                    "A card-text step names which of rulebook p.14's moments it is.", nameof(timing));
            }

            return new ResolutionStep(
                ResolutionStepKind.CardText,
                default,
                TimingCheckpoint.None,
                default,
                sourceCardInstanceId,
                actionDefinitionId,
                actorId,
                timing,
                cardTextRemainderResolution: remainderResolution,
                cardTextRemainderOutcome: remainderOutcome);
        }

        /// <summary>Defined only when <see cref="Kind"/> is <see cref="ResolutionStepKind.CardText"/>.</summary>
        public ActionDefinitionId CardTextActionDefinitionId { get; }

        /// <summary>The hero whose card text this is; defined only on a card-text step.</summary>
        public HeroId CardTextActorId { get; }

        /// <summary>Which of p.14's moments this is; defined only on a card-text step.</summary>
        public CardTextTiming CardTextTiming { get; }

        /// <summary>
        /// The attack whose remainder stands after this moment, or <c>null</c> at a moment whose
        /// remainder no identity can reproduce. Milestone 8.10; defined only on a card-text step.
        /// </summary>
        public PendingAttackResolution CardTextRemainderResolution { get; }

        /// <summary>The outcome that remainder reports; defined only beside the resolution above.</summary>
        public AttackResolutionOutcome CardTextRemainderOutcome { get; }

        /// <summary>
        /// The action that ended; defined only on an <see cref="TimingCheckpoint.ActionEnded"/> step,
        /// where the card it was performed on is <see cref="CheckpointSourceCardInstanceId"/>.
        /// </summary>
        public ActionDefinitionId CompletedActionDefinitionId { get; }

        /// <summary>The hero who performed it; defined only on a completion step.</summary>
        public HeroId CompletedActorId { get; }

        /// <summary>Which action it was; defined only on a completion step.</summary>
        public ActionKind CompletedActionKind { get; }
    }
}
