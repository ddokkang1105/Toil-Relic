# Task: Tactical Hunt Grammar

## Goal

Make hunts tactical in both runtimes by revealing a locked enemy intent before each player choice and making existing actions respond meaningfully to it.

## Scope

- Add a shared behavioral contract for Power Attack, Exposed Opening, and Direct Strike.
- Give Mine Vermin and Rust Golem learnable alternating patterns; use Direct Strike for other current enemies.
- Convert console combat to player-stepped Attack, Defend, Potion, and Flee decisions.
- Publish and display intent through the existing Unity battle surface.
- Add console and Unity coverage, including a cross-runtime fixture.

## Non-goals

- New Unity combat buttons, combo or status-effect systems, new enemies, quarry mastery, or save-schema changes.
- A broad balance pass or encounter-content authoring framework.

## Acceptance criteria

- [x] Both runtimes reveal and lock the same intent before each valid player decision.
- [x] Power Attack makes Defend the preferred response and Exposed Opening rewards Attack.
- [x] Mine Vermin and Rust Golem use learnable focused patterns; other current enemies use Direct Strike.
- [x] Potion and Flee cannot reroll or bypass a locked intent, except successful Flee ending combat.
- [x] Console combat is player-stepped and does not expire while waiting for input.
- [x] Unity exposes a persistent text/non-color intent cue and preserves battle-phase gating.
- [x] Cross-runtime contract tests and existing relevant suites pass.

## Constraints and risks

- Gameplay parity requires matching console and Unity behavior in the same change.
- Existing hunt rewards, defeat recovery, saves, and UI action contracts must not regress.
- The worktree contains unrelated user-owned untracked QA artifacts; they must remain untouched.

## Profile

`standard`
