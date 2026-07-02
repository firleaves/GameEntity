# GameEntity

GameEntity is a lightweight, engine-agnostic entity and lifecycle framework for C#.

The repository contains two package surfaces:

- `GameEntity`: the pure C# core library.
- `GameEntity.Unity`: the optional Unity integration package.

The core package does not depend on Unity. The Unity package adapts the core hierarchy to Unity lifecycle, logging, component views, and editor inspection.

## Repository Layout

```text
src/
  GameEntity/              Pure C# core library
    Core/                  Public entity API and internal hierarchy implementation
      Hierarchy/               Ownership hierarchy, node stores, scene registry
      Scheduling/          Entity update scheduler
      Diagnostics/         EntityHierarchy snapshot and validation views

tests/
  GameEntity.Tests/        Pure C# core tests

apps/
  GameEntity.CoreTestApp/
                            Core hierarchy smoke test console app

unity/
  GameEntity.Unity/        Unity Package Manager package

docs/                      Project documentation
tools/                     Build and maintenance scripts
```

The original Unity-project-based version is preserved as:

```text
branch: legacy/original-unity-project
tag:    legacy-unity-project-v0.1.0
```

## Core Library

The core library lives in:

```text
src/GameEntity
```

It targets:

```text
net8.0
netstandard2.1
```

Build it with:

```bash
dotnet build "src/GameEntity/GameEntity.csproj"
```

## Unity Package

The Unity integration package lives in:

```text
unity/GameEntity.Unity
```

Install it in Unity Package Manager with:

```text
https://github.com/firleaves/GameEntity.git?path=unity/GameEntity.Unity
```

Or pin a version:

```text
https://github.com/firleaves/GameEntity.git?path=unity/GameEntity.Unity#v0.1.0
```

The Unity package includes a prebuilt `GameEntity.dll` under:

```text
unity/GameEntity.Unity/Runtime/Plugins/GameEntity.dll
```

That DLL is built from the pure C# core in `src/GameEntity`.

## License

License text has not been finalized yet.
