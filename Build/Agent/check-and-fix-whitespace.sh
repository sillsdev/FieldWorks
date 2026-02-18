#!/usr/bin/env bash
set +e
bash "$(dirname "$0")/check-whitespace.sh"
ec=$?
bash "$(dirname "$0")/fix-whitespace.sh"
exit $ec
