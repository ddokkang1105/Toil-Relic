# Task: Camp HUD Completion

## Goal

Complete the Unity camp HUD so players can see their current health, level progress, inventory resources, and equipped weapon when entering or returning to camp.

## Scope

- Wire `HudController` to concrete HUD text elements in `SampleScene`.
- Ensure player-state events populate HP, level, inventory, and weapon information.
- Add Play Mode checks for the HUD scene contract and visible initial values.

## Non-goals

- Redesign battle UI, add new game mechanics, or change console UI behavior.
- Build an inventory-management screen.

## Acceptance criteria

- [x] Camp HUD shows HP, level, inventory summary, and equipped weapon.
- [x] HUD updates from `GameEvents.PlayerChanged` on initial game entry and after gameplay state changes.
- [x] SampleScene contains a fully wired `HudController` and required uGUI `Text` fields.
- [x] Unity Play Mode validation passes.

## Constraints and risks

- Preserve the existing title, camp, and battle-panel state model.
- Do not overwrite unrelated user-owned changes in the dirty worktree.

## Profile

`standard`
