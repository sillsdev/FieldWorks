#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Debug"
PLATFORM="x64"
LOG_FILE=""
MSBUILD_ARGS=()

print_usage() {
  cat <<'EOF'
Usage: build.sh [options] [-- additional msbuild arguments]

Builds FieldWorks using the MSBuild Traversal SDK (dirs.proj).

This script performs:
  1. Locates MSBuild (Visual Studio 2022/2019/2017 or from PATH)
  2. Sets architecture environment variable for legacy tasks
  3. NuGet package restoration
  4. Full traversal build via dirs.proj

Options:
  -c, --configuration CONFIG      Build configuration (default: Debug)
  -p, --platform PLATFORM         Build platform (default: x64)
  -l, --log-file FILE             Tee final msbuild output to FILE
  -h, --help                      Show this help message

Any arguments after "--" are passed directly to msbuild.

Examples:
  ./build.sh                                    # Build Debug x64
  ./build.sh -c Release                         # Build Release x64
  ./build.sh -- /m /v:detailed                  # Build with parallel and verbose logging
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
      shift 2
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
  case "${platform,,}" in
    x86)
      export arch="x86"
      ;;
    x64)
      export arch="x64"
      ;;
    *)
      export arch="$platform"
      ;;
  esac
  echo "Set arch environment variable to: $arch"
}

# Run an MSBuild step with error handling
run_msbuild_step() {
  local msbuild_exe="$1"
  local description="$2"
  shift 2
  local args=("$@")

  echo "Running: MSBuild ${args[*]}"

  if [[ -n "$LOG_FILE" && "$description" == "FieldWorks" ]]; then
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

# Main build sequence
echo ""
echo "Building FieldWorks using MSBuild Traversal SDK (dirs.proj)..."
echo "Configuration: $CONFIGURATION, Platform: $PLATFORM"
echo ""

# Find MSBuild
MSBUILD=$(find_msbuild)

# Set architecture environment variable for legacy MSBuild tasks
set_arch_environment "$PLATFORM"

# Disable Git Bash path translation for MSBuild arguments
# This prevents /t: and /p: from being converted to Windows paths
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL="*"

echo ""

# Restore packages first
run_msbuild_step "$MSBUILD" "RestorePackages" \
  "Build/Orchestrator.proj" \
  "/t:RestorePackages" \
  "/p:Configuration=$CONFIGURATION" \
  "/p:Platform=$PLATFORM"

# Build using traversal project
run_msbuild_step "$MSBUILD" "FieldWorks" \
  "dirs.proj" \
  "/p:Configuration=$CONFIGURATION" \
  "/p:Platform=$PLATFORM" \
  "${MSBUILD_ARGS[@]}"

echo ""
echo "✅ Build complete!"
echo "Output directory: Output/$CONFIGURATION/"
