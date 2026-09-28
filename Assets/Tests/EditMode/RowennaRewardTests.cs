using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
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
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.57a. The five printed clauses of Rowenna's that hand another hero something -
    /// Code of Chivalry's two retrieves, the Reds' coin and retrieval for a friendly hero in radius,
    /// and the Paragons' repeat gated on whether that rider was actually performed.
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Rowenna's is registered - milestone 7.9's gate holds her content out of
    /// <see cref="MatchContentCatalogs"/> until 8.57g deals her - so no composed match can put one of
    /// these cards in a hand, offer its Attack, take p.14's Defense window and reach step 1 or step 7.
    /// Milestone 8.40i recorded exactly this boundary for Bain's retrieval clause and 8.40j is the
    /// milestone that paid it, on the match his dealing composed. So what is proved here is everything
    /// the text itself decides - who is offered what, whose decision each one is, what a decline
    /// leaves behind, which coins move and whether a repeat is asked for - and what is deliberately
    /// NOT proved here is the enclosing rulebook p.14 order: 8.57f and 8.57g own that, and the
    /// permanent player-path gate at 8.57h owns the pointer.</para>
    ///
    /// <para><b>The board is the smallest one on which "a friendly hero in radius" can be true of
    /// anybody</b> - <see cref="StagedBoardParticipants.CreateWithBlueAlly"/>'s three seats, which
    /// milestone 8.21 added for exactly this printed phrase. Rowenna stands on the Blue side with one
    /// ally; the enemy hero is the unit her Attack takes.</para>
    /// </summary>
    public sealed class RowennaRewardTests
    {
        private static readonly HeroId Rowenna = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static readonly HexCoordinate RowennaAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        /// <summary>Adjacent to Rowenna: inside every printed Radius these five faces carry.</summary>
        private static readonly HexCoordinate Beside = new HexCoordinate(0, 1);

        /// <summary>Three spaces away: outside Token of Gratitude's Radius 1 and inside Fair Share's 3.</summary>
        private static readonly HexCoordinate ThreeAway = new HexCoordinate(0, 3);

        /// <summary>Five spaces away: outside every Radius in her composed set.</summary>
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 5);

        private static readonly MinionId SomeMinion = new MinionId("rowenna-reward-minion");

        // -------------------------------------------------------------------------------------------
        // Code of Chivalry - "Before the attack: If you target a hero, both you and the target may
        // retrieve a discarded card."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Two prompts, in the printed order, each addressed to the hero whose pile it is about and
        /// each offering only that hero's own cards. p.10's "you can never retrieve another hero's
        /// cards" is not a rule this text has to restate - it is why the node names ONE hero as both
        /// the owner and the chooser - and this case is what proves the two never cross.
        /// </summary>
        [Test]
        public void CodeOfChivalry_AgainstAHero_OffersEachHeroTheirOwnPileAndNobodyElses()
        {
            var world = World.WithHeroTarget(RowennaCardContent.CodeOfChivalry);

            var mine = world.RunToPause();
            Assert.That(mine.ChooserId, Is.EqualTo(Rowenna),
                "Her own retrieval is hers to answer, and the sentence puts it first.");
            Assert.That(CardIds(mine), Is.EqualTo(new[] { "rowenna-discard-a", "rowenna-discard-b" }),
                "and it offers her own discard pile, in the canonical order the capability derives.");

            var theirs = world.Answer(mine, Pick(mine, "rowenna-discard-a"));
            Assert.That(theirs.ChooserId, Is.EqualTo(Enemy),
                "The target's retrieval belongs to the TARGET - a decision the card text instructs " +
                "the other hero to make, which is rulebook p.10's own exception.");
            Assert.That(CardIds(theirs), Is.EqualTo(new[] { "enemy-discard-a" }),
                "and it offers the target's own pile. Neither prompt ever names the other hero's " +
                "card, which is p.10 read exactly.");

            world.AnswerToCompletion(theirs, Pick(theirs, "enemy-discard-a"));

            Assert.That(world.InHand("rowenna-discard-a"), Is.True);
            Assert.That(world.InHand("enemy-discard-a"), Is.True);
            Assert.That(world.InHand("rowenna-discard-b"), Is.False,
                "One card each: the sentence gives each hero one retrieval, not a pile.");
        }

        /// <summary>
        /// "may" is each hero's own, on each hero's own prompt: declining one leaves the other exactly
        /// as it was. The decline rides on the retrieval's own card choice rather than on a separate
        /// confirm-or-decline, which is what a <c>may</c> Capability node whose handler names its
        /// chooser already means.
        /// </summary>
        [Test]
        public void CodeOfChivalry_LetsEitherHeroDecline_WithoutTouchingTheOther()
        {
            var world = World.WithHeroTarget(RowennaCardContent.CodeOfChivalry);

            var mine = world.RunToPause();
            Assert.That(mine.DeclineInvocation, Is.Not.Null,
                "A `may` retrieval is declinable however many cards are in the pile.");

            var theirs = world.Answer(mine, mine.DeclineInvocation);
            world.AnswerToCompletion(theirs, Pick(theirs, "enemy-discard-a"));

            Assert.That(world.InHand("rowenna-discard-a"), Is.False);
            Assert.That(world.InHand("rowenna-discard-b"), Is.False,
                "She declined, so nothing of hers moved.");
            Assert.That(world.InHand("enemy-discard-a"), Is.True,
                "and the target's own answer is untouched by her decline.");
        }

        /// <summary>
        /// The other decline, and the refusal that keeps the two apart: Rowenna cannot answer the
        /// target's question. Milestone 7.1a made "who sent this" a question the resolver asks before
        /// what the answer was, and this is the first Rowenna clause that needs it.
        /// </summary>
        [Test]
        public void CodeOfChivalry_RefusesTheActorAnsweringTheTargetsOwnQuestion()
        {
            var world = World.WithHeroTarget(RowennaCardContent.CodeOfChivalry);
            var mine = world.RunToPause();
            var theirs = world.Answer(mine, mine.DeclineInvocation);

            var forged = new EffectChoiceCommand(
                theirs.ResolutionId, theirs.ChoiceId, EffectChoiceCommandKind.Decline,
                answeringHeroId: Rowenna);
            var refused = world.Submit(forged);

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.WrongChooser),
                "It is refused as the wrong chooser rather than as an invalid answer, because the " +
                "difference is exactly what a player needs told.");
            Assert.That(world.InHand("enemy-discard-a"), Is.False,
                "and the target's card stays where it was, with their question still standing.");
        }

        /// <summary>
        /// An Attack that took a minion offers neither retrieval. p.13 lets "a unit" be a minion, so
        /// "no" is an ordinary answer: the Conditional applies, takes no branch, and the text is over
        /// without anybody being asked anything.
        /// </summary>
        [Test]
        public void CodeOfChivalry_AgainstAMinion_OffersNeitherRetrieval()
        {
            var world = World.WithMinionTarget(RowennaCardContent.CodeOfChivalry);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses: a text whose whole content is behind 'if you target a hero' asks " +
                "nobody anything when the Attack took a minion.");
            Assert.That(world.InHand("rowenna-discard-a"), Is.False);
            Assert.That(world.InHand("enemy-discard-a"), Is.False);
        }

        /// <summary>
        /// And an empty pile is not a prompt with no answers: a <c>may</c> step that cannot be taken
        /// is simply not taken. Her pile is emptied here and the target's is not, so the printed
        /// sentence's first half is skipped in silence and its second half still happens.
        /// </summary>
        [Test]
        public void CodeOfChivalry_SkipsAHeroWithNothingToRetrieve_AndStillAsksTheOther()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.CodeOfChivalry, rowennaDiscards: 0);

            var theirs = world.RunToPause();

            Assert.That(theirs.ChooserId, Is.EqualTo(Enemy),
                "The first prompt reached is the target's, because hers could not be opened at all.");
            world.AnswerToCompletion(theirs, Pick(theirs, "enemy-discard-a"));
            Assert.That(world.InHand("enemy-discard-a"), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Token of Gratitude / Fair Share - "After the attack: A friendly hero in radius gains 1 coin."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The coin goes to the hero ROWENNA chooses, and the set she chooses from is her own side
        /// inside her own printed Radius, without her. "Friendly" excludes the actor - the reading
        /// Widget's shard settled for the same printed word - so a selection that offered her would be
        /// offering a hero the sentence does not name.
        /// </summary>
        [Test]
        public void TokenOfGratitude_PaysOneCoinToTheFriendlyHeroSheNames_AndOffersNobodyElse()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.TokenOfGratitude, allyAt: Beside);

            var pick = world.RunToPause();

            Assert.That(pick.ChooserId, Is.EqualTo(Rowenna),
                "Her card, her choice: the sentence instructs no other hero to choose anything.");
            Assert.That(HeroIds(pick), Is.EqualTo(new[] { Ally.Value }),
                "One candidate and exactly one: not Rowenna, because 'friendly' is somebody else, " +
                "and not the enemy hero standing beside her, because it is her own side.");

            world.AnswerToCompletion(pick, Pick(pick, Ally));

            Assert.That(world.Coins(Ally), Is.EqualTo(1));
            Assert.That(world.Coins(Rowenna), Is.EqualTo(0),
                "and the hero who played the card is paid nothing by it.");
            Assert.That(world.Coins(Enemy), Is.EqualTo(0));
        }

        /// <summary>
        /// No friendly hero inside the disk is the text stopping rather than a coin paid to nobody:
        /// "gains 1 coin" prints no "may" and no "if able", so the selection is mandatory and an empty
        /// disk is p.10's stop after everything already applied.
        /// </summary>
        [Test]
        public void TokenOfGratitude_WithNoFriendlyHeroInRadius_AsksNothingAndPaysNobody()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.TokenOfGratitude, allyAt: FarAway);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True, "The text is over rather than standing open.");
            Assert.That(world.Coins(Ally), Is.EqualTo(0));
            Assert.That(world.Coins(Rowenna), Is.EqualTo(0));
        }

        /// <summary>
        /// The two faces share one body and differ only in the number their icon carries, which is the
        /// whole reason the Radius is read off the acting card rather than written into the definition:
        /// one ally, one position, two cards, two answers.
        /// </summary>
        [Test]
        public void TheSharedRewardBody_ReadsEachFacesOwnPrintedRadius()
        {
            var nearMiss = World.WithHeroTarget(
                RowennaCardContent.TokenOfGratitude, allyAt: ThreeAway);
            Assert.That(nearMiss.Run().IsComplete, Is.True);
            Assert.That(nearMiss.Coins(Ally), Is.EqualTo(0),
                "Token of Gratitude prints Radius 1 and the ally is three spaces away.");

            var reaches = World.WithHeroTarget(RowennaCardContent.FairShare, allyAt: ThreeAway);
            var pick = reaches.RunToPause();
            reaches.AnswerToCompletion(pick, Pick(pick, Ally));
            Assert.That(reaches.Coins(Ally), Is.EqualTo(1),
                "and Fair Share prints Radius 3 on the identical body, which reaches him.");
        }

        // -------------------------------------------------------------------------------------------
        // Feat of Bravery - "After the attack, a friendly hero in radius may retrieve a discarded card."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rowenna names the hero; the hero names the card. Two decisions, two owners, and the second
        /// one is over that hero's OWN pile - which is p.10's retrieval rule and p.10's rule that a
        /// step's choice belongs to whoever performs it, in one clause.
        /// </summary>
        [Test]
        public void FeatOfBravery_LetsTheNamedHeroChooseWhichOfTheirOwnCardsComesBack()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.FeatOfBravery, allyAt: Beside);

            var pick = world.RunToPause();
            Assert.That(pick.ChooserId, Is.EqualTo(Rowenna));
            Assert.That(HeroIds(pick), Is.EqualTo(new[] { Ally.Value }));

            var card = world.Answer(pick, Pick(pick, Ally));
            Assert.That(card.ChooserId, Is.EqualTo(Ally),
                "The retrieval is his act out of his own pile, so the card choice is his.");
            Assert.That(CardIds(card), Is.EqualTo(new[] { "ally-discard-a", "ally-discard-b" }),
                "and what he is offered is his own pile and nothing of hers.");

            world.AnswerToCompletion(card, Pick(card, "ally-discard-b"));

            Assert.That(world.InHand("ally-discard-b"), Is.True);
            Assert.That(world.InHand("ally-discard-a"), Is.False);
            Assert.That(world.InHand("rowenna-discard-a"), Is.False,
                "Nothing of hers moved: the sentence names one hero and it is not her.");
        }

        /// <summary>
        /// "may" is his, so he may decline - and declining leaves the text finished with nothing done.
        /// </summary>
        [Test]
        public void FeatOfBravery_IsTheNamedHerosToDecline()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.FeatOfBravery, allyAt: Beside);

            var pick = world.RunToPause();
            var card = world.Answer(pick, Pick(pick, Ally));
            Assert.That(card.DeclineInvocation, Is.Not.Null);

            var finished = world.Submit(card.DeclineInvocation);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.InHand("ally-discard-a"), Is.False);
            Assert.That(world.InHand("ally-discard-b"), Is.False);
        }

        /// <summary>
        /// A hero with nothing in the discard is not offered at all. The selection declares the
        /// retrieval as its consumer, and this catalog's rule 15 says a selection offers only the
        /// objects that consumer can act on - the identical narrowing Bain's Drinking Buddies carries
        /// on the identical clause.
        /// </summary>
        [Test]
        public void FeatOfBravery_OffersNoHeroWhoHasNothingToRetrieve()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.FeatOfBravery, allyAt: Beside, allyDiscards: 0);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "The only friendly hero in the disk has an empty pile, so the selection has no " +
                "candidate its consumer could act on and the text stops without a prompt.");
        }

        // -------------------------------------------------------------------------------------------
        // The Paragons - "If they do, repeat once on a different target."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The repeat follows only after the coin actually moved, and it is automatic: "repeat once"
        /// prints no "may", so it is not Rowenna's to decline (ruling W1, milestone 9.1 - it was
        /// declinable before). What it requests is p.10's repeat of the whole Attack with the unit this
        /// instance took excluded.
        /// </summary>
        [Test]
        public void ParagonOfGrace_RepeatsOnADifferentTargetWithoutAsking_OnceTheCoinIsPaid()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.ParagonOfGrace, allyAt: Beside);

            var pick = world.RunToPause();
            var finished = world.AnswerToCompletion(pick, Pick(pick, Ally));

            Assert.That(world.Coins(Ally), Is.EqualTo(1), "The coin is paid first.");
            Assert.That(finished.IsComplete, Is.True,
                "W1: no confirm-or-decline stands between the coin and the repeat.");
            Assert.That(finished.RepeatRequested.IsRequested, Is.True);
            Assert.That(
                finished.RepeatRequested.Binding, Is.EqualTo(RepeatTargetBinding.DifferentTarget),
                "'on a different target' excludes the unit this Attack took, which is the action's " +
                "own state rather than anything a node here selected.");
        }

        /// <summary>
        /// And the gate is a gate: with nobody to pay, no coin moves, nothing asks and no repeat is
        /// requested. This is the whole difference between "if they do" and "repeat once".
        /// </summary>
        [Test]
        public void ParagonOfGrace_AsksForNoRepeatWhenNoFriendlyHeroIsInRadius()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.ParagonOfGrace, allyAt: FarAway);

            var finished = world.Run();

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Coins(Ally), Is.EqualTo(0));
            Assert.That(finished.RepeatRequested.IsRequested, Is.False,
                "A rider that did not happen gates the sentence after it, and that sentence is the " +
                "whole of the repeat.");
        }

        /// <summary>
        /// The retrieval half of the same gate, on both answers: a performed retrieval opens the
        /// repeat and a declined one does not - which is the case the coin half cannot make, because
        /// a coin has no decline.
        /// </summary>
        [Test]
        public void ParagonOfValor_RepeatsOnAPerformedRetrievalAndNotOnADeclinedOne()
        {
            var performed = World.WithHeroTarget(
                RowennaCardContent.ParagonOfValor, allyAt: Beside);
            var pick = performed.RunToPause();
            var card = performed.Answer(pick, Pick(pick, Ally));
            var taken = performed.AnswerToCompletion(card, Pick(card, "ally-discard-a"));

            Assert.That(taken.IsComplete, Is.True, "W1: the repeat follows without a question.");
            Assert.That(taken.RepeatRequested.IsRequested, Is.True);
            Assert.That(
                taken.RepeatRequested.Binding, Is.EqualTo(RepeatTargetBinding.DifferentTarget));

            var declined = World.WithHeroTarget(
                RowennaCardContent.ParagonOfValor, allyAt: Beside);
            var pickAgain = declined.RunToPause();
            var cardAgain = declined.Answer(pickAgain, Pick(pickAgain, Ally));
            var finished = declined.Submit(cardAgain.DeclineInvocation);

            Assert.That(finished.IsComplete, Is.True,
                "A declined retrieval finishes the text where it stands: nothing else is asked.");
            Assert.That(finished.RepeatRequested.IsRequested, Is.False,
                "'If they do' is false, so the repeat is not offered and not requested.");
        }

        /// <summary>
        /// Ruling W1: the Paragons print "repeat once", not "may repeat once", so the repeat is not
        /// Rowenna's to decline - no confirm-or-decline opens, and the repeat is requested wherever the
        /// rider happened. (Milestone 8.57a composed it declinable; 9.1 corrected it.)
        /// </summary>
        [Test]
        public void TheParagonsRepeat_IsNotHersToDecline()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.ParagonOfGrace, allyAt: Beside);
            var pick = world.RunToPause();

            var finished = world.AnswerToCompletion(pick, Pick(pick, Ally));

            Assert.That(finished.IsComplete, Is.True, "Nothing is asked after the coin.");
            Assert.That(finished.RepeatRequested.IsRequested, Is.True);
            Assert.That(world.Coins(Ally), Is.EqualTo(1));
        }

        /// <summary>
        /// The repeated instance runs the same rider again - it may name a different friendly hero,
        /// because nothing about the first instance's choice is carried over - and it never asks for a
        /// third: "once" is counted across the whole chain rather than per instance, so the repeat node
        /// of the second instance is ineligible and is skipped in silence.
        /// </summary>
        [Test]
        public void TheRepeatedInstance_PaysItsRiderAgainAndNeverAsksForAThird()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.ParagonOfGrace, allyAt: Beside, repeatsCompleted: 1);

            var pick = world.RunToPause();
            Assert.That(HeroIds(pick), Is.EqualTo(new[] { Ally.Value }),
                "The rider is asked again from scratch on the repeated instance.");

            var finished = world.AnswerToCompletion(pick, Pick(pick, Ally));

            Assert.That(world.Coins(Ally), Is.EqualTo(1), "and it pays again.");
            Assert.That(finished.IsComplete, Is.True,
                "The second instance finishes without opening a confirm-or-decline of its own.");
            Assert.That(finished.RepeatRequested.IsRequested, Is.False,
                "'once' is the whole chain's maximum, so there is never a third order.");
        }

        // -------------------------------------------------------------------------------------------
        // Restore, replay, and the stale answer
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A capability choice paused INSIDE an Attack's rider survives being rebound to freshly built
        /// trusted content - a new definition object, a new catalog - and resumes to the same result.
        /// That is milestone 8.35i's lesson in the shape this altitude can state it: definition
        /// semantics come from the catalog and the runtime state comes from the frame, so a frame that
        /// has recorded an output its node does not declare, or a choice its node cannot re-derive,
        /// fails here rather than on a player's turn.
        /// </summary>
        [Test]
        public void APauseInsideTheRider_ResumesAgainstFreshlyBoundContentWithTheSameAnswer()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.ParagonOfValor, allyAt: Beside);
            var pick = world.RunToPause();
            var card = world.Answer(pick, Pick(pick, Ally));

            // Everything trusted is rebuilt, which is what a restore does; the frame is what a
            // snapshot carries and it is the only thing kept.
            world.RebindTrustedContent();

            var finished = world.AnswerToCompletion(card, Pick(card, "ally-discard-a"));

            Assert.That(world.InHand("ally-discard-a"), Is.True);
            Assert.That(finished.RepeatRequested.IsRequested, Is.True,
                "and the condition after the pause reads the fact the pause's own node published, " +
                "which is the half a restore could silently lose.");
        }

        /// <summary>
        /// An answer that has already been accepted cannot be sent again into the question that
        /// followed it: it names a choice that no longer stands and is refused as stale, leaving the
        /// hero's own card question exactly where it was.
        /// </summary>
        [Test]
        public void AnAcceptedAnswer_CannotBeReplayedOntoTheQuestionThatFollowedIt()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.FeatOfBravery, allyAt: Beside);
            var pick = world.RunToPause();
            var named = Pick(pick, Ally);
            var card = world.Answer(pick, named);

            var replayed = world.Submit(named);

            Assert.That(replayed.Accepted, Is.False);
            Assert.That(replayed.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.StaleChoice),
                "The selection it names has been answered; what stands now is the retrieving hero's " +
                "own card question, and one answer cannot serve both.");

            world.AnswerToCompletion(card, Pick(card, "ally-discard-a"));
            Assert.That(world.InHand("ally-discard-a"), Is.True,
                "and the question that really was standing still takes its own answer.");
            Assert.That(world.InHand("ally-discard-b"), Is.False,
                "with nothing else coming back beside it.");
        }

        // -------------------------------------------------------------------------------------------
        // The scoreboard column a coin lands in
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.53's "Coins earned" column counts the coin this card pays, and counts it for the
        /// hero who collected it rather than for the hero who played the card. The ledger is the one
        /// place in the build where a coin is credited at all, which is why a card-text grant is
        /// counted without the scoreboard knowing anything about Rowenna.
        /// </summary>
        [Test]
        public void TheCoinAFriendlyHeroCollects_LandsInHisOwnScoreboardColumn()
        {
            var world = World.WithHeroTarget(
                RowennaCardContent.TokenOfGratitude, allyAt: Beside, withScoreboard: true);

            var pick = world.RunToPause();
            world.AnswerToCompletion(pick, Pick(pick, Ally));

            Assert.That(world.CoinsEarned(Ally), Is.EqualTo(1));
            Assert.That(world.CoinsEarned(Rowenna), Is.EqualTo(0),
                "She paid it and did not earn it.");
        }

        // -------------------------------------------------------------------------------------------
        // The definitions themselves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition,
        /// asked at the p.14 moment the face prints - the check that would otherwise fault
        /// mid-resolution with the card already spent.
        ///
        /// <para><b>The catalog is the build's plus what this hero's blocks defer, and that sum is
        /// stable across the dealing - which is the whole reason it is written this way.</b> Until
        /// milestone 8.57g, <c>SyntheticPlacementCapabilities.DeferredHandlers</c> held Throw the
        /// Gauntlet's <c>synthetic-place-in-range-beside-a-named-hero@1</c> out of the registered set,
        /// so validating against the registered set alone would have asserted the gate was broken
        /// rather than that the bodies were legal; since the dealing that list is empty and this same
        /// expression is the build's own catalog. Milestone 8.40j's BA-18 is the trap it avoids: a
        /// fixture that named the list HOLDING a contract rather than the list HAVING it threw
        /// "not registered" the moment the hero was dealt. The test below asserts the gate itself;
        /// this one asserts the composition.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogHerDealingWillRegister()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                SyntheticPlacementCapabilities.DeferredHandlers());
            var composed = 0;

            foreach (var definition in RowennaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions.Where(a => a.EffectRoot != null))
                {
                    composed++;
                    Assert.That(
                        EffectDefinitionValidator.ValidateCardText(
                            action.EffectRoot, catalog, action.CardTextTiming),
                        Is.Empty,
                        definition.Id.Value + " carries a body a configured match would refuse.");
                }
            }

            Assert.That(composed, Is.EqualTo(17),
                "Seventeen faces: 8.57a's six - Token of Gratitude and Fair Share are two cards " +
                "printing one sentence at two Radii and each carries its own body - 8.57b's four, " +
                "8.57c's two Blue Defeats, and 8.57d's five Greens.");
        }

        /// <summary>
        /// And NO body of hers is refused by the REGISTERED catalog any more, which is milestone
        /// 7.9's gate discharged. Milestones 8.57a-8.57d wrote this case the other way round - the
        /// four faces naming a deferred Place were refused, and that refusal was the gate observable
        /// rather than a defect - and milestone 8.57g's registration flip turns it around, in the same
        /// edit as the staged-configuration factories, which is Silverarrow's own 8.35z46 law.
        /// </summary>
        [Test]
        public void NoBodyOfHers_IsRefusedByWhatThisBuildActuallyRegisters()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;
            var refused = new List<string>();
            var walked = 0;

            foreach (var definition in RowennaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions.Where(a => a.EffectRoot != null))
                {
                    walked++;
                    if (EffectDefinitionValidator.ValidateCardText(
                            action.EffectRoot, registered, action.CardTextTiming).Count > 0)
                    {
                        refused.Add(definition.Id.Value);
                    }
                }
            }

            Assert.That(refused, Is.Empty,
                "The four that used to be refused - Throw the Gauntlet, Close Quarters, Melee and " +
                "Grand Melee - name the two Places her dealing registered, and her other composed " +
                "faces never named anything the build was withholding.");
            Assert.That(walked, Is.EqualTo(17),
                "and the walk really covered her seventeen composed bodies, so this case is not " +
                "passing because it found nothing to validate.");

            Assert.That(
                EffectDefinitionValidator.ValidateCardText(
                    RowennaCardContent.Definition(RowennaCardContent.ThrowTheGauntlet)
                        .ActionDefinitions
                        .Single(action => action.EffectRoot != null).EffectRoot,
                    registered,
                    CardTextTiming.None),
                Is.Empty,
                "and the one face this block's gate was written about starts cleanly against the " +
                "registered set, read on its own.");
        }

        /// <summary>
        /// The moments the faces print, transcribed a second time here from the shard rather than read
        /// back off the content: her Gold basic is the only one of these five at step 1.
        /// </summary>
        [Test]
        public void ThePrintedMoments_AreStepOneForTheGoldBasicAndStepSevenForEveryRed()
        {
            Assert.That(
                RowennaEffectContent.AttackTextTimingFor(RowennaCardContent.CodeOfChivalry),
                Is.EqualTo(CardTextTiming.BeforeTheAttack));

            foreach (var red in new[]
            {
                RowennaCardContent.TokenOfGratitude, RowennaCardContent.FairShare,
                RowennaCardContent.FeatOfBravery, RowennaCardContent.ParagonOfGrace,
                RowennaCardContent.ParagonOfValor
            })
            {
                Assert.That(
                    RowennaEffectContent.AttackTextTimingFor(red),
                    Is.EqualTo(CardTextTiming.AfterTheAttack), red.Value);
            }
        }

        /// <summary>
        /// Her five composed faces carry the printed values the shard's table records, transcribed a
        /// second time here so a typo in either is a failure rather than a shared assumption. The
        /// Radius column is the one these bodies actually read.
        /// </summary>
        [Test]
        public void TheFiveComposedFaces_CarryTheirTranscribedPrintedValues()
        {
            var rows = new[]
            {
                new object[] { RowennaCardContent.CodeOfChivalry, 11, 2, 1, 4, 0 },
                new object[] { RowennaCardContent.TokenOfGratitude, 8, 6, 4, 5, 1 },
                new object[] { RowennaCardContent.FairShare, 8, 6, 4, 6, 3 },
                new object[] { RowennaCardContent.FeatOfBravery, 8, 7, 4, 7, 4 },
                new object[] { RowennaCardContent.ParagonOfGrace, 9, 7, 4, 6, 3 },
                new object[] { RowennaCardContent.ParagonOfValor, 9, 8, 4, 7, 4 }
            };

            foreach (var row in rows)
            {
                var id = (CardDefinitionId)row[0];
                var definition = RowennaCardContent.Definition(id);
                var attack = definition.ActionDefinitions.Single(
                    action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

                Assert.That(definition.Initiative.Value, Is.EqualTo((int)row[1]), id.Value);
                Assert.That(Icon(definition, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo((int)row[2]), id.Value);
                Assert.That(Icon(definition, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo((int)row[3]), id.Value);
                Assert.That(
                    attack.BaseValues.Single(v => v.Channel == PrintedValueChannel.Attack).Amount,
                    Is.EqualTo((int)row[4]), id.Value);
                Assert.That(
                    attack.BaseValues.Where(v => v.Channel == PrintedValueChannel.Radius)
                        .Select(v => v.Amount).SingleOrDefault(),
                    Is.EqualTo((int)row[5]), id.Value);
                Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged), id.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.NonRanged),
                    Is.False,
                    id.Value + " prints 'Target a unit adjacent to you', which is p.13's own reach " +
                    "for a non-Ranged Attack and narrows nothing.");
                Assert.That(definition.ContentRevision,
                    Is.EqualTo(RowennaCardContent.ContentRevision), id.Value);
            }
        }

        /// <summary>
        /// The revision is the real one from milestone 8.57f on, and the placeholder it replaced is
        /// gone from the tree entirely.
        ///
        /// <para>8.57a through 8.57e carried a deliberately malformed placeholder, under the project's
        /// ID rule learned when a peer's draft minted an id that collided with a real one: a
        /// placeholder must not be a value anything could mistake for content identity. 8.57f made her
        /// set whole, which is the milestone at which the whole set moves together, so the real value
        /// is minted there, every one of the nineteen definitions carries it, and the placeholder
        /// string itself is gone from the tree rather than left behind in a comment for a later grep
        /// to find.</para>
        /// </summary>
        [Test]
        public void TheContentRevision_IsTheRealOneMintedWhenHerSetBecameWhole()
        {
            Assert.That(RowennaCardContent.ContentRevision, Is.EqualTo("rowenna-9-1-rev-1"));
            Assert.That(RowennaCardContent.ContentRevision, Does.Contain("-rev-"),
                "Every dealt hero's revision ends in '-rev-<n>', and hers now reads as one.");
            Assert.That(
                RowennaCardContent.AllDefinitions.Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { "rowenna-9-1-rev-1" }),
                "and one string covers the set, so no face can be left on the placeholder.");
        }

        /// <summary>
        /// And all of it is reachable now, which is milestone 8.57g's dealing read from the roster's
        /// own side. Milestones 8.57a-8.57f wrote this case the other way round - nothing may offer
        /// her, because a selector row makes her PICKABLE and composing her match would throw for want
        /// of a registered printed set - and the dealing is exactly the edit that makes both halves
        /// true at once, which is Silverarrow's 8.35z46 law: the registration flip and the
        /// staged-configuration factories land together.
        /// </summary>
        [Test]
        public void EveryHalfOfHerDealing_IsReachableFromThisBuildsRoster()
        {
            Assert.That(
                QuickGameSetupConfiguration.DealtHeroes.Any(hero =>
                    hero.ContentId.Value.Equals(
                        RowennaCardContent.ContentSetId, StringComparison.Ordinal)),
                Is.True,
                "A selector row makes her pickable, and 8.57g is the milestone that adds one.");

            var hand = MatchConfiguration.CreateVerifiedHandForInternalTest(
                QuickGameSetupConfiguration.RowennaHeroIdValue, "rowenna-prefix");
            Assert.That(hand, Is.Not.Null);
            Assert.That(hand.Definitions, Has.Count.EqualTo(18),
                "and the one switch that hands a seat a verified printed set names her arm, so the " +
                "roster row and the hand factory are the two halves of one claim rather than one " +
                "half of a pick that throws at composition.");
            Assert.That(
                hand.Definitions.Select(definition => definition.Id),
                Is.EquivalentTo(RowennaCardContent.DealtDefinitions
                    .Select(definition => definition.Id)));
        }

        /// <summary>
        /// The two capability OUTPUTS this block added are declared on the capabilities that already
        /// answered the same facts, rather than as capabilities of their own - and both are optional,
        /// so every body composed before this milestone publishes exactly what it published.
        /// </summary>
        [Test]
        public void TheTwoAddedOutputs_AreDeclaredOnTheCapabilitiesThatAlreadyAnsweredTheirFacts()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;

            var attack = catalog.Load(SyntheticConsequenceCapabilities.AttackTookAHeroV1);
            Assert.That(
                attack.Schema.Outputs.Select(output => output.OutputName),
                Is.EquivalentTo(new[]
                {
                    SyntheticConsequenceCapabilities.TookAHeroOutput,
                    SyntheticConsequenceCapabilities.AttackTargetHeroOutput
                }),
                "Whether the Attack took a hero and which hero it took are one fact read two ways.");

            var grant = catalog.Load(SyntheticRosterCapabilities.GainResourceV1);
            Assert.That(
                grant.Schema.Outputs.Select(output => output.OutputName),
                Is.EquivalentTo(new[]
                {
                    "recipient", "amount", SyntheticRosterCapabilities.DidGrantOutputName
                }),
                "and 'if they do' about a coin is the count form of the grant, exactly as " +
                "milestone 8.40i's didRetrieve is for a retrieval.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static int Icon(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount)
                .SingleOrDefault();

        private static string[] CardIds(PendingEffectChoice choice) =>
            choice.Candidates
                .Select(candidate => candidate.Value.CardInstanceId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static string[] HeroIds(PendingEffectChoice choice) =>
            choice.Candidates
                .Select(candidate => candidate.Value.HeroId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, string cardInstanceId) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                invocation.Candidate.Value.CardInstanceId.Value == cardInstanceId);

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HeroId hero) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero));

        /// <summary>
        /// One staged position and the frame walking one of Rowenna's bodies over it.
        ///
        /// <para>It holds the live card instances, the coin ledger and the board, and rebuilds the
        /// trusted content on demand so a case can ask what a restore would ask. Nothing here is a
        /// configured match: see the fixture's own remarks for why, and for who owns the part this
        /// cannot reach.</para>
        /// </summary>
        private sealed class World
        {
            private readonly CardDefinitionId cardId;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly HeroCoinLedger ledger;
            private readonly MatchComponents components;
            private readonly EnclosingAttackFacts enclosingAttack;
            private readonly RepeatContinuation repeat;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private World(
                CardDefinitionId cardId,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                MatchParticipants participants,
                HeroCoinLedger ledger,
                MatchComponents components,
                EnclosingAttackFacts enclosingAttack,
                RepeatContinuation repeat,
                MatchScoreboard scoreboard)
            {
                this.cardId = cardId;
                this.definitions = definitions;
                this.instances = instances;
                this.participants = participants;
                this.ledger = ledger;
                this.components = components;
                this.enclosingAttack = enclosingAttack;
                this.repeat = repeat;
                Scoreboard = scoreboard;
                context = BuildContext();
            }

            internal MatchScoreboard Scoreboard { get; }

            internal static World WithHeroTarget(
                CardDefinitionId cardId,
                HexCoordinate? allyAt = null,
                int rowennaDiscards = 2,
                int allyDiscards = 2,
                int repeatsCompleted = 0,
                bool withScoreboard = false) =>
                Create(
                    cardId, allyAt ?? FarAway, rowennaDiscards, allyDiscards, repeatsCompleted,
                    withScoreboard,
                    actor => new EnclosingAttackFacts(actor, Enemy, AttackRangeKind.NonRanged));

            internal static World WithMinionTarget(
                CardDefinitionId cardId,
                HexCoordinate? allyAt = null) =>
                Create(
                    cardId, allyAt ?? FarAway, 2, 2, 0, false,
                    actor => new EnclosingAttackFacts(
                        actor, default, SomeMinion, AttackRangeKind.NonRanged, null));

            private static World Create(
                CardDefinitionId cardId,
                HexCoordinate allyAt,
                int rowennaDiscards,
                int allyDiscards,
                int repeatsCompleted,
                bool withScoreboard,
                Func<HeroId, EnclosingAttackFacts> attack)
            {
                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                var ledger = new HeroCoinLedger(participants);
                MatchScoreboard scoreboard = null;
                if (withScoreboard)
                {
                    scoreboard = new MatchScoreboard(participants);
                    ledger.BindScoreboard(scoreboard);
                }

                var pile = PileDefinition();
                var definitions = RowennaCardContent.AllDefinitions
                    .Concat(new[] { pile })
                    .ToArray();

                var instances = new List<CardInstance>();
                AddDiscards(instances, pile, Rowenna, "rowenna-discard", rowennaDiscards);
                AddDiscards(instances, pile, Ally, "ally-discard", allyDiscards);
                AddDiscards(instances, pile, Enemy, "enemy-discard", 1);

                return new World(
                    cardId,
                    definitions,
                    instances,
                    participants,
                    ledger,
                    Components(allyAt),
                    attack(Rowenna),
                    repeatsCompleted == 0
                        ? RepeatContinuation.FirstInstance
                        : RepeatContinuation.Next(repeatsCompleted),
                    scoreboard);
            }

            private static void AddDiscards(
                ICollection<CardInstance> into,
                CardDefinition definition,
                HeroId owner,
                string prefix,
                int count)
            {
                for (var index = 0; index < count; index++)
                {
                    into.Add(new CardInstance(
                        new CardInstanceId($"{prefix}-{(char)('a' + index)}"),
                        definition.Reference,
                        owner,
                        CardStateFacts.DiscardedUnplayed()));
                }
            }

            /// <summary>
            /// A definition for the cards sitting in the three piles. It is a synthetic fixture card
            /// rather than one of Rowenna's, because what these cases are about is who may take a card
            /// back and whose pile it came from - not which face it is. A Movement primary, because a
            /// card with no text is the plainest thing a pile can hold.
            /// </summary>
            private static CardDefinition PileDefinition() =>
                new CardDefinition(
                    new CardDefinitionId("rowenna-reward-pile-card"),
                    "rev-1",
                    "TEST - Rowenna Reward Pile Card",
                    new InitiativeValue(7),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    new[]
                    {
                        new ActionDefinition(
                            new ActionDefinitionId("pile-movement"),
                            ActionKind.Movement,
                            ActionDefinitionOrigin.PrintedPrimary,
                            new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Movement, 2)
                            },
                            new ActionDefinitionPolicy(
                                CardTextApplicationPolicy.Applies, false, true))
                    });

            private static MatchComponents Components(HexCoordinate allyAt)
            {
                var zone = new ZoneId("rowenna-reward-zone");
                var spaces = new List<BoardMapSpace>();
                var index = 0;
                for (var q = -6; q <= 6; q++)
                {
                    for (var r = -6; r <= 6; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, RowennaAt) > 6)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            new SpaceId($"s{index++}"), coordinate,
                            BoardMapSpaceKind.Playable, zone));
                    }
                }

                var map = new BoardMapDefinition(
                    new MapId("rowenna-reward-map"), 1, "Rowenna Reward Map",
                    spaces,
                    new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());

                var board = new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(Ally, allyAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                return new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board);
            }

            /// <summary>
            /// Rebuilds everything the catalog and the content own, keeping only the frame - which is
            /// what a snapshot carries and what a restore has to be able to resume from.
            /// </summary>
            internal void RebindTrustedContent() => context = BuildContext();

            private EffectResolutionContext BuildContext()
            {
                var definition = RowennaEffectContent.AttackTextFor(
                    cardId, RowennaCardContent.ContentRevision);
                return new EffectResolutionContext(
                    definition,
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Attack,
                    components: components,
                    enclosingAttack: enclosingAttack,
                    rosterState: EffectRosterState.Of(
                        Array.Empty<RoundTurnSlot>(),
                        new RoundHeroDefeatHistory(participants),
                        ledger));
            }

            internal EffectResolutionStepResult Run()
            {
                var definition = RowennaCardContent.Definition(cardId);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("rowenna-reward-resolution"),
                    new CardInstanceId("rowenna-reward-source"),
                    definition.Reference,
                    RowennaCardContent.PrimaryAttack,
                    Rowenna,
                    Rowenna,
                    repeat.CompletedRepeats == 0
                        ? ActionInvocationContext.PrintedPrimary
                        : ActionInvocationContext.Repeated,
                    context,
                    repeat);

                Assert.That(result.Accepted, Is.True, "The walk must start.");
                frame = result.Frame;
                return result;
            }

            internal PendingEffectChoice RunToPause()
            {
                var result = Run();
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal PendingEffectChoice Answer(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.False, "This case expects another question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult AnswerToCompletion(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                return result;
            }

            internal EffectResolutionStepResult Submit(EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                if (result.Accepted)
                {
                    frame = result.Frame;
                }

                return result;
            }

            internal bool InHand(string cardInstanceId) =>
                instances.Single(card => card.Id.Value == cardInstanceId).State.Location ==
                CardLocation.Hand;

            internal int Coins(HeroId hero) => ledger.GetCoins(hero);

            internal int CoinsEarned(HeroId hero) =>
                Scoreboard.Entries.Single(entry => entry.HeroId.Equals(hero)).CoinsEarned;
        }
    }
}
