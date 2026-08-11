# Review

| Severity | Finding | Disposition | Verification |
|---|---|---|---|
| P1 | New Game deleted the recovery marker before the final valid payload, so a later delete failure could leave recoverable old data without its pending notice. | Resolved: classify authority without mutation, delete non-authority roles, delete the final validated payload at the data commit edge, then delete the marker last. | KTD7, U5, New Game sequence, and retry vectors agree; marker alone remains non-authoritative. |
| P1 | Successful New Game did not explicitly clear retained recovery UI state, while failed invalidation could accidentally publish a false clear. | Resolved: clear semantic state exactly once only after every disk deletion succeeds. | U4/U5 production-path tests require success-only clear and failure retention. |
| P2 | Recovery and save-failure feedback had no exact rendered priority across Title, Camp, and Battle. | Resolved: explicit per-surface composition table; Battle retains semantic state without adding a persistent row. | KTD6 and U4 name copy, ordering, retained state, and post-clear behavior. |
| P2 | Worst-case recovery, save-failure, and gameplay/level-up composition had no layout proof. | Resolved: require 800x600 and 1280x720 evidence with no clipping, overlap, or hidden primary action. | U4 production-path layout evidence gate. |
| P2 | Developer diagnostics could inherit raw exception strings containing paths or save/player data. | Resolved: strict allowlist for operation role, artifact role, exception type, and redacted relative filename. | U1-U3 seed distinctive secrets/roots and reject bytes, fields, absolute roots, and unfiltered messages. |
| P1 | Unity Standalone was in the guarantee despite an Editor-only reflection seam that cannot drive a built-player cold restart. | Resolved: initial executable guarantee is local-volume .NET 8 Windows console plus Unity 6 Windows Editor PlayMode. | Standalone proof is recorded in followups.md with a required development-player harness. |
| P1 | Recovery marker durability was underspecified. | Resolved during engineering review: create-new zero-content marker, flush-to-disk, close, and deterministic create/flush failure vectors. | KTD4 and U1-U3 require absent/existing/directory/create/flush cases. |
| P1 | New Game authority-classification I/O failure had no named deterministic vector. | Resolved during engineering review: add shared classification-failure vector and assert zero mutation/zero false UI clear. | U1 and U5 include the vector in both runtimes. |
| P2 | “Windows” was too broad because console defaults to the current working directory, which may be a UNC or unverified filesystem. | Resolved during engineering review: guarantee limited to a local Windows volume; other paths keep conservative behavior without the claim. | KTD10, assumptions, risk table, and docs task align. |

## Engineering gate

- Scope challenge: accepted as-is. The file count is caused by two aligned runtimes, production-path tests, and one shared contract fixture; splitting the feature would create unsafe partial landing states.
- Architecture: 2 findings, both folded.
- Code quality: no findings.
- Tests: complete branch/user-flow diagram produced; 1 gap found and folded.
- Performance: no findings; fixed generations and bounded synchronous snapshot I/O remain appropriate.
- Failure modes: 0 critical gaps after fixes.
- Outside voice: skipped because no attestably different provider CLI was available.
- gstack review log: `clean`, commit `206b4e3`, 3 issues, 0 unresolved, 0 critical gaps.
- Test-plan artifact: `C:\Users\User\.gstack\projects\ddokkang1105-Toil-Relic\doyoung-user-codex-purposeful-hunt-vertical-slice-eng-review-test-plan-20260811-131630.md`.
- Implementation-task JSONL: skipped because `jq` is not installed; the three tasks are preserved in the canonical plan's Implementation Tasks section.

## Result

`pass`

The implementation plan is cleared to enter Personal Flow `work`. No code, build, runtime test, or QA execution is claimed by this document review.

## Implementation code review

### Result

`approved`

- Review run: `20260811-144706-67bd3c4e`.
- Scope: base `206b4e3c0c4a691c89c2a89297e92a3513f4e8a3` through implementation head `32218a301678bffbfc5f3002afe95b34f7187132`, followed by review fixes.
- Reviewers: correctness, project standards, testing, maintainability, learnings, reliability, and adversarial failure modes.
- Cross-model review was unavailable because no installed CLI exposed a different attested provider family; a local adversarial reviewer ran instead.
- Validator checked six synthesized candidates: five were valid and fixed; one was rejected because it contradicted the plan's explicit unknown-commit reconciliation contract.

| Severity | Finding | Disposition | Verification |
|---|---|---|---|
| P1 | A failed Unity New Game delete after the data edge retained stale Loaded/Recovered state and could leave Continue enabled. | Fixed: reload actual envelope authority after failure and republish Title state; refresh Continue from the semantic state event. | `IroncladNewGameUx` 4/4 and full PlayMode pass. |
| P1 | `File.Exists`/`Directory.Exists` could collapse access errors to absence and let Delete claim success. | Fixed: checked `File.GetAttributes` probe; only not-found exceptions mean absence. | Console/Unity metadata-probe fault tests pass. |
| P2 | Fixture readers did not pin the exact required vector list or operation partition. | Fixed: exact ordered 33-case manifest plus Save/Load/NewGame partition in both readers. | Negative missing-case/repartition tests and 33/33 parity pass. |
| P2 | Missing-live no-overwrite promotion lacked both before- and after-mutation tests. | Fixed in both runtimes with fresh-load reconciliation assertions. | Console 152/152; Unity envelope 66/66 twice. |
| P2 | Unity accepted arbitrary unknown fields inside `expectedDiagnostic`. | Fixed with an exact four-field allowlist and malformed-fixture tests. | Unity envelope 66/66 twice. |

No actionable review finding remains. Splitting the large shared `SampleSceneP0PlayModeTests` fixture remains a non-blocking maintainability signal because the current tests depend on extensive common serialized-scene setup.
