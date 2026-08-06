# Task: Purposeful Hunt Vertical Slice

## Goal

Give the existing hunt, battle, reward, equipment, crafting, and save loop a clear player-facing purpose through one coherent, deliberately bounded vertical slice.

## Scope

- Define the smallest Hunt Contract experience that creates a meaningful quarry choice.
- Define quarry-specific reward expectations and one active relic-project payoff.
- Keep console and Unity behavior feature-aligned.
- Produce a requirements-only Product Contract before implementation planning.

## Non-goals

- General-purpose contract authoring framework.
- Seasons, daily runs, nemesis persistence, or a broad mastery system.
- Implementation during the brainstorm stage.

## Acceptance criteria

- [x] The primary player outcome and first-session flow are explicit.
- [x] In-scope and deferred behavior are separated.
- [x] Requirements and acceptance examples are concrete enough for implementation planning.
- [x] The requirements cover both console and Unity without inventing implementation details.

## Constraints and risks

- Gameplay logic changes must stay aligned across `src/ToilRelic` and `unity/Assets/Scripts`.
- The slice must reuse the current loop and avoid growing into a new expedition framework.
- Existing save, equipment-preview, generated-scene, and PlayMode action contracts must remain intact.

## Profile

`standard`
