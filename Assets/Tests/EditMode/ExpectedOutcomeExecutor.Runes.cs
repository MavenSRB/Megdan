using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    internal static partial class ExpectedOutcomeExecutor
    {
        private sealed partial class CaseRun
        {
            private bool runeSlotAssignmentSeen;

            private void ValidateRuneSlotDeclaration()
            {
                var setupSpec = entry.Get("setup") as JsonObject ?? new JsonObject();
                if (setupSpec.Has("rune-slots")) RuneSlotPermutation();
            }

            private void ValidateRuneSlotConsumption()
            {
                var setupSpec = entry.Get("setup") as JsonObject ?? new JsonObject();
                if (setupSpec.Has("rune-slots") && !runeSlotAssignmentSeen)
                    throw Refuse("setup.rune-slots was not used by a real prior rune-slot assignment");
            }

            // The numbers are the four distinct glyphs in printed order on Rune Sigils.
            // They are not colors: different cards attach different clauses to each glyph.
            private int[] RuneSlotPermutation()
            {
                var setupSpec = entry.Get("setup") as JsonObject ?? new JsonObject();
                if (!(setupSpec.Get("rune-slots") is List<object> values) || values.Count != 4)
                    throw Unsupported("rune-slots", Describe(setupSpec.Get("rune-slots")));
                var ordinals = values.Select((value, index) => IntOf(value, "rune-slots[" + index + "]")).ToArray();
                if (!ordinals.OrderBy(value => value).SequenceEqual(new[] { 1, 2, 3, 4 }))
                    throw Unsupported("rune-slots permutation", Describe(setupSpec.Get("rune-slots")));
                return ordinals;
            }

            private bool TryAnswerStagedRuneAssignment(MatchSessionProjection projection, out DirectorCommand command)
            {
                command = null;
                var choice = projection.PendingEffectChoice;
                if (choice == null || choice.ValueVocabulary != EffectChoiceValueVocabulary.RuneTurnSlot)
                    return false;
                runeSlotAssignmentSeen = true;
                var slots = RuneSlotPermutation();
                var contract = projection.DecisionContract;
                for (var slot = 1; slot <= 4; slot++)
                {
                    var encoded = (slots[slot - 1] - 1) * 4 + slot;
                    var candidate = choice.Candidates.FirstOrDefault(value =>
                        value.Kind == EffectChoiceCandidateKind.Value &&
                        value.Value.Kind == EffectValueKind.Integer && value.Value.IntegerValue == encoded);
                    if (choice.SelectedCandidates.Contains(candidate)) continue;
                    var invocation = choice.SelectInvocations.FirstOrDefault(value => value.Candidate.Equals(candidate));
                    command = EffectCommand(contract, invocation) ??
                        throw Refuse("the staged Inscribe choice did not offer rune " + slots[slot - 1] +
                            " below slot " + slot);
                    return true;
                }

                command = EffectCommand(contract, choice.ConfirmInvocation);
                if (command == null) throw Refuse("the staged Inscribe permutation could not be confirmed");
                return true;
            }

            private EffectChoiceCandidate? SelectRuneCandidate(
                MatchSessionProjection projection, PendingEffectChoice choice,
                IReadOnlyList<EffectChoiceCandidate> open,
                AnswerShape shape, string value)
            {
                if (shape == AnswerShape.RuneSlot)
                {
                    if (choice.ValueVocabulary != EffectChoiceValueVocabulary.RuneTurnSlot) return null;
                    var pair = value.Split(',');
                    var slot = int.Parse(pair[0], CultureInfo.InvariantCulture);
                    var rune = int.Parse(pair[1], CultureInfo.InvariantCulture);
                    var encoded = (rune - 1) * 4 + slot;
                    return open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Integer && candidate.Value.IntegerValue == encoded)
                        .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
                }

                if (shape != AnswerShape.Rune || choice.ValueVocabulary != EffectChoiceValueVocabulary.None ||
                    choice.NodeId.Value != "choose-rune" || choice.Candidates.Any(candidate =>
                        candidate.Kind != EffectChoiceCandidateKind.Value ||
                        candidate.Value.Kind != EffectValueKind.Integer ||
                        candidate.Value.IntegerValue < 1 || candidate.Value.IntegerValue > 4))
                    return null;
                var physical = projection.HeldMarkers.Where(marker =>
                    marker.HolderHeroId.Equals(choice.ChooserId) && marker.TurnSlotIndex.HasValue &&
                    SnorriComponentContent.Runes.Any(definition =>
                        definition.Reference.Equals(marker.Definition.Reference))).ToArray();
                if (physical.Length != 4 || physical.Select(marker => marker.TurnSlotIndex.Value).Distinct().Count() != 4 ||
                    physical.Select(marker => marker.Definition.Reference).Distinct().Count() != 4)
                    return null;
                var ordinal = int.Parse(value, CultureInfo.InvariantCulture);
                return open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                        candidate.Value.Kind == EffectValueKind.Integer && candidate.Value.IntegerValue == ordinal)
                    .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
            }

            private bool TryEvaluateRunes(JsonObject expectation)
            {
                if (!expectation.Has("runes")) return false;
                if (expectation.Count != 1 || !(expectation.Get("runes") is JsonObject spec) ||
                    spec.Count == 0 || spec.Any(pair => pair.Key != "assigned" && pair.Key != "active" &&
                        pair.Key != "slots"))
                    throw Unsupported("runes expectation", Describe(expectation));
                var runes = after.HeldMarkers.Where(marker =>
                    SnorriComponentContent.Runes.Any(definition =>
                        definition.Reference.Equals(marker.Definition.Reference))).ToArray();
                if (spec.Has("assigned"))
                {
                    var wanted = IntOf(spec.Get("assigned"), "runes.assigned");
                    if (runes.Length != wanted) Mismatch(expectation, "assigned " + runes.Length);
                }

                if (spec.Has("slots"))
                {
                    var wanted = RuneOrdinals(spec.Get("slots"), "runes.slots");
                    var actual = Enumerable.Range(1, 4).Select(slot =>
                    {
                        var marker = runes.SingleOrDefault(value => value.TurnSlotIndex == slot);
                        return marker == null ? 0 : RuneOrdinal(marker.Definition.Reference);
                    }).ToArray();
                    if (!actual.SequenceEqual(wanted))
                        Mismatch(expectation, "rune slots are [" + string.Join(",", actual) + "]");
                }

                if (spec.Has("active"))
                {
                    var wanted = RuneOrdinals(spec.Get("active"), "runes.active");
                    var actual = runes.Where(marker => marker.IsActiveInTurn(after.TurnIndex))
                        .Select(marker => RuneOrdinal(marker.Definition.Reference)).OrderBy(value => value).ToArray();
                    if (!actual.SequenceEqual(wanted.OrderBy(value => value)))
                        Mismatch(expectation, "active runes are [" + string.Join(",", actual) + "]");
                }

                return true;
            }

            private int[] RuneOrdinals(object value, string phrase)
            {
                if (!(value is List<object> list) || list.Any(item => !(item is double)))
                    throw Unsupported("runes expectation", phrase + ": " + Describe(value));
                var ordinals = list.Select(item => IntOf(item, phrase)).ToArray();
                if (ordinals.Any(item => item < 1 || item > 4) || ordinals.Distinct().Count() != ordinals.Length)
                    throw Unsupported("runes expectation", phrase + ": " + Describe(value));
                return ordinals;
            }

            private static int RuneOrdinal(GuardsDigital.Core.Components.MarkerDefinitionReference reference)
            {
                for (var index = 0; index < SnorriComponentContent.Runes.Length; index++)
                    if (SnorriComponentContent.Runes[index].Reference.Equals(reference)) return index + 1;
                return 0;
            }
        }
    }
}
