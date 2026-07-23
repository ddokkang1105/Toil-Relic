---
title: Validate Unity Play Mode screenshot pixels before accepting visual evidence
date: 2026-07-23
category: best-practices
module: Unity Play Mode visual QA
problem_type: best_practice
component: testing_framework
severity: medium
applies_when:
  - "Capturing Unity Play Mode screenshots from batch-mode test runs"
  - "Using RenderTexture and Texture2D output as layout or visual QA evidence"
  - "Running Unity tests with headless or no-graphics command-line options"
tags: [unity, playmode, batchmode, nographics, screenshot-validation, visual-qa, rendertexture]
---

# Validate Unity Play Mode screenshot pixels before accepting visual evidence

## Context

The optional layout-evidence test in `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs` creates a camera, renders the Canvas into a `RenderTexture`, copies the result into a `Texture2D`, and writes PNG files. Its final assertions only verify that each file exists and has a non-zero length.

During save UX QA, the first filtered capture run used `-batchmode -nographics`. Unity reported the test as passed and wrote every expected PNG, but direct inspection showed uniform gray frames. Those files were rejected and replaced by a graphics-enabled batch run. This exposed a useful distinction: a passing capture test proves that the capture pipeline completed, not that the pixels are usable visual evidence.

An earlier HUD QA session also rejected a capture of the wrong or obscured Editor view. That separate failure mode reinforces the same trust boundary: inspect the actual image content and confirm that it represents the intended Game view before accepting it. *(session history)*

## Guidance

Treat visual QA as three independent checks:

1. **Capture execution:** run the filtered Play Mode capture test with `-batchmode`, but omit `-nographics` when the output itself is the evidence.
2. **Artifact validity:** verify the test-result XML, expected filenames, file sizes, requested dimensions, and non-uniform pixel content.
3. **Layout behavior:** keep deterministic geometry and typography assertions separate, then visually inspect every required state and viewport.

For this repository, a graphics-enabled evidence run follows this shape:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
$project = (Resolve-Path 'unity').Path
$task = (Resolve-Path '.flow/tasks/저장-ux-보강').Path
$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR = Join-Path $task 'qa-layout-evidence'

& $unity `
    -batchmode `
    -projectPath $project `
    -runTests `
    -testPlatform PlayMode `
    -assemblyNames ToilRelic.PlayModeTests `
    -testFilter ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_CaptureLayoutEvidenceWhenRequested `
    -testResults (Join-Path $task 'qa-unity-layout-results.xml') `
    -logFile (Join-Path $task 'qa-unity-layout.log')
```

Do not add `-nographics` to this command. It remains appropriate for the ordinary Play Mode regression suite when no rendered frames are being accepted as evidence.

A lightweight pixel sanity check can reject a uniform frame before manual inspection:

```powershell
Add-Type -AssemblyName System.Drawing

Get-ChildItem $env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR -Filter '*.png' | ForEach-Object {
    $bitmap = [System.Drawing.Bitmap]::new($_.FullName)
    try {
        $colors = [System.Collections.Generic.HashSet[int]]::new()
        for ($y = 0; $y -lt $bitmap.Height; $y += 24) {
            for ($x = 0; $x -lt $bitmap.Width; $x += 24) {
                [void]$colors.Add($bitmap.GetPixel($x, $y).ToArgb())
            }
        }

        if ($colors.Count -le 1) {
            throw "$($_.Name) is uniform at the sampled pixels."
        }
    }
    finally {
        $bitmap.Dispose()
    }
}
```

This check is deliberately only a rejection guard. Multiple colors do not prove that the correct state rendered, that text is readable, or that panels do not overlap. Direct inspection and the Play Mode layout assertions are still required.

## Why This Matters

File-existence and byte-length assertions can both pass for a semantically empty render. If QA records those files as screenshots without inspecting their pixels, the evidence can falsely certify a layout that was never visible.

Separating the checks also makes failures diagnosable:

- A failed test result or missing file indicates a capture-pipeline failure.
- A uniform image indicates invalid rendering evidence.
- A geometry assertion indicates a deterministic layout-contract violation.
- A visually wrong but mechanically valid image indicates a state, readability, hierarchy, or composition problem.

The save UX QA accepted eight replacement screenshots only after the filtered graphics-enabled test passed, each file matched its requested `1280x720` or `800x600` dimensions, sampled pixels were non-uniform, and every Title, Camp, and Battle state was inspected directly.

## When to Apply

- Producing screenshots from Unity batch-mode Play Mode tests.
- Verifying UI layout at more than one viewport size.
- Capturing Canvas output through `RenderTexture` or an auxiliary camera.
- Using screenshots as release, review, or task-close evidence.
- Re-running a capture after changing graphics-related Unity command-line flags.

## Examples

The capture path is implemented in `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`:

- Lines 613–644 select the evidence directory and capture eight state-and-viewport combinations.
- Lines 1062–1101 render the Canvas through a camera and write the PNG.
- Lines 1114–1115 assert only that the file exists and is non-empty.

The layout contract is tested independently in the same file:

- Lines 421–450 verify that Title, Camp, and Battle panels fit below the visible top regions.
- Lines 872–910 verify the minimum top-region gap, button height, action bindings, and vertical button spacing.

The rejected `-nographics` attempt and the accepted graphics-enabled evidence set are recorded in `.flow/tasks/저장-ux-보강/qa.md`.

## Related

- [Unity Play Mode HUD contract testing without a runtime assembly reference](unity-playmode-hud-contracts.md)
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- `.flow/tasks/저장-ux-보강/qa.md`
