using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.46c. The two states the decision window has.
    ///
    /// <para><see cref="Expanded"/> covers the board only - the card or cards in question, one control
    /// per choice, its text saying what happens when pressed. <see cref="Docked"/> is a slim bar in the
    /// round dock's own rectangle beside the hand: the question, a running count where one applies, and
    /// at most one verb. A button-answered decision opens expanded; a board-answered one opens docked;
    /// the player may toggle either way, and every NEW decision re-decides the state rather than
    /// remembering the toggle.</para>
    /// </summary>
    public enum DecisionWindowState
    {
        Expanded = 0,
        Docked = 1
    }

    /// <summary>
    /// Milestone 8.46c. Which decision the window is drawing. It is the window's own vocabulary rather
    /// than a pending-family id, because two authority families share one window shape (Defense and
    /// removal prevention) and one family splits into five by what it is asking about (an effect
    /// choice's subject), which is exactly what decides whether the window opens expanded or docked.
    /// </summary>
    public enum DecisionWindowFamily
    {
        None = 0,
        MinionBattleRemoval = 1,
        TiedInitiative = 2,
        Defense = 3,
        RemovalPrevention = 4,
        Mode = 5,
        Number = 6,
        Colour = 7,
        OptionalStep = 8,
        CardChoice = 9,
        Direction = 10,
        PieceOrSpace = 11,
        Correction = 12,
        HeroRespawn = 13,

        /// <summary>
        /// Milestone 8.46g. Rulebook p.15's Upgrade a Card, moved here mid-playtest on 2026-09-18 in
        /// the user's own words: "players want the same treatment for level up screen, to be
        /// minimisable. So we need to put it in new thing you made, and show all information that we
        /// show now." It is the one family whose facts are not read from a pending choice but from the
        /// prompt the strip already built - <c>RoundActionPresenter.CreateSystemTransitionPrompt</c> -
        /// so the window and the strip cannot disagree about what the pause is offering.
        ///
        /// <para>The OTHER system-transition kind, p.8's blocked-Spawn Lane Push placement, is
        /// answered by clicking a lit space and stays where it is.</para>
        /// </summary>
        LevelUp = 14,

        /// <summary>Four printed rune glyphs dragged onto four numbered turn-card slots.</summary>
        RuneSlots = 15
    }

    /// <summary>
    /// Milestone 8.46c. What one control in the window is for. The role is stated rather than inferred
    /// from the label, because the labels are content vocabulary now and a surface must never have to
    /// read one to know what it is drawing.
    /// </summary>
    public enum DecisionWindowControlRole
    {
        /// <summary>One of the choices: an arm, a value, a card, a candidate, a verb.</summary>
        Answer = 0,

        /// <summary>A chosen control pressed again, taking that selection back.</summary>
        Deselect = 1,

        /// <summary>`DONE` - drawn only where the choice admits several, never for an exactly-one.</summary>
        Confirm = 2,

        /// <summary>The small corner answer: decline, resolve without, not yet.</summary>
        Corner = 3
    }

    /// <summary>
    /// Milestone 8.46c. One control the decision window draws, bound to the exact command the standing
    /// contract published - never one composed here. Exactly one of <see cref="Command"/> and
    /// <see cref="AgreementCommand"/> is non-null on an answerable control; both are null on a control
    /// drawn unavailable, which is what keeps "greyed" a structural fact rather than a rendering
    /// convention (the same rule <c>UnavailableEffectChoiceViewModel</c> has followed since 6.5ac).
    /// </summary>
    public sealed class DecisionWindowControlViewModel
    {
        public DecisionWindowControlViewModel(
            string label,
            DirectorCommand command = null,
            TeamAgreementCommand agreementCommand = null,
            DecisionWindowControlRole role = DecisionWindowControlRole.Answer,
            TrayCardViewModel card = null,
            string valueChip = null,
            string detail = null,
            CardColor swatch = CardColor.None,
            CardShineSlot shineSlot = CardShineSlot.None,
            bool isPrimary = false,
            bool isChosen = false,
            string unavailableReason = null,
            HeroId hero = default,
            SystemTransitionOptionViewModel upgradeOption = null,
            DefenseBreakdownViewModel defenseBreakdown = null,
            int runeOrdinal = 0,
            int turnSlotIndex = 0,
            SpellCardViewModel spell = null)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A decision control requires a label.", nameof(label))
                : label;
            if (command != null && agreementCommand != null)
            {
                throw new ArgumentException(
                    "A decision control answers through one contract, not two.", nameof(command));
            }
            if ((runeOrdinal == 0) != (turnSlotIndex == 0) ||
                runeOrdinal < 0 || runeOrdinal > 4 ||
                turnSlotIndex < 0 || turnSlotIndex > 4)
            {
                throw new ArgumentException(
                    "A rune placement names both a glyph and a numbered turn slot.");
            }
            RuneOrdinal = runeOrdinal;
            TurnSlotIndex = turnSlotIndex;

            if (unavailableReason != null && (command != null || agreementCommand != null))
            {
                throw new ArgumentException(
                    "A control drawn unavailable carries no command at all, so a click cannot reach " +
                    "the session by any route.",
                    nameof(unavailableReason));
            }

            Command = command;
            AgreementCommand = agreementCommand;
            Role = role;
            Card = card;
            ValueChip = valueChip;
            Detail = detail;
            Swatch = swatch;
            ShineSlot = shineSlot;
            IsPrimary = isPrimary;
            IsChosen = isChosen;
            UnavailableReason = unavailableReason;
            Hero = hero;
            UpgradeOption = upgradeOption;
            DefenseBreakdown = defenseBreakdown;
            if (spell != null && card != null)
            {
                throw new ArgumentException("A control is one face: a card or a spell, never both.", nameof(spell));
            }

            Spell = spell;
        }

        /// <summary>
        /// Milestone 9.30h. The spell face this control IS - a "Choose and cast one" bullet, or one of
        /// Find Familiar's "remove up to three other spells" - or null for every other control
        /// (approved mockup A). <see cref="Label"/> is then the face's caption.
        /// </summary>
        public SpellCardViewModel Spell { get; }

        /// <summary>
        /// Milestone 8.46g. Rulebook p.15's own upgrade option, when this control IS one - and
        /// <c>null</c> for every other control in the window.
        ///
        /// <para>It carries the option WHOLE rather than copied field by field, for the reason the
        /// block was authorized with: "show all information that we show now". The strip's prompt is
        /// the only place those facts are derived, so the item this choice gains, the card it sets
        /// aside, the pair it belongs to and the colour it is are all read from the same object the
        /// strip reads, and a window that disagreed with the strip about any of them would have to
        /// have been built from a second derivation. There is not one.</para>
        /// </summary>
        public SystemTransitionOptionViewModel UpgradeOption { get; }

        /// <summary>
        /// Milestone 8.58. The per-card Defense breakdown this control has on HOVER, and null for
        /// every control that is not a Defense offer.
        ///
        /// <para>The user, on seeing the same arithmetic printed inline in the panel: "Remove it and
        /// keep the old mouseover when mousing over the defense cards." So the window carries the
        /// strip's own <see cref="DefenseBreakdownViewModel"/> - the SAME object
        /// <c>RoundActionPresenter.CreateDefenseBreakdown</c> builds for the strip - and the HUD opens
        /// it on the one breakdown layer that has always drawn it, rather than the window growing a
        /// second reading of the same preview.</para>
        /// </summary>
        public DefenseBreakdownViewModel DefenseBreakdown { get; }

        /// <summary>
        /// The hero this control is ABOUT, when it is about one - a tied Initiative's candidate.
        /// Pointing at such a control cues that hero where they stand, which is the affordance the
        /// strip's own preference row had and this window inherits rather than loses.
        /// </summary>
        public HeroId Hero { get; }

        /// <summary>The words on the control - the consequence, never the mechanism.</summary>
        public string Label { get; }

        public DirectorCommand Command { get; }

        /// <summary>A team agreement's own contract, which is not a <see cref="DirectorCommand"/>.</summary>
        public TeamAgreementCommand AgreementCommand { get; }

        public DecisionWindowControlRole Role { get; }

        /// <summary>The whole printed face this control is, or null when it is a worded control.</summary>
        public TrayCardViewModel Card { get; }

        /// <summary>The per-card fact beneath a face: `DEFENSE 2`, `BLOCK - CONDITION MET`.</summary>
        public string ValueChip { get; }

        /// <summary>A second, quieter line - a price, a range, a teammate's statement.</summary>
        public string Detail { get; }

        /// <summary>The printed colour this control NAMES, per milestone 8.37's swatch rule.</summary>
        public CardColor Swatch { get; }

        /// <summary>Which printed badge a card-faced Defense offer would defend with.</summary>
        public CardShineSlot ShineSlot { get; }

        /// <summary>Whether this is the window's one leading verb.</summary>
        public bool IsPrimary { get; }

        /// <summary>Whether this control's candidate is already part of the standing selection.</summary>
        public bool IsChosen { get; }

        /// <summary>Printed glyph ordinal, 1-4, when this control places a rune.</summary>
        public int RuneOrdinal { get; }

        /// <summary>Numbered turn-card slot, 1-4, for the paired rune placement.</summary>
        public int TurnSlotIndex { get; }

        /// <summary>Why this control is drawn and refused, or null when it answers.</summary>
        public string UnavailableReason { get; }

        public bool IsAnswerable => Command != null || AgreementCommand != null;
    }

    /// <summary>
    /// Milestone 8.46c. A card the window shows but does not offer: the attacker's played card beside a
    /// Defense, the source card beside its own `Choose`. It carries its own caption so the window never
    /// decodes a printed face to say what the card is doing here.
    /// </summary>
    public sealed class DecisionWindowCardViewModel
    {
        public DecisionWindowCardViewModel(
            TrayCardViewModel card, string caption = null, string detail = null, SpellCardViewModel spell = null)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Caption = caption;
            Detail = detail;
            Spell = spell;
        }

        public TrayCardViewModel Card { get; }

        /// <summary>
        /// Milestone 9.34 corrective. The spell's own printed face when <see cref="Card"/> is a spell -
        /// the attack a Defense is asked about, or the spell whose text raised the question - which the
        /// window draws in place of the hero-card face; <c>null</c> for every other card.
        /// </summary>
        public SpellCardViewModel Spell { get; }

        /// <summary>`ATTACK 7` - what this card is contributing to the question.</summary>
        public string Caption { get; }

        /// <summary>`RANGED` - a second printed fact about the same card, or null.</summary>
        public string Detail { get; }
    }

    /// <summary>
    /// Milestone 8.46c. The standing decision as the one window that asks it: what is being asked, the
    /// cards it is about, one control per answer, and the small corner answer that is not a choice.
    ///
    /// <para>It is built ONLY for the seat that owns the standing decision - team-owned decisions belong
    /// to every seat on the side, and the Director owns them all at once, in which case
    /// <see cref="Title"/> names the side or the hero. Every other viewer gets no window and keeps the
    /// narration line, which is the same ownership reading
    /// <c>RoundActionPresenter.FormatPublicDecisionWait</c> makes.</para>
    /// </summary>
    public sealed class DecisionWindowViewModel
    {
        public DecisionWindowViewModel(
            long version,
            DecisionWindowFamily family,
            DecisionWindowState state,
            string decisionKey,
            string title,
            string eyebrow,
            string question,
            string count = null,
            IEnumerable<DecisionWindowCardViewModel> contextCards = null,
            IEnumerable<DecisionWindowControlViewModel> controls = null,
            DecisionWindowControlViewModel cornerControl = null,
            IEnumerable<string> statements = null,
            bool isMinimisable = true,
            IEnumerable<TrayCardViewModel> handPreview = null)
        {
            if (family == DecisionWindowFamily.None)
            {
                throw new ArgumentException(
                    "A window is built for a decision that is actually standing.", nameof(family));
            }

            Version = version;
            Family = family;
            State = state;
            DecisionKey = string.IsNullOrWhiteSpace(decisionKey)
                ? throw new ArgumentException(
                    "A window names the decision it stands for, so a NEW one can re-decide the state.",
                    nameof(decisionKey))
                : decisionKey;
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("A window states what it is asking.", nameof(title))
                : title;
            Eyebrow = eyebrow ?? string.Empty;
            Question = question ?? string.Empty;
            Count = count;
            ContextCards = Array.AsReadOnly((contextCards ??
                Enumerable.Empty<DecisionWindowCardViewModel>()).ToArray());
            Controls = Array.AsReadOnly((controls ??
                Enumerable.Empty<DecisionWindowControlViewModel>()).ToArray());
            CornerControl = cornerControl;
            Statements = Array.AsReadOnly((statements ?? Enumerable.Empty<string>()).ToArray());
            IsMinimisable = isMinimisable;
            HandPreview = Array.AsReadOnly((handPreview ??
                Enumerable.Empty<TrayCardViewModel>()).ToArray());

            // Milestone 8.46c, the cross-cutting rule the user set for the whole series: NO UNDO
            // inside the window, ever. The seat undo square above it covers a wrong press inside a
            // turn, and every answer outside a turn is final on press or restatable already. The rule
            // is structural rather than asserted: a rewind is not a DirectorCommand at all (it is
            // `IMatchClient.SubmitUndo`, and `DirectorCommandKind` has no member for it), so this type
            // cannot carry one however it is composed. A Deselect is not an undo - it is the same
            // choice pressed again, which is how a choose-several selection is taken back.
        }

        public long Version { get; }
        public DecisionWindowFamily Family { get; }
        public DecisionWindowState State { get; }

        /// <summary>
        /// The standing decision's own identity. The window's state is re-decided from the family
        /// whenever this changes, and a manual minimise or expand survives exactly as long as it does.
        /// </summary>
        public string DecisionKey { get; }

        /// <summary>The question in three or four upper-case words: `WHO ACTS FIRST?`, `RELOAD!`.</summary>
        public string Title { get; }

        /// <summary>Where the question comes from: `TIED INITIATIVE · RED TEAM`.</summary>
        public string Eyebrow { get; }

        /// <summary>The question as a sentence, in the player's own language.</summary>
        public string Question { get; }

        /// <summary>`1 of 2 chosen`, where a bound applies; null where none does.</summary>
        public string Count { get; }

        public IReadOnlyList<DecisionWindowCardViewModel> ContextCards { get; }
        public IReadOnlyList<DecisionWindowControlViewModel> Controls { get; }

        /// <summary>The small corner answer: `TAKE THE HIT`, `DON'T RESPAWN`, `NOT YET`.</summary>
        public DecisionWindowControlViewModel CornerControl { get; }

        /// <summary>What teammates have already said: `Tali prefers Sabina`, `Arien is ready`.</summary>
        public IReadOnlyList<string> Statements { get; }

        /// <summary>Whether the player may toggle this window between its two states.</summary>
        public bool IsMinimisable { get; }

        /// <summary>
        /// Milestone 8.46g. The deciding seat's OWN current hand, shown above the offer and answering
        /// nothing - the user's directive of 2026-09-07, "when leveling. Show current hand (all 5
        /// cards) above the offer", carried over from the strip unchanged.
        ///
        /// <para>Empty for every family but <see cref="DecisionWindowFamily.LevelUp"/>. Milestone 8.58
        /// made it the LEVELLING hero's hand rather than the viewer's, so the Director sees it too: the
        /// hand shown is the hand being chosen for, the title already names whose upgrade it is, and a
        /// Director screen drawing the offer with this row missing left half the panel empty.</para>
        /// </summary>
        public IReadOnlyList<TrayCardViewModel> HandPreview { get; }
    }

    /// <summary>
    /// Milestone 8.46c. The passive port for the decision window. A null model means no decision this
    /// viewer owns is standing, and therefore that no window may be shown.
    /// </summary>
    public interface IDecisionWindowView
    {
        void Render(DecisionWindowViewModel model);
    }
}
