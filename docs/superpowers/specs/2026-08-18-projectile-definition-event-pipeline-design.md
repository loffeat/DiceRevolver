# 子弹定义与模块化骰面事件管线设计

## 目标

把当前“左轮直接生成弹丸、骰面词条同时保存弹丸属性”的流程改为模块化事件管线：骰面负责组合基础事件与构筑事件，子弹定义负责完整弹丸数据，单次骰面激活上下文负责把生成、命中和后续连锁关联起来。

首个子弹类型是“基础左轮子弹”。它使用独立运行时 Prefab 外壳，并将 `Assets/Art/Effect/perfab/fire_1.prefab` 作为纯视觉子物体。

## 已确认规则

- 采用单次骰面激活上下文，不使用全局事件总线。
- 六个骰面都配置同一个“发射基础左轮子弹”基础事件。
- `DoubleTap` 只作为开火时事件，`BlastRound` 只作为命中时事件，`LoadedFour` 只作为开火后事件。
- 骰面词条不直接绑定子弹，也不保存弹丸属性。
- 子弹 SO 完全管理弹幕类型、Tag、伤害、距离、速度、穿透、扩展端口、Prefab 和默认攻击特效判定。
- 主基础弹丸命中时允许触发本次骰面的命中事件。
- 双发射击的第二发默认不触发命中事件。
- 攻击特效判定支持双层配置：子弹 SO 提供默认值，生成事件可继承、强制开启或强制关闭。
- 每次骰面激活的事件执行预算默认最多 `32` 次，避免错误配置形成无限连锁。

## 术语

- **子弹定义**：`ProjectileDefinition` ScriptableObject，描述一种完整弹丸类型。
- **子弹类库**：`ProjectileDefinitionLibrary` ScriptableObject，保存可用子弹定义。
- **基础事件**：骰面槽位自身拥有的事件，不属于镶嵌词条。
- **构筑事件**：`DiceFaceEntry` 提供的开火时、命中时和开火后事件。
- **骰面激活**：一次骰面被抽中后，从事件派发到其弹丸与连锁事件结束的独立运行时上下文。
- **攻击特效**：允许其命中继续触发当前骰面命中事件的附加弹幕。

## 数据模型

### ProjectileDefinition

新增 `ProjectileDefinition : ScriptableObject`，字段如下：

- 显示名称与描述。
- 运行时 `Projectile` Prefab。
- 弹幕类型。
- 弹幕 Tag。
- 弹幕伤害。
- 飞行距离。
- 飞行速度。
- 敌人穿透数量。
- 扩展端口。
- 默认是否视为攻击特效。

`ProjectileDefinition.BuildRuntimeStats()` 统一生成 `ProjectileRuntimeStats`。左轮、骰面词条和事件不再手工拼装这些属性。

### ProjectileDefinitionLibrary

新增 `ProjectileDefinitionLibrary : ScriptableObject`，只保存 `ProjectileDefinition[]` 并以只读集合暴露。首个资源：

```text
基础左轮子弹
Prefab: BasicRevolverBullet.prefab
类型: Revolver
Tag: PlayerBullet
伤害: 1
距离: 18
速度: 18
穿透: 0
默认攻击特效: false
```

数值取当前原型默认值，不改动用户已经填写的 `DiceRevolverGun` 射速、换弹或弹速端口。

### DiceFaceEntry

`DiceFaceEntry` 只保留：

- 显示名称、描述、颜色。
- 开火时事件。
- 命中时事件。
- 开火后事件。

移除弹丸 Prefab、类型、Tag、伤害、距离、速度、穿透和弹丸扩展端口的运行时职责。已有三个资源继续分别承载：

- `DoubleTap`：开火时追加第二发。
- `BlastRound`：主弹丸或攻击特效弹幕命中后触发爆炸。
- `LoadedFour`：开火后检索骰面 4。

### DiceFaceLoadout

`DiceFaceLoadout` 保留六个镶嵌词条槽，并新增六个基础事件槽。当前六个基础槽都引用同一个“发射基础左轮子弹”事件资源。

基础事件与镶嵌词条互不包含、互不读取。更换 `DoubleTap`、`BlastRound` 或 `LoadedFour` 不会改变骰面的基础弹丸类型。

## 运行时架构

### DiceFaceActivation

每次成功抽取骰面时创建一个普通 C# 激活对象，保存：

- 骰面值。
- 当时装备的 `DiceFaceEntry` 快照。
- 枪口位置与方向。
- 所属弹巢与时间调度入口。
- 命中事件列表。
- 剩余事件预算，初始为 `32`。

延迟事件捕获该激活对象，因此玩家在 `0.25` 秒内移动、换词条或转向不会改变已经登记的第二发及其事件归属。

激活对象只协调本次攻击，不继承 `MonoBehaviour`，不成为全局单例。一个激活链出错、耗尽预算或失去对象引用时，只终止该链路。

### 事件阶段

一次骰面激活按下列顺序派发：

1. 发布骰面开火开始通知，供弹药 HUD 闪烁。
2. 执行该面的基础事件。“发射基础左轮子弹”通过时间系统登记延迟 `0` 的生成请求。
3. 执行当前词条的开火时事件。`DoubleTap` 登记延迟 `0.25` 秒的生成请求。
4. 执行当前词条的开火后事件。`LoadedFour` 独立操作弹巢。
5. 当前帧末时间系统执行延迟 `0` 的基础弹丸生成。
6. 弹丸命中后，若命中权限成立，激活对象执行当次快照中的命中事件。

“开火后”表示本次开火事件已派发完成，不等待弹丸销毁或命中。

### ProjectileSpawnEffect

新增通用生成弹幕事件，持有：

- `ProjectileDefinition`。
- 延迟秒数。
- 攻击特效覆盖策略。
- 是否为本次激活的主基础攻击。

攻击特效覆盖枚举：

```text
UseProjectileDefault
ForceEnabled
ForceDisabled
```

主基础攻击无论子弹默认值如何，都允许命中事件。非主弹幕按“事件覆盖优先、子弹默认值其次”计算命中权限。

### DoubleTap

`ExtraShotOnFireEffect` 不再复制 `DiceRevolverShotContext` 或直接克隆已有弹丸。它从激活上下文读取最近一次主弹丸定义，并登记：

```text
延迟: 0.25 秒
子弹定义: 本次基础事件使用的基础左轮子弹
攻击特效覆盖: ForceDisabled
主基础攻击: false
```

因此第二发默认不会触发 `BlastRound`，也不会再次触发 `DoubleTap`。

### 命中与 BlastRound

生成的 `Projectile` 保存轻量运行时命中绑定，其中包含激活对象和本弹丸是否允许触发命中事件。`ProjectileHitReporter` 只报告碰撞；它不认识 `BlastRound`、爆炸或骰面构筑。

基础左轮子弹命中时，由激活对象读取当次 `DiceFaceEntry.OnHitEffects` 并执行。`BlastRound` 与基础生成事件保持独立，只通过激活对象关联同一次攻击。

`ExplosionOnHitEffect` 继续只负责在命中位置请求自己的爆炸定义。爆炸定义未配置时记录 warning 并停止该效果，不影响其他事件。

## fire_1 美术包装

检查结果：`fire_1.prefab` 根节点只有 Transform，包含两个循环播放的 Particle System，材质引用完整，没有 `Projectile`、Collider、Rigidbody 或业务脚本。它不能直接作为运行时弹丸，但适合作为纯视觉子 Prefab。

新建 `BasicRevolverBullet.prefab`：

- 根节点：`Projectile`、`ProjectileHitReporter`、Trigger Collider、Kinematic Rigidbody。
- 子节点：嵌套 `fire_1.prefab` 实例。
- 根节点负责移动、旋转、碰撞和生命周期。
- `fire_1` 只负责显示，不修改原始 Prefab、材质或粒子参数。

若 Play Mode 中发现粒子模拟空间导致视觉长时间滞留或尺寸不合适，只调整嵌套实例的局部 Transform 或在包装 Prefab 上覆盖必要粒子属性；不会回写原始 `fire_1.prefab`。

## 事件预算与错误隔离

- 每次执行事件或生成请求前消耗一次预算。
- 预算耗尽后停止该激活对象的后续事件并输出一次 warning。
- 一个事件抛出异常时记录异常并继续同一阶段的其他独立事件，除非预算已经耗尽。
- 延迟回调发现激活对象失效时安全退出。
- 空子弹定义、空 Prefab 或空效果只跳过当前模块，不阻止移动、瞄准、换弹和其他激活对象。

## 兼容与迁移

- 保留现有时间调度器，并继续由 `DiceRevolverGun` 使用 `Time.time` 驱动。
- 保留当前射速、换弹、瞄准、相机、HUD 和弹巢行为。
- 允许对 `Player.prefab` 的唯一新增数据是 `DiceFaceLoadout` 六个基础事件引用。
- 不修改 Body、AimRoot、ArmVisual、GunBody、Muzzle、Transform、sorting layer 或现有 `DiceRevolverGun` Inspector 数值。
- `PrototypeProjectile.prefab` 保留，不作为新基础事件的运行时弹丸。
- 三个现有骰面词条资源迁移为纯事件词条，不直接引用基础子弹。

## 测试

EditMode 测试覆盖：

1. 子弹定义完整生成运行时属性，子弹库暴露基础左轮子弹。
2. `DiceFaceEntry` 只暴露三阶段事件，不再作为弹丸属性来源。
3. 六个骰面基础槽都引用发射基础左轮子弹事件。
4. 基础事件通过时间系统登记延迟 `0`，并在当前帧 Tick 时生成主弹丸。
5. `DoubleTap` 在 `0.25` 秒后生成同定义弹丸，默认 `ForceDisabled`。
6. 主弹丸命中触发 `BlastRound`；默认第二发命中不触发。
7. 子弹默认攻击特效和事件三态覆盖按优先级生效。
8. 标记为攻击特效的附加弹幕命中时可以触发 `BlastRound`。
9. 事件预算耗尽只终止当前激活链。
10. 基础子弹 Prefab 包含运行时组件和嵌套 `fire_1` 实例。
11. 完整 EditMode 回归通过，Player 受保护数据保持不变。

Play Mode 人工验证：

- 六个骰面都能发射基础左轮子弹。
- `fire_1` 跟随弹丸方向与移动，尺寸清晰且不会异常铺满场景。
- `DoubleTap` 第二发间隔、`BlastRound` 主弹命中关联和 `LoadedFour` 开火后行为符合规则。

## 非目标

- 本轮不实现正式爆炸子弹美术和数值；未配置时继续 warning。
- 本轮不实现敌人有限生命、穿透消费、对象池或构筑存档。
- 本轮不新增全局事件总线、全局时间单例或 Prefab 反向查找左轮。

