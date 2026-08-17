$ErrorActionPreference = 'Stop'

$script:Shell = (Get-Process -Id $PID).Path
$script:Installer = Join-Path (Split-Path $PSScriptRoot -Parent) 'install.ps1'
$script:FrameworkRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:Failures = [System.Collections.Generic.List[string]]::new()

function Write-Utf8Text {
    param([string]$Path, [string]$Content)

    $parent = Split-Path $Path -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function New-InstallFixture {
    param([switch]$SeedHumanAgents, [switch]$SeedBrokenMarker)

    $root = Join-Path ([System.IO.Path]::GetTempPath()) ("dice-context-install-" + [guid]::NewGuid())
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $contextRoot = Join-Path $root '.project-context'
    New-Item -ItemType Directory -Path $contextRoot -Force | Out-Null
    Copy-Item -LiteralPath $script:FrameworkRoot -Destination (Join-Path $contextRoot 'framework') -Recurse

    if ($SeedHumanAgents) {
        Write-Utf8Text (Join-Path $root 'AGENTS.md') "# Human rules`n`nKeep this paragraph.`n"
    }
    if ($SeedBrokenMarker) {
        Write-Utf8Text (Join-Path $root 'AGENTS.md') "# Human rules`n`n<!-- project-context:begin -->`n"
    }

    return $root
}

function Get-TreeDigest {
    param([string]$Root)

    $records = Get-ChildItem -LiteralPath $Root -Recurse -File | Sort-Object FullName | ForEach-Object {
        $relative = $_.FullName.Substring($Root.Length)
        "{0}|{1}" -f $relative, (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($records -join "`n"))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [Convert]::ToBase64String($sha.ComputeHash($bytes))
    }
    finally {
        $sha.Dispose()
    }
}

function Invoke-TestInstall {
    param([string]$Root, [string]$Mode, [switch]$Apply)

    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $script:Installer, '-Mode', $Mode, '-Root', $Root)
    if ($Apply) {
        $arguments += '-Apply'
    }
    $output = & $script:Shell @arguments 2>&1 | Out-String
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Invoke-IsolatedTest {
    param([string]$Name, [scriptblock]$Test, [switch]$SeedHumanAgents, [switch]$SeedBrokenMarker)

    $root = New-InstallFixture -SeedHumanAgents:$SeedHumanAgents -SeedBrokenMarker:$SeedBrokenMarker
    try {
        & $Test $root
        Write-Host "[test:ok] $Name"
    }
    catch {
        $script:Failures.Add("$Name - $($_.Exception.Message)")
    }
    finally {
        if (Test-Path -LiteralPath $root) {
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }
}

function Assert-Equal {
    param([object]$Actual, [object]$Expected, [string]$Message)
    if ($Actual -ne $Expected) {
        throw "$Message. Expected '$Expected', received '$Actual'."
    }
}

Invoke-IsolatedTest 'existing-project preview makes no changes' {
    param($root)
    $before = Get-TreeDigest $root
    $result = Invoke-TestInstall $root 'ExistingProject'
    Assert-Equal $result.ExitCode 0 'Preview exit code'
    Assert-Equal (Get-TreeDigest $root) $before 'Preview changed the fixture'
}

Invoke-IsolatedTest 'existing project creates configuration and project documents' {
    param($root)
    $result = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $result.ExitCode 0 'Apply exit code'
    foreach ($relative in @(
        'AGENTS.md',
        '.project-context\project-context.json',
        '.project-context\project\PROJECT.md',
        '.project-context\project\STATUS.md',
        '.project-context\project\DECISIONS.md',
        '.project-context\project\ENVIRONMENT.md'
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            throw "Missing generated file: $relative"
        }
    }
    $configuration = Get-Content -LiteralPath (Join-Path $root '.project-context\project-context.json') -Raw | ConvertFrom-Json
    Assert-Equal $configuration.schemaVersion '1.0.0' 'Schema version'
    Assert-Equal $configuration.projectType 'mixed' 'Default project type'
    Assert-Equal $configuration.projectName (Split-Path $root -Leaf) 'Default project name'
}

Invoke-IsolatedTest 'second existing-project apply is idempotent' {
    param($root)
    $first = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $first.ExitCode 0 'First apply exit code'
    $afterFirst = Get-TreeDigest $root
    $second = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $second.ExitCode 0 'Second apply exit code'
    Assert-Equal (Get-TreeDigest $root) $afterFirst 'Second apply changed the fixture'
}

Invoke-IsolatedTest 'human AGENTS content survives managed insertion' -SeedHumanAgents {
    param($root)
    $result = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $result.ExitCode 0 'Apply exit code'
    $agents = [System.IO.File]::ReadAllText((Join-Path $root 'AGENTS.md'))
    if ($agents -notmatch '# Human rules' -or $agents -notmatch 'Keep this paragraph\.' -or $agents -notmatch '<!-- project-context:begin -->') {
        throw 'Human AGENTS content or managed block is missing.'
    }
}

Invoke-IsolatedTest 'unpaired managed marker stops without writes' -SeedBrokenMarker {
    param($root)
    $before = Get-TreeDigest $root
    $result = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $result.ExitCode 1 'Broken-marker exit code'
    Assert-Equal (Get-TreeDigest $root) $before 'Broken-marker run changed the fixture'
}

Invoke-IsolatedTest 'repair restores missing managed entry' {
    param($root)
    $install = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $install.ExitCode 0 'Initial install exit code'
    Remove-Item -LiteralPath (Join-Path $root 'AGENTS.md') -Force
    $repair = Invoke-TestInstall $root 'Repair' -Apply
    Assert-Equal $repair.ExitCode 0 'Repair exit code'
    if (-not (Test-Path -LiteralPath (Join-Path $root 'AGENTS.md'))) {
        throw 'Repair did not recreate AGENTS.md.'
    }
}

if ($script:Failures.Count -gt 0) {
    $script:Failures | ForEach-Object { Write-Host "[test:error] $_" }
    exit 1
}

Write-Host '[test:ok] installer acceptance tests passed'
exit 0
