using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Diagnostics;

namespace GuardsDigital.Core.Replay
{
    /// <summary>
    /// The accepted-command history, held as an append-only log so reading it costs nothing however
    /// long the match is.
    /// </summary>
    /// <remarks>
    /// Milestone 7.9b. `RISK-PERF-001`'s measured growth term included every projection refresh
    /// copying this history; the log's constant-time snapshots are the same answer the two
    /// rule-event streams gave the same problem at 6.1o, applied to the third growing sequence a
    /// refresh hands over. <see cref="Commands"/> and <see cref="WithoutLatest"/> are immutable
    /// values: an append writes past the end of every outstanding snapshot's visible range, and a
    /// replacement leaves the old buffer to whoever still reads it.
    /// </remarks>
    public sealed class DirectorCommandHistory
    {
        private readonly AppendOnlyEventLog<DirectorCommand> commands =
            new AppendOnlyEventLog<DirectorCommand>();

        public IReadOnlyList<DirectorCommand> Commands => commands.Entries;
        public int Count => commands.Count;

        public void Append(DirectorCommand command)
        {
            commands.Add(command ?? throw new ArgumentNullException(nameof(command)));
        }

        public IReadOnlyList<DirectorCommand> WithoutLatest()
        {
            return commands.Count == 0
                ? commands.Entries
                : commands.PrefixEntries(commands.Count - 1);
        }

        public void ReplaceWith(IEnumerable<DirectorCommand> replacement)
        {
            var values = replacement?.ToArray() ?? throw new ArgumentNullException(nameof(replacement));
            if (values.Any(command => command == null))
            {
                throw new ArgumentException("Director command history cannot contain null entries.", nameof(replacement));
            }

            commands.Reset(values);
        }

        public void Clear() => commands.Reset();
    }
}
