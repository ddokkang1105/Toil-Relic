# Decisions

## Confirmed

## Rejected options

## Open questions

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
## Decisions

- Profile: `standard`; HUD completion changes a visible user journey and needs a design/scene-wiring review.
- Framework detection was recorded at task start. OpenSpec is installed but not active and the project has no OpenSpec artifacts; task.md is the requirements source.
- Current discovery: HudController exists, but no HudController or TMP text components are found in SampleScene or its bootstrap script. The work must include scene construction and serialized field wiring.
- Design review fallback: use a persistent upper-left HUD with four concise labels. This preserves centered action panels and reuses the existing TMP controller; no external design artifact is available.
- Implementation adjustment: TMP Essential Resources are absent and cannot be imported from the installed UGUI package. Use serializable uGUI `Text` fields instead; this preserves the HUD display contract without a missing font-asset dependency.
