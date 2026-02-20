#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib_git.sh
source "${SCRIPT_DIR}/lib_git.sh"

# Determine base ref for commit range
BASE_REF=""
if [[ -n "${GITHUB_BASE_REF:-}" ]]; then
  BASE_REF="origin/${GITHUB_BASE_REF}"
else
  BASE_REF="$(git_default_branch_ref)"
fi

# Ensure gitlint is available
if ! command -v gitlint >/dev/null 2>&1; then
  if command -v python3 >/dev/null 2>&1; then
    python3 -m pip install --upgrade gitlint
  else
    pip install --upgrade gitlint
  fi
fi

echo "Running gitlint against range: ${BASE_REF}..HEAD"
# Run gitlint and tee output to check_results.log (used by CI summary/comment)
set +e
gitlint --ignore body-is-missing --commits "${BASE_REF}.." 2>&1 | tee check_results.log
exit_code=${PIPESTATUS[0]}
set -e

exit ${exit_code}
