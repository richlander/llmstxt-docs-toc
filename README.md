# llms.txt Generator Tool

A .NET 10 command-line tool that **converts toc.yml files to llms.txt format**. Simple 1:1 conversion - each toc.yml becomes an llms.txt in the same directory.

## Overview

This tool automates toc.yml → llms.txt conversion:
- Finds all `toc.yml` files recursively
- Converts each to `llms.txt` in the same directory
- Handles nested YAML structures (5+ levels deep)
- Flattens to stay under 50-line limit

## Quick Start

```bash
cd LlmsTxtSynthesizer
dotnet build
dotnet run -- ~/git/docs/docs
```

That's it! Every directory with a toc.yml gets an llms.txt.

## What Gets Generated

**Input structure**:
```
docs/
├── toc.yml
└── core/
    └── toc.yml
```

**Output**:
```
docs/
├── toc.yml
├── llms.txt        (from toc.yml)
└── core/
    ├── toc.yml
    └── llms.txt    (from toc.yml)
```

**One file per directory, no variants.**

## Usage

### Default Mode: Convert All

```bash
dotnet run -- ~/git/docs/docs
```

Converts all toc.yml → llms.txt recursively.

### Preview Mode

```bash
dotnet run -- ~/git/docs/docs --dry-run
```

Shows what would be generated without writing files.

### View Structure

```bash
dotnet run -- ~/git/docs/docs --tree
```

Example output:
```
Directory tree for: /home/user/docs

  core/
    - llms.txt (25 lines, 890 bytes)
  advanced/
    - llms.txt (18 lines, 612 bytes)
./
  - llms.txt (30 lines, 1.1KB)

Total: 3 files in 3 directories
```

### Validate Files

```bash
dotnet run -- ~/git/docs/docs --validate
```

Example output:
```
✓ core/llms.txt (25 lines)
✓ advanced/llms.txt (18 lines)

whats-new/llms.txt:
  ✗ File exceeds 50 line limit: 53 lines
```

### Custom Configuration

```bash
dotnet run -- ~/git/docs/docs \
  --title "My Documentation" \
  --summary "Complete guide to my project" \
  --max-lines 60
```

## YAML Structure Support

The tool handles standard toc.yml patterns:

```yaml
items:
  - name: Section Name
    href: relative/path.md
    items:
      - name: Subsection
        href: another/path.md
      - name: Deep Topic
        tocHref: ./nested-toc.yml
```

Converted to:

```markdown
# Section Name

> Documentation for Section Name (N topics)

## Overview

- [Section Name](relative/path.md)

## Topics

- [Subsection](another/path.md)
- [Deep Topic](./nested-toc.yml)
```

## Command-Line Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `<target-dir>` | - | Target directory containing toc.yml files | Required |
| `--dry-run` | - | Preview without writing files | - |
| `--tree` | `-t` | Show tree structure of llms.txt files | - |
| `--validate` | `-v` | Validate all llms.txt files | - |
| `--max-lines` | - | Maximum lines per file | 50 |
| `--title` | - | Title for root llms.txt | ".NET Documentation" |
| `--summary` | - | Summary for root llms.txt | "Build applications..." |

## How It Works

1. **Find**: Locate all toc.yml files recursively
2. **Parse**: Extract items, names, hrefs from YAML
3. **Flatten**: Handle deep nesting (5+ levels) → curated links
4. **Generate**: Create llms.txt with title, summary, links
5. **Limit**: Stay under 50 lines (configurable)

### Line Budget

Per llms.txt file:
- Header: 4 lines (title + summary)
- Overview: 2-8 lines (top-level items)
- Topics: 15-30 lines (nested items)
- Total: ≤50 lines

## Features

- ✅ YAML to llms.txt conversion (1:1 mapping)
- ✅ Handles deeply nested structures
- ✅ Automatic flattening and curation
- ✅ 50-line limit enforcement
- ✅ Dry-run preview mode
- ✅ Tree visualization
- ✅ Validation mode

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

After installation, use `llms-synthesize` command directly.

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: Generate llms.txt files

on:
  push:
    paths:
      - '**/toc.yml'

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Generate llms.txt files
        run: |
          cd LlmsTxtSynthesizer
          dotnet run -- ../docs
      
      - name: Commit changes
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add '**/llms.txt'
          git commit -m "Update llms.txt files" || exit 0
          git push
```

## Project Structure

```
LlmsTxtSynthesizer/
├── LlmsTxtSynthesizer.csproj  # Project (YamlDotNet, System.CommandLine)
├── Program.cs                  # CLI entry point
├── YmlToLlmsConverter.cs       # toc.yml → llms.txt conversion
├── RecursiveGenerator.cs       # Orchestrates conversion
├── LlmsSynthesizer.cs          # File discovery (for validation)
├── LlmsParser.cs               # Parser for llms.txt format
├── LlmsFile.cs                 # Data model
└── Validator.cs                # Validation rules
```

## Design Principles

1. **One File Per Directory**: Each directory gets exactly one llms.txt
2. **Direct Conversion**: toc.yml → llms.txt (no intermediate files)
3. **50-Line Hard Limit**: Enforced through intelligent curation
4. **Radical Curation**: Extract top ~20-30 links from nested YAML
5. **Task-Oriented**: Organize by "Overview" and "Topics"

## Requirements

- .NET 10 SDK
- YamlDotNet package (included)
- System.CommandLine package (included)

## Troubleshooting

**Problem**: "Directory does not exist"
- Check that the path is correct
- Use absolute paths or ensure relative path is valid

**Problem**: "No toc.yml files found"
- Verify files are named exactly `toc.yml`
- Check they're in the target directory tree

**Problem**: File exceeds line limit
- YAML file has too many nested items
- Tool will warn but still generate
- Consider restructuring the toc.yml

## Related

- [llms.txt Specification](https://llmstxt.org)
- [SINGLE_FILE_APPROACH.md](SINGLE_FILE_APPROACH.md) - Design decisions
- [.NET Documentation](https://learn.microsoft.com/dotnet)

## License

MIT License
