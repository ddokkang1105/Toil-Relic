# Review

## Scope

- Explicit implementation-ready plan: `plan.md`
- Task-owned implementation: scene bootstrap geometry, compact HUD labels, regenerated `SampleScene`, Play Mode contracts, and viewport evidence
- Review lenses: correctness, testing, maintainability, project standards, adversarial verification fidelity, agent-native applicability, and repository learnings
- Cross-model adversarial pass: unavailable because no non-Codex peer CLI (`claude` or `gemini`) is installed

## Findings and disposition

| # | Severity | Finding | Disposition | Verification |
|---|---|---|---|---|
| 1 | P2 | The status-capacity test used `Win.` and a short level-up line, so it did not represent the bounded production victory and level-up formats. | Fixed | Replaced fixtures with production-shaped victory loot, level-up, and bounded save-failure text. The strengthened test failed at 60px with preferred height 70px; status body increased to 72px, root to 96px, and Title/Camp offset to `-4`. |
| 2 | P2 | The opt-in capture test silently passed when no evidence directory was provided, and it only proved that a file was written. | Fixed | Missing environment now produces an explicit NUnit ignore. Capture verifies camera target dimensions and non-empty output; state warm-up renders prevent incomplete first-frame evidence. Opt-in capture run passed 1/1. |
| 3 | P2 | Title/Camp tests checked that action names existed somewhere but did not verify each named button's exact persistent method; Quit was omitted. | Fixed | Added `Quit` to title action coverage and one-to-one checks for Continue/New Game/Quit and Hunt/Rest/Craft Treasure. Full suite passed. |

No P0 or P1 findings were reported. No unresolved actionable finding remains.

## Requirements completeness

| Contract | Result | Evidence |
|---|---|---|
| R1–R2 | Met | Deterministic `800×450` separation test plus 1280×720 and 800×600 Title/Camp captures |
| R3 | Met | Exact six-button persistent-action mapping and existing battle action contracts |
| R4–R5 | Met | 16px/no-Best-Fit assertions and production-shaped three-message preferred-height test |
| R6–R7 | Met | 44px button/gap assertions and semantic HUD field checks |
| R8–R9 | Met | Bootstrap regeneration succeeded; serialized scene matches final constants; full Play Mode suite passes |
| R10 | Met within scope | Battle information and controls remain usable; known upper-right overlap remains documented as a separate follow-up |
| U1–U4 | Complete | Red proof, implementation, regeneration, automated suite, and viewport evidence recorded |

## Known patterns

- `docs/solutions/best-practices/unity-playmode-hud-contracts.md`: reflection-based Play Mode scene contracts remain the correct pattern for the assembly-neutral test project.
- `docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md`: the review preserved semantic status events and the real save-failure integration path.

## Coverage and residual risk

- Task attribution is artifact-based because the repository began with a heavily dirty worktree and most Unity paths are untracked.
- RenderTexture evidence is an opt-in test and is explicitly skipped during the standard suite; QA must run the dedicated capture invocation and inspect the PNGs.
- BattlePanel's known 16:9 upper-right overlap is pre-existing and out of scope.
- No agent-native gap applies to this cosmetic local-game UI change.

## Result

`pass`

The review found three moderate verification defects, applied and verified all three, and cleared the task for QA.
