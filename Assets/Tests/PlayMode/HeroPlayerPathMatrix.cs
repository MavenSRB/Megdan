using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// One behaviourally distinct thing a player does, or reads, on the real surface. Milestone 8.20a.
    ///
    /// <para>The gate in <c>Testing/POLICY.md</c> asks for "every behaviorally distinct player
    /// interaction shape" a face introduces, and the word doing the work there is <em>shape</em>: two
    /// cards that both ask a player to click a unit on the board introduce one shape between them, and
    /// the page says so outright - "Play Mode need not duplicate permutations that change no input,
    /// presentation, or transition shape". So these members name input and reading shapes, never
    /// cards. Raging Stream and Dangerous Current print different numbers and introduce the same four
    /// shapes; that is the point.</para>
    ///
    /// <para><b>Nothing here is declared by hand for a card.</b>
    /// <see cref="HeroPlayerPathMatrix.ShapesIntroducedBy"/> derives a face's set from the content
    /// itself - action kind, origin, range, card-text timing, effect-node kind, failure policy,
    /// capability identity, progression profile - and the guard compares that derivation against the
    /// row. A hand-written row that drifts from the printed face fails, and so does a face nobody
    /// wrote a row for. The declaration is therefore a claim about coverage, not a second copy of the
    /// content.</para>
    /// </summary>
    internal enum PlayerInteractionShape
    {
        /// <summary>Picking a card up in the tray and pressing CONFIRM FACEDOWN, through p.7's reveal.</summary>
        CommitAndReveal,

        /// <summary>A printed non-ranged Attack primary, whose target is clicked on the board.</summary>
        AdjacentAttackTarget,

        /// <summary>A printed ranged Attack primary, whose target is clicked at p.9's printed Range.</summary>
        RangedAttackTarget,

        /// <summary>A printed Skill primary, started from its own control.</summary>
        PrimarySkillControl,

        /// <summary>A printed Movement primary - p.11's card, not p.10's secondary icon.</summary>
        PrimaryMovementDestination,

        /// <summary>The secondary Movement icon every hand's cards print, walked by board clicks.</summary>
        SecondaryMovementDestination,

        /// <summary>
        /// p.14's Primary Defense Value: a Defense printed in the primary slot, offered as a whole
        /// face in the defender's prompt. Arien's three Duelist cards are the first in this build.
        /// </summary>
        PrimaryDefenseOffer,

        /// <summary>
        /// p.14's Block card, offered as a whole face in the same prompt and resolving as the page's
        /// other Defense: "a Block succeeds regardless of the Attack Value" when its printed condition
        /// is met, and the defender is defeated when it is not. Milestone 8.20b separated it from the
        /// Primary Defense Value above because the two are one input and two transitions - the Value
        /// is compared and the Block is not - and a hero printing a Block would otherwise inherit a
        /// journey in which nobody was ever defeated by answering. Wasp's three Green cards are the
        /// only Blocks in this build.
        /// </summary>
        BlockDefenseOffer,

        /// <summary>The ordinary numeric secondary Defense icon, offered in the same prompt.</summary>
        SecondaryDefenseOffer,

        /// <summary>p.14's pause before the attack is checked, drawn as a real prompt.</summary>
        BeforeAttackTextPause,

        /// <summary>p.14's pause after the attack has been applied.</summary>
        AfterAttackTextPause,

        /// <summary>p.11's default moment for a Movement card's own text, before the walk.</summary>
        BeforeMovementTextPause,

        /// <summary>
        /// Milestone 8.35z50. p.11's "unless it says otherwise" moment for a Movement
        /// card's own text, after the walk - Ursafar's three swap faces, "after movement, you may
        /// swap...".
        ///
        /// <para>It is a separate shape from <see cref="BeforeMovementTextPause"/> for the same reason
        /// <see cref="AfterAttackTextPause"/> is separate from <see cref="BeforeAttackTextPause"/>: the
        /// two sides of one primary action are two different moments a player meets, holding different
        /// board state each time - p.14's consequences have or have not happened yet for an Attack, and
        /// the walk has or has not happened yet for a Movement. It is not
        /// <see cref="ActionTextPlacementChoice"/> either, because the page decides this side rather
        /// than the player: Ursafar's Blue text carries no choice of which side to run on, only
        /// Xargatha's three Green Movements do.</para>
        /// </summary>
        AfterMovementTextPause,

        /// <summary>
        /// Milestone 8.24. The player choosing which side of their own primary action the card's text
        /// runs on - Xargatha's Charm, Control and Dominate, "before or after movement".
        ///
        /// <para>It is a separate shape from <see cref="BeforeMovementTextPause"/> rather than a
        /// variant of it, because the two are different things happening to the player. That one is a
        /// moment the page picks and the text simply arrives at; this is a question with two answers
        /// and no way to decline, and answering it differently changes the board. p.11 draws the line
        /// itself: the text comes into effect before movement "unless it says otherwise", and these
        /// three faces are the first verified content to say otherwise.</para>
        /// </summary>
        ActionTextPlacementChoice,

        /// <summary>Card text that reaches the table only because the card was discarded to defend.</summary>
        DefenseTextApplication,

        /// <summary>A printed "may" or "up to" answered by a confirm or a decline control.</summary>
        ConfirmOrDecline,

        /// <summary>A printed "may repeat", answered on its own control.</summary>
        RepeatDecision,

        /// <summary>A hero or minion chosen by clicking the piece where it stands.</summary>
        BoardUnitSelection,

        /// <summary>An empty space chosen by clicking the board.</summary>
        BoardSpaceSelection,

        /// <summary>A card chosen from a hand, on its whole face, under 8.15a's contract.</summary>
        HandCardSelection,

        /// <summary>A printed distance range answered by its own numeric controls.</summary>
        PushDistanceChoice,

        /// <summary>
        /// Milestone 8.35z34. Which minion type comes back, answered on a control naming the
        /// miniatures the supply holds - Dodger's Necromancy and Necromastery, once her Ultimate has
        /// granted the Spawn point she is respawning into.
        ///
        /// <para><b>It is its own shape because its subject is not on the board.</b> Every other
        /// selection this table names points at a piece standing somewhere or a space a player can
        /// click; this one picks among miniatures in the supply, which is a control and not a board
        /// click. Classifying it as <see cref="BoardUnitSelection"/> would claim a journey in which a
        /// player clicks a piece, and there is no piece to click.</para>
        ///
        /// <para><b>Why it is derived from the capability rather than from the node</b>, unlike the
        /// push distance and the named Move destination beside it. Those two are read off a literal the
        /// card declared, so the derivation can tell in advance whether a player is asked. Whether this
        /// question is asked depends on live board state - RULING 48 gives the type to p.19's own
        /// eligibility count, and one eligible type opens no control at all - so no reading of the
        /// definition can decide it. A capability that may ask therefore declares the shape, and the
        /// hero who prints it owes a journey where the question is actually put.</para>
        /// </summary>
        RespawnedMinionTypeChoice,

        /// <summary>
        /// Milestone 8.23. A printed quantity of a resource, answered by its own numeric controls -
        /// Tigerclaw's three thieving Greens, "Take 1 coin" and Master Thief's "Take 1 or 2 coins".
        ///
        /// <para>It is its own shape rather than <see cref="PushDistanceChoice"/> because the two
        /// choose different things: one is how far a piece travels and the other is how much of a
        /// hero's pile changes hands, and a Green card claiming a push distance would name a control
        /// about a displacement it never performs. They share a control kind, which is exactly the
        /// relation <see cref="AdjacentAttackTarget"/> and <see cref="RangedAttackTarget"/> already
        /// have.</para>
        ///
        /// <para>Every transfer opens it, including the two faces printing one amount: the candidate
        /// list is the amounts the source can actually pay, so a card naming one still draws that one
        /// as a control the player presses.</para>
        /// </summary>
        ResourceAmountChoice,

        /// <summary>
        /// Milestone 8.21. Rulebook p.10's modal form - "Choose one —" followed by bullets, of which
        /// the chosen one applies and the rest are ignored.
        ///
        /// <para>It is its own shape rather than one of the two decisions above because it is neither.
        /// A confirm-or-decline offers one thing and takes an answer of yes or no; this offers two
        /// printed sentences and the one chosen goes on to ask its own questions - Brogan's Throwing
        /// Axe takes a target beside him on one bullet and spends a card for range on the other.</para>
        /// </summary>
        ModalBulletChoice,

        /// <summary>An Active effect the hero raised, readable on the tray or the rail while it runs.</summary>
        ActiveEffectReading,

        /// <summary>
        /// An Active effect of this hero's that forbids an enemy an activity they could otherwise
        /// perform, read as the greyed control the enemy is actually shown. p.17's restrictions are
        /// only honest if the player who is stopped can see they are stopped.
        ///
        /// <para>Milestone 8.20b narrowed this to restrictions on what the affected enemy <em>does</em>
        /// - the ones a player meets as their own control. A restriction on what may be done <em>to</em>
        /// a unit is enforced by leaving that piece out of the offering hero's candidates, which draws
        /// no control at all; the derivation below records why.</para>
        /// </summary>
        OpponentRestrictionReading,

        /// <summary>An Active effect that outlives the turn that raised it and is still read next turn.</summary>
        RoundLongActiveEffectReading,

        /// <summary>p.9's item I on a Defense: the minion contributions this face suppresses.</summary>
        MinionDefenseSuppressionReading,

        /// <summary>p.16's unlocked Ultimate, drawn where a player can see it.</summary>
        UltimateRailVisibility,

        /// <summary>An Ultimate whose printed trigger stops the game and asks the controller something.</summary>
        UltimateTriggerPause,

        /// <summary>p.16's item, gained from the untaken sibling and readable on the dashboard.</summary>
        ItemVisibilityAndUse,

        /// <summary>
        /// Milestone 8.37. An unconstrained choice among p.17's six directions, answered
        /// on its own control rather than by clicking anything on the board - Tali's Cold Snap,
        /// Snowstorm and Blizzard, "Choose a direction."
        ///
        /// <para>It is its own shape rather than <see cref="BoardSpaceSelection"/> because nothing about
        /// it is a space: the six candidates exist unconditionally, wherever the actor stands and
        /// whatever the board looks like, which no board click could be - clicking implies a place, and
        /// this names an orientation.</para>
        /// </summary>
        DirectionChoice,

        /// <summary>
        /// Milestone 8.37. A hero mandatorily naming one of p.9's six printed colours,
        /// answered on its own control - Commune with Spirits' naming step and Reign of Winter's
        /// "colour of your choice" discard.
        ///
        /// <para>It is its own shape rather than <see cref="HandCardSelection"/> because the colour
        /// named is independent of any card in any hand - Arty's own ruling is explicit that the naming
        /// step must succeed even with an empty hand - so a row claiming a hand-card control would name
        /// a candidate set the printed sentence does not read from.</para>
        /// </summary>
        ColorNamingChoice,


        // -------------------------------------------------------------------------------------
        // Component shapes. These belong to the hero rather than to any one face, and the gate
        // names every one of them. They are required of every hero's matrix by
        // HeroPlayerPathMatrix.RequiredComponentShapes rather than derived from content, because
        // their subject is the piece and the session, not the printed set.
        // -------------------------------------------------------------------------------------

        /// <summary>The official token stands at the configured spawn, as this hero and only once.</summary>
        HeroPieceSpawnAndIdentity,

        /// <summary>A camera ray on the piece points the tray at its hero.</summary>
        HeroPiecePointerSelection,

        /// <summary>The piece follows its hero's authoritative space after a move.</summary>
        HeroPieceMovement,

        /// <summary>p.13's Defeat removes the piece, and p.13's respawn puts it back.</summary>
        HeroPieceDefeatAndRespawn,

        /// <summary>A pause survives a snapshot restore and is answered afterwards on the same surface.</summary>
        SnapshotRestoreContinuation,

        /// <summary>The same pause survives a retained-prefix Undo and is answered again.</summary>
        RetainedPrefixUndo,

        /// <summary>The accepted commands replay into the same authoritative state.</summary>
        ReplayReconstruction,

        /// <summary>RESTART returns the initial state with no accepted history and a redrawn board.</summary>
        RestartReconstruction,

        /// <summary>
        /// A rebuilt presentation reconstructs hero, hand, prompt and cues from the current
        /// projection alone - no stale view, no duplicate piece.
        /// </summary>
        PresentationRebuild,

        /// <summary>
        /// Milestone 8.20c: every verified face this hero prints, drawn at the validated player
        /// viewport, is exactly <see cref="GuardsDigital.Presentation.Match.CardFootprint"/> and shows
        /// its title, values, action, rules text and history whole.
        ///
        /// <para>A reading rather than an input, on the same terms as the Active-effect and Ultimate
        /// readings above: what it proves is what a player sees. It is a component shape because its
        /// subject is the whole printed set at once - "no face of this hero resizes a card" is not a
        /// claim any one face can make - and it is required of every hero rather than derived from
        /// content, because a hero whose longest sentence does not fit is exactly the case that must
        /// fail. The check is <c>StandardCardFootprintCheck</c>, which enumerates the hero's own
        /// content, so the coverage cannot be narrowed by the row that names it.</para>
        /// </summary>
        StandardCardFootprintConformance,

        /// <summary>
        /// Milestone 8.21a: this hero's coin balance, drawn on their own side rail, following every
        /// authoritative grant and spend and standing clear of every other row on that rail.
        ///
        /// <para>A reading rather than an input, on the terms the two above are. It is a component
        /// shape because its subject is the rail rather than any printed face - no card of any hero
        /// puts the balance there - and it is deliberately <em>not</em> in
        /// <see cref="HeroPlayerPathMatrix.RequiredComponentShapes"/>, which every hero owes. One
        /// shared builder draws every rail from one repeated view model, so a second hero's coin
        /// count is the same code drawing the same row with a different number in it, and the gate's
        /// own sentence permits exactly that: Play Mode need not duplicate permutations that change
        /// no input, presentation, or transition shape. What is hero-specific about a rail - the
        /// piece, the identity, the items - already has its own shapes above.</para>
        /// </summary>
        HeroCoinBalanceReading,

        /// <summary>
        /// Milestone 8.23a: a rulebook p.18 marker this hero holds, drawn on their own rail together
        /// with the value channels holding it changes.
        ///
        /// <para>A reading rather than an input, on the terms the readings above are - and the one
        /// this table was carrying as an explicit debt. Milestone 8.23 gave Tigerclaw the first
        /// verified marker in the build and could draw it nowhere, so his two Poison capabilities
        /// mapped to no shape at all rather than to a shape no player could perform. The two entries
        /// in <see cref="CapabilityShapes"/> name this now, which is what closes the debt: a hero
        /// whose content attaches a marker owes a journey where a player reads one.</para>
        ///
        /// <para><b>It is derived from content, unlike the component shapes above it.</b> The subject
        /// is a capability a hero's cards actually print, so a hero who gives no marker owes nothing
        /// here and a hero who does cannot avoid it - which is the opposite of
        /// <see cref="HeroCoinBalanceReading"/>, whose subject is a rail row every hero has. It is
        /// therefore deliberately <em>not</em> in
        /// <see cref="HeroPlayerPathMatrix.RequiredComponentShapes"/>.</para>
        ///
        /// <para>The reading is proved on the <em>holder's</em> rail, which is normally the enemy's:
        /// p.18 gives markers to other heroes, a marker property affects its holder where a token
        /// property affects its controller, and the whole point of drawing one is that the player
        /// whose values dropped can see why.</para>
        /// </summary>
        HeldMarkerReading,

        /// <summary>
        /// Milestone 8.40k. A card turned face up for the WHOLE table by card text, mid-resolution -
        /// Bain's three guessing Blues, "then reveal the card".
        ///
        /// <para><b>It is a reading rather than an input</b>, on exactly the terms
        /// <see cref="HeldMarkerReading"/> is: nobody is asked anything and
        /// <c>synthetic-reveal-chosen-card@1</c> opens no control, but what it leaves behind is a
        /// thing every seat must be able to see. The capability's own remarks say so - the handler
        /// reports a <c>RevealedCard</c>, the backend writes one seat-public log row naming hero and
        /// card, and <c>SeatProjection.IsPublic</c>'s hand arm then makes that card nameable in every
        /// seat.</para>
        ///
        /// <para><b>Why it is its own shape and not folded into the hand-card selection beside
        /// it.</b> The selection is answered by ONE seat and is hidden from the rest until this
        /// moment; the reveal is the moment the other seats learn the answer. A row claiming only the
        /// selection would be satisfied by a journey driven entirely from the chooser's seat, which is
        /// precisely the journey that cannot tell a working reveal from a reveal that never reached
        /// anybody else - so the shape exists to force a journey that reads a LOSING seat's
        /// projection. Milestone 8.23a added <see cref="HeldMarkerReading"/> for the same reason and
        /// recorded the same sentence: a hero whose content reveals a card owes a journey where
        /// somebody else reads one.</para>
        /// </summary>
        PublicCardRevealReading
    }

    /// <summary>
    /// One shape, and the Play Mode journey that drives it through real pointer events or a camera
    /// ray. Milestone 8.20a.
    ///
    /// <para>Pairing the two is what makes the matrix impossible to fill in dishonestly: there is no
    /// way to declare that a face asks for a board selection without naming the journey where a
    /// player makes one. A row's shapes are read off its evidence rather than listed beside it, so
    /// "declared but unproved" is not a state this table can be in.</para>
    /// </summary>
    internal readonly struct ShapeEvidence
    {
        public ShapeEvidence(PlayerInteractionShape shape, string journeyMethodName)
        {
            Shape = shape;
            Journey = journeyMethodName;
        }

        public PlayerInteractionShape Shape { get; }

        /// <summary>The journey method's name, resolved against this assembly by the guard.</summary>
        public string Journey { get; }
    }

    /// <summary>
    /// One row of the gate's mechanical completeness matrix: one printed face, or one hero-owned
    /// component, and the evidence that proves it through the input route a player uses.
    /// </summary>
    internal sealed class PlayerPathRow
    {
        private PlayerPathRow(
            string subjectId,
            string displayName,
            bool isComponent,
            ShapeEvidence[] evidence,
            string[] authoritativeScenarios,
            string absenceReason)
        {
            SubjectId = subjectId;
            DisplayName = displayName;
            IsComponent = isComponent;
            Evidence = Array.AsReadOnly(evidence ?? Array.Empty<ShapeEvidence>());
            Shapes = Array.AsReadOnly(Evidence.Select(item => item.Shape).Distinct().ToArray());
            Journeys = Array.AsReadOnly(Evidence
                .Select(item => item.Journey)
                .Distinct(StringComparer.Ordinal)
                .ToArray());
            AuthoritativeScenarios =
                Array.AsReadOnly(authoritativeScenarios ?? Array.Empty<string>());
            AbsenceReason = absenceReason;
        }

        /// <summary>One shape and the journey that proves it, for a row's evidence list.</summary>
        public static ShapeEvidence Prove(PlayerInteractionShape shape, string journeyMethodName) =>
            new ShapeEvidence(shape, journeyMethodName);

        /// <summary>A printed face's row: its subject is a card definition identity.</summary>
        public static PlayerPathRow Face(
            CardDefinitionId cardId,
            string displayName,
            ShapeEvidence[] evidence,
            string[] authoritativeScenarios) =>
            new PlayerPathRow(
                cardId.Value, displayName, false, evidence, authoritativeScenarios, null);

        /// <summary>
        /// A printed face that no player can reach in this build, with the reason recorded on the row
        /// rather than left as an empty cell. The guard admits it only when the content itself says
        /// the same thing - the card is returned to the box at setup, or its text composition carries
        /// a named deferral - so a row cannot excuse itself.
        /// </summary>
        public static PlayerPathRow UnreachableFace(
            CardDefinitionId cardId, string displayName, string absenceReason) =>
            new PlayerPathRow(cardId.Value, displayName, false, null, null, absenceReason);

        /// <summary>A hero-owned component's row: the piece, the item track, the tokens it prints.</summary>
        public static PlayerPathRow Component(
            string componentId,
            string displayName,
            ShapeEvidence[] evidence,
            string[] authoritativeScenarios) =>
            new PlayerPathRow(componentId, displayName, true, evidence, authoritativeScenarios, null);

        /// <summary>
        /// A component this hero's printed set does not have. The guard derives the same absence from
        /// the content and fails if a later face contradicts the row, which is what stops "this hero
        /// prints no token" from surviving the hero who does.
        /// </summary>
        public static PlayerPathRow AbsentComponent(
            string componentId, string displayName, string absenceReason) =>
            new PlayerPathRow(componentId, displayName, true, null, null, absenceReason);

        public string SubjectId { get; }
        public string DisplayName { get; }
        public bool IsComponent { get; }

        /// <summary>Each shape this row covers, paired with the journey that drives it.</summary>
        public IReadOnlyList<ShapeEvidence> Evidence { get; }

        public IReadOnlyList<PlayerInteractionShape> Shapes { get; }

        /// <summary>
        /// The Play Mode journey methods that drive this row's shapes through real pointer events or
        /// camera rays. Named as method names in this assembly and resolved by reflection, so a
        /// renamed or deleted journey fails the guard instead of leaving a row pointing at nothing.
        /// </summary>
        public IReadOnlyList<string> Journeys { get; }

        /// <summary>
        /// The source-backed authoritative scenarios this row rests on, as
        /// <c>Fixture.Case</c> names in the Edit Mode assembly. The gate requires the link; the guard
        /// resolves it when that assembly is loaded, which it is whenever these tests run in the
        /// Editor.
        /// </summary>
        public IReadOnlyList<string> AuthoritativeScenarios { get; }

        /// <summary>Why this row has no journey, or null when it has one.</summary>
        public string AbsenceReason { get; }

        public bool IsDeclaredAbsence => AbsenceReason != null;

        public override string ToString() => $"{SubjectId} ({DisplayName})";
    }

    /// <summary>
    /// The permanent hero player-path completion gate's mechanical completeness matrix, as machinery
    /// rather than as one hero's table. Milestone 8.20a.
    ///
    /// <para>The gate is permanent and applies to "any hero first becomes configured and playable",
    /// so the part that can be shared is: what a face introduces, what every hero owes for its own
    /// piece, and what makes a row's evidence real. Only the rows themselves are per hero. Milestone
    /// 8.20a built this for Arien and milestone 8.20b brought Wasp to the same table.</para>
    ///
    /// <para><b>Why a derivation and not a checklist.</b> A hand-maintained list of faces answers the
    /// question "did somebody remember to add a line", which is the question milestone 6.1k built the
    /// IL scanner to stop trusting. The derivation below answers "what does this printed content
    /// actually ask a player to do", and the row has to agree with it. Adding a nineteenth face, or
    /// giving an existing face a repeat it did not have, fails here on the next run without anybody
    /// choosing to check.</para>
    /// </summary>
    internal sealed class HeroPlayerPathMatrix
    {
        /// <summary>
        /// The component shapes every hero's matrix must cover, whatever they print. These come
        /// straight from the gate's own sentences - initial hero spawn and identity, Defeat/respawn,
        /// restore, Undo, replay, restart and presentation rebuild - and are required rather than
        /// derived because their subject is the configured session and the board piece, which every
        /// hero has and no hero's cards describe.
        /// </summary>
        public static readonly PlayerInteractionShape[] RequiredComponentShapes =
        {
            PlayerInteractionShape.HeroPieceSpawnAndIdentity,
            PlayerInteractionShape.HeroPiecePointerSelection,
            PlayerInteractionShape.HeroPieceMovement,
            PlayerInteractionShape.HeroPieceDefeatAndRespawn,
            PlayerInteractionShape.SnapshotRestoreContinuation,
            PlayerInteractionShape.RetainedPrefixUndo,
            PlayerInteractionShape.ReplayReconstruction,
            PlayerInteractionShape.RestartReconstruction,
            PlayerInteractionShape.PresentationRebuild,

            // Milestone 8.20c. Required of every hero, including one added after the mapped pitch
            // roster: the standard card footprint is only a standard while the hero who cannot fit it
            // fails here instead of widening it.
            PlayerInteractionShape.StandardCardFootprintConformance
        };

        /// <summary>
        /// Every capability a hero's content may name, classified by what it asks the player for. The
        /// guard fails when a hero's content names a capability that is not in here, which is what
        /// stops a later face from contributing an unproved input shape by being unrecognised: an
        /// unknown capability is a question this table cannot answer, not a capability that asks
        /// nothing.
        /// </summary>
        private static readonly Dictionary<string, PlayerInteractionShape[]> CapabilityShapes =
            new Dictionary<string, PlayerInteractionShape[]>(StringComparer.Ordinal)
            {
                // Selections whose subject stands on the map are answered on the map, under the
                // standing board-first contract, so every one of these is a board click. The roster
                // capabilities milestone 8.19b added carry no `synthetic-` prefix and the general
                // ones do; both spellings are the capability's own identity and neither is guessed.
                ["choose-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                // Milestone 8.35z46. The `synthetic-` sibling of the roster capability above: the same
                // board-click question, asked with the distance as a literal a definition writes rather
                // than a total the roster reads off state. Silverarrow's Disorient, Divert Attention and
                // Lead Astray are the first dealt content to name it - "adjacent" is card text, and the
                // printed-icon selections below cannot express a distance no icon prints.
                ["synthetic-choose-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["behind-target-line"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-range-minion-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-swap-actor"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-place-in-range"] = new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["synthetic-direct-move"] = new[] { PlayerInteractionShape.BoardSpaceSelection },

                // Milestone 8.20b. p.9's printed-icon selections, which differ from one another in
                // which icon they read and which class of piece they offer, and agree in what they ask
                // a player for: the piece, where it stands. Wasp's content is the first to name any of
                // them, and every one is answered by a board click in her own journeys.
                ["synthetic-radius-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-range-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-radius-hero-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-range-hero-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-radius-minion-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["synthetic-hero-adjacent-to-minion"] =
                    new[] { PlayerInteractionShape.BoardUnitSelection },

                // Milestone 8.35z53. The last unclassified member of the printed-icon
                // family, and its absence was an accident of which heroes had been dealt rather than a
                // decision: the written-radius, range-hero, range-unit, radius-hero, radius-minion and
                // range-minion arms have all been here since milestone 8.20b or 8.21, and the
                // RADIUS-UNIT arm is the one no dealt hero had named. Whisper's three Blue displacements
                // name it - "Target an enemy unit in radius occupying a spawn point" - and it asks what
                // its six siblings ask: the piece, where it stands. The `occupiesSpawnPoint` narrowing
                // changes which pieces are offered and not what a player supplies, which is the same
                // candidate-set-versus-shape line milestone 8.35z38 drew for Misa's ring.
                ["synthetic-radius-unit-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                // The written-radius Place, whose destination is a space a player clicks. Its
                // printed-Range sibling `synthetic-place-in-range` is above.
                ["synthetic-place"] = new[] { PlayerInteractionShape.BoardSpaceSelection },

                // p.18's batch Push takes which piece on the board. How far is a second question only
                // when the card printed a range of distances, and that is read off the node rather
                // than off the capability - see NamesMoreThanOnePushDistance.
                ["synthetic-object-push"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                ["synthetic-discard-chosen-card"] = new[] { PlayerInteractionShape.HandCardSelection },

                // Milestone 8.21. Brogan's contracts, split by what each one asks a player for.
                //
                // The charge line asks which space to end on. Milestone 8.35p took its second
                // question away: naming the unit beside that space is now `choose-target` asked after
                // the Move, so the click is the same click on the same board and it is counted where
                // it is now made. The charge faces therefore keep both shapes - the space from here,
                // the unit from `choose-target` - and their derived rows do not move. The
                // unit-valued printed-Range selection is a third printed-icon sibling of the six
                // above and asks what they ask. And a card the actor spends out of their own hand is
                // the same hand-card question milestone 7.1a's discard asks, put to the actor instead
                // of to somebody the card named.
                ["choose-charge-line"] = new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["synthetic-range-unit-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["discard-owned-card"] = new[] { PlayerInteractionShape.HandCardSelection },
                ["retrieve-card"] = new[] { PlayerInteractionShape.HandCardSelection },

                // Consequences, predicates and readings: real work, no player input of their own.
                ["synthetic-defeat-hero"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-defeat-minion"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-is-flagged"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-actor-space"] = Array.Empty<PlayerInteractionShape>(),
                ["spaces-adjacent"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.21's readings and its one silent write. `attacked-target-space` captures
                // where the target stood, `hero-defeated-this-round` and `owned-card-present` are
                // predicates, and `gain-resource` moves coins without asking anybody anything.
                ["attacked-target-space"] = Array.Empty<PlayerInteractionShape>(),
                ["hero-defeated-this-round"] = Array.Empty<PlayerInteractionShape>(),
                ["owned-card-present"] = Array.Empty<PlayerInteractionShape>(),
                ["gain-resource"] = Array.Empty<PlayerInteractionShape>(),
                ["set-attack-target"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.35z45. `attack-sequence` is a DECLARATION rather than a question: each
                // leaf performs nothing and publishes its bullet's printed targeting sentence, and the
                // target is then asked by p.14's own prompt, which the face already declares as its
                // printed Attack shape. The bullet CHOICE is contributed by the enclosing `Choose` as
                // `ModalBulletChoice`, so classifying this leaf as a shape of its own would put a
                // second control on the row where a player is shown one - the same correction
                // milestone 8.24 made for `around-primary-action` and milestone 8.20b for a push
                // distance. The subject narrowing milestone 8.35z45 added changes WHICH candidates
                // that prompt offers, never who is asked or how.
                ["attack-sequence"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.22. Sabina's contracts. The two multi-piece selections are the same
                // board click the single-piece ones are, asked more than once: `choose-target-pair`
                // takes two minions and `choose-target-set` takes up to N of them, and a player names
                // each by clicking where it stands. What differs between them is how many, and that is
                // the selection's own count rather than a different kind of input.
                ["choose-target-set"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["choose-target-pair"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                // Readings and consequences, which ask nobody anything. The first three answer a
                // question about the board or the moment - how many friendly minions are in the disk,
                // whether the Attack's target was adjacent when it landed, whether it is adjacent now
                // - and the last three do work a selection already named the subject of.
                ["presence-count"] = Array.Empty<PlayerInteractionShape>(),
                ["pre-consequence-adjacency"] = Array.Empty<PlayerInteractionShape>(),
                ["target-is-adjacent"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-remove"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-swap"] = Array.Empty<PlayerInteractionShape>(),
                ["synthetic-push-target"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.23. Tigerclaw's five.
                //
                // The through-line is one board click that answers two printed clauses at once: the
                // unit crossed and, by the geometry, the space landed on. A row claiming a separate
                // space selection beside it would name a control no player is ever shown, which is
                // the correction milestone 8.21 made for a Move that names its own destination.
                ["choose-through-target-line"] =
                    new[] { PlayerInteractionShape.BoardUnitSelection },

                // Blend Into Shadows' Place, whose destination is a space a player clicks inside the
                // printed radius - the same input its Range and written-radius siblings above ask for.
                ["synthetic-place-in-radius"] = new[] { PlayerInteractionShape.BoardSpaceSelection },

                // A presence question about the board, answered by the rules rather than by anybody.
                ["adjacent-terrain-presence"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.24. Where the card's own text runs relative to the primary action it
                // encloses - Xargatha's three Green Movements, "before or after movement". It is one
                // decision with two answers and no decline, so it contributes its own shape rather
                // than the confirm-or-decline every optional step already carries.
                ["around-primary-action"] =
                    new[] { PlayerInteractionShape.ActionTextPlacementChoice },

                // p.15's coins, moved by the amount its own numeric controls offer. Who they come
                // from is a separate selection node's board click, so this contributes the number
                // alone.
                ["transfer-resource"] = new[] { PlayerInteractionShape.ResourceAmountChoice },

                // The two Poison supplies. The hero who receives one is named by the selection node
                // in front of them, so the attachment itself asks nobody anything - but what it
                // leaves behind is read, which is milestone 8.23a and the debt milestone 8.23 wrote
                // here. Both entries stood empty while this build drew no held marker anywhere; the
                // shape they name now is the reading on the holder's own rail, and a hero attaching a
                // marker can no longer pass this gate without one.
                ["tigerclaw-give-poison-marker-1"] =
                    new[] { PlayerInteractionShape.HeldMarkerReading },
                ["tigerclaw-give-poison-marker-2"] =
                    new[] { PlayerInteractionShape.HeldMarkerReading },

                // Milestone 8.35z34. Dodger the Warlock's six, and the split between them is the
                // difference between a question and a fact.
                //
                // The two Spawn-point counts ask nobody anything: "if you are adjacent to an empty
                // spawn point" and "if there are 2 or more empty spawn points in radius" are readings
                // of the board that gate a step, and a player who plays the card is not shown a
                // control for them. That is the same classification `presence-count@1` has carried
                // since milestone 8.22 for the identical grammar.
                ["empty-minion-spawn-points-adjacent"] = Array.Empty<PlayerInteractionShape>(),
                ["empty-minion-spawn-points-in-radius"] = Array.Empty<PlayerInteractionShape>(),

                // Her two progression contracts, on the same terms. Whether the Ultimate is unlocked
                // is a fact about the hero's own area, and p.16's item grant is a consequence whose
                // recipient the card names - "gain +1 Attack item" asks nothing and offers nothing.
                // What the item does afterwards is read on the dashboard, which is the hero's own
                // component row and not this card's.
                ["ultimate-unlocked"] = Array.Empty<PlayerInteractionShape>(),
                ["gain-item-from-the-box"] = Array.Empty<PlayerInteractionShape>(),

                // p.19's two Respawns, which are the one place her content asks TWO questions on one
                // node. The destination is a space clicked on the board, exactly as every Place is;
                // the type is a control among miniatures in the supply, which is not a board click at
                // all - see RespawnedMinionTypeChoice. Both arms declare both, because the arms differ
                // only in where the reach comes from and not in what is asked.
                ["synthetic-respawn-friendly-minion-adjacent"] = new[]
                {
                    PlayerInteractionShape.BoardSpaceSelection,
                    PlayerInteractionShape.RespawnedMinionTypeChoice
                },
                ["synthetic-respawn-friendly-minion-in-radius"] = new[]
                {
                    PlayerInteractionShape.BoardSpaceSelection,
                    PlayerInteractionShape.RespawnedMinionTypeChoice
                },

                // Milestone 8.35z38. Misa the Samurai's six, and the split between them is the same
                // difference between a question and a fact that Dodger's six drew.
                //
                // Her two Places ask for a space a player clicks, exactly as the three Place arms above
                // do. What differs is which spaces are offered - a ring rather than a disk for the
                // first (RULING 1's "at maximum range"), and a disk narrowed to spaces standing beside
                // two or more enemy units for the second - and that is a difference in the CANDIDATE
                // SET, not in what the player is asked. A shape is the input a player supplies, so two
                // reaches that both end in one board click introduce one shape.
                //
                // It is worth saying what this classification does NOT claim, because RULING 1 makes
                // the empty case ordinary rather than exotic: a ring of radius four can have no empty
                // member, and a disk can hold no space beside two enemies. Both are then a node with
                // no legal answer, which milestone 8.35l's machinery greys with its blocker named
                // rather than offering. That is the absence of the question, not a different question,
                // so it moves no shape - and Misa's gate rows owe a journey that reaches it.
                ["synthetic-place-at-maximum-range"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["synthetic-place-in-radius-beside-two-or-more-enemies"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },

                // Her five Blue Skills' two nodes. The line's end is a space clicked on the board; the
                // unit she moved through is a piece clicked on the board. They are two shapes rather
                // than one because they are two clicks in sequence and a player can be stopped after
                // either - the line may have no legal end, and a legal line may have crossed nobody.
                ["choose-line-destination"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["choose-traversed-unit"] =
                    new[] { PlayerInteractionShape.BoardUnitSelection },

                // Her two Swap Skills' pair selections, on exactly the terms `choose-target-pair@1`
                // carries since milestone 8.22: a pair of units is units clicked on the board, and the
                // pair-ness is a constraint on which clicks are admitted rather than a second kind of
                // input. Storm Spirit's equal-distance relation is the first RELATIONAL constraint in
                // the build and still asks for the same thing - two pieces.
                ["choose-unit-pair-at-maximum-radius"] =
                    new[] { PlayerInteractionShape.BoardUnitSelection },
                ["choose-unit-pair-at-equal-distance-in-radius"] =
                    new[] { PlayerInteractionShape.BoardUnitSelection },

                // Her Red ranged pair's two tenses, which ask nobody anything. "If the target was at
                // maximum range" and "if the target is not adjacent to you" are readings of p.14's own
                // captured facts and of the live board; the player is shown no control for either, and
                // sees only what the rider does afterwards. Same classification, same grammar and the
                // same milestone-8.22 sentence as `pre-consequence-adjacency@1` and
                // `target-is-adjacent@1` two dozen lines above - which is the point: RULING 32 makes
                // the two MOMENTS differ and leaves the player's part in them identical.
                ["target-was-at-maximum-range"] = Array.Empty<PlayerInteractionShape>(),
                ["target-is-not-adjacent"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.35z49. Tigerclaw's, and it is here now because the SECOND CARD TEXT is
                // read now - see AddCardTextShapes. Combat Reflexes' step-7 body asks whether its actor
                // moved during its own step-1 body, which is a reading of what the resolution already
                // recorded and draws no control at all, on exactly the terms `pre-consequence-adjacency@1`
                // and `hero-defeated-this-round@1` carry. It was unclassified for two milestones without
                // failing anything, because nothing walked the node it sits in - so classifying it is not
                // an addition this milestone chose but one the walk forces: an unclassified capability
                // THROWS rather than deriving nothing, and the two rows the fix moves are exactly the two
                // faces that name it.
                ["actor-moved-this-action"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.35z46. Silverarrow the Pathfinder's two NEW entries.
                // `choose-line-destination` is not repeated here - Misa's dealing already classifies
                // it above, and it is the same contract her own three straight-line faces name
                // (`choose-line-destination@1` is the wave's shared declaration; see
                // SyntheticRosterCapabilities.ChooseLineDestinationV1). Adding the key a second time
                // would collide on the same dictionary this classification is.
                //
                // `fast-travel@1` is p.11's Fast Travel reached from card text rather than from the
                // secondary-action row, and what it offers is the set of spaces the printed scope
                // admits - an adjacent Zone's for both Gold basics, p.11's own for Trailblazer. It is
                // NOT the iconless Fast Travel every card already carries, which is an action rather
                // than a node, so it earns its own classification instead of inheriting one.
                ["fast-travel"] = new[] { PlayerInteractionShape.BoardSpaceSelection },

                // A reading of the instance that just resolved - "If you target a hero" - which gates
                // a step and shows a player no control. Wasp's content minted it and Silverarrow's
                // Rain of Arrows is the first face to name it from a hero's own set.
                ["synthetic-attack-took-a-hero"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.35z50. Ursafar the Savage's four.
                //
                // Whether the acting hero is currently in a named condition, asked of the match rather
                // than of the frame. A reading and never a question: twelve of his faces gate a clause
                // on it and the player is shown no control for the gate itself - what they are asked
                // comes from the clause it governs, which declares its own shape. Its absence from
                // this table was the first Play Mode run's own find.
                ["hero-condition"] = Array.Empty<PlayerInteractionShape>(),
                //
                // The borrow names a card by its whole face, wherever it currently sits - discarded or
                // resolved beside the dashboard, never the hand - which is the same "a card chosen on
                // its whole face" question `retrieve-card@1` asks of the discard pile. A shape is the
                // input a player supplies, and both are the same click on the same kind of surface;
                // which pile is a difference in the candidate set, not in what is asked.
                ["borrow-primary-action"] = new[] { PlayerInteractionShape.HandCardSelection },

                // Milestone 8.19b's targeting family under `SyntheticTargetingCapabilities` carries
                // its own `synthetic-` prefixed id, distinct from the roster family's bare
                // `choose-target` above despite the shared method-name root - Ursafar's three Blue
                // swap riders (`ChooseTargetV3`) are the first dealt content to reach it. Same input
                // either way: a unit or a token, clicked where it stands.
                ["synthetic-choose-target"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                // The card's own printed Radius, published as a value with no player input of its own -
                // the same grammar `presence-count@1` and `pre-consequence-adjacency@1` carry. What a
                // player is asked comes from the selection it feeds, which declares its own shape.
                ["synthetic-card-radius"] = Array.Empty<PlayerInteractionShape>(),

                // Whether this Attack's own target survived p.14's step 6, read live off board
                // occupancy. A reading, not a question: the player sees only what the removal rider
                // does afterwards, on the same terms `pre-consequence-adjacency@1` and
                // `target-is-adjacent@1` already carry for the Attack's other captured facts.
                ["attack-target-survived"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.35z53. Whisper the Outcast's four, and the split between them is the
                // same difference between a question and a fact that Dodger's six and Misa's six drew.
                //
                // THE FORCED MOVE IS A QUESTION, and it is asked of somebody who is not the actor -
                // which is what makes it the most surprising entry in this whole table. "That hero
                // performs a movement action on the card they defended with, moving full distance in a
                // straight line" offers the DEFENDER the furthest space each of p.19's six directions
                // reaches and takes one board click, with `EffectChoiceRequest.ChooserHeroId` naming
                // that hero rather than the actor. "Full distance" removes p.11's "up to", so the
                // intermediate spaces are not offers and the six candidates are the whole question; a
                // direction that cannot be entered contributes nothing, which is how a hero hemmed in on
                // every side leaves the `if able` doing nothing at all. It is a space and not a unit
                // because the piece is already named - the Attack named it - and what is being asked is
                // where it goes.
                ["defended-card-forced-move"] = new[] { PlayerInteractionShape.BoardSpaceSelection },

                // Her two multi-arm selections, which are board clicks on exactly the terms
                // `choose-target-set@1` and `choose-target-pair@1` carry since milestone 8.22. The trio
                // takes up to three minions adjacent to her and the swap subject takes one unit out of
                // the union of two differently reached sets - "a unit adjacent to you, OR an enemy hero
                // in range with a card in the discard". A player names each by clicking where it
                // stands, and the union-ness is a constraint on which clicks are admitted rather than a
                // second kind of input: composing it as a modal would have drawn a "which kind of
                // swap?" control the card never prints.
                ["choose-target-trio"] = new[] { PlayerInteractionShape.BoardUnitSelection },
                ["choose-swap-subject"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                // A presence question about the board, answered by the rules rather than by anybody -
                // "if an enemy hero in radius has a card in the discard", which Death Seeker asks once
                // and Blood Frenzy asks again before every instance of its chain. Whisper's content
                // reaches it only as a `Conditional`, and this table classifies only `Capability`
                // nodes, so today nothing consults this entry. It is declared all the same, for the
                // reason the dictionary's own header gives: an unknown capability is a question this
                // table cannot answer, and a later face that names this one as a Capability node would
                // fail on the id rather than on the shape. Classifying it costs nothing and leaving it
                // out would be a trap laid for whoever prints the next discard-gated sentence.
                ["enemy-hero-with-discard-present"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.37. Tali the Spiritcaller's new entries.
                //
                // Her six token capabilities - a placement and a p.18 short-supply reclamation per
                // role - all resolve to a space clicked on the board. Both open a real choice whenever
                // more than one candidate space exists (ReclaimHandler asks exactly the placement's own
                // question, over standing tokens of the group rather than empty spaces), so both are
                // BoardSpaceSelection and neither is empty - a reclaim that removes the wrong reading
                // would claim a token vanishes by rule alone when RULING 44's own text is p.10's
                // ordinary board click.
                ["tali-place-ice-token"] = new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["tali-reclaim-for-ice-token"] = new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["tali-place-ancestral-totem"] = new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["tali-reclaim-for-ancestral-totem"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["tali-place-venerated-totem"] = new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["tali-reclaim-for-venerated-totem"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },

                // Commune with Spirits' "a friendly hero in play" - a hero clicked on the board, on the
                // same terms every other hero selection here already reads. See
                // SyntheticBorrowedActionCapabilities.FriendlyHeroInPlayV1's own remarks for why it is a
                // new capability rather than a reuse of a printed-icon family: it is anchored on no
                // Range or Radius icon at all.
                ["friendly-hero-in-play"] = new[] { PlayerInteractionShape.BoardUnitSelection },

                // Cold Snap, Snowstorm and Blizzard's free direction - see DirectionChoice's own
                // remarks for why no board-anchored shape fits it.
                ["synthetic-choose-direction"] = new[] { PlayerInteractionShape.DirectionChoice },

                // Commune with Spirits' naming step and Reign of Winter's colour-of-choice discard -
                // see ColorNamingChoice's own remarks.
                ["name-color"] = new[] { PlayerInteractionShape.ColorNamingChoice },

                // p.18's batch direct Move over every enemy unit in radius: a reading and a consequence
                // together, over a set the printed sentence names outright ("each"), so nobody is asked
                // which units move - the same reading `attack-sequence` and `synthetic-object-push`'s
                // own unbounded batch already carry.
                ["synthetic-batch-direct-move"] = Array.Empty<PlayerInteractionShape>(),

                // Ice Blast's step-3-to-step-7 rider: a reading of the defended card's own colour,
                // doubling as the Conditional's gate. Shows a player no control of its own - the
                // selection and the discard that follow it declare theirs separately.
                ["defended-card-color"] = Array.Empty<PlayerInteractionShape>(),

                // The source card's own discard-state predicate: a reading, gating a Conditional or
                // scaling a dynamic value, never a control of its own.
                ["synthetic-source-card-in-discard"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.38f. Swift the Sharpshooter's three new entries.
                //
                // Her two straight-line placements are a space clicked on the board, exactly as every
                // other Place in this table is: what the two qualifiers change is WHICH spaces the
                // offer contains - the file through her own space, and its complement - never how the
                // player names one. They are two rows rather than one because this table is keyed by
                // capability id and both ids are registered; the shape they contribute is identical,
                // which is the honest answer rather than a distinction invented to fill the column.
                ["synthetic-place-in-radius-in-straight-line"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },
                ["synthetic-place-in-radius-off-every-straight-line"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },

                // The enemy-scoped terrain predicate her three Green faces qualify their target by. A
                // READING, like the actor-scoped twin above it and like every other predicate here: it
                // narrows the set a later node offers and asks the player for nothing itself, so an
                // empty row is what it contributes and a non-empty one would claim a control that does
                // not exist.
                ["subject-adjacent-terrain-presence"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.39j. Cutter the Sky Pirate's three new entries, and she introduces no
                // new SHAPE either - which is the same finding Swift's dealing recorded one hero
                // earlier and is worth stating rather than leaving as an absence.
                //
                // THE OBSTACLE TARGET IS A BOARD SPACE, on the capability's own terms rather than on
                // a convenience. `synthetic-range-obstacle-target@1` publishes a COORDINATE, because
                // p.18 allows at most one obstacle in any one space - "Ever. No exceptions." - so an
                // obstacle is named one-to-one by where it stands and p.19's own Target keyword
                // already reads "Target (Unit / Token / Space)". A player answers it by clicking a hex
                // exactly as they answer every Place in this table; what differs is which hexes the
                // offer contains, which is the candidate-set-versus-shape line milestone 8.35z38 drew
                // for Misa's ring. Classifying it as a unit selection would claim the piece is the
                // subject, and a heap of terrain is not a piece.
                ["synthetic-range-obstacle-target"] =
                    new[] { PlayerInteractionShape.BoardSpaceSelection },

                // The solo-victory claim asks nobody anything: it reads the controller's own coin
                // ledger at the branch the card always reaches and writes the winner when the printed
                // thirteen is met. A player sees the match end, which is not a control they operate -
                // the same classification `gain-resource@1` carries two dozen lines above for the
                // grant that fed it.
                ["claim-solo-victory"] = Array.Empty<PlayerInteractionShape>(),

                // And the actor note is a publication rather than a question - it applies
                // unconditionally and puts the acting hero on the frame so the grant beside it can
                // bind a recipient. Nothing is offered and nothing is picked.
                ["synthetic-note-actor"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.40k. Bain the Bounty Hunter's six new entries, and he introduces exactly
                // ONE new shape - the reveal below - where the two heroes before him introduced none.
                //
                // HIS FOUR MARKER OPERATIONS ARE READINGS, on the terms Tigerclaw's two gives have
                // carried since milestone 8.23a: the hero a marker lands on, leaves, or comes off is
                // named by a selection node or derived from the board, so none of the four asks a
                // player for anything - and what each leaves behind is a row on a rail that a player
                // has to be able to read, because three of his five Green faces do nothing else.
                //
                // The GIVE and the TRANSFER are the same reading Tigerclaw's Poison is: after it, one
                // named hero wears the bounty and their own rail says so. The RETURN is the same
                // reading taken from the other side - the row is GONE - which is a reading and not an
                // absence of one: a player who cannot see the bounty leave cannot tell whether Narrow
                // Escape did anything at all, and every Green face of his that follows reads the same
                // board fact. Classifying it empty would let his second Green Block be proved by a
                // journey that never looked at a rail.
                ["bain-give-bounty-marker"] = new[] { PlayerInteractionShape.HeldMarkerReading },
                ["bain-transfer-bounty-marker"] = new[] { PlayerInteractionShape.HeldMarkerReading },
                ["bain-return-bounty-marker"] = new[] { PlayerInteractionShape.HeldMarkerReading },

                // And the predicate all five of his Greens and both Movement primaries gate on is a
                // reading of the board answered by the rules, on exactly the terms `presence-count@1`
                // and `enemy-hero-with-discard-present@1` carry. His content reaches it only as a
                // `Conditional`, which this table does not walk, so nothing consults this entry today
                // - it is declared for the reason the dictionary's own header gives, and for the
                // reason Whisper's discard predicate was declared: a later face naming it as a
                // Capability node must fail on the shape rather than on the id.
                ["bain-bounty-marker-held"] = Array.Empty<PlayerInteractionShape>(),

                // Milestone 8.40f's two hidden-information contracts, and the split between them is
                // the difference between what a player SUPPLIES and what a player SEES.
                //
                // The hidden pick is a card chosen from a hand on its whole face, which is the same
                // input `discard-owned-card@1` and `retrieve-card@1` already ask for - the 8.15a
                // contract, unchanged. What is new about it is WHO is asked and who may see the
                // answer, and neither is a shape: `EffectChoiceRequest.ChooserHeroId` has directed a
                // pause at a named hero since milestone 7.1a, and Whisper's `defended-card-forced-move`
                // already established that a different chooser answering the same kind of control is
                // the same shape. The privacy is a projection property, asserted inside the journey
                // from a seat that must NOT see it.
                ["synthetic-hidden-card-choice"] =
                    new[] { PlayerInteractionShape.HandCardSelection },

                // The reveal asks nobody anything and is still a thing every seat has to be shown -
                // see PublicCardRevealReading's own remarks for why it is not folded into the
                // selection above it.
                ["synthetic-reveal-chosen-card"] =
                    new[] { PlayerInteractionShape.PublicCardRevealReading }
            };

        private readonly Dictionary<string, ActiveEffectDefinition> activeEffectsByCapability;
        private readonly IReadOnlyList<EffectDefinition> triggeredEffects;

        public HeroPlayerPathMatrix(
            string heroDisplayName,
            IReadOnlyList<CardDefinition> allDefinitions,
            IReadOnlyList<CardDefinitionId> returnedToBoxAtSetup,
            IEnumerable<IEffectCapabilityHandler> activeEffectHandlers,
            IReadOnlyList<EffectDefinition> ultimateTriggeredEffects,
            IReadOnlyList<PlayerPathRow> rows)
        {
            triggeredEffects = ultimateTriggeredEffects;
            HeroDisplayName = heroDisplayName;
            AllDefinitions = allDefinitions;
            ReturnedToBoxAtSetup = returnedToBoxAtSetup;
            Rows = rows;

            activeEffectsByCapability = activeEffectHandlers
                .OfType<ActiveEffectCreationHandler>()
                .SelectMany(handler => handler.ActiveEffectDefinitions
                    .Select(definition => (handler.Reference.CapabilityId.Value, definition)))
                .ToDictionary(pair => pair.Value, pair => pair.definition, StringComparer.Ordinal);
        }

        public string HeroDisplayName { get; }
        public IReadOnlyList<CardDefinition> AllDefinitions { get; }
        public IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; }
        public IReadOnlyList<PlayerPathRow> Rows { get; }

        /// <summary>
        /// Every shape this printed face asks of a player, read off the definition rather than
        /// declared. An unreachable face - one the box takes at setup, or one whose composition
        /// carries a named deferral - introduces none, because nobody can reach it to be asked.
        /// </summary>
        public IReadOnlyList<PlayerInteractionShape> ShapesIntroducedBy(CardDefinition definition)
        {
            var shapes = new SortedSet<PlayerInteractionShape>();
            if (IsUnreachable(definition))
            {
                return Array.Empty<PlayerInteractionShape>();
            }

            // A card with an action is a card a player commits to a turn slot and reveals at p.7. The
            // Ultimate has none: p.16 turns it faceup in the hero's area and its rule fires from
            // there, so requiring a commitment journey of it would be requiring a journey for
            // something the rulebook does not ask anybody to do.
            if (definition.ActionDefinitions.Count > 0)
            {
                shapes.Add(PlayerInteractionShape.CommitAndReveal);
            }

            if (definition.Progression.IsUltimate)
            {
                // p.16 puts the unlocked Ultimate faceup in the hero's area, which is a thing a player
                // must be able to see before it is a thing they can use.
                shapes.Add(PlayerInteractionShape.UltimateRailVisibility);
                if (definition.UltimateRule != null &&
                    definition.UltimateRule.TriggeredEffect.HasValue)
                {
                    shapes.Add(PlayerInteractionShape.UltimateTriggerPause);
                    AddTriggeredEffectShapes(definition, shapes);
                }
            }

            // A printed item icon is deliberately not a shape this face introduces. p.16 gains the item
            // from the sibling the Level Up did NOT take, so the icon is content that p.15's procedure
            // consumes rather than anything the card itself asks a player to do. The hero's own
            // component row owns it, and owns the reason no journey reaches it.

            foreach (var action in definition.ActionDefinitions)
            {
                AddActionShapes(action, shapes);
            }

            return shapes.ToArray();
        }

        private void AddActionShapes(ActionDefinition action, ISet<PlayerInteractionShape> shapes)
        {
            var isPrimary = action.Origin == ActionDefinitionOrigin.PrintedPrimary;
            var isSecondary = action.Origin == ActionDefinitionOrigin.PrintedSecondary;

            switch (action.Kind)
            {
                case ActionKind.Attack when isPrimary:
                    shapes.Add(action.AttackRange == AttackRangeKind.Ranged
                        ? PlayerInteractionShape.RangedAttackTarget
                        : PlayerInteractionShape.AdjacentAttackTarget);
                    break;
                case ActionKind.Skill when isPrimary:
                    shapes.Add(PlayerInteractionShape.PrimarySkillControl);
                    break;
                case ActionKind.Movement when isPrimary:
                    shapes.Add(PlayerInteractionShape.PrimaryMovementDestination);
                    break;
                case ActionKind.Movement when isSecondary:
                    shapes.Add(PlayerInteractionShape.SecondaryMovementDestination);
                    break;
                // p.14 prints two kinds of Defense in the primary slot and they resolve differently:
                // a Defense Value is compared against the Attack Total, and a Block succeeds outright
                // or leaves the defender defeated. One input, two transitions, so two shapes.
                case ActionKind.Defense when isPrimary:
                    shapes.Add(action.DefenseResolution == PrintedDefenseResolution.Block
                        ? PlayerInteractionShape.BlockDefenseOffer
                        : PlayerInteractionShape.PrimaryDefenseOffer);
                    break;
                // p.10's secondary Defense icon is declared with a Reaction origin rather than a
                // printed-secondary one, because it is offered inside somebody else's Attack rather
                // than from the actor's own turn. Reading it as a secondary would silently drop the
                // shape from every hand in the game, which is exactly the failure this matrix exists
                // to make loud.
                case ActionKind.Defense when action.Origin == ActionDefinitionOrigin.Reaction:
                    shapes.Add(PlayerInteractionShape.SecondaryDefenseOffer);
                    break;
            }

            if (action.MinionDefenseContributions == MinionDefenseContributionPolicy.SuppressAll)
            {
                shapes.Add(PlayerInteractionShape.MinionDefenseSuppressionReading);
            }

            // BOTH printed bodies, and reading only the first was a standing under-report this table
            // corrected at milestone 8.35z49.
            //
            // <para><b>What was wrong.</b> Milestone 8.5 gave one action TWO card-text bodies -
            // `EffectRoot` at `CardTextTiming` and `SecondCardTextRoot` at `SecondCardTextTiming` -
            // because a face may print "Before the attack:" and "After the attack:" on one card. This
            // derivation read the first pair only, so every shape the SECOND body asks for was
            // invisible to the gate. Tigerclaw's Combat Reflexes and Leaping Strike have carried a
            // step-7 body since milestone 8.23 and neither row declared p.14's after-attack pause,
            // which is a coverage claim the content did not support in the direction this table exists
            // to catch.</para>
            //
            // <para><b>Why it stayed invisible for two milestones.</b> Both of those second bodies
            // compose an optional step and a direct Move, and both faces already declare
            // `ConfirmOrDecline` and `BoardSpaceSelection` from their FIRST body - so the missing walk
            // dropped no shape that was not already on the row by another route, and the missing timing
            // was one enum member on two rows. The only observable was an absent
            // `AfterAttackTextPause`.</para>
            //
            // <para><b>The blast radius was MEASURED before the change rather than discovered after
            // it</b>, by enumerating the whole corpus structurally rather than by diffing derived
            // shapes: of 611 actions across all twelve composed heroes, exactly FIVE declare a
            // non-null `SecondCardTextRoot` - Tigerclaw's Combat Reflexes and Leaping Strike, Ursafar's
            // Claws That Catch and Tear, and Whisper's Swift Justice, every one of them
            // `AfterTheAttack`. Only Tigerclaw's two belong to a DEALT hero, so exactly two shipped
            // rows move and each gains exactly one shape; the other three heroes are undealt and gate
            // their own faces at their own dealings. Every other action's derivation is byte-identical
            // by construction, because the added call walks a null root and returns after its timing
            // switch. Both moved rows are proved by `TigerclawProgressedUiJourneyPlayModeTests
            // .CombatReflexes_DeclinesItsFirstMoveAndTakesTheSecondAfterTheAttack`, which has driven
            // that exact step since milestone 8.23 and says so in its own name - so this is a gate that
            // starts asking for evidence that already exists, the cheapest shape a correction of this
            // kind can have.</para>
            AddCardTextShapes(action.CardTextTiming, action.EffectRoot, shapes);
            AddCardTextShapes(action.SecondCardTextTiming, action.SecondCardTextRoot, shapes);
        }

        /// <summary>
        /// One printed card-text body: the p.14 or p.11 moment it arrives at, and every shape its own
        /// nodes ask for. Called once per body, because an action may print two.
        /// </summary>
        private void AddCardTextShapes(
            CardTextTiming timing, EffectDefinition root, ISet<PlayerInteractionShape> shapes)
        {
            switch (timing)
            {
                case CardTextTiming.BeforeTheAttack:
                    shapes.Add(PlayerInteractionShape.BeforeAttackTextPause);
                    break;
                case CardTextTiming.AfterTheAttack:
                    shapes.Add(PlayerInteractionShape.AfterAttackTextPause);
                    break;
                case CardTextTiming.BeforeTheMovement:
                    shapes.Add(PlayerInteractionShape.BeforeMovementTextPause);
                    break;
                case CardTextTiming.AfterTheMovement:
                    shapes.Add(PlayerInteractionShape.AfterMovementTextPause);
                    break;
                case CardTextTiming.DefenseAction:
                    shapes.Add(PlayerInteractionShape.DefenseTextApplication);
                    break;
            }

            if (root == null)
            {
                return;
            }

            foreach (var node in root.Nodes)
            {
                AddNodeShapes(node, root, shapes);
            }
        }

        /// <summary>
        /// The shapes an Ultimate's own triggered effect asks for. p.16's Ultimate carries its effect
        /// by reference rather than inline, so the hero's matrix is handed the definitions and this
        /// resolves the one the rule names - and fails when it cannot, because an Ultimate whose
        /// effect nobody supplied would otherwise be an Ultimate that appears to ask for nothing.
        /// </summary>
        private void AddTriggeredEffectShapes(
            CardDefinition definition, ISet<PlayerInteractionShape> shapes)
        {
            var reference = definition.UltimateRule.TriggeredEffect.Value;
            var effect = triggeredEffects.FirstOrDefault(candidate =>
                candidate.Reference.Equals(reference));
            Assert.That(effect, Is.Not.Null,
                $"{definition.DisplayName}'s Ultimate rule triggers effect '{reference}', which this " +
                "hero's matrix was not given. Pass it in ultimateTriggeredEffects, or the shapes its " +
                "trigger asks a player for are counted as none.");

            foreach (var node in effect.Nodes)
            {
                AddNodeShapes(node, effect, shapes);
            }
        }

        /// <summary>
        /// Whether a Choose node's branches are the two sides of the primary action it encloses, which
        /// is Xargatha's "before or after movement" and not rulebook p.10's modal form.
        ///
        /// <para>It is read off the branches rather than off the card, so it says what it means: a
        /// Choose between placements is a placement decision whoever prints it, and a Choose between
        /// anything else is p.10's modal whoever prints it. Milestone 8.24.</para>
        /// </summary>
        private static bool ChoosesAnActionTextPlacement(
            EffectNodeDefinition node, EffectDefinition definition)
        {
            if (node.ChildNodeIds.Count == 0)
            {
                return false;
            }

            return node.ChildNodeIds.All(childId =>
                definition.Nodes.Any(child =>
                    child.Id.Equals(childId) &&
                    child.Capability.IsDefined &&
                    string.Equals(
                        child.Capability.CapabilityId.Value,
                        "around-primary-action",
                        StringComparison.Ordinal)));
        }

        private void AddNodeShapes(
            EffectNodeDefinition node,
            EffectDefinition definition,
            ISet<PlayerInteractionShape> shapes)
        {
            // "may" and "up to" are the two printed words that open a decision; `if able` and a
            // mandatory step do not, which is the distinction EffectFailurePolicy exists to keep.
            // Direct Move expresses its up-to distance inside the destination-space selection itself:
            // it never opens a separate confirm-or-decline control. Milestone 8.25e5f1 made that
            // distinction observable by separating Siren's Call's IfAble wrapper from its UpTo move.
            //
            // Milestone 8.39j generalised the Direct Move exception into the sentence the resolver
            // already documents, having measured that it is one case of two rather than one of one.
            // `EffectResolutionSession`'s own remarks state the rule: <i>"`up to` needs no change: a
            // handler that admits the printed `up to` already declares a floor of zero itself"</i> -
            // so an UpTo node draws a control of its own exactly where its handler lowers its floor,
            // and where the handler keeps a floor of one the optionality lives in the OFFER instead.
            // `choose-line-destination@1` is the second such handler: it offers 1..N spaces, declares
            // a floor of one, and turns a refused or empty offer into a step not taken through the
            // node's policy rather than through a control. Measured in Play Mode at this milestone -
            // Cutter's retreat stands as `Choose 1 of 1 space` with no confirm and no decline drawn -
            // and the same is true of every line-destination face in the build.
            if (node.FailurePolicy == EffectFailurePolicy.May ||
                (node.FailurePolicy == EffectFailurePolicy.UpTo &&
                    !OffersItsUpToInsideItsOwnSelection(node)))
            {
                shapes.Add(PlayerInteractionShape.ConfirmOrDecline);
            }

            if (node.Kind == EffectNodeKind.RepeatAction)
            {
                shapes.Add(PlayerInteractionShape.RepeatDecision);
            }

            // Milestone 8.21. Rulebook p.10's modal form: "choose a bullet point and apply its text,
            // ignoring the rest". Brogan's Throwing Axe and Throwing Spear are the first printed faces
            // to use it, and picking a bullet is not any of the shapes above - it is neither a
            // confirm-or-decline nor a piece on the board, and the two bullets go on to ask different
            // questions of the player who chose one.
            //
            // Milestone 8.24 narrows it by what the branches ARE, not by which card carries them.
            // Xargatha's three Green Movements choose between two `around-primary-action` placements -
            // "before or after movement" - and that is not p.10's modal form: no bullet is printed, no
            // sentence is ignored, and the same text runs either way. Classifying it as a modal would
            // put a second control on the row beside `ActionTextPlacementChoice` when the player is
            // shown one, which is the correction milestone 8.20b made for a push distance. A Choose
            // whose branches are anything else is unchanged, so every Brogan row stays byte-identical.
            if (node.Kind == EffectNodeKind.Choose && !ChoosesAnActionTextPlacement(node, definition))
            {
                shapes.Add(PlayerInteractionShape.ModalBulletChoice);
            }

            if (node.Kind != EffectNodeKind.Capability)
            {
                return;
            }

            var capabilityId = node.Capability.CapabilityId.Value;
            if (activeEffectsByCapability.TryGetValue(capabilityId, out var activeEffect))
            {
                AddActiveEffectShapes(activeEffect, shapes);
                return;
            }

            Assert.That(CapabilityShapes.ContainsKey(capabilityId), Is.True,
                $"{HeroDisplayName}'s content names capability '{capabilityId}', which this gate " +
                "cannot classify. Add it to HeroPlayerPathMatrix.CapabilityShapes with the input it " +
                "asks a player for - an unclassified capability would otherwise contribute no shape " +
                "and be proved by nothing.");

            foreach (var shape in CapabilityShapes[capabilityId])
            {
                // Milestone 8.21, and the same correction milestone 8.20b made for the Push distance:
                // a direct Move whose node names its destination asks nobody anything. Brogan's
                // Onslaught walks into the space p.14's step 6 emptied and his charges walk to the
                // space the charge line already chose, so the capability's ordinary board click is not
                // a click those cards produce - and a row claiming one would name a journey for an
                // input no player is ever offered.
                if (shape == PlayerInteractionShape.BoardSpaceSelection &&
                    NamesItsOwnDestination(node))
                {
                    continue;
                }

                shapes.Add(shape);
            }

            // Milestone 8.39j. BOTH of this build's Pushes, rather than the batch alone.
            //
            // <para>The distance question belongs to the DECLARATION and not to which push capability
            // asks it: `synthetic-object-push@3` and `synthetic-push-target@3` read the same two
            // literals (`distance` and `leastDistance`), and both hand the player an integer control
            // when the two differ - <c>PushOrAskDistance</c> is one shared helper in
            // <see cref="SyntheticDisplacementCapabilities"/> and it is what asks. Keying the shape on
            // the batch id alone was an accident of which hero had been dealt: every
            // `synthetic-push-target` in the build until now named ONE distance, so the question was
            // never asked and the omission was invisible.</para>
            //
            // <para>Cutter's Walk the Plank is the first to print "up to 4 spaces" on the named-target
            // push, and its player really is shown the distance. <b>The refinement leaves every
            // earlier hero byte-identical</b>, which is the test of a generalisation rather than a
            // re-fit: Sabina's Point Blank and Ricochet are the only other verified faces naming this
            // capability and both declare `distance` 1 against `leastDistance` 1, so the predicate is
            // false for them exactly as it was before.</para>
            if ((string.Equals(
                        capabilityId,
                        SyntheticDisplacementCapabilities.ObjectPush.Value,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        capabilityId,
                        SyntheticDisplacementCapabilities.PushTarget.Value,
                        StringComparison.Ordinal)) &&
                NamesMoreThanOnePushDistance(node))
            {
                shapes.Add(PlayerInteractionShape.PushDistanceChoice);
            }
        }

        /// <summary>
        /// Whether this batch Push offers the player a distance to pick, read off the node's own
        /// declared bounds. Milestone 8.20b.
        ///
        /// <para>The capability is one capability and the question is not always asked. Arien's Rogue
        /// Wave prints "up to 2 spaces" and declares a floor of zero, so the distance is a decision;
        /// Wasp's Kinetic Repulse prints "3 spaces" and declares floor and ceiling alike at 3, so
        /// there is one legal answer and the handler opens no second question - which
        /// <c>KineticRepulse_SelectsItsPushedUnitOnTheBoardAndConfirmsTheBatch</c> has asserted since
        /// milestone 8.15. Classifying the shape on the capability alone would have made that row
        /// claim a control no player is ever shown.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.21. Whether this node's declaration names the space it moves into, which is the
        /// one direct-Move policy that opens no choice: the destination is a value an earlier node
        /// published, so the capability applies it rather than offering a set to pick from.
        ///
        /// <para>It is read off the node for the reason
        /// <see cref="NamesMoreThanOnePushDistance"/> is: the capability is the same one either way,
        /// and what decides whether a player is asked anything is the declaration the card made. A
        /// classification keyed on the capability alone would give Brogan's Onslaught a board click it
        /// never produces.</para>
        /// </summary>
        /// <summary>
        /// Whether an <c>up to</c> node's own handler expresses the printed optionality inside the
        /// selection it offers rather than in a control beside it. Milestone 8.39j.
        ///
        /// <para>Three handlers in this build do, and they were measured rather than guessed at:</para>
        /// <list type="bullet">
        /// <item><description><c>synthetic-direct-move@3</c>, which milestone 8.25e5f1 separated from
        /// Siren's Call's IfAble wrapper.</description></item>
        /// <item><description><c>choose-line-destination@1</c>, which offers every distance from one
        /// to the printed maximum and keeps a floor of one. Silverarrow's own nought-distance journey
        /// states the consequence in as many words - "the zero-distance answer is the occupied origin
        /// on the board, not a decline button".</description></item>
        /// <item><description><c>synthetic-push-target@3</c>, whose printed "up to N spaces" is drawn
        /// as the INTEGERS nought to N and answered by picking one. That is
        /// <see cref="PlayerInteractionShape.PushDistanceChoice"/>, which the same node already
        /// declares, so counting it twice would put two controls on a row where a player meets
        /// one.</description></item>
        /// </list>
        /// <para>The batch <c>synthetic-object-push@3</c> is deliberately NOT here: it declares a
        /// floor of nought for its own "up to", which is the sentence
        /// <c>EffectResolutionSession</c> names, so its offer really does carry a decline.</para>
        /// </summary>
        private static bool OffersItsUpToInsideItsOwnSelection(EffectNodeDefinition node) =>
            node.Capability.IsDefined &&
            (string.Equals(
                    node.Capability.CapabilityId.Value,
                    SyntheticDisplacementCapabilities.DirectMove.Value,
                    StringComparison.Ordinal) ||
                string.Equals(
                    node.Capability.CapabilityId.Value,
                    SyntheticDisplacementCapabilities.PushTarget.Value,
                    StringComparison.Ordinal) ||
                string.Equals(
                    node.Capability.CapabilityId.Value,
                    "choose-line-destination",
                    StringComparison.Ordinal));

        private static bool NamesItsOwnDestination(EffectNodeDefinition node) =>
            string.Equals(
                node.Capability.CapabilityId.Value,
                SyntheticDisplacementCapabilities.DirectMove.Value,
                StringComparison.Ordinal) &&
            node.Inputs.Any(input =>
                string.Equals(
                    input.ParameterName,
                    SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                    StringComparison.Ordinal) &&
                input.Literal.IsDefined &&
                input.Literal.IntegerValue ==
                    (int)GuardsDigital.Core.Rules.Displacement.DirectMoveDestinationPolicy.NamedSpace);

        private static bool NamesMoreThanOnePushDistance(EffectNodeDefinition node) =>
            DeclaredInteger(node, PushDistanceParameterName) >
            DeclaredInteger(node, SyntheticDisplacementCapabilities.LeastDistanceParameterName);

        /// <summary>
        /// The batch Push's own ceiling parameter. It is the one bound of the two the capability does
        /// not spell as a constant, so it is spelled here and <see cref="DeclaredInteger"/> refuses a
        /// node that does not bind it - a rename fails this gate loudly rather than quietly deciding
        /// that no card offers a distance.
        /// </summary>
        private const string PushDistanceParameterName = "distance";

        /// <summary>
        /// One literal integer this node binds. The push declares both of its bounds as literals, so a
        /// node that binds neither is a definition this gate cannot read rather than one with a
        /// default worth inventing.
        /// </summary>
        private static int DeclaredInteger(EffectNodeDefinition node, string parameterName)
        {
            var bound = node.Inputs
                .Where(input =>
                    string.Equals(input.ParameterName, parameterName, StringComparison.Ordinal))
                .ToArray();
            Assert.That(bound, Has.Length.EqualTo(1),
                $"The batch Push at node '{node.Id.Value}' binds no literal '{parameterName}', so " +
                "this gate cannot tell whether it offers a player a distance to pick.");
            return bound[0].Literal.IntegerValue;
        }

        private static void AddActiveEffectShapes(
            ActiveEffectDefinition activeEffect, ISet<PlayerInteractionShape> shapes)
        {
            shapes.Add(PlayerInteractionShape.ActiveEffectReading);

            // What makes this a second reading shape is that the effect is still standing on a turn
            // that did not raise it, so a player meets it where nothing on screen just happened. Every
            // duration except `This turn` does that; `CarriesAcrossRoundBoundary` is a narrower
            // question - whether p.8's boundary itself spares it - and Master Duelist answers it no
            // while still outliving three turns.
            if (activeEffect.DurationKind != ActiveEffectDurationKind.ThisTurn ||
                activeEffect.CarriesAcrossRoundBoundary)
            {
                shapes.Add(PlayerInteractionShape.RoundLongActiveEffectReading);
            }

            // Milestone 8.20b narrowed this to the restrictions an enemy meets as a control of their
            // own. A restriction that names no actor bars the activity however it is reached, so the
            // affected enemy performing it is one of the barred actors and the enemy's own control is
            // drawn greyed - Arien's Spell Break, Slippery Ground and Deluge are all of that shape. A
            // restriction that names its actors is about what may be done *to* the affected unit, and
            // this build deliberately never draws a control for it at all: both consumers filter such
            // a piece out of the offered candidates, "because a piece the rules will not let this hero
            // move is not one to offer and then refuse" (SyntheticPlacementCapabilities,
            // SyntheticExchangeCapabilities). Wasp's Magnetic Dagger is the only content of that shape
            // in this build, and requiring a reading journey of it would require a journey for
            // something no player is shown. What the pin does is owned at the authoritative tier by
            // ConfiguredBasicCardTests, which the card's own row links.
            var stopsAnEnemyFromActing =
                activeEffect.AffectedUnitRelation == SourceTeamRelation.OpposingSource &&
                activeEffect.Modifications.Any(modification =>
                    (modification.Kind == RuleModificationKind.Restriction ||
                        modification.Kind == RuleModificationKind.ActivityLimit) &&
                    modification.RestrictedActors != null &&
                    modification.RestrictedActors.IsUnqualified);
            if (stopsAnEnemyFromActing)
            {
                shapes.Add(PlayerInteractionShape.OpponentRestrictionReading);
            }
        }

        /// <summary>
        /// Whether this face is one no player of the configured match can reach, on the content's own
        /// statement rather than on a row's say-so: p.6 returns it to the box, or its printed text
        /// carries a named deferral in the sense <see cref="PrintedTextComposition.DeferredUntil"/>
        /// defines.
        /// </summary>
        public bool IsUnreachable(CardDefinition definition) =>
            ReturnedToBoxAtSetup.Any(id => id.Equals(definition.Id)) ||
            definition.ActionDefinitions.Any(action =>
                action.TextComposition.NamedTrigger != null);

        /// <summary>
        /// Every shape this hero's whole printed set and its components introduce - the set the gate's
        /// journeys must cover between them.
        /// </summary>
        public IReadOnlyList<PlayerInteractionShape> AllIntroducedShapes() =>
            AllDefinitions
                .SelectMany(ShapesIntroducedBy)
                .Concat(RequiredComponentShapes)
                .Distinct()
                .OrderBy(shape => shape)
                .ToArray();

        // -----------------------------------------------------------------------------------------
        // The guard's assertions. They live beside the derivation rather than in the fixture so that
        // 8.20b's Wasp matrix is a table and a fixture calling these, not a second copy of the rules.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Every printed face has exactly one row, and no row invents a face. This is the case that
        /// fails when a hero gains a nineteenth card and nobody writes a journey for it.
        /// </summary>
        public void AssertEveryFaceHasExactlyOneRow()
        {
            var faceIds = AllDefinitions.Select(definition => definition.Id.Value).ToArray();
            var rowIds = Rows.Where(row => !row.IsComponent).Select(row => row.SubjectId).ToArray();

            var missing = faceIds.Except(rowIds, StringComparer.Ordinal).OrderBy(id => id).ToArray();
            Assert.That(missing, Is.Empty,
                $"{HeroDisplayName} prints faces with no row in the player-path matrix, so nothing " +
                "requires them to be reachable through the player's own input: " +
                string.Join(", ", missing));

            var invented = rowIds.Except(faceIds, StringComparer.Ordinal).OrderBy(id => id).ToArray();
            Assert.That(invented, Is.Empty,
                "The matrix has face rows for cards this hero does not print: " +
                string.Join(", ", invented));

            var duplicated = rowIds
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            Assert.That(duplicated, Is.Empty,
                "One face, one row - these are declared more than once: " +
                string.Join(", ", duplicated));
        }

        /// <summary>
        /// Each row declares exactly the shapes its own printed content introduces. A row that claims
        /// less has a shape nothing proves; a row that claims more is a coverage claim the content
        /// does not support.
        /// </summary>
        public void AssertEveryRowDeclaresTheShapesItsContentIntroduces()
        {
            var complaints = new List<string>();
            foreach (var definition in AllDefinitions)
            {
                var row = Rows.Single(candidate =>
                    !candidate.IsComponent &&
                    string.Equals(candidate.SubjectId, definition.Id.Value, StringComparison.Ordinal));
                var derived = ShapesIntroducedBy(definition).OrderBy(shape => shape).ToArray();
                var declared = row.Shapes.OrderBy(shape => shape).ToArray();
                if (derived.SequenceEqual(declared))
                {
                    continue;
                }

                complaints.Add(
                    $"{row}: content introduces [{string.Join(", ", derived)}] but the row declares " +
                    $"[{string.Join(", ", declared)}]");
            }

            Assert.That(complaints, Is.Empty,
                $"{HeroDisplayName}'s matrix has drifted from the printed content it describes:" +
                Environment.NewLine + string.Join(Environment.NewLine, complaints));
        }

        /// <summary>
        /// A row has journeys exactly when its face is reachable, and states a reason exactly when it
        /// is not - checked against the content, so a row cannot excuse itself out of the gate.
        /// </summary>
        public void AssertUnreachableFacesAreDeclaredAndReachableOnesAreJourneyed()
        {
            var complaints = new List<string>();
            foreach (var definition in AllDefinitions)
            {
                var row = Rows.Single(candidate =>
                    !candidate.IsComponent &&
                    string.Equals(candidate.SubjectId, definition.Id.Value, StringComparison.Ordinal));
                var unreachable = IsUnreachable(definition);

                if (unreachable && !row.IsDeclaredAbsence)
                {
                    complaints.Add($"{row} is unreachable in this build but declares no reason.");
                }

                if (!unreachable && row.IsDeclaredAbsence)
                {
                    complaints.Add(
                        $"{row} declares the absence \"{row.AbsenceReason}\", but the content makes " +
                        "the face reachable, so a player can meet a face no journey drives.");
                }

                if (!unreachable && row.Journeys.Count == 0)
                {
                    complaints.Add($"{row} is reachable and names no journey.");
                }
            }

            Assert.That(complaints, Is.Empty,
                string.Join(Environment.NewLine, complaints));
        }

        /// <summary>
        /// Every shape a row declares is proved by a journey that row itself names, and the journey
        /// exists. This is the sentence the gate is actually made of: "a guard fails when a
        /// face/component or interaction shape is absent".
        ///
        /// <para><b>Why per row rather than per shape.</b> A single global "is this shape proved
        /// anywhere" would let one Attack journey discharge every Attack in the set, and the row for a
        /// face nobody had thought about would still read as covered. Per row, a face that gains a
        /// repeat it did not have gains an unproved shape on its own line, named by its own id.
        /// Journeys are shared between rows on purpose where the shape genuinely is the same one -
        /// that is what the gate's own "need not duplicate permutations" sentence permits - and the
        /// face-specific half of the evidence is the authoritative scenario column, which is per face
        /// by construction.</para>
        /// </summary>
        public void AssertEveryRowsShapesAreProvedByItsOwnJourneys(Assembly journeyAssembly)
        {
            var journeyNames = JourneyMethodNames(journeyAssembly);
            var unproved = Rows
                .Where(row => !row.IsDeclaredAbsence)
                .SelectMany(row => row.Evidence
                    .Where(item => !journeyNames.Contains(item.Journey))
                    .Select(item =>
                        $"{row}: {item.Shape} rests on '{item.Journey}', which is not a " +
                        "[UnityTest] in this assembly"))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.That(unproved, Is.Empty,
                $"{HeroDisplayName}'s player-path gate leaves interaction shapes unproved:" +
                Environment.NewLine + string.Join(Environment.NewLine, unproved));
        }

        /// <summary>
        /// Every component shape the gate requires of every hero appears on some component row. This
        /// is the half of the matrix the printed set cannot produce: the piece on the board, and the
        /// session operations the gate names by hand.
        /// </summary>
        public void AssertEveryRequiredComponentShapeIsDeclared()
        {
            var declared = Rows
                .Where(row => row.IsComponent)
                .SelectMany(row => row.Shapes)
                .ToHashSet();
            var missing = RequiredComponentShapes
                .Where(shape => !declared.Contains(shape))
                .ToArray();

            Assert.That(missing, Is.Empty,
                $"{HeroDisplayName}'s matrix declares no component row covering: " +
                string.Join(", ", missing));
        }

        /// <summary>
        /// Every journey a row names is a real Play Mode test in this assembly. A renamed journey
        /// fails here rather than leaving the matrix quietly pointing at nothing.
        /// </summary>
        public void AssertEveryNamedJourneyExists(Assembly journeyAssembly)
        {
            var journeyNames = JourneyMethodNames(journeyAssembly);
            var dangling = Rows
                .SelectMany(row => row.Journeys.Select(journey => (row, journey)))
                .Where(pair => !journeyNames.Contains(pair.journey))
                .Select(pair => $"{pair.row} names '{pair.journey}'")
                .ToArray();

            Assert.That(dangling, Is.Empty,
                "The matrix names journeys that are not [UnityTest] methods in this assembly:" +
                Environment.NewLine + string.Join(Environment.NewLine, dangling));
        }

        /// <summary>
        /// Every journey in the hero's own fixtures is named by a row. An unnamed journey is coverage
        /// nothing accounts for, which is the state the whole matrix exists to prevent from
        /// re-forming quietly on the other side.
        /// </summary>
        public void AssertEveryJourneyInFixturesIsNamedByARow(params Type[] journeyFixtures)
        {
            var named = new HashSet<string>(
                Rows.SelectMany(row => row.Journeys), StringComparer.Ordinal);
            var orphans = journeyFixtures
                .SelectMany(fixture => fixture
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => method.GetCustomAttributes(typeof(UnityTestAttribute), false).Any())
                    .Select(method => $"{fixture.Name}.{method.Name}"))
                .Where(entry => !named.Contains(entry.Substring(entry.IndexOf('.') + 1)))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.That(orphans, Is.Empty,
                "These journeys prove something the matrix does not account for:" +
                Environment.NewLine + string.Join(Environment.NewLine, orphans));
        }

        /// <summary>
        /// A component this hero's content does have, whose row records why no journey reaches it.
        /// The absence is only admissible while the content itself says the component exists - a hero
        /// printing no item icon at all may not carry an item row's excuse - so the debt is stated
        /// against the content rather than asserted.
        /// </summary>
        public void AssertDeclaredDebtHasItsSubject(
            string componentId, bool contentHasIt, string subjectDescription)
        {
            var row = Rows.SingleOrDefault(candidate =>
                candidate.IsComponent &&
                string.Equals(candidate.SubjectId, componentId, StringComparison.Ordinal));
            Assert.That(row, Is.Not.Null, $"The matrix has no component row '{componentId}'.");
            Assert.That(row.IsDeclaredAbsence, Is.True,
                $"{row} is recorded as a debt, so it must state its reason.");
            Assert.That(contentHasIt, Is.True,
                $"{row} records why {subjectDescription} is not journeyed, and the content does not " +
                "have it at all - the debt is about nothing and the row should go.");
        }

        /// <summary>
        /// A declared component absence, checked against the content that would contradict it. A row
        /// saying "this hero prints no token" is only worth having if the hero who does print one
        /// fails here rather than inheriting the sentence.
        /// </summary>
        public void AssertNoContentNamesAnyCapability(
            IReadOnlyList<string> capabilityIds, string absenceClaim)
        {
            var found = AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes
                    .Where(node => node.Kind == EffectNodeKind.Capability)
                    .Select(node => (action, capability: node.Capability.CapabilityId.Value)))
                .Where(pair => capabilityIds.Contains(pair.capability))
                .Select(pair => $"{pair.action.Id.Value} names '{pair.capability}'")
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            Assert.That(found, Is.Empty,
                $"The matrix records \"{absenceClaim}\", and the content contradicts it: " +
                string.Join("; ", found));
        }

        private static HashSet<string> JourneyMethodNames(Assembly journeyAssembly) =>
            new HashSet<string>(
                journeyAssembly
                    .GetTypes()
                    .SelectMany(type => type.GetMethods(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    .Where(method => method.GetCustomAttributes(typeof(UnityTestAttribute), false).Any())
                    .Select(method => method.Name),
                StringComparer.Ordinal);
    }
}
