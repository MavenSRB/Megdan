using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z49. THE OFFER-DELTA SWEEP HARNESS the Crimson Trail
    /// Conditional fix will be judged by, built OFFLINE against the corpus's own composed content -
    /// no Unity launcher, no staged board, no registration.
    ///
    /// <para><b>What the feasibility prover's own family sweep could not see, and why.</b>
    /// `worker-moveprover`'s throwaway probe (milestone 8.35z42, its own `family-sweep-findings.txt`)
    /// walked every effect definition's DIRECT sequence children looking for a Move standing before
    /// another node, and found 30 such shapes across 264 cards. It never descended into a `Conditional`
    /// or `Choose` node's OWN children, so a Move nested inside one - which is exactly Whisper's Crimson
    /// Trail's shape - was invisible to it. Its own notes name the gap: "A sweep for this shape must
    /// descend into Conditional and Choose children." This fixture is that descent.</para>
    ///
    /// <para><b>What "at risk" means here, precisely.</b> A card is flagged when its composed tree
    /// contains a `Conditional` or `Choose` node whose OWN subtree reaches no mandatory typed
    /// attack-target binding (`set-attack-target@1` or a mandatory `attack-sequence@1` leaf), while the
    /// WHOLE definition contains one such binding somewhere else. That is deliberately narrower than
    /// "contains a Conditional/Choose and a binding": Swift Justice and Middlefinger of Death both
    /// compose their binding AS one of a `Choose`'s own children (the modal picks the bullet, and the
    /// bullet IS the binding) - the shape `WalkToAttackTargetBinding` already reads correctly, and
    /// flagging it would be noise. Crimson Trail's gate instead wraps an unrelated optional Move, and
    /// the binding stands AFTER the gate, in the enclosing Sequence, which the walk's current
    /// `Choose`/`Conditional` arm cannot see past.</para>
    ///
    /// <para><b>THE SWEEP FOUND A SECOND CARD ITS OWN AUTHOR DID NOT PREDICT, which is the point of
    /// building it rather than reasoning about Crimson Trail alone.</b> Ursafar's Claws That Catch
    /// (`UrsafarEffectContent.CreateProwl`, "If enraged, you may move 1 space to a space adjacent to an
    /// enemy hero; target a unit adjacent to you") composes the identical shape one level deeper - a
    /// mandatory `Conditional` (enraged?) wrapping an `Optional`(May) wrapping the move - and it fails
    /// for the same reason: the `Optional` case returns `Continue` correctly, but the walk then finds
    /// the enclosing `Conditional` itself binds nothing in its own subtree and reports `Blocked` before
    /// ever reaching the mandatory retarget standing after it. Ursafar is also undealt, and his own
    /// dealing-prep worker already wrote a journey for exactly this card without knowing it cannot
    /// currently start - flagged below and in this milestone's own notes for whoever executes his
    /// dealing, since fixing it is the identical engine change and not this milestone's to make either
    /// way.</para>
    ///
    /// <para><b>Why this is the harness the FIX is judged by, not a fix itself.</b> This prep worker's
    /// brief forbids touching `WalkToAttackTargetBinding` - the `Choose` arm shares the exact code this
    /// bug lives in and 8.35z27's whole modal-Attack family depends on the `Choose` reading standing
    /// still, so the corrective needs its own offer-delta sweep and belongs to the Opus executor. What
    /// this fixture records is CURRENT BEHAVIOUR: the complete, corpus-wide set of cards the fix could
    /// possibly move, enumerated by structure rather than guessed at. Re-run it after the fix lands -
    /// the STRUCTURAL enumeration below will not change (the fix does not alter composition, only the
    /// walk's verdict), and <c>AttackTargetBindingProbe_CrimsonTrailShape_...</c> in
    /// <c>EffectOptionFeasibilityTests.cs</c> is the behavioural half for Whisper's own card: `Is.False`
    /// today, `Is.True` once the fix lands. Ursafar's Claws That Catch would move the same way but has
    /// no regression case of its own here - one behavioural proof of the shared engine code is enough,
    /// and duplicating it under his name would be this milestone reaching into his.</para>
    ///
    /// <para><b>Why no staged-board offer table accompanies this.</b> The moveprover's own 89-row
    /// `offers-before.txt`/`offers-after.txt` table already exhaustively measured every DEALT hero
    /// (Tigerclaw, Brogan) with a card-text-bound Attack, and neither of the two cards that table moved
    /// (Combat Reflexes, Leaping Strike) composes a `Conditional` or `Choose` at all - their gap was an
    /// unenumerated Move, a different bug in the same family, already fixed. Dodger's and Misa's dealt
    /// sets contain no card-text-bound Attack that nests a Move under a `Conditional`/`Choose` either
    /// (confirmed by the sweep below). So the corpus-wide structural enumeration IS the complete
    /// board-level picture today: both heroes it affects (Whisper, Ursafar) are undealt, which is
    /// exactly why nobody has hit it in Play Mode.</para>
    ///
    /// <para><b>THE PARAGRAPH ABOVE HAS SINCE EXPIRED, and it is left standing rather than rewritten
    /// because it records what was true when the sweep was built.</b> Whisper was dealt at milestone
    /// 8.35z53 and Ursafar at 8.35z50, the shared Conditional fix landed with them, and both cards now
    /// run through real player input: Crimson Trail's behavioural half is the `Is.True` case in
    /// `EffectOptionFeasibilityTests`, and Claws That Catch has a journey of its own -
    /// `UrsafarEngagedUiJourneyPlayModeTests.ClawsThatCatch_FizzlesCalmWithNoTargetAndMovesThenTargetsOnceEnraged`,
    /// named by Ursafar's player-path gate. Recorded 2026-09-11 while discharging cleanup entry 8.35z74;
    /// the sweep itself is unchanged and its structural enumeration still stands.</para>
    /// </summary>
    public sealed class AttackTargetBindingConditionalChooseOfferSweepTests
    {
        [Test, Explicit(
            "Offline structural sweep, not a regression case - prints its findings rather than " +
            "asserting one. Run it (dotnet test --filter) before and after the executor's Crimson " +
            "Trail Conditional fix and diff stdout: the fix changes no composition, so this list " +
            "should be BYTE-IDENTICAL both times - see the class doc comment for why the behavioural " +
            "half lives in EffectOptionFeasibilityTests.cs instead.")]
        public void SweepEveryHerosCorpus_ForAConditionalOrChooseGateHidingAMandatoryBindingBehindIt()
        {
            var corpus = WholeCorpus().ToArray();
            Assert.That(corpus, Is.Not.Empty, "The sweep has to have something to walk.");

            var atRisk = new List<string>();
            foreach (var (owner, definition) in corpus)
            {
                if (definition == null)
                {
                    continue;
                }

                foreach (var gate in FindUnresolvedGates(definition))
                {
                    atRisk.Add(
                        $"{owner} | {definition.Id.Value} | gate={gate.Id.Value}:{gate.Kind} " +
                        $"({gate.FailurePolicy}) hides a binding that stands elsewhere in the " +
                        "definition");
                }
            }

            TestContext.WriteLine($"cards swept: {corpus.Length}");
            foreach (var line in atRisk)
            {
                TestContext.WriteLine(line);
            }

            TestContext.WriteLine($"at-risk gates found: {atRisk.Count}");

            // MEASURED, milestone 8.35z49, and NOT what this file predicted on its
            // first draft: TWO cards in the whole corpus have this shape today, not one.
            //
            // Whisper's Crimson Trail (`CreateGatedStepThenTarget`) - undealt, which is why nothing in
            // Play Mode has ever exercised it. Sanguine Path and Blood Pilgrimage share the identical
            // family but gate on `empty-minion-spawn-points-in-radius@1` with an UNCONDITIONAL move (an
            // `UpTo` Capability node directly under the Sequence, not wrapped in a `Conditional`) -
            // only Crimson Trail wraps its move in an actual `Conditional`, because only its clause is
            // boolean ("if there is...") rather than numeric ("up to N for every...").
            //
            // Ursafar's Claws That Catch (`UrsafarEffectContent.CreateProwl`) - ALSO undealt as of this
            // writing, found by running this sweep rather than predicted: "If enraged, you may move 1
            // space to a space adjacent to an enemy hero; target a unit adjacent to you" composes
            // `Sequence[EnragedGate(Conditional, Mandatory) -> [Optional(May) -> moveSequence], victim,
            // retarget]` - the identical shape one level deeper (the Conditional's child is an
            // `Optional` wrapping the move rather than the move directly, which changes nothing about
            // the defect: `WalkToAttackTargetBinding`'s `Optional` case already returns `Continue`
            // correctly, so the walk still reaches the `Conditional` node itself, finds no binding in
            // ITS subtree, and reports `Blocked` before ever reaching `retarget`). Ursafar's own
            // dealing-prep worker already wrote a journey for this exact card
            // (`ClawsThatCatch_..._MovesThenTargetsOnceEnraged`) without knowing it is currently
            // unstartable on every board - flagged for whoever executes his dealing, separately from
            // this milestone's own scope.
            //
            // This assertion is the sweep's own claim about the corpus, not a claim about the engine;
            // a different result here means either a new card shares the shape (read which) or one of
            // these two cards' composition changed (re-derive the regression case in
            // EffectOptionFeasibilityTests.cs against the new shape).
            Assert.That(
                atRisk.Select(line => line.Split('|')[1].Trim()).Distinct().OrderBy(id => id).ToArray(),
                Is.EqualTo(new[]
                {
                    "snorri-rune-sigils-before-attack",
                    "snorri-runic-battleaxe-before-attack",
                    "snorri-runic-hammer-before-attack",
                    "ursafar-claws-that-catch-attack-text",
                    "whisper-crimson-trail-attack-text"
                }),
                "Five composed definitions carry a mandatory Conditional the binding does not stand " +
                "inside: Whisper's Crimson Trail and Ursafar's Claws That Catch, and (8.93f) " +
                "Snorri's Runic Hammer and Runic Battleaxe, whose rune-gated optional Move stands " +
                "ahead of the binding and whose offer is proved by SnorriAttackTurnTests." +
                "TheAttackOffer_IsJudgedThroughItsRuneGatedStep, plus Rune Sigils, whose two coin " +
                "gates stand AFTER its binding and so cannot hide it. A different result here " +
                "means either a new card shares the shape (read which) or one of these two changed " +
                "(re-derive the regression case in EffectOptionFeasibilityTests.cs against the new " +
                "shape).");
        }

        // -------------------------------------------------------------------------------------------
        // The corpus - every hero's composed content, dealt or not, exactly as the family sweep's own
        // corpus was assembled: one hero, one AllDefinitions call, no registration required. A
        // `CardDefinition` is the whole printed card; its `ActionDefinitions` each carry the actual
        // `EffectDefinition`s (`EffectRoot`, and `SecondCardTextRoot` where a card prints a second p.14
        // moment) - the units `WalkToAttackTargetBinding` actually walks.
        // -------------------------------------------------------------------------------------------

        private static IEnumerable<(string Owner, EffectDefinition Definition)> WholeCorpus()
        {
            foreach (var card in ArienCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("arien", d);
            foreach (var card in BroganCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("brogan", d);
            foreach (var card in SabinaCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("sabina", d);
            foreach (var card in TigerclawCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("tigerclaw", d);
            foreach (var card in WaspCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("wasp", d);
            foreach (var card in XargathaCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("xargatha", d);
            foreach (var card in DodgerCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("dodger", d);
            foreach (var card in MisaCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("misa", d);
            foreach (var card in SilverarrowCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("silverarrow", d);
            foreach (var card in UrsafarCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("ursafar", d);
            foreach (var card in TaliCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("tali", d);
            foreach (var card in WhisperCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("whisper", d);
            foreach (var card in SwiftCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("swift", d);
            foreach (var card in CutterCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("cutter", d);
            foreach (var card in BainCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("bain", d);
            foreach (var card in GarrusCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("garrus", d);
            foreach (var card in WidgetCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("widget", d);
            // Milestone 8.57a. The eighteenth hero's first composed faces - five of nineteen at this
            // block, which is the whole point of the case below reading the content CLASSES rather
            // than a list: her line is owed the moment a `RowennaCardContent` exists, dealt or not.
            // None of her five bodies binds an Attack target by card text, so she adds no at-risk gate.
            foreach (var card in RowennaCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("rowenna", d);
            // Milestone 8.61g added the nineteenth hero's line while her set was being composed by a
            // parallel wave, reading the per-block lists through the shared corpus helper because no
            // aggregate existed yet. MILESTONE 8.61i MINTED IT, so this reads her nineteen directly
            // like every hero above. None of her faces binds an Attack target by card text - her
            // Stance riders push, move and swap off the attack's own facts - so she adds no at-risk
            // gate.
            foreach (var card in MinCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("min", d);
            // Milestone 8.69a. The twentieth hero's first composed faces - four of nineteen at this
            // block, which is again the point of the case below reading the content CLASSES rather
            // than a list: his line is owed the moment a `BrynnCardContent` exists, dealt or not. His
            // one body is Familiar Ground's modal, whose two `attack-sequence@1` bullets ARE the
            // binding and are the `Choose`'s own children - the shape this sweep's own remarks name as
            // correctly read and deliberately not flagged - so he adds no at-risk gate.
            foreach (var card in BrynnCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("brynn", d);
            // Milestone 8.71e. The twenty-first hero's first composed faces - two of nineteen at this
            // block, which is once more the point of the case below reading the content CLASSES rather
            // than a list: his line is owed the moment a `HanuCardContent` exists, dealt or not. Both
            // of his co-moves are Skills whose modal bullets are coupled Moves, so nothing in them
            // binds an Attack target at all and he adds no at-risk gate.
            foreach (var card in HanuCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("hanu", d);
            // Milestone 8.75a. The twenty-second hero, whose line is owed the moment an
            // `IgnatiaCardContent` exists, dealt or not - and hers has existed since the 8.75
            // inventory commit, which never ran this suite. Seventeen of her eighteen dealt faces
            // compose a body, and every one of them binds its Attack target by card text: her whole
            // set is ONE Conditional on `ignatia-coin-sun@1` whose two arms are the sun and moon
            // lines, and each arm of an Attack ends in `set-attack-target@1`. That is the shape this
            // sweep watches for - a gate hiding a binding - but it is not the shape it FLAGS: the
            // binding stands INSIDE both arms rather than after the gate, so each branch the
            // predicate can take reaches one, which is what `FindUnresolvedGates` asks.
            foreach (var card in IgnatiaCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("ignatia", d);
            foreach (var card in TrinketsCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("trinkets", d);
            foreach (var card in SnorriCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("snorri", d);
            foreach (var card in MrakCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("mrak", d);
            foreach (var card in WukCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("wuk", d);
            // Milestone 9.8a. The twenty-seventh hero's line is owed the moment a
            // `MortimerCardContent` exists, dealt or not. Awaken! is a Skill of token placements and
            // binds no Attack target, so he adds no at-risk gate yet.
            foreach (var card in MortimerCardContent.AllDefinitions) foreach (var d in EffectRoots(card)) yield return ("mortimer", d);
            // Milestone 9.21d. The twenty-eighth hero's line, owed as his content class exists. Stunt
            // Doubles' spawn is three optional body placements after the Attack and binds no Attack
            // target, so it adds no at-risk gate. 9.21j swaps the block list for the aggregate.
            foreach (var card in RazzleCardContent.DoublesDefinitions) foreach (var d in EffectRoots(card)) yield return ("razzle", d);
            // Milestone 9.21e. The removal faces: Phantom Strike's Conditional gates an optional removal
            // after the Attack and binds no Attack target, so it adds no at-risk gate either.
            foreach (var card in RazzleCardContent.RemovalDefinitions) foreach (var d in EffectRoots(card)) yield return ("razzle", d);
            // Milestone 9.21f. The moves and swaps: Skills and Movement text with no Attack, so they
            // bind no Attack target and add no at-risk gate.
            foreach (var card in RazzleCardContent.MoveDefinitions) foreach (var d in EffectRoots(card)) yield return ("razzle", d);
            // Milestone 9.21g. The counting faces: Rummage's and Ransack's Conditionals gate optional
            // retrievals after the Attack and bind no Attack target, so they add no at-risk gate.
            foreach (var card in RazzleCardContent.CountingDefinitions) foreach (var d in EffectRoots(card)) yield return ("razzle", d);
        }

        /// <summary>
        /// The corpus above is enumerated hero by hero, by name, and the sweep that reads it is
        /// <c>[Explicit]</c> - so a hero whose line is missing is a corpus nothing sweeps and no suite
        /// run reports. This case is NOT <c>[Explicit]</c>, and it derives what the enumeration ought to
        /// cover from the verified content classes the build actually contains rather than from a list.
        /// Milestone 8.38d.
        ///
        /// <para><b>Swift is the first hero who could have been dealt without a line</b> - all twelve
        /// before her were added as they went, verified at 8.38d against
        /// <c>grep -rho "class [A-Za-z]*CardContent"</c>. Naming a thirteenth literal here would have
        /// closed the hole for her and left it open for the fourteenth, which is why this reads the
        /// classes instead.</para>
        /// </summary>
        [Test]
        public void TheSweptCorpus_CoversEveryVerifiedContentClassTheBuildContains()
        {
            var declared = typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .Select(type => type.Name.Substring(0, type.Name.Length - "CardContent".Length)
                    .ToLowerInvariant())
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            var swept = WholeCorpus()
                .Select(entry => entry.Owner)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.Not.Empty, "The reflection must find the content classes at all.");
            Assert.That(
                swept, Is.EquivalentTo(declared),
                "Every verified hero's corpus is swept, and nothing is swept that is not one. A hero " +
                "added without a line in WholeCorpus fails here rather than going silently uncovered.");
        }

        /// <summary>Every `EffectDefinition` one printed card composes, across all of its actions.</summary>
        private static IEnumerable<EffectDefinition> EffectRoots(CardDefinition card)
        {
            foreach (var action in card.ActionDefinitions)
            {
                if (action.EffectRoot != null)
                {
                    yield return action.EffectRoot;
                }

                if (action.SecondCardTextRoot != null)
                {
                    yield return action.SecondCardTextRoot;
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // The descent WalkToAttackTargetBinding itself does not yet do.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every `Conditional`/`Choose` node in <paramref name="definition"/> whose own subtree binds
        /// nothing, where the definition's WHOLE tree binds something anyway - the shape a `Sequence`
        /// standing over the gate can reach past today, and a `Sequence` standing over a `Choose`
        /// cannot (nothing to reach past to: a `Choose` IS the decision).
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> FindUnresolvedGates(EffectDefinition definition)
        {
            if (!ContainsAMandatoryBinding(definition, definition.RootNodeId))
            {
                yield break;
            }

            foreach (var node in definition.Nodes)
            {
                if ((node.Kind != EffectNodeKind.Conditional && node.Kind != EffectNodeKind.Choose) ||
                    ContainsAMandatoryBinding(definition, node.Id))
                {
                    continue;
                }

                yield return node;
            }
        }

        /// <summary>Whether the subtree rooted at <paramref name="nodeId"/> reaches a mandatory typed binding.</summary>
        private static bool ContainsAMandatoryBinding(EffectDefinition definition, EffectNodeId nodeId)
        {
            if (!definition.TryGetNode(nodeId, out var node))
            {
                return false;
            }

            if (node.FailurePolicy == EffectFailurePolicy.Mandatory &&
                (node.Capability.Equals(SyntheticContinuationCapabilities.SetAttackTargetV1) ||
                    node.Capability.Equals(SyntheticSequenceCapabilities.AttackSequenceV1)))
            {
                return true;
            }

            return node.ChildNodeIds != null &&
                node.ChildNodeIds.Any(child => ContainsAMandatoryBinding(definition, child));
        }
    }
}
