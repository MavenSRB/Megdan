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
        /// </summary>
        public static PrintedAttackTargetPolicy Overlay(
            PrintedAttackTargetPolicy printedPolicy,
            AttackRangeKind printedClassification,
            HeroId attackerId,
            RuleModificationSet modifications) =>
            printedPolicy.Reach == AttackTargetReach.AdjacentOnly &&
            Classify(printedClassification, attackerId, modifications) == AttackRangeKind.Ranged
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
