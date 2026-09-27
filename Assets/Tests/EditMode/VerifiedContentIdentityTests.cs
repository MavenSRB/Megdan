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
        private static readonly IdentityPin[] Pins =
        {
            new IdentityPin("arien", "arien-8-35l-rev-1", "d33d1df2d0a012361be3a285331a5b340ce8bf2ba8111e410ee8c51dcf0fcaba"),
            // Milestone 8.35p moved Brogan alone: the three charges ask where he lands, move, and
            // only then ask whom he takes. The other five pins are byte-identical, which is the
            // measurement that says the reorder reached his content and nobody else's.
            new IdentityPin("brogan", "brogan-8-35p-rev-1", "5b03652442fed56f8ff14bd4e5c0b6ee2436bc6977ca82198d4f43ee6fb754d6"),
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
            new IdentityPin("sabina", "sabina-8-83-rev-1", "ab01b89f929ca9ac4093b9726f759765788e1ce22fb2601a76b0a488d2f66899"),
            // Milestone 8.35z20 corrected Blend Into Shadows' secondary icon (Defense 2, not Movement
            // 2 - the one-icon slot-position trap; see Docs/AI/Rules/HERO_TIGERCLAW.md's
            // settled-corrections block), which is the whole reason this digest moved.
            // MILESTONE 8.83 moved Tigerclaw for one word on one face: Master Thief ends "you may move
            // UP TO 2 spaces" and its follow-up step declared `may` at an exact 2, which is p.11's
            // OTHER wording. Light-Fingered's and Pick Pocket's "you may move 1 space" are unchanged
            // and still declare `may`, which is what makes this one card's correction rather than a
            // family's.
            new IdentityPin("tigerclaw", "tigerclaw-8-83-rev-1", "e45bac162e24500f81728e71d135991b6b67594b0c293250b33db6f4bee40923"),
            new IdentityPin("wasp", "wasp-8-25e7-rev-1", "b698578d1318ececdab14974d276dce37ecedd5e11fa314c0e3d82e0c8b5e122"),
            // MILESTONE 8.83 moved Xargatha for the same clause on three faces: Charm, Control and
            // Dominate print "you may move an enemy minion in radius UP TO 2 spaces" and their move
            // declared `Mandatory` at 2, so the minion could only ever be put in the second ring.
            // Her Siren's Call is untouched - its literal 1 is a number the sentence writes down.
            new IdentityPin("xargatha", "xargatha-8-83-rev-1", "98f522e78a90d5394a8cfb7c23051836f0baadbd3b37165815fdda8fbfcff127"),
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
            new IdentityPin("dodger", "dodger-8-35z45-rev-1", "808c9021f2db36699250ddd088ae46314c25cebd21257cd23016de0fd0293a6d"),
            // Milestone 8.35z38. THE EIGHTH PIN, arriving on exactly the terms the seventh did: a pin
            // is a claim about content a configured match loads, and milestone 8.35z29 composed Misa
            // without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output. A
            // filtered pin run has been green while digests moved, which is why the rule is the
            // complete run rather than the cheap one.
            new IdentityPin("misa", "misa-8-36f-rev-1", "53a1e174907cb1ce751b52c26109934517ece7fdc6cc72c866f67f8083524c73"),
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
            new IdentityPin("silverarrow", "silverarrow-8-66-rev-1", "11788f758120c458f46eb53712c635e196c2a12160d3186f5785a4810d08b770"),
            // Milestone 8.35z50. THE TENTH PIN, arriving on exactly the terms the seventh through
            // ninth did: a pin is a claim about content a configured match loads, and milestone
            // 8.35z31 composed Ursafar without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the nine pins above confirmed byte-identical in that same run. A filtered pin run has
            // been green while digests moved, which is why the rule is the complete run rather than
            // the cheap one.
            new IdentityPin("ursafar", "ursafar-8-35z50-rev-1", "6afc2a46e8ded1ca39c739e35d393a9b22e5970371176b369d1c17e9d141c6bb"),
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
            new IdentityPin("whisper", "whisper-8-36f3-rev-1", "21e1ec7fb5288a6f0e78324c098cd97895d81d976822f88341c90f00ed575ea4"),
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
            new IdentityPin("tali", "tali-8-37a-rev-1", "c6c67c032f5958a8e2887ee48c37ae6e0ea450826a0bd7886d330224d7623588"),
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
            new IdentityPin("swift", "swift-8-83-rev-1", "c43ec6af33606d0daf1e97dde74aa21700353179bf12c5047b6e8d3aa5d4267a"),
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
            new IdentityPin("cutter", "cutter-8-39h-rev-1", "552f6bb69f7cb9ca3a46928b2fa808e08898b3f0393e996a965ac2b95ce7c4d7"),
            // Milestone 8.40j. THE FIFTEENTH PIN, on exactly the terms the seventh through fourteenth
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.40i composed Bain's eighteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the fourteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: a pin added WITHOUT its staged configuration measures the hero as
            // contributing ZERO cards, and his went in beside the row in EveryDealtDefinition below,
            // in one change.
            new IdentityPin("bain", "bain-8-40i-rev-1", "0dc50c9f41365e63ced1fd5e752e791ed0609f2bdb38f719e2a86822caed98b0"),
            new IdentityPin("garrus", "garrus-8-41d-rev-1", "8e77e5416493208228062356a4ef801702cfc0f37d6c6dfabdc49e775d8ed3fd"),
            new IdentityPin("widget", "widget-8-43d-rev-1", "25a3bfcea7e90a57f8563047d70cf2a782907bd2e6da5bab47b102ede6b38bfd"),
            // Milestone 8.57g. THE EIGHTEENTH PIN, on exactly the terms every pin from the seventh on
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.57f composed Rowenna's nineteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the seventeen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: her staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees her eighteen rather than measuring her as zero.
            new IdentityPin("rowenna", "rowenna-8-57f-rev-1", "83a95f36192da92cf0586c04bb4ca6e0e9e7879dfa780d82ff1c45ccc829f76d"),
            // Milestone 8.61j. THE NINETEENTH PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.61i composed Min's nineteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the eighteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for
            // and did not recur: her staged configuration went into EveryDealtDefinition below in
            // this same change, so the walk sees her eighteen rather than measuring her as zero.
            new IdentityPin("min", "min-8-61i-rev-1", "21a226902a4d3cda16cf7e91f396dde019c6cb0bf8f06bf5e3c921386d306036"),
            // Milestone 8.69g. THE TWENTIETH PIN, on exactly the terms every pin from the seventh on
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.69f composed Brynn's nineteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the nineteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: his staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees his eighteen rather than measuring him as zero.
            new IdentityPin("brynn", "brynn-8-69f-rev-1", "32bd6584bed4d4a9283473464bf1a86b68ad9c8fe20cd27559a05490cc7a2e8d"),
            // Milestone 8.71h. THE TWENTY-FIRST PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.71g composed Hanu's nineteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the twenty pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: his staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees his eighteen rather than measuring him as zero.
            new IdentityPin("hanu", "hanu-8-71g-rev-1", "5637931ca485749e4343a2672beffe73a630d67932e51ab483335c7a93fc42d4"),
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
            new IdentityPin("ignatia", "ignatia-8-75-rev-1", "166986e315339edf015beb4eb6286ada58420819ee8d89df97f02a7bc86d5300"),
            // 8.92d's generic object-selection vocabulary widened the canonical walk for eight
            // earlier owners above without changing their printed behavior; the measured pins move
            // with the shared contract. Trinkets is the newly dealt owner of its full graph.
            new IdentityPin("trinkets", "trinkets-8-92e-complete", "294850cef87d54727d35b3f8ba9b9a5ef81c6aa18419a63ba0ba99d080eeb7e0"),
            new IdentityPin("snorri", "snorri-8-93f-rev-1", "b8f7a7cd2a2f12bec30c0577fce038cb0a705011f769d38f5a7635b7eaca3869")
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
