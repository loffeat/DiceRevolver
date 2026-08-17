$ErrorActionPreference = 'Stop'

$script:Shell = (Get-Process -Id $PID).Path
$script:Checker = Join-Path (Split-Path $PSScriptRoot -Parent) 'check.ps1'
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

function New-CheckFixture {
    param([ValidateSet('unity', 'web', 'library')][string]$Fixture)

    $root = Join-Path ([System.IO.Path]::GetTempPath()) ("dice-context-check-" + [guid]::NewGuid())
    New-Item -ItemType Directory -Path $root -Force | Out-Null

    $contextRoot = Join-Path $root '.project-context'
    New-Item -ItemType Directory -Path $contextRoot -Force | Out-Null
    Copy-Item -LiteralPath $script:FrameworkRoot -Destination (Join-Path $contextRoot 'framework') -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "fixtures\$Fixture\project-context.json") -Destination (Join-Path $contextRoot 'project-context.json')

    $configuration = Get-Content -LiteralPath (Join-Path $contextRoot 'project-context.json') -Raw | ConvertFrom-Json
    $agents = @"
# Fixture agents

<!-- project-context:begin -->
<!-- project-context:instance $($configuration.instanceId) -->
Read ``.project-context/framework/PROTOCOL.md``.
<!-- project-context:end -->
"@
    Write-Utf8Text (Join-Path $root 'AGENTS.md') $agents

    Write-Utf8Text (Join-Path $contextRoot 'project\PROJECT.md') @'
# Fixture 项目地图
## 项目目标
- Validate portable context.
## 当前玩法或业务循环
- Input produces output.
## 技术栈
- Fixture tools.
## 运行入口与操作
- Use the fixture entry.
## 目录地图
- Source roots are configured.
## 核心模块
- Context module.
## 关键数据流
- Configuration to checker.
## 明确非目标
- No runtime product.
## 术语
- Fixture: isolated test repository.
'@
    Write-Utf8Text (Join-Path $contextRoot 'project\STATUS.md') @'
# Fixture 项目状态
## 当前阶段
- Validation.
## 全局有效状态
- Fixture is synthetic.
## 活跃工作流
- [Example](workstreams/2026-08-17-example/STATE.md)
## 完成历史
- 无
## 已知缺口
- 无
## 最近项目级验证
- [not-run] Checker will run in this test.
'@
    Write-Utf8Text (Join-Path $contextRoot 'project\DECISIONS.md') @'
# Fixture 架构决策日志
## 记录规则
- Append decisions.
'@
    Write-Utf8Text (Join-Path $contextRoot 'project\ENVIRONMENT.md') @'
# Fixture 开发环境与跨设备准备
## 仓库硬性要求
- PowerShell.
## 推荐配置
- 无
## 首次克隆
- Copy fixture.
## 运行
- Run checker.
## 测试与构建
- Run test script.
## Git LFS
- Not used.
## 设备本地差异
- 无
## 已知环境问题
- 无
'@
    Write-Utf8Text (Join-Path $contextRoot 'project\workstreams\2026-08-17-example\STATE.md') @'
# Example
- ID: `2026-08-17-example`
- Status: `active`
- Branch: `main`
- Created: `2026-08-17`
- Updated: `2026-08-17`
## 目标
- Validate the fixture.
## 非目标
- 无
## 已确认事实
- Fixture is synthetic.
## 已完成
- Structure created.
## 当前正在进行
- Run checker.
## 下一步
1. Inspect result.
## 阻塞
- 无
## 涉及文件
- `STATE.md`
## 验证记录
- [not-run] Checker has not run yet.
## 相关资料
- [Project](../../PROJECT.md)
'@
    Write-Utf8Text (Join-Path $contextRoot 'project\workstreams\2026-08-17-example\HANDOFF.md') @'
# Example 交接
## 当前状态
- Ready for validation.
## 当前方案及原因
- Minimal fixture.
## 尚未完成
- Run checker.
## 下一步首个动作
- Run checker.
## 首先读取
- [State](STATE.md)
## 必须保护
- Fixture isolation.
## 最近验证
- [not-run] Checker has not run yet.
## 风险与不可假定事项
- Do not treat fixture as a real project.
'@

    return $root
}

function Set-FileText {
    param([string]$Path, [scriptblock]$Transform)

    $text = [System.IO.File]::ReadAllText($Path)
    Write-Utf8Text $Path (& $Transform $text)
}

$removeProjectHeading = {
    param($root)
    $path = Join-Path $root '.project-context\project\PROJECT.md'
    Set-FileText $path { param($text) $text.Replace("## 技术栈`r`n", '').Replace("## 技术栈`n", '') }
}
$setIllegalStatus = {
    param($root)
    $path = Join-Path $root '.project-context\project\workstreams\2026-08-17-example\STATE.md'
    Set-FileText $path { param($text) $text.Replace('- Status: `active`', '- Status: `paused`') }
}
$addBrokenLink = {
    param($root)
    Add-Content -LiteralPath (Join-Path $root '.project-context\project\STATUS.md') -Value '[Missing](workstreams/missing/STATE.md)' -Encoding UTF8
}
$addPlaceholder = {
    param($root)
    Add-Content -LiteralPath (Join-Path $root '.project-context\project\PROJECT.md') -Value '{{PROJECT_NAME}}' -Encoding UTF8
}
$completeWithCurrentWork = {
    param($root)
    $path = Join-Path $root '.project-context\project\workstreams\2026-08-17-example\STATE.md'
    Set-FileText $path { param($text) $text.Replace('- Status: `active`', '- Status: `completed`') }
}
$setHighSchema = {
    param($root)
    $path = Join-Path $root '.project-context\project-context.json'
    Set-FileText $path { param($text) $text.Replace('"schemaVersion": "1.0.0"', '"schemaVersion": "2.0.0"') }
}

function Assert-Check {
    param(
        [string]$Name,
        [ValidateSet('unity', 'web', 'library')][string]$Fixture,
        [int]$ExpectedExit,
        [scriptblock]$Mutate = $null
    )

    $root = New-CheckFixture -Fixture $Fixture
    try {
        if ($null -ne $Mutate) {
            & $Mutate $root
        }

        & $script:Shell -NoProfile -ExecutionPolicy Bypass -File $script:Checker -Root $root *> $null
        $actualExit = $LASTEXITCODE
        if ($actualExit -ne $ExpectedExit) {
            $script:Failures.Add("$Name expected exit $ExpectedExit but received $actualExit")
        }
        else {
            Write-Host "[test:ok] $Name"
        }
    }
    finally {
        if (Test-Path -LiteralPath $root) {
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }
}

Assert-Check -Name 'valid unity fixture' -Fixture 'unity' -ExpectedExit 0
Assert-Check -Name 'valid web fixture' -Fixture 'web' -ExpectedExit 0
Assert-Check -Name 'valid library fixture' -Fixture 'library' -ExpectedExit 0
Assert-Check -Name 'missing project heading' -Fixture 'unity' -ExpectedExit 1 -Mutate $removeProjectHeading
Assert-Check -Name 'illegal workstream status' -Fixture 'unity' -ExpectedExit 1 -Mutate $setIllegalStatus
Assert-Check -Name 'broken local link' -Fixture 'unity' -ExpectedExit 1 -Mutate $addBrokenLink
Assert-Check -Name 'actual placeholder' -Fixture 'unity' -ExpectedExit 1 -Mutate $addPlaceholder
Assert-Check -Name 'completed work still active' -Fixture 'unity' -ExpectedExit 1 -Mutate $completeWithCurrentWork
Assert-Check -Name 'high schema rejected' -Fixture 'unity' -ExpectedExit 1 -Mutate $setHighSchema

if ($script:Failures.Count -gt 0) {
    $script:Failures | ForEach-Object { Write-Host "[test:error] $_" }
    exit 1
}

Write-Host '[test:ok] context checker acceptance tests passed'
exit 0
