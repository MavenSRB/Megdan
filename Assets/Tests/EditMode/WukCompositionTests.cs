using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114c. Every node Wuk's composed faces declare binds every REQUIRED parameter its
    /// capability declares.
    ///
    /// <para>The offer walk (6.5af, 8.35l) judges a node it cannot decide as performable, so a
    /// selection missing a required input is not refused - the face is offered, starts, and ends having
    /// done nothing. 8.114c measured exactly that: Gifts of Nature's Tree selection left the object
    /// arm's <c>includeSelf</c> unbound and was offered with no Tree in reach. This asserts the whole
    /// set at once so the next face cannot repeat it.</para>
    /// </summary>
    public sealed class WukCompositionTests
    {
        [Test]
        public void EveryWukNode_BindsEveryRequiredParameterOfItsCapability()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(WukComponentContent.RegisteredHandlers());
            var missing = new List<string>();
            foreach (var card in WukCardContent.AllDefinitions)
            {
                foreach (var root in card.ActionDefinitions.SelectMany(action =>
                             new[] { action.EffectRoot, action.SecondCardTextRoot }).Where(root => root != null))
                {
                    foreach (var node in root.Nodes.Where(node => node.Kind == EffectNodeKind.Capability))
                    {
                        Assert.That(catalog.TryLoad(node.Capability, out var handler), Is.True,
                            $"{card.Id.Value}/{node.Id.Value} names {node.Capability}, which nothing registers");
                        var bound = new HashSet<string>(node.Inputs.Select(input => input.ParameterName));
                        missing.AddRange(handler.Schema.Parameters
                            .Where(parameter => parameter.IsRequired && !bound.Contains(parameter.Name))
                            .Select(parameter => $"{card.Id.Value}/{node.Id.Value}: {parameter.Name}"));
                    }
                }
            }

            Assert.That(missing, Is.Empty,
                "an unbound required parameter makes the node undecidable, and the offer walk then " +
                "offers a face that can do nothing.");
        }
    }
}
