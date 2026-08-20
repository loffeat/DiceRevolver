# 战斗事件因果 Debug

- ID: `2026-08-21-combat-debug-trace`
- Status: `completed`
- Branch: `codex/combat-debug-trace`
- Created: `2026-08-21`
- Updated: `2026-08-21`

## 目标

- 在屏幕左上角按真实执行顺序和因果层级显示玩家枪械触发的事件。
- 保持追踪、事件运行时和 UI 低耦合，不影响角色 3C、Prefab 或既有调参。

## 已确认事实

- 用户批准使用每枪独立追踪器、递增序号、因果缩进、延迟实际执行时记录、仅记录真实被动行为的方案。
- Debug 功能完成后，再编写规则列表事件配置系统方案；本工作流不实施规则列表重构。
- 相关基线 EditMode 为 `42/42`。

## 当前正在进行

- 无

## 已完成

- 读取项目上下文与当前事件管线。
- 建立隔离工作树并完成相关测试基线。
- 增加每枪独立 `CombatDebugTrace`，按真实执行顺序分配编号并保存父子激活关系。
- 活动事件、命中、弹丸生成、延迟安排/执行、闪电链、骰面检索、覆盖、奖励射击和换弹均可发布结构化记录。
- 呼应协同奖励射击携带来源激活并显示为子因果链。
- 特斯拉只在雷电弹丸实际增加层数时记录；收尾者只在确实把绑定面排除出当前抽取池时记录。
- 运行时自动在玩家 HUD 左上角创建 Debug 面板，只订阅玩家枪械。
- 增加可持久编辑的 `CombatDebugSettings.asset`，开放启用、行数、停留时间、字号和面板尺寸。
- 完成规则列表事件编辑器正式设计方案，本工作流不实施该重构。

## 下一步

1. 在可见 PlayMode 中观察长文本换行、事件密度和因果缩进。
2. 用户批准规则列表设计后，再创建独立实施工作流。

## 阻塞

- 无。

## 尚未完成

- 可见 PlayMode 人工视觉验收。
- 规则列表系统尚未实施。

## 非目标

- 不修改角色、枪械、AimRoot、sorting layer、场景或现有骰面装备。
- 不让 Debug UI 参与事件执行或改变事件结果。
- 本工作流不实施规则列表事件系统。

## 涉及文件

- `Assets/Scripts/Prototype/`
- `Assets/Tests/EditMode/`
- `Assets/Resources/DiceFacePrototype/CombatDebugSettings.asset`
- `.project-context/project/`
- `docs/superpowers/specs/2026-08-21-event-rule-editor-design.md`

## 验证记录

- [passed] `2026-08-21`：事件管线、枪械、被动和 UI 相关基线 EditMode `42/42`。
- [passed] `2026-08-21`：Debug 核心、因果链和 UI 测试 `9/9`。
- [passed] `2026-08-21`：Debug 与相关事件管线聚焦回归 `107/107`。
- [passed] `2026-08-21`：特斯拉、收尾者与 Debug 联合测试 `20/20`。
- [failed] `2026-08-21`：完整 EditMode `251/252`；唯一失败为既有 Ground `Y=-0.01` 契约差异，没有新增失败。
- [not-run] `2026-08-21`：可见 PlayMode 左上角排版与事件密度人工验收。

## 相关资料

- [项目地图](../../PROJECT.md)
- [项目状态](../../STATUS.md)
- [雷电构筑系统](../2026-08-20-lightning-build-backlog/STATE.md)
