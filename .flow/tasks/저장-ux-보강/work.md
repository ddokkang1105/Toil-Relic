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

## Review rework: complete save contract validation

### Result

Resolved all three P1 findings from the second review without changing the production save shape. Console saves now satisfy stable value invariants before normalization; Unity checks stable field presence before hydrated defaults can hide omissions; Unity continues to load version-1 and historical versionless envelopes while current saves remain explicit version 2.

### Framework and execution route

- Personal Flow routed implementation to active `compound-engineering:ce-work` in return-to-caller mode; Personal Flow retains the review and QA tail.
- Framework probe: CE and gstack available, OpenSpec CLI available without project artifacts, OMX unavailable.
- No live or checkout cross-model engine preference was configured. Execution used native Codex serial subagents in the shared workspace, with the host owning authoritative tests and commits.
- Existing feature branch `codex/save-ux-reliability` was continued as the directly related task branch.

### Implementation

- U1: Added post-deserialization validation for HP bounds, positive max HP/level, nonnegative experience/treasure/inventory values, and defined `ItemType` keys. Empty inventory and optional equipment fields remain compatible.
- U1 coverage: Added per-field presence/kind matrices, eight isolated invalid-value cases, and a real Title-flow fixture proving the approved diagnosis, disabled Continue, and byte preservation.
- U3: Added a private sentinel-based `JsonUtility.FromJsonOverwrite` probe so missing `maxHp`, `hp`, `level`, `experience`, `treasureCount`, or `inventory` cannot be mistaken for hydrated defaults.
- U3 compatibility: Accept versionless `0`, legacy version `1`, and current version `2`; continue rejecting unknown versions such as `999`; keep writes explicit version 2.
- U3 coverage: Added version-1/versionless normalization and Continue flows, per-field omission checks, isolated value-invariant checks, and partial-current-save Title diagnosis.
- Simplification: Applied three quality fixes (private nested probes, typed console invalid-case data, shared Unity unreadable assertion helper); reuse 0, quality 3, efficiency 0. Skipped one single-deserialization DTO rewrite because it widened the conversion boundary and could not guarantee exact behavior preservation.

### Proof-first evidence

- Console red: all eight `Load_ImpossibleCoreValue_ReturnsUnreadableWithoutChangingBytes` cases returned `Loaded`; the real Title-flow fixture lacked the approved unreadable diagnosis before production changes.
- Unity red: `work-rework-u3-contract-red2-results.xml` ran 6 scenarios with 2 passing and 4 failing. Stable scalar omissions returned `Loaded`, the partial save emitted no load-failure diagnosis, and version 1/versionless fixtures were rejected as unsupported.

### Verification evidence

| Layer | Result | Evidence |
|---|---|---|
| Console build | Passed with 0 warnings and 0 errors | `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore` |
| Console tests | 39 passed, 0 failed | `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore` |
| Unity full PlayMode | 35 passed, 0 failed, 1 optional capture skipped | `work-rework-u3-contract-full-results.xml` |
| Unity post-simplification focus | 9 passed, 0 failed | `work-rework-u3-contract-simplify-results.xml` |
| Diff hygiene | Passed | `git diff --check 3cd3b96` after normalizing generated XML trailing whitespace |

### Commits

- `75c0e62` - Reject impossible console save values
- `c386fa6` - Validate Unity save contracts before loading

### CE work return envelope

- Status: `complete`; plan: `docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md`; SHA-256: `68A4F261571E139127A7D6CB9719C03104B82604CE33E97DF33B098CFBB0001C`.
- Attempted/completed units: U1, U3. Behavior-bearing work has proof-first and final verification evidence above.
- Implementation engine binding: `null`; requested/actual route: native Codex serial subagents; requested/actual model: default / unverified; external run ID and recovery path: `null`.
- Fallback reason: `null`; blockers: none; settled-decision conflicts: none; standalone shipping skipped: `true`.

### Remaining review scope

- The three P1 implementation blockers are resolved and ready for focused re-review.
- Runtime QA has not been advanced; Personal Flow review must clear before QA.

## Review rework: close remaining save-contract boundaries

### Result

Resolved the two P1 findings from the focused re-review. Console load validation now rejects experience at or above the current level threshold before `Player.FromSaveData` can clamp it. Unity version 0 now accepts either the modern required fields or the authentic original `score`-based field set from commit `8b0be99`, while versions 1 and 2 remain strict and current writes remain version 2.

### Framework and execution route

- Personal Flow routed implementation to active `compound-engineering:ce-work` in return-to-caller mode; Personal Flow retains the review and QA tail.
- No live implementation-engine preference or `.compound-engineering/config.local.yaml` was present. Execution used native Codex serial subagents in the shared workspace, with the host owning authoritative verification and canonical commits.
- `compound-engineering:ce-simplify-code` reviewed the final human-authored diff for reuse, quality, and efficiency while preserving the plan's settled structure pins. It found no behavior-preserving improvement to apply.

### Implementation

- U1: Exposed the canonical console `Player.RequiredExperience` calculation to the assembly and reused it in `SaveSystem` so a persisted value must satisfy `0 <= Experience < RequiredExperience(Level)` before materialization.
- U1 coverage: Added the exact Level 1 / Experience 20 boundary to the direct invalid-value matrix and strengthened the real Title-flow fixture to prove the approved unreadable diagnosis, unavailable Continue, and byte preservation.
- U3: Added `score` to the Unity presence probe and split modern from historical required-field sets. Version 0 accepts either set; versions 1 and 2 continue to require the modern set.
- U3 coverage: Replaced the fabricated modern-without-version fixture with the authentic original JSON shape from `8b0be99`. The real scene flow proves load diagnosis, Continue, default level/experience, representative HP/inventory/treasure values, starter-weapon normalization, Camp entry, and unchanged bytes.
- Preserved constraints: no serialized write-shape change, no score-to-XP conversion, no migration framework, no UI/layout change, and no broadening into unverified wrong-kind `JsonUtility` behavior.

### Proof-first evidence

- Console red: the focused direct-load and Title command ran 10 cases with 8 passing and 2 failing. Level 1 / Experience 20 returned `Loaded`, and Title did not show the approved unreadable diagnosis before production changes.
- Console green: the same focused command passed 10/10 after implementation; current-format and equipment-optional compatibility checks passed 2/2.
- Unity red: `P0_AuthenticVersionlessScoreSaveLoadsAndNormalizes` failed 1/1 before production changes with `The save did not contain all required player fields.`
- Unity green: the authentic versionless, version-1, current required-field, and unsupported-version scenarios passed 4/4.

### Verification evidence

| Layer | Result | Evidence |
|---|---|---|
| Console build | Passed with 0 warnings and 0 errors | `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore` |
| Console full tests | 40 passed, 0 failed | `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore` |
| Unity focused save contract | 4 passed, 0 failed | `work-rework-u3-versionless-focused-results.xml` |
| Unity full PlayMode | 35 passed, 0 failed, 1 optional layout capture skipped | `work-rework-u3-versionless-full-results.xml`; capture requires `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` |
| Simplification | Applied 0: reuse 0, quality 0, efficiency 0; skipped 0 | Three independent behavior-preservation lenses found no actionable change |
| Diff hygiene | Passed | `git diff --check cb7243a..HEAD` |

### Commits

- `83a724f` - Reject oversized console save experience
- `c1f5648` - Load authentic versionless Unity saves

### CE work return envelope

- Status: `complete`; plan: `docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md`; SHA-256: `68A4F261571E139127A7D6CB9719C03104B82604CE33E97DF33B098CFBB0001C`.
- Attempted/completed units: U1, U3. Both behavior-bearing units have proof-first and final verification evidence above.
- Changed files: `src/ToilRelic/Models/Player.cs`, `src/ToilRelic/Systems/SaveSystem.cs`, `tests/ToilRelic.Tests/SaveSystemTests.cs`, `tests/ToilRelic.Tests/GameSaveUxTests.cs`, `unity/Assets/Scripts/Save/SaveService.cs`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`, and the two Unity result XML files listed above.
- Implementation engine binding: `null`; requested/actual route: native Codex shared-workspace serial subagents; requested model: default; actual model: `unverified`.
- Source kind: `plan`; external run ID, fallback reason, recovery path, and plan checkpoint: `null`.
- Unit receipts: U1 integrated and committed as `83a724f` after host console verification; U3 integrated and committed as `c1f5648` after host focused and full Unity verification.
- Blockers: none; settled-decision conflicts: none; behavior change: `true`; standalone shipping skipped: `true`.

### Remaining review scope

- Both focused P1 blockers are implemented and ready for another report-only review.
- Runtime QA remains gated on that review; this work stage does not advance directly to QA.
