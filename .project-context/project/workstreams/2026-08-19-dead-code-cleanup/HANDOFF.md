# 死代码清理与碰撞过滤收敛交接

## 当前状态

- 实现和自动化验证均已完成。

## 尚未完成

- 自动化范围内无。

## 当前方案及原因

- 只删除有零引用证据的旧枪械脚本。
- `Projectile` 作为实际碰撞处理者拥有统一过滤规则，Reporter 只转发有效命中。
- 保留序列化兼容字段和仍在词条库中的四个词条。

## 下一步首个动作

- 如未来允许修改 `DiceRevolverGun`，先设计由 `Projectile` 直接广播命中的迁移，再移除 Reporter。

## 必须保护

- 不修改 `DiceRevolverGun`、Player Prefab、TestRobot Prefab 或场景资源。

## 风险与不可假定事项

- `ProjectileHitReporter` 仍因现有枪械接线而保留；完全合并该组件需要未来修改 `DiceRevolverGun`。

## 最近验证

- [passed] 基线 `123/123`。
- [failed] 共享碰撞接口缺失红灯符合预期。
- [passed] 聚焦测试 `3/3`。
- [passed] 完整 EditMode 回归 `126/126`；`DiceRevolverGun.cs` 无差异。

## 首先读取

- [工作流状态](STATE.md)
