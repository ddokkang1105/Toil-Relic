# Unity Setup Guide

## 1) Project and data

1. Open `unity` as the Unity project. The supported editor for the committed scene is Unity `6000.3.19f1`.
2. Keep generated data under `Assets/ScriptableObjects`:
   - `EnemyDatabase_Main.asset`
   - `DropTable_Default.asset`
   - the Mine Vermin, Rust Golem, and Ruin Wraith enemy assets
   - `HuntContract_FirstRelic.asset`
   - `EquipmentDropProfileDatabase_Main.asset` and its three 35% quarry profiles
3. The scene bootstrap creates or refreshes those assets and wires them into `GameManager`.

## 2) Regenerate the committed scene

Use `Tools -> Toil Relic -> Regenerate Sample Scene` in the Unity editor, or run:

```powershell
& '<Unity.exe>' -batchmode -quit -projectPath '<repo>\unity' -executeMethod ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene -logFile '<bootstrap.log>'
```

The bootstrap is the authority for `Assets/Scenes/SampleScene.unity`; do not hand-edit the scene YAML. Regenerate the scene after changing the bootstrap, then commit both files together.

## 3) Generated scene contract

The generated canvas contains:

- `TitlePanel`: Continue, New Game, and Quit.
- `CampPanel`: the Camp state root assigned to `StatePanelController.campPanel`.
  - `CampActionMenu`: Hunt Contract, Rest, Craft Treasure, and Equipment.
  - `HuntContractPanel`: three generated quarry rows, selected-quarry detail, Back, Confirm Hunt, and Ready-gated Forge Relic.
  - `EquipmentPanel`: the dedicated slot, candidate, comparison, totals, validation, and action UI.
- `BattlePanel`: battle status plus Attack, Defend, Flee, and Potion.
- compact `Hud` and `GameStatus` regions above the state panels.

Rest, Craft Treasure, and battle actions persistently target `UIActions/GameActionBridge`. Hunt Contract and Equipment are mutually exclusive Camp-local panels owned by `HuntContractPanelController` and `EquipmentPanelController`. Quarry confirmation sends the selected stable ID and content revision to `GameManager`; runtime rows never own reward authority.

`HuntContractPanelController` serializes `GameManager`, Camp/Contract roots, Hunt entry, quarry-row container, title/detail/project texts, Back/Confirm/Forge controls, and focus targets. Forge is distinct from optional Craft Treasure. A successful Forge grants but does not auto-equip the Toilbound Relic, then opens Equipment focused on the Necklace candidate through the supported controller event.

`EquipmentPanelController` must serialize all of the following references:

- `GameManager`
- `CampActionMenu` and `EquipmentPanel`
- Equipment entry, Back, Equip, and Unequip buttons
- `SlotRowsContainer` and `CandidateRowsContainer`
- comparison, totals, and validation texts

The 12 slot rows and owned candidate rows are created at runtime by the controller. The bootstrap only creates their bounded scroll/layout containers. The primary weapon is mandatory; optional occupied slots can be unequipped. Equip and Unequip revalidate through `GameManager.EquipEquipment(slot, equipmentId)` and `GameManager.UnequipEquipment(slot)` before saving.

## 4) Run and verify

- Press Play. The title screen appears first; New Game or Continue enters Camp.
- Use `Camp -> Equipment` to preview an owned candidate, compare projected totals, Equip, Unequip an optional slot, or Back to Camp.
- The main loop is Camp -> Hunt Contract -> confirmed quarry -> Battle -> profile/base rewards and project progress -> Camp -> Forge -> Equipment choice.
- Saves use `Application.persistentDataPath/toil_relic_save.json`.
- Current Unity saves use schema v3. Valid v0-v2 saves load with an empty First Relic Project without rewriting source bytes until a later legitimate save action.

Run the Play Mode assembly without `-quit` so the Unity Test Runner can terminate its own process:

```powershell
& '<Unity.exe>' -batchmode -nographics -projectPath '<repo>\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults '<results.xml>' -logFile '<tests.log>'
```

Focused implementation checks:

```powershell
& '<Unity.exe>' -batchmode -nographics -projectPath '<repo>\unity' -runTests -testPlatform EditMode -testResults '<edit-results.xml>' -logFile '<edit.log>'
& '<Unity.exe>' -batchmode -nographics -projectPath '<repo>\unity' -runTests -testPlatform PlayMode -testFilter 'ToilRelic.PlayModeTests.PurposefulHuntRuntimePlayModeTests' -testResults '<runtime-results.xml>' -logFile '<runtime.log>'
& '<Unity.exe>' -batchmode -nographics -projectPath '<repo>\unity' -runTests -testPlatform PlayMode -testCategory 'PurposefulHuntActionContracts' -testResults '<action-results.xml>' -logFile '<actions.log>'
```

The Purposeful Hunt action-category run must report a non-zero test total. A successful Unity exit with `total=0` does not validate the action flow.

For rendered layout evidence, set `TOIL_RELIC_LAYOUT_EVIDENCE_DIR`, run `P0_CaptureLayoutEvidenceWhenRequested` with graphics enabled, and inspect the 1280x720 and 800x600 PNGs.

## Notes

- Gameplay logic remains in plain C# types under `Systems` and `Core`; scene UI only presents and dispatches it.
- `BattlePhaseChanged` separates player input, enemy response, and resolution. Sequence animation or VFX around that event instead of adding waits to gameplay systems.
- Enemy `Equipment Drop Profile Id` is live Hunt Contract content. Every profile reward remains optional at 35%, while each quarry's first victory guarantees one unique project contribution.
