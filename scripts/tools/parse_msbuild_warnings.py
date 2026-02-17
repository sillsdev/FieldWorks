#!/usr/bin/env python3
"""Parse an msbuild warnings-only log into CSV and JSON.

Generates warnings.json and warnings.csv in the repo root.

Usage:
  python scripts/tools/parse_msbuild_warnings.py warnings.log

The parser extracts: project, warning_code, file, line, message, raw_line
"""
import json
import csv
import re
import sys
from pathlib import Path


def parse_line(line):
    # Patterns for msbuild warnings like:
    #  32>C:\path\to\file.targets(2433,5): warning MSB3245: message [C:\path\to\project.csproj]
    #  65>CSC : warning CS2002: Source file 'C:\...\AssemblyInfoForTests.cs' specified multiple times [C:\...\project.csproj]
    m = re.match(r"\s*(?:(\d+)>)*(.+?): warning (MSB\d+): (.+?) \[(.+?)\]", line)
    if m:
        return {
            "project": m.group(5),
            "warning_code": m.group(3),
            "file": None,
            "line": None,
            "message": m.group(4).strip(),
            "raw": line.strip(),
        }

    m2 = re.match(r"\s*(?:(\d+)>)*CSC : warning (CS\d+): (.+?) \[(.+?)\]", line)
    if m2:
        # try to extract file within message
        file_match = re.search(r"'([A-Za-z0-9:/\\._-]+)'", m2.group(3))
        return {
            "project": m2.group(4),
            "warning_code": m2.group(2),
            "file": file_match.group(1) if file_match else None,
            "line": None,
            "message": m2.group(3).strip(),
            "raw": line.strip(),
        }

    # fallback: find MSB or CS codes anywhere
    m3 = re.search(r"(MSB\d+|CS\d+): (.+?) \[(.+?)\]", line)
    if m3:
        return {
            "project": m3.group(3),
            "warning_code": m3.group(1),
            "file": None,
            "line": None,
            "message": m3.group(2).strip(),
            "raw": line.strip(),
        }

    return None


def main():
    if len(sys.argv) < 2:
        print("Usage: parse_msbuild_warnings.py <warnings.log>")
        sys.exit(2)

    path = Path(sys.argv[1])
    if not path.exists():
        print(f"File {path} not found")
        sys.exit(1)

    out_json = Path.cwd() / "warnings.json"
    out_csv = Path.cwd() / "warnings.csv"

    records = []
    with path.open('r', encoding='utf-8', errors='ignore') as fh:
        for raw in fh:
            parsed = parse_line(raw)
            if parsed:
                records.append(parsed)

    # write json
    with out_json.open('w', encoding='utf-8') as fh:
        json.dump(records, fh, indent=2, ensure_ascii=False)

    # write csv
    if records:
        with out_csv.open('w', encoding='utf-8', newline='') as fh:
            w = csv.DictWriter(fh, fieldnames=["project","warning_code","file","line","message","raw"])
            w.writeheader()
            for r in records:
                w.writerow(r)

    print(f"Parsed {len(records)} warnings -> {out_json.name}, {out_csv.name}")


if __name__ == '__main__':
    main()
