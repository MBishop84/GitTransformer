# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
  - [Binding Redirect Configuration](#binding-redirect-configuration)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [GitTransformer\GitTransformer.csproj](#gittransformergittransformercsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 1 | All require upgrade |
| Total NuGet Packages | 6 | 3 need upgrade |
| Total Code Files | 11 |  |
| Total Code Files with Incidents | 4 |  |
| Total Lines of Code | 1304 |  |
| Total Number of Issues | 14 |  |
| Estimated LOC to modify | 10+ | at least 0.8% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Binding Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| [GitTransformer\GitTransformer.csproj](#gittransformergittransformercsproj) | net9.0 | 🟢 Low | 3 | 10 | 0 | 10+ | AspNetCore, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 3 | 50.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 3 | 50.0% |
| ***Total NuGet Packages*** | ***6*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 10 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 10508 |  |
| ***Total APIs Analyzed*** | ***10518*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| BlazorMonaco | 3.3.0 |  | [GitTransformer.csproj](#gittransformergittransformercsproj) | ✅Compatible |
| Microsoft.AspNetCore.Components.WebAssembly | 9.0.1 | 10.0.10 | [GitTransformer.csproj](#gittransformergittransformercsproj) | NuGet package upgrade is recommended |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 9.0.1 | 10.0.10 | [GitTransformer.csproj](#gittransformergittransformercsproj) | NuGet package upgrade is recommended |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | [GitTransformer.csproj](#gittransformergittransformercsproj) | NuGet package upgrade is recommended |
| PluralizeService.Core | 1.2.21147.2 |  | [GitTransformer.csproj](#gittransformergittransformercsproj) | ✅Compatible |
| Radzen.Blazor | 5.9.8 |  | [GitTransformer.csproj](#gittransformergittransformercsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |
| M:Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute.#ctor(System.Object) | 4 | 40.0% | Behavioral Change |
| T:System.Uri | 4 | 40.0% | Behavioral Change |
| M:System.Uri.#ctor(System.String) | 2 | 20.0% | Behavioral Change |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;GitTransformer.csproj</b><br/><small>net9.0</small>"]
    click P1 "#gittransformergittransformercsproj"

```

## Project Details

<a id="gittransformergittransformercsproj"></a>
### GitTransformer\GitTransformer.csproj

#### Project Info

- **Current Target Framework:** net9.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** AspNetCore
- **Dependencies**: 0
- **Dependants**: 0
- **Number of Files**: 101
- **Number of Files with Incidents**: 4
- **Lines of Code**: 1304
- **Estimated LOC to modify**: 10+ (at least 0.8% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["GitTransformer.csproj"]
        MAIN["<b>📦&nbsp;GitTransformer.csproj</b><br/><small>net9.0</small>"]
        click MAIN "#gittransformergittransformercsproj"
    end

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 10 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 10508 |  |
| ***Total APIs Analyzed*** | ***10518*** |  |

