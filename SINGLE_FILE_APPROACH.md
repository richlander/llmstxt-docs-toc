# Single llms.txt File Approach

## Design Change

### Previous Approach (Multi-File)
```
docs/
├── toc.yml
├── llms-docs.txt       ← Converted from toc.yml
├── llms.txt            ← Synthesized from children
└── core/
    ├── toc.yml
    ├── llms-core.txt   ← Converted from toc.yml
    └── llms.txt        ← Synthesized from children
```

**Problem**: Multiple llms files per directory, confusing hierarchy

### New Approach (Single File)
```
docs/
├── toc.yml
├── llms.txt            ← Directly converted from toc.yml
└── core/
    ├── toc.yml
    └── llms.txt        ← Directly converted from toc.yml
```

**Solution**: One llms.txt per directory, direct 1:1 conversion

## Implementation

### What Changed

1. **YmlToLlmsConverter.cs**
   - Output filename changed from `llms-{dirname}.txt` to `llms.txt`
   - Converts root toc.yml (no longer skipped)

2. **RecursiveGenerator.cs**
   - Removed synthesis step entirely
   - Only does YAML conversion now
   - No longer builds hierarchy from multiple files

3. **LlmsSynthesizer.cs**
   - Updated `DiscoverChildFiles()` to find `llms.txt` in subdirectories
   - Used by validation mode only

4. **Program.cs**
   - Updated description text

### Workflow

**Single command**:
```bash
dotnet run -- ~/git/docs/docs
```

**What it does**:
1. Find all `toc.yml` files recursively
2. Convert each `toc.yml` → `llms.txt` in same directory
3. Done!

No hierarchy synthesis, no multi-step process.

## Benefits

✅ **Simpler**: One file per directory  
✅ **Clearer**: Direct 1:1 mapping from toc.yml  
✅ **No duplicates**: Each directory has exactly one llms.txt  
✅ **No confusion**: No llms-{topic}.txt vs llms.txt distinction

## Example Output

Input structure:
```
docs/
├── toc.yml (16 items)
└── whats-new/
    └── toc.yml (20 items)
```

Output:
```
docs/
├── toc.yml
├── llms.txt (18 lines)
└── whats-new/
    ├── toc.yml
    └── llms.txt (25 lines)
```

## Validation

```bash
dotnet run -- ~/docs --validate
✓ advanced/llms.txt (7 lines)
✓ core/llms.txt (7 lines)
```

All llms.txt files are checked for:
- Line count ≤ 50
- Valid link format
