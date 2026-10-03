using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    internal static partial class ExpectedOutcomeExecutor
    {
        private sealed partial class CaseRun
        {
            private readonly Dictionary<HeroId, HeroId> actionChoosers = new Dictionary<HeroId, HeroId>();

            private TeamSide ReadCoinSide(object value)
            {
                if (value is string text)
                {
                    if (string.Equals(text, "Red", StringComparison.OrdinalIgnoreCase)) return TeamSide.Red;
                    if (string.Equals(text, "Blue", StringComparison.OrdinalIgnoreCase)) return TeamSide.Blue;
                }
                throw Unsupported("tie-breaker", Describe(value));
            }

            private bool TryEvaluateBatchEFacts(JsonObject expectation)
            {
                if (expectation.Has("tie-breaker"))
                {
                    if (expectation.Count != 1) throw Unsupported("tie-breaker expectation", Describe(expectation));
                    var wanted = ReadCoinSide(expectation.Get("tie-breaker"));
                    if (after.TieBreakerState.RevealedSide != wanted)
                        Mismatch(expectation, "Tie Breaker is " + after.TieBreakerState.RevealedSide);
                    return true;
                }
                if (expectation.Has("unresolved-initiative"))
                {
                    if (expectation.Count != 2 || !(expectation.Get("ref") is string reference))
                        throw Unsupported("unresolved-initiative expectation", Describe(expectation));
                    var owner = HeroOfRef(reference);
                    var cards = after.Cards.Where(card => card.OwnerId.Equals(owner) &&
                        card.State.Location == CardLocation.TurnArea &&
                        card.State.ResolutionStatus == CardResolutionStatus.Unresolved).ToArray();
                    if (cards.Length != 1) Mismatch(expectation, "no unique unresolved turn card");
                    else
                    {
                        var actual = PrintedValueQuery.CalculatePrintedInitiative(cards[0], after.Definitions).Total;
                        if (actual != IntOf(expectation.Get("unresolved-initiative"), "unresolved-initiative"))
                            Mismatch(expectation, "printed Initiative is " + actual);
                    }
                    return true;
                }
                if (expectation.Has("action-chosen-by"))
                {
                    if (expectation.Count != 2 || !(expectation.Get("ref") is string reference) ||
                        !(expectation.Get("action-chosen-by") is string chooser))
                        throw Unsupported("action-chosen-by expectation", Describe(expectation));
                    var wanted = HeroOfRef(chooser);
                    if (!wanted.IsDefined) throw Unsupported("action chooser", chooser);
                    if (!actionChoosers.TryGetValue(HeroOfRef(reference), out var actual))
                        Mismatch(expectation, "no accepted action start was observed for " + reference);
                    else if (!actual.Equals(wanted))
                        Mismatch(expectation, "action was chosen by " + RefOfHero(actual));
                    return true;
                }
                return false;
            }
        }
    }
}
