using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30a. Who the authority is currently waiting on, and nothing about what they were
    /// asked.
    /// </summary>
    /// <remarks>
    /// <para>Every seat receives this, because "we are waiting for Blue" is public and is what a
    /// waiting client has to show. The decision's own content reaches only the seat that owns it.</para>
    /// <para>The owner is read off the authoritative pending object, never inferred: each of the ten
    /// families already records its own chooser, so this derives nothing and restates the authority.</para>
    /// </remarks>
    public readonly struct PendingDecisionOwner
    {
        private PendingDecisionOwner(PendingDecisionFamilyId familyId, HeroId heroId, TeamSide teamSide)
        {
            FamilyId = familyId;
            HeroId = heroId;
            TeamSide = teamSide;
        }

        public static readonly PendingDecisionOwner None =
            new PendingDecisionOwner(default, default, TeamSide.None);

        internal static PendingDecisionOwner ForHero(PendingDecisionFamilyId familyId, HeroId heroId) =>
            new PendingDecisionOwner(familyId, heroId, TeamSide.None);

        internal static PendingDecisionOwner ForTeam(PendingDecisionFamilyId familyId, TeamSide side) =>
            new PendingDecisionOwner(familyId, default, side);

        public PendingDecisionFamilyId FamilyId { get; }

        /// <summary>The one hero that may answer, or undefined for a team-owned decision.</summary>
        public HeroId HeroId { get; }

        /// <summary>The side that may answer, or <c>None</c> for a hero-owned decision.</summary>
        public TeamSide TeamSide { get; }

        public bool IsDefined => FamilyId.IsDefined;

        public bool IsTeamOwned => TeamSide != TeamSide.None;

        public bool IsOwnedBy(MatchSeat seat)
        {
            if (seat == null || !IsDefined)
            {
                return false;
            }

            return IsTeamOwned ? seat.PlaysFor(TeamSide) : seat.Owns(HeroId);
        }
    }

    /// <summary>
    /// Milestone 8.30a. What is publicly countable about one hero's cards without naming any of them.
    /// </summary>
    /// <remarks>
    /// Hand and deck sizes are public at the table - cards are dealt, played and discarded in the
    /// open, so anyone can count them - while the identities behind those counts are not. Publishing
    /// the count and withholding the cards is exactly that distinction, and it is what lets a client
    /// draw an opponent's hand as backs without ever holding a card it may not see.
    /// </remarks>
    public sealed class SeatHandSummary
    {
        internal SeatHandSummary(HeroId heroId, int handCount, int deckCount)
        {
            HeroId = heroId;
            HandCount = handCount;
            DeckCount = deckCount;
        }

        public HeroId HeroId { get; }
        public int HandCount { get; }
        public int DeckCount { get; }
    }

    /// <summary>
    /// Milestone 8.30a. One seat's authorized view of the match.
    /// </summary>
    /// <remarks>
    /// <para>A separate read model rather than a filtered <see cref="MatchSessionProjection"/>, which
    /// is the point of `F-ER2-07`: another seat's hand is <em>absent</em> here, not concealed by a
    /// view that still holds it. Nothing a presentation forgets to hide can leak, because nothing it
    /// could forget is in the object.</para>
    /// <para>Director-only authority is absent for the same structural reason: there is no
    /// accepted-command history, no snapshot, no diagnostic log, no Undo, no restart, and no
    /// Tie-Breaker override on this type or on the client that carries it.</para>
    /// <para>What a seat may actually submit is <see cref="AuthorizedCommands"/>, and the host
    /// refuses anything outside it. The set is derived from the same authoritative state this
    /// projection reports, so a client never decides its own legality and the two can never
    /// disagree.</para>
    /// </remarks>
    public sealed class SeatProjection
    {
        internal SeatProjection(
            MatchSeat seat,
            long version,
            MatchSessionProjection source,
            IReadOnlyList<CardInstance> ownCards,
            IReadOnlyList<CardInstance> publicCards,
            IReadOnlyList<SeatHandSummary> handSummaries,
            PendingDecisionOwner decisionOwner,
            PendingDecisionContract decisionContract,
            IReadOnlyList<DirectorCommand> authorizedCommands)
        {
            Seat = seat;
            Version = version;
            Setup = source.Setup;
            SelectedMap = source.SelectedMap;
            Participants = source.Participants;
            Definitions = source.Definitions;
            Heroes = source.Heroes;
            Minions = source.Minions;
            Obstacles = source.Obstacles;
            PlacedTokens = source.PlacedTokens;
            HeldMarkers = source.HeldMarkers;
            RoundIndex = source.RoundIndex;
            TurnIndex = source.TurnIndex;
            Phase = source.Phase;
            TieBreakerState = source.TieBreakerState;
            TurnSlots = source.TurnSlots;
            CompletedRounds = source.CompletedRounds;
            CurrentEndOfRoundStage = source.CurrentEndOfRoundStage;
            Commitments = source.Commitments;
            SelectedActor = source.SelectedActor;
            IsTurnActive = source.IsTurnActive;
            TeamLifeCounters = source.TeamLifeCounters;
            HeroProgression = source.HeroProgression;
            Outcome = source.Outcome;
            OwnCards = ownCards;
            PublicCards = publicCards;
            HandSummaries = handSummaries;
            DecisionOwner = decisionOwner;
            DecisionContract = decisionContract;
            AuthorizedCommands = authorizedCommands;
        }

        public MatchSeat Seat { get; }

        /// <summary>
        /// The authoritative projection version this view was built from. A submission carries the
        /// version it was decided against, and the host refuses anything else.
        /// </summary>
        public long Version { get; }

        public QuickGameSetupState Setup { get; }
        public BoardMapDefinition SelectedMap { get; }
        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<MinionOccupancy> Minions { get; }
        public IReadOnlyList<ObstacleOccupancy> Obstacles { get; }
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }
        public IReadOnlyList<MarkerAttachment> HeldMarkers { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public TieBreakerState TieBreakerState { get; }
        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }
        public EndOfRoundStage? CurrentEndOfRoundStage { get; }

        /// <summary>
        /// Every hero's commitment status, with the card identity only once it is revealed. The
        /// authority already publishes it this way; a facedown commitment has no identity to hide.
        /// </summary>
        public IReadOnlyList<MatchCommitmentProjection> Commitments { get; }

        public SelectedInitiativeActor SelectedActor { get; }
        public bool IsTurnActive { get; }
        public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters { get; }
        public IReadOnlyList<HeroProgressionProjection> HeroProgression { get; }
        public GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome { get; }

        /// <summary>
        /// Every card this seat's own hero owns, wherever it is - hand, deck, turn area, discard,
        /// item slot or Ultimate area.
        /// </summary>
        public IReadOnlyList<CardInstance> OwnCards { get; }

        /// <summary>
        /// The other heroes' cards whose identity is already public: a revealed commitment, a card
        /// some public projection field already names, or a card in a location the table can read.
        ///
        /// <para>Their hands and decks are absent, and absent together: a client that could see one
        /// opponent deck could subtract it from the printed set and read the hand.</para>
        /// </summary>
        public IReadOnlyList<CardInstance> PublicCards { get; }

        /// <summary>The countable shape of every hero's hidden cards, naming none of them.</summary>
        public IReadOnlyList<SeatHandSummary> HandSummaries { get; }

        /// <summary>Who the authority is waiting on, or <see cref="PendingDecisionOwner.None"/>.</summary>
        public PendingDecisionOwner DecisionOwner { get; }

        /// <summary>
        /// The standing decision's exact answers, and <c>null</c> unless this seat owns it. A seat
        /// waiting on another seat's decision sees <see cref="DecisionOwner"/> and no answers.
        /// </summary>
        public PendingDecisionContract DecisionContract { get; }

        /// <summary>
        /// Every command this seat may submit right now, in a stable order, and empty while it is
        /// this seat's turn to wait.
        /// </summary>
        public IReadOnlyList<DirectorCommand> AuthorizedCommands { get; }

        /// <summary>Whether the authority is waiting on a decision this seat does not own.</summary>
        public bool IsWaitingForAnotherSeat =>
            DecisionOwner.IsDefined && !DecisionOwner.IsOwnedBy(Seat);
    }

    /// <summary>
    /// Milestone 8.30a. Builds one seat's authorized view, and the set of commands that view may
    /// answer with, from the authoritative projection.
    /// </summary>
    /// <remarks>
    /// It is the single derivation for both. The host authorizes a submission against exactly the
    /// set it published, so "the client offered a control the host then refused" is not a state this
    /// design can reach.
    /// </remarks>
    internal static class SeatProjectionFactory
    {
        public static SeatProjection ForSeat(MatchSessionProjection source, MatchSeat seat)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (seat == null) throw new ArgumentNullException(nameof(seat));

            var owner = OwnerOfStandingDecision(source);
            var ownsDecision = owner.IsOwnedBy(seat);

            var ownCards = source.Cards
                .Where(card => card.OwnerId.Equals(seat.HeroId))
                .ToArray();
            var publicCards = source.Cards
                .Where(card => !card.OwnerId.Equals(seat.HeroId))
                .Where(IsPublic)
                .ToArray();
            var handSummaries = source.Participants.Heroes
                .Select(hero => new SeatHandSummary(
                    hero.Id,
                    source.Cards.Count(card => card.OwnerId.Equals(hero.Id) &&
                        card.State.Location == CardLocation.Hand),
                    source.Cards.Count(card => card.OwnerId.Equals(hero.Id) &&
                        card.State.Location == CardLocation.Deck)))
                .ToArray();

            return new SeatProjection(
                seat,
                source.Version,
                source,
                Array.AsReadOnly(ownCards),
                Array.AsReadOnly(publicCards),
                Array.AsReadOnly(handSummaries),
                owner,
                ownsDecision ? source.DecisionContract : null,
                AuthorizedCommands(source, seat, owner));
        }

        /// <summary>
        /// The standing decision's owner, read off whichever authoritative pending object the
        /// contract names.
        /// </summary>
        /// <remarks>
        /// Switching on the contract's own family, rather than testing the pending objects in some
        /// order of this method's choosing, is what keeps the two from disagreeing - and a family
        /// added without an owner rule fails loudly here instead of quietly belonging to nobody.
        /// </remarks>
        public static PendingDecisionOwner OwnerOfStandingDecision(MatchSessionProjection source)
        {
            var contract = source.DecisionContract;
            if (contract == null)
            {
                return PendingDecisionOwner.None;
            }

            var family = contract.FamilyId;
            if (family.Equals(PendingDecisionFamilies.Initiative))
            {
                return PendingDecisionOwner.ForTeam(family, source.PendingInitiativeChoice.OwningSide);
            }

            if (family.Equals(PendingDecisionFamilies.Movement))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingMovementChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.AttackTarget))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingAttackTargetChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.Defense))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingDefenseChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.Effect))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingEffectChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.Correction))
            {
                return PendingDecisionOwner.ForTeam(
                    family, source.PendingDisplacedMinionCorrection.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.MinionBattle))
            {
                return PendingDecisionOwner.ForTeam(
                    family, source.PendingMinionBattleChoice.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.HeroRespawn))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingHeroRespawnChoice.HeroId);
            }

            if (family.Equals(PendingDecisionFamilies.SystemTransition))
            {
                return PendingDecisionOwner.ForTeam(
                    family, source.PendingSystemTransitionStep.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.RemovalPrevention))
            {
                return PendingDecisionOwner.ForHero(
                    family, source.PendingRemovalPrevention.DecidingHeroId);
            }

            throw new InvalidOperationException(
                "Pending decision family '" + family.Value + "' has no seat-ownership rule. Every " +
                "family a projection can publish must name the seat that may answer it, or the " +
                "decision belongs to nobody and no client can be authorized to answer it.");
        }

        /// <summary>
        /// Every command this seat may submit against this exact projection.
        /// </summary>
        /// <remarks>
        /// <para>The order below is the authority's own: a standing decision suspends everything
        /// else, then the end-of-round advance, then the commitment the authority is currently
        /// asking for, then the selected actor's turn actions.</para>
        /// <para><c>AdvanceEndOfRound</c> is the one command with no owner, and deliberately so: it
        /// carries no choice - the choices inside p.8's stages are their own owned families - so it
        /// is a continuation any seat may press rather than a decision anyone owns. Milestone 8.30b
        /// decides how a client presents that; nothing about the match state depends on which seat
        /// sends it.</para>
        /// <para><c>BeginSystemTransition</c> is absent: no surface offers it and no rule gives a
        /// player the choice of starting one.</para>
        /// </remarks>
        public static IReadOnlyList<DirectorCommand> AuthorizedCommands(
            MatchSessionProjection source, MatchSeat seat, PendingDecisionOwner owner)
        {
            if (source.Phase == RoundResolutionPhase.GameEnded)
            {
                return Array.Empty<DirectorCommand>();
            }

            if (source.DecisionContract != null)
            {
                return owner.IsOwnedBy(seat)
                    ? source.DecisionContract.Commands
                    : Array.Empty<DirectorCommand>();
            }

            if (source.Phase == RoundResolutionPhase.EndOfRound)
            {
                return Array.AsReadOnly(new[] { DirectorCommand.AdvanceEndOfRound() });
            }

            if (source.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                return CommitmentCommands(source, seat);
            }

            if (!source.IsTurnActive ||
                !source.SelectedActor.IsDefined ||
                !seat.Owns(source.SelectedActor.ActorId))
            {
                return Array.Empty<DirectorCommand>();
            }

            return TurnActionCommands(source);
        }

        /// <summary>
        /// This seat's own commitment answer, offered to every seat the authority is still waiting
        /// on rather than to one at a time.
        /// </summary>
        /// <remarks>
        /// The Director's own surface asks each hero in turn, which is a serialization of one
        /// question a single controller has to ask somehow. Rulebook p.7 is not serialized: every
        /// player chooses a card facedown at the same time, and the commitment authority already
        /// tracks each hero independently. Six seats are where that difference becomes visible, so
        /// the seat surface authorizes every awaiting seat at once.
        /// </remarks>
        private static IReadOnlyList<DirectorCommand> CommitmentCommands(
            MatchSessionProjection source, MatchSeat seat)
        {
            var asked = source.Commitments.FirstOrDefault(value =>
                value.HeroId.Equals(seat.HeroId) &&
                value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            if (!asked.HeroId.IsDefined)
            {
                return Array.Empty<DirectorCommand>();
            }

            var commands = source.Cards
                .Where(card => card.OwnerId.Equals(asked.HeroId) &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .Select(card => DirectorCommand.CommitCard(asked.HeroId, card.Id))
                .ToList();
            if (asked.CanPass)
            {
                commands.Add(DirectorCommand.Pass(asked.HeroId));
            }

            return Array.AsReadOnly(commands.ToArray());
        }

        private static IReadOnlyList<DirectorCommand> TurnActionCommands(MatchSessionProjection source)
        {
            var commands = new List<DirectorCommand>();
            commands.AddRange(source.AvailableTurnActions
                .Where(offer => offer.IsAvailable)
                .Select(offer => DirectorCommand.StartAction(
                    offer.SourceCardInstanceId, offer.ActionDefinitionId)));
            foreach (var option in source.AvailableFastTravelOptions)
            {
                commands.AddRange(option.LegalDestinations.Select(destination =>
                    DirectorCommand.PerformFastTravel(
                        option.ReplacedMovementActionDefinitionId, destination)));
            }

            foreach (var option in source.AvailableClearOptions)
            {
                commands.AddRange(option.SubsetInvocations);
            }

            // The projected Hold offer rather than a fresh rules query: the authority already decided
            // it, and an application-layer surface that re-derived an offer would be the second
            // answer 8.7 removed.
            if (source.AvailableHoldOffer.HasValue && source.AvailableHoldOffer.Value.IsAvailable)
            {
                commands.Add(DirectorCommand.PerformHold(source.SelectedActor.CardInstanceId));
            }

            return Array.AsReadOnly(commands.ToArray());
        }

        /// <summary>
        /// Whether another hero's card may be named to this seat.
        /// </summary>
        /// <remarks>
        /// <para>This is rulebook p.4's Hidden Information section, restated: a player may always be
        /// asked to read "any of their unresolved cards, resolved cards, and discarded cards, as well
        /// as visible information on their dashboards", while "cards in the deck", "cards in hand"
        /// and "any facedown cards" are hidden. Both halves are needed - a facedown card is hidden
        /// wherever it lies, which p.7's rule that a resolved or discarded card can be facedown makes
        /// a real case and not a hypothetical one.</para>
        /// <para>The switch is fail-closed on purpose: a location this rule has not considered
        /// withholds the card rather than publishing it, so adding one is a visible gap in a client
        /// rather than a silent leak.</para>
        /// <para>Hand and deck are withheld <b>together</b>. A hero's printed deck is public
        /// knowledge, so a client that could read an opponent's deck could subtract it from the
        /// printed set and read the hand. What remains derivable is the union of the two, which is
        /// inherent to a known printed deck and is not something a projection can take back.</para>
        /// </remarks>
        private static bool IsPublic(CardInstance card)
        {
            if (card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return false;
            }

            switch (card.State.Location)
            {
                case CardLocation.TurnArea:
                case CardLocation.DiscardPile:
                case CardLocation.ItemSlot:
                case CardLocation.UltimateArea:
                    return true;
                default:
                    return false;
            }
        }
    }
}
