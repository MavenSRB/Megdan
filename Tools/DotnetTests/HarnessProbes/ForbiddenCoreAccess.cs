// Deliberate compiler violation, included only by the boundary verification script.
// Core does not grant its internals to the Edit Mode test assembly.
namespace GuardsDigital.Tests.Harness
{
    internal static class ForbiddenCoreAccess
    {
        internal static object Read() => default(GuardsDigital.Core.Snapshots.CompletedRoundsFragment);
    }
}
