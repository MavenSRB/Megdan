using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Presentation.Match
{

    public sealed class DirectorHandViewModel
    {
        public DirectorHandViewModel(
            TeamSide teamSide,
            HeroId heroId,
            string heroDisplayName,
            TurnCardCommitmentStatus commitmentStatus,
            bool canPass,
            CardInstanceId? revealedCardInstanceId,
            CardInstance revealedCard,
            IEnumerable<CardInstance> cards,
            IEnumerable<CardDefinition> definitions,
            CardInstanceId? selectedCardInstanceId,
            IEnumerable<TrayCardViewModel> trayCards = null,
            CardInstanceId? selectedSecondCardInstanceId = null,
            bool canCommitTwoCards = false, bool isReadOnly = false)
        {
            IsReadOnly = isReadOnly;
            TrayCards = Array.AsReadOnly((trayCards ??
                Enumerable.Empty<TrayCardViewModel>()).ToArray());
            PlayableTrayCards = Array.AsReadOnly(TrayCards
                .Where(card => card.Group == TrayCardGroup.Available || card.IsResolving)
                .ToArray());
            UnavailableTrayCards = Array.AsReadOnly(TrayCards
                .Where(card => card.Group != TrayCardGroup.Available && !card.IsResolving)
                .ToArray());
            TeamSide = teamSide;
            HeroId = heroId;
            HeroDisplayName = heroDisplayName ?? throw new ArgumentNullException(nameof(heroDisplayName));
            CommitmentStatus = commitmentStatus;
            CanPass = canPass;
            RevealedCardInstanceId = revealedCardInstanceId;
            RevealedCard = revealedCard;
            Cards = Array.AsReadOnly((cards ?? throw new ArgumentNullException(nameof(cards))).ToArray());
            Definitions = Array.AsReadOnly((definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            SelectedCardInstanceId = selectedCardInstanceId;
            SelectedSecondCardInstanceId = selectedSecondCardInstanceId;
            CanCommitTwoCards = canCommitTwoCards;
            HeaderLabel = teamSide == TeamSide.Red
                ? $"[R] RED {HeroDisplayName.ToUpperInvariant()}"
                : $"[B] BLUE {HeroDisplayName.ToUpperInvariant()}";
            CommitmentLabel = FormatCommitmentLabel();
            var labels = new Dictionary<CardInstanceId, string>();
            foreach (var card in Cards)
            {
                var definition = Definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
                labels.Add(card.Id, $"{definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}");
            }

            CardButtonLabels = new ReadOnlyDictionary<CardInstanceId, string>(labels);
        }

        public TeamSide TeamSide { get; }
        public HeroId HeroId { get; }
        public string HeroDisplayName { get; }
        public TurnCardCommitmentStatus CommitmentStatus { get; }
        public bool IsReadOnly { get; }
        public bool CanPass { get; }
        public CardInstanceId? RevealedCardInstanceId { get; }
        public CardInstance RevealedCard { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public CardInstanceId? SelectedCardInstanceId { get; }
        public CardInstanceId? SelectedSecondCardInstanceId { get; }
        public bool CanCommitTwoCards { get; }
        public string HeaderLabel { get; }
        public string CommitmentLabel { get; }
        public IReadOnlyDictionary<CardInstanceId, string> CardButtonLabels { get; }

        /// <summary>
        /// Milestone 7.8a: this hero's whole current round, left to right - already played cards in
        /// ascending turn order, then the ones still available, then the discarded ones.
        ///
        /// It is a superset of <see cref="Cards"/>, which is the hand alone and remains what a
        /// commitment is chosen from. The extra entries carry no command; they are how a player reads
        /// what this hero has already spent the round on.
        /// </summary>
        public IReadOnlyList<TrayCardViewModel> TrayCards { get; }

        /// <summary>
        /// The bottom hand's projection-driven composition: every card still in the hero's hand plus
        /// the one card whose printed action is resolving. Temporary action-resolution exclusivity
        /// removes commands, not cards from this stationary hand surface.
        /// </summary>
        public IReadOnlyList<TrayCardViewModel> PlayableTrayCards { get; }

        /// <summary>
        /// Genuinely unavailable current-round cards: played or discarded cards other than the card
        /// currently resolving. Their lifecycle, identity and relative order remain the ones
        /// <see cref="TrayCards"/> owns; temporary command exclusivity never moves a held card here.
        /// </summary>
        public IReadOnlyList<TrayCardViewModel> UnavailableTrayCards { get; }

        private string FormatCommitmentLabel()
        {
            switch (CommitmentStatus)
            {
                case TurnCardCommitmentStatus.AwaitingResponse:
                    return "AWAITING CARD";
                case TurnCardCommitmentStatus.CardCommittedFacedown:
                    return "FACEDOWN COMMITTED";
                case TurnCardCommitmentStatus.Passed:
                    return "PASS - NO CARD";
                case TurnCardCommitmentStatus.Revealed:
                    if (RevealedCard != null)
                    {
                        var definition = Definitions.Single(value =>
                            value.Reference.Equals(RevealedCard.DefinitionReference));
                        return $"REVEALED: {definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}";
                    }

                    return "REVEALED";
                default:
                    return CommitmentStatus.ToString().ToUpperInvariant();
            }
        }
    }

    public sealed class DirectorHandsViewModel
    {
        public DirectorHandsViewModel(
            long version,
            IEnumerable<DirectorHandViewModel> hands,
            HeroId viewedHeroId = default,
            DirectorHandViewModel decidedForHand = null,
            bool viewedHandDrawsActionControls = true)
        {
            Version = version;
            Hands = Array.AsReadOnly((hands ?? throw new ArgumentNullException(nameof(hands))).ToArray());
            ViewedHeroId = viewedHeroId;
            ViewedHand = Hands.FirstOrDefault(hand => hand.HeroId.Equals(viewedHeroId)) ??
                Hands.FirstOrDefault();
            DecidedForHand = decidedForHand;
            ViewedHandDrawsActionControls = viewedHandDrawsActionControls;
        }

        public long Version { get; }
        public IReadOnlyList<DirectorHandViewModel> Hands { get; }

        /// <summary>
        /// Milestone 7.8a: the hero whose tray is on screen. Local presentation state - it submits no
        /// command, is not serialized, and is not part of any authoritative record.
        /// </summary>
        public HeroId ViewedHeroId { get; }

        /// <summary>
        /// That hero's hand, or the first one when the viewed hero names none. One tray is drawn, never
        /// a stack of every hand at once.
        /// </summary>
        public DirectorHandViewModel ViewedHand { get; }

        /// <summary>
        /// Milestone 8.72. The hand of a hero whose action THIS viewer is deciding - Hanu the
        /// Trickster's The Ultimate Trick - carrying exactly one card: that hero's revealed,
        /// resolving card, the one the host authorizes this seat to act off. <c>null</c> whenever no
        /// delegation stands over this viewer, which is every board in every earlier milestone.
        ///
        /// <para>It is a hand OF THE OTHER HERO and is deliberately not merged into
        /// <see cref="ViewedHand"/>: the card is not the viewer's, was never in their hand, and is
        /// drawn in a slot of its own that names whose it is. Only the one revealed card is here -
        /// the delegated hero's own hand stays private, exactly as a seat projection already keeps
        /// it, and nothing in this model could name a card the viewer's projection does not carry.</para>
        /// </summary>
        public DirectorHandViewModel DecidedForHand { get; }

        /// <summary>
        /// Milestone 8.72. Whether the VIEWED hand's resolving card draws its printed-action
        /// controls - true unless somebody else is deciding this turn's action, in which case the
        /// card is this player's and the controls are not.
        /// </summary>
        public bool ViewedHandDrawsActionControls { get; }
    }

    public interface IDirectorHandsView
    {
        void Render(DirectorHandsViewModel model);
    }

    public sealed class DirectorHandsPresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly IDirectorHandsView view;
        private HeroId homeHeroId;
        private CardInstanceId? selectedCardInstanceId;
        private CardInstanceId? selectedSecondCardInstanceId;
        private IMatchReadModel lastProjection;
        private CardInstanceId? lastSelectedCardInstanceId;
        private CardInstanceId? lastSelectedSecondCardInstanceId;
        private DirectorHandsViewModel lastViewModel;
        private HeroId pinnedHeroId;
        private HeroId pinnedAgainstAwaitedHeroId;
        private HeroId lastViewedHeroId;
        private bool useTrustedLocalHeroPerspective;


        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor every existing fixture and journey uses,
        /// composing the Director's own client. A seat-scoped surface passes its client instead.
        /// </summary>
        public DirectorHandsPresenter(IMatchSession session, IDirectorHandsView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public DirectorHandsPresenter(IMatchClient session, IDirectorHandsView view)
            : this(session, view, default)
        {
        }

        /// <summary>
        /// Milestone 8.30c. A seat-scoped composition names the hero whose private tray is home for
        /// that player. The Director leaves it undefined and keeps following the currently awaited
        /// hero exactly as before. This is presentation-local routing only: the hero must already
        /// belong to the supplied read model's setup, and it grants no command or card identity.
        /// </summary>
        public DirectorHandsPresenter(
            IMatchClient session, IDirectorHandsView view, HeroId homeHeroId)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            if (homeHeroId.IsDefined && !session.Projection.Setup.Heroes.Any(hero =>
                    hero.HeroId.Equals(homeHeroId)))
            {
                throw new ArgumentException(
                    "A home tray must name a hero in the supplied match.", nameof(homeHeroId));
            }

            this.homeHeroId = homeHeroId;
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;
        /// <summary>
        /// The unseated local host is trusted to inspect every hero as that hero sees their own
        /// current cards. This is presentation-only: the resulting hand is read-only and grants no
        /// card command. Remote and seated clients keep using their scoped projection unchanged.
        /// </summary>
        public bool UseTrustedLocalHeroPerspective
        {
            get => useTrustedLocalHeroPerspective;
            set
            {
                if (useTrustedLocalHeroPerspective == value) return;
                useTrustedLocalHeroPerspective = value;
                lastProjection = null;
                lastViewModel = null;
                Refresh();
            }
        }


        /// <summary>
        /// The hero whose tray is currently drawn (milestone 7.8a).
        ///
        /// For the Director it defaults to the hero the match is waiting on - the one still owing a
        /// commitment, or the one resolving a turn. For a seat it defaults to that seat's home hero,
        /// whose private hand is the one the projection actually carries. Only the Director can pin
        /// another hero, until the awaited hero changes. A player seat always keeps its own tray.
        ///
        /// It is local presentation state throughout. Nothing here submits a command or reads back into
        /// authority, which is why <see cref="SelectViewedHero"/> can be answered from a board click that
        /// the authority itself refused.
        /// </summary>
        public HeroId ViewedHeroId => Resolve(session.Projection);

        /// <summary>The hero the current projection is waiting on, or the first configured hero.</summary>
        public HeroId AwaitedHeroId => Awaited(session.Projection);

        /// <summary>
        /// Points the tray at one hero. Answers false for a hero this match does not have, so a stale
        /// board identity changes nothing.
        /// </summary>
        public bool SelectViewedHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined || (!session.IsDirector && !CanInspect(projection, heroId)) ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            selectedCardInstanceId = null;
            selectedSecondCardInstanceId = null;
            pinnedHeroId = heroId;
            pinnedAgainstAwaitedHeroId = Awaited(projection);
            Refresh();
            return true;
        }

        /// <summary>
        /// Binds the tray's durable home after the interactive lobby assigns this runtime its
        /// admitted online seat. Any Director-era pin or card selection is discarded before the
        /// network projection is rendered, so a later awaited-hero change returns to this hero.
        /// </summary>
        public bool SetHomeHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            homeHeroId = heroId;
            pinnedHeroId = default;
            pinnedAgainstAwaitedHeroId = default;
            selectedCardInstanceId = null;
            selectedSecondCardInstanceId = null;
            lastProjection = null;
            Refresh();
            return true;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => homeHeroId;

        private bool CanInspect(IMatchReadModel projection, HeroId heroId)
        {
            if (heroId.Equals(homeHeroId)) return true;
            if (!(projection is IExperimentalReadModel experimental) || !experimental.Experiments.TeammateCoordination) return false;
            var home = projection.Setup.Heroes.FirstOrDefault(h => h.HeroId.Equals(homeHeroId));
            return home != null && projection.Setup.Heroes.Any(h => h.HeroId.Equals(heroId) && h.TeamSide == home.TeamSide);
        }
        private IEnumerable<CardInstance> DisplayCards(IMatchReadModel projection, HeroId heroId) =>
            !session.IsDirector && !heroId.Equals(homeHeroId) && projection is IExperimentalReadModel experimental
                ? experimental.AlliedHandCards.Where(c => c.OwnerId.Equals(heroId))
                : projection.Cards.Where(c => c.OwnerId.Equals(heroId));

        private HeroId Resolve(IMatchReadModel projection)
        {
            if (!session.IsDirector)
            {
                if (pinnedHeroId.IsDefined && CanInspect(projection, pinnedHeroId)) return pinnedHeroId;
                pinnedHeroId = default;
                pinnedAgainstAwaitedHeroId = default;
                return homeHeroId;
            }

            var awaited = Awaited(projection);
            if (pinnedHeroId.IsDefined && pinnedAgainstAwaitedHeroId.Equals(awaited))
            {
                return pinnedHeroId;
            }

            pinnedHeroId = default;
            pinnedAgainstAwaitedHeroId = default;
            return homeHeroId.IsDefined ? homeHeroId : awaited;
        }

        private static HeroId Awaited(IMatchReadModel projection)
        {
            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var owing = projection.Setup.Heroes
                    .OrderBy(hero => hero.TeamSide)
                    .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                    .FirstOrDefault(hero => projection.GetCommitment(hero.HeroId).Status ==
                        TurnCardCommitmentStatus.AwaitingResponse);
                if (owing != null)
                {
                    return owing.HeroId;
                }
            }

            if (projection.SelectedActor.IsDefined)
            {
                return projection.SelectedActor.ActorId;
            }

            var first = projection.Setup.Heroes
                .OrderBy(hero => hero.TeamSide)
                .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            return first?.HeroId ?? default;
        }

        private long lastCoordinationRevision = -1;
        public void Refresh()
        {
            var projection = session.Projection;
            var coordinationRevision = (projection as IExperimentalReadModel)?.CoordinationRevision ?? 0;
            if (!projection.IsInputEnabled ||
                selectedCardInstanceId.HasValue && !projection.Cards.Any(card =>
                card.Id.Equals(selectedCardInstanceId.Value) && card.State.Location == CardLocation.Hand))
            {
                selectedCardInstanceId = null;
                selectedSecondCardInstanceId = null;
            }

            if (selectedSecondCardInstanceId.HasValue && !projection.Cards.Any(card =>
                    card.Id.Equals(selectedSecondCardInstanceId.Value) &&
                    card.State.Location == CardLocation.Hand))
                selectedSecondCardInstanceId = null;

            var viewedHeroId = Resolve(projection);
            if (coordinationRevision != lastCoordinationRevision || !ReferenceEquals(projection, lastProjection) ||
                !Nullable.Equals(selectedCardInstanceId, lastSelectedCardInstanceId) ||
                !Nullable.Equals(selectedSecondCardInstanceId, lastSelectedSecondCardInstanceId) ||
                !viewedHeroId.Equals(lastViewedHeroId) ||
                lastViewModel == null)
            {
                var hands = projection.Setup.Heroes
                    // An unbound player renders no fallback hand; binding supplies the admitted hero.
                    .Where(hero => session.IsDirector || CanInspect(projection, hero.HeroId))
                    .OrderBy(hero => hero.TeamSide)
                    .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                    .Select(hero =>
                    {
                        var commitment = projection.GetCommitment(hero.HeroId);
                        var displayCards = DisplayCards(projection, hero.HeroId).ToArray();
                        var trustedInspection = useTrustedLocalHeroPerspective && session.IsDirector
                            ? HeroCardInspection.ForHero(projection, hero.HeroId)
                            : null;
                        var handCards = trustedInspection == null
                            ? displayCards.Where(card => card.State.Location == CardLocation.Hand)
                            : displayCards.Where(card =>
                                trustedInspection.HandCards.Contains(card.DefinitionReference));
                        var readOnly = useTrustedLocalHeroPerspective ||
                            !session.IsDirector && !hero.HeroId.Equals(homeHeroId);
                        return new DirectorHandViewModel(
                            hero.TeamSide,
                            hero.HeroId,
                            hero.DisplayName,
                            commitment.Status,
                            !readOnly && projection.IsInputEnabled && commitment.CanPass &&
                                (session.IsDirector || hero.HeroId.Equals(homeHeroId)),
                            commitment.RevealedCardInstanceId,
                            // Milestone 8.35e. `Single` here failed the whole tray refresh, and the
                            // absence it refused is legitimate for any projection that withholds a
                            // named card. The current seat projection retains a retrieved card when
                            // the bounded public log already revealed its exact identity, but this
                            // remains tolerant so a presentation read cannot turn a privacy omission
                            // or older projection into a whole-tray failure.
                            commitment.RevealedCardInstanceId.HasValue
                                ? projection.Cards.SingleOrDefault(card =>
                                    card.Id.Equals(commitment.RevealedCardInstanceId.Value))
                                : null,
                            handCards,
                            projection.Definitions,
                            selectedCardInstanceId,
                            BuildTray(projection, hero.HeroId, selectedCardInstanceId,
                                selectedSecondCardInstanceId: selectedSecondCardInstanceId,
                                displayCards: displayCards, readOnly: readOnly,
                                trustedInspection: trustedInspection),
                            selectedSecondCardInstanceId,
                            !readOnly && commitment.CanCommitTwoCards &&
                                (session.IsDirector || hero.HeroId.Equals(homeHeroId)),
                            readOnly);
                    });
                lastCoordinationRevision = coordinationRevision;
                lastProjection = projection;
                lastSelectedCardInstanceId = selectedCardInstanceId;
                lastSelectedSecondCardInstanceId = selectedSecondCardInstanceId;
                lastViewedHeroId = viewedHeroId;
                lastViewModel = new DirectorHandsViewModel(
                    projection.Version,
                    hands,
                    viewedHeroId,
                    BuildDecidedForHand(projection, viewedHeroId),
                    DelegatedActionPresentation.DrawsTheActionControls(projection, viewedHeroId));
            }

            view.Render(lastViewModel);
        }

        /// <summary>
        /// Milestone 8.72. The delegated hero's one revealed resolving card, as a hand of its own, for
        /// the viewer whose seat is deciding that hero's action - or <c>null</c>, which is every board
        /// no delegation stands on.
        ///
        /// <para><b>Why the card is composed here rather than found in the viewer's own tray.</b> A
        /// printed action is drawn on the card it is printed on, and the deciding seat does not own
        /// that card - <see cref="DelegatedActionPresentation"/>'s own remarks name this as the whole
        /// of HA-Q15i. So the seat is given the card, once, in a slot that says whose it is.</para>
        ///
        /// <para>Everything about the card is read through the SAME per-hero derivation the ordinary
        /// tray uses, narrowed to the one delegated instance: nothing here invents a second reading of
        /// a card's lifecycle, and a card the viewer's own projection does not carry (a projection
        /// that somehow withholds the revealed card) yields no slot rather than an empty one.</para>
        /// </summary>
        private static DirectorHandViewModel BuildDecidedForHand(
            IMatchReadModel projection, HeroId viewedHeroId)
        {
            var delegation = DelegatedActionPresentation.DecidedBy(projection, viewedHeroId);
            if (!delegation.IsDefined)
            {
                return null;
            }

            var hero = projection.Setup.Heroes.FirstOrDefault(candidate =>
                candidate.HeroId.Equals(delegation.DelegatedHeroId));
            var card = projection.Cards.FirstOrDefault(candidate =>
                candidate.Id.Equals(delegation.DelegatedCardInstanceId));
            if (hero == null || card == null)
            {
                return null;
            }

            // The resolving card is answered HERE - by this viewer - which is the one fact the
            // per-hero derivation cannot know on its own, because it is asked about a hero and the
            // answer belongs to a seat.
            var tray = BuildTray(projection, delegation.DelegatedHeroId, null, true)
                .Where(entry => entry.CardInstanceId.Equals(delegation.DelegatedCardInstanceId))
                .ToArray();
            if (tray.Length != 1)
            {
                return null;
            }

            var commitment = projection.GetCommitment(delegation.DelegatedHeroId);
            return new DirectorHandViewModel(
                hero.TeamSide,
                delegation.DelegatedHeroId,
                hero.DisplayName,
                commitment.Status,
                // Never a commitment surface: this hero's turn is running and the card is revealed.
                false,
                commitment.RevealedCardInstanceId,
                card,
                Array.Empty<CardInstance>(),
                projection.Definitions,
                null,
                tray);
        }

        /// <summary>
        /// Milestone 7.8a: one hero's current-round cards in the tray's own order - played in ascending
        /// turn order, then discarded/unavailable, then available.
        ///
        /// The three groups are read from projected card lifecycle state and the round's own assignment
        /// history; nothing here decides that a card was retrieved, discarded by a Defense, or cleared by
        /// a round reset. A card the hero owns that is in neither the hand, the turn area, nor the
        /// discard pile - a deck card, a set-aside card, an item, an unlocked Ultimate - is not part of
        /// this round's five and does not appear.
        ///
        /// <para>Milestone 8.35z58 widens this from <c>private</c> to <c>internal</c> so
        /// <see cref="RoundActionPresenter"/> can reuse the SAME per-hero hand derivation for the
        /// level-up hand preview rather than deriving a second one - one seat's hand is one
        /// computation, read by both consumers.</para>
        /// </summary>
        /// <param name="resolvingCardIsAnsweredHere">
        /// Milestone 8.72. Whether the resolving card in this tray is the one THIS surface is being
        /// asked to answer. <c>null</c> - every caller before 8.72, and every ordinary tray since -
        /// means "ask the delegation": a hero's own resolving card is theirs to answer unless
        /// somebody else is deciding that action. The "deciding for X" slot passes <c>true</c>,
        /// because there the card belongs to one hero and the answer belongs to another.
        /// </param>
        internal static IReadOnlyList<TrayCardViewModel> BuildTray(
            IMatchReadModel projection,
            HeroId heroId,
            CardInstanceId? selectedCardInstanceId,
            bool? resolvingCardIsAnsweredHere = null,
            CardInstanceId? selectedSecondCardInstanceId = null,
            IEnumerable<CardInstance> displayCards = null,
            bool readOnly = false,
            HeroCardInspection trustedInspection = null)
        {
            var commitment = projection.GetCommitment(heroId);
            var canSelect = !readOnly && projection.IsInputEnabled &&
                projection.Phase == RoundResolutionPhase.AwaitingCommitments &&
                commitment.Status == TurnCardCommitmentStatus.AwaitingResponse;
            // Milestone 8.5b's correction: the card this hero revealed for the current turn is the card
            // in play, and it is drawn at full strength while its turn runs. It carries no commitment
            // command - the round is past choosing - but "carries no command" and "is spent" are not the
            // same thing, and the shared unavailable treatment said the second about the first.
            //
            // Milestone 8.7a adds the second half of the question. One turn is resolved hero by hero in
            // Initiative order, so the hero who acted first has a resolved card while the turn is still
            // running for somebody else; asking the commitment alone left that card reading as in play
            // on the tray at the same moment the rail moved it into resolved history. The card's own
            // resolution status is what both surfaces ask now, so one card cannot be in two states.
            var revealedCardInstanceId =
                commitment.Status == TurnCardCommitmentStatus.Revealed
                    ? commitment.RevealedCardInstanceId
                    : null;
            var resolvingCardInstanceId = revealedCardInstanceId.HasValue &&
                projection.Cards.Any(card => card.Id.Equals(revealedCardInstanceId.Value) &&
                    card.State.ResolutionStatus != CardResolutionStatus.Resolved)
                ? revealedCardInstanceId
                : null;
            var entries = new List<TrayCardViewModel>();
            foreach (var card in (displayCards ?? projection.Cards).Where(card => card.OwnerId.Equals(heroId)))
            {
                var group = trustedInspection != null &&
                    trustedInspection.HandCards.Contains(card.DefinitionReference)
                    ? TrayCardGroup.Available
                    : card.State.Location == CardLocation.Hand
                    ? TrayCardGroup.Available
                    : card.State.Location == CardLocation.DiscardPile
                        ? TrayCardGroup.Discarded
                        : card.State.Location == CardLocation.TurnArea
                            ? TrayCardGroup.Played
                            : TrayCardGroup.None;
                if (group == TrayCardGroup.None)
                {
                    continue;
                }

                var playedTurnIndex = CurrentRoundCards.PlayedTurnIndex(projection, heroId, card.Id);
                if (group == TrayCardGroup.Played && playedTurnIndex == 0)
                {
                    // Before Initiative creates its slot, both face-up unresolved cards still belong
                    // to this numbered turn and remain visible in the played group for retrieval.
                    if (projection.Phase == RoundResolutionPhase.AwaitingCommitments &&
                        commitment.Status == TurnCardCommitmentStatus.Revealed &&
                        card.State.FaceOrientation == CardFaceOrientation.FaceUp)
                        playedTurnIndex = projection.TurnIndex;
                    else
                        group = TrayCardGroup.Discarded;
                }

                var isAvailableForCommitment = group == TrayCardGroup.Available && canSelect;
                var isResolvingThisCard = resolvingCardInstanceId.HasValue &&
                    resolvingCardInstanceId.Value.Equals(card.Id);
                // Milestone 8.35z9, and a defect live testing found: `isResolvingThisCard` is true
                // from the moment this hero's card is REVEALED until it finishes resolving, which is
                // a far wider window than "resolving now". Every hero reveals at the start of the
                // round and turns then run one at a time in Initiative order, so between the reveal
                // and this hero's own turn - potentially five other turns - the card was claiming the
                // awaited gold while somebody else acted. The reported symptom was exact: during
                // another hero's release pause, correctly narrated in the third person and with that
                // hero's own tray correctly lit, the viewer's own committed card still wore the gold
                // border that means "act now". The gold's whole grammar is "this is yours to answer",
                // so a card only keeps it while the turn it belongs to is actually under way and this
                // hero is the one acting. Both facts are public and unscoped, so this reads the same
                // for a seat and for the Director.
                //
                // Milestone 8.72 adds the second half of the same sentence, and it is the targeted
                // player's half of Hanu's The Ultimate Trick. The gold's whole grammar is "THIS IS
                // YOURS TO ANSWER", and while a delegation stands the hero whose piece is acting is
                // precisely the one player who may answer nothing: the host refuses that seat's every
                // command. So "the turn is under way and this hero is the actor" is no longer enough -
                // the answer has to be this surface's to give. With no delegation the added clause is
                // the actor comparison restated (DecidingHeroFor answers the actor itself), so every
                // hero and every earlier milestone reads exactly as before.
                var resolutionIsUnderWayForThisHero = projection.IsTurnActive &&
                    projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(heroId) &&
                    (resolvingCardIsAnsweredHere ??
                        DelegatedActionPresentation.DecidingHeroFor(projection).Equals(heroId));
                entries.Add(CurrentRoundCards.TrayCard(
                    projection,
                    card,
                    group,
                    group == TrayCardGroup.Played ? playedTurnIndex : 0,
                    isAvailableForCommitment,
                    (selectedCardInstanceId.HasValue && selectedCardInstanceId.Value.Equals(card.Id)) ||
                        (selectedSecondCardInstanceId.HasValue &&
                            selectedSecondCardInstanceId.Value.Equals(card.Id)),
                    isResolvingThisCard,
                    // Milestone 8.35m, the SEVENTH amendment's card scope and resolution focus, both
                    // read off facts this method already computed rather than derived a second time:
                    // a card offered for commitment right now is a legal answer, and so - for the
                    // resolution-focus case - is the one card currently resolving, which is why it
                    // shines exactly like a clickable candidate although it carries no click any more.
                    isAwaitedAnswer: isAvailableForCommitment ||
                        (isResolvingThisCard && resolutionIsUnderWayForThisHero),
                    // Every OTHER hand card dims while one is resolving, so the eye lands on the card
                    // whose text is running; the resolving card itself is excluded by construction
                    // (its own group is never Available - it moved to the turn area on commitment).
                    isDimmedByResolution: resolvingCardInstanceId.HasValue &&
                        group == TrayCardGroup.Available && !isResolvingThisCard));
            }

            return Array.AsReadOnly(entries
                .OrderBy(entry => entry.Group == TrayCardGroup.Played
                    ? 0
                    : entry.Group == TrayCardGroup.Discarded ? 1 : 2)
                .ThenBy(entry => entry.PlayedTurnIndex)
                .ThenBy(entry => entry.CardInstanceId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public bool SelectCommitmentCard(CardInstanceId cardInstanceId)
        {
            var projection = session.Projection;
            if (useTrustedLocalHeroPerspective ||
                FindCommittableCard(projection, cardInstanceId) == null)
            {
                return false;
            }

            if (!selectedCardInstanceId.HasValue ||
                !projection.GetCommitment(FindCommittableCard(projection, cardInstanceId).OwnerId)
                    .CanCommitTwoCards)
            {
                selectedCardInstanceId = cardInstanceId;
                selectedSecondCardInstanceId = null;
            }
            else if (selectedCardInstanceId.Value.Equals(cardInstanceId))
                selectedSecondCardInstanceId = null;
            else
                selectedSecondCardInstanceId = selectedSecondCardInstanceId.HasValue &&
                    selectedSecondCardInstanceId.Value.Equals(cardInstanceId)
                    ? (CardInstanceId?)null : cardInstanceId;
            Refresh();
            return true;
        }

        public bool ConfirmSelectedCard()
        {
            if (useTrustedLocalHeroPerspective ||
                !selectedCardInstanceId.HasValue || !session.Projection.IsInputEnabled)
            {
                return false;
            }

            var card = session.Projection.Cards.SingleOrDefault(value => value.Id.Equals(selectedCardInstanceId.Value));
            if (card == null)
            {
                return false;
            }

            var result = session.Submit(selectedSecondCardInstanceId.HasValue
                ? DirectorCommand.CommitTwoCards(
                    card.OwnerId, card.Id, selectedSecondCardInstanceId.Value)
                : DirectorCommand.CommitCard(card.OwnerId, card.Id));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                selectedSecondCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        /// <summary>
        /// Commits one currently projected hand card without first storing it as the click-selection.
        /// Milestone 8.30g uses this for the drag-to-map path: the pointer's drop position is deliberately
        /// absent because the map is only a presentation boundary, never a card target or rules input.
        /// The card is re-read from the standing projection immediately before submission, so a stale,
        /// hidden, already committed, or otherwise ineligible drag is refused without mutation.
        /// </summary>
        public bool CommitCardImmediately(CardInstanceId cardInstanceId)
        {
            if (useTrustedLocalHeroPerspective)
            {
                return false;
            }

            var projection = session.Projection;
            var card = FindCommittableCard(projection, cardInstanceId);
            if (card == null)
            {
                return false;
            }

            var result = session.Submit(DirectorCommand.CommitCard(card.OwnerId, card.Id));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        private CardInstance FindCommittableCard(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (!projection.IsInputEnabled ||
                projection.Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                return null;
            }

            var card = projection.Cards.SingleOrDefault(value => value.Id.Equals(cardInstanceId));
            return card != null && (session.IsDirector || card.OwnerId.Equals(homeHeroId)) &&
                card.State.Location == CardLocation.Hand &&
                projection.GetCommitment(card.OwnerId).Status ==
                    TurnCardCommitmentStatus.AwaitingResponse
                ? card
                : null;
        }

        public bool Pass(HeroId heroId)
        {
            if (useTrustedLocalHeroPerspective)
            {
                return false;
            }

            var projection = session.Projection;
            if (!projection.IsInputEnabled)
            {
                return false;
            }

            var commitment = projection.Commitments.SingleOrDefault(value => value.HeroId.Equals(heroId));
            if (!commitment.HeroId.IsDefined || !commitment.CanPass)
            {
                return false;
            }

            var result = session.Submit(DirectorCommand.Pass(heroId));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }
}
