using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z46. Silverarrow's "End of turn:" keyword DRIVEN through a whole
    /// turn's close, rather than asserted at the lifecycle.
    ///
    /// <para><b>Why this file exists, in one sentence: milestone 8.35z30 named this the single
    /// largest unverified claim in Silverarrow's composition and said so.</b> That milestone built
    /// <see cref="ActiveEffectDurationKind.EndOfTurn"/>, added <c>TurnEnded</c> to the supported
    /// trigger windows, inverted the lifecycle's trigger/expiry order for the coincident case, and
    /// gave the turn's close a producer - and it proved all of that at the lifecycle, where a trigger
    /// firing is a recorded event. What it could not prove is the part a player experiences: that the
    /// turn WITHHOLDS its own close while the trigger's questions stand, that those questions are
    /// answerable, and that the close then completes. Its own notes say to drive one before believing
    /// it. This drives one.</para>
    ///
    /// <para><b>What it composes and why it composes it by hand.</b> A staged Silverarrow
    /// <see cref="MatchConfiguration"/> cannot be built until her deferred capability contracts are
    /// registered: the configuration's constructor validates every dealt card's effect roots against
    /// <see cref="MatchContentCatalogs.ForThisBuild"/>, and Shoot and Scoot names
    /// <c>fast-travel@1</c>, which this build defers. So the turn's inputs are composed directly -
    /// a valid configuration's participants and hands, plus Warning Shot's own definition and one
    /// instance of it - against a BOUNDED catalog carrying the deferred handlers. That is exactly what
    /// <see cref="SyntheticRosterCapabilities.DeferredHandlers"/> documents itself as being for, and it
    /// keeps this file's subject the turn's close rather than the registration list.</para>
    ///
    /// <para><b>The ordering fact this establishes for whoever deals her</b>, because it is not
    /// obvious and it costs a red suite to discover: the staged-configuration factories and the
    /// registration flip are ONE edit. A factory naming Silverarrow's hand throws
    /// <c>UnregisteredCapability</c> until her contracts are registered, so no fixture may name one
    /// before that same change.</para>
    /// </summary>
    public sealed class SilverarrowEndOfTurnDriveTests
    {
        /// <summary>
        /// The turn's close is withheld while the trigger's questions stand, and completes when they
        /// are answered - which is the handshake milestone 8.35z30 reasoned about and could not drive.
        ///
        /// <para><b>Two pauses, one per printed question, and the count is a surface fact worth
        /// pinning.</b> The face prints two questions - which enemy hero, then which card - and each
        /// names exactly one subject, so each reaches a player as ONE command that both names it and
        /// closes the question.</para>
        ///
        /// <para>Milestone 8.46b is what made that true and this assertion is where the change is
        /// measured on real content. Until then each of these two questions stood on Select and then
        /// Confirm, this drive recorded FOUR pauses, and a journey that sent two commands stopped
        /// halfway through a question rather than finishing it. The user's rule for the decision
        /// window - "choose-exactly-one answers in one press" - halves that, and nothing else about
        /// the face moved: the same two questions, the same choosers, the same discarded card.</para>
        /// </summary>
        [Test]
        public void WarningShot_WithholdsTheTurnsCloseUntilBothOfItsQuestionsAreAnswered()
        {
            var drive = Drive(separation: 3);

            Assert.That(drive.Pauses, Has.Count.EqualTo(2),
                "Two printed questions, each of exactly one subject and so each answered by naming " +
                "it.");
            Assert.That(drive.Pauses.All(pause => pause.ResolvesOnSelection), Is.True,
                "and that is why: both are exactly-one selections. Asked: " + string.Join(", ",
                    drive.Pauses.Select(pause => pause.NodeId.Value + " " +
                        pause.MinimumSelections + ".." + pause.MaximumSelections)));
            Assert.That(drive.EndedBeforeTheLastAnswer, Is.False,
                "The work is printed as happening at the end of THIS turn, so the turn is not over " +
                "while the question stands.");
            Assert.That(drive.Ended, Is.True,
                "And the answer is what completes the close, rather than the turn ending beside it.");
        }

        /// <summary>
        /// Who each question belongs to. p.10 gives an unassigned decision to the card's controller, so
        /// Silverarrow names which enemy hero; the card comes out of that hero's own hand, so they name
        /// which card. The two choosers differing is the whole of what makes this face a negotiation
        /// rather than a number.
        /// </summary>
        [Test]
        public void WarningShot_AsksSilverarrowWhichEnemyAndThatEnemyWhichCard()
        {
            var drive = Drive(separation: 3);

            Assert.That(drive.Pauses[0].NodeId.Value, Is.EqualTo("warning-select"));
            Assert.That(drive.Pauses[0].ChooserId, Is.EqualTo(drive.Silverarrow));
            Assert.That(drive.Pauses[0].Candidates, Has.Count.EqualTo(1),
                "One enemy hero stands inside her printed Radius 3.");

            // Milestone 8.46b: the discard is the SECOND pause rather than the third. Each question
            // names exactly one subject and is therefore asked once, not once to offer and again to
            // confirm - see WarningShot_WithholdsTheTurnsCloseUntilBothOfItsQuestionsAreAnswered.
            Assert.That(drive.Pauses[1].NodeId.Value, Is.EqualTo("warning-discard"));
            Assert.That(drive.Pauses[1].ChooserId, Is.EqualTo(drive.Enemy),
                "It is their hand, so it is their choice - which is what \"discards a card\" means.");
            Assert.That(drive.Pauses[1].Candidates, Has.Count.EqualTo(drive.EnemyHandBefore));
        }

        /// <summary>The work actually happens: a card leaves the named hero's hand.</summary>
        [Test]
        public void WarningShot_TakesACardOutOfTheNamedEnemysHand()
        {
            var drive = Drive(separation: 3);

            Assert.That(drive.EnemyHandAfter, Is.EqualTo(drive.EnemyHandBefore - 1));
        }

        /// <summary>
        /// SL-06's second half, measured on two boards rather than argued: the disk is derived when the
        /// trigger FIRES, against wherever she is standing then.
        ///
        /// <para>Nothing about the effect's creation differs between the two cells - same definition,
        /// same source card, same controller, same round and turn - so the only thing that can decide
        /// the outcome is the board the trigger reads. At exactly her printed Radius the enemy is a
        /// candidate and loses a card; one space further out there is no candidate at all, nothing is
        /// discarded, and the turn closes with no question asked. Under the reading SL-06 rejected -
        /// a disk recorded when the Skill resolved - the two cells could not differ, because at
        /// creation neither board had been read.</para>
        /// </summary>
        [Test]
        public void SL06_TheRadiusIsMeasuredWhenTheTriggerFires_SoOneSpaceFurtherOutIsNoCandidate()
        {
            var inside = Drive(separation: 3);
            var outside = Drive(separation: 4);

            Assert.That(inside.Pauses, Is.Not.Empty);
            Assert.That(inside.EnemyHandAfter, Is.EqualTo(inside.EnemyHandBefore - 1));

            Assert.That(outside.Pauses, Is.Empty,
                "Nobody is inside the disk when the trigger fires, so the selection asks nothing.");
            Assert.That(outside.EnemyHandAfter, Is.EqualTo(outside.EnemyHandBefore),
                "And nothing is discarded, which is the observable difference from the inside cell.");
            Assert.That(outside.Ended, Is.True,
                "A mandatory step that cannot happen stops the EFFECT, exactly as p.10 says, and " +
                "never the turn: an end-of-turn face with no legal subject must not hang the close.");
        }

        /// <summary>
        /// It fires once. The effect expires at the very checkpoint it triggers at, which is what
        /// milestone 8.35z30's inverted lifecycle order is for, and this is that order observed from
        /// the turn rather than from the lifecycle's own event list.
        /// </summary>
        [Test]
        public void WarningShot_EndsAtTheSameCheckpointItFiresAt()
        {
            var drive = Drive(separation: 3);

            Assert.That(drive.EffectPhase, Is.EqualTo(ActiveEffectPhase.Ended));
            // Milestone 8.46b: once, not twice. The count used to be two because one question was
            // asked once to offer its candidates and again to confirm; an exactly-one selection is
            // now answered by naming it, so a single appearance is the whole of "asked once" and a
            // second firing would still show up here as two.
            Assert.That(drive.Pauses.Count(pause => pause.NodeId.Value == "warning-select"),
                Is.EqualTo(1),
                "One question asked once - a second firing would ask it again.");
        }

        // -----------------------------------------------------------------------------------------
        // The drive itself.
        // -----------------------------------------------------------------------------------------

        private sealed class DriveResult
        {
            public HeroId Silverarrow;
            public HeroId Enemy;
            public List<PendingEffectChoice> Pauses = new List<PendingEffectChoice>();
            public bool Ended;
            public bool EndedBeforeTheLastAnswer;
            public int EnemyHandBefore;
            public int EnemyHandAfter;
            public ActiveEffectPhase EffectPhase;
        }

        /// <summary>
        /// One whole turn, with a Warning Shot effect standing and the two heroes the named number of
        /// spaces apart on a straight line of playable spaces.
        /// </summary>
        private static DriveResult Drive(int separation)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var participants = configuration.Participants;
            var result = new DriveResult
            {
                Silverarrow = participants.Heroes.Single(hero =>
                    participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id,
                Enemy = participants.Heroes.Single(hero =>
                    participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id
            };

            var warningShotDefinition = SilverarrowCardContent.Definition(
                SilverarrowCardContent.WarningShot);
            var skill = warningShotDefinition.ActionDefinitions.Single(
                action => action.Kind == ActionKind.Skill);
            var warningShot = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-end-of-turn-source"),
                warningShotDefinition.Reference,
                result.Silverarrow);

            var definitions = configuration.CardDefinitions
                .Concat(new[] { warningShotDefinition }).ToArray();
            var instances = configuration.CardInstances.Concat(new[] { warningShot }).ToArray();

            var spaces = Enumerable.Range(0, separation + 1)
                .Select(step => new HexCoordinate(step, 0))
                .ToArray();
            var board = new AdjacentMoveAuthority(
                spaces,
                new[]
                {
                    new HeroOccupancy(result.Silverarrow, spaces[0]),
                    new HeroOccupancy(result.Enemy, spaces[spaces.Length - 1])
                });

            // The bounded catalog this file's summary describes: the build's registered set, plus
            // Silverarrow's Active-effect creations and the deferred roster contracts her triggered
            // effects reach. Nothing production registers is changed by composing one here.
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    SilverarrowActiveEffectContent.Handlers()
                        .Concat(SyntheticRosterCapabilities.DeferredHandlers())),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(
                    SilverarrowEffectContent.TriggeredEffects(
                        SilverarrowCardContent.ContentRevision)));
            var cardActions = new CardActionResolutionAuthority(catalogs, board);

            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(
                new ActiveEffectId("silverarrow-end-of-turn-drive"),
                SilverarrowActiveEffectContent.WarningShotDefinition,
                result.Silverarrow,
                TeamSide.Blue,
                result.Silverarrow,
                warningShot.Id,
                skill.Id,
                1,
                1);

            // p.7 needs one revealed card a side before a turn may start at all.
            foreach (var committed in new[]
                { configuration.RedHand.Instances[0], configuration.BlueHand.Instances[0] })
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }

            result.EnemyHandBefore = instances.Count(card =>
                card.OwnerId.Equals(result.Enemy) && card.State.Location == CardLocation.Hand);

            var turn = new TurnResolutionAuthority(
                definitions, instances, participants, new TieBreakerState(TeamSide.Red),
                cardActions, lifecycle, 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);

            for (var step = 0; step < 40 && !turn.IsEnded; step++)
            {
                var choice = turn.PendingEffectChoice;
                if (choice != null)
                {
                    result.Pauses.Add(choice);
                    result.EndedBeforeTheLastAnswer = turn.IsEnded;
                    Assert.That(turn.TryAnswerEffectChoice(
                            choice.SelectInvocations.Count > 0
                                ? choice.SelectInvocations[0]
                                : choice.ConfirmInvocation).Accepted,
                        Is.True,
                        "An end-of-turn trigger's own question must be answerable at the moment it " +
                        "is asked - a question nobody can answer is a hung turn.");
                    continue;
                }

                if (turn.PendingTurnRelease != null)
                {
                    Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                    continue;
                }

                Assert.That(turn.SelectedActor.CardInstanceId.IsDefined, Is.True,
                    "A turn that is neither ended, nor asking, nor waiting to be released must have " +
                    "an actor to act with.");
                Assert.That(
                    turn.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted, Is.True);
            }

            result.Ended = turn.IsEnded;
            result.EnemyHandAfter = instances.Count(card =>
                card.OwnerId.Equals(result.Enemy) && card.State.Location == CardLocation.Hand);
            result.EffectPhase = lifecycle.Effects
                .Single(effect => effect.Id.Value == "silverarrow-end-of-turn-drive").Phase;
            return result;
        }
    }
}
