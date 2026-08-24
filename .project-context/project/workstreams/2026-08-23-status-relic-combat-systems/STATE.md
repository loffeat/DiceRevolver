# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统

- ID: `2026-08-23-status-relic-combat-systems`
- Status: `active`
- Branch: `main`
- Created: `2026-08-23`
- Updated: `2026-08-24`

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
- 呼应协同实机不触发的根因不是资产配置：点燃的全局通知丢失来源激活/共享预算，随后被动面信号改写又丢失状态目标；旧测试直接手工构造完整信号，未覆盖这两个边界。
- 右上角骰面 HUD 与实际射击不一致的根因是 UI 监听了 `FireStarted` 并自行推断弹仓；奖励激活也会广播该事件，导致 UI 置灰但真实弹仓未消耗。运行时实际是每枪从剩余池随机抽取，并不存在预生成顺序。
- 收尾者在“链式反应”构筑中看似不触发的根因是：链式反应把来源骰面的非空基础槽覆盖到下一发；收尾者恰好被延后为最后一发时，其基础规则被覆盖成上一面的基础弹幕，因此抽到了收尾者所在面却生成了别的弹丸。

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
- **呼应协同真实链路修复**：状态施加通知现在携带造成点燃的 `DiceFaceActivation`，Rule Runtime 改写为装备面信号时保留 `StatusTarget`；新增真实 Gun 边界回归测试，验证面 4 燃烧命中后按顺序追加激活 `{1,2,4,6}`。
- **出千：4 拾取物紫色修复**：`RelicPickup_Face4.prefab` 两个 `SpriteRenderer` 的错误内置材质引用（`fileID 2100000/type 2`）改为项目 `GraphicsSettings` 声明的默认 Sprite 材质（`fileID 10754/type 0`）；保留用户在场景实例设置的 `A_Item_StartWithFour` Sprite。
- **骰面 HUD 真实弹仓同步**：`DiceRevolverRuntime` 在抽取、补回与换弹完成时发布只读剩余面快照，`DiceRevolverGun` 暴露当前快照并转发变更；`DiceRevolverAmmoUI` 启用时先读取当前快照，之后只按快照实时刷新，不再监听开火/奖励事件推断弹药。
- **保护槽位能力与事件配置回档**：`EventRuleDefinition` 保留数据化的“保护槽位免受覆盖”开关，`DiceFaceConfigurationSnapshot.MergeActiveOverlay` 对四个活动槽统一遵守；按用户要求，现有事件均恢复为修改前配置，没有事件默认启用保护。收尾者再次可被链式反应覆盖。
- **静态门禁**：`DiceFaceSlotType.Passive`/`DiceFaceSlotMask.Passive` 代码零引用；受保护 Prefab git 干净。

## 当前正在进行

- 用户 PlayMode 反馈的呼应协同与骰面 HUD 已修复；收尾者覆盖行为按用户确认回档：仍保持最后抽取，但若前一发链式反应复制了基础槽，则收尾者基础弹丸会被覆盖。通用保护槽位能力保留，当前没有现有事件启用。

## 下一步

1. 在后续正常 PlayMode 中观察右上角六面：每次真实抽取只灰掉对应面，奖励激活不改变 HUD，手动/自动换弹完成后恢复全部活动面。
2. 用截图构筑复验收尾者最后一发：倒数第二发触发链式反应并带有基础事件时，收尾者应发射被复制的基础弹丸；继续验收呼应协同、特斯拉增伤手感和点燃表现。
3. 根据人工验收结果调整占位参数；若无问题，将本工作流标记为 `completed`。

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
- [passed] `2026-08-24`：新增 `BurningHitTriggersEchoAdjacentFacesThroughTheGunNotificationBoundary`；修复前 Unity 全量运行中明确红灯（只观察到初始面 4），修复后聚焦重跑绿灯并观察到 `{4,1,2,4,6}`。Unity Test Runner 汇总由 `389/393`、4 failed 更新为 `390/393`、3 failed；剩余失败属于当前工作区既有的资产迁移/场景契约等问题，未作为本修复通过依据。
- [passed] `2026-08-24`：出千：4 拾取物材质引用与 `GraphicsSettings.m_SpritesDefaultMaterial` 对照通过；场景实例没有 `m_Materials` 覆盖，Unity Editor 日志确认 Prefab 已重新导入且未报告该资源错误。
- [passed] `2026-08-24`：`DiceRevolverAmmoUITests` 修复前 `0/3`，修复后隔离 Unity EditMode `3/3`、`0 skipped`，覆盖延迟启用读取真实状态、抽取实时置灰、手动换弹实时恢复；Unity 日志 code 0。
- [blocked] `2026-08-24`：主 Unity 编辑器保持开启时，第二个隔离批处理进程因 Unity 全局 `CurlRequestCache.db` 冲突而无法补跑完整 Runtime 测试；已终止本次后台测试进程，没有关闭或操作主编辑器。
- [passed] `2026-08-24`：按用户要求回档事件配置。隔离 Unity 先得到 RED `1/3 passed, 2/3 failed`（收尾者仍受保护），移除 `FinisherRule` 与构建器的保护启用后 GREEN `3/3`、`0 failed`：收尾者可被链式反应基础槽覆盖，显式开启保护的自定义规则仍保留槽位。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
- [被动事件迁移工作流](../2026-08-23-passive-base-events/STATE.md)
