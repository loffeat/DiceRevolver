# 测试机器人行为树

- ID: `2026-08-19-test-robot-behavior-tree`
- Status: `active`
- Branch: working tree
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

- 用户要求暂停推进；当前没有运行中的 Unity 批处理或文件生成任务。

## 已完成

- 完成设计规格和实施计划。
- 新增行为树与战斗距离决策测试，红灯已确认缺少目标生产类型。
- 新增通用行为树基础节点和 `TestRobotCombatBrain` 最小实现。

## 下一步

1. 在隔离 Unity 工程重新运行 `TestRobotBehaviorTreeTests`，确认当前最小实现绿灯。
2. 为共享角色控制接缝写失败测试，再实现玩家/机器人两类适配器。
3. 让瞄准、动画桥和左轮改为消费共享角色意图，但不重存 Player Prefab。
4. 生成机器人零伤害弹丸定义、Prefab 与场景实例。

## 阻塞

- 无技术阻塞；按用户要求暂停，等待后续继续指令。

## 尚未完成

- 行为树聚焦绿灯测试尚未得到结果。
- 共享角色控制器和 `TestRobotController` 尚未创建。
- 现有瞄准、动画和左轮尚未切换到共享角色类型。
- `TestRobot.prefab`、零伤害弹丸资源和场景实例尚未生成。

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

## 验证记录

- [failed] `2026-08-19`：隔离 Unity 聚焦红灯编译失败，`BehaviorAction<>` 等行为树类型不存在，失败原因符合预期。
- [blocked] `2026-08-19`：补入最小实现后的绿灯运行在隔离工程首次资源导入期间被用户要求暂停；没有生成测试结果 XML，不得视为通过。
- [passed] `2026-08-19`：暂停后检查确认本次隔离 Unity 测试进程已结束。
- [not-run] `2026-08-19`：共享控制器、机器人资产和完整 EditMode 回归尚未运行。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-19-test-robot-behavior-tree-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-19-test-robot-behavior-tree.md)
