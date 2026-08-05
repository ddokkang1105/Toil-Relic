# Review

## Scope

- Reviewed commit range: `d4c7cfda16ac1c4747bce6116ece33073b2633eb..342004b078f4131f939bbe1aa1806470a6138822`
- Reviewed intent: redesign only the Unity BattlePanel for safe 16:9 status separation while preserving GameStatus, CanvasScaler, combat behavior, and persistent actions.
- Review mode: report-only; no implementation fixes were applied.
- Reviewers: correctness, project standards, testing, maintainability, learnings, and adversarial.
- Cross-model review was unavailable because no attested different-provider CLI was installed; the required in-process adversarial fallback ran.

## Findings

| # | Severity | Finding | Disposition | Verification |
|---|---|---|---|---|
| 1 | P1 | `SampleSceneP0PlayModeTests.cs:2021` measures generated glyph separation only on the runner's current Canvas. The `1280x720` path uses synthetic rectangle math and accepts any non-empty screenshot, so a widescreen-only glyph overlap can leave the automated suite green. | Rework required. Add viewport-scoped render assertions for `1280x720` and `800x600`, assert the resulting virtual Canvas size and serialized CanvasScaler contract, then run glyph containment and status-to-enemy gap checks before capture. | Independent validator confirmed the failure path. |
| 3 | P2 | `SampleSceneP0PlayModeTests.cs:2039` sends three separate one-line log events, but the explicit plan requires one multiline event covering embedded newlines, blank segments, CRLF, trailing newlines, trimming, and newest-two eviction. | Rework required. Drive one representative multiline payload through `GameEvents` and assert the exact two newest trimmed non-empty logical lines. | Correctness and testing reviews agreed on the gap; independent validator confirmed it. |

## Requirements completeness

- R1-R7, R9-R10: met in the reviewed diff and recorded work evidence.
- R8: partially addressed because the glyph assertions are not bound to the requested widescreen render viewport; tracked by finding #1.
- U1-U3: addressed.
- U4: partially addressed. The four requested screenshots are present and were visually inspected, but the automated widescreen glyph guard is not render-size faithful; tracked by finding #1.

## Advisory and residual risks

- The new BattlePanel coverage adds about 340 lines to the 3,232-line all-purpose Play Mode fixture. A focused `BattlePanelLayoutPlayModeTests` fixture would improve maintainability, but this single-reviewer P2 advisory does not block the task.
- Manual BattlePanel disable/re-enable during an active battle clears its surface and does not repopulate it on enable. This predates the reviewed change and is outside the normal state transition path.
- Relevant known patterns were checked in `docs/solutions/`: width-first virtual layout floors, graphics screenshot validity, deterministic action contracts, status/save-failure composition, and layered Personal Flow QA recovery.

## Coverage

- Mechanical merge: 3 candidates, 0 malformed, 0 confidence-suppressed.
- Soft-bucket demotion: 1 maintainability P2 moved to residual risk; the testing P2 remained primary because it violates an explicit plan verification contract.
- Independent validation: one batch; findings #1 and #3 both validated; 0 dropped.
- Fast pass: no urgent preliminary finding.
- Failed or timed-out reviewers: none.
- Excluded from review scope: unrelated untracked `equipment-comparison/work-*` evidence and untracked intermediate evidence under this task. The committed work range and canonical task artifacts were reviewed.
- Tests were not rerun during this report-only review; the work-stage Unity and console results were inspected as evidence.

## Result

`rework required`

Fix order: #1 viewport-faithful glyph guard, then #3 multiline logical-log regression case. QA must wait until both findings are resolved and reviewed.
