#!/bin/bash
# Complete workflow demonstration

set -e

echo "=== llms.txt Synthesizer Tool - Complete Workflow Demo ==="
echo

# Step 1: Tree view
echo "Step 1: Show tree structure"
echo "Command: dotnet run -- /tmp/llms-recursive-test --tree"
echo "---"
cd LlmsTxtSynthesizer
dotnet run --no-build -- /tmp/llms-recursive-test --tree
echo

# Step 2: Validate existing files
echo "Step 2: Validate existing llms*.txt files"
echo "Command: dotnet run -- /tmp/llms-recursive-test --validate"
echo "---"
dotnet run --no-build -- /tmp/llms-recursive-test --validate
echo

# Step 3: Dry-run recursive generation
echo "Step 3: Dry-run recursive generation"
echo "Command: dotnet run -- /tmp/llms-recursive-test --recursive --dry-run"
echo "---"
dotnet run --no-build -- /tmp/llms-recursive-test --recursive --dry-run
echo

# Step 4: Actual recursive generation
echo "Step 4: Actual recursive generation"
echo "Command: dotnet run -- /tmp/llms-recursive-test --recursive"
echo "---"
# Clean up any existing llms.txt files first
find /tmp/llms-recursive-test -name "llms.txt" -delete
dotnet run --no-build -- /tmp/llms-recursive-test --recursive
echo

# Step 5: Validate generated files
echo "Step 5: Validate all files after generation"
echo "Command: dotnet run -- /tmp/llms-recursive-test --validate"
echo "---"
dotnet run --no-build -- /tmp/llms-recursive-test --validate
echo

# Step 6: Show generated structure
echo "Step 6: Show generated files"
echo "---"
find /tmp/llms-recursive-test -name "llms*.txt" -type f | sort | while read f; do
    echo "File: $f ($(wc -l < "$f") lines)"
done
echo

echo "=== Workflow complete! ==="
