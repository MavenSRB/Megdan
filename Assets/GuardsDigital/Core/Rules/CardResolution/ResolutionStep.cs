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
        Checkpoint = 2
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
            CardInstanceId checkpointSourceCardInstanceId)
        {
            Kind = kind;
            Fact = fact;
            Checkpoint = checkpoint;
            CheckpointAffectedHeroId = checkpointAffectedHeroId;
            CheckpointSourceCardInstanceId = checkpointSourceCardInstanceId;
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

            return new ResolutionStep(
                ResolutionStepKind.Checkpoint,
                default,
                checkpoint,
                checkpointAffectedHeroId,
                checkpointSourceCardInstanceId);
        }
    }
}
