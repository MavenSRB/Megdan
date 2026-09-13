using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Bain the Bounty Hunter's five p.17 Active effects, and the reason he has exactly five.
    /// Milestone 8.40i.
    ///
    /// <para><b>Three of his nineteen faces print an Active-effect keyword and they are named here
    /// rather than counted later.</b> Vantage Point and High Ground each print two independent
    /// sentences that last beyond the node that writes them - "Ignore obstacles" and "+N movement" -
    /// and Another One! prints "End of turn: May repeat once". Nothing else in his set does: his
    /// marker is a COMPONENT rather than an Active effect (its one rule rides the attachment, which is
    /// <see cref="BainComponentContent"/>'s business), his Blocks resolve inside p.14's own window, and
    /// his Ultimate is a permanent ability rather than a created effect.</para>
    ///
    /// <para><b>Why the two Movement faces declare TWO definitions each rather than one carrying two
    /// modifications.</b> The sentences are gated differently: "Ignore obstacles." is unconditional and
    /// "+N movement" happens only while a hero in play holds the marker, so a single definition would
    /// have to be created on the condition and would then take the obstacle permission away on a board
    /// with no marker out - a rule neither face prints.</para>
    ///
    /// <para><b>BA-Q3, settled here and proved in <c>BainPrintedSetTests</c>: the raise is the TURN's,
    /// which means a second Movement action in the same turn is raised too.</b> That is Ursafar's Cold
    /// Ire answer inherited rather than re-decided, and the reason is structural rather than
    /// preferential: <c>RuleValueApplicability</c> has nothing scoped to one ACTION, and
    /// <see cref="ActiveEffectDurationKind"/> names no such window either, so the shortest duration the
    /// build can express for a value raise is the turn. Both of his faces are Movement primaries, and a
    /// hero performs at most one primary action per turn, so the second Movement action this reading
    /// reaches is a SECONDARY one - the boot on another card - or one a text granted. Recorded as the
    /// reading taken rather than left to fall out of the vocabulary.</para>
    ///
    /// <para><b>Deferred, not registered</b>, on the standing rule that the registered capability list
    /// is serialized into every snapshot: nothing here joins the build's catalog until milestone 8.40j
    /// deals him. Every one of these is another instance of <c>ActiveEffectCreationHandler</c>, a
    /// handler TYPE this build has carried since milestone 6.8 and showcased since, so none of them
    /// owes milestone 7.9's gate a showcase consumer of its own.</para>
    /// </summary>
    public static class BainActiveEffectContent
    {
        /// <summary>
        /// The two conditions that end one of his turn-scoped effects early, on exactly Ursafar's
        /// terms: his own defeat, and p.8's card retrieval, which takes back the card the effect is
        /// about. The duration covers the ordinary end.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] TurnCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// The two Green faces that print "Ignore obstacles." and the raise beside it - Vantage Point's
        /// <c>+1</c> and High Ground's <c>+2</c>. These are the faces printing BA-09's <c>2+</c> cue.
        /// </summary>
        public static IReadOnlyDictionary<CardDefinitionId, int> MovementRaises { get; } =
            new Dictionary<CardDefinitionId, int>
            {
                { BainCardContent.VantagePoint, 1 },
                { BainCardContent.HighGround, 2 }
            };

        /// <summary>The capability that lets one named card's Movement action cross obstacles.</summary>
        public static EffectCapabilityReference IgnoreObstaclesFor(CardDefinitionId cardId) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(cardId.Value + "-ignore-obstacles"), 1);

        /// <summary>The capability that raises one named card's Movement allowance.</summary>
        public static EffectCapabilityReference MovementRaiseFor(CardDefinitionId cardId) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(cardId.Value + "-movement"), 1);

        /// <summary>
        /// "Ignore obstacles." - Silverarrow's Trailblazer permission, scoped to the hero who played the
        /// card instead of to an allied disk, because this sentence names nobody else.
        ///
        /// <para>It is <see cref="RuleActivity.Movement"/> and not <see cref="RuleActivity.All"/>: the
        /// clause is printed on a MOVEMENT primary and p.18's obstacle rule is what a Movement asks, so
        /// a permission spanning every activity would say something about his Attacks that the card does
        /// not.</para>
        /// </summary>
        public static ActiveEffectDefinition IgnoreObstaclesDefinitionFor(CardDefinitionId cardId)
        {
            RequireMovementFace(cardId);
            return new ActiveEffectDefinition(
                cardId.Value + "-ignore-obstacles-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                TurnCancellations,
                false,
                "BainIgnoreObstacles",
                cardId.Value + "-ignore-obstacles",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForPermission(
                        RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement)
                });
        }

        /// <summary>
        /// The raise itself: an ordinary value contribution on p.9's Movement channel, for this turn.
        ///
        /// <para>It is an Active effect rather than a roster contribution because
        /// <c>RosterValueContributionDefinition</c>'s constructor <b>refuses the Movement channel
        /// outright</b> - "Roster card-text contributions modify Attack, Range or Defense" - which is
        /// what milestone 8.40 measured and what makes this Ursafar's shape rather than Cutter's
        /// <c>+2 Attack</c> shape. The allowance is composed from declared contributions and nothing
        /// else, so a raised total is composed BEFORE p.11's action-start restriction judges it.</para>
        /// </summary>
        public static ActiveEffectDefinition MovementRaiseDefinitionFor(CardDefinitionId cardId)
        {
            if (!MovementRaises.TryGetValue(cardId, out var amount))
            {
                throw new ArgumentException(
                    $"Bain's card '{cardId}' prints no Movement raise.", nameof(cardId));
            }

            return new ActiveEffectDefinition(
                cardId.Value + "-movement-raise",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                TurnCancellations,
                false,
                "BainMovementRaise",
                cardId.Value + "-movement-raise",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForValue(PrintedValueChannel.Movement, amount) });
        }

        /// <summary>
        /// "<b>End of turn:</b> May repeat once." - Another One!'s second sentence, which Tali's
        /// Blizzard established the shape of and Silverarrow's two End-of-turn Skills established
        /// before her: an effect created and activated by the Skill that prints it, triggering at the
        /// turn's own end and expiring at that same moment, so it fires exactly once.
        ///
        /// <para><b>It is not a p.10 Repeat, and the difference is the reason this file exists.</b>
        /// <c>EffectNodeKind.RepeatAction</c> re-runs the enclosing ACTION, and at
        /// <see cref="TimingCheckpoint.TurnEnded"/> there is no enclosing action - the turn is over. So
        /// the clause costs the four things Tali's does: a hero-owned capability id, a hero-owned
        /// definition, an <c>ActiveEffectCreationHandler</c> registration, and a hand-written duplicate
        /// of the node graph inside <see cref="BainEffectContent.AnotherOneRepeatTriggeredEffect"/>.
        /// The consequence worth naming is the same one Tali's records: a card that forbids REPEATING
        /// an action does not read this, because nothing here is spelled as a repeat to the
        /// engine.</para>
        ///
        /// <para><b>It affects nobody while it stands</b> - no radius, no modification. Its whole
        /// content is that a piece of work happens at the end of the turn, and every question the
        /// clause asks is asked by the triggered effect when the checkpoint arrives.</para>
        ///
        /// <para><b>And it is armed unconditionally.</b> Another One! prints no "if" before its keyword
        /// sentence, which is what separates it from Blizzard's "If Blizzard is in your discard" - so
        /// the Skill arms it whether or not the first sentence retrieved anything, and the repeat's own
        /// "May" is what the player answers at the turn's end.</para>
        /// </summary>
        public static EffectCapabilityId AnotherOneEndOfTurnRepeat { get; } =
            new EffectCapabilityId("bain-another-one-end-of-turn-repeat");

        public static EffectCapabilityReference AnotherOneEndOfTurnRepeatV1 =>
            new EffectCapabilityReference(AnotherOneEndOfTurnRepeat, 1);

        /// <summary>
        /// Built on first use rather than in the static initializer, for the reason
        /// <see cref="BainCardContent.AllDefinitions"/> is: this definition names a triggered effect
        /// that names his card ids, and a class-initialization cycle in this corner presents as
        /// <c>NUnit failed to load</c> with zero tests discovered rather than as a failure.
        /// </summary>
        private static readonly Lazy<ActiveEffectDefinition> LazyAnotherOneRepeatDefinition =
            new Lazy<ActiveEffectDefinition>(() => new ActiveEffectDefinition(
                "bain-another-one-repeat-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                TurnCancellations,
                false,
                "BainAnotherOneRepeat",
                "bain-another-one-repeat",
                new[] { TimingCheckpoint.TurnEnded },
                triggeredEffect: BainEffectContent
                    .AnotherOneRepeatTriggeredEffect(BainCardContent.ContentRevision)
                    .Reference));

        public static ActiveEffectDefinition AnotherOneRepeatDefinition =>
            LazyAnotherOneRepeatDefinition.Value;

        /// <summary>Every definition this file declares, for a fixture that needs them all.</summary>
        public static IEnumerable<ActiveEffectDefinition> Definitions() =>
            MovementRaises.Keys
                .Select(IgnoreObstaclesDefinitionFor)
                .Concat(MovementRaises.Keys.Select(MovementRaiseDefinitionFor))
                .Concat(new[] { AnotherOneRepeatDefinition });

        /// <summary>
        /// Everything this file declares, registered or not, for the offline content fixtures that
        /// compose catalogs of their own.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// Deliberately empty until milestone 8.40j deals him, for the reason
        /// <see cref="BainComponentContent"/> states: the registered capability list is serialized into
        /// every snapshot, so registering a capability before its hero is dealt moves pinned bytes and
        /// fails milestone 7.9's reachability gate.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// His five creations, all on the proved <c>ActiveEffectCreationHandler</c> type, waiting for
        /// the dealing that registers them.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            MovementRaises.Keys
                .Select(cardId => (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                    IgnoreObstaclesFor(cardId), IgnoreObstaclesDefinitionFor(cardId)))
                .Concat(MovementRaises.Keys.Select(cardId =>
                    (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                        MovementRaiseFor(cardId), MovementRaiseDefinitionFor(cardId))))
                .Concat(new IEffectCapabilityHandler[]
                {
                    new ActiveEffectCreationHandler(
                        AnotherOneEndOfTurnRepeatV1, AnotherOneRepeatDefinition)
                });

        private static void RequireMovementFace(CardDefinitionId cardId)
        {
            if (!MovementRaises.ContainsKey(cardId))
            {
                throw new ArgumentException(
                    $"Bain's card '{cardId}' prints no Movement primary.", nameof(cardId));
            }
        }
    }
}
