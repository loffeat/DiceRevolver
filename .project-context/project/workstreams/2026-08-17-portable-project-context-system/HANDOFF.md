# 可移植项目上下文系统交接

## 当前状态

- 框架、检查器、四种安装模式、DiceRevolver 基线和最终回归均已完成。

## 当前方案及原因

- `.project-context/framework/` 与 `.project-context/project/` 分层，复制后通过实例 UUID 和根入口判断是否需要重新绑定。
- 安装器默认预览，只有 `-Apply` 写入，旧项目资料在 NewProject 模式中先备份。

## 尚未完成

- 无。本工作流已完成；新的产品或框架变更应创建新工作流。

## 下一步首个动作

- 若要移植到其他仓库，复制整个 `.project-context/` 文件夹后，在目标项目根目录先运行预览：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/install.ps1 -Mode NewProject
```

- 核对计划无误后显式应用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/install.ps1 -Mode NewProject -Apply
```

- 随后让 Codex 扫描目标项目并填充 `.project-context/project/` 中的新项目基线，再运行检查器。

## 首先读取

- [工作流状态](STATE.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-17-portable-project-context-system.md)
- [框架协议](../../../framework/PROTOCOL.md)

## 必须保护

- 不修改 `Assets/`、`Packages/` 或 `ProjectSettings/`。
- 不覆盖根 `AGENTS.md` 托管标记之外的人工内容。
- 不在框架升级中覆盖 `.project-context/project/`。

## 最近验证

- [passed] `2026-08-17`：检查器 10 项验收测试通过。
- [passed] `2026-08-17`：安装器 13 项生命周期测试通过。
- [passed] `2026-08-17`：真实仓库默认根目录检查、Repair 零写入预览和冷启动恢复通过。
- [not-run] `2026-08-17`：本工作流实施阶段没有运行 Unity 测试；本工作流没有更改 Unity 受保护路径。
- [passed] `2026-08-17`：后续项目同步确认匹配版本 Unity 可用，游戏侧隔离 EditMode 回归 `29/29` 通过。

## 风险与不可假定事项

- 全仓库 Git 检查可能被既有 Git LFS 权限问题打断。
- 完整 PlayMode 战斗流程仍未运行，不能仅凭 EditMode 结果声称最终场景表现已人工确认。
