# 左轮核心底层重构

- ID: `2026-08-20-dice-revolver-core-refactor`
- Status: `active`
- Branch: `main`
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

- 实施计划已写入，等待用户选择执行方式。

## 已完成

- 完成职责范围、数据流、兼容迁移、错误处理和测试策略的分段确认。
- 完成正式架构设计规格及自审。
- 完成测试先行、分任务提交的详细实施计划。

## 下一步

1. 用户选择 Subagent-Driven 或当前会话内联执行。
2. 使用隔离工作树按测试先行实施。
3. 完成聚焦测试、完整 EditMode 回归和上下文同步。

## 阻塞

- 无；运行时代码实施等待用户选择执行方式。

## 尚未完成

- 运行时代码、Prefab 迁移和测试迁移均未开始。

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
- [not-run] Unity 测试未运行；当前仅完成架构设计和实施计划，不修改运行时代码或资源。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-20-dice-revolver-core-refactor.md)
- [雷电构筑待办](../2026-08-20-lightning-build-backlog/STATE.md)
- [既有事件管线](../2026-08-18-projectile-definition-event-pipeline/STATE.md)
