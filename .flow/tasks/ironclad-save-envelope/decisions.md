# Decisions

## Confirmed

- Ironclad Save Envelope is the active next work item after Purposeful Hunt. (session-settled: user-approved — chosen after the repository-grounded shortlist was re-evaluated and the user proceeded with this candidate.)
- The active outcome is save durability and visible automatic recovery across console and Unity; manual save, cloud sync, session redesign, and a generalized persistence framework are outside this work.
- If the live save is unreadable and last-known-good is valid, preserve the damaged live file, automatically promote the validated recovery copy, continue loading, and tell the player that recovery occurred. (session-settled: user-directed — chosen over temporary in-memory recovery and a blocking recovery/New Game prompt to keep recovery safe and non-blocking.)
- Keep one rotating last-known-good save and at most one latest quarantined damaged live save. (session-settled: user-approved — the user delegated remaining choices to the recommended path; this bounds storage and lifecycle complexity while retaining one verified rollback point and one diagnostic artifact.)
- Use a validated single-generation snapshot flow: stage away from live, validate, preserve the current valid live as last-known-good, then atomically promote the candidate.
- Ignore orphaned staging data during startup; only validated live and last-known-good copies participate in recovery precedence.
- A recovery notice is non-blocking, states that recent progress may be missing, and remains until a later progress save succeeds.
- New Game replacement must clear or invalidate live, recovery, staging, and quarantine candidates so old progress cannot reappear later.
- The durability promise covers unexpected process interruption and filesystem-operation failure; physical-device loss or corruption after the operating system reports success is outside scope.
- The requirements-only Product Contract is `docs/plans/2026-08-11-001-feat-ironclad-save-envelope-plan.md`; planning may choose platform mechanics but must preserve its observable behavior.
- Independent claim verification confirmed the current console and Unity write paths, result-status gap, deletion scope, test coverage, and prior scope boundary against repository sources on 2026-08-11.
- Recovery-pending state must survive a restart until the player can be notified and a later player-progress save succeeds; planning owns the narrow representation.

## Rejected options

- Starting implementation before a Product Contract: recovery precedence and user-visible recovery semantics would otherwise be invented during coding.
- Loading a backup only in memory: leaves the on-disk live state unresolved and defers normalization to an unrelated future save.
- Blocking on a recovery/New Game choice: adds a new decision flow despite a validated recovery copy being available.
- Append-only save generations: provides more history but turns a bounded recovery feature into a generalized persistence store.
- Load-time-only fallback: does not remove Unity's live-write exposure and gives weaker interruption guarantees.
- Two or unlimited normal backup generations: adds retention and precedence complexity without evidence that one verified rollback point is insufficient.

## Open questions

- No Product Contract blocker remains.
- No document-review decision remains open.

## Planning resolutions

- Derive fixed stage, LKG, quarantine, and recovery-marker siblings from the configured live path so all promotion operations remain same-directory and same-volume.
- Use one side-effect-free candidate validator per runtime. Public Load owns recovery and must not be reused to validate stage or LKG.
- For a valid live save, keep replace-with-backup as one indivisible promotion primitive. For missing live, use a same-directory no-overwrite move. Never use copy-overwrite or delete-then-move as a fallback.
- Keep load status independent from durable notice state: the recovery operation returns Recovered plus pending notice; a later restart returns Loaded plus pending notice.
- Treat the marker as notice state only, never as recovery authority.
- Keep failure injection save-specific and runtime-local. The shared JSON owns behavior vectors; runtime readers translate payload labels only.
- Limit durability claims to capability-verified targets. A local-volume .NET 8 Windows console run and Unity Editor PlayMode on its local persistent-data path are the executable baseline; Unity Standalone Windows requires a separately scoped development-player harness and cold-restart proof.
- Sequence units U1 → U2/U3 → U4 → U5 and land console/Unity together.

## Document review record

- Applied one confidence-100 mechanical fix: U1 now traces its New Game vectors to R13, F4, and AE10.
- Applied the three proposed fixes for New Game recovery-state clearing, worst-case recovery/status rendering verification, and diagnostic redaction.
- Resolved the three judgment decisions for New Game deletion/marker ordering, rendered recovery priority, and Unity Standalone proof scope using the recommended dispositions below.
- The independent cross-model add-on did not run because no attestably different provider CLI was reachable; all selected in-process personas completed.
- This is a planning-document review, not the required Personal Flow engineering review gate.

## Document review dispositions

- New Game keeps dynamic authority classification, deletes the final validated payload at the data commit edge, and deletes the recovery marker last. A marker-only restart is Missing; current-call failure stays at title and retry removes the marker.
- Retained in-memory/UI recovery state clears exactly once only after full New Game invalidation succeeds. Any deletion failure retains current-call title/notice state.
- Title and Camp have an explicit recovery/save-failure composition contract; Battle retains semantic state without adding a persistent row.
- Worst-case recovery, save-failure, and simultaneous gameplay/level-up composition requires 800x600 and 1280x720 layout evidence with no clipping or hidden action.
- Developer diagnostics are allowlisted to operation role, artifact role, exception type, and redacted relative filename. Raw save bytes, player fields, absolute roots, and unfiltered exception messages are forbidden.
- The executable guarantee is narrowed to a local-volume .NET 8 Windows console run and Unity 6 Windows Editor PlayMode. Standalone compatibility remains, but its durability guarantee is deferred to the recorded follow-up.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | OpenSpec CLI present without project artifacts; CE brainstorm active; gstack available; OMX unavailable | `probe-frameworks.ps1`, 2026-08-11 |
| CE brainstorm | Required: product behavior and success boundaries remain to be decided | Active `compound-engineering:ce-brainstorm` skill |
| Engineering review selection | Required for the cross-runtime durability and data-integrity boundary | Personal Flow large-profile heuristic |
| Design review | Skipped: no new interaction model or visual system; recovery copy/status is covered as product behavior here | Personal Flow gate heuristic |
| OMX | Skipped until a plan proves independent workstreams and the user approves a launch command | Personal Flow safety rule |
| Product scope confirmation | Approved through the user's standing instruction to proceed with the recommended direction | Session direction, 2026-08-11 |
| Repository claim verification | Passed: all six save-system and test-baseline claims confirmed | Independent verifier, 2026-08-11 |
| Requirements contract validation | Passed: required frontmatter and sections, 17 contiguous requirements, 11 acceptance examples, 5 success criteria, source paths, and Markdown diff checks | Local validation, 2026-08-11 |
| Implementation planning | Passed: Planning Contract, five U-IDs, Verification Contract, Definition of Done, state diagrams, platform bounds, and deterministic failure mapping recorded | compound-engineering:ce-plan, 2026-08-11 |
| Planning document review | Passed: one mechanical fix plus all six remaining items resolved with recommended dispositions | compound-engineering:ce-doc-review + Personal Flow, 2026-08-11 |
| Engineering review | Passed: three findings folded; zero unresolved decisions and zero critical gaps | plan-eng-review + `.flow/tasks/ironclad-save-envelope/review.md`, 2026-08-11 |
