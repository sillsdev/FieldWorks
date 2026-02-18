#!/usr/bin/env python3
"""Shared helpers for COPILOT automation scripts."""
from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Dict, Iterable, List

RESOURCE_EXTS = {
    ".resx",
    ".xaml",
    ".xml",
    ".xsd",
    ".xsl",
    ".xslt",
    ".config",
    ".json",
}

TEST_TOKENS = (
    "/tests/",
    "\\tests\\",
    ".tests/",
    ".tests\\",
    ".tests.",
)


@dataclass
class ChangeClassification:
    path: str
    ext: str
    kind: str  # code, test, resource, or other
    is_test: bool
    is_resource: bool


def classify_path(rel_path: str) -> ChangeClassification:
    """Classify a relative path into buckets used by risk scoring."""
    norm = rel_path.replace("\\", "/")
    lower = norm.lower()
    ext = os.path.splitext(lower)[1]
    is_test = any(token in lower for token in TEST_TOKENS) or lower.endswith("tests.cs")
    is_resource = ext in RESOURCE_EXTS
    if is_test:
        kind = "test"
    elif is_resource:
        kind = "resource"
    else:
        kind = "code"
    return ChangeClassification(path=norm, ext=ext, kind=kind, is_test=is_test, is_resource=is_resource)


def summarize_paths(paths: Iterable[str]) -> Dict[str, int]:
    """Return aggregate counts for a collection of relative paths."""
    counts = {
        "total": 0,
        "code": 0,
        "tests": 0,
        "resources": 0,
    }
    for rel in paths:
        info = classify_path(rel)
        counts["total"] += 1
        if info.kind == "test":
            counts["tests"] += 1
        elif info.kind == "resource":
            counts["resources"] += 1
        else:
            counts["code"] += 1
    return counts


def compute_risk_score(counts: Dict[str, int]) -> str:
    """Estimate a simple risk level based on change counts."""
    total = counts.get("total", 0)
    if total == 0:
        return "none"
    code_changes = counts.get("code", 0)
    test_changes = counts.get("tests", 0)
    if code_changes >= 10 or (code_changes >= 5 and test_changes == 0):
        return "high"
    if total >= 5:
        return "medium"
    return "low"


def classify_with_status(entries: Iterable[str]) -> List[Dict[str, str]]:
    """Split "status\tpath" lines into structured dictionaries."""
    results: List[Dict[str, str]] = []
    for raw in entries:
        if not raw.strip():
            continue
        parts = raw.split("\t", 1)
        if len(parts) != 2:
            continue
        status, rel_path = parts
        info = classify_path(rel_path)
        results.append(
            {
                "status": status,
                "path": info.path,
                "kind": info.kind,
                "ext": info.ext,
                "is_test": info.is_test,
                "is_resource": info.is_resource,
            }
        )
    return results
