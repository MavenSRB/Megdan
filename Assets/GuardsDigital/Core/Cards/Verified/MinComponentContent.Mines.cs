using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's mines: two kinds drawn from one pile, placed facedown, and obstacles that
    /// units may walk over. Milestone 8.61d.
    ///
    /// <para><b>What the source says.</b> Three Blue faces print the same placement in the same words.
    /// Trip Mine: "Place 2 mine tokens, 1 [Mine] blast and 1 [Mine] dud, facedown in radius; units may
    /// move through them." Cluster Mine places "1 [Mine] blast and 2 [Mine] duds" and Mine Field "2
    /// [Mine] blasts and 1 [Mine] dud", each with the same two clauses after the semicolon. The trap
    /// those sentences end with - "After an enemy hero moves through a mine, remove it; for every blast
    /// removed, that hero discards a card, if able" - is milestone 8.61e's and is deliberately absent
    /// here; see <see cref="MinEffectContent"/>'s named seam.</para>
    ///
    /// <para><b>Three sentences, three pieces of vocabulary, and each is declared where it belongs.</b>
    /// "1 blast and 1 dud" is two <see cref="TokenDefinition"/>s naming one
    /// <see cref="TokenDefinition.SupplyGroupId"/> - Tali's RULING 44 shape, one pile whose role is
    /// decided by the placing card. "Units may move through them" is a token property addressed to
    /// <see cref="TokenPropertyAffectedObject.TheTokenItself"/>, carrying
    /// <see cref="RuleModificationKind.MayBeMovedThrough"/> - rulebook p.18's obstacle-side clause,
    /// which <c>ObstacleQuery</c> is the one reader of. "Facedown" is
    /// <see cref="TokenDefinition.FacedownFace"/>, a third definition that names no kind and which
    /// every seat but Min's is handed in place of the real one.</para>
    ///
    /// <para><b>The two kinds print identical properties, and that is load-bearing rather than a
    /// coincidence.</b> A blast and a dud differ only in what happens when one is REMOVED, which is a
    /// rule about the removal and not about the standing piece. So a masked board and the authority's
    /// own answer every movement, push, Place, Clear and end-of-round question identically, and the
    /// hiding can be a pure projection filter with no rules consequence at all.</para>
    ///
    /// <para><b>Nothing here is registered.</b> Milestone 8.61j deals her and moves the definitions and
    /// the handlers across in one change, exactly as milestone 8.37 did for Tali's tokens and 8.40j for
    /// Bain's marker: a match registers one list of what it can put on the board and the snapshot pins
    /// that list, so a component registered before a dealt card could place it moves pinned bytes for
    /// nothing. <see cref="MineDeferredHandlers"/> is that debt, written down.</para>
    /// </summary>
    public static partial class MinComponentContent
    {
        /// <summary>
        /// The applicability every token property of hers declares: p.19's own lifetime sentence,
        /// "Once a token is placed, it retains all its properties until that token is removed."
        /// </summary>
        public const string MineApplicabilityKind = "WhileTokenPlaced";

        /// <summary>
        /// MI-Q1's working number, held for the user: the box has two blasts and two duds, so the one
        /// pile is <b>four</b>.
        ///
        /// <para><b>Why the count is the pile's and not a kind's.</b> A shared supply is one physical
        /// stack and <c>ComponentDefinitionCatalog</c> refuses a group whose members disagree about how
        /// big it is - two numbers for one pile is exactly the drift that design exists to make
        /// impossible. So the number every member declares is four, and p.18's "remove up to that number
        /// of tokens of the same type from the board" reaches across blasts and duds alike, which is the
        /// corollary RULING 44 draws out loud for Tali and which applies here word for word.</para>
        ///
        /// <para><b>What that model cannot express, and why nothing needs it to.</b> One pile of four
        /// would also admit four blasts. No printed face asks for more than two of either kind - Mine
        /// Field's 2 + 1 and Cluster Mine's 1 + 2 are the maxima - so the widening is unreachable from
        /// content, and a per-kind cap would be a rule no card prints. If the user answers MI-Q1 with
        /// different numbers, this constant is the one place that changes.</para>
        /// </summary>
        public const int MineSupplyCount = 4;

        /// <summary>
        /// The one physical pile both kinds draw from - the identity that makes p.18's "tokens of the
        /// same type" reach a standing dud when a blast is being placed, and the reverse.
        /// </summary>
        public const string MineSupplyGroupId = "min-mine-tokens";

        /// <summary>
        /// What a mine placement writes into its minted instance ID where a placement normally writes
        /// the definition's own ID. Milestone 8.61d, and the reason it exists is the whole point of the
        /// facedown reading: <c>NetworkTokenRow</c> carries the instance ID verbatim, so an ID reading
        /// <c>...:min-mine-blast:...</c> would have handed every seat the kind the definition masking
        /// had just taken away. Both kinds mint the same label; the node ID the placement already
        /// carries is what keeps two mines of one card distinct.
        /// </summary>
        public const string MineInstanceIdentityLabel = "mine";

        /// <summary>The effect that prices a crossing which removed <paramref name="blasts"/> blasts.</summary>
        public static EffectDefinitionId MineTrapEffectId(int blasts) =>
            new EffectDefinitionId($"min-mine-trap-{blasts}");

        /// <summary>
        /// "for every blast removed, that hero discards a card, if able", as the blast declares it: one
        /// entry per number of blasts one crossing can take off the board at once, which the pile
        /// bounds at <see cref="MineSupplyCount"/>.
        ///
        /// <para>The reference names <see cref="MinCardContent.ContentRevision"/>, the one real string
        /// her whole set carries since milestone 8.61i - a reference whose revision disagreed with the
        /// definition's would be a link that stopped resolving, which is why the trap's own effects are
        /// authored at exactly this value rather than at a caller's.</para>
        /// </summary>
        public static IEnumerable<EffectDefinitionReference> MineTrapConsequences() =>
            Enumerable
                .Range(1, MineSupplyCount)
                .Select(blasts => new EffectDefinitionReference(
                    MineTrapEffectId(blasts), MinCardContent.ContentRevision))
                .ToArray();

        public static TokenDefinitionId MineBlast { get; } = new TokenDefinitionId("min-mine-blast");

        public static TokenDefinitionId MineDud { get; } = new TokenDefinitionId("min-mine-dud");

        /// <summary>
        /// The face every seat but Min's sees. It is a real registered definition rather than an absence
        /// because a client loads a token's definition by ID from the catalog, so the substituted value
        /// has to be something the catalog knows; it is placeable by nothing, because no handler is
        /// bound to it.
        /// </summary>
        public static TokenDefinitionId MineFacedown { get; } =
            new TokenDefinitionId("min-mine-facedown");

        /// <summary>
        /// The facedown face: "Mine", and not one word more. It shares the pile and the count so a seat
        /// counting what is left on a masked board gets the authority's own answer, and it carries the
        /// same two properties so a masked client walks exactly the routes the host walks and knows, as
        /// every seat may, that the piece goes off when an enemy hero walks over it.
        ///
        /// <para>What it does NOT carry is the price - <c>MoveThroughTrapConsequences</c> is empty here
        /// and non-empty on the blast, and that single difference is the whole of what "facedown"
        /// hides. Nothing reads it from this face: the trap is applied off the authority's own trusted
        /// definition, which is never the mask.</para>
        /// </summary>
        public static TokenDefinition MineFacedownDefinition { get; } = new TokenDefinition(
            MineFacedown,
            1,
            "Mine",
            MineSupplyCount,
            new[] { MayBeMovedThroughProperty(), RemovedByAnEnemyCrossingProperty() },
            TokenRemovalCondition.EndOfRoundCleanup,
            MineSupplyGroupId);

        /// <summary>
        /// The blast. It declares the same two properties every mine face does and one thing besides:
        /// what its removal costs the hero who walked over it. That is the only difference between the
        /// two kinds, which is exactly what the printed sentence says - "for every BLAST removed, that
        /// hero discards a card, if able" - and exactly what the facedown face takes away.
        /// </summary>
        public static TokenDefinition MineBlastDefinition { get; } =
            Mine(MineBlast, "Mine (blast)", MineTrapConsequences());

        /// <summary>The dud: the same piece, and the removal that costs the mover nothing.</summary>
        public static TokenDefinition MineDudDefinition { get; } = Mine(MineDud, "Mine (dud)");

        /// <summary>
        /// Every token definition her mines need, including the projection-only facedown face. It is a
        /// list for the fixtures and for milestone 8.61i's composition; nothing registers it yet.
        /// </summary>
        public static IEnumerable<TokenDefinition> MineTokens() => new[]
        {
            MineBlastDefinition, MineDudDefinition, MineFacedownDefinition
        };

        public static EffectCapabilityId PlaceMineBlast { get; } =
            new EffectCapabilityId("min-place-mine-blast");

        public static EffectCapabilityId PlaceMineDud { get; } =
            new EffectCapabilityId("min-place-mine-dud");

        /// <summary>
        /// p.18's short-supply step, and there is one of it rather than one per kind.
        ///
        /// <para><c>ComponentDefinitionCatalog.ReclaimableForPlacement</c> answers "tokens of the same
        /// type" over the supply GROUP for a definition that shares a pile, so a reclamation bound to
        /// the blast and one bound to the dud would offer the same tokens, decline in the same states
        /// and remove the same piece. One capability is therefore the honest shape: the binding names
        /// the pile, and the node that precedes every one of her placements is the same node.</para>
        /// </summary>
        public static EffectCapabilityId ReclaimMine { get; } =
            new EffectCapabilityId("min-reclaim-mine");

        public static EffectCapabilityReference PlaceMineBlastV1 =>
            new EffectCapabilityReference(PlaceMineBlast, 1);

        public static EffectCapabilityReference PlaceMineDudV1 =>
            new EffectCapabilityReference(PlaceMineDud, 1);

        public static EffectCapabilityReference ReclaimMineV1 =>
            new EffectCapabilityReference(ReclaimMine, 1);

        /// <summary>
        /// Nothing, from milestone 8.61j on - her three mine faces are cards a configured match now
        /// puts in a hand, so the three capabilities below left this list for
        /// <see cref="MineRegisteredHandlers"/> in that one edit. The member stays for the reason
        /// every emptied deferred list in this build stays: the fixtures that assert the gate from
        /// both sides ask a question rather than failing to compile.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> MineDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The three capabilities her mines run on, registered at milestone 8.61j - the shape
        /// <c>BainComponentContent</c> and <c>SyntheticPlacementCapabilities</c> both took at their
        /// own dealings.
        ///
        /// <para>All three run on handler TYPES this build already registers and showcases -
        /// <c>SyntheticTokenCapabilities</c>'s place and reclaim, registered at milestone 8.37 with the
        /// showcase instances milestone 7.9's gate asks for - so none of them owed a showcase consumer
        /// of its own at the dealing. What milestone 8.61d added to that type is an optional label on
        /// the minted instance ID, which is a parameter of an existing type and not a new one.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> MineRegisteredHandlers() =>
            new[]
            {
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceMineBlastV1, MineBlastDefinition, PrintedValueChannel.Radius,
                    MineInstanceIdentityLabel),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceMineDudV1, MineDudDefinition, PrintedValueChannel.Radius,
                    MineInstanceIdentityLabel),
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimMineV1, MineBlastDefinition)
            };

        /// <summary>
        /// One kind of mine: the pile, p.19's end-of-round removal, the one property both kinds print,
        /// and the facedown face that hides which kind this is from everyone but its controller.
        /// </summary>
        private static TokenDefinition Mine(
            TokenDefinitionId id,
            string displayName,
            IEnumerable<EffectDefinitionReference> trapConsequences = null) =>
            new TokenDefinition(
                id,
                1,
                displayName,
                MineSupplyCount,
                new[] { MayBeMovedThroughProperty(), RemovedByAnEnemyCrossingProperty() },
                // p.19's baseline, and no mine face says otherwise: "Unless the card text says
                // otherwise, tokens are removed at the end of round." The trap removes one earlier;
                // that is a rule this piece carries, not a lifetime the enum declares.
                TokenRemovalCondition.EndOfRoundCleanup,
                MineSupplyGroupId,
                MineFacedownDefinition,
                trapConsequences);

        /// <summary>
        /// "Units may move through them", as p.18's obstacle-side clause addressed to the piece that
        /// prints it. It says nothing about ending a move there, because the very next sentence of p.18
        /// is unconditional - "a unit can never end its movement on the same space as an obstacle, and
        /// any special effects of tokens are still applied" - and <c>ObstacleQuery</c> offers this
        /// declaration to the transit question alone.
        /// </summary>
        private static TokenPropertyDefinition MayBeMovedThroughProperty() =>
            new TokenPropertyDefinition(
                "units-may-move-through",
                "may-be-moved-through",
                MineApplicabilityKind,
                RuleModification.ForMayBeMovedThrough(),
                TokenPropertyAffectedObject.TheTokenItself);

        /// <summary>
        /// "After an enemy hero moves through a mine, remove it", as the piece's own rule. Milestone
        /// 8.61e.
        ///
        /// <para>It is declared by all three faces, the facedown one included, because it is public:
        /// p.4 hides which KIND a facedown mine is, and nothing about a mine standing there going off
        /// when an enemy walks over it. It says nothing about the price, because the price is the kind -
        /// that is <c>TokenDefinition.MoveThroughTrapConsequences</c>, which only the blast carries.
        /// </para>
        ///
        /// <para>It is a second property rather than a second clause of the first, because the two are
        /// asked at opposite moments: <c>ObstacleQuery</c> asks "may this be walked over" before
        /// anything moves, and the trap is asked only once a displacement has committed. p.18 prints
        /// them as two sentences and this file declares them as two.</para>
        /// </summary>
        private static TokenPropertyDefinition RemovedByAnEnemyCrossingProperty() =>
            new TokenPropertyDefinition(
                "removed-when-an-enemy-hero-moves-through",
                "removed-when-an-enemy-hero-moves-through",
                MineApplicabilityKind,
                RuleModification.ForRemovedWhenAnEnemyHeroMovesThrough(),
                TokenPropertyAffectedObject.TheTokenItself);
    }
}
