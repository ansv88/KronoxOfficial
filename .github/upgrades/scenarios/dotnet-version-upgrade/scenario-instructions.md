# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (.NET 10 LTS)

## Source Control
- **Source Branch**: main
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: Single Commit at End
- **Branch Sync**: Auto (Merge)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Strategy
**Selected**: All-at-Once
**Rationale**: 2 independent SDK-style projects both on .NET 8 — a mechanical target-framework bump where incremental phasing adds overhead without benefit.

### Execution Constraints
- Single atomic upgrade — both projects (KronoxApi, KronoxFront) are updated together; validate the full solution build after the upgrade.
- Update all target frameworks and package references first, then restore, then build-and-fix all compilation errors in one bounded pass (not a repeated retry loop).
- Testing runs only after the atomic upgrade builds with 0 errors and 0 warnings.
- Fix all build warnings in modified projects — never suppress them without explicit user approval.
