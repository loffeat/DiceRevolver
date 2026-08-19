# 命中范围爆炸交接

## 当前状态

- 命中范围爆炸的运行时、圆环视觉、资源绑定和自动化验证已完成。

## 尚未完成

- 可选的可见 Play Mode 视觉与手感验收。

## 当前方案及原因

- 复用现有爆炸弹丸定义请求，让骰面事件只负责在命中点请求爆炸。
- 爆炸 Prefab 自己拥有范围伤害和圆环表现，避免把物理与视觉逻辑塞入枪械或骰面事件。
- 伤害来自爆炸弹丸定义，半径和表现来自爆炸 Prefab。

## 下一步首个动作

- 在构筑页为任一骰面装备 BlastRound，射击 TargetDummy 或 TestRobot，观察圆环半径、颜色和持续时间。

## 必须保护

- 不修改 Player、TargetDummy、TestRobot Prefab 和玩家枪械参数。

## 风险与不可假定事项

- 当前没有通用阵营模块；敌人识别暂以实现 `IDamageReceiver` 且不属于发射者层级为准。
- 可见 Play Mode 的爆炸颜色与大小仍需人工验收。

## 最近验证

- [passed] 运行时聚焦 `4/4`，资源聚焦 `3/3`。
- [passed] 完整 EditMode 回归 `139/139`。
- [passed] 共享场景父节点下的兄弟角色不会被误判为爆炸发射者。
- [passed] Player、TargetDummy、TestRobot Prefab 哈希及玩家枪械参数保持不变。
- [not-run] 可见 Play Mode 视觉验收。

## 首先读取

- [工作流状态](STATE.md)
