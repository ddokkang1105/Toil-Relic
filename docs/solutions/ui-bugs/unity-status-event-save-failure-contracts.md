---
title: Preserve Unity terminal outcomes through level-ups and save failures
date: 2026-07-16
category: ui-bugs
module: Unity battle status presentation
problem_type: ui_bug
component: testing_framework
symptoms:
  - "A level-up log could replace the visible victory outcome."
  - "A save-failure log could replace a terminal battle outcome or be immediately hidden by a later success log."
root_cause: logic_error
resolution_type: code_fix
severity: medium
tags: [unity, playmode, status-events, save-failure]
---

# Preserve Unity terminal outcomes through level-ups and save failures

## Problem

`GameStatusController` renders the last user-facing status message. Reusing a generic battle-log event for terminal outcomes, level-ups, and save errors made the final Camp message depend on event wording and call order.

## Symptoms

- A level-up message could overwrite `Win.` immediately after victory.
- A post-victory save failure could hide the outcome.
- Rest or Equip could publish a normal success log after a failed save and hide the error.

## What Didn't Work

- Matching display strings such as `Win.` and `Level up!` in the presenter. This breaks when wording or localization changes.
- Suppressing the next generic log after a level-up. This made the UI depend on an implicit event-order contract.
- Directly raising UI events in a test. That did not prove `GameManager -> SaveProgress -> SaveService.TrySave` was wired correctly.

## Solution

Use explicit presentation events in `GameEvents`:

- `BattleOutcome` sets the retained terminal message.
- `LevelUp` appends to a retained terminal message without publishing a duplicate generic log.
- `SaveFailed` appends to a retained terminal message; otherwise it becomes the current status.
- A normal `BattleLog` clears terminal-outcome retention and replaces stale status text for the next action.

Keep save failures last in action flows. Rest and Equip publish their success log before `SaveProgress()`, allowing `SaveFailed` to remain visible when persistence fails.

For Play Mode coverage, `SaveService` exposes a private `saveWritePathOverride` used only by `TrySave()`. Tests redirect one write to a nonexistent GUID directory, restore the value in `finally`, and drive public `GameManager` actions.

## Why This Works

The presenter responds to event meaning rather than message text or a presumed next event. The write-only test seam does not affect save discovery, loading, or deletion, while still forcing the real persistence call to fail.

## Prevention

- Add a dedicated event when a UI message has retention or composition semantics.
- Test terminal UI state by invoking public game actions, not presenter events alone.
- Restore any static test override in `finally`.
- Verify save-failure visibility for every action that logs a success and then saves.

## Related Issues

- [Unity Play Mode HUD contracts](../best-practices/unity-playmode-hud-contracts.md)
