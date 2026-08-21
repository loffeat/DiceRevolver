# 事件配置页面交接

## 当前状态

- `planned`，需求、正式设计稿、页面视觉方向和测试先行实施计划均已完成，尚未开始代码实施。

## 下一步首个动作

- 读取[实施计划](../../../../docs/superpowers/plans/2026-08-21-event-rule-editor.md)，确认采用 Subagent-Driven 或 Inline Execution；随后从纯 C# 规则数据模型和执行器开始测试驱动实现。

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

- 尚未创建规则运行时、EditorWindow、兼容适配器或迁移资源。
- 尚未运行本工作流的 Unity 测试和可见 Play Mode 验收。

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

- [passed] 战斗 Debug 与相关事件管线聚焦 EditMode `107/107`。
- [passed] 特斯拉、收尾者与战斗 Debug 联合 EditMode `20/20`。
- [failed] 完整 EditMode `251/252`，唯一失败为已知 Ground `Y=-0.01` 契约差异。

## 首先读取

- [工作流状态](STATE.md)
- [规则列表设计稿](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-event-rule-editor.md)
- [战斗 Debug 工作流](../2026-08-21-combat-debug-trace/HANDOFF.md)
- [项目状态](../../STATUS.md)
