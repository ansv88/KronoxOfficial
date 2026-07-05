# Task 01 — Upgrade KronoxApi and KronoxFront to .NET 10 — Progress Details

## Summary
Both projects in `Kronox.sln` were retargeted from `net8.0` to `net10.0` in a single All-at-Once pass. KronoxApi's EF Core / Identity packages were bumped to 10.0.9. All pre-existing nullable-reference warnings surfaced by the new SDK were fixed (no suppressions). The full solution builds clean: **0 errors, 0 warnings**.

## Changes Made

### Target Framework
- `KronoxApi/KronoxApi.csproj`: `<TargetFramework>net8.0` → `net10.0`
- `KronoxFront/KronoxFront.csproj`: `<TargetFramework>net8.0` → `net10.0`

### Package Updates (KronoxApi only)
| Package | From | To |
|---|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.14 | 10.0.9 |
| Microsoft.EntityFrameworkCore.Design | 8.0.14 | 10.0.9 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.14 | 10.0.9 |

- `MailKit` (4.16.0) and `Swashbuckle.AspNetCore` (8.1.2) left unchanged — already compatible.
- KronoxFront has no PackageReference items — TFM bump only.

### Warning Fixes (15 pre-existing nullable warnings, all resolved)
- `KronoxApi/Services/MailKitEmailService.cs` — validated `EmailSettings` config values (FromEmail, SmtpServer, SmtpPort, UseSsl) with explicit null-checks/throws before use (CS8604 ×4).
- `KronoxApi/Controllers/DocumentController.cs` — applied null-forgiving operator on the required `MainCategory` navigation inside three EF `Select` projections (CS8602 ×3; false-positives — translated to SQL, never dereferenced in-memory).
- `KronoxFront/Middleware/PageAuthorizationMiddleware.cs` — use `context.User.IsInRole(...)` instead of the flow-narrowed `user` local (CS8602 ×1).
- `KronoxFront/Program.cs` — `context.User.Identity?.IsAuthenticated == true` in default policy; null-coalesce `path.Value` before `TrimStart` (CS8602 ×2).
- `KronoxFront/Components/Pages/ContentPage.razor` — null-conditional `pageContent?.Images` in `GetHeroBannerUrl`/`GetHeroBannerAlt` (CS8602 ×2).
- `KronoxFront/Services/CmsService.cs` — null-forgiving on `JsonSerializer.Deserialize<object>(...)` result assigned to dictionary (CS8601 ×1).
- `KronoxFront/Components/Admin/NewsAdmin.razor` — null-forgiving on `editingNews!` in the "existing news" branches of `AttachDocument`/`DetachDocument` (CS8602 ×2).

## Validation
- `dotnet build Kronox.sln --no-incremental`: **Build succeeded — 0 Warning(s), 0 Error(s)**.
- Output assemblies confirmed under `bin\Debug\net10.0\`.

## Notes
- Behavioral-change APIs flagged by the assessment (`HttpContent`, `JsonDocument`, `Uri`) compile unchanged; runtime behavior to be exercised in task 02 (final validation).
- No `global.json`, no Central Package Management — no reconciliation needed.
