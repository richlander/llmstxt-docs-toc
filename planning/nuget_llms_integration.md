# NuGet Package Documentation Integration with llms.txt

## The Problem

**Scenario**: Developer installs `Microsoft.Extensions.Logging` NuGet package
- Package brings in code/assemblies
- But where's the relevant documentation?
- Currently: scattered across docs, hard to discover
- LLMs need to know: "What docs are relevant for this package?"

## Core Concept

**NuGet packages "subscribe" to documentation** - declaring which docs are relevant to that package.

When an LLM sees a package reference, it can discover the associated documentation automatically.

---

## Option 1: Package Metadata + Generated llms.txt

### Package-Level Metadata File

Each NuGet package declares its relevant docs in its repository:

**File: `eng/docs-manifest.json` (in package source repo)**
```json
{
  "package": "Microsoft.Extensions.Logging",
  "version": "9.0.0",
  "docs_subscription": {
    "primary_guides": [
      "fundamentals/logging",
      "core/extensions/logging"
    ],
    "related_packages": [
      "Microsoft.Extensions.Logging.Console",
      "Microsoft.Extensions.Logging.Debug"
    ],
    "external_docs": [
      "https://learn.microsoft.com/dotnet/core/extensions/logging"
    ],
    "samples": [
      "fundamentals/logging/snippets"
    ],
    "api_docs": [
      "api/microsoft.extensions.logging"
    ]
  }
}
```

### Build-Time Generation

During package build, generate `llms.txt` and include in package:

**Generated: `{package}/llms.txt` (embedded in .nupkg)**
```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications

## Getting Started

- [Logging in .NET](https://learn.microsoft.com/dotnet/core/extensions/logging): Overview and concepts
- [Logging providers](https://learn.microsoft.com/dotnet/core/extensions/logging-providers): Built-in and third-party providers
- [High-performance logging](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator): Compile-time source generation

## Quick Start

```csharp
// Add to Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();

// Use in your code
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoWork()
    {
        _logger.LogInformation("Work started");
    }
}
```

## Key Concepts

- [Log levels](https://learn.microsoft.com/dotnet/core/extensions/logging#log-level): Trace, Debug, Information, Warning, Error, Critical
- [Log categories](https://learn.microsoft.com/dotnet/core/extensions/logging#log-category): Organize logs by namespace
- [Log scopes](https://learn.microsoft.com/dotnet/core/extensions/logging#log-scopes): Add context to related logs
- [Structured logging](https://learn.microsoft.com/dotnet/core/extensions/logging#structured-logging): Log with typed parameters

## Configuration

- [Configure logging](https://learn.microsoft.com/dotnet/core/extensions/logging#configure-logging): appsettings.json setup
- [Logging filters](https://learn.microsoft.com/dotnet/core/extensions/logging#logging-filters): Control what gets logged
- [Custom providers](https://learn.microsoft.com/dotnet/core/extensions/custom-logging-provider): Implement your own

## Related Packages

- [Microsoft.Extensions.Logging.Console](nuget:Microsoft.Extensions.Logging.Console): Console output
- [Microsoft.Extensions.Logging.Debug](nuget:Microsoft.Extensions.Logging.Debug): Debug window output
- [Microsoft.Extensions.Logging.EventLog](nuget:Microsoft.Extensions.Logging.EventLog): Windows Event Log
- [Microsoft.Extensions.Logging.EventSource](nuget:Microsoft.Extensions.Logging.EventSource): EventSource/ETW

## API Reference

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Main logging interface
- [LoggerExtensions](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.loggerextensions): Convenience methods
- [ILoggerFactory](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggerfactory): Create loggers

## Samples

- [Basic logging](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/Basic): Simple console app
- [Custom provider](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/CustomProvider): Build your own
- [ASP.NET Core integration](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Web app logging
```

### How It Works

1. **Package build time**: 
   - Read `docs-manifest.json`
   - Fetch referenced docs from main repo
   - Generate consolidated `llms.txt`
   - Include in `.nupkg` as `docs/llms.txt`

2. **Developer installs package**:
   - NuGet restores package
   - `llms.txt` available in package cache
   - IDE/LLM can discover: `~/.nuget/packages/microsoft.extensions.logging/9.0.0/docs/llms.txt`

3. **LLM queries**:
   - User asks: "How do I use ILogger?"
   - LLM detects package reference in project file
   - Reads package's `llms.txt`
   - Gets curated, package-specific docs

---

## Option 2: Central Registry + Fragment Assembly

### Central Package-Docs Registry

**File: `docs/packages/registry.json`**
```json
{
  "packages": {
    "Microsoft.Extensions.Logging": {
      "doc_fragments": [
        "fundamentals/logging/overview",
        "fundamentals/logging/providers",
        "fundamentals/logging/configuration",
        "core/extensions/logging-best-practices"
      ],
      "api_namespace": "Microsoft.Extensions.Logging",
      "samples": [
        "core/extensions/logging/snippets"
      ],
      "related": [
        "Microsoft.Extensions.Logging.Console",
        "Microsoft.Extensions.DependencyInjection"
      ]
    },
    "System.Text.Json": {
      "doc_fragments": [
        "standard/serialization/system-text-json/overview",
        "standard/serialization/system-text-json/how-to",
        "standard/serialization/system-text-json/converters"
      ],
      "api_namespace": "System.Text.Json",
      "samples": [
        "standard/serialization/system-text-json/snippets"
      ]
    },
    "Microsoft.EntityFrameworkCore": {
      "doc_fragments": [],
      "external_docs": "https://learn.microsoft.com/ef/core/llms.txt",
      "note": "EF Core docs live in separate repo"
    }
  }
}
```

### Build-Time Fragment Assembly

Script that generates per-package llms.txt files:

**Generated: `docs/packages/microsoft.extensions.logging/llms.txt`**
```markdown
# Microsoft.Extensions.Logging Package Documentation

> Assembled from .NET documentation for package: Microsoft.Extensions.Logging v9.0

## Overview

[Content extracted from fundamentals/logging/overview.md first paragraphs]

Logging in .NET provides a flexible, high-performance logging framework that works across all application types...

## Quick Start

[Content extracted from fundamentals/logging/quickstart section]

## Core Concepts

### ILogger Interface
[Extracted from core/extensions/logging-best-practices.md]

### Log Levels
[Extracted from fundamentals/logging/configuration.md]

## Configuration

[Content from fundamentals/logging/configuration.md]

## Related Documentation

- [Full logging guide](../../fundamentals/logging/overview.md)
- [Dependency injection](../../core/extensions/dependency-injection.md)
- [Configuration system](../../core/extensions/configuration.md)

## API Reference

- [ILogger API](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1)
- [Browse all APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)
```

### Directory Structure

```
docs/
├── llms.txt                                    # Root docs navigation
├── llms-fundamentals.txt                       # Topic files
├── llms-web.txt
├── ...
└── packages/                                   # NEW: Package-specific docs
    ├── registry.json                          # Central registry
    ├── microsoft.extensions.logging/
    │   └── llms.txt                           # Generated per-package
    ├── system.text.json/
    │   └── llms.txt
    ├── microsoft.aspnetcore.mvc/
    │   └── llms.txt
    └── ...
```

---

## Option 3: Package Tags in Existing Docs

### Tag Documents with Package Metadata

**In existing markdown files**, add package metadata:

**File: `docs/fundamentals/logging/overview.md`**
```markdown
---
title: Logging in .NET
description: Learn about the logging framework in .NET
ms.date: 01/08/2026
packages:
  - Microsoft.Extensions.Logging
  - Microsoft.Extensions.Logging.Abstractions
  - Microsoft.Extensions.Logging.Console
related-packages:
  - Microsoft.Extensions.DependencyInjection
  - Microsoft.Extensions.Configuration
---

# Logging in .NET

[Content...]
```

### Build Process Aggregates

During doc build:
1. Scan all markdown files
2. Extract `packages` metadata
3. Group docs by package
4. Generate `docs/packages/{package-name}/llms.txt`

### Generated Output

**`docs/packages/microsoft.extensions.logging/llms.txt`**
```markdown
# Microsoft.Extensions.Logging

> Documentation aggregated from 12 articles tagged with this package

## Overview Articles (3)
- [Logging in .NET](../../fundamentals/logging/overview.md): Core concepts and architecture
- [Logging providers](../../fundamentals/logging/providers.md): Built-in and third-party providers
- [High-performance logging](../../core/extensions/logger-message-generator.md): Source generation

## How-To Guides (5)
- [Configure logging](../../fundamentals/logging/configuration.md): Setup in appsettings.json
- [Create custom logger](../../fundamentals/logging/custom-provider.md): Build your own provider
- [Use logging in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Web apps
- [Log to Azure Application Insights](../../azure/logging-appinsights.md): Cloud logging
- [Implement log scopes](../../fundamentals/logging/scopes.md): Add context

## Tutorials (2)
- [Logging quickstart](../../fundamentals/logging/quickstart.md): 5-minute tutorial
- [Custom provider tutorial](../../fundamentals/logging/tutorial-custom.md): Step-by-step

## API Reference
- [Browse APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Related Packages
- [Microsoft.Extensions.Logging.Console](../microsoft.extensions.logging.console/llms.txt)
- [Microsoft.Extensions.DependencyInjection](../microsoft.extensions.dependencyinjection/llms.txt)
```

---

## Option 4: NuGet Package Includes llms.txt URL

### Package Metadata Points to Docs

**In `.nuspec` file**:
```xml
<package>
  <metadata>
    <id>Microsoft.Extensions.Logging</id>
    <version>9.0.0</version>
    <projectUrl>https://github.com/dotnet/runtime</projectUrl>
    <documentationUrl>https://learn.microsoft.com/dotnet/core/extensions/logging</documentationUrl>
    
    <!-- NEW: LLM-specific documentation -->
    <llmsUrl>https://learn.microsoft.com/dotnet/packages/microsoft.extensions.logging/llms.txt</llmsUrl>
    
    <!-- OR: Include in package -->
    <files>
      <file src="docs\llms.txt" target="docs\llms.txt" />
    </files>
  </metadata>
</package>
```

### LLM Discovery Flow

1. **LLM sees project file**:
   ```xml
   <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
   ```

2. **LLM queries package metadata**:
   - Check package cache for `llms.txt`
   - OR fetch from `<llmsUrl>`
   - OR fallback to `<documentationUrl>`

3. **Get package-specific guidance**:
   - Read curated docs for THIS package
   - Not generic "all of .NET docs"

---

## Comparison Matrix

| Option | Maintenance | Discoverability | Accuracy | Implementation |
|--------|-------------|-----------------|----------|----------------|
| **1. Package metadata + generated** | Low (auto-gen) | High (in package) | High (curated) | Medium (build changes) |
| **2. Central registry + assembly** | Medium (manual) | Medium (web URL) | High (curated) | Medium (build script) |
| **3. Package tags in docs** | Low (in frontmatter) | Medium (web URL) | Medium (auto) | Easy (metadata only) |
| **4. NuGet metadata URL** | Low (per-package) | High (standard field) | Depends | Easy (nuspec change) |

---

## Recommended Approach: Hybrid (Option 1 + Option 3)

### Phase 1: Tag Existing Docs
```markdown
---
packages:
  - Microsoft.Extensions.Logging
---
```

Add package tags to existing documentation (low effort, high value).

### Phase 2: Generate Package llms.txt
Build process:
1. Scan docs for package tags
2. Generate `docs/packages/{package}/llms.txt` (≤50 lines per package)
3. Publish to web

### Phase 3: Include in NuGet Package
Update package build:
```xml
<files>
  <file src="https://learn.microsoft.com/dotnet/packages/microsoft.extensions.logging/llms.txt" 
        target="docs/llms.txt" />
</files>
```

Or add URL to `.nuspec`:
```xml
<llmsUrl>https://learn.microsoft.com/dotnet/packages/microsoft.extensions.logging/llms.txt</llmsUrl>
```

---

## Example: Microsoft.Extensions.Logging Package llms.txt

**Generated: `docs/packages/microsoft.extensions.logging/llms.txt` (48 lines)**

```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications. Install: `dotnet add package Microsoft.Extensions.Logging`

## Essential Documentation

- [Logging in .NET overview](../../core/extensions/logging.md): Core concepts and architecture
- [Logging providers](../../core/extensions/logging-providers.md): Console, Debug, EventLog, and third-party
- [High-performance logging](../../core/extensions/logger-message-generator.md): Source generators for zero-allocation logging
- [Configure logging](../../core/extensions/logging.md#configure-logging): appsettings.json configuration

## Quick Start

```csharp
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
```

- [Quick start tutorial](../../core/extensions/logging.md#quick-start): 5-minute tutorial

## Key APIs

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Primary logging interface
- [ILoggerFactory](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggerfactory): Create logger instances
- [LogLevel enum](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel): Trace, Debug, Information, Warning, Error, Critical

## Common Scenarios

- [Log filtering](../../core/extensions/logging.md#log-filtering): Control what gets logged
- [Log scopes](../../core/extensions/logging.md#log-scopes): Add context to related logs
- [Custom logging provider](../../core/extensions/custom-logging-provider.md): Implement your own provider
- [ASP.NET Core integration](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Built-in web app logging

## Related Packages

- [Microsoft.Extensions.Logging.Console](../microsoft.extensions.logging.console/llms.txt): Console output provider
- [Microsoft.Extensions.Logging.Debug](../microsoft.extensions.logging.debug/llms.txt): Debug window output
- [Microsoft.Extensions.Logging.EventLog](../microsoft.extensions.logging.eventlog/llms.txt): Windows Event Log
- [Microsoft.Extensions.DependencyInjection](../microsoft.extensions.dependencyinjection/llms.txt): Required for DI integration

## Samples

- [Basic logging sample](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/Basic)
- [Custom provider sample](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/CustomProvider)
- [ASP.NET Core sample](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/AspNetCore)

## Package Information

- **NuGet**: https://www.nuget.org/packages/Microsoft.Extensions.Logging
- **Source**: https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Logging
- **Version**: 9.0.0+ (.NET 9)
- **License**: MIT
```

---

## Implementation Checklist

### Step 1: Document Tagging (2 weeks)
- [ ] Add `packages:` frontmatter to ~500 key docs
- [ ] Focus on high-traffic packages first
- [ ] Validate tags with package owners

### Step 2: Build Pipeline (1 week)
- [ ] Create script to extract package tags
- [ ] Generate per-package llms.txt files
- [ ] Enforce 50-line limit per package
- [ ] Integrate into docs build

### Step 3: Publishing (1 week)
- [ ] Publish to `learn.microsoft.com/dotnet/packages/{package}/llms.txt`
- [ ] Add robots.txt rules
- [ ] Set up CDN/caching

### Step 4: NuGet Integration (2 weeks)
- [ ] Add `<llmsUrl>` to .nuspec schema
- [ ] Update package build to include llms.txt
- [ ] Test with popular packages
- [ ] Document for package authors

### Step 5: Validation (ongoing)
- [ ] Test with LLM tools
- [ ] Gather feedback from community
- [ ] Measure adoption
- [ ] Iterate based on usage

---

## Open Questions

1. **Versioning**: Should llms.txt be version-specific or always latest?
   - Option: `packages/microsoft.extensions.logging/9.0/llms.txt`
   - Or: `packages/microsoft.extensions.logging/llms.txt` (latest)

2. **Maintenance**: Who owns package documentation?
   - Package owner? Docs team? Auto-generated?

3. **External packages**: How do third-party packages participate?
   - Need public schema/API
   - Allow packages to self-register

4. **Discovery**: How do LLMs find package llms.txt?
   - Standard NuGet metadata field
   - Convention: always at `packages/{package-id}/llms.txt`

5. **Size limits**: 50 lines enough for complex packages?
   - Could allow up to 75 for very complex packages
   - Or split into sub-topics if needed
