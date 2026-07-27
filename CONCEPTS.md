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

## Testing vocabulary

### Action Contract

A Play Mode scenario that activates a real serialized player action and verifies its state transition, ordered feedback, resource changes, persistence boundary, and return of control.
