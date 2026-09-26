using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75a. The live Tie Breaker coin reaching the card text that branches on it.
    ///
    /// <para><b>Why this fixture exists, and what it holds.</b> Every one of Ignatia the Mad's action
    /// cards is composed as a Conditional on <c>ignatia-coin-sun@1</c>, which reads the revealed face
    /// off <see cref="EffectCapabilityContext.TieBreakerRevealedSide"/>. A path that walked her card
    /// text without a face reports the predicate impossible, which stops the action - and at OFFER
    /// time that greys the card. So the question "did this path carry the coin?" is not a nicety on
    /// her set; it is what makes the set playable, and it is asked here of the live dispatcher rather
    /// than path by path.</para>
    ///
    /// <para>Nothing of hers is registered - milestone 7.9's gate - so the card walked below is built
    /// in this fixture out of her own public coin branch
    /// (<see cref="IgnatiaEffectContent.CoinBranched"/>) with two witness arms, over a catalog that
    /// holds her deferred handlers and the witness. That is the same shape her real faces have, with
    /// the board work taken out of it.</para>
    /// </summary>
    public sealed class IgnatiaCoinThreadingTests
    {
        private static readonly CardDefinitionId WitnessCard = new CardDefinitionId("coin-witness");
        private const string Revision = "coin-witness-rev-1";

        [Test]
        public void AResolutionThatCarriedNoCoinStopsAtTheGateAndRunsNeitherLine()
        {
            var unbound = new Harness(coin: null);
            var started = unbound.StartSkill();

            Assert.That(started.Accepted, Is.True, "the action is offered and started.");
            Assert.That(unbound.Witness.Arms, Is.Empty,
                "ignatia-coin-sun@1 refuses an unrevealed face rather than guessing one, and p.10 " +
                "stops an action at a mandatory step nobody can take - so a path that did not carry " +
                "the coin runs NEITHER of her two printed lines. This is the failure a missing " +
                "thread produces, stated so the fix is not merely assumed.");
        }

        [Test]
        public void TheSunFaceRunsTheSunLineAndTheMoonFaceTheMoonLine()
        {
            var sun = new Harness(new StubCoin(IgnatiaComponentContent.SunFace));
            Assert.That(sun.StartSkill().Accepted, Is.True);
            Assert.That(sun.Witness.Arms, Is.EqualTo(new[] { Arm.Sun }),
                "\"Apply the text matching the symbol on the Tie Breaker coin\" - one line runs.");
            Assert.That(sun.Witness.Sides, Is.EqualTo(new[] { IgnatiaComponentContent.SunFace }),
                "and the node inside that line was handed the same face the match is showing.");

            var moon = new Harness(new StubCoin(IgnatiaComponentContent.MoonFace));
            Assert.That(moon.StartSkill().Accepted, Is.True);
            Assert.That(moon.Witness.Arms, Is.EqualTo(new[] { Arm.Moon }));
            Assert.That(moon.Witness.Sides, Is.EqualTo(new[] { IgnatiaComponentContent.MoonFace }));
        }

        [Test]
        public void TheCoinIsReadLiveSoAFlipChangesWhichLineTheNextResolutionRuns()
        {
            var coin = new StubCoin(IgnatiaComponentContent.SunFace);
            var harness = new Harness(coin);

            Assert.That(harness.StartSkill().Accepted, Is.True);
            Assert.That(harness.Witness.Arms, Is.EqualTo(new[] { Arm.Sun }));

            harness.Witness.Reset();
            harness.ResetResolution();
            coin.FlipTieBreaker();

            Assert.That(harness.StartSkill().Accepted, Is.True);
            Assert.That(harness.Witness.Arms, Is.EqualTo(new[] { Arm.Moon }),
                "the dispatcher is bound to the coin rather than handed a copy of the face it was " +
                "showing when the match was composed - which is what makes Chaos Incarnate's flip " +
                "reach the performance that follows it.");
        }

        [Test]
        public void ATurnBindsItsOwnCoinToTheDispatcherWithoutBeingAsked()
        {
            var harness = new Harness(coin: null);
            var turn = harness.StartTurnOver(TeamSide.Blue);

            Assert.That(harness.StartSkill().Accepted, Is.True);
            Assert.That(harness.Witness.Sides, Is.EqualTo(new[] { TeamSide.Blue }),
                "the turn owns the Initiative state that holds the coin, so composing one is what " +
                "hands the dispatcher a face - no caller has to remember to.");
            Assert.That(turn.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void ACardFlipReachesTheTurnsOwnCoinAndEveryLaterReadOfIt()
        {
            var harness = new Harness(coin: null);
            var turn = harness.StartTurnOver(TeamSide.Red);
            var flipped = ((IMatchTieBreakerCoin)turn).FlipTieBreaker();

            Assert.That(flipped, Is.EqualTo(TeamSide.Blue));
            Assert.That(turn.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue),
                "the flip is applied to the authority that HOLDS the coin - which is the state the " +
                "round section of the snapshot serializes, so it survives a restore with no field " +
                "of its own.");

            harness.Witness.Reset();
            Assert.That(harness.StartSkill().Accepted, Is.True);
            Assert.That(harness.Witness.Arms, Is.EqualTo(new[] { Arm.Sun }),
                "and every read after the flip is the new face.");
        }

        [Test]
        public void TheRoundAuthorityAnswersTheCoinForTheMomentsBetweenTurns()
        {
            Assert.That(
                typeof(GuardsDigital.Core.Cards.RoundResolutionAuthority)
                    .GetInterfaces(),
                Has.Member(typeof(IMatchTieBreakerCoin)),
                "a checkpoint visited between turns - the round's own - still walks card text, so " +
                "the round answers which face is showing when no turn is running.");
            Assert.That(
                typeof(GuardsDigital.Core.Cards.TurnResolutionAuthority).GetInterfaces(),
                Has.Member(typeof(IMatchTieBreakerCoin)));
        }

        // -------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------

        private enum Arm
        {
            Sun = 1,
            Moon = 0
        }

        private sealed class StubCoin : IMatchTieBreakerCoin
        {
            public StubCoin(TeamSide showing)
            {
                TieBreakerState = new TieBreakerState(showing);
            }

            public TieBreakerState TieBreakerState { get; private set; }

            public TeamSide FlipTieBreaker()
            {
                TieBreakerState = new TieBreakerState(
                    TieBreakerState.RevealedSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red);
                return TieBreakerState.RevealedSide;
            }
        }

        /// <summary>
        /// A capability that does nothing and remembers two things: which arm of the coin branch
        /// reached it, and the face the context it was handed was carrying. It is registered only in
        /// this fixture's own catalog.
        /// </summary>
        private sealed class WitnessHandler : IEffectCapabilityHandler
        {
            public const string ArmParameter = "arm";

            public EffectCapabilityReference Reference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("coin-witness"), 1);

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(ArmParameter, EffectValueKind.Integer) },
                    Array.Empty<EffectOutputBinding>());

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public List<Arm> Arms { get; } = new List<Arm>();

            public List<TeamSide> Sides { get; } = new List<TeamSide>();

            public void Reset()
            {
                Arms.Clear();
                Sides.Clear();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                Sides.Add(context.TieBreakerRevealedSide);
                Arms.Add((Arm)context.Inputs[ArmParameter].IntegerValue);
                return EffectExecutionResult.Applied(Array.Empty<EffectOutcomeFact>());
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                Sides.Add(context.TieBreakerRevealedSide);
                Arms.Add((Arm)context.Inputs[ArmParameter].IntegerValue);
                return EffectExecutionResultKind.Applied;
            }
        }

        /// <summary>
        /// A live dispatcher over one card whose body is Ignatia's own coin branch, with a witness at
        /// the end of each arm.
        /// </summary>
        private sealed class Harness
        {
            private readonly MatchConfiguration configuration;
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] instances;
            private readonly CardInstance card;
            private readonly ActionOffer offer;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly CardActionResolutionAuthority cardActions;

            public Harness(IMatchTieBreakerCoin coin)
            {
                Witness = new WitnessHandler();
                configuration =
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
                var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                var definition = WitnessDefinition(Witness);
                card = CardInstance.CreateInHand(
                    new CardInstanceId("coin-witness-card"), definition.Reference, actor);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                definitions = configuration.CardDefinitions.Concat(new[] { definition })
                    .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
                instances = configuration.CardInstances.Concat(new[] { card }).ToArray();

                var coordinates = new List<HexCoordinate>();
                for (var q = -3; q <= 3; q++)
                for (var r = -3; r <= 3; r++)
                {
                    if (Math.Abs(q + r) <= 3) coordinates.Add(new HexCoordinate(q, r));
                }

                Board = new AdjacentMoveAuthority(
                    coordinates,
                    configuration.Participants.Heroes.Select((hero, index) =>
                        new HeroOccupancy(hero.Id, coordinates[index])),
                    Enumerable.Empty<ObstacleOccupancy>(),
                    Enumerable.Empty<QuickGameMinionSetupState>());

                cardActions = new CardActionResolutionAuthority(
                    new MatchContentCatalogs(
                        SyntheticEffectCapabilities.CreateCatalog(
                            IgnatiaComponentContent.RegisteredHandlers()
                                .Concat(new IEffectCapabilityHandler[] { Witness })),
                        SyntheticComponentDefinitions.CreateCatalog(),
                        new TriggeredEffectCatalog(
                            IgnatiaEffectContent.TriggeredEffects(
                                IgnatiaCardContent.ContentRevision))),
                    Board);

                if (coin != null)
                {
                    typeof(CardActionResolutionAuthority)
                        .GetMethod(
                            "BindTieBreakerCoin",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic)
                        .Invoke(cardActions, new object[] { coin });
                }

                offer = new ActionOffer(
                    card.Id,
                    definition.Reference,
                    new ActionDefinitionId("primary-skill"),
                    actor,
                    ActionInvocationContext.PrintedPrimary,
                    true);
            }

            public WitnessHandler Witness { get; }

            public AdjacentMoveAuthority Board { get; }

            /// <summary>Clears the finished resolution so the same card can be walked again.</summary>
            public void ResetResolution()
            {
                if (cardActions.PendingSkillResolution != null)
                {
                    throw new InvalidOperationException("The previous walk did not finish.");
                }
            }

            public bool CanApplyAnything() =>
                cardActions.CanSkillApplyAnything(
                    offer, definitions, instances, configuration.Participants, timing,
                    RuleModificationSet.Empty, 1, 1, out _);

            public CardActionResolutionResult StartSkill() =>
                cardActions.StartTurnSkill(
                    new ActionResolutionId("coin-witness-skill"), offer, definitions, instances,
                    configuration.Participants, timing, RuleModificationSet.Empty,
                    TieBreakerState.Unrevealed, 1, 1);

            /// <summary>
            /// Composes a turn over this dispatcher, which is the only thing this method does: the
            /// binding under test is the one the turn's own constructor makes.
            /// </summary>
            public GuardsDigital.Core.Cards.TurnResolutionAuthority StartTurnOver(TeamSide showing) =>
                new GuardsDigital.Core.Cards.TurnResolutionAuthority(
                    definitions, instances, configuration.Participants,
                    new TieBreakerState(showing), cardActions, timing, 1, 1);
        }

        private static CardDefinition WitnessDefinition(WitnessHandler witness)
        {
            var sun = new EffectNodeId("witness-sun");
            var moon = new EffectNodeId("witness-moon");
            var body = IgnatiaEffectContent.CoinBranched(
                WitnessCard,
                Revision,
                sun,
                moon,
                new[]
                {
                    WitnessNode(witness, sun, Arm.Sun),
                    WitnessNode(witness, moon, Arm.Moon)
                });

            return new CardDefinition(
                WitnessCard,
                Revision,
                "Coin witness",
                new InitiativeValue(10),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        body)
                });
        }

        private static EffectNodeDefinition WitnessNode(
            WitnessHandler witness, EffectNodeId id, Arm arm) =>
            new EffectNodeDefinition(
                id,
                EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                witness.Reference,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral(
                        WitnessHandler.ArmParameter, EffectValue.Integer((int)arm))
                });
    }
}
