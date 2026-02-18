"""
Generate RegFree COM manifests for applications based on audit report.
"""

import csv
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
ARTIFACTS_DIR = (
    REPO_ROOT / "specs" / "003-convergence-regfree-com-coverage" / "artifacts"
)
OUTPUT_DIR = REPO_ROOT / "Output" / "Debug"


def generate_app_manifest(exe_name: str, output_dir: Path):
    ns = "urn:schemas-microsoft-com:asm.v1"
    ET.register_namespace("", ns)

    root = ET.Element(f"{{{ns}}}assembly", manifestVersion="1.0")

    ET.SubElement(
        root,
        f"{{{ns}}}assemblyIdentity",
        type="win32",
        name=exe_name.replace(".exe", ""),
        version="1.0.0.0",
        processorArchitecture="amd64",
    )

    # Add dependency on FieldWorks.RegFree
    dep_elem = ET.SubElement(root, f"{{{ns}}}dependency")
    dep_asm = ET.SubElement(dep_elem, f"{{{ns}}}dependentAssembly")
    ET.SubElement(
        dep_asm,
        f"{{{ns}}}assemblyIdentity",
        type="win32",
        name="FieldWorks.RegFree",
        version="1.0.0.0",
        processorArchitecture="amd64",
    )

    output_path = output_dir / f"{exe_name}.manifest"
    output_dir.mkdir(parents=True, exist_ok=True)

    tree = ET.ElementTree(root)
    if hasattr(ET, "indent"):
        ET.indent(tree, space="  ")

    tree.write(output_path, encoding="utf-8", xml_declaration=True)
    print(f"Generated {output_path}")


def main():
    csv_path = ARTIFACTS_DIR / "com_usage_report.csv"
    if not csv_path.exists():
        print("Report not found. Run audit_com_usage.py first.")
        return

    with open(csv_path, "r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            if row["UsesCOM"] == "True":
                exe = row["Executable"]
                generate_app_manifest(exe, OUTPUT_DIR)


if __name__ == "__main__":
    main()
