# Portable Project Context System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Package the Codex context, handoff, validation, and installation system inside `.project-context/` so it can be copied and safely adapted to DiceRevolver or another repository.

**Architecture:** Keep reusable behavior in `.project-context/framework/` and instance-specific knowledge in `.project-context/project/`. A thin managed block in root `AGENTS.md` points Codex to the framework protocol; dependency-free PowerShell scripts install, repair, migrate, and validate the package with preview-first writes.

**Tech Stack:** Markdown, JSON, Windows PowerShell 5.1-compatible PowerShell, Git. No Pester, JSON Schema package, or Unity runtime dependency.

## Global Constraints

- Except for a thin root `AGENTS.md`, every new system file lives under `.project-context/`.
- `schemaVersion` starts at `1.0.0`; `instanceId` is a UUID.
- Installer modes are exactly `NewProject`, `ExistingProject`, `Upgrade`, and `Repair`.
- Installer runs are read-only unless `-Apply` is explicitly supplied.
- Preview must make zero filesystem changes; repeated `-Apply` with the same inputs must be idempotent.
- Never silently overwrite project context, human-authored `AGENTS.md` content, secrets, or device-specific data.
- Workstream IDs match `YYYY-MM-DD-topic-slug`; statuses are `planned`, `active`, `blocked`, `completed`, or `superseded`.
- Verification labels are `passed`, `failed`, `not-run`, or `blocked`.
- Actual code, Git state, and verification output override stale documentation.
- Do not modify `Assets/`, `Packages/`, or `ProjectSettings/`.

---

### Task 1: Framework Contract, Protocol, and Templates

**Files:**
- Create: `.project-context/project-context.json`
- Create: `.project-context/framework/schema.json`
- Create: `.project-context/framework/PROTOCOL.md`
- Create: `.project-context/framework/templates/PROJECT.md`
- Create: `.project-context/framework/templates/STATUS.md`
- Create: `.project-context/framework/templates/DECISIONS.md`
- Create: `.project-context/framework/templates/ENVIRONMENT.md`
- Create: `.project-context/framework/templates/WORKSTREAM_STATE.md`
- Create: `.project-context/framework/templates/WORKSTREAM_HANDOFF.md`

**Interfaces:**
- Produces: configuration contract consumed by `check.ps1` and `install.ps1`.
- Produces: project-neutral protocol and templates using `{{UPPER_CASE_FIELD}}` tokens.
- Consumes: the approved design at `docs/superpowers/specs/2026-08-17-project-context-system-design.md`.

- [ ] **Step 1: Create the schema contract**

Create `schema.json` with Draft 2020-12 metadata and this required shape:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://local.invalid/project-context.schema.json",
  "title": "Portable Project Context Configuration",
  "type": "object",
  "additionalProperties": false,
  "required": [
    "schemaVersion", "instanceId", "projectName", "projectType",
    "sourceRoots", "entryFiles", "testCommands", "protectedPaths",
    "additionalContextFiles", "ignorePatterns", "workstreamStaleDays"
  ],
  "properties": {
    "schemaVersion": { "const": "1.0.0" },
    "instanceId": { "type": "string", "format": "uuid" },
    "projectName": { "type": "string", "minLength": 1 },
    "projectType": { "enum": ["unity", "web", "library", "service", "mixed"] },
    "sourceRoots": { "$ref": "#/$defs/pathArray" },
    "entryFiles": { "$ref": "#/$defs/pathArray" },
    "testCommands": { "type": "array", "items": { "type": "string", "minLength": 1 }, "uniqueItems": true },
    "protectedPaths": { "$ref": "#/$defs/pathArray" },
    "additionalContextFiles": { "$ref": "#/$defs/pathArray" },
    "ignorePatterns": { "type": "array", "items": { "type": "string", "minLength": 1 }, "uniqueItems": true },
    "workstreamStaleDays": { "type": "integer", "minimum": 1, "maximum": 3650 }
  },
  "$defs": {
    "pathArray": {
      "type": "array",
      "items": { "type": "string", "minLength": 1, "pattern": "^(?![A-Za-z]:|/|\\\\).*" },
      "uniqueItems": true
    }
  }
}
```

- [ ] **Step 2: Create the DiceRevolver instance configuration**

Generate a UUID for `instanceId` and create:

```json
{
  "schemaVersion": "1.0.0",
  "instanceId": "dc7e71ab-741f-4cc8-9880-92b9c821a4fe",
  "projectName": "DiceRevolver",
  "projectType": "unity",
  "sourceRoots": ["Assets/Scripts", "Assets/Tests"],
  "entryFiles": ["ProjectSettings/ProjectVersion.txt", "Packages/manifest.json", "ProjectSettings/EditorBuildSettings.asset"],
  "testCommands": [
    "powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1"
  ],
  "protectedPaths": ["Assets", "Packages", "ProjectSettings"],
  "additionalContextFiles": ["docs/superpowers/specs", "docs/superpowers/plans", ".superpowers/sdd"],
  "ignorePatterns": ["Library/**", "Temp/**", "Logs/**", "obj/**", ".git/**"],
  "workstreamStaleDays": 14
}
```

- [ ] **Step 3: Write the project-neutral protocol**

Create `PROTOCOL.md` with these exact headings:

```markdown
# Codex 项目上下文协议
## 启动读取顺序
## 开始或继续工作流
## 执行期间
## 结束前强制更新
## 事实优先级
## 并发与合并
## 安全规则
## 校验命令
```

Include the seven-step startup sequence and seven-step ending sequence from the spec. The validation command is:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
```

- [ ] **Step 4: Create all six templates**

Use `{{PROJECT_NAME}}`, `{{WORKSTREAM_ID}}`, `{{TITLE}}`, `{{STATUS}}`, `{{BRANCH}}`, `{{CREATED_DATE}}`, and `{{UPDATED_DATE}}` only inside templates. Required headings are:

```text
PROJECT: 项目目标, 当前玩法或业务循环, 技术栈, 运行入口与操作, 目录地图, 核心模块, 关键数据流, 明确非目标, 术语
STATUS: 当前阶段, 全局有效状态, 活跃工作流, 完成历史, 已知缺口, 最近项目级验证
DECISIONS: 记录规则
ENVIRONMENT: 仓库硬性要求, 推荐配置, 首次克隆, 运行, 测试与构建, Git LFS, 设备本地差异, 已知环境问题
WORKSTREAM_STATE: 目标, 非目标, 已确认事实, 已完成, 当前正在进行, 下一步, 阻塞, 涉及文件, 验证记录, 相关资料
WORKSTREAM_HANDOFF: 当前状态, 当前方案及原因, 尚未完成, 下一步首个动作, 首先读取, 必须保护, 最近验证, 风险与不可假定事项
```

Every template heading gets one HTML comment explaining the required content. Verification examples use all four legal labels.

- [ ] **Step 5: Inspect framework files for project leakage**

Run:

```powershell
rg -n "DiceRevolver|Unity|Assets/|ProjectSettings/" .project-context/framework
```

Expected: no output. Project-specific values belong only in `project-context.json` and `.project-context/project/`.

- [ ] **Step 6: Commit the framework contract**

```powershell
git add .project-context/project-context.json .project-context/framework docs/superpowers/plans/2026-08-17-portable-project-context-system.md
git commit -m "docs: add portable context framework contract"
```

---

### Task 2: Structural Checker with Fixture Tests

**Files:**
- Create: `.project-context/framework/scripts/check.ps1`
- Create: `.project-context/framework/scripts/tests/check-tests.ps1`
- Create: `.project-context/framework/scripts/tests/fixtures/unity/project-context.json`
- Create: `.project-context/framework/scripts/tests/fixtures/web/project-context.json`
- Create: `.project-context/framework/scripts/tests/fixtures/library/project-context.json`

**Interfaces:**
- Produces: `check.ps1 [-Root <repository-root>] [-StaleAfterDays <positive-int>]`.
- Produces: exit `0` with `[context:ok]` when valid; exit `1` with `[context:error]` when invalid.
- Consumes: `project-context.json`, `schema.json`, protocol, templates, project documents, and workstreams.

- [ ] **Step 1: Write failing checker tests**

`check-tests.ps1` must create isolated temporary repositories and invoke the checker using `(Get-Process -Id $PID).Path`. Implement these assertions:

```powershell
function Assert-Check {
    param(
        [string]$Name,
        [string]$Fixture,
        [int]$ExpectedExit,
        [scriptblock]$Mutate = $null
    )
    $root = New-CheckFixture -Fixture $Fixture
    try {
        if ($null -ne $Mutate) { & $Mutate $root }
        & $shell -NoProfile -ExecutionPolicy Bypass -File $checker -Root $root *> $null
        if ($LASTEXITCODE -ne $ExpectedExit) {
            $failures.Add("$Name expected $ExpectedExit but received $LASTEXITCODE")
        }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

Assert-Check -Name 'valid unity fixture'   -Fixture 'unity'   -ExpectedExit 0
Assert-Check -Name 'valid web fixture'     -Fixture 'web'     -ExpectedExit 0
Assert-Check -Name 'valid library fixture' -Fixture 'library' -ExpectedExit 0
Assert-Check -Name 'missing project heading' -Fixture 'unity' -ExpectedExit 1 -Mutate $removeProjectHeading
Assert-Check -Name 'illegal status' -Fixture 'unity' -ExpectedExit 1 -Mutate $setIllegalStatus
Assert-Check -Name 'broken local link' -Fixture 'unity' -ExpectedExit 1 -Mutate $addBrokenLink
Assert-Check -Name 'actual placeholder' -Fixture 'unity' -ExpectedExit 1 -Mutate $addPlaceholder
Assert-Check -Name 'completed work still active' -Fixture 'unity' -ExpectedExit 1 -Mutate $completeWithCurrentWork
Assert-Check -Name 'high schema rejected' -Fixture 'unity' -ExpectedExit 1 -Mutate $setHighSchema
```

`New-CheckFixture -Fixture <unity|web|library>` copies the framework into a new GUID-named directory under `[System.IO.Path]::GetTempPath()`, copies the selected fixture config, writes a matching managed `AGENTS.md`, renders every project template by replacing all `{{...}}` tokens, and creates one valid active workstream. Define each mutation as a scriptblock that makes only the named defect; for example, `$setIllegalStatus` replaces `- Status: \`active\`` with `- Status: \`paused\`` in the generated state file.

Each fixture config uses the same schema with only these intended variations:

```text
unity: projectType=unity, sourceRoots=[Assets/Scripts], entryFiles=[ProjectSettings/ProjectVersion.txt]
web: projectType=web, sourceRoots=[src], entryFiles=[package.json]
library: projectType=library, sourceRoots=[src], entryFiles=[README.md]
```

- [ ] **Step 2: Run tests to verify missing checker fails**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/check-tests.ps1
```

Expected: non-zero exit because `check.ps1` does not exist.

- [ ] **Step 3: Implement checker helpers**

Create `check.ps1` with:

```powershell
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [ValidateRange(1, 3650)][int]$StaleAfterDays = 14
)
$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-ContextError { param([string]$Message) $script:errors.Add($Message) }
function Add-ContextWarning { param([string]$Message) $script:warnings.Add($Message) }
function Read-Utf8File { param([string]$Path) [System.IO.File]::ReadAllText($Path) }
function Assert-RequiredFile { param([string]$RelativePath) }
function Assert-Headings { param([string]$RelativePath, [string[]]$Headings) }
function Get-MarkdownSection { param([string]$Text, [string]$Heading) }
function Assert-LocalLinks { param([string]$Path, [string]$Text) }
function Assert-NoPlaceholders { param([string]$Path, [string]$Text) }
function Test-Configuration { param([object]$Configuration) }
function Test-Workstream { param([System.IO.DirectoryInfo]$Directory) }
```

Manual configuration validation must enforce every schema field, UUID parsing, legal `projectType`, repository-relative paths, unique array items, and stale-day range. Do not introduce a JSON Schema dependency.

- [ ] **Step 4: Implement Markdown and workstream validation**

Scan `AGENTS.md`, `.project-context/framework/PROTOCOL.md`, `.project-context/project/*.md`, and workstream Markdown. Ignore external links, anchors, fenced code, templates, and `.project-context/backups/`. Enforce the exact headings from Task 1, valid directory names, metadata lines, verification labels, completed-state current work, relative link existence, and placeholder rules.

Print diagnostics with:

```powershell
$errors   | ForEach-Object { Write-Host "[context:error] $_" }
$warnings | ForEach-Object { Write-Host "[context:warning] $_" }
if ($errors.Count -gt 0) { exit 1 }
Write-Host '[context:ok] project context is structurally valid'
exit 0
```

- [ ] **Step 5: Run checker tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/check-tests.ps1
```

Expected: exit `0`; all nine named assertions pass.

- [ ] **Step 6: Commit checker and fixtures**

```powershell
git add .project-context/framework/scripts/check.ps1 .project-context/framework/scripts/tests
git commit -m "test: validate portable project context"
```

---

### Task 3: Installer Preview, ExistingProject, and Repair

**Files:**
- Create: `.project-context/framework/scripts/install.ps1`
- Create: `.project-context/framework/scripts/tests/install-tests.ps1`
- Create: `.project-context/framework/templates/AGENTS_MANAGED_BLOCK.md`

**Interfaces:**
- Produces: `install.ps1 -Mode <NewProject|ExistingProject|Upgrade|Repair> [-Apply] [-Root <path>]`.
- Produces: preview lines prefixed `[context:plan]`; applied operations prefixed `[context:apply]`.
- Consumes: templates, schema `1.0.0`, current configuration, and managed markers.

- [ ] **Step 1: Write failing preview and ExistingProject tests**

Add test cases that hash the full fixture tree before and after preview, then validate:

```powershell
function Invoke-TestInstall {
    param([string]$Root, [string]$Mode, [switch]$Apply)
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $installer, '-Mode', $Mode, '-Root', $Root)
    if ($Apply) { $arguments += '-Apply' }
    & $shell @arguments *> $null
    return $LASTEXITCODE
}

function Get-TreeDigest {
    param([string]$Root)
    $records = Get-ChildItem -LiteralPath $Root -Recurse -File | Sort-Object FullName | ForEach-Object {
        "{0}|{1}" -f $_.FullName.Substring($Root.Length), (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($records -join "`n"))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return [Convert]::ToBase64String($sha.ComputeHash($bytes))
}

Assert-InstallPreviewMakesNoChanges -Mode ExistingProject
Assert-InstallApply -Name 'existing project creates config and project docs' -Mode ExistingProject
Assert-InstallApply -Name 'second existing project apply is idempotent' -Mode ExistingProject -Repeat 2
Assert-InstallApply -Name 'new AGENTS receives managed block' -Mode ExistingProject
Assert-InstallApply -Name 'human AGENTS content survives managed insertion' -Mode ExistingProject -SeedHumanAgents
Assert-InstallFails -Name 'unpaired managed marker stops' -Mode ExistingProject -SeedBrokenMarker
```

All `Assert-Install*` helpers create a temporary repository, arrange only the named initial state, call `Invoke-TestInstall`, inspect files and exit code, append failures to one shared list, and remove the temporary repository in `finally`. `Assert-InstallPreviewMakesNoChanges` compares `Get-TreeDigest` before and after. `Assert-InstallApply -Repeat 2` compares the digest after the first and second apply.

The managed block is exactly:

```markdown
<!-- project-context:begin -->
<!-- project-context:instance {{INSTANCE_ID}} -->
## Portable project context

Before changing this repository, read `.project-context/framework/PROTOCOL.md` and follow it.

Validate context with:
`powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1`
<!-- project-context:end -->
```

- [ ] **Step 2: Run installer tests and verify failure**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/install-tests.ps1
```

Expected: non-zero exit because `install.ps1` is missing.

- [ ] **Step 3: Implement preview-safe operation planning**

Use an operation list rather than writing during discovery:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('NewProject','ExistingProject','Upgrade','Repair')]
    [string]$Mode,
    [switch]$Apply,
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
)

$operations = [System.Collections.Generic.List[object]]::new()
function Add-WriteOperation { param([string]$Path, [string]$Content) }
function Add-MoveOperation { param([string]$Source, [string]$Destination) }
function Test-PathInside { param([string]$Candidate, [string]$ExpectedParent) }
function Get-ManagedAgentsContent { param([string]$ExistingContent) }
function Invoke-Operations { param([System.Collections.Generic.List[object]]$Operations) }
```

`Add-*Operation` only records intent. `Invoke-Operations` prints the plan and returns without writes unless `$Apply`; every applied target must pass `Test-PathInside`.

- [ ] **Step 4: Implement ExistingProject and Repair**

`ExistingProject` creates a `1.0.0` config using the repository folder name, project type `mixed`, empty adaptable arrays, stale days `14`, new UUID, and all project documents from templates. It inserts the managed root block without altering text outside markers.

`Repair` only repairs or inserts the managed block and reports missing framework or project files. It does not overwrite or regenerate existing project documents.

- [ ] **Step 5: Run installer tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/install-tests.ps1
```

Expected: preview, idempotence, preservation, and marker-conflict cases pass.

- [ ] **Step 6: Commit installer foundation**

```powershell
git add .project-context/framework/scripts/install.ps1 .project-context/framework/scripts/tests/install-tests.ps1 .project-context/framework/templates/AGENTS_MANAGED_BLOCK.md
git commit -m "feat: install portable project context safely"
```

---

### Task 4: NewProject Backup and Upgrade Migration

**Files:**
- Modify: `.project-context/framework/scripts/install.ps1`
- Modify: `.project-context/framework/scripts/tests/install-tests.ps1`

**Interfaces:**
- Extends: installer `NewProject` and `Upgrade` modes.
- Produces: backups under `.project-context/backups/<source-instance-id>/<UTC-timestamp>/`.
- Produces: explicit `0.9.0` to `1.0.0` migration; rejects versions greater than `1.0.0`.

- [ ] **Step 1: Write failing NewProject tests**

Add:

```powershell
Assert-InstallPreviewMakesNoChanges -Mode NewProject -SeedInitializedProject
Assert-NewProjectBackupContains 'project/PROJECT.md'
Assert-NewProjectBackupContains 'project-context.json'
Assert-NewProjectCreatesDifferentInstanceId
Assert-NewProjectUsesTargetFolderName
Assert-InstallApply -Name 'second new-project apply is idempotent after rebinding' -Mode NewProject -Repeat 2
```

The second run recognizes that the instance UUID embedded in the root managed block matches `project-context.json`, and plans no backup or reset.

- [ ] **Step 2: Implement NewProject backup and rebind**

Before adding move/write operations:

```powershell
$contextRoot = Join-Path $Root '.project-context'
$sourceId = [guid]::Parse($configuration.instanceId).ToString()
$timestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$backupRoot = Join-Path $contextRoot ("backups/$sourceId/$timestamp")
```

Verify `$backupRoot` is inside `.project-context/backups`, back up `project/` and `project-context.json`, generate a new UUID and target-folder project name, seed clean documents, then update the managed root block with the new UUID. A matching managed-block UUID and configuration UUID means the instance is already bound and makes a repeated `NewProject -Apply` a no-op; a missing or different UUID means the copied package must be rebound.

- [ ] **Step 3: Write failing upgrade tests**

Add:

```powershell
Assert-UpgradeMigrates -FromVersion '0.9.0' -ToVersion '1.0.0'
Assert-UpgradeAddsDefault -Property 'workstreamStaleDays' -Expected 14
Assert-UpgradePreservesProjectFile 'project/PROJECT.md'
Assert-InstallFails -Name 'newer schema refuses downgrade' -Mode Upgrade -SchemaVersion '2.0.0'
Assert-InstallPreviewMakesNoChanges -Mode Upgrade -SchemaVersion '0.9.0'
```

- [ ] **Step 4: Implement Upgrade**

For `0.9.0`, create a configuration backup, add missing adaptable arrays as empty arrays, add `workstreamStaleDays = 14`, and set `schemaVersion = '1.0.0'`. For `1.0.0`, plan no migration. For any other version, return a non-zero error explaining supported versions. Never write under `project/` in Upgrade mode.

- [ ] **Step 5: Run all installer tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/install-tests.ps1
```

Expected: all ExistingProject, Repair, NewProject, and Upgrade cases pass.

- [ ] **Step 6: Commit portable lifecycle support**

```powershell
git add .project-context/framework/scripts/install.ps1 .project-context/framework/scripts/tests/install-tests.ps1
git commit -m "feat: rebind and upgrade project context"
```

---

### Task 5: DiceRevolver Project Baseline and Workstreams

**Files:**
- Create: `AGENTS.md`
- Create: `.project-context/project/PROJECT.md`
- Create: `.project-context/project/STATUS.md`
- Create: `.project-context/project/DECISIONS.md`
- Create: `.project-context/project/ENVIRONMENT.md`
- Create: `.project-context/project/workstreams/2026-08-16-dice-face-build-system/STATE.md`
- Create: `.project-context/project/workstreams/2026-08-16-dice-face-build-system/HANDOFF.md`
- Create: `.project-context/project/workstreams/2026-08-17-portable-project-context-system/STATE.md`
- Create: `.project-context/project/workstreams/2026-08-17-portable-project-context-system/HANDOFF.md`

**Interfaces:**
- Produces: the real DiceRevolver baseline consumed by future Codex tasks.
- Consumes: current source, resources, tests, project settings, approved spec, this plan, and existing Superpowers records.

- [ ] **Step 1: Generate the managed root entry**

Run preview, inspect it, then apply:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/install.ps1 -Mode Repair
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/install.ps1 -Mode Repair -Apply
```

Expected: root `AGENTS.md` contains exactly one valid managed block.

- [ ] **Step 2: Write the project map**

Document Unity `6000.3.10f1`, URP `17.3.0`, Input System `1.18.0`, build scene, controls, runtime bootstrap, editor builders, test assembly, and the data flow:

```text
input -> TopDownPlayerController -> DiceRevolverGun
DiceChamber draw -> DiceFaceLoadout -> DiceFaceEntry
DiceFaceEntry -> projectile stats + fire/hit/fire-end effects
DiceBuildPageUI -> DiceFaceLoadout equipment changes
```

Use repository-relative links to concrete source files.

- [ ] **Step 3: Write status, decisions, and environment**

Status records the portable context workstream as active and dice-face build as completed history. Known gaps remain: missing explosion projectile Prefab, no complete enemy damage/pierce behavior, no build persistence, no full PlayMode verification, and current-device Git LFS clean-filter permission failure.

Decisions include ADR-001 for ScriptableObject dice entries and ADR-002 for portable framework/project separation. Environment documents clone, Unity discovery via `$UnityEditor`, test command patterns, Git LFS, and device-local differences without claiming Unity tests passed.

- [ ] **Step 4: Record completed dice-face history**

Set status `completed`, current work exactly `- 无`, and link:

```text
docs/superpowers/specs/2026-08-16-dice-face-build-system-design.md
docs/superpowers/plans/2026-08-16-dice-face-build-system.md
.superpowers/sdd/2026-08-16-dice-face-build-system/progress.md
```

Mark Unity verification `[not-run]` because this session has not rerun it.

- [ ] **Step 5: Record the active portable-system workstream**

Set status `active`, branch `main`, updated date `2026-08-17`, completed Tasks 1–4, current Task 5, and next Task 6. Record only checker and installer tests that actually passed.

- [ ] **Step 6: Run the repository checker**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
```

Expected: exit `0` with `[context:ok]`.

- [ ] **Step 7: Commit DiceRevolver context**

```powershell
git add AGENTS.md .project-context/project .project-context/project-context.json
git commit -m "docs: establish DiceRevolver project context"
```

---

### Task 6: Final Verification and Cold-Start Handoff

**Files:**
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/workstreams/2026-08-17-portable-project-context-system/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-17-portable-project-context-system/HANDOFF.md`

**Interfaces:**
- Consumes: complete portable package and DiceRevolver project instance.
- Produces: completed workstream and reproducible cross-device handoff.

- [ ] **Step 1: Run all framework tests**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/check-tests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/install-tests.ps1
```

Expected: both exit `0` and report every named case passed.

- [ ] **Step 2: Verify preview zero-write on the real repository**

Capture `git status --porcelain=v1` before and after:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/install.ps1 -Mode Repair
```

Expected: identical status output before and after preview.

- [ ] **Step 3: Run the real repository checker**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
```

Expected: exit `0`, no context errors, and no unexpected warnings.

- [ ] **Step 4: Perform the cold-start rehearsal**

Starting only from `AGENTS.md`, follow the protocol and produce a console-only summary that correctly identifies Unity version, build scene, dice data flow, completed dice-face history, active portable-system work, next action, known gaps, and exactly which tests ran. Do not save the rehearsal as another context file.

- [ ] **Step 5: Mark the portable-system workstream completed**

Set status `completed`, current work exactly `- 无`, record actual test results with legal labels, rewrite the handoff to explain the copy-and-install workflow, and move the workstream from active to completed history in `STATUS.md`.

- [ ] **Step 6: Run final checks after status changes**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
git diff --name-only 3ed1ec2 -- Assets Packages ProjectSettings
```

Expected: checker exit `0`; the Git diff command produces no output.

- [ ] **Step 7: Commit completion records**

```powershell
git add .project-context/project
git commit -m "docs: complete portable context system rollout"
```
