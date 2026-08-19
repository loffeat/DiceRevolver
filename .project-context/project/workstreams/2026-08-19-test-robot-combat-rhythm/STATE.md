# 测试机器人战斗节奏与站立姿态

- ID: `2026-08-19-test-robot-combat-rhythm`
- Status: `completed`
- Branch: `codex/test-robot-combat-rhythm`
- Created: `2026-08-19`
- Updated: `2026-08-19`

## 目标

- 让测试机器人保持与玩家一致的站立立绘方式，不旋转整个角色根节点。
- 把持续移动改为移动一段时间、站定攻击一段时间、再重新决策的循环。

## 非目标

- 不新增寻路、掩体、攻击预警、技能或正式敌人生命系统。
- 不修改 Player Prefab、TargetDummy Prefab 或玩家枪械参数。

## 已确认事实

- 用户确认站定期间继续瞄准并射击。
- 默认节奏采用移动 `0.7` 秒、站定 `1.0` 秒。
- 每次结束站定后重新判断接近、后退或横移方向。

## 当前正在进行

- 无

## 已完成

- 已定位立绘异常来自机器人继承的 `rotateBodyTowardAim=true`，玩家 Prefab 对应值为 `false`。
- `TestRobotCombatBrain` 增加 Moving/Holding 阶段，默认移动 `0.7` 秒、站定 `1.0` 秒。
- 站定阶段移动输入为零，但瞄准点与开火意图持续更新。
- 站定结束后重新按当前距离选择接近、后退或横移；横移换向发生在新的移动爆发中。
- `TestRobotController` 暴露中文 Inspector 时长端口并把配置传入行为脑。
- 专用构建器可幂等更新已有 TestRobot Prefab，关闭根节点朝瞄准方向旋转并写入节奏参数。
- TestRobot Prefab 只改动 `rotateBodyTowardAim`、`movementDuration` 和 `holdingDuration`。

## 下一步

1. 可选：在可见 Play Mode 中人工验收移动距离、站定时长和射击节奏。

## 阻塞

- 无。

## 涉及文件

- `Assets/Scripts/Prototype/TestRobotCombatBrain.cs`
- `Assets/Scripts/Prototype/TestRobotController.cs`
- `Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs`
- `Assets/Prefab/TestRobot.prefab`
- `Assets/Tests/EditMode/TestRobotBehaviorTreeTests.cs`
- `Assets/Tests/EditMode/TestRobotAssetTests.cs`

## 验证记录

- [passed] `2026-08-19`：隔离工作树基线 `121/121`。
- [failed] `2026-08-19`：战斗节奏 RED 因缺少五参数构造、阶段枚举和决策端口而编译失败，符合预期。
- [passed] `2026-08-19`：行为树与共享控制器聚焦测试 `11/11`。
- [failed] `2026-08-19`：侧向瞄准让机器人根节点旋转 `90°`，站立姿态 RED 符合预期。
- [passed] `2026-08-19`：机器人行为、共享控制和 Prefab 姿态联合聚焦测试 `16/16`。
- [failed] `2026-08-19`：把站定时长临时缩短为一半后，边界测试按预期从 Holding 变为 Moving 并失败；恢复正确实现后完整回归通过。
- [passed] `2026-08-19`：完整 EditMode 回归 `123/123`，0 失败、0 跳过。
- [passed] `2026-08-19`：Player 与 TargetDummy Prefab SHA256 与任务前一致。

## 相关资料

- [前序工作流](../2026-08-19-test-robot-behavior-tree/STATE.md)
