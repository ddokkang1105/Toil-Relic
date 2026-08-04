---
title: Resume blocked Personal Flow QA with layered environment validation
date: 2026-07-28
category: workflow-issues
module: Personal Flow QA across console and Unity
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "A Personal Flow task was blocked because a required SDK, editor, or project fixture was unavailable"
  - "A recorded environment limitation may be stale when the task is resumed"
  - "Console and Unity implementations must satisfy the same gameplay and persistence contract"
  - "Save and reload scenarios need isolated temporary state"
  - "Headless Play Mode coverage needs separate rendered evidence for visual acceptance"
related_components:
  - "testing_framework"
  - "tooling"
tags:
  - "personal-flow"
  - "qa-resumption"
  - "environment-probing"
  - "unity-playmode"
  - "console-validation"
  - "save-isolation"
  - "visual-evidence"
  - "contract-parity"
---

# Resume blocked Personal Flow QA with layered environment validation

## Context

An environment blocker is a dated observation, not a permanent property of a task. The equipment-system foundation originally passed static review but could not finish runtime QA because the host lacked a usable .NET SDK and Unity project. When the task resumed, the repository had a complete Unity project, .NET was available, and the exact Unity Editor version was installed. Carrying the old blocker forward would have left completed work permanently unverified.

The durable close criterion was broader than “the tools now exist.” The console and Unity implementations define the same stable equipment IDs and the same `reward-weapon` attack bonus (`src/ToilRelic/Models/EquipmentDefinition.cs:54-60`, `unity/Assets/Scripts/Core/EquipmentDefinition.cs:29-34`). QA therefore needed current evidence from both runtimes, persistence compatibility, cross-runtime contract parity, and rendered UI evidence before advancing the Personal Flow stage.

## Guidance

### 1. Re-probe every transient prerequisite

Run the framework probe and the actual build/runtime version commands again when a blocked task resumes. Record the observed versions and date in the task QA artifact. Do not treat an old “SDK missing” or “Unity unavailable” note as current evidence.

For Unity, compare `unity/ProjectSettings/ProjectVersion.txt` with the installed Editor path. For .NET, run `dotnet --version` and the real project build rather than relying on host/runtime presence alone.

### 2. Keep console persistence scenarios isolated

The console save defaults to the current working directory (`src/ToilRelic/Systems/SaveSystem.cs:15-18`). Run the built executable from a new OS temporary directory so QA cannot read or overwrite a developer’s real `savegame.json`.

Exercise semantic transitions, not only process startup:

1. Start a new player and confirm the starter equipment.
2. Win twice and require the reward acquisition exactly once.
3. Equip the reward item and inspect the serialized ownership and slot.
4. Restart from the same isolated directory and confirm the equipped bonus.
5. Load a valid pre-equipment save and confirm equipment normalization without inventory loss.

The duplicate assertion matters because `GrantEquipment` rejects already-owned IDs and the victory log includes the reward only when that grant succeeds (`src/ToilRelic/Models/Player.cs:50-55`, `src/ToilRelic/Game.cs:130-133`, `src/ToilRelic/Game.cs:170-174`).

### 3. Prove legacy normalization without destructive load behavior

Loading an old save should repair missing equipment state in memory while preserving unrelated progress. Console normalization supplies a valid primary weapon after filtering invalid ownership and equipped entries (`src/ToilRelic/Models/Player.cs:123-147`).

Unity Play Mode tests should cover each historical format the loader accepts. The current fixture verifies equipment-free version 2 and version 1 saves and checks that loading does not rewrite the source bytes (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:265-289`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1427-1461`).

### 4. Separate behavioral regression from rendered evidence

Use two Unity runs:

- A full `-batchmode -nographics` Play Mode assembly for fast behavioral regression.
- A filtered graphics-enabled capture run, without `-nographics`, for screenshots that will be accepted as visual evidence.

Wait for the Unity process to finish and parse the NUnit result XML. A launcher return or the presence of a log file is not terminal test evidence. Treat an opt-in capture skip in the full suite as expected only when the separate capture run passes.

The Play Mode fixture installs a unique save-path override before scene loading and restores it during teardown (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:94-100`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1506-1530`). Preserve that isolation when adding scenarios.

### 5. Compare the shared contract explicitly

After runtime checks, assert the console and Unity definitions directly:

- `starter-weapon` and `reward-weapon` IDs match.
- `reward-weapon` contributes attack `+2`.
- duplicate and unknown grants are rejected.
- the first-victory reward uses the duplicate-safe grant result.
- owned and equipped state is serialized.
- the attack bonus is applied once in both combat paths.

The console adds `player.AttackBonus` to its attack roll and Unity passes the same aggregate to `RollPlayerAttack` (`src/ToilRelic/Systems/CombatSystem.cs:35`, `unity/Assets/Scripts/Core/GameManager.cs:121`, `unity/Assets/Scripts/Systems/CombatSystem.cs:35`).

### 6. Distinguish superseding design from regression

A later approved task may intentionally replace an early acceptance assumption. Record that relationship before judging current behavior.

For equipment, the later slot design made `PrimaryWeapon` mandatory and allowed unequip only for optional slots. Both runtimes enforce that invariant (`src/ToilRelic/Models/Player.cs:70-73`, `unity/Assets/Scripts/Core/PlayerState.cs:65`). The foundation task records the superseding decision rather than misclassifying the intentional rule as a QA failure (`.flow/tasks/equipment-system-foundation/decisions.md:39-43`).

### 7. Promote the workflow only after writing evidence

Update `qa.md`, resolve or retain each follow-up, and then move `state.yaml` from `qa` to `close`. The stage change is the consequence of recorded evidence, not a substitute for it.

The completed equipment QA recorded a clean console build, isolated save lifecycle checks, 42 passed Unity tests with no failures, a separate one-test capture pass with eight PNGs, static parity assertions, and no unresolved P0/P1 defects (`.flow/tasks/equipment-system-foundation/qa.md:20-29`, `.flow/tasks/equipment-system-foundation/qa.md:58-62`).

## Why This Matters

Reusing a stale blocker can strand verified code indefinitely. Clearing the blocker without layered QA creates the opposite risk: installed tools may hide duplicate rewards, contaminated local saves, destructive legacy migration, divergent combat math, or invalid screenshots.

The layered approach gives each failure a narrow meaning:

- probe failure means the environment still cannot execute the gate;
- console scenario failure means the model or persistence contract is wrong;
- Unity XML failure means the Play Mode behavior regressed;
- unusable screenshots mean the visual evidence is invalid even if capture code ran;
- parity failure means the two implementations drifted;
- artifact or stage mismatch means the workflow record is not trustworthy.

## When to Apply

- Resuming a task previously blocked by missing SDKs, editors, licenses, project files, or test fixtures.
- Closing work that changes both console and Unity gameplay or persistence.
- Verifying additive save fields against historical saves.
- Accepting Unity screenshots produced in batch mode.
- Reconciling an older task’s acceptance criteria with a later approved design.

## Examples

### Console isolation

Build once, switch to a unique temporary working directory, and pipe deterministic menu input into the built executable. Inspect the generated save there, rerun from the same directory, and discard the fixture after verification.

### Unity behavioral and visual evidence

Run the complete `ToilRelic.PlayModeTests` assembly with `-nographics`. Then set `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` and run `P0_CaptureLayoutEvidenceWhenRequested` without `-nographics`. Parse both XML files and inspect every required viewport image.

### Stage transition

```text
historical blocker recorded
→ current prerequisites re-probed
→ console lifecycle and legacy save verified
→ Unity full suite and graphics capture verified
→ console/Unity parity checked
→ qa.md and followups.md finalized
→ state.yaml advanced to close
```

## Related

- [Deterministic Unity Play Mode action contracts through serialized UI](../best-practices/deterministic-unity-playmode-action-contracts.md)
- [Validate Unity Play Mode screenshot pixels before accepting visual evidence](../best-practices/unity-playmode-screenshot-evidence-validity.md)
- `.flow/tasks/equipment-system-foundation/qa.md`
