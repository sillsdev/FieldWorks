from __future__ import annotations

import argparse
import json
import logging
import sys
from pathlib import Path
from typing import Dict, List

from .config import ServerConfig, load_config, repo_root_from_env
from .ps_tools import ToolDiscovery, ToolRunner

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="[%(levelname)s] %(message)s")


try:
    # Optional dependency; if unavailable we can still run tools via CLI mode.
    from mcp.server.fastmcp import FastMCP
except Exception:  # pragma: no cover - optional
    FastMCP = None  # type: ignore


def build_tools(config: ServerConfig):
    discovery = ToolDiscovery(config)
    tools = discovery.discover()
    runner = ToolRunner(config)
    return tools, runner


def serve(config: ServerConfig, host: str = "127.0.0.1", port: int = 5000) -> None:
    tools, runner = build_tools(config)

    if FastMCP is None:
        raise RuntimeError(
            "fastmcp (modelcontext-protocol) is not installed. Install dependencies via "
            "`pip install -r scripts/mcp/requirements.txt`."
        )

    server = FastMCP()

    @server.tool()
    def run_tool(name: str, args: Dict | List | None = None) -> Dict:
        match = next((t for t in tools if t.name == name), None)
        if not match:
            return {"stdout": "", "stderr": f"Tool '{name}' not found", "exitCode": -1, "truncated": False, "timedOut": False}
        return runner.run(match, args)

    server.run(host=host, port=port)


def cli_run(config: ServerConfig, name: str, args: List[str]) -> int:
    tools, runner = build_tools(config)
    match = next((t for t in tools if t.name == name), None)
    if not match:
        logger.error("Tool '%s' not found", name)
        return 1
    result = runner.run(match, args)
    json.dump(result, sys.stdout, indent=2)
    sys.stdout.write("\n")
    return 0 if result.get("exitCode", 1) == 0 else 1


def main() -> int:
    parser = argparse.ArgumentParser(description="PowerShell MCP server and runner")
    parser.add_argument("--config", type=Path, help="Path to JSON config overrides", default=None)
    parser.add_argument("--serve", action="store_true", help="Start MCP server (requires fastmcp)")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5000)
    parser.add_argument("--tool", help="Run a single tool by name (CLI mode)")
    parser.add_argument("--args", nargs="*", default=[], help="Arguments for the tool in CLI mode")
    args = parser.parse_args()

    repo_root = repo_root_from_env()
    config = load_config(repo_root, args.config)

    if args.serve:
        serve(config, host=args.host, port=args.port)
        return 0

    if args.tool:
        return cli_run(config, args.tool, args.args)

    parser.print_help()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
