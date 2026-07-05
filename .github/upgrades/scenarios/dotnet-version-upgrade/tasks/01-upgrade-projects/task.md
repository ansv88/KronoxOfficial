# 01-upgrade-projects: Upgrade KronoxApi and KronoxFront to .NET 10

Upgrade both projects together in a single atomic pass. This task covers prerequisites (verify the .NET 10 SDK is installed and reconcile any `global.json`), retargeting both `KronoxApi.csproj` and `KronoxFront.csproj` from `net8.0` to `net10.0`, updating NuGet packages, and fixing any breaking API changes surfaced by the build.

Package updates are isolated to **KronoxApi**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, and `Microsoft.EntityFrameworkCore.SqlServer` all move from 8.0.14 → 10.0.9. **KronoxFront** has no package changes. The assessment flagged a small set of source/binary-incompatible APIs to address during the build-fix pass — notably `ConfigurationBinder.Get<T>` / `GetValue<T>`, `OptionsConfigurationServiceCollectionExtensions.Configure<T>`, and `IdentityEntityFrameworkBuilderExtensions.AddEntityFrameworkStores<T>`. The large majority of flagged issues (183 of 207) are low-impact behavioral changes (`HttpContent`, `JsonDocument`, `Uri`) that compile unchanged and are verified at validation.

Research starting points: confirm .NET 10 SDK availability, check for a `global.json` at the repo root, review EF Core 10 / ASP.NET Core Identity 10 breaking changes for the Identity + SqlServer setup, and inventory the `ConfigurationBinder` / `Configure<T>` call sites flagged in the assessment.

**Done when**: both `.csproj` files target `net10.0`; the three KronoxApi packages are at 10.0.9; `dotnet restore` succeeds; and the full solution builds with **0 errors and 0 warnings**.
