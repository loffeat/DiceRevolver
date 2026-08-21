# 事件配置页面交接

## 当前状态

- `completed`（自动化）：Task 1–10 全部完成并验证；代码已合并回 `main`。
- 剩余工作仅为人工可见验收（三栏编辑器页面与 Play Mode 战斗表现），记录为 `[not-run]`。

## 当前方案及原因

- `EventRuleDefinition` 主资产包含一个 Trigger、规则级 Conditions 和有序 ResultEntries，模块保存为 SubAsset，由 `TypeCache` 自动发现。
- 每把 Gun、每个装备面创建独立 Runtime；SO 资产不保存战斗期可变状态；模块只能通过受限服务接口请求行为，避免直接依赖角色或场景单例。
- 迁移阶段保留 `BulletEventEffect`/`PassiveEventEffect` 回退与 `ProjectileSpawnEffect` 兼容边界，同一槽位只执行 Rule 或 legacy 其中一种。
- 雷电规则构建器收敛为"只创建缺失资产、保留既有非空参数"，不再依赖已删除的具体 Effect。

## 尚未完成

- 三栏编辑器页面排版/交互与 Play Mode 战斗表现的人工可见验收（`[not-run]`）。
- 完整 PlayMode 战斗流程验证（`[not-run]`）。

## 下一步首个动作

- 在可见 Unity 中打开 `Window > Dice Revolver > 事件规则编辑器`，按 STATE.md 的人工验收清单逐项验收；若发现缺陷，在 `main` 上以新工作流修复。

## 首先读取

- [工作流状态](STATE.md)
- [规则列表设计稿](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-event-rule-editor.md)
- [项目状态](../../STATUS.md)

## 必须保护

- 不覆盖用户在 Player、TestRobot、TargetDummy、AimRoot、Renderer 和 DiceRevolverGun Inspector 中填写的数据。
- 不运行 `TopDownPrototypeSceneBuilder`。
- 不自动修改 sorting layer、Transform、枪械射速、换弹速度、弹丸速度或美术尺寸。
- 保留抽象 `BulletEventEffect`、`PassiveEventEffect`、hidden legacy 字段/read fallback，以及 `ProjectileSpawnEffect` 与 FireBasic/FireTestRobot/FireLightningOrb 三资产。

## 最近验证

- [failed] `2026-08-21`：清理后完整 EditMode `351/350`，唯一失败为 Ground `Y=-0.01` 豁免项。
- [passed] `2026-08-21`：Task 9 fix 聚焦 `12/12`、联合 `43/43`；GUID/类型零引用；十个保护哈希一致。
- [not-run] `2026-08-21`：人工可见验收。

## 风险与不可假定事项

- 不得把 ScriptableObject 当作运行时状态容器，否则多把枪和多个骰面会互相污染状态。
- 不得让规则模块直接依赖 Player、场景单例或任意反射调用。
- 编辑器校验只能提示或阻止装备，不得擅自修正和覆盖用户填写的数据。
- 完整 PlayMode 战斗流程仍未运行；不得把 EditMode 回归当作人工可见验收。
