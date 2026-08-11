# Plan

## Readiness

- Status: Complete on `codex/ironclad-save-envelope`; implementation, simplification, review, QA, and knowledge capture passed.
- Canonical plan: docs/plans/2026-08-11-001-feat-ironclad-save-envelope-plan.md.
- Product Contract: Preserved unchanged with R1–R17, F1–F4, and AE1–AE11.
- Plan depth: Deep.
- Product blockers: None.
- Document review: All six remaining items resolved with the recommended dispositions; no document-review decision remains open.
- Engineering review: Passed with three findings folded, zero unresolved decisions, and zero critical gaps.

## Implementation units

1. U1 — Establish cross-runtime result and shared behavioral vector contracts.
2. U2 — Implement console staging, atomic promotion, LKG, quarantine, recovery marker, and deterministic tests.
3. U3 — Implement the equivalent Unity envelope and reflection-safe PlayMode seam.
4. U4 — Integrate playable Recovered outcomes and independent non-blocking recovery feedback.
5. U5 — Invalidate the full envelope from every authorized New Game path.

Sequence: U1 → U2/U3 → U4 → U5. Console and Unity land together.

Execution engine: native serial subagents. The harness exposes a shared checkout rather than isolated worker worktrees, so units run one at a time and the primary agent owns verification and commits.

## Implementation progress

- U1 complete in commit `41fe803` (`feat(save): add shared envelope contracts`).
- Console contract tests: 3 passed; console build: 0 warnings and 0 errors.
- Unity `SaveEnvelopeContracts`: 3 passed in `work-u1-contract-results.xml`.
- Integration review corrected the shared promotion boundary mapping so `PromoteLive` and `PromoteRecovery` are pre-mutation faults, while the corresponding `After...Promotion` checkpoints are post-mutation interruptions.
- U2 complete in commit `464f36b` (`feat(save): add console recovery envelope`).
- Console focused envelope/save suite: 67 passed; full console suite: 123 passed; build: 0 warnings and 0 errors.
- U2 integration corrected cases 08-15 so exact recovery artifacts include the already validated `Stage=valid-lkg` required by KTD4.
- U3 complete in commit `8a5738a` (`feat(save): add Unity recovery envelope`).
- Unity `SaveEnvelopeContracts`: 44 passed in each of two fresh processes with identical test-name sets and no static seam leakage.
- Unity production-action regression: 23 passed; Purposeful Hunt action regression: 5 passed, 1 existing conditional capture ignored.
- U4 complete in commit `641dbdc` (`feat(save): surface automatic recovery state`).
- Console recovery UX plus envelope suite: 48 passed; build: 0 warnings and 0 errors.
- Unity recovery UX: 5 passed with Title/Camp layout evidence at 800x600 and 1280x720; scene and prefab diffs remain empty.
- U5 complete in commit `2dabae6` (`feat(save): invalidate complete envelope on new game`).
- Console focused suite: 103 passed; full xUnit: 149 passed; build: 0 warnings and 0 errors.
- Unity envelope contracts: 63 passed; New Game UX: 4 passed; classification I/O and authority-last retry paths are covered in both runtimes.
- Implementation units are complete.

## Simplification pass

- Reuse review: 0 changes; console/Unity duplication remains intentional for runtime alignment.
- Quality review: 2 changes applied. Unity checkpoint names and mutation sides are now local enums while the reflection test callback remains string-based, and parameter-for-parameter delete forwarding wrappers were removed in both runtimes.
- Efficiency review: 2 suggestions deferred. Rewriting the Unity lexical validator around offsets/spans would add correctness risk to the save boundary, and caching the shared fixture would weaken same-process reload coverage for negligible suite cost.
- Verification after simplification: full console tests 149/149, console build 0 warnings/0 errors, Unity `SaveEnvelopeContracts` 63/63.

## Code review and final verification

- Structured review run: `20260811-144706-67bd3c4e`; verdict `Ready to merge`.
- Five validated findings were fixed: stale Unity Continue state after a failed New Game delete, checked deletion metadata probes, exact 33-case manifest enforcement, missing-live promotion boundaries, and Unity diagnostic-field allowlisting.
- Review fixes landed in `06d7a42` (`fix(save): harden envelope review boundaries`).
- Final console suite: 152/152; build: 0 warnings and 0 errors.
- Final Unity save-envelope category: 66/66 in each of two fresh processes.
- Final Unity PlayMode assembly: 176 passed, 0 failed, 3 conditional graphics-capture skips out of 179.
- Shared console/Unity case parity: 33/33 exact IDs, no one-sided vector.
- Unity logs: no compiler, unhandled-exception, serialization, or static-sentinel leakage signals.
- Scene and Prefab diff: empty.
- Four Title/Camp recovery-layout captures were directly inspected at 800x600 and 1280x720; recovery copy, failure status, and primary actions remain visible without overlap.
- Reusable learning: `docs/solutions/architecture-patterns/validated-authority-save-envelope.md`.

## Affected paths

- Console production: src/ToilRelic/Systems/SaveSystem.cs, SaveResults.cs, a narrow save-specific file-operation seam, and src/ToilRelic/Game.cs.
- Console tests: SaveSystemTests.cs, GameSaveUxTests.cs, a focused IroncladSaveEnvelope test/fixture pair, and the test project fixture link.
- Unity production: SaveService.cs, SaveResults.cs, a narrow save-specific file-operation seam, GameManager.cs, GameEvents.cs, and GameStatusController.cs.
- Unity tests/data: shared JSON fixture, focused reflection-based PlayMode tests, existing title/action regression tests, and required .meta files.
- Documentation: unity/UNITY_SETUP.md and CONCEPTS.md if implemented names or target limits differ.

## Validation contract

- Console build, focused xUnit TRX, and full xUnit suite.
- Focused Unity SaveEnvelopeContracts category twice in fresh processes, with direct static-reset sentinels.
- Full Unity PlayMode assembly.
- Exact shared case-ID parity and byte-level artifact assertions for every deterministic boundary.
- Manual console and Unity Editor restart/notice lifecycle checks; Standalone proof is a separately scoped follow-up and target guarantees remain capability-bounded.

No build or test was run during planning.

## Rollback or migration

- No save-schema migration. Existing current and legacy saves remain read-only until a normal later save enrolls them in the envelope.
- Old code can still read live after rollback but ignores the LKG, quarantine, stage, and marker siblings and therefore suspends the new durability guarantee.
- New Game is the only planned operation that intentionally clears all envelope artifacts.
