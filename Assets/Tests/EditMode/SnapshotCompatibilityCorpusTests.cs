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
        /// Milestone 6.10a moves every fixture from v14 to v15 for one persistence cause: a multi-step
        /// system transition can now stop on an intra-stage continuation, and that continuation is
        /// authority-owned state the round section persists. The decision is reject rather than migrate,
        /// by the same reasoning as every bump: a v14 payload has no field for a paused transition, and
        /// reading one as "nothing is running" would restore a match that had silently skipped a step a
        /// player still owes. What the steps mean stays in the trusted catalog, so no registration list
        /// joins the wire and a payload naming a transition this build no longer supplies is refused
        /// rather than reinterpreted. No capability is registered or revised, no card revision moves and
        /// no component definition changes.
        /// Measured at the byte level rather than assumed, against the v14 payloads captured earlier in
        /// the same session: every one of the eight grows by exactly one byte - the trailing <c>00</c>
        /// saying no transition is paused - 2746 to 2747, 2942 to 2943, 3122 to 3123, 3152 to 3153, 3357
        /// to 3358, 3797 to 3798, 3846 to 3847 and 3857 to 3858. With the fixed-width schema int at
        /// offset 30 accounted for, each differs in exactly one inserted byte at the round section's
        /// marker boundary - offsets 2733, 2812, 2812, 3061, 3266, 3669, 3718 and 3541 - and every byte
        /// before and after is identical. Unlike the Battle Zone at 6.10, no fixture is exempt: a
        /// transition belongs to the match rather than to a board or an action, so even the
        /// <c>setup</c> payload records that none is running.
        /// Milestone 6.12 keeps schema v16 and re-pins every fixture for one content cause: the
        /// synthetic Hazard token moved from revision 3 to revision 4 because its supply count fell from
        /// two to one. The supply is definition content rather than runtime state - the register derives
        /// what remains from the trusted total minus what is on the board - so changing it changes what
        /// revision 3 meant, and the revision advances rather than being reused. The cleanup this
        /// milestone wires is what forced it: p.8 returns every placed token at the end of every round,
        /// a round offers exactly two component-Skill activations, and a two-token supply therefore
        /// could never be exhausted again, which made p.18's short-supply reclamation unreachable. The
        /// decision is reject rather than migrate, by the existing content-revision check: a payload
        /// naming <c>synthetic-hazard-token@3</c> states a supply this build no longer supplies, and
        /// reading it as revision 4 would silently halve a saved match's token pool.
        /// The cleanup itself reaches the wire nowhere. It requests no choice, cannot pause, and so adds
        /// no continuation and no persisted field; the stage cursor it advances has crossed the boundary
        /// since the stage list existed, which is why the schema does not move and the fixture names
        /// keep their <c>-v16</c> suffix.
        /// Measured in a throwaway worktree at pre-change revision <c>1345152</c> rather than assumed.
        /// All eight payloads keep their exact decoded lengths - 2761, 2957, 3137, 3167, 3372, 3812,
        /// 3861 and 3872 bytes - because a revision is a fixed-width int beside an unchanged id. Seven
        /// differ in exactly one byte, <c>03</c> to <c>04</c>, at the component definition table's
        /// Hazard entry: offset 1236 in the five ordinary fixtures and 1471 in the three Skill-fixture
        /// ones. Only <c>placed-token-v16</c> differs in two, gaining the same change at offset 2124
        /// where the placed instance names its own definition, which is the corpus's own proof that a
        /// token's revision reaches the wire twice and its supply count not at all - the payload names
        /// definitions and the catalog supplies what they contain.
        /// The five-card scene keeps schema v16 and re-pins every fixture for one cause, which is
        /// content and nothing else: the configured scene stopped being dealt milestone 6.2's Cache
        /// card as a sixth card per hero, and Blue Feint took all three of its modes - p.18's `if able`
        /// reclamation and placement, and the give/return marker pair - moving from
        /// <c>phase-6-8b-rev-1</c> to <c>phase-6-8b-rev-2</c>. Exactly one card definition moves. Red's
        /// copy of the Cache card was dropped rather than rehoused, so no Red profile moved; Blue
        /// Sentinel is untouched at <c>phase-6-6a-rev-1</c>, and that is a decision rather than an
        /// omission - the placement belongs to its family, but p.8 removes every placed token at the
        /// end of a round and a card is played at most once in a round, so a Sentinel that supplied its
        /// own Hazard could never have one standing when it returned to hand. Nothing else is a cause:
        /// no capability was registered or revised - all four were already in the catalog, since the
        /// Cache card named them - no schema field was added, and the supply is the catalog's rather
        /// than the card's.
        /// Measured in a throwaway worktree at pre-change revision <c>8a84b68</c> rather than assumed.
        /// All eight payloads keep their exact stored length, because the revision string is the same
        /// sixteen characters before and after. Seven differ in exactly two bytes and
        /// <c>pending-choice-v16</c> in three, and every one of them is that revision's final
        /// character: it appears once in the sorted definition table and once where the
        /// <c>blue-feint-card</c> instance names its definition reference - offsets 114 and 3082 in
        /// <c>setup-v16</c>, 166 and 3893 in <c>placed-token-v16</c> - while <c>pending-choice-v16</c>
        /// carries it a third time at 4093, because the Blue hero is its acting hero and its paused
        /// Movement continuation records the same reference, which is the same third occurrence
        /// milestone 6.8b measured in that fixture. The decision is reject rather than migrate, by the
        /// existing content-revision check: a payload naming Blue Feint at <c>phase-6-8b-rev-1</c>
        /// describes a two-mode Skill that can neither place the Hazard a restored match might have
        /// standing nor account for a marker it might be holding.
        /// Milestone 7.1 moves schema v16 to v17 and renames every fixture with it, and there are three
        /// causes rather than one. The schema cause is the hero-Level section the round now carries.
        /// The content cause is that all ten configured card definitions gain a colour, a
        /// classification and either a Tier or p.15's explicit absence of one, so all ten advance to
        /// <c>phase-7-1-rev-1</c>. The third is that each of the six non-Basic Tier I cards now supplies
        /// the four same-colour cards p.15 has it upgrade into, which is twenty-four definitions the
        /// definition table did not have; none of them is dealt, so no fixture's card list grows.
        /// Both decisions are reject. For the schema, see
        /// <c>AuthoritativeSnapshotSerializer.CurrentSchemaVersion</c>: a v16 payload records no Level
        /// at all, and reading one as Level 1 would guess a value p.15 makes authoritative. For the
        /// content, the existing content-revision check already refuses it, and it should: a payload
        /// naming these cards at their 6.x revisions describes cards with no colour and no Tier, which
        /// a restored match's lowest-Tier reading and upgrade candidates now depend on.
        /// Measured in a throwaway worktree at pre-change revision <c>a9fda8b</c> rather than assumed,
        /// and the arithmetic closes exactly. Seven payloads grow by 1082 bytes and
        /// <c>pending-choice-v17</c> by 1081. The definition table gains 1060: twenty-four entries of a
        /// one-byte length prefix plus a 26-to-29-character id plus a one-byte prefix plus the
        /// fifteen-character revision. It then loses 10, because each of the ten configured revision
        /// strings goes from sixteen characters to fifteen, and the card list loses 10 more for the
        /// same reason where each of the ten instances names its definition reference. The hero-Level
        /// section adds 42: an int32 count plus, per hero, a one-byte length prefix, a fourteen-
        /// character hero id and an int32 Level. 1060 - 10 - 10 + 42 = 1082.
        /// <c>pending-choice-v17</c> is one byte smaller because its paused Movement continuation
        /// records the acting card's definition reference and that card is one of the ten, so the same
        /// revision string shrinks a third time. The other three fixtures with a paused resolution do
        /// not, because theirs are the fixture-only Skill cards, whose revisions this milestone leaves
        /// alone. The common prefix is 30 bytes in every fixture, which is where the version field
        /// itself sits.
        /// Milestone 7.1a re-pins all eight for a registration cause and no schema or content cause at
        /// all: <c>CurrentSchemaVersion</c> stays 17, no card definition or revision moves, and the
        /// fixture names keep their <c>-v17</c> suffix because there is no new version to name. What
        /// changed is the registered capability set, which every payload records so a restore can
        /// refuse a build whose capabilities differ - the same cause as milestones 6.4 and 6.8k.
        /// Measured in a throwaway worktree at pre-change revision <c>d3a3aa4</c> rather than assumed.
        /// Every payload grows by exactly 34 bytes: a one-byte length prefix, the twenty-nine-character
        /// <c>synthetic-discard-chosen-card</c> id, and the int32 revision. The registration count is a
        /// fixed int32 and does not move when it goes from 23 to 24. Nothing else in any payload
        /// changes, which is what makes the delta identical across all eight rather than varying with
        /// what each fixture holds.
        /// The decision is reject, and the existing registration check already makes it: a payload
        /// written by a build without this capability describes a match whose effects could not have
        /// required a hero to discard, and a build with it must not silently accept one - that
        /// mismatch is exactly what the check exists to catch. Migration is not available and should
        /// not be, because a registration list is a statement about the build that wrote the payload
        /// rather than state to convert.
        /// Note for a later reader comparing these numbers by hand: a fixture string is the outer
        /// envelope, which base64-encodes the payload, so the outer length moves by 44 or 48 characters
        /// rather than by 34. That is base64 alignment and nothing else - 34 is eleven groups of three
        /// plus one, so the outer length grows by 48 for the two fixtures whose payload length is a
        /// multiple of three (<c>setup-v17</c> at 3843 bytes and <c>pending-choice-v17</c> at 4218) and
        /// by 44 for the other six.
        /// Milestone 7.2 moves schema v17 to v18 and renames every fixture with it, and there are two
        /// causes. The schema cause is the item section the round now carries, immediately after the
        /// hero Levels. The content cause is that the twenty-four generated upgrade targets gain p.16's
        /// Item icon and advance to <c>phase-7-2-rev-1</c>; the ten configured cards are Tier I or Basic,
        /// print no Item icon, and deliberately keep <c>phase-7-1-rev-1</c>, because moving a revision
        /// for content that did not change would declare a compatibility cause that does not exist.
        /// Both decisions are reject. For the schema, see
        /// <c>AuthoritativeSnapshotSerializer.CurrentSchemaVersion</c>: a v17 payload records no item at
        /// all, and reading one as "this hero owns none" is a guess about state p.16 makes authoritative
        /// the moment a hero levels. For the content, the existing content-revision check already
        /// refuses it, and should: a payload naming these cards at <c>phase-7-1-rev-1</c> describes
        /// cards with no Item icon, which is the one thing a placement needs in order to know which slot
        /// a set-aside card belongs under.
        /// Measured in a throwaway worktree at pre-change revision <c>a4b7613</c> rather than assumed,
        /// and the arithmetic is the smallest a schema move has produced here. Every payload grows by
        /// exactly 4 bytes - the int32 item count, which is zero in all eight, because nothing in this
        /// build gains an item - inserted at offset 3861 in <c>setup-v18</c> and correspondingly in the
        /// rest. Outside that insertion every payload differs in exactly 25 bytes: one is the version
        /// field itself at offset 30 (<c>0x11</c> to <c>0x12</c>), and the other twenty-four are the
        /// final character of each generated upgrade definition's revision string, <c>0x31</c> to
        /// <c>0x32</c>, one occurrence each. That each appears exactly once is itself the corpus
        /// confirming milestone 7.1's decision not to deal them: a definition that had an instance would
        /// name its reference a second time, as Blue Feint's did at 6.8b.
        /// The base64 payload string grows by 4 characters in six fixtures and by 8 in
        /// <c>pending-minion-battle-v18</c> and <c>attached-marker-v18</c>, and the outer envelope by 4,
        /// 8 or 12 - base64 alignment again, and the reason the note above says to decode before
        /// measuring.
        /// Milestone 7.2a moves schema v18 to v19 and renames every fixture with it, for one cause with
        /// two parts, both of them the setup section: every hero now writes the four rulebook p.16 item
        /// slots of its dashboard, and an item writes the slot it occupies and the icon it supplies
        /// rather than one field naming both. No fixture holds an item, so only the first part appears
        /// in these bytes. No card definition, capability or component registration moved, and no
        /// content revision changed: this milestone adds no card content, which is why the definition
        /// table is byte-identical either side.
        /// The decision is reject rather than migrate. A v18 payload states no dashboards at all, and
        /// every hero this build supplies has the same four slots, so reading one as "p.16's layout"
        /// would be right for every payload that exists - which is exactly the argument v17 and v16
        /// refused, since the first payload it would be wrong about is the first one a verified hero
        /// with a different dashboard writes. The reader also refuses a v19 payload whose dashboards are
        /// not this build's, with its own reason <c>HeroDashboardMismatch</c> rather than a malformed
        /// payload, and that refusal is proved on a tampered payload in
        /// <c>DashboardSlotsAndComposedItemTotalsTests</c> rather than here, because it is a content
        /// mismatch of the kind the definition table already has a case for.
        /// Measured in a throwaway worktree at pre-change revision <c>6a83b15</c> rather than assumed.
        /// Every payload grows by exactly 120 bytes, in TWO contiguous insertions of 60 rather than one,
        /// because the slots are written per hero and the second hero's id and player id sit between
        /// them: in the five ordinary fixtures the insertions begin at offsets 2502 and 2534, separated
        /// by exactly the 32 bytes of <c>blue-test-hero</c> and <c>blue-test-player</c> with their
        /// length prefixes, and in the three Skill-fixture payloads at 2737 and 2769. Each 60-byte block
        /// is fifteen int32s and decodes to exactly what the layout says: a slot count of 4, then
        /// (1, 1 icon, Attack), (2, 1 icon, Defense), (3, 1 icon, Initiative), and (4, 3 icons, Range,
        /// Movement, Radius). Outside those two insertions exactly ONE byte differs in every payload -
        /// the version field at offset 30, <c>0x12</c> to <c>0x13</c> - and every later section is
        /// byte-identical, which is the corpus confirming that no item, board, card or round field
        /// moved. 120 is a multiple of three, so for once base64 alignment adds nothing: each payload
        /// string grows by exactly 160 characters.
        /// Milestone 7.2b keeps schema v19 and re-pins every fixture for one registration cause: the
        /// build registers <c>synthetic-radius-target</c>, the target selection bounded by rulebook
        /// p.9's printed Radius icon rather than by a number a definition writes down. The decision is
        /// reject rather than migrate, by the same existing <c>ValidateCapabilityRegistrations</c>
        /// sequence check milestones 6.4, 6.8k, 6.5 and 7.1a relied on: a registration list is a
        /// statement about the build that wrote the payload rather than state to convert, and a payload
        /// recording the twenty-nine-reference set describes a build that cannot resolve this card's
        /// binding.
        /// Measured in a throwaway worktree at pre-change revision <c>889007c</c> rather than assumed,
        /// and it is uniform. Every payload grows by exactly 28 bytes - a one-byte length prefix, the
        /// twenty-three-character id, and the int32 revision - inside one contiguous changed region of
        /// the registration list that begins at the list's own int32 count, which moves 29 to 30, and
        /// ends at the new entry. The entry sorts last among the ids that region covers, immediately
        /// after <c>synthetic-push-target@2</c>. Every byte before the count and after the new entry is
        /// identical, which is the corpus confirming what this milestone claims about its own content:
        /// the two cards that print a Radius are dealt only by their own configuration, so no
        /// definition-table entry, card instance, or content revision moves here. The base64 payload
        /// string grows by 36 or 40 characters depending on each payload's length modulo three, which
        /// is alignment alone and is recorded so a later reader comparing by hand is not misled.
        /// Milestone 7.2c keeps schema v19 and re-pins every fixture for two causes, one registration
        /// and one content. The build registers <c>synthetic-card-radius</c>, which publishes a card's
        /// composed Radius Total as a fact a definition can bind, and Red Needle advances from
        /// <c>phase-7-1-rev-1</c> to <c>phase-7-2c-rev-1</c> because it now prints rulebook p.9's Radius
        /// icon at the value 4 its own text has named since milestone 6.5a. The other nine configured
        /// cards deliberately keep <c>phase-7-1-rev-1</c>: they share one revision constant, and moving
        /// it would declare a compatibility cause for nine cards that did not change, which is the
        /// reasoning milestone 7.2 recorded when it left all ten alone.
        /// Both decisions are reject. The registration is refused by the existing
        /// <c>ValidateCapabilityRegistrations</c> sequence check, for the reason a registration list is
        /// always refused: it states which contracts the writing build had. The content is refused by
        /// the existing content-revision check, and should be: a payload naming Red Needle at
        /// <c>phase-7-1-rev-1</c> describes a card whose Place is bounded by a number written into its
        /// effect rather than by a printed icon, which is exactly the reading p.9 contradicts and which
        /// p.16's items cannot raise.
        /// Measured in a throwaway worktree at pre-change revision <c>570ba0e</c> rather than assumed,
        /// and the arithmetic closes exactly. Every payload grows by 28 bytes: 26 for the registration
        /// entry - a one-byte length prefix, the twenty-one-character id, and the int32 revision - plus
        /// 1 byte for each of the two places Red Needle's revision string appears, the definition table
        /// and that card instance's own definition reference. The count is measured rather than
        /// reasoned: <c>phase-7-2c-rev-1</c> occurs exactly twice in every payload afterwards and
        /// <c>phase-7-1-rev-1</c> drops by exactly two, in all eight. The first differing byte is the
        /// definition table's length prefix for that string, at offset 1270 in the five ordinary
        /// fixtures and 1466 in the three Skill-fixture ones.
        ///
        /// Milestone 7.3 keeps schema v19 again and re-pins every fixture for one content cause: each
        /// hand supplies rulebook p.6's eighteenth card, its Ultimate, at the new revision
        /// <c>phase-7-3-rev-1</c>. Nothing else moves. The schema is untouched because the milestone adds
        /// no field to the wire at all - p.16 records the unlock by moving the card, so the state that
        /// says a hero has one is the card's own location, which every payload has always carried, and
        /// the rule the Ultimate supplies is re-derived from the trusted definition exactly as a placed
        /// token's properties are. No existing card's revision moves either: these two definitions are
        /// new content rather than changed content.
        /// The decision is reject, by the existing content-revision check and for the reason every
        /// definition-table change is refused: a payload written without these two describes a build
        /// whose heroes have no Ultimate to unlock, and reading it as "unlocked nothing" would be a
        /// guess about state milestone 7.5's level purchase makes authoritative.
        /// Measured in a throwaway worktree at pre-change revision <c>0957e6f</c> rather than assumed,
        /// and the arithmetic closes exactly. Every payload grows by 83 bytes in two insertions and one
        /// changed byte, and no fixture differs anywhere else: the definition count at offset 34 moves
        /// from 34 to 36, 42 bytes are inserted for <c>placeholder-blue-ultimate</c> - a one-byte length
        /// prefix, its twenty-five-character id, a one-byte prefix and the fifteen-character revision -
        /// and 41 for the one-character-shorter <c>placeholder-red-ultimate</c>. The table is ordered by
        /// definition id, so the two land apart: at offsets 776 and 1497 in the five ordinary fixtures
        /// and 895 and 1732 in the three Skill-fixture ones, which carry additional Skill-card
        /// definitions ahead of them.
        ///
        /// Milestone 7.4 moves schema v19 to v20 and renames every fixture with it, for one cause: every
        /// team's remaining rulebook p.13 Life counters join the round section, immediately after the
        /// items. It is the milestone in which a Life counter can first change - p.13's "Defeated hero's
        /// team spends the number of Life counters, equal to that hero's lowest card Tier" is the only
        /// rule that moves one - so until now the count existed solely as the setup constant the HUD
        /// printed.
        /// The decision is reject rather than migrate, and the reason is the sharpest of the series. A
        /// v19 payload records no counters at all, and the only reading available for their absence is
        /// "nobody has spent any" - a statement about a resource whose exhaustion loses the game. It is
        /// right for every payload a v19 build could produce, because no v19 rule spends one, and that
        /// is exactly why it must not be written into the reader: the first payload it would be wrong
        /// about is the first one this milestone writes, and the match would come back with a full pile
        /// and a team that had already lost.
        /// Measured in a throwaway worktree at pre-change revision <c>f7552e0</c> rather than assumed,
        /// and the arithmetic closes exactly. Every payload grows by 31 bytes in one contiguous
        /// insertion, and exactly one byte differs outside it - the version at offset 30. The insertion
        /// is the section itself: a four-byte count of two, then <c>blue-team</c> as a one-byte length
        /// prefix, nine characters and a four-byte counter total, then <c>red-team</c> at one character
        /// shorter, which is 4 + 14 + 13. It lands after the items in each fixture, at offset 4127 in
        /// <c>setup</c>, 4206 in <c>active-turn</c> and <c>pending-choice</c>, 4455 in
        /// <c>end-of-round</c>, 4660 in <c>pending-minion-battle</c>, 4935 in
        /// <c>pending-correction</c>, 5063 in <c>placed-token</c> and 5112 in <c>attached-marker</c>.
        /// 31 is not a multiple of 3, so the base64 the envelope carries grows by 44 characters in the
        /// two fixtures whose length crosses a padding boundary and by 40 in the other six.
        ///
        /// Milestone 7.5 keeps schema v20 - the version byte at offset 30 is unchanged and no field
        /// moved - and re-pins every fixture for two content causes, neither of which is a definition
        /// revision.
        /// The first is p.6's own setup, finally dealt: "return the remaining Tier II, Tier III, and
        /// Tier IV cards to your deck." Milestones 7.1 and 7.3 supplied those thirteen cards per hero as
        /// definitions with no instance, because nothing could take one; p.15's Upgrade a Card step
        /// searches the deck for them, so this milestone deals them and the configured match holds
        /// thirty-six cards where it held ten.
        /// The second is the coin ledger. p.15's level purchase spends from the balances p.13's Defeat
        /// fills, so the round authority now asks the dispatcher for that ledger when it composes
        /// itself rather than leaving it to be created by whichever rule reaches it first - two ledgers
        /// would be two answers to what a hero can afford - and a section that used to be absent until
        /// the first Defeat is present from the start.
        /// The decision is reject rather than migrate, and it is the ordinary one for a content cause. A
        /// payload written before this milestone names ten card instances for a match this build deals
        /// thirty-six, and the only reading available for the twenty-six it does not mention is "this
        /// hero's deck is empty" - which is right for every payload the earlier build could produce, and
        /// wrong for the first one this milestone writes. There is still no persisted state outside this
        /// corpus to migrate.
        /// Measured in a throwaway worktree at pre-change revision <c>a4f76f2</c> rather than assumed,
        /// and the arithmetic closes exactly. Every payload changes in the same four places and nowhere
        /// else. One byte is replaced - the card-instance count at offset 3160 (3137 in
        /// <c>pending-minion-battle</c>, 3395 in the two marker fixtures, 3523 in <c>placed-token</c>,
        /// which carry more definitions ahead of it), where 10 becomes 36 inside an int32 whose other
        /// three bytes stay zero. Then 1203 bytes are inserted after the Red hero's five dealt cards and
        /// 1213 after the Blue hero's: thirteen instances each, every one an id, a definition id, a
        /// revision, an owner and four state values, and the twenty-byte difference between the two
        /// heroes is the difference between their card names. Then 46 bytes for the ledger - a
        /// four-byte balance count, <c>primitive-hero</c> and <c>blue-test-hero</c> with an int32 each,
        /// and a four-byte awarded-outcome count - which <c>setup</c> carries once and the seven
        /// mid-turn fixtures carry twice, because a paused card-action section persists its own copy.
        /// So <c>setup</c> grows by 1203 + 1213 + 46 = 2462 bytes and every other fixture by 2508.
        /// </summary>
        private static readonly CompatibilityFixture[] SupportedFixtures =
        {
            new CompatibilityFixture("setup-v20", "d0f4379c9acd2b6b541a68be03170f4435cebb4effd9dc178841dfa8859b5277"),
            new CompatibilityFixture("active-turn-v20", "319f454f3ecd304ecf4263437acb11c97cf5b654bbc790d4ba6dcc78a8713796"),
            new CompatibilityFixture("pending-choice-v20", "549d3d877708d3b07feb71b6907c8943cfab0736fbb198acf9076a5500b54f04"),
            new CompatibilityFixture("end-of-round-v20", "300da05b1f278130609ff5c7f047a513713c78d25855537bfd6a971049da847e"),
            new CompatibilityFixture("pending-minion-battle-v20", "44e184f0bd705372d73bb7bf27953f1b4e24afa19d3f80258d788ca025db68c1"),
            new CompatibilityFixture("placed-token-v20", "6650e680bcf51480f240b305c44fce5b8b069fa7569a97e36fd52b522f4dc713"),
            new CompatibilityFixture("attached-marker-v20", "ecca0e01ead26ebad03b15f0acc6b30b448d3ebf84f116538ecb0ce0af256ef3"),
            new CompatibilityFixture("pending-correction-v20", "8935e3ab359a0bc239a226146f703c4e8fd901722ad172d3ab863728d8efa93e")
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
                original.Progression,
                original.ActionDefinitions,
                original.UltimateRule);
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
            values.Add("setup-v20", session.CaptureSnapshot());

            CommitUnequalCards(session);
            values.Add("active-turn-v20", session.CaptureSnapshot());

            var movement = session.Projection.AvailableTurnActions.First(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            values.Add("pending-choice-v20", session.CaptureSnapshot());

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

            values.Add("end-of-round-v20", session.CaptureSnapshot());

            session = CreateSessionWithMinionImbalance();
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var minionChoice = session.Projection.PendingMinionBattleChoice;
            Assert.That(minionChoice, Is.Not.Null);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                minionChoice.SelectInvocations[0])).Accepted, Is.True);
            values.Add("pending-minion-battle-v20", session.CaptureSnapshot());

            // Milestone 6.2's two new wire sections. A version-controlled corpus whose fixtures all
            // carry zero components would leave the sections it exists to protect untested, so one
            // payload holds a token on the board and one holds a marker on a hero.
            values.Add("placed-token-v20", CaptureComponentSnapshot("component-place-branch"));
            values.Add("attached-marker-v20", CaptureComponentSnapshot("component-give-marker"));

            // Milestone 6.4a's wire block: every persisted pending family keeps one corpus fixture,
            // so the paused after-action correction gets the eighth.
            values.Add("pending-correction-v20", CapturePendingCorrectionSnapshot());
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
