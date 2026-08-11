# Learning: Validated authority save envelope

## Context

Interrupted writes and ambiguous post-mutation exceptions make filesystem return values insufficient for deciding which save generation is authoritative. The console and Unity implementations needed one bounded behavioral contract without sharing runtime filesystem code.

## Reusable insight

Make validation the authority gate: flush and validate stage, transfer authority with one filesystem primitive, recover only from exact validated LKG bytes, treat post-mutation exceptions as unknown commit states settled by a fresh Load, and delete the final validated authority last. Pin the exact shared failure-vector manifest so a missing test cannot masquerade as parity.

## Evidence

- Durable learning: `docs/solutions/architecture-patterns/validated-authority-save-envelope.md`.
- Mechanical documentation validation: 12 paths, 0 SHAs, 3 links, 0 flags; frontmatter parser-safe.
- Semantic grounding: 15 code-behavior claims verified, 2 count assertions complete, 0 contradicted or unverifiable claims.
- Final QA: console 152/152, Unity save contracts 66/66 twice, full Unity PlayMode 176 passed/0 failed, exact shared 33/33 case parity.

## Applies when

Use this pattern for synchronous single-writer persistence on a verified local volume when one prior validated generation is enough, automatic recovery must be visible, and correctness-critical mutation boundaries can be modeled deterministically.
