"""Repository-wide audit for GenerateAssemblyInfo convergence."""

from __future__ import annotations

import logging
from pathlib import Path

from . import __doc__ as package_doc  # noqa: F401
from . import cli_args
from .models import ManagedProject
from .project_scanner import scan_projects, summarize_categories
from .reporting import write_managed_projects_csv, write_projects_json

LOGGER = logging.getLogger(__name__)


def main() -> None:
    parser = cli_args.build_common_parser(
        "Audit managed projects for CommonAssemblyInfo template compliance."
    )
    parser.add_argument(
        "--json",
        action="store_true",
        help="Also emit a JSON copy of the project inventory for downstream tooling.",
    )
    args = parser.parse_args()

    logging.basicConfig(level=args.log_level)
    repo_root: Path = args.repo_root.resolve()

    LOGGER.info("Scanning projects under %s", repo_root / "Src")
    projects = scan_projects(
        repo_root,
        release_ref=args.release_ref,
        enable_history=not args.skip_history,
    )
    _log_summary(projects)

    csv_path = Path(args.output) / "generate_assembly_info_audit.csv"
    write_managed_projects_csv(projects, csv_path)
    LOGGER.info("Wrote audit CSV to %s", csv_path)

    if args.json:
        json_path = Path(args.output) / "generate_assembly_info_audit.json"
        write_projects_json(projects, json_path)
        LOGGER.info("Wrote audit JSON to %s", json_path)


def _log_summary(projects: list[ManagedProject]) -> None:
    summary = summarize_categories(projects)
    LOGGER.info(
        "Category counts: Template-only=%s Template+Custom=%s NeedsFix=%s",
        summary.get("T", 0),
        summary.get("C", 0),
        summary.get("G", 0),
    )


if __name__ == "__main__":
    main()
