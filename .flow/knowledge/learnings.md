# Project learnings

## Equipment slots are physical positions, not item types

Store equipment state as a serializable collection of `{ slot, equipmentId }` entries and validate entries using a separate item category. This supports independent ring and earring positions, keeps console and Unity persistence aligned, and makes catalog expansion additive. New or legacy-normalized players must own and equip only the primary starter weapon; optional slots remain absent rather than being filled with placeholders.

## Unity Play Mode QA must validate the configured scene

Keep Play Mode smoke tests focused on the actual scene’s runtime contract: required manager components, serialized data references, action bridges, and persistent button bindings. In batch mode, omit `-quit` when using `-runTests`, because the Test Runner terminates Unity itself. For Input System-only projects, create EventSystems with `InputSystemUIInputModule`, not `StandaloneInputModule`.

## Save-aware title menus should trust successful loads

Enable Continue only after deserializing a valid save, not merely because a file exists. This prevents a corrupt or outdated save file from advertising an unavailable resume path. Keep destructive New Game behavior out of shared persistent-data tests and verify its UI contract separately.
