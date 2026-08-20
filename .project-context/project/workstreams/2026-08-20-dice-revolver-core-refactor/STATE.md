# 左轮核心底层重构

- ID: `2026-08-20-dice-revolver-core-refactor`
- Status: `active`
- Branch: `codex/dice-revolver-core-refactor`
- Created: `2026-08-20`
- Updated: `2026-08-20`

## 目标

- 统一固定六面规则，删除虚假的骰面数量配置。
- 把左轮机械状态、骰面事件流程和 Unity 场景执行拆成清晰模块。
- 消除枪械集成测试对私有实现的大量反射依赖。
- 将事件预算开放为每把枪可配置、每次激活固化的 Inspector 端口。
- 统一 Projectile 命中职责并移除 ProjectileHitReporter。

## 非目标

- 不实现雷电构筑。
- 不改变现有玩法、四槽位顺序、抽面、换弹、爆炸或事件预算语义。
- 不重构 UI、角色控制、AI 或伤害系统。

## 已确认事实

- 项目领域固定为六面骰，不需要可配置面数。
- 采用 `DiceRevolverRuntime` 与 `DiceShotPipeline` 两个深模块，Gun 保留为 Unity 适配器。
- 允许精确清理 Player/TestRobot Prefab 的失效字段并新增默认预算 `32`。
- 事件预算每把枪独立配置，并在每次开火时固化到激活上下文。
- Projectile 统一拥有命中广播，现有 Reporter 将删除。
- 本轮严格保持现有玩法行为，雷电构筑另行实施。

## 当前正在进行

- 核心规则、事件能力、Runtime、Pipeline、Projectile.Hit 与 Gun 适配器已按 Task 1-6 完成。
- 下一步迁移 Player/TestRobot Prefab，随后删除 Reporter 类型与 Builder 依赖。

## 已完成

- 完成职责范围、数据流、兼容迁移、错误处理和测试策略的分段确认。
- 完成正式架构设计规格及自审。
- 完成测试先行、分任务提交的详细实施计划。
- 固定六面规则并抽取 `DiceRevolverRuntime` 与 `DiceShotPipeline`。
- `Projectile` 已统一广播命中，Gun 已切换到 Runtime/Pipeline/Projectile.Hit 适配路径。
- 删除旧 `DiceChamber`、Gun 私有事件管线与公开 `SpawnConfiguredProjectile` 测试端口。
- 新增每把枪 `eventBudgetPerActivation` Inspector 端口及真实 Gun 行为/接线契约测试。

## 下一步

1. 执行 Task 7：只迁移 Player/TestRobot Prefab 的失效字段与默认预算。
2. 删除 `ProjectileHitReporter` 类型、Prefab 组件和 Builder 依赖。
3. 完成最终聚焦测试、完整 EditMode 回归和上下文收尾。

## 阻塞

- 无。

## 尚未完成

- Player/TestRobot Prefab 序列化迁移。
- Reporter 类型、Prefab 组件与 Builder 引用清理。
- 最终完整回归与可见 Play Mode 人工验收。

## 涉及文件

- `docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md`
- `docs/superpowers/plans/2026-08-20-dice-revolver-core-refactor.md`
- `.project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/STATE.md`
- `.project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/HANDOFF.md`
- `.project-context/project/STATUS.md`
- `.project-context/project/PROJECT.md`

## 验证记录

- [passed] `2026-08-20`：用户逐段确认模块职责、开火与命中数据流、兼容清理和测试设计。
- [passed] `2026-08-20`：实施计划完成规格覆盖、占位符和接口一致性自审。
- [failed] `2026-08-20`：Task 6 RED 为 `43/48`，预期暴露预算字段、固定六面 Inspector、Runtime 适配与 Projectile.Hit 接线缺口。
- [passed] `2026-08-20`：Task 6 Gun/Runtime/Pipeline/Inspector 联合测试 `70/70`。
- [failed] `2026-08-20`：完整 EditMode 为 `165/166`；唯一失败是已获豁免的 Ground Y `-0.01` 既有差异，没有新增失败。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-20-dice-revolver-core-refactor.md)
- [雷电构筑待办](../2026-08-20-lightning-build-backlog/STATE.md)
- [既有事件管线](../2026-08-18-projectile-definition-event-pipeline/STATE.md)
