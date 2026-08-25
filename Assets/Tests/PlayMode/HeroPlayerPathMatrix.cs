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
        HeldMarkerReading
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
                // The charge line asks two questions in one node and they are two different clicks:
                // which space to end on, and which of the units beside it this Attack then takes. The
                // unit-valued printed-Range selection is a third printed-icon sibling of the six above
                // and asks what they ask. And a card the actor spends out of their own hand is the
                // same hand-card question milestone 7.1a's discard asks, put to the actor instead of
                // to somebody the card named.
                ["choose-charge-line"] = new[]
                {
                    PlayerInteractionShape.BoardSpaceSelection,
                    PlayerInteractionShape.BoardUnitSelection
                },
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
                    new[] { PlayerInteractionShape.HeldMarkerReading }
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

            switch (action.CardTextTiming)
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
                case CardTextTiming.DefenseAction:
                    shapes.Add(PlayerInteractionShape.DefenseTextApplication);
                    break;
            }

            if (action.EffectRoot == null)
            {
                return;
            }

            foreach (var node in action.EffectRoot.Nodes)
            {
                AddNodeShapes(node, shapes);
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
                AddNodeShapes(node, shapes);
            }
        }

        private void AddNodeShapes(EffectNodeDefinition node, ISet<PlayerInteractionShape> shapes)
        {
            // "may" and "up to" are the two printed words that open a decision; `if able` and a
            // mandatory step do not, which is the distinction EffectFailurePolicy exists to keep.
            if (node.FailurePolicy == EffectFailurePolicy.May ||
                node.FailurePolicy == EffectFailurePolicy.UpTo)
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
            if (node.Kind == EffectNodeKind.Choose)
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

            if (string.Equals(
                    capabilityId,
                    SyntheticDisplacementCapabilities.ObjectPush.Value,
                    StringComparison.Ordinal) &&
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
