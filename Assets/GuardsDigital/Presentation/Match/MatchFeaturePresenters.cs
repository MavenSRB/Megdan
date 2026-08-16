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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Units.Minions;

namespace GuardsDigital.Presentation.Match
{
    public readonly struct MinionDefenseCueViewModel
    {
        public MinionDefenseCueViewModel(MinionId minionId, int amount)
        {
            if (!minionId.IsDefined || (amount != -1 && amount != 1))
            {
                throw new ArgumentException("A minion Defense cue requires a stable identity and signed unit contribution.");
            }

            MinionId = minionId;
            Amount = amount;
        }

        public MinionId MinionId { get; }
        public int Amount { get; }
    }

    /// <summary>
    /// What a board cue says about one minion in a pending minion choice. Both visible kinds point
    /// at the same thing - this minion - so the board draws one marker for both and distinguishes
    /// them by color: a selected minion keeps its marker while the pointer moves across the other
    /// controls, and a highlighted one answers "which minion is this control".
    /// </summary>
    public readonly struct MinionChoiceCueViewModel
    {
        public MinionChoiceCueViewModel(MinionId minionId, MinionChoiceCueKind kind)
        {
            if (!minionId.IsDefined || kind == MinionChoiceCueKind.None)
            {
                throw new ArgumentException(
                    "A minion choice cue requires a stable identity and a visible kind.");
            }

            MinionId = minionId;
            Kind = kind;
        }

        public MinionId MinionId { get; }
        public MinionChoiceCueKind Kind { get; }
    }

    public readonly struct HeroChoiceCueViewModel
    {
        public HeroChoiceCueViewModel(HeroId heroId, MinionChoiceCueKind kind)
        {
            if (!heroId.IsDefined || kind == MinionChoiceCueKind.None)
            {
                throw new ArgumentException(
                    "A hero choice cue requires a stable identity and a visible kind.");
            }

            HeroId = heroId;
            Kind = kind;
        }

        public HeroId HeroId { get; }
        public MinionChoiceCueKind Kind { get; }
    }

    /// <summary>
    /// The one place the minion-choice prompt names a candidate. The ordinal is the candidate's
    /// position in the projection's own ordinal candidate order, so two candidates of the same team
    /// and type stay distinguishable by text rather than by color.
    /// </summary>
    public static class MinionChoiceLabels
    {
        public static string TeamLabel(TeamSide side) =>
            side == TeamSide.Red ? "[R] RED" : "[B] BLUE";

        public static string CandidateLabel(TeamSide side, StandardMinionType minionType, int ordinal)
        {
            if (ordinal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return $"{TeamLabel(side)} {minionType.ToString().ToUpperInvariant()} #{ordinal}";
        }
    }

    /// <summary>
    /// The one place the effect-choice prompt names a candidate. Milestone 6.8a added it when the first
    /// configured card carried a Skill: a mode is identified by its ordinal in the projection's own
    /// candidate order and by the effect node's stable ID, both definition data. Printed card text is
    /// never read here, and no label may become the thing a rule branches on.
    /// </summary>
    public static class EffectChoiceLabels
    {
        public static string CandidateLabel(EffectNodeId optionNodeId, int ordinal)
        {
            if (ordinal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return optionNodeId.IsDefined
                ? $"MODE #{ordinal} {optionNodeId.Value.ToUpperInvariant()}"
                : $"OPTION #{ordinal}";
        }

        public static string DeselectLabel(EffectNodeId optionNodeId, int ordinal) =>
            $"UNDO {CandidateLabel(optionNodeId, ordinal)}";

        public const string ConfirmLabel = "CONFIRM CHOICE";
        public const string DeclineLabel = "DECLINE";

        /// <summary>
        /// Milestone 6.5c: a value candidate that names a board object is named after that object, so
        /// a player reads who a control targets instead of an ordinal. Every part is projected state -
        /// the configured display name, the projected team and intrinsic type, the candidate's own
        /// ordinal - and none of it is printed card text or a rules answer. The ordinal stays because
        /// two candidates of one team and type are otherwise indistinguishable, which is exactly why
        /// milestone 6.1a put it on the minion prompt.
        /// </summary>
        public static string HeroTargetLabel(TeamSide side, string displayName) =>
            string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("A hero target label requires the projected display name.", nameof(displayName))
                : $"{MinionChoiceLabels.TeamLabel(side)} {displayName.ToUpperInvariant()}";

        public static string MinionTargetLabel(TeamSide side, StandardMinionType minionType, int ordinal) =>
            MinionChoiceLabels.CandidateLabel(side, minionType, ordinal);

        /// <summary>
        /// Milestone 6.2d: a token is named by what it is.
        ///
        /// Milestone 6.5c asked for the catalog name here and settled for an ordinal, because 6.2's
        /// decision left presentation with no token projection to read one from. That decision is
        /// retired: the projected instance carries its trusted definition, so the display name is
        /// projected state exactly as a hero's is. The ordinal is kept only where two candidates
        /// would otherwise read identically - the same rule the minion label follows, and the reason
        /// the hero label carries no ordinal at all.
        /// </summary>
        public static string TokenTargetLabel(string displayName, int? ordinal) =>
            string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException(
                    "A token target label requires the projected display name.", nameof(displayName))
                : !ordinal.HasValue
                    ? displayName.ToUpperInvariant()
                    : ordinal.Value < 1
                        ? throw new ArgumentOutOfRangeException(nameof(ordinal))
                        : $"{displayName.ToUpperInvariant()} #{ordinal.Value}";
    }

    public sealed class BoardInteractionViewModel
    {
        public BoardInteractionViewModel(
            long version,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<MinionOccupancy> minions,
            IEnumerable<HexCoordinate> movementDestinations,
            IEnumerable<HeroId> attackTargets,
            IEnumerable<MinionId> minionAttackTargets,
            HeroId focusedHeroId,
            IEnumerable<PlacedToken> placedTokens = null,
            IEnumerable<HexCoordinate> fastTravelDestinations = null,
            IEnumerable<MinionDefenseCueViewModel> minionDefenseCues = null,
            IEnumerable<MinionChoiceCueViewModel> minionChoiceCues = null,
            HexCoordinate? correctionDestination = null,
            IEnumerable<HexCoordinate> effectChoiceDestinations = null,
            HexCoordinate? effectChoiceTargetSpace = null,
            HexCoordinate? effectChoiceResultingSpace = null,
            HexCoordinate? heroRespawnHighlight = null,
            IEnumerable<HeroChoiceCueViewModel> heroChoiceCues = null)
        {
            Version = version;
            Heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes))).ToArray());
            Minions = Array.AsReadOnly((minions ?? throw new ArgumentNullException(nameof(minions))).ToArray());
            PlacedTokens = Array.AsReadOnly((placedTokens ??
                Enumerable.Empty<PlacedToken>()).ToArray());
            MovementDestinations = Array.AsReadOnly((movementDestinations ??
                throw new ArgumentNullException(nameof(movementDestinations))).ToArray());
            AttackTargets = Array.AsReadOnly((attackTargets ??
                throw new ArgumentNullException(nameof(attackTargets))).ToArray());
            MinionAttackTargets = Array.AsReadOnly((minionAttackTargets ??
                throw new ArgumentNullException(nameof(minionAttackTargets))).ToArray());
            FocusedHeroId = focusedHeroId;
            FastTravelDestinations = Array.AsReadOnly((fastTravelDestinations ??
                Enumerable.Empty<HexCoordinate>()).ToArray());
            MinionDefenseCues = Array.AsReadOnly((minionDefenseCues ??
                Enumerable.Empty<MinionDefenseCueViewModel>()).ToArray());
            MinionChoiceCues = Array.AsReadOnly((minionChoiceCues ??
                Enumerable.Empty<MinionChoiceCueViewModel>()).ToArray());
            HeroChoiceCues = Array.AsReadOnly((heroChoiceCues ??
                Enumerable.Empty<HeroChoiceCueViewModel>()).ToArray());
            if (MinionChoiceCues.Select(value => value.MinionId).Distinct().Count() !=
                MinionChoiceCues.Count)
            {
                throw new ArgumentException(
                    "One minion carries at most one choice cue.",
                    nameof(minionChoiceCues));
            }

            CorrectionDestination = correctionDestination;
            EffectChoiceDestinations = Array.AsReadOnly((effectChoiceDestinations ??
                Enumerable.Empty<HexCoordinate>()).ToArray());
            EffectChoiceTargetSpace = effectChoiceTargetSpace;
            EffectChoiceResultingSpace = effectChoiceResultingSpace;
            HeroRespawnHighlight = heroRespawnHighlight;
            if (effectChoiceResultingSpace.HasValue && !effectChoiceTargetSpace.HasValue)
            {
                throw new ArgumentException(
                    "A resulting space belongs to the target being pointed at.",
                    nameof(effectChoiceResultingSpace));
            }

            if (effectChoiceTargetSpace.HasValue &&
                EffectChoiceDestinations.Contains(effectChoiceTargetSpace.Value))
            {
                throw new ArgumentException(
                    "A space is either an empty destination or a target, never both.",
                    nameof(effectChoiceTargetSpace));
            }
        }

        public long Version { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<MinionOccupancy> Minions { get; }

        /// <summary>
        /// Milestone 6.2d: the tokens standing on the board, straight off the projection. The board
        /// draws one piece per entry and nothing else reads them, which is what keeps a token a thing
        /// that is shown rather than a thing presentation reasons about.
        /// </summary>
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }
        public IReadOnlyList<HexCoordinate> MovementDestinations { get; }
        public IReadOnlyList<HeroId> AttackTargets { get; }
        public IReadOnlyList<MinionId> MinionAttackTargets { get; }
        public HeroId FocusedHeroId { get; }
        public IReadOnlyList<HexCoordinate> FastTravelDestinations { get; }
        public IReadOnlyList<MinionDefenseCueViewModel> MinionDefenseCues { get; }
        public IReadOnlyList<MinionChoiceCueViewModel> MinionChoiceCues { get; }
        public IReadOnlyList<HeroChoiceCueViewModel> HeroChoiceCues { get; }

        /// <summary>
        /// The pending correction's route endpoint or Place space that the player is currently
        /// pointing at, and no other.
        ///
        /// Milestone 6.4b projected every legal endpoint as a list, and every one of them stayed lit
        /// for as long as the choice stood, so the board said only that several spaces were involved.
        /// Milestone 6.2d made it a single value rather than emptying a list, because that is the
        /// difference between a rule and a habit: a nullable cannot regress to a standing set, and it
        /// is the same shape <see cref="HeroRespawnHighlight"/> has carried since that prompt was
        /// built. What is legal has not changed - the controls are still one per endpoint, and a
        /// click still answers the same way.
        /// </summary>
        public HexCoordinate? CorrectionDestination { get; }

        /// <summary>
        /// Milestone 6.5c: the empty spaces a pending effect choice currently offers. They are the
        /// whole surface for that kind of candidate - a space the actor may take carries no prompt
        /// control, because the board already says everything the control would have.
        /// </summary>
        public IReadOnlyList<HexCoordinate> EffectChoiceDestinations { get; }

        /// <summary>The space of the effect-choice target the pointer or keyboard focus is on, if any.</summary>
        public HexCoordinate? EffectChoiceTargetSpace { get; }

        /// <summary>
        /// Where the authority projects that target would end up. Present only alongside a pointed-at
        /// target, and never drawn as a destination: it is a consequence, not a choice.
        /// </summary>
        public HexCoordinate? EffectChoiceResultingSpace { get; }
        public HexCoordinate? HeroRespawnHighlight { get; }
    }

    public interface IBoardInteractionView
    {
        void Render(BoardInteractionViewModel model);
    }

    /// <summary>
    /// Milestone 6.5c: how an effect-choice selection reaches its end.
    ///
    /// An exactly-one choice - `Choose one`, one target, one destination - is finished the moment the
    /// single selection is made, but the authority still models the answer as select then confirm, so
    /// a player who has already decided is asked to say so twice. Presentation closes that by
    /// forwarding both invocations the authority itself offers, in the order it offers them; it
    /// invents no command, decides no legality, and changes nothing about how the choice confirms.
    /// A choice with any other bounds keeps its explicit confirm, because there the selection really
    /// is unfinished.
    ///
    /// It applies to a value candidate only. A mode is the one decision whose control set milestone
    /// 6.5c leaves exactly as milestone 6.8a built it, and taking a mode back before committing to it
    /// is worth the second click; a target or a destination is picked off the board, where the pick
    /// itself is the commitment.
    /// </summary>
    internal static class EffectChoiceCompletion
    {
        public static DirectorCommand FindImmediateConfirm(
            MatchSessionProjection projection, EffectChoiceCommand accepted)
        {
            var choice = projection?.PendingEffectChoice;
            if (choice == null || accepted == null ||
                accepted.Kind != EffectChoiceCommandKind.Select ||
                accepted.Candidate.Kind != EffectChoiceCandidateKind.Value ||
                !choice.ChoiceId.Equals(accepted.ChoiceId) ||
                !choice.ResolutionId.Equals(accepted.ResolutionId) ||
                choice.MinimumSelections != 1 || choice.MaximumSelections != 1 ||
                choice.ConfirmInvocation == null)
            {
                return null;
            }

            return DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation);
        }
    }

    public sealed class BoardInteractionPresenter
    {
        private readonly IMatchSession session;
        private readonly IBoardInteractionView view;
        private MatchSessionProjection lastProjection;
        private BoardInteractionViewModel lastViewModel;
        private MatchSessionProjection fastTravelSelectionProjection;
        private ActionDefinitionId? selectedFastTravelActionDefinitionId;
        private MatchSessionProjection choiceHighlightProjection;
        private RuleObject highlightedChoiceUnit;
        private HexCoordinate? highlightedCorrectionDestination;
        private MatchSessionProjection effectChoiceHighlightProjection;
        private HexCoordinate? highlightedEffectChoiceSpace;
        private MatchSessionProjection respawnHighlightProjection;
        private HexCoordinate? highlightedRespawnSpace;

        public BoardInteractionPresenter(IMatchSession session, IBoardInteractionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

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

            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
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
                    CreateMinionChoiceCues(
                        projection.PendingMinionBattleChoice,
                        projection.PendingDisplacedMinionCorrection,
                        highlightedChoiceUnit.Kind == RuleObjectKind.Minion
                            ? highlightedChoiceUnit.MinionId
                            : default),
                    highlightedCorrectionDestination,
                    SelectableEffectChoiceDestinations(projection),
                    highlightedEffectChoiceSpace,
                    FindResultingSpace(projection, highlightedEffectChoiceSpace),
                    highlightedRespawnSpace,
                    CreateHeroChoiceCues(
                        projection.PendingMinionBattleChoice,
                        highlightedChoiceUnit));
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
            MatchSessionProjection projection)
        {
            var choice = projection.PendingEffectChoice;
            if (choice == null || projection.PendingEffectChoicePreview.Count == 0)
            {
                return Array.Empty<HexCoordinate>();
            }

            var empty = new HashSet<HexCoordinate>(projection.PendingEffectChoicePreview
                .Where(preview => !preview.NamesObject)
                .Select(preview => preview.Coordinate));
            return choice.SelectInvocations
                .Where(invocation =>
                    invocation.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    empty.Contains(invocation.Candidate.Value.Coordinate))
                .Select(invocation => invocation.Candidate.Value.Coordinate)
                .ToArray();
        }

        private static HexCoordinate? FindResultingSpace(
            MatchSessionProjection projection, HexCoordinate? targetSpace)
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

        private static IReadOnlyList<MinionChoiceCueViewModel> CreateMinionChoiceCues(
            PendingMinionBattleChoice choice,
            PendingDisplacedMinionCorrection correction,
            MinionId highlightedMinionId)
        {
            if (choice != null)
            {
                var cues = new List<MinionChoiceCueViewModel>();
                foreach (var candidate in choice.Candidates.Where(candidate =>
                    candidate.MinionId.IsDefined))
                {
                    var kind = choice.SelectedMinionIds.Contains(candidate.MinionId)
                        ? MinionChoiceCueKind.Selected
                        : candidate.MinionId.Equals(highlightedMinionId)
                            ? MinionChoiceCueKind.Highlighted
                            : MinionChoiceCueKind.None;
                    if (kind == MinionChoiceCueKind.None)
                    {
                        continue;
                    }

                    cues.Add(new MinionChoiceCueViewModel(candidate.MinionId, kind));
                }

                return cues;
            }

            // Milestone 6.4b: the correction's board cues ride the same vocabulary. During a subject
            // choice the pointed-at candidate is highlighted; during a destination choice the one
            // displaced subject is marked selected, so the board says which minion the offered
            // destinations would receive.
            if (correction == null)
            {
                return Array.Empty<MinionChoiceCueViewModel>();
            }

            if (correction.Stage == DisplacedMinionCorrectionStage.SelectSubject)
            {
                return correction.SubjectCandidates
                    .Where(subject => subject.MinionId.Equals(highlightedMinionId))
                    .Select(subject => new MinionChoiceCueViewModel(
                        subject.MinionId, MinionChoiceCueKind.Highlighted))
                    .ToArray();
            }

            return new[]
            {
                new MinionChoiceCueViewModel(correction.SubjectMinionId, MinionChoiceCueKind.Selected)
            };
        }

        private static IReadOnlyList<HeroChoiceCueViewModel> CreateHeroChoiceCues(
            PendingMinionBattleChoice choice,
            RuleObject highlightedUnit)
        {
            if (choice == null)
            {
                return Array.Empty<HeroChoiceCueViewModel>();
            }

            return choice.Candidates
                .Where(candidate => candidate.HeroId.IsDefined)
                .Select(candidate =>
                {
                    var kind = choice.SelectedUnits.Contains(candidate.Unit)
                        ? MinionChoiceCueKind.Selected
                        : highlightedUnit.Equals(candidate.Unit)
                            ? MinionChoiceCueKind.Highlighted
                            : MinionChoiceCueKind.None;
                    return new { candidate.HeroId, Kind = kind };
                })
                .Where(value => value.Kind != MinionChoiceCueKind.None)
                .Select(value => new HeroChoiceCueViewModel(value.HeroId, value.Kind))
                .ToArray();
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
            // Milestone 6.4b: a correction destination is selectable straight off the board. The
            // families are mutually exclusive - a correction pauses mid-action, when no movement
            // choice or fast-travel activation can be live - so this is a route, not a priority.
            var correction = session.Projection.PendingDisplacedMinionCorrection;
            if (correction?.Stage == DisplacedMinionCorrectionStage.SelectDestination)
            {
                var invocation = correction.SelectInvocations.SingleOrDefault(value =>
                    value.Destination.Equals(destination));
                return invocation != null &&
                    Execute(DirectorCommand.ChooseCorrectionDestination(invocation));
            }

            // Milestone 6.5c: a lit effect-choice space is answered from the board alone. Same
            // exclusivity as the correction above - a paused effect frame forbids a movement choice
            // or a fast-travel activation - so this is a route rather than a priority.
            var effectChoice = session.Projection.PendingEffectChoice;
            if (effectChoice != null)
            {
                // Only an empty space answers from the board. A candidate that names an object is
                // answered by its named control, so a click on an occupied space selects nothing.
                if (!SelectableEffectChoiceDestinations(session.Projection).Contains(destination))
                {
                    return false;
                }

                var selection = effectChoice.SelectInvocations.SingleOrDefault(value =>
                    value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    value.Candidate.Value.Coordinate.Equals(destination));
                return selection != null && SubmitEffectChoiceSelection(selection);
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

            return Execute(DirectorCommand.ChooseMovementDestination(
                choice.ResolutionId,
                choice.ChoiceId,
                destination));
        }

        public bool SubmitHero(HeroId heroId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            if (choice == null || !choice.LegalTargetHeroIds.Contains(heroId))
            {
                return false;
            }

            return Execute(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId,
                choice.ChoiceId,
                heroId));
        }

        public bool SubmitMinion(MinionId minionId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            if (choice == null || !choice.LegalTargetMinionIds.Contains(minionId))
            {
                return false;
            }

            return Execute(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId,
                choice.ChoiceId,
                minionId));
        }

        /// <summary>
        /// Forwards one projected select invocation and, for an exactly-one choice, the confirm the
        /// authority offers immediately afterwards. Both are the authority's own commands.
        /// </summary>
        private bool SubmitEffectChoiceSelection(EffectChoiceCommand selection)
        {
            var result = session.Execute(DirectorCommand.AnswerEffectChoice(selection));
            if (!result.Accepted)
            {
                return false;
            }

            SessionProjectionChanged?.Invoke(result.Projection);
            var confirm = EffectChoiceCompletion.FindImmediateConfirm(result.Projection, selection);
            if (confirm != null)
            {
                Execute(confirm);
            }

            return true;
        }

        private FastTravelOptionProjection GetSelectedFastTravelOption(MatchSessionProjection projection)
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
            var result = session.Execute(command);
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }

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
            CardInstanceId? selectedCardInstanceId)
        {
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
        public bool CanPass { get; }
        public CardInstanceId? RevealedCardInstanceId { get; }
        public CardInstance RevealedCard { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public CardInstanceId? SelectedCardInstanceId { get; }
        public string HeaderLabel { get; }
        public string CommitmentLabel { get; }
        public IReadOnlyDictionary<CardInstanceId, string> CardButtonLabels { get; }

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
        public DirectorHandsViewModel(long version, IEnumerable<DirectorHandViewModel> hands)
        {
            Version = version;
            Hands = Array.AsReadOnly((hands ?? throw new ArgumentNullException(nameof(hands))).ToArray());
        }

        public long Version { get; }
        public IReadOnlyList<DirectorHandViewModel> Hands { get; }
    }

    public interface IDirectorHandsView
    {
        void Render(DirectorHandsViewModel model);
    }

    public sealed class DirectorHandsPresenter
    {
        private readonly IMatchSession session;
        private readonly IDirectorHandsView view;
        private CardInstanceId? selectedCardInstanceId;
        private MatchSessionProjection lastProjection;
        private CardInstanceId? lastSelectedCardInstanceId;
        private DirectorHandsViewModel lastViewModel;

        public DirectorHandsPresenter(IMatchSession session, IDirectorHandsView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (selectedCardInstanceId.HasValue && !projection.Cards.Any(card =>
                card.Id.Equals(selectedCardInstanceId.Value) && card.State.Location == CardLocation.Hand))
            {
                selectedCardInstanceId = null;
            }

            if (!ReferenceEquals(projection, lastProjection) ||
                !Nullable.Equals(selectedCardInstanceId, lastSelectedCardInstanceId))
            {
                var hands = projection.Setup.Heroes
                    .OrderBy(hero => hero.TeamSide)
                    .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                    .Select(hero =>
                    {
                        var commitment = projection.GetCommitment(hero.HeroId);
                        return new DirectorHandViewModel(
                            hero.TeamSide,
                            hero.HeroId,
                            hero.DisplayName,
                            commitment.Status,
                            commitment.CanPass,
                            commitment.RevealedCardInstanceId,
                            commitment.RevealedCardInstanceId.HasValue
                                ? projection.Cards.Single(card =>
                                    card.Id.Equals(commitment.RevealedCardInstanceId.Value))
                                : null,
                            projection.Cards.Where(card =>
                                card.OwnerId.Equals(hero.HeroId) && card.State.Location == CardLocation.Hand),
                            projection.Definitions,
                            selectedCardInstanceId);
                    });
                lastProjection = projection;
                lastSelectedCardInstanceId = selectedCardInstanceId;
                lastViewModel = new DirectorHandsViewModel(projection.Version, hands);
            }

            view.Render(lastViewModel);
        }

        public bool SelectCommitmentCard(CardInstanceId cardInstanceId)
        {
            var projection = session.Projection;
            var card = projection.Cards.SingleOrDefault(value => value.Id.Equals(cardInstanceId));
            if (projection.Phase != RoundResolutionPhase.AwaitingCommitments || card == null ||
                card.State.Location != CardLocation.Hand ||
                projection.GetCommitment(card.OwnerId).Status != TurnCardCommitmentStatus.AwaitingResponse)
            {
                return false;
            }

            selectedCardInstanceId = cardInstanceId;
            Refresh();
            return true;
        }

        public bool ConfirmSelectedCard()
        {
            if (!selectedCardInstanceId.HasValue)
            {
                return false;
            }

            var card = session.Projection.Cards.SingleOrDefault(value => value.Id.Equals(selectedCardInstanceId.Value));
            if (card == null)
            {
                return false;
            }

            var result = session.Execute(DirectorCommand.CommitCard(card.OwnerId, card.Id));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        public bool Pass(HeroId heroId)
        {
            var commitment = session.Projection.Commitments.SingleOrDefault(value => value.HeroId.Equals(heroId));
            if (!commitment.HeroId.IsDefined || !commitment.CanPass)
            {
                return false;
            }

            var result = session.Execute(DirectorCommand.Pass(heroId));
            if (result.Accepted)
            {
                selectedCardInstanceId = null;
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }

    public sealed class TypedInvocationViewModel
    {
        public TypedInvocationViewModel(
            string label,
            DirectorCommand command,
            DefenseBreakdownViewModel defenseBreakdown = null)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A typed invocation requires a label.", nameof(label))
                : label;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            DefenseBreakdown = defenseBreakdown;
        }

        public string Label { get; }
        public DirectorCommand Command { get; }
        public DefenseBreakdownViewModel DefenseBreakdown { get; }
    }

    public sealed class DefenseBreakdownViewModel
    {
        public DefenseBreakdownViewModel(
            long version,
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            CardInstanceId? cardInstanceId,
            string title,
            string text,
            AttackResolutionOutcome outcome)
        {
            Version = version;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            CardInstanceId = cardInstanceId;
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("A Defense breakdown requires a title.", nameof(title))
                : title;
            Text = string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException("A Defense breakdown requires text.", nameof(text))
                : text;
            Outcome = outcome;
        }

        public long Version { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public CardInstanceId? CardInstanceId { get; }
        public string Title { get; }
        public string Text { get; }
        public AttackResolutionOutcome Outcome { get; }
    }

    public sealed class FastTravelOfferViewModel
    {
        public FastTravelOfferViewModel(
            string label,
            ActionDefinitionId replacedMovementActionDefinitionId,
            int legalDestinationCount)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Fast Travel offer requires a label.", nameof(label))
                : label;
            if (!replacedMovementActionDefinitionId.IsDefined || legalDestinationCount <= 0)
            {
                throw new ArgumentException("A Fast Travel offer requires its replaced action and legal destinations.");
            }

            ReplacedMovementActionDefinitionId = replacedMovementActionDefinitionId;
            LegalDestinationCount = legalDestinationCount;
        }

        public string Label { get; }
        public ActionDefinitionId ReplacedMovementActionDefinitionId { get; }
        public int LegalDestinationCount { get; }
    }

    /// <summary>
    /// Milestone 6.5c: one control of a pending effect choice that names a board object. The space is
    /// the object's identity for this purpose - the same space-as-identity the effect definition binds
    /// - so hovering the control and lighting the board agree without presentation holding a second
    /// notion of which object a control means.
    /// </summary>
    public sealed class EffectChoiceTargetViewModel
    {
        public EffectChoiceTargetViewModel(
            string label,
            HexCoordinate space,
            HexCoordinate? resultingSpace,
            DirectorCommand command)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("An effect choice target requires a label.", nameof(label))
                : label;
            Space = space;
            ResultingSpace = resultingSpace;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Label { get; }
        public HexCoordinate Space { get; }

        /// <summary>Where the authority projects this target would end up, when an operation moves it.</summary>
        public HexCoordinate? ResultingSpace { get; }
        public DirectorCommand Command { get; }
    }

    /// <summary>
    /// Milestone 6.5ac: one option of a pending effect choice the acting player cannot perform.
    ///
    /// It carries a label and the candidate it stands for, and deliberately no
    /// <see cref="DirectorCommand"/> at all - which is why it is a type of its own rather than a
    /// <see cref="TypedInvocationViewModel"/> with a flag beside it. A control with no command cannot
    /// be submitted through any route a view has, so "greyed" is a structural fact here rather than a
    /// rendering convention a later view could forget.
    ///
    /// There is no reason text. The 2026-08-16 user decision recorded as open rules question 11 is
    /// that greying alone is the surface this owes.
    /// </summary>
    /// <summary>
    /// Milestone 6.5af: one printed action the authority judged unstartable, drawn rather than
    /// dropped. It carries no <c>DirectorCommand</c> at all, for the reason a refused mode does not:
    /// a click cannot reach the session by any route rather than being refused once it arrives.
    /// </summary>
    public sealed class UnavailableActionViewModel
    {
        public UnavailableActionViewModel(string label, ActionDefinitionId actionDefinitionId)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException(
                    "An unavailable action requires a label.", nameof(label))
                : label;
            ActionDefinitionId = actionDefinitionId.IsDefined
                ? actionDefinitionId
                : throw new ArgumentException(
                    "An unavailable action names the action it stands for.", nameof(actionDefinitionId));
        }

        public string Label { get; }

        /// <summary>
        /// The action this control would have started. Presentation holds it so the presenter can
        /// refuse a command naming it rather than only failing to offer one.
        /// </summary>
        public ActionDefinitionId ActionDefinitionId { get; }
    }

    public sealed class UnavailableEffectChoiceViewModel
    {
        public UnavailableEffectChoiceViewModel(string label, EffectChoiceCandidate candidate)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException(
                    "An unavailable effect choice requires a label.", nameof(label))
                : label;
            Candidate = candidate.IsDefined
                ? candidate
                : throw new ArgumentException(
                    "An unavailable effect choice names the candidate it stands for.", nameof(candidate));
        }

        public string Label { get; }

        /// <summary>
        /// The option this control would have answered. Presentation holds it so the presenter can
        /// refuse a command naming it rather than only failing to offer one.
        /// </summary>
        public EffectChoiceCandidate Candidate { get; }
    }

    public sealed class RoundActionViewModel
    {
        public RoundActionViewModel(
            long version,
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            string status,
            IEnumerable<TypedInvocationViewModel> actions,
            TypedInvocationViewModel endTurnInvocation,
            TypedInvocationViewModel continueEndOfRoundInvocation,
            IEnumerable<TypedInvocationViewModel> initiativeChoices,
            IEnumerable<TypedInvocationViewModel> defenseChoices,
            IEnumerable<FastTravelOfferViewModel> fastTravelOffers = null,
            IEnumerable<TypedInvocationViewModel> effectChoices = null,
            IEnumerable<EffectChoiceTargetViewModel> effectChoiceTargets = null,
            IEnumerable<TypedInvocationViewModel> effectChoiceDestinations = null,
            IEnumerable<HeroRespawnOptionViewModel> heroRespawnOptions = null,
            TypedInvocationViewModel resolveWithoutRespawn = null,
            IEnumerable<UnavailableEffectChoiceViewModel> unavailableEffectChoices = null,
            IEnumerable<UnavailableActionViewModel> unavailableActions = null)
        {
            Version = version;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Actions = Array.AsReadOnly((actions ?? throw new ArgumentNullException(nameof(actions))).ToArray());
            EndTurnInvocation = endTurnInvocation;
            ContinueEndOfRoundInvocation = continueEndOfRoundInvocation;
            InitiativeChoices = Array.AsReadOnly((initiativeChoices ??
                throw new ArgumentNullException(nameof(initiativeChoices))).ToArray());
            DefenseChoices = Array.AsReadOnly((defenseChoices ??
                throw new ArgumentNullException(nameof(defenseChoices))).ToArray());
            FastTravelOffers = Array.AsReadOnly((fastTravelOffers ??
                Enumerable.Empty<FastTravelOfferViewModel>()).ToArray());
            EffectChoices = Array.AsReadOnly((effectChoices ??
                Enumerable.Empty<TypedInvocationViewModel>()).ToArray());
            EffectChoiceTargets = Array.AsReadOnly((effectChoiceTargets ??
                Enumerable.Empty<EffectChoiceTargetViewModel>()).ToArray());
            EffectChoiceDestinations = Array.AsReadOnly((effectChoiceDestinations ??
                Enumerable.Empty<TypedInvocationViewModel>()).ToArray());
            HeroRespawnOptions = Array.AsReadOnly((heroRespawnOptions ??
                Enumerable.Empty<HeroRespawnOptionViewModel>()).ToArray());
            ResolveWithoutRespawn = resolveWithoutRespawn;
            UnavailableEffectChoices = Array.AsReadOnly((unavailableEffectChoices ??
                Enumerable.Empty<UnavailableEffectChoiceViewModel>()).ToArray());
            UnavailableActions = Array.AsReadOnly((unavailableActions ??
                Enumerable.Empty<UnavailableActionViewModel>()).ToArray());
            HudHeader =$"MIRRORED WASP QUICK GAME - DIRECTOR MODE - ROUND {roundIndex} - " +
                $"TURN {turnIndex}/{RoundResolutionAuthority.TurnsPerRound} - {phase.ToString().ToUpperInvariant()}";
        }

        public long Version { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public string Status { get; }
        public IReadOnlyList<TypedInvocationViewModel> Actions { get; }
        public TypedInvocationViewModel EndTurnInvocation { get; }
        public TypedInvocationViewModel ContinueEndOfRoundInvocation { get; }
        public IReadOnlyList<TypedInvocationViewModel> InitiativeChoices { get; }
        public IReadOnlyList<TypedInvocationViewModel> DefenseChoices { get; }
        public IReadOnlyList<FastTravelOfferViewModel> FastTravelOffers { get; }

        /// <summary>
        /// The decision a paused effect frame is waiting on, as the exact invocations the projection
        /// already built. Empty whenever no effect choice is pending.
        /// </summary>
        public IReadOnlyList<TypedInvocationViewModel> EffectChoices { get; }

        /// <summary>
        /// Milestone 6.5c: the candidates of that decision which name a board object. Each carries its
        /// space, so pointing at the control tells the board which object it means.
        /// </summary>
        public IReadOnlyList<EffectChoiceTargetViewModel> EffectChoiceTargets { get; }

        /// <summary>
        /// The candidates which name an empty space. They carry legal invocations but no control: the
        /// board lights them, and clicking a lit space is how they are answered.
        /// </summary>
        public IReadOnlyList<TypedInvocationViewModel> EffectChoiceDestinations { get; }

        /// <summary>
        /// Milestone 6.5ac: the options of that decision the acting player cannot perform. They are
        /// shown greyed rather than hidden, so a player sees the mode exists and that it cannot be
        /// taken, and none of them carries a command.
        /// </summary>
        public IReadOnlyList<UnavailableEffectChoiceViewModel> UnavailableEffectChoices { get; }

        /// <summary>
        /// Milestone 6.5af: the actions this card prints that the authority has judged unstartable.
        /// They keep their label and their place in the offer order and carry no command, so a player
        /// reads that the action exists and cannot be taken - the treatment milestone 6.5ac gave a
        /// refused mode, on an offer.
        /// </summary>
        public IReadOnlyList<UnavailableActionViewModel> UnavailableActions { get; }

        public IReadOnlyList<HeroRespawnOptionViewModel> HeroRespawnOptions { get; }
        public TypedInvocationViewModel ResolveWithoutRespawn { get; }

        public string HudHeader { get; }
    }

    public sealed class HeroRespawnOptionViewModel
    {
        public HeroRespawnOptionViewModel(
            string label,
            HexCoordinate destination,
            DirectorCommand command)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A hero respawn option requires a label.", nameof(label))
                : label;
            Destination = destination;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Label { get; }
        public HexCoordinate Destination { get; }
        public DirectorCommand Command { get; }
    }

    public interface IRoundActionView
    {
        void Render(RoundActionViewModel model);
    }

    public sealed class RoundActionPresenter
    {
        private readonly IMatchSession session;
        private readonly IRoundActionView view;
        private MatchSessionProjection lastProjection;
        private RoundActionViewModel lastViewModel;

        public RoundActionPresenter(IMatchSession session, IRoundActionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;
        public event Action<FastTravelOfferViewModel> FastTravelSelectionRequested;

        public void Refresh()
        {
            view.Render(GetOrCreateViewModel(session.Projection));
        }

        public bool Submit(TypedInvocationViewModel invocation)
        {
            if (invocation == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);

            // Milestone 6.5ac: the unavailable list joins this legality decision rather than sitting
            // outside it. Its own entries carry no command, so nothing a view holds can reach here
            // through them; what this refuses is a command left over from an older model naming an
            // option the authority has since refused - the presenter's half of the same atomicity the
            // session enforces on a stale or forged select.
            if (current.UnavailableEffectChoices.Any(option =>
                    invocation.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    invocation.Command.EffectChoice.Candidate.Equals(option.Candidate)))
            {
                return false;
            }

            // Milestone 6.5af: the same guard for a greyed action. Its own entry carries no command,
            // so nothing a view holds reaches here through it; what this refuses is a start left over
            // from an older model naming an action the authority has since judged unstartable.
            if (current.UnavailableActions.Any(action =>
                    invocation.Command.Kind == DirectorCommandKind.StartAction &&
                    invocation.Command.ActionDefinitionId.Equals(action.ActionDefinitionId)))
            {
                return false;
            }

            var legal = current.Actions
                .Concat(current.InitiativeChoices)
                .Concat(current.DefenseChoices)
                .Concat(current.EffectChoices)
                // Milestone 6.5c: a destination carries no control, but it is still a projected
                // invocation this presenter owns, so the board's click submits through here.
                .Concat(current.EffectChoiceDestinations)
                .Concat(current.ResolveWithoutRespawn != null
                    ? new[] { current.ResolveWithoutRespawn }
                    : Array.Empty<TypedInvocationViewModel>())
                .Concat(current.EndTurnInvocation != null
                    ? new[] { current.EndTurnInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Concat(current.ContinueEndOfRoundInvocation != null
                    ? new[] { current.ContinueEndOfRoundInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Any(value => value.Command.Equals(invocation.Command)) ||
                current.EffectChoiceTargets.Any(value => value.Command.Equals(invocation.Command)) ||
                current.HeroRespawnOptions.Any(value => value.Command.Equals(invocation.Command));
            if (!legal)
            {
                return false;
            }

            return Execute(invocation.Command);
        }

        /// <summary>
        /// Forwards one named target control. Selecting a target of an exactly-one choice finishes it,
        /// so the confirm the authority offers straight afterwards is forwarded with it.
        /// </summary>
        public bool SubmitTarget(EffectChoiceTargetViewModel target)
        {
            if (target == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            return current.EffectChoiceTargets.Any(value => value.Command.Equals(target.Command)) &&
                Execute(target.Command);
        }

        private bool Execute(DirectorCommand command)
        {
            var result = session.Execute(command);
            if (!result.Accepted)
            {
                return false;
            }

            SessionProjectionChanged?.Invoke(result.Projection);
            if (command.Kind != DirectorCommandKind.AnswerEffectChoice)
            {
                return true;
            }

            var confirm = EffectChoiceCompletion.FindImmediateConfirm(
                result.Projection, command.EffectChoice);
            if (confirm == null)
            {
                return true;
            }

            var confirmed = session.Execute(confirm);
            if (confirmed.Accepted)
            {
                SessionProjectionChanged?.Invoke(confirmed.Projection);
            }

            return true;
        }

        public bool SelectFastTravel(FastTravelOfferViewModel selection)
        {
            if (selection == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var legalSelection = current.FastTravelOffers.SingleOrDefault(option =>
                option.ReplacedMovementActionDefinitionId.Equals(
                    selection.ReplacedMovementActionDefinitionId) &&
                option.LegalDestinationCount == selection.LegalDestinationCount);
            if (legalSelection == null)
            {
                return false;
            }

            FastTravelSelectionRequested?.Invoke(legalSelection);
            return true;
        }

        public static RoundActionViewModel CreateViewModel(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            // A paused Skill frame counts here for the same reason paused Movement and Attack do:
            // Core refuses a second action while one is in progress, so an offer shown during the
            // pause is a control that can only produce a rejection.
            var hasPendingResolution = projection.PendingInitiativeChoice != null ||
                projection.PendingHeroRespawnChoice != null ||
                projection.PendingMovementChoice != null || projection.PendingMovementResolution != null ||
                projection.PendingAttackTargetChoice != null || projection.PendingDefenseChoice != null ||
                projection.PendingAttackResolution != null ||
                projection.PendingSkillResolution != null || projection.PendingEffectChoice != null ||
                projection.PendingDisplacedMinionCorrection != null;
            var offered = projection.IsTurnActive && !hasPendingResolution
                ? projection.AvailableTurnActions
                : Array.Empty<ActionOffer>();
            var actions = offered
                .Where(offer => offer.IsAvailable)
                .Select(offer => new TypedInvocationViewModel(
                    FormatActionLabel(projection, offer),
                    DirectorCommand.StartAction(offer.ActionDefinitionId)))
                .ToArray();

            // Milestone 6.5af: an offer the authority judged unstartable is drawn greyed rather than
            // dropped, keeping its label and its place. Presentation decides nothing here - which
            // actions those are is projected, exactly as which options are refused already is.
            var unavailableActions = offered
                .Where(offer => !offer.IsAvailable)
                .Select(offer => new UnavailableActionViewModel(
                    FormatActionLabel(projection, offer),
                    offer.ActionDefinitionId))
                .ToArray();
            var initiativeChoices = projection.PendingInitiativeChoice == null
                ? Array.Empty<TypedInvocationViewModel>()
                : projection.PendingInitiativeChoice.CandidateCardInstanceIds
                    .Select(cardId => new TypedInvocationViewModel(
                        $"ACT WITH {GetCardDisplayName(projection, cardId)}",
                        DirectorCommand.ChooseTiedActor(
                            projection.PendingInitiativeChoice.OwningSide,
                            cardId)))
                    .ToArray();
            var defenseChoices = CreateDefenseChoices(projection);
            var fastTravelOffers = hasPendingResolution
                ? Array.Empty<FastTravelOfferViewModel>()
                : projection.AvailableFastTravelOptions
                    .Select(option => new FastTravelOfferViewModel(
                        "FAST TRAVEL",
                        option.ReplacedMovementActionDefinitionId,
                        option.LegalDestinations.Count))
                    .ToArray();
            CreateEffectChoices(
                projection,
                out var effectChoiceControls,
                out var effectChoiceTargets,
                out var effectChoiceDestinations,
                out var unavailableEffectChoices);
            var respawn = projection.PendingHeroRespawnChoice;
            var respawnOptions = respawn == null
                ? Array.Empty<HeroRespawnOptionViewModel>()
                : respawn.Destinations.Select((destination, index) =>
                    new HeroRespawnOptionViewModel(
                        destination.IsFallback
                            ? $"RESPAWN NEAR SPAWN #{index + 1}"
                            : $"RESPAWN AT SPAWN #{index + 1}",
                        destination.Coordinate,
                        DirectorCommand.RespawnHero(respawn.RespawnInvocations[index])))
                    .ToArray();

            return new RoundActionViewModel(
                projection.Version,
                projection.RoundIndex,
                projection.TurnIndex,
                projection.Phase,
                FormatStatus(projection),
                actions,
                CreateEndTurnInvocation(projection),
                // A pending removal choice is mandatory: the authority rejects an advance while it
                // stands, so the control that would produce that rejection is not offered.
                projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.PendingMinionBattleChoice == null
                    ? new TypedInvocationViewModel("CONTINUE END OF ROUND", DirectorCommand.AdvanceEndOfRound())
                    : null,
                initiativeChoices,
                defenseChoices,
                fastTravelOffers,
                effectChoiceControls,
                effectChoiceTargets,
                effectChoiceDestinations,
                respawnOptions,
                respawn == null
                    ? null
                    : new TypedInvocationViewModel(
                        "RESOLVE WITHOUT ACTION",
                        DirectorCommand.ResolveCardWithoutRespawn(respawn.DeclineInvocation)),
                unavailableEffectChoices,
                unavailableActions);
        }

        /// <summary>
        /// Forwards a paused effect frame's decision without deciding anything. Every invocation here
        /// is one the projection already constructed and bounded - which candidates may still be
        /// selected, whether the selection satisfies the minimum, whether declining is allowed - so
        /// this method chooses only the order they are shown in, which is the projection's own.
        ///
        /// Milestone 6.5c splits where a candidate is answered, and the split is the option/value one
        /// the authority already draws plus the projected occupancy of a value. An option is a mode
        /// and stays a control; a value naming an object is a control named after that object; a value
        /// naming an empty space has no control at all, because the board says it better. No branch
        /// here reads a card, a hero, or a node name.
        /// </summary>
        private static void CreateEffectChoices(
            MatchSessionProjection projection,
            out IReadOnlyList<TypedInvocationViewModel> controls,
            out IReadOnlyList<EffectChoiceTargetViewModel> targets,
            out IReadOnlyList<TypedInvocationViewModel> destinations,
            out IReadOnlyList<UnavailableEffectChoiceViewModel> unavailable)
        {
            controls = Array.Empty<TypedInvocationViewModel>();
            targets = Array.Empty<EffectChoiceTargetViewModel>();
            destinations = Array.Empty<TypedInvocationViewModel>();
            unavailable = Array.Empty<UnavailableEffectChoiceViewModel>();
            var choice = projection.PendingEffectChoice;
            if (choice == null)
            {
                return;
            }

            var ordinals = new Dictionary<EffectChoiceCandidate, int>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                ordinals[choice.Candidates[index]] = index + 1;
            }

            var previews = projection.PendingEffectChoicePreview
                .ToDictionary(preview => preview.Coordinate);

            // Milestone 6.2d: which token names this choice would repeat, decided once over the whole
            // offered set rather than per candidate. A name that occurs twice keeps its ordinal, so two
            // Hazards stay distinguishable; a name that occurs once drops it.
            var repeatedTokenNames = new HashSet<string>(projection.PendingEffectChoicePreview
                .Select(preview => FindTokenName(projection, preview))
                .Where(name => name != null)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key), StringComparer.Ordinal);
            var invocations = new List<TypedInvocationViewModel>();
            var namedTargets = new List<EffectChoiceTargetViewModel>();
            var boardDestinations = new List<TypedInvocationViewModel>();
            foreach (var invocation in choice.SelectInvocations)
            {
                var candidate = invocation.Candidate;
                var command = DirectorCommand.AnswerEffectChoice(invocation);

                // Milestone 6.5b: a value candidate can also name a piece that is not on the board at
                // all - a Spawn draws from the supply - and such a candidate is a control like a mode,
                // because there is no space for the board to light and nothing standing for a cue to
                // mark. What it is not is anonymous.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Minion)
                {
                    invocations.Add(new TypedInvocationViewModel(
                        FormatSupplyMinionLabel(projection, candidate.Value.MinionId, ordinals[candidate]),
                        command));
                    continue;
                }

                if (candidate.Kind != EffectChoiceCandidateKind.Value ||
                    candidate.Value.Kind != EffectValueKind.Coordinate ||
                    !previews.TryGetValue(candidate.Value.Coordinate, out var preview))
                {
                    invocations.Add(new TypedInvocationViewModel(
                        EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinals[candidate]),
                        command));
                    continue;
                }

                if (!preview.NamesObject)
                {
                    boardDestinations.Add(new TypedInvocationViewModel(
                        EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinals[candidate]),
                        command));
                    continue;
                }

                namedTargets.Add(new EffectChoiceTargetViewModel(
                    FormatTargetLabel(projection, preview, ordinals[candidate], repeatedTokenNames),
                    preview.Coordinate,
                    preview.ResultingCoordinate,
                    command));
            }

            foreach (var invocation in choice.DeselectInvocations)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeselectLabel(
                        invocation.Candidate.OptionNodeId, ordinals[invocation.Candidate]),
                    DirectorCommand.AnswerEffectChoice(invocation)));
            }

            if (choice.ConfirmInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.ConfirmLabel,
                    DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation)));
            }

            if (choice.DeclineInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeclineLabel,
                    DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation)));
            }

            controls = invocations;
            targets = namedTargets;
            destinations = boardDestinations;

            // Milestone 6.5ac: an option the authority refused keeps its label and its place in the
            // ordering, and gains no command. Presentation decides nothing here - which options are
            // unperformable is projected, exactly as which candidates are legal always has been.
            unavailable = choice.UnavailableCandidates
                .Select(candidate => new UnavailableEffectChoiceViewModel(
                    EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinals[candidate]),
                    candidate))
                .ToArray();
        }

        /// <summary>
        /// Names the object a target candidate stands on, from projected state alone. A hero is named
        /// by its configured display name, a minion by the team and intrinsic type its occupancy
        /// carries, and - since milestone 6.2d - a token by the display name its projected definition
        /// carries, with an ordinal only where the choice offers that name twice.
        /// </summary>
        private static string FormatTargetLabel(
            MatchSessionProjection projection,
            EffectChoiceCandidatePreview preview,
            int ordinal,
            ICollection<string> repeatedTokenNames)
        {
            switch (preview.Occupant.Kind)
            {
                case BoardObjectKind.Hero:
                    var hero = projection.Setup.Heroes.SingleOrDefault(value =>
                        value.HeroId.Equals(preview.Occupant.HeroId));
                    return hero == null
                        ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                        : EffectChoiceLabels.HeroTargetLabel(hero.TeamSide, hero.DisplayName);
                case BoardObjectKind.Minion:
                    var minion = projection.Minions.SingleOrDefault(value =>
                        value.MinionId.Equals(preview.Occupant.MinionId));
                    return minion == null
                        ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                        : EffectChoiceLabels.MinionTargetLabel(
                            minion.TeamSide, minion.MinionType, ordinal);
                default:
                    var tokenName = FindTokenName(projection, preview);
                    return tokenName == null
                        ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                        : EffectChoiceLabels.TokenTargetLabel(
                            tokenName,
                            repeatedTokenNames.Contains(tokenName) ? ordinal : (int?)null);
            }
        }

        /// <summary>
        /// The projected display name of the token a preview names, or <c>null</c> when the preview
        /// names something else. A projection that has lost the instance falls back to the anonymous
        /// label rather than faulting, which is the same tolerance the hero and minion arms show.
        /// </summary>
        private static string FindTokenName(
            MatchSessionProjection projection, EffectChoiceCandidatePreview preview)
        {
            if (preview.Occupant.Kind != BoardObjectKind.Token)
            {
                return null;
            }

            return projection.PlacedTokens
                .SingleOrDefault(token => token.Id.Equals(preview.Occupant.TokenInstanceId))
                ?.Definition.DisplayName;
        }

        /// <summary>
        /// Names a miniature a candidate would take from the supply. The board projection cannot
        /// describe it, because it is standing nowhere; the declared setup roster carries the team and
        /// the intrinsic type, which is the same pair a standing minion is named by, so a supply
        /// candidate and a board candidate read alike rather than one of them reading as an ordinal.
        /// </summary>
        private static string FormatSupplyMinionLabel(
            MatchSessionProjection projection, MinionId minionId, int ordinal)
        {
            var declared = projection.Setup.Minions.SingleOrDefault(value =>
                value.MinionId.Equals(minionId));
            return declared == null
                ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                : EffectChoiceLabels.MinionTargetLabel(
                    declared.TeamSide, declared.MinionType, ordinal);
        }

        private RoundActionViewModel GetOrCreateViewModel(MatchSessionProjection projection)
        {
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            return lastViewModel;
        }

        private static TypedInvocationViewModel CreateEndTurnInvocation(MatchSessionProjection projection)
        {
            var movementChoice = projection.PendingMovementChoice;
            if (movementChoice != null && movementChoice.CanStop)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.StopMovement(movementChoice.ResolutionId, movementChoice.ChoiceId));
            }

            // Milestone 6.5ae: the same contextual control on the decision family that had lacked it.
            // A paused effect choice whose every option was refused projects a stop of its own, and it
            // is offered exactly where the authority offers it - this branch reads the invocation and
            // never decides that a choice is closable.
            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice?.StopInvocation != null)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.AnswerEffectChoice(effectChoice.StopInvocation));
            }

            var hasMandatoryChoice = projection.PendingInitiativeChoice != null ||
                projection.PendingAttackTargetChoice != null || projection.PendingDefenseChoice != null ||
                projection.PendingAttackResolution != null || projection.PendingMovementChoice != null ||
                projection.PendingMovementResolution != null ||
                projection.PendingSkillResolution != null || projection.PendingEffectChoice != null ||
                projection.PendingDisplacedMinionCorrection != null;
            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined || hasMandatoryChoice)
            {
                return null;
            }

            var hold = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                projection.SelectedActor.CardInstanceId,
                projection.SelectedActor.ActorId,
                projection.Definitions,
                projection.Cards);
            return hold.HasValue && hold.Value.IsAvailable
                ? new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId))
                : null;
        }

        private static IReadOnlyList<TypedInvocationViewModel> CreateDefenseChoices(
            MatchSessionProjection projection)
        {
            var choice = projection.PendingDefenseChoice;
            var preview = projection.PendingDefensePreview;
            if (choice == null || preview == null ||
                !preview.ResolutionId.Equals(choice.ResolutionId) ||
                !preview.ChoiceId.Equals(choice.ChoiceId))
            {
                return Array.Empty<TypedInvocationViewModel>();
            }

            var values = choice.LegalDefenseOffers
                .Select(offer =>
                {
                    var choicePreview = preview.Choices.Single(value =>
                        value.CardInstanceId.HasValue &&
                        value.CardInstanceId.Value.Equals(offer.SourceCardInstanceId));
                    var printed = choicePreview.DefenseContributions.Single(value =>
                        value.Kind == ValueContributionKind.PrintedIcon);
                    return new TypedInvocationViewModel(
                        $"DISCARD {choicePreview.ChoiceLabel.ToUpperInvariant()}\n" +
                        $"PRINTED DEFENSE {printed.Amount}",
                        DirectorCommand.ChooseDefenseCard(
                            choice.ResolutionId,
                            choice.ChoiceId,
                            offer.SourceCardInstanceId),
                        CreateDefenseBreakdown(projection.Version, preview, choicePreview));
                })
                .ToList();
            if (choice.CanDecline)
            {
                var declinePreview = preview.Choices.Single(value => value.IsDecline);
                values.Add(new TypedInvocationViewModel(
                    "DECLINE DEFENSE",
                    DirectorCommand.DeclineDefense(choice.ResolutionId, choice.ChoiceId),
                    CreateDefenseBreakdown(projection.Version, preview, declinePreview)));
            }

            return values;
        }

        public static DefenseBreakdownViewModel CreateDefenseBreakdown(
            long version,
            PendingDefensePreviewProjection preview,
            DefenseChoicePreviewProjection choice)
        {
            var lines = new List<string> { "ATTACK:" };
            lines.AddRange(preview.AttackContributions.Select(FormatContribution));
            lines.Add($"ATTACK TOTAL: {preview.AttackTotal}");
            lines.Add(string.Empty);
            lines.Add("DEFENSE:");
            if (!choice.IsDecline)
            {
                lines.Add($"  CARD: {choice.ChoiceLabel.ToUpperInvariant()}");
                lines.AddRange(choice.DefenseContributions.Select(FormatContribution));
            }
            if (choice.BlockResult == DefenseBlockResult.Blocked)
            {
                lines.Add("  BLOCK");
                lines.Add("RESULT: BLOCK - DEFENDED");
            }
            else if (!choice.DefenseTotal.HasValue)
            {
                lines.Add("  NO DEFENSE TOTAL");
                lines.Add("RESULT: NO DEFENSE TOTAL - ATTACK SUCCEEDS");
            }
            else
            {
                lines.Add($"DEFENSE TOTAL: {choice.DefenseTotal.Value}");
                lines.Add(choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                    ? "RESULT: DEFENDED"
                    : "RESULT: ATTACK SUCCEEDS");
            }

            return new DefenseBreakdownViewModel(
                version,
                preview.ResolutionId,
                preview.ChoiceId,
                choice.CardInstanceId,
                choice.IsDecline
                    ? "DEFENSE PREVIEW: DECLINE"
                    : $"DEFENSE PREVIEW: {choice.ChoiceLabel.ToUpperInvariant()}",
                string.Join("\n", lines),
                choice.Outcome);
        }

        private static string FormatContribution(ValueContributionProjection contribution)
        {
            var sign = contribution.Amount >= 0 ? "+" : string.Empty;
            return $"  {sign}{contribution.Amount} — {contribution.SourceLabel}";
        }

        private static string FormatActionLabel(MatchSessionProjection projection, ActionOffer offer)
        {
            var definition = projection.Definitions.Single(value =>
                value.Reference.Equals(offer.SourceDefinitionReference));
            var action = definition.ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId));
            var channel = action.Kind == ActionKind.Attack
                ? PrintedValueChannel.Attack
                : action.Kind == ActionKind.Movement
                    ? PrintedValueChannel.Movement
                    : PrintedValueChannel.None;
            var value = channel == PrintedValueChannel.None
                ? string.Empty
                : $" {PrintedValueQuery.CalculatePrintedValue(offer, projection.Definitions, channel).Total}";
            var range = action.Kind == ActionKind.Attack && offer.AttackRange == AttackRangeKind.Ranged
                ? $" RANGE {PrintedValueQuery.CalculatePrintedValue(offer, projection.Definitions, PrintedValueChannel.Range).Total}"
                : string.Empty;
            var policy = action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies
                ? "text applies"
                : "text suppressed";
            return $"{FormatInvocationContext(offer.InvocationContext)} " +
                $"{action.Kind.ToString().ToUpperInvariant()}{value}{range}\n{policy}";
        }

        private static string FormatInvocationContext(ActionInvocationContext context)
        {
            return context == ActionInvocationContext.PrintedPrimary ? "PRIMARY" : "SECONDARY";
        }

        private static string GetCardDisplayName(MatchSessionProjection projection, CardInstanceId cardId)
        {
            var card = projection.Cards.Single(value => value.Id.Equals(cardId));
            return projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).DisplayName.ToUpperInvariant();
        }

        private static string FormatStatus(MatchSessionProjection projection)
        {
            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                return $"ROUND {projection.RoundIndex} COMPLETE - END OF ROUND";
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                return $"Round {projection.RoundIndex}, turn {projection.TurnIndex}: awaiting commitments.";
            }

            if (projection.PendingInitiativeChoice != null)
            {
                return "Choose the tied Initiative actor.";
            }

            if (projection.PendingDefenseChoice != null)
            {
                return $"Defense response pending against Attack {projection.PendingDefenseChoice.AttackTotal}.";
            }

            if (projection.PendingHeroRespawnChoice != null)
            {
                return "Hero is off board: choose a respawn point or resolve the card without an action.";
            }

            if (projection.PendingAttackTargetChoice != null)
            {
                return "Choose a legal Attack target.";
            }

            if (projection.PendingMovementChoice != null)
            {
                return $"Movement has {projection.PendingMovementChoice.RemainingAllowance} remaining.";
            }

            if (projection.PendingDisplacedMinionCorrection != null)
            {
                return "Displaced-minion correction: " +
                    $"{MinionChoiceLabels.TeamLabel(projection.PendingDisplacedMinionCorrection.ChoosingSide)} decides.";
            }

            return projection.SelectedActor.IsDefined
                ? $"Initiative {projection.SelectedActor.InitiativeTotal}: choose an action or End Turn."
                : "Awaiting the next authoritative choice.";
        }
    }

    /// <summary>
    /// Which authoritative minion-choice family the one shared prompt is presenting. Milestone 6.1a
    /// built the interface for the Minion Battle removal; milestone 6.4b widened it by prompt kind for
    /// the displaced-minion correction rather than adding a second correction-only framework, so the
    /// presenter's submit, confirm, and stale-refresh paths never branch on this value - only labels
    /// and candidate identity do.
    /// </summary>
    public enum MinionChoicePromptKind
    {
        MinionBattleRemoval = 0,
        CorrectionSubject = 1,
        CorrectionMove = 2,
        CorrectionPlace = 3
    }

    public sealed class MinionChoiceCandidateViewModel
    {
        public MinionChoiceCandidateViewModel(
            MinionId minionId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command)
            : this(RuleObject.ForMinion(minionId), label, ordinal, isSelected, command, null)
        {
        }

        public MinionChoiceCandidateViewModel(
            HeroId heroId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command)
            : this(RuleObject.ForHero(heroId), label, ordinal, isSelected, command, null)
        {
        }

        /// <summary>
        /// Milestone 6.4b's destination-bearing form: a correction route endpoint or Place space. The
        /// minion is the displaced subject the destination would receive, so every candidate of one
        /// destination prompt shares it; the destination is what distinguishes them.
        /// </summary>
        public MinionChoiceCandidateViewModel(
            MinionId minionId,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command,
            HexCoordinate? destination)
            : this(
                RuleObject.ForMinion(minionId), label, ordinal, isSelected, command, destination)
        {
        }

        private MinionChoiceCandidateViewModel(
            RuleObject unit,
            string label,
            int ordinal,
            bool isSelected,
            DirectorCommand command,
            HexCoordinate? destination)
        {
            if (!unit.IsDefined || ordinal < 1)
            {
                throw new ArgumentException(
                    "A minion choice candidate requires a stable identity and its projected ordinal.");
            }

            Unit = unit;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A minion choice candidate requires a label.", nameof(label))
                : label;
            Ordinal = ordinal;
            IsSelected = isSelected;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Destination = destination;
        }

        public RuleObject Unit { get; }
        public MinionId MinionId => Unit.Kind == RuleObjectKind.Minion ? Unit.MinionId : default;
        public HeroId HeroId => Unit.Kind == RuleObjectKind.Hero ? Unit.HeroId : default;
        public string Label { get; }
        public int Ordinal { get; }

        /// <summary>
        /// Whether this candidate is currently part of the authoritative selection. The control it
        /// carries is then the projected deselect invocation rather than the select one.
        /// </summary>
        public bool IsSelected { get; }
        public DirectorCommand Command { get; }

        /// <summary>The correction destination this control selects, or <c>null</c> for a minion control.</summary>
        public HexCoordinate? Destination { get; }
    }

    /// <summary>
    /// The contextual prompt for a pending minion choice. It holds only what the authoritative
    /// pending choice already decided - chooser identity, required count, current selection, the
    /// candidates that carry a legal control right now, and the exact typed invocations - so the view
    /// never compares minion counts, orders Heavy removals, or advances the owning continuation.
    /// </summary>
    public sealed class MinionChoiceViewModel
    {
        public MinionChoiceViewModel(
            long version,
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            int requiredRemovalCount,
            int selectedCount,
            IEnumerable<MinionChoiceCandidateViewModel> candidates,
            TypedInvocationViewModel confirmInvocation)
            : this(
                version, MinionChoicePromptKind.MinionBattleRemoval, resolutionId, choiceId,
                default, default, default, choosingTeamId, choosingSide, requiredRemovalCount,
                selectedCount, candidates, confirmInvocation)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException(
                    "A Minion Battle prompt requires the authoritative resolution and choice identities.");
            }
        }

        /// <summary>
        /// Milestone 6.4b's correction form of the same prompt: the displaced subject, the correction
        /// kind, and destination- or subject-valued candidates, addressed by the owning action's
        /// identity rather than an end-of-round one. Selection is the advance for a correction, so it
        /// carries no confirm invocation - the shared contract's confirm/advance slot is satisfied by
        /// each candidate's one-shot command.
        /// </summary>
        public static MinionChoiceViewModel ForCorrection(
            long version,
            MinionChoicePromptKind kind,
            ActionResolutionId actionResolutionId,
            ActionResolutionStepId actionChoiceId,
            MinionId subjectMinionId,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            IEnumerable<MinionChoiceCandidateViewModel> candidates)
        {
            if (kind == MinionChoicePromptKind.MinionBattleRemoval)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "The correction form presents correction prompt kinds only.");
            }

            if (!actionResolutionId.IsDefined || !actionChoiceId.IsDefined)
            {
                throw new ArgumentException(
                    "A correction prompt requires the owning action's resolution and choice identities.");
            }

            if (kind != MinionChoicePromptKind.CorrectionSubject && !subjectMinionId.IsDefined)
            {
                throw new ArgumentException(
                    "A correction destination prompt names its displaced subject.", nameof(subjectMinionId));
            }

            return new MinionChoiceViewModel(
                version, kind, default, default, actionResolutionId, actionChoiceId,
                subjectMinionId, choosingTeamId, choosingSide, 1, 0, candidates, null);
        }

        private MinionChoiceViewModel(
            long version,
            MinionChoicePromptKind kind,
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            ActionResolutionId actionResolutionId,
            ActionResolutionStepId actionChoiceId,
            MinionId subjectMinionId,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            int requiredRemovalCount,
            int selectedCount,
            IEnumerable<MinionChoiceCandidateViewModel> candidates,
            TypedInvocationViewModel confirmInvocation)
        {
            if (!choosingTeamId.IsDefined)
            {
                throw new ArgumentException(
                    "A minion choice prompt requires the authoritative team identity.");
            }

            if (choosingSide != TeamSide.Red && choosingSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(choosingSide));
            }

            if (requiredRemovalCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredRemovalCount));
            }

            if (selectedCount < 0 || selectedCount > requiredRemovalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedCount));
            }

            Version = version;
            Kind = kind;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ActionResolutionId = actionResolutionId;
            ActionChoiceId = actionChoiceId;
            SubjectMinionId = subjectMinionId;
            ChoosingTeamId = choosingTeamId;
            ChoosingSide = choosingSide;
            RequiredRemovalCount = requiredRemovalCount;
            SelectedCount = selectedCount;
            Candidates = Array.AsReadOnly((candidates ??
                throw new ArgumentNullException(nameof(candidates))).ToArray());

            // One control per distinguishing identity: minions for the battle and subject prompts,
            // destinations for a correction destination prompt, whose candidates all share the one
            // displaced subject.
            var distinct = kind == MinionChoicePromptKind.CorrectionMove ||
                kind == MinionChoicePromptKind.CorrectionPlace
                    ? Candidates.Select(value => (object)value.Destination).Distinct().Count()
                    : Candidates.Select(value => (object)value.MinionId).Distinct().Count();
            if (distinct != Candidates.Count)
            {
                throw new ArgumentException(
                    "A minion choice prompt offers at most one control per candidate identity.",
                    nameof(candidates));
            }

            ConfirmInvocation = confirmInvocation;
            var team = MinionChoiceLabels.TeamLabel(choosingSide);
            switch (kind)
            {
                case MinionChoicePromptKind.CorrectionSubject:
                    SectionLabel = "Correction";
                    PromptLabel = $"CORRECTION - {team} RETURNS A DISPLACED MINION";
                    break;
                case MinionChoicePromptKind.CorrectionMove:
                    SectionLabel = "Correction";
                    PromptLabel = $"CORRECTION (MOVE) - {team} CHOOSES THE RETURN SPACE";
                    break;
                case MinionChoicePromptKind.CorrectionPlace:
                    SectionLabel = "Correction";
                    PromptLabel = $"CORRECTION (PLACE) - {team} CHOOSES THE PLACE SPACE";
                    break;
                default:
                    SectionLabel = "Minion Battle";
                    PromptLabel = $"MINION BATTLE - {team} REMOVES " +
                        $"{requiredRemovalCount} {(requiredRemovalCount == 1 ? "MINION" : "MINIONS")}";
                    break;
            }

            StatusLabel = $"SELECTED {selectedCount} OF {requiredRemovalCount}";
        }

        public long Version { get; }
        public MinionChoicePromptKind Kind { get; }
        public EndOfRoundResolutionId ResolutionId { get; }
        public EndOfRoundChoiceId ChoiceId { get; }

        /// <summary>The owning action's identity when <see cref="Kind"/> is a correction prompt.</summary>
        public ActionResolutionId ActionResolutionId { get; }
        public ActionResolutionStepId ActionChoiceId { get; }

        /// <summary>The displaced subject of a correction destination prompt; undefined otherwise.</summary>
        public MinionId SubjectMinionId { get; }
        public TeamId ChoosingTeamId { get; }
        public TeamSide ChoosingSide { get; }
        public int RequiredRemovalCount { get; }
        public int SelectedCount { get; }
        public IReadOnlyList<MinionChoiceCandidateViewModel> Candidates { get; }

        /// <summary>Present only while the authoritative choice reports it can be confirmed.</summary>
        public TypedInvocationViewModel ConfirmInvocation { get; }
        public string PromptLabel { get; }
        public string StatusLabel { get; }

        /// <summary>The prompt family's short gutter label, so the view carries no literal of its own.</summary>
        public string SectionLabel { get; }
    }

    /// <summary>
    /// The passive port for the minion-choice prompt. A null model means no minion choice is pending
    /// and therefore no control may be shown.
    /// </summary>
    public interface IMinionChoiceView
    {
        void Render(MinionChoiceViewModel model);
    }

    public sealed class MinionChoicePresenter
    {
        private readonly IMatchSession session;
        private readonly IMinionChoiceView view;
        private MatchSessionProjection lastProjection;
        private MinionChoiceViewModel lastViewModel;

        public MinionChoicePresenter(IMatchSession session, IMinionChoiceView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            view.Render(lastViewModel);
        }

        /// <summary>
        /// Forwards the exact projected select or deselect invocation the candidate carries. A control
        /// left over from an earlier projection matches nothing current, so it submits nothing and the
        /// view is rebuilt from the authoritative pending choice instead.
        /// </summary>
        public bool Submit(MinionChoiceCandidateViewModel candidate)
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (candidate == null || current == null ||
                !current.Candidates.Any(value => value.Command.Equals(candidate.Command)))
            {
                Refresh();
                return false;
            }

            return Execute(candidate.Command);
        }

        public bool Confirm()
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (current?.ConfirmInvocation == null)
            {
                Refresh();
                return false;
            }

            return Execute(current.ConfirmInvocation.Command);
        }

        public static MinionChoiceViewModel CreateViewModel(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var choice = projection.PendingMinionBattleChoice;
            if (choice == null)
            {
                // At most one minion-choice family is ever pending: the battle is end-of-round state
                // and the correction pauses mid-turn, so this is a route rather than a priority.
                return CreateCorrectionViewModel(projection);
            }

            var candidates = new List<MinionChoiceCandidateViewModel>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                var candidate = choice.Candidates[index];
                var select = choice.SelectInvocations.SingleOrDefault(value =>
                    value.Unit.Equals(candidate.Unit));
                var deselect = choice.DeselectInvocations.SingleOrDefault(value =>
                    value.Unit.Equals(candidate.Unit));
                if (select == null && deselect == null)
                {
                    // The authority currently offers neither invocation for this minion - a Heavy
                    // while a non-Heavy is still unselected is the live case. Presentation offers no
                    // control rather than reproducing the ordering rule that withheld it.
                    continue;
                }

                var isSelected = deselect != null;
                var ordinal = index + 1;
                var command = isSelected
                    ? DirectorCommand.DeselectMinionBattleRemoval(deselect)
                    : DirectorCommand.SelectMinionBattleRemoval(select);
                var label = candidate.HeroId.IsDefined
                    ? $"{(isSelected ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.TeamLabel(choice.ChoosingSide)} HERO {candidate.HeroId.Value.ToUpperInvariant()}"
                    : $"{(isSelected ? "KEEP" : "REMOVE")} " +
                        $"{MinionChoiceLabels.CandidateLabel(choice.ChoosingSide, candidate.MinionType, ordinal)}";
                candidates.Add(candidate.HeroId.IsDefined
                    ? new MinionChoiceCandidateViewModel(
                        candidate.HeroId, label, ordinal, isSelected, command)
                    : new MinionChoiceCandidateViewModel(
                        candidate.MinionId, label, ordinal, isSelected, command));
            }

            return new MinionChoiceViewModel(
                projection.Version,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.LosingTeamId,
                choice.ChoosingSide,
                choice.RequiredRemovalCount,
                choice.SelectedUnits.Count,
                candidates,
                choice.CanConfirm
                    ? new TypedInvocationViewModel(
                        choice.RequiredRemovalCount == 1 ? "CONFIRM REMOVAL" : "CONFIRM REMOVALS",
                        DirectorCommand.ConfirmMinionBattleRemovals(choice.ConfirmInvocation))
                    : null);
        }

        /// <summary>
        /// Milestone 6.4b: the displaced-minion correction rides the same prompt. Everything shown is
        /// read off the projected pending choice - candidates, kinds, invocations - so presentation
        /// calculates no path, distance, Battle-Zone membership, occupancy, or Tie-Breaker ordering.
        /// A subject label joins the projection's minion occupancy for team and intrinsic type, which
        /// is a lookup of projected state, not a rules answer.
        /// </summary>
        private static MinionChoiceViewModel CreateCorrectionViewModel(MatchSessionProjection projection)
        {
            var pending = projection.PendingDisplacedMinionCorrection;
            if (pending == null)
            {
                return null;
            }

            if (pending.Stage == DisplacedMinionCorrectionStage.SelectSubject)
            {
                var subjects = new List<MinionChoiceCandidateViewModel>();
                for (var index = 0; index < pending.SubjectCandidates.Count; index++)
                {
                    var subject = pending.SubjectCandidates[index];
                    var occupancy = projection.Minions.Single(minion =>
                        minion.MinionId.Equals(subject.MinionId));
                    var invocation = pending.SelectInvocations.Single(value =>
                        value.MinionId.Equals(subject.MinionId));
                    var ordinal = index + 1;
                    subjects.Add(new MinionChoiceCandidateViewModel(
                        subject.MinionId,
                        $"RETURN {MinionChoiceLabels.CandidateLabel(occupancy.TeamSide, occupancy.MinionType, ordinal)}",
                        ordinal,
                        false,
                        DirectorCommand.ChooseCorrectionSubject(invocation)));
                }

                return MinionChoiceViewModel.ForCorrection(
                    projection.Version,
                    MinionChoicePromptKind.CorrectionSubject,
                    pending.ResolutionId,
                    pending.ChoiceId,
                    default,
                    pending.ChoosingTeamId,
                    pending.ChoosingSide,
                    subjects);
            }

            var kind = pending.Kind == DisplacedMinionCorrectionKind.Place
                ? MinionChoicePromptKind.CorrectionPlace
                : MinionChoicePromptKind.CorrectionMove;
            var verb = kind == MinionChoicePromptKind.CorrectionPlace ? "PLACE AT" : "MOVE TO";
            var destinations = new List<MinionChoiceCandidateViewModel>();
            for (var index = 0; index < pending.DestinationCandidates.Count; index++)
            {
                var candidate = pending.DestinationCandidates[index];
                var invocation = pending.SelectInvocations.Single(value =>
                    value.Destination.Equals(candidate.Destination));
                var ordinal = index + 1;
                destinations.Add(new MinionChoiceCandidateViewModel(
                    pending.SubjectMinionId,
                    $"{verb} ({candidate.Destination.Q},{candidate.Destination.R})",
                    ordinal,
                    false,
                    DirectorCommand.ChooseCorrectionDestination(invocation),
                    candidate.Destination));
            }

            return MinionChoiceViewModel.ForCorrection(
                projection.Version,
                kind,
                pending.ResolutionId,
                pending.ChoiceId,
                pending.SubjectMinionId,
                pending.ChoosingTeamId,
                pending.ChoosingSide,
                destinations);
        }

        private MinionChoiceViewModel GetOrCreateViewModel(MatchSessionProjection projection)
        {
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(projection);
            }

            return lastViewModel;
        }

        private bool Execute(DirectorCommand command)
        {
            var result = session.Execute(command);
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }
            else
            {
                // A refusal leaves the authoritative choice standing and hands back the projection
                // that still holds it. Rebuilding from that is what puts the visible selection back
                // in step with authority instead of with the control the player just used.
                Refresh();
            }

            return result.Accepted;
        }
    }

    public sealed class UndoRestartViewModel
    {
        public UndoRestartViewModel(
            long version,
            bool canUndo,
            bool canRestart,
            int acceptedCommandCount,
            bool isHistoryAvailable)
        {
            Version = version;
            CanUndo = canUndo;
            CanRestart = canRestart;
            AcceptedCommandCount = acceptedCommandCount;
            IsHistoryAvailable = isHistoryAvailable;
        }

        public long Version { get; }
        public bool CanUndo { get; }
        public bool CanRestart { get; }
        public int AcceptedCommandCount { get; }

        /// <summary>
        /// False once a snapshot has been loaded, which permanently disables history. Both states
        /// leave <see cref="CanUndo"/> false, but only this one means no future command will ever
        /// make it true, so a view can say "history unavailable" rather than "nothing to undo".
        /// </summary>
        public bool IsHistoryAvailable { get; }
    }

    public interface IUndoRestartView
    {
        void Render(UndoRestartViewModel model);
    }

    public sealed class UndoRestartPresenter
    {
        private readonly IMatchSession session;
        private readonly IUndoRestartView view;
        private MatchSessionProjection lastProjection;
        private UndoRestartViewModel lastViewModel;

        public UndoRestartPresenter(IMatchSession session, IUndoRestartView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<MatchSessionProjection> SessionProjectionChanged;

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = new UndoRestartViewModel(
                    projection.Version,
                    projection.AcceptedCommands.Count > 0,
                    projection.AcceptedCommands.Count > 0,
                    projection.AcceptedCommands.Count,
                    projection.IsHistoryAvailable);
            }

            view.Render(lastViewModel);
        }

        public bool Undo()
        {
            var result = session.Undo();
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }

        public bool Restart()
        {
            var result = session.Restart();
            if (result.Accepted)
            {
                SessionProjectionChanged?.Invoke(result.Projection);
            }

            return result.Accepted;
        }
    }

    public sealed class DiagnosticLogViewModel
    {
        public DiagnosticLogViewModel(long version, bool isExpanded, IEnumerable<RuleEventLogEntry> entries)
        {
            Version = version;
            IsExpanded = isExpanded;
            Entries = Array.AsReadOnly((entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
            ToggleLabel = IsExpanded ? "HIDE EVENT LOG" : $"EVENT LOG ({Entries.Count})";
            PanelLabel = $"INTERNAL RULE-EVENT LOG - {Entries.Count} ACCEPTED EVENTS";
        }

        public long Version { get; }
        public bool IsExpanded { get; }
        public IReadOnlyList<RuleEventLogEntry> Entries { get; }
        public string ToggleLabel { get; }
        public string PanelLabel { get; }
    }

    public interface IDiagnosticLogView
    {
        void Render(DiagnosticLogViewModel model);
    }

    public sealed class DiagnosticLogPresenter
    {
        private readonly IMatchSession session;
        private readonly IDiagnosticLogView view;
        private bool isExpanded;
        private MatchSessionProjection lastProjection;
        private bool lastExpanded;
        private DiagnosticLogViewModel lastViewModel;

        public DiagnosticLogPresenter(IMatchSession session, IDiagnosticLogView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (!ReferenceEquals(projection, lastProjection) || isExpanded != lastExpanded)
            {
                lastProjection = projection;
                lastExpanded = isExpanded;
                lastViewModel = new DiagnosticLogViewModel(
                    projection.Version,
                    isExpanded,
                    projection.DiagnosticEvents);
            }

            view.Render(lastViewModel);
        }

        public void Toggle()
        {
            isExpanded = !isExpanded;
            Refresh();
        }
    }

    public sealed class MatchFeatureCoordinator
    {
        private readonly BoardInteractionPresenter board;
        private readonly DirectorHandsPresenter hands;
        private readonly RoundActionPresenter round;
        private readonly UndoRestartPresenter undoRestart;
        private readonly DiagnosticLogPresenter diagnostics;
        private readonly MinionChoicePresenter minionChoice;

        public MatchFeatureCoordinator(
            BoardInteractionPresenter board,
            DirectorHandsPresenter hands,
            RoundActionPresenter round,
            UndoRestartPresenter undoRestart,
            DiagnosticLogPresenter diagnostics,
            MinionChoicePresenter minionChoice,
            Action<MatchSessionProjection> afterSessionChange = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hands = hands ?? throw new ArgumentNullException(nameof(hands));
            this.round = round ?? throw new ArgumentNullException(nameof(round));
            this.undoRestart = undoRestart ?? throw new ArgumentNullException(nameof(undoRestart));
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            this.minionChoice = minionChoice ?? throw new ArgumentNullException(nameof(minionChoice));

            void Changed(MatchSessionProjection projection)
            {
                afterSessionChange?.Invoke(projection);
                RefreshAll();
            }

            board.SessionProjectionChanged += Changed;
            hands.SessionProjectionChanged += Changed;
            round.SessionProjectionChanged += Changed;
            undoRestart.SessionProjectionChanged += Changed;
            minionChoice.SessionProjectionChanged += Changed;
        }

        public void RefreshAll()
        {
            board.Refresh();
            hands.Refresh();
            round.Refresh();
            undoRestart.Refresh();
            diagnostics.Refresh();
            minionChoice.Refresh();
        }
    }
}
