param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('NewProject', 'ExistingProject', 'Upgrade', 'Repair')]
    [string]$Mode,
    [switch]$Apply,
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
)

$ErrorActionPreference = 'Stop'
$Root = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
$script:Operations = [System.Collections.Generic.List[object]]::new()
$script:ContextRoot = Join-Path $Root '.project-context'
$script:FrameworkRoot = Join-Path $script:ContextRoot 'framework'
$script:TemplatesRoot = Join-Path $script:FrameworkRoot 'templates'
$script:ConfigurationPath = Join-Path $script:ContextRoot 'project-context.json'
$script:ProjectRoot = Join-Path $script:ContextRoot 'project'

function Test-PathInside {
    param([string]$Candidate, [string]$ExpectedParent)

    $candidatePath = [System.IO.Path]::GetFullPath($Candidate).TrimEnd('\', '/')
    $parentPath = [System.IO.Path]::GetFullPath($ExpectedParent).TrimEnd('\', '/')
    return $candidatePath -eq $parentPath -or $candidatePath.StartsWith(
        $parentPath + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-SafeTarget {
    param([string]$Path)
    if (-not (Test-PathInside $Path $Root)) {
        throw "Refusing to write outside repository root: $Path"
    }
}

function Read-Utf8Text {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path)
}

function Add-WriteOperation {
    param([string]$Path, [string]$Content, [switch]$Force)

    Assert-SafeTarget $Path
    if (-not $Force -and (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $existing = Read-Utf8Text $Path
        if ($existing -eq $Content) {
            return
        }
    }

    $script:Operations.Add([pscustomobject]@{
        Type = 'Write'
        Path = [System.IO.Path]::GetFullPath($Path)
        Content = $Content
    })
}

function Add-MoveOperation {
    param([string]$Source, [string]$Destination)

    Assert-SafeTarget $Source
    Assert-SafeTarget $Destination
    $script:Operations.Add([pscustomobject]@{
        Type = 'Move'
        Source = [System.IO.Path]::GetFullPath($Source)
        Destination = [System.IO.Path]::GetFullPath($Destination)
    })
}

function Write-Utf8Text {
    param([string]$Path, [string]$Content)

    $parent = Split-Path $Path -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-Operations {
    param([System.Collections.Generic.List[object]]$Operations)

    if ($Operations.Count -eq 0) {
        Write-Host '[context:plan] no changes'
        return
    }

    foreach ($operation in $Operations) {
        if ($operation.Type -eq 'Write') {
            Write-Host "[context:plan] write $($operation.Path.Substring($Root.Length + 1))"
        }
        elseif ($operation.Type -eq 'Move') {
            Write-Host "[context:plan] move $($operation.Source.Substring($Root.Length + 1)) -> $($operation.Destination.Substring($Root.Length + 1))"
        }
    }

    if (-not $Apply) {
        Write-Host '[context:plan] preview only; rerun with -Apply to write'
        return
    }

    foreach ($operation in $Operations) {
        if ($operation.Type -eq 'Write') {
            Write-Utf8Text $operation.Path $operation.Content
            Write-Host "[context:apply] wrote $($operation.Path.Substring($Root.Length + 1))"
        }
        elseif ($operation.Type -eq 'Move') {
            $parent = Split-Path $operation.Destination -Parent
            if (-not (Test-Path -LiteralPath $parent)) {
                New-Item -ItemType Directory -Path $parent -Force | Out-Null
            }
            Move-Item -LiteralPath $operation.Source -Destination $operation.Destination
            Write-Host "[context:apply] moved $($operation.Source.Substring($Root.Length + 1))"
        }
    }
}

function New-DefaultConfiguration {
    $projectName = Split-Path $Root -Leaf
    return [ordered]@{
        schemaVersion = '1.0.0'
        instanceId = [guid]::NewGuid().ToString()
        projectName = $projectName
        projectType = 'mixed'
        sourceRoots = @()
        entryFiles = @()
        testCommands = @('powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1')
        protectedPaths = @()
        additionalContextFiles = @()
        ignorePatterns = @('.git/**')
        workstreamStaleDays = 14
    }
}

function Convert-ConfigurationToJson {
    param([object]$Configuration)
    return ($Configuration | ConvertTo-Json -Depth 8) + "`n"
}

function Get-TemplateContent {
    param([string]$Name)

    $path = Join-Path $script:TemplatesRoot $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing framework template: $Name"
    }
    return Read-Utf8Text $path
}

function Render-ProjectTemplate {
    param([string]$Name, [string]$ProjectName)
    return (Get-TemplateContent $Name).Replace('{{PROJECT_NAME}}', $ProjectName)
}

function Get-ManagedAgentsContent {
    param([string]$ExistingContent, [string]$InstanceId)

    $block = (Get-TemplateContent 'AGENTS_MANAGED_BLOCK.md').Replace('{{INSTANCE_ID}}', $InstanceId).Trim()
    $begin = '<!-- project-context:begin -->'
    $end = '<!-- project-context:end -->'
    $beginMatches = [regex]::Matches($ExistingContent, [regex]::Escape($begin))
    $endMatches = [regex]::Matches($ExistingContent, [regex]::Escape($end))

    if ($beginMatches.Count -eq 0 -and $endMatches.Count -eq 0) {
        if ([string]::IsNullOrWhiteSpace($ExistingContent)) {
            return $block + "`n"
        }
        return $ExistingContent.TrimEnd() + "`n`n" + $block + "`n"
    }

    if ($beginMatches.Count -ne 1 -or $endMatches.Count -ne 1 -or $beginMatches[0].Index -gt $endMatches[0].Index) {
        throw 'AGENTS.md has invalid or unpaired project-context markers.'
    }

    $pattern = '(?s)' + [regex]::Escape($begin) + '.*?' + [regex]::Escape($end)
    return ([regex]::Replace($ExistingContent, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($match) $block }, 1)).TrimEnd() + "`n"
}

function Get-ManagedInstanceId {
    $agentsPath = Join-Path $Root 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $agentsPath -PathType Leaf)) {
        return $null
    }

    $text = Read-Utf8Text $agentsPath
    $match = [regex]::Match($text, '<!-- project-context:instance (?<id>[0-9a-fA-F-]+) -->')
    if (-not $match.Success) {
        return $null
    }
    return $match.Groups['id'].Value
}

function Get-Configuration {
    if (-not (Test-Path -LiteralPath $script:ConfigurationPath -PathType Leaf)) {
        return $null
    }
    return Read-Utf8Text $script:ConfigurationPath | ConvertFrom-Json
}

function Plan-ManagedAgents {
    param([object]$Configuration)

    $agentsPath = Join-Path $Root 'AGENTS.md'
    $existing = if (Test-Path -LiteralPath $agentsPath -PathType Leaf) { Read-Utf8Text $agentsPath } else { '' }
    $updated = Get-ManagedAgentsContent $existing ([string]$Configuration.instanceId)
    Add-WriteOperation $agentsPath $updated
}

function Plan-MissingProjectDocuments {
    param([object]$Configuration)

    $documents = [ordered]@{
        'PROJECT.md' = 'PROJECT.md'
        'STATUS.md' = 'STATUS.md'
        'DECISIONS.md' = 'DECISIONS.md'
        'ENVIRONMENT.md' = 'ENVIRONMENT.md'
    }
    foreach ($entry in $documents.GetEnumerator()) {
        $target = Join-Path $script:ProjectRoot $entry.Key
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
            Add-WriteOperation $target (Render-ProjectTemplate $entry.Value ([string]$Configuration.projectName))
        }
    }
}

function Plan-AllProjectDocuments {
    param([object]$Configuration)

    foreach ($name in @('PROJECT.md', 'STATUS.md', 'DECISIONS.md', 'ENVIRONMENT.md')) {
        Add-WriteOperation (Join-Path $script:ProjectRoot $name) (Render-ProjectTemplate $name ([string]$Configuration.projectName)) -Force
    }
}

function Plan-ExistingProject {
    $configuration = Get-Configuration
    if ($null -eq $configuration) {
        $configuration = New-DefaultConfiguration
        Add-WriteOperation $script:ConfigurationPath (Convert-ConfigurationToJson $configuration)
    }
    elseif ($configuration.schemaVersion -ne '1.0.0') {
        throw "ExistingProject requires schemaVersion 1.0.0; found $($configuration.schemaVersion). Use Upgrade."
    }

    Plan-MissingProjectDocuments $configuration
    Plan-ManagedAgents $configuration
}

function Plan-Repair {
    $configuration = Get-Configuration
    if ($null -eq $configuration) {
        throw 'Repair requires an existing project-context.json. Use ExistingProject first.'
    }
    if ($configuration.schemaVersion -ne '1.0.0') {
        throw "Repair requires schemaVersion 1.0.0; found $($configuration.schemaVersion). Use Upgrade."
    }

    foreach ($relative in @('PROJECT.md', 'STATUS.md', 'DECISIONS.md', 'ENVIRONMENT.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $script:ProjectRoot $relative) -PathType Leaf)) {
            Write-Host "[context:warning] missing project document: $relative"
        }
    }
    Plan-ManagedAgents $configuration
}

function New-BackupRoot {
    param([string]$SourceInstanceId)

    $parsed = [guid]::Empty
    if (-not [guid]::TryParse($SourceInstanceId, [ref]$parsed)) {
        throw 'Cannot create backup because source instanceId is not a UUID.'
    }

    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $backupsRoot = Join-Path $script:ContextRoot 'backups'
    $backupRoot = Join-Path $backupsRoot (Join-Path $parsed.ToString() $timestamp)
    if (-not (Test-PathInside $backupRoot $backupsRoot)) {
        throw 'Backup target resolved outside .project-context/backups.'
    }
    return $backupRoot
}

function Plan-NewProject {
    $configuration = Get-Configuration
    if ($null -eq $configuration) {
        throw 'NewProject requires copied project-context.json. Use ExistingProject for a fresh framework.'
    }

    $managedInstance = Get-ManagedInstanceId
    if ($managedInstance -eq [string]$configuration.instanceId) {
        Plan-ManagedAgents $configuration
        return
    }

    $backupRoot = New-BackupRoot ([string]$configuration.instanceId)
    if (Test-Path -LiteralPath $script:ProjectRoot -PathType Container) {
        Add-MoveOperation $script:ProjectRoot (Join-Path $backupRoot 'project')
    }
    Add-MoveOperation $script:ConfigurationPath (Join-Path $backupRoot 'project-context.json')

    $newConfiguration = New-DefaultConfiguration
    Add-WriteOperation $script:ConfigurationPath (Convert-ConfigurationToJson $newConfiguration)
    Plan-AllProjectDocuments $newConfiguration
    Plan-ManagedAgents $newConfiguration
}

function Get-LegacyArray {
    param([object]$Configuration, [string]$Name)

    $property = $Configuration.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) {
        return @()
    }
    return @($property.Value)
}

function Plan-Upgrade {
    if (-not (Test-Path -LiteralPath $script:ConfigurationPath -PathType Leaf)) {
        throw 'Upgrade requires an existing project-context.json.'
    }

    $rawConfiguration = Read-Utf8Text $script:ConfigurationPath
    $configuration = $rawConfiguration | ConvertFrom-Json
    if ($configuration.schemaVersion -eq '1.0.0') {
        Plan-ManagedAgents $configuration
        return
    }
    if ($configuration.schemaVersion -ne '0.9.0') {
        throw "Unsupported schemaVersion $($configuration.schemaVersion); supported upgrade source is 0.9.0 and current is 1.0.0."
    }

    $backupRoot = New-BackupRoot ([string]$configuration.instanceId)
    Add-WriteOperation (Join-Path $backupRoot 'project-context.json') $rawConfiguration

    $migrated = [ordered]@{
        schemaVersion = '1.0.0'
        instanceId = [string]$configuration.instanceId
        projectName = [string]$configuration.projectName
        projectType = [string]$configuration.projectType
        sourceRoots = @(Get-LegacyArray $configuration 'sourceRoots')
        entryFiles = @(Get-LegacyArray $configuration 'entryFiles')
        testCommands = @(Get-LegacyArray $configuration 'testCommands')
        protectedPaths = @(Get-LegacyArray $configuration 'protectedPaths')
        additionalContextFiles = @(Get-LegacyArray $configuration 'additionalContextFiles')
        ignorePatterns = @(Get-LegacyArray $configuration 'ignorePatterns')
        workstreamStaleDays = 14
    }
    Add-WriteOperation $script:ConfigurationPath (Convert-ConfigurationToJson $migrated)
    Plan-ManagedAgents $migrated
}

try {
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        throw "Repository root does not exist: $Root"
    }
    if (-not (Test-PathInside $script:ContextRoot $Root)) {
        throw 'Context root resolved outside repository.'
    }

    switch ($Mode) {
        'ExistingProject' { Plan-ExistingProject }
        'Repair' { Plan-Repair }
        'NewProject' { Plan-NewProject }
        'Upgrade' { Plan-Upgrade }
    }

    Invoke-Operations $script:Operations

    if ($Apply) {
        $checker = Join-Path $PSScriptRoot 'check.ps1'
        if (Test-Path -LiteralPath $checker -PathType Leaf) {
            $shell = (Get-Process -Id $PID).Path
            & $shell -NoProfile -ExecutionPolicy Bypass -File $checker -Root $Root
            if ($LASTEXITCODE -ne 0) {
                throw "Context check failed after apply with exit code $LASTEXITCODE."
            }
        }
    }

    exit 0
}
catch {
    Write-Host "[context:error] $($_.Exception.Message)"
    exit 1
}
