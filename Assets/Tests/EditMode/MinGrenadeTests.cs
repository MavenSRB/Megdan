using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.61f. Min the Dragonmonk's two Grenade faces, driven.
    ///
    /// <para><b>The printed text is the whole specification.</b> Death Grenade: "Place a [Grenade]
    /// Grenade token into a space in radius. <b>End of turn:</b> Up to 1 enemy hero adjacent to that
    /// token discards a card, or is defeated. Remove the Grenade token."; Holy Death Grenade says the
    /// same with "Up to 2 enemy heroes ... discard a card, or are defeated". Every case below fails for
    /// a reason one of those clauses names.</para>
    ///
    /// <para><b>Why the worlds are composed by hand.</b> Nothing of Min's is registered until 8.61j, so
    /// a <see cref="MatchConfiguration"/> naming her cards would be refused by its own constructor.
    /// Both probes therefore compose a BOUNDED catalog - the build's registered set plus this block's
    /// deferred handlers, plus a component catalog carrying her Grenade - which is exactly what
    /// <c>SilverarrowEndOfTurnDriveTests</c> documents the shape as being for, and keeps the subject the
    /// card rather than the registration list.</para>
    ///
    /// <para><b>What could NOT be driven here, stated rather than left to be noticed.</b> The seat
    /// rewind lives on <c>MatchHost</c>, which needs a configured match, which needs registration. So
    /// <see cref="TheEndOfTurnQuestionIsAnOpponentsToAnswer_WhichIsWhatClosesTheSeatRewind"/> measures
    /// the two facts <c>MatchHost.CanSeatUndo</c> is computed FROM - the standing decision belongs to
    /// another seat, and the step changes what an opponent owns - rather than the refusal itself. The
    /// refusal is 8.61j's to drive, on the dealt card.</para>
    /// </summary>
    public sealed class MinGrenadeTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId OtherEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static HexCoordinate At(int q, int r = 0) => new HexCoordinate(q, r);

        // -----------------------------------------------------------------------------------------
        // The printed definitions.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The shard's row for <c>min10__7571085</c> and <c>min12__7571084</c>, transcribed: Initiative
        /// 11, a Skill primary at Radius 2, a Movement ribbon of 2, an Initiative Item icon, and a
        /// Defense ribbon that is the one number the two faces differ by.
        /// </summary>
        [TestCase(false, 4, CardTier.II)]
        [TestCase(true, 5, CardTier.III)]
        public void BothFaces_PrintTheShardsValues(bool holy, int defense, CardTier tier)
        {
            var definition = MinCardContent.GrenadeDefinition(
                holy ? MinCardContent.HolyDeathGrenade : MinCardContent.DeathGrenade);

            Assert.That(definition.Initiative.Value, Is.EqualTo(11));
            Assert.That(definition.Progression.Color, Is.EqualTo(CardColor.Blue));
            Assert.That(definition.Progression.Tier, Is.EqualTo(tier));
            Assert.That(definition.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Initiative));
            Assert.That(definition.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));

            var skill = definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(
                skill.BaseValues.Single(value => value.Channel == PrintedValueChannel.Radius).Amount,
                Is.EqualTo(2));
            Assert.That(skill.BaseValues.Any(value => value.Channel == PrintedValueChannel.Range),
                Is.False,
                "p.9 gives the face a Radius icon and no Range icon; an absent icon is content.");

            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Movement)
                    .BaseValues.Single().Amount,
                Is.EqualTo(2));
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Defense)
                    .BaseValues.Single().Amount,
                Is.EqualTo(defense));
        }

        /// <summary>
        /// The two texts, as printed. Holy Death Grenade is Death Grenade's sentence with "1 enemy
        /// hero ... discards" read as "2 enemy heroes ... discard", and nothing else changes.
        /// </summary>
        [Test]
        public void BothFaces_CarryTheirPrintedTextWhole()
        {
            Assert.That(
                MinCardContent.GrenadeDefinition(MinCardContent.DeathGrenade).SourceContent.PrintedText,
                Is.EqualTo(MinCardContent.DeathGrenadeText));
            Assert.That(
                MinCardContent.GrenadeDefinition(MinCardContent.HolyDeathGrenade)
                    .SourceContent.PrintedText,
                Is.EqualTo(MinCardContent.HolyDeathGrenadeText));
            Assert.That(MinCardContent.DeathGrenadeText, Does.Contain("Up to 1 enemy hero"));
            Assert.That(MinCardContent.HolyDeathGrenadeText, Does.Contain("Up to 2 enemy heroes"));
        }

        /// <summary>Both bodies are structurally legal against the catalog that carries their contracts.</summary>
        [Test]
        public void EveryComposedBody_ValidatesAgainstTheBoundedCatalog()
        {
            var catalog = BoundedCapabilities();
            foreach (var cardId in MinCardContent.GrenadeCards())
            {
                foreach (var action in MinCardContent.GrenadeDefinition(cardId).ActionDefinitions
                    .Where(action => action.EffectRoot != null))
                {
                    Assert.That(
                        EffectDefinitionValidator.Validate(action.EffectRoot, catalog), Is.Empty,
                        cardId.Value + " / " + action.Id.Value);
                }
            }

            foreach (var triggered in MinEffectContent.GrenadeTriggeredEffects)
            {
                Assert.That(EffectDefinitionValidator.Validate(triggered, catalog), Is.Empty,
                    triggered.Id.Value);
            }
        }

        // -----------------------------------------------------------------------------------------
        // Sentence one: "Place a [Grenade] Grenade token into a space in radius."
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The offer is every empty playable space inside the card's own printed Radius, measured from
        /// Min - p.18's Place - and p.16's Radius item raises it, because p.9 says the text's "in
        /// radius" IS the icon's value.
        /// </summary>
        [TestCase(false, 2)]
        [TestCase(true, 3)]
        public void ThePlacementReachesThePrintedRadius_AndAnItemRaisesIt(bool withItem, int reach)
        {
            var probe = new SkillProbe(
                MinCardContent.DeathGrenade,
                items: withItem ? RadiusItem() : null);

            Assert.That(probe.Choices, Is.Not.Empty);
            Assert.That(
                probe.Choices.All(value =>
                    HexDistanceQuery.Calculate(At(0), value.Coordinate) <= reach),
                Is.True,
                "Nothing outside the printed Radius is offered.");
            Assert.That(
                probe.Choices.Any(value =>
                    HexDistanceQuery.Calculate(At(0), value.Coordinate) == reach),
                Is.True,
                "and the whole of it is: the disk reaches exactly the printed value.");
        }

        /// <summary>
        /// MI-Q1's consequence, driven. With one physical grenade a second placement cannot come from
        /// the supply, so the presence branch RELOCATES the standing piece - the same token instance,
        /// in a new space - which is the reading Widget's Dragon Bond settled for Pyro.
        /// </summary>
        [Test]
        public void WithOneStanding_TheSecondPlacementMovesThatSameTokenRatherThanAddingASecond()
        {
            var probe = new SkillProbe(MinCardContent.DeathGrenade, grenadeAt: At(1));
            var before = probe.Board.PlacedTokens.Single();

            probe.Pick(probe.Choices.Single(value => value.Coordinate.Equals(At(2))));

            Assert.That(probe.Board.PlacedTokens, Has.Count.EqualTo(1),
                "One grenade exists and one grenade stands.");
            Assert.That(probe.Board.PlacedTokens.Single().Id, Is.EqualTo(before.Id),
                "Identity is preserved: the piece moved, it was not replaced.");
            Assert.That(probe.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(2)));
        }

        /// <summary>
        /// p.10's impossible step, and what it takes with it. A card whose radius holds no empty space
        /// places nothing, and because the "End of turn:" keyword is armed AFTER the placement inside
        /// the same sequence, nothing is armed either - so the second sentence never runs with no
        /// referent for "that token".
        /// </summary>
        [Test]
        public void WithNoEmptySpaceInRadius_ThePlacementIsImpossibleAndNothingIsArmed()
        {
            var probe = new SkillProbe(MinCardContent.DeathGrenade, crowded: true);

            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.Board.PlacedTokens, Is.Empty);
            Assert.That(probe.Timing.Effects, Is.Empty,
                "\"End of turn:\" belongs to a grenade that was placed.");
        }

        /// <summary>
        /// The keyword is armed once the placement succeeded, carrying the card that placed it - which
        /// is what the triggered body reads its printed Radius and its own grenade through.
        /// </summary>
        [TestCase(false, "min-death-grenade-effect")]
        [TestCase(true, "min-holy-death-grenade-effect")]
        public void APlacedGrenadeArmsItsOwnEndOfTurnEffect(bool holy, string definitionId)
        {
            var probe = new SkillProbe(
                holy ? MinCardContent.HolyDeathGrenade : MinCardContent.DeathGrenade);
            probe.Pick(probe.Choices.First(value => value.Coordinate.Equals(At(1))));

            var effect = probe.Timing.Effects.Single();
            Assert.That(effect.Definition.DefinitionId, Is.EqualTo(definitionId));
            Assert.That(effect.Definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.EndOfTurn));
            Assert.That(effect.Definition.TriggerCheckpoints, Does.Contain(TimingCheckpoint.TurnEnded));
            Assert.That(effect.ControllerHeroId, Is.EqualTo(Min));
            Assert.That(probe.Board.PlacedTokens.Single().SourceCardInstanceId,
                Is.EqualTo(probe.SourceCardId),
                "The token records the card that placed it, which is what \"that token\" names.");
        }

        /// <summary>
        /// p.19 classes a token as an obstacle and p.18 admits no second obstacle in a space "ever, no
        /// exceptions" - so the standing grenade blocks its own space for as long as it stands, without
        /// its definition declaring a single property.
        /// </summary>
        [Test]
        public void TheStandingGrenadeIsAnObstacleForAsLongAsItStands()
        {
            var probe = new SkillProbe(MinCardContent.DeathGrenade);
            probe.Pick(probe.Choices.First(value => value.Coordinate.Equals(At(1))));

            Assert.That(
                probe.Board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(Min), At(1)).Accepted,
                Is.False,
                "Nothing enters a space a token occupies.");
            probe.Board.CommitValidatedTokenReclamation(probe.Board.PlacedTokens.Single().Id);
            Assert.That(
                probe.Board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(Min), At(1)).Accepted,
                Is.True,
                "and it is the token that blocked it: the same step is legal once it is gone.");
        }

        // -----------------------------------------------------------------------------------------
        // Sentence two: "Up to N enemy heroes adjacent to that token discard a card, or are defeated."
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Up to" makes the whole performance optional (p.10), and Min owns the decision because the
        /// text assigns it to nobody else. Declining costs nothing and the turn still closes.
        /// </summary>
        [Test]
        public void UpTo_IsMinsOwnOptionalStep_AndDecliningLeavesEveryHandAlone()
        {
            var drive = Drive(holy: false, enemiesBeside: 1);

            Assert.That(drive.Pauses, Is.Not.Empty);
            Assert.That(drive.Pauses[0].ChooserId, Is.EqualTo(Min),
                "p.10 gives an unassigned decision to the card's controller.");
            Assert.That(drive.Pauses[0].NodeId.Value, Is.EqualTo("grenade-first-may"));

            var declined = Drive(holy: false, enemiesBeside: 1, declineEveryStep: true);
            Assert.That(declined.EnemyHandAfter, Is.EqualTo(declined.EnemyHandBefore));
            Assert.That(declined.TurnEnded, Is.True);
            Assert.That(declined.GrenadesOnBoard, Is.EqualTo(0),
                "\"Remove the Grenade token\" is the third sentence and runs either way.");
        }

        /// <summary>
        /// With nobody beside the token the optional step has no legal subject, so it is not offered at
        /// all - 8.35z15's rule - and the sentence's third clause still removes the grenade.
        /// </summary>
        [Test]
        public void WithNoEnemyHeroBesideTheToken_NothingIsAskedAndTheGrenadeIsStillRemoved()
        {
            var drive = Drive(holy: false, enemiesBeside: 0);

            Assert.That(drive.Pauses, Is.Empty,
                "An optional step with no legal subject is never offered.");
            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore));
            Assert.That(drive.GrenadesOnBoard, Is.EqualTo(0));
            Assert.That(drive.TurnEnded, Is.True);
        }

        /// <summary>
        /// Min names the hero and that hero names the card - the two choosers differing is the whole of
        /// what "discards a card" means, and it is what
        /// <see cref="SyntheticCardStateCapabilities.DiscardChosenCardV2"/> records on the pause so the
        /// resolver refuses an answer submitted for anybody else.
        /// </summary>
        [Test]
        public void MinNamesTheHero_AndThatHeroNamesTheCardOnTheirOwnSeat()
        {
            var drive = Drive(holy: false, enemiesBeside: 1);

            var discard = drive.Pauses.Single(pause => pause.NodeId.Value.EndsWith("-discard"));
            Assert.That(discard.ChooserId, Is.EqualTo(Enemy),
                "It is their hand, so it is their choice.");
            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore - 1));
            Assert.That(drive.GrenadesOnBoard, Is.EqualTo(0));
        }

        /// <summary>
        /// "adjacent to that token" is measured from the TOKEN, not from Min - so a hero she stands
        /// beside but the grenade does not is no candidate, and a hero beside the grenade is one
        /// however far from her they are.
        /// </summary>
        [Test]
        public void TheDiskIsDrawnAroundTheTokenRatherThanAroundMin()
        {
            var beside = Drive(holy: false, enemiesBeside: 1);
            Assert.That(beside.FirstOffer, Is.EqualTo(new[] { Enemy }));

            var apart = Drive(holy: false, enemiesBeside: 1, enemyOffset: 2);
            Assert.That(apart.Pauses, Is.Empty,
                "Two spaces from the token is not adjacent to it, whoever else is nearby.");
        }

        /// <summary>
        /// "enemy heroes". Min stands beside her own grenade and is never among them, which is
        /// <see cref="SourceTeamRelation.OpposingSource"/> read off the printed word.
        /// </summary>
        [Test]
        public void AHeroOnMinsOwnSideIsNeverOffered()
        {
            var drive = Drive(holy: false, enemiesBeside: 1, minBesideTheToken: true);

            Assert.That(drive.FirstOffer, Has.None.EqualTo(Min));
            Assert.That(drive.FirstOffer, Is.EqualTo(new[] { Enemy }));
        }

        /// <summary>
        /// Holy Death Grenade's "Up to <b>2</b>": two performances, asked one at a time, and the second
        /// cannot name the hero the first named. That exclusion is the only thing "2 enemy heroes"
        /// means beyond "1", and without it one hero would discard twice.
        /// </summary>
        [Test]
        public void HolysSecondPassOffersEverybodyButTheHeroTheFirstPassNamed()
        {
            var drive = Drive(holy: true, enemiesBeside: 2);

            Assert.That(drive.FirstOffer, Is.EquivalentTo(new[] { Enemy, OtherEnemy }),
                "Both enemy heroes stand beside the token, so both are candidates for the first.");
            Assert.That(drive.FirstChoice.IsDefined, Is.True);
            Assert.That(drive.SecondOffer, Has.None.EqualTo(drive.FirstChoice),
                "The first pass took " + drive.FirstChoice.Value + ", so the second may not.");
            Assert.That(drive.SecondOffer,
                Is.EqualTo(new[] { Enemy, OtherEnemy }
                    .Where(hero => !hero.Equals(drive.FirstChoice)).ToArray()),
                "and the rest of the disk is still offered.");
            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore - 1));
            Assert.That(drive.OtherEnemyHandAfter, Is.EqualTo(drive.OtherEnemyHandBefore - 1),
                "Two heroes, one card each.");
        }

        /// <summary>
        /// The two passes NEST, and this block found the reason rather than chose it:
        /// <c>EffectDefinitionValidator</c> refuses a binding whose producer is not guaranteed to have
        /// run, so a second pass that must exclude the first pass's hero cannot be that pass's
        /// declinable SIBLING. Declining the first therefore ends the sentence at one question.
        ///
        /// <para>Nothing reachable is lost, which is why the shape is right rather than merely legal:
        /// taking the second after declining the first would be one hero out of the same candidates -
        /// exactly what taking the first and declining the second already is.</para>
        /// </summary>
        [Test]
        public void DecliningHolysFirstPassEndsTheSentenceRatherThanAskingTheSameQuestionAgain()
        {
            var declined = Drive(holy: true, enemiesBeside: 2, declineEveryStep: true);

            Assert.That(declined.Pauses.Count(pause => pause.NodeId.Value.EndsWith("-may")),
                Is.EqualTo(1),
                "One decline answers the whole of \"up to 2\".");
            Assert.That(declined.SecondOffer, Is.Empty);
            Assert.That(declined.EnemyHandAfter, Is.EqualTo(declined.EnemyHandBefore));
            Assert.That(declined.OtherEnemyHandAfter, Is.EqualTo(declined.OtherEnemyHandBefore));
            Assert.That(declined.GrenadesOnBoard, Is.EqualTo(0));

            var validation = EffectDefinitionValidator.Validate(
                MinEffectContent.HolyDeathGrenadeTriggeredEffect.Definition, BoundedCapabilities());
            Assert.That(validation, Is.Empty,
                "and the nested shape is what makes the exclusion bindable at all.");
        }

        /// <summary>
        /// Ledger question 13's escalation. "discards a card, or is defeated" is one mandatory step with
        /// two ways to complete: only a hero who CANNOT discard is defeated, and that defeat is p.13's
        /// own - the coins go to Min as the defeating hero and the 8.53 scoreboard records her Kill.
        /// </summary>
        [Test]
        public void AHeroWithNoCardToGiveIsDefeated_WithP13sRewardsAndTheScoreboardsKill()
        {
            var drive = Drive(holy: false, enemiesBeside: 1, enemyHasCards: false);

            Assert.That(drive.Pauses.Select(pause => pause.NodeId.Value),
                Does.Contain("grenade-first-target"),
                "An empty-handed hero is the candidate the second half of the sentence exists for, " +
                "so the selection must still offer them.");
            Assert.That(drive.EnemyOnBoardAfter, Is.False, "p.13's Defeat removes the miniature.");
            Assert.That(drive.MinCoinsAfter, Is.GreaterThan(drive.MinCoinsBefore),
                "The defeating hero collects p.13's coins.");
            Assert.That(drive.MinKillsAfter, Is.EqualTo(drive.MinKillsBefore + 1));
            Assert.That(drive.GrenadesOnBoard, Is.EqualTo(0),
                "and the third sentence still removes the token.");
        }

        /// <summary>
        /// The other half of that pair, and the reason the escalation is not a choice: a hero who CAN
        /// discard does, and stands.
        /// </summary>
        [Test]
        public void AHeroWithACardToGiveDiscardsItAndIsNotDefeated()
        {
            var drive = Drive(holy: false, enemiesBeside: 1);

            Assert.That(drive.EnemyOnBoardAfter, Is.True);
            Assert.That(drive.MinKillsAfter, Is.EqualTo(drive.MinKillsBefore));
            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore - 1));
        }

        /// <summary>
        /// MI-Q4's last clause. "That token" is the grenade this card placed, and a grenade that is gone
        /// before the turn ends leaves the sentence with no subject: p.10 stops the mandatory step,
        /// nothing is asked of anybody, "Remove the Grenade token" is never reached, and NOTHING FAILS.
        /// </summary>
        [Test]
        public void AGrenadeRemovedBeforeTheTurnEndsDoesNothingAtAll()
        {
            var drive = Drive(holy: false, enemiesBeside: 1, clearTheGrenade: true);

            Assert.That(drive.Pauses, Is.Empty);
            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore));
            Assert.That(drive.GrenadesOnBoard, Is.EqualTo(0));
            Assert.That(drive.TurnEnded, Is.True,
                "An end-of-turn face with no subject must never hang the close.");
        }

        /// <summary>
        /// The order inside the sentence, measured on the board rather than argued: the grenade is
        /// still standing while its own question is being answered, and gone once the sentence is
        /// over. A removal before the selection would leave the anchor with nothing to measure from.
        /// </summary>
        [Test]
        public void TheGrenadeIsRemovedAfterTheSentenceItAnchors_NotBefore()
        {
            var drive = Drive(holy: false, enemiesBeside: 1, stopAtTheDiscard: true);

            Assert.That(drive.GrenadesOnBoard, Is.EqualTo(1),
                "The token the sentence is about is still on the board while it is being answered.");

            drive.RunToTheEnd(drive.Round, drive.Board);

            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore - 1));
            Assert.That(drive.GrenadesOnBoard, Is.EqualTo(0),
                "and \"Remove the Grenade token\" is the sentence after it.");
        }

        /// <summary>
        /// p.19's fallback for the turn that never reaches its own end. The grenade declares the page's
        /// baseline lifetime, so the round's cleanup takes a grenade the card's own third sentence never
        /// got to - which is the only thing that keeps a cancelled effect from leaving a piece standing
        /// forever.
        /// </summary>
        [Test]
        public void ACancelledEffectLeavesAGrenadeThatEndOfRoundCleanupStillTakes()
        {
            Assert.That(
                MinComponentContent.GrenadeDefinition.RemovalCondition,
                Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup));

            var probe = new SkillProbe(MinCardContent.DeathGrenade);
            probe.Pick(probe.Choices.First(value => value.Coordinate.Equals(At(1))));

            var cleanup = ComponentCleanupRules
                .TokensRemovedAtEndOfRound(probe.Board.PlacedTokens)
                .ToArray();
            Assert.That(cleanup.Select(token => token.Definition.Reference),
                Is.EqualTo(new[] { MinComponentContent.GrenadeDefinition.Reference }));
        }

        /// <summary>
        /// Ordering beside another hero's "End of turn:" face. Nothing about this card decides it: the
        /// end-of-turn pipeline orders every trigger by the revealed side first, then by when each
        /// effect was created, then by its identity - and this Grenade's effect sorts ahead of the
        /// Silverarrow effect standing beside it, so it is the one the close reaches first and it runs
        /// to completion exactly as it does alone.
        ///
        /// <para><b>The second half is a measured limitation of the pipeline, not of this card, and it
        /// is pinned here rather than left to be rediscovered.</b>
        /// <c>TriggeredEffectResolutionAuthority.ApplyCheckpoint</c> breaks out of its candidate loop
        /// the moment one trigger pauses, and nothing resumes the rest when that pause is answered - so
        /// the SECOND "End of turn:" face at one checkpoint never runs at all. Silverarrow's Warning
        /// Shot is standing here and takes nothing. It is reported rather than corrected: the fix is a
        /// continuation for every trigger family at every checkpoint, which is wider than this block's
        /// two faces, and no face of Min's introduces it.</para>
        /// </summary>
        [Test]
        public void ItRunsBesideAnotherEndOfTurnFaceInThePipelinesOwnOrder()
        {
            var alone = Drive(holy: false, enemiesBeside: 1, minBesideTheToken: true, spareCards: 3);
            var beside = Drive(
                holy: false, enemiesBeside: 1, minBesideTheToken: true, spareCards: 3,
                besideWarningShot: true);

            Assert.That(beside.Pauses.Count(pause => pause.NodeId.Value.StartsWith("grenade-")),
                Is.EqualTo(alone.Pauses.Count),
                "The Grenade asks exactly what it asks alone, beside another end-of-turn face.");
            Assert.That(beside.GrenadesOnBoard, Is.EqualTo(0),
                "and finishes its own third sentence.");
            Assert.That(beside.TurnEnded, Is.True);
            Assert.That(beside.EffectPhases, Is.All.EqualTo(ActiveEffectPhase.Ended),
                "Both expire at the checkpoint they fire at, so neither can fire twice.");

            // The pinned pipeline gap - see this case's own remarks.
            Assert.That(beside.Pauses.Count(pause => pause.NodeId.Value.StartsWith("warning-")),
                Is.Zero,
                "MEASURED 8.61f: the second end-of-turn trigger at one checkpoint is dropped once " +
                "the first has paused. Reported, not this block's to correct.");
            Assert.That(beside.EnemyHandBefore - beside.EnemyHandAfter, Is.EqualTo(1),
                "so only the trigger the pipeline reached first took a card.");
        }

        /// <summary>
        /// The pause the other seat owns survives a snapshot: restored, it is the same question with the
        /// same chooser and the same candidates, and answering both branches leaves two identical
        /// authoritative payloads.
        /// </summary>
        [Test]
        public void TheDiscardPauseRestoresAndReplaysToTheSameAuthoritativeState()
        {
            var drive = Drive(holy: false, enemiesBeside: 1, stopAtTheDiscard: true);

            var standing = drive.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(standing, Is.Not.Null);
            Assert.That(standing.ChooserId, Is.EqualTo(Enemy));

            var payload = AuthoritativeSnapshotSerializer.Serialize(drive.Round, drive.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                drive.Catalogs, payload, drive.Definitions,
                new[] { drive.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());

            var restoredChoice = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(restoredChoice, Is.Not.Null);
            Assert.That(restoredChoice.ChooserId, Is.EqualTo(Enemy));
            Assert.That(restoredChoice.NodeId, Is.EqualTo(standing.NodeId));
            Assert.That(restoredChoice.Candidates.Count, Is.EqualTo(standing.Candidates.Count));

            drive.RunToTheEnd(drive.Round, drive.Board);
            drive.RunToTheEnd(restored.Round, restored.Board);

            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(drive.Round, drive.Board)));
        }

        /// <summary>
        /// The seat rewind, as far as this block can measure it - see the type's own remarks. Both facts
        /// <c>MatchHost.CanSeatUndo</c> reads are true here: the standing decision belongs to a seat
        /// that is not Min's, and the step changes what an opponent owns. 8.61j drives the refusal
        /// itself on the dealt card.
        /// </summary>
        [Test]
        public void TheEndOfTurnQuestionIsAnOpponentsToAnswer_WhichIsWhatClosesTheSeatRewind()
        {
            var drive = Drive(holy: false, enemiesBeside: 1, stopAtTheDiscard: true);

            var standing = drive.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(standing.ChooserId, Is.EqualTo(Enemy));
            Assert.That(standing.ChooserId, Is.Not.EqualTo(Min),
                "A rewind may not reach under a decision another seat is answering.");

            drive.RunToTheEnd(drive.Round, drive.Board);
            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore - 1),
                "and what the step did is a change to what an opponent owns, which is the " +
                "interaction the 8.42e wall is drawn at.");
        }

        // -----------------------------------------------------------------------------------------
        // Registration: nothing of this block's is in the build, from both sides.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate, asked from both directions. Every capability this block declares is in
        /// <see cref="MinComponentContent.GrenadeDeferredHandlers"/> and none of them is registered; the
        /// Grenade is not in the build's component catalog either. 8.61j moves both halves at once, and
        /// this case is what refuses a change that moves one and forgets the other.
        /// </summary>
        [Test]
        public void EveryGrenadeContractIsDeferred_AndNoneOfItIsRegistered()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            var deferred = MinComponentContent.GrenadeDeferredHandlers().ToArray();

            Assert.That(deferred, Is.Not.Empty);
            foreach (var handler in deferred)
            {
                Assert.That(build.Capabilities.Registered.Contains(handler.Reference), Is.False,
                    handler.Reference + " is registered before Min is dealt.");
            }

            Assert.That(
                SyntheticComponentDefinitions.CreateCatalog()
                    .IsRegistered(MinComponentContent.GrenadeDefinition.Reference),
                Is.False,
                "The Grenade joins the one registered token set at the dealing, not before.");

            var named = MinCardContent.GrenadeCards()
                .SelectMany(cardId => MinCardContent.GrenadeDefinition(cardId).ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Concat(MinEffectContent.GrenadeTriggeredEffects.SelectMany(effect => effect.Nodes))
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();
            var covered = deferred.Select(handler => handler.Reference)
                .Concat(build.Capabilities.Registered)
                .ToArray();
            Assert.That(named.Where(reference => !covered.Contains(reference)), Is.Empty,
                "Every contract the two faces name is either registered already or deferred here.");
        }

        /// <summary>
        /// The widened foundation this block took, registered as a claim: the anchored selection now
        /// carries an optional named-hero exclusion, and it carries it only where an instance says so.
        /// Widget's four registered instances are byte-identical to what 8.35z32 registered.
        /// </summary>
        [Test]
        public void TheAnchoredSelectionsExclusionIsCarriedOnlyByAnInstanceThatDeclaresIt()
        {
            var excluding = SyntheticAnchoredTargetCapabilities.Handler(
                MinEffectContent.GrenadeSecondBlastTargetV1, RosterUnitKind.Hero,
                SourceTeamRelation.OpposingSource, AnchorTargetShape.Adjacent,
                withinPrintedRange: false, excludesNamedHero: true);
            var plain = SyntheticAnchoredTargetCapabilities.Handler(
                MinEffectContent.GrenadeBlastTargetV1, RosterUnitKind.Hero,
                SourceTeamRelation.OpposingSource, AnchorTargetShape.Adjacent,
                withinPrintedRange: false);

            Assert.That(
                excluding.Schema.Parameters.Select(parameter => parameter.Name),
                Does.Contain(SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName));
            Assert.That(
                excluding.Schema.Parameters
                    .Single(parameter => parameter.Name ==
                        SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName)
                    .IsRequired,
                Is.False,
                "A declined first pass leaves it unbound, and unbound excludes nobody.");
            Assert.That(
                plain.Schema.Parameters.Select(parameter => parameter.Name),
                Does.Not.Contain(SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName));

            foreach (var widget in WidgetComponentContent.RegisteredHandlers()
                .Where(handler => handler.Schema.Parameters
                    .Any(parameter => parameter.Name == "anchor")))
            {
                Assert.That(
                    widget.Schema.Parameters.Select(parameter => parameter.Name),
                    Does.Not.Contain(SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName),
                    widget.Reference + " prints no such clause and must not declare one.");
            }

            Assert.That(
                () => SyntheticAnchoredTargetCapabilities.Handler(
                    MinEffectContent.GrenadeSecondBlastTargetV1, RosterUnitKind.Minion,
                    SourceTeamRelation.OpposingSource, AnchorTargetShape.Adjacent,
                    withinPrintedRange: false, excludesNamedHero: true),
                Throws.ArgumentException,
                "A minion selection has no hero to exclude.");
        }

        // -----------------------------------------------------------------------------------------
        // Sentence one's probe: the Skill body, resolved directly.
        // -----------------------------------------------------------------------------------------

        private static HeroItemState RadiusItem() => HeroItemState.Restore(
            StagedBoardParticipants.CreateWithBlueAlly(), new HeroItemRestoreState(new[]
            {
                new HeroItem(Min, new CardInstanceId("min-radius-item"),
                    HeroDashboard.RulebookLayout.Slots[3].Position, PrintedValueChannel.Radius, 1)
            }));

        private static EffectCapabilityCatalog BoundedCapabilities()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            return new EffectCapabilityCatalog(
                build.Capabilities.Registered.Select(build.Capabilities.Load)
                    .Concat(MinComponentContent.GrenadeDeferredHandlers()));
        }

        private static ComponentDefinitionCatalog BoundedComponents() =>
            new ComponentDefinitionCatalog(
                MinComponentContent.GrenadeTokens(), Array.Empty<MarkerDefinition>());

        private sealed class SkillProbe
        {
            internal readonly AdjacentMoveAuthority Board;
            internal readonly ActiveEffectLifecycle Timing = new ActiveEffectLifecycle();
            internal readonly CardInstanceId SourceCardId = new CardInstanceId("min-grenade-source");
            internal EffectResolutionStepResult Step;

            private readonly EffectResolutionContext context;

            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;

            internal EffectValue[] Choices =>
                Choice?.Candidates.Select(candidate => candidate.Value).ToArray()
                ?? Array.Empty<EffectValue>();

            internal SkillProbe(
                CardDefinitionId cardId,
                HexCoordinate? grenadeAt = null,
                bool crowded = false,
                HeroItemState items = null)
            {
                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                var definition = MinCardContent.GrenadeDefinition(cardId);
                var skill = definition.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Skill);
                var cards = new[]
                {
                    CardInstance.CreateInHand(SourceCardId, definition.Reference, Min)
                };

                // A three-space line when the radius must hold nothing free, and a wide field
                // otherwise. On the line Min stands at 0 and the two enemies fill 1 and 2, so every
                // space inside Radius 2 is occupied and p.10's step is impossible.
                var spaces = crowded
                    ? new[] { At(0), At(1), At(2) }
                    : Enumerable.Range(-4, 9)
                        .SelectMany(q => Enumerable.Range(-4, 9).Select(r => At(q, r)))
                        .ToArray();
                var map = BoardMapDefinition.CreateSynthetic(
                    new MapId("min-grenade-skill-map"), 1, spaces);
                Board = new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Min, At(0)),
                        new HeroOccupancy(Enemy, crowded ? At(1) : At(-4, 4)),
                        new HeroOccupancy(OtherEnemy, crowded ? At(2) : At(-3, 4))
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                if (grenadeAt.HasValue)
                {
                    Board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId("min-standing-grenade"),
                        MinComponentContent.GrenadeDefinition, Min, Min, SourceCardId,
                        grenadeAt.Value));
                }

                var capabilities = BoundedCapabilities();
                var components = BoundedComponents();
                Assert.That(
                    EffectDefinitionValidator.Validate(skill.EffectRoot, capabilities), Is.Empty);

                context = new EffectResolutionContext(
                    skill.EffectRoot, capabilities, participants, Timing, 1, 1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(new[] { definition }, cards), ActionKind.Skill,
                    components: new MatchComponents(
                        components, Board, new MarkerAttachmentRegister(), Board, Board, Board, Board),
                    heroItems: items);
                Step = EffectResolutionSession.Start(
                    new EffectResolutionId("min-grenade-skill"), SourceCardId, definition.Reference,
                    skill.Id, Min, Min, ActionInvocationContext.PrintedPrimary, context);
                Assert.That(Step.Accepted, Is.True);
            }

            internal void Pick(EffectValue value)
            {
                var id = Choice.ChoiceId;
                Answer(Choice.SelectInvocations.Single(
                    command => command.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }

            internal void Answer(EffectChoiceCommand command)
            {
                Step = EffectResolutionSession.Answer(Step.Frame, command, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            }
        }

        // -----------------------------------------------------------------------------------------
        // Sentences two and three: a whole turn's close, driven through a round.
        // -----------------------------------------------------------------------------------------

        private sealed class DriveResult
        {
            internal RoundResolutionAuthority Round;
            internal AdjacentMoveAuthority Board;
            internal MatchContentCatalogs Catalogs;
            internal CardDefinition[] Definitions;
            internal CardInstance[] Instances;
            internal readonly List<PendingEffectChoice> Pauses = new List<PendingEffectChoice>();
            internal ActiveEffectLifecycle Timing;
            internal HeroId[] FirstOffer = Array.Empty<HeroId>();
            internal HeroId[] SecondOffer = Array.Empty<HeroId>();
            internal HeroId FirstChoice;
            internal bool TurnEnded;
            internal int EnemyHandBefore;
            internal int EnemyHandAfter;
            internal int OtherEnemyHandBefore;
            internal int OtherEnemyHandAfter;
            internal bool EnemyOnBoardAfter;
            internal int MinCoinsBefore;
            internal int MinCoinsAfter;
            internal int MinKillsBefore;
            internal int MinKillsAfter;
            internal int GrenadesOnBoard;

            internal void RunToTheEnd(RoundResolutionAuthority round, AdjacentMoveAuthority board)
            {
                for (var step = 0; step < 60; step++)
                {
                    var turn = round.CurrentTurnAuthority;
                    if (turn == null || turn.IsEnded) break;
                    if (turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        Assert.That(round.TryChooseTiedActor(
                            tie.OwningSide, tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    var choice = turn.PendingEffectChoice;
                    if (choice != null)
                    {
                        var answered = round.TryAnswerEffectChoice(
                            choice.SelectInvocations.Count > 0
                                ? choice.SelectInvocations[0]
                                : choice.ConfirmInvocation);
                        Assert.That(answered.Accepted, Is.True);
                        Observe(round, answered);
                        continue;
                    }

                    if (turn.PendingTurnRelease != null)
                    {
                        var released = round.TryReleaseTurn();
                        Assert.That(released.Accepted, Is.True);
                        Observe(round, released);
                        continue;
                    }

                    if (!turn.SelectedActor.CardInstanceId.IsDefined) break;
                    var held = round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                    Assert.That(held.Accepted, Is.True);
                    Observe(round, held);
                }

                Refresh(round, board);
            }

            /// <summary>Every Active effect this drive created, as the lifecycle now holds it.</summary>
            internal IEnumerable<ActiveEffectPhase> EffectPhases =>
                Timing.Effects.Select(effect => effect.Phase);

            /// <summary>
            /// The 8.53 scoreboard's own observation of a command's rule events. It is fed here for
            /// the reason it is fed at all: <c>MatchAggregate</c> is what calls
            /// <c>Scoreboard.Observe</c> in a configured match, and this drive composes the round
            /// directly. What is measured is still the production event and the production reader.
            /// </summary>
            internal void Observe(RoundResolutionAuthority round, RoundResolutionResult outcome)
            {
                if (outcome.Events == null) return;
                foreach (var value in outcome.Events) round.Scoreboard.Observe(value);
            }

            internal void Refresh(RoundResolutionAuthority round, AdjacentMoveAuthority board)
            {
                EnemyHandAfter = HandCount(Instances, Enemy);
                OtherEnemyHandAfter = HandCount(Instances, OtherEnemy);
                EnemyOnBoardAfter = board.Occupancies.Any(hero => hero.HeroId.Equals(Enemy));
                MinCoinsAfter = round.CoinBalances
                    .Single(balance => balance.Key.Equals(Min)).Value;
                MinKillsAfter = round.Scoreboard.For(Min).Kills;
                GrenadesOnBoard = board.PlacedTokens.Count(token =>
                    token.Definition.Reference.Equals(
                        MinComponentContent.GrenadeDefinition.Reference));
                TurnEnded = round.CurrentTurnAuthority == null || round.CurrentTurnAuthority.IsEnded;
            }
        }

        private static int HandCount(IEnumerable<CardInstance> instances, HeroId hero) =>
            instances.Count(card =>
                card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);

        /// <summary>
        /// One turn with a Grenade effect standing and a grenade on the board at (1,0). Min stands at
        /// (0,0) and each enemy hero is placed beside the token or two spaces from it, as the case
        /// asks. The effect is created directly in the lifecycle rather than by playing the Skill,
        /// which is <c>SilverarrowEndOfTurnDriveTests</c>' shape and keeps the subject the turn's close.
        /// </summary>
        private static DriveResult Drive(
            bool holy,
            int enemiesBeside,
            bool declineEveryStep = false,
            bool enemyHasCards = true,
            bool clearTheGrenade = false,
            bool besideWarningShot = false,
            bool minBesideTheToken = false,
            bool stopAtTheDiscard = false,
            int enemyOffset = 1,
            int spareCards = 1)
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var cardId = holy ? MinCardContent.HolyDeathGrenade : MinCardContent.DeathGrenade;
            var definition = MinCardContent.GrenadeDefinition(cardId);
            var skill = definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var warningShot = SilverarrowCardContent.Definition(SilverarrowCardContent.WarningShot);
            var warningShotSkill = warningShot.ActionDefinitions.Single(
                action => action.Kind == ActionKind.Skill);

            var source = CardInstance.CreateInHand(
                new CardInstanceId("min-grenade-card"), definition.Reference, Min);
            var minSpare = CardInstance.CreateInHand(
                new CardInstanceId("min-spare"), definition.Reference, Min);
            var enemyCards = new List<CardInstance>
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("enemy-commit"), definition.Reference, Enemy)
            };
            if (enemyHasCards)
            {
                for (var spare = 0; spare < spareCards; spare++)
                {
                    enemyCards.Add(CardInstance.CreateInHand(
                        new CardInstanceId("enemy-spare-" + spare), definition.Reference, Enemy));
                }
            }

            var otherEnemyCards = new List<CardInstance>
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("other-enemy-commit"), definition.Reference, OtherEnemy),
                CardInstance.CreateInHand(
                    new CardInstanceId("other-enemy-spare"), definition.Reference, OtherEnemy)
            };

            var warningShotCard = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-warning-shot"), warningShot.Reference, Min);

            var instances = new[] { source, minSpare }
                .Concat(enemyCards).Concat(otherEnemyCards)
                .Concat(besideWarningShot ? new[] { warningShotCard } : Array.Empty<CardInstance>())
                .ToArray();
            var definitions = new[] { definition, warningShot };

            var spaces = Enumerable.Range(-4, 9)
                .SelectMany(q => Enumerable.Range(-4, 9).Select(r => At(q, r)))
                .ToArray();
            var map = BoardMapDefinition.CreateSynthetic(new MapId("min-grenade-turn-map"), 1, spaces);
            var grenadeAt = At(1);
            var occupancies = new List<HeroOccupancy>
            {
                new HeroOccupancy(Min, minBesideTheToken ? At(0) : At(-3, 0)),
                new HeroOccupancy(
                    Enemy, enemiesBeside >= 1 ? At(1 + enemyOffset) : At(-4, 4)),
                new HeroOccupancy(
                    OtherEnemy, enemiesBeside >= 2 ? At(1, -1) : At(-3, 4))
            };
            var board = new AdjacentMoveAuthority(
                map, occupancies, Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("min-drive-grenade"), MinComponentContent.GrenadeDefinition,
                Min, Min, source.Id, grenadeAt));
            if (clearTheGrenade)
            {
                board.CommitValidatedTokenReclamation(board.PlacedTokens.Single().Id);
            }

            var build = MatchContentCatalogs.ForThisBuild();
            var capabilities = new EffectCapabilityCatalog(
                build.Capabilities.Registered.Select(build.Capabilities.Load)
                    .Concat(MinComponentContent.GrenadeDeferredHandlers())
                    .Concat(SilverarrowActiveEffectContent.Handlers())
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
            var components = new ComponentDefinitionCatalog(
                MinComponentContent.GrenadeTokens(), Array.Empty<MarkerDefinition>());
            var catalogs = new MatchContentCatalogs(
                capabilities, components,
                new TriggeredEffectCatalog(
                    MinEffectContent.GrenadeTriggeredEffects.Concat(
                        SilverarrowEffectContent.TriggeredEffects(
                            SilverarrowCardContent.ContentRevision))));

            var markers = new MarkerAttachmentRegister();
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("min-grenade-end-of-turn"),
                holy
                    ? MinEffectContent.HolyDeathGrenadeEndOfTurnDefinition
                    : MinEffectContent.DeathGrenadeEndOfTurnDefinition,
                Min, TeamSide.Red, Min, source.Id, skill.Id, 1, 1);
            if (besideWarningShot)
            {
                // The identity sorts after the Grenade's, which is how the pipeline's third ordering
                // key is exercised deliberately rather than by accident of spelling.
                timing.Create(
                    new ActiveEffectId("silverarrow-drive-warning-shot"),
                    SilverarrowActiveEffectContent.WarningShotDefinition,
                    Min, TeamSide.Red, Min, warningShotCard.Id, warningShotSkill.Id, 1, 1);
            }

            var result = new DriveResult
            {
                Board = board,
                Catalogs = catalogs,
                Definitions = definitions,
                Instances = instances,
                Timing = timing
            };

            var round = new RoundResolutionAuthority(
                definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                RoundResolutionComposition.ForMatch(
                    new CardActionResolutionAuthority(catalogs, board, markers),
                    board, timing, markers, null, null,
                    startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));
            result.Round = round;
            result.MinCoinsBefore = round.CoinBalances
                .Single(balance => balance.Key.Equals(Min)).Value;
            result.MinKillsBefore = round.Scoreboard.For(Min).Kills;

            Assert.That(round.TryCommitCard(Min, source.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Enemy, enemyCards[0].Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(OtherEnemy, otherEnemyCards[0].Id).Accepted, Is.True);

            result.EnemyHandBefore = HandCount(instances, Enemy);
            result.OtherEnemyHandBefore = HandCount(instances, OtherEnemy);

            for (var step = 0; step < 60; step++)
            {
                var turn = round.CurrentTurnAuthority;
                if (turn == null || turn.IsEnded) break;

                // Every card here prints Initiative 11, so p.7's order is a tie and the side that
                // owns the choice names its own actor before the turn can start. Min's card is
                // preferred where it is a candidate, so the effect fires at the close of HER turn.
                if (turn.PendingChoice != null)
                {
                    var tie = turn.PendingChoice;
                    Assert.That(round.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(source.Id)
                                ? source.Id
                                : tie.CandidateCardInstanceIds[0]).Accepted,
                        Is.True);
                    continue;
                }

                var choice = turn.PendingEffectChoice;
                if (choice != null)
                {
                    result.Pauses.Add(choice);
                    if (stopAtTheDiscard && choice.NodeId.Value.EndsWith("-discard"))
                    {
                        RecordOffer(result, choice, null);
                        break;
                    }

                    var decline = declineEveryStep && choice.NodeId.Value.EndsWith("-may");
                    var command = decline || choice.SelectInvocations.Count == 0
                        ? choice.ConfirmInvocation
                        : choice.SelectInvocations[0];
                    if (decline && choice.DeclineInvocation != null)
                    {
                        command = choice.DeclineInvocation;
                    }

                    RecordOffer(result, choice, command);
                    var answered = round.TryAnswerEffectChoice(command);
                    Assert.That(answered.Accepted, Is.True,
                        "An end-of-turn trigger's question must be answerable when it is asked.");
                    result.Observe(round, answered);
                    continue;
                }

                if (turn.PendingTurnRelease != null)
                {
                    var released = round.TryReleaseTurn();
                    Assert.That(released.Accepted, Is.True);
                    result.Observe(round, released);
                    continue;
                }

                if (!turn.SelectedActor.CardInstanceId.IsDefined) break;
                var held = round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                Assert.That(held.Accepted, Is.True);
                result.Observe(round, held);
            }

            result.Refresh(round, board);
            return result;
        }

        private static void RecordOffer(
            DriveResult result, PendingEffectChoice choice, EffectChoiceCommand answer)
        {
            if (!choice.NodeId.Value.EndsWith("-target")) return;
            var heroes = choice.Candidates
                .Select(candidate => candidate.Value)
                .Where(value => value.Kind == EffectValueKind.Hero)
                .Select(value => value.HeroId)
                .ToArray();
            if (choice.NodeId.Value.Contains("-first-"))
            {
                result.FirstOffer = heroes;
                if (answer != null && answer.Candidate.Value.Kind == EffectValueKind.Hero)
                {
                    result.FirstChoice = answer.Candidate.Value.HeroId;
                }
            }
            else
            {
                result.SecondOffer = heroes;
            }
        }
    }
}
