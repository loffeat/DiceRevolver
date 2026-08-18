# 基础射击可视与换弹手臂修复

- ID: `2026-08-18-base-shot-reload-visual-fix`
- Status: `completed`
- Branch: working tree
- Created: `2026-08-18`
- Updated: `2026-08-18`

## 目标

- 修复基础射击逻辑执行后看不到弹丸的问题。
- 让换弹期间的 `ArmVisual` 只做明暗闪烁，不再消失或改变姿态。
- 不修改角色 Transform、AimRoot 子物体、sorting 或现有左轮调参。

## 非目标

- 不修改原始 `fire_1.prefab`、材质资产或粒子参数。
- 不调整角色姿态、枪口位置、左轮数值或场景布局。
- 不在本轮完成最终 Play Mode 美术尺寸与手感调校。

## 已确认事实

- 六面基础事件、时间系统和弹丸 Prefab 引用完整。
- 模拟左键能够消耗弹药并生成逻辑弹丸。
- `fire_1` 两个材质的 Shader 在运行时解析为 `Hidden/InternalErrorShader`。
- 换弹代码会直接改写作为 `visualRoot` 的 `ArmVisual` 局部位置。

## 当前正在进行

- 无

## 根因

- Player Prefab、六面基础事件、时间系统和弹丸实例化链均正常；模拟左键测试确认会消耗一发并生成 `Projectile`。
- `fire_1.prefab` 的两个粒子材质引用同一个不存在于项目中的 Shader GUID，运行时材质 Shader 为 `Hidden/InternalErrorShader`，导致弹丸逻辑存在但美术包装无法正常渲染。
- `DiceRevolverGun` 仍把 `ArmVisual` 作为 `visualRoot`，换弹时将其向下移动 `reloadDropDistance`，与“只明暗闪烁”的要求冲突。

## 已完成

- 新增 `DiceRevolver/Projectile Particle Unlit` 透明粒子 Shader。
- `ProjectileVisualWrapper` 只替换缺失或错误的粒子 Shader，复用原材质纹理、纹理缩放偏移和颜色；不修改 `fire_1.prefab`、材质资产、粒子参数或 sorting。
- `DiceRevolverGun` 的换弹动画不再读取或写入 `ArmVisual` 的位置、旋转，只按现有闪烁速度和暗色端口改变 SpriteRenderer 颜色并在结束时恢复。
- 新增真实 Player Prefab 基础事件、模拟左键、错误 Shader 替换和 ArmVisual Transform 保护测试。
- 未修改 `Player.prefab`、`TargetDummy.prefab` 或用户维护的场景序列化数据。

## 涉及文件

- `Assets/Scripts/Prototype/ProjectileVisualWrapper.cs`
- `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- `Assets/Shaders/ProjectileParticleUnlit.shader`
- `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`
- `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`

## 验证记录

- [passed] `2026-08-18`：真实基础事件经时间系统生成配置弹丸。
- [passed] `2026-08-18`：模拟左键会把弹巢从六发减到五发并生成基础弹丸。
- [failed] `2026-08-18`：修复前可视化测试得到 `Hidden/InternalErrorShader`，确认缺失 Shader 根因。
- [failed] `2026-08-18`：修复前换弹测试确认 `ArmVisual` 局部位置被改写。
- [passed] `2026-08-18`：修复后相关聚焦测试 `16/16` 通过。
- [passed] `2026-08-18`：隔离 Unity 工程完整 EditMode 回归 `98/98` 通过，无 C# 或 Shader 编译错误。
- [passed] `2026-08-18`：主 Unity 会话成功导入相关脚本与 Shader，最新日志无编译错误。
- [not-run] `2026-08-18`：当前可见 Play Mode 中的弹丸最终尺寸、颜色和运动观感尚未人工确认。

## 下一步

1. 在 `TopDownShooterPrototype` Play Mode 中确认基础弹丸可见且沿枪口方向运动。
2. 连续打空六发，确认换弹期间手臂保持原位和原旋转，只做明暗闪烁。
3. 若只需调美术尺寸或朝向，修改 `BasicRevolverBullet.prefab` 上 `ProjectileVisualWrapper` 的视觉缩放或局部旋转端口，不修改原始 `fire_1.prefab`。

## 阻塞

- 无。

## 相关资料

- [项目状态](../../STATUS.md)
- [前置弹丸事件管线](../2026-08-18-projectile-definition-event-pipeline/STATE.md)
