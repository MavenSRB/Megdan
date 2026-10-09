using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public interface ICardValueReadModel
    {
        /// <summary>Visible, current nonzero deltas. A missing channel means no displayed modifier.</summary>
        IReadOnlyList<CardValuePreview> CardValues { get; }
    }

    public static class CardValueReadModel
    {
        public static IReadOnlyList<CardValuePreview> ForCard(IMatchReadModel source,
            HeroId owner, CardDefinitionReference definition) =>
            source is ICardValueReadModel values
                ? Array.AsReadOnly(values.CardValues.Where(value => value.Owner.Equals(owner) &&
                    value.Definition.Equals(definition)).ToArray())
                : Array.Empty<CardValuePreview>();
    }
}
