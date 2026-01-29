"""Conversion script to enforce CommonAssemblyInfoTemplate policy."""

from __future__ import annotations

import re
import logging
import shutil
import os
import csv
import xml.etree.ElementTree as ET

from pathlib import Path
from typing import List, Optional, Set, Dict, Any
from xml.etree.ElementTree import Comment

from . import cli_args, history_diff, git_restore
from .models import ManagedProject, RestoreInstruction
from .project_scanner import scan_projects, COMMON_INCLUDE_TOKEN

LOGGER = logging.getLogger(__name__)

# Namespace handling for MSBuild
MSBUILD_NS = "http://schemas.microsoft.com/developer/msbuild/2003"
ET.register_namespace("", MSBUILD_NS)

SANITIZED_ATTRIBUTES = {
    "AssemblyConfiguration",
    "AssemblyConfigurationAttribute",
    "AssemblyCompany",
    "AssemblyCompanyAttribute",
    "AssemblyProduct",
    "AssemblyProductAttribute",
    "AssemblyCopyright",
    "AssemblyCopyrightAttribute",
    "AssemblyTrademark",
    "AssemblyTrademarkAttribute",
    "AssemblyCulture",
    "AssemblyCultureAttribute",
    "AssemblyFileVersion",
    "AssemblyFileVersionAttribute",
    "AssemblyInformationalVersion",
    "AssemblyInformationalVersionAttribute",
    "AssemblyVersion",
    "AssemblyVersionAttribute",
    "AssemblyTitle",
    "AssemblyTitleAttribute",
    "AssemblyDescription",
    "AssemblyDescriptionAttribute",
    "AssemblyDelaySign",
    "AssemblyDelaySignAttribute",
    "AssemblyKeyFile",
    "AssemblyKeyFileAttribute",
    "AssemblyKeyName",
    "AssemblyKeyNameAttribute",
    "ComVisible",
    "ComVisibleAttribute",
    "System.Runtime.InteropServices.ComVisible",
    "System.Runtime.InteropServices.ComVisibleAttribute",
    "AssemblyMetadata",
    "AssemblyMetadataAttribute",
}


def main() -> None:
    parser = cli_args.build_common_parser(
        "Remediate projects to use CommonAssemblyInfoTemplate."
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Preview changes without modifying files.",
    )
    parser.add_argument(
        "--restore-missing",
        action="store_true",
        default=True,
        help="Attempt to restore missing AssemblyInfo files from git history.",
    )
    args = parser.parse_args()

    logging.basicConfig(level=args.log_level)
    repo_root: Path = args.repo_root.resolve()

    # 1. Scan projects to get current state
    LOGGER.info("Scanning projects...")
    projects = scan_projects(
        repo_root,
        release_ref=args.release_ref,
        enable_history=not args.skip_history,
    )

    # 2. Build restore map if needed
    restore_map: List[RestoreInstruction] = []
    if args.restore_missing and not args.skip_history:
        LOGGER.info("Building restore map from history...")
        try:
            restore_map = history_diff.build_restore_map(repo_root)
            LOGGER.info("Found %d files to restore.", len(restore_map))
        except Exception as e:
            LOGGER.warning("Failed to build restore map: %s", e)

    # Load decisions if present
    decisions: Dict[str, Dict[str, str]] = {}
    if args.decisions and args.decisions.exists():
        LOGGER.info("Loading decisions from %s", args.decisions)
        with args.decisions.open("r", encoding="utf-8") as f:
            reader = csv.DictReader(f)
            for row in reader:
                decisions[row["project_id"]] = row

    # 3. Process projects
    modified_count = 0
    restored_count = 0
    sanitized_count = 0

    for project in projects:
        decision = decisions.get(project.project_id)

        if project.category == "C" and not _needs_generate_false_fix(project):
            # Even if compliant with GenerateAssemblyInfo, we might need to sanitize attributes
            pass

        LOGGER.info("Processing %s (%s)", project.project_id, project.category)

        if args.dry_run:
            continue

        # Restore files if applicable
        if args.restore_missing:
            restored = _restore_files_for_project(repo_root, project, restore_map)
            if restored:
                restored_count += restored

        # Sanitize existing files
        for asm_file in project.assembly_info_files:
            if _sanitize_assembly_info(asm_file.path):
                sanitized_count += 1

        # Modify .csproj
        if _remediate_csproj(repo_root, project, decision):
            modified_count += 1

    LOGGER.info(
        "Conversion complete. Modified %d projects, restored %d files, sanitized %d files.",
        modified_count,
        restored_count,
        sanitized_count,
    )


def _sanitize_assembly_info(file_path: Path) -> bool:
    if not file_path.exists():
        return False

    content = None
    encoding = "utf-8"

    try:
        content = file_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        try:
            content = file_path.read_text(encoding="utf-8-sig")
            encoding = "utf-8-sig"
        except UnicodeDecodeError:
            try:
                content = file_path.read_text(encoding="latin-1")
                encoding = "latin-1"
            except Exception:
                LOGGER.warning("Could not read %s, skipping sanitization.", file_path)
                return False

    new_lines = []
    changed = False

    for line in content.splitlines():
        # Check if line contains one of the attributes
        # Regex: ^\s*\[assembly:\s*(AttributeName)\s*\(
        match = re.match(r"^\s*\[assembly:\s*([\w.]+)", line)
        if match:
            attr_name = match.group(1)
            if attr_name in SANITIZED_ATTRIBUTES:
                new_lines.append(
                    f"// {line} // Sanitized by convert_generate_assembly_info"
                )
                changed = True
                continue
        new_lines.append(line)

    if changed:
        file_path.write_text("\n".join(new_lines), encoding=encoding)
        return True
    return False


def _needs_generate_false_fix(project: ManagedProject) -> bool:
    return project.generate_assembly_info_value is not False


def _restore_files_for_project(
    repo_root: Path, project: ManagedProject, restore_map: List[RestoreInstruction]
) -> int:
    count = 0
    project_dir = project.path.parent

    for instr in restore_map:
        # Check if the file belongs to this project
        instr_path = Path(instr.relative_path)
        abs_instr_path = repo_root / instr_path

        try:
            # Check if instr_path is inside project_dir
            abs_instr_path.relative_to(project_dir)

            # Check for nested projects (stop if another csproj is found in the path)
            is_nested = False
            current = abs_instr_path.parent
            while current != project_dir and current != repo_root:
                # We are looking for ANY csproj in the intermediate directories
                if list(current.glob("*.csproj")):
                    is_nested = True
                    break
                current = current.parent

            if is_nested:
                continue

            target_path = abs_instr_path
            if target_path.exists():
                continue

            LOGGER.info("Restoring %s from %s", instr.relative_path, instr.commit_sha)
            try:
                # Use parent commit because instr.commit_sha is the deletion commit
                git_restore.restore_file(
                    repo_root, target_path, f"{instr.commit_sha}~1"
                )
                count += 1
                _sanitize_assembly_info(target_path)
            except Exception as e:
                LOGGER.error("Failed to restore %s: %s", instr.relative_path, e)

        except ValueError:
            # Not inside this project
            continue

    return count


def _remediate_csproj(
    repo_root: Path, project: ManagedProject, decision: Optional[Dict[str, str]] = None
) -> bool:
    """Apply fixes to the .csproj file."""
    # We use a simple text-based approach for robustness with comments/formatting,
    # or fallback to ET if needed.
    # But for adding Compile links and PropertyGroup elements, ET is safer for structure.
    # Let's try ET first.

    try:
        tree = ET.parse(project.path)
        root = tree.getroot()

        changed = False

        # 1. Enforce GenerateAssemblyInfo = false
        changed |= _enforce_generate_assembly_info(root)

        # 2. Ensure CommonAssemblyInfo.cs link
        changed |= _ensure_common_link(root, project, repo_root)

        # 3. Normalize Compile includes
        changed |= _normalize_compile_includes(root)

        # 4. Scaffold if needed
        if decision and "scaffold" in decision.get("notes", "").lower():
            changed |= _scaffold_assembly_info(repo_root, project, root)

        if changed:
            # Write back
            # ET in Python 3.8 doesn't support indent well without tweaks.
            # And it might strip comments.
            # Let's try to write to a temp file and see.
            # Actually, for this task, maybe we should use a custom writer or just accept some formatting changes.
            # Or use the 'indent' function if available (Python 3.9+).
            if hasattr(ET, "indent"):
                ET.indent(tree, space="  ", level=0)

            tree.write(project.path, encoding="utf-8", xml_declaration=True)
            return True

    except Exception as e:
        LOGGER.error("Failed to update %s: %s", project.path, e)
        return False

    return False


def _enforce_generate_assembly_info(root: ET.Element) -> bool:
    # Find existing PropertyGroups
    # Check if GenerateAssemblyInfo exists
    ns = "{" + MSBUILD_NS + "}" if root.tag.startswith("{") else ""
    comment_text = " Using CommonAssemblyInfoTemplate; prevent SDK duplication. "

    prop_groups = root.findall(f"{ns}PropertyGroup")
    for pg in prop_groups:
        gai = pg.find(f"{ns}GenerateAssemblyInfo")
        if gai is not None:
            if gai.text != "false":
                gai.text = "false"
                return True
            return False  # Already false

    # If not found, add to the first PropertyGroup
    if prop_groups:
        pg = prop_groups[0]
        # Insert comment before GenerateAssemblyInfo
        # Note: ElementTree doesn't support inserting comments easily before a specific element
        # if we are appending. But we can append the comment then the element.

        comment = Comment(comment_text)
        pg.append(comment)

        gai = ET.SubElement(pg, f"{ns}GenerateAssemblyInfo")
        gai.text = "false"
        return True

    return False


def _normalize_compile_includes(root: ET.Element) -> bool:
    """Ensure custom AssemblyInfo files are not included multiple times."""
    ns = "{" + MSBUILD_NS + "}" if root.tag.startswith("{") else ""
    changed = False

    # Track seen includes to detect duplicates
    seen_includes = set()
    items_to_remove = []

    for ig in root.findall(f"{ns}ItemGroup"):
        for compile_item in ig.findall(f"{ns}Compile"):
            include = compile_item.get("Include", "")
            if not include:
                continue

            # Normalize path separators for comparison
            norm_include = include.replace("/", "\\").lower()

            # We only care about AssemblyInfo files or CommonAssemblyInfo
            if "assemblyinfo" in norm_include:
                if norm_include in seen_includes:
                    items_to_remove.append((ig, compile_item))
                    changed = True
                else:
                    seen_includes.add(norm_include)

    for ig, item in items_to_remove:
        ig.remove(item)
        # If ItemGroup is empty, we could remove it, but let's leave it for now

    return changed


def _scaffold_assembly_info(repo_root: Path, project: ManagedProject, root: ET.Element) -> bool:
    """Scaffold a minimal AssemblyInfo file if requested."""
    # Determine path: Properties/AssemblyInfo.<ProjectName>.cs
    # Or just Properties/AssemblyInfo.cs if it doesn't exist

    props_dir = project.path.parent / "Properties"
    if not props_dir.exists():
        props_dir.mkdir()

    # Try to find a good name
    proj_name = project.path.stem
    asm_filename = f"AssemblyInfo.{proj_name}.cs"
    asm_path = props_dir / asm_filename

    if asm_path.exists():
        return False # Already exists

    # Create file content
    content = """using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
"""
    asm_path.write_text(content, encoding="utf-8")
    LOGGER.info("Scaffolded %s", asm_path)

    # Add to csproj
    # We need to add <Compile Include="Properties\AssemblyInfo.<ProjectName>.cs" />
    # But usually SDK style projects include *.cs by default.
    # If EnableDefaultCompileItems is true (default), we don't need to add it.
    # But if it's false, we do.
    # Let's check if we need to add it.
    # For now, assume SDK style handles it, or if we are in a mixed mode, we might need it.
    # But wait, if we are converting to SDK style, we usually rely on globbing.
    # However, FieldWorks might have EnableDefaultCompileItems=false.

    # Let's check if EnableDefaultCompileItems is false
    ns = "{" + MSBUILD_NS + "}" if root.tag.startswith("{") else ""
    enable_default = True
    for pg in root.findall(f"{ns}PropertyGroup"):
        edci = pg.find(f"{ns}EnableDefaultCompileItems")
        if edci is not None and edci.text.lower() == "false":
            enable_default = False
            break

    if not enable_default:
        # Add Compile item
        rel_path = f"Properties\\{asm_filename}"

        # Find ItemGroup
        item_groups = root.findall(f"{ns}ItemGroup")
        target_ig = None
        for ig in item_groups:
            if ig.find(f"{ns}Compile") is not None:
                target_ig = ig
                break

        if target_ig is None:
            target_ig = ET.SubElement(root, f"{ns}ItemGroup")

        compile_elem = ET.SubElement(target_ig, f"{ns}Compile")
        compile_elem.set("Include", rel_path)
        return True

    return True # File created, so changed is True (even if csproj not touched)



def _ensure_common_link(
    root: ET.Element, project: ManagedProject, repo_root: Path
) -> bool:
    ns = "{" + MSBUILD_NS + "}" if root.tag.startswith("{") else ""

    # Check if already linked
    for compile_item in root.findall(f".//{ns}Compile"):
        include = compile_item.get("Include", "")
        if COMMON_INCLUDE_TOKEN in include:
            return False  # Already present

    # Calculate relative path
    # Src/CommonAssemblyInfo.cs
    common_path = repo_root / "Src" / "CommonAssemblyInfo.cs"
    try:
        rel_path_str = os.path.relpath(common_path, project.path.parent)
    except ValueError:
        # Should not happen for projects under Src
        LOGGER.warning(
            "Could not calculate relative path to CommonAssemblyInfo.cs for %s",
            project.path,
        )
        return False

    rel_path_str = rel_path_str.replace("/", "\\")

    # Add to an ItemGroup
    # Try to find an ItemGroup with Compile items
    item_groups = root.findall(f"{ns}ItemGroup")
    target_ig = None
    for ig in item_groups:
        if ig.find(f"{ns}Compile") is not None:
            target_ig = ig
            break

    if target_ig is None:
        # Create new ItemGroup
        target_ig = ET.SubElement(root, f"{ns}ItemGroup")

    compile_elem = ET.SubElement(target_ig, f"{ns}Compile")
    compile_elem.set("Include", rel_path_str)

    link_elem = ET.SubElement(compile_elem, f"{ns}Link")
    link_elem.text = r"Properties\CommonAssemblyInfo.cs"

    return True


if __name__ == "__main__":
    main()
