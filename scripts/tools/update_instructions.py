#!/usr/bin/env python3
"""Run the instruction inventory, manifest generator, and validator in order."""
from __future__ import annotations
import subprocess
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
def run(cmd):
    print('Running', cmd)
    subprocess.check_call(cmd, shell=True)

run(f'python {ROOT}/scripts/tools/generate_instruction_inventory.py')
run(f'python {ROOT}/scripts/tools/generate_instruction_manifest.py')
run(f'python {ROOT}/scripts/tools/validate_instructions.py')
