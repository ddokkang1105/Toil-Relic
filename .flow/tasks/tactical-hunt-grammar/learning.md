# Learning: Lock intent in orchestration and mirror only pure rules

## Context

The same tactical turn grammar had to work in a blocking C# console loop and an event-driven Unity scene without moving reward, save, or UI authority.

## Reusable insight

- Treat a telegraphed intent as encounter state owned by orchestration: reveal and lock it, resolve one consuming action against it, then advance. Presentation only renders published state.
- Invalid actions must return to the same locked state without consuming randomness. Terminal actions interrupt before a new intent is published.
- Mirror small pure rule modules per runtime and bind them to one semantic fixture. Do not force runtime-specific orchestration into shared code.
- Prove composition through the real input path as well as pure functions. A test that only observes the next label does not prove that the next action resolves that label.
- Prefer stable readable geometry over shrinking central decision text. Long tactical cues exposed a hidden one-line layout assumption that full regression plus rendered evidence caught.

## Evidence

- Console tactical tests expanded from 3 to 8 and the full suite passes 160/160.
- Unity targeted tactical and action-contract categories pass 3/3 and 23/23; full PlayMode passes 179 with 3 intentional opt-in skips.
- The shared fixture covers canonical intents, both focused profiles, and fallback enemies in both runtimes.
- 1280x720 and 800x600 rendered battle evidence shows the complete intent label, marker, and cue.

## Applies when

Use this pattern when two runtimes must share gameplay meaning but have different input, event, UI, random, or persistence boundaries.
