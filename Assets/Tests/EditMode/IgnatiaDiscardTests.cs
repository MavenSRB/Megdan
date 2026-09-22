using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's Blue DISCARD line - Abrupt Combustion, Spontaneous Immolation
    /// and Violent Conflagration - the three faces whose sun makes an enemy hero throw a card away and
    /// whose moon takes an enemy minion off the board.
    ///
    /// <list type="bullet">
    /// <item><description>Abrupt Combustion / Spontaneous Immolation: "☀ An enemy hero in radius,
    /// adjacent to a token, or a minion, discards a card, if able. ☾ Remove an enemy minion in radius
    /// adjacent to an enemy hero."</description></item>
    /// <item><description>Violent Conflagration: the same two subjects, with "discards a card, OR IS
    /// DEFEATED" and "DEFEAT an enemy minion" - the two words its Tier changes.</description></item>
    /// </list>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaDiscardTests
    {
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(2, 0);
        private static readonly HexCoordinate MinionBesideEnemyAt = new HexCoordinate(3, 0);
        private static readonly HexCoordinate LonelyEnemyAt = new HexCoordinate(0, 2);
        private static readonly HexCoordinate BesideLonelyEnemyAt = new HexCoordinate(0, 3);
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);

        private static readonly CardInstanceId EnemyCard = new CardInstanceId("ignatia-enemy-hand");

        /// <summary>
        /// One enemy hero standing beside an enemy minion, and a second enemy hero standing beside
        /// nothing at all - the two halves of the printed narrowing, on one board.
        /// </summary>
        private static AdjacentMoveAuthority Board() =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, EnemyAt),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, LonelyEnemyAt)
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, MinionBesideEnemyAt)
                });

        /// <summary>One card in the named hero's hand, so "discards a card" has something to take.</summary>
        private static IEnumerable<CardInstance> HandFor(HeroId hero) =>
            new[]
            {
                CardInstance.CreateInHand(
                    EnemyCard,
                    IgnatiaBoard.Face(IgnatiaCardContent.ChaosBolt).Reference,
                    hero)
            };

        // -------------------------------------------------------------------------------------------
        // The sun narrowing - "adjacent to a token, or a minion"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The printed clause names TWO ways to qualify and a hero satisfying neither is not a subject.
        /// A minion beside him is one; a token beside him is the other; standing alone is neither.
        /// </summary>
        [Test]
        public void TheSunLineNamesOnlyAnEnemyHeroBesideATokenOrAMinion()
        {
            var besideAMinion = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, Board());
            Assert.That(
                besideAMinion.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "The enemy with a minion beside him qualifies; the enemy standing alone two spaces " +
                "the other way does not, and \"adjacent to\" is what tells them apart.");

            var board = Board();
            IgnatiaBoard.PlaceToken(board, BesideLonelyEnemyAt);
            var besideAToken = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, board);
            Assert.That(
                besideAToken.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.SecondEnemy.Value
                }),
                "and one magma token dropped beside the second enemy is the OTHER half of the same " +
                "printed clause: the two are joined by \"or\", so either qualifies.");
        }

        /// <summary>
        /// ID-Q8c, SETTLED at this block: "a token" is ANY token, not only her magma.
        ///
        /// <para>The card prints "adjacent to a token, or a minion" with no qualifier on the noun -
        /// not "your token", not "a magma token" - and p.19's word covers every placed token whoever
        /// put it there. The sentence naming a token AND a minion separately is what makes it narrower
        /// than p.18's obstacle list, which is where terrain and other heroes are; it is not what makes
        /// it narrower than "token".</para>
        ///
        /// <para>The inventory pass read it as her magma alone. This case is the measurement that
        /// settles it, and it is the reason the narrowing asks the token authority for ANY token at
        /// that space rather than matching a definition id.</para>
        /// </summary>
        [Test]
        public void AForeignTokenSatisfiesThePrintedWordJustAsHerMagmaDoes()
        {
            var magma = Board();
            IgnatiaBoard.PlaceToken(magma, BesideLonelyEnemyAt);
            var withMagma = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, magma);

            var foreign = Board();
            IgnatiaBoard.PlaceToken(
                foreign, BesideLonelyEnemyAt, IgnatiaBoard.ForeignTokenDefinition);
            var withForeign = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, foreign);

            Assert.That(
                withForeign.Candidates(), Is.EqualTo(withMagma.Candidates()),
                "The same board with somebody else's token on the same space admits the same heroes: " +
                "\"a token\" is the printed word, and a narrowing to her own magma is one the card " +
                "does not carry.");
            Assert.That(
                withForeign.Candidates(), Has.Member(IgnatiaBoard.SecondEnemy.Value),
                "and the hero it qualifies is the one standing beside it.");
        }

        /// <summary>
        /// The printed RADIUS bounds the sun narrowing exactly as it bounds every other selection of
        /// hers, and the Tier II prints a wider one than the Tier I over the same sentence.
        /// </summary>
        [Test]
        public void ThePrintedRadiusBoundsTheSunNarrowing_AndTheTierIIPrintsAWiderOne()
        {
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(4, 0)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(0, 6))
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, new HexCoordinate(5, 0))
                });

            var tierOne = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, board);
            Assert.That(tierOne.Choice, Is.Null,
                "Abrupt Combustion prints Radius 3, which does not reach a hero four spaces out.");

            var tierTwo = new IgnatiaFrame(
                IgnatiaCardContent.SpontaneousImmolation, IgnatiaBoard.Sun, board);
            Assert.That(tierTwo.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "and Spontaneous Immolation prints Radius 4 over the identical sentence, which is " +
                "the one space of difference the upgrade buys on this clause.");
        }

        // -------------------------------------------------------------------------------------------
        // The discard itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The card the sentence takes is chosen by the hero who loses it, not by Ignatia - p.10's
        /// rule about who answers a decision, which this sentence makes somebody else's.
        /// </summary>
        [Test]
        public void TheHeroWhoDiscardsChoosesTheCard()
        {
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, Board(),
                extraCards: HandFor(IgnatiaBoard.Enemy));
            frame.Pick(IgnatiaBoard.Enemy);

            Assert.That(frame.Choice, Is.Not.Null);
            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-discard-sun-discard"));
            Assert.That(frame.Choice.ChooserId, Is.EqualTo(IgnatiaBoard.Enemy),
                "\"An enemy hero ... discards a card\" - the hero discarding picks which, which is " +
                "why this pause is answered by his seat and not hers.");
            Assert.That(
                frame.Candidates(), Is.EqualTo(new[] { EnemyCard.Value }),
                "and the cards he may give up are the ones in his hand.");

            frame.Pick(EffectValue.Card(EnemyCard));
            Assert.That(frame.Step.IsComplete, Is.True);
        }

        /// <summary>
        /// "If able" is p.17's keyword over the discard alone: an enemy hero holding nothing is still a
        /// legal subject who simply gives nothing, and the Skill goes on.
        ///
        /// <para>That is the reading the selection declares no consumer for - a hero with an empty hand
        /// is offered rather than filtered out - and it is what tells this sentence apart from Violent
        /// Conflagration's, which does something else to exactly that hero.</para>
        /// </summary>
        [Test]
        public void AHeroWithAnEmptyHandIsStillNamed_AndSimplyGivesNothing()
        {
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Sun, Board());

            Assert.That(frame.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "He holds nothing and is offered anyway: \"if able\" is about the discard, not about " +
                "who the sentence is allowed to name.");

            frame.Pick(IgnatiaBoard.Enemy);
            Assert.That(frame.Step.IsComplete, Is.True,
                "and the step completes without asking him anything, because there is nothing to ask " +
                "about.");
            Assert.That(
                frame.Step.RequestedHeroDefeats, Is.Empty,
                "Abrupt Combustion's sentence ends there - nothing else happens to a hero who could " +
                "not discard, which is the whole of the difference from the Tier III.");
        }

        /// <summary>
        /// "Discards a card, OR IS DEFEATED" - Violent Conflagration's Tier III change, composed as ONE
        /// mandatory step with two ways to complete rather than as a decision. Nobody chooses: the hero
        /// discards if he can, and is defeated if he cannot.
        /// </summary>
        [Test]
        public void ViolentConflagration_DefeatsTheHeroWhoCannotDiscard_AndAsksNobody()
        {
            var empty = new IgnatiaFrame(
                IgnatiaCardContent.ViolentConflagration, IgnatiaBoard.Sun, Board());
            empty.Pick(IgnatiaBoard.Enemy);

            Assert.That(empty.Step.IsComplete, Is.True);
            Assert.That(
                empty.Step.RequestedHeroDefeats.Select(defeat => defeat.HeroId),
                Is.EqualTo(new[] { IgnatiaBoard.Enemy }),
                "He held nothing, so the second way to complete the step is the one that ran - and " +
                "no pause stood between them, because p.10 gives nobody a decision here.");

            var holding = new IgnatiaFrame(
                IgnatiaCardContent.ViolentConflagration, IgnatiaBoard.Sun, Board(),
                extraCards: HandFor(IgnatiaBoard.Enemy));
            holding.Pick(IgnatiaBoard.Enemy);
            holding.Pick(EffectValue.Card(EnemyCard));

            Assert.That(holding.Step.IsComplete, Is.True);
            Assert.That(
                holding.Step.RequestedHeroDefeats, Is.Empty,
                "and a hero who COULD discard is not defeated: the same one step, completed the " +
                "other way.");
        }

        // -------------------------------------------------------------------------------------------
        // The moon narrowing, and Remove against Defeat
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "An enemy minion in radius adjacent to an enemy hero" - the mirror narrowing, and a minion
        /// standing beside nobody is not a subject.
        /// </summary>
        [Test]
        public void TheMoonLineNamesOnlyAnEnemyMinionBesideAnEnemyHero()
        {
            var beside = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Moon, Board());
            Assert.That(beside.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.EnemyMinion.Value }));

            var alone = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(0, 6)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(1, 6))
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, MinionBesideEnemyAt)
                });

            var frame = new IgnatiaFrame(
                IgnatiaCardContent.AbruptCombustion, IgnatiaBoard.Moon, alone);
            Assert.That(frame.Choice, Is.Null,
                "The same minion on the same space, with every enemy hero moved away, is nobody's " +
                "subject - \"adjacent to an enemy hero\" is the clause and it is not satisfied.");
            Assert.That(
                frame.Outcome("ignatia-discard-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        /// <summary>
        /// THE TIER III'S SECOND CHANGED WORD. Abrupt Combustion and Spontaneous Immolation REMOVE the
        /// minion - p.19's operation, which takes the miniature off the board and collects nothing -
        /// while Violent Conflagration DEFEATS it, which runs p.13's list and pays its bounty.
        ///
        /// <para>Measured as the difference between the two requests the step reports, because that is
        /// exactly where a Remove and a Defeat part company: a Defeat is staged for the card authority
        /// to run p.13 over, and a Remove has already happened.</para>
        /// </summary>
        [Test]
        public void TheLowerTiersRemoveTheMinion_AndTheTierIIIDefeatsIt()
        {
            foreach (var face in new[]
            {
                IgnatiaCardContent.AbruptCombustion,
                IgnatiaCardContent.SpontaneousImmolation
            })
            {
                var frame = new IgnatiaFrame(face, IgnatiaBoard.Moon, Board());
                frame.Pick(IgnatiaBoard.EnemyMinion);

                Assert.That(frame.Step.IsComplete, Is.True, face.Value);
                Assert.That(frame.Holds(IgnatiaBoard.EnemyMinion), Is.False,
                    face.Value + " REMOVES it: the miniature is off the board when the step ends.");
                Assert.That(
                    frame.Step.RequestedMinionDefeats, Is.Empty,
                    face.Value + " asks for no Defeat at all, so p.13's list never runs and nobody " +
                    "is paid - which is the difference p.18 and p.19 draw between the two words.");
            }

            var violent = new IgnatiaFrame(
                IgnatiaCardContent.ViolentConflagration, IgnatiaBoard.Moon, Board());
            violent.Pick(IgnatiaBoard.EnemyMinion);

            Assert.That(
                violent.Step.RequestedMinionDefeats, Is.EqualTo(new[] { IgnatiaBoard.EnemyMinion }),
                "Violent Conflagration DEFEATS it, which is staged for the authority that runs " +
                "p.13's consequences rather than performed as a board edit.");
        }

        /// <summary>
        /// Her three discard faces name exactly two contracts of her own, and they are the two the
        /// printed sentences could not be built from anything registered - stated as a set so a third
        /// arriving unnoticed fails here.
        /// </summary>
        [Test]
        public void TheDiscardLineNamesExactlyTwoContractsOfHerOwn()
        {
            var named = new[]
                {
                    IgnatiaCardContent.AbruptCombustion,
                    IgnatiaCardContent.SpontaneousImmolation,
                    IgnatiaCardContent.ViolentConflagration
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
                "ignatia-enemy-hero-adjacent-to-magma-or-minion@1",
                "ignatia-enemy-minion-adjacent-to-enemy-hero@1"
            }),
                "The coin every face of hers branches on, and the two narrowings this line mints. " +
                "Everything else these three bodies reach - the discard at both revisions, the two " +
                "Defeats, the Remove and the flag predicate - is the roster's own vocabulary.");
        }
    }
}
