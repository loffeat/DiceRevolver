# 测试靶与伤害飘字交接

## 当前状态

- 伤害协议、无限生命测试靶、世界空间飘字、Prefab 和场景实例均已完成。

## 当前方案及原因

- 使用 `IDamageReceiver` 隔离弹丸与具体敌人；测试靶只广播伤害，飘字组件只监听表现事件。

## 尚未完成

- 当前可见 Unity 会话中的 Play Mode 连续射击与飘字节奏需要人工体验确认。

## 必须保护

- 不覆盖 `Player.prefab`、AimRoot 子物体、sorting layer 或 `DiceRevolverGun` Inspector 数值。
- 不运行完整场景重建器。

## 下一步首个动作

- 打开 `TopDownShooterPrototype` 场景进入 Play Mode，向玩家右侧的测试靶连续射击。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-18-target-dummy-damage-numbers-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-18-target-dummy-damage-numbers.md)
- [项目状态](../../STATUS.md)

## 最近验证

- [passed] `2026-08-18`：完整 Unity EditMode 回归 `69/69`。
- [passed] `2026-08-18`：Prefab/场景资产与真实脚本 GUID 测试 `2/2`。
- [passed] `2026-08-18`：静态场景与受击数字渲染检查通过。
- [passed] `2026-08-18`：Player Prefab、左轮参数、sorting、Packages 和 ProjectSettings 保护检查通过。

## 风险与不可假定事项

- EditMode 碰撞边界测试通过反射调用触发器，不能替代完整 Physics Play Mode 手感验证。
- 静态渲染只证明伤害数字初始位置、朝向和可读性，不证明上浮速度与淡出时长已经达到最终手感。
- `TargetDummy` 故意没有生命和死亡语义，正式敌人仍需独立生命组件。
