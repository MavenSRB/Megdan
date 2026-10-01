using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.1. Every printed face this build deals, beside every effect body it composes -
    /// derived from the roster a picker offers, never from a hand-kept list.
    ///
    /// <para><b>Why derived.</b> Milestone 8.83's sweep named its twenty-two heroes one line each,
    /// so a hero dealt after it was audited only if somebody remembered to add a line. This walks
    /// <see cref="QuickGameSetupConfiguration.DealtHeroes"/> through the same factory a match deals
    /// with, so a new hero is in the corpus the moment it is pickable, and
    /// <see cref="PrintedWordingCorpusTests"/> fails if any dealt hero contributes nothing.</para>
    ///
    /// <para><b>What a face composes.</b> Its actions' card-text bodies; every Active effect those
    /// bodies raise, and that effect's own trigger body (the p.17 "End of turn:" / "End of round:"
    /// half of the sentence, which milestone 8.117 found no audit walked); and its Ultimate's
    /// triggered effect. Bodies reached from more than one face are listed once per face that
    /// prints the words.</para>
    ///
    /// <para><b>Printed text is read here and nowhere in production.</b>
    /// <see cref="CardSourceContent"/> stays inert to rules; only this test assembly compares it to
    /// the composition.</para>
    /// </summary>
    internal static class PrintedWordingCorpus
    {
        private static readonly Lazy<IReadOnlyList<PrintedFace>> Faces =
            new Lazy<IReadOnlyList<PrintedFace>>(Compose);

        internal static IReadOnlyList<PrintedFace> All => Faces.Value;

        private static IReadOnlyList<PrintedFace> Compose()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var faces = new List<PrintedFace>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var hero in QuickGameSetupConfiguration.DealtHeroes)
            {
                var hand = MatchConfiguration.CreateVerifiedHandForInternalTest(
                    hero.HeroIdValue, "wording-" + hero.ContentId.Value);
                foreach (var card in hand.Definitions)
                {
                    if (card.ContentAuthority != CardContentAuthority.VerifiedOfficial ||
                        !seen.Add(card.Id.Value))
                    {
                        continue;
                    }

                    var raised = new List<ActiveEffectDefinition>();
                    var bodies = Bodies(card, catalogs, raised).ToArray();
                    faces.Add(new PrintedFace(hero.ContentId.Value, card, bodies, raised));
                }
            }

            // Milestone 9.30d held Gydion's reuse spells to every family before his dealing through a
            // list of its own. Since 9.30i his hand's definitions carry all twenty-two spells beside his
            // fronts (they lie in his book), so the walk above reaches every one of them.
            return faces;
        }

        private static IEnumerable<EffectDefinition> Bodies(
            CardDefinition card, MatchContentCatalogs catalogs, ICollection<ActiveEffectDefinition> raised)
        {
            var roots = new List<EffectDefinition>();
            foreach (var action in card.ActionDefinitions)
            {
                if (action.EffectRoot != null) roots.Add(action.EffectRoot);
                if (action.SecondCardTextRoot != null) roots.Add(action.SecondCardTextRoot);
            }

            if (card.UltimateRule?.TriggeredEffect is EffectDefinitionReference ultimate &&
                catalogs.TriggeredEffects.TryGet(ultimate, out var ultimateBody))
            {
                roots.Add(ultimateBody);
            }

            var walked = new HashSet<EffectDefinitionReference>();
            var pending = new Queue<EffectDefinition>(roots);
            while (pending.Count > 0)
            {
                var body = pending.Dequeue();
                if (!walked.Add(body.Reference)) continue;
                yield return body;

                foreach (var node in body.Nodes.Where(node => node.Kind == EffectNodeKind.Capability))
                {
                    if (!catalogs.Capabilities.TryLoad(node.Capability, out var handler) ||
                        !(handler is ActiveEffectCreationHandler creation))
                    {
                        continue;
                    }

                    foreach (var effect in creation.ActiveEffectDefinitions)
                    {
                        raised.Add(effect);
                        if (effect.TriggeredEffect is EffectDefinitionReference trigger &&
                            catalogs.TriggeredEffects.TryGet(trigger, out var triggerBody))
                        {
                            pending.Enqueue(triggerBody);
                        }
                    }
                }
            }
        }
    }

    /// <summary>One dealt face: its hero, its definition, its printed words and every body it runs.</summary>
    internal sealed class PrintedFace
    {
        internal PrintedFace(
            string hero,
            CardDefinition card,
            IReadOnlyList<EffectDefinition> bodies,
            IReadOnlyList<ActiveEffectDefinition> raisedEffects)
        {
            Hero = hero;
            Card = card;
            Bodies = bodies;
            RaisedEffects = raisedEffects;
        }

        internal string Hero { get; }

        internal CardDefinition Card { get; }

        internal string Text => Card.SourceContent.PrintedText ?? string.Empty;

        internal IReadOnlyList<EffectDefinition> Bodies { get; }

        internal IEnumerable<EffectNodeDefinition> Nodes => Bodies.SelectMany(body => body.Nodes);

        /// <summary>The Active effects this face's bodies raise.</summary>
        internal IReadOnlyList<ActiveEffectDefinition> RaisedEffects { get; }

        /// <summary>
        /// Every rule change the face declares: its raised Active effects' and its Ultimate's
        /// standing modifications.
        /// </summary>
        internal IEnumerable<RuleModification> Modifications =>
            RaisedEffects.SelectMany(effect => effect.Modifications)
                .Concat(Card.UltimateRule?.Modifications ?? Array.Empty<RuleModification>());

        public override string ToString() => $"{Hero} | {Card.Id.Value}";
    }
}
