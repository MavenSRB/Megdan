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

        public const string CompletionLabel = "DONE";
        public const string AcceptLabel = "ACCEPT";
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

        /// <summary>
        /// Milestone 8.15: a candidate that names a card instance is named as that card, so the hero
        /// rulebook p.10 hands the choice to reads which card each control gives up rather than an
        /// anonymous ordinal - the first player-pipeline journey met `OPTION #1` where a discard
        /// chooser stood. The name is the card's projected display name, exactly the reading the
        /// tray draws the same card by, and the ordinal appears only where the offered set repeats a
        /// name, the same rule the token label follows.
        /// </summary>
        public static string CardTargetLabel(string displayName, int? ordinal) =>
            string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException(
                    "A card target label requires the projected display name.", nameof(displayName))
                : !ordinal.HasValue
                    ? displayName.ToUpperInvariant()
                    : ordinal.Value < 1
                        ? throw new ArgumentOutOfRangeException(nameof(ordinal))
                        : $"{displayName.ToUpperInvariant()} #{ordinal.Value}";
    }

    /// <summary>
    /// What a standing effect choice is asking about, as one typed fact.
    ///
    /// <para>Milestone 8.20d. It exists so the status line and the region heading can say which
    /// decision the match is waiting on without either of them reading an option identity, a
    /// resolution ID, a node name, or printed card text. The candidates the authority projected
    /// already carry their own kinds, and those kinds are the whole of this reading: a set of card
    /// instances is a question about cards, a set of integers is a question about a printed number,
    /// a set of option nodes is a question about modes.</para>
    /// </summary>
    public enum EffectChoiceSubject
    {
        /// <summary>Nothing is chosen among: a printed `may` is taken or refused.</summary>
        OptionalStep = 0,

        /// <summary>The modes of a control node.</summary>
        Mode = 1,

        Card = 2,

        /// <summary>A printed number, such as the kinetic `3 or 4 spaces` distance.</summary>
        Number = 3,

        /// <summary>Pieces - heroes, minions, tokens - whether standing on the board or in supply.</summary>
        Piece = 4,

        /// <summary>Empty spaces the actor may be put on.</summary>
        Space = 5,

        /// <summary>Candidates of more than one shape, so no narrower noun is true of all of them.</summary>
        Mixed = 6
    }

    /// <summary>
    /// Milestone 8.20d: the one place a standing effect choice says what it is.
    ///
    /// <para>Written because it had never said anything. The status derivation ran two phase checks
    /// and six pending-family branches and had no <see cref="PendingEffectChoice"/> branch at all, so
    /// every effect choice since milestone 8.15 - rulebook p.10's discard chooser above all - fell
    /// through to "Initiative N: choose an action or End Turn" while its controls sat under a row
    /// headed <c>ACTIONS</c>. A player being asked which card to give up was labelled as though they
    /// were picking an action. Milestone 8.20b's Wasp journey met it and recorded it rather than
    /// fixing it, because one derivation branch plus the heading corrects every effect choice at
    /// once and wanted doing in one place.</para>
    ///
    /// <para>Every word here is composed from the pause's own typed facts - its kind, its bounds, how
    /// many candidates it offers, how many are selected, and whether it projected a decline or a stop.
    /// Nothing decodes an option identity, a resolution ID, or a printed sentence, which is the same
    /// rule <see cref="EffectChoiceLabels"/> has followed since milestone 6.8a.</para>
    /// </summary>
    public static class EffectChoicePromptLabels
    {
        /// <summary>
        /// The one noun the offered candidates are all true of, or <see cref="EffectChoiceSubject.Mixed"/>
        /// where they are not. The previews are consulted only to separate a candidate naming a piece
        /// from one naming an empty space, which is the same reading milestone 8.11c gave the board.
        /// </summary>
        public static EffectChoiceSubject SubjectOf(
            PendingEffectChoice choice,
            IReadOnlyList<EffectChoiceCandidatePreview> previews)
        {
            if (choice == null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            if (choice.Kind == EffectChoiceKind.ConfirmOrDecline)
            {
                return EffectChoiceSubject.OptionalStep;
            }

            var named = new HashSet<EffectValue>((previews ??
                    Array.Empty<EffectChoiceCandidatePreview>())
                .Where(preview => preview.NamesObject)
                .Select(preview => preview.CandidateValue));
            var subjects = choice.Candidates
                .Select(candidate => SubjectOfCandidate(candidate, named))
                .Distinct()
                .ToArray();
            return subjects.Length == 1 ? subjects[0] : EffectChoiceSubject.Mixed;
        }

        private static EffectChoiceSubject SubjectOfCandidate(
            EffectChoiceCandidate candidate, HashSet<EffectValue> namesObject)
        {
            if (candidate.Kind == EffectChoiceCandidateKind.Option)
            {
                return EffectChoiceSubject.Mode;
            }

            switch (candidate.Value.Kind)
            {
                case EffectValueKind.CardInstance:
                    return EffectChoiceSubject.Card;
                case EffectValueKind.Integer:
                    return EffectChoiceSubject.Number;
                case EffectValueKind.Hero:
                case EffectValueKind.Minion:
                    return EffectChoiceSubject.Piece;
                case EffectValueKind.Coordinate:
                    // Milestone 8.11c's reading: a coordinate with something standing on it is that
                    // piece, and one with nothing standing on it is a space the actor may take.
                    return namesObject.Contains(candidate.Value)
                        ? EffectChoiceSubject.Piece
                        : EffectChoiceSubject.Space;
                default:
                    return EffectChoiceSubject.Mixed;
            }
        }

        /// <summary>
        /// What the status line says while this decision stands. It states the bounds the authority
        /// set, what is being chosen among, and the two answers that are not selections - declining,
        /// which milestone 8.20a made an answer a player must be able to give by name, and milestone
        /// 6.5ae's stop, which is the only answer left when every offered mode was refused.
        /// </summary>
        public static string StatusLine(PendingEffectChoice choice, EffectChoiceSubject subject)
        {
            if (choice == null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            if (choice.StopInvocation != null)
            {
                return "No offered mode can be performed: End Turn closes the action.";
            }

            if (choice.Kind == EffectChoiceKind.ConfirmOrDecline)
            {
                return "An optional step is offered: confirm it or decline.";
            }

            var count = choice.Candidates.Count;
            var bounds = choice.MinimumSelections == choice.MaximumSelections
                ? $"Choose {choice.MinimumSelections}"
                : choice.MinimumSelections == 0
                    ? $"Choose up to {choice.MaximumSelections}"
                    : $"Choose {choice.MinimumSelections} to {choice.MaximumSelections}";
            var line = $"{bounds} of {count} {Noun(subject, count)}" +
                (choice.DeclineInvocation != null ? ", or decline." : ".");
            return choice.MaximumSelections > 1
                ? $"{line} {choice.SelectedCandidates.Count} chosen."
                : line;
        }

        /// <summary>
        /// Adds the rules-facing purpose that can be read from the standing card candidates. A
        /// hand-only card set is the configured discard family; the configured retrieval family
        /// selects cards already outside the hand. This reads typed lifecycle and owner facts rather
        /// than a node identity, card name, or printed sentence.
        /// </summary>
        public static string StatusLine(
            PendingEffectChoice choice,
            EffectChoiceSubject subject,
            IMatchReadModel projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (subject != EffectChoiceSubject.Card)
                return StatusLine(choice, subject);

            var candidateIds = new HashSet<CardInstanceId>(choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => candidate.Value.CardInstanceId));
            var cards = projection.Cards
                .Where(card => candidateIds.Contains(card.Id))
                .ToArray();
            if (cards.Length != candidateIds.Count || cards.Length == 0)
                return StatusLine(choice, subject);

            var owners = cards.Select(card => card.OwnerId).Distinct().ToArray();
            if (owners.Length != 1)
                return StatusLine(choice, subject);

            var hero = projection.Setup.Heroes
                .SingleOrDefault(candidate => candidate.HeroId.Equals(owners[0]));
            var heroName = hero?.DisplayName ?? owners[0].Value;
            var count = choice.Candidates.Count;
            var bounds = choice.MinimumSelections == choice.MaximumSelections
                ? $"Choose {choice.MinimumSelections}"
                : choice.MinimumSelections == 0
                    ? $"Choose up to {choice.MaximumSelections}"
                    : $"Choose {choice.MinimumSelections} to {choice.MaximumSelections}";
            var purpose = cards.All(card => card.State.Location == CardLocation.Hand)
                ? $"for {heroName} to discard"
                : $"for {heroName} to retrieve";
            var line = $"{bounds} of {count} {(count == 1 ? "card" : "cards")} {purpose}" +
                (choice.DeclineInvocation != null ? ", or decline." : ".");
            return choice.MaximumSelections > 1
                ? $"{line} {choice.SelectedCandidates.Count} chosen."
                : line;
        }

        /// <summary>
        /// The semantic family label of the region holding this decision's controls. Non-card
        /// families paint it as their gutter; the whole-face card row deliberately suppresses its
        /// redundant visible label while retaining this typed classification for tests and assistive
        /// structure.
        /// </summary>
        public static string SectionLabel(EffectChoiceSubject subject)
        {
            switch (subject)
            {
                case EffectChoiceSubject.OptionalStep:
                    return "OPTIONAL STEP";
                case EffectChoiceSubject.Mode:
                    return "MODE CHOICE";
                case EffectChoiceSubject.Card:
                    return "CARD CHOICE";
                case EffectChoiceSubject.Number:
                    return "NUMBER CHOICE";
                case EffectChoiceSubject.Piece:
                    return "TARGET CHOICE";
                case EffectChoiceSubject.Space:
                    return "SPACE CHOICE";
                default:
                    return "CHOICE";
            }
        }

        private static string Noun(EffectChoiceSubject subject, int count)
        {
            switch (subject)
            {
                case EffectChoiceSubject.Mode:
                    return count == 1 ? "mode" : "modes";
                case EffectChoiceSubject.Card:
                    return count == 1 ? "card" : "cards";
                case EffectChoiceSubject.Number:
                    return count == 1 ? "printed number" : "printed numbers";
                case EffectChoiceSubject.Piece:
                    return count == 1 ? "target" : "targets";
                case EffectChoiceSubject.Space:
                    return count == 1 ? "space" : "spaces";
                default:
                    return count == 1 ? "option" : "options";
            }
        }
    }

    /// <summary>
    /// Milestone 8.21: the one place a standing removal-prevention decision says what it is.
    ///
    /// <para>Every word is composed from the projected pause's own typed facts - how many payments it
    /// offers, how many sources are offering them, and the price each one names. Nothing decodes a
    /// source id, a card id or a printed sentence, which is the rule
    /// <see cref="EffectChoicePromptLabels"/> follows and the reason this is a second composer rather
    /// than a branch inside that one: the two pauses carry different facts.</para>
    ///
    /// <para>The minion is not named here. It is standing on the board and it is cued there, which is
    /// milestone 8.11a's reading of every candidate that has a board surface - a prompt that named it
    /// as well would be two ways of saying one thing.</para>
    /// </summary>
    public static class RemovalPreventionPromptLabels
    {
        /// <summary>
        /// What the status line says while the decision stands. It states what is being bought, what
        /// it costs, and that declining is an answer - which it always is, because the removal was
        /// going to happen anyway.
        /// </summary>
        public static string StatusLine(RemovalPreventionPrompt prompt)
        {
            if (prompt == null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            var count = prompt.Payments.Count;
            if (count == 0)
            {
                // The rules keep offering the decision while any source covers the subject, and the
                // query drops an option nobody can afford - so a prompt with no payment left is a
                // decision whose only answer is the one that was always there.
                return "A friendly minion would be removed: no card can pay to keep it, so decline.";
            }

            var price = PriceOf(prompt);
            return $"A friendly minion would be removed: discard {price} to keep it, or decline. " +
                $"{count} {(count == 1 ? "card" : "cards")} can pay.";
        }

        /// <summary>
        /// The gutter label of the region holding the payments, naming the family independently of
        /// the frameless card-valued effect-choice row.
        /// </summary>
        public const string SectionLabel = "KEEP THE MINION";

        /// <summary>The control that lets the removal happen.</summary>
        public const string DeclineLabel = "DECLINE - LET IT BE REMOVED";

        /// <summary>
        /// The price the offering sources charge, in the words the declaration states it in: the
        /// colours it admits, or p.15's supertype where it names one instead.
        ///
        /// <para>Where two sources charge differently the prompt says so rather than picking one, and
        /// each payment still carries its own source, so the answer names which price is being
        /// paid.</para>
        /// </summary>
        private static string PriceOf(RemovalPreventionPrompt prompt)
        {
            var prices = prompt.Payments
                .Select(payment => PriceOf(payment.Cost))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return prices.Length == 1 ? prices[0] : "a card one of them accepts";
        }

        private static string PriceOf(RemovalPreventionCost cost)
        {
            if (cost.AdmittedColors.Count > 0)
            {
                return "a " + string.Join(
                    " or ",
                    cost.AdmittedColors.Select(color => color.ToString().ToLowerInvariant())) +
                    " card";
            }

            return cost.AdmittedClassification == CardClassification.None
                ? "a card"
                : $"a {cost.AdmittedClassification.ToString().ToLowerInvariant()} card";
        }
    }

    /// <summary>
    /// Milestone 6.3a: the one place the Clear surface names its controls.
    ///
    /// A candidate is named by <see cref="EffectChoiceLabels.TokenTargetLabel"/> rather than by a
    /// label of its own, because a player choosing which Hazard to clear and a player choosing which
    /// Hazard to Remove are looking at the same piece and must read the same words for it. The
    /// disambiguating ordinal therefore follows the same rule too: it appears only where the offered
    /// set repeats a name.
    /// </summary>
    public static class ClearLabels
    {
        /// <summary>
        /// The offer states what it replaces. Clear is a secondary action that costs the card's
        /// Attack (p.7, p.13), and a control saying only `CLEAR` would let a player spend the Attack
        /// without being told they had.
        /// </summary>
        public const string OfferLabel = "CLEAR (REPLACES ATTACK)";

        public const string ConfirmLabel = "CONFIRM CLEAR";
        public const string CancelLabel = "CANCEL CLEAR";

        public static string CandidateLabel(string displayName, int? ordinal) =>
            EffectChoiceLabels.TokenTargetLabel(displayName, ordinal);
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
            IEnumerable<AttackTargetDefensePreviewProjection> attackTargetDefensePreviews = null)
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
    }

    public interface IBoardInteractionView
    {
        void Render(BoardInteractionViewModel model);
    }

    /// <summary>
    /// Resolves a visible answer to the exact command object carried by the standing application
    /// contract. Predicates identify an already-typed answer; this helper never constructs one.
    /// </summary>
    internal static class PendingDecisionCommandBinding
    {
        public static DirectorCommand Require(
            IMatchReadModel projection,
            PendingDecisionFamilyId familyId,
            Func<DirectorCommand, bool> matches) =>
            Require(projection?.DecisionContract, familyId, matches);

        public static DirectorCommand Require(
            PendingDecisionContract contract,
            PendingDecisionFamilyId familyId,
            Func<DirectorCommand, bool> matches)
        {
            if (contract == null || !contract.FamilyId.Equals(familyId))
            {
                throw new InvalidOperationException(
                    $"The standing surface requires the {familyId.Value} pending-decision contract.");
            }

            if (matches == null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            var commands = contract.Commands.Where(matches).ToArray();
            if (commands.Length != 1)
            {
                throw new InvalidOperationException(
                    $"The {familyId.Value} surface resolved {commands.Length} exact commands; expected one.");
            }

            return commands[0];
        }
    }

    /// <summary>
    /// Milestone 6.5c: how an effect-choice selection reaches its end.
    ///
    /// A bounded choice is finished when a selection reaches its maximum - one mode, target, card,
    /// number, or destination for an exactly-one choice, or the final piece of a larger set. The
    /// authority still models that answer as select then confirm, so a player who has already filled
    /// the choice would otherwise be asked to say so twice. Presentation closes that by
    /// forwarding both invocations the authority itself offers, in the order it offers them; it
    /// invents no command, decides no legality, and changes nothing about how the choice confirms.
    /// A choice that has not reached its maximum keeps an explicit completion action, because there
    /// the selection may really be unfinished.
    ///
    /// It applies to every candidate kind. A pick is the commitment regardless of whether its surface
    /// is the map, a card face, a printed number, or a mode control. Choices with wider bounds retain
    /// an explicit completion action because selecting one candidate does not necessarily finish them.
    /// </summary>
    internal static class EffectChoiceCompletion
    {
        public static DirectorCommand FindImmediateConfirm(
            IMatchReadModel projection, EffectChoiceCommand accepted)
        {
            var choice = projection?.PendingEffectChoice;
            if (choice == null || accepted == null ||
                accepted.Kind != EffectChoiceCommandKind.Select ||
                !choice.ChoiceId.Equals(accepted.ChoiceId) ||
                !choice.ResolutionId.Equals(accepted.ResolutionId) ||
                choice.MaximumSelections < 1 ||
                choice.SelectedCandidates.Count != choice.MaximumSelections ||
                choice.ConfirmInvocation == null)
            {
                return null;
            }

            return PendingDecisionCommandBinding.Require(
                projection,
                PendingDecisionFamilies.Effect,
                command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    ReferenceEquals(command.EffectChoice, choice.ConfirmInvocation));
        }
    }

    public sealed class BoardInteractionPresenter
    {
        private readonly IMatchClient session;
        private readonly IBoardInteractionView view;
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

        public BoardInteractionPresenter(IMatchClient session, IBoardInteractionView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
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
                    projection.PendingAttackTargetDefensePreviews);
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
            var cues = new Dictionary<BoardObjectReference, PieceTargetCueViewModel>();

            void Add(
                BoardObjectReference target,
                BoardTargetCueState state,
                BoardTargetConsequenceIntent consequenceIntent)
            {
                var next = new PieceTargetCueViewModel(target, state, consequenceIntent);
                if (!cues.TryGetValue(target, out var current))
                {
                    cues.Add(target, next);
                    return;
                }

                if (current.ConsequenceIntent != consequenceIntent)
                {
                    throw new InvalidOperationException(
                        $"One piece cannot carry conflicting target consequence intents: {target}.");
                }

                if ((int)state > (int)current.State)
                {
                    cues[target] = next;
                }
            }

            var attack = projection.PendingAttackTargetChoice;
            if (attack != null)
            {
                foreach (var heroId in attack.LegalTargetHeroIds)
                {
                    Add(
                        BoardObjectReference.ForHero(heroId),
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

                    Add(
                        preview.Occupant,
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
            IEnumerable<TrayCardViewModel> trayCards = null)
        {
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
            HeroId viewedHeroId = default)
        {
            Version = version;
            Hands = Array.AsReadOnly((hands ?? throw new ArgumentNullException(nameof(hands))).ToArray());
            ViewedHeroId = viewedHeroId;
            ViewedHand = Hands.FirstOrDefault(hand => hand.HeroId.Equals(viewedHeroId)) ??
                Hands.FirstOrDefault();
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
        private IMatchReadModel lastProjection;
        private CardInstanceId? lastSelectedCardInstanceId;
        private DirectorHandsViewModel lastViewModel;
        private HeroId pinnedHeroId;
        private HeroId pinnedAgainstAwaitedHeroId;
        private HeroId lastViewedHeroId;


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
        /// The hero whose tray is currently drawn (milestone 7.8a).
        ///
        /// For the Director it defaults to the hero the match is waiting on - the one still owing a
        /// commitment, or the one resolving a turn. For a seat it defaults to that seat's home hero,
        /// whose private hand is the one the projection actually carries. A map selection pins another
        /// hero, and the pin lasts exactly as long as the currently awaited hero does; when the match
        /// moves on, the Director follows the new wait and a seat returns to its own tray.
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
            if (!heroId.IsDefined ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

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
            lastProjection = null;
            Refresh();
            return true;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => homeHeroId;

        private HeroId Resolve(IMatchReadModel projection)
        {
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

        public void Refresh()
        {
            var projection = session.Projection;
            if (!projection.IsInputEnabled ||
                selectedCardInstanceId.HasValue && !projection.Cards.Any(card =>
                card.Id.Equals(selectedCardInstanceId.Value) && card.State.Location == CardLocation.Hand))
            {
                selectedCardInstanceId = null;
            }

            var viewedHeroId = Resolve(projection);
            if (!ReferenceEquals(projection, lastProjection) ||
                !Nullable.Equals(selectedCardInstanceId, lastSelectedCardInstanceId) ||
                !viewedHeroId.Equals(lastViewedHeroId) ||
                lastViewModel == null)
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
                            projection.IsInputEnabled && commitment.CanPass,
                            commitment.RevealedCardInstanceId,
                            // Milestone 8.35e. `Single` here failed the whole tray refresh, and the
                            // absence it refused is legitimate: rulebook p.8's Retrieve stage puts
                            // each hero's revealed card back in that hero's hand, and a seat's
                            // scoped card list withholds another hero's hand and deck together, so
                            // from that stage until the next round every seat holds five
                            // commitments naming a card its own projection may not carry. The
                            // identity stays - it is public, and the label reads REVEALED from it -
                            // and only the card's detail is missing. `HeroOverviewPresentation`
                            // already reads the same field this way.
                            commitment.RevealedCardInstanceId.HasValue
                                ? projection.Cards.SingleOrDefault(card =>
                                    card.Id.Equals(commitment.RevealedCardInstanceId.Value))
                                : null,
                            projection.Cards.Where(card =>
                                card.OwnerId.Equals(hero.HeroId) && card.State.Location == CardLocation.Hand),
                            projection.Definitions,
                            selectedCardInstanceId,
                            BuildTray(projection, hero.HeroId, selectedCardInstanceId));
                    });
                lastProjection = projection;
                lastSelectedCardInstanceId = selectedCardInstanceId;
                lastViewedHeroId = viewedHeroId;
                lastViewModel = new DirectorHandsViewModel(projection.Version, hands, viewedHeroId);
            }

            view.Render(lastViewModel);
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
        /// </summary>
        private static IReadOnlyList<TrayCardViewModel> BuildTray(
            IMatchReadModel projection,
            HeroId heroId,
            CardInstanceId? selectedCardInstanceId)
        {
            var commitment = projection.GetCommitment(heroId);
            var canSelect = projection.IsInputEnabled &&
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
            foreach (var card in projection.Cards.Where(card => card.OwnerId.Equals(heroId)))
            {
                var group = card.State.Location == CardLocation.Hand
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
                    // A card in the turn area that this round's history does not name is the facedown
                    // commitment of a turn that has not been revealed yet. It is the hero's own card and
                    // is no longer choosable, so it reads as spent rather than as available.
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
                var resolutionIsUnderWayForThisHero = projection.IsTurnActive &&
                    projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(heroId);
                entries.Add(CurrentRoundCards.TrayCard(
                    projection,
                    card,
                    group,
                    group == TrayCardGroup.Played ? playedTurnIndex : 0,
                    isAvailableForCommitment,
                    selectedCardInstanceId.HasValue && selectedCardInstanceId.Value.Equals(card.Id),
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
            if (FindCommittableCard(projection, cardInstanceId) == null)
            {
                return false;
            }

            selectedCardInstanceId = cardInstanceId;
            Refresh();
            return true;
        }

        public bool ConfirmSelectedCard()
        {
            if (!selectedCardInstanceId.HasValue || !session.Projection.IsInputEnabled)
            {
                return false;
            }

            var card = session.Projection.Cards.SingleOrDefault(value => value.Id.Equals(selectedCardInstanceId.Value));
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

        /// <summary>
        /// Commits one currently projected hand card without first storing it as the click-selection.
        /// Milestone 8.30g uses this for the drag-to-map path: the pointer's drop position is deliberately
        /// absent because the map is only a presentation boundary, never a card target or rules input.
        /// The card is re-read from the standing projection immediately before submission, so a stale,
        /// hidden, already committed, or otherwise ineligible drag is refused without mutation.
        /// </summary>
        public bool CommitCardImmediately(CardInstanceId cardInstanceId)
        {
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

        private static CardInstance FindCommittableCard(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (!projection.IsInputEnabled ||
                projection.Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                return null;
            }

            var card = projection.Cards.SingleOrDefault(value => value.Id.Equals(cardInstanceId));
            return card != null &&
                card.State.Location == CardLocation.Hand &&
                projection.GetCommitment(card.OwnerId).Status ==
                    TurnCardCommitmentStatus.AwaitingResponse
                ? card
                : null;
        }

        public bool Pass(HeroId heroId)
        {
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

    public sealed class TypedInvocationViewModel
    {
        public TypedInvocationViewModel(
            string label,
            DirectorCommand command,
            DefenseBreakdownViewModel defenseBreakdown = null,
            TrayCardViewModel card = null,
            CardShineSlot defenseShineSlot = CardShineSlot.None)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A typed invocation requires a label.", nameof(label))
                : label;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            DefenseBreakdown = defenseBreakdown;
            Card = card;
            DefenseShineSlot = defenseShineSlot;
        }

        public string Label { get; }
        public DirectorCommand Command { get; }
        public DefenseBreakdownViewModel DefenseBreakdown { get; }

        /// <summary>
        /// Which printed badge this Defense offer would defend with, stated by the presenter from
        /// the offered action's typed origin - the rail shield for a secondary Defense, the primary
        /// printed-lines ring for a Duelist or Block - so the view lights it without inferring anything from
        /// card content. <see cref="CardShineSlot.None"/> on every invocation that is not a card
        /// Defense offer, DECLINE included. Milestone 8.30d.
        /// </summary>
        public CardShineSlot DefenseShineSlot { get; }

        /// <summary>
        /// The card this invocation is about, as the tray draws one, or null when it is not about a
        /// card.
        ///
        /// Milestone 8.11b. p.11's Defense response is a choice between cards in the defender's own
        /// hand, and until now it was offered as an abbreviated text button while the same cards sat
        /// on the tray as whole faces - two designs for one card. The face is carried here so the
        /// region can draw the offer with <see cref="DirectorHandsUnityView.BuildStandaloneCard"/>,
        /// which is the builder every other card surface uses. <c>DECLINE DEFENSE</c> is about no card
        /// and carries none, which is what keeps it a distinct control rather than a sixth face.
        /// </summary>
        public TrayCardViewModel Card { get; }
    }

    public sealed class DefenseContributionRowViewModel
    {
        public DefenseContributionRowViewModel(int amount, string sourceLabel)
        {
            Amount = amount;
            SourceLabel = string.IsNullOrWhiteSpace(sourceLabel)
                ? throw new ArgumentException(
                    "A Defense contribution row requires a source label.", nameof(sourceLabel))
                : sourceLabel;
        }

        public int Amount { get; }
        public string SourceLabel { get; }
        public string SignedAmount => Amount >= 0 ? $"+{Amount}" : Amount.ToString();
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
            AttackResolutionOutcome outcome,
            IEnumerable<DefenseContributionRowViewModel> attackRows,
            int attackTotal,
            IEnumerable<DefenseContributionRowViewModel> defenseRows,
            int? defenseTotal,
            DefenseBlockResult blockResult,
            string resultStatement)
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
            AttackRows = Array.AsReadOnly((attackRows ??
                throw new ArgumentNullException(nameof(attackRows))).ToArray());
            DefenseRows = Array.AsReadOnly((defenseRows ??
                throw new ArgumentNullException(nameof(defenseRows))).ToArray());
            if (AttackRows.Sum(value => value.Amount) != attackTotal ||
                (defenseTotal.HasValue && DefenseRows.Sum(value => value.Amount) != defenseTotal.Value))
            {
                throw new ArgumentException(
                    "Defense breakdown totals must equal their typed contribution rows.");
            }
            AttackTotal = attackTotal;
            DefenseTotal = defenseTotal;
            BlockResult = blockResult;
            ResultStatement = string.IsNullOrWhiteSpace(resultStatement)
                ? throw new ArgumentException(
                    "A Defense breakdown requires a result statement.", nameof(resultStatement))
                : resultStatement;
        }

        public long Version { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public CardInstanceId? CardInstanceId { get; }
        public string Title { get; }
        public string Text { get; }
        public AttackResolutionOutcome Outcome { get; }
        public IReadOnlyList<DefenseContributionRowViewModel> AttackRows { get; }
        public int AttackTotal { get; }
        public IReadOnlyList<DefenseContributionRowViewModel> DefenseRows { get; }
        public int? DefenseTotal { get; }
        public DefenseBlockResult BlockResult { get; }
        public string ResultStatement { get; }
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
    /// One printed action on the currently resolving card, bound to the exact offer the authority
    /// projected. The view may place this on a fixed card region; it never has to infer an action from
    /// artwork, printed words, a hero, or a card name.
    /// </summary>
    public sealed class PrintedActionRegionViewModel
    {
        public PrintedActionRegionViewModel(
            ActionOffer offer,
            ActionKind actionKind,
            string actionLabel,
            int secondaryIndex,
            int secondaryCount,
            string assistiveLabel,
            TypedInvocationViewModel invocation,
            FastTravelOfferViewModel fastTravelOffer = null,
            ClearOfferViewModel clearOffer = null,
            CardColor cardColor = CardColor.None)
        {
            if (!offer.SourceCardInstanceId.IsDefined || !offer.ActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("A printed action region requires its exact projected offer.");
            }

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary && secondaryIndex < 0)
            {
                throw new ArgumentException("A Secondary printed region requires its ordered index.");
            }

            Offer = offer;
            ActionKind = actionKind;
            ActionLabel = string.IsNullOrWhiteSpace(actionLabel)
                ? throw new ArgumentException("A printed action region requires its typed label.", nameof(actionLabel))
                : actionLabel;
            SecondaryIndex = secondaryIndex;
            SecondaryCount = secondaryCount;
            AssistiveLabel = string.IsNullOrWhiteSpace(assistiveLabel)
                ? throw new ArgumentException(
                    "A printed action region requires an assistive reading.", nameof(assistiveLabel))
                : assistiveLabel;
            Invocation = invocation;
            FastTravelOffer = fastTravelOffer;
            ClearOffer = clearOffer;
            CardColor = cardColor;
        }

        public ActionOffer Offer { get; }
        public CardInstanceId SourceCardInstanceId => Offer.SourceCardInstanceId;
        public ActionDefinitionId ActionDefinitionId => Offer.ActionDefinitionId;
        public ActionInvocationContext InvocationContext => Offer.InvocationContext;
        public ActionKind ActionKind { get; }
        public string ActionLabel { get; }
        public int SecondaryIndex { get; }
        public int SecondaryCount { get; }
        public string AssistiveLabel { get; }

        /// <summary>The exact projected start, or null when the authority stamped the offer unavailable.</summary>
        public TypedInvocationViewModel Invocation { get; }

        /// <summary>The existing typed replacement selectors attached to the action they replace.</summary>
        public FastTravelOfferViewModel FastTravelOffer { get; }
        public ClearOfferViewModel ClearOffer { get; }

        /// <summary>
        /// The printed colour of the card this region sits on - a typed fact from the definition,
        /// carried so the replacement ribbons can wear the card's own cloth.
        /// </summary>
        public CardColor CardColor { get; }
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
            DirectorCommand command,
            TokenInstanceId tokenId = default)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("An effect choice target requires a label.", nameof(label))
                : label;
            Space = space;
            ResultingSpace = resultingSpace;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            TokenId = tokenId;
        }

        public string Label { get; }
        public HexCoordinate Space { get; }

        /// <summary>Where the authority projects this target would end up, when an operation moves it.</summary>
        public HexCoordinate? ResultingSpace { get; }
        public DirectorCommand Command { get; }

        /// <summary>
        /// Milestone 6.3a: the token this control names, or an undefined identity when it names a
        /// hero, a minion, or an empty space.
        ///
        /// It rides beside <see cref="Space"/> rather than replacing it, because the two answer
        /// different questions. The space is what the command carries and what a hero or minion cue is
        /// resolved by; the identity is what lets the board mark the piece without reading occupancy
        /// back out of a coordinate. Milestone 6.6a's finding was exactly this gap: the control read
        /// `TEST - HAZARD` and the board marked nothing, because a token view had no cue to set and
        /// the space alone could not say which piece was meant.
        /// </summary>
        public TokenInstanceId TokenId { get; }
    }

    /// <summary>
    /// Milestone 6.3a: the control that starts a Clear, bound to the exact Attack it replaces.
    ///
    /// It carries the source card and the actor beside the replaced action, because a Clear is one
    /// card's replacement rather than a general-purpose button: the presenter re-reads the offer off
    /// the current projection before it starts anything, and these three are what it re-reads it by.
    /// </summary>
    public sealed class ClearOfferViewModel
    {
        public ClearOfferViewModel(
            string label,
            ActionDefinitionId replacedAttackActionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            int candidateCount,
            bool isAvailable = true)
        {
            IsAvailable = isAvailable;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Clear offer requires a label.", nameof(label))
                : label;
            if (!replacedAttackActionDefinitionId.IsDefined || !sourceCardInstanceId.IsDefined ||
                !actorId.IsDefined || candidateCount < 0)
            {
                throw new ArgumentException(
                    "A Clear offer requires its replaced Attack, source card, and actor.");
            }

            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActorId = actorId;
            CandidateCount = candidateCount;
        }

        public string Label { get; }
        public ActionDefinitionId ReplacedAttackActionDefinitionId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public HeroId ActorId { get; }

        /// <summary>
        /// How many adjacent tokens this Clear would offer. Zero is drawn rather than hidden: milestone
        /// 8.8b greys such an offer and refuses it, on the treatment every other refused control here
        /// gets, rather than taking the control off the surface.
        /// </summary>
        public int CandidateCount { get; }

        /// <summary>
        /// Whether the authority currently offers this Clear as one that can be started.
        ///
        /// Milestone 8.8b. It is the authority's own stamp on the replacement offer, never a count made
        /// here: a Clear with no adjacent token could remove nothing whatever the hero chose, so it is
        /// drawn greyed and refused at the start, exactly as an inert Skill and a targetless Attack are.
        /// p.13's "any number" still includes zero wherever a token *is* adjacent, and that case stays
        /// available.
        /// </summary>
        public bool IsAvailable { get; }
    }

    /// <summary>
    /// Milestone 6.3a: one token the open Clear selection offers, and whether it is currently chosen.
    /// The identity is the token's own, so pointing at this control and marking the piece agree
    /// without presentation holding a second notion of which object a control means.
    /// </summary>
    public sealed class ClearTokenCandidateViewModel
    {
        public ClearTokenCandidateViewModel(
            string label,
            TokenInstanceId tokenId,
            HexCoordinate space,
            bool isSelected)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Clear candidate requires a label.", nameof(label))
                : label;
            TokenId = tokenId.IsDefined
                ? tokenId
                : throw new ArgumentException(
                    "A Clear candidate names the token it stands for.", nameof(tokenId));
            Space = space;
            IsSelected = isSelected;
        }

        public string Label { get; }
        public TokenInstanceId TokenId { get; }
        public HexCoordinate Space { get; }
        public bool IsSelected { get; }
    }

    /// <summary>
    /// Milestone 6.3a: the open Clear selection - every offered token, the exact command for the
    /// subset currently chosen, and the way back out.
    ///
    /// Selection is local to presentation and reaches the session exactly once, on confirm. That is
    /// the shape milestone 6.3 built for: it made Clear one atomic command with no pending
    /// continuation, so there is no authoritative selection state to answer into and inventing one
    /// would be a second Clear rule. <see cref="Cancel"/> is deliberately a label rather than a
    /// <see cref="TypedInvocationViewModel"/> - it submits nothing at all, and a type that cannot
    /// carry a command cannot be routed into one by mistake.
    /// </summary>
    public sealed class ClearSelectionViewModel
    {
        public ClearSelectionViewModel(
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<ClearTokenCandidateViewModel> candidates,
            TypedInvocationViewModel confirm,
            string cancelLabel)
        {
            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId.IsDefined
                ? replacedAttackActionDefinitionId
                : throw new ArgumentException(
                    "A Clear selection names the Attack it replaces.",
                    nameof(replacedAttackActionDefinitionId));
            Candidates = Array.AsReadOnly((candidates ??
                throw new ArgumentNullException(nameof(candidates))).ToArray());
            if (Candidates.Select(value => value.TokenId).Distinct().Count() != Candidates.Count)
            {
                throw new ArgumentException(
                    "A Clear selection offers each token once.", nameof(candidates));
            }

            Confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
            CancelLabel = string.IsNullOrWhiteSpace(cancelLabel)
                ? throw new ArgumentException(
                    "A Clear selection requires a cancel label.", nameof(cancelLabel))
                : cancelLabel;
            SelectedTokenIds = Array.AsReadOnly(Candidates
                .Where(value => value.IsSelected)
                .Select(value => value.TokenId)
                .ToArray());
        }

        public ActionDefinitionId ReplacedAttackActionDefinitionId { get; }
        public IReadOnlyList<ClearTokenCandidateViewModel> Candidates { get; }
        public IReadOnlyList<TokenInstanceId> SelectedTokenIds { get; }

        /// <summary>
        /// The projected invocation for exactly the chosen subset. Always present, because p.13
        /// permits any number and the empty subset is a confirmable answer rather than a decline.
        /// </summary>
        public TypedInvocationViewModel Confirm { get; }

        public string CancelLabel { get; }
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

    /// <summary>
    /// Milestone 8.20d: the standing effect choice as the region that draws it needs to know it -
    /// what it is asking about, and its semantic section label.
    ///
    /// <para>Null whenever no effect choice is pending, and non-null whenever one is, so the region
    /// cannot draw a decision's controls without knowing their family. Non-card families paint the
    /// label; card-valued choices use purpose-bearing narration and an unlabelled whole-face row. The
    /// words are composed by <see cref="EffectChoicePromptLabels"/>, which is also where
    /// <see cref="RoundActionViewModel.Status"/> gets its sentence, so a heading and a status line
    /// describing one pause cannot disagree about which pause it is.</para>
    /// </summary>
    public sealed class EffectChoicePromptViewModel
    {
        public EffectChoicePromptViewModel(EffectChoiceSubject subject)
        {
            Subject = subject;
            SectionLabel = EffectChoicePromptLabels.SectionLabel(subject);
        }

        public EffectChoiceSubject Subject { get; }

        public string SectionLabel { get; }
    }

    /// <summary>
    /// Milestone 8.21. The standing removal-prevention decision as the region draws it: the heading it
    /// arrives under, one control per affordable payment - each the whole printed face of the card that
    /// would pay - the decline that is always offered, and the minion the price would keep.
    ///
    /// <para>The payments are the projection's own minted commands, carried through unchanged. This
    /// model composes no option of its own, which is what makes an answer the pause did not project
    /// unsubmittable from this surface rather than merely absent from it.</para>
    /// </summary>
    public sealed class RemovalPreventionPromptViewModel
    {
        public RemovalPreventionPromptViewModel(
            MinionId subjectMinionId,
            IEnumerable<TypedInvocationViewModel> payments,
            TypedInvocationViewModel decline)
        {
            if (!subjectMinionId.IsDefined)
            {
                throw new ArgumentException(
                    "A removal-prevention prompt names the miniature it is about, so the board can " +
                    "cue it.",
                    nameof(subjectMinionId));
            }

            SubjectMinionId = subjectMinionId;
            Payments = Array.AsReadOnly((payments ??
                throw new ArgumentNullException(nameof(payments))).ToArray());
            Decline = decline ?? throw new ArgumentNullException(
                nameof(decline),
                "Declining is always offered: the removal was going to happen anyway, and a decision " +
                "that could not be refused would stop the match.");
            SectionLabel = RemovalPreventionPromptLabels.SectionLabel;
        }

        /// <summary>The minion the price would keep on the board, cued there rather than named here.</summary>
        public MinionId SubjectMinionId { get; }

        /// <summary>One control per affordable payment, each carrying the face of the card that pays.</summary>
        public IReadOnlyList<TypedInvocationViewModel> Payments { get; }

        public TypedInvocationViewModel Decline { get; }

        public string SectionLabel { get; }
    }

    /// <summary>
    /// The two voices milestone 8.30d gives the top status bar's narration: the warm lane for a
    /// sentence addressed to the viewing seat, the calm one for everything the match is doing on
    /// its own or at another seat.
    /// </summary>
    public enum NarrationVoice
    {
        Calm = 0,
        YourMove = 1
    }

    public sealed class TeamAgreementChoiceViewModel
    {
        public TeamAgreementChoiceViewModel(
            string label, HeroId heroId, TeamAgreementCommand command)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            HeroId = heroId;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Label { get; }
        public HeroId HeroId { get; }
        public TeamAgreementCommand Command { get; }
    }

    public sealed class TeamAgreementParticipantViewModel
    {
        public TeamAgreementParticipantViewModel(string label, string status)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }

        public string Label { get; }
        public string Status { get; }
    }

    public sealed class TeamAgreementHudViewModel
    {
        public TeamAgreementHudViewModel(
            TeamAgreementKind kind,
            string heading,
            IEnumerable<TeamAgreementParticipantViewModel> participants,
            IEnumerable<TeamAgreementChoiceViewModel> choices,
            bool isInputEnabled = true)
        {
            Kind = kind;
            Heading = heading ?? throw new ArgumentNullException(nameof(heading));
            Participants = Array.AsReadOnly((participants ??
                throw new ArgumentNullException(nameof(participants))).ToArray());
            Choices = Array.AsReadOnly((choices ??
                Enumerable.Empty<TeamAgreementChoiceViewModel>()).ToArray());
            IsInputEnabled = isInputEnabled;
        }

        public TeamAgreementKind Kind { get; }
        public string Heading { get; }
        public IReadOnlyList<TeamAgreementParticipantViewModel> Participants { get; }
        public IReadOnlyList<TeamAgreementChoiceViewModel> Choices { get; }

        /// <summary>
        /// Milestone 8.35z2. Mirrors <c>IMatchReadModel.IsInputEnabled</c> at the moment this model was
        /// built. A seat whose <see cref="Choices"/> is empty because it is not a participant at all
        /// keeps this <c>true</c> (nothing to wait on); a participant whose own statement is in flight
        /// - or whose whole client is momentarily desynchronized - reads <c>false</c> here, which is
        /// what tells an empty <see cref="Choices"/> apart from a genuinely quiet one.
        /// </summary>
        public bool IsInputEnabled { get; }
    }

    internal static class TeamAgreementHudPresentation
    {
        public static TeamAgreementHudViewModel Create(IMatchReadModel projection)
        {
            var agreement = projection?.TeamAgreement;
            if (agreement == null)
                return null;

            // Milestone 8.35z2. `NetworkMatchClient` forces `AuthorizedCommands` empty for the whole
            // window its own `actionable` gate is false - a participant's just-submitted statement in
            // flight, or a stale/resynchronizing/disconnected client - which is otherwise
            // indistinguishable here from a seat that was never a participant at all (permanently
            // empty, e.g. the non-choosing Minion Battle side). Carrying the flag lets the view draw
            // "WAITING FOR HOST…" only for the first case.
            var isInputEnabled = projection.IsInputEnabled;

            var heroNames = projection.Setup.Heroes.ToDictionary(
                hero => hero.HeroId,
                hero => hero.DisplayName.ToUpperInvariant());
            string Name(HeroId heroId) => heroNames.TryGetValue(heroId, out var name)
                ? name
                : heroId.Value.ToUpperInvariant();

            if (agreement.Kind == TeamAgreementKind.Initiative)
            {
                var candidateNames = agreement.Candidates.ToDictionary(
                    candidate => candidate.CardInstanceId,
                    candidate => Name(candidate.HeroId));

                // Milestone 8.35h. Read with the same tolerance the hero-name lookup above already
                // has. A bare indexer and a `Single` sat beside that `TryGetValue` fallback in this
                // one method, which is the shape milestone 8.35e's scoped-seat crash came in: a
                // presenter shared between the Director and a seat must not assume the Director's
                // complete collections. A candidate this seat cannot name degrades to its own
                // identity rather than taking the panel - and, before 8.35h, the whole HUD - down.
                string CandidateName(CardInstanceId cardInstanceId) =>
                    candidateNames.TryGetValue(cardInstanceId, out var candidateName)
                        ? candidateName
                        : cardInstanceId.Value.ToUpperInvariant();
                HeroId CandidateHero(CardInstanceId cardInstanceId)
                {
                    foreach (var candidate in agreement.Candidates)
                    {
                        if (candidate.CardInstanceId.Equals(cardInstanceId))
                        {
                            return candidate.HeroId;
                        }
                    }

                    return default;
                }

                return new TeamAgreementHudViewModel(
                    agreement.Kind,
                    "WHO ACTS FIRST?",
                    agreement.Participants.Select(participant =>
                        new TeamAgreementParticipantViewModel(
                            Name(participant.HeroId),
                            participant.HasStatement
                                ? "chose " + CandidateName(participant.PreferredCardInstanceId)
                                : "is choosing")),
                    agreement.AuthorizedCommands.Select(command =>
                        new TeamAgreementChoiceViewModel(
                            CandidateName(command.PreferredCardInstanceId),
                            CandidateHero(command.PreferredCardInstanceId),
                            command)),
                    isInputEnabled);
            }

            return new TeamAgreementHudViewModel(
                agreement.Kind,
                "TEAM AGREEMENT",
                agreement.Participants.Select(participant =>
                    new TeamAgreementParticipantViewModel(
                        Name(participant.HeroId),
                        participant.Agreed ? "agreed" : "is deciding")),
                agreement.AuthorizedCommands.Select(command =>
                    new TeamAgreementChoiceViewModel(
                        command.Kind == TeamAgreementCommandKind.WithdrawMinionBattleAgreement
                            ? "WITHDRAW MY AGREEMENT"
                            : "AGREE WITH THIS SELECTION",
                        default,
                        command)),
                isInputEnabled);
        }
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
            IEnumerable<UnavailableActionViewModel> unavailableActions = null,
            IEnumerable<ClearOfferViewModel> clearOffers = null,
            ClearSelectionViewModel clearSelection = null,
            SystemTransitionPromptViewModel systemTransitionPrompt = null,
            EffectChoicePromptViewModel effectChoicePrompt = null,
            RemovalPreventionPromptViewModel removalPreventionPrompt = null,
            IEnumerable<PrintedActionRegionViewModel> printedActionRegions = null,
            TeamAgreementHudViewModel teamAgreement = null,
            bool seatUndoAvailable = false,
            bool isAwaitingViewerInput = false,
            string statusDetail = null,
            bool endTurnCancelsOpenAttack = false,
            string endTurnUnavailableReason = null)
        {
            IsAwaitingViewerInput = isAwaitingViewerInput;
            StatusDetail = statusDetail;
            SeatUndoAvailable = seatUndoAvailable;
            EndTurnCancelsOpenAttack = endTurnCancelsOpenAttack;
            EndTurnUnavailableReason = endTurnUnavailableReason;
            SystemTransitionPrompt = systemTransitionPrompt;
            EffectChoicePrompt = effectChoicePrompt;
            RemovalPreventionPrompt = removalPreventionPrompt;
            TeamAgreement = teamAgreement;
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
            ClearOffers = Array.AsReadOnly((clearOffers ??
                Enumerable.Empty<ClearOfferViewModel>()).ToArray());
            ClearSelection = clearSelection;
            PrintedActionRegions = Array.AsReadOnly((printedActionRegions ??
                Enumerable.Empty<PrintedActionRegionViewModel>()).ToArray());
            if (clearSelection != null && !ClearOffers.Any(offer =>
                    offer.ReplacedAttackActionDefinitionId.Equals(
                        clearSelection.ReplacedAttackActionDefinitionId)))
            {
                throw new ArgumentException(
                    "An open Clear selection belongs to an offer this model still carries.",
                    nameof(clearSelection));
            }

            // Milestone 8.20d: a decision's controls and the heading that names them arrive together
            // or not at all. Before this the controls were appended to the shared `ACTIONS` row, so
            // there was nothing to disagree with; now that the region asks the model what to call
            // them, a model carrying controls and no prompt would put a discard chooser back under
            // the heading this milestone exists to stop.
            if (effectChoicePrompt == null &&
                (EffectChoices.Count > 0 || UnavailableEffectChoices.Count > 0))
            {
                throw new ArgumentException(
                    "An effect choice's controls are drawn under the prompt that names them, so a " +
                    "model carrying either list carries the prompt too.",
                    nameof(effectChoicePrompt));
            }

            // Milestone 8.35v (the THIRD amendment): END TURN reads as exactly one of three
            // mutually exclusive facts - a direct invocation, "press it and it cancels the open
            // attack selection first", or a named reason it is drawn disabled - never two at once,
            // which is what would let a view draw one and a driver read another.
            var endTurnFactCount = (endTurnInvocation != null ? 1 : 0) +
                (endTurnCancelsOpenAttack ? 1 : 0) +
                (endTurnUnavailableReason != null ? 1 : 0);
            if (endTurnFactCount > 1)
            {
                throw new ArgumentException(
                    "End Turn's invocation, its cancels-open-attack flag and its unavailable " +
                    "reason are mutually exclusive.",
                    nameof(endTurnInvocation));
            }

            HudHeader =$"MIRRORED WASP QUICK GAME - DIRECTOR MODE - ROUND {roundIndex} - " +
                $"TURN {turnIndex}/{RoundResolutionAuthority.TurnsPerRound} - {phase.ToString().ToUpperInvariant()}";
        }

        public long Version { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public string Status { get; }

        /// <summary>
        /// Which voice the narration lane speaks <see cref="Status"/> in. Milestone 8.30d: warm
        /// when this model carries anything the viewing seat can act on - the sentence is then
        /// addressed to the player - and calm otherwise. It is derived from the same lists the
        /// region renders, so the voice and the controls cannot drift apart; a seat-scoped model
        /// carries only that seat's own offers, which is what makes presence mean "yours".
        /// </summary>
        /// <summary>
        /// Whether the match is waiting on this viewer, from
        /// <see cref="AwaitedInputQuery"/>. The top bar's gold and this lane's warm voice read the
        /// one flag, so the two surfaces cannot come to disagree about whose move it is.
        /// </summary>
        public bool IsAwaitingViewerInput { get; }

        /// <summary>
        /// Milestone 8.35n (the FIFTH amendment, typography). A secondary fact the narration lane
        /// draws smaller and quieter beside <see cref="Status"/>, demoting Initiative to secondary
        /// detail exactly as the amendment specifies - "Initiative may stay as secondary detail" -
        /// rather than folding it back into the headline sentence it used to be the whole of. Null
        /// wherever a branch has nothing to demote.
        /// </summary>
        public string StatusDetail { get; }

        public NarrationVoice Voice =>
            IsAwaitingViewerInput ? NarrationVoice.YourMove : NarrationVoice.Calm;
        public IReadOnlyList<TypedInvocationViewModel> Actions { get; }
        public TypedInvocationViewModel EndTurnInvocation { get; }

        /// <summary>
        /// Milestone 8.35v (the THIRD amendment: "end turn button should be present the whole time
        /// on players action turn, even if he has attack selection on"). True exactly when the
        /// acting seat has opened an Attack whose target is not yet chosen - the one open,
        /// not-yet-committed selection this build has no authority-offered decline for, so END TURN
        /// composes the seat's own bounded rewind (milestone 8.35f, still short of its wall) to
        /// discard it before completing and releasing the turn, all in the one press. Mutually
        /// exclusive with <see cref="EndTurnInvocation"/> and <see cref="EndTurnUnavailableReason"/>.
        /// </summary>
        public bool EndTurnCancelsOpenAttack { get; }

        /// <summary>
        /// Milestone 8.35v. Non-null exactly when it is this seat's own active turn and END TURN is
        /// drawn DISABLED rather than hidden - the user's rule that nothing is greyed without saying
        /// why, extended to END TURN itself: a printed rule leaves the standing step no legal decline
        /// (<see cref="ActionUnavailabilityLabels.MandatoryStep"/>), or the resolution has moved past
        /// anything this seat can answer or cancel (<see cref="ActionUnavailabilityLabels.AnotherSeatDeciding"/>).
        /// Mutually exclusive with <see cref="EndTurnInvocation"/> and
        /// <see cref="EndTurnCancelsOpenAttack"/>; null (and the control hidden) wherever it is not
        /// this seat's own active turn at all, exactly as before this milestone.
        /// </summary>
        public string EndTurnUnavailableReason { get; }

        public TypedInvocationViewModel ContinueEndOfRoundInvocation { get; }

        /// <summary>
        /// Milestone 8.35f. Whether this seat's own UNDO control stands. It carries no invocation,
        /// because a rewind is not a command a surface could forward: the control asks the client,
        /// the client asks the host, and the host decides by the same rule that published this.
        /// </summary>
        public bool SeatUndoAvailable { get; }
        public IReadOnlyList<TypedInvocationViewModel> InitiativeChoices { get; }

        public TeamAgreementHudViewModel TeamAgreement { get; }
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

        /// <summary>
        /// Ordinary printed starts as typed regions of the resolving face. Headless callers retain
        /// <see cref="Actions"/> and <see cref="UnavailableActions"/>; the configured HUD draws this
        /// list instead of duplicating those starts in its external dock.
        /// </summary>
        public IReadOnlyList<PrintedActionRegionViewModel> PrintedActionRegions { get; }

        public IReadOnlyList<HeroRespawnOptionViewModel> HeroRespawnOptions { get; }
        public TypedInvocationViewModel ResolveWithoutRespawn { get; }

        /// <summary>
        /// Milestone 6.3a: the Clear replacements the current card offers, one per available
        /// replaceable Attack. Empty when the card prints none, which is how a player reads that this
        /// card cannot Clear rather than reading nothing at all.
        /// </summary>
        public IReadOnlyList<ClearOfferViewModel> ClearOffers { get; }

        /// <summary>
        /// The Clear the player has opened, or <c>null</c>. It is presentation state and no command
        /// has been submitted while it stands: the card is still unresolved, the authoritative action
        /// choice is unchanged, and cancelling returns to exactly that.
        /// </summary>
        public ClearSelectionViewModel ClearSelection { get; }

        /// <summary>
        /// Milestone 7.8a: the decision a paused multi-step system transition is waiting on, or
        /// <c>null</c> when none is paused. It shares the one contextual region with every other
        /// decision, because the match asks exactly one thing at a time.
        /// </summary>
        public SystemTransitionPromptViewModel SystemTransitionPrompt { get; }

        /// <summary>
        /// Milestone 8.20d: the standing effect choice, or <c>null</c> when none is pending. It is
        /// what the contextual region reads to head this decision's own section, rather than adding
        /// its controls to the row of actions the player is not being asked for.
        /// </summary>
        public EffectChoicePromptViewModel EffectChoicePrompt { get; }

        /// <summary>
        /// Milestone 8.21: the standing removal-prevention decision, or <c>null</c> when none is
        /// pending. It is the whole of the contextual region while it stands, for the reason a paused
        /// system transition is: the authority refuses every other command until it is answered.
        /// </summary>
        public RemovalPreventionPromptViewModel RemovalPreventionPrompt { get; }

        public string HudHeader { get; }
    }

    /// <summary>
    /// One option of a paused system transition, labelled from the projected description and carrying
    /// the exact invocation the continuation already built.
    ///
    /// Milestone 7.8a. The label is composed here from typed facts - card instances, definition
    /// references, a printed icon, a space - and never by decoding the option identity: this type has no
    /// access to one beyond the command it forwards unchanged.
    /// </summary>
    public sealed class SystemTransitionOptionViewModel
    {
        public SystemTransitionOptionViewModel(
            string label,
            string detail,
            DirectorCommand command,
            CardColor color = CardColor.None,
            CardInstanceId sourceCardInstanceId = default,
            TrayCardViewModel card = null,
            string returnedCardName = null,
            PrintedValueChannel itemIcon = PrintedValueChannel.None,
            string setAsideCardName = null)
        {
            SourceCardInstanceId = sourceCardInstanceId;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A transition option requires a label.", nameof(label))
                : label;
            Detail = detail ?? string.Empty;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Color = color;
            ColorName = color == CardColor.None ? string.Empty : MatchHudTheme.ColorName(color);
            Card = card;
            ReturnedCardName = returnedCardName ?? string.Empty;
            ItemIcon = itemIcon;
            SetAsideCardName = setAsideCardName ?? string.Empty;
            ItemLabel = itemIcon == PrintedValueChannel.None
                ? string.Empty
                : $"{MatchHudTheme.ChannelToken(itemIcon)} {MatchHudTheme.Signed(1)}";
        }

        public string Label { get; }

        /// <summary>What else taking this option does - the sibling set aside, and the item it becomes.</summary>
        public string Detail { get; }

        public DirectorCommand Command { get; }

        /// <summary>
        /// The printed colour of the card this option is about, or <see cref="CardColor.None"/> for an
        /// option that is not about a card at all - p.8's Place names a space.
        ///
        /// For p.15's upgrade it is the upgraded card's own projected colour, which is also the taken
        /// card's: the page's own rule is that the two cards found in the deck "have the same color as
        /// the chosen card". So the control can wear that colour the way the tray's cards do, and a
        /// player matches the option to the card in their hand by the same swatch rather than by
        /// reading two names. It is paired with <see cref="ColorName"/> wherever it is drawn.
        /// </summary>
        public CardColor Color { get; }

        public string ColorName { get; }

        /// <summary>
        /// The card this option is one of the answers for, or undefined when the decision is not about a
        /// card. p.15 asks two questions in one option - which card to upgrade, and which of the two
        /// next-Tier cards to take - so several consecutive options share one upgraded card.
        ///
        /// It exists so a surface can group what the projection has already grouped, by breaking a line
        /// where this changes. That is a line break rather than a sort: the options stay in the order the
        /// pause offered them, which is the rule presentation must not bend.
        /// </summary>
        public CardInstanceId SourceCardInstanceId { get; }

        /// <summary>
        /// The card taking this option puts in the hero's hand, as the tray draws a card, or null for an
        /// option that is not about a card at all - p.8's Place names a space.
        ///
        /// <para>Milestone 8.8a. p.15's decision is a choice between two cards, and every fact a player
        /// needs to make it - Initiative, the printed values, the action and its qualifier, p.9's Range
        /// or Radius icon, and the exact words the face prints - is on the card rather than in a name.
        /// It is built by the same reading the tray builds a card with, so a candidate and the card that
        /// arrives in hand cannot be drawn as two different cards.</para>
        /// </summary>
        public TrayCardViewModel Card { get; }

        /// <summary>
        /// p.15's "Return this card to your deck": the display name of the card the hero gives up. Empty
        /// when the option is not an upgrade.
        /// </summary>
        public string ReturnedCardName { get; }

        /// <summary>
        /// p.16's icon on the card p.15 sets aside - the item this option gains - or
        /// <see cref="PrintedValueChannel.None"/> when nothing is set aside or no dashboard slot accepts
        /// its icon.
        ///
        /// <para>It is the <em>other</em> candidate's icon, which is why it belongs on the option rather
        /// than on the card face: a player choosing between two cards of one colour is also choosing
        /// which of the two items they gain, and the item they gain is printed on the card they do not
        /// take.</para>
        /// </summary>
        public PrintedValueChannel ItemIcon { get; }

        /// <summary>`ATK +1` - the gained item as the rails already draw an item bonus, or empty.</summary>
        public string ItemLabel { get; }

        /// <summary>The display name of the card p.15 sets aside, or empty when none is.</summary>
        public string SetAsideCardName { get; }
    }

    /// <summary>
    /// The decision a paused multi-step system transition is waiting on, as a surface.
    ///
    /// Milestone 7.8a builds this because no view drew one: rulebook p.8's Lane-Push respawn Place has
    /// been reachable since milestone 6.11 and p.15's Upgrade a Card since milestone 7.6, and the
    /// configured scene could only pass them through a director command that named an option without
    /// being able to say what it was.
    ///
    /// The options are the projection's own, in the projection's order. This adds none, removes none,
    /// reorders none, and chooses none.
    /// </summary>
    public sealed class SystemTransitionPromptViewModel
    {
        public SystemTransitionPromptViewModel(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind kind,
            string sectionLabel,
            string promptLabel,
            string statusLabel,
            IEnumerable<SystemTransitionOptionViewModel> options)
        {
            Kind = kind;
            SectionLabel = sectionLabel ?? throw new ArgumentNullException(nameof(sectionLabel));
            PromptLabel = promptLabel ?? throw new ArgumentNullException(nameof(promptLabel));
            StatusLabel = statusLabel ?? string.Empty;
            Options = Array.AsReadOnly((options ?? throw new ArgumentNullException(nameof(options))).ToArray());
            if (Options.Count == 0)
            {
                throw new ArgumentException(
                    "A paused step offers at least one option, so a prompt for one is never empty.",
                    nameof(options));
            }
        }

        public GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind Kind { get; }

        /// <summary>The family the shared contextual gutter names - `LEVEL UP` or `LANE PUSH`.</summary>
        public string SectionLabel { get; }

        /// <summary>Who is deciding and what they are deciding.</summary>
        public string PromptLabel { get; }

        /// <summary>What the decision has already cost and what is left, for a purchase that has one.</summary>
        public string StatusLabel { get; }

        public IReadOnlyList<SystemTransitionOptionViewModel> Options { get; }
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

    public sealed class RoundActionPresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly IRoundActionView view;
        // Milestone 8.35x: NOT readonly. A networked client is composed before its seat is granted,
        // so this identity is bound a second time by `ConfiguredMatchSceneRuntime.BindHomeHero`
        // once the lobby admits the player. See `ISeatScopedPresenter` for what an unrepaired
        // identity does to every predicate below that asks "is this decision mine?".
        private HeroId homeHeroId;
        private readonly List<TokenInstanceId> clearSelection = new List<TokenInstanceId>();
        private IMatchReadModel lastProjection;
        private RoundActionViewModel lastViewModel;
        private IMatchReadModel clearModeProjection;
        private ActionDefinitionId clearModeActionDefinitionId;


        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor every existing fixture and journey uses,
        /// composing the Director's own client. A seat-scoped surface passes its client instead.
        /// </summary>
        public RoundActionPresenter(IMatchSession session, IRoundActionView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public RoundActionPresenter(IMatchClient session, IRoundActionView view)
            : this(session, view, default)
        {
        }

        /// <summary>
        /// A seat-scoped composition names the hero it is drawn for, on the terms
        /// <see cref="DirectorHandsPresenter"/> already takes one: every other fact this presenter
        /// reads is scoped to the viewer by the projection itself, and the viewer's own commitment is
        /// the one wait that is public and therefore has to be named. The Director passes the
        /// undefined hero and answers for everybody.
        /// </summary>
        public RoundActionPresenter(IMatchClient session, IRoundActionView view, HeroId homeHeroId)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.homeHeroId = homeHeroId;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => homeHeroId;

        /// <summary>
        /// Milestone 8.35x. Binds this panel's viewer identity once the interactive lobby admits the
        /// seat, exactly as <see cref="DirectorHandsPresenter.SetHomeHero"/> has always done for the
        /// tray. Until this existed the field was <c>readonly</c> and a networked client's round
        /// panel kept the undefined hero for the whole session, reading every seat's turn release,
        /// every commitment wait and every END TURN as its own.
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
            // The cached model was composed against the OLD identity, and the projection reference
            // does not have to change for the binding to - so without this the next Refresh could
            // serve back the very view model this binding exists to correct.
            lastProjection = null;
            Refresh();
            return true;
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;
        public event Action<FastTravelOfferViewModel> FastTravelSelectionRequested;

        /// <summary>
        /// Milestone 6.3a: the Clear selection as it now stands, or <c>null</c> once it is closed.
        /// The board listens so the chosen pieces keep their cue; nothing authoritative rides on it.
        /// </summary>
        public event Action<ClearSelectionViewModel> ClearSelectionChanged;

        public void Refresh()
        {
            // Milestone 8.35x: an END TURN press that ran out of things it could submit while the
            // client was waiting on the host picks up here, on the projection that answered it.
            ResumeEndTurnComposition();
            view.Render(GetOrCreateViewModel(session.Projection));
        }

        /// <summary>
        /// Milestone 8.35f. Steps this seat's own turn back by one accepted command.
        /// </summary>
        /// <remarks>
        /// It takes no invocation and composes no command, because there is none: the control's
        /// whole content is "the seat that owns this turn asks to step back". The presenter refuses
        /// to ask when the view it is currently rendering says the control does not stand, so a
        /// stale click cannot become a question the host has to refuse; the host re-decides anyway.
        /// </remarks>
        public bool SubmitSeatUndo()
        {
            if (!GetOrCreateViewModel(session.Projection).SeatUndoAvailable)
            {
                return false;
            }

            var result = session.SubmitUndo();
            if (result.Accepted)
            {
                Refresh();
                SessionProjectionChanged?.Invoke(session.Projection);
            }

            return result.Accepted;
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
                current.HeroRespawnOptions.Any(value => value.Command.Equals(invocation.Command)) ||
                // Milestone 7.8a: a paused transition's options join the same legality decision as
                // everything else this presenter projects. An option from an older prompt - one the
                // pause has since moved past, or one a restore replaced - is refused here and never
                // reaches the authority, which would refuse it too and for the same reason.
                current.SystemTransitionPrompt?.Options
                    .Any(value => value.Command.Equals(invocation.Command)) == true ||
                // Milestone 8.21: the standing removal-prevention decision's own controls. Every one
                // of them is a command the pause minted and this presenter projected, so they belong
                // in the same legality decision as every other control here - and until they were in
                // it, this method refused each of them, which drew a player a prompt whose faces did
                // nothing when clicked. It was invisible to the projection and presenter tiers alike,
                // because both compose the controls correctly; only a pointer on the live panel finds
                // a control that forwards a command the presenter then declines to execute.
                (current.RemovalPreventionPrompt != null &&
                    (current.RemovalPreventionPrompt.Payments
                            .Any(value => value.Command.Equals(invocation.Command)) ||
                        current.RemovalPreventionPrompt.Decline.Command.Equals(
                            invocation.Command))) ||
                // Milestone 6.3a: the open Clear's confirm joins the same legality decision, because
                // this presenter refuses anything it did not project and a Clear confirm is projected
                // like every other control here. What it refuses in practice is a confirm left over
                // from a selection the current projection no longer offers.
                current.ClearSelection != null &&
                    current.ClearSelection.Confirm.Command.Equals(invocation.Command);
            if (!legal)
            {
                return false;
            }

            // Milestone 8.35v (the THIRD amendment's REVISED SEMANTICS): one END TURN press ends
            // the turn regardless of how many accepted commands that takes. `invocation` is END
            // TURN's own exactly when it is the same object this presenter's own view model just
            // drew - never a coincidental match on some other control that happens to submit the
            // same command kind - so a movement Stop, an effect choice's own stop, or a Hold
            // submitted through here is the completion half of the press; the release below is the
            // second half, performed in the same press rather than left for a separate one. Release
            // itself needs no further chaining - reaching this branch already IS the whole press.
            var isEndTurnPress = ReferenceEquals(invocation, current.EndTurnInvocation);
            // Milestone 8.35x: read BEFORE the submission, because a network client hands back a
            // projection that has not yet been decided and may already be stripped of the actor.
            var pressActorId = isEndTurnPress ? PressActorOf(session.Projection) : default;
            if (!Execute(invocation.Command))
            {
                return false;
            }

            if (isEndTurnPress && invocation.Command.Kind != DirectorCommandKind.ReleaseTurn)
            {
                ReleaseTheTurnIfItNowStandsReleasable();
                // The same asynchronous-client fault this press path has always had: over the wire
                // the release above finds no standing contract yet, because the completion it
                // follows has only been SENT. Arm the continuation so the release lands on the
                // projection that answers it rather than waiting for a second press.
                ArmOrClearEndTurnContinuation(pressActorId, isFreshPress: true);
            }

            return true;
        }

        /// <summary>
        /// Milestone 8.35v. The end-turn press's own composition, reachable from every state the
        /// omnipresent control draws enabled: a direct completion via <see cref="Execute"/> below;
        /// the seat's one open, uncommitted Attack-target selection cancelled by its own bounded
        /// rewind (milestone 8.35f) first; or an open Clear selection (milestone 6.3a) cancelled by
        /// <see cref="CancelClear"/> first - purely a presentation reset, since nothing is submitted
        /// to the authority until Clear is confirmed, so there is nothing to rewind. Clear is
        /// checked first of the three: it is presentation-only and the authority has not paused
        /// underneath it, so <see cref="CreateEndTurnInvocation"/> can already project an ordinary
        /// completion (p.12's Hold, most commonly) alongside it, and submitting that FIRST would
        /// leave a confirmed Hold sitting beside a still-open, now-orphaned board selection - the
        /// discard has to land before the completion it would otherwise race. All three paths
        /// converge on the same one-press rule: keep composing whatever the projection legally
        /// offers next until the turn is released, disabled would refuse it, or nothing more is
        /// offered - never a fabricated stop the rules do not.
        ///
        /// <para>Bounded to the ONE actor this press is for. A Director client "answers for
        /// everybody" - unlike a seat, which sees only its own hero - so the instant this press's
        /// own release lands, the very next actor's own untouched card can already project its OWN
        /// Hold; composing past that boundary would silently spend a second player's turn inside a
        /// press that was only ever about the first. `pressActorId` is read once, before any
        /// submission, and every loop iteration verifies the state still concerns that same hero -
        /// either still resolving it, or still awaiting ITS release - before composing anything
        /// else. `Submit`'s own release-chain is not reused here for exactly this reason: it cannot
        /// tell "this call's own actor" from "whoever is offered next", which is what let 8.35v's
        /// first draft submit a second player's Hold inside a press the seeded suite proved was
        /// only ever about the first.</para>
        /// </summary>
        public bool SubmitEndTurn()
        {
            var pressActorId = PressActorOf(session.Projection);
            var progressed = ComposeEndTurn(pressActorId);
            ArmOrClearEndTurnContinuation(pressActorId, isFreshPress: true);
            return progressed;
        }

        /// <summary>
        /// The actor one END TURN press is about: the hero currently acting, or - once the turn's
        /// resolution has finished - the hero its release is standing on.
        /// </summary>
        private static HeroId PressActorOf(IMatchReadModel projection) =>
            projection?.SelectedActor.IsDefined == true
                ? projection.SelectedActor.ActorId
                : projection?.PendingTurnRelease?.ActorId ?? default;

        private bool ComposeEndTurn(HeroId pressActorId)
        {
            var progressed = false;
            for (var guard = 0; guard < MaxEndTurnComposedSteps; guard++)
            {
                var projection = session.Projection;
                var stillThisActor = pressActorId.IsDefined &&
                    (projection.SelectedActor.ActorId.Equals(pressActorId) ||
                        (projection.PendingTurnRelease?.ActorId.Equals(pressActorId) ?? false));
                if (!stillThisActor)
                {
                    return progressed;
                }

                var current = GetOrCreateViewModel(projection);
                if (current.ClearSelection != null)
                {
                    if (!CancelClear())
                    {
                        return progressed;
                    }

                    progressed = true;
                    continue;
                }

                if (current.EndTurnInvocation != null)
                {
                    if (!Execute(current.EndTurnInvocation.Command))
                    {
                        return progressed;
                    }

                    progressed = true;
                    continue;
                }

                if (!current.EndTurnCancelsOpenAttack)
                {
                    return progressed;
                }

                if (!SubmitSeatUndo())
                {
                    return progressed;
                }

                progressed = true;
            }

            // Never spin: a state that keeps offering a cancel without ever reaching a direct
            // completion is a defect this composition should surface loudly rather than loop on.
            throw new InvalidOperationException(
                "End Turn's cancel-and-release composition did not reach a legal completion " +
                "within its bound.");
        }

        /// <summary>The bound in <see cref="SubmitEndTurn"/>: one rewind past the deepest observed
        /// case (an open Attack target choice) plus the completion and release it composes into,
        /// with headroom for a repeat-ability's own extra cycle.</summary>
        private const int MaxEndTurnComposedSteps = 8;

        /// <summary>
        /// Milestone 8.35x, and the SECOND defect the first live internet playtest found: "first end
        /// turn clears the card, second ends the turn".
        ///
        /// <para><b>What 8.35v's one-press rule assumed, and why only the network broke it.</b>
        /// <see cref="ComposeEndTurn"/> re-reads <c>session.Projection</c> after every step and keeps
        /// composing until the turn is released. That is exact against a SYNCHRONOUS client - the
        /// Director's and a local seat's - where a submission has already been decided by the time
        /// <see cref="Execute"/> returns. Over the wire it is not: <c>NetworkMatchClient.Submit</c>
        /// returns as soon as the command is SENT, handing back the pre-command projection, and the
        /// session then refuses every further submission until the host answers. So the second step
        /// of the composition - the release after a Hold, a movement Stop, an effect Decline - was
        /// refused, the loop ended having spent exactly one command, and the player had to press
        /// again. Every existing one-press case passes because every one of them drives a
        /// synchronous client; nothing in the suite ever composed over the client the shipped build
        /// actually uses.</para>
        ///
        /// <para><b>The fix, and why it lives here.</b> A press that could not finish because the
        /// client is mid-round-trip is not a finished press - it is a press with more to compose as
        /// soon as there is something to compose against. The continuation is remembered here, in
        /// the one place that knows what END TURN means, and resumed from <see cref="Refresh"/>,
        /// which every projection push already calls. <c>NetworkMatchClient</c> carries a
        /// hand-written continuation of exactly this shape for the effect select-then-confirm pair
        /// (<c>PendingEffectAutoConfirm</c>); that one was built before 8.35v's composition and
        /// 8.35g's release chain existed, and neither got an equivalent until now.</para>
        ///
        /// <para><b>What keeps it from spending somebody else's turn.</b> The continuation is bound
        /// to the ONE actor the press was for and is dropped the moment the state stops concerning
        /// that hero - the identical bound <see cref="ComposeEndTurn"/> already enforces inside a
        /// single press, and for the identical reason 8.35v records: a Director client answers for
        /// everybody, so composing past the actor boundary would silently end a second player's
        /// turn. It is also dropped after <see cref="MaxEndTurnResumes"/> resumptions, so a state
        /// that never becomes composable cannot leave a press armed for the rest of the match.</para>
        /// </summary>
        private HeroId composingEndTurnFor;

        private int composingEndTurnResumes;

        /// <summary>
        /// The bound on how many projection pushes one press may be resumed across. Generous enough
        /// for the deepest composition (rewind, completion, release) to survive a round trip each,
        /// and finite so a press can never become permanent.
        /// </summary>
        private const int MaxEndTurnResumes = 12;

        /// <summary>
        /// Remembers an END TURN press that has more to compose, or forgets one that is finished.
        ///
        /// <para>The signal that a press is unfinished rather than complete is the surface's own
        /// input gate: a client waiting on the host reports <c>IsInputEnabled == false</c>, which is
        /// precisely "this press could not continue for a reason that will pass". A synchronous
        /// client never reports it, so a local press - which always finishes inside its own loop -
        /// never arms a continuation at all.</para>
        /// </summary>
        private void ArmOrClearEndTurnContinuation(HeroId pressActorId, bool isFreshPress)
        {
            // Milestone 8.35z4. The budget belongs to the PRESS, not to each resumption of it.
            // Resetting it unconditionally here made MaxEndTurnResumes unreachable: every resume
            // re-arms through this method, so the counter returned to zero before it could ever be
            // compared, and a press whose submission kept being refused resumed forever. The
            // six-client rehearsal found it - one seat spent 17,486 refused submissions at a single
            // authoritative version while the match stood still. No offline probe could reach it,
            // because against an in-memory wire the composition always completed within two frames
            // and disarmed at the actor boundary long before the bound could matter.
            if (isFreshPress)
            {
                composingEndTurnResumes = 0;
            }

            var projection = session.Projection;

            // Deliberately NOT conditioned on whether this press submitted anything. An impatient
            // second press, landing while the first one's command is still in flight, submits
            // nothing and must leave the first press's continuation ARMED - clearing it there would
            // punish the player for pressing twice by making the turn need a third press, which is
            // the very failure this whole mechanism exists to remove. What decides is the state: a
            // disabled input gate means the press has somewhere left to go once the host answers.
            composingEndTurnFor =
                pressActorId.IsDefined && projection != null &&
                !projection.IsInputEnabled &&
                StillConcerns(projection, pressActorId)
                    ? pressActorId
                    : default;
        }

        /// <summary>
        /// Continues an armed press once the host's answer has landed. Called from
        /// <see cref="Refresh"/>, which every projection push already reaches.
        /// </summary>
        private void ResumeEndTurnComposition()
        {
            var actor = composingEndTurnFor;
            if (!actor.IsDefined)
            {
                return;
            }

            var projection = session.Projection;
            if (projection == null || !projection.IsInputEnabled)
            {
                // Still waiting on the host. Nothing has changed that this press can act on.
                return;
            }

            if (++composingEndTurnResumes > MaxEndTurnResumes ||
                !StillConcerns(projection, actor))
            {
                composingEndTurnFor = default;
                return;
            }

            // Cleared BEFORE composing, so the refreshes this composition itself provokes find no
            // armed press and cannot recurse into it. The step below re-arms if there is still more.
            composingEndTurnFor = default;
            ComposeEndTurn(actor);
            ArmOrClearEndTurnContinuation(actor, isFreshPress: false);
        }

        /// <summary>
        /// Whether the state still concerns the hero a press was for - still resolving that hero's
        /// turn, or still awaiting ITS release. The press's own boundary, asked in one place.
        /// </summary>
        private static bool StillConcerns(IMatchReadModel projection, HeroId actorId) =>
            projection.SelectedActor.ActorId.Equals(actorId) ||
            (projection.PendingTurnRelease?.ActorId.Equals(actorId) ?? false);

        private void ReleaseTheTurnIfItNowStandsReleasable()
        {
            var projection = session.Projection;
            if (projection?.DecisionContract != null &&
                projection.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
            {
                Execute(DirectorCommand.ReleaseTurn());
            }
        }

        public bool SubmitAgreement(TeamAgreementChoiceViewModel choice)
        {
            if (choice == null)
                return false;
            var current = GetOrCreateViewModel(session.Projection);
            if (current.TeamAgreement?.Choices.Any(value =>
                    value.Command.Equals(choice.Command)) != true)
                return false;

            var result = session.SubmitAgreement(choice.Command);
            if (result.Accepted)
                SessionProjectionChanged?.Invoke(result.Projection);
            else
                Refresh();
            return result.Accepted;
        }

        /// <summary>
        /// Milestone 8.11a, at the user's request. Answers a Hero Respawn by clicking the space rather
        /// than a control naming it. Declining to respawn has no board surface, so it stays a control.
        /// </summary>
        public bool SubmitRespawnAtDestination(HexCoordinate destination)
        {
            var current = GetOrCreateViewModel(session.Projection);
            var option = current?.HeroRespawnOptions
                .SingleOrDefault(value => value.Destination.Equals(destination));
            return option != null && Execute(option.Command);
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
            var result = session.Submit(command);
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

            var confirmed = session.Submit(confirm);
            if (confirmed.Accepted)
            {
                SessionProjectionChanged?.Invoke(confirmed.Projection);
            }

            return true;
        }

        /// <summary>
        /// Milestone 6.3a: opens the Clear selection for one projected offer. Nothing is submitted and
        /// no authoritative state moves - the card stays unresolved and the action choice stands - so
        /// this returns to exactly where it started if the player cancels.
        /// </summary>
        public bool SelectClear(ClearOfferViewModel offer)
        {
            if (offer == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var legal = current.ClearOffers.SingleOrDefault(value =>
                value.ReplacedAttackActionDefinitionId.Equals(
                    offer.ReplacedAttackActionDefinitionId) &&
                value.SourceCardInstanceId.Equals(offer.SourceCardInstanceId) &&
                value.ActorId.Equals(offer.ActorId));
            // Milestone 8.8b: a Clear the authority stamped unavailable opens nothing. Refusing the
            // mode is what keeps the surface from standing in a selection whose only confirm the
            // authority would reject - the same reason a greyed action carries no start.
            if (legal == null || !legal.IsAvailable)
            {
                return false;
            }

            clearModeProjection = session.Projection;
            clearModeActionDefinitionId = legal.ReplacedAttackActionDefinitionId;
            clearSelection.Clear();
            return AfterClearSelectionChanged();
        }

        /// <summary>
        /// Adds or removes one offered token from the subset. The presenter tracks nothing else about
        /// it: which tokens may be chosen, and which command each subset means, are both projected.
        /// </summary>
        public bool ToggleClearToken(ClearTokenCandidateViewModel candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            if (current.ClearSelection == null ||
                !current.ClearSelection.Candidates.Any(value =>
                    value.TokenId.Equals(candidate.TokenId)))
            {
                return false;
            }

            if (!clearSelection.Remove(candidate.TokenId))
            {
                clearSelection.Add(candidate.TokenId);
            }

            return AfterClearSelectionChanged();
        }

        /// <summary>
        /// Toggles the offered token standing on this space, for the board click. Milestone 8.15a, at
        /// the user's direction: a Clear candidate is a piece on the map, so the map is where it is
        /// chosen - the same board-first rule every other standing piece already follows. A space
        /// carrying no offered token toggles nothing, so the click falls through to whatever else the
        /// board may mean by it.
        /// </summary>
        public bool ToggleClearTokenAtSpace(HexCoordinate space)
        {
            var current = GetOrCreateViewModel(session.Projection);
            var candidate = current.ClearSelection?.Candidates
                .FirstOrDefault(value => value.Space.Equals(space));
            return candidate != null && ToggleClearToken(candidate);
        }

        /// <summary>Forwards the projected invocation for exactly the chosen subset, unchanged.</summary>
        public bool ConfirmClear()
        {
            var current = GetOrCreateViewModel(session.Projection);
            return current.ClearSelection != null && Submit(current.ClearSelection.Confirm);
        }

        /// <summary>
        /// Closes the selection without submitting anything. The card is left unresolved and the
        /// authoritative action choice is the one it always was.
        /// </summary>
        public bool CancelClear()
        {
            if (GetOrCreateViewModel(session.Projection).ClearSelection == null)
            {
                return false;
            }

            clearModeProjection = null;
            clearModeActionDefinitionId = default;
            clearSelection.Clear();
            return AfterClearSelectionChanged();
        }

        private bool AfterClearSelectionChanged()
        {
            lastProjection = null;
            ClearSelectionChanged?.Invoke(
                GetOrCreateViewModel(session.Projection).ClearSelection);
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

        /// <summary>
        /// The Director-shaped overload, which keeps its own manual end-of-round control. Milestone
        /// 8.35d added the parameter below rather than changing this signature, so every existing
        /// caller still describes the surface it always described.
        /// </summary>
        public static RoundActionViewModel CreateViewModel(IMatchReadModel projection) =>
            CreateViewModel(projection, default, Array.Empty<TokenInstanceId>(), true, default);

        public static RoundActionViewModel CreateViewModel(
            IMatchReadModel projection, bool offersRoundContinuation) =>
            CreateViewModel(
                projection, default, Array.Empty<TokenInstanceId>(), offersRoundContinuation, default);

        /// <summary>
        /// Milestone 8.35v. The seat-shaped overload: every fact below this presenter's
        /// per-turn-actor gate now needs the exact viewer <see cref="IMatchReadModel.SelectedActor"/>
        /// is compared against - <see cref="IsViewerTheActingHero"/>'s own remarks name the trap a
        /// caller passing the Director's default sentinel falls into against a seat-scoped
        /// projection: every seat's own <c>SelectedActor</c> and <c>IsTurnActive</c> are unscoped
        /// public facts, so only the homeHeroId comparison tells "the acting hero" apart from "a
        /// seat that merely owns some standing decision the acting hero's turn produced" or a
        /// plain spectator. Test and tooling code composing a seat's own view without a full
        /// <see cref="RoundActionPresenter"/> instance should call this rather than the Director-
        /// shaped overload above.
        /// </summary>
        public static RoundActionViewModel CreateViewModel(
            IMatchReadModel projection, HeroId homeHeroId) =>
            CreateViewModel(
                projection, default, Array.Empty<TokenInstanceId>(), true, homeHeroId);

        /// <summary>
        /// Whether the match is waiting on a decision inside this turn, asked in one place.
        ///
        /// A paused Skill frame counts here for the same reason paused Movement and Attack do: Core
        /// refuses a second action while one is in progress, so an offer shown during the pause is a
        /// control that can only produce a rejection.
        ///
        /// Milestone 7.9c, finding F-PRES-01. Two lists asked this, and they had drifted by one entry:
        /// the End Turn derivation's copy omitted <c>PendingHeroRespawnChoice</c>, so a defeated hero's
        /// mandatory respawn answer was the one decision beside which the HUD still drew END TURN - a
        /// control the turn authority refuses with <c>HeroRespawnChoicePending</c> every time. Adding the
        /// missing entry to the second list would have left the next pending kind free to drift the same
        /// way, so there is now one list and no second place to forget.
        ///
        /// <para>Milestone 8.20d separates the two things that list had always held together, and
        /// reads nothing of its own so neither half can be bypassed. A <b>question</b> is a family the
        /// match is waiting for an answer to, and every one of them is named by
        /// <see cref="FormatStatus"/>; <c>StatusDerivationConformanceTests</c> compares the two by
        /// what the compiled code reads, so a family added to the question list without a status
        /// branch fails there rather than reaching a player as "choose an action or End Turn". A
        /// <b>frame</b> is a resolution in progress, which asks nothing itself - its question, when it
        /// has one, is one of the families above it.</para>
        /// </summary>
        private static bool HasPendingDecision(IMatchReadModel projection) =>
            IsAwaitingAnAnsweredDecision(projection) || IsInsideAnUnfinishedResolution(projection);

        /// <summary>
        /// Every pending family the match is waiting for a player's answer to. Milestone 8.20d: the
        /// set <see cref="FormatStatus"/> must name, one entry per family, checked as compiled reads
        /// rather than as a written-down list a later milestone could forget to extend.
        /// </summary>
        private static bool IsAwaitingAnAnsweredDecision(IMatchReadModel projection) =>
            projection.PendingInitiativeChoice != null ||
            projection.PendingDefenseChoice != null ||
            projection.PendingEffectChoice != null ||
            projection.PendingHeroRespawnChoice != null ||
            projection.PendingAttackTargetChoice != null ||
            projection.PendingMovementChoice != null ||
            projection.PendingDisplacedMinionCorrection != null ||
            projection.PendingRemovalPrevention != null;

        /// <summary>
        /// A resolution that has started and not finished. It suppresses the offers a second action
        /// would produce, exactly as a question does - Core refuses a second action while one is in
        /// progress - but it is not itself something a player answers, so it carries no status words
        /// of its own.
        /// </summary>
        private static bool IsInsideAnUnfinishedResolution(IMatchReadModel projection) =>
            projection.HasPendingMovementResolution ||
            projection.HasPendingAttackResolution ||
            projection.HasPendingSkillResolution;

        private static RoundActionViewModel CreateViewModel(
            IMatchReadModel projection,
            ActionDefinitionId openClearActionDefinitionId,
            IReadOnlyList<TokenInstanceId> selectedClearTokenIds,
            bool offersRoundContinuation,
            HeroId homeHeroId)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var hasPendingResolution = HasPendingDecision(projection);
            var offered = projection.IsTurnActive && !hasPendingResolution
                ? projection.AvailableTurnActions
                : Array.Empty<ActionOffer>();
            var actions = offered
                .Where(offer => offer.IsAvailable)
                .Select(offer => new TypedInvocationViewModel(
                    FormatActionLabel(projection, offer),
                    DirectorCommand.StartAction(
                        offer.SourceCardInstanceId, offer.ActionDefinitionId)))
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
            var initiativeChoices = projection.PendingInitiativeChoice == null ||
                projection.TeamAgreement?.Kind == TeamAgreementKind.Initiative
                ? Array.Empty<TypedInvocationViewModel>()
                : projection.PendingInitiativeChoice.CandidateCardInstanceIds
                    .Select(cardId => new TypedInvocationViewModel(
                        $"ACT WITH {GetCardDisplayName(projection, cardId)}",
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Initiative,
                            command => command.Kind == DirectorCommandKind.ChooseTiedActor &&
                                command.TeamSide == projection.PendingInitiativeChoice.OwningSide &&
                                command.CardInstanceId.Equals(cardId))))
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
            // Milestone 6.3a: a Clear is offered wherever the projection carries one, and the open
            // selection is drawn from that same option, so a control and the command behind it can
            // never come from different readings of the position.
            var clearOptions = hasPendingResolution || !projection.IsTurnActive
                ? Array.Empty<ClearOptionProjection>()
                : projection.AvailableClearOptions.ToArray();
            var clearOffers = clearOptions
                .Select(option => new ClearOfferViewModel(
                    ClearLabels.OfferLabel,
                    option.ReplacedAttackActionDefinitionId,
                    option.ReplacementOffer.SourceCardInstanceId,
                    option.ReplacementOffer.ActorId,
                    option.AdjacentTokens.Count,
                    // Milestone 8.8b: the authority's own stamp, never a count made here.
                    option.ReplacementOffer.IsAvailable))
                .ToArray();
            var printedActionRegions = CreatePrintedActionRegions(
                projection, offered, actions, fastTravelOffers, clearOffers);
            var clearSelection = CreateClearSelection(
                clearOptions, openClearActionDefinitionId, selectedClearTokenIds);
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
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.HeroRespawn,
                            command => command.Kind == DirectorCommandKind.RespawnHero &&
                                command.HeroId.Equals(respawn.RespawnInvocations[index].HeroId) &&
                                command.CardInstanceId.Equals(
                                    respawn.RespawnInvocations[index].CardInstanceId) &&
                                command.Coordinate.Equals(destination.Coordinate))))
                    .ToArray();

            var status = FormatStatus(projection, homeHeroId, out var statusDetail);
            var endTurnInvocation = CreateEndTurnInvocation(projection);
            var endTurnCancelsOpenAttack = ResolveEndTurnCancelsOpenAttack(
                projection, homeHeroId, endTurnInvocation);
            var endTurnUnavailableReason = endTurnInvocation != null || endTurnCancelsOpenAttack
                ? null
                : ResolveEndTurnUnavailableReason(projection, homeHeroId);
            return new RoundActionViewModel(
                projection.Version,
                projection.RoundIndex,
                projection.TurnIndex,
                projection.Phase,
                status,
                actions,
                endTurnInvocation,
                // A pending removal choice is mandatory: the authority rejects an advance while it
                // stands, so the control that would produce that rejection is not offered. Milestone
                // 7.8a adds the second such pause on the same terms - a paused system transition is
                // answered by answering it, and an advance offered beside it could only be refused.
                //
                // Milestone 8.35d adds the first clause, and it is why the control was drawn for a
                // seat that could never submit it: the two pauses below are read from the seat's own
                // scoped surface, which reports the standing decision it does NOT own as absent, so
                // five of six seats saw a CONTINUE control beside somebody else's question and
                // pressing it produced an unauthorized refusal. A seat-served round is now continued
                // by its host; only the Director's direct surface still steps it by hand.
                offersRoundContinuation &&
                    projection.IsInputEnabled &&
                    projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.PendingMinionBattleChoice == null &&
                    projection.PendingSystemTransitionStep == null
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
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.HeroRespawn,
                            command => command.Kind ==
                                    DirectorCommandKind.ResolveCardWithoutRespawn &&
                                command.HeroId.Equals(respawn.DeclineInvocation.HeroId) &&
                                command.CardInstanceId.Equals(
                                    respawn.DeclineInvocation.CardInstanceId))),
                unavailableEffectChoices,
                unavailableActions,
                clearOffers,
                clearSelection,
                CreateSystemTransitionPrompt(projection),
                // Milestone 8.20d: read from the same pause the controls above were built from, so
                // the heading names the decision those controls answer and nothing else.
                projection.PendingEffectChoice == null
                    ? null
                    : new EffectChoicePromptViewModel(EffectChoicePromptLabels.SubjectOf(
                        projection.PendingEffectChoice, projection.PendingEffectChoicePreview)),
                CreateRemovalPreventionPrompt(projection),
                printedActionRegions,
                TeamAgreementHudPresentation.Create(projection),
                // Milestone 8.35f. Read straight off the projection: the host publishes whether
                // this seat may step back, and the control is enabled exactly when it says so.
                projection.IsSeatUndoAvailable,
                AwaitedInputQuery.IsWaitingOnViewer(projection, homeHeroId),
                statusDetail,
                endTurnCancelsOpenAttack,
                endTurnUnavailableReason);
        }

        private static IReadOnlyList<PrintedActionRegionViewModel> CreatePrintedActionRegions(
            IMatchReadModel projection,
            IReadOnlyList<ActionOffer> offered,
            IReadOnlyList<TypedInvocationViewModel> actions,
            IReadOnlyList<FastTravelOfferViewModel> fastTravelOffers,
            IReadOnlyList<ClearOfferViewModel> clearOffers)
        {
            var regions = new List<PrintedActionRegionViewModel>(offered.Count);
            for (var index = 0; index < offered.Count; index++)
            {
                var offer = offered[index];
                var definition = projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference));
                var action = definition.ActionDefinitions.Single(value =>
                    value.Id.Equals(offer.ActionDefinitionId));
                var secondaries = offered.Where(value =>
                    value.SourceCardInstanceId.Equals(offer.SourceCardInstanceId) &&
                    value.InvocationContext != ActionInvocationContext.PrintedPrimary).ToArray();
                var secondaryIndex = offer.InvocationContext == ActionInvocationContext.PrintedPrimary
                    ? -1
                    : Array.FindIndex(secondaries, value => value.Equals(offer));
                var invocation = actions.SingleOrDefault(value =>
                    value.Command.Kind == DirectorCommandKind.StartAction &&
                    value.Command.ActionDefinitionId.Equals(offer.ActionDefinitionId));
                var label = FormatActionLabel(projection, offer);
                regions.Add(new PrintedActionRegionViewModel(
                    offer,
                    action.Kind,
                    label,
                    secondaryIndex,
                    secondaries.Length,
                    offer.IsAvailable
                        ? $"{label} - available"
                        : $"{label} - {FormatUnavailabilityReason(projection, offer)}",
                    invocation,
                    fastTravelOffers.SingleOrDefault(value =>
                        value.ReplacedMovementActionDefinitionId.Equals(offer.ActionDefinitionId)),
                    clearOffers.SingleOrDefault(value =>
                        value.ReplacedAttackActionDefinitionId.Equals(offer.ActionDefinitionId)),
                    definition.Progression.Color));
            }

            // Keyboard order follows the face from its upper Secondary strip into its Primary band.
            return Array.AsReadOnly(regions
                .OrderBy(value => value.InvocationContext == ActionInvocationContext.PrintedPrimary ? 1 : 0)
                .ThenBy(value => value.SecondaryIndex)
                .ToArray());
        }

        /// <summary>
        /// Milestone 8.21. The standing removal-prevention decision as controls, or <c>null</c> when
        /// none is pending.
        ///
        /// <para>Each payment is drawn as the whole printed face of the card that would pay it, on the
        /// terms milestone 8.15a set for every card-valued choice and milestone 8.11b set for a Defense
        /// offer: the tray's own standalone builder, with the card's name kept as the control's hover
        /// and assistive reading. The decline carries no card, which is what keeps it a distinct
        /// control rather than another face.</para>
        ///
        /// <para>Every command here is the pause's own minted invocation, forwarded unchanged. Nothing
        /// composes an answer, so a source or a card the prompt did not project cannot be submitted
        /// from this surface at all.</para>
        /// </summary>
        private static RemovalPreventionPromptViewModel CreateRemovalPreventionPrompt(
            IMatchReadModel projection)
        {
            var prompt = projection.PendingRemovalPrevention;
            if (prompt == null)
            {
                return null;
            }

            return new RemovalPreventionPromptViewModel(
                prompt.SubjectMinionId,
                prompt.Payments.Select(payment => new TypedInvocationViewModel(
                    FindCardName(projection, payment.PaymentCardInstanceId) ??
                        payment.PaymentCardInstanceId.Value,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.RemovalPrevention,
                        command => command.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                            ReferenceEquals(command.RemovalPrevention, payment.Invocation)),
                    card: CreateCardFace(projection, payment.PaymentCardInstanceId))),
                new TypedInvocationViewModel(
                    RemovalPreventionPromptLabels.DeclineLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.RemovalPrevention,
                        command => command.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                            ReferenceEquals(command.RemovalPrevention, prompt.DeclineInvocation))));
        }

        /// <summary>
        /// Milestone 7.8a: the paused transition as a prompt, or <c>null</c> when nothing is paused or
        /// the rules that paused it describe nothing.
        ///
        /// Every option is built from the description's typed content and carries the invocation the
        /// continuation itself projected, forwarded unchanged. An undescribed pause draws no prompt
        /// rather than a prompt of option identities, because a control a player cannot read is worse
        /// than the absence the milestone is fixing.
        /// </summary>
        public static SystemTransitionPromptViewModel CreateSystemTransitionPrompt(
            IMatchReadModel projection) =>
            CreateSystemTransitionPrompt(
                projection?.PendingSystemTransitionDescription,
                projection?.Definitions,
                projection?.Setup?.Heroes,
                projection?.DecisionContract);

        public static SystemTransitionPromptViewModel CreateSystemTransitionPrompt(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription description,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<QuickGameHeroSetupState> heroes,
            PendingDecisionContract contract)
        {
            if (description == null || description.Options.Count == 0)
            {
                return null;
            }

            // Milestone 8.35u: the DESCRIPTION's own chooser, not the pause's. Rulebook p.15 can
            // hold one open item per hero, so the pause has no single side and asking it throws.
            var side = description.ChoosingSide == TeamSide.Red ? "RED" : "BLUE";
            if (description.Kind ==
                GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade)
            {
                var heroName = HeroDisplayName(heroes, description.HeroId);
                return new SystemTransitionPromptViewModel(
                    description.Kind,
                    "LEVEL UP",
                    $"{side} {heroName} REACHED LEVEL {description.Level} - UPGRADE A CARD",
                    $"PAID {description.PaidCoins} COIN{(description.PaidCoins == 1 ? string.Empty : "S")} - " +
                        $"{description.RemainingCoins} REMAINING",
                    description.Options.Select(option => new SystemTransitionOptionViewModel(
                        $"UPGRADE {DefinitionName(definitions, option.UpgradedCard)} " +
                            $"INTO {DefinitionName(definitions, option.TakenCard)}",
                        FormatUpgradeDetail(definitions, option),
                        PendingDecisionCommandBinding.Require(
                            contract,
                            PendingDecisionFamilies.SystemTransition,
                            command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                                command.SystemTransitionChoice.Equals(
                                    Invocation(description, option))),
                        DefinitionColor(definitions, option.UpgradedCard),
                        option.UpgradedCardInstanceId,
                        // Milestone 8.8a: the candidate itself, as a whole printed face. A definition the
                        // projection does not carry reads as a face that states nothing, which is what
                        // `ReadFace` already does for a tray card - one missing registration must not
                        // take a mandatory decision off the screen.
                        CurrentRoundCards.DeckCard(
                            option.TakenCardInstanceId, Definition(definitions, option.TakenCard)),
                        DefinitionName(definitions, option.UpgradedCard),
                        option.ItemIcon,
                        option.SetAsideCardInstanceId.IsDefined
                            ? DefinitionName(definitions, option.SetAsideCard)
                            : string.Empty)));
            }

            return new SystemTransitionPromptViewModel(
                description.Kind,
                "LANE PUSH",
                $"{side} PLACES THE RESPAWNING MINION",
                "ITS SPAWN POINT IS BLOCKED - CHOOSE A NEAREST EMPTY SPACE",
                description.Options.Select(option => new SystemTransitionOptionViewModel(
                    $"PLACE AT {option.DestinationSpaceId.Value}",
                    $"({option.Destination.Q}, {option.Destination.R})",
                    PendingDecisionCommandBinding.Require(
                        contract,
                        PendingDecisionFamilies.SystemTransition,
                        command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                            command.SystemTransitionChoice.Equals(
                                Invocation(description, option))))));
        }

        private static GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand Invocation(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription description,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionDescription option) =>
            description.PendingStep.Invocations.Single(candidate =>
                candidate.OptionId.Equals(option.OptionId));

        private static string FormatUpgradeDetail(
            IReadOnlyList<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionDescription option)
        {
            if (!option.SetAsideCardInstanceId.IsDefined)
            {
                return "NOTHING IS SET ASIDE - NO ITEM";
            }

            var setAside = DefinitionName(definitions, option.SetAsideCard);
            return option.ItemIcon == PrintedValueChannel.None
                ? $"SETS {setAside} ASIDE - NO MATCHING ITEM SLOT"
                : $"SETS {setAside} ASIDE AS A " +
                    $"{MatchHudTheme.ChannelToken(option.ItemIcon)} {MatchHudTheme.Signed(1)} ITEM";
        }

        /// <summary>
        /// The card's own printed colour, read from its definition exactly as the tray reads one, so a
        /// transition option and a card in hand can never disagree about what colour a card is.
        /// </summary>
        private static CardColor DefinitionColor(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference reference) =>
            (definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(reference))
                ?.Progression.Color ?? CardColor.None;

        /// <summary>
        /// The definition behind one reference, or null when this projection does not carry it - which
        /// only happens for content that was never registered. Milestone 8.8a: the upgrade prompt draws
        /// a whole card face from it, and <see cref="CurrentRoundCards.ReadFace"/> already states the
        /// absence as a face rather than faulting.
        /// </summary>
        private static CardDefinition Definition(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference reference) =>
            (definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(reference));

        private static string DefinitionName(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference reference) =>
            (definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(reference))?.DisplayName
                ?? reference.DefinitionId.Value;

        private static string HeroDisplayName(
            IReadOnlyList<QuickGameHeroSetupState> heroes, HeroId heroId) =>
            (heroes ?? Array.Empty<QuickGameHeroSetupState>())
                .FirstOrDefault(hero => hero.HeroId.Equals(heroId))?.DisplayName.ToUpperInvariant()
                ?? heroId.Value;

        /// <summary>
        /// Milestone 6.3a: the open Clear's controls, or <c>null</c> when none is open.
        ///
        /// The subset the player has chosen is matched to one projected invocation rather than being
        /// turned into a payload here - <see cref="ClearOptionProjection.FindInvocation"/> answers it,
        /// and an answer of none closes the selection instead of composing a command the projection
        /// never offered. A token the player had chosen and the projection no longer offers therefore
        /// cannot survive into a confirm.
        /// </summary>
        private static ClearSelectionViewModel CreateClearSelection(
            IReadOnlyList<ClearOptionProjection> options,
            ActionDefinitionId openClearActionDefinitionId,
            IReadOnlyList<TokenInstanceId> selectedTokenIds)
        {
            if (!openClearActionDefinitionId.IsDefined)
            {
                return null;
            }

            var option = options.SingleOrDefault(value =>
                value.ReplacedAttackActionDefinitionId.Equals(openClearActionDefinitionId));
            if (option == null)
            {
                return null;
            }

            var offered = new HashSet<TokenInstanceId>(option.AdjacentTokens.Select(token => token.Id));
            var selected = selectedTokenIds.Where(offered.Contains).Distinct().ToArray();
            var invocation = option.FindInvocation(selected);
            if (invocation == null)
            {
                return null;
            }

            // The ordinal rule the token label has followed since milestone 6.2d, decided once over
            // the whole offered set: a name that occurs twice keeps its ordinal, one that occurs once
            // drops it.
            var repeated = new HashSet<string>(option.AdjacentTokens
                .GroupBy(token => token.Definition.DisplayName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key), StringComparer.Ordinal);
            var candidates = option.AdjacentTokens
                .Select((token, index) => new ClearTokenCandidateViewModel(
                    ClearLabels.CandidateLabel(
                        token.Definition.DisplayName,
                        repeated.Contains(token.Definition.DisplayName) ? index + 1 : (int?)null),
                    token.Id,
                    token.Coordinate,
                    selected.Contains(token.Id)))
                .ToArray();
            return new ClearSelectionViewModel(
                option.ReplacedAttackActionDefinitionId,
                candidates,
                new TypedInvocationViewModel(ClearLabels.ConfirmLabel, invocation),
                ClearLabels.CancelLabel);
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
            IMatchReadModel projection,
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

            // Milestone 8.11c: keyed by the candidate's value rather than by its coordinate, so a
            // candidate naming a hero or a minion by identity finds its own preview. The query
            // guarantees one preview per candidate value and refuses two on one space, so neither
            // this nor the reverse lookup below can be ambiguous.
            var previews = projection.PendingEffectChoicePreview
                .ToDictionary(preview => preview.CandidateValue);

            // Milestone 6.2d: which token names this choice would repeat, decided once over the whole
            // offered set rather than per candidate. A name that occurs twice keeps its ordinal, so two
            // Hazards stay distinguishable; a name that occurs once drops it.
            var repeatedTokenNames = new HashSet<string>(projection.PendingEffectChoicePreview
                .Select(preview => FindTokenName(projection, preview))
                .Where(name => name != null)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key), StringComparer.Ordinal);

            // Milestone 8.15, on the same decided-once-over-the-set terms: which card names this
            // choice offers more than once. Wasp's eighteen are distinct, so the ordinal this feeds
            // is for content that deals duplicates, exactly as the token rule is.
            var repeatedCardNames = new HashSet<string>(choice.SelectInvocations
                .Select(invocation => invocation.Candidate)
                .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => FindCardName(projection, candidate.Value.CardInstanceId))
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
                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.Effect,
                    candidateCommand => candidateCommand.Kind ==
                            DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(candidateCommand.EffectChoice, invocation));

                // Milestone 6.5b: a value candidate can also name a piece that is not on the board at
                // all - a Spawn draws from the supply - and such a candidate is a control like a mode,
                // because there is no space for the board to light and nothing standing for a cue to
                // mark. What it is not is anonymous.
                //
                // Milestone 8.11c: "not on the board" is now asked of the board rather than assumed
                // from the value's kind. A minion named by identity is a supply candidate only when
                // occupancy has it standing nowhere; one that is standing somewhere is marked and
                // clicked like every other piece, and the preview is what knows which.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Minion &&
                    !previews.ContainsKey(candidate.Value))
                {
                    invocations.Add(new TypedInvocationViewModel(
                        FormatSupplyMinionLabel(projection, candidate.Value.MinionId, ordinals[candidate]),
                        command));
                    continue;
                }

                // Milestone 8.15: a candidate that is a printed number is named as that number. The
                // one production select of this shape is the kinetic "3 or 4 spaces" distance, whose
                // two controls read `OPTION #1` and `OPTION #2` until the player-pipeline journey
                // met them - a player choosing between the printed 3 and the printed 4 reads 3 and 4.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Integer)
                {
                    invocations.Add(new TypedInvocationViewModel(
                        candidate.Value.IntegerValue.ToString(
                            System.Globalization.CultureInfo.InvariantCulture),
                        command));
                    continue;
                }

                // Milestone 8.15: a candidate that names a card is named as that card - the chooser
                // rulebook p.10 hands a discard to was reading `OPTION #1` where their own hand
                // stood, which the first player-pipeline journey met and no headless fixture could.
                // Milestone 8.15a carries the whole printed face beside the name, at the user's
                // direction, so the control is the card itself exactly as a Defense offer is; the
                // name stays as the control's hover and assistive reading.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.CardInstance)
                {
                    var cardName = FindCardName(projection, candidate.Value.CardInstanceId);
                    invocations.Add(new TypedInvocationViewModel(
                        cardName == null
                            ? EffectChoiceLabels.CandidateLabel(
                                candidate.OptionNodeId, ordinals[candidate])
                            : EffectChoiceLabels.CardTargetLabel(
                                cardName,
                                repeatedCardNames.Contains(cardName)
                                    ? ordinals[candidate]
                                    : (int?)null),
                        command,
                        card: CreateCardFace(projection, candidate.Value.CardInstanceId)));
                    continue;
                }

                if (candidate.Kind != EffectChoiceCandidateKind.Value ||
                    !previews.TryGetValue(candidate.Value, out var preview))
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

                // Milestone 6.3a: a target that names a token carries that token's identity, so the
                // board can mark the piece rather than reading occupancy back out of the space.
                namedTargets.Add(new EffectChoiceTargetViewModel(
                    FormatTargetLabel(projection, preview, ordinals[candidate], repeatedTokenNames),
                    preview.Coordinate,
                    preview.ResultingCoordinate,
                    command,
                    FindToken(projection, preview)?.Id ?? default));
            }

            foreach (var invocation in choice.DeselectInvocations)
            {
                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.Effect,
                    candidateCommand => candidateCommand.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(candidateCommand.EffectChoice, invocation));
                var candidate = invocation.Candidate;
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    previews.TryGetValue(candidate.Value, out var preview))
                {
                    if (preview.NamesObject)
                    {
                        namedTargets.Add(new EffectChoiceTargetViewModel(
                            FormatTargetLabel(
                                projection, preview, ordinals[candidate], repeatedTokenNames),
                            preview.Coordinate,
                            preview.ResultingCoordinate,
                            command,
                            FindToken(projection, preview)?.Id ?? default));
                    }
                    else
                    {
                        boardDestinations.Add(new TypedInvocationViewModel(
                            EffectChoiceLabels.DeselectLabel(
                                candidate.OptionNodeId, ordinals[candidate]),
                            command));
                    }

                    continue;
                }

                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeselectLabel(
                        candidate.OptionNodeId, ordinals[candidate]),
                    command));
            }

            if (choice.ConfirmInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    choice.Kind == EffectChoiceKind.ConfirmOrDecline
                        ? EffectChoiceLabels.AcceptLabel
                        : EffectChoiceLabels.CompletionLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.ConfirmInvocation))));
            }

            if (choice.DeclineInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeclineLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.DeclineInvocation))));
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
        /// The projected display name of one card instance, or null when the projection does not
        /// carry it. Read exactly the way the tray names the same card - instance to definition to
        /// display name - and never from printed card text.
        /// </summary>
        private static string FindCardName(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(cardInstanceId));
            if (card == null)
            {
                return null;
            }

            return projection.Definitions
                .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference))?
                .DisplayName;
        }

        /// <summary>
        /// Names the object a target candidate stands on, from projected state alone. A hero is named
        /// by its configured display name, a minion by the team and intrinsic type its occupancy
        /// carries, and - since milestone 6.2d - a token by the display name its projected definition
        /// carries, with an ordinal only where the choice offers that name twice.
        /// </summary>
        private static string FormatTargetLabel(
            IMatchReadModel projection,
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
            IMatchReadModel projection, EffectChoiceCandidatePreview preview) =>
            FindToken(projection, preview)?.Definition.DisplayName;

        /// <summary>
        /// The projected token a preview names, or <c>null</c> when it names something else or the
        /// projection no longer carries that instance. Milestone 6.3a made it the shared reading: the
        /// label and the board cue must be talking about the same piece, and they are only guaranteed
        /// to be when both come from one lookup against one list.
        /// </summary>
        private static PlacedToken FindToken(
            IMatchReadModel projection, EffectChoiceCandidatePreview preview) =>
            preview.Occupant.Kind != BoardObjectKind.Token
                ? null
                : projection.PlacedTokens.SingleOrDefault(token =>
                    token.Id.Equals(preview.Occupant.TokenInstanceId));

        /// <summary>
        /// Names a miniature a candidate would take from the supply. The board projection cannot
        /// describe it, because it is standing nowhere; the declared setup roster carries the team and
        /// the intrinsic type, which is the same pair a standing minion is named by, so a supply
        /// candidate and a board candidate read alike rather than one of them reading as an ordinal.
        /// </summary>
        private static string FormatSupplyMinionLabel(
            IMatchReadModel projection, MinionId minionId, int ordinal)
        {
            var declared = projection.Setup.Minions.SingleOrDefault(value =>
                value.MinionId.Equals(minionId));
            return declared == null
                ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                : EffectChoiceLabels.MinionTargetLabel(
                    declared.TeamSide, declared.MinionType, ordinal);
        }

        private RoundActionViewModel GetOrCreateViewModel(IMatchReadModel projection)
        {
            // Milestone 6.3a: an open Clear belongs to the projection it was opened against. A
            // projection-version change - an accepted or refused command, End Turn, Undo, restart,
            // restore, or replay - drops it, so a stale token control or subset can never be
            // submitted and the surface is rebuilt from the position as it now is.
            if (clearModeProjection != null && !ReferenceEquals(projection, clearModeProjection))
            {
                clearModeProjection = null;
                clearModeActionDefinitionId = default;
                clearSelection.Clear();
                lastProjection = null;
            }

            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(
                    projection, clearModeActionDefinitionId, clearSelection, session.IsDirector,
                    homeHeroId);
            }

            return lastViewModel;
        }

        private static TypedInvocationViewModel CreateEndTurnInvocation(IMatchReadModel projection)
        {
            var movementChoice = projection.PendingMovementChoice;
            if (movementChoice != null && movementChoice.CanStop)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Movement,
                        command => command.Kind == DirectorCommandKind.StopMovement &&
                            command.ResolutionId.Equals(movementChoice.ResolutionId) &&
                            command.ChoiceId.Equals(movementChoice.ChoiceId)));
            }

            // Milestone 8.35v (the THIRD amendment: "an undeclined optional branch" is named among
            // the states a player may walk away from). A confirm-or-decline step, or any bounded
            // choice whose printed minimum is zero, already projects its own Decline the same way
            // the contextual region's own ACCEPT/DECLINE banners read it - this branch reads that
            // existing invocation rather than composing a new one, so END TURN's press and the
            // banner's own DECLINE press are provably the same authoritative answer.
            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice?.DeclineInvocation != null)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, effectChoice.DeclineInvocation)));
            }

            // Milestone 6.5ae: the same contextual control on the decision family that had lacked it.
            // A paused effect choice whose every option was refused projects a stop of its own, and it
            // is offered exactly where the authority offers it - this branch reads the invocation and
            // never decides that a choice is closable.
            if (effectChoice?.StopInvocation != null)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, effectChoice.StopInvocation)));
            }

            // Milestone 8.35g's pause; milestone 8.35v (the THIRD amendment's REVISED SEMANTICS)
            // collapses what used to be its own second press into the SAME press that stopped,
            // declined or Held above - see <see cref="RoundActionPresenter.Submit"/>, which chains
            // straight into this branch's command once the completion it just submitted lands the
            // projection here. Read here, this branch is unchanged: the turn's resolution is
            // finished and waiting for this player to hand it on, read off the standing contract so
            // only the seat that owns the pause is drawn one.
            if (projection.DecisionContract != null &&
                projection.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.TurnRelease,
                        command => command.Kind == DirectorCommandKind.ReleaseTurn));
            }

            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined ||
                HasPendingDecision(projection))
            {
                return null;
            }


            // Milestone 8.7: the authority decides whether p.12's Hold is offered, and this draws its
            // answer. Deriving it here a second time is what made the control and the command able to
            // disagree.
            var hold = projection.AvailableHoldOffer;
            return hold.HasValue && hold.Value.IsAvailable
                ? new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId))
                : null;
        }

        /// <summary>
        /// Milestone 8.35v (the THIRD amendment). The one open, not-yet-committed selection this
        /// build has no authority-offered decline for: an Attack whose target is not yet chosen.
        /// Rulebook p.14's target choice is mandatory once an Attack starts and the authority offers
        /// no stop for it the way Movement and an optional effect choice do - but the choice itself
        /// has not committed anything yet (milestone 8.35f's own wall rises only once
        /// <c>ChooseAttackTarget</c> is accepted, and 8.35p extended that same wall to a card-text-
        /// bound Attack that never emits the command at all), so the seat's own bounded rewind can
        /// discard it. True only when that rewind is actually standing - the same
        /// <see cref="IMatchReadModel.IsSeatUndoAvailable"/> fact milestone 8.35f's own square
        /// reads - so a state this method has not anticipated falls through to
        /// <see cref="ResolveEndTurnUnavailableReason"/> rather than composing a rewind the host
        /// would refuse. Gated by <see cref="IsViewerTheActingHero"/> for the same reason that
        /// gates the other helper below - see its remarks.
        /// </summary>
        private static bool ResolveEndTurnCancelsOpenAttack(
            IMatchReadModel projection, HeroId homeHeroId, TypedInvocationViewModel endTurnInvocation) =>
            endTurnInvocation == null &&
            IsViewerTheActingHero(projection, homeHeroId) &&
            projection.PendingAttackTargetChoice != null &&
            projection.IsSeatUndoAvailable;

        /// <summary>
        /// Milestone 8.35v. Whether this viewer is the hero the match is presently waiting on for a
        /// turn action - never true for a seat that merely owns SOME standing decision the acting
        /// hero's own turn produced (a defender answering Defense, a hero displaced by a push, one
        /// paying a removal prevention). <see cref="IMatchReadModel.SelectedActor"/> is public
        /// (every viewer's board marks the same acting piece, milestone 8.35n), so a seat-scoped
        /// projection cannot be trusted to read as "mine" from its mere presence the way an
        /// owner-narrowed pending choice can - <c>AwaitedInputQuery</c> names the identical trap for
        /// the top-bar predicate: "an attacker is still the selected actor while the DEFENDER is
        /// being asked, so a SelectedActor-based predicate lights the attacker's bar during someone
        /// else's decision." The Director answers every family at once and passes a default
        /// <paramref name="homeHeroId"/>, so it reads every branch below as its own.
        /// </summary>
        private static bool IsViewerTheActingHero(IMatchReadModel projection, HeroId homeHeroId) =>
            projection.IsTurnActive &&
            projection.SelectedActor.IsDefined &&
            (!homeHeroId.IsDefined || projection.SelectedActor.ActorId.Equals(homeHeroId));

        /// <summary>
        /// Milestone 8.35v. Reached only once <see cref="CreateEndTurnInvocation"/> found no direct
        /// completion and <see cref="ResolveEndTurnCancelsOpenAttack"/> found no open Attack
        /// selection to discard - so if this viewer is genuinely the acting hero, every
        /// printed-mandatory step and every wall the game can put in front of it is exactly one of
        /// the two named reasons below. Named per the SIXTH amendment's cursor-tooltip rule: nothing
        /// is ever greyed without saying why.
        /// </summary>
        private static string ResolveEndTurnUnavailableReason(
            IMatchReadModel projection, HeroId homeHeroId)
        {
            if (!IsViewerTheActingHero(projection, homeHeroId))
            {
                return null;
            }

            // A printed rule leaves the standing step no legal decline: Movement below its required
            // minimum, or an effect choice with real candidates, no printed "may", and no stop
            // invocation of its own - neither an undeclined optional branch nor an emptied mandatory
            // one.
            if (projection.PendingMovementChoice != null && !projection.PendingMovementChoice.CanStop)
            {
                return ActionUnavailabilityLabels.MandatoryStep;
            }

            if (projection.PendingEffectChoice != null &&
                projection.PendingEffectChoice.DeclineInvocation == null &&
                projection.PendingEffectChoice.StopInvocation == null)
            {
                return ActionUnavailabilityLabels.MandatoryStep;
            }

            // The resolution has moved past anything this seat can answer or cancel: a target was
            // already chosen and the defender (or the card's own text) now owns the wait, exactly
            // milestone 8.35f/8.35p's own wall. The remaining declared families never coincide with
            // this seat's own acting turn in practice - defense, respawn, correction and removal
            // prevention are each asked of a different hero, and tied Initiative precedes a turn
            // rather than standing inside one - but each is named here rather than assumed absent,
            // so a state nobody anticipated still reads as "another seat's decision" instead of
            // silently drawing an END TURN that would refuse.
            if ((projection.HasPendingAttackResolution && projection.PendingAttackTargetChoice == null) ||
                (projection.HasPendingMovementResolution && projection.PendingMovementChoice == null) ||
                (projection.HasPendingSkillResolution && projection.PendingEffectChoice == null) ||
                projection.PendingDefenseChoice != null ||
                projection.PendingHeroRespawnChoice != null ||
                projection.PendingDisplacedMinionCorrection != null ||
                projection.PendingRemovalPrevention != null ||
                projection.PendingInitiativeChoice != null)
            {
                return ActionUnavailabilityLabels.AnotherSeatDeciding;
            }

            // Every named cause is accounted for above; a state that reaches here has none of them
            // and still could not compose a direct completion (Hold refused for some other reason a
            // future milestone may name) - the honest generic word rather than a guess.
            return ActionUnavailabilityLabels.Generic;
        }

        private static IReadOnlyList<TypedInvocationViewModel> CreateDefenseChoices(
            IMatchReadModel projection)
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
                    var sourceDefinition = projection.Definitions.Single(value =>
                        value.Reference.Equals(offer.SourceDefinitionReference));
                    var sourceAction = sourceDefinition.ActionDefinitions.Single(value =>
                        value.Id.Equals(offer.ActionDefinitionId));
                    var choicePreview = preview.Choices.Single(value =>
                        value.CardInstanceId.HasValue &&
                        value.CardInstanceId.Value.Equals(offer.SourceCardInstanceId));

                    // Milestone 8.11a. A Block has no printed Defense to name, so it is named by what
                    // it does. Asking for its printed icon was the fault that stopped an attack
                    // answered by one: `Single` found nothing on a Block's empty contribution list and
                    // threw where the prompt is drawn, so the defender was asked nothing at all.
                    var label = choicePreview.DefenseTotal.HasValue
                        ? $"DISCARD {choicePreview.ChoiceLabel.ToUpperInvariant()}\n" +
                            "PRINTED DEFENSE " +
                            choicePreview.DefenseContributions
                                .Single(value => value.Kind == ValueContributionKind.PrintedIcon).Amount
                        : $"DISCARD {choicePreview.ChoiceLabel.ToUpperInvariant()}\n" +
                            (choicePreview.BlockResult == DefenseBlockResult.Blocked
                                ? "BLOCK - CONDITION MET"
                                : "BLOCK - CONDITION NOT MET");
                    return new TypedInvocationViewModel(
                        label,
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Defense,
                            command => command.Kind == DirectorCommandKind.ChooseDefenseCard &&
                                command.ResolutionId.Equals(choice.ResolutionId) &&
                                command.ChoiceId.Equals(choice.ChoiceId) &&
                                command.CardInstanceId.Equals(offer.SourceCardInstanceId)),
                        CreateDefenseBreakdown(projection.Version, preview, choicePreview),
                        CreateCardFace(projection, offer.SourceCardInstanceId),
                        sourceAction.Origin == ActionDefinitionOrigin.PrintedPrimary
                            ? CardShineSlot.PrimaryRing
                            : CardShineSlot.SecondaryDefense);
                })
                .ToList();
            if (choice.CanDecline)
            {
                var declinePreview = preview.Choices.Single(value => value.IsDecline);
                values.Add(new TypedInvocationViewModel(
                    "DECLINE DEFENSE",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Defense,
                        command => command.Kind == DirectorCommandKind.DeclineDefense &&
                            command.ResolutionId.Equals(choice.ResolutionId) &&
                            command.ChoiceId.Equals(choice.ChoiceId)),
                    CreateDefenseBreakdown(projection.Version, preview, declinePreview)));
            }

            return values;
        }

        /// <summary>
        /// One offered card as the tray draws it.
        ///
        /// <para>Milestone 8.11b built it for p.11's Defense response; milestone 8.15a hands the same
        /// face to every card-instance effect-choice candidate - rulebook p.10's discard chooser
        /// included - so a choice among cards is a choice among the cards themselves. It is
        /// <see cref="CurrentRoundCards.TrayCard"/> and nothing composed beside it, so an offer shows
        /// the same Initiative, name, printed values, qualifiers and exact source text the same card
        /// shows in the hand it is still sitting in - one card, one face. It is built selectable
        /// because the authority is offering it, which is what keeps the face at full strength rather
        /// than wearing the spent-card treatment.</para>
        /// </summary>
        private static TrayCardViewModel CreateCardFace(
            IMatchReadModel projection,
            CardInstanceId cardInstanceId)
        {
            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(cardInstanceId));
            return card == null
                ? null
                : CurrentRoundCards.TrayCard(
                    projection,
                    card,
                    card.State.Location == CardLocation.Hand
                        ? TrayCardGroup.Available
                        : TrayCardGroup.Discarded,
                    0,
                    true,
                    false,
                    // Milestone 8.35m: every caller of this method is offering the card as a legal
                    // answer - the doc comment above already says so - so the card-scope shine is
                    // unconditional here too, on the same reasoning `isSelectable: true` already used.
                    isAwaitedAnswer: true);
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
            else if (choice.BlockResult == DefenseBlockResult.ConditionUnfulfilled)
            {
                // Milestone 8.11a: named apart from the no-Defense-Total case below, because p.14's
                // other half is not "the attack succeeds" - it is "you are defeated", and a defender
                // deciding between offers has to be told which of the two this is.
                lines.Add("  BLOCK - CONDITION NOT MET");
                lines.Add("RESULT: DEFEATED");
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

            var resultStatement = choice.BlockResult == DefenseBlockResult.Blocked
                ? "BLOCK — DEFENDED"
                : choice.BlockResult == DefenseBlockResult.ConditionUnfulfilled
                    ? "DEFEATED"
                    : !choice.DefenseTotal.HasValue
                        ? "NO DEFENSE — ATTACK SUCCEEDS"
                        : choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                            ? "DEFENDED"
                            : "ATTACK SUCCEEDS";
            return new DefenseBreakdownViewModel(
                version,
                preview.ResolutionId,
                preview.ChoiceId,
                choice.CardInstanceId,
                choice.IsDecline
                    ? "DEFENSE PREVIEW: DECLINE"
                    : $"DEFENSE PREVIEW: {choice.ChoiceLabel.ToUpperInvariant()}",
                string.Join("\n", lines),
                choice.Outcome,
                preview.AttackContributions.Select(CreateBreakdownRow),
                preview.AttackTotal,
                choice.DefenseContributions.Select(CreateBreakdownRow),
                choice.DefenseTotal,
                choice.BlockResult,
                resultStatement);
        }

        private static DefenseContributionRowViewModel CreateBreakdownRow(
            ValueContributionProjection contribution) =>
            new DefenseContributionRowViewModel(
                contribution.Amount, contribution.SourceLabel);

        private static string FormatContribution(ValueContributionProjection contribution)
        {
            var sign = contribution.Amount >= 0 ? "+" : string.Empty;
            return $"  {sign}{contribution.Amount} — {contribution.SourceLabel}";
        }

        private static string FormatActionLabel(IMatchReadModel projection, ActionOffer offer)
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

        private static string GetCardDisplayName(IMatchReadModel projection, CardInstanceId cardId)
        {
            var card = projection.Cards.Single(value => value.Id.Equals(cardId));
            return projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).DisplayName.ToUpperInvariant();
        }

        /// <summary>
        /// Milestone 8.35s (the SIXTH amendment's cursor-tooltip design): the vocabulary word an
        /// unavailable printed-action offer's hover reads, sourced from the authority's own answer and
        /// never re-derived here.
        ///
        /// <para>Only <see cref="ActionUnavailabilityKind.BlockedByRuleModification"/> is a stated
        /// reason today - milestone 8.35l's Swap/Place restriction, threaded end to end as
        /// <see cref="ActionOffer.Unavailability"/>. Every other unavailable printed-action offer
        /// carries <see cref="ActionUnavailabilityKind.None"/>, and that single unstated bit is honestly
        /// ambiguous: <c>TurnResolutionAuthority.StampAvailability</c> greys the same way whether the
        /// cause is an Attack or Skill with nothing left to target, an action definition whose card text
        /// has no typed composition yet, an actor a rule modification restricts that
        /// <c>ActorMayPerform</c> does not yet thread through <c>ActionUnavailability</c>, or - milestone
        /// 8.29f, and the most common of the four in ordinary play - the turn's other printed action
        /// already under way while this one's sibling resolves. No fact published to presentation tells
        /// these apart, so naming one specifically would be a guess; the generic word is what the
        /// milestone's own no-invented-reasons rule asks for instead. This is a recorded gap, not an
        /// oversight - see the milestone's own notes for the Core-side extension it would take
        /// (a typed reason per clause, the way milestone 8.35l added one for the Skill clause alone).
        /// </para>
        /// </summary>
        private static string FormatUnavailabilityReason(IMatchReadModel projection, ActionOffer offer)
        {
            if (offer.Unavailability.Kind == ActionUnavailabilityKind.BlockedByRuleModification)
            {
                return ActionUnavailabilityLabels.BlockedBy(
                    HeroDisplayName(projection.Setup.Heroes, offer.Unavailability.BlockingHeroId),
                    GetCardDisplayName(projection, offer.Unavailability.BlockingCardInstanceId));
            }

            return ActionUnavailabilityLabels.Generic;
        }

        /// <summary>
        /// Milestone 8.35n (the FIFTH amendment, playtest finding: "ACTING ON INITIATIVE X is not
        /// enough ... hero name, who is acting, not only initiative"). Every branch below reads as
        /// one of two shapes, and the shape is what the narration says rather than a choice made
        /// here: a decision <paramref name="projection"/> carries in full - defense, leveling,
        /// respawn, movement, an attack target, tied Initiative, a Minion Battle removal, a
        /// correction, a removal prevention, an effect choice, and the viewer's own acting turn or
        /// release - is the VIEWER'S OWN, because a seat projection publishes those fields only to
        /// the seat that owns the standing decision (the Director owns every family at once, so
        /// every branch below is honest for both compositions without a second read). Everything
        /// else this method says is a fact <see cref="IMatchReadModel"/> already publishes to EVERY
        /// viewer regardless of ownership - the acting hero's name (public, from
        /// <see cref="IMatchReadModel.SelectedActor"/>), the turn-release owner (public, unscoped by
        /// 8.35g's own decision), and who still owes a commitment or a team-agreement answer (both
        /// stated public on their own declarations).
        ///
        /// <para><b>What this deliberately does NOT say, and why.</b> A non-owning viewer's
        /// projection carries <c>null</c> for defense, leveling and respawn, so this method cannot
        /// name that one of those is standing on the acting hero's turn, only that the hero is
        /// acting - the same recorded gap <see cref="AwaitedInputQuery.IsHeroAwaited"/> already
        /// carries for the hero-scope tray shine, closed only once the Core track's event-channel
        /// milestone adds the missing public owner fact to <see cref="IMatchReadModel"/>. Composing a
        /// spectator sentence that guessed at it from local state would be exactly the rule
        /// re-derivation the project's own discipline forbids.</para>
        /// </summary>
        private static string FormatStatus(
            IMatchReadModel projection, HeroId viewerHeroId, out string statusDetail)
        {
            statusDetail = null;
            var agreement = projection.TeamAgreement;
            if (agreement?.Kind == TeamAgreementKind.Initiative)
            {
                return "TIED INITIATIVE — " + FormatAgreementWait(projection, agreement);
            }

            if (agreement?.Kind == TeamAgreementKind.MinionBattle)
            {
                statusDetail = $"{agreement.SelectedUnits.Count} OF " +
                    $"{agreement.RequiredSelectionCount} MINIONS SELECTED";
                return "MINION BATTLE PROPOSAL — " + FormatAgreementWait(projection, agreement);
            }

            // Milestone 8.30b, and these two branches are what the user-approved amendment found.
            //
            // Moving this sentence into the top status bar made it the standing report of what the
            // match is doing, and two of the ten declared pending families had never been named here.
            // Both stand while the round phase reads EndOfRound, so both were reaching a player as
            // "ROUND 1 COMPLETE - END OF ROUND" while the match was in fact waiting on an answer - the
            // same shape milestone 8.20d found for the effect choice, which reached the table as
            // "choose an action or End Turn". They are read before the phase for exactly that reason:
            // a question the match is asking now outranks the phase it is asking it in.
            //
            // Neither is added to `IsAwaitingAnAnsweredDecision`. That list decides which offers are
            // suppressed, and these two already suppress their own - the end-of-round advance is
            // withheld against both by name - so moving them onto it would change what is offered in
            // order to fix what is said. `EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation`
            // compares this method against the declared inventory instead, which is the comparison
            // that was missing.
            // Milestone 8.30d rewords every sentence here into the player's language - who acts,
            // in game words, no engine vocabulary - without touching which family each branch
            // reads: `EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation` still compares this
            // method's compiled reads against the declared inventory, and the branches are the
            // same branches.
            // Milestone 8.30c. An ended match is a terminal player state, not another wait. Before
            // this branch it reached the fallback as "Waiting for the match to continue", which
            // drew a live-sounding prompt after every control had correctly disappeared. The outcome
            // is public and identical in all six seat projections, so the one shared derivation names
            // the winning side and the rulebook victory sentence for every perspective.
            if (projection.Phase == RoundResolutionPhase.GameEnded)
            {
                var outcome = projection.Outcome;
                var side = projection.Participants.GetTeam(outcome.WinningTeamId).Side;
                return MinionChoiceLabels.TeamLabel(side) + " wins: " +
                    FormatVictoryCause(outcome.Cause) + ".";
            }

            var minionBattle = projection.PendingMinionBattleChoice;
            if (minionBattle != null)
            {
                // The deciding side is kept in the demoted detail rather than the headline: a
                // seat viewer's own screen already knows whose decision "YOUR" is, and the
                // Director - who answers for both sides at once - is exactly who still needs it
                // named.
                statusDetail = $"{MinionChoiceLabels.TeamLabel(minionBattle.ChoosingSide)} — " +
                    $"{minionBattle.SelectedUnits.Count} CHOSEN";
                return NarrationLabels.Own(
                    "MINION BATTLE",
                    $"REMOVE {minionBattle.RequiredRemovalCount} " +
                    (minionBattle.RequiredRemovalCount == 1 ? "MINION" : "MINIONS"));
            }

            var transitionStep = projection.PendingSystemTransitionStep;
            if (transitionStep != null)
            {
                return NarrationLabels.Own("LEVEL UP", "CHOOSE HOW TO PROCEED");
            }

            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                return $"Round {projection.RoundIndex} complete.";
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var stillOwed = CommitmentsStillOwed(projection);
                var viewerOwes = viewerHeroId.IsDefined
                    ? stillOwed.Contains(viewerHeroId)
                    : stillOwed.Count > 0;
                var names = stillOwed
                    .Select(heroId => HeroDisplayName(projection.Setup.Heroes, heroId))
                    .ToArray();
                return viewerOwes
                    ? NarrationLabels.Own("COMMITMENT", "CHOOSE A CARD, OR PASS")
                    : names.Length > 0
                        ? "COMMITTING — " + NarrationLabels.WaitingOn(names)
                        : $"Round {projection.RoundIndex}, Turn {projection.TurnIndex}: " +
                            "players are choosing their cards.";
            }

            if (projection.PendingInitiativeChoice != null)
            {
                return NarrationLabels.Own("INITIATIVE", "CHOOSE WHO ACTS FIRST");
            }

            if (projection.PendingDefenseChoice != null)
            {
                statusDetail = $"ATTACK {projection.PendingDefenseChoice.AttackTotal}";
                return NarrationLabels.Own("DEFENSE", "CHOOSE A CARD, OR DECLINE");
            }

            // Milestone 8.20d: the seventh pending family, and the one that had never been here.
            //
            // It is read directly after the Defense response because that is the order the region
            // itself draws in: a standing Defense response is the whole of the contextual region and
            // returns before any effect control is built, so the status must say Defense there; below
            // it the decision has its own labelled section, and the four families under this branch
            // share the action row with it.
            // Milestone 8.21: the eighth pending family, read here for the reason the seventh is read
            // where it is - this is the order the region draws in. A standing removal prevention is
            // the whole of the contextual region: the Defeat it stands inside has stopped, so no
            // effect question of its own can be open beneath it, and everything below this branch
            // belongs to a turn that is currently accepting nothing.
            var removalPrevention = projection.PendingRemovalPrevention;
            if (removalPrevention != null)
            {
                return NarrationLabels.Own(
                    RemovalPreventionPromptLabels.SectionLabel,
                    RemovalPreventionPromptLabels.StatusLine(removalPrevention));
            }

            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice != null)
            {
                var subject = EffectChoicePromptLabels.SubjectOf(
                    effectChoice, projection.PendingEffectChoicePreview);
                return NarrationLabels.Own(
                    EffectChoicePromptLabels.SectionLabel(subject),
                    EffectChoicePromptLabels.StatusLine(effectChoice, subject, projection));
            }

            if (projection.PendingHeroRespawnChoice != null)
            {
                return NarrationLabels.Own(
                    "RESPAWN", "CHOOSE A SPACE, OR RESOLVE WITHOUT ACTING");
            }

            if (projection.PendingAttackTargetChoice != null)
            {
                return NarrationLabels.Own("ATTACK", "CHOOSE A TARGET");
            }

            if (projection.PendingMovementChoice != null)
            {
                var remaining = projection.PendingMovementChoice.RemainingAllowance;
                statusDetail = $"{remaining} " + (remaining == 1 ? "SPACE LEFT" : "SPACES LEFT");
                return NarrationLabels.Own("MOVEMENT", "CHOOSE A DESTINATION");
            }

            if (projection.PendingDisplacedMinionCorrection != null)
            {
                return NarrationLabels.Own("CORRECTION", "PLACE THE DISPLACED MINION");
            }

            // Milestone 8.35g: the eleventh pending family, and the last decision branch because the
            // pause is withheld while anything else stands - so anything above it that is standing
            // is the truer sentence. It has to be here at all because the acknowledgement that
            // raised the pause is what empties the selection: without this branch a turn waiting on
            // its own player fell through to "Waiting for the match to continue", the live-sounding
            // fallback milestone 8.30c removed for the ended match and 8.20d for the effect choice.
            // It names the side rather than the pressing seat, because one derivation serves every
            // viewer and the other five are waiting on exactly this.
            // Milestone 8.35n: `ActorId` is unscoped by 8.35g's own deliberate decision, so unlike
            // every branch above this one reads the SAME object on every viewer's projection - the
            // comparison below is what decides which of the two vocabularies applies, not the field.
            var turnRelease = projection.PendingTurnRelease;
            if (turnRelease != null)
            {
                return !viewerHeroId.IsDefined || turnRelease.ActorId.Equals(viewerHeroId)
                    ? NarrationLabels.Own("TURN", "RELEASE TO CONTINUE")
                    : NarrationLabels.HeroAwaitingRelease(
                        HeroDisplayName(projection.Setup.Heroes, turnRelease.ActorId));
            }

            if (projection.SelectedActor.IsDefined)
            {
                var initiative = projection.SelectedActor.InitiativeTotal;
                if (AwaitedInputQuery.MayTakeATurnAction(projection))
                {
                    statusDetail = $"INITIATIVE {initiative}";
                    return NarrationLabels.Own("TURN", "CHOOSE AN ACTION, OR END YOUR TURN");
                }

                return NarrationLabels.HeroActing(
                    HeroDisplayName(projection.Setup.Heroes, projection.SelectedActor.ActorId),
                    initiative);
            }

            return "Waiting for the match to continue.";
        }

        /// <summary>
        /// The heroes still owed a commitment - public, per <see cref="IMatchReadModel.Commitments"/>'s
        /// own declaration, the same fact <see cref="AwaitedInputQuery"/> reads at viewer and hero
        /// scope. Read directly here rather than through that class because the narration also needs
        /// the full set of names, not a single hero's yes/no answer.
        /// </summary>
        private static IReadOnlyList<HeroId> CommitmentsStillOwed(IMatchReadModel projection) =>
            Array.AsReadOnly((projection.Commitments ?? Array.Empty<MatchCommitmentProjection>())
                .Where(commitment => commitment.Status == TurnCardCommitmentStatus.AwaitingResponse)
                .Select(commitment => commitment.HeroId)
                .ToArray());

        /// <summary>
        /// The heroes on the standing team agreement's own participant list who have not yet given
        /// their part - a stated Initiative preference, or an explicit Minion Battle agreement, the
        /// same per-kind reading <c>AwaitedInputQuery.OwesAnAgreementAnswer</c> and
        /// <c>TeamAgreementHudPresentation.Create</c> both already use.
        /// </summary>
        /// <summary>
        /// Milestone 8.35x, and the defect the first live internet playtest found within minutes.
        ///
        /// <para>A standing team agreement whose participants have ALL answered is an ordinary,
        /// legal, transient state - the moment between the last statement landing and the authority
        /// resolving what it unblocked - and every seat refreshes through it. Both branches above
        /// handed that empty set straight to <see cref="NarrationLabels.WaitingOn"/>, which refuses
        /// it by design, so the round panel threw on EVERY refresh from that moment on. Milestone
        /// 8.35h's panel isolation then did its job exactly as designed and contained the throw to
        /// this one panel - which is what turned a crash into something far quieter and worse: the
        /// panel froze at its last good text (the playtest's immortal "YOUR TURN — RELEASE TO
        /// CONTINUE") and everything else riding the same refresh, the kinetic cue emission
        /// included, was starved for the rest of the match.
        /// </para>
        ///
        /// <para>The fix is here, at the caller, and deliberately not in the vocabulary: an empty
        /// owed-set is a real state that deserves a real sentence, not a constructed empty list and
        /// not a relaxed guard. <see cref="NarrationLabels.WaitingOn"/> stays defensive, so the next
        /// caller that forgets the empty case is still caught rather than quietly printing
        /// "WAITING ON: ".</para>
        /// </summary>
        private static string FormatAgreementWait(
            IMatchReadModel projection, TeamAgreementProjection agreement)
        {
            var stillOwed = AgreementStillOwed(projection, agreement);
            return stillOwed.Count == 0
                ? NarrationLabels.EveryAnswerIn
                : NarrationLabels.WaitingOn(stillOwed);
        }

        private static IReadOnlyList<string> AgreementStillOwed(
            IMatchReadModel projection, TeamAgreementProjection agreement) =>
            Array.AsReadOnly(agreement.Participants
                .Where(participant => agreement.Kind == TeamAgreementKind.Initiative
                    ? !participant.HasStatement
                    : !participant.Agreed)
                .Select(participant =>
                    HeroDisplayName(projection.Setup.Heroes, participant.HeroId))
                .ToArray());

        private static string FormatVictoryCause(MatchVictoryCause cause)
        {
            switch (cause)
            {
                case MatchVictoryCause.FinalWaveFlipped:
                    return "the final Wave counter was flipped";
                case MatchVictoryCause.ThroneCaptured:
                    return "the enemy Throne was captured";
                case MatchVictoryCause.LifeCountersExhausted:
                    return "the opposing team ran out of Life counters";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cause), cause, "An ended match requires one rulebook victory cause.");
            }
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
            TypedInvocationViewModel confirmInvocation,
            TeamAgreementHudViewModel agreement = null)
            : this(
                version, MinionChoicePromptKind.MinionBattleRemoval, resolutionId, choiceId,
                default, default, default, choosingTeamId, choosingSide, requiredRemovalCount,
                selectedCount, candidates, confirmInvocation, agreement)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException(
                    "A Minion Battle prompt requires the authoritative resolution and choice identities.");
            }
        }

        /// <summary>
        /// Milestone 8.35e. The Minion Battle's public stage: milestone 8.30n's team agreement
        /// standing over a selection whose authoritative pending detail this reader does not hold.
        /// </summary>
        /// <remarks>
        /// <para>Two readers reach it, and both are ordinary. A seat on the side that is not
        /// choosing never holds the detail at all - <c>SeatPendingSurface</c> gives a decision's own
        /// content to the seat that answers it - and a seat on the choosing side stops holding it
        /// for as long as its own accepted press is in flight, because
        /// <c>NetworkMatchReadModel</c> strips every pending answer while the view is not
        /// actionable. The public agreement is public in both, so what remains renderable is exactly
        /// what the table can see: the counts, the agreement's own statements, and the selected
        /// units the board already cues from <c>TeamAgreement.SelectedUnits</c>.</para>
        /// <para>It carries no end-of-round identities because there is no pending choice here to
        /// read them from, which is why it cannot go through the removal-prompt constructor: that
        /// one's guard exists to refuse exactly the fabricated identities this stage would have to
        /// invent. Nothing on this form submits a rules command - the choosing side answers through
        /// <see cref="MinionChoicePresenter.SubmitAgreement"/>, and the other side answers nothing.</para>
        /// </remarks>
        public static MinionChoiceViewModel ForPublicAgreement(
            long version,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            int requiredRemovalCount,
            int selectedCount,
            TeamAgreementHudViewModel agreement)
        {
            if (agreement == null)
            {
                throw new ArgumentNullException(nameof(agreement));
            }

            if (agreement.Kind != TeamAgreementKind.MinionBattle)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(agreement),
                    agreement.Kind,
                    "The public form of this prompt draws the Minion Battle's own team agreement.");
            }

            return new MinionChoiceViewModel(
                version, MinionChoicePromptKind.MinionBattleRemoval, default, default,
                default, default, default, choosingTeamId, choosingSide, requiredRemovalCount,
                selectedCount, Array.Empty<MinionChoiceCandidateViewModel>(), null, agreement);
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
                subjectMinionId, choosingTeamId, choosingSide, 1, 0, candidates, null, null);
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
            TypedInvocationViewModel confirmInvocation,
            TeamAgreementHudViewModel agreement)
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
            Agreement = agreement;
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
        public TeamAgreementHudViewModel Agreement { get; }

        /// <summary>
        /// Milestone 8.35e. Whether this is the battle's public stage rather than an owned removal
        /// prompt: no authoritative pending detail stands behind it, so it names no end-of-round
        /// identities, offers no candidates and carries no confirm. Everything it does draw - the
        /// counts and <see cref="Agreement"/> - is public to all six seats.
        /// </summary>
        public bool IsPublicAgreementStage =>
            Kind == MinionChoicePromptKind.MinionBattleRemoval && !ResolutionId.IsDefined;

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
        private readonly IMatchClient session;
        private readonly IMinionChoiceView view;
        private IMatchReadModel lastProjection;
        private MinionChoiceViewModel lastViewModel;


        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor every existing fixture and journey uses,
        /// composing the Director's own client. A seat-scoped surface passes its client instead.
        /// </summary>
        public MinionChoicePresenter(IMatchSession session, IMinionChoiceView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public MinionChoicePresenter(IMatchClient session, IMinionChoiceView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;

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
        /// Milestone 8.11a, at the user's request. Answers the Minion Battle by clicking the unit it is
        /// asking about, which is the same click that takes an Attack target.
        ///
        /// <para>The board already identified every candidate before this milestone; the unified
        /// <see cref="PieceTargetCueViewModel"/> now carries that same fact. The piece a player was
        /// being asked about was already the piece they were looking at. What was missing was only
        /// that clicking it did anything.</para>
        /// </summary>
        public bool SubmitUnit(RuleObject unit)
        {
            if (!unit.IsDefined)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var candidate = current?.Candidates.SingleOrDefault(value => value.Unit.Equals(unit));
            return candidate != null && Execute(candidate.Command);
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

        public bool SubmitAgreement(TeamAgreementChoiceViewModel choice)
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (choice == null || current?.Agreement?.Choices.Any(value =>
                    value.Command.Equals(choice.Command)) != true)
            {
                Refresh();
                return false;
            }

            var result = session.SubmitAgreement(choice.Command);
            if (result.Accepted)
                SessionProjectionChanged?.Invoke(result.Projection);
            else
                Refresh();
            return result.Accepted;
        }

        public static MinionChoiceViewModel CreateViewModel(IMatchReadModel projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var choice = projection.PendingMinionBattleChoice;
            if (choice == null)
            {
                var publicAgreement = TeamAgreementHudPresentation.Create(projection);
                if (publicAgreement?.Kind == TeamAgreementKind.MinionBattle)
                {
                    // Milestone 8.35e. This went through the removal-prompt constructor with default
                    // identities, and that constructor's own guard refuses exactly those, so the
                    // branch threw on every render in the one state it exists to draw - on both
                    // sides, from the first selection onward, and each throw took the rest of
                    // MatchFeatureCoordinator.RefreshAll with it. The public stage has its own form
                    // now; the guarded one is reached only from an owned pending choice below.
                    var agreement = projection.TeamAgreement;
                    var team = projection.Participants.Teams.Single(value =>
                        value.Side == agreement.ChoosingSide);
                    return MinionChoiceViewModel.ForPublicAgreement(
                        projection.Version, team.Id, agreement.ChoosingSide,
                        agreement.RequiredSelectionCount, agreement.SelectedUnits.Count,
                        publicAgreement);
                }

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
                var typedInvocation = isSelected ? deselect : select;
                var expectedKind = isSelected
                    ? DirectorCommandKind.DeselectMinionBattleRemoval
                    : DirectorCommandKind.SelectMinionBattleRemoval;
                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.MinionBattle,
                    candidateCommand => candidateCommand.Kind == expectedKind &&
                        candidateCommand.TeamSide == typedInvocation.ChoosingSide &&
                        candidateCommand.MinionId.Equals(typedInvocation.MinionId) &&
                        candidateCommand.HeroId.Equals(typedInvocation.HeroId) &&
                        candidateCommand.EndOfRoundResolutionId.Equals(
                            typedInvocation.ResolutionId) &&
                        candidateCommand.EndOfRoundChoiceId.Equals(typedInvocation.ChoiceId));
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
                projection.TeamAgreement == null && choice.CanConfirm
                    ? new TypedInvocationViewModel(
                        choice.RequiredRemovalCount == 1 ? "CONFIRM REMOVAL" : "CONFIRM REMOVALS",
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.MinionBattle,
                            command => command.Kind ==
                                    DirectorCommandKind.ConfirmMinionBattleRemovals &&
                                command.TeamSide == choice.ConfirmInvocation.ChoosingSide &&
                                command.EndOfRoundResolutionId.Equals(
                                    choice.ConfirmInvocation.ResolutionId) &&
                                command.EndOfRoundChoiceId.Equals(
                                    choice.ConfirmInvocation.ChoiceId)))
                    : null,
                TeamAgreementHudPresentation.Create(projection));
        }

        /// <summary>
        /// Milestone 6.4b: the displaced-minion correction rides the same prompt. Everything shown is
        /// read off the projected pending choice - candidates, kinds, invocations - so presentation
        /// calculates no path, distance, Battle-Zone membership, occupancy, or Tie-Breaker ordering.
        /// A subject label joins the projection's minion occupancy for team and intrinsic type, which
        /// is a lookup of projected state, not a rules answer.
        /// </summary>
        private static MinionChoiceViewModel CreateCorrectionViewModel(IMatchReadModel projection)
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
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Correction,
                            command => command.Kind == DirectorCommandKind.ChooseCorrectionSubject &&
                                command.TeamSide == invocation.ChoosingSide &&
                                command.MinionId.Equals(invocation.MinionId) &&
                                command.ResolutionId.Equals(invocation.ResolutionId) &&
                                command.ChoiceId.Equals(invocation.ChoiceId))));
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
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Correction,
                        command => command.Kind == DirectorCommandKind.ChooseCorrectionDestination &&
                            command.TeamSide == invocation.ChoosingSide &&
                            command.MinionId.Equals(invocation.MinionId) &&
                            command.Coordinate.Equals(invocation.Destination) &&
                            command.ResolutionId.Equals(invocation.ResolutionId) &&
                            command.ChoiceId.Equals(invocation.ChoiceId)),
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

        private MinionChoiceViewModel GetOrCreateViewModel(IMatchReadModel projection)
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
            var result = session.Submit(command);
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

        public event Action<IMatchReadModel> SessionProjectionChanged;

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
        private readonly HeroOverviewsPresenter heroOverviews;
        private readonly KineticCuePresenter kinetics;
        private readonly MatchLogPresenter matchLog;
        private readonly Action<string, Exception> panelRefreshFailed;
        private IReadOnlyList<string> failedPanelNames = Array.Empty<string>();

        public MatchFeatureCoordinator(
            BoardInteractionPresenter board,
            DirectorHandsPresenter hands,
            RoundActionPresenter round,
            UndoRestartPresenter undoRestart,
            DiagnosticLogPresenter diagnostics,
            MinionChoicePresenter minionChoice,
            Action<IMatchReadModel> afterSessionChange = null,
            HeroOverviewsPresenter heroOverviews = null,
            Action<string, Exception> panelRefreshFailed = null,
            // Milestone 8.35y. Optional, and appended, so every existing composition and
            // fixture keeps its exact call. A surface with no kinetics is the surface this project had
            // before the milestone, which is also the state a failed driver falls back to.
            KineticCuePresenter kinetics = null,
            // Milestone 8.35x. Same reason, same shape: the narration bar's log drop-down joins the
            // isolated fan-out rather than the unisolated header callback beside it, so one broken
            // log line degrades that one panel and nothing else - the EIGHTH amendment's own rule,
            // applied to the panel it did not yet cover.
            MatchLogPresenter matchLog = null)
        {
            this.panelRefreshFailed = panelRefreshFailed;
            this.heroOverviews = heroOverviews;
            this.kinetics = kinetics;
            this.matchLog = matchLog;
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hands = hands ?? throw new ArgumentNullException(nameof(hands));
            this.round = round ?? throw new ArgumentNullException(nameof(round));
            // Milestone 8.30b: the Director-only pair is optional, because a seat-scoped HUD composes
            // neither. Undo, restart and the diagnostic log are the Director's authority, and a player
            // surface must not be able to reach them even by being handed one.
            this.undoRestart = undoRestart;
            this.diagnostics = diagnostics;
            this.minionChoice = minionChoice ?? throw new ArgumentNullException(nameof(minionChoice));

            void Changed(IMatchReadModel projection)
            {
                afterSessionChange?.Invoke(projection);
                RefreshAll();
            }

            board.SessionProjectionChanged += Changed;
            hands.SessionProjectionChanged += Changed;
            round.SessionProjectionChanged += Changed;
            if (undoRestart != null)
            {
                undoRestart.SessionProjectionChanged += Changed;
            }

            minionChoice.SessionProjectionChanged += Changed;
        }

        /// <summary>
        /// Milestone 8.35h. The panels whose refresh threw during the latest <see cref="RefreshAll"/>,
        /// in fan-out order, so a surface can draw a named failed state instead of leaving a stale
        /// panel indistinguishable from a current one. Empty after any refresh in which nothing threw.
        /// </summary>
        public IReadOnlyList<string> FailedPanelNames => failedPanelNames;

        /// <summary>
        /// Milestone 8.35h. Refreshes every panel, each independently of the rest.
        ///
        /// <para>This ran as a straight line of seven calls until the invited playtest, where one
        /// view-model defect in the second presenter silently skipped the five behind it on every
        /// projection change and presented as a whole frozen client rather than one broken panel. The
        /// authority kept accepting submissions throughout, which is what made the symptom so
        /// misleading. Isolation is per panel and the failure is never swallowed: a composed HUD is
        /// told which panel failed and why, and a composition that supplies no sink is raised at
        /// rather than quietly degraded - the throw simply waits until every other panel has had its
        /// turn.</para>
        ///
        /// <para>It catches broadly on purpose. The point is that no presenter's defect can reach
        /// another panel, and narrowing the catch to the exception types seen so far would only mean
        /// the next unfamiliar one takes the client down again.</para>
        /// </summary>
        public void RefreshAll()
        {
            var failed = new List<string>();
            var errors = new List<Exception>();
            RefreshPanel("board", board.Refresh, failed, errors);
            RefreshPanel("hands", hands.Refresh, failed, errors);
            RefreshPanel("round", round.Refresh, failed, errors);
            RefreshPanel("undo-restart", () => undoRestart?.Refresh(), failed, errors);
            RefreshPanel("diagnostics", () => diagnostics?.Refresh(), failed, errors);
            RefreshPanel("minion-choice", minionChoice.Refresh, failed, errors);
            RefreshPanel("hero-overviews", () => heroOverviews?.Refresh(), failed, errors);
            // Last, and isolated like every other panel: a kinetic is a flourish, and it must be the
            // one thing on this list that can never cost a player a fact on the surfaces above it.
            RefreshPanel("kinetics", () => kinetics?.Refresh(), failed, errors);
            RefreshPanel("match-log", () => matchLog?.Refresh(), failed, errors);

            failedPanelNames = failed.Count == 0
                ? Array.Empty<string>()
                : new ReadOnlyCollection<string>(failed.ToArray());
            if (failed.Count == 0)
            {
                return;
            }

            if (panelRefreshFailed != null)
            {
                for (var index = 0; index < failed.Count; index++)
                {
                    panelRefreshFailed(failed[index], errors[index]);
                }

                return;
            }

            throw new AggregateException(
                "HUD panel refresh failed: " + string.Join(", ", failed.ToArray()) + ".", errors);
        }

        private static void RefreshPanel(
            string panel, Action refresh, List<string> failed, List<Exception> errors)
        {
            try
            {
                refresh();
            }
            catch (Exception error)
            {
                failed.Add(panel);
                errors.Add(error);
            }
        }
    }
}
