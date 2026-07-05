# Upgrade Options — Kronox

Assessment: 2 projects (KronoxApi, KronoxFront) on net8.0 → net10.0; both SDK-style ASP.NET Core, independent (no inter-project dependencies), low difficulty.

## Strategy

### Upgrade Strategy
Both projects are already on modern .NET (net8.0), SDK-style, and have no dependencies on each other — a small, mechanical target-framework bump where an incremental approach would add overhead without benefit.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade both projects together in a single atomic pass. Fastest approach, no multi-targeting overhead. |
| Top-Down | Upgrade entry-point applications first, temporarily multi-targeting shared libraries so the solution stays buildable throughout. Adds overhead not needed at this scale. |
