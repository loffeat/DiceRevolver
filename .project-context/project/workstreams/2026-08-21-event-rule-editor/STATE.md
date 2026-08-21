# 事件配置页面

- ID: `2026-08-21-event-rule-editor`
- Status: `completed`
- Branch: `codex/event-rule-editor`
- Created: `2026-08-21`
- Updated: `2026-08-21`

## 目标

- 推进 Unity 编辑器内的事件配置页面。
- 按基础、开火时、命中时、开火后和被动类型集中展示所有事件规则。
- 允许设计者配置触发器、规则条件、有序结果、结果局部条件及模块公开参数。
- 保持事件运行时低耦合，单条规则或模块故障不得影响角色 3C 和其他低关联系统。

## 非目标

- 第一版不实现任意节点连线、循环节点、脚本表达式或运行时代码生成。
- 不允许配置模块直接查找或任意修改场景对象。
- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer 或 DiceRevolverGun 调参数值。

## 已确认事实

- 用户已批准规则列表方案，第一版不采用任意节点图。
- 页面采用 Unity Editor 风格的三栏布局：左侧分类和筛选、中间规则列表、右侧完整配置与 Play Mode 调试记录。
- 一个规则由一个触发器、按 AND 判定的规则级条件、按顺序执行的结果列表组成；每个结果可拥有独立局部条件。
- 规则主资产使用 `EventRuleDefinition` ScriptableObject，模块作为 SubAsset 保存。
- 模块类型通过 `TypeCache.GetTypesDerivedFrom` 自动发现，规则资产通过 `AssetDatabase.FindAssets` 自动发现。
- 运行时可变状态由每把 Gun、每个装备面的 Runtime 实例持有，ScriptableObject 资产保持只读。
- 编辑器只提示校验问题，不擅自覆盖设计者填写的数据。

## 已完成

- Task 1–10 自动化全部完成：规则数据模型/执行器/预算/异常隔离、活动与被动兼容路径、内置模块、三栏 EditorWindow、SubAsset 工具与校验器、十个骰面词条迁移为 Rule-backed、旧 Effect 兼容边界与清理。
- 八个具体 Effect（Core 三个 + 雷电五个）均已按类型名与 GUID 零引用门禁删除；抽象 `BulletEventEffect`、`PassiveEventEffect`、隐藏 legacy 序列化字段与只读回退保留。
- `ProjectileSpawnEffect` 与 FireBasic/FireTestRobot/FireLightningOrb 三份 spawn 资产按受保护 Player/TestRobot `baseEffects` 兼容边界保留。
- `LightningBuildPrototypeBuilder` 收敛为只创建缺失规则/SubAsset 并保留既有非空参数，不再依赖已删除的具体 Effect。
- 用户选择 Subagent-Driven Development；实现全部在 `codex/event-rule-editor` 隔离工作树完成并已合并回 `main`。

## 当前正在进行

- 无

## 下一步

1. 在可见 Unity 中打开 `Window > Dice Revolver > 事件规则编辑器`，验收三栏布局、筛选、新建/复制/重命名、模块菜单、排序、Undo/Redo、校验信息与 Play Mode 详细 Debug。
2. 进入 Play Mode 验证十个 Rule-backed 词条（核心四件 + 雷电六件）与既有战斗表现一致。
3. 验收后将工作流正式收尾（如无需修复，可标记人工验收完成）。

## 阻塞

- 无。

## 涉及文件

- `docs/superpowers/specs/2026-08-21-event-rule-editor-design.md`
- `docs/superpowers/plans/2026-08-21-event-rule-editor.md`
- `Assets/Scripts/Prototype/EventRule*.cs`、`EventSignal.cs`、`DiceEventRuleRuntimeSet.cs`、`BulletEventRuleServices.cs`、`PassiveEventRuleServices.cs`、模块文件、`LightningChainTargetSelector.cs`
- `Assets/Scripts/Editor/EventRuleEditorWindow.cs`、`EventRuleModuleCatalog.cs`、`EventRuleValidator.cs`、`EventRuleAssetUtility.cs`、`EventRuleMigrationUtility.cs`、`LightningBuildPrototypeBuilder.cs`、`DiceFacePrototypeAssetBuilder.cs`
- `Assets/Tests/EditMode/EventRule*Tests.cs` 及各联合回归测试
- `Assets/Resources/DiceFacePrototype/EventRules/`（Core 四件 + Lightning 六件 Rule 资产）
- `.project-context/project/`

## 验证记录

- [passed] `2026-08-21`：Task 9 fix 聚焦 `EventRuleLightningMigrationTests` `12/12`、`0 skipped`。
- [passed] `2026-08-21`：Task 9 fix 十套联合 EditMode `43/43`、`0 skipped`。
- [failed] `2026-08-21`：清理前完整 EditMode `374/373`；唯一失败为已批准 Ground `Y=-0.01` 豁免项。
- [failed] `2026-08-21`：雷电五 Effect 清理后完整 EditMode `351/350`、`0 skipped`；唯一失败仍为 Ground `Y=-0.01` 豁免项，无新增失败。
- [passed] `2026-08-21`：五个雷电 Effect 类型名与五个资产 GUID 在 `Assets` 零引用；`BulletEventLibrary` 修剪为三个 spawn 效果；十个骰面词条均 Rule-backed。
- [passed] `2026-08-21`：十个受保护文件（Player、TestRobot、TargetDummy、场景、三个基础弹丸 Prefab、fire_1、BlastExplosion、LightningOrb、LightningChain）SHA256 与清理前完全一致。
- [not-run] `2026-08-21`：三栏编辑器页面排版/交互与 Play Mode 战斗表现的人工验收。

## 相关资料

- [正式设计稿](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-event-rule-editor.md)
- [项目地图](../../PROJECT.md)
- [项目状态](../../STATUS.md)
- [战斗事件因果 Debug](../2026-08-21-combat-debug-trace/STATE.md)
