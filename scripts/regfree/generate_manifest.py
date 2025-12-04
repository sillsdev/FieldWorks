import json
import os
import xml.dom.minidom


def generate_manifest(json_path, output_path):
    with open(json_path, "r") as f:
        data = json.load(f)

    # Group by DLL
    dlls = {}
    for name, info in data.items():
        if info["type"] == "Class":
            dll = info["dll"]
            if dll not in dlls:
                dlls[dll] = []
            dlls[dll].append(
                {
                    "name": name,
                    "guid": info["guid"],
                    "progid": info["progid"],
                    "threadingModel": info["threadingModel"],
                }
            )

    # Create XML
    doc = xml.dom.minidom.Document()
    assembly = doc.createElement("assembly")
    assembly.setAttribute("xmlns", "urn:schemas-microsoft-com:asm.v1")
    assembly.setAttribute("manifestVersion", "1.0")
    doc.appendChild(assembly)

    # Add assemblyIdentity (placeholder)
    identity = doc.createElement("assemblyIdentity")
    identity.setAttribute("name", "FieldWorks.RegFree")
    identity.setAttribute("version", "1.0.0.0")
    identity.setAttribute("type", "win32")
    identity.setAttribute(
        "processorArchitecture", "amd64"
    )  # Assuming x64 based on build instructions
    assembly.appendChild(identity)

    for dll_name, classes in dlls.items():
        file_elem = doc.createElement("file")
        file_elem.setAttribute("name", dll_name)

        for cls in classes:
            com_class = doc.createElement("comClass")
            com_class.setAttribute("clsid", cls["guid"])
            com_class.setAttribute("threadingModel", cls["threadingModel"])

            if cls["progid"]:
                com_class.setAttribute("progid", cls["progid"])

            com_class.setAttribute("description", cls["name"])
            file_elem.appendChild(com_class)

        assembly.appendChild(file_elem)

    with open(output_path, "w") as f:
        f.write(doc.toprettyxml(indent="  "))

    print(f"Generated manifest at {output_path}")


if __name__ == "__main__":
    base_dir = os.getcwd()
    json_path = os.path.join(base_dir, "scripts", "regfree", "com_guids.json")
    output_path = os.path.join(
        base_dir, "scripts", "regfree", "FieldWorks.regfree.manifest"
    )

    if os.path.exists(json_path):
        generate_manifest(json_path, output_path)
    else:
        print(f"JSON file not found: {json_path}")
