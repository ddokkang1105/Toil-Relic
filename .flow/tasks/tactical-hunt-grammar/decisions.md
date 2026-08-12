# Decisions

## Confirmed

- Use the Product Contract in `docs/plans/2026-08-11-002-feat-tactical-hunt-grammar-plan.md` as product authority.
- Use Power Attack `[POWER]`, Exposed Opening `[OPEN]`, and Direct Strike `[BASIC]` as canonical shared intent labels.
- Power Attack adds 2 raw damage and Defend reduces it by 6; other strikes use the existing 3-point Defend reduction.
- Exposed Opening grants Attack +3 damage; its enemy strike remains otherwise normal.
- Mine Vermin alternates Opening → Power, Rust Golem alternates Power → Opening, and all other current enemies use Direct Strike.
- A valid Potion use and failed Flee consume the locked intent; invalid Potion does not; successful Flee is terminal.
- Remove the console wall-clock timeout because decision time must be safe.
- Intent state is encounter-local and does not alter the save schema.

## Rejected options

- Random intent selection: it weakens learnability and deterministic parity coverage.
- Persisting mid-combat intent: it expands the save envelope beyond this slice.
- Adding new combat actions or a generalized content-authored intent graph: neither is required to prove tactical value.
- Keeping console combat autonomous: it would preserve the runtime experience gap the task exists to close.

## Open questions

- None blocking. Exact method boundaries and test placement belong to implementation planning.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Brainstorm | Product Contract captured; user requested uninterrupted recommended defaults | `docs/plans/2026-08-11-002-feat-tactical-hunt-grammar-plan.md` |
| Office hours | Skipped; feature value and actor are already established | Selection task and ideation continuation |
| CEO review | Skipped; bounded next task was already ranked and selected | `.flow/tasks/select-tactical-hunt-grammar-as-next-task/` |
| Design review | Deferred to code/document review; no new layout is introduced | Existing Battle Panel surface |
| Multi-agent discovery | Skipped; requirements and relevant seams were already grounded | Selection task review plus targeted repo scan |
