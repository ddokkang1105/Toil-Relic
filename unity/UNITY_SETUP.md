# Unity Setup Guide

## 1) Project and folders
1. Create a Unity 2D project in Unity Hub.
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
3. Create empty object `UIActions` and attach `GameActionBridge`
   - Assign `GameManager` field.
4. Create buttons and bind OnClick to `UIActions`:
   - Camp: `StartHunt`, `Rest`, `CraftTreasure`
   - Battle: `Attack`, `Defend`, `Flee`
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
- You can later swap UI without rewriting battle/crafting math.
