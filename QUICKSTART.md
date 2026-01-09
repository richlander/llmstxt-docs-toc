# Quick Start Guide

## Installation

```bash
cd LlmsTxtSynthesizer
dotnet build
```

## Basic Usage

Convert all toc.yml files to llms.txt:

```bash
dotnet run -- ~/git/docs/docs
```

**That's it!** Every directory with a toc.yml gets an llms.txt.

## What Happens

**Before**:
```
docs/
├── toc.yml
└── core/
    └── toc.yml
```

**After**:
```
docs/
├── toc.yml
├── llms.txt        ← Generated
└── core/
    ├── toc.yml
    └── llms.txt    ← Generated
```

## Preview First

```bash
dotnet run -- ~/git/docs/docs --dry-run
```

Output:
```
Found 2 toc.yml files

Processing: toc.yml
  ⊘ Would create: llms.txt
Processing: core/toc.yml
  ⊘ Would create: core/llms.txt

Converted 2/2 files
```

## View Structure

```bash
dotnet run -- ~/git/docs/docs --tree
```

## Validate Files

```bash
dotnet run -- ~/git/docs/docs --validate
```

## Full Example

```bash
# 1. Build
cd LlmsTxtSynthesizer
dotnet build

# 2. Generate
dotnet run -- ~/git/docs/docs

# 3. Check output
cat ~/git/docs/docs/llms.txt
```

## Next Steps

See [README.md](README.md) for complete documentation.
