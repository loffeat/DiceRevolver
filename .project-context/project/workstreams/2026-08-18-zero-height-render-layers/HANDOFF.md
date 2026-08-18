# 零高度与渲染层级契约交接

## 当前状态

- 实现与自动化验证已完成；磁盘场景使用 SpriteRenderer Ground，玩家、测试靶与弹丸统一在 `Y=0`。

## 当前方案及原因

- 用无重力 `CharacterController.Move` 维持纯平面 3C 移动，避免删除 Ground Collider 后玩家下坠。
- 在左轮的唯一弹丸生成出口统一归零高度，使基础弹、延迟附加弹和后续模块化弹丸共享同一契约。
- 由 `ProjectileVisualWrapper` 只管理自身粒子 renderer 的 sorting，避免弹丸表现反向耦合 AimHandRig、角色或 3C。
- Ground 使用独立 Full Rect Sprite 平铺，避免修改原地图图集的 Tight Mesh 导入设置。

## 尚未完成

- 当前可见 Play Mode 中的地面平铺尺寸、透明遮挡与最终美术观感尚未人工验收。

## 下一步首个动作

- 在 Unity 主窗口退出 Play Mode并重新打开 `Assets/Scenes/TopDownShooterPrototype.unity`，再进入 Play Mode 做一次视觉检查。

## 首先读取

- [工作流状态](STATE.md)
- [项目状态](../../STATUS.md)
- [基础射击可视交接](../2026-08-18-base-shot-reload-visual-fix/HANDOFF.md)

## 必须保护

- 不运行完整 `TopDownPrototypeSceneBuilder`。
- 不修改 Body、AimRoot、ArmVisual、GunBody、Muzzle 的用户局部 Transform 或 sorting。
- 不修改 `DiceRevolverGun` 的用户序列化调参数值。
- 不让 3C 或 AimHandRig 接管弹丸包装 sorting；每种包装只管理自己的 renderer。
- 工作区中的 Player、TargetDummy、Recovery 和既有上下文改动来自用户或前置工作，不回退。

## 最近验证

- [passed] 聚焦红测实现前 `0/4`，实现后 `4/4`。
- [passed] 完整 EditMode `102/102`。
- [passed] 最终场景渲染契约测试通过。
- [passed] Player 与 TargetDummy Prefab 哈希未改变。
- [not-run] 当前可见 Play Mode 人工视觉验收。

## 风险与不可假定事项

- Unity 主窗口在改动期间处于打开状态，并曾加载 Play Mode 备份场景；磁盘文件已经更新，但人工验收前应重新打开场景，避免继续观察旧内存场景。
- Ground 已移除 Collider；这是与无重力平面移动、`Y=0` 弹丸并用的设计，不能只恢复 Collider 而不重新设计碰撞层。
- 自动化测试能验证结构、图层和高度，不能替代最终屏幕上的透明粒子观感检查。
