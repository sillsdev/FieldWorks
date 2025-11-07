#!/usr/bin/env bash
set -euo pipefail

TARGETS="all"
CONFIGURATION="Debug"
PLATFORM="x64"
LOG_FILE=""
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

run_msbuild "FwBuildTasks" \
  "Build/Src/FwBuildTasks/FwBuildTasks.csproj" \
  "/t:Restore;Build" \
  "/p:Configuration=$CONFIGURATION" \
  "/p:Platform=AnyCPU"

run_msbuild "FieldWorks" \
  "Build/FieldWorks.proj" \
  "/t:$TARGETS" \
  "/p:Configuration=$CONFIGURATION" \
  "/p:Platform=$PLATFORM" \
  "${MSBUILD_ARGS[@]}"
