"""
Extract CLSIDs from source files and generate RegFree COM manifests for native DLLs.
"""

import re
import uuid
from pathlib import Path
from collections import defaultdict
import xml.etree.ElementTree as ET
from typing import Dict, List, Tuple

REPO_ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = REPO_ROOT / "Output" / "Debug"  # Default to Debug for now

# Map directory/filename to DLL name
DLL_MAP = {
    "views": "Views.dll",
    "Kernel": "FwKernel.dll",
}


def parse_guid_parts(parts: List[str]) -> str:
    """
    Convert split GUID parts to string format.
    Input: ['0xCF0F1C0B', '0x0E44', '0x4C1E', '0x99', '0x12', '0x20', '0x48', '0xED', '0x12', '0xC2', '0xB4']
    Output: '{CF0F1C0B-0E44-4C1E-9912-2048ED12C2B4}'
    """
    try:
        d1 = int(parts[0], 16)
        d2 = int(parts[1], 16)
        d3 = int(parts[2], 16)
        d4 = int(parts[3], 16)
        d5 = int(parts[4], 16)

        node_parts = [int(p, 16) for p in parts[5:]]
        node = 0
        for b in node_parts:
            node = (node << 8) | b

        g = uuid.UUID(fields=(d1, d2, d3, d4, d5, node))
        return f"{{{str(g).upper()}}}"
    except Exception:
        return ""


def extract_clsids(repo_root: Path) -> Dict[str, List[Tuple[str, str]]]:
    clsids = defaultdict(list)  # DLL -> [(Name, GUID)]
    seen_guids = defaultdict(set)  # DLL -> Set of GUIDs

    # 1. Scan IDH files (Mainly for Views)
    print("Scanning IDH files...")
    for idh in repo_root.glob("Src/**/*.idh"):
        content = idh.read_text(errors="ignore")
        # DeclareCoClass(Name, GUID)
        # GUID can be {GUID} or just GUID
        matches = re.findall(
            r"DeclareCoClass\s*\(\s*(\w+)\s*,\s*([0-9a-fA-F-]+)\s*\)", content
        )

        dll = None
        for key, val in DLL_MAP.items():
            if key in str(idh):
                dll = val
                break

        if dll and matches:
            for name, guid in matches:
                # Ensure GUID has braces
                if not guid.startswith("{"):
                    guid = f"{{{guid}}}"
                guid = guid.upper()

                if guid not in seen_guids[dll]:
                    clsids[dll].append((name, guid))
                    seen_guids[dll].add(guid)
                    print(f"  Found {name} in {dll}")

    # 2. Scan GUIDs.cpp files (Mainly for FwKernel)
    print("Scanning GUIDs.cpp files...")
    for cpp in repo_root.glob("Src/**/*_GUIDs.cpp"):
        content = cpp.read_text(errors="ignore")

        dll = None
        for key, val in DLL_MAP.items():
            if key in str(cpp):
                dll = val
                break

        print(f"  Mapped to {dll}")
        if not dll:
            continue

        # DEFINE_UUIDOF(Name, p1, p2, p3, ...)
        # Regex to capture Name and the rest of arguments
        matches = re.findall(r"DEFINE_UUIDOF\s*\(\s*(\w+)\s*,\s*([^)]+)\)", content)
        print(f"  Matches: {len(matches)}")
        for name, args in matches:
            # Skip Interfaces (start with I)
            if name.startswith("I") and name[1].isupper():
                continue

            parts = [p.strip() for p in args.split(",")]
            if len(parts) >= 11:
                guid = parse_guid_parts(parts)
                if guid:
                    if guid not in seen_guids[dll]:
                        clsids[dll].append((name, guid))
                        seen_guids[dll].add(guid)
                        print(f"  Found {name} in {dll}")

    return clsids


def generate_manifest(dll_name: str, classes: List[Tuple[str, str]], output_dir: Path):
    ns = "urn:schemas-microsoft-com:asm.v1"
    ET.register_namespace("", ns)

    root = ET.Element(f"{{{ns}}}assembly", manifestVersion="1.0")

    # Assembly Identity
    assembly_name = Path(dll_name).stem
    ET.SubElement(
        root,
        f"{{{ns}}}assemblyIdentity",
        type="win32",
        name=assembly_name,
        version="1.0.0.0",
    )

    # File element
    file_elem = ET.SubElement(root, f"{{{ns}}}file", name=dll_name)

    for name, guid in classes:
        # comClass element
        # We assume threadingModel="Apartment" for now
        ET.SubElement(
            file_elem,
            f"{{{ns}}}comClass",
            clsid=guid,
            threadingModel="Apartment",
            progid=f"{assembly_name}.{name}",
        )  # Guessing ProgID

    # Write to file
    output_path = output_dir / f"{dll_name}.manifest"
    output_dir.mkdir(parents=True, exist_ok=True)

    tree = ET.ElementTree(root)
    # Indent (requires Python 3.9+)
    if hasattr(ET, "indent"):
        ET.indent(tree, space="  ")

    tree.write(output_path, encoding="utf-8", xml_declaration=True)
    print(f"Generated {output_path}")


def main():
    clsids = extract_clsids(REPO_ROOT)

    for dll, classes in clsids.items():
        generate_manifest(dll, classes, OUTPUT_DIR)


if __name__ == "__main__":
    main()
