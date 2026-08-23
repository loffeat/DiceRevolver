# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统 — 交接

## 当前状态

- `active`：T1–T8 代码与测试已实现，**编译门禁通过（MSBuild 三程序集 exit 0）**，静态门禁与资产终态核验通过；等待 Unity 导入新文件 + EditMode 测试执行。

## 当前方案及原因

- 五个系统为一个 Build 服务（出千锁 4 → 面4 链式反应 → 点燃触发呼应协同相邻激活 → 126 强制 4 点循环 → 收尾者+特斯拉增伤穿甲弹）。
- 已确认决策 D1–D6；规格 `docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`（提交 `a16736b`）；计划 `docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md`（提交 `b613c70`）。

## 尚未完成

- Unity 导入新文件（编辑器 18:20 重启但日志无新文件导入标记，需手动 `Ctrl+R`/`Assets > Refresh`）。
- 运行菜单 `Dice Revolver > Build Lightning Prototype Content` 重构 Finisher/Tesla/Echo 规则资产。
- EditMode 聚焦测试 + 全量回归；受保护 SHA256 复核；提交。

## 下一步首个动作

- 用户在 Unity 刷新（Ctrl+R）导入新文件；运行 Build Lightning 菜单；Test Runner 跑聚焦测试（EnemyHealthTests、EnemyStatusHostTests、EnemyStatusModuleTests、RelicTests、DiceRevolverRuntimeTests、EventRulePassiveIntegrationTests、EventRuleLightningMigrationTests、LightningBuildAssetTests、RoundProjectileStatisticTests）。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md)

## 必须保护

- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer、枪械调参。
- 受保护资产 SHA256 不变；规则资产迁移幂等。

## 最近验证

- [passed] `2026-08-23`：MSBuild 编译 Prototype/Editor/EditMode.Tests 三程序集 exit 0（T1–T8 全量）。
- [passed] `2026-08-23`：静态门禁——`DiceFaceSlotType.Passive`/`DiceFaceSlotMask.Passive` 代码零引用；受保护 Prefab git 干净。
- [passed] `2026-08-23`：资产终态核验（Tesla=OnFire/非被动、Echo/Finisher=被动基础、规则掩码对齐）。
- [not-run] `2026-08-23`：Unity 导入新文件与 EditMode 测试执行。

## 风险与不可假定事项

- 相邻触发含面 4（D4/D5）→ 链式反应排队可能造成额外循环，PlayMode 待验收。
- 特斯拉增伤公式（`TeslaDamagePerOrb`）与 DoT 频率为参数，默认值待调优。
- 规则资产重构（Finisher/Tesla/Echo）需在 Unity 运行 Build Lightning 菜单后生效；旧模块 SubAsset 会残留为孤儿（不影响运行）。
