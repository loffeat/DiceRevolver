# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统 — 交接

## 当前状态

- `active`：五系统 T1–T8 + 用户后续需求全部实现，**编译门禁通过（MSBuild 三程序集 exit 0）**、静态门禁/资产/GUID 核验通过；等待 Unity EditMode 测试执行与提交收尾。

## 当前方案及原因

- 通用框架优先（状态框架/遗物框架）；收尾者=普通基础事件（最后抽到+穿甲弹，`isPassiveBase=0`）；特斯拉=开火时按雷电弹幕统计增伤；呼应协同=点燃触发相邻面完整激活；燃烧子弹=命中时施加点燃。
- 构筑页与编辑器同步：词条显示元数据优先解析规则；打开重建；支持"清空"。
- 弹丸立绘：`ProjectileDefinition.ProjectileSprite` 为正式美术挂载点，程序化占位形状兜底。

## 尚未完成

- Unity EditMode 聚焦测试 + 全量回归（编辑器 UPM 单实例限制批处理；需用户 Test Runner 或关闭编辑器）。
- 受保护 SHA256 复核、提交（多工作流文件拆分）。

## 下一步首个动作

- 用户在 Unity 刷新后运行聚焦 EditMode 测试（清单见 STATE.md）；或临时关闭编辑器由我跑隔离副本批处理。

## 首先读取

- [工作流状态](STATE.md)
- [设计规格](../../../../docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-23-status-relic-combat-systems.md)

## 必须保护

- 不修改 Player/TestRobot/TargetDummy Prefab、AimRoot、sorting layer、枪械调参。
- 受保护资产 SHA256 不变；规则资产迁移幂等；词条槽位与规则掩码一致（燃烧子弹=命中时、特斯拉=开火时、收尾者=基础非被动、呼应协同=基础被动）。

## 最近验证

- [passed] `2026-08-23`：MSBuild 三程序集 exit 0；静态门禁；GUID 链与资产导入核验；装备失败诊断（燃烧子弹 slotType 修复后 `BurningBulletEntryEquipsIntoOnHitSlot` 复现测试可跑）。
- [not-run] `2026-08-23`：Unity EditMode 测试执行。

## 风险与不可假定事项

- 相邻触发含面 4（D4/D5）→ 链式反应排队可能造成额外循环，PlayMode 待验收。
- 特斯拉增伤公式（`TeslaDamagePerOrb=0.05`）、点燃参数（3 秒/2 dps）、弹丸立绘占位形状均为占位参数，待调优。
- 规则资产重构后旧模块 SubAsset 残留为孤儿（不影响运行）。
- 受保护 Prefab 未改，但 `ProjectileSpriteVisual` 运行时挂载依赖 spawn 路径（Gun），TestRobot 弹丸同样生效。
