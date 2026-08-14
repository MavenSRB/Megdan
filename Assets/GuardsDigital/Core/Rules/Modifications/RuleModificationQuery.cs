using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Derives the modifications currently in effect from authoritative state.
    ///
    /// Nothing is stored: an Active effect supplies its declaration only while its instance is
    /// <see cref="ActiveEffectPhase.Active"/>, and a token supplies its property declarations only while
    /// that instance is on the board. Both meanings come from the trusted catalog objects the instances
    /// already hold, which is why milestone 6.8 needed no snapshot payload change - a restored effect or
    /// token rebinds to its definition and produces the same modifications again.
    /// </summary>
    public static class RuleModificationQuery
    {
        public static RuleModificationSet Build(
            ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            TieBreakerState tieBreaker = default)
        {
            if (board != null && participants == null)
            {
                throw new ArgumentNullException(
                    nameof(participants),
                    "A token-owned modification needs its controller's team to be ordered against the Tie Breaker.");
            }

            var applied = new List<AppliedRuleModification>();
            if (timing != null)
            {
                applied.AddRange(FromActiveEffects(timing, participants));
            }

            if (board != null)
            {
                applied.AddRange(FromPlacedTokens(board, participants));
            }

            return applied.Count == 0
                ? new RuleModificationSet(Array.Empty<AppliedRuleModification>(), tieBreaker)
                : new RuleModificationSet(applied, tieBreaker);
        }

        private static IEnumerable<AppliedRuleModification> FromActiveEffects(
            ActiveEffectLifecycle timing,
            MatchParticipants participants)
        {
            foreach (var effect in timing.Effects)
            {
                // A dormant Next Turn effect has been created but has not come into effect yet, and an
                // ended one no longer applies. Only the active window supplies rules.
                if (effect.Phase != ActiveEffectPhase.Active ||
                    effect.Definition.Modifications.Count == 0)
                {
                    continue;
                }

                foreach (var affectedObject in AffectedObjects(effect, participants))
                {
                    foreach (var modification in effect.Definition.Modifications)
                    {
                        yield return AppliedRuleModification.FromActiveEffect(
                            modification,
                            affectedObject,
                            effect.Id,
                            effect.ControllerHeroId,
                            effect.ControllerTeamSide,
                            effect.CreationSequence);
                    }
                }
            }
        }

        private static IEnumerable<RuleObject> AffectedObjects(
            ActiveEffectInstance effect,
            MatchParticipants participants)
        {
            switch (effect.Definition.AffectedScope)
            {
                case ActiveEffectAffectedScopeKind.ControllerHero:
                    yield return RuleObject.ForHero(effect.ControllerHeroId);
                    break;
                case ActiveEffectAffectedScopeKind.Hero:
                    if (!effect.AffectedHeroId.IsDefined)
                    {
                        throw new InvalidOperationException(
                            $"Active effect '{effect.Id.Value}' declares modifications for a named hero but " +
                            "names none; its affected object cannot be derived.");
                    }

                    yield return RuleObject.ForHero(effect.AffectedHeroId);
                    break;
                case ActiveEffectAffectedScopeKind.ControllerTeam:
                    foreach (var heroId in HeroesOnSide(participants, effect.ControllerTeamSide, effect.Id))
                    {
                        yield return RuleObject.ForHero(heroId);
                    }

                    break;
                case ActiveEffectAffectedScopeKind.Global:
                    foreach (var heroId in AllHeroes(participants, effect.Id))
                    {
                        yield return RuleObject.ForHero(heroId);
                    }

                    break;
                default:
                    throw new InvalidOperationException(
                        $"Active effect '{effect.Id.Value}' declares modifications under an unsupported scope.");
            }
        }

        private static IEnumerable<HeroId> HeroesOnSide(
            MatchParticipants participants,
            TeamSide side,
            ActiveEffectId effectId) =>
            AllHeroes(participants, effectId)
                .Where(heroId => participants.GetTeamForHero(heroId).Side == side);

        private static IEnumerable<HeroId> AllHeroes(MatchParticipants participants, ActiveEffectId effectId)
        {
            if (participants == null)
            {
                throw new InvalidOperationException(
                    $"Active effect '{effectId.Value}' declares modifications for a team or the whole match, " +
                    "which cannot be expanded without the match participants.");
            }

            return participants.Heroes
                .Select(hero => hero.Id)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal);
        }

        /// <summary>
        /// Token-owned properties, ordered by the token instance's stable identity so the sequence does
        /// not depend on the order placements happen to sit in a list.
        /// </summary>
        private static IEnumerable<AppliedRuleModification> FromPlacedTokens(
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            var orderedTokens = board.PlacedTokens
                .OrderBy(token => token.Id.Value, StringComparer.Ordinal)
                .ToArray();
            for (var tokenIndex = 0; tokenIndex < orderedTokens.Length; tokenIndex++)
            {
                var token = orderedTokens[tokenIndex];
                if (!token.ControllerHeroId.IsDefined)
                {
                    continue;
                }

                var controllerSide = participants.GetTeamForHero(token.ControllerHeroId).Side;
                var affectedObject = RuleObject.ForHero(token.ControllerHeroId);

                // PlacedToken builds one property source per declared property, in declaration order,
                // so the two collections address the same property at the same index.
                for (var propertyIndex = 0; propertyIndex < token.Definition.Properties.Count; propertyIndex++)
                {
                    var property = token.Definition.Properties[propertyIndex];
                    if (property.Modification == null)
                    {
                        continue;
                    }

                    yield return AppliedRuleModification.FromContinuousSource(
                        property.Modification,
                        affectedObject,
                        token.PropertySources[propertyIndex],
                        controllerSide,
                        tokenIndex);
                }
            }
        }
    }
}
