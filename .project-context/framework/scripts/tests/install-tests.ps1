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

function Prepare-CopiedPackage {
    param([string]$Root)

    $install = Invoke-TestInstall $Root 'ExistingProject' -Apply
    Assert-Equal $install.ExitCode 0 'Source install exit code'
    $projectPath = Join-Path $Root '.project-context\project\PROJECT.md'
    Add-Content -LiteralPath $projectPath -Value "`nSOURCE_PROJECT_SENTINEL" -Encoding UTF8
    Remove-Item -LiteralPath (Join-Path $Root 'AGENTS.md') -Force
    return Get-Content -LiteralPath (Join-Path $Root '.project-context\project-context.json') -Raw | ConvertFrom-Json
}

Invoke-IsolatedTest 'new-project preview makes no changes' {
    param($root)
    $null = Prepare-CopiedPackage $root
    $before = Get-TreeDigest $root
    $preview = Invoke-TestInstall $root 'NewProject'
    Assert-Equal $preview.ExitCode 0 'NewProject preview exit code'
    Assert-Equal (Get-TreeDigest $root) $before 'NewProject preview changed the fixture'
}

Invoke-IsolatedTest 'new-project backs up source and rebinds instance' {
    param($root)
    $oldConfiguration = Prepare-CopiedPackage $root
    $result = Invoke-TestInstall $root 'NewProject' -Apply
    Assert-Equal $result.ExitCode 0 'NewProject apply exit code'

    $newConfiguration = Get-Content -LiteralPath (Join-Path $root '.project-context\project-context.json') -Raw | ConvertFrom-Json
    if ($newConfiguration.instanceId -eq $oldConfiguration.instanceId) {
        throw 'NewProject did not generate a different instance UUID.'
    }
    Assert-Equal $newConfiguration.projectName (Split-Path $root -Leaf) 'Rebound project name'

    $sourceBackupRoot = Join-Path $root ('.project-context\backups\' + $oldConfiguration.instanceId)
    $backup = Get-ChildItem -LiteralPath $sourceBackupRoot -Directory | Select-Object -First 1
    if ($null -eq $backup) {
        throw 'NewProject did not create a timestamped backup.'
    }
    foreach ($relative in @('project\PROJECT.md', 'project-context.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $backup.FullName $relative))) {
            throw "Backup is missing $relative"
        }
    }
    $backedUpProject = [System.IO.File]::ReadAllText((Join-Path $backup.FullName 'project\PROJECT.md'))
    if ($backedUpProject -notmatch 'SOURCE_PROJECT_SENTINEL') {
        throw 'Backup did not preserve source project content.'
    }

    $agents = [System.IO.File]::ReadAllText((Join-Path $root 'AGENTS.md'))
    if ($agents -notmatch [regex]::Escape("<!-- project-context:instance $($newConfiguration.instanceId) -->")) {
        throw 'Managed AGENTS instance does not match rebound configuration.'
    }
}

Invoke-IsolatedTest 'second new-project apply is idempotent after rebind' {
    param($root)
    $null = Prepare-CopiedPackage $root
    $first = Invoke-TestInstall $root 'NewProject' -Apply
    Assert-Equal $first.ExitCode 0 'First NewProject apply exit code'
    $afterFirst = Get-TreeDigest $root
    $second = Invoke-TestInstall $root 'NewProject' -Apply
    Assert-Equal $second.ExitCode 0 'Second NewProject apply exit code'
    Assert-Equal (Get-TreeDigest $root) $afterFirst 'Second NewProject apply changed the fixture'
}

function Convert-FixtureToLegacyConfiguration {
    param([string]$Root)

    $path = Join-Path $Root '.project-context\project-context.json'
    $configuration = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $legacy = [ordered]@{
        schemaVersion = '0.9.0'
        instanceId = $configuration.instanceId
        projectName = $configuration.projectName
        projectType = $configuration.projectType
        sourceRoots = @($configuration.sourceRoots)
        entryFiles = @($configuration.entryFiles)
        testCommands = @($configuration.testCommands)
        protectedPaths = @($configuration.protectedPaths)
    }
    Write-Utf8Text $path (($legacy | ConvertTo-Json -Depth 8) + "`n")
    return $legacy
}

Invoke-IsolatedTest 'upgrade preview makes no changes' {
    param($root)
    $install = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $install.ExitCode 0 'Initial install exit code'
    $null = Convert-FixtureToLegacyConfiguration $root
    $before = Get-TreeDigest $root
    $preview = Invoke-TestInstall $root 'Upgrade'
    Assert-Equal $preview.ExitCode 0 'Upgrade preview exit code'
    Assert-Equal (Get-TreeDigest $root) $before 'Upgrade preview changed the fixture'
}

Invoke-IsolatedTest 'upgrade migrates configuration and preserves project' {
    param($root)
    $install = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $install.ExitCode 0 'Initial install exit code'
    $projectPath = Join-Path $root '.project-context\project\PROJECT.md'
    $projectBefore = [System.IO.File]::ReadAllText($projectPath)
    $legacy = Convert-FixtureToLegacyConfiguration $root

    $upgrade = Invoke-TestInstall $root 'Upgrade' -Apply
    Assert-Equal $upgrade.ExitCode 0 'Upgrade apply exit code'
    $configuration = Get-Content -LiteralPath (Join-Path $root '.project-context\project-context.json') -Raw | ConvertFrom-Json
    Assert-Equal $configuration.schemaVersion '1.0.0' 'Migrated schema version'
    Assert-Equal $configuration.workstreamStaleDays 14 'Migrated stale-day default'
    Assert-Equal ([System.IO.File]::ReadAllText($projectPath)) $projectBefore 'Upgrade changed PROJECT.md'

    $sourceBackupRoot = Join-Path $root ('.project-context\backups\' + $legacy.instanceId)
    $backupConfig = Get-ChildItem -LiteralPath $sourceBackupRoot -Recurse -File -Filter 'project-context.json' | Select-Object -First 1
    if ($null -eq $backupConfig) {
        throw 'Upgrade did not back up the old configuration.'
    }
}

Invoke-IsolatedTest 'newer schema refuses downgrade' {
    param($root)
    $install = Invoke-TestInstall $root 'ExistingProject' -Apply
    Assert-Equal $install.ExitCode 0 'Initial install exit code'
    $path = Join-Path $root '.project-context\project-context.json'
    $configuration = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $configuration.schemaVersion = '2.0.0'
    Write-Utf8Text $path (($configuration | ConvertTo-Json -Depth 8) + "`n")
    $before = Get-TreeDigest $root
    $upgrade = Invoke-TestInstall $root 'Upgrade' -Apply
    Assert-Equal $upgrade.ExitCode 1 'Newer-schema exit code'
    Assert-Equal (Get-TreeDigest $root) $before 'Rejected downgrade changed the fixture'
}

Invoke-IsolatedTest 'installer resolves default root in preview' {
    param($root)
    $copiedInstaller = Join-Path $root '.project-context\framework\scripts\install.ps1'
    $before = Get-TreeDigest $root
    $output = & $script:Shell -NoProfile -ExecutionPolicy Bypass -File $copiedInstaller -Mode ExistingProject 2>&1 | Out-String
    Assert-Equal $LASTEXITCODE 0 "Default-root preview exit code. Output: $output"
    Assert-Equal (Get-TreeDigest $root) $before 'Default-root preview changed the fixture'
}

if ($script:Failures.Count -gt 0) {
    $script:Failures | ForEach-Object { Write-Host "[test:error] $_" }
    exit 1
}

Write-Host '[test:ok] installer acceptance tests passed'
exit 0
