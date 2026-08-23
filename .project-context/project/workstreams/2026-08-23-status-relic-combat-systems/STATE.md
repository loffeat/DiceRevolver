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
- 规格经用户评审批准；实施计划已写入 `docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md` 并提交（`b613c70`）。
- 用户选择内联执行；**T1–T8 代码与测试已实现**：
  - T1 `EnemyHealth`（有限生命+死亡）+ `TargetDummy` 有限血化
  - T2 `EnemyStatusDefinition`/`EnemyStatusHost`（DoT/叠加/到期/全局状态事件）
  - T3 `ApplyEnemyStatusResultModule`/`HasEnemyStatusConditionModule` + 点燃资产；`EventSignal.StatusTarget` 与 `EnemyStatusApplied` 信号位提前加入
  - T4 遗物框架（`RelicDefinition`/`RelicRuntime`/`LoadedFirstFaceRelicDefinition`）+ `SetFirstDrawForce` + `TryRefillAndForceNextFace` 被动面守卫
  - T5 抽牌评估覆盖所有面基础槽规则
  - T6 收尾者重构（DrawCandidate|Base 双触发 + 穿甲弹生成）+ `ArmorPiercingBullet.asset`
  - T7 `RoundProjectileStatistic` + `ScaleActivationDamageFromStatisticResultModule` + `DiceFaceActivation.DamageMultiplier` + 特斯拉改开火时语义（Tesla 词条转 OnFire）
  - T8 `DiceFaceAdjacency` + `TriggerAdjacentFacesResultModule` + 呼应协同重构（EnemyStatusApplied 触发 + 点燃条件 + 相邻触发）
- **编译门禁已验证**：MSBuild 2022 编译 Prototype/Editor/EditMode.Tests 三程序集全部 exit 0（期间修复：csproj 新文件登记、4 个测试假服务补 `RoundProjectileStatistic` 属性、模块辅助方法名等）。
- 资产终态核验：Tesla 词条 OnFire/非被动、Echo/Finisher 被动基础；TeslaRule allowedSlots=OnFire、Echo/FinisherRule=Base；`DiceFaceSlotType.Passive`/`DiceFaceSlotMask.Passive` 代码零引用；受保护 Prefab 未触碰。
- **规则资产已更新到新版本（直接资产编辑，等价于构建器结果）**：`TeslaRule.asset`（触发器=开火时，结果=`ScaleActivationDamageFromStatisticResultModule` 按雷电球统计增伤）；`FinisherRule.asset`（触发器=DrawCandidate|Base 双触发，结果1=SetDrawPriority+SignalType(DrawCandidate)+SourceFace，结果2=SpawnProjectile 穿甲弹+SignalType(Base)）；`EchoSynergyRule.asset`（触发器=EnemyStatusApplied，规则条件=`HasEnemyStatusConditionModule` 点燃，结果=`TriggerAdjacentFacesResultModule` 触发相邻面）。为 `ScaleActivationDamageFromStatisticResultModule`/`TriggerAdjacentFacesResultModule`/`ArmorPiercingBullet`/`Ignite` 等创建固定 GUID meta；构建器的 `Has*Structure` 校验与手写结构一致（幂等 no-op）。
- **新增"燃烧子弹"事件**（用户需求）：`BurningBulletRule.asset`（命中时触发 → `ApplyEnemyStatusResultModule` 施加点燃）+ `BurningBullet.asset`（命中时槽位词条）+ 登记进 `DiceFaceLibrary`。
- **构筑页同步与清除能力（用户反馈）**：新增 `DiceFaceConfiguration.ClearSlot`/`DiceFaceLoadout.ClearFace` + 骰面卡片"清空"按钮；`DiceFaceEntry.DisplayName/Description/DisplayColor` 优先解析绑定的规则（编辑器改动即构筑页所见）；构筑页每次打开重建；词条按钮背景随规则颜色着色。
- **弹丸专属 2D 立绘（用户需求）**：`ProjectileDefinition` 新增可选 `ProjectileSprite`（弹丸立绘，正式美术挂载点）；新增 `ProjectileSpriteVisual`（运行时挂 SpriteRenderer，俯视朝向 Euler(90,0,0) 与角色一致，默认隐藏原粒子视觉）+ `ProjectileSpriteFactory`（程序化占位贴图：基础=灰色长方形、雷电球=蓝色圆形、穿甲弹=黑色细长方形，按定义名缓存）；`ProjectileVisualWrapper` 暴露 `VisualInstance`；Gun 生成弹丸时挂载。不改受保护 Prefab。

## 当前正在进行

- 等待 EditMode 测试执行（Unity 测试受打开的编辑器 UPM 单实例限制；需用户 Test Runner 或关闭编辑器后批处理；含 `LightningBuildPrototypeBuilder.Build()` 运行以重构 Finisher/Tesla/Echo 规则资产）。

## 下一步

1. 用户在 Unity 刷新并运行聚焦测试（EnemyHealthTests、EnemyStatusHostTests、EnemyStatusModuleTests、RelicTests、DiceRevolverRuntimeTests、EventRulePassiveIntegrationTests、EventRuleLightningMigrationTests、LightningBuildAssetTests、RoundProjectileStatisticTests 等）；运行 `Dice Revolver/Build Lightning Prototype Content` 菜单完成规则资产重构。
2. 全量回归 + 受保护 SHA256 复核。
3. 提交（注意多工作流文件提交拆分）。

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
