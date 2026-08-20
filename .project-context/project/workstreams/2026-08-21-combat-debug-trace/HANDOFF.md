# 战斗事件因果 Debug 交接

## 当前状态

- `completed`，实现、资源、测试和规则列表设计文档均已完成。

## 当前方案及原因

- 使用每把 Gun 独立的纯 C# 追踪器分配顺序号与因果层级。
- Pipeline、被动 Runtime 和结果请求只发布结构化记录；左上角 UI 只是订阅者。

## 尚未完成

- 可见 PlayMode 排版与事件密度验收。
- 规则列表事件系统的代码实施。

## 下一步首个动作

- 打开 PlayMode 射击并观察左上角日志；若排版无问题，下一事项读取规则列表设计并编写实施计划。

## 必须保护

- 不修改 Player/TestRobot/TargetDummy Prefab、场景、AimRoot、sorting layer 或枪械调参。

## 风险与不可假定事项

- 不得把效果被检查误记为效果实际触发。
- 延迟结果必须在真实执行时记录，不能只在安排延迟时显示结果已发生。

## 最近验证

- [passed] Debug 聚焦回归 `107/107`。
- [failed] 完整 EditMode `249/250`，仅有既有 Ground 高度契约失败。
- [not-run] 可见 PlayMode 人工验收。

## 首先读取

- [工作流状态](STATE.md)
- [DiceShotPipeline.cs](../../../../Assets/Scripts/Prototype/DiceShotPipeline.cs)
- [DicePassiveRuntime.cs](../../../../Assets/Scripts/Prototype/DicePassiveRuntime.cs)
- [规则列表设计](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
