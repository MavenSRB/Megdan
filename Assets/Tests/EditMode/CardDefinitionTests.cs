using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.3. What a definition says about the face it came from, and the guarantee that
    /// nothing reads it.
    ///
    /// The 7.9a review's F-CARD-08 recorded that transcribed card text had no home in the content model
    /// at all: a verified card's numbers would live in code while its sentences lived only in a
    /// document, so an implemented definition could not be audited against its own printed wording. The
    /// home is <see cref="CardSourceContent"/>, and the discipline that keeps it from becoming a
    /// printed-text parser is that no production type may read it back.
    /// </summary>
    public sealed class CardDefinitionTests
    {
        /// <summary>
        /// The closing check the roadmap block names. Both halves matter and neither implies the other:
        /// content that states nothing cannot be audited, and content that something reads is no longer
        /// inert provenance - it is a rules input arriving through the one channel this project has
        /// spent every card milestone refusing to build.
        /// </summary>
        [Test]
        public void EveryDefinitionStatesItsSourceContentOrAnExplicitAbsence_AndNoRuleReadsIt()
        {
            var unstated = EveryDefinitionThisBuildSupplies()
                .Where(definition =>
                    definition.ContentAuthority == CardContentAuthority.VerifiedOfficial &&
                    !definition.SourceContent.StatesASourceFace)
                .Select(definition => definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(unstated, Is.Empty,
                "Verified official content states the words its face prints, or states that the face " +
                "prints none. A definition that states neither cannot be checked against its source.");

            var inventedProvenance = EveryDefinitionThisBuildSupplies()
                .Where(definition =>
                    definition.ContentAuthority != CardContentAuthority.VerifiedOfficial &&
                    definition.SourceContent.StatesASourceFace)
                .Select(definition => definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(inventedProvenance, Is.Empty,
                "A synthetic fixture has no source face, so carrying printed words would be inventing " +
                "source authority - which is what this milestone's 'synthetic test values are not used " +
                "as Wasp authority' forbids.");

            var audited = ProductionTypes();
            Assert.That(ILConformanceScanner.UnresolvedTokens(audited), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a reader could hide " +
                "behind one.");

            Assert.That(
                ILConformanceScanner.FindForbiddenReferences(
                    audited,
                    SourceContentReaders(),
                    "a rule reading transcribed card text"),
                Is.Empty,
                "Transcribed text is audit provenance. The moment a production type reads it, this " +
                "project has a printed-text parser deciding rules, which every card milestone since " +
                "4.4 has gone out of its way not to build.");
        }

        [Test]
        public void VerifiedContentThatStatesNoSourceFace_IsRefused()
        {
            var refused = Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("verified-with-no-transcription"),
                "rev-1",
                "Verified With No Transcription",
                new InitiativeValue(5),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                Array.Empty<ActionDefinition>()));

            Assert.That(refused.Message, Does.Contain("CardSourceContent.PrintsNoText"),
                "The refusal names the way a face that prints nothing states so, because that is the " +
                "legitimate case a caller might have been reaching for.");
        }

        [Test]
        public void ASyntheticFixtureClaimingPrintedWords_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("synthetic-with-transcription"),
                "rev-1",
                "TEST - Synthetic With Transcription",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                Array.Empty<ActionDefinition>(),
                CardSourceContent.Transcribed("Target a unit adjacent to you.")));
        }

        [Test]
        public void AVerifiedFaceThatPrintsNoText_StatesThatAbsenceExplicitly()
        {
            var definition = new CardDefinition(
                new CardDefinitionId("verified-wordless"),
                "rev-1",
                "Verified Wordless",
                new InitiativeValue(5),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                Array.Empty<ActionDefinition>(),
                CardSourceContent.PrintsNoText);

            Assert.That(definition.SourceContent.Kind, Is.EqualTo(CardSourceContentKind.PrintsNoText));
            Assert.That(definition.SourceContent.StatesASourceFace, Is.True);
            Assert.That(definition.SourceContent.PrintedText, Is.Empty,
                "An absence carries no words rather than a null a diagnostic would fault on.");
        }

        [Test]
        public void ATranscriptionWithNoWords_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => CardSourceContent.Transcribed("   "));
        }

        /// <summary>
        /// Milestone 8.3's third acceptance check, asked of the model rather than of a habit: the
        /// extracted card faces stay audit and presentation assets, so no part of the content model has
        /// anywhere to put one.
        /// </summary>
        [Test]
        public void TheContentModel_NamesNoImageArtOrAssetPath()
        {
            var contentTypes = new[]
            {
                typeof(CardDefinition), typeof(ActionDefinition), typeof(CardProgressionProfile),
                typeof(UltimateRuleDefinition), typeof(CardSourceContent), typeof(PrintedValueDefinition),
                typeof(WaspCardContent)
            };

            var assetWords = new[]
            {
                "image", "images", "art", "artwork", "sprite", "texture", "path", "asset", "file", "icon"
            };

            var assetShaped = contentTypes
                .SelectMany(type => type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(member => NameWords(member.Name)
                    .Any(word => assetWords.Contains(word, StringComparer.OrdinalIgnoreCase)))
                // p.16's Item icon and p.9's Range/Radius icons are printed values the rules read, not
                // pictures. They are the one place the word appears and they are named for what the
                // page calls them.
                .Where(member => !member.Name.Contains("Icon"))
                .Select(member => $"{member.DeclaringType?.Name}.{member.Name}")
                .ToArray();

            Assert.That(assetShaped, Is.Empty,
                "A definition is its values, its words and its relationships. The faces are audit and " +
                "presentation assets, and the content model has no member that could hold one.");
        }

        /// <summary>
        /// A member name's PascalCase words, with property, backing-field and accessor decoration
        /// stripped. Whole words rather than substrings, or <c>StartingHand</c> would be reported for
        /// containing "art" and <c>Profiles</c> for containing "file".
        /// </summary>
        private static IEnumerable<string> NameWords(string memberName)
        {
            var undecorated = memberName
                .Replace("get_", string.Empty)
                .Replace("set_", string.Empty)
                .Replace("k__BackingField", string.Empty)
                .Trim('<', '>', '_');

            var word = new System.Text.StringBuilder();
            foreach (var character in undecorated)
            {
                if (char.IsUpper(character) || !char.IsLetter(character))
                {
                    if (word.Length > 0)
                    {
                        yield return word.ToString();
                        word.Clear();
                    }
                }

                if (char.IsLetter(character))
                {
                    word.Append(character);
                }
            }

            if (word.Length > 0)
            {
                yield return word.ToString();
            }
        }

        /// <summary>
        /// Every definition this build supplies: Wasp's verified nineteen and the configured synthetic
        /// hands. Derived rather than listed, so content added after this milestone is audited by
        /// existing.
        /// </summary>
        private static IEnumerable<CardDefinition> EveryDefinitionThisBuildSupplies() =>
            WaspCardContent.AllDefinitions
                .Concat(SyntheticShowcaseHand.CreateRed(new HeroId("source-content-red")).Definitions)
                .Concat(SyntheticShowcaseHand.CreateBlue(new HeroId("source-content-blue")).Definitions);

        private static IEnumerable<MemberInfo> SourceContentReaders()
        {
            var members = new MemberInfo[]
                {
                    ILConformanceScanner.Property(typeof(CardDefinition), nameof(CardDefinition.SourceContent))
                        .GetGetMethod(true),
                    ILConformanceScanner.Property(typeof(CardSourceContent), nameof(CardSourceContent.PrintedText))
                        .GetGetMethod(true),
                    ILConformanceScanner.Property(typeof(CardSourceContent), nameof(CardSourceContent.Kind))
                        .GetGetMethod(true)
                }
                .Where(member => member != null)
                .ToArray();

            Assert.That(members, Has.Length.EqualTo(3),
                "A forbidden member no longer resolves. Correct the rule rather than deleting it; a " +
                "rule that names nothing protects nothing.");
            return members;
        }

        /// <summary>
        /// Every production Core and Application type. The namespace filter matters in the
        /// cross-platform harness, which compiles Core, Application and these fixtures into one
        /// assembly and would otherwise sweep this fixture's own assertions into the subject set.
        /// <see cref="CardDefinition"/> itself is excluded because its constructor legitimately reads
        /// the value it is storing; what the rule is about is a rule consulting it afterwards.
        /// </summary>
        private static IReadOnlyList<Type> ProductionTypes() =>
            ILConformanceScanner.TypesIn(typeof(CardDefinition).Assembly)
                .Concat(ILConformanceScanner.TypesIn(typeof(MatchSession).Assembly))
                .Distinct()
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital", StringComparison.Ordinal) &&
                    !(type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital.Tests", StringComparison.Ordinal))
                .Where(type => type != typeof(CardDefinition) && type != typeof(CardSourceContent))
                .ToArray();
    }
}
