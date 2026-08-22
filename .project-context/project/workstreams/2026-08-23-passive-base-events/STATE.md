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

- brainstorming 全部决策（D1–D6）经用户确认；设计规格已写入 `docs/superpowers/specs/2026-08-23-passive-base-events-design.md`。

## 当前正在进行

- 规格用户评审（等待用户确认规格文件）。

## 下一步

1. 用户评审规格；按反馈修订。
2. 调用 `writing-plans` 技能产出实施计划。
3. 实现与测试。

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
