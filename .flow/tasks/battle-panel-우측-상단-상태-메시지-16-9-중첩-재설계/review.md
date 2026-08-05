# Review

## Re-review scope

- Reviewed rework range: `0437307c073f8702821b17376d0270a6d3500771..7c1d2e0327e56f129198d086f5ed98ced2a6d7d5`
- Reviewed intent: verify that the prior viewport-fidelity and multiline-log findings are resolved without adding a new false-pass path.
- Plan source: explicit implementation-ready Personal Flow plan (`ce-unified-plan/v1`).
- Review mode: report-only; no implementation fixes were applied.
- Reviewers: correctness, project standards, testing, learnings, and adversarial fallback.
- Cross-model review: not run because no attested different-provider CLI was installed; the in-process adversarial fallback covered the lens.

## Prior findings disposition

| # | Severity | Prior finding | Disposition | Verification |
|---|---|---|---|---|
| 1 | P1 | The widescreen screenshot path did not bind glyph checks to the requested render target or preserve the serialized CanvasScaler contract. | Resolved. The capture callback now runs after the requested camera and RenderTexture are attached and after Canvas updates. It asserts `ScaleWithScreenSize`, `800x600`, width-first matching, the derived `800x450` or `800x600` Canvas rect, glyph containment, and the status-to-enemy gap before capture. | Generated-scene parity passed 1/1. The graphics-enabled capture contract passed 1/1. All four Battle PNGs were inspected at `1280x720` and `800x600`; no overlap was visible. |
| 3 | P2 | The focused log test used separate one-line events and did not cover multiline parsing, blank segments, CRLF, trimming, or a trailing newline. | Resolved. The focused action test now sends one whitespace-padded multiline payload containing CRLF, blank and tab-only segments, LF, and a trailing newline, then asserts the exact newest two trimmed logical lines. | The focused action/log contract passed 1/1. The committed graphics path also proves cross-event eviction: `EnterBattle()` publishes the initial log, then two separate log events replace it and the exact newest two lines are asserted. |

## Fresh findings

No actionable findings remain.

One adversarial P2 candidate proposed adding another three-separate-event test. Independent validation rejected it because the committed graphics flow already observes three separate `BattleLog` notifications: the initial `EnterBattle()` log plus two explicit events, followed by an exact newest-two assertion.

## Requirements completeness

- R1-R7: met by the previously reviewed implementation and unchanged by this test-only rework.
- R8: met. Generated and committed CanvasScaler contracts match, and the requested render targets now drive the live Canvas and glyph checks.
- R9-R10: met. The dedicated graphics run passed, four requested Battle captures are committed and visually valid, the full Play Mode assembly reports 62 passed / 0 failed / 1 intentionally ignored opt-in capture, and the console build is recorded clean.
- U1-U3: met; the rework preserves the implemented geometry, actions, navigation, phase behavior, and scene parity contracts.
- U4: met; the render evidence is tied to the actual viewports and all required evidence is reviewable in the task directory.

## Learnings and past solutions

- `docs/solutions/design-patterns/width-first-unity-ui-virtual-layout-floor.md`: the live `1280x720 -> 800x450` virtual Canvas assertion matches the documented width-first floor.
- `docs/solutions/best-practices/unity-playmode-screenshot-evidence-validity.md`: graphics execution, XML success, dimensions, non-uniform pixels, and visual inspection are all present.
- `docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md`: the multiline input travels through `GameEvents` and asserts an exact user-visible result.
- `docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md`: the prior save-failure status composition is retained in the failure-state capture.
- `docs/solutions/workflow-issues/resume-blocked-personal-flow-qa-with-layered-environment-validation.md`: behavioral, graphics, full-suite, and console evidence remain separate gates.

## Coverage

- Exact rework range: 12 committed files, 114 executable changed lines; no runtime, editor bootstrap, scene, or console implementation change.
- Mechanical merge: 1 candidate, 0 malformed returns, 0 malformed findings.
- Independent validation: 1 batch; the sole P2 candidate was rejected as already covered. Final actionable findings: 0.
- Failed or timed-out reviewers: none.
- Cross-model pass: not run; local adversarial fallback completed.
- Residual risk: the viewport-faithful assertions are in the opt-in graphics lane, so future regression protection depends on continuing to run that lane. The assertions execute before final readback, but no late-render mutation path was found and both stable captures passed with valid images.
- Tests were not rerun during this report-only review. The committed XML results and four Battle PNGs were inspected directly.
- Excluded from scope: unrelated untracked `equipment-comparison/work-*` evidence and untracked intermediate non-Battle evidence under this task. No excluded file was edited, staged, or deleted.

## Result

`pass`

The task is ready for the Personal Flow QA stage.
