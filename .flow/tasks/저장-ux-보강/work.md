# Work

## Result

Implemented the approved save UX reliability plan across the console and Unity runtimes. Missing, loaded, and unreadable saves are now distinct; automatic save success, failure, and recovery are visible without replacing gameplay feedback; unreadable data remains protected until New Game authorizes replacement.

## Implementation

- U1: Added typed console load/save/delete outcomes, developer-only diagnostics, atomic same-directory replacement, and an isolated .NET 8 xUnit project.
- U2: Routed console Title through the typed diagnosis, preserved unreadable bytes on replacement failure, kept all existing autosave triggers including Quit, and added exact success/failure/recovery ordering tests.
- U3: Added Unity-local typed outcomes and one whole-path override covering load, save, delete, and classification before scene lifecycle entry.
- U4: Replaced raw-error UI events with semantic save feedback. Gameplay text, auxiliary save state, and persistent failure warning remain independently composed; the next successful autosave clears only the warning.
- U5: Added and wired `SaveStatusText`, expanded `GameStatus` to 120px, moved Title/Camp panels to preserve a 16px gap, compacted BattlePanel to remove the 16:9 upper-right overlap, regenerated `SampleScene`, and captured both required viewports.
- Simplification: removed duplicate state and dead APIs, eliminated redundant file existence probes, reused scene/test sources of truth, and made Unity fixture cleanup safe after setup failure.

## Verification evidence

| Layer | Result | Evidence |
|---|---|---|
| Console build | Passed with 0 warnings and 0 errors | `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore` |
| Console tests | 13 passed, 0 failed | `tests/ToilRelic.Tests`; missing/loaded/unreadable, read/write/delete failure, Title routing, recovery, and Quit |
| Unity U3 | 23 passed, 0 failed, 1 optional capture skipped | `work-u3-results.xml` |
| Unity U4 | 26 passed, 0 failed, 1 optional capture skipped | `work-u4-results.xml` |
| Unity final layout | 28 passed, 0 failed with capture enabled | `work-u5-results.xml` |
| Simplification regression | 27 passed, 0 failed, 1 optional capture skipped | `work-simplify-results.xml` |
| Visual evidence | Title hidden state, Camp success/failure, and Battle failure at 800x600 and 1280x720 | `layout-evidence/` |

## Visual observations

- Title shows only the matching load diagnosis and keeps the auxiliary row hidden.
- Camp keeps the action result primary while the separate row shows `Save: Saved just now` or `Save: Failed`.
- Failure warnings remain readable as a second/third logical line and disappear on successful recovery.
- Battle hides the auxiliary row, retains the safe warning, and remains separated from the upper-right status text at both viewports.

## Commits

- `f00c506` — Add typed console save outcomes
- `446e050` — Improve console save feedback flow
- `c3d5fb4` — Improve Unity save load diagnosis
- `810e078` — Show semantic Unity save feedback
- `4c0b993` — Add Unity save status layout
- `1fbd5cd` — Simplify save UX implementation

## Residual scope

- No implementation blocker remains for review.
- Atomic last-known-good replacement for Unity and explicit manual save controls remain documented follow-ups rather than implicit scope expansion.

## Review rework: structural save validation

### Result

Resolved the review P1 without changing either serialized save shape. Parseable data must now satisfy the stable console core shape or Unity version-2/core-state contract before it can become `Loaded`; rejected bytes remain untouched and the existing Title path keeps Continue unavailable.

### Implementation

- U1: Console validates the seven fields present since the first `PlayerSaveData` format while leaving later equipment fields optional. `{}` is now `Unreadable`, and the real Title flow shows only the approved New Game diagnosis.
- U3: Unity writes the envelope version explicitly, rejects unsupported versions, and validates core `PlayerState` invariants before `InitDefaults` can normalize corrupt data. Version-2 saves without optional equipment fields still load and normalize starter equipment.
- Simplification: reused the Unity unreadable-Title assertion contract, removed redundant xUnit result checks, and removed an unreachable post-shape-validation null branch. Reuse: 1; quality: 2; efficiency: 0; skipped findings: 0.

### Proof-first evidence

- Console red: `Load_EmptyJsonObject_ReturnsUnreadableWithoutChangingBytes` failed with expected `Unreadable`, actual `Loaded` before implementation.
- Unity red: `{"version":2,"player":{}}` did not emit the expected `Save load failed` diagnostic before implementation, proving it was incorrectly accepted.

### Verification evidence

| Layer | Result | Evidence |
|---|---|---|
| Console build | Passed with 0 warnings and 0 errors | `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore` |
| Console tests | 16 passed, 0 failed | `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore` |
| Unity focused validation | 5 passed, 0 failed | `work-rework-u3-results.xml`; valid v2, legacy optional fields, malformed, structural invalidity, unsupported version |
| Unity full PlayMode | 30 passed, 0 failed, 1 optional capture skipped | `work-rework-full-results.xml`; capture test requires `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` |
| Diff hygiene | Passed | `git diff --check` |

### Rework commits

- `0986e81` - Reject structurally invalid console saves
- `614d175` - Reject structurally invalid Unity saves
- `0c871c5` - Simplify save validation coverage

### Remaining review scope

- The P1 implementation blocker is resolved and ready for re-review.
- Previously recorded advisory risks and broader E2E gaps remain review inputs; this rework did not expand into atomic Unity writes, manual save controls, or unrelated documentation refresh.
