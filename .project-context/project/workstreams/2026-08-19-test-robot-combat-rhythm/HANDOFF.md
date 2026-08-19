# 测试机器人战斗节奏与站立姿态交接

## 当前状态

- 实现与自动化验证已完成；工作分支为 `codex/test-robot-combat-rhythm`。

## 尚未完成

- 可选的可见 Play Mode 手感验收。

## 当前方案及原因

- 行为脑增加移动与站定两个阶段；移动阶段维持原距离策略，站定阶段只停止位移，持续瞄准射击。
- 机器人禁用角色根节点朝瞄准方向旋转，视觉方向继续交给现有 Sprite 翻面与手臂瞄准。

## 下一步首个动作

- 在原型场景 Play Mode 中观察移动距离与站定时长；若需调优，只调整 TestRobot Prefab 的两个中文时长端口。

## 必须保护

- 不重存 Player Prefab 和 TargetDummy Prefab；不修改玩家枪械数值。

## 风险与不可假定事项

- EditMode 能验证状态节奏与 Prefab 配置，但不能替代最终 Play Mode 手感验收。

## 最近验证

- [passed] 基线 `121/121`，最终完整回归 `123/123`。
- [passed] 联合聚焦测试 `16/16`。
- [passed] 受保护 Player 与 TargetDummy Prefab 哈希未变化。

## 首先读取

- [工作流状态](STATE.md)
- [前序工作流](../2026-08-19-test-robot-behavior-tree/STATE.md)
