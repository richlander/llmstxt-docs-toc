# Multi-File llms.txt Design: 50-Line Maximum Per File

## Core Principle
**Each llms.txt file must be ≤50 lines** (matching Claude Code's 53-line sweet spot)

This forces:
- Extreme curation per file
- Clear topical boundaries
- Multiple small, scannable files instead of one giant file

---

## Example: How to Fix Stripe's 600-line llms.txt

### Current Problem
Stripe has ONE 600-line file covering:
- Payment methods (100+ lines)
- Checkout (100+ lines)
- Billing/subscriptions (80+ lines)
- Connect/platforms (40+ lines)
- Terminal (20+ lines)
- Radar/fraud (20+ lines)
- APIs/webhooks (40+ lines)
- etc.

### Proposed Fix: Split into Topic Files

```
docs/
├── llms.txt                          # Root index (40 lines)
├── llms-payments.txt                 # Core payments (50 lines)
├── llms-checkout.txt                 # Checkout products (45 lines)
├── llms-billing.txt                  # Subscriptions (50 lines)
├── llms-connect.txt                  # Platforms/marketplaces (50 lines)
├── llms-fraud.txt                    # Radar & fraud prevention (35 lines)
└── llms-developers.txt               # APIs, webhooks, testing (50 lines)
```

#### Root: docs/llms.txt (40 lines)
```markdown
# Stripe Documentation

> Payment infrastructure for the internet. For detailed topics, see specialized llms files below.

## Start Here

- [Quickstart: Accept a payment](quickstart.md): Build your first integration in 10 minutes
- [API Reference](api.md): Complete REST API documentation
- [Testing guide](testing.md): Test payments without real money
- [Supported currencies](currencies.md): 135+ currencies supported

## By Use Case

- [Online payments](llms-payments.txt): Accept cards, wallets, and local payment methods
- [Subscription billing](llms-billing.txt): Recurring payments and subscription management
- [Checkout pages](llms-checkout.txt): Pre-built hosted payment pages
- [Platforms & marketplaces](llms-connect.txt): Multi-party payments with Connect
- [Fraud prevention](llms-fraud.txt): Radar and machine learning fraud detection
- [Developer tools](llms-developers.txt): APIs, webhooks, SDKs, and testing

## By Product

- [Payment Links](payment-links.md): No-code payment pages (share a link)
- [Terminal](terminal.md): In-person card readers
- [Invoicing](invoicing.md): Send and manage invoices
- [Tax](tax.md): Automated sales tax calculation
- [Climate](climate.md): Carbon removal contributions

## Common Tasks

- [Refund a payment](refunds.md): Cancel or partially refund charges
- [Handle disputes](disputes.md): Respond to chargebacks
- [Set up webhooks](webhooks/quickstart.md): Listen for events
- [Go live checklist](go-live.md): Pre-launch requirements
- [Security best practices](security/guide.md): PCI compliance and secure integration

## Get Help

- [Support](https://support.stripe.com): Contact Stripe support
- [Status](https://status.stripe.com): API uptime and incidents
- [Community](https://github.com/stripe): Open source projects
```

#### Topic File: llms-payments.txt (50 lines)
```markdown
# Stripe Payments

> Parent: [Stripe Documentation](llms.txt)

Accept online payments with cards, wallets, and 40+ local payment methods.

## Getting Started

- [Payments overview](payments.md): How Stripe payments work
- [Accept a payment](payments/quickstart.md): 10-minute integration tutorial
- [Payment Methods API](payments/payment-methods.md): Unified API for all methods
- [Payment Intents](payments/payment-intents.md): Track payment lifecycle

## Cards

- [How cards work](payments/cards/overview.md): Card payment flow explained
- [3D Secure authentication](payments/3ds.md): Strong customer authentication (SCA)
- [Save cards for later](payments/save-card.md): Store payment methods securely
- [Decline handling](declines.md): Reduce decline rates

## Wallets

- [Apple Pay](payments/apple-pay.md): Accept Apple Pay on web and iOS
- [Google Pay](payments/google-pay.md): Accept Google Pay on web and Android
- [Link](payments/link.md): Stripe's one-click checkout wallet
- [PayPal](payments/paypal.md): Accept PayPal payments

## Buy Now, Pay Later

- [Affirm](payments/affirm.md): US/Canada installment payments
- [Afterpay/Clearpay](payments/afterpay-clearpay.md): AU/UK/US installments
- [Klarna](payments/klarna.md): EU/UK/US flexible payments

## Bank Payments

- [ACH Direct Debit](payments/ach-debit.md): US bank transfers
- [SEPA Direct Debit](payments/sepa-debit.md): EU bank transfers
- [Bacs Direct Debit](payments/bacs-debit.md): UK bank transfers
- [Bank redirects](payments/bank-redirects.md): iDEAL, Bancontact, etc.

## Regional Methods

- [Alipay](payments/alipay.md): China's leading digital wallet
- [WeChat Pay](payments/wechat-pay.md): Chinese mobile payments
- [GrabPay](payments/grabpay.md): Singapore and Malaysia
- [UPI](payments/upi.md): India's instant payment system
- [All payment methods](payments/payment-methods/overview.md): Full list by region

## Advanced

- [Dynamic payment methods](payments/dynamic-payment-methods.md): Auto-show relevant methods
- [Manual capture](payments/capture.md): Authorize now, capture later
- [Multi-currency](payments/currencies.md): Charge in customer's local currency
- [Payment orchestration](payments/orchestration.md): Route payments across processors
```

---

## Applying This to .NET Docs

### Proposed Structure

```
docs/
├── llms.txt                          # Root index (45 lines)
├── llms-getting-started.txt          # Beginners (50 lines)
├── llms-web.txt                      # Web development (50 lines)
├── llms-desktop-mobile.txt           # Desktop & mobile (40 lines)
├── llms-cloud.txt                    # Cloud, Azure, microservices (50 lines)
├── llms-data-ai.txt                  # Data access, ML, AI (45 lines)
├── llms-languages.txt                # C#, F#, VB guides (50 lines)
├── llms-fundamentals.txt             # Core concepts (50 lines)
└── llms-tools.txt                    # CLI, deployment, debugging (50 lines)
```

**Total: 9 files, ~430 lines** (vs 100-line single file or 28K-line toc.yml dump)

---

## Root: docs/llms.txt (45 lines)

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

---

## Topic File: llms-getting-started.txt (50 lines)

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

---

## Topic File: llms-web.txt (50 lines)

```markdown
# Web Development with .NET

> Parent: [.NET Documentation](llms.txt)

Build modern web applications, APIs, and real-time services with ASP.NET Core.

## Overview

- [ASP.NET Core overview](https://learn.microsoft.com/aspnet/core): Modern web framework
- [What's new in ASP.NET 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0): Latest features
- [Choose your app type](https://learn.microsoft.com/aspnet/core/introduction): MVC, Razor Pages, Blazor, or API

## Getting Started

- [Build a web app (MVC)](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app): Model-View-Controller tutorial
- [Build a web API](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api): REST API tutorial
- [Razor Pages tutorial](https://learn.microsoft.com/aspnet/core/tutorials/razor-pages): Page-based web apps
- [Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis): Lightweight HTTP APIs

## Blazor (Interactive UI)

- [Blazor overview](https://learn.microsoft.com/aspnet/core/blazor): WebAssembly and Server rendering
- [Blazor tutorial](https://learn.microsoft.com/aspnet/core/blazor/tutorials): Build interactive web UI with C#
- [Blazor components](https://learn.microsoft.com/aspnet/core/blazor/components): Reusable UI components
- [Blazor vs React/Angular](https://learn.microsoft.com/aspnet/core/blazor/hosting-models): Comparison guide

## Core Features

- [Routing](https://learn.microsoft.com/aspnet/core/fundamentals/routing): Map URLs to handlers
- [Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware): Request pipeline
- [Dependency injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection): Built-in DI container
- [Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration): App settings and options
- [Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging): Structured logging
- [Error handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling): Exception and status code pages

## Authentication & Security

- [Authentication overview](https://learn.microsoft.com/aspnet/core/security/authentication): Identity and auth schemes
- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity): User management
- [OAuth/OpenID Connect](https://learn.microsoft.com/aspnet/core/security/authentication/social): Social login providers
- [JWT tokens](https://learn.microsoft.com/aspnet/core/security/authentication/jwt): API authentication
- [Authorization policies](https://learn.microsoft.com/aspnet/core/security/authorization): Role and claim-based access
- [HTTPS and TLS](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl): Secure communications

## Data Access

- [Entity Framework Core](https://learn.microsoft.com/ef/core): ORM for databases
- [Dapper](https://github.com/DapperLib/Dapper): Micro-ORM alternative
- [Database providers](https://learn.microsoft.com/ef/core/providers): SQL Server, PostgreSQL, MySQL, SQLite

## Real-Time Communication

- [SignalR](https://learn.microsoft.com/aspnet/core/signalr): WebSocket and long-polling
- [SignalR tutorial](https://learn.microsoft.com/aspnet/core/tutorials/signalr): Real-time chat app

## Testing & Quality

- [Integration testing](https://learn.microsoft.com/aspnet/core/test/integration-tests): Test full request pipeline
- [Unit testing](core/testing/index.md): Test individual components
- [Load testing](https://learn.microsoft.com/aspnet/core/test/load-tests): Performance testing

## Deployment

- [Publish to Azure App Service](azure/migration/app-service.md): Cloud hosting
- [Containerize with Docker](core/docker/build-container.md): Docker images
- [IIS hosting](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis): Windows Server
- [Linux hosting](https://learn.microsoft.com/aspnet/core/host-and-deploy/linux-nginx): Nginx/Apache

## Performance

- [Performance best practices](https://learn.microsoft.com/aspnet/core/performance/performance-best-practices): Optimization guide
- [Caching](https://learn.microsoft.com/aspnet/core/performance/caching/overview): Response and distributed caching
- [Response compression](https://learn.microsoft.com/aspnet/core/performance/response-compression): Reduce payload size
```

---

## Design Principles

### 1. **50-Line Hard Limit**
- Forces ruthless curation
- Each file = one clear topic
- Scannable in 30 seconds
- Claude Code quality at scale

### 2. **Clear Topic Boundaries**
- NO overlap between files
- Each file answers: "What if I want to [X]?"
  - llms-web.txt → "What if I want to build web apps?"
  - llms-data-ai.txt → "What if I want to work with data?"

### 3. **Parent Linking**
```markdown
> Parent: [.NET Documentation](llms.txt)
```
- Every topic file links back to root
- Root links to all topic files
- Forms navigable graph

### 4. **No Sub-Files**
- Two levels only: root + topics
- No llms-web-blazor.txt or llms-web-api.txt
- If a topic file hits 50 lines, split into TWO topic files
- Keep hierarchy flat

### 5. **External Links Welcome**
- Link to ASP.NET Core docs directly
- Link to Entity Framework docs directly
- Treat all Microsoft Learn as one cohesive resource
- Don't say "see external docs" - just link

---

## Comparison Matrix

| Approach | Total Lines | Files | Avg Lines/File | Maintainability | LLM-Friendly |
|----------|-------------|-------|----------------|-----------------|--------------|
| **Multi-file (50-line limit)** | ~450 | 9 | 50 | ✅ Excellent | ✅ Excellent |
| Single file (100-line) | 100 | 1 | 100 | ✅ Good | ⚠️ Getting large |
| Hierarchical per-section | 5,000+ | 114 | 44 | ❌ Too many files | ❌ Overwhelming |
| Stripe approach | 20,000+ | 1 | 20,000 | ❌ Impossible | ❌ Unusable |
| Claude Code (gold standard) | 53 | 1 | 53 | ✅ Perfect | ✅ Perfect |

---

## File Naming Convention

```
llms.txt                 # Always the root
llms-{topic}.txt         # Topic files
```

**Topic names must be:**
- Lowercase with hyphens
- Single word if possible (web, cloud, tools)
- Two words max (getting-started, data-ai)
- Clear and self-explanatory
- NOT nested (no llms-web-blazor.txt)

---

## Quality Checklist (Per File)

- [ ] ≤50 lines total
- [ ] Every link has description
- [ ] Clear topic boundary (no overlap with other files)
- [ ] Parent link to llms.txt
- [ ] Section headings for organization
- [ ] Links verified (no 404s)
- [ ] Descriptions are action-oriented
- [ ] Covers 80% of common use cases for this topic
- [ ] Scannable in <30 seconds

---

## Implementation: Stripe Example

If we were Stripe team:

1. **Split 600-line llms.txt into 7 files:**
   - llms.txt (root, 40 lines)
   - llms-payments.txt (50 lines)
   - llms-checkout.txt (45 lines)
   - llms-billing.txt (50 lines)
   - llms-connect.txt (50 lines)
   - llms-fraud.txt (35 lines)
   - llms-developers.txt (50 lines)

2. **Total: 320 lines across 7 files** (down from 600 in one)

3. **Each file = one clear use case:**
   - "I want to accept payments" → llms-payments.txt
   - "I want recurring billing" → llms-billing.txt
   - "I'm a platform" → llms-connect.txt

4. **LLM can:**
   - Quickly scan root (40 lines)
   - Choose relevant topic file (50 lines)
   - Get precise answer without wading through 600 lines

