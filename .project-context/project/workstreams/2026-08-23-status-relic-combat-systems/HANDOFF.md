# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统 — 交接

## 当前状态

- `active`：五系统 T1–T8 + 用户后续需求已实现；呼应协同真实 Gun 链路与右上角骰面 HUD 均已修复并完成 RED→GREEN。当前仍需后续正常 Play Mode 观察与占位参数调优。

## 当前方案及原因

- 通用框架优先（状态框架/遗物框架）；收尾者=普通基础事件（最后抽到+穿甲弹，`isPassiveBase=0`）；特斯拉=开火时按雷电弹幕统计增伤；呼应协同=点燃触发相邻面完整激活；燃烧子弹=命中时施加点燃。
- 构筑页与编辑器同步：词条显示元数据优先解析规则；打开重建；支持"清空"。
- 弹丸立绘：`ProjectileDefinition.ProjectileSprite` 为正式美术挂载点，程序化占位形状兜底。
- 遗物拾取物立绘：`RelicPickup_Face4.prefab` 使用 Unity 内置默认 Sprite 材质；场景可覆盖 Sprite，但不覆盖材质，避免导入图片显示紫色。
- 呼应协同修复：点燃通知保留来源 `DiceFaceActivation`（事件预算/因果链），改写装备面信号时保留 `StatusTarget`，因此状态条件与相邻面奖励激活都能得到完整上下文。
- 骰面 HUD 修复：真实弹仓通过只读剩余面快照通知 UI；UI 不再从 `FireStarted` 推断消耗，因此奖励激活不会错误置灰，延迟启用与手动/自动换弹也会立即呈现真实状态。

## 尚未完成

- 修复后的骰面 HUD、呼应协同（含面 4 链式反应）、特斯拉增伤和点燃表现的正常 Play Mode 验收。
- 根据手感决定是否调整特斯拉 `0.05`、点燃 `3 秒 / 2 dps` 与弹丸占位立绘；未验收前不把这些参数视为正式定稿。

## 下一步首个动作

- 在后续正常 Play Mode 中先观察右上角骰面 HUD：真实抽取实时置灰、奖励激活不置灰、换弹完成立即恢复；随后复验呼应协同、链式反应、特斯拉和点燃表现。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md)

## 必须保护

- 不修改 Player/TestRobot/TargetDummy Prefab、AimRoot、sorting layer、枪械调参。
- 受保护资产 SHA256 不变；规则资产迁移幂等；词条槽位与规则掩码一致（燃烧子弹=命中时、特斯拉=开火时、收尾者=基础非被动、呼应协同=基础被动）。

## 最近验证

- [passed] `2026-08-23`：MSBuild 三程序集 exit 0；静态门禁；GUID 链与资产导入核验；装备失败诊断与迁移测试均通过。
- [passed] `2026-08-23`：Unity EditMode 聚焦 `18/18`；全量 `388/389`、`0 skipped`，唯一失败是已批准的 Ground `Y=-0.01` 豁免项，无新增失败。
- [passed] `2026-08-23`：受保护 Prefab/场景 SHA256 与 Git 状态复核通过。
- [not-run] `2026-08-24`：可见 Play Mode 战斗表现与参数手感验收。
- [passed] `2026-08-24`：`BurningHitTriggersEchoAdjacentFacesThroughTheGunNotificationBoundary` 修复前红灯、修复后绿灯；真实 Gun 链路观察到初始面 4 后追加 `{1,2,4,6}`。Test Runner 当前汇总 `390/393`，其余 3 个既有失败不属于本次呼应修复。
- [passed] `2026-08-24`：`RelicPickup_Face4.prefab` 两个 SpriteRenderer 的内置材质引用修正为 `fileID 10754/type 0`；场景无材质覆盖，Unity Editor 已成功重新导入。
- [passed] `2026-08-24`：隔离 Unity EditMode `DiceRevolverAmmoUITests` 完成 RED `0/3` → GREEN `3/3`、`0 skipped`；Unity 日志 code 0。
- [blocked] `2026-08-24`：主编辑器开启期间的额外 Runtime 批处理回归被 Unity 全局 `CurlRequestCache.db` 冲突中止；未关闭或操作用户主编辑器。

## 风险与不可假定事项

- 相邻触发含面 4（D4/D5）→ 链式反应排队可能造成额外循环，PlayMode 待验收。
- 特斯拉增伤公式（`TeslaDamagePerOrb=0.05`）、点燃参数（3 秒/2 dps）、弹丸立绘占位形状均为占位参数，待调优。
- 规则资产重构后旧模块 SubAsset 残留为孤儿（不影响运行）。
- 受保护 Prefab 未改，但 `ProjectileSpriteVisual` 运行时挂载依赖 spawn 路径（Gun），TestRobot 弹丸同样生效。
