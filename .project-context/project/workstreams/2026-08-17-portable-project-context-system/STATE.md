# 可移植项目上下文系统

- ID: `2026-08-17-portable-project-context-system`
- Status: `active`
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

## 当前正在进行

- Task 5：写入 DiceRevolver 项目基线和工作流记录。

## 下一步

1. 完成真实仓库结构检查。
2. 执行 Task 6 的全量框架测试、预览零写入和冷启动演练。
3. 将本工作流标记完成并更新交接。

## 阻塞

- Unity 测试受当前设备缺少已知 Unity Editor 路径限制，但不阻塞纯文档与 PowerShell 系统验证。

## 涉及文件

- `AGENTS.md`
- `.project-context/project-context.json`
- `.project-context/framework/`
- `.project-context/project/`
- `docs/superpowers/specs/2026-08-17-project-context-system-design.md`
- `docs/superpowers/plans/2026-08-17-portable-project-context-system.md`

## 验证记录

- [passed] `2026-08-17`：检查器九项验收用例通过。
- [passed] `2026-08-17`：安装器 ExistingProject、Repair、NewProject 和 Upgrade 验收用例通过。
- [passed] `2026-08-17`：真实仓库 `ExistingProject -Apply` 后的结构检查通过。
- [not-run] `2026-08-17`：Task 6 最终全量回归和冷启动演练尚未运行。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-17-project-context-system-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-17-portable-project-context-system.md)
