# llms.txt Synthesizer Tool

A .NET 10 command-line tool that synthesizes a root `llms.txt` file from child `llms*.txt` files discovered in a directory tree. Built to support the llms.txt implementation plan for .NET documentation.

## Overview

This tool helps maintain a curated, hierarchical `llms.txt` navigation system by:

1. **Discovering** all `llms*.txt` files in subdirectories
2. **Parsing** their content to extract sections, links, and metadata
3. **Synthesizing** a root `llms.txt` file that curates the most important content
4. **Validating** that all files meet the 50-line limit constraint

## Features

- ✅ Recursive discovery of child `llms*.txt` files
- ✅ **Depth-first recursive generation** of llms.txt throughout directory tree
- ✅ Markdown parsing with support for headings, links, and blockquotes
- ✅ Automatic extraction of title, summary, and parent links
- ✅ Curated synthesis of Quick Start, Common Tasks, and Reference sections
- ✅ 50-line limit validation (configurable)
- ✅ Link format validation
- ✅ Tree view of all llms*.txt files in directory structure
- ✅ Dry-run mode for testing without writing files
- ✅ Support for custom titles and summaries

## Installation

### Build from Source

```bash
cd LlmsTxtSynthesizer
dotnet build
```

### Install as Global Tool

```bash
cd LlmsTxtSynthesizer
dotnet pack
dotnet tool install --global --add-source ./nupkg LlmsTxtSynthesizer
```

After installation, use the `llms-synthesize` command directly.

## Usage

### Show Tree Structure

View all `llms*.txt` files in a directory tree:

```bash
dotnet run -- ~/git/docs/docs --tree
```

Example output:
```
Directory tree for: /home/user/docs

.../
  - llms-getting-started.txt (48 lines, 1.2KB)
core/
  - llms-fundamentals.txt (50 lines, 1.5KB)
aspnet/
  - llms-web.txt (45 lines, 1.3KB)
  api/
    - llms-webapi.txt (42 lines, 1.1KB)

Total: 4 files in 3 directories
```

### Recursive Generation (Depth-First)

**NEW**: Automatically generate `llms.txt` files throughout the entire directory tree, starting from the deepest directories and working up to the root:

```bash
# Generate all llms.txt files recursively
dotnet run -- ~/git/docs/docs --recursive

# Dry-run mode (see what would be generated without writing files)
dotnet run -- ~/git/docs/docs --recursive --dry-run
```

This will:
1. Find all directories containing `llms*.txt` files (excluding `llms.txt` itself)
2. Generate `llms.txt` for each directory, starting from deepest first
3. Each generated `llms.txt` synthesizes content from its child files
4. Finally generates the root `llms.txt` that references all subdirectory files

Example output:
```
Found 4 directories with llms*.txt files
Generating in depth-first order:

    [Depth 2] api/advanced/
      ✓ Generated: llms.txt (11 lines, 1 children)
    [Depth 2] guides/tutorials/
      ✓ Generated: llms.txt (11 lines, 1 children)
  [Depth 1] api/
    ✓ Generated: llms.txt (15 lines, 2 children)
  [Depth 1] guides/
    ✓ Generated: llms.txt (15 lines, 2 children)

[Root] docs/
  ✓ Generated: llms.txt (24 lines, 8 children)

Summary:
  Total files generated: 5
  Root file: /home/user/docs/llms.txt
```

### Discover Child Files

Find all `llms*.txt` files in subdirectories:

```bash
dotnet run -- ~/git/docs/docs --discover
```

Example output:
```
Found 9 child llms*.txt files:
  - core/llms-getting-started.txt
  - aspnet/llms-web.txt
  - maui/llms-desktop-mobile.txt
  ...
```

### Validate Files

Check that all files meet the 50-line limit and have valid link formats:

```bash
dotnet run -- ~/git/docs/docs --validate
```

Example output:
```
✓ core/llms-getting-started.txt (48 lines)
✓ aspnet/llms-web.txt (50 lines)

maui/llms-desktop-mobile.txt:
  ✗ File exceeds 50 line limit: 53 lines
```

### Generate Root llms.txt

Synthesize a root `llms.txt` from all child files:

```bash
# Output to stdout
dotnet run -- ~/git/docs/docs --generate

# Output to file
dotnet run -- ~/git/docs/docs --generate --output ~/git/docs/docs/llms.txt
```

### Custom Configuration

```bash
dotnet run -- ~/git/docs/docs --generate \
  --title "My Project Documentation" \
  --summary "Build amazing things with our platform." \
  --max-lines 60 \
  --output ./llms.txt
```

## How It Works

### 1. Discovery Phase

The tool recursively searches the target directory for files matching `llms*.txt`, excluding the root `llms.txt` in the target directory itself.

### 2. Parsing Phase

Each child file is parsed to extract:
- **Title**: First level-1 heading (`# Title`)
- **Summary**: Blockquote content (`> Summary text`)
- **Parent Link**: Blockquote starting with `Parent:` (`> Parent: [Link](url)`)
- **Sections**: Level-2 headings with their associated links
- **Links**: Markdown links in the format `- [Description](url)` or `- [Title](url): Description`

### 3. Synthesis Phase

The root file is generated with the following structure:

```markdown
# {Title}

> {Summary}

## Quick Start
{Top 4 links from llms-getting-started.txt if present}

## By Topic (Detailed Guides)
{Links to all child llms*.txt files with their summaries}

## Common Tasks
{Top 6 links from "Common Tasks" sections across all files}

## Reference
{Top 4 links from "Reference" sections across all files}
```

### 4. Validation

The tool validates:
- Line count is ≤ max-lines (default: 50)
- Link format matches the pattern: `- [text](url)` or `- [text](url): description`

## Example Input/Output

### Input: Child File

**File**: `subdir/llms-getting-started.txt`

```markdown
# Getting Started with .NET

> Parent: [.NET Documentation](../llms.txt)

Learn .NET from scratch, install tools, and build your first applications.

## Absolute Beginners

- [What is .NET?](core/introduction.md): Platform overview and key concepts
- [Install .NET](core/install/windows.md): Download SDK for Windows, Mac, or Linux

## Your First Apps

- [Console app tutorial](core/get-started.md): Hello World in 5 minutes
- [Web app tutorial](aspnet/core/tutorials/first-mvc-app): Build a web app
```

### Output: Generated Root File

```markdown
# .NET Documentation

> Build applications for any platform with C#, F#, and Visual Basic.

## Quick Start

- [What is .NET?: Platform overview and key concepts](core/introduction.md)
- [Install .NET: Download SDK for Windows, Mac, or Linux](core/install/windows.md)
- [Console app tutorial: Hello World in 5 minutes](core/get-started.md)
- [Web app tutorial: Build a web app](aspnet/core/tutorials/first-mvc-app)

## By Topic (Detailed Guides)

- [Getting Started with .NET](subdir/llms-getting-started.txt): Learn .NET from scratch, install tools, and build your first applications.

## Common Tasks

- [Console app tutorial: Hello World in 5 minutes](core/get-started.md)
- [Web app tutorial: Build a web app](aspnet/core/tutorials/first-mvc-app)

## Reference

...
```

## Command-Line Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `<target-dir>` | - | Target directory containing llms*.txt files | Required |
| `--recursive` | `-r` | **Recursively generate llms.txt files depth-first** | - |
| `--tree` | `-t` | **Show tree structure of all llms*.txt files** | - |
| `--generate` | `-g` | Generate root llms.txt file | - |
| `--validate` | `-v` | Validate all llms*.txt files | - |
| `--discover` | `-d` | Discover and list child llms*.txt files | - |
| `--dry-run` | - | **Show what would be generated without writing files** | - |
| `--output` | `-o` | Output file for generated content | stdout |
| `--max-lines` | - | Maximum lines per file | 50 |
| `--title` | - | Title for root llms.txt | ".NET Documentation" |
| `--summary` | - | Summary for root llms.txt | "Build applications for any platform with C#, F#, and Visual Basic." |

## Integration with CI/CD

### GitHub Actions Example

Add validation to your docs repository:

```yaml
name: Validate llms.txt files

on:
  pull_request:
    paths:
      - '**/llms*.txt'

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Build tool
        run: dotnet build LlmsTxtSynthesizer/LlmsTxtSynthesizer.csproj
      
      - name: Validate llms.txt files
        run: |
          cd LlmsTxtSynthesizer
          dotnet run -- ../docs --validate
```

### Pre-commit Hook

```bash
#!/bin/bash
# .git/hooks/pre-commit

cd LlmsTxtSynthesizer
dotnet run -- ../docs --validate

if [ $? -ne 0 ]; then
  echo "❌ llms.txt validation failed"
  exit 1
fi

echo "✅ llms.txt validation passed"
```

## Project Structure

```
LlmsTxtSynthesizer/
├── LlmsTxtSynthesizer.csproj  # Project file with System.CommandLine
├── Program.cs                  # Main entry point with CLI setup
├── LlmsFile.cs                 # Data model for llms.txt files
├── LlmsParser.cs               # Parser for llms.txt format
├── LlmsSynthesizer.cs          # Core synthesis logic (single directory)
├── RecursiveGenerator.cs       # Recursive depth-first tree generation
└── Validator.cs                # Validation rules
```

## Design Principles

Following the implementation plan:

1. **50-Line Hard Limit**: Enforced through validation
2. **Radical Curation**: Synthesizer extracts only top links from sections
3. **Task-Oriented**: Organizes by "Common Tasks" and "Quick Start"
4. **Two-Level Hierarchy**: Root + topic files (no deeper nesting)
5. **Parent Linking**: Parsed from child files for navigation

## Requirements

- .NET 10 SDK
- System.CommandLine package (included)

## Contributing

When adding features, maintain these principles:

1. Keep the synthesized output under 50 lines
2. Prefer quality over quantity in link extraction
3. Make curation logic configurable but opinionated by default
4. Validate all output against the llms.txt specification

## License

MIT License - See LICENSE file

## Related

- [llms.txt Specification](https://llmstxt.org)
- [Implementation Plan](planning/llms_txt_implementation_plan.md)
- [.NET Documentation](https://learn.microsoft.com/dotnet)

## Support

For issues or questions:
1. Check the implementation plan documentation
2. Open an issue in the repository
3. Contact the .NET Docs Team
