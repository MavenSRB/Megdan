using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Play;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-026 (playtest report 2026-10-11, PVE-D02). With the Director playing Blue, the existing HUD
    /// presenters never draw an enemy as the player's own: the tray follows only Blue heroes and
    /// cannot be pointed at an enemy, and while an enemy owns the moment there is no turn control and
    /// no decision window. Without a player side the Director follows every hero, as it always has.
    /// Every other HUD presenter the match scene composes is refreshed alongside, through two rounds
    /// of enemy content, so a PvE shape the existing HUD cannot draw fails here rather than in play.
    /// Unity-only: the presenters are not in the offline harness.
    /// </summary>
    public sealed class PveDirectorPerspectiveTests
    {
        private static readonly string[] ThreeHeroes = { "hero-arien", "hero-brogan", "hero-sabina" };

        [TestCase(21UL)]
        [TestCase(22UL)]
        public void WithBlueAsThePlayerSideTheHudNeverDrawsAnEnemyAsThePlayers(ulong seed)
        {
            var hud = new Hud(PveTestDriver.Start(seed, ThreeHeroes), TeamSide.Blue);
            var enemyMoments = 0;
            hud.Play(seed, () =>
            {
                var match = hud.Driver.Match;
                var viewed = hud.Hands.ViewedHeroId;
                Assert.That(hud.Driver.Session.Controller.IsEnemy(viewed), Is.False,
                    $"r{match.RoundIndex} t{match.TurnIndex} {match.Phase}: the tray shows enemy {viewed.Value}");
                if (DirectorPlayerSide.OwnsTheMoment(match, TeamSide.Blue)) return;

                enemyMoments++;
                var round = hud.Round;
                Assert.That(round.Actions, Is.Empty, "No turn control is offered for an enemy.");
                Assert.That(round.EndTurnInvocation, Is.Null);
                Assert.That(round.InitiativeChoices, Is.Empty);
                Assert.That(round.DefenseChoices, Is.Empty);
                Assert.That(round.EffectChoices, Is.Empty);
                Assert.That(round.HeroRespawnOptions, Is.Empty);
                Assert.That(hud.Window, Is.Null, "No decision window opens for an enemy's decision.");
            });

            Assert.That(enemyMoments, Is.GreaterThan(0), "The run reached moments the enemies owned.");
            var enemy = hud.Driver.Composed.Enemies.First().HeroId;
            Assert.That(hud.Hands.SelectViewedHero(enemy), Is.False, "The tray cannot be pointed at an enemy.");
        }

        [Test]
        public void WithoutAPlayerSideTheDirectorTrayStillFollowsTheEnemies()
        {
            var hud = new Hud(PveTestDriver.Start(21UL, ThreeHeroes), null);
            var followedAnEnemy = false;
            hud.Play(21UL, () => followedAnEnemy |= hud.Driver.Session.Controller.IsEnemy(hud.Hands.ViewedHeroId));
            Assert.That(followedAnEnemy, Is.True, "The Director with no side answers for every hero, as before.");
        }

        private sealed class Hud
        {
            public Hud(PveTestDriver driver, TeamSide? side)
            {
                Driver = driver;
                var client = new DirectorMatchClient(driver.Session);
                HandsView = new CapturingHandsView();
                RoundView = new CapturingRoundView();
                WindowView = new CapturingWindowView();
                Hands = new DirectorHandsPresenter(client, HandsView) { PlayerSide = side };
                RoundPresenter = new RoundActionPresenter(client, RoundView) { DirectorPlayerSide = side };
                WindowPresenter = new DecisionWindowPresenter(client, WindowView) { DirectorPlayerSide = side };
                var sink = new Sink();
                others = new Action[]
                {
                    new BoardInteractionPresenter(client, sink).Refresh,
                    new HeroOverviewsPresenter(client, sink, () => Hands.ViewedHeroId).Refresh,
                    new MatchLogPresenter(client, sink).Refresh,
                    new MinionChoicePresenter(client, sink).Refresh,
                    new VictoryScreenPresenter(client, sink).Refresh,
                    new KineticCuePresenter(client, sink).Refresh,
                    new AttackSpotlightPresenter(client, sink).Refresh
                };
            }

            private readonly Action[] others;

            public PveTestDriver Driver { get; }
            public DirectorHandsPresenter Hands { get; }
            public RoundActionPresenter RoundPresenter { get; }
            public DecisionWindowPresenter WindowPresenter { get; }
            public CapturingHandsView HandsView { get; }
            public CapturingRoundView RoundView { get; }
            public CapturingWindowView WindowView { get; }
            public RoundActionViewModel Round => RoundView.Model;
            public DecisionWindowViewModel Window => WindowView.Model;

            /// <summary>
            /// Plays the encounter one command at a time - the enemies' own steps and random legal
            /// Blue answers - refreshing the presenters and checking after every command.
            /// </summary>
            public void Play(ulong seed, Action check)
            {
                var random = new Random((int)seed);
                for (var step = 0; step < 900 && Driver.Session.Status == PveEncounterStatus.Ongoing; step++)
                {
                    if (Driver.Match.RoundIndex > 2) break;
                    var next = Driver.Session.Peek();
                    if (next.Kind == PveStepKind.Command)
                    {
                        Assert.That(Driver.Session.Step(out _).Accepted, Is.True, next.Note);
                    }
                    else
                    {
                        var answer = BlueAnswer(random);
                        if (answer == null) break;
                        Assert.That(Driver.Session.Execute(answer).Accepted, Is.True, answer.Kind.ToString());
                    }

                    Hands.Refresh();
                    RoundPresenter.Refresh();
                    WindowPresenter.Refresh();
                    foreach (var refresh in others) refresh();
                    check();
                }
            }

            private DirectorCommand BlueAnswer(Random random)
            {
                var prompt = Driver.Session.CurrentPrompt;
                if (prompt != null) return prompt.Answers[random.Next(prompt.Answers.Count)];
                var pending = Driver.Match.PendingSystemTransitionStep;
                if (pending != null)
                {
                    var item = pending.OpenItems.FirstOrDefault(value => value.ChoosingSide == TeamSide.Blue);
                    return item == null
                        ? null
                        : DirectorCommand.AnswerSystemTransitionStep(new SystemTransitionChoiceCommand(
                            pending.InstanceId, pending.StepId, item.ItemId, TeamSide.Blue,
                            item.Options[random.Next(item.Options.Count)]));
                }

                var commands = Driver.Composed.Heroes.SelectMany(Driver.Session.AuthorizedFor)
                    .Where(command => command.Kind != DirectorCommandKind.RetractCommitment &&
                        command.Kind != DirectorCommandKind.BeginSystemTransition &&
                        Driver.Session.Refusal(command) == null)
                    .Distinct()
                    .ToArray();
                return commands.Length == 0
                    ? PvePassivePlayer.NextAnswer(Driver.Session)
                    : commands[random.Next(commands.Length)];
            }
        }

        private sealed class Sink : IBoardInteractionView, IHeroOverviewsView, IMatchLogView, IMinionChoiceView,
            IVictoryScreenView, IKineticCueView, IAttackSpotlightView
        {
            public void Render(BoardInteractionViewModel model) { }
            public void Render(HeroOverviewsViewModel model) { }
            public void Render(MatchLogViewModel model) { }
            public void Render(MinionChoiceViewModel model) { }
            public void Render(VictoryScreenViewModel model) { }
            public void Render(KineticCueViewModel model) { }
            public void Render(AttackSpotlightViewModel model) { }
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }
            public void Render(DirectorHandsViewModel model) => Model = model;
        }

        private sealed class CapturingRoundView : IRoundActionView
        {
            public RoundActionViewModel Model { get; private set; }
            public void Render(RoundActionViewModel model) => Model = model;
        }

        private sealed class CapturingWindowView : IDecisionWindowView
        {
            public DecisionWindowViewModel Model { get; private set; }
            public void Render(DecisionWindowViewModel model) => Model = model;
        }
    }
}
