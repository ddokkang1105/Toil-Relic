# Plan

## Readiness

- Status: executable; stage advances to `work`.
- Scope: feature-aligned console and Unity equipment-state, combat, monster-data, save, and presentation changes.
- Exclusions: live equipment drops, rates, rarity, item instances, and art.

## Steps

1. [x] Replace the one-value `EquipmentSlot` models with the 12 physical positions and separate compatibility/category rules in both equipment catalogs. Add attack, defense, damage-reduction, and maximum-HP modifiers while preserving starter and reward weapons as primary-weapon definitions.
2. [x] Replace each player state's single equipped-weapon field with a serializable slot-to-equipment entry collection. Add shared invariants for ownership, compatibility, duplicate prevention, primary-weapon occupancy, and safe legacy normalization.
3. [x] Expose aggregate equipment stats and update combat damage/HP resolution. Player attack consumes aggregate attack; incoming enemy damage applies defense and damage reduction; maximum HP changes are clamped safely on initialization, loading, and re-equipping.
4. [x] Add an optional future drop-profile identifier to console enemy definitions and Unity `EnemyData`; preserve existing non-equipment loot behavior and configure no live equipment drop content.
5. [x] Replace the console weapon-only equipment menu with grouped slot-first selection and comparison output. Update Unity state consumers/HUD bridges to display aggregate stats and preserve a compact HUD; defer a polished 12-slot Unity screen if the scene/UI assets are unavailable.
6. [x] Update save version/normalization behavior and setup notes. Validate new games, legacy saves, invalid equipment, combat effects, and console/Unity parity before review.

## Affected paths

- Console models and saves: `src/ToilRelic/Models/EquipmentDefinition.cs`, `src/ToilRelic/Models/Player.cs`, `src/ToilRelic/Models/PlayerSaveData.cs`, `src/ToilRelic/Models/Enemy.cs`.
- Console gameplay and presentation: `src/ToilRelic/Systems/CombatSystem.cs`, `src/ToilRelic/Game.cs`, `src/ToilRelic/Util/ConsoleUI.cs`.
- Unity state and combat: `unity/Assets/Scripts/Core/EquipmentDefinition.cs`, `unity/Assets/Scripts/Core/PlayerState.cs`, `unity/Assets/Scripts/Systems/CombatSystem.cs`.
- Unity data/save/UI: `unity/Assets/Scripts/Data/EnemyData.cs`, `unity/Assets/Scripts/Save/SaveService.cs`, `unity/Assets/Scripts/Core/GameManager.cs`, `unity/Assets/Scripts/UI/GameActionBridge.cs`, `unity/Assets/Scripts/UI/HudController.cs`, `unity/UNITY_SETUP.md`.

## Validation

- Run `dotnet build` in `src/ToilRelic`.
- Manually exercise console new game, legacy load, starter/reward primary-weapon replacement, empty optional slots, incompatible/duplicate equip rejection, and attack/defense/damage-reduction/max-HP combat effects.
- In Unity Play Mode, load legacy and new saves, verify equipment normalization and aggregate stats, then hunt and confirm combat results and save/reload parity.
- Verify an enemy can carry an unset or named drop-profile identifier without creating an equipment reward.

## Rollback or migration

- Keep legacy weapon ownership and equipped-weapon data readable during normalization; migrate it into the primary-weapon entry only.
- Do not generate optional-slot entries with equipment during migration. If a migrated primary ID is unknown or incompatible, grant and equip only the starter primary weapon.
- Retain prior save compatibility through explicit version handling and normalization rather than rejecting old saves.
