# 左轮核心底层重构交接

## 当前状态

- Task 1-6 已完成：固定六面规则、Runtime、Pipeline、Projectile.Hit 与 Gun Unity 适配路径已落地。
- Gun 联合聚焦测试 `70/70`；完整 EditMode 只有已知 Ground Y 豁免失败。

## 尚未完成

- Task 7 Prefab/Reporter 迁移，以及最终完整验证与上下文收尾。

## 当前方案及原因

- 用 Runtime 管理左轮机械状态，用 Pipeline 管理骰面事件生命周期，Gun 只承担 Unity 适配职责。
- 固定六面使用统一规则常量，不保留虚假配置能力。
- Projectile 自己广播命中；Reporter 类型与 Prefab 组件暂留，待 Task 7 精确删除。
- 事件预算由每把枪配置，并在开火时固化到激活上下文。

## 下一步首个动作

- 读取 Task 7 简报，先写 Prefab/Builder/Reporter 迁移契约 RED，再只修改允许的资源与 Builder。

## 必须保护

- 重构保持玩法行为不变。
- 只允许 Player/TestRobot Prefab 删除 `faceCount`、`reloadDropDistance` 并新增默认事件预算；其他配置不得改变。
- 不实现任何雷电构筑内容。

## 风险与不可假定事项

- 自动换弹必须在开火后事件执行完成后判断，否则会破坏 LoadedFour。
- 命中广播、直接伤害和 OnHit 的相对顺序必须通过现有行为测试锁定。
- Task 6 没有修改 Prefab、场景、Builder 或 Reporter；这些仍属于 Task 7。

## 最近验证

- [passed] Task 6 RED `43/48`，失败来自新预算/Runtime/Projectile.Hit 适配契约缺失。
- [passed] Task 6 Gun/Runtime/Pipeline/Inspector 联合测试 `70/70`。
- [failed] 完整 EditMode `165/166`；唯一失败为已知 Ground Y `-0.01` 豁免项。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-20-dice-revolver-core-refactor.md)
- [工作流状态](STATE.md)
