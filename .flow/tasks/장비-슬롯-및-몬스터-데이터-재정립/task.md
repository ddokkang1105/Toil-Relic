# Task: 장비 슬롯 및 몬스터 데이터 재정립

## Goal

Re-establish the equipment and monster-data foundations so gameplay can support a primary weapon, secondary weapon, four armor slots, and five accessory positions consistently in the console and Unity implementations.

## Scope

- Define equipment categories and the following equip positions:
  - Weapons: primary weapon and secondary weapon.
  - Armor: hat, chest armor, gloves, and shoes.
  - Accessories: necklace, belt, two rings, and two earrings.
- Make the primary weapon the only default owned and equipped item for a new player and a migrated legacy save.
- Prepare equipment and monster data structures for future monster-drop configuration without implementing the full drop table in this task unless the plan explicitly approves a minimal integration.
- Keep console and Unity gameplay rules, persistence, and combat-stat interfaces aligned.

## Non-goals

- Configure final monster drop rates, rarity tables, or a complete item catalog.
- Design final equipment art, item icons, or polished inventory UI.
- Introduce random affixes, upgrading, crafting, or item trading.

## Acceptance criteria

- [ ] The data model represents all requested equipment positions, including two independent ring positions and two independent earring positions.
- [ ] A new player owns and equips only the default primary weapon; every other position is empty.
- [ ] Legacy saves normalize safely without duplicating the default weapon or forcing a previously intentional empty optional position to equip an item.
- [ ] Console and Unity expose matching slot, ownership, equipment, and stat-resolution rules.
- [ ] Monster data has an explicit extension point for future equipment drops, with no invented production drop balance.
- [ ] The design identifies the future comparison UI inputs and the required QA migration and combat scenarios.

## Constraints and risks

- This is an architectural change to persistence and equipment state; preserve existing saves and the already implemented starter/reward weapon behavior unless a reviewed migration supersedes it.
- The Unity project is not currently available for Play Mode validation, so runtime Unity QA remains dependent on the real project being provided.
- The existing equipment-comparison task must consume this slot model rather than maintain a separate weapon-only model.

## Profile

`large`
