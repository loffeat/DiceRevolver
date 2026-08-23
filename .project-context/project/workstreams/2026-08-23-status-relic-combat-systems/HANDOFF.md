# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统 — 交接

## 当前状态

- `planned`：brainstorming 完成，规格已写，等待用户评审。

## 当前方案及原因

- 五个系统为一个 Build 服务：出千锁首抽 4 → 面 4（雷电球/双发/点燃/链式反应）→ 点燃触发呼应协同相邻激活（{1,2,4,6} 完整激活）→ 126 的强制 4 点循环 → 收尾者最后抽到 + 特斯拉增伤穿甲弹。
- 已确认决策 D1–D6（通用框架优先：状态框架/遗物框架；收尾者=基础事件；相邻含面 4；完整激活；抽牌评估覆盖普通面）。

## 尚未完成

- 规格用户评审；实施计划（writing-plans）；实现与测试。

## 下一步首个动作

- 请用户评审 `docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`，确认后调用 `writing-plans`。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
- [工作流状态](STATE.md)

## 必须保护

- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer、枪械调参。
- 既有受保护资产 SHA256 不变；规则资产迁移幂等。

## 最近验证

- [not-run] `2026-08-23`：尚无实现验证。

## 风险与不可假定事项

- 相邻触发含面 4（D4/D5）→ 链式反应排队可能造成额外循环，PlayMode 待验收。
- 特斯拉增伤公式与统计口径、DoT 频率为参数，默认值待资产设计时定。
