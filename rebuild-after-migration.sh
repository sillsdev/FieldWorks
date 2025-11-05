#!/bin/bash
# Script to clean build artifacts and rebuild after .NET 4.8 migration fixes

set -e

echo "=========================================="
echo "Cleaning old build artifacts..."
echo "=========================================="

# Clean output directories
rm -rf Output/Debug
rm -rf Output/Release
rm -rf Output/x64

# Clean obj directories throughout the repo
find Src -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
find Lib -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

echo "✓ Cleaned build artifacts"
echo ""

echo "=========================================="
echo "Restoring packages..."
echo "=========================================="
nuget restore FieldWorks.sln

echo "✓ NuGet restore complete"
echo ""

echo "=========================================="
echo "Building solution (Debug configuration)..."
echo "=========================================="
msbuild FieldWorks.sln /m /p:Configuration=Debug /t:Rebuild

echo ""
echo "✓ Build complete!"
echo ""
echo "If errors remain, check:"
echo "  1. Namespace collisions (CS0118 for 'IText' in xWorksTests)"
echo "  2. Missing package references (CS0234)"
echo "  3. Type conflicts (CS0436 - might need test file exclusions)"
