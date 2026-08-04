# Unity Setup Guide

## 1) Project and data

1. Open `unity` as the Unity project. The supported editor for the committed scene is Unity `6000.3.19f1`.
2. Keep generated data under `Assets/ScriptableObjects`:
   - `EnemyDatabase_Main.asset`
   - `DropTable_Default.asset`
   - the Mine Vermin, Rust Golem, and Ruin Wraith enemy assets
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
  - `CampActionMenu`: Hunt, Rest, Craft Treasure, and Equipment.
  - `EquipmentPanel`: the dedicated slot, candidate, comparison, totals, validation, and action UI.
- `BattlePanel`: battle status plus Attack, Defend, Flee, and Potion.
- compact `Hud` and `GameStatus` regions above the state panels.

Hunt, Rest, Craft Treasure, and battle actions persistently target `UIActions/GameActionBridge`. Equipment is Camp-local: the Camp Equipment entry and the fixed Back, Equip, and Unequip buttons persistently target `EquipmentPanelController`, which is attached to `CampPanel`.

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
- The main loop remains Camp -> Hunt -> Battle -> Loot -> Camp.
- Saves use `Application.persistentDataPath/toil_relic_save.json`.

Run the Play Mode assembly without `-quit` so the Unity Test Runner can terminate its own process:

```powershell
& '<Unity.exe>' -batchmode -nographics -projectPath '<repo>\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults '<results.xml>' -logFile '<tests.log>'
```

For rendered layout evidence, set `TOIL_RELIC_LAYOUT_EVIDENCE_DIR`, run `P0_CaptureLayoutEvidenceWhenRequested` with graphics enabled, and inspect the 1280x720 and 800x600 PNGs.

## Notes

- Gameplay logic remains in plain C# types under `Systems` and `Core`; scene UI only presents and dispatches it.
- `BattlePhaseChanged` separates player input, enemy response, and resolution. Sequence animation or VFX around that event instead of adding waits to gameplay systems.
- Enemy `Equipment Drop Profile Id` remains a future-content reference; do not assign equipment rewards until a drop-table task defines rates and rarity.
