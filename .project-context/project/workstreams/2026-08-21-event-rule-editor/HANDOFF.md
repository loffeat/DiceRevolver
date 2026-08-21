# 事件配置页面交接

## 当前状态

- `active`，Task 1–9 主实现已完成；Task 10 已安全删除 Core 三个无引用具体 Effect。Task 9 staged fix、Task 10 Unity 验证及雷电五 Effect 最终清理仍未完成。

## 下一步首个动作

- 平台恢复后先提交当前 staged Task 9 fix，运行 `EventRuleLightningMigrationTests` custom-value focused 与必要联合回归；通过后提交 Task 10 清理并运行 focused/full EditMode。

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

- Task 10 的雷电五个具体 Effect 删除、focused/full EditMode 与最终提交尚未完成；staged builder/tests 和生产 `SelectTargets` helper 仍是删除门禁。
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

- [passed] Task 10 Core 三类型与六 GUID 在 `Assets` 零引用；已删除其 scripts/assets/.meta，`ProjectileSpawnEffect` 与三份兼容资产保留。
- [passed] 八个保护 SHA256 与既有基线一致；Player/TestRobot 枪械调参仍为 `2/2/32`。
- [not-run] Task 10 tests、focused/full EditMode 与人工 Editor/战斗验收（platform usage limit）。
- [not-run] Task 9 fix round 1 custom-value RED、核心 GREEN 与联合回归：平台 usage limit；不得把下列修复前结果当作本轮通过证据。
- [passed] Task 9 核心迁移 EditMode `10/10`、`0 skipped`。
- [passed] Task 9 指定十套联合 EditMode `41/41`、`0 skipped`。
- [passed] Task 9 前后八个受保护文件 SHA256 与 Player/TestRobot 枪械参数完全一致。
- [passed] Task 9 fix round 1 静态 diff、八个保护哈希与 Player/TestRobot 枪械参数保持不变。
- [failed] 完整 EditMode `251/252`，唯一失败为已知 Ground `Y=-0.01` 契约差异。

## Task 10 保留门禁

- 保留 `ElectromagneticResonanceEffect`、`TeslaPassiveEffect`、`EchoSynergyPassiveEffect`、`ChainReactionOnFireEndEffect`、`FinisherPassiveEffect`，直到 Task 9 staged 引用迁移且类型/GUID 再扫描为零。
- 保留抽象 `BulletEventEffect`、`PassiveEventEffect`、hidden legacy 字段/read fallback，以及 `ProjectileSpawnEffect` 与 FireBasic/FireTestRobot/FireLightningOrb 三资产。

## 首先读取

- [工作流状态](STATE.md)
- [规则列表设计稿](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-event-rule-editor.md)
- [战斗 Debug 工作流](../2026-08-21-combat-debug-trace/HANDOFF.md)
- [项目状态](../../STATUS.md)
