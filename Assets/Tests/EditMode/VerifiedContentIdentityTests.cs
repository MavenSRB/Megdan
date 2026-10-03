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
        // 9.13a adds RequiresFriendlyUnitAdjacentToTarget to the shared Defense restriction.
        // Temporarily excluding only that property before the content fix preserved every pin.
        // The full walker is restored; false defaults change every canonical shape, while only
        // Tigerclaw changes semantics and revision by declaring the condition true on Ballista.
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
        // Milestone 9.39 (W10: a decline ends an "up to N" asked a pass at a time) moved six to
        // `-9-39-rev-1`, the six whose pass Sequences now declare `up to`: Mortimer (Awaken! and the
        // seven "choose up to N times" faces, Master of Puppets' raised passes with them), Wuk (Mystic
        // Saplings, Treetop Ride), Ignatia (the three moon scatters), Tali (Wall of Frost, Pack Ice),
        // Trinkets (both Barrier faces) and Rowenna (Grand Melee). Every other pin is byte-identical:
        // Swift's and Brynn's nested passes already stopped on a decline and were not touched.
        // Milestone 9.22 (W9, superseding W6) moved no pin: Bain's and Cutter's targets still name
        // their Move, and the engine now reads a Move of 0 spaces as a move.
        // MILESTONE 9.30b MOVED ALL TWENTY-EIGHT PINS, revisions unchanged: the shared
        // `CardProgressionProfile` gained `IsSpell` (the spellbook zone's spell definitions), so every
        // canonical string gained one `IsSpell=...False;` segment per profile while no hero's printed
        // content moved. THE CAUSE WAS ISOLATED BEFORE THE PINS WERE TOUCHED: with `IsSpell` added to
        // the walk's exclusion list all twenty-eight measured BYTE-IDENTICAL to the values replaced
        // here. The lesson's first question was asked: the marker cannot live narrower than the
        // definition, because the setup validator, the restore's hand split and the Initiative rule
        // each read it off the definition an instance names.
        // MILESTONE 9.32f MOVED ALL TWENTY-NINE PINS, revisions unchanged: the shared
        // `UltimateRuleDefinition` gained `UnlockCardMovement` (Takahide's Ready for War, a card
        // movement performed once at the unlock), so every Ultimate's canonical string gained one
        // `UnlockCardMovement=null;` segment while no hero's printed content moved. THE CAUSE WAS
        // ISOLATED BEFORE THE PINS WERE TOUCHED: with `UnlockCardMovement` added to the walk's
        // exclusion list all twenty-nine measured BYTE-IDENTICAL to the values replaced here. The
        // lesson's first question was asked: the movement is the Ultimate's whole printed content and
        // the level-up that performs it reads it off the unlocked Ultimate's rule, so there is nothing
        // narrower to put it on.
        private static readonly IdentityPin[] Pins =
        {
            new IdentityPin("arien", "arien-8-35l-rev-1", "d32f29090e443b9ea695b4832c22832b47844fb851b07cbcf0def0667a2dd0cb"),
            // Milestone 8.35p moved Brogan alone: the three charges ask where he lands, move, and
            // only then ask whom he takes. The other five pins are byte-identical, which is the
            // measurement that says the reorder reached his content and nobody else's.
            new IdentityPin("brogan", "brogan-8-35p-rev-1", "945bfb0098cad8e09a7a5fd30af03681ac88120d3cb3acdd0e6fe07092ffddec"),
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
            new IdentityPin("sabina", "sabina-9-2-rev-1", "021a961482ab62f58fe675ece6d0470b38a3a09ab4272b9ef9785657ccd8e10c"),
            // Milestone 8.35z20 corrected Blend Into Shadows' secondary icon (Defense 2, not Movement
            // 2 - the one-icon slot-position trap; see Docs/AI/Rules/HERO_TIGERCLAW.md's
            // settled-corrections block), which is the whole reason this digest moved.
            // MILESTONE 8.83 moved Tigerclaw for one word on one face: Master Thief ends "you may move
            // UP TO 2 spaces" and its follow-up step declared `may` at an exact 2, which is p.11's
            // OTHER wording. Light-Fingered's and Pick Pocket's "you may move 1 space" are unchanged
            // and still declare `may`, which is what makes this one card's correction rather than a
            // family's.
            new IdentityPin("tigerclaw", "tigerclaw-9-13a-rev-1", "fc5409843524098ec2de45b109c92c84328b8e9c1999c072bb3f469d980cd67e"),
            // MILESTONE 9.32c MOVED WASP ALONE, revision unchanged: `ConditionalObstacleOverlay` gained
            // `UnitRelation` (Takahide's unit overlays), and Static Barrier is the only dealt face that
            // declares an overlay, so her canonical string gained one `UnitRelation=None;` segment
            // while her printed content did not move. Isolated before re-pinning: with `UnitRelation`
            // excluded from the walk every pin, hers included, measured byte-identical.
            new IdentityPin("wasp", "wasp-8-25e7-rev-1", "c08ecbadb13f56dc78838d466abb8ad8ab1edf388137e31110f5056d550ff296"),
            // MILESTONE 8.83 moved Xargatha for the same clause on three faces: Charm, Control and
            // Dominate print "you may move an enemy minion in radius UP TO 2 spaces" and their move
            // declared `Mandatory` at 2, so the minion could only ever be put in the second ring.
            // Her Siren's Call is untouched - its literal 1 is a number the sentence writes down.
            new IdentityPin("xargatha", "xargatha-8-83-rev-1", "d38694b1eedd460855ee06390dc05a3ff93ab8f072a60b3cc1b0b722fda2a385"),
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
            new IdentityPin("dodger", "dodger-8-35z45-rev-1", "a69c2d77593bb1d6f6c041e287e3d4f710c629d359cafc4b220716c54fb41d12"),
            // Milestone 8.35z38. THE EIGHTH PIN, arriving on exactly the terms the seventh did: a pin
            // is a claim about content a configured match loads, and milestone 8.35z29 composed Misa
            // without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output. A
            // filtered pin run has been green while digests moved, which is why the rule is the
            // complete run rather than the cheap one.
            new IdentityPin("misa", "misa-9-35-rev-1", "120030130551f6b3bf617d6708b6c19cb33b3e756ac77a72f3ae49dacfc1b110"),
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
            new IdentityPin("silverarrow", "silverarrow-9-35-rev-1", "3a6b285e67386795e30cddb8a8ed260283a7fe7f1ff8f72578d3757bd37cd126"),
            // Milestone 8.35z50. THE TENTH PIN, arriving on exactly the terms the seventh through
            // ninth did: a pin is a claim about content a configured match loads, and milestone
            // 8.35z31 composed Ursafar without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the nine pins above confirmed byte-identical in that same run. A filtered pin run has
            // been green while digests moved, which is why the rule is the complete run rather than
            // the cheap one.
            new IdentityPin("ursafar", "ursafar-8-35z50-rev-1", "d9db938ce3bcd6a15cb27093ff10f75f9a4fa99324491ec9d272f77c1e18c177"),
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
            new IdentityPin("whisper", "whisper-9-2-rev-1", "a6735996e5fe372b306edd6f39de79c93c463a66aa951f22ab7623c98d5c750a"),
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
            new IdentityPin("tali", "tali-9-39-rev-1", "2e498d673c3bbd63138636d8c1a97d9336ff441fbee97cddc6c4362105dfce9d"),
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
            new IdentityPin("swift", "swift-9-6-rev-1", "59c01194faee96d9c6f83b0d14a65a75591308322f3571fe813d121abfe6faf9"),
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
            new IdentityPin("cutter", "cutter-9-35-rev-1", "c0c8ad46112baf4470ad1211f34c11c788b27fe18be29113f3dd5637649ed91d"),
            // Milestone 8.40j. THE FIFTEENTH PIN, on exactly the terms the seventh through fourteenth
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.40i composed Bain's eighteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the fourteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: a pin added WITHOUT its staged configuration measures the hero as
            // contributing ZERO cards, and his went in beside the row in EveryDealtDefinition below,
            // in one change.
            new IdentityPin("bain", "bain-9-6-rev-1", "099cfbabf35ee89a48a74f1973cd04a55c441d59636fa67f5b1a12db108b8269"),
            new IdentityPin("garrus", "garrus-8-41d-rev-1", "bae66cb5da29427490c748037b7d21c5ecd418841dd13a50cc51294b670cc862"),
            new IdentityPin("widget", "widget-8-43d-rev-1", "94b80c0feb76cd2b03f7d6246e93aac46a9457854bd9ba2ade931027d3865072"),
            // Milestone 8.57g. THE EIGHTEENTH PIN, on exactly the terms every pin from the seventh on
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.57f composed Rowenna's nineteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the seventeen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: her staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees her eighteen rather than measuring her as zero.
            new IdentityPin("rowenna", "rowenna-9-39-rev-1", "08a62e723e50e15279946d7d5d774744c49724235d719653c18f2b4d59588612"),
            // Milestone 8.61j. THE NINETEENTH PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.61i composed Min's nineteen faces without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the eighteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for
            // and did not recur: her staged configuration went into EveryDealtDefinition below in
            // this same change, so the walk sees her eighteen rather than measuring her as zero.
            // Milestone 9.23. Her three pushing stances' subject node declares the chosenHero and
            // chosenMinion outputs choose-target@2 always publishes, so a match paused at the push
            // restores; min-9-1-rev-1 -> min-9-23-rev-1, measured on the run that made the change.
            new IdentityPin("min", "min-9-23-rev-1", "5c3d6b4e0faccb8bc8e9e19dea9f8160da6afb4a06d27caac1a7c9a1c3a413e8"),
            // Milestone 8.69g. THE TWENTIETH PIN, on exactly the terms every pin from the seventh on
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.69f composed Brynn's nineteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the nineteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: his staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees his eighteen rather than measuring him as zero.
            new IdentityPin("brynn", "brynn-8-69f-rev-1", "7469cb2c1e039b447c9267400beffbe017e5d6f367e77b80b1aa0f797add8efd"),
            // Milestone 8.71h. THE TWENTY-FIRST PIN, on exactly the terms every pin from the seventh
            // on arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.71g composed Hanu's nineteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the twenty pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: his staged configuration went into EveryDealtDefinition below in this
            // same change, so the walk sees his eighteen rather than measuring him as zero.
            new IdentityPin("hanu", "hanu-8-71g-rev-1", "fdd7bd3e1009b530a2cd9ecee3451642798b16c5295c1c2354320550f2438f52"),
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
            new IdentityPin("ignatia", "ignatia-9-39-rev-1", "44e5d43a61b7223471cc4dd60c40fbeeb66d6272299b4fe6a6fb89edf87938d5"),
            // 8.92d's generic object-selection vocabulary widened the canonical walk for eight
            // earlier owners above without changing their printed behavior; the measured pins move
            // with the shared contract. Trinkets is the newly dealt owner of its full graph.
            new IdentityPin("trinkets", "trinkets-9-39-rev-1", "0a488bce4ecb235dcaf852df0dd57574720452866418ef82cec441f6d2da76f1"),
            // Milestone 9.17a MOVED THIS PIN WITH ITS REVISION: Charged Runestone's blue clause and
            // Inscribed Runebomb's blue and gold colours were bound to the wrong rune ordinals (blue is
            // the anvil, rune 3; gold the winged rune 1, by the glyphs on Rune Sigils), so the
            // reachable semantics changed (snorri-8-93f-rev-1 / 133f27b0... -> snorri-9-17a-rev-1).
            // Measured, not predicted.
            new IdentityPin("snorri", "snorri-9-17a-rev-1", "441eb61e5e347aabaa0ec848646fa7a9af9b7526bd92323baaaab0af6afb0141"),
            new IdentityPin("mrak", "mrak-9-1-rev-1", "84e34d686a5b4d85ae1c858bc7a3ffabf5f999937b27a7e49d18c64320ab81a9"),
            new IdentityPin("wuk", "wuk-9-39-rev-1", "0abbca1a589ecb1e8b66c1ea94a1b82dd474b96d11501866be58f8ab78b06fd0"),
            // Milestone 9.8f. Mortimer, dealt; measured on the run that dealt him, with his staged
            // configuration in EveryDealtDefinition in the same change (057a98dc...).
            // MILESTONE 9.14 MOVED THIS PIN, revision unchanged: the Tier III red card prints
            // BRAAAAAAAAINS...! with eight a's (mortimer7__7703468) and the display name had seven;
            // the display name feeds this digest, so it moved, while the identity id (braaaaaaains),
            // every printed value, every reading and the reachable semantics are as 9.8f dealt them.
            // Measured, not predicted.
            new IdentityPin("mortimer", "mortimer-9-39-rev-1", "f8fd0270b0b8f38ea53f41fbd92f35ad01bcc69818b3c4f3e9ba45dac2b5385a"),
            // Milestone 9.21j. Razzle, dealt under his first dealt revision (razzle-9-21d-prep ->
            // razzle-9-21j-rev-1); measured on the run that dealt him, with his staged configuration
            // in EveryDealtDefinition in the same change.
            new IdentityPin("razzle", "razzle-9-21j-rev-1", "261160f44dfcd6288695a9e1dbbcce0faa7fa773bca37152fc02e8a72190ae33"),
            // Milestone 9.30i. Gydion, dealt under his first dealt revision (gydion-9-30-prep ->
            // gydion-9-30i-rev-1); measured on the run that dealt him, with his staged configuration in
            // EveryDealtDefinition in the same change. The first pin over FORTY definitions: his
            // eighteen dealt fronts and the twenty-two spells setup puts in his book (GY-Q1), which a
            // configured match loads beside them and his fronts cast.
            new IdentityPin("gydion", "gydion-9-30i-rev-1", "fd2e5c5809e884c7e2ffd9a12a993a73eba4efc6fb97b59ef38c04034b8fef2c", 40),
            // Milestone 9.32h. Takahide, dealt under his first dealt revision (takahide-9-32g-rev-1 ->
            // takahide-9-32h-rev-1); measured on the run that dealt him, with his staged configuration in
            // EveryDealtDefinition in the same change. Twenty definitions: his three gold cards are all
            // dealt, and Shameful Display is boxed.
            new IdentityPin("takahide", "takahide-9-32h-rev-1", "d354701b5f10d0b58641e9b3ba8bc895d4641b1a2d0140e24f09d37a5944cc4e", 20)
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
                // Milestone 9.21j. Razzle's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedRazzleForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 9.30i. Gydion's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedGydionForInternalTest(TeamSide.Red)
                    .CardDefinitions)
                // Milestone 9.32h. Takahide's staged configuration, added in the same change as his pin.
                .Concat(MatchConfiguration.CreateStagedTakahideForInternalTest(TeamSide.Red)
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
                    Assert.That(definitions, Has.Length.EqualTo(pin.Definitions),
                        $"{pin.Owner} must contribute all seventeen playable cards and the Ultimate" +
                        (pin.Definitions > 18 ? " and every card setup puts in a spellbook." : "."));
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
            public IdentityPin(string owner, string contentRevision, string digest, int definitions = 18)
            {
                Owner = owner;
                ContentRevision = contentRevision;
                Digest = digest;
                Definitions = definitions;
            }

            /// <summary>
            /// How many definitions the owner's configured set loads: eighteen dealt faces, plus - since
            /// milestone 9.30i - the spells a spellbook holds.
            /// </summary>
            public int Definitions { get; }

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
