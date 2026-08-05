# 01-upgrade-application: Upgrade the Blazor WebAssembly application and dependencies

Upgrade `GitTransformer/GitTransformer.csproj` to `net10.0`, verify the .NET 10 SDK and repository SDK configuration, and align Microsoft framework packages with the target framework. Update `Radzen.Blazor` to the latest stable .NET 10-compatible version and apply other compatible package updates identified by the assessment, including Newtonsoft.Json where appropriate.

Review the ten low-impact behavioral compatibility findings around keyed services and URI handling, restore dependencies, and resolve any resulting build or runtime compatibility issues. Validate the complete solution and available tests after the atomic upgrade, treating warnings as errors to resolve rather than suppress.

**Done when**: The project targets `net10.0`; Radzen and applicable dependencies are on stable compatible versions; restore succeeds; the full solution builds with zero errors and zero warnings; all discovered tests pass; and the project no longer reports dependency conflicts or known package vulnerabilities.

## Research Findings

### Projects Affected
- `GitTransformer/GitTransformer.csproj` — single SDK-style Blazor WebAssembly project; replace `net9.0` with `net10.0` in place.

### Files to Modify
- `GitTransformer/GitTransformer.csproj` — update the target framework and direct package versions.
- `GitTransformer/Pages/Documents.razor` — align upload collection and Radzen dialog parameter nullability.
- `GitTransformer/Pages/Transformer.razor.cs` — align Radzen dialog parameter and return annotations.
- `GitTransformer/Pages/Components/VSCodeJS.razor.cs` — align Radzen dialog parameter annotations.

### Packages to Update
| Package | Current | Target | Notes |
|---------|---------|--------|-------|
| BlazorMonaco | 3.3.0 | 3.5.0 | Latest stable version reported by NuGet. |
| Microsoft.AspNetCore.Components.WebAssembly | 9.0.1 | 10.0.10 | Align with the installed .NET 10 SDK. |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 9.0.1 | 10.0.10 | Align with the .NET 10 WebAssembly tooling; retain `PrivateAssets="all"`. |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | Latest stable patch release. |
| Radzen.Blazor | 5.9.8 | 11.2.2 | Latest stable .NET 10-compatible release reported by NuGet. |

### API Changes / Migration Patterns
- The assessment reports ten behavioral compatibility findings involving keyed-service attributes and URI construction, but no binary or source incompatibilities. Validate these through compilation and available tests; no preemptive code rewrite is indicated.
- Radzen 11 annotates dialog parameter values as nullable and dialog results as nullable dynamic values; callers must use `Dictionary<string, object?>` and propagate `Task<dynamic?>` where appropriate.

### Dependencies & Risks
- Package versions are defined directly in the project file; Central Package Management is not enabled.
- The .NET 10 SDK is installed, no `global.json` constrains SDK selection, and no project references exist.
- No `// STUB:` markers or test projects were found. NuGet reports no known vulnerabilities from the public feed.
- The configured private NPS feed returns HTTP 401 for package audit operations, so version and vulnerability verification uses `https://api.nuget.org/v3/index.json`.

### Decisions Made
- Apply all stable direct-package updates reported by NuGet as part of the atomic upgrade.
- Use `dotnet build` because the project is SDK-style and targets modern .NET without desktop or legacy build requirements.
