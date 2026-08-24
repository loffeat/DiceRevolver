# 事件规则编辑器：类型更换与中文可编辑字段

- ID: `2026-08-22-event-rule-editor-type-switch-i18n`
- Status: `active`
- Branch: `main`
- Created: `2026-08-22`
- Updated: `2026-08-24`

## 目标

- 允许在事件规则编辑器内直接更换规则的触发器类型（事件类型），无需先移除再添加。
- 为编辑器窗口中仍显示英文的可编辑数据补齐中文名称：规则元数据字段、枚举弹窗选项、模块标题与窗口分区标题。

## 非目标

- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer 或 DiceRevolverGun 调参数值。
- 不改变既有规则资产的序列化数据与运行行为；`InspectorName` 只影响显示名。
- 不实现任意节点连线或新的触发器模块类型。

## 已确认事实

- 模块字段已普遍使用 `[InspectorName]` 中文名；规则元数据字段（`displayName`、`description`、`displayColor`、`tags`、`rarity`、`allowedSlots`、`eventBudgetCost`、`recursionPolicy`）与 `CounterComparisonOperator` 枚举值仍是英文。
- `DiceFaceSlotMask`、`EventSignalMask` 为 `[Flags]` 枚举；`EventSignalType`、`EventRuleRecursionPolicy`、`CounterComparisonOperator` 为普通枚举，弹窗选项显示英文。
- 触发器由 `EventRuleDefinition.trigger` 单引用持有；窗口目前只提供"添加 Trigger"（为空时）与"移除 Trigger"。
- 资产操作统一经 `EventRuleAssetUtility` 的 Undo 分组、SubAsset 创建与引用清理边界，测试集中在 `EventRuleEditorWindowTests`。

## 已完成

- 建立本工作流记录。
- `EventRuleDefinition` 与 `EventResultEntry` 字段补中文 `InspectorName`。
- `DiceFaceSlotMask`、`EventSignalMask`、`EventSignalType`、`EventRuleRecursionPolicy`、`CounterComparisonOperator` 枚举成员补中文名。
- `EventRuleAssetUtility.ReplaceTrigger`：同一类型 no-op，异类型创建新 SubAsset、清理解除引用的旧触发器闭包并合并 Undo。
- `EventRuleEditorWindow`：新增"更换类型"入口，模块标题显示中文菜单路径，分区标题本地化。
- `EventRuleEditorWindowTests`：新增触发器更换与同类型 no-op 测试。
- 按用户要求将 `allowedSlots` 字段改名为"事件类型"，区块标题改为"基础信息与事件类型"；触发器区块标题去重为"触发器"。
- 编译错误修复：`ReplaceTrigger` 同类型 no-op 返回值补 `(ScriptableObject)` 显式转换；测试断言改用 LINQ `Contains` 替代 `Does.Not.Contain`。
- 事件类型分类调整（用户确认方案 A）：`DiceFaceSlotMask` 成员标签改为事件语义（无/基础事件/开火时事件/命中时事件/开火后事件/所有事件），删除复合项 `Active`（零引用）；左栏"事件类型"新增"所有事件"按钮（`EventRuleEditorSelection.showAllEvents`，默认 true=显示全部规则）；"仅显示错误规则"与实时校验在"所有事件"模式下用规则自身允许的第一个槽位作为校验上下文（`ResolveValidationSlot`）。

## 当前正在进行

- 代码已编译并纳入 Unity EditMode 全量回归；等待可见编辑器窗口人工验收。

## 下一步

1. 在可见 Unity 中打开 `Window > Dice Revolver > 事件规则编辑器` 验收：所有事件按钮、触发器更换、事件语义分类与中文标签。
2. 人工验收无问题后将本工作流标记为 `completed`。

## 阻塞

- 无（测试执行同被动迁移工作流一样受打开的编辑器 UPM 单实例限制，可经 Test Runner 运行）。

## 涉及文件

- `Assets/Scripts/Prototype/EventRuleDefinition.cs`
- `Assets/Scripts/Prototype/EventRuleTypes.cs`
- `Assets/Scripts/Prototype/CounterComparisonOperator.cs`
- `Assets/Scripts/Editor/EventRuleAssetUtility.cs`
- `Assets/Scripts/Editor/EventRuleEditorWindow.cs`
- `Assets/Tests/EditMode/EventRuleEditorWindowTests.cs`
- `.project-context/project/`

## 验证记录

- [passed] `2026-08-23`：CS0266 与 CS1503 修复后 Unity 编译 `Tundra build success`（`2.35 seconds`，`888 evaluated`），无错误。
- [passed] `2026-08-23`：Unity 编译 build 3394 成功（含事件类型改名与被动迁移代码）。
- [passed] `2026-08-23`：MSBuild 2022 编译 Prototype/Editor/EditMode.Tests 三个程序集 exit 0（含"所有事件"分类改动，期间修复新测试的 `ConfigureMetadata` tag 参数遗漏）。
- [passed] `2026-08-23`：相关代码纳入 Unity EditMode 全量回归 `388/389`、`0 skipped`；唯一失败是已批准的 Ground `Y=-0.01` 豁免项，无新增失败。
- [not-run] `2026-08-22`：可见编辑器窗口人工验收。

## 相关资料

- [事件配置页面工作流](../2026-08-21-event-rule-editor/STATE.md)
- [事件规则列表与配置页设计](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
