using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Application
{
    public static class ApplicationAssemblyDependencyPolicy
    {
        public static IReadOnlyList<string> FindForbiddenReferences(IEnumerable<string> assemblyNames)
        {
            if (assemblyNames == null) throw new ArgumentNullException(nameof(assemblyNames));
            return Array.AsReadOnly(assemblyNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => name.Equals("UnityEngine", StringComparison.Ordinal) ||
                    name.StartsWith("UnityEngine.", StringComparison.Ordinal) ||
                    name.Equals("UnityEditor", StringComparison.Ordinal) ||
                    name.StartsWith("UnityEditor.", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
        }
    }
}
