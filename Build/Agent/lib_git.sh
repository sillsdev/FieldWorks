#!/usr/bin/env bash
# Shared Git helpers for Build/Agent bash scripts
set -euo pipefail

# Returns the default branch name configured for origin (e.g., main, develop)
git_default_branch_name() {
  git remote show origin 2>/dev/null | awk -F': ' '/HEAD branch/ {print $2}' || true
}

# Returns the full ref for the origin default branch (e.g., origin/main)
git_default_branch_ref() {
  local name
  name="$(git_default_branch_name)"
  if [[ -n "$name" ]]; then
    echo "origin/${name}"
  else
    echo "origin/develop"
  fi
}
