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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71d. Hanu the Trickster's three Blue journeys - Unexpected Journey, There and Back
    /// Again and Safe Travels: "Swap with an enemy hero in radius. <b>This turn:</b> That hero is
    /// immune. <b>End of turn:</b> Swap with that hero regardless of radius and immunity." Safe
    /// Travels adds "Move up to 1 space." after the End-of-turn sentence.
    ///
    /// <para><b>What the cases are about.</b> The clause spans a whole turn, so the file is in four
    /// parts: the offer and the first exchange, which are measured at the effect frame; the immunity,
    /// which is measured as the rules question it is, one actor at a time; the turn's own close, which
    /// is DRIVEN because that is the only place the swap back exists; and the composition facts a
    /// later block inherits - the deferral from both sides, the derived decision-label requirement,
    /// and the printed values.</para>
    ///
    /// <para><b>The board these cases all read.</b> Hanu at the origin; his own teammate beside him,
    /// so "an ENEMY hero" has something to exclude; one enemy hero two spaces out, inside every
    /// printed Radius of the three; one more three spaces out, inside Tier II's and Tier III's Radius
    /// 3 and outside Tier I's Radius 2 - which is the same space a Radius item brings into reach.</para>
    ///
    /// <para><b>Why the participants are this file's own rather than the staged pair.</b>
    /// <see cref="StagedBoardParticipants"/> fields ONE hero on the Red team, and "every hero's
    /// actions" cannot be measured without a hero on the acting side who is not the actor: the
    /// immunity this card grants bars Hanu's own teammates exactly as it bars the enemy's, and a board
    /// with no teammate would have proved half of the declaration and left the other half untested.
    /// Four seats, two a side, is the smallest board on which every class the declaration names has a
    /// member.</para>
    ///
    /// <para><b>Nothing of Hanu's is registered</b> - milestone 7.9's gate holds his content out of
    /// <see cref="MatchContentCatalogs"/> until 8.71h deals him - so every harness below composes a
    /// bounded catalog carrying this block's deferred contracts, exactly as
    /// <c>SilverarrowEndOfTurnDriveTests</c> documents for the same reason. Unity-independent, so it
    /// runs in the cross-platform harness as well as the Windows tier.</para>
    /// </summary>
    public sealed class HanuJourneyTests
    {
        private static readonly TeamId RedTeam = new TeamId("hanu-red-team");
        private static readonly TeamId BlueTeam = new TeamId("hanu-blue-team");
        private static readonly PlayerId HanuPlayer = new PlayerId("hanu-player");
        private static readonly PlayerId AllyPlayer = new PlayerId("hanu-ally-player");
        private static readonly PlayerId EnemyPlayer = new PlayerId("hanu-enemy-player");
        private static readonly PlayerId SecondEnemyPlayer = new PlayerId("hanu-second-enemy-player");

        private static readonly HeroId Hanu = new HeroId("hanu-trickster");

        /// <summary>His own teammate - p.17's Friendly, and never a candidate for "an enemy hero".</summary>
        private static readonly HeroId Ally = new HeroId("hanu-red-ally");

        private static readonly HeroId Enemy = new HeroId("hanu-blue-enemy");
        private static readonly HeroId SecondEnemy = new HeroId("hanu-blue-second-enemy");

        private static readonly MapId Map = new MapId("hanu-journey-map");

        private static readonly HexCoordinate HanuAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent to him, and on his own side.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(1, 0);

        /// <summary>Two spaces out: inside every Radius the three faces print.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(0, 2);

        /// <summary>
        /// Three spaces out: outside Unexpected Journey's printed Radius 2, inside the other two
        /// faces' 3, and inside Unexpected Journey's 2 once p.16's Radius item raises it.
        /// </summary>
        private static readonly HexCoordinate SecondEnemyAt = new HexCoordinate(0, 3);

        // -------------------------------------------------------------------------------------------
        // "Swap with an enemy hero in radius"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE OFFER. Every word of the first sentence doing its own work: "an ENEMY hero"
        /// drops his teammate standing right beside him, "hero" would drop a minion if one stood
        /// there, and "in radius" is the performing card's printed icon rather than any other reach -
        /// Unexpected Journey prints 2, so the enemy three spaces out is not in the sentence at all.
        /// </summary>
        [Test]
        public void TheOfferIsEveryEnemyHeroInThePrintedRadius_AndNobodyElse()
        {
            var probe = new Probe(HanuCardContent.UnexpectedJourney);

            Assert.That(Named(probe.Choice), Is.EqualTo(new[] { Enemy.Value }),
                "Radius 2 reaches the enemy two spaces out and nobody else; his own teammate is " +
                "beside him and is not an enemy, and the second enemy is three spaces out.");
            Assert.That(probe.Choice.ChooserId, Is.EqualTo(Hanu),
                "\"Swap with\" is his own step, so p.10 gives the decision to him.");
        }

        /// <summary>
        /// Tier II prints Radius 3 and reaches both, which is the only thing that differs between the
        /// two faces' first sentence.
        /// </summary>
        [Test]
        public void ThePrintedRadiusIsReadOffTheCardBeingPerformed()
        {
            Assert.That(
                Named(new Probe(HanuCardContent.ThereAndBackAgain).Choice),
                Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }));
            Assert.That(
                Named(new Probe(HanuCardContent.SafeTravels).Choice),
                Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }));
        }

        /// <summary>
        /// p.16's items raise the channel they name and no other. A Radius item brings the hero three
        /// spaces out inside Tier I's printed 2; the Attack item Tier II and Tier III actually hand
        /// out raises nothing this sentence reads.
        /// </summary>
        [Test]
        public void ARadiusItemWidensTheOffer_AndTheAttackItemTheseFacesHandOutDoesNot()
        {
            Assert.That(
                Named(new Probe(
                    HanuCardContent.UnexpectedJourney,
                    item: PrintedValueChannel.Radius).Choice),
                Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }));

            Assert.That(
                Named(new Probe(
                    HanuCardContent.UnexpectedJourney,
                    item: PrintedValueChannel.Attack).Choice),
                Is.EqualTo(new[] { Enemy.Value }),
                "An Attack item raises the Attack channel; the disk this sentence measures is the " +
                "Radius one.");
        }

        /// <summary>
        /// p.19 makes a Swap a TARGETING of its subject, so p.18's immunity narrows this offer - and
        /// a disk holding nobody else is p.10's impossible mandatory step: the Skill performs nothing
        /// rather than offering a hero the exchange would then refuse (milestone 8.35l).
        /// </summary>
        [Test]
        public void AnImmuneEnemyIsNeverOffered_AndADiskOfNobodyElseStopsTheSkill()
        {
            var narrowed = new Probe(HanuCardContent.ThereAndBackAgain, immuneHero: Enemy);
            Assert.That(Named(narrowed.Choice), Is.EqualTo(new[] { SecondEnemy.Value }));

            var stopped = new Probe(HanuCardContent.UnexpectedJourney, immuneHero: Enemy);
            Assert.That(stopped.Choice, Is.Null);
            Assert.That(stopped.Step.IsComplete, Is.True,
                "A mandatory step with no legal subject stops the effect; the action is performed " +
                "and does nothing.");
            Assert.That(stopped.Timing.Effects.Any(effect =>
                    effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero),
                Is.False,
                "and the creation below it is never reached, so no journey stands to fire at the " +
                "turn's end.");
        }

        /// <summary>
        /// The answer, performed: the two exchange places, and ONE Hero-scoped occurrence is created,
        /// bound to the hero the sentence named rather than to the hero who played the card.
        ///
        /// <para>The <c>affected</c> output is the named hero too. Every earlier registration
        /// publishes the actor there because every earlier scope is about the actor; the Hero scope is
        /// the one that is not, and an output called "affected" that named somebody else would be a
        /// reader's trap.</para>
        /// </summary>
        [Test]
        public void TheSwapHappensAndBindsTheJourneyToTheHeroItNamed()
        {
            var probe = new Probe(HanuCardContent.UnexpectedJourney);
            probe.Pick(EffectValue.Hero(Enemy));

            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.Space(Hanu), Is.EqualTo(EnemyAt));
            Assert.That(probe.Space(Enemy), Is.EqualTo(HanuAt));

            var journey = probe.Journey;
            Assert.That(journey.AffectedHeroId, Is.EqualTo(Enemy));
            Assert.That(journey.ControllerHeroId, Is.EqualTo(Hanu),
                "p.17 ends an Active effect when ITS hero is defeated, and its hero is the one whose " +
                "action created it.");
            Assert.That(journey.Definition.DurationKind,
                Is.EqualTo(ActiveEffectDurationKind.EndOfTurn));
            Assert.That(journey.Phase, Is.EqualTo(ActiveEffectPhase.Active),
                "\"This turn\" comes into effect immediately (p.17), so the immunity stands from the " +
                "moment the action creates it.");

            Assert.That(
                probe.CompletedOutput(
                    HanuEffectContent.JourneyEffectNode,
                    ActiveEffectCreationHandler.AffectedOutputName),
                Is.EqualTo(EffectValue.Hero(Enemy)),
                "The `affected` output names the hero the sentence chose, not the hero who played " +
                "the card - the one scope where those differ.");
        }

        /// <summary>
        /// The three faces create three different occurrences, which is what a uniqueness key per face
        /// is for: two of his Blues played in one turn are impossible by roster, and two different
        /// definitions standing at one checkpoint is a state the pipeline must still be able to order.
        /// </summary>
        [Test]
        public void EachFaceCreatesItsOwnDefinition()
        {
            Assert.That(
                HanuActiveEffectContent.Definitions.Select(definition => definition.UniquenessKey)
                    .Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(
                HanuActiveEffectContent.Definitions.All(definition =>
                    definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero &&
                    definition.TriggeredEffect.HasValue &&
                    definition.TriggerCheckpoints.Single() == TimingCheckpoint.TurnEnded &&
                    definition.ExpirationCheckpoint == TimingCheckpoint.TurnEnded &&
                    definition.CancellationConditions.Contains(
                        ActiveEffectCancellationCondition.ControllerHeroDefeated) &&
                    definition.CancellationConditions.Contains(
                        ActiveEffectCancellationCondition.CardsRetrieved)),
                Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // "This turn: That hero is immune."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// HA-Q11, asked one actor at a time. The hero the journey named is immune to every HERO's
        /// actions - the hero who swapped with them, that hero's teammate, and their own teammate -
        /// and is not immune to their own, which is p.18's fixed rule and not something this card
        /// declares.
        /// </summary>
        [Test]
        public void TheNamedHeroIsImmuneToEveryHerosActions_AndNeverToTheirOwn()
        {
            var probe = new Probe(HanuCardContent.UnexpectedJourney);
            probe.Pick(EffectValue.Hero(Enemy));
            var rules = probe.Rules;

            foreach (var activity in new[]
                { RuleActivity.Attack, RuleActivity.Skill, RuleActivity.Swap, RuleActivity.ObjectPush })
            {
                Assert.That(IsImmune(probe, rules, Enemy, activity, Hanu), Is.True,
                    "Hanu's own " + activity + " is barred, which is what the unqualified keyword " +
                    "says about the hero who granted it as much as about anybody else.");
                Assert.That(IsImmune(probe, rules, Enemy, activity, Ally), Is.True,
                    "and his teammate's " + activity + " - the half of \"every hero\" that " +
                    "Whisper's \"enemy actions\" would not have covered.");
                Assert.That(IsImmune(probe, rules, Enemy, activity, SecondEnemy), Is.True,
                    "and their own teammate's " + activity + ".");
                Assert.That(IsImmune(probe, rules, Enemy, activity, Enemy), Is.False,
                    "p.18: \"You are never immune to your own actions\" - a rule the page applies " +
                    "before any declaration is read.");
            }
        }

        /// <summary>
        /// The other half of HA-Q11, and the reason it needs no clause: presence predicates never ask
        /// immunity at all, so p.18's "effects that check for unit presence will count immune units"
        /// is true of this card by construction.
        /// </summary>
        [Test]
        public void APresenceCheckStillCountsTheImmuneHero()
        {
            var probe = new Probe(HanuCardContent.UnexpectedJourney);
            probe.Pick(EffectValue.Hero(Enemy));

            var found = RosterPresenceQuery.FindUnitsWithin(
                probe.Space(Ally), 6, RosterUnitKind.Hero, SourceTeamRelation.OpposingSource,
                Ally, probe.Board, probe.Participants, probe.Rules);

            Assert.That(found.Any(unit => unit.HeroId.Equals(Enemy)), Is.True);
        }

        /// <summary>
        /// The immunity is not a restriction on the hero themselves: an immune hero still acts. Read
        /// here as the absence of any declared restriction rather than as a second immunity question,
        /// because those are two different families and this card prints only one of them.
        /// </summary>
        [Test]
        public void TheImmuneHeroCanStillAct()
        {
            var probe = new Probe(HanuCardContent.UnexpectedJourney);
            probe.Pick(EffectValue.Hero(Enemy));

            foreach (var activity in new[]
                { RuleActivity.Attack, RuleActivity.Skill, RuleActivity.Movement })
            {
                Assert.That(
                    probe.Rules.IsRestricted(RuleObject.ForHero(Enemy), activity), Is.False);
            }
        }

        /// <summary>
        /// "regardless of ... immunity", measured as the difference one declaration makes on the
        /// state the end-of-turn body actually runs against. Without it the swap back would be
        /// refused by the very immunity the card's own second sentence granted.
        /// </summary>
        [Test]
        public void ThePrintedOverrideIsWhatAdmitsTheSwapBack()
        {
            var probe = new Probe(HanuCardContent.UnexpectedJourney);
            probe.Pick(EffectValue.Hero(Enemy));
            var rules = probe.Rules;

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForHero(Enemy), RuleActivity.Swap, RuleObject.ForHero(Hanu),
                    probe.Board, probe.Participants, rules,
                    Array.Empty<ImmunityOverrideDeclaration>()),
                Is.True);

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForHero(Enemy), RuleActivity.Swap, RuleObject.ForHero(Hanu),
                    probe.Board, probe.Participants, rules,
                    new[]
                    {
                        new ImmunityOverrideDeclaration(
                            RuleActivity.Swap, ImmunityOverrideReason.PrintedUnqualifiedImmunity)
                    }),
                Is.False,
                "p.18 writes its own keyword to be overridden - \"unless the card text says " +
                "otherwise\" - and this is the card text it anticipates.");
        }

        /// <summary>
        /// The declaration is on the node that performs the swap BACK and on no node of the Skill's
        /// own body, because the first swap is the one the printed sentence leaves subject to p.18.
        /// </summary>
        [Test]
        public void OnlyTheEndOfTurnExchangeDeclaresTheOverride()
        {
            foreach (var body in HanuEffectContent.JourneyTriggeredEffects())
            {
                var exchange = body.Nodes.Single(
                    node => node.Id.Value == HanuEffectContent.JourneyBackExchangeNode);
                Assert.That(
                    exchange.ImmunityOverrides.Single(),
                    Is.EqualTo(new ImmunityOverrideDeclaration(
                        RuleActivity.Swap, ImmunityOverrideReason.PrintedUnqualifiedImmunity)));
            }

            // MERGE NOTE (8.71 wave): this block's own faces, not the integrated aggregate.
            foreach (var face in HanuCardContent.JourneyDefinitions())
            {
                var skill = face.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Skill).EffectRoot;
                Assert.That(skill.Nodes.All(node => node.ImmunityOverrides.Count == 0), Is.True,
                    "The FIRST swap prints no override, so an immune enemy is simply not a " +
                    "candidate for it.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // "End of turn: Swap with that hero regardless of radius and immunity."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE WHOLE CLAUSE, DRIVEN. One real round: Hanu plays Unexpected Journey, exchanges places
        /// with the enemy two spaces out, and at the turn's own close the two are exchanged back -
        /// through the immunity the same card granted, which is the only reason the override is not
        /// decoration.
        /// </summary>
        [Test]
        public void TheTurnsCloseSwapsThemBack_ThroughTheImmunityTheCardItselfGranted()
        {
            var turn = new Turn(HanuCardContent.UnexpectedJourney);
            turn.Pick(EffectValue.Hero(Enemy));

            Assert.That(turn.Space(Hanu), Is.EqualTo(EnemyAt));
            Assert.That(turn.Space(Enemy), Is.EqualTo(HanuAt));

            turn.RunToClose();

            Assert.That(turn.Space(Hanu), Is.EqualTo(HanuAt));
            Assert.That(turn.Space(Enemy), Is.EqualTo(EnemyAt));
            Assert.That(turn.Journey.Phase, Is.EqualTo(ActiveEffectPhase.Ended),
                "An End of turn effect expires at the checkpoint it fires at, so it fires once.");
            Assert.That(turn.Journey.EndedAt, Is.EqualTo(TimingCheckpoint.TurnEnded));
        }

        /// <summary>
        /// "regardless of radius", measured rather than argued: the two stand FIVE spaces apart when
        /// the turn ends and the card prints Radius 2. The end-of-turn exchange reads no icon at all -
        /// the hero is named rather than found - so no reach question is asked.
        /// </summary>
        [Test]
        public void TheSwapBackIgnoresEveryReach()
        {
            var close = new Close(
                HanuActiveEffectContent.UnexpectedJourneyDefinition,
                HanuCardContent.UnexpectedJourney,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 5));

            close.Run();

            Assert.That(close.Space(Hanu), Is.EqualTo(new HexCoordinate(0, 5)));
            Assert.That(close.Space(Enemy), Is.EqualTo(new HexCoordinate(0, 0)));
        }

        /// <summary>
        /// "it is nothing when either hero is off the board at the turn's end" - the partner lookup
        /// finds a hero standing nowhere, publishes nothing, and p.10 stops the body there. The turn
        /// still closes, which is the half an end-of-turn face must never get wrong.
        /// </summary>
        [Test]
        public void NothingHappensWhenThePartnerIsOffTheBoard_AndTheTurnStillCloses()
        {
            var close = new Close(
                HanuActiveEffectContent.UnexpectedJourneyDefinition,
                HanuCardContent.UnexpectedJourney,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 2),
                partnerOnBoard: false);

            close.Run();

            Assert.That(close.Space(Hanu), Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(close.Pauses, Is.Empty);
            Assert.That(close.Ended, Is.True);
        }

        /// <summary>
        /// p.17's "If you are defeated" - the controller's Defeat cancels the journey before its own
        /// moment arrives, so the two are never swapped back. It reads HANU rather than the hero the
        /// effect is about, which is milestone 7.4's correction seen from a card that finally makes
        /// the two different heroes.
        /// </summary>
        [Test]
        public void HanusOwnDefeatCancelsTheJourneyBeforeItFires()
        {
            var close = new Close(
                HanuActiveEffectContent.UnexpectedJourneyDefinition,
                HanuCardContent.UnexpectedJourney,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 2));

            // The producer is p.13's own: AttackDefenseCardActionStrategyBackend raises this
            // checkpoint naming the defeated hero when a Defeat is rewarded.
            close.RaiseDefeat(Hanu);
            close.Run();

            Assert.That(close.Journey.Phase, Is.EqualTo(ActiveEffectPhase.Ended));
            Assert.That(close.Journey.EndedAt,
                Is.EqualTo(TimingCheckpoint.ControllerHeroDefeated));
            Assert.That(close.Space(Hanu), Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(close.Space(Enemy), Is.EqualTo(new HexCoordinate(0, 2)));
        }

        /// <summary>
        /// The swapped hero's OWN Defeat does not cancel it, which is the other direction of the same
        /// rule: the effect is on Hanu's card and his card is untouched. What decides the outcome is
        /// whether they are standing somewhere when the moment arrives.
        /// </summary>
        [Test]
        public void ThePartnersDefeatDoesNotCancelTheJourney()
        {
            var close = new Close(
                HanuActiveEffectContent.UnexpectedJourneyDefinition,
                HanuCardContent.UnexpectedJourney,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 2));

            close.RaiseDefeat(Enemy);
            close.Run();

            Assert.That(close.Journey.EndedAt, Is.EqualTo(TimingCheckpoint.TurnEnded));
            Assert.That(close.Space(Hanu), Is.EqualTo(new HexCoordinate(0, 2)));
            Assert.That(close.Space(Enemy), Is.EqualTo(new HexCoordinate(0, 0)));
        }

        /// <summary>
        /// HA-Q8, built: Safe Travels' "Move up to 1 space." is Hanu's own optional move at the end of
        /// the turn, AFTER the swap back, and it starts from the space that exchange left him in. The
        /// two faces that print no such sentence ask nothing at all.
        /// </summary>
        [Test]
        public void SafeTravelsOffersItsMoveAfterTheSwapBack_FromWhereTheExchangeLeftHim()
        {
            var close = new Close(
                HanuActiveEffectContent.SafeTravelsDefinition,
                HanuCardContent.SafeTravels,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 3));

            close.Run(answerMove: move => move.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Space(new HexCoordinate(1, 2)))));

            Assert.That(close.Pauses.Select(pause => pause.NodeId.Value),
                Is.EqualTo(new[] { HanuEffectContent.JourneyBackMoveNode }),
                "The swap back asks nothing; the move is the only question the body has.");
            Assert.That(close.Space(Enemy), Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(close.Space(Hanu), Is.EqualTo(new HexCoordinate(1, 2)),
                "One space from (0,3), which is where the exchange had just put him.");
        }

        /// <summary>
        /// "up to" admits zero (Garrus's GA-Q1), so declining is a complete answer and the turn closes
        /// with him standing exactly where the swap back left him.
        /// </summary>
        [Test]
        public void SafeTravelsTrailingMoveAdmitsZero()
        {
            var close = new Close(
                HanuActiveEffectContent.SafeTravelsDefinition,
                HanuCardContent.SafeTravels,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 3));

            close.Run(answerMove: close.StayPut);

            Assert.That(close.Space(Hanu), Is.EqualTo(new HexCoordinate(0, 3)));
            Assert.That(close.Ended, Is.True);
        }

        /// <summary>
        /// The two faces that print no trailing sentence compose no node for one - read off the bodies
        /// rather than inferred from a drive, because an absent node is what makes the drives above
        /// ask nothing.
        /// </summary>
        [Test]
        public void OnlySafeTravelsComposesATrailingMove()
        {
            Assert.That(
                HanuEffectContent.UnexpectedJourneyTriggeredEffect.Definition.Nodes
                    .Any(node => node.Id.Value == HanuEffectContent.JourneyBackMoveNode),
                Is.False);
            Assert.That(
                HanuEffectContent.ThereAndBackAgainTriggeredEffect.Definition.Nodes
                    .Any(node => node.Id.Value == HanuEffectContent.JourneyBackMoveNode),
                Is.False);

            var move = HanuEffectContent.SafeTravelsTriggeredEffect.Definition.Nodes
                .Single(node => node.Id.Value == HanuEffectContent.JourneyBackMoveNode);
            Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo));
        }

        /// <summary>
        /// Two journeys standing at one checkpoint both run. Two Hanus cannot exist in a match, so
        /// this is a pipeline fact rather than a printed one - and it is the fact milestone 8.67 built
        /// after 8.61f measured a checkpoint dropping everything behind its first pause.
        /// </summary>
        [Test]
        public void TwoJourneysAtOneCheckpointBothRun()
        {
            var close = new Close(
                HanuActiveEffectContent.UnexpectedJourneyDefinition,
                HanuCardContent.UnexpectedJourney,
                partner: Enemy,
                hanuAt: new HexCoordinate(0, 0),
                partnerAt: new HexCoordinate(0, 2),
                second: new SecondJourney(
                    HanuActiveEffectContent.SafeTravelsDefinition,
                    HanuCardContent.SafeTravels,
                    SecondEnemy,
                    SecondEnemyAt));

            close.Run(answerMove: close.StayPut);

            Assert.That(close.Space(Enemy), Is.EqualTo(new HexCoordinate(0, 0)),
                "The first journey exchanged Hanu out of the origin and put the hero IT named " +
                "there; the second never touches them.");
            Assert.That(close.Space(SecondEnemy), Is.EqualTo(new HexCoordinate(0, 2)),
                "The second journey then exchanges Hanu - standing where the first left him - with " +
                "the hero IT named.");
            Assert.That(close.Space(Hanu), Is.EqualTo(SecondEnemyAt));
            Assert.That(close.Pauses.Count, Is.EqualTo(1),
                "One trailing move, from the one face that prints one - and it is asked at all " +
                "only because milestone 8.67 carries a checkpoint past its first candidate.");
        }

        /// <summary>
        /// The whole authoritative state through a serialize/deserialize cycle taken WHILE the trailing
        /// move's question stands - which is what a restore, a replay and a rewind each re-run. The
        /// answer then arrives against the restored state and finishes the checkpoint, which is
        /// milestone 8.67's regenerated remainder doing its work on this content.
        /// </summary>
        [Test]
        public void TheTrailingMovesPauseSurvivesARoundTrip()
        {
            var turn = new Turn(HanuCardContent.SafeTravels);
            turn.Pick(EffectValue.Hero(Enemy));
            turn.RunToPauseOrClose();

            Assert.That(turn.Choice, Is.Not.Null);
            Assert.That(turn.Choice.NodeId.Value,
                Is.EqualTo(HanuEffectContent.JourneyBackMoveNode));

            turn.RoundTrip();

            Assert.That(turn.Choice, Is.Not.Null,
                "A restored pause is still standing, and the turn is still not over.");
            turn.Answer(turn.StayPut(turn.Choice));
            turn.RunToClose();

            Assert.That(turn.Space(Hanu), Is.EqualTo(HanuAt));
            Assert.That(turn.Space(Enemy), Is.EqualTo(EnemyAt));
        }

        // -------------------------------------------------------------------------------------------
        // Composition facts
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate, from both sides: this build registers none of the four contracts this
        /// block declares, holds none of the three end-of-turn bodies, and can deal none of the three
        /// faces - and the deferral list names exactly what is withheld.
        /// </summary>
        [Test]
        public void EveryContractThisBlockDeclaresIsWithheldFromTheBuild()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            var deferred = HanuComponentContent.JourneyDeferredHandlers().ToArray();

            Assert.That(HanuComponentContent.JourneyRegisteredHandlers(), Is.Empty);
            Assert.That(deferred.Select(handler => handler.Reference.CapabilityId.Value).OrderBy(value => value),
                Is.EqualTo(new[]
                {
                    "hanu-journey-partner",
                    "hanu-safe-travels-effect",
                    "hanu-there-and-back-again-effect",
                    "hanu-unexpected-journey-effect"
                }));

            foreach (var handler in deferred)
            {
                Assert.That(build.Capabilities.Registered.Contains(handler.Reference), Is.False,
                    handler.Reference + " must not be registered until 8.71h deals him.");
            }

            foreach (var body in HanuEffectContent.JourneyTriggeredEffects())
            {
                Assert.That(build.TriggeredEffects.TryGet(body.Reference, out _), Is.False);
            }
        }

        /// <summary>
        /// The decision-label requirement, DERIVED rather than declared: these faces raise no
        /// <c>Choose</c> arm and no confirm-or-decline, so the block owes no words - and
        /// <see cref="HanuEffectContent.JourneyDecisionLabels"/> says exactly that.
        ///
        /// <para>Safe Travels' trailing move is the one thing that could have owed some, and it does
        /// not: <see cref="EffectFailurePolicy.UpTo"/> is a step whose distance is the decision rather
        /// than p.10's confirm-or-decline, which is why Brynn's Hide Traces and Silverarrow's whole
        /// set declare nothing for the same shape.</para>
        /// </summary>
        [Test]
        public void TheseFacesRaiseNoDecisionControlThatOwesWords()
        {
            // MERGE NOTE (8.71 wave): derived over this block's faces, not the integrated aggregate.
            var required = DecisionLabelRequirementQuery
                .For(HanuCardContent.JourneyDefinitions().ToArray())
                .Concat(DecisionLabelRequirementQuery.For(
                    HanuEffectContent.JourneyTriggeredEffects()))
                .ToArray();

            Assert.That(required, Is.Empty,
                "Derived from the bodies themselves: " + string.Join(
                    ", ", required.Select(value => value.ToString())));
            Assert.That(HanuEffectContent.JourneyDecisionLabels(), Is.Empty);
        }

        /// <summary>
        /// The three rows of his shard's printed-inventory table this block owns, transcribed rather
        /// than derived. Each names only the successor this block composes - This Way! and That Way!
        /// are 8.71e's ids and 8.71g completes both pairs.
        /// </summary>
        [Test]
        public void ThePrintedValuesAreHisShardsOwnTable()
        {
            // MILESTONE 8.71g COMPLETED BOTH BLUE FORKS. 8.71d could name only the successors it
            // composed itself; p.15 forks Blue twice and 8.71e's This Way! and That Way! are the other
            // half of each pair, which is what the set-level fixture states whole.
            AssertFace(HanuCardContent.UnexpectedJourney, "Unexpected Journey", CardTier.I, 10, 4, 3,
                2, PrintedValueChannel.None,
                new[] { HanuCardContent.ThereAndBackAgain, HanuCardContent.ThisWay });
            AssertFace(HanuCardContent.ThereAndBackAgain, "There and Back Again", CardTier.II, 11, 4,
                3, 3, PrintedValueChannel.Attack,
                new[] { HanuCardContent.SafeTravels, HanuCardContent.ThatWay });
            AssertFace(HanuCardContent.SafeTravels, "Safe Travels", CardTier.III, 11, 5, 3, 3,
                PrintedValueChannel.Attack, Array.Empty<CardDefinitionId>());

            Assert.That(HanuCardContent.UnexpectedJourneyText,
                Is.EqualTo(HanuCardContent.JourneySentence));
            Assert.That(HanuCardContent.ThereAndBackAgainText,
                Is.EqualTo(HanuCardContent.JourneySentence));
            Assert.That(HanuCardContent.SafeTravelsText,
                Is.EqualTo(HanuCardContent.JourneySentence + " Move up to 1 space."));
            // MILESTONE 8.71g: this block's placeholder is gone and its three faces carry the set's
            // one real revision, which `HanuPrintedSetTests` asserts over all nineteen at once.
            Assert.That(HanuCardContent.JourneyDefinitions().All(face =>
                    face.Reference.ContentRevision == HanuCardContent.ContentRevision),
                Is.True);
            Assert.That(HanuCardContent.AllDefinitions.Select(face => face.Id.Value),
                Is.SupersetOf(HanuCardContent.JourneyDefinitions().Select(face => face.Id.Value)));
        }

        private static void AssertFace(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int movement,
            int radius,
            PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> nextTier)
        {
            var face = HanuCardContent.AllDefinitions.Single(value => value.Id.Equals(id));
            Assert.That(face.DisplayName, Is.EqualTo(displayName));
            Assert.That(face.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Blue));
            Assert.That(face.Progression.Tier, Is.EqualTo(tier));
            Assert.That(face.Progression.ItemIcon, Is.EqualTo(item));
            Assert.That(face.Progression.NextTierCandidateIds, Is.EqualTo(nextTier));

            var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.Id, Is.EqualTo(HanuCardContent.PrimarySkill));
            Assert.That(
                skill.BaseValues.Single(value => value.Channel == PrintedValueChannel.Radius)
                    .Amount,
                Is.EqualTo(radius));
            Assert.That(
                face.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                    .BaseValues.Single().Amount,
                Is.EqualTo(movement));
            Assert.That(
                face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                    .BaseValues.Single().Amount,
                Is.EqualTo(defense));
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates.Select(candidate => candidate.Value.HeroId.Value).ToArray();

        private static bool IsImmune(
            Probe probe, RuleModificationSet rules, HeroId target, RuleActivity activity,
            HeroId actor) =>
            ImmunityQuery.IsImmuneTo(
                RuleObject.ForHero(target), activity, RuleObject.ForHero(actor),
                probe.Board, probe.Participants, rules);

        private static MatchParticipants Participants() =>
            new MatchParticipants(
                new[]
                {
                    new Team(RedTeam, TeamSide.Red),
                    new Team(BlueTeam, TeamSide.Blue)
                },
                new[]
                {
                    new Player(HanuPlayer, RedTeam),
                    new Player(AllyPlayer, RedTeam),
                    new Player(EnemyPlayer, BlueTeam),
                    new Player(SecondEnemyPlayer, BlueTeam)
                },
                new[]
                {
                    new Hero(Hanu, HanuPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, AllyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, EnemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(SecondEnemy, SecondEnemyPlayer, HeroDashboard.RulebookLayout)
                });

        /// <summary>
        /// The bounded catalog this block's content runs against: everything the build registers, plus
        /// the four contracts 8.71d declares and the three end-of-turn bodies they name. Nothing
        /// production registers is changed by composing one here - milestone 8.40j's BA-18 is why the
        /// GroupBy is there rather than a plain concat.
        /// </summary>
        private static MatchContentCatalogs Catalogs()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            return new MatchContentCatalogs(
                new EffectCapabilityCatalog(
                    build.Capabilities.Registered.Select(build.Capabilities.Load)
                        .Concat(HanuComponentContent.JourneyDeferredHandlers())
                        .GroupBy(handler => handler.Reference)
                        .Select(group => group.First())),
                build.Components,
                new TriggeredEffectCatalog(
                    build.TriggeredEffects.Definitions
                        .Concat(HanuEffectContent.JourneyTriggeredEffects())));
        }

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius) yield return new HexCoordinate(q, r);
                }
            }
        }

        private static BoardMapDefinition MapDefinition() =>
            BoardMapDefinition.CreateSynthetic(Map, 1, Disk(7).ToArray());

        /// <summary>
        /// Initiative 1 so Hanu's Blue faces - 10 and 11 - are always the turn under test; Defense 9
        /// so nothing here turns into an Attack this block is not about.
        /// </summary>
        private static readonly CardDefinition Wall = new CardDefinition(
            new CardDefinitionId("hanu-journey-wall"), "hanu-journey-fixture-rev-1",
            "TEST - Hanu journey wall", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("wall-move"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)),
                new ActionDefinition(
                    new ActionDefinitionId("wall-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        private static CardDefinition Face(CardDefinitionId id) =>
            HanuCardContent.AllDefinitions.Single(value => value.Id.Equals(id));

        /// <summary>
        /// One Skill body, started at the effect frame - the offer, the exchange and the creation,
        /// without the turn around them. Milestone 8.35z46's own reason: nothing of his is registered,
        /// so a staged configuration cannot deal one of these cards.
        /// </summary>
        private sealed class Probe
        {
            internal readonly AdjacentMoveAuthority Board;
            internal readonly MatchParticipants Participants;
            internal readonly ActiveEffectLifecycle Timing = new ActiveEffectLifecycle();
            internal readonly HeroItemState Items;
            private readonly EffectResolutionContext context;
            internal EffectResolutionStepResult Step;

            internal Probe(
                CardDefinitionId cardId,
                PrintedValueChannel item = PrintedValueChannel.None,
                HeroId immuneHero = default)
            {
                Participants = HanuJourneyTests.Participants();
                HeroDashboard.RulebookLayout.TryFindSlotAccepting(
                    item == PrintedValueChannel.None ? PrintedValueChannel.Radius : item,
                    out var slot);
                Items = HeroItemState.Restore(Participants, new HeroItemRestoreState(
                    item == PrintedValueChannel.None
                        ? Array.Empty<HeroItem>()
                        : new[]
                        {
                            new HeroItem(Hanu, new CardInstanceId("hanu-item"), slot.Position, item, 1)
                        }));

                var face = Face(cardId);
                var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
                var source = CardInstance.CreateInHand(
                    new CardInstanceId("hanu-probe-source"), face.Reference, Hanu);
                CardLifecycleAuthority.TryPlayFacedown(source);
                CardLifecycleAuthority.TryReveal(source);
                var cards = new[] { source };

                Board = new AdjacentMoveAuthority(
                    MapDefinition(),
                    new[]
                    {
                        new HeroOccupancy(Hanu, HanuAt),
                        new HeroOccupancy(Ally, AllyAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(SecondEnemy, SecondEnemyAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                if (immuneHero.IsDefined)
                {
                    // A standing immunity of somebody else's, so the narrowing this block measures is
                    // p.18's own rule rather than the one Hanu's card is about to grant.
                    Timing.Create(
                        new ActiveEffectId("hanu-probe-standing-immunity"),
                        new ActiveEffectDefinition(
                            "hanu-probe-immunity", ActiveEffectAffectedScopeKind.ControllerHero,
                            ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted,
                            TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd,
                            new[] { ActiveEffectCancellationCondition.Explicit }, false,
                            "HanuProbeImmunity", "hanu-probe-immunity",
                            Array.Empty<TimingCheckpoint>(),
                            new[] { RuleModification.ForImmunity(RuleActivity.All) }),
                        immuneHero, TeamSide.Blue, default,
                        new CardInstanceId("hanu-probe-immunity-source"), skill.Id, 1, 1);
                }

                var catalogs = Catalogs();
                context = new EffectResolutionContext(
                    skill.EffectRoot, catalogs.Capabilities, Participants, Timing, 1, 1, Rules,
                    EffectCardState.Of(HanuCardContent.AllDefinitions, cards), skill.Kind,
                    components: new MatchComponents(
                        catalogs.Components, Board, new MarkerAttachmentRegister(), Board, Board,
                        Board, Board),
                    heroItems: Items);
                Step = EffectResolutionSession.Start(
                    new EffectResolutionId("hanu-journey-probe"), source.Id, face.Reference,
                    skill.Id, Hanu, Hanu, ActionInvocationContext.PrintedPrimary, context);
                Assert.That(Step.Accepted, Is.True);
                Record();
            }

            internal RuleModificationSet Rules => RuleModificationQuery.Build(
                Timing, Board, Participants, new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, Items, null);

            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;

            /// <summary>The one Hero-scoped occurrence this Skill created.</summary>
            internal ActiveEffectInstance Journey => Timing.Effects.Single(effect =>
                effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero);

            internal HexCoordinate Space(HeroId hero) =>
                Board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(hero)).Coordinate;

            /// <summary>Every node event this resolution has raised, in the order it raised them.</summary>
            private readonly List<EffectResolutionEvent> log = new List<EffectResolutionEvent>();

            internal EffectValue CompletedOutput(string nodeId, string outputName) =>
                log.Single(value => value.NodeId.Value == nodeId)
                    .Outputs.Single(fact => fact.OutputName == outputName).Value;

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
                Record();
            }

            private void Record()
            {
                if (Step.Events != null) log.AddRange(Step.Events);
            }
        }

        /// <summary>One whole round: Hanu plays the face, and the turn is then carried to its close.</summary>
        private sealed class Turn
        {
            private CardDefinition[] definitions;
            private CardInstance[] cards;

            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;

            internal Turn(CardDefinitionId cardId)
            {
                var face = Face(cardId);
                Board = new AdjacentMoveAuthority(
                    MapDefinition(),
                    new[]
                    {
                        new HeroOccupancy(Hanu, HanuAt),
                        new HeroOccupancy(Ally, AllyAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(SecondEnemy, SecondEnemyAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                var source = CardInstance.CreateInHand(
                    new CardInstanceId("hanu-source"), face.Reference, Hanu);
                cards = new[]
                {
                    source,
                    CardInstance.CreateInHand(
                        new CardInstanceId("ally-commit"), Wall.Reference, Ally),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-commit"), Wall.Reference, Enemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("second-enemy-commit"), Wall.Reference, SecondEnemy)
                };
                definitions = HanuCardContent.AllDefinitions.Concat(new[] { Wall }).ToArray();

                var markers = new MarkerAttachmentRegister();
                var participants = HanuJourneyTests.Participants();
                Round = new RoundResolutionAuthority(
                    definitions, cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForMatch(
                        new CardActionResolutionAuthority(Catalogs(), Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null,
                        startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));

                Commit(Hanu, source.Id);
                Commit(Ally, new CardInstanceId("ally-commit"));
                Commit(Enemy, new CardInstanceId("enemy-commit"));
                Commit(SecondEnemy, new CardInstanceId("second-enemy-commit"));

                Assert.That(Round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(Hanu),
                    "The three other seats commit Initiative 1, so his Blue face is the highest " +
                    "committed and the turn under test is his.");

                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, cards, Hanu, source.Id)
                    .Single(candidate =>
                        candidate.ActionDefinitionId.Equals(HanuCardContent.PrimarySkill));
                Observe(Round.TryStartSkill(new ActionResolutionId("hanu-journey-action"), offer));
            }

            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;

            internal ActiveEffectInstance Journey => Round.Timing.Effects.Single(effect =>
                effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero);

            internal HexCoordinate Space(HeroId hero) =>
                Board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(hero)).Coordinate;

            internal void Pick(EffectValue value)
            {
                var id = Choice.ChoiceId;
                Answer(Choice.SelectInvocations.Single(
                    command => command.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }

            internal void Answer(EffectChoiceCommand command) =>
                Observe(Round.TryAnswerEffectChoice(command));

            /// <summary>"up to 1" admits zero: naming the space he already stands in.</summary>
            internal EffectChoiceCommand StayPut(PendingEffectChoice move) =>
                move.SelectInvocations.Single(command =>
                    command.Candidate.Value.Equals(EffectValue.Space(Space(Hanu))));

            /// <summary>Carries the round forward until the end-of-turn body asks something, or the turn ends.</summary>
            internal void RunToPauseOrClose() => Drive(stopAtPause: true);

            internal void RunToClose() => Drive(stopAtPause: false);

            private void Drive(bool stopAtPause)
            {
                for (var step = 0; step < 60; step++)
                {
                    var turn = Round.CurrentTurnAuthority;
                    if (turn == null || turn.IsEnded) return;

                    if (turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        Observe(Round.TryChooseTiedActor(
                            tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                        continue;
                    }

                    if (Choice != null)
                    {
                        if (stopAtPause) return;
                        Answer(Choice.SelectInvocations.Count > 0
                            ? Choice.SelectInvocations[0]
                            : Choice.ConfirmInvocation);
                        continue;
                    }

                    if (turn.PendingTurnRelease != null)
                    {
                        Observe(Round.TryReleaseTurn());
                        continue;
                    }

                    if (!turn.SelectedActor.CardInstanceId.IsDefined) return;
                    Observe(Round.TryPerformHold(turn.SelectedActor.CardInstanceId));
                }
            }

            /// <summary>
            /// The whole authoritative state through a serialize/deserialize cycle - which is what a
            /// restore, a replay and an Undo each re-run.
            /// </summary>
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    Catalogs(), bytes, definitions, new[] { Board.MapDefinition },
                    Array.Empty<QuickGameMinionSetupState>());

                Assert.That(
                    AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(bytes),
                    "A payload that does not re-serialize to itself is a payload that lost " +
                    "something on the way through.");

                Round = restored.Round;
                Board = restored.Board;
                cards = restored.Cards.ToArray();
            }

            private void Commit(HeroId hero, CardInstanceId card) =>
                Observe(Round.TryCommitCard(hero, card));

            private void Observe(RoundResolutionResult result) =>
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" +
                    result.CardActionRejectionReason);
        }

        /// <summary>A second journey to stand at the same checkpoint.</summary>
        private sealed class SecondJourney
        {
            internal SecondJourney(
                ActiveEffectDefinition definition,
                CardDefinitionId cardId,
                HeroId partner,
                HexCoordinate partnerAt)
            {
                Definition = definition;
                CardId = cardId;
                Partner = partner;
                PartnerAt = partnerAt;
            }

            internal ActiveEffectDefinition Definition { get; }
            internal CardDefinitionId CardId { get; }
            internal HeroId Partner { get; }
            internal HexCoordinate PartnerAt { get; }
        }

        /// <summary>
        /// One turn's close with journeys already standing, and the two heroes placed wherever the
        /// case needs them. It is the shape <c>SilverarrowEndOfTurnDriveTests</c> composes for the
        /// same reason: what these cases are about is the moment the trigger fires, and staging the
        /// board directly is the only way to put the pair five spaces apart or to take one of them off
        /// the board entirely.
        /// </summary>
        private sealed class Close
        {
            private readonly TurnResolutionAuthority turn;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly AdjacentMoveAuthority board;

            internal readonly List<PendingEffectChoice> Pauses = new List<PendingEffectChoice>();

            internal Close(
                ActiveEffectDefinition definition,
                CardDefinitionId cardId,
                HeroId partner,
                HexCoordinate hanuAt,
                HexCoordinate partnerAt,
                bool partnerOnBoard = true,
                SecondJourney second = null)
            {
                var participants = HanuJourneyTests.Participants();
                var face = Face(cardId);
                var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
                var source = CardInstance.CreateInHand(
                    new CardInstanceId("hanu-close-source"), face.Reference, Hanu);

                var held = new List<CardInstance> { source };
                foreach (var hero in new[] { Hanu, Ally, Enemy, SecondEnemy })
                {
                    held.Add(CardInstance.CreateInHand(
                        new CardInstanceId(hero.Value + "-commit"), Wall.Reference, hero));
                }

                // Every hero stands somewhere unless the case is about one who does not: a hero off
                // the board is offered p.13's respawn when their own turn comes, which the drive
                // below declines so they stay off it for the whole turn - which is exactly the state
                // HA-Q11 names.
                var places = new Dictionary<HeroId, HexCoordinate>
                {
                    { Hanu, hanuAt },
                    { Ally, new HexCoordinate(-1, 0) },
                    { Enemy, new HexCoordinate(-2, 0) },
                    { SecondEnemy, new HexCoordinate(-3, 0) }
                };
                places[partner] = partnerAt;
                if (second != null) places[second.Partner] = second.PartnerAt;
                if (!partnerOnBoard) places.Remove(partner);
                var occupancies = places
                    .Select(place => new HeroOccupancy(place.Key, place.Value))
                    .ToList();

                board = new AdjacentMoveAuthority(
                    MapDefinition(), occupancies, Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                var catalogs = Catalogs();
                var cards = held.ToArray();
                var definitions = HanuCardContent.AllDefinitions.Concat(new[] { Wall }).ToArray();

                timing.Create(
                    new ActiveEffectId("hanu-journey-standing"), definition, Hanu, TeamSide.Red,
                    partner, source.Id, skill.Id, 1, 1);
                if (second != null)
                {
                    var secondFace = Face(second.CardId);
                    var secondSkill = secondFace.ActionDefinitions
                        .Single(action => action.Kind == ActionKind.Skill);
                    var secondSource = CardInstance.CreateInHand(
                        new CardInstanceId("hanu-close-second-source"), secondFace.Reference, Hanu);
                    cards = cards.Concat(new[] { secondSource }).ToArray();
                    timing.Create(
                        new ActiveEffectId("hanu-journey-standing-second"), second.Definition, Hanu,
                        TeamSide.Red, second.Partner, secondSource.Id, secondSkill.Id, 1, 1);
                }

                // p.7 needs one revealed card a seat before a turn may start at all.
                foreach (var committed in cards.Where(card => card.DefinitionReference
                    .Equals(Wall.Reference)))
                {
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
                }

                turn = new TurnResolutionAuthority(
                    definitions, cards, participants, new TieBreakerState(TeamSide.Red),
                    new CardActionResolutionAuthority(catalogs, board), timing, 1, 1);
                Assert.That(turn.TryStart().Accepted, Is.True);
            }

            internal bool Ended => turn.IsEnded;

            /// <summary>
            /// "up to 1" admits zero, and p.11 spells zero as naming the space he is already
            /// standing in - the same answer Brynn's Hide Traces takes of "move up to 2 spaces".
            /// </summary>
            internal EffectChoiceCommand StayPut(PendingEffectChoice move) =>
                move.SelectInvocations.Single(command =>
                    command.Candidate.Value.Equals(EffectValue.Space(Space(Hanu))));

            internal ActiveEffectInstance Journey =>
                timing.Effects.Single(effect => effect.Id.Value == "hanu-journey-standing");

            internal HexCoordinate Space(HeroId hero) =>
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(hero)).Coordinate;

            /// <summary>
            /// p.13's own producer, raised directly: the Attack backend emits this checkpoint naming
            /// the defeated hero when a Defeat is rewarded, and applying it here is what a Defeat
            /// earlier in the turn would have done to a standing effect.
            /// </summary>
            internal void RaiseDefeat(HeroId hero) =>
                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(
                        TimingCheckpoint.ControllerHeroDefeated, 1, 1, hero),
                    new TieBreakerState(TeamSide.Red));

            internal void Run(Func<PendingEffectChoice, EffectChoiceCommand> answerMove = null)
            {
                for (var step = 0; step < 60 && !turn.IsEnded; step++)
                {
                    var respawn = turn.PendingHeroRespawnChoice;
                    if (respawn != null)
                    {
                        // p.13's other answer: a defeated hero may resolve their card without
                        // acting and stay off the board, which is what keeps the partner missing
                        // when the turn's own close arrives.
                        Assert.That(
                            turn.TryAnswerHeroRespawn(respawn.DeclineInvocation).Accepted, Is.True);
                        continue;
                    }

                    var choice = turn.PendingEffectChoice;
                    if (choice != null)
                    {
                        Pauses.Add(choice);
                        var command = answerMove != null
                            ? answerMove(choice)
                            : choice.SelectInvocations.Count > 0
                                ? choice.SelectInvocations[0]
                                : choice.ConfirmInvocation;
                        Assert.That(turn.TryAnswerEffectChoice(command).Accepted, Is.True,
                            "An end-of-turn trigger's own question must be answerable when asked.");
                        continue;
                    }

                    if (turn.PendingChoice != null)
                    {
                        Assert.That(turn.TryChooseTiedActor(
                                turn.PendingChoice.OwningSide,
                                turn.PendingChoice.CandidateCardInstanceIds[0]).Accepted,
                            Is.True);
                        continue;
                    }

                    if (turn.PendingTurnRelease != null)
                    {
                        Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (!turn.SelectedActor.CardInstanceId.IsDefined) return;
                    var held = turn.TryPerformHold(turn.SelectedActor.CardInstanceId);
                    Assert.That(held.Accepted, Is.True,
                        held.RejectionReason + "/" + held.CardActionRejectionReason);
                }
            }
        }
    }
}
