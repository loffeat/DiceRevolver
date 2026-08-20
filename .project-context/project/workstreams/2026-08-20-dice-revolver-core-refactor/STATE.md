# 左轮核心底层重构

- ID: `2026-08-20-dice-revolver-core-refactor`
- Status: `completed`
- Branch: `codex/dice-revolver-core-refactor`
- Created: `2026-08-20`
- Updated: `2026-08-20`

## 目标

- 统一固定六面规则，删除虚假的骰面数量配置。
- 把左轮机械状态、骰面事件流程和 Unity 场景执行拆成清晰模块。
- 消除枪械集成测试对旧私有实现管线的反射依赖。
- 将事件预算开放为每把枪可配置、每次激活固化的 Inspector 端口。
- 统一 Projectile 命中职责并移除 ProjectileHitReporter。

## 非目标

- 不实现雷电构筑。
- 不改变现有玩法、四槽位顺序、抽面、换弹、爆炸或事件预算语义。
- 不重构 UI、角色控制、AI 或伤害系统。
- 不修改场景，也不运行会重建或覆盖资产的 Builder。

## 已确认事实

- 项目领域固定为六面骰，不需要可配置面数。
- `DiceRevolverRuntime` 管理抽面、射击冷却与换弹；`DiceShotPipeline` 管理四阶段事件、延迟、命中和预算；`DiceRevolverGun` 只保留 Unity 适配职责。
- `eventBudgetPerActivation` Inspector 名称为“单次骰面事件预算”，默认 `32`、最小 `1`，并在每次激活创建时固化。
- Projectile 自己广播命中；顺序为捕获命中、广播、`ProjectileHit`、OnHit、直接伤害、销毁。
- `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities` 是用户明确豁免的唯一既有失败，不修改 Ground `-0.01` 或测试期望 `0`。

## 当前正在进行

- 无

## 已完成

- 完成职责范围、数据流、兼容迁移、错误处理和测试策略的分段确认。
- 完成正式架构设计规格、测试先行的分任务实施计划及自审。
- 固定六面规则并抽取 `DiceRevolverRuntime` 与 `DiceShotPipeline`。
- 收窄事件上下文能力，保持 FireStarted、Base、OnFire、FireEnded、OnFireEnd 顺序与 LoadedFour 自动换弹语义。
- `Projectile` 统一广播命中，Gun 切换到 Runtime/Pipeline/Projectile.Hit 适配路径。
- 删除旧 `DiceChamber`、`ProjectileHitReporter`、Gun 私有事件管线和公开 `SpawnConfiguredProjectile` 测试端口。
- Player/TestRobot Prefab 仅删除 `faceCount`、`reloadDropDistance`，新增 `eventBudgetPerActivation: 32`；其余受保护值保持不变。
- 测试不再反射 Gun 私有调度器、私有事件上下文创建、私有弹丸生成或命中桥接；`SpawnActivationProjectile` 仅保留为 Gun 私有 Unity 适配方法。
- 六次抽面测试已加强为精确集合 `{1, 2, 3, 4, 5, 6}`，并通过 `0..5` 受控 mutation 证明能捕获边界回归。
- 自动化结构重构完成：聚焦测试全绿，完整回归没有新增失败。

## 下一步

1. 读取[雷电构筑待办](../2026-08-20-lightning-build-backlog/STATE.md)，先确认设计边界，再另行规划实现。
2. 在可见 Unity PlayMode 人工验收六发不重复、空膛换弹、手动换弹、DoubleTap、BlastRound 与 LoadedFour。

## 阻塞

- 无。

## 尚未完成

- [not-run] 可见 PlayMode 战斗流程与手感人工验收；这不阻止已通过自动化的结构重构记为完成。
- 雷电构筑属于独立 planned 工作流，尚未设计或实现。

## 涉及文件

- `Assets/Scripts/Prototype/DiceRevolverRules.cs`
- `Assets/Scripts/Prototype/DiceRevolverRuntime.cs`
- `Assets/Scripts/Prototype/DiceShotPipeline.cs`
- `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs`
- `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`
- `Assets/Tests/EditMode/DiceRevolverCoreAssetMigrationTests.cs`
- `Assets/Prefab/Player.prefab`
- `Assets/Prefab/TestRobot.prefab`
- `.project-context/project/PROJECT.md`
- `.project-context/project/STATUS.md`
- `.project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/STATE.md`
- `.project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/HANDOFF.md`

## 验证记录

- [passed] `2026-08-20`：用户逐段确认模块职责、开火与命中数据流、兼容清理和测试设计。
- [passed] `2026-08-20`：实施计划完成规格覆盖、占位符和接口一致性自审。
- [failed] `2026-08-20`：Task 6 RED 为 `43/48`，预期暴露预算字段、固定六面 Inspector、Runtime 适配与 Projectile.Hit 接线缺口。
- [passed] `2026-08-20`：Task 6 Gun/Runtime/Pipeline/Inspector 联合测试 `70/70`。
- [failed] `2026-08-20`：Task 6 完整 EditMode 为 `165/166`；唯一失败是已获豁免的 Ground Y `-0.01` 既有差异，没有新增失败。
- [passed] `2026-08-20`：扫描 `rg -n "DiceChamber|ProjectileHitReporter|SpawnConfiguredProjectile|CreateEventContext|SpawnActivationProjectile|BridgeProjectileHit|eventTimeScheduler|faceCount|reloadDropDistance" Assets/Scripts Assets/Tests Assets/Prefab --glob '!*.meta'` 共 6 个预期测试/适配匹配；禁止的旧生产引用和四类测试私有管线反射均为 0。
- [failed] `2026-08-20`：受控 mutation 将补膛范围临时改为 `0..5` 后，`DiceRevolverRuntimeTests.DrawsAllSixFacesWithoutReplacement` 为 `0/1`，证明精确 `{1..6}` 断言能捕获旧“仅计数”断言遗漏的边界错误；临时生产改动随后恢复。
- [passed] `2026-08-20`：恢复真实 `1..6` 补膛范围后，同一抽面测试 `1/1`，0 失败、0 跳过。
- [passed] `2026-08-20`：`DiceRevolverRulesTests;DiceRevolverRuntimeTests;DiceFaceActivationTests;DiceShotPipelineTests;ProjectileCollisionTests;DiceRevolverGunIntegrationTests;DiceRevolverCoreAssetMigrationTests` 分层聚焦 EditMode `56/56`，0 失败、0 跳过；结果为 `Logs/core-refactor-focused.xml`。
- [failed] `2026-08-20`：完整 EditMode `169/170`，0 跳过；唯一失败为已获用户豁免的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`（Ground `-0.01`，测试期望 `0`），没有新增失败；结果为 `Logs/core-refactor-full.xml`。
- [passed] `2026-08-20`：项目上下文检查返回 `[context:ok]`；`git diff --check` 无错误。
- [passed] `2026-08-20`：提交前 `git status --short` 只包含本任务的 5 个测试/上下文文件。
- [not-run] `2026-08-20`：可见 PlayMode 人工验收未运行，不声称手感已验收。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-20-dice-revolver-core-refactor.md)
- [雷电构筑待办](../2026-08-20-lightning-build-backlog/STATE.md)
- [既有事件管线](../2026-08-18-projectile-definition-event-pipeline/STATE.md)
