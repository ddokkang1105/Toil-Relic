# Concepts

Shared domain vocabulary for this project — entities, named processes, and status concepts with project-specific meaning. Seeded with core domain vocabulary, then accretes as ce-compound and ce-compound-refresh process learnings; direct edits are fine. Glossary only, not a spec or catch-all.

## Gameplay flow

### Game State

The top-level mode that determines which gameplay context, interface, and action set are active.

### Battle Phase

The combat substate that determines whether player input is accepted or an action is being resolved.

Battle Phase is separate from Game State so presentation can wait for animation, effects, or camera transitions without changing combat rules.

### Save Load Status

The classification of the startup save-load attempt that governs whether progress can continue normally or must be replaced through New Game.

### Equipment Comparison Preview

A read-only view of a candidate equipment item against an explicit destination slot, including item-stat deltas and the resulting aggregate combat stats.

The preview does not change equipped state or saved progress until the player confirms an equip action.

### Hunt Contract

The Camp-facing commitment that lets the player inspect and confirm a specific quarry before battle, including its danger, featured reward, and contribution to the active Relic Project.

Confirming a Hunt Contract makes the selected quarry authoritative for the encounter and its reward profile; the game does not replace it with a random enemy.

### Quarry Reward Profile

The live reward definition associated with one quarry, used to distinguish that quarry's featured equipment outcome from generic battle loot.

### Relic Project

A persistent, player-visible goal advanced by distinct quarry contributions and completed by explicitly forging its one-time relic reward.

The Purposeful Hunt vertical slice supports one First Relic Project rather than a selectable project catalog.

## Development workflow

### Personal Flow

The repository's development task lifecycle that preserves decisions, execution status, review findings, verification evidence, and follow-ups from definition through closure.

## Testing vocabulary

### Action Contract

A Play Mode scenario that activates a real serialized player action and verifies its state transition, ordered feedback, resource changes, persistence boundary, and return of control.

### Viewport-Faithful Render Contract

A Play Mode visual-layout contract that measures the live Canvas and visible glyphs after the requested render viewport is attached, so its semantic assertions and captured pixels describe the same UI state.
