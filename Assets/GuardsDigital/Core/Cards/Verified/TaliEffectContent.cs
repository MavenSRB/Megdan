using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Tali the Spiritcaller's printed card text, composed - milestone 8.35z32's phase one built her
    /// five Green token faces end to end and named thirteen deferrals with their exact engine causes;
    /// milestone 8.37 composes twelve of those thirteen, having read the CURRENT engine
    /// rather than trusted phase one's own inventory of it - two of the twelve turned out to need less
    /// new engine than phase one costed (Ice Blast's debt was already paid by Whisper's own dealing;
    /// Reign of Winter's trigger needed exactly the three pieces phase one named and nothing more), and
    /// one predicate capability unlocked seven faces exactly as predicted.
    ///
    /// <para><b>The Green line, composed by phase one and unchanged here.</b> Her five token faces are
    /// the hero's structural centre and the batch's largest single piece: nothing in the verified roster
    /// had tokens before her, and everything her Ice and Totem cards do is either a <b>placement</b> or
    /// a <b>property the placed piece carries</b>. Both halves are content here -
    /// <see cref="TaliComponentContent"/> owns the pieces, this file owns the placements - so all five
    /// Green faces are composed end to end, including p.18's short-supply step over the one shared
    /// supply RULING 44 gives her. The totems' save and the Venerated totem's immunity are token
    /// <em>properties</em>, not effect steps - p.19 gives a placed token its properties "until that
    /// token is removed", present tense about a state of the board rather than a step in an action - and
    /// their consumers are wired now too; see <see cref="MandatoryPreventionWiringIsOwed"/>.</para>
    ///
    /// <para><b>What this milestone composes, face by face.</b> The self-referential discard predicate
    /// (<see cref="SyntheticConditionCapabilities.SourceCardInDiscardV1"/>) reads Winter Dagger, Winter
    /// Spear and Winter Scepter's dynamic +3 (a value contribution, never a node - see
    /// <see cref="RaisedAttackFor"/>), gates Spirit Wolf and Spirit Bear's printed-Range bullet
    /// (<see cref="CreateSpiritModal"/>), and gates Guardian Spirit and Warrior Spirit's self-retrieval
    /// clause (<see cref="CreateSpiritRetrieval"/>). Cold Snap and Snowstorm's free-direction batch Move
    /// is <see cref="CreateFreeDirectionBatchMove"/>. Commune with Spirits - the hero, and the reason
    /// eight of her nineteen faces read their own discard state - is
    /// <see cref="CreateCommuneWithSpirits"/>. Ice Blast's step-3-to-step-7 rider is
    /// <see cref="CreateIceBlastRider"/>. Reign of Winter's trigger is
    /// <see cref="ReignOfWinterTriggeredEffect"/>, run by a fourth
    /// <see cref="UltimateTriggerCondition"/> member this milestone adds.</para>
    ///
    /// <para><b>What still defers, and why it is one face rather than thirteen.</b>
    /// <see cref="CompositionOwed"/> now names two: p.20's Handicap card (parked by standing decision,
    /// unrelated to engine debt) and Blizzard, whose second, independent sentence - an End-of-turn
    /// repeat that re-invokes a whole primary action from a timing trigger - is a genuinely different
    /// mechanic from anything this build has, named rather than improvised. Every other face's printed
    /// values, words, colours and upgrade pairs ship complete regardless of composition state, so the
    /// deck is whole and playable as values throughout.</para>
    /// </summary>
    public static class TaliEffectContent
    {
        /// <summary>p.16's permanent ability, identified for the continuous source it becomes.</summary>
        public const string ReignOfWinterRuleId = "tali-reign-of-winter";

        /// <summary>The rule kind every reader of Reign of Winter matches on.</summary>
        public const string ReignOfWinterRuleKind = "TaliReignOfWinter";

        /// <summary>The triggered effect Reign of Winter's rule runs each time it fires.</summary>
        public static EffectDefinitionId ReignOfWinterTriggerId { get; } =
            new EffectDefinitionId("tali-reign-of-winter-trigger");

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
        /// <para><b>Registered at milestone 8.37</b>, the milestone that deals her. The standing
        /// roster law held them out of the serialized capability set while no build dealt her; her
        /// five Green faces are now cards a configured match puts in a hand, so both halves of
        /// milestone 7.9's gate are satisfied and this is where the corpus digests moved.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
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
        /// The faces whose printed text this build does not compose, each with the vocabulary it waits
        /// on. Stated as content so a reader of the definitions finds it without reading a note, and
        /// asserted against the definitions' own deferral markers by this milestone's tests.
        ///
        /// <para>Milestone 8.37 composed twelve of phase one's thirteen deferrals - the
        /// self-referential discard predicate unlocked seven of them exactly as phase one predicted, and
        /// probing the CURRENT engine (rather than trusting phase one's own inventory) found Ice Blast's
        /// costed debt already paid by Whisper's dealing and Reign of Winter's trigger buildable in one
        /// pass. What remains is genuinely narrower than what phase one named, and the two rows below
        /// say exactly what is left rather than repeating a debt that is now partly settled.</para>
        ///
        /// <para><b>Lazy, and that is load-bearing rather than a style choice.</b> This is the only
        /// member of this type whose INITIALIZER reaches <see cref="TaliCardContent"/>, and that class
        /// reaches back here for every composed face - a static-initialization cycle whose outcome
        /// depends on which of the two the CLR is asked for first. Milestone 8.37 entered it from this
        /// side for the first time, because <see cref="RegisteredHandlers"/> is now called from
        /// <c>MatchContentCatalogs</c>, and the run threw <see cref="NullReferenceException"/> out of
        /// a half-initialized capability id on 1,423 cases. Deferring the dictionary until something
        /// actually asks for it removes the edge entirely, in both directions.</para>
        /// </summary>
        public static IReadOnlyDictionary<CardDefinitionId, string> CompositionOwed =>
            LazyCompositionOwed.Value;

        private static readonly Lazy<IReadOnlyDictionary<CardDefinitionId, string>>
            LazyCompositionOwed = new Lazy<IReadOnlyDictionary<CardDefinitionId, string>>(() =>
            new Dictionary<CardDefinitionId, string>
            {
                // p.20's Handicap card, parked by the standing 2026-08-21 decision rather than by any
                // missing vocabulary - p.6 returns it to the box and the six-player pitch never brings
                // it back.
                [TaliCardContent.IceShard] = "first odd-player-count configuration",

                // Milestone 8.37. The card's FIRST sentence (the free direction, the
                // simultaneous batch Move) is composed and proven on Cold Snap and Snowstorm, which
                // print nothing else. Blizzard alone carries a second, independent clause - "If Blizzard
                // is in your discard, End of turn: May repeat once" - which asks the primary Skill to
                // run again from a TURN-ENDED trigger rather than from a card-text node inside its own
                // resolution. ActionDefinition refuses a definition carrying both a composed EffectRoot
                // and an awaited PrintedTextComposition, so the two sentences cannot ship separately:
                // until the repeat-from-trigger shape exists, the whole face stays deferred rather than
                // shipping its first sentence silently uncoupled from its second. Silverarrow's
                // End-of-turn discard (SilverarrowActiveEffectContent) proves the CHECKPOINT half of
                // this shape; what it does not prove is RE-INVOKING A FULL PRIMARY ACTION rather than
                // running a bounded effect at that checkpoint, which is the genuinely new piece.
                [TaliCardContent.Blizzard] =
                    "an End-of-turn repeat that re-invokes a full primary action from a timing " +
                    "trigger, distinct from Silverarrow's End-of-turn effect (which runs a bounded " +
                    "discard, not a repeated action)"
            });

        /// <summary>
        /// The one part of the Green line that is declared but not yet consumed, stated out loud so it
        /// cannot be mistaken for finished work when phase one wrote it and is kept, now updated, so a
        /// reader of the history sees the debt closed rather than silently dropped.
        ///
        /// <para><b>What is done, all of it now.</b> Both Totem definitions declare the save as a
        /// <see cref="Rules.Modifications.RuleModificationKind.RemovalPrevention"/> whose price is
        /// <see cref="Rules.Modifications.RemovalPreventionCost.RemoveTheSourceToken"/>;
        /// <c>RemovalPreventionQuery.Automatic</c> reads them back for a subject, in the set's canonical
        /// order; <c>Offered</c> refuses to swallow them into the card-payment family it was written
        /// for; and milestone 8.37 wired all three call sites phase one named -
        /// <c>AttackDefenseCardActionStrategyBackend.ResolveRemovalStage</c> asks
        /// <c>RemovalStageDecision.IsAutomaticallyPrevented</c> before ever offering a card-priced pause,
        /// <c>AnswerRemovalPrevention</c> ORs the same read into its own final decision, and
        /// <c>CardActionResolutionAuthority.DescribeRemovalPreventionOptions</c> needed no change because
        /// <c>Offered</c> already excludes a mandatory source structurally. A totem now actually saves an
        /// adjacent minion.</para>
        /// </summary>
        public const string MandatoryPreventionWiringIsOwed =
            "CLOSED at milestone 8.37: RemovalStageDecision.IsAutomaticallyPrevented is " +
            "asked ahead of the offered, card-priced family at all three OfferedPreventions call sites, " +
            "so a totem's mandatory save now actually stops the removal rather than merely declaring it.";

        // -----------------------------------------------------------------------------------------
        // Attack text. Milestone 8.37 composes Spirit Wolf and Spirit Bear's modal
        // (the self-referential predicate gates their printed-Range bullet); Ice Blast's rider is a
        // second body, below. Winter Dagger, Winter Spear and Winter Scepter print a plain "Target a
        // unit adjacent to you" with a dynamic +3 that is a value contribution and never a node - see
        // RaisedAttackFor, bound on TaliCardContent's own Attack action - so they compose nothing here,
        // exactly as Sabina's identically-shaped Quickdraw family composes nothing.
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(TaliCardContent.SpiritWolf))
            {
                return CreateSpiritModal(cardId, contentRevision, orBoth: false);
            }

            if (cardId.Equals(TaliCardContent.SpiritBear))
            {
                return CreateSpiritModal(cardId, contentRevision, orBoth: true);
            }

            // Ice Blast prints a second body (the After-the-attack rider below), and
            // ActionDefinition refuses one without an explicit first: "an action whose only text were
            // declared here would be invisible to every reader that asks about CardTextTiming." Its
            // OWN first sentence, "Target a unit adjacent to you.", is p.13's plain default reading and
            // asks nothing beyond it - so the explicit body is one harmless read
            // (SyntheticPlacementCapabilities.ActorSpaceV1, unconsumed) rather than a step that
            // pretends to do something the card does not print.
            if (cardId.Equals(TaliCardContent.IceBlast))
            {
                return CreateNoOpFirstBody(cardId, contentRevision);
            }

            return null;
        }

        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(TaliCardContent.SpiritWolf) || cardId.Equals(TaliCardContent.SpiritBear) ||
                cardId.Equals(TaliCardContent.IceBlast))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            return CardTextTiming.None;
        }

        /// <summary>
        /// A first body that changes nothing on the board and binds no target, for a face whose printed
        /// first sentence is p.13's own default reading and nothing more, but which prints a second body
        /// - see the caller. The one node reads the actor's own space and publishes it to nobody.
        /// </summary>
        private static EffectDefinition CreateNoOpFirstBody(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("no-op-first-body");
            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("space", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "+3 Attack if this card is not in the discard" - Winter Dagger, Winter Spear and Winter
        /// Scepter's whole printed clause beyond their targeting, as p.14's own dynamic-value pipeline
        /// rather than an effect node. T-11 settled the timing: the Attack Total is computed at p.14's
        /// worked moment (printed value + items + card text), which is exactly what
        /// <see cref="RosterDynamicValueQuery"/> already answers every "+" cue through - Sabina's
        /// identically-printed Quickdraw family is the direct template. Every other face returns none.
        /// </summary>
        public static IEnumerable<RosterValueContributionDefinition> RaisedAttackFor(CardDefinitionId cardId) =>
            cardId.Equals(TaliCardContent.WinterDagger) ||
            cardId.Equals(TaliCardContent.WinterSpear) ||
            cardId.Equals(TaliCardContent.WinterScepter)
                ? new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Attack, RosterValueQueryKind.SourceCardNotInDiscard, 3)
                }
                : Enumerable.Empty<RosterValueContributionDefinition>();

        /// <summary>
        /// "Choose one [, or both, on different targets] - • If this card is not in the discard, target
        /// a unit in range. • Target a hero adjacent to you." - Spirit Wolf (<paramref name="orBoth"/>
        /// false) and Spirit Bear (true), on the multi-sequence contract Dodger's Middlefinger of Death
        /// and Whisper's Swift Justice already prove.
        ///
        /// <para><b>The first bullet is gated by a Conditional wrapping its own leaf</b>, not by a
        /// subject narrowing: the printed condition is about the SOURCE CARD, not about which units
        /// qualify as targets, so <see cref="AttackSequenceSubjectNarrowing"/> - built for candidate-level
        /// questions like Middlefinger's "who has cards in the discard" - is the wrong shape for it.
        /// <see cref="SyntheticConditionCapabilities.SourceCardInDiscardV1"/> negated (bind
        /// <c>true</c>: publish "not in discard") is asked once per instance the modal is offered, which
        /// is exactly when the sentence needs it read - live, at the moment of offer, restore or
        /// answer.</para>
        ///
        /// <para><b>The Choose still sees a binding either way</b>, so the walk that judges whether this
        /// whole Attack can ever bind a target is unaffected by which branch the live predicate would
        /// take: the gated bullet's own attack-sequence leaf sits INSIDE the Conditional's then-branch,
        /// not after it as a Sequence sibling, so this composition does not reach the
        /// mandatory-Conditional-before-a-sibling-binding shape the Ursafar executor's queued
        /// <c>WalkToAttackTargetBinding</c> fix is for.</para>
        /// </summary>
        private static EffectDefinition CreateSpiritModal(
            CardDefinitionId cardId, string contentRevision, bool orBoth)
        {
            var modal = new EffectNodeId("spirit-modal");
            var gate = new EffectNodeId("spirit-not-in-discard-gate");
            var rangedBullet = new EffectNodeId("spirit-ranged-unit");
            var adjacentHero = new EffectNodeId("spirit-adjacent-hero");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate, adjacentHero },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: orBoth ? 2 : 1),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticConditionCapabilities.SourceCardInDiscardV1,
                        childNodeIds: new[] { rangedBullet },
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticConditionCapabilities.NegateParameter, EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConditionCapabilities.InDiscardOutput, EffectValueKind.Boolean)
                        }),

                    // "target a unit in range" - the printed Range icon, both unit kinds.
                    SpiritModalBulletNode(
                        rangedBullet, AttackTargetableUnits.HeroesAndMinions,
                        AttackTargetReach.PrintedRange, orBoth),

                    // "Target a hero adjacent to you." - p.19's innermost ring, heroes only.
                    SpiritModalBulletNode(
                        adjacentHero, AttackTargetableUnits.HeroesOnly,
                        AttackTargetReach.AdjacentOnly, orBoth)
                });
        }

        /// <summary>One bullet of Spirit Wolf/Spirit Bear's modal, as an <c>attack-sequence@1</c> leaf.</summary>
        private static EffectNodeDefinition SpiritModalBulletNode(
            EffectNodeId nodeId,
            AttackTargetableUnits targetableUnits,
            AttackTargetReach reach,
            bool distinctTargets) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSequenceCapabilities.AttackSequenceV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticSequenceCapabilities.TargetableUnitsParameter, targetableUnits),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticSequenceCapabilities.ReachParameter, reach),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticSequenceCapabilities.LineParameter, AttackTargetLine.AnyLine),

                    // Spirit Bear prints "(on different targets)"; Spirit Wolf's plain "Choose one"
                    // never lets both fire in the same action, so the flag is moot but stated honestly.
                    EffectInputBinding.FromLiteral(
                        SyntheticSequenceCapabilities.DistinctTargetsParameter,
                        EffectValue.Boolean(distinctTargets))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticSequenceCapabilities.TargetableUnitsOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(
                        SyntheticSequenceCapabilities.ReachOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(
                        SyntheticSequenceCapabilities.LineOutput, EffectValueKind.Integer),
                    new EffectOutputBinding(
                        SyntheticSequenceCapabilities.DistinctTargetsOutput, EffectValueKind.Boolean)
                });

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");

        /// <summary>
        /// Ice Blast's whole "After the attack:" rider - the only second body she prints. See
        /// <see cref="SyntheticRosterCapabilities.DefendedCardColorV1"/>'s own remarks for the finding
        /// that phase one's costed engine debt here does not exist on this base: Whisper's own dealing
        /// already carries the fact this rider reads.
        /// </summary>
        public static EffectDefinition SecondAttackTextFor(CardDefinitionId cardId, string contentRevision) =>
            cardId.Equals(TaliCardContent.IceBlast)
                ? CreateIceBlastRider(cardId, contentRevision)
                : null;

        public static CardTextTiming SecondAttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(TaliCardContent.IceBlast) ? CardTextTiming.AfterTheAttack : CardTextTiming.None;

        /// <summary>
        /// "If the target discards a card to defend, After the attack: Another enemy hero in radius
        /// discards a card of the same color, if able."
        ///
        /// <para>The whole rider is gated behind one Conditional reading whether the target actually
        /// defended with a card - <see cref="SyntheticRosterCapabilities.DefendedCardColorV1"/>'s
        /// <c>defended</c> output - so a target who declined to defend, held nothing, or was removed
        /// before step 4 leaves this rider applying nothing, which is the printed "If" rather than a
        /// separate "if able".</para>
        ///
        /// <para>Inside the gate: the printed Radius disk (p.16's items composed on, exactly as every
        /// other Radius selection in the build), "ANOTHER enemy hero" (excluding the Attack's own
        /// target, who already defended), and the discard restricted to the colour the gate itself
        /// published - one node answering both the branch and the fact the branch's content needs.</para>
        /// </summary>
        private static EffectDefinition CreateIceBlastRider(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("ice-blast-root");
            var gate = new EffectNodeId("ice-blast-defended-gate");
            var radius = new EffectNodeId("ice-blast-radius");
            var select = new EffectNodeId("ice-blast-select-hero");
            var discard = new EffectNodeId("ice-blast-discard");

            return new EffectDefinition(
                SecondAttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.DefendedCardColorV1,
                        childNodeIds: new[] { radius },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.DefendedOutput, EffectValueKind.Boolean),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ColorOutput, EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        radius, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { RiderRadiusNodeId(cardId), select, discard }),
                    new EffectNodeDefinition(
                        RiderRadiusNodeId(cardId), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticValueCapabilities.CardRadiusV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticRosterCapabilities.DistanceParameter,
                                RiderRadiusNodeId(cardId), SyntheticValueCapabilities.RadiusOutput,
                                EffectValueKind.Integer),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.ExcludeTargetParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter,
                                select, SyntheticRosterCapabilities.ChosenHeroOutput,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.ColorParameter,
                                gate, SyntheticRosterCapabilities.ColorOutput,
                                EffectValueKind.Integer, EffectIntegerDomain.All<CardColor>())
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                        })
                });
        }

        private static EffectNodeId RiderRadiusNodeId(CardDefinitionId cardId) =>
            new EffectNodeId(cardId.Value + "-rider-radius");

        private static EffectDefinitionId SecondAttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-second-attack-text");

        // -----------------------------------------------------------------------------------------
        // Skill text. The five Green token faces from phase one, plus this milestone's Blue
        // free-direction faces, Commune with Spirits, and the two Spirit-retrieval faces. Blizzard
        // stays deferred - see CompositionOwed's narrowed reason.
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

            if (cardId.Equals(TaliCardContent.ColdSnap) || cardId.Equals(TaliCardContent.Snowstorm))
            {
                return CreateFreeDirectionBatchMove(cardId, contentRevision);
            }

            if (cardId.Equals(TaliCardContent.CommuneWithSpirits))
            {
                return CreateCommuneWithSpirits(cardId, contentRevision);
            }

            if (cardId.Equals(TaliCardContent.GuardianSpirit))
            {
                return CreateSpiritRetrieval(cardId, contentRevision);
            }

            if (cardId.Equals(TaliCardContent.WarriorSpirit))
            {
                return CreateSpiritRetrieval(cardId, contentRevision);
            }

            return null;
        }

        /// <summary>
        /// "Choose a direction. Move each enemy unit in radius 1 space in that direction, if able." -
        /// Cold Snap and Snowstorm's whole printed text, and word-for-word Blizzard's first sentence
        /// too. Blizzard itself stays deferred as a whole face rather than reusing this helper - see
        /// <see cref="CompositionOwed"/>'s own entry for why its second, independent sentence forces
        /// the whole card to wait: <see cref="ActionDefinition"/> refuses a definition that carries both
        /// a composed <see cref="EffectDefinition"/> and an awaited <see cref="PrintedTextComposition"/>
        /// at once, so a face whose second sentence still needs vocabulary this milestone does not
        /// build cannot ship its first sentence alone.
        ///
        /// <para><b>The radius is item-scaled</b>, through <see cref="SyntheticValueCapabilities.CardRadiusV1"/>
        /// exactly as every other printed Radius disk in the build - both faces print the icon and
        /// nothing about a direction-and-move sentence exempts it from p.16.</para>
        ///
        /// <para><b>The batch is one mandatory node</b>, never a decision: the sentence names "each"
        /// enemy unit, not "up to" a chosen number, so nobody is asked which units - see
        /// <see cref="SyntheticDisplacementCapabilities.BatchDirectMoveV1"/>'s own remarks for the
        /// simultaneity this reuses from p.18's push.</para>
        /// </summary>
        private static EffectDefinition CreateFreeDirectionBatchMove(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("batch-move-root");
            var chooseDirection = new EffectNodeId("batch-move-direction");
            var radius = new EffectNodeId("batch-move-radius");
            var move = new EffectNodeId("batch-move-batch");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { chooseDirection, radius, move }),
                    new EffectNodeDefinition(
                        chooseDirection, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.ChooseDirectionV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.ChosenDirectionOutput,
                                EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        radius, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticValueCapabilities.CardRadiusV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.BatchDirectMoveV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.BatchRadiusParameterName,
                                radius, SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.BatchDirectionParameterName,
                                chooseDirection, SyntheticDisplacementCapabilities.ChosenDirectionOutput,
                                EffectValueKind.Integer,
                                EffectIntegerDomain.All<Board.HexDirection>())
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.BatchMovedCountOutput,
                                EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "A friendly hero in play names a color; you may discard a card of that color. Perform the
        /// primary action on a card of that color in your discard." - Commune with Spirits, the hero.
        ///
        /// <para>RULING 20 (a friendly hero must be ON THE BOARD), RULING 21 (all six colours nameable,
        /// Purple inert), Arty's 8/15 ruling (naming is mandatory even with an empty hand; the action
        /// binds to the colour) and RULING 22 (the borrow is a full action instance) are all structural
        /// here rather than separately checked: the selection, the naming and the borrow contract each
        /// already carry their own half of it.</para>
        ///
        /// <para><b>Three steps, one colour threaded through all of them.</b>
        /// <see cref="SyntheticBorrowedActionCapabilities.FriendlyHeroInPlayV1"/> names WHO names the
        /// colour; <see cref="SyntheticBorrowedActionCapabilities.NameColorV1"/> directs that naming to
        /// them; the optional discard and the mandatory borrow both bind the SAME published colour, so
        /// "of that color" cannot read two different answers.</para>
        /// </summary>
        private static EffectDefinition CreateCommuneWithSpirits(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("commune-root");
            var selectFriendly = new EffectNodeId("commune-select-friendly");
            var nameColor = new EffectNodeId("commune-name-color");
            var optionalDiscard = new EffectNodeId("commune-optional-discard");
            var discard = new EffectNodeId("commune-discard");
            var borrow = new EffectNodeId("commune-borrow");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { selectFriendly, nameColor, optionalDiscard, borrow }),
                    new EffectNodeDefinition(
                        selectFriendly, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedActionCapabilities.FriendlyHeroInPlayV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticBorrowedActionCapabilities.ChosenFriendlyHeroOutput,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        nameColor, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedActionCapabilities.NameColorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticBorrowedActionCapabilities.NameColorChooserParameter,
                                selectFriendly, SyntheticBorrowedActionCapabilities.ChosenFriendlyHeroOutput,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticBorrowedActionCapabilities.NamedColorOutput, EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        optionalDiscard, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { discard },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                SyntheticCardStateCapabilities.HeroParameter,
                                EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.ColorParameter,
                                nameColor, SyntheticBorrowedActionCapabilities.NamedColorOutput,
                                EffectValueKind.Integer, EffectIntegerDomain.All<CardColor>())
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                        }),
                    new EffectNodeDefinition(
                        borrow, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticBorrowedActionCapabilities.PoolParameter,
                                BorrowedActionPool.OwnedDiscardedCards),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticBorrowedActionCapabilities.ColorParameter,
                                nameColor, SyntheticBorrowedActionCapabilities.NamedColorOutput,
                                EffectValueKind.Integer, EffectIntegerDomain.All<CardColor>())
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// "Up to 1 friendly hero in radius may retrieve a discarded card. If [this card] is in your
        /// discard, retrieve it." - Guardian Spirit and Warrior Spirit, identical in shape and differing
        /// only in which card names itself.
        ///
        /// <para><b>The first sentence is Silverarrow's Blessing template, narrowed to friendly and
        /// stripped of its reward</b> - neither face prints a coin grant, and "up to 1" against a single
        /// candidate role is the same observable outcome as Blessing's own "a hero ... may": Tali picks
        /// at most one candidate (there being only one role to fill), and that hero's own "may" is
        /// theirs to decline.</para>
        ///
        /// <para><b>The second sentence is unconditional self-retrieval, gated by the source card's own
        /// discard state</b> (un-negated: "IS in the discard"), and it names no choice at all -
        /// <see cref="SyntheticRosterCapabilities.RestrictToCardParameter"/> bound from
        /// <see cref="EffectRoleKind.SourceCard"/> is what lets <c>retrieve-card@1</c> answer "retrieve
        /// IT" without opening a one-candidate prompt.</para>
        /// </summary>
        private static EffectDefinition CreateSpiritRetrieval(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("spirit-retrieval-root");
            var select = new EffectNodeId("spirit-retrieval-select");
            var retrieveChosen = new EffectNodeId("spirit-retrieval-retrieve-chosen");
            var selfGate = new EffectNodeId("spirit-retrieval-self-gate");
            var retrieveSelf = new EffectNodeId("spirit-retrieval-retrieve-self");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    // The retrieval is a SIBLING of the selection, not merely its declared consumer.
                    // Naming a node in `ConsumerParameterName` says which step the selection is FOR;
                    // it does not put that step in the graph. Left out of this sequence,
                    // `retrieveChosen` is unreachable from the root and reads a hero from a node the
                    // validator cannot prove ran first - both of which it says out loud, and neither
                    // of which any offline tier below `MatchConfiguration` can see.
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, retrieveChosen, selfGate }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(retrieveChosen))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        retrieveChosen, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "owner", select, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                "chooser", select, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.RetrievedByOutputName, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        selfGate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticConditionCapabilities.SourceCardInDiscardV1,
                        childNodeIds: new[] { retrieveSelf },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConditionCapabilities.InDiscardOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        retrieveSelf, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind", RosterCardRetrievalKind.OwnedDiscardedCard),
                            EffectInputBinding.FromRole(
                                SyntheticRosterCapabilities.RestrictToCardParameter,
                                EffectRoleKind.SourceCard, EffectValueKind.CardInstance)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// p.16's permanent ability for Reign of Winter, carrying its printed Radius 4 - which is what
        /// ledger question 12's approved reading needs in order for a +1 Radius item to make it 5, via
        /// the same <see cref="SyntheticTargetingCapabilities.RadiusHeroTargetV1"/> item composition
        /// Wasp's High Voltage and Misa's own Ultimate already prove.
        ///
        /// <para>Milestone 8.37 composes what phase one left explicit debt for: "an
        /// after-basic-Attack-that-defeated-a-minion trigger condition and its colour-of-your-choice
        /// discard". The trigger condition is
        /// <see cref="UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack"/>, new at
        /// this milestone for the reason its own remarks give; the discard is
        /// <see cref="ReignOfWinterTriggeredEffect"/>.</para>
        /// </summary>
        public static UltimateRuleDefinition ReignOfWinterRule(string contentRevision) =>
            UltimateRuleDefinition.TriggeredAbility(
                ReignOfWinterRuleId,
                ReignOfWinterRuleKind,
                ReignOfWinterTriggeredEffect(contentRevision).Reference,
                UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack,
                new[] { TimingCheckpoint.ActionEnded },
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) });

        /// <summary>
        /// "an enemy hero in radius discards a card of the color of your choice, if able" - Reign of
        /// Winter's whole triggered clause, run once for every basic Attack of Tali's that defeats a
        /// minion.
        ///
        /// <para><b>The selection is p.16's own item-scaled Radius</b>, read through
        /// <see cref="SyntheticTargetingCapabilities.RadiusHeroTargetV1"/> exactly as Wasp's High
        /// Voltage and Misa's own Ultimate read theirs, so a +1 Radius item raises this trigger's reach
        /// the identical way it raises every other printed Radius disk in the build - ledger question
        /// 12's reading, wired rather than merely approved.</para>
        ///
        /// <para><b>Both the selection and the discard are <see cref="EffectFailurePolicy.IfAble"/></b>,
        /// mirroring Misa's Power Overwhelming exactly: no enemy hero may stand in radius at all, and a
        /// found one may hold an empty hand, and either is an ordinary quiet outcome rather than a
        /// failure.</para>
        ///
        /// <para><b>The naming is unconditional</b> - composed to run every time the trigger fires,
        /// whether or not a subject is ultimately found, which is the plain reading of Arty's 8/15
        /// ruling on Commune with Spirits' identical naming step extended to this one: naming a colour
        /// is never gated on the hand, or the board, holding anything for it to describe. Flagged here
        /// as a judgement call rather than a separately-ruled fact, since no designer answer names Reign
        /// of Winter by name - if a future ruling narrows it (asking only once a target is confirmed
        /// found), this is the one node to re-order.</para>
        /// </summary>
        public static EffectDefinition ReignOfWinterTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("reign-root");
            var select = new EffectNodeId("reign-select-hero");
            var nameColor = new EffectNodeId("reign-name-color");
            var discard = new EffectNodeId("reign-discard");

            return new EffectDefinition(
                ReignOfWinterTriggerId,
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, nameColor, discard }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        nameColor, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedActionCapabilities.NameColorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                SyntheticBorrowedActionCapabilities.NameColorChooserParameter,
                                EffectRoleKind.Actor,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticBorrowedActionCapabilities.NamedColorOutput,
                                EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter,
                                select, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.ColorParameter,
                                nameColor, SyntheticBorrowedActionCapabilities.NamedColorOutput,
                                EffectValueKind.Integer, EffectIntegerDomain.All<CardColor>())
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }

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
