using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.19. A card-text discard's one published row, end to end through the production
    /// translation: the discard a handler reports on its result becomes a
    /// <see cref="CardActionResolutionEventKind.CardTextDiscardedCard"/> fact, which becomes a
    /// rule-event log line, which becomes exactly one seat-public row naming the card's OWNER and
    /// the card - on the terms milestone 8.40f set for the reveal, which this file's sibling
    /// (BainHiddenGuessTests) proves the same way.
    /// </summary>
    [TestFixture]
    public sealed class CardTextDiscardLogTests
    {
        private static readonly HeroId Actor = new HeroId("hero-snorri");
        private static readonly HeroId Owner = new HeroId("hero-bain");
        private static readonly CardInstanceId Card = new CardInstanceId("blue-bain-drinking-buddies");

        [Test]
        public void TheDiscardFact_BecomesOneSeatPublicRowNamingTheOwnerAndTheCard()
        {
            var node = new EffectResolutionEvent(
                new EffectNodeId("discard-node"),
                EffectExecutionResultKind.Applied,
                discardedCards: new[] { new DiscardedCard(Owner, Card) });

            var derived = DeriveLog(node);

            Assert.That(derived.Count, Is.EqualTo(1));
            Assert.That(derived[0].Kind, Is.EqualTo(MatchLogEntryKind.CardDiscardedByCardText));
            Assert.That(derived[0].SubjectHeroId, Is.EqualTo(Owner),
                "The row is about the hero whose hand the card left, never the acting hero.");
            Assert.That(derived[0].SubjectCardInstanceId, Is.EqualTo(Card));
            Assert.That(derived[0].Kind, Is.Not.EqualTo(MatchLogEntryKind.CardRevealedByCardText),
                "A discard changed the card's state; a reveal only showed it. Two kinds, two arms.");
        }

        [Test]
        public void ANodeThatRevealedAndDiscarded_PublishesBothRowsRevealFirst()
        {
            var revealed = new CardInstanceId("blue-bain-shown");
            var node = new EffectResolutionEvent(
                new EffectNodeId("both-node"),
                EffectExecutionResultKind.Applied,
                revealedCards: new[] { new RevealedCard(Owner, revealed) },
                discardedCards: new[] { new DiscardedCard(Owner, Card) });

            var derived = DeriveLog(node);

            Assert.That(derived.Select(entry => entry.Kind), Is.EqualTo(new[]
            {
                MatchLogEntryKind.CardRevealedByCardText, MatchLogEntryKind.CardDiscardedByCardText
            }));
            Assert.That(derived.Select(entry => entry.SubjectCardInstanceId),
                Is.EqualTo(new[] { revealed, Card }));
        }

        [Test]
        public void ANodeThatDiscardedNothing_PublishesNoDiscardRow()
        {
            var node = new EffectResolutionEvent(
                new EffectNodeId("quiet-node"), EffectExecutionResultKind.Applied);

            Assert.That(DeriveLog(node), Is.Empty);
        }

        [Test]
        public void TheDiscardFact_CarriesTheCardOnItsOwnField_AndTheOwnerAsTheTarget()
        {
            var node = new EffectResolutionEvent(
                new EffectNodeId("discard-node"),
                EffectExecutionResultKind.Applied,
                discardedCards: new[] { new DiscardedCard(Owner, Card) });

            var fact = CardActionResolutionEvent.DiscardedCardFacts(
                new ActionResolutionId("resolution"), new CardInstanceId("source"),
                new ActionDefinitionId("action"), Actor, node).Single();

            Assert.That(fact.Kind, Is.EqualTo(CardActionResolutionEventKind.CardTextDiscardedCard));
            Assert.That(fact.DiscardedCardInstanceId, Is.EqualTo(Card));
            Assert.That(fact.RevealedCardInstanceId.IsDefined, Is.False);
            Assert.That(fact.TargetHeroId, Is.EqualTo(Owner));
            Assert.That(fact.ActorId, Is.EqualTo(Actor));
            Assert.That(fact.StepId.Value, Does.Contain(":discard:").And.Contain(Card.Value));
        }

        [Test]
        public void OnlyAHandlerThatApplied_MayReportADiscard()
        {
            Assert.That(
                () => new DiscardedCard(default, Card),
                Throws.ArgumentException,
                "A discard names its owner.");
            Assert.That(
                EffectExecutionResult.Applied(discardedCards: new[] { new DiscardedCard(Owner, Card) })
                    .DiscardedCards.Single(),
                Is.EqualTo(new DiscardedCard(Owner, Card)));
            Assert.That(EffectExecutionResult.DeclinedOptional().DiscardedCards, Is.Empty);
        }

        private static System.Collections.Generic.IReadOnlyList<MatchLogEntry> DeriveLog(
            EffectResolutionEvent node)
        {
            var facts = CardActionResolutionEvent.RevealedAndDiscardedCardFacts(
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
