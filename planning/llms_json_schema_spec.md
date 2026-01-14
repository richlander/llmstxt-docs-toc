# _llms.json Schema Specification

This document specifies the schema for `_llms.json` customization files that control llms.txt generation.

## Overview

`_llms.json` files can be placed in any directory to customize how that directory's llms.txt is generated. Settings apply to the directory containing the file.

## Schema

```json
{
  "title": "string",
  "description": "string",
  "shortDescription": "string",
  "preamble": "string",
  "guidance": { ... },
  "offers": ["string"],
  "sections": [{ ... }],
  "nodes": { ... },
  "filter": ["string"],
  "promote": [{ ... }],
  "related": [{ ... }]
}
```

## Properties

### `title` (string, optional)
Override the section/document title. Falls back to directory name converted to title case.

### `description` (string, optional)
Description text rendered as a blockquote under the H1 heading. Should summarize the directory's content.

### `shortDescription` (string, optional)
Brief description for use in link text when this directory appears in a parent's Topic Indices section. Keep concise (one line).

### `preamble` (string, optional)
Important preamble text (warnings, cautions) rendered prominently after the description.

### `guidance` (object, optional)
Structured guidance section for AI assistants.

```json
{
  "title": "Guidance for AI Assistants",
  "intro": "When helping users with this topic:",
  "items": [
    "**Recommendation 1**: Details...",
    "**Recommendation 2**: Details..."
  ]
}
```

### `offers` (array of strings, optional)
Ordered list of child file/directory names to offer to parent directories. First = most important. Parents may pull these into their llms.txt based on priority settings.

```json
{
  "offers": ["overview", "getting-started", "api-reference"]
}
```

### `sections` (array of SectionDefinition, optional)
Define custom sections for the llms.txt output. Two modes based on `path` vs `name`:

#### Mode 1: Reference a child directory (`path`)

```json
{
  "path": "ai",
  "priority": 90,
  "include": ["microsoft-extensions-ai", "ichatclient", "get-started-mcp"]
}
```

- `path`: Relative path to a child directory
- `priority`: Sort order (higher = appears first). Also controls how many offers to pull (100 = all, 50 = half)
- `include`: Items to include, **relative to the child path** (e.g., "overview" → "ai/overview")
- Title and description come from the child's `_llms.json`
- Automatically adds a link to the child's llms.txt as an index

#### Mode 2: Create a standalone section (`name`)

```json
{
  "name": "What's New",
  "priority": 100,
  "include": [
    "core/whats-new/dotnet-10/overview",
    "core/compatibility/10.0/10.0"
  ]
}
```

- `name`: Section heading text
- `priority`: Sort order (higher = appears first)
- `include`: Items to include, as **absolute paths from repo root**
- No automatic index link added

### `nodes` (object, optional)
Per-node overrides keyed by relative path.

```json
{
  "nodes": {
    "some-file": {
      "rename": "Better Title",
      "description": "Custom description for this file"
    }
  }
}
```

### `filter` (array of strings, optional)
Paths to exclude from output.

```json
{
  "filter": ["internal-only", "deprecated-api"]
}
```

### `promote` (array of PromoteRule, optional)
Nodes to promote up the hierarchy.

```json
{
  "promote": [
    { "path": "deeply/nested/important-file", "levels": 2 }
  ]
}
```

### `related` (array of RelatedTopic, optional)
Cross-references to related topics.

```json
{
  "related": [
    {
      "path": "other-section",
      "weight": 0.8,
      "keywords": ["related", "see-also"],
      "reason": "Complementary topic"
    }
  ]
}
```

---

## Section Definition Details

### The `path` vs `name` Pivot

The key distinction in section definitions is whether you use `path` or `name`:

| Property | `path` (child reference) | `name` (standalone section) |
|----------|-------------------------|----------------------------|
| Title source | Child's `_llms.json` title | Explicit `name` value |
| Description source | Child's `_llms.json` description | None |
| `include` paths | Relative to child directory | Absolute from repo root |
| Index link | Auto-added to child's llms.txt | None |
| Use case | Surface content from subdirectory | Create ad-hoc grouping |

### Examples

**Reference a child directory and surface specific items:**
```json
{
  "sections": [
    {
      "path": "serialization/system-text-json",
      "priority": 80,
      "include": ["overview", "how-to", "migrate-from-newtonsoft"]
    }
  ]
}
```
Generated output:
```markdown
## System.Text.Json

High-performance JSON serialization...

- [Serialize and deserialize JSON](url): Overview...
- [How to serialize JSON](url): Learn how...
- [Migrate from Newtonsoft.Json](url): Migration guide...
- [System.Text.Json Documentation Index](system-text-json/llms.txt)
```

**Create a standalone section from arbitrary paths:**
```json
{
  "sections": [
    {
      "name": "Migration Guides",
      "priority": 60,
      "include": [
        "core/porting/index",
        "serialization/binaryformatter-migration-guide/index",
        "core/compatibility/fx-core"
      ]
    }
  ]
}
```
Generated output:
```markdown
## Migration Guides

- [Port from .NET Framework](url): Upgrade legacy apps...
- [BinaryFormatter Migration](url): Migrate from obsolete...
- [Breaking changes](url): .NET Framework to .NET Core...
```

---

## Title Resolution

When a directory is referenced (in `include`, Topic Indices, etc.), titles are resolved in this order:

1. **Child's `_llms.json` `title`** - Preferred, explicit control
2. **Directory name converted to title case** - Fallback

If a directory appears with a poor title (like "10.0" instead of "Breaking changes in .NET 10"), add a `_llms.json` file to that directory with the proper `title`.

---

## File Placement

Place `_llms.json` files in the directory they customize:

```
docs/
├── _llms.json              # Customizes docs/llms.txt
├── standard/
│   ├── _llms.json          # Customizes docs/standard/llms.txt
│   └── serialization/
│       ├── _llms.json      # Customizes docs/standard/serialization/llms.txt
│       └── system-text-json/
│           └── _llms.json  # Customizes this leaf directory
```
