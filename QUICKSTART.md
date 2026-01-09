# Quick Start Guide

Get started with the llms.txt Synthesizer tool in under 5 minutes.

## Prerequisites

- .NET 10 SDK installed
- A directory with child `llms*.txt` files (or the test examples below)

## Installation

```bash
cd /Users/rich/git/llmstxt-docs-toc/LlmsTxtSynthesizer
dotnet build
```

## Try It Now

### 1. Create Nested Test Structure

Create a hierarchical test directory with llms.txt files at multiple levels:

```bash
# Create nested structure
mkdir -p /tmp/llms-demo/guides/tutorials
mkdir -p /tmp/llms-demo/api/advanced

# Create deep nested files
cat > /tmp/llms-demo/guides/tutorials/llms-basics.txt << 'EOF'
# Basic Tutorials

> Parent: [Guides](../llms.txt)

Learn the basics step by step.

## Getting Started

- [Installation](install.md): Install the tools
- [First project](first-project.md): Create your first project

## Reference

- [Tutorials index](index.md): All tutorials
EOF

cat > /tmp/llms-demo/api/advanced/llms-patterns.txt << 'EOF'
# Advanced Patterns

> Parent: [API Documentation](../llms.txt)

Master advanced techniques.

## Design Patterns

- [Singleton](singleton.md): Singleton pattern guide
- [Factory](factory.md): Factory pattern guide

## Reference

- [Patterns catalog](catalog.md): All patterns
EOF

# Create mid-level files
cat > /tmp/llms-demo/guides/llms-quickstart.txt << 'EOF'
# Quick Start Guide

> Parent: [Documentation Home](../llms.txt)

Get up and running in minutes.

## Installation

- [Download the SDK](install/download.md): Get the latest version
- [System requirements](install/requirements.md): Check compatibility

## Your First Project

- [Create a project](tutorials/first-project.md): Step-by-step guide
- [Run your app](tutorials/run-app.md): Build and execute

## Reference

- [CLI commands](reference/cli.md): Command-line reference
EOF

cat > /tmp/llms-demo/api/llms-reference.txt << 'EOF'
# API Reference

> Parent: [Documentation Home](../llms.txt)

Complete API documentation.

## Core APIs

- [Authentication API](api/auth.md): Secure your applications
- [Data API](api/data.md): Work with data

## Reference

- [API Browser](https://api.example.com): Browse all APIs
- [SDK Reference](sdk/reference.md): Complete SDK documentation
EOF
```

### 2. Show Tree Structure

```bash
cd /Users/rich/git/llmstxt-docs-toc/LlmsTxtSynthesizer
dotnet run -- /tmp/llms-demo --tree
```

**Output:**
```
Directory tree for: /tmp/llms-demo

  api/
    - llms-reference.txt (18 lines, 350 bytes)
    advanced/
      - llms-patterns.txt (15 lines, 300 bytes)
  guides/
    - llms-quickstart.txt (22 lines, 450 bytes)
    tutorials/
      - llms-basics.txt (15 lines, 280 bytes)

Total: 4 files in 4 directories
```

### 3. Recursive Generation (Dry-Run)

```bash
dotnet run -- /tmp/llms-demo --recursive --dry-run
```

**Output:**
```
Found 4 directories with llms*.txt files
Generating in depth-first order:

    [Depth 2] api/advanced/
      ⊘ Dry run: llms.txt (11 lines, 1 children)
    [Depth 2] guides/tutorials/
      ⊘ Dry run: llms.txt (11 lines, 1 children)
  [Depth 1] api/
    ⊘ Dry run: llms.txt (15 lines, 2 children)
  [Depth 1] guides/
    ⊘ Dry run: llms.txt (15 lines, 2 children)

[Root] llms-demo/
  ⊘ Dry run: llms.txt (24 lines, 8 children)

Summary:
  Total files generated: 5
  Mode: Dry run (no files written)
```

### 4. Recursive Generation (Actual)

```bash
dotnet run -- /tmp/llms-demo --recursive
```

This generates 5 `llms.txt` files:
- `/tmp/llms-demo/api/advanced/llms.txt` (synthesized from patterns file)
- `/tmp/llms-demo/guides/tutorials/llms.txt` (synthesized from basics file)
- `/tmp/llms-demo/api/llms.txt` (synthesized from reference + advanced)
- `/tmp/llms-demo/guides/llms.txt` (synthesized from quickstart + tutorials)
- `/tmp/llms-demo/llms.txt` (root, synthesized from all children)

### 5. View the Results

```bash
# View the root file
cat /tmp/llms-demo/llms.txt

# View a mid-level file
cat /tmp/llms-demo/api/llms.txt

# View a deep nested file
cat /tmp/llms-demo/api/advanced/llms.txt
```

## Using with Real Documentation

### For the .NET Docs Repository

Once child `llms*.txt` files are created in the docs repo:

```bash
# Show the tree structure
cd /Users/rich/git/llmstxt-docs-toc/LlmsTxtSynthesizer
dotnet run -- ~/git/docs/docs --tree

# Generate ALL llms.txt files recursively (recommended)
dotnet run -- ~/git/docs/docs --recursive

# Or discover existing llms*.txt files  
dotnet run -- ~/git/docs/docs --discover

# Or generate only the root file
dotnet run -- ~/git/docs/docs --generate --output ~/git/docs/docs/llms.txt

# Validate everything meets constraints
dotnet run -- ~/git/docs/docs --validate
```

## Common Workflows

### Daily Development

```bash
# Test recursive generation before committing
dotnet run -- ~/git/docs/docs --recursive --dry-run

# Generate all llms.txt files
dotnet run -- ~/git/docs/docs --recursive

# Validate all files meet constraints
dotnet run -- ~/git/docs/docs --validate
```

### Working with Directory Structure

```bash
# View the tree to understand the structure
dotnet run -- ~/git/docs/docs --tree

# Generate recursively for entire tree
dotnet run -- ~/git/docs/docs --recursive

# Generate just one directory (non-recursive)
dotnet run -- ~/git/docs/docs/core --generate --output ~/git/docs/docs/core/llms.txt
```

### Custom Configuration

```bash
# Generate with custom settings
dotnet run -- ~/git/docs/docs --generate \
  --title "My Documentation" \
  --summary "Custom summary text." \
  --max-lines 45 \
  --output ./llms.txt
```

### CI/CD Integration

Add to your GitHub Actions workflow:

```yaml
- name: Validate llms.txt files
  run: |
    cd LlmsTxtSynthesizer
    dotnet run -- ../docs --validate
```

## Next Steps

1. **Read the full README**: See [README.md](README.md) for complete documentation
2. **Review the implementation plan**: Check [planning/llms_txt_implementation_plan.md](planning/llms_txt_implementation_plan.md)
3. **Create your first child file**: Follow the 50-line limit and llms.txt format
4. **Set up CI validation**: Add the tool to your build pipeline

## Troubleshooting

### "No child llms*.txt files found"

- Make sure child files are in subdirectories (not the root)
- Files must match the pattern `llms*.txt`
- Check the target directory path is correct

### "File exceeds 50 line limit"

- Edit the file to stay within the limit (use `--max-lines` to check)
- Consider moving some content to a separate section or topic file
- Remove less important links

### Generated file is too long

The tool will warn if the generated root exceeds the limit. To fix:
- Reduce the number of child files
- Limit the number of links extracted from each section
- Adjust the synthesis logic in `LlmsSynthesizer.cs`

## Get Help

- Open an issue in the repository
- Check the [implementation plan](planning/llms_txt_implementation_plan.md)
- Contact the .NET Docs Team
