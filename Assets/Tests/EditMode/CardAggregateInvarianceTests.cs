using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The behaviour-invariance harness for milestone 6.1f. It pins, for six scenarios, a digest of
    /// the authoritative snapshot and a digest of the diagnostic snapshot - the latter being the
    /// serialized ordered rule-event log, so an event that changed order, count, or content moves
    /// its digest.
    /// </summary>
    /// <remarks>
    /// 6.1f is a pure refactor: one card mutation path, one aggregate root, one commit boundary. Its
    /// whole claim is that no observable behaviour changes, and a refactor of that size cannot be
    /// argued safe from a green suite alone - the suites would also pass if the refactor quietly
    /// reordered an event or dropped a field that no assertion happens to name.
    ///
    /// So the digests below were captured at the PRE-refactor revision and are asserted unchanged
    /// afterwards. They are not free-floating expected values: any difference means the refactor
    /// changed authoritative state or the event stream, and the refactor is wrong until explained.
    ///
    /// Each run also writes the measured digests to tmp/ so a mismatch can be diffed rather than
    /// only reported.
    /// </remarks>
    public sealed class CardAggregateInvarianceTests
    {
        // Do not "update to match" a failing run. A difference here means authoritative state or the
        // event stream changed; re-pin only after the delta is explained at the byte level, and record
        // that explanation in the re-pinning commit.
        //
        // Originally captured at pre-refactor revision 18bf74d and re-pinned once, at milestone 6.1x,
        // for the deliberate schema v6 -> v7 change in 6.1s. That delta was located by decoding the
        // payloads either side of the change rather than assumed: two causes, both intended. The
        // serialized version field moved 6 -> 7, and the board section gained an 8-byte coordinate per
        // remaining minion, which is 96 bytes across this setup's twelve minions and accounts exactly
        // for the observed 1972 -> 2068 byte growth. Nothing else moved.
        //
        // 6.1s did not run this fixture: it is in the dotnet harness exclude list, so the Linux harness
        // could not run it, and it was not in the focused Windows filter either. The complete Edit Mode
        // suite caught it afterwards. Any milestone changing CurrentSchemaVersion must run this fixture.
        //
        // Re-pinned a second time at milestone 6.2a for the deliberate schema v7 -> v8 change. Three
        // intended causes, all in the authoritative payload: the version field moved 7 -> 8; a
        // capability-registration section was added after the definition table, recording which
        // capability contract set the payload was written against; and the Active-effect section lost
        // every semantic field - scope, duration, three lifecycle checkpoints, cancellation conditions,
        // the cross-round flag, effect kind, uniqueness key, and trigger checkpoints - because a restore
        // now binds those from the registered capability catalog. These scenarios carry no Active
        // effect, so that third change contributes nothing to their bytes; the first two account for
        // the movement.
        //
        // Both digests in every pair moved, which the milestone plan had not predicted. The reason is
        // benign and worth stating: the diagnostic snapshot is the rule-event log envelope, and that
        // envelope wraps the authoritative payload, so an authoritative change necessarily moves the
        // diagnostic digest with it. The two are not independent measurements of different things.
        //
        // The structural invariants below still hold, which is what makes this re-pin safe rather than
        // merely green: accepted, rejected and stale remain one shared pair, so a refusal still changes
        // nothing; restored and replayed still equal their live counterparts; and the defense-discard
        // family still differs, because a card really moved.
        //
        // Re-pinned a third time at milestone 6.2 for the deliberate schema v8 -> v9 change, with three
        // intended causes again, all in the authoritative payload: the version field moved 8 -> 9; a
        // component-registration section - token references then marker references - was added beside
        // the capability registrations; and the board and round sections each gained a count-prefixed
        // component array. These scenarios place no token and give no marker, so both arrays are empty
        // and contribute a zero count each; the version field and the registration section account for
        // the movement. Every invariant above survived the re-pin unchanged, which is the check that the
        // schema moved and nothing else did.
        //
        // Re-pinned a fourth time at milestone 6.3 without a schema change. The one byte-level cause is
        // the registered synthetic token reference moving from Hazard revision 1 to revision 2, whose
        // trusted definition now supplies immutable token-property content. Every scenario records that
        // registration even though none places a token, so every authoritative digest moves; the
        // diagnostic envelope wraps that payload, so its digest moves too. The equality groups below
        // remain identical, demonstrating that no scenario state or event ordering changed.
        //
        // Re-pinned a fifth time at milestone 6.8, again without a schema change. Two byte-level causes,
        // both in the registration sections the payload writes before any state: the capability
        // registration list gained the five synthetic modification capabilities as (id, revision) pairs,
        // and the registered synthetic token reference moved from Hazard revision 2 to revision 3,
        // whose trusted definition now supplies the typed rule its property declares. Nothing else in
        // the writer moved: the new continuous-source contribution kind is written only where such a
        // contribution exists, and no scenario here has one. The equality groups below are unchanged,
        // which is what makes this a registration delta rather than a behaviour change.
        //
        // Re-pinned a sixth time at milestone 6.8a, again without a schema change, and this time for a
        // content cause: Red Bulwark's printed Primary Attack became its showcase Skill, so that card
        // definition moved from revision phase-5-1a-rev-3 to phase-6-8a-rev-1. The delta was read at the
        // byte level either side of the change rather than assumed. Every payload keeps its exact
        // decoded length and differs in six to eight bytes across exactly two regions - the definition
        // table's Red Bulwark entry and the red-bulwark-card instance's definition reference - because
        // both are the same sixteen-character revision string and nothing shifted. The replaced action
        // set never reaches the wire at all: the payload names definitions and the trusted catalog
        // supplies what they contain. No scenario here commits Red Bulwark, so none of their behaviour
        // could have changed, and the equality groups below are identical, which is the check for that.
        //
        // Re-pinned a seventh time at milestone 6.4, again without a schema change, and again in the
        // registration section the payload writes before any state: the capability registration list
        // gained synthetic-direct-move and synthetic-object-push as (id, revision) pairs, moving from
        // sixteen entries to eighteen. Measured rather than assumed - the same two entries cost exactly
        // 52 bytes in one contiguous region of the payload, with byte-identical common prefix and
        // suffix either side - so every authoritative digest moves and the diagnostic envelope, which
        // wraps that payload alongside the ordered entries, moves with it. That coupling is why the
        // equality groups are the evidence here and the digests alone are not: all five of accepted,
        // rejected, stale, restored, and replayed still share one pair, undo-truncated is still
        // distinct, and the three defense-discard scenarios still share a third, exactly as before. No
        // scenario here commits either displacement Skill card - those live only on the Skill-fixture
        // hands, not the ordinary five these scenarios use - so none of their behaviour could have
        // changed, and the preserved grouping is what demonstrates it.
        //
        // Re-pinned an eighth time at milestone 6.8k, again without a schema change and again in the
        // registration section: the capability list gained synthetic-choose-target and
        // synthetic-direct-move moved from revision 1 to revision 2, because its subject parameter
        // widened from a typed hero to a typed coordinate. Measured on the compatibility corpus at the
        // authoritative-payload layer: exactly 28 bytes - the one new 7-bit length-prefixed
        // 23-character id plus its int32 revision - in one contiguous region with byte-identical
        // prefix and suffix either side; the revision bump moves bytes inside that same region without
        // changing its length. No scenario here commits a displacement or targeting Skill card, and
        // every equality group below is unchanged - accepted, rejected, stale, restored, and replayed
        // still share one pair, undo-truncated is still distinct, and the three defense-discard
        // scenarios still share a third - which is what demonstrates the registration cause and
        // nothing else moved.
        //
        // Re-pinned a ninth time at milestone 6.4a for the deliberate schema v9 -> v10 change: the
        // paused after-action displaced-minion correction joined the card-action section as
        // continuation kind 4. Measured on the compatibility corpus rather than assumed: every
        // carried-over payload keeps its exact decoded length and differs from its v9 predecessor at
        // exactly one byte - the version integer at offset 30 - because the correction block is
        // written only under continuation kind 4 and none of these scenarios displaces a minion. The
        // registration set is unchanged this time: the correction is an authority-level operation, not
        // an effect capability. The equality groups below are identical, which is the check that the
        // schema moved and nothing else did.
        //
        // Re-pinned a tenth time at milestone 6.5, again without a schema change and again in the
        // registration section the payload writes before any state: the capability list gained
        // synthetic-actor-space, synthetic-place, and synthetic-spawn, moving from nineteen entries to
        // twenty-two. Measured on the compatibility corpus at the authoritative-payload layer rather
        // than assumed: exactly 66 bytes - the three 7-bit length-prefixed ids of 21, 15, and 15
        // characters plus an int32 revision each - in one contiguous region with a 452-byte common
        // prefix and every later section byte-identical. The milestone's second cause, the shove cards
        // advancing to phase-6-5-rev-1 as the fixture hand's board-object Skill, reaches none of these
        // scenarios: they commit only the ordinary five-card matrix. Nothing about the unit supply a
        // Spawn draws from reaches the wire either - it is derived on restore from the configured
        // roster minus the restored positions. The equality groups below are identical, which is what
        // demonstrates the registration cause and nothing else moved.
        //
        // Re-pinned an eleventh time at milestone 6.8b, again without a schema change and for the same
        // content cause 6.8a recorded on the other side of the board: Blue Feint's printed Primary
        // Attack became its showcase Skill, so that card definition advanced from phase-5-1a-rev-3 to
        // phase-6-8b-rev-1. Measured on the compatibility corpus at the byte level either side of the
        // change: both revision strings are sixteen characters, so every payload keeps its exact
        // decoded length and differs in exactly eight bytes across exactly two regions - the definition
        // table's Blue Feint entry and the blue-feint-card instance's definition reference - which is
        // the four positions where the two strings differ, twice. No scenario here commits Blue Feint's
        // Skill; the Blue cards these scenarios play are its Defense reaction, which the conversion
        // leaves at 2. The equality groups below are identical, which is the check for that.
        //
        // Re-pinned a twelfth time at milestone 6.5a, again without a schema change and for three
        // causes this time. Two are the familiar pair, measured on the compatibility corpus: the
        // capability list gains synthetic-push-target, costing every payload exactly 26 bytes in one
        // contiguous region, and Red Needle's printed Primary Attack becomes its showcase Skill, moving
        // its definition from phase-5-1a-rev-3 to phase-6-5a-rev-1 - both sixteen characters, so that
        // cause shifts nothing. The third is this fixture's own: Red Needle used to be Red's
        // highest-Initiative Attack card at 13, so the tied-attack scenarios committed it against
        // Blue's highest; with Red's top remaining Attack now Red Lancer at 9, below Blue's own top
        // card, the fixture commits Blue's lowest instead so the Red Attack card is still the acting
        // one. That changes which Blue card these scenarios play and therefore what they hash, which
        // is a fixture change rather than an engine one - and the equality groups below are still
        // identical, which is the check that nothing about behaviour moved with it.
        //
        // Re-pinned a thirteenth time at milestone 6.5aa for the deliberate schema v10 -> v11
        // change and Red Needle's content revision. The schema widens a pending Minion Battle
        // identity from minion-only to a hero/minion union; none of these scenarios is pending at
        // that stage, so the wire change here is the version field. Red Needle advances to
        // phase-6-5aa-rev-1 because its target node now names Object Push as the exact consuming
        // activity; none of these scenarios plays that Skill. The equality groups remain identical,
        // demonstrating that rejected/stale, restore/replay, Undo, and Defense-discard behaviour did
        // not move. The diagnostic hashes move because that envelope contains the authoritative
        // payload; their grouping likewise remains unchanged.
        //
        // Re-pinned a fourteenth time at milestone 6.5b for one cause and one only: Red Bombard's
        // printed Primary Attack becomes its showcase Skill, moving its definition from
        // phase-5-1a-rev-3 to phase-6-5b-rev-1. No capability was registered and no schema changed, so
        // every payload keeps its exact length and differs in exactly eight bytes, measured on the
        // compatibility corpus against the pre-change revision. Unlike 6.5a's re-pin this one needed
        // no fixture change at all: Red Bombard is Red's lowest-Initiative card at 5 and prints a
        // Ranged Attack, so no scenario here ever selected it - these commit Red's highest card that
        // prints the required action against Blue's lowest. The equality groups below are identical,
        // which is the check that no behaviour moved with the content.
        //
        // Re-pinned a fifteenth time at milestone 6.6 for one cause: the registered capability set
        // gains p.19's Swap, Replace, and Remove, and every payload records that set as one list. The
        // card content this fixture plays is untouched - the three branches exposing them are appended
        // to the synthetic shove card, which no scenario here commits - and no schema field was added,
        // because the three operations pause through the existing effect frame and store nothing of
        // their own. Measured on the compatibility corpus in a throwaway worktree at the pre-change
        // revision: every payload grows by exactly 124 bytes in exactly one contiguous region. The
        // equality groups below are identical, which is the check that no behaviour moved with the
        // registration.
        //
        // Re-pinned a sixteenth time at milestone 6.8l, again without a schema change, for a
        // registration cause that costs nothing and a content cause that costs almost nothing. The
        // selection capability's declared schema gains the node it selects for, so its revisions 1 and
        // 2 are replaced by 3 and 4 - a revision is a fixed-width int beside an unchanged id, so the
        // registration list differs without moving a single byte of length, which is precisely enough
        // to make a payload naming the old contracts reject. Red Needle, Red Bombard, and the two
        // shove cards advance to phase-6-8l-rev-1. Measured on the compatibility corpus in a throwaway
        // worktree at pre-change revision f8ad140: the five ordinary fixtures lose exactly 2 bytes and
        // the three Skill-fixture ones gain exactly 2, entirely explained by revision strings one
        // character shorter or longer appearing twice each. No scenario here commits a converted card
        // - Red Needle and Red Bombard reach these scenarios only through the ordinary matrix, which
        // this fixture already stopped selecting Red Needle for at 6.5a - and the equality groups
        // below are identical, which is the check that no behaviour moved with any of it.
        //
        // Re-pinned a seventeenth time at milestone 6.7, for a schema change and nothing else: the
        // version field moves 11 -> 12 because a paused after-action correction now records whether the
        // action it holds open asked to repeat. None of these scenarios pauses a correction, so not one
        // of them writes that field; the version int is the entire delta they see. No capability is
        // registered or revised, and the cards this fixture commits are untouched - milestone 6.7's own
        // Repeat card is dealt only by its own configuration, exactly as every earlier fixture Skill is.
        // Measured against pre-change revision 59766f2 in a throwaway worktree, on these scenarios
        // themselves rather than by inference from the corpus: all nine authoritative payloads keep
        // their exact decoded length - 2920, 2757 and 2673 bytes - and each differs in exactly one
        // byte, 0b -> 0c at offset 30, with a 30-byte common prefix and a byte-identical suffix of
        // 2889, 2726 and 2642 bytes. The diagnostic digests move with them because the diagnostic
        // snapshot wraps the authoritative payload. The equality groups below are identical, which is
        // the check that no behaviour moved with the schema.
        //
        // Re-pinned an eighteenth time at milestone 6.7a, for one content cause: Blue Longbow's printed
        // Primary Attack became its showcase Skill, so that card definition advanced from
        // phase-5-1a-rev-3 to phase-6-7a-rev-1. Nothing else moved - schema stays v12, no capability is
        // registered or revised, and none of these scenarios plays Blue Longbow, which sits at
        // Initiative 4 below every card they commit. Measured against pre-change revision 5c360a8 in a
        // throwaway worktree, on these scenarios themselves rather than by inference from the corpus:
        // all nine authoritative payloads keep their exact decoded length - 2920, 2757 and 2673 bytes -
        // and each differs in exactly six bytes across exactly two regions, the definition table's Blue
        // Longbow entry and the blue-longbow-card instance's definition reference. Both are the same
        // sixteen-character revision string, whose three moving characters sit at offsets 6, 8 and 15,
        // so nothing shifted and every other section is byte-identical. The replaced Ranged Attack
        // never appears at all, because the payload names definitions and the catalog supplies what
        // they contain. The equality groups below are identical, which is the check that no behaviour
        // moved with the conversion.
        //
        // Re-pinned a nineteenth time at milestone 6.4c, for two causes - one registration and one
        // content. Both Push declarations gain a declared eligible piece kind and move to revision 2,
        // and four card revisions move: Red Needle and Blue Longbow now say `unit`, and the two fixture
        // Skill cards restate p.18's own unit-or-token breadth. Schema stays v12. Measured against
        // pre-change revision 0116c5c in a throwaway worktree, on these scenarios themselves: all nine
        // authoritative payloads keep their exact decoded length - 2920, 2757 and 2673 bytes - and each
        // differs in exactly ten bytes across six runs. Two of those bytes are the revision ints inside
        // the synthetic-object-push and synthetic-push-target registration entries, which are
        // fixed-width, and the other eight are the two moved characters of each of the two card
        // revisions these scenarios name, at its definition-table and card-instance sites. The declared
        // parameter itself never reaches the wire: a payload records the contract set and the card
        // definitions, and the catalog supplies what those contain. The equality groups below are
        // identical, which is the check that no behaviour moved with the contract set.
        //
        // Re-pinned a twentieth time at milestone 6.9, for one persistence cause: the schema moves from
        // v12 to v13 because rulebook p.8's mid-turn Lane Push can now be requested, and a turn holding
        // a request is stopped at that checkpoint until milestones 6.10 and 6.11 resolve it. No
        // capability is registered or revised and no card revision moves. Measured against pre-change
        // revision 8ccacca in a throwaway worktree, on these scenarios themselves: every payload differs
        // at byte 31, the fixed-width schema version int. The five accepted-group payloads grow 2920 to
        // 2921 and the three defense-discarded ones 2673 to 2674, gaining exactly the trailing false
        // that says the completed action owes no push. undo-truncated is the one that does not move,
        // 2757 both sides, because its retained prefix ends before any card action exists and a match
        // with no card-action authority writes no request field at all - which is the same thing the
        // corpus's setup fixture shows, and the clearest evidence that the field belongs to the action
        // boundary rather than to the match. None of these nine scenarios requests a push, so that
        // single byte is the whole of the cost. The equality groups below are identical, which is the
        // check that no behaviour moved with the schema.
        //
        // Re-pinned a twenty-first time at milestone 6.10, for one persistence cause: the schema moves
        // from v13 to v14 because rulebook p.8's Battle Zone is now authoritative state the Lane-Push
        // Zone stage advances, so it travels beside the map reference rather than being derived from the
        // map's starting-Battle-Zone flag. No capability is registered or revised and no card revision
        // moves. Measured against pre-change revision 7d73cb7 in a throwaway worktree, on the version-
        // controlled corpus these scenarios share a serializer with: every payload gains exactly 21
        // bytes - the 0x14 length prefix plus the twenty characters of starting-battle-zone - in one
        // contiguous insertion, and differs otherwise only at the fixed-width schema int. Unlike 6.9's
        // move, undo-truncated is not the exception here: the Zone belongs to the board rather than to a
        // card action, so a retained prefix that ends before any card action exists still carries one.
        // None of these nine scenarios pushes the Lane, so every one of them records the zone the match
        // began in and the insertion is the whole of the cost. The equality groups below are identical,
        // which is the check that no behaviour moved with the schema.
        private static readonly ScenarioDigest[] ExpectedDigests =
        {
            new ScenarioDigest("accepted", "d15e0ff3cfead0511b296ed14a9c175bad34632b6b3ae5fc76ea761caf6795e4", "ad219943a08f63ea43f94c5deffd203f370b38f243df7fd6f943b6fd198bcd22"),
            new ScenarioDigest("rejected", "d15e0ff3cfead0511b296ed14a9c175bad34632b6b3ae5fc76ea761caf6795e4", "ad219943a08f63ea43f94c5deffd203f370b38f243df7fd6f943b6fd198bcd22"),
            new ScenarioDigest("stale", "d15e0ff3cfead0511b296ed14a9c175bad34632b6b3ae5fc76ea761caf6795e4", "ad219943a08f63ea43f94c5deffd203f370b38f243df7fd6f943b6fd198bcd22"),
            new ScenarioDigest("restored", "d15e0ff3cfead0511b296ed14a9c175bad34632b6b3ae5fc76ea761caf6795e4", "ad219943a08f63ea43f94c5deffd203f370b38f243df7fd6f943b6fd198bcd22"),
            new ScenarioDigest("replayed", "d15e0ff3cfead0511b296ed14a9c175bad34632b6b3ae5fc76ea761caf6795e4", "ad219943a08f63ea43f94c5deffd203f370b38f243df7fd6f943b6fd198bcd22"),
            new ScenarioDigest("undo-truncated", "8e6deba994c1cfff34429d7df9712e297a8538bcaa5bf44538939e68aa2140c5", "dcf0191d293cce794d46ca08ef0a26585302be619571e6c7bb48155616a4e6e6"),
            new ScenarioDigest("defense-discarded", "ee8df53931c84a63565a6aaaac08659513e00297285aa8a37e8229285aa43bdf", "a3248755319dfd0cc3d95d13237ee8361640ab4a1a6e2f4d84690117ce879724"),
            new ScenarioDigest("defense-discarded-restored", "ee8df53931c84a63565a6aaaac08659513e00297285aa8a37e8229285aa43bdf", "a3248755319dfd0cc3d95d13237ee8361640ab4a1a6e2f4d84690117ce879724"),
            new ScenarioDigest("defense-discarded-replayed", "ee8df53931c84a63565a6aaaac08659513e00297285aa8a37e8229285aa43bdf", "a3248755319dfd0cc3d95d13237ee8361640ab4a1a6e2f4d84690117ce879724")
        };

        // Three invariants are visible in the pinned values themselves, and each would break loudly:
        //   - accepted, rejected, and stale share one digest pair, because a refused command must
        //     leave authoritative state and the event log untouched.
        //   - restored and replayed match their live counterparts, because a snapshot round trip and
        //     a command replay must both converge on the state they came from.
        //   - the two defense-discard families differ from every other scenario, because a card
        //     actually changed location.

        [Test]
        public void SixScenarios_ProduceByteIdenticalSnapshotsAndOrderedEvents()
        {
            var measured = MeasureAllScenarios();

            WriteMeasuredDigests(measured);

            Assert.That(measured.Select(value => value.Name),
                Is.EqualTo(ExpectedDigests.Select(value => value.Name)),
                "The scenario set itself changed; the pinned digests no longer describe the same six cases.");

            foreach (var expected in ExpectedDigests)
            {
                var actual = measured.Single(value => value.Name == expected.Name);
                Assert.That(actual.AuthoritativeSha256, Is.EqualTo(expected.AuthoritativeSha256),
                    $"Scenario '{expected.Name}': the authoritative snapshot changed. A refactor that " +
                    "must preserve behaviour changed authoritative state.");
                Assert.That(actual.DiagnosticSha256, Is.EqualTo(expected.DiagnosticSha256),
                    $"Scenario '{expected.Name}': the ordered rule-event log changed. Events must be " +
                    "emitted in the same order, with the same content, as before the refactor.");
            }
        }

        private static IReadOnlyList<ScenarioDigest> MeasureAllScenarios()
        {
            return new[]
            {
                MeasureAccepted(),
                MeasureRejected(),
                MeasureStale(),
                MeasureRestored(),
                MeasureReplayed(),
                MeasureUndoTruncated(),
                MeasureDefenseDiscarded(),
                MeasureDefenseDiscardedRestored(),
                MeasureDefenseDiscardedReplayed()
            };
        }

        /// <summary>
        /// Drives an Attack through to a chosen Defense card, which is the ONE path that discards a
        /// card by calling the location transition directly instead of through
        /// <see cref="CardLifecycleAuthority"/>. 6.1f reroutes exactly this call, so a harness that
        /// never reaches it would not be measuring the thing being changed.
        /// </summary>
        private static ScenarioDigest MeasureDefenseDiscarded()
        {
            var session = CreateAdjacentSession();
            DiscardOneDefenseCard(session);
            return Digest("defense-discarded", session.Projection);
        }

        private static ScenarioDigest MeasureDefenseDiscardedRestored()
        {
            var source = CreateAdjacentSession();
            DiscardOneDefenseCard(source);
            var payload = source.CaptureSnapshot();

            var restored = CreateAdjacentSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True,
                "A discarded-Defense snapshot must restore.");

            return Digest("defense-discarded-restored", restored.Projection);
        }

        private static ScenarioDigest MeasureDefenseDiscardedReplayed()
        {
            var source = CreateAdjacentSession();
            DiscardOneDefenseCard(source);
            var commands = source.Projection.AcceptedCommands.ToArray();

            var replayed = CreateAdjacentSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay, including the Defense discard.");
            }

            return Digest("defense-discarded-replayed", replayed.Projection);
        }

        private static void DiscardOneDefenseCard(MatchSession session)
        {
            StartTiedAttackTurn(session);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);

            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single())).Accepted, Is.True);

            var defenseChoice = session.Projection.PendingDefenseChoice;
            Assert.That(defenseChoice, Is.Not.Null, "The scenario must actually reach a Defense choice.");
            var defenseCardId = defenseChoice.LegalDefenseOffers
                .Select(offer => offer.SourceCardInstanceId)
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .First();

            Assert.That(session.Execute(DirectorCommand.ChooseDefenseCard(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId,
                defenseCardId)).Accepted, Is.True);

            var discarded = session.Projection.Cards.Single(card => card.Id.Equals(defenseCardId));
            Assert.That(discarded.State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "The Defense card must actually have moved to the discard pile; otherwise this " +
                "scenario is not exercising the transition it exists to pin.");
        }

        private static void StartTiedAttackTurn(MatchSession session)
        {
            var red = HandCards(session, TeamSide.Red)
                .Where(card => DefinitionOf(session, card).ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack))
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
                .First();

            // Blue's lowest Initiative, so the Red Attack card is the acting one whatever the showcase
            // conversions have done to the top of either band. Red's highest remaining Attack was
            // Red Needle at 13 until milestone 6.5a converted it and Red Lancer at 9 took over, which
            // is below Blue's own top card - taking Blue's lowest keeps the fixture's actor fixed
            // without steering Initiative.
            var blue = HandCards(session, TeamSide.Blue)
                .OrderBy(card => DefinitionOf(session, card).Initiative.Value)
                .First();

            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);

            var choice = session.Projection.PendingInitiativeChoice;
            if (choice != null)
            {
                Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(choice.OwningSide, red.Id)).Accepted, Is.True);
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card)
        {
            return session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference));
        }

        private static ActionKind ActionKindOf(MatchSessionProjection projection, ActionOffer offer)
        {
            return projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        /// <summary>
        /// Two heroes adjacent on a two-space synthetic board, so an Attack has a legal target and a
        /// Defense response is reachable. Mirrors the adjacent fixture in MatchFeaturePresenterTests;
        /// the project keeps session builders fixture-local, and consolidating them is 6.1k's subject.
        /// </summary>
        private static MatchSession CreateAdjacentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("aggregate-invariance-adjacent"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-aggregate"),
                        "Red Aggregate",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-aggregate"),
                        "Blue Aggregate",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                        blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>Both sides commit, and any tie is resolved. The ordinary accepted path.</summary>
        private static ScenarioDigest MeasureAccepted()
        {
            var session = CreateSession();
            CommitBothSides(session);
            return Digest("accepted", session.Projection);
        }

        /// <summary>
        /// A rejected command must leave authoritative state and the event log untouched, so this
        /// scenario's digests must equal the accepted scenario's.
        /// </summary>
        private static ScenarioDigest MeasureRejected()
        {
            var session = CreateSession();
            CommitBothSides(session);

            var unknownHero = new HeroId("no-such-hero");
            var anyCard = session.Projection.Cards[0];
            var result = session.Execute(DirectorCommand.CommitCard(unknownHero, anyCard.Id));
            Assert.That(result.Accepted, Is.False, "The rejected scenario must actually reject.");

            return Digest("rejected", session.Projection);
        }

        /// <summary>Re-committing an already committed card is the stale-choice path.</summary>
        private static ScenarioDigest MeasureStale()
        {
            var session = CreateSession();
            var red = HandCards(session, TeamSide.Red)[0];
            var blue = HandCards(session, TeamSide.Blue)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);

            var stale = session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Assert.That(stale.Accepted, Is.False, "The stale scenario must actually reject.");

            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            ResolveInitiativeTieIfPending(session);

            return Digest("stale", session.Projection);
        }

        /// <summary>Restoring a captured snapshot into a fresh session must reproduce it exactly.</summary>
        private static ScenarioDigest MeasureRestored()
        {
            var source = CreateSession();
            CommitBothSides(source);
            var payload = source.CaptureSnapshot();

            var restored = CreateSession();
            var result = restored.RestoreSnapshot(payload);
            Assert.That(result.Accepted, Is.True, "The restored scenario must actually restore.");

            return Digest("restored", restored.Projection);
        }

        /// <summary>Replaying the accepted command prefix into a fresh session must converge.</summary>
        private static ScenarioDigest MeasureReplayed()
        {
            var source = CreateSession();
            CommitBothSides(source);
            var commands = source.Projection.AcceptedCommands.ToArray();

            var replayed = CreateSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay.");
            }

            return Digest("replayed", replayed.Projection);
        }

        /// <summary>Undo must truncate to exactly the state the prefix produced.</summary>
        private static ScenarioDigest MeasureUndoTruncated()
        {
            var session = CreateSession();
            var red = HandCards(session, TeamSide.Red)[0];
            var blue = HandCards(session, TeamSide.Blue)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            ResolveInitiativeTieIfPending(session);

            var undo = session.Undo();
            Assert.That(undo.Accepted, Is.True, "The undo-truncated scenario must actually undo.");

            return Digest("undo-truncated", session.Projection);
        }

        private static void CommitBothSides(MatchSession session)
        {
            var red = HandCards(session, TeamSide.Red)[0];
            var blue = HandCards(session, TeamSide.Blue)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            ResolveInitiativeTieIfPending(session);
        }

        private static void ResolveInitiativeTieIfPending(MatchSession session)
        {
            var choice = session.Projection.PendingInitiativeChoice;
            if (choice == null)
            {
                return;
            }

            Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(
                choice.OwningSide, choice.CandidateCardInstanceIds[0])).Accepted, Is.True);
        }

        private static IReadOnlyList<CardInstance> HandCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static ScenarioDigest Digest(string name, MatchSessionProjection projection)
        {
            return new ScenarioDigest(
                name,
                Sha256(projection.AuthoritativeSnapshot),
                Sha256(projection.DiagnosticSnapshot));
        }

        private static void WriteMeasuredDigests(IReadOnlyList<ScenarioDigest> measured)
        {
            var builder = new StringBuilder();
            foreach (var digest in measured)
            {
                builder.AppendLine(
                    $"new ScenarioDigest(\"{digest.Name}\", \"{digest.AuthoritativeSha256}\", \"{digest.DiagnosticSha256}\"),");
            }

            var directory = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName, "tmp");
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "milestone-6-1f-measured-digests.txt"),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static string Sha256(string payload)
        {
            Assert.That(payload, Is.Not.Null.And.Not.Empty,
                "A scenario produced no snapshot, so its digest would be meaningless.");
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(payload))
                    .Select(value => value.ToString("x2")));
            }
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private sealed class ScenarioDigest
        {
            public ScenarioDigest(string name, string authoritativeSha256, string diagnosticSha256)
            {
                Name = name;
                AuthoritativeSha256 = authoritativeSha256;
                DiagnosticSha256 = diagnosticSha256;
            }

            public string Name { get; }
            public string AuthoritativeSha256 { get; }
            public string DiagnosticSha256 { get; }
        }
    }
}
