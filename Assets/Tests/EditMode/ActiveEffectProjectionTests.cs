using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.5b's projected fact: which card instances currently source a live Active effect.
    ///
    /// It lives in its own fixture rather than beside the tray that draws it, and deliberately outside
    /// the Presentation-typed fixtures the cross-platform harness excludes, because this is application
    /// authority: the answer has to be the same on Mono and on .NET, and it has to survive restore and
    /// replay. What the tray does with it is <c>MatchHudPresentationTests</c>' subject.
    ///
    /// The producer is synthetic by the milestone's own acceptance rule - the complete transition is
    /// proven before Wasp composes a persistent card at 8.9 - and every case asserts that something is
    /// really live before asserting anything about the answer, because a fixture whose effect never
    /// activated would pass every one of these while proving nothing.
    /// </summary>
    public sealed class ActiveEffectProjectionTests
    {
        [Test]
        public void TheProjectionNamesEachSourceCardWhoseActiveEffectIsLive()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out var sourceCardId);

            var live = RestoredMatch(session).Round.Timing.Effects
                .Where(effect => effect.Phase == ActiveEffectPhase.Active)
                .ToArray();
            Assert.That(live, Is.Not.Empty,
                "The fixture is only meaningful while an effect is actually live.");
            Assert.That(session.Projection.ActiveEffectSourceCardInstanceIds,
                Is.EqualTo(live.Select(effect => effect.SourceCardInstanceId)
                    .Distinct()
                    .OrderBy(id => id.Value, StringComparer.Ordinal)
                    .ToArray()),
                "The projected fact is the lifecycle's own Active instances, deduplicated by source.");
            Assert.That(session.Projection.IsActiveEffectSource(sourceCardId), Is.True);
        }

        [Test]
        public void ADormantOrEndedInstance_IsNotALiveOne()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out _);
            var effects = RestoredMatch(session).Round.Timing.Effects;
            var liveSources = effects
                .Where(effect => effect.Phase == ActiveEffectPhase.Active)
                .Select(effect => effect.SourceCardInstanceId)
                .ToArray();
            var others = effects
                .Where(effect => effect.Phase != ActiveEffectPhase.Active)
                .Select(effect => effect.SourceCardInstanceId)
                .Where(id => !liveSources.Contains(id))
                .Distinct()
                .ToArray();

            foreach (var id in others)
            {
                Assert.That(session.Projection.IsActiveEffectSource(id), Is.False,
                    $"{id.Value} sources only a dormant or ended instance, which is not in effect.");
            }

            // The definition that could create an effect is not the same fact as an instance that has.
            // Every card in hand is one a Skill could still be started from, and none of them is here.
            foreach (var card in session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Hand))
            {
                Assert.That(session.Projection.IsActiveEffectSource(card.Id), Is.False,
                    "A card that has not created an effect is not a source, whatever it could do.");
            }
        }

        [Test]
        public void ADormantInstance_IsAStandingSourceButNotAMechanicallyActiveOne()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADormantActiveEffect(
                out var sourceCardId);
            var effect = RestoredMatch(session).Round.Timing.Effects
                .Single(value => value.SourceCardInstanceId.Equals(sourceCardId));

            Assert.That(effect.Phase, Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(session.Projection.IsActiveEffectSource(sourceCardId), Is.False,
                "Dormant future rules must not feed active values or target auras.");
            Assert.That(session.Projection.StandingActiveEffectSourceCardInstanceIds
                .Contains(sourceCardId), Is.True,
                "The source-card reminder begins when the future effect is created.");
            Assert.That(session.Projection.StandingActiveEffectSourceCardInstanceIds,
                Does.Contain(sourceCardId));
        }

        [Test]
        public void ARestoredPosition_DerivesTheSameSourcesRatherThanRememberingThem()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out _);
            var before = session.Projection.ActiveEffectSourceCardInstanceIds.ToArray();
            Assert.That(before, Is.Not.Empty);

            var snapshot = session.CaptureSnapshot();
            ConfiguredMatchFixtures.PlayOutTheRestOfTheRound(session);
            Assert.That(session.Projection.ActiveEffectSourceCardInstanceIds, Is.Empty,
                "The position moved on, so a restore that merely remembered would be caught here.");
            Assert.That(session.Projection.StandingActiveEffectSourceCardInstanceIds, Is.Empty);

            Assert.That(session.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(session.Projection.ActiveEffectSourceCardInstanceIds, Is.EqualTo(before),
                "A restored position derives the same live sources from the lifecycle it preserved - " +
                "no presentation-owned lifetime, and nothing new on the wire to remember it with.");
            Assert.That(session.Projection.StandingActiveEffectSourceCardInstanceIds,
                Is.EqualTo(before));
        }

        [Test]
        public void OneSourceWithSeveralLiveInstances_IsNamedOnce()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out _);
            var sources = session.Projection.ActiveEffectSourceCardInstanceIds;

            Assert.That(sources.Distinct().Count(), Is.EqualTo(sources.Count),
                "The list is a set of cards, so a card-level indicator can never be ambiguous.");
            Assert.That(sources, Is.Ordered.By(nameof(CardInstanceId.Value)),
                "and its order is a fact rather than an iteration accident.");
        }

        private static RestoredAuthoritativeMatch RestoredMatch(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
    }
}
