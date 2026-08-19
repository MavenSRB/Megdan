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
    /// <see cref="ActiveEffectPhase.Active"/>, a token supplies its property declarations only while
    /// that instance is on the board, and milestone 7.3's unlocked Ultimate supplies its permanent
    /// ability only while its hero is. All three meanings come from the trusted catalog objects the
    /// instances already hold, which is why milestone 6.8 needed no snapshot payload change and 7.3
    /// needs none either - a restored effect, token, or Ultimate rebinds to its definition and produces
    /// the same modifications again.
    /// </summary>
    public static class RuleModificationQuery
    {
        /// <summary>
        /// The live set at one authoritative moment. The Tie Breaker is required since milestone
        /// 6.8i: it orders p.17's simultaneous modifications, so a caller that omitted it silently
        /// derived a differently ordered set. A genuinely order-free caller passes
        /// <see cref="TieBreakerState.Unrevealed"/>.
        ///
        /// Milestone 7.3 adds the third source family and requires it for the same reason: a caller
        /// with genuinely no card content to read writes
        /// <see cref="GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None"/> rather than
        /// leaving it to a default.
        /// </summary>
        public static RuleModificationSet Build(
            ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates)
        {
            if (board != null && participants == null)
            {
                throw new ArgumentNullException(
                    nameof(participants),
                    "A token-owned modification needs its controller's team to be ordered against the Tie Breaker.");
            }

            if (ultimates == null)
            {
                throw new ArgumentNullException(
                    nameof(ultimates),
                    "An unlocked Ultimate supplies a rule; state UnlockedUltimateSet.None when there is none.");
            }

            if (!ultimates.IsEmpty && board == null)
            {
                // Rulebook p.16 makes the board the whole of an Ultimate's applicability. A caller that
                // supplies unlocked Ultimates and no board is asking a question that cannot be answered,
                // and answering it as "none apply" would be the silent substitution milestone 7.1a's
                // acceptance check forbids elsewhere for exactly this reason.
                throw new ArgumentNullException(
                    nameof(board),
                    "An unlocked Ultimate applies while its hero is on the board (rulebook p.16), " +
                    "which cannot be decided without one.");
            }

            var applied = new List<AppliedRuleModification>();
            if (timing != null)
            {
                applied.AddRange(FromActiveEffects(timing, participants));
            }

            if (board != null)
            {
                applied.AddRange(FromPlacedTokens(board, participants));
                applied.AddRange(FromUnlockedUltimates(ultimates, board, participants));
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

        /// <summary>
        /// Milestone 7.3. Rulebook p.16's Ultimate: "a powerful permanent ability that is always in
        /// effect (as long as your hero is on the board)."
        ///
        /// The parenthesis is the whole of this method's condition, and it is asked of the board rather
        /// than of any defeat, respawn, or removal record. That is deliberate: a hero leaves the board
        /// by more than one route in this build and will leave it by more again once milestone 7.4
        /// resolves Defeat, and a list of causes would have to be extended by every one of them, while
        /// "is this hero on the board" is the single question the page actually asks. The unlocked
        /// source itself is untouched by the answer - it is the card beside the dashboard, which no
        /// removal moves - so applicability is suppressed and restored without anything being deleted or
        /// recreated.
        ///
        /// The affected object is the controller. p.16 says "It provides <em>you</em> with" the ability,
        /// which is the same object a token property affects and for the same reason.
        /// </summary>
        private static IEnumerable<AppliedRuleModification> FromUnlockedUltimates(
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            for (var index = 0; index < ultimates.Ultimates.Count; index++)
            {
                var ultimate = ultimates.Ultimates[index];
                if (!board.TryGetHeroCoordinate(ultimate.OwnerId, out _))
                {
                    continue;
                }

                yield return AppliedRuleModification.FromContinuousSource(
                    ultimate.Rule.Modification,
                    RuleObject.ForHero(ultimate.OwnerId),
                    ultimate.Source,
                    participants.GetTeamForHero(ultimate.OwnerId).Side,
                    index);
            }
        }
    }
}
