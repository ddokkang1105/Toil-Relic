# Task: Ironclad Save Envelope

## Goal

Protect player progression in both the console and Unity runtimes from interrupted or partial save writes, and make any automatic recovery understandable to the player.

## Scope

- Define one observable durability contract shared by both runtimes.
- Write new state away from the live save before replacing it atomically.
- Preserve and validate a last-known-good save that can recover from an unreadable live save.
- Surface recovery as a typed result and player-facing outcome without blocking continued play.
- Prove interruption behavior with deterministic failure-injection tests.

## Non-goals

- Manual save or Save & Quit controls.
- Cloud synchronization or multi-device conflict resolution.
- A new session or expedition model.
- A generalized serialization or persistence framework.

## Acceptance criteria

- [x] A completed save exposes either the whole new state or the previous valid state, never a partial new state.
- [x] An interrupted write cannot destroy the last validated player progression.
- [x] Storage remains bounded to one rotating last-known-good copy and one latest quarantined damaged live copy, excluding transient staging data.
- [x] Loading can distinguish normal load, missing save, unreadable save, and successful recovery in both runtimes.
- [x] When the live save is unreadable and last-known-good is valid, the game preserves the damaged file, promotes the validated recovery copy, and continues automatically.
- [x] A successful recovery tells the player that an older valid save was restored while keeping developer diagnostics separate.
- [x] Console and Unity tests deterministically exercise equivalent interruption and recovery outcomes.
- [x] Existing supported saves remain loadable without requiring manual intervention.
- [x] Starting New Game cannot later resurrect stale live, recovery, staging, or quarantine data.

## Constraints and risks

- Console and Unity use different serialization and filesystem APIs, but must expose aligned player-visible behavior.
- Recovery precedence and backup lifecycle are settled in the requirements-only Product Contract.
- Platform-specific atomic replacement mechanics belong to planning; this task defines the required outcomes first.
- Existing user-owned untracked QA evidence must remain untouched.

## Profile

`large` — cross-runtime data integrity, recovery behavior, failure injection, and persistent-state compatibility.
