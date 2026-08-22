# 被动事件迁移为被动型基础事件 — 交接

## 当前状态

- `active`：brainstorming→规格→计划→内联执行全部完成；**T1–T8 代码与测试已实现**，等待 Unity 编译验证与 EditMode 测试（用户需刷新编辑器）。

## 当前方案及原因

- 删被动槽；被动只以"被动型基础事件"存在（`DiceFaceEntry.isPassiveBase` + 基础槽）；被动面永不入抽池（`DiceRevolverRuntime.RebuildActiveFaces` 池级排除）；每轮可抽面数 = 6−N；被动规则绑定被动面的 base 槽规则按信号随时生效；奖励射击不消耗骰面（现成机制）。
- 已确认决策 D1–D6；规格 `docs/superpowers/specs/2026-08-23-passive-base-events-design.md`（已提交 `daec3e0`）；计划 `docs/superpowers/plans/2026-08-23-passive-base-events.md`（已提交 `8cb8c99`）。

## 尚未完成

- Unity 编译验证（T1–T8 批量改动未编译）。
- EditMode 聚焦测试运行（DiceFacePassiveSlotTests、DiceRevolverRuntimeTests、EventRulePassiveIntegrationTests、EventRuleLightningMigrationTests、LightningBuildAssetTests 等）。
- 迁移执行：3 词条 slotType=0+isPassiveBase、3 规则 allowedSlots=1（由迁移测试/菜单执行）。
- T9：受保护资产 SHA256、全量回归、提交。

## 下一步首个动作

- 用户在 Unity 刷新（Ctrl+R / Assets > Refresh）确认编译，在 Test Runner 运行上述聚焦测试；有错误发回修复。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-23-passive-base-events-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-23-passive-base-events.md)

## 必须保护

- 不覆盖用户对 3 个被动规则 allowedSlots 的工作区改动语义（规格已批准归一为"基础(1)"）。
- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer、枪械调参。
- 保留抽象 `BulletEventEffect`/`PassiveEventEffect`、`DicePassiveRuntime` 类、hidden legacy 字段/read fallback 与三个 spawn 资产。

## 最近验证

- [passed] `2026-08-23`：MSBuild 2022 编译三个程序集（Prototype/Editor/EditMode.Tests）全部 exit 0——编译门禁通过。
- [passed] `2026-08-23`：资产迁移应用并核验（3 词条 slotType=0+isPassiveBase、3 规则 allowedSlots=1、`slotType: 4` 零残留）。
- [passed] `2026-08-23`：静态门禁（代码侧）——`DiceFaceSlotMask.Passive` 零引用；`DiceFaceSlotType.Passive` 仅剩迁移工具 legacy 读取（文档化豁免）。
- [passed] `2026-08-23`：受保护资产 git status 干净（Player/TestRobot/TargetDummy/场景/弹丸 Prefab 未触碰）。
- [blocked] `2026-08-23`：批处理 EditMode 测试——Unity UPM 单实例锁（打开的编辑器独占 UPM 服务器与许可证 IPC），隔离副本批处理两次均以"Could not establish a connection with the Unity Package Manager local server process"退出；需用户在 Test Runner 运行或关闭编辑器后重试。
- [not-run] `2026-08-23`：EditMode 测试执行与全量回归。

## 风险与不可假定事项

- 迁移测试与 `LightningBuildAssetTests` 已自足（各自先跑迁移），消除执行顺序依赖。
- 特斯拉/呼应协同/收尾者的"来源为装备骰面"类条件在被动面语义下需 PlayMode 人工验证其实际触发（被动面不发射弹丸，源面条件可能不再命中——规格 D2 已含此语义，待人工验收确认）。
- 提交拆分：`EventRuleEditorWindow.cs`、`EventRuleTypes.cs`、`EventRuleDefinition.cs` 同时含事件规则编辑器工作流（未提交）与本迁移改动，提交需按 hunk 拆分或与用户确认合并策略。
