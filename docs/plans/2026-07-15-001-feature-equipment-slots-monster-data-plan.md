---
artifact_contract: ce-unified-plan/v1
artifact_readiness: implementation-ready
product_contract_source: ce-brainstorm
created_at: 2026-07-15
topic: equipment-slots-and-monster-data
execution: code
---

# Equipment Slots and Monster Data - Plan

## Goal Capsule

- **Objective:** Establish a consistent equipment and monster-data contract for the console and Unity versions.
- **Product authority:** The player uses 12 physical equipment positions; only the default primary weapon is initially owned and equipped.
- **Open blockers:** None. Drop-content balance is intentionally deferred.

---

## Product Contract

### Summary

The game will support primary and secondary weapons, four armor positions, and six accessory positions. Equipment rules must remain feature-aligned across console and Unity while preserving legacy saves and leaving all optional positions empty until equipment rewards exist.

### Requirements

**Equipment positions and ownership**

- R1. Provide these physical positions: primary weapon, secondary weapon, hat, chest armor, gloves, shoes, necklace, belt, ring 1, ring 2, earring 1, and earring 2.
- R2. Keep compatibility separate from a physical position so one ring or earring type can fit either numbered counterpart.
- R3. A new player and normalized legacy save own and equip only the default primary weapon; every optional position starts empty.
- R4. The primary weapon may be replaced by an owned compatible primary weapon but cannot be unequipped into an empty state.
- R5. Keep `Reward Weapon` as a primary-weapon candidate.
- R6. Equipment ownership is definition-ID based for this release. The same ID cannot occupy more than one position, including both ring or earring positions.

**Equipment effects and comparison**

- R7. Weapons grant attack only.
- R8. Armor may grant defense, damage reduction, and maximum HP.
- R9. Accessories may grant attack and/or defense.
- R10. Treat damage reduction as an explicit stat distinct from defense.
- R11. Resolve combat-facing equipment effects through aggregate player stats rather than a weapon-only lookup.
- R12. Compare a candidate against a selected destination position and show the destination, per-stat change, and resulting attack, defense, damage reduction, and maximum HP.

**Monster data and acquisition boundaries**

- R13. Each monster can optionally reference an equipment-drop profile identifier.
- R14. This release must not claim or configure live equipment item lists, drop probabilities, rarity, or guaranteed drops.
- R15. Empty optional positions communicate that equipment will be acquired through future monster drops without placeholder items or fake rewards.

**Parity and save behavior**

- R16. Console and Unity use matching slot, ownership, compatibility, equip, stat-resolution, and save-normalization rules.
- R17. Legacy save normalization neither duplicates the default weapon nor fills intentionally empty optional positions.

### Experience Boundaries

- Group the equipment screen into Weapons, Armor, and Accessories.
- Use numbered ring and earring labels, not left/right semantics.
- Keep the HUD compact by showing the primary weapon and aggregate combat-facing stats; show all positions on the equipment screen.
- The slot-first flow selects a position, then filters owned equipment to compatible choices.

### Deferred

- Equipment item instances, duplicate copies, and per-item unique-equip restrictions.
- Shared drop-profile content, equipment catalogs, drop rates, rarity tables, and acquisition balance.
- Random affixes, upgrades, crafting, trading, final inventory presentation, art, and icons.

### Acceptance Signals

- All 12 positions can be represented, with independent ring and earring occupancy.
- A new or migrated player has exactly the default primary weapon equipped and no optional equipment equipped.
- Incompatible, unknown, unowned, or duplicate equipment cannot be equipped.
- Aggregate equipment stats affect combat consistently in both game implementations.
- Monster data can name a future drop profile without creating a live drop reward.

---

## Planning Contract

### Key Technical Decisions

- KTD-1. Model physical positions separately from equipment compatibility so numbered ring and earring positions remain independent without creating separate item types.
- KTD-2. Persist equipped state as a serializable collection of slot-to-equipment entries; retain owned equipment IDs as the ownership authority in both JSON systems.
- KTD-3. Compute equipment effects once as aggregate player stats. Combat callers consume aggregate attack, defense, damage reduction, and maximum HP rather than inspecting an equipped weapon.
- KTD-4. Keep the monster drop boundary as an optional profile identifier. Shared profile data and balance are a later content task.

### Implementation Units

#### U1. Equipment domain and persistence

- **Files:** `src/ToilRelic/Models/EquipmentDefinition.cs`, `src/ToilRelic/Models/Player.cs`, `src/ToilRelic/Models/PlayerSaveData.cs`, `unity/Assets/Scripts/Core/EquipmentDefinition.cs`, `unity/Assets/Scripts/Core/PlayerState.cs`, `unity/Assets/Scripts/Save/SaveService.cs`.
- **Goal:** Define slot compatibility, modifiers, ownership, equipped entries, and idempotent legacy normalization.
- **Tests:** New state owns/equips only the starter primary; invalid, unowned, incompatible, duplicate, and empty-primary state are normalized or rejected; legacy weapon state migrates to primary without filling optional positions.

#### U2. Combat and monster data

- **Files:** `src/ToilRelic/Systems/CombatSystem.cs`, `src/ToilRelic/Models/Enemy.cs`, `unity/Assets/Scripts/Systems/CombatSystem.cs`, `unity/Assets/Scripts/Data/EnemyData.cs`.
- **Goal:** Apply aggregate stats to combat and add the future drop-profile reference without live drops.
- **Tests:** Weapon attack changes player damage; armor defense and damage reduction lower incoming damage; maximum-HP effects clamp current HP; unset and named drop-profile references do not change current loot.

#### U3. Equipment presentation and integration

- **Files:** `src/ToilRelic/Game.cs`, `src/ToilRelic/Util/ConsoleUI.cs`, `unity/Assets/Scripts/Core/GameManager.cs`, `unity/Assets/Scripts/UI/GameActionBridge.cs`, `unity/Assets/Scripts/UI/HudController.cs`, `unity/UNITY_SETUP.md`.
- **Goal:** Present grouped positions and slot-first comparison in console, and expose equivalent aggregate state through Unity's existing UI bridge and HUD.
- **Tests:** Console shows empty optional positions and only compatible owned choices; comparison reports deltas and resulting totals; Unity new/load/save flow retains the same visible combat-facing totals.

### Verification Contract

- Run `dotnet build` from `src/ToilRelic`.
- Exercise console new-game, legacy-load, equip, replacement, and combat scenarios.
- Run Unity Play Mode against new and legacy saves, then hunt, save, reload, and compare displayed aggregate stats.
