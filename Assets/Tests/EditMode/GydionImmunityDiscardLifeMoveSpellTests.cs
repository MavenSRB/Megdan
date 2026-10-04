using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30f: Gydion's Shield, Invulnerability, Cloud Kill, Energy Drain and Suggestion, each
    /// cast through 9.30c's path on a staged round - the caster (Red) owns all twenty-two spells in his
    /// book - and measured on the board, the hands, the Life counters and the Active effects (9.13).
    /// Nothing is registered: the fixture's catalog carries the deferred handlers (9.30i).
    /// </summary>
    public sealed class GydionImmunityDiscardLifeMoveSpellTests
    {
        private static readonly HeroId Caster = SpellRound.Caster;
        private static readonly HeroId Enemy = SpellRound.Enemy;

        // -------------------------------------------------------------------------------------------
        // The content
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheFiveSpells_AreComposed_AndRaiseNoControlThatOwesWords()
        {
            var catalogs = SpellRound.CreateCatalogs();
            Assert.That(GydionCardContent.ImmunityDiscardLifeMoveSpells, Is.EqualTo(new[]
            {
                GydionCardContent.EnergyDrain, GydionCardContent.Suggestion, GydionCardContent.Shield,
                GydionCardContent.Invulnerability, GydionCardContent.CloudKill
            }));
            foreach (var id in GydionCardContent.ImmunityDiscardLifeMoveSpells)
            {
                var spell = GydionCardContent.Definition(id);
                var primary = spell.ActionDefinitions.Single();
                Assert.That(primary.TextComposition.IsAwaited, Is.False, spell.DisplayName + " is composed at 9.30f");
                Assert.That(primary.EffectRoot, Is.Not.Null);
                Assert.That(BorrowedPrimaryActionDeclaration.Refuses(spell), Is.EqualTo(BorrowRefusal.None),
                    spell.DisplayName + " can be cast");
                Assert.That(DecisionLabelRequirementQuery.For(spell, catalogs), Is.Empty,
                    spell.DisplayName + ": no Choose arm and no confirm-or-decline step");
            }

            Assert.That(GydionCardContent.ImmunityDiscardLifeMoveDecisionLabels(), Is.Empty,
                "the labels are derived from the controls the content raises, and it raises none");

            foreach (var (definition, activity) in new[]
            {
                (GydionCardContent.ShieldDefinition, RuleActivity.BasicAttack),
                (GydionCardContent.InvulnerabilityDefinition, RuleActivity.NonBasicAttack)
            })
            {
                Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisRound));
                Assert.That(definition.ExpirationCheckpoint, Is.EqualTo(TimingCheckpoint.RoundEnd));
                Assert.That(definition.CancellationConditions,
                    Has.Member(ActiveEffectCancellationCondition.SourceCardStateChanged),
                    "the spell returning to the book ends it (GY-05 designer)");
                var immunity = definition.Modifications.Single();
                Assert.That(immunity.Kind, Is.EqualTo(RuleModificationKind.Immunity));
                Assert.That(immunity.Activities, Is.EqualTo(new[] { activity }));
            }
        }

        // -------------------------------------------------------------------------------------------
        // Shield and Invulnerability
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// An Attack immunity declared before 9.30f still reaches every Attack (Tigerclaw's "immune to
        /// enemy attack actions"); a class immunity reaches its class and nothing it cannot name.
        /// </summary>
        [Test]
        public void AnAttackImmunity_CoversBothClasses_AndAClassImmunityOnlyItsOwn()
        {
            var attacks = RuleModification.ForImmunity(new[] { RuleActivity.Attack }, RestrictedActorScope.AnyActor);
            var basic = RuleModification.ForImmunity(new[] { RuleActivity.BasicAttack }, RestrictedActorScope.AnyActor);
            var nonBasic = RuleModification.ForImmunity(new[] { RuleActivity.NonBasicAttack }, RestrictedActorScope.AnyActor);

            Assert.That(attacks.Covers(RuleActivity.BasicAttack) && attacks.Covers(RuleActivity.NonBasicAttack) &&
                attacks.Covers(RuleActivity.Attack), Is.True, "every Attack, of either class");
            Assert.That(basic.Covers(RuleActivity.BasicAttack), Is.True);
            Assert.That(basic.Covers(RuleActivity.NonBasicAttack), Is.False);
            Assert.That(basic.Covers(RuleActivity.Attack), Is.False, "an Attack whose class is not stated");
            Assert.That(basic.Covers(RuleActivity.Skill), Is.False);
            Assert.That(nonBasic.Covers(RuleActivity.NonBasicAttack), Is.True);
            Assert.That(nonBasic.Covers(RuleActivity.BasicAttack), Is.False);

            Assert.That(RuleActivityQuery.ForAttack(CardClassification.Basic), Is.EqualTo(RuleActivity.BasicAttack));
            Assert.That(RuleActivityQuery.ForAttack(CardClassification.NonBasic), Is.EqualTo(RuleActivity.NonBasicAttack));
            Assert.That(RuleActivityQuery.ForAttack(CardClassification.Ultimate), Is.EqualTo(RuleActivity.NonBasicAttack),
                "non-basic as Snorri's Oath of Perseverance reads it");
            Assert.That(RuleActivityQuery.ForAttack(CardClassification.None), Is.EqualTo(RuleActivity.Attack));
        }

        [Test]
        public void Shield_MakesHimImmuneToABasicAttack_ButNotToANonBasicOne()
        {
            var basic = new SpellRound(SpellRound.CastingCard(GydionCardContent.Shield),
                enemyPlayed: SpellRound.EnemyBasicStrike);
            basic.StartCasting();
            var shield = basic.Round.Timing.Effects.Single(effect =>
                effect.Definition.Equals(GydionCardContent.ShieldDefinition));
            Assert.That(shield.SourceCardInstanceId, Is.EqualTo(basic.Spell(GydionCardContent.Shield).Id),
                "the Active effect's source is the SPELL, the card the borrow performed");
            Assert.That(shield.ControllerHeroId, Is.EqualTo(Caster));
            basic.ToTheEnemysTurn();
            Assert.That(basic.EnemyStrikeIsOffered, Is.False,
                "\"You are immune to basic attacks\": a Gold card's Attack cannot target him");

            var nonBasic = new SpellRound(SpellRound.CastingCard(GydionCardContent.Shield),
                enemyPlayed: SpellRound.EnemyNonBasicStrike);
            nonBasic.StartCasting();
            nonBasic.ToTheEnemysTurn();
            Assert.That(nonBasic.EnemyStrikeIsOffered, Is.True, "a Tier I card's Attack is not basic");
            Assert.That(nonBasic.EnemyStrikeTargets(), Is.EqualTo(new[] { Caster }));
        }

        [Test]
        public void Invulnerability_MakesHimImmuneToANonBasicAttack_ButNotToABasicOne()
        {
            var nonBasic = new SpellRound(SpellRound.CastingCard(GydionCardContent.Invulnerability),
                enemyPlayed: SpellRound.EnemyNonBasicStrike);
            nonBasic.StartCasting();
            Assert.That(nonBasic.Round.Timing.Effects.Single(effect =>
                    effect.Definition.Equals(GydionCardContent.InvulnerabilityDefinition)).SourceCardInstanceId,
                Is.EqualTo(nonBasic.Spell(GydionCardContent.Invulnerability).Id));
            nonBasic.ToTheEnemysTurn();
            Assert.That(nonBasic.EnemyStrikeIsOffered, Is.False,
                "\"You are immune to non-basic attacks\": a Tier I card's Attack cannot target him");

            var basic = new SpellRound(SpellRound.CastingCard(GydionCardContent.Invulnerability),
                enemyPlayed: SpellRound.EnemyBasicStrike);
            basic.StartCasting();
            basic.ToTheEnemysTurn();
            Assert.That(basic.EnemyStrikeIsOffered, Is.True, "a Gold card's Attack is basic");
            Assert.That(basic.EnemyStrikeTargets(), Is.EqualTo(new[] { Caster }));
        }

        /// <summary>
        /// GY-Q3: a cast spell's Attack is classified by the SPELL. The enemy casts a Gold (basic) Attack
        /// spell from a Red Tier I (non-basic) card, so reading the played card would invert both answers.
        /// </summary>
        [Test]
        public void ACastSpellsAttack_IsClassifiedByTheSpell_NotByTheCardThatCastIt()
        {
            var shielded = new SpellRound(SpellRound.CastingCard(GydionCardContent.Shield),
                enemyPlayed: SpellRound.EnemyNonBasicCastsTheBasicAttackSpell, enemyOwnsSpells: true);
            shielded.StartCasting();
            shielded.ToTheEnemysTurn();
            Assert.That(shielded.IsOfferedOn(SpellRound.EnemyPlayed), Is.True, "GY-Q12: an in-book spell is castable");
            shielded.EnemyCasts();
            Assert.That(shielded.EnemySpell(SyntheticSpellbook.AttackSpellId).State.Location,
                Is.EqualTo(CardLocation.RemovedFromSpellbook), "the enemy's spell is cast");
            Assert.That(shielded.Turn.PendingAttackTargetChoice, Is.Null,
                "the Gold spell's Attack is basic, so the Shielded caster is no target - the Red card that cast it is not what is read");

            var invulnerable = new SpellRound(SpellRound.CastingCard(GydionCardContent.Invulnerability),
                enemyPlayed: SpellRound.EnemyNonBasicCastsTheBasicAttackSpell, enemyOwnsSpells: true);
            invulnerable.StartCasting();
            invulnerable.ToTheEnemysTurn();
            invulnerable.EnemyCasts();
            Assert.That(invulnerable.Turn.PendingAttackTargetChoice?.LegalTargetHeroIds, Is.EqualTo(new[] { Caster }),
                "the Gold spell's Attack is basic, so Invulnerability does not stop it");
        }

        /// <summary>
        /// Lesson 26 and the designer's answer (GY-05): Prepare Spells returns Shield to the book, a p.17
        /// state change, so the immunity ends - and without the refill it runs on through the round.
        /// </summary>
        [Test]
        public void ARefillReturningShield_EndsItsImmunity_WhichOtherwiseRunsOnThroughTheRound()
        {
            var refilled = new SpellRound(SpellRound.CastingCard(GydionCardContent.Shield),
                enemyPlayed: SpellRound.EnemyBasicStrike,
                next: SpellRound.PrepareSpellsCard, enemyNext: SpellRound.EnemyBasicStrike);
            refilled.StartCasting();
            var shield = refilled.Round.Timing.Effects.Single(effect =>
                effect.Definition.Equals(GydionCardContent.ShieldDefinition));
            refilled.ToTheEnemysTurn();
            Assert.That(refilled.EnemyStrikeIsOffered, Is.False, "Shield is running");
            refilled.NextTurn();
            Assert.That(refilled.Round.Timing.Effects.Single(effect => effect.Id.Equals(shield.Id)).EndedAt,
                Is.Null, "\"This round\": it still stands on the next turn, while the refill resolves");

            refilled.StartCastingOff(SpellRound.NextCard);
            Assert.That(refilled.Spell(GydionCardContent.Shield).State, Is.EqualTo(CardStateFacts.InitialInSpellbook()),
                "\"Add all spell cards to the spellbook\": Shield is facedown in the book again");
            Assert.That(refilled.Round.Timing.Effects.Single(effect => effect.Id.Equals(shield.Id)).EndedAt,
                Is.EqualTo(TimingCheckpoint.SourceCardStateChanged),
                "\"Card changes state - becomes facedown\" (GY-05 designer): Shield ends with its spell's return");
            refilled.ToTheEnemysTurn();
            Assert.That(refilled.EnemyStrikeIsOffered, Is.True, "and a basic Attack reaches him again");
            Assert.That(refilled.EnemyStrikeTargets(), Is.EqualTo(new[] { Caster }));

            var held = new SpellRound(SpellRound.CastingCard(GydionCardContent.Shield),
                enemyPlayed: SpellRound.EnemyBasicStrike,
                next: SpellRound.HoldOnlyCard, enemyNext: SpellRound.EnemyBasicStrike);
            held.StartCasting();
            held.ToTheEnemysTurn();
            held.NextTurn();
            Assert.That(held.Round.TryPerformHold(SpellRound.NextCard).Accepted, Is.True);
            held.ToTheEnemysTurn();
            Assert.That(held.EnemyStrikeIsOffered, Is.False, "without the refill Shield still stands this round");
        }

        [Test]
        public void ShieldAndInvulnerability_EndAtTheEndOfTheRound()
        {
            foreach (var spell in new[] { GydionCardContent.Shield, GydionCardContent.Invulnerability })
            {
                var round = new SpellRound(SpellRound.CastingCard(spell));
                round.StartCasting();
                var effect = round.Round.Timing.Effects.Single(value =>
                    value.SourceCardInstanceId.Equals(round.Spell(spell).Id));
                Assert.That(effect.EndedAt, Is.Null);

                round.Round.Timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.RoundEnd, 1, round.Round.TurnIndex),
                    new TieBreakerState(TeamSide.Red));
                Assert.That(round.Round.Timing.Effects.Single(value => value.Id.Equals(effect.Id)).EndedAt,
                    Is.EqualTo(TimingCheckpoint.RoundEnd), spell + ": \"This round\" ends at the end of the round");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Cloud Kill
        // -------------------------------------------------------------------------------------------

        [Test]
        public void CloudKill_AnEnemyHeroInRadiusDiscardsABasicCardOfHisOwnChoosing()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.CloudKill),
                enemyAt: new HexCoordinate(3, 0),
                enemyHand: new[] { SpellRound.EnemyTierICard, SpellRound.EnemySilverCard, SpellRound.EnemyGoldCard });
            round.StartCasting();
            Assert.That(round.Choice.ChooserId, Is.EqualTo(Caster), "Gydion picks the enemy hero");
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { Enemy }), "the enemy hero in radius 3");
            round.PickHero(Enemy);

            Assert.That(round.Choice?.ChooserId, Is.EqualTo(Enemy), "the discarding hero chooses which card");
            Assert.That(round.OfferedCards(), Is.EquivalentTo(new[] { SpellRound.EnemyHandCard(1), SpellRound.EnemyHandCard(2) }),
                "only his basic cards - the Silver and the Gold, never the Tier I");
            round.PickCard(SpellRound.EnemyHandCard(2));
            Assert.That(round.Card(SpellRound.EnemyHandCard(2)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(round.Card(SpellRound.EnemyHandCard(1)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(round.Card(SpellRound.EnemyHandCard(0)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(round.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void CloudKill_AnEnemyWithNoBasicCard_DiscardsNothing_AndTheCastResolves()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.CloudKill),
                enemyAt: new HexCoordinate(2, 0), enemyHand: new[] { SpellRound.EnemyTierICard });
            round.StartCasting();
            Assert.That(round.Choice, Is.Null,
                "\"if able\": no enemy hero in radius holds a basic card, so the sentence can do nothing and nobody is asked");
            Assert.That(round.Card(SpellRound.EnemyHandCard(0)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(round.Spell(GydionCardContent.CloudKill).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(round.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // -------------------------------------------------------------------------------------------
        // Energy Drain
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EnergyDrain_AnEnemyInRangeDiscardsANonBasicCard_AndHisTeamRegainsASpentLife()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.EnergyDrain),
                enemyAt: new HexCoordinate(3, 0),
                enemyHand: new[] { SpellRound.EnemySilverCard, SpellRound.EnemyTierICard },
                redLivesSpent: 2);
            Assert.That(round.RedLives, Is.EqualTo(2));
            round.StartCasting();
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { Enemy }), "the enemy hero in range 3");
            round.PickHero(Enemy);
            Assert.That(round.Choice?.ChooserId, Is.EqualTo(Enemy));
            Assert.That(round.OfferedCards(), Is.EqualTo(new[] { SpellRound.EnemyHandCard(1) }),
                "only his non-basic card");
            round.PickCard(SpellRound.EnemyHandCard(1));
            Assert.That(round.Card(SpellRound.EnemyHandCard(1)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(round.RedLives, Is.EqualTo(3), "\"Your team regains 1 spent Life counter\"");
            Assert.That(round.Round.TeamLives.GetCounters(SpellRound.BlueTeam), Is.EqualTo(4), "and only his team");
            Assert.That(round.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>GY-Q11 (user, 2026-10-01): only a SPENT counter, never above the starting Lives.</summary>
        [Test]
        public void EnergyDrain_NeverRaisesHisTeamAboveItsStartingLives_GyQ11()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.EnergyDrain),
                enemyAt: new HexCoordinate(3, 0), enemyHand: new[] { SpellRound.EnemyTierICard });
            Assert.That(round.RedLives, Is.EqualTo(4), "a full team");
            round.StartCasting();
            round.PickHero(Enemy);
            round.PickCard(SpellRound.EnemyHandCard(0));
            Assert.That(round.RedLives, Is.EqualTo(4), "nothing spent, nothing regained");
            Assert.That(round.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));

            var lives = new TeamLifeState(StagedBoardParticipants.Create(), 4);
            Assert.That(lives.TrySpend(SpellRound.RedTeam, 1).Accepted, Is.True);
            var regain = lives.TryRegainSpent(SpellRound.RedTeam, 3);
            Assert.That(regain.Accepted, Is.True);
            Assert.That(regain.Regain.Regained, Is.EqualTo(1), "clamped to what was spent");
            Assert.That(lives.GetCounters(SpellRound.RedTeam), Is.EqualTo(4));
            Assert.That(lives.TryRegainSpent(SpellRound.RedTeam, 1).RejectionReason,
                Is.EqualTo(TeamLifeRegainRejectionReason.NoSpentCounter));
        }

        /// <summary>GY-Q11: "whether or not the enemy hero discards" - the two sentences are separate steps.</summary>
        [Test]
        public void EnergyDrain_RegainsTheLifeWhetherOrNotAnEnemyDiscards_GyQ11()
        {
            var noMatchingCard = new SpellRound(SpellRound.CastingCard(GydionCardContent.EnergyDrain),
                enemyAt: new HexCoordinate(2, 0), enemyHand: new[] { SpellRound.EnemyGoldCard }, redLivesSpent: 1);
            noMatchingCard.StartCasting();
            Assert.That(noMatchingCard.Choice, Is.Null,
                "he holds no non-basic card, so the first sentence can do nothing and nobody is asked");
            Assert.That(noMatchingCard.Card(SpellRound.EnemyHandCard(0)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(noMatchingCard.RedLives, Is.EqualTo(4), "and the Life is regained anyway");

            var outOfRange = new SpellRound(SpellRound.CastingCard(GydionCardContent.EnergyDrain),
                enemyAt: new HexCoordinate(5, 0), enemyHand: new[] { SpellRound.EnemyTierICard }, redLivesSpent: 1);
            outOfRange.StartCasting();
            Assert.That(outOfRange.Choice, Is.Null, "no enemy hero in range 3 is asked about");
            Assert.That(outOfRange.RedLives, Is.EqualTo(4), "and the Life is regained anyway");
            Assert.That(outOfRange.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>
        /// The regain needs what each team BEGAN with, which v51 did not persist: a restored match could
        /// not tell a team that had spent a counter from a full one. Snapshot v52 carries it.
        /// </summary>
        [Test]
        public void ASpentLife_SurvivesARestore_SoTheRestoredMatchRegainsIt_SnapshotV52()
        {
            var live = new SpellRound(SpellRound.CastingCard(GydionCardContent.EnergyDrain),
                enemyAt: new HexCoordinate(5, 0), redLivesSpent: 1);
            var bytes = live.Snapshot();
            var restored = live.Restore(bytes);
            Assert.That(restored.Snapshot(), Is.EqualTo(bytes), "byte-identical round trip");
            Assert.That(restored.Round.TeamLives.GetStartingCounters(SpellRound.RedTeam), Is.EqualTo(4),
                "the restored match knows what the team began with");
            Assert.That(restored.Round.TeamLives.GetCounters(SpellRound.RedTeam), Is.EqualTo(3));

            restored.StartCasting();
            live.StartCasting();
            Assert.That(restored.RedLives, Is.EqualTo(4), "so it regains the spent counter");
            Assert.That(restored.Snapshot(), Is.EqualTo(live.Snapshot()), "exactly as the live match does");
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(55));
        }

        // -------------------------------------------------------------------------------------------
        // Suggestion
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// GY-Q6 (user): Gydion picks the hero, THAT hero's seat picks the direction, exactly 3 spaces or
        /// not at all (Whisper Ruling 16a). The enemy at (2,0): west runs into Gydion at (0,0) and
        /// north-west into a rock at (2,-2), both short of three, so neither is offered.
        /// </summary>
        [Test]
        public void Suggestion_GydionPicksTheHero_AndThatHerosSeatPicksADirectionReachingExactlyThree_GyQ6()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.Suggestion),
                enemyAt: new HexCoordinate(2, 0), obstacles: new[] { new HexCoordinate(2, -2) });
            round.StartCasting();
            Assert.That(round.Choice.ChooserId, Is.EqualTo(Caster), "Gydion picks the hero");
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { Enemy }));
            round.PickHero(Enemy);

            Assert.That(round.Choice?.ChooserId, Is.EqualTo(Enemy), "that hero's seat picks the direction");
            var offered = round.OfferedSpaces();
            Assert.That(offered, Is.EquivalentTo(new[]
            {
                new HexCoordinate(5, 0), new HexCoordinate(5, -3), new HexCoordinate(-1, 3), new HexCoordinate(2, 3)
            }), "every direction that reaches exactly three; west (Gydion) and north-west (the rock) stop short");
            Assert.That(offered.All(space => HexDistanceQuery.Calculate(new HexCoordinate(2, 0), space) == 3), Is.True);

            round.PickSpace(new HexCoordinate(5, -3));
            Assert.That(round.Board.TryGetHeroCoordinate(Enemy, out var at) && at.Equals(new HexCoordinate(5, -3)), Is.True,
                "the enemy moved exactly three spaces in a straight line");
            Assert.That(round.Board.TryGetHeroCoordinate(Caster, out var casterAt) && casterAt.Equals(SpellRound.CasterAt), Is.True);
            Assert.That(round.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void Suggestion_ADirectionTheBoardsEdgeStopsShort_IsNotOffered()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.Suggestion),
                enemyAt: new HexCoordinate(3, 0));
            round.StartCasting();
            round.PickHero(Enemy);
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { new HexCoordinate(3, -3), new HexCoordinate(0, 3) }),
                "east, north-east and south-east leave the board within three; west runs into Gydion");
        }

        /// <summary>6.8l: the selection offers what the line can act on; "If able" then does nothing.</summary>
        [Test]
        public void Suggestion_AnEnemyNoDirectionCanCarryThree_IsNotOffered_AndNothingMoves()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.Suggestion),
                enemyAt: new HexCoordinate(3, 0),
                obstacles: new[] { new HexCoordinate(3, -3), new HexCoordinate(0, 3) });
            Assert.That(round.IsOffered, Is.True, "GY-Q12: the cast is playable while the spell is in the book");
            round.StartCasting();
            Assert.That(round.Choice, Is.Null, "an enemy in radius whom no straight line carries three is not offered");
            Assert.That(round.Board.TryGetHeroCoordinate(Enemy, out var at) && at.Equals(new HexCoordinate(3, 0)), Is.True);
            Assert.That(round.Spell(GydionCardContent.Suggestion).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(round.Card(SpellRound.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>
        /// GY-Q6: "a plain move, NOT a Movement action - the card does not say movement action". A standing
        /// "cannot move more than 1 space with a movement action" on the enemy (Arien's Slippery Ground)
        /// stops Whisper's forced Movement action (Ruling 16b) and does not touch this move.
        /// </summary>
        [Test]
        public void Suggestion_IsAPlainMove_SoAMovementActionLimitDoesNotStopIt_GyQ6()
        {
            var round = new SpellRound(SpellRound.CastingCard(GydionCardContent.Suggestion),
                enemyAt: new HexCoordinate(3, 0));
            round.Round.Timing.Create(
                new ActiveEffectId("slippery-ground-on-the-enemy"), SpellRound.MovementActionLimit, Enemy, TeamSide.Blue,
                default, SpellRound.EnemyPlayed, new ActionDefinitionId("movement"), 1, 1);
            round.StartCasting();
            round.PickHero(Enemy);
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { new HexCoordinate(3, -3), new HexCoordinate(0, 3) }),
                "a Movement-action limit does not reach a move the card does not call a movement action");
            round.PickSpace(new HexCoordinate(0, 3));
            Assert.That(round.Board.TryGetHeroCoordinate(Enemy, out var at) && at.Equals(new HexCoordinate(0, 3)), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // The fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One round on a disk: the caster (Red) owns Gydion's twenty-two spells and has committed a
        /// casting card; the enemy (Blue) has committed a Hold-only card or an Attack, holds the cards a
        /// discard test names, and optionally owns 9.30c's synthetic spells and casts one.
        /// </summary>
        internal sealed class SpellRound
        {
            internal static readonly HeroId Caster = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly TeamId RedTeam = new TeamId(StagedBoardParticipants.RedTeamIdValue);
            internal static readonly TeamId BlueTeam = new TeamId(StagedBoardParticipants.BlueTeamIdValue);
            internal static readonly CardInstanceId Played = new CardInstanceId("ward-played");
            internal static readonly CardInstanceId NextCard = new CardInstanceId("ward-next");
            internal static readonly CardInstanceId EnemyPlayed = new CardInstanceId("ward-enemy-played");
            internal static readonly CardInstanceId EnemyNext = new CardInstanceId("ward-enemy-next");
            internal static readonly HexCoordinate CasterAt = new HexCoordinate(0, 0);
            internal static readonly ActionDefinitionId CastingPrimary = new ActionDefinitionId("primary-skill");
            private static readonly ActionDefinitionId StrikePrimary = new ActionDefinitionId("primary-attack");

            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();
            private readonly MatchContentCatalogs catalogs;
            private readonly int enemyHandCount;

            internal SpellRound(
                CardDefinition played,
                CardDefinition enemyPlayed = null,
                CardDefinition next = null,
                CardDefinition enemyNext = null,
                IEnumerable<CardDefinition> enemyHand = null,
                HexCoordinate? enemyAt = null,
                IEnumerable<HexCoordinate> obstacles = null,
                int redLivesSpent = 0,
                bool enemyOwnsSpells = false,
                int boardRadius = 5)
            {
                catalogs = CreateCatalogs();
                var hand = (enemyHand ?? Enumerable.Empty<CardDefinition>()).ToArray();
                enemyHandCount = hand.Length;
                enemyPlayed = enemyPlayed ?? EnemyHold;
                Definitions = GydionCardContent.SpellDefinitions
                    .Concat(SyntheticSpellbook.CastingSpellDefinitions)
                    .Concat(new[] { played, enemyPlayed, next, enemyNext }.Where(value => value != null))
                    .Concat(hand)
                    .GroupBy(definition => definition.Reference)
                    .Select(group => group.First())
                    .ToArray();

                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Played, played.Reference, Caster),
                    CardInstance.CreateInHand(EnemyPlayed, enemyPlayed.Reference, Enemy)
                };
                if (next != null) cards.Add(CardInstance.CreateInHand(NextCard, next.Reference, Caster));
                if (enemyNext != null) cards.Add(CardInstance.CreateInHand(EnemyNext, enemyNext.Reference, Enemy));
                cards.AddRange(hand.Select((definition, index) =>
                    CardInstance.CreateInHand(EnemyHandCard(index), definition.Reference, Enemy)));
                cards.AddRange(GydionCardContent.SpellDefinitions.Select(spell => CardInstance.CreateInSpellbook(
                    new CardInstanceId("ward-book-" + spell.Id.Value), spell.Reference, Caster)));
                if (enemyOwnsSpells)
                {
                    cards.AddRange(SyntheticSpellbook.CreateCastingSpellbookInstances(Enemy, "ward-enemy"));
                }

                Cards = cards.ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("gydion-ward"), 1, Disk(boardRadius)),
                    new[]
                    {
                        new HeroOccupancy(Caster, CasterAt),
                        new HeroOccupancy(Enemy, enemyAt ?? new HexCoordinate(1, 0))
                    },
                    (obstacles ?? Enumerable.Empty<HexCoordinate>())
                        .Select((at, index) => new ObstacleOccupancy(new ObstacleId($"ward-rock-{index:00}"), at))
                        .ToArray());

                var participants = StagedBoardParticipants.Create();
                var lives = new TeamLifeState(participants, 4);
                if (redLivesSpent > 0)
                {
                    Assert.That(lives.TrySpend(RedTeam, redLivesSpent).Accepted, Is.True, "staged by p.13's own spend");
                }

                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, lives, null, null));
                Accept(Round.TryCommitCard(Caster, Played));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            private SpellRound(SpellRound template, string snapshot)
            {
                catalogs = template.catalogs;
                enemyHandCount = template.enemyHandCount;
                Definitions = template.Definitions;
                var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot, Definitions,
                    new[] { template.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
            }

            internal static MatchContentCatalogs CreateCatalogs()
            {
                // Since Gydion's dealing (9.30i) the build registers his spells' contracts; the fixture
                // adds only the synthetic Attack spell's naming step (9.30c) its enemy casts.
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(new[]
                        {
                            SyntheticSpellbookCapabilities.SpellInBookHandler(
                                SyntheticSpellbook.AttackSpellInBookV1, SyntheticSpellbook.AttackSpellId)
                        })),
                    defaults.Components,
                    defaults.TriggeredEffects);
            }

            internal static IReadOnlyList<HexCoordinate> Disk(int radius) =>
                (from q in Enumerable.Range(-radius, 2 * radius + 1)
                 from r in Enumerable.Range(-radius, 2 * radius + 1)
                 where Math.Abs(q + r) <= radius
                 select new HexCoordinate(q, r)).ToArray();

            internal static CardInstanceId EnemyHandCard(int index) => new CardInstanceId($"ward-enemy-hand-{index}");

            /// <summary>"Cast the X spell in the spellbook" - 9.30c's single cast bullet as the root.</summary>
            internal static CardDefinition CastingCard(CardDefinitionId spell)
            {
                var arm = new EffectNodeId("cast-" + spell.Value);
                return CastingCardOf(new CardDefinitionId("ward-cast-" + spell.Value), "TEST - Cast " + spell.Value, 9,
                    CardProgressionProfile.BasicCard(CardColor.Silver), arm,
                    SyntheticSpellbookCapabilities.CastArm(arm, GydionCardContent.InBookV1(spell)));
            }

            /// <summary>"Add all spell cards to the spellbook" - Prepare Spells' refill (9.30c).</summary>
            internal static CardDefinition PrepareSpellsCard { get; } = CastingCardOf(
                new CardDefinitionId("ward-prepare-spells"), "TEST - Prepare Spells", 9,
                CardProgressionProfile.BasicCard(CardColor.Silver), new EffectNodeId("add-all-spell-cards"),
                new[]
                {
                    new EffectNodeDefinition(new EffectNodeId("add-all-spell-cards"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticSpellbookCapabilities.RefillSpellbookV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticSpellbookCapabilities.ReturnedOutput, EffectValueKind.Integer)
                        })
                });

            /// <summary>
            /// A Red Tier I (non-basic) card that casts 9.30c's Gold (basic) Attack spell - so a reading of
            /// the played card and a reading of the spell disagree about the Attack's classification.
            /// </summary>
            internal static CardDefinition EnemyNonBasicCastsTheBasicAttackSpell { get; } = CastingCardOf(
                new CardDefinitionId("ward-enemy-red-cast"), "TEST - Red card casting the Attack spell", 1,
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.I, PrintedValueChannel.None),
                SyntheticSpellbook.CastAttackArm,
                SyntheticSpellbookCapabilities.CastArm(SyntheticSpellbook.CastAttackArm, SyntheticSpellbook.AttackSpellInBookV1));

            private static CardDefinition CastingCardOf(
                CardDefinitionId id, string name, int initiative, CardProgressionProfile profile,
                EffectNodeId root, IEnumerable<EffectNodeDefinition> nodes) =>
                new CardDefinition(
                    id, "rev-1", name, new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                    profile,
                    new[]
                    {
                        new ActionDefinition(CastingPrimary, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            Array.Empty<PrintedValueDefinition>(),
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default,
                            new EffectDefinition(new EffectDefinitionId(id.Value + "-skill"), "rev-1", root, nodes))
                    });

            internal static CardDefinition HoldOnlyCard { get; } = MovementOnly("ward-caster-hold", "TEST - Caster Hold", 9);

            internal static CardDefinition EnemyHold { get; } = MovementOnly("ward-enemy-hold", "TEST - Enemy Hold", 1);

            /// <summary>A Gold (basic) card printing a melee Attack.</summary>
            internal static CardDefinition EnemyBasicStrike { get; } =
                Strike("ward-enemy-gold-strike", "TEST - Gold Strike", CardProgressionProfile.BasicCard(CardColor.Gold));

            /// <summary>A Red Tier I (non-basic) card printing a melee Attack.</summary>
            internal static CardDefinition EnemyNonBasicStrike { get; } =
                Strike("ward-enemy-red-strike", "TEST - Red Strike",
                    CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.I, PrintedValueChannel.None));

            internal static CardDefinition EnemyGoldCard { get; } =
                HandCard("ward-enemy-gold", "TEST - Gold", CardProgressionProfile.BasicCard(CardColor.Gold));

            internal static CardDefinition EnemySilverCard { get; } =
                HandCard("ward-enemy-silver", "TEST - Silver", CardProgressionProfile.BasicCard(CardColor.Silver));

            internal static CardDefinition EnemyTierICard { get; } =
                HandCard("ward-enemy-tier-one", "TEST - Tier I",
                    CardProgressionProfile.NonBasicCard(CardColor.Blue, CardTier.I, PrintedValueChannel.None));

            /// <summary>Arien's Slippery Ground's rule: "cannot ... move more than 1 space with a movement action".</summary>
            internal static ActiveEffectDefinition MovementActionLimit { get; } = new ActiveEffectDefinition(
                "ward-movement-action-limit",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated },
                false,
                "WardMovementActionLimit",
                "ward-movement-action-limit",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForActivityLimit(RuleActivity.Movement, 1) });

            private static CardDefinition MovementOnly(string id, string name, int initiative) =>
                new CardDefinition(
                    new CardDefinitionId(id), "rev-1", name, new InitiativeValue(initiative),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                    new[]
                    {
                        new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                            ActionDefinitionOrigin.PrintedSecondary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                    });

            private static CardDefinition Strike(string id, string name, CardProgressionProfile profile) =>
                new CardDefinition(
                    new CardDefinitionId(id), "rev-1", name, new InitiativeValue(1),
                    CardContentAuthority.SyntheticTestFixture, profile,
                    new[]
                    {
                        new ActionDefinition(StrikePrimary, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 20) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.NonRanged, default)
                    });

            private static CardDefinition HandCard(string id, string name, CardProgressionProfile profile) =>
                new CardDefinition(
                    new CardDefinitionId(id), "rev-1", name, new InitiativeValue(2),
                    CardContentAuthority.SyntheticTestFixture, profile,
                    new[]
                    {
                        new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                            ActionDefinitionOrigin.Reaction,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                            AttackRangeKind.None,
                            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                    });

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;
            internal int RedLives => Round.TeamLives.GetCounters(RedTeam);

            internal SpellRound Restore(string snapshot) => new SpellRound(this, snapshot);

            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal CardInstance Spell(CardDefinitionId spell) =>
                Cards.Single(card => card.OwnerId.Equals(Caster) && card.DefinitionReference.DefinitionId.Equals(spell));

            internal CardInstance EnemySpell(CardDefinitionId spell) =>
                Cards.Single(card => card.OwnerId.Equals(Enemy) && card.DefinitionReference.DefinitionId.Equals(spell));

            internal bool IsOffered => IsOfferedOn(Played);

            internal bool IsOfferedOn(CardInstanceId card) => CastingOfferOn(card).IsAvailable;

            internal void StartCasting() => StartCastingOff(Played);

            internal void StartCastingOff(CardInstanceId card) =>
                Accept(Round.TryStartSkill(new ActionResolutionId("cast-" + card.Value), CastingOfferOn(card)));

            internal void EnemyCasts() => StartCastingOff(EnemyPlayed);

            /// <summary>The caster's turn ends; the enemy acts next.</summary>
            internal void ToTheEnemysTurn()
            {
                ReleaseIfAsked();
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
            }

            /// <summary>The enemy Holds, and the round's next turn is committed.</summary>
            internal void NextTurn()
            {
                if (Turn.SelectedActor.ActorId.Equals(Enemy))
                {
                    Accept(Round.TryPerformHold(Turn.SelectedActor.CardInstanceId));
                }

                ReleaseIfAsked();
                Accept(Round.TryCommitCard(Caster, NextCard));
                Accept(Round.TryCommitCard(Enemy, EnemyNext));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Caster));
            }

            internal bool EnemyStrikeIsOffered => EnemyStrike.IsAvailable;

            /// <summary>Starts the enemy's Attack and reads who it may target.</summary>
            internal IReadOnlyList<HeroId> EnemyStrikeTargets()
            {
                Accept(Round.TryStartAttack(new ActionResolutionId("enemy-strike"), EnemyStrike));
                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "the Attack asks for its target");
                return target.LegalTargetHeroIds.ToArray();
            }

            internal HeroId[] OfferedHeroes()
            {
                Assert.That(Choice, Is.Not.Null, "a hero is asked");
                return Choice.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId).ToArray();
            }

            internal CardInstanceId[] OfferedCards()
            {
                Assert.That(Choice, Is.Not.Null, "a card is asked");
                return Choice.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => candidate.Value.CardInstanceId).ToArray();
            }

            internal HexCoordinate[] OfferedSpaces()
            {
                Assert.That(Choice, Is.Not.Null, "a space is asked");
                return Choice.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();
            }

            internal void PickHero(HeroId hero) => Pick(value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(hero));

            internal void PickCard(CardInstanceId card) =>
                Pick(value => value.Kind == EffectValueKind.CardInstance && value.CardInstanceId.Equals(card));

            internal void PickSpace(HexCoordinate space) =>
                Pick(value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space));

            private void Pick(Func<EffectValue, bool> matches)
            {
                Assert.That(Choice, Is.Not.Null, "a choice stands");
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    matches(invocation.Candidate.Value))));
            }

            /// <summary>The Attack on the card the enemy committed this turn.</summary>
            private ActionOffer EnemyStrike =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(Turn.SelectedActor.CardInstanceId) &&
                    offer.ActionDefinitionId.Equals(StrikePrimary));

            private ActionOffer CastingOfferOn(CardInstanceId card) =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.SourceCardInstanceId.Equals(card) && offer.ActionDefinitionId.Equals(CastingPrimary));

            private void ReleaseIfAsked()
            {
                if (Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }
        }
    }
}
