using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z11 (the TWENTIETH AMENDMENT's principle 1: NOTHING TELEPORTS). One case per
    /// journey the motion pass added - the committed card's arc, the reveal's flip, the round-end
    /// retrieval, the coins, the level-up - plus the rules of the second derivation those journeys
    /// forced into existence.
    ///
    /// <para>The board's five kinetics are <see cref="KineticCueTests"/>'s; these are the CARDS AND
    /// COINS, which until this milestone appeared and disappeared instantly.</para>
    /// </summary>
    [TestFixture]
    public sealed class KineticMotionVocabularyTests
    {
        private static readonly HeroId First = new HeroId("red-hero-01");
        private static readonly HeroId Second = new HeroId("blue-hero-01");
        private static readonly HeroId Third = new HeroId("red-hero-02");
        private static readonly CardInstanceId ACard = new CardInstanceId("card-instance-1");
        private static readonly CardInstanceId AnotherCard = new CardInstanceId("card-instance-2");

        private static MatchLogEntry Row(
            int sequence,
            MatchLogEntryKind kind,
            HeroId subjectHero = default,
            CardInstanceId card = default,
            int amount = 0) =>
            new MatchLogEntry(
                sequence, kind, 1, 1,
                subjectHeroId: subjectHero,
                subjectCardInstanceId: card,
                amount: amount);

        private static KineticCueStream PrimedStream()
        {
            var stream = new KineticCueStream();
            stream.Observe(Array.Empty<MatchLogEntry>());
            return stream;
        }

        private static IReadOnlyList<KineticScene> ScenesFor(params MatchLogEntry[] rows) =>
            PrimedStream().ObserveScenes(rows);

        // ---- The log-derived half.

        [Test]
        public void ARevealedCommitment_Flips_AndNamesTheCardItTurnedOver()
        {
            var scenes = ScenesFor(Row(4, MatchLogEntryKind.CardRevealed, First, ACard));

            Assert.That(scenes.Count, Is.EqualTo(1));
            Assert.That(scenes[0].Kind, Is.EqualTo(KineticSceneKind.Reveal));
            Assert.That(scenes[0].Cues[0].Kind, Is.EqualTo(KineticCueKind.CardReveal));
            Assert.That(scenes[0].Cues[0].Subject.HeroId, Is.EqualTo(First));
            Assert.That(scenes[0].Cues[0].CardInstanceId, Is.EqualTo(ACard),
                "A reveal is public by definition, and a flip has to know what it turned over.");
        }

        [Test]
        public void ARevealRowWithNoCard_DrawsNothing()
        {
            Assert.That(
                ScenesFor(Row(4, MatchLogEntryKind.CardRevealed, First)).Count, Is.EqualTo(0),
                "A reveal that names no card describes nothing a flip could show.");
        }

        [Test]
        public void ALevelPurchase_PopsOnTheRail_CarryingTheLevelReached()
        {
            var scenes = ScenesFor(Row(9, MatchLogEntryKind.LevelPurchased, Second, amount: 4));

            Assert.That(scenes.Count, Is.EqualTo(1));
            Assert.That(scenes[0].Kind, Is.EqualTo(KineticSceneKind.Progression));
            Assert.That(scenes[0].Cues[0].Kind, Is.EqualTo(KineticCueKind.LevelPop));
            Assert.That(scenes[0].Cues[0].Amount, Is.EqualTo(4));
        }

        /// <summary>
        /// Milestone 8.35u levels every hero AT ONCE, so this row arrives in batches of up to six. Six
        /// simultaneous pops is exactly the "simultaneous soup" principle 3 was written against.
        /// </summary>
        [Test]
        public void SixHeroesLevellingTogether_AreSixSeparateMoments()
        {
            var scenes = ScenesFor(
                Row(11, MatchLogEntryKind.LevelPurchased, First, amount: 2),
                Row(12, MatchLogEntryKind.LevelPurchased, Second, amount: 3),
                Row(13, MatchLogEntryKind.LevelPurchased, Third, amount: 2));

            Assert.That(scenes.Count, Is.EqualTo(3));
            foreach (var scene in scenes)
            {
                Assert.That(scene.Cues.Count, Is.EqualTo(1),
                    "One hero's level-up is one moment; staging them is what makes them readable.");
            }
        }

        // ---- Principle 4: staged reveals in initiative order.

        /// <summary>
        /// THE ORDERING FINDING, pinned. <c>TurnCardCommitmentAuthority.CompleteRevealIfReady</c> walks
        /// <c>expectedHeroIds</c>, and <c>RoundTurnSlot</c> builds that list by ORDINAL HERO-ID SORT.
        /// So the log's own order for a reveal batch is alphabetical by identifier, which is stable and
        /// completely meaningless to watch. Staging by sequence alone would have satisfied the letter
        /// of "staged reveals" and none of the point.
        ///
        /// <para>With no projection to read initiative from, the ranking degrades to arrival order
        /// rather than to a guess - which is this case's other half.</para>
        /// </summary>
        [Test]
        public void WithNoProjection_RevealsKeepTheirArrivalOrder_RatherThanGuessing()
        {
            var scenes = ScenesFor(
                Row(20, MatchLogEntryKind.CardRevealed, First, ACard),
                Row(21, MatchLogEntryKind.CardRevealed, Second, AnotherCard));

            Assert.That(scenes.Count, Is.EqualTo(2));
            Assert.That(scenes[0].Cues[0].Subject.HeroId, Is.EqualTo(First));
            Assert.That(scenes[1].Cues[0].Subject.HeroId, Is.EqualTo(Second));
            Assert.That(scenes[0].Order, Is.LessThan(scenes[1].Order),
                "Unrankable reveals still stage in a deterministic order, so two clients agree.");
        }

        /// <summary>
        /// The ranking's main path: highest printed Initiative turns over FIRST, which is the direction
        /// <c>InitiativeResolutionAuthority</c> reads (it takes <c>candidates.Max(InitiativeTotal)</c>).
        /// The arrival order here is deliberately the reverse of the ranks, so a no-op implementation
        /// would fail.
        /// </summary>
        [Test]
        public void RevealsAreStagedHighestInitiativeFirst_NotInTheOrderTheEngineEmitsThem()
        {
            var initiatives = new Dictionary<CardInstanceId, int>
            {
                { new CardInstanceId("slow"), 2 },
                { new CardInstanceId("middle"), 5 },
                { new CardInstanceId("fast"), 9 }
            };

            var scenes = new List<KineticScene>
            {
                Reveal(40, First, new CardInstanceId("slow")),
                Reveal(41, Second, new CardInstanceId("fast")),
                Reveal(42, Third, new CardInstanceId("middle"))
            };

            KineticCueStream.RankRevealsByInitiative(
                scenes, card => initiatives.TryGetValue(card, out var value) ? value : int.MinValue);

            var order = scenes.OrderBy(scene => scene.Order)
                .Select(scene => scene.Cues[0].CardInstanceId.Value)
                .ToArray();

            Assert.That(order, Is.EqualTo(new[] { "fast", "middle", "slow" }),
                "The engine emits reveals in ordinal hero-id order, which is meaningless to watch.");
        }

        [Test]
        public void RevealsThatRankEqual_KeepTheirArrivalOrder()
        {
            var scenes = new List<KineticScene>
            {
                Reveal(50, First, new CardInstanceId("a")),
                Reveal(51, Second, new CardInstanceId("b")),
                Reveal(52, Third, new CardInstanceId("c"))
            };

            KineticCueStream.RankRevealsByInitiative(scenes, _ => 4);

            var order = scenes.OrderBy(scene => scene.Order)
                .Select(scene => scene.Cues[0].CardInstanceId.Value)
                .ToArray();

            Assert.That(order, Is.EqualTo(new[] { "a", "b", "c" }),
                "Two cards printing the same Initiative must break their tie identically on every " +
                "client, or two players watch a different story.");
        }

        /// <summary>
        /// p.16's Ultimate prints no Initiative at all, and a card the catalogue cannot resolve reads
        /// as nothing either. Both sort last and keep their arrival order rather than being guessed at.
        /// </summary>
        [Test]
        public void ARevealWhoseInitiativeCannotBeRead_SortsLastRatherThanBeingGuessed()
        {
            var scenes = new List<KineticScene>
            {
                Reveal(60, First, new CardInstanceId("unreadable")),
                Reveal(61, Second, new CardInstanceId("readable"))
            };

            KineticCueStream.RankRevealsByInitiative(
                scenes, card => card.Value == "readable" ? 6 : int.MinValue);

            var order = scenes.OrderBy(scene => scene.Order)
                .Select(scene => scene.Cues[0].CardInstanceId.Value)
                .ToArray();

            Assert.That(order, Is.EqualTo(new[] { "readable", "unreadable" }));
        }

        private static KineticScene Reveal(int order, HeroId hero, CardInstanceId card) =>
            new KineticScene(
                order,
                KineticSceneKind.Reveal,
                new[]
                {
                    new KineticCue(
                        order, KineticCueKind.CardReveal, KineticSubject.ForHero(hero),
                        cardInstanceId: card)
                });

        [Test]
        public void ARevealBatchIsRankedContiguously_SoItCannotJumpPastANeighbouringMoment()
        {
            var scenes = ScenesFor(
                Row(30, MatchLogEntryKind.CardRevealed, First, ACard),
                Row(31, MatchLogEntryKind.CardRevealed, Second, AnotherCard),
                Row(32, MatchLogEntryKind.LevelPurchased, Third, amount: 5));

            var reveals = scenes.Where(s => s.Kind == KineticSceneKind.Reveal).ToArray();
            var level = scenes.Single(s => s.Kind == KineticSceneKind.Progression);

            foreach (var reveal in reveals)
            {
                Assert.That(reveal.Order, Is.LessThan(level.Order),
                    "Re-ranking a reveal batch must stay inside the span the batch already occupied.");
            }
        }

        // ---- The projection-derived half, and the finding that forced it.

        private static MatchCommitmentProjection Commitment(HeroId hero, TurnCardCommitmentStatus status) =>
            new MatchCommitmentProjection(hero, status, false, null);

        private static HeroProgressionProjection Progress(HeroId hero, int coins, int level = 1) =>
            new HeroProgressionProjection(
                hero, level, coins, false, Array.Empty<KeyValuePair<PrintedValueChannel, int>>());

        private static IReadOnlyList<KineticScene> Watch(
            KineticProjectionWatch watch,
            IReadOnlyList<MatchCommitmentProjection> commitments = null,
            IReadOnlyList<HeroProgressionProjection> progression = null,
            EndOfRoundStage? stage = null,
            IReadOnlyList<HeroId> heroes = null,
            bool snap = false) =>
            watch.Observe(
                commitments ?? Array.Empty<MatchCommitmentProjection>(),
                progression ?? Array.Empty<HeroProgressionProjection>(),
                stage,
                heroes ?? Array.Empty<HeroId>(),
                7,
                snap);

        /// <summary>
        /// THE FINDING: <c>MatchLogEntryKind</c> names "a facedown commitment" in its own list of
        /// exclusions, so principle 1's very first example had no source in the one stream milestone
        /// 8.35y derives from. The arc is a projection delta instead.
        /// </summary>
        [Test]
        public void ACardGoingFacedown_Arcs_AndTheCueRefusesToNameIt()
        {
            var watch = new KineticProjectionWatch();
            Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.AwaitingResponse) });

            var scenes = Watch(
                watch, new[] { Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown) });

            Assert.That(scenes.Count, Is.EqualTo(1));
            Assert.That(scenes[0].Kind, Is.EqualTo(KineticSceneKind.Commitment));
            Assert.That(scenes[0].Cues[0].Kind, Is.EqualTo(KineticCueKind.CardCommit));
            Assert.That(scenes[0].Cues[0].CardInstanceId.IsDefined, Is.False,
                "A commitment travels facedown. Naming the card would put a hand into a cue.");
        }

        /// <summary>The privacy rule is a constructor invariant, not a convention downstream code follows.</summary>
        [Test]
        public void ACommitCueCannotBeBuiltCarryingACard_AndARevealCannotBeBuiltWithout()
        {
            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.CardCommit, KineticSubject.ForHero(First), cardInstanceId: ACard));

            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.CardReveal, KineticSubject.ForHero(First)));

            Assert.Throws<ArgumentOutOfRangeException>(() => new KineticCue(
                1, KineticCueKind.CoinFly, KineticSubject.ForHero(First)));

            Assert.Throws<ArgumentOutOfRangeException>(() => new KineticCue(
                1, KineticCueKind.LevelPop, KineticSubject.ForHero(First), amount: 0));
        }

        [Test]
        public void ACardAlreadySittingInItsSlot_DoesNotReFly()
        {
            var watch = new KineticProjectionWatch();
            Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.AwaitingResponse) });
            Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown) });

            var again = Watch(
                watch, new[] { Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown) });

            Assert.That(again.Count, Is.EqualTo(0),
                "A projection change for an unrelated reason must not re-fly a card that has been " +
                "sitting in its slot for a minute.");
        }

        [Test]
        public void APassIsNotACommitment_AndARevealIsNotOneEither()
        {
            var watch = new KineticProjectionWatch();
            Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.AwaitingResponse) });

            Assert.That(
                Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.Passed) }).Count,
                Is.EqualTo(0),
                "A hero who passed placed no card, so nothing travelled.");

            Assert.That(
                Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.Revealed) }).Count,
                Is.EqualTo(0),
                "The flip is the log's CardRevealed row. Two cues for one turn-over would be two events.");
        }

        [Test]
        public void CoinsArriving_Fly_AndCoinsBeingSpentDoNot()
        {
            var watch = new KineticProjectionWatch();
            Watch(watch, progression: new[] { Progress(First, 2) });

            var gained = Watch(watch, progression: new[] { Progress(First, 5) });
            Assert.That(gained.Count, Is.EqualTo(1));
            Assert.That(gained[0].Kind, Is.EqualTo(KineticSceneKind.Economy));
            Assert.That(gained[0].Cues[0].Kind, Is.EqualTo(KineticCueKind.CoinFly));
            Assert.That(gained[0].Cues[0].Amount, Is.EqualTo(3));

            var spent = Watch(watch, progression: new[] { Progress(First, 1) });
            Assert.That(spent.Count, Is.EqualTo(0),
                "Coins LEAVING a hero is a Level being bought, and that is the level-up pop's story.");
        }

        [Test]
        public void AVeryLargeCoinHaul_FliesABoundedNumberOfDiscs()
        {
            var watch = new KineticProjectionWatch();
            Watch(watch, progression: new[] { Progress(First, 0) });

            var scenes = Watch(watch, progression: new[] { Progress(First, 40) });

            Assert.That(
                scenes[0].Cues[0].Amount, Is.EqualTo(KineticProjectionWatch.MaxCoinGhosts),
                "Six discs read as a pile arriving; forty read as a swarm and cost forty ghosts.");
        }

        [Test]
        public void EnteringTheRetrievalStage_SendsEveryHerosCardBack_OneAtATime()
        {
            var watch = new KineticProjectionWatch();
            var heroes = new[] { First, Second, Third };
            Watch(watch, stage: EndOfRoundStage.ResolveRequestedLanePush, heroes: heroes);

            var scenes = Watch(
                watch,
                stage: EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
                heroes: heroes);

            Assert.That(scenes.Count, Is.EqualTo(3),
                "The retrieval is three separate journeys, and the conductor stages them.");
            foreach (var scene in scenes)
            {
                Assert.That(scene.Cues[0].Kind, Is.EqualTo(KineticCueKind.CardRetrieve));
            }

            var still = Watch(
                watch,
                stage: EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
                heroes: heroes);
            Assert.That(still.Count, Is.EqualTo(0),
                "Sitting in the stage is not entering it; only the transition is an event.");
        }

        // ---- The rules the second derivation has to share with the first.

        [Test]
        public void TheFirstSightOfAMatch_PrimesAndPlaysNothing()
        {
            var watch = new KineticProjectionWatch();

            var scenes = Watch(
                watch,
                new[] { Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown) },
                new[] { Progress(First, 9) },
                EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
                new[] { First });

            Assert.That(scenes.Count, Is.EqualTo(0),
                "A client joining a match in progress must not watch the whole round replay at once.");
            Assert.That(watch.PrimedOnLastObservation, Is.True);
        }

        /// <summary>
        /// The two derivations must re-prime TOGETHER. Silencing the log's cues on an undo while the
        /// projection watch happily animated six cards flying back into hands that had just been
        /// rewound is the exact failure this shared signal exists to prevent.
        /// </summary>
        [Test]
        public void ASnapRePrimesTheWatchToo_SoARewindSilencesBothDerivations()
        {
            var watch = new KineticProjectionWatch();
            Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.AwaitingResponse) });

            var scenes = Watch(
                watch,
                new[] { Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown) },
                snap: true);

            Assert.That(scenes.Count, Is.EqualTo(0),
                "An undo plays nothing; the board and the HUD both snap.");
            Assert.That(watch.PrimedOnLastObservation, Is.True);

            // And the re-primed state is the state it snapped to, so nothing replays afterwards.
            Assert.That(
                Watch(watch, new[] { Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown) })
                    .Count,
                Is.EqualTo(0));
        }

        [Test]
        public void TwoWatchesReadingTheSameProjections_PlayTheSameStory()
        {
            var left = new KineticProjectionWatch();
            var right = new KineticProjectionWatch();

            var before = new[]
            {
                Commitment(First, TurnCardCommitmentStatus.AwaitingResponse),
                Commitment(Second, TurnCardCommitmentStatus.AwaitingResponse)
            };
            var after = new[]
            {
                Commitment(First, TurnCardCommitmentStatus.CardCommittedFacedown),
                Commitment(Second, TurnCardCommitmentStatus.CardCommittedFacedown)
            };

            Watch(left, before);
            Watch(right, before);

            var leftScenes = Watch(left, after);
            var rightScenes = Watch(right, after);

            Assert.That(leftScenes.Count, Is.EqualTo(rightScenes.Count));
            for (var index = 0; index < leftScenes.Count; index++)
            {
                Assert.That(
                    leftScenes[index].Cues[0].Subject,
                    Is.EqualTo(rightScenes[index].Cues[0].Subject),
                    "Two clients watching the same commitments must watch them in the same order.");
            }
        }
    }
}
