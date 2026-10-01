using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.70. <b>The one owner of "does this bullet of a modal Attack have a legal unit right
    /// now".</b>
    ///
    /// <para>Three different moments ask it and they must not be able to disagree: the modal choice's
    /// own offer (a bullet naming nobody is greyed rather than offered, which is this milestone), the
    /// whole-primary greying that refuses an Attack whose EVERY bullet names nobody
    /// (<see cref="AttackDefenseCardActionStrategyBackend.CanAnyDeclaredBulletFindATarget"/>, milestone
    /// 8.35z27), and the target set the chosen bullet's own p.14 sequence derives when it starts
    /// (<c>StartAttackResolution</c>). Before this milestone the third had its own code and the first
    /// did not exist; the derivation now lives here and all three arrive at it, so the greyed set, the
    /// refused answer and the candidates the decision window draws are one reading asked three
    /// times.</para>
    ///
    /// <para><b>What "the same question" means, exactly.</b> The bullet's own printed sentence - which
    /// units, how far, and whether p.19's straight line is excluded - narrowed by the predicate the
    /// bullet prints about its candidate (milestone 8.35z45's
    /// <see cref="AttackSequenceSubjectNarrowing"/>, including 8.69a's obstacle adjacency, which
    /// reaches p.17's counts-as overlay and therefore Brynn's Over the Top), against p.13's reach with
    /// p.16's items composed onto it, under p.18's immunities and 8.57e's ranged overlay. Nothing is
    /// re-stated here that any of those owners already states.</para>
    ///
    /// <para><b>Only the CHOICE reads the board; the sequence still reads its own.</b> Milestone
    /// 8.35z27 refused to anticipate a bullet because "whether that bullet's sequence will find a unit
    /// is p.14's question, asked when the sequence starts and answered against the board it starts
    /// on". That reasoning is kept and narrowed at the user's direction (BR-Q19h, 2026-09-21): the
    /// modal choice is a decision a player makes NOW, so it is answered against the board as it stands
    /// now, and after the choice nothing changes - a bullet chosen while legal still binds its target
    /// against the board its sequence reaches, and p.10's impossible step covers a board that moved in
    /// between. So this query is asked at the modal and at the offer, and never to second-guess a
    /// sequence that has already started.</para>
    /// </summary>
    public static class AttackSequenceBulletSubjectQuery
    {
        /// <summary>
        /// Whether this bullet's own sentence names a legal unit on the board as it stands.
        /// </summary>
        public static bool HasALegalSubject(
            HeroId attackerId,
            ActionOffer offer,
            AttackSequenceBullet bullet,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            MarkerAttachmentRegister markers,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectRosterState rosterState,
            RuleModificationSet modifications) =>
            FindLegalSubjects(
                attackerId, offer, bullet, definitions, cardInstances, participants, board, spaces,
                markers, heroItems, rosterState, modifications).Count > 0;

        /// <summary>
        /// The same question asked by a caller that holds the bullet's identity rather than an
        /// <see cref="ActionOffer"/> - which is the capability handler standing on the bullet's own
        /// node while the modal is being offered. The offer is built HERE rather than at that call
        /// site, so the address this derivation runs against is composed in one place.
        /// </summary>
        public static bool HasALegalSubject(
            HeroId attackerId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            ActionInvocationContext invocationContext,
            AttackSequenceBullet bullet,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            MarkerAttachmentRegister markers,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectRosterState rosterState,
            RuleModificationSet modifications)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var action = FindAction(definitions, sourceDefinitionReference, actionDefinitionId);
            if (action == null || action.Kind != ActionKind.Attack)
            {
                // Not an Attack, or content this build cannot name: the honest answer is the one the
                // whole-primary gate gives in the same position - undecided, so nothing is greyed.
                return true;
            }

            var offer = new ActionOffer(
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                attackerId,
                // The performance a bullet is chosen inside is the frame's own, and the named absence
                // means this question was asked outside a frame. An offer refuses `None` outright, and
                // p.10's printed primary is what a modal Attack's text is running under whenever
                // nobody said otherwise - it changes nothing this derivation reads.
                invocationContext == ActionInvocationContext.None
                    ? ActionInvocationContext.PrintedPrimary
                    : invocationContext,
                isAvailable: true,
                attackRange: action.AttackRange,
                sourceCardStateTransition: default);

            return HasALegalSubject(
                attackerId, offer, bullet, definitions, cardInstances, participants, board, spaces,
                markers, heroItems, rosterState, modifications);
        }

        /// <summary>
        /// Every unit this bullet's own printed sentence admits on the board as it stands.
        ///
        /// <para>Milestone 8.35z27 wrote this inside the attack backend's own target derivation and
        /// milestone 8.35z45 widened it to carry the bullet's predicate; it moved here unchanged when
        /// a second caller appeared, rather than being written a second time.</para>
        /// </summary>
        public static IReadOnlyList<AttackTarget> FindLegalSubjects(
            HeroId attackerId,
            ActionOffer offer,
            AttackSequenceBullet bullet,
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            MarkerAttachmentRegister markers,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectRosterState rosterState,
            RuleModificationSet modifications)
        {
            if (!bullet.IsDefined)
            {
                throw new ArgumentException(
                    "A modal Attack bullet's subject derivation requires the bullet.", nameof(bullet));
            }

            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            // Milestone 8.57e. The classification the RULES read - the card's own unless a standing
            // declaration says otherwise - asked through the one query the offer, the defence preview
            // and the Block all read.
            var attackRange = RangedAttackOverlayQuery.Classify(
                offer.AttackRange, offer.ActorId, modifications);
            var rangeTotal = attackRange == AttackRangeKind.Ranged
                ? RangeTotal(
                    offer, definitions, participants, board, heroItems, rosterState, modifications,
                    cardInstances)
                : 1;

            // Milestone 8.35z27. A modal Attack's bullets each print their own targeting sentence, so
            // the sequence running this bullet reaches what that bullet says and not what the action's
            // own icons would say. Everything else about the set is unchanged: the same Range Total,
            // the same p.18 immunities, the same query.
            var admitted = AttackTargetQuery.FindLegalEnemyTargets(
                attackerId,
                RangedAttackOverlayQuery.Overlay(
                    bullet.TargetPolicy, offer.AttackRange, offer.ActorId, modifications),
                rangeTotal,
                participants,
                board,
                modifications);

            return NarrowedToTheBulletsSubject(
                admitted, bullet, cardInstances, attackerId, participants, board, markers, spaces,
                modifications);
        }

        /// <summary>
        /// Whether the state this bullet's own sentence reads is present at all.
        ///
        /// <para><b>It exists because the offer walk may not disable what it cannot read.</b>
        /// <see cref="NarrowedToTheBulletsSubject"/> refuses a missing register loudly - admitting
        /// everybody and admitting nobody are a widened offer and a card greyed for nothing, and both
        /// look exactly like a board position - and that refusal is right where a live derivation asks.
        /// The option-feasibility walk is the other kind of caller: milestone 6.5ac's rule there is
        /// that a step the walk cannot judge stays performable, so a composition holding no board, no
        /// cards or no marker register greys nothing rather than throwing at a player. This is that
        /// question asked before the loud one, and it is the only place the two readings meet.</para>
        /// </summary>
        public static bool CanBeAsked(
            AttackSequenceBullet bullet,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            MarkerAttachmentRegister markers,
            IEnumerable<CardInstance> cardInstances,
            RuleModificationSet modifications)
        {
            if (!bullet.IsDefined || participants == null || board == null || modifications == null)
            {
                return false;
            }

            if (!bullet.NarrowsItsSubject)
            {
                return true;
            }

            switch (bullet.SubjectNarrowing)
            {
                case AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles:
                case AttackSequenceSubjectNarrowing.AdjacentToATokenTheActorOwns:
                    return spaces != null;
                case AttackSequenceSubjectNarrowing.HoldsTheActorsMarker:
                    return markers != null;

                // Milestone 8.71c. "Adjacent to your friendly hero", with and without "and not
                // adjacent to you": both read the board and the roster, and this method already
                // refused a call holding neither. So there is nothing further to ask for, and the
                // predicate is always answerable here.
                case AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor:
                case AttackSequenceSubjectNarrowing
                    .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor:
                    return true;
                default:
                    return cardInstances != null;
            }
        }

        /// <summary>
        /// p.13's Range Total for this Attack: the printed icon with p.16's items composed onto it,
        /// plus whatever the roster's own dynamic declarations add to the Range channel.
        ///
        /// <para>It is the derivation the attack backend has always used for its own target set -
        /// <c>AttackDefenseCardActionStrategyBackend.RangeValue</c> now delegates here - so a bullet's
        /// "in range" at the modal, the same bullet's "in range" when its sequence starts, and the
        /// Range the HUD shows are one number.</para>
        /// </summary>
        public static ValueResult RangeValue(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectRosterState rosterState,
            RuleModificationSet modifications,
            // Milestone 9.30e. The cards as they stand, which Sunburst's "+1 Range for each other spell
            // card removed" counts; absent, no card-counting contribution is read - every caller
            // written before that milestone composes exactly the Range it did.
            IEnumerable<CardInstance> cardInstances = null)
        {
            var composed = RangeValueQuery.Calculate(offer, definitions, heroItems, modifications);
            var action = ActionOfferQuery.FindActionDefinition(offer, definitions);
            var roster = RosterDynamicValueQuery.Contributions(
                action,
                offer,
                default,
                PrintedValueChannel.Range,
                definitions,
                cardInstances?.ToArray() ?? Array.Empty<CardInstance>(),
                participants,
                board,
                modifications,
                rosterState,
                0,
                0);
            return roster.Count == 0
                ? composed
                : new ValueResult(
                    PrintedValueChannel.Range,
                    composed.Contributions.Concat(roster));
        }

        private static int RangeTotal(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectRosterState rosterState,
            RuleModificationSet modifications,
            IEnumerable<CardInstance> cardInstances) =>
            RangeValue(
                offer, definitions, participants, board, heroItems, rosterState, modifications,
                cardInstances).Total;

        private static ActionDefinition FindAction(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId) =>
            definitions
                .FirstOrDefault(definition =>
                    definition.Reference.Equals(sourceDefinitionReference))?
                .ActionDefinitions
                .FirstOrDefault(action => action.Id.Equals(actionDefinitionId));

        /// <summary>
        /// Milestone 8.35z45. The bullet's own predicate about the candidate, applied to the set its
        /// printed sentence already admitted.
        ///
        /// <para><b>Applied here rather than inside the target query, and that is the whole design.</b>
        /// <see cref="AttackTargetQuery.FindLegalEnemyTargets"/> answers rulebook p.13's question -
        /// which enemy units this reach and this policy admit, under p.18's immunities - and it is
        /// asked by every Attack in the build. The predicate is one bullet of one card's own extra
        /// sentence, so it narrows the answer rather than changing the question. That also makes the
        /// two orders commute: nothing the predicate removes could have changed an immunity or a
        /// distance.</para>
        ///
        /// <para><b>Heroes only, and by the sentence rather than by convenience.</b> "a hero in range
        /// who has one or more cards in the discard" is a hero clause; a minion owns no cards and has
        /// no pile for it to be about. The bullets that print one also print
        /// <see cref="AttackTargetableUnits.HeroesOnly"/>, so no minion reaches here in practice - but
        /// a minion IS dropped rather than admitted, because a bullet that narrowed by a hero fact and
        /// then let every minion through would be reading the clause as if it were not there.</para>
        ///
        /// <para><b>The absence of the state a predicate reads is loud.</b> A bullet declaring a
        /// predicate that reaches this without cards has been asked a question it cannot answer, and
        /// the two silent answers - admit everybody, or admit nobody - are a widened offer and a card
        /// greyed for nothing. A caller that may not throw asks <see cref="CanBeAsked"/> first.</para>
        /// </summary>
        private static IReadOnlyList<AttackTarget> NarrowedToTheBulletsSubject(
            IReadOnlyList<AttackTarget> admitted,
            AttackSequenceBullet bullet,
            IEnumerable<CardInstance> cardInstances,
            HeroId attackerId,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            MarkerAttachmentRegister markers,
            ITokenPlacementAuthority spaces,
            RuleModificationSet modifications)
        {
            if (!bullet.NarrowsItsSubject)
            {
                return admitted;
            }

            // Milestone 8.71c. "Target a hero (minion) in range, adjacent to your friendly hero
            // [and not adjacent to you]" - Hanu's four graded Reds. Both members say the same thing
            // about the candidate's neighbourhood and nothing about which kind of unit it is: the noun
            // is the bullet's own `AttackTargetableUnits`, which is why one reading serves "a hero in
            // range" and "a minion in range" (HA-Q5).
            if (bullet.SubjectNarrowing ==
                    AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor ||
                bullet.SubjectNarrowing == AttackSequenceSubjectNarrowing
                    .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor)
            {
                if (board == null || participants == null || modifications == null)
                {
                    throw new InvalidOperationException(
                        $"The modal Attack bullet '{bullet.BulletId}' prints " +
                        $"'{bullet.SubjectNarrowing}', which is a predicate about who stands beside " +
                        "the candidate, and this target derivation was given no board, no roster or " +
                        "no live rules to read. Supply all three at the call site rather than letting " +
                        "the narrowing silently admit or silently exclude every candidate.");
                }

                var excludesTheActorsOwnRing = bullet.SubjectNarrowing == AttackSequenceSubjectNarrowing
                    .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor;
                return admitted
                    .Where(target => StandsBesideAFriendlyHeroOfTheActor(
                        target, attackerId, participants, board, modifications,
                        excludesTheActorsOwnRing))
                    .ToArray();
            }

            // Milestone 8.114b. Wuk's Nature's faces, bullet 2: "Target a unit in range adjacent to a
            // Tree token" - a token the attacker owns on one of the candidate's six neighbours.
            if (bullet.SubjectNarrowing == AttackSequenceSubjectNarrowing.AdjacentToATokenTheActorOwns)
            {
                if (spaces == null || board == null)
                {
                    throw new InvalidOperationException(
                        $"The modal Attack bullet '{bullet.BulletId}' prints " +
                        $"'{bullet.SubjectNarrowing}', which is a predicate about the tokens around " +
                        "the candidate, and this target derivation was given no token register or no " +
                        "board to read. Supply both at the call site rather than letting the narrowing " +
                        "silently admit or silently exclude every candidate.");
                }

                var owned = spaces.PlacedTokens
                    .Where(token => token.OwnerHeroId.Equals(attackerId))
                    .Select(token => token.Coordinate)
                    .ToArray();
                return admitted
                    .Where(target => board.TryGetObjectCoordinate(
                            target.Kind == AttackTargetKind.Hero
                                ? BoardObjectReference.ForHero(target.HeroId)
                                : BoardObjectReference.ForMinion(target.MinionId),
                            out var at) &&
                        owned.Any(token => HexDistanceQuery.Calculate(token, at) == 1))
                    .ToArray();
            }

            // Milestone 8.69a. Brynn's Familiar Ground, bullet 2: "Target a hero in range who is
            // adjacent to 3 or more obstacles." It is asked of the one owner of that phrase, so the
            // offer, the target choice and the resolution all see the same candidate set - and so does
            // his Ultimate, whose counts-as overlay that owner consults before it counts anything.
            if (bullet.SubjectNarrowing ==
                AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles)
            {
                if (spaces == null || modifications == null)
                {
                    throw new InvalidOperationException(
                        $"The modal Attack bullet '{bullet.BulletId}' prints " +
                        $"'{bullet.SubjectNarrowing}', which is a predicate about the board around the " +
                        "candidate, and this target derivation was given no board or no live rules to " +
                        "read. Supply both at the call site rather than letting the narrowing " +
                        "silently admit or silently exclude every candidate.");
                }

                return admitted
                    .Where(target => target.Kind == AttackTargetKind.Hero &&
                        ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                            spaces, target.HeroId, modifications, target.HeroBody))
                    .ToArray();
            }

            if (bullet.SubjectNarrowing == AttackSequenceSubjectNarrowing.HoldsTheActorsMarker)
            {
                if (markers == null)
                {
                    throw new InvalidOperationException(
                        $"The modal Attack bullet '{bullet.BulletId}' prints " +
                        $"'{bullet.SubjectNarrowing}', which is a predicate about a component the " +
                        "candidate is wearing, and this target derivation was given no marker " +
                        "register to read. Supply the match's register at the call site rather than " +
                        "letting the narrowing silently admit or silently exclude every candidate.");
                }

                var holders = HeldMarkerQuery.HoldersOfAMarkerDeclaredBy(
                    markers.Attachments, attackerId);
                return admitted
                    .Where(target => target.Kind == AttackTargetKind.Hero &&
                        holders.Any(holder => holder.Equals(target.HeroId)))
                    .ToArray();
            }

            if (cardInstances == null)
            {
                throw new InvalidOperationException(
                    $"The modal Attack bullet '{bullet.BulletId}' prints " +
                    $"'{bullet.SubjectNarrowing}', which is a predicate about the candidate's own " +
                    "cards, and this target derivation was given no card state to read. Supply the " +
                    "match's card instances at the call site rather than letting the narrowing " +
                    "silently admit or silently exclude every candidate.");
            }

            var instances = cardInstances as IReadOnlyList<CardInstance> ?? cardInstances.ToArray();
            return admitted
                .Where(target => AdmitsTheSubject(target, bullet.SubjectNarrowing, instances))
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.71c. Rulebook p.17's Friendly, asked of the candidate's own ring: is one of the
        /// six spaces around this unit held by a hero on the ACTOR's team who is not the actor?
        ///
        /// <para><b>p.17 is read literally and that is HA-Q5.</b> "Friendly: another unit on your team.
        /// It never includes you." So Hanu is excluded from the neighbourhood explicitly - a candidate
        /// standing beside him alone satisfies the bullet printing "adjacent to you" and not this one -
        /// and only a hero counts, because "your friendly hero" is the noun the four Reds print beside
        /// a teammate's minion doing nothing for the clause.</para>
        ///
        /// <para><b>It composes on the one owner rather than walking the board again.</b>
        /// <see cref="RosterPresenceQuery.FindUnitsWithin"/> is where "the units within a disk standing
        /// in this relation to the source" already lives - Sabina's "adjacent to your friendly minion"
        /// asks it with <see cref="RosterUnitKind.Minion"/>, Brynn's radius predicates ask it with
        /// <see cref="SourceTeamRelation.OpposingSource"/> - so this clause's team question and every
        /// other card's cannot come to disagree, which is the discipline the discard members keep with
        /// <c>HeroDiscardPileQuery</c>. p.9 measures a disk inclusively, so a radius of one contains
        /// the candidate's own space; the candidate is excluded as well as the actor, which costs
        /// nothing today (an Attack's candidates are enemies, never the actor's teammates) and keeps
        /// the sentence true of a board where a counts-as overlay made one of them a hero.</para>
        ///
        /// <para><b>The exclusion half is measured from the ACTOR's space</b>, which is who "you" is on
        /// Trusted Sidekick, and it is p.19's innermost ring removed from under the printed Range -
        /// never a second distance of its own. An actor standing nowhere satisfies nothing here,
        /// because a reach measured from no space is not a shorter reach.</para>
        /// </summary>
        private static bool StandsBesideAFriendlyHeroOfTheActor(
            AttackTarget target,
            HeroId attackerId,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            bool excludesTheActorsOwnRing)
        {
            var candidate = AsBoardObject(target);
            if (!candidate.IsDefined ||
                !attackerId.IsDefined ||
                !board.TryGetObjectCoordinate(candidate, out var candidateAt) ||
                !board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(attackerId), out var attackerAt))
            {
                return false;
            }

            if (excludesTheActorsOwnRing &&
                GuardsDigital.Core.Board.HexDistanceQuery.Calculate(attackerAt, candidateAt) <= 1)
            {
                return false;
            }

            return RosterPresenceQuery.FindUnitsWithin(
                candidateAt,
                1,
                RosterUnitKind.Hero,
                SourceTeamRelation.AlliedWithSource,
                attackerId,
                board,
                participants,
                modifications,
                excludedFirst: BoardObjectReference.ForHero(attackerId),
                excludedSecond: candidate).Count > 0;
        }

        /// <summary>The board identity of a bound Attack candidate, of either of p.19's unit kinds.</summary>
        private static BoardObjectReference AsBoardObject(AttackTarget target)
        {
            switch (target.Kind)
            {
                case AttackTargetKind.Hero:
                    return BoardObjectReference.ForHero(target.HeroId);
                case AttackTargetKind.Minion:
                    return BoardObjectReference.ForMinion(target.MinionId);
                default:
                    return default;
            }
        }

        private static bool AdmitsTheSubject(
            AttackTarget target,
            AttackSequenceSubjectNarrowing narrowing,
            IReadOnlyList<CardInstance> instances)
        {
            switch (narrowing)
            {
                case AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard:
                    return target.Kind == AttackTargetKind.Hero &&
                        HeroDiscardPileQuery.HasAnyCard(instances, target.HeroId);
                // Milestone 8.35z53. Whisper's Swift Justice, bullet 1: "Target a hero in range with
                // an empty discard" - the exact negation of the case above, reading the same query so
                // the two cannot come to disagree about one pile.
                case AttackSequenceSubjectNarrowing.HasAnEmptyDiscard:
                    return target.Kind == AttackTargetKind.Hero &&
                        !HeroDiscardPileQuery.HasAnyCard(instances, target.HeroId);
                default:
                    return true;
            }
        }
    }
}
