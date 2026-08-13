# Compile probes

These files are **not** compiled. They live outside `Assets/`, so Unity never imports
them. Each probe is copied into the project by hand, compiled once to capture the
compiler's answer, and removed again.

A probe exists because some contracts are enforced by the compiler rather than by a test.
A test can only assert that correct code behaves correctly; it cannot assert that
incorrect code fails to build. That distinction is the entire point of milestone 6.1g.

## `IncompleteRestore.*.cs.probe` - finding `F613-07`

The probes demonstrate one act: **restore an authority while omitting one persisted
field.** Before milestone 6.1g that act compiled cleanly and silently dropped the field
on every restore, replay, and Undo. After 6.1g it does not compile.

The two probe files are deliberately **different sources**, because the signature under
test is exactly what the milestone changed. They are the same act expressed against each
revision's API, not the same text run twice:

| Probe | Revision | Expression of the act | Expected result |
| --- | --- | --- | --- |
| `IncompleteRestore.PreFix.cs.probe` | before 6.1g | `RoundResolutionAuthority.Restore(...)` with the four defaulted parameters omitted | **compiles cleanly** |
| `IncompleteRestore.PostFix.cs.probe` | 6.1g onward | `RoundResolutionRestoreState` constructed without its pending-continuation state | **CS7036** |

The pre-fix probe compiling is the *failing* half of the mechanical closure transition.
The defect is that it builds.

`RoundResolutionAuthority.Restore` is `internal` and `GuardsDigital.Core` declares no
`InternalsVisibleTo`, so a probe must be copied into the Core assembly itself, not into
the test assembly. Do not add `InternalsVisibleTo` to make a probe more convenient - the
assembly boundary is a contract, and the probe is temporary.

## Procedure

1. Copy the probe into `Assets/GuardsDigital/Core/` as `__CompileProbe.cs`.
2. Write `tmp/unity-validation-request.json` and run the fixed launcher:

   ```powershell
   & 'C:\Projects\Guards Digital\Guards 0.1\Tools\Invoke-UnityValidation.ps1'
   ```

   Any focused Edit Mode filter works; the probe is answered at compile time, before test
   discovery. Read the compiler result from the run's log.
3. Record the exact revision, command, and compiler output.
4. Delete `Assets/GuardsDigital/Core/__CompileProbe.cs` and its generated `.meta`.

Never leave a probe in `Assets/`. A probe that stays behind is either a build break or,
worse, code that silently drops authoritative state.
