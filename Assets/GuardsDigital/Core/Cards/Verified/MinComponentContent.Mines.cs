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
        /// same traversable property so a masked client walks exactly the routes the host walks.
        /// </summary>
        public static TokenDefinition MineFacedownDefinition { get; } = new TokenDefinition(
            MineFacedown,
            1,
            "Mine",
            MineSupplyCount,
            new[] { MayBeMovedThroughProperty() },
            TokenRemovalCondition.EndOfRoundCleanup,
            MineSupplyGroupId);

        /// <summary>
        /// The blast. Its kind is what the trap milestone 8.61e composes reads on removal; while it
        /// stands it is a traversable obstacle and nothing else, which is why its property list and the
        /// dud's are the same list.
        /// </summary>
        public static TokenDefinition MineBlastDefinition { get; } = Mine(MineBlast, "Mine (blast)");

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
        /// The three capabilities her mines run on, held OUT of production registration until milestone
        /// 8.61j deals her - the shape <c>BainComponentContent.DeferredHandlers</c> and
        /// <c>SyntheticPlacementCapabilities.DeferredHandlers</c> both keep.
        ///
        /// <para>All three run on handler TYPES this build already registers and showcases -
        /// <c>SyntheticTokenCapabilities</c>'s place and reclaim, registered at milestone 8.37 with the
        /// showcase instances milestone 7.9's gate asks for - so none of them owes a showcase consumer
        /// of its own at the dealing. What milestone 8.61d added to that type is an optional label on
        /// the minted instance ID, which is a parameter of an existing type and not a new one.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> MineDeferredHandlers() =>
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
        private static TokenDefinition Mine(TokenDefinitionId id, string displayName) =>
            new TokenDefinition(
                id,
                1,
                displayName,
                MineSupplyCount,
                new[] { MayBeMovedThroughProperty() },
                // p.19's baseline, and no mine face says otherwise: "Unless the card text says
                // otherwise, tokens are removed at the end of round." The trap removes one earlier;
                // that is an effect removing a token, not a lifetime the definition declares.
                TokenRemovalCondition.EndOfRoundCleanup,
                MineSupplyGroupId,
                MineFacedownDefinition);

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
    }
}
