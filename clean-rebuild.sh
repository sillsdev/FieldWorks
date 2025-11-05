#!/bin/bash
# Force clean rebuild after migration fixes

echo "=========================================="
echo "Cleaning all build artifacts..."
echo "=========================================="

# Remove all obj and bin directories
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true

# Remove Output directories
rm -rf Output

# Remove packages cache
rm -rf ~/.nuget/packages -ErrorAction SilentlyContinue || true

echo "✓ Clean complete"
echo ""

echo "=========================================="
echo "Restoring packages..."
echo "=========================================="

# Restore with nuget
nuget restore FieldWorks.sln -NonInteractive -Verbosity quiet

echo "✓ Package restore complete"
echo ""

echo "=========================================="
echo "Building solution..."
echo "=========================================="

# Build with detailed error logging
msbuild FieldWorks.sln \
  /m \
  /p:Configuration=Debug \
  /verbosity:normal \
  /flp1:LogFile=build.log;Verbosity=detailed

BUILD_RESULT=$?

echo ""
echo "=========================================="
if [ $BUILD_RESULT -eq 0 ]; then
  echo "✓ BUILD SUCCESSFUL!"
else
  echo "✗ BUILD FAILED"
  echo ""
  echo "Error summary from build.log:"
  grep -E "error CS|error NU" build.log | head -20
fi
echo "=========================================="

exit $BUILD_RESULT
