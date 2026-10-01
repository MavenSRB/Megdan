using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30d: Gydion's twelve spells that compose on existing contracts, each cast through
    /// 9.30c's path on a staged round and measured on the board, the hands, the coins and the Lives
    /// (9.13's style: the board, the play, the answers, what must follow). The caster (Red) owns the
    /// twenty-two spells in his book; a Red ally, two Blue enemies and the minions and tokens each case
    /// names stand on a radius-5 disk that is one Battle Zone. Nothing is registered: the fixture's
    /// catalog carries Gydion's deferred handlers.
    /// </summary>
    public sealed class GydionReuseSpellTests
    {
        private static readonly CardDefinitionId[] TheTwelve =
        {
            GydionCardContent.MagicMissile, GydionCardContent.ShockingGrasp, GydionCardContent.ExpeditiousRetreat,
            GydionCardContent.BurningHands, GydionCardContent.Fireball, GydionCardContent.VampiricTouch,
            GydionCardContent.CreateUndead, GydionCardContent.DominatePerson, GydionCardContent.PowerWordKill,
            GydionCardContent.Disintegrate, GydionCardContent.Polymorph, GydionCardContent.Banishment
        };

        // -------------------------------------------------------------------------------------------
        // The content
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheTwelve_AreComposed_AndPassTheValidatorAMatchCompositionRuns()
        {
            var catalog = ReuseRound.CreateCatalogs().Capabilities;
            foreach (var id in TheTwelve)
            {
                var spell = GydionCardContent.Definition(id);
                var primary = spell.ActionDefinitions.Single();
                Assert.That(primary.TextComposition.IsAwaited, Is.False, id.Value + ": composed at 9.30d");
                Assert.That(BorrowedPrimaryActionDeclaration.Refuses(spell), Is.EqualTo(BorrowRefusal.None),
                    id.Value + ": a cast performs it");
                if (primary.EffectRoot == null)
                {
                    continue;
                }

                var rejections = primary.CardTextTiming != CardTextTiming.None
                    ? EffectDefinitionValidator.ValidateCardText(primary.EffectRoot, catalog, primary.CardTextTiming)
                    : EffectDefinitionValidator.Validate(primary.EffectRoot, catalog);
                Assert.That(rejections, Is.Empty, id.Value + ": the definition a match composition validates");
            }
        }

        [Test]
        public void TheOneConfirmOrDecline_IsVampiricTouchs_WithItsLabel()
        {
            var catalogs = ReuseRound.CreateCatalogs();
            var labels = GydionCardContent.ReuseSpellDecisionLabels().ToArray();
            foreach (var id in TheTwelve)
            {
                var required = DecisionLabelRequirementQuery.For(GydionCardContent.Definition(id), catalogs)
                    .Select(row => row.NodeId).ToArray();
                Assert.That(required, Is.EquivalentTo(labels.Where(label => label.CardId.Equals(id))
                    .Select(label => label.NodeId).ToArray()), id.Value + ": the controls it raises are labelled");
            }

            Assert.That(labels.Single().Label.Chosen, Is.EqualTo("Retrieve a discarded card"));
            Assert.That(labels.Single().Label.Declined, Is.EqualTo("Leave my discard as it is"));
        }

        /// <summary>
        /// The closing check's second half: the twelve faces are in the printed-wording corpus before
        /// the dealing, so every family in <c>PrintedWordingConformanceTests</c> holds them now.
        /// </summary>
        [Test]
        public void TheTwelve_AreHeldToEveryPrintedWordingFamily_BeforeTheDealing()
        {
            Assert.That(GydionCardContent.ReuseSpellIds, Is.EqualTo(TheTwelve), "the block's own list");
            var faces = PrintedWordingCorpus.All.Where(face => face.Hero == GydionCardContent.ContentSetId)
                .Select(face => face.Card.Id).ToArray();
            Assert.That(faces, Is.SupersetOf(TheTwelve));
            Assert.That(PrintedWordingCorpus.All.Single(face => face.Card.Id.Equals(GydionCardContent.BurningHands))
                .Bodies, Is.Not.Empty, "with the bodies they compose");
        }

        /// <summary>
        /// Lesson 18: the question 9.6 asks of a spell's primary before the cast reads the board the cast
        /// then finds - a target or a choice exactly where the probe said the primary can act.
        /// </summary>
        [Test]
        public void EachSpellsProbe_ReadsWhatItsCastFinds()
        {
            var boards = new (string Name, CardDefinitionId Spell, Func<CardDefinitionId, ReuseRound> Stage)[]
            {
                ("fireball, enemy free", GydionCardContent.Fireball,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(2, 0))),
                ("fireball, enemy beside his minion", GydionCardContent.Fireball,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(2, 0),
                        minions: new[] { ReuseRound.RedMelee("friend", new HexCoordinate(3, 0)) })),
                ("magic missile, enemy beside him", GydionCardContent.MagicMissile,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(1, 0))),
                ("dominate person, minion beside the hero", GydionCardContent.DominatePerson,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(2, 0),
                        minions: new[] { ReuseRound.BlueMelee("m", new HexCoordinate(2, 1)) })),
                ("dominate person, no minion beside the hero", GydionCardContent.DominatePerson,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(2, 0))),
                ("power word kill, empty hand", GydionCardContent.PowerWordKill,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(2, 0), enemyHolds: false)),
                ("power word kill, holding a card", GydionCardContent.PowerWordKill,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(2, 0))),
                ("disintegrate, lone heavy", GydionCardContent.Disintegrate,
                    spell => new ReuseRound(spell, reserveMinions: false,
                        minions: new[] { ReuseRound.Blue("h", new HexCoordinate(1, 0), StandardMinionType.Heavy) })),
                ("disintegrate, token", GydionCardContent.Disintegrate,
                    spell => new ReuseRound(spell, familiarAt: new HexCoordinate(1, 0))),
                ("create undead, nothing removed", GydionCardContent.CreateUndead,
                    spell => new ReuseRound(spell)),
                ("create undead, one removed", GydionCardContent.CreateUndead,
                    spell => new ReuseRound(spell,
                        minions: new[] { ReuseRound.RedMelee("fallen", new HexCoordinate(-2, -2)) },
                        removedMinions: new[] { "fallen" })),
                ("banishment, nothing beside him", GydionCardContent.Banishment,
                    spell => new ReuseRound(spell)),
                ("banishment, the enemy beside him", GydionCardContent.Banishment,
                    spell => new ReuseRound(spell, enemyAt: new HexCoordinate(1, 0)))
            };

            foreach (var (name, spell, stage) in boards)
            {
                var round = stage(spell);
                var probe = round.SpellsPrimaryCanAct(spell);
                round.StartCasting();
                var found = round.Turn.PendingAttackTargetChoice != null || round.Turn.PendingEffectChoice != null;
                Assert.That(found, Is.EqualTo(probe), name + ": the probe said " + probe);
            }
        }

        /// <summary>GY-Q12: every one is castable while it is in the book, and a cast removes it faceup.</summary>
        [Test]
        public void EveryOneOfTheTwelve_IsCastableFromTheBook_AndIsRemovedFaceupWhenCast_GyQ12()
        {
            foreach (var id in TheTwelve)
            {
                var round = new ReuseRound(id);
                Assert.That(round.IsOffered, Is.True, id.Value + ": castable while in the book (GY-Q12)");
                round.StartCasting();
                Assert.That(round.Spell(id).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                    id.Value + ": cast and removed faceup");

                var removed = new ReuseRound(id, removed: new[] { id });
                Assert.That(removed.IsOffered, Is.False, id.Value + ": a removed spell cannot be cast");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Magic Missile
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: an enemy hero 2 away, a second enemy beside Gydion, an enemy minion 3 away, an enemy
        /// minion 4 away. Play: Magic Missile. Expect: targets in Range 3 and not adjacent - the hero
        /// at 2 and the minion at 3; the Attack is 1.
        /// </summary>
        [Test]
        public void MagicMissile_TargetsAUnitInRangeAndNotAdjacent_WithAttackOne()
        {
            var round = new ReuseRound(GydionCardContent.MagicMissile,
                enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(-1, 0),
                minions: new[]
                {
                    ReuseRound.BlueMelee("three", new HexCoordinate(0, 3)),
                    ReuseRound.BlueMelee("four", new HexCoordinate(0, -4))
                });
            round.StartCasting();
            Assert.That(round.Targets(), Is.EquivalentTo(new[]
            {
                AttackTarget.Hero(ReuseRound.Enemy), AttackTarget.Minion(ReuseRound.Minion("three"))
            }), "in range (3) and not adjacent: not the second enemy beside him, not the minion at 4");

            round.TargetHero(ReuseRound.Enemy);
            Assert.That(round.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(1), "Attack 1");
        }

        [Test]
        public void MagicMissile_ReachesFurtherWithHisRangeItem()
        {
            var round = new ReuseRound(GydionCardContent.MagicMissile,
                enemyAt: new HexCoordinate(4, 0), items: new[] { PrintedValueChannel.Range });
            round.StartCasting();
            Assert.That(round.Targets(), Is.EqualTo(new[] { AttackTarget.Hero(ReuseRound.Enemy) }),
                "Range 3 + the Range item reaches the enemy at 4");
        }

        // -------------------------------------------------------------------------------------------
        // Shocking Grasp
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: an enemy hero beside Gydion holding a Defense 9 card; a second enemy 2 away. Play:
        /// Shocking Grasp; target the hero; he defends. Expect: only the adjacent hero is a target, the
        /// Attack is 3, and after the attack Gydion moves him one space.
        /// </summary>
        [Test]
        public void ShockingGrasp_TargetsAnAdjacentUnit_ThenGydionMovesTheTargetOneSpace()
        {
            var round = new ReuseRound(GydionCardContent.ShockingGrasp,
                enemyAt: new HexCoordinate(1, 0), secondEnemyAt: new HexCoordinate(-2, 0));
            round.StartCasting();
            Assert.That(round.Targets(), Is.EqualTo(new[] { AttackTarget.Hero(ReuseRound.Enemy) }),
                "\"Target a unit adjacent to you\"");
            round.TargetHero(ReuseRound.Enemy);
            Assert.That(round.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(3), "Attack 3");
            round.DefendWith(ReuseRound.EnemyWall);

            var choice = round.Choice;
            Assert.That(choice?.NodeId, Is.EqualTo(GydionCardContent.ShockingGraspMove),
                "after the attack: \"Move the target up to 1 space\"");
            Assert.That(choice.ChooserId, Is.EqualTo(ReuseRound.Caster), "Gydion steers the move");
            var spaces = round.OfferedSpaces();
            Assert.That(spaces, Is.EquivalentTo(new[]
            {
                new HexCoordinate(1, 0), new HexCoordinate(2, 0), new HexCoordinate(2, -1),
                new HexCoordinate(1, -1), new HexCoordinate(1, 1), new HexCoordinate(0, 1)
            }), "every empty space one from the target, and its own space - \"up to\" 1, so 0 too (W5/W9)");

            round.PickSpace(new HexCoordinate(2, 0));
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(2, 0)), "the target moved");
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
        }

        [Test]
        public void ShockingGrasp_MovingZeroSpaces_IsARealAnswer_W5W9()
        {
            var round = new ReuseRound(GydionCardContent.ShockingGrasp, enemyAt: new HexCoordinate(1, 0));
            round.StartCasting();
            round.TargetHero(ReuseRound.Enemy);
            round.DefendWith(ReuseRound.EnemyWall);
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.ShockingGraspMove));
            Assert.That(round.OfferedSpaces(), Has.Member(new HexCoordinate(1, 0)),
                "\"up to\": the target's own space is the 0-space answer");
            round.PickSpace(new HexCoordinate(1, 0));
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(1, 0)), "0 spaces: he stays");
            Assert.That(round.Turn.PendingEffectChoice, Is.Null, "and the action is over");
        }

        [Test]
        public void ShockingGrasp_ADefeatedTarget_IsNotThereToMove()
        {
            var round = new ReuseRound(GydionCardContent.ShockingGrasp,
                minions: new[] { ReuseRound.BlueMelee("beside", new HexCoordinate(0, 1)) });
            round.StartCasting();
            round.TargetMinion("beside");
            Assert.That(round.MinionAt("beside"), Is.Null, "Attack 3 defeats the minion");
            Assert.That(round.Turn.PendingEffectChoice, Is.Null, "nothing is left to move, so nothing is asked");
            Assert.That(round.Card(ReuseRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // -------------------------------------------------------------------------------------------
        // Expeditious Retreat
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: a rock two spaces east of Gydion. Play: Expeditious Retreat (Movement 5). Expect: a
        /// Movement action whose destinations all lie on p.19's six files from his space, stopping
        /// before the rock; after one leg along a file, only that file's continuation.
        /// </summary>
        [Test]
        public void ExpeditiousRetreat_IsAMovementOfFive_OnlyInAStraightLine()
        {
            var rock = new HexCoordinate(2, 0);
            var round = new ReuseRound(GydionCardContent.ExpeditiousRetreat, obstacles: new[] { rock });
            round.StartCasting();
            var move = round.Turn.PendingMovementChoice;
            Assert.That(move, Is.Not.Null, "a Movement action (GY-Q3)");
            Assert.That(move.RemainingAllowance, Is.EqualTo(5), "the spell's Movement 5");

            var offered = move.LegalDestinations.Select(destination => destination.Destination).ToArray();
            Assert.That(offered.All(space => StraightLineQuery.AreInStraightLine(ReuseRound.CasterAt, space)), Is.True,
                "\"Move only in a straight line\": every destination is on one of the six files");
            Assert.That(offered, Has.Member(new HexCoordinate(1, 0)), "up to the rock");
            Assert.That(offered, Has.No.Member(new HexCoordinate(3, 0)), "not through it");
            Assert.That(offered, Has.No.Member(new HexCoordinate(2, -1)), "not a space reached by bending");
            Assert.That(offered, Has.Length.EqualTo(5 + 1 + 4 * 4),
                "the north file's five; the east file's one; four files of four, each ending before the hero or " +
                "minion the fixture stands at its far end");

            round.MoveTo(new HexCoordinate(0, 3));
            var next = round.Turn.PendingMovementChoice;
            Assert.That(next, Is.Not.Null, "two spaces of the allowance are left");
            Assert.That(next.LegalDestinations.Select(destination => destination.Destination).ToArray(),
                Is.EquivalentTo(new[] { new HexCoordinate(0, 4), new HexCoordinate(0, 5) }),
                "after the first leg, only the same file onwards");
            round.MoveTo(new HexCoordinate(0, 5));
            Assert.That(round.HeroAt(ReuseRound.Caster), Is.EqualTo(new HexCoordinate(0, 5)));
        }

        // -------------------------------------------------------------------------------------------
        // Burning Hands
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: an enemy minion beside Gydion, the enemy hero beside that minion holding one card.
        /// Play: Burning Hands on the minion. Expect: before the attack the hero beside the target
        /// discards his card (W3: hit, not skipped), then the minion is defeated by Attack 5.
        /// </summary>
        [Test]
        public void BurningHands_TheEnemyHeroBesideTheTarget_DiscardsBeforeTheAttack_W3()
        {
            var round = new ReuseRound(GydionCardContent.BurningHands,
                enemyAt: new HexCoordinate(2, 0),
                minions: new[] { ReuseRound.BlueMelee("target", new HexCoordinate(1, 0)) });
            round.StartCasting();
            round.TargetMinion("target");

            round.AnswerHeroIfAsked(ReuseRound.Enemy);
            round.EnemyDiscardsIfAsked(ReuseRound.EnemyWall);
            Assert.That(round.Card(ReuseRound.EnemyWall).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "\"Up to 1 enemy hero adjacent to the target discards a card\"");
            Assert.That(round.MinionAt("target"), Is.Null, "then the attack: Attack 5 defeats the minion");
        }

        [Test]
        public void BurningHands_WithTwoHeroesBesideTheTarget_GydionPicksTheOneWhoDiscards()
        {
            var round = new ReuseRound(GydionCardContent.BurningHands,
                enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(1, 1),
                minions: new[] { ReuseRound.BlueMelee("target", new HexCoordinate(1, 0)) });
            round.StartCasting();
            round.TargetMinion("target");

            var choice = round.Choice;
            Assert.That(choice?.NodeId, Is.EqualTo(GydionCardContent.BurningHandsHero), "up to 1 of the two");
            Assert.That(choice.ChooserId, Is.EqualTo(ReuseRound.Caster));
            Assert.That(choice.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EquivalentTo(new[] { ReuseRound.Enemy, ReuseRound.SecondEnemy }));
            Assert.That(choice.DeclineInvocation, Is.Null, "W3: a hero present is hit, not skipped");
            round.PickHero(ReuseRound.SecondEnemy);
            round.EnemyDiscardsIfAsked(ReuseRound.SecondEnemyCard);

            Assert.That(round.Card(ReuseRound.SecondEnemyCard).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(round.Card(ReuseRound.EnemyWall).State.Location, Is.EqualTo(CardLocation.Hand),
                "only one discards");
        }

        [Test]
        public void BurningHands_TheTargetIsNotBesideItself_AndNoHeroBesideItAsksNothing()
        {
            var lone = new ReuseRound(GydionCardContent.BurningHands, enemyAt: new HexCoordinate(1, 0));
            lone.StartCasting();
            lone.TargetHero(ReuseRound.Enemy);
            Assert.That(lone.Turn.PendingEffectChoice, Is.Null, "the target is not adjacent to the target");
            Assert.That(lone.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(5), "straight to p.14: Attack 5");
            Assert.That(lone.Card(ReuseRound.EnemyWall).State.Location, Is.EqualTo(CardLocation.Hand));

            var emptyHanded = new ReuseRound(GydionCardContent.BurningHands, enemyAt: new HexCoordinate(2, 0),
                enemyHolds: false,
                minions: new[] { ReuseRound.BlueMelee("target", new HexCoordinate(1, 0)) });
            emptyHanded.StartCasting();
            emptyHanded.TargetMinion("target");
            emptyHanded.AnswerHeroIfAsked(ReuseRound.Enemy);
            Assert.That(emptyHanded.Turn.PendingEffectChoice, Is.Null, "\"if able\": nothing to discard");
            Assert.That(emptyHanded.MinionAt("target"), Is.Null, "and the attack goes on");
        }

        // -------------------------------------------------------------------------------------------
        // Fireball
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: a Red minion 2 north, the Red ally 3 west; an enemy hero 2 east (beside nobody of
        /// his), an enemy minion beside the Red minion, an enemy minion beside the ally, the second
        /// enemy beside Gydion. Play: Fireball. Expect: only the enemy hero is a target.
        /// </summary>
        [Test]
        public void Fireball_TargetsInRange_NotAdjacentToHimOrToHisFriendlyUnit()
        {
            var round = new ReuseRound(GydionCardContent.Fireball,
                enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(-1, 0),
                allyAt: new HexCoordinate(-3, 0),
                minions: new[]
                {
                    ReuseRound.RedMelee("friend", new HexCoordinate(0, 2)),
                    ReuseRound.BlueMelee("beside-friend", new HexCoordinate(1, 2)),
                    ReuseRound.BlueMelee("beside-ally", new HexCoordinate(-3, 1))
                });
            round.StartCasting();
            Assert.That(round.Targets(), Is.EqualTo(new[] { AttackTarget.Hero(ReuseRound.Enemy) }),
                "not the minion beside his friendly minion, not the one beside his friendly hero, not " +
                "the hero beside him");
            round.TargetHero(ReuseRound.Enemy);
            Assert.That(round.Turn.PendingDefenseChoice?.AttackTotal, Is.EqualTo(5), "Attack 5");
        }

        [Test]
        public void Fireball_AnEnemyBesideAnotherEnemyOrAToken_IsStillATarget()
        {
            var round = new ReuseRound(GydionCardContent.Fireball,
                enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(3, 0),
                familiarAt: new HexCoordinate(2, 1));
            round.StartCasting();
            Assert.That(round.Targets(), Is.EquivalentTo(new[]
            {
                AttackTarget.Hero(ReuseRound.Enemy), AttackTarget.Hero(ReuseRound.SecondEnemy)
            }), "an enemy unit is not his friendly unit, and a token is not a unit (p.19)");
        }

        // -------------------------------------------------------------------------------------------
        // Vampiric Touch
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: an enemy minion beside Gydion; one of his action cards in his discard pile. Play:
        /// Vampiric Touch on the minion. Expect: the minion is defeated; after the attack he may
        /// retrieve - he does, and the card is back in his hand.
        /// </summary>
        [Test]
        public void VampiricTouch_AfterTheAttack_HeMayRetrieveADiscardedCard()
        {
            var round = new ReuseRound(GydionCardContent.VampiricTouch, casterDiscards: true,
                minions: new[] { ReuseRound.BlueMelee("beside", new HexCoordinate(1, 0)) });
            round.StartCasting();
            round.TargetMinion("beside");
            Assert.That(round.MinionAt("beside"), Is.Null, "Attack 5 defeats the minion");

            var may = round.Choice;
            Assert.That(may?.NodeId, Is.EqualTo(GydionCardContent.VampiricTouchMayRetrieve), "\"You may retrieve\"");
            Assert.That(may.DeclineInvocation, Is.Not.Null, "a may");
            round.Confirm();
            Assert.That(round.Choice?.Candidates.Select(candidate => candidate.Value.CardInstanceId).ToArray(),
                Is.EqualTo(new[] { ReuseRound.CasterSpare }),
                "his discarded action card - never a spell, which is in the book or removed faceup");
            round.PickCard(ReuseRound.CasterSpare);
            Assert.That(round.Card(ReuseRound.CasterSpare).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(round.Turn.PendingEffectChoice, Is.Null);
        }

        [Test]
        public void VampiricTouch_DecliningLeavesTheDiscard_AndAnEmptyDiscardAsksNothing()
        {
            var declined = new ReuseRound(GydionCardContent.VampiricTouch, casterDiscards: true,
                minions: new[] { ReuseRound.BlueMelee("beside", new HexCoordinate(1, 0)) });
            declined.StartCasting();
            declined.TargetMinion("beside");
            declined.Decline();
            Assert.That(declined.Card(ReuseRound.CasterSpare).State.Location, Is.EqualTo(CardLocation.DiscardPile));

            var empty = new ReuseRound(GydionCardContent.VampiricTouch,
                minions: new[] { ReuseRound.BlueMelee("beside", new HexCoordinate(1, 0)) });
            empty.StartCasting();
            empty.TargetMinion("beside");
            Assert.That(empty.Turn.PendingEffectChoice, Is.Null, "nothing to retrieve: no question (8.29e)");
        }

        // -------------------------------------------------------------------------------------------
        // Create Undead
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: Red melee spawn points 3 and 4 away and a Blue one 3 away, all empty, in the Battle
        /// Zone; a Red melee minion was removed earlier (p.19's Respawn needs one). Play: Create Undead
        /// (Range 3). Expect: the Red point in range is offered and a Red melee minion returns there.
        /// </summary>
        [Test]
        public void CreateUndead_RespawnsAFriendlyMinion_InAnEmptyFriendlySpawnPointInRange()
        {
            var round = new ReuseRound(GydionCardContent.CreateUndead,
                minions: new[] { ReuseRound.RedMelee("fallen", new HexCoordinate(-2, -2)) },
                removedMinions: new[] { "fallen" });
            Assert.That(round.Board.Minions.Any(minion => minion.TeamSide == TeamSide.Red &&
                minion.MinionType == StandardMinionType.Melee), Is.False, "staged: no Red melee stands");
            round.StartCasting();
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { ReuseRound.RedPointInRange }),
                "the empty friendly point in Range 3 - not the one at 4, not the enemy's");

            round.PickSpace(ReuseRound.RedPointInRange);
            var back = round.Board.Minions.SingleOrDefault(minion => minion.Coordinate.Equals(ReuseRound.RedPointInRange));
            Assert.That(back, Is.Not.Null, "a minion is respawned there");
            Assert.That(back.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(back.MinionType, Is.EqualTo(StandardMinionType.Melee), "the point decides the type");
        }

        [Test]
        public void CreateUndead_HisRangeItemReachesTheFartherPoint()
        {
            var round = new ReuseRound(GydionCardContent.CreateUndead,
                minions: new[] { ReuseRound.RedMelee("fallen", new HexCoordinate(-2, -2)) },
                removedMinions: new[] { "fallen" }, items: new[] { PrintedValueChannel.Range });
            round.StartCasting();
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { ReuseRound.RedPointInRange, ReuseRound.RedPointBeyond }),
                "Range 3 + the Range item (R1)");
        }

        // -------------------------------------------------------------------------------------------
        // Dominate Person
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: the enemy hero 2 away with an enemy minion beside him 3 away; the second enemy 2 away
        /// with only a Red minion beside him. Play: Dominate Person. Expect (GY-Q8): only the first hero
        /// is offered; the minion beside him is defeated, and Gydion collects its coins.
        /// </summary>
        [Test]
        public void DominatePerson_OffersOnlyAHeroWithAnEnemyMinionBesideHim_GyQ8_AndDefeatsIt()
        {
            var round = new ReuseRound(GydionCardContent.DominatePerson,
                enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, -2),
                minions: new[]
                {
                    ReuseRound.BlueMelee("dominated", new HexCoordinate(3, 0)),
                    ReuseRound.RedMelee("ours", new HexCoordinate(0, -3))
                });
            var coins = round.Coins(ReuseRound.Caster);
            round.StartCasting();
            var hero = round.Choice;
            Assert.That(hero?.NodeId, Is.EqualTo(GydionCardContent.DominatePersonHero));
            Assert.That(hero.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { ReuseRound.Enemy }),
                "GY-Q8: the second enemy has no ENEMY minion beside him");
            round.PickHero(ReuseRound.Enemy);

            var minion = round.Choice;
            Assert.That(minion?.NodeId, Is.EqualTo(GydionCardContent.DominatePersonMinion));
            Assert.That(minion.Candidates.Select(candidate => candidate.Value.MinionId).ToArray(),
                Is.EqualTo(new[] { ReuseRound.Minion("dominated") }));
            round.PickMinion("dominated");
            Assert.That(round.MinionAt("dominated"), Is.Null, "defeated");
            Assert.That(round.Coins(ReuseRound.Caster), Is.GreaterThan(coins), "an ordinary p.13 Defeat by Gydion");
        }

        [Test]
        public void DominatePerson_AMinionBesideTheHeroButOutOfRadius_DoesNotQualifyHim()
        {
            var round = new ReuseRound(GydionCardContent.DominatePerson,
                enemyAt: new HexCoordinate(3, 0),
                minions: new[] { ReuseRound.BlueMelee("beyond", new HexCoordinate(4, 0)) });
            round.StartCasting();
            Assert.That(round.Turn.PendingEffectChoice, Is.Null,
                "\"an enemy minion in radius\": the one beside him stands 4 away, so no hero is offered");
            Assert.That(round.MinionAt("beyond"), Is.Not.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Power Word Kill
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: the enemy hero 2 away with no cards in hand; the second enemy 2 away holding one. Play:
        /// Power Word Kill. Expect: only the empty-handed hero is offered; he is defeated - off the
        /// board, his team spends a Life, Gydion collects Defeat coins.
        /// </summary>
        [Test]
        public void PowerWordKill_DefeatsAnEnemyHeroInRadiusWithNoCardsInHand()
        {
            var round = new ReuseRound(GydionCardContent.PowerWordKill,
                enemyAt: new HexCoordinate(2, 0), enemyHolds: false, secondEnemyAt: new HexCoordinate(0, 2));
            var lives = round.Lives(TeamSide.Blue);
            var coins = round.Coins(ReuseRound.Caster);
            round.StartCasting();
            Assert.That(round.Choice?.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { ReuseRound.Enemy }), "the second enemy holds a card");
            round.PickHero(ReuseRound.Enemy);

            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.Null, "defeated and off the board");
            Assert.That(round.Lives(TeamSide.Blue), Is.LessThan(lives), "his team spends Life counters");
            Assert.That(round.Coins(ReuseRound.Caster), Is.GreaterThan(coins), "Gydion collects Defeat coins");
        }

        [Test]
        public void PowerWordKill_AnEmptyHandedHeroOutOfRadius_IsNotOffered()
        {
            var round = new ReuseRound(GydionCardContent.PowerWordKill,
                enemyAt: new HexCoordinate(4, 0), enemyHolds: false);
            round.StartCasting();
            Assert.That(round.Turn.PendingEffectChoice, Is.Null, "Radius 3");
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(4, 0)));
        }

        // -------------------------------------------------------------------------------------------
        // Disintegrate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: beside Gydion his Familiar, an enemy melee minion, the enemy hero and a Red minion.
        /// Play: Disintegrate. Expect: the token and the enemy minion are offered - not the hero, not
        /// his own minion; the minion is removed (no Defeat, no coins).
        /// </summary>
        [Test]
        public void Disintegrate_RemovesATokenOrAnEnemyNonHeavyMinionBesideHim()
        {
            var round = new ReuseRound(GydionCardContent.Disintegrate,
                enemyAt: new HexCoordinate(0, 1), familiarAt: new HexCoordinate(1, 0),
                minions: new[]
                {
                    ReuseRound.BlueMelee("enemy", new HexCoordinate(-1, 0)),
                    ReuseRound.RedMelee("ours", new HexCoordinate(0, -1))
                });
            var coins = round.Coins(ReuseRound.Caster);
            round.StartCasting();
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { new HexCoordinate(1, 0), new HexCoordinate(-1, 0) }),
                "\"a token or an enemy non-heavy minion adjacent to you\"");

            round.PickSpace(new HexCoordinate(-1, 0));
            Assert.That(round.MinionAt("enemy"), Is.Null, "removed");
            Assert.That(round.Board.FindUnitSupply().Any(unit => unit.MinionId.Equals(ReuseRound.Minion("enemy"))), Is.True,
                "p.19: a removed minion rejoins the reserve");
            Assert.That(round.Coins(ReuseRound.Caster), Is.EqualTo(coins), "p.19: Remove collects no coins");
        }

        [Test]
        public void Disintegrate_RemovesHisFamiliar_AndNeverAHeavyMinion()
        {
            var token = new ReuseRound(GydionCardContent.Disintegrate, familiarAt: new HexCoordinate(1, 0));
            token.StartCasting();
            token.PickSpace(new HexCoordinate(1, 0));
            Assert.That(token.Board.PlacedTokens, Is.Empty, "a token is removed");

            // A Heavy with no escort (p.13) is not immune - only "non-heavy" keeps it off the offer.
            var heavy = new ReuseRound(GydionCardContent.Disintegrate, reserveMinions: false,
                minions: new[] { ReuseRound.Blue("heavy", new HexCoordinate(1, 0), StandardMinionType.Heavy) });
            heavy.StartCasting();
            Assert.That(heavy.Turn.PendingEffectChoice, Is.Null, "\"non-heavy\": nothing to remove");
            Assert.That(heavy.MinionAt("heavy"), Is.Not.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Polymorph
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: the enemy hero 2 east and the second enemy 2 north; his Familiar 3 away; an enemy melee
        /// 2 west and an escorted enemy Heavy 3 west; a Red minion 2 south-west. Play: Polymorph; the
        /// enemy hero; then the Familiar. Expect: the second subject is a token or an enemy minion -
        /// never a hero, never his own minion, never the protected Heavy - and the two swap.
        /// </summary>
        [Test]
        public void Polymorph_SwapsAnEnemyHeroWithATokenOrAnEnemyMinionInRadius()
        {
            var round = new ReuseRound(GydionCardContent.Polymorph,
                enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, -2),
                familiarAt: new HexCoordinate(0, 3),
                minions: new[]
                {
                    ReuseRound.BlueMelee("melee", new HexCoordinate(-2, 0)),
                    ReuseRound.Blue("heavy", new HexCoordinate(-3, 0), StandardMinionType.Heavy),
                    ReuseRound.RedMelee("ours", new HexCoordinate(-2, 2))
                });
            round.StartCasting();
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.PolymorphHero));
            Assert.That(round.Choice.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EquivalentTo(new[] { ReuseRound.Enemy, ReuseRound.SecondEnemy }), "an enemy hero in radius");
            round.PickHero(ReuseRound.Enemy);

            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.PolymorphSubject));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { new HexCoordinate(0, 3), new HexCoordinate(-2, 0) }),
                "a token in radius or an enemy minion in radius - the Heavy is immune (p.13)");
            round.PickSpace(new HexCoordinate(0, 3));
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(0, 3)), "swapped");
            Assert.That(round.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
        }

        /// <summary>6.8l: a hero with no token or enemy minion in radius to trade places with is not offered.</summary>
        [Test]
        public void Polymorph_WithNothingToSwapWith_OffersNoHero()
        {
            var round = new ReuseRound(GydionCardContent.Polymorph, enemyAt: new HexCoordinate(2, 0),
                minions: new[] { ReuseRound.BlueMelee("far", new HexCoordinate(0, 4)) });
            Assert.That(round.SpellsPrimaryCanAct(GydionCardContent.Polymorph), Is.False,
                "the probe sees the minion is out of radius");
            round.StartCasting();
            Assert.That(round.Turn.PendingEffectChoice, Is.Null,
                "and so does the hero selection: nobody is asked to pick a hero who can swap with nothing");
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(round.Spell(GydionCardContent.Polymorph).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "the cast still happens (GY-Q12)");
        }

        [Test]
        public void Polymorph_WithAnEnemyMinion()
        {
            var round = new ReuseRound(GydionCardContent.Polymorph,
                enemyAt: new HexCoordinate(2, 0),
                minions: new[] { ReuseRound.BlueMelee("melee", new HexCoordinate(-2, 0)) });
            round.StartCasting();
            round.PickHero(ReuseRound.Enemy);
            round.PickSpace(new HexCoordinate(-2, 0));
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(-2, 0)));
            Assert.That(round.MinionAt("melee"), Is.EqualTo(new HexCoordinate(2, 0)));
        }

        // -------------------------------------------------------------------------------------------
        // Banishment
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Board: beside Gydion the enemy hero, a Red minion and his Familiar; the second enemy 2 away.
        /// Play: Banishment; the enemy hero; a space 3 away. Expect: any unit or token beside him is
        /// offered (either side), the destination is an empty space in Radius 3, and the hero is
        /// placed there.
        /// </summary>
        [Test]
        public void Banishment_PlacesAUnitOrATokenBesideHimIntoASpaceInRadius()
        {
            var round = new ReuseRound(GydionCardContent.Banishment,
                enemyAt: new HexCoordinate(1, 0), secondEnemyAt: new HexCoordinate(2, 0),
                familiarAt: new HexCoordinate(0, 1),
                minions: new[] { ReuseRound.RedMelee("ours", new HexCoordinate(-1, 0)) });
            round.StartCasting();
            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.BanishmentSubject));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[]
            {
                new HexCoordinate(1, 0), new HexCoordinate(-1, 0), new HexCoordinate(0, 1)
            }), "a unit or a token adjacent to you, of either side");
            round.PickSpace(new HexCoordinate(1, 0));

            Assert.That(round.Choice?.NodeId, Is.EqualTo(GydionCardContent.BanishmentPlace));
            var destinations = round.OfferedSpaces();
            Assert.That(destinations.Select(space => HexDistanceQuery.Calculate(ReuseRound.CasterAt, space)),
                Is.All.InRange(1, 3), "in radius");
            Assert.That(destinations, Has.No.Member(new HexCoordinate(2, 0)), "an empty space");
            round.PickSpace(new HexCoordinate(0, -3));
            Assert.That(round.HeroAt(ReuseRound.Enemy), Is.EqualTo(new HexCoordinate(0, -3)), "placed");
        }

        [Test]
        public void Banishment_PlacesHisFamiliar_FartherWithHisRadiusItem()
        {
            var round = new ReuseRound(GydionCardContent.Banishment, familiarAt: new HexCoordinate(0, 1),
                items: new[] { PrintedValueChannel.Radius });
            round.StartCasting();
            round.PickSpace(new HexCoordinate(0, 1));
            Assert.That(round.OfferedSpaces(), Has.Member(new HexCoordinate(0, -4)), "Radius 3 + the item (R1)");
            round.PickSpace(new HexCoordinate(0, -4));
            Assert.That(round.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(new HexCoordinate(0, -4)));
        }

        // -------------------------------------------------------------------------------------------
        // The staged round
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One round on a radius-5 disk that is one Battle Zone with three empty minion spawn points:
        /// the caster (Red, at the centre) owns Gydion's twenty-two spells and has committed a card that
        /// casts <c>spell</c>; a Red ally and two Blue enemies commit cards they can only Hold. The enemy
        /// holds a Defense 9 card, the second enemy one card; a Red and a Blue ranged minion stand at the
        /// disk's edge unless the case clears them.
        /// </summary>
        internal sealed class ReuseRound
        {
            internal static readonly HeroId Caster = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Ally = new HeroId("gydion-test-ally");
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly HeroId SecondEnemy = new HeroId("gydion-test-second-enemy");
            internal static readonly HexCoordinate CasterAt = new HexCoordinate(0, 0);

            internal static readonly CardInstanceId Played = new CardInstanceId("gydion-reuse-played");
            internal static readonly CardInstanceId CasterSpare = new CardInstanceId("gydion-reuse-spare");
            internal static readonly CardInstanceId EnemyWall = new CardInstanceId("gydion-reuse-enemy-wall");
            internal static readonly CardInstanceId SecondEnemyCard = new CardInstanceId("gydion-reuse-second-card");

            internal static readonly HexCoordinate RedPointInRange = new HexCoordinate(-3, 3);
            internal static readonly HexCoordinate RedPointBeyond = new HexCoordinate(-4, 4);
            internal static readonly HexCoordinate BluePointInRange = new HexCoordinate(3, -3);

            private const int BoardRadius = 5;
            private static readonly MapId Map = new MapId("gydion-reuse-board");
            private static readonly ZoneId BattleZone = new ZoneId("gydion-reuse-battle-zone");
            private static readonly TeamId RedTeam = new TeamId(StagedBoardParticipants.RedTeamIdValue);
            private static readonly TeamId BlueTeam = new TeamId(StagedBoardParticipants.BlueTeamIdValue);
            private static readonly ActionDefinitionId CastingPrimary = new ActionDefinitionId("primary-skill");

            private readonly CardActionResolutionAuthority cardActions;
            private readonly MatchParticipants participants = CreateParticipants();
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly TeamLifeState lives;

            internal ReuseRound(
                CardDefinitionId spell,
                IEnumerable<CardDefinitionId> removed = null,
                HexCoordinate? enemyAt = null,
                HexCoordinate? secondEnemyAt = null,
                HexCoordinate? allyAt = null,
                IEnumerable<StagedMinion> minions = null,
                IEnumerable<string> removedMinions = null,
                bool reserveMinions = true,
                HexCoordinate? familiarAt = null,
                IEnumerable<HexCoordinate> obstacles = null,
                IEnumerable<PrintedValueChannel> items = null,
                bool enemyHolds = true,
                bool casterDiscards = false)
            {
                var played = CastingCard(spell);
                Definitions = GydionCardContent.SpellDefinitions
                    .Concat(new[] { played, Hold(1), Hold(2), Hold(3), Spare, Wall })
                    .ToArray();

                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Played, played.Reference, Caster),
                    CardInstance.CreateInHand(CasterSpare, Spare.Reference, Caster),
                    CardInstance.CreateInHand(new CardInstanceId("gydion-reuse-ally-played"), Hold(3).Reference, Ally),
                    CardInstance.CreateInHand(new CardInstanceId("gydion-reuse-enemy-played"), Hold(2).Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("gydion-reuse-second-played"), Hold(1).Reference, SecondEnemy),
                    CardInstance.CreateInHand(SecondEnemyCard, Wall.Reference, SecondEnemy)
                };
                if (enemyHolds)
                {
                    cards.Add(CardInstance.CreateInHand(EnemyWall, Wall.Reference, Enemy));
                }

                var spells = GydionCardContent.SpellDefinitions
                    .Select(definition => CardInstance.CreateInSpellbook(
                        new CardInstanceId("gydion-book-" + definition.Id.Value), definition.Reference, Caster))
                    .ToArray();
                foreach (var gone in removed ?? Enumerable.Empty<CardDefinitionId>())
                {
                    Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(
                        spells.Single(card => card.DefinitionReference.DefinitionId.Equals(gone))), Is.True);
                }

                cards.AddRange(spells);
                Cards = cards.ToArray();

                var standing = (minions ?? Enumerable.Empty<StagedMinion>()).ToList();
                if (reserveMinions)
                {
                    standing.Add(new StagedMinion("red-reserve", new HexCoordinate(-5, 0), TeamSide.Red, StandardMinionType.Ranged));
                    standing.Add(new StagedMinion("blue-reserve", new HexCoordinate(5, -5), TeamSide.Blue, StandardMinionType.Ranged));
                }

                Board = new AdjacentMoveAuthority(
                    CreateMap(),
                    new[]
                    {
                        new HeroOccupancy(Caster, CasterAt),
                        new HeroOccupancy(Ally, allyAt ?? new HexCoordinate(-5, 5)),
                        new HeroOccupancy(Enemy, enemyAt ?? new HexCoordinate(5, 0)),
                        new HeroOccupancy(SecondEnemy, secondEnemyAt ?? new HexCoordinate(0, -5))
                    },
                    (obstacles ?? Enumerable.Empty<HexCoordinate>())
                        .Select((at, index) => new ObstacleOccupancy(new ObstacleId($"gydion-reuse-rock-{index:00}"), at))
                        .ToArray(),
                    standing.Select(minion => new QuickGameMinionSetupState(
                        Minion(minion.Name),
                        minion.Side == TeamSide.Red ? RedTeam : BlueTeam,
                        minion.Side,
                        minion.Type,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("red-point-in-range")),
                        new ConfiguredSpaceReference(Map, SpaceFor(minion.At)),
                        minion.At)).ToArray());
                foreach (var name in removedMinions ?? Enumerable.Empty<string>())
                {
                    Assert.That(Board.CommitValidatedLanePushRemoval(Minion(name)), Is.True,
                        "p.19: a minion is removed before it can be respawned");
                }

                if (familiarAt.HasValue)
                {
                    Board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId("gydion-reuse-familiar"), GydionComponentContent.Familiar,
                        Caster, Caster, new CardInstanceId("gydion-earlier-find-familiar"), familiarAt.Value));
                }

                var markers = new MarkerAttachmentRegister();
                lives = new TeamLifeState(participants, 4);
                cardActions = new CardActionResolutionAuthority(CreateCatalogs(), Board, markers);
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(cardActions, Board, timing, markers,
                        null, null, null, lives, null, null));
                foreach (var item in items ?? Enumerable.Empty<PrintedValueChannel>())
                {
                    Assert.That(Round.HeroItems.TryGrantItemFromTheBox(Caster, item).IsPlaced, Is.True);
                }

                if (casterDiscards)
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(Card(CasterSpare)), Is.True,
                        "staged: one of his action cards in his discard pile");
                }

                foreach (var (hero, card) in new[]
                {
                    (Caster, Played),
                    (Ally, new CardInstanceId("gydion-reuse-ally-played")),
                    (Enemy, new CardInstanceId("gydion-reuse-enemy-played")),
                    (SecondEnemy, new CardInstanceId("gydion-reuse-second-played"))
                })
                {
                    Accept(Round.TryCommitCard(hero, card));
                }

                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            internal readonly struct StagedMinion
            {
                internal StagedMinion(string name, HexCoordinate at, TeamSide side, StandardMinionType type)
                {
                    Name = name;
                    At = at;
                    Side = side;
                    Type = type;
                }

                internal string Name { get; }
                internal HexCoordinate At { get; }
                internal TeamSide Side { get; }
                internal StandardMinionType Type { get; }
            }

            internal static StagedMinion RedMelee(string name, HexCoordinate at) =>
                new StagedMinion(name, at, TeamSide.Red, StandardMinionType.Melee);

            internal static StagedMinion BlueMelee(string name, HexCoordinate at) =>
                new StagedMinion(name, at, TeamSide.Blue, StandardMinionType.Melee);

            internal static StagedMinion Blue(string name, HexCoordinate at, StandardMinionType type) =>
                new StagedMinion(name, at, TeamSide.Blue, type);

            internal static MinionId Minion(string name) => new MinionId("gydion-reuse-minion-" + name);

            internal static MatchContentCatalogs CreateCatalogs()
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                var components = defaults.Components;
                return new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(GydionComponentContent.DeferredHandlers())
                        .Concat(GydionCardContent.ReuseSpellDeferredHandlers())
                        .Concat(SyntheticSpellbookCapabilities.DeferredHandlers())
                        .Concat(SyntheticPlacementCapabilities.DeferredHandlers())),
                    new ComponentDefinitionCatalog(
                        components.RegisteredTokens.Select(components.LoadToken)
                            .Concat(GydionComponentContent.Tokens()),
                        components.RegisteredMarkers.Select(components.LoadMarker)),
                    defaults.TriggeredEffects);
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;

            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal CardInstance Spell(CardDefinitionId spell) =>
                Cards.Single(card => card.OwnerId.Equals(Caster) && card.DefinitionReference.DefinitionId.Equals(spell));

            internal int Coins(HeroId hero) => cardActions.CoinLedger == null ? 0 : cardActions.CoinLedger.GetCoins(hero);

            internal int Lives(TeamSide side) => lives.GetCounters(side == TeamSide.Red ? RedTeam : BlueTeam);

            internal HexCoordinate? HeroAt(HeroId hero) =>
                Board.TryGetHeroCoordinate(hero, out var at) ? at : (HexCoordinate?)null;

            internal HexCoordinate? MinionAt(string name) =>
                Board.Minions.FirstOrDefault(minion => minion.MinionId.Equals(Minion(name)))?.Coordinate;

            internal bool IsOffered => OfferOn(Played).IsAvailable;

            /// <summary>
            /// 9.6's question of <paramref name="spell"/>'s printed primary, asked before the cast with
            /// the spell still in the book - the offer the borrow would mint, judged by the rules the turn
            /// applies to its own offers (6.5af's Skill, 7.8a1's Attack).
            /// </summary>
            internal bool SpellsPrimaryCanAct(CardDefinitionId spell)
            {
                var card = Spell(spell);
                var definition = Definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
                var primary = definition.ActionDefinitions.Single();
                var offer = new ActionOffer(card.Id, definition.Reference, primary.Id, Caster,
                    ActionInvocationContext.TextGranted, true, primary.AttackRange, primary.SourceCardStateTransition);
                return primary.Kind == ActionKind.Attack
                    ? cardActions.CanAttackFindTarget(offer, Definitions, Cards, participants, timing,
                        RuleModificationSet.Empty, 1, Round.TurnIndex)
                    : cardActions.CanSkillApplyAnything(offer, Definitions, Cards, participants, timing,
                        RuleModificationSet.Empty, 1, Round.TurnIndex, out _);
            }

            internal void StartCasting() =>
                Accept(Round.TryStartSkill(new ActionResolutionId("cast-" + Played.Value), OfferOn(Played)));

            internal IReadOnlyList<AttackTarget> Targets()
            {
                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "the cast Attack asks for its target");
                return target.LegalTargets.ToArray();
            }

            internal void TargetHero(HeroId hero) => AnswerTarget(AttackTarget.Hero(hero));

            internal void TargetMinion(string name) => AnswerTarget(AttackTarget.Minion(Minion(name)));

            private void AnswerTarget(AttackTarget target)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null, "the cast Attack asks for its target");
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, target)));
            }

            internal void DefendWith(CardInstanceId card)
            {
                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null, "the target is asked to defend");
                Accept(Round.TryAnswerDefenseResponse(
                    DefenseResponseChoiceCommand.SelectCards(defense.ResolutionId, defense.ChoiceId, card)));
            }

            internal IReadOnlyList<HexCoordinate> OfferedSpaces()
            {
                Assert.That(Choice, Is.Not.Null, "a space is asked");
                return Choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate)
                    .ToArray();
            }

            internal void PickSpace(HexCoordinate space) =>
                Select(candidate => candidate.Value.Kind == EffectValueKind.Coordinate &&
                    candidate.Value.Coordinate.Equals(space), "space " + space);

            internal void PickHero(HeroId hero) =>
                Select(candidate => candidate.Value.Kind == EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(hero), "hero " + hero.Value);

            internal void PickMinion(string name) =>
                Select(candidate => candidate.Value.Kind == EffectValueKind.Minion &&
                    candidate.Value.MinionId.Equals(Minion(name)), "minion " + name);

            internal void PickCard(CardInstanceId card) =>
                Select(candidate => candidate.Value.Kind == EffectValueKind.CardInstance &&
                    candidate.Value.CardInstanceId.Equals(card), "card " + card.Value);

            /// <summary>W3's hero step: answered when the selection asks; a one-candidate pick needs no answer.</summary>
            internal void AnswerHeroIfAsked(HeroId hero)
            {
                if (Choice != null && Choice.NodeId.Equals(GydionCardContent.BurningHandsHero))
                {
                    PickHero(hero);
                }
            }

            /// <summary>p.10: the hero who discards picks the card from his own hand.</summary>
            internal void EnemyDiscardsIfAsked(CardInstanceId card)
            {
                if (Choice != null && Choice.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.CardInstance))
                {
                    PickCard(card);
                }
            }

            internal void Confirm()
            {
                Assert.That(Choice?.ConfirmInvocation, Is.Not.Null, "the choice can be confirmed");
                Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));
            }

            internal void Decline()
            {
                Assert.That(Choice?.DeclineInvocation, Is.Not.Null, "the choice can be declined");
                Accept(Round.TryAnswerEffectChoice(Choice.DeclineInvocation));
            }

            internal void MoveTo(HexCoordinate destination)
            {
                var move = Turn.PendingMovementChoice;
                Assert.That(move, Is.Not.Null, "a Movement destination is asked");
                Accept(Round.TryAnswerMovementChoice(
                    MovementChoiceCommand.SelectDestination(move.ResolutionId, move.ChoiceId, destination)));
            }

            private void Select(Func<EffectChoiceCandidate, bool> matches, string what)
            {
                Assert.That(Choice, Is.Not.Null, "a choice stands for " + what);
                var invocation = Choice.SelectInvocations.SingleOrDefault(candidate => matches(candidate.Candidate));
                Assert.That(invocation, Is.Not.Null, what + " is offered among " +
                    string.Join(", ", Choice.Candidates.Select(candidate => candidate.Value.ToString())));
                Accept(Round.TryAnswerEffectChoice(invocation));
                if (Choice != null && Choice.SelectedCandidates.Count > 0 && Choice.ConfirmInvocation != null &&
                    Choice.SelectedCandidates.Count >= Choice.MaximumSelections)
                {
                    Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));
                }
            }

            private ActionOffer OfferOn(CardInstanceId card) =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(card) && offer.ActionDefinitionId.Equals(CastingPrimary));

            private void Accept(RoundResolutionResult result) =>
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);

            private static CardDefinition CastingCard(CardDefinitionId spell)
            {
                var arm = new EffectNodeId("cast-" + spell.Value);
                return new CardDefinition(
                    new CardDefinitionId("test-cast-" + spell.Value), "rev-1", "TEST - Cast " + spell.Value,
                    new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.BasicCard(CardColor.Silver),
                    new[]
                    {
                        new ActionDefinition(CastingPrimary, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            Array.Empty<PrintedValueDefinition>(),
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default,
                            new EffectDefinition(new EffectDefinitionId("test-cast-" + spell.Value + "-skill"), "rev-1",
                                arm, SyntheticSpellbookCapabilities.CastArm(arm, GydionCardContent.InBookV1(spell))))
                    });
            }

            private static readonly CardDefinition[] Holds = { MakeHold(1), MakeHold(2), MakeHold(3) };

            /// <summary>A card its hero can only Hold, at Initiative 1-3 (each seat its own, no tie).</summary>
            private static CardDefinition Hold(int initiative) => Holds[initiative - 1];

            // Tier III, so a hero holding it has a lowest Tier and a Defeat prices Life counters (p.13).
            private static CardDefinition MakeHold(int initiative) =>
                new CardDefinition(
                    new CardDefinitionId("gydion-reuse-hold-" + initiative), "rev-1", "TEST - Hold",
                    new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.III, PrintedValueChannel.Attack),
                    new[]
                    {
                        new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                            ActionDefinitionOrigin.PrintedSecondary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                    });

            /// <summary>One of Gydion's action cards, for his discard pile.</summary>
            private static readonly CardDefinition Spare = new CardDefinition(
                new CardDefinitionId("gydion-reuse-spare"), "rev-1", "TEST - Spare",
                new InitiativeValue(8), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            /// <summary>A Defense 9 card - the enemies' only card in hand.</summary>
            private static readonly CardDefinition Wall = new CardDefinition(
                new CardDefinitionId("gydion-reuse-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(4), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });

            private static MatchParticipants CreateParticipants()
            {
                var red = new PlayerId(StagedBoardParticipants.RedPlayerIdValue);
                var ally = new PlayerId("gydion-test-ally-player");
                var blue = new PlayerId(StagedBoardParticipants.BluePlayerIdValue);
                var second = new PlayerId("gydion-test-second-enemy-player");
                return new MatchParticipants(
                    new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                    new[]
                    {
                        new Player(red, RedTeam), new Player(ally, RedTeam),
                        new Player(blue, BlueTeam), new Player(second, BlueTeam)
                    },
                    new[]
                    {
                        new Hero(Caster, red, HeroDashboard.RulebookLayout),
                        new Hero(Ally, ally, HeroDashboard.RulebookLayout),
                        new Hero(Enemy, blue, HeroDashboard.RulebookLayout),
                        new Hero(SecondEnemy, second, HeroDashboard.RulebookLayout)
                    });
            }

            private static BoardMapDefinition CreateMap()
            {
                var spaces = (from q in Enumerable.Range(-BoardRadius, 2 * BoardRadius + 1)
                              from r in Enumerable.Range(-BoardRadius, 2 * BoardRadius + 1)
                              where Math.Abs(q + r) <= BoardRadius
                              select new BoardMapSpace(SpaceFor(new HexCoordinate(q, r)), new HexCoordinate(q, r),
                                  BoardMapSpaceKind.Playable, BattleZone)).ToArray();
                return new BoardMapDefinition(
                    Map, 1, "Gydion Reuse Board", spaces,
                    new[] { new BoardMapZone(BattleZone, "Gydion Battle Zone", "lane", null, false, true) },
                    new[]
                    {
                        new BoardMapSpawnPoint(new SpawnPointId("red-point-in-range"), SpaceFor(RedPointInRange), "red", "melee-minion"),
                        new BoardMapSpawnPoint(new SpawnPointId("red-point-beyond"), SpaceFor(RedPointBeyond), "red", "melee-minion"),
                        new BoardMapSpawnPoint(new SpawnPointId("blue-point-in-range"), SpaceFor(BluePointInRange), "blue", "melee-minion")
                    },
                    Array.Empty<ZoneId>());
            }

            private static SpaceId SpaceFor(HexCoordinate at) => new SpaceId($"q{at.Q}_r{at.R}");
        }
    }
}
