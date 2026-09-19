using GuardsDigital.Core.Cards.Verified;
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
using GuardsDigital.Core.Rules.CardResolution;
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
        ///
        /// Milestone 7.6 moves schema v20 to v21 and renames every fixture with it, for one cause: which
        /// heroes bought a level in the current round joins the round section, immediately after the Life
        /// counters. It is the milestone in which that question can first be asked - rulebook p.8's
        /// Collect a Pity Coin pays "All players who have NOT leveled up this round" - and it is asked a
        /// stage later than the Level Up that answers it, so a match saved between the two stages has to
        /// carry the answer.
        /// The decision is reject rather than migrate, on the same ground as the Life counters before it.
        /// A v20 payload records no such section, and the only reading available for its absence is
        /// "nobody levelled this round". That is right for every payload a v20 build could produce,
        /// because no v20 rule records a level purchase, and it is exactly why it must not be written
        /// into the reader: the first payload it would be wrong about is the first one this milestone
        /// writes, and a hero that had already levelled would come back eligible and collect a coin the
        /// page denies it.
        /// Measured in a throwaway worktree at pre-change revision <c>3b8e55d</c> rather than assumed,
        /// and it is the smallest delta this corpus has recorded. Every payload grows by exactly 4 bytes
        /// in one contiguous insertion - the int32 count of an empty list, four zero bytes - with exactly
        /// one byte differing outside it, the version at offset 30. Every fixture is empty there because
        /// none of them reaches an end of round at all, which is the honest reading of the number: what
        /// this milestone adds to a payload that never levels is the statement that nobody did. The
        /// insertion lands after the Life counters, at offset 6574 in <c>setup</c>, 6653 in
        /// <c>active-turn</c> and <c>pending-choice</c>, 6902 in <c>end-of-round</c>, 7107 in
        /// <c>pending-minion-battle</c>, 7382 in <c>pending-correction</c>, 7510 in <c>placed-token</c>
        /// and 7559 in <c>attached-marker</c>. 4 is not a multiple of 3, so the base64 the envelope
        /// carries grows by 4 or 8 characters depending on where each payload sits against a padding
        /// boundary.
        /// Milestone 7.9 keeps schema v21 and re-pins nothing. It adds two fixtures and changes no
        /// existing one, which is the compatibility evidence rather than an absence of it: all eight
        /// digests above are byte-identical, because a corpus that gains coverage of a state shape it
        /// did not reach is not a change to the wire. The two are <c>paused-level-up-v22</c>, a match
        /// stopped inside p.15's Upgrade a Card carrying a hero's Level, coins, items and a
        /// system-transition continuation at once, and <c>ended-match-v22</c>, a match p.13's last Life
        /// counter has ended carrying its derived outcome. Both are shapes the complete synthetic loop
        /// reaches every time and no earlier corpus fixture reached at all, so nothing in the sections
        /// they exercise had version-controlled coverage before this milestone. There is no
        /// migrate-or-reject decision to record because no existing payload's meaning moved; the
        /// standing decision for every future cause is unchanged.
        /// Milestone 8.4 keeps schema v21, re-pins nothing, and adds one fixture. The re-baseline had
        /// predicted a schema move here for verified hand and setup persistence, and implementation
        /// showed there is nothing to move: the definition table is a list of id-and-revision pairs
        /// (see <c>AuthoritativeSnapshotSerializer.WriteDefinitions</c>), which is what a definition
        /// has been on the wire since the corpus began, and what a card's face prints - values, words,
        /// item icons, a text composition still owed - lives in the trusted catalog the reader binds
        /// to. Verified content is content, not a new wire shape. The mirror's shared definition set
        /// needed nothing either: two heroes owning instances of one definition is what
        /// <c>CardDefinitionReference</c> already means. So there is no migrate-or-reject decision to
        /// record, and every digest above is byte-identical with two mirrored Wasps composing the
        /// configured session, which is the compatibility evidence rather than an absence of it - the
        /// eight ordinary fixtures and both component ones are built from the internal-test and
        /// Skill-fixture showcase configurations, which this milestone deliberately left alone so that
        /// the corpus measures the wire rather than the content that happens to be in the scene.
        /// Milestone 8.6 moves schema v21 to v22 and renames every fixture with it, for one cause: a
        /// repeat continuation - how many repeats an instance's chain has already run, and the target it
        /// inherited - joins both the pending Skill frame and the owed repeated action. Center of Mass's
        /// "may repeat up to two times" is the first verified count above one, so "am I a repeat" stopped
        /// being the whole answer; her two siblings print "on the same target", which has to cross an
        /// action boundary a snapshot can fall inside.
        /// The decision is reject rather than migrate, on the same ground as every bump before it. A v21
        /// payload records no such section, and the only reading available for its absence is "this is
        /// the first instance and nothing is inherited". That is right for every payload a v21 build
        /// could produce, because no v21 content declares a maximum above one or a target binding, and
        /// it is exactly why it must not be written into the reader: the first payload it would be wrong
        /// about is one this milestone writes, where a second repeat would come back believing it was
        /// the first and could ask for one more than the card allows.
        /// Measured in a throwaway worktree at pre-change revision <c>3578dda</c> rather than assumed,
        /// and the measurement is the reason this entry can be specific about what did <i>not</i>
        /// change. Every payload grows by exactly 27 bytes in one contiguous insertion, and the 27 bytes
        /// are not the new section at all: they are one more entry in the capability registration list -
        /// a one-byte length prefix, the twenty-two characters of <c>synthetic-range-target</c>, and a
        /// four-byte revision - inserted after <c>synthetic-radius-target</c>, which is where ordinal
        /// order puts it. Outside that insertion exactly four bytes differ in every fixture: the schema
        /// version at offset 30, the registered-capability count (31 to 32), direct Move's revision (2
        /// to 3) and Place's revision (1 to 2). The insertion lands at offset 2227 in <c>setup</c>,
        /// <c>active-turn</c>, <c>pending-choice</c>, <c>end-of-round</c>, <c>pending-minion-battle</c>,
        /// <c>paused-level-up</c> and <c>ended-match</c>, at 2462 in <c>placed-token</c>,
        /// <c>attached-marker</c> and <c>pending-correction</c>, and at 1305 in
        /// <c>verified-hand-setup</c>, which carries a smaller definition table.
        /// <c>verified-hand-setup</c> differs in 54 further single bytes, every one of them the same
        /// character: Wasp's content revision moving from <c>wasp-8-3-rev-1</c> to <c>wasp-8-6-rev-1</c>
        /// in the eighteen definition-table rows and the thirty-six per-seat instance rows, which is the
        /// coupling milestone 8.3 built that revision for and the first time a slice has exercised it.
        /// The repeat continuation itself adds nothing to any of those eleven, and that is a statement
        /// about the corpus rather than about the field: the continuation is written inside a pending
        /// Skill frame and inside an owed repeated action, and none of the eleven holds either, so none
        /// of them has anywhere to put one. That is precisely why a twelfth fixture is added -
        /// <c>paused-orbit-repeat-v22</c>, a match stopped inside the second instance of a bounded
        /// repeat, carrying a count of one and the target that instance inherited. Without it the
        /// schema move this milestone owes would have no version-controlled coverage at all, which is
        /// the same reason milestone 8.4 added the verified-hand fixture rather than swapping one.
        /// The added fixture is <c>verified-hand-setup-v22</c>, and it is added rather than swapped
        /// for one of the others because until now no payload in this corpus carried verified content
        /// at all: the ids, the <c>wasp-8-3-rev-1</c> revisions and the thirty-six per-seat instance
        /// identities of a mirrored deal had no version-controlled coverage, so a later slice advancing
        /// Wasp's content revision would have moved no digest here and said nothing.
        /// Milestone 8.8 keeps schema v22 and re-pins all twelve fixtures for two content causes, and
        /// this is the slice the entry above predicted. Both Push declarations advance to revision 3 -
        /// they carry the printed least distance, and the batch carries the count bound and the target
        /// relation Wasp's kinetic Skills print - and Wasp's content revision advances from
        /// <c>wasp-8-6-rev-1</c> to <c>wasp-8-8-rev-1</c> because two more of her cards now carry a
        /// composition. The decision is reject rather than migrate on the same ground as every
        /// registration cause before it: a payload naming <c>synthetic-object-push@2</c> states a
        /// contract whose declaration could not say how many units a card may push, so restoring it
        /// would hand a saved match a batch that takes everything standing beside the pusher where the
        /// printed card says two. No reader change is needed, because
        /// <c>ValidateCapabilityRegistrations</c> already refuses a list that is not sequence-equal to
        /// this build's, and no persisted state outside this corpus exists to strand.
        /// The delta was read at the byte level in a throwaway worktree at pre-change revision
        /// <c>43cab2e</c> rather than assumed, and it is the smallest this corpus has recorded. Every
        /// payload keeps its exact decoded length. Eleven of the twelve differ in exactly two bytes,
        /// both <c>0x02 -> 0x03</c>: the two Push revisions in the registration list, whose ids are
        /// unchanged and whose length prefixes therefore are too. <c>verified-hand-setup-v22</c> differs
        /// in those same two and in 54 further single bytes, every one of them <c>'6' -> '8'</c> - the
        /// content revision in the eighteen definition-table rows and the thirty-six per-seat instance
        /// rows. Nothing else moved anywhere: the eleven showcase payloads are otherwise byte-identical,
        /// which is this corpus's own proof that verified content never reaches a showcase scene, and
        /// the two new stop-cause facts add nothing at all, because they are written only inside a
        /// pending frame that has already resolved a push and no fixture holds one.
        /// Milestone 8.9 keeps schema v22 and re-pins all twelve again, for a registration cause and a
        /// content cause. The registration cause is two new capabilities -
        /// <c>wasp-pin-enemy-units-in-radius</c> and <c>wasp-close-barrier-in-radius</c>, the Active
        /// effects Wasp's two basic cards create - and the decision is reject rather than migrate on the
        /// same ground as every registration cause before it: a payload written without them was written
        /// by a build in which Magnetic Dagger's After-the-attack text and Static Barrier's Skill did
        /// nothing, so restoring it into this build would silently re-interpret a match played under
        /// different cards. No reader change is needed - <c>ValidateCapabilityRegistrations</c> already
        /// refuses a list that is not sequence-equal to this build's - and no persisted state outside
        /// this corpus exists to strand. The content cause is Wasp's revision advancing from
        /// <c>wasp-8-8-rev-1</c> to <c>wasp-8-9-rev-1</c>.
        /// The delta was read at the byte level in a throwaway worktree at pre-change revision
        /// <c>e4b121d</c>, and it is exactly three things and no fourth. Every one of the twelve
        /// authoritative payloads grows by exactly 68 bytes - the two registration entries, 35 and 33
        /// with their length prefixes and revisions - and changes exactly one byte in place, the
        /// capability count, <c>0x20 -> 0x22</c>. <c>verified-hand-setup-v22</c> changes 54 further
        /// single bytes, every one of them <c>'8' -> '9'</c>, which is the content revision in the
        /// eighteen definition-table rows and the thirty-six per-seat instance rows; the eleven showcase
        /// payloads are otherwise byte-identical, which is this corpus's own proof once more that
        /// verified content never reaches a showcase scene. The third is outside the authoritative
        /// payload: the rule-event log gains eight <c>Turn.BasicActionPerformed</c> entries in total -
        /// two in <c>end-of-round-v22</c>, four in <c>paused-level-up-v22</c>, two in
        /// <c>pending-minion-battle-v22</c> - and loses none, which is p.9's supertype being recorded
        /// wherever those fixtures Hold with a basic card. No other fixture's log moves at all.
        ///
        /// Milestone 8.10 moves schema v22 to v23, renames every fixture with it, and adds a
        /// thirteenth. The schema moves for two sections a v22 payload cannot describe: a card-text
        /// continuation - the paused effect frame, which of rulebook p.14's moments it stopped at, and
        /// the Attack whose remaining steps are owed - and, on the Attack resolution itself, the target
        /// a repeated instance may not take. v22 is rejected rather than migrated on this corpus's
        /// standing ground: the only reading available for either section's absence is "nothing is
        /// paused" and "this instance may take anyone", both true of every payload a v22 build could
        /// write and both false of the first payload this milestone writes.
        /// The added fixture is <c>paused-card-text-v23</c>, and it is added for the reason
        /// <c>paused-orbit-repeat-v22</c> was: the card-text continuation is written only while p.14 is
        /// stopped on a question, no fixture above holds one, and a section with no fixture holding it
        /// is protected vacuously.
        /// The delta was read at the byte level in a throwaway worktree at pre-change revision
        /// <c>381dc5b</c>, and it is exactly two things and no third. Every one of the twelve renamed
        /// authoritative payloads grows by exactly 126 bytes and by nothing else - four new capability
        /// registration entries, <c>synthetic-radius-hero-target</c>, <c>synthetic-defeat-hero</c>,
        /// <c>synthetic-attack-took-a-hero</c> and <c>synthetic-discard-chosen-card</c> revision 2, at
        /// 33, 26, 33 and 34 bytes with their length prefixes and revisions - plus the capability count
        /// in place. <c>verified-hand-setup-v23</c> grows by 180, which is those same 126 and 54 more:
        /// Wasp's content revision advancing from <c>wasp-8-9-rev-1</c> to <c>wasp-8-10-rev-1</c> is one
        /// character longer, in each of the 54 occurrences this corpus counted at 8.9. <b>The two new
        /// sections themselves add nothing to any of the twelve</b>, because none of them holds a paused
        /// card text or a repeated Attack - which is the measurement saying, in bytes, why the
        /// thirteenth fixture had to exist.
        ///
        /// Milestone 8.11 moves schema v23 to v24, renames every fixture with it, and adds a
        /// fourteenth. The schema moves for one field a v23 payload cannot describe: on an Attack
        /// resolution, the verdict of rulebook p.14's Block condition - whether the Defense card's
        /// condition was met, was not met, or was never asked. v23 is rejected rather than migrated on
        /// the same standing ground: the only reading available for the field's absence is "no Block
        /// condition was ever evaluated", true of every payload a v23 build could write and false of
        /// the first payload this milestone writes.
        /// The added fixture is <c>paused-block-rider-v24</c>, and it is added for the reason
        /// <c>paused-card-text-v23</c> was: the Block verdict is only interesting while a Block's own
        /// rider is stopped on a question, no fixture above holds one, and a section with no fixture
        /// holding it is protected vacuously. It earned its place immediately - it is what caught a
        /// Conditional root declaring an output its predicate did not publish, on this milestone's
        /// rider and on 8.10's repeat root, where no other fixture looked.
        /// The delta was read at the byte level in a throwaway worktree at pre-change revision
        /// <c>6ed3144f</c>, and it is exactly three things and no fourth. Every one of the thirteen
        /// renamed authoritative payloads grows by exactly 63 bytes - two new capability registration
        /// entries, <c>synthetic-attack-is-ranged</c> and <c>synthetic-range-hero-target</c>, at 31 and
        /// 32 bytes with their length prefixes and revisions - plus the capability count 0x26 -> 0x28
        /// in place. <c>paused-card-text-v24</c> grows by 67 rather than 63, and the extra four bytes
        /// are the new field itself: it is the only renamed fixture holding an Attack resolution, so it
        /// is the only one that writes a Block verdict. Wasp's content revision advancing from
        /// <c>wasp-8-10-rev-1</c> to <c>wasp-8-11-rev-1</c> is the same width and moves nothing, which
        /// is why <c>verified-hand-setup-v24</c> grows by 63 like the rest rather than by more.
        /// </summary>
        // Milestone 8.13 migrate-in-place decision: v24 already records the registered capability
        // contract set. High Voltage adds three general declarations and one triggered definition but
        // no state field or continuation discriminator, so the schema remains v24 and every supported
        // fixture is re-pinned to that expanded trusted-content registration.
        //
        // Milestone 8.19a reject decision, recorded before the corpus moved: v24 is refused rather
        // than migrated, and the pre-consequence target space is the cause that settles it. The other
        // three fields this milestone adds have a reading available for their absence that is true of
        // every v24 payload - no build could produce a pre-action frame, place work either side of an
        // action, or spend a before-action trigger - but every v24 Attack had a target standing
        // somewhere and simply did not record where. Recovering it would mean reading the piece off
        // the restored board, and the whole reason the field exists is that rulebook p.14 may already
        // have taken that piece off. Every fixture is renamed with the version and re-pinned; the
        // fifteenth is new and is the only one holding a pre-action continuation.
        //
        // Milestone 8.19a1 reject decision: v25 has no current-round hero-Defeat section. Defaulting
        // that absence to empty would make a post-Defeat save fail Brogan's round-history condition;
        // cumulative Life and coin state cannot recover the round scope, and diagnostic events are not
        // rules authority. Every fixture moves to v26 and gains the count-prefixed section. The
        // existing ended-match fixture contains a real p.13 Defeat and therefore pins a non-empty arm;
        // the other fourteen pin the ordinary empty arm. Measured at the authoritative-payload layer,
        // those fourteen grow by exactly four bytes for the zero count. Ended-match grows by 77 bytes:
        // four for count one, one plus 57 UTF-8 bytes for its outcome ID, and one plus 14 UTF-8 bytes
        // for blue-test-hero. The schema int changes 25 -> 26 in place; every other byte is unchanged.
        // Milestone 8.19c re-baselines all fifteen without a schema change. The payload pins the
        // registered capability contract set, and this milestone registers five more, so every fixture
        // grows by exactly 187 bytes: 162 UTF-8 bytes of capability id, five one-byte length prefixes,
        // and five four-byte versions. The arithmetic is checkable against the two figures the previous
        // milestone recorded - `ended-match` 7449 -> 7636 and `verified-hand-setup` 6065 -> 6252 - and
        // it is uniform because the registration list is the only thing that moved. The decision is
        // migrate in place: the schema stays v26, no field is added, and every fixture still round-trips
        // byte-identically and restores to the same state.
        //
        // Milestone 8.19c1 reject decision: v26 has nowhere to put a stopped decision. Three of them
        // arrive at once - a trigger standing inside the effect that pushed, a Defeat waiting at its
        // removal stage, and rulebook p.8's moment before the Minion Battle already having run - and
        // none has a reading available for its absence. A v26 payload restored into a match waiting for
        // one of those answers would resume the trigger as the effect that pushed, remove a miniature
        // its controller had been offered the chance to keep, or fire its pre-battle cards a second
        // time. Every fixture is renamed to v27 and re-pinned. The delta on each is three things: the
        // two new capability registrations `synthetic-shield-friendly-minions` and
        // `synthetic-constrict-before-minion-battle`, at 33 and 40 UTF-8 bytes plus a one-byte length
        // prefix and a four-byte version each; the schema int 26 -> 27 in place; and the absence
        // markers for the new optional sections - one byte per card-action block for each of the two
        // card-action pauses, and one byte in the round block for the pre-battle visit. The arithmetic
        // is checkable against the two figures the previous milestone recorded: `verified-hand-setup`
        // 6252 -> 6336 is 83 + 1, a setup with no turn and therefore no card-action block, and
        // `ended-match` 7636 -> 7722 is 83 + 1 + 2, the same setup plus one turn's block.
        //
        // Milestone 8.20 reject decision: v27 has no field for the hero an Active-effect occurrence
        // bound with the word "this". Arien's Expert and Master Duelist print "immune to attack
        // actions of all enemy heroes, except this attacker", and which hero that is was decided at
        // rulebook p.14's step 4 by an Attack that has ended by the time anything asks - so it cannot
        // be re-derived. Reading the absent field as "bound nobody" is true of a definition that binds
        // nobody and false of a live Master Duelist, which would restore as immunity against the
        // attacker too: a stronger card than the printed one, and one no later state could detect.
        // Every fixture is renamed to v28 and re-pinned, and the measured delta is exactly +276 bytes
        // on all fifteen. It is uniform because the only thing that moved is the registration list a
        // payload pins: ten capabilities join it - `choose-target`, `behind-target-line` and
        // `spaces-adjacent` at 13, 18 and 15 UTF-8 bytes, `synthetic-place-in-range` and
        // `synthetic-range-minion-target` at 24 and 29, and Arien's five Active effects at 34, 20, 21,
        // 30 and 22 - which is 226 id bytes plus ten one-byte length prefixes plus ten four-byte
        // versions. The schema int changes 27 -> 28 in place, and the new bound-actor field costs one
        // absence byte per live Active-effect instance, of which these fixtures hold none. The
        // arithmetic is checkable against the two figures the previous milestone recorded:
        // `verified-hand-setup` 6336 -> 6612 and `ended-match` 7722 -> 7998.
        //
        // The staged configuration also stopped being a mirror at this milestone - the Blue seat deals
        // Arien's printed set - and that is deliberately absent from the delta: these fixtures compose
        // from the showcase and mirror configurations by name, not from the default one, so none of
        // them holds a card identity that moved.
        //
        // Milestone 8.21 reject decision, and it is the first one that is about the pinned list itself
        // rather than about a field. Brogan's printed faces reach eleven contracts this build had
        // registered nowhere, so every v28 payload names a strictly shorter registration list than an
        // 8.21 build has - and a restore compares that list for equality, because it is the identity a
        // payload is read back against. Accepting the shorter list would let a restored match resolve
        // against contracts its own capture never had, which is exactly the silent reinterpretation
        // `RISK-COMPAT-001` exists to prevent. Every fixture is renamed to v29 and re-pinned, and the
        // measured delta is exactly +329 bytes on all fifteen. It is uniform because the registration
        // list is again the only thing that moved: twelve capabilities join it - milestone 8.19b's
        // `attacked-target-space`, `choose-charge-line`, `hero-defeated-this-round`, `retrieve-card`
        // and `gain-resource` at 21, 18, 24, 13 and 13 UTF-8 bytes, this milestone's own
        // `discard-owned-card`, `owned-card-present` and `synthetic-range-unit-target` at 18, 18 and
        // 27, and Brogan's four Active effects at 25, 30, 31 and 31 - which is 269 id bytes plus
        // twelve one-byte length prefixes plus twelve four-byte versions. The schema int changes
        // 28 -> 29 in place and costs nothing, and no new field is added at all. The arithmetic is
        // checkable against the two figures the previous milestone recorded: `verified-hand-setup`
        // 6612 -> 6941 and `ended-match` 7998 -> 8327.
        //
        // Brogan's own staged configurations are deliberately absent from the delta for the reason
        // Arien's were: these fixtures compose from the showcase and mirror configurations by name.
        //
        // Milestone 8.22 reject decision, and it is the same cause in the same place - which is now the
        // expected shape of a hero milestone rather than a surprise. Sabina's printed faces reach five
        // contracts this build had registered nowhere, so every v29 payload names a strictly shorter
        // registration list than an 8.22 build has, and the same equality comparison refuses it on
        // sight. The measured delta for that move is exactly +117 bytes on all fifteen. It is uniform
        // for the same reason: five capabilities join the list - milestone 8.19b's
        // `choose-target-set`, `choose-target-pair`, `presence-count` and `pre-consequence-adjacency`
        // at 17, 18, 14 and 25 UTF-8 bytes, and this milestone's own `target-is-adjacent` at 18 -
        // which is 92 id bytes plus five one-byte length prefixes plus five four-byte versions. The
        // schema int changes in place and costs nothing, and no new field was added at all. The
        // arithmetic is checkable against the two figures the previous milestone recorded:
        // `verified-hand-setup` 6941 -> 7058 and `ended-match` 8327 -> 8444.
        //
        // Sabina's own staged configurations are absent from the delta for the reason Brogan's and
        // Arien's are: these fixtures compose from the showcase and mirror configurations by name.
        //
        // Milestone 8.22 then moves the version a second time within one milestone, to v31, and this
        // one is a field rather than the list. Finding `F-ATK-02` let a Push inside rulebook p.14's
        // card text set off an after-push trigger, which stops the page part-way through; what is
        // owed while that trigger stands - the attack, the moment, and the outcome the remainder
        // reports - is written outside the continuation discriminator, beside the Lane-Push request.
        // A v30 payload cannot describe a page stopped that way at all, because that altitude could
        // not set a trigger off, so accepting one would resume an Attack whose remaining steps nothing
        // owed. Every fixture is renamed to v31 and re-pinned, and the measured delta is **+1 byte on
        // fourteen of the fifteen and none on the fifteenth** - the one place in this corpus where a
        // schema move is not uniform, and the reason is worth stating: the marker is a boolean written
        // beside the other card-action pauses, and `WriteCardAction` writes nothing at all for a
        // payload holding no card-action authority. `verified-hand-setup` is that payload, so it stays
        // 7058 bytes while `ended-match` goes 8444 -> 8445. A uniform figure here would have meant the
        // marker was being written where there is no continuation to hold it.
        //
        // Milestone 8.23 moves it to v32, and it is a field again in the same place: the completed
        // basic action a permanent after-action ability may still be asked to repeat. Tigerclaw's
        // Cloak and Daggers opens its decision at p.8's ActionEnded checkpoint, after the action it is
        // about has finished, so the subject has to cross the pause on the wire. A v31 payload cannot
        // describe it - absent would read as "no ability is standing", which is true of every payload
        // written before this milestone and false of one captured while the question was up, and
        // confirming a restored question with no subject would do nothing at all.
        //
        // The measured delta is **+1 byte on thirteen of the fifteen and none on the other two**, and
        // it is the second non-uniform move for exactly the reason the first was: the subject is
        // written as a boolean beside the other card-action pauses, and `WriteCardAction` returns at
        // `NoCardActionAuthorityKind` before reaching it. `setup` and `verified-hand-setup` are the two
        // payloads with no card-action authority, so both stay at 8067 and 7058 bytes while
        // `ended-match` goes 8445 -> 8446. Every digest still moves, including those two, because the
        // schema int itself is part of the payload.
        //
        // Milestone 8.23 moves it to v33, and this one has two causes at once - the first time a
        // single version has. The pinned lists grow by eight capability ids and two marker
        // definitions: `choose-through-target-line`, `adjacent-terrain-presence` and
        // `transfer-resource`, which milestone 8.19b built and Tigerclaw's faces are the first printed
        // content to name; `actor-moved-this-action` and `synthetic-place-in-radius`, which this
        // milestone minted; his Active effect and his two marker gives; and `poison-1` and `poison-2`,
        // the first hero-owned rules component this build registers. And a field joins the Attack
        // resolution: where the attacker stood when the Attack started, which Combat Reflexes asks
        // about at p.14's step 7.
        //
        // The measured delta separates them exactly. **+270 bytes on thirteen fixtures and +279 on
        // two** - the two that hold a paused Attack on the wire, `paused-card-text` and
        // `paused-block-rider`, where the extra nine are the field's boolean and its coordinate. Every
        // other fixture carries the list growth alone, including the two with no card-action authority
        // at all, because a registration list is not a continuation. The arithmetic is checkable: the
        // eight ids cost 244 bytes of id, length prefix and revision, the two marker definitions 26,
        // and 244 + 26 = 270. `ended-match` goes 8446 -> 8716 and `paused-card-text` 7946 -> 8225.
        //
        // v32 is rejected rather than migrated on both grounds at once, and either would be enough:
        // accepting a shorter list would let a restored match resolve against contracts its own capture
        // never had, and absence of the actor space would report a paused Combat Reflexes as not having
        // moved whether or not he had.
        //
        // Milestone 8.24 moves it to v34, and this one is the pinned list ALONE - the third time that
        // list has moved the version, and the first move since 8.21 with no field beside it. Xargatha's
        // five persistent texts are five capability contracts no v33 payload names: her three
        // petrification disks (`xargatha-stone-gaze`, `xargatha-petrifying-stare`,
        // `xargatha-turn-into-statues`) and the two end-of-round sources her Green Skills leave
        // standing (`xargatha-constrict`, `xargatha-final-embrace`).
        //
        // The measured delta is **+135 bytes on all fifteen**, and it is uniform for the reason 8.22's
        // list move was: a registration list is not a continuation, so the two payloads holding no
        // card-action authority carry it exactly like the thirteen that do. The arithmetic is
        // checkable against the figures the previous entry recorded: the five ids are 19, 25, 26, 18
        // and 22 UTF-8 bytes, which is 110, plus five one-byte length prefixes and five four-byte
        // revisions - 110 + 5 + 20 = 135. `ended-match` goes 8716 -> 8851, `verified-hand-setup`
        // 7328 -> 7463, and `paused-card-text` 8225 -> 8360.
        //
        // NOTHING ABOUT HER SET NEEDED A FIELD, which is the part worth recording rather than the
        // bytes. TECHNICAL_RISKS told itself at 8.22 to expect each remaining roster hero to lengthen
        // this list and cost a version; what it could not say was whether any of them would also stop
        // the game somewhere the wire had no room for. She does not. Her Ultimate is a standing
        // declaration rather than a stopped decision, and the class and target narrowings her Green
        // line and her two repeats print are re-read from trusted definitions when the question is
        // asked rather than carried. So the predicted delta is the whole delta, and v33 is rejected
        // rather than migrated on the single standing ground: accepting a shorter list would let a
        // restored match resolve a card against contracts its own capture never had.
        //
        // Milestone 8.25e moves THREE digests and NO version, and the measurement is what says why that
        // is right: every one of the three authoritative payloads is byte-identical on both sides -
        // `end-of-round` 8933, `pending-minion-battle` 9138 and `paused-level-up` 9443 bytes, with
        // `cmp` reporting zero differing bytes in each - so no state was added, removed or reshaped,
        // and the other twelve fixtures do not move at all. What moved is the rule-event log the
        // envelope carries beside the payload, and only there: two swapped entries per Hold, four in
        // `end-of-round`, four in `pending-minion-battle` and eight in `paused-level-up`, each one a
        // Hold now raising `Turn.BasicActionPerformed` BEFORE `Turn.CardResolved` instead of after it.
        // Those three are exactly the fixtures built out of Holds.
        //
        // The order is the correction rather than a side effect. p.9's supertype fact is recorded at
        // the checkpoint that ends the action, and milestone 8.25a had already put that checkpoint
        // ahead of the card's resolution for the Attack, Skill and Movement tails, because an ability
        // that may repeat the action is asked there and needs an unresolved card and a selected actor
        // to run its second instance on. p.11's Fast Travel, p.12's Hold and p.13's Clear kept the old
        // order and so asked over a turn that had already finished. Migration is not a question here:
        // no payload changes, so every v34 snapshot in the wild still loads byte-identically, and the
        // digest is re-pinned rather than the version moved.
        // Milestone 8.25e7 keeps schema v34 and moves Wasp's content identity to cover High Voltage's
        // later reachable triggered definition. A v34 payload naming wasp-8-11-rev-1 is rejected on
        // the existing content-revision mismatch path rather than rebound to semantics its identity
        // did not cover. Twelve existing fixtures stay byte-identical. The three carrying verified
        // Wasp content move by the measured revision-string occurrences: verified-hand-setup
        // authoritative 7463 -> 7571 and envelope 10064 -> 10208; paused-card-text 8360 -> 8474 and
        // 13889 -> 14041; paused-block-rider 8639 -> 8755 and 14214 -> 14370. The added repeated-Attack
        // witness is 8329 authoritative bytes and 17374 decoded envelope bytes at the new identity.
        //
        // MILESTONE 8.35z KEEPS SCHEMA v36 AND RE-PINS TWO DIGESTS. THE DECISION IS ACCEPT-UNCHANGED -
        // A COMPATIBLE WIDENING - AND IT IS MEASURED RATHER THAN ASSERTED.
        //
        // What changed: CardActionResolutionEvent.FromAttackResolution now passes the resolution's own
        // TargetMinionId beside the TargetHeroId it always passed, so RuleEventLogProjection writes a
        // `minion=` reference for an attack whose defender is a minion. What that costs the corpus, in
        // full: fourteen of the sixteen fixtures do not move at all, and the two that do move ONLY in
        // the rule-event log the envelope carries beside the payload - paused-card-text envelope
        // 14041 -> 14083 and paused-level-up 26364 -> 26427, authoritative 8475 and 9448 bytes
        // UNCHANGED on both. Every one of the sixteen decoded authoritative payloads is byte-identical
        // across the change, `cmp` reporting zero differing bytes in each, so no state was added,
        // removed or reshaped anywhere and the version has nothing to move for.
        //
        // The log delta is five rows, all of them rows that already existed, each gaining exactly the
        // twenty-one characters `;minion=blue-melee-01` and nothing else: two rows in paused-card-text
        // (2 x 21 = 42, which is the whole envelope delta) and three in paused-level-up (3 x 21 = 63,
        // likewise the whole delta). No row is added, removed, reordered or re-worded. The arithmetic
        // closing exactly is what proves the delta is the fragment and not something else riding along.
        //
        // Does the added reference change the MEANING of an old payload? No, and this is the half that
        // decides migrate-or-reject rather than the byte counts. `minion=` is a REFERENCE, not a fact:
        // it names the board object the resolution had already targeted, and that targeting has always
        // been in the authoritative payload as PendingAttackResolution.Target. So a reader of an old
        // row could never have concluded "this attack had no minion defender" from the absence - the
        // absence was an omission of a reference the writer held, never a claim that there was none.
        // The new rows say more; the old rows say exactly what they always said. That is a widening.
        //
        // And the round-trip was measured directly rather than reasoned about: every one of the sixteen
        // snapshot strings captured at the PRE-change revision was replayed through the POST-change
        // restore path, and all sixteen - both moved fixtures included - were ACCEPTED and re-captured
        // BYTE-IDENTICALLY. The envelope carries its log verbatim through a restore instead of
        // re-deriving it, so a snapshot in the wild neither needs migrating nor deserves rejecting: it
        // keeps its own historically honest rows, recorded when the writer did not name the defender.
        // Nothing is rejected, nothing is migrated, and the digests are re-pinned.
        // MILESTONE 8.35z21 MOVES THE SCHEMA TO v37 AND REJECTS v36. THE DECISION IS REJECT, on the
        // reasoning v25 and v32 used and for the same kind of cause: the absent field has no reading
        // true of both builds.
        //
        // What changed: the card-action block now writes the two repeat instances that have been
        // DECIDED and not yet STARTED - p.14's card-text repeat and p.10's after-action repeat - each
        // beside the unit it may not take. Four fields, and when nothing is owed they cost exactly ten
        // bytes: two absent markers and the two `none` target kinds. Measured directly in this
        // worktree against the pre-change revision: `paused-card-text` authoritative 8,475 -> 8,485 and
        // `paused-level-up` 9,448 -> 9,458 (the two the previous milestone recorded exact figures for),
        // and beside the corpus a coin-earning-round fixture 9,035 -> 9,045. The two fixtures whose
        // turn carries no card-action authority at all - `setup` at 8,472 and `verified-hand-setup` at
        // 7,571 - do not move a byte, because the block early-returns before these fields are reached.
        // No repeat is owed in any of the sixteen, so every one of those ten bytes is an absent marker
        // and no fixture's MEANING changed.
        //
        // Why reject rather than migrate, which is the half the byte counts do not decide: a v36
        // payload's absence would have to be read as "no repeat instance is owed". That reading is
        // true of every v36 capture EXCEPT the one this milestone creates, and it is exactly the one a
        // migration would have to be right about. Before 8.35z21 an owed repeat was always started
        // inside the command that decided it, so a v36 build could not capture between the two; 8.35z21
        // drains rulebook p.8's mid-turn Lane Push at the end of each action instance, and a push
        // stopped on p.8's sidebar Place now stands precisely there. A migrated payload taken at that
        // state would restore an action that quietly loses p.10's second instance and finishes as
        // though the card were done. The absence is not an omission of a reference the reader can
        // recover, the way 8.35z's `minion=` was; it is a claim about the rules, and it would be false.
        // Milestone 8.35z27, v37 -> v38. REJECT rather than migrate.
        //
        // What changed, and it is two things rather than one - which the byte counts separate cleanly.
        // (1) The registered-capability list gained `attack-sequence@1`, the leaf a modal Attack's
        // "choose one, or both" publishes each chosen bullet's printed targeting sentence through. That
        // costs +20 bytes in EVERY fixture, including the two whose turn carries no card-action
        // authority at all: `setup` 8,472 -> 8,492 and `verified-hand-setup` 7,571 -> 7,591, both
        // exactly +20 and not one byte more. (2) The card-action block now writes the modal Attack a
        // "choose one, or both" opened and has not finished. When none is running that is a single
        // absent marker, so every fixture that HAS a card-action block moves +21 rather than +20:
        // `paused-card-text` 8,485 -> 8,506 and `paused-level-up` 9,458 -> 9,479, the two the previous
        // milestone recorded exact figures for. No modal Attack runs in any of the sixteen, so all of
        // those bytes are the absent marker and no fixture's MEANING changed.
        //
        // Why reject rather than migrate, which the byte counts do not decide: a v37 payload's absence
        // would have to be read as "no modal Attack is part-way through its plan". That reading is true
        // of every v37 capture EXCEPT the one this milestone creates, and it is precisely the one a
        // migration would have to be right about. Ruling 5 makes both sequences of a choose-both Attack
        // ONE action, so a capture taken between them - or inside the second one's Defense window, or
        // while p.14's minion arm holds a Defeat at its removal stage - is an action that is half done.
        // A migrated payload would resume it as a finished one, silently dropping the sequence the
        // rules still owe and a whole p.14 with it. Nor can the absence be recovered from the board the
        // way milestone 8.35z's `minion=` was: a target that successfully DEFENDED is still standing
        // and looks exactly like a unit no sequence has taken, so nothing on the board distinguishes
        // "already attacked once" from "untouched". The absence is a claim about the rules, and it
        // would be false.
        // Milestone 8.35z45, v38 -> v40. REJECT rather than migrate, and the byte counts
        // are unusually eloquent here because they moved by NOTHING.
        //
        // WHAT CHANGED IN THE FORMAT. Each bullet of a persisted modal Attack plan gained a fifth enum:
        // the predicate that bullet prints about its candidate, which is the field Middlefinger of
        // Death's "a hero in range WHO HAS one or more cards in the discard" needed and which the
        // multi-sequence contract had named as its one open extension point.
        //
        // WHAT IT COST THIS CORPUS: zero bytes. Not "a small number" - zero. The previous milestone
        // recorded that "no modal Attack runs in any of the sixteen", so every fixture here writes the
        // chain's single absent marker and never reaches the per-bullet loop the field lives in. Every
        // authoritative payload below is therefore byte-identical in LENGTH to its v38 predecessor, and
        // the ONLY reason all sixteen digests move is the four bytes of the version field itself
        // changing value from 38 to 40. That was measured before the version was touched, by running
        // this corpus with the field added and the version left alone: sixteen unchanged digests.
        //
        // RE-MEASURED AT INTEGRATION, and the reason is a merge hazard rather than a behaviour
        // change. This milestone's branch was cut before milestone 8.35z38 dealt Misa the Samurai,
        // so the sixteen digests it measured were taken with FOURTEEN fewer contracts in the
        // registered list every snapshot records; landing them unmodified would have pinned a
        // pre-Misa number on a post-Misa tree, and the complete run caught exactly that. The
        // zero-cost measurement above was then REPEATED on the merged tree - field in place,
        // version temporarily returned to 38 - and all sixteen of these fixtures plus all nine
        // aggregate digests reproduced their v38 values exactly. So the claim holds on the tree
        // that ships: the whole of the movement below is the version integer.
        //
        // WHY THE VERSION MOVED ANYWAY, since this corpus did not force it. A v38 payload that DOES
        // carry a chain is producible by a shipped v38 build - Whisper's Grim Reaper composes four
        // attack-sequence bullets today - and such a payload has one fewer integer per bullet. A v40
        // reader walking it would take the NEXT bullet's id string out of the middle of this bullet's
        // policy and carry on. That is corruption rather than a short read, and turning it into a
        // refusal is the entire job of the version guard. A corpus that happens to hold no example of
        // the shape is not evidence the shape is unreachable; it is evidence about this corpus.
        //
        // WHY REJECT RATHER THAN MIGRATE, which the byte counts do not decide. A v38 payload's missing
        // field would have to be read as "this bullet prints no predicate about its candidate". That is
        // true of every v38 capture EXCEPT one taken mid-chain on a card that prints one - precisely
        // the capture a migration would have to be right about, and precisely the capture this
        // milestone makes possible. A migrated pause inside Middlefinger's second sequence would resume
        // offering every enemy hero in range, the empty-discard ones included, and the player would
        // spend a Tier III card on a target the card never admitted. Nor is the absence recoverable
        // from the board the way milestone 8.35z's `minion=` was: the pile is live state and says
        // nothing about which printed sentence the paused sequence derived its candidates from.
        //
        // WHY 40 AND NOT 39: v39 is spoken for by Misa's deferred EffectInterruptKind claimant, an
        // unrelated payload shape with its own migrate-or-reject decision still to make. Taking the
        // next FREE number rather than the next number leaves that claim intact, and costs nothing -
        // the guard is an equality test, so an unused 39 is not a hole a reader can fall into.
        //
        // MILESTONE 8.35z34 RE-PINNED ALL SIXTEEN, and the explicit decision that block asked for was
        // MIGRATE rather than reject - with the reason stated, because "the digests moved" is not one.
        //
        // The schema did NOT move then: every fixture name read `-v38`, the writer's version
        // field is unchanged, and no field was added, removed or reordered. What moved is the CONTENT
        // of a field the format has always had - the registered capability list, which every snapshot
        // records and which grew by six the moment Dodger the Warlock was dealt and her contracts
        // registered. A payload written before this milestone still parses under exactly the same
        // reader, because the list is read out of the payload rather than compared against the build's.
        //
        // That is the same event every registration milestone from 8.20 to 8.24 caused, and it is the
        // one this project deliberately makes loud: registration is not a refactor, so the numbers
        // move and somebody has to say why. The half worth checking, and checked: the byte counts move
        // by a handful per fixture and in one direction, which is a longer capability list and nothing
        // else. A structural change would not be uniform.
        //
        // MILESTONE 8.35z38 RE-PINNED ALL SIXTEEN AGAIN, and the decision is MIGRATE for the same
        // reason - stated here with the measurement that turns 8.35z34's reasoning into evidence.
        //
        // The schema did NOT move: every fixture name below still reads `-v38`, the writer's version
        // field is unchanged, and no field was added, removed or reordered. What moved is the
        // registered capability list, which grew by FOURTEEN contracts the moment Misa the Samurai was
        // dealt: six roster, two placement and her six Active-effect creations.
        //
        // TWO MEASUREMENTS MAKE THAT A FINDING RATHER THAN A CLAIM, and both were taken rather than
        // reasoned. First, reverting the three registration files alone - `SyntheticRosterCapabilities`,
        // `SyntheticPlacementCapabilities` and `MatchContentCatalogs` - and leaving every other edit of
        // this milestone in place restored all sixteen digests below to their 8.35z34 values EXACTLY,
        // so registration is the sole cause and dealing her is not independently moving a byte.
        // Second, the growth is UNIFORM: every one of the sixteen authoritative payloads grew by
        // exactly 458 bytes (setup 8,799 to 9,257; verified-hand-setup 7,898 to 8,356; parked-pre-action
        // 9,984 to 10,442; and so on for all sixteen). One identical constant across sixteen unrelated
        // captures is a longer list appended to a field they all carry. A structural change would vary
        // with what each fixture contains, and a behavioural one would not touch the fixtures that
        // never reach the new contracts at all - eleven of these sixteen never so much as name Misa.
        // MILESTONE 8.35z46 RE-PINNED ALL SIXTEEN AGAIN, on the same terms 8.35z38 and 8.35z45
        // recorded. The schema did NOT move: every fixture name below still reads `-v40`, the writer's
        // version field is unchanged, and no field was added, removed or reordered. What moved is the
        // registered capability list, which grew by exactly ONE contract the moment Silverarrow the
        // Pathfinder was dealt: `fast-travel@1`, her one new handler TYPE.
        // MILESTONE 8.35z50 RE-PINNED ALL SIXTEEN AGAIN, and the MIGRATE-OR-REJECT DECISION IS MIGRATE,
        // recorded here rather than left implicit. The schema did NOT move: every fixture name below
        // still reads `-v40`, the writer's version field is unchanged, and no field was added, removed
        // or reordered. What moved is the registered capability list, which the moment Ursafar the
        // Savage was dealt grew by THREE handler types - `hero-condition@1`, `borrow-primary-action@1`
        // and `attack-target-survived@1` - plus the fifteen Active-effect creation contracts his cards
        // declare, all of which every snapshot records as part of the registered set it pins. That is
        // registration rather than behaviour, which is what makes migrating correct: the same three
        // aggregate invariants this corpus's sibling fixture asserts survive the move intact, and the
        // authoritative byte counts move by the same small constant across all sixteen captures -
        // eleven of which never so much as name Ursafar - which is the signature of a longer list
        // appended to a field they all carry rather than of a structural or behavioural change.
        // MILESTONE 8.35z53 RE-PINNED ALL SIXTEEN AGAIN, AND THE DECISION IS MIGRATE, recorded here
        // rather than left implicit. The schema did NOT move: every fixture name below still reads
        // `-v40`, the writer's version field is unchanged, and no field was added, removed or
        // reordered. What moved is the registered capability list, which the moment Whisper the
        // Outcast was dealt grew by FOUR handler types - `choose-target-trio@1`,
        // `defended-card-forced-move@1`, `enemy-hero-with-discard-present@1` and
        // `choose-swap-subject@1` - plus `synthetic-radius-unit-target@1`, which is a seventh
        // declaration over a handler this build already registers rather than a new type, plus the one
        // Active-effect creation Death Seeker declares. That is registration rather than behaviour,
        // which is what makes migrating correct on exactly the terms the two paragraphs above state:
        // every snapshot records the registered set it was written against, so a longer list moves all
        // sixteen captures - including the eleven that never so much as name Whisper - while the three
        // aggregate invariants this corpus's sibling fixture asserts survive the move intact.
        // 8.36f3: schema 41 rejects v40, which cannot retain accumulated repeat exclusions or a
        // borrowing Skill suspended around the borrowed primary it started. The final schema-41
        // layout writes the borrowed-parent count beside every card-action continuation. These
        // corpus rows hold no borrowed parent, so the second addition is an empty four-byte count;
        // the focused Angry Roar restore journey is the non-default witness. Log tails are unchanged.
        //
        // MILESTONE 8.37: MIGRATE, on exactly the terms Whisper's own paragraph above states, and for
        // the last time a dealing will ask it. THE SCHEMA DID NOT MOVE - every fixture name below
        // still reads `-v41`, the writer's version field is unchanged, and no field was added,
        // removed or reordered. What moved is the registered capability list, which the moment Tali
        // the Spiritcaller was dealt grew by EIGHT handler types: her six generic contracts
        // (`synthetic-source-card-in-discard`, `friendly-hero-in-play`, `name-color`,
        // `synthetic-choose-direction`, `synthetic-batch-direct-move`, `defended-card-color`),
        // and the two her token line needs (`PlaceHeroTokenHandler`, `ReclaimHeroTokenHandler`),
        // whose showcase instances register beside her own six token capability ids. Her three TOKEN
        // DEFINITIONS moved with them, which no earlier dealing has done - she is the first verified
        // hero whose content reaches the component catalog at all. That is registration rather than
        // behaviour, so migrating is correct: every snapshot records the registered set it was
        // written against, and a longer list moves all sixteen captures - the eleven that never name
        // her included - while the aggregate invariants the sibling fixture asserts survive intact.
        //
        // The wire moved to `guards-private-online/13` in the same milestone and is NOT why these
        // digests moved: nothing a wire frame carries reaches a snapshot payload, and the value
        // vocabulary that version carries is re-derived on restore by re-asking the handler rather
        // than persisted at all.
        //
        // MILESTONE 8.37a: MIGRATE, and the smallest move this decision has ever recorded. THE SCHEMA
        // DID NOT MOVE - every fixture name below still reads `-v41`, the writer's version field is
        // unchanged, and no field was added, removed or reordered. What moved is the registered
        // capability list, by exactly ONE id: `tali-blizzard-end-of-turn-repeat@1`, the Active effect
        // her composed Blizzard arms. It adds no handler TYPE (`ActiveEffectCreationHandler` has been
        // registered since milestone 6.8) and no component, so the whole of what these sixteen
        // captures record differently is one more entry in the registered set they were written
        // against - registration rather than behaviour, on the identical terms every dealing above
        // states. The eleven captures that never name her move with the rest for the same reason they
        // always have.
        // MILESTONE 8.38e: MIGRATE, on exactly the terms milestone 8.37a's paragraph above states, and
        // for the same kind of cause one hero later. THE SCHEMA DID NOT MOVE - every fixture name below
        // still reads `-v41`, the writer's version field is unchanged, and no field was added, removed
        // or reordered. What moved is the registered capability list, by exactly THREE ids: Swift's two
        // straight-line placements and `subject-adjacent-terrain-presence@1`, all three held out of the
        // registered set by milestone 7.9's gate since 8.38a and 8.38d and moved across by the
        // milestone that DEALS her. Two of the three add no handler TYPE (`PlaceHandler` has been
        // registered since milestone 6.6) and the third adds one; none adds a component. So the whole
        // of what these sixteen captures record differently is three more entries in the registered set
        // they were written against - registration rather than behaviour, on the identical terms every
        // dealing above states. The captures that never name her move with the rest for the reason they
        // always have: the registered set is part of what a snapshot records, not part of what it plays.
        // MILESTONE 8.39i: MIGRATE, on exactly those terms one hero later. THE SCHEMA DID NOT MOVE -
        // every fixture name below still reads `-v42`, the writer's version field is unchanged, and no
        // field was added, removed or reordered; milestone 8.39g is what took this corpus to v42 and
        // renamed it, and this milestone renames nothing. What moved is the registered capability list,
        // by exactly TWO ids: `synthetic-range-obstacle-target@1` (minted 8.39b for Grappling Bolt's
        // obstacle) and `claim-solo-victory@1` (minted 8.39g1 for A Fistful of Coins' ending), both
        // held out of the registered set by milestone 7.9's gate until the milestone that DEALS her.
        // Both add a handler TYPE and neither adds a component or a field.
        //
        // AND THE BYTE DELTA IS ATTRIBUTED RATHER THAN ASSERTED, against the two captures milestone
        // 8.39g wrote its own byte counts down for: `setup` 11,123 -> 11,182 and
        // `verified-hand-setup` 10,222 -> 10,281, both exactly +59. Fifty-nine is what the two ids
        // cost - `synthetic-range-obstacle-target` is 31 characters and `claim-solo-victory` is 18,
        // each with a one-byte length prefix and a four-byte revision, so 31+1+4 plus 18+1+4 - and the
        // two captures agreeing on it while carrying different content is what says the growth is the
        // recorded registration list rather than anything a capture plays. The other fourteen have no
        // earlier byte count written down to subtract from, which is stated rather than glossed.
        //
        // MILESTONE 8.40j: MIGRATE, on exactly those terms one hero later. THE SCHEMA DID NOT MOVE -
        // every fixture name below still reads `-v42`, the writer's version field is unchanged, and
        // no field was added, removed or reordered. Two lists did. The registered CAPABILITY list
        // grew by THIRTEEN ids: Bain's four marker operations (`bain-give-bounty-marker@1`,
        // `bain-transfer-bounty-marker@1`, `bain-return-bounty-marker@1`,
        // `bain-bounty-marker-held@1`), his five Active-effect creations
        // (`bain-vantage-point-ignore-obstacles@1`, `bain-high-ground-ignore-obstacles@1`,
        // `bain-vantage-point-movement@1`, `bain-high-ground-movement@1`,
        // `bain-another-one-end-of-turn-repeat@1`), milestone 8.40f's two hidden-information
        // contracts (`synthetic-hidden-card-choice@1`, `synthetic-reveal-chosen-card@1`) and the two
        // showcase instances milestone 7.9's gate asks of a new handler TYPE
        // (`synthetic-transfer-marker@1`, `synthetic-marker-held@1`). And the registered COMPONENT
        // list grew by ONE marker definition, `bounty` - the second in this build after Tigerclaw's
        // Poison, and the first component a dealing has added since Tali's three tokens.
        //
        // AND THE BYTE DELTA IS ATTRIBUTED RATHER THAN ASSERTED, against the two captures whose byte
        // counts are written down above: `setup` 11,182 -> 11,613 and `verified-hand-setup`
        // 10,281 -> 10,712, both exactly +431. Four hundred and thirty-one is what those fourteen
        // entries cost and nothing else: the thirteen capability ids are 355 characters between them,
        // each with a one-byte length prefix and a four-byte revision (355 + 13 + 52 = 420), and the
        // marker definition id `bounty` is six more with the same prefix and revision (6 + 1 + 4 =
        // 11). The two captures agreeing on it while carrying different content is what says the
        // growth is the recorded registration lists rather than anything a capture plays.
        // 8.41e: migrate the corpus registration fingerprints; schema v42 and wire /15 stay.
        // Four new capability entries cost 30 + 41 + 35 + 26 = 132 bytes. Measured setup
        // 11613 -> 11745 and verified-hand-setup 10712 -> 10844 both grow by exactly that.
        // No field, component, existing definition meaning, or previous hero identity changes.
        // This migrates test captures, not saved files: the unchanged strict registration reader
        // rejects pre-Garrus captures with EffectCapabilityRevisionMismatch; no silent load migration.
        // 8.43e: regenerate test fingerprints, reject earlier saved registration sets, no migration.
        // Schema v42 and wire /15 stay unchanged. Eleven capabilities plus widget-pyro@1 cost
        // 319 bytes: setup 11745 -> 12064, verified-hand-setup 10844 -> 11163. No prior hero
        // content identity changed. The existing strict catalog comparison rejects old captures.
        //
        // MILESTONE 8.46b: MIGRATE, and the decision is recorded here rather than in the commit
        // alone because this list exists to force it. A choice admitting exactly one selection is
        // now answered in ONE accepted command instead of two, so a payload captured after such an
        // answer carries one fewer command in the retained prefix it holds. FOUR of the sixteen
        // move for that reason and no other - placed-token, attached-marker, pending-correction and
        // paused-orbit-repeat, the four whose fixtures drive an effect choice to its end. The other
        // twelve are byte-identical, which is what says this is the answer's own command count
        // moving rather than the format: the schema version does not move, no field is added or
        // removed, and every one of the sixteen still restores and re-captures byte-identically
        // below. Reject was considered and refused: there is nothing to reject, because no payload
        // this build wrote before the change has become unreadable - the old captures were never
        // stored, they are rebuilt here from the fixtures every run.
        //
        // MILESTONE 8.53: REJECT, and this is a schema move rather than a re-pin. The round section
        // gains the match scoreboard - one count-prefixed hero id and six Int32 counters per
        // participating hero, written immediately after the round's leveling record. Every fixture
        // is renamed from `-v42` to `-v43` and re-pinned.
        //
        // AND THE BYTE DELTA IS ATTRIBUTED RATHER THAN ASSERTED. It is **+82 bytes on all sixteen**,
        // measured against the two captures whose byte counts this list already writes down: `setup`
        // 12,064 -> 12,146 and `verified-hand-setup` 11,163 -> 11,245. Eighty-two is exactly what the
        // section costs on a two-hero match and nothing else: a four-byte list count, plus per hero a
        // one-byte length prefix and the id itself - `primitive-hero` and `blue-test-hero` are
        // fourteen characters each, so 30 for the two with their prefixes - plus six four-byte
        // counters each, which is 24 per hero and 48 for the pair: 4 + 30 + 48 = 82. The delta being
        // identical on sixteen captures carrying different content is what says the growth is this
        // dense per-hero section rather than anything a capture plays - every one of them is a 1v1
        // with the same two hero ids, and every one of them therefore pays exactly the same 82.
        //
        // v42 is REJECTED rather than migrated, and the reason is the one this series keeps making.
        // A v42 payload records no scoreboard at all, and the only reading available for its absence
        // is "nothing has happened to anybody yet" - all six columns zero for every hero. That
        // reading is right for every payload a v42 BUILD could produce, because no v42 rule counts
        // any of it, and that is precisely why it must not be written into the reader: the first
        // payload it would be wrong about is the first one this milestone writes. A match restored
        // mid-play from a migrated v42 payload would come back with every hero at zero kills and
        // zero coins earned and keep counting from there, and the victory screen - the whole point
        // of the number - would print a board quietly wrong in a way nothing in the match could
        // detect. It is a HISTORY rather than a resource, which makes the case stronger than v20's
        // for the Life counters or v17's for the Level: those two are values a later rule moves, and
        // this one cannot be recovered from any live state, because the board says what happened and
        // nothing else in the payload remembers. The coins column is not recoverable from the coin
        // balances the payload already carries either, which is worth saying because they are right
        // there: a balance is what a hero HOLDS after p.15 has spent it back down, and the column is
        // what they COLLECTED. No persisted state exists outside this corpus to strand.
        //
        // The wire moved to `guards-private-online/17` in the same milestone, for the same board.
        // That is only the second time the two have moved together - 8.39g's solo ending was the
        // first - and for the same underlying reason: this is state the build accumulates rather
        // than a reading it re-derives, so it has to be both persisted and carried.
        //
        // MILESTONE 8.57g: MIGRATE, and it is a re-pin rather than a schema move. Dealing Rowenna the
        // Vanguard registers exactly two capabilities -
        // `synthetic-place-in-range-beside-a-named-hero@1` (milestone 8.57b) and
        // `synthetic-place-in-radius-beside-a-named-hero@1` (milestone 8.57d) - and every snapshot
        // records the registration list, so all sixteen captures grow and re-hash while no field, no
        // reader, no `MatchLogEntryKind` and no behaviour moves. SCHEMA STAYS v43 AND THE WIRE STAYS
        // `/17`.
        //
        // MILESTONE 8.61e: NEITHER. The wire moved to `guards-private-online/18` for a thirty-third
        // `MatchLogEntryKind` - a hero moved through a token that goes off, and the removal revealed
        // it - and the schema did NOT move with it, nor did these digests. The row is re-derived from
        // the retained rule-event log like every other one; the pause the trap opens is an ordinary
        // `PendingEffectInterrupt`, whose kind the card-action payload has written as an int since
        // milestone 8.19c1, so a fourth reason a frame can be standing costs no version; and nothing
        // Min the Dragonmonk owns is registered until her dealing, so no capture's registration list
        // grew a character.
        //
        // THE CAUSE WAS ISOLATED BEFORE A PIN WAS TOUCHED - milestone 8.38d's rule, and a dealing
        // changes far too much for "the registration did it" to be assumed. With
        // `SyntheticPlacementCapabilities` reverted to its pre-registration blob (`49c758a4`) and
        // every other change of the milestone in place - the selector row, the hand-switch arm, four
        // staged configurations, nine `SceneCardContent` members, eighteen artwork rows, fifteen
        // decision labels and the four Presentation overlay readers - all sixteen of these and all
        // nine of `CardAggregateInvarianceTests`' were GREEN on their previous pins. Restoring the
        // file byte-identically (`b22232a6`) moved exactly the same twenty-five again.
        //
        // AND THE BYTE DELTA IS ATTRIBUTED RATHER THAN ASSERTED. It is **+99 bytes on all sixteen**,
        // measured against the two captures whose byte counts this list writes down: `setup`
        // 12,146 -> 12,245 and `verified-hand-setup` 11,245 -> 11,344. Ninety-nine is exactly what the
        // two ids cost and nothing else: `synthetic-place-in-range-beside-a-named-hero` is 44
        // characters and `synthetic-place-in-radius-beside-a-named-hero` is 45, which is 89, plus two
        // one-byte length prefixes and two four-byte revision integers - 89 + 2 + 8 = 99. Both
        // captures agreeing on it while carrying completely different content is what says the growth
        // is the recorded registration list rather than anything a capture plays.
        //
        // Migrate rather than reject, on this list's own standing reasoning: an earlier payload is
        // refused by the existing strict catalog comparison rather than silently reinterpreted, and
        // nothing this build wrote has become unreadable - these captures are rebuilt from the
        // fixtures every run and were never stored.
        private static readonly CompatibilityFixture[] SupportedFixtures =
        {
            new CompatibilityFixture("setup-v43", "602abafbc8be1afd1dd6a64d1ae374db37925f36e3bb32797b78514ae6b6a78c"),
            new CompatibilityFixture("active-turn-v43", "039d3df5ac9c28a95b16e0c5b2f889eba0867ca3ed450c523f906f56ae842440"),
            new CompatibilityFixture("pending-choice-v43", "704e8009e92371b9f95ec11a65f84c1b28b59480c2df1642b8c470d7139e37b4"),
            new CompatibilityFixture("end-of-round-v43", "c02285ec42ac51cd9cbd0777fae95914eec5a8e7ca416f0433064fa395bf6302"),
            new CompatibilityFixture("pending-minion-battle-v43", "2217fb01f189960fc205c7854aa860b2d20309305887abaa5d0a3e0ef2d54ea5"),
            new CompatibilityFixture("placed-token-v43", "ec70554605bc5aa3d9747453b93ef571969602e76521278aec8572e279aaa86b"),
            new CompatibilityFixture("attached-marker-v43", "f54a9823e35b7ba090de2ba7a8e5e5c669ff96331da54ac89f1684576458b2c5"),
            new CompatibilityFixture("pending-correction-v43", "0e589d9c8c583d8de9442c7a65031b0e1e91209593a28035a5787467922564eb"),
            new CompatibilityFixture("paused-level-up-v43", "159057e94d07e5f0e1fdd7851e34ab669cae4b500bf6d7aa78afbfbed45578c5"),
            new CompatibilityFixture("ended-match-v43", "3f9bced71d7a812ac0de91f1421832dd8fda4507ac92cdb6e8aa0aa891fff8f7"),
            new CompatibilityFixture("verified-hand-setup-v43", "4803320fbf76448e61b1c233bc0b7b45ee08046164d9775f3d7d651a4b1dc11a"),
            new CompatibilityFixture("paused-orbit-repeat-v43", "0a34b19a350efcf6f9d5d9e03d353bf12bb38156cc63e4915e9ab6bdc4b1b63c"),
            new CompatibilityFixture("paused-card-text-v43", "a2f0f05ac9f2fea2b390b9d0d2ef495cb1f06b52a88b8abded789f0559efa1de"),
            new CompatibilityFixture("paused-block-rider-v43", "8f3521a04ab1c03feafc27dba06360bac7798323253748bd043c66671a5bb8a6"),
            new CompatibilityFixture("paused-repeated-attack-v43", "1154a605f051fb8dc5e7892a3516d92a6e991d369f57a33c551e4c51dedaa66f"),
            new CompatibilityFixture("parked-pre-action-v43", "909e95dd1932cce5a1cc1b179c7a8883818dd0f6b0449e5a153e5503fd34940a")
        };

        [Test]
        public void SupportedVersionedCorpus_RoundTripsByteIdentically()
        {
            var snapshots = CreateSupportedSnapshots();
            Assert.That(snapshots.Keys, Is.EquivalentTo(SupportedFixtures.Select(value => value.Name)));

            var actualHashes = SupportedFixtures
                .Select(fixture => Sha256(snapshots[fixture.Name]))
                .ToArray();
            var actualMeasurements = SupportedFixtures
                .Select(fixture =>
                    $"{fixture.Name}={Sha256(snapshots[fixture.Name])} " +
                    $"(authoritative {AuthoritativeByteLength(snapshots[fixture.Name])} bytes; " +
                    $"envelope {Convert.FromBase64String(snapshots[fixture.Name]).Length} decoded bytes)")
                .ToArray();
            Assert.That(
                actualHashes,
                Is.EqualTo(SupportedFixtures.Select(fixture => fixture.Sha256).ToArray()),
                $"Compatibility corpus changed. Actual measurements: " +
                $"{string.Join(", ", actualMeasurements)}. " +
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

        /// <summary>
        /// Milestone 8.25e7. Schema v34 already writes the target excluded by a repeated Attack, but
        /// the compatibility corpus previously wrote only its default value. One real Thunder
        /// Boomerang repeat now owns that witness, and both supported restore layers must preserve the
        /// same repeated context, excluded hero, legal remaining minion, and accepted continuation.
        /// </summary>
        [Test]
        public void RepeatedAttackExcludedTargetHasANonDefaultCorpusWitness()
        {
            var witnesses = CreateSupportedSnapshots()
                .Select(entry =>
                {
                    var target = CreateRestoreTarget(entry.Key);
                    Assert.That(
                        RuleEventLogSnapshotEnvelope.TryDeserialize(
                            entry.Value, out var envelope),
                        Is.True,
                        entry.Key);
                    var restored = AuthoritativeSnapshotSerializer.Deserialize(
                        MatchContentCatalogs.ForThisBuild(),
                        envelope.AuthoritativePayload,
                        target.Projection.Definitions,
                        new[] { target.Projection.SelectedMap },
                        target.Projection.Setup.Minions);
                    return new RepeatedAttackWitness(entry.Key, entry.Value, envelope, restored);
                })
                .Where(value =>
                    value.Restored.CardActions.PendingAttackResolution != null &&
                    value.Restored.CardActions.PendingAttackResolution.InvocationContext ==
                        ActionInvocationContext.Repeated &&
                    value.Restored.CardActions.PendingAttackResolution.ExcludedTarget.IsDefined)
                .ToArray();

            Assert.That(
                witnesses.Select(value => value.Name),
                Is.EqualTo(new[] { "paused-repeated-attack-v43" }),
                "The corpus continuation inventory names the one real non-default repeated-Attack " +
                "exclusion exactly once.");

            var witness = witnesses.Single();
            var rawResolution = witness.Restored.CardActions.PendingAttackResolution;
            var rawChoice = witness.Restored.CardActions.PendingAttackTargetChoice;
            Assert.That(rawResolution.InvocationContext, Is.EqualTo(ActionInvocationContext.Repeated));
            Assert.That(rawResolution.ExcludedTarget.Kind, Is.EqualTo(AttackTargetKind.Hero));
            Assert.That(rawChoice.LegalTargetHeroIds,
                Has.No.Member(rawResolution.ExcludedTarget.HeroId));
            Assert.That(rawChoice.LegalTargetMinionIds, Is.Not.Empty,
                "Thunder Boomerang's other real target remains legal on the second Attack.");
            var remainingMinion = rawChoice.LegalTargetMinionIds.Single();
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(
                    witness.Restored.Round, witness.Restored.Board),
                Is.EqualTo(witness.Envelope.AuthoritativePayload),
                "The inner authoritative wire form round-trips byte-identically.");

            var rawContinuation = witness.Restored.Round.TryAnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    rawChoice.ResolutionId, rawChoice.ChoiceId, remainingMinion));
            Assert.That(rawContinuation.Accepted, Is.True,
                "The raw authoritative restore accepts the remaining legal target.");

            var envelopeRestore = CreateRestoreTarget(witness.Name);
            var envelopeResult = envelopeRestore.RestoreSnapshot(witness.Snapshot);
            Assert.That(envelopeResult.Accepted, Is.True, envelopeResult.Detail);
            Assert.That(envelopeRestore.CaptureSnapshot(), Is.EqualTo(witness.Snapshot),
                "The outer rule-event-log envelope round-trips byte-identically.");
            var outerChoice = envelopeRestore.Projection.PendingAttackTargetChoice;
            Assert.That(outerChoice.LegalTargetHeroIds,
                Has.No.Member(rawResolution.ExcludedTarget.HeroId));
            Assert.That(outerChoice.LegalTargetMinionIds, Is.EqualTo(new[] { remainingMinion }));
            var outerContinuation = envelopeRestore.Execute(
                DirectorCommand.ChooseAttackTarget(
                    outerChoice.ResolutionId, outerChoice.ChoiceId, remainingMinion));
            Assert.That(outerContinuation.Accepted, Is.True,
                "The envelope restore accepts the same continuation.");
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
                    MatchContentCatalogs.ForThisBuild(),
                    futureCompletedMatchPayload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(schemaException.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion));

            var priorSchemaPayload = RewriteSchemaVersion(
                envelope.AuthoritativePayload,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion - 1);
            var priorSchemaException = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
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
                    MatchContentCatalogs.ForThisBuild(),
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
            values.Add("setup-v43", session.CaptureSnapshot());

            CommitUnequalCards(session);
            values.Add("active-turn-v43", session.CaptureSnapshot());

            var movement = session.Projection.AvailableTurnActions.First(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            values.Add("pending-choice-v43", session.CaptureSnapshot());

            session = CreateSession();
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);

                // Milestone 8.35g. A finished resolution waits for its own player's release, so
                // the loop presses it exactly as a player would before the next hero can act.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Assert.That(session.Execute(DirectorCommand.ReleaseTurn()).Accepted, Is.True);
                    continue;
                }

                Assert.That(session.Execute(
                    DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }

            values.Add("end-of-round-v43", session.CaptureSnapshot());

            session = CreateSessionWithMinionImbalance();
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var minionChoice = session.Projection.PendingMinionBattleChoice;
            Assert.That(minionChoice, Is.Not.Null);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                minionChoice.SelectInvocations[0])).Accepted, Is.True);
            values.Add("pending-minion-battle-v43", session.CaptureSnapshot());

            // Milestone 6.2's two new wire sections. A version-controlled corpus whose fixtures all
            // carry zero components would leave the sections it exists to protect untested, so one
            // payload holds a token on the board and one holds a marker on a hero.
            values.Add("placed-token-v43", CaptureComponentSnapshot("component-place-branch"));
            values.Add("attached-marker-v43", CaptureComponentSnapshot("component-give-marker"));

            // Milestone 6.4a's wire block: every persisted pending family keeps one corpus fixture,
            // so the paused after-action correction gets the eighth.
            values.Add("pending-correction-v43", CapturePendingCorrectionSnapshot());

            // Milestone 7.9. The complete synthetic loop reaches two state shapes the eight fixtures
            // above never do, and both are shapes a payload has to survive rather than merely reach:
            // a match paused inside p.15's Upgrade a Card, which carries a hero's Level, coins, items
            // and a system-transition continuation at once, and a match that has ended, which carries
            // the derived outcome and refuses every later command. They are captured from short
            // scripted sequences rather than from a seeded complete match on purpose - a corpus digest
            // is a pin, and pinning one to a generated run would re-pin the corpus on every change to
            // anything the generator touches, which is the opposite of what a compatibility corpus is
            // for.
            values.Add("paused-level-up-v43", CapturePausedLevelUpSnapshot());
            values.Add("ended-match-v43", CaptureEndedMatchSnapshot());

            // Milestone 8.4. The configured session as a player is dealt it: two mirrored Wasps holding
            // p.5's starting five with thirteen in each deck, which is the first verified content any
            // payload in this corpus has ever carried.
            values.Add(
                "verified-hand-setup-v43",
                ConfiguredMatchFixtures.CreateVerifiedMirrorSession().CaptureSnapshot());

            // Milestone 8.6's own schema move. The repeat continuation is written inside a pending Skill
            // frame and inside an owed repeat, and no fixture above holds either, so the section this
            // milestone added would have had no version-controlled coverage at all without this one.
            values.Add("paused-orbit-repeat-v43", CapturePausedOrbitRepeatSnapshot());

            // Milestone 8.10's own schema move. The card-text continuation is written only while
            // rulebook p.14 is stopped on a question inside an Attack, and no fixture above holds one,
            // so the section this milestone added would have had no version-controlled coverage at all
            // without this one.
            values.Add("paused-card-text-v43", CapturePausedCardTextSnapshot());

            // Milestone 8.11's own schema move. The Block result is written on an Attack resolution
            // that is still pending, and the one state a Block leaves one pending in is its own
            // rider's question - so no fixture above holds a payload with the field set to anything
            // but None, and a section only ever written as its zero value is protected vacuously.
            values.Add("paused-block-rider-v43", CapturePausedBlockRiderSnapshot());

            // Milestone 8.25e7. The repeated-Attack wire section predates this milestone, but every
            // corpus fixture left ExcludedTarget at its default. Thunder Boomerang is the verified
            // source whose real second Attack both excludes the first hero and leaves a minion legal.
            values.Add("paused-repeated-attack-v43", CapturePausedRepeatedAttackSnapshot());

            // Milestone 8.19a's own schema move, and the one fixture that holds both halves of it at
            // once: a frame standing outside an action, and the action it is standing outside of
            // running with a continuation of its own. No fixture above holds either, so the section
            // this milestone added would have had no version-controlled coverage at all without it.
            values.Add("parked-pre-action-v43", CaptureParkedPreActionSnapshot());
            return values;
        }

        /// <summary>
        /// A match whose actor has placed a card's printed text after the Movement it is printed on, so
        /// the Movement is mid-route and the text's work is parked behind it.
        ///
        /// It is the only payload in this corpus carrying a pre-action continuation at all, and the
        /// only one carrying two continuations at once - which is exactly why milestone 8.19a writes
        /// the pre-action block beside the card-action discriminator rather than as one of its kinds.
        /// </summary>
        private static string CaptureParkedPreActionSnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateContinuationSession();
            var red = session.Projection.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Value == "placeholder-red-charmer" &&
                card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards
                .Where(card =>
                    card.State.Location == CardLocation.Hand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue)
                .OrderBy(card => session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);

            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            Assert.That(
                session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted,
                Is.True);

            // The second branch of the ordering question is "after the action". Milestone 8.46b: the
            // ordering admits exactly one branch, so naming it is the whole answer and the
            // completion is asked for rather than assumed.
            var ordering = session.Projection.PendingEffectChoice;
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session, ordering.SelectInvocations[1]);
            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null,
                "The Movement has to be running, or this fixture pins an ordinary paused choice.");
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// A match stopped inside rulebook p.14's step 4, on the question Reflect Projectiles' rider
        /// asks after its Block succeeded. It is the only payload in this corpus whose Attack
        /// resolution carries a Block result at all.
        /// </summary>
        private static string CapturePausedBlockRiderSnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            var attacker = HandCardOf(session, TeamSide.Red, WaspCardContent.ThunderBoomerang);
            var defenderCommit = HandCardOf(session, TeamSide.Blue, WaspCardContent.Shock);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(attacker.OwnerId, attacker.Id)).Accepted,
                Is.True);
            Assert.That(
                session.Execute(
                    DirectorCommand.CommitCard(defenderCommit.OwnerId, defenderCommit.Id)).Accepted,
                Is.True);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Attack &&
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            Assert.That(
                session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted,
                Is.True);

            var defenderHero = session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side ==
                        TeamSide.Blue)
                .HeroId;
            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, defenderHero)).Accepted, Is.True);

            var block = session.Projection.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                offer.SourceDefinitionReference.DefinitionId.Equals(
                    WaspCardContent.ReflectProjectiles));
            Assert.That(
                session.Execute(DirectorCommand.ChooseDefenseCard(block.SourceCardInstanceId)).Accepted,
                Is.True);
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The rider has to be holding the page open, or this fixture pins an ordinary defence.");
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// A real Thunder Boomerang stopped on its repeated Attack target question. The first target
        /// is a hero; the repeat therefore carries that non-default exclusion while retaining the
        /// fixture's one other legal target, a minion.
        /// </summary>
        private static string CapturePausedRepeatedAttackSnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out var minionId);
            var red = HandCardOf(session, TeamSide.Red, WaspCardContent.ThunderBoomerang);
            var blue = HandCardOf(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted,
                Is.True);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted,
                Is.True);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Attack &&
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            Assert.That(
                session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted,
                Is.True);

            var blueHero = session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side ==
                        TeamSide.Blue)
                .HeroId;
            var firstTarget = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                firstTarget.ResolutionId, firstTarget.ChoiceId, blueHero)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.DeclineDefense()).Accepted, Is.True);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var repeatedTarget = session.Projection.PendingAttackTargetChoice;
            Assert.That(repeatedTarget, Is.Not.Null);
            Assert.That(repeatedTarget.LegalTargetHeroIds, Has.No.Member(blueHero));
            Assert.That(repeatedTarget.LegalTargetMinionIds, Is.EqualTo(new[] { minionId }));
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// A match stopped inside rulebook p.14's step 7, on the first of the two questions Shock's
        /// rider asks. It is the only state in which the card-text continuation reaches the wire: the
        /// paused frame, the moment the page stopped at, and the attack whose remaining steps are owed.
        /// </summary>
        private static string CapturePausedCardTextSnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var red = HandCardOf(session, TeamSide.Red, WaspCardContent.Shock);
            var blue = HandCardOf(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(
                session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                FindActionKind(session.Projection, offer) == ActionKind.Attack &&
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            Assert.That(
                session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted,
                Is.True);

            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, minionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The rider has to be holding the page open, or this fixture pins an ordinary attack.");
            return session.CaptureSnapshot();
        }

        private static CardInstance HandCardOf(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        /// <summary>
        /// A match stopped inside the <i>second</i> instance of a bounded repeat, which is the only
        /// shape that exercises everything milestone 8.6 put on the wire at once: a positive repeat
        /// count, and the target that instance inherited from the first.
        ///
        /// The sequence is the card's own sentence. The actor picks a subject inside the printed
        /// Radius, moves it one space around themselves, and confirms the repeat; the second instance
        /// then does not ask which subject - `on the same target` binds it - so it pauses on the
        /// destination, which is exactly where a payload carrying an inherited target has to be
        /// legible.
        /// </summary>
        private static string CapturePausedOrbitRepeatSnapshot()
        {
            var session = CreateOrbitSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("orbit") &&
                    candidate.State.Location == CardLocation.Hand);
                Assert.That(session.Execute(
                    DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-orbit-skill")));
            Assert.That(session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted,
                Is.True);

            // The subject, then the space it orbits to, then p.10's confirm-or-decline for the
            // repeat. Milestone 8.46b: each of the first two admits exactly one, so naming it is the
            // whole answer and the completion is asked for rather than assumed; the third offers
            // nothing to name and is taken on its own Confirm exactly as before.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            AnswerEffect(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingSkillResolution, Is.Not.Null,
                "The captured payload has to hold a paused frame, or it pins no continuation at all.");
            Assert.That(
                session.Projection.PendingSkillResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Repeated),
                "The pause has to be inside the repeated instance, which is what carries the count.");
            return session.CaptureSnapshot();
        }

        private static MatchSession CreateOrbitSession() => ConfiguredMatchFixtures.CreateOrbitSession();

        /// <summary>
        /// A configured match stopped exactly where p.15's Upgrade a Card asks its question, which is
        /// the state milestone 7.5 built and milestone 7.8a drew.
        /// </summary>
        private static string CapturePausedLevelUpSnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Not.Null,
                "The fixture must be paused, or this pins an ordinary end of round twice.");
            return session.CaptureSnapshot();
        }

        /// <summary>
        /// A configured match that p.13's last Life counter has ended: the Red hero's Attack defeats
        /// the adjacent Blue one on the first turn, and the team it belonged to had one counter.
        /// </summary>
        private static string CaptureEndedMatchSnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedSession(1);
            var red = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blue = session.Projection.Cards.Single(card => card.Id.Value == "blue-hammer-card");
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "Red Pursuit's Initiative 7 acts before Blue Hammer's 6.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable && FindActionKind(session.Projection, offer) == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.DeclineDefense());

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(session.Projection.Outcome.HasEnded, Is.True);
            return session.CaptureSnapshot();
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

            // Milestone 8.46b: "Choose one" admits exactly one bullet, so taking the push is the
            // whole answer and the completion is asked for rather than assumed.
            var push = session.Projection.PendingEffectChoice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Value == "displacement-object-push");
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, push);

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
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
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
            // Milestone 8.46b: each of these two questions admits exactly one answer, so naming it
            // closes it - the completions are asked for rather than assumed, and the payload this
            // builds is the same committed component state it always was.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, option);

            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);

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
        private static MatchSession CreateRestoreTarget(string fixtureName)
        {
            if (fixtureName.StartsWith("verified-hand-setup", StringComparison.Ordinal))
            {
                return ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            }

            if (fixtureName.StartsWith("paused-orbit-repeat", StringComparison.Ordinal))
            {
                return CreateOrbitSession();
            }

            // Milestone 8.10. Restored into the board the payload was captured from, for the reason the
            // two above are: a restore target holding different content is refused before the section
            // under test is ever read.
            if (fixtureName.StartsWith("paused-card-text", StringComparison.Ordinal))
            {
                return ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out _);
            }

            if (fixtureName.StartsWith("paused-block-rider", StringComparison.Ordinal))
            {
                return ConfiguredMatchFixtures.CreateBlockRiderSession(
                    WaspCardContent.ReflectProjectiles);
            }

            if (fixtureName.StartsWith("paused-repeated-attack", StringComparison.Ordinal))
            {
                return ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            }

            if (fixtureName.StartsWith("parked-pre-action", StringComparison.Ordinal))
            {
                return ConfiguredMatchFixtures.CreateContinuationSession();
            }

            return fixtureName.StartsWith("placed-token", StringComparison.Ordinal) ||
                fixtureName.StartsWith("attached-marker", StringComparison.Ordinal) ||
                fixtureName.StartsWith("pending-correction", StringComparison.Ordinal)
                    ? CreateComponentSession()
                    : CreateSession();
        }

        private static MatchSession CreateComponentSession() =>
            CreateFixtureSession(MatchConfiguration.CreateForSkillFixture(TeamSide.Blue));

        private sealed class RepeatedAttackWitness
        {
            public RepeatedAttackWitness(
                string name,
                string snapshot,
                RuleEventLogSnapshotEnvelope envelope,
                RestoredAuthoritativeMatch restored)
            {
                Name = name;
                Snapshot = snapshot;
                Envelope = envelope;
                Restored = restored;
            }

            public string Name { get; }
            public string Snapshot { get; }
            public RuleEventLogSnapshotEnvelope Envelope { get; }
            public RestoredAuthoritativeMatch Restored { get; }
        }

        /// <summary>
        /// One session from one named fixture configuration. Extracted at milestone 8.6, when the
        /// component session's body was needed a second time: the setup, scenario and map are the same
        /// for every fixture configuration, and only which hands are dealt differs.
        /// </summary>
        private static MatchSession CreateFixtureSession(
            MatchConfiguration configuration)
        {
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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

                // Milestone 8.35g. The release the finished turn is waiting for.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Assert.That(session.Execute(DirectorCommand.ReleaseTurn()).Accepted, Is.True);
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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

        private static int AuthoritativeByteLength(string snapshot)
        {
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(
                    snapshot,
                    out var envelope,
                    out var rejection),
                Is.True,
                $"Compatibility fixture envelope was rejected as {rejection}.");
            Assert.That(rejection, Is.EqualTo(RuleEventLogEnvelopeRejectionReason.None));
            return Convert.FromBase64String(envelope.AuthoritativePayload).Length;
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
