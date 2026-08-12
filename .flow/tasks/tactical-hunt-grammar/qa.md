# QA

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --filter TacticalHuntGrammar` | Pass: 8/8 | Deterministic Attack, Defend, Potion guard/use, Flee success/failure, defeat, parity fixture, and modifier tests. |
| `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj` | Pass: 160/160 | Full console regression after review fixes. |
| Unity PlayMode category `TacticalHuntGrammar` | Pass: 3/3 | Shared fixture plus real scene locked-intent tests. |
| Unity PlayMode category `PlayModeActionContracts` | Pass: 23/23 | Existing serialized action, ordering, guard, and persistence contracts. |
| Focused `P0_BattleTextUsesReadableNonoverlappingRenderedBounds` | Pass: 1/1 | 18px two-line phase cue fits and does not overlap enemy/log rectangles. |
| Full Unity PlayMode assembly | Pass: 179, fail: 0, skipped: 3 of 182 | Skips are the existing opt-in graphics evidence cases; graphics capture was run separately. |
| Full Unity EditMode assembly | Pass: 2/2 | Editor scene-bootstrap changes compile and existing bootstrap contracts pass. |
| Graphics layout evidence capture | Pass: 1/1 | Generated 1280x720 and 800x600 battle/camp/equipment/title PNGs under `qa-layout-evidence-r6/`. |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Inspect battle at 1280x720 | Full label, marker, and cue remain readable without overlap | Pass |
| Inspect battle at 800x600 | Same intent information remains readable and contained | Pass |
| Trace invalid Potion in both runtimes | Current intent and turn index remain unchanged | Pass through captured console and real Unity button tests |
| Trace consuming/terminal actions | Defend, valid Potion, failed Flee advance once; successful Flee and lethal outcomes stop | Pass through deterministic console and Unity action contracts |
| Compare console and Unity contract rows | Names, markers, cues, profiles, and modifiers match | Pass through shared fixture tests |

## Known limits

- Tactical values and cue wording are verified for correctness and readability, not tuned through a fresh human playtest.
- No save schema was changed, so recovery from an in-progress battle is intentionally outside this slice.
- Platform validation used the local Windows console and Unity Editor environment.
