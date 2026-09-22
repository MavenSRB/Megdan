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
    /// physical pile of THREE (USER-SETTLED 2026-09-22), and both leave the same thing on the board: a
    /// bare p.18 obstacle that nothing may cross and that p.19 removes at the end of the round.</para>
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
        /// What a magma token IS: a bare p.18 obstacle from a pile of three, with no standing property
        /// of any kind and p.19's own lifetime. Every one of those is an absence, and an absence is what
        /// the printed faces say - none of the three prints an aura, a screen, a trap or a duration.
        /// </summary>
        [Test]
        public void TheMagmaTokenIsABareImpassableObstacle_FromAPileOfThree()
        {
            var definition = IgnatiaMagmaContent.MagmaTokenDefinition;

            Assert.That(IgnatiaMagmaContent.MagmaSupplyCount, Is.EqualTo(3),
                "USER-SETTLED 2026-09-22: \"Magma token count is 3\".");
            Assert.That(definition.SupplyCount, Is.EqualTo(3),
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
        /// The pile is physical and it is shared: a move longer than the supply lays as many tokens as
        /// there are, and then stops. p.18's short-supply step, measured on the one face that can
        /// outrun three.
        /// </summary>
        [Test]
        public void AWakeLongerThanTheSupplyLaysExactlyTheThreeTokensThatExist()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Sun, Board());
            frame.Pick(new HexCoordinate(4, 0));

            Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(new HexCoordinate(4, 0)),
                "The move itself is unaffected by the pile: she walks the four spaces her card " +
                "prints.");
            Assert.That(
                frame.TokenSpaces(),
                Is.EqualTo(new[]
                {
                    IgnatiaBoard.Origin, new HexCoordinate(1, 0), new HexCoordinate(2, 0)
                }),
                "and three of the four wake spaces take a token, in the order the walk visits them - " +
                "the supply is three, and a piece that does not exist is not placed.");
            Assert.That(frame.Tokens, Has.Count.EqualTo(IgnatiaMagmaContent.MagmaSupplyCount));
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
            }
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
        /// The pile bounds the scatter exactly as it bounds the wake: Path of Flames prints "up to 4"
        /// and there are three tokens, so the fourth placement has nothing to place and is skipped.
        /// </summary>
        [Test]
        public void TheFourthPlacementHasNoTokenToPlace_BecauseThePileIsThree()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.PathOfFlames, IgnatiaBoard.Moon, Board());

            frame.Pick(new HexCoordinate(2, 0));
            frame.Pick(new HexCoordinate(3, 0));
            frame.Pick(new HexCoordinate(4, 0));

            Assert.That(frame.Tokens, Has.Count.EqualTo(3));
            Assert.That(frame.Step.IsComplete, Is.True,
                "The fourth placement is not asked at all: there is no fourth magma token, and a " +
                "`may` node with nothing to do is skipped rather than opening an empty question.");
            Assert.That(frame.Tokens, Has.Count.EqualTo(IgnatiaMagmaContent.MagmaSupplyCount),
                "so a card printing \"up to 4\" places at most the three that exist - p.18's " +
                "short-supply step, which bounds the printed number by the physical pile.");
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
        public void TheMagmaLineNamesExactlyTwoPlacementContracts()
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
                "ignatia-place-magma-wake@1"
            }));

            Assert.That(
                IgnatiaMagmaContent.Handlers().Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "ignatia-place-magma-in-radius@1",
                    "ignatia-place-magma-wake@1"
                }),
                "and the subsystem ships exactly those two, which is what her dealing registers.");
        }
    }
}
