using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z36. Team colour as a RUNTIME fact: the same hero presents a
    /// different team on a different line-up, and the shipped six present exactly what they always
    /// did.
    /// </summary>
    /// <remarks>
    /// <para>The user's standing directive: "with selectable heroes no token may bake a team
    /// identity - the team ring/tint on board tokens, and every other team-coloured surface,
    /// derives from the live roster assignment at composition", with an AUDIT of every team-coloured
    /// surface and "a regression composing one hero on BOTH teams and asserting every surface
    /// follows". This fixture is that regression.</para>
    /// <para><b>What the audit found, recorded here because a fixture is where a finding stays
    /// true.</b> Every team-coloured surface in the build already reads the authoritative
    /// <see cref="TeamSide"/> and none of them keys off hero identity: the board piece takes it from
    /// its setup state, the token rim and the HUD's team face are both functions of the side, and
    /// the lobby writes the team out in words. Exactly ONE thing baked a team: the token ARTWORK,
    /// addressed <c>&lt;content-id&gt;-red</c> / <c>-blue</c> as two prepared files. That is the one
    /// thing this milestone changed, and everything else needed only to be proven rather than
    /// fixed.</para>
    /// <para>Unity-free, so it runs in the cross-platform harness. The Unity-side consumers
    /// (<c>HeroTokenArtwork</c>, <c>PrimitiveHeroView</c>) are thin adapters over exactly these
    /// answers, and are compile-proven.</para>
    /// </remarks>
    public sealed class RuntimeTeamIdentityTests
    {
        /// <summary>The six heroes whose prepared artwork predates the selector.</summary>
        private static readonly string[] ShippedContentIds =
        {
            "wasp", "xargatha", "tigerclaw", "arien", "brogan", "sabina"
        };

        // ---------------------------------------------------------------------------------------
        // The directive's own regression: one hero, both teams.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The same hero composed onto Red and onto Blue presents two different team identities,
        /// through every surface that derives one.
        /// </summary>
        /// <remarks>
        /// The two compositions differ ONLY in which team chose the hero - same registration, same
        /// format, same map - so anything that differs between them is derived from the assignment
        /// and anything that does not, is not.
        /// </remarks>
        [Test]
        public void TheSameHeroOnBothTeamsPresentsTwoTeamIdentities()
        {
            var printed = RegisteredHeroRoster.PrintedLineup(3);
            var onRed = printed;                                      // Wasp is Red here
            var onBlue = new HeroLineup(printed.Blue, printed.Red);    // ... and Blue here

            const string hero = "hero-wasp";
            var content = new HeroContentId("wasp");

            // 1. The line-up itself - the single source every other surface reads.
            Assert.That(onRed.SideOf(hero), Is.EqualTo(TeamSide.Red));
            Assert.That(onBlue.SideOf(hero), Is.EqualTo(TeamSide.Blue));

            // 2. The participant graph: the hero's player belongs to the team that chose it.
            foreach (var pair in new[]
            {
                (Lineup: onRed, Side: TeamSide.Red),
                (Lineup: onBlue, Side: TeamSide.Blue)
            })
            {
                Assert.That(
                    HeroLineupComposition.TryFindPlayerFor(pair.Lineup, hero, out _, out var side),
                    Is.True);
                Assert.That(
                    side, Is.EqualTo(pair.Side),
                    "The composed participants follow the line-up, not the printed pairing.");
            }

            // 3. The board piece: a different Throne zone's marked point.
            var redBoard = HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, onRed);
            var blueBoard = HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, onBlue);
            var redSpawn = redBoard.Heroes
                .Single(h => h.HeroId.Value == hero).SpawnPoint.SpawnPointId.Value;
            var blueSpawn = blueBoard.Heroes
                .Single(h => h.HeroId.Value == hero).SpawnPoint.SpawnPointId.Value;
            Assert.That(redSpawn, Does.StartWith("red-hero-"));
            Assert.That(blueSpawn, Does.StartWith("blue-hero-"));

            // 4. The token: ring colour and, where a baked face exists, the face itself.
            var redToken = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, true, false);
            var blueToken = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Blue, true, false);
            Assert.That(
                redToken.RingColor, Is.Not.EqualTo(blueToken.RingColor),
                "A piece states its team in the ring, and the two teams are two colours.");
            Assert.That(redToken.ResourcePath, Is.Not.EqualTo(blueToken.ResourcePath));

            // ... and the SAME is true of a hero with only a white token, which is the whole point:
            // the ring carries the identity where the artwork no longer does.
            var redNeutral = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, false, true);
            var blueNeutral = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Blue, false, true);
            Assert.That(
                redNeutral.RingColor, Is.Not.EqualTo(blueNeutral.RingColor),
                "A white-background token still says which team it is on.");
            Assert.That(
                redNeutral.ResourcePath, Is.EqualTo(blueNeutral.ResourcePath),
                "... and it says it WITHOUT a second file, which is the directive.");

            // 5. And the prism, for a hero with no artwork at all.
            var redPrism = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, false, false);
            var bluePrism = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Blue, false, false);
            Assert.That(redPrism.FaceKind, Is.EqualTo(TokenFaceKind.GeneratedPrism));
            Assert.That(redPrism.RingColor, Is.Not.EqualTo(bluePrism.RingColor));
        }

        /// <summary>
        /// The whole registration can be composed onto either team, so no hero in this build owns a
        /// side. Swept rather than sampled: a selector that works for five heroes and not the sixth
        /// is a selector that breaks on whichever one a player picks.
        /// </summary>
        [Test]
        public void NoRegisteredHeroOwnsATeam()
        {
            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var others = RegisteredHeroRoster.ForThisBuild
                        .Where(candidate => candidate.HeroIdValue != hero.HeroIdValue)
                        .Take(5)
                        .ToArray();
                    if (others.Length < 5) continue;

                    var withHero = new[] { hero }.Concat(others.Take(2)).ToArray();
                    var without = others.Skip(2).Take(3).ToArray();

                    var lineup = side == TeamSide.Red
                        ? new HeroLineup(withHero, without)
                        : new HeroLineup(without, withHero);

                    Assert.That(
                        lineup.SideOf(hero.HeroIdValue), Is.EqualTo(side),
                        $"{hero.HeroIdValue} must be composable onto {side}.");

                    var board = HeroLineupComposition.Compose(
                        MatchFormat.ThreeVersusThreeQuick, lineup);
                    var placement = board.Heroes.Single(
                        placed => placed.HeroId.Value == hero.HeroIdValue);
                    Assert.That(
                        placement.SpawnPoint.SpawnPointId.Value,
                        Does.StartWith(side == TeamSide.Red ? "red-hero-" : "blue-hero-"),
                        $"{hero.HeroIdValue} on {side} must stand in that team's Throne zone.");
                    Assert.That(
                        placement.ContentId.Value, Is.EqualTo(hero.ContentId.Value),
                        "The printed set never changes with the team.");
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // Preserve shipped face selection and outer footprint while normalizing its ring.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Every shipped hero still resolves to its prepared face and the same outer footprint,
        /// but now uses the common 0.15-wide player-colour ring.
        /// </summary>
        /// <remarks>
        /// The values are explicit rather than read back from the type under test, so this fails
        /// if the new shared radius or the original outer footprint moves unexpectedly.
        /// </remarks>
        [Test]
        public void TheShippedRosterKeepsItsFacesAndOuterFootprintWithUniformRims()
        {
            foreach (var contentId in ShippedContentIds)
            {
                var content = new HeroContentId(contentId);

                foreach (var pair in new[]
                {
                    (Side: TeamSide.Red, Suffix: "red"),
                    (Side: TeamSide.Blue, Suffix: "blue")
                })
                {
                    var token = RuntimeTeamIdentity.ResolveToken(content, pair.Side, true, false);

                    Assert.That(token.FaceKind, Is.EqualTo(TokenFaceKind.TeamBaked));
                    Assert.That(
                        token.ResourcePath,
                        Is.EqualTo($"Private/Heroes/{contentId}-{pair.Suffix}"),
                        "Milestone 8.11b's resource path, unchanged.");
                    Assert.That(
                        token.FaceRadius, Is.EqualTo(0.66f).Within(0f),
                        "Every artwork-backed hero now has the same face radius.");
                    Assert.That(
                        RuntimeTeamIdentity.TokenOuterRadius, Is.EqualTo(0.81f).Within(0f),
                        "Milestone 8.11b's outer radius, unchanged.");
                    Assert.That(
                        token.RingWidth, Is.EqualTo(0.15f).Within(0.0001f),
                        "The player-colour band is one width across the roster.");
                }

                // The team-baked face is preferred wherever one exists, EVEN IF a neutral face is
                // also prepared. This ordering is the zero-regression guarantee: preparing a white
                // token for a shipped hero must not silently restyle its board piece.
                var both = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, true, true);
                Assert.That(both.FaceKind, Is.EqualTo(TokenFaceKind.TeamBaked));
                Assert.That(both.ResourcePath, Is.EqualTo($"Private/Heroes/{contentId}-red"));
            }
        }

        /// <summary>
        /// The two team colours are milestone 4.4's, so a board's Red is one Red whichever surface
        /// draws it, and neither moved when the colour moved house.
        /// </summary>
        [Test]
        public void TheTeamColoursAreTheOnesTheBoardHasAlwaysUsed()
        {
            var red = RuntimeTeamIdentity.TeamColor(TeamSide.Red);
            Assert.That(red.R, Is.EqualTo(0.82f).Within(0f));
            Assert.That(red.G, Is.EqualTo(0.12f).Within(0f));
            Assert.That(red.B, Is.EqualTo(0.12f).Within(0f));
            Assert.That(red.A, Is.EqualTo(1f).Within(0f));

            var blue = RuntimeTeamIdentity.TeamColor(TeamSide.Blue);
            Assert.That(blue.R, Is.EqualTo(0.1f).Within(0f));
            Assert.That(blue.G, Is.EqualTo(0.35f).Within(0f));
            Assert.That(blue.B, Is.EqualTo(0.9f).Within(0f));
            Assert.That(blue.A, Is.EqualTo(1f).Within(0f));
        }

        // ---------------------------------------------------------------------------------------
        // The new mechanism: a white token that states its team in the ring.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// A team-neutral token's address carries no team at all, so nothing downstream can bake
        /// one back into it.
        /// </summary>
        [Test]
        public void ANeutralTokensAddressCarriesNoTeam()
        {
            var path = RuntimeTeamIdentity.NeutralResourcePath(new HeroContentId("dodger"));

            Assert.That(path, Is.EqualTo("Private/Heroes/dodger"));
            Assert.That(path, Does.Not.Contain("red").And.Not.Contain("blue"));
        }

        /// <summary>
        /// Team-neutral and team-baked faces state their player colour in equally wide rings.
        /// </summary>
        /// <remarks>
        /// Team-baked and neutral faces use one ring width, while the source-art crop prevents a
        /// baked orange or blue edge from becoming a second visible ring.
        /// </remarks>
        [Test]
        public void BakedAndNeutralTokensUseTheSameRingWidth()
        {
            var content = new HeroContentId("dodger");
            var neutral = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, false, true);
            var baked = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, true, false);

            Assert.That(neutral.FaceKind, Is.EqualTo(TokenFaceKind.TeamNeutral));
            Assert.That(
                neutral.RingWidth, Is.EqualTo(baked.RingWidth).Within(0.0001f),
                "The ring must look equally thick for both source-art kinds.");
            Assert.That(
                neutral.FaceRadius, Is.EqualTo(baked.FaceRadius).Within(0.0001f),
                "The common outer footprint and face radius imply one band thickness.");
            Assert.That(
                RuntimeTeamIdentity.TokenOuterRadius - neutral.FaceRadius,
                Is.EqualTo(neutral.RingWidth).Within(0.0001f));
            Assert.That(
                neutral.RingColor, Is.EqualTo(RuntimeTeamIdentity.TeamColor(TeamSide.Red)),
                "In the project's own Red, not the source set's orange.");
        }

        /// <summary>
        /// Nothing tints the portrait. The neutral face's resource is the artwork as prepared, and
        /// the team is a ring around it - which is what keeps a hero's face the same face on both
        /// teams and the white background white.
        /// </summary>
        [Test]
        public void TheTeamIsAppliedAroundTheArtworkAndNeverOverIt()
        {
            var content = new HeroContentId("dodger");
            var red = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Red, false, true);
            var blue = RuntimeTeamIdentity.ResolveToken(content, TeamSide.Blue, false, true);

            Assert.That(red.ResourcePath, Is.EqualTo(blue.ResourcePath));
            Assert.That(red.FaceRadius, Is.EqualTo(blue.FaceRadius));
            Assert.That(
                red.FaceKind, Is.EqualTo(blue.FaceKind),
                "One face, two teams: everything that differs is outside the artwork.");
        }

        /// <summary>
        /// A piece with no team is refused rather than defaulted, so a composition bug cannot reach
        /// a player looking correct.
        /// </summary>
        [Test]
        public void APieceWithNoTeamIsRefusedRatherThanPaintedRed()
        {
            Assert.That(
                () => RuntimeTeamIdentity.TeamColor(TeamSide.None),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => RuntimeTeamIdentity.ResolveToken(
                    new HeroContentId("wasp"), TeamSide.None, true, false),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => RuntimeTeamIdentity.TeamBakedResourcePath(
                    new HeroContentId("wasp"), TeamSide.None),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// Every face kind resolves to a ring colour, including the prism - so no drawable state
        /// exists in which a piece has no team statement at all.
        /// </summary>
        [Test]
        public void EveryDrawableStateStatesItsTeam()
        {
            var content = new HeroContentId("wasp");
            var seen = new System.Collections.Generic.HashSet<TokenFaceKind>();

            foreach (var pair in new[]
            {
                (Baked: true, Neutral: false),
                (Baked: false, Neutral: true),
                (Baked: false, Neutral: false)
            })
            {
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var token = RuntimeTeamIdentity.ResolveToken(
                        content, side, pair.Baked, pair.Neutral);
                    seen.Add(token.FaceKind);
                    Assert.That(
                        token.RingColor, Is.EqualTo(RuntimeTeamIdentity.TeamColor(side)),
                        $"{token.FaceKind} on {side} must state its team.");
                }
            }

            Assert.That(
                seen, Is.EquivalentTo((TokenFaceKind[])Enum.GetValues(typeof(TokenFaceKind))),
                "Every face kind the enum declares is reachable and was covered here.");
        }
    }
}
