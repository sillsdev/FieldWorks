import os
import re
import json
import glob


def extract_guids_from_cpp(file_path):
    guids = {}
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
        # Pattern for DEFINE_UUIDOF(name, l, w1, w2, b1, b2, b3, b4, b5, b6, b7, b8);
        pattern = re.compile(
            r"DEFINE_UUIDOF\s*\(\s*(\w+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*,\s*(0x[0-9a-fA-F]+)\s*\);"
        )

        for match in pattern.finditer(content):
            name = match.group(1)
            parts = [match.group(i) for i in range(2, 13)]
            # Format as {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}
            guid_str = "{{{:08x}-{:04x}-{:04x}-{:02x}{:02x}-{:02x}{:02x}{:02x}{:02x}{:02x}{:02x}}}".format(
                int(parts[0], 16),
                int(parts[1], 16),
                int(parts[2], 16),
                int(parts[3], 16),
                int(parts[4], 16),
                int(parts[5], 16),
                int(parts[6], 16),
                int(parts[7], 16),
                int(parts[8], 16),
                int(parts[9], 16),
                int(parts[10], 16),
            )
            guids[name] = guid_str.upper()
    return guids


def parse_generic_factory(root_dir, guids_map):
    # Map CLSID_Name -> {progid, threadingModel}
    factory_info = {}

    # Regex to match GenericFactory instantiation
    # static GenericFactory g_fact(_T("SIL.Views.VwRootBox"), &CLSID_VwRootBox, _T("SIL Root Box"), _T("Apartment"), ...);
    # We need to be flexible with whitespace and _T() macros

    # Capture group 1: ProgID
    # Capture group 2: CLSID variable name (without &)
    # Capture group 3: Description (ignored)
    # Capture group 4: ThreadingModel

    pattern = re.compile(
        r'static\s+GenericFactory\s+\w+\s*\(\s*(?:_T\(")?([^")]+)(?:"\))?\s*,\s*&(\w+)\s*,\s*(?:_T\(")?([^")]+)(?:"\))?\s*,\s*(?:_T\(")?([^")]+)(?:"\))?'
    )

    for root, dirs, files in os.walk(root_dir):
        for file in files:
            if file.endswith(".cpp"):
                file_path = os.path.join(root, file)
                with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                    content = f.read()
                    for match in pattern.finditer(content):
                        progid = match.group(1)
                        clsid_var = match.group(2)
                        threading_model = match.group(4)

                        factory_info[clsid_var] = {
                            "progid": progid,
                            "threadingModel": threading_model,
                        }

    return factory_info


def main():
    base_dir = os.getcwd()
    src_dir = os.path.join(base_dir, "Src")

    # 1. Extract GUIDs
    all_guids = {}

    # FwKernel GUIDs
    fwkernel_guids_path = os.path.join(src_dir, "Kernel", "FwKernel_GUIDs.cpp")
    if os.path.exists(fwkernel_guids_path):
        print(f"Extracting from {fwkernel_guids_path}...")
        guids = extract_guids_from_cpp(fwkernel_guids_path)
        for name, guid in guids.items():
            all_guids[name] = {"guid": guid, "dll": "FwKernel.dll"}

    # Views GUIDs
    views_guids_path = os.path.join(src_dir, "views", "Views_GUIDs.cpp")
    if os.path.exists(views_guids_path):
        print(f"Extracting from {views_guids_path}...")
        guids = extract_guids_from_cpp(views_guids_path)
        for name, guid in guids.items():
            all_guids[name] = {"guid": guid, "dll": "Views.dll"}

    # ViewsExtra GUIDs
    views_extra_guids_path = os.path.join(src_dir, "views", "ViewsExtra_GUIDs.cpp")
    if os.path.exists(views_extra_guids_path):
        print(f"Extracting from {views_extra_guids_path}...")
        guids = extract_guids_from_cpp(views_extra_guids_path)
        for name, guid in guids.items():
            all_guids[name] = {"guid": guid, "dll": "Views.dll"}

    print(f"Found {len(all_guids)} GUIDs.")

    # 2. Parse GenericFactory usage to get ProgIDs and ThreadingModels
    print("Scanning for GenericFactory usage...")
    factory_info = parse_generic_factory(src_dir, all_guids)
    print(f"Found {len(factory_info)} GenericFactory instantiations.")

    # Overrides for special cases (Managed classes, Linux-only, Internal)
    overrides = {
        "PictureFactory": {
            "progid": "SIL.FieldWorks.Common.FwUtils.ManagedPictureFactory",
            "threadingModel": "Both",
            "type": "Class",
        },
        "VwWindow": {"ignore": True},
        "VwEnv": {"ignore": True},
        "VwTextSelection": {"ignore": True},
        "VwPictureSelection": {"ignore": True},
        "VwTextStore": {"ignore": True},
        "UniscribeSegment": {"ignore": True},
        "GraphiteSegment": {"ignore": True},
        "VwGraphicsWin32": {
            "progid": "SIL.Text.VwGraphicsWin32",
            "threadingModel": "Apartment",
            "type": "Class",
        },
    }

    # 3. Merge info
    results = {}
    for name, info in all_guids.items():
        guid = info["guid"]
        dll = info["dll"]

        # Check overrides first
        if name in overrides:
            if overrides[name].get("ignore"):
                continue
            if "progid" in overrides[name]:
                progid = overrides[name]["progid"]
                threading_model = overrides[name].get("threadingModel", "Apartment")
                type_ = overrides[name].get("type", "Class")
                results[name] = {
                    "guid": guid,
                    "type": type_,
                    "dll": dll,
                    "progid": progid,
                    "threadingModel": threading_model,
                }
                continue

        # Determine type (Class vs Interface)
        # Heuristic: If it starts with I, it's an Interface. Else Class.
        # Also check if we found factory info for it.

        type_ = "Class"
        if name.startswith("I") and name[1].isupper():
            type_ = "Interface"

        # Check if we have factory info (which confirms it's a Class)
        # The variable name in GenericFactory might match 'name' or 'CLSID_name'

        progid = None
        threading_model = "Apartment"  # Default

        # Try exact match
        if name in factory_info:
            progid = factory_info[name]["progid"]
            threading_model = factory_info[name]["threadingModel"]
            type_ = "Class"
        # Try with CLSID_ prefix
        elif f"CLSID_{name}" in factory_info:
            progid = factory_info[f"CLSID_{name}"]["progid"]
            threading_model = factory_info[f"CLSID_{name}"]["threadingModel"]
            type_ = "Class"
        # Try removing CLSID_ prefix if present in name
        elif name.startswith("CLSID_") and name[6:] in factory_info:
            progid = factory_info[name[6:]]["progid"]
            threading_model = factory_info[name[6:]]["threadingModel"]
            type_ = "Class"

        results[name] = {
            "guid": guid,
            "type": type_,
            "dll": dll,
            "progid": progid,
            "threadingModel": threading_model,
        }

    # Save to JSON
    output_path = os.path.join(base_dir, "scripts", "regfree", "com_guids.json")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        json.dump(results, f, indent=2)

    print(f"Saved {len(results)} entries to {output_path}")


if __name__ == "__main__":
    main()
