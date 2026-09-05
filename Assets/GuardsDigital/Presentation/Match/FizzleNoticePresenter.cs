using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z14-blockerid (the SIXTH amendment's deferred part b, tracked as "old M7"): the
    /// acknowledge-to-continue notice for a resolution step the recorded
    /// <c>EffectExecutionResultKind.ImpossibleMandatory</c> fact says applied nothing mid-
    /// resolution - the printed p.10 fizzle, as distinct from the SIXTH amendment's other half
    /// (milestone 8.35l/8.35s), which greys and explains an offer BEFORE it is taken.
    /// </summary>
    /// <remarks>
    /// <para><b>What this reads, and the gap it used to record.</b> A mandatory step that resolves
    /// nothing stops the whole card action right there (<c>EffectResolutionSession</c>'s
    /// <c>RefuseCapability</c> clears the continuation). Milestone 8.35r's seat-public match log
    /// derives one row from that same event - <c>MatchLogEntryKind.BlockedAction</c> - naming the
    /// acting hero and the card whose resolution stopped. Until milestone 8.35z14-blockerid, that row
    /// carried no blocker: milestone 8.35l's <c>AppliedRuleModification</c>/<c>ActionUnavailability</c>
    /// channel answered "who blocked this" only at OFFER time (the feasibility probe that greys a
    /// control before it is ever pressed). This milestone asks the SAME question the offer-time walk
    /// already asks - the refusing node's own <c>IEffectRefusalExplainingCapabilityHandler</c>, or
    /// failing that the consumer that narrowed a selection to nothing - at the moment
    /// <c>EffectResolutionSession</c> itself records the stop, and carries the answer through
    /// <c>CardActionResolutionEvent</c>, the rule-event log's own references and
    /// <c>MatchLogEntry.BlockingHeroId</c>/<c>BlockingCardInstanceId</c> to this presenter. The
    /// acting-player-only scoping below stays exactly what it was (a seat is exactly one hero here,
    /// and the log's own <c>SubjectHeroId</c> states whose resolution stopped); what changed is that
    /// the blocking hero and card are now named where the authority could explain itself, and left
    /// unstated - honestly, never guessed - exactly as <see cref="ActionUnavailabilityLabels.Generic"/>
    /// already declines to invent a reason the authority never published.</para>
    /// <para><b>Why the match log and nothing else.</b> The same reason
    /// <c>KineticCuePresenter</c> gives: it is the one event stream Presentation can see, it is
    /// derived rather than stored, and milestone 8.35r's own selection contract makes it identical
    /// on every client. <see cref="FizzleNoticeStream"/> borrows that presenter's exact three
    /// rules - emit once, prime silently on first sight, re-prime silently on a rewind - rather
    /// than sharing its code, so this stays clear of <c>KineticCuePresenter.cs</c> while a parallel
    /// milestone reworks it.</para>
    /// <para><b>Privacy.</b> A blocking source is always a standing PUBLIC Active effect (rulebook
    /// p.17), so naming its controller and card reveals nothing a viewer's own projection does not
    /// already carry as one of <c>ActiveEffectSourceCardInstanceIds</c> - the same public-by-
    /// construction reasoning the match log's own selection contract already applies to every other
    /// row.</para>
    /// </remarks>
    public readonly struct FizzleNoticeViewModel : IEquatable<FizzleNoticeViewModel>
    {
        /// <summary>No notice stands.</summary>
        public static readonly FizzleNoticeViewModel None = default;

        public FizzleNoticeViewModel(string message)
        {
            Message = string.IsNullOrEmpty(message) ? null : message;
        }

        /// <summary>Whether a notice is standing, awaiting the viewer's OK.</summary>
        public bool HasNotice => Message != null;

        /// <summary>The one line the notice states, or null while none stands.</summary>
        public string Message { get; }

        public bool Equals(FizzleNoticeViewModel other) => Message == other.Message;
        public override bool Equals(object obj) => obj is FizzleNoticeViewModel other && Equals(other);
        public override int GetHashCode() => Message?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// The fizzle notice's own fixed vocabulary, alongside <see cref="ActionUnavailabilityLabels"/>
    /// rather than folded into it: that class names why a control is greyed BEFORE it is taken,
    /// and this names what just happened to one that WAS taken and turned out to apply nothing.
    /// </summary>
    public static class FizzleNoticeLabels
    {
        /// <summary>
        /// The named absence: a fizzled resolution whose own card could not be resolved from the
        /// projection (should not happen in practice - every <c>BlockedAction</c> row names a card
        /// the whole table can see - but a surface must still say something honest rather than
        /// throw if it ever does).
        /// </summary>
        public const string Generic = "THAT DID NOTHING — CANCELED";

        /// <summary>
        /// The ordinary case: the one fact the match log states, and no invented activity or
        /// blocker beside it.
        /// </summary>
        public static string Canceled(string cardDisplayName)
        {
            if (string.IsNullOrWhiteSpace(cardDisplayName))
            {
                throw new ArgumentException(
                    "A fizzle notice names the card whose resolution stopped.", nameof(cardDisplayName));
            }

            return $"{cardDisplayName} CANCELED";
        }

        /// <summary>
        /// Milestone 8.35z14-blockerid: the fizzled card's own line, plus the SAME "BLOCKED BY" reason
        /// milestone 8.35l's cursor tooltip already prints for the offer-time greying - reused rather
        /// than restated, so the two halves of the SIXTH amendment speak in one voice.
        /// </summary>
        public static string CanceledBlockedBy(
            string cardDisplayName, string blockingHeroDisplayName, string blockingCardDisplayName) =>
            $"{Canceled(cardDisplayName)} — " +
            ActionUnavailabilityLabels.BlockedBy(blockingHeroDisplayName, blockingCardDisplayName);
    }

    /// <summary>
    /// The watermark and row-to-notice translation, with no session and no view attached - the
    /// same separation <c>KineticCueStream</c> draws, for the same reason: every rule that could
    /// get this wrong (emit once, prime on first sight, re-prime on a rewind) lives here, where a
    /// test can hand it the exact rows it means without standing up an <see cref="IMatchReadModel"/>.
    /// </summary>
    public sealed class FizzleNoticeStream
    {
        private const int NeverObserved = -1;

        private int observedSequence = NeverObserved;
        private string pendingMessage;

        /// <summary>Whether an acknowledge-to-continue notice is standing right now.</summary>
        public bool HasPendingNotice => pendingMessage != null;

        /// <summary>The standing notice's own line, or null while none stands.</summary>
        public string PendingMessage => pendingMessage;

        /// <summary>
        /// One observation of the seat-public match log. A row this viewer has not seen before,
        /// naming this viewer's own hero as the one whose resolution stopped, latches a notice; an
        /// undefined <paramref name="viewerHeroId"/> is the Director, who answers for every seat
        /// and so owns every hero's fizzle, the same reading <c>AwaitedInputQuery.OwesACommitment</c>
        /// gives its own undefined-viewer branch. The latch survives further observations
        /// (including ones that see no new row) until <see cref="Acknowledge"/> is called or the
        /// log itself goes backwards.
        /// </summary>
        public void Observe(
            IReadOnlyList<MatchLogEntry> log,
            HeroId viewerHeroId,
            Func<MatchLogEntry, string> composeMessage)
        {
            if (composeMessage == null)
            {
                throw new ArgumentNullException(nameof(composeMessage));
            }

            if (log == null || log.Count == 0)
            {
                // A cleared log is a restore to a point before anything happened; re-prime rather
                // than hold a watermark - or a notice - no future row can exceed or explain.
                if (observedSequence != 0 && observedSequence != NeverObserved)
                {
                    pendingMessage = null;
                }

                observedSequence = 0;
                return;
            }

            var highest = 0;
            for (var index = 0; index < log.Count; index++)
            {
                if (log[index] != null && log[index].Sequence > highest)
                {
                    highest = log[index].Sequence;
                }
            }

            if (observedSequence == NeverObserved || highest < observedSequence)
            {
                // First sight of a match already in progress, or a log that went backwards - an
                // undo, a resync, a restore. Both re-prime the watermark silently and drop any
                // standing notice: a client that just arrived did not just watch this fizzle, and
                // a state the board already snapped back to owns no stale acknowledgment.
                observedSequence = highest;
                pendingMessage = null;
                return;
            }

            if (highest == observedSequence)
            {
                return;
            }

            for (var index = 0; index < log.Count; index++)
            {
                var entry = log[index];
                if (entry == null || entry.Sequence <= observedSequence)
                {
                    continue;
                }

                // The Director's own viewer hero is undefined, and the Director answers for every
                // seat - the same reading AwaitedInputQuery.OwesACommitment gives - so an undefined
                // viewer owns every hero's fizzle, while a seat owns only its own.
                var ownsThisFizzle = entry.Kind == MatchLogEntryKind.BlockedAction &&
                    (!viewerHeroId.IsDefined || entry.SubjectHeroId.Equals(viewerHeroId));
                if (ownsThisFizzle)
                {
                    pendingMessage = composeMessage(entry);
                }
            }

            observedSequence = highest;
        }

        /// <summary>The viewer's OK: dismisses the standing notice without moving the watermark.</summary>
        public void Acknowledge()
        {
            pendingMessage = null;
        }
    }

    /// <summary>
    /// Wires <see cref="FizzleNoticeStream"/> to a live session and to the contextual region's own
    /// notice host on <see cref="RoundActionUnityView"/>. Composed and refreshed exactly like
    /// <c>KineticCuePresenter</c>, deliberately kept out of <c>MatchFeatureCoordinator</c> so this
    /// milestone's edits stay clear of the file a parallel milestone is reworking; <see
    /// cref="ConfiguredMatchSceneRuntime.RefreshAll"/> calls it directly instead.
    /// </summary>
    public sealed class FizzleNoticePresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly RoundActionUnityView view;
        // Milestone 8.35z8 (integration): NOT readonly, for the reason `ISeatScopedPresenter`
        // states. This branch was written against `983aa205`, before milestone 8.35z4 existed, so
        // it stored its viewer identity exactly the way the two presenters that milestone had to
        // repair did - and `ConfiguredMatchSceneRuntime` composes it with `Seat?.HeroId ?? default`.
        // The undefined hero is the DIRECTOR's identity, and this stream's own ownership rule reads
        // an undefined viewer as owning EVERY hero's fizzle, so an unbound seat would have popped an
        // acknowledge-to-continue notice for every other player's blocked action. That is the
        // "yes, everything is mine" defect the first live internet playtest found, in a fourth place.
        private HeroId viewerHeroId;
        private readonly FizzleNoticeStream stream = new FizzleNoticeStream();

        public FizzleNoticePresenter(IMatchClient session, RoundActionUnityView view, HeroId viewerHeroId = default)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.viewerHeroId = viewerHeroId;
            view.FizzleNoticeAcknowledged += stream.Acknowledge;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => viewerHeroId;

        /// <summary>
        /// Milestone 8.35z8 (integration). Binds the viewer identity this notice is scoped on once
        /// the lobby admits the seat.
        /// </summary>
        /// <remarks>
        /// The standing notice is dropped through the stream's own <see
        /// cref="FizzleNoticeStream.Acknowledge"/>, which dismisses WITHOUT moving the watermark -
        /// exactly the two halves this rebinding wants. Anything latched before admission was
        /// latched under the Director's own "I own every hero's fizzle" branch and may name another
        /// player's card, so it must not survive; and the watermark must NOT be rewound, because a
        /// seat that has just been admitted did not watch the fizzles already in the log, which is
        /// the same reading <see cref="FizzleNoticeStream.Observe"/>'s own first-sight branch gives.
        /// </remarks>
        public bool SetHomeHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined || projection == null ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            viewerHeroId = heroId;
            stream.Acknowledge();
            Refresh();
            return true;
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (projection != null)
            {
                stream.Observe(projection.MatchLog, viewerHeroId, entry => ComposeMessage(projection, entry));
            }

            view.RenderFizzleNotice(stream.HasPendingNotice
                ? new FizzleNoticeViewModel(stream.PendingMessage)
                : FizzleNoticeViewModel.None);
        }

        /// <summary>
        /// Milestone 8.35z14-blockerid. Internal rather than private so
        /// <c>FizzleNoticeStreamTests</c> - in the same <c>GuardsDigital.EditModeTests</c> assembly
        /// this Presentation assembly already declares <c>InternalsVisibleTo</c> for - can drive it
        /// directly against a real projection instead of restating its composition rules.
        /// </summary>
        internal static string ComposeMessage(IMatchReadModel projection, MatchLogEntry entry)
        {
            var cardName = CardDisplayName(projection, entry.SubjectCardInstanceId);
            if (cardName == null)
            {
                return FizzleNoticeLabels.Generic;
            }

            // Milestone 8.35z14-blockerid. Named only where BOTH halves of the blocker resolve to a
            // display name. `AppliedRuleModification` always carries a defined controlling hero, but
            // `SourceCardInstanceId` is optional on the type (a continuous source may leave it
            // undefined) - a half-named blocker is read the same honest way an entirely absent one
            // is, rather than printed with a hole in it.
            var blockingHeroName = HeroDisplayName(projection, entry.BlockingHeroId);
            var blockingCardName = CardDisplayName(projection, entry.BlockingCardInstanceId);
            return blockingHeroName != null && blockingCardName != null
                ? FizzleNoticeLabels.CanceledBlockedBy(cardName, blockingHeroName, blockingCardName)
                : FizzleNoticeLabels.Canceled(cardName);
        }

        /// <summary>
        /// The same lookup <c>MatchFeaturePresenters.GetCardDisplayName</c> performs, restated here
        /// rather than shared: that method is <c>private</c> to a class in a file a parallel
        /// milestone is reworking, and this lookup is three lines over a projection every seat
        /// already carries - restating it is cheaper and safer than widening that file's surface
        /// for one caller.
        /// </summary>
        private static string CardDisplayName(IMatchReadModel projection, CardInstanceId cardId)
        {
            if (!cardId.IsDefined)
            {
                return null;
            }

            var card = projection.Cards?.FirstOrDefault(value => value.Id.Equals(cardId));
            if (card == null)
            {
                return null;
            }

            var definition = projection.Definitions?.FirstOrDefault(
                value => value.Reference.Equals(card.DefinitionReference));
            return definition?.DisplayName.ToUpperInvariant();
        }

        /// <summary>
        /// Milestone 8.35z14-blockerid. The same lookup <c>MatchFeaturePresenters.HeroDisplayName</c>
        /// performs against <c>projection.Setup.Heroes</c>, restated here for the same reason
        /// <see cref="CardDisplayName"/> is: that method is <c>private</c> to a file a parallel
        /// milestone is reworking.
        /// </summary>
        private static string HeroDisplayName(IMatchReadModel projection, HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                return null;
            }

            var hero = projection.Setup?.Heroes?.FirstOrDefault(value => value.HeroId.Equals(heroId));
            return hero?.DisplayName.ToUpperInvariant();
        }
    }
}
