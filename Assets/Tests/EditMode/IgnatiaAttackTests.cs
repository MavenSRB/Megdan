using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's SEVEN card-text Attacks - two Gold basics and five Reds -
    /// whose target is named by the card rather than chosen at p.14's step 1, with the Tie Breaker coin
    /// picking which of the two printed subjects the card asks for.
    ///
    /// <para><b>What makes her Attacks unlike every other hero's.</b> The action reaches step 1 with no
    /// target at all (<c>AttackTargetBinding.ByCardText</c>), each coin arm SELECTS a subject from its
    /// own narrowing, and <c>set-attack-target@1</c> binds it. So every case here measures the same two
    /// things on one board: which units the printed sentence admits, and that the sentence which ran is
    /// the one the coin named.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaAttackTests
    {
        private static readonly HexCoordinate AdjacentInLine = new HexCoordinate(1, 0);
        private static readonly HexCoordinate AdjacentBehind = new HexCoordinate(0, -1);
        private static readonly HexCoordinate TwoInLine = new HexCoordinate(2, 0);
        private static readonly HexCoordinate TwoOffLine = new HexCoordinate(2, -1);
        private static readonly HexCoordinate ThreeInLine = new HexCoordinate(3, 0);
        private static readonly HexCoordinate ThreeOffLine = new HexCoordinate(3, -1);
        private static readonly HexCoordinate FiveInLine = new HexCoordinate(5, 0);
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);

        // -------------------------------------------------------------------------------------------
        // Chaos Bolt - the Gold basic whose two lines name two different KINDS
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "☀: Target a minion adjacent to you. ☾: Target a hero in range." The two printed subjects
        /// differ in p.19's KIND as well as in reach, which no other face of hers does - so the sun arm
        /// refuses a hero standing on the very space it would accept a minion on, and the moon arm
        /// refuses that minion.
        /// </summary>
        [Test]
        public void ChaosBolt_SunTakesAnAdjacentMinion_AndMoonAHeroAnywhereInRange()
        {
            var sun = new IgnatiaFrame(
                IgnatiaCardContent.ChaosBolt, IgnatiaBoard.Sun, ChaosBoltBoard());
            Assert.That(sun.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.EnemyMinion.Value }),
                "☀ names a MINION and names it ADJACENT: the enemy hero standing beside her is the " +
                "wrong kind, the friendly minion beside her is the wrong side, and the minion three " +
                "spaces out is inside her Range but not beside her.");

            var moon = new IgnatiaFrame(
                IgnatiaCardContent.ChaosBolt, IgnatiaBoard.Moon, ChaosBoltBoard());
            Assert.That(
                moon.Candidates(),
                Is.EquivalentTo(new[] { IgnatiaBoard.Enemy.Value, IgnatiaBoard.SecondEnemy.Value }),
                "☾ names a HERO in range, so both enemy heroes inside the printed Range 3 qualify " +
                "and neither minion does.");
        }

        /// <summary>
        /// The binding, which is what makes a card-text target a TARGET: answering either arm hands
        /// p.14 the unit it named, and the two arms bind two different kinds through the one node.
        /// </summary>
        [Test]
        public void ChaosBolt_BindsWhicheverKindItsOwnArmNamed()
        {
            var sun = new IgnatiaFrame(
                IgnatiaCardContent.ChaosBolt, IgnatiaBoard.Sun, ChaosBoltBoard());
            sun.Pick(IgnatiaBoard.EnemyMinion);
            Assert.That(sun.Step.IsComplete, Is.True);
            Assert.That(sun.Step.BoundAttackTarget,
                Is.EqualTo(AttackTarget.Minion(IgnatiaBoard.EnemyMinion)));

            var moon = new IgnatiaFrame(
                IgnatiaCardContent.ChaosBolt, IgnatiaBoard.Moon, ChaosBoltBoard());
            moon.Pick(IgnatiaBoard.SecondEnemy);
            Assert.That(moon.Step.IsComplete, Is.True);
            Assert.That(moon.Step.BoundAttackTarget,
                Is.EqualTo(AttackTarget.Hero(IgnatiaBoard.SecondEnemy)));
        }

        private static AdjacentMoveAuthority ChaosBoltBoard() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, AdjacentInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, ThreeInLine)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, AdjacentBehind),
                    IgnatiaBoard.RedMinion(IgnatiaBoard.SecondEnemyMinion, ThreeOffLine),
                    IgnatiaBoard.BlueMinion(IgnatiaBoard.FriendlyMinion, new HexCoordinate(-1, 0))
                });

        // -------------------------------------------------------------------------------------------
        // The straight-line line - Playing with Fire, Erratic Fireblast, Loosely-Aimed Firebolts
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "☀: Target a unit in range not in a straight line. ☾: Target a unit in range in a straight
        /// line." p.19's straight line is anchored at her, the two sets are complements inside the one
        /// disk, and the printed RANGE is what bounds both.
        ///
        /// <para>Playing with Fire prints Range 2 and Erratic Fireblast prints Range 3 over the same
        /// sentence, so the same board answers differently for the two faces - which is the printed
        /// number doing the work rather than the narrowing.</para>
        /// </summary>
        [Test]
        public void TheStraightLinePair_SplitsTheDiskAtThePrintedRange()
        {
            var atTwo = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Moon, LineBoard());
            Assert.That(atTwo.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "Playing with Fire prints Range 2, so its ☾ \"in a straight line\" reaches the hero " +
                "two spaces along the file and not the minion three along it.");

            var atThree = new IgnatiaFrame(
                IgnatiaCardContent.ErraticFireblast, IgnatiaBoard.Moon, LineBoard());
            Assert.That(
                atThree.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }),
                "and Erratic Fireblast prints Range 3 over the identical sentence, which is the one " +
                "space of difference between the two faces.");

            var offLine = new IgnatiaFrame(
                IgnatiaCardContent.ErraticFireblast, IgnatiaBoard.Sun, LineBoard());
            Assert.That(
                offLine.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.SecondEnemy.Value, IgnatiaBoard.SecondEnemyMinion.Value
                }),
                "☀ is the exact complement inside the same disk - every unit in range that is NOT on " +
                "one of her six files.");

            Assert.That(
                atThree.Candidates().Intersect(offLine.Candidates()), Is.Empty,
                "and no unit is in both, which is what makes \"in\" and \"not in\" a partition rather " +
                "than two overlapping readings.");
        }

        /// <summary>
        /// p.16's items compose onto the printed icon, exactly as they do for every registered
        /// printed-Range selection: a Range item on her dashboard moves what "in range" means, and
        /// nothing else about the narrowing changes.
        /// </summary>
        [Test]
        public void ThePrintedRangeIsATotal_SoARangeItemWidensBothPrintedLines()
        {
            var participants = IgnatiaBoard.Participants();

            var bare = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Moon, LineBoard(), participants);
            Assert.That(bare.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }));

            var withItem = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Moon, LineBoard(), participants,
                items: IgnatiaBoard.Items(participants, PrintedValueChannel.Range, 1));
            Assert.That(
                withItem.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }),
                "Range 2 + 1 is Range 3, and the minion three spaces along the file is inside it - " +
                "the same total the printed-icon family reads, asked by her own narrowing.");
        }

        private static AdjacentMoveAuthority LineBoard() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, TwoInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, TwoOffLine)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, ThreeInLine),
                    IgnatiaBoard.RedMinion(IgnatiaBoard.SecondEnemyMinion, ThreeOffLine)
                });

        // -------------------------------------------------------------------------------------------
        // The adjacent / maximum-range line - Crack of Doom, Imminent Eruption
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "☀: Target a unit adjacent to you. ☾: Target a unit at maximum range." RULING 1's reading of
        /// "at maximum range" is an EQUALITY: the candidate stands at exactly the printed Range Total,
        /// never merely within it - which is the one thing a fixture can get wrong by staging a unit
        /// that satisfies both readings.
        /// </summary>
        [Test]
        public void MaximumRange_IsExactlyThePrintedRange_AndNotMerelyWithinIt()
        {
            var sun = new IgnatiaFrame(
                IgnatiaCardContent.CrackOfDoom, IgnatiaBoard.Sun, MaxRangeBoard());
            Assert.That(
                sun.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }),
                "☀ \"adjacent to you\" is p.19's ring of six, and both kinds of unit standing on it " +
                "qualify.");

            var moon = new IgnatiaFrame(
                IgnatiaCardContent.CrackOfDoom, IgnatiaBoard.Moon, MaxRangeBoard());
            Assert.That(moon.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.SecondEnemy.Value }),
                "☾ \"at maximum range\" admits ONLY the hero standing at exactly five - Crack of " +
                "Doom's printed Range. The minion three spaces out is inside the disk and is not at " +
                "its edge, and neither adjacent unit is.");
        }

        /// <summary>
        /// The same equality read from the other side: a Range item moves the maximum, so a unit that
        /// was AT it no longer is and the arm has nobody at all.
        /// </summary>
        [Test]
        public void ARangeItemMovesTheMaximum_SoAUnitThatWasAtItNoLongerIs()
        {
            var participants = IgnatiaBoard.Participants();
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.CrackOfDoom, IgnatiaBoard.Moon, MaxRangeBoard(), participants,
                items: IgnatiaBoard.Items(participants, PrintedValueChannel.Range, 1));

            Assert.That(frame.Choice, Is.Null,
                "Range 5 + 1 is Range 6 and nobody stands at six, so the sentence names nobody.");
            Assert.That(
                frame.Outcome("ignatia-attack-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and \"at maximum range\" is an equality rather than a bound, so the hero at five is " +
                "no longer a legal subject - which is the whole difference the item makes.");
        }

        private static AdjacentMoveAuthority MaxRangeBoard() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, AdjacentInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, FiveInLine)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, AdjacentBehind),
                    IgnatiaBoard.RedMinion(IgnatiaBoard.SecondEnemyMinion, ThreeInLine)
                });

        // -------------------------------------------------------------------------------------------
        // The boxed Handicap - the only face of hers whose both arms name HEROES
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.20's Unreliable Jolt: "☀: Target a hero adjacent to you. ☾: Target a hero in range, not
        /// adjacent to you." Both arms are HeroesOnly, and the moon arm is p.9's disk WITHOUT its
        /// innermost ring - which is why the two arms of this one card can never name the same unit.
        /// </summary>
        [Test]
        public void UnreliableJolt_NamesHeroesOnly_AndItsTwoArmsPartitionTheDisk()
        {
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, AdjacentInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, TwoOffLine)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, AdjacentBehind),
                    IgnatiaBoard.RedMinion(IgnatiaBoard.SecondEnemyMinion, TwoInLine)
                });

            var sun = new IgnatiaFrame(IgnatiaCardContent.UnreliableJolt, IgnatiaBoard.Sun, board);
            Assert.That(sun.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "☀ \"a HERO adjacent to you\" - the enemy minion on the other adjacent space is the " +
                "wrong kind, which is the word p.20's Handicap changes from her Gold basic's.");

            var moon = new IgnatiaFrame(IgnatiaCardContent.UnreliableJolt, IgnatiaBoard.Moon, board);
            Assert.That(moon.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.SecondEnemy.Value }),
                "☾ \"a hero in range, NOT adjacent to you\" - the hero standing beside her is " +
                "excluded by those three words, and the minion two spaces out by the kind.");

            Assert.That(
                sun.Candidates().Intersect(moon.Candidates()), Is.Empty,
                "and the two arms of this card can never name one unit, which is what the exclusion " +
                "is for.");
        }

        // -------------------------------------------------------------------------------------------
        // The two repeats
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "May repeat once on a different hero" and "May repeat once on a minion" - the two clauses
        /// her Tier III Reds add to ONE arm each, declared exactly as every printed "may repeat" in
        /// this build is: <see cref="EffectNodeKind.RepeatAction"/> at
        /// <see cref="EffectFailurePolicy.May"/>, bounded at one, with the Actor answering.
        ///
        /// <para>Which arm carries which is the load-bearing half: Loosely-Aimed prints its repeat on
        /// the MOON face and Imminent Eruption on the SUN face, so a transposition would give a player
        /// a second attack on the wrong coin.</para>
        /// </summary>
        [Test]
        public void TheTwoRepeats_SitOnTheArmTheirOwnFacePrintsThemOn_AndAreBoundedAtOne()
        {
            var looselyAimed = RepeatNode(
                IgnatiaCardContent.LooselyAimedFirebolts, "ignatia-attack-moon-repeat");
            Assert.That(looselyAimed.RepeatTargetBinding,
                Is.EqualTo(RepeatTargetBinding.DifferentEnemyHero),
                "\"on a different HERO\" - a hero other than the one this instance took.");

            var imminentEruption = RepeatNode(
                IgnatiaCardContent.ImminentEruption, "ignatia-attack-sun-repeat");
            Assert.That(imminentEruption.RepeatTargetBinding,
                Is.EqualTo(RepeatTargetBinding.DifferentEnemyMinion),
                "\"on a MINION\" - which is the binding whose own remarks read that phrase.");

            foreach (var node in new[] { looselyAimed, imminentEruption })
            {
                Assert.That(node.Kind, Is.EqualTo(EffectNodeKind.RepeatAction));
                Assert.That(node.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                    "A mandatory repeat is refused at construction because an unbounded repeat has " +
                    "no bound, so a printed \"may\" is the policy as well as the word.");
                Assert.That(node.MaximumRepeats, Is.EqualTo(1), "\"once\".");
                Assert.That(node.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            }

            // And the OTHER arm of each of those two faces carries no repeat at all.
            Assert.That(
                IgnatiaBoard.PrimaryText(IgnatiaCardContent.LooselyAimedFirebolts).Nodes
                    .Count(node => node.Kind == EffectNodeKind.RepeatAction),
                Is.EqualTo(1),
                "Loosely-Aimed Firebolts prints one repeat, on its moon face alone.");
            Assert.That(
                IgnatiaBoard.PrimaryText(IgnatiaCardContent.ImminentEruption).Nodes
                    .Count(node => node.Kind == EffectNodeKind.RepeatAction),
                Is.EqualTo(1),
                "and Imminent Eruption one, on its sun face alone.");

            // The five faces that print no repeat declare none, which is HER version of "what the
            // card does not say is absent".
            foreach (var face in new[]
            {
                IgnatiaCardContent.ChaosBolt,
                IgnatiaCardContent.PlayingWithFire,
                IgnatiaCardContent.ErraticFireblast,
                IgnatiaCardContent.CrackOfDoom,
                IgnatiaCardContent.UnreliableJolt
            })
            {
                Assert.That(
                    IgnatiaBoard.PrimaryText(face).Nodes
                        .Any(node => node.Kind == EffectNodeKind.RepeatAction),
                    Is.False,
                    face.Value + " prints no repeat, so it declares none.");
            }
        }

        /// <summary>
        /// The repeat is REACHED, and only on its own coin face: answering Loosely-Aimed's moon arm
        /// opens p.10's confirm-or-decline, and the same card under the sun does not.
        /// </summary>
        [Test]
        public void LooselyAimedsRepeat_OpensOnlyAfterItsOwnMoonArmHasBound()
        {
            var moon = new IgnatiaFrame(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Moon, LineBoard());
            moon.Pick(IgnatiaBoard.Enemy);

            Assert.That(moon.Choice, Is.Not.Null,
                "The binding is done and the printed \"may repeat once\" is the next step.");
            Assert.That(moon.Choice.NodeId.Value, Is.EqualTo("ignatia-attack-moon-repeat"));
            Assert.That(moon.Choice.ConfirmInvocation, Is.Not.Null);
            Assert.That(moon.Choice.DeclineInvocation, Is.Not.Null,
                "\"May\" is a question with two answers, which is why this node owes words for both.");

            moon.Confirm();
            Assert.That(moon.Step.IsComplete, Is.True);
            Assert.That(moon.Step.RepeatRequested.IsRequested, Is.True,
                "and confirming asks the enclosing Attack to run again.");
            Assert.That(moon.Step.RepeatRequested.Binding,
                Is.EqualTo(RepeatTargetBinding.DifferentEnemyHero),
                "on a different hero, which is what the card prints.");

            var sun = new IgnatiaFrame(
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaBoard.Sun, LineBoard());
            sun.Pick(IgnatiaBoard.SecondEnemy);
            Assert.That(sun.Step.IsComplete, Is.True);
            Assert.That(sun.Step.RepeatRequested.IsRequested, Is.False,
                "and the SUN face of the same card prints no repeat, so nothing is offered and " +
                "nothing is asked - the coin decides whether the clause exists at all.");
        }

        /// <summary>
        /// Declining is a real answer: the step completes, the attack stands, and nothing is repeated.
        /// </summary>
        [Test]
        public void DecliningARepeat_CompletesTheTextWithoutRequestingOne()
        {
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.ImminentEruption, IgnatiaBoard.Sun, MaxRangeBoard());
            frame.Pick(IgnatiaBoard.Enemy);

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-attack-sun-repeat"));
            frame.Decline();

            Assert.That(frame.Step.IsComplete, Is.True);
            Assert.That(frame.Step.RepeatRequested.IsRequested, Is.False);
            Assert.That(frame.Step.BoundAttackTarget,
                Is.EqualTo(AttackTarget.Hero(IgnatiaBoard.Enemy)),
                "and the attack the card already bound is untouched by the answer to a later clause.");
        }

        // -------------------------------------------------------------------------------------------
        // Immunity, and an arm with nobody in it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.18: an Attack targets what it may target. Her narrowings ask the same
        /// <c>TargetQuery</c> every printed-icon selection asks, so an immune hero is not offered -
        /// and when he was the arm's only subject, the arm refuses rather than reaching past him.
        /// </summary>
        [Test]
        public void AnImmuneHeroIsNotOffered_AndAnArmWhoseOnlySubjectIsImmuneRefuses()
        {
            var participants = IgnatiaBoard.Participants();

            var open = new IgnatiaFrame(
                IgnatiaCardContent.ErraticFireblast, IgnatiaBoard.Moon, LineBoard(), participants);
            Assert.That(
                open.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }));

            var immune = new IgnatiaFrame(
                IgnatiaCardContent.ErraticFireblast, IgnatiaBoard.Moon, LineBoard(), participants,
                modifications: IgnatiaBoard.ImmunityFor(
                    participants, IgnatiaBoard.Enemy, RuleActivity.Attack));
            Assert.That(immune.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.EnemyMinion.Value }),
                "The hero p.18 makes immune to Attacks is gone from a sentence whose whole job is to " +
                "name an Attack's target.");

            var onlySubject = new IgnatiaFrame(
                IgnatiaCardContent.PlayingWithFire, IgnatiaBoard.Moon, LineBoard(), participants,
                modifications: IgnatiaBoard.ImmunityFor(
                    participants, IgnatiaBoard.Enemy, RuleActivity.Attack));
            Assert.That(onlySubject.Choice, Is.Null);
            Assert.That(
                onlySubject.Outcome("ignatia-attack-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Playing with Fire's Range 2 reaches one unit along the file and he is immune, so " +
                "the moon line names nobody.");
        }

        /// <summary>
        /// THE CLAUSE A COIN-BRANCH MAKES EASY TO GET WRONG: an arm with nobody in it refuses, and the
        /// OTHER arm is not run instead. "Apply the text matching the symbol" names one text, and a
        /// board on which that text can do nothing is a card that does nothing - never a card that
        /// quietly applies the other line.
        /// </summary>
        [Test]
        public void AnEmptyArmRefuses_AndTheOtherPrintedLineIsNotRunInstead()
        {
            // Every enemy stands ON one of her files, so the sun line - "not in a straight line" -
            // has nobody at all while the moon line has two.
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, TwoInLine),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(0, 2))
                });

            var moon = new IgnatiaFrame(
                IgnatiaCardContent.ErraticFireblast, IgnatiaBoard.Moon, board);
            Assert.That(
                moon.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.SecondEnemy.Value
                }),
                "The moon line has two subjects on this board.");

            var sun = new IgnatiaFrame(
                IgnatiaCardContent.ErraticFireblast, IgnatiaBoard.Sun, board);
            Assert.That(sun.Choice, Is.Null, "and the sun line has none, so nobody is asked.");
            Assert.That(
                sun.Outcome("ignatia-attack-sun-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                sun.Outcome("ignatia-attack-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.None),
                "and the MOON arm never ran: a Conditional takes one branch, and an empty branch is " +
                "still the branch the coin named.");
            Assert.That(sun.Step.BoundAttackTarget.IsDefined, Is.False,
                "so p.14 is handed no target at all, which is the honest answer rather than the " +
                "other line's.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static EffectNodeDefinition RepeatNode(CardDefinitionId cardId, string nodeId) =>
            IgnatiaBoard.PrimaryText(cardId).Nodes.Single(node => node.Id.Value == nodeId);
    }
}
