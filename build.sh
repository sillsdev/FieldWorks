#!/usr/bin/env bash
set -euo pipefail

TARGETS="all"
CONFIGURATION="Debug"
PLATFORM="x64"
LOG_FILE=""
USE_TRAVERSAL=false
MSBUILD_ARGS=()
MSBUILD_BIN="${MSBUILD:-msbuild}"

print_usage() {
  cat <<'EOF'
Usage: build.sh [options] [-- additional msbuild arguments]

Options:
  -t, --targets TARGETS           Semicolon-separated msbuild target list (default: all)
  -c, --configuration CONFIG      Build configuration (default: Debug)
  -p, --platform PLATFORM         Build platform (default: x64)
  -l, --log-file FILE             Tee final msbuild output to FILE
  --use-traversal                 Use new MSBuild Traversal SDK approach (recommended)
  -h, --help                      Show this help message

Any arguments after "--" are passed directly to msbuild.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -t|--targets)
      TARGETS="$2"
      shift 2
      ;;
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
    --use-traversal)
      USE_TRAVERSAL=true
      shift
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

run_msbuild() {
  local description="$1"
  shift

  echo "Running: $MSBUILD_BIN $*"
  if [[ -n "$LOG_FILE" && "$description" == "FieldWorks" ]]; then
    local log_dir
    log_dir="$(dirname "$LOG_FILE")"
    [[ -n "$log_dir" && "$log_dir" != "." ]] && mkdir -p "$log_dir"

    set +e
    $MSBUILD_BIN "$@" | tee "$LOG_FILE"
    local exit_code=${PIPESTATUS[0]}
    set -e
  else
    $MSBUILD_BIN "$@"
    local exit_code=$?
  fi

  if [[ $exit_code -ne 0 ]]; then
    echo "MSBuild failed during $description (exit code $exit_code)" >&2
    exit $exit_code
  fi
}

if [[ "$USE_TRAVERSAL" == "true" ]]; then
  echo "Using MSBuild Traversal SDK approach (dirs.proj)..."
  
  # Restore packages first
  run_msbuild "RestorePackages" \
    "Build/FieldWorks.proj" \
    "/t:RestorePackages" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=$PLATFORM"
  
  # Build using traversal project
  run_msbuild "Traversal build" \
    "dirs.proj" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=$PLATFORM" \
    "${MSBUILD_ARGS[@]}"
else
  echo "Using legacy build approach (FieldWorks.proj)..."
  
  run_msbuild "FwBuildTasks" \
    "Build/Src/FwBuildTasks/FwBuildTasks.csproj" \
    "/t:Restore;Build" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=AnyCPU"

  run_msbuild "RestorePackages" \
    "Build/FieldWorks.proj" \
    "/t:RestorePackages" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=$PLATFORM"

  # CheckDevelopmentPropertiesFile is optional
  set +e
  run_msbuild "CheckDevelopmentPropertiesFile" \
    "Build/FieldWorks.proj" \
    "/t:CheckDevelopmentPropertiesFile" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=$PLATFORM" || echo "Warning: CheckDevelopmentPropertiesFile failed (this is optional)"
  set -e

  run_msbuild "refreshTargets" \
    "Build/FieldWorks.proj" \
    "/t:refreshTargets" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=$PLATFORM"

  run_msbuild "FieldWorks" \
    "Build/FieldWorks.proj" \
    "/t:$TARGETS" \
    "/p:Configuration=$CONFIGURATION" \
    "/p:Platform=$PLATFORM" \
    "${MSBUILD_ARGS[@]}"
fi
