# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6000.3.9f1 project. Gameplay C# code lives in `Assets/02_Scripts/`, grouped by responsibility (`01 Player`, `02 Box`, `03 Managers`, and `04 UI`). The playable scene is `Assets/01_Scenes/Main.unity`; prefabs are under `Assets/03_Prefabs/`, and input actions are in `Assets/05_InputSystem/PlayerControls.inputactions`. Rendering assets live in `Assets/Settings/`. Unity package versions are recorded in `Packages/manifest.json`, while editor and build settings live in `ProjectSettings/`. Keep each asset's `.meta` file with it so Unity references remain stable.

## Build, Test, and Development Commands

- Open the repository with Unity Hub using Editor **6000.3.9f1**, then open `Main.unity` and press Play for local development.
- Use **File > Build Profiles** in the Editor to create a player build. `Main.unity` is the enabled scene in Build Settings; there is no repository build script yet.
- Run tests from **Window > General > Test Runner**. For CI, use `Unity -batchmode -projectPath <repo-path> -runTests -testPlatform EditMode -testResults <results.xml> -quit`, replacing `Unity` with the installed Editor executable.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, braces on separate lines, `PascalCase` for types and methods, and `camelCase` for fields and locals. Keep gameplay types in the `OneMoreBox` namespace. Use `[SerializeField] private` for Inspector references and tuning values instead of public fields. Name MonoBehaviour files after their classes, such as `BoxController.cs`. Preserve the numbered asset-folder scheme and use descriptive Unity asset names.

## Testing Guidelines

The Unity Test Framework package is installed, but this repository has no committed tests or coverage threshold yet. Add Edit Mode tests for isolated logic and Play Mode tests for scene, input, or physics behavior under `Assets/Tests/`. Name test files `<Feature>Tests.cs` and test methods for the behavior they verify. Before submitting gameplay changes, run relevant tests and check the `Main.unity` flow in Play Mode.

## Commit & Pull Request Guidelines

The available history contains short imperative commit subjects (`Install Unity`, `Initial commit`); follow that pattern and keep each commit focused. In pull requests, describe the player-visible change, list the scene or prefab assets affected, and include test or Play Mode results. Add screenshots or a short clip for UI or visual changes. Review the diff for unintended `.meta`, scene, and `ProjectSettings/` changes before submitting.
