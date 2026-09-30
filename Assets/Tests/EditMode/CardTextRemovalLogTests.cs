using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.15. A card-text Removal's one published row, end to end through the production
    /// translation: the minion a p.19 Remove reports on its result becomes a
    /// <see cref="CardActionResolutionEventKind.MinionRemovalCompleted"/> fact, which becomes a
    /// rule-event log line, which becomes exactly one seat-public MinionRemoved row naming the minion
    /// and the acting hero - the same row a Defeat's removal has always produced.
    /// </summary>
    [TestFixture]
    public sealed class CardTextRemovalLogTests
    {
        private static readonly HeroId Actor = new HeroId("hero-ignatia");
        private static readonly MinionId Minion = new MinionId("blue-melee-3");

        [Test]
        public void TheRemovalFact_BecomesOneSeatPublicRowNamingTheMinionAndTheActor()
        {
            var node = new EffectResolutionEvent(
                new EffectNodeId("remove-node"),
                EffectExecutionResultKind.Applied,
                removedMinions: new[] { new RemovedMinion(Minion) });

            var derived = DeriveLog(node);

            Assert.That(derived.Count, Is.EqualTo(1));
            Assert.That(derived[0].Kind, Is.EqualTo(MatchLogEntryKind.MinionRemoved));
            Assert.That(derived[0].SubjectMinionId, Is.EqualTo(Minion));
            Assert.That(derived[0].SubjectHeroId, Is.EqualTo(Actor), "the hero whose text removed it");
            Assert.That(derived[0].Kind, Is.Not.EqualTo(MatchLogEntryKind.MinionDefeated),
                "p.19's Remove is not a Defeat: no coins, no kill, and the row says which it was.");
        }

        [Test]
        public void ANodeThatRemovedNothing_PublishesNoRemovalRow()
        {
            Assert.That(DeriveLog(new EffectResolutionEvent(
                new EffectNodeId("quiet-node"), EffectExecutionResultKind.Applied)), Is.Empty);
        }

        [Test]
        public void TheRemovalFact_CarriesTheTypedRemovalKind_AndNoDefeatOutcome()
        {
            var node = new EffectResolutionEvent(
                new EffectNodeId("remove-node"),
                EffectExecutionResultKind.Applied,
                removedMinions: new[] { new RemovedMinion(Minion) });

            var fact = CardActionResolutionEvent.RemovedMinionFacts(
                new ActionResolutionId("resolution"), new CardInstanceId("source"),
                new ActionDefinitionId("action"), Actor, node).Single();

            Assert.That(fact.Kind, Is.EqualTo(CardActionResolutionEventKind.MinionRemovalCompleted));
            Assert.That(fact.MinionDefeatFactKind, Is.EqualTo(MinionDefeatFactKind.RemovalCompleted));
            Assert.That(fact.TargetMinionId, Is.EqualTo(Minion));
            Assert.That(fact.DefeatOutcomeId.IsDefined, Is.False, "no Defeat outcome exists for a Remove");
            Assert.That(fact.ActorId, Is.EqualTo(Actor));
            Assert.That(fact.StepId.Value, Does.Contain(":remove:").And.Contain(Minion.Value));
        }

        [Test]
        public void OnlyAHandlerThatApplied_MayReportARemoval()
        {
            Assert.That(() => new RemovedMinion(default), Throws.ArgumentException);
            Assert.That(
                EffectExecutionResult.Applied(removedMinions: new[] { new RemovedMinion(Minion) })
                    .RemovedMinions.Single(),
                Is.EqualTo(new RemovedMinion(Minion)));
            Assert.That(EffectExecutionResult.ImpossibleMandatory().RemovedMinions, Is.Empty);
        }

        private static System.Collections.Generic.IReadOnlyList<MatchLogEntry> DeriveLog(
            EffectResolutionEvent node)
        {
            var facts = CardActionResolutionEvent.CardTextNodeFacts(
                new ActionResolutionId("card-text-resolution"),
                new CardInstanceId("card-text-source"),
                new ActionDefinitionId("card-text-action"),
                Actor,
                node);

            var log = new RuleEventLogProjection();
            log.Append(facts.Select(fact => RoundResolutionEvent.FromTurn(
                1, 1, TurnResolutionEvent.FromSkillAction(fact))));

            return MatchLogProjection.Derive(log.Entries, int.MaxValue);
        }
    }
}
