# QA

## Runtime QA recheck — 2026-07-15

- Rechecked the execution environment while attempting the P0 runtime QA. No .NET SDK is available and no Unity Editor executable is discoverable.
- The console host cannot provide a build-capable SDK, and Unity Play Mode cannot be launched. The runtime scenarios below therefore remain pending and this QA stage cannot be marked complete.
- Required next environment changes: install a .NET SDK compatible with this project and the Unity Editor version specified by the project, then run every listed console and Unity scenario.

## Console runtime execution — 2026-07-15

- Installed .NET SDK `8.0.423` and built `src/ToilRelic/ToilRelic.csproj`: passed with 0 errors (one existing nullable warning, `CS8601`, in `Models/EquipmentDefinition.cs`).
- In an isolated temporary save directory, a new game began with `Starter Weapon +0`; the first hunt granted `Reward Weapon` exactly once; the weapon list displayed both entries; unequip persisted across reload; and `Reward Weapon +2` could be equipped again.
- A subsequent successful hunt kept `reward-weapon` at exactly one owned entry.
- A handcrafted legacy save with no equipment fields loaded with the starter weapon owned and equipped, preserved its inventory, and saved back with `EquipmentInitialized: true`.

## Unity runtime blocker — 2026-07-15

- `unity/` contains `Assets/Scripts` and `UNITY_SETUP.md`, but no `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, or scene assets. It is not an openable Unity project in this workspace.
- Unity Play Mode, save serialization through `JsonUtility`, and UI button wiring remain unverified. Provide the complete Unity project (including its version and scenes), or identify its location, to finish the remaining P0 Unity QA.

## Current QA status

- Console P0 runtime QA: passed.
- Unity P0 runtime QA: blocked by the missing Unity project, not by a failing test.
- Task stage remains `qa`; do not close the prerequisite until the Unity scenarios pass against the real project.

## Unity static QA — 2026-07-15

- Passed by code inspection: `GameManager.Start()` calls `PlayerState.InitDefaults()` after both fresh start and `SaveService.TryLoad`; legacy saves without equipment data receive and equip the starter weapon.
- Passed by code inspection: `GrantEquipment` rejects duplicate IDs; victory grants the reward through that method; `CombatSystem.RollPlayerAttack(player.AttackBonus)` applies the equipped weapon bonus.
- Passed by code inspection: camp-only equip/unequip methods publish player state and save; `GameActionBridge` exposes all three equipment button handlers; `HudController` renders equipped and unequipped states when its optional text field is assigned.
- Passed by code inspection: save envelope version is `2`, and serialized private equipment fields are normalized after load.
- This is a fallback verification only. It does not replace Unity Editor compilation, Play Mode, real `JsonUtility` round-trip, scene references, or button bindings.

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `dotnet --list-sdks` | blocked | No .NET SDK is installed; only the .NET host/runtime is available. |
| `cd src/ToilRelic && dotnet build` | blocked | Exit `-2147450735`: no .NET SDKs found. |
| Static console/Unity parity checks | pass | Verified initialization flag, persistent unequip API, first-win reward grant, single attack-bonus call site, and invalid-ID guards in both implementations. |
| `git diff --check` | pass | No whitespace errors reported. |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Console: start a new game | Starter weapon is owned and equipped; status shows attack bonus. | pending — .NET SDK required |
| Console: win first hunt | Reward weapon is granted once and listed in loot; subsequent wins do not duplicate it. | pending — .NET SDK required |
| Console: equip, unequip, save, and reload | Selected weapon or intentional unequip state survives reload. | pending — .NET SDK required |
| Console: load pre-equipment save | Missing equipment fields are normalized to starter weapon without changing item inventory. | pending — .NET SDK required |
| Unity: enter Play Mode and win first hunt | HUD and log show starter/reward weapon state; attack bonus applies after equip. | pending — Unity Editor required |
| Unity: equip, unequip, restart | Button actions work in camp and persisted state is restored after restart. | pending — Unity Editor required |
| Unity: load version-1 save | Missing equipment fields normalize without breaking camp, battle, crafting, or potion flows. | pending — Unity Editor required |

## Known limits

- QA cannot be completed in this environment because no .NET SDK or Unity Editor executable is installed or discoverable.
- Runtime behavior, serialization through the actual Unity `JsonUtility`, and UI button wiring remain unverified.

## Closure status

Implementation and static checks are complete, but this task remains open in `qa` until the pending console and Unity runtime scenarios pass. Runtime QA is tracked as a P0 follow-up.
