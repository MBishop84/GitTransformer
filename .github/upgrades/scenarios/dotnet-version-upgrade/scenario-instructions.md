# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: .NET 10 (`net10.0`)
- **Radzen Components**: Update to the latest stable .NET 10-compatible release

## Source Control
- **Source Branch**: `master`
- **Working Branch**: `upgrade-dotnet-10`
- **Commit Strategy**: Single Commit at End
- **Branch Sync**: Auto (Merge)

## Key Decisions Log
- **Initialization**: Upgrade to .NET 10 LTS rather than .NET 11 Preview, and include the latest stable compatible Radzen component packages.
- **Planning**: Use the All-at-Once strategy for the single-project modern .NET solution.

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Strategy
**Selected**: All-at-Once
**Rationale**: The solution contains one SDK-style modern .NET project with no project dependencies, no incompatible packages, and low assessed upgrade difficulty.

### Execution Constraints
- Upgrade the target framework and packages in one atomic pass.
- Keep framework package versions aligned with .NET 10.
- Update Radzen to the latest stable .NET 10-compatible release.
- Validate the full solution build and tests after all changes.
