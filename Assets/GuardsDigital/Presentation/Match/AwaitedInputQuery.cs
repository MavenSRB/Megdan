using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Whose input the match is waiting on, asked of the projection rather than of what a surface
    /// happens to be drawing.
    ///
    /// <para>The user's requirement is one predicate at three scopes - "is it waiting on ME" for the
    /// top bar, "who is it waiting on" for the hero trays, and "what do I answer with" for the cards -
    /// so it is derived once here and read by each surface rather than restated per surface. This
    /// milestone owns the viewer scope; the other two read the same derivation.</para>
    ///
    /// <para><b>Why it is not the reading it replaces.</b> The narration lane's warm voice was a
    /// fifteen-way disjunction over the controls the round view model happened to expose, which is a
    /// good approximation and has one real hole: a commitment is not one of the declared pending
    /// decision families and exposes no round-action control, because the hand owns it. So the most
    /// common wait in the game - every player choosing a card at the top of a round - reached the
    /// player as the calm voice. Reading the projection answers that arm directly.</para>
    ///
    /// <para><b>Why the offer set is a scoped fact and not control presence.</b>
    /// <see cref="IMatchReadModel.AvailableTurnActions"/> and
    /// <see cref="IMatchReadModel.AvailableHoldOffer"/> are published to a seat only while
    /// <c>isActor &amp;&amp; !standing</c> - the Application layer's own scoping - so a non-empty
    /// offer set already means "this seat's turn, with no decision standing anywhere". That is what
    /// keeps an attacker's bar from lighting while the defender is the one being asked: the attacker
    /// is still the selected actor, but their offers are withheld for exactly that reason, and
    /// <see cref="IMatchReadModel.SelectedActor"/> alone could not tell the two apart.</para>
    /// </summary>
    public static class AwaitedInputQuery
    {
        /// <summary>
        /// Whether the match is waiting on input from the viewer this projection was built for.
        ///
        /// <para><paramref name="viewerHeroId"/> is the seat's own hero, or the undefined hero for
        /// the Director, who answers for everyone. Every other fact read here is already scoped to
        /// the viewer by the projection itself, which is why the hero is needed for the commitment
        /// arm alone: commitments are public, so the viewer's own has to be named.</para>
        /// </summary>
        public static bool IsWaitingOnViewer(IMatchReadModel projection, HeroId viewerHeroId)
        {
            if (projection == null || projection.Phase == RoundResolutionPhase.GameEnded)
            {
                // An ended match is a terminal state rather than another wait, the reading milestone
                // 8.30c gave the status line for the same reason.
                return false;
            }

            return OwnsAStandingDecision(projection) ||
                OwesACommitment(projection, viewerHeroId) ||
                MayTakeATurnAction(projection);
        }

        /// <summary>
        /// A standing decision this viewer may answer.
        ///
        /// <para>A seat projection publishes the contract only to the seat that owns it - "null unless
        /// this seat owns it; a seat waiting on another seat's decision sees the owner and no
        /// answers" - and the Director may answer every family, so one read is honest at both
        /// compositions. It covers every declared pending family at once, which is what keeps a
        /// twelfth from having to be added here by hand.</para>
        /// </summary>
        private static bool OwnsAStandingDecision(IMatchReadModel projection) =>
            projection.DecisionContract != null;

        /// <summary>
        /// Rulebook p.7's commitment, still owed by the viewer. This is the arm the control-presence
        /// reading could not see.
        /// </summary>
        private static bool OwesACommitment(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var commitments = projection.Commitments;
            if (commitments == null || commitments.Count == 0)
            {
                return false;
            }

            if (!viewerHeroId.IsDefined)
            {
                // The Director commits for every seat, so any outstanding commitment is one it owes.
                return commitments.Any(
                    commitment => commitment.Status == TurnCardCommitmentStatus.AwaitingResponse);
            }

            foreach (var commitment in commitments)
            {
                if (commitment.HeroId.Equals(viewerHeroId))
                {
                    return commitment.Status == TurnCardCommitmentStatus.AwaitingResponse;
                }
            }

            return false;
        }

        /// <summary>
        /// The viewer's own turn, with the match accepting an action from them. Both readings are the
        /// projection's own scoped offer set rather than a re-derivation of whose turn it is; an
        /// offer that exists but is unavailable is not a wait, which is why availability is asked.
        ///
        /// <para>Milestone 8.35n (the FIFTH amendment, narration): made <c>public</c> rather than
        /// duplicated. <c>RoundActionPresenter.FormatStatus</c> reuses this exact reading to decide
        /// whether the bare "acting" fallback belongs to the viewer's own rich "YOUR TURN" narration
        /// or to the public hero-name line every other viewer reads - the same offer-scoped fact that
        /// keeps the top bar's gold from lighting on the wrong seat now keeps the narration from
        /// claiming the wrong seat's turn.</para>
        /// </summary>
        public static bool MayTakeATurnAction(IMatchReadModel projection)
        {
            var hold = projection.AvailableHoldOffer;
            if (hold.HasValue && hold.Value.IsAvailable)
            {
                return true;
            }

            var actions = projection.AvailableTurnActions;
            return actions != null && actions.Any(action => action.IsAvailable);
        }

        /// <summary>
        /// Milestone 8.35m (the SEVENTH amendment, hero scope). Whether the match is currently waiting
        /// on <paramref name="heroId"/>'s own input - the same question <see cref="IsWaitingOnViewer"/>
        /// asks of the viewer, asked instead of one named hero, so every hero's tray can read one shine
        /// answer rather than each surface guessing at "whose turn is it" a second way.
        ///
        /// <para><b>The event-channel follow-up (milestone 8.35z7) completes the four arms 8.35m
        /// deferred.</b> The gap that milestone recorded - <see cref="OwnsAStandingDecision"/> and
        /// <see cref="MayTakeATurnAction"/> both read facts <see cref="IMatchReadModel"/> scoped to the
        /// seat that owns them, so nothing on the interface named WHICH hero a non-viewer's decision or
        /// offer belonged to - is closed now that <see cref="IMatchReadModel.DecisionOwner"/> (milestone
        /// 8.35r) publishes exactly that fact unscoped: table-public by design, naming who is awaited
        /// while <see cref="IMatchReadModel.DecisionContract"/> - what they may choose - stays owner-
        /// private. Extending this method to read it is therefore no longer a read-model addition;
        /// it is reading one that already shipped.</para>
        ///
        /// <para>Seven arms in total now. Three are unchanged from 8.35m: the commitment a hero owes
        /// (public, per-hero, the same fact <see cref="OwesACommitment"/> reads for the viewer), the
        /// turn release's named owner (unscoped by 8.35g's own deliberate decision, read directly off
        /// <see cref="IMatchReadModel.PendingTurnRelease"/> rather than through <c>DecisionOwner</c> -
        /// it was already reachable and stays that way), and a standing team agreement's participants
        /// who have not yet answered. Four are new, each read through <c>DecisionOwner</c>: the
        /// leveling hero or heroes - <see cref="OwesALevelingAnswer"/>, plural since milestone 8.35u's
        /// simultaneous leveling, because <c>DecisionOwner.HeroIds</c> already carries every awaited
        /// hero at once and <see cref="IsHeroOwnedFamily"/> tests membership rather than the single
        /// first-named hero; the defense answerer - <see cref="OwesADefenseAnswer"/>; the respawn
        /// chooser - <see cref="OwesARespawnAnswer"/>; and the acting hero's own turn, corrected for
        /// the exact bug 8.35m's doc comment named and left unsolved - <see
        /// cref="IsActingWithNoDecisionStandingOnTop"/> - a naive <see cref="IMatchReadModel.
        /// SelectedActor"/> read stays true while an attacker's Attack is still being asked of the
        /// DEFENDER, so this arm shines the actor only while <c>DecisionOwner</c> names nobody else -
        /// "better a tray that stays dark than one that shines on the wrong hero" no longer needs to
        /// mean "dark", now that the owner fact exists to tell the two apart.</para>
        /// </summary>
        public static bool IsHeroAwaited(IMatchReadModel projection, HeroId heroId)
        {
            if (projection == null || !heroId.IsDefined ||
                projection.Phase == RoundResolutionPhase.GameEnded)
            {
                return false;
            }

            return OwesACommitment(projection, heroId) ||
                IsTurnReleaseOwner(projection, heroId) ||
                OwesAnAgreementAnswer(projection, heroId) ||
                OwesALevelingAnswer(projection, heroId) ||
                OwesADefenseAnswer(projection, heroId) ||
                OwesARespawnAnswer(projection, heroId) ||
                IsActingWithNoDecisionStandingOnTop(projection, heroId);
        }

        /// <summary>
        /// Milestone 8.35z7. Rulebook p.15's leveling, simultaneous since 8.35u: every hero the standing
        /// decision's owner names, not only the first, per <see cref="PendingDecisionOwner.HeroIds"/>'s
        /// own doc comment - "true for each of them at once".
        /// </summary>
        private static bool OwesALevelingAnswer(IMatchReadModel projection, HeroId heroId) =>
            IsHeroOwnedFamily(projection, PendingDecisionFamilies.SystemTransition, heroId);

        /// <summary>Milestone 8.35z7. Whether <paramref name="heroId"/> is the awaited defense answer.</summary>
        private static bool OwesADefenseAnswer(IMatchReadModel projection, HeroId heroId) =>
            IsHeroOwnedFamily(projection, PendingDecisionFamilies.Defense, heroId);

        /// <summary>Milestone 8.35z7. Whether <paramref name="heroId"/> is the awaited respawn chooser.</summary>
        private static bool OwesARespawnAnswer(IMatchReadModel projection, HeroId heroId) =>
            IsHeroOwnedFamily(projection, PendingDecisionFamilies.HeroRespawn, heroId);

        /// <summary>
        /// Whether <see cref="IMatchReadModel.DecisionOwner"/> stands on <paramref name="familyId"/>
        /// and names <paramref name="heroId"/> among its hero-owned awaited heroes. Team-owned decisions
        /// (an Initiative tie, a Correction, a Minion Battle) never match here - <see
        /// cref="PendingDecisionOwner.IsTeamOwned"/> names no single hero, by design, and a family's own
        /// identity keeps this reading from ever crossing into another family's owner.
        /// </summary>
        private static bool IsHeroOwnedFamily(
            IMatchReadModel projection, PendingDecisionFamilyId familyId, HeroId heroId)
        {
            var owner = projection.DecisionOwner;
            return owner.IsDefined && !owner.IsTeamOwned && owner.FamilyId.Equals(familyId) &&
                owner.HeroIds.Any(candidate => candidate.Equals(heroId));
        }

        /// <summary>
        /// Milestone 8.35z7. The acting hero's own turn - <see cref="IMatchReadModel.SelectedActor"/> and
        /// <see cref="IMatchReadModel.IsTurnActive"/> are both public and unscoped, the same pair
        /// <c>MatchFeaturePresenters.IsViewerTheActingHero</c>'s own remarks note reaching every viewer
        /// - corrected for the bug this method's own doc comment names: a defender's standing Defense
        /// answer (or any other
        /// hero's standing decision produced by the actor's own turn) must NOT shine the actor's tray,
        /// because the actor is no longer who is being asked. <see cref="PendingDecisionOwner.None"/> -
        /// nothing stands - and a decision this same hero owns both count as "nothing else is standing
        /// on top of it"; a decision naming anyone else does not.
        /// </summary>
        private static bool IsActingWithNoDecisionStandingOnTop(IMatchReadModel projection, HeroId heroId)
        {
            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined ||
                !projection.SelectedActor.ActorId.Equals(heroId))
            {
                return false;
            }

            var owner = projection.DecisionOwner;
            return !owner.IsDefined || (!owner.IsTeamOwned && owner.HeroIds.Any(heroId.Equals));
        }

        /// <summary>
        /// Milestone 8.35g's release, named directly by its own <c>ActorId</c> rather than through
        /// <c>SeatProjection.DecisionOwner</c> - the one standing-decision family whose owner already
        /// reaches <see cref="IMatchReadModel"/> unscoped, because the release stands after every other
        /// pending fact has cleared and the top bar needed to name it without a twelfth family.
        /// </summary>
        private static bool IsTurnReleaseOwner(IMatchReadModel projection, HeroId heroId) =>
            projection.PendingTurnRelease != null &&
            projection.PendingTurnRelease.ActorId.Equals(heroId);

        /// <summary>
        /// Whether <paramref name="heroId"/> is a participant in the standing public team agreement
        /// (tied Initiative or Minion Battle) who has not yet given their answer. Read from
        /// <see cref="IMatchReadModel.TeamAgreement"/>'s own participant list rather than from
        /// <see cref="OwnsAStandingDecision"/>, because both agreement kinds are also declared pending-
        /// decision families and <see cref="PendingDecisionContract"/> stays non-null for every
        /// teammate on the choosing side for as long as the proposal stands - including one who has
        /// already answered and could still revise, per <c>MatchFeaturePresenters.
        /// TeamAgreementHudPresentation</c>'s own AGREE/WITHDRAW toggle. That would shine every
        /// teammate's tray until the last holdout answers, which is not "still owed a response" - the
        /// same distinction the commitment arm already draws between a hero who has committed and one
        /// who has not. Which field answers "has this participant already given their part" depends on
        /// the kind, exactly as <c>TeamAgreementHudPresentation.Create</c> already reads it: a stated
        /// Initiative preference, or an explicit Minion Battle agreement.
        /// </summary>
        private static bool OwesAnAgreementAnswer(IMatchReadModel projection, HeroId heroId)
        {
            var agreement = projection.TeamAgreement;
            if (agreement == null)
            {
                return false;
            }

            foreach (var participant in agreement.Participants)
            {
                if (!participant.HeroId.Equals(heroId))
                {
                    continue;
                }

                return agreement.Kind == TeamAgreementKind.Initiative
                    ? !participant.HasStatement
                    : !participant.Agreed;
            }

            return false;
        }
    }
}
