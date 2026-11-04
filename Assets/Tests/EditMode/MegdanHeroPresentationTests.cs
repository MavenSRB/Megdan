using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MegdanHeroPresentationTests
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<CardDefinition>> Definitions =
            new Dictionary<string, IReadOnlyList<CardDefinition>>(StringComparer.Ordinal)
            {
                ["brogan"] = BroganCardContent.DealtDefinitions,
                ["misa"] = MisaCardContent.DealtDefinitions,
                ["rowenna"] = RowennaCardContent.DealtDefinitions,
                ["tigerclaw"] = TigerclawCardContent.DealtDefinitions,
                ["gydion"] = GydionCardContent.DealtDefinitions,
                ["tali"] = TaliCardContent.DealtDefinitions
            };

        [Test]
        public void TemporaryCatalog_MapsExactlySixHeroesAndAll108ExistingDefinitions()
        {
            Assert.That(MegdanHeroPresentationCatalog.Entries.Select(entry => entry.ContentId.Value),
                Is.EquivalentTo(Definitions.Keys));
            Assert.That(Definitions.Values.SelectMany(value => value).Count(), Is.EqualTo(108));

            foreach (var pair in Definitions)
            {
                Assert.That(pair.Value, Has.Count.EqualTo(18), pair.Key);
                Assert.That(MegdanHeroPresentationCatalog.TryForContent(
                    new HeroContentId(pair.Key), out var hero), Is.True, pair.Key);
                Assert.That(hero.HeroIdValue, Is.EqualTo("hero-" + pair.Key));
                Assert.That(hero.OriginalCardArtFile, Does.EndWith(".jpg"));
                Assert.That(File.Exists(Path.Combine(
                    UnityEngine.Application.dataPath, "..", "Art", "Inputs",
                    hero.OriginalCardArtFile)), Is.True,
                    pair.Key + " card art must come from Veljko's supplied original input.");
                foreach (var definition in pair.Value)
                {
                    Assert.That(MegdanHeroPresentationCatalog.TryForCard(
                        definition.Reference, out var fromCard), Is.True, definition.Reference.ToString());
                    Assert.That(fromCard, Is.SameAs(hero));
                }
            }
        }

        [Test]
        public void MegdanFrame_UsesEachCardsTypedColorForTheValueRail()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            try
            {
                var byColor = Definitions.Values.SelectMany(value => value)
                    .GroupBy(definition => definition.Progression.Color)
                    .ToArray();
                Assert.That(byColor.Select(group => group.Key), Is.EquivalentTo(new[]
                {
                    CardColor.Gold, CardColor.Silver, CardColor.Red,
                    CardColor.Green, CardColor.Blue, CardColor.None
                }));
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    foreach (var group in byColor)
                    {
                        var definition = group.First();
                        var face = DirectorHandsUnityView.BuildStandaloneCard(
                            CurrentRoundCards.DeckCard(
                                new CardInstanceId("megdan-color-" + group.Key), definition));
                        var expected = MatchHudTheme.Face(group.Key);
                        foreach (var name in new[]
                                 {
                                     "gd-card-initiative-clock", "gd-card-defense-shield-face",
                                     "gd-card-movement-arrow-face"
                                 })
                        {
                            Assert.That(Descendants(face).Single(element => element.name == name)
                                .style.backgroundColor.value, Is.EqualTo(expected), group.Key.ToString());
                        }

                        var initiative = Descendants(face).Single(element =>
                            element.name == "gd-card-initiative");
                        var clock = Descendants(face).Single(element =>
                            element.name == "gd-card-initiative-clock");
                        Assert.That(initiative.style.backgroundColor.value, Is.EqualTo(Color.clear));
                        Assert.That(clock.style.width.value.value, Is.EqualTo(42f));
                        Assert.That(clock.style.height.value.value, Is.EqualTo(42f));
                        Assert.That(clock.style.borderTopLeftRadius.value.value, Is.EqualTo(21f));
                        Assert.That(Descendants(clock).Count(element =>
                            element.name == "gd-card-initiative-clock-mark"), Is.EqualTo(4));
                        Assert.That(Descendants(face).Count(element =>
                            element.name == "gd-card-defense-shield-point"), Is.EqualTo(1));
                        Assert.That(Descendants(face).Count(element =>
                            element.name == "gd-card-movement-arrow-head"), Is.EqualTo(1));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Misa_RemainsTheRuntimeIdentityWhileOnlyHerDisplayNameBecomesRiki()
        {
            var roster = RegisteredHeroRoster.ForThisBuild.Single(hero =>
                hero.ContentId.Equals(new HeroContentId("misa")));
            Assert.That(roster.HeroIdValue, Is.EqualTo("hero-misa"));
            Assert.That(roster.ContentId.Value, Is.EqualTo("misa"));
            Assert.That(MegdanHeroPresentationCatalog.DisplayName(roster.ContentId, "Misa"),
                Is.EqualTo("Riki"));
            Assert.That(MegdanHeroPresentationCatalog.LobbyIdentifier(roster.HeroIdValue),
                Is.EqualTo("RIKI"));
            Assert.That(MisaCardContent.DealtDefinitions.All(definition =>
                definition.Reference.DefinitionId.Value.StartsWith("misa-", StringComparison.Ordinal)),
                Is.True, "Presentation must never mint or rename a card/runtime identity.");
        }

        [Test]
        public void RepresentativeSyntheticFaces_KeepTypedContentAndUseMappedPortraitArt()
        {
            var portrait = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            try
            {
                using (ConfiguredCardArtwork.UseTextureLoader(path =>
                    path.StartsWith("Private/Megdan/HeroPortraits/", StringComparison.Ordinal)
                        ? portrait
                        : null))
                {
                    foreach (var pair in Definitions)
                    {
                        var definition = pair.Value.First();
                        var card = CurrentRoundCards.DeckCard(
                            new CardInstanceId("megdan-face-" + pair.Key), definition);
                        var face = DirectorHandsUnityView.BuildStandaloneCard(card);
                        var descendants = Descendants(face).ToArray();

                        Assert.That(descendants.Any(element =>
                            element.name == "gd-megdan-card-frame"), Is.True, pair.Key);
                        Assert.That(descendants.OfType<Image>().Single(element =>
                            element.name == "gd-card-artwork").image, Is.SameAs(portrait), pair.Key);
                        Assert.That(descendants.OfType<Label>().Single(element =>
                            element.name == "gd-card-name").text,
                            Is.EqualTo(definition.DisplayName), pair.Key);
                        Assert.That(descendants.OfType<Label>().Single(element =>
                            element.name == "gd-card-printed-text").text,
                            Is.EqualTo(definition.SourceContent.PrintedText), pair.Key);
                        Assert.That(descendants.Any(element =>
                            element.name == "gd-card-primary-action"), Is.True, pair.Key);
                        Assert.That(descendants.Any(element =>
                            element.name == "gd-card-history"), Is.True, pair.Key);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portrait);
            }
        }

        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            for (var index = 0; index < root.hierarchy.childCount; index++)
            {
                var child = root.hierarchy[index];
                yield return child;
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
