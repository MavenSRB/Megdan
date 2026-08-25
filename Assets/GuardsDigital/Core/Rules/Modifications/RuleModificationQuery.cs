using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
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
        ///
        /// Milestone 8.7 requires the acting object for the third time over: a conditional obstacle
        /// overlay applies "while an enemy hero is performing an action", so which hero that is decides
        /// what the board's spaces are. A caller at a moment when nobody is acting writes
        /// <see cref="RuleObject.None"/>, and every overlay then applies to nobody.
        ///
        /// Milestone 8.9 adds the items, and they are nullable rather than required because that is
        /// what they already are wherever a printed total is composed: p.16's Item Bonus reaches an
        /// Active effect's declared Radius icon exactly as it reaches the same icon on the card being
        /// performed, and <c>null</c> is a composition holding no items at all rather than a hero
        /// holding none.
        /// </summary>
        public static RuleModificationSet Build(
            ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet ultimates,
            RuleObject actingObject,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
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
                applied.AddRange(FromActiveEffects(timing, board, participants, heroItems));
            }

            if (board != null)
            {
                applied.AddRange(FromPlacedTokens(board, participants));
                applied.AddRange(FromUnlockedUltimates(ultimates, board, participants));
            }

            var overlays = timing == null || board == null
                ? (IEnumerable<AppliedObstacleOverlay>)Array.Empty<AppliedObstacleOverlay>()
                : FromDeclaredOverlays(timing, board, participants, actingObject, heroItems);
            return new RuleModificationSet(applied, overlays, tieBreaker);
        }

        /// <summary>
        /// Milestone 8.7's conditional obstacle overlays, bound to the moment this set describes.
        ///
        /// Two things are resolved here rather than left to the consumer, and both for the reason the
        /// team and global affected scopes are expanded here: this derivation holds the board and the
        /// participants. The anchor is the source hero's current space, which is p.17's "the Radius
        /// moves with its hero" stated once; and whether the hero performing the current action
        /// qualifies, and on which side of the disk it stands, is decided from
        /// <paramref name="actingObject"/>.
        ///
        /// A set built with <see cref="RuleObject.None"/> carries every overlay applying to nobody.
        /// That is the honest answer rather than an omission: an overlay conditions on a hero
        /// performing an action, and at a round boundary or a restore with nothing in flight, none is.
        /// A source whose hero has left the board supplies nothing at all, exactly as p.16's Ultimate
        /// does.
        /// </summary>
        private static IEnumerable<AppliedObstacleOverlay> FromDeclaredOverlays(
            ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            RuleObject actingObject,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
        {
            TeamSide? actingSide = null;
            var actingSpace = default(GuardsDigital.Core.Board.HexCoordinate);
            if (actingObject.IsDefined &&
                IntrinsicKindQuery.IntrinsicUnitKind(actingObject) == EffectiveUnitKind.Hero &&
                participants != null &&
                board.TryGetHeroCoordinate(actingObject.HeroId, out actingSpace))
            {
                actingSide = participants.GetTeamForHero(actingObject.HeroId).Side;
            }

            foreach (var effect in timing.Effects)
            {
                if (effect.Phase != ActiveEffectPhase.Active ||
                    effect.Definition.ObstacleOverlays.Count == 0 ||
                    !board.TryGetHeroCoordinate(effect.ControllerHeroId, out var anchor))
                {
                    continue;
                }

                var radius = ResolveRadius(effect, heroItems);
                for (var index = 0; index < effect.Definition.ObstacleOverlays.Count; index++)
                {
                    yield return AppliedObstacleOverlay.Bind(
                        effect.Definition.ObstacleOverlays[index],
                        anchor,
                        radius,
                        $"{effect.Id.Value}:overlay:{index}",
                        effect.ControllerHeroId,
                        effect.ControllerTeamSide,
                        actingSide,
                        actingSpace);
                }
            }
        }

        private static IEnumerable<AppliedRuleModification> FromActiveEffects(
            ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
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

                foreach (var affectedObject in AffectedObjects(effect, board, participants, heroItems))
                {
                    foreach (var modification in effect.Definition.Modifications)
                    {
                        var resolved = ResolveDerivedAmount(
                            modification, affectedObject, effect.ControllerHeroId, board, participants);
                        if (resolved == null)
                        {
                            continue;
                        }

                        yield return AppliedRuleModification.FromActiveEffect(
                            resolved,
                            affectedObject,
                            effect.Id,
                            effect.ControllerHeroId,
                            effect.ControllerTeamSide,
                            effect.CreationSequence,
                            effect.BoundActorHeroId);
                    }
                }
            }
        }

        private static IEnumerable<RuleObject> AffectedObjects(
            ActiveEffectInstance effect,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
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
                case ActiveEffectAffectedScopeKind.UnitsInRadius:
                case ActiveEffectAffectedScopeKind.HeroesInRadius:
                case ActiveEffectAffectedScopeKind.MinionsInRadius:
                case ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius:
                    // Milestone 8.20 added the second of these and milestone 8.21 the third and
                    // fourth - p.19's heroes alone, p.19's minions alone, and the units beside the
                    // hero the disk is drawn around. The walk below is shared rather than copied,
                    // because the disk, the anchor and the side are one reading whichever kinds it
                    // admits, and four copies could disagree about any of the three.
                    foreach (var unit in UnitsInRadius(
                        effect, board, participants, heroItems, effect.Definition.AffectedScope))
                    {
                        yield return unit;
                    }

                    break;
                default:
                    throw new InvalidOperationException(
                        $"Active effect '{effect.Id.Value}' declares modifications under an unsupported scope.");
            }
        }

        /// <summary>
        /// Milestone 8.9. Rulebook p.19's units standing inside the disk this effect draws, on the side
        /// of its source that it names - Wasp's Magnetic Dagger, "Enemy units in radius".
        ///
        /// <para>Three of its four decisions are deliberate refusals to ask a second question. The disk
        /// is anchored at the controller's current space, which is p.17's Radius moving with its hero
        /// and the same anchor the overlays take. Membership is geometry alone -
        /// <see cref="RadiusQuery.Contains"/>, counting spaces through obstacles - because p.9 says
        /// there are no line-of-sight rules. And the side is
        /// <see cref="TargetQuery.StandsInRelationTo"/>, the relationship arm milestone 8.8 opened,
        /// rather than a second reading of who is whose enemy.</para>
        ///
        /// <para>The fourth is the one that could not have been otherwise: nothing here consults the
        /// modification set, because this <em>is</em> its derivation. That costs nothing the rules
        /// wanted. p.17's <c>Counts As</c> replaces a unit's type, and both types this walks are units,
        /// so no declaration can move an object in or out of "enemy units" - only off the board, which
        /// takes it out of the walk itself. A controller who has left the board anchors no disk and the
        /// effect affects nobody, exactly as p.16's Ultimate supplies nothing from off board.</para>
        ///
        /// <para>The source itself is never among them, for the reason
        /// <see cref="TargetQuery.FindTargetsWithin"/> asks before admitting the object at its origin:
        /// p.19 keeps self and another distinct. The card that needed this scope excludes its own hero
        /// by naming the other side anyway, so the exclusion decides nothing today and states the
        /// page's reading for the declaration that names no side.</para>
        /// </summary>
        /// <param name="scope">
        /// Which of the four disk scopes is being walked, which decides two things and nothing else:
        /// the unit kinds it admits - every unit for Wasp's "Enemy units in radius", p.19's heroes
        /// alone for Arien's "Enemy heroes in radius", p.19's minions alone for Brogan's "any friendly
        /// minion in radius" - and whether the hero the disk is drawn around is one of them, which only
        /// Brogan's Bulwark says. It is intrinsic membership on purpose and by necessity: the paragraph
        /// above says why this walk consults no declaration, and which miniature is standing in the
        /// disk is the one question about a piece that a <c>Counts As</c> does not answer.
        /// </param>
        private static IEnumerable<RuleObject> UnitsInRadius(
            ActiveEffectInstance effect,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            ActiveEffectAffectedScopeKind scope)
        {
            var admitsHeroes = scope != ActiveEffectAffectedScopeKind.MinionsInRadius;
            var admitsMinions = scope != ActiveEffectAffectedScopeKind.HeroesInRadius;
            var admitsController = scope == ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius;

            if (board == null)
            {
                throw new InvalidOperationException(
                    $"Active effect '{effect.Id.Value}' affects the units inside a radius, which " +
                    "cannot be decided without the board they are standing on.");
            }

            if (!board.TryGetHeroCoordinate(effect.ControllerHeroId, out var anchor))
            {
                yield break;
            }

            var radius = new ValueResult(
                PrintedValueChannel.Radius,
                new[]
                {
                    new ValueContribution(
                        ValueContributionKind.PrintedIcon,
                        effect.SourceCardInstanceId,
                        effect.SourceActionDefinitionId,
                        PrintedValueChannel.Radius,
                        ResolveRadius(effect, heroItems))
                });
            var relation = effect.Definition.AffectedUnitRelation;

            // Milestone 8.21. Bulwark's own "You", admitted before the walk and without asking the
            // relation: the hero anchoring the disk is standing at its centre, which p.9 measures
            // inclusively, and he is trivially on his own side.
            if (admitsController)
            {
                yield return RuleObject.ForHero(effect.ControllerHeroId);
            }

            if (admitsHeroes)
            {
                foreach (var occupancy in board.Occupancies)
                {
                    var hero = BoardObjectReference.ForHero(occupancy.HeroId);
                    if (!occupancy.HeroId.Equals(effect.ControllerHeroId) &&
                        RadiusQuery.Contains(anchor, radius, occupancy.Coordinate) &&
                        TargetQuery.StandsInRelationTo(
                            relation, hero, effect.ControllerHeroId, participants, board))
                    {
                        yield return RuleObject.ForHero(occupancy.HeroId);
                    }
                }
            }

            if (!admitsMinions)
            {
                yield break;
            }

            foreach (var minion in board.Minions)
            {
                var unit = BoardObjectReference.ForMinion(minion.MinionId);
                if (RadiusQuery.Contains(anchor, radius, minion.Coordinate) &&
                    TargetQuery.StandsInRelationTo(
                        relation, unit, effect.ControllerHeroId, participants, board))
                {
                    yield return RuleObject.ForMinion(minion.MinionId);
                }
            }
        }

        /// <summary>
        /// The disk one effect draws right now: its declared radius with p.16's items composed onto it
        /// when the declaration is a printed icon. Asked once per effect so the scope and the overlays
        /// of one definition cannot measure different disks.
        /// </summary>
        private static int ResolveRadius(
            ActiveEffectInstance effect,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems) =>
            effect.Definition.Radius.Resolve(
                effect.ControllerHeroId,
                effect.SourceCardInstanceId,
                effect.SourceActionDefinitionId,
                heroItems);

        /// <summary>
        /// Milestone 8.19c. One declaration with its derived count already taken, or <c>null</c> when
        /// the count is zero and the declaration therefore supplies nothing.
        ///
        /// <para>Resolving it here rather than at the consumer is what makes "recalculated at each
        /// owning query" true without any consumer knowing a count was involved: this derivation runs
        /// again every time the live set is built, so Xargatha's Metamorphosis reports +2 Movement
        /// while two enemies stand beside her and +1 the moment one walks away. A consumer that had to
        /// re-count would be a second place the rule could be forgotten.</para>
        ///
        /// <para>A zero count yields no modification at all rather than a zero one, which is the same
        /// statement <see cref="RuleModification.ForValue(PrintedValueChannel,int)"/> makes by refusing
        /// a zero amount: "+1 for each enemy adjacent to you" with no enemy adjacent is not a rule
        /// contributing nothing, it is a rule that has not applied.</para>
        ///
        /// <para>Nothing here consults a modification set, for the reason
        /// <see cref="UnitsInRadius"/> consults none: this <em>is</em> its derivation. The count is
        /// therefore geometry and team relation alone, exactly as that scope's membership is, and a
        /// <c>Counts As</c> cannot move a unit in or out of "enemy units" because both types it could
        /// name are units.</para>
        /// </summary>
        private static RuleModification ResolveDerivedAmount(
            RuleModification modification,
            RuleObject affectedObject,
            HeroId sourceControllerHeroId,
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            if (modification == null || !modification.IsDerived)
            {
                return modification;
            }

            if (board == null)
            {
                throw new InvalidOperationException(
                    "A derived value modifier counts units on the board, which cannot be done " +
                    "without one.");
            }

            var matches = CountDerivedMatches(
                modification.DerivedQuery, affectedObject, sourceControllerHeroId, board, participants);
            return matches == 0 ? null : modification.WithResolvedAmount(matches);
        }

        private static int CountDerivedMatches(
            SourceValueQueryKind query,
            RuleObject affectedObject,
            HeroId sourceControllerHeroId,
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            switch (query)
            {
                case SourceValueQueryKind.EnemyUnitsAdjacentToAffectedObject:
                    return CountAdjacentEnemyUnits(
                        affectedObject, sourceControllerHeroId, board, participants);
                default:
                    throw new InvalidOperationException(
                        $"No derivation is declared for source value query '{query}'.");
            }
        }

        /// <summary>
        /// p.19's units on the team opposing the source's controller, standing one space from the
        /// affected object. An affected object that is not on the board is beside nobody, which is the
        /// same answer p.16 gives an Ultimate whose hero has left it.
        /// </summary>
        private static int CountAdjacentEnemyUnits(
            RuleObject affectedObject,
            HeroId sourceControllerHeroId,
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            if (participants == null)
            {
                throw new InvalidOperationException(
                    "A derived enemy-unit count needs the participants whose sides it compares.");
            }

            var anchorObject = affectedObject.Kind == RuleObjectKind.Hero
                ? BoardObjectReference.ForHero(affectedObject.HeroId)
                : BoardObjectReference.ForMinion(affectedObject.MinionId);
            if (!board.TryGetObjectCoordinate(anchorObject, out var anchor))
            {
                return 0;
            }

            var count = 0;
            foreach (var occupancy in board.Occupancies)
            {
                var hero = BoardObjectReference.ForHero(occupancy.HeroId);
                if (!hero.Equals(anchorObject) &&
                    HexDistanceQuery.Calculate(anchor, occupancy.Coordinate) == 1 &&
                    TargetQuery.StandsInRelationTo(
                        SourceTeamRelation.OpposingSource, hero, sourceControllerHeroId, participants,
                        board))
                {
                    count++;
                }
            }

            foreach (var minion in board.Minions)
            {
                var unit = BoardObjectReference.ForMinion(minion.MinionId);
                if (!unit.Equals(anchorObject) &&
                    HexDistanceQuery.Calculate(anchor, minion.Coordinate) == 1 &&
                    TargetQuery.StandsInRelationTo(
                        SourceTeamRelation.OpposingSource, unit, sourceControllerHeroId, participants,
                        board))
                {
                    count++;
                }
            }

            return count;
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

                    var resolved = ResolveDerivedAmount(
                        property.Modification, affectedObject, token.ControllerHeroId, board,
                        participants);
                    if (resolved == null)
                    {
                        continue;
                    }

                    yield return AppliedRuleModification.FromContinuousSource(
                        resolved,
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

                // Milestone 8.3. An Ultimate whose printed ability is transcribed but whose composition
                // its own milestone still owes supplies no typed rule, so it contributes nothing here.
                // Contributing something would mean inventing the rule the card is waiting for, and
                // faulting would make an unlocked card unplayable; the card is unlocked and inert until
                // the milestone its declaration names composes it.
                if (ultimate.Rule.Modification == null)
                {
                    continue;
                }

                var affected = RuleObject.ForHero(ultimate.OwnerId);
                var resolved = ResolveDerivedAmount(
                    ultimate.Rule.Modification, affected, ultimate.OwnerId, board, participants);
                if (resolved == null)
                {
                    continue;
                }

                yield return AppliedRuleModification.FromContinuousSource(
                    resolved,
                    affected,
                    ultimate.Source,
                    participants.GetTeamForHero(ultimate.OwnerId).Side,
                    index);
            }
        }
    }
}
