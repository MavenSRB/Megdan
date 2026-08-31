using System.Runtime.CompilerServices;

// Milestone 8.30g keeps the drag gesture state internal to Presentation while allowing the Edit Mode
// assembly to prove its threshold, one-pointer, cancellation, and exactly-once state transitions with
// compile-time names. This adds no runtime assembly dependency and grants no access to Play Mode.
[assembly: InternalsVisibleTo("GuardsDigital.EditModeTests")]
