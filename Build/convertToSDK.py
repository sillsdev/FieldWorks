#!/usr/bin/env python3
"""
convertToSDK - Convert FieldWorks .csproj files from traditional to SDK format

This script converts all traditional .csproj files in the FieldWorks repository
to the new SDK format, handling package references, project references, and
preserving important properties.
"""

import os
import sys
import xml.etree.ElementTree as ET
import re
from pathlib import Path
import subprocess
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class SDKConverter:
    def __init__(self, repo_root):
        self.repo_root = Path(repo_root)
        self.common_packages = self._load_packages_config("Build/nuget-common/packages.config")
        self.windows_packages = self._load_packages_config("Build/nuget-windows/packages.config")
        self.all_packages = {**self.common_packages, **self.windows_packages}
        self.converted_projects = []
        self.failed_projects = []

        # Define packages that should be excluded or handled specially
        self.excluded_packages = {
            'Microsoft.Net.Client.3.5', 'Microsoft.Net.Framework.3.5.SP1',
            'Microsoft.Windows.Installer.3.1'
        }

        # SIL.Core version mapping - prefer the newer version
        self.sil_core_version = "15.0.0-beta0117"

        # Build maps for intelligent reference resolution
        self.assembly_to_project_map = {}  # assembly name -> project path
        self.package_names = set(self.all_packages.keys())  # set of package names for quick lookup

        # Build the assembly-to-project mapping on initialization
        self._build_assembly_project_map()

    def _build_assembly_project_map(self):
        """First pass: scan all project files to map assembly names to project paths"""
        logger.info("Building assembly-to-project mapping...")

        # Only search in specific subdirectories of the repo
        search_dirs = ['Src', 'Lib', 'Build']

        for search_dir in search_dirs:
            search_path = self.repo_root / search_dir
            if not search_path.exists():
                continue

            for root, dirs, files in os.walk(search_path):
                # Filter out excluded directories from the search
                dirs[:] = [d for d in dirs]

                for file in files:
                    if file.endswith('.csproj'):
                        csproj_path = Path(root) / file
                        try:
                            assembly_name = self._extract_assembly_name(csproj_path)
                            if assembly_name:
                                # Store relative path from repo root
                                rel_path = csproj_path.relative_to(self.repo_root)
                                self.assembly_to_project_map[assembly_name] = str(rel_path)
                                logger.debug(f"Mapped assembly '{assembly_name}' -> '{rel_path}'")
                        except Exception as e:
                            logger.warning(f"Could not process {csproj_path}: {e}")

        logger.info(f"Built mapping for {len(self.assembly_to_project_map)} assemblies")

    def _extract_assembly_name(self, csproj_path):
        """Extract the assembly name from a project file"""
        try:
            with open(csproj_path, 'r', encoding='utf-8-sig') as f:
                content = f.read()

            # Handle both SDK and traditional formats
            if 'Project Sdk=' in content:
                # SDK format - assembly name might be explicit or derived from project name
                root = ET.fromstring(content)
                assembly_name_elem = root.find('.//AssemblyName')
                if assembly_name_elem is not None:
                    return assembly_name_elem.text
                else:
                    # For SDK projects, default assembly name is the project file name without extension
                    return csproj_path.stem
            else:
                # Traditional format
                ET.register_namespace('', 'http://schemas.microsoft.com/developer/msbuild/2003')
                root = ET.fromstring(content)
                ns = {'ms': 'http://schemas.microsoft.com/developer/msbuild/2003'}

                assembly_name_elem = root.find('.//ms:AssemblyName', ns)
                if assembly_name_elem is not None:
                    return assembly_name_elem.text
                else:
                    # Fallback to project file name
                    return csproj_path.stem

        except Exception as e:
            logger.debug(f"Error extracting assembly name from {csproj_path}: {e}")
            return None

    def _load_packages_config(self, packages_file):
        """Load package references from packages.config file"""
        packages = {}
        config_path = self.repo_root / packages_file
        if not config_path.exists():
            logger.warning(f"Package config file not found: {config_path}")
            return packages

        try:
            tree = ET.parse(config_path)
            root = tree.getroot()
            for package in root.findall('package'):
                pkg_id = package.get('id')
                version = package.get('version')
                target_framework = package.get('targetFramework', '')
                exclude = package.get('exclude', '')
                packages[pkg_id] = {
                    'version': version,
                    'targetFramework': target_framework,
                    'exclude': exclude
                }
            logger.info(f"Loaded {len(packages)} packages from {packages_file}")
        except ET.ParseError as e:
            logger.error(f"Error parsing {packages_file}: {e}")

        return packages

    def _get_target_framework_from_version(self, version_string):
        """Convert TargetFrameworkVersion to TargetFramework"""
        version_map = {
            'v4.6.2': 'net462',
            'v4.6.1': 'net461',
            'v4.6': 'net46',
            'v4.5.2': 'net452',
            'v4.5.1': 'net451',
            'v4.5': 'net45',
            'v4.0': 'net40',
            'v3.5': 'net35'
        }
        return version_map.get(version_string, 'net462')

    def _has_assembly_info(self, project_dir):
        """Check if project has AssemblyInfo.cs or references CommonAssemblyInfo.cs"""
        # Check for AssemblyInfo.cs in various locations
        assembly_info_paths = [
            project_dir / "AssemblyInfo.cs",
            project_dir / "Properties" / "AssemblyInfo.cs"
        ]

        for path in assembly_info_paths:
            if path.exists():
                return True

        return False

    def _has_common_assembly_info_reference(self, csproj_content):
        """Check if project references CommonAssemblyInfo.cs"""
        return "CommonAssemblyInfo.cs" in csproj_content

    def _extract_conditional_property_groups(self, root, ns):
        """Extract conditional PropertyGroups that should be preserved"""
        conditional_groups = []

        for prop_group in root.findall('.//ms:PropertyGroup[@Condition]', ns):
            condition = prop_group.get('Condition')
            if prop_group.find('ms:DefineConstants', ns) is not None:
                conditional_groups.append((condition, prop_group))

        return conditional_groups

    def _extract_references(self, root, ns):
        """Extract Reference and ProjectReference items"""
        references = []
        project_references = []

        # Extract References
        for ref in root.findall('.//ms:Reference', ns):
            include = ref.get('Include')
            if include:
                # Check if it's a NuGet package or system reference
                hint_path = ref.find('ms:HintPath', ns)
                if hint_path is not None and ('packages' in hint_path.text or 'nuget' in hint_path.text.lower()):
                    # This is likely a NuGet package reference
                    package_name = include.split(',')[0]  # Remove version info
                    references.append(('package', package_name))
                else:
                    # System or local reference
                    references.append(('reference', include.split(',')[0]))

        # Extract ProjectReferences
        for proj_ref in root.findall('.//ms:ProjectReference', ns):
            include = proj_ref.get('Include')
            if include:
                project_references.append(include)

        return references, project_references

    def _find_project_references(self, project_dir, references):
        """Convert assembly references to project references where possible using intelligent mapping"""
        project_references = []
        remaining_references = []

        for ref_type, ref_name in references:
            if ref_type == 'reference':
                # First check if this reference should be a PackageReference
                if ref_name in self.package_names:
                    # This should be a PackageReference, not a ProjectReference
                    remaining_references.append(('package', ref_name))
                elif ref_name in self.assembly_to_project_map:
                    # This assembly is built by another project in the solution
                    target_project_path = Path(self.assembly_to_project_map[ref_name])

                    # Calculate relative path from current project to target project
                    try:
                        # Get relative path from current project directory to target project
                        rel_path = os.path.relpath(self.repo_root / target_project_path, project_dir)
                        project_references.append(rel_path)
                        logger.debug(f"Converted reference '{ref_name}' to ProjectReference: {rel_path}")
                    except Exception as e:
                        logger.warning(f"Could not calculate relative path for {ref_name}: {e}")
                        remaining_references.append((ref_type, ref_name))
                else:
                    # Keep as regular reference (system libraries, third-party DLLs, etc.)
                    remaining_references.append((ref_type, ref_name))
            else:
                remaining_references.append((ref_type, ref_name))

        return project_references, remaining_references

    def convert_project(self, csproj_path):
        """Convert a single .csproj file to SDK format"""
        logger.info(f"Converting {csproj_path}")

        try:
            with open(csproj_path, 'r', encoding='utf-8-sig') as f:
                content = f.read()

            # Parse XML with namespace handling
            # Register the default namespace to handle MSBuild XML properly
            ET.register_namespace('', 'http://schemas.microsoft.com/developer/msbuild/2003')
            root = ET.fromstring(content)

            # Define namespace for XPath queries
            ns = {'ms': 'http://schemas.microsoft.com/developer/msbuild/2003'}

            # Extract key information
            project_dir = Path(csproj_path).parent

            # Get basic properties with namespace
            assembly_name_elem = root.find('.//ms:AssemblyName', ns)
            assembly_name = assembly_name_elem.text if assembly_name_elem is not None else project_dir.name

            output_type_elem = root.find('.//ms:OutputType', ns)
            output_type = output_type_elem.text if output_type_elem is not None else 'Library'

            target_framework = 'net48'

            root_namespace_elem = root.find('.//ms:RootNamespace', ns)
            root_namespace = root_namespace_elem.text if root_namespace_elem is not None else assembly_name

            # Extract conditional property groups
            conditional_groups = self._extract_conditional_property_groups(root, ns)

            # Extract references
            references, existing_project_references = self._extract_references(root, ns)

            # Try to convert assembly references to project references
            new_project_references, remaining_references = self._find_project_references(project_dir, references)
            all_project_references = existing_project_references + new_project_references

            # Check for AssemblyInfo
            has_assembly_info = (self._has_assembly_info(project_dir) or
                               self._has_common_assembly_info_reference(content))

            # Generate new SDK format content
            new_content = self._generate_sdk_project(
                assembly_name, output_type, target_framework, root_namespace,
                remaining_references, all_project_references, conditional_groups,
                has_assembly_info, project_dir
            )

            # Write new file
            with open(csproj_path, 'w', encoding='utf-8') as f:
                f.write(new_content)

            self.converted_projects.append(str(csproj_path))
            logger.info(f"Successfully converted {csproj_path}")

        except Exception as e:
            logger.error(f"Failed to convert {csproj_path}: {e}")
            self.failed_projects.append((str(csproj_path), str(e)))

    def _generate_sdk_project(self, assembly_name, output_type, target_framework,
                             root_namespace, references, project_references,
                             conditional_groups, has_assembly_info, project_dir):
        """Generate SDK format project content"""

        lines = [
            '<Project Sdk="Microsoft.NET.Sdk">',
            '  <PropertyGroup>',
            f'    <AssemblyName>{assembly_name}</AssemblyName>',
            f'    <RootNamespace>{root_namespace}</RootNamespace>',
            f'    <TargetFramework>{target_framework}</TargetFramework>',
            f'    <OutputType>{output_type}</OutputType>',
            '    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>',
            '    <NoWarn>168,169,219,414,649,1635,1702,1701</NoWarn>'
        ]

        if has_assembly_info:
            lines.append('    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>')

        lines.append('  </PropertyGroup>')
        lines.append('')

        # Add conditional property groups
        for condition, prop_group in conditional_groups:
            lines.append(f'  <PropertyGroup Condition="{condition}">')

            for child in prop_group:
                tag_name = child.tag.split('}')[-1] if '}' in child.tag else child.tag  # Remove namespace prefix
                if tag_name == 'DefineConstants':
                    lines.append(f'    <DefineConstants>{child.text or ""}</DefineConstants>')
                elif tag_name == 'DebugSymbols':
                    lines.append(f'    <DebugSymbols>{child.text or "false"}</DebugSymbols>')
                elif tag_name == 'DebugType':
                    lines.append(f'    <DebugType>{child.text or "none"}</DebugType>')
                elif tag_name == 'Optimize':
                    lines.append(f'    <Optimize>{child.text or "false"}</Optimize>')

            lines.append('  </PropertyGroup>')
            lines.append('')

        # Add package references
        package_refs = []
        system_refs = []

        for ref_type, ref_name in references:
            if ref_type == 'package' and ref_name in self.all_packages:
                if ref_name in self.excluded_packages:
                    continue

                pkg_info = self.all_packages[ref_name]
                version = pkg_info["version"]

                # Handle SIL.Core version conflict - use the newer version
                if ref_name == 'SIL.Core':
                    version = self.sil_core_version

                exclude_attr = ''
                if pkg_info.get('exclude'):
                    exclude_attr = f' Exclude="{pkg_info["exclude"]}"'

                package_refs.append(f'    <PackageReference Include="{ref_name}" Version="{version}"{exclude_attr} />')
            elif ref_type == 'reference':
                # Skip common system references that are included by default in SDK projects
                if ref_name not in ['System', 'System.Core', 'System.Xml', 'System.Data', 'mscorlib']:
                    system_refs.append(f'    <Reference Include="{ref_name}" />')

        if package_refs:
            lines.append('  <ItemGroup>')
            lines.extend(sorted(set(package_refs)))  # Remove duplicates and sort
            lines.append('  </ItemGroup>')
            lines.append('')

        if system_refs:
            lines.append('  <ItemGroup>')
            lines.extend(sorted(set(system_refs)))  # Remove duplicates and sort
            lines.append('  </ItemGroup>')
            lines.append('')

        # Add project references
        if project_references:
            lines.append('  <ItemGroup>')
            for proj_ref in sorted(set(project_references)):  # Remove duplicates and sort
                lines.append(f'    <ProjectReference Include="{proj_ref}" />')
            lines.append('  </ItemGroup>')
            lines.append('')

        lines.append('</Project>')

        return '\n'.join(lines)

    def find_all_csproj_files(self):
        """Find all traditional .csproj files (excluding SDK format ones)"""
        csproj_files = []

        # Only search in specific subdirectories of the repo
        search_dirs = ['Src', 'Lib', 'Build', 'Bin']
        exclude_dirs = {'.git', 'bin', 'obj', 'packages', '.vs', '.vscode', 'node_modules'}

        for search_dir in search_dirs:
            search_path = self.repo_root / search_dir
            if not search_path.exists():
                continue

            for root, dirs, files in os.walk(search_path):
                # Filter out excluded directories from the search
                dirs[:] = [d for d in dirs if d not in exclude_dirs]

                for file in files:
                    if file.endswith('.csproj'):
                        csproj_path = Path(root) / file
                        try:
                            with open(csproj_path, 'r', encoding='utf-8-sig') as f:
                                content = f.read()

                            # Skip if already SDK format
                            if 'Project Sdk=' in content:
                                logger.info(f"Skipping already converted: {csproj_path}")
                                continue

                            csproj_files.append(csproj_path)

                        except Exception as e:
                            logger.warning(f"Could not read {csproj_path}: {e}")

        return csproj_files

    def convert_all_projects(self):
        """Convert all traditional .csproj files"""
        csproj_files = self.find_all_csproj_files()
        logger.info(f"Found {len(csproj_files)} projects to convert")

        for csproj_path in csproj_files:
            self.convert_project(csproj_path)

        logger.info(f"Conversion complete: {len(self.converted_projects)} successful, {len(self.failed_projects)} failed")

        if self.failed_projects:
            logger.error("Failed projects:")
            for project, error in self.failed_projects:
                logger.error(f"  {project}: {error}")

        # Generate solution file after all conversions are complete
        self._generate_solution_file()

    def _generate_solution_file(self):
        """Generate a FieldWorks.sln file that includes all converted projects"""
        logger.info("Generating FieldWorks.sln file...")

        solution_path = self.repo_root / "FieldWorks.sln"

        try:
            # Find all .csproj files (both converted and already SDK format)
            all_projects = []
            project_names_seen = set()

            search_dirs = ['Src', 'Lib', 'Build', 'Bin']
            exclude_dirs = {'.git', 'bin', 'obj', 'packages', '.vs', '.vscode', 'node_modules'}

            for search_dir in search_dirs:
                search_path = self.repo_root / search_dir
                if not search_path.exists():
                    continue

                for root, dirs, files in os.walk(search_path):
                    dirs[:] = [d for d in dirs if d not in exclude_dirs]

                    for file in files:
                        if file.endswith('.csproj'):
                            csproj_path = Path(root) / file
                            rel_path = csproj_path.relative_to(self.repo_root)
                            project_name = csproj_path.stem

                            # Handle duplicate project names by making them unique
                            unique_project_name = project_name
                            counter = 1
                            while unique_project_name in project_names_seen:
                                unique_project_name = f"{project_name}_{counter}"
                                counter += 1

                            project_names_seen.add(unique_project_name)
                            all_projects.append((unique_project_name, str(rel_path)))

            # Sort projects by name for consistent ordering
            all_projects.sort(key=lambda x: x[0])

            # Generate solution content
            solution_content = self._generate_solution_content(all_projects)

            # Write solution file
            with open(solution_path, 'w', encoding='utf-8') as f:
                f.write(solution_content)

            logger.info(f"Generated solution file with {len(all_projects)} projects: {solution_path}")

        except Exception as e:
            logger.error(f"Failed to generate solution file: {e}")

    def _generate_solution_content(self, projects):
        """Generate the content of the solution file"""
        import uuid

        lines = [
            '',
            'Microsoft Visual Studio Solution File, Format Version 12.00',
            '# Visual Studio Version 17',
            'VisualStudioVersion = 17.0.31903.59',
            'MinimumVisualStudioVersion = 10.0.40219.1'
        ]

        # Generate project entries
        project_guids = {}
        for project_name, project_path in projects:
            # Generate a consistent GUID for each project based on its path
            project_guid = str(uuid.uuid5(uuid.NAMESPACE_URL, project_path)).upper()
            project_guids[project_name] = project_guid

            lines.append(f'Project("{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}") = "{project_name}", "{project_path}", "{{{project_guid}}}"')
            lines.append('EndProject')

        lines.extend([
            'Global',
            '\tGlobalSection(SolutionConfigurationPlatforms) = preSolution',
            '\t\tDebug|x64 = Debug|x64',
            '\t\tDebug|x86 = Debug|x86',
            '\t\tRelease|x64 = Release|x64',
            '\t\tRelease|x86 = Release|x86',
            '\tEndGlobalSection',
            '\tGlobalSection(ProjectConfigurationPlatforms) = postSolution'
        ])

        # Add project configuration mappings
        for project_name, _ in projects:
            project_guid = project_guids[project_name]
            lines.extend([
                f'\t\t{{{project_guid}}}.Debug|x64.ActiveCfg = Debug|x64',
                f'\t\t{{{project_guid}}}.Debug|x64.Build.0 = Debug|x64',
                f'\t\t{{{project_guid}}}.Debug|x86.ActiveCfg = Debug|x86',
                f'\t\t{{{project_guid}}}.Debug|x86.Build.0 = Debug|x86',
                f'\t\t{{{project_guid}}}.Release|x64.ActiveCfg = Release|x64',
                f'\t\t{{{project_guid}}}.Release|x64.Build.0 = Release|x64',
                f'\t\t{{{project_guid}}}.Release|x86.ActiveCfg = Release|x86',
                f'\t\t{{{project_guid}}}.Release|x86.Build.0 = Release|x86'
            ])

        lines.extend([
            '\tEndGlobalSection',
            '\tGlobalSection(SolutionProperties) = preSolution',
            '\t\tHideSolutionNode = FALSE',
            '\tEndGlobalSection',
            '\tGlobalSection(ExtensibilityGlobals) = postSolution',
            f'\t\tSolutionGuid = {{{str(uuid.uuid4()).upper()}}}',
            '\tEndGlobalSection',
            'EndGlobal',
            ''
        ])

        return '\n'.join(lines)

def main():
    if len(sys.argv) > 1:
        repo_root = sys.argv[1]
    else:
        try:
            repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        except NameError:
            # Handle case where __file__ is not defined (e.g., when exec'd)
            repo_root = os.getcwd()

    converter = SDKConverter(repo_root)
    converter.convert_all_projects()

if __name__ == '__main__':
    main()