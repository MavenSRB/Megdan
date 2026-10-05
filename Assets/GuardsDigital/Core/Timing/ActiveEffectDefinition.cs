using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Timing
{
    public enum ActiveEffectDurationKind
    {
        None = 0,
        ThisTurn = 1,
        ThisRound = 2,
        NextTurn = 3,
        NextTurnAfterPlayingCards = 4,

        /// <summary>
        /// Milestone 8.35z30. The keyword Silverarrow's Warning Shot and Treetop Sentinel
        /// print and no verified hero had printed before: "<b>End of turn:</b> An enemy hero in radius
        /// discards a card[, if able. / , or is defeated.]"
        ///
        /// <para><b>It is a duration and not one of p.17's four, because it bounds a different thing.</b>
        /// <see cref="ThisTurn"/> and its siblings say how long a <em>state</em> lasts, and everything
        /// declared under them is a rule that is true while they run. This one carries no rule at all -
        /// it declares a piece of work and the moment that work happens, and the moment is after every
        /// hero has acted. So its whole observable content is when it fires.</para>
        ///
        /// <para><b>Which turn, and where the radius is measured.</b> Both are the coordinator-adopted
        /// reading of SL-06 and both follow from the same sentence: the effect is created by her Skill,
        /// so it is her turn's; and it selects at <see cref="TimingCheckpoint.TurnEnded"/>, against the
        /// disk drawn around wherever she is standing then. p.17 already moves an Active effect's
        /// radius with its hero and stores no membership, so a hero who walked into the disk after the
        /// Skill resolved is inside it when this fires - which is exactly the difference between the
        /// two readings the card leaves open, and the one its own words ask for.</para>
        ///
        /// <para><b>It fires once and is then over</b>, which is why creation activates it immediately
        /// and expiry is the same checkpoint it triggers at.
        /// <see cref="ActiveEffectLifecycle"/> emits the trigger before the expiry when a definition
        /// names one checkpoint for both, so the work happens and the effect does not survive into the
        /// next turn looking for a second one.</para>
        /// </summary>
        EndOfTurn = 5
    }

    public enum ActiveEffectAffectedScopeKind
    {
        None = 0,
        ControllerHero = 1,
        Hero = 2,
        ControllerTeam = 3,
        Global = 4,

        /// <summary>
        /// Milestone 8.9. Every unit standing in a declared relation to the source, inside the disk the
        /// definition's <see cref="ActiveEffectDefinition.Radius"/> draws around its controller - Wasp's
        /// Magnetic Dagger, "Enemy units in radius".
        ///
        /// It is the first scope whose membership is decided by the board rather than by the match
        /// roster, and it is therefore the first that changes without the effect changing: p.17 moves
        /// the disk with its hero and every unit inside it walks in and out on its own. Nothing is
        /// stored, so the set is exactly whoever is inside at the moment a rule is asked for, which is
        /// what "in radius" means on a card that does not say "when this resolves".
        ///
        /// It names units rather than heroes because p.19 does: a pinned minion is as much an enemy
        /// unit as a pinned hero, and a scope that expanded to heroes alone would drop half the card.
        /// </summary>
        UnitsInRadius = 5,

        /// <summary>
        /// Milestone 8.20. The same moving disk, admitting heroes only - Arien's Spell Break, Slippery
        /// Ground and Deluge, each of which begins "Enemy heroes in radius" or "Enemy heroes adjacent
        /// to you".
        ///
        /// <para>It is a sixth member rather than a filter on <see cref="UnitsInRadius"/> because the
        /// two cards say different things and the difference is live. Magnetic Dagger pins enemy
        /// <em>units</em>, and p.19 makes a minion one; Spell Break bars enemy <em>heroes</em> from
        /// Skill actions, which is the word the face prints, and a scope that widened it to units would
        /// attribute to Arien a sentence about minions his card does not have. That the widened reading
        /// would be inert today - no minion performs a Skill - is exactly why it must not be taken: a
        /// scope chosen because the difference cannot currently be seen is a scope that will be wrong
        /// the first time it can.</para>
        ///
        /// <para>Membership is <em>intrinsic</em> - which miniature is standing there - and not p.17's
        /// effective type, for the reason <see cref="UnitsInRadius"/> already gives: deriving the live
        /// modification set is what walks this scope, so a scope that asked what an object counts as
        /// would be asking the answer it is being used to produce. It is the same reading
        /// <see cref="Rules.Modifications.RestrictedActorResolution"/> takes of who is acting, and for
        /// the same reason - a hero taking a turn is still the hero taking the turn while it counts as
        /// a minion. Nothing is stored, for the reason <see cref="UnitsInRadius"/> stores nothing.</para>
        /// </summary>
        HeroesInRadius = 6,

        /// <summary>
        /// Milestone 8.21. The same moving disk, admitting p.19's other unit kind only - Brogan's
        /// Shield, Bolster and Fortify, each of which begins "When any friendly <b>minion</b> in radius
        /// is defeated".
        ///
        /// <para>It is a seventh member for the reason <see cref="HeroesInRadius"/> is a sixth, and the
        /// case is the mirror image of that one. Those three cards buy a minion out of its removal at a
        /// printed price; a scope widened to units would offer the same purchase for a friendly
        /// <em>hero</em>, which is a sentence Brogan's faces do not print. That the widened reading
        /// would be nearly inert today - p.13 defeats a hero and respawns them rather than removing
        /// them - is the same argument milestone 8.20 declined to accept for the sixth member.</para>
        ///
        /// <para>Membership is intrinsic here too, and this is the scope where that matters most
        /// visibly: One Man Army makes its own hero count as a heavy minion during rulebook p.8's
        /// Minion Battle, and this walk still does not find him, because the miniature standing in the
        /// disk is a hero. The counts-as is scoped to the battle's counting by its own first word, and
        /// the card that substitutes his removal says what happens to him instead.</para>
        /// </summary>
        MinionsInRadius = 7,

        /// <summary>
        /// Milestone 8.21. The disk plus the hero it is drawn around - Brogan's Bulwark, "<b>You</b>
        /// and friendly units in radius cannot be moved, pushed, swapped or placed by enemy heroes".
        ///
        /// <para>It is a member rather than a flag because it is a different printed sentence, and it
        /// exists because <see cref="UnitsInRadius"/> deliberately excludes the source: p.19 keeps self
        /// and another distinct, so "units in radius" is read as the units other than the hero the disk
        /// follows. Every scope before this one named a side its controller could not be on, so the
        /// exclusion decided nothing; Bulwark is the first face that protects its own hero and says so
        /// in the first word of the clause.</para>
        ///
        /// <para>The controller is admitted unconditionally rather than through the declared relation.
        /// A hero is trivially allied with himself, and asking the relation about him would let a
        /// declaration that named the opposing side quietly drop the "You" the sentence opens
        /// with.</para>
        /// </summary>
        ControllerAndUnitsInRadius = 8,

        /// <summary>
        /// Milestone 8.35z30. The disk plus the hero it is drawn around, admitting p.19's
        /// heroes alone - Silverarrow's Trailblazer, "<b>This round:</b> You and friendly heroes in
        /// radius may ignore obstacles while performing movement actions."
        ///
        /// <para>It is a ninth member for the reason the sixth and the eighth are members: two cards
        /// print two different nouns and the difference is live. Brogan's Bulwark protects "You and
        /// friendly <em>units</em> in radius", and p.19 makes a minion one; Trailblazer grants its
        /// permission to "You and friendly <em>heroes</em>", which is the word the face prints. That
        /// the widened reading would be nearly inert today - the permission is scoped to movement
        /// actions, and a minion performs none - is exactly the argument milestone 8.20 refused to
        /// accept for <see cref="HeroesInRadius"/>, and it is refused again here.</para>
        ///
        /// <para>Membership is re-derived at every ask, so an ally who walks into the disk later in the
        /// round is inside it then. That is p.17's own reading of a radius and, per SL-07, the same
        /// behaviour Wasp's Static Barrier overlay has had since milestone 8.7 - reuse rather than
        /// invention, on a window one round long rather than one turn.</para>
        /// </summary>
        ControllerAndHeroesInRadius = 9,

        /// <summary>A named token kind counts differently only while this effect's controller acts.</summary>
        TokensOfKindWhileControllerActs = 10
    }

    /// <summary>
    /// Where the one hero an occurrence is bound to comes from, as definition content.
    ///
    /// <para>Milestone 8.20. Arien's Expert and Master Duelist print "except <b>this</b> attacker",
    /// and the word "this" is a referent the definition cannot hold: which hero it names is decided by
    /// the moment the card was discarded to defend. The definition therefore declares where to look,
    /// the occurrence carries whom it found, and
    /// <see cref="Rules.Modifications.RestrictedActorScope.ExceptsTheBoundActor"/> declares what the
    /// sentence then does with them - three statements, none of which guesses either of the others.</para>
    /// </summary>
    public enum ActiveEffectBoundActorSource
    {
        /// <summary>
        /// This definition points at nobody, which is what every definition declared before milestone
        /// 8.20 means. An occurrence of it binds no hero.
        /// </summary>
        None = 0,

        /// <summary>
        /// The hero attacking at rulebook p.14's step 4, where the defender applies their Defense
        /// action's text. It is the attacker rather than the actor performing the action, because at
        /// step 4 those are opposite sides and the card's "this attacker" names the other one -
        /// <see cref="Rules.EffectResolution.EnclosingAttackFacts.ActorId"/> is the fact, and it is
        /// documented as defined at exactly this moment.
        /// </summary>
        EnclosingAttacker = 1
    }

    public enum ActiveEffectCancellationCondition
    {
        None = 0,
        SourceCardStateChanged = 1,
        /// <summary>
        /// p.17: an Active effect on your card ends when "you are defeated" - the hero whose action
        /// created it, which p.13 states from the other side as "Defeated hero's actions with Active
        /// effects are canceled". Named `AffectedHeroDefeated` until milestone 7.4.
        /// </summary>
        ControllerHeroDefeated = 2,
        CardsRetrieved = 3,
        Explicit = 4
    }

    public sealed class ActiveEffectDefinition
    {
        private readonly string ownedObstacleRadiusAnchorKind;
        /// <summary>
        /// The trigger windows this build actually executes a declared effect at.
        ///
        /// It is a list rather than a single constant since milestone 8.19c, and it is still a list of
        /// what has a producer rather than of what a card could say. Milestone 8.12 built the Action
        /// Ended window for Wasp's High Voltage; 8.19c adds the end-of-round moment before the Minion
        /// Battle for Xargatha's Constrict, whose reminder text prints exactly that position. A window
        /// declared here with nothing visiting it would be a card that silently never fires.
        ///
        /// <para>Milestone 8.35z29 adds the third, and it has a producer for the same reason the
        /// other two do: Misa's Focus, Discipline and Mastery print "<b>Next turn:</b> Before you
        /// perform a primary action, you may move up to N spaces", and
        /// <see cref="GuardsDigital.Core.Rules.Progression.BeforeActionTriggerQuery"/> visits it -
        /// through the pre-action machinery milestone 8.19a built for Arien's Living Tsunami rather
        /// than through the lifecycle's own checkpoint pass, which is why nothing raises it as an
        /// ordinary checkpoint and why the window is still one an effect can be executed at.</para>
        /// </summary>
        private static readonly TimingCheckpoint[] SupportedTriggerWindows =
        {
            TimingCheckpoint.ActionEnded,
            TimingCheckpoint.EndOfRoundBeforeMinionBattle,
            TimingCheckpoint.BeforePrimaryAction,

            // Milestone 8.35z30 adds another, for Silverarrow's two "End of turn:" faces.
            // It is listed here only because that milestone also gave it a producer: the turn's own
            // close now visits it through the triggered-effect authority, exactly as milestone 8.19c1
            // gave the round's pre-battle moment one.
            TimingCheckpoint.TurnEnded
        };

        public ActiveEffectDefinition(
            string definitionId,
            ActiveEffectAffectedScopeKind affectedScope,
            ActiveEffectDurationKind durationKind,
            TimingCheckpoint creationCheckpoint,
            TimingCheckpoint activationCheckpoint,
            TimingCheckpoint expirationCheckpoint,
            IEnumerable<ActiveEffectCancellationCondition> cancellationConditions,
            bool carriesAcrossRoundBoundary,
            string effectKind,
            string uniquenessKey,
            IEnumerable<TimingCheckpoint> triggerCheckpoints,
            IEnumerable<RuleModification> modifications = null,
            IEnumerable<ConditionalObstacleOverlay> obstacleOverlays = null,
            DeclaredRadius radius = default,
            SourceTeamRelation affectedUnitRelation = SourceTeamRelation.None,
            EffectDefinitionReference? triggeredEffect = null,
            ActiveEffectBoundActorSource boundActorSource = ActiveEffectBoundActorSource.None,
            string ownedObstacleRadiusAnchorKind = null,
            GuardsDigital.Core.Components.TokenDefinitionId affectedTokenKind = default)
        {
            if (affectedScope == ActiveEffectAffectedScopeKind.TokensOfKindWhileControllerActs
                ? !affectedTokenKind.IsDefined
                : affectedTokenKind.IsDefined)
                throw new ArgumentException(
                    "Only an action-scoped token effect names its affected token kind.",
                    nameof(affectedTokenKind));
            AffectedTokenKind = affectedTokenKind;
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("An Active-effect definition requires a stable ID.", nameof(definitionId));
            if (affectedScope == ActiveEffectAffectedScopeKind.None)
                throw new ArgumentException("An Active-effect definition requires an affected scope.", nameof(affectedScope));
            if (durationKind == ActiveEffectDurationKind.None)
                throw new ArgumentException("An Active-effect definition requires a duration kind.", nameof(durationKind));
            if (creationCheckpoint == TimingCheckpoint.None || activationCheckpoint == TimingCheckpoint.None ||
                expirationCheckpoint == TimingCheckpoint.None)
                throw new ArgumentException("Creation, activation, and expiration checkpoints must all be explicit.");
            if (string.IsNullOrWhiteSpace(effectKind))
                throw new ArgumentException("An Active-effect definition requires a typed effect kind.", nameof(effectKind));
            if (string.IsNullOrWhiteSpace(uniquenessKey))
                throw new ArgumentException("An Active-effect definition requires an explicit uniqueness key.", nameof(uniquenessKey));

            var conditions = (cancellationConditions ?? throw new ArgumentNullException(nameof(cancellationConditions)))
                .ToArray();
            if (conditions.Length == 0 || conditions.Contains(ActiveEffectCancellationCondition.None) ||
                conditions.Distinct().Count() != conditions.Length)
                throw new ArgumentException("Active-effect cancellation conditions must be nonempty, defined, and unique.", nameof(cancellationConditions));
            var triggers = (triggerCheckpoints ?? throw new ArgumentNullException(nameof(triggerCheckpoints))).ToArray();
            if (triggers.Contains(TimingCheckpoint.None) || triggers.Distinct().Count() != triggers.Length)
                throw new ArgumentException("Active-effect trigger checkpoints must be defined and unique.", nameof(triggerCheckpoints));
            if (triggeredEffect.HasValue && !triggeredEffect.Value.IsDefined)
                throw new ArgumentException("A triggered effect reference must be defined.", nameof(triggeredEffect));
            if (triggeredEffect.HasValue && triggers.Length == 0)
                throw new ArgumentException("A declared triggered effect requires at least one trigger checkpoint.", nameof(triggerCheckpoints));
            if (triggeredEffect.HasValue && triggers.Any(value => !SupportedTriggerWindows.Contains(value)))
                throw new ArgumentException(
                    "An Active effect triggers at a window this build executes: Action Ended " +
                    "(milestone 8.12) or the end-of-round moment before the Minion Battle " +
                    "(milestone 8.19c).",
                    nameof(triggerCheckpoints));
            var declaredModifications = (modifications ?? Enumerable.Empty<RuleModification>()).ToArray();
            if (declaredModifications.Any(modification => modification == null))
                throw new ArgumentException("Declared modifications must be non-null.", nameof(modifications));
            var declaredOverlays =
                (obstacleOverlays ?? Enumerable.Empty<ConditionalObstacleOverlay>()).ToArray();
            if (declaredOverlays.Any(overlay => overlay == null))
                throw new ArgumentException("Declared obstacle overlays must be non-null.", nameof(obstacleOverlays));

            ValidateDuration(durationKind, creationCheckpoint, activationCheckpoint, expirationCheckpoint, carriesAcrossRoundBoundary);
            ValidateDisk(affectedScope, radius, affectedUnitRelation, declaredOverlays.Length);
            if (ownedObstacleRadiusAnchorKind != null &&
                (string.IsNullOrWhiteSpace(ownedObstacleRadiusAnchorKind) || !radius.IsDefined))
                throw new ArgumentException(
                    "An obstacle-centred active disk requires a named obstacle and a radius.",
                    nameof(ownedObstacleRadiusAnchorKind));
            this.ownedObstacleRadiusAnchorKind = ownedObstacleRadiusAnchorKind;
            ValidateBoundActor(boundActorSource, creationCheckpoint, declaredModifications);
            BoundActorSource = boundActorSource;
            Radius = radius;
            AffectedUnitRelation = affectedUnitRelation;
            DefinitionId = definitionId;
            AffectedScope = affectedScope;
            DurationKind = durationKind;
            CreationCheckpoint = creationCheckpoint;
            ActivationCheckpoint = activationCheckpoint;
            ExpirationCheckpoint = expirationCheckpoint;
            CancellationConditions = new ReadOnlyCollection<ActiveEffectCancellationCondition>(conditions);
            CarriesAcrossRoundBoundary = carriesAcrossRoundBoundary;
            EffectKind = effectKind;
            UniquenessKey = uniquenessKey;
            TriggerCheckpoints = new ReadOnlyCollection<TimingCheckpoint>(triggers);
            TriggeredEffect = triggeredEffect;
            Modifications = new ReadOnlyCollection<RuleModification>(declaredModifications);
            ObstacleOverlays = new ReadOnlyCollection<ConditionalObstacleOverlay>(declaredOverlays);
        }

        public string DefinitionId { get; }
        public ActiveEffectAffectedScopeKind AffectedScope { get; }
        /// <summary>
        /// The exact physical token kind selected by an action-scoped token effect. Undefined for
        /// every other scope; public so verified-content identity sees this semantic declaration.
        /// </summary>
        public GuardsDigital.Core.Components.TokenDefinitionId AffectedTokenKind { get; }
        public ActiveEffectDurationKind DurationKind { get; }
        public TimingCheckpoint CreationCheckpoint { get; }
        public TimingCheckpoint ActivationCheckpoint { get; }
        public TimingCheckpoint ExpirationCheckpoint { get; }
        public IReadOnlyList<ActiveEffectCancellationCondition> CancellationConditions { get; }
        public bool CarriesAcrossRoundBoundary { get; }
        public string EffectKind { get; }
        public string UniquenessKey { get; }
        public IReadOnlyList<TimingCheckpoint> TriggerCheckpoints { get; }

        /// <summary>
        /// Milestone 8.12. The trusted effect definition executed when one of
        /// <see cref="TriggerCheckpoints"/> is visited. The reference, rather than a copied definition,
        /// keeps trigger meaning in the configured content catalog across restore and replay.
        /// </summary>
        public EffectDefinitionReference? TriggeredEffect { get; }

        /// <summary>
        /// The typed rules this effect supplies while it is active, as definition content.
        ///
        /// Milestone 6.8 put them here rather than on the instance for the reason milestone 6.2a moved
        /// the rest of the semantics here: a snapshot names the definition and the trusted catalog
        /// supplies its meaning, so a restored instance produces exactly these modifications again and no
        /// payload can restate what a saved effect does. An effect that declares none is a timing marker,
        /// not a modifier.
        /// </summary>
        public IReadOnlyList<RuleModification> Modifications { get; }

        /// <summary>
        /// Milestone 8.7. The conditional obstacle overlays this effect supplies while it is active,
        /// as definition content, beside <see cref="Modifications"/> and for the same reasons.
        ///
        /// They are a separate collection because they are a different kind of rule: a modification
        /// names an object and changes something about it, while an overlay changes what p.18 counts
        /// as an obstacle and names no object at all. An effect that declares neither is a timing
        /// marker; one that declares both is Wasp's Static Barrier's shape without her card's numbers.
        /// </summary>
        public IReadOnlyList<ConditionalObstacleOverlay> ObstacleOverlays { get; }

        /// <summary>
        /// Milestone 8.9. The one disk this effect draws around its controller, or
        /// <see cref="DeclaredRadius.None"/> when it draws none.
        ///
        /// It lives on the definition rather than on either consumer because p.9 gives a card one
        /// Range/Radius icon and card text "refers to this value": an effect whose affected units and
        /// whose obstacle overlay measured different disks would be describing two cards. Both
        /// consumers read it, and it is resolved - items and all - at derivation, never stored.
        /// </summary>
        public DeclaredRadius Radius { get; }

        /// <summary>Optional live centre for a disk measured from a controller-owned obstacle.</summary>
        public bool TryGetOwnedObstacleRadiusAnchorKind(out string kind)
        {
            kind = ownedObstacleRadiusAnchorKind;
            return kind != null;
        }

        /// <summary>
        /// Which side of the source the units inside that disk stand on, for every scope
        /// <see cref="DrawsADisk"/> admits; <c>None</c> for every other scope, because none of them
        /// selects by position at all.
        /// </summary>
        public SourceTeamRelation AffectedUnitRelation { get; }

        /// <summary>
        /// Milestone 8.20. Where an occurrence of this definition finds the one hero its declarations
        /// point at with "this", or <see cref="ActiveEffectBoundActorSource.None"/> when none of them
        /// does.
        /// </summary>
        public ActiveEffectBoundActorSource BoundActorSource { get; }

        /// <summary>
        /// The bound actor's own consistency, stated once: a definition that looks for a hero must
        /// have a declaration that uses one, and a declaration that excepts one must say where to look.
        /// Either half alone is a card whose sentence has lost its other half - an exception nothing
        /// can meet, or a hero nothing asks about - and both are silent at the table.
        ///
        /// <para>The creation checkpoint is checked with them because
        /// <see cref="ActiveEffectBoundActorSource.EnclosingAttacker"/> names a fact that exists at
        /// exactly one moment. Rulebook p.14's step 4 is the defender applying their Defense action's
        /// text, and <c>DefenseResponse</c> is that step's checkpoint; a definition that looked for the
        /// attacker anywhere else would bind the undefined hero every time and except nobody, which is
        /// a card that quietly does something other than what it prints.</para>
        /// </summary>
        private static void ValidateBoundActor(
            ActiveEffectBoundActorSource boundActorSource,
            TimingCheckpoint creationCheckpoint,
            IReadOnlyList<RuleModification> declaredModifications)
        {
            if (!Enum.IsDefined(typeof(ActiveEffectBoundActorSource), boundActorSource))
            {
                throw new ArgumentException(
                    "An Active-effect definition states a defined bound-actor source.",
                    nameof(boundActorSource));
            }

            var usesTheBoundActor = declaredModifications.Any(
                modification => modification.RestrictedActors.ExceptsTheBoundActor);
            if (usesTheBoundActor && boundActorSource == ActiveEffectBoundActorSource.None)
            {
                throw new ArgumentException(
                    "A declaration that excepts the bound actor states where its occurrence finds one.",
                    nameof(boundActorSource));
            }

            if (!usesTheBoundActor && boundActorSource != ActiveEffectBoundActorSource.None)
            {
                throw new ArgumentException(
                    "A definition that binds an actor has a declaration that reads one.",
                    nameof(boundActorSource));
            }

            if (boundActorSource == ActiveEffectBoundActorSource.EnclosingAttacker &&
                creationCheckpoint != TimingCheckpoint.DefenseResponse)
            {
                throw new ArgumentException(
                    "The enclosing attacker is a fact of rulebook p.14's step 4, so a definition that " +
                    "binds one is created there.",
                    nameof(creationCheckpoint));
            }
        }

        /// <summary>
        /// The disk's own consistency, stated once. A scope that selects by position needs a radius and
        /// a side to select by, and a declared overlay needs a radius to draw itself at; a definition
        /// that needs neither may not carry one, because a disk nothing measures is a number a reader
        /// would have to decide the meaning of.
        /// </summary>
        /// <summary>
        /// Whether a scope's membership is decided by the disk its definition draws. Stated once
        /// because four of the eight kinds are now radius-scoped and every reader of that fact - this
        /// validation, the modification walk and the lifecycle's uniqueness rule - must agree about
        /// which; milestone 8.21's two new kinds are what turned a pair of comparisons into a question
        /// worth naming.
        /// </summary>
        public static bool DrawsADisk(ActiveEffectAffectedScopeKind affectedScope) =>
            affectedScope == ActiveEffectAffectedScopeKind.UnitsInRadius ||
            affectedScope == ActiveEffectAffectedScopeKind.HeroesInRadius ||
            affectedScope == ActiveEffectAffectedScopeKind.MinionsInRadius ||
            affectedScope == ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius ||
            affectedScope == ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius;

        private static void ValidateDisk(
            ActiveEffectAffectedScopeKind affectedScope,
            DeclaredRadius radius,
            SourceTeamRelation affectedUnitRelation,
            int declaredOverlayCount)
        {
            var scopeDrawsADisk = DrawsADisk(affectedScope);
            if (scopeDrawsADisk && affectedUnitRelation == SourceTeamRelation.None)
            {
                throw new ArgumentException(
                    "A radius-scoped Active effect states which side of its source the units it " +
                    "affects stand on; state SourceTeamRelation.AnyTeam when it affects every unit.",
                    nameof(affectedUnitRelation));
            }

            if (!scopeDrawsADisk && affectedUnitRelation != SourceTeamRelation.None)
            {
                throw new ArgumentException(
                    "Only a radius-scoped Active effect selects the units it affects by side.",
                    nameof(affectedUnitRelation));
            }

            if (!Enum.IsDefined(typeof(SourceTeamRelation), affectedUnitRelation))
            {
                throw new ArgumentException(
                    "A radius-scoped Active effect states a defined team relation.",
                    nameof(affectedUnitRelation));
            }

            var needsADisk = scopeDrawsADisk || declaredOverlayCount > 0;
            if (needsADisk && !radius.IsDefined)
            {
                throw new ArgumentException(
                    "An Active effect whose scope or overlay is measured from a disk requires the " +
                    "radius that disk is drawn at.",
                    nameof(radius));
            }

            if (!needsADisk && radius.IsDefined)
            {
                throw new ArgumentException(
                    "An Active effect that measures nothing from a disk declares no radius.",
                    nameof(radius));
            }
        }

        private static void ValidateDuration(
            ActiveEffectDurationKind duration,
            TimingCheckpoint creation,
            TimingCheckpoint activation,
            TimingCheckpoint expiration,
            bool carriesAcrossRound)
        {
            if (duration == ActiveEffectDurationKind.NextTurn ||
                duration == ActiveEffectDurationKind.NextTurnAfterPlayingCards)
            {
                if (creation == TimingCheckpoint.TurnStarted || creation == TimingCheckpoint.CardsPlayedAndRevealed ||
                    creation == TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded || creation == TimingCheckpoint.TurnEnded ||
                    creation == TimingCheckpoint.RoundEnd || creation == TimingCheckpoint.CardsRetrieved)
                    throw new ArgumentException("A Next Turn effect must be created during an action/effect window before its future activation anchor.");
            }

            if (duration == ActiveEffectDurationKind.ThisTurn)
            {
                if (activation != TimingCheckpoint.ActionStarted && activation != TimingCheckpoint.BeforeAttack &&
                    activation != TimingCheckpoint.DefenseResponse &&
                    activation != TimingCheckpoint.AttackResultComparison &&
                    activation != TimingCheckpoint.AfterAttack && activation != TimingCheckpoint.ActionEnded &&
                    activation != TimingCheckpoint.TurnStarted)
                    throw new ArgumentException("A This Turn effect must activate within the current turn.");
                if (expiration != TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded || carriesAcrossRound)
                    throw new ArgumentException("A This Turn effect expires at the turn-only checkpoint and never carries across rounds.");
            }
            else if (duration == ActiveEffectDurationKind.ThisRound)
            {
                if (expiration != TimingCheckpoint.RoundEnd || carriesAcrossRound)
                    throw new ArgumentException("A This Round effect expires at Round End and never carries across rounds.");
            }
            else if (duration == ActiveEffectDurationKind.NextTurn)
            {
                if (activation != TimingCheckpoint.TurnStarted || expiration != TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded)
                    throw new ArgumentException("A Next Turn effect activates at Turn Started and expires at that turn's cancellation checkpoint.");
            }
            else if (duration == ActiveEffectDurationKind.EndOfTurn)
            {
                // Milestone 8.35z30. The keyword names one moment, so the definition may
                // not put its work anywhere else: it is created and activated inside the turn that
                // will end, and it ends at that end. A definition that expired earlier would be gone
                // before its own moment arrived, and one that expired later would still be standing at
                // the next turn's end looking for a second.
                if (activation != TimingCheckpoint.ActionStarted &&
                    activation != TimingCheckpoint.AfterAttack &&
                    activation != TimingCheckpoint.ActionEnded)
                {
                    throw new ArgumentException(
                        "An End of turn effect is created by the action that prints it, so it " +
                        "activates at one of that action's own moments.");
                }

                if (expiration != TimingCheckpoint.TurnEnded || carriesAcrossRound)
                {
                    throw new ArgumentException(
                        "An End of turn effect expires at the end of the turn it was created in and " +
                        "never carries across rounds.");
                }
            }
            else if (duration == ActiveEffectDurationKind.NextTurnAfterPlayingCards)
            {
                if (activation != TimingCheckpoint.CardsPlayedAndRevealed ||
                    expiration != TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded)
                    throw new ArgumentException("Next Turn after Playing Cards has distinct reveal activation and turn expiry checkpoints.");
            }
        }
    }
}
