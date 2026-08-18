# 基础射击可视与换弹手臂修复交接

## 当前状态

- 修复已完成；基础射击完整逻辑链、粒子 Shader 兼容和换弹视觉均有回归测试，完整 EditMode 为 `98/98`。

## 当前方案及原因

- 把兼容逻辑限制在 `ProjectileVisualWrapper`，使弹丸视觉资源故障不会影响骰面、左轮或 3C 系统。
- 只替换错误 Shader 并复用原纹理和颜色，保留 `fire_1` 原资产与正常材质的未来兼容性。
- 换弹系统只控制 `ArmVisual` 的颜色，避免动画覆盖用户维护的 Transform。

## 尚未完成

- 修复后的弹丸尺寸、颜色、朝向和运动观感尚未在当前可见 Play Mode 中人工确认。

## 关键结论

- 基础事件没有失效。实际故障是 `fire_1` 材质引用的 Shader 资源缺失，逻辑弹丸已经生成但不能正确显示。
- `ProjectileVisualWrapper` 只在材质为空、Shader 为空或 Shader 为 `Hidden/InternalErrorShader` 时使用兼容材质；若以后补回原 Shader，不会强制覆盖正常材质。
- 换弹系统不再写入 `ArmVisual` Transform；`reloadDropDistance` 的既有序列化值保留但不参与当前只闪烁方案。

## 必须保护

- 不运行完整 `TopDownPrototypeSceneBuilder`。
- 不修改 Body、AimRoot、ArmVisual、GunBody、Muzzle 的用户 Transform 或 sorting。
- 不修改 `DiceRevolverGun` 的射速、换弹时间、闪烁速度、颜色等用户序列化数值。
- 不直接修改 `fire_1.prefab`、其材质或粒子参数。

## 首先读取

- [工作流状态](STATE.md)
- [项目状态](../../STATUS.md)
- [前置弹丸事件管线交接](../2026-08-18-projectile-definition-event-pipeline/HANDOFF.md)

## 最近验证

- [passed] 聚焦测试 `16/16`。
- [passed] 完整 EditMode `98/98`。
- [passed] 主 Unity 会话导入日志无 C# 或 Shader 错误。
- [not-run] 当前可见 Play Mode 人工视觉验收。

## 下一步首个动作

- 直接在已打开的 `TopDownShooterPrototype` 场景进入 Play Mode，左键射击并打空弹巢，分别观察弹丸可见性与换弹闪烁。

## 风险与不可假定事项

- EditMode 已证明逻辑弹丸生成和 Shader 替换正确，但不等同于最终屏幕视觉已人工验收。
- `reloadDropDistance` 仍保留旧序列化值以避免覆盖用户数据，但当前只闪烁方案不会读取它。
- 工作区中的 Player、TargetDummy 和 Recovery 变更不是本工作流创建，不应回退或覆盖。
