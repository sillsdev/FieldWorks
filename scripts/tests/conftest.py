from __future__ import annotations

import sys
from pathlib import Path
from textwrap import dedent

import pytest

# Ensure the repository root is importable so tests can reach
# scripts.test_exclusions without needing editable installs.
REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))


@pytest.fixture
def repo_root() -> Path:
    """Return the absolute repository root used for relative path checks."""

    return REPO_ROOT


@pytest.fixture
def temp_repo(tmp_path: Path) -> Path:
    """Create a temporary repository-like layout with a Src folder."""

    repo = tmp_path / "repo"
    (repo / "Src").mkdir(parents=True)
    return repo


@pytest.fixture
def csproj_writer() -> "Writer":
    """Utility for producing SDK-style csproj files inside tests."""

    def _writer(path: Path, item_groups: str = "") -> Path:
        xml = dedent(
            f"""
            <Project Sdk=\"Microsoft.NET.Sdk\">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              {item_groups}
            </Project>
            """
        ).strip()
        path.write_text(xml, encoding="utf-8")
        return path

    return _writer


class Writer:  # pragma: no cover - helper type for static analyzers
    def __call__(self, path: Path, item_groups: str = "") -> Path: ...
