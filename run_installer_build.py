import subprocess
import sys
import os

def run_command(command):
    print(f"Running: {command}")
    try:
        result = subprocess.run(command, shell=True, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        print(result.stdout)
    except subprocess.CalledProcessError as e:
        print(f"Error running command: {e}")
        print(f"Stdout: {e.stdout}")
        print(f"Stderr: {e.stderr}")
        sys.exit(1)

# Check if msbuild is in path, if not try to find it (optional, assuming it is in path for now or using vswhere)
# For now, just try running the powershell script using powershell.exe (Windows PowerShell)

print("Validating environment...")
run_command("powershell.exe -ExecutionPolicy Bypass -File Build\\Agent\\Setup-InstallerBuild.ps1 -ValidateOnly")

print("Building installer (Release)...")
# Using the command from instructions
run_command("msbuild Build\\Orchestrator.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release /m")
