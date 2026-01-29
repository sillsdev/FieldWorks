import io
import json
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest import mock

from scripts.mcp import server
from scripts.mcp.config import ServerConfig, ToolSelection


class StubFastMCP:
    created = []

    def __init__(self):
        self.tools = {}
        StubFastMCP.created.append(self)

    def tool(self):
        def decorator(fn):
            self.tools[fn.__name__] = fn
            return fn

        return decorator

    def run(self, host: str, port: int):
        self.host = host
        self.port = port
        self.success = self.tools["run_tool"]("Echo", ["hi"])
        self.missing = self.tools["run_tool"]("missing", None)


class ServerTests(unittest.TestCase):
    def setUp(self) -> None:
        StubFastMCP.created.clear()
        self.temp_dir = tempfile.TemporaryDirectory()
        self.repo_root = Path(self.temp_dir.name)
        self.agent_dir = self.repo_root / "scripts" / "Agent"
        self.agent_dir.mkdir(parents=True)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def _config(self) -> ServerConfig:
        config = ServerConfig.defaults(self.repo_root)
        config.agent_scripts_dir = self.agent_dir
        config.working_dir = self.repo_root
        config.tools = ToolSelection()
        config.extra_tools = {}
        return config

    def _write_script(self, path: Path, lines: list[str]) -> None:
        path.write_text("\n".join(lines), encoding="utf-8")

    def test_cli_run_reports_exit_code(self):
        echo = self.agent_dir / "Echo.ps1"
        self._write_script(echo, ["Write-Output 'ok'"])

        config = self._config()

        buffer = io.StringIO()
        with redirect_stdout(buffer):
            exit_code = server.cli_run(config, "Echo", [])
        output = json.loads(buffer.getvalue())

        self.assertEqual(exit_code, 0)
        self.assertEqual(output["exitCode"], 0)
        self.assertIn("ok", output["stdout"])

        buffer_missing = io.StringIO()
        with redirect_stdout(buffer_missing):
            missing_code = server.cli_run(config, "Missing", [])

        self.assertEqual(missing_code, 1)
        self.assertEqual(buffer_missing.getvalue(), "")

    def test_serve_registers_tools_and_runs(self):
        echo = self.agent_dir / "Echo.ps1"
        self._write_script(echo, ["Write-Output 'hello'"])

        config = self._config()

        with mock.patch.object(server, "FastMCP", StubFastMCP):
            server.serve(config, host="127.0.0.2", port=5050)

        stub = StubFastMCP.created[-1]
        self.assertEqual((stub.host, stub.port), ("127.0.0.2", 5050))
        self.assertEqual(stub.success["exitCode"], 0)
        self.assertIn("hello", stub.success["stdout"])
        self.assertEqual(stub.missing["exitCode"], -1)


if __name__ == "__main__":
    unittest.main()
