from audit_framework import (
    ConvergenceAuditor,
    ConvergenceConverter,
    ConvergenceValidator,
    parse_csproj,
    update_csproj,
)

TARGET_PACKAGES = [
    "SIL.LCModel.Core.Tests",
    "SIL.LCModel.Tests",
    "SIL.LCModel.Utils.Tests",
]


class PrivateAssetsAuditor(ConvergenceAuditor):
    """Audit PrivateAssets settings on LCM test packages"""

    def analyze_project(self, project_path):
        """Check for missing PrivateAssets on target packages"""
        # Only interested in test projects
        if not project_path.name.endswith("Tests.csproj"):
            return None

        try:
            tree = parse_csproj(project_path)
        except Exception as e:
            print(f"Error parsing {project_path}: {e}")
            return None

        root = tree.getroot()
        ns = {"": "http://schemas.microsoft.com/developer/msbuild/2003"}

        # Find all PackageReferences
        # Handle both with and without namespace (SDK style vs legacy)
        refs = []
        for item in root.findall(".//PackageReference", ns) + root.findall(
            ".//PackageReference"
        ):
            refs.append(item)

        missing_packages = []

        for ref in refs:
            include = ref.get("Include")
            if include in TARGET_PACKAGES:
                private_assets = ref.get("PrivateAssets")
                if private_assets != "All":
                    missing_packages.append(include)

        if not missing_packages:
            return None

        return {
            "ProjectPath": str(project_path),
            "ProjectName": project_path.stem,
            "PackagesMissingPrivateAssets": ";".join(missing_packages),
            "Action": "AddPrivateAssets",
        }

    def print_custom_summary(self):
        """Print summary"""
        print(f"\nProjects with missing PrivateAssets: {len(self.results)}")
        for r in self.results:
            print(f"  - {r['ProjectName']}: {r['PackagesMissingPrivateAssets']}")


class PrivateAssetsConverter(ConvergenceConverter):
    """Add PrivateAssets="All" to target packages"""

    def convert_project(self, project_path, **kwargs):
        """Add PrivateAssets attribute"""
        if kwargs.get("Action") != "AddPrivateAssets":
            return

        try:
            tree = parse_csproj(project_path)
        except Exception as e:
            print(f"Error parsing {project_path}: {e}")
            return

        root = tree.getroot()
        changed = False

        packages_to_fix = kwargs.get("PackagesMissingPrivateAssets", "").split(";")

        ns = {"": "http://schemas.microsoft.com/developer/msbuild/2003"}

        # Find and update PackageReferences
        for item in root.findall(".//PackageReference", ns) + root.findall(
            ".//PackageReference"
        ):
            include = item.get("Include")
            if include in packages_to_fix:
                if item.get("PrivateAssets") != "All":
                    item.set("PrivateAssets", "All")
                    changed = True

        if changed:
            update_csproj(project_path, tree)
            print(f"✓ Added PrivateAssets='All' to {project_path.name}")


class PrivateAssetsValidator(ConvergenceValidator):
    """Validate PrivateAssets settings"""

    def validate_project(self, project_path):
        """Validate a single project"""
        # Only interested in test projects
        if not project_path.name.endswith("Tests.csproj"):
            return []

        violations = []
        try:
            tree = parse_csproj(project_path)
        except Exception as e:
            return [f"Error parsing {project_path}: {e}"]

        root = tree.getroot()
        ns = {"": "http://schemas.microsoft.com/developer/msbuild/2003"}

        for item in root.findall(".//PackageReference", ns) + root.findall(
            ".//PackageReference"
        ):
            include = item.get("Include")
            if include in TARGET_PACKAGES:
                private_assets = item.get("PrivateAssets")
                if private_assets != "All":
                    violations.append(
                        f"{project_path.name}: Package '{include}' missing PrivateAssets='All' (found '{private_assets}')"
                    )

        return violations
