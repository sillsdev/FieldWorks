import tempfile
import unittest
from pathlib import Path

from scripts.mcp.config import ServerConfig, ToolSelection
from scripts.mcp.ps_tools import ToolDiscovery, ToolRunner


class UserStory2Tests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_root = Path(tempfile.mkdtemp())
        self.agent_dir = self.temp_root / "scripts" / "Agent"
        self.agent_dir.mkdir(parents=True)

        # Stub build/test scripts that echo received args
        self.build_script = self.temp_root / "build.ps1"
        self.test_script = self.temp_root / "test.ps1"
        stub_body = "Write-Output ($args -join ' ')"
        self.build_script.write_text(stub_body, encoding="utf-8")
        self.test_script.write_text(stub_body, encoding="utf-8")

        self.config = ServerConfig.defaults(self.temp_root)
        self.config.agent_scripts_dir = self.agent_dir
        self.config.working_dir = self.temp_root
        self.config.tools = ToolSelection(allow={"build", "test"})
        self.config.extra_tools = {"build": self.build_script, "test": self.test_script}

        self.runner = ToolRunner(self.config)
        self.tools = {t.name: t for t in ToolDiscovery(self.config).discover()}

    def test_build_arguments_mapped(self):
        tool = self.tools["build"]
        result = self.runner.run(
            tool,
            {
                "configuration": "Release",
                "msBuildArgs": ["/m:1"],
                "buildTests": True,
            },
        )
        self.assertEqual(result["exitCode"], 0)
        stdout = result["stdout"].strip()
        self.assertIn("-Configuration Release", stdout)
        self.assertIn("-MsBuildArgs /m:1", stdout)
        self.assertIn("-BuildTests", stdout)

    def test_test_arguments_mapped(self):
        tool = self.tools["test"]
        result = self.runner.run(
            tool,
            {
                "testFilter": "TestCategory!=Slow",
                "noBuild": True,
                "configuration": "Debug",
            },
        )
        self.assertEqual(result["exitCode"], 0)
        stdout = result["stdout"].strip()
        self.assertIn("-TestFilter TestCategory!=Slow", stdout)
        self.assertIn("-NoBuild", stdout)
        self.assertIn("-Configuration Debug", stdout)


if __name__ == "__main__":
    unittest.main()
