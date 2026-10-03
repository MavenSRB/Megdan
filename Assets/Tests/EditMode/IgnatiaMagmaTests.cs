using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's MAGMA line - Path of Ashes, Path of Cinders and Path of
    /// Flames - the half of her Green ladder that leaves impassable obstacles behind her.
    ///
    /// <para>"☀ Move up to N spaces in a straight line. Place a magma token in each empty space you
    /// moved through, or out of. ☾ Place up to N magma tokens in radius." Both faces draw from one
    /// physical pile of FOUR (USER-CORRECTED 2026-09-27 at milestone 8.113, superseding the 2026-09-22
    /// count of three), and both leave the same thing on the board: a bare p.18 obstacle that nothing
    /// may cross and that p.19 removes at the end of the round. When the pile cannot cover a
    /// placement, p.18's short-supply step takes standing magma back from the board first.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaMagmaTests
    {
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);

        /// <summary>An open board with everybody else parked out of her way.</summary>
        private static AdjacentMoveAuthority Board(int radius = 7) =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(0, 6)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(1, 6))
                },
                radius: radius);

        // -------------------------------------------------------------------------------------------
        // The token
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// What a magma token IS: a bare p.18 obstacle from a pile of four, with no standing property
        /// of any kind and p.19's own lifetime. Every one of those is an absence, and an absence is what
        /// the printed faces say - none of the three prints an aura, a screen, a trap or a duration.
        /// </summary>
        [Test]
        public void TheMagmaTokenIsABareImpassableObstacle_FromAPileOfFour()
        {
            var definition = IgnatiaMagmaContent.MagmaTokenDefinition;

            Assert.That(IgnatiaMagmaContent.MagmaSupplyCount, Is.EqualTo(4),
                "USER-CORRECTED 2026-09-27 (milestone 8.113): \"Ignatia has 4 tokens in supply, i " +
                "was wrong\".");
            Assert.That(definition.SupplyCount, Is.EqualTo(4),
                "and the definition carries that pile rather than a second number.");

            Assert.That(definition.Properties, Is.Empty,
                "It declares NO standing property, which is exactly what makes it impassable on this " +
                "board: the move-through permission is a property a token declares, and one that " +
                "declares none cannot be crossed.");
            Assert.That(
                ObstacleQuery.DeclaresMayBeMovedThrough(definition), Is.False,
                "read through the one query that owns that clause.");

            Assert.That(
                definition.RemovalCondition, Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup),
                "p.19: \"Unless the card text says otherwise, tokens are removed at the end of " +
                "round\" - and not one of her three faces says otherwise.");

            Assert.That(
                IgnatiaMagmaContent.MagmaTokens().Select(token => token.Id),
                Is.EqualTo(new[] { IgnatiaMagmaContent.Magma }),
                "One definition, so both printed faces drop the same physical thing.");
        }

        /// <summary>
        /// p.19's end-of-round cleanup, asked of a token actually standing on a board rather than of
        /// the definition: the removal the rulebook performs names hers.
        /// </summary>
        [Test]
        public void EveryMagmaTokenIsRemovedAtTheEndOfTheRound()
        {
            var board = Board();
            IgnatiaBoard.PlaceToken(board, new HexCoordinate(1, 0));
            IgnatiaBoard.PlaceToken(board, new HexCoordinate(2, 0));

            Assert.That(board.PlacedTokens, Has.Count.EqualTo(2));
            Assert.That(
                ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens)
                    .Select(token => token.Id),
                Is.EquivalentTo(board.PlacedTokens.Select(token => token.Id)),
                "Both of them: her magma carries p.19's baseline lifetime and nothing shortens or " +
                "lengthens it.");
        }

        /// <summary>
        /// Impassable, measured as a difference on one board: a hero who could reach a space two steps
        /// away cannot once a magma token stands between - which is the whole of what this token does.
        /// </summary>
        [Test]
        public void AMagmaTokenClosesTheRouteThroughItsOwnSpace()
        {
            var open = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, new HexCoordinate(0, 2)),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(0, 6)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(1, 6))
                });
            var before = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, open);
            before.Pick(IgnatiaBoard.Ally);
            Assert.That(before.Spaces(), Has.Member(new HexCoordinate(0, 4)),
                "With the file open, the teammate's two-space move can end four spaces from the " +
                "origin.");

            var blocked = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, new HexCoordinate(0, 2)),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(0, 6)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(1, 6))
                });
            IgnatiaBoard.PlaceToken(blocked, new HexCoordinate(0, 3));
            var after = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, blocked);
            after.Pick(IgnatiaBoard.Ally);

            Assert.That(after.Spaces(), Has.No.Member(new HexCoordinate(0, 4)),
                "and one magma token on the space between closes the only route to it - \"all tokens " +
                "are obstacles\", and hers declares no way through.");
            Assert.That(after.Spaces(), Has.No.Member(new HexCoordinate(0, 3)),
                "nor may anybody stand on the token itself.");
        }

        // -------------------------------------------------------------------------------------------
        // The sun face - the wake
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Move up to N spaces IN A STRAIGHT LINE" - every destination she is offered lies on one of
        /// p.19's six files from where she stands, and the reach is the card's printed number.
        ///
        /// <para>Corrected at this block: composed as an unrestricted direct Move, the offer was
        /// p.11's ordinary reachability, which turns corners - so the printed three words did nothing.
        /// </para>
        /// </summary>
        [Test]
        public void TheSunFaceOffersOnlyStraightLines_AtThePrintedDistance()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfAshes, IgnatiaBoard.Sun, Board());

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-sun-line"));
            Assert.That(
                frame.Spaces().All(space =>
                    StraightLineQuery.AreInStraightLine(IgnatiaBoard.Origin, space)),
                Is.True,
                "Every destination is on one of her six files. A space two steps away round a corner " +
                "is not \"in a straight line\" and is not offered.");
            Assert.That(
                frame.Spaces().Select(space =>
                    HexDistanceQuery.Calculate(IgnatiaBoard.Origin, space)).Distinct()
                    .OrderBy(value => value),
                Is.EqualTo(new[] { 1, 2 }),
                "\"up to 2 spaces\" - one or two, and nothing further.");
            Assert.That(frame.Spaces(), Has.Length.EqualTo(12),
                "which on an open board is both spaces along each of the six files.");

            var further = new IgnatiaFrame(
                IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, Board());
            Assert.That(
                further.Spaces().Select(space =>
                    HexDistanceQuery.Calculate(IgnatiaBoard.Origin, space)).Distinct()
                    .OrderBy(value => value),
                Is.EqualTo(new[] { 1, 2, 3, 4 }),
                "and Path of Flames prints \"up to 4\" over the same sentence.");
        }

        /// <summary>
        /// THE WAKE. "Place a magma token in each empty space you moved THROUGH, OR OUT OF" - the origin
        /// is one of them and the space she now stands on is not.
        ///
        /// <para>The "out of" half is the one a reading loses: a Place measures its reach from an anchor
        /// and never admits the anchor's own space, so anchoring the wake where she started silently
        /// dropped the first token the sentence names.</para>
        /// </summary>
        [Test]
        public void TheWakeCoversTheSpaceSheLeftAndEverySpaceBetween_ButNotWhereSheNowStands()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfAshes, IgnatiaBoard.Sun, Board());
            var destination = new HexCoordinate(2, 0);
            frame.Pick(destination);

            Assert.That(frame.Step.IsComplete, Is.True);
            Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(destination));
            Assert.That(
                frame.TokenSpaces(),
                Is.EqualTo(new[] { IgnatiaBoard.Origin, new HexCoordinate(1, 0) }),
                "Two tokens: the space she moved OUT OF and the one space she moved THROUGH.");
            Assert.That(frame.TokenSpaces(), Has.No.Member(destination),
                "and none where she is standing - a token is an obstacle, and nothing shares a space " +
                "with a hero.");
        }

        /// <summary>
        /// A longer move lays a longer trail, and the trail is as long as the move rather than as long
        /// as the printed number: a card that prints "up to 4" and is answered with one space places
        /// one token.
        /// </summary>
        [Test]
        public void TheTrailIsAsLongAsTheMoveThatWasActuallyMade()
        {
            var oneSpace = new IgnatiaFrame(
                IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, Board());
            oneSpace.Pick(new HexCoordinate(1, 0));
            Assert.That(oneSpace.TokenSpaces(), Is.EqualTo(new[] { IgnatiaBoard.Origin }),
                "One space moved, one space left behind.");

            var threeSpaces = new IgnatiaFrame(
                IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, Board());
            threeSpaces.Pick(new HexCoordinate(3, 0));
            Assert.That(
                threeSpaces.TokenSpaces(),
                Is.EqualTo(new[]
                {
                    IgnatiaBoard.Origin, new HexCoordinate(1, 0), new HexCoordinate(2, 0)
                }),
                "and three spaces moved leave the origin and the two she crossed.");
        }

        /// <summary>
        /// "Up to" includes none. Declining the move is a legal answer, and a Move that did not happen
        /// has no wake at all.
        /// </summary>
        [Test]
        public void DecliningTheMoveLaysNoTrail()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfAshes, IgnatiaBoard.Sun, Board());

            Assert.That(frame.Choice.DeclineInvocation, Is.Not.Null,
                "\"Move UP TO 2 spaces\" admits nought, so the pause offers a Decline.");
            frame.Decline();

            Assert.That(frame.Step.IsComplete, Is.True);
            Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(IgnatiaBoard.Origin));
            Assert.That(frame.Tokens, Is.Empty,
                "and nothing is placed: there is no space she moved through and none she moved out " +
                "of.");
        }

        /// <summary>
        /// Path of Flames' longest wake spends the whole pile: four wake spaces, four tokens. Before
        /// milestone 8.113 the pile was read as three and the fourth space stayed bare.
        /// </summary>
        [Test]
        public void AFourSpaceWakeSpendsTheWholePileOfFour()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, Board());
            frame.Pick(new HexCoordinate(4, 0));

            Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(new HexCoordinate(4, 0)));
            Assert.That(
                frame.TokenSpaces(),
                Is.EqualTo(new[]
                {
                    IgnatiaBoard.Origin, new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                    new HexCoordinate(3, 0)
                }),
                "every space she moved out of or through takes a token - four of them, her whole pile.");
            Assert.That(frame.Tokens, Has.Count.EqualTo(IgnatiaMagmaContent.MagmaSupplyCount));
            Assert.That(frame.Step.IsComplete, Is.True, "Nothing needed reclaiming, so nothing asked.");
        }

        /// <summary>
        /// p.18's short supply on the wake, when every standing token must come back: "If you need to
        /// place more tokens than are available in the supply, first remove up to that number of
        /// tokens of the same type from the board, if able." Three stand from an earlier scatter, one
        /// is in the pile, and the four-space wake needs four - so all three return and there is
        /// nothing to choose.
        /// </summary>
        [Test]
        public void AShortWakeTakesBackEveryStandingTokenWhenItNeedsThemAll()
        {
            var board = Board();
            var scatter = new IgnatiaFrame(IgnatiaCardContent.PathOfCinders, IgnatiaBoard.Moon, board);
            var standing = new[]
            {
                new HexCoordinate(-2, 0), new HexCoordinate(-1, -1), new HexCoordinate(0, -2)
            };
            foreach (var space in standing)
            {
                scatter.Pick(space);
            }

            Assert.That(scatter.Tokens, Has.Count.EqualTo(3));

            var wake = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, board);
            wake.Pick(new HexCoordinate(4, 0));

            Assert.That(wake.Step.IsComplete, Is.True,
                "Needing three and having exactly three standing is no choice at all.");
            Assert.That(
                wake.TokenSpaces(),
                Is.EqualTo(new[]
                {
                    IgnatiaBoard.Origin, new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                    new HexCoordinate(3, 0)
                }),
                "The three scattered tokens came back and all four wake spaces took one.");
            Assert.That(wake.TokenSpaces().Intersect(standing), Is.Empty);
        }

        /// <summary>
        /// The same step when more stands than the wake is short of: the actor names which standing
        /// tokens return, exactly as many as the shortfall, and the rest stay where they are.
        /// </summary>
        [Test]
        public void AShortWakeAsksWhichStandingTokenReturnsWhenMoreStandThanItNeeds()
        {
            var board = Board();
            var scatter = new IgnatiaFrame(IgnatiaCardContent.PathOfCinders, IgnatiaBoard.Moon, board);
            var standing = new[]
            {
                new HexCoordinate(-2, 0), new HexCoordinate(-1, -1), new HexCoordinate(0, -2)
            };
            foreach (var space in standing)
            {
                scatter.Pick(space);
            }

            // One token left in the pile; Path of Flames walks three spaces, so the wake needs three
            // and is short by two, with three standing to choose from.
            var wake = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, board);
            wake.Pick(new HexCoordinate(3, 0));

            Assert.That(wake.Choice, Is.Not.Null, "Three stand and two must return - a real choice.");
            Assert.That(wake.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-sun-wake"));
            Assert.That(wake.Spaces(), Is.EquivalentTo(standing),
                "Only standing magma is offered - never the wake spaces it is about to fill.");
            Assert.That(wake.Choice.MinimumSelections, Is.EqualTo(2));
            Assert.That(wake.Choice.MaximumSelections, Is.EqualTo(2));

            wake.Answer(wake.Choice.SelectInvocations.Single(
                invocation => invocation.Candidate.Value.Equals(EffectValue.Space(standing[0]))));
            wake.Answer(wake.Choice.SelectInvocations.Single(
                invocation => invocation.Candidate.Value.Equals(EffectValue.Space(standing[2]))));
            if (wake.Choice != null)
            {
                wake.Confirm();
            }

            Assert.That(wake.Step.IsComplete, Is.True);
            Assert.That(
                wake.TokenSpaces(),
                Is.EquivalentTo(new[]
                {
                    standing[1], IgnatiaBoard.Origin, new HexCoordinate(1, 0), new HexCoordinate(2, 0)
                }),
                "the two named tokens returned, the unnamed one stayed, and the three wake spaces " +
                "took the pile's four.");
        }

        // -------------------------------------------------------------------------------------------
        // The moon face - the scatter
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Place up to N magma tokens in radius" - N separate optional placements, each reaching the
        /// card's own printed Radius, because a stateless placement cannot tell a second drop from a
        /// first and so a second decision belongs to a second node.
        /// </summary>
        [Test]
        public void TheMoonFaceIsOneOptionalPlacementPerPrintedToken()
        {
            foreach (var pair in new[]
            {
                (Card: IgnatiaCardContent.PathOfAshes, Count: 2),
                (Card: IgnatiaCardContent.PathOfCinders, Count: 3),
                (Card: IgnatiaCardContent.PathOfFlames, Count: 4)
            })
            {
                var places = IgnatiaBoard.PrimaryText(pair.Card).Nodes
                    .Where(node => node.Capability.Equals(IgnatiaMagmaContent.PlaceMagmaInRadiusV1))
                    .ToArray();

                Assert.That(places, Has.Length.EqualTo(pair.Count),
                    pair.Card.Value + " prints \"up to " + pair.Count + "\", so it declares that " +
                    "many placements.");
                Assert.That(
                    places.All(node => node.FailurePolicy == EffectFailurePolicy.May), Is.True,
                    pair.Card.Value + ": each is a `may`, which is how \"up to\" is honoured one " +
                    "token at a time.");

                var arm = IgnatiaBoard.PrimaryText(pair.Card).Nodes
                    .Single(node => node.Kind == EffectNodeKind.Sequence &&
                        node.ChildNodeIds.Contains(places[0].Id));
                Assert.That(arm.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    pair.Card.Value + ": the arm that orders the passes is one `up to` count (W10, " +
                    "milestone 9.39), so the first declined pass closes it.");
            }
        }

        /// <summary>
        /// Milestone 9.39, ruling W10: one of "up to 3" placed and the second declined - the third is
        /// not asked, and the placed token stands. Before it the third placement was still a question.
        /// </summary>
        [Test]
        public void TheScatter_OneOfThreeThenADecline_AsksNoThird()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfCinders, IgnatiaBoard.Moon, Board());
            frame.Pick(new HexCoordinate(2, 0));
            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-2"));

            frame.Decline();

            Assert.That(frame.Step.IsComplete, Is.True, "W10: the decline closes the count.");
            Assert.That(frame.Step.Frame?.PendingChoice, Is.Null, "no third placement is asked.");
            Assert.That(frame.TokenSpaces(), Is.EqualTo(new[] { new HexCoordinate(2, 0) }));
        }

        /// <summary>
        /// The scatter actually places, inside the printed Radius and nowhere else, and declining the
        /// first placement ends the line - "up to N" admits nought here too.
        /// </summary>
        [Test]
        public void TheScatterPlacesInsideThePrintedRadius_AndMayPlaceNoneAtAll()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfAshes, IgnatiaBoard.Moon, Board());

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-1"));
            Assert.That(
                frame.Spaces().All(space =>
                    HexDistanceQuery.Calculate(IgnatiaBoard.Origin, space) <= 3),
                Is.True,
                "Path of Ashes prints Radius 3, so no offered space is further out.");
            Assert.That(frame.Spaces(), Has.No.Member(IgnatiaBoard.Origin),
                "and never the space she is standing on.");

            var first = new HexCoordinate(2, 0);
            frame.Pick(first);
            Assert.That(frame.TokenSpaces(), Is.EqualTo(new[] { first }));

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-2"),
                "and the second printed token is a second question.");
            Assert.That(frame.Spaces(), Has.No.Member(first),
                "which cannot name the space the first one is now standing on.");

            frame.Decline();
            Assert.That(frame.Step.IsComplete, Is.True);
            Assert.That(frame.Tokens, Has.Count.EqualTo(1),
                "\"Up to 2\" is satisfied by one, which is what declining the second placement means.");
        }

        /// <summary>
        /// Path of Flames' "up to 4" and a pile of four: every placement is asked and every one lands,
        /// and none of the short-supply steps between them asks anything while the pile holds a token.
        /// </summary>
        [Test]
        public void TheScatterCanSpendTheWholePileOfFour()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Moon, Board());

            for (var placement = 1; placement <= 4; placement++)
            {
                Assert.That(frame.Choice.NodeId.Value,
                    Is.EqualTo("ignatia-magma-moon-place-" + placement),
                    "the pile is not empty, so the placement is the question and not a reclamation.");
                frame.Pick(new[]
                {
                    new HexCoordinate(2, 0), new HexCoordinate(3, 0), new HexCoordinate(0, 2),
                    new HexCoordinate(0, 3)
                }[placement - 1]);
            }

            Assert.That(frame.Step.IsComplete, Is.True);
            Assert.That(frame.Tokens, Has.Count.EqualTo(IgnatiaMagmaContent.MagmaSupplyCount));
            Assert.That(frame.Outcome("ignatia-magma-moon-reclaim-2"),
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "and the reclamation before each placement stood aside on its own.");
        }

        /// <summary>
        /// p.18's short supply on the scatter: once the pile is empty the next placement is preceded by
        /// the question of which standing magma token comes back. Taking one lets the placement ask
        /// where it goes; declining it declines that token, and the empty pile then leaves the
        /// placement with nothing to do.
        /// </summary>
        [Test]
        public void AnEmptyPileAsksWhichStandingTokenReturnsBeforeTheNextPlacement()
        {
            var board = Board();
            var first = new IgnatiaFrame(IgnatiaCardContent.PathOfCinders, IgnatiaBoard.Moon, board);
            var standing = new[]
            {
                new HexCoordinate(-2, 0), new HexCoordinate(-1, -1), new HexCoordinate(0, -2)
            };
            foreach (var space in standing)
            {
                first.Pick(space);
            }

            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Moon, board);
            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-1"),
                "The fourth token is still in the pile, so the first placement asks as it always did.");
            frame.Pick(new HexCoordinate(2, 0));

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-reclaim-2"),
                "The pile is empty now, so p.18 asks which standing token comes back first.");
            Assert.That(frame.Spaces(),
                Is.EquivalentTo(standing.Concat(new[] { new HexCoordinate(2, 0) })),
                "every standing magma token is a candidate, the one just placed included.");
            Assert.That(frame.Choice.DeclineInvocation, Is.Not.Null,
                "and it may be declined, because the placement it makes room for is optional.");

            frame.Pick(standing[0]);
            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-2"));
            frame.Pick(new HexCoordinate(3, 0));
            Assert.That(frame.TokenSpaces(), Has.No.Member(standing[0]));
            Assert.That(frame.TokenSpaces(), Has.Member(new HexCoordinate(3, 0)));

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-magma-moon-reclaim-3"));
            frame.Decline();

            Assert.That(frame.Step.IsComplete, Is.True,
                "declining the reclamation declines that pass, and W10 (9.39) closes the count with " +
                "it: the fourth is not asked.");
            Assert.That(frame.Step.Frame?.PendingChoice, Is.Null);
            Assert.That(frame.Tokens, Has.Count.EqualTo(IgnatiaMagmaContent.MagmaSupplyCount),
                "still four magma on the board, never five.");
        }

        // -------------------------------------------------------------------------------------------
        // What the rest of her set sees
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The line joins up: a magma token her Green face drops is a token her Blue face's narrowing
        /// sees, which is the combination the whole hero is built around. Measured end to end - she
        /// scatters, and the discard narrowing then names the hero standing beside what she dropped.
        /// </summary>
        [Test]
        public void AMagmaTokenSheDroppedQualifiesAnEnemyHeroForHerDiscardLine()
        {
            var enemyAt = new HexCoordinate(2, 0);
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, enemyAt),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(0, 6))
                });

            var before = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, board);
            Assert.That(before.Choice, Is.Null,
                "Nothing stands beside him, so her Blue face names nobody.");

            var scatter = new IgnatiaFrame(IgnatiaCardContent.PathOfAshes, IgnatiaBoard.Moon, board);
            scatter.Pick(new HexCoordinate(3, 0));
            scatter.Decline();
            Assert.That(scatter.TokenSpaces(), Is.EqualTo(new[] { new HexCoordinate(3, 0) }));

            var after = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, board);
            Assert.That(after.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "and one magma token beside him is what \"adjacent to a token\" means - the two " +
                "halves of her set, on one board, with nothing between them but the token.");
        }

        /// <summary>
        /// Her three magma faces name exactly two contracts of her own beside the coin, and the radius
        /// placement is milestone 8.37's <c>PlaceHeroTokenHandler</c> reused rather than a new type -
        /// stated as a set so a third arriving unnoticed fails here.
        /// </summary>
        [Test]
        public void TheMagmaLineNamesExactlyTwoPlacementContractsAndTheShortSupplyStep()
        {
            var named = new[]
                {
                    IgnatiaCardContent.PathOfAshes,
                    IgnatiaCardContent.PathOfCinders,
                    IgnatiaCardContent.PathOfFlames
                }
                .SelectMany(face => IgnatiaBoard.PrimaryText(face).Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability.ToString())
                .Distinct()
                .Where(value => value.StartsWith("ignatia-", StringComparison.Ordinal))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(named, Is.EqualTo(new[]
            {
                "ignatia-coin-sun@1",
                "ignatia-place-magma-in-radius@1",
                "ignatia-place-magma-wake@1",
                "ignatia-reclaim-magma@1"
            }));

            Assert.That(
                IgnatiaMagmaContent.Handlers().Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "ignatia-place-magma-in-radius@1",
                    "ignatia-place-magma-wake@1",
                    "ignatia-reclaim-magma@1"
                }),
                "and the subsystem ships exactly those three, which is what her dealing registers - " +
                "the reclamation being p.18's short-supply step (milestone 8.113).");
        }
    }
}
