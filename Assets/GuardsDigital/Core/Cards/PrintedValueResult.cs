using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public sealed class PrintedValueResult
    {
        public PrintedValueResult(
            PrintedValueChannel channel,
            IEnumerable<PrintedValueContribution> contributions)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("Printed value result requires a channel.", nameof(channel));
            }

            if (contributions == null)
            {
                throw new ArgumentNullException(nameof(contributions));
            }

            Channel = channel;
            Contributions = Array.AsReadOnly(contributions.ToArray());
            Total = Contributions.Sum(contribution => contribution.Amount);
        }

        public PrintedValueChannel Channel { get; }

        public IReadOnlyList<PrintedValueContribution> Contributions { get; }

        public int Total { get; }
    }
}
