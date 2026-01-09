# llms.txt Implementation Plan for .NET Documentation

**Version**: 1.0  
**Date**: January 2026  
**Status**: Proposal

---

## Executive Summary

This plan establishes an LLM-optimized documentation navigation system for .NET using the `llms.txt` format. The system consists of three components:

1. **Core Documentation Navigation**: Multi-file llms.txt hierarchy for the main docs repo
2. **Size Constraints**: Hard 50-line limit per file to force curation and maintain scannability
3. **NuGet Package Integration**: Curated package-specific documentation via subscription manifests

**Total Scope**: ~9 topic llms.txt files + 1 root (~450 lines total) + per-package manifests

---

## Component 1: Core Documentation llms.txt Files

### Overview

Convert existing toc.yml navigation (114 files, 28K lines) into a curated, hierarchical llms.txt system optimized for LLM consumption.

### Design Principles

1. **50-Line Hard Limit**: Each llms.txt file must be ≤50 lines
2. **Radical Curation**: Include only top 10-20% most valuable links
3. **Task-Oriented**: Organize by what developers want to DO, not file structure
4. **Two-Level Hierarchy**: Root + topics only (no sub-topics)
5. **Parent Linking**: All topic files link back to root

### File Structure

```
docs/
├── llms.txt                          # Root navigation (45 lines)
├── llms-getting-started.txt          # Beginners guide (50 lines)
├── llms-web.txt                      # Web development (50 lines)
├── llms-desktop-mobile.txt           # Desktop & mobile apps (40 lines)
├── llms-cloud.txt                    # Cloud & microservices (50 lines)
├── llms-data-ai.txt                  # Data access & AI (45 lines)
├── llms-languages.txt                # C#, F#, VB guides (50 lines)
├── llms-fundamentals.txt             # Core concepts (50 lines)
└── llms-tools.txt                    # CLI, deployment, debugging (50 lines)
```

**Total**: 9 files, ~430 lines (vs 28,165 lines in toc.yml)

### Root File Structure

**File**: `docs/llms.txt` (45 lines)

```markdown
# .NET Documentation

> Build applications for any platform with C#, F#, and Visual Basic.

## Quick Start

- [What is .NET?](core/introduction.md): Platform overview and capabilities
- [Install .NET](core/install/windows.md): Download SDK for Windows, Mac, or Linux
- [Your first app](core/get-started.md): 5-minute console app tutorial
- [What's new in .NET 10](core/whats-new/dotnet-10/overview.md): Latest features

## By Topic (Detailed Guides)

- [Getting Started Guide](llms-getting-started.txt): Tutorials, installation, first apps
- [Web Development](llms-web.txt): ASP.NET Core, Blazor, APIs, web apps
- [Desktop & Mobile Apps](llms-desktop-mobile.txt): WPF, WinForms, MAUI, cross-platform
- [Cloud & Microservices](llms-cloud.txt): Azure, Aspire, containers, distributed apps
- [Data & AI](llms-data-ai.txt): Entity Framework, ML.NET, databases, AI integration
- [Programming Languages](llms-languages.txt): C#, F#, Visual Basic language guides
- [Core Fundamentals](llms-fundamentals.txt): Runtime, async, DI, logging, testing
- [Tools & Deployment](llms-tools.txt): .NET CLI, Visual Studio, debugging, CI/CD

## By Language

- [C# guide](csharp/index.yml): Modern object-oriented language
- [F# guide](fsharp/index.yml): Functional-first .NET language
- [Visual Basic guide](visual-basic/index.yml): Approachable language for beginners

## Common Tasks

- [Migrate from .NET Framework](core/porting/index.md): Upgrade legacy apps to modern .NET
- [Build a web app](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app): ASP.NET Core MVC
- [Deploy to Azure](azure/migration/app-service.md): Host apps in the cloud
- [Troubleshooting](core/diagnostics/index.md): Debug and diagnose issues

## Reference

- [API Browser (.NET 10)](https://learn.microsoft.com/api/?view=net-10.0): Browse all APIs
- [Breaking changes](core/compatibility/breaking-changes.md): Version migration guide
- [.NET glossary](standard/glossary.md): Terms and definitions
- [GitHub repository](https://github.com/dotnet/docs): Contribute to documentation

## External Docs

- [ASP.NET Core](https://learn.microsoft.com/aspnet/core): Web framework documentation
- [Entity Framework Core](https://learn.microsoft.com/ef/core): ORM and data access
- [.NET MAUI](https://learn.microsoft.com/dotnet/maui): Mobile and desktop apps
```

### Topic File Structure

**Example**: `docs/llms-getting-started.txt` (50 lines)

```markdown
# Getting Started with .NET

> Parent: [.NET Documentation](llms.txt)

Learn .NET from scratch, install tools, and build your first applications.

## Absolute Beginners

- [What is .NET?](core/introduction.md): Platform overview and key concepts
- [.NET for beginners video](https://dotnet.microsoft.com/learn/videos): 10-minute introduction
- [Choose your language](fundamentals/languages.md): C#, F#, or Visual Basic comparison
- [.NET vs .NET Framework](fundamentals/implementations.md): Understand the differences

## Installation

- [Install on Windows](core/install/windows.md): Windows 10/11 installation guide
- [Install on macOS](core/install/macos.md): macOS installation (Intel and Apple Silicon)
- [Install on Linux](core/install/linux.md): Ubuntu, Debian, RHEL, and more
- [Check installed versions](core/install/how-to-detect-installed-versions.md): Verify SDK/runtime
- [Remove old versions](core/install/remove-runtime-sdk-versions.md): Clean up outdated installs

## Your First Apps

- [Console app tutorial](core/get-started.md): Hello World in 5 minutes
- [Web app tutorial](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app): Build a web app
- [Desktop app tutorial](https://learn.microsoft.com/dotnet/desktop/wpf/get-started/create-app-visual-studio): Windows desktop app
- [Sample apps](samples-and-tutorials/index.md): Browse working code examples

## Learning Paths

- [.NET tutorials overview](core/tutorials/index.md): Structured learning paths
- [C# 101 videos](https://dotnet.microsoft.com/learn/csharp): Learn C# basics
- [Build .NET apps training](https://learn.microsoft.com/training/paths/build-dotnet-applications-csharp): Microsoft Learn course
- [Architecture guides](architecture/index.yml): Design patterns and best practices

## Development Tools

- [Visual Studio](https://visualstudio.microsoft.com): Full-featured IDE (Windows/Mac)
- [Visual Studio Code](https://code.visualstudio.com/docs/languages/dotnet): Lightweight cross-platform editor
- [.NET CLI](core/tools/index.md): Command-line tools and commands
- [JetBrains Rider](https://www.jetbrains.com/rider): Third-party .NET IDE

## Key Concepts

- [Projects and solutions](core/tutorials/with-visual-studio.md): Organize your code
- [NuGet packages](core/tools/dependencies.md): Add libraries to your project
- [Build and run apps](core/tools/dotnet-build.md): Compile and execute code
- [Debug your code](https://learn.microsoft.com/visualstudio/debugger): Find and fix bugs
- [Unit testing](core/testing/index.md): Test your applications

## Next Steps

- [Fundamentals guide](llms-fundamentals.txt): Core .NET concepts and APIs
- [Web development](llms-web.txt): Build web applications
- [Desktop/mobile](llms-desktop-mobile.txt): Client applications
- [Cloud development](llms-cloud.txt): Azure and microservices

## Help & Community

- [Q&A Forum](https://learn.microsoft.com/answers/products/dotnet): Ask questions
- [.NET Foundation](https://dotnetfoundation.org): Open source community
- [Discord server](https://aka.ms/dotnet-discord): Chat with developers
```

### Conversion Process

**Source**: Existing toc.yml files (114 files, 28,165 lines)  
**Method**: Manual curation with assistance from toc.yml structure

#### Phase 1: Manual Curation (Weeks 1-2)
1. **Analyze toc.yml files**: Review existing structure and identify high-value docs
2. **Create root llms.txt**: 
   - Identify top 10-15 entry points
   - Add links to topic files
   - Include quick reference section
3. **Create 9 topic files**:
   - Extract ~15-20 key docs per topic from toc.yml
   - Write compelling descriptions (action-oriented, not generic)
   - Add parent reference link
   - Include related topic cross-links

#### Phase 2: Review & Validation (Week 3)
1. **Internal review**: Docs team validates completeness and accuracy
2. **Link verification**: Ensure all links work (no 404s)
3. **LLM testing**: Test with ChatGPT, Claude, GitHub Copilot
4. **Community feedback**: Share with .NET community for input

#### Phase 3: Publishing (Week 4)
1. **Publish to web**: Deploy to `https://learn.microsoft.com/dotnet/llms.txt`
2. **Update README**: Add LLM navigation section to repo README
3. **Announcement**: Blog post and social media

---

## Component 2: Size Constraints & Quality Standards

### Hard Limits

**Per-File Maximum**: 50 lines  
**Root File Target**: 40-45 lines  
**Topic File Target**: 45-50 lines

### Rationale

- **LLM Scanning**: File readable in <30 seconds
- **Context Efficiency**: ~1,200 tokens vs Stripe's 20,000
- **Forces Curation**: Must choose what's truly essential
- **Human Usable**: Also useful for humans browsing
- **Gold Standard**: Matches Claude Code's 53-line example

### Quality Checklist (per file)

Before publishing, each file must satisfy:

- [ ] Total lines ≤50
- [ ] Every link has a clear, action-oriented description
- [ ] No broken links (all URLs return 200 OK)
- [ ] Organized by task/goal, not file structure
- [ ] Covers 80% of common use cases for this topic
- [ ] Scannable in <30 seconds
- [ ] No generic descriptions ("Learn about X")
- [ ] Includes external docs where relevant (ASP.NET, EF Core)
- [ ] Parent link present (for topic files)
- [ ] Cross-links to related topics

### Enforcement

**CI/CD Validation**:
```bash
# GitHub Actions check on PR
- name: Validate llms.txt files
  run: |
    for file in docs/llms*.txt; do
      lines=$(wc -l < "$file")
      if [ $lines -gt 50 ]; then
        echo "ERROR: $file has $lines lines (max 50)"
        exit 1
      fi
    done
```

**Manual Review**: Docs team reviews all changes to llms.txt files

---

## Component 3: NuGet Package Integration

### Overview

Enable NuGet packages to curate and include documentation specific to their package, discoverable by LLMs.

### Package Manifest System

#### Step 1: Package Owner Creates Manifest

**Location**: `{package-repo}/docs/package-docs.json`

**Example**: `runtime/src/libraries/Microsoft.Extensions.Logging/docs/package-docs.json`

```json
{
  "$schema": "https://learn.microsoft.com/schemas/nuget-package-docs-v1.json",
  "package": "Microsoft.Extensions.Logging",
  "curated_docs": [
    {
      "title": "Logging in .NET Overview",
      "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging.md",
      "sections": ["overview", "getting-started"],
      "priority": 1,
      "required": true
    },
    {
      "title": "Logging Providers",
      "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging-providers.md",
      "priority": 2
    },
    {
      "title": "High-Performance Logging",
      "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logger-message-generator.md",
      "sections": ["source-generation"],
      "priority": 3
    },
    {
      "title": "Custom Provider Tutorial",
      "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/custom-logging-provider.md",
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
    "title": "5-Minute Quick Start",
    "source": "https://raw.githubusercontent.com/dotnet/docs/main/docs/core/extensions/logging.md",
    "sections": ["quick-start"]
  },
  "api_reference": "https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging",
  "samples": [
    "https://github.com/dotnet/samples/tree/main/core/extensions/Logging/Basic",
    "https://github.com/dotnet/samples/tree/main/core/extensions/Logging/CustomProvider"
  ],
  "related_packages": [
    "Microsoft.Extensions.Logging.Console",
    "Microsoft.Extensions.Logging.Debug",
    "Microsoft.Extensions.DependencyInjection"
  ],
  "metadata": {
    "maintainer": "dotnet-team",
    "last_updated": "2026-01-09",
    "min_package_version": "9.0.0"
  }
}
```

#### Step 2: Build-Time Doc Generation

**Tool**: `dotnet-docs-generator` (new CLI tool)

**During package build**:
```bash
# In package .csproj or build script
<Target Name="GeneratePackageDocs" BeforeTargets="Pack">
  <Exec Command="dotnet docs-generator generate --manifest docs/package-docs.json --output $(IntermediateOutputPath)docs/" />
</Target>

# Includes generated docs in package
<ItemGroup>
  <None Include="$(IntermediateOutputPath)docs/**" Pack="true" PackagePath="docs/" />
</ItemGroup>
```

**Tool behavior**:
1. Read `package-docs.json`
2. Fetch referenced docs from URLs
3. Extract specified sections (if `sections` array provided)
4. Generate consolidated llms.txt (≤50 lines)
5. Optionally bundle full extracted docs or just create llms.txt with links

#### Step 3: Generated Package llms.txt

**Location in package**: `{package}/docs/llms.txt`

**Example output**: `Microsoft.Extensions.Logging/docs/llms.txt` (48 lines)

```markdown
# Microsoft.Extensions.Logging

> Logging framework for .NET applications

## Quick Start

- [5-Minute Quick Start](https://learn.microsoft.com/dotnet/core/extensions/logging#quick-start): Set up logging in a console app

## Essential Documentation

Curated by package maintainers - the must-read docs for this package:

1. [Logging in .NET Overview](https://learn.microsoft.com/dotnet/core/extensions/logging): Core concepts and getting started (10 min read)
2. [Logging Providers](https://learn.microsoft.com/dotnet/core/extensions/logging-providers): Built-in providers and configuration (8 min read)
3. [High-Performance Logging](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator): Source generators for zero-allocation logging (7 min read)
4. [Custom Provider Tutorial](https://learn.microsoft.com/dotnet/core/extensions/custom-logging-provider): Step-by-step implementation guide (15 min read)
5. [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Integration with web applications

## API Reference

- [ILogger<T>](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1): Primary logging interface
- [ILoggerFactory](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggerfactory): Create logger instances
- [LogLevel enum](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel): Trace, Debug, Information, Warning, Error, Critical
- [Browse all APIs](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Code Samples

- [Basic console logging](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/Basic): Complete working example
- [Custom provider implementation](https://github.com/dotnet/samples/tree/main/core/extensions/Logging/CustomProvider): Build your own provider

## Related Packages

- [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console): Console output provider
- [Microsoft.Extensions.Logging.Debug](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug): Debug window output
- [Microsoft.Extensions.Logging.EventLog](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventLog): Windows Event Log provider
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection): Required for DI integration

## Package Information

- **Install**: `dotnet add package Microsoft.Extensions.Logging`
- **Version**: 9.0.0+
- **Source**: https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Logging
- **License**: MIT

## More Resources

- [Full documentation index](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Report issues](https://github.com/dotnet/runtime/issues)
```

#### Step 4: LLM Discovery

**Developer workflow**:
1. `dotnet add package Microsoft.Extensions.Logging`
2. Package restored to `~/.nuget/packages/microsoft.extensions.logging/9.0.0/`
3. Docs available at `~/.nuget/packages/microsoft.extensions.logging/9.0.0/docs/llms.txt`

**LLM workflow**:
1. LLM sees `<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />` in project
2. LLM checks local package cache for docs: `~/.nuget/packages/microsoft.extensions.logging/9.0.0/docs/llms.txt`
3. If found: Read curated package docs
4. If not found: Fall back to main docs at `https://learn.microsoft.com/dotnet/core/extensions/logging`

### Package Documentation Options

**Option A: Links Only (Recommended)**
- Package contains only llms.txt (~2KB)
- Links point to web docs
- Always up-to-date
- Small package size

**Option B: Bundled Docs**
- Package contains llms.txt + extracted markdown files
- Works offline
- ~5-10KB additional size
- Can go stale

### Manifest Schema v1.0

```json
{
  "$schema": "https://learn.microsoft.com/schemas/nuget-package-docs-v1.json",
  "package": "string (required) - NuGet package ID",
  "curated_docs": [
    {
      "title": "string (required) - Display title",
      "source": "string (optional) - URL to markdown doc",
      "url": "string (optional) - Direct URL (no extraction)",
      "sections": ["array (optional) - Section IDs to extract"],
      "priority": "number (required) - Display order (1-10)",
      "external": "boolean (optional) - External to docs repo",
      "required": "boolean (optional) - Essential reading"
    }
  ],
  "quick_start": {
    "title": "string (optional)",
    "source": "string (optional) - URL to quick start doc",
    "sections": ["array (optional)"]
  },
  "api_reference": "string (optional) - URL to API browser",
  "samples": ["array (optional) - URLs to sample code repos"],
  "related_packages": ["array (optional) - Related package IDs"],
  "metadata": {
    "maintainer": "string (optional) - GitHub username",
    "last_updated": "string (optional) - ISO 8601 date",
    "min_package_version": "string (optional) - Minimum package version"
  }
}
```

---

## Implementation Timeline

### Phase 1: Core Documentation (Weeks 1-4)

**Week 1: Planning & Structure**
- [ ] Finalize 9 topic areas
- [ ] Map toc.yml content to topics
- [ ] Create draft root llms.txt

**Week 2: Content Creation**
- [ ] Write all 9 topic llms.txt files
- [ ] Ensure ≤50 lines each
- [ ] Write compelling descriptions

**Week 3: Review & Validation**
- [ ] Internal docs team review
- [ ] Link validation (automated)
- [ ] LLM testing (ChatGPT, Claude, Copilot)
- [ ] Community preview and feedback

**Week 4: Publishing**
- [ ] Deploy to learn.microsoft.com
- [ ] Update repo README
- [ ] Add CI/CD validation
- [ ] Announcement blog post

**Deliverables**:
- 10 llms.txt files (1 root + 9 topics)
- ~430 lines total
- CI/CD validation pipeline
- Documentation for contributors

### Phase 2: NuGet Package System (Weeks 5-10)

**Week 5: Schema & Tooling**
- [ ] Define package-docs.json schema v1.0
- [ ] Publish schema to learn.microsoft.com/schemas
- [ ] Create `dotnet-docs-generator` CLI tool
- [ ] Test extraction and generation logic

**Week 6: Pilot Packages**
- [ ] Create manifests for 3 pilot packages:
  - Microsoft.Extensions.Logging
  - System.Text.Json
  - Microsoft.Extensions.DependencyInjection
- [ ] Generate and validate llms.txt output
- [ ] Test bundled vs links-only modes

**Week 7-8: Build Integration**
- [ ] Integrate into runtime build pipeline
- [ ] Test with nightly builds
- [ ] Document package owner workflow
- [ ] Create manifest templates

**Week 9: Documentation & Outreach**
- [ ] Write package owner guide
- [ ] Create how-to videos
- [ ] Publish examples to GitHub
- [ ] Prepare announcement materials

**Week 10: Launch**
- [ ] Ship with .NET 10 Preview 2 packages
- [ ] Blog post announcement
- [ ] Community call presentation
- [ ] Monitor adoption and feedback

**Deliverables**:
- package-docs.json schema v1.0
- dotnet-docs-generator CLI tool
- 3 pilot package manifests
- Package owner documentation
- Build pipeline integration

### Phase 3: Rollout & Iteration (Weeks 11-16)

**Weeks 11-12: Top Package Coverage**
- [ ] Work with top 20 package teams to create manifests
- [ ] Provide hands-on assistance
- [ ] Gather feedback and pain points
- [ ] Iterate on tooling based on feedback

**Weeks 13-14: Third-Party Support**
- [ ] Publish guide for third-party packages
- [ ] Create self-service tooling
- [ ] Set up support channels
- [ ] Monitor adoption metrics

**Weeks 15-16: Analysis & Optimization**
- [ ] Analyze LLM usage patterns
- [ ] Measure discovery success rate
- [ ] Gather developer feedback
- [ ] Plan v2.0 improvements

**Deliverables**:
- 20+ packages with manifests
- Third-party package guide
- Usage analytics dashboard
- v2.0 feature roadmap

---

## Success Metrics

### Core Documentation Metrics

**Adoption**:
- [ ] LLM tools (ChatGPT, Claude, Copilot) reference llms.txt files
- [ ] 50+ stars on announcement tweet/post
- [ ] 10+ community contributions to llms.txt files

**Quality**:
- [ ] All files maintain ≤50 line limit
- [ ] Zero broken links in production
- [ ] 90%+ developer satisfaction in surveys

**Usage**:
- [ ] 1000+ page views per month on llms.txt files
- [ ] Cited in 5+ blog posts or tutorials
- [ ] Referenced in GitHub Copilot context

### Package Integration Metrics

**Adoption**:
- [ ] 20+ Microsoft packages with manifests
- [ ] 10+ third-party packages with manifests
- [ ] Tool downloaded 500+ times

**Quality**:
- [ ] 100% of manifests pass schema validation
- [ ] 95%+ of generated llms.txt files ≤50 lines
- [ ] Zero build failures due to doc generation

**Impact**:
- [ ] 50%+ reduction in "where's the docs?" issues
- [ ] 30%+ increase in package doc engagement
- [ ] Positive LLM feedback in testing

---

## Maintenance Plan

### Ongoing Responsibilities

**Docs Team**:
- **Monthly**: Review and update core llms.txt files
- **Quarterly**: Add new major features/products
- **Continuous**: Respond to community PRs
- **As needed**: Fix broken links

**Package Owners**:
- **Per release**: Update package-docs.json if major changes
- **Quarterly**: Review curated docs for relevance
- **As needed**: Fix issues reported by users

### Update Process

**Core llms.txt files**:
1. Propose change via GitHub PR
2. Validate ≤50 line limit (CI check)
3. Docs team review
4. Merge and auto-deploy

**Package manifests**:
1. Package owner updates package-docs.json in their repo
2. Next package build auto-generates updated llms.txt
3. Published with package to NuGet

### Governance

**Decision Making**:
- Docs team owns core llms.txt content
- Package teams own their package manifests
- Community can propose additions via PRs
- Final approval: Docs team lead

**Breaking Changes**:
- File structure: Avoid, maintain backward compatibility
- Schema changes: Version bump (v2.0), maintain v1.0 support for 6 months
- URL changes: Use redirects

---

## Risk Mitigation

### Risk 1: Files Exceed 50 Lines
**Likelihood**: High  
**Impact**: Medium  
**Mitigation**: 
- CI/CD enforces limit
- Regular audits
- Clear guidance on what to remove

### Risk 2: Links Go Stale
**Likelihood**: Medium  
**Impact**: High  
**Mitigation**:
- Automated link checker (weekly)
- Community reports via GitHub issues
- Redirect old URLs

### Risk 3: Low Adoption by Package Owners
**Likelihood**: Medium  
**Impact**: Medium  
**Mitigation**:
- Hands-on assistance for top packages
- Clear value proposition
- Make tooling easy to use
- Showcase success stories

### Risk 4: LLMs Don't Use llms.txt
**Likelihood**: Low  
**Impact**: High  
**Mitigation**:
- Test with major LLMs before launch
- Engage with AI companies
- Follow llms.txt community standards
- Provide feedback to LLM vendors

### Risk 5: Maintenance Burden
**Likelihood**: Medium  
**Impact**: Medium  
**Mitigation**:
- Keep file count low (9 topics)
- 50-line limit reduces update scope
- Automated tooling for packages
- Community contributions

---

## Open Questions

### Technical

1. **Section Extraction**: How to mark sections in markdown for extraction?
   - **Proposal**: Use heading IDs: `## Overview {#overview}`
   - **Alternative**: Use HTML comments: `<!-- section:overview -->`

2. **Versioning**: Should package llms.txt be version-specific?
   - **Proposal**: Latest only, with min version in manifest
   - **Alternative**: Per-version: `docs/v9.0/llms.txt`

3. **External Packages**: How do third-party packages participate?
   - **Proposal**: Self-service via schema + tool
   - **Alternative**: Submit manifest to central registry

### Process

4. **Approval Process**: Who approves changes to core llms.txt files?
   - **Proposal**: Any docs team member can approve
   - **Alternative**: Designated llms.txt curator

5. **Update Frequency**: How often should we review core files?
   - **Proposal**: Monthly spot checks, quarterly deep reviews
   - **Alternative**: On-demand when major .NET releases

6. **Community Contributions**: How to handle external PRs?
   - **Proposal**: Welcome PRs, docs team reviews within 1 week
   - **Alternative**: Lock files, only internal edits

---

## Appendices

### Appendix A: File Naming Convention

```
llms.txt                 # Always the root
llms-{topic}.txt         # Topic files
```

**Rules**:
- Lowercase with hyphens
- Single word preferred: `web`, `cloud`, `tools`
- Two words maximum: `getting-started`, `data-ai`
- Descriptive and self-explanatory
- No nested structure: no `llms-web-blazor.txt`

### Appendix B: Link Description Guidelines

**Good Descriptions** (action-oriented, specific):
- ✅ "Build your first web app in 10 minutes"
- ✅ "Migrate from .NET Framework to .NET 8"
- ✅ "Use source generators for zero-allocation logging"

**Bad Descriptions** (generic, vague):
- ❌ "Learn about web apps"
- ❌ "Migration guide"
- ❌ "Logging documentation"

### Appendix C: Tools and Automation

**Required Tooling**:
1. **llms.txt validator**: CI check for 50-line limit
2. **Link checker**: Weekly automated scan
3. **dotnet-docs-generator**: Package manifest processor
4. **Schema validator**: Validate package-docs.json files

**Optional Tooling**:
5. **Analytics dashboard**: Track usage and adoption
6. **AI description generator**: Suggest descriptions from doc content
7. **Manifest wizard**: Interactive tool to create manifests

### Appendix D: Related Standards

- **llms.txt specification**: https://llmstxt.org (community standard)
- **Markdown CommonMark**: https://commonmark.org
- **JSON Schema**: https://json-schema.org
- **NuGet package format**: https://learn.microsoft.com/nuget/reference/nuspec

---

## Conclusion

This plan establishes a modern, LLM-optimized documentation navigation system for .NET that:

1. **Replaces** unfocused 28K-line toc.yml dumps with curated 50-line files
2. **Enables** hierarchical navigation via root + 9 topic files
3. **Empowers** package owners to curate essential docs for their packages
4. **Delivers** high-quality, scannable documentation discovery for LLMs and humans

**Next Steps**: 
1. Approve plan
2. Assign team members
3. Begin Week 1 activities
4. Schedule regular check-ins

**Questions or Feedback**: Contact docs team lead or open GitHub issue

---

**Document Version**: 1.0  
**Last Updated**: January 9, 2026  
**Authors**: .NET Docs Team  
**Status**: Awaiting Approval
