param(
    [string]$OutputPath = "Repository.Intelligence.Graph.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outputFile = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repoRoot $OutputPath
}

function Get-RepoRelativePath {
    param([string]$AbsolutePath)

    $rootPath = (Resolve-Path $repoRoot).Path.TrimEnd('\\')
    $resolvedPath = (Resolve-Path $AbsolutePath).Path

    if ($resolvedPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolvedPath.Substring($rootPath.Length).TrimStart('\\')
    }

    $uriRoot = [Uri]($rootPath + "\\")
    $uriPath = [Uri]$resolvedPath
    return [Uri]::UnescapeDataString($uriRoot.MakeRelativeUri($uriPath).ToString()).Replace('/', '\\')
}

function Resolve-ProjectReference {
    param(
        [string]$ProjectPath,
        [string]$IncludeValue
    )

    if ([string]::IsNullOrWhiteSpace($IncludeValue)) {
        return $null
    }

    $projectDir = Split-Path -Parent $ProjectPath
    $candidate = Join-Path $projectDir $IncludeValue

    try {
        $resolved = Resolve-Path $candidate -ErrorAction Stop
        return (Get-RepoRelativePath -AbsolutePath $resolved.Path)
    } catch {
        return $null
    }
}

function Get-ProjectKind {
    param(
        [string]$ProjectPath,
        [xml]$ProjectXml
    )

    $extension = [System.IO.Path]::GetExtension($ProjectPath).ToLowerInvariant()

    if ($extension -eq ".vcxproj") {
        return "native"
    }

    if ($extension -eq ".wixproj") {
        return "installer"
    }

    $targetFramework = @($ProjectXml.SelectNodes("//*[local-name()='TargetFramework']") | Select-Object -First 1)
    $targetFrameworkVersion = @($ProjectXml.SelectNodes("//*[local-name()='TargetFrameworkVersion']") | Select-Object -First 1)

    if ($targetFrameworkVersion.Count -gt 0 -and $targetFrameworkVersion[0].InnerText -like "v4.*") {
        return "managed-dotnet-framework"
    }

    if ($targetFramework.Count -gt 0 -and $targetFramework[0].InnerText -like "net4*") {
        return "managed-dotnet-framework"
    }

    return "managed"
}

function Get-TargetFramework {
    param([xml]$ProjectXml)

    $tfm = @($ProjectXml.SelectNodes("//*[local-name()='TargetFramework']") | Select-Object -First 1)
    if ($tfm.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($tfm[0].InnerText)) {
        return [string]$tfm[0].InnerText
    }

    $tfms = @($ProjectXml.SelectNodes("//*[local-name()='TargetFrameworks']") | Select-Object -First 1)
    if ($tfms.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($tfms[0].InnerText)) {
        return [string]$tfms[0].InnerText
    }

    $tfv = @($ProjectXml.SelectNodes("//*[local-name()='TargetFrameworkVersion']") | Select-Object -First 1)
    if ($tfv.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($tfv[0].InnerText)) {
        return [string]$tfv[0].InnerText
    }

    return $null
}

function Get-ProjectReferences {
    param(
        [string]$ProjectPath,
        [xml]$ProjectXml
    )

    $refs = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

    $projectRefNodes = $ProjectXml.SelectNodes("//*[local-name()='ProjectReference']")
    foreach ($projectRef in @($projectRefNodes)) {
        if ($null -eq $projectRef) {
            continue
        }

        $include = $projectRef.GetAttribute("Include")
        $normalized = Resolve-ProjectReference -ProjectPath $ProjectPath -IncludeValue $include
        if (-not [string]::IsNullOrWhiteSpace($normalized)) {
            [void]$refs.Add($normalized)
        }
    }

    return @($refs) | Sort-Object
}

function Get-IsTestProject {
    param(
        [string]$ProjectPath,
        [xml]$ProjectXml
    )

    $pathLower = $ProjectPath.ToLowerInvariant()
    if ($pathLower -match "\\tests?\\" -or $pathLower -match "tests?\.csproj$" -or $pathLower -match "tests?\.vcxproj$") {
        return $true
    }

    $packageNodes = $ProjectXml.SelectNodes("//*[local-name()='PackageReference']")
    foreach ($pkg in @($packageNodes)) {
        if ($null -eq $pkg) {
            continue
        }

        $include = $pkg.GetAttribute("Include")
        if ([string]$include -match "(?i)nunit|xunit|mstest") {
            return $true
        }
    }

    return $false
}

$projectFiles = Get-ChildItem -Path (Join-Path $repoRoot "Src") -Recurse -File -Include *.csproj,*.vcxproj,*.wixproj
$projectFiles += Get-ChildItem -Path (Join-Path $repoRoot "FLExInstaller") -Recurse -File -Include *.wixproj

$projectFiles = $projectFiles |
    Where-Object {
        $_.FullName -notmatch "\\(Obj|Output|packages|\\.git)\\"
    } |
    Sort-Object FullName -Unique

$projects = @()
foreach ($project in $projectFiles) {
    [xml]$xml = Get-Content -Path $project.FullName -Raw
    $relativePath = Get-RepoRelativePath -AbsolutePath $project.FullName
    $references = Get-ProjectReferences -ProjectPath $project.FullName -ProjectXml $xml
    $targetFramework = Get-TargetFramework -ProjectXml $xml
    $kind = Get-ProjectKind -ProjectPath $project.FullName -ProjectXml $xml
    $isTest = Get-IsTestProject -ProjectPath $project.FullName -ProjectXml $xml

    $segments = $relativePath -split '[\\/]'
    $topArea = $segments[0]
    if ($relativePath -match '^Src[\\/]') {
        if ($segments.Length -ge 2) {
            $topArea = "Src/" + $segments[1]
        } else {
            $topArea = "Src"
        }
    }

    $projects += [ordered]@{
        name = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
        path = $relativePath.Replace('\','/')
        kind = $kind
        target_framework = $targetFramework
        is_test_project = $isTest
        top_area = $topArea
        project_references = @($references | ForEach-Object { $_.Replace('\','/') })
    }
}

$projects = $projects | Sort-Object path

$buildCommands = @(
    [ordered]@{ name = "build-debug"; command = ".\\build.ps1" },
    [ordered]@{ name = "build-release"; command = ".\\build.ps1 -Configuration Release" },
    [ordered]@{ name = "test-managed"; command = ".\\test.ps1" },
    [ordered]@{ name = "test-native"; command = ".\\test.ps1 -Native" }
)

$constraints = @(
    "windows-x64-only",
    "native-build-before-managed-build",
    "registration-free-com",
    "localization-via-resx",
    "use-build-ps1-and-test-ps1"
)

$headCommit = "unknown"
try {
    $headCommit = (git -C $repoRoot rev-parse HEAD).Trim()
} catch {
}

$graph = [ordered]@{
    graph_version = 1
    repository = "FieldWorks"
    generated_from_commit = $headCommit
    generation_tool = "Build/Agent/Generate-RepositoryIntelligenceGraph.ps1"
    build_commands = $buildCommands
    constraints = $constraints
    projects = $projects
}

$graphDir = Split-Path -Parent $outputFile
if (-not (Test-Path $graphDir)) {
    New-Item -ItemType Directory -Path $graphDir | Out-Null
}

$graph | ConvertTo-Json -Depth 10 | Set-Content -Path $outputFile -Encoding UTF8
Write-Host "Wrote repository intelligence graph to $outputFile"
