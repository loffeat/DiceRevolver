# 事件配置页面

- ID: `2026-08-21-event-rule-editor`
- Status: `active`
- Branch: `codex/event-rule-editor`
- Created: `2026-08-21`
- Updated: `2026-08-21`

## 目标

- 推进 Unity 编辑器内的事件配置页面。
- 按基础、开火时、命中时、开火后和被动类型集中展示所有事件规则。
- 允许设计者配置触发器、规则条件、有序结果、结果局部条件及模块公开参数。
- 保持事件运行时低耦合，单条规则或模块故障不得影响角色 3C 和其他低关联系统。

## 已确认事实

- 用户已批准规则列表方案，第一版不采用任意节点图。
- 页面采用 Unity Editor 风格的三栏布局：左侧分类和筛选、中间规则列表、右侧完整配置与 Play Mode 调试记录。
- 一个规则由一个触发器、按 AND 判定的规则级条件、按顺序执行的结果列表组成；每个结果可拥有独立局部条件。
- 规则主资产使用 `EventRuleDefinition` ScriptableObject，模块作为 SubAsset 保存。
- 模块类型通过 `TypeCache.GetTypesDerivedFrom` 自动发现，规则资产通过 `AssetDatabase.FindAssets` 自动发现。
- 运行时可变状态必须由每把 Gun、每个装备面的 Runtime 实例持有，ScriptableObject 资产保持只读。
- 新规则系统先与 `BulletEventEffect`、`PassiveEventEffect` 兼容并存，再逐项迁移既有事件。
- 编辑器只提示校验问题，不擅自覆盖设计者填写的数据。

## 待办

1. 按 Subagent-Driven Development 执行已完成的实施计划。
2. 建立 Rule、Trigger、Condition、Result 数据接口和纯 C# 执行器。
3. 建立三栏 Unity Editor 页面、模块自动发现、SubAsset 编辑和即时校验。
4. 增加旧 Effect 兼容适配器。
5. 依次迁移基础射击、DoubleTap、BlastRound、LoadedFour。
6. 迁移带状态的雷电被动与连锁事件。
7. 补齐运行时、Editor、迁移和保护性测试。

## 当前正在进行

- 已在隔离工作树 `codex/event-rule-editor` 启动 Subagent-Driven Development；实施前 Unity 基线已完成，随后实施 Task 1。

## 下一步

1. 从 Task 1 纯 C# 规则数据模型、信号、状态和受限服务上下文开始测试驱动实施。
2. 每个任务完成实现、规格与质量审查后再推进下一任务。

## 已完成

- 完成并批准规则列表事件编辑器设计稿。
- 确认 Unity Editor 深色三栏页面的可视化方向和主要交互层级。
- 战斗事件因果 Debug 已可作为编辑器 Play Mode 调试区的数据来源。
- 完成 10 个独立验收任务、69 个细分步骤的测试先行实施计划，并明确受保护 Prefab、旧 Effect 兼容和 Ground Y 豁免边界。
- 用户选择 Subagent-Driven Development；已建立 `codex/event-rule-editor` 隔离工作树并通过上下文检查。

## 阻塞

- 无。

## 非目标

- 第一版不实现任意节点连线、循环节点、脚本表达式或运行时代码生成。
- 不允许配置模块直接查找或任意修改场景对象。
- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer 或 DiceRevolverGun 调参数值。

## 涉及文件

- `docs/superpowers/specs/2026-08-21-event-rule-editor-design.md`
- `docs/superpowers/plans/2026-08-21-event-rule-editor.md`
- 计划新增的 `Assets/Scripts/Prototype/` 规则运行时文件。
- 计划新增的 `Assets/Scripts/Editor/` 事件规则编辑器文件。
- 计划新增的 `Assets/Tests/EditMode/` 规则运行时与编辑器测试。
- `.project-context/project/`

## 相关资料

- [正式设计稿](../../../../docs/superpowers/specs/2026-08-21-event-rule-editor-design.md)
- [项目地图](../../PROJECT.md)
- [项目状态](../../STATUS.md)
- [战斗事件因果 Debug](../2026-08-21-combat-debug-trace/STATE.md)

## 验证记录

- [passed] `2026-08-21`：用户确认三栏事件配置页面的可视化方向。
- [passed] `2026-08-21`：实施计划完成规格覆盖、占位符和跨任务接口一致性自审。
- [passed] `2026-08-21`：隔离工作树上下文检查输出 `[context:ok]`。
- [failed] `2026-08-21`：实施前完整 EditMode `251/252`、`0 skipped`；唯一失败为 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`，即已批准的 Ground `Y=-0.01` 例外。
