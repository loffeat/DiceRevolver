# 手臂可见性与基础弹丸尺寸修复交接

## 当前状态

- 修复与自动化验证已完成；手臂不会再被运行时放到 Ground 以下，基础弹丸包装缩放为 `0.4`。

## 当前方案及原因

- 根因不是 Sprite、Renderer、颜色或 sorting 丢失，而是玩家根节点归零后，原 `visualHeight=-0.58` 使 AimRoot 世界高度落到地面以下。
- 在运行时只限制 AimRoot 的最低世界渲染高度，可以保护零高度玩法平面，同时不改用户维护的 Prefab 子物体数据。
- 弹丸尺寸继续由独立 `ProjectileVisualWrapper` Prefab 数据控制，不耦合左轮、骰面或事件系统。

## 尚未完成

- 当前可见 Play Mode 中左右瞄准的最终手臂对齐和放大弹丸观感尚未人工验收。

## 下一步首个动作

- 重新进入 `TopDownShooterPrototype` Play Mode，向左右两侧瞄准并发射基础弹丸做视觉确认。

## 首先读取

- [工作流状态](STATE.md)
- [项目状态](../../STATUS.md)
- [零高度渲染交接](../2026-08-18-zero-height-render-layers/HANDOFF.md)

## 必须保护

- 不修改 Player Prefab 中 AimRoot、ArmVisual、GunBody、Muzzle 的局部 Transform 和 sorting。
- 不修改现有左轮调参数值。
- 不用重新打包或重建 Player Prefab 来处理该问题。

## 最近验证

- [passed] 聚焦测试 `2/2`。
- [passed] 完整 EditMode `104/104`。
- [passed] Player Prefab 哈希未改变。
- [not-run] 当前可见 Play Mode 人工视觉验收。

## 风险与不可假定事项

- 自动化测试确认手臂渲染中心位于 Ground 上方，但不能替代最终屏幕位置的人工美术检查。
- `visualHeight` 原值仍保留；只有其计算结果低于玩法平面时才被最低高度保护覆盖。
- 基础弹丸只放大视觉实例，Collider 与逻辑数据没有同步放大。
