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
- **资产迁移已应用**：词条终态 Tesla=开火时(1)/非被动、EchoSynergy=基础(0)/被动、Finisher=基础(0)/非被动；规则掩码 TeslaRule=OnFire、Echo/FinisherRule=Base；`slotType: 4` 零残留。
- 静态门禁（代码侧）：`DiceFaceSlotMask.Passive` 零引用；`DiceFaceSlotType.Passive` 仅剩迁移工具的 legacy 读取（文档化豁免）。
- **构筑页被动词条无法装备的 bug 修复**（用户反馈）：根因 = `EventRuleDefinition.CollectValidationIssues` 对所有基础槽规则强制"必须提供主弹丸定义"，被动监听规则（呼应协同等，不生成弹丸）`CanEquip(Base)` 失败被拒。修复：主弹丸要求仅对"触发器含基础信号（会随抽面发射）"的规则生效；`DiceBuildEntryButtonUI` 对被动词条显示"被动"标签。新增 2 个测试（被动监听规则可装备；含基础信号规则仍需主弹丸）。
- **词条状态再修正（随状态/遗物工作流）**：`Finisher.asset` `isPassiveBase` 1→0——收尾者由"被动面规则"改为"普通基础事件"（最后抽到 + 发射穿甲弹），面 5 正常入池；`MigratePassiveBaseEntries` 终态调整为：Tesla=开火时非被动、EchoSynergy=基础被动、Finisher=基础非被动；相关测试断言同步。当前仅呼应协同保留被动面语义。

## 当前正在进行

- EditMode 测试已全部执行（编辑器关闭后批处理 + 移除 `-quit` 参数解决测试运行器不启动问题）；聚焦 `DiceFacePassiveSlotTests` 12/12，全量 `388/389`（唯一失败=已批准 Ground 豁免）。修复了被动迁移相关测试首次真实运行暴露的问题（`SignalTypeTriggerModule`/`SignalTypeConditionModule` 缺 `EnemyStatusApplied` 映射、`ForceFaceRejectsPassiveFaces` refill 语义、词条库 11 词条期望等）。

## 下一步

1. 提交收尾（与状态/遗物工作流、事件规则编辑器工作流的文件提交拆分）。
2. PlayMode 人工验收（被动面不占抽池、构筑页被动词条、收尾者最后抽到）。

## 阻塞

- 无硬阻塞。

## 涉及文件

- `docs/superpowers/specs/2026-08-23-passive-base-events-design.md`
- `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`、`DiceFaceEntry.cs`、`DiceFaceLoadout.cs`、`DiceRevolverRuntime.cs`、`DiceRevolverGun.cs`、`DiceEventRuleRuntimeSet.cs`、`DiceFaceSlotType.cs`、`EventRuleTypes.cs`
- `Assets/Scripts/Editor/EventRuleEditorWindow.cs`、`EventRuleValidator.cs`、`EventRuleMigrationUtility.cs`、`LightningBuildPrototypeBuilder.cs`
- `Assets/Resources/DiceFacePrototype/DiceFaces/Tesla.asset`、`EchoSynergy.asset`、`Finisher.asset`
- `Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset`、`EchoSynergyRule.asset`、`FinisherRule.asset`
- `Assets/Tests/EditMode/`

## 验证记录

- [passed] `2026-08-23`：Unity EditMode 聚焦 `DiceFacePassiveSlotTests` 12/12、`0 skipped`。
- [passed] `2026-08-23`：全量 EditMode 回归 `388/389`、`0 skipped`；唯一失败为已批准 Ground `Y=-0.01` 豁免项，无新增失败。被动迁移相关修复：`SignalTypeTriggerModule`/`SignalTypeConditionModule` 补 `EnemyStatusApplied` 信号映射（呼应协同触发器此前永不匹配）；`ForceFaceRejectsPassiveFaces` 改为先抽空池再验证 refill（池满时 refill 拒绝是正确语义）；词条库 10→11 词条期望更新；迁移工具 `AssetDatabase.SaveAssets()` 改 `SaveAssetIfDirty` 遵守定向保存契约。
- [passed] `2026-08-23`：测试后受保护 SHA256 复核（Player/TestRobot/TargetDummy 与基线一致，场景与其余六文件 git 干净）。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-23-passive-base-events-design.md)
