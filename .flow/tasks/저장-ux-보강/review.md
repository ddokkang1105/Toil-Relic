# Review

## Scope

- Base: `d63d55bad0870972b555fe503665a673e48607e8`
- Reviewed head: `5d52720f4d9f538df8e8a2941476c2670fa40ad6`
- Mode: report-only re-review
- Review run: `C:\Users\User\AppData\Local\Temp\compound-engineering-codex\ce-code-review\20260723-082835-60a0d30f`

## Findings

| # | Severity | Finding | Disposition | Verification |
|---|---|---|---|---|
| 1 | P1 | `SaveSystem.Load` checks only required JSON kinds. A full-shape console payload with impossible core values is normalized by `Player.FromSaveData`, returned as `Loaded`, exposed through Continue, and can later overwrite the original bytes without New Game authorization. | Reopen `work`. Validate stable core value invariants before normalization while keeping empty inventory and equipment fields compatible; add direct-load and Title-flow preservation fixtures. | Correctness, reliability, and adversarial reviewers agreed. The independent validator traced `SaveSystem.cs:54-55` through `Player.FromSaveData` and `SaveProgress`. |
| 2 | P1 | Unity validates `PlayerState` only after `JsonUtility` has erased field-presence information. A version-2 payload can supply valid `maxHp`, `level`, and one inventory slot while omitting zero-capable core fields such as `hp`, `experience`, and `treasureCount`; those omissions become valid zeroes and the save is returned as `Loaded`. | Reopen `work`. Add a presence-aware raw versioned payload check in `SaveService` for stable core fields, keep equipment fields optional, then apply value validation; add partial-omission PlayMode fixtures. | Maintainability and adversarial reviewers agreed. The independent validator reproduced the omission path and confirmed Continue can become available. |
| 3 | P1 | The new exact `version == 2` gate rejects documented version-1 saves and historical versionless Unity envelopes, breaking the repository's compatibility decision before normalization can run. | Reopen `work`. Accept version 1 and the historical versionless envelope alongside version 2, continue rejecting unknown versions such as 999, and add compatibility fixtures with byte-preservation assertions. | Reliability and API-contract reviewers agreed. The independent validator confirmed `.flow/tasks/equipment-system-foundation/decisions.md` requires version-1 support and history contains a versionless `SaveEnvelope`. |

## Requirements completeness

- Partial: R1-R3 and AE3. The new guards reject empty or malformed payloads, but the three validated paths above can still misclassify supported or structurally invalid saves.
- Met and unaffected by this rework: R4-R14; AE1-AE2 and AE4-AE8.
- Partial implementation units: U1 still needs core value validation; U3 still needs raw field-presence validation and legacy version compatibility.
- Implemented as planned and unaffected: U2, U4, and U5.

## Review coverage

- Lenses: correctness, project standards, testing, maintainability, reliability, API/save-format contract, adversarial fallback, and repository learnings.
- Cross-model review was unavailable because no authenticated different-provider reviewer was configured; a local adversarial reviewer and a fresh independent validator were used instead.
- Mechanical merge: 9 raw findings -> 5 deduplicated candidates -> 3 primary P1 findings; 2 narrow P2 coverage findings moved to testing gaps.
- Validator batch: 3 findings selected, 3 validated, 0 dropped, 0 validation-degraded.
- Project-standards review found no violation. The two-file learning corpus had no direct structural-save-validation precedent; its orchestration and PlayMode patterns support keeping diagnosis visible through the real Title flow.
- No source fixes were applied during review.

## Residual risks

- Unity inventory uniqueness/completeness and `treasureCount` consistency remain unspecified and should not be broadened into required validation without a contract decision.
- Console validation should explicitly reject undefined numeric `ItemType` keys as part of the core inventory invariant without making later equipment fields mandatory.

## Testing gaps

- Add table-driven console cases that isolate each required-field presence/kind guard and a full-shape invalid-value case through both `Load` and Title.
- Add Unity partial-omission cases that preserve valid required siblings, plus one-invalid-invariant-at-a-time cases for `HasValidSaveData`.
- Add version-1 and versionless Unity compatibility fixtures while retaining the unsupported-version-999 rejection case.

## Result

`rework required`
