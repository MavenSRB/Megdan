using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
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
    ///
    /// <para><b>The catalog it validates against is the build's registration list PLUS every
    /// declaration the build deliberately withholds, and milestone 8.39h is why that distinction had to
    /// be written down.</b> Milestone 7.9's deferral gate keeps a contract out of the registration list
    /// until a dealt card can start it, so a hero composed and not yet dealt names capabilities the
    /// build does not register - Cutter's Grappling Bolt names 8.39b's obstacle selection and her A
    /// Fistful of Coins names 8.39g1's solo-victory claim. Validating her against the registered list
    /// alone would report both as unregistered and say nothing at all about the parameter and output
    /// names this case exists to check, which is the opposite of covering a composition milestone. The
    /// two lists are concatenated rather than named here, so a contract that moves from one to the
    /// other needs no edit in this file, and the registration debt is asserted from the other side by
    /// each hero's own printed-set fixture.</para>
    /// </summary>
    [TestFixture]
    public sealed class DealtCorpusCompositionValidationTests
    {
        [Test]
        public void EveryVerifiedFace_PassesTheValidatorAMatchCompositionRuns()
        {
            var build = MatchContentCatalogs.ForThisBuild().Capabilities;
            var catalog = new EffectCapabilityCatalog(
                build.Registered.Select(build.Load)
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .Concat(SyntheticTargetingCapabilities.DeferredHandlers())
                    .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                    .Concat(SyntheticDisplacementCapabilities.DeferredHandlers())
                    .Concat(SyntheticBorrowedActionCapabilities.DeferredHandlers())
                    .Concat(SyntheticConditionCapabilities.DeferredHandlers())
                    .Concat(SyntheticAttackTargetSurvivalCapabilities.DeferredHandlers())

                    // Milestone 8.40i widened this concatenation for the three lists a fifteenth
                    // hero's composition named and the build did not register - the hidden card
                    // choice and the public reveal his Blue line prints, his four marker operations
                    // and his five Active effects - for the reason the seven above did: this case
                    // validates against registration PLUS every deliberately withheld declaration, or
                    // a composed-but-undealt hero's faces report "unregistered" and say nothing at
                    // all about the parameter and output names the case exists to check. MILESTONE
                    // 8.40j DEALT HIM, so all three now return EMPTY and the entries they used to
                    // supply arrive through the registered catalog above. The lines stay rather than
                    // being deleted: an empty concat is what a discharged deferral looks like, and
                    // the next hero to compose against these files finds the shape already here.
                    .Concat(SyntheticHiddenChoiceCapabilities.DeferredHandlers())
                    .Concat(BainComponentContent.DeferredHandlers())
                    .Concat(BainActiveEffectContent.DeferredHandlers())
                    .Concat(SyntheticResolvedDiscardCapabilities.DeferredHandlers())
                    .Concat(SyntheticNamedBorrowCapabilities.DeferredHandlers())
                    .Concat(GarrusActiveEffectContent.DeferredHandlers())
                    .Concat(WidgetComponentContent.DeferredHandlers())
                    .Concat(SyntheticCoupledMoveCapabilities.DeferredHandlers())

                    // Milestone 8.61i widened it again, for the nineteenth hero and for exactly the
                    // reason every line above it was added. Her printed set became whole at that
                    // block and its eighteen playable faces went into `DealtDefinitions`, while
                    // milestone 7.9's gate holds every contract they name out of the registration
                    // list until 8.61j deals her. Validating her against the registered list alone
                    // would report seventeen faces as naming unregistered capabilities and say
                    // NOTHING about the parameter and output names this case exists to check - which
                    // is the opposite of covering a composition milestone, and is what 8.57f's
                    // precedent settled for Rowenna's two withheld Places.
                    //
                    // `MinComponentContent.DeferredHandlers` is her own components' whole debt - the
                    // Smoke bomb's six operations, the mines' three, the Grenade's eight and the two
                    // doubling Active effects - and the borrowed after-attack text waits beside the
                    // family that owns its handler type. `take-deck-card-as-item@1`, her other new
                    // type, already arrives through `SyntheticRosterCapabilities` above.
                    .Concat(MinComponentContent.DeferredHandlers())
                    .Concat(SyntheticBorrowedCardTextCapabilities.DeferredHandlers())

                    // Milestone 8.69f widened it again, for the twentieth hero and for exactly the
                    // reason every line above it was added. Brynn the Seeker's printed set became
                    // whole at that block and its eighteen playable faces went into
                    // `DealtDefinitions`, while milestone 7.9's gate holds his three contracts out of
                    // the registration list until 8.69g deals him. Validating him against the
                    // registered list alone would report ten faces as naming unregistered capabilities
                    // and say NOTHING about the parameter and output names this case exists to check.
                    //
                    // `BrynnComponentContent.DeferredHandlers` is his whole debt: the obstacle-radius
                    // presence predicate, the question his split faces ask of the Attack that is
                    // running, and the union selection his three swaps choose over. He declares no
                    // token, no marker and no triggered effect, and every other contract his bodies
                    // name is the roster's own.
                    .Concat(BrynnComponentContent.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
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
