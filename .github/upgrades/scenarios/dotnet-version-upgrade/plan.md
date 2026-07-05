# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade all projects in `Kronox.sln` from .NET 8 to .NET 10 (LTS).
**Scope**: 2 SDK-style ASP.NET Core projects — KronoxApi (~37.5k LOC) and KronoxFront (Blazor, ~5.4k LOC). The projects are independent (no inter-project references). Low difficulty overall; no incompatible packages and no security vulnerabilities detected.

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 2 projects, both on .NET 8, SDK-style, and independent — a mechanical target-framework bump where incremental phasing would add overhead without benefit.

## Tasks

### 01-upgrade-projects: Upgrade KronoxApi and KronoxFront to .NET 10

Upgrade both projects together in a single atomic pass. This task covers prerequisites (verify the .NET 10 SDK is installed and reconcile any `global.json`), retargeting both `KronoxApi.csproj` and `KronoxFront.csproj` from `net8.0` to `net10.0`, updating NuGet packages, and fixing any breaking API changes surfaced by the build.

Package updates are isolated to **KronoxApi**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, and `Microsoft.EntityFrameworkCore.SqlServer` all move from 8.0.14 → 10.0.9. **KronoxFront** has no package changes. The assessment flagged a small set of source/binary-incompatible APIs to address during the build-fix pass — notably `ConfigurationBinder.Get<T>` / `GetValue<T>`, `OptionsConfigurationServiceCollectionExtensions.Configure<T>`, and `IdentityEntityFrameworkBuilderExtensions.AddEntityFrameworkStores<T>`. The large majority of flagged issues (183 of 207) are low-impact behavioral changes (`HttpContent`, `JsonDocument`, `Uri`) that compile unchanged and are verified at validation.

Research starting points: confirm .NET 10 SDK availability, check for a `global.json` at the repo root, review EF Core 10 / ASP.NET Core Identity 10 breaking changes for the Identity + SqlServer setup, and inventory the `ConfigurationBinder` / `Configure<T>` call sites flagged in the assessment.

**Done when**: both `.csproj` files target `net10.0`; the three KronoxApi packages are at 10.0.9; `dotnet restore` succeeds; and the full solution builds with **0 errors and 0 warnings**.

---

### 02-final-validation: Validate the upgraded solution

Verify the upgraded solution end-to-end. Perform a clean full-solution build, run any existing test suite, and review the behavioral-change APIs the assessment flagged so nothing regresses at runtime.

The assessment identified 183 behavioral changes concentrated in `System.Net.Http.HttpContent`, `System.Text.Json.JsonDocument`, and `System.Uri`. These require no code changes to compile but should be exercised (e.g., API request/response handling in KronoxApi, `HttpClient` calls and JSON handling in KronoxFront) to confirm behavior is unchanged on .NET 10. Record any recommendation that is intentionally deferred.

**Done when**: the solution builds clean (0 errors, 0 warnings); all discovered tests pass (or it is confirmed there are no test projects); and any deferred behavioral-change notes are recorded in this task's progress.
