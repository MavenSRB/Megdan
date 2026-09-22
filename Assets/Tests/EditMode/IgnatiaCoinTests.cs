using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's one mechanism no dealt hero carries: "Apply the text matching
    /// the symbol on the Tie Breaker coin." Every action card of hers prints TWO texts, a sun (☀) and a
    /// moon (☾), and the live coin face picks which one runs - which is composed as a single
    /// <see cref="EffectNodeKind.Conditional"/> on <c>ignatia-coin-sun@1</c> whose then-branch is the
    /// sun line and whose else-branch is the moon line.
    ///
    /// <para><b>What this fixture measures and the block fixtures do not.</b> The other six prove what
    /// each LINE does; this proves that the coin is what chooses between them, that the choice is
    /// re-read on every resolution rather than baked into the content, that Equilibrium takes the
    /// choice away from the coin for exactly one round, and that nothing of hers can resolve with the
    /// coin unrevealed.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaCoinTests
    {
        /// <summary>
        /// One board every case here re-uses: an enemy hero and an enemy minion in a straight line from
        /// her, a second enemy hero off every file, and an ally behind her.
        /// </summary>
        private static AdjacentMoveAuthority Board() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, new HexCoordinate(0, 2)),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(2, 0)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(2, -1))
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, new HexCoordinate(1, 0))
                });

        // -------------------------------------------------------------------------------------------
        // The mapping
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// USER-SETTLED 2026-09-22 ("Moon is blue team, sun is read team"): her two printed symbols are
        /// the two sides of p.7's Tie Breaker coin, sun on Red and moon on Blue. The mapping is total
        /// over the three <see cref="TeamSide"/> values, opposite on the two real faces, and it refuses
        /// the third - a coin nobody has revealed is not her sun and not her moon.
        /// </summary>
        [Test]
        public void TheSunIsTheRedFaceAndTheMoonTheBlue_AndAnUnrevealedCoinIsNeither()
        {
            Assert.That(IgnatiaComponentContent.SunFace, Is.EqualTo(TeamSide.Red));
            Assert.That(IgnatiaComponentContent.MoonFace, Is.EqualTo(TeamSide.Blue));

            Assert.That(IgnatiaComponentContent.RevealedFaceIsSun(TeamSide.Red), Is.True);
            Assert.That(IgnatiaComponentContent.RevealedFaceIsSun(TeamSide.Blue), Is.False);
            Assert.That(IgnatiaComponentContent.RevealedFaceIsSun(TeamSide.None), Is.False,
                "An unrevealed coin is not her sun, which is what makes the refusal below a refusal " +
                "rather than a silent moon.");

            Assert.That(IgnatiaComponentContent.IsRevealed(TeamSide.Red), Is.True);
            Assert.That(IgnatiaComponentContent.IsRevealed(TeamSide.Blue), Is.True);
            Assert.That(IgnatiaComponentContent.IsRevealed(TeamSide.None), Is.False);
        }

        /// <summary>
        /// The predicate refuses rather than guessing. Her cards are never resolved with the coin
        /// unrevealed in a real match - p.7 reveals it before any card is played - so a frame that
        /// reached one is a bug somewhere else, and answering "moon" would hide it inside a legal-looking
        /// resolution.
        /// </summary>
        [Test]
        public void WithTheCoinUnrevealed_HerTextRefusesRatherThanChoosingAFace()
        {
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, TeamSide.None, Board());

            Assert.That(frame.Step.IsComplete, Is.True,
                "The mandatory gate cannot be performed, so the walk ends rather than pausing.");
            Assert.That(
                frame.Outcome(IgnatiaEffectContent.CoinGateNode),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and it ends by REFUSING - the coin was not revealed, so neither printed line is the " +
                "one to apply and the predicate says so instead of defaulting to the else-branch.");
            Assert.That(frame.Choice, Is.Null,
                "nobody is asked anything: which line applies is a fact about the coin, never a " +
                "decision.");
        }

        // -------------------------------------------------------------------------------------------
        // The branch
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE MEASUREMENT. One card, one board, one instant - and the coin is the only thing
        /// that differs between the two runs, so the arm that runs is what the coin chose.
        ///
        /// <para>Playing with Fire prints ☀ "a unit in range not in a straight line" and ☾ "a unit in
        /// range in a straight line", and the board is arranged so the two sets are disjoint: the
        /// second enemy stands off every file and the first enemy and the minion stand on one.</para>
        /// </summary>
        [Test]
        public void TheCoinChoosesWhichPrintedLineRuns_AndTheTwoLinesAdmitDifferentSubjects()
        {
            var sun = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Sun, Board());
            Assert.That(sun.Choice, Is.Not.Null);
            Assert.That(sun.Choice.NodeId.Value, Is.EqualTo("ignatia-attack-sun-select"),
                "The Red face is her sun, so the SUN arm is the one that runs.");
            Assert.That(sun.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.SecondEnemy.Value }),
                "☀ \"a unit in range NOT in a straight line\" - only the enemy standing off every " +
                "file qualifies.");

            var moon = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Moon, Board());
            Assert.That(moon.Choice, Is.Not.Null);
            Assert.That(moon.Choice.NodeId.Value, Is.EqualTo("ignatia-attack-moon-select"),
                "and the Blue face is her moon, so the MOON arm is.");
            Assert.That(
                moon.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }),
                "☾ \"a unit in range IN a straight line\" - the two standing on one, and p.19's units " +
                "are heroes AND minions.");

            Assert.That(
                sun.Candidates().Intersect(moon.Candidates()), Is.Empty,
                "and on this board the two printed lines share no subject at all, so neither result " +
                "could have come from the other arm.");
        }

        /// <summary>
        /// The branch is a property of the SET, not of one card: every composed face of hers roots in
        /// the one Conditional on the one predicate - except her Silver, which is the card that frees
        /// the choice and therefore cannot be gated by it.
        /// </summary>
        [Test]
        public void EveryComposedFace_RootsInTheOneCoinConditional_ExceptTheSilverThatFreesIt()
        {
            var branched = 0;
            foreach (var definition in IgnatiaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions
                    .Where(action => action.EffectRoot != null))
                {
                    if (definition.Id.Equals(IgnatiaCardContent.Equilibrium))
                    {
                        Assert.That(action.EffectRoot.Root.Capability,
                            Is.EqualTo(IgnatiaComponentContent.EquilibriumV1),
                            "Equilibrium is the one face that is not coin-branched: it creates the " +
                            "marker that frees the choice, and a card gated by the coin could not.");
                        continue;
                    }

                    branched++;
                    Assert.That(action.EffectRoot.Root.Id.Value,
                        Is.EqualTo(IgnatiaEffectContent.CoinGateNode), definition.Id.Value);
                    Assert.That(action.EffectRoot.Root.Kind, Is.EqualTo(EffectNodeKind.Conditional),
                        definition.Id.Value + " branches rather than asking.");
                    Assert.That(action.EffectRoot.Root.Capability,
                        Is.EqualTo(IgnatiaComponentContent.CoinSunV1), definition.Id.Value);
                    Assert.That(action.EffectRoot.Root.ChildNodeIds, Has.Count.EqualTo(2),
                        definition.Id.Value + " has exactly two arms, because the card prints two " +
                        "texts and never a third.");
                }
            }

            Assert.That(branched, Is.EqualTo(17),
                "Seventeen coin-branched bodies: her Gold basic, the five Reds, the five Blues, the " +
                "five Greens and the boxed Handicap. The eighteenth composed body is Equilibrium's " +
                "and the nineteenth face, the Ultimate, composes none.");
        }

        // -------------------------------------------------------------------------------------------
        // Equilibrium
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "This round: Each time you perform or repeat a primary action you may apply either ☀ or ☾
        /// card text, regardless of the Tie Breaker coin." One marker, on HER, for the round.
        /// </summary>
        [Test]
        public void Equilibrium_StandsOneRoundLongMarkerOnTheActingHero()
        {
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.Equilibrium, IgnatiaBoard.Sun, Board());

            Assert.That(frame.Step.IsComplete, Is.True,
                "The Silver asks nothing: it creates the marker and ends.");
            Assert.That(frame.Choice, Is.Null);

            var marker = frame.Timing.Effects.Single();
            Assert.That(marker.Definition.EffectKind,
                Is.EqualTo(IgnatiaComponentContent.FreeCoinChoiceKind));
            Assert.That(marker.ControllerHeroId, Is.EqualTo(IgnatiaBoard.Ignatia),
                "\"you\" is the hero who performed it, and a ControllerHero-scoped occurrence carries " +
                "that hero on its controller.");
            Assert.That(marker.Definition.AffectedScope,
                Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
            Assert.That(marker.Definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisRound),
                "\"This round:\" - p.8's round, not her turn.");
            Assert.That(marker.Definition.Modifications, Is.Empty,
                "and it changes no rule of its own: it is a flag the coin predicate reads, which is " +
                "why nothing about targeting, movement or values moves while it stands.");
        }

        /// <summary>
        /// THE SECOND HEADLINE MEASUREMENT. While the marker stands the coin is NOT READ at all: the
        /// predicate asks the actor which face to apply, and BOTH printed lines stay reachable whatever
        /// the coin shows - which is the whole of "regardless of the Tie Breaker coin".
        ///
        /// <para>The two answers are taken from the offer rather than written out as numbers. What the
        /// card promises is that either printed text may be applied, not that a particular integer
        /// means sun: the integers are the pause's own vocabulary and belong to the block that words
        /// it, so a case that named them would be asserting somebody else's spelling.</para>
        /// </summary>
        [Test]
        public void UnderEquilibrium_TheActorPicksTheFace_AndEitherLineStaysReachable()
        {
            foreach (var coin in new[] { IgnatiaBoard.Sun, IgnatiaBoard.Moon })
            {
                var opening = new IgnatiaFrame(
                    IgnatiaCardContent.PlayingWithFire, coin, Board(),
                    timing: EquilibriumTiming(coin));

                Assert.That(opening.Choice, Is.Not.Null,
                    "The predicate stops and asks instead of reading the coin.");
                Assert.That(opening.Choice.NodeId.Value,
                    Is.EqualTo(IgnatiaEffectContent.CoinGateNode),
                    "and the question is raised by the gate itself, which is the node whose answer " +
                    "the branch is.");

                var answers = opening.Choice.Candidates.Select(candidate => candidate.Value).ToArray();
                Assert.That(answers, Has.Length.EqualTo(2),
                    "Two answers and no more - the card prints two texts and never a third.");
                Assert.That(
                    answers.All(value => value.Kind == EffectValueKind.Integer), Is.True,
                    "each naming one of the two printed faces.");

                // Each answer, on its own fresh board, so what is measured is which line it applies.
                var arms = answers
                    .Select(answer =>
                    {
                        var frame = new IgnatiaFrame(
                            IgnatiaCardContent.PlayingWithFire, coin, Board(),
                            timing: EquilibriumTiming(coin));
                        frame.Pick(answer);
                        return frame.Choice.NodeId.Value;
                    })
                    .ToArray();

                Assert.That(
                    arms,
                    Is.EquivalentTo(new[]
                    {
                        "ignatia-attack-sun-select", "ignatia-attack-moon-select"
                    }),
                    "One answer applies the SUN text and the other the MOON text, on a board where " +
                    "the coin shows " + coin + " either way - so the line that runs is the one the " +
                    "ACTOR named and not the one the coin did.");
            }
        }

        /// <summary>A lifecycle with Equilibrium's marker standing on her, performed rather than built.</summary>
        private static ActiveEffectLifecycle EquilibriumTiming(TeamSide coin)
        {
            var silver = new IgnatiaFrame(IgnatiaCardContent.Equilibrium, coin, Board());
            Assert.That(silver.Step.IsComplete, Is.True);
            return silver.Timing;
        }

        /// <summary>
        /// "This round:" ends. Once p.8's round-end checkpoint has run the marker is no longer standing,
        /// so the next round's primary action reads the coin again rather than asking.
        /// </summary>
        [Test]
        public void TheMarkerEndsWithTheRound_AndTheCoinIsReadAgainAfterIt()
        {
            var board = Board();
            var silver = new IgnatiaFrame(IgnatiaCardContent.Equilibrium, IgnatiaBoard.Sun, board);

            silver.Timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.RoundEnd, 1, 9),
                new TieBreakerState(TeamSide.Blue));

            Assert.That(
                silver.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended),
                "p.8 removes it at the end of the round it was played in.");

            var next = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Sun, board,
                timing: silver.Timing, roundIndex: 2);

            Assert.That(next.Choice.NodeId.Value, Is.EqualTo("ignatia-attack-sun-select"),
                "so the next round's card reads the coin again and runs the face it shows, with " +
                "nobody asked - an ENDED marker is not a standing one.");
        }

        /// <summary>
        /// p.13 and p.17: an Active effect on the card of a hero who is defeated ends with them. Her
        /// Silver declares exactly that cancellation, and it is the controller's Defeat that triggers
        /// it rather than anybody else's.
        /// </summary>
        [Test]
        public void TheMarkerIsCancelledWhenSheIsDefeated_AndNotWhenAnotherHeroIs()
        {
            var board = Board();
            var silver = new IgnatiaFrame(IgnatiaCardContent.Equilibrium, IgnatiaBoard.Sun, board);

            Assert.That(
                IgnatiaComponentContent.FreeCoinChoiceDefinition.CancellationConditions,
                Has.Member(ActiveEffectCancellationCondition.ControllerHeroDefeated));

            silver.Timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.ControllerHeroDefeated, 1, 2, IgnatiaBoard.Enemy),
                new TieBreakerState(TeamSide.Blue));
            Assert.That(
                silver.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active),
                "An enemy's Defeat is nothing to do with the card she played.");

            silver.Timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.ControllerHeroDefeated, 1, 3, IgnatiaBoard.Ignatia),
                new TieBreakerState(TeamSide.Blue));
            Assert.That(
                silver.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended),
                "and hers ends it, which is the cancellation her Silver declares.");

            var after = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Moon, board,
                timing: silver.Timing);
            Assert.That(after.Choice.NodeId.Value, Is.EqualTo("ignatia-attack-moon-select"),
                "so the free choice is gone with it and the coin decides again.");
        }

        /// <summary>
        /// The marker frees HER choice and nobody else's. It is ControllerHero-scoped, so a second
        /// hero's resolution under the same lifecycle still reads the coin - which matters because the
        /// engine's own <c>IsRelevantHero</c> reads a ControllerHero occurrence by its CONTROLLER, and
        /// a predicate asking any other field would match either everybody or nobody.
        /// </summary>
        [Test]
        public void TheFreeChoiceIsHersAlone_AndAnotherHeroUnderTheSameLifecycleStillReadsTheCoin()
        {
            var board = Board();
            var silver = new IgnatiaFrame(IgnatiaCardContent.Equilibrium, IgnatiaBoard.Sun, board);
            var marker = silver.Timing.Effects.Single();

            Assert.That(marker.ControllerHeroId, Is.EqualTo(IgnatiaBoard.Ignatia));
            Assert.That(marker.AffectedHeroId.IsDefined, Is.False,
                "A ControllerHero-scoped occurrence names no affected hero at all - which is exactly " +
                "why the predicate reads the controller.");

            var handler = IgnatiaComponentContent.RegisteredHandlers()
                .Single(candidate => candidate.Reference.Equals(IgnatiaComponentContent.CoinSunV1));

            Assert.That(
                handler.Anticipate(CoinContext(silver.Timing, IgnatiaBoard.Ignatia, IgnatiaBoard.Sun)),
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "Her own resolution asks.");
            Assert.That(
                handler.Anticipate(CoinContext(silver.Timing, IgnatiaBoard.Enemy, IgnatiaBoard.Sun)),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "and a resolution by anybody else reads the coin, because the sentence starts with " +
                "\"you\".");
        }

        /// <summary>
        /// The bare predicate, asked with no board around it: it publishes ONE boolean and mutates
        /// nothing, which is what lets a Conditional branch on it wherever a Conditional may stand.
        /// </summary>
        [Test]
        public void ThePredicatePublishesOneBoolean_AndAsksNobodyWithoutTheMarker()
        {
            var handler = IgnatiaComponentContent.RegisteredHandlers()
                .Single(candidate => candidate.Reference.Equals(IgnatiaComponentContent.CoinSunV1));

            Assert.That(handler.Schema.Parameters, Is.Empty,
                "It takes nothing: the coin is match state, not a parameter a card supplies.");
            Assert.That(
                handler.Schema.Outputs.Select(output => (output.OutputName, output.ValueKind)),
                Is.EqualTo(new[]
                {
                    (IgnatiaComponentContent.SunOutput, EffectValueKind.Boolean)
                }));

            var sun = handler.Execute(CoinContext(null, IgnatiaBoard.Ignatia, IgnatiaBoard.Sun));
            Assert.That(sun.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(SunFact(sun), Is.True);

            var moon = handler.Execute(CoinContext(null, IgnatiaBoard.Ignatia, IgnatiaBoard.Moon));
            Assert.That(moon.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(SunFact(moon), Is.False);

            // The words a player reads on the two candidates - and their spelling as values - belong
            // to the block that composes the coin pause's vocabulary. What the handler owes, and all
            // this case asserts, is that the two answers it OFFERS publish the two opposite booleans.
            var offered = handler.Execute(
                CoinContext(EquilibriumTiming(IgnatiaBoard.Sun), IgnatiaBoard.Ignatia, IgnatiaBoard.Sun));
            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "Under Equilibrium's marker the predicate asks rather than reading.");
            Assert.That(offered.Choice.Candidates, Has.Count.EqualTo(2));

            Assert.That(
                offered.Choice.Candidates
                    .Select(candidate => SunFact(handler.Resume(
                        CoinContext(null, IgnatiaBoard.Ignatia, IgnatiaBoard.Moon),
                        new[] { candidate })))
                    .OrderBy(value => value),
                Is.EqualTo(new[] { false, true }),
                "One of the two answers applies the sun text and the other the moon text - measured " +
                "against a MOON coin both times, so neither answer is agreeing with the coin.");

            Assert.That(
                handler.Resume(
                    CoinContext(null, IgnatiaBoard.Ignatia, IgnatiaBoard.Sun),
                    Array.Empty<EffectValue>()).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and an answer that is not one of the two is refused rather than read as a moon.");
        }

        private static bool SunFact(EffectExecutionResult result) =>
            result.Outcomes
                .Single(outcome => outcome.OutputName == IgnatiaComponentContent.SunOutput)
                .Value.BooleanValue;

        /// <summary>
        /// The smallest context the predicate is a function of: a lifecycle, an actor and a coin face.
        /// It reads no board, so none is given.
        /// </summary>
        private static EffectCapabilityContext CoinContext(
            ActiveEffectLifecycle timing, HeroId actor, TeamSide coin) =>
            new EffectCapabilityContext(
                new EffectNodeId(IgnatiaEffectContent.CoinGateNode),
                EffectFailurePolicy.Mandatory,
                actor,
                actor,
                IgnatiaBoard.FaceInstance,
                IgnatiaBoard.Face(IgnatiaCardContent.PlayingWithFire).Reference,
                IgnatiaCardContent.PrimaryRangedAttack,
                null,
                1,
                1,
                IgnatiaBoard.Participants(),
                timing,
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Attack,
                tieBreakerRevealedSide: coin);
    }
}
