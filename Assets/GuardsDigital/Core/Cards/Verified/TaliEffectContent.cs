using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Tali the Spiritcaller's printed card text, composed - milestone 8.35z32, phase one.
    ///
    /// <para><b>What phase one composes, and why it is the Green line.</b> Her five token faces are the
    /// hero's structural centre and the batch's largest single piece: nothing in the verified roster had
    /// tokens before her, and everything her Ice and Totem cards do is either a <b>placement</b> or a
    /// <b>property the placed piece carries</b>. Both halves are content here -
    /// <see cref="TaliComponentContent"/> owns the pieces, this file owns the placements - so all five
    /// Green faces are composed end to end, including p.18's short-supply step over the one shared
    /// supply RULING 44 gives her.</para>
    ///
    /// <para><b>What phase one defers, named face by face rather than left to a reader to notice.</b>
    /// Thirteen faces print text that needs vocabulary this milestone does not build, and each one is
    /// listed in <see cref="CompositionOwed"/> with the exact missing piece. Their card definitions
    /// carry <see cref="PrintedTextComposition.DeferredUntil"/> and their printed values, words, colours
    /// and upgrade pairs ship complete, so the deck is whole and playable as values while the text they
    /// print is honestly marked absent. The direction of every shortfall is the safe one: those cards do
    /// less than they print, never more - and the borrow contract reads the same marker, so Commune with
    /// Spirits could not reach a face whose text is not composed even once it exists.</para>
    ///
    /// <para><b>The one thing composed here that is not a node.</b> The totems' save and the Venerated
    /// totem's immunity are token <em>properties</em>, not effect steps: p.19 gives a placed token its
    /// properties "until that token is removed", and the sentence is in the present tense about a state
    /// of the board rather than a step in an action. So the whole of both Totem faces' text is the
    /// placement node below plus the definition beside it, and nothing about the save is written as an
    /// effect. What is still owed for the save is not its declaration but its <em>consumers</em>; see
    /// <see cref="MandatoryPreventionWiringIsOwed"/>.</para>
    /// </summary>
    public static class TaliEffectContent
    {
        /// <summary>p.16's permanent ability, identified for the continuous source it becomes.</summary>
        public const string ReignOfWinterRuleId = "tali-reign-of-winter";

        /// <summary>The rule kind every reader of Reign of Winter matches on.</summary>
        public const string ReignOfWinterRuleKind = "TaliReignOfWinter";

        // -----------------------------------------------------------------------------------------
        // Capability identities. One placement and one p.18 short-supply handler per token role, on
        // the marker pattern milestone 8.23 established: the definition is bound to the handler, so a
        // payload can name a node but never a different token.
        // -----------------------------------------------------------------------------------------

        public static EffectCapabilityId PlaceIce { get; } = new EffectCapabilityId("tali-place-ice-token");

        public static EffectCapabilityId ReclaimForIce { get; } =
            new EffectCapabilityId("tali-reclaim-for-ice-token");

        public static EffectCapabilityId PlaceAncestralTotem { get; } =
            new EffectCapabilityId("tali-place-ancestral-totem");

        public static EffectCapabilityId ReclaimForAncestralTotem { get; } =
            new EffectCapabilityId("tali-reclaim-for-ancestral-totem");

        public static EffectCapabilityId PlaceVeneratedTotem { get; } =
            new EffectCapabilityId("tali-place-venerated-totem");

        public static EffectCapabilityId ReclaimForVeneratedTotem { get; } =
            new EffectCapabilityId("tali-reclaim-for-venerated-totem");

        public static EffectCapabilityReference PlaceIceV1 => new EffectCapabilityReference(PlaceIce, 1);

        public static EffectCapabilityReference ReclaimForIceV1 =>
            new EffectCapabilityReference(ReclaimForIce, 1);

        public static EffectCapabilityReference PlaceAncestralTotemV1 =>
            new EffectCapabilityReference(PlaceAncestralTotem, 1);

        public static EffectCapabilityReference ReclaimForAncestralTotemV1 =>
            new EffectCapabilityReference(ReclaimForAncestralTotem, 1);

        public static EffectCapabilityReference PlaceVeneratedTotemV1 =>
            new EffectCapabilityReference(PlaceVeneratedTotem, 1);

        public static EffectCapabilityReference ReclaimForVeneratedTotemV1 =>
            new EffectCapabilityReference(ReclaimForVeneratedTotem, 1);

        /// <summary>
        /// Her six token handlers - a placement and a p.18 short-supply step for each of the three
        /// roles of her one pile.
        ///
        /// <para><b>Deferred, not registered</b>, on the standing roster law: the registered capability
        /// set is serialized into every snapshot, and no build deals her yet. The dealing change adds
        /// these to the registered list, and that is where the corpus digests move.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new[]
            {
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceIceV1, TaliComponentContent.IceDefinition, PrintedValueChannel.Radius),
                SyntheticTokenCapabilities.ReclaimHandler(
                    ReclaimForIceV1, TaliComponentContent.IceDefinition),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceAncestralTotemV1, TaliComponentContent.AncestralTotemDefinition,
                    PrintedValueChannel.Range),
                SyntheticTokenCapabilities.ReclaimHandler(
                    ReclaimForAncestralTotemV1, TaliComponentContent.AncestralTotemDefinition),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceVeneratedTotemV1, TaliComponentContent.VeneratedTotemDefinition,
                    PrintedValueChannel.Range),
                SyntheticTokenCapabilities.ReclaimHandler(
                    ReclaimForVeneratedTotemV1, TaliComponentContent.VeneratedTotemDefinition)
            };

        /// <summary>
        /// The faces whose printed text phase one does not compose, each with the vocabulary it waits
        /// on. Stated as content so a reader of the definitions finds it without reading a note, and
        /// asserted against the definitions' own deferral markers by this milestone's tests.
        ///
        /// <para>Every entry is a missing <em>engine</em> piece rather than an unanswered rules
        /// question: the rulings that decide what each face means are all in hand, and each row below
        /// names what would have to exist for the composition to be written.</para>
        /// </summary>
        public static IReadOnlyDictionary<CardDefinitionId, string> CompositionOwed { get; } =
            new Dictionary<CardDefinitionId, string>
            {
                // p.20's Handicap card, parked by the standing 2026-08-21 decision rather than by any
                // missing vocabulary - p.6 returns it to the box and the six-player pitch never brings
                // it back.
                [TaliCardContent.IceShard] = "first odd-player-count configuration",

                // The colour a defender discarded at p.14 step 3, carried to step 7. Nothing in this
                // engine captures it: EnclosingAttackFacts holds the target, the range and the space,
                // and the defence's discarded card is not among them. The costed shape is 8.13's
                // was-adjacent snapshot exactly - a fact produced where step 3 already knows the
                // answer, re-derived on restore, and read by a colour-restricted discard node.
                [TaliCardContent.IceBlast] =
                    "the defender's discarded colour captured at p.14 step 3 and carried to step 7",

                // The naming step. The borrow contract is built and its FromDiscardedCards(colour)
                // surface is exactly right, but its colour is a composition-time literal and hers is
                // named at runtime by an ally. Needs one capability that publishes a CardColor chosen
                // by a bound non-actor hero, which the retrieve family's runtime-chooser binding
                // already shows how to write; the borrow node then binds its colour parameter
                // FromNodeOutput instead of FromClosedVocabulary.
                [TaliCardContent.CommuneWithSpirits] =
                    "a runtime colour-naming capability whose chooser is a bound friendly hero",

                // A condition a card asks about ITSELF. The three Winter Attacks and both Spirit
                // Attacks read their own lifecycle state, which no predicate capability answers.
                [TaliCardContent.WinterDagger] = "a predicate over the source card's own discard state",
                [TaliCardContent.WinterSpear] = "a predicate over the source card's own discard state",
                [TaliCardContent.WinterScepter] = "a predicate over the source card's own discard state",
                [TaliCardContent.SpiritWolf] = "a predicate over the source card's own discard state",
                [TaliCardContent.SpiritBear] = "a predicate over the source card's own discard state",

                // The free direction and the simultaneous batch move. p.18's push already resolves a
                // multi-unit displacement against one pre-effect state, which is the simultaneity
                // RULING 3 (Arty) requires - "Since it's the same clause, it happens at the same time" -
                // so the batch half is reuse. What is new is that the direction is an unconstrained
                // actor choice among p.17's six, not one derived from a pusher and a target.
                [TaliCardContent.ColdSnap] =
                    "a free six-way direction choice and a simultaneous batch direct Move",
                [TaliCardContent.Snowstorm] =
                    "a free six-way direction choice and a simultaneous batch direct Move",
                [TaliCardContent.Blizzard] =
                    "a free six-way direction choice, a simultaneous batch direct Move, and the " +
                    "End-of-turn repeat over both",

                // retrieve-card@1 exists and Silverarrow's Nature's Blessing is the template for the
                // first sentence. The second sentence is the self-referential discard predicate the
                // Winter cards wait on, so both faces wait on the same one piece.
                [TaliCardContent.GuardianSpirit] =
                    "a predicate over the source card's own discard state (the retrieval half reuses " +
                    "retrieve-card@1)",
                [TaliCardContent.WarriorSpirit] =
                    "a predicate over the source card's own discard state (the retrieval half reuses " +
                    "retrieve-card@1)",

                // The Ultimate. Its trigger is a completed basic Attack THAT DEFEATED A MINION, which
                // is a third UltimateTriggerCondition member beside Wasp's basic Skill and Tigerclaw's
                // basic action - and unlike those two it reads a consequence of the action rather than
                // its kind. AttackActionResolutionResult already carries a MinionDefeatResolution, so
                // the fact exists; what is missing is the enum member, its reader, and the
                // colour-of-your-choice discard the trigger runs.
                [TaliCardContent.ReignOfWinter] =
                    "an after-basic-Attack-that-defeated-a-minion trigger condition and its " +
                    "colour-of-your-choice discard"
            };

        /// <summary>
        /// The one part of the Green line that is declared but not yet consumed, stated out loud so it
        /// cannot be mistaken for finished work.
        ///
        /// <para><b>What is done.</b> Both Totem definitions declare the save as a
        /// <see cref="Rules.Modifications.RuleModificationKind.RemovalPrevention"/> whose price is
        /// <see cref="Rules.Modifications.RemovalPreventionCost.RemoveTheSourceToken"/>;
        /// <c>RemovalPreventionQuery.Automatic</c> reads them back for a subject, in the set's canonical
        /// order, and <c>Offered</c> now refuses to swallow them into the card-payment family it was
        /// written for. All of that is measured by this milestone's tests.</para>
        ///
        /// <para><b>What is owed.</b> The three call sites that ask <c>OfferedPreventions</c> -
        /// <c>AttackDefenseCardActionStrategyBackend</c> twice and
        /// <c>CardActionResolutionAuthority</c> once - must ask <c>Automatic</c> first and apply it
        /// without opening a pause: a mandatory prevention has no decision to make, so it belongs
        /// before the decision window rather than inside it. Until that wiring lands, a totem does not
        /// actually save an adjacent minion, and the shortfall is in the safe direction - the card does
        /// less than it prints.</para>
        /// </summary>
        public const string MandatoryPreventionWiringIsOwed =
            "The three OfferedPreventions call sites must ask RemovalPreventionQuery.Automatic first " +
            "and apply it without a pause; until then a totem declares its save but does not perform it.";

        // -----------------------------------------------------------------------------------------
        // Attack text. None this phase; every Attack she prints waits on the self-referential discard
        // predicate or on the step-3 colour capture, both named in CompositionOwed.
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision) => null;

        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId) => CardTextTiming.None;

        public static EffectDefinition SecondAttackTextFor(
            CardDefinitionId cardId, string contentRevision) => null;

        public static CardTextTiming SecondAttackTextTimingFor(CardDefinitionId cardId) =>
            CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // Skill text. The five Green token faces, composed; every other Skill waits.
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(TaliCardContent.GlacialBarrier))
            {
                return CreateTokenPlacements(cardId, contentRevision, PlaceIceV1, ReclaimForIceV1, 1, false);
            }

            if (cardId.Equals(TaliCardContent.WallOfFrost))
            {
                return CreateTokenPlacements(cardId, contentRevision, PlaceIceV1, ReclaimForIceV1, 2, true);
            }

            if (cardId.Equals(TaliCardContent.PackIce))
            {
                return CreateTokenPlacements(cardId, contentRevision, PlaceIceV1, ReclaimForIceV1, 3, true);
            }

            if (cardId.Equals(TaliCardContent.AncestralTotem))
            {
                return CreateTokenPlacements(
                    cardId, contentRevision, PlaceAncestralTotemV1, ReclaimForAncestralTotemV1, 1, false);
            }

            if (cardId.Equals(TaliCardContent.VeneratedTotem))
            {
                return CreateTokenPlacements(
                    cardId, contentRevision, PlaceVeneratedTotemV1, ReclaimForVeneratedTotemV1, 1, false);
            }

            return null;
        }

        /// <summary>
        /// p.16's permanent ability for Reign of Winter, carrying its printed Radius and an explicit
        /// debt rather than a stand-in rule.
        ///
        /// <para>This is the same shape Wasp's High Voltage carried between milestones 7.3 and 8.13,
        /// and for the same reason: the ability is a <em>triggered</em> one and none of
        /// <see cref="Rules.Modifications.RuleModification"/>'s families can express a trigger. The
        /// card's printed Radius 4 ships here, which is what ledger question 12's approved reading
        /// needs in order for a +1 Radius item to make it 5.</para>
        /// </summary>
        public static UltimateRuleDefinition ReignOfWinterRule(string contentRevision) =>
            UltimateRuleDefinition.AwaitingComposition(
                ReignOfWinterRuleId,
                ReignOfWinterRuleKind,
                PrintedTextComposition.DeferredUntil(
                    CompositionOwed[TaliCardContent.ReignOfWinter]),
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) });

        /// <summary>
        /// "Place [an / up to 2 / up to 3] token(s) in [radius / range]" - the whole printed text of
        /// all five Green faces, because everything else those cards say is a property the placed piece
        /// carries rather than a step this action performs.
        ///
        /// <para><b>Two nodes per placement, in p.18's own order.</b> The page puts the short-supply
        /// step first - "When a card tells you to place a token, take it from the supply first. If you
        /// need to place more tokens than are available in the supply, first remove up to that number
        /// of tokens of the same type from the board, if able" - so each placement is a reclamation
        /// followed by a placement. The reclamation declines when the supply already covers the
        /// placement, and its <c>if able</c> policy is the page's own closing words.</para>
        ///
        /// <para><b>"Up to" is optionality per copy, and that is the printed reading.</b> p.10 lists
        /// "up to" among its three non-mandatory softeners, and Wall of Frost and Pack Ice each name one
        /// number for a placement that may be taken fewer times - so each copy after the first hangs off
        /// its own <see cref="EffectNodeKind.Optional"/>. Glacial Barrier and both Totem faces print no
        /// softener at all, so their single placement is mandatory and a board with nowhere to put a
        /// token stops the card at that step per p.10.</para>
        /// </summary>
        private static EffectDefinition CreateTokenPlacements(
            CardDefinitionId cardId,
            string contentRevision,
            EffectCapabilityReference place,
            EffectCapabilityReference reclaim,
            int count,
            bool upTo)
        {
            var root = new EffectNodeId("place-root");
            var nodes = new List<EffectNodeDefinition>();
            var rootChildren = new List<EffectNodeId>();

            for (var index = 0; index < count; index++)
            {
                var pair = new EffectNodeId($"place-pair-{index}");
                var reclaimNode = new EffectNodeId($"place-reclaim-{index}");
                var placeNode = new EffectNodeId($"place-token-{index}");

                nodes.Add(new EffectNodeDefinition(
                    pair, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { reclaimNode, placeNode }));
                nodes.Add(new EffectNodeDefinition(
                    reclaimNode, EffectNodeKind.Capability, EffectFailurePolicy.IfAble, reclaim,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    placeNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, place,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }));

                if (upTo)
                {
                    var optional = new EffectNodeId($"place-optional-{index}");
                    nodes.Add(new EffectNodeDefinition(
                        optional, EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                        childNodeIds: new[] { pair },
                        chooser: EffectChooserRole.Actor));
                    rootChildren.Add(optional);
                }
                else
                {
                    rootChildren.Add(pair);
                }
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: rootChildren));

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");
    }
}
