# Repository Guidelines

## Project Structure & Module Organization
This repository contains two game implementations that should stay feature-aligned.

- `src/ToilRelic/`: C# console prototype (`Program.cs`, `Game.cs`, `Models/`, `Systems/`, `Util/`).
- `unity/Assets/Scripts/`: Unity gameplay/runtime scripts, organized by `Core/`, `Systems/`, `Data/`, `UI/`, `Save/`.
- `unity/UNITY_SETUP.md`: Unity scene/setup instructions.
- `README.md`: high-level project overview and run guidance.

<!-- When changing gameplay logic (combat, loot, leveling, crafting), update both `src/ToilRelic` and `unity/Assets/Scripts` in the same PR. -->

## Build, Test, and Development Commands
- `cd src/ToilRelic && dotnet run`: run the console game locally.
- `cd src/ToilRelic && dotnet build`: compile and catch C# errors.
- `cd src/ToilRelic && dotnet format` (optional): apply standard .NET formatting if installed.
- Unity: open your Unity project, copy/update `unity/Assets/Scripts`, then press Play.

## Coding Style & Naming Conventions
- Language: C# (console + Unity).
- Indentation: 4 spaces, UTF-8 text, braces on new lines.
- Naming: `PascalCase` for types/methods/properties, `camelCase` for locals/parameters, private fields as `_camelCase` (console) or serialized `camelCase` (Unity).
- Keep systems focused: data in `Models/Data`, behavior in `Systems`, orchestration in `Game`/`GameManager`.

## Testing Guidelines
There is no automated test project yet. Minimum validation before commit:

- `dotnet build` succeeds for console code.
- Manually verify key gameplay loop changes (hunt, reward, craft, level progression) in console and Unity.
- For balancing changes, include tested values in PR notes (example: EXP curve, loot rates).

## Default Development Workflow
For every request that changes code, Unity scenes/assets, configuration, tests, or documentation, use the Personal Flow workflow by default, even when the user does not explicitly invoke `$personal-flow`.

- Create a task under `.flow/tasks/<task-slug>/`, or resume a directly related existing task.
- Follow `.flow/WORKFLOW.md` and keep the selected stage and gates current in `state.yaml`.
- Record task, plan, work, review, QA, and close outcomes in the task artifacts; do not claim review or QA completion without its corresponding artifact.
- Do not apply Personal Flow to questions, explanations, read-only investigation, or status reports unless the user requests it.
- Skip Personal Flow only when the user explicitly asks to work without it, to work quickly without artifacts, or not to record the workflow.

## Workflow Status Reporting
When reporting progress for an open Personal Flow task, end the report with the next stage and an exact copy-paste command the user can use to continue. Do not provide a next-stage command once the task is closed.

## Commit & Pull Request Guidelines
- Use concise, imperative commit messages, matching existing history:
  - `Add XP and level progression to console version`
  - `Show level progress only and hide EXP text`
- Prefer one logical change per commit.
- PRs should include:
  - What changed and why.
  - Affected paths (console, Unity, or both).
  - Manual verification steps and results.
  - Screenshots/gifs for Unity UI changes when applicable.

## Security & Config Tips
- Do not commit secrets, tokens, or machine-specific credentials.
- Use SSH remote for GitHub operations.
