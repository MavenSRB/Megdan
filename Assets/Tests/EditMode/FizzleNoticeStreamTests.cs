using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35x (the SIXTH amendment's deferred part b, "old M7"): the acknowledge-to-
    /// continue fizzle notice. <see cref="FizzleNoticeStream"/> is exercised directly, the way
    /// <c>KineticCueTests</c> exercises <c>KineticCueStream</c> - a stubbed message composer and
    /// hand-built <see cref="MatchLogEntry"/> rows, no session and no view, so each rule (emit
    /// once, prime silently on first sight, re-prime silently on a rewind, scope to the viewer's
    /// own hero) is asserted in isolation.
    /// </summary>
    public sealed class FizzleNoticeStreamTests
    {
        private static readonly HeroId Viewer = new HeroId("hero-viewer");
        private static readonly HeroId Other = new HeroId("hero-other");

        private static MatchLogEntry BlockedAction(int sequence, HeroId subject, string cardId = "card-1") =>
            new MatchLogEntry(
                sequence, MatchLogEntryKind.BlockedAction, roundIndex: 1, turnIndex: 1,
                subjectHeroId: subject, subjectCardInstanceId: new CardInstanceId(cardId));

        private static string Compose(MatchLogEntry entry) => "MESSAGE:" + entry.Sequence;

        [Test]
        public void FirstSightOfALogAlreadyCarryingTheViewersOwnFizzle_PrimesSilently()
        {
            var stream = new FizzleNoticeStream();
            var log = new List<MatchLogEntry> { BlockedAction(1, Viewer) };

            stream.Observe(log, Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.False,
                "A client's first sight of a match already in progress must not replay a stale " +
                "fizzle as a new one - the same rule KineticCueStream states for its own cues.");
        }

        [Test]
        public void ANewBlockedActionRowNamingTheViewer_LatchesANotice()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);

            stream.Observe(new List<MatchLogEntry> { BlockedAction(1, Viewer) }, Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.True);
            Assert.That(stream.PendingMessage, Is.EqualTo("MESSAGE:1"));
        }

        [Test]
        public void ABlockedActionRowNamingAnotherHero_NeverLatchesForThisViewer()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);

            stream.Observe(new List<MatchLogEntry> { BlockedAction(1, Other) }, Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.False,
                "Acting-player-only scoping: a teammate's or opponent's fizzled resolution is " +
                "never this viewer's notice, derived directly from the log's own SubjectHeroId.");
        }

        [Test]
        public void ANonBlockedActionRow_NeverLatchesEvenForTheViewersOwnHero()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);
            var move = new MatchLogEntry(
                1, MatchLogEntryKind.Move, roundIndex: 1, turnIndex: 1, subjectHeroId: Viewer);

            stream.Observe(new List<MatchLogEntry> { move }, Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.False);
        }

        [Test]
        public void AStandingNotice_SurvivesAFurtherObservationWithNoNewRow()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);
            var log = new List<MatchLogEntry> { BlockedAction(1, Viewer) };
            stream.Observe(log, Viewer, Compose);

            stream.Observe(log, Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.True,
                "Unlike a momentary kinetic cue, the acknowledge-to-continue notice must hold " +
                "through repeated Refresh() calls until the player answers it.");
        }

        [Test]
        public void Acknowledge_DismissesTheStandingNoticeWithoutMovingTheWatermark()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);
            var log = new List<MatchLogEntry> { BlockedAction(1, Viewer) };
            stream.Observe(log, Viewer, Compose);

            stream.Acknowledge();

            Assert.That(stream.HasPendingNotice, Is.False);
            // Observing the SAME log again must not re-latch: the watermark already covers
            // sequence 1, so acknowledging is a one-way dismissal, not a rewind.
            stream.Observe(log, Viewer, Compose);
            Assert.That(stream.HasPendingNotice, Is.False);
        }

        [Test]
        public void ALogThatWentBackwards_RePrimesAndDropsAnyStandingNotice()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);
            var full = new List<MatchLogEntry>
            {
                new MatchLogEntry(1, MatchLogEntryKind.Move, 1, 1, subjectHeroId: Viewer),
                BlockedAction(2, Viewer)
            };
            stream.Observe(full, Viewer, Compose);
            Assert.That(stream.HasPendingNotice, Is.True, "Sanity: the notice latched before the rewind.");

            var rewound = new List<MatchLogEntry>
            {
                new MatchLogEntry(1, MatchLogEntryKind.Move, 1, 1, subjectHeroId: Viewer)
            };
            stream.Observe(rewound, Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.False,
                "An undo/resync/restore that removed the fizzled row from the retained log must " +
                "drop the notice silently, exactly as the board itself snaps to the earlier state " +
                "with no held-over kinetic cue.");
        }

        [Test]
        public void AnEmptyLogAfterANonEmptyOne_RePrimesAndDropsAnyStandingNotice()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);
            stream.Observe(new List<MatchLogEntry> { BlockedAction(1, Viewer) }, Viewer, Compose);
            Assert.That(stream.HasPendingNotice, Is.True);

            stream.Observe(new List<MatchLogEntry>(), Viewer, Compose);

            Assert.That(stream.HasPendingNotice, Is.False);
        }

        [Test]
        public void AnUndefinedViewerHeroId_IsTheDirectorAndLatchesForAnyHero()
        {
            var stream = new FizzleNoticeStream();
            stream.Observe(new List<MatchLogEntry>(), default, Compose);

            stream.Observe(new List<MatchLogEntry> { BlockedAction(1, Viewer) }, default, Compose);

            Assert.That(stream.HasPendingNotice, Is.True,
                "The Director answers for every seat - AwaitedInputQuery.OwesACommitment's own " +
                "undefined-viewer reading - so it owns a notice for ANY hero's fizzled resolution, " +
                "not only a named one.");
        }
    }

    /// <summary>The fizzle notice's fixed vocabulary: the named card case, and the no-blocker generic case.</summary>
    public sealed class FizzleNoticeLabelsTests
    {
        [Test]
        public void Canceled_NamesOnlyTheCardTheMatchLogStates()
        {
            Assert.That(FizzleNoticeLabels.Canceled("MAGNETIC DAGGER"), Is.EqualTo("MAGNETIC DAGGER CANCELED"));
        }

        [Test]
        public void Canceled_RequiresACardName()
        {
            Assert.Throws<ArgumentException>(() => FizzleNoticeLabels.Canceled(string.Empty));
        }

        [Test]
        public void Generic_IsTheNoBlockerFallbackWhenNoCardCanBeNamed()
        {
            // The recorded gap this milestone inherits: MatchLogEntryKind.BlockedAction (and the
            // Core-side ImpossibleMandatory fact it derives from) never names a blocking hero or
            // card, so this vocabulary never invents one. Generic is what a surface draws in the
            // one case a card lookup itself cannot resolve - never a guessed "BLOCKED BY" clause.
            Assert.That(FizzleNoticeLabels.Generic, Does.Not.Contain("BLOCKED BY"));
            Assert.That(FizzleNoticeLabels.Generic, Is.Not.Empty);
        }
    }

    /// <summary>The view model's own equality/empty-state contract.</summary>
    public sealed class FizzleNoticeViewModelTests
    {
        [Test]
        public void None_HasNoNotice()
        {
            Assert.That(FizzleNoticeViewModel.None.HasNotice, Is.False);
        }

        [Test]
        public void AMessage_HasANotice()
        {
            Assert.That(new FizzleNoticeViewModel("X").HasNotice, Is.True);
        }

        [Test]
        public void AnEmptyOrNullMessage_IsTheSameAsNone()
        {
            Assert.That(new FizzleNoticeViewModel(string.Empty).HasNotice, Is.False);
            Assert.That(new FizzleNoticeViewModel(null).HasNotice, Is.False);
        }
    }
}
