# Curated Package Documentation Files

## The Vision

**Instead of auto-generating from tags**, package owners manually curate a small set of essential docs that ship with or link from their package.

Think: "If someone installs my package, these are the 5-6 docs they MUST read."

---

## Approach: Package Subscribes to Specific Docs

### Package Owner Creates Manifest

**File: `src/libraries/Microsoft.Extensions.Logging/docs/package-docs.json`**
```json
{
  "package": "Microsoft.Extensions.Logging",
  "curated_docs": [
    {
      "title": "Logging in .NET Overview",
      "source": "docs/core/extensions/logging.md",
      "sections": ["overview", "getting-started"],
      "priority": 1
    },
    {
      "title": "Logging Providers",
      "source": "docs/core/extensions/logging-providers.md",
      "sections": ["built-in-providers", "configuration"],
      "priority": 2
    },
    {
      "title": "High-Performance Logging",
      "source": "docs/core/extensions/logger-message-generator.md",
      "sections": ["source-generation", "benchmarks"],
      "priority": 3
    },
    {
      "title": "Configure Logging",
      "source": "docs/core/extensions/logging.md",
      "sections": ["configuration", "filtering"],
      "priority": 4
    },
    {
      "title": "Custom Logging Provider",
      "source": "docs/core/extensions/custom-logging-provider.md",
      "sections": ["implementation", "registration"],
      "priority": 5
    },
    {
      "title": "ASP.NET Core Logging",
      "url": "https://learn.microsoft.com/aspnet/core/fundamentals/logging",
      "priority": 6,
      "external": true
    }
  ],
  "quick_start": {
    "title": "5-Minute Quick Start",
    "content": "docs/core/extensions/logging-quickstart.md"
  },
  "api_reference": "https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging",
  "samples": [
    "samples/core/extensions/Logging/Basic",
    "samples/core/extensions/Logging/CustomProvider"
  ]
}
```

### What This Enables

1. **Explicit Curation**: Package owner says "these exact docs, in this order"
2. **Section Extraction**: Pull specific sections from larger docs (not full 5000-word articles)
3. **Priority Ordering**: Most important docs first
4. **External Links**: Include docs from other repos (ASP.NET Core, EF Core)
5. **Bundled Content**: Can extract and bundle into package OR link to web

---

## Build Process: Assemble Package Documentation

### Step 1: Extract Doc Sections

Script reads `package-docs.json` and extracts specified sections:

**Input: `docs/core/extensions/logging.md`** (5000 words)
**Sections requested**: `["overview", "getting-started"]`
**Output**: ~500 words extracted

### Step 2: Generate Consolidated llms.txt

**Generated: `Microsoft.Extensions.Logging/docs/llms.txt` (45 lines)**

```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications

## Quick Start

- [5-Minute Quick Start](quickstart.md): Get started with logging immediately

## Essential Documentation (Curated)

1. [Logging in .NET Overview](logging-overview.md): Core concepts and architecture (5 min read)
2. [Logging Providers](logging-providers.md): Built-in providers and configuration (8 min read)
3. [High-Performance Logging](logging-performance.md): Source generators for zero-allocation logging (10 min read)
4. [Configure Logging](logging-configuration.md): appsettings.json and filtering (7 min read)
5. [Custom Logging Provider](logging-custom-provider.md): Implement your own provider (15 min read)
6. [ASP.NET Core Integration](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Web application logging

## API Reference

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Primary logging interface
- [ILoggerFactory](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggerfactory): Create logger instances
- [Browse all APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Code Samples

- [Basic logging sample](../../samples/logging-basic/Program.cs): Simple console app
- [Custom provider sample](../../samples/logging-custom/CustomLoggerProvider.cs): Build your own

## Related Packages

- [Microsoft.Extensions.Logging.Console](../Microsoft.Extensions.Logging.Console/llms.txt)
- [Microsoft.Extensions.Logging.Debug](../Microsoft.Extensions.Logging.Debug/llms.txt)
- [Microsoft.Extensions.DependencyInjection](../Microsoft.Extensions.DependencyInjection/llms.txt)

## Package Information

- **Install**: `dotnet add package Microsoft.Extensions.Logging`
- **NuGet**: https://www.nuget.org/packages/Microsoft.Extensions.Logging
- **Source**: https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Logging
- **Version**: 9.0.0+
```

### Step 3: Bundle Docs with Package

Two options:

#### Option A: Copy Full Extracted Docs into Package
```
Microsoft.Extensions.Logging.9.0.0.nupkg
├── lib/
│   └── net9.0/
│       └── Microsoft.Extensions.Logging.dll
└── docs/
    ├── llms.txt                        # 45 lines - navigation
    ├── quickstart.md                   # Extracted from source
    ├── logging-overview.md             # Extracted: ~500 words
    ├── logging-providers.md            # Extracted: ~800 words
    ├── logging-performance.md          # Extracted: ~600 words
    ├── logging-configuration.md        # Extracted: ~700 words
    ├── logging-custom-provider.md      # Extracted: ~1200 words
    └── samples/
        ├── logging-basic/
        │   └── Program.cs
        └── logging-custom/
            └── CustomLoggerProvider.cs
```

**Size**: ~5-10KB of markdown + samples
**Benefit**: Works offline, no network required
**Downside**: Larger package, can go stale

#### Option B: llms.txt with Links Only
```
Microsoft.Extensions.Logging.9.0.0.nupkg
├── lib/
│   └── net9.0/
│       └── Microsoft.Extensions.Logging.dll
└── docs/
    └── llms.txt                        # Links to web docs
```

**Size**: ~2KB
**Benefit**: Always fresh, small package
**Downside**: Requires network

---

## Package Owner Workflow

### 1. Package Owner Curates Docs

In package source repo, create `docs/package-docs.json`:

```json
{
  "package": "MyAwesome.Package",
  "curated_docs": [
    {
      "title": "Getting Started",
      "source": "docs/fundamentals/mypackage-overview.md",
      "sections": ["introduction", "installation"]
    },
    {
      "title": "Common Scenarios",
      "source": "docs/fundamentals/mypackage-scenarios.md",
      "sections": ["scenario-1", "scenario-2"]
    },
    {
      "title": "Advanced Topics",
      "source": "docs/advanced/mypackage-advanced.md"
    }
  ]
}
```

### 2. Build Process Assembles Docs

When building package:

```bash
# In package build script
dotnet pack --include-docs

# Behind the scenes:
# 1. Read package-docs.json
# 2. Fetch referenced docs from main docs repo
# 3. Extract specified sections
# 4. Generate llms.txt
# 5. Bundle into package or publish to web
```

### 3. Result: Package Contains Curated Docs

Developer installs package, gets exactly the docs the package owner chose.

---

## Directory Structure

### In Main Docs Repo

```
docs/
├── llms.txt                                    # Root (45 lines)
├── llms-web.txt                                # Topics (50 lines each)
├── llms-fundamentals.txt
├── ...
├── packages/                                   # Package documentation area
│   ├── microsoft.extensions.logging/
│   │   ├── llms.txt                           # Generated from package-docs.json
│   │   ├── logging-overview.md                # Extracted section
│   │   ├── logging-providers.md               # Extracted section
│   │   └── ...
│   ├── system.text.json/
│   │   ├── llms.txt
│   │   ├── json-overview.md
│   │   └── ...
│   └── ...
└── core/
    └── extensions/
        └── logging.md                          # Original full doc (5000 words)
```

### In Package Source Repo

```
src/libraries/Microsoft.Extensions.Logging/
├── src/
│   └── Microsoft.Extensions.Logging.csproj
├── docs/
│   └── package-docs.json                      # Curation manifest
└── README.md
```

---

## Curation Manifest Schema

```json
{
  "$schema": "https://learn.microsoft.com/schemas/nuget-package-docs.json",
  
  "package": "string (required) - NuGet package ID",
  
  "curated_docs": [
    {
      "title": "string (required) - Display title",
      "source": "string (optional) - Path to doc in main docs repo",
      "url": "string (optional) - External URL",
      "sections": ["array of section IDs to extract"],
      "priority": "number (1-10) - Display order",
      "external": "boolean - Is this external to docs repo?",
      "required": "boolean - Is this essential reading?"
    }
  ],
  
  "quick_start": {
    "title": "string",
    "content": "string - Path to quick start doc or markdown content"
  },
  
  "api_reference": "string (optional) - URL to API browser",
  
  "samples": [
    "array of paths to sample code"
  ],
  
  "related_packages": [
    "array of other package IDs"
  ],
  
  "metadata": {
    "maintainer": "string - GitHub username",
    "last_updated": "ISO 8601 date",
    "version": "string - Package version this applies to"
  }
}
```

---

## Section Extraction: How It Works

### Markdown Sections Identified by Headings

**Original doc: `docs/core/extensions/logging.md`**
```markdown
# Logging in .NET

## Overview {#overview}

Logging provides a way to capture information about the execution of your code...

## Getting Started {#getting-started}

To use logging in your application, add the Microsoft.Extensions.Logging package...

## Log Levels {#log-levels}

.NET defines several log levels...

## Configuration {#configuration}

Logging is configured in appsettings.json...

## Advanced Topics {#advanced-topics}

For advanced scenarios...
```

### Extraction Requests Specific Sections

**In `package-docs.json`:**
```json
{
  "source": "docs/core/extensions/logging.md",
  "sections": ["overview", "getting-started"]
}
```

**Extracted output:**
```markdown
# Logging in .NET

## Overview

Logging provides a way to capture information about the execution of your code...

## Getting Started

To use logging in your application, add the Microsoft.Extensions.Logging package...
```

### Smart Extraction Features

1. **Include code blocks** from sections
2. **Preserve links** to other docs
3. **Include images** referenced in sections
4. **Handle nested sections** (### sub-headings)
5. **Add "Read more" link** to full doc

---

## Example: Complete Workflow for Microsoft.Extensions.Logging

### 1. Package Owner Creates Manifest

**File: `runtime/src/libraries/Microsoft.Extensions.Logging/docs/package-docs.json`**

```json
{
  "package": "Microsoft.Extensions.Logging",
  "curated_docs": [
    {
      "title": "Logging in .NET",
      "source": "docs/core/extensions/logging.md",
      "sections": ["overview", "getting-started", "log-levels"],
      "priority": 1,
      "required": true
    },
    {
      "title": "Logging Providers",
      "source": "docs/core/extensions/logging-providers.md",
      "priority": 2
    },
    {
      "title": "High-Performance Logging",
      "source": "docs/core/extensions/logger-message-generator.md",
      "sections": ["source-generation"],
      "priority": 3
    },
    {
      "title": "Custom Provider Tutorial",
      "source": "docs/core/extensions/custom-logging-provider.md",
      "priority": 4
    },
    {
      "title": "ASP.NET Core Logging",
      "url": "https://learn.microsoft.com/aspnet/core/fundamentals/logging",
      "external": true,
      "priority": 5
    }
  ],
  "quick_start": {
    "content": "docs/core/extensions/logging.md",
    "sections": ["quick-start"]
  },
  "samples": [
    "samples/core/extensions/Logging/Basic",
    "samples/core/extensions/Logging/CustomProvider"
  ],
  "api_reference": "https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging",
  "related_packages": [
    "Microsoft.Extensions.Logging.Console",
    "Microsoft.Extensions.Logging.Debug",
    "Microsoft.Extensions.DependencyInjection"
  ]
}
```

### 2. Build Script Processes Manifest

```bash
# During package build
dotnet pack src/libraries/Microsoft.Extensions.Logging/src/Microsoft.Extensions.Logging.csproj

# Triggers doc assembly:
# 1. Clone/fetch docs repo
# 2. Read package-docs.json
# 3. Extract 4 docs + sections
# 4. Copy 2 sample folders
# 5. Generate llms.txt (43 lines)
# 6. Bundle in package at docs/
```

### 3. Generated llms.txt

**File: `docs/packages/microsoft.extensions.logging/llms.txt` (43 lines)**

```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications

## Quick Start

- [5-Minute Quick Start](quickstart.md): Set up logging in a console app

## Essential Reading (4 docs curated by package maintainers)

1. [Logging in .NET](logging-overview.md): Core concepts, log levels, and getting started (10 min)
2. [Logging Providers](logging-providers.md): Console, Debug, EventLog, and third-party providers (8 min)
3. [High-Performance Logging](logging-source-generation.md): Use source generators for zero-allocation logging (7 min)
4. [Custom Provider Tutorial](logging-custom-provider.md): Step-by-step guide to implement your own provider (15 min)
5. [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Integration with web applications (external)

## API Reference

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Main logging interface
- [Browse all APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Code Samples

- [Basic console logging](../../samples/logging-basic/): Complete working example
- [Custom provider implementation](../../samples/logging-custom/): Build your own provider

## Related Packages

- [Microsoft.Extensions.Logging.Console](../microsoft.extensions.logging.console/llms.txt)
- [Microsoft.Extensions.Logging.Debug](../microsoft.extensions.logging.debug/llms.txt)
- [Microsoft.Extensions.DependencyInjection](../microsoft.extensions.dependencyinjection/llms.txt)

## More Resources

- [Full documentation index](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Report issues](https://github.com/dotnet/runtime/issues)
```

### 4. Package Structure

**Option A: Bundled Docs**
```
microsoft.extensions.logging.9.0.0.nupkg
├── lib/net9.0/Microsoft.Extensions.Logging.dll
└── docs/
    ├── llms.txt                      # 43 lines
    ├── quickstart.md                 # ~400 words
    ├── logging-overview.md           # ~1000 words (extracted)
    ├── logging-providers.md          # ~800 words
    ├── logging-source-generation.md  # ~600 words
    ├── logging-custom-provider.md    # ~1200 words
    └── samples/
        ├── logging-basic/
        └── logging-custom/
```

**Option B: Web Links Only**
```
microsoft.extensions.logging.9.0.0.nupkg
├── lib/net9.0/Microsoft.Extensions.Logging.dll
└── docs/
    └── llms.txt                      # 43 lines with URLs
```

---

## Benefits of This Approach

### For Package Owners
- ✅ **Full control** over what docs ship with their package
- ✅ **Curate quality** - pick the best 4-6 docs, not dump everything
- ✅ **Stay focused** - only package-relevant content
- ✅ **Version-specific** - docs match package version
- ✅ **Include samples** - bundle working code

### For Developers
- ✅ **Discover easily** - docs right in package cache
- ✅ **Offline access** - no internet needed (Option A)
- ✅ **Relevant content** - no wading through unrelated docs
- ✅ **Quick wins** - 4-6 docs cover 80% of use cases

### For LLMs
- ✅ **High signal** - curated by humans who know the package
- ✅ **Small context** - 5-10KB vs 28MB of all docs
- ✅ **Structured** - clear priority order
- ✅ **Complete** - includes samples, not just links

---

## Comparison: Auto-Generated vs Curated

| Aspect | Auto-Generated (Tags) | Curated (Manifest) |
|--------|----------------------|-------------------|
| **Maintenance** | Easy (tags in frontmatter) | Medium (package owner creates manifest) |
| **Quality** | Medium (all tagged docs) | High (hand-picked best docs) |
| **Relevance** | Medium (may include tangential) | Very High (laser-focused) |
| **Sections** | Full docs only | Can extract specific sections |
| **Samples** | Separate | Bundled together |
| **Size** | Potentially large | Controlled (5-6 docs) |

---

## Implementation Checklist

### Phase 1: Schema & Tooling (1 week)
- [ ] Define `package-docs.json` schema
- [ ] Build doc extraction tool
- [ ] Create llms.txt generator
- [ ] Test with 2-3 packages

### Phase 2: Pilot Packages (2 weeks)
- [ ] Microsoft.Extensions.Logging
- [ ] System.Text.Json
- [ ] Microsoft.Extensions.DependencyInjection
- [ ] Gather feedback from maintainers

### Phase 3: Build Integration (1 week)
- [ ] Integrate into package build pipeline
- [ ] Support both bundled and URL-only modes
- [ ] Add to NuGet.org package display

### Phase 4: Rollout (4 weeks)
- [ ] Document for package owners
- [ ] Create templates/examples
- [ ] Assist top 50 packages
- [ ] Track adoption metrics

