#!/usr/bin/env python3
"""Cache helpers for COPILOT planning scripts."""
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Optional

ISO_FORMAT = "%Y-%m-%dT%H:%M:%SZ"


class CopilotCache:
    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root
        self.cache_root = repo_root / ".cache" / "copilot"
        self.diff_dir = self.cache_root / "diffs"
        self.cache_root.mkdir(parents=True, exist_ok=True)
        self.diff_dir.mkdir(parents=True, exist_ok=True)

    def _path_for_folder(self, folder: str) -> Path:
        safe = folder.replace("\\", "/").replace("/", "__")
        return self.diff_dir / f"{safe}.json"

    def load_folder(self, folder: str, recorded_tree: str, head_tree: str) -> Optional[Dict[str, Any]]:
        path = self._path_for_folder(folder)
        if not path.exists():
            return None
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            return None
        if data.get("recorded_tree") == recorded_tree and data.get("current_tree") == head_tree:
            return data
        return None

    def save_folder(self, folder: str, payload: Dict[str, Any]) -> None:
        payload = dict(payload)
        payload["cached_at"] = datetime.now(timezone.utc).strftime(ISO_FORMAT)
        path = self._path_for_folder(folder)
        path.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")

    def clear_folder(self, folder: str) -> None:
        path = self._path_for_folder(folder)
        if path.exists():
            path.unlink()
