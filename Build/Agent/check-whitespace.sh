#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib_git.sh
source "${SCRIPT_DIR}/lib_git.sh"

# Determine base commit/ref
BASE_SHA="${1:-}"
if [[ -z "$BASE_SHA" ]]; then
  if [[ -n "${GITHUB_EVENT_PULL_REQUEST_BASE_SHA:-}" ]]; then
    BASE_SHA="$GITHUB_EVENT_PULL_REQUEST_BASE_SHA"
  elif [[ -n "${GITHUB_BASE_REF:-}" ]]; then
    BASE_SHA="origin/${GITHUB_BASE_REF}"
  else
    BASE_SHA="$(git_default_branch_ref)"
  fi
fi

echo "Checking whitespace with git log --check from ${BASE_SHA}..HEAD"
git log --check --pretty=format:"---% h% s" "${BASE_SHA}.." | tee check-results.log

# Parse results to prepare a summary if running in GitHub Actions
problems=()
commit=""
commitText=""
commitTextmd=""
while IFS='' read -r line || [[ -n "$line" ]]; do
  case "$line" in
    "--- "*)
      # format: --- <sha> <subject>
      read -r _ commit commitText <<<"$line"
      if [[ -n "${GITHUB_REPOSITORY:-}" ]]; then
        commitTextmd="[${commit}](https://github.com/${GITHUB_REPOSITORY}/commit/${commit}) ${commitText}"
      else
        commitTextmd="${commit} ${commitText}"
      fi
      ;;
    "") ;;
    *:[1-9]*:*)
      file="${line%%:*}"
      afterFile="${line#*:}"
      lineNumber="${afterFile%%:*}"
      problems+=("[${commitTextmd}]")
      if [[ -n "${GITHUB_REPOSITORY:-}" ]] && [[ -n "${GITHUB_REF_NAME:-}" ]]; then
        problems+=("[${line}](https://github.com/${GITHUB_REPOSITORY}/blob/${GITHUB_REF_NAME}/${file}#L${lineNumber})")
      else
        problems+=("${line}")
      fi
      problems+=("")
      ;;
  esac
done < check-results.log

if [[ ${#problems[@]} -gt 0 ]]; then
  echo "Whitespace issues were found." >&2
  if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    {
      echo "⚠️ Please review the Summary output for further information."
      echo "### A whitespace issue was found in one or more of the commits."
      echo
      echo "Errors:"
      for i in "${problems[@]}"; do
        echo "$i"
      done
    } >> "$GITHUB_STEP_SUMMARY"
  fi
  exit 1
fi

echo "No problems found"
exit 0
