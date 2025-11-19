from audit_framework import (
    ConvergenceAuditor,
    ConvergenceConverter,
    ConvergenceValidator,
    parse_csproj,
    find_property_value,
    update_csproj,
)


class PlatformTargetAuditor(ConvergenceAuditor):
    """Audit PlatformTarget settings"""

    def analyze_project(self, project_path):
        """Check for explicit PlatformTarget"""
        try:
            tree = parse_csproj(project_path)
        except Exception as e:
            print(f"Error parsing {project_path}: {e}")
            return None

        platform_target = find_property_value(tree, "PlatformTarget")

        if not platform_target:
            return None  # Inheriting from Directory.Build.props

        # Check OutputType to determine if exception is justified
        output_type = find_property_value(tree, "OutputType")

        is_exception = platform_target == "AnyCPU" and "BuildTasks" in project_path.stem

        return {
            "ProjectPath": str(project_path),
            "ProjectName": project_path.stem,
            "PlatformTarget": platform_target,
            "OutputType": output_type or "Library",
            "IsException": is_exception,
            "Action": "Keep" if is_exception else "Remove",
        }

    def print_custom_summary(self):
        """Print summary"""
        remove_count = sum(1 for r in self.results if r["Action"] == "Remove")
        keep_count = sum(1 for r in self.results if r["Action"] == "Keep")

        print(f"\nProjects with explicit PlatformTarget: {len(self.results)}")
        print(f"  - Should remove (redundant x64): {remove_count}")
        print(f"  - Should keep (justified exceptions): {keep_count}")


class PlatformTargetConverter(ConvergenceConverter):
    """Remove redundant PlatformTarget settings"""

    def convert_project(self, project_path, **kwargs):
        """Remove explicit PlatformTarget if redundant"""
        if kwargs.get("Action") != "Remove":
            return

        try:
            tree = parse_csproj(project_path)
        except Exception as e:
            print(f"Error parsing {project_path}: {e}")
            return

        root = tree.getroot()
        changed = False

        # Find and remove PlatformTarget
        # Handle namespace
        ns = {"": "http://schemas.microsoft.com/developer/msbuild/2003"}

        # Look in all PropertyGroups
        for prop_group in root.findall(".//PropertyGroup", ns) + root.findall(
            ".//PropertyGroup"
        ):
            # Try with namespace
            platform_target = prop_group.find("PlatformTarget", ns)
            if platform_target is None:
                # Try without namespace
                platform_target = prop_group.find("PlatformTarget")

            if platform_target is not None and platform_target.text == "x64":
                prop_group.remove(platform_target)
                changed = True

        if changed:
            update_csproj(project_path, tree)
            print(f"✓ Removed redundant PlatformTarget from {project_path.name}")


class PlatformTargetValidator(ConvergenceValidator):
    """Validate PlatformTarget settings"""

    def validate_project(self, project_path):
        """Validate a single project"""
        violations = []
        try:
            tree = parse_csproj(project_path)
        except Exception as e:
            return [f"Error parsing {project_path}: {e}"]

        platform_target = find_property_value(tree, "PlatformTarget")

        if platform_target == "x64":
            violations.append(
                f"{project_path.name}: Explicit PlatformTarget=x64 found (should be inherited)"
            )

        if platform_target == "AnyCPU" and "BuildTasks" not in project_path.stem:
            violations.append(
                f"{project_path.name}: Explicit PlatformTarget=AnyCPU found in non-BuildTasks project"
            )

        return violations
