using System.Runtime.CompilerServices;

// The aggregate's commit boundary is internal, and its contract - a refused operation leaves no
// partial state - is exactly what the Edit Mode fixtures need to assert. Milestone 6.1o asserted it
// reflectively, through an expression tree and a private nested type named in a string literal,
// which bypasses the same boundary with none of the compile safety: a rename left the test passing
// against nothing. This grants the test assembly what it was already taking, and takes it back under
// the type system.
//
// Scope is deliberately narrow. `GuardsDigital.Core` stays closed, so the three internal strategy
// backends and `MinionBattleRules.Plan` are still named as milestone 6.1k named them, and the
// reasons 6.1k recorded for that remain true.
//
// This adds no assembly reference. `ApplicationAssemblyDependencyPolicy` reads
// `Assembly.GetReferencedAssemblies()`, so the rule that keeps Core and Application free of Unity
// sees exactly what it saw before.
[assembly: InternalsVisibleTo("GuardsDigital.EditModeTests")]
