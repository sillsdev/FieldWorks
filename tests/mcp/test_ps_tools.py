import tempfile
import time
import unittest
from pathlib import Path

from scripts.mcp.config import ServerConfig, ToolSelection
from scripts.mcp.ps_tools import ToolDiscovery, ToolRunner, ToolDescriptor


class ToolTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.repo_root = Path(self.temp_dir.name)
        self.agent_dir = self.repo_root / "scripts" / "Agent"
        self.agent_dir.mkdir(parents=True)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def _config(self, allow=None, deny=None, output_cap=1_000_000, timeout=5):
        config = ServerConfig.defaults(self.repo_root)
        config.agent_scripts_dir = self.agent_dir
        config.working_dir = self.repo_root
        config.tools = ToolSelection(set(allow or []), set(deny or []))
        config.extra_tools = {}
        config.output_cap_bytes = output_cap
        config.timeout_seconds = timeout
        return config

    def _write_script(self, path: Path, lines: list[str]) -> None:
        path.write_text("\n".join(lines), encoding="utf-8")

    def test_discovery_respects_allow_and_extra_tools(self):
        foo = self.agent_dir / "Foo.ps1"
        self._write_script(foo, ["Write-Output 'foo'"])
        extra = self.repo_root / "extra.ps1"
        self._write_script(extra, ["Write-Output 'extra'"])

        config = self._config()
        config.extra_tools = {"extra": extra}

        tools = ToolDiscovery(config).discover()
        names = {t.name for t in tools}
        self.assertEqual(names, {"Foo", "extra"})

        config_allow = self._config(allow={"Foo"})
        config_allow.extra_tools = {"extra": extra}
        tools_allow = ToolDiscovery(config_allow).discover()
        self.assertEqual([t.name for t in tools_allow], ["Foo"])

    def test_runner_success_and_nonzero(self):
        ok = self.agent_dir / "Ok.ps1"
        self._write_script(ok, ["Write-Output 'hello'"])
        fail = self.agent_dir / "Fail.ps1"
        self._write_script(fail, ["Write-Error 'bad'", "exit 5"])

        config = self._config()
        runner = ToolRunner(config)

        ok_result = runner.run(ToolDescriptor("Ok", ok))
        self.assertEqual(ok_result["exitCode"], 0)
        self.assertIn("hello", ok_result["stdout"])
        self.assertFalse(ok_result["truncated"])
        self.assertFalse(ok_result["timedOut"])

        fail_result = runner.run(ToolDescriptor("Fail", fail))
        self.assertEqual(fail_result["exitCode"], 5)
        self.assertIn("bad", fail_result["stderr"])
        self.assertFalse(fail_result["timedOut"])

    def test_runner_truncates_large_output(self):
        noisy = self.agent_dir / "Noisy.ps1"
        self._write_script(noisy, ["Write-Output ('x' * 50)"])

        config = self._config(output_cap=10)
        runner = ToolRunner(config)

        result = runner.run(ToolDescriptor("Noisy", noisy))
        self.assertTrue(result["truncated"])
        self.assertTrue(result["stdout"].endswith("[truncated]"))

    def test_runner_times_out(self):
        slow = self.agent_dir / "Slow.ps1"
        self._write_script(slow, ["Start-Sleep -Seconds 2"])

        config = self._config(timeout=1)
        runner = ToolRunner(config)

        result = runner.run(ToolDescriptor("Slow", slow))
        self.assertEqual(result["exitCode"], -1)
        self.assertTrue(result["timedOut"])
        self.assertIn("Timed out", result["stderr"])


if __name__ == "__main__":
    unittest.main()
