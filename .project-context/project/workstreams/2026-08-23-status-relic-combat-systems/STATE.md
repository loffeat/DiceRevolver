# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统

- ID: `2026-08-23-status-relic-combat-systems`
- Status: `active`
- Branch: `main`
- Created: `2026-08-23`
- Updated: `2026-08-23`

## 目标

- 按已确认设计（`docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`）实现：负面效果框架（点燃 DoT + 敌人有限生命/死亡）、遗物框架（出千）、收尾者重做（基础事件+穿甲弹+抽牌评估扩展）、特斯拉开火时增伤、呼应协同相邻触发；并落地用户后续需求（燃烧子弹事件、构筑页同步/清除、弹丸 2D 立绘、TestRobot 自动移动、Debug 格式简化）。

## 非目标

- 不改受保护 Prefab；不做遗物 UI/商店；不实现其他负面效果；不做"部分激活"模式。

## 已确认事实

- 敌人模型原为极简（`IDamageReceiver` + 无限血 `TargetDummy`），无状态/HP/死亡。
- `RequestBonusActivationResultModule` 原硬编码触发装备面，无目标面参数。
- 收尾者/特斯拉/呼应协同在旧被动物面语义下因 SourceFace/SameProjectileType 条件失效（本次随重做解决）。
- `TryRefillAndForceNextFace` 原不检查被动面（可把被动面拉回抽池，已修复）。
- 构筑 UI 布局 `FacePositions`：面 3 的 8 向相邻 = {1,2,4,6}。

## 已完成

- **流程**：brainstorming 6 问 + 设计 5 节经用户确认；规格（`a16736b`）、实施计划（`b613c70`）已提交；用户选内联执行。
- **T1–T8 五系统实现**：`EnemyHealth`/`TargetDummy` 有限血化；`EnemyStatusDefinition`/`EnemyStatusHost`（DoT/叠加/到期/全局状态事件）；`ApplyEnemyStatusResultModule`/`HasEnemyStatusConditionModule` + 点燃资产；`EventSignal.StatusTarget` + `EnemyStatusApplied` 信号位；遗物框架（`RelicDefinition`/`RelicRuntime`/`LoadedFirstFaceRelicDefinition` 出千）+ `SetFirstDrawForce` + `TryRefillAndForceNextFace` 被动面守卫；抽牌评估覆盖所有面基础槽规则；收尾者重构（DrawCandidate|Base 双触发 + 穿甲弹）；`RoundProjectileStatistic` + `ScaleActivationDamageFromStatisticResultModule` + `DiceFaceActivation.DamageMultiplier`（特斯拉开火时语义）；`DiceFaceAdjacency` + `TriggerAdjacentFacesResultModule`（呼应协同相邻触发）。
- **编译门禁**：MSBuild 三程序集（Prototype/Editor/EditMode.Tests）多次全量 exit 0。
- **规则资产更新**：Tesla/Finisher/EchoSynergy 三条规则按新结构重写（直接资产编辑，幂等，构建器 no-op）；新模块/资产固定 GUID meta。
- **燃烧子弹事件**：`BurningBulletRule.asset`（命中时 → 施加点燃）+ `BurningBullet.asset`（命中时槽位）+ 登记进 `DiceFaceLibrary`；期间修复该词条 `slotType` 误写（1→2），并新增装备失败诊断（`DiceFaceLoadout.Equip` 返回 bool + 构筑页 Console 警告 + `BurningBulletEntryEquipsIntoOnHitSlot`/`NewLightningRulesPassTheirSlotValidation` 复现测试）。
- **构筑页同步与清除**：`ClearSlot`/`ClearFace` + "清空"按钮；`DiceFaceEntry` 显示元数据优先解析绑定规则；打开重建；背景随规则颜色着色。
- **弹丸专属 2D 立绘**：`ProjectileDefinition.ProjectileSprite`（正式美术挂载点）+ `ProjectileSpriteVisual`（运行时 SpriteRenderer，俯视 Euler(90,0,0)，默认隐藏粒子）+ `ProjectileSpriteFactory`（灰色长方形/蓝色圆形/黑色细长方形占位）。
- **TestRobot 自动移动开关**：`TestRobotController.autoMove`（默认关，勾选后开始自动移动；瞄准/射击始终生效）。
- **Debug 显示简化**：`CombatDebugFormatter` 紧凑单行格式（`#12 骰面6 · 开火时：电磁共鸣：…`），测试断言同步。
- **收尾者词条状态修正**：`Finisher.asset` `isPassiveBase` 1→0（普通基础事件，不再占被动面；面 5 可正常抽取并发射穿甲弹）；迁移工具与测试同步。
- **静态门禁**：`DiceFaceSlotType.Passive`/`DiceFaceSlotMask.Passive` 代码零引用；受保护 Prefab git 干净。

## 当前正在进行

- EditMode 测试已全部执行完毕：聚焦 18/18（EnemyHealth/EnemyStatusHost/EventRuleLightningMigration）+ 全量 388/389（唯一失败=已批准 Ground 豁免）。首次真实运行修复了 13 个失败（见验证记录），代码与测试已同步。

## 下一步

1. 提交收尾（代码+测试+上下文，注意与事件规则编辑器工作流的文件提交拆分）。
2. 呼应协同 PlayMode 链式反应、特斯拉增伤手感、点燃表现的人工验收。

## 阻塞

- 无硬阻塞。

## 涉及文件

- `docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`、`docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md`
- 新：`EnemyHealth`、`EnemyStatusHost`、`EnemyStatusDefinition`、`ApplyEnemyStatusResultModule`、`HasEnemyStatusConditionModule`、`RelicDefinition`、`RelicRuntime`、`LoadedFirstFaceRelicDefinition`、`RoundProjectileStatistic`、`TriggerAdjacentFacesResultModule`、`DiceFaceAdjacency`、`ProjectileSpriteVisual`、`ProjectileSpriteFactory`、燃烧子弹/穿甲弹/点燃/出千资产
- 改：`DiceRevolverRuntime`、`DiceEventRuleRuntimeSet`、`EventRuleTypes`、`DiceRevolverGun`、`TargetDummy`、`DiceFaceEntry`、`DiceFaceLoadout`、`DiceFaceConfiguration`、`DiceBuildPageUI`、`DiceBuildFaceSlotUI`、`DiceBuildEntryButtonUI`、`DiceBuildRuntimeView`、`ProjectileDefinition`、`ProjectileVisualWrapper`、`CombatDebugFormatter`、`TestRobotController`、`EventRuleMigrationUtility`、`LightningBuildPrototypeBuilder` 及规则/词条/库资产
- `Assets/Tests/EditMode/`（EnemyHealthTests、EnemyStatusHostTests、EnemyStatusModuleTests、RelicTests、RoundProjectileStatisticTests、DiceFacePassiveSlotTests、EventRuleLightningMigrationTests、LightningBuildAssetTests、CombatDebugTraceTests 等）

## 验证记录

- [passed] `2026-08-23`：MSBuild 三程序集多次全量编译 exit 0（含全部新代码与测试）。
- [passed] `2026-08-23`：静态门禁与资产终态核验（词条槽位/规则掩码/受保护 Prefab 未触碰）。
- [passed] `2026-08-23`：GUID 链核验（脚本/资产 meta 与引用一致）、Unity 程序集含全部新类型、资产已导入。
- [passed] `2026-08-23`：Unity EditMode 全量回归 `388/389`、`0 skipped`；唯一失败为已批准 Ground `Y=-0.01` 豁免项，无新增失败。首次真实运行（此前被 UPM 单实例锁阻塞仅编译）修复 13 个失败：EditMode 测试中 `AddComponent` 不触发 Awake（EnemyHealth/EnemyStatusHost/TargetDummy 测试改用显式 `InvokePrivate("Awake")` 并在 Awake 前设好 MaxHealth）；`SignalTypeTriggerModule`/`SignalTypeConditionModule` 补 `EnemyStatusApplied` 信号映射（呼应协同触发器此前永不匹配）；迁移后资产语义与旧测试不同步（Tesla=OnFire+统计服务+`activation.DamageMultiplier`、Echo=EnemyStatusApplied+点燃条件+相邻面触发 spread 0/0）；`ForceFaceRejectsPassiveFaces` refill 池满拒绝语义（先抽空池验证）；`BaseRuleWithoutResolvablePrimaryProjectileReportsValidationError` 触发器补设 `signals=Base`；迁移工具 `SaveAssets()` 改 `SaveAssetIfDirty` 遵守定向保存契约；词条库 10→11 词条期望更新；删除与同类型保留语义矛盾的 `WindowReplacesTriggerModuleAndDestroysTheOldSubAsset` 调试测试；TestRobot 测试补设 `autoMove`。
- [passed] `2026-08-23`：测试后受保护 SHA256 复核——Player/TestRobot/TargetDummy 与基线一致，场景与其余六文件 git 干净。
- [not-run] `2026-08-23`：呼应协同含面 4 触发的 PlayMode 链式反应人工验收；特斯拉增伤公式与点燃参数占位待调。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
- [被动事件迁移工作流](../2026-08-23-passive-base-events/STATE.md)
