# Description Extraction Feature

## Overview

The tool now extracts descriptions from markdown file frontmatter and includes them in the generated llms.txt files, matching the Claude Code llms.txt format.

## Implementation

### Detection

The tool reads the YAML frontmatter from each referenced markdown file and extracts the `description:` field.

### Format

**Markdown frontmatter**:
```yaml
---
title: "Interoperating with unmanaged code"
description: Review interoperation with unmanaged code. The CLR conceals from clients and servers how the object models of .NET components and unmanaged code differ.
ms.date: "01/17/2018"
---
```

**Generated llms.txt**:
```markdown
- [Interoperate with unmanaged code](index.md): Review interoperation with unmanaged code. The CLR conceals from clients and servers how the object models of .NET components and unmanaged code differ.
```

## Example Output

**Before** (without descriptions):
```markdown
# Interop

> Documentation for Interop (30 topics)

## Overview

- [Interoperate with unmanaged code](index.md)
- [Expose COM components to .NET Framework](exposing-com-components.md)
```

**After** (with descriptions):
```markdown
# Interop

> Documentation for Interop (30 topics)

## Overview

- [Interoperate with unmanaged code](index.md): Review interoperation with unmanaged code. The CLR conceals from clients and servers how the object models of .NET components and unmanaged code differ.
- [Expose COM components to .NET Framework](exposing-com-components.md): Know the process of exposing COM components to .NET. COM components are valuable in managed code as middle-tier business applications or isolated functionality.
```

## Comparison with Claude Code

The output format now matches Claude Code's llms.txt:
- https://code.claude.com/docs/llms.txt

Both provide:
- Link with title
- Colon separator
- Description from source document

## Technical Details

### File: `YmlToLlmsConverter.cs`

**New Methods**:
- `ExtractDescriptionFromMarkdown(url, baseDir)` - Reads markdown frontmatter
- `FormatLink(title, url, baseDir)` - Formats link with description

**Process**:
1. Parse toc.yml to get href links
2. For each .md file, read first ~30 lines
3. Find YAML frontmatter between `---` markers
4. Extract `description:` field value
5. Append to link format: `- [Title](url): Description`

### Handling

- **Relative paths**: Resolved using baseDir from toc.yml location
- **Query parameters**: Stripped (`?toc=...`)
- **Fragments**: Stripped (`#section`)
- **Non-markdown**: Skipped (no description)
- **Missing files**: Gracefully handled (no description added)
- **Missing descriptions**: Link without description

## Benefits

✅ **Richer context**: Users understand what each link is about  
✅ **Better discoverability**: Descriptions help users find relevant content  
✅ **Professional format**: Matches industry best practices (Claude Code)  
✅ **Automatic**: No manual description writing needed  
✅ **Source of truth**: Uses existing frontmatter from docs  

## Testing

Tested with .NET docs:
```bash
dotnet run -- /home/rich/git/docs/docs/framework/interop
```

Result: 39-line llms.txt with rich descriptions for all 30 topics.
