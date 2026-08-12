# Review

| # | Severity | Finding | Disposition | Verification |
|---|---|---|---|---|
| 2 | P1 | Console Defend, valid Potion, Flee, defeat, and multi-turn orchestration lacked deterministic production-path coverage. | Applied: `CombatSystem` accepts a production-default `Random` seam and the tactical suite now exercises each consuming, rejecting, and terminal branch. | Targeted console tactical suite: 8/8 pass; full console suite: 160/160 pass. |
| 3 | P1 | Unity displayed the second locked Power Attack but did not resolve it through a second real scene action. | Applied: the scene test now resolves consecutive Exposed Opening and Power Attack turns with fixed damage, then verifies the next intent. Potion and failed Flee tests also assert intent advancement. | Tactical Hunt Grammar PlayMode category: 3/3 pass; action contracts: 23/23 pass. |
| 5 | P3 | New console private static fields used PascalCase instead of the repository's `_camelCase` rule. | Applied: renamed the three canonical intent fields. | Console build and full tests pass. |
| 1 | P1 candidate | Replacing Unity `CombatSystem` method signatures might require compatibility overloads. | Rejected by independent validation: all repository consumers were updated, the class is an internal game implementation boundary, and no project contract promises external binary/source compatibility. | Repository call-site search plus independent validator verdict. |
| 4 | P2 candidate | Move the two real-scene tactical tests out of the 4,000-line P0 action-contract fixture. | Demoted to residual risk and not applied. The documented action-contract pattern intentionally centralizes serialized Button -> bridge -> GameManager tests in this harness; extracting a shared reflection harness would add more complexity than this slice removes. | Compared with `docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md`. |
| Q1 | P1 | The complete intent label wrapped beyond the original 24px phase row. | Applied: preserved 18px text, expanded PhaseText to 42px, moved the log row, and kept the scene generator and serialized scene aligned. | Focused rendered-bounds test 1/1, full PlayMode 179 passed/0 failed/3 intentional skips, graphics capture 1/1; 1280x720 and 800x600 images inspected. |

### Requirements Completeness

- R1-R4: met by mirrored intent rules, deterministic profiles, shared fixture, and fallback rows.
- R5-R7: met by pure modifier tests plus player-stepped console and real Unity action tests.
- R8: met by console reveal-before-menu output and persistent Unity text/marker rendering.
- R9: met by unchanged reward/save authorities and the full regression suites.
- U1-U4: all implementation units are present in the scoped diff and have their planned verification evidence.

### Actionable Findings

None. Findings #2, #3, #5, and Q1 were applied and verified. Findings #1 and #4 were rejected or demoted with recorded reasoning.

### Learnings & Past Solutions

- Known pattern: `docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md` supports real serialized-button tests and isolated random/event/save state.
- Known pattern: `docs/solutions/best-practices/unity-playmode-hud-contracts.md` supports reflection-based event/UI contract tests without a runtime assembly reference.
- Known pattern: `docs/solutions/architecture-patterns/validated-authority-save-envelope.md` supports shared semantic fixtures while preserving runtime-local authorities.

### Coverage

- Review lenses: correctness, project standards, testing, maintainability, institutional learnings, API contract, and in-process adversarial turn sequences.
- Cross-model pass: not run because no attested different-provider CLI is installed; the adversarial fallback found no additional issue.
- Validator batch: 4 candidates checked; 3 validated and fixed, 1 rejected as hypothetical external compatibility.
- Fast pass: no urgent P0/P1 candidate.
- Residual risk: the large central Unity P0 fixture remains costly to navigate, but splitting it was not justified for this change.

---

### Verdict

`Ready to merge: pass after verified fixes.`
