# Decisions

## Task initialization — 2026-07-15

- Profile: `large`; engineering review, design review, and brainstorming are required before implementation planning.
- Confirmed positions: primary weapon, secondary weapon, hat, chest armor, gloves, shoes, necklace, belt, ring 1, ring 2, earring 1, and earring 2.
- Only the primary weapon is initially owned and equipped. Secondary weapon, armor, and accessories are future monster-drop content.
- Framework probe: CE is installed in the local cache but its skills are not active in this session. OpenSpec is not runnable, and gstack/OMX are unavailable; use documented fallback reviews until the corresponding tools are active.

## Brainstorm decisions

- Monster equipment rewards will use a future-facing, optional drop-profile reference only. This task will not assign per-monster equipment lists, drop probabilities, or rarity balance.
- This keeps the empty optional equipment positions honest while allowing the console and Unity implementations to share a stable monster-data contract before content balancing begins.
- `Reward Weapon` remains a primary-weapon candidate only; it is not reclassified as a secondary weapon.
- Equipment modifiers are category-scoped: weapons grant attack only; armor may grant defense, damage reduction, and maximum HP; accessories may grant attack and/or defense. Damage reduction is a distinct modifier, not a reinterpretation of defense.
- Initial ownership is definition-ID based, so the same equipment ID cannot be equipped in both ring or both earring positions. Per-item duplicate and unique-equip rules are deferred until item instances are introduced.
- A monster carries only an optional drop-profile identifier in this scope; shared profile content, item lists, rarity, and probability are deferred.
- The equipment interaction is slot-first: choose a physical slot, then choose from compatible owned equipment. Comparison shows the candidate, destination slot, stat deltas, and resulting attack, defense, damage reduction, and maximum HP.
- Brainstorm gate completed with the user's instruction to adopt all remaining recommendations and move to planning.

## Confirmed

- Engineering-review recommendation: model physical equip positions separately from item compatibility. The 12 physical positions are `PrimaryWeapon`, `SecondaryWeapon`, `Hat`, `Armor`, `Gloves`, `Shoes`, `Necklace`, `Belt`, `Ring1`, `Ring2`, `Earring1`, and `Earring2`.
- A ring item must be compatible with both ring positions and an earring item with both earring positions. Therefore `EquipmentDefinition` needs a category or mount/compatibility rule in addition to an individual equipped position.
- Store equipped state as a serializable collection of slot-to-equipment entries, not one property per slot and not a dictionary. This keeps console JSON and Unity `JsonUtility` aligned and supports empty positions explicitly.
- Preserve `OwnedEquipmentIds` as the ownership source of truth. Equip only a known, owned item that is compatible with the destination position; one owned item cannot occupy more than one position.
- Replace weapon-only combat lookup with one equipment-stat aggregation step. Existing weapon attack bonuses remain supported while future defense, max HP, and other modifiers can be added without changing combat callers repeatedly.
- Reserve a data extension point on each monster definition for a future equipment-drop profile/table ID. Do not assign live drop rates or new loot balance in this task.

## Design review — 2026-07-15

### Assessment

- Initial readiness: 5/10. The requested 12 physical positions are clear, but the player-facing equipment information architecture, empty-state behavior, and comparison interaction were unspecified.
- Recommended direction: one equipment screen grouped into Weapons, Armor, and Accessories. Preserve every physical position while avoiding an undifferentiated list of 12 rows.

### Recommended interaction rules

- Show `Primary Weapon` and `Secondary Weapon` together, armor as four labeled positions, and accessories as necklace, belt, ring 1/ring 2, and earring 1/earring 2.
- Selecting a position shows its equipped item or an explicit empty state. Selecting a compatible owned item compares it with that exact destination position before replacement.
- Primary weapon is always occupied after new-game and legacy-save normalization. It may be replaced by another compatible owned primary weapon, but it has no unequip action.
- Every optional position starts empty. Do not create placeholder items or fake drops; show a neutral “obtain from future monster drops” empty-state message instead.
- Ring and earring positions are numbered rather than given left/right semantics. A compatible ring or earring may be equipped in either numbered position.
- The comparison view must show item name, affected stats, per-stat delta, destination position, and the player's resulting aggregate stats before confirming equip.
- Console should expose the same model as grouped text plus a numbered target-position selection; Unity may render grouped panels, but must not add rules unavailable in console.

### Design constraints for follow-up work

- The existing equipment-comparison task should accept a candidate item ID and explicit destination position; it must not infer a single global weapon position.
- HUD should remain compact: show primary weapon and aggregate combat-facing stats by default. The full 12-position layout belongs in the equipment screen.
- Monster drop data should not be shown as guaranteed until a later drop-table task defines rarity and acquisition rules.

## Rejected options

- One generic `Weapon` slot plus ad-hoc booleans for secondary weapon, rings, and earrings: this cannot represent independent ring/earring occupancy or validate compatibility consistently.
- Separate serialized fields for every equipped item: it makes save migrations and console/Unity parity error-prone as slots grow.
- Giving every optional position a default item during migration: violates the requirement that only the primary weapon is initially equipped.

## Open questions

- Should `Reward Weapon` migrate as a primary-weapon candidate only, or should it be reclassified as a secondary weapon? The safe default is primary-only, preserving its current behavior.
- Which initial modifiers are in scope beyond attack: defense, max HP, critical chance, or none until item drops exist?
- Will accessories have unique-equip restrictions, or may two copies of the same ring/earring be equipped once item instances are introduced? The current ID-based ownership model supports one catalog item per ID only.
- Should monsters use a shared drop-profile asset/data object from the first implementation, or an optional string ID until a drop-table task is created?
- Should the first equipment screen allow direct equip from a slot selection, or require opening a separate owned-item list/panel? The recommended default is slot selection followed by a filtered owned-item list.
- Which aggregate stats should appear in the first comparison panel beyond attack and defense? The recommended initial set is attack, defense, and max HP only when those modifiers exist.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| gstack engineering review | complete | Direct fallback review: gstack was unavailable in that session. Reviewed the console and Unity equipment catalogs, player persistence, combat lookup, Unity `JsonUtility` save envelope, and monster data assets. |
| gstack design review | complete | Direct fallback review: gstack skills are installed on disk but are not active in this Codex session. Reviewed equipment-screen information architecture, empty states, comparison inputs, and console/Unity parity. |
