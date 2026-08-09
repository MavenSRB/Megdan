using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class PendingMovementChoice
    {
        public PendingMovementChoice(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId chooserId,
            HeroId movingHeroId,
            HexCoordinate origin,
            int remainingAllowance,
            bool canStop,
            IEnumerable<HexCoordinate> legalStepDestinations)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Pending movement choice requires a resolution ID.", nameof(resolutionId));
            }

            if (!choiceId.IsDefined)
            {
                throw new ArgumentException("Pending movement choice requires a choice ID.", nameof(choiceId));
            }

            if (!chooserId.IsDefined)
            {
                throw new ArgumentException("Pending movement choice requires a chooser ID.", nameof(chooserId));
            }

            if (!movingHeroId.IsDefined)
            {
                throw new ArgumentException("Pending movement choice requires a moving hero ID.", nameof(movingHeroId));
            }

            if (remainingAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingAllowance),
                    remainingAllowance,
                    "Remaining movement allowance must not be negative.");
            }

            if (legalStepDestinations == null)
            {
                throw new ArgumentNullException(nameof(legalStepDestinations));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            MovingHeroId = movingHeroId;
            Origin = origin;
            RemainingAllowance = remainingAllowance;
            CanStop = canStop;
            var materializedDestinations = legalStepDestinations.ToArray();
            if (materializedDestinations.Distinct().Count() != materializedDestinations.Length)
            {
                throw new ArgumentException("Legal movement destinations must be unique.", nameof(legalStepDestinations));
            }

            LegalStepDestinations = Array.AsReadOnly(materializedDestinations);
        }

        public ActionResolutionId ResolutionId { get; }

        public ActionResolutionStepId ChoiceId { get; }

        public HeroId ChooserId { get; }

        public HeroId MovingHeroId { get; }

        public HexCoordinate Origin { get; }

        public int RemainingAllowance { get; }

        public bool CanStop { get; }

        public IReadOnlyList<HexCoordinate> LegalStepDestinations { get; }
    }
}
