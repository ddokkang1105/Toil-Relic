# QA

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| Python `HTMLParser` tag/id/fragment audit | Pass: balanced tags, 15 unique IDs, 12 resolved fragment links, 6 preserved idea cards | Standard-library audit against UTF-8 source |
| Current-selection and marker assertions | Pass: exactly one Tactical selection, no stale Ironclad current-selection footer, five required ASCII markers present | Same parser run |
| `git diff --check -- docs/ideation/2026-08-06-open-ideation.html` | Pass: no whitespace errors | Git emitted only the repository's LF-to-CRLF conversion notice |
| Task-artifact trailing-whitespace scan | Pass: none | PowerShell scan of every file in this task directory |
| Changed tracked path audit | Pass: only `docs/ideation/2026-08-06-open-ideation.html` | `git diff --name-only`; Personal Flow artifacts are new under this task directory |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Read the latest continuation decision | Purposeful Hunt and Ironclad are complete; Tactical Hunt Grammar is the sole next task | Pass |
| Compare the recommendation with current combat seams | Unity's shallow action loop and console's autonomous loop are both acknowledged | Pass |
| Inspect the bounded first slice | Canonical turn sequence, player-stepped console, two differentiated intents, fallback, and accessible labels are explicit | Pass |
| Inspect original ideation history | Six ranked idea cards and rejection table remain intact | Pass |
| Inspect repository scope | No console or Unity runtime file changed | Pass |

## Known limits

- This QA validates the recommendation artifact, not Tactical Hunt Grammar implementation or play feel.
- The recommendation's player-value ranking is grounded in repository state and completed roadmap work, not a new player study.
- External URLs in the historical evidence index were not re-fetched; local fragment integrity and newly cited repository paths were checked structurally.
