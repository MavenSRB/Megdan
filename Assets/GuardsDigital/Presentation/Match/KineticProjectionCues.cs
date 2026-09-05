using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z11. The half of the motion language the MATCH LOG cannot supply, derived
    /// from projection deltas instead - and the reason it has to exist is worth recording, because it
    /// contradicts an assumption the amendment was written on.
    ///
    /// <para><b>THE FINDING: a facedown commitment is excluded from the log by contract, so the
    /// committed card's arc cannot be a log-derived cue.</b> <c>MatchLogEntryKind</c>'s own remarks name
    /// "a facedown commitment" in the list of things no member describes ("What is absent is the
    /// point"), and that exclusion is deliberate and load-bearing: it is why every seat's log is the
    /// same log and why its seat filter is an identity rather than a redaction. Principle 1's very
    /// first example - "committed cards ARC hand to facedown slot" - therefore had no source in the one
    /// stream milestone 8.35y derives from. Adding a commitment row to the log would put a hand's
    /// movements on a wire that has never carried one, for a flourish. So the arc is derived HERE, from
    /// the viewer's own projection, where the fact already legitimately lives.</para>
    ///
    /// <para><b>What that costs, stated honestly.</b> A log-derived cue is identical on every client by
    /// construction; a projection-derived one is identical on every client that can SEE the fact. This
    /// watch fires for exactly the commitments the viewer's own seat projection reports, which is the
    /// correct rule rather than a weaker one - a player who cannot see that a card was placed should
    /// not watch it fly. The cue it builds carries no card identity at all
    /// (<see cref="KineticCue.CardInstanceId"/> refuses one for a commit), so the flourish reveals
    /// nothing the projection did not already show.</para>
    ///
    /// <para>Coins and the end-of-round retrieval are here for a plainer reason: the log records
    /// <c>PityCoinCollected</c> and <c>RoundCompleted</c>, but coins also arrive from p.13 defeats and
    /// the retrieval is a STAGE rather than an event. The projection carries both completely -
    /// <c>HeroProgression[h].Coins</c> and <c>CurrentEndOfRoundStage</c> - so one derivation covers
    /// every source instead of a per-source arm that silently misses one.</para>
    /// </summary>
    public sealed class KineticProjectionWatch
    {
        /// <summary>
        /// The most coin ghosts one arrival flies, however many coins arrived. Six discs read as "a
        /// pile arrived"; twenty read as a swarm and cost twenty ghosts on the overlay.
        /// </summary>
        public const int MaxCoinGhosts = 6;

        private readonly Dictionary<HeroId, TurnCardCommitmentStatus> lastStatus =
            new Dictionary<HeroId, TurnCardCommitmentStatus>();
        private readonly Dictionary<HeroId, int> lastCoins = new Dictionary<HeroId, int>();
        private EndOfRoundStage? lastStage;
        private bool everObserved;

        /// <summary>
        /// Whether the most recent observation primed rather than emitting - a first sight, or a caller
        /// that told it the story went backwards.
        /// </summary>
        public bool PrimedOnLastObservation { get; private set; }

        /// <summary>
        /// The scenes this projection's changes call for, or none.
        ///
        /// <para><paramref name="snap"/> is the log stream's own priming signal, passed straight
        /// through: the two derivations must re-prime TOGETHER, or an undo would silence the log's
        /// cues while the projection watch happily animated six cards flying back into hands that had
        /// just been rewound.</para>
        /// </summary>
        public IReadOnlyList<KineticScene> Observe(IMatchReadModel projection, int sequenceFloor, bool snap)
        {
            if (projection == null)
            {
                PrimedOnLastObservation = false;
                return Array.Empty<KineticScene>();
            }

            HeroId[] heroIds;
            var heroes = projection.Heroes;
            if (heroes == null)
            {
                heroIds = Array.Empty<HeroId>();
            }
            else
            {
                heroIds = new HeroId[heroes.Count];
                for (var index = 0; index < heroes.Count; index++)
                {
                    heroIds[index] = heroes[index].HeroId;
                }
            }

            return Observe(
                projection.Commitments,
                projection.HeroProgression,
                projection.CurrentEndOfRoundStage,
                heroIds,
                sequenceFloor,
                snap);
        }

        /// <summary>
        /// The same observation, taking only the four facts it actually reads.
        ///
        /// <para>It is a separate entry point for the reason milestone 8.35y gave when it split
        /// <see cref="KineticCueStream"/> out of its presenter: every rule that could get this wrong -
        /// prime on first sight, re-prime with the log, emit on a TRANSITION and never on a state -
        /// lives here, and a case about those rules should not have to stand up an
        /// <see cref="IMatchReadModel"/> with its sixty-odd members to ask the question. The overload
        /// above is the adapter, and it is deliberately the only thing in this file that knows the
        /// interface exists.</para>
        /// </summary>
        public IReadOnlyList<KineticScene> Observe(
            IReadOnlyList<MatchCommitmentProjection> commitments,
            IReadOnlyList<HeroProgressionProjection> progression,
            EndOfRoundStage? endOfRoundStage,
            IReadOnlyList<HeroId> heroIds,
            int sequenceFloor,
            bool snap)
        {
            PrimedOnLastObservation = false;
            var order = sequenceFloor < 1 ? 1 : sequenceFloor;
            if (snap || !everObserved)
            {
                PrimedOnLastObservation = true;
                everObserved = true;
                Remember(commitments, progression, endOfRoundStage);
                return Array.Empty<KineticScene>();
            }

            var scenes = new List<KineticScene>();
            AppendCommitments(scenes, commitments, order);
            AppendCoins(scenes, progression, order);
            AppendRetrieval(scenes, endOfRoundStage, heroIds, order);
            Remember(commitments, progression, endOfRoundStage);

            return scenes.Count == 0
                ? Array.Empty<KineticScene>()
                : new ReadOnlyCollection<KineticScene>(scenes.ToArray());
        }

        /// <summary>Forgets everything, so the next observation primes. Used when a presenter is rebound.</summary>
        public void Reset()
        {
            lastStatus.Clear();
            lastCoins.Clear();
            lastStage = null;
            everObserved = false;
        }

        private void AppendCommitments(
            List<KineticScene> scenes, IReadOnlyList<MatchCommitmentProjection> commitments, int order)
        {
            if (commitments == null)
            {
                return;
            }

            for (var index = 0; index < commitments.Count; index++)
            {
                var commitment = commitments[index];
                if (!commitment.HeroId.IsDefined ||
                    commitment.Status != TurnCardCommitmentStatus.CardCommittedFacedown)
                {
                    continue;
                }

                if (lastStatus.TryGetValue(commitment.HeroId, out var previous) &&
                    previous == TurnCardCommitmentStatus.CardCommittedFacedown)
                {
                    // Already committed when we last looked. A projection change for some unrelated
                    // reason must not re-fly a card that has been sitting in its slot for a minute.
                    continue;
                }

                scenes.Add(new KineticScene(
                    order,
                    KineticSceneKind.Commitment,
                    new[]
                    {
                        new KineticCue(
                            order,
                            KineticCueKind.CardCommit,
                            KineticSubject.ForHero(commitment.HeroId))
                    },
                    label: "commit:" + commitment.HeroId));
            }
        }

        private void AppendCoins(
            List<KineticScene> scenes, IReadOnlyList<HeroProgressionProjection> progression, int order)
        {
            if (progression == null)
            {
                return;
            }

            for (var index = 0; index < progression.Count; index++)
            {
                var hero = progression[index];
                if (hero == null || !hero.HeroId.IsDefined ||
                    !lastCoins.TryGetValue(hero.HeroId, out var previous))
                {
                    continue;
                }

                var gained = hero.Coins - previous;
                if (gained <= 0)
                {
                    // Coins LEAVING a hero is a Level being bought, and that journey is the level-up
                    // pop the log already reports. Two cues for one purchase would read as two events.
                    continue;
                }

                var ghosts = gained > MaxCoinGhosts ? MaxCoinGhosts : gained;
                scenes.Add(new KineticScene(
                    order,
                    KineticSceneKind.Economy,
                    new[]
                    {
                        new KineticCue(
                            order,
                            KineticCueKind.CoinFly,
                            KineticSubject.ForHero(hero.HeroId),
                            amount: ghosts)
                    },
                    label: "coins:" + hero.HeroId + "+" + gained));
            }
        }

        private void AppendRetrieval(
            List<KineticScene> scenes, EndOfRoundStage? stage, IReadOnlyList<HeroId> heroes, int order)
        {
            var entered =
                stage == EndOfRoundStage.RetrieveCardsAndCancelActiveEffects &&
                lastStage != EndOfRoundStage.RetrieveCardsAndCancelActiveEffects;
            if (!entered || heroes == null)
            {
                return;
            }

            // One scene per hero rather than one scene for the table: the retrieval is six separate
            // journeys and the conductor stages them, which is what turns "all the cards blink back"
            // into something a player can follow.
            for (var index = 0; index < heroes.Count; index++)
            {
                var heroId = heroes[index];
                if (!heroId.IsDefined)
                {
                    continue;
                }

                scenes.Add(new KineticScene(
                    order,
                    KineticSceneKind.Commitment,
                    new[]
                    {
                        new KineticCue(
                            order,
                            KineticCueKind.CardRetrieve,
                            KineticSubject.ForHero(heroId))
                    },
                    label: "retrieve:" + heroId));
            }
        }

        private void Remember(
            IReadOnlyList<MatchCommitmentProjection> commitments,
            IReadOnlyList<HeroProgressionProjection> progression,
            EndOfRoundStage? endOfRoundStage)
        {
            lastStatus.Clear();
            if (commitments != null)
            {
                for (var index = 0; index < commitments.Count; index++)
                {
                    if (commitments[index].HeroId.IsDefined)
                    {
                        lastStatus[commitments[index].HeroId] = commitments[index].Status;
                    }
                }
            }

            lastCoins.Clear();
            if (progression != null)
            {
                for (var index = 0; index < progression.Count; index++)
                {
                    var hero = progression[index];
                    if (hero != null && hero.HeroId.IsDefined)
                    {
                        lastCoins[hero.HeroId] = hero.Coins;
                    }
                }
            }

            lastStage = endOfRoundStage;
        }
    }
}
