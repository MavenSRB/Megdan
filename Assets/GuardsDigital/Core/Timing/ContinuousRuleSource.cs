using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Timing
{
    public enum ContinuousRuleSourceKind
    {
        Ultimate = 1,
        TokenProperty = 2,
        MarkerProperty = 3
    }

    public sealed class ContinuousRuleSource
    {
        public ContinuousRuleSource(
            string sourceId,
            ContinuousRuleSourceKind kind,
            HeroId controllerHeroId,
            string ruleKind,
            string applicabilityKind,
            CardInstanceId sourceCardInstanceId = default)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(ruleKind) ||
                string.IsNullOrWhiteSpace(applicabilityKind))
                throw new ArgumentException("A continuous source requires stable identity, rule, and applicability.");
            if (!controllerHeroId.IsDefined)
                throw new ArgumentException("A continuous source requires its controller.", nameof(controllerHeroId));
            if (!Enum.IsDefined(typeof(ContinuousRuleSourceKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            SourceId = sourceId;
            Kind = kind;
            ControllerHeroId = controllerHeroId;
            RuleKind = ruleKind;
            ApplicabilityKind = applicabilityKind;
            SourceCardInstanceId = sourceCardInstanceId;
        }

        public string SourceId { get; }
        public ContinuousRuleSourceKind Kind { get; }
        public HeroId ControllerHeroId { get; }
        public string RuleKind { get; }
        public string ApplicabilityKind { get; }
        public CardInstanceId SourceCardInstanceId { get; }
    }
}
