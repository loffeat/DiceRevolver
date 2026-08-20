# 左轮核心底层重构交接

## 当前状态

- 架构设计和详细实施计划均已写入，等待用户选择执行方式；尚未修改运行时代码。

## 尚未完成

- TDD 实现、Prefab 迁移和完整验证。

## 当前方案及原因

- 用 Runtime 管理左轮机械状态，用 Pipeline 管理骰面事件生命周期，Gun 只承担 Unity 适配职责。
- 固定六面使用统一规则常量，不保留虚假配置能力。
- Projectile 自己广播命中，删除 Reporter。
- 事件预算由每把枪配置，并在开火时固化到激活上下文。

## 下一步首个动作

- 按用户选择调用 subagent-driven-development（推荐）或 executing-plans，并在隔离工作树执行实施计划 Task 1。

## 必须保护

- 重构保持玩法行为不变。
- 只允许 Player/TestRobot Prefab 删除 `faceCount`、`reloadDropDistance` 并新增默认事件预算；其他配置不得改变。
- 不实现任何雷电构筑内容。

## 风险与不可假定事项

- 自动换弹必须在开火后事件执行完成后判断，否则会破坏 LoadedFour。
- 命中广播、直接伤害和 OnHit 的相对顺序必须通过现有行为测试锁定。
- 计划完成不代表运行时代码已经修改。

## 最近验证

- [passed] 用户已逐段确认完整架构设计。
- [passed] 实施计划已完成规格覆盖、占位符和类型签名一致性自审。
- [not-run] Unity 测试未运行，因为尚未进入实施阶段。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-20-dice-revolver-core-refactor.md)
- [工作流状态](STATE.md)
