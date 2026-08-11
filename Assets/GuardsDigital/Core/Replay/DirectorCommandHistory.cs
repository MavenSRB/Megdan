using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Replay
{
    public sealed class DirectorCommandHistory
    {
        private readonly List<DirectorCommand> commands = new List<DirectorCommand>();

        public IReadOnlyList<DirectorCommand> Commands => commands.AsReadOnly();
        public int Count => commands.Count;

        public void Append(DirectorCommand command)
        {
            commands.Add(command ?? throw new ArgumentNullException(nameof(command)));
        }

        public IReadOnlyList<DirectorCommand> WithoutLatest()
        {
            return commands.Count == 0
                ? Array.Empty<DirectorCommand>()
                : Array.AsReadOnly(commands.Take(commands.Count - 1).ToArray());
        }

        public void ReplaceWith(IEnumerable<DirectorCommand> replacement)
        {
            var values = replacement?.ToArray() ?? throw new ArgumentNullException(nameof(replacement));
            if (values.Any(command => command == null))
            {
                throw new ArgumentException("Director command history cannot contain null entries.", nameof(replacement));
            }

            commands.Clear();
            commands.AddRange(values);
        }

        public void Clear() => commands.Clear();
    }
}
