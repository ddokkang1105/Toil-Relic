# Learning: Physical slots and item categories must be separate

## Context

Equipment must support two independently occupied ring and earring positions while retaining serializable saves in console and Unity.

## Reusable insight

Persist `{ slot, equipmentId }` entries, but validate the entry against a separate equipment category. A category describes where an item may mount; a slot describes the specific occupied position. This preserves parity across JSON serializers and makes future catalog additions additive.

## Evidence

New-game normalization writes only the starter primary-weapon entry, and console QA rendered the remaining 11 slots as empty.

## Applies when

Adding equipment, cosmetic, loadout, or attachment systems with repeated physical positions.
