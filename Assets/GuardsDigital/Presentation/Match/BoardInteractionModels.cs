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
    /// One passive, identity-keyed target cue for every kind of piece the board can draw. State says
    /// how the candidate relates to the current choice; consequence says what the invoked operation
    /// intends. Neither field is legality, and neither asks presentation to predict the result.
    /// </summary>
    public readonly struct PieceTargetCueViewModel
    {
        public PieceTargetCueViewModel(
            BoardObjectReference target,
            BoardTargetCueState state,
            BoardTargetConsequenceIntent consequenceIntent)
        {
            if (!target.IsDefined || state == BoardTargetCueState.None)
            {
                throw new ArgumentException(
                    "A piece target cue requires a stable identity and a visible state.");
            }

            if (state != BoardTargetCueState.Eligible &&
                state != BoardTargetCueState.Selected &&
                state != BoardTargetCueState.Hovered)
            {
                throw new ArgumentOutOfRangeException(nameof(state));
            }

            if (consequenceIntent != BoardTargetConsequenceIntent.Affect &&
                consequenceIntent != BoardTargetConsequenceIntent.Removal &&
                consequenceIntent != BoardTargetConsequenceIntent.Attack &&
                consequenceIntent != BoardTargetConsequenceIntent.Ally)
            {
                throw new ArgumentOutOfRangeException(nameof(consequenceIntent));
            }

            Target = target;
            State = state;
            ConsequenceIntent = consequenceIntent;
        }

        public BoardObjectReference Target { get; }
        public BoardTargetCueState State { get; }
        public BoardTargetConsequenceIntent ConsequenceIntent { get; }
    }

    /// <summary>
    /// One space-valued answer to a paused system transition. The destination and the standing
    /// application's exact command travel together, so a board click never reconstructs an option
    /// identity or answer payload from a coordinate.
    /// </summary>
    public sealed class SystemTransitionDestinationViewModel
    {
        public SystemTransitionDestinationViewModel(
            HexCoordinate destination,
            DirectorCommand command)
        {
            Destination = destination;
            Command = command != null &&
                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep
                ? command
                : throw new ArgumentException(
                    "A system-transition board destination requires its exact projected command.",
                    nameof(command));
        }

        public HexCoordinate Destination { get; }
        public DirectorCommand Command { get; }
    }

    /// <summary>
    /// Milestone 8.46d. One of the six directions a declared
    /// <see cref="EffectChoiceValueVocabulary.HexDirection"/> choice offers, carried as the board
    /// affordance that answers it.
    ///
    /// <para>Shaped on <see cref="SystemTransitionDestinationViewModel"/>, which is this project's
    /// precedent for a board candidate travelling with its own exact command: the arrow a player
    /// clicks submits the Select the prompt control submits, and nothing reconstructs an answer from
    /// a coordinate. The neighbour is where the arrow stands, and it is derived from the chooser's
    /// own space through <see cref="HexCoordinate.GetNeighbor"/> rather than from anything drawn.</para>
    /// </summary>
    public sealed class DirectionCandidateViewModel
    {
        public DirectionCandidateViewModel(
            HexDirection direction,
            HexCoordinate neighbour,
            string label,
            DirectorCommand command)
        {
            if (!Enum.IsDefined(typeof(HexDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException(
                    "A direction candidate is named by its own declared vocabulary.", nameof(label));
            }

            Direction = direction;
            Neighbour = neighbour;
            Label = label;
            Command = command != null && command.Kind == DirectorCommandKind.AnswerEffectChoice
                ? command
                : throw new ArgumentException(
                    "A board direction candidate requires its exact projected command.",
                    nameof(command));
        }

        public HexDirection Direction { get; }

        /// <summary>The space one step that way, which is where the arrow is drawn and clicked.</summary>
        public HexCoordinate Neighbour { get; }

        /// <summary>The same words the prompt control wears - EAST, NORTH EAST, and so on.</summary>
        public string Label { get; }

        public DirectorCommand Command { get; }
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
            IEnumerable<PieceTargetCueViewModel> pieceTargetCues = null,
            HexCoordinate? correctionDestination = null,
            IEnumerable<HexCoordinate> effectChoiceDestinations = null,
            HexCoordinate? effectChoiceTargetSpace = null,
            HexCoordinate? effectChoiceResultingSpace = null,
            HexCoordinate? heroRespawnHighlight = null,
            IEnumerable<HexCoordinate> heroRespawnDestinations = null,
            IEnumerable<SystemTransitionDestinationViewModel> systemTransitionDestinations = null,
            IEnumerable<HexCoordinate> effectChoiceMovementDestinations = null,
            IEnumerable<AttackTargetDefensePreviewProjection> attackTargetDefensePreviews = null,
            ActiveEffectVisibilityModel activeEffects = null,
            HexCoordinate? directionChooserSpace = null,
            IEnumerable<DirectionCandidateViewModel> directionCandidates = null)
        {
            ActiveEffects = activeEffects ?? ActiveEffectVisibilityModel.Empty;
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
            AttackTargetDefensePreviews = Array.AsReadOnly((attackTargetDefensePreviews ??
                Enumerable.Empty<AttackTargetDefensePreviewProjection>()).ToArray());
            PieceTargetCues = Array.AsReadOnly((pieceTargetCues ??
                Enumerable.Empty<PieceTargetCueViewModel>()).ToArray());
            if (PieceTargetCues.Select(value => value.Target).Distinct().Count() !=
                PieceTargetCues.Count)
            {
                throw new ArgumentException(
                    "One projected piece carries exactly zero or one target cue.",
                    nameof(pieceTargetCues));
            }

            var projectedPieces = new HashSet<BoardObjectReference>(
                Heroes.Select(value => BoardObjectReference.ForHero(value.HeroId))
                    .Concat(Minions.Select(value =>
                        BoardObjectReference.ForMinion(value.MinionId)))
                    .Concat(PlacedTokens.Select(value =>
                        BoardObjectReference.ForToken(value.Id))));
            if (PieceTargetCues.Any(value => !projectedPieces.Contains(value.Target)))
            {
                throw new ArgumentException(
                    "A target cue must name a piece carried by the same board projection.",
                    nameof(pieceTargetCues));
            }

            var attackIntentHeroIds = new HashSet<HeroId>(AttackTargets);
            attackIntentHeroIds.UnionWith(PieceTargetCues
                .Where(value =>
                    value.Target.Kind == BoardObjectKind.Hero &&
                    value.ConsequenceIntent == BoardTargetConsequenceIntent.Attack)
                .Select(value => value.Target.HeroId));
            if (AttackTargetDefensePreviews.Any(value => value == null) ||
                AttackTargetDefensePreviews.Select(value => value.HeroId).Distinct().Count() !=
                    AttackTargetDefensePreviews.Count ||
                AttackTargetDefensePreviews.Any(value =>
                    !attackIntentHeroIds.Contains(value.HeroId)))
            {
                throw new ArgumentException(
                    "Attack-target Defense previews must name unique projected heroes with Attack intent.",
                    nameof(attackTargetDefensePreviews));
            }

            CorrectionDestination = correctionDestination;
            EffectChoiceDestinations = Array.AsReadOnly((effectChoiceDestinations ??
                Enumerable.Empty<HexCoordinate>()).ToArray());
            EffectChoiceMovementDestinations = Array.AsReadOnly((effectChoiceMovementDestinations ??
                Enumerable.Empty<HexCoordinate>()).ToArray());
            if (EffectChoiceMovementDestinations.Any(value => !EffectChoiceDestinations.Contains(value)))
            {
                throw new ArgumentException(
                    "An effect movement destination must also be a selectable effect destination.",
                    nameof(effectChoiceMovementDestinations));
            }
            HeroRespawnDestinations = Array.AsReadOnly((heroRespawnDestinations ??
                Enumerable.Empty<HexCoordinate>()).ToArray());
            SystemTransitionDestinations = Array.AsReadOnly((systemTransitionDestinations ??
                Enumerable.Empty<SystemTransitionDestinationViewModel>()).ToArray());
            if (SystemTransitionDestinations.Any(value => value == null) ||
                SystemTransitionDestinations.Select(value => value.Destination).Distinct().Count() !=
                    SystemTransitionDestinations.Count ||
                SystemTransitionDestinations.Select(value => value.Command).Distinct().Count() !=
                    SystemTransitionDestinations.Count)
            {
                throw new ArgumentException(
                    "Each system-transition option maps one distinct destination to one exact command.",
                    nameof(systemTransitionDestinations));
            }
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

            // Milestone 8.46d. The arrows are around ONE hero and each stands one step from them, so
            // the two halves are checked against each other here rather than trusted from whoever
            // derived them: a candidate whose space is not the chooser's own neighbour would draw an
            // arrow the click path could never match back to this command.
            DirectionChooserSpace = directionChooserSpace;
            DirectionCandidates = Array.AsReadOnly((directionCandidates ??
                Enumerable.Empty<DirectionCandidateViewModel>()).ToArray());
            if (DirectionCandidates.Any(value => value == null))
            {
                throw new ArgumentException(
                    "A direction candidate is a projected offer, never an absence.",
                    nameof(directionCandidates));
            }

            if (DirectionCandidates.Count > 0 && !DirectionChooserSpace.HasValue)
            {
                throw new ArgumentException(
                    "Direction arrows are drawn around the hero being asked, so the chooser's space " +
                    "is required.", nameof(directionChooserSpace));
            }

            if (DirectionCandidates.Select(value => value.Direction).Distinct().Count() !=
                    DirectionCandidates.Count ||
                DirectionCandidates.Select(value => value.Command).Distinct().Count() !=
                    DirectionCandidates.Count)
            {
                throw new ArgumentException(
                    "Each offered direction maps one distinct direction to one exact command.",
                    nameof(directionCandidates));
            }

            if (DirectionCandidates.Any(value =>
                !DirectionChooserSpace.Value.GetNeighbor(value.Direction).Equals(value.Neighbour)))
            {
                throw new ArgumentException(
                    "A direction candidate stands on the chooser's own neighbour in that direction.",
                    nameof(directionCandidates));
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
        public IReadOnlyList<AttackTargetDefensePreviewProjection> AttackTargetDefensePreviews { get; }

        /// <summary>
        /// The one stable-identity cue projection consumed by hero, minion, and token views. Attack,
        /// Minion Battle, effect choices, correction, and Clear all meet here; a view never resolves a
        /// coordinate back into an occupant and never receives two independently drifting cues.
        /// </summary>
        public IReadOnlyList<PieceTargetCueViewModel> PieceTargetCues { get; }

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

        /// <summary>
        /// The subset whose typed consuming operation is a direct Move, Place, Spawn, or token Place.
        /// It remains an effect choice for submission while sharing Movement's green board language.
        /// </summary>
        public IReadOnlyList<HexCoordinate> EffectChoiceMovementDestinations { get; }

        /// <summary>
        /// Milestone 8.11a: every space p.7's Hero Respawn offers, marked so the choice is made on the
        /// board. Declining to respawn has no space, so it stays a control in the prompt.
        /// </summary>
        public IReadOnlyList<HexCoordinate> HeroRespawnDestinations { get; }

        /// <summary>
        /// Milestone 8.16c: every equally-nearest empty space offered by p.8's blocked-Spawn-point
        /// Lane-Push Place, paired with the exact continuation invocation that space answers.
        /// </summary>
        public IReadOnlyList<SystemTransitionDestinationViewModel> SystemTransitionDestinations { get; }

        /// <summary>The space of the effect-choice target the pointer or keyboard focus is on, if any.</summary>
        public HexCoordinate? EffectChoiceTargetSpace { get; }

        /// <summary>
        /// Where the authority projects that target would end up. Present only alongside a pointed-at
        /// target, and never drawn as a destination: it is a consequence, not a choice.
        /// </summary>
        public HexCoordinate? EffectChoiceResultingSpace { get; }
        public HexCoordinate? HeroRespawnHighlight { get; }

        /// <summary>
        /// Milestone 8.35z63: which hero pieces an Active effect is standing on, and which
        /// spaces its disk covers, so the board can say in the tray's own violet what the rules are
        /// still doing.
        ///
        /// <para>Never null - <see cref="ActiveEffectVisibilityModel.Empty"/> is what "nothing is
        /// running" looks like - so every existing fixture that omits it renders exactly as before.
        /// It is table-public by construction: it is derived from
        /// <see cref="IMatchReadModel.ActiveEffectSourceCardInstanceIds"/>, which every seat and the
        /// Director have carried identically since milestone 8.5b, so all six seats and any spectator
        /// see the same rings and the same disks.</para>
        /// </summary>
        public ActiveEffectVisibilityModel ActiveEffects { get; }

        /// <summary>
        /// Milestone 8.46d: where the hero a standing direction choice is asking is standing, and null
        /// whenever no such choice stands. It is the centre the six arrows are drawn around.
        /// </summary>
        public HexCoordinate? DirectionChooserSpace { get; }

        /// <summary>
        /// The six directions that choice offers, each with the space its arrow stands on and the
        /// exact Select a click on it submits. Empty for every other decision, and empty for a seat
        /// that does not own this one - a non-owner is published no effect choice and no contract at
        /// all, so there is nothing to derive an offer from.
        /// </summary>
        public IReadOnlyList<DirectionCandidateViewModel> DirectionCandidates { get; }
    }

    public interface IBoardInteractionView
    {
        void Render(BoardInteractionViewModel model);
    }
}
