using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Pve.Composition;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-022, PVE-D19. Under the PvE mode rules an enemy made to discard gives up only a purple
    /// card, and is defeated instead when it holds none; without the rules the step is unchanged.
    /// </summary>
    public sealed class PveDiscardPolicyTests
    {
        private static readonly HeroId Enemy = new HeroId("pve-enemy-01");
        private static readonly HeroId Hero = new HeroId("hero-arien");

        [Test]
        public void AnEnemyWithPurpleCardsIsOfferedOnlyThose()
        {
            var raider = PveTestContent.Catalog().Find("raider");
            var hand = PveCardFactory.CreateHand(raider, Enemy, "red-01");
            var result = Execute(hand, bound: true);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates.Select(value => value.CardInstanceId.Value),
                Is.EqualTo(new[] { "red-01-p" }));
        }

        [Test]
        public void AnEnemyWithoutPurpleCardsIsDefeatedInstead()
        {
            var raider = PveTestContent.Catalog().Find("raider");
            var full = PveCardFactory.CreateHand(raider, Enemy, "red-01");
            var orangeOnly = new ConfiguredHand(
                full.Definitions, full.Instances.Where(card => !card.Id.Value.EndsWith("-p")), new CardInstance[0]);
            var result = Execute(orangeOnly, bound: true);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(result.Outcomes.Single(fact => fact.OutputName == SyntheticConsequenceCapabilities.DefeatedHeroOutput)
                .Value.HeroId, Is.EqualTo(Enemy));
        }

        [Test]
        public void WithoutModeRulesTheDiscardIsUnchanged()
        {
            var raider = PveTestContent.Catalog().Find("raider");
            var hand = PveCardFactory.CreateHand(raider, Enemy, "red-01");
            var result = Execute(hand, bound: false);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates.Count, Is.EqualTo(hand.Instances.Count), "Every card in hand is offered.");
        }

        private static EffectExecutionResult Execute(ConfiguredHand hand, bool bound)
        {
            var redTeam = new TeamId("red-team");
            var blueTeam = new TeamId("blue-team");
            var participants = new MatchParticipants(
                new[] { new Team(redTeam, TeamSide.Red), new Team(blueTeam, TeamSide.Blue) },
                new[] { new Player(new PlayerId("red-player-01"), redTeam), new Player(new PlayerId("blue-player-01"), blueTeam) },
                new[]
                {
                    new Hero(Enemy, new PlayerId("red-player-01"), HeroDashboard.RulebookLayout),
                    new Hero(Hero, new PlayerId("blue-player-01"), HeroDashboard.RulebookLayout)
                });
            var roster = EffectRosterState.Of(
                new List<RoundTurnSlot>(),
                new RoundHeroDefeatHistory(participants),
                new HeroCoinLedger(participants),
                modeRules: new Source(bound ? new MatchModeRules(defeatInsteadOfDiscardSide: TeamSide.Red) : null));
            var context = new EffectCapabilityContext(
                new EffectNodeId("pve-discard"),
                EffectFailurePolicy.Mandatory,
                Hero,
                Hero,
                default,
                default,
                new ActionDefinitionId("pve-discard-action"),
                new Dictionary<string, EffectValue>
                {
                    [SyntheticCardStateCapabilities.HeroParameter] = EffectValue.Hero(Enemy)
                },
                1,
                1,
                participants,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(hand.Definitions, hand.Instances),
                ActionKind.Skill,
                rosterState: roster);
            var handler = new EffectCapabilityCatalog(SyntheticCardStateCapabilities.Handlers())
                .Load(SyntheticCardStateCapabilities.DiscardChosenCardV1);
            return handler.Execute(context);
        }

        private sealed class Source : IMatchModeRulesSource
        {
            public Source(MatchModeRules rules)
            {
                ModeRules = rules;
            }

            public MatchModeRules ModeRules { get; }
        }
    }
}
