import argparse
import subprocess
import re

# The script is in the 'Build' folder, so the path to the version file is one level up.
VERSION_FILE = '../Src/MasterVersionInfo.txt'

def run_command(command, check=True):
    """Runs a shell command and returns the output."""
    try:
        result = subprocess.run(command, shell=True, check=check, capture_output=True, text=True)
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Error executing command: {command}")
        print(e.stderr)
        return None

def read_version_file():
    """Reads the version file and returns a dictionary."""
    versions = {}
    try:
        with open(VERSION_FILE, 'r') as f:
            for line in f:
                if '=' in line:
                    key, value = line.strip().split('=', 1)
                    versions[key] = value
    except FileNotFoundError:
        print(f"Error: Version file not found at {VERSION_FILE}")
        return None
    return versions

def write_version_file(versions):
    """Writes the new version to the file."""
    try:
        with open(VERSION_FILE, 'w') as f:
            for key, value in versions.items():
                f.write(f"{key}={value}\n")
    except Exception as e:
        print(f"Error writing to version file: {e}")
        return False
    return True

def main():
    parser = argparse.ArgumentParser(description="Bump software version for release.")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('-m', '--major', action='store_true', help='Bump the major version.')
    group.add_argument('-i', '--minor', action='store_true', help='Bump the minor version.')
    group.add_argument('-r', '--revision', action='store_true', help='Bump the revision version.')
    parser.add_argument('-s', '--stability', choices=['Alpha', 'Beta', ''], default=None, help='Set the stability level.')
    args = parser.parse_args()

    # Read current version
    current_versions = read_version_file()
    if not current_versions:
        return

    fw_major = int(current_versions.get('FWMAJOR', 0))
    fw_minor = int(current_versions.get('FWMINOR', 0))
    fw_revision = int(current_versions.get('FWREVISION', 0))
    fw_beta = current_versions.get('FWBETAVERSION', '')

    # Get current branch
    current_branch = run_command("git rev-parse --abbrev-ref HEAD")
    if not current_branch:
        return

    # Validate branch name
    branch_pattern = rf"^(release/{fw_major}\.{fw_minor}|hotfix/{fw_major}\.{fw_minor}\.{fw_revision})$"
    if not re.match(branch_pattern, current_branch):
        print(f"Error: Current branch '{current_branch}' does not match the expected format.")
        return

    # Git stash
    print("Stashing any local changes...")
    if run_command("git stash") is None:
        print("Git stash failed. Aborting.")
        return

    # Git pull
    print("Performing git pull...")
    if run_command("git pull") is None:
        print("Git pull failed. Please resolve issues manually and re-run.")
        return

    # Calculate new version
    new_versions = current_versions.copy()
    if args.major:
        new_versions['FWMAJOR'] = str(fw_major + 1)
        new_versions['FWMINOR'] = '0'
        new_versions['FWREVISION'] = '0'
    elif args.minor:
        new_versions['FWMINOR'] = str(fw_minor + 1)
        new_versions['FWREVISION'] = '0'
    elif args.revision:
        new_versions['FWREVISION'] = str(fw_revision + 1)

    # Handle stability only if the flag was provided
    if args.stability is not None:
        new_versions['FWBETAVERSION'] = args.stability

    # Display changes
    print("--- Version Bump ---")
    print(f"Old Version: {fw_major}.{fw_minor}.{fw_revision} {fw_beta}")
    print(f"New Version: {new_versions['FWMAJOR']}.{new_versions['FWMINOR']}.{new_versions['FWREVISION']} {new_versions['FWBETAVERSION']}")

    # Write the new version file
    if not write_version_file(new_versions):
        return

    # Git commit
    new_version_string = f"{new_versions['FWMAJOR']}.{new_versions['FWMINOR']}.{new_versions['FWREVISION']}"
    if new_versions['FWBETAVERSION']:
        new_version_string += f" {new_versions['FWBETAVERSION']}"

    print("Committing version bump...")
    run_command(f"git add {VERSION_FILE}")
    if run_command(f"git commit -m \"Bump version to {new_version_string}\"") is None:
        print("Git commit failed. Aborting.")
        return

    print("Version bump successful. Ready to push.")

if __name__ == "__main__":
    main()