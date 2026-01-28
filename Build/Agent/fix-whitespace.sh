#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib_git.sh
source "${SCRIPT_DIR}/lib_git.sh"

# Determine base ref
if [[ -n "${GITHUB_BASE_REF:-}" ]]; then
  base="origin/${GITHUB_BASE_REF}"
else
  base="$(git_default_branch_ref)"
fi

files=()
if [[ -f "check-results.log" ]]; then
  # Extract unique file paths from check results
  mapfile -t files < <(awk -F':' '/^[^:]+:[1-9][0-9]*:/ {print $1}' check-results.log | awk '!seen[$0]++')
  [[ ${#files[@]} -gt 0 ]] && echo "Fixing whitespace for files listed in check-results.log"
fi

if [[ ${#files[@]} -eq 0 ]]; then
  echo "Fixing whitespace for files changed since ${base}..HEAD"
  mapfile -t files < <(git diff --name-only "$base"..HEAD)
fi

for f in "${files[@]}"; do
  [[ -f "$f" ]] || continue
  # Strip trailing spaces/tabs on each line and ensure exactly one trailing newline
  # Using perl for robust in-place editing across platforms
  perl -0777 -pe 's/[ \t]+$//mg; s/\s*\z/\n/s' -i "$f" || true
  echo "Fixed whitespace: $f"
done

echo "Whitespace fix completed. Review changes, commit, and rebase as needed."
exit 0
