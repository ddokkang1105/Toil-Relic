# Decisions

## Confirmed

- Select **Tactical Hunt Grammar** as the next task.
- Purposeful Hunt Vertical Slice and Ironclad Save Envelope are complete, so the original shortlist's first and second recommendations no longer compete for the next slot.
- The next product gap is combat expression: Unity exposes Attack, Defend, Flee, and Potion during a player-action phase, but enemy intent is not revealed and Defend is a flat reduction; the console fight still resolves autonomously.
- The shared turn grammar reveals and locks the next enemy intent before each player choice, accepts one Attack/Defend decision, resolves the telegraphed enemy action and its outcome, then reveals the next intent when combat continues.
- Console combat becomes player-stepped for each tactical turn; Unity retains its existing action buttons. Both runtimes share intent labels and outcome meaning rather than presentation technology.
- The recommended first slice proves at least two intents with different best responses across one or two existing enemy patterns; repeating only Attack or only Defend must be observably disadvantageous for at least one intent.
- Enemies outside the focused pattern slice use a basic-attack intent through the same grammar rather than bypassing it.
- Every intent has a concise shared text label. Unity also supplies a redundant non-color cue so the central tactical signal is not color-only.
- Keep the future task below a full combo system, additional combat buttons, a broad enemy-roster expansion, quarry mastery, or a new session architecture.
- The user's requested direction is workable: current code and tests expose clear cross-runtime seams, and no repository evidence invalidates the recommendation.

## Rejected options

- **Quarry Mastery Loop:** becomes more valuable after enemies have differentiated, learnable tactical patterns; building it first would still risk documenting shallow differences.
- **Unity Standalone Windows durability proof:** useful capability expansion, but the completed envelope already has bounded, verified Windows Editor and console guarantees; the follow-up is Medium priority and offers less immediate player value.
- **One Content Source, Two Runtime Builds:** still risks hardening content contracts while combat meaning is about to change.
- **Relic Vow Commission / catalog expansion:** depends on broader product and progression decisions and does not improve the moment-to-moment hunt as directly.
- **Save-status learning refresh:** worthwhile maintenance, but explicitly Low priority and not a player-facing next feature.

## Open questions

- Exact intent names, damage/reduction values, enemy assignment, Potion/Flee interaction, terminal interruption behavior, and save compatibility belong to the future Tactical Hunt Grammar product-contract stage.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Personal Flow probe | OpenSpec CLI present without project artifacts; gstack and CE active; OMX unavailable | `probe-frameworks.ps1` on 2026-08-11; first attempt hit Windows execution policy, successful retry used process-local bypass |
| OpenSpec | Skipped because this repository has no OpenSpec project artifacts | Probe result |
| CE planning | Answer-seeking repository analysis; recommendation and execution steps captured in the canonical Personal Flow artifacts | `ce-plan` universal-planning route |
| Discovery / brainstorm | Skipped: the candidate, trade-offs, and recommendation direction were already named; repository evidence resolved the remaining ranking question | Small-profile direct analysis |
| External research | Skipped: the decision depends on current local implementation and completed task evidence, not changing external facts | Repository-grounded analysis |
| Multi-agent implementation | Skipped: no independent implementation workstreams exist in this documentation-only task | Small profile; OMX unavailable |
