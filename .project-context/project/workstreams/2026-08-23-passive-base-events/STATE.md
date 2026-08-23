# 被动事件迁移为被动型基础事件

- ID: `2026-08-23-passive-base-events`
- Status: `planned`
- Branch: `main`
- Created: `2026-08-23`
- Updated: `2026-08-23`

## 目标

- 按已确认设计（`docs/superpowers/specs/2026-08-23-passive-base-events-design.md`）把被动事件迁移为"被动型基础事件"：删被动槽、词条级被动标志、被动面永不入抽池、每轮可抽面数 = 6 − N、被动规则按信号随时生效。

## 非目标

- 不新增触发信号类型；不实现构筑存档；不限制被动面数量（0–6 警告）；不修改受保护 Prefab。

## 已确认事实

- 10 个词条均为 Rule-backed；3 个被动词条（Tesla/EchoSynergy/Finisher）`slotType: 4`。
- 受保护 Prefab 的 loadout 不含被动数据。
- 会话期间 3 个被动规则资产 `allowedSlots` 被编辑器从 16 改为 2/4/1（工作区未提交）；规格要求迁移归一为 `基础(1)`。
- 奖励射击不消耗骰面（现成机制支持"触发指定面事件且不消耗"）。

## 已完成

- brainstorming 全部决策（D1–D6）经用户确认；设计规格已写入 `docs/superpowers/specs/2026-08-23-passive-base-events-design.md` 并提交（`daec3e0`）。
- 规格经用户评审批准（含允许把 3 个被动规则 allowedSlots 归一为"基础(1)"）。
- 实施计划已写入 `docs/superpowers/plans/2026-08-23-passive-base-events.md`（9 个任务，writing-plans 自审通过，含跨任务编译断点修正）。
- 用户选择内联执行（executing-plans）并在 main 上实施。
- **T1–T8 代码与测试已实现**：数据模型（删被动槽/词条标志/快照 IsPassiveFace）、Loadout 被动面集合、骰池池级排除（RebuildActiveFaces/ActiveFaceCount）、Gun 接线（legacy 被动路径摘除）、规则运行时被动绑定（被动面 base 槽）、校验器清理、编辑器 4 分类、构筑 UI 被动徽标、迁移工具（MigratePassiveBaseEvents/MigratePassiveBaseEntries/MigratePassiveRuleSlots）与 LightningBuildPrototypeBuilder 新语义、被动集成测试改造（含 MSBuild 发现并修复的 4 处测试编译错误）。
- **编译门禁已验证**：MSBuild 2022 编译 `DiceRevolver.Prototype`、`DiceRevolver.Editor`、`DiceRevolver.EditMode.Tests` 三个程序集全部 exit 0（期间修复 `DiceFaceLoadout` 缺失 `using System.Collections.Generic` 与 4 处测试编译错误）。
- **资产迁移已应用**：3 词条 `slotType: 0` + `isPassiveBase: 1`；3 规则 `allowedSlots: 1`；`slotType: 4` 零残留。
- 静态门禁（代码侧）：`DiceFaceSlotMask.Passive` 零引用；`DiceFaceSlotType.Passive` 仅剩迁移工具的 legacy 读取（文档化豁免）。
- **构筑页被动词条无法装备的 bug 修复**（用户反馈）：根因 = `EventRuleDefinition.CollectValidationIssues` 对所有基础槽规则强制"必须提供主弹丸定义"，被动监听规则（呼应协同等，不生成弹丸）`CanEquip(Base)` 失败被拒。修复：主弹丸要求仅对"触发器含基础信号（会随抽面发射）"的规则生效；`DiceBuildEntryButtonUI` 对被动词条显示"被动"标签。新增 2 个测试（被动监听规则可装备；含基础信号规则仍需主弹丸）。

## 当前正在进行

- 等待 EditMode 测试执行（被打开的 Unity 编辑器阻塞：批处理模式因 UPM 单实例锁失败——"Could not establish a connection with the Unity Package Manager local server process"；需用户在 Test Runner 运行或关闭编辑器后批处理）。

## 下一步

1. 用户在 Unity 中刷新并运行聚焦 EditMode 测试（DiceFacePassiveSlotTests、DiceRevolverRuntimeTests、EventRulePassiveIntegrationTests、EventRuleLightningMigrationTests、LightningBuildAssetTests、EventRuleBuiltInModuleTests 等）。
2. 全量回归 + 受保护资产 SHA256 复核。
3. 提交（注意与事件规则编辑器工作流文件的提交拆分）。

## 阻塞

- 无（等待用户规格评审）。

## 涉及文件

- `docs/superpowers/specs/2026-08-23-passive-base-events-design.md`
- `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`、`DiceFaceEntry.cs`、`DiceFaceLoadout.cs`、`DiceRevolverRuntime.cs`、`DiceRevolverGun.cs`、`DiceEventRuleRuntimeSet.cs`、`DiceFaceSlotType.cs`、`EventRuleTypes.cs`
- `Assets/Scripts/Editor/EventRuleEditorWindow.cs`、`EventRuleValidator.cs`、`EventRuleMigrationUtility.cs`、`LightningBuildPrototypeBuilder.cs`
- `Assets/Resources/DiceFacePrototype/DiceFaces/Tesla.asset`、`EchoSynergy.asset`、`Finisher.asset`
- `Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset`、`EchoSynergyRule.asset`、`FinisherRule.asset`
- `Assets/Tests/EditMode/`

## 验证记录

- [not-run] `2026-08-23`：本工作流尚无实现验证。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-passive-base-events-design.md)
