using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Rulebook p.14's Block condition, asked once and answered the same way everywhere.
    ///
    /// <para>Milestone 8.11 evaluated this inside the Attack resolver, which was the only place that
    /// needed it. Milestone 8.11a found the second place: the defence preview a defender is shown
    /// before choosing. That preview had been describing a Block as an ordinary Defense card worth
    /// nothing - total zero, no contributions, verdict <see cref="DefenseBlockResult.None"/> - so it
    /// predicted the defender losing an attack the Block in fact stops, and the prompt drawn from it
    /// looked for a printed Defense icon that a Block does not have and failed where it was drawn.</para>
    ///
    /// <para>Both callers ask this now. A preview that disagreed with the resolution would be worse
    /// than no preview at all: it is the thing a player decides from.</para>
    ///
    /// <para><b>Milestone 8.40g widened what both callers are handed, and made it required rather
    /// than optional for that same reason.</b> Bain the Bounty Hunter's three Green Blocks resolve by
    /// a fact about the board - "If a hero in play has a [bounty] bounty marker" - which no signature
    /// carrying the offer, the definitions and the attack's reference could answer at all. The marker
    /// register is therefore a parameter with no default: a caller that could omit it is a caller that
    /// could reach a different verdict from the other one, which is the exact failure milestone 8.11a
    /// exists to prevent. Where the condition needs the register and none was supplied the query
    /// refuses loudly rather than answering, on milestone 8.40c's terms - a silent "no marker out"
    /// looks exactly like a board position and would grey a defence that in fact holds.</para>
    /// </summary>
    public static class DefenseBlockQuery
    {
        /// <summary>
        /// The verdict for answering an Attack with this Defense offer.
        /// <see cref="DefenseBlockResult.None"/> whenever the offer is not a printed Block, which is
        /// what says "this one is settled by its Defense value in the ordinary way".
        /// </summary>
        /// <param name="markers">
        /// The match's marker register, for a Block whose printed condition is about the board.
        /// <c>null</c> is a composition with no register at all rather than a board with no marker on
        /// it, and it is refused rather than answered - see the type's own remarks.
        /// </param>
        /// <param name="attackerId">
        /// Milestone 8.57e. The hero whose Attack opened p.14's window. p.13 classifies an Attack, and
        /// a standing declaration can change what that classification is - Rowenna's Fabled Lance -
        /// so the condition can no longer be answered from the attacking CARD alone. The undefined
        /// hero is a composition naming no attacker, and then the card's own printed classification is
        /// the whole answer, which is what it was before this milestone.
        /// </param>
        /// <param name="modifications">
        /// The live set the overlay is read from. Required rather than optional, by milestone 6.8f's
        /// rule and by milestone 8.11a's: a preview handed none while the resolution was handed the
        /// live set would predict a Block the Attack then refuses, which is the disagreement this
        /// query exists to prevent. <see cref="RuleModificationSet.Empty"/> is the named none.
        /// </param>
        /// <param name="tokens">
        /// Milestone 8.61b. The board's token port, for a Block whose printed condition is about a piece
        /// the defender has placed - Min the Dragonmonk's Poof!, Vanish and Ruse. It is the board both
        /// call sites already hold, exactly as the marker register was, so neither needed a new
        /// collaborator to find one; <c>null</c> is a composition with no board at all and is refused
        /// rather than answered, on milestone 8.40c's terms.
        /// </param>
        /// <param name="heroItems">
        /// Milestone 8.61b. p.16's items, because her condition measures the card's printed Range and a
        /// Range item raises it. Optional in the same sense every other reach reading takes it: no item
        /// state is a match in which nobody has one.
        /// </param>
        /// <param name="capabilities">
        /// Milestone 8.61b. The capability catalog the Defense text was composed against, which is where
        /// "her Smoke bomb" is named - the bounded owned-token fact her mandatory swap consumes. Required
        /// for that condition and unread by every other; see
        /// <see cref="PrintedBlockCondition.TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange"/>
        /// for why the token is read off the card's own text rather than declared beside it.
        /// </param>
        public static DefenseBlockResult Evaluate(
            ActionOffer defenseOffer,
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference attackDefinitionReference,
            ActionDefinitionId attackActionDefinitionId,
            MarkerAttachmentRegister markers,
            HeroId attackerId,
            RuleModificationSet modifications,
            ITokenPlacementAuthority tokens,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            EffectCapabilityCatalog capabilities)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            if (definitions == null)
            {
                return DefenseBlockResult.None;
            }

            var action = ActionOfferQuery.FindActionDefinition(defenseOffer, definitions);
            if (action.DefenseResolution != PrintedDefenseResolution.Block)
            {
                return DefenseBlockResult.None;
            }

            // Milestone 8.61b. Answered before the attack is named for the reason the board arm below
            // is: "a Smoke bomb of yours in range" is true or false of the board whether the attack that
            // opened p.14's window was Ranged or not, and her three faces print no classification clause
            // for it to fall through to.
            if (action.BlockCondition ==
                PrintedBlockCondition.TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange)
            {
                return FulfilsTheSwapsOwnPrecondition(
                        defenseOffer, action, definitions, tokens, heroItems, modifications, capabilities)
                    ? DefenseBlockResult.Blocked
                    : DefenseBlockResult.ConditionUnfulfilled;
            }

            // Milestone 8.40g. The board arm is answered before the attack is named, because it reads
            // nothing the attack carries: "If a hero in play has a bounty marker" is true or false of
            // the board whether the attack that opened p.14's window was Ranged or not. Answering it
            // here rather than inside `Fulfils` is what makes that independence structural instead of
            // incidental - there is no classification for it to fall through to.
            if (action.BlockCondition == PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker)
            {
                return FulfilsTheBoardCondition(defenseOffer, markers)
                    ? DefenseBlockResult.Blocked
                    : DefenseBlockResult.ConditionUnfulfilled;
            }

            // p.14's condition is read off the attacking action's own printed classification rather
            // than off anything the defender holds, which is why the attack has to be named to answer
            // this at all. Which classification fulfils it is the defending card's own printed word:
            // milestone 8.11 could assume "ranged" because all three of Wasp's Blocks print it, and
            // milestone 8.23 cannot, because Tigerclaw's Parry and Riposte print the other arm.
            //
            // Milestone 8.57e. The classification is still the attacking card's, read through the one
            // query that also decides the attack's reach and its target set - Rowenna's Fabled Lance
            // makes every Attack of hers Ranged, so a "Block a ranged attack" now answers her Code of
            // Chivalry and a "Block a non-ranged attack" no longer does. Asking the overlay here rather
            // than letting the resolver hand this query an already-classified value is what keeps the
            // preview and the resolution reading one answer: both call this, and this calls one thing.
            var attack = definitions
                .FirstOrDefault(definition => definition.Reference.Equals(attackDefinitionReference))
                ?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(attackActionDefinitionId));
            return attack != null &&
                Fulfils(
                    action.BlockCondition,
                    RangedAttackOverlayQuery.Classify(
                        attack.AttackRange, attackerId, modifications))
                ? DefenseBlockResult.Blocked
                : DefenseBlockResult.ConditionUnfulfilled;
        }

        /// <summary>
        /// Milestone 8.40g. Whether any hero in play is wearing a marker the DEFENDING hero's own
        /// content put out.
        ///
        /// <para>The defender is the Defense offer's own actor, and "his own marker" is the
        /// attachment's owner, so this is <c>HeldMarkerQuery</c>'s actor-derived reading asked from a
        /// third site rather than a third reading of one printed clause - the same composition
        /// milestone 8.40c's predicate and its two selection narrowings take, which is what lets one
        /// board give all four of them the same answer.</para>
        /// </summary>
        private static bool FulfilsTheBoardCondition(
            ActionOffer defenseOffer, MarkerAttachmentRegister markers)
        {
            if (markers == null)
            {
                throw new InvalidOperationException(
                    "This Defense offer prints " +
                    $"'{PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker}', which is rulebook " +
                    "p.14's Block condition stated as a fact about the board, and this evaluation was " +
                    "given no marker register to read. Supply the match's register at the call site " +
                    "rather than letting the condition silently fail for a board nobody looked at: " +
                    "the defence preview and the Attack resolution both ask this, and a verdict one " +
                    "of them derived from an absent register is the disagreement milestone 8.11a " +
                    "exists to prevent.");
            }

            return HeldMarkerQuery
                .HoldersOfAMarkerDeclaredBy(markers.Attachments, defenseOffer.ActorId)
                .Count > 0;
        }

        /// <summary>
        /// Milestone 8.61b. Whether the mandatory swap this Block's own Defense text opens with could be
        /// performed - which is what "Swap with a [Smoke bomb] Smoke bomb token in range; if you do,
        /// block the attack" makes the Block condition.
        ///
        /// <para><b>The piece is read off the card's text and nowhere else.</b> The swap consumes one
        /// bounded owned-token fact, and that capability declares which token it is bound to and which
        /// printed icon it bounds the position by (<see cref="IOwnedTokenFactDeclaration"/>). Asking it
        /// here is what makes the preview, the resolution and the swap one answer: they are not three
        /// derivations that agree, they are one reading - <see cref="OwnedTokenReachQuery"/> - reached
        /// from three places.</para>
        ///
        /// <para><b>Every way of failing to find it is refused loudly rather than answered.</b> A
        /// composition that prints this condition and no Defense text, or a text naming no bounded fact,
        /// or two of them, is a card nobody can resolve - and a silent "not in range" would grey a
        /// defence that in fact holds, which is milestone 8.40c's rule and the reason the marker arm
        /// below throws for an absent register.</para>
        /// </summary>
        private static bool FulfilsTheSwapsOwnPrecondition(
            ActionOffer defenseOffer,
            ActionDefinition action,
            IReadOnlyList<CardDefinition> definitions,
            ITokenPlacementAuthority tokens,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications,
            EffectCapabilityCatalog capabilities)
        {
            if (tokens == null)
            {
                throw new InvalidOperationException(Refusal(
                    "this evaluation was given no board to read the piece from"));
            }

            if (capabilities == null)
            {
                throw new InvalidOperationException(Refusal(
                    "this evaluation was given no capability catalog, so nothing could say which token " +
                    "that card text swaps with"));
            }

            var defenseText = action.CardTextAt(CardTextTiming.DefenseAction);
            if (defenseText == null)
            {
                throw new InvalidOperationException(Refusal(
                    "the action prints no Defense-action card text at all, so there is no swap for the " +
                    "condition to be the precondition of"));
            }

            var declarations = defenseText.Nodes
                .Where(node => node.Capability.IsDefined)
                .Select(node =>
                    capabilities.TryLoad(node.Capability, out var handler)
                        ? handler as IOwnedTokenFactDeclaration
                        : null)
                .Where(declaration => declaration != null &&
                    !declaration.PublishesPresenceOnly &&
                    declaration.ReachChannel == PrintedValueChannel.Range)
                .ToArray();
            if (declarations.Length != 1)
            {
                throw new InvalidOperationException(Refusal(
                    $"its Defense text names {declarations.Length} capabilities publishing an owned " +
                    "token's position bounded by the card's printed Range, and this condition is that " +
                    "one fact restated - one names the piece, none or several name nothing"));
            }

            return OwnedTokenReachQuery.TryFindOwnedToken(
                declarations[0].TokenDefinition,
                PrintedValueChannel.Range,
                defenseOffer.ActorId,
                tokens,
                defenseOffer.SourceCardInstanceId,
                defenseOffer.SourceDefinitionReference,
                defenseOffer.ActionDefinitionId,
                definitions,
                heroItems,
                modifications,
                out _);
        }

        private static string Refusal(string cause) =>
            "This Defense offer prints " +
            $"'{PrintedBlockCondition.TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange}', " +
            "which is rulebook p.14's Block condition stated as the precondition of the swap the card's " +
            $"own Defense text performs, and {cause}. Answering it anyway would grey a defence that in " +
            "fact holds, or promise one the swap cannot perform: the defence preview and the Attack " +
            "resolution both ask this, and milestone 8.11a exists to keep them one verdict.";

        /// <summary>
        /// Whether one printed Block condition is fulfilled by one attack's printed classification.
        ///
        /// <para>Both arms are stated rather than one tested and the other left to fall through, for
        /// the reason <c>UltimateTriggeredEffectSourceQuery.Matches</c> states both of its: p.13
        /// classifies every printed Attack as Ranged or non-Ranged, so an action carrying neither is
        /// not an Attack at all and fulfils neither condition. A negation would have made such an
        /// action fulfil the non-Ranged arm, which is a rule no card prints.</para>
        /// </summary>
        private static bool Fulfils(PrintedBlockCondition condition, AttackRangeKind attackRange)
        {
            switch (condition)
            {
                case PrintedBlockCondition.AttackIsRanged:
                    return attackRange == AttackRangeKind.Ranged;
                case PrintedBlockCondition.AttackIsNotRanged:
                    return attackRange == AttackRangeKind.NonRanged;
                default:
                    // Neither board condition arrives here - milestone 8.40g's marker clause and
                    // milestone 8.61b's swap precondition are both answered above, from the board,
                    // before the attack is even named - so what is left is a Block carrying no
                    // condition at all, which the definition layer refuses to construct.
                    return false;
            }
        }

        /// <summary>
        /// Whether this offer is answered by a condition rather than by a number, which is what decides
        /// that no Defense value is calculated for it and no printed Defense icon is looked for.
        /// </summary>
        public static bool IsBlock(ActionOffer defenseOffer, IReadOnlyList<CardDefinition> definitions) =>
            definitions != null &&
            ActionOfferQuery.FindActionDefinition(defenseOffer, definitions).DefenseResolution ==
                PrintedDefenseResolution.Block;
    }
}
