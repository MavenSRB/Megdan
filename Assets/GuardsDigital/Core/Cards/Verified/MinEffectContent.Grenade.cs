using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61f. Min the Dragonmonk's two Grenade faces, composed whole.
    ///
    /// <para><b>The printed text, both faces, exactly.</b> Death Grenade: "Place a [Grenade] Grenade
    /// token into a space in radius. <b>End of turn:</b> Up to 1 enemy hero adjacent to that token
    /// discards a card, or is defeated. Remove the Grenade token." Holy Death Grenade prints the same
    /// three sentences with "Up to 2 enemy heroes ... discard a card, or are defeated". Every structural
    /// decision below is one of those sentences and nothing else is added.</para>
    ///
    /// <para><b>Three sentences, two moments, and that split is the whole shape.</b> The Place happens
    /// when the Skill resolves; the other two happen at the end of the turn. So the Skill's body is a
    /// placement followed by the creation of an <see cref="ActiveEffectDurationKind.EndOfTurn"/> Active
    /// effect, which is the keyword Silverarrow's Warning Shot and Treetop Sentinel minted at 8.35z30 -
    /// created and activated by the Skill that prints it, triggering at the turn's own end, and
    /// expiring at that same moment so it fires exactly once. p.17 is what fixes the moment: "applied
    /// once, at the end of the current turn (after all players have had their chance to act)", and
    /// ordering beside any other end-of-turn effect is the pipeline's, not this card's.</para>
    ///
    /// <para><b>"That token" is the instance the first sentence placed, and with MI-Q1's supply of one
    /// the owned-token fact IS that instance.</b> The triggered body opens on
    /// <see cref="MinComponentContent.GrenadeSpaceV1"/> as a MANDATORY step, which settles the two
    /// negatives at once: a grenade removed before the turn ends (a Clear, a spawn-point removal)
    /// publishes no space, p.10 stops the effect there, nothing is asked of anybody, and "Remove the
    /// Grenade token" is never reached - a no-op rather than an error.</para>
    ///
    /// <para><b>"Up to N" is N optional performances - MI-Q4.</b> p.10 gives every unassigned choice in
    /// a card's text to its controller ("You make all the choices in your card's text, unless the card
    /// text instructs the other hero to make a choice"), so Min chooses WHICH enemy heroes and HOW
    /// MANY, and "up to" makes zero legal. There is no loop in this vocabulary and no maximum-targets
    /// parameter on any selection contract, so a printed count of two is two subtrees - Rowenna's Grand
    /// Melee reading of "up to two times", and Wasp's Kinetic Push's "up to 2" before it - performed
    /// one at a time, each before the next is offered.</para>
    ///
    /// <para><b>They NEST rather than sit as siblings, and that is a rule of this vocabulary rather
    /// than a preference.</b> "2 enemy heroes" is two DIFFERENT heroes, so the second pass has to name
    /// the hero the first named - through
    /// <see cref="SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName"/> - and
    /// <c>EffectDefinitionValidator</c> refuses a binding whose producer is not guaranteed to have run
    /// first (<c>NodeOutputNotGuaranteed</c>), which a DECLINABLE sibling never is. That refusal is
    /// this block's finding and it costs the card nothing: the reachable outcomes are still zero, one
    /// or two heroes, because declining the first pass and taking the second would be the same single
    /// hero as taking the first and declining the second. The two passes are one sentence read twice,
    /// not two different offers, so asking the second after a decline would repeat a question whose
    /// answer is already in.</para>
    ///
    /// <para><b>"discards a card, or is defeated" is ledger question 13's escalation, not a choice.</b>
    /// One mandatory step with two ways to complete: the named hero discards a card of their own
    /// choosing, out of their own hand, on their own seat - which is what
    /// <see cref="SyntheticCardStateCapabilities.DiscardChosenCardV2"/> means by naming the chooser -
    /// and only a hero who CANNOT is defeated, through p.13's Defeat with its Life counters, its coins
    /// to Min as the defeating hero, its assist coins and the 8.53 scoreboard's Kill. Widget's Scorching
    /// Breath, Silverarrow's Treetop Sentinel and Brogan's Counterattack print the same construction and
    /// this is the same composition.</para>
    ///
    /// <para><b>Nothing here is registered</b> - see <see cref="MinComponentContent"/>.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // Node id prefixes. One per face, because two cards printing one sentence still resolve as two
        // definitions and a pause names the node a player is answering.
        // -----------------------------------------------------------------------------------------

        private const string DeathGrenadePrefix = "grenade";
        private const string HolyDeathGrenadePrefix = "holy-grenade";

        /// <summary>
        /// "an enemy hero adjacent to that token" - the anchor-only adjacency shape Widget's Drag Off
        /// reads for units, narrowed to p.19's hero. The anchor is the grenade's own space rather than
        /// Min's, and no printed reach is intersected: the sentence measures from the TOKEN.
        /// </summary>
        public static EffectCapabilityReference GrenadeBlastTargetV1 =>
            GrenadeCapability("enemy-hero-beside-grenade");

        /// <summary>
        /// Holy Death Grenade's second pass: the same selection, minus the hero the first pass named.
        /// A separate capability rather than a parameter on the first, for the reason every
        /// definition-bound instance in this project is separate - a payload can name a node but never
        /// a different declaration - and because the first pass prints no exclusion at all.
        /// </summary>
        public static EffectCapabilityReference GrenadeSecondBlastTargetV1 =>
            GrenadeCapability("second-enemy-hero-beside-grenade");

        /// <summary>Death Grenade's "End of turn:" keyword.</summary>
        public static EffectCapabilityReference ArmDeathGrenadeV1 =>
            GrenadeCapability("min-death-grenade-at-end-of-turn");

        /// <summary>Holy Death Grenade's "End of turn:" keyword.</summary>
        public static EffectCapabilityReference ArmHolyDeathGrenadeV1 =>
            GrenadeCapability("min-holy-death-grenade-at-end-of-turn");

        /// <summary>
        /// p.17's two ways an Active effect ends other than by its own duration - the same pair every
        /// verified hero declares, stated here because a hero's content owns its own declarations.
        ///
        /// <para>Both matter to this face in a way they do not to Silverarrow's. If Min is defeated
        /// during her own turn after placing, the effect is cancelled and its three sentences never
        /// run - and the grenade then stands until p.19's end-of-round cleanup takes it, because
        /// "Remove the Grenade token" is work the card does and not a lifetime the piece carries.</para>
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] GrenadeCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// What Death Grenade runs when the turn ends: "Up to 1 enemy hero adjacent to that token
        /// discards a card, or is defeated. Remove the Grenade token."
        /// </summary>
        public static class DeathGrenadeTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                CreateGrenadeEndOfTurn(
                    new EffectDefinitionId("min-death-grenade-triggered"),
                    MinComponentContent.GrenadeRevision,
                    DeathGrenadePrefix,
                    performances: 1);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>
        /// Holy Death Grenade's own: "Up to 2 enemy heroes adjacent to that token discard a card, or
        /// are defeated. Remove the Grenade token." - Death Grenade's sentence with one more
        /// performance, and the second one excluding the first hero.
        /// </summary>
        public static class HolyDeathGrenadeTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                CreateGrenadeEndOfTurn(
                    new EffectDefinitionId("min-holy-death-grenade-triggered"),
                    MinComponentContent.GrenadeRevision,
                    HolyDeathGrenadePrefix,
                    performances: 2);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>
        /// Death Grenade's Active effect. It carries no modification at all - nothing about the board
        /// changes while it stands, and its whole content is that a piece of work happens at the end of
        /// the turn - and it declares no radius, because the disk its work measures is drawn around the
        /// TOKEN and read by the triggered body rather than by the effect's own scope.
        /// </summary>
        public static ActiveEffectDefinition DeathGrenadeEndOfTurnDefinition { get; } =
            GrenadeEndOfTurnDefinition(
                "min-death-grenade-effect",
                "MinDeathGrenade",
                "min-death-grenade",
                DeathGrenadeTriggeredEffect.Reference);

        /// <summary>Holy Death Grenade's, differing only in the body it schedules.</summary>
        public static ActiveEffectDefinition HolyDeathGrenadeEndOfTurnDefinition { get; } =
            GrenadeEndOfTurnDefinition(
                "min-holy-death-grenade-effect",
                "MinHolyDeathGrenade",
                "min-holy-death-grenade",
                HolyDeathGrenadeTriggeredEffect.Reference);

        /// <summary>
        /// "Place a [Grenade] Grenade token into a space in radius", then the "End of turn:" keyword -
        /// the two things the Skill itself does.
        ///
        /// <para><b>The placement branches on whether her grenade already stands, which is Widget's
        /// Dragon Bond shape and MI-Q1's reading.</b> With one physical piece, a second placement while
        /// one stands cannot come from the supply, and p.18's short-supply step is not what the sentence
        /// means either: the piece MOVES, identity preserved, which is the reading Dragon Bond settled
        /// for Pyro. So the presence predicate picks between an identity-preserving relocation into a
        /// space in radius and a Place from the supply. Both arms read the card's own printed Radius
        /// icon with p.16's items composed onto it, because p.9 says "if the card text mentions
        /// Range/Radius, it refers to this value".</para>
        ///
        /// <para><b>The keyword is armed AFTER the placement and inside the same sequence, and that
        /// order is load-bearing.</b> A Place with no empty space in radius is p.10's impossible step:
        /// it stops the sequence keeping prior work, so no Active effect is created and the end-of-turn
        /// sentence - which begins "adjacent to <b>that</b> token" - never has a missing referent to
        /// reason about.</para>
        /// </summary>
        public static EffectDefinition Grenade(CardDefinitionId card, string revision, bool holy)
        {
            var nodes = new[]
            {
                GrenadeSequence("root", "place", "arm"),
                new EffectNodeDefinition(
                    GrenadeNode("place"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    MinComponentContent.GrenadePresentV1,
                    childNodeIds: new[] { GrenadeNode("relocate"), GrenadeNode("from-supply") },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticOwnedTokenCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }),
                GrenadeSequence("relocate", "standing", "replace"),
                GrenadeSpace("standing"),
                new EffectNodeDefinition(
                    GrenadeNode("replace"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.PlaceInRadiusV1,
                    inputs: new[]
                    {
                        GrenadeFrom("subject", "standing", SyntheticOwnedTokenCapabilities.SpaceOutput),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.Token),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                            PlaceSpawnPointExclusion.None)
                    },
                    outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    GrenadeNode("from-supply"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    MinComponentContent.PlaceGrenadeV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    GrenadeNode("arm"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    holy ? ArmHolyDeathGrenadeV1 : ArmDeathGrenadeV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                    })
            };

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), revision, GrenadeNode("root"), nodes);
        }

        /// <summary>
        /// Milestone 8.61f. What her Grenade decision controls say - the consequence in the printed
        /// text's own words, never the mechanism.
        ///
        /// <para>Only the "up to" steps declare one, because only they open p.10's confirm-or-decline:
        /// the presence branch is a predicate nobody answers, the placement's own question is a space
        /// rather than a step, and the discard rides on the named hero's card prompt.</para>
        ///
        /// <para>They are not wired into <c>DecisionLabelCatalog</c>, for the reason nothing else of
        /// hers is registered: the catalog is reachable content, and 8.61i and 8.61j are what make her
        /// cards reachable.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> GrenadeDecisionLabels()
        {
            yield return DecisionStep(MinCardContent.DeathGrenade,
                GrenadePassNode(DeathGrenadePrefix, 0, "may").Value,
                "An enemy hero beside the Grenade discards a card, or is defeated",
                "Leave them");
            yield return DecisionStep(MinCardContent.HolyDeathGrenade,
                GrenadePassNode(HolyDeathGrenadePrefix, 0, "may").Value,
                "An enemy hero beside the Grenade discards a card, or is defeated",
                "Leave them");
            yield return DecisionStep(MinCardContent.HolyDeathGrenade,
                GrenadePassNode(HolyDeathGrenadePrefix, 1, "may").Value,
                "A second enemy hero beside the Grenade discards a card, or is defeated",
                "Stop at one");
        }

        /// <summary>The two Active-effect creations this block declares, both deferred.</summary>
        public static IEnumerable<IEffectCapabilityHandler> GrenadeActiveEffectHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActiveEffectCreationHandler(ArmDeathGrenadeV1, DeathGrenadeEndOfTurnDefinition),
                new ActiveEffectCreationHandler(
                    ArmHolyDeathGrenadeV1, HolyDeathGrenadeEndOfTurnDefinition)
            };

        /// <summary>Every Active-effect definition those two can create, for a fixture that needs them.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> GrenadeActiveEffectDefinitions { get; } =
            Array.AsReadOnly(new[]
            {
                DeathGrenadeEndOfTurnDefinition, HolyDeathGrenadeEndOfTurnDefinition
            });

        /// <summary>Every triggered body they schedule, for a fixture that configures them.</summary>
        public static IReadOnlyList<EffectDefinition> GrenadeTriggeredEffects { get; } =
            Array.AsReadOnly(new[]
            {
                DeathGrenadeTriggeredEffect.Definition, HolyDeathGrenadeTriggeredEffect.Definition
            });

        /// <summary>
        /// The shape both "End of turn:" faces share. It is Silverarrow's <c>EndOfTurnDefinition</c>
        /// word for word and deliberately so - the keyword is hers, this card only prints it - and it is
        /// restated here rather than referenced because a hero's content owns its own declarations.
        /// </summary>
        private static ActiveEffectDefinition GrenadeEndOfTurnDefinition(
            string definitionId,
            string effectKind,
            string uniquenessKey,
            EffectDefinitionReference triggeredEffect) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                GrenadeCancellations,
                false,
                effectKind,
                uniquenessKey,
                new[] { TimingCheckpoint.TurnEnded },
                triggeredEffect: triggeredEffect);

        /// <summary>
        /// "Up to N enemy heroes adjacent to that token discard a card, or are defeated. Remove the
        /// Grenade token." - both faces' second and third sentences, written once and instantiated per
        /// printed count, because Holy Death Grenade is Death Grenade's sentence read twice.
        ///
        /// <para>The token's space is read TWICE on purpose, once to anchor the selections and once
        /// immediately before the removal, which is Widget's Nibble shape. Both reads are mandatory, so
        /// a grenade that is gone at either moment stops the sentence where it stands rather than
        /// removing something else or failing.</para>
        /// </summary>
        private static EffectDefinition CreateGrenadeEndOfTurn(
            EffectDefinitionId effectId, string contentRevision, string prefix, int performances)
        {
            var root = GrenadePrefixedNode(prefix, "root");
            var anchor = GrenadePrefixedNode(prefix, "anchor");
            var standing = GrenadePrefixedNode(prefix, "standing");
            var remove = GrenadePrefixedNode(prefix, "remove");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[]
                    {
                        anchor, GrenadePassNode(prefix, 0, "may"), standing, remove
                    }),
                GrenadeSpaceNode(anchor),
                GrenadeSpaceNode(standing),
                new EffectNodeDefinition(
                    remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.RemoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", standing, SyntheticOwnedTokenCapabilities.SpaceOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            RemoveEligiblePieceKind.Token)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                    })
            };

            for (var pass = 0; pass < performances; pass++)
            {
                nodes.AddRange(GrenadeBlast(
                    prefix, pass, anchor,
                    pass + 1 < performances
                        ? GrenadePassNode(prefix, pass + 1, "may")
                        : (EffectNodeId?)null));
            }

            return new EffectDefinition(effectId, contentRevision, root, nodes);
        }

        /// <summary>
        /// One printed performance of "up to N ... discards a card, or is defeated": the optional step
        /// that opens it, the hero selection anchored on the token, the discard on that hero's own
        /// seat, and ledger question 13's escalation when their hand is empty. The next performance,
        /// where the card prints one, is the last step of this one - see the type's own remarks on why
        /// the passes nest.
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> GrenadeBlast(
            string prefix, int pass, EffectNodeId anchor, EffectNodeId? next)
        {
            var may = GrenadePassNode(prefix, pass, "may");
            var blast = GrenadePassNode(prefix, pass, "blast");
            var target = GrenadePassNode(prefix, pass, "target");
            var discard = GrenadePassNode(prefix, pass, "discard");
            var escalation = GrenadePassNode(prefix, pass, "escalation");
            var defeat = GrenadePassNode(prefix, pass, "defeat");

            // NO CONSUMER IS DECLARED, and that is a printed decision rather than an omission.
            //
            // A selection names the node it feeds so that a candidate the consumer could do nothing
            // with is never offered - "a unit with nowhere to go", in Silverarrow's own words. That
            // reading is exactly wrong for THIS sentence: "discards a card, OR IS DEFEATED" is one
            // step with two ways to complete, so a hero with an empty hand is not an unusable
            // candidate, they are the candidate the second half of the sentence was written for.
            // Declared as a consumer, the discard narrows every empty-handed hero out of the offer,
            // the optional step then has no legal subject, and the face can never defeat anybody -
            // measured on this block's own drive before it was removed.
            //
            // Widget's Scorching Breath prints the same escalation and declares no consumer either,
            // which is the precedent this follows.
            var selection = new List<EffectInputBinding>
            {
                EffectInputBinding.FromNodeOutput(
                    "anchor", anchor, SyntheticOwnedTokenCapabilities.SpaceOutput,
                    EffectValueKind.Coordinate)
            };
            if (pass > 0)
            {
                selection.Add(EffectInputBinding.FromNodeOutput(
                    SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName,
                    GrenadePassNode(prefix, pass - 1, "target"), "hero", EffectValueKind.Hero));
            }

            yield return new EffectNodeDefinition(
                may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                childNodeIds: new[] { blast }, chooser: EffectChooserRole.Actor);
            yield return new EffectNodeDefinition(
                blast, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: next.HasValue
                    ? new[] { target, discard, escalation, next.Value }
                    : new[] { target, discard, escalation });
            yield return new EffectNodeDefinition(
                target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                pass == 0 ? GrenadeBlastTargetV1 : GrenadeSecondBlastTargetV1,
                inputs: selection,
                outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) });
            yield return new EffectNodeDefinition(
                discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticCardStateCapabilities.DiscardChosenCardV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.HeroParameter, target, "hero",
                        EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance),
                    new EffectOutputBinding(
                        SyntheticCardStateCapabilities.UnableToDiscardOutput, EffectValueKind.Boolean)
                });
            yield return new EffectNodeDefinition(
                escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { defeat },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "flag", discard, SyntheticCardStateCapabilities.UnableToDiscardOutput,
                        EffectValueKind.Boolean)
                },
                outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });
            yield return new EffectNodeDefinition(
                defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticConsequenceCapabilities.DefeatHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticConsequenceCapabilities.HeroParameter, target, "hero",
                        EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                });
        }

        // -----------------------------------------------------------------------------------------
        // Local composition helpers. Named for this block so five parallel partial files over one
        // class never collide on a helper.
        // -----------------------------------------------------------------------------------------

        private static EffectNodeDefinition GrenadeSpace(string id) => GrenadeSpaceNode(GrenadeNode(id));

        private static EffectNodeDefinition GrenadeSpaceNode(EffectNodeId id) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                MinComponentContent.GrenadeSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticOwnedTokenCapabilities.SpaceOutput, EffectValueKind.Coordinate)
                });

        private static EffectNodeDefinition GrenadeSequence(string id, params string[] children) =>
            new EffectNodeDefinition(
                GrenadeNode(id), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: Array.ConvertAll(children, GrenadeNode));

        private static EffectInputBinding GrenadeFrom(string name, string node, string output) =>
            EffectInputBinding.FromNodeOutput(
                name, GrenadeNode(node), output, EffectValueKind.Coordinate);

        private static EffectNodeId GrenadeNode(string id) => new EffectNodeId(id);

        private static EffectNodeId GrenadePrefixedNode(string prefix, string id) =>
            new EffectNodeId(prefix + "-" + id);

        /// <summary>
        /// One printed performance's node, named rather than numbered where the card prints one
        /// performance and numbered where it prints two - so a player answering Holy Death Grenade's
        /// second question can see that it is the second.
        /// </summary>
        private static EffectNodeId GrenadePassNode(string prefix, int pass, string id) =>
            new EffectNodeId(prefix + "-" + (pass == 0 ? "first" : "second") + "-" + id);

        private static EffectCapabilityReference GrenadeCapability(string id) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), 1);
    }
}
