# 骰面四槽位配置交接

## 当前状态

- 四槽位数据、运行时、UI 和示例资源迁移已完成，完整 EditMode 回归通过。

## 当前方案及原因

- 使用每面四槽位配置和单次激活快照，保证槽位独立并隔离构筑期间的数据变化。
- 保留旧 `entries` 与 `baseEffects` 作为只读兼容来源，避免重存 Player Prefab。

## 尚未完成

- 当前可见 Play Mode 中的构筑页排版和四槽位组合射击人工验收。

## 下一步首个动作

- 进入 `TopDownShooterPrototype` Play Mode，按 `E` 查看六个骰面的四行槽位文本是否清晰。

## 必须保护

- 不改 Player Prefab AimRoot 子物体 Transform、sorting 和左轮调参。
- 不覆盖工作树中的用户改动。
- 不运行完整场景生成器。

## 最近验证

- [passed] 四槽位数据模型 `10/10`。
- [passed] 激活快照 `7/7`、枪械集成 `13/13`、构筑 UI `7/7`、资源契约 `8/8`。
- [passed] 完整 EditMode `107/107`。
- [passed] Player Prefab 哈希、射速和换弹时间保持不变。
- [not-run] 当前可见 Play Mode 人工视觉与手感验收。

## 风险与不可假定事项

- EditMode 已验证四槽位数据和事件链，但不能替代构筑页最终排版的人工视觉确认。
- `ExplosionOnHitEffect.asset` 仍未配置爆炸弹丸定义，不应把 BlastRound 视为已有最终爆炸表现。
- 隐藏旧字段只用于序列化兼容；新代码不得重新依赖旧单词条接口。
- 不要运行完整 `TopDownPrototypeSceneBuilder`，它可能覆盖用户维护的场景和 Prefab 数据。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-19-dice-face-four-slots-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-19-dice-face-four-slots.md)
