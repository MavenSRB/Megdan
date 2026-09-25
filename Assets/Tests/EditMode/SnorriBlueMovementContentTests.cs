using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueMovementContentTests
    {
        [Test]
        public void ThreePassageFacesHaveExactPrintedIconsAndRuneGatedText()
        {
            var faces = SnorriBlueMovementFaces.Definitions;
            Assert.That(faces.Count, Is.EqualTo(3));
            Assert.That(faces.Select(face => face.Initiative.Value),
                Is.EqualTo(new[] { 9, 9, 10 }));
            Assert.That(faces.Select(face => face.Progression.ItemIcon),
                Is.EqualTo(new[]
                {
                    PrintedValueChannel.None,
                    PrintedValueChannel.Attack,
                    PrintedValueChannel.Range
                }));
            Assert.That(faces.Select(face => face.ActionDefinitions
                .Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount),
                Is.EqualTo(new[] { 5, 6, 6 }));
            foreach (var face in faces)
            {
                Assert.That(face.ActionDefinitions.Count, Is.EqualTo(2),
                    "None of the Passage fronts prints a separate ribbon Movement.");
                var movement = face.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Movement);
                Assert.That(movement.Origin,
                    Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
                Assert.That(movement.BaseValues.Single().Amount, Is.EqualTo(3));
                Assert.That(movement.CardTextTiming,
                    Is.EqualTo(CardTextTiming.BeforeTheMovement));
            }
        }

        [Test]
        public void EachPassageEffectGraphBindsARegisteredTypedCreation()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                SnorriComponentContent.Handlers()
                    .Concat(SnorriBlueActiveEffectContent.Handlers()));
            foreach (var face in SnorriBlueMovementFaces.Definitions)
            {
                var movement = face.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Movement);
                var errors = EffectDefinitionValidator.ValidateCardText(
                    movement.EffectRoot, catalog, movement.CardTextTiming);
                Assert.That(errors, Is.Empty,
                    face.DisplayName + ": " + string.Join("; ", errors));
                var gates = movement.EffectRoot.Nodes
                    .Where(node => node.Kind == EffectNodeKind.Conditional)
                    .ToArray();
                Assert.That(gates.All(node =>
                    node.Capability.Equals(SnorriComponentContent.ActiveRuneV1)), Is.True);
                Assert.That(gates.Length,
                    Is.EqualTo(face.Id.Equals(SnorriBlueMovementFaces.SafePassage)
                        ? 1 : face.Id.Equals(SnorriBlueMovementFaces.HiddenPassage)
                            ? 2 : 3));
            }
        }
    }
}
