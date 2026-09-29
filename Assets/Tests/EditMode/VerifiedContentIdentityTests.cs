using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25e7's test-owned content-identity oracle. It deliberately does not call a
    /// production digest: every configured face is walked from its own public definition graph, and
    /// references to catalog-owned semantics are resolved and walked here as well.
    /// </summary>
    public sealed class VerifiedContentIdentityTests
    {
        // MILESTONE 8.75b MOVED ALL TWENTY-ONE PINS AT ONCE, and it is the SECOND change ever to
        // move every pin together - the first was 8.38d, whose note is below and whose standing
        // lesson this is an instance of, exactly as that note predicted.
        //
        // THE CAUSE. Ignatia the Mad is the first hero whose action cards print TWO texts, a sun (☀)
        // line and a moon (☾) line, with the live Tie Breaker coin face selecting which applies. A
        // surface has to be able to say which half of such a card is live, and the only ways it could
        // work that out for itself - recognising a hero, a card name, a capability id, or the printed
        // words - are the ones AGENTS.md forbids. So the content states it, on the shared
        // `CardDefinition`: `PrintedTextSelection`, whose value is `None` on every card of the
        // twenty-one heroes dealt today. This walker encodes every public property of every object it
        // reaches, so each canonical string gained one segment reading `PrintedTextSelection=...None;`
        // and twenty-one digests moved while twenty-one heroes' printed content did not.
        //
        // THE CAUSE WAS ISOLATED BEFORE THE PINS WERE TOUCHED, by 8.38d's own probe and no inference:
        // `PrintedTextSelection` was temporarily added to the exclusion list in the property walk
        // below, and all twenty-one then measured BYTE-IDENTICAL to the values this change replaces.
        // That is the whole proof that no hero's semantics moved, and it is why the twenty-one
        // revision strings are unchanged - bumping them would assert that twenty-one heroes' content
        // changed, which is false, and would drag `ConfiguredCardArtworkCatalog.json` and the
        // git-ignored shine sprites along with it for nothing.
        //
        // 8.38d'S QUESTION WAS ASKED AND ANSWERED: can the field live on the content that needs it
        // instead? It cannot. The reader is presentation, which meets a card only as the definition
        // its instance names in `IMatchReadModel.Definitions` - so a declaration on anything narrower
        // than `CardDefinition` is a declaration no card surface can reach. It is otherwise as inert
        // as it can be made: no rule branches on it, the coin branch is an ordinary `Conditional` over
        // a predicate capability and resolves identically without it, and no snapshot or wire frame
        // carries a definition at all, so it moves neither the schema nor the protocol.
        //
        // MILESTONE 8.38d MOVED ALL TWELVE PINS AT ONCE, AND IT IS THE FIRST CHANGE THAT EVER HAS.
        // Every move recorded below it is one hero's: a reorder that reached Brogan alone, a phantom
        // declaration dropped from Sabina alone, an icon corrected on Tigerclaw alone. This one
        // reached all twelve because it added a property to the SHARED
        // `UltimateRuleDefinition` - `GrantedTargetExclusion`, Bullet Time's "you cannot target the
        // same enemy hero twice in the same turn this way" - and this walker encodes every public
        // property of every object it reaches. So each canonical string gained one segment reading
        // `GrantedTargetExclusion=...None;`, and twelve digests moved while twelve heroes' printed
        // content did not.
        //
        // THE CAUSE WAS ISOLATED BEFORE THE PINS WERE TOUCHED, rather than inferred from the fact that
        // all twelve moved together: the property was temporarily added to the exclusion list in the
        // property walk below, and all twelve then measured BYTE-IDENTICAL to the values this comment
        // replaces. That is the whole proof that no hero's semantics moved, and it is why the twelve
        // revision strings below are unchanged - bumping them would assert that twelve heroes' content
        // changed, which is false, and would drag `ConfiguredCardArtworkCatalog.json` and the shine
        // sprites along with it for nothing.
        //
        // THE STANDING LESSON, which milestone 8.20 states for capability declarations and this is the
        // first instance of for a content TYPE: a shared declaration gaining a field moves every hero
        // who already referenced it. A later milestone adding a field here owes the same isolation
        // probe, and should first ask whether the field can live on the content that needs it instead.
        // 8.97's full gate measured nine old owners moving after 8.93b appended the optional
        // MarkerDefinition property to RosterValueContributionDefinition. Their existing queries
        // all keep the undefined default and their content revisions stay put; the reflection walk
        // below nevertheless includes the new property on each contribution. The nine digests
        // below are the complete measured set, not new card semantics or an inferred revision bump.
        // Milestone 9.1 moved exactly five, each with a new `-9-1-rev-1` revision, for the user's
        // printed-wording rulings (Rules/PRINTED_WORDING.md): Whisper's Blood Frenzy repeat is
        // stoppable (W2); Rowenna's two Paragons and Mrak's Epicentre repeat automatically (W1); Min's
        // two Grenades and Trinkets' two detonations hit every enemy hero present (W3).
        // Milestone 9.2 moved two revisions to `-9-2-rev-1` for ruling W4 ("defeat a minion" is an
        // enemy minion): Whisper's digest moved with Lesser Evil's and Greater Good's relation; Wuk's
        // digest did not, because Trample's narrowing lives inside his minion-choice handler.
        // It also moved Sabina and Cutter to `-9-2-rev-1`: six selections that wrote their printed
        // Radius as a literal reach now measure the icon, so p.16's Radius items raise them.
        // Milestone 9.6 moved four to `-9-6-rev-1`, the four whose compositions changed so an offer
        // can read what they do: Tali's batch move is the printed "if able" behind a direction that
        // names its consumer; Swift's subject excludes a hero beside terrain; Bain's and Cutter's line
        // targets name their Move (W6), and Cutter reads her own space first. Every other pin is
        // byte-identical, which is the measurement that the engine's new reading changed no content.
        private static readonly IdentityPin[] Pins =
        {
            new IdentityPin("arien", "arien-8-35l-rev-1", "a980d139925f8bfe87fe6b501978697944d0a7473c0d42cf6df70ee85c1758a5"),
            // Milestone 8.35p moved Brogan alone: the three charges ask where he lands, move, and
            // only then ask whom he takes. The other five pins are byte-identical, which is the
            // measurement that says the reorder reached his content and nobody else's.
            new IdentityPin("brogan", "brogan-8-35p-rev-1", "11c81f6230b8211054147bf5d43809272b96bfcde9ddbe0135fbe00eec7b11bd"),
            // The phantom-declaration removal moved Sabina alone: Troop Movement, Marching Orders and
            // Path to Victory each dropped a RepeatTargetBinding.DifferentTarget that no printed face
            // supports and that no code path could enforce on a Skill. The other seven pins are
            // byte-identical, which is the measurement that says the removal reached her content and
            // nobody else's - and the digest moving at all is what says a declaration is part of the
            // content's identity even when it changes no behaviour.
            // MILESTONE 8.83 moved Sabina alone among the three Green command faces' owners: Troop
            // Movement, Marching Orders and Path to Victory bound their "to a space in radius" leash
            // as the literal 2 / 3 / 3 while the SELECTION above it read her printed icon, so a
            // Radius item widened who she could command and not where she could put them. The leash
            // is 8.82's printed member now and declares no number at all.
            new IdentityPin("sabina", "sabina-9-2-rev-1", "1fb63a966c17a743744c6cf749a21d28a922205c6d6da9c81e1c325267518478"),
            // Milestone 8.35z20 corrected Blend Into Shadows' secondary icon (Defense 2, not Movement
            // 2 - the one-icon slot-position trap; see Docs/AI/Rules/HERO_TIGERCLAW.md's
            // settled-corrections block), which is the whole reason this digest moved.
            // MILESTONE 8.83 moved Tigerclaw for one word on one face: Master Thief ends "you may move
            // UP TO 2 spaces" and its follow-up step declared `may` at an exact 2, which is p.11's
            // OTHER wording. Light-Fingered's and Pick Pocket's "you may move 1 space" are unchanged
            // and still declare `may`, which is what makes this one card's correction rather than a
            // family's.
            new IdentityPin("tigerclaw", "tigerclaw-8-83-rev-1", "4abc85e826bbd9d2300869d48fa418cc771811716c7262c6bd171462d143e023"),
            new IdentityPin("wasp", "wasp-8-25e7-rev-1", "f0eb74ab6120918b6d70337331efab0ec7cba75fce476bdb2ff3b702921294db"),
            // MILESTONE 8.83 moved Xargatha for the same clause on three faces: Charm, Control and
            // Dominate print "you may move an enemy minion in radius UP TO 2 spaces" and their move
            // declared `Mandatory` at 2, so the minion could only ever be put in the second ring.
            // Her Siren's Call is untouched - its literal 1 is a number the sentence writes down.
            new IdentityPin("xargatha", "xargatha-8-83-rev-1", "7c89cabcbadbe5903b1fff92b07685e43cc9d8822c08ca3b42188a8711390db8"),
            // Milestone 8.35z34. THE SEVENTH PIN, and the first added since this oracle was built -
            // every one above it was pinned at milestone 8.25e7 and has only ever moved. It arrives
            // now rather than at the milestone that composed her because a pin is a claim about
            // content a configured match loads, and 8.35z28 composed her without dealing her.
            //
            // MEASURED, not predicted. The digest below was read off this oracle's own failure output
            // after the pin was added with a deliberately wrong value, on a COMPLETE unfiltered run -
            // never off a filtered pin run, on the near-miss milestone 8.35z32 recorded.
            // MOVED at milestone 8.35z45, and the SHAPE of the move is the evidence the milestone
            // exists to produce. Middlefinger of Death stopped being a deferral and became a composed
            // modal Attack, so Dodger's content revision moved and her digest with it. Every OTHER pin
            // did NOT move by a single byte on the same unfiltered run - which is the measured form of
            // this milestone's central design decision: the per-candidate predicate went on
            // AttackSequenceBullet, a type no card definition graph reaches, rather than on
            // PrintedAttackTargetPolicy, which every Attack action in the roster declares and which
            // could not have been widened without re-identifying every dealt hero.
            //
            // The worker measured this against seven pins, before milestone 8.35z38 dealt Misa; the
            // eighth pin below was present for the complete run that landed it, and did not move
            // either. Dodger's revision string carried a placeholder until integration; renaming it to
            // the landed id re-pins that string and re-measures NOTHING, because the digest is
            // computed over the content and not over the revision - measured at milestone 8.35z44 by
            // renaming Sabina's and watching her digest stay byte-identical while a deliberately
            // corrupted digest was still caught.
            new IdentityPin("dodger", "dodger-8-35z45-rev-1", "fa0482237b599de001dec721f1e093302b6f74160c05328eb1be3ed7613b7c3b"),
            // Milestone 8.35z38. THE EIGHTH PIN, arriving on exactly the terms the seventh did: a pin
            // is a claim about content a configured match loads, and milestone 8.35z29 composed Misa
            // without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output. A
            // filtered pin run has been green while digests moved, which is why the rule is the
            // complete run rather than the cheap one.
            new IdentityPin("misa", "misa-8-36f-rev-1", "47b245d43af40b1aaed0b93628eb64708c101db1dfe527177913acef06b8955c"),
            // Milestone 8.35z46. THE NINTH PIN, arriving on exactly the terms the seventh and eighth
            // did: a pin is a claim about content a configured match loads, and milestone 8.35z30
            // composed Silverarrow without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output.
            //
            // MOVED at milestone 8.59, on the same complete unfiltered run that moved Swift's below
            // and no other pin by a byte. Wild Hunt's "you may move 2 spaces in a straight line" bound
            // its offer a ceiling and no floor, so the free move lit the spaces ONE away too; the
            // floor is now bound on `wild-hunt-line`, which is a literal added to one node of one
            // face and is the whole of this digest's move.
            new IdentityPin("silverarrow", "silverarrow-8-66-rev-1", "d24c50ebfa541fdedc874630937ab607dfba5552865603dea638777ec1a32289"),
            // Milestone 8.35z50. THE TENTH PIN, arriving on exactly the terms the seventh through
            // ninth did: a pin is a claim about content a configured match loads, and milestone
            // 8.35z31 composed Ursafar without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the nine pins above confirmed byte-identical in that same run. A filtered pin run has
            // been green while digests moved, which is why the rule is the complete run rather than
            // the cheap one.
            new IdentityPin("ursafar", "ursafar-8-35z50-rev-1", "ed7d8ec18d75054be9c61aa7a90079963a499f1558231ba33c1a9c34030d2328"),
            // Milestone 8.35z53. THE ELEVENTH PIN, arriving on exactly the terms the seventh through
            // tenth did: a pin is a claim about content a configured match loads, and milestone
            // 8.35z33 composed Whisper without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the ten pins above confirmed byte-identical in that same run. Milestone 8.35z50's own
            // trap was watched for and did not recur: a pin added WITHOUT its staged configuration
            // measures the hero as contributing ZERO cards, and hers was added in the same commit that
            // made her pickable, so the digest below is over her whole dealt set.
            new IdentityPin("whisper", "whisper-9-2-rev-1", "9dfb89cf73055d69a98c80c58d8744b027a415b3b0e4666b67b1d65814fabc7f"),
            // Milestone 8.37. THE TWELFTH AND LAST PIN of the package arc, arriving on exactly the
            // terms the seventh through eleventh did: a pin is a claim about content a configured
            // match loads, and milestone 8.35z32 composed Tali's Green line without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the eleven pins above confirmed byte-identical in that same run. Milestone 8.35z50's
            // trap was watched for and did not recur: a pin added WITHOUT its staged configuration
            // measures the hero as contributing ZERO cards, and hers went in beside the row in
            // EveryDealtDefinition below, in one change.
            new IdentityPin("tali", "tali-9-6-rev-1", "3f1bb083d93addb88ffe3c5aefb1f4bc0c95defc4e6458b61d77a85e39426490"),
            // Milestone 8.38e. THE THIRTEENTH PIN, and the first added after the package arc closed.
            // It arrives on exactly the terms the seventh through twelfth did: a pin is a claim about
            // content a configured match loads, and milestone 8.38d composed Swift without dealing
            // her. MEASURED, not predicted, on a COMPLETE unfiltered run, with the twelve pins above
            // confirmed byte-identical in that same run - which for this milestone means identical to
            // the values IT re-pinned, because 8.38d moved all twelve for a reason recorded at the top
            // of this list. Milestone 8.35z50's trap was watched for and did not recur: her staged
            // configuration went into EveryDealtDefinition in this same change, so the digest below is
            // over her whole dealt set rather than over zero cards.
            //
            // MOVED at milestone 8.48, when Bounce's repeat gate took the wide reading, and again at
            // milestone 8.59, when Bounce's OFFER took the floor its Move already declared - the
            // table's second report about the same card: "it should just offer legal spaces 2 away,
            // not this space 1 away which is illegal." Both moves are one literal on one node of one
            // face, and on 8.59's complete unfiltered run every pin but Silverarrow's - whose own
            // Ultimate the same sweep corrected - measured byte-identical.
            //
            // MOVED AGAIN at milestone 8.82, and this one is a live playtest report: Mark for Death
            // and Hunting Season print "Move [up to two] enemy minion[s] in radius up to 3 spaces to
            // [a space / spaces] in radius", and the move declared 8.22's `WithinActorRadius` while
            // binding it NO number - which `DirectMoveDestinations.Admit` reads as admitting nothing,
            // so the face offered the player nobody at all. The leash is now
            // `WithinActorPrintedRadius`, which reads the card's own icon with p.16's items composed
            // onto it (a literal would have stopped following her Super-Shotgun's +1 Radius), and the
            // same node's "up to 3 spaces" is now `UpTo` rather than `IfAble`. Two literals on one
            // node of two faces; measured on the offline harness, not predicted.
            // MILESTONE 8.83 moved Swift for two corrections 8.82 routed and found: Hunting Season now
            // composes TWO subject+move pairs for "Move up to two enemy minions in radius" (it
            // composed one, so half its own sentence was unreachable), and her three jump Skills'
            // push declares `up to` rather than `may`, which had made "up to 1 / 2 spaces" an exact
            // distance and Drop Trooper's "up to TWO enemy units" a forced both.
            new IdentityPin("swift", "swift-9-6-rev-1", "237b99fb616201508264a0811ef1838843772947016c52329cf760be71d639a6"),
            // Milestone 8.39i. THE FOURTEENTH PIN, on exactly the terms the seventh through thirteenth
            // arrived on: a pin is a claim about content a configured match loads, and milestone 8.39h
            // composed Cutter's eighteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the thirteen pins above confirmed byte-identical in that same run. Milestone 8.35z50's
            // trap was watched for and did not recur: a pin added WITHOUT its staged configuration
            // measures the hero as contributing ZERO cards, and hers went in beside the row in
            // EveryDealtDefinition below, in one change.
            new IdentityPin("cutter", "cutter-9-6-rev-1", "fd2983678b27f58bf89ac56c322026838fd1ef85dbab59f254a27cb668c63257"),
            // Milestone 8.40j. THE FIFTEENTH PIN, on exactly the terms the seventh through fourteenth
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.40i composed Bain's eighteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the fourteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: a pin added WITHOUT its staged configuration measures the hero as
            // contributing ZERO cards, and his went in beside the row in EveryDealtDefinition below,
            // in one change.
            new IdentityPin("bain", "bain-9-6-rev-1", "c4c608de12b41e59840aa98b7616cd7adc6e7b98c78ce9b5441c473c59c037d9"),
            new IdentityPin("garrus", "garrus-8-41d-rev-1", "b222004923fee801bdb2bf46c2d780a31f8377e2a84162190c2c9b9ef57cc793"),
            new IdentityPin("widget", "widget-8-43d-rev-1", "5aa8887cf06a4c9111385818ee9a2e92b584057a1876d1b57d6c5072517c9ac2"),
            // Milestone 8.57g. THE EIGHTEENTH PIN, on exactly the terms every pin from the seventh on
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.57f composed Rowenna's nineteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the seventeen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: her staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees her eighteen rather than measuring her as zero.
            new IdentityPin("rowenna", "rowenna-9-1-rev-1", "fce7f0431601bb5378ca9d249d3458209564df2985499cbb385b6474e57f5f93"),
            // Milestone 8.61j. THE NINETEENTH PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.61i composed Min's nineteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the eighteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for
            // and did not recur: her staged configuration went into EveryDealtDefinition below in
            // this same change, so the walk sees her eighteen rather than measuring her as zero.
            new IdentityPin("min", "min-9-1-rev-1", "4c1afdda2ab251074e4e47a6a41436c4791cd5406bda5d8e02c8a08dd10c1f6c"),
            // Milestone 8.69g. THE TWENTIETH PIN, on exactly the terms every pin from the seventh on
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.69f composed Brynn's nineteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the nineteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: his staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees his eighteen rather than measuring him as zero.
            new IdentityPin("brynn", "brynn-8-69f-rev-1", "34a30af3b2261deb9f6038e1d64653945162fbd62b671a4412e9f68f3a6ee928"),
            // Milestone 8.71h. THE TWENTY-FIRST PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.71g composed Hanu's nineteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the twenty pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: his staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees his eighteen rather than measuring him as zero.
            new IdentityPin("hanu", "hanu-8-71g-rev-1", "0221cf38d5c1a4cbff71fc59cf891217cc32a09dbafead37fb65a9b775f417ca"),
            // Milestone 8.75b. THE TWENTY-SECOND PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestones
            // 8.75, 8.75a and the two 8.75b preparations composed Ignatia's nineteen faces without
            // dealing her.
            //
            // SHE IS THE FIRST HERO WHOSE PIN COVERS A CARD WITH TWO PRINTED TEXTS. Every one of her
            // eighteen dealt faces is one `Conditional` on `ignatia-coin-sun@1` with the sun subtree
            // then and the moon subtree else, so the canonical string this walker builds carries BOTH
            // halves of every card - which is exactly what a pin should carry, because both halves
            // are printed and either one can run.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the twenty-one pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: her staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees her eighteen rather than measuring her as zero.
            //
            // Milestone 8.113 MOVED THIS PIN WITH ITS REVISION: the user corrected her magma supply
            // from three to four and directed p.18's short-supply step onto her placements (a new
            // `ignatia-reclaim-magma@1` node before each moon placement and a shortfall question in
            // the wake), so the reachable semantics changed and the revision moved with them
            // (ignatia-8-75-rev-1 / 166986e3... -> ignatia-8-113-rev-1). Measured, not predicted.
            new IdentityPin("ignatia", "ignatia-8-113-rev-1", "4e186aa701bbea15dae7b4cbb94564839fcc9eedaa04907a2205f8cb93f6d306"),
            // 8.92d's generic object-selection vocabulary widened the canonical walk for eight
            // earlier owners above without changing their printed behavior; the measured pins move
            // with the shared contract. Trinkets is the newly dealt owner of its full graph.
            new IdentityPin("trinkets", "trinkets-9-1-rev-1", "151104c2486030a425a2177d10ebfcb24c2f3562cd40ee135e49105afb063695"),
            new IdentityPin("snorri", "snorri-8-93f-rev-1", "133f27b08c4e3f21853e200c6807ac162778587d0226d98d7de86248e3f46e93"),
            new IdentityPin("mrak", "mrak-9-1-rev-1", "1398d39d034d44313c25a63fc101548fc9b044a1de82c07cf7e3e541e70de378"),
            new IdentityPin("wuk", "wuk-9-2-rev-1", "ba5a77ea2f15e34d4b383b31b70755e2f127f72a1ee3b9dedc21d9161f410fdc"),
            // Milestone 9.8f. Mortimer, dealt; measured on the run that dealt him, with his staged
            // configuration in EveryDealtDefinition in the same change.
            new IdentityPin("mortimer", "mortimer-9-8-rev-1", "ddac9405d44a1f238ea17080e840eed85bd6e5e015991378c655573cd80426fc")
        };

        [Test]
        public void EveryReachableSemanticReferenceMovesWithItsOwningContentIdentity()
        {
            var measured = Measure(MatchContentCatalogs.ForThisBuild());
            var violations = FindViolations(Pins, measured);

            Assert.That(violations, Is.Empty,
                "A configured hero's reachable semantics and its owning content identity did not " +
                "move together. Measured identity pins:\n" +
                string.Join("\n", measured.Select(value => value.AsPinSource())) +
                "\nViolations:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ReferencedEffectMutationWithoutOwnerRevision_IsRejectedByTheOracle()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var baseline = Measure(catalogs);
            var baselinePins = baseline
                .Select(value => new IdentityPin(value.Owner, value.ContentRevision, value.Digest))
                .ToArray();

            var highVoltage = catalogs.TriggeredEffects.Definitions.Single(definition =>
                definition.Id.Equals(
                    WaspEffectContent.HighVoltageTriggeredEffect(
                        WaspCardContent.ContentRevision).Id));
            var changedNode = highVoltage.Nodes.First(node =>
                node.Kind == EffectNodeKind.Capability);
            var mutation = new EffectNodeDefinition(
                changedNode.Id,
                changedNode.Kind,
                changedNode.FailurePolicy == EffectFailurePolicy.Mandatory
                    ? EffectFailurePolicy.IfAble
                    : EffectFailurePolicy.Mandatory,
                changedNode.Capability,
                changedNode.ChildNodeIds,
                changedNode.Inputs,
                changedNode.Outputs,
                changedNode.Chooser,
                changedNode.MinimumSelections,
                changedNode.MaximumSelections,
                changedNode.MaximumRepeats,
                changedNode.RepeatTargetBinding,
                changedNode.BoundSelectionNodeId,
                changedNode.ImmunityOverrides);
            var changedDefinition = new EffectDefinition(
                highVoltage.Id,
                highVoltage.ContentRevision,
                highVoltage.RootNodeId,
                highVoltage.Nodes.Select(node => node.Id.Equals(mutation.Id) ? mutation : node));
            var changedCatalogs = new MatchContentCatalogs(
                catalogs.Capabilities,
                catalogs.Components,
                new TriggeredEffectCatalog(catalogs.TriggeredEffects.Definitions.Select(definition =>
                    definition.Reference.Equals(highVoltage.Reference)
                        ? changedDefinition
                        : definition)));

            var violations = FindViolations(baselinePins, Measure(changedCatalogs));
            Assert.That(violations.Count, Is.EqualTo(1));
            Assert.That(violations[0], Does.StartWith("wasp:"),
                "Changing High Voltage's referenced definition under the same revision must be " +
                "attributed to Wasp by the same uniform owner walk used for all six heroes.");
        }

        /// <summary>
        /// Every definition this build deals to anybody, from every configuration that deals one.
        ///
        /// <para>Until milestone 8.35z34 this was one configuration - rulebook p.5's official
        /// six-player line-up - because the six pinned heroes were exactly that line-up. Dodger the
        /// Warlock is dealt by a staged configuration and by no official one, so reading the official
        /// line-up alone would have measured her as absent, which is what it did on this pin's first
        /// run.</para>
        ///
        /// <para><b>The union is the honest source rather than a widening of convenience.</b> What
        /// this oracle claims is that a face's identity moves when its content moves, and the claim is
        /// about content a match can load - not about which line-up happens to load it. A definition
        /// appears once however many configurations deal it, because a definition IS its identity and
        /// two seats holding one are not two faces.</para>
        /// </summary>
        private static IReadOnlyList<CardDefinition> EveryDealtDefinition() =>
            MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red).CardDefinitions
                .Concat(MatchConfiguration.CreateStagedDodgerForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.35z38. Misa's staged configuration joins for the reason Dodger's did,
                // and the list is now the shape every later dealing milestone appends to rather than
                // a special case for one hero: no official line-up deals either of them, so an
                // official-only read would measure both as absent.
                .Concat(MatchConfiguration.CreateStagedMisaForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.35z46. Silverarrow's staged configuration joins on the same terms: no
                // official line-up deals her either.
                .Concat(MatchConfiguration.CreateStagedSilverarrowForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.35z50. Ursafar's staged configuration joins on the same terms, and the
                // omission is worth a line rather than a silent append: adding his pin without adding
                // this row measured him as contributing ZERO definitions, which is what the run that
                // minted the pin actually reported. The append is not optional bookkeeping - it is
                // half of what a pin means.
                .Concat(MatchConfiguration.CreateStagedUrsafarForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.35z53. Whisper's staged configuration joins on the same terms, and the
                // line above earned its warning: her pin was added first and the complete run reported
                // her contributing ZERO definitions, exactly as his did. The trap is that a pin and its
                // staged source are ONE change - a pin alone names a hero the union cannot see, and the
                // oracle then fails on the count rather than on a digest, which reads like a content
                // fault and is not one.
                .Concat(MatchConfiguration.CreateStagedWhisperForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.37. Tali's staged configuration joins on the same terms, and this is
                // the last row the package arc adds: with it the union covers every hero this build
                // deals. Added in the same change as her pin, which is what the two warnings above
                // ask for.
                .Concat(MatchConfiguration.CreateStagedTaliForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.38e. Swift's staged configuration, on the same terms and added in the
                // same change as her pin, which is what the two warnings above ask for. Tali's row
                // called itself the last the arc adds; the roster re-opened on 2026-09-11, so what
                // that sentence claims - the union covering every hero this build deals - is kept true
                // by this row rather than by that one.
                .Concat(MatchConfiguration.CreateStagedSwiftForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.39i. Cutter's staged configuration, on the same terms and added in the
                // same change as her pin, which is what the two warnings above ask for. She is the
                // fourteenth, so the union covering every hero this build deals is kept true by this
                // row rather than by the one above it.
                .Concat(MatchConfiguration.CreateStagedCutterForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.40j. Bain's staged configuration, on the same terms and added in the
                // same change as his pin, which is what the two warnings above ask for. He is the
                // fifteenth, so the union covering every hero this build deals is kept true by this
                // row rather than by the one above it.
                .Concat(MatchConfiguration.CreateStagedBainForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                .Concat(MatchConfiguration.CreateStagedGarrusForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                .Concat(MatchConfiguration.CreateStagedWidgetForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.57g. Rowenna's staged configuration, on the same terms and added in the
                // SAME change as her pin - which is what the two warnings above ask for and what they
                // earned the hard way twice: a pin without its staged source names a hero this union
                // cannot see, and the oracle then fails on the count rather than on a digest, which
                // reads like a content fault and is not one. She is the eighteenth, so the union
                // covering every hero this build deals is kept true by this row rather than by the
                // one above it.
                .Concat(MatchConfiguration.CreateStagedRowennaForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.61j. Min's staged configuration, on the same terms and added in the
                // SAME change as her pin - which is what the two warnings above ask for. She is the
                // nineteenth, so the union covering every hero this build deals is kept true by
                // this row rather than by the one above it.
                .Concat(MatchConfiguration.CreateStagedMinForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.69g. Brynn's staged configuration, on the same terms and added in the
                // SAME change as his pin - which is what the two warnings above ask for. He is the
                // twentieth, so the union covering every hero this build deals is kept true by this
                // row rather than by the one above it.
                .Concat(MatchConfiguration.CreateStagedBrynnForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.71h. Hanu's staged configuration, on the same terms and added in the
                // SAME change as his pin - which is what the two warnings above ask for. He is the
                // twenty-first, so the union covering every hero this build deals is kept true by
                // this row rather than by the one above it.
                .Concat(MatchConfiguration.CreateStagedHanuForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.75b. Ignatia's staged configuration, on the same terms and added in the
                // SAME change as her pin - which is what the two warnings above ask for. She is the
                // twenty-second, so the union covering every hero this build deals is kept true by
                // this row rather than by the one above it.
                .Concat(MatchConfiguration.CreateStagedIgnatiaForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                .Concat(MatchConfiguration.CreateStagedTrinketsForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.93f. Snorri's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedSnorriForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.112g. Mrak's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedMrakForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 8.114f. Wuk's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedWukForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 9.8f. Mortimer's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedMortimerForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                .GroupBy(definition => definition.Id.Value, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();

        private static IReadOnlyList<MeasuredIdentity> Measure(MatchContentCatalogs catalogs)
        {
            var dealt = EveryDealtDefinition();
            return Pins.Select(pin =>
                {
                    var definitions = dealt
                        .Where(definition => definition.Id.Value.StartsWith(
                            pin.Owner + "-", StringComparison.Ordinal))
                        .OrderBy(definition => definition.Id.Value, StringComparer.Ordinal)
                        .ToArray();
                    Assert.That(definitions, Has.Length.EqualTo(18),
                        $"{pin.Owner} must contribute all seventeen playable cards and the Ultimate.");
                    var revisions = definitions.Select(definition => definition.ContentRevision)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                    Assert.That(revisions, Has.Length.EqualTo(1),
                        $"{pin.Owner}'s configured set must move as one content identity.");

                    var walker = new SemanticWalker(catalogs);
                    foreach (var definition in definitions)
                    {
                        walker.Append(definition);
                    }

                    return new MeasuredIdentity(pin.Owner, revisions[0], walker.Digest());
                })
                .ToArray();
        }

        private static IReadOnlyList<string> FindViolations(
            IEnumerable<IdentityPin> pins,
            IEnumerable<MeasuredIdentity> measured)
        {
            var expectedByOwner = pins.ToDictionary(value => value.Owner, StringComparer.Ordinal);
            return measured
                .Where(value =>
                    !expectedByOwner.TryGetValue(value.Owner, out var expected) ||
                    !string.Equals(
                        expected.ContentRevision, value.ContentRevision, StringComparison.Ordinal) ||
                    !string.Equals(expected.Digest, value.Digest, StringComparison.Ordinal))
                .Select(value =>
                {
                    var expected = expectedByOwner[value.Owner];
                    return $"{value.Owner}: expected {expected.ContentRevision}/{expected.Digest}, " +
                        $"measured {value.ContentRevision}/{value.Digest}";
                })
                .ToArray();
        }

        private sealed class SemanticWalker
        {
            private readonly MatchContentCatalogs catalogs;
            private readonly StringBuilder canonical = new StringBuilder();
            private readonly HashSet<object> seen = new HashSet<object>(ReferenceIdentityComparer.Instance);
            private readonly HashSet<EffectCapabilityReference> expandedCapabilities =
                new HashSet<EffectCapabilityReference>();
            private readonly HashSet<EffectDefinitionId> expandedTriggeredDefinitions =
                new HashSet<EffectDefinitionId>();

            public SemanticWalker(MatchContentCatalogs catalogs)
            {
                this.catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
            }

            public void Append(object value) => AppendValue(value);

            public string Digest()
            {
                using (var sha = SHA256.Create())
                {
                    return BitConverter.ToString(
                            sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString())))
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();
                }
            }

            private void AppendValue(object value)
            {
                if (value == null)
                {
                    canonical.Append("null;");
                    return;
                }

                if (value is string text)
                {
                    canonical.Append("string:").Append(text.Length).Append(':').Append(text).Append(';');
                    return;
                }

                if (value is EffectDefinitionReference effectReference)
                {
                    AppendTriggeredReference(effectReference);
                    return;
                }

                if (value is CardDefinitionReference cardReference)
                {
                    canonical.Append("card-ref:")
                        .Append(cardReference.DefinitionId.Value).Append(';');
                    return;
                }

                if (value is EffectCapabilityReference capabilityReference)
                {
                    AppendCapability(capabilityReference);
                    return;
                }

                if (value is EffectInputBinding input)
                {
                    canonical.Append("input{");
                    AppendValue(input.ParameterName);
                    AppendValue(input.SourceKind);
                    AppendValue(input.ValueKind);
                    AppendValue(input.Role);
                    AppendValue(input.SourceNodeId);
                    AppendValue(input.SourceOutputName);
                    AppendValue(input.Literal);
                    canonical.Append("};");
                    return;
                }

                if (value is Type semanticType)
                {
                    canonical.Append("type:").Append(StableTypeName(semanticType)).Append(';');
                    return;
                }

                var type = value.GetType();
                if (type.IsEnum || type.IsPrimitive || value is decimal)
                {
                    canonical.Append(StableTypeName(type)).Append(':').Append(value).Append(';');
                    return;
                }

                if (value is IEnumerable sequence)
                {
                    canonical.Append(StableTypeName(type)).Append("[");
                    foreach (var item in sequence)
                    {
                        AppendValue(item);
                    }

                    canonical.Append("]; ");
                    return;
                }

                if (!type.IsValueType && !seen.Add(value))
                {
                    canonical.Append("seen:").Append(StableTypeName(type)).Append(';');
                    return;
                }

                canonical.Append(StableTypeName(type)).Append('{');
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.GetIndexParameters().Length == 0)
                    .Where(property => property.Name != "ContentRevision" &&
                        property.Name != "Reference" && property.Name != "Root")
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .ToArray();
                if (properties.Length == 0)
                {
                    canonical.Append(value);
                }
                else
                {
                    foreach (var property in properties)
                    {
                        canonical.Append(property.Name).Append('=');
                        try
                        {
                            AppendValue(property.GetValue(value, null));
                        }
                        catch (TargetInvocationException)
                        {
                            canonical.Append("throws;");
                        }
                    }
                }

                canonical.Append("};");
            }

            private static string StableTypeName(Type type)
            {
                if (type.IsArray)
                {
                    return StableTypeName(type.GetElementType()) + "[]";
                }

                if (!type.IsGenericType)
                {
                    return type.FullName ?? type.Name;
                }

                var definition = type.GetGenericTypeDefinition();
                return (definition.FullName ?? definition.Name) + "<" + string.Join(
                    ",", type.GetGenericArguments().Select(StableTypeName)) + ">";
            }

            private void AppendCapability(EffectCapabilityReference reference)
            {
                canonical.Append("capability:").Append(reference).Append(';');
                if (!reference.IsDefined)
                {
                    return;
                }

                if (!expandedCapabilities.Add(reference))
                {
                    return;
                }

                Assert.That(catalogs.Capabilities.TryLoad(reference, out var handler), Is.True,
                    $"The owner walk reached unregistered capability '{reference}'.");
                foreach (var activeDefinition in handler.ActiveEffectDefinitions
                    .OrderBy(definition => definition.DefinitionId, StringComparer.Ordinal))
                {
                    AppendValue(activeDefinition);
                }
            }

            private void AppendTriggeredReference(EffectDefinitionReference reference)
            {
                canonical.Append("triggered-ref:").Append(reference.DefinitionId.Value).Append(';');
                if (!expandedTriggeredDefinitions.Add(reference.DefinitionId))
                {
                    return;
                }

                var definition = catalogs.TriggeredEffects.Definitions.SingleOrDefault(candidate =>
                    candidate.Id.Equals(reference.DefinitionId));
                Assert.That(definition, Is.Not.Null,
                    $"The owner walk reached unregistered triggered effect '{reference.DefinitionId}'.");
                AppendValue(definition);
            }
        }

        private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
        {
            public static ReferenceIdentityComparer Instance { get; } =
                new ReferenceIdentityComparer();

            public new bool Equals(object left, object right) => ReferenceEquals(left, right);

            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }

        private sealed class IdentityPin
        {
            public IdentityPin(string owner, string contentRevision, string digest)
            {
                Owner = owner;
                ContentRevision = contentRevision;
                Digest = digest;
            }

            public string Owner { get; }
            public string ContentRevision { get; }
            public string Digest { get; }
        }

        private sealed class MeasuredIdentity
        {
            public MeasuredIdentity(string owner, string contentRevision, string digest)
            {
                Owner = owner;
                ContentRevision = contentRevision;
                Digest = digest;
            }

            public string Owner { get; }
            public string ContentRevision { get; }
            public string Digest { get; }

            public string AsPinSource() =>
                $"new IdentityPin(\"{Owner}\", \"{ContentRevision}\", \"{Digest}\"),";
        }
    }
}
