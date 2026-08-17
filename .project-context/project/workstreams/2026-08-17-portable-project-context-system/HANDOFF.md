# 可移植项目上下文系统交接

## 当前状态

- 框架、检查器和四种安装模式已实现；正在建立 DiceRevolver 真实基线，尚未完成最终回归。

## 当前方案及原因

- `.project-context/framework/` 与 `.project-context/project/` 分层，复制后通过实例 UUID 和根入口判断是否需要重新绑定。
- 安装器默认预览，只有 `-Apply` 写入，旧项目资料在 NewProject 模式中先备份。

## 尚未完成

- 最终运行检查器和安装器全量测试。
- 验证无 `-Root` 调用、真实仓库预览零写入和冷启动恢复。
- 将本工作流标记为 `completed`。

## 下一步首个动作

- 运行 `.project-context/framework/scripts/check.ps1`，修复任何真实仓库链接或结构错误。

## 首先读取

- [工作流状态](STATE.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-17-portable-project-context-system.md)
- [框架协议](../../../framework/PROTOCOL.md)

## 必须保护

- 不修改 `Assets/`、`Packages/` 或 `ProjectSettings/`。
- 不覆盖根 `AGENTS.md` 托管标记之外的人工内容。
- 不在框架升级中覆盖 `.project-context/project/`。

## 最近验证

- [passed] `2026-08-17`：安装器 12 项生命周期测试通过。

## 风险与不可假定事项

- 全仓库 Git 检查可能被既有 Git LFS 权限问题打断。
- Unity 测试尚未运行，不能声称游戏行为已在本次实施中回归。
