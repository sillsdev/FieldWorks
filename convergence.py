#!/usr/bin/env python3
"""
SDK Migration Convergence Tool
Unified CLI for all convergence efforts
"""

import argparse
import sys
from pathlib import Path

# Import specific convergences
# from convergence_generate_assembly_info import GenerateAssemblyInfoAuditor, GenerateAssemblyInfoConverter, GenerateAssemblyInfoValidator
# from convergence_regfree_com import RegFreeComAuditor, RegFreeComConverter, RegFreeComValidator
# from convergence_test_exclusions import TestExclusionAuditor, TestExclusionConverter, TestExclusionValidator
from convergence_platform_target import (
    PlatformTargetAuditor,
    PlatformTargetConverter,
    PlatformTargetValidator,
)
from convergence_private_assets import (
    PrivateAssetsAuditor,
    PrivateAssetsConverter,
    PrivateAssetsValidator,
)

CONVERGENCES = {
    # 'generate-assembly-info': {
    #     'auditor': GenerateAssemblyInfoAuditor,
    #     'converter': GenerateAssemblyInfoConverter,
    #     'validator': GenerateAssemblyInfoValidator,
    #     'description': 'Standardize GenerateAssemblyInfo settings'
    # },
    # 'regfree-com': {
    #     'auditor': RegFreeComAuditor,
    #     'converter': RegFreeComConverter,
    #     'validator': RegFreeComValidator,
    #     'description': 'Complete RegFree COM coverage'
    # },
    # 'test-exclusions': {
    #     'auditor': TestExclusionAuditor,
    #     'converter': TestExclusionConverter,
    #     'validator': TestExclusionValidator,
    #     'description': 'Standardize test exclusion patterns'
    # },
    "platform-target": {
        "auditor": PlatformTargetAuditor,
        "converter": PlatformTargetConverter,
        "validator": PlatformTargetValidator,
        "description": "Remove redundant PlatformTarget settings",
    },
    "private-assets": {
        "auditor": PrivateAssetsAuditor,
        "converter": PrivateAssetsConverter,
        "validator": PrivateAssetsValidator,
        "description": "Enforce PrivateAssets='All' on LCM test packages",
    },
}


def main():
    parser = argparse.ArgumentParser(description="SDK Migration Convergence Tool")
    parser.add_argument(
        "convergence",
        choices=list(CONVERGENCES.keys()) + ["all"],
        help="Which convergence to process",
    )
    parser.add_argument(
        "action", choices=["audit", "convert", "validate"], help="Action to perform"
    )
    parser.add_argument("--repo", default=".", help="Repository root path")
    parser.add_argument(
        "--output", help="Output file path for audit/validation reports"
    )
    parser.add_argument(
        "--decisions", help="CSV file with conversion decisions (for convert action)"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be done without making changes",
    )

    args = parser.parse_args()

    if args.convergence == "all":
        convergences = CONVERGENCES.keys()
    else:
        convergences = [args.convergence]

    for convergence_name in convergences:
        print(f"\n{'='*60}")
        print(f"Processing: {convergence_name}")
        print(f"Action: {args.action}")
        print(f"{'='*60}\n")

        convergence = CONVERGENCES[convergence_name]

        if args.action == "audit":
            auditor = convergence["auditor"](args.repo)
            auditor.run_audit()
            output_path = args.output or f"{convergence_name}_audit.csv"
            auditor.export_csv(output_path)
            auditor.print_summary()

        elif args.action == "convert":
            if not args.decisions:
                print("Error: --decisions CSV file required for convert action")
                sys.exit(1)
            converter = convergence["converter"](args.repo)
            converter.run_conversions(args.decisions, dry_run=args.dry_run)

        elif args.action == "validate":
            validator = convergence["validator"](args.repo)
            violations = validator.run_validation()
            exit_code = validator.print_report()
            output_path = args.output or f"{convergence_name}_validation.txt"
            validator.export_report(output_path)
            if exit_code != 0:
                sys.exit(exit_code)


if __name__ == "__main__":
    main()
