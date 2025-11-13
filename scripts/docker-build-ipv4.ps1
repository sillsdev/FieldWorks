<#
.SYNOPSIS
    Wrapper for 'docker build' that forces IPv4 resolution for registry hostnames.

.DESCRIPTION
    This script works around a known issue on Windows 11 where Docker fails to pull
    images from container registries over IPv6 with errors like:
        "read tcp [IPv6]:port->[IPv6]:443: wsarecv: An existing connection was
        forcibly closed by the remote host."

    The issue occurs because:
    - Windows 11's IPv6 TCP stack has connection stability issues
    - Docker Desktop inherits the host's network stack
    - Container registries (like mcr.microsoft.com) support both IPv4 and IPv6
    - DNS returns IPv6 addresses first, triggering the buggy code path

    This wrapper:
    1. Resolves registry hostnames to their IPv4 addresses
    2. Passes --add-host flags to docker build to force IPv4 usage
    3. No admin rights required (--add-host only affects the build container)

.PARAMETER RegistryHosts
    Hostnames to resolve and add IPv4 mappings for. Defaults to mcr.microsoft.com.

.PARAMETER BuildArgs
    All remaining arguments are passed directly to 'docker build'.

.EXAMPLE
    .\scripts\docker-build-ipv4.ps1 -t myimage:latest -f Dockerfile .

.EXAMPLE
    .\scripts\docker-build-ipv4.ps1 -RegistryHosts @("mcr.microsoft.com", "docker.io") -t myimage:latest .

.NOTES
    If you don't experience IPv6 issues, just use 'docker build' directly.
    This is purely a workaround for Windows 11 + Docker Desktop + IPv6 bugs.

    Alternative permanent fixes (require admin):
    - Disable IPv6 on network adapter:
      Disable-NetAdapterBinding -Name "Wi-Fi" -ComponentID ms_tcpip6
    - Add IPv4 entries to C:\Windows\System32\drivers\etc\hosts
#>

[CmdletBinding()]
param(
    [string[]]$RegistryHosts = @("mcr.microsoft.com"),
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$BuildArgs
)

$ErrorActionPreference = "Stop"

# Collect --add-host flags for each registry
if (-not (Get-Variable -Name RegistryIpv4Cache -Scope Script -ErrorAction SilentlyContinue)) {
    $script:RegistryIpv4Cache = @{}
}

$addHostFlags = @()
foreach ($registryHost in $RegistryHosts) {
    try {
        if (-not $script:RegistryIpv4Cache.ContainsKey($registryHost)) {
            $ipv4 = [System.Net.Dns]::GetHostAddresses($registryHost) |
                    Where-Object { $_.AddressFamily -eq 'InterNetwork' } |
                    Select-Object -First 1
            if ($ipv4) {
                $script:RegistryIpv4Cache[$registryHost] = $ipv4.IPAddressToString
            } else {
                $script:RegistryIpv4Cache[$registryHost] = $null
            }
        }

        $ipAddress = $script:RegistryIpv4Cache[$registryHost]

        if ($ipAddress) {
            Write-Host "Resolved $registryHost to IPv4: $ipAddress"
            $addHostFlags += "--add-host"
            $addHostFlags += "${registryHost}:${ipAddress}"
        } else {
            Write-Warning "No IPv4 address found for $registryHost, skipping --add-host"
        }
    } catch {
        Write-Warning "Failed to resolve $registryHost : $_"
    }
}

# Combine --add-host flags with user's build arguments
$finalArgs = $addHostFlags + $BuildArgs

# Execute docker build
if ($finalArgs.Count -eq 0) {
    Write-Error "No build arguments provided. Usage: docker-build-ipv4.ps1 [docker build arguments]"
    exit 1
}

Write-Host "Running: docker build $($finalArgs -join ' ')"
$proc = Start-Process -FilePath "docker" -ArgumentList (@("build") + $finalArgs) -NoNewWindow -PassThru -Wait
exit $proc.ExitCode