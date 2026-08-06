# Decisions

## Confirmed

- The selected direction is **Purposeful Hunt Vertical Slice**, chosen as the highest player-value idea from the repo-wide ideation pass.
- Requirements discovery will use the active Compound Engineering `ce-brainstorm` skill.
- The first slice uses one automatically active First Relic Project and three quarry choices shared by console and Unity.
- Each quarry's first victory guarantees one distinct project contribution; quarry reward profiles remain separate from existing generic loot.
- Completed quarries remain replayable, but their project contribution is idempotent.
- Forging grants one named relic equipment reward through the existing ownership and comparison behavior.
- Contract and project progress persist; Ironclad Save Envelope remains separate work.
- Requirements-only Product Contract: `docs/plans/2026-08-06-001-feat-purposeful-hunt-vertical-slice-plan.md`.

## Rejected options

- Ironclad Save Envelope remains the safer reliability-first alternative, but it is not the active product scope.
- A persistent rotating contract board was rejected because it creates ongoing content and framework scope before one loop is proven.
- A relic-project-first catalog was rejected because choosing among projects adds another decision layer before quarry choice has demonstrated value.
- Seasons, daily challenges, mastery, nemesis, and generalized contract authoring are deferred beyond this slice.

## Open questions

- None blocking implementation. The named content and initial balance values are explicit planning assumptions and may be tuned during work only if console/Unity parity and the Product Contract behavior remain unchanged.

## Planning decisions

- The shared live quarry set is Mine Vermin, Rust Golem, and Ruin Wraith. The unmatched fourth console enemy remains inactive legacy content and is not reachable from the product Hunt flow in this slice.
- Profile equipment has an explicit independent 35% chance. The first victory contribution is guaranteed, and completed quarry replays state that they add no project progress.
- Craft Treasure remains the optional material-economy action. Forge is a distinct, Ready-gated project-completion action that grants the Toilbound Relic without auto-equipping it.
- Console saves add schema v1 and Unity saves add schema v3. Raw version, presence, type, and closed project/equipment invariants are validated before normalization; valid legacy data upgrades only on a later ordinary save.
- A selected quarry's stable IDs remain the battle and reward authority. Invalid content at victory rejects the whole transaction, including generic loot and EXP.
- Domain commands are atomic before persistence. If saving fails after a successful mutation, the state remains visible in memory and the save failure is reported last; atomic file replacement remains deferred.
- Work is divided into U1-U7 so content, migration, pure commands, console orchestration, Unity runtime behavior, scene structure, and serialized-action/rendered evidence have distinct owners.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | OpenSpec CLI present but project artifacts absent; gstack and CE present; OMX absent | `probe-frameworks.ps1` with execution-policy bypass |
| CE brainstorm | Active | `compound-engineering:ce-brainstorm` |
| Grounding scout | Complete | `C:\Users\User\AppData\Local\Temp\compound-engineering-User\ce-brainstorm\2f7c9e1a\grounding.md` |
| Claim verification | 8 claims confirmed; scope-separation claim not code-verifiable but no invalidating dependency found | Fresh-context verifier with targeted repo reads |
| Ready for Planning | Pass: Complete, Consistent, Focused, Usable by planning | Requirements artifact audit on 2026-08-06 |
| CE plan | Complete | Repository research, institutional learnings, spec-flow tracing, and implementation-unit deepening applied in place |
| Document review | Complete | Coherence, feasibility, product, design, scope, and adversarial lenses; one confirmed responsibility-overlap fix applied, recommended product/scope resolutions incorporated, no actionable findings remain |
| Cross-model review | Skipped | No independently identifiable different-provider CLI was available |
| Ready for Work | Pass: implementation-ready, complete unit dependencies, verification contract, migration and rollback boundaries | Plan audit on 2026-08-06 |
