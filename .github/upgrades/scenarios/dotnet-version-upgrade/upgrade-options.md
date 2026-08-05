# Upgrade Options — GitTransformer

Assessment: One SDK-style ASP.NET Core/Blazor WebAssembly project targeting net9.0, with no incompatible packages and low upgrade difficulty.

## Strategy

### Upgrade Strategy
A single modern .NET project with no dependency graph is best upgraded in one atomic pass.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade the project, framework packages, and compatible dependencies together, then validate the complete solution. |
| Top-Down | Upgrade entry-point applications first and temporarily multi-target shared libraries; unnecessary here because there are no shared projects. |
