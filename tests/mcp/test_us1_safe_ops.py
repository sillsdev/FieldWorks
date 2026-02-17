import subprocess
import tempfile
import unittest
from pathlib import Path

from scripts.mcp.config import ServerConfig
from scripts.mcp.ps_tools import ToolDiscovery, ToolRunner


class UserStory1Tests(unittest.TestCase):
    def setUp(self) -> None:
        self.repo_root = Path(__file__).resolve().parents[2]
        self.config = ServerConfig.defaults(self.repo_root)
        self.runner = ToolRunner(self.config)
        self.tools = {t.name: t for t in ToolDiscovery(self.config).discover()}

    def _init_git_repo(self) -> Path:
        temp_dir = Path(tempfile.mkdtemp())
        subprocess.check_call(["git", "init"], cwd=temp_dir)
        subprocess.check_call(["git", "config", "user.email", "dev@example.com"], cwd=temp_dir)
        subprocess.check_call(["git", "config", "user.name", "Dev"], cwd=temp_dir)
        sample = temp_dir / "sample.txt"
        sample.write_text("alpha\nbeta\n", encoding="utf-8")
        subprocess.check_call(["git", "add", "sample.txt"], cwd=temp_dir)
        subprocess.check_call(["git", "commit", "-m", "init"], cwd=temp_dir)
        return temp_dir

    def test_git_search_log(self):
        repo = self._init_git_repo()
        tool = self.tools["Git-Search"]

        result = self.runner.run(
            tool,
            {
                "action": "log",
                "repoPath": str(repo),
                "headLines": 5,
            },
        )

        self.assertEqual(result["exitCode"], 0)
        self.assertIn("init", result["stdout"])

    def test_read_file_content_head(self):
        temp_dir = Path(tempfile.mkdtemp())
        target = temp_dir / "file.txt"
        target.write_text("line1\nline2\nline3\n", encoding="utf-8")

        tool = self.tools["Read-FileContent"]
        result = self.runner.run(
            tool,
            {
                "path": str(target),
                "headLines": 2,
            },
        )

        self.assertEqual(result["exitCode"], 0)
        self.assertIn("line1", result["stdout"])
        self.assertIn("line2", result["stdout"])
        self.assertNotIn("line3", result["stdout"])


if __name__ == "__main__":
    unittest.main()
