# 事件规则编辑器：类型更换与中文可编辑字段 — 交接

## 当前状态

- `active`：代码、编译与 Unity EditMode 自动化验证均已完成；仅余可见编辑器窗口人工验收。

## 当前方案及原因

- 事件的"类型"由触发器模块决定：`EventRuleDefinition.trigger`。窗口新增"更换类型"按钮，经 `EventRuleAssetUtility.ReplaceTrigger` 在 Undo 分组内创建新触发器 SubAsset、清理解除引用的旧触发器闭包；选择与当前相同类型时 no-op，保留已有配置。
- 中文名使用 `UnityEngine.InspectorName` 属性：规则元数据字段、`EventResultEntry` 字段、`DiceFaceSlotMask`/`EventSignalMask`/`EventSignalType`/`EventRuleRecursionPolicy`/`CounterComparisonOperator` 枚举成员；窗口分区标题与模块标题（改用 `EventRuleModuleMenuAttribute.Path` 中文路径）同步本地化。
- `InspectorName` 只改显示名，不改变序列化数据与运行行为，既有规则资产零迁移成本。

## 尚未完成

- 可见编辑器窗口人工验收：所有事件按钮、更换类型按钮、中文元数据字段和中文枚举选项。

## 下一步首个动作

- 在 Unity 中打开 `Window > Dice Revolver > 事件规则编辑器`，依次验收所有事件筛选、触发器更换和中文显示。

## 首先读取

- [工作流状态](STATE.md)
- [事件配置页面工作流](../2026-08-21-event-rule-editor/STATE.md)
- [项目状态](../../STATUS.md)

## 必须保护

- 不覆盖用户在 Player、TestRobot、TargetDummy、AimRoot、Renderer 和 DiceRevolverGun Inspector 中填写的数据。
- 不运行 `TopDownPrototypeSceneBuilder`；不自动修改 sorting layer、Transform、枪械参数或美术尺寸。
- 不改变既有规则资产的序列化字段名与枚举数值（`InspectorName` 仅显示层）。

## 最近验证

- [passed] `2026-08-23`：CS0266/CS1503 修复后 Unity 编译 `Tundra build success`，无错误。
- [passed] `2026-08-23`：相关代码纳入 Unity EditMode 全量回归 `388/389`、`0 skipped`；唯一失败是已批准的 Ground `Y=-0.01` 豁免项，无新增失败。
- [not-run] `2026-08-22`：可见编辑器窗口人工验收。

## 风险与不可假定事项

- 不可把 ScriptableObject 当作运行时状态容器。
- 不得让规则模块直接依赖 Player、场景单例或反射调用。
- `InspectorName` 在 `[Flags]` 枚举弹窗上的显示依赖 Unity 版本行为；若掩码弹窗未显示中文，只影响显示，不影响功能。
