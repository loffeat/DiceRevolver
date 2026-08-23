# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统

- ID: `2026-08-23-status-relic-combat-systems`
- Status: `planned`
- Branch: `main`
- Created: `2026-08-23`
- Updated: `2026-08-23`

## 目标

- 按已确认设计（`docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`）实现五组能力：负面效果框架（点燃 DoT + 敌人有限生命/死亡）、遗物框架（出千）、收尾者重做（基础事件+穿甲弹+抽牌评估扩展）、特斯拉开火时增伤、呼应协同相邻触发。

## 非目标

- 不改受保护 Prefab；不做遗物 UI/商店；不实现其他负面效果；不做"部分激活"模式。

## 已确认事实

- 敌人模型极简（`IDamageReceiver` + 无限血 `TargetDummy`），无状态/HP/死亡。
- `RequestBonusActivationResultModule` 硬编码触发装备面，无目标面参数。
- 收尾者/特斯拉/呼应协同在被动物面语义下均因 SourceFace/SameProjectileType 条件失效（迁移隐藏后果，本次随重做解决）。
- `TryRefillAndForceNextFace` 不检查被动面（可把被动面拉回抽池，需修复）。
- 构筑 UI 布局 `FacePositions`：面 3 的 8 向相邻 = {1,2,4,6}。

## 已完成

- brainstorming 澄清 6 问 + 设计 5 节全部经用户确认；规格已写入 `docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md` 并提交（`a16736b`）。
- 规格经用户评审批准。
- 实施计划已写入 `docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md`（9 个任务，writing-plans 自审通过）。

## 当前正在进行

- 等待用户选择执行方式（子代理驱动 / 内联执行）。

## 下一步

1. 用户评审规格；按反馈修订。
2. 调用 `writing-plans` 产出实施计划。
3. 实现与测试。

## 阻塞

- 无（等待用户规格评审）。

## 涉及文件

- `docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`
- 新：`EnemyHealth`、`EnemyStatusHost`、`EnemyStatusDefinition`、`ApplyEnemyStatusResultModule`、`HasEnemyStatusConditionModule`、`RelicDefinition`、`RelicRuntime`、`LoadedFirstFaceRelicDefinition`、`RoundProjectileStatistic`、`TriggerAdjacentFacesResultModule`、`DiceFaceAdjacency`、穿甲弹 `ProjectileDefinition`、点燃/出千/新规则资产
- 改：`DiceRevolverRuntime`（SetFirstDrawForce + 被动面守卫）、`DiceEventRuleRuntimeSet`（抽牌评估覆盖所有面）、`EventRuleTypes`（EnemyStatusApplied 信号）、`DiceFaceConfiguration`（如有）、`DiceRevolverGun`（统计/遗物接线）、`TargetDummy`（有限血）
- `Assets/Tests/EditMode/`

## 验证记录

- [not-run] `2026-08-23`：本工作流尚无实现验证。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
