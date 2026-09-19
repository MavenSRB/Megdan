using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.57c. One request for rulebook p.13's Defeat list: the hero it is run about, and
    /// what that one Defeat multiplies its ASSIST coins by.
    ///
    /// <para><b>Why the request grew a second field rather than the spend site growing a rule.</b>
    /// Rowenna's Glorious Triumph prints "your friendly heroes gain triple assist coins" in the same
    /// sentence that asks for the Defeat, so the clause is true of THIS Defeat and of no other - and a
    /// <see cref="GuardsDigital.Core.Rules.Modifications.RuleModificationSet"/>, which is derived once
    /// at the command boundary and is true of everything while it stands, cannot say "this one". Bain's
    /// <c>AdditionalDefeatLifeCostWhenDefeated</c> is the shape that CAN be said that way - a property
    /// of a marker a hero is wearing - and the two are kept apart deliberately.</para>
    ///
    /// <para><b>Every request carries a multiplier and almost every one carries one.</b> p.14's step 6
    /// and every effect-invoked Defeat composed before this milestone multiply by
    /// <see cref="UnmultipliedAssistCoins"/>, which is p.13's own schedule unchanged; the field exists
    /// so the spend site reads a number rather than asking whether somebody declared one.</para>
    /// </summary>
    public readonly struct RequestedHeroDefeat : IEquatable<RequestedHeroDefeat>
    {
        /// <summary>p.13's own schedule, as a multiplier: one. Spelled where the capability spells it.</summary>
        public const int UnmultipliedAssistCoins =
            SyntheticConsequenceCapabilities.UnmultipliedAssistCoins;

        public RequestedHeroDefeat(HeroId heroId, int assistCoinMultiplier = UnmultipliedAssistCoins)
        {
            if (assistCoinMultiplier < UnmultipliedAssistCoins)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(assistCoinMultiplier),
                    assistCoinMultiplier,
                    "A printed clause multiplies p.13's assist coins; nothing printed reduces them, " +
                    "so the smallest legal multiplier is the page's own schedule.");
            }

            HeroId = heroId;
            AssistCoinMultiplier = assistCoinMultiplier;
        }

        /// <summary>The hero p.13's list is run about.</summary>
        public HeroId HeroId { get; }

        /// <summary>What this one Defeat's assist coins are multiplied by.</summary>
        public int AssistCoinMultiplier { get; }

        public bool Equals(RequestedHeroDefeat other) =>
            HeroId.Equals(other.HeroId) && AssistCoinMultiplier == other.AssistCoinMultiplier;

        public override bool Equals(object obj) => obj is RequestedHeroDefeat other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (HeroId.GetHashCode() * 397) ^ AssistCoinMultiplier;
            }
        }

        public override string ToString() =>
            AssistCoinMultiplier == UnmultipliedAssistCoins
                ? HeroId.ToString()
                : $"{HeroId} x{AssistCoinMultiplier} assist";
    }

    /// <summary>
    /// Milestone 8.10: which heroes a resolution's completed outcomes asked rulebook p.13's Defeat list
    /// to be run about, read exactly the way milestone 6.9 reads p.8's Lane-Push cause.
    ///
    /// <para>A request is an ordinary applied node outcome, which is the transport milestone 6.7
    /// established and 6.9 reused: a fact in the completed frame survives a pause, a snapshot restore,
    /// a replay and a retained-prefix Undo without a field of its own inside the effect frame. Nothing
    /// new is persisted to make a defeat requested inside card text survive the question that card text
    /// stopped on - and Electroblast's rider stops on two.</para>
    ///
    /// <para>The node is confirmed against the trusted definition before its fact is believed, which is
    /// 6.7's discipline: a payload names node identities and never semantics, so an outcome naming a
    /// node this definition does not declare asks for nothing.</para>
    /// </summary>
    public static class RequestedHeroDefeatQuery
    {
        /// <summary>
        /// The heroes named by every applied defeat request in <paramref name="outcomes"/>, in the
        /// order the frame recorded them, so a composition asking twice is run twice in its own order
        /// rather than collapsed to one answer this query picked.
        /// </summary>
        public static IReadOnlyList<RequestedHeroDefeat> Requested(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return Array.Empty<RequestedHeroDefeat>();
            }

            var requested = new List<RequestedHeroDefeat>();
            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out _))
                {
                    continue;
                }

                // Milestone 8.57c. The multiplier is read from the SAME outcome as the hero, because
                // it is part of the same request: a composition asking for two Defeats in one frame -
                // which this query has admitted since 8.10 - must not have one sentence's clause reach
                // the other sentence's Defeat. A request that published none is p.13's own schedule.
                var multiplier = RequestedHeroDefeat.UnmultipliedAssistCoins;
                foreach (var fact in outcome.Outputs)
                {
                    if (string.Equals(
                            fact.OutputName,
                            SyntheticConsequenceCapabilities.AssistCoinMultiplierOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Integer &&
                        fact.Value.IntegerValue > multiplier)
                    {
                        multiplier = fact.Value.IntegerValue;
                    }
                }

                foreach (var fact in outcome.Outputs)
                {
                    if (string.Equals(
                            fact.OutputName,
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Hero &&
                        fact.Value.HeroId.IsDefined)
                    {
                        requested.Add(new RequestedHeroDefeat(fact.Value.HeroId, multiplier));
                    }
                }
            }

            return requested;
        }
    }
}
