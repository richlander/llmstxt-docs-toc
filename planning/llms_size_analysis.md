# llms.txt Size Analysis & Recommendations

## Comparison: Good vs Bad Examples

### ✅ Claude Code (Good Example)
- **Lines**: 53
- **Words**: ~500
- **Characteristics**:
  - Every link has a clear, concise description
  - Organized into single logical section ("Docs")
  - Scannable in seconds
  - High signal-to-noise ratio
  - Complete but not exhaustive

### ❌ Stripe (Bad Example)
- **Lines**: 1000+ (estimated from truncated fetch)
- **Words**: ~20,000 (20k tokens as you noted)
- **Problems**:
  - Overwhelming - impossible to scan quickly
  - Kitchen-sink approach (everything is important = nothing is important)
  - LLM context window waste
  - Likely auto-generated dump from navigation
  - No curation or prioritization

## Proposed Design Constraint

**Maximum Lines**: **100 lines** (2x Claude Code's 53 lines)

### Rationale
- **LLM scanning**: Should be readable in <30 seconds
- **Context efficiency**: Leaves room for actual documentation in context window
- **Forces curation**: Must choose what's truly important
- **Human readable**: Useful for humans too, not just machines
- **Comparison**: 100 lines ≈ 1000 words ≈ 1300 tokens (vs Stripe's 20k)

## Implications for .NET Docs

Given constraints:

### Current State
- 114 toc.yml files
- 28,165 total lines
- Fundamentals alone: 2,400 lines

### With 100-line Constraint

#### ❌ **Options That Won't Work**
- **Option A** (Hierarchical per-section): 114 files × 100 lines = still overwhelming
- **Option B** (Single flattened): Would be 28K+ lines - totally infeasible
- **Option F** (Enhanced YAML): Would generate same bloat

#### ✅ **Options That Work**
- **Option D** (Purpose-built): ✅ Perfect fit - manually curate ~100 high-value links
- **Option H** (Enhanced README): ✅ Very lightweight, just pointers
- **Option G** (Smart index): ✅ Can limit to key docs only

#### ⚠️ **Options That Could Work**
- **Option C** (Two-tier): ✅ If root is <100 lines, but defeats purpose if still pointing to toc.yml
- **Option E** (Topic-based): ⚠️ Only if total across ALL files is <300 lines

## Recommended Approach: Enhanced Option D

### Single File: `docs/llms.txt` (Target: 80-100 lines)

```markdown
# .NET Documentation

> Curated navigation for AI assistants. Full navigation at https://learn.microsoft.com/dotnet

## Getting Started (10 links)

- [What is .NET?](core/introduction.md): Overview of .NET platform and capabilities
- [Your first app](core/get-started.md): 5-minute tutorial to build a console app
- [Install .NET](core/install/windows.md): Download and install .NET SDK
- [Choose your language](fundamentals/languages.md): C#, F#, or Visual Basic
- [.NET vs .NET Framework](fundamentals/implementations.md): Understanding version differences
- [Tutorials overview](core/tutorials/index.md): Step-by-step learning paths
- [Sample apps](samples-and-tutorials/index.md): Browse working code examples
- [Video: .NET in 100 seconds](https://dotnet.microsoft.com/learn/videos): Quick visual introduction
- [Architecture guide](architecture/index.yml): Design patterns and best practices
- [What's new in .NET 10](core/whats-new/dotnet-10/overview.md): Latest features

## Building Applications (20 links)

### Web Development
- [ASP.NET Core overview](https://learn.microsoft.com/aspnet/core): Modern web framework
- [Build a web app](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app): MVC tutorial
- [Build a REST API](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api): API tutorial
- [Blazor apps](https://learn.microsoft.com/aspnet/core/blazor): Interactive WebAssembly + server
- [Deploy to Azure](azure/migration/app-service.md): Host web apps in the cloud

### Desktop & Mobile
- [Windows desktop (WPF/WinForms)](https://learn.microsoft.com/dotnet/desktop): Desktop applications
- [.NET MAUI](https://learn.microsoft.com/dotnet/maui): Cross-platform mobile and desktop

### Cloud & Microservices
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire): Cloud-native app development
- [Azure for .NET](azure/index.yml): Integrate with Azure services
- [Microservices patterns](architecture/microservices/index.md): Distributed system design
- [Containers & Docker](core/docker/build-container.md): Containerize .NET apps

### AI & Data
- [Build AI apps](ai/index.yml): Integrate AI models with .NET
- [ML.NET](machine-learning/index.yml): Machine learning framework
- [Entity Framework Core](https://learn.microsoft.com/ef/core): ORM for databases
- [LINQ](standard/linq/index.md): Query data with Language Integrated Query

### Specialized
- [IoT devices](iot/index.yml): Raspberry Pi, sensors, embedded systems
- [Game development](https://learn.microsoft.com/dotnet/games): Build games with .NET
- [Orleans](orleans/index.yml): Virtual actor framework for distributed apps

## Core Concepts (20 links)

- [Runtime & BCL](standard/runtime-libraries-overview.md): Base class libraries overview
- [Dependency injection](core/extensions/dependency-injection.md): Built-in DI container
- [Configuration](core/extensions/configuration.md): App settings and options pattern
- [Logging](core/extensions/logging.md): Structured logging framework
- [Async/await](standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap.md): Asynchronous programming
- [Threading](standard/threading/managed-threading-basics.md): Concurrency fundamentals
- [Memory & GC](standard/garbage-collection/index.md): Garbage collection explained
- [Serialization (JSON)](standard/serialization/system-text-json/overview.md): Work with JSON data
- [HTTP client](fundamentals/networking/http/httpclient-guidelines.md): Make HTTP requests
- [File I/O](standard/io/index.md): Read and write files
- [Error handling](standard/exceptions/best-practices-for-exceptions.md): Exception best practices
- [Unit testing](core/testing/index.md): Test your applications
- [Performance](fundamentals/code-analysis/overview.md): Optimize for speed
- [Security](standard/security/index.md): Secure your applications
- [Native interop](standard/native-interop/index.md): Call C/C++ libraries
- [Reflection](standard/reflection-and-codedom/index.md): Runtime type inspection
- [Globalization](standard/globalization-localization/index.md): Build world-ready apps
- [Collections](standard/collections/index.md): Lists, dictionaries, and more
- [Regular expressions](standard/base-types/regular-expressions.md): Text pattern matching
- [DateTime handling](standard/datetime/index.md): Work with dates and times

## Language Guides (15 links)

### C#
- [C# guide home](csharp/index.yml): Full C# documentation
- [C# language tour](csharp/tour-of-csharp/index.yml): Quick introduction
- [What's new in C# 13](csharp/whats-new/csharp-13.md): Latest language features
- [C# language reference](csharp/language-reference/index.yml): Syntax and keywords
- [C# tutorials](csharp/tutorials/index.md): Hands-on learning

### F#
- [F# guide home](fsharp/index.yml): Full F# documentation
- [F# language tour](fsharp/tour.md): Functional programming basics
- [F# language reference](fsharp/language-reference/index.md): Syntax and features

### Visual Basic
- [VB guide home](visual-basic/index.yml): Full Visual Basic documentation
- [VB language reference](visual-basic/language-reference/index.md): Syntax and keywords

### Multi-language
- [Choose a language](fundamentals/languages.md): Compare C#, F#, and VB
- [Language interoperability](standard/language-independence.md): Use multiple languages together
- [Porting between languages](core/porting/index.md): Convert code

## Tools & Deployment (15 links)

- [.NET CLI](core/tools/index.md): Command-line interface reference
- [SDK overview](core/sdk.md): .NET SDK components
- [NuGet packages](core/tools/dependencies.md): Manage dependencies
- [MSBuild reference](core/project-sdk/msbuild-props.md): Build system properties
- [Publishing apps](core/deploying/index.md): Deploy .NET applications
- [GitHub Actions](devops/github-actions-overview.md): CI/CD with GitHub
- [Docker integration](core/docker/introduction.md): Containerization basics
- [Visual Studio](https://visualstudio.com): Full-featured IDE
- [VS Code](https://code.visualstudio.com/docs/languages/dotnet): Lightweight editor
- [Debugging](core/diagnostics/index.md): Debug and diagnose issues
- [Performance profiling](core/diagnostics/profile-app.md): Find performance bottlenecks
- [Code analysis](fundamentals/code-analysis/overview.md): Static analysis tools
- [Package validation](fundamentals/package-validation/overview.md): Ensure API compatibility
- [Global tools](core/tools/global-tools.md): Install CLI tools
- [Templates](core/install/templates.md): Project templates

## Migration & Compatibility (10 links)

- [Migrate from .NET Framework](core/porting/index.md): Move to modern .NET
- [Breaking changes](core/compatibility/breaking-changes.md): Version upgrade guide
- [Compatibility analyzer](core/compatibility/index.md): Detect compatibility issues
- [API comparison](fundamentals/implementations.md): .NET vs Framework APIs
- [Windows compatibility pack](core/porting/windows-compat-pack.md): Use Framework APIs
- [Upgrade Assistant](core/porting/upgrade-assistant-overview.md): Automated migration tool
- [GitHub Copilot upgrade](core/porting/github-copilot-app-modernization/overview.md): AI-assisted migration
- [.NET Framework guide](framework/index.yml): Legacy .NET documentation
- [Support policy](core/releases-and-support.md): LTS and release schedule
- [Download .NET](https://dotnet.microsoft.com/download): Get SDK and runtimes

## Reference (5 links)

- [API browser (.NET 10)](https://learn.microsoft.com/api/?view=net-10.0): Browse all APIs
- [API browser (ASP.NET Core)](https://learn.microsoft.com/api/?view=aspnetcore-10.0): Web APIs
- [.NET glossary](standard/glossary.md): Common terms defined
- [Ecma standards](fundamentals/standards.md): Language specifications
- [GitHub repository](https://github.com/dotnet/docs): Contribute to docs

---

**Total: 95 links organized into 7 sections**
**Estimated tokens: ~1200** (vs Stripe's 20,000)
```

## Key Design Principles

### 1. **Radical Curation**
- Include only the top 10-20% most valuable docs
- Every link must justify its existence
- Remove redundancy ruthlessly

### 2. **Task-Oriented Organization**
- "Getting Started" → "Building Applications" → "Core Concepts"
- Group by what developers want to DO, not repository structure
- Progressive disclosure: beginner → intermediate → advanced

### 3. **Rich Descriptions**
- Every link gets a clear, value-focused description
- No generic descriptions like "Learn more about X"
- Action-oriented: "Build a web app", not "Web apps overview"

### 4. **External Link Strategy**
- Include key external docs (ASP.NET Core, EF Core) inline
- Don't say "see external docs" - just link them
- Treat entire Microsoft Learn as one cohesive resource

### 5. **Maintenance Strategy**
- Manual curation by docs team
- Review quarterly for link rot and new essential content
- Add new links = remove old links (stay under 100 lines)

## Implementation Plan

### Phase 1: Create Initial Version (4-6 hours)
1. Draft structure with sections
2. Identify top 15-20 docs per section
3. Write compelling descriptions
4. Review with stakeholders
5. Deploy to `docs/llms.txt`

### Phase 2: Validation (2 weeks)
1. Test with actual LLM tools (ChatGPT, Claude, GitHub Copilot)
2. Gather feedback from community
3. Track usage analytics if possible
4. Iterate based on findings

### Phase 3: Maintenance Process (ongoing)
1. Assign owner (docs team member)
2. Quarterly review cycle
3. Add new major features to llms.txt
4. Remove deprecated/outdated links
5. Keep under 100 lines always

## Quality Checklist

Before publishing, verify:
- [ ] Total lines < 100
- [ ] Every link has description
- [ ] No broken links
- [ ] Organized task-first, not structure-first
- [ ] Covers all major .NET scenarios
- [ ] Includes C#, F#, VB equally
- [ ] Links to external docs (ASP.NET, EF)
- [ ] Scannable in <1 minute
- [ ] Useful for humans too, not just LLMs
- [ ] Maintained independently of toc.yml

---

## Comparison: This Approach vs Others

| Approach | Lines | Maintainable? | LLM-Friendly? | Human Value? | Verdict |
|----------|-------|---------------|---------------|--------------|---------|
| **Option D (100-line limit)** | 100 | ✅ Yes | ✅ Excellent | ✅ High | ✅ **RECOMMENDED** |
| Option A (Hierarchical) | 5,000+ | ⚠️ Complex | ⚠️ Too detailed | ❌ Low | ❌ Too large |
| Option B (Single flat) | 38,000+ | ❌ No | ❌ Terrible | ❌ None | ❌ Stripe-like failure |
| Stripe approach | 20,000+ | ❌ No | ❌ Overwhelming | ❌ None | ❌ **Anti-pattern** |
| Claude Code | 53 | ✅ Yes | ✅ Perfect | ✅ High | ✅ **Gold standard** |

