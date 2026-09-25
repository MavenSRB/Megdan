using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueResolvedSwapTests
    {
        private static readonly HeroId Red =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Friend =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Actor =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        [Test]
        public void FriendChoosesBothCardsAndAStaleHandAnswerMovesNothing()
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var resolved = new CardInstance(
                new CardInstanceId("friend-resolved"),
                SnorriBlueMovementFaces.Definitions[0].Reference, Friend,
                new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Resolved,
                    CardFaceOrientation.FaceUp));
            var inHand = CardInstance.CreateInHand(
                new CardInstanceId("friend-hand"),
                SnorriBlueMovementFaces.Definitions[1].Reference, Friend);
            var actorCard = CardInstance.CreateInHand(
                new CardInstanceId("actor-card"),
                SnorriBlueSupportFaces.AncestralBoon.Reference, Actor);
            var redCard = CardInstance.CreateInHand(
                new CardInstanceId("red-card"),
                SnorriBlueMovementFaces.Definitions[0].Reference, Red);
            var instances = new[] { resolved, inHand, actorCard, redCard };
            var slot = new RoundTurnSlot(
                1, 1,
                new[]
                {
                    new TurnCardAssignment(Red, redCard.Id, false),
                    new TurnCardAssignment(Friend, resolved.Id, false),
                    new TurnCardAssignment(Actor, actorCard.Id, false)
                },
                instances, participants);
            var port = new TestSwapPort(resolved, inHand);
            var roster = EffectRosterState.Of(
                new[] { slot }, new RoundHeroDefeatHistory(participants),
                new HeroCoinLedger(participants), resolvedTurnSlotSwap: port);
            EffectCapabilityContext Context(
                EffectCapabilityReference reference, CardInstanceId selected = default) =>
                new EffectCapabilityContext(
                    new EffectNodeId("support-swap"),
                    EffectFailurePolicy.Mandatory,
                    Actor, Actor, actorCard.Id, actorCard.DefinitionReference,
                    SnorriBlueSupportFaces.PrimarySkill,
                    selected.IsDefined
                        ? new System.Collections.Generic.Dictionary<string, EffectValue>
                        {
                            ["owner"] = EffectValue.Hero(Friend),
                            ["resolved"] = EffectValue.Card(selected)
                        }
                        : new System.Collections.Generic.Dictionary<string, EffectValue>
                        {
                            ["owner"] = EffectValue.Hero(Friend)
                        },
                    1, 1, participants, null, RuleModificationSet.Empty,
                    EffectCardState.Of(
                        SnorriBlueMovementFaces.Definitions
                            .Concat(SnorriBlueSupportFaces.Definitions), instances),
                    ActionKind.Skill, rosterState: roster);

            var choose = SnorriBlueSupportCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(
                    SnorriBlueSupportCapabilities.ChooseResolvedCardV1));
            var first = choose.Execute(Context(choose.Reference));
            Assert.That(first.Choice.ChooserHeroId, Is.EqualTo(Friend));
            Assert.That(first.Choice.Candidates.Single(),
                Is.EqualTo(EffectValue.Card(resolved.Id)));
            Assert.That(choose.Resume(
                Context(choose.Reference),
                new[] { EffectValue.Card(resolved.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));

            var swap = SnorriBlueSupportCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(
                    SnorriBlueSupportCapabilities.SwapResolvedWithHandV1));
            var offered = swap.Execute(Context(swap.Reference, resolved.Id));
            Assert.That(offered.Choice.ChooserHeroId, Is.EqualTo(Friend));
            Assert.That(offered.Choice.Candidates.Single(),
                Is.EqualTo(EffectValue.Card(inHand.Id)));
            Assert.That(swap.Resume(
                Context(swap.Reference, resolved.Id),
                new[] { EffectValue.Card(actorCard.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(resolved.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(inHand.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(swap.Resume(
                Context(swap.Reference, resolved.Id),
                new[] { EffectValue.Card(inHand.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(port.Calls, Is.EqualTo(1));
            Assert.That(resolved.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(inHand.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(inHand.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        private sealed class TestSwapPort : IResolvedTurnSlotSwap
        {
            private readonly CardInstance resolved;
            private readonly CardInstance inHand;

            public TestSwapPort(CardInstance resolved, CardInstance inHand)
            {
                this.resolved = resolved;
                this.inHand = inHand;
            }

            public int Calls { get; private set; }

            public bool TrySwapResolvedWithHand(
                HeroId owner, CardInstanceId resolvedCard, CardInstanceId handCard)
            {
                if (!owner.Equals(Friend) || !resolvedCard.Equals(resolved.Id) ||
                    !handCard.Equals(inHand.Id))
                    return false;
                Calls++;
                return CardLifecycleAuthority.TrySwapResolvedWithHand(resolved, inHand);
            }
        }
    }
}
