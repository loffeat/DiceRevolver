# 测试机器人行为树交接

## 当前状态

- 用户要求暂停。设计、计划、失败测试、行为树基础节点和战斗脑最小实现已保存。
- 行为树绿灯测试尚无结果；后续模块均未开始。

## 尚未完成

- 重新运行并确认行为树聚焦绿灯。
- 实现共享角色控制器、玩家适配器和机器人适配器。
- 生成零伤害弹丸资源、测试机器人 Prefab 和场景实例。
- 运行完整 EditMode 回归与受保护资产核验。

## 当前方案及原因

- 共享角色控制器只接受移动、瞄准、射击和换弹意图。
- 纯 C# 行为树决定机器人意图，Unity 适配器负责执行。
- 这种接缝让玩家键鼠输入与机器人 AI 共用运动/瞄准/武器执行，同时隔离故障。

## 下一步首个动作

- 继续时先把当前文件同步到隔离测试工程，重新运行 `DiceRevolver.Tests.TestRobotBehaviorTreeTests`，确认现有最小实现是否通过；不要直接推进共享控制器。

## 必须保护

- Player Prefab、TargetDummy Prefab、玩家枪械数值、AimRoot Transform 和 sorting 不得被覆盖。
- 共享资源库只追加机器人条目。

## 风险与不可假定事项

- 当前项目未安装 Unity Behavior package；本事项使用内部小型行为树模块。
- EditMode 验证不能替代最终 Play Mode 的战斗距离与横移手感验收。
- 机器人子弹伤害为 0，当前不扩展玩家受伤系统。
- 不得把被暂停的绿灯运行视为通过：日志停在首次资源导入阶段，没有测试结果 XML。
- 当前只新增了 `BehaviorTree.cs`、`TestRobotCombatBrain.cs` 和对应测试；Player、TargetDummy、场景与资源库都未改写。

## 最近验证

- [failed] 行为树红灯符合预期：缺少 `BehaviorAction<>` 等生产类型。
- [blocked] 行为树绿灯因用户要求暂停而终止，没有结果文件。
- [passed] 暂停后没有遗留本次 Unity 测试进程。
- [not-run] 共享控制、机器人资产、场景和完整回归。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-19-test-robot-behavior-tree-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-19-test-robot-behavior-tree.md)
