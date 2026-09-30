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

    public sealed class BoardInteractionPresenter
    {
        private readonly IMatchClient session;
        private readonly IBoardInteractionView view;
        private readonly EffectCapabilityCatalog activeEffectCapabilities;
        private IMatchReadModel lastProjection;
        private BoardInteractionViewModel lastViewModel;
        private IMatchReadModel fastTravelSelectionProjection;
        private ActionDefinitionId? selectedFastTravelActionDefinitionId;
        private IMatchReadModel choiceHighlightProjection;
        private RuleObject highlightedChoiceUnit;
        private HexCoordinate? highlightedCorrectionDestination;
        private IMatchReadModel effectChoiceHighlightProjection;
        private HexCoordinate? highlightedEffectChoiceSpace;
        private IMatchReadModel respawnHighlightProjection;
        private HexCoordinate? highlightedRespawnSpace;
        private IMatchReadModel clearCueProjection;
        private IReadOnlyList<TokenInstanceId> offeredClearTokens = Array.Empty<TokenInstanceId>();
        private IReadOnlyList<TokenInstanceId> selectedClearTokens = Array.Empty<TokenInstanceId>();


        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor every existing fixture and journey uses,
        /// composing the Director's own client. A seat-scoped surface passes its client instead.
        /// </summary>
        public BoardInteractionPresenter(IMatchSession session, IBoardInteractionView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        /// <summary>
        /// Milestone 8.35z63 adds <paramref name="activeEffectCapabilities"/>, and it is
        /// TAKEN rather than asked for: <c>EffectSeamConformanceTests</c> forbids a production type
        /// naming <c>MatchContentCatalogs.ForThisBuild</c>, so the composition that already holds this
        /// match's content hands its capability catalog down. Left null - which is every historical
        /// fixture and every lower-level composition - the board simply draws no Active-effect
        /// treatment, exactly as it did before this milestone.
        /// </summary>
        public BoardInteractionPresenter(
            IMatchClient session,
            IBoardInteractionView view,
            EffectCapabilityCatalog activeEffectCapabilities = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.activeEffectCapabilities = activeEffectCapabilities;
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, fastTravelSelectionProjection))
            {
                selectedFastTravelActionDefinitionId = null;
                fastTravelSelectionProjection = null;
            }

            // A pointed-at candidate belongs to the projection it was read from. Anything that
            // rebuilds authoritative state - an accepted command, a rejection, restore, replay, or
            // Undo - drops it, so the board never keeps a cue for a choice that has moved on.
            if (!ReferenceEquals(projection, choiceHighlightProjection))
            {
                highlightedChoiceUnit = RuleObject.None;
                highlightedCorrectionDestination = null;
                choiceHighlightProjection = null;
            }

            // Milestone 6.5c: a pointed-at effect-choice target is anchored the same way, so the cue
            // and the resulting space it predicts cannot outlive the projection they were read from.
            if (!ReferenceEquals(projection, effectChoiceHighlightProjection))
            {
                highlightedEffectChoiceSpace = null;
                effectChoiceHighlightProjection = null;
            }

            if (!ReferenceEquals(projection, respawnHighlightProjection))
            {
                highlightedRespawnSpace = null;
                respawnHighlightProjection = null;
            }

            // Milestone 6.3a: a token cue is anchored the same way. A selection is local presenter
            // state on the round presenter rather than authoritative state, so nothing else would drop
            // it when the projection moves on, and a subset held across a command is a subset a player
            // never chose for the position they are now in.
            if (!ReferenceEquals(projection, clearCueProjection))
            {
                offeredClearTokens = Array.Empty<TokenInstanceId>();
                selectedClearTokens = Array.Empty<TokenInstanceId>();
                clearCueProjection = null;
            }

            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                var directions = CreateDirectionCandidates(projection);
                lastViewModel = new BoardInteractionViewModel(
                    projection.Version,
                    projection.Heroes,
                    projection.Minions,
                    projection.PendingMovementChoice?.LegalDestinations.Select(value => value.Destination) ??
                        Enumerable.Empty<HexCoordinate>(),
                    projection.PendingAttackTargetChoice?.LegalTargetHeroIds ?? Array.Empty<HeroId>(),
                    projection.PendingAttackTargetChoice?.LegalTargetMinionIds ?? Array.Empty<MinionId>(),
                    projection.SelectedActor.IsDefined ? projection.SelectedActor.ActorId : default,
                    projection.PlacedTokens,
                    GetSelectedFastTravelOption(projection)?.LegalDestinations ??
                        Array.Empty<HexCoordinate>(),
                    projection.PendingDefensePreview?.MinionContributions.Select(value =>
                        new MinionDefenseCueViewModel(value.SourceMinionId, value.Amount)) ??
                        Enumerable.Empty<MinionDefenseCueViewModel>(),
                    CreatePieceTargetCues(
                        projection,
                        highlightedChoiceUnit,
                        offeredClearTokens,
                        selectedClearTokens),
                    highlightedCorrectionDestination,
                    SelectableEffectChoiceDestinations(projection),
                    highlightedEffectChoiceSpace,
                    FindResultingSpace(projection, highlightedEffectChoiceSpace),
                    highlightedRespawnSpace,
                    projection.PendingHeroRespawnChoice?.Destinations
                        .Select(destination => destination.Coordinate),
                    CreateSystemTransitionDestinations(projection),
                    SelectableEffectChoiceMovementDestinations(projection),
                    projection.PendingAttackTargetDefensePreviews,
                    // Milestone 8.35z63. Derived once per authoritative projection, on the
                    // same identity gate every other member here rides, so the disks are re-anchored
                    // exactly when the board is rebuilt - which is p.17's "an Active effect's Radius
                    // moves with its hero" for free, and no per-frame work at all.
                    ActiveEffectVisibility.Derive(projection, activeEffectCapabilities),
                    // Milestone 8.46d. Derived on the same per-projection gate as everything else
                    // here, so the arrows are cued exactly when the pause stands and cleared exactly
                    // when it resolves - there is no arrow state of this presenter's own to go stale.
                    directions.ChooserSpace,
                    directions.Candidates,
                    projection.Obstacles,
                    // Milestone 9.21i. The engine's settled acting body of the selected actor, so the
                    // acting cue stands on the body that acts and follows it wherever the engine moves it.
                    FocusedActingBody(projection));
            }

            view.Render(lastViewModel);
        }

        /// <summary>
        /// Marks one candidate of the pending minion choice as pointed at, so the board can say which
        /// minion a hovered or focused prompt control refers to. Returns false when nothing changed,
        /// which is what stops a per-frame hover notification from rebuilding the board every repaint.
        /// </summary>
        public bool HighlightMinionChoiceCandidate(RuleObject unit) =>
            HighlightMinionChoiceCandidate(unit, null);

        /// <summary>
        /// Milestone 6.2d: the same report, carrying both halves of the candidate a correction
        /// destination control names.
        ///
        /// The pointer has been tracked since 6.4b and the destination has ridden on the candidate
        /// since 6.4b as well; only the unit half was ever reported, so the board lit every endpoint
        /// at once and left the player to guess which control was theirs. Both halves are resolved
        /// against the authoritative pending choice here rather than trusted from the control - a
        /// stale prompt names a space this choice no longer offers, and that must light nothing.
        /// </summary>
        public bool HighlightMinionChoiceCandidate(RuleObject unit, HexCoordinate? destination)
        {
            var projection = session.Projection;
            var choice = projection.PendingMinionBattleChoice;
            var correction = projection.PendingDisplacedMinionCorrection;
            var resolved = unit.IsDefined &&
                (choice != null &&
                        choice.Candidates.Any(candidate => candidate.Unit.Equals(unit)) ||
                    correction?.Stage == DisplacedMinionCorrectionStage.SelectSubject &&
                        IntrinsicKindQuery.IsMinionMiniature(unit) &&
                        correction.SubjectCandidates.Any(subject => subject.MinionId.Equals(unit.MinionId)))
                    ? unit
                    : RuleObject.None;
            var resolvedDestination = destination.HasValue &&
                correction?.Stage == DisplacedMinionCorrectionStage.SelectDestination &&
                correction.DestinationCandidates.Any(candidate =>
                    candidate.Destination.Equals(destination.Value))
                    ? destination
                    : null;
            var pointsAtSomething = resolved.IsDefined || resolvedDestination.HasValue;
            if (resolved.Equals(highlightedChoiceUnit) &&
                Nullable.Equals(resolvedDestination, highlightedCorrectionDestination) &&
                (!pointsAtSomething || ReferenceEquals(projection, choiceHighlightProjection)))
            {
                return false;
            }

            highlightedChoiceUnit = resolved;
            highlightedCorrectionDestination = resolvedDestination;
            choiceHighlightProjection = pointsAtSomething ? projection : null;
            lastProjection = null;
            return true;
        }

        /// <summary>
        /// Marks one target of the pending effect choice as pointed at, so the board can say which
        /// object a hovered or focused control names and where the projected operation would leave it.
        /// Returns false when nothing changed, which is what stops a per-frame hover notification from
        /// rebuilding the board every repaint.
        /// </summary>
        public bool HighlightEffectChoiceCandidate(HexCoordinate? space)
        {
            var projection = session.Projection;
            var resolved = space.HasValue && projection.PendingEffectChoicePreview.Any(preview =>
                    preview.NamesObject && preview.Coordinate.Equals(space.Value))
                ? space
                : null;
            if (Nullable.Equals(resolved, highlightedEffectChoiceSpace) &&
                (!resolved.HasValue || ReferenceEquals(projection, effectChoiceHighlightProjection)))
            {
                return false;
            }

            highlightedEffectChoiceSpace = resolved;
            effectChoiceHighlightProjection = resolved.HasValue ? projection : null;
            lastProjection = null;
            return true;
        }

        /// <summary>
        /// Marks the Clear candidate a hovered or keyboard-focused control names, so its eligible
        /// removal ring says which token the words on that control mean. Passing an undefined
        /// identity clears the local cue.
        ///
        /// Milestone 8.15a, at the user's direction: the cues are derived from the open selection
        /// itself rather than from pointer state - every offered token is marked Eligible where it
        /// stands and every chosen one Selected, so the board is the choosing surface and nothing
        /// depends on where the pointer happens to rest. Each identity is resolved against the
        /// projected token list and nothing else, which is what makes a stale selection harmless: a
        /// token that has since been replaced, removed or moved is simply not there to mark.
        /// </summary>
        public bool SetOpenClearSelection(ClearSelectionViewModel selection)
        {
            var projection = session.Projection;
            var offered = (selection?.Candidates ?? Array.Empty<ClearTokenCandidateViewModel>())
                .Select(candidate => candidate.TokenId)
                .Where(tokenId => tokenId.IsDefined &&
                    projection.PlacedTokens.Any(token => token.Id.Equals(tokenId)))
                .Distinct()
                .ToArray();
            var selected = (selection?.SelectedTokenIds ?? Array.Empty<TokenInstanceId>())
                .Where(tokenId => offered.Contains(tokenId))
                .Distinct()
                .ToArray();
            var marksSomething = offered.Length > 0;
            if (offered.SequenceEqual(offeredClearTokens) &&
                selected.SequenceEqual(selectedClearTokens) &&
                (!marksSomething || ReferenceEquals(projection, clearCueProjection)))
            {
                return false;
            }

            offeredClearTokens = offered;
            selectedClearTokens = selected;
            clearCueProjection = marksSomething ? projection : null;
            lastProjection = null;
            return true;
        }

        public bool HighlightHeroRespawnDestination(HexCoordinate? space)
        {
            var projection = session.Projection;
            var resolved = space.HasValue && projection.PendingHeroRespawnChoice != null &&
                projection.PendingHeroRespawnChoice.Destinations.Any(destination =>
                    destination.Coordinate.Equals(space.Value))
                ? space
                : null;
            if (Nullable.Equals(resolved, highlightedRespawnSpace) &&
                (!resolved.HasValue || ReferenceEquals(projection, respawnHighlightProjection)))
            {
                return false;
            }

            highlightedRespawnSpace = resolved;
            respawnHighlightProjection = resolved.HasValue ? projection : null;
            lastProjection = null;
            return true;
        }

        /// <summary>
        /// The empty spaces the choice can still be answered with. A candidate the authority no longer
        /// offers a select invocation for - because the selection is already at its maximum - is not
        /// lit, so a lit space is always one a click can take.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> SelectableEffectChoiceDestinations(
            IMatchReadModel projection) =>
            SelectableEffectChoiceSpaces(projection, emptyOnly: true);

        private static IReadOnlyList<HexCoordinate> SelectableEffectChoiceMovementDestinations(
            IMatchReadModel projection)
        {
            var selectable = new HashSet<HexCoordinate>(SelectableEffectChoiceDestinations(projection));
            return projection.PendingEffectChoicePreview
                .Where(value =>
                    !value.NamesObject &&
                    value.ConsequenceIntent == EffectChoiceConsequenceIntent.Movement &&
                    selectable.Contains(value.Coordinate))
                .Select(value => value.Coordinate)
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.11a: every space a click can answer this choice on, occupied or not.
        ///
        /// <para><paramref name="emptyOnly"/> separates the two readings that used to be one. The board
        /// lights empty candidates as destinations and marks object-naming ones as targets, which are
        /// drawn differently and must not be confused; but a click answers either, so the submission
        /// path asks for all of them.</para>
        /// </summary>
        private static IReadOnlyList<HexCoordinate> SelectableEffectChoiceSpaces(
            IMatchReadModel projection, bool emptyOnly = false)
        {
            var choice = projection.PendingEffectChoice;
            if (choice == null || projection.PendingEffectChoicePreview.Count == 0)
            {
                return Array.Empty<HexCoordinate>();
            }

            // Milestone 8.11c: matched by the candidate's own value rather than by a coordinate the
            // candidate carries, because a candidate naming a hero by identity carries none. The
            // preview is where the two meet - it holds the value the authority offered and the space
            // occupancy currently puts it on.
            var admitted = new Dictionary<EffectValue, HexCoordinate>();
            foreach (var preview in projection.PendingEffectChoicePreview)
            {
                if (!emptyOnly || !preview.NamesObject)
                {
                    admitted[preview.CandidateValue] = preview.Coordinate;
                }
            }

            return choice.SelectInvocations.Concat(choice.DeselectInvocations)
                .Where(invocation =>
                    invocation.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    admitted.ContainsKey(invocation.Candidate.Value))
                .Select(invocation => admitted[invocation.Candidate.Value])
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.46d. The six arrows a declared
        /// <see cref="EffectChoiceValueVocabulary.HexDirection"/> choice is answered by, and the space
        /// they are drawn around.
        ///
        /// <para><b>Ownership is asked before a single command is bound.</b> A seat that does not own
        /// the decision is published no <c>PendingEffectChoice</c> and no
        /// <c>DecisionContract</c> at all (<c>SeatProjection</c>'s own scoping rule), and
        /// <see cref="PendingDecisionCommandBinding.Require"/> THROWS on a null or other-family
        /// contract rather than returning nothing - so the contract is checked here on
        /// <c>AwaitedInputQuery</c>'s own reading of "owns a standing decision". A non-owner draws no
        /// arrows because it has nothing to draw them from, not because it is filtered.</para>
        ///
        /// <para>Every candidate is matched by the vocabulary the handler DECLARED rather than by
        /// guessing at integers, which is the rule <c>EffectChoiceValueVocabulary</c> exists to state:
        /// the six values are directions because the capability that offered them said so.</para>
        /// </summary>
        private static DirectionChoiceOffer CreateDirectionCandidates(IMatchReadModel projection)
        {
            var choice = projection?.PendingEffectChoice;
            if (choice == null ||
                choice.ValueVocabulary != EffectChoiceValueVocabulary.HexDirection ||
                projection.DecisionContract == null ||
                !projection.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.Effect))
            {
                return default;
            }

            var chooserId = choice.ChooserId.IsDefined
                ? choice.ChooserId
                : projection.SelectedActor.IsDefined
                    ? projection.SelectedActor.ActorId
                    : default;
            if (!chooserId.IsDefined)
            {
                return default;
            }

            var chooser = projection.Heroes.FirstOrDefault(value => value.HeroId.Equals(chooserId));
            if (!chooser.HeroId.IsDefined)
            {
                // The hero being asked is not standing anywhere this board draws, so there is no
                // centre to put arrows around. The prompt's own controls still answer the choice.
                return default;
            }

            var candidates = new List<DirectionCandidateViewModel>();
            foreach (var invocation in choice.SelectInvocations)
            {
                var candidate = invocation.Candidate;
                if (candidate.Kind != EffectChoiceCandidateKind.Value ||
                    candidate.Value.Kind != EffectValueKind.Integer ||
                    !Enum.IsDefined(typeof(HexDirection), candidate.Value.IntegerValue))
                {
                    continue;
                }

                var direction = (HexDirection)candidate.Value.IntegerValue;
                candidates.Add(new DirectionCandidateViewModel(
                    direction,
                    chooser.Coordinate.GetNeighbor(direction),
                    EffectChoiceLabels.VocabularyLabel(
                        EffectChoiceValueVocabulary.HexDirection, candidate.Value.IntegerValue),
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, invocation))));
            }

            return new DirectionChoiceOffer(chooser.Coordinate, candidates);
        }

        /// <summary>
        /// The candidate whose arrow stands on this space, or null. Milestone 8.46d.
        /// </summary>
        private static DirectionCandidateViewModel FindDirectionCandidate(
            IMatchReadModel projection, HexCoordinate space) =>
            CreateDirectionCandidates(projection).Candidates
                .FirstOrDefault(value => value.Neighbour.Equals(space));

        /// <summary>The chooser's space and its arrows, derived together because neither is meaningful
        /// alone. Milestone 8.46d.</summary>
        private readonly struct DirectionChoiceOffer
        {
            private readonly IReadOnlyList<DirectionCandidateViewModel> candidates;

            internal DirectionChoiceOffer(
                HexCoordinate chooserSpace, IReadOnlyList<DirectionCandidateViewModel> candidates)
            {
                ChooserSpace = chooserSpace;
                this.candidates = candidates;
            }

            internal HexCoordinate? ChooserSpace { get; }

            internal IReadOnlyList<DirectionCandidateViewModel> Candidates =>
                candidates ?? Array.Empty<DirectionCandidateViewModel>();
        }

        private static HexCoordinate? FindResultingSpace(
            IMatchReadModel projection, HexCoordinate? targetSpace)
        {
            if (!targetSpace.HasValue)
            {
                return null;
            }

            foreach (var preview in projection.PendingEffectChoicePreview)
            {
                if (preview.NamesObject && preview.Coordinate.Equals(targetSpace.Value))
                {
                    return preview.ResultingCoordinate;
                }
            }

            return null;
        }

        /// <summary>
        /// Merges every piece-target consumer into one identity-keyed projection. This is a routing
        /// operation over authority-shaped offers: it does not discover occupants, re-run legality,
        /// or predict whether the declared consequence will succeed. Selected outranks eligible;
        /// different consequence intents for one identity are an impossible concurrent projection.
        /// </summary>
        private static IReadOnlyList<PieceTargetCueViewModel> CreatePieceTargetCues(
            IMatchReadModel projection,
            RuleObject highlightedChoiceUnit,
            IReadOnlyList<TokenInstanceId> offeredClearTokenIds,
            IReadOnlyList<TokenInstanceId> selectedClearTokenIds)
        {
            // Milestone 9.21i. Keyed by the PIECE, which for a hero on several spaces is one body.
            var cues = new Dictionary<(BoardObjectReference, int), PieceTargetCueViewModel>();

            void Add(
                BoardObjectReference target,
                BoardTargetCueState state,
                BoardTargetConsequenceIntent consequenceIntent)
            {
                var next = new PieceTargetCueViewModel(target, state, consequenceIntent);
                var key = BoardInteractionViewModel.PieceKey(target);
                if (!cues.TryGetValue(key, out var current))
                {
                    cues.Add(key, next);
                    return;
                }

                if (current.ConsequenceIntent != consequenceIntent)
                {
                    throw new InvalidOperationException(
                        $"One piece cannot carry conflicting target consequence intents: {target}.");
                }

                if ((int)state > (int)current.State)
                {
                    cues[key] = next;
                }
            }

            // Milestone 9.21i. "Which of you acts" (RZ-Q4) is answered on the board: every standing
            // body is cued as a friendly target is, and a click on one answers the pick.
            var actingBodyPick = projection.PendingActingBodyChoice;
            if (actingBodyPick != null)
            {
                foreach (var body in actingBodyPick.Bodies)
                {
                    Add(
                        BoardObjectReference.ForHeroBody(body.HeroId, body.Body),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Ally);
                }
            }

            var attack = projection.PendingAttackTargetChoice;
            if (attack != null)
            {
                // Milestone 9.21i. An Attack targets a BODY (RZ-Q3): a hero on several spaces is cued
                // on each body the choice offers and on no other.
                foreach (var target in attack.LegalTargets.Where(value =>
                             value.Kind == AttackTargetKind.Hero))
                {
                    Add(
                        HeroPiece(projection, target.HeroId, target.HeroBody),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack);
                }

                foreach (var minionId in attack.LegalTargetMinionIds)
                {
                    Add(
                        BoardObjectReference.ForMinion(minionId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack);
                }
            }

            var battle = projection.PendingMinionBattleChoice;
            if (battle != null)
            {
                foreach (var candidate in battle.Candidates)
                {
                    var isSelected = battle.SelectedUnits.Contains(candidate.Unit);
                    if (!isSelected && !battle.LegalSelectUnits.Contains(candidate.Unit))
                    {
                        continue;
                    }

                    Add(
                        BoardReference(candidate.Unit),
                        isSelected ? BoardTargetCueState.Selected : BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal);
                }
            }
            else if (projection.TeamAgreement?.Kind == TeamAgreementKind.MinionBattle)
            {
                foreach (var selected in projection.TeamAgreement.SelectedUnits)
                {
                    Add(
                        BoardReference(selected),
                        BoardTargetCueState.Selected,
                        BoardTargetConsequenceIntent.Removal);
                }
            }

            // Milestone 8.21. The minion a standing removal prevention is about, cued where it stands
            // rather than named in the prompt - milestone 8.11a's reading of every candidate with a
            // board surface. It is not a candidate: nothing is chosen by clicking it, and the cue says
            // which piece the question is about. The consequence is the removal that is waiting on the
            // answer, which is what the price would buy it out of.
            var removalPrevention = projection.PendingRemovalPrevention;
            if (removalPrevention != null)
            {
                Add(
                    BoardObjectReference.ForMinion(removalPrevention.SubjectMinionId),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Removal);
            }

            var correction = projection.PendingDisplacedMinionCorrection;
            if (correction?.Stage == DisplacedMinionCorrectionStage.SelectSubject &&
                highlightedChoiceUnit.Kind == RuleObjectKind.Minion &&
                correction.SubjectCandidates.Any(candidate =>
                    candidate.MinionId.Equals(highlightedChoiceUnit.MinionId)))
            {
                Add(
                    BoardObjectReference.ForMinion(highlightedChoiceUnit.MinionId),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Affect);
            }
            else if (correction?.Stage == DisplacedMinionCorrectionStage.SelectDestination)
            {
                Add(
                    BoardObjectReference.ForMinion(correction.SubjectMinionId),
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Affect);
            }

            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice != null)
            {
                foreach (var preview in projection.PendingEffectChoicePreview.Where(value =>
                    value.NamesObject))
                {
                    var candidate = EffectChoiceCandidate.Value_(preview.CandidateValue);
                    var isSelected = effectChoice.SelectedCandidates.Contains(candidate);
                    var isEligible = effectChoice.SelectInvocations.Any(invocation =>
                        invocation.Candidate.Equals(candidate));
                    if (!isSelected && !isEligible)
                    {
                        continue;
                    }

                    // Milestone 9.21i. A preview names the hero; the body is the one on its space.
                    var occupant = preview.Occupant.Kind == BoardObjectKind.Hero
                        ? HeroPieceAt(projection, preview.Occupant.HeroId, preview.Coordinate)
                        : preview.Occupant;
                    Add(
                        occupant,
                        isSelected ? BoardTargetCueState.Selected : BoardTargetCueState.Eligible,
                        preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Removal
                            ? BoardTargetConsequenceIntent.Removal
                            : preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Attack
                                ? BoardTargetConsequenceIntent.Attack
                                : IsTeammate(projection, effectChoice.ChooserId, preview.Occupant)
                                    ? BoardTargetConsequenceIntent.Ally
                            : BoardTargetConsequenceIntent.Affect);
                }
            }

            // Milestone 8.15a: the open Clear selection's cues stand on their own - every offered
            // token Eligible, every chosen one Selected - because the board is where the choice is
            // made, and a piece a player may click has to say so before the pointer finds it.
            foreach (var token in projection.PlacedTokens)
            {
                var isSelected = selectedClearTokenIds.Contains(token.Id);
                var isEligible = offeredClearTokenIds.Contains(token.Id);
                if (isSelected || isEligible)
                {
                    Add(
                        BoardObjectReference.ForToken(token.Id),
                        isSelected ? BoardTargetCueState.Selected : BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal);
                }
            }

            return cues.Values
                .OrderBy(value => value.Target.Kind)
                .ThenBy(value => value.Target.ToString(), StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Milestone 9.21i. The piece a hero cue names: the hero alone while he stands on one space
        /// (every single-bodied hero's cue is unchanged), and the named body while he stands on several.
        /// </summary>
        private static BoardObjectReference HeroPiece(IMatchReadModel projection, HeroId heroId, int body) =>
            StandsOnSeveralSpaces(projection, heroId) && body >= HeroOccupancy.FirstBody
                ? BoardObjectReference.ForHeroBody(heroId, body)
                : BoardObjectReference.ForHero(heroId);

        private static BoardObjectReference HeroPieceAt(
            IMatchReadModel projection, HeroId heroId, HexCoordinate space)
        {
            if (!StandsOnSeveralSpaces(projection, heroId)) return BoardObjectReference.ForHero(heroId);
            var standing = projection.Heroes.FirstOrDefault(value =>
                value.HeroId.Equals(heroId) && value.Coordinate.Equals(space));
            return standing.HeroId.IsDefined
                ? BoardObjectReference.ForHeroBody(heroId, standing.Body)
                : BoardObjectReference.ForHero(heroId);
        }

        private static bool StandsOnSeveralSpaces(IMatchReadModel projection, HeroId heroId) =>
            projection.Heroes.Count(value => value.HeroId.Equals(heroId)) > 1;

        /// <summary>
        /// Milestone 9.21i. The selected actor's settled acting body, read from the engine
        /// (<see cref="IMatchReadModel.ActingBodies"/>), or 0 when none is settled or none is visible.
        /// </summary>
        internal static int FocusedActingBody(IMatchReadModel projection)
        {
            if (!projection.SelectedActor.IsDefined) return 0;
            var actor = projection.SelectedActor.ActorId;
            foreach (var acting in projection.ActingBodies ?? Array.Empty<HeroActingBody>())
            {
                if (acting.HeroId.Equals(actor)) return acting.Body;
            }

            return 0;
        }

        private static bool IsTeammate(
            IMatchReadModel projection,
            HeroId chooserId,
            BoardObjectReference target)
        {
            if (!chooserId.IsDefined || !target.IsDefined)
            {
                return false;
            }

            var chooserSide = projection.Participants.GetTeamForHero(chooserId).Side;
            switch (target.Kind)
            {
                case BoardObjectKind.Hero:
                    return projection.Participants.GetTeamForHero(target.HeroId).Side == chooserSide;
                case BoardObjectKind.Minion:
                    return projection.Minions.Single(value => value.MinionId.Equals(target.MinionId))
                        .TeamSide == chooserSide;
                case BoardObjectKind.Token:
                    var controller = projection.PlacedTokens
                        .Single(value => value.Id.Equals(target.TokenInstanceId)).ControllerHeroId;
                    return controller.IsDefined &&
                        projection.Participants.GetTeamForHero(controller).Side == chooserSide;
                default:
                    return false;
            }
        }

        private static BoardObjectReference BoardReference(RuleObject unit)
        {
            if (unit.Kind == RuleObjectKind.Hero)
            {
                return BoardObjectReference.ForHero(unit.HeroId);
            }

            if (unit.Kind == RuleObjectKind.Minion)
            {
                return BoardObjectReference.ForMinion(unit.MinionId);
            }

            throw new ArgumentException("A piece target cue requires a hero or minion.", nameof(unit));
        }

        public bool ActivateFastTravel(ActionDefinitionId replacedMovementActionDefinitionId)
        {
            var projection = session.Projection;
            var matches = projection.AvailableFastTravelOptions
                .Where(option => option.ReplacedMovementActionDefinitionId.Equals(
                    replacedMovementActionDefinitionId) && option.LegalDestinations.Count > 0)
                .ToArray();
            if (matches.Length != 1)
            {
                return false;
            }

            selectedFastTravelActionDefinitionId = replacedMovementActionDefinitionId;
            fastTravelSelectionProjection = projection;
            lastProjection = null;
            return true;
        }

        public bool SubmitDestination(HexCoordinate destination)
        {
            // Milestone 8.16c: the p.8 blocked-Spawn-point Place is a space-valued decision even
            // though it lives in the shared system-transition continuation. Resolve the click from
            // the current description and forward its exact invocation; never decode an option id
            // from the coordinate, and never trust a model retained from an older projection.
            var transitionDestinations = CreateSystemTransitionDestinations(session.Projection);
            if (transitionDestinations.Count > 0)
            {
                var option = transitionDestinations.SingleOrDefault(value =>
                    value.Destination.Equals(destination));
                return option != null && Execute(option.Command);
            }

            // Milestone 6.4b: a correction destination is selectable straight off the board. The
            // families are mutually exclusive - a correction pauses mid-action, when no movement
            // choice or fast-travel activation can be live - so this is a route, not a priority.
            var correction = session.Projection.PendingDisplacedMinionCorrection;
            if (correction?.Stage == DisplacedMinionCorrectionStage.SelectDestination)
            {
                var invocation = correction.SelectInvocations.SingleOrDefault(value =>
                    value.Destination.Equals(destination));
                if (invocation == null)
                {
                    return false;
                }

                return Execute(PendingDecisionCommandBinding.Require(
                    session.Projection,
                    PendingDecisionFamilies.Correction,
                    command => command.Kind == DirectorCommandKind.ChooseCorrectionDestination &&
                        command.ResolutionId.Equals(invocation.ResolutionId) &&
                        command.ChoiceId.Equals(invocation.ChoiceId) &&
                        command.MinionId.Equals(invocation.MinionId) &&
                        command.Coordinate.Equals(invocation.Destination)));
            }

            // Milestone 6.5c: a lit effect-choice space is answered from the board alone. Same
            // exclusivity as the correction above - a paused effect frame forbids a movement choice
            // or a fast-travel activation - so this is a route rather than a priority.
            var effectChoice = session.Projection.PendingEffectChoice;
            if (effectChoice != null)
            {
                // Milestone 8.46d. A direction is answered by clicking the arrow around the chooser,
                // and an arrow stands on a neighbouring SPACE - so this click arrives here exactly as
                // an empty-space candidate's does. It is asked first because the two can never
                // collide: a direction's candidates are integers, so the space-valued lookup below
                // finds nothing for them and would answer false on a space this one just matched.
                var chosenDirection = FindDirectionCandidate(session.Projection, destination);
                if (chosenDirection != null)
                {
                    return SubmitEffectChoiceSelection(chosenDirection.Command);
                }

                // Milestone 8.11a, at the user's request: every candidate answers from the board, the
                // way an Attack target always has. Until now only an empty space did, and a candidate
                // naming an object was answered by a control in the prompt instead - which meant the
                // board could show you the piece being asked about but not let you pick it.
                //
                // Nothing about the command changes. An object-naming candidate already carries the
                // coordinate its object stands on, which is why the same lookup finds both; what the
                // preview adds is only whether that space is occupied, and that no longer decides
                // whether a click counts.
                if (!SelectableEffectChoiceSpaces(session.Projection).Contains(destination))
                {
                    return false;
                }

                var selection = effectChoice.SelectInvocations
                    .Concat(effectChoice.DeselectInvocations)
                    .SingleOrDefault(value =>
                    value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    value.Candidate.Value.Coordinate.Equals(destination));
                return selection != null && SubmitEffectChoiceSelection(
                    PendingDecisionCommandBinding.Require(
                        session.Projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, selection)));
            }

            var choice = session.Projection.PendingMovementChoice;
            if (choice == null || choice.LegalDestinations.Count(value => value.Destination.Equals(destination)) != 1)
            {
                var fastTravelOption = GetSelectedFastTravelOption(session.Projection);
                if (fastTravelOption == null || !fastTravelOption.LegalDestinations.Contains(destination))
                {
                    return false;
                }

                return Execute(DirectorCommand.PerformFastTravel(
                    fastTravelOption.ReplacedMovementActionDefinitionId,
                    destination));
            }

            return Execute(PendingDecisionCommandBinding.Require(
                session.Projection,
                PendingDecisionFamilies.Movement,
                command => command.Kind == DirectorCommandKind.ChooseMovementDestination &&
                    command.ResolutionId.Equals(choice.ResolutionId) &&
                    command.ChoiceId.Equals(choice.ChoiceId) &&
                    command.Coordinate.Equals(destination)));
        }

        /// <summary>
        /// Projects only the system-transition family whose answers are board spaces. Upgrade a Card
        /// remains a card-faced HUD decision, and an absent or unknown description has no board surface.
        /// </summary>
        public static IReadOnlyList<SystemTransitionDestinationViewModel>
            CreateSystemTransitionDestinations(IMatchReadModel projection) =>
            CreateSystemTransitionDestinations(
                projection?.PendingSystemTransitionDescription,
                projection?.DecisionContract);

        public static IReadOnlyList<SystemTransitionDestinationViewModel>
            CreateSystemTransitionDestinations(
                SystemTransitionStepDescription description,
                PendingDecisionContract contract)
        {
            if (description == null || description.Kind != SystemTransitionPromptKind.LanePushRespawnPlace)
            {
                return Array.Empty<SystemTransitionDestinationViewModel>();
            }

            return Array.AsReadOnly(description.Options.Select(option =>
            {
                var invocation = description.PendingStep.Invocations.Single(candidate =>
                    candidate.OptionId.Equals(option.OptionId));
                return new SystemTransitionDestinationViewModel(
                    option.Destination,
                    PendingDecisionCommandBinding.Require(
                        contract,
                        PendingDecisionFamilies.SystemTransition,
                        command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                            command.SystemTransitionChoice.Equals(invocation)));
            }).ToArray());
        }

        /// <summary>
        /// Milestone 9.21i. A click on one standee: the body it draws is part of the answer. It answers
        /// "which of you acts" with that body (RZ-Q4), an Attack aimed at that body (RZ-Q3), or an
        /// effect choice at that body's space - each through the standing contract's own command. For
        /// a hero on one space it is exactly <see cref="SubmitHero"/>.
        /// </summary>
        public bool SubmitHeroBody(HeroId heroId, int body)
        {
            var projection = session.Projection;
            var pick = projection.PendingActingBodyChoice;
            if (pick != null && pick.HeroId.Equals(heroId))
            {
                return pick.Bodies.Any(value => value.Body == body) &&
                    Execute(PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.ActingBody,
                        command => command.Kind == DirectorCommandKind.ChooseActingBody &&
                            command.HeroId.Equals(heroId) &&
                            command.HeroBody == body));
            }

            if (!StandsOnSeveralSpaces(projection, heroId))
            {
                return SubmitHero(heroId);
            }

            var choice = projection.PendingAttackTargetChoice;
            if (choice != null && choice.LegalTargets.Any(value =>
                    value.Kind == AttackTargetKind.Hero && value.HeroId.Equals(heroId) &&
                    value.HeroBody == body))
            {
                return Execute(PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.AttackTarget,
                    command => command.Kind == DirectorCommandKind.ChooseAttackTarget &&
                        command.ResolutionId.Equals(choice.ResolutionId) &&
                        command.ChoiceId.Equals(choice.ChoiceId) &&
                        command.HeroId.Equals(heroId) &&
                        (command.HeroBody == body || command.HeroBody == 0)));
            }

            var standing = projection.Heroes.FirstOrDefault(value =>
                value.HeroId.Equals(heroId) && value.Body == body);
            return standing.HeroId.IsDefined && SubmitEffectChoiceAtSpace(standing.Coordinate);
        }

        public bool SubmitHero(HeroId heroId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            if (choice == null || !choice.LegalTargetHeroIds.Contains(heroId))
            {
                // Milestone 8.11a: a hero an effect choice names is answered by clicking the hero, the
                // same click that takes an Attack target. It is routed through the space the hero
                // stands on, because that is what the candidate carries.
                var occupancy = session.Projection.Heroes
                    .FirstOrDefault(value => value.HeroId.Equals(heroId));
                return occupancy.HeroId.IsDefined && SubmitEffectChoiceAtSpace(occupancy.Coordinate);
            }

            // Milestone 9.21i. With two of his bodies offered, a click naming only the hero answers
            // nothing: the body is the answer (RZ-Q3), and it arrives through SubmitHeroBody.
            if (choice.LegalTargets.Count(value =>
                    value.Kind == AttackTargetKind.Hero && value.HeroId.Equals(heroId)) > 1)
            {
                return false;
            }

            return Execute(PendingDecisionCommandBinding.Require(
                session.Projection,
                PendingDecisionFamilies.AttackTarget,
                command => command.Kind == DirectorCommandKind.ChooseAttackTarget &&
                    command.ResolutionId.Equals(choice.ResolutionId) &&
                    command.ChoiceId.Equals(choice.ChoiceId) &&
                    command.HeroId.Equals(heroId)));
        }

        /// <summary>
        /// Milestone 8.11a. Answers a pending effect choice with the candidate standing on this space,
        /// if one does. Shared by the hero, minion and empty-space clicks, because all three are the
        /// same question asked of the same board.
        /// </summary>
        private bool SubmitEffectChoiceAtSpace(HexCoordinate space)
        {
            // Milestone 8.46d. A piece standing on a neighbouring hex is consumed by the hero and
            // minion selectors before the board plane is ever asked, so an arrow drawn under a piece
            // would be unanswerable if this route lived only in SubmitDestination. The click means
            // the same thing wherever it was claimed: the arrow on that space.
            var chosenDirection = FindDirectionCandidate(session.Projection, space);
            if (chosenDirection != null)
            {
                return SubmitEffectChoiceSelection(chosenDirection.Command);
            }

            var effectChoice = session.Projection.PendingEffectChoice;
            if (effectChoice == null ||
                !SelectableEffectChoiceSpaces(session.Projection).Contains(space))
            {
                return false;
            }

            // Milestone 8.11c: the click names a space, the preview says which candidate value stands
            // there, and the invocation is found by that value - so a candidate naming a hero by
            // identity is reached by the same click as one naming the space it stands on.
            var standing = session.Projection.PendingEffectChoicePreview
                .Where(preview => preview.Coordinate.Equals(space))
                .Select(preview => preview.CandidateValue)
                .ToArray();
            if (standing.Length != 1)
            {
                return false;
            }

            var selection = effectChoice.SelectInvocations
                .Concat(effectChoice.DeselectInvocations)
                .SingleOrDefault(value =>
                value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                value.Candidate.Value.Equals(standing[0]));
            return selection != null && SubmitEffectChoiceSelection(
                PendingDecisionCommandBinding.Require(
                    session.Projection,
                    PendingDecisionFamilies.Effect,
                    command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(command.EffectChoice, selection)));
        }

        public bool SubmitMinion(MinionId minionId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            if (choice == null || !choice.LegalTargetMinionIds.Contains(minionId))
            {
                // Milestone 8.11a: as for a hero, a minion an effect choice names is answered by
                // clicking it.
                var occupancy = session.Projection.Minions
                    .FirstOrDefault(value => value.MinionId.Equals(minionId));
                return occupancy != null && SubmitEffectChoiceAtSpace(occupancy.Coordinate);
            }

            return Execute(PendingDecisionCommandBinding.Require(
                session.Projection,
                PendingDecisionFamilies.AttackTarget,
                command => command.Kind == DirectorCommandKind.ChooseAttackTarget &&
                    command.ResolutionId.Equals(choice.ResolutionId) &&
                    command.ChoiceId.Equals(choice.ChoiceId) &&
                    command.MinionId.Equals(minionId)));
        }

        /// <summary>
        /// Forwards one projected select invocation and, for an exactly-one choice, the confirm the
        /// authority offers immediately afterwards. Both are the authority's own commands.
        /// </summary>
        private bool SubmitEffectChoiceSelection(DirectorCommand command)
        {
            if (command == null || command.Kind != DirectorCommandKind.AnswerEffectChoice)
            {
                return false;
            }

            var result = session.Submit(command);
            if (!result.Accepted)
            {
                return false;
            }

            SessionProjectionChanged?.Invoke(result.Projection);
            var confirm = EffectChoiceCompletion.FindImmediateConfirm(
                result.Projection, command.EffectChoice);
            if (confirm != null)
            {
                Execute(confirm);
            }

            return true;
        }

        private FastTravelOptionProjection GetSelectedFastTravelOption(IMatchReadModel projection)
        {
            if (!ReferenceEquals(projection, fastTravelSelectionProjection) ||
                !selectedFastTravelActionDefinitionId.HasValue)
            {
                return null;
            }

            return projection.AvailableFastTravelOptions.SingleOrDefault(option =>
                option.ReplacedMovementActionDefinitionId.Equals(
                    selectedFastTravelActionDefinitionId.Value) && option.LegalDestinations.Count > 0);
        }

        private bool Execute(DirectorCommand command)
        {
            var result = session.Submit(command);
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }
}
