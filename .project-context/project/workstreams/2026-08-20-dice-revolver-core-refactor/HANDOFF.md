# 左轮核心底层重构交接

## 当前状态

- 自动化结构重构已完成：固定六面 Runtime、四阶段 Pipeline、Gun Unity 适配器和 Projectile 命中广播路径均已落地。
- 终审修复后分层聚焦 EditMode `58/58`；完整 EditMode `171/172`，唯一失败为用户明确豁免的 Ground Y 既有契约差异，没有新增失败。
- 可见 PlayMode 战斗流程和手感仍为 `[not-run]`，不得声称已人工验收。

## 尚未完成

- 人工确认玩家与机器人六发不重复射击、空膛换弹、手动换弹、DoubleTap 延迟、BlastRound 直击加爆炸、LoadedFour 下一发固定为 4。
- 雷电构筑仍是独立 planned 工作流，没有在本次重构中实现。

## 当前方案及原因

- `DiceRevolverRuntime` 管理左轮机械状态，`DiceShotPipeline` 管理骰面事件生命周期，`DiceRevolverGun` 只承担 Unity 适配职责。
- 固定六面使用统一规则常量，不保留虚假配置能力。
- Projectile 自己广播命中，Pipeline 处理合格 OnHit，Projectile 随后提交直接伤害。
- 事件预算由每把枪配置，并在开火时固化到激活上下文。

## 下一步首个动作

- 读取[雷电构筑待办](../2026-08-20-lightning-build-backlog/STATE.md)，提醒用户该工作流仍为 `planned`，并在实现前确认其中列出的设计边界。

## 必须保护

- 不修改 Ground `-0.01` 或其既有失败测试；这是用户明确豁免项。
- 继续保护六面不放回、事件阶段顺序、LoadedFour 自动换弹时机、命中顺序、事件预算与异常隔离。
- 不把 `[not-run]` 的 PlayMode 人工验收表述为已通过。

## 风险与不可假定事项

- 完整回归不是全绿：真实状态必须保持 `[failed] 171/172`，唯一失败为 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`。
- 雷电构筑的抽面约束、临时快照、元素反应和重复触发语义尚需设计确认。

## 最近验证

- [failed] 抽面边界 mutation `0/1`，按预期捕获 `0..5` 错误集合；恢复后 `1/1` 通过。
- [failed] AmmoFace 规则源 mutation `0/1`，按预期捕获旧 HUD 上界 `6` 与临时规则源 `7` 的分叉；恢复固定六面并修复后相关测试通过。
- [passed] 终审核心分层聚焦 EditMode `58/58`，0 失败、0 跳过。
- [failed] 完整 EditMode `171/172`，0 跳过；唯一失败为已知 Ground Y `-0.01` 豁免项，没有新增失败。
- [not-run] 可见 PlayMode 战斗流程与手感人工验收。

## 首先读取

- [雷电构筑待办](../2026-08-20-lightning-build-backlog/STATE.md)
- [项目状态](../../STATUS.md)
- [本工作流状态](STATE.md)
