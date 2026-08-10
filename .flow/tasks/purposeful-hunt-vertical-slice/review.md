# Review

## Code Review Results

**Scope:** base `415ab5f` -> task head `37965c7` (84 tracked files; 10,309 insertions, 2,772 deletions)
**Intent:** Deliver the Purposeful Hunt vertical slice across the console and Unity implementations.
**Mode:** markdown report-only

**Reviewers:** correctness, project-standards, testing, maintainability, learnings, api-contract, reliability, adversarial

- `adversarial` was selected for the stateful Cancel/Confirm and UI-event ordering surfaces.
- A different-provider cross-model pass was unavailable because no supported peer CLI was installed; an independent local adversarial pass and a separate validator pass were used instead.
- Untracked test/evidence outputs were inventoried but excluded from the production diff.

### Triage Groups

| Group | Findings | Context | Preferred Resolution | Why |
|---|---|---|---|---|
| Contract cancellation and navigation | #1, #2 | Both defects occur in the synchronous Cancel close sequence. | First require an actively presented snapshot in `ConfirmHunt`, then make the controller restore Camp focus before or independently of the close event. Add manager-level and serialized-button regressions together. | One sequence-focused work unit closes both the stale action and controller focus gaps without widening manager/controller ownership. |

### P2 -- Moderate

| # | File | Issue | Reviewer | Confidence |
|---|---|---|---|---|
| 1 | `unity/Assets/Scripts/Core/GameManager.cs:191` | A delayed confirmation can start a Hunt after the Contract was cancelled. | adversarial | 100 |
| 2 | `unity/Assets/Scripts/UI/HuntContractPanelController.cs:98` | The synchronous close event clears the state needed to restore focus after Cancel. | adversarial | 100 |
| 3 | `unity/Assets/Scripts/Core/GameManager.cs:477` | The third-victory project event overwrites the detailed terminal outcome and level-up message. | correctness | 75 |

- **#1** - `CancelHunt` clears `presentedHuntContract`, but `ConfirmHunt` rebuilds the same revision and never requires an active presented snapshot. Require the stored presentation to exist and match the submitted revision/quarry before fresh-content revalidation; prove present -> cancel -> delayed confirm remains in Camp with no encounter or mutation.
- **#2** - `Cancel()` calls the manager first; the manager synchronously raises `HuntContractClosed`, and `OnContractClosed` runs `Close(false)` before `Close(true)`. Restore focus before dispatching cancellation or make focus restoration independent of `isOpen`; verify the serialized Cancel button selects the Hunt entry control.
- **#3** - `ResolveVictory` publishes `BattleOutcome` and `LevelUp`, then `PublishPlayer` raises `RelicProjectChanged`; `GameStatusController.OnProjectChanged` replaces `primaryMessage` with generic Ready text. Publish project state before terminal facts or append/deduplicate readiness; add a third-distinct-victory action test that retains Win, reward, Ready, and level-up facts.

### Requirements Completeness

| Requirement or unit | Status | Review evidence |
|---|---|---|
| R1-R2, R4-R9, R11, R13 | Met | Reviewed production paths and prior green work evidence cover content, rewards, persistence, equipment handoff, replay, and parity. |
| R3 | Partial | Finding #1 permits confirmation after cancellation even though no active selection surface remains. |
| R10 | Partial | Finding #3 removes semantic victory/reward details before control returns. |
| R12 | Met with coverage gap | Known malformed-content paths reject before mutation, but canonical contribution membership and the exact production RNG upper boundary remain unproven residual cases. |
| U1-U4 | Met | Mirrored content/save/domain/console units are implemented and their recorded regressions are green. |
| U5 | Partial | Findings #1 and #3 affect Unity manager authority and event ordering. |
| U6 | Partial | Finding #2 breaks the intended Camp-local navigation contract. |
| U7 | Partial | Serialized-action coverage does not currently assert the three reviewed sequences. |

### Actionable Findings

| # | File | Issue | Route | Notes |
|---|---|---|---|---|
| 1 | `unity/Assets/Scripts/Core/GameManager.cs:191` | Cancelled Contract can be confirmed later. | `manual -> downstream-resolver` | Require an active matching presented snapshot and add a sequence regression. |
| 2 | `unity/Assets/Scripts/UI/HuntContractPanelController.cs:98` | Cancel does not restore Hunt-entry focus. | `manual -> downstream-resolver` | Correct synchronous close/focus ordering and verify the real serialized button. |
| 3 | `unity/Assets/Scripts/Core/GameManager.cs:477` | Third-victory terminal detail is overwritten. | `gated_auto -> downstream-resolver` | Reorder semantic publications or preserve the terminal message, then add a visible action assertion. |

### Learnings & Past Solutions

- [Pure equipment preview with commit revalidation](../../../docs/solutions/architecture-patterns/pure-equipment-preview-with-commit-revalidation.md) - keep the new relic handoff read-only until the established commit boundary.
- [Deterministic Unity PlayMode action contracts](../../../docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md) - drive the serialized controls and isolate static, save, and RNG state for the new regressions.
- [Unity status event and save-failure contracts](../../../docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md) - preserve terminal gameplay feedback while keeping save failure last.
- [Width-first Unity UI virtual layout floor](../../../docs/solutions/design-patterns/width-first-unity-ui-virtual-layout-floor.md) - retain viewport-faithful geometry/evidence expectations when rerunning Unity checks.
- [Resume blocked Personal Flow QA with layered validation](../../../docs/solutions/workflow-issues/resume-blocked-personal-flow-qa-with-layered-environment-validation.md) - use the recorded Unity evidence when the editor is unavailable, but keep real action proof explicit.

### Coverage

- Validation: all 3 primary findings were independently rechecked against the current source and retained.
- Console regression: `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore --nologo` passed 86/86 during review.
- Prior work evidence: Unity Edit Mode 2/2; Unity Play Mode 101 passed, 0 failed, 2 conditional graphics skips. Unity was not rerun during this review pass.
- Suppressed: 2 findings below confidence anchor 75 (2 at anchor 50).
- Residual risks: content validation does not prove canonical contribution membership; the production Unity RNG adapter can theoretically supply the resolver's rejected upper endpoint; confirmation revision omits mutable enemy presentation/combat fields; synchronous event subscribers are not exception-isolated; duplicate JSON properties and unusual escapes are not covered.
- Testing gaps: no third-victory visible-outcome action assertion; no delayed-confirm-after-cancel manager test; no serialized Cancel focus test; no console end-to-end mutation-first save-failure/recovery journey; no canonical-contribution or RNG-upper-bound adapter tests.
- Failed reviewers: none. The alternate-provider pass was skipped because no supported peer CLI was available.

---

> **Verdict:** Not ready
>
> **Reasoning:** No P0/P1 issue survived synthesis, but three independently validated P2 defects break cancellation, controller navigation, and the final project-milestone feedback contract.
>
> **Fix order:** #1 cancelled-confirm guard -> #2 Cancel focus restoration -> #3 victory publication ordering -> focused Unity action regressions -> full regression.

## Personal Flow Result

`rework required`

Return the task to `work`. The required rework is tracked as RW7-RW9 in `plan.md`; QA must wait until those items and their focused regressions are complete.
