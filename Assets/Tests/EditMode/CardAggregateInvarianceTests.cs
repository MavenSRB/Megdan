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
        //
        // Re-pinned a twenty-second time at milestone 6.10a, for one persistence cause: the schema moves
        // from v14 to v15 because a multi-step system transition can now stop on an authority-owned
        // intra-stage continuation, which the round section persists. No capability is registered or
        // revised and no card revision moves. Measured against the v14 payloads captured earlier in the
        // same session, on the version-controlled corpus these scenarios share a serializer with: every
        // payload grows by exactly one byte, the trailing 00 saying no transition is paused, inserted at
        // the round section's marker boundary with every byte before and after identical. None of these
        // nine scenarios begins a transition, so that byte is the whole of the cost, and unlike 6.9 and
        // 6.10 no scenario is exempt - a transition belongs to the match rather than to a board or an
        // action. The equality groups below are identical, which is the check that no behaviour moved
        // with the schema.
        //
        // Re-pinned a twenty-third time at milestone 6.12, and for the first time in a while for a
        // content cause rather than a schema one: the schema stays at v16 and the synthetic Hazard token
        // moves from revision 3 to revision 4, because its supply count fell from two to one so that
        // p.18's short-supply reclamation stays reachable once p.8's end-of-round cleanup returns every
        // placed token. The revision is a fixed-width int in the component definition table every
        // scenario's payload carries, so no payload changes length and each differs in exactly the one
        // byte that names it - the same delta the compatibility corpus measured at pre-change revision
        // 1345152. None of these nine scenarios reaches an end of round, so the cleanup itself
        // contributes nothing here, which is why the cause is the revision alone. The equality groups
        // below are identical, which is the check that no behaviour moved with the content.
        //
        // Re-pinned a twenty-fourth time for the five-card scene, again for content and again with the
        // schema at v16. The configured scene stopped being dealt milestone 6.2's Cache card as a sixth
        // card per hero, and Blue Feint took all three of its modes - the placement behind p.18's `if
        // able` reclamation, and the give/return marker pair - at phase-6-8b-rev-2. That one definition
        // is the whole cause: Red's copy was dropped rather than rehoused, and Blue Sentinel stayed at
        // phase-6-6a-rev-1 because p.8 clears the board of tokens each round, so the card whose Replace
        // and Remove take a Hazard cannot also be the card that supplies it. These nine scenarios deal
        // the ordinary five and none of them plays Blue Feint, so what reaches their payloads is that
        // one revision string - sixteen characters before and after, appearing twice, in the definition
        // table and where its instance names its reference, which is the same delta the compatibility
        // corpus measured at pre-change revision 8a84b68. The equality groups below are identical,
        // which is the check that no behaviour moved with the content.
        //
        // Re-pinned a twenty-fifth time at milestone 7.1, and this one is a schema move as well as a
        // content one, so every digest was always going to shift. Three causes, all measured on the
        // compatibility corpus in a throwaway worktree at pre-change revision a9fda8b rather than
        // assumed: the version field moves 16 -> 17; every one of the ten configured card definitions
        // advances to phase-7-1-rev-1 because it gains a colour, a classification and either a Tier or
        // p.15's explicit absence of one; and the definition table gains the twenty-four same-colour
        // upgrade targets the six non-Basic Tier I cards now name. The round section then gains one
        // hero-Level record per hero. The corpus arithmetic closes exactly at +1082 bytes per payload,
        // and the reasoning carries here unchanged because these scenarios deal the same ordinary five
        // cards. What did not change is the part that makes the re-pin safe: all three equality groups
        // below hold exactly as before - accepted, rejected and stale still share one pair, restored
        // and replayed still match their live counterparts, and the defense-discard family still
        // differs from every other scenario - so the schema and the content moved and no behaviour did.
        //
        // Re-pinned a twenty-sixth time at milestone 7.1a, and this one is neither a schema nor a
        // content move: the version field stays 17 and not one card definition or revision changes.
        // The single cause is that the build registers one more capability - the chooser-owned in-hand
        // discard - and every payload records the capability set it was written against. Measured on
        // the compatibility corpus in a throwaway worktree at pre-change revision d3a3aa4 rather than
        // assumed: every payload grows by exactly 34 bytes, being a one-byte length prefix, the
        // twenty-nine-character capability id, and the int32 revision. The reasoning carries here
        // unchanged because these scenarios deal the same ordinary five cards and none of them binds
        // the new capability; the fixture card that does is dealt only by its own configuration.
        // All three equality groups again hold exactly as before, which is what makes the re-pin safe:
        // a registration moved and no behaviour did.
        //
        // Re-pinned a twenty-seventh time at milestone 7.2, which is a schema move and a content one,
        // and both are smaller than 7.1's. Measured on the compatibility corpus in a throwaway worktree
        // at pre-change revision a4b7613 rather than assumed: the version field moves 17 -> 18; the
        // round section gains an item count of zero, four bytes, because nothing in this build gains an
        // item; and the twenty-four generated upgrade targets advance to phase-7-2-rev-1 because they
        // gain p.16's Item icon. Outside the four-byte insertion each payload differs in exactly
        // twenty-five bytes - the version field and one character of each of those twenty-four revision
        // strings. The ten configured cards keep phase-7-1-rev-1, so the five cards these scenarios deal
        // are byte-for-byte what they were, and the reasoning carries here unchanged. All three equality
        // groups hold exactly as before, which is again what makes the re-pin safe: a schema field and a
        // content revision moved, and no behaviour did.
        //
        // Re-pinned a twenty-eighth time at milestone 7.2a, which is a schema move and nothing else -
        // no card definition, capability or component registration changed, so for once no content
        // revision moved at all. Measured on the compatibility corpus in a throwaway worktree at
        // pre-change revision 6a83b15 rather than assumed: the version field moves 18 -> 19, and the
        // setup section gains each hero's four rulebook p.16 item slots, sixty bytes per hero written
        // where that hero's id and player id already are. Every payload therefore grows by exactly 120
        // bytes in two insertions rather than one, and outside them exactly one byte differs - the
        // version field. No hero in these scenarios owns an item, so the item records that gained a
        // slot position and an icon contribute nothing here. All three equality groups hold exactly as
        // before: a schema field moved and no behaviour did.
        //
        // Re-pinned a twenty-ninth time at milestone 7.2b, and this one is a registration cause alone -
        // no schema move, no content revision, no component. The build registers
        // synthetic-radius-target, the target selection bounded by rulebook p.9's printed Radius icon,
        // and every payload records the capability contract set it was written against. Measured on the
        // compatibility corpus in a throwaway worktree at pre-change revision 889007c rather than
        // assumed: each payload grows by exactly 28 bytes - a one-byte length prefix, the
        // twenty-three-character id, and the int32 revision - inside one changed region that starts at
        // the list's own count, which moves 29 to 30. The two cards that print a Radius are dealt by
        // their own configuration alone, so no scenario here holds one and the reasoning carries
        // unchanged. All three equality groups hold exactly as before.
        //
        // Re-pinned a thirtieth time at milestone 7.2c, for one registration cause and one content
        // cause, with no schema move. The build registers synthetic-card-radius, which publishes a
        // card's composed Radius Total as a fact, and Red Needle advances from phase-7-1-rev-1 to
        // phase-7-2c-rev-1 because it now prints rulebook p.9's Radius icon at the value 4 its own text
        // has named since 6.5a. The other nine configured cards keep their shared revision, which is
        // what makes this a one-card content change rather than a ten-card one. Measured on the
        // compatibility corpus in a throwaway worktree at pre-change revision 570ba0e rather than
        // assumed: 28 bytes per payload, of which 26 are the registration entry and 2 are the one extra
        // character of the new revision string in each of the two places it appears - the definition
        // table and that card's own instance reference, counted rather than reasoned. These scenarios
        // commit Red Needle in the accepted family, so the card is one they actually deal; what moves
        // is the revision they name it at, and all three equality groups hold exactly as before.
        //
        // Re-pinned a thirty-first time at milestone 7.3, for one content cause and nothing else: each
        // hand supplies rulebook p.6's eighteenth card, its Ultimate, at the new revision
        // phase-7-3-rev-1. No schema moves, because p.16 records the unlock by moving the card and the
        // card's location has always been on the wire; no capability is registered; no existing card's
        // revision changes, because these two definitions are new content. Measured on the
        // compatibility corpus in a throwaway worktree at pre-change revision 0957e6f rather than
        // assumed: 83 bytes per payload in two definition-table insertions, 42 and 41, plus the table's
        // own count moving from 34 to 36. No scenario here unlocks an Ultimate - nothing in this build
        // does until milestone 7.5's level purchase - so what moves is the content set they are written
        // against, and all three equality groups hold exactly as before.
        //
        // Re-pinned a thirty-second time at milestone 7.4, for one schema cause: every team's remaining
        // rulebook p.13 Life counters join the round section, and the version moves from 19 to 20. It is
        // the milestone in which a counter can first change, so the field is authoritative state rather
        // than a constant the HUD printed. Measured on the compatibility corpus in a throwaway worktree
        // at pre-change revision f7552e0 rather than assumed: 31 bytes per payload in one contiguous
        // insertion - a two-count, then blue-team and red-team each with a length prefix and a
        // four-byte total - with exactly one byte differing outside it, the version at offset 30.
        // The `defense-discarded` family is the one that moves for a second reason as well: its Attack
        // now defeats the target, so p.13's removal, unresolved-card resolution, reward and Life spend
        // are inside the state it captures. The three equality groups hold exactly as before: the five
        // accepted-family scenarios agree, `undo-truncated` differs from them, and the three
        // defense-discarded scenarios agree with each other and differ from both.
        //
        // Re-pinned a thirty-third time at milestone 7.5, for two content causes and no schema move: the
        // version at offset 30 stays 20. The first is rulebook p.6's own setup, dealt at last - the
        // thirteen Tier II, Tier III and Tier IV cards each hero returns to their deck existed as
        // definitions with no instance until p.15's Upgrade a Card step had a reason to look for them
        // there, so every configured match now holds thirty-six cards where it held ten. The second is
        // the coin ledger, which the round authority now asks the dispatcher for when it composes
        // itself, because p.15's level purchase spends from the balances p.13's Defeat fills and two
        // ledgers would be two answers to what a hero can afford. Measured on the compatibility corpus
        // in a throwaway worktree at pre-change revision a4f76f2 rather than assumed: one replaced byte
        // where the card count goes 10 -> 36, then 1203 bytes for the Red hero's thirteen cards and 1213
        // for the Blue hero's, then 46 for the ledger - 2462 bytes per payload, and 2508 for the seven
        // fixtures whose paused card-action section persists a second copy of the ledger. No scenario
        // here buys a level, because none of them reaches an end of round; what moves is the setup they
        // are written against, and all three equality groups hold exactly as before.
        //
        // Re-pinned a thirty-fourth time at milestone 7.6, for one schema cause: the record of which
        // heroes bought a level in the current round joins the round section, and the version moves from
        // 20 to 21. It is the milestone in which that question can first be asked, because rulebook p.8's
        // Collect a Pity Coin reads the negation of it a stage after the Level Up that answers it.
        // Measured on the compatibility corpus in a throwaway worktree at pre-change revision 3b8e55d
        // rather than assumed, and it is the smallest delta this fixture has recorded: 4 bytes per
        // payload in one contiguous insertion - the int32 count of an empty list - with exactly one byte
        // differing outside it, the version at offset 30. Every scenario here records nobody, because
        // none of them reaches an end of round at all, so what the four bytes say is exactly that. All
        // three equality groups hold exactly as before.
        //
        // Re-pinned a thirty-fifth time at milestone 8.6, for one schema cause and one content cause,
        // and the interesting part is that neither of them is the schema move itself. The version moves
        // from 21 to 22 because a repeat continuation joins the pending Skill frame and the owed
        // repeated action - Center of Mass's "may repeat up to two times" is the first verified count
        // above one - but no scenario here holds either, so not one of these payloads has anywhere to
        // put one. What actually moves the bytes is the registration list the snapshot records to say
        // which capability contract set it was written against: the printed-Range selection is new, and
        // direct Move and Place each advanced a revision. Measured on the compatibility corpus in a
        // throwaway worktree at pre-change revision 3578dda rather than assumed: 27 bytes per payload in
        // one contiguous insertion - a length prefix, the twenty-two characters of
        // `synthetic-range-target`, and a four-byte revision - with exactly four bytes differing outside
        // it: the version at offset 30, the capability count 31 -> 32, direct Move 2 -> 3, and Place
        // 1 -> 2. All three equality groups hold exactly as before.
        //
        // Re-pinned a thirty-sixth time at milestone 8.8, for one content cause and no schema move at
        // all. Both Push declarations advance to revision 3 to carry what Wasp's kinetic Skills print -
        // the least distance a card names, and for the batch the count bound and the side of the board -
        // and the registration list every payload records is what moves. These scenarios play showcase
        // content, so Wasp's own revision advance is invisible here; it is visible on the compatibility
        // corpus, which carries a verified-hand fixture for exactly that reason. Measured on that corpus
        // in a throwaway worktree at pre-change revision 43cab2e rather than assumed: no payload changes
        // length at all, and each differs in exactly two bytes, both 0x02 -> 0x03. All three equality
        // groups hold exactly as before.
        //
        // Re-pinned a thirty-seventh time at milestone 8.9, again for a registration cause and again
        // with no schema move. Wasp's two basic cards create Active effects, and each is a registered
        // capability - `wasp-pin-enemy-units-in-radius` and `wasp-close-barrier-in-radius` - so the
        // registration list every payload records grows by two. These scenarios play showcase content,
        // so neither capability can run in them and Wasp's own revision advance is invisible here; both
        // are visible on the compatibility corpus, which carries a verified-hand fixture for exactly
        // that reason. Measured on that corpus in a throwaway worktree at pre-change revision e4b121d
        // rather than assumed: every payload grows by exactly 68 bytes - the two registration entries -
        // and changes one byte in place, the capability count 0x20 -> 0x22. The event-log digests move
        // for a second reason of this milestone's own: p.9's supertype is now recorded at each action's
        // completion checkpoint, so a scenario that Holds with a basic card gains a
        // Turn.BasicActionPerformed entry. All three equality groups hold exactly as before, which is
        // what says the new entry is produced by the command rather than remembered anywhere.
        //
        // Re-pinned a thirty-eighth time at milestone 8.10, for one schema cause and one registration
        // cause. This paragraph is written retroactively at 8.11: 8.10 moved all nine pairs and did not
        // record why, which is the one thing the note at the top of this block asks of a re-pinning
        // commit, so the delta was measured after the fact rather than reconstructed from memory. The
        // schema moves 22 -> 23 to carry the Block result an in-flight Attack now remembers, and the
        // registration list gains four entries - `synthetic-attack-took-a-hero`, `synthetic-defeat-hero`,
        // `synthetic-discard-chosen-card` and `synthetic-radius-hero-target`. Measured on the
        // compatibility corpus in a throwaway worktree at pre-change revision 19a6def8: every carried
        // payload grows by exactly 126 bytes, which is those four entries at a length prefix plus the id
        // plus a four-byte revision each (33 + 26 + 34 + 33), and changes one byte in place, the
        // capability count 0x22 -> 0x26. The verified-hand payload grows by 180 instead, the extra 54
        // being Wasp's content revision `wasp-8-9-rev-1` -> `wasp-8-10-rev-1` at one byte longer in each
        // of its 54 occurrences. All three equality groups held.
        //
        // Re-pinned a thirty-ninth time at milestone 8.11, for one schema cause and one registration
        // cause again. The schema moves 23 -> 24 to carry the Defense-Block condition's verdict, and the
        // registration list gains two entries - `synthetic-attack-is-ranged`, the predicate that asks
        // whether the Attack being answered came from range, and `synthetic-range-hero-target`, the
        // channel a Block rider targets through. Measured on the compatibility corpus in a throwaway
        // worktree at pre-change revision 6ed3144f: every carried payload grows by exactly 63 bytes,
        // which is those two entries at a length prefix plus the id plus a four-byte revision each
        // (31 + 32), and changes one byte in place, the capability count 0x26 -> 0x28. One payload,
        // `paused-card-text`, grows by 67 rather than 63, and the extra four bytes are this milestone's
        // schema field itself: the Block verdict is written once per in-flight Attack resolution state,
        // and that is the only carried fixture holding one. Wasp's own revision advance
        // `wasp-8-10-rev-1` -> `wasp-8-11-rev-1` is the same width and moves nothing. These scenarios
        // play showcase content, so neither new capability can run in them; both are exercised on the
        // corpus, which carries a paused-Block fixture for exactly that reason. All three equality
        // groups hold exactly as before.
        // Milestone 8.13 deliberately re-baselines every authoritative digest because snapshots pin
        // the registered capability contract set, which now includes High Voltage's three general
        // declarations and its triggered-effect catalog entry. Schema v24 and scenario semantics are
        // unchanged; the equality groups below are the migrate-in-place decision and remain intact.
        //
        // Milestone 8.19a re-baselines both halves, and the event half is the interesting one. The
        // authoritative digests move for schema v25 and its two new registered capabilities, which is
        // the ordinary reason. The ordered event digests move because one payload genuinely changed:
        // rulebook p.14 announces the Attack Total at step 2, and step 1's card text now stands
        // between the target's validation and that announcement, so `AttackTargetValidated` reports no
        // total where it used to report one the page had not announced yet. No event moved position
        // and none was added or dropped; the three equality groups below hold exactly as before.
        //
        // Milestone 8.19a1 re-baselines both encoded halves for schema v26. Each scenario has an empty
        // current-round hero-Defeat history, so its authoritative payload changes the schema byte in
        // place and grows by exactly four bytes for the zero entry count. The diagnostic envelope moves
        // because it embeds that authoritative payload; this milestone emits no new diagnostic event.
        // The same three equality groups remain exact.
        //
        // Milestone 8.19c re-baselines both encoded halves without a schema change at all. A snapshot
        // pins the registered capability contract set, and this milestone registers five more - the
        // persistent source shapes - so every authoritative payload grows by exactly 187 bytes: 162
        // UTF-8 bytes of capability id, five one-byte length prefixes, and five four-byte versions. The
        // schema stays v26 and no scenario semantics move; the diagnostic envelope moves only because
        // it embeds the authoritative payload, and this milestone emits no new diagnostic event. These
        // scenarios play showcase content, so none of the five can run in them - what changed is the
        // contract set the payload says it was written against, which is exactly what this pin is for.
        // The three equality groups below hold as before.
        //
        // Milestone 8.19c1 re-baselines both encoded halves for schema v27. Each scenario grows by 86
        // bytes and no scenario semantics move: 83 for the two new capability registrations - 73 UTF-8
        // bytes of capability id, two one-byte length prefixes and two four-byte versions - plus one
        // byte in the round block for p.8's pre-battle visit and two in the card-action block for the
        // two new optional pauses, each written as an absent marker. The schema int changes in place.
        // The diagnostic envelope moves only because it embeds the authoritative payload; this
        // milestone emits no new diagnostic event. The same three equality groups remain exact.
        //
        // Milestone 8.21 re-baselines both encoded halves for schema v29, and again nothing about
        // these scenarios moved. Each authoritative payload grows by exactly 329 bytes - twelve
        // capability registrations at 269 UTF-8 bytes of id, twelve one-byte length prefixes and
        // twelve four-byte versions - and the schema int changes 28 -> 29 in place at no cost. No
        // field is added at all. These scenarios play showcase content, and the twelve contracts
        // belong to Brogan's printed faces, so none of them can run here; what changed is the contract
        // set the payload says it was written against, which is exactly what this pin is for. The
        // diagnostic envelope moves only because it embeds the authoritative payload. The same three
        // equality groups remain exact.
        //
        // Milestone 8.22 re-baselines both encoded halves for schema v30, for the same cause and with
        // nothing about these scenarios moved. Each authoritative payload grows by exactly 117 bytes -
        // five capability registrations at 92 UTF-8 bytes of id, five one-byte length prefixes and five
        // four-byte versions - and the schema int changes 29 -> 30 in place at no cost. No field is
        // added. These scenarios play showcase content and the five contracts belong to Sabina's
        // printed faces, so none of them can run here; what changed is the contract set the payload
        // says it was written against, which is exactly what this pin is for. The diagnostic envelope
        // moves only because it embeds the authoritative payload. The same three equality groups
        // remain exact.
        //
        // Milestone 8.22 re-baselines them a second time within one milestone, for schema v31, and
        // this move is a field rather than the list. Finding `F-ATK-02` lets a Push inside rulebook
        // p.14's card text set off an after-push trigger, which stops the page part-way through; the
        // remainder owed while that trigger stands is written outside the continuation discriminator.
        // Each of these payloads grows by exactly one byte - the absent marker that says no remainder
        // is held - and the schema int changes 30 -> 31 in place at no cost. The marker is written by
        // every payload that carries a card-action authority and by no payload that carries none,
        // which is why the corpus beside this one moves by one byte on fourteen fixtures and by none
        // on `verified-hand-setup`. Nothing about these scenarios moved, no trigger runs in any of
        // them, and the same three equality groups remain exact.
        //
        // Milestone 8.35u re-baselines them for schema v36, and this move costs these nine scenarios
        // NOTHING but the version int: rulebook p.15's simultaneous levelling writes one item,
        // chooser and option list per hero still choosing, and not one of these payloads carries a
        // paused transition at all. So every digest here moves while every byte count stays put -
        // measured in a worktree at the pre-change revision, where the only fixture in the whole
        // corpus beside this one that grew was `paused-level-up`, by exactly the 4-byte count. The
        // same three equality groups remain exact, which is the statement that matters: a version
        // bump that reordered or dropped anything would have split them.
        //
        // Milestone 8.35z21 re-baselines them for schema v37, and this move costs each of these nine
        // scenarios exactly TEN bytes: the four fields the card-action block now writes for the two
        // repeat instances that have been decided and not yet started - two absent markers and the two
        // `none` target kinds beside them - written by every payload whose turn carries a card-action
        // authority and by no payload that carries none. Measured in this worktree at the pre-change
        // revision: `coin-earning-round` 9,035 -> 9,045, and three fixtures whose turn carries no
        // card-action authority (`attack-text`, `orbit`, `engaged-verified-mirror`) unmoved at 8,865,
        // 8,713 and 7,571. The one fixture in the corpus that moved by more is the paused mid-turn
        // push, 9,593 -> 9,608, and the extra five bytes are this milestone's own behaviour rather
        // than its format: a push now stops on p.8's sidebar Place with the card still unresolved and
        // its actor still selected, where a v36 capture at the same point held a resolved card
        // awaiting its release. No repeat is owed in any scenario here, so every one of these ten
        // bytes is an absent marker. The diagnostic envelope moves only because it embeds the
        // authoritative payload; this milestone emits no new diagnostic event in these scenarios. The
        // same three equality groups remain exact.
        // Milestone 8.35z27 re-baselines every authoritative digest, for the two ordinary
        // reasons this pin has moved for before and for no third one. The snapshot writes its schema
        // version, which moved 37 -> 38 for the modal Attack a "choose one, or both" opened and has
        // not finished; and it writes the registered-capability list, which gained `attack-sequence@1`.
        // Neither is a behaviour change in these nine scenarios - none of them plays a modal Attack, and
        // the chain they all write is the absent one, a single false byte.
        //
        // The EVIDENCE is the equality groups rather than the digests, and all three survive exactly:
        // accepted/rejected/stale/restored/replayed still share one pair, the defense-discarded trio
        // still shares another, and undo-truncated still differs from both. The diagnostic digests move
        // with the authoritative ones because the diagnostic envelope wraps the authoritative payload;
        // they are not an independent measurement, which this file has recorded since 6.1f.
        private static readonly ScenarioDigest[] ExpectedDigests =
        {
            new ScenarioDigest("accepted", "02ad5aa818177e9a483a26ed3a6b44b5450974fc4d095ce5dee00fc0f6323d15", "107ab36eb55bcc5256777fa3fd03ca5e6a466f65a2f9ae105c1e92ed1b8afe4e"),
            new ScenarioDigest("rejected", "02ad5aa818177e9a483a26ed3a6b44b5450974fc4d095ce5dee00fc0f6323d15", "107ab36eb55bcc5256777fa3fd03ca5e6a466f65a2f9ae105c1e92ed1b8afe4e"),
            new ScenarioDigest("stale", "02ad5aa818177e9a483a26ed3a6b44b5450974fc4d095ce5dee00fc0f6323d15", "107ab36eb55bcc5256777fa3fd03ca5e6a466f65a2f9ae105c1e92ed1b8afe4e"),
            new ScenarioDigest("restored", "02ad5aa818177e9a483a26ed3a6b44b5450974fc4d095ce5dee00fc0f6323d15", "107ab36eb55bcc5256777fa3fd03ca5e6a466f65a2f9ae105c1e92ed1b8afe4e"),
            new ScenarioDigest("replayed", "02ad5aa818177e9a483a26ed3a6b44b5450974fc4d095ce5dee00fc0f6323d15", "107ab36eb55bcc5256777fa3fd03ca5e6a466f65a2f9ae105c1e92ed1b8afe4e"),
            new ScenarioDigest("undo-truncated", "3417c119106cd12ecc46b4b1aa04c85536c919217dd5af039dc2ad3fbc78b95f", "c877d015c35da650b20cffd73f0e43e44339c2050a0dd27379d1fe7ff88f6b77"),
            new ScenarioDigest("defense-discarded", "ff2f5ff8b7152f1fb636483f3d642db81ee5a53f6d9fb2e0a3ed6aebdb8189a7", "826a2f75b7184089f4b1b52fdea92eacee87f85b66649c40aefc387ff5ba9013"),
            new ScenarioDigest("defense-discarded-restored", "ff2f5ff8b7152f1fb636483f3d642db81ee5a53f6d9fb2e0a3ed6aebdb8189a7", "826a2f75b7184089f4b1b52fdea92eacee87f85b66649c40aefc387ff5ba9013"),
            new ScenarioDigest("defense-discarded-replayed", "ff2f5ff8b7152f1fb636483f3d642db81ee5a53f6d9fb2e0a3ed6aebdb8189a7", "826a2f75b7184089f4b1b52fdea92eacee87f85b66649c40aefc387ff5ba9013"),
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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

        /// <summary>
        /// Writes the measured digests beside the repository so a mismatch can be diffed rather than
        /// transcribed out of an assertion message.
        /// </summary>
        /// <remarks>
        /// Milestone 7.9d, finding F-TEST-01. This convenience was the fixture's only Unity dependency
        /// - one <c>UnityEngine.Application.dataPath</c> - and it kept the project's only
        /// version-controlled snapshot-digest pin out of the cross-platform harness, so nothing
        /// anywhere compared these payloads across the two runtimes. The root is now found by walking
        /// up from wherever the test assembly runs, which answers the same directory under Unity and
        /// under <c>dotnet test</c>, and a failed write is swallowed on purpose: this is a diagnostic
        /// aid, and a read-only or relocated output directory must not turn a passing digest
        /// comparison into a failing one. The comparison itself is the test and asserts above.
        /// </remarks>
        private static void WriteMeasuredDigests(IReadOnlyList<ScenarioDigest> measured)
        {
            var builder = new StringBuilder();
            foreach (var digest in measured)
            {
                builder.AppendLine(
                    $"new ScenarioDigest(\"{digest.Name}\", \"{digest.AuthoritativeSha256}\", \"{digest.DiagnosticSha256}\"),");
            }

            if (!TryFindRepositoryRoot(out var root))
            {
                return;
            }

            try
            {
                var directory = Path.Combine(root, "tmp");
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    Path.Combine(directory, "milestone-6-1f-measured-digests.txt"),
                    builder.ToString(),
                    new UTF8Encoding(false));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>
        /// The repository root, identified by the two directories every checkout has, from the test
        /// assembly's own location and then from the working directory. Both runtimes reach it: Unity
        /// runs the assembly out of <c>Library/ScriptAssemblies</c> and the harness out of
        /// <c>Tools/DotnetTests/bin</c>, and both are inside the checkout.
        /// </summary>
        private static bool TryFindRepositoryRoot(out string root)
        {
            foreach (var start in new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory()
            })
            {
                for (var directory = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                    directory != null;
                    directory = directory.Parent)
                {
                    if (Directory.Exists(Path.Combine(directory.FullName, "Assets")) &&
                        Directory.Exists(Path.Combine(directory.FullName, "Tools")))
                    {
                        root = directory.FullName;
                        return true;
                    }
                }
            }

            root = null;
            return false;
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
