import tempfile
import unittest
from pathlib import Path

from scripts.mcp.config import ServerConfig, ToolSelection
from scripts.mcp.ps_tools import ToolDiscovery, ToolRunner


class UserStory3Tests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_root = Path(tempfile.mkdtemp())
        self.agent_dir = self.temp_root / "scripts" / "Agent"
        self.agent_dir.mkdir(parents=True)

        # Stub Copilot scripts output arguments
        self.scripts = {}
        for name in [
            "Copilot-Detect.ps1",
            "Copilot-Plan.ps1",
            "Copilot-Apply.ps1",
            "Copilot-Validate.ps1",
        ]:
            path = self.agent_dir / name
            path.write_text("Write-Output ($args -join ' ')", encoding="utf-8")
            self.scripts[name] = path

        self.config = ServerConfig.defaults(self.temp_root)
        self.config.agent_scripts_dir = self.agent_dir
        self.config.working_dir = self.temp_root
        self.config.tools = ToolSelection(allow={
            "Copilot-Detect",
            "Copilot-Plan",
            "Copilot-Apply",
            "Copilot-Validate",
        })
        self.config.extra_tools = {}

        self.runner = ToolRunner(self.config)
        self.tools = {t.name: t for t in ToolDiscovery(self.config).discover()}

    def test_detect_args(self):
        tool = self.tools["Copilot-Detect"]
        result = self.runner.run(tool, {"base": "release/9.3", "out": "detect.json"})
        self.assertIn("-Base release/9.3", result["stdout"])
        self.assertIn("-Out detect.json", result["stdout"])

    def test_plan_args(self):
        tool = self.tools["Copilot-Plan"]
        result = self.runner.run(tool, {"detectJson": "detect.json", "out": "plan.json", "base": "main"})
        stdout = result["stdout"]
        self.assertIn("-DetectJson detect.json", stdout)
        self.assertIn("-Out plan.json", stdout)
        self.assertIn("-Base main", stdout)

    def test_apply_args(self):
        tool = self.tools["Copilot-Apply"]
        result = self.runner.run(tool, {"plan": "plan.json", "folders": "Src/Common"})
        stdout = result["stdout"]
        self.assertIn("-Plan plan.json", stdout)
        self.assertIn("-Folders Src/Common", stdout)

    def test_validate_args(self):
        tool = self.tools["Copilot-Validate"]
        result = self.runner.run(tool, {"base": "main", "paths": "Src/xWorks"})
        stdout = result["stdout"]
        self.assertIn("-Base main", stdout)
        self.assertIn("-Paths Src/xWorks", stdout)


if __name__ == "__main__":
    unittest.main()
