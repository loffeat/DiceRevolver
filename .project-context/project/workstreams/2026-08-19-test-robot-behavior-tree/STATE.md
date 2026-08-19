# 测试机器人行为树

- ID: `2026-08-19-test-robot-behavior-tree`
- Status: `completed`
- Branch: `codex/test-robot-behavior-tree`
- Created: `2026-08-19`
- Updated: `2026-08-19`

## 目标

- 新增测试机器人 Prefab 和场景实例。
- 使用行为树让机器人按距离接近、后退、横移，并持续瞄准射击。
- 六个骰面均发射伤害为 0 的基础左轮子弹。

## 非目标

- 不实现寻路、掩体、视野、仇恨或玩家受伤系统。
- 不调整玩家角色、AimRoot、sorting 或左轮数值。
- 不修改现有靶子 Prefab。

## 已确认事实

- 用户已批准直接实施，无需再设置审核节点。
- 机器人过远接近、过近后退、合适距离横移，持续瞄准射击。
- 机器人复用玩家移动、动画、瞄准和枪械执行方式。
- 机器人六面均为基础左轮子弹，伤害暂时为 0。

## 当前正在进行

- 无

## 已完成

- 完成设计规格和实施计划。
- 新增行为树与战斗距离决策测试，红灯已确认缺少目标生产类型。
- 新增通用行为树基础节点和 `TestRobotCombatBrain` 最小实现。
- 新增 `TopDownCharacterController` 共享运动接缝，玩家与机器人适配器分别提供输入和 AI 意图。
- 瞄准、动画桥和左轮在保留序列化字段名 `player` 的前提下改为消费共享控制器。
- 新增零伤害机器人弹丸定义、生成事件、`TestRobot.prefab` 和一个原型场景实例。
- 六个机器人骰面均绑定机器人零伤害基础射击事件；弹丸定义复用现有基础左轮弹丸 Prefab。
- 两个共享资源库只在末尾追加机器人条目。

## 下一步

1. 可选：在可见 Play Mode 中人工验收距离阈值、横移换向和连续射击手感。
2. 后续若加入正式敌人生命值或玩家受伤系统，保持 `TestRobotCombatBrain` 与执行层分离。

## 阻塞

- 无。

## 尚未完成

- 自动化范围内无；可见 Play Mode 手感验收未运行。

## 必须保护

- 不重存 `Assets/Prefab/Player.prefab` 和 `Assets/Prefab/TargetDummy.prefab`。
- 不运行完整 `TopDownPrototypeSceneBuilder`。
- 不修改玩家枪械参数、AimRoot 子物体 Transform 和 sorting。

## 涉及文件

- `docs/superpowers/specs/2026-08-19-test-robot-behavior-tree-design.md`
- `docs/superpowers/plans/2026-08-19-test-robot-behavior-tree.md`
- `Assets/Tests/EditMode/TestRobotBehaviorTreeTests.cs`
- `Assets/Scripts/Prototype/BehaviorTree.cs`
- `Assets/Scripts/Prototype/TestRobotCombatBrain.cs`
- `Assets/Scripts/Prototype/TopDownCharacterController.cs`
- `Assets/Scripts/Prototype/TestRobotController.cs`
- `Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs`
- `Assets/Tests/EditMode/TopDownCharacterControllerTests.cs`
- `Assets/Tests/EditMode/TestRobotAssetTests.cs`
- `Assets/Prefab/TestRobot.prefab`
- `Assets/Resources/DiceFacePrototype/Projectiles/TestRobotRevolverBullet.asset`
- `Assets/Resources/DiceFacePrototype/BulletEvents/FireTestRobotRevolverProjectile.asset`

## 验证记录

- [failed] `2026-08-19`：隔离 Unity 聚焦红灯编译失败，`BehaviorAction<>` 等行为树类型不存在，失败原因符合预期。
- [blocked] `2026-08-19`：补入最小实现后的绿灯运行在隔离工程首次资源导入期间被用户要求暂停；没有生成测试结果 XML，不得视为通过。
- [passed] `2026-08-19`：暂停后检查确认本次隔离 Unity 测试进程已结束。
- [not-run] `2026-08-19`：共享控制器、机器人资产和完整 EditMode 回归尚未运行。
- [passed] `2026-08-19`：行为树与战斗脑聚焦测试 `8/8`。
- [passed] `2026-08-19`：共享控制器聚焦测试 `2/2`，原有瞄准与枪械集成回归 `19/19`。
- [passed] `2026-08-19`：机器人 Prefab、零伤弹丸、六面绑定、共享库与场景实例资源测试 `4/4`。
- [passed] `2026-08-19`：场景渲染、原始 TargetDummy Prefab 边界和机器人资源联合回归 `7/7`。
- [passed] `2026-08-19`：完整 EditMode 回归 `121/121`，0 失败、0 跳过。
- [passed] `2026-08-19`：Player 与 TargetDummy Prefab SHA256 与任务前一致，玩家枪械调参未变化。
- [passed] `2026-08-19`：可移植上下文结构检查返回 `[context:ok]`。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-19-test-robot-behavior-tree-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-19-test-robot-behavior-tree.md)
