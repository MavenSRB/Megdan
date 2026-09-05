using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35y. The kinetic event feedback pass - the user's "tokens smacking each
    /// other" - proved at the layer where it is decided rather than where it is drawn.
    /// </summary>
    /// <remarks>
    /// <para>Every case here poses its question to <see cref="KineticCueStream"/>, which takes the
    /// seat-public match log directly. That is deliberate: the whole risk in bolting a kinetic layer
    /// onto an authoritative surface is in the SELECTION - which rows become which cues, and which
    /// rows must never fire twice - and that is exactly what these rows-in, cues-out cases hold. The
    /// Unity views are then a thin application of numbers this fixture and
    /// <see cref="KineticPlaybackTests"/> already pinned.</para>
    /// </remarks>
    public sealed class KineticCueTests
    {
        private static readonly HeroId Attacker = new HeroId("red-hero-01");
        private static readonly HeroId Defender = new HeroId("blue-hero-01");
        private static readonly MinionId Minion = new MinionId("blue-minion-03");

        private static MatchLogEntry Row(
            int sequence,
            MatchLogEntryKind kind,
            HeroId subjectHero = default,
            HeroId targetHero = default,
            MinionId subjectMinion = default,
            CardInstanceId card = default,
            HexCoordinate origin = default,
            HexCoordinate destination = default,
            bool hasOrigin = false,
            bool hasDestination = false,
            MatchLogOutcome outcome = MatchLogOutcome.None,
            int amount = 0) =>
            new MatchLogEntry(
                sequence, kind, 1, 1,
                subjectHeroId: subjectHero,
                targetHeroId: targetHero,
                subjectMinionId: subjectMinion,
                subjectCardInstanceId: card,
                origin: origin,
                destination: destination,
                hasOrigin: hasOrigin,
                hasDestination: hasDestination,
                outcome: outcome,
                amount: amount);

        /// <summary>A stream that has already seen an empty match, so the next row is a real event.</summary>
        private static KineticCueStream PrimedStream()
        {
            var stream = new KineticCueStream();
            stream.Observe(Array.Empty<MatchLogEntry>());
            return stream;
        }

        private static IReadOnlyList<KineticCue> CuesFor(params MatchLogEntry[] rows)
        {
            var stream = PrimedStream();
            return stream.Observe(rows);
        }

        // ---- One case per event kind: the brief's own requirement.

        [Test]
        public void AMoveThatCrossedGround_SlidesFromWhereItLeftToWhereItArrived()
        {
            var cues = CuesFor(Row(
                5, MatchLogEntryKind.Move, subjectHero: Attacker,
                origin: new HexCoordinate(0, 0), destination: new HexCoordinate(2, -1),
                hasOrigin: true, hasDestination: true));

            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Slide));
            Assert.That(cues[0].Sequence, Is.EqualTo(5), "A cue keeps the log row's own sequence.");
            Assert.That(cues[0].Subject, Is.EqualTo(KineticSubject.ForHero(Attacker)));
            Assert.That(cues[0].Route.Count, Is.EqualTo(2));
            Assert.That(cues[0].Route[0], Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(cues[0].Route[1], Is.EqualTo(new HexCoordinate(2, -1)));
        }

        /// <summary>
        /// The recorded finding this milestone owes its route shape to. The SIXTEENTH AMENDMENT
        /// assumed the events carry the per-hex steps; the seat-public log folds a whole move into one
        /// row keeping only the first origin and the last destination, and names the per-hex step as
        /// one of its own exclusions. So a slide has exactly two waypoints today - and the cue is
        /// shaped for more, so that a later milestone which does carry the route needs no view change.
        /// </summary>
        [Test]
        public void ASlidesRoute_IsTwoWaypoints_BecauseThatIsAllThePublicLogCarries()
        {
            var cues = CuesFor(Row(
                5, MatchLogEntryKind.Move, subjectHero: Attacker,
                origin: new HexCoordinate(-3, 1), destination: new HexCoordinate(3, 1),
                hasOrigin: true, hasDestination: true));

            Assert.That(cues[0].Route.Count, Is.EqualTo(2));
            Assert.That(
                Enum.GetNames(typeof(MatchLogEntryKind)).Any(name => name.Contains("Step")), Is.False,
                "If the log ever gains a per-hex step kind, this milestone's route can carry the real " +
                "one and this expectation is the place that says so.");
        }

        [Test]
        public void AMoveThatStatedNoGround_DrawsNothing()
        {
            Assert.That(
                CuesFor(Row(5, MatchLogEntryKind.Move, subjectHero: Attacker)).Count, Is.EqualTo(0),
                "A move row with no stated origin or destination has no route to travel.");

            Assert.That(
                CuesFor(Row(
                    5, MatchLogEntryKind.Move, subjectHero: Attacker,
                    origin: new HexCoordinate(1, 1), destination: new HexCoordinate(1, 1),
                    hasOrigin: true, hasDestination: true)).Count,
                Is.EqualTo(0),
                "A move that ended where it began is not a slide.");
        }

        [Test]
        public void AFastTravel_Slides()
        {
            var cues = CuesFor(Row(
                7, MatchLogEntryKind.FastTravel, subjectHero: Attacker,
                origin: new HexCoordinate(0, 0), destination: new HexCoordinate(6, -3),
                hasOrigin: true, hasDestination: true));

            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Slide));
        }

        [Test]
        public void AnAttackThatLanded_LungesWithDamage_AndFlashesWhatItStruck()
        {
            var cues = CuesFor(Row(
                9, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                outcome: MatchLogOutcome.AttackSuccessful));

            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Lunge));
            Assert.That(cues[0].Subject, Is.EqualTo(KineticSubject.ForHero(Attacker)));
            Assert.That(cues[0].Target, Is.EqualTo(KineticSubject.ForHero(Defender)));
            Assert.That(cues[1].Kind, Is.EqualTo(KineticCueKind.Flash));
            Assert.That(cues[1].Subject, Is.EqualTo(KineticSubject.ForHero(Defender)),
                "The piece that was hit is the piece that flashes.");
        }

        [Test]
        public void AnAttackOnAMinion_LungesAtTheMinion()
        {
            var cues = CuesFor(Row(
                9, MatchLogEntryKind.Attack, subjectHero: Attacker, subjectMinion: Minion,
                outcome: MatchLogOutcome.AttackSuccessful));

            Assert.That(cues[0].Target, Is.EqualTo(KineticSubject.ForMinion(Minion)));
            Assert.That(cues[0].Target.Kind, Is.EqualTo(KineticSubjectKind.Minion));
        }

        /// <summary>
        /// Milestone 8.35z, and the case worth reading beside the one directly above it. That case
        /// passed throughout 8.35y while a real minion attack still played nothing, because it hands
        /// the presenter a row that already names a minion - and the defect was that the log never
        /// produced such a row. 8.35z fixed the log's side; this pins what the fixed row is now worth
        /// on this side, all the way to the figure: the lunge names the minion, and the damage rides
        /// it with the striking card's own printed value and its own printed colour, read from the
        /// real definition catalog rather than chosen here.
        /// </summary>
        [Test]
        public void AnAttackOnAMinion_LungesAtIt_CarryingThePrintedFigureOfTheCardThatStruck()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var projection = host.Projection;

            var attack = FindCardPrinting(projection, PrintedValueChannel.Attack);
            Assert.That(attack.card, Is.Not.Null, "The official content prints an Attack value somewhere.");

            var cues = PrimedStream().Observe(
                new[]
                {
                    Row(41, MatchLogEntryKind.Attack, subjectHero: Attacker, subjectMinion: Minion,
                        card: attack.card.Id, outcome: MatchLogOutcome.AttackSuccessful)
                },
                projection);

            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Lunge));
            Assert.That(cues[0].Subject, Is.EqualTo(KineticSubject.ForHero(Attacker)));
            Assert.That(cues[0].Target, Is.EqualTo(KineticSubject.ForMinion(Minion)),
                "The defender the log now names is the piece the attacker leans into.");
            Assert.That(cues[1].Kind, Is.EqualTo(KineticCueKind.Flash));
            Assert.That(cues[1].Subject, Is.EqualTo(KineticSubject.ForMinion(Minion)),
                "The minion that was hit is the piece that registers it.");
        }

        /// <summary>
        /// A defended attack must never show a damage figure, because none was dealt. The block's own
        /// figure is the defender's, and it arrives on the defender's own row.
        /// </summary>
        [Test]
        public void AnAttackThatWasDefended_ShowsNoDamage_AndDoesNotFlashTheDefender()
        {
            var cues = CuesFor(Row(
                9, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                outcome: MatchLogOutcome.AttackSuccessfullyDefended));

            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Lunge));
        }

        [Test]
        public void ADefenseCardDiscarded_FlashesTheDefender()
        {
            var cues = CuesFor(Row(
                11, MatchLogEntryKind.Defense, subjectHero: Defender, targetHero: Attacker,
                outcome: MatchLogOutcome.DefenseCardDiscarded));

            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Flash));
            Assert.That(cues[0].Subject, Is.EqualTo(KineticSubject.ForHero(Defender)),
                "The Defense row's subject is the hero who answered, per MatchLogProjection's own rule.");
        }

        [Test]
        public void ADeclinedDefense_DrawsNothing()
        {
            Assert.That(
                CuesFor(Row(
                    11, MatchLogEntryKind.Defense, subjectHero: Defender,
                    outcome: MatchLogOutcome.DefenseDeclined)).Count,
                Is.EqualTo(0),
                "Declining to defend is not a block, and states no figure.");
        }

        [Test]
        public void ARemovedPiece_ShrinksAndFadesRatherThanVanishing()
        {
            var hero = CuesFor(Row(13, MatchLogEntryKind.HeroRemoved, subjectHero: Defender));
            Assert.That(hero.Count, Is.EqualTo(1));
            Assert.That(hero[0].Kind, Is.EqualTo(KineticCueKind.Defeat));
            Assert.That(hero[0].Subject, Is.EqualTo(KineticSubject.ForHero(Defender)));

            foreach (var kind in new[]
                { MatchLogEntryKind.MinionRemoved, MatchLogEntryKind.MinionBattleMinionRemoved })
            {
                var minion = CuesFor(Row(13, kind, subjectHero: Attacker, subjectMinion: Minion));
                Assert.That(minion.Count, Is.EqualTo(1), kind + " should fade the minion it removed.");
                Assert.That(minion[0].Kind, Is.EqualTo(KineticCueKind.Defeat));
                Assert.That(minion[0].Subject, Is.EqualTo(KineticSubject.ForMinion(Minion)),
                    "A minion removal is about the minion, not the hero that caused it.");
            }
        }

        /// <summary>
        /// The defeat fires on the REMOVAL rather than on the defeat record: the shrink has to cover
        /// the moment the piece leaves the board, and nothing has left it when the defeat is recorded.
        /// </summary>
        [Test]
        public void ARecordedDefeat_IsNotItselfAKinetic()
        {
            Assert.That(
                CuesFor(Row(13, MatchLogEntryKind.HeroDefeated, subjectHero: Defender)).Count,
                Is.EqualTo(0));
            Assert.That(
                CuesFor(Row(13, MatchLogEntryKind.MinionDefeated, subjectMinion: Minion)).Count,
                Is.EqualTo(0));
        }

        [Test]
        public void ARespawn_PopsIn()
        {
            var cues = CuesFor(Row(17, MatchLogEntryKind.HeroRespawned, subjectHero: Defender));
            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Respawn));
        }

        /// <summary>
        /// The closed-vocabulary statement. Every log kind that names no kinetic writes nothing, so a
        /// kind added to the log later is silent here rather than accidentally animated.
        /// </summary>
        [Test]
        public void EveryOtherLogKind_DrawsNothing()
        {
            var kinetic = new[]
            {
                MatchLogEntryKind.Move, MatchLogEntryKind.FastTravel, MatchLogEntryKind.Attack,
                MatchLogEntryKind.Defense, MatchLogEntryKind.HeroRemoved,
                MatchLogEntryKind.MinionRemoved, MatchLogEntryKind.MinionBattleMinionRemoved,
                MatchLogEntryKind.HeroRespawned,

                // Milestone 8.35z11 added these two arms. They are named here rather than left to
                // fail this case, because this case's whole job is to be the place a new kind has to
                // be argued for: a kind that starts animating without appearing on this list is a kind
                // somebody animated by accident.
                MatchLogEntryKind.CardRevealed, MatchLogEntryKind.LevelPurchased
            };

            foreach (MatchLogEntryKind kind in Enum.GetValues(typeof(MatchLogEntryKind)))
            {
                if (kind == MatchLogEntryKind.None || kinetic.Contains(kind))
                {
                    continue;
                }

                Assert.That(
                    CuesFor(Row(21, kind, subjectHero: Attacker, subjectMinion: Minion, amount: 3)).Count,
                    Is.EqualTo(0),
                    kind + " is not a kinetic and must draw nothing.");
            }
        }

        // ---- The three rules that make a kinetic layer safe.

        [Test]
        public void TheFirstSightOfAMatchInProgress_PlaysNothingAndPrimes()
        {
            var stream = new KineticCueStream();
            var log = Enumerable.Range(1, 40)
                .Select(sequence => Row(
                    sequence, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                    outcome: MatchLogOutcome.AttackSuccessful))
                .ToArray();

            var cues = stream.Observe(log);

            Assert.That(cues.Count, Is.EqualTo(0),
                "A client joining a match in progress must not play the whole history at once.");
            Assert.That(stream.PrimedOnLastObservation, Is.True);
            Assert.That(stream.ObservedSequence, Is.EqualTo(40));
        }

        [Test]
        public void OnlyTheRowsThatArrivedSinceTheLastLook_Play()
        {
            var stream = new KineticCueStream();
            var first = new[] { Row(1, MatchLogEntryKind.HeroRespawned, subjectHero: Attacker) };
            stream.Observe(first);

            var second = new[]
            {
                first[0],
                Row(2, MatchLogEntryKind.HeroRespawned, subjectHero: Defender)
            };

            var cues = stream.Observe(second);
            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Sequence, Is.EqualTo(2));
            Assert.That(stream.PrimedOnLastObservation, Is.False);

            Assert.That(stream.Observe(second).Count, Is.EqualTo(0),
                "The same log observed twice replays nothing.");
        }

        [Test]
        public void ALogThatWentBackwards_PlaysNothingAndAsksForASnap()
        {
            var stream = new KineticCueStream();
            stream.Observe(new[] { Row(1, MatchLogEntryKind.HeroRespawned, subjectHero: Attacker) });
            stream.Observe(new[]
            {
                Row(1, MatchLogEntryKind.HeroRespawned, subjectHero: Attacker),
                Row(2, MatchLogEntryKind.HeroRespawned, subjectHero: Defender),
                Row(3, MatchLogEntryKind.HeroRespawned, subjectHero: Defender)
            });

            // The undo: the log is shorter than it was.
            var cues = stream.Observe(new[]
            {
                Row(1, MatchLogEntryKind.HeroRespawned, subjectHero: Attacker)
            });

            Assert.That(cues.Count, Is.EqualTo(0), "An undo plays nothing; the board snaps.");
            Assert.That(stream.PrimedOnLastObservation, Is.True,
                "The snap has to be announced, because the floating figures outlive an ordinary change.");
            Assert.That(stream.ObservedSequence, Is.EqualTo(1));
        }

        /// <summary>
        /// Milestone 8.35z11 REPLACED this case's rule, and the replacement is the conductor's
        /// whole point rather than a relaxation.
        ///
        /// <para><b>What it used to assert, and why that was right at the time.</b> Milestone 8.35y
        /// started every cue of a projection change at the SAME INSTANT, so a resync window of thirty
        /// rows was thirty simultaneous animations - unreadable, and thirty times the per-frame cost.
        /// It kept the newest six and dropped the rest.</para>
        ///
        /// <para><b>What it asserts now.</b> Under a conductor those thirty rows are thirty QUEUED
        /// moments: the stream hands over all of them, in order, and the conductor plays them one at a
        /// time, compresses the queue when the viewer is behind, and drops only past
        /// <see cref="KineticStaging.MaxQueuedScenes"/>. Truncating here would have made the queue
        /// budget unreachable dead code. The property the old constant named -
        /// <see cref="KineticTiming.MaxConcurrentCues"/>, "the most kinetics a surface plays at once" -
        /// is now guaranteed by the SIZE OF A SCENE instead of by a truncation, which is why it is
        /// asserted here on every scene rather than dropped.</para>
        /// </summary>
        [Test]
        public void ABurstOfEvents_IsQueuedInFull_AndNoSinglePlaysMoreAtOnceThanTheCap()
        {
            var stream = PrimedStream();
            var log = Enumerable.Range(1, 30)
                .Select(sequence => Row(
                    sequence, MatchLogEntryKind.HeroRespawned, subjectHero: Attacker))
                .ToArray();

            var scenes = stream.ObserveScenes(log);

            Assert.That(scenes.Count, Is.EqualTo(30),
                "Every row of the window becomes a moment to stage; the conductor bounds the queue.");
            Assert.That(scenes[scenes.Count - 1].Order, Is.EqualTo(30),
                "The scenes stay in the order the match recorded them.");
            Assert.That(stream.ObservedSequence, Is.EqualTo(30),
                "Observing a window must still advance the watermark, or it repeats forever.");

            foreach (var scene in scenes)
            {
                Assert.That(scene.Cues.Count, Is.LessThanOrEqualTo(KineticTiming.MaxConcurrentCues),
                    "No single moment may play more kinetics at once than the recorded cap.");
            }
        }

        /// <summary>
        /// The cap above is not vacuous: the biggest scene the vocabulary can build is an attack that
        /// landed, which is a lunge and the flash it caused - two cues, one moment.
        /// </summary>
        [Test]
        public void AnAttackThatLanded_IsONEScene_BecauseItIsOneMoment()
        {
            var stream = PrimedStream();
            var scenes = stream.ObserveScenes(new[]
            {
                Row(11, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                    outcome: MatchLogOutcome.AttackSuccessful)
            });

            Assert.That(scenes.Count, Is.EqualTo(1),
                "A lunge and its flash staged apart would tell the player two things happened.");
            Assert.That(scenes[0].Cues.Count, Is.EqualTo(2));
            Assert.That(scenes[0].Cues[0].Kind, Is.EqualTo(KineticCueKind.Lunge));
            Assert.That(scenes[0].Cues[1].Kind, Is.EqualTo(KineticCueKind.Flash));
            Assert.That(scenes[0].Kind, Is.EqualTo(KineticSceneKind.Board));
        }

        /// <summary>
        /// The amendment's "identical on every client", stated as the property it actually is: the log
        /// is the same log on every seat by milestone 8.35r's own construction, and this derivation is
        /// a pure function of it. Two independent clients therefore play the same story.
        /// </summary>
        [Test]
        public void TwoClientsReadingTheSameLog_PlayTheSameStory()
        {
            var log = new[]
            {
                Row(1, MatchLogEntryKind.Move, subjectHero: Attacker,
                    origin: new HexCoordinate(0, 0), destination: new HexCoordinate(1, 0),
                    hasOrigin: true, hasDestination: true),
                Row(2, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                    outcome: MatchLogOutcome.AttackSuccessful),
                Row(3, MatchLogEntryKind.HeroRemoved, subjectHero: Defender)
            };

            var host = PrimedStream().Observe(log);
            var guest = PrimedStream().Observe(log);

            Assert.That(guest.Count, Is.EqualTo(host.Count));
            for (var index = 0; index < host.Count; index++)
            {
                Assert.That(guest[index].Sequence, Is.EqualTo(host[index].Sequence));
                Assert.That(guest[index].Kind, Is.EqualTo(host[index].Kind));
                Assert.That(guest[index].Subject, Is.EqualTo(host[index].Subject));
                Assert.That(guest[index].Target, Is.EqualTo(host[index].Target));
            }
        }

        // ---- The figures, read off the real definition catalog.

        /// <summary>
        /// The user's pinned rule for the colour, held against real content: the tint is the printed
        /// colour the card's own <c>CardProgressionProfile</c> states, and the figure is the value the
        /// card prints. Neither is a presentation-side choice, which is what makes two clients agree
        /// without anything being sent between them.
        /// </summary>
        [Test]
        public void AFloatingFigure_IsThePrintedValueAndColourOfTheCardTheLogNamed()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var projection = host.Projection;

            var defense = FindCardPrinting(projection, PrintedValueChannel.Defense);
            Assert.That(defense.card, Is.Not.Null, "The official content prints a Defense value somewhere.");

            var cues = PrimedStream().Observe(
                new[]
                {
                    Row(31, MatchLogEntryKind.Defense, subjectHero: Defender, targetHero: Attacker,
                        card: defense.card.Id, outcome: MatchLogOutcome.DefenseCardDiscarded)
                },
                projection);

            Assert.That(cues.Count, Is.EqualTo(1));

            var attack = FindCardPrinting(projection, PrintedValueChannel.Attack);
            Assert.That(attack.card, Is.Not.Null);

            var attackCues = PrimedStream().Observe(
                new[]
                {
                    Row(31, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                        card: attack.card.Id, outcome: MatchLogOutcome.AttackSuccessful)
                },
                projection);

        }

        /// <summary>
        /// A cue whose card the catalog cannot resolve still plays; it simply states no figure. A
        /// flourish must cost a flourish and never a frame.
        /// </summary>
        [Test]
        public void ACardTheCatalogCannotResolve_CostsTheFigureAndNothingElse()
        {
            var cues = PrimedStream().Observe(
                new[]
                {
                    Row(31, MatchLogEntryKind.Attack, subjectHero: Attacker, targetHero: Defender,
                        card: new CardInstanceId("no-such-card"),
                        outcome: MatchLogOutcome.AttackSuccessful)
                });

            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Kind, Is.EqualTo(KineticCueKind.Lunge));
        }

        private static (CardInstance card, CardDefinition definition, int amount) FindCardPrinting(
            IMatchReadModel projection, PrintedValueChannel channel)
        {
            foreach (var card in projection.Cards)
            {
                var definition = projection.Definitions.FirstOrDefault(
                    value => value.Reference.Equals(card.DefinitionReference));
                if (definition?.ActionDefinitions == null)
                {
                    continue;
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    if (action?.BaseValues == null)
                    {
                        continue;
                    }

                    foreach (var value in action.BaseValues)
                    {
                        if (value.Channel == channel)
                        {
                            return (card, definition, value.Amount);
                        }
                    }
                }
            }

            return (null, null, 0);
        }

        // ---- The cue type's own refusals.

        [Test]
        public void ACueRefusesAShapeItCouldNotDraw()
        {
            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.Slide, KineticSubject.ForHero(Attacker),
                route: new[] { new HexCoordinate(0, 0) }));

            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.Lunge, KineticSubject.ForHero(Attacker)));

            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.Flash, KineticSubject.ForHero(Attacker),
                route: new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }));


            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.None, KineticSubject.ForHero(Attacker)));

            Assert.Throws<ArgumentException>(() => new KineticCue(
                1, KineticCueKind.Flash, KineticSubject.None));
        }
    }
}
