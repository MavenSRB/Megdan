using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5ac acceptance check 2: a registered capability cannot omit the feasibility answer,
    /// and answering it performs nothing.
    ///
    /// The compiler already forces every handler to declare
    /// <see cref="IEffectCapabilityHandler.Anticipate"/>, so what is left to enforce is the part a
    /// signature cannot state. Two rules do it, in the shape milestone 6.5aa used for the shared
    /// immunity check and milestone 6.8g for the unit-type queries: what the probe must read, and what
    /// it must never touch.
    ///
    /// The first rule is derivation. A handler whose execution can refuse - it reaches
    /// <see cref="EffectExecutionResult.ImpossibleMandatory"/> or
    /// <see cref="EffectExecutionResult.DeclinedOptional"/> - and that derives that refusal from
    /// anything at all must reach at least one of the same members from its probe. A second, separate
    /// implementation of the same rule is exactly the failure this milestone cannot afford: it would
    /// grey an option the execution would have performed, or offer one it would refuse. A handler that
    /// only ever applies, and one whose execution is a bare constant, are outside the rule because they
    /// have no derivation to share.
    ///
    /// The second rule is the probe's whole reason for existing. Handlers apply rather than pause -
    /// they commit displacements, spawn from the supply, create Active effects, hand over markers, take
    /// them back, and mint identities for all of it - so a probe that reached one of those members
    /// would perform the effect it was asked about.
    ///
    /// Both walk the call graph rather than one method body, so a probe cannot escape either rule by
    /// putting the work one call away. Unity-independent, so it runs in the cross-platform harness as
    /// well as the Windows tier.
    /// </summary>
    public sealed class CapabilityFeasibilityConformanceTests
    {
        [Test]
        public void EveryRegisteredHandler_DerivesItsProbeFromItsOwnExecutionPath()
        {
            var handlers = RegisteredHandlerTypes();

            Assert.That(ILConformanceScanner.UnresolvedTokens(handlers), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindUnsharedDerivations(handlers), Is.Empty,
                "A registered capability answers the feasibility question from code its own execution " +
                "never reads. Two derivations of one rule can disagree, and when they do a player is " +
                "either shown a mode that does nothing or refused one that would have worked.");
        }

        [Test]
        public void NoProbe_ReachesAMemberThatPerformsSomething()
        {
            Assert.That(FindPerformingProbes(RegisteredHandlerTypes()), Is.Empty,
                "A feasibility probe reaches a member that mutates or mints. Asking whether an option " +
                "can be taken must never take it - which is why the contract asks Anticipate rather " +
                "than running Execute in the first place.");
        }

        [Test]
        public void ConformanceRules_DistinguishAllowedAndForbiddenExamples()
        {
            Assert.That(FindUnsharedDerivations(new[] { typeof(SecondOpinionSample) }), Is.Not.Empty,
                "A handler whose probe re-derives its answer through unrelated code must be reported, " +
                "or the rule is inert.");
            Assert.That(FindUnsharedDerivations(new[] { typeof(SharedDerivationSample) }), Is.Empty,
                "A handler whose probe asks the same derivation its execution asks must not be " +
                "reported, or the rule bans the ordinary shape it exists to permit.");
            Assert.That(FindUnsharedDerivations(new[] { typeof(AlwaysAppliesSample) }), Is.Empty,
                "A handler that cannot refuse has no refusal to derive, so it must not be reported.");

            Assert.That(FindPerformingProbes(new[] { typeof(PerformingProbeSample) }), Is.Not.Empty,
                "A probe that commits a displacement must be reported, or the rule is inert.");
            Assert.That(FindPerformingProbes(new[] { typeof(SharedDerivationSample) }), Is.Empty,
                "A probe that only reads must not be reported.");
        }

        [Test]
        public void TheNamedMembers_Resolve()
        {
            Assert.That(PerformingMembers(), Has.Length.EqualTo(18),
                "A named member no longer resolves. Correct the rule rather than deleting it; a rule " +
                "that names nothing protects nothing.");
            // Milestone 6.6a registers p.19's Swap a second time, with the acting hero as its first
            // object, so the count moves by one. Milestone 7.1a registers the first capability that
            // reads card state - the chooser-owned in-hand discard - so it moves by one again, and
            // milestone 7.2b registers the first that reads a printed value - the target selection
            // bounded by rulebook p.9's Radius icon - and 7.2c the first that publishes one, so it
            // moves by one more twice. Milestone 8.10 moves it by one, and it is worth saying which of
            // its two new declarations did: the printed-Radius hero selection did *not*, because it
            // shares the implementation the other three printed-icon declarations use; the capability
            // that reaches rulebook p.13's Defeat list did, because nothing before it declared a
            // consequence. Both are proved on showcase content before the verified card names them,
            // which is what milestone 7.9's deferral gate requires of a hero slice. Milestone 8.11
            // moves it by one more, and again only one of its two new declarations does: the
            // printed-Range hero selection shares the same implementation, and the predicate over the
            // enclosing Attack.s printed subtype is the new one.
            // Milestone 8.13 adds the staged minion-Defeat request and the pre-removal adjacency
            // selector. The Radius-minion declaration shares the existing printed-icon handler.
            // Milestone 8.19a adds two, and both are new contracts rather than new declarations over an
            // existing one: one names the unit an Attack's own text binds as its target, and one
            // publishes which side of a primary action the rest of that text runs on. The milestone's
            // other two identities - `attack-target@2` and `before-action-trigger@1` - are typed
            // declarations on a card and on a rule source, so they register no handler and move nothing
            // here.
            // Milestone 8.20 moves it by three, and it is worth saying which of its identities did and
            // which did not. `choose-target@2`, `behind-target-line@1` and `spaces-adjacent@1` register
            // handlers, and the first two of those were implemented at milestone 8.19b and only now
            // reach the build's catalog, because that is the milestone at which a printed card first
            // names them. The other two new identities register nothing here: `synthetic-place-in-range`
            // and `synthetic-range-minion-target` are second declarations over the placement and
            // printed-icon handlers that already existed, which is exactly what p.9's Range/Radius split
            // asks a second declaration to be. Nor do the milestone's four typed declarations -
            // `restrict-action@2`, `limit-activity@1`, `immunity@2`'s bound-actor carve-out and
            // `place-destination@2` - which are content on a rule or a policy rather than handlers.
            // Milestone 8.21 moves it by seven, and the split is the same one. Five - milestone
            // 8.19b's `attacked-target-space@1`, `choose-charge-line@1`, `hero-defeated-this-round@1`,
            // `retrieve-card@1` and `gain-resource@1` - were implemented then and reach the catalog
            // only now, because Brogan's faces are the first printed cards that name them. Two are this
            // milestone's own: `discard-owned-card@1`, which is the actor spending a card as a price
            // and reporting that they did, and `owned-card-present@1`, which asks whether a hero owns a
            // card in a named place. His four Active effects register nothing new here - all four run
            // on the creation handler milestone 6.8 proved - and neither do this milestone's typed
            // declarations: `MinionsInRadius`, `ControllerAndUnitsInRadius`, `NamedSpace`,
            // `OpposingSourceOrUnaligned` and `choose-target@2`'s played-an-Attack narrowing are
            // content on a scope, a policy or a parameter rather than handlers.
            // Milestone 8.22 moves it by five, and the split is the same one again. Four - milestone
            // 8.19b's `choose-target-set@1`, `choose-target-pair@1`, `presence-count@1` and
            // `pre-consequence-adjacency@1` - were implemented then and reach the catalog only now,
            // because Sabina's faces are the first printed cards that name them. One is this
            // milestone's own: `target-is-adjacent@1`, which asks whether an enclosing Attack's target
            // is adjacent right now, and which exists separately from `pre-consequence-adjacency@1`
            // because her two riders are printed in different tenses. This milestone's typed
            // declarations register nothing here, as ever: `WithinActorRadius`,
            // `EffectRoleKind.OpposingHero`, `choose-target@2`'s friendly-minion-adjacency narrowing,
            // `presence-count@1`'s threshold, the two selections' space outputs and the Ultimate's list
            // of standing rules are content on a policy, a role, a parameter or a declaration rather
            // than handlers. `target-played-attack-this-turn@1` is still not registered even though
            // Sabina is the hero it was minted for - her three `+` Attacks reach the same question
            // through the value pipeline, so no effect node names it.
            // Milestone 8.23 adds five. Four are Tigerclaw's printed faces reaching contracts this
            // build had registered nowhere - `choose-through-target-line` for Blink Strike,
            // `adjacent-terrain-presence` for Blend Into Shadows, `transfer-resource` for his
            // three thieving Greens, and `actor-moved-this-action`, which this milestone minted
            // because Combat Reflexes is the first face to ask at one of p.14's moments about what
            // happened at another. The fifth is `synthetic-place-in-radius`, the printed-Radius twin
            // of a Place, so Blend Into Shadows' reach is raised by p.16's item as its icon says.
            // His Active effect and his two marker gives are three more registrations on top of these,
            // each a definition bound to its own capability the way every hero's Active effects are -
            // and each on a handler *type* the showcase already reaches, which is why they add
            // registrations here without adding implementations.
            // Milestone 8.24 adds NOTHING here, and that is the point worth recording. Xargatha's five
            // persistent texts are five more registrations - three petrification disks and two
            // end-of-round sources - and every one of them is an ActiveEffectCreationHandler, the type
            // milestone 6.8 proved on synthetic content. Her other new vocabulary is content on a
            // parameter, an enum member or a declaration rather than a handler: `RosterMinionClass` on
            // two selection families, `RepeatTargetBinding.DifferentEnemyHero`, the unit selection's
            // `excludeAdjacent` and its space output, and the Ultimate's list of standing rules. So the
            // last hero of the roster lands with no new implementation to review, which is what the
            // dependency slices 8.19a-8.19c1 were for.
            // Milestone 8.35z27 adds exactly one: `attack-sequence@1`, the leaf a modal
            // Attack's "choose one, or both" publishes each chosen bullet's printed targeting sentence
            // through. It is a new handler TYPE rather than another ActiveEffectCreationHandler
            // registration, so unlike 8.24's five it moves this count - and it joins these rules like
            // any other: it performs nothing, and its Anticipate and its Execute read the same
            // declared inputs through one shared reading.
            // MILESTONE 8.70 gave that handler's probe a question its execution does not ask - whether
            // this bullet's own printed sentence names a unit on the board right now - and it joins
            // these rules unchanged, which is worth stating because it is the first probe in the build
            // to read MORE than its execution does. Rule one is satisfied because the two still share
            // the declaration reading they always shared, and that rule is about a probe re-deriving
            // the SAME answer through unrelated code rather than about one asking a further question:
            // the further question belongs to the CHOICE, and the execution deliberately does not
            // re-ask it, because a bullet chosen while legal resolves against the board its own
            // sequence reaches. Rule two is satisfied because the board reading is a query - it
            // commits nothing, mints nothing, and reaches none of the members named below.
            // THE SIX-HERO CONTENT WAVE (milestones 8.35z28 through 8.35z33) adds NOTHING here, and
            // for the reason milestone 8.24 added nothing: the count is of what the build REGISTERS.
            // Every contract the six new heroes declare is DEFERRED until the milestone that deals its
            // hero - which is milestone 7.9's gate working, not an omission - so the wave moves no
            // registration, no corpus digest and no pinned snapshot byte. Each hero's dealing milestone
            // moves this number by its own new handler TYPES, and by nothing for the declarations that
            // are further references over a handler that already exists.
            // Milestone 8.35z29 is the worked example: Misa contributes eight capability declarations
            // and six Active-effect creations, and every one of them is deferred until the milestone
            // that deals her. Four of the
            // eight are new handler types and will move this number by four when she is dealt:
            // `choose-line-destination` and `choose-traversed-unit` for her five Blue Skills, one
            // shared handler behind her two unit-pair selections, and one shared handler behind her
            // Red ranged pair's two tenses. The other two - `synthetic-place-at-maximum-range` and
            // `synthetic-place-in-radius-beside-two-or-more-enemies` - are further declarations over
            // the placement handler that already exists, so they will move it by nothing.
            // MILESTONE 8.35z34 IS THE FIRST OF THOSE DEALING MILESTONES, and it moves this count by
            // FOUR - from 53 to 57 - which is the prediction above proved on the first hero to reach
            // it. Dodger the Warlock's dealing registers SIX capability ids across exactly four new
            // handler types, and the gap between those two numbers is the whole point of counting
            // types: `empty-minion-spawn-points-in-radius@1` and `-adjacent@1` are two declarations
            // over one EmptyMinionSpawnPointCountHandler, and her two Respawns are two declarations
            // over one RespawnFriendlyMinionHandler. The four are therefore that count handler, that
            // Respawn handler, UltimateUnlockedHandler and GainItemFromTheBoxHandler.
            //
            // Her two Active-effect creations move it by NOTHING, and that is the same sentence
            // milestone 8.24 wrote about Xargatha's five: they are further registrations over
            // ActiveEffectCreationHandler, a type this build has had since milestone 6.8.
            //
            // Each of the four joins these rules like any other - each performs its own work, each
            // Anticipate mirrors its Execute against the same live state, and none of them refuses a
            // question it was not asked. The Respawn is the one worth naming: RULING 48 gives it a
            // SECOND question, and it is still one handler under these rules because the second
            // question is asked of the same context the first was answered against.
            // MILESTONE 8.35z38 IS THE SECOND DEALING MILESTONE, and it moves this count by FOUR
            // again - from 57 to 61 - on the same arithmetic and for a different set of reasons.
            // Misa the Samurai's dealing registers EIGHT capability ids across exactly four new
            // handler types: the two tenses of her Red ranged pair share one
            // EnclosingTargetDistanceHandler, her two Swap Skills share one UnitPairHandler, her five
            // Blue Skills bring LineDestinationHandler and TraversedUnitHandler one each - and her TWO
            // PLACE DECLARATIONS BRING NOTHING, being further declarations over PlaceHandler, the type
            // the three arms of Place have registered since milestone 6.6.
            //
            // Her six Active-effect creations move it by nothing on the sentence milestones 8.24 and
            // 8.35z34 already wrote: ActiveEffectCreationHandler is a type this build has had since
            // milestone 6.8. That is now three heroes in a row for whom the sentence is true, which is
            // what makes it a rule about the type rather than a coincidence about the content.
            //
            // Each of the four joins these rules like any other - each performs its own work, each
            // Anticipate mirrors its Execute against the same live state, and none refuses a question
            // it was not asked. The pair selection is the one worth naming: it is the first RELATIONAL
            // constraint in the build - two units at the same distance from the actor - and it is
            // still one handler under these rules because the relation is derived from the same live
            // board each candidate is drawn from.
            //
            // MILESTONE 8.35z46 IS THE THIRD DEALING MILESTONE, and it moves this count by exactly
            // ONE - from 61 to 62 - because only one of Silverarrow's two new-to-her roster contracts
            // is a new handler TYPE. `fast-travel@1` is CardTextFastTravelHandler, a type this build
            // had never registered, so it owed the showcase consumer milestone 8.35z46 added.
            // `choose-line-destination@1` registered already at Misa's dealing, and her three
            // straight-line faces reach that same handler now - which moves this count by nothing,
            // exactly as the rule ledger row 25 above predicts for a shared declaration. Her six
            // Active-effect creations move it by nothing either, on the same sentence three heroes
            // running now have written: ActiveEffectCreationHandler is a type this build has had since
            // milestone 6.8.
            // Milestone 8.35z50 moves it 62 -> 65, and the arithmetic is the dealing's own: Ursafar's
            // registration crosses THREE new handler TYPES - `HeroConditionHandler` (the enraged
            // predicate twelve of his faces read), `BorrowPrimaryActionHandler` (the contract Angry Roar
            // and his two borrowing Greens start) and `AttackTargetSurvivedHandler` (the reading his
            // three removal Reds gate on). His FIFTEEN Active-effect creations move it by nothing, on
            // the sentence four dealings have now written: `ActiveEffectCreationHandler` is a type this
            // build has had since milestone 6.8, and this rule counts TYPES.
            // Milestone 8.35z53 moves it 65 -> 69, and the arithmetic is the dealing's own: Whisper's
            // registration crosses FOUR new handler TYPES - `ChooseTargetTrioHandler` (the up-to-three
            // minion selection her Green line names), `DefendedCardForcedMoveHandler` (Swift Justice's
            // printed rider, the first contract in this build whose choice is issued to a hero who is
            // not the actor), `EnemyHeroWithDiscardPresentHandler` (the discard predicate Death Seeker
            // and Blood Frenzy gate on) and `ChooseSwapSubjectHandler` (the union-selection her two
            // Swaps name). `synthetic-radius-unit-target@1` moves it by NOTHING even though it left the
            // deferred list in the same commit - it is a seventh declaration over
            // `PrintedIconTargetHandler`, and this rule counts TYPES - and her one Active-effect
            // creation moves it by nothing on the sentence five dealings have now written.
            // Milestone 8.37, the twelfth and last dealing, is the largest single move this count has
            // seen. SIX new handler TYPES arrive with Tali's own faces - `SourceCardInDiscardHandler`
            // (the predicate seven of her nineteen read about themselves), `FriendlyHeroInPlayHandler`
            // and `NameColorHandler` (Commune with Spirits' ally-routed naming, the first contract
            // whose colour is a runtime output rather than a composition-time literal),
            // `ChooseDirectionHandler` and `BatchDirectMoveHandler` (Cold Snap's free direction and
            // simultaneous batch) and `DefendedCardColorHandler` (Ice Blast's step-3-to-step-7
            // reading) - and TWO more arrive with her token line, `PlaceHeroTokenHandler` and
            // `ReclaimHeroTokenHandler`, whose showcase instances register from
            // `SyntheticTokenCapabilities` while her six real ones register from her own content.
            // Her six token capability IDS move this by nothing beyond those two types, because this
            // rule counts TYPES. Milestone 8.37a then composed Blizzard, which gives her the one Active
            // effect her set's one `End of turn:` prints - and moves this count by NOTHING, on the
            // sentence six dealings have now written: `ActiveEffectCreationHandler` is the type, and it
            // has been registered since milestone 6.8.
            // Milestone 8.38e, the THIRTEENTH dealing and the first after the package arc, moves it 77 -> 78.
            // Swift the Sharpshooter registers THREE capability ids across exactly ONE new handler
            // TYPE: `SubjectAdjacentTerrainPresenceHandler`, the enemy-scoped reading her three Green
            // faces qualify their target by. Her two straight-line PLACE declarations move it by
            // NOTHING - further declarations over `PlaceHandler`, the type registered since milestone
            // 6.6 - and `borrow-primary-action@1`, which her Reload! names, registered already at
            // Ursafar's dealing, so her reaching it moves this by nothing on rule ledger row 25's own
            // sentence. Her Active-effect creations move it by nothing on the sentence seven dealings
            // have now written.
            // Milestone 8.39i, the FOURTEENTH dealing, moves it 78 -> 80. Cutter the Sky Pirate
            // registers exactly TWO capability ids and both are new handler TYPES, which is the first
            // time since milestone 8.35z46 that the two numbers a dealing moves are equal:
            // `RangeObstacleTargetHandler`, the obstacle selection her Silver basic's grapple names -
            // its own type rather than an eighth arm of `PrintedIconTargetHandler`, because p.18's
            // obstacles include terrain and its candidates are therefore SPACES - and
            // `ClaimSoloVictoryHandler`, the only reader in the build that claims an ending for one
            // hero. Her other nineteen contracts were all registered before she was dealt, which is
            // what thirteen lettered build blocks bought: every one of them is a further reference
            // over a handler that already exists, and this rule counts TYPES. She contributes no
            // Active-effect creation at all - no face of hers prints an Active-effect keyword - so the
            // sentence seven dealings wrote about `ActiveEffectCreationHandler` simply does not arise.
            // Milestone 8.40j, the FIFTEENTH dealing, moves it 80 -> 84. Bain the Bounty Hunter
            // registers ELEVEN capability ids across exactly FOUR new handler TYPES, which is the
            // widest gap between the two numbers any dealing has had: `MarkerTransferHandler` (8.40b)
            // and `MarkerHolderHandler` (8.40c), each also registered a second time over the
            // fixture's own Focus marker as the showcase consumer milestone 7.9's gate asks of a
            // type, and `HiddenCardChoiceHandler` and `RevealChosenCardHandler` (8.40f), the first
            // contracts in this build to hold an answer private and then make a card public. His
            // give, his return, his five Active-effect creations and the other sixteen contracts his
            // set names all run on types that already existed, and this rule counts TYPES.
            // Garrus adds resolved discard and named discarded-primary borrowing (two types).
            // Widget adds owned-token facts, anchored targets and coupled movement (three types).
            // Milestone 8.61j, the NINETEENTH dealing, moves it 89 -> 91. Min the Dragonmonk
            // registers NINETEEN capability ids across exactly TWO new handler TYPES, which is the
            // second-widest gap any dealing has had after Bain's: `BorrowAfterAttackTextHandler`
            // (8.61g), the one contract in this build that names WHICH CARD'S TEXT another card's
            // Attack is about to apply, and `TakeDeckCardAsItemHandler` (8.61h), the first that
            // moves a card out of a deck and onto a dashboard as p.16's item. Each took a showcase
            // consumer in the same change - the Red Understudy and the Red Archivist. Her other
            // seventeen contracts all run on types that already existed: her token operations on
            // `OwnedTokenFactHandler` and `PlaceHeroTokenHandler` (8.35z32, 8.37), her two anchored
            // hero selections on Widget's anchored-target handler, and her four Active-effect
            // creations on `ActiveEffectCreationHandler`, the type Arien's five persistent texts
            // proved at milestone 6.8 - and this rule counts TYPES.
            // Milestone 8.69g, the TWENTIETH dealing, moves it 91 -> 94. Brynn the Seeker registers
            // exactly THREE capability ids and every one of them is a new handler TYPE, which is the
            // first time a dealing's id count and its type count have been equal: nothing of his
            // reuses a contract of his own, because his three questions are asked of three different
            // subjects - a disk of enemy heroes, the target the running Attack bound, and the union
            // of p.19's ring with that disk. `EnemyHeroInRadiusAdjacentToObstaclesHandler` (8.69a),
            // `AttackTargetWasAdjacentToObstaclesHandler` (8.69b) and `TrackingSwapSubjectHandler`
            // (8.69d) each took a showcase consumer in the same change - the Red Surveyor, the Red
            // Quarryman's "After the attack" rider and the Red Pathkeeper. Everything else his
            // eighteen faces name runs on types this build already had - the modal Attack, the
            // printed-Radius selections, the direct move, the swap, the discard, the Defeat and the
            // retrieval - and this rule counts TYPES.
            // Milestone 8.71h, the TWENTY-FIRST dealing, moves it 94 -> 97. Hanu the Trickster
            // registers SIX capability ids of which exactly THREE are new handler TYPES, which is the
            // first dealing since Min's whose two counts differ again - and they differ for the
            // reason her tokens made them differ, not for a new one: three of his six are
            // `ActiveEffectCreationHandler` registrations over his three journeys' Hero-scoped
            // end-of-turn definitions, and that type has been counted since Arien's five persistent
            // texts proved it at milestone 6.8. `HurryUpInitiativeHandler` (8.71a) is the first
            // contract in this build that REPLACES a printed value rather than adding to one;
            // `JourneyPartnerHandler` (8.71d) re-derives "that hero" from the lifecycle at fire time;
            // `FriendlyUnitPairHandler` (8.71f) is one selection over an unordered pair of friendly
            // units in the performing card's own printed Radius. Each took a showcase consumer in
            // this same change - the Red Hastener, the Red Wayfarer and the Red Troupemaster. The
            // Red Wayfarer also registers ONE more id than a showcase card usually does,
            // `synthetic-journey-showcase-effect@1`, and it moves no type for the same reason his
            // three journeys' do not. Everything else his eighteen faces name runs on types this
            // build already had - the modal Attack, the printed-Radius and printed-Range selections,
            // the swap, the coupled move, the direct move and the straight line - and this rule
            // counts TYPES.
            Assert.That(RegisteredHandlerTypes(), Has.Length.EqualTo(97),
                "The registered contract set changed. A new capability joins these rules rather than " +
                "arriving outside them.");
        }

        // -------------------------------------------------------------------------------------------
        // The rules
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindUnsharedDerivations(IEnumerable<Type> handlerTypes) =>
            handlerTypes
                .Select(handler => new
                {
                    Handler = handler,
                    Probe = Reachable(handler, nameof(IEffectCapabilityHandler.Anticipate)),
                    Execution = Reachable(handler, nameof(IEffectCapabilityHandler.Execute))
                })
                .Where(entry => CanRefuse(entry.Execution) && Derives(entry.Execution))
                .Where(entry => !Leaves(entry.Probe).Intersect(Leaves(entry.Execution)).Any())
                .Select(entry =>
                    $"{entry.Handler.FullName}: its probe shares no derivation with its execution")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static IReadOnlyList<string> FindPerformingProbes(IEnumerable<Type> handlerTypes)
        {
            var forbidden = PerformingMembers().Select(Identity).ToArray();
            return handlerTypes
                .SelectMany(handler => Reachable(handler, nameof(IEffectCapabilityHandler.Anticipate))
                    .SelectMany(scanned => scanned.Instructions
                        .Where(instruction => instruction.Member != null)
                        .Where(instruction => forbidden.Contains(Identity(instruction.Member)))
                        .Select(instruction =>
                            $"{handler.FullName}: its probe reaches " +
                            $"{ILConformanceScanner.Describe(instruction.Member)}")))
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Whether this execution path can report a refusal at all. A handler that only ever applies
        /// has nothing for its probe to derive, and requiring a shared read there would only invite a
        /// meaningless one.
        /// </summary>
        private static bool CanRefuse(IReadOnlyList<ILConformanceScanner.ScannedMethod> execution) =>
            execution.Any(scanned =>
                scanned.Calls(ResultFactory(nameof(EffectExecutionResult.ImpossibleMandatory))) ||
                scanned.Calls(ResultFactory(nameof(EffectExecutionResult.DeclinedOptional))));

        /// <summary>
        /// Whether this execution reads anything to decide with, counting the result factories as the
        /// answer rather than as a reading. A handler that refuses unconditionally - the resolver
        /// fixture's `synthetic-impossible`, whose whole purpose is to be impossible - states its
        /// answer as a constant in both places, and two identical constants cannot disagree.
        /// </summary>
        private static bool Derives(IReadOnlyList<ILConformanceScanner.ScannedMethod> execution)
        {
            var answers = new HashSet<string>(
                typeof(EffectExecutionResult)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(Identity));
            return Leaves(execution).Any(leaf => !answers.Contains(leaf));
        }

        /// <summary>
        /// The members a call graph reads, minus the methods the walk itself stepped through. Without
        /// that subtraction a probe and an execution would look unrelated purely because each named its
        /// own entry point, and the shape this rule most wants to permit - a probe and an execution
        /// that both call one shared derivation - is exactly that shape.
        /// </summary>
        private static IReadOnlyList<string> Leaves(
            IReadOnlyList<ILConformanceScanner.ScannedMethod> reachable)
        {
            var walked = new HashSet<string>(reachable.Select(scanned => Identity(scanned.Method)));
            return reachable
                .SelectMany(scanned => scanned.Instructions)
                .Where(instruction => instruction.Member != null)
                .Select(instruction => Identity(instruction.Member))
                .Where(identity => !walked.Contains(identity))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Every method body reachable from one of a handler's members, following calls into types the
        /// handler is declared beside - its own body, its nested types, and the static class that holds
        /// the routines its declarations share. That boundary is what makes the walk finite while still
        /// crossing the one hop the shared push and place routines live behind.
        /// </summary>
        private static IReadOnlyList<ILConformanceScanner.ScannedMethod> Reachable(
            Type handlerType, string entryPointName)
        {
            var reached = ILConformanceScanner.ReachableFrom(
                handlerType,
                entryPointName,
                ILConformanceScanner.WithNestedTypes(handlerType)
                    .Concat(ILConformanceScanner.WithNestedTypes(handlerType.DeclaringType))
                    .Distinct());

            Assert.That(reached, Is.Not.Empty,
                $"{handlerType.Name} declares no {entryPointName}; a rule cannot judge a member that " +
                "does not exist.");
            return reached;
        }

        private static Type[] RegisteredHandlerTypes()
        {
            var catalog = GuardsDigital.Core.Cards.MatchContentCatalogs.ForThisBuild().Capabilities;
            return catalog.Registered
                .Select(reference => catalog.Load(reference).GetType())
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Everything a probe must never reach: the seven board and component commits, the two identity
        /// mints, the two arms of the shared Place operation, milestone 6.6's three board-object
        /// operations, the marker register's two transitions, and the lifecycle creation. Each is named
        /// as a resolved member rather than a string.
        /// </summary>
        private static MemberInfo[] PerformingMembers() => new MemberInfo[]
        {
            ILConformanceScanner.Method(
                typeof(IBoardDisplacementAuthority),
                nameof(IBoardDisplacementAuthority.CommitValidatedDisplacement)),
            ILConformanceScanner.Method(
                typeof(IUnitSpawnAuthority), nameof(IUnitSpawnAuthority.CommitValidatedUnitSpawn)),
            ILConformanceScanner.Method(
                typeof(ITokenPlacementAuthority),
                nameof(ITokenPlacementAuthority.CommitValidatedTokenPlacement)),
            ILConformanceScanner.Method(
                typeof(ITokenPlacementAuthority),
                nameof(ITokenPlacementAuthority.CommitValidatedTokenReclamation)),
            ILConformanceScanner.Method(
                typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Give)),
            ILConformanceScanner.Method(
                typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Return)),
            // Milestone 8.40b's third mutating marker operation. It is named here at the milestone
            // that builds it rather than at the one that registers a handler reaching it, because the
            // rule protects nothing it does not name: the give already reaches it on BA-D1's move arm,
            // and that handler is registered today.
            ILConformanceScanner.Method(
                typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Transfer)),
            ILConformanceScanner.Method(
                typeof(ActiveEffectLifecycle), nameof(ActiveEffectLifecycle.Create)),
            ILConformanceScanner.Method(
                typeof(ActiveEffectInstanceIdentity), nameof(ActiveEffectInstanceIdentity.Mint)),
            ILConformanceScanner.Method(
                typeof(MarkerInstanceIdentity), nameof(MarkerInstanceIdentity.Mint)),
            ILConformanceScanner.Method(
                typeof(PlaceOperation), nameof(PlaceOperation.PlaceFromBoard)),
            ILConformanceScanner.Method(
                typeof(PlaceOperation), nameof(PlaceOperation.PlaceFromSupply)),
            ILConformanceScanner.Method(
                typeof(IBoardObjectExchangeAuthority),
                nameof(IBoardObjectExchangeAuthority.CommitValidatedSwap)),
            ILConformanceScanner.Method(
                typeof(IBoardObjectExchangeAuthority),
                nameof(IBoardObjectExchangeAuthority.CommitValidatedRemoval)),
            ILConformanceScanner.Method(
                typeof(IBoardObjectExchangeAuthority),
                nameof(IBoardObjectExchangeAuthority.CommitValidatedTokenReplacement)),
            ILConformanceScanner.Method(typeof(SwapOperation), nameof(SwapOperation.Swap)),
            ILConformanceScanner.Method(
                typeof(ReplaceOperation), nameof(ReplaceOperation.ReplaceToken)),
            ILConformanceScanner.Method(typeof(RemoveOperation), nameof(RemoveOperation.Remove))
        };

        private static MethodInfo ResultFactory(string name) =>
            ILConformanceScanner.Method(typeof(EffectExecutionResult), name);

        private static string Identity(MemberInfo member)
        {
            try
            {
                return $"{member.Module.ModuleVersionId:N}:{member.MetadataToken:X8}";
            }
            catch (InvalidOperationException)
            {
                return $"{member.DeclaringType?.FullName}.{member.Name}";
            }
        }

        // -------------------------------------------------------------------------------------------
        // Samples. None is registered; each exists so the rules above are shown to have teeth.
        // -------------------------------------------------------------------------------------------

        /// <summary>Deliberate violation: its probe answers from a rule its execution never asks.</summary>
        private sealed class SecondOpinionSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                Targets(context).Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.RoundIndex > 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>Compliant twin: both ask the one derivation.</summary>
        private sealed class SharedDerivationSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                Targets(context).Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Targets(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        /// <summary>Outside the derivation rule: it can never refuse, so it derives no refusal.</summary>
        private sealed class AlwaysAppliesSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>Deliberate violation: its probe performs the displacement it was asked about.</summary>
        private sealed class PerformingProbeSample : SampleHandler
        {
            public override EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                context.Components.Board.CommitValidatedDisplacement(
                    default, default);
                return EffectExecutionResultKind.Applied;
            }
        }

        private abstract class SampleHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference =>
                new EffectCapabilityReference(new EffectCapabilityId("conformance-sample"), 1);

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public abstract EffectExecutionResult Execute(EffectCapabilityContext context);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A sample never requests a choice.");

            public abstract EffectExecutionResultKind Anticipate(EffectCapabilityContext context);

            /// <summary>The shared derivation the samples differ in their use of.</summary>
            protected static IReadOnlyList<HeroId> Targets(EffectCapabilityContext context) =>
                context.Participants == null
                    ? Array.Empty<HeroId>()
                    : context.Participants.Heroes.Select(hero => hero.Id).ToArray();
        }
    }
}
