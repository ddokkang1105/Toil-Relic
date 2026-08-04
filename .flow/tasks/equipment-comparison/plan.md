# Plan

## Readiness

- Canonical plan: `docs/plans/2026-07-28-001-feat-equipment-comparison-plan.md`
- The unified plan is structurally implementation-ready and has completed confidence deepening plus a non-interactive document review.
- All five review items were approved through the repeated continue command and applied to the canonical plan.
- No material planning question remains. The plan is executable, so Personal Flow advances to `work`.

## Steps

1. [x] U1 — Add mirrored, side-effect-free comparison and eligibility contracts plus shared parity vectors.
2. [x] U2 — Replace console immediate equip with slot-first preview, explicit confirmation, and existing save semantics.
3. [x] U3 — Add the Unity Camp-local equipment controller and typed command outcomes without adding a top-level game state.
4. [x] U4 — Regenerate the scene, wire serialized actions, and capture two-viewport layout evidence. Commit remains a later user-directed shipping action.

## Affected paths

- Console runtime: `src/ToilRelic/Models/`, `src/ToilRelic/Game.cs`, `src/ToilRelic/Util/ConsoleUI.cs`
- Console tests: `tests/ToilRelic.Tests/`
- Unity runtime: `unity/Assets/Scripts/Core/`, `unity/Assets/Scripts/UI/`
- Unity scene and bootstrap: `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`, `unity/Assets/Scenes/SampleScene.unity`
- Unity fixtures and Play Mode tests: `unity/Assets/Tests/`
- Setup guidance: `unity/UNITY_SETUP.md`

## Validation

- Planning checks: Product Contract trace, dependency ordering, event/save lifecycle, scene/bootstrap ownership, and cross-runtime parity coverage.
- Console implementation gates: build, full automated tests, fixture-backed equipment UX tests, and production-data isolated smoke QA.
- Unity implementation gates: repeated targeted Action Contracts, full Play Mode regression, graphics-enabled two-viewport captures, XML/PNG validity checks, and visual inspection.
- Cross-cutting gates: shared parity vectors, global test-state restoration, and `git diff --check`.
- No build, test, or runtime probe was executed during the plan stage.

## Rollback or migration

- No save DTO, version, migration, production equipment definition, or balance change is planned.
- Roll back the mirrored console and Unity comparison/UI changes together so their behavior cannot drift.
- Roll back bootstrap and committed scene changes as one unit.
- Save failure intentionally preserves the in-memory mutation and warning; rollback/retry transaction redesign is outside scope.

## Resolved review items

| Priority | Item | Applied resolution |
|---|---|---|
| P1 decision | Unequip has no candidate, but the authoritative evaluator contract required one. | Added a mirrored candidate-free unequip-eligibility operation with typed occupancy and mandatory-slot outcomes. |
| P1 proposed fix | Rejected confirmations lacked a visible feedback and action-enabled-state contract. | Defined panel-local typed rejection text, deterministic clearing, and fixed Equip/Unequip/Back enablement. |
| P1 proposed fix | Focus and navigation lifecycle were unspecified. | Defined deterministic initial, empty-state, refresh, return focus, and non-pointer navigation order. |
| P1 proposed fix | Isolated built-console QA could not access test-only optional equipment. | Limited the smoke gate to production starter/reward flows; optional equip/unequip remains fixture-backed automation. |
| P2 proposed fix | Minimum control geometry and typography lacked numeric pass thresholds. | Applied 44-pixel interactive height, 16-point text, best-fit-off, and positive-gap floors at both viewports. |

## Work evidence — 2026-08-03

### Implementation units

- U1 added mirrored pure comparison and unequip-eligibility contracts, deterministic read-only catalog enumeration, one Unity-owned parity fixture, and scoped catalog restoration helpers. The initial red runs recorded missing evaluator/catalog contracts; the final console comparison tests passed 3/3 and Unity parity/isolation passed 2/2 in `work-u1-r4-results.xml`.
- U2 replaced console immediate equip with slot-first preview, explicit confirmation, stepwise Back behavior, confirmation-time revalidation, and save-only-on-success semantics. Targeted equipment UX passed 6/6; the final console suite passed 49/49.
- U3 added typed Unity equip/unequip command outcomes plus a Camp-local `EquipmentPanelController`. Preview and rejection remain side-effect free; applied mutations preserve the required player/log/save event order and keep save failure authoritative. Targeted interaction tests passed 4/4 in `work-u3-r2-results.xml`.
- U4 regenerated `SampleScene.unity` from the updated bootstrap, wired the Camp entry and fixed actions, added pointer/non-pointer/layout/bootstrap contracts, updated `UNITY_SETUP.md`, and captured all equipment states at 1280x720 and 800x600. Scene/action checks passed 5/5 in `work-u4-r2-results.xml`; graphics capture passed 1/1 in `work-u4-layout-r3-results.xml`.

### Final verification

- `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore --nologo`: passed with 0 warnings and 0 errors.
- `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore --nologo`: passed 49/49.
- Fresh Unity `PlayModeActionContracts` processes: passed 16/16 twice in `work-action-r1c-results.xml` and `work-action-r2-results.xml`.
- Full Unity Play Mode assembly: 54 total, 53 passed, 0 failed, 1 intentionally ignored in `work-unity-full-results.xml`.
- Isolated production-data console smoke from the OS temporary directory: reward preview/cancel left the canonical save SHA-256 unchanged; confirm changed it; reload displayed and retained `reward-weapon`; the post-reload save hash remained stable.
- Final rendered evidence: eight equipment images under `work-u4-layout-evidence-r3/` were dimension/size/pixel-variance checked and visually inspected. Empty, long-name preview, success, and save-failure states do not overlap the compact HUD or status region at either required viewport.
- `git diff --check`: recorded after artifact updates as the final work-stage hygiene gate.

### Simplification and deviations

- Applied current-diff simplifications: reused candidate index lookup, centralized persistent-action assertions, avoided rebuilding unchanged candidate rows after successful equip/unequip, and guarded no-op uGUI text/button assignments.
- Did not cache `EquipmentCatalog.All`: scoped parity tests intentionally mutate the private production dictionary and require each enumeration to reflect the active isolated fixture. The production catalog is only two items, so preserving the test isolation contract is the safer tradeoff.
- Kept equality text as `±0`; that exact non-color equality signal is required by R4/R13 and is present in the approved rendered evidence.
- Two initial Unity retries inside the filesystem sandbox could not access the local editor entitlement. The same commands passed outside the sandbox with result XMLs generated and parsed.
- The worktree already contained unrelated `equipment-system-foundation` artifacts. They were preserved untouched, and no commit or push was performed during this work stage.

## Review rework queue — 2026-08-03

1. [x] Filter candidates already equipped in another physical slot in both runtimes and add Ring 1 to Ring 2 interaction coverage.
2. [x] Render the required `No equipment stat change (±0)` fallback in both runtimes and assert it.
3. [x] Keep unavailable console Equip/Unequip actions visible but non-executable while preserving stale-confirmation revalidation.
4. [x] Add symmetric Unity Unequip success, rejection, event/save-order, reload, and save-failure tests.

After these items pass targeted and full regression checks, return to the Personal Flow `review` stage.

## Review rework evidence — 2026-08-04

### Implemented fixes

- Candidate discovery in both runtimes now consumes the same typed eligibility reason as comparison, so an ID used in another physical slot is absent while the item in the selected slot remains comparable. The reason-only path avoids allocating full comparison results for list filtering.
- Console and Unity render the exact `No equipment stat change (±0)` fallback for a zero-stat same-item comparison.
- Console Equip and Unequip confirmation menus keep unavailable actions visible but constrain accepted input to `Back`; a valid preview still revalidates immediately before mutation.
- Unity Play Mode coverage now proves optional Unequip success plus reload, mandatory/empty rejection silence, exact player/log/save ordering, and mutation-first save-failure behavior.
- The graphics capture gate now includes the zero-stat same-item state at 1280×720 and 800×600.

### Proof-first and verification evidence

- Red console proof: `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore --nologo --filter GameEquipmentUxTests` failed 2/7 before implementation on the missing `±0` fallback and Ring 1 to Ring 2 candidate exclusion.
- Red Unity proof: `work-rework-red-results.xml` failed 1/1 before implementation because `Current Ring` remained in the Ring 2 candidate list.
- Focused green proof: console equipment UX passed 7/7; `work-rework-targeted-r1-results.xml` passed the four new Unity candidate/fallback/Unequip tests 4/4.
- Final console gates: `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore --nologo` passed with 0 warnings and 0 errors; the full console suite passed 50/50.
- Final Unity stability: `work-rework-action-final-r1-results.xml` and `work-rework-action-final-r2-results.xml` each passed 20/20 in separate fresh processes.
- Final Unity regression: `work-rework-unity-full-final-results.xml` recorded 58 total, 57 passed, 0 failed, and 1 intentionally ignored capture test.
- Rendered evidence: `work-rework-layout-results.xml` passed 1/1. All 18 PNGs under `work-rework-layout-evidence/` had the expected dimensions, non-zero size, and non-uniform pixels. The two zero-stat same-item images were visually inspected and showed the full `±0` fallback without clipping or overlap at either viewport.
- `git diff --check` passed after implementation and evidence generation; the final artifact update is checked once more before handoff.

### Simplification and deviations

- `ce-simplify-code` results: reuse 0 applied, quality 1 applied, efficiency 1 applied, skipped 0. The quality pass added explicit empty-optional Unequip disabled proof; the efficiency pass replaced per-candidate full comparison allocation with a mirrored reason-only validation helper.
- The CE context fences could not start WSL Bash because `Bash/Service/CreateInstance/E_ACCESSDENIED`; both skills continued under their documented fallback behavior.
- The first Unity attempts inside the filesystem sandbox could not access the local editor entitlement. The same commands ran successfully outside the sandbox with the user's approval.
- Existing unrelated `equipment-system-foundation` changes remain untouched. No commit, push, or Git index mutation was performed.

## Review rework queue — 2026-08-04

1. [x] Keep the selected Unity slot row fully visible during keyboard/gamepad navigation, reset the slot viewport deterministically on open/rebuild, and prove the final rows at 1280×720 and 800×600.
2. [x] Add a disposable-scene Edit Mode integration contract that runs `ToilRelicSceneBootstrap` and compares its generated hierarchy, serialized references, persistent actions, and key geometry with the committed scene contract.

The previous four P1 rework items remain verified as resolved. After these two items pass focused action/navigation/bootstrap checks and the full Unity Play Mode regression, return to the Personal Flow `review` stage.

## Second review rework evidence — 2026-08-04

### Implemented fixes

- `EquipmentPanelController` now observes non-pointer focus changes, scrolls a focused slot row fully inside the resolved slot `ScrollRect`, and resets the viewport to the first row whenever slot rows are rebuilt. Rows scheduled for destruction are disabled first so same-frame close/reopen cannot pollute the new layout.
- The Play Mode Action Contract drives real `MoveDirection.Down` events through all 12 slot controls, proves the last row is fully visible, and proves reopening restores first-slot focus and the top viewport position.
- The graphics capture gate now includes `equipment-final-slot-focus` at 1280×720 and 800×600.
- `ToilRelicSceneBootstrap` separates asset/committed-scene persistence from an active-scene builder. A new Edit Mode assembly runs that builder in an unsaved disposable scene and compares the bootstrap-owned hierarchy, controller serialized references, four persistent actions, and key equipment geometry with `SampleScene.unity`; it also verifies the committed scene bytes are unchanged.

### Proof-first and verification evidence

- Red navigation proof: `work-rework-scroll-red-results.xml` failed 1/1 because `Slot_Earring2` remained below the slot viewport. The first implementation exposed a second same-frame reopen defect in `work-rework-scroll-green-results.xml`, where the normalized position was `-0.215686277`; disabling old rows before deferred destruction fixed it.
- Red bootstrap proof: after a compile-only test correction, `work-rework-bootstrap-red-r2-results.xml` failed 1/1 because no disposable active-scene builder existed. The generated/committed comparison then revealed that `Main Camera` is not bootstrap-owned, so the hierarchy contract was correctly narrowed to `GameManager`, `Canvas`, `UIActions`, and `EventSystem` roots.
- Focused green proof: `work-rework-scroll-green-r2-results.xml` passed the navigation contract 1/1, and `work-rework-bootstrap-green-r3-results.xml` passed the disposable-scene integration contract 1/1.
- Final Action Contract stability: `work-rework-scroll-action-r1-results.xml` and `work-rework-scroll-action-r2-results.xml` each passed 21/21 in separate fresh Unity processes.
- Final Edit Mode integration: `work-rework-editmode-final-results.xml` passed 1/1.
- Final Play Mode regression after simplification: `work-rework-scroll-unity-full-final-results.xml` recorded 59 total, 58 passed, 0 failed, and 1 intentionally ignored opt-in capture test.
- Final rendered evidence after simplification: `work-rework-scroll-layout-final-results.xml` passed 1/1. All 20 PNGs under `work-rework-scroll-layout-final-evidence/` had the expected 1280×720 or 800×600 dimensions, non-zero size, and non-uniform sampled pixels. Both final-slot images were visually inspected; `Earring 2` is fully visible without HUD or status overlap.
- `git diff --check` passed after the final task-artifact updates; the existing review metadata hard breaks were converted to blank-line-separated fields to remove their three trailing-whitespace findings without changing the recorded review outcome.

### Simplification and deviations

- `ce-simplify-code` results: reuse 1 applied, quality 0 applied, efficiency 1 applied, skipped 0. The reuse pass removed velocity assignments already guaranteed by `ScrollRect.StopMovement`; the efficiency pass removed a global canvas rebuild from ordinary focus changes while preserving the build/reset layout update.
- Framework probing detected active CE and gstack installations, OpenSpec without project artifacts, and no OMX CLI. Native inline execution remained authoritative because the checkout has no cross-model work-engine configuration and the two fixes share Unity UI/bootstrap contracts and a singleton Editor runner.
- Both CE context fences found no Node runtime in the Bash environment and continued under their documented fallback behavior. No commit, push, or Git index mutation was performed.

## Third review rework queue - 2026-08-04

1. [x] Preserve dirty and pathless Unity editor scenes when the disposable bootstrap Edit Mode contract runs. Use additive isolation, restore the original active scene, and close only test-created scenes.
2. [x] Exercise the public bootstrap regeneration and persistence path against a temporary project-relative scene. Reopen the saved scene and compare its bootstrap-owned contract with the committed scene.
3. [x] Add the explicit reward-equipped to starter negative-delta vector to the canonical fixture and assert the rendered negative value in console and Unity UI coverage.
4. [x] Add a Play Mode lifecycle test that opens the equipment panel, transitions from Camp to Battle through the normal state event, and proves reset, no save, and event silence.
5. [x] Rename console private field `NoStatDeltas` to `_noStatDeltas`.

After these items pass focused Edit Mode, action-contract, console, full Unity, evidence, and diff-hygiene gates, return to the Personal Flow `review` stage.

## Third review rework evidence - 2026-08-04

### Implemented fixes

- Scene regeneration now opens its target additively, restores the original active scene, closes only the target it opened, and scopes `EventSystem` discovery to the target scene. The Edit Mode contract preserves a dirty pathless scene and every pre-existing loaded scene while rebuilding and reopening a temporary project-relative scene.
- The public `ConfigureSampleScene` entry point and the integration contract share one persisted target-path pipeline. The public entry point successfully regenerated `SampleScene.unity`; the reopened temporary target retained the committed bootstrap-owned hierarchy, serialized references, persistent actions, and geometry.
- The canonical comparison fixture now includes reward-equipped to starter replacement with `Attack 2 -> 0 (-2)`. Console scripted I/O and Unity panel text both assert the negative delta and projected attack total without saving.
- A fresh Play Mode lifecycle contract opens and previews equipment, starts a hunt through `GameManager.StartHunt`, and proves the panel and selection details clear, existing save bytes remain unchanged, and the controller adds no events beyond the normal `StateChanged` and `BattleLog` transition.
- The console evaluator's private empty-delta field now follows the required `_noStatDeltas` convention.

### Proof-first and verification evidence

- Red bootstrap proof: after two test-harness-only corrections, `work-third-editmode-red-r3-results.xml` failed 1/1 because the persisted target-path regeneration method did not exist. The first generated/committed comparison then failed in `work-third-editmode-green-r3-results.xml` with 66 versus 65 hierarchy paths, exposing the cross-scene `EventSystem` lookup; the scene-local fix passed in `work-third-editmode-green-r4-results.xml`.
- Public persistence proof: `ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene` completed successfully in batch mode, and `work-third-editmode-post-public-results.xml` passed 1/1 against the regenerated committed scene. The post-simplification Edit Mode assembly passed 1/1 in `work-third-editmode-final-results.xml`.
- Focused negative-delta proof: console comparison and equipment UX tests passed 11/11 in `work-third-negative-console.trx`; Unity canonical comparison and panel rendering each passed 1/1 in `work-third-negative-unity-contract-results.xml` and `work-third-negative-unity-results.xml`.
- Focused lifecycle proof: `work-third-camp-exit-results.xml` passed 1/1 with exact cleanup, save-byte, and event-order assertions.
- Final console gates: `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore --nologo` passed with 0 warnings and 0 errors; the full console suite passed 51/51 in `work-third-console-final.trx`.
- Final Unity stability: `work-third-action-final-r1-results.xml` and `work-third-action-final-r2-results.xml` each passed 23/23 in separate fresh processes.
- Final Unity regression: `work-third-unity-full-results.xml` recorded 61 total, 60 passed, 0 failed, and 1 intentionally skipped opt-in capture test.
- Final rendered evidence: `work-third-layout-results.xml` passed 1/1. All 20 PNGs under `work-third-layout-evidence/` had the expected 1280x720 or 800x600 dimensions, non-zero size, and non-uniform pixels. Every image was visually inspected; the equipment states remain legible and clear of the HUD and status regions.
- `git diff --check` passed after generated-scene whitespace normalization and is rerun after this artifact update.

### Simplification and deviations

- `ce-simplify-code` results: reuse 0 applied, quality 1 applied, efficiency 1 applied, 5 skipped. The quality pass made Editor cleanup unconditional even when a byte assertion fails; the efficiency pass retained the newly created or loaded DropTable reference instead of reloading it from the asset database.
- The shared-test-helper proposal was outside the resolved mutation boundary, the source-token delegation check remains necessary to connect the public wrapper to the tested pipeline, and broader evaluator/reflection/fixture caching proposals were skipped as low-value or behavior-risking for this rework.
- Personal Flow and CE probes selected native inline execution: CE and gstack were present, OpenSpec had no project artifacts, OMX was unavailable, and no cross-model work-engine configuration was found. Both CE context fences found no Node runtime in Bash and continued under their documented fallback behavior.
- Existing unrelated `equipment-system-foundation` changes remain untouched. No commit, push, or Git index mutation was performed.

## Fourth review rework queue - 2026-08-04

1. [x] Keep a disabled `Equip` action visible when the console selected slot has no compatible owned candidate, restrict accepted input to `Back`, and prove the path cannot equip, unequip, or save.
2. [x] Remove the review-reported trailing whitespace from the five new Unity `.meta` files and restore the plan's diff-hygiene gate.

After the focused console proof, full console regression, production build, and diff-hygiene gate pass, return to the Personal Flow `review` stage.

## Fourth review rework evidence - 2026-08-04

### Implemented fixes

- `Game.ShowEquipmentSlot` now renders `Equip (unavailable: No compatible owned equipment is available.)` before the disabled Unequip action whenever candidate discovery returns an empty set.
- The no-candidate menu accepts only `Back` through the existing `ConsoleUI.ReadInt` range contract, so the visible disabled actions cannot reach comparison, mutation, or persistence.
- The existing optional-equipment integration test now asserts the disabled Equip label, the `Select (0-0)` prompt, absence of equip and unequip success output, and byte-identical save data.
- Trailing spaces were removed from the five new Unity `.meta` files named by the review-scope diff check. No Unity runtime, scene, fixture data, or serialized identifier changed.

### Proof-first and verification evidence

- Red proof: `work-fourth-r16-red.trx` recorded 8 total, 7 passed, and 1 failed before production implementation because the console output did not contain the required disabled Equip label.
- Focused green proof: `work-fourth-r16-green-final.trx` passed `GameEquipmentUxTests` 8/8 after the implementation and final disabled-input assertion.
- Final console regression: `work-fourth-console-final.trx` passed 51/51.
- Production build: `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore --nologo` passed with 0 warnings and 0 errors.
- Diff hygiene: `git diff --check` passed after the `.meta` cleanup and review-artifact EOF cleanup.

### Execution and scope notes

- Personal Flow probing found CE and gstack, OpenSpec without project artifacts, and no OMX CLI. The CE engine gate found no live, caller, or checkout cross-model preference, so native inline execution remained authoritative in Personal Flow return-to-caller mode.
- The CE context fence found no Node runtime in Bash and continued under its documented fallback behavior.
- The system-wide check found no callbacks or alternate mutation boundary: the changed menu path still runs through the real `Game`, `ConsoleUI`, and `SaveSystem` integration test.
- Dedicated simplification was skipped because the behavioral patch is under 30 substantive lines and introduces no duplicate abstraction. Unity suites were not rerun because Unity behavior was unchanged; the previous 1/1 Edit Mode, two 23/23 Action Contract runs, 60-pass full Play Mode run, and 20 inspected graphics captures remain the latest Unity evidence.
- Existing unrelated `equipment-system-foundation` changes remain untouched. No commit, push, or Git index mutation was performed.
