# 骰面构筑系统交接

## 当前状态

- 历史实现已完成并保留；后续缺口应创建新的独立工作流。

## 当前方案及原因

- 使用 ScriptableObject 组合骰面词条和事件效果，以保持 UI、枪械和弹丸低耦合。

## 尚未完成

- 爆炸 Prefab、敌人伤害与穿透消费、构筑持久化和完整 PlayMode 验证不在本历史事项范围内。

## 下一步首个动作

- 先在 `TopDownShooterPrototype.unity` 进入 Play Mode，人工确认 E 页面自举、左右枪口对齐和鼠标贴近枪口时的视觉稳定性；后续产品缺口使用新工作流。

## 首先读取

- [项目地图](../../PROJECT.md)
- [原设计规格](../../../../docs/superpowers/specs/2026-08-16-dice-face-build-system-design.md)

## 必须保护

- 不擅自覆盖 `Player.prefab` 的 AimRoot、ArmVisual、GunBody、Muzzle、sorting layer 和枪械调参。

## 最近验证

- [passed] `2026-08-17`：Unity `6000.3.10f1` 隔离临时工程 EditMode 测试 `29/29` 通过。
- [passed] `2026-08-17`：当前实现只读复审未发现 Critical 或 Important 问题。
- [not-run] `2026-08-17`：完整 PlayMode 战斗流程和当前场景最终视觉对齐未运行。

## 风险与不可假定事项

- EditMode 通过不等于当前场景的美术视觉表现已经人工确认。
- `ExplosionOnHitEffect.asset` 的 Prefab 端口仍为空。
