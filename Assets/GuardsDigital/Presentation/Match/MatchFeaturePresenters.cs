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
using GuardsDigital.Core.Effects.Synthetic;
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

        /// <summary>
        /// Milestone 8.37b. Taking back a NAMED candidate says the name back.
        ///
        /// <para>The first Play Mode run of Tali's direction journey is what found this: the six
        /// controls read EAST, NORTH EAST and so on, the status line read "Choose 1 of 6 directions" -
        /// and the control that takes the answer back still read <c>UNDO OPTION #1</c>, because
        /// selecting and DESELECTING were two label paths and only the first had been taught the
        /// vocabulary. A player who has just pressed NORTH EAST is not offered "undo option 1".</para>
        /// </summary>
        public static string DeselectLabel(EffectChoiceValueVocabulary vocabulary, int value) =>
            $"UNDO {VocabularyLabel(vocabulary, value)}";

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
        /// <summary>
        /// Milestone 8.37. The word a candidate wears when its choice DECLARED what its integers
        /// mean - a card colour or a board direction - rather than the digit it wears otherwise.
        ///
        /// <para>Composed from the enum member's own name rather than from a per-member table, so a
        /// colour or direction added to the rules reaches the HUD without a second edit here and can
        /// never appear as a number by omission. `NorthEast` reads as `NORTH EAST` on the same rule:
        /// the boundary between words is where the printed name has one.</para>
        ///
        /// <para>A value outside the declared vocabulary is a fault rather than a fallback: the
        /// vocabulary is the handler's own statement about the set it offered, so a candidate outside
        /// it means the two disagree, and drawing a digit would hide that.</para>
        /// </summary>
        public static string VocabularyLabel(EffectChoiceValueVocabulary vocabulary, int value)
        {
            string name;
            switch (vocabulary)
            {
                case EffectChoiceValueVocabulary.CardColor:
                    name = Enum.IsDefined(typeof(Core.Cards.CardColor), value)
                        ? ((Core.Cards.CardColor)value).ToString()
                        : null;
                    break;
                case EffectChoiceValueVocabulary.HexDirection:
                    name = Enum.IsDefined(typeof(Core.Board.HexDirection), value)
                        ? ((Core.Board.HexDirection)value).ToString()
                        : null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(vocabulary),
                        "A candidate label by vocabulary requires a declared vocabulary.");
            }

            if (name == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"{value} is not a member of the declared vocabulary {vocabulary}.");
            }

            return SpaceOutWords(name).ToUpperInvariant();
        }

        /// <summary>
        /// `NorthEast` to `North East`. The enum member is the printed name with its spaces removed,
        /// so putting them back is a reading of the name rather than a translation of it.
        /// </summary>
        private static string SpaceOutWords(string name)
        {
            var builder = new System.Text.StringBuilder(name.Length + 4);
            for (var index = 0; index < name.Length; index++)
            {
                if (index > 0 && char.IsUpper(name[index]))
                {
                    builder.Append(' ');
                }

                builder.Append(name[index]);
            }

            return builder.ToString();
        }

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
        Mixed = 6,

        /// <summary>
        /// Milestone 8.37. Rulebook p.9's printed card colours - Tali's Commune with Spirits and
        /// Reign of Winter. They are integers underneath, and were read as
        /// <see cref="Number"/> until the choice began declaring what its integers MEAN.
        /// </summary>
        Colour = 7,

        /// <summary>
        /// Milestone 8.37. Rulebook p.17's six board directions - Tali's Cold Snap and Snowstorm. The
        /// same story as <see cref="Colour"/>: integers whose meaning is declared, not inferred.
        /// </summary>
        Direction = 8
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
            // Milestone 8.37. A declared vocabulary answers this outright and takes precedence over
            // reading the candidates: it is the handler's own statement about the set, and every
            // candidate under one is an integer by construction.
            switch (choice.ValueVocabulary)
            {
                case EffectChoiceValueVocabulary.CardColor:
                    return EffectChoiceSubject.Colour;
                case EffectChoiceValueVocabulary.HexDirection:
                    return EffectChoiceSubject.Direction;
            }

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
        /// Names the owner of the standing card candidates. The configured hand-only card set is
        /// the discard family; retrieval is named only when the paused definition declares it.
        /// Public non-hand location alone cannot distinguish retrieval from borrowing a Primary.
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
                : IsDeclaredRetrieval(choice, projection)
                    ? $"for {heroName} to retrieve"
                    : $"belonging to {heroName}";
            var line = $"{bounds} of {count} {(count == 1 ? "card" : "cards")} {purpose}" +
                (choice.DeclineInvocation != null ? ", or decline." : ".");
            return choice.MaximumSelections > 1
                ? $"{line} {choice.SelectedCandidates.Count} chosen."
                : line;
        }

        private static bool IsDeclaredRetrieval(PendingEffectChoice choice, IMatchReadModel projection)
        {
            var resolution = projection.PendingSkillResolution;
            if (resolution == null || !resolution.ResolutionId.Equals(choice.ResolutionId)) return false;
            var source = projection.Definitions.FirstOrDefault(definition =>
                definition.Reference.Equals(resolution.SourceDefinitionReference));
            var effect = source?.ActionDefinitions
                .Select(action => action.EffectRootFor(resolution.EffectDefinitionReference))
                .FirstOrDefault(definition => definition != null);
            var node = effect?.Nodes.FirstOrDefault(value => value.Id.Equals(choice.NodeId));
            // Read declared presentation meaning, never infer it from card names/text or resolve it here.
            return node != null && node.Capability.Equals(SyntheticRosterCapabilities.RetrieveCardV1);
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
                case EffectChoiceSubject.Colour:
                    return "NAME A COLOUR";
                case EffectChoiceSubject.Direction:
                    return "CHOOSE A DIRECTION";
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
                case EffectChoiceSubject.Colour:
                    return count == 1 ? "colour" : "colours";
                case EffectChoiceSubject.Direction:
                    return count == 1 ? "direction" : "directions";
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
    ///
    /// <para>Milestone 8.46b narrowed what reaches here, without changing a line of it. A choice
    /// whose minimum and maximum are both one is now closed by the resolution session inside the
    /// Select itself, so by the time this reads the projection that pause is gone and the
    /// <c>ChoiceId</c> guard below answers null - the exactly-one case this was built for in 6.5c is
    /// no longer presentation's to complete. What still reaches it is every other way a selection can
    /// fill a choice: an `up to 1`, whose floor of zero means closing it having taken none is a real
    /// answer and so keeps a completion action, and a choose-several on its last candidate. Both are
    /// genuinely two answers at the authority, and both are left exactly as they were.</para>
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
        private readonly AttackSpotlightPresenter attackSpotlight;
        private readonly MatchLogPresenter matchLog;

        /// <summary>Milestone 8.46c. The one surface that asks the player a question.</summary>
        private readonly DecisionWindowPresenter decisionWindow;
        private readonly PhaseBannerPresenter phaseBanner;

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
            // Milestone 8.35z7. Same reason, same shape: the narration bar's log drop-down joins the
            // isolated fan-out rather than the unisolated header callback beside it, so one broken
            // log line degrades that one panel and nothing else - the EIGHTH amendment's own rule,
            // applied to the panel it did not yet cover.
            MatchLogPresenter matchLog = null,
            // Milestone 8.35z12. Same reason, same shape, and it is refreshed BEFORE the
            // kinetics below it deliberately: the spotlight's pacing decides whether that panel plays
            // its cues or holds them, so it has to have seen this projection first.
            AttackSpotlightPresenter attackSpotlight = null,
            // Milestone 8.46c. Same reason, same shape, and it is refreshed immediately BEFORE the
            // round panel deliberately: the strip yields a decision family to the window exactly
            // while the window is drawing it, so the window must have observed this projection
            // before the strip rebuilds against it.
            DecisionWindowPresenter decisionWindow = null,
            PhaseBannerPresenter phaseBanner = null)
        {
            this.panelRefreshFailed = panelRefreshFailed;
            this.decisionWindow = decisionWindow;
            this.heroOverviews = heroOverviews;
            this.kinetics = kinetics;
            this.attackSpotlight = attackSpotlight;
            this.matchLog = matchLog;
            this.phaseBanner = phaseBanner;
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
            if (decisionWindow != null)
            {
                decisionWindow.SessionProjectionChanged += Changed;
            }
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
            // Milestone 8.46c, immediately before the strip it takes the decision families from: the
            // strip asks the window what it is drawing while it rebuilds, so the window has to have
            // observed this projection first. Isolated like every other panel.
            RefreshPanel("decision-window", () => decisionWindow?.Refresh(), failed, errors);
            RefreshPanel("round", round.Refresh, failed, errors);
            RefreshPanel("undo-restart", () => undoRestart?.Refresh(), failed, errors);
            RefreshPanel("diagnostics", () => diagnostics?.Refresh(), failed, errors);
            RefreshPanel("minion-choice", minionChoice.Refresh, failed, errors);
            RefreshPanel("hero-overviews", () => heroOverviews?.Refresh(), failed, errors);
            // Milestone 8.35z12, immediately before the kinetics it paces: the spotlight
            // decides whether that panel plays this projection's cues now or holds them until the
            // camera has arrived, so it must observe this projection first.
            RefreshPanel("attack-spotlight", () => attackSpotlight?.Refresh(), failed, errors);
            // Last, and isolated like every other panel: a kinetic is a flourish, and it must be the
            // one thing on this list that can never cost a player a fact on the surfaces above it.
            RefreshPanel("kinetics", () => kinetics?.Refresh(), failed, errors);
            RefreshPanel("match-log", () => matchLog?.Refresh(), failed, errors);
            RefreshPanel("phase-banner", () => phaseBanner?.Refresh(), failed, errors);

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
