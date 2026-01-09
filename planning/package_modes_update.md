# Package Documentation Modes - Implementation Update

## Two Package Modes

Package owners can choose between two modes when configuring their package documentation:

### Mode 1: Remote References (Recommended)
- Package contains **only llms.txt** with URLs to live documentation
- Always up-to-date with latest docs
- Minimal package size (~2KB)
- Requires internet connection

### Mode 2: Local Copy
- Package contains **llms.txt + copied markdown files**
- Works offline without internet
- Slightly larger package size (~5-10KB)
- Can go stale between package versions

---

## Configuration in Manifest

Add `mode` field to `package-docs.json`:

```json
{
  "$schema": "https://learn.microsoft.com/schemas/nuget-package-docs-v1.json",
  "package": "Microsoft.Extensions.Logging",
  "mode": "remote",  // NEW: "remote" or "local"
  "curated_docs": [
    {
      "title": "Logging in .NET Overview",
      "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging.md",
      "sections": ["overview", "getting-started"],
      "priority": 1
    }
  ]
}
```

---

## Mode 1: Remote References (Default)

### Package Structure
```
Microsoft.Extensions.Logging.9.0.0.nupkg
├── lib/net9.0/Microsoft.Extensions.Logging.dll
└── docs/
    └── llms.txt                        # ~2KB, links only
```

### Generated llms.txt
```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications

## Quick Start

- [5-Minute Quick Start](https://learn.microsoft.com/dotnet/core/extensions/logging#quick-start): Set up logging in a console app

## Essential Documentation

Curated by package maintainers - the must-read docs for this package:

1. [Logging in .NET Overview](https://learn.microsoft.com/dotnet/core/extensions/logging): Core concepts and getting started (10 min read)
2. [Logging Providers](https://learn.microsoft.com/dotnet/core/extensions/logging-providers): Built-in providers and configuration (8 min read)
3. [High-Performance Logging](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator): Source generators (7 min read)
4. [Custom Provider Tutorial](https://learn.microsoft.com/dotnet/core/extensions/custom-logging-provider): Implementation guide (15 min read)
5. [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Web application integration

## API Reference

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Primary logging interface
- [Browse all APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Code Samples

- [Basic console logging](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/Basic)
- [Custom provider](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/CustomProvider)

## Related Packages

- [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

## More Resources

- [Full documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Report issues](https://github.com/dotnet/runtime/issues)
```

### Pros
- ✅ Always fresh - docs update independently of package
- ✅ Minimal package size
- ✅ Easy to maintain - no doc sync needed
- ✅ Links to canonical docs (SEO, analytics)

### Cons
- ❌ Requires internet connection
- ❌ Link rot possible if docs move
- ❌ No offline support

### Best For
- Microsoft-owned packages with stable doc URLs
- Packages with frequently updated documentation
- Libraries with cross-cutting concerns (linking to ASP.NET, EF)

---

## Mode 2: Local Copy

### Package Structure
```
Microsoft.Extensions.Logging.9.0.0.nupkg
├── lib/net9.0/Microsoft.Extensions.Logging.dll
└── docs/
    ├── llms.txt                        # ~2KB, relative links
    ├── logging-overview.md             # ~1KB (extracted sections)
    ├── logging-providers.md            # ~1KB
    ├── logging-performance.md          # ~800 bytes
    ├── logging-custom-provider.md      # ~1.5KB
    └── samples/
        ├── logging-basic/
        │   └── Program.cs
        └── logging-custom/
            └── CustomLoggerProvider.cs
```

### Generated llms.txt
```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications

## Quick Start

- [5-Minute Quick Start](quickstart.md): Set up logging in a console app

## Essential Documentation

Curated by package maintainers - the must-read docs for this package:

1. [Logging in .NET Overview](logging-overview.md): Core concepts and getting started (10 min read)
2. [Logging Providers](logging-providers.md): Built-in providers and configuration (8 min read)
3. [High-Performance Logging](logging-performance.md): Source generators (7 min read)
4. [Custom Provider Tutorial](logging-custom-provider.md): Implementation guide (15 min read)
5. [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Web application integration (external)

## API Reference

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Primary logging interface
- [Browse all APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Code Samples

- [Basic console logging](samples/logging-basic/Program.cs): Complete working example
- [Custom provider](samples/logging-custom/CustomLoggerProvider.cs): Build your own provider

## Related Packages

- [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

## More Resources

- [Full documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Report issues](https://github.com/dotnet/runtime/issues)
```

### Example Copied File: logging-overview.md
```markdown
# Logging in .NET

> Extracted from [full documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)

## Overview

Logging provides a way to capture information about the execution of your code. The .NET logging infrastructure enables you to send logging information to various outputs, called providers.

Key features:
- Unified logging API across all .NET workloads
- Built-in providers for console, debug, event log, and more
- High-performance with compile-time source generation
- Structured logging with semantic logging support

## Getting Started

To add logging to your application:

```csharp
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
```

[Read full documentation →](https://learn.microsoft.com/dotnet/core/extensions/logging)
```

### Pros
- ✅ Works offline
- ✅ No link rot concerns
- ✅ Self-contained package
- ✅ Versioned with package (docs match code)

### Cons
- ❌ Larger package size
- ❌ Can go stale (docs may be outdated)
- ❌ More complex build process
- ❌ Duplicate content (same docs in multiple packages)

### Best For
- Third-party packages with unstable doc hosting
- Enterprise scenarios requiring offline access
- Packages with version-specific documentation
- Libraries used in air-gapped environments

---

## Build-Time Behavior

### Tool: `dotnet-docs-generator`

```bash
dotnet docs-generator generate \
  --manifest docs/package-docs.json \
  --mode remote \                    # or "local"
  --output $(IntermediateOutputPath)docs/
```

### Mode: Remote

**Process**:
1. Read `package-docs.json`
2. Validate all URLs are accessible
3. Generate `llms.txt` with full URLs
4. Include only `llms.txt` in package (no content download)

**Output**: Single `llms.txt` file (~2KB)

### Mode: Local

**Process**:
1. Read `package-docs.json`
2. Download markdown from URLs
3. Extract specified sections (if `sections` array provided)
4. Save as local files with sanitized names
5. Download sample code (if specified)
6. Generate `llms.txt` with relative paths
7. Bundle all files in package

**Output**: `llms.txt` + markdown files + samples (~5-10KB)

### Section Extraction Example

**Original**: `https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging.md` (5000 words)

**Manifest**:
```json
{
  "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging.md",
  "sections": ["overview", "getting-started"]
}
```

**Extracted**: `logging-overview.md` (~800 words)
- Only "overview" and "getting-started" sections
- Includes code blocks and images from those sections
- Adds footer link to full documentation

---

## Manifest Schema Update

```json
{
  "$schema": "https://learn.microsoft.com/schemas/nuget-package-docs-v1.json",
  "package": "string (required) - NuGet package ID",
  
  "mode": "string (required) - 'remote' or 'local'",
  
  "curated_docs": [
    {
      "title": "string (required) - Display title",
      "source": "string (optional) - URL to markdown doc (for local mode)",
      "url": "string (optional) - Direct URL (for remote mode or external links)",
      "sections": ["array (optional) - Section IDs to extract (local mode only)"],
      "priority": "number (required) - Display order (1-10)",
      "external": "boolean (optional) - Always keep as URL, even in local mode"
    }
  ],
  
  "quick_start": {
    "title": "string (optional)",
    "source": "string (optional) - URL to quick start doc",
    "sections": ["array (optional)"]
  },
  
  "samples": [
    "string (optional) - URLs to sample code (copied in local mode)"
  ],
  
  "api_reference": "string (optional) - URL to API browser (always remote)",
  "related_packages": ["array (optional) - Related package IDs"],
  
  "metadata": {
    "maintainer": "string (optional)",
    "last_updated": "string (optional)",
    "min_package_version": "string (optional)"
  }
}
```

### Key Schema Changes

**New `mode` field**: 
- `"remote"`: Generate llms.txt with URLs only
- `"local"`: Download and bundle docs

**`external` flag on docs**:
- If `true`, always keep as URL even in local mode
- Example: External docs like ASP.NET Core that shouldn't be copied

---

## Package Owner Workflow

### Choose Mode

**Remote Mode** (Recommended for most):
```json
{
  "package": "Microsoft.Extensions.Logging",
  "mode": "remote",
  "curated_docs": [
    {
      "title": "Logging in .NET",
      "url": "https://learn.microsoft.com/dotnet/core/extensions/logging"
    }
  ]
}
```

**Local Mode** (For offline scenarios):
```json
{
  "package": "Microsoft.Extensions.Logging",
  "mode": "local",
  "curated_docs": [
    {
      "title": "Logging in .NET",
      "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging.md",
      "sections": ["overview", "getting-started"]
    },
    {
      "title": "ASP.NET Core Logging",
      "url": "https://learn.microsoft.com/aspnet/core/fundamentals/logging",
      "external": true  // Don't copy, keep as URL
    }
  ]
}
```

### Build Integration

```xml
<!-- In package .csproj -->
<PropertyGroup>
  <!-- Choose mode: Remote or Local -->
  <PackageDocsMode>Remote</PackageDocsMode>
</PropertyGroup>

<Target Name="GeneratePackageDocs" BeforeTargets="Pack">
  <Exec Command="dotnet docs-generator generate --manifest docs/package-docs.json --mode $(PackageDocsMode) --output $(IntermediateOutputPath)docs/" />
</Target>

<ItemGroup>
  <None Include="$(IntermediateOutputPath)docs/**" Pack="true" PackagePath="docs/" />
</ItemGroup>
```

---

## Recommendations by Scenario

### Use Remote Mode When:
- ✅ Microsoft-owned packages (stable URLs)
- ✅ Documentation updates frequently
- ✅ Package has external dependencies (ASP.NET Core, EF Core)
- ✅ Want to minimize package size
- ✅ Target audience has internet access

### Use Local Mode When:
- ✅ Third-party package with unstable doc hosting
- ✅ Enterprise/air-gapped environments
- ✅ Documentation is version-specific (breaking changes)
- ✅ Want guaranteed offline access
- ✅ Package is distributed via internal feeds

### Hybrid Approach:
Many packages will use both:
- Core docs: Local copy
- External resources: Remote URLs
- API reference: Remote URL (always)

**Example**:
```json
{
  "mode": "local",
  "curated_docs": [
    {
      "title": "Core Concepts",
      "source": "...",  // Downloaded locally
      "sections": ["overview"]
    },
    {
      "title": "ASP.NET Integration",
      "url": "...",  // Remote link
      "external": true
    }
  ]
}
```

---

## Migration Path

### Phase 1: All Remote (Recommended Start)
- Launch with remote mode only
- Simpler to implement
- Get feedback on URLs and structure
- **Duration**: Weeks 5-10

### Phase 2: Add Local Mode
- Implement download and extraction
- Add section extraction logic
- Test with pilot packages
- **Duration**: Weeks 11-14

### Phase 3: Package Owner Choice
- Document both modes
- Let package owners choose based on needs
- Provide guidance for decision
- **Duration**: Week 15+

---

## Implementation Updates

### Updated Tool Features

**`dotnet-docs-generator` commands**:

```bash
# Generate remote-only llms.txt
dotnet docs-generator generate \
  --manifest package-docs.json \
  --mode remote

# Generate local copy with extracted docs
dotnet docs-generator generate \
  --manifest package-docs.json \
  --mode local \
  --extract-sections

# Validate manifest without generating
dotnet docs-generator validate \
  --manifest package-docs.json

# Convert remote to local or vice versa
dotnet docs-generator convert \
  --manifest package-docs.json \
  --from remote \
  --to local
```

### CI/CD Validation Updates

```yaml
# GitHub Actions check
- name: Validate package docs
  run: |
    # Check mode is valid
    mode=$(jq -r '.mode' docs/package-docs.json)
    if [[ "$mode" != "remote" && "$mode" != "local" ]]; then
      echo "ERROR: mode must be 'remote' or 'local'"
      exit 1
    fi
    
    # Validate schema
    dotnet docs-generator validate --manifest docs/package-docs.json
    
    # If remote: check all URLs are accessible
    if [[ "$mode" == "remote" ]]; then
      dotnet docs-generator check-links --manifest docs/package-docs.json
    fi
```

---

## Success Metrics Update

### Package Size Metrics

**Remote Mode**:
- [ ] Average package size increase: <5KB
- [ ] llms.txt size: <2KB per package

**Local Mode**:
- [ ] Average package size increase: <15KB
- [ ] Total bundled docs: <10KB per package
- [ ] Extraction successful: 95%+ of attempts

### Mode Adoption

- [ ] 80%+ of Microsoft packages use remote mode
- [ ] 100% of packages in air-gapped scenarios use local mode
- [ ] Clear documentation on when to choose each mode

