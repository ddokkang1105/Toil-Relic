# Task: Unity P0 Play Mode QA

## Goal

Run a repeatable Unity Play Mode P0 QA check against `unity/Assets/Scenes/SampleScene.unity` and record the actual runtime result.

## Scope

- Verify the scene has the runtime objects and serialized references required to start the hunt/battle loop.
- Verify the UI binds the six P0 player actions: hunt, rest, craft, attack, defend, and flee.
- Run the checks with Unity Test Framework on the Unity 6000.3.19f1 editor.

## Non-goals

- Do not implement or redesign the game scene/UI while performing QA.
- Do not change console gameplay code.

## Acceptance criteria

- [ ] Unity Play Mode test command completes and writes XML/log artifacts.
- [ ] Each P0 scene prerequisite has a named pass/fail result.
- [ ] QA findings and exact reproduction command are recorded in `qa.md`.

## Constraints and risks

- Existing working-tree changes are user-owned and must not be overwritten.
- `SampleScene` currently appears to contain only the default camera; this is expected to cause configuration failures.

## Profile

`standard`
