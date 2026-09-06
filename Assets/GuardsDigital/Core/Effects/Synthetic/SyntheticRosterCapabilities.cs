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
        public static EffectCapabilityReference ChooseThroughTargetLineV1 =>
            Reference("choose-through-target-line", 1);
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

        public static EffectCapabilityReference RetrieveCardV1 => Reference("retrieve-card", 1);
        public static EffectCapabilityReference GainResourceV1 => Reference("gain-resource", 1);
        public static EffectCapabilityReference TransferResourceV1 => Reference("transfer-resource", 1);

        /// <summary>
        /// Milestone 8.35z28. `empty-minion-spawn-points-in-radius@1` - how many empty minion Spawn
        /// points of the current Battle Zone stand inside the acting card's own printed Radius disk,
        /// and whether that count reaches the number the card asks for.
        ///
        /// <para>Dodger's Dark Ritual and Darker Ritual are the faces that name it: "If there are 2 or
        /// more empty spawn points in radius in the battle zone, gain N coins." The quantity itself is
        /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/>'s, which her
        /// three Blue Defense primaries also read through the value pipeline - one printed phrase, one
        /// derivation, reached two ways because a Defense value is a number and a ritual is a
        /// step.</para>
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
        /// How many qualifying Spawn points an empty-spawn-point question found. Milestone 8.35z28, and
        /// spelled the same as <c>presence-count@1</c>'s because it is the same kind of answer.
        /// </summary>
        public const string CountOutput = "count";

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
        /// Milestone 8.22. The count <c>presence-count@1</c>'s <c>present</c> answer is measured
        /// against - Sabina's Steady Advance and Unwavering Resolve, "If there are two or more
        /// friendly minions in radius".
        /// </summary>
        public const string MinimumPresentParameter = "minimum";

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
        /// reaches. They are here rather than registered because this phase composes her printed set and
        /// does not deal her: no card a configured match deals starts either declaration, and milestone
        /// 7.9's deferral gate refuses a registration no card can reach. Her completion milestone is
        /// what moves these two lines into the registration list - together with the showcase consumer
        /// that gate's second half requires, which is what keeps a new handler from arriving for the
        /// first time inside a verified hero's slice.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers()
        {
            yield return new PlayedAttackThisTurnHandler();
            yield return new EmptyMinionSpawnPointCountHandler(
                EmptyMinionSpawnPointsInRadiusV1, readsPrintedRadius: true);
            yield return new EmptyMinionSpawnPointCountHandler(
                EmptyMinionSpawnPointsAdjacentV1, readsPrintedRadius: false);
            yield return new UltimateUnlockedHandler();
            yield return new GainItemFromTheBoxHandler();
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
            yield return new ActorMovedThisActionHandler();
        }

        private static EffectCapabilityReference Reference(string id, int revision) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), revision);

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
                    new EffectCapabilityParameter(DistanceParameter, EffectValueKind.Integer),
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
                    .Where(target => !ImmunityQuery.IsImmuneTo(
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
                    context.Input(DistanceParameter).IntegerValue,
                    (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue,
                    (SourceTeamRelation)context.Input(RelationParameter).IntegerValue,
                    activity,
                    context.Input(ExcludeTargetParameter).BooleanValue);
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
                        AdjacentToFriendlyMinionParameter, EffectValueKind.Boolean, isRequired: false)
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
                        ImmunityQuery.IsImmuneTo(
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
        /// than argued - see <c>ChargeReorderEquivalenceTests</c>.</para>
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
                            integerDomain: EffectIntegerDomain.AllExcept(RosterUnitKind.None))
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
                // Brogan's charge prints no crossing, so it walks p.18's obstacles the way every
                // Move did before milestone 8.23 - the declaration is the card's, and this card
                // makes none.
                foreach (var route in board.FindDirectMoveDestinations(
                    actor, maximum, context.Modifications, moveIgnoresObstacles: false))
                {
                    if (route.Cost < minimum || route.Cost > maximum ||
                        !StraightLineQuery.TryGetDirection(
                            actorAt, route.Destination, out var direction, out var distance) ||
                        distance != route.Cost || !IsStraightRoute(actorAt, direction, route.Route))
                        continue;

                    var targets = RosterPresenceQuery.FindUnitsWithin(
                            route.Destination, 1, kind, SourceTeamRelation.OpposingSource,
                            context.ActorId, board, context.Participants, context.Modifications)
                        .Where(target => !ImmunityQuery.IsImmuneTo(
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
                        ImmunityQuery.IsImmuneTo(
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

        private sealed class FormerTargetSpaceHandler : IEffectCapabilityHandler
        {
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

        private sealed class HeroDefeatedThisRoundHandler : IEffectCapabilityHandler
        {
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

        private sealed class PresenceCountHandler : IEffectCapabilityHandler
        {
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
                        MinimumPresentParameter, EffectValueKind.Integer, isRequired: false)
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
                var distance = context.Input(DistanceParameter).IntegerValue;
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
                return RosterPresenceQuery.FindUnitsWithin(
                    at,
                    distance,
                    kind,
                    relation,
                    context.ActorId,
                    board,
                    context.Participants,
                    context.Modifications,
                    excludedActor,
                    excludedTarget).Count;
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
        private sealed class EmptyMinionSpawnPointCountHandler : IEffectCapabilityHandler
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
                            MinimumPresentParameter, EffectValueKind.Integer, isRequired: false)
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
                    context.HeroItems);
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

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var count = Count(context);
                return count < 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(CountOutput, EffectValue.Integer(count)),
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
        private sealed class SpacesAdjacentHandler : IEffectCapabilityHandler
        {
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
                                RosterCardRetrievalKind.None))
                    },
                    new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) });
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
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner) && Eligible(card, kind, definitions))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                var chooser = context.Input("chooser").HeroId;
                return candidates.Count == 0 || !IsKnownHero(context, chooser)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
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
                if (card == null || !CardLifecycleAuthority.TryRetrieveEligibleCard(card))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("retrieved", EffectValue.Card(card.Id))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ||
                !IsKnownHero(context, context.Input("chooser").HeroId)
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static bool Eligible(
                CardInstance card,
                RosterCardRetrievalKind kind,
                IReadOnlyList<CardDefinition> definitions)
            {
                var isRetrievableDiscard =
                    card.State.Location == CardLocation.DiscardPile &&
                    !card.State.HasBeenPlayed &&
                    card.State.ResolutionStatus == CardResolutionStatus.None;
                if (kind == RosterCardRetrievalKind.OwnedDiscardedCard)
                    return isRetrievableDiscard;

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
                    chooser);

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
                    context.CardState.Instances);

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
                return chooser.IsDefined
                    ? CardCharacteristicQuery
                        .FindCandidates(
                            HandCriteria(chooser),
                            context.CardState.Definitions,
                            context.CardState.Instances)
                        .Select(EffectValue.Card)
                        .ToArray()
                    : Array.Empty<EffectValue>();
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
        private sealed class OwnedCardPresentHandler : IEffectCapabilityHandler
        {
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

                var owner = context.Input(HeroParameter).HeroId;
                var location = (CardLocation)context.Input(LocationParameter).IntegerValue;
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(
                        context.CardState.Instances.Any(card =>
                            card.OwnerId.Equals(owner) && card.State.Location == location)))
                });
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
        private sealed class UltimateUnlockedHandler : IEffectCapabilityHandler
        {
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
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
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
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("recipient", EffectValue.Hero(operation.RecipientHeroId)),
                    new EffectOutcomeFact("amount", EffectValue.Integer(operation.Amount))
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

        private static bool TryOrigin(
            EffectCapabilityContext context,
            RosterQueryOrigin origin,
            out HexCoordinate coordinate)
        {
            coordinate = default;
            var board = context.Components?.Board;
            if (board == null) return false;
            if (origin == RosterQueryOrigin.Actor)
                return board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(context.ActorId), out coordinate);
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
