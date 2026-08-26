using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.23. A marker supplies its declared rules to the hero holding it, for exactly as long
    /// as that hero holds it.
    ///
    /// <para>Rulebook p.18 makes markers components "usually given to other heroes", and milestone 6.2
    /// built the whole attachment lifecycle - a finite supply, a holder, and p.8's and p.13's two return
    /// checkpoints - without any marker carrying a rule, because none in verified content did.
    /// Tigerclaw's Poisoned Dagger and Poisoned Dart are the first that do: "The hero with a poison
    /// marker has -1 Initiative, -1 Attack and -1 Defense."</para>
    ///
    /// <para>These cases are hero-neutral and use a synthetic marker, because what is under test is the
    /// source family rather than Tigerclaw's numbers. His two faces get their own coverage.</para>
    /// </summary>
    public sealed class MarkerRuleSourceTests
    {
        private static readonly HeroId Red =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        // ---------------------------------------------------------------------------------------
        // The rule reaches the holder
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AHeldMarker_LowersTheHoldersValuesAndNobodyElses()
        {
            var fixture = Fixture();
            Give(fixture, WeakeningMarker(), owner: Red, holder: Blue);

            var set = Build(fixture);

            Assert.That(
                ValueAmountFor(set, RuleObject.ForHero(Blue), PrintedValueChannel.Attack),
                Is.EqualTo(-2),
                "p.18's marker is given to another hero, and the printed sentence names the hero " +
                "with the marker as the one whose values move.");
            Assert.That(
                ValueAmountFor(set, RuleObject.ForHero(Red), PrintedValueChannel.Attack),
                Is.EqualTo(0),
                "The hero who gave it keeps their own values; owner is provenance, holder is the rule.");
        }

        [Test]
        public void AHeldMarker_SuppliesEveryChannelItDeclares()
        {
            var fixture = Fixture();
            Give(fixture, WeakeningMarker(), owner: Red, holder: Blue);

            var set = Build(fixture);
            var affected = RuleObject.ForHero(Blue);

            Assert.That(ValueAmountFor(set, affected, PrintedValueChannel.Initiative), Is.EqualTo(-1));
            Assert.That(ValueAmountFor(set, affected, PrintedValueChannel.Attack), Is.EqualTo(-2));
            Assert.That(ValueAmountFor(set, affected, PrintedValueChannel.Defense), Is.EqualTo(-3),
                "Three declared properties are three modifications; one RuleModification names one " +
                "channel, so a marker that changes three values declares three.");
        }

        [Test]
        public void AMarkerGivenToAFriendlyHero_WeakensThatHeroJustTheSame()
        {
            var fixture = Fixture();
            Give(fixture, WeakeningMarker(), owner: Red, holder: Red);

            Assert.That(
                ValueAmountFor(Build(fixture), RuleObject.ForHero(Red), PrintedValueChannel.Attack),
                Is.EqualTo(-2),
                "Tigerclaw's two Green cards print \"a hero\" rather than \"an enemy hero\", and " +
                "nothing in the derivation consults the two heroes' sides.");
        }

        // ---------------------------------------------------------------------------------------
        // The rule ends exactly when the attachment does
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AReturnedMarker_SuppliesNothing()
        {
            var fixture = Fixture();
            var attachment = Give(fixture, WeakeningMarker(), owner: Red, holder: Blue);

            Assert.That(
                ValueAmountFor(Build(fixture), RuleObject.ForHero(Blue), PrintedValueChannel.Attack),
                Is.EqualTo(-2),
                "held");

            fixture.Markers.Return(attachment.Id);

            Assert.That(Build(fixture).Modifications, Is.Empty,
                "p.8 returns every marker at end of round and p.13 returns a defeated hero's. Both " +
                "run through the register, so the rule ends with the attachment it was derived from " +
                "and no cleanup of its own is needed.");
        }

        [Test]
        public void AMarkerDeclaringNoRule_SuppliesNothingWhileStillBeingHeld()
        {
            var fixture = Fixture();
            Give(fixture, SyntheticComponentDefinitions.FocusDefinition, owner: Red, holder: Blue);

            Assert.That(Build(fixture).Modifications, Is.Empty,
                "Milestone 6.2's markers carry lifetime and no rule, and adding the seam must not " +
                "have given them one.");
            Assert.That(fixture.Markers.Attachments.Count, Is.EqualTo(1),
                "and the attachment itself is untouched.");
        }

        // ---------------------------------------------------------------------------------------
        // Provenance and ordering
        // ---------------------------------------------------------------------------------------

        [Test]
        public void EachSuppliedRule_NamesItsOwnMarkerInstanceAsItsSource()
        {
            var fixture = Fixture();
            var attachment = Give(fixture, WeakeningMarker(), owner: Red, holder: Blue);

            foreach (var applied in Build(fixture).Modifications)
            {
                Assert.That(applied.SourceKind, Is.EqualTo(RuleModificationSourceKind.ContinuousSource),
                    "A marker property is a continuous source, not an Active effect: nothing creates " +
                    "it into the lifecycle and no checkpoint expires it.");
                Assert.That(applied.SourceId, Does.StartWith($"marker:{attachment.Id.Value}:property:"),
                    "Every answer can name the exact marker instance that produced it.");
            }
        }

        [Test]
        public void TwoMarkersOnOneHolder_AreOrderedByInstanceIdentityRatherThanByGiveOrder()
        {
            var later = Fixture();
            Give(later, WeakeningMarker("marker-b"), owner: Red, holder: Blue, instanceId: "instance-b");
            Give(later, WeakeningMarker("marker-a"), owner: Red, holder: Blue, instanceId: "instance-a");

            var sources = Build(later).Modifications.Select(value => value.SourceId).ToArray();

            Assert.That(
                sources.Select(value => value.Split(':')[1]).Distinct(),
                Is.EqualTo(new[] { "instance-a", "instance-b" }),
                "p.17 orders simultaneous modifications, so the sequence has to be a fact about the " +
                "instances rather than about the order two gives happen to have run in.");
        }

        // ---------------------------------------------------------------------------------------
        // The declaration itself
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AMarkerDefinition_RefusesDuplicatePropertyIdentities()
        {
            var refusal = Assert.Throws<ArgumentException>(() => new MarkerDefinition(
                new MarkerDefinitionId("duplicated"),
                1,
                "TEST - Duplicated",
                1,
                MarkerReturnCondition.GeneralCleanup,
                new[]
                {
                    Property("same", PrintedValueChannel.Attack, -1),
                    Property("same", PrintedValueChannel.Defense, -1)
                }));

            Assert.That(refusal.ParamName, Is.EqualTo("properties"),
                "The property index is what binds a declaration to its derived source, so two " +
                "properties sharing an identity would produce two sources nothing could tell apart.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private sealed class MarkerFixture
        {
            public MarkerFixture(AdjacentMoveAuthority board, MatchParticipants participants)
            {
                Board = board;
                Participants = participants;
                Markers = new MarkerAttachmentRegister();
            }

            public AdjacentMoveAuthority Board { get; }
            public MatchParticipants Participants { get; }
            public MarkerAttachmentRegister Markers { get; }
        }

        private static MarkerFixture Fixture()
        {
            var board = new AdjacentMoveAuthority(
                RadiusThreeCoordinates(),
                Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
            return new MarkerFixture(board, StagedBoardParticipants.Create());
        }

        /// <summary>
        /// A board with no piece on it. Nothing here reads occupancy: a marker.s lifetime is its
        /// attachment rather than anybody.s position, which is the fact <see
        /// cref="AReturnedMarker_SuppliesNothing"/> turns on.
        /// </summary>
        private static IEnumerable<HexCoordinate> RadiusThreeCoordinates()
        {
            for (var q = -3; q <= 3; q++)
            {
                for (var r = -3; r <= 3; r++)
                {
                    if (Math.Abs(q + r) <= 3) yield return new HexCoordinate(q, r);
                }
            }
        }

        /// <summary>The total one channel receives, through the accessor every value composition uses.</summary>
        private static int ValueAmountFor(
            RuleModificationSet set, RuleObject affected, PrintedValueChannel channel) =>
            set.ValueContributions(affected, channel, RuleValueApplicability.Always)
                .Sum(contribution => contribution.Amount);

        private static RuleModificationSet Build(MarkerFixture fixture) =>
            RuleModificationQuery.Build(
                null,
                fixture.Board,
                fixture.Participants,
                TieBreakerState.Unrevealed,
                UnlockedUltimateSet.None,
                RuleObject.None,
                null,
                fixture.Markers);

        private static MarkerAttachment Give(
            MarkerFixture fixture,
            MarkerDefinition definition,
            HeroId owner,
            HeroId holder,
            string instanceId = "marker-instance")
        {
            var attachment = new MarkerAttachment(
                new MarkerInstanceId(instanceId),
                definition,
                owner,
                holder,
                new CardInstanceId("marker-source-card"),
                new ActionDefinitionId("marker-source-action"));
            fixture.Markers.Give(attachment);
            return attachment;
        }

        /// <summary>
        /// A marker printing three distinct amounts on three channels, so a test that reads one cannot
        /// pass by reading another.
        /// </summary>
        private static MarkerDefinition WeakeningMarker(string id = "weakening") =>
            new MarkerDefinition(
                new MarkerDefinitionId(id),
                1,
                "TEST - Weakening",
                4,
                MarkerReturnCondition.GeneralCleanup,
                new[]
                {
                    Property("initiative", PrintedValueChannel.Initiative, -1),
                    Property("attack", PrintedValueChannel.Attack, -2),
                    Property("defense", PrintedValueChannel.Defense, -3)
                });

        private static MarkerPropertyDefinition Property(
            string propertyId, PrintedValueChannel channel, int amount) =>
            new MarkerPropertyDefinition(
                propertyId,
                "value-modifier",
                "while-held",
                RuleModification.ForValue(channel, amount));
    }
}
