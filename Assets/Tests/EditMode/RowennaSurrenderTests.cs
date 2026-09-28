using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.57c. Rowenna's two printed clauses that DEFEAT - Accept Surrender, "Defeat an enemy
    /// hero adjacent to you with no cards in hand", and Glorious Triumph, the same sentence with "your
    /// friendly heroes gain triple assist coins" after the semicolon.
    ///
    /// <para><b>Why these cases run through the DISPATCHER and not only at the effect frame.</b>
    /// <see cref="RowennaRewardTests"/> and <see cref="RowennaSwapAndGauntletTests"/> sit at the frame
    /// because the clauses they prove are decided there. This block's whole second half is decided
    /// somewhere else: rulebook p.13's list is the card ACTION's business, run by
    /// <c>AttackDefenseCardActionStrategyBackend.ResolveHeroDefeat</c> at the one spend site, and a
    /// fixture that stopped at the frame would prove that a Defeat was REQUESTED and nothing about what
    /// it costs. Both of her faces name only registered capabilities -
    /// <c>choose-target@2</c> and <c>synthetic-defeat-hero@1</c> - so unlike 8.57b's gauntlet these can
    /// be driven through <see cref="CardActionResolutionAuthority.StartTurnSkill"/> against this
    /// build's own catalog, with her definitions handed in as content. That is measurement rather than
    /// convenience: it is the same path a dealt card will take at 8.57g.</para>
    ///
    /// <para><b>What is still NOT proved here, and who owns it.</b> Nothing of Rowenna's is registered
    /// - milestone 7.9's gate holds her content out of <see cref="MatchContentCatalogs"/> until 8.57g
    /// deals her - so no configured match deals these cards, and the seat projection, the wire and the
    /// player path belong to 8.57f, 8.57g and the permanent gate at 8.57h.</para>
    ///
    /// <para><b>The board is six seats and says why.</b> Three Blue - Rowenna and TWO team-mates, so
    /// "your friendly heroes" is plural and the assist coins can be seen landing on more than one hero
    /// while never landing on her - and three Red, so one enemy can be adjacent with an empty hand, one
    /// adjacent holding a card, and one empty-handed but out of reach, all on one board without moving
    /// anybody between cases.</para>
    /// </summary>
    public sealed class RowennaSurrenderTests
    {
        private static readonly HeroId Rowenna = new HeroId("rowenna-vanguard");
        private static readonly HeroId Ally = new HeroId("rowenna-blue-ally");
        private static readonly HeroId SecondAlly = new HeroId("rowenna-blue-second-ally");

        /// <summary>Adjacent, and holding nothing: the one hero the printed sentence admits.</summary>
        private static readonly HeroId Victim = new HeroId("rowenna-red-victim");

        /// <summary>Adjacent, and holding one card: excluded by the clause and by nothing else.</summary>
        private static readonly HeroId Holder = new HeroId("rowenna-red-holder");

        /// <summary>Empty-handed, and three spaces away: excluded by the reach and by nothing else.</summary>
        private static readonly HeroId Distant = new HeroId("rowenna-red-distant");

        private static readonly TeamId BlueTeam = new TeamId("rowenna-blue-team");
        private static readonly TeamId RedTeam = new TeamId("rowenna-red-team");

        private static readonly MapId Map = new MapId("rowenna-surrender-map");

        private static readonly HexCoordinate RowennaAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate VictimAt = new HexCoordinate(1, 0);
        private static readonly HexCoordinate HolderAt = new HexCoordinate(1, -1);
        private static readonly HexCoordinate DistantAt = new HexCoordinate(3, 0);
        private static readonly HexCoordinate AllyAt = new HexCoordinate(0, 1);
        private static readonly HexCoordinate SecondAllyAt = new HexCoordinate(-1, 0);

        // -------------------------------------------------------------------------------------------
        // Who the sentence admits
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The offer is exactly the adjacent enemy heroes with an empty hand, and every exclusion in it
        /// is a different word of the sentence doing work: "enemy" keeps both of her team-mates out,
        /// "adjacent to you" keeps the hero three spaces away out, and "with no cards in hand" keeps
        /// the hero holding one card out.
        /// </summary>
        [Test]
        public void AcceptSurrender_OffersExactlyTheAdjacentEnemyHeroesWithAnEmptyHand()
        {
            var match = new SurrenderMatch();

            var choice = match.StartSkill(RowennaCardContent.AcceptSurrender);

            Assert.That(Named(choice), Is.EqualTo(new[] { Victim.Value }),
                "Both allies stand beside her with empty hands and are never offered; the distant " +
                "enemy's hand is empty too and he is out of reach; the adjacent enemy holding one " +
                "card is the clause's own exclusion.");
        }

        /// <summary>
        /// THE READING THE CLAUSE IS ABOUT, asserted from the board rather than argued: a hero whose
        /// whole hand has been played, resolved or discarded holds nothing, and rulebook p.10 is what
        /// makes that a complete answer - a card is in exactly one place, and none of those places is
        /// the hand it came from.
        /// </summary>
        [Test]
        public void AcceptSurrender_AdmitsAHeroWhoseOnlyCardsArePlayedResolvedOrDiscarded()
        {
            var match = new SurrenderMatch();

            Assert.That(match.CardsOwnedBy(Victim), Is.GreaterThan(0),
                "The victim owns cards - otherwise this proves nothing about WHERE they are.");
            Assert.That(HeroHandQuery.Count(match.Instances, Victim), Is.Zero);
            Assert.That(
                match.LocationsOf(Victim),
                Is.EquivalentTo(new[] { CardLocation.TurnArea, CardLocation.DiscardPile }),
                "One card played and unresolved, the rest discarded: p.13's own two non-hand places.");

            Assert.That(Named(match.StartSkill(RowennaCardContent.AcceptSurrender)),
                Is.EqualTo(new[] { Victim.Value }));
        }

        /// <summary>
        /// The violator of the same clause: one card is not no cards. The hero is adjacent, an enemy,
        /// and immune to nothing - the single card in his hand is the whole reason he is not offered.
        /// </summary>
        [Test]
        public void AcceptSurrender_LeavesOutAnAdjacentEnemyHoldingASingleCard()
        {
            var match = new SurrenderMatch();

            Assert.That(HeroHandQuery.Count(match.Instances, Holder), Is.EqualTo(1));
            Assert.That(match.AdjacentToRowenna(Holder), Is.True);

            Assert.That(Named(match.StartSkill(RowennaCardContent.AcceptSurrender)),
                Has.No.Member(Holder.Value));
        }

        /// <summary>
        /// A hero immune to her Skill is not a candidate at all. p.12 makes a Skill's effect the
        /// action, so the selection asks immunity about the Skill; p.18 states immunity as a rule about
        /// targeting rather than about the operation that follows.
        /// </summary>
        [Test]
        public void AcceptSurrender_DoesNotOfferAnEnemyHeroImmuneToHerSkill()
        {
            var match = new SurrenderMatch(modifications: ImmunityFor(Victim, RuleActivity.Skill));

            var result = match.StartSkillExpectingNoQuestion(RowennaCardContent.AcceptSurrender);

            Assert.That(match.Standing(Victim), Is.True,
                "The only qualifying hero is immune, so the mandatory selection is impossible and " +
                "p.10 stops the card there with nobody defeated.");
            Assert.That(result.Accepted, Is.True);
            Assert.That(match.RemainingLife(RedTeam), Is.EqualTo(match.StartingLifeCounters));
        }

        /// <summary>
        /// THE GREYING. Nobody qualifying is the card having no legal target, and the offer says so
        /// before it is taken: the selection's feasibility probe derives its answer from the same
        /// candidate list its execution uses, so a Skill whose first mandatory step admits nobody
        /// cannot apply anything.
        /// </summary>
        [Test]
        public void AcceptSurrender_IsNotOfferedAtAllWhenNobodyQualifies()
        {
            var qualifying = new SurrenderMatch();
            Assert.That(qualifying.CanStart(RowennaCardContent.AcceptSurrender), Is.True,
                "The same board with a qualifying hero on it offers the Skill, or the negative " +
                "below would pass for the wrong reason.");

            var none = new SurrenderMatch(victimHoldsCards: 1);

            Assert.That(none.CanStart(RowennaCardContent.AcceptSurrender), Is.False,
                "Every adjacent enemy holds a card, so the card is greyed rather than started and " +
                "stopped.");
            Assert.That(none.CanStart(RowennaCardContent.GloriousTriumph), Is.False,
                "The Tier III prints the identical first sentence and greys on the identical set.");
        }

        /// <summary>
        /// A friendly hero with an empty hand standing right beside her is still never offered: the
        /// selection is <see cref="SourceTeamRelation.OpposingSource"/>, which is the printed word
        /// "enemy", and the roster arm excludes the actor herself unconditionally.
        /// </summary>
        [Test]
        public void AcceptSurrender_NeverOffersAFriendlyHeroOrHerself()
        {
            var match = new SurrenderMatch();

            Assert.That(HeroHandQuery.IsEmpty(match.Instances, Ally), Is.True);
            Assert.That(HeroHandQuery.IsEmpty(match.Instances, SecondAlly), Is.True);
            Assert.That(match.AdjacentToRowenna(Ally), Is.True);
            Assert.That(match.AdjacentToRowenna(SecondAlly), Is.True);

            var named = Named(match.StartSkill(RowennaCardContent.AcceptSurrender));

            Assert.That(named, Has.No.Member(Ally.Value));
            Assert.That(named, Has.No.Member(SecondAlly.Value));
            Assert.That(named, Has.No.Member(Rowenna.Value),
                "\"An enemy hero\" is somebody else, and the roster arm excludes the actor before " +
                "any qualifier is asked - which is why her own full hand decides nothing here.");
        }

        /// <summary>
        /// The Tier III's candidate set is the Tier II's. The two faces print one first sentence, and
        /// the clause after the semicolon is about the coins rather than about who may be chosen.
        /// </summary>
        [Test]
        public void GloriousTriumph_OffersTheSameSetAsAcceptSurrender()
        {
            Assert.That(
                Named(new SurrenderMatch().StartSkill(RowennaCardContent.GloriousTriumph)),
                Is.EqualTo(Named(new SurrenderMatch().StartSkill(RowennaCardContent.AcceptSurrender))));
        }

        // -------------------------------------------------------------------------------------------
        // p.13's list, run through the one spend site
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The whole page, in its printed order, for a Defeat a Skill asked for: the hero is removed,
        /// his unresolved card is resolved, his Active effects are cancelled and his markers returned,
        /// the coins are awarded and the Life counters are spent.
        /// </summary>
        [Test]
        public void AcceptSurrender_RunsRulebookPageThirteensListInItsPrintedOrder()
        {
            var match = new SurrenderMatch();

            match.DefeatThroughSurrender(RowennaCardContent.AcceptSurrender);

            Assert.That(match.Standing(Victim), Is.False, "p.13's first line removes him.");
            Assert.That(match.UnresolvedCardOf(Victim), Is.Null,
                "p.13's second line resolves the card he had played and not yet resolved.");
            Assert.That(match.DefeatEventKinds, Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.HeroDefeatRecorded,
                    CardActionResolutionEventKind.HeroRemovalAttempted,
                    CardActionResolutionEventKind.HeroRemovalCompleted,
                    CardActionResolutionEventKind.HeroUnresolvedCardResolved,
                    CardActionResolutionEventKind.HeroDefeatRewardAwarded,
                    CardActionResolutionEventKind.TeamLifeSpent
                }),
                "The page's own order - removal, the unresolved card, the cancellation and marker " +
                "returns, the coins, the counters - produced by the same method p.14's step 6 " +
                "produces it with, because it IS that method.");
            Assert.That(match.CancelledTheVictimsActiveEffects, Is.True,
                "p.13's third line: \"Defeated hero's actions with Active effects are canceled\".");
        }

        /// <summary>
        /// The other half of p.13's third line: "Any markers they had are returned." It is the same
        /// list and the same method, so a marker the defeated hero was wearing comes back exactly as it
        /// does when an Attack defeats him.
        /// </summary>
        [Test]
        public void AcceptSurrender_ReturnsTheMarkersTheDefeatedHeroWasWearing()
        {
            var match = new SurrenderMatch();
            match.GiveAMarkerTo(Victim);
            Assert.That(match.Markers.Attachments, Has.Count.EqualTo(1));

            match.DefeatThroughSurrender(RowennaCardContent.AcceptSurrender);

            Assert.That(match.Markers.Attachments, Is.Empty);
            Assert.That(match.DefeatEventKinds,
                Has.No.Member(CardActionResolutionEventKind.HeroRemovalPrevented),
                "and nothing about the marker stopped the removal.");
        }

        /// <summary>
        /// The prices, each read from its own sentence on the page: Defeat coins by the defeated hero's
        /// LEVEL and collected by Rowenna, assist coins by his lowest card TIER and collected by her
        /// team-mates and not by her, Life counters by the same Tier and spent by HIS team.
        /// </summary>
        [Test]
        public void AcceptSurrender_PricesTheDefeatByLevelForHerAndByTierForEverybodyElse()
        {
            var match = new SurrenderMatch();
            match.RaiseTo(Victim, 4);
            var tier = match.LowestTierOf(Victim);
            Assert.That(tier, Is.EqualTo(CardTier.II), "The staged victim plays with Tier II cards.");

            match.DefeatThroughSurrender(RowennaCardContent.AcceptSurrender);

            Assert.That(match.Coins(Rowenna), Is.EqualTo(HeroDefeatRewardSchedule.DefeatCoins(
                    new HeroLevel(4))),
                "\"When you defeat an enemy hero, collect coins equal to that hero's level\" - and " +
                "the number is his Level rather than his Tier, which a staged Level of 4 separates.");
            Assert.That(match.Coins(Ally), Is.EqualTo(HeroDefeatRewardSchedule.AssistCoins(tier)));
            Assert.That(match.Coins(SecondAlly), Is.EqualTo(HeroDefeatRewardSchedule.AssistCoins(tier)));
            Assert.That(match.Coins(Victim), Is.Zero);
            Assert.That(match.Coins(Holder), Is.Zero,
                "Assist coins are collected by the DEFEATING hero's friendly heroes, not by the " +
                "defeated hero's.");
            Assert.That(
                match.StartingLifeCounters - match.RemainingLife(RedTeam),
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)));
            Assert.That(match.RemainingLife(BlueTeam), Is.EqualTo(match.StartingLifeCounters),
                "Her own team spends nothing.");
        }

        /// <summary>
        /// RO-Q3, end to end. The tripling reaches exactly one column: each team-mate collects three
        /// times the assist coins the Tier II Defeat would otherwise pay, Rowenna's Defeat coins are
        /// the same number Accept Surrender paid, and the Life counters do not move at all.
        /// </summary>
        [Test]
        public void GloriousTriumph_TriplesTheAssistCoinsAndNothingElse()
        {
            var plain = new SurrenderMatch();
            plain.DefeatThroughSurrender(RowennaCardContent.AcceptSurrender);

            var tripled = new SurrenderMatch();
            var tier = tripled.LowestTierOf(Victim);
            tripled.DefeatThroughSurrender(RowennaCardContent.GloriousTriumph);

            Assert.That(tripled.Coins(Ally),
                Is.EqualTo(HeroDefeatRewardSchedule.AssistCoins(tier) * 3));
            Assert.That(tripled.Coins(SecondAlly), Is.EqualTo(tripled.Coins(Ally)),
                "\"Your friendly heroes\" is plural and every one of them collects the tripled sum.");
            Assert.That(tripled.Coins(Rowenna), Is.EqualTo(plain.Coins(Rowenna)),
                "She collects DEFEAT coins, which the clause does not name - RO-Q3.");
            Assert.That(
                tripled.StartingLifeCounters - tripled.RemainingLife(RedTeam),
                Is.EqualTo(plain.StartingLifeCounters - plain.RemainingLife(RedTeam)),
                "The Life counters are p.13's third sentence and the clause does not name them.");
        }

        /// <summary>
        /// The tripling at each Tier the page prints, read from the schedule the spend site reads:
        /// Tier I pays 3, Tier II pays 6 and Tier III pays 9, while the unmultiplied column is the
        /// page's own 1, 2 and 3.
        /// </summary>
        [TestCase(CardTier.I, 1, 3)]
        [TestCase(CardTier.II, 2, 6)]
        [TestCase(CardTier.III, 3, 9)]
        public void TheAssistSchedule_MultipliesTheTierAndLeavesTheUnmultipliedReadingAlone(
            CardTier tier, int plain, int tripled)
        {
            Assert.That(HeroDefeatRewardSchedule.AssistCoins(tier), Is.EqualTo(plain));
            Assert.That(
                HeroDefeatRewardSchedule.AssistCoins(
                    tier, SyntheticConsequenceCapabilities.UnmultipliedAssistCoins),
                Is.EqualTo(plain),
                "p.13's own schedule is a multiplier of one, so the two readings are one number.");
            Assert.That(HeroDefeatRewardSchedule.AssistCoins(tier, 3), Is.EqualTo(tripled));
        }

        /// <summary>
        /// Nothing printed reduces p.13's assist coins, so the smallest legal multiplier is the page's
        /// own schedule - refused loudly at the schedule and at the request rather than quietly
        /// producing a smaller number.
        /// </summary>
        [Test]
        public void AMultiplierBelowThePagesOwnScheduleIsRefused()
        {
            Assert.That(() => HeroDefeatRewardSchedule.AssistCoins(CardTier.II, 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RequestedHeroDefeat(Victim, 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(new RequestedHeroDefeat(Victim).AssistCoinMultiplier,
                Is.EqualTo(SyntheticConsequenceCapabilities.UnmultipliedAssistCoins),
                "A request that names no clause carries the page's own schedule.");
        }

        /// <summary>
        /// THE CONTROL THROUGH THE OTHER ENTRANCE. p.14's step 6 reaches the identical method and must
        /// price the identical Defeat: an ordinary Attack's assist coins are the Tier, unmultiplied,
        /// because no card text handed that entrance a clause.
        /// </summary>
        [Test]
        public void ThePageFourteenAttackEntrance_StillPaysThePagesOwnAssistCoins()
        {
            var control = new AttackDefeatControl();
            var tier = control.LowestTierOfTheDefender();

            var defeat = control.DefeatTheDefender();

            Assert.That(defeat.Defeat.AssistCoins,
                Is.EqualTo(HeroDefeatRewardSchedule.AssistCoins(tier)),
                "Milestone 8.57c adds an argument with a default, so the entrance that carries no " +
                "clause reads exactly what it read before.");
            Assert.That(defeat.Defeat.DefeatCoins, Is.EqualTo(1),
                "and the Defeat coins are the defeated hero's starting Level.");
            Assert.That(defeat.LifeSpend.Spent,
                Is.EqualTo(HeroDefeatRewardSchedule.LifeCounters(tier)));
        }

        /// <summary>
        /// Milestone 8.53's scoreboard, which reads the resolution stream rather than the ledger: the
        /// Kill goes to the ACTING hero of an effect-invoked Defeat, which is Rowenna, and the Death to
        /// the hero the Defeat was run about. Every collected coin is counted once, at the single place
        /// it is credited.
        /// </summary>
        [Test]
        public void TheScoreboard_CreditsTheKillToRowennaTheDeathToTheVictimAndEachCoinOnce()
        {
            var match = new SurrenderMatch(withScoreboard: true);
            var tier = match.LowestTierOf(Victim);

            match.DefeatThroughSurrender(RowennaCardContent.GloriousTriumph);

            Assert.That(match.DefeatRecordedActor, Is.EqualTo(Rowenna),
                "HeroDefeatRecorded.ActorId is the acting hero of the text, and the scoreboard reads " +
                "exactly that field for the Kill.");
            Assert.That(match.DefeatRecordedTarget, Is.EqualTo(Victim));
            Assert.That(match.CoinsEarned(Rowenna), Is.EqualTo(match.Coins(Rowenna)));
            Assert.That(match.CoinsEarned(Ally), Is.EqualTo(HeroDefeatRewardSchedule.AssistCoins(tier) * 3),
                "ONE grant of the tripled sum per hero, which is why the multiplier is expressed at " +
                "the spend site rather than paid a second time afterwards.");
            Assert.That(match.CoinsEarned(SecondAlly), Is.EqualTo(match.CoinsEarned(Ally)));
            Assert.That(match.CoinsEarned(Victim), Is.Zero);
        }

        /// <summary>
        /// The game can END on this card. p.13's Life counters are spent by the same list and the same
        /// spend, so a team on its last counters loses them here exactly as it would to an Attack.
        /// </summary>
        [Test]
        public void AcceptSurrender_EndsTheMatchWhenTheLastLifeCounterIsSpent()
        {
            var match = new SurrenderMatch(lifeCounters: 2);
            Assert.That(
                HeroDefeatRewardSchedule.LifeCounters(match.LowestTierOf(Victim)), Is.EqualTo(2),
                "The staged Tier spends exactly the counters the red team has left.");

            match.DefeatThroughSurrender(RowennaCardContent.AcceptSurrender);

            Assert.That(match.RemainingLife(RedTeam), Is.Zero);
            Assert.That(match.DefeatEventKinds,
                Has.Member(CardActionResolutionEventKind.TeamEliminated),
                "The last counter going is the page's own end, reported on the same list.");
        }

        /// <summary>
        /// The Tier III's tripling does not spend more counters, which is the half of RO-Q3 easiest to
        /// get wrong: a card that multiplied "the reward" rather than "the assist coins" would end the
        /// match three defeats early.
        /// </summary>
        [Test]
        public void GloriousTriumph_SpendsExactlyTheLifeCountersAcceptSurrenderSpends()
        {
            var plain = new SurrenderMatch();
            plain.DefeatThroughSurrender(RowennaCardContent.AcceptSurrender);

            var tripled = new SurrenderMatch();
            tripled.DefeatThroughSurrender(RowennaCardContent.GloriousTriumph);

            Assert.That(tripled.RemainingLife(RedTeam), Is.EqualTo(plain.RemainingLife(RedTeam)));
        }

        /// <summary>
        /// A stale answer cannot defeat the same hero twice. The choice is gone once it is answered,
        /// and the accounting behind p.13's reward refuses a second award for one outcome id even if
        /// something did reach it.
        /// </summary>
        [Test]
        public void AcceptSurrender_RefusesAStaleAnswerRatherThanDefeatingTwice()
        {
            var match = new SurrenderMatch();
            var choice = match.StartSkill(RowennaCardContent.AcceptSurrender);
            var naming = Pick(choice, Victim);
            match.Answer(naming);

            var coins = match.Coins(Ally);
            var remaining = match.RemainingLife(RedTeam);

            Assert.That(match.Submit(naming).Accepted, Is.False);
            Assert.That(match.Coins(Ally), Is.EqualTo(coins));
            Assert.That(match.RemainingLife(RedTeam), Is.EqualTo(remaining));
        }

        // -------------------------------------------------------------------------------------------
        // The request itself, at the frame where it is published and restored
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// What the Defeat node publishes is the request the spend site reads: the hero on both faces,
        /// and the multiplier only on the face whose sentence prints one. A node that declared an
        /// output it does not carry would publish a fact nothing asks for.
        /// </summary>
        [Test]
        public void TheDefeatRequest_CarriesTheHeroOnBothFacesAndTheMultiplierOnOnlyOne()
        {
            var plain = Frame.Run(RowennaCardContent.AcceptSurrender);
            var tripled = Frame.Run(RowennaCardContent.GloriousTriumph);

            Assert.That(plain, Is.EqualTo(new[] { new RequestedHeroDefeat(Victim) }));
            Assert.That(tripled, Is.EqualTo(new[] { new RequestedHeroDefeat(Victim, 3) }));
        }

        /// <summary>
        /// A restore taken while the victim choice is standing resumes against freshly rebuilt content
        /// and answers the same question with the same request - which is what a snapshot carries and
        /// what the resolver has to be able to resume from. Milestone 8.35i is where that boundary was
        /// learned.
        /// </summary>
        [Test]
        public void GloriousTriumph_ResumesThroughARestoreOfThePendingVictimChoice()
        {
            var frame = new Frame(RowennaCardContent.GloriousTriumph);
            var choice = frame.RunToPause();

            frame.RebindTrustedContent();

            var result = frame.SubmitToCompletion(Pick(choice, Victim));

            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestedHeroDefeats,
                Is.EqualTo(new[] { new RequestedHeroDefeat(Victim, 3) }),
                "The multiplier is a published FACT rather than a field, so it survives the restore " +
                "for the same reason the hero does.");
        }

        /// <summary>
        /// THE HERO WHO DRAWS A CARD BETWEEN THE OFFER AND THE ANSWER, which the roadmap named and
        /// which is worth stating precisely because the two refusals in this file are different
        /// refusals. The candidate set is re-derived on every ask rather than remembered from the
        /// moment the question opened, so the named hero is no longer one the sentence admits - and
        /// what that produces is not a rejected command but p.10's impossible mandatory step: the
        /// answer stands, the card stops on it, and nobody is defeated.
        /// </summary>
        [Test]
        public void AcceptSurrender_DefeatsNobodyWhenTheNamedHeroHasGainedACardSinceTheOffer()
        {
            var match = new SurrenderMatch();
            var choice = match.StartSkill(RowennaCardContent.AcceptSurrender);
            var naming = Pick(choice, Victim);

            match.PutACardIntoTheHandOf(Victim);

            Assert.That(match.Submit(naming).Accepted, Is.True,
                "The command was offered and is still the command that was offered; what changed is " +
                "the board, and p.10 answers that with a step that cannot be carried out.");
            Assert.That(match.Standing(Victim), Is.True,
                "\"With no cards in hand\" is asked again when the answer arrives, because a hand is " +
                "state and a set remembered from the offer would defeat a hero the sentence no " +
                "longer admits.");
            Assert.That(match.RemainingLife(RedTeam), Is.EqualTo(match.StartingLifeCounters));
            Assert.That(match.Coins(Ally), Is.Zero);
            Assert.That(match.Coins(Rowenna), Is.Zero);
            Assert.That(match.DefeatEventKinds, Is.Empty);
        }

        /// <summary>
        /// The frame refuses a hero it did not offer. The candidate set is re-derived on the answer
        /// rather than remembered, so naming the hero holding a card is refused at the moment it
        /// arrives rather than pricing a Defeat the sentence forbids.
        /// </summary>
        [Test]
        public void AcceptSurrender_RefusesAnAnswerNamingAHeroTheClauseExcludes()
        {
            var frame = new Frame(RowennaCardContent.AcceptSurrender);
            var choice = frame.RunToPause();

            var forged = new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Hero(Holder)),
                Rowenna);

            var result = frame.Submit(forged);

            Assert.That(result.Accepted, Is.False,
                "A hero holding a card is not a candidate, and the answer is checked against the set " +
                "the offer was built from rather than against the offer as it was drawn.");
        }

        // -------------------------------------------------------------------------------------------
        // The contracts, their optional shapes, and the deferral
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Both widened contracts are OPTIONAL, which is what makes this milestone invisible to every
        /// body composed before it: a selection that never names the empty-hand clause admits exactly
        /// the heroes it admitted, and a Defeat that never names the multiplier is priced by p.13's own
        /// schedule.
        /// </summary>
        [Test]
        public void BothWidenedContracts_AreOptionalAndDefaultToWhatThisBuildAlreadyDid()
        {
            var capabilities = MatchContentCatalogs.ForThisBuild().Capabilities;

            var selection = Parameter(
                capabilities, SyntheticRosterCapabilities.ChooseTargetV2,
                SyntheticRosterCapabilities.RequiresEmptyHandParameter);
            Assert.That(selection.IsRequired, Is.False);
            Assert.That(selection.ValueKind, Is.EqualTo(EffectValueKind.Boolean));

            var multiplier = Parameter(
                capabilities, SyntheticConsequenceCapabilities.DefeatHeroV1,
                SyntheticConsequenceCapabilities.AssistCoinMultiplierParameter);
            Assert.That(multiplier.IsRequired, Is.False);
            Assert.That(multiplier.ValueKind, Is.EqualTo(EffectValueKind.Integer));

            Assert.That(
                SyntheticRosterCapabilities.RequiresEmptyHandParameter,
                Is.EqualTo(SyntheticTargetingCapabilities.RequiresEmptyHandParameterName),
                "One clause has one name in this catalog, which is milestone 8.39h's rule for the " +
                "same pair of families.");
        }

        /// <summary>
        /// Both capabilities her two faces name are ALREADY REGISTERED by this build, so these faces
        /// add no deferral of their own - which is why they can be driven through the dispatcher at all
        /// while 8.57b's gauntlet cannot. The one declaration that block deferred is still deferred and
        /// still the only one.
        /// </summary>
        [Test]
        public void HerDefeatFaces_NameOnlyCapabilitiesThisBuildAlreadyRegisters()
        {
            var capabilities = MatchContentCatalogs.ForThisBuild().Capabilities;

            Assert.That(capabilities.IsRegistered(SyntheticRosterCapabilities.ChooseTargetV2), Is.True);
            Assert.That(capabilities.IsRegistered(SyntheticConsequenceCapabilities.DefeatHeroV1), Is.True);
            Assert.That(
                SyntheticPlacementCapabilities.DeferredHandlers().Select(handler => handler.Reference),
                Has.No.Member(SyntheticPlacementCapabilities.PlaceV2)
                    .And.No.Member(SyntheticRosterCapabilities.ChooseTargetV2),
                "This block's two faces defer NOTHING: both capabilities they name are registered, " +
                "which is why they can be driven through the dispatcher at all while 8.57b's " +
                "gauntlet cannot. Which declarations ARE deferred is 8.57b's and 8.57d's claim and " +
                "PlacementLineQualifierTests asserts the exact list, so this case states what these " +
                "two faces owe rather than counting somebody else's deferrals.");

            Assert.That(RowennaCardContent.ContentRevision, Is.EqualTo("rowenna-9-1-rev-1"),
                "Milestone 8.57f minted the real revision when her whole set moved together, " +
                "replacing the deliberately malformed placeholder these composition blocks carried. " +
                "Milestone 7.9's gate is unaffected and is asserted where it lives: nothing of hers " +
                "is registered or dealt until 8.57g, and RowennaRewardTests owns the roster half.");
        }

        /// <summary>
        /// The two faces as her shard's table prints them, transcribed rather than derived - and the
        /// absences are content: neither prints a Range or a Radius icon, which is why "adjacent to
        /// you" is a number the sentence writes down rather than one p.16's items could raise.
        /// </summary>
        [TestCase("accept-surrender", "Accept Surrender", 10, 6, 2, (int)CardTier.II,
            (int)PrintedValueChannel.Defense)]
        [TestCase("glorious-triumph", "Glorious Triumph", 10, 7, 2, (int)CardTier.III,
            (int)PrintedValueChannel.Movement)]
        public void HerDefeatFaces_CarryTheirPrintedValues(
            string idSuffix, string name, int initiative, int defense, int movement, int tier,
            int itemChannel)
        {
            var definition = RowennaCardContent.Definition(
                new CardDefinitionId("rowenna-" + idSuffix));

            Assert.That(definition.DisplayName, Is.EqualTo(name));
            Assert.That(definition.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(definition.Progression.Color, Is.EqualTo(CardColor.Blue),
                "RO-03 is open on both faces and the shard's working assumption is the RIBBON.");
            Assert.That(definition.Progression.Tier, Is.EqualTo((CardTier)tier));
            Assert.That(definition.Progression.ItemIcon,
                Is.EqualTo((PrintedValueChannel)itemChannel));
            Assert.That(Printed(definition, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(defense));
            Assert.That(Printed(definition, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(movement));
            Assert.That(
                definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill)
                    .BaseValues,
                Is.Empty,
                "RO-05 again: a missing icon is absent, never zero, and the Skill's reach is the " +
                "printed word \"adjacent\".");
        }

        /// <summary>
        /// Her Blue ladder is whole now, so both of p.15's upgrade pairs on it name both siblings -
        /// which 8.57b could not do, because the Defeat faces did not exist yet.
        /// </summary>
        [Test]
        public void HerBlueLadder_NamesBothSiblingsAtEveryStep()
        {
            Assert.That(
                RowennaCardContent.Definition(RowennaCardContent.StandGuard)
                    .Progression.NextTierCandidateIds,
                Is.EqualTo(new[]
                {
                    RowennaCardContent.ProtectTheWeak, RowennaCardContent.AcceptSurrender
                }));
            foreach (var tierTwo in new[]
            {
                RowennaCardContent.ProtectTheWeak, RowennaCardContent.AcceptSurrender
            })
            {
                Assert.That(
                    RowennaCardContent.Definition(tierTwo).Progression.NextTierCandidateIds,
                    Is.EqualTo(new[]
                    {
                        RowennaCardContent.DefendTheInnocent, RowennaCardContent.GloriousTriumph
                    }),
                    tierTwo.Value);
            }
        }

        /// <summary>
        /// The empty-hand reading is one derivation, and the seat's own hand count asks it too. Two
        /// copies of "cards in hand" could disagree about a played card; there is one.
        /// </summary>
        [Test]
        public void TheEmptyHandReading_IsTheSameQueryTheCountFormAsks()
        {
            var match = new SurrenderMatch();

            Assert.That(HeroHandQuery.IsEmpty(match.Instances, Victim), Is.True);
            Assert.That(HeroHandQuery.HasAtLeast(match.Instances, Victim, 1), Is.False);
            Assert.That(HeroHandQuery.IsEmpty(match.Instances, Holder), Is.False);
            Assert.That(HeroHandQuery.HasAtLeast(match.Instances, Holder, 1), Is.True);
            Assert.That(
                HeroHandQuery.IsEmpty(Array.Empty<CardInstance>(), Victim), Is.True,
                "A hero with no cards anywhere holds an empty hand, which is the same answer the " +
                "count form gives.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                .Select(candidate => candidate.Value.HeroId.Value)
                .ToArray();

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HeroId hero) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero));

        private static EffectCapabilityParameter Parameter(
            EffectCapabilityCatalog capabilities,
            EffectCapabilityReference reference,
            string parameterName)
        {
            Assert.That(capabilities.IsRegistered(reference), Is.True, reference.ToString());
            return capabilities.Load(reference).Schema.Parameters
                .Single(parameter => parameter.Name == parameterName);
        }

        private static int Printed(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount)
                .SingleOrDefault();

        /// <summary>
        /// A live modification set carrying one immunity, built the way a match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled, on milestone 8.39b's terms.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"rowenna-surrender-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"RowennaSurrender{activity}Immunity",
                $"rowenna-surrender-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"rowenna-surrender-immunity-{heroId.Value}"),
                definition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("rowenna-surrender-immunity-source"),
                new ActionDefinitionId("rowenna-surrender-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                Participants(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, null, null);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Rowenna)),
                Is.True,
                "The fixture must actually hold the immunity, or the case above proves nothing.");
            return set;
        }

        /// <summary>
        /// Six seats: three Blue, so "your friendly heroes" is plural and the assist coins can be seen
        /// landing on two heroes and never on Rowenna, and three Red, so the three exclusions the
        /// printed sentence makes can each be shown on its own hero.
        /// </summary>
        private static MatchParticipants Participants()
        {
            var seats = new[]
            {
                (Rowenna, BlueTeam), (Ally, BlueTeam), (SecondAlly, BlueTeam),
                (Victim, RedTeam), (Holder, RedTeam), (Distant, RedTeam)
            };

            return new MatchParticipants(
                new[]
                {
                    new Team(BlueTeam, TeamSide.Blue),
                    new Team(RedTeam, TeamSide.Red)
                },
                seats.Select(seat => new Player(PlayerOf(seat.Item1), seat.Item2)).ToArray(),
                seats.Select(seat =>
                    new Hero(seat.Item1, PlayerOf(seat.Item1), HeroDashboard.RulebookLayout))
                    .ToArray());
        }

        private static PlayerId PlayerOf(HeroId hero) => new PlayerId(hero.Value + "-player");

        private static BoardMapDefinition SurrenderMap()
        {
            var zone = new ZoneId("rowenna-surrender-zone");
            var spaces = new List<BoardMapSpace>();
            for (var q = -5; q <= 5; q++)
            {
                for (var r = -5; r <= 5; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(coordinate, RowennaAt) > 5)
                    {
                        continue;
                    }

                    spaces.Add(new BoardMapSpace(
                        new SpaceId($"q{q}_r{r}"), coordinate, BoardMapSpaceKind.Playable, zone));
                }
            }

            return new BoardMapDefinition(
                Map, 1, "Rowenna Surrender Map",
                spaces,
                new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                Array.Empty<BoardMapSpawnPoint>(),
                Array.Empty<ZoneId>());
        }

        private static IReadOnlyList<HeroOccupancy> Occupancies() => new[]
        {
            new HeroOccupancy(Rowenna, RowennaAt),
            new HeroOccupancy(Ally, AllyAt),
            new HeroOccupancy(SecondAlly, SecondAllyAt),
            new HeroOccupancy(Victim, VictimAt),
            new HeroOccupancy(Holder, HolderAt),
            new HeroOccupancy(Distant, DistantAt)
        };

        /// <summary>
        /// A Tier II fixture card for the heroes whose hands and piles these cases stage. It is a
        /// synthetic definition rather than one of Rowenna's because what the cases are about is WHERE
        /// a card is and what Tier it is, never which face it holds.
        /// </summary>
        private static CardDefinition FixtureCard(string id, CardTier tier) =>
            new CardDefinition(
                new CardDefinitionId(id),
                "rowenna-surrender-fixture",
                "TEST - " + id,
                new InitiativeValue(7),
                CardContentAuthority.SyntheticTestFixture,
                // p.16 gives every non-Basic Tier II and III card an Item icon, and
                // CardProgressionProfile refuses one that prints none - so the fixture card carries
                // the same Defense item her own Tier II prints. Nothing here reads it.
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, PrintedValueChannel.Defense),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("fixture-movement"),
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });

        /// <summary>
        /// One staged position with Rowenna's Skill driven through the card-action dispatcher - the
        /// same path a dealt card takes, because both of her Defeat faces name only registered
        /// capabilities.
        /// </summary>
        private sealed class SurrenderMatch
        {
            public const int StartingLife = 8;

            private static readonly CardDefinition Fixture = FixtureCard(
                "rowenna-surrender-fixture-card", CardTier.II);

            private readonly List<CardDefinition> definitions;
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly CardActionResolutionAuthority authority;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly TeamLifeState teamLives;
            private readonly HeroProgressionState levels;
            private readonly HeroCoinLedger ledger;
            private readonly RuleModificationSet modifications;
            private readonly MatchScoreboard scoreboard;
            private IReadOnlyList<ResolutionStep> lastSteps = Array.Empty<ResolutionStep>();

            public SurrenderMatch(
                RuleModificationSet modifications = null,
                int victimHoldsCards = 0,
                int lifeCounters = StartingLife,
                bool withScoreboard = false)
            {
                participants = Participants();
                board = new AdjacentMoveAuthority(
                    SurrenderMap(), Occupancies(), Array.Empty<ObstacleOccupancy>());
                this.modifications = modifications ?? RuleModificationSet.Empty;
                definitions = new List<CardDefinition>(RowennaCardContent.AllDefinitions) { Fixture };
                levels = new HeroProgressionState(participants);
                teamLives = new TeamLifeState(participants, lifeCounters);
                authority = new CardActionResolutionAuthority(
                    MatchContentCatalogs.ForThisBuild(), board, Markers, levels, teamLives);

                // Rowenna's whole composed set in hand, so the face under test is a card she is
                // holding and the offer is derived from it the way a turn derives one.
                foreach (var definition in RowennaCardContent.AllDefinitions)
                {
                    instances.Add(new CardInstance(
                        new CardInstanceId("rowenna-" + definition.Id.Value),
                        definition.Reference,
                        Rowenna,
                        CardStateFacts.InitialInHand()));
                }

                // The victim: one card played and still unresolved - p.13's second line - and two in
                // the discard. Nothing in hand, which is the printed clause.
                Play(Victim, "victim-unresolved");
                Discard(Victim, "victim-discard-a");
                Discard(Victim, "victim-discard-b");
                for (var index = 0; index < victimHoldsCards; index++)
                {
                    Hold(Victim, "victim-held-" + index);
                }

                // The adjacent enemy who is excluded by the clause and by nothing else, and the
                // out-of-reach enemy who is excluded by the reach and by nothing else.
                Hold(Holder, "holder-held");
                Discard(Distant, "distant-discard");

                if (withScoreboard)
                {
                    scoreboard = new MatchScoreboard(participants);
                }

                ledger = EnsureLedger();
                if (scoreboard != null)
                {
                    ledger.BindScoreboard(scoreboard);
                }
            }

            public IReadOnlyList<CardInstance> Instances => instances;

            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();

            public int StartingLifeCounters => StartingLife;

            /// <summary>
            /// One of the roster's own markers, put on the named hero the way a card puts one there -
            /// Tigerclaw's Poison, chosen because what this fixture is about is that p.13 RETURNS a
            /// marker rather than which marker it is.
            /// </summary>
            public void GiveAMarkerTo(HeroId holder) =>
                Markers.Give(new MarkerAttachment(
                    new MarkerInstanceId("rowenna-surrender-marker-" + holder.Value),
                    TigerclawComponentContent.PoisonOneDefinition,
                    Rowenna,
                    holder,
                    new CardInstanceId("rowenna-surrender-marker-source"),
                    new ActionDefinitionId("primary-skill")));

            public bool CancelledTheVictimsActiveEffects => lastSteps.Any(step =>
                step.Kind == ResolutionStepKind.Checkpoint &&
                step.Checkpoint == TimingCheckpoint.ControllerHeroDefeated &&
                step.CheckpointAffectedHeroId.Equals(Victim));

            public IReadOnlyList<CardActionResolutionEventKind> DefeatEventKinds =>
                lastSteps
                    .Where(step => step.Kind == ResolutionStepKind.Fact &&
                        step.Fact.TargetHeroId.Equals(Victim))
                    .Select(step => step.Fact.Kind)
                    .ToArray();

            public HeroId DefeatRecordedActor => RecordedDefeat().ActorId;

            public HeroId DefeatRecordedTarget => RecordedDefeat().TargetHeroId;

            public int Coins(HeroId hero) => ledger.GetCoins(hero);

            public int CoinsEarned(HeroId hero) =>
                scoreboard.Entries.Single(entry => entry.HeroId.Equals(hero)).CoinsEarned;

            public int RemainingLife(TeamId team) => teamLives.GetCounters(team);

            public bool Standing(HeroId hero) =>
                board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(hero));

            public bool AdjacentToRowenna(HeroId hero) =>
                HexDistanceQuery.Calculate(
                    Occupancies().Single(occupancy => occupancy.HeroId.Equals(hero)).Coordinate,
                    RowennaAt) == 1;

            public int CardsOwnedBy(HeroId hero) =>
                instances.Count(card => card.OwnerId.Equals(hero));

            public IReadOnlyList<CardLocation> LocationsOf(HeroId hero) =>
                instances
                    .Where(card => card.OwnerId.Equals(hero))
                    .Select(card => card.State.Location)
                    .Distinct()
                    .ToArray();

            public CardInstance UnresolvedCardOf(HeroId hero) =>
                instances.FirstOrDefault(card =>
                    card.OwnerId.Equals(hero) &&
                    card.State.Location == CardLocation.TurnArea &&
                    card.State.HasBeenPlayed &&
                    card.State.ResolutionStatus == CardResolutionStatus.Unresolved);

            public CardTier LowestTierOf(HeroId hero)
            {
                Assert.That(
                    HeroDefeatCostQuery.TryGetLowestCardTier(hero, definitions, instances, out var tier),
                    Is.True);
                return tier;
            }

            /// <summary>
            /// One more card in the named hero's hand, added to the live instance list the authority
            /// is handed on every ask - which is how a hand changes between a question and its answer.
            /// </summary>
            public void PutACardIntoTheHandOf(HeroId hero) =>
                Hold(hero, "late-draw-" + hero.Value);

            public void RaiseTo(HeroId hero, int level)
            {
                while (levels.GetLevel(hero).Value < level)
                {
                    Assert.That(levels.TryRaiseLevel(hero, out _), Is.True);
                }
            }

            /// <summary>Milestone 6.5af's question, asked of the offer this face produces.</summary>
            public bool CanStart(CardDefinitionId cardId) =>
                authority.CanSkillApplyAnything(
                    Offer(cardId), definitions, instances, participants, timing, modifications, 1, 1);

            /// <summary>Starts the Skill and returns the question it stops on.</summary>
            public PendingEffectChoice StartSkill(CardDefinitionId cardId)
            {
                var started = Start(cardId);
                Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());
                Assert.That(authority.PendingEffectChoice, Is.Not.Null,
                    "The selection is mandatory and opens a question whenever anybody qualifies.");
                return authority.PendingEffectChoice;
            }

            public CardActionResolutionResult StartSkillExpectingNoQuestion(CardDefinitionId cardId)
            {
                var started = Start(cardId);
                Assert.That(authority.PendingEffectChoice, Is.Null,
                    "Nobody qualifies, so p.10 stops the card rather than asking an unanswerable " +
                    "question.");
                return started;
            }

            /// <summary>The whole face, from the offer through the answer that defeats the victim.</summary>
            public void DefeatThroughSurrender(CardDefinitionId cardId)
            {
                var choice = StartSkill(cardId);
                Answer(Pick(choice, Victim));
            }

            public void Answer(EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
            }

            public CardActionResolutionResult Submit(EffectChoiceCommand command)
            {
                var result = authority.AnswerEffectChoice(
                    command, definitions, instances, participants, timing, modifications,
                    TieBreakerState.Unrevealed, 1, 1);
                if (result.Accepted)
                {
                    lastSteps = result.Steps;
                }

                return result;
            }

            private CardActionResolutionResult Start(CardDefinitionId cardId) =>
                authority.StartTurnSkill(
                    new ActionResolutionId("rowenna-surrender-" + cardId.Value),
                    Offer(cardId),
                    definitions,
                    instances,
                    participants,
                    timing,
                    modifications,
                    TieBreakerState.Unrevealed,
                    1,
                    1);

            /// <summary>
            /// p.7 commits a card facedown and reveals it; the turn's own offers are derived from the
            /// card standing in the turn area, which is the window a turn Skill starts from.
            /// </summary>
            private ActionOffer Offer(CardDefinitionId cardId)
            {
                var card = instances.Single(instance =>
                    instance.OwnerId.Equals(Rowenna) &&
                    instance.DefinitionReference.DefinitionId.Equals(cardId));
                if (card.State.Location == CardLocation.Hand)
                {
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                }

                return ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, instances, Rowenna, card.Id)
                    .Single(candidate =>
                        candidate.ActionDefinitionId.Equals(RowennaCardContent.PrimarySkill));
            }

            private CardActionResolutionEvent RecordedDefeat() =>
                lastSteps
                    .Where(step => step.Kind == ResolutionStepKind.Fact &&
                        step.Fact.Kind == CardActionResolutionEventKind.HeroDefeatRecorded)
                    .Select(step => step.Fact)
                    .Single();

            /// <summary>
            /// The dispatcher creates p.13's ledger lazily on the first Defeat, and a scoreboard has to
            /// be bound to it BEFORE the coins are credited. The seam that creates it is internal to
            /// Core and Core publishes no <c>InternalsVisibleTo</c> for this assembly, which is the
            /// same route <see cref="BainMarkerDefeatCostTests"/> takes to the effect-invoked entrance.
            /// Nothing is replaced: the production method is called, and what it returns is the ledger
            /// the spend site will use.
            /// </summary>
            private HeroCoinLedger EnsureLedger()
            {
                var seam = typeof(CardActionResolutionAuthority).GetMethod(
                    "EnsureCoinLedger",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(MatchParticipants) },
                    null);
                Assert.That(seam, Is.Not.Null,
                    "The dispatcher still owns one coin ledger and one way to create it.");
                return (HeroCoinLedger)seam.Invoke(authority, new object[] { participants });
            }

            private void Play(HeroId owner, string id)
            {
                var card = Add(owner, id, CardStateFacts.InitialInHand());
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            }

            private void Discard(HeroId owner, string id) =>
                Add(owner, id, CardStateFacts.DiscardedUnplayed());

            private void Hold(HeroId owner, string id) =>
                Add(owner, id, CardStateFacts.InitialInHand());

            private CardInstance Add(HeroId owner, string id, CardStateFacts state)
            {
                var card = new CardInstance(
                    new CardInstanceId(id), Fixture.Reference, owner, state);
                instances.Add(card);
                return card;
            }
        }

        /// <summary>
        /// The same staged position walked at the EFFECT FRAME, for the two questions a dispatcher
        /// cannot answer: what the completed request publishes, and whether the walk resumes from a
        /// frame whose trusted content has been rebuilt - which is what a snapshot carries.
        /// </summary>
        private sealed class Frame
        {
            private static readonly CardDefinition Fixture = FixtureCard(
                "rowenna-surrender-frame-card", CardTier.II);

            private readonly CardDefinitionId cardId;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly MatchComponents components;
            private EffectResolutionContext context;
            private PendingResolution frame;

            public Frame(CardDefinitionId cardId)
            {
                this.cardId = cardId;
                participants = Participants();
                var board = new AdjacentMoveAuthority(
                    SurrenderMap(), Occupancies(), Array.Empty<ObstacleOccupancy>());
                components = new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board, board);
                definitions = RowennaCardContent.AllDefinitions.Concat(new[] { Fixture }).ToArray();
                instances = new[]
                {
                    new CardInstance(
                        new CardInstanceId("frame-victim-discard"), Fixture.Reference, Victim,
                        CardStateFacts.DiscardedUnplayed()),
                    new CardInstance(
                        new CardInstanceId("frame-holder-held"), Fixture.Reference, Holder,
                        CardStateFacts.InitialInHand())
                };
                context = BuildContext();
            }

            /// <summary>The requests one whole walk of the named face publishes.</summary>
            public static IReadOnlyList<RequestedHeroDefeat> Run(CardDefinitionId cardId)
            {
                var frame = new Frame(cardId);
                var choice = frame.RunToPause();
                return frame.SubmitToCompletion(Pick(choice, Victim)).RequestedHeroDefeats;
            }

            public PendingEffectChoice RunToPause()
            {
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("rowenna-surrender-frame"),
                    new CardInstanceId("rowenna-surrender-frame-source"),
                    RowennaCardContent.Definition(cardId).Reference,
                    RowennaCardContent.PrimarySkill,
                    Rowenna,
                    Rowenna,
                    ActionInvocationContext.PrintedPrimary,
                    context,
                    RepeatContinuation.FirstInstance);
                Assert.That(result.Accepted, Is.True, "The walk must start.");
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                frame = result.Frame;
                Assert.That(frame.PendingChoice, Is.Not.Null);
                return frame.PendingChoice;
            }

            /// <summary>
            /// Rebuilds everything the catalog and the content own, keeping only the frame - which is
            /// what a snapshot carries and what a restore has to be able to resume from.
            /// </summary>
            public void RebindTrustedContent() => context = BuildContext();

            public EffectResolutionStepResult SubmitToCompletion(EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                return result;
            }

            public EffectResolutionStepResult Submit(EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                if (result.Accepted)
                {
                    frame = result.Frame;
                }

                return result;
            }

            private EffectResolutionContext BuildContext() =>
                new EffectResolutionContext(
                    RowennaEffectContent.SkillEffectFor(
                        cardId, RowennaCardContent.ContentRevision),
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Skill,
                    components: components);
        }

        /// <summary>
        /// The OTHER entrance to p.13's list: an ordinary Attack driven to step 6, on
        /// <see cref="BainMarkerDefeatCostTests"/>' staged two-hero board. It exists so this block can
        /// prove the entrance it did not touch is the entrance it did not touch.
        /// </summary>
        private sealed class AttackDefeatControl
        {
            private readonly MatchConfiguration configuration;
            private readonly CardActionResolutionAuthority authority;
            private readonly HeroId attacker;
            private readonly HeroId defender;
            private readonly ActionOffer attackOffer;

            public AttackDefeatControl()
            {
                configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                attacker = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                defender = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                var board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(attacker, new HexCoordinate(0, 0)),
                        new HeroOccupancy(defender, new HexCoordinate(1, 0))
                    });
                authority = new CardActionResolutionAuthority(
                    MatchContentCatalogs.ForThisBuild(),
                    board,
                    new MarkerAttachmentRegister(),
                    new HeroProgressionState(configuration.Participants),
                    new TeamLifeState(configuration.Participants, 8));
                attackOffer = ActionOfferQuery.GetAvailableOffers(
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        attacker,
                        ActionOfferWindow.NormalPlay)
                    .Single(candidate =>
                        candidate.SourceCardInstanceId.Value == "red-pursuit-card" &&
                        candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            }

            public CardTier LowestTierOfTheDefender()
            {
                Assert.That(HeroDefeatCostQuery.TryGetLowestCardTier(
                        defender,
                        configuration.CardDefinitions,
                        configuration.CardInstances,
                        out var tier), Is.True);
                return tier;
            }

            public HeroDefeatResolution DefeatTheDefender()
            {
                var start = authority.StartAttackResolution(
                    new ActionResolutionId("surrender-control"),
                    attackOffer,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                var opened = authority.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        start.PendingTargetChoice.ResolutionId,
                        start.PendingTargetChoice.ChoiceId,
                        defender),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                var weakest = opened.PendingDefenseChoice.LegalDefenseOffers
                    .OrderBy(offer => PrintedValueQuery.CalculatePrintedValue(
                        offer, configuration.CardDefinitions, PrintedValueChannel.Defense).Total)
                    .First();
                authority.AnswerDefenseResponse(
                    DefenseResponseChoiceCommand.SelectCards(
                        opened.PendingDefenseChoice.ResolutionId,
                        opened.PendingDefenseChoice.ChoiceId,
                        weakest.SourceCardInstanceId),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                var outcome = authority.ResolveAttackOutcome(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
                Assert.That(outcome.HeroDefeat, Is.Not.Null);
                return outcome.HeroDefeat;
            }

            private static HexCoordinate[] FeaturelessDisk(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates.ToArray();
            }
        }
    }
}
