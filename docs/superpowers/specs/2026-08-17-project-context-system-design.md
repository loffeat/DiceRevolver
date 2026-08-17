# 可移植项目上下文与跨设备交接系统设计

## 目标

为 DiceRevolver 建立一套仓库内、受 Git 管理且可复制到其他项目的上下文系统。即使 Codex 位于另一台设备、缺失大段聊天历史或接手不同分支，也能从项目根目录的入口迅速确认：

- 项目目标、运行方式和核心模块；
- 活跃事项、完成进度和下一步；
- 重要决定及其原因；
- 已验证、失败、未运行和受阻的结果；
- 必须保护的文件、资源和参数。

除根目录的薄 `AGENTS.md` 入口外，系统的框架、配置、项目资料、工作流、安装器、检查器和测试全部封装在 `.project-context/`。复制该文件夹并运行一次安装命令即可适配新项目。

## 核心规则

1. 每个 Codex 任务结束前必须复核上下文；如果纯只读任务没有发现状态变化，允许零文件改动，但不得跳过复核。
2. 上下文文件受 Git 管理，并随正常实现提交；Codex 不得仅为例行上下文更新擅自制造提交。
3. 系统必须支持多设备、多分支和多个工作流并发。
4. 代码、Git 状态和实际验证结果优先于可能过期的记录。
5. 禁止记录密钥、令牌、个人信息、私有推理、聊天全文或设备临时路径。
6. 框架升级不得静默覆盖项目资料。
7. 安装和迁移默认只预览；只有显式传入 `-Apply` 才能写入。

## 方案选择

采用“单目录双层结构”：`.project-context/framework/` 保存可复用框架，`.project-context/project/` 保存项目实例资料。

整包无边界复制会把来源项目的状态误认为目标项目事实；依赖设备级 Codex Skill 又会削弱仓库自包含能力。双层结构让同一文件夹既可以随项目同步，也可以复制到新项目并安全重新绑定。

## 目录结构

```text
AGENTS.md
.project-context/
  project-context.json
  backups/
  framework/
    PROTOCOL.md
    schema.json
    templates/
      PROJECT.md
      STATUS.md
      DECISIONS.md
      ENVIRONMENT.md
      WORKSTREAM_STATE.md
      WORKSTREAM_HANDOFF.md
    scripts/
      install.ps1
      check.ps1
      tests/
        install-tests.ps1
        check-tests.ps1
        fixtures/
  project/
    PROJECT.md
    STATUS.md
    DECISIONS.md
    ENVIRONMENT.md
    workstreams/
      YYYY-MM-DD-topic-slug/
        STATE.md
        HANDOFF.md
docs/
  superpowers/
    specs/
    plans/
.superpowers/
  sdd/
```

`docs/superpowers` 和 `.superpowers/sdd` 保留为项目历史证据。工作流可以链接它们，但不复制其全文。

## 根目录入口

根目录 `AGENTS.md` 是安装器管理的薄入口。它只负责：

- 指向 `.project-context/framework/PROTOCOL.md`；
- 要求 Codex在修改项目前执行协议读取顺序；
- 给出检查命令。

如果根目录不存在 `AGENTS.md`，安装器创建它。如果文件已存在，安装器只在 `<!-- project-context:begin -->` 与 `<!-- project-context:end -->` 之间插入或更新托管区块；缺少成对标记、标记重复或顺序非法时停止并报告，不覆盖人工内容。

## 可复用框架

### `framework/PROTOCOL.md`

保存所有项目通用的 Codex 启动、执行、强制结束、事实优先级、安全、并发和交接规则。协议不得包含 DiceRevolver 专属事实。

### `framework/schema.json`

定义 `project-context.json` 的格式、合法枚举和必填字段。它同时作为安装器和检查器的版本契约。

### `framework/templates/`

保存共享上下文及工作流模板。模板使用 `{{FIELD_NAME}}` 占位符；占位符只允许存在于模板中，不得残留在实际项目资料中。

### `framework/scripts/install.ps1`

提供安装、重新绑定、升级和修复能力。接口为：

```powershell
install.ps1 -Mode <NewProject|ExistingProject|Upgrade|Repair> [-Apply]
```

不带 `-Apply` 时只输出操作计划，不写文件。

### `framework/scripts/check.ps1`

执行只读结构检查。接口为：

```powershell
check.ps1 [-Root <repository-root>] [-StaleAfterDays <positive-int>]
```

发现错误返回退出码 `1`；只有错误为零时返回 `0`。警告不改变退出码。

## 项目实例配置

`.project-context/project-context.json` 至少包含：

- `schemaVersion`：框架配置格式版本，首版固定为 `1.0.0`；
- `instanceId`：当前项目实例的 UUID；
- `projectName`：项目名称；
- `projectType`：`unity`、`web`、`library`、`service` 或 `mixed`；
- `sourceRoots`：主要源代码目录；
- `entryFiles`：入口、清单或构建配置；
- `testCommands`：允许执行的项目验证命令；
- `protectedPaths`：上下文维护不得擅自覆盖的路径；
- `additionalContextFiles`：项目特有必读文件；
- `ignorePatterns`：扫描和链接检查忽略的生成物或第三方目录；
- `workstreamStaleDays`：陈旧工作流阈值，默认 14 天。

配置不得保存密钥或设备绝对路径。不同项目类型只通过配置和项目资料适配，不分叉出多套框架。

## 项目资料

### `project/PROJECT.md`

保存低频变化的项目地图：目标、玩法或业务循环、技术栈、入口、目录、核心模块、关键数据流、明确非目标和术语。

### `project/STATUS.md`

保存项目阶段、全局有效状态、活跃工作流索引、完成历史索引、主要缺口和最近项目级验证。它不复制工作流的详细进度。

### `project/DECISIONS.md`

追加式架构决策日志。每项决策包含日期、状态、背景、决定、原因、影响、替代方案和相关资料。旧决策被取代时追加说明，不静默改写历史。

### `project/ENVIRONMENT.md`

记录硬性要求、推荐配置、首次克隆、运行、测试、构建、Git LFS、设备本地差异和已知环境问题。绝对路径只能作为明确的本地示例变量，不能成为跨设备事实。

## 工作流资料

每个独立事项使用 `.project-context/project/workstreams/YYYY-MM-DD-topic-slug/`。合法状态为 `planned`、`active`、`blocked`、`completed` 和 `superseded`。

`STATE.md` 固定包含：

- ID、标题、状态、分支、创建日期和更新时间；
- 目标、非目标和已确认事实；
- 已完成、当前进行和下一步；
- 阻塞条件及解除方式；
- 涉及文件；
- 验证记录；
- 相关规格、计划、决策和历史执行记录。

每条验证使用 `passed`、`failed`、`not-run` 或 `blocked`。`not-run` 表示未尝试，`blocked` 表示尝试执行但被明确条件阻止。

`HANDOFF.md` 是一分钟摘要，固定包含：当前状态、方案原因、尚未完成、下一步首个动作、首先读取、必须保护、最近验证和不可假定事项。

## Codex 生命周期

### 启动

1. 读取根目录 `AGENTS.md`。
2. 读取 `framework/PROTOCOL.md`。
3. 读取 `project-context.json`。
4. 读取 `project/PROJECT.md`、`STATUS.md` 和 `ENVIRONMENT.md`。
5. 根据用户请求、当前分支和状态索引定位工作流。
6. 读取工作流的 `STATE.md`、`HANDOFF.md` 及其链接资料。
7. 用 Git、代码和实际验证核对记录。

在完成必要读取前不得修改项目。

### 执行期间

- 新事项创建独立工作流目录。
- 需求、范围、关键假设或架构决定变化时更新对应状态；影响全局架构时追加决策日志。
- 多分支通常只编辑自己的工作流。
- 只有稳定项目事实改变时才更新共享项目资料。
- 不记录聊天全文、私有推理和无价值命令输出。

### 结束前

1. 更新工作流 `STATE.md` 的真实状态。
2. 重写 `HANDOFF.md`，明确下一位 Codex 的首个动作。
3. 记录涉及文件、验证命令、实际结果和日期。
4. 同步受影响的共享项目资料。
5. 检查秘密、临时路径、失效占位符和未经验证的完成声明。
6. 运行 `.project-context/framework/scripts/check.ps1`。
7. 仅随正常任务提交上下文更新。

检查失败时不得声称任务完成；可以记录为阻塞并交接。

## 安装与重新绑定

### `NewProject`

用于把已经初始化过的 `.project-context/` 复制到另一个项目：

1. 读取来源实例 ID 和项目资料。
2. 预览将要备份、创建和修改的文件。
3. `-Apply` 后将旧 `project/` 和旧配置快照保存到 `.project-context/backups/<source-instance-id>/<UTC-timestamp>/`。
4. 生成新的实例 ID。
5. 从模板生成结构合法但等待 Codex 填充的新 `project/`。
6. 创建或安全合并根目录 `AGENTS.md`。
7. 运行结构检查。
8. 输出让 Codex 扫描目标仓库并填写基线的下一步指令。

旧项目资料不得静默删除。备份目标必须解析为 `.project-context/` 内的明确目录。

### `ExistingProject`

用于首次向已有项目安装尚未初始化的框架。它生成实例配置、项目资料和根入口，但不备份不存在的来源资料。

### `Upgrade`

只升级 `framework/` 和配置格式。项目资料迁移前必须预览并备份。配置版本高于当前安装器时停止，禁止降级写入。

### `Repair`

验证框架和根入口，补建缺失的非项目资料文件；不得覆盖已有项目内容。

## 幂等与安全

- 预览模式必须零写入。
- 相同参数重复执行 `-Apply` 必须保持结果稳定。
- 所有移动和覆盖目标先解析为绝对路径，并验证位于当前仓库或 `.project-context/` 预期范围。
- 现有人工 `AGENTS.md` 不能安全合并时停止。
- 任何写入失败都应返回非零退出码，并报告已完成和未完成操作。
- 安装器不分析业务代码，也不自动声称项目基线完成。

## 检查器

检查器验证：

- 根入口、配置、协议、schema、模板和项目资料存在；
- 配置符合 schema 版本、枚举和必填字段；
- 项目资料及工作流包含固定章节；
- 工作流目录名、状态和验证标签合法；
- 本地 Markdown 链接可解析；
- 实际资料没有 `TBD`、未解释的 `TODO` 或模板占位符；
- 完成工作流的当前进行项为“无”；
- `planned`、`active` 或 `blocked` 超过配置阈值未更新时发出警告。

检查器不推断代码语义，也不把文档声明当成实际测试结果。

## DiceRevolver 初始化基线

首次实施时建立以下真实记录：

- Unity `6000.3.10f1`，URP `17.3.0`，Input System `1.18.0`。
- 主场景是 `Assets/Scenes/TopDownShooterPrototype.unity`。
- 核心玩法包括顶视角移动、鼠标瞄准、骰面随机弹巢、骰面构筑和弹丸事件。
- 既有资料位于 `docs/superpowers` 和 `.superpowers/sdd`。
- 首个历史工作流记录已完成的骰面构筑实施。
- 已知缺口包括爆炸弹丸 Prefab 未配置、敌人伤害与穿透未完整实现、构筑无持久化、缺少完整 PlayMode 验证，以及当前设备上的 Git LFS 临时目录权限问题。
- 未在本次执行中运行的 Unity 测试不得标记为通过。

## 验证策略

自动测试覆盖：

1. 完整实例检查成功。
2. 缺失章节、非法状态、非法配置、断链和占位符检查失败。
3. `NewProject`、`ExistingProject`、`Upgrade` 和 `Repair` 四种模式。
4. 预览零写入和 `-Apply` 幂等。
5. 现有 `AGENTS.md` 的安全合并与冲突停止。
6. 旧项目资料先备份再初始化。
7. 配置版本升级和高版本拒绝降级。
8. Unity、Web 和通用库三类夹具使用同一框架完成适配。

实施完成后执行一次 DiceRevolver 冷启动演练：假设没有聊天历史，仅按根入口读取，能够准确复述项目目标、核心架构、工作流、下一步、验证结果和主要风险。

## 成功标准

- 除薄根入口外，系统全部位于 `.project-context/`。
- 复制文件夹并运行一次安装命令即可安全适配新项目。
- 框架和项目资料能够独立升级与维护。
- 新设备 Codex 无需聊天历史即可恢复有效上下文。
- 并发工作流不会共同编辑详细进度文件。
- 每次任务结束都有真实、简洁、可执行的交接信息。
- 已验证、失败、未运行和受阻不会混淆。
- 安装和升级不会静默丢失来源或目标项目资料。
