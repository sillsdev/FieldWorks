#!/usr/bin/env python3
"""Remove explicit <Compile Include="...AssemblyInfoForTests.cs" /> entries from test csproj files.

Usage:
  python scripts/tools/remove_duplicate_assemblyinfo.py --dry-run
  python scripts/tools/remove_duplicate_assemblyinfo.py --apply

By default it searches under Src/** for .csproj files that include "AssemblyInfoForTests.cs" and reports them.
With --apply it makes an in-place backup (filename.bak) and removes the offending <Compile> elements.
"""
import argparse
import xml.etree.ElementTree as ET
from pathlib import Path
import sys


def find_csproj_files(root: Path):
    return list(root.glob('Src/**/*.csproj'))


def has_duplicate_compile(root: Path, tree: ET.ElementTree):
    root_el = tree.getroot()
    ns = {'msb': root_el.tag.split('}')[0].strip('{')} if '}' in root_el.tag else {}
    found = []
    for compile_el in root_el.findall('.//Compile', ns):
        inc = compile_el.get('Include')
        if inc and inc.endswith('AssemblyInfoForTests.cs'):
            found.append((inc, ET.tostring(compile_el, encoding='unicode').strip()))
    return found


def remove_compile_entries(path: Path, tree: ET.ElementTree):
    root_el = tree.getroot()
    ns = {'msb': root_el.tag.split('}')[0].strip('{')} if '}' in root_el.tag else {}
    changed = False
    # find ItemGroup/Compile elements with Include ending with AssemblyInfoForTests.cs
    for itemgroup in root_el.findall('.//ItemGroup', ns):
        to_remove = []
        for compile_el in itemgroup.findall('Compile', ns):
            inc = compile_el.get('Include')
            if inc and inc.replace('/', '\\').endswith('AssemblyInfoForTests.cs'):
                to_remove.append(compile_el)
        for el in to_remove:
            itemgroup.remove(el)
            changed = True
    return changed


def backup_file(path: Path):
    bak = path.with_suffix(path.suffix + '.bak')
    if not bak.exists():
        bak.write_bytes(path.read_bytes())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default='.', help='Repo root (default: .)')
    parser.add_argument('--apply', action='store_true', help='Apply changes (default is dry-run)')
    args = parser.parse_args()

    root = Path(args.root).resolve()
    csproj_files = find_csproj_files(root)
    if not csproj_files:
        print('No .csproj files found under Src/ - nothing to do')
        return 0

    report = []
    for csproj in csproj_files:
        try:
            tree = ET.parse(csproj)
        except Exception as e:
            # skip projects that are not well-formed XML
            print(f'SKIP (parse error): {csproj} -> {e}')
            continue

        matches = has_duplicate_compile(csproj, tree)
        if matches:
            report.append((csproj, matches))
            if args.apply:
                backup_file(csproj)
                changed = remove_compile_entries(csproj, tree)
                if changed:
                    # write back with pretty formatting
                    tree.write(csproj, encoding='utf-8', xml_declaration=True)
                    print(f'UPDATED: {csproj} (removed {len(matches)} Compile entries)')
                else:
                    print(f'NO CHANGE: {csproj} (found entries but none removed)')

    if not args.apply:
        if not report:
            print('No explicit AssemblyInfoForTests.cs Compile includes found (dry-run)')
        else:
            print('Projects with explicit AssemblyInfoForTests.cs includes:')
            for csproj, matches in report:
                print(f' - {csproj} -> {len(matches)} entry(s)')
        print('\nUse --apply to make edits (creates .bak backup).')
    else:
        if not report:
            print('Nothing to change.')

    return 0


if __name__ == '__main__':
    sys.exit(main())
