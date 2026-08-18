# 零高度与渲染层级契约

- ID: `2026-08-18-zero-height-render-layers`
- Status: `completed`
- Branch: working tree
- Created: `2026-08-18`
- Updated: `2026-08-18`

## 目标

- 让玩家、测试靶、弹丸和地面以世界 `Y=0` 作为统一玩法平面。
- 把场景 Ground 从 Plane Mesh 改为 `SpriteRenderer`，使用 `Background` Sorting Layer。
- 让基础弹丸的粒子包装固定使用 `projectile` Sorting Layer，降低透明包装的深度闪烁。

## 非目标

- 不调整 Body、AimRoot、ArmVisual、GunBody、Muzzle 的局部 Transform 或现有 sorting 设置。
- 不修改 `DiceRevolverGun` 已有射速、换弹、闪烁或其他用户调参数值。
- 不重建 Player 或 TargetDummy Prefab，不运行完整 `TopDownPrototypeSceneBuilder`。

## 已确认事实

- 原玩家移动使用 `CharacterController.SimpleMove`，会引入重力，场景 Player 根节点另有 `Y=1` 覆盖。
- 原弹丸按枪口传入高度直接生成，没有统一玩法平面约束。
- 原 Ground 是带 MeshCollider 的 Plane；若弹丸统一生成在 `Y=0`，地面 Collider 会与弹丸重叠。
- 原基础弹丸粒子使用 `Default` Sorting Layer。
- 原 Player 和 TargetDummy 的 SpriteRenderer 已使用非 Default 的 Character/Gun 图层，不需要重写。

## 当前正在进行

- 无

## 已完成

- 玩家移动改为无重力的平面 `CharacterController.Move`，Awake 和每帧移动后都把根节点收敛到 `Y=0`。
- 所有经 `DiceRevolverGun` 生成的弹丸统一把出生点约束到 `Y=0`。
- `ProjectileVisualWrapper` 新增中文 Inspector 端口，只管理所生成粒子包装的 Sorting Layer 和 Order，默认分别为 `projectile` 与 `0`。
- 场景 Ground 已替换为 40x40 的平铺 `SpriteRenderer`，Sorting Layer 为 `Background`，并移除 MeshRenderer、MeshFilter 和 MeshCollider。
- 从原地图图集默认地砖 `map1_58` 派生独立 Full Rect Sprite，未修改原图集导入设置。
- 场景 Player 和 TargetDummy 根节点均保存为 `Y=0`；Player、TargetDummy Prefab 内容及哈希未改变。
- 新增幂等定向迁移器和零高度、场景 Ground、角色/敌人图层、弹丸图层回归测试。

## 下一步

1. 退出当前 Play Mode 或重新打开 `TopDownShooterPrototype`，确保 Unity 主窗口载入磁盘上的新 Ground 场景。
2. 在 Play Mode 中观察 Ground 平铺尺寸、玩家与测试靶遮挡，以及基础弹丸穿过角色和敌人时的透明排序表现。
3. 后续新增弹丸包装时通过各自 `ProjectileVisualWrapper` 的中文渲染层端口配置，不从瞄准或 3C 系统接管 sorting。

## 阻塞

- 无。

## 涉及文件

- `Assets/Scenes/TopDownShooterPrototype.unity`
- `Assets/Art/Map/GroundBackgroundTile.asset`
- `Assets/Scripts/Prototype/TopDownPlayerController.cs`
- `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- `Assets/Scripts/Prototype/ProjectileVisualWrapper.cs`
- `Assets/Scripts/Editor/ZeroHeightRenderingMigration.cs`
- `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`
- `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`
- `Assets/Tests/EditMode/RenderingLayerContractTests.cs`

## 验证记录

- [failed] `2026-08-18`：实现前聚焦测试 `0/4`，分别确认玩家、弹丸、场景 Player 高度和粒子 Default 图层缺口。
- [passed] `2026-08-18`：实现后聚焦测试 `4/4`。
- [passed] `2026-08-18`：隔离 Unity 工程完整 EditMode 回归 `102/102`。
- [passed] `2026-08-18`：最终场景渲染契约测试通过，确认 Ground、Player、TargetDummy 与 sorting 契约。
- [passed] `2026-08-18`：Player 与 TargetDummy Prefab SHA256 与工作开始时一致。
- [not-run] `2026-08-18`：当前可见 Play Mode 的最终遮挡与平铺观感尚未人工验收。

## 相关资料

- [项目状态](../../STATUS.md)
- [基础射击可视与换弹修复](../2026-08-18-base-shot-reload-visual-fix/STATE.md)
