# 被动事件迁移为被动型基础事件 — 交接

## 当前状态

- `planned`：brainstorming 完成，规格已写，等待用户评审。

## 当前方案及原因

- 删被动槽，被动只以"被动型基础事件"存在：`DiceFaceEntry.isPassiveBase` 标志 + 基础槽；被动面永不入抽池（池级排除），每轮可抽面数 = 6 − N；被动规则按信号随时生效；奖励射击不消耗骰面（现成机制）。
- 已确认决策：D1 删被动槽 / D2 纯被动面 / D3 数量不限(0–6 警告) / D4 词条级标志 / D5 池级排除 / D6 构筑改变即重建活动池。

## 尚未完成

- 规格用户评审；实施计划（writing-plans）；实现与测试。

## 下一步首个动作

- 请用户评审 `docs/superpowers/specs/2026-08-23-passive-base-events-design.md`，确认后调用 `writing-plans`。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-passive-base-events-design.md)
- [工作流状态](STATE.md)

## 必须保护

- 不覆盖用户对 TeslaRule/EchoSynergyRule/FinisherRule `allowedSlots` 的工作区改动前，先与用户确认归一为 `基础(1)`。
- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer、枪械调参。
- 保留抽象 `BulletEventEffect`/`PassiveEventEffect`、hidden legacy 字段/read fallback 与三个 spawn 资产。

## 最近验证

- [not-run] `2026-08-23`：尚无实现验证。

## 风险与不可假定事项

- 工作区 3 个被动规则 `allowedSlots`（2/4/1）与规格目标（1）不一致，需评审确认后归一。
- 构筑改变即重置本轮进度（D6）手感待人工验收。
