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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnapshotCompatibilityCorpusTests
    {
        /// <summary>
        /// Milestone 6.2 moved every fixture from v8 to v9 and added the two component cases. The
        /// migrate-or-reject decision this rename records is reject: a v8 payload could not represent a
        /// token or a marker, so reading one as a component-free match would have been faithful, and it
        /// is refused anyway - the engine has one read path per version, and no persisted state outside
        /// this corpus exists to strand. See <c>AuthoritativeSnapshotSerializer.CurrentSchemaVersion</c>.
        /// Milestone 6.3 keeps schema v9 and deliberately re-pins every fixture because the registered
        /// synthetic token definition moved from revision 1 to revision 2 to acquire its immutable
        /// property content. The content decision is reject: a v9 payload bound to Hazard v1 names a
        /// definition revision the trusted catalog no longer supplies, rather than silently gaining v2
        /// semantics. No persisted state outside this test corpus exists to migrate.
        /// Milestone 6.8 keeps schema v9 for the same reason and re-pins every fixture again, for two
        /// registration causes: the capability list gained the five synthetic modification capabilities,
        /// and Hazard moved from revision 2 to revision 3 to carry the typed rule its property now
        /// supplies. Both decisions are reject rather than migrate: a payload naming a capability set
        /// this build does not register, or a token revision the catalog no longer supplies, states
        /// semantics that no longer exist, and there is still no persisted state outside this corpus.
        /// The general modification contracts themselves added no wire field - they are definition
        /// content that a restored effect or token rebinds to from the trusted catalog.
        /// Milestone 6.8a keeps schema v9 and re-pins every fixture for one content cause: Red Bulwark's
        /// printed Primary Attack became its showcase Skill, so that card definition advanced from
        /// revision <c>phase-5-1a-rev-3</c> to <c>phase-6-8a-rev-1</c>. The decision is reject rather
        /// than migrate: a v9 payload naming Red Bulwark at the 5.1a revision states a printed action
        /// set the trusted catalog no longer supplies, and reading it as the converted card would
        /// silently give a saved match a Skill where it recorded an Attack.
        /// The delta was read at the byte level either side of the change rather than assumed. Every
        /// payload keeps its exact decoded length, and each differs in six to eight bytes across
        /// exactly two regions: the definition table's Red Bulwark entry and the <c>red-bulwark-card</c>
        /// instance's definition reference. Both are the same sixteen-character revision string, so
        /// nothing shifted. The capability registration list and the Hazard token revision are
        /// byte-identical, and the replaced action set never appears at all, because the payload names
        /// definitions and the catalog supplies what they contain.
        /// Milestone 6.8h keeps schema v9 and re-pins exactly one fixture, <c>attached-marker-v9</c>,
        /// for one cause: the marker capability now composes its attachment's instance ID through
        /// <c>MarkerInstanceIdentity</c>, so the persisted ID gains the action and the round/turn
        /// anchors. The decision here is accept without migration, unlike the revision causes above: a
        /// marker instance ID is an opaque wire string that nothing parses, re-derives, or compares
        /// against content, so a v9 payload written before this milestone still restores and still
        /// names the same attachment. Only newly captured payloads differ, which is why the fixture is
        /// re-pinned rather than the reader changed. The delta was read at the byte level: the decoded
        /// payload grows from 3232 to 3261 bytes in exactly one region - common prefix 2957 bytes,
        /// common suffix 238 bytes - where the ID's single-byte length prefix moves from 51 to 80 and
        /// the string gains <c>:primary-component-skill</c> and <c>:r1t1</c>. Both lengths stay inside
        /// one 7-bit-encoded prefix byte, so nothing else shifted, and the other six payloads including
        /// <c>placed-token-v9</c> are byte-identical, which is the token family's independent proof
        /// that this milestone left it alone.
        /// Milestone 6.4 keeps schema v9 and re-pins all seven fixtures for one cause: the build now
        /// registers two more effect capabilities - <c>synthetic-direct-move</c> and
        /// <c>synthetic-object-push</c> - and every payload records the capability contract set it was
        /// written against. The decision is reject rather than migrate, and it needs no reader change
        /// because <c>ValidateCapabilityRegistrations</c> already refuses any payload whose stored list
        /// is not sequence-equal to this build's: a payload written against the sixteen-capability set
        /// states a contract set that no longer describes this build, and restoring it would leave a
        /// saved match unable to resolve a definition binding either new capability. The delta was read
        /// at the byte level rather than assumed. The registration list grows 16 -> 18 entries, the
        /// decoded payload grows 2893 -> 2945 bytes - exactly the 52 bytes those two entries cost as
        /// 7-bit length-prefixed ids plus int32 revisions - and the change is confined to one
        /// contiguous region, with a 608-byte common prefix and a 1986-byte common suffix that are
        /// byte-identical either side. No effect, board, component, or round field moved.
        /// A second and separable 6.4 cause moves only <c>placed-token-v9</c> and
        /// <c>attached-marker-v9</c>: the milestone's displacement Skill card joins the Skill-fixture
        /// hands, and those are the two fixtures built from them, so their definition table gains
        /// <c>placeholder-red-shove</c> and <c>placeholder-blue-shove</c> at
        /// <c>phase-6-4-rev-1</c> and their card list gains one instance per hand. Measured rather than
        /// assumed: 241 bytes on top of the capability delta, of which 77 are the two definition-table
        /// entries and 164 the two instance records. The decision is reject for the same reason as the
        /// capability cause and by the same existing check - a payload written against the two-card
        /// fixture hand states a card set the trusted catalog no longer supplies. The other five
        /// fixtures are untouched by this cause because only the component snapshots use the Skill
        /// fixture, which is that hand's own proof that adding a card to it leaves the ordinary
        /// five-card matrix alone, exactly as 6.2a's separate-profile decision intended.
        /// Milestone 6.8k keeps schema v9 and re-pins all seven fixtures for one registration cause
        /// with two parts: the capability list gains <c>synthetic-choose-target</c>, and
        /// <c>synthetic-direct-move</c> moves from revision 1 to revision 2 because its subject
        /// parameter widened from a typed hero to a typed coordinate. The decision is reject rather
        /// than migrate, by the same existing check as 6.4's cause: a payload recording the
        /// eighteen-capability set with direct-move at revision 1 states a contract set this build no
        /// longer supplies, and restoring one would ask the build to resolve a Hero input into a
        /// Coordinate parameter. Measured rather than assumed, at the same authoritative-payload layer
        /// as the 6.4 record: the five ordinary fixtures each grow exactly 28 bytes - the one new
        /// registration entry as a 7-bit length-prefixed 23-character id plus an int32 revision - in
        /// one contiguous region with a 452-byte common prefix and every later section byte-identical.
        /// A second and separable cause moves <c>placed-token-v9</c> and <c>attached-marker-v9</c> by
        /// 4 more bytes each: the two Skill-fixture shove cards advance from <c>phase-6-4-rev-1</c> to
        /// <c>phase-6-8k-rev-1</c> - one byte longer, twice per card across the card and effect
        /// revision strings - because their move branch now runs a target-selection node whose
        /// published space is the direct move's subject. The reject decision for that cause is the
        /// content-revision check, exactly as at 6.8a.
        /// Milestone 6.4a moves every fixture from v9 to v10 and adds the eighth,
        /// <c>pending-correction-v10</c>: a completed Object Push has displaced one minion outside the
        /// Battle Zone and the after-action correction is paused on its team's route choice between
        /// two equal-shortest return endpoints. The pending block carries the owning action's
        /// identity, the stage, the deciding team, and candidate identities only - routes and costs
        /// are never on the wire, because restore re-derives the whole correction from the restored
        /// board and the Tie Breaker and refuses any payload the derivation does not reproduce. The
        /// migrate-or-reject decision is reject, as at every bump: a v9 payload could not represent a
        /// paused correction, the engine has one read path per version, and no persisted state exists
        /// outside this corpus. Measured rather than assumed: each carried-over payload keeps its
        /// exact decoded length and differs from its v9 predecessor in the one version integer,
        /// because the correction block is written only under continuation kind 4 and no carried-over
        /// fixture holds one.
        /// Milestone 6.5 keeps schema v10 and re-pins all eight fixtures for two separable causes, and
        /// adds no fixture: it introduces no persisted family, because the unit supply a Spawn draws
        /// from is derived on restore as the configured roster minus the restored positions and never
        /// written. The first cause reaches every payload - the capability list gains
        /// <c>synthetic-actor-space</c>, <c>synthetic-place</c>, and <c>synthetic-spawn</c> - and the
        /// decision is reject rather than migrate, by the same existing
        /// <c>ValidateCapabilityRegistrations</c> sequence check 6.4 and 6.8k relied on: a payload
        /// recording the nineteen-capability set states a contract set this build no longer supplies.
        /// Measured rather than assumed: each of the five ordinary fixtures grows exactly 66 bytes -
        /// the three registration entries as 7-bit length-prefixed ids of 21, 15, and 15 characters
        /// plus an int32 revision each - in one contiguous region with a 452-byte common prefix and
        /// every later section byte-identical.
        /// The second cause moves only the three Skill-fixture payloads, by 4 bytes the other way: the
        /// two shove cards become the fixture hand's board-object Skill and advance from
        /// <c>phase-6-8k-rev-1</c> to <c>phase-6-5-rev-1</c>, one character shorter, and each card
        /// writes its revision twice - once for the card and once for its effect definition - so the
        /// string appears four times per payload and each payload nets 66 - 4 = 62 bytes. Its reject
        /// decision is the content-revision check, exactly as at 6.8a: a payload naming the shove card
        /// at the 6.8k revision states a branch set the trusted catalog no longer supplies, and reading
        /// it as the widened card would silently give a saved match Place and Spawn options it never
        /// recorded.
        /// Milestone 6.8b keeps schema v10 and re-pins every fixture for one content cause, the same
        /// shape 6.8a recorded: Blue Feint's printed Primary Attack became its showcase Skill, so that
        /// card definition advanced from <c>phase-5-1a-rev-3</c> to <c>phase-6-8b-rev-1</c>. The
        /// decision is reject rather than migrate, because a payload naming Blue Feint at the 5.1a
        /// revision states a printed action set the trusted catalog no longer supplies, and reading it
        /// as the converted card would silently give a saved match a Skill where it recorded an Attack.
        /// Measured at the byte level either side of the change rather than assumed: both revision
        /// strings are sixteen characters, so every payload keeps its exact decoded length and differs
        /// in exactly eight bytes across exactly two regions - the definition table's Blue Feint entry
        /// and the <c>blue-feint-card</c> instance's definition reference - which is the four positions
        /// where the two strings differ, twice. <c>pending-choice-v10</c> differs in twelve bytes
        /// across three regions rather than two, because the Blue hero is its acting hero and its
        /// paused Movement continuation records the same source definition reference a third time. The
        /// capability registration list is byte-identical, and the replaced action set never appears at
        /// all, because the payload names definitions and the catalog supplies what they contain.
        /// Milestone 6.5a keeps schema v10 and re-pins every fixture for two separable causes, both
        /// already familiar. The registration list gains <c>synthetic-push-target</c>, p.18's Push with
        /// the target the definition names beside the adjacency-derived declaration 6.4 registered, and
        /// that costs every payload exactly 26 bytes - a 7-bit length-prefixed 21-character id plus an
        /// int32 revision - in one contiguous region. The content cause is Red Needle's printed Primary
        /// Attack becoming its showcase Skill, so its definition advances from
        /// <c>phase-5-1a-rev-3</c> to <c>phase-6-5a-rev-1</c>; both strings are sixteen characters, so
        /// that cause shifts nothing and the string appears twice per payload, in the definition table
        /// and in the <c>red-needle-card</c> instance's definition reference. Measured rather than
        /// assumed: every payload grows by exactly 26 bytes and every occurrence count moves as stated.
        /// Both decisions are reject rather than migrate, by the existing capability-sequence and
        /// content-revision checks.
        /// Milestone 6.5aa moves every fixture from v10 to v11 because a pending Minion Battle
        /// continuation now persists a discriminated unit identity rather than only a minion ID, so
        /// it can faithfully retain a counts-as hero removal choice. The migrate-or-reject decision
        /// is reject: v10 has no hero arm for that union, the engine intentionally keeps one reader,
        /// and no persisted state exists outside this corpus. Red Needle also advances to
        /// <c>phase-6-5aa-rev-1</c> because its target node now declares Object Push as the consuming
        /// activity; restoring the old revision as the corrected definition would silently change
        /// its offered choices, so the existing content-revision check rejects it rather than
        /// migrating it. All fixtures are therefore renamed and re-pinned as v11.
        /// Milestone 6.5b re-pins every fixture for one cause and one only: Red Bombard's printed
        /// Primary Attack becomes its showcase Skill, so its definition advances from
        /// <c>phase-5-1a-rev-3</c> to <c>phase-6-5b-rev-1</c>. It registers no capability and changes
        /// no schema, so the registration list and the version int are byte-identical. Measured in a
        /// throwaway worktree at pre-change revision <c>82fc064</c> rather than assumed: both revision
        /// strings are sixteen characters, so every authoritative payload keeps its exact decoded
        /// length - 2609, 2804, 2984, 3014, 3655, 3704 and 3714 bytes - and differs in exactly eight
        /// bytes, which is the four positions where the two strings differ, twice, in the definition
        /// table's Red Bombard entry and in the <c>red-bombard-card</c> instance's definition
        /// reference. The decision is reject rather than migrate, by the existing content-revision
        /// check: the saved revision states a printed Attack the trusted catalog no longer supplies.
        /// Milestone 6.6 keeps schema v11 and re-pins every fixture for two causes, both registration
        /// rather than persistence: the capability list gains p.19's Swap, Replace, and Remove, and the
        /// synthetic shove card's Skill advances from <c>phase-6-5-rev-1</c> to <c>phase-6-6-rev-1</c>
        /// because it appends the three branches that expose them. Both decisions are reject rather
        /// than migrate, by the existing capability-sequence and content-revision checks: a payload
        /// naming a capability set this build does not register, or a Skill revision offering different
        /// branches, states semantics that no longer exist, and there is still no persisted state
        /// outside this corpus. The three operations themselves add no wire field - they pause through
        /// the existing effect frame and store nothing of their own - which is why the version int is
        /// byte-identical. Measured in a throwaway worktree at pre-change revision <c>86fa070</c>
        /// rather than assumed: every one of the eight payloads grows by exactly 124 bytes in exactly
        /// one contiguous region, with a common prefix and a common suffix either side of it, so
        /// nothing else shifted.
        /// Milestone 6.8l keeps schema v11 and re-pins every fixture for causes that are all content
        /// revisions and one registration list whose <em>length</em> does not move. The selection
        /// capability's declared schema gains the node it selects for, so its two revisions are
        /// replaced rather than joined - 1 and 2 become 3 and 4, the precedent being 6.8k retiring
        /// direct Move revision 1 when it widened the subject - and because a revision is a fixed-width
        /// int beside an unchanged id, the registration list costs exactly nothing while still
        /// differing, which is what makes a payload naming the old contracts reject. Three card
        /// revisions move with it: Red Needle from <c>phase-6-5aa-rev-1</c>, Red Bombard from
        /// <c>phase-6-5b-rev-1</c>, and the two shove Skill cards from <c>phase-6-6-rev-1</c>, all to
        /// <c>phase-6-8l-rev-1</c>. Measured in a throwaway worktree at pre-change revision
        /// <c>f8ad140</c> rather than assumed: Red Needle's string is one character shorter than
        /// before and appears twice per payload, Red Bombard's is the same length, and each shove card
        /// is one character longer and appears twice, so the five ordinary fixtures lose exactly 2
        /// bytes each and the three Skill-fixture ones - <c>placed-token</c>, <c>attached-marker</c>,
        /// and <c>pending-correction</c> - gain exactly 2, with a common prefix and a common suffix
        /// either side. Every decision is reject rather than migrate, by the existing
        /// capability-sequence and content-revision checks: a payload naming revision 1 or 2 of the
        /// selection describes an offer that no longer knows what it is for.
        /// Milestone 6.6a keeps schema v11 and re-pins every fixture for three causes, all of them
        /// registration or content rather than persistence: nothing about a declared eligible piece
        /// kind is stored. The capability list gains <c>synthetic-swap-actor</c>, p.19's Swap declared
        /// a second time with the acting hero as its first object; <c>synthetic-swap</c> and
        /// <c>synthetic-remove</c> move to revision 2, each declaring the kind of piece it may take;
        /// and two card revisions move, Blue Sentinel from <c>phase-5-1a-rev-3</c> because its printed
        /// Primary Attack became this showcase Skill, and the two shove Skill cards from
        /// <c>phase-6-8l-rev-1</c> because their Remove branch now declares p.19's own breadth, both to
        /// <c>phase-6-6a-rev-1</c>. Every decision is reject rather than migrate, by the existing
        /// capability-sequence and content-revision checks: a payload naming the twenty-two-capability
        /// set, or either operation at revision 1, states a contract set that no longer describes this
        /// build, and one naming Blue Sentinel at the 5.1a revision states a printed Attack the trusted
        /// catalog no longer supplies. Measured in a throwaway worktree at pre-change revision
        /// <c>c64e928</c> rather than assumed: every one of the eight payloads grows by exactly 25
        /// bytes, which is the one new registration entry as a 7-bit length-prefixed twenty-character
        /// id plus an int32 revision, and nothing else changes length - both revision ints are
        /// fixed-width and all three content revisions are the same sixteen characters, which is why
        /// the total growth is exactly the entry's own size. The first difference in each payload is
        /// Blue Sentinel's revision in the sorted definition table, after a 239-byte common prefix in
        /// the five ordinary fixtures and a 318-byte one in the three Skill-fixture payloads; the new
        /// registration entry appears immediately after <c>synthetic-swap</c> in the sorted list. The
        /// converted card's revision appears twice per payload - the definition table entry and the
        /// <c>blue-sentinel-card</c> instance's definition reference - and the two shove cards' twice
        /// each again in the three Skill-fixture payloads, which is the whole of the content cause.
        /// Milestone 6.7 moves every fixture from v11 to v12 for one persistence cause and one only: a
        /// paused after-action correction now records whether the action it holds open asked to repeat,
        /// so a second bounded instance is still owed when the correction is answered. The decision is
        /// reject rather than migrate, and it is the strongest reject in this list rather than a
        /// formality: a v11 correction payload has no field for that fact, reading one as "did not ask"
        /// would silently drop the second half of a card's printed text, and reading it as "did" would
        /// invent an action nobody chose. The engine keeps one reader per version and no persisted state
        /// exists outside this corpus. Nothing else in the milestone reaches the wire - the request
        /// itself is an ordinary node outcome inside the effect frame, the repeated instance rebinds its
        /// effect from the trusted definitions, and no capability is registered or revised.
        /// Measured in a throwaway worktree at pre-change revision <c>59766f2</c> rather than assumed.
        /// Every payload has a 30-byte common prefix and then one byte differs, <c>0b</c> to <c>0c</c>,
        /// which is the schema version int; it is fixed-width, so seven of the eight keep their exact
        /// decoded length - 2725, 2920, 3100, 3130, 3335, 3775 and 3824 bytes - and are byte-identical
        /// everywhere else. Only <c>pending-correction</c> moves, 3834 to 3835 bytes, gaining exactly
        /// one trailing <c>00</c>: the boolean saying this correction owes no repeat. That single byte
        /// is the whole persistence cost of the milestone, and the fixture that grows is the one whose
        /// section grew, which is the corpus's own proof that nothing else did.
        /// Milestone 6.7a keeps schema v12 and re-pins every fixture for one content cause: Blue
        /// Longbow's printed Primary Attack became its showcase Skill, so that card definition advanced
        /// from <c>phase-5-1a-rev-3</c> to <c>phase-6-7a-rev-1</c>. The decision is reject rather than
        /// migrate, the same one 6.8a's identical cause took: a v12 payload naming Blue Longbow at the
        /// 5.1a revision states a printed action set the trusted catalog no longer supplies, and reading
        /// it as the converted card would silently give a saved match a Skill where it recorded a Ranged
        /// Attack. The existing content-revision check already refuses it, so no reader changed.
        /// Measured in a throwaway worktree at pre-change revision <c>5c360a8</c> rather than assumed.
        /// All eight payloads keep their exact decoded lengths - 2725, 2920, 3100, 3130, 3335, 3775,
        /// 3824 and 3835 bytes - and each differs in exactly six bytes across exactly two regions: the
        /// definition table's Blue Longbow entry and the <c>blue-longbow-card</c> instance's definition
        /// reference. Both are the same sixteen-character revision string, whose three moving characters
        /// sit at offsets 6, 8 and 15, so nothing shifted and every other section is byte-identical. The
        /// capability registration list is untouched, because this conversion registers and revises
        /// nothing, and the replaced action set never appears at all - the payload names definitions and
        /// the catalog supplies what they contain.
        /// Milestone 6.4c keeps schema v12 and re-pins every fixture for two causes, one registration
        /// and one content. Both Push declarations gain a declared eligible piece kind and therefore
        /// move from revision 1 to revision 2, and four card revisions move - Red Needle and Blue
        /// Longbow, which now say `unit`, and the two fixture Skill cards, which restate p.18's own
        /// unit-or-token breadth. Both decisions are reject rather than migrate, by the existing
        /// capability-sequence and content-revision checks: a payload recording either Push at revision
        /// 1 states a contract set that cannot express which pieces its definitions admit, and reading
        /// it under revision 2 would have to invent that declaration.
        /// Measured in a throwaway worktree at pre-change revision <c>0116c5c</c> rather than assumed,
        /// and the measurement is the clearest evidence that the registration cause costs nothing on the
        /// wire: all eight payloads keep their exact decoded lengths - 2725, 2920, 3100, 3130, 3335,
        /// 3775, 3824 and 3835 bytes - because a capability revision is a fixed-width int32 and every
        /// content revision is the same sixteen characters. The five ordinary fixtures differ in ten
        /// bytes across six runs, two of them the single revision ints inside the
        /// <c>synthetic-object-push</c> and <c>synthetic-push-target</c> registration entries and the
        /// other eight the two moved characters of each card revision at its definition-table and
        /// card-instance sites. The three Skill-fixture payloads differ in eighteen bytes across ten
        /// runs, which is the same two registration ints plus four cards instead of two. Nothing else
        /// moved, and the new parameter itself never appears: a payload records the capability contract
        /// set and the card definitions, and the catalog supplies what those contain.
        /// Milestone 6.9 moves every fixture from v12 to v13 for one persistence cause and one only:
        /// rulebook p.8's mid-turn Lane Push can now be requested, and a turn holding a request is
        /// stopped at that checkpoint until milestones 6.10 and 6.11 resolve it. That stop has to be a
        /// property of the match rather than of the running process, so the request is persisted beside
        /// whatever continuation the causing action left behind. The decision is reject rather than
        /// migrate, and the reason is the same shape 6.7's was: a v12 payload has no field for the
        /// request, reading one as "no push is owed" would let a saved match carry on past a push the
        /// rules made immediate, and there is nothing in a restored board that could tell a side emptied
        /// by a Defeat or a Remove from one emptied by a move p.8 excludes. The engine keeps one reader
        /// per version and no persisted state exists outside this corpus. No capability is registered or
        /// revised and no card revision moves, so the contract set and the definition table are untouched.
        /// Measured in a throwaway worktree at pre-change revision <c>8ccacca</c> rather than assumed.
        /// Every payload differs at byte 31, <c>0c</c> to <c>0d</c>, which is the fixed-width schema
        /// version int. <c>setup</c> keeps its exact decoded length of 2725 bytes, because a match that
        /// has not started a turn writes no card-action authority at all and therefore no request field;
        /// the other seven each grow by exactly one byte - 2920 to 2921, 3100 to 3101, 3130 to 3131,
        /// 3335 to 3336, 3775 to 3776, 3824 to 3825 and 3835 to 3836 - the trailing <c>00</c> saying
        /// this action owes no push. In the five fixtures whose card-action block ends the payload that
        /// byte is appended; in <c>pending-choice</c> and <c>pending-correction</c> it is inserted at
        /// offsets 2921 and 3647 and shifts the rest unchanged. One byte per fixture is the whole
        /// persistence cost of the milestone, and the fixture that does not grow is the corpus's own
        /// proof that the field belongs to the action boundary rather than to the match.
        /// Milestone 6.10 moves every fixture from v13 to v14 for one persistence cause and one only:
        /// rulebook p.8's Battle Zone is now authoritative state the Lane-Push Zone stage advances, so it
        /// travels beside the map reference it is scoped to instead of being derived from the map's
        /// starting-Battle-Zone flag. The decision is reject rather than migrate, and the reason is
        /// unusually concrete for a bump: a v13 payload records where every piece stands and nothing
        /// about which Zone the Lane has reached, and no derivation can recover it - an emptied Battle
        /// Zone and a Zone the Lane never reached look identical in occupancy, and the flag names only
        /// where the match began. Reading a v13 payload as "still the starting Zone" would answer the
        /// Minion Battle, the p.13 Heavy-immunity accompaniment, p.18's displaced-minion correction and
        /// p.8's own Lane-Push cause about the wrong Zone for the rest of the match. No capability is
        /// registered or revised, no card revision moves and no component definition changes, so the
        /// contract set and the definition table are untouched.
        /// Measured in a throwaway worktree at pre-change revision <c>7d73cb7</c> rather than assumed,
        /// and the measurement is unusually uniform because no fixture has pushed the Lane: every one of
        /// the eight grows by exactly 21 bytes - the <c>0x14</c> length prefix plus the twenty characters
        /// of <c>starting-battle-zone</c> - 2725 to 2746, 2921 to 2942, 3101 to 3122, 3131 to 3152, 3336
        /// to 3357, 3776 to 3797, 3825 to 3846 and 3836 to 3857. Two regions differ and no third: the
        /// fixed-width schema int at offset 30, <c>0d</c> to <c>0e</c>, and one contiguous insertion at
        /// offset 1475 in the five ordinary fixtures and at 1710 in the three built from the Skill
        /// fixture, whose definition table carries two more cards. With the schema int alone accounted
        /// for, the common prefix runs to that offset and every later byte is identical, which is the
        /// corpus's own proof that adding the Zone moved no board, card, component or round field.
        /// </summary>
        private static readonly CompatibilityFixture[] SupportedFixtures =
        {
            new CompatibilityFixture("setup-v14", "a31a7e5d568806e7c5ddbeab75629354c7eb1574a43085a1eab73803ac36e79b"),
            new CompatibilityFixture("active-turn-v14", "0160f17b3ffbd4c5656e8b019aac0074f7efc90a9116215c2ee80017cf02c278"),
            new CompatibilityFixture("pending-choice-v14", "274086fd40d5c52e08f3cebd702451e4763cdadd13390c8268701151d88fc3a2"),
            new CompatibilityFixture("end-of-round-v14", "a57c7971c54f88aa1f1e2a9dd56f2a955252706127fb21f90373ce800388be0d"),
            new CompatibilityFixture("pending-minion-battle-v14", "0d6b378384c8327187a9340dcc04c81efe1f5fd5810782fa71721b07f2cfbac0"),
            new CompatibilityFixture("placed-token-v14", "606943da6d216c4e09a147342524f45abe286e57592a51b0cea8034840985a9c"),
            new CompatibilityFixture("attached-marker-v14", "9b8758b72d34d37bfb4fdc179444f49e1ef659d7f05a8909241355b1928f1bec"),
            new CompatibilityFixture("pending-correction-v14", "1638dbb4e871751188648375c9c083df0b7a696561e488b0b48a6d102fb82090")
        };

        [Test]
        public void SupportedVersionedCorpus_RoundTripsByteIdentically()
        {
            var snapshots = CreateSupportedSnapshots();
            Assert.That(snapshots.Keys, Is.EquivalentTo(SupportedFixtures.Select(value => value.Name)));

            var actualHashes = SupportedFixtures
                .Select(fixture => Sha256(snapshots[fixture.Name]))
                .ToArray();
            Assert.That(
                actualHashes,
                Is.EqualTo(SupportedFixtures.Select(fixture => fixture.Sha256).ToArray()),
                $"Compatibility corpus changed. Actual SHA-256 values: {string.Join(", ", actualHashes)}. " +
                "Record an explicit migrate-or-reject decision before updating the corpus.");

            foreach (var fixture in SupportedFixtures)
            {
                var snapshot = snapshots[fixture.Name];
                var restored = CreateRestoreTarget(fixture.Name);
                var result = restored.RestoreSnapshot(snapshot);
                Assert.That(result.Accepted, Is.True, $"{fixture.Name}: {result.Detail}");
                Assert.That(restored.CaptureSnapshot(), Is.EqualTo(snapshot), fixture.Name);
            }
        }

        [Test]
        public void UnsupportedCompletedMatchSchemaAndContentRevision_RejectDistinctly()
        {
            var session = CreateSession();
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                session.CaptureSnapshot(), out var envelope, out var envelopeRejection), Is.True);
            Assert.That(envelopeRejection, Is.EqualTo(RuleEventLogEnvelopeRejectionReason.None));

            var futureCompletedMatchPayload = RewriteSchemaVersion(
                envelope.AuthoritativePayload,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion + 1);
            var schemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    futureCompletedMatchPayload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(schemaException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion));

            var priorSchemaPayload = RewriteSchemaVersion(
                envelope.AuthoritativePayload,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion - 1);
            var priorSchemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    priorSchemaPayload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(priorSchemaException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion),
                "The immediately prior schema is rejected rather than migrated, which every bump so far " +
                "has decided the same way: v7 could not express an Active effect without stating its " +
                "semantics inline (6.2a), and v8 has no section a placed token or attached marker could " +
                "occupy (6.2).");

            var changedDefinitions = session.Projection.Definitions.ToArray();
            var original = changedDefinitions[0];
            changedDefinitions[0] = new CardDefinition(
                original.Id,
                original.ContentRevision + "-incompatible",
                original.DisplayName,
                original.Initiative,
                original.ContentAuthority,
                original.ActionDefinitions);
            var contentException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    envelope.AuthoritativePayload,
                    changedDefinitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(contentException.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.CardDefinitionContentRevisionMismatch));
        }

        private static Dictionary<string, string> CreateSupportedSnapshots()
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var session = CreateSession();
            values.Add("setup-v14", session.CaptureSnapshot());

            CommitUnequalCards(session);
            values.Add("active-turn-v14", session.CaptureSnapshot());

            var movement = session.Projection.AvailableTurnActions.First(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            values.Add("pending-choice-v14", session.CaptureSnapshot());

            session = CreateSession();
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Assert.That(session.Execute(
                    DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }

            values.Add("end-of-round-v14", session.CaptureSnapshot());

            session = CreateSessionWithMinionImbalance();
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var minionChoice = session.Projection.PendingMinionBattleChoice;
            Assert.That(minionChoice, Is.Not.Null);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                minionChoice.SelectInvocations[0])).Accepted, Is.True);
            values.Add("pending-minion-battle-v14", session.CaptureSnapshot());

            // Milestone 6.2's two new wire sections. A version-controlled corpus whose fixtures all
            // carry zero components would leave the sections it exists to protect untested, so one
            // payload holds a token on the board and one holds a marker on a hero.
            values.Add("placed-token-v14", CaptureComponentSnapshot("component-place-branch"));
            values.Add("attached-marker-v14", CaptureComponentSnapshot("component-give-marker"));

            // Milestone 6.4a's wire block: every persisted pending family keeps one corpus fixture,
            // so the paused after-action correction gets the eighth.
            values.Add("pending-correction-v14", CapturePendingCorrectionSnapshot());
            return values;
        }

        /// <summary>
        /// Drives the displacement Skill's Push branch on real official-map geography chosen so the
        /// batch displaces exactly one minion outside the Battle Zone: the pusher stands at (0,-1),
        /// and red-melee-01 at (-1,-1) travels its full two spaces to (-3,-1), outside the zone, while
        /// every other adjacent object ends inside it. The correction then pauses on the Red team's
        /// route choice between the two equal-shortest return endpoints, which is the paused state
        /// this fixture exists to pin.
        /// </summary>
        private static string CapturePendingCorrectionSnapshot()
        {
            var session = CreateCorrectionSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("shove") &&
                    candidate.State.Location == CardLocation.Hand);
                Assert.That(session.Execute(
                    DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0])).Accepted, Is.True);
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-displacement-skill")));
            Assert.That(session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted, Is.True);

            var push = session.Projection.PendingEffectChoice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Value == "displacement-object-push");
            AnswerEffect(session, push);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null,
                "The push must leave the after-action correction paused, or this fixture pins nothing.");
            Assert.That(pending.DestinationCandidates.Count, Is.GreaterThanOrEqualTo(2),
                "The paused stage must be a real route choice.");
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// The Skill-fixture configuration with the acting Red hero placed beside the Battle Zone's
        /// north-east minions rather than on its Throne spawn, so a two-space push crosses the zone
        /// boundary. Scenario occupancies override setup placements exactly as the displacement
        /// dispatch fixture does.
        /// </summary>
        private static MatchSession CreateCorrectionSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, new HexCoordinate(0, -1)),
                    new HeroOccupancy(blueHeroId, new HexCoordinate(-1, -5))
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Runs the synthetic component Skill once, taking the named branch, and captures the result.
        /// Both branches pause on exactly one capability choice, so the driving is the same either way.
        /// </summary>
        private static string CaptureComponentSnapshot(string optionNodeId)
        {
            var session = CreateComponentSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("cache") &&
                    candidate.State.Location == CardLocation.Hand);
                Assert.That(session.Execute(
                    DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-component-skill")));
            Assert.That(session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted, Is.True);

            var option = session.Projection.PendingEffectChoice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Value == optionNodeId);
            AnswerEffect(session, option);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            AnswerEffect(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                $"The {optionNodeId} branch must complete, so the captured payload holds committed " +
                "component state rather than a paused frame.");
            return session.CaptureSnapshot();
        }

        private static void AnswerEffect(MatchSession session, EffectChoiceCommand invocation)
        {
            var result = session.Execute(DirectorCommand.AnswerEffectChoice(invocation));
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// A session whose configured content matches the fixture's. A restore validates the payload's
        /// card definitions against the ones the session was configured with, so a component payload -
        /// which was produced by the Skill-bearing configuration - has to come back into that same one.
        /// </summary>
        private static MatchSession CreateRestoreTarget(string fixtureName) =>
            fixtureName.StartsWith("placed-token", StringComparison.Ordinal) ||
            fixtureName.StartsWith("attached-marker", StringComparison.Ordinal) ||
            fixtureName.StartsWith("pending-correction", StringComparison.Ordinal)
                ? CreateComponentSession()
                : CreateSession();

        private static MatchSession CreateComponentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
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

        private static MatchSession CreateSessionWithMinionImbalance()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var removed = fullSetup.Minions.First(value =>
                value.TeamSide == TeamSide.Blue && value.MinionType == StandardMinionType.Ranged);
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                fullSetup.Minions.Where(value => !value.MinionId.Equals(removed.MinionId)));
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

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Execute(DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }
        }

        private static void CommitUnequalCards(MatchSession session)
        {
            var red = EligibleCards(session, TeamSide.Red).OrderBy(value => Initiative(session, value)).ToArray();
            var blue = EligibleCards(session, TeamSide.Blue).OrderBy(value => Initiative(session, value)).ToArray();
            Assert.That(red, Is.Not.Empty);
            Assert.That(blue, Is.Not.Empty);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red[0].OwnerId, red[0].Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue[blue.Length - 1].OwnerId,
                blue[blue.Length - 1].Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side)
        {
            var heroId = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards.Where(value =>
                value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand);
        }

        private static int Initiative(MatchSession session, CardInstance card)
        {
            return session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).Initiative.Value;
        }

        private static ActionKind FindActionKind(MatchSessionProjection projection, ActionOffer offer)
        {
            return projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
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

        private static string RewriteSchemaVersion(string payload, int schemaVersion)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    reader.ReadString();
                }

                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(schemaVersion);
                }
            }

            return Convert.ToBase64String(bytes);
        }

        private static string Sha256(string payload)
        {
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(Convert.FromBase64String(payload))
                    .Select(value => value.ToString("x2")));
            }
        }

        private readonly struct CompatibilityFixture
        {
            public CompatibilityFixture(string name, string sha256)
            {
                Name = name;
                Sha256 = sha256;
            }

            public string Name { get; }
            public string Sha256 { get; }
        }
    }
}
