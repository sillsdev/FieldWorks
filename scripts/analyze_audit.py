"""Quick analysis of the audit CSV to understand project distribution."""

import csv
from pathlib import Path

csv_path = (
    Path(__file__).parent.parent
    / "Output"
    / "GenerateAssemblyInfo"
    / "generate_assembly_info_audit.csv"
)

with open(csv_path, encoding="utf-8") as f:
    data = list(csv.DictReader(f))

print(f"Total projects: {len(data)}")
print(f"\nCategory distribution:")
print(
    f"  C (template+custom, compliant): {sum(1 for r in data if r['category'] == 'C')}"
)
print(f"  G (needs remediation): {sum(1 for r in data if r['category'] == 'G')}")
print(f"  T (template only, compliant): {sum(1 for r in data if r['category'] == 'T')}")

g_with = sum(
    1 for r in data if r["category"] == "G" and r["has_custom_assembly_info"] == "True"
)
g_without = sum(
    1 for r in data if r["category"] == "G" and r["has_custom_assembly_info"] == "False"
)
print(f"\nCategory G breakdown:")
print(f"  With custom files: {g_with}")
print(f"  Without custom files: {g_without}")

baseline_yes = sum(1 for r in data if r["release_ref_has_custom_files"] == "True")
baseline_no = sum(1 for r in data if r["release_ref_has_custom_files"] == "False")
print(f"\nRelease/9.3 baseline:")
print(f"  Had custom files: {baseline_yes}")
print(f"  No custom files: {baseline_no}")

# Test projects
test_projects = [r for r in data if "Test" in r["project_id"]]
print(f"\nTest projects: {len(test_projects)}")
test_with_custom = sum(
    1 for r in test_projects if r["has_custom_assembly_info"] == "True"
)
print(f"  Test projects with custom files: {test_with_custom}")

# Check for nested project issues
print(f"\nSample projects with multiple AssemblyInfo files:")
multi = [r for r in data if r["assembly_info_details"].count(";") >= 1][:5]
for r in multi:
    print(f"  {r['project_id']}: {r['assembly_info_details'].count(';') + 1} files")
