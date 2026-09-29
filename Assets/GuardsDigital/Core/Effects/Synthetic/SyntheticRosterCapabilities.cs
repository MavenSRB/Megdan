using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.19b's reusable roster vocabulary. These are synthetic consumers over shared typed
    /// contracts; no verified hero definition is registered here.
    /// </summary>
    public static class SyntheticRosterCapabilities
    {
        public static EffectCapabilityReference ChooseTargetV2 => Reference("choose-target", 2);
        public static EffectCapabilityReference ChooseTargetSetV1 => Reference("choose-target-set", 1);
        public static EffectCapabilityReference ChooseTargetPairV1 => Reference("choose-target-pair", 1);
        public static EffectCapabilityReference BehindTargetLineV1 => Reference("behind-target-line", 1);
        public static EffectCapabilityReference ChooseChargeLineV1 => Reference("choose-charge-line", 1);

        /// <summary>
        /// Milestone 8.35z30. <c>choose-line-destination@1</c> - where a straight-line
        /// movement written in card text ends. Silverarrow's Divert Attention and Lead Astray, "move
        /// up to that number of spaces in a straight line", and Wild Hunt's "you may move 2 spaces in
        /// a straight line".
        ///
        /// <para><b>It is not <see cref="ChooseChargeLineV1"/> with a flag.</b> Brogan's charge offers
        /// only the spaces with an enemy unit beside them, because his sentence prints that clause;
        /// these sentences print no clause at all, so every space the line reaches is offered. A
        /// parameter that switched the clause off would put a card's words into a capability's
        /// options.</para>
        ///
        /// <para><b>The path is walked, not filtered.</b> "In a straight line" is a statement about
        /// how the hero travels, not only about where they end - so the walk keeps a route whose every
        /// step is the same p.19 direction, and a space reachable only by turning a corner is not
        /// offered even though it lies on a file. That is the same test Brogan's charge applies, and
        /// it is the shared <see cref="IsStraightRoute"/> both reach.</para>
        ///
        /// <para><b>Obstacles are the declaring card's business.</b> The parameter is optional and
        /// defaults to walking p.18's obstacles the way every Move did before milestone 8.23:
        /// Silverarrow's three faces print no crossing. A card whose own text crosses obstacles - Misa's
        /// five Blue Skills, "ignoring obstacles" - declares it, which is why the parameter is here at
        /// all rather than being hard-coded either way.
        ///
        /// <para><b>Both halves of p.18's obstacle rule are the movement query's, and neither is
        /// restated here.</b> The crossing permission is handed to the transit predicate; the endpoint
        /// predicate is offered no permission at all, by p.18's own second sentence, which is what keeps
        /// the user's 2026-09-06 ruling on Misa's line - through any obstacle including the map's water
        /// ring, "but as long as hero ends up on a playable space" - true without this handler knowing
        /// what terrain is. Off the map is not an obstacle to cross but the absence of a space, so a
        /// line aimed across the ring at open water simply has no destination there.</para>
        ///
        /// <para><b>ONE declaration, settled at the six-hero content wave.</b> Milestones 8.35z29 and
        /// 8.35z30 minted this id independently against the same output; this is 8.35z30's, the strict
        /// superset, and 8.35z29's five Blue nodes bind the parameter rather than carrying a second
        /// capability with one id.</para>
        /// </summary>
        public static EffectCapabilityReference ChooseLineDestinationV1 =>
            Reference("choose-line-destination", 1);

        /// <summary>
        /// Milestone 8.35z30. <c>fast-travel@1</c> - rulebook p.11's Fast Travel invoked by
        /// card text rather than taken as p.12's replacement action. Silverarrow's Trailblazer, "You
        /// may fast travel, if able", and her two Gold basics' "If able, you may fast travel to an
        /// adjacent zone".
        ///
        /// <para><b>Why a capability and not the existing action.</b> p.12's Fast Travel is an action a
        /// player takes <em>instead of</em> a Movement, and everything about how it is offered says so:
        /// it replaces a named Movement action on a named card, and the whole offer is validated as
        /// that replacement. These three faces perform one from inside card text while another action
        /// is already running - Trailblazer's own Skill, and the Gold cards' Attack at p.14's step 7 -
        /// so there is no Movement being replaced and nothing for the replacement query to
        /// validate.</para>
        ///
        /// <para><b>What is shared is the rule, not the framing.</b> Where a Fast Travel may finish is
        /// <see cref="FastTravelActionQuery.EvaluateDestinationForActor"/> for both, so a destination
        /// this capability offers is exactly one the replacement action would have accepted. And
        /// whether the hero may Fast Travel at all is asked the same way the offer asks it - p.18's
        /// restriction on <see cref="ActionKind.FastTravel"/>, read against the colour of the card the
        /// text is printed on - so Silverarrow's own Grasping Roots stops her Trailblazer exactly as it
        /// stops an opponent's replacement action.</para>
        /// </summary>
        public static EffectCapabilityReference FastTravelV1 => Reference("fast-travel", 1);
        public static EffectCapabilityReference ChooseThroughTargetLineV1 =>
            Reference("choose-through-target-line", 1);

        /// <summary>
        /// Milestone 8.35z29. `choose-traversed-unit@1` - one of the units a straight-line movement
        /// passed through. Misa's five Blue Skills: "you may place an enemy unit you moved through into
        /// a space adjacent to you", and "an enemy hero you moved through discards a card, if able".
        ///
        /// <para><b>The set is re-derived from the two ends, not captured during the walk.</b> A
        /// straight line between two spaces has exactly one interior, so the spaces Misa passed through
        /// are arithmetic rather than history - and asking the board who is standing on them is the
        /// same re-derivation every selection in this catalog performs at offer, at answer and at a
        /// restored pause. The forecast for this hero predicted a captured traversal record on the
        /// grounds that a later node might displace someone; the measurement is that nothing between
        /// the movement and this selection moves anybody, and a captured set would have been a second
        /// source of truth that a restored pause could disagree with.</para>
        ///
        /// <para><b>Both ends are bound rather than read off the actor.</b> By the time this is asked
        /// Misa is standing on the destination, so her current space answers only half the question;
        /// the origin is published by the actor-space bridge before the movement, which is the same
        /// ordering Tigerclaw's crossing line uses.</para>
        ///
        /// <para>The unit kind is the declaration's, because the two clauses differ in exactly that
        /// word - "an enemy unit" against "an enemy hero" - and in nothing else.</para>
        /// </summary>
        public static EffectCapabilityReference ChooseTraversedUnitV1 =>
            Reference("choose-traversed-unit", 1);

        /// <summary>
        /// Milestone 8.35z29. `choose-unit-pair-at-maximum-radius@1` - Misa's Living Tornado, "Swap
        /// two units at maximum radius".
        ///
        /// <para><b>The threshold is the printed icon read as a ring, and the ring is in the name.</b>
        /// The user's 2026-09-06 ruling settles "at maximum" as exactly the printed number, so both
        /// members must stand at exactly the card's Radius - p.9's icon with p.16's matching item
        /// composed onto it, which is why a +1 Radius item moves the whole ring outward rather than
        /// widening it into a disk.</para>
        ///
        /// <para><b>Why it is not <see cref="ChooseTargetPairV1"/> with a distance.</b> That pair takes
        /// minions only, measures a disk, and is handed a written-down number; this one takes p.19's
        /// units - "two units", either side, Misa excluded by p.10's Picking Targets - and measures a
        /// printed ring. Four differences is a different question.</para>
        /// </summary>
        public static EffectCapabilityReference ChooseUnitPairAtMaximumRadiusV1 =>
            Reference("choose-unit-pair-at-maximum-radius", 1);

        /// <summary>
        /// Milestone 8.35z29. `choose-unit-pair-at-equal-distance-in-radius@1` - Misa's Storm
        /// Spirit, "Swap two units in radius and at equal distance from you".
        ///
        /// <para><b>This is the build's first <em>relational</em> pair constraint.</b> Every selection
        /// before it filters each candidate on its own: a member is admitted or it is not, and the set
        /// is the same whichever member you look at first. Here the second member's admissible set
        /// depends on the first - only units at the same distance as whoever was chosen - so the
        /// question is about the pair rather than about its members.</para>
        ///
        /// <para><b>It is nevertheless answered as one bounded pair rather than as two questions.</b>
        /// The candidates offered are every unit standing at a distance <em>some other admissible unit
        /// also stands at</em>, and the answer is checked against the same derivation: a pair whose
        /// members disagree is refused rather than never offered, because offering only the first
        /// member's partners would mean deriving the offer from an answer that has not been given. That
        /// keeps the offer honest under milestone 8.35l's rule - every unit shown can be half of a
        /// legal pair - while leaving the pairing itself to the answer.</para>
        ///
        /// <para>Under the ruling on "at maximum radius" this card is a genuine widening of Living
        /// Tornado rather than a narrowing: the Tier II swaps only on the outermost ring, and this
        /// Tier III swaps on any ring at all, which is the upgrade direction p.15 expects.</para>
        /// </summary>
        public static EffectCapabilityReference ChooseUnitPairAtEqualDistanceInRadiusV1 =>
            Reference("choose-unit-pair-at-equal-distance-in-radius", 1);

        /// <summary>
        /// Milestone 8.35z29. `target-was-at-maximum-range@1` - Misa's Power Shot, "After the
        /// attack: If the target <b>was</b> at maximum range, you may move it 1 space".
        ///
        /// <para><b>The tense is the whole capability.</b> The user's 2026-09-06 ruling settles that
        /// Power Shot's past tense is deliberate and judged at targeting time, so this reads the two
        /// spaces p.14 captured when the Attack bound its target - where the target stood and where the
        /// attacker stood when the action began - and never the live board. Between step 2 and step 7
        /// a Defense card's text can move either of them, and that movement is exactly what the past
        /// tense excludes.</para>
        ///
        /// <para>"Maximum range" is the printed Range icon with p.16's items composed on, read exactly
        /// as every other printed-icon consumer reads it, and "at" is the same equality the user's
        /// ruling gives the phrase everywhere else in her set.</para>
        /// </summary>
        public static EffectCapabilityReference TargetWasAtMaximumRangeV1 =>
            Reference("target-was-at-maximum-range", 1);

        /// <summary>
        /// Milestone 8.35z29. `target-is-not-adjacent@1` - Misa's Thunder Shot, "After the attack:
        /// If the target <b>is</b> not adjacent to you, you may move it 1 space".
        ///
        /// <para><b>It is the live mirror of <see cref="TargetIsAdjacentV1"/> and not that capability
        /// negated by a branch, because a definition should read like the sentence it composes.</b> The
        /// present tense puts the question at p.14's step 7, by which time step 6 may have removed the
        /// target entirely - and a piece standing nowhere is not adjacent to anything, so a removed
        /// target answers true here and the optional push that follows finds nothing to move.</para>
        ///
        /// <para><b>Why the pair of cards needs two capabilities at all.</b> At base Range 2 and under
        /// the ruling on "at maximum range", Power Shot's condition (exactly 2) and Thunder Shot's
        /// (2, since 2 is the maximum) describe the same set - so the two cards look identical until a
        /// p.16 +1 Range item makes them Range 3, at which point Thunder Shot covers 2 and 3 while
        /// Power Shot covers only 3. That divergence is the Tier III card's whole purchase, and it
        /// exists only because the two questions are asked separately.</para>
        /// </summary>
        public static EffectCapabilityReference TargetIsNotAdjacentV1 =>
            Reference("target-is-not-adjacent", 1);

        public static EffectCapabilityReference FormerTargetSpaceV1 =>
            Reference("attacked-target-space", 1);
        public static EffectCapabilityReference PlayedAttackThisTurnV1 =>
            Reference("target-played-attack-this-turn", 1);
        public static EffectCapabilityReference HeroDefeatedThisRoundV1 =>
            Reference("hero-defeated-this-round", 1);
        public static EffectCapabilityReference PresenceCountV1 => Reference("presence-count", 1);
        public static EffectCapabilityReference PreConsequenceAdjacencyV1 =>
            Reference("pre-consequence-adjacency", 1);

        /// <summary>
        /// Milestone 8.22. `target-is-adjacent@1` - whether the enclosing Attack's target is adjacent
        /// to the actor <em>right now</em>, which is Sabina's Point Blank Shot: "After the attack: If
        /// the target is adjacent to you, push it 1 space."
        ///
        /// <para><b>It is a different question from <see cref="PreConsequenceAdjacencyV1"/>, and the
        /// difference is the whole reason it exists.</b> That one reads the space the target stood on
        /// before p.14's consequences, because Shootout's sentence is printed in the past tense - "If
        /// the target <em>was</em> adjacent to you" - and asks about the moment the Attack landed. This
        /// one is printed in the present tense and is read at step 7, by which time step 6 may have
        /// removed the target entirely. A piece that is not on the board is not adjacent to anything,
        /// so a removed target makes this false and the push that depends on it never runs.</para>
        ///
        /// <para>Reading current occupancy rather than the captured space is what makes that true. The
        /// two answers coincide for a target that survived - an Attack does not move its defender - so
        /// the only case that separates them is the one the tenses separate, and each face gets the
        /// answer its own words ask for. Neither capability resurrects anything: this one reports a
        /// removed target as not adjacent, and an unconditional push at a vacated space finds no object
        /// and reports itself infeasible.</para>
        ///
        /// <para>The target may be a hero or a minion, because p.13 lets an Attack take either, and
        /// the answer is a plain boolean rather than a coordinate: what needs the space is the Push,
        /// and <c>attacked-target-space@1</c> already publishes it.</para>
        /// </summary>
        public static EffectCapabilityReference TargetIsAdjacentV1 =>
            Reference("target-is-adjacent", 1);

        /// <summary>
        /// Milestone 8.20's `was-adjacent@1`, which the roster matrix names beside milestone 8.19b's
        /// <see cref="PreConsequenceAdjacencyV1"/> and which is a different question.
        ///
        /// <para>That one asks about the target of an enclosing Attack before p.14's consequences ran,
        /// which is what Sabina's Shootout prints. Arien's Ebb and Flow is a Skill and has no enclosing
        /// Attack at all: "Swap with an enemy minion in range; <b>if it was adjacent to you</b>, may
        /// repeat once." Its "was" is the moment before its own Swap, and p.19's Swap exchanges exactly
        /// two spaces - so the two spaces the Swap traded <em>are</em> where the minion and the hero
        /// were standing, and asking whether they are adjacent is asking the card's question rather
        /// than a reconstruction of it.</para>
        ///
        /// <para>Stated as a question about two spaces rather than about a swap, because that is all it
        /// reads: no board, no participants, no declaration. The past tense the face prints is what
        /// makes the sentence readable at all - by the time it is asked, the swap has already moved the
        /// minion into the hero's former space - and a predicate over the two exchanged spaces is
        /// unaffected by that, which is why it can be asked afterwards.</para>
        /// </summary>
        public static EffectCapabilityReference SpacesAdjacentV1 => Reference("spaces-adjacent", 1);

        /// <summary>
        /// Milestone 8.23. `actor-moved-this-action` - whether the attacking hero is standing
        /// somewhere other than where this Attack started.
        ///
        /// <para>Tigerclaw's Combat Reflexes is the only face that asks: "Before the attack: You may
        /// move 1 space. Target a unit adjacent to you. After the attack: <b>If you did not move
        /// before the attack</b>, you may move 1 space." The two clauses are p.14's step 1 and step 7,
        /// and the frame that offered the first move has completed and been discarded long before the
        /// second clause is reached - so the question cannot be asked of that frame and is asked of
        /// the Attack instead.</para>
        ///
        /// <para>It reports a plain boolean and reads two spaces: the one
        /// <c>ActorSpaceAtActionStart</c> recorded and the one the hero occupies now. An Attack whose
        /// facts carry no starting space - every Attack outside this milestone's content - answers
        /// false, which is the honest reading of "nothing recorded a move".</para>
        /// </summary>
        public static EffectCapabilityReference ActorMovedThisActionV1 =>
            Reference("actor-moved-this-action", 1);
        /// <summary>
        /// Milestone 8.38d. The same question asked of a SUBJECT rather than of the actor - Swift's
        /// Suppress, Pin Down and Killing Ground, "An enemy hero in radius, <b>who is not adjacent to
        /// terrain</b>, discards a card".
        ///
        /// <para><b>It is a second capability rather than a parameter on
        /// <see cref="AdjacentTerrainPresenceV1"/>, and this catalog's own rule is why:</b> a shared
        /// declaration gaining a field moves every hero who already referenced it, and Tigerclaw's
        /// Blend Into Shadows references that one. It is also a genuinely different question. His asks
        /// whether HE stands beside terrain, as the condition for a Place he then makes; hers qualifies
        /// which enemy her Skill may reach. One is about the actor's own footing and the other is a
        /// property of a candidate.</para>
        ///
        /// <para><b>Milestone 8.38's audit mapped her three Green faces onto the actor-scoped
        /// declaration and was wrong to</b> - the capability existed and answered a different
        /// question. Found at composition, which is where this project expects such things to surface,
        /// and recorded rather than papered over.</para>
        ///
        /// <para>The terrain question itself is unchanged: <c>TerrainPresenceQuery</c> answers both, so
        /// a hero petrified into terrain is terrain to stand beside for hers exactly as for his.</para>
        /// </summary>
        public static EffectCapabilityReference SubjectAdjacentTerrainPresenceV1 =>
            Reference("subject-adjacent-terrain-presence", 1);

        /// <summary>The hero whose footing is being asked about - a required parameter, never the actor.</summary>
        public const string TerrainSubjectParameter = "subject";

        /// <summary>
        /// Milestone 8.38e. Inverts the answer, on the convention
        /// <see cref="Effects.Synthetic.SyntheticConditionCapabilities"/>' own predicates set: a
        /// negation is a property of the QUESTION, never of the gate that reads it.
        /// </summary>
        public const string SubjectTerrainNegateParameter = "negate";

        public static EffectCapabilityReference AdjacentTerrainPresenceV1 =>
            Reference("adjacent-terrain-presence", 1);
        /// <summary>
        /// Milestone 8.21. `discard-owned-card@1` - the acting hero pays a card of their own choosing,
        /// and says whether they paid.
        ///
        /// <para>It is a second discard capability rather than a revision of
        /// <c>synthetic-discard-chosen-card</c> for the reason that one has two live revisions: they
        /// are different contracts. That capability makes <em>another</em> hero give a card up, which
        /// is why its chooser is the hero its input names and why milestone 8.10's revision reports
        /// inability - the sentence it serves has a second arm for a hero who cannot pay. This one is
        /// the actor spending their own card as a price, printed on Brogan's Throwing Axe as "You may
        /// discard a card; if you do, target a unit in range", and what its sentence needs to know is
        /// the opposite fact: whether the price <em>was</em> paid, so the clause after the semicolon
        /// can be conditioned on it.</para>
        ///
        /// <para>Publishing that as a positive fact is deliberate. A gate written on the inability
        /// would read true both for a hero who declined and for one who had nothing, and would need
        /// inverting to say what the card says; <c>didDiscard</c> is the "if you do" itself. Declining
        /// publishes nothing at all, which the conditional reads as the same false - so a declined
        /// step and an unpayable one reach the identical outcome the printed text gives them, which is
        /// that no target is named and p.10 stops the Attack at that step.</para>
        /// </summary>
        public static EffectCapabilityReference DiscardOwnedCardV1 =>
            Reference("discard-owned-card", 1);

        /// <summary>
        /// Milestone 8.21. `owned-card-present@1` - whether one hero owns a card in a named place.
        ///
        /// <para>Brogan's Throwing Spear is the sentence that needs it: "You may discard a card. If you
        /// have a card in the discard, target a unit in range." The Tier III card is paid for by a card
        /// <em>already</em> in the discard, which is what makes its own discard optional where the Tier
        /// II card's is the price - so the question is about the pile rather than about the step that
        /// just ran, and it is asked after that step so a card discarded to it counts.</para>
        /// </summary>
        public static EffectCapabilityReference OwnedCardPresentV1 =>
            Reference("owned-card-present", 1);

        /// <summary>
        /// Milestone 8.37. Optional, defaulting to unbound - the reading every
        /// composition before Guardian Spirit and Warrior Spirit already has. When bound, this
        /// capability's candidate set narrows to exactly the one named card, and since a chooser facing
        /// one legal answer has nothing to decide - the same reading milestone 6.5a gives "yourself" and
        /// milestone 8.21's <see cref="DirectMoveDestinationPolicy.NamedSpace"/> gives an unambiguous
        /// destination - it retrieves that card directly rather than opening a one-candidate prompt.
        ///
        /// <para>"If Guardian Spirit is in your discard, retrieve it" and Warrior Spirit's identical
        /// second sentence are the first printed text naming a SPECIFIC card rather than asking which
        /// one: retrieve-card@1 always offered a choice among every eligible card before this, which is
        /// the right reading for Silverarrow's Blessing and the wrong one for a card that names
        /// itself.</para>
        /// </summary>
        public const string RestrictToCardParameter = "restrictToCard";

        /// <summary>
        /// Milestone 8.69e. Whether a retrieval admits only p.9's ATTACK CARDS - Brynn the Seeker's
        /// True Grit and Die Hard, "You may retrieve a <b>discarded attack card</b>".
        ///
        /// <para><b>p.9 defines the noun and this reads that definition rather than a second one:</b>
        /// "a card with an Attack primary action is referred to as an Attack card". So the question is
        /// asked of the candidate's printed primary action and of nothing else - a Ranged Attack is an
        /// Attack, a Skill primary is not, and the band printed above the icon decides nothing. For
        /// Brynn that admits Familiar Ground and his five Reds and refuses his twelve Blues and
        /// Greens, his Decoy and his Ultimate (BR-Q8).</para>
        ///
        /// <para><b>It is a parameter and not a fourth <see cref="RosterCardRetrievalKind"/>.</b> That
        /// enum answers WHERE a retrievable card may rest - the discard pile, or the played-and-
        /// resolved Basic Skill milestone 6.5a's faces reach - and this answers WHAT is printed on it.
        /// A member combining the two would need one per pairing the moment a second face printed a
        /// different place, which is the multiplication this catalog's rule 1 refuses; a filter that
        /// narrows whatever place the kind named costs one declaration for every kind at once.</para>
        ///
        /// <para><b>A Boolean rather than an action-kind Integer</b>, on the rule milestone 8.57c
        /// settled: an unbound optional Integer reads as ZERO, and zero is
        /// <see cref="ActionKind.None"/> - a retrieval that forgot to bind it would ask for a card
        /// whose primary is nothing at all and admit none. Optional and false where nothing sets it, so
        /// Silverarrow's Blessing, Rowenna's four retrievals, Widget's and Garrus's keep their exact
        /// candidate sets without naming it, and an added parameter moves no pinned byte because the
        /// identity walker digests a capability's reference and its Active-effect definitions and never
        /// its parameter schema.</para>
        /// </summary>
        public const string AttackCardsOnlyParameter = "attackCardsOnly";

        public static EffectCapabilityReference RetrieveCardV1 => Reference("retrieve-card", 1);
        public static EffectCapabilityReference GainResourceV1 => Reference("gain-resource", 1);
        public static EffectCapabilityReference TransferResourceV1 => Reference("transfer-resource", 1);

        /// <summary>
        /// Milestone 8.39g1. `claim-solo-victory@1` - Cutter's A Fistful of Coins, "If you have 13 or
        /// more coins, <b>you alone win the game</b>", and the only sentence in the corpus that claims
        /// an ending for one hero.
        ///
        /// <para><b>It sits in this file because the question is a coin balance</b>, which is the
        /// round-owned ledger <c>gain-resource@1</c> above already writes through the same
        /// <see cref="EffectRosterState"/>. Milestone 8.39g1 put the claim itself on that state beside
        /// the ledger for the same reason, so this handler reads one thing and writes one thing and
        /// reaches nothing else.</para>
        ///
        /// <para><b>The threshold is the card's own number, declared rather than constant.</b> Thirteen
        /// is printed on A Fistful of Coins, so it is bound at the call site exactly as every printed
        /// distance and radius in this catalog is - and a second card printing a different number costs
        /// a binding rather than a capability.</para>
        ///
        /// <para><b>Below the threshold it applies and does nothing</b>, which is the printed sentence
        /// rather than a softening of it: "If you have 13 or more coins" is a condition on a clause the
        /// card always reaches, so a hero holding twelve has resolved the whole face correctly. The
        /// published boolean is what a later node would read, and the node needs no Conditional in
        /// front of it because there is nothing after it to gate.</para>
        ///
        /// <para><b>The winner is the CONTROLLER and not the actor.</b> "You" on a card is the hero
        /// whose card it is; a borrowed or granted performance is still that hero winning, and reading
        /// the actor would hand the match to whoever happened to be performing.</para>
        /// </summary>
        public static EffectCapabilityReference ClaimSoloVictoryV1 =>
            Reference("claim-solo-victory", 1);

        /// <summary>The printed count at or above which the claim is taken. See <see cref="ClaimSoloVictoryV1"/>.</summary>
        public const string SoloVictoryThresholdParameter = "threshold";

        /// <summary>Whether this resolution claimed the match. See <see cref="ClaimSoloVictoryV1"/>.</summary>
        public const string SoloVictoryClaimedOutput = "claimed";

        /// <summary>
        /// Milestones 8.35z28 and 8.35z33. `empty-minion-spawn-points-in-radius@1` - how many empty
        /// minion Spawn points of the current Battle Zone stand inside the acting card's own printed
        /// Radius disk, and whether that count reaches the number the card asks for.
        ///
        /// <para><b>ONE declaration, settled at the six-hero content wave.</b> Both milestones minted
        /// this id against the same output; this is 8.35z33's, the strict superset, whose one extra
        /// optional parameter 8.35z28's faces simply do not set.</para>
        ///
        /// <para>Dodger's Dark Ritual and Darker Ritual name it: "If there are 2 or more empty spawn
        /// points in radius in the battle zone, gain N coins." Her three Blue Defense primaries read
        /// the same quantity through the value pipeline instead - one printed phrase, one derivation,
        /// reached two ways because a Defense value is a number and a ritual is a step.</para>
        ///
        /// <para>Whisper's Red line names it three times and reads the answer two different ways.
        /// Crimson Trail asks whether there is one at all - "You may move up to 1 space <em>if there
        /// is</em> an empty minion spawn point in radius in the battle zone" - which is the boolean.
        /// Sanguine Path and Blood Pilgrimage ask how many - "up to 1 space <em>for every</em> empty
        /// minion spawn point in radius in the battle zone up to a maximum of 2 / 3 spaces" - which is
        /// the count, and the maximum the same sentence prints is
        /// <see cref="MaximumCountedParameter"/>.</para>
        ///
        /// <para>The quantity itself is
        /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/>'s: one
        /// printed phrase, one derivation, which is the defect milestone 6.5ad was written to correct.
        /// Rulebook p.19's Spawn Point entry is broader than the phrase - "Spaces on the board used to
        /// spawn minions <em>and heroes</em>" - and the 2026-09-06 ruling reads the printed word
        /// "minion" as narrowing it, which printed p.14's own worked example of Dodger's Vampiric
        /// Shield II counts the same way.</para>
        ///
        /// <para><b>Why it is not <c>presence-count@1</c> with a different unit kind.</b> That
        /// capability counts <em>units</em> - pieces standing on the board, filtered by effective kind
        /// and by side. A Spawn point is not a unit, has no side in the sense p.19's Friendly entry
        /// means, and is a property of the map rather than of the pieces on it; the emptiness that
        /// makes it qualify is the <em>absence</em> of a unit, which is the opposite question. Widening
        /// the presence count to answer it would have made one capability read two unrelated
        /// registers.</para>
        ///
        /// <para><b>The disk is p.9's printed Radius with p.16's item composed onto it</b>, read off
        /// the card this node is inside - the same total the printed-icon selections read, and the
        /// reason this is a separate declaration from the adjacency twin below rather than a distance
        /// parameter: p.16 raises a printed icon and never a number a sentence writes down, so one
        /// declaration spanning both would let a card with no Radius icon read a Radius item.</para>
        /// </summary>
        public static EffectCapabilityReference EmptyMinionSpawnPointsInRadiusV1 =>
            Reference("empty-minion-spawn-points-in-radius", 1);

        /// <summary>
        /// Milestone 8.35z28. `empty-minion-spawn-points-adjacent@1` - the same question asked of p.19's
        /// innermost ring, which is a distance the sentence writes down rather than an icon the card
        /// prints.
        ///
        /// <para>Dodger's Dread Razor and Rusty Razor name it: "If you are adjacent to an empty spawn
        /// point in the battlezone, target a unit in range." Both faces print a <em>Range</em> icon and
        /// no Radius at all, so there is no printed disk for the radius twin above to read - the
        /// adjacency is the card's own word, and p.16 never raises one of those.</para>
        ///
        /// <para>A second capability id over the same handler, by the rule the Range/Radius selection
        /// pairs already record. Both publish the same two facts, because the sentence a card builds on
        /// the answer is the same sentence either way.</para>
        /// </summary>
        public static EffectCapabilityReference EmptyMinionSpawnPointsAdjacentV1 =>
            Reference("empty-minion-spawn-points-adjacent", 1);

        /// <summary>
        /// Milestone 8.35z28. `ultimate-unlocked@1` - whether the acting hero has unlocked their
        /// Ultimate, published as the same <c>present</c> flag every other condition in this vocabulary
        /// publishes so an existing conditional node reads it unchanged.
        ///
        /// <para>Dodger's Darkest Ritual is the face that names it: "If you have Ultimate, gain +1
        /// [Attack] Attack item." The user's 2026-09-06 ruling on D-04 makes that clause independent of
        /// the spawn-point clause beside it, so it is its own condition over its own gate rather than a
        /// second step inside the first one's branch.</para>
        ///
        /// <para><b>It asks p.16's own question and stores nothing.</b> An Ultimate is unlocked exactly
        /// when its card sits in the Ultimate area beside the dashboard, which is where p.16 puts it and
        /// where <c>UltimateUnlockQuery</c> has read it since milestone 7.3. A capability that recorded
        /// an unlock would be a second answer that could disagree with the card.</para>
        /// </summary>
        public static EffectCapabilityReference UltimateUnlockedV1 =>
            Reference("ultimate-unlocked", 1);

        /// <summary>
        /// Milestone 8.35z28. `gain-item-from-the-box@1` - rulebook p.16's Gain an Item performed
        /// by card text, with no card leaving the deck.
        ///
        /// <para>Dodger's Darkest Ritual is the only verified face that grants one, and its own
        /// reminder names the source: "<em>( Use any spare card from the box wtih the corresponding
        /// icon )</em>". Every item until now came from a card p.15's Level Up had set aside, so the
        /// existing transitions all start from a card in the hero's own deck; this one starts from the
        /// box, which is why it is a capability rather than a leveling transition.</para>
        ///
        /// <para><b>The slot is not a parameter.</b> p.16 decides it: "the icon matches the icon on one
        /// of four slots on the top of your hero dashboard". The card names the icon, the dashboard
        /// names the slot, and a definition that could name a slot would be able to put an Attack item
        /// somewhere the page does not.</para>
        ///
        /// <para><b>It counts nothing and expires nothing</b>, which is the user's D-05 ruling and also
        /// the card's silence: every performance grants another permanent item, stacking under the one
        /// slot p.16 already allows several items under.</para>
        /// </summary>
        public static EffectCapabilityReference GainItemFromTheBoxV1 =>
            Reference("gain-item-from-the-box", 1);

        /// <summary>The p.16 icon `gain-item-from-the-box@1` grants, which is the one its card prints.</summary>
        public const string ItemIconParameter = "itemIcon";

        /// <summary>Which dashboard slot the granted item landed under, as the item's own 1-based position.</summary>
        public const string GrantedItemOrderOutput = "grantedItemOrder";

        /// <summary>
        /// Milestone 8.61h. `take-deck-card-as-item@1` - rulebook p.16's Gain an Item performed on a
        /// card the hero takes out of her own DECK, chosen by her, at a moment that is not p.15's
        /// Upgrade step: Min the Dragonmonk's Perfect Self, "Take a Tier II card from your deck and
        /// add it to your dashboard as a permanent item."
        ///
        /// <para><b>It is a third way to gain an item because the page has three sources and this
        /// build now prints all three.</b> p.15's Upgrade step sets a card aside and p.16 tucks it
        /// under the dashboard - that is <c>HeroLevelUp</c>'s, and it asks nobody anything.
        /// <c>gain-item-from-the-box@1</c> takes a spare card with no rules on it - that is Dodger's
        /// Darkest Ritual. This one takes a REAL card out of the deck: the card leaves the deck
        /// permanently, so the hero's remaining upgrade ladder is one card shorter, which neither of
        /// the others does.</para>
        ///
        /// <para><b>It is synthetic rather than hero-owned because the shape is hero-neutral.</b>
        /// "Take a Tier N card from your deck and add it as an item" is a p.15/p.16 operation any
        /// printed face could carry; nothing in it names Min, her colours or her cards. The Tier is
        /// the declaration's own parameter for the same reason.</para>
        ///
        /// <para><b>The slot is not a parameter and neither is the icon.</b> p.16 decides both: the
        /// card names the icon it prints and the dashboard names the slot that accepts it. A
        /// declaration able to name either would be able to file a card under a slot the page does
        /// not.</para>
        ///
        /// <para><b>It offers rather than takes.</b> p.10 puts every choice a card does not assign to
        /// the hero performing it, so the deck's qualifying cards are candidates and Min picks one -
        /// even when only one qualifies, which is this codebase's uniform precedent for a
        /// chooser-owned selection.</para>
        /// </summary>
        public static EffectCapabilityReference TakeDeckCardAsItemV1 =>
            Reference("take-deck-card-as-item", 1);

        /// <summary>
        /// The Tier a card must print to be taken by `take-deck-card-as-item@1` - Perfect Self says
        /// "a Tier II card" and means that Tier and no other. Milestone 8.61h.
        /// </summary>
        public const string ItemCardTierParameter = "itemCardTier";

        /// <summary>The card instance `take-deck-card-as-item@1` turned into an item.</summary>
        public const string TakenItemCardOutput = "takenItemCard";

        /// <summary>
        /// How many qualifying Spawn points an empty-spawn-point question found. Milestone 8.35z28, and
        /// spelled the same as <c>presence-count@1</c>'s because it is the same kind of answer.
        /// </summary>
        public const string CountOutput = "count";

        /// <summary>
        /// Milestone 8.35z33. `choose-target-trio@1` - up to three subjects named by one printed
        /// sentence, published as three slots the consumers read one apiece.
        ///
        /// <para>Whisper's Greater Good is the face: "You defeat up to 3 minions adjacent to you." It
        /// is <c>choose-target-set@1</c>'s shape with one more slot, and it is a third capability id
        /// rather than a revision of that one because a revision retires what it replaces - which
        /// would move Sabina's Shootout and Bullet Hell declarations, and with them a shipped hero's
        /// content identity, for a card that is not hers.</para>
        ///
        /// <para><b>Three slots rather than a list, for the reason
        /// <c>choose-target-set@1</c> has two.</b> An <see cref="EffectExecutionResult"/> publishes
        /// each output name once, so a selection that named three subjects under one name could not
        /// state which was which; and the consumers are three separate nodes, each carrying its own
        /// failure policy, so a slot nobody filled is skipped by that node rather than by a count
        /// somebody has to read first.</para>
        /// </summary>
        public static EffectCapabilityReference ChooseTargetTrioV1 =>
            Reference("choose-target-trio", 1);

        /// <summary>
        /// Milestone 8.35z33. <c>defended-card-forced-move@1</c> - Whisper's Swift Justice and Vile
        /// Accusation at rulebook p.14's step 7: "If able, that hero performs a movement action on the
        /// card they defended with, moving full distance in a straight line."
        ///
        /// <para><b>It is the first contract in this build where a hero who is not the actor performs
        /// something.</b> Every other card-text step belongs to whoever is taking the action; this one
        /// is done <em>by</em> the hero who was just attacked, on <em>their</em> card, and the
        /// 2026-08-19 designer answer settles who steers it - "There's no functional distinction
        /// between 'Player' and 'Hero' in Guards of Atlantis II as far as the rules text is
        /// concerned" - so the decision belongs to the target's seat. That is
        /// <see cref="EffectChoiceRequest.ChooserHeroId"/>, the same seam milestone 7.1a built so that
        /// "another hero discards a card" could put the card choice to that hero, and the same seam
        /// milestone 8.35r's decision-owner projection already routes to the right seat.</para>
        ///
        /// <para><b>Four printed clauses, and each is a decision this handler does not take.</b>
        /// <list type="bullet">
        /// <item><description>"<b>on the card they defended with</b>" - the card the Defense window
        /// accepted, read off the enclosing Attack rather than guessed from the discard pile. Its
        /// printed Movement value is what the move spends, through the ordinary
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.MovementValueQuery"/>, so rulebook
        /// p.16's matching item raises it exactly as it raises any other printed Movement.</description></item>
        /// <item><description>"<b>moving full distance</b>" - p.11's "up to" is overridden, so the
        /// only destinations offered lie EXACTLY the full distance away. Whisper Ruling 16a (user,
        /// 2026-09-28, milestone 8.115): "If able" governs the whole sentence, so a direction stopped
        /// short is not offered at all - superseding the "as far as possible when blocked" reading
        /// shipped at 8.35z33.</description></item>
        /// <item><description>"<b>in a straight line</b>" - p.19's file, checked on the route and not
        /// only on the endpoint, so a walk that curves around an obstacle and happens to end in line
        /// is not one of these.</description></item>
        /// <item><description>"<b>If able</b>" - a defender who was removed at step 6, declined to
        /// defend, defended with a card printing no Movement value, or stands with every direction
        /// blocked, simply does nothing. The handler reports the step impossible and the enclosing
        /// <see cref="EffectFailurePolicy.IfAble"/> makes that a silent skip.</description></item>
        /// </list></para>
        ///
        /// <para><b>It is the defending hero's Movement action</b> - Whisper Ruling 16b (user,
        /// 2026-09-28, milestone 8.115): "its movement action that forces you to move as specified".
        /// So a standing Movement restriction prevents it, a Movement limit below the full distance
        /// leaves it unable, obstacle permissions scoped to Movement actions are read, and Movement
        /// raises and items count. It is still performed here, at p.14's step 7 of Whisper's Attack,
        /// as one committed straight-line displacement; there is no Fast Travel to substitute, because
        /// the card fixes the move. Whether the defender's own after-Movement-action triggers fire off
        /// it is not decided by this milestone.</para>
        /// </summary>
        public static EffectCapabilityReference DefendedCardForcedMoveV1 =>
            Reference("defended-card-forced-move", 1);

        /// <summary>
        /// Milestone 8.35z33. <c>enemy-hero-with-discard-present@1</c> - whether any enemy hero inside
        /// the acting card's own printed Radius disk has a card in their discard pile.
        ///
        /// <para>Three of Whisper's faces gate on it and all three print the same clause: Death
        /// Seeker's "If an enemy hero in radius has a card in the discard", and Blood Fury's and Blood
        /// Frenzy's identical condition on their repeats. One phrase, one derivation.</para>
        ///
        /// <para><b>Nothing in the shipped build reads another hero's discard pile as a question.</b>
        /// <c>owned-card-present@1</c> asks about a <em>named</em> hero's own cards and is how "if you
        /// have a card in hand" is composed; this one asks about a <em>class</em> of heroes picked out
        /// by the disk, and its answer is a single boolean rather than a hero, because the sentence
        /// selects nobody - it is a condition, and the card's other clauses choose their own
        /// subjects.</para>
        ///
        /// <para><b>It is asked live, every time.</b> Blood Frenzy re-checks it before each repeat, and
        /// the 2026-09-06 ruling makes that consequential rather than incidental: a hero who discards
        /// <em>to defend</em> one instance of the chain makes the condition true for the instances
        /// after it. Deriving on each ask is what makes that fall out rather than needing to be
        /// arranged.</para>
        /// </summary>
        public static EffectCapabilityReference EnemyHeroWithDiscardPresentV1 =>
            Reference("enemy-hero-with-discard-present", 1);

        /// <summary>
        /// Milestone 8.35z33. <c>choose-swap-subject@1</c> - one selection over a union of two differently
        /// reached sets, for Whisper's Cruel Twist and Sealed Fate: "Swap with a unit adjacent to you,
        /// <b>or</b> with an enemy hero in range with a card in the discard."
        ///
        /// <para><b>Why the union cannot be two nodes.</b> The face offers one choice, not a choice of
        /// which kind of choice to make. Composing it as a <see cref="EffectNodeKind.Choose"/> over two
        /// selections would put a question to the player that the card never asks - and would let one
        /// arm be greyed for emptiness while the other still had subjects, which is a shape the printed
        /// sentence has no room for.</para>
        ///
        /// <para><b>Both arms are optional and each is absent when its parameter is.</b> That is what
        /// keeps this a general statement of "a selection over a union" rather than one card's sentence
        /// spelled into a handler: a face printing only the adjacency half declares only that half, and
        /// gets exactly the candidates it prints.</para>
        ///
        /// <para><b>The two reaches are different kinds of number and stay apart.</b> Adjacency is
        /// p.19's innermost ring, written into the sentence, which p.16's items never raise; the hero
        /// arm's reach is the card's printed Range icon, which a +1 Range item does raise. That is the
        /// same doctrine the Range/Radius selection split records, applied inside one sentence.</para>
        /// </summary>
        public static EffectCapabilityReference ChooseSwapSubjectV1 =>
            Reference("choose-swap-subject", 1);

        /// <summary>
        /// Milestone 8.37. <c>defended-card-color@1</c> - Ice Blast's step-3-to-step-7
        /// span: "If the target discards a card to defend, After the attack: Another enemy hero in
        /// radius discards a card of the same color, if able."
        ///
        /// <para><b>Reads a fact that already exists rather than capturing a new one.</b> Phase one's own
        /// report named this the costed shape of 8.13's was-adjacent snapshot - "a fact produced where
        /// step 3 already knows the answer, re-derived on restore" - and expected new plumbing. Reading
        /// the CURRENT engine first (the discipline every dealing prep before this one has paid for)
        /// finds <see cref="EnclosingAttackFacts.DefenseCardInstanceId"/> already carrying exactly that
        /// fact: it was added for Whisper's own <see cref="DefendedCardForcedMoveV1"/> rider, which reads
        /// the identical span for a different purpose (the defended card's Movement value rather than its
        /// colour) and is proof the fact survives correctly from step 3 or 4 to step 7 and through a
        /// restore. So the whole of phase one's costed engine debt for this face is one small reader
        /// beside an already-shipped sibling, not new capture-and-carry plumbing.</para>
        ///
        /// <para><b>Two outputs on one node, so the gate and the colour are the same read.</b>
        /// <see cref="DefendedOutput"/> is the Boolean a Conditional branches on - "If the target
        /// discards a card to defend" - and <see cref="ColorOutput"/> is the colour a later node in the
        /// same branch binds. One node rather than two keeps the predicate and the fact it gates from
        /// disagreeing about which defense the sentence means.</para>
        /// </summary>
        public static EffectCapabilityReference DefendedCardColorV1 =>
            Reference("defended-card-color", 1);

        /// <summary>Whether the enclosing Attack's target actually defended with a card.</summary>
        public const string DefendedOutput = "defended";

        /// <summary>The colour of the card the target defended with, when <see cref="DefendedOutput"/> is true.</summary>
        public const string ColorOutput = "color";

        /// <summary>
        /// Which units the adjacency arm of <see cref="ChooseSwapSubjectV1"/> admits, as a
        /// <see cref="RosterUnitKind"/>, or absent where the sentence prints no such arm.
        /// </summary>
        public const string AdjacentUnitKindParameter = "adjacentUnitKind";

        /// <summary>
        /// Which side the printed-Range hero arm of <see cref="ChooseSwapSubjectV1"/> admits, as a
        /// <see cref="SourceTeamRelation"/>, or absent where the sentence prints no such arm.
        /// </summary>
        public const string RangedHeroRelationParameter = "rangedHeroRelation";

        /// <summary>
        /// Whether the printed-Range hero arm of <see cref="ChooseSwapSubjectV1"/> requires the hero to
        /// have a card in their discard - "with a card in the discard", which both faces print.
        /// </summary>
        public const string RangedHeroRequiresDiscardParameter = "rangedHeroRequiresDiscard";

        public const string OriginParameter = "origin";

        /// <summary>
        /// How far from its origin a selection reaches, as a number the definition writes down.
        ///
        /// <para>Milestone 8.20 renames the wire name from `distance` to `reach`, and the rename is the
        /// point. `OptionalDistanceConformanceTests` requires every capability declaring a *distance*
        /// to read how much of it is optional through the one statement of rulebook pages 10 and 11 -
        /// because a displacement that decided for itself once let one capability move nothing where
        /// another refused the same choice. A selection radius has no such question: p.9's disk is
        /// inclusive and reaches everything within it, and there is nothing about it a player could
        /// choose to do less of. Two different questions sharing one parameter name made this
        /// vocabulary look like a subject of that rule the moment the build registered it.</para>
        /// </summary>
        public const string DistanceParameter = "reach";

        /// <summary>
        /// Milestone 9.2. Bound true in place of <see cref="DistanceParameter"/> where the sentence
        /// says "in radius" or the card's Radius icon bounds the action without a number in its text:
        /// p.9 makes that the printed icon, and p.16's items raise it. A literal reach is a number the
        /// sentence writes down, which no item raises - binding the icon's number as a literal was
        /// milestone 8.82/8.83's defect, found on Sabina's and Cutter's selections by 9.2's audit.
        /// Read inside the selection itself so the offer check stays exact about who can be named.
        /// </summary>
        public const string MeasuresPrintedRadiusParameter = "measuresPrintedRadius";
        public const string UnitKindParameter = "unitKind";
        public const string RelationParameter = "relation";
        public const string ChosenHeroOutput = "chosenHero";
        public const string ChosenMinionOutput = "chosenMinion";

        /// <summary>
        /// Milestone 8.20. Whether the enclosing Attack's own target is one of the candidates.
        ///
        /// <para>Arien's Noble Blade is what needs it: "You may move <b>another</b> unit that is
        /// adjacent to the target 1 space". A disk of radius one drawn on the target's space contains
        /// the target, because p.9 measures inclusively - and the target is not adjacent to itself, so
        /// offering it would let this card reposition the very unit the Attack is about to resolve
        /// against.</para>
        ///
        /// <para>It is optional and defaults to false, so every declaration written at milestone 8.19b
        /// keeps its exact candidate set. That is the same shape <c>presence-count@1</c> already gave
        /// the question, stated once more here rather than answered a second way.</para>
        /// </summary>
        public const string ExcludeTargetParameter = "excludeTarget";

        /// <summary>
        /// Milestone 8.24. Which of p.22's minion classes a roster selection admits, as
        /// <see cref="RosterMinionClass"/>. Spelled the same as the printed-icon selections' parameter
        /// because it is the same adjective on a different reach.
        /// </summary>
        public const string AdmittedMinionClassParameter = "admittedMinionClass";

        /// <summary>
        /// Milestone 8.24. Whether one minion's printed class satisfies the adjective a card's sentence
        /// prints. It is stated once and shared by both selection families, because the adjective means
        /// the same thing whether the reach came from p.9's icon or from the card's own words - two
        /// copies of it could disagree about what "ranged or melee" admits, and the classes are
        /// rulebook content rather than either family's.
        /// </summary>
        public static bool AdmittedClassIncludes(
            RosterMinionClass admittedClass, StandardMinionType minionType)
        {
            switch (admittedClass)
            {
                case RosterMinionClass.AnyClass:
                    return true;
                case RosterMinionClass.Ranged:
                    return minionType == StandardMinionType.Ranged;
                case RosterMinionClass.Melee:
                    return minionType == StandardMinionType.Melee;
                case RosterMinionClass.RangedOrMelee:
                    return minionType == StandardMinionType.Ranged ||
                        minionType == StandardMinionType.Melee;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Milestone 8.21. Whether the selection admits only heroes who have played an Attack card
        /// this turn - Brogan's Shield Bash and Counterattack, "An enemy hero adjacent to you who has
        /// played an attack card this turn".
        ///
        /// <para>It narrows the candidates rather than gating what happens to the chosen one, and that
        /// is the whole point: the clause is part of who may be picked, so a hero who has played
        /// nothing is not one to offer and then refuse. It is the same reading milestone 8.20b settled
        /// for a restriction that names its actors.</para>
        ///
        /// <para>The question itself is <c>target-played-attack-this-turn@1</c>'s -
        /// <see cref="PlayedAttackCardThisTurnQuery"/> - asked here rather than answered a second way,
        /// so a card that conditions on the fact and a card that selects by it cannot come to disagree
        /// about what it means. p.9 makes a card with an Attack primary an Attack card and p.10 makes
        /// a card played the moment it is placed facedown, which is why neither reading asks whether
        /// the Attack was performed.</para>
        ///
        /// <para>Optional and defaulting to false, so every declaration written before this milestone
        /// keeps its exact candidate set.</para>
        /// </summary>
        public const string PlayedAttackThisTurnParameter = "playedAttackThisTurn";

        /// <summary>
        /// Milestone 8.22. Whether the selection admits only candidates standing next to a minion of
        /// the actor's own side - Sabina's Close Support and Covering Fire, "An enemy hero in radius
        /// adjacent to your friendly minion".
        ///
        /// <para>It narrows the candidates for the reason
        /// <see cref="PlayedAttackThisTurnParameter"/> does: the clause is part of who may be picked,
        /// so an enemy hero standing alone is not one to offer and then refuse. The sentence has two
        /// bounds and they are about different things - the disk is measured from Sabina and the
        /// adjacency is measured from each of her minions - which is why this cannot be expressed by
        /// moving the selection's origin.</para>
        ///
        /// <para><b>It is not <c>synthetic-hero-adjacent-to-minion@1</c>, which is the near miss worth
        /// naming.</b> That capability is handed one minion and offers the heroes beside <em>it</em>.
        /// This clause names no minion at all: any friendly minion of hers will do, and asking the
        /// player to pick the minion first would invent a decision the card does not print - and a
        /// wrong one, because a hero adjacent to two of her minions would be offered twice.</para>
        ///
        /// <para>Optional and defaulting to false, so every declaration written before this milestone
        /// keeps its exact candidate set.</para>
        /// </summary>
        public const string AdjacentToFriendlyMinionParameter = "adjacentToFriendlyMinion";
        /// <summary>
        /// Milestone 8.39d. Whether the selection admits only candidates standing next to another unit
        /// of their OWN side - Cutter's Bombardment, Barrage and Broadside, "An enemy hero adjacent to
        /// another enemy unit and not adjacent to you discards a card, if able."
        ///
        /// <para><b>It is the sibling of <see cref="AdjacentToFriendlyMinionParameter"/> and not a
        /// widening of it</b>, which is what the 8.38b lesson predicted and what reading Sabina's
        /// filter confirmed. Hers narrows the same radius-hero selection by adjacency measured from the
        /// ACTOR's team and restricted to p.19's minions; this one measures from the SUBJECT's team and
        /// admits any unit. Two parameter differences on one clause, so re-expressing hers through a
        /// shared pair would change her node's inputs and move a shipped digest - a second boolean
        /// costs nothing and says what it means.</para>
        ///
        /// <para><b>"Another" is doing real work here where it did not for Sabina.</b> Her remarks
        /// record that a minion cannot be standing on an enemy hero, so nothing needed excluding. This
        /// clause measures a disk around the candidate for units on the candidate's OWN side, and the
        /// candidate is one - so the candidate is excluded from its own neighbourhood explicitly, or
        /// every enemy hero on the board would qualify by standing next to itself.</para>
        ///
        /// <para>Optional and defaulting to false, so every declaration written before this milestone
        /// keeps its exact candidate set.</para>
        /// </summary>
        public const string AdjacentToAnotherEnemyUnitParameter = "adjacentToAnotherEnemyUnit";

        /// <summary>
        /// Milestone 8.39h. Whether the selection drops p.9's innermost ring - the second half of the
        /// clause the parameter above carries, "an enemy hero adjacent to another enemy unit <b>and not
        /// adjacent to you</b>".
        ///
        /// <para><b>Milestone 8.39d recorded this half as already registered and it was not.</b> It
        /// named <c>target-is-not-adjacent@1</c>, and so did her shard's mapping row. That capability
        /// asks whether the enclosing ATTACK's bound target is adjacent to the actor right now -
        /// Misa's Thunder Shot at p.14's step 7 - and Bombardment, Barrage and Broadside are Skills
        /// with no attack target to ask about. The composition milestone is where that surfaced, which
        /// is where this project expects it to, and both records are corrected beside this parameter
        /// rather than papered over.</para>
        ///
        /// <para><b>It is spelled with the printed-icon family's own constant rather than a second
        /// literal that happens to match.</b> <c>synthetic-radius-hero-target@1</c> has carried this
        /// exact narrowing since milestone 8.11 for Wasp's Shock, Electrocute and Electroblast - "An
        /// enemy hero in radius and not adjacent to you" - and a card author declaring the clause on a
        /// written-distance selection and on a printed-icon one must not be able to get one of them
        /// wrong. The reading is that family's, unchanged: the ring removed is measured from the ACTOR,
        /// which is who "you" is on every face that prints the clause.</para>
        ///
        /// <para>Optional and defaulting to false, so every declaration written before this milestone
        /// keeps its exact candidate set.</para>
        /// </summary>
        public const string ExcludeAdjacentParameter =
            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName;

        /// <summary>
        /// Milestone 8.57c. Whether the selection admits only candidates holding no cards at all -
        /// Rowenna's Accept Surrender and Glorious Triumph, "Defeat an enemy hero adjacent to you
        /// <b>with no cards in hand</b>".
        ///
        /// <para>It narrows the candidates for the reason
        /// <see cref="PlayedAttackThisTurnParameter"/> does: the clause is part of who may be picked,
        /// so a hero holding a card is not one to offer and then refuse - and here the point is
        /// sharper than usual, because the consequence is a Defeat and an offer that had to be taken
        /// back would have removed a hero from the board first.</para>
        ///
        /// <para><b>The spelling is the printed-icon family's own constant</b>, exactly as
        /// <see cref="ExcludeAdjacentParameter"/> is, and for the same reason milestone 8.39h gave:
        /// this clause is the complement of that family's <c>minimumCardsInHand</c>, and a card
        /// author declaring one of the two must not be able to mistype the other. Her faces print no
        /// icon at all, so the arm that carries it is this one - "adjacent to you" is card text and
        /// p.16's items do not raise a bound a card never printed.</para>
        ///
        /// <para>Optional and defaulting to false, so every declaration written before this
        /// milestone keeps its exact candidate set.</para>
        /// </summary>
        public const string RequiresEmptyHandParameter =
            SyntheticTargetingCapabilities.RequiresEmptyHandParameterName;

        /// <summary>
        /// Milestone 8.40c. Whether the selection admits only candidates wearing a marker the ACTOR's
        /// own content put out - Bain the Bounty Hunter's Hand Crossbow, "Target a hero in range with a
        /// [bounty] bounty marker".
        ///
        /// <para>It narrows the candidates for the reason
        /// <see cref="PlayedAttackThisTurnParameter"/> does: the clause is part of who may be picked, so
        /// a hero wearing nothing is not one to offer and then refuse.</para>
        ///
        /// <para><b>It says "the actor's" because no value kind can say "the bounty".</b>
        /// <see cref="EffectValueKind"/> names booleans, quantities, heroes, minions, cards, spaces and
        /// nodes, and none of them is a marker - so a declaration that wanted to name a DEFINITION here
        /// could only do it by putting a component id into a payload, which is the sketch
        /// <c>AttackSequenceSubjectNarrowing</c>'s own remarks measured and refused. What a declaration
        /// CAN name is a hero, and the hero whose content owns the marker is the actor on every face
        /// that prints the clause. <see cref="HeldMarkerQuery.HoldersOfAMarkerDeclaredBy"/> is where
        /// that is turned back into a definition, so this narrowing, the bullet's
        /// <c>AttackSequenceSubjectNarrowing.HoldsTheActorsMarker</c> and the marker-presence predicate
        /// all read one method and cannot come to disagree about one printed sentence.</para>
        ///
        /// <para>Optional and defaulting to false, so every declaration written before this milestone
        /// keeps its exact candidate set.</para>
        /// </summary>
        public const string HoldsTheActorsMarkerParameter = "holdsTheActorsMarker";

        /// <summary>
        /// Milestone 8.22. The count <c>presence-count@1</c>'s <c>present</c> answer is measured
        /// against - Sabina's Steady Advance and Unwavering Resolve, "If there are two or more
        /// friendly minions in radius".
        /// </summary>
        public const string MinimumPresentParameter = "minimum";

        /// <summary>
        /// Milestone 8.35z33. The ceiling a counting sentence prints on its own answer - Whisper's
        /// Sanguine Path and Blood Pilgrimage, "up to 1 space for every empty minion spawn point in
        /// radius in the battle zone <em>up to a maximum of 2 / 3 spaces</em>".
        ///
        /// <para>It caps the published <see cref="CountOutput"/> rather than being applied by whoever
        /// reads it, because the sentence's arithmetic is one sentence: the number the card yields IS
        /// the lesser of the board's count and the printed maximum, and a consumer that had to
        /// remember to clamp is a second place the same reading could be wrong. The count of Spawn
        /// points on the board is not itself changed by this - what is published is what the sentence
        /// yields.</para>
        ///
        /// <para>Optional, and defaulting to no ceiling at all, so a face that prints a bare "for
        /// every" reads the whole count.</para>
        /// </summary>
        public const string MaximumCountedParameter = "maximumCounted";


        /// <summary>
        /// Where a move this vocabulary performs put its subject. Milestone 8.35z33, spelled the same as
        /// the direct Move's because it is the same kind of answer.
        /// </summary>
        public const string MovedOutput = "moved";

        /// <summary>The hero whose own cards a card-state capability reads. Milestone 8.21.</summary>
        public const string HeroParameter = "hero";

        /// <summary>Which of rulebook p.10's places a card question is asked about. Milestone 8.21.</summary>
        public const string LocationParameter = "location";

        /// <summary>The card `discard-owned-card@1` spent, published only when one was spent.</summary>
        public const string DiscardedOutput = "discarded";

        /// <summary>
        /// Throwing Axe's "if you do", as the positive fact a conditional can read. False where the
        /// hero held nothing; absent where they declined, which reads as the same false.
        /// </summary>
        public const string DidDiscardOutput = "didDiscard";

        /// <summary>Whether `owned-card-present@1` found a card where it looked.</summary>
        public const string PresentOutput = "present";

        /// <summary>
        /// One statement of what a hero may spend: an unplayed card in their own hand, which is the
        /// state rulebook p.15's discard starts from and the state the in-hand mutation accepts. It is
        /// the same criteria milestone 7.1a's discard states, restated against this capability's own
        /// chooser rather than shared across an assembly boundary that does not exist yet.
        /// </summary>
        private static CardCharacteristicCriteria HandCriteria(HeroId owner) =>
            new CardCharacteristicCriteria(
                ownerId: owner,
                locations: new[] { CardLocation.Hand },
                hasBeenPlayed: false);

        /// <summary>
        /// Milestone 8.20. The chosen piece's current space, published beside its identity.
        ///
        /// <para>It exists because the operations that reposition a piece take a coordinate - p.18's
        /// Push, Place and direct Move each name a space - while this capability names a unit, and
        /// Noble Blade needs both: the sentence chooses a unit and then moves it. Wasp's radius
        /// selections have published a coordinate since milestone 6.8, so this is the same output on a
        /// second selection rather than a new idea.</para>
        ///
        /// <para>A node that does not declare it never sees it, which is why adding it leaves every
        /// milestone 8.19b definition unchanged.</para>
        /// </summary>
        public const string ChosenSpaceOutput = "chosenSpace";

        /// <summary>
        /// Milestone 8.20. Whether an up-to-one selection named anybody at all.
        ///
        /// <para>Arien's Dangerous Current, Raging Stream and Violent Torrent each print "Up to 1 enemy
        /// hero in any of the N spaces ... discards a card, or is defeated", and the two halves of that
        /// sentence are a selection that may reach nobody followed by a step that is mandatory once it
        /// does. A conditional reads a boolean, and this is that boolean: naming nobody is a legal and
        /// complete outcome, which is exactly what a consumer must be able to tell apart from a
        /// question that was never asked.</para>
        /// </summary>
        public const string NamedAHeroOutput = "namedAHero";

        /// <summary>
        /// The whole vocabulary, for the bounded fixture catalogs that exercise each contract over
        /// hero-shaped consumers. It is deliberately not what the build registers: milestone 7.9's
        /// deferral gate refuses a registration no printed card can start, and most of this set is
        /// still waiting for the hero whose face names it.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// The part of this vocabulary the build does <em>not</em> register, stated as its own list so
        /// that what is deferred can be read, counted and composed onto a bounded catalog without a
        /// caller having to subtract one list from another.
        ///
        /// <para><c>target-played-attack-this-turn@1</c> has been here since milestone 8.19b, for the
        /// reason <see cref="RegisteredHandlers"/> records: Sabina reaches the same question through the
        /// value pipeline, so no card starts this contract.</para>
        ///
        /// <para>Milestone 8.35z28 adds Dodger the Warlock's empty-minion-Spawn-point question in its two
        /// reaches, plus her Ultimate's unlocked test and her item grant. They are here rather than
        /// registered because that milestone composes her printed set and does not deal her: no card a
        /// configured match deals starts any of them, and milestone 7.9's deferral gate refuses a
        /// registration no card can reach. Her dealing milestone is what moves these lines into the
        /// registration list - together with the showcase consumer that gate's second half requires,
        /// which is what keeps a new handler from arriving for the first time inside a verified hero's
        /// slice.</para>
        ///
        /// <para>Milestone 8.35z29 adds six on the same terms, all of them named by Misa's composed
        /// faces and by nothing else in the build: the straight-line destination and its traversal
        /// selection for her five Blue Skills, the two unit-pair selections her Swap cards need, and the
        /// two tenses her Red ranged pair reads. They register the moment she is dealt, which is the
        /// same moment her Active-effect creations join the catalog - and until then this list is what a
        /// fixture composes onto a bounded one.</para>
        ///
        /// <para>Milestone 8.35z30 adds Silverarrow's card-text Fast Travel on the same terms, and
        /// supplies the ONE <c>choose-line-destination@1</c> the line above names: both milestones minted
        /// that id against the same output, and the wave kept this - the superset, whose optional
        /// <see cref="IgnoresObstaclesParameter"/> is what Misa's five Blue Skills bind.</para>
        ///
        /// <para>Milestone 8.35z33 adds Whisper the Outcast's four, and supplies the ONE
        /// <c>empty-minion-spawn-points-in-radius@1</c> her Red line and Dodger's rituals both name -
        /// the superset again, whose optional <see cref="MaximumCountedParameter"/> is the ceiling
        /// Sanguine Path and Blood Pilgrimage print and Dodger's faces do not. THE GATE HAS TWO HALVES
        /// and no milestone in this wave can satisfy either: a registered capability must be reachable
        /// from a card a match <em>deals</em>, and a new handler type must additionally be reachable
        /// from a <c>SyntheticShowcaseHand</c> card. Each hero's dealing milestone is what moves her
        /// lines into the registration list, together with the showcase consumers that second half
        /// requires.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers()
        {
            yield return new PlayedAttackThisTurnHandler();

            // MILESTONE 8.61h'S ONE HAS LEFT THIS LIST - see `RegisteredHandlers` below.
            // `take-deck-card-as-item@1` was minted for Min the Dragonmonk's Perfect Self ("Take a
            // Tier II card from your deck and add it to your dashboard as a permanent item") and
            // waited here while 8.61h composed three of her faces without dealing her. Milestone
            // 8.61j deals her, so the contract is one a card a configured match puts in a hand
            // starts, and both halves of milestone 7.9's gate are satisfied in that one commit: the
            // dealing satisfies the first, and the showcase Skill it adds - the Red Quartermaster -
            // satisfies the second, which a new handler TYPE owes.

            // MILESTONE 8.35z38'S SIX HAVE LEFT THIS LIST - see `RegisteredHandlers` below. Misa the
            // Samurai is dealt, so her Red ranged pair, her five Blue Skills and her two Swap Skills
            // are cards a configured match puts in a hand, and the gate that held them here is
            // satisfied on both halves at once.
            //
            // MILESTONE 8.35z46's ONE HAS ALSO LEFT THIS LIST - see `RegisteredHandlers` below.
            // Silverarrow the Pathfinder is dealt, so her two Gold basics and Trailblazer, which name
            // `fast-travel@1`, are cards a configured match puts in a hand. `choose-line-destination@1`
            // left this list already, at Misa's dealing: registering it there was not a registration on
            // Silverarrow's behalf, but her three straight-line faces reach the same registered
            // contract now that it is one, which is the ordinary case of two heroes sharing one rule.
            //
            // MILESTONE 8.35z53'S FOUR HAVE ALSO LEFT THIS LIST - see `RegisteredHandlers` below.
            // Whisper the Outcast is dealt, so her Gold basic's forced-move rider, her Red trio, her
            // discard-gated Conditional and her two Swaps are all cards a configured match puts in a
            // hand, and the gate that held them here is satisfied on both halves at once.

            // Milestone 8.37 registered Tali's `defended-card-color` beside her own dealt Ice
            // Blast - see `RegisteredHandlers` below.

            // Milestone 8.38d's `subject-adjacent-terrain-presence@1` HAS LEFT THIS LIST - see
            // `RegisteredHandlers` below. Swift the Sharpshooter is dealt at milestone 8.38e, so her
            // three Green faces, which qualify the enemy they reach by that enemy's own footing, are
            // cards a configured match puts in a hand.

            // MILESTONE 8.39g1'S ONE HAS LEFT THIS LIST - see `RegisteredHandlers` below. Cutter the
            // Sky Pirate is dealt at milestone 8.39i, so A Fistful of Coins, the only face in the
            // corpus that claims an ending for one hero, is a card a configured match puts in a hand.
            // Both halves of milestone 7.9's gate are satisfied in that one commit: the dealing
            // satisfies the first, and the showcase Skill it adds - the Red Privateer - satisfies the
            // second, which a new handler TYPE owes.
        }

        /// <summary>
        /// The part of this vocabulary a verified hero's printed set actually reaches, which is what
        /// <see cref="SyntheticEffectCapabilities.CreateCatalog"/> registers.
        ///
        /// <para>Milestone 8.20 is the first milestone with anything to put here. Arien's Noble Blade
        /// names <c>choose-target@2</c>, his three Currents name <c>behind-target-line@1</c>, and Ebb
        /// and Flow names <c>spaces-adjacent@1</c>. The remaining contracts belong to Brogan, Sabina,
        /// Tigerclaw and Xargatha, and each joins this list at the milestone whose printed card first
        /// names it - which is what keeps the registration list a statement about what this build can
        /// do rather than about what it has written down.</para>
        ///
        /// <para>Milestone 8.21 adds Brogan's five and the two it had to mint here, beside a third it
        /// minted in the printed-icon family: `synthetic-range-unit-target@1`, the unit-valued
        /// printed-Range selection his modal Ranged Attacks name, which registers there because it is a
        /// sixth declaration over the handler p.9's two icons already share. His three charges name
        /// <c>choose-charge-line@1</c>; Onslaught names <c>attacked-target-space@1</c>; Bulwark names
        /// <c>retrieve-card@1</c>; War Drummer and Master Skald name <c>gain-resource@1</c> and
        /// <c>hero-defeated-this-round@1</c>; and his two modal Ranged Attacks name
        /// <c>discard-owned-card@1</c> and <c>owned-card-present@1</c>. His Shield Bash and
        /// Counterattack name no contract of their own: "who has played an attack card this turn" is
        /// a narrowing on <c>choose-target@2</c>'s candidates.</para>
        ///
        /// <para>Milestone 8.22 adds Sabina's four and the one it minted: her three swap Skills name
        /// <c>choose-target-pair@1</c>; Shootout and Bullet Hell name <c>choose-target-set@1</c> and
        /// <c>pre-consequence-adjacency@1</c>; Steady Advance and Unwavering Resolve name
        /// <c>presence-count@1</c>; and Point Blank Shot names <c>target-is-adjacent@1</c>, which this
        /// milestone had to mint because her present-tense rider and Shootout's past-tense one are
        /// different questions.</para>
        ///
        /// <para><b><c>target-played-attack-this-turn@1</c> is still not registered, and Sabina is the
        /// reason it is worth saying why.</b> Her Quickdraw, Gunslinger and Dead Shot are exactly the
        /// cards it was minted for - "+3 Attack if the target played an attack card this turn" - but a
        /// printed <c>+</c> on the Attack value is p.14's own convention for a number the card text
        /// raises, and a number is not an effect node. Those three declare a
        /// <c>RosterValueContributionDefinition</c> on the Attack action instead, which
        /// <c>RosterDynamicValueQuery</c> answers with <c>PlayedAttackCardThisTurnQuery</c> - the same
        /// question this capability asks, reached through the value pipeline rather than the effect
        /// frame. Registering it would put a contract in the pinned list that no dealt card starts,
        /// which is precisely what milestone 7.9's deferral gate refuses.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers()
        {
            yield return new ChooseTargetHandler();
            yield return new BehindTargetLineHandler();
            yield return new SpacesAdjacentHandler();
            yield return new ChargeLineHandler();
            yield return new FormerTargetSpaceHandler();
            yield return new HeroDefeatedThisRoundHandler();
            yield return new RetrieveCardHandler();
            yield return new GainResourceHandler();
            yield return new DiscardOwnedCardHandler();
            yield return new OwnedCardPresentHandler();
            yield return new ChooseTargetSetHandler();
            yield return new ChooseTargetPairHandler();
            yield return new PresenceCountHandler();
            yield return new PreConsequenceAdjacencyHandler();
            yield return new TargetIsAdjacentHandler();

            // Milestone 8.23 adds Tigerclaw's four. Blink Strike and Nimble Strike name
            // `choose-through-target-line`; Blend Into Shadows names `adjacent-terrain-presence`;
            // his three Green thieving cards name `transfer-resource`; and Combat Reflexes names
            // `actor-moved-this-action`, which this milestone had to mint because no earlier face
            // asks at one of p.14's moments about what happened at another.
            yield return new ThroughTargetLineHandler();
            yield return new AdjacentTerrainPresenceHandler();
            yield return new TransferResourceHandler();

            // THE SIX-HERO CONTENT WAVE ADDED NOTHING HERE. None of milestones 8.35z28 through
            // 8.35z33 deals its hero, and milestone 7.9's deferral gate refuses a registration no
            // printed card can start - so every contract those six mint is declared beside
            // `target-played-attack-this-turn@1` in `DeferredHandlers` above, and each hero's own
            // dealing milestone moves its lines across in one edit.
            yield return new ActorMovedThisActionHandler();

            // Milestone 8.35z34 is the first of those dealing milestones, and this is the edit it
            // makes. Dodger the Warlock's four roster contracts move across because that milestone
            // deals her: her Dread Razor and Death Trap start the adjacent count, her three Blue
            // Defense primaries and three Green rituals start the printed-Radius one, and her Darkest
            // Ritual starts both progression contracts. Both halves of the gate are satisfied
            // together - the first by dealing her, and the second by the two showcase Skills milestone
            // 8.35z34 added, because each of these is a handler TYPE this build had never registered.
            //
            // `empty-minion-spawn-points-in-radius@1` is the wave's shared declaration and Whisper's
            // Red line names it too. Registering it here is not a registration on her behalf: the
            // gate asks whether a DEALT card starts the contract, and Dodger's rituals do. Whisper's
            // faces reaching the same registered contract when she is dealt is the ordinary case of
            // two heroes sharing one rule.
            yield return new EmptyMinionSpawnPointCountHandler(
                EmptyMinionSpawnPointsInRadiusV1, readsPrintedRadius: true);
            yield return new EmptyMinionSpawnPointCountHandler(
                EmptyMinionSpawnPointsAdjacentV1, readsPrintedRadius: false);
            yield return new UltimateUnlockedHandler();
            yield return new GainItemFromTheBoxHandler();

            // Milestone 8.35z38 is the second dealing milestone, and this is the edit it makes. Misa
            // the Samurai's six roster contracts move across because that milestone deals her: Power
            // Shot and Thunder Shot start the two tenses of the enclosing-target distance question,
            // her five Blue Skills start the straight-line destination and its traversal selection,
            // and Living Tornado and Storm Spirit start the two unit-pair selections. Both halves of
            // the gate are satisfied together - the first by dealing her, and the second by the three
            // showcase cards milestone 8.35z38 added, because each of these is a handler TYPE this
            // build had never registered.
            //
            // SIX IDS ACROSS FOUR TYPES, and the gap is the whole reason the conformance count counts
            // types: the two tenses share one EnclosingTargetDistanceHandler and the two pair
            // selections share one UnitPairHandler.
            yield return new EnclosingTargetDistanceHandler(
                TargetWasAtMaximumRangeV1, "wasAtMaximumRange", atTargetingTime: true);
            yield return new EnclosingTargetDistanceHandler(
                TargetIsNotAdjacentV1, "notAdjacent", atTargetingTime: false);
            yield return new LineDestinationHandler();
            yield return new TraversedUnitHandler();
            yield return new UnitPairHandler(
                ChooseUnitPairAtMaximumRadiusV1, onTheMaximumRingOnly: true);
            yield return new UnitPairHandler(
                ChooseUnitPairAtEqualDistanceInRadiusV1, onTheMaximumRingOnly: false);

            // Milestone 8.35z46 is the third dealing milestone, and this is the edit it makes.
            // Silverarrow the Pathfinder's one roster contract moves across because that milestone
            // deals her: her two Gold basics' "you may fast travel to an adjacent zone" and
            // Trailblazer's "you may fast travel, if able" all start `fast-travel@1`. It is a handler
            // TYPE this build had never registered, so the gate's second half is satisfied by the
            // showcase card milestone 8.35z46 added beside dealing her.
            //
            // `choose-line-destination@1` moved across already, at Misa's dealing (8.35z38) - her three
            // straight-line faces reach the same registered contract Misa's five Blue Skills do, which
            // is the ordinary case of two heroes sharing one rule and not a second registration.
            yield return new CardTextFastTravelHandler();

            // Milestone 8.35z53. Whisper the Outcast's four roster contracts move across because that
            // milestone deals her, and the split between them is worth reading: three are questions a
            // player answers and one is a question the rules answer.
            //
            // `choose-target-trio@1` is Sprouts of Panic's and its two upgrades' "defeat up to 3
            // minions adjacent to you" - `choose-target-set@1`'s own terms with a third slot.
            // `choose-swap-subject@1` is Cruel Twist's and Sealed Fate's one selection over the UNION
            // of two differently reached sets. `defended-card-forced-move@1` is Swift Justice's
            // printed rider, and it is the first contract in this build whose choice is issued to a
            // hero who is NOT the actor - `EffectChoiceRequest.ChooserHeroId` names the DEFENDER, which
            // milestone 8.35r's decision-owner routing already sends to the right seat because it reads
            // exactly that field. `enemy-hero-with-discard-present@1` is the discard gate Death Seeker
            // asks once and Blood Frenzy asks before every instance of its chain; it asks nobody
            // anything and is reached only as a `Conditional`.
            //
            // All four are handler TYPES this build had never registered, so milestone 7.9's gate asks
            // its second half of each.
            yield return new ChooseTargetTrioHandler();
            yield return new DefendedCardForcedMoveHandler();
            yield return new EnemyHeroWithDiscardPresentHandler();
            yield return new ChooseSwapSubjectHandler();

            // Milestone 8.37, the milestone that deals Tali the Spiritcaller. `defended-card-color`
            // is her Ice Blast's rider: the colour the defender spent at p.14 step 3, read at step 7
            // and gating the discard that follows it. It reads a fact Whisper's own dealing already
            // captured - `EnclosingAttackFacts.DefenseCardInstanceId` - so it registers a new READER
            // over existing plumbing rather than new capture-and-carry, and Ice Blast is a dealt
            // basic Attack, which satisfies the gate's first half outright.
            yield return new DefendedCardColorHandler();

            // Milestone 8.38e, the milestone that deals Swift the Sharpshooter.
            // `subject-adjacent-terrain-presence@1` is what her three Green faces qualify their enemy
            // by - "an enemy hero NOT adjacent to terrain" - and it is a second reader over the same
            // `TerrainPresenceQuery` that `adjacent-terrain-presence@1` above has read since milestone
            // 8.23. Minted apart at 8.38d rather than added as a parameter to that one, because a
            // shared declaration gaining a field moves every hero who already references it, and
            // Tigerclaw does. Suppress, Pin Down and Killing Ground are dealt cards, so the gate's
            // first half is satisfied outright. It IS a new handler type, so it owes the second half a
            // showcase consumer, and milestone 8.38e adds one.
            yield return new SubjectAdjacentTerrainPresenceHandler();

            // Milestone 8.39i, the milestone that deals Cutter the Sky Pirate. `claim-solo-victory@1`
            // is A Fistful of Coins' third bullet - "If you have 13 or more coins, you alone win the
            // game" - and it is the only sentence in the corpus that claims an ending for one hero.
            // Milestone 8.39g built the contract it writes through and 8.39g1 minted this reader; the
            // gate held it out of this list until a card a configured match DEALS could start it, and
            // her Blue Tier III is that card. It IS a new handler type, so it owes the gate's second
            // half a showcase consumer, and this milestone adds one - the Red Privateer, whose own
            // threshold is deliberately unreachable so that proving the contract cannot end a fixture
            // match as a side effect.
            yield return new ClaimSoloVictoryHandler();

            // Milestone 8.61j, the milestone that deals Min the Dragonmonk as the NINETEENTH hero.
            // `take-deck-card-as-item@1` is Perfect Self's second bullet - "Take a Tier II card from
            // your deck and add it to your dashboard as a permanent item" - which milestone 8.61h
            // minted and deliberately left out of this list, because 8.61h composed three of her
            // faces without dealing her and the gate refuses a registration no dealt card can start.
            // Her Green Tier III is that card. It IS a new handler type, so it owes the gate's second
            // half a showcase consumer, and this milestone adds one - the Red Archivist, whose
            // own Tier is one no fixture deck holds, so proving the contract cannot take a card out
            // of somebody else's deck as a side effect.
            yield return new TakeDeckCardAsItemHandler();
        }

        private static EffectCapabilityReference Reference(string id, int revision) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), revision);

        /// <summary>
        /// The parameter Misa's five Blue Skills set and Silverarrow's three straight-line faces do
        /// not: whether the line's own text crosses p.18's obstacles.
        /// </summary>
        public const string IgnoresObstaclesParameter = "ignoresObstacles";
        /// <summary>
        /// Milestone 8.39a. The far end of a line an earlier step of this action already travelled,
        /// which narrows the declaration binding it to the continuation of that line past the actor.
        ///
        /// <para><b>One name because it is one question, asked by two clauses that read as
        /// opposites.</b> Cutter's three lunges print "Before the attack: Move 1 space in a straight
        /// line. Target a hero adjacent to you <b>in the direction of the move</b>", and this parameter
        /// carries the space she left; her two ranged Reds print "After the attack: Move up to 2 spaces
        /// <b>in the opposite direction</b>", and it carries the space she attacked. In both the line
        /// runs from the bound space, through the actor, and on - so
        /// <see cref="GuardsDigital.Core.Board.ContinuedLineQuery"/> is the single query the offer, the
        /// answer and a restored pause all read, which is what keeps the two clauses from drifting
        /// apart into two geometries.</para>
        ///
        /// <para><b>A bound space equal to the actor's own narrows to nothing, deliberately.</b> A
        /// lunge branch that performed no move fixed no heading, and a clause that narrows to the
        /// continuation of a line nobody travelled offers no candidate rather than falling back to the
        /// unnarrowed set. That is the whole difference between "in the direction of the move" and
        /// "adjacent", and a fallback would silently turn the first into the second.</para>
        ///
        /// <para>Optional and unbound by default, so every declaration written before this milestone
        /// keeps its exact candidate set.</para>
        /// </summary>
        public const string ContinuedLineFromParameter = "continuedLineFrom";

        /// <summary>
        /// Milestone 8.75b. WHOSE straight line <c>choose-line-destination@1</c> walks, when it is not
        /// the acting hero's - the space the subject is standing on, bound from the node that named
        /// them.
        ///
        /// <para><b>Why the declaration had to widen rather than a card compose around it.</b> Ignatia
        /// the Mad's Searing Heat and Scorching Blaze print "Move a friendly/enemy hero in radius 2 (2
        /// or 3) spaces <b>in a straight line</b>" - the only sentences in this build that put a
        /// straight line under somebody ELSE'S feet. Every earlier consumer of this offer moves the
        /// actor themselves (Swift's Bounce, Cutter's retreats, Misa's Blues, Silverarrow's travels,
        /// Hanu's Flight), so the walk read <c>context.ActorId</c> and was right to. Composed on
        /// <c>synthetic-direct-move@3</c> alone, her two faces offered p.11's whole reachability disk
        /// and the corner-turning spaces with it, which is a clause the card prints and the offer did
        /// not carry (<c>IgnatiaPushTests</c> measured ten offered against four on a file).</para>
        ///
        /// <para><b>It is this contract's business and not a new one.</b> This capability's whole
        /// subject is "where a straight-line movement written in card text ends", and p.19's Straight
        /// Line is one geometry whoever is walking it: the same six files, the same first-obstacle
        /// stop, the same floor and ceiling. What the parameter changes is the anchor, exactly as
        /// <see cref="ContinuedLineFromParameter"/> changes the heading and
        /// <see cref="IgnoresObstaclesParameter"/> changes what stops it - a fourth optional narrowing
        /// on the same discipline. A sibling capability would have been the identical walk with one
        /// coordinate read from another field.</para>
        ///
        /// <para><b>Unbound is the actor, so nothing composed before this milestone moves.</b> An
        /// unbound optional Coordinate arrives as no value at all, and the walk then asks where the
        /// acting hero is standing, which is what it always did. A bound space with no piece standing
        /// on it offers nothing rather than falling back to the actor - the subject is a referent the
        /// binding names, and reading its absence as "the actor" would silently move the wrong hero.
        /// </para>
        /// </summary>
        public const string LineSubjectSpaceParameter = "subjectSpace";

        /// <summary>
        /// Milestone 8.112d. A space fixing the HEADING of the line, read from where the mover stands
        /// through that space and on: Mrak's Boulder Rush, Boulder Blitz and Boulderdozer, "you may
        /// move up to 2 [3 / 4] spaces <b>in the direction of the push</b>".
        ///
        /// <para><b>It is <see cref="ContinuedLineFromParameter"/>'s mirror.</b> That parameter names
        /// the far end of a line already travelled BEHIND the mover; this one names a space AHEAD of
        /// the mover on the heading it keeps. The Boulders' push travels directly away from Mrak
        /// (p.18) from the space its subject stood on, so the space the selection published is the
        /// heading, and it stays the heading whether the push carried the subject one space, several
        /// or none: the text sets no condition on the follow move.</para>
        ///
        /// <para>A bound space equal to the mover's own, or off every one of p.19's six files, names
        /// no heading and keeps nothing - the reading its mirror gives a degenerate line. Optional and
        /// unbound everywhere else, so every consumer written before this milestone keeps its exact
        /// candidate set.</para>
        /// </summary>
        public const string LineHeadingThroughParameter = "headingThrough";

        /// <summary>
        /// Milestone 8.112e. The most EMPTY spaces the line may pass through - Mrak's Rolling Stone,
        /// "without moving through more than one empty space. (The starting space and the destination
        /// space do not count.)", and Strolling Stone's "more than two empty spaces".
        ///
        /// <para>Counted over the spaces strictly between the mover and the destination, which is the
        /// card's own parenthetical; a space is empty where it is no obstacle (p.18's endpoint
        /// question), so a unit, a token and terrain crossed under "ignoring obstacles" spend nothing.
        /// Unbound, the line has no budget at all, so every consumer written before this milestone keeps
        /// its exact candidate set; a bound zero is a line through obstacles only.</para>
        /// </summary>
        public const string LineMaximumEmptyCrossedParameter = "maximumEmptyCrossed";

        /// <summary>
        /// The hero a <c>retrieve-card@1</c> node actually retrieved for, published only on success -
        /// the "if they do" of Silverarrow's two Green healing faces.
        /// </summary>
        public const string RetrievedByOutputName = "retrievedBy";

        /// <summary>
        /// Milestone 8.40i. Whether the retrieval happened at all, published only on success - the "if
        /// they do" of Bain's Drinking Buddies and Another One!, whose consequence is about somebody
        /// ELSE.
        ///
        /// <para><b>Why the hero above could not carry it, measured rather than assumed.</b>
        /// Silverarrow's faces read "if they do" through the binding itself: the coin grant's recipient
        /// IS the hero the retrieval published, so a decline leaves the binding undefined and an
        /// <c>if able</c> grant is simply not made. Bain's second sentence is "you may ALSO retrieve",
        /// which is about the ACTOR - so nothing in the dependent step can be bound from the first one,
        /// and the condition needs a node of its own. An <see cref="EffectNodeKind.Optional"/> gated by
        /// a hero-consuming node cannot carry it either: the feasibility walk that decides whether to
        /// OFFER an optional branch treats a node whose required input is not yet bound as undecidable
        /// and therefore performable, so the prompt was opened and then refused - which is a wasted
        /// decision rather than a condition.</para>
        ///
        /// <para>So it is the count form of the fact, exactly as <c>discard-owned-card@1</c>'s
        /// <see cref="DidDiscardOutputName"/> is, and for the identical recorded reason: "the sentence
        /// after the semicolon has to be told, and a decline publishes nothing to tell it with". A
        /// <see cref="EffectNodeKind.Conditional"/> over <c>synthetic-is-flagged@1</c> reads it, and a
        /// binding left undefined by a decline reads as false, which is what the printed "if" asks
        /// for.</para>
        ///
        /// <para>Published only for a node that declared it, so Silverarrow's and Tali's four
        /// retrievals are byte-identical without naming it - and an added OUTPUT moves no corpus
        /// digest, because the identity walker digests a capability's reference and its Active-effect
        /// definitions and never its parameter schema.</para>
        /// </summary>
        public const string DidRetrieveOutputName = "didRetrieve";

        /// <summary>
        /// Milestone 8.57a. Whether a <c>gain-resource@1</c> node actually credited its coins,
        /// published only on success - the "if they do" of Rowenna's Paragon of Grace, "A friendly hero
        /// in radius gains 1 coin. <b>If they do</b>, repeat once on a different target".
        ///
        /// <para><b>It is <see cref="DidRetrieveOutputName"/>'s twin and exists for the identical
        /// recorded reason.</b> That output's remarks explain why Silverarrow's binding trick does not
        /// generalise: reading "if they do" through a later step's own binding works only while the
        /// dependent step is ABOUT the hero the first one published. What Paragon of Grace does next is
        /// repeat the whole Attack - a step about nobody in particular - so there is nothing for a
        /// binding to carry, and the condition needs the fact as a Boolean a
        /// <see cref="EffectNodeKind.Conditional"/> over <c>synthetic-is-flagged@1</c> can read.</para>
        ///
        /// <para><b>"They do" is the GRANT happening, which is not the same as a hero existing.</b> The
        /// clause before it is mandatory - "gains 1 coin", with no "may" - so the only ways it does not
        /// happen are the ways the step itself cannot: no friendly hero in radius for the selection to
        /// name, or a ledger that refuses the credit. Both leave this unpublished, and an unbound flag
        /// reads as false, which is exactly what the printed "if" asks for.</para>
        ///
        /// <para>Published only for a node that declared it, so the eleven coin grants composed before
        /// this milestone are byte-identical without naming it - and an added OUTPUT moves no corpus
        /// digest, for the reason <see cref="DidRetrieveOutputName"/> states.</para>
        /// </summary>
        public const string DidGrantOutputName = "didGrant";

        /// <summary>
        /// The Zone reach a card-text Fast Travel declares - p.11's own, or the "adjacent zone" both
        /// of Silverarrow's Gold basics print.
        /// </summary>
        public const string ZoneScopeParameter = "zoneScope";

        /// <summary>
        /// Where a straight-line movement written in card text ends. See
        /// <see cref="ChooseLineDestinationV1"/>.
        /// </summary>
        private sealed class LineDestinationHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();

            public EffectCapabilityReference Reference => ChooseLineDestinationV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("maximumDistance", EffectValueKind.Integer),

                        // Milestone 8.59. Reported at the table on Swift's Bounce: "why are
                        // unavailable spaces offered for this silver? It should just offer legal
                        // spaces 2 away, not this space 1 away which is illegal." The offer had only
                        // a ceiling, so "Move 2 spaces in a straight line" lit every space one away
                        // as well - and answering with one of them named a move the card cannot
                        // perform, which p.11 stops the action over.
                        //
                        // The floor belongs HERE rather than in the consuming Move, because the Move
                        // sees a space that was already named: only the offer can decline to name it.
                        // It is the third optional narrowing this declaration carries, on the same
                        // discipline `ignoresObstacles` and `continuedLineFrom` record - unbound (or
                        // bound to nought) reads as ONE, which is the floor every body written before
                        // this milestone already walked, so none of their candidate sets moves. It is
                        // named the way its sibling `choose-charge-line@1` has named the same bound
                        // since Brogan, where it is required rather than optional because all three
                        // of that declaration's consumers print an exact or enumerated distance.
                        new EffectCapabilityParameter(
                            "minimumDistance", EffectValueKind.Integer, isRequired: false),
                        new EffectCapabilityParameter(
                            IgnoresObstaclesParameter, EffectValueKind.Boolean, isRequired: false),

                        // Milestone 8.39a. Cutter's Evasive Shot and Tumble Shot, "After the attack:
                        // Move up to 2 spaces in the opposite direction". The line ran from the space
                        // she attacked, through where she stands, and the retreat continues it - so
                        // "the opposite direction" is one of the six this offer already walks, named
                        // by the Attack rather than chosen again.
                        new EffectCapabilityParameter(
                            ContinuedLineFromParameter, EffectValueKind.Coordinate,
                            isRequired: false),

                        // Milestone 8.75b. Whose line this is, when it is not the actor's - see
                        // LineSubjectSpaceParameter for why Ignatia's two pushes needed the anchor
                        // rather than a second capability.
                        new EffectCapabilityParameter(
                            LineSubjectSpaceParameter, EffectValueKind.Coordinate,
                            isRequired: false),

                        // Milestone 8.112d. "In the direction of the push" - see
                        // LineHeadingThroughParameter.
                        new EffectCapabilityParameter(
                            LineHeadingThroughParameter, EffectValueKind.Coordinate,
                            isRequired: false),

                        // Milestone 8.112e. Rolling and Strolling Stone's empty-space budget - see
                        // LineMaximumEmptyCrossedParameter.
                        new EffectCapabilityParameter(
                            LineMaximumEmptyCrossedParameter, EffectValueKind.Integer,
                            isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var destinations = Destinations(context);
                return destinations.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, destinations.Select(EffectValue.Space), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate ||
                    !Destinations(context).Any(value => value.Equals(selected[0].Coordinate)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("destination", EffectValue.Space(selected[0].Coordinate))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Destinations(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Derived on every ask - the offer, the answer and a restored pause - because the board is
            /// state, and a route remembered from the moment the question opened could name a space
            /// something has since stepped into.
            ///
            /// <para>A maximum of nought is a legal question with no answer, which is what RULING 37
            /// produces when the enemy did not move: "up to that number" is up to nothing, and the
            /// declaring node's own <c>up to</c> policy turns an empty offer into a step simply not
            /// taken.</para>
            ///
            /// <para><b>Milestone 8.59: the floor is a declaration, and its default is one.</b> An
            /// unbound optional Integer reads as nought here as everywhere, and nought is exactly the
            /// floor this walk already had - standing still is not a straight-line move - so it is
            /// raised to one rather than treated as a narrower question. A body that binds the
            /// parameter says the distance its printed sentence states, and a floor above the ceiling
            /// is a legal question with no answer, on the same reading as a ceiling of nought.</para>
            /// </summary>
            public static IReadOnlyList<HexCoordinate> Destinations(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var maximum = context.Input("maximumDistance").IntegerValue;
                var minimum = Math.Max(1, context.Input("minimumDistance").IntegerValue);
                var crosses = context.Inputs.ContainsKey(IgnoresObstaclesParameter) &&
                    context.Input(IgnoresObstaclesParameter).BooleanValue;

                // Milestone 8.39a. "In the opposite direction" names one of the six files this offer
                // already walks; it does not change how far the line reaches or what stops it.
                var continuesFrom = context.Inputs.ContainsKey(ContinuedLineFromParameter)
                    ? context.Input(ContinuedLineFromParameter).Coordinate
                    : (HexCoordinate?)null;

                // Milestone 8.75b. Whose feet the line runs from. An unbound anchor is the acting
                // hero - every consumer written before Ignatia - and a bound one names the piece
                // standing on that space, which is the hero her two pushes are moving. A bound space
                // with nothing on it names a referent that is not there and keeps nothing.
                var subjectSpaceInput = context.Input(LineSubjectSpaceParameter);
                var subjectSpace = subjectSpaceInput.Kind == EffectValueKind.Coordinate
                    ? subjectSpaceInput.Coordinate
                    : (HexCoordinate?)null;
                // Milestone 8.112d. The heading a named space ahead of the mover fixes.
                var headingInput = context.Input(LineHeadingThroughParameter);
                var headingThrough = headingInput.Kind == EffectValueKind.Coordinate
                    ? headingInput.Coordinate
                    : (HexCoordinate?)null;

                // Milestone 8.112e. Unbound is no budget; a negative bound admits nothing.
                var budgetInput = context.Input(LineMaximumEmptyCrossedParameter);
                var emptyBudget = budgetInput.Kind == EffectValueKind.Integer
                    ? budgetInput.IntegerValue
                    : (int?)null;
                if (emptyBudget < 0)
                {
                    return Array.Empty<HexCoordinate>();
                }
                if (board == null || maximum < 1 || minimum > maximum)
                {
                    return Array.Empty<HexCoordinate>();
                }

                BoardObjectReference mover;
                HexCoordinate moverAt;
                if (subjectSpace.HasValue)
                {
                    if (!board.TryGetObjectAt(subjectSpace.Value, out mover) || !mover.IsDefined)
                    {
                        return Array.Empty<HexCoordinate>();
                    }

                    moverAt = subjectSpace.Value;
                }
                else
                {
                    mover = BoardObjectReference.ForHero(context.ActorId);
                    if (!board.TryGetObjectCoordinate(mover, out moverAt))
                    {
                        return Array.Empty<HexCoordinate>();
                    }
                }

                var found = new List<HexCoordinate>();
                foreach (var route in board.FindDirectMoveDestinations(
                    mover, maximum, context.Modifications, moveIgnoresObstacles: crosses))
                {
                    if (route.Cost < minimum || route.Cost > maximum ||
                        !StraightLineQuery.TryGetDirection(
                            moverAt, route.Destination, out var direction, out var distance) ||
                        distance != route.Cost || !IsStraightRoute(moverAt, direction, route.Route))
                    {
                        continue;
                    }

                    // The narrowing is applied to the walked route rather than to the six headings,
                    // so a retreat still stops at the first thing p.19 stops it at - and a bound
                    // space the mover is standing on names no heading and keeps nothing.
                    if (continuesFrom.HasValue && !ContinuedLineQuery.ContinuesTheLine(
                            continuesFrom.Value, moverAt, route.Destination))
                    {
                        continue;
                    }

                    if (headingThrough.HasValue &&
                        (!StraightLineQuery.TryGetDirection(
                             moverAt, headingThrough.Value, out var heading, out _) ||
                         heading != direction))
                    {
                        continue;
                    }

                    // Milestone 8.112e. The empty spaces strictly between, against the budget.
                    if (emptyBudget.HasValue &&
                        Enumerable.Range(1, distance - 1)
                            .Select(step => StraightLineQuery.Step(moverAt, direction, step))
                            .Count(space => !ObstacleQuery.IsObstacle(
                                context.Components.Tokens, space, context.Modifications)) >
                        emptyBudget.Value)
                    {
                        continue;
                    }

                    found.Add(route.Destination);
                }

                return found
                    .Distinct()
                    .OrderBy(value => value.Q)
                    .ThenBy(value => value.R)
                    .ToArray();
            }
        }

        /// <summary>
        /// Rulebook p.11's Fast Travel performed by card text. See <see cref="FastTravelV1"/>.
        /// </summary>
        private sealed class CardTextFastTravelHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => FastTravelV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            ZoneScopeParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<FastTravelZoneScope>())
                    },
                    new[]
                    {
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var destinations = Destinations(context);
                return destinations.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, destinations.Select(EffectValue.Space), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate ||
                    !Destinations(context).Any(value => value.Equals(selected[0].Coordinate)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var components = context.Components;
                // Milestone 8.61e. A charge is a straight line by construction - `IsStraightRoute` is
                // what admits the destination at all - so the spaces it entered and left are p.18's
                // realised line between where the charge started and where it stopped.
                var actor = BoardObjectReference.ForHero(context.ActorId);
                components.Board.TryGetObjectCoordinate(actor, out var chargeFrom);
                var committed = components.Board.CommitValidatedDisplacement(
                    actor,
                    selected[0].Coordinate,
                    GuardsDigital.Core.Rules.Displacement.MoveThroughTrapQuery.RealisedLine(
                        chargeFrom, selected[0].Coordinate));
                if (!committed.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("destination", EffectValue.Space(committed.Destination))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Destinations(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The Fast Travel this text may perform, or nothing at all - which is the case all three
            /// faces answer with "if able", and which happens for two separate printed reasons: p.18
            /// bars the hero from Fast Travel outright, or p.11 leaves them no eligible Zone.
            /// </summary>
            public static IReadOnlyList<HexCoordinate> Destinations(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace || context.Participants == null ||
                    !context.CardState.IsAvailable)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var scope = (FastTravelZoneScope)context.Input(ZoneScopeParameter).IntegerValue;
                if (!Enum.IsDefined(typeof(FastTravelZoneScope), scope))
                {
                    return Array.Empty<HexCoordinate>();
                }

                // The same question p.12's offer asks, read against the colour of the card this text
                // is printed on: a colour-qualified restriction reaches a card-text Fast Travel exactly
                // as it reaches the replacement action performed from the same card.
                if (ActionRestrictionQuery.IsRestrictedFrom(
                    context.ActorId,
                    ActionKind.FastTravel,
                    context.Modifications,
                    ActionOfferQuery.SourceCardColor(
                        context.SourceCardInstanceId,
                        context.CardState.Definitions,
                        context.CardState.Instances)))
                {
                    return Array.Empty<HexCoordinate>();
                }

                return components.Tokens == null
                    ? Array.Empty<HexCoordinate>()
                    : FastTravelActionQuery.FindDestinationsForActor(
                        context.ActorId,
                        components.Board,
                        components.Tokens,
                        context.Participants,
                        components.Board.Minions,
                        scope);
            }
        }

        private static EffectCapabilityParameterSchema TargetSchema(
            IEnumerable<EffectCapabilityParameter> extra = null,
            IEnumerable<EffectOutputBinding> outputs = null) =>
            new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        OriginParameter,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(RosterQueryOrigin.None)),
                    // Milestone 9.2: exactly one of a written reach and the printed Radius is bound.
                    new EffectCapabilityParameter(DistanceParameter, EffectValueKind.Integer, isRequired: false),
                    new EffectCapabilityParameter(
                        MeasuresPrintedRadiusParameter, EffectValueKind.Boolean, isRequired: false),
                    new EffectCapabilityParameter(
                        UnitKindParameter,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(RosterUnitKind.None)),
                    new EffectCapabilityParameter(
                        RelationParameter,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(SourceTeamRelation.None)),
                    new EffectCapabilityParameter(
                        "activity",
                        EffectValueKind.Integer,
                        isRequired: false,
                        integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None)),

                    // Milestone 8.24. p.22's class adjective, where the sentence prints one:
                    // Xargatha's Constrict reaches "an enemy melee minion adjacent to you" and Final
                    // Embrace "an enemy melee or ranged minion". It sits beside `unitKind` rather than
                    // inside it because the two answer different questions - p.19's kind of piece, and
                    // p.22's class of minion - and a card can print either without the other.
                    // Optional and defaulting to every class, so every roster selection declared
                    // before this milestone keeps its exact candidate set.
                    new EffectCapabilityParameter(
                        AdmittedMinionClassParameter,
                        EffectValueKind.Integer,
                        isRequired: false,
                        integerDomain: EffectIntegerDomain.All<RosterMinionClass>())
                }.Concat(extra ?? Enumerable.Empty<EffectCapabilityParameter>()),
                outputs ?? new[]
                {
                    new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion)
                });

        private abstract class TargetingHandler : IEffectImmunityOverrideCapabilityHandler
        {
            public abstract IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities { get; }

            protected static IReadOnlyList<BoardObjectReference> Targets(
                EffectCapabilityContext context,
                RosterQueryOrigin origin,
                int distance,
                RosterUnitKind unitKind,
                SourceTeamRelation relation,
                RuleActivity activity,
                bool excludeEnclosingTarget = false)
            {
                if (!TryOrigin(context, origin, out var at) || distance < 0 ||
                    unitKind == RosterUnitKind.None ||
                    !Enum.IsDefined(typeof(RosterUnitKind), unitKind) ||
                    relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    activity == RuleActivity.None ||
                    !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var board = context.Components?.Board;
                if (board == null || context.Participants == null)
                    return Array.Empty<BoardObjectReference>();

                // Milestone 8.24. The class adjective, applied to the minions in the set and to
                // nothing else - a sentence naming "an enemy melee minion" says nothing about heroes,
                // and a selection admitting both kinds keeps every hero it already admitted.
                var admittedClass =
                    (RosterMinionClass)context.Input(AdmittedMinionClassParameter).IntegerValue;
                if (!Enum.IsDefined(typeof(RosterMinionClass), admittedClass))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                return RosterPresenceQuery.FindUnitsWithin(
                        at,
                        distance,
                        unitKind,
                        relation,
                        context.ActorId,
                        board,
                        context.Participants,
                        context.Modifications,
                        BoardObjectReference.ForHero(context.ActorId),
                        excludeEnclosingTarget ? EnclosingTarget(context) : default)
                    .Where(target => AdmitsClass(admittedClass, target, board))
                    .Where(target => !TargetQuery.IsPrevented(
                        target.AsRuleObject(),
                        activity,
                        RuleObject.ForHero(context.ActorId),
                        board,
                        context.Participants,
                        context.Modifications,
                        context.ImmunityOverrides))
                    .ToArray();
            }

            /// <summary>
            /// Whether one piece in a selection's set is of a class the printed sentence admits.
            /// A hero is admitted by every class adjective, because the adjective is about minions.
            /// </summary>
            private static bool AdmitsClass(
                RosterMinionClass admittedClass,
                BoardObjectReference target,
                IBoardDisplacementAuthority board)
            {
                if (admittedClass == RosterMinionClass.AnyClass ||
                    target.Kind != BoardObjectKind.Minion)
                {
                    return true;
                }

                var minion = board.Minions
                    .FirstOrDefault(value => value.MinionId.Equals(target.MinionId));
                if (minion == null)
                {
                    return false;
                }

                return AdmittedClassIncludes(admittedClass, minion.MinionType);
            }

            protected static IReadOnlyList<BoardObjectReference> DeclaredTargets(
                EffectCapabilityContext context)
            {
                var declaredActivity = (RuleActivity)context.Input("activity").IntegerValue;
                var activity = declaredActivity == RuleActivity.None
                    ? context.EnclosingActivity
                    : declaredActivity;
                return Targets(
                    context,
                    (RosterQueryOrigin)context.Input(OriginParameter).IntegerValue,
                    DeclaredReach(context),
                    (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue,
                    (SourceTeamRelation)context.Input(RelationParameter).IntegerValue,
                    activity,
                    context.Input(ExcludeTargetParameter).BooleanValue);
            }

            /// <summary>
            /// Milestone 9.2. The written reach, or the card's printed Radius with p.16's items where
            /// the selection measures it. A card printing no Radius has no disk: nobody is in it.
            /// </summary>
            internal static int DeclaredReach(EffectCapabilityContext context)
            {
                var measuresRadius = context.Inputs.TryGetValue(MeasuresPrintedRadiusParameter, out var flag) &&
                    flag.BooleanValue;
                var writesReach = context.Inputs.ContainsKey(DistanceParameter);
                if (measuresRadius == writesReach)
                {
                    throw new InvalidOperationException(
                        "A roster selection binds exactly one of a written reach and the printed Radius.");
                }

                if (writesReach)
                {
                    return context.Input(DistanceParameter).IntegerValue;
                }

                if (!context.CardState.IsAvailable)
                {
                    return -1;
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications);
                return radius.Total <= 0 ? -1 : radius.Total;
            }

            protected static EffectValue AsValue(BoardObjectReference target) =>
                target.Kind == BoardObjectKind.Hero
                    ? EffectValue.Hero(target.HeroId)
                    : EffectValue.Minion(target.MinionId);

            protected static EffectOutcomeFact AsOutcome(
                BoardObjectReference target,
                string heroOutput = ChosenHeroOutput,
                string minionOutput = ChosenMinionOutput) =>
                target.Kind == BoardObjectKind.Hero
                    ? new EffectOutcomeFact(heroOutput, EffectValue.Hero(target.HeroId))
                    : new EffectOutcomeFact(minionOutput, EffectValue.Minion(target.MinionId));

            protected static BoardObjectReference FromValue(EffectValue value)
            {
                if (value.Kind == EffectValueKind.Hero)
                    return BoardObjectReference.ForHero(value.HeroId);
                if (value.Kind == EffectValueKind.Minion)
                    return BoardObjectReference.ForMinion(value.MinionId);
                return default;
            }

            /// <summary>
            /// Milestone 8.22. Where a chosen piece is standing, added to the outcome only when the
            /// node asked for it - the identical shape milestone 8.20 gave <c>choose-target@2</c>'s
            /// <c>chosenSpace</c> and milestone 8.20 gave the printed-icon minion arm.
            ///
            /// <para>Sabina's multi-piece selections need it because the operations they feed take
            /// spaces: p.19's Swap exchanges two coordinates and Remove takes one, while her sentences
            /// name minions. Reading the space now rather than deriving it from the identity later is
            /// what makes it the answer's own fact - a node between the choice and the operation may
            /// have moved something, and the coordinate published here is the one the chooser was
            /// looking at.</para>
            /// </summary>
            protected static void AddSpaceIfPublished(
                EffectCapabilityContext context,
                ICollection<EffectOutcomeFact> facts,
                BoardObjectReference target,
                string spaceOutput)
            {
                if (context.Publishes(spaceOutput) &&
                    context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(target, out var at))
                {
                    facts.Add(new EffectOutcomeFact(spaceOutput, EffectValue.Space(at)));
                }
            }
        }

        private sealed class ChooseTargetHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                AllActivities;
            public EffectCapabilityReference Reference => ChooseTargetV2;

            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                // Milestone 8.20. Carried by this selection alone, because it is the only one a
                // verified card asks the question of - the same rule that keeps every other narrowing
                // on the declaration that prints it. `presence-count@1` names its own, beside an
                // actor exclusion this selection makes unconditionally.
                new[]
                {
                    new EffectCapabilityParameter(
                        ExcludeTargetParameter, EffectValueKind.Boolean, isRequired: false),
                    new EffectCapabilityParameter(
                        PlayedAttackThisTurnParameter, EffectValueKind.Boolean, isRequired: false),
                    new EffectCapabilityParameter(
                        AdjacentToFriendlyMinionParameter, EffectValueKind.Boolean, isRequired: false),

                    // Milestone 8.39d. Cutter's three Blue discards, "An enemy hero adjacent to
                    // another enemy unit". A second boolean rather than a shared pair, because
                    // re-expressing Sabina's would change her node's inputs.
                    new EffectCapabilityParameter(
                        AdjacentToAnotherEnemyUnitParameter, EffectValueKind.Boolean,
                        isRequired: false),

                    // Milestone 8.39h. The other half of that same sentence - "and not adjacent to
                    // you" - which 8.39d recorded as already registered against a capability that
                    // reads an enclosing Attack's target. These three faces are Skills. See
                    // `ExcludeAdjacentParameter` for the correction; the spelling is the printed-icon
                    // family's own constant, so one clause has one name in this catalog.
                    new EffectCapabilityParameter(
                        ExcludeAdjacentParameter, EffectValueKind.Boolean, isRequired: false),

                    // Milestone 8.39a. Cutter's three lunges, "Target a hero adjacent to you in the
                    // direction of the move". It sits beside the three narrowings above for the reason
                    // they sit here: the clause is part of WHO may be picked, so a hero standing off
                    // the line is not one to offer and then refuse.
                    new EffectCapabilityParameter(
                        ContinuedLineFromParameter, EffectValueKind.Coordinate, isRequired: false),

                    // Milestone 8.40c. Bain's Hand Crossbow, "Target a hero in range with a [bounty]
                    // bounty marker" - the seventh narrowing and the first about a COMPONENT rather
                    // than about the board, the geometry or a candidate's own cards.
                    new EffectCapabilityParameter(
                        HoldsTheActorsMarkerParameter, EffectValueKind.Boolean, isRequired: false),

                    // Milestone 8.57c. Rowenna's two Blue Defeats, "an enemy hero adjacent to you
                    // with no cards in hand" - the eighth narrowing, and the printed-icon family's
                    // `minimumCardsInHand` complement carried under that family's own name.
                    new EffectCapabilityParameter(
                        RequiresEmptyHandParameter, EffectValueKind.Boolean, isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion),
                    new EffectOutputBinding(ChosenSpaceOutput, EffectValueKind.Coordinate)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// The declared targets, narrowed by the one clause this selection can carry about the
            /// card a candidate has already played. Derived on every ask - the offer, the answer and a
            /// restored pause - because a hero's turn slot is state and a set remembered from the
            /// moment the question opened could name a card that is no longer there.
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> Candidates(
                EffectCapabilityContext context)
            {
                var declared = DeclaredTargets(context);

                // Milestone 8.22. Each narrowing filters the set the one before it left, because a
                // card printing both clauses would mean both - none does today, and reading them as
                // alternatives would be a decision no face has asked for.
                if (context.Input(AdjacentToFriendlyMinionParameter).BooleanValue)
                {
                    declared = AdjacentToAFriendlyMinion(context, declared);
                }

                if (context.Input(AdjacentToAnotherEnemyUnitParameter).BooleanValue)
                {
                    declared = AdjacentToAnotherUnitOfItsOwnSide(context, declared);
                }

                // Milestone 8.39h, and it filters after the clause above for the reason that clause
                // filters after Sabina's: a card printing both means both, and Cutter's three Blue
                // discards print both in one sentence.
                if (context.Input(ExcludeAdjacentParameter).BooleanValue)
                {
                    declared = StandingBeyondTheActorsOwnRing(context, declared);
                }

                // Milestone 8.39a, and it is bound rather than flagged because the clause carries a
                // space rather than a yes: the space an earlier step of this action started from.
                if (context.Inputs.ContainsKey(ContinuedLineFromParameter))
                {
                    declared = AlongTheContinuedLine(context, declared);
                }

                // Milestone 8.40c, and it filters in its turn for the reason the clauses above do: a
                // card printing two of them means both.
                if (context.Input(HoldsTheActorsMarkerParameter).BooleanValue)
                {
                    declared = WearingAMarkerTheActorDeclared(context, declared);
                }

                // Milestone 8.57c, and it filters in its turn for the same reason.
                if (context.Input(RequiresEmptyHandParameter).BooleanValue)
                {
                    declared = HoldingNoCardsAtAll(context, declared);
                }

                if (!context.Input(PlayedAttackThisTurnParameter).BooleanValue)
                {
                    return declared;
                }

                if (!context.RosterState.IsAvailable || !context.CardState.IsAvailable)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                return declared
                    .Where(target => target.Kind == BoardObjectKind.Hero &&
                        PlayedAttackCardThisTurnQuery.HasPlayed(
                            target.HeroId,
                            context.RoundIndex,
                            context.TurnIndex,
                            context.RosterState.TurnSlots,
                            context.CardState.Definitions,
                            context.CardState.Instances))
                    .ToArray();
            }

            /// <summary>
            /// The candidates standing beside at least one minion allied with the actor.
            ///
            /// <para>The question is asked from each candidate outwards rather than from the actor's
            /// minions inwards, and that is not just convenience: the sentence bounds the hero by the
            /// card's Radius and bounds the minion by nothing at all, so a minion outside the disk
            /// still qualifies the hero standing next to it. A disk of one drawn on the candidate is
            /// exactly "adjacent to it" with no second bound smuggled in.</para>
            ///
            /// <para>It reaches <see cref="RosterPresenceQuery"/> rather than walking occupancies here,
            /// so "friendly minion" means what it means to every other declaration in this vocabulary -
            /// including the effective-type overlays that let a piece count as a minion, which p.17
            /// allows and which a raw occupancy scan would silently miss.</para>
            /// </summary>
            /// <summary>
            /// Milestone 8.39a. The candidates standing on the continuation of a line an earlier step
            /// of this action already travelled - Cutter's lunges, "Target a hero adjacent to you in
            /// the direction of the move".
            ///
            /// <para>The line runs from the bound space, through where the actor is standing NOW, and
            /// on; <see cref="ContinuedLineQuery"/> answers it, and this filter does nothing else. The
            /// distance the clause reaches is still the selection's own <c>reach</c>, which is what
            /// makes "adjacent to you in the direction of the move" exactly one space where the
            /// unnarrowed clause admits up to six.</para>
            ///
            /// <para><b>A heading the actor never travelled admits nobody.</b> The bound space equal to
            /// the actor's own, an actor the board cannot place, and a pair of spaces sharing no file
            /// all name no direction, and each answers with the empty set rather than with the
            /// unnarrowed one.</para>
            /// </summary>
            /// <summary>
            /// Milestone 8.40c. The candidates wearing a marker the actor's own content put out -
            /// Bain's Hand Crossbow, "Target a hero in range with a [bounty] bounty marker".
            ///
            /// <para>A minion is dropped rather than admitted, on
            /// <c>AttackDefenseCardActionStrategyBackend.AdmitsTheSubject</c>'s reading: p.18 gives
            /// markers to heroes, so a clause narrowed by one that let every minion through would be
            /// reading the sentence as if it were not there.</para>
            ///
            /// <para>An absent marker register admits nobody rather than everybody, which is the same
            /// answer <see cref="AlongTheContinuedLine"/> gives an absent board: the two silent
            /// alternatives are a widened offer and a card greyed for nothing, and only one of them is
            /// a refusal the player can see.</para>
            /// </summary>
            /// <summary>
            /// Milestone 8.57c. The candidates holding no cards at all - Rowenna's Accept Surrender
            /// and Glorious Triumph, "an enemy hero adjacent to you with no cards in hand".
            ///
            /// <para><b>A minion is dropped rather than admitted</b>, on the reading
            /// <see cref="WearingAMarkerTheActorDeclared"/> records: p.10 gives cards to heroes, so a
            /// clause about a hand that let every minion through would be reading the sentence as if
            /// it were not there. Both faces that print the clause admit only heroes anyway, so this
            /// changes nothing for them and states the rule rather than relying on the caller.</para>
            ///
            /// <para><b>Card state the fixture did not bind admits nobody rather than everybody</b>,
            /// which is the same answer <see cref="AlongTheContinuedLine"/> gives an absent board: a
            /// narrowing that silently does nothing would offer a Defeat the sentence forbids, and of
            /// the two silent alternatives only a greyed card is one the player can see.</para>
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> HoldingNoCardsAtAll(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> declared)
            {
                if (!context.CardState.IsAvailable)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                return declared
                    .Where(target => target.Kind == BoardObjectKind.Hero &&
                        GuardsDigital.Core.Cards.HeroHandQuery.IsEmpty(
                            context.CardState.Instances, target.HeroId))
                    .ToArray();
            }

            private static IReadOnlyList<BoardObjectReference> WearingAMarkerTheActorDeclared(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> declared)
            {
                var markers = context.Components?.Markers;
                if (markers == null)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var holders = HeldMarkerQuery.HoldersOfAMarkerDeclaredBy(
                    markers.Attachments, context.ActorId);
                return declared
                    .Where(target => target.Kind == BoardObjectKind.Hero &&
                        holders.Any(holder => holder.Equals(target.HeroId)))
                    .ToArray();
            }

            private static IReadOnlyList<BoardObjectReference> AlongTheContinuedLine(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> declared)
            {
                var board = context.Components?.Board;
                if (board == null || !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                // Milestone 9.9a. The heading runs through where the actor stands - on a hypothetical
                // board, the square the offer walk is judging him from, exactly as the origin of the
                // selection itself is read.
                if (context.ProspectiveActorSpace.HasValue)
                {
                    actorAt = context.ProspectiveActorSpace.Value;
                }

                var from = context.Input(ContinuedLineFromParameter).Coordinate;
                var kept = new List<BoardObjectReference>();
                foreach (var target in declared)
                {
                    if (board.TryGetObjectCoordinate(target, out var targetAt) &&
                        ContinuedLineQuery.ContinuesTheLine(from, actorAt, targetAt))
                    {
                        kept.Add(target);
                    }
                }

                return kept;
            }

            /// <summary>
            /// Milestone 8.39d. The candidates standing beside at least one OTHER unit on their own
            /// side - the clause Cutter's three Blue discards print as "adjacent to another enemy
            /// unit".
            ///
            /// <para>"Enemy" is read from the ACTOR, as every relation in this catalog is, so a unit
            /// that opposes the actor is on the candidate's own side by construction - the candidate
            /// opposes the actor too. That is why this asks the same <c>OpposingSource</c> question
            /// Sabina's asks with <c>AlliedWithSource</c>, rather than inventing a relation measured
            /// from the subject.</para>
            ///
            /// <para><b>The candidate is excluded from its own neighbourhood.</b> p.9 measures a disk
            /// inclusively, so a radius of one around an enemy hero contains that hero - who is an
            /// enemy unit. Without this exclusion the clause would admit every enemy hero on the
            /// board, which is the opposite of what "another" says.</para>
            /// </summary>
            /// <summary>
            /// Milestone 8.39h. The candidates standing further than p.9's innermost ring from the
            /// actor - "and not adjacent to you", the second half of the clause Cutter's Bombardment,
            /// Barrage and Broadside print.
            ///
            /// <para>It is the identical test <c>synthetic-radius-hero-target@1</c> has applied since
            /// milestone 8.11 for Wasp's Shock: the exact distance the word means rather than "further
            /// than one", so it says nothing about the actor's own space and does not quietly depend on
            /// that space having been excluded elsewhere. This selection excludes the actor
            /// unconditionally anyway, which is why the two readings agree everywhere.</para>
            ///
            /// <para>A board that cannot place the actor keeps nobody rather than everybody: a
            /// narrowing that silently does nothing is worse than one that refuses, which is the
            /// reading milestone 8.39d1 recorded for the same reason.</para>
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> StandingBeyondTheActorsOwnRing(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> declared)
            {
                var board = context.Components?.Board;
                if (board == null || !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var kept = new List<BoardObjectReference>();
                foreach (var target in declared)
                {
                    if (board.TryGetObjectCoordinate(target, out var targetAt) &&
                        HexDistanceQuery.Calculate(actorAt, targetAt) != 1)
                    {
                        kept.Add(target);
                    }
                }

                return kept;
            }

            private static IReadOnlyList<BoardObjectReference> AdjacentToAnotherUnitOfItsOwnSide(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> declared)
            {
                var board = context.Components?.Board;
                if (board == null || context.Participants == null)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var kept = new List<BoardObjectReference>();
                foreach (var target in declared)
                {
                    if (!board.TryGetObjectCoordinate(target, out var targetAt)) continue;
                    var beside = RosterPresenceQuery.FindUnitsWithin(
                        targetAt,
                        1,
                        RosterUnitKind.Unit,
                        SourceTeamRelation.OpposingSource,
                        context.ActorId,
                        board,
                        context.Participants,
                        context.Modifications);

                    if (beside.Any(unit => !unit.Equals(target)))
                    {
                        kept.Add(target);
                    }
                }

                return kept;
            }

            private static IReadOnlyList<BoardObjectReference> AdjacentToAFriendlyMinion(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> declared)
            {
                var board = context.Components?.Board;
                if (board == null || context.Participants == null)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var kept = new List<BoardObjectReference>();
                foreach (var target in declared)
                {
                    if (!board.TryGetObjectCoordinate(target, out var targetAt)) continue;
                    var beside = RosterPresenceQuery.FindUnitsWithin(
                        targetAt,
                        1,
                        RosterUnitKind.Minion,
                        SourceTeamRelation.AlliedWithSource,
                        context.ActorId,
                        board,
                        context.Participants,
                        context.Modifications);

                    // p.9 measures a disk inclusively, so a radius of one contains the candidate's own
                    // space. A minion cannot be standing on an enemy hero, so nothing needs excluding.
                    if (beside.Count > 0)
                    {
                        kept.Add(target);
                    }
                }

                return kept;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected?.Count == 1 ? FromValue(selected[0]) : default;
                if (!chosen.IsDefined || !Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.20 publishes where the chosen piece is standing beside who it is, for a
                // node that asked - the space is read now rather than derived later by whatever
                // consumes it, because that is the moment the answer is about and a later node may
                // already have moved something. A node that did not ask never sees it, which is what
                // keeps every milestone 8.19b definition publishing exactly what it declared.
                var outcomes = new List<EffectOutcomeFact> { AsOutcome(chosen) };
                if (context.Publishes(ChosenSpaceOutput) &&
                    context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(chosen, out var at))
                {
                    outcomes.Add(new EffectOutcomeFact(ChosenSpaceOutput, EffectValue.Space(at)));
                }

                return EffectExecutionResult.Applied(outcomes);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private sealed class ChooseTargetSetHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();
            public EffectCapabilityReference Reference => ChooseTargetSetV1;
            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                new[]
                {
                    new EffectCapabilityParameter("minimum", EffectValueKind.Integer),
                    new EffectCapabilityParameter("maximum", EffectValueKind.Integer)
                },
                new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                    new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                    new EffectOutputBinding("secondMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                    new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out var minimum, out var maximum))
                    return EffectExecutionResult.ImpossibleMandatory();
                if (maximum == 0)
                    return SetOutcome(context, Array.Empty<BoardObjectReference>());
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, candidates.Select(AsValue), minimum, maximum));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out var minimum, out var maximum))
                    return EffectExecutionResult.ImpossibleMandatory();
                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                if (chosen.Any(value => !value.IsDefined || !candidates.Contains(value)) ||
                    chosen.Distinct().Count() != chosen.Length ||
                    chosen.Length < minimum || chosen.Length > maximum)
                    return EffectExecutionResult.ImpossibleMandatory();
                return SetOutcome(context, Canonical(chosen));
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out _, out var maximum))
                    return EffectExecutionResultKind.ImpossibleMandatory;
                return maximum == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
            }

            private static bool Bounds(
                EffectCapabilityContext context, int candidates, out int minimum, out int maximum)
            {
                minimum = context.Input("minimum").IntegerValue;
                maximum = context.Input("maximum").IntegerValue;
                if (minimum < 0 || maximum < minimum || maximum > 2) return false;
                maximum = Math.Min(maximum, candidates);
                return minimum <= maximum;
            }

            private static EffectExecutionResult SetOutcome(
                EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> selected)
            {
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact("selectedCount", EffectValue.Integer(selected.Count))
                };
                if (selected.Count > 0)
                {
                    facts.Add(AsOutcome(selected[0], "firstHero", "firstMinion"));
                    AddSpaceIfPublished(context, facts, selected[0], "firstSpace");
                }

                if (selected.Count > 1)
                {
                    facts.Add(AsOutcome(selected[1], "secondHero", "secondMinion"));
                    AddSpaceIfPublished(context, facts, selected[1], "secondSpace");
                }

                return EffectExecutionResult.Applied(facts);
            }
        }

        /// <summary>
        /// Milestone 8.35z33. <c>choose-target-trio@1</c> - <see cref="ChooseTargetSetHandler"/>'s
        /// question with a third slot, for Whisper's Greater Good: "You defeat up to 3 minions
        /// adjacent to you."
        ///
        /// <para><b>Zero is a legal answer and that is the whole of "up to".</b> The minimum the
        /// definition declares is what a bare "up to N" means at its floor, and Ruling 18 is why it
        /// matters here rather than being a formality: "a minion" includes a friendly one, so an
        /// opponent who chose this bullet may be handing Whisper nothing but her own minions, and
        /// naming none of them is the answer she is entitled to give. Nothing in the count is
        /// forced.</para>
        ///
        /// <para>Everything else is the two-slot handler's: the same candidate derivation, the same
        /// p.18 immunity question, the same canonical ordering of the answer, so the two selections
        /// cannot come to disagree about who is standing where.</para>
        /// </summary>
        private sealed class ChooseTargetTrioHandler : TargetingHandler, IEffectCapabilityHandler
        {
            /// <summary>The most slots this selection publishes, and therefore its ceiling.</summary>
            private const int Slots = 3;

            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();

            public EffectCapabilityReference Reference => ChooseTargetTrioV1;

            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                new[]
                {
                    new EffectCapabilityParameter("minimum", EffectValueKind.Integer),
                    new EffectCapabilityParameter("maximum", EffectValueKind.Integer)
                },
                new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                    new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                    new EffectOutputBinding("secondMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("thirdHero", EffectValueKind.Hero),
                    new EffectOutputBinding("thirdMinion", EffectValueKind.Minion)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out var minimum, out var maximum))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return maximum == 0
                    ? SetOutcome(Array.Empty<BoardObjectReference>())
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), minimum, maximum));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out var minimum, out var maximum))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                if (chosen.Any(value => !value.IsDefined || !candidates.Contains(value)) ||
                    chosen.Distinct().Count() != chosen.Length ||
                    chosen.Length < minimum || chosen.Length > maximum)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return SetOutcome(Canonical(chosen));
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out _, out var maximum))
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                return maximum == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
            }

            private static bool Bounds(
                EffectCapabilityContext context, int candidates, out int minimum, out int maximum)
            {
                minimum = context.Input("minimum").IntegerValue;
                maximum = context.Input("maximum").IntegerValue;
                if (minimum < 0 || maximum < minimum || maximum > Slots)
                {
                    return false;
                }

                maximum = Math.Min(maximum, candidates);
                return minimum <= maximum;
            }

            private static EffectExecutionResult SetOutcome(
                IReadOnlyList<BoardObjectReference> selected)
            {
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact("selectedCount", EffectValue.Integer(selected.Count))
                };

                if (selected.Count > 0)
                {
                    facts.Add(AsOutcome(selected[0], "firstHero", "firstMinion"));
                }

                if (selected.Count > 1)
                {
                    facts.Add(AsOutcome(selected[1], "secondHero", "secondMinion"));
                }

                if (selected.Count > 2)
                {
                    facts.Add(AsOutcome(selected[2], "thirdHero", "thirdMinion"));
                }

                return EffectExecutionResult.Applied(facts);
            }
        }

        /// <summary>
        /// Milestone 8.35l gave this selection the two members that let it be narrowed, and the reason
        /// it had none is worth stating rather than inferring. Milestone 6.8l's contract is opt-in: a
        /// selection accepts a consumer by declaring the parameter, and this one declared none, so
        /// nothing about Sabina's "Swap two minions in radius" could be narrowed even in principle -
        /// the narrowing was <em>unavailable</em> rather than merely undeclared, and no content edit
        /// could have reached it. With Wasp's Magnetic Dagger standing, the offer was made on a board
        /// where the only pair formable was barred, the player took it, and the card was spent for
        /// nothing.
        ///
        /// <para>Its floor of two is the part that does not follow from the rest. A selection narrowed
        /// to one legal minion is narrowed correctly and is still unanswerable, so the offer question
        /// has to read the floor as well as the count - see
        /// <c>EffectSelectionEligibilityQuery.JudgeSelection</c>, where the live pause's own floor check
        /// finally has its counterpart at offer time.</para>
        /// </summary>
        private sealed class ChooseTargetPairHandler
            : TargetingHandler, IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                SwapActivity;
            public EffectCapabilityReference Reference => ChooseTargetPairV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            OriginParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(
                                RosterQueryOrigin.None)),
                        new EffectCapabilityParameter(DistanceParameter, EffectValueKind.Integer),
                        new EffectCapabilityParameter(
                            RelationParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(
                                SourceTeamRelation.None)),
                        // Milestone 8.35l. Optional, exactly as every other selection declares it: a
                        // pair whose result no single later node consumes still has a legal shape.
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node,
                            isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                        new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                        new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                        new EffectOutputBinding("secondMinion", EffectValueKind.Minion),
                        new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                        new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<BoardObjectReference> Candidates(EffectCapabilityContext context) =>
                Targets(
                    context,
                    (RosterQueryOrigin)context.Input(OriginParameter).IntegerValue,
                    context.Input(DistanceParameter).IntegerValue,
                    RosterUnitKind.Minion,
                    (SourceTeamRelation)context.Input(RelationParameter).IntegerValue,
                    RuleActivity.Swap);

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count < 2
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 2, 2));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var facts = Publish(context, selected);
                return facts == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            /// <summary>
            /// What answering this pair publishes, or nothing where the answer is not one this
            /// selection would take. Lifted out of <see cref="Resume"/> unchanged at milestone 8.35l so
            /// the facts the consuming Swap is asked about before the offer and the facts it receives
            /// after the answer are one derivation.
            /// </summary>
            private IReadOnlyList<EffectOutcomeFact> Publish(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                if (chosen.Length != 2 || chosen.Distinct().Count() != 2 ||
                    chosen.Any(value => !value.IsDefined || !candidates.Contains(value)))
                    return null;
                var canonical = Canonical(chosen);
                var facts = new List<EffectOutcomeFact>
                {
                    AsOutcome(canonical[0], "firstHero", "firstMinion"),
                    AsOutcome(canonical[1], "secondHero", "secondMinion")
                };
                AddSpaceIfPublished(context, facts, canonical[0], "firstSpace");
                AddSpaceIfPublished(context, facts, canonical[1], "secondSpace");
                return facts;
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count < 2
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(AsValue).ToArray();

            /// <summary>Naming the pair mutates nothing; the Swap after it does. Milestone 8.35l.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>
            /// Two, always: the card says "swap two minions" and p.19's Swap moves both, so an answer
            /// naming one is not a smaller version of this question but no answer at all.
            /// </summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 2;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Publish(context, chosen) ?? Array.Empty<EffectOutcomeFact>();
        }

        private sealed class BehindTargetLineHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();
            public EffectCapabilityReference Reference => BehindTargetLineV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("length", EffectValueKind.Integer) },
                    new[]
                    {
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(NamedAHeroOutput, EffectValueKind.Boolean)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<HeroId> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var targetAt = context.EnclosingAttack.TargetSpaceBeforeConsequences;
                var length = context.Input("length").IntegerValue;
                if (board == null || context.Participants == null || !targetAt.HasValue || length < 1 ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt) ||
                    !StraightLineQuery.TryGetDirection(
                        actorAt, targetAt.Value, out var direction, out _))
                    return Array.Empty<HeroId>();

                var result = new List<HeroId>();
                for (var distance = 1; distance <= length; distance++)
                {
                    var at = StraightLineQuery.Step(targetAt.Value, direction, distance);
                    if (!board.TryGetObjectAt(at, out var target) ||
                        target.Kind != BoardObjectKind.Hero ||
                        !TargetQuery.StandsInRelationTo(
                            SourceTeamRelation.OpposingSource, target, context.ActorId,
                            context.Participants, board) ||
                        TargetQuery.IsPrevented(
                            target.AsRuleObject(), context.EnclosingActivity,
                            RuleObject.ForHero(context.ActorId), board, context.Participants,
                            context.Modifications, context.ImmunityOverrides))
                        continue;
                    result.Add(target.HeroId);
                }

                return result;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.Applied(NamedNobody)
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Hero), 0, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count == 0)
                {
                    return EffectExecutionResult.Applied(NamedNobody);
                }

                return selected.Count == 1 && selected[0].Kind == EffectValueKind.Hero &&
                    Candidates(context).Contains(selected[0].HeroId)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(ChosenHeroOutput, selected[0]),
                        new EffectOutcomeFact(NamedAHeroOutput, EffectValue.Boolean(true))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            /// <summary>
            /// The named absence, published rather than left out. Milestone 8.20: "Up to 1 enemy hero
            /// ... discards a card, or is defeated" is a step that may reach nobody, and what follows
            /// it has to be able to tell "nobody was named" from "the question has not been asked".
            /// An outcome nothing published resolves to an undefined value at the consumer, which is
            /// the same thing both cases would look like.
            /// </summary>
            private static readonly EffectOutcomeFact[] NamedNobody =
            {
                new EffectOutcomeFact(NamedAHeroOutput, EffectValue.Boolean(false))
            };

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
        }

        /// <summary>
        /// Where a charge may end: the straight files at the printed distances, keeping only the
        /// destinations that are actually reachable and that have at least one enemy unit beside
        /// them.
        ///
        /// <para><b>Milestone 8.35p asks one question here, not two.</b> Until then this node asked
        /// for the destination and then, inside the same node, for the unit to attack - so both
        /// answers were given before the Move node ran and the player named a victim while the hero
        /// authoritatively still stood on his starting space. The user's rule is the printed order:
        /// "they move as soon as they select, and then they attack". So the destination answer is
        /// this node's whole result, the Move commits on it, and the unit is named afterwards by an
        /// ordinary <c>choose-target@2</c> measured from where the hero now stands.</para>
        ///
        /// <para><b>The feasibility filter stays here, and it is what makes the reorder safe.</b> A
        /// destination is offered only when <see cref="Options"/> finds a legal target beside it, so
        /// no answer can strand the hero on a space with nobody to attack; and because a direct Move
        /// of the actor removes no unit and moves no other, the set beside the destination before the
        /// Move is the same set beside the actor after it. That equality is asserted directly rather
        /// than argued - see
        /// <c>RosterTargetGeometryCapabilityTests.ChargeLine_OffersOnlyDestinationsWhoseTargetSetSurvivesTheMove</c>.</para>
        ///
        /// <para><b>Milestone 8.39e: this node names no unit, and that is the settled reading of
        /// "that unit" and "that hero".</b> The offer is a set of destinations - <see cref="Options"/>
        /// keeps the qualifying units only long enough to decide whether a destination is offered at
        /// all, and <see cref="Destinations"/> then collapses them away. So on a destination adjacent
        /// to two enemy heroes there is no "the hero the destination was chosen against" to read: the
        /// subject is named afterwards by the ordinary radius-1 <c>choose-target@2</c> that Brogan's
        /// "then target that unit" already uses, which offers both and lets the mover choose. That is
        /// what the printed text leaves to the player, and it is the only answer available without
        /// putting a second question back inside this node - which is exactly what the 8.35p split
        /// took out, on the user's rule that they move as soon as they select and only then
        /// attack.</para>
        ///
        /// <para><b>Obstacles are the declaring card's business</b> - see
        /// <see cref="IgnoresObstaclesParameter"/>, optional here since milestone 8.39e and unbound by
        /// every declaration written before it.</para>
        /// </summary>
        private sealed class ChargeLineHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();
            public EffectCapabilityReference Reference => ChooseChargeLineV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("minimumDistance", EffectValueKind.Integer),
                        new EffectCapabilityParameter("maximumDistance", EffectValueKind.Integer),
                        new EffectCapabilityParameter(
                            UnitKindParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(RosterUnitKind.None)),

                        // Milestone 8.39e. Cutter's three Green charges print "Move 3 spaces in a
                        // straight line, IGNORING OBSTACLES, to a space adjacent to an enemy hero".
                        // Optional and unbound by default, so Brogan's and Ursafar's declarations
                        // keep the exact candidate set they had before this milestone - the same
                        // discipline `choose-line-destination@1` has carried since Misa.
                        new EffectCapabilityParameter(
                            IgnoresObstaclesParameter, EffectValueKind.Boolean, isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                Ask(context);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate ||
                    !Destinations(context).Any(value => value.Equals(selected[0].Coordinate)))
                    return EffectExecutionResult.ImpossibleMandatory();

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("destination", EffectValue.Space(selected[0].Coordinate))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Options(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static EffectExecutionResult Ask(EffectCapabilityContext context)
            {
                var destinations = Destinations(context);
                return destinations.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, destinations.Select(EffectValue.Space), 1, 1));
            }

            /// <summary>
            /// The offered spaces. Derived on every ask - the offer, the answer and a restored pause -
            /// for the reason every other selection here re-derives: the board is state, and a set
            /// remembered from the moment the question opened could name a space something has since
            /// moved into.
            /// </summary>
            private static IReadOnlyList<HexCoordinate> Destinations(
                EffectCapabilityContext context) =>
                Options(context).Select(value => value.Destination).Distinct().ToArray();

            private static IReadOnlyList<ChargeOption> Options(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var minimum = context.Input("minimumDistance").IntegerValue;
                var maximum = context.Input("maximumDistance").IntegerValue;
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                if (board == null || context.Participants == null || minimum < 1 || maximum < minimum ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    return Array.Empty<ChargeOption>();

                var actor = BoardObjectReference.ForHero(context.ActorId);
                var result = new List<ChargeOption>();

                // Obstacles are the declaring card's business, exactly as they are on
                // `choose-line-destination@1`. Brogan's three charges and Ursafar's Gold print no
                // crossing, so an unbound parameter walks p.18's obstacles the way every Move did
                // before milestone 8.23; Cutter's three Greens print "ignoring obstacles" and bind
                // it. The permission reaches the transit predicate and nothing else, so p.18's
                // second sentence still forbids stopping on one - which is why the adjacency this
                // node measures is asked of a destination the mover could legally occupy either
                // way.
                var crosses = context.Inputs.ContainsKey(IgnoresObstaclesParameter) &&
                    context.Input(IgnoresObstaclesParameter).BooleanValue;
                foreach (var route in board.FindDirectMoveDestinations(
                    actor, maximum, context.Modifications, moveIgnoresObstacles: crosses))
                {
                    if (route.Cost < minimum || route.Cost > maximum ||
                        !StraightLineQuery.TryGetDirection(
                            actorAt, route.Destination, out var direction, out var distance) ||
                        distance != route.Cost || !IsStraightRoute(actorAt, direction, route.Route))
                        continue;

                    var targets = RosterPresenceQuery.FindUnitsWithin(
                            route.Destination, 1, kind, SourceTeamRelation.OpposingSource,
                            context.ActorId, board, context.Participants, context.Modifications)
                        .Where(target => !TargetQuery.IsPrevented(
                            target.AsRuleObject(), context.EnclosingActivity,
                            actor.AsRuleObject(), board, context.Participants,
                            context.Modifications, context.ImmunityOverrides))
                        .ToArray();
                    if (targets.Length > 0)
                        result.Add(new ChargeOption(route.Destination, targets));
                }

                return result.OrderBy(value => value.Destination.Q)
                    .ThenBy(value => value.Destination.R).ToArray();
            }
        }

        private sealed class ThroughTargetLineHandler : TargetingHandler, IEffectCapabilityHandler
        {
            private static readonly HexDirection[] Directions =
            {
                HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
            };

            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();

            public EffectCapabilityReference Reference => ChooseThroughTargetLineV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            UnitKindParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(RosterUnitKind.None))
                    },
                    new[]
                    {
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate),
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var options = Options(context);
                return options.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, options.Select(value => AsValue(value.Target)), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var target = selected?.Count == 1 ? FromValue(selected[0]) : default;
                var option = target.IsDefined
                    ? Options(context).SingleOrDefault(value => value.Target.Equals(target))
                    : null;
                if (option == null) return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("destination", EffectValue.Space(option.Destination)),
                    AsOutcome(option.Target)
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Options(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static IReadOnlyList<ThroughOption> Options(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                if (board == null || context.Participants == null ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    return Array.Empty<ThroughOption>();

                var actor = BoardObjectReference.ForHero(context.ActorId);
                var options = new List<ThroughOption>();
                foreach (var direction in Directions)
                {
                    var crossedAt = StraightLineQuery.Step(actorAt, direction, 1);
                    var destination = StraightLineQuery.Step(actorAt, direction, 2);
                    if (!board.TryGetObjectAt(crossedAt, out var target) ||
                        !Admits(target, kind, context.Modifications) ||
                        !TargetQuery.StandsInRelationTo(
                            SourceTeamRelation.OpposingSource, target, context.ActorId,
                            context.Participants, board) ||
                        TargetQuery.IsPrevented(
                            target.AsRuleObject(), context.EnclosingActivity,
                            actor.AsRuleObject(), board, context.Participants,
                            context.Modifications, context.ImmunityOverrides) ||
                        !board.MapDefinition.PlayableCoordinates.Contains(destination) ||
                        board.TryGetObjectAt(destination, out _) ||
                        context.Modifications.AnyOverlayMakesAnObstacleOf(destination))
                        continue;
                    options.Add(new ThroughOption(target, destination));
                }

                return options.OrderBy(value => value.Target.Kind)
                    .ThenBy(value => RosterPresenceQuery.Identity(value.Target), StringComparer.Ordinal)
                    .ToArray();
            }
        }

        private sealed class FormerTargetSpaceHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>
            /// Milestone 9.8c: it reads the Attack's facts and changes nothing, so the offer walk can
            /// carry the attacked space to the step that reads it - Knife of the Living Dead's "a
            /// Zombie token adjacent to the target", which is unavailable with none there.
            /// </summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => FormerTargetSpaceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                context.EnclosingAttack.TargetSpaceBeforeConsequences.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("space", EffectValue.Space(
                            context.EnclosingAttack.TargetSpaceBeforeConsequences.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.EnclosingAttack.TargetSpaceBeforeConsequences.HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class PlayedAttackThisTurnHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PlayedAttackThisTurnV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("hero", EffectValueKind.Hero) },
                    new[] { new EffectOutputBinding("played", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static bool CanRead(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable && context.CardState.IsAvailable &&
                context.Input("hero").HeroId.IsDefined;
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                !CanRead(context)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("played", EffectValue.Boolean(
                            PlayedAttackCardThisTurnQuery.HasPlayed(
                                context.Input("hero").HeroId,
                                context.RoundIndex,
                                context.TurnIndex,
                                context.RosterState.TurnSlots,
                                context.CardState.Definitions,
                                context.CardState.Instances)))
                    });
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanRead(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class HeroDefeatedThisRoundHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>Milestone 9.6: a predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>Milestone 9.6: what the predicate would publish, so the offer reads its branch.</summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => HeroDefeatedThisRoundV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("defeated", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                !context.RosterState.IsAvailable
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("defeated", EffectValue.Boolean(
                            HeroDefeatedThisRoundQuery.IsSatisfied(context.RosterState.HeroDefeats)))
                    });
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Milestone 8.23. Whether the acting hero has left the space this Attack started from.
        ///
        /// <para>Two coordinates and nothing else: the one the Attack recorded and the one the board
        /// reports now. It is a reading rather than a step, so it asks nobody anything and applies
        /// immediately - and an Attack that recorded no starting space, which is every Attack whose
        /// card never asks, answers false.</para>
        /// </summary>
        private sealed class ActorMovedThisActionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ActorMovedThisActionV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding("moved", EffectValueKind.Boolean),

                        // The card prints the negative - "If you did NOT move before the attack" -
                        // and a boolean gate reads the fact it is handed rather than inverting one.
                        // Both views of one answer are published for the reason `presence-count`
                        // publishes a count and a `present` beside it.
                        new EffectOutputBinding("didNotMove", EffectValueKind.Boolean)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var moved = HasMoved(context);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("moved", EffectValue.Boolean(moved)),
                    new EffectOutcomeFact("didNotMove", EffectValue.Boolean(!moved))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;

            private static bool HasMoved(EffectCapabilityContext context)
            {
                var started = context.EnclosingAttack.ActorSpaceAtActionStart;
                if (!started.HasValue || context.Components?.Board == null) return false;
                return context.Components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var now)
                    && !now.Equals(started.Value);
            }
        }

        private sealed class PresenceCountHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>Milestone 9.6: a predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>Milestone 9.6: what the predicate would publish, so the offer reads its branch.</summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => PresenceCountV1;
            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                new[]
                {
                    new EffectCapabilityParameter("excludeActor", EffectValueKind.Boolean),
                    new EffectCapabilityParameter("excludeTarget", EffectValueKind.Boolean),

                    // Milestone 8.22. How many the card asks for - Sabina's Steady Advance and
                    // Unwavering Resolve, "If there are two or more friendly minions in radius".
                    //
                    // It belongs on the declaration rather than in a comparison a definition writes,
                    // because the threshold is the card's own number and `present` is the answer the
                    // sentence asks for. A definition reading `count` and comparing it would need a
                    // second capability that compares integers, and every card that printed a
                    // different number would declare the comparison again.
                    //
                    // Optional and defaulting to one, which is what `count > 0` already meant, so
                    // every declaration written before this milestone keeps its exact answer.
                    new EffectCapabilityParameter(
                        MinimumPresentParameter, EffectValueKind.Integer, isRequired: false),

                    // Milestone 9.8c. Mortimer's Braains faces, "If an enemy hero in radius is
                    // ADJACENT TO A ZOMBIE TOKEN": only units standing beside a token of the named
                    // kind count. The printed-icon selections' own clause, by the same name and read
                    // the same way; unbound, every unit counts as before.
                    new EffectCapabilityParameter(
                        SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName,
                        EffectValueKind.TokenKind, isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding("count", EffectValueKind.Integer),
                    new EffectOutputBinding("present", EffectValueKind.Boolean)
                });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static int Count(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var distance = TargetingHandler.DeclaredReach(context);
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                var relation = (SourceTeamRelation)context.Input(RelationParameter).IntegerValue;
                if (board == null || context.Participants == null || distance < 0 ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    !TryOrigin(
                        context, (RosterQueryOrigin)context.Input(OriginParameter).IntegerValue,
                        out var at))
                    return -1;
                var excludedActor = context.Input("excludeActor").BooleanValue
                    ? BoardObjectReference.ForHero(context.ActorId)
                    : default;
                var excludedTarget = context.Input("excludeTarget").BooleanValue
                    ? EnclosingTarget(context)
                    : default;
                var units = RosterPresenceQuery.FindUnitsWithin(
                    at,
                    distance,
                    kind,
                    relation,
                    context.ActorId,
                    board,
                    context.Participants,
                    context.Modifications,
                    excludedActor,
                    excludedTarget);
                var tokenKind = context.Input(SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName);
                return tokenKind.Kind != EffectValueKind.TokenKind
                    ? units.Count
                    : units.Count(unit => board.TryGetObjectCoordinate(unit, out var unitAt) &&
                        context.Components?.Tokens != null &&
                        context.Components.Tokens.PlacedTokens.Any(token =>
                            HexDistanceQuery.Calculate(unitAt, token.Coordinate) == 1 &&
                            SyntheticTargetingCapabilities.PrintedIconSpaceClauses.HoldsTokenOfKind(
                                context, token.Coordinate, tokenKind.TokenKindId)));
            }

            /// <summary>
            /// How many the card asked for, floored at one: an undeclared threshold is the old
            /// reading, and a declared zero would make `present` a constant rather than a question.
            /// </summary>
            private static int Minimum(EffectCapabilityContext context)
            {
                var declared = context.Input(MinimumPresentParameter);
                return declared.Kind == EffectValueKind.Integer && declared.IntegerValue > 1
                    ? declared.IntegerValue
                    : 1;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var count = Count(context);
                return count < 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("count", EffectValue.Integer(count)),
                        new EffectOutcomeFact(
                            "present", EffectValue.Boolean(count >= Minimum(context)))
                    });
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Count(context) < 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// Milestone 8.35z28. The empty-minion-Spawn-point count behind both declarations above.
        ///
        /// <para>One handler and two declarations, exactly as the printed-icon selections are one
        /// handler and six: the question, the emptiness, the Battle-Zone membership and the minion-only
        /// ruling are identical, and the only thing that differs is where the reach comes from - p.9's
        /// printed Radius icon, which p.16's matching item raises, or p.19's innermost ring, which the
        /// sentence writes down and no item touches. Two ids keep those two apart, so a card printing
        /// no Radius cannot read a Radius item.</para>
        ///
        /// <para><b>An absent board is a count of nothing, not a failure.</b> The derivation returns an
        /// empty set for a board with no current Battle Zone, and this reports zero rather than
        /// <see cref="EffectExecutionResultKind.ImpossibleMandatory"/> - a condition that is simply not
        /// satisfied is what "If there are 2 or more" means when there are not, and the user's RULING 3
        /// says such a clause skips and the card continues. Only a genuinely unanswerable question - no
        /// board port at all, or an actor standing nowhere - is impossible.</para>
        /// </summary>
        private sealed class EmptyMinionSpawnPointCountHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly bool readsPrintedRadius;

            public EmptyMinionSpawnPointCountHandler(
                EffectCapabilityReference reference, bool readsPrintedRadius)
            {
                Reference = reference;
                this.readsPrintedRadius = readsPrintedRadius;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        // "2 or more", stated as the bound the card prints rather than a comparison a
                        // definition writes - the same shape and the same parameter name
                        // `presence-count@1` has carried since milestone 8.22, so a reader who knows
                        // one knows both. Optional and defaulting to one, which is what a bare
                        // "adjacent to an empty spawn point" asks.
                        new EffectCapabilityParameter(
                            MinimumPresentParameter, EffectValueKind.Integer, isRequired: false),

                        // Milestone 8.35z33's half of the shared declaration: the ceiling Sanguine
                        // Path and Blood Pilgrimage print - "up to a maximum of 2 / 3 spaces" - which
                        // Dodger's own faces do not print and therefore do not set. Optional, so
                        // every node written against this contract before the wave is unchanged
                        // without naming it.
                        new EffectCapabilityParameter(
                            MaximumCountedParameter, EffectValueKind.Integer, isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding(CountOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>The count, or a negative number where the question cannot be asked at all.</summary>
            private int Count(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components?.Board == null || components.Tokens == null ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return -1;
                }

                // Milestone 8.35z28: Dodger's Ultimate, if she has it up. The user's D-02 ruling
                // puts it over any action she performs and D-03 makes its granted points satisfy
                // "empty" wherever the space is, so the switch belongs on the count itself rather than
                // on each card that reads it.
                var overlay = SpawnPointOverlay.ForActor(
                    context.Modifications, context.Participants, context.ActorId);

                if (!readsPrintedRadius)
                {
                    // p.19 measures adjacency as one space, and p.16 never raises a number a sentence
                    // writes down.
                    return EmptyMinionSpawnPointQuery.CountWithin(
                        components.Board, components.Tokens, actorAt, 1, overlay);
                }

                if (!context.CardState.IsAvailable)
                {
                    return -1;
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications);
                if (radius.Total <= 0)
                {
                    // A card printing no Radius icon has no disk, and p.16 does not invent one. The
                    // count is zero rather than adjacent-by-default, which is the same reading the
                    // printed-icon selections take of the same absence.
                    return 0;
                }

                return EmptyMinionSpawnPointQuery.CountWithin(
                    components.Board, components.Tokens, actorAt, radius.Total, overlay);
            }

            /// <summary>
            /// How many the card asked for, floored at one: an undeclared threshold is "at least one",
            /// which is what the Gold basics' bare adjacency clause means, and a declared zero would
            /// make the answer a constant rather than a question.
            /// </summary>
            private static int Minimum(EffectCapabilityContext context)
            {
                var declared = context.Input(MinimumPresentParameter);
                return declared.Kind == EffectValueKind.Integer && declared.IntegerValue > 1
                    ? declared.IntegerValue
                    : 1;
            }

            /// <summary>
            /// The count under the ceiling the sentence prints, or the count where it prints none.
            /// Milestone 8.35z33.
            /// </summary>
            private static int Capped(EffectCapabilityContext context, int count)
            {
                var declared = context.Input(MaximumCountedParameter);
                return declared.Kind == EffectValueKind.Integer && declared.IntegerValue >= 0
                    ? Math.Min(count, declared.IntegerValue)
                    : count;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var count = Count(context);
                return count < 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(CountOutput, EffectValue.Integer(Capped(context, count))),

                        // The presence question is asked of the board's own count and never of the
                        // capped one: "if there is an empty minion spawn point" is about the board,
                        // and a printed ceiling on how far a hero may walk says nothing about what is
                        // standing there. No face prints both, but the two clauses are different
                        // questions and reading one through the other would be a coincidence rather
                        // than a rule.
                        new EffectOutcomeFact(
                            PresentOutput, EffectValue.Boolean(count >= Minimum(context)))
                    });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Count(context) < 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;

            // This query only reads the board. Carry its count into a following move's
            // feasibility check, so a computed positive budget is not mistaken for zero.
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var count = Count(context);
                return count < 0 ? Array.Empty<EffectOutcomeFact>() : new[]
                {
                    new EffectOutcomeFact(CountOutput, EffectValue.Integer(Capped(context, count))),
                    new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(count >= Minimum(context)))
                };
            }
        }

        private sealed class PreConsequenceAdjacencyHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PreConsequenceAdjacencyV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("wasAdjacent", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static bool? Read(EffectCapabilityContext context)
            {
                var targetAt = context.EnclosingAttack.TargetSpaceBeforeConsequences;
                return targetAt.HasValue && context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt)
                    ? (bool?)(HexDistanceQuery.Calculate(actorAt, targetAt.Value) == 1)
                    : null;
            }
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("wasAdjacent", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Milestone 8.22. Point Blank Shot's present-tense adjacency, read off live occupancy.
        ///
        /// <para>The target is whichever unit the enclosing Attack bound - p.13 admits a hero or a
        /// minion - and the answer is false when the board no longer holds it. That is not a failure
        /// case handled specially: <c>TryGetObjectCoordinate</c> simply does not find a removed piece,
        /// and "the target is adjacent to you" is exactly as false for a piece that is gone as for one
        /// standing three spaces away.</para>
        ///
        /// <para>An Attack with no target at all is a different thing from a target that is not
        /// adjacent, so it reports itself impossible rather than answering false. Card text applied
        /// inside p.14 always has one by step 7, and a definition that reached here without one is
        /// asking about something that did not happen.</para>
        /// </summary>
        private sealed class TargetIsAdjacentHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => TargetIsAdjacentV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("adjacent", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var attack = context.EnclosingAttack;
                var board = context.Components?.Board;
                if (board == null || !attack.IsDefined || !attack.HasTarget)
                {
                    return null;
                }

                var target = attack.HeroTargetId.IsDefined
                    ? BoardObjectReference.ForHero(attack.HeroTargetId)
                    : BoardObjectReference.ForMinion(attack.MinionTargetId);

                // A removed target has no coordinate, which is the answer rather than an error: the
                // sentence asks where it is standing and it is not standing anywhere.
                if (!board.TryGetObjectCoordinate(target, out var targetAt) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return false;
                }

                return HexDistanceQuery.Calculate(actorAt, targetAt) == 1;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("adjacent", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Milestone 8.20. Whether two named spaces are rulebook-adjacent, published as a boolean a
        /// conditional can read. Nothing about the board is consulted: p.19's adjacency is a property
        /// of two coordinates, and both arrive bound from earlier nodes.
        /// </summary>
        private sealed class SpacesAdjacentHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>Milestone 9.6: a predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>Milestone 9.6: what the predicate would publish, so the offer reads its branch.</summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => SpacesAdjacentV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("first", EffectValueKind.Coordinate),
                        new EffectCapabilityParameter("second", EffectValueKind.Coordinate)
                    },
                    new[] { new EffectOutputBinding("adjacent", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var first = context.Input("first");
                var second = context.Input("second");
                return first.Kind == EffectValueKind.Coordinate &&
                    second.Kind == EffectValueKind.Coordinate
                    ? (bool?)(HexDistanceQuery.Calculate(first.Coordinate, second.Coordinate) == 1)
                    : null;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("adjacent", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class AdjacentTerrainPresenceHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => AdjacentTerrainPresenceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("present", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            // Milestone 8.19c routed this through the one terrain question, so an effective-type
            // overlay reaches it: a hero petrified into terrain is terrain to stand beside, exactly as
            // the printed rock is. Reading the map alone was correct while the map was the only source
            // and would silently have been half an answer once a card made a second one.
            private static bool? Read(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || !board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(context.ActorId), out var actorAt)) return null;
                return TerrainPresenceQuery.IsAdjacentToTerrain(board, actorAt, context.Modifications);
            }
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("present", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// Asking whether the actor stands beside terrain changes nothing, and answering "yes" is
            /// not the card doing anything. Milestone 8.35l: Blend Into Shadows opens on this
            /// question, and while it counted as the card applying something, the Place it gates was
            /// never judged - so a barred Place was offered, drew no prompt, and spent the card.
            /// </summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var value = Read(context);
                return value.HasValue
                    ? new[] { new EffectOutcomeFact("present", EffectValue.Boolean(value.Value)) }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }

        /// <summary>
        /// Milestone 8.38d. Whether the bound SUBJECT stands beside terrain - the qualifier Swift's
        /// three Green faces put on the enemy hero they reach.
        ///
        /// <para>It shares <c>TerrainPresenceQuery</c> with the actor-scoped handler above, so the two
        /// cannot disagree about what terrain is, and differ only in whose coordinate they read. A
        /// subject that is not on the board answers nothing rather than false: a hero who has left the
        /// board is not a candidate at all, and reporting "not adjacent to terrain" about them would
        /// make an absent hero the most eligible one.</para>
        /// </summary>
        private sealed class SubjectAdjacentTerrainPresenceHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => SubjectAdjacentTerrainPresenceV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(TerrainSubjectParameter, EffectValueKind.Hero),
                        // Milestone 8.38e. All three of Swift's Green faces print the NEGATIVE of this
                        // question - "an enemy hero not adjacent to terrain" - and the predicate is
                        // where a negation belongs in this build: `synthetic-is-flagged@1` declares no
                        // `negate` and is the wrong place for one, because a gate that could invert
                        // its own input would let two readings of one predicate exist. Optional, so
                        // the showcase card that asks the plain question binds nothing.
                        new EffectCapabilityParameter(
                            SubjectTerrainNegateParameter, EffectValueKind.Boolean,
                            isRequired: false)
                    },
                    new[] { new EffectOutputBinding("present", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var subject = context.Input(TerrainSubjectParameter);
                if (board == null || subject.Kind != EffectValueKind.Hero ||
                    !subject.HeroId.IsDefined ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(subject.HeroId), out var subjectAt))
                {
                    return null;
                }

                var present = TerrainPresenceQuery.IsAdjacentToTerrain(
                    board, subjectAt, context.Modifications);
                var negate = context.Input(SubjectTerrainNegateParameter);
                return negate.Kind == EffectValueKind.Boolean && negate.BooleanValue
                    ? !present
                    : present;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("present", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// Asking about a hero's footing changes nothing, exactly as the actor-scoped twin records:
            /// publishing an answer is not the card applying something, and milestone 8.35l's defect -
            /// a barred step offered because its gate counted as application - is the reason both say
            /// so.
            /// </summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var value = Read(context);
                return value.HasValue
                    ? new[] { new EffectOutcomeFact("present", EffectValue.Boolean(value.Value)) }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }

        private sealed class RetrieveCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RetrieveCardV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("owner", EffectValueKind.Hero),
                        new EffectCapabilityParameter("chooser", EffectValueKind.Hero),
                        new EffectCapabilityParameter(
                            "kind",
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(
                                RosterCardRetrievalKind.None)),
                        new EffectCapabilityParameter(
                            RestrictToCardParameter, EffectValueKind.CardInstance, isRequired: false),

                        // Milestone 8.69e. Brynn's "a discarded ATTACK card" - see
                        // AttackCardsOnlyParameter. Optional, so every retrieval composed before him
                        // keeps its exact candidate set without naming it.
                        new EffectCapabilityParameter(
                            AttackCardsOnlyParameter, EffectValueKind.Boolean, isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding("retrieved", EffectValueKind.CardInstance),
                        // Milestone 8.35z30. Who retrieved, published only when a card
                        // actually was. Silverarrow's Fae Healing and Nature's Blessing print "A hero
                        // in radius may retrieve a discarded card; <b>if they do</b>, that hero gains
                        // N coin(s)", and this is the whole of that condition: the coin node binds its
                        // recipient here, so a hero who declined leaves the binding undefined and the
                        // grant reports itself impossible - which its own `if able` policy turns into
                        // a step not taken. Nothing separate asks "did it happen".
                        new EffectOutputBinding(RetrievedByOutputName, EffectValueKind.Hero),

                        // Milestone 8.40i. The same fact as a Boolean, for a card whose "if they do"
                        // gates a step about somebody else - see DidRetrieveOutputName.
                        new EffectOutputBinding(DidRetrieveOutputName, EffectValueKind.Boolean)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                var owner = context.Input("owner").HeroId;
                var kind = (RosterCardRetrievalKind)context.Input("kind").IntegerValue;
                if (!context.CardState.IsAvailable || !owner.IsDefined ||
                    kind == RosterCardRetrievalKind.None ||
                    !Enum.IsDefined(typeof(RosterCardRetrievalKind), kind))
                    return Array.Empty<CardInstance>();
                var definitions = context.CardState.Definitions;
                var attackCardsOnly = context.Input(AttackCardsOnlyParameter).BooleanValue;
                var eligible = context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner) &&
                        Eligible(card, kind, definitions, attackCardsOnly));

                // Milestone 8.37. Guardian Spirit and Warrior Spirit name a specific
                // card - themselves - rather than asking which one; see RestrictToCardParameter.
                var restrict = context.Input(RestrictToCardParameter);
                if (restrict.Kind == EffectValueKind.CardInstance)
                {
                    eligible = eligible.Where(card => card.Id.Equals(restrict.CardInstanceId));
                }

                return eligible.OrderBy(card => card.Id.Value, StringComparer.Ordinal).ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                var chooser = context.Input("chooser").HeroId;
                if (candidates.Count == 0 || !IsKnownHero(context, chooser))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.37, milestone 6.5a's reading of "yourself" applied to
                // retrieval: a restriction that has already named the one legal answer has nothing to
                // ask, so it retrieves directly rather than opening a one-candidate prompt.
                if (context.Input(RestrictToCardParameter).Kind == EffectValueKind.CardInstance)
                {
                    return Resume(context, new[] { EffectValue.Card(candidates[0].Id) });
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.None, candidates.Select(card => EffectValue.Card(card.Id)),
                    1, 1, chooser));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var card = selected?.Count == 1 && selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(value => value.Id.Equals(
                        selected[0].CardInstanceId))
                    : null;
                // Milestone 8.69b. The card being resolved is not a card at rest, so it is moved by
                // the transition that names that state rather than by p.8's end-of-round one - which
                // milestone 6.13's audit forbids widening. Which of the two runs is decided by the
                // declared pool and never by trying one and falling back to the other.
                var retrieved = card != null &&
                    ((RosterCardRetrievalKind)context.Input("kind").IntegerValue ==
                        RosterCardRetrievalKind.OwnedResolvingSourceCard
                        ? CardLifecycleAuthority.TryRetrieveResolvingCard(card)
                        : CardLifecycleAuthority.TryRetrieveEligibleCard(card));
                if (!retrieved)
                    return EffectExecutionResult.ImpossibleMandatory();
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact("retrieved", EffectValue.Card(card.Id))
                };
                if (context.Publishes(RetrievedByOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        RetrievedByOutputName, EffectValue.Hero(card.OwnerId)));
                }

                if (context.Publishes(DidRetrieveOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        DidRetrieveOutputName, EffectValue.Boolean(true)));
                }

                return EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ||
                !IsKnownHero(context, context.Input("chooser").HeroId)
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static bool Eligible(
                CardInstance card,
                RosterCardRetrievalKind kind,
                IReadOnlyList<CardDefinition> definitions,
                bool attackCardsOnly)
            {
                // Milestone 8.69e. p.9's noun, asked once and ahead of the place the kind names, so
                // that a face printing "a discarded attack card" narrows whichever pile its kind
                // opens rather than needing a member per pairing. See AttackCardsOnlyParameter.
                if (attackCardsOnly && !PrintsAnAttackPrimary(card, definitions))
                {
                    return false;
                }

                var isRetrievableDiscard = CardLifecycleAuthority.IsDiscardedRestingCard(card);
                if (kind == RosterCardRetrievalKind.OwnedDiscardedCard)
                    return isRetrievableDiscard;

                // Milestone 8.69b. Peak Precision's "this card" - the one card in the owner's pile
                // that is neither at rest nor resolved, because its own action is what is running.
                // The pool is stated rather than added to either of the two above: a face printing
                // "a discarded card" must not pick this card up, and this face must not pick up a
                // discarded one.
                if (kind == RosterCardRetrievalKind.OwnedResolvingSourceCard)
                    return CardLifecycleAuthority.IsResolvingCardInTurnArea(card);

                var definition = definitions.SingleOrDefault(value =>
                    value.Reference.Equals(card.DefinitionReference));
                var primary = definition?.ActionDefinitions.SingleOrDefault(value =>
                    value.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (definition == null || !definition.Progression.IsBasic ||
                    primary?.Kind != ActionKind.Skill) return false;
                return isRetrievableDiscard ||
                    (card.State.Location == CardLocation.TurnArea && card.State.HasBeenPlayed &&
                     card.State.ResolutionStatus == CardResolutionStatus.Resolved);
            }

            /// <summary>
            /// p.9's "Attack card": a card whose PRINTED PRIMARY action is an Attack. The Ranged
            /// subtype is still an Attack (p.13 classifies the reach, not the action), and a card this
            /// composition holds no definition for is refused rather than admitted - the same answer
            /// every other reader of an unknown definition gives.
            /// </summary>
            private static bool PrintsAnAttackPrimary(
                CardInstance card, IReadOnlyList<CardDefinition> definitions)
            {
                var definition = definitions.SingleOrDefault(value =>
                    value.Reference.Equals(card.DefinitionReference));
                var primary = definition?.ActionDefinitions.SingleOrDefault(value =>
                    value.Origin == ActionDefinitionOrigin.PrintedPrimary);
                return primary?.Kind == ActionKind.Attack;
            }
        }

        /// <summary>
        /// `discard-owned-card@1`. The hero its input names spends one card of their own choosing out
        /// of their hand, and publishes whether they spent it.
        ///
        /// <para>Everything about which cards are eligible, how the offer is built and how the answer
        /// is committed is <see cref="ColorRestrictedDiscard"/>'s, reached through the same criteria
        /// milestone 7.1a's discard uses. Two capabilities that read a hand differently would be two
        /// answers to one question, and the price a card charges is not the place to have two.</para>
        ///
        /// <para>A hero holding nothing applies rather than declining, publishing <c>didDiscard</c>
        /// false. That is the difference from <c>synthetic-discard-chosen-card@1</c>'s `if able`
        /// reading and it is the whole reason this exists: the sentence after the semicolon has to be
        /// told, and a decline publishes nothing to tell it with.</para>
        /// </summary>
        private sealed class DiscardOwnedCardHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public EffectCapabilityReference Reference => DiscardOwnedCardV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero) },
                    new[]
                    {
                        new EffectOutputBinding(DiscardedOutput, EffectValueKind.CardInstance),
                        new EffectOutputBinding(DidDiscardOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                var offer = ColorRestrictedDiscard.Offer(
                    HandCriteria(chooser),
                    EffectChooserRole.None,
                    context.CardState.Definitions,
                    context.CardState.Instances,
                    chooser,
                    SyntheticCardStateCapabilities.DiscardSubstitutes(context, chooser));

                return offer != null
                    ? EffectExecutionResult.NeedsChoice(offer)
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(DidDiscardOutput, EffectValue.Boolean(false))
                    });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.CardInstance)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived rather than trusted, exactly as its sibling re-derives: a card played or
                // discarded between the offer and the answer is refused instead of spent twice.
                var result = ColorRestrictedDiscard.Resolve(
                    HandCriteria(Chooser(context)),
                    selected[0].CardInstanceId,
                    context.CardState.Definitions,
                    context.CardState.Instances,
                    SyntheticCardStateCapabilities.DiscardSubstitutes(context, Chooser(context)));

                // Milestone 8.112c: p.17 cancels a substitute's This round grant once it is spent.
                if (result.Accepted && result.DiscardedASubstitute)
                {
                    context.Timing?.CancelForSourceCardStateChange(
                        result.DiscardedCardId, context.RoundIndex, context.TurnIndex);
                }

                return result.Accepted
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(
                            DiscardedOutput, EffectValue.Card(result.DiscardedCardId)),
                        new EffectOutcomeFact(DidDiscardOutput, EffectValue.Boolean(true))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateCandidates(context).Count == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                if (!chooser.IsDefined)
                {
                    return Array.Empty<EffectValue>();
                }

                var hand = CardCharacteristicQuery.FindCandidates(
                    HandCriteria(chooser),
                    context.CardState.Definitions,
                    context.CardState.Instances);
                return hand
                    .Concat(SyntheticCardStateCapabilities.DiscardSubstitutes(context, chooser)
                        .Where(card => !hand.Contains(card)))
                    .Select(EffectValue.Card)
                    .ToArray();
            }

            /// <summary>One card, which is what the discard offer requests. Milestone 8.35l.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            private static HeroId Chooser(EffectCapabilityContext context)
            {
                var named = context.Input(HeroParameter);
                return named.Kind == EffectValueKind.Hero ? named.HeroId : default;
            }
        }

        /// <summary>
        /// `owned-card-present@1`. Whether the hero its input names owns at least one card in the
        /// place its input names - Brogan's Throwing Spear, "If you have a card in the discard".
        ///
        /// <para>A pure predicate: it reads the card state and publishes a boolean, mutating nothing
        /// and asking nobody. The location is a parameter rather than a second capability because the
        /// question is the same one wherever it is asked, and the card is what says where.</para>
        /// </summary>
        private sealed class OwnedCardPresentHandler : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public bool PublishesWithoutApplying => true;
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                CanRead(context) ? Observe(context) : Array.Empty<EffectOutcomeFact>();
            public EffectCapabilityReference Reference => OwnedCardPresentV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero),
                        new EffectCapabilityParameter(
                            LocationParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(CardLocation.None))
                    },
                    new[] { new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool CanRead(EffectCapabilityContext context)
            {
                var location = (CardLocation)context.Input(LocationParameter).IntegerValue;
                return context.CardState.IsAvailable &&
                    context.Input(HeroParameter).HeroId.IsDefined &&
                    location != CardLocation.None && Enum.IsDefined(typeof(CardLocation), location);
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanRead(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(Observe(context));
            }

            private static IReadOnlyList<EffectOutcomeFact> Observe(EffectCapabilityContext context)
            {
                var owner = context.Input(HeroParameter).HeroId;
                var location = (CardLocation)context.Input(LocationParameter).IntegerValue;
                return new[]
                {
                    new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(
                        context.CardState.Instances.Any(card =>
                            card.OwnerId.Equals(owner) && card.State.Location == location)))
                };
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanRead(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// `ultimate-unlocked@1`. Whether the acting hero's Ultimate card sits in the Ultimate area,
        /// which is p.16's whole definition of having one. Milestone 8.35z28.
        /// </summary>
        private sealed class UltimateUnlockedHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>Milestone 9.6: a predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>Milestone 9.6: what the predicate would publish, so the offer reads its branch.</summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => UltimateUnlockedV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool IsUnlocked(EffectCapabilityContext context) =>
                context.CardState.IsAvailable &&
                context.ActorId.IsDefined &&
                GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery
                    .Derive(context.CardState.Definitions, context.CardState.Instances)
                    .IsUnlockedFor(context.ActorId);

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(IsUnlocked(context)))
                });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            // A hero who has not unlocked their Ultimate still asks the question and still gets an
            // answer - the clause is a condition and the user's RULING 3 makes a false one skip, so
            // "no" is an outcome rather than an impossibility.
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// `gain-item-from-the-box@1`. p.16's Gain an Item, granted by card text.
        /// Milestone 8.35z28.
        /// </summary>
        private sealed class GainItemFromTheBoxHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => GainItemFromTheBoxV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("recipient", EffectValueKind.Hero),
                        // p.16's icon, closed to the six the page prints. It is required because the
                        // page conditions the whole item on it, and a grant that named none would be an
                        // item with nothing to raise.
                        new EffectCapabilityParameter(
                            ItemIconParameter,
                            EffectValueKind.Integer,
                            isRequired: true,
                            integerDomain: EffectIntegerDomain.All<PrintedValueChannel>())
                    },
                    new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding(GrantedItemOrderOutput, EffectValueKind.Integer)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// The p.16 icon the card printed, read back out of the closed vocabulary the definition
            /// bound it through - an Integer at runtime, exactly as every other closed vocabulary in
            /// this file is.
            /// </summary>
            private static PrintedValueChannel Icon(EffectCapabilityContext context)
            {
                var declared = context.Input(ItemIconParameter);
                if (declared.Kind != EffectValueKind.Integer ||
                    !Enum.IsDefined(typeof(PrintedValueChannel), declared.IntegerValue))
                {
                    return PrintedValueChannel.None;
                }

                return (PrintedValueChannel)declared.IntegerValue;
            }

            private static bool CanApply(EffectCapabilityContext context) =>
                context.HeroItems != null &&
                context.Input("recipient").HeroId.IsDefined &&
                Icon(context) != PrintedValueChannel.None;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanApply(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var placement = context.HeroItems.TryGrantItemFromTheBox(
                    context.Input("recipient").HeroId, Icon(context));
                if (!placement.IsPlaced)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("recipient", EffectValue.Hero(placement.Item.OwnerId)),
                    new EffectOutcomeFact(
                        GrantedItemOrderOutput, EffectValue.Integer(placement.Item.Order))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanApply(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// `take-deck-card-as-item@1`. p.16's Gain an Item, performed on a Tier the hero picks out of
        /// her own deck - Min the Dragonmonk's Perfect Self. Milestone 8.61h.
        ///
        /// <para><b>The candidate set is the whole of the printed sentence.</b> "A Tier II card from
        /// your deck" is three conditions - the owner, <c>CardLocation.Deck</c> and the Tier - and
        /// <see cref="CardCharacteristicQuery"/> answers all three, which is why nothing here walks
        /// instances itself. A Tier II card RETURNED to the deck by a Tier III upgrade is in the deck,
        /// so it qualifies: p.15 says the upgraded card "returns it to the deck", and the sentence
        /// asks where the card is rather than how it got there (MI-Q5).</para>
        ///
        /// <para><b>Two conditions beyond the sentence, and both are p.16's own.</b> A candidate must
        /// print an Item icon and its owner's dashboard must have a slot that accepts it - the page
        /// conditions the whole item on the match, and milestone 8.40g's law is that the offer must
        /// admit exactly what the answer can perform. Every Tier II and Tier III card in the corpus
        /// prints an icon and every dashboard accepts all six, so today these drop nothing; they are
        /// asked so that a card which could not become an item is never offered as one, rather than
        /// discovered after the card has already left the deck.</para>
        ///
        /// <para><b>The two mutations are ordered and the second cannot fail.</b>
        /// <c>TrySetAsideFromDeck</c> is p.15's own Deck-to-SetAside transition and
        /// <c>TryPlaceSetAsideCard</c> is p.16's SetAside-to-ItemSlot one; the card has to pass through
        /// the page's intermediate state because that is the state the item placement reads. The
        /// candidate filter has already answered every refusal the placement can state, so a refusal
        /// here is an engine defect - reported as <see cref="EffectExecutionResultKind.ImpossibleMandatory"/>
        /// rather than thrown, for the reason every other capability in this file reports one.</para>
        /// </summary>
        private sealed class TakeDeckCardAsItemHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public EffectCapabilityReference Reference => TakeDeckCardAsItemV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("hero", EffectValueKind.Hero),

                        // p.15's Tier, closed to the four the page prints. Required, because a
                        // declaration naming none would take any card out of the deck and the printed
                        // sentence names one.
                        new EffectCapabilityParameter(
                            ItemCardTierParameter,
                            EffectValueKind.Integer,
                            isRequired: true,
                            integerDomain: EffectIntegerDomain.All<CardTier>())
                    },
                    new[]
                    {
                        new EffectOutputBinding("hero", EffectValueKind.Hero),
                        new EffectOutputBinding(TakenItemCardOutput, EffectValueKind.CardInstance),
                        new EffectOutputBinding(GrantedItemOrderOutput, EffectValueKind.Integer)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    // p.10's impossible step. The bullet that prints this is one of a modal's, and a
                    // bullet nothing can perform is not offered - which `Anticipate` below reports so
                    // the offer never admits it in the first place.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // One card, chosen by the hero whose deck it is, even when the deck holds exactly one
                // candidate: p.10 gives her the decision and a rule that took her last Tier II card
                // for her would be deciding it.
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.None,
                    candidates.Select(EffectValue.Card).ToArray(),
                    1,
                    1,
                    Chooser(context)));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.CardInstance)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived, not trusted: the answer is checked against the deck as it stands now, so
                // a card that has moved between the offer and the answer is refused rather than taken
                // from wherever it has got to. Nothing is mutated on that path.
                if (!Candidates(context).Any(candidate => candidate.Equals(selected[0].CardInstanceId)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var card = context.CardState.Instances.FirstOrDefault(
                    instance => instance != null && instance.Id.Equals(selected[0].CardInstanceId));
                if (card == null || !CardLifecycleAuthority.TrySetAsideFromDeck(card))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var placement = context.HeroItems.TryPlaceSetAsideCard(
                    card, context.CardState.Definitions);
                if (!placement.IsPlaced)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("hero", EffectValue.Hero(placement.Item.OwnerId)),
                    new EffectOutcomeFact(
                        TakenItemCardOutput, EffectValue.Card(placement.Item.SourceCardInstanceId)),
                    new EffectOutcomeFact(
                        GrantedItemOrderOutput, EffectValue.Integer(placement.Item.Order))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(EffectValue.Card).ToArray();

            /// <summary>One card, which is what "a Tier II card" asks for.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            /// <summary>The hero whose deck is read and whose decision this is.</summary>
            private static HeroId Chooser(EffectCapabilityContext context)
            {
                var declared = context.Input("hero");
                return declared.Kind == EffectValueKind.Hero && declared.HeroId.IsDefined
                    ? declared.HeroId
                    : default;
            }

            /// <summary>p.15's Tier the declaration names, read back out of its closed vocabulary.</summary>
            private static CardTier Tier(EffectCapabilityContext context)
            {
                var declared = context.Input(ItemCardTierParameter);
                return declared.Kind == EffectValueKind.Integer &&
                    Enum.IsDefined(typeof(CardTier), declared.IntegerValue)
                    ? (CardTier)declared.IntegerValue
                    : CardTier.None;
            }

            /// <summary>
            /// The cards this sentence can take, in the query's deterministic order - and the one place
            /// the set is decided, so the offer, the probe and the answer cannot disagree about it.
            /// </summary>
            private static IReadOnlyList<CardInstanceId> Candidates(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                var tier = Tier(context);
                if (!chooser.IsDefined || tier == CardTier.None ||
                    context.HeroItems == null || !context.CardState.IsAvailable)
                {
                    return Array.Empty<CardInstanceId>();
                }

                var definitions = context.CardState.Definitions;
                return Array.AsReadOnly(CardCharacteristicQuery
                    .FindCandidates(
                        new CardCharacteristicCriteria(
                            ownerId: chooser,
                            tiers: new[] { tier },
                            locations: new[] { CardLocation.Deck }),
                        definitions,
                        context.CardState.Instances)
                    .Where(candidateId => AcceptsAsItem(context, candidateId, definitions))
                    .ToArray());
            }

            /// <summary>
            /// p.16's two conditions on the card itself: it prints an Item icon, and its owner's
            /// dashboard has a slot that accepts that icon. See the type remarks for why they are
            /// asked at the offer rather than discovered at the placement.
            /// </summary>
            private static bool AcceptsAsItem(
                EffectCapabilityContext context,
                CardInstanceId candidateId,
                IReadOnlyList<CardDefinition> definitions)
            {
                var card = context.CardState.Instances.FirstOrDefault(
                    instance => instance != null && instance.Id.Equals(candidateId));
                var definition = card == null
                    ? null
                    : definitions.FirstOrDefault(
                        value => value != null && value.Reference.Equals(card.DefinitionReference));
                return definition != null &&
                    definition.Progression.HasItemIcon &&
                    context.HeroItems.AcceptsIcon(card.OwnerId, definition.Progression.ItemIcon);
            }
        }

        private sealed class GainResourceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => GainResourceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("recipient", EffectValueKind.Hero),
                        new EffectCapabilityParameter("amount", EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer),

                        // Milestone 8.57a. Whether the credit happened, for a card whose "if they do"
                        // gates a step about nobody in particular - see DidGrantOutputName.
                        new EffectOutputBinding(DidGrantOutputName, EffectValueKind.Boolean)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static bool CanApply(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable && context.Input("recipient").HeroId.IsDefined &&
                context.RosterState.Coins.CanGrant(
                    context.Input("recipient").HeroId, context.Input("amount").IntegerValue);
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanApply(context) || !context.RosterState.Coins.TryGrant(
                    context.Input("recipient").HeroId,
                    context.Input("amount").IntegerValue,
                    CoinGrantKind.CardEffect,
                    out var operation))
                    return EffectExecutionResult.ImpossibleMandatory();
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact("recipient", EffectValue.Hero(operation.RecipientHeroId)),
                    new EffectOutcomeFact("amount", EffectValue.Integer(operation.Amount))
                };
                if (context.Publishes(DidGrantOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        DidGrantOutputName, EffectValue.Boolean(true)));
                }

                return EffectExecutionResult.Applied(facts);
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanApply(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Milestone 8.39g1. See <see cref="ClaimSoloVictoryV1"/>.
        ///
        /// <para>It records and does not end. <c>ISoloVictoryClaim.TryClaim</c> writes the winner and
        /// returns; the match stops at the end of the turn command, through the boundary milestone 8.16
        /// widened to notice every ending the state describes. Ending it here would mean calling
        /// <c>EndGame</c> - which stops the turn's in-flight resolution - from inside the resolution
        /// still running.</para>
        /// </summary>
        private sealed class ClaimSoloVictoryHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ClaimSoloVictoryV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            SoloVictoryThresholdParameter, EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding(SoloVictoryClaimedOutput, EffectValueKind.Boolean),
                        new EffectOutputBinding("winner", EffectValueKind.Hero)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Whether this state can be asked the question at all - a composition carrying no round to
            /// claim through answers no, which the node's own policy then reads as an impossible step
            /// rather than as a silent nothing.
            /// </summary>
            private static bool CanApply(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable &&
                context.RosterState.SoloVictory != null &&
                context.ControllerId.IsDefined &&
                context.Input(SoloVictoryThresholdParameter).IntegerValue > 0;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanApply(context))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var controller = context.ControllerId;

                // Read at the branch, from the live ledger. Milestone 8.39g's own case records why it
                // cannot be a high-water mark: coins are farmed by everyone and spent back down at
                // p.15's Level Up, so what wins is holding them HERE.
                var held = context.RosterState.Coins.TryGetCoins(controller, out var coins)
                    ? coins
                    : 0;
                var claimed =
                    held >= context.Input(SoloVictoryThresholdParameter).IntegerValue &&
                    context.RosterState.SoloVictory.TryClaim(controller);

                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(
                        SoloVictoryClaimedOutput, EffectValue.Boolean(claimed))
                };
                if (claimed && context.Publishes("winner"))
                {
                    facts.Add(new EffectOutcomeFact("winner", EffectValue.Hero(controller)));
                }

                return EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// A walk judging whether the branch can be taken must not TAKE it, so this asks only
            /// whether the question is answerable and never touches the claim.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanApply(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class TransferResourceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => TransferResourceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("source", EffectValueKind.Hero),
                        new EffectCapabilityParameter("recipient", EffectValueKind.Hero),
                        new EffectCapabilityParameter("chooser", EffectValueKind.Hero),
                        new EffectCapabilityParameter("minimum", EffectValueKind.Integer),
                        new EffectCapabilityParameter("maximum", EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding("source", EffectValueKind.Hero),
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<int> Candidates(EffectCapabilityContext context)
            {
                if (!context.RosterState.IsAvailable) return Array.Empty<int>();
                var source = context.Input("source").HeroId;
                var recipient = context.Input("recipient").HeroId;
                var minimum = context.Input("minimum").IntegerValue;
                var maximum = context.Input("maximum").IntegerValue;
                if (!source.IsDefined || !recipient.IsDefined || source.Equals(recipient) ||
                    minimum < 1 || maximum < minimum) return Array.Empty<int>();
                var ledger = context.RosterState.Coins;
                if (!ledger.TryGetCoins(source, out var sourceBalance) ||
                    !ledger.TryGetCoins(recipient, out var recipientBalance))
                    return Array.Empty<int>();
                var admittedMaximum = Math.Min(maximum, sourceBalance);
                if (admittedMaximum < minimum) return Array.Empty<int>();
                return Enumerable.Range(minimum, admittedMaximum - minimum + 1)
                    .Where(amount => (long)recipientBalance + amount <= int.MaxValue)
                    .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                var chooser = context.Input("chooser").HeroId;
                return candidates.Count == 0 || !IsKnownHero(context, chooser)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None, candidates.Select(EffectValue.Integer), 1, 1, chooser));
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Integer ||
                    !Candidates(context).Contains(selected[0].IntegerValue) ||
                    !context.RosterState.Coins.TryTransfer(
                        context.Input("source").HeroId,
                        context.Input("recipient").HeroId,
                        selected[0].IntegerValue,
                        out var operation))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("source", EffectValue.Hero(operation.SourceHeroId)),
                    new EffectOutcomeFact("recipient", EffectValue.Hero(operation.RecipientHeroId)),
                    new EffectOutcomeFact("amount", EffectValue.Integer(operation.Amount))
                });
            }
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ||
                !IsKnownHero(context, context.Input("chooser").HeroId)
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private static bool IsKnownHero(EffectCapabilityContext context, HeroId heroId) =>
            heroId.IsDefined && context.Participants != null &&
            context.Participants.Heroes.Any(hero => hero.Id.Equals(heroId));

        /// <summary>
        /// Milestone 8.35z29. The two questions Misa's Red ranged pair asks about its own target,
        /// sharing one handler because both are "how far is the target from me" and differing only in
        /// which moment they measure - which is the entire printed difference between the two cards.
        /// </summary>
        private sealed class EnclosingTargetDistanceHandler : IEffectCapabilityHandler
        {
            /// <summary>
            /// Whether this arm reads the spaces p.14 captured when the Attack bound its target (Power
            /// Shot's "was") or the live board (Thunder Shot's "is").
            /// </summary>
            private readonly bool atTargetingTime;

            public EnclosingTargetDistanceHandler(
                EffectCapabilityReference reference, string output, bool atTargetingTime)
            {
                Reference = reference;
                this.atTargetingTime = atTargetingTime;
                Schema = new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding(output, EffectValueKind.Boolean) });
                OutputName = output;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            private string OutputName { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(OutputName, EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private bool? Read(EffectCapabilityContext context)
            {
                var attack = context.EnclosingAttack;
                if (!attack.IsDefined || !attack.HasTarget)
                {
                    return null;
                }

                return atTargetingTime ? WasAtMaximumRange(context, attack) : IsNotAdjacent(context, attack);
            }

            /// <summary>
            /// Power Shot's question, asked of the two spaces p.14 captured at step 2 rather than of
            /// the board. An Attack with no captured target space has not bound one, which is a
            /// question with no answer rather than a false one.
            /// </summary>
            private static bool? WasAtMaximumRange(
                EffectCapabilityContext context,
                GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts attack)
            {
                if (!attack.TargetSpaceBeforeConsequences.HasValue ||
                    !attack.ActorSpaceAtActionStart.HasValue ||
                    !context.CardState.IsAvailable)
                {
                    return null;
                }

                var range = RangeValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications,
                    ActionValueApplicabilityQuery.For(
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions)).Total;
                return range >= 1 && HexDistanceQuery.Calculate(
                    attack.ActorSpaceAtActionStart.Value,
                    attack.TargetSpaceBeforeConsequences.Value) == range;
            }

            /// <summary>
            /// Thunder Shot's question, asked of the live board at p.14's step 7. A target step 6
            /// removed stands nowhere and is therefore not adjacent, which is the sentence read
            /// literally rather than an exception made for it.
            /// </summary>
            private static bool IsNotAdjacent(
                EffectCapabilityContext context,
                GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts attack)
            {
                var board = context.Components?.Board;
                if (board == null)
                {
                    return true;
                }

                var target = attack.HeroTargetId.IsDefined
                    ? BoardObjectReference.ForHero(attack.HeroTargetId)
                    : BoardObjectReference.ForMinion(attack.MinionTargetId);
                return !board.TryGetObjectCoordinate(target, out var targetAt) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt) ||
                    HexDistanceQuery.Calculate(actorAt, targetAt) != 1;
            }
        }

        /// <summary>
        /// Milestone 8.35z29. One of the units a straight-line movement passed through - Misa's
        /// five Blue Skills' second clause.
        ///
        /// <para>The interior of a straight line between two named spaces is arithmetic, so this asks
        /// the board who is standing on those spaces rather than reading a record of who was. Both ends
        /// are bound inputs: by the time this runs the actor is standing on the destination, so her own
        /// space answers only half the question.</para>
        ///
        /// <para>A line with an empty interior - a one-space move, or a move through nobody - offers
        /// nothing, which the declaring node's own <c>may</c> or <c>if able</c> policy turns into a
        /// step simply not taken. That is the same silence p.10 gives every optional step nobody can
        /// perform, and it is why neither clause needs a condition in front of it.</para>
        /// </summary>
        private sealed class TraversedUnitHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();

            public EffectCapabilityReference Reference => ChooseTraversedUnitV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("origin", EffectValueKind.Coordinate),
                        new EffectCapabilityParameter("destination", EffectValueKind.Coordinate),
                        new EffectCapabilityParameter(
                            UnitKindParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(RosterUnitKind.None)),
                        new EffectCapabilityParameter(
                            RelationParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(SourceTeamRelation.None))
                    },
                    new[]
                    {
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion),
                        new EffectOutputBinding(ChosenSpaceOutput, EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = FromValue(selected[0]);
                if (!chosen.IsDefined || !Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var facts = new List<EffectOutcomeFact> { AsOutcome(chosen) };
                AddSpaceIfPublished(context, facts, chosen, ChosenSpaceOutput);
                return EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static IReadOnlyList<BoardObjectReference> Candidates(
                EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var origin = context.Input("origin");
                var destination = context.Input("destination");
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                var relation = (SourceTeamRelation)context.Input(RelationParameter).IntegerValue;
                if (board == null || context.Participants == null ||
                    origin.Kind != EffectValueKind.Coordinate ||
                    destination.Kind != EffectValueKind.Coordinate ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    !StraightLineQuery.TryGetDirection(
                        origin.Coordinate, destination.Coordinate, out var direction, out var distance) ||
                    distance < 2)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                // The interior only: p.17 lets a crossing mover enter and leave an obstacle space, and
                // "moved through" is exactly those spaces - not the one she left and not the one she
                // is standing on.
                var interior = new List<HexCoordinate>();
                for (var step = 1; step < distance; step++)
                {
                    interior.Add(StraightLineQuery.Step(origin.Coordinate, direction, step));
                }

                // Milestone 8.90, the user's ruling on open question 36: an immune unit is never the
                // one these riders choose - asked for the action the text is running inside, as the
                // base class's own targeting asks it.
                var activity = context.EnclosingActivity;
                return interior
                    .SelectMany(space => RosterPresenceQuery.FindUnitsWithin(
                        space, 0, kind, relation, context.ActorId, board,
                        context.Participants, context.Modifications))
                    .Where(target => activity == RuleActivity.None || !TargetQuery.IsPrevented(
                        target.AsRuleObject(),
                        activity,
                        RuleObject.ForHero(context.ActorId),
                        board,
                        context.Participants,
                        context.Modifications,
                        context.ImmunityOverrides))
                    .Distinct()
                    .OrderBy(value => value.Kind)
                    .ThenBy(RosterPresenceQuery.Identity, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        /// Milestone 8.35z29. The two pair selections Misa's Swap cards need, sharing one handler
        /// because the two sentences differ in exactly one clause and nothing else: both take p.19's
        /// units of either side inside her printed Radius disk, both exclude her by p.10's Picking
        /// Targets, and both hand a canonical ordered pair to p.19's Swap.
        /// </summary>
        private sealed class UnitPairHandler
            : TargetingHandler, IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>
            /// Whether the pair must stand on the printed ring (Living Tornado) or merely on one ring
            /// each, the same one (Storm Spirit). It is a constructor argument rather than a parameter
            /// for the reason the capability names are two: a reader auditing a node should learn which
            /// question is asked from the reference the node carries.
            /// </summary>
            private readonly bool onTheMaximumRingOnly;

            public UnitPairHandler(EffectCapabilityReference reference, bool onTheMaximumRingOnly)
            {
                Reference = reference;
                this.onTheMaximumRingOnly = onTheMaximumRingOnly;
            }

            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                SwapActivity;

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        // Milestone 8.35l's optionality, declared exactly as every other selection
                        // declares it: a pair whose result no single later node consumes still has a
                        // legal shape.
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node,
                            isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                        new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                        new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                        new EffectOutputBinding("secondMinion", EffectValueKind.Minion),
                        new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                        new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count < 2
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 2, 2));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var facts = Publish(context, selected);
                return facts == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count < 2
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(AsValue).ToArray();

            /// <summary>Naming the pair mutates nothing; the Swap after it does. Milestone 8.35l.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>
            /// Two, always: "swap two units" and p.19's Swap moves both, so an answer naming one is not
            /// a smaller version of this question but no answer at all.
            /// </summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 2;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Publish(context, chosen) ?? Array.Empty<EffectOutcomeFact>();

            /// <summary>
            /// What answering publishes, or nothing where the answer is not one this selection would
            /// take. The equal-distance clause is checked HERE rather than only in the candidate set,
            /// because it is a fact about the pair: two units this offer legitimately showed can still
            /// be an illegal pair, and refusing that answer is what makes the relational constraint
            /// real without deriving the offer from an answer nobody has given yet.
            /// </summary>
            private IReadOnlyList<EffectOutcomeFact> Publish(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                if (chosen.Length != 2 || chosen.Distinct().Count() != 2 ||
                    chosen.Any(value => !value.IsDefined || !candidates.Contains(value)) ||
                    !StandAtOneDistance(context, chosen[0], chosen[1]))
                {
                    return null;
                }

                var canonical = Canonical(chosen);
                var facts = new List<EffectOutcomeFact>
                {
                    AsOutcome(canonical[0], "firstHero", "firstMinion"),
                    AsOutcome(canonical[1], "secondHero", "secondMinion")
                };
                AddSpaceIfPublished(context, facts, canonical[0], "firstSpace");
                AddSpaceIfPublished(context, facts, canonical[1], "secondSpace");
                return facts;
            }

            /// <summary>
            /// "at equal distance from you" for the Tier III card, and nothing at all for the Tier II -
            /// whose members are already all on one ring, so a second test would restate the candidate
            /// filter rather than add to it.
            /// </summary>
            private bool StandAtOneDistance(
                EffectCapabilityContext context,
                BoardObjectReference first,
                BoardObjectReference second)
            {
                if (onTheMaximumRingOnly)
                {
                    return true;
                }

                var board = context.Components?.Board;
                return board != null &&
                    board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt) &&
                    board.TryGetObjectCoordinate(first, out var firstAt) &&
                    board.TryGetObjectCoordinate(second, out var secondAt) &&
                    HexDistanceQuery.Calculate(actorAt, firstAt) ==
                        HexDistanceQuery.Calculate(actorAt, secondAt);
            }

            /// <summary>
            /// Every unit this card could swap, re-derived on every ask. For the Tier III card the set
            /// is narrowed to units that have a partner at their own distance, so that milestone
            /// 8.35l's rule holds: nothing is offered that cannot be half of a legal pair.
            /// </summary>
            private IReadOnlyList<BoardObjectReference> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || context.Participants == null ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var reach = PrintedRadius(context);
                if (reach < 1)
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var inRadius = RosterPresenceQuery.FindUnitsWithin(
                        actorAt, reach, RosterUnitKind.Unit, SourceTeamRelation.AnyTeam,
                        context.ActorId, board, context.Participants, context.Modifications,
                        BoardObjectReference.ForHero(context.ActorId))
                    .Where(target => !TargetQuery.IsPrevented(
                        target.AsRuleObject(), RuleActivity.Swap,
                        RuleObject.ForHero(context.ActorId), board, context.Participants,
                        context.Modifications, context.ImmunityOverrides))
                    .Select(target => board.TryGetObjectCoordinate(target, out var at)
                        ? (Unit: target, Distance: HexDistanceQuery.Calculate(actorAt, at), Found: true)
                        : (Unit: target, Distance: 0, Found: false))
                    .Where(value => value.Found)
                    .ToArray();

                if (onTheMaximumRingOnly)
                {
                    return inRadius
                        .Where(value => value.Distance == reach)
                        .Select(value => value.Unit)
                        .ToArray();
                }

                return inRadius
                    .Where(value => inRadius.Count(other =>
                        !other.Unit.Equals(value.Unit) && other.Distance == value.Distance) > 0)
                    .Select(value => value.Unit)
                    .ToArray();
            }

            /// <summary>
            /// p.9's printed Radius icon with p.16's matching item composed onto it, read through the
            /// same query every other printed-icon consumer in this catalog reads, so a card's
            /// selection and a card's Place cannot come to measure different disks.
            /// </summary>
            private static int PrintedRadius(EffectCapabilityContext context) =>
                !context.CardState.IsAvailable
                    ? 0
                    : RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications).Total;
        }

        private static bool TryOrigin(
            EffectCapabilityContext context,
            RosterQueryOrigin origin,
            out HexCoordinate coordinate)
        {
            coordinate = default;
            var board = context.Components?.Board;
            if (board == null) return false;
            if (origin == RosterQueryOrigin.Actor)
            {
                // Milestone 8.35z42. The one place a declared `origin = Actor` becomes a
                // square, so it is the one place a walk standing before an untaken Move can say which
                // square it means. A live resolution never carries one and reads the board exactly as
                // it always did.
                if (context.ProspectiveActorSpace.HasValue)
                {
                    coordinate = context.ProspectiveActorSpace.Value;
                    return true;
                }

                return board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(context.ActorId), out coordinate);
            }

            if (origin == RosterQueryOrigin.EnclosingTargetBeforeConsequences &&
                context.EnclosingAttack.TargetSpaceBeforeConsequences.HasValue)
            {
                coordinate = context.EnclosingAttack.TargetSpaceBeforeConsequences.Value;
                return true;
            }
            return false;
        }

        private static BoardObjectReference EnclosingTarget(EffectCapabilityContext context)
        {
            if (context.EnclosingAttack.HeroTargetId.IsDefined)
                return BoardObjectReference.ForHero(context.EnclosingAttack.HeroTargetId);
            if (context.EnclosingAttack.MinionTargetId.IsDefined)
                return BoardObjectReference.ForMinion(context.EnclosingAttack.MinionTargetId);
            return default;
        }

        private static bool Admits(
            BoardObjectReference target, RosterUnitKind kind, RuleModificationSet modifications)
        {
            if (!target.AsRuleObject().IsDefined) return false;
            if (kind == RosterUnitKind.Unit) return true;
            return EffectiveUnitKindQuery.Is(
                target.AsRuleObject(),
                kind == RosterUnitKind.Hero ? EffectiveUnitKind.Hero : EffectiveUnitKind.Minion,
                modifications);
        }

        private static IReadOnlyList<BoardObjectReference> Canonical(
            IEnumerable<BoardObjectReference> values) =>
            values.OrderBy(value => value.Kind)
                .ThenBy(value => RosterPresenceQuery.Identity(value), StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// Milestone 8.35z33. The handler behind <see cref="DefendedCardForcedMoveV1"/>: the forced
        /// straight-line move Whisper's Gold cards make their target perform at p.14's step 7. The
        /// printed reading of each of its four clauses is on the declaration.
        /// </summary>
        private sealed class DefendedCardForcedMoveHandler : IEffectCapabilityHandler
        {
            private static readonly HexDirection[] Directions =
            {
                HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
            };

            public EffectCapabilityReference Reference => DefendedCardForcedMoveV1;

            /// <summary>
            /// It takes nothing and publishes where the defender ended up. Every fact it needs is a
            /// property of the Attack it is printed inside - who was targeted and what they defended
            /// with - so a parameter naming any of them would be a second place the same fact could be
            /// wrong, which is the reading <c>attacked-target-space@1</c> already takes of the space.
            /// </summary>
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var destinations = Destinations(context, out var moverId);
                if (destinations.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The decision is the defender's, named exactly rather than through a role: with
                // several opponents there is no single "the" opposing hero, and this one is picked
                // out by the Attack rather than by the frame.
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.None,
                    destinations.Select(EffectValue.Space),
                    1,
                    1,
                    chooserHeroId: moverId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived rather than trusted from the paused offer, exactly as the direct Move
                // does: a space that was the furthest this direction reached when the question opened
                // may not be reachable at all by the time the answer arrives.
                var destinations = Destinations(context, out var moverId);
                if (!destinations.Any(value => value.Equals(selected[0].Coordinate)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.61e. The forced move runs one of p.19's six files at full distance, so
                // what it entered and left is p.18's realised line to where it stopped.
                var forcedMover = BoardObjectReference.ForHero(moverId);
                context.Components.Board.TryGetObjectCoordinate(forcedMover, out var forcedFrom);
                var committed = context.Components.Board.CommitValidatedDisplacement(
                    forcedMover,
                    selected[0].Coordinate,
                    GuardsDigital.Core.Rules.Displacement.MoveThroughTrapQuery.RealisedLine(
                        forcedFrom, selected[0].Coordinate));
                return committed.Accepted
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(MovedOutput, EffectValue.Space(committed.Destination))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Destinations(context, out _).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The furthest space each of p.19's six directions reaches at the defended card's printed
            /// Movement value, or nothing at all where any clause of the sentence cannot be satisfied.
            ///
            /// <para><b>One destination per direction, and it is the furthest one.</b> "Moving full
            /// distance" removes p.11's "up to", so the intermediate spaces of a line are not offers;
            /// "as far as possible" is what keeps a blocked direction in the set at the last space it
            /// reaches rather than dropping it. A direction that cannot be entered at all contributes
            /// nothing, which is how a hero hemmed in on every side ends up doing nothing at all.</para>
            /// </summary>
            private static IReadOnlyList<HexCoordinate> Destinations(
                EffectCapabilityContext context, out HeroId moverId)
            {
                moverId = default;

                var attack = context.EnclosingAttack;
                var components = context.Components;
                if (!attack.IsDefined || !attack.HeroTargetId.IsDefined ||
                    !attack.DefenseCardInstanceId.IsDefined ||
                    components?.Board == null || !components.CanDisplace ||
                    !context.CardState.IsAvailable)
                {
                    return Array.Empty<HexCoordinate>();
                }

                // "If able" covers the hero the page removed at step 6: a defeated defender is not
                // standing anywhere, so there is nothing to move.
                var mover = BoardObjectReference.ForHero(attack.HeroTargetId);
                if (!components.Board.TryGetObjectCoordinate(mover, out var moverAt))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var distance = DefendedCardMovement(context, attack.HeroTargetId);
                if (distance < 1)
                {
                    return Array.Empty<HexCoordinate>();
                }

                // Milestone 8.115, Whisper Ruling 16b (user, 2026-09-28): "its movement action that
                // forces you to move as specified". It is the defending hero's Movement ACTION, so a
                // standing Movement restriction prevents it and a Movement limit below the full
                // distance leaves it unable - and "If able" makes either a step not taken.
                var moverRule = GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(attack.HeroTargetId);
                if (context.Modifications != null &&
                    (context.Modifications.IsRestricted(moverRule, RuleActivity.Movement) ||
                        (context.Modifications.TryGetActivityLimit(
                            moverRule, RuleActivity.Movement, out var limit) && limit < distance)))
                {
                    return Array.Empty<HexCoordinate>();
                }

                // Asked as the Movement action it is, so an obstacle permission scoped to Movement
                // actions (RULING 47) is read.
                var routes = components.Board
                    .FindDirectMoveDestinations(
                        mover, distance, context.Modifications, moveIgnoresObstacles: false,
                        permittedActivity: RuleActivity.Movement)
                    .ToArray();

                // Whisper Ruling 16a (user, 2026-09-28): "If able" governs the whole sentence, so only
                // a straight line of EXACTLY the full distance is a destination; a direction stopped
                // short is not offered, and with none the step does nothing. This supersedes the
                // "furthest each direction reaches" reading shipped at 8.35z33.
                var furthest = new List<HexCoordinate>();
                foreach (var direction in Directions)
                {
                    var best = default(HexCoordinate);
                    var bestCost = 0;
                    foreach (var route in routes)
                    {
                        if (route.Cost != distance ||
                            !StraightLineQuery.TryGetDirection(
                                moverAt, route.Destination, out var walked, out var walkedDistance) ||
                            walked != direction || walkedDistance != route.Cost ||
                            !IsStraightRoute(moverAt, direction, route.Route))
                        {
                            continue;
                        }

                        if (route.Cost > bestCost)
                        {
                            bestCost = route.Cost;
                            best = route.Destination;
                        }
                    }

                    if (bestCost > 0)
                    {
                        furthest.Add(best);
                    }
                }

                moverId = attack.HeroTargetId;
                return furthest
                    .Distinct()
                    .OrderBy(value => value.Q)
                    .ThenBy(value => value.R)
                    .ToArray();
            }

            /// <summary>
            /// The printed Movement value of the card the defender defended with, composed with
            /// rulebook p.16's matching item, or zero where that card prints no Movement action at
            /// all - which is the "if able" this sentence fails silently on.
            /// </summary>
            private static int DefendedCardMovement(EffectCapabilityContext context, HeroId defenderId)
            {
                var instance = context.CardState.Instances
                    .FirstOrDefault(card =>
                        card.Id.Equals(context.EnclosingAttack.DefenseCardInstanceId));
                if (instance == null)
                {
                    return 0;
                }

                var definition = context.CardState.Definitions
                    .FirstOrDefault(candidate =>
                        candidate.Reference.Equals(instance.DefinitionReference));

                // p.9 prints one Movement icon per card, wherever it sits: on the left ribbon for a
                // face whose primary is something else, and in the primary band for a face whose
                // primary IS the Movement. "a movement action on the card" names whichever one that
                // card has, so the action is looked up by kind rather than by origin.
                var movement = definition?.ActionDefinitions
                    .FirstOrDefault(action => action.Kind == ActionKind.Movement);
                if (movement == null)
                {
                    return 0;
                }

                var offer = new ActionOffer(
                    instance.Id,
                    instance.DefinitionReference,
                    movement.Id,
                    defenderId,
                    movement.Origin == ActionDefinitionOrigin.PrintedPrimary
                        ? ActionInvocationContext.PrintedPrimary
                        : ActionInvocationContext.PrintedSecondary,
                    true);

                return MovementValueQuery
                    .Calculate(offer, context.CardState.Definitions, context.HeroItems, context.Modifications)
                    .Total;
            }
        }

        /// <summary>
        /// Milestone 8.35z33. The handler behind <see cref="EnemyHeroWithDiscardPresentV1"/>.
        /// </summary>
        private sealed class EnemyHeroWithDiscardPresentHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>Milestone 9.6: a predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>Milestone 9.6: what the predicate would publish, so the offer reads its branch.</summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => EnemyHeroWithDiscardPresentV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(Present(context)))
                });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;

            /// <summary>
            /// Whether any enemy hero in the printed disk holds a discarded card right now.
            ///
            /// <para>An unanswerable board - no components, no card state, an actor standing nowhere -
            /// reports false rather than impossible. The clause is a condition, and rulebook p.10 lets
            /// a condition that is not satisfied leave the rest of the card alone; reporting the step
            /// impossible would stop an action over a question that simply had no answer.</para>
            /// </summary>
            private static bool Present(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components?.Board == null || context.Participants == null ||
                    !context.CardState.IsAvailable ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return false;
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications);
                if (radius.Total <= 0)
                {
                    return false;
                }

                // p.18 makes immunity irrelevant to a presence question - "Effects that check for unit
                // presence will count immune units" - so no immunity filter is applied here, and none
                // would be right: this asks whether somebody is standing there holding something, not
                // whether anything may be done to them.
                return RosterPresenceQuery.FindUnitsWithin(
                        actorAt, radius.Total, RosterUnitKind.Hero, SourceTeamRelation.OpposingSource,
                        context.ActorId, components.Board, context.Participants, context.Modifications)
                    .Where(unit => unit.Kind == BoardObjectKind.Hero)
                    .Any(unit => context.CardState.Instances.Any(card =>
                        card.OwnerId.Equals(unit.HeroId) &&
                        card.State.Location == CardLocation.DiscardPile));
            }
        }

        /// <summary>
        /// Milestone 8.37. The handler behind <see cref="DefendedCardColorV1"/>. See the
        /// type remarks there.
        /// </summary>
        private sealed class DefendedCardColorHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DefendedCardColorV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding(DefendedOutput, EffectValueKind.Boolean),
                        new EffectOutputBinding(ColorOutput, EffectValueKind.Integer)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// The card the target defended with, or <c>null</c> where step 3 recorded none - a
            /// declined defense, a Hold, or no Defense window at all.
            /// </summary>
            private static CardDefinition DefenseCardDefinition(EffectCapabilityContext context)
            {
                if (!context.EnclosingAttack.IsDefined ||
                    !context.EnclosingAttack.DefenseCardInstanceId.IsDefined ||
                    !context.CardState.IsAvailable)
                {
                    return null;
                }

                var instance = context.CardState.Instances.FirstOrDefault(
                    card => card.Id.Equals(context.EnclosingAttack.DefenseCardInstanceId));
                return instance == null
                    ? null
                    : context.CardState.Definitions.FirstOrDefault(
                        definition => definition.Reference.Equals(instance.DefinitionReference));
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var definition = DefenseCardDefinition(context);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(DefendedOutput, EffectValue.Boolean(definition != null)),
                    new EffectOutcomeFact(
                        ColorOutput,
                        EffectValue.Integer((int)(definition?.Progression.Color ?? CardColor.None)))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// Milestone 8.35z33. The handler behind <see cref="ChooseSwapSubjectV1"/>: one selection over
        /// the union of an adjacency arm and a printed-Range hero arm.
        /// </summary>
        private sealed class ChooseSwapSubjectHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                SwapActivity;

            public EffectCapabilityReference Reference => ChooseSwapSubjectV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            AdjacentUnitKindParameter,
                            EffectValueKind.Integer,
                            isRequired: false,
                            integerDomain: EffectIntegerDomain.All<RosterUnitKind>()),
                        new EffectCapabilityParameter(
                            RangedHeroRelationParameter,
                            EffectValueKind.Integer,
                            isRequired: false,
                            integerDomain: EffectIntegerDomain.All<SourceTeamRelation>()),
                        new EffectCapabilityParameter(
                            RangedHeroRequiresDiscardParameter,
                            EffectValueKind.Boolean,
                            isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding(ChosenSpaceOutput, EffectValueKind.Coordinate),
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Subjects(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = FromValue(selected[0]);
                if (!chosen.IsDefined || !Subjects(context).Contains(chosen) ||
                    !context.Components.Board.TryGetObjectCoordinate(chosen, out var at))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    AsOutcome(chosen, ChosenHeroOutput, ChosenMinionOutput),
                    new EffectOutcomeFact(ChosenSpaceOutput, EffectValue.Space(at))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Subjects(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The union the sentence names, in canonical order and without duplicates - an enemy hero
            /// standing adjacent with a card in their discard satisfies both arms and is one candidate,
            /// which is what "or" means about a set.
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> Subjects(
                EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || context.Participants == null ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var found = new List<BoardObjectReference>();

                var adjacentKind = (RosterUnitKind)context.Input(AdjacentUnitKindParameter).IntegerValue;
                if (adjacentKind != RosterUnitKind.None &&
                    Enum.IsDefined(typeof(RosterUnitKind), adjacentKind))
                {
                    // p.19's innermost ring, written into the sentence, and every side of the board:
                    // "a unit adjacent to you" names neither team, so neither is excluded.
                    found.AddRange(Admitted(
                        context, board,
                        RosterPresenceQuery.FindUnitsWithin(
                            actorAt, 1, adjacentKind, SourceTeamRelation.AnyTeam,
                            context.ActorId, board, context.Participants, context.Modifications)));
                }

                var heroRelation = (SourceTeamRelation)context
                    .Input(RangedHeroRelationParameter).IntegerValue;
                if (heroRelation != SourceTeamRelation.None &&
                    Enum.IsDefined(typeof(SourceTeamRelation), heroRelation) &&
                    context.CardState.IsAvailable)
                {
                    var range = RangeValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications,
                        // Card text resolving inside an action asks for the same Range total that
                        // action's own targeting asked for, so the class is read from the card and
                        // action this node is inside rather than assumed from the node.
                        ActionValueApplicabilityQuery.For(
                            context.SourceDefinitionReference,
                            context.ActionDefinitionId,
                            context.CardState.Definitions));
                    if (range.Total > 0)
                    {
                        var requiresDiscard =
                            context.Input(RangedHeroRequiresDiscardParameter).BooleanValue;
                        found.AddRange(Admitted(
                            context, board,
                            RosterPresenceQuery.FindUnitsWithin(
                                    actorAt, range.Total, RosterUnitKind.Hero, heroRelation,
                                    context.ActorId, board, context.Participants, context.Modifications)
                                .Where(unit => unit.Kind == BoardObjectKind.Hero)
                                .Where(unit => !requiresDiscard || HoldsADiscard(context, unit.HeroId))
                                .ToArray()));
                    }
                }

                // A radius-one query includes its origin; "adjacent" does not. A swap
                // with the actor itself is invalid in either arm of this union.
                return found
                    .Where(value => !value.Equals(BoardObjectReference.ForHero(context.ActorId)))
                    .Distinct()
                    .OrderBy(value => value.Kind)
                    .ThenBy(value => value.HeroId.Value ?? value.MinionId.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            /// <summary>
            /// p.18's immunity question, asked once for both arms: a unit immune to being swapped by
            /// this actor is not a legal subject of either half of the sentence.
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> Admitted(
                EffectCapabilityContext context,
                IBoardDisplacementAuthority board,
                IReadOnlyList<BoardObjectReference> candidates) =>
                candidates
                    .Where(candidate => !TargetQuery.IsPrevented(
                        candidate.AsRuleObject(),
                        RuleActivity.Swap,
                        BoardObjectReference.ForHero(context.ActorId).AsRuleObject(),
                        board,
                        context.Participants,
                        context.Modifications,
                        context.ImmunityOverrides))
                    .ToArray();

            // Milestone 8.35z45: one derivation rather than this file's own copy of it, so
            // a card that swaps by the fact, a card that selects by it and a modal Attack bullet that
            // narrows by it cannot come to disagree about what "in the discard" means.
            private static bool HoldsADiscard(EffectCapabilityContext context, HeroId heroId) =>
                HeroDiscardPileQuery.HasAnyCard(context.CardState.Instances, heroId);
        }

        private static bool IsStraightRoute(
            HexCoordinate origin,
            HexDirection direction,
            IReadOnlyList<HexCoordinate> route)
        {
            if (route == null || route.Count < 2 || !route[0].Equals(origin)) return false;
            for (var index = 1; index < route.Count; index++)
            {
                if (!route[index].Equals(StraightLineQuery.Step(origin, direction, index)))
                    return false;
            }
            return true;
        }

        private static IReadOnlyList<RuleActivity> AllActivities { get; } =
            Array.AsReadOnly(Enum.GetValues(typeof(RuleActivity)).Cast<RuleActivity>()
                .Where(value => value != RuleActivity.None).ToArray());

        private static IReadOnlyList<RuleActivity> SwapActivity { get; } =
            Array.AsReadOnly(new[] { RuleActivity.Swap });

        private sealed class ChargeOption
        {
            public ChargeOption(
                HexCoordinate destination,
                IReadOnlyList<BoardObjectReference> targets)
            {
                Destination = destination;
                Targets = targets;
            }
            public HexCoordinate Destination { get; }
            public IReadOnlyList<BoardObjectReference> Targets { get; }
        }

        private sealed class ThroughOption
        {
            public ThroughOption(BoardObjectReference target, HexCoordinate destination)
            {
                Target = target;
                Destination = destination;
            }
            public BoardObjectReference Target { get; }
            public HexCoordinate Destination { get; }
        }
    }
}
