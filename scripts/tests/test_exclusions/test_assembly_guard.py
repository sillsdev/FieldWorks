from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

import pytest

GUARD_SCRIPT = (
    Path(__file__).resolve().parents[3]
    / "scripts"
    / "test_exclusions"
    / "assembly_guard.ps1"
)


def _compile_dll(source_code: str, output_path: Path):
    src_file = output_path.with_suffix(".cs")
    src_file.write_text(source_code, encoding="utf-8")

    # csc might not be in PATH if not in Dev Cmd.
    # We try running it.
    cmd = ["csc", "/target:library", f"/out:{output_path}", str(src_file)]
    subprocess.run(cmd, check=True, capture_output=True)


def test_assembly_guard_fails_on_test_types(tmp_path: Path):
    if not shutil.which("csc"):
        pytest.skip("csc not found")

    dll_path = tmp_path / "Bad.dll"
    _compile_dll("public class MyTests {}", dll_path)

    cmd = [
        "powershell",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        str(GUARD_SCRIPT),
        "-Assemblies",
        str(dll_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)

    assert result.returncode != 0
    assert "contains test types" in result.stderr


def test_assembly_guard_passes_clean_assembly(tmp_path: Path):
    if not shutil.which("csc"):
        pytest.skip("csc not found")

    dll_path = tmp_path / "Good.dll"
    _compile_dll("public class MyClass {}", dll_path)

    cmd = [
        "powershell",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        str(GUARD_SCRIPT),
        "-Assemblies",
        str(dll_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)

    assert result.returncode == 0
    assert "Assembly guard passed" in result.stdout
