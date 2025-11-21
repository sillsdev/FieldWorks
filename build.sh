#!/usr/bin/env bash
set -euo pipefail

# NOTE: This script is primarily intended for use in Git Bash or MSYS2 on Windows.
# While it can be run on Linux (if msbuild/dotnet is available), the native C++ components
# and Visual Studio environment setup are Windows-specific.
# For Linux builds, ensure you have the Mono or .NET SDK environment configured manually.

CONFIGURATION="Debug"
PLATFORM="x64"
LOG_FILE=""
SERIAL=false
BUILD_TESTS=false
BUILD_ADDITIONAL_APPS=false
VERBOSITY="minimal"
NODE_REUSE="true"
MSBUILD_ARGS=()

print_usage() {
  cat <<'EOF'
Usage: build.sh [options] [-- additional msbuild arguments]

Builds FieldWorks using the MSBuild Traversal SDK (FieldWorks.proj).

This script performs:
  1. Locates MSBuild (Visual Studio 2022/2019/2017 or from PATH)
  2. Sets architecture environment variable for legacy tasks
  3. NuGet package restoration
  4. Full traversal build via FieldWorks.proj

Options:
  -c, --configuration CONFIG      Build configuration (default: Debug)
  -p, --platform PLATFORM         Build platform (default: x64, x86 not supported)
  -s, --serial                    Disable parallel build (default: parallel enabled)
  -t, --build-tests               Include test projects (default: false)
  -a, --build-additional-apps     Include optional utility apps (default: false)
  -v, --verbosity LEVEL           Logging verbosity: quiet, minimal, normal, detailed, diagnostic (default: minimal)
  --no-node-reuse                 Disable MSBuild node reuse (default: enabled)
  -l, --log-file FILE             Tee final msbuild output to FILE
  -h, --help                      Show this help message

Any arguments after "--" are passed directly to msbuild.

Examples:
  ./build.sh                                    # Build Debug x64 parallel, minimal log
  ./build.sh -c Release -t                      # Build Release x64 with tests
  ./build.sh -s -v detailed                     # Build serial with detailed log
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -c|--configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    -p|--platform)
      PLATFORM="$2"
      if [[ "$PLATFORM" == "x86" ]]; then
        echo "ERROR: x86 build is no longer supported." >&2
        exit 1
      fi
      shift 2
      ;;
    -s|--serial)
      SERIAL=true
      shift
      ;;
    -t|--build-tests)
      BUILD_TESTS=true
      shift
      ;;
    -a|--build-additional-apps)
      BUILD_ADDITIONAL_APPS=true
      shift
      ;;
    -v|--verbosity)
      VERBOSITY="$2"
      shift 2
      ;;
    --no-node-reuse)
      NODE_REUSE="false"
      shift
      ;;
    -l|--log-file)
      LOG_FILE="$2"
      shift 2
      ;;
    -h|--help)
      print_usage
      exit 0
      ;;
    --)
      shift
      MSBUILD_ARGS+=("$@")
      break
      ;;
    *)
      MSBUILD_ARGS+=("$1")
      shift
      ;;
  esac
done

# Find MSBuild - check Visual Studio installations and PATH
find_msbuild() {
  local msbuild_path=""

  # Try common Visual Studio installation paths first
  local vs_versions=(2022 2019 2017)
  local vs_editions=(Community Professional Enterprise)

  for version in "${vs_versions[@]}"; do
    for edition in "${vs_editions[@]}"; do
      local candidate="/c/Program Files/Microsoft Visual Studio/$version/$edition/MSBuild/Current/Bin/MSBuild.exe"
      if [[ -f "$candidate" ]]; then
        msbuild_path="$candidate"
        echo "Found MSBuild: $candidate" >&2
        echo "$msbuild_path"
        return 0
      fi
    done
  done

  # Try finding msbuild in PATH
  if command -v msbuild.exe &>/dev/null; then
    msbuild_path=$(command -v msbuild.exe)
    echo "Found MSBuild in PATH: $msbuild_path" >&2
    echo "$msbuild_path"
    return 0
  fi

  # Last resort - try the one that comes with VS Build Tools
  if [[ -f "/c/Program Files (x86)/Microsoft Visual Studio/2019/BuildTools/MSBuild/Current/Bin/MSBuild.exe" ]]; then
    msbuild_path="/c/Program Files (x86)/Microsoft Visual Studio/2019/BuildTools/MSBuild/Current/Bin/MSBuild.exe"
    echo "Found MSBuild: $msbuild_path" >&2
    echo "$msbuild_path"
    return 0
  fi

  echo "ERROR: Could not find MSBuild.exe. Please install Visual Studio or run from a Developer Command Prompt." >&2
  exit 1
}

# Set architecture environment variable for legacy MSBuild tasks
set_arch_environment() {
  local platform="$1"
  if [[ "${platform,,}" == "x86" ]]; then
    echo "ERROR: x86 build is no longer supported." >&2
    exit 1
  fi
  export arch="x64"
  echo "Set arch environment variable to: $arch"
}

# Check Visual Studio Developer Environment
check_vs_environment() {
  # Check if already initialized
  if [[ -n "${VCINSTALLDIR:-}" ]]; then
    echo "✓ Visual Studio Developer environment detected"
    return 0
  fi

  # Not Windows? Skip (likely CI on Linux)
  if [[ ! "$OSTYPE" =~ "msys" && ! "$OSTYPE" =~ "cygwin" ]]; then
    return 0
  fi

  # Find if Visual Studio is installed
  local vs_versions=(2022 2019 2017)
  local vs_editions=(Community Professional Enterprise BuildTools)
  local vs_found=""

  for version in "${vs_versions[@]}"; do
    for edition in "${vs_editions[@]}"; do
      if [[ -d "/c/Program Files/Microsoft Visual Studio/$version/$edition" ]]; then
        vs_found="$version $edition"
        break 2
      fi
    done
  done

  echo ""
  echo "❌ ERROR: Visual Studio Developer environment not initialized" >&2
  echo "" >&2

  if [[ -n "$vs_found" ]]; then
    echo "   Visual Studio $vs_found is installed, but the environment is not set up." >&2
    echo "" >&2
    echo "   Please run this script from a Developer Command Prompt:" >&2
    echo "" >&2
    echo "   1. Press Windows key and search for 'Developer Command Prompt for VS'" >&2
    echo "   2. Open 'Developer Command Prompt for VS 2022' (or your VS version)" >&2
    echo "   3. Navigate to: cd '$PWD'" >&2
    echo "   4. Run: ./build.sh" >&2
  else
    echo "   Visual Studio 2017+ not found." >&2
    echo "" >&2
    echo "   Install from: https://visualstudio.microsoft.com/downloads/" >&2
    echo "   Required workloads:" >&2
    echo "     - Desktop development with C++" >&2
    echo "     - .NET desktop development" >&2
  fi

  echo "" >&2
  echo "   Alternatively, use PowerShell which can auto-initialize:" >&2
  echo "   PS> .\\build.ps1" >&2
  echo "" >&2

  return 1
}

# Run an MSBuild step with error handling
run_msbuild_step() {
  local msbuild_exe="$1"
  local description="$2"
  shift 2
  local args=("$@")

  echo "Running $description..."

  if [[ -n "$LOG_FILE" && "$description" == "FieldWorks Solution" ]]; then
    local log_dir
    log_dir="$(dirname "$LOG_FILE")"
    if [[ -n "$log_dir" && "$log_dir" != "." ]]; then
      mkdir -p "$log_dir"
    fi

    set +e
    "$msbuild_exe" "${args[@]}" | tee "$LOG_FILE"
    local exit_code=${PIPESTATUS[0]}
    set -e
  else
    set +e
    "$msbuild_exe" "${args[@]}"
    local exit_code=$?
    set -e
  fi

  if [[ $exit_code -ne 0 ]]; then
    echo "ERROR: MSBuild failed during $description (exit code $exit_code)" >&2
    exit $exit_code
  fi
}

# Disable Git Bash path translation for MSBuild arguments EARLY
# This prevents /t: and /p: from being converted to Windows paths
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL="*"

# Determine logical core count for CL_MPCount
if [[ -n "${CL_MPCount:-}" ]]; then
  MP_COUNT="$CL_MPCount"
else
  MP_COUNT=8
  if [[ -n "${NUMBER_OF_PROCESSORS:-}" ]]; then
    if [[ "$NUMBER_OF_PROCESSORS" -lt 8 ]]; then
      MP_COUNT="$NUMBER_OF_PROCESSORS"
    fi
  fi
fi

# Construct MSBuild arguments
FINAL_ARGS=()

if [[ "$SERIAL" == "false" ]]; then
  FINAL_ARGS+=("/m")
fi

FINAL_ARGS+=("/v:$VERBOSITY")
FINAL_ARGS+=("/nologo")
FINAL_ARGS+=("/consoleloggerparameters:Summary")
FINAL_ARGS+=("/nr:$NODE_REUSE")
FINAL_ARGS+=("/p:Configuration=$CONFIGURATION")
FINAL_ARGS+=("/p:Platform=$PLATFORM")
FINAL_ARGS+=("/p:CL_MPCount=$MP_COUNT")

if [[ "$BUILD_TESTS" == "true" ]]; then
  FINAL_ARGS+=("/p:BuildTests=true")
fi

if [[ "$BUILD_ADDITIONAL_APPS" == "true" ]]; then
  FINAL_ARGS+=("/p:BuildAdditionalApps=true")
fi

# Add user-supplied args
FINAL_ARGS+=("${MSBUILD_ARGS[@]}")

# Main build sequence
echo ""
echo "Building FieldWorks..."
echo "Configuration: $CONFIGURATION | Platform: $PLATFORM | Parallel: $(if [[ "$SERIAL" == "false" ]]; then echo "true"; else echo "false"; fi) | Tests: $BUILD_TESTS"
echo ""

# Find MSBuild
MSBUILD=$(find_msbuild)

# Check for Visual Studio Developer environment (required for native C++ builds)
if ! check_vs_environment; then
  exit 1
fi

# Set architecture environment variable for legacy MSBuild tasks
set_arch_environment "$PLATFORM"

echo ""

# Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
# Quiet mode for bootstrap
run_msbuild_step "$MSBUILD" "FwBuildTasks (Bootstrap)" \
  "Build/Src/FwBuildTasks/FwBuildTasks.csproj" \
  "/t:Restore;Build" \
  "/p:Configuration=$CONFIGURATION" \
  "/p:Platform=$PLATFORM" \
  "/v:quiet" "/nologo"

echo ""

# Restore packages
run_msbuild_step "$MSBUILD" "RestorePackages" \
  "Build/Orchestrator.proj" \
  "/t:RestorePackages" \
  "/p:Configuration=$CONFIGURATION" \
  "/p:Platform=$PLATFORM" \
  "/v:quiet" "/nologo"

# Build using traversal project
run_msbuild_step "$MSBUILD" "FieldWorks Solution" \
  "FieldWorks.proj" \
  "${FINAL_ARGS[@]}"

echo ""
echo "✅ Build complete!"
echo "Output directory: Output/$CONFIGURATION/"
