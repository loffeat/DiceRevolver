# 骰面构筑系统交接

## 当前状态

- 历史实现已完成并保留；后续缺口应创建新的独立工作流。

## 当前方案及原因

- 使用 ScriptableObject 组合骰面词条和事件效果，以保持 UI、枪械和弹丸低耦合。

## 尚未完成

- 爆炸 Prefab、敌人伤害与穿透消费、构筑持久化和完整 PlayMode 验证不在本历史事项范围内。

## 下一步首个动作

- 从 `STATUS.md` 选择一个已知缺口，并用模板创建新工作流。

## 首先读取

- [项目地图](../../PROJECT.md)
- [原设计规格](../../../../docs/superpowers/specs/2026-08-16-dice-face-build-system-design.md)

## 必须保护

- 不擅自覆盖 `Player.prefab` 的 AimRoot、ArmVisual、GunBody、Muzzle、sorting layer 和枪械调参。

## 最近验证

- [not-run] `2026-08-17`：新上下文系统没有重跑 Unity 测试。

## 风险与不可假定事项

- 测试文件存在不等于当前设备已经运行并通过。
- `ExplosionOnHitEffect.asset` 的 Prefab 端口仍为空。
