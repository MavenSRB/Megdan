using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38e. Every dealt face of every verified hero is walked past the effect-definition
    /// validator that <c>MatchConfiguration</c> runs when a MATCH is composed, and every violation in
    /// the whole build is reported at once.
    ///
    /// <para><b>Why this case exists, and why it is derived rather than listed.</b> That validator is
    /// the one tier a COMPOSITION milestone cannot reach: it runs against a composed match, and a
    /// composed-but-undealt hero never reaches one. So milestone 8.38d shipped Swift's eighteen faces
    /// with twenty-seven parameter and output mismatches across ten of them - guessed names where a
    /// capability's own schema says otherwise - and every offline tier called that green. The dealing
    /// found it, but a dealing finds it ONE CARD AT A TIME: <c>ValidateEffectDefinitions</c> throws on
    /// the first failing definition, which turns a systematic mistake into a sequence of ten-minute
    /// discoveries.</para>
    ///
    /// <para>It reflects over the content classes for the reason
    /// <c>AttackTargetBindingConditionalChooseOfferSweepTests</c>' own derived guard does: naming
    /// thirteen literals would close the hole for this build and leave it open for the fourteenth
    /// hero. A hero composed but not yet dealt is covered here too, which is the point - this is the
    /// check that lets a composition milestone see what only a dealing used to.</para>
    /// </summary>
    [TestFixture]
    public sealed class DealtCorpusCompositionValidationTests
    {
        [Test]
        public void EveryVerifiedFace_PassesTheValidatorAMatchCompositionRuns()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;
            var violations = new List<string>();
            var walked = 0;

            foreach (var type in typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal))
            {
                var dealt = type.GetProperty(
                    "DealtDefinitions", BindingFlags.Public | BindingFlags.Static);
                if (dealt == null)
                {
                    continue;
                }

                foreach (var definition in (IEnumerable<CardDefinition>)dealt.GetValue(null))
                {
                    foreach (var action in definition.ActionDefinitions)
                    {
                        if (action.EffectRoot == null)
                        {
                            continue;
                        }

                        walked++;
                        var rejections = action.CardTextTiming != CardTextTiming.None
                            ? EffectDefinitionValidator.ValidateCardText(
                                action.EffectRoot, catalog, action.CardTextTiming)
                            : EffectDefinitionValidator.Validate(action.EffectRoot, catalog);
                        foreach (var rejection in rejections)
                        {
                            violations.Add($"{definition.Id.Value} / {action.Id.Value}: {rejection}");
                        }
                    }
                }
            }

            Assert.That(walked, Is.GreaterThan(0),
                "The reflection must find the content classes and their effect roots at all.");
            Assert.That(violations, Is.Empty,
                "A face a configured match would fault on mid-resolution. Every one is listed, " +
                "because finding them one dealing-run at a time is what this case exists to stop:" +
                Environment.NewLine + string.Join(Environment.NewLine, violations));
        }
    }
}
