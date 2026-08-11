---
title: Build save envelopes around validated authority and deterministic failure boundaries
date: 2026-08-11
category: architecture-patterns
module: Save persistence
problem_type: architecture_pattern
component: service_object
severity: high
applies_when:
  - "A synchronous save must survive interruption without exposing a partial candidate as live data"
  - "One previously validated generation is sufficient for bounded automatic recovery"
  - "Multiple runtimes must share persistence semantics while keeping runtime-specific filesystem primitives"
  - "Correctness-critical mutations need deterministic before-and-after failure coverage"
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "save-envelope"
  - "validated-authority"
  - "atomic-replacement"
  - "last-known-good"
  - "crash-consistency"
  - "deterministic-failure-injection"
  - "console-unity-parity"
---

# Build save envelopes around validated authority and deterministic failure boundaries

## Context

Safe persistence is primarily an authority problem, not a serialization problem. After an interrupted write or a filesystem call that may have mutated before throwing, the next load must decide which complete payload is eligible without trusting partial bytes or the return value of the failed call.

The Ironclad Save Envelope uses five bounded sibling roles: live, stage, last-known-good (LKG), quarantine, and recovery marker. Only a validated live payload or validated LKG can be authoritative; stage, quarantine, and marker files never become load authorities merely because they exist. The console and Unity implementations derive the same roles from the live path (`src/ToilRelic/Systems/SaveSystem.cs:28`, `unity/Assets/Scripts/Save/SaveService.cs:27`).

## Guidance

### Put validation before every authority transfer

Serialize only to a same-directory stage file, request an OS flush, close the file, and validate the exact staged bytes with the same version and semantic rules used by Load. Both runtime wrappers expose a staged write with `Flush(true)` before their post-write checkpoint (`src/ToilRelic/Systems/SaveEnvelopeFileOperations.cs:83`, `unity/Assets/Scripts/Save/SaveEnvelopeFileOperations.cs:79`), and both save orchestrators validate stage before inspecting live (`src/ToilRelic/Systems/SaveSystem.cs:47`, `unity/Assets/Scripts/Save/SaveService.cs:183`).

Keep candidate validation non-mutating. A candidate should classify as missing, inaccessible, invalid, current, or supported legacy while retaining the original validated bytes for later promotion or recovery (`src/ToilRelic/Systems/SaveSystem.cs:278`, `unity/Assets/Scripts/Save/SaveService.cs:317`). This lets live, stage, and LKG use one eligibility rule without recursively triggering recovery.

### Use one primitive for the authority transition

When live is valid, rotate live to LKG and promote stage through one replace-with-backup operation. The console and Unity wrappers place logical checkpoints around a single `File.Replace` call (`src/ToilRelic/Systems/SaveEnvelopeFileOperations.cs:118`, `unity/Assets/Scripts/Save/SaveEnvelopeFileOperations.cs:122`). Do not model a fictional intermediate state inside that primitive and do not emulate it with copy-overwrite or delete-then-move.

When live is missing, promote stage with a no-overwrite move. When live is invalid or inaccessible, fail conservatively rather than overwriting evidence or rotating invalid bytes into LKG. These branches are selected only after live classification (`src/ToilRelic/Systems/SaveSystem.cs:58`, `unity/Assets/Scripts/Save/SaveService.cs:196`).

An exception after a mutating primitive is an unknown commit state. Return failure for the current call, do not attempt destructive rollback, and let a fresh validating Load settle which complete payload exists. The deterministic suites cover both sides of missing-live promotion as well as replace and recovery promotion (`tests/ToilRelic.Tests/IroncladSaveEnvelopeTests.cs:440`, `tests/ToilRelic.Tests/IroncladSaveEnvelopeTests.cs:485`, `unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs:484`, `unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs:528`).

### Normalize recovery through stage

Load valid live first. Only missing or invalid live may fall back to validated LKG; inaccessible live is a conservative unreadable result rather than permission to mutate (`src/ToilRelic/Systems/SaveSystem.cs:96`, `unity/Assets/Scripts/Save/SaveService.cs:234`).

Recovery should copy the exact bytes already validated as LKG into stage, flush, validate stage again, and compare byte identity before promotion. The console and Unity recovery paths implement that source-identity check (`src/ToilRelic/Systems/SaveSystem.cs:249`, `unity/Assets/Scripts/Save/SaveService.cs:286`). If damaged live exists, preserve it in a bounded quarantine before moving stage into the now-missing live slot.

Persist recovery-notice intent before recovery promotion. The marker is notice state, not save authority: a marker affects the result only when live itself validates. Recovery returns a typed `Recovered` result with the pending flag (`src/ToilRelic/Systems/SaveResults.cs:42`, `unity/Assets/Scripts/Save/SaveResults.cs:45`), and a later successful progress save clears the marker after live promotion.

### Probe deletion and remove the final authority last

Do not use `File.Exists` as the final deletion gate. Access failures can be indistinguishable from absence through convenience existence APIs. The envelope wrappers call `File.GetAttributes`, treat only file-not-found and directory-not-found as absent, and wrap every other exception as a failed delete operation (`src/ToilRelic/Systems/SaveEnvelopeFileOperations.cs:383`, `unity/Assets/Scripts/Save/SaveEnvelopeFileOperations.cs:398`). Fault tests verify that a metadata-probe failure cannot claim deletion success (`tests/ToilRelic.Tests/IroncladSaveEnvelopeTests.cs:519`, `unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs:560`).

For New Game invalidation, classify live and LKG before deleting anything. Delete transient and non-authoritative artifacts first, delete the last validated eligible payload at the data commit edge, and delete the notice marker last. Before the edge, at least one old validated payload remains recoverable; after it, none remains eligible. The shared New Game runners assert exact survivors and retry behavior at every boundary (`tests/ToilRelic.Tests/IroncladSaveEnvelopeTests.cs:179`, `unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs:196`).

### Share semantic vectors, not implementation code

Keep filesystem wrappers runtime-local, but drive both suites from one behavioral contract. The console test project links the Unity-owned JSON fixture rather than copying it (`tests/ToilRelic.Tests/ToilRelic.Tests.csproj:29`). Both readers pin the exact ordered 33-case manifest and its Save/Load/NewGame partition (`tests/ToilRelic.Tests/IroncladSaveEnvelopeContractFixture.cs:105`, `unity/Assets/Tests/PlayMode/IroncladSaveEnvelopeContractFixture.cs:124`). A deleted or reclassified vector therefore fails fixture validation instead of silently reducing coverage.

Each vector should assert more than the immediate return value:

- exact role-to-payload survivors;
- current-call status and redacted diagnostic shape;
- fresh-load status and authoritative payload;
- recovery-notice state; and
- idempotent retry outcome when invalidation was interrupted.

## Why This Matters

Validated authority makes ambiguous filesystem outcomes inspectable. Before promotion, the previous validated authority remains eligible. After an exception whose mutation state is unknown, a fresh Load derives truth from validated artifacts rather than guessing or rolling back destructively.

The fixed artifact set also keeps recovery bounded. LKG is one rotating prior generation, quarantine is one damaged-live copy, and stage is transient. None of them creates a hidden alternate load order.

Deterministic seams and target durability evidence are different proofs. Injected before/after checkpoints prove orchestration for modeled states, but they do not certify `File.Replace`, rename, or flush behavior on every filesystem or Unity player target. Keep the product guarantee capability-bounded until a target-specific process-interruption and cold-restart harness supplies that evidence (`docs/plans/2026-08-11-001-feat-ironclad-save-envelope-plan.md:672`).

## When to Apply

Apply this pattern when:

- persistence is synchronous and single-writer;
- one prior validated generation is enough for recovery;
- stage and live can reside on the same verified local volume;
- the runtime offers an indivisible replace-with-backup or destination-preserving move; and
- automatic recovery must remain visible without blocking play.

Do not stretch this narrow envelope into cloud conflict resolution, multi-writer locking, manual save UX, a generic serializer, or physical-device durability guarantees.

## Examples

### Save transition

```text
candidate -> write + flush stage -> validate stage
valid live   -> replace(stage, live, lkg)
missing live -> move-no-overwrite(stage, live)
invalid live -> fail and preserve evidence for Load/recovery
```

### Recovery transition

```text
validate live -> if valid, load it
otherwise validate lkg -> stage the same bytes -> validate again
create + flush recovery marker -> quarantine invalid live if present
move stage to live -> return Recovered(player, pending=true)
```

### Deterministic boundary contract

```text
seed symbolic artifacts
inject at (checkpoint, Before|After)
invoke Save, Load, or NewGame
assert immediate result and exact surviving bytes
reset the seam and perform a fresh validating Load
assert authority, pending notice, and retry behavior
```

## Related

- [Deterministic Unity PlayMode action contracts](../best-practices/deterministic-unity-playmode-action-contracts.md) describes the adjacent seam-isolation and fresh-process testing discipline.
- [Unity status-event save-failure contracts](../ui-bugs/unity-status-event-save-failure-contracts.md) covers semantic save feedback; its concrete event and seam names should be refreshed against the current recovery implementation.
- [Pure equipment preview with commit revalidation](pure-equipment-preview-with-commit-revalidation.md) is an adjacent example of rejecting stale authority and sharing behavioral vectors across console and Unity.
