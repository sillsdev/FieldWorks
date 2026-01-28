from __future__ import annotations

from pathlib import Path
from typing import Dict, Iterable, List, Tuple
from xml.etree import ElementTree as ET

from .models import ExclusionRule, ExclusionScope, RuleSource

# MSBuild project files often use the legacy namespace. We keep the helper
# generic by detecting the namespace dynamically and mirroring it for new
# nodes so the resulting XML stays consistent with existing files.


def _detect_namespace(tag: str) -> str:
    if tag.startswith("{") and "}" in tag:
        return tag[1 : tag.index("}")]
    return ""


def _qualify(tag: str, namespace: str) -> str:
    return f"{{{namespace}}}{tag}" if namespace else tag


def load_project(path: Path) -> ET.ElementTree:
    """Load an MSBuild project from disk."""

    return ET.parse(path)


def save_project(tree: ET.ElementTree, path: Path) -> None:
    """Persist the given tree back to disk using UTF-8 encoding."""

    tree.write(path, encoding="utf-8", xml_declaration=True)


def read_exclusion_rules(path: Path) -> List[ExclusionRule]:
    """Return all `<Compile Remove>`/`<None Remove>` rules for a project."""

    tree = load_project(path)
    root = tree.getroot()
    namespace = _detect_namespace(root.tag)
    compile_tag = _qualify("Compile", namespace)
    none_tag = _qualify("None", namespace)

    rules: Dict[str, ExclusionRule] = {}

    for item_group in root.findall(f".//{_qualify('ItemGroup', namespace)}"):
        for element in list(item_group):
            if element.tag not in {compile_tag, none_tag}:
                continue
            pattern = element.attrib.get("Remove")
            if not pattern:
                continue
            scope = (
                ExclusionScope.COMPILE
                if element.tag == compile_tag
                else ExclusionScope.NONE
            )
            rule = rules.get(pattern)
            if rule:
                if rule.scope != scope:
                    rule.scope = ExclusionScope.BOTH
                continue
            rules[pattern] = ExclusionRule(
                project_name=path.stem,
                pattern=pattern.replace("\\", "/"),
                scope=scope,
                source=RuleSource.EXPLICIT,
                covers_nested=pattern.endswith("/**"),
            )
    return list(rules.values())


def ensure_explicit_exclusion(
    path: Path,
    pattern: str,
    include_compile: bool = True,
    include_none: bool = True,
) -> None:
    """Insert or update a `<Compile Remove>`/`<None Remove>` block."""

    tree = load_project(path)
    root = tree.getroot()
    namespace = _detect_namespace(root.tag)
    item_group_tag = _qualify("ItemGroup", namespace)
    compile_tag = _qualify("Compile", namespace)
    none_tag = _qualify("None", namespace)

    item_group = None
    for group in root.findall(f".//{item_group_tag}"):
        _remove_existing_entries(group, pattern, {compile_tag, none_tag})
        if item_group is None:
            item_group = group

    if item_group is None:
        item_group = ET.SubElement(root, item_group_tag)

    if include_compile:
        compile_element = ET.SubElement(item_group, compile_tag)
        compile_element.set("Remove", pattern)
    if include_none:
        none_element = ET.SubElement(item_group, none_tag)
        none_element.set("Remove", pattern)

    save_project(tree, path)


def _remove_existing_entries(
    item_group: ET.Element, pattern: str, tag_pool: Iterable[str]
) -> None:
    targets = [
        element
        for element in item_group
        if element.tag in tag_pool and element.attrib.get("Remove") == pattern
    ]
    for element in targets:
        item_group.remove(element)


def remove_exclusion(path: Path, pattern: str) -> None:
    """Remove all `<Compile Remove>`/`<None Remove>` entries matching the pattern."""

    tree = load_project(path)
    root = tree.getroot()
    namespace = _detect_namespace(root.tag)
    item_group_tag = _qualify("ItemGroup", namespace)
    compile_tag = _qualify("Compile", namespace)
    none_tag = _qualify("None", namespace)

    changed = False
    for group in root.findall(f".//{item_group_tag}"):
        before = len(group)
        _remove_existing_entries(group, pattern, {compile_tag, none_tag})
        if len(group) != before:
            changed = True

    if changed:
        save_project(tree, path)
