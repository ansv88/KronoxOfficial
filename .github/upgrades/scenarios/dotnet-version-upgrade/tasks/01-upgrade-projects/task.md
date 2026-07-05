# 01-upgrade-projects: Upgrade KronoxApi and KronoxFront to .NET 10

Upgrade both projects together in a single atomic pass. This task covers prerequisites (verify the .NET 10 SDK is installed and reconcile any `global.json`), retargeting both `KronoxApi.csproj` and `KronoxFront.csproj` from `net8.0` to `net10.0`, updating NuGet packages, and fixing any breaking API changes surfaced by the build.

Package updates are isolated to **KronoxApi**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, and `Microsoft.EntityFrameworkCore.SqlServer` all move from 8.0.14 → 10.0.9. **KronoxFront** has no package changes. The assessment flagged a small set of source/binary-incompatible APIs to address during the build-fix pass — notably `ConfigurationBinder.Get<T>` / `GetValue<T>`, `OptionsConfigurationServiceCollectionExtensions.Configure<T>`, and `IdentityEntityFrameworkBuilderExtensions.AddEntityFrameworkStores<T>`. The large majority of flagged issues (183 of 207) are low-impact behavioral changes (`HttpContent`, `JsonDocument`, `Uri`) that compile unchanged and are verified at validation.

Research starting points: confirm .NET 10 SDK availability, check for a `global.json` at the repo root, review EF Core 10 / ASP.NET Core Identity 10 breaking changes for the Identity + SqlServer setup, and inventory the `ConfigurationBinder` / `Configure<T>` call sites flagged in the assessment.

## Research Findings

### Prerequisites (verified)
- **.NET 10 SDK**: installed and compatible (`validate_dotnet_sdk_installation` → "Compatible SDK found").
- **global.json**: none in the repo — nothing to reconcile.
- **Central Package Management**: not used — no `Directory.Packages.props`, `Directory.Build.props`, or `nuget.config`. Package versions are declared inline per project (Standard mode).
- **Build tool**: both projects are SDK-style `Microsoft.NET.Sdk.Web` targeting modern .NET with no `.resx`/WPF/COM concerns → use `dotnet build`.

### KronoxApi\KronoxApi.csproj (net8.0 → net10.0)
- SDK: `Microsoft.NET.Sdk.Web`; `Nullable=enable`, `ImplicitUsings=enable`.
- Package actions (verified target versions via `get_supported_package_version` for net10.0):
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.14 → **10.0.9**
  - `Microsoft.EntityFrameworkCore.Design` 8.0.14 → **10.0.9** (keep `PrivateAssets`/`IncludeAssets` metadata)
  - `Microsoft.EntityFrameworkCore.SqlServer` 8.0.14 → **10.0.9**
  - `MailKit` 4.16.0 — compatible, no change
  - `Swashbuckle.AspNetCore` 8.1.2 — compatible, no change
- Assessment API issues to watch during build-fix: `ConfigurationBinder.Get<T>`/`GetValue<T>`, `OptionsConfigurationServiceCollectionExtensions.Configure<T>`, `IdentityEntityFrameworkBuilderExtensions.AddEntityFrameworkStores<T>` (source/binary-incompatible — likely compile clean but verify).

### KronoxFront\KronoxFront.csproj (net8.0 → net10.0)
- SDK: `Microsoft.NET.Sdk.Web` (Blazor); `Nullable=enable`, `ImplicitUsings=enable`.
- **No PackageReference items** — TFM bump only. API issues are behavioral (`HttpContent`, `JsonDocument`, `Uri`) via the ASP.NET Core shared framework; verified at build/runtime.

### Execution approach (All-at-Once)
1. Update both `<TargetFramework>` values to `net10.0`.
2. Bump the three KronoxApi package versions to 10.0.9.
3. `dotnet restore`, then build the full solution and fix all errors/warnings in one bounded pass.

**Done when**: both `.csproj` files target `net10.0`; the three KronoxApi packages are at 10.0.9; `dotnet restore` succeeds; and the full solution builds with **0 errors and 0 warnings**.
