using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8h: the marker capability composes its attachment's instance identity through
    /// <see cref="MarkerInstanceIdentity"/>, so two activations of one card produce two attachments
    /// that can be told apart.
    ///
    /// The proofs execute the production handler object the catalog registers, against a live register,
    /// so what is proved is the capability's own give path rather than a re-implementation of it. The
    /// give is driven through <c>Resume</c> because who holds the marker is the node's decision, and
    /// the answer is what the identity's last term reads.
    /// </summary>
    public sealed class MarkerInstanceIdentityTests
    {
        private static readonly HeroId Actor =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HeroId Holder =
            new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        /// <summary>
        /// Milestone 6.8h's failing observation. The marker supply is one, so give, return, and give
        /// again is the shortest reachable replay of one card; before the shared contract both
        /// attachments carried a byte-identical ID, because the composition read card, node, and holder
        /// alone and all three repeat.
        /// </summary>
        [Test]
        public void GiveReturnAndGiveAgainAcrossRounds_ProducesTwoDistinctAttachments()
        {
            var handler = GiveMarkerHandler();
            var components = CreateComponents();

            handler.Resume(Context(components, 1, 1), Chosen(Holder));
            var first = components.Markers.Attachments.Single();
            components.Markers.Return(first.Id);

            var second = handler.Resume(Context(components, 2, 1), Chosen(Holder));

            Assert.That(second.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The returned marker is back in the supply, so the later give must succeed.");
            Assert.That(components.Markers.Attachments, Has.Count.EqualTo(1),
                "The spent attachment must not suppress the later give.");
            Assert.That(components.Markers.Attachments.Single().Id, Is.Not.EqualTo(first.Id),
                "Two activations of the same card produce two attachments, so they must carry two " +
                "identities. A repeated ID is the same marker as far as every operation that names " +
                "one is concerned, and it crosses the snapshot boundary that way.");
            Assert.That(components.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(Holder),
                "The second give reaches the same holder; only the activation differs.");
        }

        [Test]
        public void ReExecutingTheSameActivationInsideItsOwnResolution_StaysALegalNoOp()
        {
            var handler = GiveMarkerHandler();
            var components = CreateComponents();
            var context = Context(components, 1, 1);

            handler.Resume(context, Chosen(Holder));
            var repeated = handler.Resume(context, Chosen(Holder));

            Assert.That(repeated.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Re-executing one activation is a legal no-op, not an impossible step: this " +
                "activation's own marker is exactly what the supply is currently short of, so the " +
                "already-attached test has to precede the supply test.");
            Assert.That(components.Markers.Attachments, Has.Count.EqualTo(1),
                "The same activation composes the same identity and gives nothing twice.");
        }

        [Test]
        public void TheContract_ComposesTheSameIdForTheSameActivation()
        {
            var components = CreateComponents();

            Assert.That(
                MarkerInstanceIdentity.Mint(Context(components, 2, 3), Holder),
                Is.EqualTo(MarkerInstanceIdentity.Mint(Context(components, 2, 3), Holder)),
                "Identical activation anchors must compose an identical ID, or within-resolution " +
                "re-execution stops being a no-op.");
        }

        [Test]
        public void TheContract_DistinguishesEveryActivationAnchor()
        {
            var components = CreateComponents();
            var baseline = MarkerInstanceIdentity.Mint(Context(components, 2, 3), Holder);
            var variants = new[]
            {
                MarkerInstanceIdentity.Mint(Context(components, 3, 3), Holder),
                MarkerInstanceIdentity.Mint(Context(components, 2, 4), Holder),
                MarkerInstanceIdentity.Mint(
                    Context(components, 2, 3, cardId: "marker-other-card"), Holder),
                MarkerInstanceIdentity.Mint(
                    Context(components, 2, 3, actionId: "marker-other-action"), Holder),
                MarkerInstanceIdentity.Mint(
                    Context(components, 2, 3, nodeId: "marker-other-node"), Holder),
                MarkerInstanceIdentity.Mint(Context(components, 2, 3), Actor)
            };

            Assert.That(variants, Is.Unique, "Each varied anchor must produce its own identity.");
            Assert.That(variants, Has.None.EqualTo(baseline),
                "Round, turn, card, action, node, and holder each distinguish the identity; an " +
                "anchor the composition ignores is an activation it cannot distinguish.");
        }

        [Test]
        public void TheContract_RefusesToMintWithoutTheHeroWhoReceivesTheMarker()
        {
            var components = CreateComponents();

            Assert.That(
                () => MarkerInstanceIdentity.Mint(Context(components, 1, 1), default),
                Throws.ArgumentException,
                "The holder is a term of the identity, so an undefined one would silently compose a " +
                "marker nobody holds.");
            Assert.That(
                () => MarkerInstanceIdentity.Mint(null, Holder),
                Throws.InstanceOf<ArgumentNullException>());
        }

        /// <summary>
        /// Acceptance check 3's second half. Tokens have carried round and turn since milestone 6.2 -
        /// the precedent this milestone follows - and this milestone changes markers only.
        /// </summary>
        [Test]
        public void TokenInstanceIdentity_IsUnchanged()
        {
            var handler = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticComponentCapabilities.PlaceTokenV1);
            var components = CreateComponents();
            var destination = new HexCoordinate(1, 0);

            var placed = handler.Resume(
                Context(components, 4, 2), new[] { EffectValue.Space(destination) });

            Assert.That(placed.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                components.Tokens.PlacedTokens.Single().Id,
                Is.EqualTo(new TokenInstanceId("marker-card:marker-node:r4t2:1:0")),
                "The token composition is card, node, round, turn, and space, exactly as milestone " +
                "6.2 wrote it; only the marker family moves in this milestone.");
        }

        private static IEffectCapabilityHandler GiveMarkerHandler() =>
            SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticComponentCapabilities.GiveMarkerV1);

        private static IReadOnlyList<EffectValue> Chosen(HeroId holder) =>
            new[] { EffectValue.Hero(holder) };

        /// <summary>
        /// A live component set: the trusted catalog, a small board standing in for the token and
        /// displacement ports, and an empty marker register.
        /// </summary>
        private static MatchComponents CreateComponents()
        {
            var board = new AdjacentMoveAuthority(
                Coordinates(), new[] { new HeroOccupancy(Actor, new HexCoordinate(0, 0)) });
            return new MatchComponents(
                SyntheticComponentDefinitions.CreateCatalog(),
                board,
                new MarkerAttachmentRegister(),
                board,
                board,
                board);
        }

        private static HexCoordinate[] Coordinates()
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -2; q <= 2; q++)
            {
                for (var r = Math.Max(-2, -q - 2); r <= Math.Min(2, -q + 2); r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            return coordinates.ToArray();
        }

        private static EffectCapabilityContext Context(
            MatchComponents components,
            int roundIndex,
            int turnIndex,
            string cardId = "marker-card",
            string actionId = "marker-action",
            string nodeId = "marker-node") =>
            new EffectCapabilityContext(
                new EffectNodeId(nodeId),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                new CardInstanceId(cardId),
                new ActionDefinitionId(actionId),
                null,
                roundIndex,
                turnIndex,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                components);
    }
}
