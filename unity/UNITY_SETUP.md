# Unity Setup Guide

## 1) Project and folders
1. Create a Unity 2D or 3D project in Unity Hub.
2. Copy this folder into your project:
   - `Toil-Relic/unity/Assets/Scripts`
3. In Unity, create folders:
   - `Assets/ScriptableObjects`

## 2) Create ScriptableObjects
1. Enemy database
   - Right click `Assets/ScriptableObjects` -> `Create -> ToilRelic -> Enemy Database`
   - Name it `EnemyDatabase_Main`
2. Enemy entries
   - Create 3-5 assets with `Create -> ToilRelic -> Enemy`
   - Example values:
     - Mine Vermin: HP 10, ATK 2-4, EXP 10
     - Rust Golem: HP 14, ATK 3-5, EXP 14
     - Ruin Wraith: HP 18, ATK 4-6, EXP 20
   - Add them into `EnemyDatabase_Main` list.
   - Optional: assign `Battle Visual Prefab` (2D or 3D) and a 2D HUD portrait.
3. Drop table
   - `Create -> ToilRelic -> Drop Table`
   - Name it `DropTable_Default`
   - Default is junk 1-3, relic chance 0.25

## 3) Scene objects
1. Create empty object `GameManager` and attach `GameManager.cs`
   - Assign `EnemyDatabase_Main` and `DropTable_Default`.
2. Create UI texts and attach:
   - `HudController` (HP/Level/Inventory TMP texts)
   - `BattlePanelController` (Enemy text + Log text)
   - Optional: assign an additional TMP text to `HudController`'s `Equipment Text` field to show the equipped weapon.
3. Create empty object `UIActions` and attach `GameActionBridge`
   - Assign `GameManager` field.
4. Create buttons and bind OnClick to `UIActions`:
   - Camp: `StartHunt`, `Rest`, `CraftTreasure`
   - Battle: `Attack`, `Defend`, `Flee`
  - Optional camp equipment buttons: `EquipStarterWeapon`, `EquipRewardWeapon`.
  - The primary weapon is always occupied. A future equipment screen can call `EquipEquipment(slot, equipmentId)` and `UnequipEquipment(slot)` for every non-primary physical slot.
5. Optional panel toggle
   - Attach `StatePanelController`
   - Assign camp panel and battle panel.

## 4) Run
- Press Play.
- Camp state starts first.
- Hunt -> Battle -> Loot -> Camp loop.
- Save file path: `Application.persistentDataPath/toil_relic_save.json`

## Notes
- The gameplay logic is in plain C# classes under `Systems` and `Core`.
- `BattlePhaseChanged` separates player input, enemy response, and resolution. Use it to sequence animations, VFX, or camera movement; do not put those waits into the gameplay systems.
- You can later swap UI or 2D/3D presentation without rewriting battle/crafting math.
- Enemy `Equipment Drop Profile Id` is a future-content reference only; do not assign equipment rewards until a drop-table task defines rates and rarity.
