# 雷电构筑系统交接

## 当前状态

- `completed`。运行时、资源、迁移、UI 和 EditMode 自动化均已完成。
- 新内容已进入资源库，但未自动装备到 Player 或 TestRobot。

## 当前方案及原因

- 左轮机械、活动事件和被动状态分别由 `DiceRevolverRuntime`、`DiceShotPipeline`、`DicePassiveRuntime` 管理，避免单个构筑效果故障影响角色 3C。
- 类型/标签使用 ScriptableObject 身份，弹丸查询按 Gun 隔离；新词条通过资源库发现，不直接改写角色 Prefab。

## 已实现边界

- 每个骰面拥有基础、开火时、命中时、开火后、被动五个独立槽位。
- 被动通过每枪每面的独立 Runtime 工作；活动事件仍由 `DiceShotPipeline` 派发。
- 雷电球、收尾者、电磁共鸣、特斯拉、呼应协同、链式反应均按已批准规则完成。
- 闪电链直接结算范围伤害，不属于攻击特效，不触发命中后事件。
- 呼应协同奖励射击允许同帧触发，使用自然角度散布并共享最多四次的因果预算。
- 链式反应只复制非空活动槽，消耗自身开火后槽，不复制被动，也不由奖励射击消费。

## 下一步首个动作

- 在可见 PlayMode 中临时装备新词条，人工观察雷电球尺寸、链路可读性、同帧散布和六发节奏；确认后再做数值微调。

## 尚未完成

- 可见 PlayMode 视觉与手感验收。
- 正式平衡、局内掉落、存档和正式敌人生命/死亡流程。

## 必须保护

- 不覆盖 Player、TargetDummy、TestRobot Prefab。
- 不改变 AimRoot 子物体 Transform、sorting layer、Ground `Y=-0.01`、角色美术或现有骰面装备。
- 不改写 Player 的 `holdDistance=0.85`、`holdHeight=0.72`、`shotsPerSecond=2`、`reloadDuration=2`。

## 风险与不可假定事项

- 自动化验证不等于最终视觉验收，雷电链宽度、持续时间和散布角度仍可能需要手感调整。
- 不得假定资源库中的新词条已经装备；Player 和 TestRobot 当前配置保持原样。
- 完整 EditMode 的 Ground 高度契约失败是既有豁免，不应通过改写 Ground 或受保护场景来消除。

## 最近验证

- [passed] 雷电资源/UI/Inspector 联合 EditMode `65/65`。
- [failed] 完整 EditMode `233/234`；唯一失败是既有 Ground 高度契约差异，没有新增失败。
- [passed] 三个受保护 Prefab 哈希及 Player 左轮参数实施前后完全一致。
- [not-run] 可见 PlayMode 人工验收。

## 首先读取

- [工作流状态](STATE.md)
- [正式设计规格](../../../../docs/superpowers/specs/2026-08-20-lightning-build-system-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-lightning-build-system.md)
