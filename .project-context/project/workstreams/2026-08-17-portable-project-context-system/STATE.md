# 可移植项目上下文系统

- ID: `2026-08-17-portable-project-context-system`
- Status: `completed`
- Branch: `codex/portable-context-system`
- Created: `2026-08-17`
- Updated: `2026-08-17`

## 目标

- 将 Codex 协议、项目资料、工作流、安装器、检查器和测试封装为可复制的 `.project-context/`。

## 非目标

- 不修改 Unity 运行时代码、资源、场景、Prefab、包或项目设置。
- 不自动推断新项目业务架构，也不保存聊天全文或私有推理。

## 已确认事实

- 根目录只需要安装器管理的薄 `AGENTS.md` 入口。
- 通用框架和项目实例资料已经分层。
- PowerShell 5.1 读取含中文脚本时需要 UTF-8 BOM；框架脚本已按此处理。

## 已完成

- Task 1：配置 schema、通用协议和六种模板。
- Task 2：结构检查器及 Unity、Web、Library 三类夹具。
- Task 3：预览、ExistingProject、Repair、幂等和安全入口合并。
- Task 4：NewProject 备份/重新绑定，以及 `0.9.0` 到 `1.0.0` 升级。
- Task 5：建立 DiceRevolver 项目地图、环境资料、状态索引和历史工作流。
- Task 6：完成全量框架回归、真实仓库零写入预览、默认根目录调用和冷启动演练。

## 当前正在进行

- 无

## 下一步

1. 后续事项创建新的工作流目录，不恢复本已完成工作流。
2. 复制到其他项目时先运行 NewProject 预览，再显式应用并补全新项目基线。

## 阻塞

- 无。工作流实施时未运行 Unity 测试；后续项目同步已确认当前设备存在匹配版本的 Unity Editor。

## 涉及文件

- `AGENTS.md`
- `.project-context/project-context.json`
- `.project-context/framework/`
- `.project-context/project/`
- `docs/superpowers/specs/2026-08-17-project-context-system-design.md`
- `docs/superpowers/plans/2026-08-17-portable-project-context-system.md`

## 验证记录

- [passed] `2026-08-17`：检查器 10 项验收用例通过，包括无 `-Root` 调用。
- [passed] `2026-08-17`：安装器 13 项 ExistingProject、Repair、NewProject 和 Upgrade 生命周期验收用例通过。
- [passed] `2026-08-17`：真实仓库 `ExistingProject -Apply` 后的结构检查通过。
- [passed] `2026-08-17`：真实仓库 `Repair` 预览返回 `no changes`，路径限定 Git 状态执行前后一致。
- [passed] `2026-08-17`：仅凭项目内入口、协议、项目资料和工作流完成冷启动恢复。
- [not-run] `2026-08-17`：本工作流实施阶段没有运行 Unity 测试，且没有修改 Unity 内容。
- [passed] `2026-08-17`：后续项目同步确认 Unity `6000.3.10f1` 可用，游戏侧隔离 EditMode 回归 `29/29` 通过。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-17-project-context-system-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-17-portable-project-context-system.md)
