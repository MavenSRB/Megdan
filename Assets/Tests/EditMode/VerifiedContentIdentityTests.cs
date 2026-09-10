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
        private static readonly IdentityPin[] Pins =
        {
            new IdentityPin("arien", "arien-8-35l-rev-1", "988ef437c7135eca77d70639b1d90cf433c4029dfe729410762c2bd29481d1b0"),
            // Milestone 8.35p moved Brogan alone: the three charges ask where he lands, move, and
            // only then ask whom he takes. The other five pins are byte-identical, which is the
            // measurement that says the reorder reached his content and nobody else's.
            new IdentityPin("brogan", "brogan-8-35p-rev-1", "d546a8e4744ea2b400e638ad29e626d1fc64f089fbe850e92524730830a152f9"),
            // The phantom-declaration removal moved Sabina alone: Troop Movement, Marching Orders and
            // Path to Victory each dropped a RepeatTargetBinding.DifferentTarget that no printed face
            // supports and that no code path could enforce on a Skill. The other seven pins are
            // byte-identical, which is the measurement that says the removal reached her content and
            // nobody else's - and the digest moving at all is what says a declaration is part of the
            // content's identity even when it changes no behaviour.
            new IdentityPin("sabina", "sabina-8-35z44-rev-1", "cde457981c27b328481dda578e6eef9d06ee818983ad44e369ce8b130d2129d1"),
            // Milestone 8.35z20 corrected Blend Into Shadows' secondary icon (Defense 2, not Movement
            // 2 - the one-icon slot-position trap; see Docs/AI/Rules/HERO_TIGERCLAW.md's
            // settled-corrections block), which is the whole reason this digest moved.
            new IdentityPin("tigerclaw", "tigerclaw-8-35z25-rev-1", "f1291921057ecaee084cecbab9e8c1aca07924e4fe49fcadfe11d34b0c4f8da1"),
            new IdentityPin("wasp", "wasp-8-25e7-rev-1", "eb7cf4f63a247c31b2f94faa93125fbd4ee977ebe260b91b803b0c92296db927"),
            new IdentityPin("xargatha", "xargatha-8-25e5f1-rev-1", "15ed75b47588e0ef758fd9cf0b3e06fb1458bb1113da26b6e019eba7dee08b77"),
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
            new IdentityPin("dodger", "dodger-8-35z45-rev-1", "0ae08679963c4ad7f9737fe935c9f9bba5128495fac283e380c5e42d9f7d0c98"),
            // Milestone 8.35z38. THE EIGHTH PIN, arriving on exactly the terms the seventh did: a pin
            // is a claim about content a configured match loads, and milestone 8.35z29 composed Misa
            // without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output. A
            // filtered pin run has been green while digests moved, which is why the rule is the
            // complete run rather than the cheap one.
            new IdentityPin("misa", "misa-8-36f-rev-1", "211c427604fd42053b516548187343efd245b71b0a8ab376ee40a2b3ea1ccaed"),
            // Milestone 8.35z46. THE NINTH PIN, arriving on exactly the terms the seventh and eighth
            // did: a pin is a claim about content a configured match loads, and milestone 8.35z30
            // composed Silverarrow without dealing her.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output.
            new IdentityPin("silverarrow", "silverarrow-8-35z46-rev-1", "738e726343ff3837ac2871c0f3c450d320a4cfa894561b3e5456d2958913c67c"),
            // Milestone 8.35z50. THE TENTH PIN, arriving on exactly the terms the seventh through
            // ninth did: a pin is a claim about content a configured match loads, and milestone
            // 8.35z31 composed Ursafar without dealing him.
            //
            // MEASURED, not predicted, on a COMPLETE unfiltered run - the pin was added with a
            // deliberately wrong value and the digest read off this oracle's own failure output, with
            // the nine pins above confirmed byte-identical in that same run. A filtered pin run has
            // been green while digests moved, which is why the rule is the complete run rather than
            // the cheap one.
            new IdentityPin("ursafar", "ursafar-8-35z50-rev-1", "7197e73ba0c224c32b7463bcf943e96fa43d40bfc6ba1daa5eb79ad1251d9be1"),
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
            new IdentityPin("whisper", "whisper-8-36f3-rev-1", "ffab5620b82e7b1b7310fff782971a2124baf0b6b6697cd25ef113b5f525eae5"),
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
            new IdentityPin("tali", "tali-8-37-rev-1", "265b8c5e0cc533fd9423bf109404a39eac09b3743fa200a38ae720e867076f7f")
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
