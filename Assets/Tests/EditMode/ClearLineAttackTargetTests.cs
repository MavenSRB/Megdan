using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
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
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40d. The clearance clause three of Bain's five Red Attacks print - Light Crossbow's,
    /// Heavy Crossbow's and Arbalest's "Target a unit in a straight line and in range <em>with no
    /// units, or terrain, between you</em>" - proved as vocabulary before any of his content exists.
    ///
    /// <para><b>What is new and what is not.</b> <see cref="AttackTargetLine.InStraightLine"/> has been
    /// here since milestone 8.38a, and it says only which FILE a target stands on. What no member said
    /// until now is what stands ON that file, because rulebook p.9 states the default in as many words
    /// - "There are no 'line of sight' rules. You can target units through obstacles, as long as they
    /// are in range/radius" - so the clearance is a printed narrowing rather than a page this build was
    /// missing.</para>
    ///
    /// <para><b>The clause is deliberately narrower than p.18's obstacle list, which is the whole
    /// point of the milestone.</b> Get over here! prints "with no <em>obstacles</em> between you and
    /// the target" and p.18's Obstacle keyword makes that list "Units, terrain (including printed on
    /// the board), tokens, as well as any objects represented by miniatures". These three faces name
    /// two of those four kinds. So a token blocks the pull and does not block the shot, which his
    /// shard records as BA-03 and which the set equality below asserts rather than argues.</para>
    ///
    /// <para><b>Three of five, not five of five.</b> The block that ordered this work, BA-03, and the
    /// shard's contract row all said the clause was on all five of his Red faces and on both modal
    /// faces' ranged bullets. The corpus says otherwise and was read rather than remembered: Hand
    /// Crossbow and Hunter-Seeker print "Target a hero in range with a bounty marker" beside "Target a
    /// unit adjacent to you" and print no line clause at all, exactly as the shard's own verbatim
    /// transcription has them. The bullet reach below is built and asserted anyway - a member of a
    /// shared enum that one of its two consumers cannot express would be a trap for the next
    /// composition - but it is built because the enum is shared, not because those two faces print
    /// it.</para>
    ///
    /// <para>Pure Core, so it runs in the cross-platform harness; nothing here touches Unity.</para>
    /// </summary>
    public sealed class ClearLineAttackTargetTests
    {
        /// <summary>
        /// One number for every case. The board's playable disk is radius 4 and every piece on it
        /// stands within three spaces of the actor, so the reach never decides an answer the line
        /// clause is being asked about.
        /// </summary>
        private const int RangeTotal = 4;

        // ---------------------------------------------------------------------------------------
        // The declaration itself
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// p.19 counts an adjacent space as a straight line and leaves no space between the two ends
        /// of one, so an adjacent-only Attack demanding a clear file demands nothing twice over. It is
        /// refused for the reason milestone 8.38a refuses the plain straight line beside that reach:
        /// a policy that declared a narrowing while narrowing nothing would make
        /// <see cref="PrintedAttackTargetPolicy.NarrowsTheDefault"/> report one that is not there.
        /// </summary>
        [Test]
        public void TheClearLineClauseBesideTheAdjacentOnlyReach_IsRefusedAsANarrowingThatNarrowsNothing()
        {
            var message = Assert.Throws<ArgumentException>(() => new PrintedAttackTargetPolicy(
                AttackTargetableUnits.HeroesAndMinions,
                AttackTargetReach.AdjacentOnly,
                AttackTargetLine.InStraightLineClearOfUnitsAndTerrain)).Message;

            Assert.That(message, Does.Contain("narrows nothing"));
            Assert.That(message, Does.Contain("p.19"),
                "and the refusal cites the page it comes from, as the other two line refusals do.");
        }

        /// <summary>
        /// The pairing the three faces actually print: the clearance beside p.13's printed-Range
        /// ceiling, leaving both other dimensions exactly where the builder found them.
        /// </summary>
        [Test]
        public void TheBuilder_NamesThePrintedClauseAndLeavesTheOtherDimensionsAlone()
        {
            var policy = PrintedAttackTargetPolicy
                .ForClassification(AttackRangeKind.Ranged)
                .RestrictedToTheStraightLineClearOfUnitsAndTerrain();

            Assert.That(
                policy.Line, Is.EqualTo(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain));
            Assert.That(policy.Reach, Is.EqualTo(AttackTargetReach.PrintedRange),
                "\"in range\" is p.13's ceiling, which the clause says nothing about.");
            Assert.That(policy.TargetableUnits, Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                "\"Target a unit\" is p.13's default union of both registers.");
            Assert.That(policy.Binding, Is.EqualTo(AttackTargetBinding.AtActionStart));
            Assert.That(policy.NarrowsTheDefault(AttackRangeKind.Ranged), Is.True);
        }

        /// <summary>
        /// The three existing members keep the values a payload already holds, which is what makes an
        /// added member free rather than a schema event: an
        /// <see cref="GuardsDigital.Core.Snapshots.AuthoritativeSnapshotSerializer"/> bullet writes
        /// this enum as its integer, so a snapshot taken before this milestone decodes to the same
        /// three readings it always did.
        /// </summary>
        [Test]
        public void TheEnumGrowsByOne_WithEveryExistingMemberKeepingItsValue()
        {
            Assert.That((int)AttackTargetLine.AnyLine, Is.EqualTo(0));
            Assert.That((int)AttackTargetLine.NotInStraightLine, Is.EqualTo(1));
            Assert.That((int)AttackTargetLine.InStraightLine, Is.EqualTo(2));
            Assert.That((int)AttackTargetLine.InStraightLineClearOfUnitsAndTerrain, Is.EqualTo(3));
            Assert.That(Enum.GetValues(typeof(AttackTargetLine)).Length, Is.EqualTo(4));
        }

        // ---------------------------------------------------------------------------------------
        // What the clause admits
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The case this milestone exists for, asserted as a set equality on one board where each
        /// condition excludes a different candidate.
        ///
        /// <para>Six files carry six different sentences: a token between, a unit between, terrain
        /// between, nothing between, a target standing beside the actor, and a near unit with a
        /// further one behind it. A seventh candidate stands on no file at all, so the straight-line
        /// half of the member is non-vacuous here too. The board is asserted to be discriminating
        /// before anything is asserted about the answer, which is the shape milestone 8.38a's cases
        /// take.</para>
        /// </summary>
        [Test]
        public void TheClause_AdmitsAFileCarryingATokenAndRefusesOneCarryingAUnitOrTerrain()
        {
            var board = ClearLineBoard.Create();

            // The two pieces the whole milestone turns on are asserted to be where the fixture says
            // they are. A token that failed to place, or a terrain space that loaded as playable,
            // would make the two most important expectations below pass for no reason at all.
            Assert.That(
                board.PlacedTokens.Select(token => token.Coordinate),
                Is.EqualTo(new[] { ClearLineBoard.At(HexDirection.East, 1) }),
                "One token, one space out along p.19's east file, between the actor and the hero " +
                "three spaces along it.");
            Assert.That(
                board.MapDefinition.Spaces
                    .Where(space => space.Kind == BoardMapSpaceKind.Terrain)
                    .Select(space => space.Coordinate),
                Is.EqualTo(new[] { ClearLineBoard.At(HexDirection.NorthWest, 1) }),
                "and one declared terrain space, one out along the north-west file.");

            Assert.That(
                Targets(board, Line(AttackTargetLine.AnyLine)),
                Is.EquivalentTo(new[]
                {
                    "hero:" + ClearLineBoard.BehindTheToken.Value,
                    "hero:" + ClearLineBoard.BehindTheUnit.Value,
                    "hero:" + ClearLineBoard.BehindTheTerrain.Value,
                    "hero:" + ClearLineBoard.DownAClearFile.Value,
                    "hero:" + ClearLineBoard.BesideHim.Value,
                    "hero:" + ClearLineBoard.BehindTheNearMinion.Value,
                    "hero:" + ClearLineBoard.OffEveryFile.Value,
                    "minion:" + ClearLineBoard.BlockingMinion.Value,
                    "minion:" + ClearLineBoard.NearMinion.Value
                }),
                "p.13's default Ranged Attack reaches every one of them, so nothing below is a board " +
                "position dressed up as a rule.");

            Assert.That(
                Targets(board, Line(AttackTargetLine.InStraightLine)),
                Has.No.Member("hero:" + ClearLineBoard.OffEveryFile.Value),
                "and milestone 8.38a's plain file already removes the one candidate on no file.");
            Assert.That(
                Targets(board, Line(AttackTargetLine.InStraightLine)),
                Has.Member("hero:" + ClearLineBoard.BehindTheUnit.Value),
                "while it admits everything ON a file whatever stands on it - which is p.9's own " +
                "default and the reason these three faces had to print a sentence at all.");

            Assert.That(
                Targets(board, Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain)),
                Is.EquivalentTo(new[]
                {
                    "hero:" + ClearLineBoard.BehindTheToken.Value,
                    "hero:" + ClearLineBoard.DownAClearFile.Value,
                    "hero:" + ClearLineBoard.BesideHim.Value,
                    "minion:" + ClearLineBoard.BlockingMinion.Value,
                    "minion:" + ClearLineBoard.NearMinion.Value
                }),
                "A token on the file is not one of the two kinds \"no units, or terrain\" names, so " +
                "the hero behind it is still a legal target; the hero behind a minion and the hero " +
                "behind a terrain space are not, and the hero on no file never was.");
        }

        /// <summary>
        /// Both ends of the line are ends rather than places on it, stated here because a walk that
        /// included either would be wrong in a way the main set equality could hide.
        ///
        /// <para>The far end: the blocking minion and the near minion each stand on their own space
        /// and each is admitted, while the hero one space beyond the near minion is refused - so the
        /// same piece is not between itself and is between what stands past it. A unit is the only
        /// obstacle that can be there to test, and that is the board's rule rather than a gap in this
        /// case: p.18 admits one obstacle per space, so a token cannot share the target's space, and
        /// terrain is not playable so nothing stands on it at all. The near end: the actor is a hero
        /// standing at the start of every file, so a walk counting its own space would return the
        /// empty set on this board rather than the five targets it does.</para>
        /// </summary>
        [Test]
        public void AUnitOnEitherEndOfTheLine_IsNotBetweenItsOwnTwoEnds()
        {
            var admitted = Targets(
                ClearLineBoard.Create(), Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain));

            Assert.That(admitted, Has.Member("minion:" + ClearLineBoard.NearMinion.Value),
                "The minion two spaces along its file is the obstacle on its own space, and an " +
                "obstacle standing where the target stands is not \"between you\".");
            Assert.That(admitted, Has.No.Member("hero:" + ClearLineBoard.BehindTheNearMinion.Value),
                "and the identical piece IS between the actor and the hero one space past it.");
            Assert.That(admitted, Has.Member("hero:" + ClearLineBoard.DownAClearFile.Value),
                "The actor is a hero standing at the near end of every file on this board, so a walk " +
                "that counted its own space would admit nothing at all.");
        }

        /// <summary>
        /// p.19's separation-of-one leaves no space to walk, so an adjacent target is unaffected by
        /// the clause however crowded its file is beyond it.
        /// </summary>
        [Test]
        public void AnAdjacentTarget_IsUnaffectedBecauseNoSpaceLiesBetween()
        {
            var admitted = Targets(
                ClearLineBoard.Create(), Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain));

            Assert.That(admitted, Has.Member("hero:" + ClearLineBoard.BesideHim.Value),
                "The hero beside the actor is on a file with nothing between its two ends.");
            Assert.That(admitted, Has.Member("minion:" + ClearLineBoard.BlockingMinion.Value),
                "and so is the minion beside him, even though that same minion blocks the hero two " +
                "spaces further along the identical file.");
        }

        /// <summary>
        /// A hole in the map between two playable spaces is neither a unit nor terrain, so it does not
        /// block.
        ///
        /// <para>That is milestone 8.39b's recorded reading of the same void - off the map is "the
        /// absence of a space" rather than an obstacle - taken here rather than inherited silently,
        /// and it is unreachable on a configured map: p.19 makes the water around the board declared
        /// terrain, which this clause does stop on. The case exists so the reading is pinned rather
        /// than left to whichever map is loaded.</para>
        /// </summary>
        [Test]
        public void AVoidBetweenTwoPlayableSpaces_IsNeitherAUnitNorTerrainAndDoesNotBlock()
        {
            var holed = ClearLineBoard.Create(holeAt: ClearLineBoard.At(HexDirection.West, 2));

            Assert.That(
                Targets(holed, Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain)),
                Has.Member("hero:" + ClearLineBoard.DownAClearFile.Value),
                "The space one step nearer than the target is not on the map at all, and the sentence " +
                "names units and terrain rather than everything that is not a playable space.");
        }

        /// <summary>
        /// Which query the clause asks about terrain, and why the answer cannot move.
        ///
        /// <para>It asks <see cref="TerrainPresenceQuery"/>, which folds the map's printed terrain
        /// together with milestone 8.19c's effective-type overlay, so "what counts as terrain" keeps
        /// one owner. Xargatha's Stone Gaze is the only declaration in the build that reaches it -
        /// "Enemy heroes in radius count as both heroes and terrain" - and it cannot change an answer
        /// here by construction: an overlay names a <see cref="RuleObject"/>, every object it can name
        /// is a unit, and a unit on the file already stops the walk. Asserted on the two places the
        /// overlay could possibly matter - a piece between, and the target itself.</para>
        /// </summary>
        [Test]
        public void AHeroCountingAsTerrain_ChangesNothingBecauseEveryObjectAnOverlayCanNameIsAlreadyAUnit()
        {
            var board = ClearLineBoard.Create();
            var plain = Targets(board, Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain));

            var overTheTarget = Targets(
                board,
                Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain),
                CountingAsTerrain(RuleObject.ForHero(ClearLineBoard.BehindTheToken)));
            Assert.That(overTheTarget, Is.EquivalentTo(plain),
                "A target that counts as terrain is standing at its own end of the line, and an end " +
                "is not between the two ends.");

            var overABlocker = Targets(
                board,
                Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain),
                CountingAsTerrain(RuleObject.ForMinion(ClearLineBoard.BlockingMinion)));
            Assert.That(overABlocker, Is.EquivalentTo(plain),
                "and a piece between that counts as terrain was already blocking as a unit, so the " +
                "overlay adds a second reason for an answer that had one.");
        }

        // ---------------------------------------------------------------------------------------
        // What did not change
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Every Attack declaration written before this milestone keeps its exact target set, derived
        /// from the build's verified content rather than from a remembered list.
        ///
        /// <para><b>The corpus is found by reflection</b>, on the rule milestone 8.38d wrote for the
        /// same problem: a list of hero names closes the hole for the heroes on it and leaves it open
        /// for the next one. Every sealed static <c>*CardContent</c> class in the verified namespace
        /// is read, and every Attack action's own policy plus every <c>attack-sequence@1</c> bullet's
        /// declared line is collected out of it.</para>
        ///
        /// <para><b>The expected set is re-derived from the pages rather than from the query.</b> A
        /// case that compared the query against itself would pass whatever the query happened to do.
        /// <see cref="ExpectedUnderTheOlderReadings"/> writes p.13's reach, p.13's unit narrowing,
        /// p.19's file membership and milestone 8.35z30's isolation union out again, and the board it
        /// is asked on carries a unit and terrain between the actor and two of its candidates - so a
        /// clearance walk leaking into any of the three older members would show up here as a missing
        /// target.</para>
        /// </summary>
        [Test]
        public void EveryAttackDeclarationWrittenBeforeThisMilestone_KeepsItsExactTargetSet()
        {
            var board = ClearLineBoard.Create();
            var declared = DeclaredAttackPolicies().ToArray();

            Assert.That(declared, Is.Not.Empty, "The reflection must find the declarations at all.");
            Assert.That(
                declared.Any(entry => entry.Policy.Line == AttackTargetLine.InStraightLine),
                Is.True,
                "Swift's Red line declares the plain straight line, so the sweep is reading real " +
                "declarations rather than an empty corpus of defaults.");

            // Milestone 8.40i composed the three faces this clause was built for - Light Crossbow,
            // Heavy Crossbow and Arbalest - so the sweep now separates them from everything written
            // before rather than asserting that nothing prints the member. The three are asked the
            // clearance question by the cases above; every OTHER declaration in the build still keeps
            // the exact set p.13 and p.19 gave it.
            var clearLine = declared
                .Where(entry => entry.Policy.Line ==
                    AttackTargetLine.InStraightLineClearOfUnitsAndTerrain)
                .ToArray();
            Assert.That(
                clearLine.Select(entry => entry.Owner).OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "BainCardContent/bain-arbalest/primary-ranged-attack",
                    "BainCardContent/bain-heavy-crossbow/primary-ranged-attack",
                    "BainCardContent/bain-light-crossbow/primary-ranged-attack"
                }),
                "Bain's three plain crossbows are the only faces in the build that print \"no units, " +
                "or terrain, between you\", which is BA-03 and the count 8.40d corrected from five - " +
                "his two modal Reds print a marker clause and no line clause at all.");

            foreach (var entry in declared.Where(entry => entry.Policy.Line !=
                AttackTargetLine.InStraightLineClearOfUnitsAndTerrain))
            {
                Assert.That(
                    Targets(board, entry.Policy),
                    Is.EquivalentTo(ExpectedUnderTheOlderReadings(entry.Policy)),
                    entry.Owner + " keeps the exact set p.13 and p.19 gave it, on a board where a " +
                    "unit and a terrain space stand between the actor and two of its candidates.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // Both consumers of the enum
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The clause reaches an <c>attack-sequence@1</c> bullet as well as a whole-action policy, and
        /// both reach the same answer.
        ///
        /// <para>The bullet's <c>line</c> parameter is declared over this very enum, so the member is
        /// expressible there by construction - which is asserted against the handler's own schema, the
        /// thing content load validates a composed node against - and then composed, read back through
        /// the query the offer-feasibility gate uses, and asked of the same board the whole-action
        /// policy was asked of.</para>
        /// </summary>
        [Test]
        public void TheClause_ReachesAnAttackSequenceBulletAsWellAsAWholeActionPolicy()
        {
            var schema = SyntheticSequenceCapabilities.Handlers().Single().Schema;
            var line = schema.Parameters.Single(
                parameter => parameter.Name == SyntheticSequenceCapabilities.LineParameter);

            Assert.That(
                line.IntegerDomain.Contains(
                    (int)AttackTargetLine.InStraightLineClearOfUnitsAndTerrain),
                Is.True,
                "`attack-sequence@1` declares its line parameter over the whole enum, so a bullet may " +
                "name the clause the moment the member exists.");

            var declared = RequestedAttackSequenceQuery.Declared(
                BulletDefinition(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain));

            Assert.That(declared, Has.Count.EqualTo(1));
            Assert.That(
                declared[0].TargetPolicy.Line,
                Is.EqualTo(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain),
                "and the bullet reads back as the sentence it was composed with.");

            var board = ClearLineBoard.Create();
            Assert.That(
                Targets(board, declared[0].TargetPolicy),
                Is.EquivalentTo(
                    Targets(board, Line(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain))),
                "and a bullet printing the clause admits exactly what a whole action printing it " +
                "admits, because both read one query.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static PrintedAttackTargetPolicy Line(AttackTargetLine line)
        {
            var ranged = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged);
            switch (line)
            {
                case AttackTargetLine.AnyLine:
                    return ranged;
                case AttackTargetLine.InStraightLine:
                    return ranged.RestrictedToTheStraightLine();
                case AttackTargetLine.NotInStraightLine:
                    return ranged.ExcludingTheStraightLine();
                case AttackTargetLine.InStraightLineClearOfUnitsAndTerrain:
                    return ranged.RestrictedToTheStraightLineClearOfUnitsAndTerrain();
                default:
                    throw new ArgumentOutOfRangeException(nameof(line), line, null);
            }
        }

        private static IReadOnlyList<string> Targets(
            AdjacentMoveAuthority board, PrintedAttackTargetPolicy policy) =>
            Targets(board, policy, RuleModificationSet.Empty);

        private static IReadOnlyList<string> Targets(
            AdjacentMoveAuthority board,
            PrintedAttackTargetPolicy policy,
            RuleModificationSet modifications) =>
            AttackTargetQuery
                .FindLegalEnemyTargets(
                    ClearLineBoard.Actor,
                    policy,
                    RangeTotal,
                    ClearLineBoard.Participants(),
                    board,
                    modifications)
                .Select(Key)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();

        private static string Key(AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? "hero:" + target.HeroId.Value
                : "minion:" + target.MinionId.Value;

        private static RuleModificationSet CountingAsTerrain(RuleObject affected) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForEffectiveTypeOverlay(EffectiveUnitKind.Terrain),
                        affected,
                        new ActiveEffectId("clear-line-petrification-probe"),
                        ClearLineBoard.Actor,
                        TeamSide.Red,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>
        /// p.13's reach, p.13's unit narrowing, p.19's file membership and milestone 8.35z30's
        /// isolation union, written out again from the pages so the corpus case above compares the
        /// query against something other than itself.
        /// </summary>
        private static IReadOnlyList<string> ExpectedUnderTheOlderReadings(
            PrintedAttackTargetPolicy policy)
        {
            var pieces = ClearLineBoard.EnemyPieces();
            var origin = ClearLineBoard.Origin;

            bool WithinTheReach(HexCoordinate at)
            {
                var distance = HexDistanceQuery.Calculate(origin, at);
                switch (policy.Reach)
                {
                    case AttackTargetReach.AdjacentOnly:
                        return distance == 1;
                    case AttackTargetReach.ExactlyPrintedRange:
                        return distance == RangeTotal;
                    case AttackTargetReach.PrintedRangeBeyondAdjacent:
                        return distance > 1 && distance <= RangeTotal;
                    default:
                        return distance <= RangeTotal;
                }
            }

            bool OnAnAdmittedFile(HexCoordinate at)
            {
                switch (policy.Line)
                {
                    case AttackTargetLine.NotInStraightLine:
                        return !StraightLineQuery.AreInStraightLine(origin, at);
                    case AttackTargetLine.InStraightLine:
                        return StraightLineQuery.AreInStraightLine(origin, at);
                    default:
                        return true;
                }
            }

            // Milestone 8.35z30's union, including the attacker among the neighbours that can spoil a
            // candidate's isolation, which is what the query itself counts.
            bool IsolatedOrBesideTheActor(HexCoordinate at) =>
                policy.TargetableUnits != AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou ||
                HexDistanceQuery.Calculate(origin, at) == 1 ||
                !ClearLineBoard.EveryPieceIncludingTheActor().Any(other =>
                    !other.Equals(at) && HexDistanceQuery.Calculate(other, at) == 1);

            return pieces
                .Where(piece =>
                    (policy.TargetableUnits != AttackTargetableUnits.HeroesOnly || piece.IsHero) &&
                    WithinTheReach(piece.At) &&
                    OnAnAdmittedFile(piece.At) &&
                    IsolatedOrBesideTheActor(piece.At))
                .Select(piece => piece.Key)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every printed Attack targeting sentence the build's verified content declares: each Attack
        /// action's own policy, and each <c>attack-sequence@1</c> bullet's.
        /// </summary>
        private static IEnumerable<DeclaredPolicy> DeclaredAttackPolicies()
        {
            foreach (var content in VerifiedContentClasses())
            {
                var definitions = (IReadOnlyList<CardDefinition>)content
                    .GetProperty("AllDefinitions").GetValue(null);
                foreach (var card in definitions)
                {
                    foreach (var action in card.ActionDefinitions)
                    {
                        if (action.AttackTargetPolicy.HasValue)
                        {
                            yield return new DeclaredPolicy(
                                content.Name + "/" + card.Id.Value + "/" + action.Id.Value,
                                action.AttackTargetPolicy.Value);
                        }

                        foreach (var root in new[] { action.EffectRoot, action.SecondCardTextRoot })
                        {
                            if (root == null)
                            {
                                continue;
                            }

                            foreach (var bullet in RequestedAttackSequenceQuery.Declared(root))
                            {
                                yield return new DeclaredPolicy(
                                    content.Name + "/" + bullet.BulletId, bullet.TargetPolicy);
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<Type> VerifiedContentClasses() =>
            typeof(PrintedAttackTargetPolicy).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == "GuardsDigital.Core.Cards.Verified" &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal) &&
                    type.GetProperty("AllDefinitions") != null)
                .OrderBy(type => type.Name, StringComparer.Ordinal);

        private static EffectDefinition BulletDefinition(AttackTargetLine line)
        {
            var node = new EffectNodeId("clear-line-bullet");
            return new EffectDefinition(
                new EffectDefinitionId("clear-line-bullet-probe"),
                "1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticSequenceCapabilities.AttackSequenceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticSequenceCapabilities.TargetableUnitsParameter,
                                AttackTargetableUnits.HeroesAndMinions),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticSequenceCapabilities.ReachParameter,
                                AttackTargetReach.PrintedRange),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticSequenceCapabilities.LineParameter, line),
                            EffectInputBinding.FromLiteral(
                                SyntheticSequenceCapabilities.DistinctTargetsParameter,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticSequenceCapabilities.TargetableUnitsOutput,
                                EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticSequenceCapabilities.ReachOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticSequenceCapabilities.LineOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticSequenceCapabilities.DistinctTargetsOutput,
                                EffectValueKind.Boolean)
                        })
                });
        }

        private sealed class DeclaredPolicy
        {
            public DeclaredPolicy(string owner, PrintedAttackTargetPolicy policy)
            {
                Owner = owner;
                Policy = policy;
            }

            public string Owner { get; }

            public PrintedAttackTargetPolicy Policy { get; }
        }

        private sealed class EnemyPiece
        {
            public EnemyPiece(string key, HexCoordinate at, bool isHero)
            {
                Key = key;
                At = at;
                IsHero = isHero;
            }

            public string Key { get; }

            public HexCoordinate At { get; }

            public bool IsHero { get; }
        }

        /// <summary>
        /// The one featureless board every case above reads, built on <c>GrappleLineBoard</c>'s terms:
        /// one zone, no Spawn points, nobody on it but the pieces named here, so the only thing that
        /// can remove a candidate is the clause under test.
        ///
        /// <para>Six of rulebook p.19's directions carry one sentence each, measured from the actor at
        /// the centre of a radius-4 playable disk:</para>
        /// <list type="bullet">
        /// <item>East - a token one space out, an enemy hero three out.</item>
        /// <item>North-east - an enemy minion one space out, an enemy hero three out.</item>
        /// <item>North-west - a terrain space one out, an enemy hero three out.</item>
        /// <item>West - an enemy hero three out and nothing between.</item>
        /// <item>South-west - an enemy hero beside the actor.</item>
        /// <item>South-east - an enemy minion two out, an enemy hero three out.</item>
        /// </list>
        /// <para>and one more enemy hero stands on no file at all.</para>
        /// </summary>
        private static class ClearLineBoard
        {
            internal static readonly MapId Map = new MapId("clear-line-board");
            internal static readonly ZoneId Battle = new ZoneId("clear-line-zone");

            internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

            internal const int PlayableRadius = 4;

            internal static readonly HeroId Actor = new HeroId("clear-line-actor");

            internal static readonly HeroId BehindTheToken = new HeroId("clear-line-behind-the-token");
            internal static readonly HeroId BehindTheUnit = new HeroId("clear-line-behind-the-unit");
            internal static readonly HeroId BehindTheTerrain =
                new HeroId("clear-line-behind-the-terrain");
            internal static readonly HeroId DownAClearFile = new HeroId("clear-line-down-a-clear-file");
            internal static readonly HeroId BesideHim = new HeroId("clear-line-beside-him");
            internal static readonly HeroId BehindTheNearMinion =
                new HeroId("clear-line-behind-the-near-minion");
            internal static readonly HeroId OffEveryFile = new HeroId("clear-line-off-every-file");

            internal static readonly MinionId BlockingMinion =
                new MinionId("clear-line-blocking-minion");
            internal static readonly MinionId NearMinion = new MinionId("clear-line-near-minion");

            private static readonly TeamId RedTeam = new TeamId("clear-line-red");
            private static readonly TeamId BlueTeam = new TeamId("clear-line-blue");

            /// <summary>
            /// The one terrain space, one step from the actor along p.19's north-west file. Terrain is
            /// not playable, so it holds no piece and is declared out of the disk below.
            /// </summary>
            private static HexCoordinate TerrainAt => At(HexDirection.NorthWest, 1);

            /// <summary>The one space on no file through the actor: two east and one north-west.</summary>
            private static HexCoordinate OffFileAt => new HexCoordinate(2, -1);

            internal static HexCoordinate At(HexDirection direction, int steps) =>
                StraightLineQuery.Step(Origin, direction, steps);

            private static IReadOnlyList<(HeroId Hero, HexCoordinate At)> EnemyHeroes =>
                new[]
                {
                    (BehindTheToken, At(HexDirection.East, 3)),
                    (BehindTheUnit, At(HexDirection.NorthEast, 3)),
                    (BehindTheTerrain, At(HexDirection.NorthWest, 3)),
                    (DownAClearFile, At(HexDirection.West, 3)),
                    (BesideHim, At(HexDirection.SouthWest, 1)),
                    (BehindTheNearMinion, At(HexDirection.SouthEast, 3)),
                    (OffEveryFile, OffFileAt)
                };

            private static IReadOnlyList<(MinionId Minion, HexCoordinate At)> EnemyMinions =>
                new[]
                {
                    (BlockingMinion, At(HexDirection.NorthEast, 1)),
                    (NearMinion, At(HexDirection.SouthEast, 2))
                };

            private static HexCoordinate TokenAt => At(HexDirection.East, 1);

            internal static IReadOnlyList<EnemyPiece> EnemyPieces() =>
                EnemyHeroes
                    .Select(entry => new EnemyPiece("hero:" + entry.Hero.Value, entry.At, true))
                    .Concat(EnemyMinions.Select(entry =>
                        new EnemyPiece("minion:" + entry.Minion.Value, entry.At, false)))
                    .ToArray();

            /// <summary>Every piece the isolation union counts, the actor included.</summary>
            internal static IReadOnlyList<HexCoordinate> EveryPieceIncludingTheActor() =>
                EnemyPieces().Select(piece => piece.At).Concat(new[] { Origin }).ToArray();

            /// <summary>
            /// The board. <paramref name="holeAt"/> names one space to leave off the map entirely -
            /// neither playable nor terrain - which is how a case reaches the void between two
            /// playable spaces.
            /// </summary>
            internal static AdjacentMoveAuthority Create(HexCoordinate? holeAt = null)
            {
                var board = new AdjacentMoveAuthority(
                    CreateMap(holeAt),
                    EnemyHeroes
                        .Select(entry => new HeroOccupancy(entry.Hero, entry.At))
                        .Concat(new[] { new HeroOccupancy(Actor, Origin) })
                        .ToArray(),
                    Array.Empty<ObstacleOccupancy>(),
                    EnemyMinions
                        .Select(entry => new QuickGameMinionSetupState(
                            entry.Minion,
                            BlueTeam,
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(Map, SpaceIdFor(entry.At)),
                            entry.At))
                        .ToArray());

                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("clear-line-token"),
                    SyntheticComponentDefinitions.CreateCatalog()
                        .LoadToken(SyntheticComponentDefinitions.HazardV4),
                    Actor,
                    Actor,
                    new CardInstanceId("clear-line-token-source"),
                    TokenAt));

                return board;
            }

            internal static MatchParticipants Participants()
            {
                // One player per hero: MatchParticipants requires "Each player must be represented by
                // exactly one hero", and these cases need enemy heroes on six files at once.
                var actorPlayer = new PlayerId("clear-line-actor-player");
                var heroes = new List<Hero>
                {
                    new Hero(Actor, actorPlayer, HeroDashboard.RulebookLayout)
                };
                var players = new List<Player> { new Player(actorPlayer, RedTeam) };
                foreach (var entry in EnemyHeroes)
                {
                    var player = new PlayerId(entry.Hero.Value + "-player");
                    heroes.Add(new Hero(entry.Hero, player, HeroDashboard.RulebookLayout));
                    players.Add(new Player(player, BlueTeam));
                }

                return new MatchParticipants(
                    new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                    players,
                    heroes);
            }

            private static BoardMapDefinition CreateMap(HexCoordinate? holeAt)
            {
                var zone = new BoardMapZone(Battle, "Clear Line Zone", "lane", null, false, true);
                var terrain = TerrainAt;
                var spaces = Disk(PlayableRadius)
                    .Where(at => !at.Equals(terrain) &&
                        (!holeAt.HasValue || !at.Equals(holeAt.Value)))
                    .Select(at => new BoardMapSpace(
                        SpaceIdFor(at), at, BoardMapSpaceKind.Playable, Battle))
                    .ToList();
                spaces.Add(new BoardMapSpace(
                    SpaceIdFor(terrain), terrain, BoardMapSpaceKind.Terrain, null));

                return new BoardMapDefinition(
                    Map, 1, "Clear Line Board", spaces, new[] { zone },
                    Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
            }

            private static SpaceId SpaceIdFor(HexCoordinate at) => new SpaceId($"q{at.Q}_r{at.R}");

            private static IEnumerable<HexCoordinate> Disk(int radius)
            {
                for (var dq = -radius; dq <= radius; dq++)
                {
                    for (var dr = Math.Max(-radius, -dq - radius);
                        dr <= Math.Min(radius, -dq + radius);
                        dr++)
                    {
                        yield return new HexCoordinate(Origin.Q + dq, Origin.R + dr);
                    }
                }
            }
        }
    }
}
