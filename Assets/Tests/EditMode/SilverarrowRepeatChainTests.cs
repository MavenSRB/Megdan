using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z46. Rain of Arrows' whole printed sentence, driven on a board:
    /// "Target a unit at maximum range. If you target a hero, repeat once on a different hero; if you
    /// do, repeat once on a minion."
    ///
    /// <para><b>What milestone 8.35z30 left, and what it turned out to be.</b> That milestone composed
    /// the first repeat clause and parked the second on a measured seam limit: a repeated Attack learns
    /// which unit kind it may take by reading its own CARD's declaration, so a card printing two
    /// clauses with different narrowings cannot be told apart. Its costed design named four pieces.
    /// Building it found a SECOND wall the design had not seen, and the second is the one that would
    /// have made the composition silently do nothing: rulebook p.10's "You cannot repeat a repeated
    /// action!" is enforced for card text by milestone 8.25d as one boolean about the enclosing attack,
    /// so the minion clause would have been skipped in silence inside the very instance it exists to
    /// fire in. Measured before anything was written - running Rain of Arrows' own card text with that
    /// flag set records <c>DeclinedOptional</c> against its repeat node and requests nothing.</para>
    ///
    /// <para><b>Both walls have one cause and one answer.</b> Card text is always started with
    /// <see cref="RepeatContinuation.FirstInstance"/>, so the frame's own counter reads zero in every
    /// instance and 8.25d's boolean is a SUBSTITUTE for a counter the card-text path never had. The
    /// answer is to give it one that costs nothing: the chain already mints each instance's identity
    /// by appending to the last one's, so the position is in restorable state already. See
    /// <c>CardTextRepeatChain</c>. No schema version moves, no snapshot field appears, and a card
    /// printing ONE clause keeps the exact walk it had - which
    /// <see cref="NoOtherActionInThisBuild_PrintsMoreThanOneCardTextRepeatClause"/> is the containment
    /// proof for.</para>
    ///
    /// <para><b>"if you do" needed no predicate.</b> The costed design's third piece was an
    /// <c>attack-is-a-repeat@1</c> capability to gate the minion clause on the hero repeat having
    /// happened. It is unnecessary: the minion clause is printed second, so the chain reaches it only
    /// at position one, and position one exists only because the hero repeat happened. That is the
    /// third time in this hero's composition that an "if you do" turned out to be the shape of the step
    /// before it rather than a condition - RULING 37's bound of nought and RULING 8's undefined
    /// binding are the other two.</para>
    /// </summary>
    public sealed class SilverarrowRepeatChainTests
    {
        private static readonly HeroId Attacker =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static readonly HeroId EnemyHero =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static readonly HeroId OtherEnemyHero =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MinionId EnemyMinion = new MinionId("silverarrow-chain-minion");

        /// <summary>
        /// The composition, read off the definition: two clauses, in the order the card prints them,
        /// narrowed to the two kinds the card names. The order is load-bearing rather than cosmetic -
        /// it is what decides which instance may ask which - so it is asserted as a sequence.
        /// </summary>
        [Test]
        public void RainOfArrows_PrintsItsTwoRepeatClausesInPrintedReadingOrder()
        {
            var attack = RainOfArrowsAttack();
            var clauses = (attack.SecondCardTextRoot ?? attack.EffectRoot).Nodes
                .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                .Select(node => node.RepeatTargetBinding)
                .ToArray();

            Assert.That(clauses, Is.EqualTo(new[]
            {
                RepeatTargetBinding.DifferentEnemyHero,
                RepeatTargetBinding.DifferentEnemyMinion
            }));
        }

        /// <summary>
        /// The whole sentence on a board that satisfies every clause: two enemy heroes and an enemy
        /// minion, all standing at exactly the printed Range 3 that RULING 1 makes this Attack's band.
        ///
        /// <para>Three instances, and each one's candidate set is the clause that produced it: the
        /// printed instance admits p.19's units, the first repeat admits enemy heroes and not the one
        /// already taken, and the second admits the minion. A fourth instance is not offered, because
        /// the card prints no third clause.</para>
        /// </summary>
        [Test]
        public void RainOfArrows_ChainsThreeInstances_EachNarrowedToWhatItsOwnClausePrints()
        {
            var chain = Drive(placeSecondEnemyHero: true, placeMinion: true, targetAHeroFirst: true);

            Assert.That(chain.Select(instance => instance.Description).ToArray(), Is.EqualTo(new[]
            {
                // RULING 1's band, unnarrowed: the printed instance takes any unit at exactly Range 3.
                "chain-attack: hero:blue-ally-hero, hero:blue-test-hero, minion:silverarrow-chain-minion",

                // "repeat once on a different hero" - heroes only, and not the one just taken.
                "chain-attack:repeat: hero:blue-test-hero",

                // "repeat once on a minion".
                "chain-attack:repeat:repeat: minion:silverarrow-chain-minion"
            }));
        }

        /// <summary>
        /// RULING 38's first half from the other side: the chain is mandatory where a legal target
        /// exists and skipped where none is. With one enemy hero on the board the hero clause has
        /// nobody left after the printed instance takes them, so the chain is one instance long and the
        /// minion clause is never reached - "if you do" being false.
        /// </summary>
        [Test]
        public void RainOfArrows_StopsAtOneInstance_WhenNoDifferentEnemyHeroIsInTheBand()
        {
            var chain = Drive(placeSecondEnemyHero: false, placeMinion: true, targetAHeroFirst: true);

            Assert.That(chain, Has.Count.EqualTo(1),
                "The hero clause has no subject, so it does not fire - and the minion clause is " +
                "printed as happening only if it did.");
        }

        /// <summary>
        /// "If you target a hero" gating the whole chain: a printed instance that took the minion asks
        /// for nothing at all, which is the condition the card prints in front of both clauses.
        /// </summary>
        [Test]
        public void RainOfArrows_RepeatsNothing_WhenItsPrintedInstanceTookAMinion()
        {
            var chain = Drive(placeSecondEnemyHero: true, placeMinion: true, targetAHeroFirst: false);

            Assert.That(chain, Has.Count.EqualTo(1));
            Assert.That(chain[0].Description, Does.Contain("minion:" + EnemyMinion.Value));
        }

        /// <summary>
        /// The containment proof, and it is what makes the rest of this milestone's engine change safe
        /// to make at all: Rain of Arrows is the ONLY action in this build that prints more than one
        /// card-text repeat clause. Every other face therefore takes the branch that existed before
        /// this milestone, byte for byte - p.10's bar as one boolean and the narrowing derived from the
        /// card alone.
        ///
        /// <para>It walks the six dealt heroes, the seventh, the six the content wave composed but has
        /// not dealt, and the showcase hand, so a second chain arriving anywhere is caught here rather
        /// than by the behaviour of a card nobody was looking at.</para>
        /// </summary>
        [Test]
        public void NoOtherActionInThisBuild_PrintsMoreThanOneCardTextRepeatClause()
        {
            var chains = EveryComposedDefinition()
                .SelectMany(definition => definition.ActionDefinitions
                    .Select(action => new { definition, action }))
                .Where(row => ClauseCount(row.action) > 1)
                .Select(row => row.definition.Id.Value + "|" + row.action.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(chains, Is.EqualTo(new[]
            {
                "silverarrow-rain-of-arrows|primary-ranged-attack"
            }),
                "A second face printing a chain inherits the position rule this milestone wrote, and " +
                "has to say so here before it inherits it silently.");
        }

        // -----------------------------------------------------------------------------------------
        // The drive
        // -----------------------------------------------------------------------------------------

        private sealed class ChainInstance
        {
            public string Description;
        }

        private static ActionDefinition RainOfArrowsAttack() =>
            SilverarrowCardContent.Definition(SilverarrowCardContent.RainOfArrows)
                .ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);

        private static int ClauseCount(ActionDefinition action) =>
            NodesOf(action.EffectRoot).Concat(NodesOf(action.SecondCardTextRoot))
                .Count(node => node.Kind == EffectNodeKind.RepeatAction);

        private static IEnumerable<EffectNodeDefinition> NodesOf(EffectDefinition effect) =>
            effect == null ? Enumerable.Empty<EffectNodeDefinition>() : effect.Nodes;

        private static IEnumerable<CardDefinition> EveryComposedDefinition() =>
            WaspCardContent.AllDefinitions
                .Concat(ArienCardContent.AllDefinitions)
                .Concat(BroganCardContent.AllDefinitions)
                .Concat(SabinaCardContent.AllDefinitions)
                .Concat(TigerclawCardContent.AllDefinitions)
                .Concat(XargathaCardContent.AllDefinitions)
                .Concat(DodgerCardContent.AllDefinitions)
                .Concat(MisaCardContent.AllDefinitions)
                .Concat(SilverarrowCardContent.AllDefinitions)
                .Concat(UrsafarCardContent.AllDefinitions)
                .Concat(WhisperCardContent.AllDefinitions)
                .Concat(TaliCardContent.AllDefinitions)
                .Concat(GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                    .CreateRedWithAttackText(new HeroId("showcase-red")).Definitions);

        /// <summary>
        /// One Rain of Arrows resolution driven to its end, reporting each instance's identity and the
        /// candidate set it was offered.
        ///
        /// <para>The board is built rather than staged, for the reason this file's summary gives: a
        /// staged Silverarrow configuration cannot exist until her deferred contracts are registered.
        /// Every enemy stands at exactly Range 3, which is where RULING 1's band is.</para>
        /// </summary>
        private static IReadOnlyList<ChainInstance> Drive(
            bool placeSecondEnemyHero, bool placeMinion, bool targetAHeroFirst)
        {
            var configuration =
                MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
            var card = SilverarrowCardContent.Definition(SilverarrowCardContent.RainOfArrows);
            var attack = RainOfArrowsAttack();
            var rain = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-chain-rain"), card.Reference, Attacker);

            var definitions = configuration.CardDefinitions.Concat(new[] { card }).ToArray();
            var instances = configuration.CardInstances.Concat(new[] { rain }).ToArray();

            var coordinates = new List<HexCoordinate>();
            for (var q = -4; q <= 4; q++)
            {
                for (var r = -4; r <= 4; r++)
                {
                    if (Math.Abs(q + r) <= 4)
                    {
                        coordinates.Add(new HexCoordinate(q, r));
                    }
                }
            }

            var heroes = new List<HeroOccupancy>
            {
                new HeroOccupancy(Attacker, new HexCoordinate(0, 0)),
                new HeroOccupancy(EnemyHero, new HexCoordinate(3, 0))
            };
            if (placeSecondEnemyHero)
            {
                heroes.Add(new HeroOccupancy(OtherEnemyHero, new HexCoordinate(0, 3)));
            }
            else
            {
                // Off the band entirely, so the card cannot reach them and the hero clause has no
                // second subject. They stay on the board because a hero with no card would change the
                // turn rather than the Attack.
                heroes.Add(new HeroOccupancy(OtherEnemyHero, new HexCoordinate(-1, 0)));
            }

            var minionSpace = new HexCoordinate(-3, 3);
            var board = new AdjacentMoveAuthority(
                coordinates,
                heroes,
                Enumerable.Empty<ObstacleOccupancy>(),
                placeMinion
                    ? new[]
                    {
                        new QuickGameMinionSetupState(
                            EnemyMinion,
                            new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(
                                new MapId("synthetic-coordinate-board"),
                                new SpawnPointId("silverarrow-chain-spawn")),
                            new ConfiguredSpaceReference(
                                new MapId("synthetic-coordinate-board"),
                                new SpaceId("q-3_r3")),
                            minionSpace)
                    }
                    : Enumerable.Empty<QuickGameMinionSetupState>());

            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    SilverarrowActiveEffectContent.Handlers()
                        .Concat(SyntheticRosterCapabilities.DeferredHandlers())),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(
                    SilverarrowEffectContent.TriggeredEffects(
                        SilverarrowCardContent.ContentRevision)));
            var cardActions = new CardActionResolutionAuthority(catalogs, board);

            foreach (var committed in new[]
            {
                rain,
                configuration.CardInstances.First(value =>
                    value.OwnerId.Equals(EnemyHero) && value.State.Location == CardLocation.Hand),
                configuration.CardInstances.First(value =>
                    value.OwnerId.Equals(OtherEnemyHero) && value.State.Location == CardLocation.Hand)
            })
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }

            var turn = new TurnResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);

            var chain = new List<ChainInstance>();
            var attackStarted = false;
            for (var step = 0; step < 60 && !turn.IsEnded; step++)
            {
                if (!attackStarted && turn.PendingChoice != null)
                {
                    var tie = turn.PendingChoice;
                    Assert.That(turn.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(rain.Id)
                                ? rain.Id
                                : tie.CandidateCardInstanceIds[0]).Accepted,
                        Is.True);
                    continue;
                }

                if (!attackStarted && turn.PendingTurnRelease != null)
                {
                    Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                    continue;
                }

                if (!attackStarted && turn.SelectedActor.CardInstanceId.IsDefined)
                {
                    if (!turn.SelectedActor.CardInstanceId.Equals(rain.Id))
                    {
                        Assert.That(
                            turn.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                        continue;
                    }

                    var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                            definitions, instances, Attacker, rain.Id)
                        .Single(value => value.ActionDefinitionId.Equals(attack.Id));
                    Assert.That(
                        turn.TryStartAttack(new ActionResolutionId("chain-attack"), offer).Accepted,
                        Is.True);
                    attackStarted = true;
                    continue;
                }

                if (turn.PendingAttackTargetChoice != null)
                {
                    var choice = turn.PendingAttackTargetChoice;
                    chain.Add(new ChainInstance
                    {
                        Description = choice.ResolutionId.Value + ": " + string.Join(
                            ", ",
                            choice.LegalTargets.Select(Describe)
                                .OrderBy(value => value, StringComparer.Ordinal))
                    });

                    // The printed instance takes whichever kind the case is about; every later
                    // instance has exactly one clause-admitted candidate and takes it.
                    var target = chain.Count == 1 && !targetAHeroFirst
                        ? choice.LegalTargets.First(
                            value => value.Kind == AttackTargetKind.Minion)
                        : choice.LegalTargets.First(
                            value => chain.Count > 1 || value.Kind == AttackTargetKind.Hero);
                    Assert.That(turn.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                        choice.ResolutionId, choice.ChoiceId, target)).Accepted, Is.True);
                    continue;
                }

                if (turn.PendingDefenseChoice != null)
                {
                    // The defender answers with a card, so the Attack is survived: a Defeat would
                    // price Levels and coins against a round authority this composition has none of,
                    // and what is under test is the chain rather than p.13.
                    var defense = turn.PendingDefenseChoice;
                    Assert.That(turn.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.SelectCards(
                            defense.ResolutionId,
                            defense.ChoiceId,
                            defense.LegalDefenseOffers[0].SourceCardInstanceId)).Accepted, Is.True);
                    continue;
                }

                if (turn.PendingEffectChoice != null)
                {
                    var effect = turn.PendingEffectChoice;
                    Assert.That(turn.TryAnswerEffectChoice(
                        effect.SelectInvocations.Count > 0
                            ? effect.SelectInvocations[0]
                            : effect.ConfirmInvocation).Accepted, Is.True);
                    continue;
                }

                break;
            }

            Assert.That(attackStarted, Is.True, "Rain of Arrows never reached its own action.");
            return chain;
        }

        private static string Describe(AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? "hero:" + target.HeroId.Value
                : "minion:" + target.MinionId.Value;
    }
}
