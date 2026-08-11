---
title: Ironclad Save Envelope - Plan
type: feat
date: 2026-08-11
topic: ironclad-save-envelope
artifact_contract: ce-unified-plan/v1
artifact_readiness: implementation-ready
product_contract_source: ce-brainstorm
execution: code
plan_depth: deep
deepened: 2026-08-11
---

# Ironclad Save Envelope - Plan

## Goal Capsule

- Objective: Give the console and Unity runtimes one bounded save-durability contract so an interrupted operation exposes either the complete new state or the previously validated state, with automatic recovery made visible to the player.
- Authority hierarchy: The Product Contract governs observable behavior and scope. The Planning Contract governs implementation mechanics. Implementation Units may choose local details only where neither contract decides them.
- Stop conditions: Stop and return to planning if a target cannot provide a safe same-directory promotion primitive, if implementation would require in-place live writes or a destructive copy/delete fallback, if a valid source would need to be discarded to continue, or if the requested scope expands into cloud, sessions, manual saves, or a general persistence framework.
- Execution profile: One feature-aligned change across console and Unity, with deterministic tests in both runtimes and the Personal Flow engineering-review gate before implementation.
- Tail ownership: ce-work owns implementation, review, verification, and commits after the plan is selected for execution.

---

## Product Contract

Product Contract unchanged.

### Summary

Ironclad Save Envelope replaces live-file writes with a validated single-generation snapshot flow shared by the console and Unity versions. When a live save cannot be used but a validated last-known-good copy can, the game automatically restores that copy, continues loading, and gives the player a non-blocking notice that recent progress may be missing.

### Problem Frame

The console currently writes to a temporary file before overwriting the live save, but it keeps no validated recovery copy. Unity writes directly to the live path. Both runtimes classify loads as only Missing, Loaded, or Unreadable, delete only the live file, and lack deterministic interruption tests around promotion. As Hunt Contracts and Relic Projects increase the value of persisted progress, a partial or interrupted write must not turn the latest valid progression into an unrecoverable startup failure.

The feature is a bounded durability envelope around the existing save models, not a new persistence platform. It protects against unexpected process interruption and filesystem-operation failure. It does not promise survival from physical device loss or corruption after the operating system reports the operation as durable.

### Key Decisions

- D1 — Use one validated snapshot generation: stage the candidate away from live, validate it, preserve the current valid live save as last-known-good, and atomically promote the candidate. Governs R1–R5 and R15–R17.
- D2 — Keep exactly one rotating last-known-good copy and at most one latest quarantined damaged live copy, excluding transient staging data. Governs R4, R5, R7, R8, and R13. (session-settled: user-approved — the user delegated remaining choices to the recommended bounded path.)
- D3 — Automatically recover a valid last-known-good save instead of loading it only in memory or presenting a blocking choice. Governs R6–R12. (session-settled: user-directed — selected as “자동 복구 후 알림”.)
- D4 — Treat staging data as uncommitted and never as a recovery candidate. Governs R2, R4, R5, and R9.
- D5 — Bound the guarantee to process interruption and filesystem-operation failure, while excluding physical-device failure and post-success media corruption. Governs R3, R8, and the success criteria.

### Requirements

#### Durability and promotion

- R1 — Both runtimes must serialize a new candidate away from the live save path; neither may write a new payload into the live file in place.
- R2 — A staged candidate must pass the same format, version, and semantic validation required for a current supported live save before it becomes eligible for promotion.
- R3 — If execution stops at any point in the save operation, the next load must be able to observe either the complete previously validated live state or the complete validated candidate, never a partial candidate presented as live.
- R4 — Before replacing a valid live save, the operation must preserve that validated live payload as the single last-known-good generation. Unreadable, unsupported, or merely staged data must never replace the last-known-good generation.
- R5 — Orphaned staging data must be ignored or cleaned on startup and must never outrank either a valid live save or a validated last-known-good save.

#### Load and recovery

- R6 — A valid live save must load normally and return Loaded; the presence of older recovery or staging artifacts must not change that outcome.
- R7 — If the live save is missing or unreadable and a last-known-good save validates, the runtime must preserve the damaged live payload when one exists, promote the validated recovery payload to live, load it, and return Recovered with the restored player data.
- R8 — Damaged-live preservation is bounded to the latest quarantined payload. If the runtime cannot safely preserve the damaged live payload or promote the recovery payload, it must not destroy the only source data and must return an unrecovered result with developer diagnostics.
- R9 — If neither the live save nor last-known-good save validates, loading must remain Missing or Unreadable as appropriate; the runtime must not fabricate progress, silently delete the unreadable payload, or use staging data.
- R10 — Recovered must be a first-class load outcome distinct from Loaded, Missing, and Unreadable in both runtimes. Player-facing recovery meaning and developer-facing diagnostic detail must remain separate.

#### Player feedback and lifecycle

- R11 — A successful automatic recovery must show a non-blocking notice that a previous valid save was restored and that recent progress may be missing. Recovery-pending state must survive another startup if the process stops before the notice can be delivered, and the notice must remain available until a later player-progress save succeeds.
- R12 — The first later successful player-progress save must clear the outstanding recovery notice and return subsequent save feedback to the normal saved state.
- R13 — An authorized New Game replacement must clear or invalidate live, last-known-good, staging, and quarantine artifacts before any old payload can become eligible again. A cleanup failure must not silently allow old progress to reappear.
- R14 — Every currently supported save format and version must remain loadable without manual intervention. This feature must not require a format migration unless planning demonstrates that the contract cannot otherwise be met.

#### Verification parity

- R15 — Console and Unity may use different filesystem APIs, but they must implement the same observable load precedence, result statuses, artifact eligibility, recovery notice semantics, and New Game invalidation behavior.
- R16 — Each runtime must provide deterministic failure injection for staged-write interruption, staged validation failure, last-known-good preservation failure, live promotion failure, interruption after promotion but before cleanup, recovery promotion failure, and interruption after recovery promotion but before notice delivery. Tests must assert both the resulting status and which exact payload remains recoverable.
- R17 — Existing normal save/load/delete behavior, isolated test paths, supported-version loading, and preservation of unreadable source bytes must remain covered by regression tests.

### Actors

- A1 — Player: expects progress to resume without managing files or answering a recovery prompt, and needs an honest signal when recent progress may have been lost.
- A2 — Persistence boundary: serializes, validates, stages, promotes, classifies, recovers, quarantines, and invalidates save artifacts.
- A3 — Runtime startup and UI: consumes the typed load outcome, continues with restored data when available, and presents the appropriate non-blocking notice or New Game path.
- A4 — Developer or test author: receives diagnostic details and uses deterministic failure points to prove interruption behavior without timing-dependent process crashes.

### Key Flows

#### F1 — Normal save

The persistence boundary writes and validates a staged candidate, preserves a valid prior live payload as last-known-good, promotes the candidate atomically, and performs best-effort cleanup. A failure before promotion leaves the prior valid state eligible; a failure after successful promotion leaves the complete candidate eligible.

#### F2 — Automatic startup recovery

~~~mermaid
flowchart TB
    Start["Load request"] --> Live{"Live save valid?"}
    Live -->|Yes| Loaded["Loaded"]
    Live -->|No| Recovery{"Last-known-good valid?"}
    Recovery -->|No| NoRecovery["Missing or Unreadable"]
    Recovery -->|Yes| Preserve["Preserve damaged live if present"]
    Preserve --> Promote["Promote validated recovery payload"]
    Promote --> Recovered["Recovered and show non-blocking notice"]
~~~

A valid live payload always wins. Only when live is missing or unreadable may a validated last-known-good payload be promoted. Staging never enters the precedence chain.

#### F3 — Unrecoverable load

When no validated recovery payload exists, the runtime preserves unreadable evidence, returns Missing or Unreadable, and leaves the existing New Game path available. A failed recovery operation does not claim success and does not discard the only candidate payload.

#### F4 — Authorized New Game replacement

New Game invalidates every artifact that could revive old progress before the replacement state becomes authoritative. Once this boundary succeeds, a later startup cannot recover the previous live, last-known-good, staging, or quarantine payload.

### Acceptance Examples

| ID | Covers | Given / When / Then |
|---|---|---|
| AE1 | R1–R6 | Given a valid live save, when a normal save completes, then the full candidate is live, the prior valid payload is the sole last-known-good copy, and load returns Loaded. |
| AE2 | R1–R5, R16 | Given a valid live save, when execution is interrupted while writing staging, then the prior live payload remains loadable and the partial staging payload is ignored. |
| AE3 | R2–R5, R16 | Given a complete but invalid staged candidate, when validation fails, then neither live nor last-known-good is replaced. |
| AE4 | R3–R6, R16 | Given promotion completed but cleanup did not, when the game starts, then the complete new live payload loads normally and stale staging does not override it. |
| AE5 | R7–R12 | Given an unreadable live payload and valid last-known-good payload, when loading starts, then the damaged live payload is quarantined, recovery is promoted, load returns Recovered, play continues, and the recovery notice appears. |
| AE6 | R7, R10, R11 | Given a missing live payload and valid last-known-good payload, when loading starts, then recovery is promoted without fabricating a quarantine payload and returns Recovered. |
| AE7 | R8–R10 | Given an unreadable live payload and no valid last-known-good payload, when loading starts, then it returns Unreadable, preserves the unreadable bytes, and offers the existing New Game path. |
| AE8 | R7, R8, R16 | Given valid recovery data, when quarantine or recovery promotion fails, then the runtime does not claim Recovered, does not destructively overwrite its only source, and reports diagnostic context. |
| AE9 | R11, R12, R16 | Given recovery promotion completed but the process stopped before notice delivery, when the game starts again, then the recovery notice is still available; when the next player-progress save succeeds, then it clears and later save feedback is normal. |
| AE10 | R13 | Given any mixture of live, last-known-good, staging, and quarantine artifacts, when New Game replacement succeeds, then none of the old payloads can be loaded or recovered later. |
| AE11 | R14, R15, R17 | Given any currently supported save version in either runtime, when it is loaded and later saved, then it remains usable and joins the same durability lifecycle without manual migration. |

### Success Criteria

- SC1 — Equivalent deterministic failure matrices pass for console and Unity at every boundary named in R16.
- SC2 — No accepted test outcome exposes a partial candidate as live or loses the only validated last-known-good payload before a replacement becomes authoritative.
- SC3 — Both runtimes return and consume Recovered, keep developer diagnostics separate, and assert the recovery notice lifecycle through the next successful player-progress save.
- SC4 — Normal save/load/delete, unreadable-save preservation, supported-version compatibility, and authorized New Game regression suites pass in both runtimes.
- SC5 — Recovery storage remains bounded to one last-known-good payload and one latest quarantined damaged payload, excluding transient staging.

### Scope Boundaries

In scope:

- Console and Unity save staging, validation, atomic promotion, and one-generation last-known-good lifecycle.
- Automatic recovery, typed recovery results, damaged-live quarantine, and non-blocking recovery notice lifecycle.
- Deterministic failure injection and equivalent cross-runtime regression coverage.
- New Game invalidation of all save-envelope artifacts.

Out of scope:

- Manual Save or Save & Quit controls.
- Cloud synchronization, multi-device conflicts, or remote backups.
- Session, expedition, checkpoint, or progression-loop redesign.
- A generalized serialization, persistence, journaling, or multi-generation backup framework.
- Recovery from physical-device loss or corruption after the operating system reports a successful durable operation.

### Dependencies and Assumptions

- The target filesystems provide a same-volume atomic replacement primitive or an equivalent platform-specific sequence; planning must verify the exact .NET and Unity APIs and their failure behavior.
- Save locations can host sibling staging, last-known-good, and quarantine artifacts under the same application-controlled directory; planning owns their exact names and cleanup mechanics.
- Existing format, version, and semantic validation remains the authority for whether a payload is eligible. A new checksum or envelope schema is not assumed by this contract.
- Automatic recovery may lose progress newer than the last validated snapshot, so the player notice must state that possibility without blocking play.
- Recovery diagnostics may identify an operation and artifact role, but must not expose raw save contents to the player-facing message.

### Outstanding Questions

Resolve Before Planning: None.

Deferred to Planning:

- Which .NET and Unity filesystem APIs, same-directory layout, and flush behavior satisfy R3 on supported platforms?
- What narrow injectable file-operation boundary enables every deterministic failure point in R16 without creating a generalized persistence framework?
- Which cleanup operations are best effort, and which must fail the save or recovery result to preserve the contract?

### Sources and Research

- Product seed: docs/ideation/2026-08-06-open-ideation.html#idea-2.
- Prior scope boundary: docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md and .flow/tasks/저장-ux-보강/plan.md.
- Console implementation and results: src/ToilRelic/Systems/SaveSystem.cs and src/ToilRelic/Systems/SaveResults.cs.
- Unity implementation and results: unity/Assets/Scripts/Save/SaveService.cs and unity/Assets/Scripts/Save/SaveResults.cs.
- Console regression evidence: tests/ToilRelic.Tests/SaveSystemTests.cs.
- Unity regression evidence: unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs.
- Existing save-failure learning: docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md.

---

## Planning Contract

### Planning Resolution

| Product question | Resolution |
|---|---|
| Atomic replacement, layout, and flush | Derive fixed sibling artifacts from the configured live path. Write and flush a stage, validate it, then use File.Replace(stage, live, lkg) for an existing valid live or a same-directory no-overwrite move when live is missing. Never fall back to copy-overwrite or delete-then-move. |
| Deterministic injection seam | Add one runtime-local, save-specific filesystem operation boundary with named logical checkpoints. Console state is instance-scoped; Unity state stays private static and is restored by every test fixture. |
| Cleanup failure policy | Candidate validation, LKG preservation, quarantine preservation, recovery-marker creation, promotion, notice-marker clearing after a progress save, and New Game invalidation are correctness-critical. Removing stale staging after a committed promotion is best effort. A thrown primitive has an unknown commit state and is reconciled by the next validating load rather than a destructive rollback. |

### Key Technical Decisions

#### KTD1 — Fixed sibling artifact set

Derive every artifact from the one configured live path so all operations remain in one directory and on one volume:

| Role | Path convention | Eligible to load? | Retention |
|---|---|---:|---|
| Live | existing save path | Yes, first priority | One |
| Stage | live path plus .stage | Never | One transient slot |
| Last-known-good | live path plus .lkg | Yes, only if live is missing or invalid | One rotating generation |
| Quarantine | live path plus .quarantine | Never | Latest damaged live only |
| Recovery marker | live path plus .recovery-pending | No; notice state only | One zero-content marker |

The marker is not a journal and never proves that a payload is valid. Staging, quarantine, and marker files cannot change load precedence.

#### KTD2 — One pure candidate validator per runtime

Extract current format, version, and semantic checks from the public Load path into a side-effect-free validator that can read live, stage, and LKG candidates without moving, deleting, or recovering files. Public Load orchestrates precedence and recovery around that validator. This prevents staged validation from recursively invoking recovery and keeps current and legacy acceptance identical.

#### KTD3 — Validated promotion with no destructive fallback

Normal save follows one of three paths after the stage is fully written, flushed where the supported runtime exposes a disk-flush primitive, and validated:

1. Valid live: File.Replace(stage, live, lkg) performs the live replacement and rotates the displaced valid live into the single LKG slot.
2. Missing live: a same-directory, no-overwrite rename promotes stage to live and leaves any existing LKG untouched.
3. Existing invalid live: fail the save without rotating the invalid bytes into LKG or overwriting live; Load owns quarantine and recovery.

File.Replace is treated as one atomic promotion primitive even though tests expose separate logical “preserve LKG” and “promote live” checkpoints. A caught exception may occur before or after the filesystem mutation. The code reports failure, does not attempt a destructive rollback, and lets the next Load validate the observable artifacts.

#### KTD4 — Recovery is a validating normalization transaction

If live is missing or invalid and LKG validates:

1. Read the bytes that were validated as LKG, write those same bytes into the stage slot, flush, and validate the stage again. Under the single-writer assumption, source identity from validation through stage creation is an invariant.
2. Durably create the recovery-pending marker if it is absent: open the fixed marker path with create-new semantics, write zero content, call the runtime's flush-to-disk primitive, and close before any quarantine or promotion. An existing regular marker is idempotent success; a directory, open, create, flush, or close failure is a recovery failure and never authorizes notice by itself.
3. If invalid live exists, replace the previous quarantine only after its deletion succeeds, then move the invalid live to quarantine. Failure aborts before recovery promotion.
4. Promote stage to the now-missing live path with a same-directory no-overwrite move, retaining LKG.
5. Return Recovered with player data, diagnostic context, and RecoveryNoticePending=true.

If recovery stops after marker creation, the next load retries from validated artifacts. If it stops after promotion, the next load sees valid live and returns Loaded with RecoveryNoticePending=true. Marker presence without a valid live never produces a recovery notice by itself.

#### KTD5 — Explicit load and notice matrix

| Live | LKG | Marker | Result | Notice | Mutation |
|---|---|---|---|---|---|
| Valid | Any | Absent | Loaded | False | Ignore or best-effort clean stage only |
| Valid | Any | Present | Loaded | True | No recovery; retain marker |
| Missing | Missing | Any | Missing | False | Ignore marker and non-candidate artifacts |
| Missing | Invalid | Any | Unreadable | False | Preserve invalid LKG |
| Invalid | Missing or invalid | Any | Unreadable | False | Preserve invalid bytes |
| Missing | Valid | Any | Recovered | True | Establish marker, promote validated recovery stage |
| Invalid | Valid | Any | Recovered | True | Establish marker, quarantine invalid live, promote recovery |

An interrupted recovery that already promoted produces the first row with a present marker on restart: Loaded plus pending notice, not another Recovered result. Missing and Unreadable may carry developer diagnostics when a recovery attempt or file operation failed.

#### KTD6 — Notice state is independent of load and save feedback

Extend each load result with RecoveryNoticePending rather than overloading the status. Recovered and Loaded are both playable. Use the same player copy in both runtimes:

> Recovered a previous valid save. Recent progress may be missing.

The notice is shown once per startup without blocking play, but display does not clear the durable marker. The first later player-progress save clears the marker after candidate promotion. Marker deletion is completion-critical: if it fails, the new live remains valid but the save returns a conservative failure and the recovery notice stays pending for retry.

Unity carries recovery through dedicated semantic event/state alongside, not inside, SaveFeedbackStatus. GameStatusController composes recovery notice, ordinary success, and persistent save failure without relying on event ordering or string inspection. Console writes the notice in the existing title/startup flow and keeps developer diagnostics on the diagnostic channel.

The rendered priority is explicit so no later gameplay or save message silently erases recovery state:

| Surface | Recovery only | Recovery plus save failure | After successful clearing save |
|---|---|---|---|
| Console startup/title | Print the recovery notice once for this startup before the normal menu. | Keep the recovery notice in the startup transcript; print save failure separately on the diagnostic/save channel. | Do not print recovery on the next startup. |
| Unity Title | Show recovery as the primary retained startup status without blocking Continue. | Keep recovery visible and compose the save failure as a separate status; failure never overwrites recovery state. | Remove recovery state and render normal save/title feedback. |
| Unity Camp | Keep recovery in the auxiliary status region. | Render recovery first and save failure second, with neither inferred from event order. | Remove recovery and retain ordinary save feedback. |
| Unity Battle | Retain semantic recovery state without adding a persistent battle-layout row. | Preserve both states and surface them again on the next Title or Camp render; battle gameplay remains unobstructed. | Clear retained recovery state and preserve existing battle feedback behavior. |

Worst-case composition is verified at 800x600 and 1280x720 with recovery, save failure, and any simultaneous level-up/gameplay status present. The evidence must show no clipping, overlap, or hidden primary action. If that cannot be achieved through existing bindings, stop before changing a scene or prefab and return to planning.

#### KTD7 — New Game deletes the last validated authority last

Every authorized New Game path calls full invalidation, including when Load returned Missing. Before deleting eligible payloads, use the pure validator to identify the current authoritative old payload: valid live if present, otherwise valid LKG. Abort without deleting either eligible slot if authority classification fails.

Delete stage, quarantine, invalid/non-authoritative payloads, and any other eligible-but-non-authoritative valid payload first; delete the authoritative valid payload at the data commit edge, then delete the recovery marker last. If neither live nor LKG is valid, delete evidence roles in a deterministic order, delete any marker last, and retain the same all-or-failure caller behavior. Any failure returns failure and keeps the caller at the title screen. Before the data commit edge, at least one validated old payload remains recoverable; after it, no old eligible payload remains. A marker-delete failure after that edge is still an operation failure for the current caller, but a later restart returns Missing because marker alone is never load or notice authority. Retrying the sequence removes the marker and is idempotent.

#### KTD8 — Shared behavioral vectors, runtime-local mechanics

Add one JSON fixture whose cases name the initial artifact labels, operation, injected checkpoint and whether it fires before or after mutation, expected operation/load status, authoritative payload label, surviving roles, and notice state. Console and Unity map those symbolic labels to their own current, legacy, corrupt, and partial bytes and execute the same case IDs.

The production seam is deliberately narrow: file existence/read/write/flush, replace-with-backup, same-directory promote, quarantine move, and delete, plus the seven named R16 checkpoints and correctness-critical marker, quarantine, and New Game operations. It is not public API and not a general filesystem abstraction. ReplaceLiveWithBackup remains one indivisible primitive; the logical LKG-preservation and live-promotion cases must not split it into copy-then-move.

Developer diagnostics use a strict allowlist: operation role, artifact role, exception type, and a redacted relative filename may be recorded. Raw save bytes, player fields, absolute filesystem roots, and unfiltered exception messages are forbidden in result diagnostics, player copy, and test snapshots.

The shared JSON is the only source of behavioral expectations. Runtime fixture readers translate symbolic payload labels into local bytes and derive role paths through one runtime-local helper; they do not duplicate case tables, expected states, or suffix construction.

The shared vectors map logical R16 boundaries to realizable filesystem states:

| Logical boundary | Concrete path / primitive | Injection side | Allowed state and reconciliation |
|---|---|---|---|
| Staged-write interruption | Stage FileStream write/flush | During write, before validation | Prior live/LKG unchanged; partial stage is ineligible |
| Staged validation failure | Pure validator over completed stage | Before promotion | Prior live/LKG unchanged; stage is ineligible |
| LKG preservation failure | Valid-live File.Replace(stage, live, lkg) | Before indivisible replace | Prior live remains authoritative; prior LKG, if any, remains unchanged |
| Live promotion failure | File.Replace for valid live; no-overwrite move for missing live | Before primitive, plus unknown-commit exception vector | Pre-mutation leaves old authority; unknown commit is settled by validating the observed old or complete-new state |
| Stop after promotion | Replace or no-overwrite move | After primitive | Complete candidate is live; valid-live path has displaced old live in LKG |
| Recovery promotion failure | Recovery-stage no-overwrite move | Before primitive, plus unknown-commit exception vector | Pre-mutation retains valid LKG; committed mutation restarts as valid live plus marker |
| Stop after recovery promotion | Recovery-stage no-overwrite move | After primitive, before result/UI | Restart returns Loaded plus pending notice |

Do not model “LKG rotated while the old live remains authoritative” as an intermediate File.Replace state. The valid-live replace is indivisible at the plan boundary.

#### KTD9 — Compatibility without migration

Do not change PlayerSaveData, PlayerState, the save envelope version, or existing supported validators. Existing saves enter the durability lifecycle on their next successful progress save. A legacy LKG is recovered byte-for-byte through the same legacy validator and upgrades only through the existing later-save behavior.

#### KTD10 — Initial platform guarantee is capability-bounded

The implementation and executable proof target .NET 8 on a local Windows volume for console and Unity 6 Windows Editor PlayMode using its local persistent-data directory for Unity. Runtime code remains compatible with Unity Standalone Windows, but Standalone does not inherit the durability guarantee in this plan because the Editor-only reflection seam cannot drive a built player through a real cold restart. All artifacts must be same-directory siblings. Configured UNC/network, removable, or otherwise unverified filesystems retain conservative failure behavior but do not inherit the ironclad guarantee. A target that cannot provide the required replace, same-directory promotion, and flush behavior fails safely and must not switch to in-place write, copy-overwrite, or delete-before-move.

Unity Standalone Windows and all other desktop/mobile targets require a separately scoped development-player fault harness plus target-specific cold-restart verification before inheriting the durability claim. WebGL is outside the initial guarantee because persistentDataPath uses an asynchronous IndexedDB-backed filesystem that requires a platform-specific synchronization bridge. tvOS is outside because persistentDataPath is unsupported and empty.

### Assumptions

These bets were not separately confirmed because scoping confirmation was intentionally skipped:

- The game remains a synchronous single-writer process. Concurrent processes, cloud writers, and multi-device conflict resolution remain outside scope.
- The existing live save override identifies an application-controlled directory where fixed sibling files are permitted.
- Capability evidence is collected on a local Windows filesystem. A console save override outside that baseline does not widen the guarantee merely because one replace call succeeds.
- The exact recovery message above fits the existing console output and Unity status surface without a scene or prefab change. If implementation reveals a layout blocker, stop before editing scene assets and return to planning.
- Windows Editor PlayMode is the current Unity release-validation baseline. Standalone and other Unity targets do not silently inherit the ironclad guarantee.

### High-Level Technical Design

These diagrams fix component and state boundaries, not class signatures.

#### Component topology

~~~mermaid
flowchart LR
    Console["Console Game"] --> CEnvelope["Console SaveSystem envelope"]
    UnityRuntime["Unity GameManager"] --> UEnvelope["Unity SaveService envelope"]
    CEnvelope --> CValidator["Console pure validator"]
    UEnvelope --> UValidator["Unity pure validator"]
    CEnvelope --> CFiles["Console file-operation seam"]
    UEnvelope --> UFiles["Unity file-operation seam"]
    CEnvelope -. "typed result" .-> Console
    UEnvelope -. "typed result" .-> UnityRuntime
    Vectors["Shared symbolic contract vectors"] --> CTests["xUnit matrix"]
    Vectors --> UTests["Unity PlayMode matrix"]
    CTests --> CEnvelope
    UTests --> UEnvelope
    UnityRuntime --> Events["Recovery semantic state/event"]
    Events --> Status["GameStatusController"]
~~~

The runtimes share observable semantics and vector IDs, not production code or serializers.

#### Normal save sequence

~~~mermaid
sequenceDiagram
    participant Caller
    participant Envelope
    participant Validator
    participant Files
    Caller->>Envelope: Save current player state
    Envelope->>Files: Write and flush stage
    Envelope->>Validator: Validate stage
    alt stage invalid or write fails
        Envelope-->>Caller: Failed; live and LKG unchanged
    else live valid
        Envelope->>Files: Replace stage -> live, old live -> LKG
        alt replace returns normally
            Envelope->>Files: Clear recovery marker if pending
            Envelope-->>Caller: Succeeded only after marker clears
        else replace throws; commit state unknown
            Envelope-->>Caller: Conservative failure; no rollback
            Note over Envelope,Files: Next Load validates old live or complete candidate live + displaced LKG
        end
    else live missing
        Envelope->>Files: Same-directory promote stage -> live
        alt move returns normally
            Envelope->>Files: Clear recovery marker if pending
            Envelope-->>Caller: Succeeded only after marker clears
        else move throws; commit state unknown
            Envelope-->>Caller: Conservative failure; no rollback
            Note over Envelope,Files: Next Load validates missing live or complete candidate live
        end
    else live invalid
        Envelope-->>Caller: Failed; recovery deferred to Load
    end
~~~

#### Recovery decision flow

~~~mermaid
flowchart TD
    Start["Validate live"] --> Live{"Live valid?"}
    Live -->|Yes, marker absent| Loaded["Loaded, notice false"]
    Live -->|Yes, marker present| Pending["Loaded, notice true"]
    Live -->|No| Lkg["Validate LKG"]
    Lkg -->|No candidates| Missing["Missing"]
    Lkg -->|Invalid source exists| Unreadable["Unreadable; preserve bytes"]
    Lkg -->|Valid| Stage["Stage the validated LKG bytes, flush, validate"]
    Stage -->|Write or validation fails| RetrySource["Unrecovered; retain valid LKG"]
    Stage -->|Valid| Mark["Durably establish pending marker"]
    Mark -->|Marker creation fails| RetrySource
    Mark --> Damaged{"Invalid live exists?"}
    Damaged -->|Yes| Quarantine["Replace bounded quarantine"]
    Damaged -->|No| Promote["Promote recovery stage"]
    Quarantine -->|Fails before mutation| RetryLive["Unrecovered; retain invalid live + valid LKG"]
    Quarantine -->|Move committed then interrupted| RetryQuarantine["Next Load: live missing, quarantine + valid LKG"]
    Quarantine -->|Succeeds| Promote
    Promote -->|Fails before mutation| RetrySource
    Promote -->|Committed then interrupted| Pending
    Promote -->|Succeeds| Recovered["Recovered, notice true"]
~~~

#### Recovery notice lifecycle

~~~mermaid
stateDiagram-v2
    [*] --> Clear
    Clear --> Pending: Recovery marker established
    Pending --> Pending: Notice displayed
    Pending --> Pending: Progress save or marker clear fails
    Pending --> Clear: Progress save promotes and marker clears
    Pending --> Pending: Restart loads valid live
~~~

#### New Game invalidation

~~~mermaid
sequenceDiagram
    participant UI as New Game caller
    participant Save as Persistence boundary
    UI->>Save: Delete all envelope artifacts
    Save->>Save: Validate live and LKG; select current authority
    Save->>Save: Delete stage and quarantine
    Save->>Save: Delete invalid and non-authoritative payloads
    Save->>Save: Delete the final validated authority at the data commit edge
    Save->>Save: Delete recovery marker last
    alt every deletion succeeds
        Save-->>UI: Success; clear retained recovery state; enter new game
    else any deletion fails
        Save-->>UI: Failure; retain current-call notice/title state
    end
~~~

### Implementation Sequence and Landing

~~~mermaid
flowchart LR
    U1["U1 Contracts and vectors"] --> U2["U2 Console envelope"]
    U1 --> U3["U3 Unity envelope"]
    U2 --> U4["U4 Recovery feedback"]
    U3 --> U4
    U4 --> U5["U5 New Game invalidation"]
    U4 --> Gate["Verification Contract"]
    U5 --> Gate
~~~

U2 and U3 may proceed in parallel after U1. U4 follows both persistence engines, and U5 follows U4 because Game.cs, GameManager.cs, and SampleSceneP0PlayModeTests.cs overlap; do not parallel-edit those units. Land the result as one aligned feature change: result statuses must not land without playable consumers, recovery must not land without the durable notice marker, and multi-artifact Delete must not land without both New Game callers.

### Research That Shapes the Plan

- Microsoft File.Replace replaces a destination, deletes the source, and creates or replaces a backup of the displaced destination; its same-volume constraint makes it the preferred valid-live promotion primitive.
- Microsoft documents that cross-volume File.Move becomes copy/delete and explicitly notes that copy-overwrite is not atomic. Fixed sibling paths prevent that fallback.
- FileStream.Flush(true) requests flushing intermediate buffers but does not create a universal physical-media or directory-metadata guarantee; the Product Contract remains bounded accordingly.
- Unity documents persistentDataPath as target-dependent, IndexedDB-backed on WebGL, and unsupported on tvOS. The platform guarantee therefore cannot be universal.
- Repository learnings require semantic retained-state events, isolated Unity save paths installed before scene load, static seam restoration, and shared behavioral vectors rather than string/order-only UI tests.

---

## Implementation Units

### U1. Establish cross-runtime result and vector contracts

- Goal: Create the shared vocabulary and test data that keep console and Unity behavior aligned before either filesystem engine changes.
- Requirements: R2, R10–R13, R15–R17; F1–F4; AE1–AE11.
- Dependencies: None. Do not land this scaffold without U2–U5 in the same feature change.
- Files:
  - src/ToilRelic/Systems/SaveResults.cs
  - unity/Assets/Scripts/Save/SaveResults.cs
  - unity/Assets/Tests/Fixtures/IroncladSaveEnvelopeContracts.json
  - unity/Assets/Tests/Fixtures/IroncladSaveEnvelopeContracts.json.meta
  - tests/ToilRelic.Tests/ToilRelic.Tests.csproj
  - tests/ToilRelic.Tests/IroncladSaveEnvelopeContractFixture.cs
  - unity/Assets/Tests/PlayMode/IroncladSaveEnvelopeContractFixture.cs
  - unity/Assets/Tests/PlayMode/IroncladSaveEnvelopeContractFixture.cs.meta
- Approach:
  - Add Recovered and RecoveryNoticePending while retaining status, player, and diagnostic separation.
  - Let Missing and Unreadable carry optional developer diagnostics for failed recovery attempts without introducing a product-level RecoveryFailed status.
  - Define symbolic payload labels, the seven required R16 checkpoints, and additional marker create/flush/delete, prior-quarantine delete, damaged-live move, New Game authority-classification I/O, and per-artifact New Game delete checkpoints with before/after-mutation effects in the shared JSON fixture.
  - Define diagnostic contract fields as allowlisted symbolic roles and redacted relative filenames; fixture expectations must reject raw bytes, player fields, absolute roots, and unfiltered exception messages.
  - Link the Unity-owned fixture into the console test output using the existing fixture pattern.
  - Keep the JSON as the sole expectation table; runtime readers only translate labels and expose cases.
- Test scenarios:
  - Both fixture readers load the identical ordered case IDs and reject missing required fields or unknown artifact roles.
  - Loaded and Recovered require player data; Recovered always has pending notice; Missing and Unreadable never fabricate player data.
  - A restarted recovered state can be represented as Loaded plus pending notice without changing precedence.
- Verification:
  - Console and Unity compile against the extended result shape.
  - Fixture contract tests compare schema and the ordered case-ID set. Executed behavioral parity belongs to U2, U3, and the final cross-runtime gate.

### U2. Implement the console save and recovery envelope

- Goal: Replace the console temporary overwrite with validated staging, single-LKG rotation, automatic recovery, bounded quarantine, and deterministic reconciliation.
- Requirements: R1–R12, R14, R16, R17; F1–F3; AE1–AE9 and AE11.
- Dependencies: U1.
- Files:
  - src/ToilRelic/Systems/SaveSystem.cs
  - src/ToilRelic/Systems/SaveEnvelopeFileOperations.cs
  - tests/ToilRelic.Tests/SaveSystemTests.cs
  - tests/ToilRelic.Tests/IroncladSaveEnvelopeTests.cs
- Approach:
  - Factor the existing schema, legacy, and semantic checks into one pure candidate validator.
  - Add an instance-scoped internal operation seam and checkpoint hook; keep public SaveSystem construction compatible for production.
  - Write stage with an explicit FileStream and flush-to-disk request, then apply KTD3 promotion.
  - Apply KTD4 recovery and KTD5 classification; never delete invalid evidence or promote staging as recovery.
  - Own recovery-marker creation, restart classification, and post-promotion marker clearing in the persistence result. Keep ReplaceLiveWithBackup indivisible.
  - Keep cleanup after a committed save limited to stale stage removal; never guess rollback state after an exception.
- Test scenarios:
  - Normal save rotates exact old live bytes to LKG and loads the new candidate.
  - Interrupted stage write and invalid completed stage leave live and LKG byte-for-byte unchanged.
  - Injected LKG-preservation and pre-promotion failures keep the prior live authoritative.
  - Injected after-promotion interruption loads the complete candidate as Loaded, retains the old live as LKG, and observes that the consumed source stage is absent. A separate load vector seeds an orphan stage and proves it is ignored.
  - Saving while live is invalid fails without changing live or LKG; a valid live replaces an invalid pre-existing LKG with the displaced valid live.
  - Marker creation failure stops before quarantine or promotion.
  - Marker create/flush failures cover absent, already-present, and path-is-directory states; only a closed, successfully flushed regular marker permits recovery to continue.
  - Missing-live and invalid-live recovery both return Recovered; only invalid live produces quarantine.
  - Existing-quarantine deletion or quarantine move failure returns unrecovered diagnostics and preserves live or quarantine plus valid LKG for retry.
  - Recovery promotion failure does not claim Recovered; after-promotion interruption restarts as Loaded plus pending notice.
  - A failed later progress save retains pending notice; marker deletion failure after data promotion reports conservative failure; the next successful save clears it.
  - Retrying after a partial stage safely reuses the fixed stage slot and completes.
  - Every injected failure is followed by a fresh Load assertion and exact surviving-byte checks, not only the immediate operation result.
  - Diagnostics at every injected failure contain only the allowlisted operation/artifact roles, exception type, and redacted relative filename; tests seed distinctive player data and absolute roots and assert they never appear.
  - Missing live plus invalid LKG is Unreadable; no live and no LKG is Missing; stage, quarantine, and marker alone never change those statuses.
  - Current and legacy bytes load through the existing validator, and legacy LKG recovery does not rewrite its format until a later save.
- Verification:
  - Every shared vector runs from an isolated temporary directory and asserts exact bytes for live, LKG, stage, quarantine, and marker.
  - Existing SaveSystem tests remain green, including unreadable-byte preservation and path failure behavior.

### U3. Implement the Unity save and recovery envelope

- Goal: Give Unity the same persistence outcomes as U2 without introducing runtime assembly references into PlayMode tests.
- Requirements: R1–R12, R14–R17; F1–F3; AE1–AE9 and AE11.
- Dependencies: U1.
- Files:
  - unity/Assets/Scripts/Save/SaveService.cs
  - unity/Assets/Scripts/Save/SaveEnvelopeFileOperations.cs
  - unity/Assets/Scripts/Save/SaveEnvelopeFileOperations.cs.meta
  - unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs
  - unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs.meta
  - unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs
  - unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs
- Approach:
  - Preserve savePathOverride as the only path override; derive every sibling from it.
  - Factor JsonUtility version/presence/value validation into one pure candidate validator.
  - Add a private static save-specific operation seam and checkpoint hook, reachable to PlayMode tests through reflection and reset with path state in all teardown and setup-failure paths.
  - Create the parent directory before staging, then apply the same promotion, recovery, quarantine, marker, and ambiguous-exception rules as console.
  - Own marker creation, restart classification, and marker clearing inside SaveService; keep the valid-live replace-with-backup primitive indivisible.
  - Replace missing-directory-based save failure fixtures that become invalid after directory creation with named deterministic failures.
- Test scenarios:
  - Execute every U2 matrix scenario with the same shared case IDs and expected artifact labels.
  - Run direct, scene-free reflection tests for SaveService so persistence failures are isolated from scene behavior.
  - Prove a stale static checkpoint cannot leak into the next test, including a setup failure and two focused runs in fresh Unity processes.
  - Confirm Unity versionless, v1, v2, and v3 saves remain loadable and recoverable without a schema migration.
  - Confirm unsupported or failed promotion capability returns failure without in-place or destructive fallback.
  - Cover invalid-live Save rejection, invalid prior LKG replacement, marker creation/deletion failure, partial-stage retry, and fresh-Load byte assertions from U2 with the same case IDs.
  - Cover the same durable marker create/flush states as console through the runtime-local operation seam.
  - Run the same diagnostic-redaction vectors as console and assert no player field, raw payload fragment, absolute persistentDataPath, or unfiltered exception message escapes.
- Verification:
  - Focused SaveEnvelopeContracts PlayMode tests pass twice in fresh processes.
  - Existing PlayMode action-save failures still reach SaveFeedbackStatus.Failed through production actions after their fixtures move to the new seam.

### U4. Integrate playable recovery and non-blocking feedback

- Goal: Make automatic recovery playable and visible while keeping recovery notice, ordinary save success, save failure, and developer diagnostics independent.
- Requirements: R6, R7, R10–R12, R15–R17; F2 and F3; AE5, AE6, AE8, AE9.
- Dependencies: U2 and U3.
- Files:
  - src/ToilRelic/Game.cs
  - tests/ToilRelic.Tests/GameSaveUxTests.cs
  - unity/Assets/Scripts/Core/GameManager.cs
  - unity/Assets/Scripts/Core/GameEvents.cs
  - unity/Assets/Scripts/UI/GameStatusController.cs
  - unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs
- Approach:
  - Treat Recovered and Loaded as playable in both runtime orchestrators.
  - Show the KTD6 recovery copy for Recovered and for Loaded with pending notice; do not expose raw diagnostics in player copy.
  - In Unity, add dedicated recovery semantic state/event and retain it independently from SaveFeedbackStatus and the primary gameplay message.
  - Consume the persistence result without manipulating marker files. After Save returns success, clear retained in-memory/UI pending state and emit ordinary success; on failure, retain both the notice and failure indication.
  - In Unity, retain the pending boolean in GameManager and publish the semantic recovery event after subscriptions are established, avoiding Awake ordering as a correctness dependency.
  - Log successful recovery as recovery context, not as “save load failed.”
  - Follow the KTD6 surface-priority table. New Game must not clear retained in-memory/UI recovery state directly; U5 clears it only after the full disk invalidation operation succeeds.
- Test scenarios:
  - Console recovery continues with restored player data and prints one non-blocking notice per startup while keeping diagnostics separate.
  - Unity Title/Continue accepts Recovered, initializes the restored player, and displays recovery state without requiring a new scene binding.
  - Restart after recovery promotion produces Loaded plus the same pending notice.
  - Display alone does not clear pending state; a failed progress save retains it; the first successful later save clears it and restores normal feedback.
  - Recovery notice and persistent save failure compose correctly regardless of event order.
  - Title and Camp render recovery plus save failure plus simultaneous gameplay or level-up feedback without clipping or hiding a primary action at 800x600 and 1280x720; Battle retains both semantic states without adding a persistent row.
- Verification:
  - Console output tests assert player-visible copy and diagnostic channel separately.
  - Unity production-path tests assert semantic event/state, restored player, and rendered status instead of matching event order or private strings alone.

### U5. Make New Game invalidate the complete envelope

- Goal: Ensure an authorized replacement cannot resurrect any old live, LKG, stage, quarantine, or marker state after success while retaining one validated old authority until the data commit edge and deleting notice state last.
- Requirements: R13, R15–R17; F4; AE10.
- Dependencies: U2, U3, and U4.
- Files:
  - src/ToilRelic/Systems/SaveSystem.cs
  - src/ToilRelic/Game.cs
  - tests/ToilRelic.Tests/SaveSystemTests.cs
  - tests/ToilRelic.Tests/GameSaveUxTests.cs
  - unity/Assets/Scripts/Save/SaveService.cs
  - unity/Assets/Scripts/Core/GameManager.cs
  - unity/Assets/Tests/PlayMode/IroncladSaveEnvelopePlayModeTests.cs
  - unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs
- Approach:
  - Classify live and LKG without mutation; abort before eligible deletion if authority cannot be determined.
  - Make Delete idempotently remove non-authoritative artifacts, delete the final validated authority at the data commit edge, and delete the recovery marker last, with no best-effort classification inside an authorized New Game.
  - Invoke Delete from every New Game path, including Missing, because LKG can exist without live.
  - Publish one semantic recovery-state clear and enter new gameplay only after every deletion, including marker deletion, succeeds; otherwise retain the current-call notice/title state and diagnostic feedback.
- Test scenarios:
  - Every artifact present: successful New Game removes all envelope paths and a restart returns Missing.
  - Missing live with valid LKG: New Game still invalidates LKG and cannot recover it later.
  - Injected I/O failure while classifying live or LKG aborts before deleting any eligible or evidence artifact and leaves retained recovery/UI state unchanged.
  - Failure injected at every deletion boundary remains at title; before the data commit edge at least one validated old payload remains recoverable, and retry completes idempotently.
  - Valid-live authority and missing/invalid-live with valid-LKG authority both preserve their selected authority until the data commit edge.
  - Deletion of the last validated authority is the data commit edge. If marker deletion then fails, the current call fails and retains in-memory notice/title state, a restart returns Missing because marker alone has no authority, and retry removes the marker.
  - Successful invalidation clears retained recovery state exactly once; failures before or after the data commit edge do not publish a false success transition.
  - No successful return leaves an old eligible payload or pending marker.
  - Existing valid-save and unreadable-save replacement flows remain behaviorally intact.
- Verification:
  - Console and Unity execute the shared New Game vectors and assert every path, including valid-live authority, invalid-live with valid-LKG authority, and missing-live with valid-LKG authority.
  - Full title, load, save-feedback, and PlayMode suites pass after the Missing shortcuts are removed.

---

## System-Wide Impact

### Data lifecycle

- Existing live saves are read without mutation. The first later successful save creates or rotates LKG and enrolls the save in the envelope.
- Recovery preserves exact source bytes in LKG, moves one damaged live to quarantine, normalizes live from a separately validated stage, and persists notice state outside the player schema.
- A successful progress save after recovery replaces live, rotates the recovered live to LKG, and clears the notice marker.
- New Game is the only operation that deliberately removes every envelope artifact.

### Failure propagation

- Persistence returns typed status/player/notice plus developer diagnostic context. Runtime orchestration decides playable versus title behavior.
- A pre-mutation failure preserves the previous authoritative state. An after-mutation failure may report failure even though the candidate is now live; the next load validates reality rather than trusting the exception boundary.
- Cleanup cannot overwrite or delete the only validated candidate. Stale stage cleanup is best effort; marker clear after a later save and every New Game deletion are completion-critical.

### Compatibility and migration

- No save schema or player model migration is introduced.
- Console current and legacy formats and Unity versionless/v1/v2/v3 formats retain their existing validation authority.
- Old application versions ignore sibling artifacts and continue reading live, but no longer maintain the durability envelope after rollback. Rollback therefore preserves load compatibility but suspends the new guarantee.

### UI and orchestration

- Console and Unity startup branches broaden playable status to include Recovered.
- Unity gains semantic recovery state/event but no new interaction flow, prompt, scene, or manual save control.
- Existing autosave triggers remain unchanged.

### Test isolation

- Console injection state is instance-local.
- Unity path and fault state are private static test seams and must be restored even when setup or assertion fails.
- Shared fixture labels prove semantic parity; each runtime still asserts its own raw bytes and filesystem roles.

---

## Risks and Dependencies

| Risk or dependency | Consequence | Mitigation / stop condition |
|---|---|---|
| File.Replace or same-directory promotion differs on a target | False atomicity claim or live loss | Support only capability-verified targets; fail safely; never use destructive fallback. |
| Flush reports success without full power-loss durability | Guarantee exceeds portable APIs | Keep the Product Contract bounded to process/filesystem-operation failure and do not claim physical-media durability. |
| A primitive mutates state before throwing | In-process result disagrees with disk | Report conservative failure, retain candidates, and reconcile by validation on next Load. |
| Marker clear fails after data promotion | New data is live but notice persists | Return conservative save failure, keep marker, and retry on the next progress save. |
| Quarantine replacement fails | Damaged live cannot be preserved safely | Abort recovery before promotion and retain valid LKG for retry. |
| New Game is interrupted during deletion | Partial artifact set remains or notice clears before old data is gone | Validate authority first, delete the last valid eligible payload at the data commit edge, then delete the marker last. Marker-only restart is Missing; current-call failure stays at title and retry is idempotent. |
| Unity static seam leaks between tests | False failures or false confidence | Restore hook and path in teardown/failure paths; run focused category twice in fresh processes. |
| Directory creation changes existing failure tests | Action tests begin succeeding unexpectedly | Replace path-shape failures with explicit deterministic operation faults. |
| Concurrent external writer | Validated bytes can change before promotion | Explicitly retain synchronous single-writer assumption; stop rather than add locking/cloud coordination. |
| Standalone, WebGL/tvOS, or another unverified player target | Platform or test seam cannot prove the Editor contract | Exclude from the initial guarantee; require a separate development-player fault harness and target-specific cold-restart evidence. |

---

## Verification Contract

### Static and console gates

~~~powershell
dotnet build src/ToilRelic/ToilRelic.csproj
dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo --filter "FullyQualifiedName~IroncladSaveEnvelope|FullyQualifiedName~SaveSystemTests|FullyQualifiedName~GameSaveUxTests" --logger "trx;LogFileName=work-save-envelope-console.trx" --results-directory .flow/tasks/ironclad-save-envelope
dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo
~~~

Required outcomes:

- Build succeeds with no new warnings attributable to the feature.
- The focused suite proves all shared vector IDs, byte-level artifact expectations, notice lifecycle, and New Game retry behavior.
- The full suite proves no regression to gameplay saves, supported-version loading, title flow, or save feedback.

### Unity focused gates

Use the Unity executable for 6000.3.19f1. Do not add -quit; follow the repository’s established batchmode behavior.

~~~powershell
& '<Unity.exe>' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -testCategory 'SaveEnvelopeContracts' -testResults 'C:\Toil-Relic-main\.flow\tasks\ironclad-save-envelope\work-save-envelope-r1-results.xml' -logFile 'C:\Toil-Relic-main\.flow\tasks\ironclad-save-envelope\work-save-envelope-r1.log'
& '<Unity.exe>' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -testCategory 'SaveEnvelopeContracts' -testResults 'C:\Toil-Relic-main\.flow\tasks\ironclad-save-envelope\work-save-envelope-r2-results.xml' -logFile 'C:\Toil-Relic-main\.flow\tasks\ironclad-save-envelope\work-save-envelope-r2.log'
~~~

Required outcomes:

- Both fresh-process runs pass with the same sorted shared case-ID set.
- A reset sentinel directly proves savePathOverride and the static fault hook returned to defaults after the first run; the second process then executes the same cases without inherited state.
- Every R16 checkpoint asserts status, authoritative payload, surviving artifact roles, and notice state.

### Unity full regression gate

~~~powershell
& '<Unity.exe>' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults 'C:\Toil-Relic-main\.flow\tasks\ironclad-save-envelope\work-playmode-full-results.xml' -logFile 'C:\Toil-Relic-main\.flow\tasks\ironclad-save-envelope\work-playmode-full.log'
~~~

Required outcomes:

- The full PlayMode assembly passes.
- Existing Purposeful Hunt, equipment, title, New Game, and save-failure action contracts remain green.
- Unity logs contain no unhandled exception, serialization error, or static-hook leakage.

### Cross-runtime parity gate

- Include the shared case ID in each generated test name. Compare the console TRX and Unity XML exact sorted case-ID sets; there must be no missing or runtime-only vector and no skipped or inconclusive case.
- For each case, compare expected operation status, next-load status, authoritative payload label, artifact eligibility, and RecoveryNoticePending.
- Runtime-specific diagnostic text and serializer bytes may differ; player-visible recovery copy and observable lifecycle may not.

### Manual behavior gate

- Console: start from an automatically recovered fixture, confirm play continues, the recovery notice is visible without a prompt, a failed later save retains it, and a successful later save restores normal save feedback.
- Unity Editor: repeat the same path through the title screen and status UI, then restart once before the clearing save to prove the notice survives.
- Record commands, result files, and manual observations in .flow/tasks/ironclad-save-envelope/qa.md. These gates are instructions for implementation; no test is claimed as run by this planning pass.

---

## Documentation and Operational Notes

- Update unity/UNITY_SETUP.md with the SaveEnvelopeContracts focused command and the supported-target caveat if the final test category is added.
- Keep CONCEPTS.md definitions for Save Load Status and Last-Known-Good Save aligned with the implemented result names.
- No data migration or one-time cleanup job is required. Orphan stage is ignored; LKG/quarantine/marker are fixed bounded siblings.
- Rollback to old code remains live-save compatible but ignores the safety artifacts and stops rotating LKG. Do not delete those artifacts during rollback unless the user explicitly starts New Game.
- Do not claim WebGL, tvOS, macOS, Linux, iOS, or Android durability from Windows Editor evidence alone.
- Do not claim Unity Standalone Windows durability from Editor PlayMode evidence. Capture a follow-up for a separately scoped development-player fault harness and cold-restart capability proof before widening the guarantee.

---

## Definition of Done

### Global

- [ ] Product Contract R1–R17 and acceptance examples AE1–AE11 remain semantically unchanged and are traced to implementation units and tests.
- [ ] Console and Unity implement the same load precedence, status/notice matrix, bounded artifacts, automatic recovery, and New Game data-commit/marker-last deletion order.
- [ ] All seven deterministic R16 failure boundaries pass in both runtimes with exact artifact and next-load assertions.
- [ ] Current and legacy save formats remain loadable without a schema migration.
- [ ] Recovery is playable, non-blocking, restart-durable, and clears only after a later successful progress save.
- [ ] Platform claims are limited to the verified baseline and no destructive fallback exists.
- [ ] Verification Contract gates pass and Personal Flow review.md and qa.md contain the corresponding evidence before review or QA is claimed complete.
- [ ] No abandoned experiment, duplicate validator, unused fault hook, temporary fixture, or unbounded artifact remains in the final diff.
- [ ] Console and Unity feature changes land together in one logical commit or PR with affected paths and verification evidence recorded.

### Per unit

- [ ] U1: Both runtimes consume the same complete behavioral vector set and expose compatible typed results.
- [ ] U2: Console save/load recovery passes byte-level deterministic and legacy regression coverage.
- [ ] U3: Unity save/load recovery passes the same vectors twice in fresh processes with no static-state leakage.
- [ ] U4: Recovered data is playable and the independent notice lifecycle composes with save feedback in both runtimes.
- [ ] U5: Every New Game path performs idempotent full invalidation and cannot resurrect old progress after success.

---

## Engineering Review

Reviewed on 2026-08-11 against the requirements-only Product Contract, current console and Unity save code, existing persistence/UI tests, repository learnings, and official .NET/Unity filesystem documentation. All three findings were accepted through the user's instruction to apply the recommended direction and were folded into this plan. No implementation or QA result is claimed by this review.

### Step 0: Scope challenge

The plan exceeds the generic eight-file/two-class smell threshold, but the breadth is irreducible: the repository has two separately shipped runtimes, each needs a narrow filesystem seam and behavior tests, and the shared vector fixture prevents semantic drift. Splitting out result consumption, recovery notice, or New Game invalidation would create an unsafe intermediate state. Scope is accepted as-is while retaining the U1 -> U2/U3 -> U4 -> U5 dependency order.

### What already exists

| Existing flow | Reuse decision |
|---|---|
| Console SaveSystem current/legacy validators and SaveSystemTests | Extract the checks into one pure candidate validator; do not create a second serializer or schema. |
| Unity SaveService validation, savePathOverride, and versionless/v1/v2/v3 tests | Preserve the path override and validation authority; replace only the write/recovery envelope. |
| Console Game and Unity GameManager title/New Game orchestration | Broaden playable status to Recovered and route all authorized replacement through the existing callers. |
| GameEvents and GameStatusController retained save-failure behavior | Add semantic recovery state alongside SaveFeedbackStatus; do not add a parallel UI framework or depend on event order. |
| Shared Unity-owned JSON fixture pattern linked into xUnit | Reuse it for cross-runtime case IDs and expectations. |
| Existing title, action, save-failure, and isolated-path tests | Extend production-path coverage instead of creating scene-only test doubles. |

### NOT in scope

- Manual save controls, cloud synchronization, concurrent writers, and session redesign: none is required for single-process autosave durability.
- A generalized serializer, journal, filesystem framework, or unbounded save generations: the fixed five-role envelope is enough.
- Save-schema migration: current and legacy validators remain authoritative.
- Unity Standalone Windows durability certification: deferred to the task follow-up because it needs a development-player fault harness and real cold-restart evidence.
- WebGL, tvOS, macOS, Linux, iOS, Android, network shares, removable media, and other unverified filesystems: they do not inherit local Windows evidence.
- Scene or prefab changes: stop and return to planning if existing status bindings cannot render the required composition.

### Architecture review

1. [P1] (confidence: 9/10) `docs/plans/2026-08-11-001-feat-ironclad-save-envelope-plan.md:231` - resolved: “Durably create the recovery-pending marker” was not concrete enough to prevent a plain create/write implementation from claiming recovery notice durability. The plan now requires create-new, zero content, flush-to-disk, close, idempotent existing-marker behavior, and deterministic create/flush failures before recovery promotion.
2. [P2] (confidence: 8/10) `src/ToilRelic/Systems/SaveSystem.cs:15` - resolved: `_savePath = savePath ?? Path.Combine(Directory.GetCurrentDirectory(), SaveFileName);` means “Windows” alone could overstate the guarantee for a UNC or unverified filesystem. The guarantee is now limited to a local Windows volume; other configured paths retain conservative failure semantics without inheriting the claim.

Component boundaries, typed result direction, validator purity, single-writer assumption, indivisible replace primitive, recovery normalization, and marker-last New Game ordering are coherent after these changes. No new artifact or distribution pipeline is introduced.

### Code quality review

No issues found. The plan keeps production duplication limited to two runtime-specific implementations, centralizes expectations in one shared fixture, avoids a generalized filesystem abstraction, and keeps diagnostic shaping explicit and allowlisted.

### Test review

3. [P1] (confidence: 9/10) `docs/plans/2026-08-11-001-feat-ironclad-save-envelope-plan.md:621` - resolved: “Injected I/O failure while classifying live or LKG aborts before deleting” was required by KTD7 but had no named deterministic vector. U1 and U5 now require that vector and assert that no eligible or evidence artifact and no retained UI state changes before the abort.

~~~text
CODE PATHS                                      USER FLOWS
[PLAN:UNIT] Save                                [PLAN:INTEGRATION] Startup
  |-- write + flush stage                         |-- valid live -> Continue
  |-- validate stage                              |-- invalid/missing live + valid LKG -> recover
  |-- valid live -> replace + LKG                  |-- restart -> Loaded + pending notice
  |-- missing live -> no-overwrite promote         `-- unreadable/no LKG -> existing New Game path
  |-- invalid live -> fail unchanged
  `-- pending marker -> clear after promotion    [PLAN:INTEGRATION] Notice lifecycle
                                                    |-- recovery shown once per startup
[PLAN:UNIT] Load/recover                             |-- failed save retains recovery + failure
  |-- validate live then LKG                         |-- successful save clears recovery
  |-- stage exact validated LKG bytes                `-- Title/Camp/Battle priority at 2 resolutions
  |-- durable marker create + flush
  |-- bounded quarantine                         [PLAN:E2E] Authorized New Game
  |-- no-overwrite recovery promote                |-- classify authority without mutation
  `-- next Load reconciles ambiguous exception      |-- delete non-authority roles
                                                    |-- delete last valid payload at data edge
[PLAN:UNIT] Diagnostics                              |-- delete marker last
  |-- allowlisted roles/type/relative name           |-- clear retained UI state only on success
  `-- reject bytes/fields/absolute path/message      `-- retry every failure boundary

PRE-IMPLEMENTATION COVERAGE: 0 executed feature paths
TARGET COVERAGE: every branch above in console and Unity, with identical shared case IDs
~~~

Unit tests cover pure validation, path derivation, artifact bytes, checkpoints, diagnostic redaction, and state matrices. Production-path integration tests cover console orchestration and Unity GameManager/GameEvents/GameStatusController. New Game and restart-sensitive recovery are end-to-end within each runtime test process; Unity focused tests run twice in fresh Editor processes to prove static reset.

### Failure-mode audit

| Codepath | Realistic failure | Planned test | Error handling | Player outcome |
|---|---|---|---|---|
| Stage write/flush/validation | partial or invalid stage | Shared before/after vectors plus exact bytes | Fail without promotion | Existing progress remains playable; save failure is clear. |
| Valid-live replace | primitive throws before or after mutation | Both observable states, then fresh Load | Conservative failure, no rollback | Save failure now; next startup validates reality. |
| Missing-live promote | destination appears or move throws | Pre/after-mutation vectors | Conservative failure | No corrupt live is accepted. |
| Marker create/flush | marker path blocked or flush fails | Absent/existing/directory/create/flush cases | Abort before quarantine/promotion | Recovery is not claimed. |
| Quarantine rotation | prior delete or damaged-live move fails | Before/after mutation cases | Retain valid LKG for retry | Unrecovered result and existing New Game path. |
| Recovery promote | move throws with unknown commit state | Fresh Load after both permitted states | Validate next startup | Recovered or Loaded+pending only when valid live exists. |
| Diagnostic shaping | exception contains path or player data | Distinctive secret/path sentinels in both runtimes | Allowlist fields only | Player receives fixed safe copy. |
| New Game classification/deletion | read or deletion fails at any edge | Classification plus every deletion boundary | Remain at title; retry idempotently | No false New Game success. |
| Recovery UI composition | event order or simultaneous messages collide | Semantic-state permutations and 800x600/1280x720 evidence | Retain independent state | Recovery and failure remain understandable. |

Critical gaps after folding the findings: 0.

### Performance review

No issues found. Save payloads are bounded player snapshots, operations are synchronous and constant-generation, no directory scan or unbounded history is introduced, and load/save performs a bounded number of reads and writes. Caching or asynchronous persistence would add complexity without solving a measured problem.

### Parallelization strategy

| Step | Modules touched | Depends on |
|---|---|---|
| U1 contracts/vectors | console systems, Unity save tests, console tests | - |
| U2 console envelope | console systems and tests | U1 |
| U3 Unity envelope | Unity Save and PlayMode tests | U1 |
| U4 feedback integration | console game/tests and Unity Core/UI/tests | U2, U3 |
| U5 New Game invalidation | both persistence/orchestration/test modules | U4 |

Lane A: U1. Then launch Lane B (U2 console) and Lane C (U3 Unity) in parallel worktrees. Merge both before Lane D: U4 -> U5 sequentially because they share orchestration and title tests. U2 and U3 share only the contract fixture/schema established by U1; after U1 freezes case IDs they have no production-module conflict.

### TODO disposition

No repository-level TODOS.md item is proposed. The only separate work, Unity Standalone Windows capability proof, is recorded with context in `.flow/tasks/ironclad-save-envelope/followups.md` so it remains attached to this task rather than creating a vague global TODO.

### Implementation Tasks

Synthesized from this engineering review. These are already folded into U1-U5.

- [ ] **T1 (P1, human: ~2h / Codex: ~20min)** - console + Unity persistence - implement a durable recovery-marker primitive
  - Surfaced by: Architecture review finding 1.
  - Files: `src/ToilRelic/Systems/SaveEnvelopeFileOperations.cs`, `unity/Assets/Scripts/Save/SaveEnvelopeFileOperations.cs`, focused envelope tests.
  - Verify: marker create/flush vectors pass in xUnit and Unity SaveEnvelopeContracts twice in fresh processes.
- [ ] **T2 (P1, human: ~1h / Codex: ~10min)** - shared New Game contract - add authority-classification I/O failure vectors
  - Surfaced by: Test review finding 3.
  - Files: shared JSON fixture, both fixture readers, console/Unity envelope tests.
  - Verify: injected classification failure leaves every seeded artifact byte-for-byte unchanged and publishes no recovery-state clear.
- [ ] **T3 (P2, human: ~30min / Codex: ~5min)** - capability boundary - keep guarantees local-volume and evidence-backed
  - Surfaced by: Architecture review finding 2.
  - Files: `unity/UNITY_SETUP.md`, task QA evidence, supported-target documentation.
  - Verify: all automated fixtures use isolated local paths and no Standalone/network/removable claim appears without separate evidence.

### Completion summary

- Step 0 Scope Challenge: scope accepted as-is; cross-runtime breadth is necessary and sequencing prevents unsafe partial landing.
- Architecture Review: 2 issues found and folded.
- Code Quality Review: 0 issues found.
- Test Review: diagram produced; 1 gap found and folded.
- Performance Review: 0 issues found.
- NOT in scope: written.
- What already exists: written.
- TODOS.md updates: 0 proposed; 1 task-scoped follow-up recorded.
- Failure modes: 0 critical gaps after fixes.
- Outside voice: skipped; no attestably different provider CLI was available.
- Parallelization: 4 lanes; U2/U3 parallel, U1/U4/U5 sequential by dependency.
- Lake Score: 3/3 recommendations chose the complete option.
- Unresolved engineering decisions: 0.

---

## Sources and References

### Repository

- src/ToilRelic/Systems/SaveSystem.cs
- src/ToilRelic/Systems/SaveResults.cs
- src/ToilRelic/Game.cs
- tests/ToilRelic.Tests/SaveSystemTests.cs
- tests/ToilRelic.Tests/GameSaveUxTests.cs
- tests/ToilRelic.Tests/ToilRelic.Tests.csproj
- unity/Assets/Scripts/Save/SaveService.cs
- unity/Assets/Scripts/Save/SaveResults.cs
- unity/Assets/Scripts/Core/GameManager.cs
- unity/Assets/Scripts/Core/GameEvents.cs
- unity/Assets/Scripts/UI/GameStatusController.cs
- unity/Assets/Tests/PlayMode/PurposefulHuntDomainPlayModeTests.cs
- unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs
- unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs
- unity/UNITY_SETUP.md

### Institutional learnings

- docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md
- docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md
- docs/solutions/workflow-issues/resume-blocked-personal-flow-qa-with-layered-environment-validation.md
- docs/solutions/architecture-patterns/pure-equipment-preview-with-commit-revalidation.md
- docs/solutions/best-practices/unity-playmode-hud-contracts.md

### Official platform references

- https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-8.0
- https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=netstandard-2.1
- https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-8.0
- https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/file-replace-exceptions-on-unix
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html
- https://docs.unity3d.com/2022.2/Documentation/Manual/webgl-debugging.html

## GSTACK REVIEW REPORT

| Review | Trigger | Why | Runs | Status | Findings |
|--------|---------|-----|------|--------|----------|
| CEO Review | `/plan-ceo-review` | Scope & strategy | 0 | NOT RUN | No CEO review required for this bounded persistence feature. |
| Codex Review | `/codex review` | Independent 2nd opinion | 0 | NOT RUN | No different-provider CLI was available. |
| Eng Review | `/plan-eng-review` | Architecture & tests (required) | 1 | CLEAR | 3 issues found and folded; 0 critical gaps. |
| Design Review | `/plan-design-review` | UI/UX gaps | 0 | NOT RUN | Existing surfaces only; explicit composition contract and layout evidence are in U4. |
| DX Review | `/plan-devex-review` | Developer experience gaps | 0 | NOT RUN | No new external developer surface. |

**VERDICT:** ENG CLEARED - ready to implement U1 through U5.

NO UNRESOLVED DECISIONS
