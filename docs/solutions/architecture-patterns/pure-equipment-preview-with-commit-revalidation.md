---
title: Keep equipment previews pure and revalidate at commit
date: 2026-08-04
category: architecture-patterns
module: Mirrored equipment comparison and command boundaries
problem_type: architecture_pattern
component: service_object
severity: medium
applies_when:
  - "A player previews a state-changing equipment action before confirming it"
  - "Console and Unity implementations must preserve one gameplay contract"
  - "Ownership, slot occupancy, or game context can change while a preview is open"
related_components:
  - "testing_framework"
  - "development_workflow"
tags:
  - "equipment-comparison"
  - "preview"
  - "commit-revalidation"
  - "console-unity-parity"
  - "time-of-check-time-of-use"
  - "shared-contract-fixture"
---

# Keep equipment previews pure and revalidate at commit

## Context

Toil Relic implements gameplay separately in the console prototype and Unity runtime. Equipment comparison therefore needs two properties at once: selecting a candidate must remain a read-only preview, and confirmation must not trust state that may have changed while the preview was visible.

Treat the displayed comparison as advisory data, not as authorization to mutate. The reusable boundary is:

```text
select slot and candidate -> calculate a pure preview -> confirm
-> evaluate current state again -> mutate -> publish feedback -> save
```

Earlier focused review showed why both halves matter: commit-time revalidation prevented an invalid cross-slot mutation, but filtering still offered an item already equipped in another compatible physical slot and produced a misleading preview. Candidate filtering and final command validation now consume the same evaluator rules. (session history)

## Guidance

### Make preview a query with an explicit physical slot

Both runtimes expose mirrored comparison evaluators in `src/ToilRelic/Models/EquipmentComparison.cs:103` and `unity/Assets/Scripts/Core/EquipmentComparison.cs:107`. A comparison receives the player, the destination slot, and a candidate identifier. It returns typed identities, rejection reason, stat deltas, projected totals, and commit capability without calling an equipment mutator, event publisher, or save service.

The physical destination slot must remain explicit through filtering, preview, command, and mutation. This distinguishes repeated categories such as Ring 1 and Ring 2. The evaluator classifies an item already occupying the selected slot as `SameItem`, which is a valid comparison but not a committable action; the same item in another physical slot is `CandidateEquippedElsewhere` (`src/ToilRelic/Models/EquipmentComparison.cs:36`, `src/ToilRelic/Models/EquipmentComparison.cs:141`).

Use the evaluator for candidate-list membership as well as detail rendering. The console filters through `IsCandidateAvailable` (`src/ToilRelic/Game.cs:296`), and Unity uses the mirrored method when rebuilding candidate rows (`unity/Assets/Scripts/UI/EquipmentPanelController.cs:365`). This prevents the UI and the command boundary from growing separate legality rules.

### Revalidate identifiers instead of committing a cached result

The preview's `CanCommit` value controls UI availability, but it is not sufficient authorization to mutate. Confirmation should send stable identifiers—the destination slot and candidate ID—to an authoritative command boundary, which evaluates the latest state again.

The console deliberately calls `Compare` or `EvaluateUnequip` once for display and again after confirmation, immediately before `Player.Equip` or `Player.Unequip` (`src/ToilRelic/Game.cs:222`, `src/ToilRelic/Game.cs:259`). Unity keeps selection and preview inside `EquipmentPanelController`, then passes identifiers to `GameManager` (`unity/Assets/Scripts/UI/EquipmentPanelController.cs:183`). `GameManager` checks the Camp context, recalculates comparison or unequip eligibility, rejects stale input, and only then invokes the player mutator (`unity/Assets/Scripts/Core/GameManager.cs:291`).

Keep defensive checks in the mutator too. The console mutator independently verifies ownership, catalog presence, slot compatibility, cross-slot duplicate use, and the mandatory primary-weapon rule (`src/ToilRelic/Models/Player.cs:57-75`). The evaluator makes commands understandable; the mutator preserves domain integrity.

### Keep rejected paths silent and successful side effects ordered

A rejected or cancelled action exits before mutation, gameplay events, and persistence. After a successful Unity mutation, the established order is player publication, battle-log feedback, and then persistence (`unity/Assets/Scripts/Core/GameManager.cs:304`). The console likewise saves only after a successful mutation (`src/ToilRelic/Game.cs:248`, `src/ToilRelic/Game.cs:285`).

This pattern does not make saving transactional. The repository's settled contract is mutation-first: if the later save attempt fails, the in-memory equipment change remains applied and failure feedback is authoritative. Adding rollback or retry would be a separate product decision; see [Preserve Unity terminal outcomes through level-ups and save failures](../ui-bugs/unity-status-event-save-failure-contracts.md).

### Share behavior vectors across mirrored implementations

Mirrored source will drift unless both runtimes consume one behavioral contract. Canonical vectors live in `unity/Assets/Tests/Fixtures/EquipmentComparisonContracts.json`, and the console test project links that same file into its output (`tests/ToilRelic.Tests/ToilRelic.Tests.csproj:22`).

Both suites serialize player state before and after comparison to prove that evaluation is read-only (`tests/ToilRelic.Tests/EquipmentComparisonTests.cs:14`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:343`). Compare shared equipment contributions and replacement deltas where runtime baselines intentionally differ; do not force identical absolute values merely to make the implementations look alike.

## Why This Matters

The interval between preview and confirmation is a time-of-check/time-of-use boundary. Ownership, catalog data, slot occupancy, or game context can change while the UI is open. Committing a cached comparison would turn disposable presentation state into stale authority.

Pure preview also keeps navigation cheap: selecting, cancelling, reopening, or refreshing requires no rollback. Typed revalidation localizes rejection feedback, while the defensive mutator remains the last guard against invalid state. A shared fixture then turns two source implementations into one testable behavioral contract.

## When to Apply

Apply this pattern when:

- a UI displays the predicted outcome of a later state-changing action;
- selection and confirmation are separated by user input, animation, asynchronous work, or another refresh boundary;
- the same rule is implemented in multiple runtimes or clients;
- physical destination identity affects legality; or
- rejected commands must produce no events or persistence activity.

The same shape also fits crafting previews, loadout swaps, purchases, or any confirmable action whose eligibility can become stale. It is unnecessary when preview and mutation occur atomically inside one authoritative operation with no externally visible intermediate state.

## Examples

Use this command shape in every runtime:

```csharp
var preview = EquipmentComparisonEvaluator.Compare(player, slot, candidateId);
Render(preview); // read-only and disposable

// Later, after confirmation, at the authoritative command boundary:
var current = EquipmentComparisonEvaluator.Compare(player, slot, candidateId);
if (!current.CanCommit)
{
    return Rejected(current.Reason);
}

if (!player.Equip(slot, candidateId))
{
    return MutationRejected();
}

PublishPlayer();
ReportSuccess();
SaveProgress();
```

Verification should cover four distinct boundaries:

1. For every comparison and unequip vector, serialize state before and after evaluation and require equality.
2. Preview, cancel, and stale-confirmation scenarios must leave state, events, and save bytes untouched.
3. Successful commands must prove mutation, feedback/event order, save, and reload behavior.
4. Both runtimes must consume the same vectors, including same-item, cross-slot duplicate, empty slot, mandatory slot, and runtime-relative replacement cases.

The Unity stale-selection contract removes ownership after preview and proves that confirmation yields local rejection without events or a save (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:902`). For broader real-UI verification, use [Deterministic Unity Play Mode action contracts through serialized UI](../best-practices/deterministic-unity-playmode-action-contracts.md).

## Related

- [Resume blocked Personal Flow QA with layered environment validation](../workflow-issues/resume-blocked-personal-flow-qa-with-layered-environment-validation.md)
- [Deterministic Unity Play Mode action contracts through serialized UI](../best-practices/deterministic-unity-playmode-action-contracts.md)
- [Preserve Unity terminal outcomes through level-ups and save failures](../ui-bugs/unity-status-event-save-failure-contracts.md)
- The domain definition is [Equipment Comparison Preview](../../../CONCEPTS.md#equipment-comparison-preview).
