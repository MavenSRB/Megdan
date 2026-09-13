using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40b. The three marker MOVEMENTS Bain's Green Blocks and his Gold basic need, built
    /// on the one attachment that already exists rather than on a second one.
    ///
    /// <para><b>Three printed sentences, three gaps, one block.</b> Close Call prints "that hero gives
    /// the marker to you"; Narrow Escape prints "retrieve the marker" - the rulebook's own word for
    /// returning one to the supply, p.8's "Return all markers back into the supply" and p.13's "Any
    /// markers they had are returned" - about a marker that face never gave; and Dead or Alive prints a
    /// give that BA-D1 reads as MOVING the marker when the one copy is already on the board.</para>
    ///
    /// <para><b>The transfer is its own operation and not a return followed by a give, and the reason
    /// is a card in the same set.</b> BA-D5: A Complicated Profession fires "after <em>you</em> give a
    /// hero the bounty marker", and Close Call's sentence is another hero giving it to Bain. A
    /// composition that ran a give would fire his own permanent ability on a clause that never says he
    /// gave anything, and would mint a second <c>MarkerInstanceIdentity</c> for a piece that never left
    /// the table. Both halves are asserted from the state a give leaves rather than from reading the
    /// composition.</para>
    ///
    /// <para><b>The board is featureless and carries TWO marker definitions.</b> A radius-3 disk with
    /// three heroes, Tigerclaw's Poison and Bain's Bounty both reachable, because "whose marker" is a
    /// claim a board with one definition on it cannot test.</para>
    /// </summary>
    public sealed class BainMarkerMovementTests
    {
        // -------------------------------------------------------------------------------------------
        // The transfer: one attachment, one operation, two heroes published
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ATransfer_MovesOneAttachmentBetweenHolders_KeepingItsInstanceIdentity()
        {
            var fixture = new MarkerMovementFixture();
            var given = fixture.GiveBountyTo(fixture.Enemy);

            var result = fixture.Transfer(to: fixture.Actor);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            var moved = fixture.Markers.Attachments.Single();
            Assert.That(moved.HolderHeroId, Is.EqualTo(fixture.Actor),
                "\"that hero gives the marker to you\" - the marker ends on the hero the sentence " +
                "names.");
            Assert.That(moved.Id, Is.EqualTo(given.Id),
                "ONE operation on the attachment that already exists: the identity a restore rebinds " +
                "against is the identity the piece arrived with.");
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(1),
                "A transfer is not a second give, so the board never holds two.");
        }

        [Test]
        public void ATransfer_MovesWhoseValuesThePropertyAffects()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyTo(fixture.Enemy);

            var before = fixture.DerivedModifications();
            Assert.That(
                before.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.Actor), RuleObject.ForHero(fixture.Enemy)),
                Is.EqualTo(1),
                "The marker declares its cost about whoever holds it, which before the transfer is " +
                "the enemy hero.");

            fixture.Transfer(to: fixture.Actor);

            var after = fixture.DerivedModifications();
            Assert.That(
                after.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.Enemy), RuleObject.ForHero(fixture.Actor)),
                Is.EqualTo(1),
                "\"(The marker's effect is applied to you)\" - the printed parenthetical, asserted " +
                "through the modification set rather than through the attachment's own field.");
            Assert.That(
                after.AdditionalDefeatLifeCost(
                    RuleObject.ForHero(fixture.Actor), RuleObject.ForHero(fixture.Enemy)),
                Is.Zero,
                "and the hero it left declares nothing, which is the half a copy rather than a move " +
                "would fail.");
        }

        [Test]
        public void ATransfer_PublishesBothHeroes()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyTo(fixture.Enemy);

            var result = fixture.Transfer(to: fixture.Ally);

            Assert.That(
                result.Outcomes.Single(fact => fact.OutputName == "holder").Value.HeroId,
                Is.EqualTo(fixture.Ally));
            Assert.That(
                result.Outcomes.Single(fact =>
                    fact.OutputName ==
                    SyntheticComponentCapabilities.MarkerTransferHandler.PreviousHolderOutputName)
                    .Value.HeroId,
                Is.EqualTo(fixture.Enemy),
                "Close Call's consequence is about the hero the marker came off, so the operation " +
                "publishes it rather than leaving a later node to ask a second question.");
        }

        [Test]
        public void ATransfer_TakesNothingFromTheSupply_AndKeepsTheOwnerAndTheGivingCard()
        {
            var fixture = new MarkerMovementFixture();
            var given = fixture.GiveBountyTo(fixture.Enemy);
            var available = fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition);

            fixture.Transfer(to: fixture.Actor);

            var moved = fixture.Markers.Attachments.Single();
            Assert.That(
                fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition),
                Is.EqualTo(available),
                "What it moves is already out of the supply, so the supply cannot change.");
            Assert.That(moved.OwnerHeroId, Is.EqualTo(given.OwnerHeroId),
                "The supply a return puts it back into is still his.");
            Assert.That(moved.SourceCardInstanceId, Is.EqualTo(given.SourceCardInstanceId));
            Assert.That(moved.SourceActionDefinitionId, Is.EqualTo(given.SourceActionDefinitionId));
            Assert.That(moved.Definition.Reference,
                Is.EqualTo(BainComponentContent.BountyDefinition.Reference));
        }

        [Test]
        public void ATransferOntoTheHeroAlreadyHoldingIt_IsALegalNoOp()
        {
            var fixture = new MarkerMovementFixture();
            var given = fixture.GiveBountyTo(fixture.Actor);

            var result = fixture.Transfer(to: fixture.Actor);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "A card that says \"gives the marker to you\" when you already hold it asks for " +
                "nothing, which is a legal nothing rather than a refusal.");
            Assert.That(fixture.Markers.Attachments.Single().Id, Is.EqualTo(given.Id));
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Actor));
        }

        [Test]
        public void ATransferWithNoMarkerOut_IsImpossible_AndTheProbeAgrees()
        {
            var fixture = new MarkerMovementFixture();

            Assert.That(fixture.Transfer(to: fixture.Actor).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Nothing on the board to move.");
            Assert.That(fixture.AnticipateTransfer(to: fixture.Actor),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            fixture.GiveBountyTo(fixture.Enemy);

            Assert.That(fixture.AnticipateTransfer(to: fixture.Actor),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "A bound recipient applies rather than asking, and the probe says what the execution " +
                "would do.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy),
                "and asking performed nothing.");
        }

        [Test]
        public void ATransfer_NeverTouchesAMarkerOfADifferentDefinition()
        {
            var fixture = new MarkerMovementFixture();
            var poison = fixture.GivePoisonTo(fixture.Enemy);

            Assert.That(fixture.Transfer(to: fixture.Actor).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A hero wearing Tigerclaw's Poison is not wearing a bounty, and a card that took " +
                "either would state a rule no face prints.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy));

            fixture.GiveBountyTo(fixture.Ally);
            fixture.Transfer(to: fixture.Actor);

            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(2),
                "Both definitions are on this board, which is what makes the negative above a " +
                "measurement rather than an empty set answering it.");
            Assert.That(
                fixture.Markers.Attachments.Single(attachment => attachment.Id.Equals(poison.Id))
                    .HolderHeroId,
                Is.EqualTo(fixture.Enemy),
                "The Poison did not move.");
            Assert.That(
                fixture.Markers.OfDefinition(BainComponentContent.BountyDefinition.Reference).Single()
                    .HolderHeroId,
                Is.EqualTo(fixture.Actor));
        }

        [Test]
        public void AnUnboundTransfer_AsksWhoReceivesIt()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyTo(fixture.Enemy);

            var result = fixture.TransferWithoutNamingARecipient();

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                result.Choice.Candidates.Select(candidate => candidate.HeroId),
                Is.EquivalentTo(new[] { fixture.Actor, fixture.Enemy, fixture.Ally }),
                "Like the give, a card whose own sentence does not bound the recipient opens the " +
                "question over the heroes in the match.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy),
                "and asking moved nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // BA-D1: a give with the supply empty and the marker held MOVES it
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AGiveWithTheSupplyEmptyAndTheMarkerHeld_MovesIt()
        {
            var fixture = new MarkerMovementFixture();
            var first = fixture.Give(to: fixture.Enemy, movesWhenSupplyIsEmpty: true, nodeSuffix: "1");
            Assert.That(first.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition), Is.Zero,
                "BA-D1: the supply is ONE, so the first give empties it.");

            var second = fixture.Give(to: fixture.Ally, movesWhenSupplyIsEmpty: true, nodeSuffix: "2");

            Assert.That(second.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "At a physical table the marker is simply taken off the first hero and pinned on the " +
                "second; without this arm Dead or Alive would silently stop offering its own printed " +
                "clause from the second turn onwards.");
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(1));
            var moved = fixture.Markers.Attachments.Single();
            Assert.That(moved.HolderHeroId, Is.EqualTo(fixture.Ally));
            Assert.That(
                moved.Id,
                Is.EqualTo(fixture.ExpectedGiveIdentity(
                    MarkerMovementFixture.GiveNode("1"), fixture.Enemy)),
                "It is the copy the FIRST give minted, moved - not a second identity for one piece.");
            Assert.That(
                fixture.Markers.Attachments.Select(attachment => attachment.Id),
                Has.No.Member(fixture.ExpectedGiveIdentity(
                    MarkerMovementFixture.GiveNode("2"), fixture.Ally)));
            Assert.That(
                fixture.AnticipateGive(to: fixture.Ally, movesWhenSupplyIsEmpty: true),
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "and the probe admits the move arm, so the option is offered rather than greyed.");
        }

        /// <summary>
        /// The block's other half - "a give with the supply empty and no marker out is still
        /// impossible" - and the probe's finding about it, asserted rather than narrated.
        ///
        /// <para><b>That configuration is unreachable by construction, and the first assertion is the
        /// proof.</b> A marker supply is derived rather than stored - available is the trusted total
        /// minus what is held - and <c>MarkerDefinition</c> refuses a total below one, so "the supply
        /// is empty" and "a copy is held" are the same fact for a supply of one. What IS reachable is
        /// the refusal the arm must not swallow, and it is here twice: a node that does not bind the
        /// arm keeps today's refusal exactly, and a definition whose empty supply has no SINGLE copy to
        /// move refuses too, which is the six-copy case beside it.</para>
        /// </summary>
        [Test]
        public void AGiveWithAnEmptySupply_RefusesWhereverThereIsNoOneMarkerToMove()
        {
            var fixture = new MarkerMovementFixture();
            fixture.Give(to: fixture.Enemy, movesWhenSupplyIsEmpty: true, nodeSuffix: "1");

            Assert.That(fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition), Is.Zero);
            Assert.That(
                fixture.Markers.OfDefinition(BainComponentContent.BountyDefinition.Reference).Count(),
                Is.EqualTo(1),
                "An empty supply IS a held copy: the count is derived from the attachments, and the " +
                "definition refuses a total below one, so an empty supply with nothing out cannot be " +
                "staged at all.");

            var unbound = fixture.Give(
                to: fixture.Ally, movesWhenSupplyIsEmpty: false, nodeSuffix: "2");

            Assert.That(unbound.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A node that does not bind BA-D1's arm keeps the refusal it has had since milestone " +
                "6.2, which is what makes the arm opt-in rather than a change of meaning.");
            Assert.That(
                fixture.AnticipateGive(to: fixture.Ally, movesWhenSupplyIsEmpty: false),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and the probe refuses it too, so the option is greyed rather than offered and then " +
                "refused.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy),
                "and nothing moved.");
        }

        [Test]
        public void AGiveWithASupplyThatCanPay_IsUnchangedByTheAddedParameter()
        {
            var bound = new MarkerMovementFixture();
            var unbound = new MarkerMovementFixture();

            var withFlag = bound.Give(to: bound.Enemy, movesWhenSupplyIsEmpty: true, nodeSuffix: "1");
            var withoutFlag =
                unbound.Give(to: unbound.Enemy, movesWhenSupplyIsEmpty: false, nodeSuffix: "1");

            Assert.That(withFlag.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(withoutFlag.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(bound.Markers.Attachments.Single().Id,
                Is.EqualTo(unbound.Markers.Attachments.Single().Id),
                "A supply that can pay mints from the supply whatever the parameter says, so the " +
                "identity is the one milestone 6.8h composes.");
            Assert.That(bound.Markers.Attachments.Single().Id,
                Is.EqualTo(bound.ExpectedGiveIdentity(
                    MarkerMovementFixture.GiveNode("1"), bound.Enemy)));
            Assert.That(
                bound.Markers.CountAvailable(BainComponentContent.BountyDefinition),
                Is.EqualTo(unbound.Markers.CountAvailable(BainComponentContent.BountyDefinition)));
        }

        /// <summary>
        /// The other half of "unchanged", proved over the SHIPPED corpus rather than reasoned about: a
        /// default-off parameter only leaves earlier declarations alone if no earlier declaration binds
        /// it, and this sweeps every dealt face of every hero rather than naming the two that give
        /// markers - which is what keeps the guard true for the sixteenth hero as well.
        /// </summary>
        [Test]
        public void NoDealtFaceInTheBuild_BindsTheNewGiveParameter_AndTheGivesSchemaIsOtherwiseUnmoved()
        {
            var walked = 0;
            var binders = new List<string>();
            foreach (var type in typeof(WaspCardContent).Assembly.GetTypes()
                .Where(candidate =>
                    candidate.IsClass && candidate.IsAbstract && candidate.IsSealed &&
                    candidate.Namespace == typeof(WaspCardContent).Namespace &&
                    candidate.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
            {
                var dealt = type.GetProperty(
                    "DealtDefinitions", BindingFlags.Public | BindingFlags.Static);
                if (dealt == null)
                {
                    continue;
                }

                foreach (var definition in (IEnumerable<CardDefinition>)dealt.GetValue(null))
                {
                    foreach (var action in definition.ActionDefinitions
                        .Where(action => action.EffectRoot != null))
                    {
                        foreach (var node in action.EffectRoot.Nodes)
                        {
                            walked++;
                            if (node.Inputs.Any(input => input.ParameterName ==
                                SyntheticComponentCapabilities.MarkerGivingHandler
                                    .MovesWhenSupplyIsEmptyParameterName))
                            {
                                binders.Add($"{definition.Id.Value} / {node.Id.Value}");
                            }
                        }
                    }
                }
            }

            Assert.That(walked, Is.GreaterThan(0),
                "The reflection must find the content classes and their nodes at all.");
            Assert.That(binders, Is.Empty,
                "BA-D1's arm is opt-in, so every face dealt before it keeps milestone 6.2's refusal: " +
                string.Join(", ", binders));

            var registered = MatchContentCatalogs.ForThisBuild().Capabilities
                .Load(SyntheticComponentCapabilities.GiveMarkerV1);
            Assert.That(
                registered.Schema.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new[]
                {
                    SyntheticComponentCapabilities.MarkerGivingHandler.HolderParameterName,
                    SyntheticComponentCapabilities.MarkerGivingHandler
                        .MovesWhenSupplyIsEmptyParameterName
                }));
            Assert.That(
                registered.Schema.Parameters.Select(parameter => parameter.IsRequired),
                Is.EqualTo(new[] { false, false }),
                "Both optional, which is what makes the addition free for every earlier node.");
            Assert.That(
                registered.Schema.Outputs.Select(output => output.OutputName),
                Is.EqualTo(new[] { "holder" }),
                "and the give publishes exactly what it published before.");
        }

        [Test]
        public void TigerclawsPoison_KeepsItsExhaustedSupplyRefusal()
        {
            var fixture = new MarkerMovementFixture();
            var definition = TigerclawComponentContent.PoisonOneDefinition;
            for (var copy = 0; copy < definition.SupplyCount; copy++)
            {
                fixture.AttachPoisonCopy(copy);
            }

            Assert.That(fixture.Markers.CountAvailable(definition), Is.Zero);
            Assert.That(
                fixture.GivePoison(to: fixture.Enemy, movesWhenSupplyIsEmpty: false).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "His declarations bind nothing, so his six-copy supply behaves exactly as it did.");
            Assert.That(
                fixture.GivePoison(to: fixture.Enemy, movesWhenSupplyIsEmpty: true).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and even a face that DID bind the arm keeps the refusal, because six copies out is " +
                "not one marker to move and choosing among them would state a rule no text prints.");
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(definition.SupplyCount));
        }

        // -------------------------------------------------------------------------------------------
        // The return the rulebook means by "retrieve", and the reading it does NOT replace
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AReturnAny_DetachesTheOneAttachmentWhereverItSits_AndPublishesItsFormerHolder()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyTo(fixture.Ally);

            var result = fixture.ReturnAny();

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(result.Outcomes.Single().OutputName, Is.EqualTo("returned"));
            Assert.That(result.Outcomes.Single().Value.HeroId, Is.EqualTo(fixture.Ally));
            Assert.That(fixture.Markers.Attachments, Is.Empty);
            Assert.That(
                fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition),
                Is.EqualTo(BainComponentContent.BountySupplyCount),
                "\"Retrieve\" is p.8's and p.13's word for returning it to the supply, performed " +
                "early by card text.");
        }

        [Test]
        public void AReturnAnyWithNoneOut_IsImpossible_AndTheProbeAgrees()
        {
            var fixture = new MarkerMovementFixture();

            Assert.That(fixture.AnticipateReturnAny(),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(fixture.ReturnAny().Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            fixture.GiveBountyTo(fixture.Enemy);

            Assert.That(fixture.AnticipateReturnAny(),
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(1),
                "Asking whether it can be returned must not return it.");
        }

        [Test]
        public void TheTwoReadingsOfTheReturn_DifferOnAMarkerAnotherCardGave()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyTo(fixture.Enemy);

            Assert.That(fixture.AnticipateSourceCardReturn(),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "synthetic-return-marker@1's finder is Markers.GivenBy(SourceCardInstanceId), and " +
                "Narrow Escape gives no marker at all - which is exactly why the definition-bound " +
                "reading had to be built rather than reused.");
            Assert.That(fixture.SourceCardReturn().Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(1),
                "and the older reading left the board alone.");

            Assert.That(fixture.ReturnAny().Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The same board, the same marker, the other reading.");
            Assert.That(fixture.Markers.Attachments, Is.Empty);
        }

        [Test]
        public void TheSourceCardReading_IsUnchangedForTheCardThatGave()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyFromThisCard(fixture.Enemy);

            Assert.That(fixture.AnticipateSourceCardReturn(),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "A card that DID give a marker still takes back its own, which is milestone 6.2's " +
                "reading untouched.");
            var result = fixture.SourceCardReturn();
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(result.Outcomes.Single().Value.HeroId, Is.EqualTo(fixture.Enemy));
            Assert.That(fixture.Markers.Attachments, Is.Empty);
            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities
                    .Load(SyntheticComponentCapabilities.ReturnMarkerV1).Schema.Outputs
                    .Select(output => output.OutputName),
                Is.EqualTo(new[] { "returned" }),
                "and the registered capability's declared shape did not move either.");
        }

        [Test]
        public void AReturnAny_NeverTouchesAMarkerOfADifferentDefinition()
        {
            var fixture = new MarkerMovementFixture();
            var poison = fixture.GivePoisonTo(fixture.Enemy);

            Assert.That(fixture.ReturnAny().Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(fixture.Markers.Attachments.Single().Id, Is.EqualTo(poison.Id));

            fixture.GiveBountyTo(fixture.Ally);
            Assert.That(fixture.ReturnAny().Kind, Is.EqualTo(EffectExecutionResultKind.Applied));

            Assert.That(fixture.Markers.Attachments.Single().Id, Is.EqualTo(poison.Id),
                "One definition left the board and the other stayed, asserted on a board carrying " +
                "both.");
        }

        // -------------------------------------------------------------------------------------------
        // BA-D5: a transfer leaves no trace a give leaves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The acceptance check this block states in the negative, asserted from the recorded stream a
        /// real resolution produces rather than from reading the composition.
        ///
        /// <para><b>What watches a give today, measured rather than assumed: nothing does.</b> No
        /// <c>TimingCheckpoint</c> member is a marker moment, no marker event exists outside p.8's
        /// end-of-round return and p.13's defeated-holder return, and the give declares no checkpoint
        /// at all - which is what milestone 8.40h will change. So the trace a give leaves is the state
        /// it leaves: a NEW attachment carrying the identity <c>MarkerInstanceIdentity</c> composes for
        /// that activation, and one fewer copy in the supply. This case runs a give and a transfer as
        /// two nodes of ONE resolution and asserts the give leaves both and the transfer leaves
        /// neither, so a trigger keyed on either cannot see the transfer.</para>
        /// </summary>
        [Test]
        public void ATransfer_LeavesNoTraceAGiveLeaves()
        {
            var fixture = new MarkerMovementFixture();

            var run = fixture.RunGiveThenTransfer();

            Assert.That(run.IsComplete, Is.True);
            Assert.That(run.Events.Select(recorded => recorded.ResultKind),
                Has.No.Member(EffectExecutionResultKind.ImpossibleMandatory));

            var give = run.Events.Single(recorded =>
                recorded.NodeId.Equals(MarkerMovementFixture.GiveNodeId));
            var transfer = run.Events.Single(recorded =>
                recorded.NodeId.Equals(MarkerMovementFixture.TransferNodeId));
            Assert.That(give.ResultKind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(transfer.ResultKind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(give.Outputs.Single().Value.HeroId, Is.EqualTo(fixture.Enemy),
                "The give published the hero it gave to, which is the one fact it has ever left in " +
                "this stream.");
            Assert.That(transfer.Outputs.Select(fact => fact.OutputName), Is.EquivalentTo(new[]
            {
                "holder",
                SyntheticComponentCapabilities.MarkerTransferHandler.PreviousHolderOutputName
            }), "and the transfer published two, because its sentence is about two heroes.");

            var givenIdentity = fixture.ExpectedGiveIdentity(
                MarkerMovementFixture.GiveNodeId, fixture.Enemy);
            var transferIdentity = fixture.ExpectedGiveIdentity(
                MarkerMovementFixture.TransferNodeId, fixture.Actor);
            Assert.That(fixture.Markers.Attachments.Select(attachment => attachment.Id),
                Has.Member(givenIdentity),
                "The give minted this activation's identity, which is the trace every give leaves.");
            Assert.That(fixture.Markers.Attachments.Select(attachment => attachment.Id),
                Has.No.Member(transferIdentity),
                "The transfer minted none, so nothing keyed on a give's own minted attachment can " +
                "see it - which is BA-D5's requirement stated as state rather than as composition.");
            Assert.That(fixture.Markers.Attachments, Has.Count.EqualTo(1),
                "and no second attachment stands, which is what a return-then-give composition would " +
                "have left mid-way.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Actor));
            Assert.That(
                fixture.Markers.CountAvailable(BainComponentContent.BountyDefinition), Is.Zero,
                "One copy left the supply across the whole resolution - the give's - so the supply " +
                "movement a watcher could key on happened exactly once.");

            Assert.That(transfer.CheckpointsAfter, Is.Empty);
            Assert.That(give.CheckpointsAfter, Is.Empty,
                "Neither node raises a timing checkpoint today, which is the probe's finding rather " +
                "than an accident: milestone 8.40h adds the moment his Ultimate watches, and it must " +
                "key it on the give capability this transfer is not.");
            Assert.That(
                BainComponentContent.TransferBountyV1, Is.Not.EqualTo(BainComponentContent.GiveBountyV1),
                "Two capabilities, so a trigger naming one names one.");
        }

        // -------------------------------------------------------------------------------------------
        // Snapshot, replay and Undo
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The persisted marker stream is written and read back through the snapshot serializer's own
        /// private codec, reached by reflection because Core publishes no InternalsVisibleTo for this
        /// assembly - the same route the Whisper and 8.40a fixtures take. Each operation is asserted
        /// after its own round trip, because what a transfer changes (the holder) is exactly what the
        /// wire carries, and what it must not change (the identity, the owner, the giving card) is the
        /// rest of the same record.
        /// </summary>
        [Test]
        public void EveryOperation_SurvivesASnapshotRoundTrip()
        {
            var fixture = new MarkerMovementFixture();
            fixture.GiveBountyTo(fixture.Enemy);
            fixture.GivePoisonTo(fixture.Ally);

            fixture.Transfer(to: fixture.Actor);
            var afterTransfer = fixture.RoundTripMarkers();
            Assert.That(
                afterTransfer.OfDefinition(BainComponentContent.BountyDefinition.Reference).Single()
                    .HolderHeroId,
                Is.EqualTo(fixture.Actor),
                "The holder is a persisted field, so the move crosses the boundary with no new field " +
                "to add.");
            Assert.That(
                afterTransfer.OfDefinition(BainComponentContent.BountyDefinition.Reference).Single()
                    .Id,
                Is.EqualTo(fixture.Markers.Attachments
                    .Single(attachment => attachment.Definition.Reference.Equals(
                        BainComponentContent.BountyDefinition.Reference))
                    .Id));
            Assert.That(
                afterTransfer.OfDefinition(TigerclawComponentContent.PoisonOneDefinition.Reference)
                    .Single().HolderHeroId,
                Is.EqualTo(fixture.Ally),
                "and the other definition round-trips beside it untouched.");

            fixture.Give(to: fixture.Ally, movesWhenSupplyIsEmpty: true, nodeSuffix: "move");
            var afterMove = fixture.RoundTripMarkers();
            Assert.That(
                afterMove.OfDefinition(BainComponentContent.BountyDefinition.Reference).Single()
                    .HolderHeroId,
                Is.EqualTo(fixture.Ally));

            fixture.ReturnAny();
            var afterReturn = fixture.RoundTripMarkers();
            Assert.That(
                afterReturn.OfDefinition(BainComponentContent.BountyDefinition.Reference), Is.Empty);
            Assert.That(afterReturn.Attachments, Has.Count.EqualTo(1),
                "A return that crossed the boundary leaves the Poison standing and the bounty in the " +
                "supply.");
        }

        /// <summary>
        /// Undo in this build is a retained-prefix re-derivation rather than an inverse operation, so
        /// what "Undo restores the previous holder" means at this tier is two things measured
        /// together: the state captured before the move restores to the previous holder with the same
        /// identity, and re-running the same operation from that restored state reproduces the moved
        /// state BYTE for byte. The second is the replay; the first is the rewind.
        /// </summary>
        [Test]
        public void UndoRestoresThePreviousHolder_AndReplayReproducesTheMoveByteForByte()
        {
            var fixture = new MarkerMovementFixture();
            var given = fixture.GiveBountyTo(fixture.Enemy);
            var beforeBytes = fixture.WriteMarkerBytes();

            fixture.Transfer(to: fixture.Actor);
            var afterBytes = fixture.WriteMarkerBytes();
            Assert.That(afterBytes, Is.Not.EqualTo(beforeBytes),
                "The move has to be visible in the persisted stream, or the rewind below would pass " +
                "against a stream that never recorded it.");

            var rewound = fixture.ReadMarkerBytes(beforeBytes);
            Assert.That(rewound.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy),
                "The prefix up to but not including the transfer re-derives the previous holder.");
            Assert.That(rewound.Attachments.Single().Id, Is.EqualTo(given.Id),
                "with the same instance identity, because the transfer never minted a new one to " +
                "leave behind.");

            var replayed = fixture.ReplayTransferOn(rewound, to: fixture.Actor);
            Assert.That(replayed.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(fixture.WriteMarkerBytes(rewound), Is.EqualTo(afterBytes),
                "The same command run again from the same state produces the same state, which is " +
                "what a replay is.");
        }

        // -------------------------------------------------------------------------------------------
        // Deferral: all three halves wait for the dealing together
        // -------------------------------------------------------------------------------------------

        [Test]
        public void BainsThreeMarkerOperations_AreDeferredTogether_AndOneOwesAShowcaseConsumer()
        {
            var deferred = BainComponentContent.DeferredHandlers().ToArray();

            Assert.That(deferred.Select(handler => handler.Reference), Is.EqualTo(new[]
            {
                BainComponentContent.GiveBountyV1,
                BainComponentContent.TransferBountyV1,
                BainComponentContent.ReturnBountyV1,
                BainComponentContent.BountyHeldV1
            }));
            Assert.That(BainComponentContent.RegisteredHandlers(), Is.Empty);

            var capabilities = MatchContentCatalogs.ForThisBuild().Capabilities;
            foreach (var handler in deferred)
            {
                Assert.That(capabilities.IsRegistered(handler.Reference), Is.False,
                    "Registering one before a dealt card can start it moves pinned bytes and fails " +
                    "milestone 7.9's reachability gate.");
            }

            Assert.That(deferred[1],
                Is.InstanceOf<SyntheticComponentCapabilities.MarkerTransferHandler>(),
                "ONE OF THE TWO NEW TYPES (8.40c's predicate is the other). Milestone 7.9's deferral gate asks its second half of the handler " +
                "TYPE, so this one owes a showcase consumer at milestone 8.40j; the give's type is " +
                "Tigerclaw's and the return's is milestone 6.2's, and neither does.");
            Assert.That(deferred[0],
                Is.InstanceOf<SyntheticComponentCapabilities.MarkerGivingHandler>());
            Assert.That(deferred[2],
                Is.InstanceOf<SyntheticComponentCapabilities.ReturnMarkerHandler>());
            Assert.That(
                capabilities.IsRegistered(SyntheticComponentCapabilities.ReturnMarkerV1), Is.True,
                "and the return's type reaches this build through milestone 6.2's own registered " +
                "instance, which is what makes the sentence above true rather than hopeful.");
            Assert.That(
                SyntheticComponentDefinitions.CreateCatalog()
                    .IsRegistered(BainComponentContent.BountyDefinition.Reference),
                Is.False,
                "and his marker definition is still deferred with them.");
        }

        // -------------------------------------------------------------------------------------------
        // The fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A featureless radius-3 disk with three heroes, a live marker register, and the component
        /// catalog milestone 8.40j will compose: this build's registered markers plus Bain's deferred
        /// bounty. Composing it here rather than registering it is what lets his operations be driven
        /// against the production handler objects without moving a pinned byte.
        /// </summary>
        private sealed class MarkerMovementFixture
        {
            public static readonly EffectNodeId GiveNodeId = new EffectNodeId("bounty-give");
            public static readonly EffectNodeId TransferNodeId = new EffectNodeId("bounty-transfer");

            private const string CardIdValue = "bain-marker-card";
            private const string ActionIdValue = "primary-skill";

            private readonly MatchParticipants participants =
                StagedBoardParticipants.CreateWithBlueAlly();
            private readonly ComponentDefinitionCatalog catalog;
            private readonly MatchComponents components;
            private readonly IEffectCapabilityHandler give;
            private readonly IEffectCapabilityHandler transfer;
            private readonly IEffectCapabilityHandler returnAny;
            private readonly IEffectCapabilityHandler returnBySourceCard;
            private readonly IEffectCapabilityHandler givePoison;

            public MarkerMovementFixture()
            {
                Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
                var board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(1, 0)),
                        new HeroOccupancy(Ally, new HexCoordinate(0, 1))
                    });
                catalog = new ComponentDefinitionCatalog(
                    new[]
                    {
                        SyntheticComponentDefinitions.HazardDefinition,
                        SyntheticComponentDefinitions.WardDefinition
                    },
                    new[] { SyntheticComponentDefinitions.FocusDefinition }
                        .Concat(TigerclawComponentContent.Markers())
                        .Concat(BainComponentContent.Markers()));
                components = new MatchComponents(catalog, board, Markers, board, board, board);

                var handlers = BainComponentContent.DeferredHandlers().ToArray();
                give = handlers.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.GiveBountyV1));
                transfer = handlers.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.TransferBountyV1));
                returnAny = handlers.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.ReturnBountyV1));
                returnBySourceCard = new EffectCapabilityCatalog(
                        SyntheticComponentCapabilities.Handlers())
                    .Load(SyntheticComponentCapabilities.ReturnMarkerV1);
                givePoison = TigerclawComponentContent.Handlers().Single(handler =>
                    handler.Reference.Equals(TigerclawComponentContent.GivePoisonOneV1));
            }

            public HeroId Actor { get; }
            public HeroId Enemy { get; }
            public HeroId Ally { get; }

            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();

            // ---------------------------------------------------------------------------------------
            // Driving the production handlers
            // ---------------------------------------------------------------------------------------

            public static EffectNodeId GiveNode(string suffix) =>
                new EffectNodeId("bounty-give-" + suffix);

            public EffectExecutionResult Give(
                HeroId to, bool movesWhenSupplyIsEmpty, string nodeSuffix) =>
                give.Resume(
                    Context(
                        GiveNode(nodeSuffix),
                        MoveFlag(movesWhenSupplyIsEmpty),
                        new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) }),
                    new[] { EffectValue.Hero(to) });

            public EffectExecutionResultKind AnticipateGive(HeroId to, bool movesWhenSupplyIsEmpty) =>
                give.Anticipate(
                    Context(
                        new EffectNodeId("bounty-give-probe"),
                        MoveFlag(movesWhenSupplyIsEmpty),
                        new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) }));

            public EffectExecutionResult GivePoison(HeroId to, bool movesWhenSupplyIsEmpty) =>
                givePoison.Resume(
                    Context(
                        new EffectNodeId("poison-give"),
                        MoveFlag(movesWhenSupplyIsEmpty),
                        new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) }),
                    new[] { EffectValue.Hero(to) });

            public EffectExecutionResult Transfer(HeroId to) =>
                transfer.Resume(
                    Context(TransferNodeId, null, TransferOutputs()),
                    new[] { EffectValue.Hero(to) });

            public EffectExecutionResultKind AnticipateTransfer(HeroId to) =>
                transfer.Anticipate(Context(
                    TransferNodeId,
                    new Dictionary<string, EffectValue>
                    {
                        {
                            SyntheticComponentCapabilities.MarkerTransferHandler.HolderParameterName,
                            EffectValue.Hero(to)
                        }
                    },
                    TransferOutputs()));

            public EffectExecutionResult TransferWithoutNamingARecipient() =>
                transfer.Execute(Context(TransferNodeId, null, TransferOutputs()));

            public EffectExecutionResult ReturnAny() =>
                returnAny.Execute(Context(new EffectNodeId("bounty-return"), null, ReturnOutputs()));

            public EffectExecutionResultKind AnticipateReturnAny() =>
                returnAny.Anticipate(
                    Context(new EffectNodeId("bounty-return"), null, ReturnOutputs()));

            public EffectExecutionResult SourceCardReturn() =>
                returnBySourceCard.Execute(
                    Context(new EffectNodeId("source-return"), null, ReturnOutputs()));

            public EffectExecutionResultKind AnticipateSourceCardReturn() =>
                returnBySourceCard.Anticipate(
                    Context(new EffectNodeId("source-return"), null, ReturnOutputs()));

            public EffectExecutionResult ReplayTransferOn(MarkerAttachmentRegister register, HeroId to) =>
                transfer.Resume(
                    Context(TransferNodeId, null, TransferOutputs(), register),
                    new[] { EffectValue.Hero(to) });

            /// <summary>
            /// The give and the transfer as two nodes of ONE resolution, run through the production
            /// session so the assertions read a recorded event stream rather than two handler calls.
            /// </summary>
            public EffectResolutionStepResult RunGiveThenTransfer()
            {
                var root = new EffectNodeId("bounty-sequence");
                var definition = new EffectDefinition(
                    new EffectDefinitionId("bain-marker-movement-fixture"),
                    "rev-1",
                    root,
                    new[]
                    {
                        new EffectNodeDefinition(
                            root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { GiveNodeId, TransferNodeId }),
                        new EffectNodeDefinition(
                            GiveNodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            BainComponentContent.GiveBountyV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromLiteral(
                                    SyntheticComponentCapabilities.MarkerGivingHandler
                                        .HolderParameterName,
                                    EffectValue.Hero(Enemy))
                            },
                            outputs: new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) }),
                        new EffectNodeDefinition(
                            TransferNodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            BainComponentContent.TransferBountyV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromLiteral(
                                    SyntheticComponentCapabilities.MarkerTransferHandler
                                        .HolderParameterName,
                                    EffectValue.Hero(Actor))
                            },
                            outputs: TransferOutputs())
                    });

                return EffectResolutionSession.Start(
                    new EffectResolutionId("bain-marker-movement"),
                    new CardInstanceId(CardIdValue),
                    default,
                    new ActionDefinitionId(ActionIdValue),
                    Actor,
                    Actor,
                    ActionInvocationContext.PrintedPrimary,
                    new EffectResolutionContext(
                        definition,
                        new EffectCapabilityCatalog(BainComponentContent.DeferredHandlers()),
                        participants,
                        null,
                        1,
                        1,
                        RuleModificationSet.Empty,
                        EffectCardState.Absent,
                        ActionKind.Skill,
                        components: components));
            }

            // ---------------------------------------------------------------------------------------
            // Staging
            // ---------------------------------------------------------------------------------------

            /// <summary>
            /// The bounty put out by a card that is NOT the one these operations run from, which is
            /// Narrow Escape's whole situation: the face that retrieves it gave nothing.
            /// </summary>
            public MarkerAttachment GiveBountyTo(HeroId holder) =>
                Attach(BainComponentContent.BountyDefinition, "bounty", holder, "another-card");

            /// <summary>The same marker put out by the card these operations run from.</summary>
            public MarkerAttachment GiveBountyFromThisCard(HeroId holder) =>
                Attach(BainComponentContent.BountyDefinition, "own-bounty", holder, CardIdValue);

            public MarkerAttachment GivePoisonTo(HeroId holder) =>
                Attach(
                    TigerclawComponentContent.PoisonOneDefinition, "poison", holder, "poison-source");

            public void AttachPoisonCopy(int copy) =>
                Attach(
                    TigerclawComponentContent.PoisonOneDefinition,
                    "poison-" + copy,
                    copy % 2 == 0 ? Enemy : Ally,
                    "poison-source-" + copy);

            /// <summary>
            /// The identity a give performed by one node for one holder composes, taken from the
            /// shared contract rather than from a copy of its format.
            /// </summary>
            public MarkerInstanceId ExpectedGiveIdentity(EffectNodeId nodeId, HeroId holder) =>
                MarkerInstanceIdentity.Mint(
                    Context(nodeId, null, Array.Empty<EffectOutputBinding>()), holder);

            public RuleModificationSet DerivedModifications() =>
                RuleModificationQuery.Build(
                    null,
                    null,
                    participants,
                    TieBreakerState.Unrevealed,
                    UnlockedUltimateSet.None,
                    RuleObject.None,
                    null,
                    Markers);

            // ---------------------------------------------------------------------------------------
            // The persisted marker stream, through the snapshot serializer's own private codec
            // ---------------------------------------------------------------------------------------

            public byte[] WriteMarkerBytes() => WriteMarkerBytes(Markers);

            public byte[] WriteMarkerBytes(MarkerAttachmentRegister register)
            {
                var writeMarkers = typeof(AuthoritativeSnapshotSerializer).GetMethod(
                    "WriteMarkers", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(writeMarkers, Is.Not.Null,
                    "The snapshot's marker section is still one write method.");
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    writeMarkers.Invoke(
                        null, new object[] { writer, register.CaptureRestoreState() });
                    writer.Flush();
                    return stream.ToArray();
                }
            }

            public MarkerAttachmentRegister ReadMarkerBytes(byte[] payload)
            {
                var readMarkers = typeof(AuthoritativeSnapshotSerializer).GetMethod(
                    "ReadMarkers", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(readMarkers, Is.Not.Null,
                    "The snapshot's marker section is still one read method.");
                using (var stream = new MemoryStream(payload))
                using (var reader = new BinaryReader(stream))
                {
                    var state = (MarkerAttachmentRestoreState)readMarkers.Invoke(
                        null, new object[] { reader, catalog, participants });
                    return MarkerAttachmentRegister.Restore(state);
                }
            }

            public MarkerAttachmentRegister RoundTripMarkers() => ReadMarkerBytes(WriteMarkerBytes());

            // ---------------------------------------------------------------------------------------
            // Helpers
            // ---------------------------------------------------------------------------------------

            private static EffectOutputBinding[] TransferOutputs() => new[]
            {
                new EffectOutputBinding("holder", EffectValueKind.Hero),
                new EffectOutputBinding(
                    SyntheticComponentCapabilities.MarkerTransferHandler.PreviousHolderOutputName,
                    EffectValueKind.Hero)
            };

            private static EffectOutputBinding[] ReturnOutputs() =>
                new[] { new EffectOutputBinding("returned", EffectValueKind.Hero) };

            private static IReadOnlyDictionary<string, EffectValue> MoveFlag(bool moves) =>
                new Dictionary<string, EffectValue>
                {
                    {
                        SyntheticComponentCapabilities.MarkerGivingHandler
                            .MovesWhenSupplyIsEmptyParameterName,
                        EffectValue.Boolean(moves)
                    }
                };

            private MarkerAttachment Attach(
                MarkerDefinition definition, string instancePrefix, HeroId holder, string sourceCard)
            {
                var attachment = new MarkerAttachment(
                    new MarkerInstanceId($"{instancePrefix}-{holder.Value}"),
                    definition,
                    Actor,
                    holder,
                    new CardInstanceId(sourceCard),
                    new ActionDefinitionId(ActionIdValue));
                Markers.Give(attachment);
                return attachment;
            }

            private EffectCapabilityContext Context(
                EffectNodeId nodeId,
                IReadOnlyDictionary<string, EffectValue> inputs,
                IReadOnlyList<EffectOutputBinding> declaredOutputs,
                MarkerAttachmentRegister register = null) =>
                new EffectCapabilityContext(
                    nodeId,
                    EffectFailurePolicy.Mandatory,
                    Actor,
                    Actor,
                    new CardInstanceId(CardIdValue),
                    new CardDefinitionReference(
                        new CardDefinitionId("bain-marker-fixture"), "fixture-rev-1"),
                    new ActionDefinitionId(ActionIdValue),
                    inputs,
                    1,
                    1,
                    participants,
                    null,
                    RuleModificationSet.Empty,
                    EffectCardState.Absent,
                    ActionKind.Skill,
                    register == null
                        ? components
                        : new MatchComponents(
                            catalog,
                            components.Tokens,
                            register,
                            components.Board,
                            components.Units,
                            components.Exchange),
                    declaredOutputs: declaredOutputs);

            private static HexCoordinate[] FeaturelessDisk(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates.ToArray();
            }
        }
    }
}
