using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Movement;
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
            IEnumerable<ReachableMovementDestination> legalDestinations)
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

            if (legalDestinations == null)
            {
                throw new ArgumentNullException(nameof(legalDestinations));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChooserId = chooserId;
            MovingHeroId = movingHeroId;
            Origin = origin;
            RemainingAllowance = remainingAllowance;
            CanStop = canStop;
            var materializedDestinations = legalDestinations.ToArray();
            if (materializedDestinations.Select(candidate => candidate.Destination).Distinct().Count() !=
                materializedDestinations.Length)
            {
                throw new ArgumentException("Legal movement destinations must be unique.", nameof(legalDestinations));
            }

            foreach (var candidate in materializedDestinations)
            {
                if (candidate.Cost <= 0 || candidate.Route == null)
                {
                    throw new ArgumentException(
                        "Every legal movement destination must contain defined route data.",
                        nameof(legalDestinations));
                }

                if (candidate.Cost > remainingAllowance)
                {
                    throw new ArgumentException(
                        "A legal movement destination cannot cost more than the remaining allowance.",
                        nameof(legalDestinations));
                }

                if (!candidate.Route[0].Equals(origin))
                {
                    throw new ArgumentException(
                        "Every legal movement route must start at the pending choice origin.",
                        nameof(legalDestinations));
                }
            }

            LegalDestinations = Array.AsReadOnly(materializedDestinations);
        }

        public ActionResolutionId ResolutionId { get; }

        public ActionResolutionStepId ChoiceId { get; }

        public HeroId ChooserId { get; }

        public HeroId MovingHeroId { get; }

        public HexCoordinate Origin { get; }

        public int RemainingAllowance { get; }

        public bool CanStop { get; }

        public IReadOnlyList<ReachableMovementDestination> LegalDestinations { get; }
    }
}
