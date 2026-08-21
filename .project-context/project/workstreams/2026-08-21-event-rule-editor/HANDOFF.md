# 事件配置页面交接

## 当前状态

- `active`，Task 1–9 已完成；核心与雷电构筑均已迁移到真实 Event Rule 资产，待 Task 10 做旧具体 Effect 的零引用审计、条件删除和最终回归。

## 下一步首个动作

- 读取 `.superpowers/sdd/2026-08-21-event-rule-editor/progress.md` 与 Task 10 brief，然后先写引用/删除契约 RED；只有真实 AssetDatabase/GUID 扫描为零引用时才删除旧具体 Effect。

## 当前方案及原因

- `EventRuleDefinition` 主资产包含一个 Trigger、规则级 Conditions 和有序 ResultEntries。
- 每个 ResultEntry 包含自己的局部 Conditions 和一个 Result 模块。
- 模块保存为规则资产的 SubAsset，并由 `TypeCache` 自动发现。
- 每把 Gun、每个装备面创建独立 Runtime；SO 资产不保存战斗期可变状态。
- 执行行为通过受限服务接口连接弹丸、弹夹、时间系统、闪电链和 Debug，不直接依赖角色或场景单例。
- 第一阶段保留旧 Effect 兼容边界，避免一次性迁移破坏当前可玩版本。

## 实施顺序

1. 规则接口、Signal、Context、预算与异常隔离。
2. 三栏 EditorWindow、SubAsset 增删排序、Undo/Redo 和校验器。
3. 旧 Effect 适配器。
4. 基础射击、DoubleTap、BlastRound、LoadedFour 迁移。
5. 雷电构筑和带状态被动迁移。

## 尚未完成

- Task 10 的旧具体 Effect 引用审计、条件删除、最终聚焦/全量回归与最终上下文收尾尚未完成。
- 实施前完整 EditMode 基线为 `[failed] 251/252`、`0 skipped`；唯一失败为已批准的 Ground `Y=-0.01` 契约差异。
- 尚未运行可见 Play Mode 验收。

## 必须保护

- 不覆盖用户在 Player、TestRobot、TargetDummy、AimRoot、Renderer 和 DiceRevolverGun Inspector 中填写的数据。
- 不运行 `TopDownPrototypeSceneBuilder`。
- 不自动修改 sorting layer、Transform、枪械射速、换弹速度、弹丸速度或美术尺寸。

## 风险与不可假定事项

- 不得把 ScriptableObject 当作运行时状态容器，否则多把枪和多个骰面会互相污染状态。
- 不得让规则模块直接依赖 Player、场景单例或任意反射调用。
- 旧 Effect 与新 Rule 并存期间，同一槽位只能执行其中一种，避免重复触发。
- 编辑器校验只能提示或阻止装备，不得擅自修正和覆盖用户填写的数据。

## 最近验证

- [passed] Task 9 核心迁移 EditMode `10/10`、`0 skipped`。
- [passed] Task 9 指定十套联合 EditMode `41/41`、`0 skipped`。
- [passed] Task 9 前后八个受保护文件 SHA256 与 Player/TestRobot 枪械参数完全一致。
- [failed] 完整 EditMode `251/252`，唯一失败为已知 Ground `Y=-0.01` 契约差异。

## 首先读取

- [工作流状态](STATE.md)
- [规则列表设计稿](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-event-rule-editor.md)
- [战斗 Debug 工作流](../2026-08-21-combat-debug-trace/HANDOFF.md)
- [项目状态](../../STATUS.md)
