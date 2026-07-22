# Task: 전투 UI·상태 전환 정리

## Goal

Make the Unity battle UI and title/camp/battle transitions clear, reliable, and consistent with the existing game-state model.

## Scope

- Inspect the current game-state transitions and the UI panels/actions that expose them.
- Define and implement the smallest UI and state-flow changes needed to make battle entry, combat actions, return to camp, and terminal results understandable.
- Cover the resulting scene contract and transition behavior with Unity Play Mode validation.

## Non-goals

- Add new enemies, combat formulas, inventory mechanics, or a redesigned main menu.
- Change save-file format unless a verified transition defect requires it.

## Acceptance criteria

- [ ] Players can identify the active Title, Camp, or Battle state from the visible UI.
- [ ] Battle entry, actions, victory/defeat handling, and return to camp follow one predictable state flow.
- [ ] Buttons unavailable in the current state are hidden or non-interactable, with no invalid transition caused by UI input.
- [ ] Unity Play Mode validation covers the final transition and UI contracts.

## Constraints and risks

- Preserve the existing `GameManager` state model and the title/save entry flow.
- Keep the console and Unity implementations feature-aligned when gameplay logic changes.
- Do not overwrite unrelated changes in the dirty worktree.

## Profile

`standard`
