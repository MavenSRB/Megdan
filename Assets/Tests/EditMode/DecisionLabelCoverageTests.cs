using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.46a. The content check that keeps every decision control in the build able to say
    /// what pressing it does.
    ///
    /// <para><b>Why the set is derived and not listed.</b> A second list of "nodes that ask" written
    /// here would agree with the resolver on the day it was written and drift the first time a node
    /// kind starts or stops opening rulebook p.10's confirm-or-decline. So the requirement is read
    /// from <see cref="DecisionLabelRequirementQuery"/>, which asks the resolver's own
    /// <see cref="OptionalStepQuery.OpensConfirmOrDecline"/> - the one predicate both pause sites in
    /// <c>EffectResolutionSession</c> now ask - and the synthetic case below proves that rule on the
    /// node shapes rather than only on the content that happens to use them today.</para>
    ///
    /// <para>This fixture is deliberately Core-only, so the offline harness and Unity's Edit Mode both
    /// run it. It reaches no Presentation type and therefore needs no entry in
    /// <c>Tools/DotnetTests/UnityOnlyTests.props</c>.</para>
    /// </summary>
    public sealed class DecisionLabelCoverageTests
    {
        /// <summary>
        /// The build's dealt roster, named the way the configured match names it: one row per hero,
        /// each pointing at that hero's own <c>DealtDefinitions</c>. A hero added to a configured match
        /// and not added here would be invisible to this check, which is why the count is asserted
        /// against <see cref="MatchConfigurationRosterSize"/> below.
        /// </summary>
        private static IEnumerable<DealtHero> Roster()
        {
            yield return new DealtHero("wasp", WaspCardContent.ContentRevision, WaspCardContent.DealtDefinitions);
            yield return new DealtHero("arien", ArienCardContent.ContentRevision, ArienCardContent.DealtDefinitions);
            yield return new DealtHero("brogan", BroganCardContent.ContentRevision, BroganCardContent.DealtDefinitions);
            yield return new DealtHero("sabina", SabinaCardContent.ContentRevision, SabinaCardContent.DealtDefinitions);
            yield return new DealtHero("tigerclaw", TigerclawCardContent.ContentRevision, TigerclawCardContent.DealtDefinitions);
            yield return new DealtHero("xargatha", XargathaCardContent.ContentRevision, XargathaCardContent.DealtDefinitions);
            yield return new DealtHero("dodger", DodgerCardContent.ContentRevision, DodgerCardContent.DealtDefinitions);
            yield return new DealtHero("misa", MisaCardContent.ContentRevision, MisaCardContent.DealtDefinitions);
            yield return new DealtHero("silverarrow", SilverarrowCardContent.ContentRevision, SilverarrowCardContent.DealtDefinitions);
            yield return new DealtHero("ursafar", UrsafarCardContent.ContentRevision, UrsafarCardContent.DealtDefinitions);
            yield return new DealtHero("whisper", WhisperCardContent.ContentRevision, WhisperCardContent.DealtDefinitions);
            yield return new DealtHero("tali", TaliCardContent.ContentRevision, TaliCardContent.DealtDefinitions);
            yield return new DealtHero("swift", SwiftCardContent.ContentRevision, SwiftCardContent.DealtDefinitions);
            yield return new DealtHero("cutter", CutterCardContent.ContentRevision, CutterCardContent.DealtDefinitions);
            yield return new DealtHero("bain", BainCardContent.ContentRevision, BainCardContent.DealtDefinitions);
            yield return new DealtHero("garrus", GarrusCardContent.ContentRevision, GarrusCardContent.DealtDefinitions);
            yield return new DealtHero("widget", WidgetCardContent.ContentRevision, WidgetCardContent.DealtDefinitions);
            yield return new DealtHero("rowenna", RowennaCardContent.ContentRevision, RowennaCardContent.DealtDefinitions);
            yield return new DealtHero("min", MinCardContent.ContentRevision, MinCardContent.DealtDefinitions);
            yield return new DealtHero("brynn", BrynnCardContent.ContentRevision, BrynnCardContent.DealtDefinitions);
        }

        /// <summary>
        /// The seventeen heroes milestone 8.44 packaged, plus Rowenna the Vanguard (milestone 8.57g),
        /// Min the Dragonmonk (milestone 8.61j) and Brynn the Seeker (milestone 8.69g) - the three
        /// dealings since, each of which is what makes that hero one a match configuration deals.
        /// </summary>
        private const int MatchConfigurationRosterSize = 20;

        /// <summary>
        /// The longest label in the content as milestone 8.46a shipped it is 58 characters. The bound
        /// is not typography - milestone 8.46e measures the drawn control against the drawn text - it
        /// is the authoring discipline that keeps a label a consequence rather than a paraphrase of the
        /// whole card, and it is what stops one long row from being discovered at layout time.
        /// </summary>
        private const int LabelCharacterBound = 60;

        /// <summary>
        /// Words that name the MECHANISM rather than the consequence - the user's rule for the whole
        /// series, and the reason it exists: the strip used to draw a mode button reading
        /// "MODE #1 RELOAD-BORROW-ARM". Matched whole-word and case-insensitively, so ordinary English
        /// that merely contains one of them ("another", "nothing") is untouched. "Yes" and "no" are
        /// checked separately, because both are ordinary words in a sentence and forbidden only as a
        /// whole label.
        /// </summary>
        private static readonly string[] MechanismWords =
        {
            "accept", "accepts", "accepted", "decline", "declines", "declined",
            "confirm", "confirms", "option", "options", "mode", "modes", "node", "nodes",
            "arm", "arms"
        };

        /// <summary>
        /// The build's own catalogs, which are what turn a named-by-reference triggered body into one
        /// this check can walk. Built once: it is immutable content, and building it per case is the
        /// only cost this fixture has.
        /// </summary>
        private static readonly MatchContentCatalogs Catalogs = MatchContentCatalogs.ForThisBuild();

        private sealed class DealtHero
        {
            public DealtHero(string name, string contentRevision, IReadOnlyList<CardDefinition> definitions)
            {
                Name = name;
                ContentRevision = contentRevision;
                Definitions = definitions;
            }

            public string Name { get; }
            public string ContentRevision { get; }
            public IReadOnlyList<CardDefinition> Definitions { get; }
        }

        private static IEnumerable<DecisionLabelRequirement> RequiredControls() =>
            Roster()
                .SelectMany(hero => hero.Definitions)
                .SelectMany(definition => DecisionLabelRequirementQuery.For(definition, Catalogs));

        private static IEnumerable<KeyValuePair<CardDefinitionId, KeyValuePair<EffectNodeId, DecisionLabel>>>
            DeclaredLabels() =>
            DecisionLabelCatalog.Registered
                .OrderBy(table => table.Key.Value, StringComparer.Ordinal)
                .SelectMany(table => table.Value
                    .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                    .Select(entry =>
                        new KeyValuePair<CardDefinitionId, KeyValuePair<EffectNodeId, DecisionLabel>>(
                            table.Key, entry)));

        [Test]
        public void Roster_CoversEveryHeroInTheBuild()
        {
            Assert.That(Roster().Count(), Is.EqualTo(MatchConfigurationRosterSize));
            Assert.That(
                Roster().Select(row => row.Name).Distinct().Count(),
                Is.EqualTo(MatchConfigurationRosterSize));
            Assert.That(
                Roster().Select(row => row.ContentRevision).Distinct().Count(),
                Is.EqualTo(MatchConfigurationRosterSize),
                "Content revisions identify a hero's own bodies among the build's triggered effects.");
        }

        /// <summary>
        /// A query that answered nothing would pass every case below without noticing anything, which
        /// is the failure mode a coverage check is worst at seeing. So the two sides are measured
        /// before they are compared.
        /// </summary>
        [Test]
        public void TheDerivedSetAndTheDeclaredSet_AreBothNonEmptyAndTheSameSize()
        {
            var required = RequiredControls().ToArray();
            var declared = DeclaredLabels().ToArray();

            Assert.That(required, Is.Not.Empty, "Dealt content DOES raise decision controls.");
            Assert.That(declared, Is.Not.Empty, "The catalog is no longer the series head's inert stub.");
            Assert.That(
                declared.Length, Is.EqualTo(required.Length),
                "One label per control the dealt roster raises - no more, and none missing.");
        }

        [Test]
        public void EveryDecisionControlInDealtContent_NamesItsConsequence()
        {
            var missing = new List<string>();
            foreach (var hero in Roster())
            {
                foreach (var definition in hero.Definitions)
                {
                    foreach (var requirement in DecisionLabelRequirementQuery.For(definition, Catalogs))
                    {
                        if (DecisionLabelCatalog.TryGetLabel(
                                requirement.CardId, requirement.NodeId, out _))
                        {
                            continue;
                        }

                        missing.Add(
                            $"{hero.Name}: {requirement.CardId.Value} / {requirement.NodeId.Value} " +
                            $"({requirement.Kind}, in {requirement.EffectDefinitionId.Value})");
                    }
                }
            }

            Assert.That(
                missing, Is.Empty,
                "Every Choose arm and every node that opens p.10's confirm-or-decline needs a " +
                "declared consequence label. Missing:" + Environment.NewLine +
                string.Join(Environment.NewLine, missing));
        }

        [Test]
        public void EveryDeclaredLabel_NamesANodeTheCardActuallyDeclares()
        {
            var dealt = Roster()
                .SelectMany(hero => hero.Definitions)
                .GroupBy(definition => definition.Id)
                .ToDictionary(group => group.Key, group => group.First());

            var strays = new List<string>();
            foreach (var table in DecisionLabelCatalog.Registered)
            {
                if (!dealt.TryGetValue(table.Key, out var definition))
                {
                    strays.Add($"{table.Key.Value}: no dealt card of that id");
                    continue;
                }

                var declared = DecisionLabelRequirementQuery.DeclaredNodeIds(definition, Catalogs);
                foreach (var entry in table.Value)
                {
                    if (!declared.Contains(entry.Key))
                    {
                        strays.Add($"{table.Key.Value} / {entry.Key.Value}: no such node");
                    }
                }
            }

            Assert.That(
                strays, Is.Empty,
                "A declared label names a node its card does not declare:" + Environment.NewLine +
                string.Join(Environment.NewLine, strays));
        }

        [Test]
        public void EveryDeclaredLabel_AnswersExactlyTheDecisionItsNodeOpens()
        {
            var required = RequiredControls()
                .GroupBy(requirement => requirement.CardId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        requirement => requirement.NodeId,
                        requirement => requirement.OpensConfirmOrDecline));

            var wrong = new List<string>();
            foreach (var declaration in DeclaredLabels())
            {
                var nodeId = declaration.Value.Key;
                var label = declaration.Value.Value;
                var where = $"{declaration.Key.Value} / {nodeId.Value}";

                if (!required.TryGetValue(declaration.Key, out var nodes) ||
                    !nodes.TryGetValue(nodeId, out var opensConfirmOrDecline))
                {
                    wrong.Add($"{where}: node opens no decision");
                    continue;
                }

                // A control that only ever appears as one bullet of a Choose has no decline of its
                // own - whether the modal as a whole may be declined is the Choose node's policy, and
                // one arm cannot answer for it. A node that opens p.10's confirm-or-decline has
                // exactly two answers and owes words for both, because "Stop here" is as much a
                // consequence as "Move it again". A node that is BOTH - an arm that is itself a
                // `may` - is the second case: its chosen words serve the arm too.
                if (opensConfirmOrDecline && !label.HasDeclined)
                {
                    wrong.Add($"{where}: an optional step names what declining does");
                }

                if (!opensConfirmOrDecline && label.HasDeclined)
                {
                    wrong.Add($"{where}: a Choose arm declines nothing");
                }

                if (string.IsNullOrWhiteSpace(label.Chosen))
                {
                    wrong.Add($"{where}: no chosen text");
                }
            }

            Assert.That(
                wrong, Is.Empty,
                "A declared label answers a decision of the wrong shape:" + Environment.NewLine +
                string.Join(Environment.NewLine, wrong));
        }

        /// <summary>
        /// The user's rule for the whole series, made checkable: a label names the consequence, in the
        /// printed text's own case, never the mechanism and never longer than a control can carry.
        /// </summary>
        [Test]
        public void EveryDeclaredLabel_NamesAConsequenceRatherThanTheMechanism()
        {
            var offences = new List<string>();
            foreach (var declaration in DeclaredLabels())
            {
                var where = $"{declaration.Key.Value} / {declaration.Value.Key.Value}";
                Inspect(where, "chosen", declaration.Value.Value.Chosen, offences);
                if (declaration.Value.Value.HasDeclined)
                {
                    Inspect(where, "declined", declaration.Value.Value.Declined, offences);
                }
            }

            Assert.That(
                offences, Is.Empty,
                "Labels name the consequence in the printed text's own words:" + Environment.NewLine +
                string.Join(Environment.NewLine, offences));
        }

        private static void Inspect(string where, string half, string text, ICollection<string> offences)
        {
            if (text.Length > LabelCharacterBound)
            {
                offences.Add($"{where} ({half}): {text.Length} characters, over {LabelCharacterBound}");
            }

            if (text.Trim().Length != text.Length)
            {
                offences.Add($"{where} ({half}): padded with whitespace");
            }

            if (text == text.ToUpperInvariant() && text.Any(char.IsLetter))
            {
                offences.Add(
                    $"{where} ({half}): authored in capitals; presentation upper-cases instead");
            }

            if (string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
            {
                offences.Add($"{where} ({half}): \"{text}\" says nothing about the board");
            }

            foreach (var word in Words(text))
            {
                if (MechanismWords.Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    offences.Add($"{where} ({half}): names the mechanism - \"{word}\"");
                }
            }
        }

        private static IEnumerable<string> Words(string text) =>
            text.Split(new[] { ' ', ',', ';', '.', ':', '(', ')', '"', '!', '?', '-', '/' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim('\''));

        /// <summary>
        /// The build's registered triggered effects - a p.16 permanent ability's own body, an Active
        /// effect's End-of-turn body - are named by reference and carry no card id of their own, so a
        /// label for one is only ever as good as the walk that attributes it. Several of them DO open
        /// p.10's confirm-or-decline (Wasp's High Voltage, Tigerclaw's Cloak and Daggers, Tali's
        /// Blizzard, Bain's Another One!, Garrus's Battle Fury), which is why this is measured rather
        /// than assumed: every one of them has to be reachable from the card that raises it, or the
        /// coverage case above is asking a question that skips them.
        ///
        /// <para>The second half answers the question milestone 8.46a was asked to settle out loud -
        /// whether a <c>may</c> living only in Active-effect content is reachable from a card
        /// definition at all. Swift's <c>swift-mobile-scout-triggered</c> is the case: it declares the
        /// one <c>may</c> in <c>SwiftActiveEffectContent</c>, that <c>may</c> sits on a CAPABILITY node
        /// and so opens no pause and owes no words, and the walk reaches the body regardless. Asserting
        /// that EVERY hero-owned triggered body is reached, rather than only the asking ones, is what
        /// keeps that an answer instead of a coincidence.</para>
        /// </summary>
        [Test]
        public void EveryTriggeredBodyADealtHeroOwns_IsReachedFromTheCardThatRaisesIt()
        {
            var revisions = new HashSet<string>(Roster().Select(hero => hero.ContentRevision));
            var heroBodies = Catalogs.TriggeredEffects.Definitions
                .Where(definition => revisions.Contains(definition.ContentRevision))
                .ToArray();
            Assert.That(
                heroBodies, Is.Not.Empty,
                "A content-revision filter that matches nothing would pass this case vacuously.");

            Assert.That(
                DecisionLabelRequirementQuery.For(heroBodies), Is.Not.Empty,
                "Verified triggered bodies DO open p.10's confirm-or-decline; a walk finding none " +
                "has stopped seeing them.");

            var reached = new HashSet<EffectDefinitionId>(Roster()
                .SelectMany(hero => hero.Definitions)
                .SelectMany(definition => DecisionLabelRequirementQuery.EffectRootsOf(definition, Catalogs))
                .Select(root => root.Id));

            Assert.That(
                heroBodies.Select(body => body.Id)
                    .Where(id => !reached.Contains(id))
                    .Select(id => id.Value),
                Is.Empty,
                "A hero owns a triggered body no dealt card of hers reaches, so nothing can key a " +
                "label on it to a card.");
        }

        [Test]
        public void TheRequirementQuery_AndTheSessionsPauseRule_AgreeOnWhatAsks()
        {
            Assert.That(
                OptionalStepQuery.OpensConfirmOrDecline(OptionalNode(EffectFailurePolicy.May)),
                Is.True, "`may` opens p.10's confirm-or-decline.");
            Assert.That(
                OptionalStepQuery.OpensConfirmOrDecline(OptionalNode(EffectFailurePolicy.UpTo)),
                Is.True, "`up to` opens a decision too; the session only exempts `if able`.");
            Assert.That(
                OptionalStepQuery.OpensConfirmOrDecline(OptionalNode(EffectFailurePolicy.IfAble)),
                Is.False, "`if able` attempts and skips a refusal; it never asks.");
            Assert.That(
                OptionalStepQuery.OpensConfirmOrDecline(RepeatNode(EffectFailurePolicy.May)),
                Is.True);
            Assert.That(
                OptionalStepQuery.OpensConfirmOrDecline(RepeatNode(EffectFailurePolicy.IfAble)),
                Is.False);
            Assert.That(OptionalStepQuery.OpensConfirmOrDecline(MandatoryCapabilityNode()), Is.False);
            Assert.That(
                OptionalStepQuery.OpensConfirmOrDecline(MayCapabilityNode()), Is.False,
                "A `may` CAPABILITY node opens no confirm-or-decline: the session's two pause sites " +
                "are the Optional and RepeatAction arms, and a capability that cannot act is refused " +
                "rather than offered. Fifty-seven `may` declarations stand across the dealt roster " +
                "and most of them are this shape.");
            Assert.That(OptionalStepQuery.OpensConfirmOrDecline(null), Is.False);

            // And the same rule read through the requirement query, on a definition holding one of
            // each: only the nodes that ask are required, the Choose contributes its ARMS, and an arm
            // that is itself a `may` is ONE control answering both.
            var required = DecisionLabelRequirementQuery.For(new[] { SyntheticShapes() });
            Assert.That(
                required.Select(requirement => requirement.NodeId.Value + ":" + requirement.Kind),
                Is.EquivalentTo(new[]
                {
                    "probe-may-optional:OptionalStep",
                    "probe-may-repeat:OptionalStep",
                    "probe-arm-one:ChooseArm",
                    "probe-arm-two:ChooseArm",
                    "probe-arm-may:ChooseArm, OptionalStep"
                }),
                "An `if able` Optional, an `if able` repeat, a mandatory capability and the Choose " +
                "node itself owe no label; the Choose node's arms do, and an arm that is itself a " +
                "`may` owes exactly one.");
        }

        [Test]
        public void ADecisionLabel_RequiresWordsForEveryAnswerItCarries()
        {
            Assert.That(() => new DecisionLabel(null), Throws.ArgumentException);
            Assert.That(() => new DecisionLabel("  "), Throws.ArgumentException);
            Assert.That(() => new DecisionLabel("Move it again", "   "), Throws.ArgumentException);

            var arm = new DecisionLabel("Defeat a minion adjacent to you");
            Assert.That(arm.IsDefined, Is.True);
            Assert.That(arm.HasDeclined, Is.False);
            Assert.That(arm.Declined, Is.Null);

            var step = new DecisionLabel("Move it again", "Stop here");
            Assert.That(step.HasDeclined, Is.True);
            Assert.That(step, Is.EqualTo(new DecisionLabel("Move it again", "Stop here")));
            Assert.That(step, Is.Not.EqualTo(arm));
            Assert.That(default(DecisionLabel).IsDefined, Is.False);
        }

        [Test]
        public void ADeclaration_RefusesAnIdOrALabelItCannotAttribute()
        {
            var card = new CardDefinitionId("probe-card-8-46a");
            Assert.That(
                () => new DecisionLabelDeclaration(default, new EffectNodeId("n"), new DecisionLabel("x")),
                Throws.ArgumentException);
            Assert.That(
                () => new DecisionLabelDeclaration(card, default, new DecisionLabel("x")),
                Throws.ArgumentException);
            Assert.That(
                () => new DecisionLabelDeclaration(card, new EffectNodeId("n"), default),
                Throws.ArgumentException);

            var step = DecisionLabelDeclaration.DecisionStep(card, "n", "Move it again", "Stop here");
            Assert.That(step.Label.HasDeclined, Is.True);
            Assert.That(DecisionLabelDeclaration.DecisionArm(card, "n", "Move it again").Label.HasDeclined,
                Is.False);
        }

        [Test]
        public void TheCatalog_AnswersNothingForAnIdItDoesNotHold()
        {
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(default, default, out var none), Is.False);
            Assert.That(none.IsDefined, Is.False);

            var known = DeclaredLabels().First();
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(
                    known.Key, new EffectNodeId("no-such-node-8-46a"), out var missingNode),
                Is.False);
            Assert.That(missingNode.IsDefined, Is.False);
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(
                    new CardDefinitionId("no-such-card-8-46a"), known.Value.Key, out var missingCard),
                Is.False);
            Assert.That(missingCard.IsDefined, Is.False);

            Assert.That(
                DecisionLabelCatalog.TryGetLabel(known.Key, known.Value.Key, out var found), Is.True);
            Assert.That(found, Is.EqualTo(known.Value.Value));
        }

        private static EffectNodeDefinition OptionalNode(EffectFailurePolicy policy) =>
            new EffectNodeDefinition(
                new EffectNodeId("probe-optional"), EffectNodeKind.Optional, policy,
                childNodeIds: new[] { new EffectNodeId("probe-leaf") },
                chooser: policy == EffectFailurePolicy.IfAble
                    ? EffectChooserRole.None
                    : EffectChooserRole.Controller);

        private static EffectNodeDefinition RepeatNode(EffectFailurePolicy policy) =>
            new EffectNodeDefinition(
                new EffectNodeId("probe-repeat"), EffectNodeKind.RepeatAction, policy,
                chooser: policy == EffectFailurePolicy.IfAble
                    ? EffectChooserRole.None
                    : EffectChooserRole.Controller);

        private static EffectNodeDefinition MandatoryCapabilityNode() =>
            new EffectNodeDefinition(
                new EffectNodeId("probe-leaf"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, ProbeCapability);

        private static EffectNodeDefinition MayCapabilityNode() =>
            new EffectNodeDefinition(
                new EffectNodeId("probe-may-leaf"), EffectNodeKind.Capability,
                EffectFailurePolicy.May, ProbeCapability);

        private static EffectCapabilityReference ProbeCapability =>
            new EffectCapabilityReference(new EffectCapabilityId("probe@1"), 1);

        /// <summary>
        /// One definition holding every shape the rule has an opinion about, so the query is proven on
        /// the shapes rather than only on the content that happens to use them today.
        /// </summary>
        private static EffectDefinition SyntheticShapes()
        {
            var root = new EffectNodeId("probe-root");
            var mayOptional = new EffectNodeId("probe-may-optional");
            var ifAbleOptional = new EffectNodeId("probe-if-able-optional");
            var choose = new EffectNodeId("probe-choose");
            var armOne = new EffectNodeId("probe-arm-one");
            var armTwo = new EffectNodeId("probe-arm-two");
            var armMay = new EffectNodeId("probe-arm-may");
            var leaf = new EffectNodeId("probe-leaf");
            var mayRepeat = new EffectNodeId("probe-may-repeat");
            var ifAbleRepeat = new EffectNodeId("probe-if-able-repeat");

            return new EffectDefinition(
                new EffectDefinitionId("decision-label-probe-8-46a"), "probe-8-46a", root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[]
                        {
                            mayOptional, ifAbleOptional, choose, mayRepeat, ifAbleRepeat
                        }),
                    new EffectNodeDefinition(
                        mayOptional, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { leaf }, chooser: EffectChooserRole.Controller),
                    new EffectNodeDefinition(
                        ifAbleOptional, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                        childNodeIds: new[] { leaf }),
                    new EffectNodeDefinition(
                        choose, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { armOne, armTwo, armMay },
                        chooser: EffectChooserRole.Controller,
                        minimumSelections: 1, maximumSelections: 1),
                    new EffectNodeDefinition(
                        armOne, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        ProbeCapability),
                    new EffectNodeDefinition(
                        armTwo, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        ProbeCapability),

                    // Ursafar's "• You may retrieve a discarded card" and Bain's "otherwise may repeat
                    // once or gain 2 coins": a printed bullet that is also a `may`, and therefore ONE
                    // label answering two controls.
                    new EffectNodeDefinition(
                        armMay, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { leaf }, chooser: EffectChooserRole.Controller),

                    new EffectNodeDefinition(
                        leaf, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, ProbeCapability),
                    new EffectNodeDefinition(
                        mayRepeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Controller),
                    new EffectNodeDefinition(
                        ifAbleRepeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble)
                });
        }
    }
}
