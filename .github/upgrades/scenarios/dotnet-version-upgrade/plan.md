# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade the GitTransformer Blazor WebAssembly application from .NET 9 to .NET 10 LTS and update Radzen to the latest stable compatible release.
**Scope**: One SDK-style ASP.NET Core project, 6 NuGet packages, and approximately 1,304 lines of application code.

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: One project already on modern .NET, no project dependencies, no incompatible packages, and low assessed upgrade difficulty.

## Tasks

### 01-upgrade-application: Upgrade the Blazor WebAssembly application and dependencies

Upgrade `GitTransformer/GitTransformer.csproj` to `net10.0`, verify the .NET 10 SDK and repository SDK configuration, and align Microsoft framework packages with the target framework. Update `Radzen.Blazor` to the latest stable .NET 10-compatible version and apply other compatible package updates identified by the assessment, including Newtonsoft.Json where appropriate.

Review the ten low-impact behavioral compatibility findings around keyed services and URI handling, restore dependencies, and resolve any resulting build or runtime compatibility issues. Validate the complete solution and available tests after the atomic upgrade, treating warnings as errors to resolve rather than suppress.

**Done when**: The project targets `net10.0`; Radzen and applicable dependencies are on stable compatible versions; restore succeeds; the full solution builds with zero errors and zero warnings; all discovered tests pass; and the project no longer reports dependency conflicts or known package vulnerabilities.
