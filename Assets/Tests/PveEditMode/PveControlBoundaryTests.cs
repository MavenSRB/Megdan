using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Pve.Play;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-026 (playtest report 2026-10-11, PVE-D02). The encounter never decides for a Blue hero: while
    /// a Blue hero owns the standing decision or is the acting hero, the only automatic steps are the
    /// encounter's own flow, never a choice of that hero's. Blue's moves here are random legal
    /// commands, so the enemies meet attacks, defeats and respawns the passive player never makes.
    /// </summary>
    public sealed class PveControlBoundaryTests
    {
        private static readonly string[] FiveHeroes =
            { "hero-arien", "hero-brogan", "hero-sabina", "hero-tigerclaw", "hero-xargatha" };

        [TestCase(11UL, 2)]
        [TestCase(12UL, 3)]
        [TestCase(13UL, 5)]
        [TestCase(14UL, 1)]
        [TestCase(15UL, 4)]
        [TestCase(21UL, 3)]
        [TestCase(22UL, 3)]
        public void AutomaticStepsNeverDecideForABlueHeroAndNoSideTargetsItsOwn(ulong seed, int heroCount)
        {
            var driver = PveTestDriver.Start(seed, FiveHeroes.Take(heroCount).ToArray());
            var random = new Random((int)seed);
            var violations = new List<string>();
            var automatic = 0;
            for (var step = 0; step < 1500 && driver.Session.Status == PveEncounterStatus.Ongoing; step++)
            {
                var match = driver.Match;
                if (match.RoundIndex > 2) break;
                SideProblem(driver, match, violations);
                // W-026. The existing HUD's card inspection reads every hero's faces at any moment
                // (a board click on an enemy opens it); an enemy's two copies of a card must not collide.
                foreach (var hero in match.Setup.Heroes)
                {
                    Assert.DoesNotThrow(() => HeroCardInspection.ForHero(match, hero.HeroId),
                        $"r{match.RoundIndex} t{match.TurnIndex}: inspecting {hero.HeroId.Value}");
                }

                var next = driver.Session.Peek();
                if (next.Kind == PveStepKind.Command)
                {
                    var blueOwner = BlueDecider(driver, match);
                    if (blueOwner != null)
                    {
                        violations.Add($"r{match.RoundIndex} t{match.TurnIndex} {match.Phase}: '{next.Note}' " +
                            $"({next.Command.Kind}) while {blueOwner}");
                    }

                    var result = driver.Session.Step(out _);
                    Assert.That(result.Accepted, Is.True, next.Note + ": " + result.Detail + " | " + Describe(match));
                    automatic++;
                    continue;
                }

                var answer = RandomBlueAnswer(driver, random);
                if (answer == null) break;
                var answered = driver.Session.Execute(answer);
                Assert.That(answered.Accepted, Is.True, answer.Kind + ": " + answered.Detail);
            }

            Assert.That(automatic, Is.GreaterThan(0));
            Assert.That(violations, Is.Empty, string.Join("\n", violations.Take(10)));
        }

        /// <summary>
        /// The playtest's "friendly creeps are counted as enemy": an attack never offers a unit of the
        /// attacker's own side, and a defender's minion modifiers read its own side's minions as friendly and the other's as enemy.
        /// </summary>
        private static void SideProblem(PveTestDriver driver, IMatchReadModel match, List<string> problems)
        {
            string SideOf(GuardsDigital.Core.Units.Heroes.HeroId hero) =>
                match.Participants.GetTeamForHero(hero).Side.ToString();
            var attack = match.PendingAttackTargetChoice;
            if (attack != null)
            {
                var side = SideOf(attack.ChooserId);
                foreach (var hero in attack.LegalTargetHeroIds.Where(hero => SideOf(hero) == side))
                    problems.Add($"r{match.RoundIndex} t{match.TurnIndex}: {attack.ChooserId.Value} may attack its own side's {hero.Value}");
                foreach (var minion in attack.LegalTargetMinionIds.Where(minion =>
                             match.Minions.Any(value => value.MinionId.Equals(minion) && value.TeamSide.ToString() == side)))
                    problems.Add($"r{match.RoundIndex} t{match.TurnIndex}: {attack.ChooserId.Value} may attack its own side's {minion.Value}");
            }

            var defense = match.PendingDefenseChoice;
            var preview = match.PendingDefensePreview;
            if (defense != null && preview != null)
            {
                var side = SideOf(defense.ChooserId);
                foreach (var support in preview.MinionContributions)
                {
                    var own = match.Minions.Any(minion => minion.MinionId.Equals(support.SourceMinionId) &&
                        minion.TeamSide.ToString() == side);
                    // p.14's minion Defense modifiers: a friendly minion helps, an enemy one hinders.
                    if (own ? support.Amount < 0 : support.Amount > 0)
                        problems.Add($"r{match.RoundIndex} t{match.TurnIndex}: {defense.ChooserId.Value} reads " +
                            $"{support.SourceMinionId.Value} ({support.Amount:+0;-0}) as {(own ? "an enemy" : "a friend")}");
                }
            }

            _ = driver;
        }

        /// <summary>The standing state, for a failure message.</summary>
        private static string Describe(IMatchReadModel m) =>
            $"phase={m.Phase} actor={m.SelectedActor.ActorId.Value} card={m.SelectedActor.CardInstanceId.Value} owner={m.DecisionOwner} " +
            $"move={m.PendingMovementChoice != null}/{m.HasPendingMovementResolution} atk={m.PendingAttackTargetChoice != null}/{m.HasPendingAttackResolution} " +
            $"def={m.PendingDefenseChoice != null} skill={m.HasPendingSkillResolution} effect={m.PendingEffectChoice != null} resolving={m.ResolvingSourceCardInstanceId.Value} " +
            $"displaced={m.PendingDisplacedMinionCorrection != null} body={m.PendingActingBodyChoice != null} removal={m.PendingRemovalPrevention != null} " +
            $"release={m.PendingTurnRelease != null} respawn={m.PendingHeroRespawnChoice != null} offers={m.AvailableTurnActions.Count} hold={m.AvailableHoldOffer.HasValue} " +
            $"contract={m.DecisionContract?.FamilyId}";

        /// <summary>Who on Blue owns what is standing now, or null when no Blue hero does.</summary>
        private static string BlueDecider(PveTestDriver driver, IMatchReadModel match)
        {
            var owner = match.DecisionOwner;
            if (owner.IsDefined)
            {
                if (owner.TeamSide == TeamSide.Blue) return "Blue's team decision " + owner.FamilyId;
                var blue = owner.HeroIds.FirstOrDefault(hero => !driver.Session.Controller.IsEnemy(hero));
                return blue.IsDefined ? blue.Value + " owns " + owner.FamilyId : null;
            }

            var actor = match.SelectedActor;
            return actor.ActorId.IsDefined && !driver.Session.Controller.IsEnemy(actor.ActorId) &&
                   match.Phase != RoundResolutionPhase.AwaitingCommitments
                ? actor.ActorId.Value + " is acting"
                : null;
        }

        private static DirectorCommand RandomBlueAnswer(PveTestDriver driver, Random random)
        {
            var prompt = driver.Session.CurrentPrompt;
            if (prompt != null) return prompt.Answers[random.Next(prompt.Answers.Count)];
            var match = driver.Match;
            var pending = match.PendingSystemTransitionStep;
            if (pending != null)
            {
                var item = pending.OpenItems.FirstOrDefault(value => value.ChoosingSide == TeamSide.Blue);
                return item == null
                    ? null
                    : DirectorCommand.AnswerSystemTransitionStep(new SystemTransitionChoiceCommand(
                        pending.InstanceId, pending.StepId, item.ItemId, TeamSide.Blue,
                        item.Options[random.Next(item.Options.Count)]));
            }

            var commands = driver.Composed.Heroes.SelectMany(driver.Session.AuthorizedFor)
                .Where(command => command.Kind != DirectorCommandKind.RetractCommitment &&
                    command.Kind != DirectorCommandKind.BeginSystemTransition &&
                    driver.Session.Refusal(command) == null)
                .Distinct()
                .ToArray();
            return commands.Length == 0 ? PvePassivePlayer.NextAnswer(driver.Session) : commands[random.Next(commands.Length)];
        }
    }
}
