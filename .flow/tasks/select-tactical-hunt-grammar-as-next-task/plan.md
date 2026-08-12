# Plan

## Readiness

- Status: executable documentation-only update.
- Open blockers: none.

## Steps

1. [x] Confirm Purposeful Hunt and Ironclad Save Envelope are closed and read their QA and follow-ups.
2. [x] Compare Tactical Hunt Grammar with the remaining candidates against current console and Unity combat seams.
3. [x] Update the existing ideation artifact without deleting the original shortlist or rejection history.
4. [x] Review the changed recommendation and validate HTML structure, fragment links, UTF-8 text, and patch hygiene.

## Affected paths

- `docs/ideation/2026-08-06-open-ideation.html`
- `.flow/tasks/select-tactical-hunt-grammar-as-next-task/`

## Validation

- Inspect the documentation diff and recommendation traceability.
- Parse the HTML with Python's standard-library parser.
- Confirm every local fragment link has a matching `id` and the six original idea cards remain.
- Decode changed text as UTF-8 and check the current selection, completed predecessors, and scope ceiling markers.
- Run `git diff --check` only against the paths changed by this task.

## Rollback or migration

- Documentation-only; revert the latest continuation wording if the recommendation changes.
- No runtime, save-data, or content migration.
