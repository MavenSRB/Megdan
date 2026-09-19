using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.57e. Rowenna the Vanguard's Fabled Lance read as the one question every rule that
    /// cares about an Attack's subtype asks: is THIS attacker's Attack a Ranged one, and what printed
    /// Range does it read?
    ///
    /// <para>The printed sentence is "All your attack actions gain the &quot;Ranged&quot; subtype,
    /// target a unit in range, and count as having a printed Range value of 2." Rulebook p.13 attaches
    /// exactly two consequences to that subtype - a "Block a ranged attack" answers it, and it "can
    /// target units based on the Range value on the card" - so the three clauses are the subtype, the
    /// reach the subtype already implies, and the number that reach reads. They are answered here, once,
    /// rather than at the three sites that need them.</para>
    ///
    /// <para><b>Why one query rather than three reads of the modification set.</b> Milestone 8.11a's
    /// lesson, restated: the offer decides which units the Attack may take, the resolver decides which
    /// it did take, and the defence preview predicts what a Block will do about it - and a player
    /// chooses from the preview. Three private readings of one declaration are how those three come to
    /// disagree about the same board, which is exactly the failure <c>DefenseBlockQuery</c> was created
    /// to prevent. Every one of them calls this.</para>
    ///
    /// <para><b>It is asked of the ACTION and never of the hero alone.</b> The declaration is carried
    /// on the controller, because p.16 hands an Ultimate's rules to "you"; the printed clause reaches
    /// her "attack actions" and nothing else. Both public readings below therefore take the action's own
    /// printed classification and answer with it unchanged unless the action is one rulebook p.13
    /// classifies at all - which makes her Movement and Skill faces untouched by construction rather
    /// than by a condition some caller could forget.</para>
    ///
    /// <para>Pure, like every other query here: it holds nothing and reads only what it is handed.</para>
    /// </summary>
    public static class RangedAttackOverlayQuery
    {
        /// <summary>
        /// The printed Range every Attack action of <paramref name="attackerId"/> counts as having, or
        /// zero where no source declares one - which is every hero in this build but Rowenna with
        /// Fabled Lance unlocked and on the board.
        /// </summary>
        /// <param name="modifications">
        /// The live set. Required rather than optional, by milestone 6.8f's rule:
        /// <see cref="RuleModificationSet.Empty"/> is where a caller holding none says so.
        /// </param>
        public static int DeclaredPrintedRange(
            HeroId attackerId, RuleModificationSet modifications)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            return attackerId.IsDefined
                ? modifications.AttackActionsCountAsRangedAtPrintedRange(
                    RuleObject.ForHero(attackerId))
                : 0;
        }

        /// <summary>
        /// Milestone 8.57g. The same question asked of PUBLISHED STATE rather than of a live rule set,
        /// for the four presentation readers that draw p.13's subtype on a card.
        ///
        /// <para><b>Why an adapter here rather than a fifth private reading.</b> The four sites this
        /// exists for - the attack spotlight's ranged cue, the decision window's RANGED chip, the hero
        /// overview's RANGE line and the round rail's chip - each read
        /// <c>ActionDefinition.AttackRange</c> straight off the card, which is the PRINTED
        /// classification and stays printed for a very good reason: <c>ActionOffer</c> equality guards
        /// every authorization path, so an offer whose classification moved with an Ultimate would stop
        /// matching the command that answers it. The overlay therefore has to be applied by the
        /// DRAWING rather than baked into the offer, and a presenter that read the declaration itself
        /// would be the fifth private reading the type comment above says produces disagreements. This
        /// is the same query, given the source a presenter actually holds.</para>
        ///
        /// <para>A projection carries the definitions and the card instances, which is exactly what
        /// <see cref="Progression.UltimateUnlockQuery.Derive"/> takes: an Ultimate is unlocked when its
        /// card sits beside the dashboard, and the permanent ability it names is on the definition. The
        /// one clause a presenter cannot answer this way is p.16's parenthesis - "as long as your hero
        /// is on the board" - because that needs the board. It is not asked, and that is deliberate
        /// rather than overlooked: a rail, an overview or a spotlight is drawn ABOUT a hero's cards,
        /// and a defeated hero's card still prints what it prints. Nothing here decides legality; the
        /// backend and <c>DefenseBlockQuery</c> keep doing that from the live set.</para>
        /// </summary>
        /// <remarks>
        /// <b>A distinct NAME rather than an overload, and that is a measured decision.</b>
        /// <c>ILConformanceScanner.Method</c> resolves a rule's named member by name and returns
        /// <c>candidates[0]</c> when several match, so adding an overload to a method a conformance
        /// rule names silently re-points that rule at the new one - which is precisely what happened
        /// on this milestone's first offline run: <c>TheOfferTheResolverAndTheDefencePreview_EachRead-
        /// TheOneOverlayQuery</c> went red because the backend was being asked whether it called the
        /// projection-shaped reading. Two names, two questions, and both rules keep asking theirs.
        /// </remarks>
        public static int DeclaredPrintedRangeFromUnlockedUltimates(
            HeroId attackerId, Progression.UnlockedUltimateSet ultimates)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (!attackerId.IsDefined)
            {
                return 0;
            }

            var declared = 0;
            for (var index = 0; index < ultimates.Ultimates.Count; index++)
            {
                var ultimate = ultimates.Ultimates[index];
                if (!ultimate.OwnerId.Equals(attackerId) || ultimate.Rule == null)
                {
                    continue;
                }

                for (var ruleIndex = 0; ruleIndex < ultimate.Rule.Modifications.Count; ruleIndex++)
                {
                    var modification = ultimate.Rule.Modifications[ruleIndex];
                    if (modification.Kind ==
                            RuleModificationKind.AttackActionsCountAsRangedAtPrintedRange &&
                        modification.Amount > declared)
                    {
                        // The LARGEST declared value, which is the reading
                        // `RuleModificationSet.AttackActionsCountAsRangedAtPrintedRange` takes and the
                        // only one under which a second source is not silently dropped.
                        declared = modification.Amount;
                    }
                }
            }

            return declared;
        }

        /// <summary>
        /// Milestone 8.57g. p.13's classification of one printed Attack for a presenter, read from the
        /// unlocked Ultimates a projection publishes - see the overload above for why the four drawing
        /// sites take this route rather than the live modification set.
        /// </summary>
        public static AttackRangeKind ClassifyFromUnlockedUltimates(
            AttackRangeKind printedClassification,
            HeroId attackerId,
            Progression.UnlockedUltimateSet ultimates) =>
            printedClassification == AttackRangeKind.NonRanged &&
            DeclaredPrintedRangeFromUnlockedUltimates(attackerId, ultimates) > 0
                ? AttackRangeKind.Ranged
                : printedClassification;

        /// <summary>
        /// Rulebook p.13's classification of one Attack, with the overlay applied.
        ///
        /// <para><paramref name="printedClassification"/> is the card's own, and an action carrying
        /// <see cref="AttackRangeKind.None"/> is not an Attack at all - a Skill, a Movement or a
        /// Defense - so it is answered unchanged. An Attack already printed
        /// <see cref="AttackRangeKind.Ranged"/> is answered unchanged too, which is the same answer
        /// the overlay would give and is stated rather than computed so that a Ranged face never
        /// depends on the overlay to stay Ranged.</para>
        /// </summary>
        public static AttackRangeKind Classify(
            AttackRangeKind printedClassification,
            HeroId attackerId,
            RuleModificationSet modifications) =>
            printedClassification == AttackRangeKind.NonRanged &&
            DeclaredPrintedRange(attackerId, modifications) > 0
                ? AttackRangeKind.Ranged
                : printedClassification;

        /// <summary>
        /// The Attack's printed target policy with "target a unit in range" put in place of its printed
        /// adjacency clause.
        ///
        /// <para><b>The reach is replaced and nothing else is.</b> Her six Attack faces print "Target a
        /// unit adjacent to you", which is p.13's own adjacency for a non-Ranged Attack, and the
        /// Ultimate's own words say what happens to it: the attacks "target a unit in range" instead.
        /// What the sentence does not touch is WHICH units - "a unit" is p.19's type-insensitive union
        /// and is what her faces already print - nor p.19's straight line, which her set never
        /// mentions, nor milestone 8.19a's binding, which is about when a target is named rather than
        /// which ones are legal. So a hero-only Attack stays hero-only and a card-text-bound one stays
        /// card-text-bound; only the distance moves.</para>
        ///
        /// <para><b>A policy that already reads the printed Range is left exactly as the card prints
        /// it.</b> <see cref="AttackTargetReach.ExactlyPrintedRange"/>,
        /// <see cref="AttackTargetReach.PrintedRangeBeyondAdjacent"/> and
        /// <see cref="AttackTargetReach.PrintedRange"/> are three readings of p.13's own number, and an
        /// overlay that flattened them to the ceiling would silently widen a face whose whole drawback
        /// is a band. No such face is Rowenna's; the arm is stated because the alternative is this
        /// query deciding what it would have meant.</para>
        ///
        /// <para><b>MILESTONE 8.57g CORRECTED THE GATE, and this is the one defect milestone 8.57e
        /// shipped.</b> The condition used to be "the overlaid classification is Ranged", and that is
        /// true of a card PRINTED Ranged whether anything declared anything or not - so an already-
        /// Ranged Attack whose CARD TEXT had narrowed its reach to adjacency had that narrowing
        /// silently undone, for every hero in the build. Whisper's Swift Justice is the face that
        /// found it: her Gold basic prints Ranged with a Range icon of 3, its second bullet prints
        /// "a unit adjacent to you", and after 8.57e the chosen bullet offered five spaces out to
        /// three instead of the innermost ring.
        /// <c>WhisperEngagedUiJourneyPlayModeTests.SwiftJustice_OffersItsTwoBulletsAndTakesThe-
        /// AdjacentUnitTheSecondOneNames</c> is where it surfaced, and it reproduces through the
        /// headless command surface with no HUD at all.</para>
        ///
        /// <para>The gate is now <b>"the classification MOVED"</b>, which is what the printed sentence
        /// actually says: Fabled Lance makes her attack actions "gain the Ranged subtype" and target
        /// in range, so the reach is replaced exactly where the subtype was replaced, and a card that
        /// was already Ranged keeps whatever reach its own text gave it. <see cref="Classify"/>
        /// returns the printed classification unchanged in every case but the one the declaration
        /// changes, so the two halves of this sentence are one question asked once.</para>
        /// </summary>
        public static PrintedAttackTargetPolicy Overlay(
            PrintedAttackTargetPolicy printedPolicy,
            AttackRangeKind printedClassification,
            HeroId attackerId,
            RuleModificationSet modifications) =>
            printedPolicy.Reach == AttackTargetReach.AdjacentOnly &&
            Classify(printedClassification, attackerId, modifications) != printedClassification
                ? printedPolicy.AtThePrintedRange()
                : printedPolicy;

        /// <summary>
        /// The printed Range total of one Attack with the overlay's icon supplied, BEFORE rulebook
        /// p.16's items compose onto it.
        ///
        /// <para><b>A printed-icon contribution, and that is the whole point of the family.</b> p.16
        /// raises an icon and not a number - "If an Action card has no matching icon, the item has no
        /// effect on that particular Action card" - and her Attack faces print no Range icon at all.
        /// The printed clause says the attacks "count as HAVING a printed Range value of 2", so the
        /// contribution this adds is <see cref="ValueContributionKind.PrintedIcon"/> and
        /// <see cref="ItemBonusQuery"/> then finds an icon to raise: Paragon of Grace's Range item makes
        /// her reach 3. A value modifier would have composed to the same two and left the item with
        /// nothing to match, which is the measured difference between the two shapes.</para>
        ///
        /// <para><b>It names the attacking card and its action, not the Ultimate.</b> A printed-icon
        /// contribution is refused without a source action definition, and an Ultimate has none -
        /// rulebook p.16's card prints an ability rather than an action. That refusal points the same
        /// way the sentence does: what counts as printing a Range of 2 is the Attack card, so the
        /// contribution is attributed to the Attack the player is looking at.</para>
        ///
        /// <para><b>The icon is replaced rather than added to.</b> "Counts as having a printed Range
        /// value of 2" states what the icon IS. No face of hers prints one, so nothing is dropped
        /// today; a face that did would read two and not two-plus-its-own, which is the only reading
        /// of the word "counts as" that leaves the sentence saying something.</para>
        /// </summary>
        /// <param name="printedValue">The card's own printed Range result for this action.</param>
        public static ValueResult OverlayPrintedRange(
            ValueResult printedValue,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            HeroId actorId,
            RuleModificationSet modifications)
        {
            if (printedValue == null) throw new ArgumentNullException(nameof(printedValue));

            var declared = DeclaredPrintedRange(actorId, modifications);
            if (declared == 0 ||
                !IsAnAttack(sourceDefinitionReference, actionDefinitionId, definitions))
            {
                return printedValue;
            }

            var kept = printedValue.Contributions
                .Where(contribution =>
                    contribution.Kind != ValueContributionKind.PrintedIcon ||
                    contribution.Channel != PrintedValueChannel.Range)
                .ToArray();
            return new ValueResult(
                printedValue.Channel,
                new[]
                {
                    new ValueContribution(
                        ValueContributionKind.PrintedIcon,
                        sourceCardInstanceId,
                        actionDefinitionId,
                        PrintedValueChannel.Range,
                        declared)
                }.Concat(kept));
        }

        /// <summary>
        /// Whether the named action is one rulebook p.13 classifies - the one condition "all your
        /// attack actions" puts on this overlay. An action this composition cannot resolve answers
        /// false: an unreadable action is not an Attack anyone can prove, and inventing a printed Range
        /// for one would be supplying a rule for a card nobody holds.
        /// </summary>
        private static bool IsAnAttack(
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions) =>
            (definitions ?? Enumerable.Empty<CardDefinition>())
                .FirstOrDefault(definition =>
                    definition != null && definition.Reference.Equals(sourceDefinitionReference))
                ?.ActionDefinitions
                .FirstOrDefault(action => action.Id.Equals(actionDefinitionId))
                ?.Kind == ActionKind.Attack;
    }
}
