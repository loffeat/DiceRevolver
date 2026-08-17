param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [ValidateRange(1, 3650)]
    [int]$StaleAfterDays = 14
)

$ErrorActionPreference = 'Stop'
$Root = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
$script:Errors = [System.Collections.Generic.List[string]]::new()
$script:Warnings = [System.Collections.Generic.List[string]]::new()
$script:ValidStatuses = @('planned', 'active', 'blocked', 'completed', 'superseded')
$script:ValidVerification = @('passed', 'failed', 'not-run', 'blocked')

function Add-ContextError {
    param([string]$Message)
    $script:Errors.Add($Message)
}

function Add-ContextWarning {
    param([string]$Message)
    $script:Warnings.Add($Message)
}

function Get-FullContextPath {
    param([string]$RelativePath)
    return [System.IO.Path]::GetFullPath((Join-Path $Root $RelativePath))
}

function Read-Utf8File {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path)
}

function Assert-RequiredFile {
    param([string]$RelativePath)

    $path = Get-FullContextPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-ContextError "missing required file: $RelativePath"
        return $false
    }

    return $true
}

function Assert-Headings {
    param([string]$RelativePath, [string[]]$Headings)

    $path = Get-FullContextPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        return
    }

    $text = Read-Utf8File $path
    foreach ($heading in $Headings) {
        if ($text -notmatch "(?m)^## $([regex]::Escape($heading))\s*$") {
            Add-ContextError "$RelativePath is missing heading: ## $heading"
        }
    }
}

function Remove-FencedCode {
    param([string]$Text)

    $insideFence = $false
    $result = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($Text -split "`r?`n")) {
        if ($line -match '^\s*```') {
            $insideFence = -not $insideFence
            $result.Add('')
            continue
        }

        if ($insideFence) {
            $result.Add('')
        }
        else {
            $result.Add($line)
        }
    }

    return $result -join "`n"
}

function Get-MarkdownSection {
    param([string]$Text, [string]$Heading)

    $pattern = "(?ms)^## $([regex]::Escape($Heading))\s*\r?\n(?<body>.*?)(?=^## |\z)"
    $match = [regex]::Match($Text, $pattern)
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups['body'].Value.Trim()
}

function Assert-LocalLinks {
    param([string]$Path, [string]$Text)

    $scannable = Remove-FencedCode $Text
    $matches = [regex]::Matches($scannable, '!?' + '\[[^\]]*\]\((?<target>[^)\s]+)')
    foreach ($match in $matches) {
        $target = $match.Groups['target'].Value.Trim('<', '>')
        if ($target -match '^(https?:|mailto:|#)') {
            continue
        }

        $target = ($target -split '[#?]', 2)[0]
        if ([string]::IsNullOrWhiteSpace($target)) {
            continue
        }

        try {
            $resolved = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $Path -Parent) $target))
        }
        catch {
            Add-ContextError "invalid local link '$target' in $($Path.Substring($Root.Length + 1))"
            continue
        }

        if (-not (Test-Path -LiteralPath $resolved)) {
            Add-ContextError "broken local link '$target' in $($Path.Substring($Root.Length + 1))"
        }
    }
}

function Assert-NoPlaceholders {
    param([string]$Path, [string]$Text)

    $scannable = Remove-FencedCode $Text
    if ($scannable -match '\{\{[A-Z0-9_]+\}\}') {
        Add-ContextError "unexpanded template placeholder in $($Path.Substring($Root.Length + 1))"
    }

    if ($scannable -match '(?i)\bTBD\b') {
        Add-ContextError "TBD placeholder in $($Path.Substring($Root.Length + 1))"
    }

    if ($scannable -match '(?i)\bTODO\b') {
        Add-ContextError "TODO placeholder in $($Path.Substring($Root.Length + 1))"
    }
}

function Test-RelativePathValue {
    param([string]$PropertyName, [string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        Add-ContextError "configuration $PropertyName contains an empty value"
        return
    }

    if ([System.IO.Path]::IsPathRooted($Value)) {
        Add-ContextError "configuration $PropertyName must be repository-relative: $Value"
        return
    }

    try {
        $resolved = [System.IO.Path]::GetFullPath((Join-Path $Root $Value))
        if (-not ($resolved -eq $Root -or $resolved.StartsWith($Root + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase))) {
            Add-ContextError "configuration $PropertyName escapes repository root: $Value"
        }
    }
    catch {
        Add-ContextError "configuration $PropertyName contains an invalid path: $Value"
    }
}

function Test-StringArray {
    param([object]$Configuration, [string]$PropertyName, [switch]$Paths)

    $property = $Configuration.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        Add-ContextError "configuration is missing array: $PropertyName"
        return
    }

    $values = @($property.Value)
    $seen = @{}
    foreach ($value in $values) {
        if (-not ($value -is [string]) -or [string]::IsNullOrWhiteSpace($value)) {
            Add-ContextError "configuration $PropertyName must contain non-empty strings"
            continue
        }

        if ($seen.ContainsKey($value)) {
            Add-ContextError "configuration $PropertyName contains duplicate value: $value"
        }
        $seen[$value] = $true

        if ($Paths) {
            Test-RelativePathValue $PropertyName $value
        }
    }
}

function Test-Configuration {
    param([object]$Configuration)

    $required = @(
        'schemaVersion', 'instanceId', 'projectName', 'projectType', 'sourceRoots',
        'entryFiles', 'testCommands', 'protectedPaths', 'additionalContextFiles',
        'ignorePatterns', 'workstreamStaleDays'
    )
    $actualProperties = @($Configuration.PSObject.Properties.Name)
    foreach ($name in $required) {
        if ($name -notin $actualProperties) {
            Add-ContextError "configuration is missing property: $name"
        }
    }
    foreach ($name in $actualProperties) {
        if ($name -notin $required) {
            Add-ContextError "configuration contains unsupported property: $name"
        }
    }

    if ($Configuration.schemaVersion -ne '1.0.0') {
        Add-ContextError "unsupported schemaVersion: $($Configuration.schemaVersion)"
    }

    $parsedId = [guid]::Empty
    if (-not [guid]::TryParse([string]$Configuration.instanceId, [ref]$parsedId)) {
        Add-ContextError 'configuration instanceId must be a UUID'
    }

    if ([string]::IsNullOrWhiteSpace([string]$Configuration.projectName)) {
        Add-ContextError 'configuration projectName must not be empty'
    }

    if ($Configuration.projectType -notin @('unity', 'web', 'library', 'service', 'mixed')) {
        Add-ContextError "unsupported projectType: $($Configuration.projectType)"
    }

    Test-StringArray $Configuration 'sourceRoots' -Paths
    Test-StringArray $Configuration 'entryFiles' -Paths
    Test-StringArray $Configuration 'testCommands'
    Test-StringArray $Configuration 'protectedPaths' -Paths
    Test-StringArray $Configuration 'additionalContextFiles' -Paths
    Test-StringArray $Configuration 'ignorePatterns'

    $staleDays = 0
    if (-not [int]::TryParse([string]$Configuration.workstreamStaleDays, [ref]$staleDays) -or $staleDays -lt 1 -or $staleDays -gt 3650) {
        Add-ContextError 'configuration workstreamStaleDays must be an integer from 1 to 3650'
    }
}

function Test-VerificationSection {
    param([string]$RelativePath, [string]$Text)

    $section = Get-MarkdownSection $Text '验证记录'
    if ($null -eq $section) {
        return
    }

    foreach ($line in ($section -split "`r?`n")) {
        if ($line -match '^\s*-\s+' -and $line -notmatch '^\s*-\s+\[(passed|failed|not-run|blocked)\]\s+.+') {
            Add-ContextError "$RelativePath has an invalid verification record: $line"
        }
    }
}

function Test-Workstream {
    param([System.IO.DirectoryInfo]$Directory, [int]$EffectiveStaleDays)

    if ($Directory.Name -notmatch '^\d{4}-\d{2}-\d{2}-[a-z0-9]+(?:-[a-z0-9]+)*$') {
        Add-ContextError "invalid workstream directory name: $($Directory.Name)"
    }

    $statePath = Join-Path $Directory.FullName 'STATE.md'
    $handoffPath = Join-Path $Directory.FullName 'HANDOFF.md'
    $stateRelative = $statePath.Substring($Root.Length + 1)
    $handoffRelative = $handoffPath.Substring($Root.Length + 1)
    if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) {
        Add-ContextError "missing workstream state: $stateRelative"
        return
    }
    if (-not (Test-Path -LiteralPath $handoffPath -PathType Leaf)) {
        Add-ContextError "missing workstream handoff: $handoffRelative"
    }

    Assert-Headings $stateRelative @('目标', '非目标', '已确认事实', '已完成', '当前正在进行', '下一步', '阻塞', '涉及文件', '验证记录', '相关资料')
    Assert-Headings $handoffRelative @('当前状态', '当前方案及原因', '尚未完成', '下一步首个动作', '首先读取', '必须保护', '最近验证', '风险与不可假定事项')

    $text = Read-Utf8File $statePath
    $statusMatch = [regex]::Match($text, '(?m)^- Status: `(?<value>[^`]+)`\s*$')
    $updatedMatch = [regex]::Match($text, '(?m)^- Updated: `(?<value>\d{4}-\d{2}-\d{2})`\s*$')
    $idMatch = [regex]::Match($text, '(?m)^- ID: `(?<value>[^`]+)`\s*$')
    if (-not $idMatch.Success -or $idMatch.Groups['value'].Value -ne $Directory.Name) {
        Add-ContextError "$stateRelative ID must match its directory name"
    }

    if (-not $statusMatch.Success -or $statusMatch.Groups['value'].Value -notin $script:ValidStatuses) {
        Add-ContextError "$stateRelative has an invalid or missing status"
    }

    $updated = [datetime]::MinValue
    if (-not $updatedMatch.Success -or -not [datetime]::TryParseExact($updatedMatch.Groups['value'].Value, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$updated)) {
        Add-ContextError "$stateRelative has an invalid or missing updated date"
    }

    if ($statusMatch.Success -and $statusMatch.Groups['value'].Value -eq 'completed') {
        $current = Get-MarkdownSection $text '当前正在进行'
        if ($current -ne '- 无') {
            Add-ContextError "$stateRelative is completed but current work is not exactly '- 无'"
        }
    }

    if ($updated -ne [datetime]::MinValue -and $statusMatch.Success -and $statusMatch.Groups['value'].Value -in @('planned', 'active', 'blocked')) {
        if (((Get-Date).Date - $updated.Date).TotalDays -gt $EffectiveStaleDays) {
            Add-ContextWarning "$stateRelative has not been updated for more than $EffectiveStaleDays days"
        }
    }

    Test-VerificationSection $stateRelative $text
}

$requiredFiles = @(
    'AGENTS.md',
    '.project-context/project-context.json',
    '.project-context/framework/PROTOCOL.md',
    '.project-context/framework/schema.json',
    '.project-context/framework/templates/PROJECT.md',
    '.project-context/framework/templates/STATUS.md',
    '.project-context/framework/templates/DECISIONS.md',
    '.project-context/framework/templates/ENVIRONMENT.md',
    '.project-context/framework/templates/WORKSTREAM_STATE.md',
    '.project-context/framework/templates/WORKSTREAM_HANDOFF.md',
    '.project-context/project/PROJECT.md',
    '.project-context/project/STATUS.md',
    '.project-context/project/DECISIONS.md',
    '.project-context/project/ENVIRONMENT.md'
)
foreach ($relativePath in $requiredFiles) {
    Assert-RequiredFile $relativePath | Out-Null
}

$configuration = $null
$configurationPath = Get-FullContextPath '.project-context/project-context.json'
if (Test-Path -LiteralPath $configurationPath -PathType Leaf) {
    try {
        $configuration = Read-Utf8File $configurationPath | ConvertFrom-Json
        Test-Configuration $configuration
    }
    catch {
        Add-ContextError "invalid project-context.json: $($_.Exception.Message)"
    }
}

if ($null -ne $configuration -and -not $PSBoundParameters.ContainsKey('StaleAfterDays')) {
    $StaleAfterDays = [int]$configuration.workstreamStaleDays
}

Assert-Headings '.project-context/framework/PROTOCOL.md' @('启动读取顺序', '开始或继续工作流', '执行期间', '结束前强制更新', '事实优先级', '并发与合并', '安全规则', '校验命令')
Assert-Headings '.project-context/project/PROJECT.md' @('项目目标', '当前玩法或业务循环', '技术栈', '运行入口与操作', '目录地图', '核心模块', '关键数据流', '明确非目标', '术语')
Assert-Headings '.project-context/project/STATUS.md' @('当前阶段', '全局有效状态', '活跃工作流', '完成历史', '已知缺口', '最近项目级验证')
Assert-Headings '.project-context/project/DECISIONS.md' @('记录规则')
Assert-Headings '.project-context/project/ENVIRONMENT.md' @('仓库硬性要求', '推荐配置', '首次克隆', '运行', '测试与构建', 'Git LFS', '设备本地差异', '已知环境问题')

if ($null -ne $configuration -and (Test-Path -LiteralPath (Get-FullContextPath 'AGENTS.md'))) {
    $agentsText = Read-Utf8File (Get-FullContextPath 'AGENTS.md')
    $beginCount = ([regex]::Matches($agentsText, '<!-- project-context:begin -->')).Count
    $endCount = ([regex]::Matches($agentsText, '<!-- project-context:end -->')).Count
    if ($beginCount -ne 1 -or $endCount -ne 1) {
        Add-ContextError 'AGENTS.md must contain exactly one managed marker pair'
    }
    $instanceMatch = [regex]::Match($agentsText, '<!-- project-context:instance (?<id>[0-9a-fA-F-]+) -->')
    if (-not $instanceMatch.Success -or $instanceMatch.Groups['id'].Value -ne [string]$configuration.instanceId) {
        Add-ContextError 'AGENTS.md managed instance does not match project-context.json'
    }
}

$workstreamsRoot = Get-FullContextPath '.project-context/project/workstreams'
if (Test-Path -LiteralPath $workstreamsRoot -PathType Container) {
    Get-ChildItem -LiteralPath $workstreamsRoot -Directory | ForEach-Object {
        Test-Workstream $_ $StaleAfterDays
    }
}

$markdownPaths = [System.Collections.Generic.List[string]]::new()
foreach ($relative in @('AGENTS.md', '.project-context/framework/PROTOCOL.md')) {
    $path = Get-FullContextPath $relative
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $markdownPaths.Add($path)
    }
}
$projectRoot = Get-FullContextPath '.project-context/project'
if (Test-Path -LiteralPath $projectRoot -PathType Container) {
    Get-ChildItem -LiteralPath $projectRoot -Recurse -File -Filter '*.md' | ForEach-Object { $markdownPaths.Add($_.FullName) }
}

foreach ($path in $markdownPaths) {
    $text = Read-Utf8File $path
    Assert-LocalLinks $path $text
    Assert-NoPlaceholders $path $text
}

$script:Errors | ForEach-Object { Write-Host "[context:error] $_" }
$script:Warnings | ForEach-Object { Write-Host "[context:warning] $_" }
if ($script:Errors.Count -gt 0) {
    exit 1
}

Write-Host '[context:ok] project context is structurally valid'
exit 0
