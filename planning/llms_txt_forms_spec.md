# llms.txt Output Forms Specification

This document specifies the four output forms for llms.txt files, determined by the combination of file children (markdown files) and directory children (subdirectories with their own llms.txt).

## Overview

| Form | File Children | Directory Children | Prioritized Sections |
|------|--------------|-------------------|---------------------|
| 1. Leaf | Yes | No | N/A |
| 2. Mixed | Yes | Yes | No |
| 3. Prioritized | Yes | Yes | Yes |
| 4. Index-Only | No | Yes | N/A |

---

## Form 1: Leaf Node (File Children Only)

**When:** Directory contains markdown files but no subdirectories with llms.txt files.

**Example:** `/docs/standard/serialization/system-text-json/llms.txt`

**Structure:**
```markdown
# [Title]

> [Description paragraph]

**.NET 10 adds:** [Optional: What's new callout]

- [Link Title](raw-url): Description text
- [Link Title](raw-url): Description text
...
```

**Characteristics:**
- Single flat list of links
- No section headers (except implicit)
- Links sorted alphabetically or by relevance
- Most common form for leaf directories

---

## Form 2: Mixed Children (Files + Directories, No Priority)

**When:** Directory contains both markdown files AND subdirectories, but no prioritized sections configured.

**Example:** `/docs/standard/collections/llms.txt`

**Structure:**
```markdown
# [Title]

> [Description paragraph]

**.NET 10 adds:** [Optional: What's new callout]

- [Link Title](raw-url): Description text
- [Link Title](raw-url): Description text
...

## Topic Indices

- [Subdirectory Title](subdirectory/llms.txt)
...
```

**Characteristics:**
- File links listed first (flat list)
- `## Topic Indices` section at bottom
- Directory children link to their llms.txt files
- Used when all content has equal importance

---

## Form 3: Prioritized Sections (Files + Directories with Priority)

**When:** Directory has prioritized sections configured, promoting certain topics/subdirectories.

**Example:** `/docs/standard/serialization/llms.txt`

**Structure:**
```markdown
# [Title]

> [Description paragraph]

## [Priority Section 1 Title]

[Section description paragraph]

- [Section 1 Index](section1/llms.txt): Complete index with 16 topics
- [Curated Link 1](raw-url): Description
- [Curated Link 2](raw-url): Description

## [Priority Section 2 Title]

[Section description paragraph]

- [Section 2 Index](section2/llms.txt): Complete index with 8 topics
- [Curated Link 1](raw-url): Description

## Other Topics

- [Remaining Link 1](raw-url): Description
- [Remaining Link 2](raw-url): Description
...

## Topic Indices

- [Remaining Subdirectory](subdirectory/llms.txt): Short description
...
```

**Characteristics:**
- Prioritized sections appear first with their own `##` headers
- Each prioritized section has: description + **index link first** + curated links
- `## Other Topics` contains remaining file children
- `## Topic Indices` contains remaining directory children
- Most complex form, used for major topic areas

---

## Form 4: Index-Only (Directory Children Only)

**When:** Directory contains ONLY subdirectories (no direct markdown file children).

**Example:** `/docs/standard/data/llms.txt`

**Structure:**
```markdown
# [Title]

> [Description paragraph]

## Topic Indices

- [Subdirectory 1 Title](subdirectory1/llms.txt): Short description
- [Subdirectory 2 Title](subdirectory2/llms.txt): Short description
```

**Characteristics:**
- Single `## Topic Indices` section
- Each subdirectory listed with title, link, and short description
- Mirrors Form 2's directory section (just without the file links above)
- Used for organizational/hub directories

---

## Decision Logic

```
Has file children?
├── Yes
│   └── Has directory children?
│       ├── Yes
│       │   └── Has prioritized sections?
│       │       ├── Yes → Form 3 (Prioritized)
│       │       └── No  → Form 2 (Mixed)
│       └── No  → Form 1 (Leaf)
└── No
    └── Has directory children?
        ├── Yes → Form 4 (Index-Only)
        └── No  → (Empty directory, no llms.txt generated)
```

---

## Common Elements

### Header Block
All forms start with:
```markdown
# [Title]

> [Description paragraph]
```

The title comes from the directory's primary document or configured name.
The description is extracted from the overview/index document.

### What's New Callout (Optional)
```markdown
**.NET 10 adds:** [Feature summary]
```
Only included when new features are documented for the current release.

### Link Format
```markdown
- [Title](raw-github-url): Description extracted from document
```

### Index Link Format

Index links should be the **first link** in their respective lists and follow this format:

`[title](url): N topics[, M in tree][; description]`

- **N topics**: Number of topics directly in the linked file
- **M in tree**: Total topics reachable recursively (only when significantly larger than N)
- **description**: Short contextual description (optional, separated by semicolon)

**Prioritized section index (first in section's link list):**
```markdown
- [C# Language Index](csharp/llms.txt): 45 topics, 1139 in tree
```
No description needed—section header and description paragraph provide context.

**Extended index (first in Topic Indices section):**
```markdown
- [.NET Standard Library Extended Index](llms-extended.txt): 20 additional topics
```
No tree count (flat overflow file), no description needed.

**Child directory index (in Topic Indices section):**
```markdown
- [Azure for .NET](azure/llms.txt): 12 topics, 87 in tree; Azure SDK, authentication, and cloud integration.
- [Serialization](serialization/llms.txt): 8 topics; Convert objects to JSON, XML, or binary formats.
```
Metrics first, then semicolon, then short description.

---

## Policy Considerations

### Prioritized Sections vs Topic Indices

Directories that appear as prioritized sections (Form 3) are **not repeated** in the Topic Indices section. This avoids redundancy since the prioritized section already surfaces the topic prominently with curated links. Topic Indices contains "the rest"—topics that weren't promoted to their own section.

---

## Section Naming Conventions

| Scenario | Section Name |
|----------|-------------|
| Directory children (no priority) | `## Topic Indices` |
| Directory children only (Form 4) | `## Topic Indices` |
| Remaining directories (with priority) | `## Topic Indices` |
| Remaining files (with priority) | `## Other Topics` |
