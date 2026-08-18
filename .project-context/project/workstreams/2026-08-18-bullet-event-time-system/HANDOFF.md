# 子弹事件时间系统交接

## 当前状态

- 时间系统、双重射击延迟、中文端口和自动化回归均已完成。

## 当前方案及原因

- 使用由左轮持有、由 Context 暴露的普通 C# 调度器。事件保持低耦合，调度器接收显式时间并可确定测试，同时避免全局单例和协程生命周期问题。

## 尚未完成

- 当前场景 PlayMode 中 `0.25` 秒双重射击的视觉与手感验证。

## 下一步首个动作

- 打开 `ExtraShotOnFireEffect.asset` 调整“第二发延迟（秒）”，进入 Play Mode 装备“双重射击”并确认两发间隔。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-18-bullet-event-time-system-design.md)
- [项目状态](../../STATUS.md)

## 必须保护

- 不覆盖 `Player.prefab` 的 AimRoot、ArmVisual、GunBody、Muzzle、sorting layer 和枪械调参。
- 不修改用户填写的 `DiceRevolverGun` Inspector 数值。
- 中文名称只通过 Inspector 元数据提供，不重命名序列化字段。

## 最近验证

- [passed] `2026-08-18`：调度器 `6/6`、事件 `6/6`、左轮集成 `5/5`、中文端口 `23/23` 分组测试通过。
- [passed] `2026-08-18`：Unity EditMode 完整回归 `63/63` 通过。
- [not-run] `2026-08-18`：PlayMode 视觉与手感验证未运行。

## 风险与不可假定事项

- EditMode 测试不能替代最终 PlayMode 视觉验证。
- 当前系统没有取消、循环或持久化能力；只有出现明确玩法需求时再扩展。
