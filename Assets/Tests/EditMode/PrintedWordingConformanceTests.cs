using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.1. Every dealt face's printed words held against its composition, family by
    /// family (Rules/PRINTED_WORDING.md). The playtest record behind it: Swift's Mark for Death,
    /// Ursafar's Angry Roar, Bounce through Reload! and Tali's Totem asked and then did nothing;
    /// Shotgun, Drop Trooper and Whisper's forced move composed a printed word as a different rule.
    /// Milestone 8.83's sweep found three more such families by comparing declarations with the
    /// printed words - by hand. This compares them mechanically, for every hero the roster deals,
    /// including heroes dealt after it was written.
    ///
    /// <para><b>How a rule reads.</b> Each family recognises every composed shape that honestly
    /// expresses its word, and flags a face whose words and composition disagree. A face the
    /// recogniser cannot see through is listed in that family's allow-list WITH the reason, so the
    /// list is a record of every judgement made, not a place to hide a defect. A new entry needs the
    /// clause and the composed shape that expresses it; a wording with two honest readings goes to
    /// the user first.</para>
    ///
    /// <para><b>Printed text is read only here.</b> Rules never read it
    /// (<c>CardDefinitionTests.EveryDefinitionStatesItsSourceContentOrAnExplicitAbsence_AndNoRuleReadsIt</c>).</para>
    /// </summary>
    public sealed class PrintedWordingConformanceTests
    {
        // -------------------------------------------------------------------------------------------
        // The corpus
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryDealtHero_ContributesItsPrintedFaces()
        {
            var faces = PrintedWordingCorpus.All;
            var missing = QuickGameSetupConfiguration.DealtHeroes
                .Select(hero => hero.ContentId.Value)
                .Where(hero => !faces.Any(face => face.Hero == hero && face.Text.Length > 0))
                .ToArray();

            Assert.That(missing, Is.Empty,
                "Every dealt hero is audited: a hero with no transcribed face here is a hero whose " +
                "printed words nothing holds the composition to.");
            Assert.That(faces.Count, Is.GreaterThan(400),
                "Twenty-six heroes deal hundreds of faces; a corpus this small lost most of them.");
        }

        [Test]
        public void TheCorpus_WalksActiveEffectTriggerBodies()
        {
            var grenade = PrintedWordingCorpus.All.Single(face => face.Card.Id.Value == "min-death-grenade");

            Assert.That(grenade.Bodies.Select(body => body.Reference.DefinitionId.Value),
                Does.Contain("min-death-grenade-triggered"),
                "\"End of turn:\" is the half of the sentence an Active effect runs later; an audit that " +
                "stops at the card-text body cannot see it (milestone 8.117 found the same gap in the " +
                "player-path gate).");
        }

        // -------------------------------------------------------------------------------------------
        // Optionality: "may"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A face that prints "may" composes something a player can decline or leave undone. A face
        /// with none is the forced-choice shape (Drop Trooper pushing both units, 8.83).
        /// </summary>
        [Test]
        public void EveryPrintedMay_ComposesSomethingOptional()
        {
            var flagged = PrintedWordingCorpus.All
                .Where(face => Words(face, @"\bmay\b") && !HasOptionalSurface(face))
                .Select(face => face.ToString())
                .Except(MayWithoutOptionalSurface.Keys)
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "These faces print \"may\" and compose nothing a player can decline. Compose the " +
                "optional step, or add the face to MayWithoutOptionalSurface with the clause that " +
                "makes \"may\" a permission, a token property or reminder text.");
            AssertAllowListIsLive(MayWithoutOptionalSurface.Keys,
                face => Words(face, @"\bmay\b") && !HasOptionalSurface(face));
        }

        private static readonly IReadOnlyDictionary<string, string> MayWithoutOptionalSurface =
            new Dictionary<string, string>
            {
                ["min | min-trip-mine"] = "\"units may move through them\" is the Mine token's property, composed on the token.",
                ["min | min-cluster-mine"] = "\"units may move through them\" is the Mine token's property, composed on the token.",
                ["min | min-mine-field"] = "\"units may move through them\" is the Mine token's property, composed on the token.",
                ["hanu | hanu-hurry-up"] = "\"This may change the initiative order\" is reminder text.",
                ["ignatia | ignatia-equilibrium"] = "\"you may apply either\" is the Equilibrium handler's per-action choice of text.",
                ["whisper | whisper-grim-reaper"] = "\"you may choose one, or both\" raises the basic modal's ceiling; one stays legal.",
                ["misa | misa-power-overwhelming"] = "\"you may choose two different options instead\" raises the modal ceiling; one stays legal."
            };

        // -------------------------------------------------------------------------------------------
        // Optionality: "up to"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "up to N" makes the distance or count the decision (p.11). An exact declaration under it is
        /// the shape 8.83 corrected on Swift, Xargatha and Tigerclaw.
        /// </summary>
        [Test]
        public void EveryPrintedUpTo_ComposesTheNumberAsAChoice()
        {
            var flagged = PrintedWordingCorpus.All
                .Where(face => Words(face, @"\bup to\b") && !HasUpToChoice(face))
                .Select(face => face.ToString())
                .Except(UpToWithoutChoice.Keys)
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "These faces print \"up to\" and compose only exact numbers. Declare UpTo, a " +
                "zero-minimum selection or a RepeatAction maximum - or add the face to " +
                "UpToWithoutChoice with the rule that settles its number.");
            AssertAllowListIsLive(UpToWithoutChoice.Keys,
                face => Words(face, @"\bup to\b") && !HasUpToChoice(face));
        }

        private static readonly IReadOnlyDictionary<string, string> UpToWithoutChoice =
            new Dictionary<string, string>
            {
                ["arien | arien-dangerous-current"] = "W3: \"Up to 1 enemy hero ... discards\" hits the hero present.",
                ["arien | arien-raging-stream"] = "W3: \"Up to 1 enemy hero ... discards\" hits the hero present.",
                ["arien | arien-violent-torrent"] = "W3: \"Up to 1 enemy hero ... discards\" hits the hero present.",
                ["min | min-death-grenade"] = "W3: \"Up to 1 enemy hero ... discards\" hits the hero present.",
                ["min | min-holy-death-grenade"] = "W3: \"Up to 2 enemy heroes ... discard\" hits the heroes present.",
                ["trinkets | trinkets-self-destruct"] = "W3: \"Up to two enemy heroes ... discard\" hits the heroes present.",
                ["trinkets | trinkets-emergency-protocol"] = "W3: \"Up to two enemy heroes ... discard\" hits the heroes present."
            };

        /// <summary>An UpTo declaration under words that print no "up to" is a choice the card never gave.</summary>
        [Test]
        public void EveryUpToDeclaration_IsPrinted()
        {
            var flagged = PrintedWordingCorpus.All
                .Where(face => !Words(face, @"\bup to\b") &&
                    face.Nodes.Any(node => node.FailurePolicy == EffectFailurePolicy.UpTo))
                .Select(face => face.ToString())
                .Except(UpToWithoutPrintedUpTo.Keys)
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "These faces declare UpTo where no \"up to\" is printed.");
            AssertAllowListIsLive(UpToWithoutPrintedUpTo.Keys,
                face => !Words(face, @"\bup to\b") &&
                    face.Nodes.Any(node => node.FailurePolicy == EffectFailurePolicy.UpTo));
        }

        private static readonly IReadOnlyDictionary<string, string> UpToWithoutPrintedUpTo =
            new Dictionary<string, string>
            {
                ["snorri | snorri-runic-hammer"] = "\"you may move 1 space\" behind a May Optional: UpTo 1 inside it performs the same exact step, and nought is the decline.",
                ["snorri | snorri-runic-battleaxe"] = "\"you may move 1 space\" behind a May Optional: UpTo 1 inside it performs the same exact step, and nought is the decline."
            };

        // -------------------------------------------------------------------------------------------
        // Repeats: W1 and W2
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// W1: a plain "repeat" is automatic (IfAble, no chooser). W2: "may repeat" and "repeat up to"
        /// are the player's (May). A face's repeat declarations must say which its words say.
        /// </summary>
        [Test]
        public void EveryRepeat_IsOptionalExactlyWhereItsWordsAre()
        {
            var flagged = new List<string>();
            foreach (var face in PrintedWordingCorpus.All)
            {
                var repeats = face.Nodes.Where(node => node.Kind == EffectNodeKind.RepeatAction).ToArray();
                if (repeats.Length == 0) continue;

                var optionalWords = Words(face, @"\bmay repeat\b|\brepeat up to\b");
                var optional = repeats.Count(node => node.FailurePolicy == EffectFailurePolicy.May);
                var automatic = repeats.Count(node => node.FailurePolicy == EffectFailurePolicy.IfAble);
                var agrees = optionalWords ? optional > 0 : optional == 0 && automatic > 0;
                if (!agrees)
                {
                    flagged.Add($"{face} | may={optional} ifAble={automatic}");
                }
            }

            var unexplained = flagged
                .Where(row => !RepeatWordingExceptions.Keys.Any(key => row.StartsWith(key + " |", StringComparison.Ordinal)))
                .ToArray();
            Assert.That(unexplained, Is.Empty,
                "W1: a plain \"repeat\" is RepeatAction/IfAble with no chooser. W2: \"may repeat\" and " +
                "\"repeat up to\" are RepeatAction/May with the actor choosing (Rules/PRINTED_WORDING.md).");
        }

        private static readonly IReadOnlyDictionary<string, string> RepeatWordingExceptions =
            new Dictionary<string, string>
            {
                ["xargatha | xargatha-cleave"] = "Reminder text \"You may repeat even if ...\" names the same May repeat.",
                ["tigerclaw | tigerclaw-cloak-and-daggers"] = "\"you may repeat it once; if you repeat an attack\": one May repeat, the second word is its condition.",
                ["ignatia | ignatia-chaos-incarnate"] = "\"you may perform that action again\" is p.10's repeat printed without the word."
            };

        // -------------------------------------------------------------------------------------------
        // W3: "Up to N enemy heroes ... discard"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// W3: a hero present is hit; the attacker does not skip them. Such a face composes no
        /// declinable Optional (May or UpTo with a chooser) around its hero steps.
        /// </summary>
        [Test]
        public void UpToEnemyHeroesDiscard_HitsEveryHeroPresent()
        {
            var flagged = PrintedWordingCorpus.All
                .Where(face => Words(face, @"\bup to \w+ enemy heroe?s?\b[^.]*\bdiscards?\b"))
                .Where(face => face.Nodes.Any(node => node.Kind == EffectNodeKind.Optional &&
                    node.FailurePolicy != EffectFailurePolicy.IfAble))
                .Select(face => face.ToString())
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "W3 (user, 2026-09-28): \"Up to N enemy heroes ... discard a card[, or are defeated]\" " +
                "hits every such hero present, up to N. Compose each hero's step inside an IfAble " +
                "Optional with no chooser.");
        }

        // -------------------------------------------------------------------------------------------
        // Targeting (milestone 9.2)
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// W4 (the rulebook's Defeat entry, "No exceptions."): every minion a card-text Defeat can
        /// reach is an enemy minion. A selection feeding a minion Defeat never admits a friendly one.
        /// </summary>
        [Test]
        public void EveryMinionDefeat_NamesAnEnemyMinion()
        {
            var flagged = new List<string>();
            foreach (var face in PrintedWordingCorpus.All)
            {
                var nodes = face.Nodes.ToArray();
                foreach (var defeat in nodes.Where(node => node.Kind == EffectNodeKind.Capability &&
                    node.Capability.CapabilityId.Value == "synthetic-defeat-minion"))
                {
                    foreach (var source in defeat.Inputs
                        .Where(input => input.SourceKind == EffectBindingSourceKind.NodeOutput)
                        .Select(input => nodes.FirstOrDefault(node => node.Id.Equals(input.SourceNodeId)))
                        .Where(node => node != null))
                    {
                        var relation = Literal(source, "relation");
                        if (relation.HasValue && relation != (int)SourceTeamRelation.OpposingSource &&
                            relation != (int)SourceTeamRelation.OpposingSourceOrUnaligned)
                        {
                            flagged.Add($"{face} | {source.Id.Value} relation={(SourceTeamRelation)relation}");
                        }
                    }
                }
            }

            Assert.That(flagged, Is.Empty,
                "W4: \"You can never defeat a friendly unit. No exceptions.\" A card that prints " +
                "\"defeat a minion\" without \"enemy\" still offers enemy minions only.");
        }

        /// <summary>
        /// "enemy" and "friendly" name the side a selection may pick from. A selection admitting only
        /// enemies where no "enemy" is printed narrows the card; one admitting friendlies where no
        /// "friendly" is printed widens it. Attack targets are enemies by p.13 and minion Defeats by
        /// W4, so neither needs the word; a word naming a side inside a condition ("adjacent to an
        /// enemy hero") is read off the condition's own parameter.
        /// </summary>
        [Test]
        public void EnemyAndFriendlyWords_MatchTheDeclaredRelations()
        {
            var flagged = new List<string>();
            foreach (var face in PrintedWordingCorpus.All)
            {
                var nodes = face.Nodes.ToArray();
                var attackBound = new HashSet<string>(nodes
                    .Where(node => node.Kind == EffectNodeKind.Capability &&
                        node.Capability.CapabilityId.Value == "set-attack-target")
                    .SelectMany(node => node.Inputs
                        .Where(input => input.SourceKind == EffectBindingSourceKind.NodeOutput)
                        .Select(input => input.SourceNodeId.Value)), StringComparer.Ordinal);
                var selections = nodes.Where(node => node.Kind == EffectNodeKind.Capability &&
                    Literal(node, "relation").HasValue).ToArray();
                var chosen = selections.Where(node =>
                    Literal(node, "activity") != (int)RuleActivity.Attack &&
                    !attackBound.Contains(node.Id.Value)).ToArray();
                var enemyWord = Words(face, @"\benem(y|ies)\b") ||
                    Words(face, @"\bdefeat (a|up to \w+) minions?\b");
                var friendlyWord = Words(face, @"\bfriendly\b|\ball(y|ies)\b");
                var sideNamedInACondition = nodes.Any(node =>
                    node.Inputs.Any(input =>
                        input.ParameterName.IndexOf("Enemy", StringComparison.Ordinal) >= 0 ||
                        input.ParameterName.IndexOf("Friendly", StringComparison.Ordinal) >= 0) ||
                    (node.Kind == EffectNodeKind.Capability &&
                        node.Capability.CapabilityId.Value.IndexOf("enemy", StringComparison.Ordinal) >= 0));

                if (chosen.Any(node => Literal(node, "relation") == (int)SourceTeamRelation.OpposingSource) &&
                    !enemyWord)
                    flagged.Add(face + " | admits only enemies where no \"enemy\" is printed");
                if (chosen.Any(node => Literal(node, "relation") == (int)SourceTeamRelation.AlliedWithSource) &&
                    !friendlyWord)
                    flagged.Add(face + " | admits only friendlies where no \"friendly\" is printed");
                if (selections.Length > 0 && Words(face, @"\benem(y|ies)\b") && !sideNamedInACondition &&
                    !selections.Any(node =>
                        Literal(node, "relation") == (int)SourceTeamRelation.OpposingSource ||
                        Literal(node, "relation") == (int)SourceTeamRelation.OpposingSourceOrUnaligned))
                    flagged.Add(face + " | prints \"enemy\" and declares no enemy selection");
                if (selections.Length > 0 && Words(face, @"\bfriendly\b") && !sideNamedInACondition &&
                    !selections.Any(node => Literal(node, "relation") == (int)SourceTeamRelation.AlliedWithSource))
                    flagged.Add(face + " | prints \"friendly\" and declares no friendly selection");
            }

            var unexplained = flagged.Where(row => !RelationWordingExceptions.Keys.Any(key =>
                row.StartsWith(key + " |", StringComparison.Ordinal))).ToArray();
            Assert.That(unexplained, Is.Empty,
                "These faces' enemy/friendly words and declared relations disagree. Correct the " +
                "relation, or add the face with the clause that settles it.");
        }

        private static readonly IReadOnlyDictionary<string, string> RelationWordingExceptions =
            new Dictionary<string, string>
            {
                ["rowenna | rowenna-glorious-triumph"] = "\"your friendly heroes gain triple assist coins\" is the Defeat's assist multiplier, not a selection."
            };

        /// <summary>
        /// "another", "different" and "other" exclude something a selection would otherwise admit.
        /// Where the word counts, compares or reminds rather than excludes, the face is listed with the
        /// reading that makes it so.
        /// </summary>
        [Test]
        public void AnotherDifferentOther_DeclareAnExclusion()
        {
            var flagged = PrintedWordingCorpus.All
                .Where(face => Words(face, @"\banother\b|\bdifferent\b|\bother\b") && !HasExclusion(face))
                .Select(face => face.ToString())
                .Except(ExclusionWordingExceptions.Keys)
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "These faces print another/different/other and declare no exclusion.");
            AssertAllowListIsLive(ExclusionWordingExceptions.Keys,
                face => Words(face, @"\banother\b|\bdifferent\b|\bother\b") && !HasExclusion(face));
        }

        private static readonly IReadOnlyDictionary<string, string> ExclusionWordingExceptions =
            new Dictionary<string, string>
            {
                ["cutter | cutter-walk-the-plank"] = "\"pushed into another zone\" is a condition on the push, not a selection.",
                ["silverarrow | silverarrow-clear-shot"] = "\"not adjacent to any other unit\" is the target predicate, which leaves the target out of its own neighbourhood.",
                ["silverarrow | silverarrow-opportunity-shot"] = "\"not adjacent to any other unit\" is the target predicate, which leaves the target out of its own neighbourhood.",
                ["silverarrow | silverarrow-snap-shot"] = "\"not adjacent to any other unit\" is the target predicate, which leaves the target out of its own neighbourhood.",
                ["min | min-smoke-bomb"] = "\"you or another unit\" is the Smoke bomb's screen, a token property.",
                ["xargatha | xargatha-dangerous-slash"] = "\"each other enemy unit\" is the Attack value count (XargathaPrintedSetTests.DangerousSlash_CountsEveryOtherAdjacentEnemy_AndNotItsTarget).",
                ["xargatha | xargatha-deadly-swipe"] = "\"each other enemy unit\" is the Attack value count, Dangerous Slash's rule at +2.",
                ["xargatha | xargatha-lethal-spin"] = "\"each other enemy unit\" is the Attack value count, Dangerous Slash's rule at +3.",
                ["tigerclaw | tigerclaw-backstab"] = "Reminder text defining \"friendly unit\" as another hero or minion on your team.",
                ["arien | arien-spell-break"] = "Reminder text: \"Other action types are unaffected.\"",
                ["misa | misa-power-overwhelming"] = "\"two different options\" is the modal ceiling; p.10 already forbids one option twice."
            };

        /// <summary>
        /// p.9: the Radius icon is the maximum reach of the action, and "in radius" refers to it; p.16's
        /// items raise it. A roster selection that writes the icon's number as a literal reach loses the
        /// items (8.82/8.83's shape, found on six selections at 9.2). A literal reach is legal only
        /// where the sentence writes the number down.
        /// </summary>
        [Test]
        public void EverySelectionReach_IsThePrintedRadiusOrAWrittenNumber()
        {
            var flagged = new List<string>();
            foreach (var face in PrintedWordingCorpus.All)
            {
                foreach (var node in face.Nodes.Where(node => node.Kind == EffectNodeKind.Capability &&
                    Literal(node, "relation").HasValue))
                {
                    var reach = Literal(node, "reach");
                    if (!reach.HasValue || reach.Value <= 1) continue;
                    var written = Words(face, @"\b" + reach.Value + @"\b") ||
                        Words(face, @"\b" + NumberWord(reach.Value) + @"\b");
                    if (!written) flagged.Add($"{face} | {node.Id.Value} reach={reach}");
                }
            }

            Assert.That(flagged, Is.Empty,
                "These selections write a reach the sentence does not print. Where the card means its " +
                "Radius, bind measuresPrintedRadius so p.16's items raise it.");
        }

        // -------------------------------------------------------------------------------------------
        // Conditions (milestone 9.3)
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "if you do" / "if they do": the later step cannot happen when the earlier did not. In a face
        /// that prints it, a skippable step followed in the same Sequence by a mandatory step that reads
        /// nothing it published is the shape where the later step runs regardless. A skippable step
        /// that is its own sentence, or a reading the user settled, is listed with its reason.
        /// </summary>
        [Test]
        public void IfYouDo_TheLaterStepCannotHappenWithoutTheEarlier()
        {
            var flagged = new List<string>();
            foreach (var face in PrintedWordingCorpus.All.Where(face =>
                Words(face, @"\bif (you|they|he|she) do\b")))
            {
                foreach (var body in face.Bodies)
                {
                    var byId = body.Nodes.ToDictionary(node => node.Id);
                    foreach (var sequence in body.Nodes.Where(node => node.Kind == EffectNodeKind.Sequence))
                    {
                        var children = sequence.ChildNodeIds.Select(id => byId[id]).ToArray();
                        for (var earlier = 0; earlier < children.Length; earlier++)
                        {
                            var first = children[earlier];
                            if (first.Kind != EffectNodeKind.Capability ||
                                first.FailurePolicy == EffectFailurePolicy.Mandatory) continue;
                            foreach (var later in children.Skip(earlier + 1).Where(node =>
                                node.Kind == EffectNodeKind.Capability &&
                                node.FailurePolicy == EffectFailurePolicy.Mandatory &&
                                !node.Capability.CapabilityId.Value.Contains("actor-space") &&
                                !node.Inputs.Any(input =>
                                    input.SourceKind == EffectBindingSourceKind.NodeOutput &&
                                    input.SourceNodeId.Equals(first.Id))))
                            {
                                flagged.Add($"{face} | {first.Id.Value} -> {later.Id.Value}");
                            }
                        }
                    }
                }
            }

            var unexplained = flagged.Where(row => !IfYouDoReadings.Keys.Any(key =>
                row.StartsWith(key + " |", StringComparison.Ordinal))).Distinct().ToArray();
            Assert.That(unexplained, Is.Empty,
                "A skippable step is followed by a mandatory one that does not read it, in a face that " +
                "prints \"if you do\". Gate the later step on the earlier's output or a Conditional, or " +
                "list the face with the reading that makes the two steps independent.");
        }

        private static readonly IReadOnlyDictionary<string, string> IfYouDoReadings =
            new Dictionary<string, string>
            {
                ["tigerclaw | tigerclaw-light-fingered"] = "\"You may move 1 space.\" is its own sentence; \"if you do\" follows the mandatory theft, which stops the text when there is nobody to rob.",
                ["tigerclaw | tigerclaw-pick-pocket"] = "\"Move up to 2 spaces.\" is its own sentence; \"if you do\" follows the mandatory theft.",
                ["tigerclaw | tigerclaw-master-thief"] = "\"Move up to 2 spaces.\" is its own sentence; \"if you do\" follows the mandatory theft.",
                ["bain | bain-another-one"] = "\"End of turn: May repeat once\" is its own sentence; the arm does not depend on the retrieval.",
                ["garrus | garrus-blunt-force"] = "W5: under \"up to\", moving 0 spaces is a move, so the push follows (user, 2026-09-28).",
                ["garrus | garrus-send-flying"] = "W5: under \"up to\", moving 0 spaces is a move, so the push follows (user, 2026-09-28)."
            };

        /// <summary>
        /// A printed "+N" or "-N" to a value is declared: a roster value contribution on the action, a
        /// value modifier on an effect it raises, or a property of the token or marker the card puts in
        /// play (listed).
        /// </summary>
        [Test]
        public void EveryPrintedValueChange_IsDeclared()
        {
            var flagged = PrintedWordingCorpus.All
                .Where(face => Words(face, @"[+-]\d+ (\[\w+\] )?(Attack|Defense|Initiative|Movement|Range|Radius)\b"))
                .Where(face =>
                    face.Card.ActionDefinitions.Sum(action =>
                        action.RosterValueContributions == null ? 0 : action.RosterValueContributions.Count) == 0 &&
                    !face.Modifications.Any(modification =>
                        modification.Kind == RuleModificationKind.ValueModifier))
                .Select(face => face.ToString())
                .Except(ValueChangeCarriedByAPiece.Keys)
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "These faces print a value change and declare no contribution or modifier.");
            AssertAllowListIsLive(ValueChangeCarriedByAPiece.Keys, face =>
                Words(face, @"[+-]\d+ (\[\w+\] )?(Attack|Defense|Initiative|Movement|Range|Radius)\b") &&
                face.Card.ActionDefinitions.Sum(action =>
                    action.RosterValueContributions == null ? 0 : action.RosterValueContributions.Count) == 0 &&
                !face.Modifications.Any(modification => modification.Kind == RuleModificationKind.ValueModifier));
        }

        private static readonly IReadOnlyDictionary<string, string> ValueChangeCarriedByAPiece =
            new Dictionary<string, string>
            {
                ["tigerclaw | tigerclaw-poisoned-dagger"] = "The Poison marker's own value modifier (its MarkerDefinition).",
                ["tigerclaw | tigerclaw-poisoned-dart"] = "The Poison marker's own value modifier, at -2.",
                ["dodger | dodger-darkest-ritual"] = "\"+1 Attack item\" is an item grant, not a value change.",
                ["tali | tali-glacial-barrier"] = "The Ice token's Initiative aura (its TokenDefinition).",
                ["tali | tali-wall-of-frost"] = "The Ice token's Initiative aura.",
                ["tali | tali-pack-ice"] = "The Ice token's Initiative aura.",
                ["trinkets | trinkets-deployable-barrier"] = "The Barrier token's Defense aura (its TokenDefinition).",
                ["trinkets | trinkets-deployable-bastion"] = "The Barrier token's Defense aura."
            };

        /// <summary>
        /// "each" reaches every subject, not one. Every face that prints it for a unit is read here with
        /// the shape that does so, so a newly dealt hero's "each" cannot pass unread.
        /// </summary>
        [Test]
        public void EveryEachFace_IsReadWithTheShapeThatReachesEverySubject()
        {
            var printed = PrintedWordingCorpus.All
                .Where(face => Words(face, @"\beach (other )?(enemy|friendly|unit|hero|minion)"))
                .Select(face => face.ToString())
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(printed, Is.EqualTo(EachReadings.Keys.OrderBy(value => value, StringComparer.Ordinal)),
                "A face printing \"each\" was added or removed. Read it and record the shape that " +
                "reaches every subject.");
        }

        private static readonly IReadOnlyDictionary<string, string> EachReadings =
            new Dictionary<string, string>
            {
                ["xargatha | xargatha-dangerous-slash"] = "A roster value contribution counting every other adjacent enemy unit.",
                ["xargatha | xargatha-deadly-swipe"] = "A roster value contribution counting every other adjacent enemy unit.",
                ["xargatha | xargatha-lethal-spin"] = "A roster value contribution counting every other adjacent enemy unit.",
                ["xargatha | xargatha-long-thrust"] = "A Range contribution counting every adjacent enemy unit.",
                ["xargatha | xargatha-rapid-thrusts"] = "A Range contribution counting every adjacent enemy unit.",
                ["xargatha | xargatha-metamorphosis"] = "The Ultimate's value modifiers counting every adjacent enemy unit.",
                ["tali | tali-cold-snap"] = "synthetic-batch-direct-move moves every enemy unit in radius.",
                ["tali | tali-snowstorm"] = "synthetic-batch-direct-move moves every enemy unit in radius.",
                ["tali | tali-blizzard"] = "synthetic-batch-direct-move moves every enemy unit in radius.",
                ["wuk | wuk-trample"] = "wuk-trampled-enemy-heroes publishes every enemy hero moved through, one slot each.",
                ["wuk | wuk-angry-stampede"] = "wuk-trampled-enemy-heroes publishes every enemy hero moved through, one slot each."
            };

        // -------------------------------------------------------------------------------------------
        // Recognisers
        // -------------------------------------------------------------------------------------------

        private static int? Literal(EffectNodeDefinition node, string parameter)
        {
            var bound = node.Inputs.Where(input => input.ParameterName == parameter &&
                input.SourceKind == EffectBindingSourceKind.Literal).ToArray();
            return bound.Length == 0 ? (int?)null : bound[0].Literal.IntegerValue;
        }

        private static string NumberWord(int value) =>
            new[] { "zero", "one", "two", "three", "four", "five", "six" }[Math.Min(value, 6)];

        private static bool HasExclusion(PrintedFace face) =>
            face.Nodes.Any(node =>
                node.Inputs.Any(input =>
                    input.ParameterName.StartsWith("exclude", StringComparison.Ordinal) ||
                    input.ParameterName == "distinctTargets") ||
                (node.Kind == EffectNodeKind.RepeatAction &&
                    node.RepeatTargetBinding.ToString().StartsWith("Different", StringComparison.Ordinal)));

        private static bool Words(PrintedFace face, string pattern) =>
            Regex.IsMatch(face.Text, pattern, RegexOptions.IgnoreCase);

        private static bool HasOptionalSurface(PrintedFace face) =>
            face.Nodes.Any(node =>
                (node.Kind == EffectNodeKind.Optional && node.FailurePolicy != EffectFailurePolicy.IfAble) ||
                node.FailurePolicy == EffectFailurePolicy.May ||
                node.FailurePolicy == EffectFailurePolicy.UpTo ||
                node.Kind == EffectNodeKind.Choose ||
                node.Kind == EffectNodeKind.RepeatAction ||
                IsZeroMinimumSelection(node)) ||
            face.Modifications.Any(modification =>
                modification.Kind == RuleModificationKind.Permission ||
                modification.RemovalPreventionCost.Kind != RemovalPreventionCostKind.None);

        private static bool HasUpToChoice(PrintedFace face) =>
            face.Nodes.Any(node =>
                node.FailurePolicy == EffectFailurePolicy.UpTo ||
                (node.Kind == EffectNodeKind.Optional && node.FailurePolicy == EffectFailurePolicy.May) ||
                (node.Kind == EffectNodeKind.Capability && node.FailurePolicy == EffectFailurePolicy.May) ||
                (node.Kind == EffectNodeKind.RepeatAction && node.MaximumRepeats > 1) ||
                IsZeroMinimumSelection(node) ||
                node.Inputs.Any(input => input.ParameterName == "maximum" &&
                    input.SourceKind == EffectBindingSourceKind.Literal));

        private static bool IsZeroMinimumSelection(EffectNodeDefinition node) =>
            (node.Kind == EffectNodeKind.Capability && node.MinimumSelections == 0 && node.MaximumSelections > 0) ||
            node.Inputs.Any(input => input.ParameterName == "minimum" &&
                input.SourceKind == EffectBindingSourceKind.Literal &&
                input.Literal.Kind == EffectValueKind.Integer && input.Literal.IntegerValue == 0);

        /// <summary>An allow-list row whose face no longer trips its rule is stale and must go.</summary>
        private static void AssertAllowListIsLive(
            IEnumerable<string> allowed, Func<PrintedFace, bool> tripsTheRule)
        {
            var tripping = new HashSet<string>(
                PrintedWordingCorpus.All.Where(tripsTheRule).Select(face => face.ToString()),
                StringComparer.Ordinal);
            var stale = allowed.Where(key => !tripping.Contains(key)).ToArray();
            Assert.That(stale, Is.Empty,
                "These allow-list rows no longer trip their rule; remove them so the list stays a " +
                "record of live judgements.");
        }
    }
}
