# 测试机器人行为树交接

## 当前状态

- 自动化实现与验证已完成。测试机器人已打包为独立 Prefab，并在原型场景中放置一个实例。
- 工作分支为 `codex/test-robot-behavior-tree`；等待正常分支收尾与集成。

## 尚未完成

- 可选的可见 Play Mode 手感验收：距离阈值、横移换向、持续射击和 0 伤害命中表现。

## 当前方案及原因

- 共享角色控制器只接受移动、瞄准、射击和换弹意图。
- 纯 C# 行为树决定机器人意图，Unity 适配器负责执行。
- 这种接缝让玩家键鼠输入与机器人 AI 共用运动/瞄准/武器执行，同时隔离故障。

## 下一步首个动作

- 若继续玩法调优，先在 `TopDownShooterPrototype.unity` 的 Play Mode 中观察机器人，再只调整 TestRobot Prefab 上的距离阈值、换向周期或共享移动速度端口。

## 必须保护

- Player Prefab、TargetDummy Prefab、玩家枪械数值、AimRoot Transform 和 sorting 不得被覆盖。
- 共享资源库只追加机器人条目。

## 风险与不可假定事项

- 当前项目未安装 Unity Behavior package；本事项使用内部小型行为树模块。
- EditMode 验证不能替代最终 Play Mode 的战斗距离与横移手感验收。
- 机器人子弹伤害为 0，当前不扩展玩家受伤系统。
- 不得把被暂停的绿灯运行视为通过：日志停在首次资源导入阶段，没有测试结果 XML。
- Player 和 TargetDummy Prefab 的 SHA256 已验证保持不变；场景只追加一个 TestRobot Prefab 实例，共享库只追加机器人定义和事件。

## 最近验证

- [failed] 行为树红灯符合预期：缺少 `BehaviorAction<>` 等生产类型。
- [passed] 行为树 `8/8`、共享控制器 `2/2`、既有瞄准与枪械 `19/19`。
- [passed] 机器人资源 `4/4`、场景联合回归 `7/7`。
- [passed] 完整 EditMode 回归 `121/121`，0 失败、0 跳过。
- [passed] 受保护 Prefab 哈希和玩家枪械数值保持不变。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-19-test-robot-behavior-tree-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-19-test-robot-behavior-tree.md)
