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
        private static readonly IdentityPin[] Pins =
        {
            new IdentityPin("arien", "arien-8-35l-rev-1", "5dd40507cb7cf29f1fc8005f6446685bf459591b9742976967df93a3028813be"),
            // Milestone 8.35p moved Brogan alone: the three charges ask where he lands, move, and
            // only then ask whom he takes. The other five pins are byte-identical, which is the
            // measurement that says the reorder reached his content and nobody else's.
            new IdentityPin("brogan", "brogan-8-35p-rev-1", "cc94ae2fc6853f29166d8a0432d5f2aa1d7df75bf3d3e884466e072f1ba649e5"),
            // The phantom-declaration removal moved Sabina alone: Troop Movement, Marching Orders and
            // Path to Victory each dropped a RepeatTargetBinding.DifferentTarget that no printed face
            // supports and that no code path could enforce on a Skill. The other seven pins are
            // byte-identical, which is the measurement that says the removal reached her content and
            // nobody else's - and the digest moving at all is what says a declaration is part of the
            // content's identity even when it changes no behaviour.
            new IdentityPin("sabina", "sabina-8-35z44-rev-1", "34255c4ea7866675ff912fabb852b480713a3db9d41dd3d5ed941747e21d2024"),
            // Milestone 8.35z20 corrected Blend Into Shadows' secondary icon (Defense 2, not Movement
            // 2 - the one-icon slot-position trap; see Docs/AI/Rules/HERO_TIGERCLAW.md's
            // settled-corrections block), which is the whole reason this digest moved.
            new IdentityPin("tigerclaw", "tigerclaw-8-35z25-rev-1", "9e90449d8355cef92a056ff4cc4ae39a7d0f00405961d2608a0b539adff798b6"),
            new IdentityPin("wasp", "wasp-8-25e7-rev-1", "d86e05cf60ca75af94b5ad3f077d4fb8541211df7369526bba762a5e3b5a096c"),
            new IdentityPin("xargatha", "xargatha-8-25e5f1-rev-1", "9301b89845b10cf00e9002bb51781eb12eb32760caa56888785a3a83009b8d81"),
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
            new IdentityPin("dodger", "dodger-8-35z45-rev-1", "f1db617e94b5223144facc8c3ee1a24f26cab41fdb339628ff1353334c6b53d1"),
            // Milestone 8.35z38. THE EIGHTH PIN, arriving on exactly the terms the seventh did: a pin
            // is a claim about content a configured match loads, and milestone 8.35z29 composed Misa
            // without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output. A
            // filtered pin run has been green while digests moved, which is why the rule is the
            // complete run rather than the cheap one.
            new IdentityPin("misa", "misa-8-36f-rev-1", "5d229a27f0a0adcf8a26e7fe831dcc2a92959db9edcb70de3c9ed8f599b0caec"),
            // Milestone 8.35z46. THE NINTH PIN, arriving on exactly the terms the seventh and eighth
            // did: a pin is a claim about content a configured match loads, and milestone 8.35z30
            // composed Silverarrow without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output.
            new IdentityPin("silverarrow", "silverarrow-8-35z46-rev-1", "be30860309c483b71964a591a71f09eae25b829519f111264e84e120b74e2d24"),
            // Milestone 8.35z50. THE TENTH PIN, arriving on exactly the terms the seventh through
            // ninth did: a pin is a claim about content a configured match loads, and milestone
            // 8.35z31 composed Ursafar without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the nine pins above confirmed byte-identical in that same run. A filtered pin run has
            // been green while digests moved, which is why the rule is the complete run rather than
            // the cheap one.
            new IdentityPin("ursafar", "ursafar-8-35z50-rev-1", "eab908f366273b956c7b1de1d0c62374cac8b6c0290264323be788365d403e85"),
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
            new IdentityPin("whisper", "whisper-8-36f3-rev-1", "e819b97041fcab05748c2b4d02a2ac5d9c5d0da43ee823f9b01a94a7f5f25f93"),
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
            new IdentityPin("tali", "tali-8-37a-rev-1", "b746dfae0d337b6513bab5c8f0a06b23e4e4d7f9b49f4a36b3bfb9240a23bde0"),
            // Milestone 8.38e. THE THIRTEENTH PIN, and the first added after the package arc closed.
            // It arrives on exactly the terms the seventh through twelfth did: a pin is a claim about
            // content a configured match loads, and milestone 8.38d composed Swift without dealing
            // her. MEASURED, not predicted, on a COMPLETE unfiltered run, with the twelve pins above
            // confirmed byte-identical in that same run - which for this milestone means identical to
            // the values IT re-pinned, because 8.38d moved all twelve for a reason recorded at the top
            // of this list. Milestone 8.35z50's trap was watched for and did not recur: her staged
            // configuration went into EveryDealtDefinition in this same change, so the digest below is
            // over her whole dealt set rather than over zero cards.
            new IdentityPin("swift", "swift-8-42-rev-1", "568e3260f07531bebf6ab4b6ae7d49dd8762c47b01d4c96982fc07193fee2ef5"),
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
            new IdentityPin("cutter", "cutter-8-39h-rev-1", "f1c3d7be14b331e994cbaa29fdd9f7e0a1970228547f1fd87e09ca686afd0172"),
            // Milestone 8.40j. THE FIFTEENTH PIN, on exactly the terms the seventh through fourteenth
            // arrived on: a pin is a claim about content a configured match loads, and milestone
            // 8.40i composed Bain's eighteen faces without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run, with the fourteen pins above
            // confirmed byte-identical in that same run. Milestone 8.35z50's trap was watched for and
            // did not recur: a pin added WITHOUT its staged configuration measures the hero as
            // contributing ZERO cards, and his went in beside the row in EveryDealtDefinition below,
            // in one change.
            new IdentityPin("bain", "bain-8-40i-rev-1", "4aa847253fd0406aef2c544ea0c5e57fb737e8801cbb3c61564e31e1609289e9"),
            new IdentityPin("garrus", "garrus-8-41d-rev-1", "8244a9e8ebadb20886be72acc9bc590dd090252d0b9bb943a12fe57cddb7b335")
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
