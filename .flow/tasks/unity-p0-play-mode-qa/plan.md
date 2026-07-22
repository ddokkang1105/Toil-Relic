# Plan

## Readiness

## Steps

1. [ ]

## Affected paths

## Validation

## Rollback or migration
## Plan

1. Add an isolated Play Mode test assembly under `unity/Assets/Tests/PlayMode`.
2. Load `SampleScene` for each test and check the P0 scene contract: `GameManager`, its enemy/drop references, `GameActionBridge`, its manager reference, and all six button bindings.
3. Execute Unity Test Framework in Play Mode from the Unity 6000.3.19f1 editor, saving XML and Editor logs below `.flow/tasks/unity-p0-play-mode-qa/results/`.
4. Record each observed result without changing the game scene; configuration failures remain QA findings for the project owner to fix.

## Rework plan

1. Fix the Unity compiler contract violation in `PlayerState.TryGetEquippedEquipment`.
2. Use an Editor bootstrap to create the enemy/drop assets and configure `SampleScene` with GameManager, UIActions, EventSystem, camp/battle panels, and the six P0 action bindings.
3. Run the existing Play Mode scene-contract suite and record its XML result.
4. Align the EventSystem with the project's Input System-only configuration so Play Mode does not emit Input API exceptions.
