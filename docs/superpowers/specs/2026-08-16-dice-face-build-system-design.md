# 骰面构筑系统设计

## 目标

为俯视角骰子左轮制作一个原型版骰面构筑页面。玩家按 `E` 呼出页面，再次按 `E` 关闭页面。页面左侧显示六个左轮骰面，右侧显示所有可装备的骰面词条。玩家可以先点击右侧词条，再点击左侧某个骰面，将该词条装备到对应骰面上。

## 范围

该功能会加入骰子左轮的第一层 roguelite 构筑系统。它不能把 UI、玩家移动、镜头移动或底层子弹移动逻辑耦合在一起。UI 只负责编辑装配数据；左轮在发射某个骰面时读取该装配数据；子弹事件效果以独立 ScriptableObject 的形式存在。

该功能暂不包含物品获取、局内持久化、商店奖励、完整敌人生命系统，也不追求最终美术表现。

## 架构

系统拆成四个单元：

- `DiceFaceEntry`：ScriptableObject，用于描述一个可装备的骰面词条。
- `BulletEventEffect`：ScriptableObject 基类，用于描述开火时、击中时、结束开火时触发的效果。
- `DiceFaceLoadout`：运行时组件，保存六个骰面当前装备的词条。
- `DiceBuildPageUI`：UI 控制器，负责按 `E` 开关页面、列出库中的词条，并把玩家选择的词条装备到指定骰面。

`DiceRevolverGun` 继续负责射击频率、换弹状态、枪口位置和子弹生成。它在掷出某个骰面后，向 `DiceFaceLoadout` 查询该骰面装备的词条，把词条中的子弹属性应用到生成的子弹上，然后在现有的事件时机执行词条里的事件效果。

## 数据模型

### DiceFaceLibrary

`DiceFaceLibrary` 存储所有可用的 `DiceFaceEntry` 资源。

字段：

- `entries`：`DiceFaceEntry` 数组

### DiceFaceEntry

`DiceFaceEntry` 描述某个骰面被掷出时，子弹会携带的全部内容。

字段：

- `displayName`
- `description`
- `displayColor`
- `projectilePrefabOverride`
- `projectileType`
- `projectileTag`
- `damage`
- `flightDistance`
- `flightSpeed`
- `enemyPierceCount`
- `extensionPorts`：可序列化的名称/数值扩展端口列表，供后续继续增加属性
- `onFireEffects`：`BulletEventEffect` 数组
- `onHitEffects`：`BulletEventEffect` 数组
- `onFireEndEffects`：`BulletEventEffect` 数组

### BulletEventLibrary

`BulletEventLibrary` 存储所有可用的 `BulletEventEffect` 资源。这样未来工具层可以单独浏览事件词条，而不是只能从完整骰面词条里查看。

字段：

- `effects`：`BulletEventEffect` 数组

## 事件效果

第一批事件效果资源包括：

- `ExtraShotOnFireEffect`：开火时，基于当前骰面的最终子弹属性，额外生成一颗子弹。额外生成的子弹不会递归触发新的额外发射。
- `ExplosionOnHitEffect`：击中时，在命中位置生成一次爆炸/子弹类型效果。该效果 SO 暴露一个可配置的爆炸 projectile prefab 端口。
- `ForceFaceFourOnFireEndEffect`：结束开火时，如果骰面 4 仍在弹夹随机池中，则跳过；如果骰面 4 不在随机池中，则把骰面 4 填回随机池，并让下一次抽取必定返回 4。

## 弹夹随机池改动

`DiceChamber` 需要增加少量受控 API：

- 查询某个骰面是否还在剩余随机池中。
- 如果某个骰面不在随机池中，则把它补回。
- 如果某个骰面存在于随机池中，则强制下一次抽取该骰面。

抽取方法仍然是唯一负责从随机池中移除骰面的入口。

## 子弹属性

`Projectile` 增加一个运行时配置方法，让左轮能为每次射击应用骰面词条中的子弹属性。

属性包括：

- 伤害
- 飞行距离
- 飞行速度
- 敌人穿透数量
- 弹幕 Tag

当前原型即使还没有完整敌人系统，也应该先存储伤害和穿透数。飞行距离应通过“距离 / 速度”换算成生命周期，从而保留当前自动销毁的行为。

## UI 行为

`DiceBuildPageUI` 挂在现有 HUD Canvas 下，或挂在一个新的 Overlay Canvas 下。页面默认隐藏。

打开和关闭：

- 按 `E` 打开。
- 再次按 `E` 关闭。
- 原型阶段页面打开时可以暂时不暂停游戏。

布局：

- 左侧：放大版六面骰子展开图，沿用右上角弹药 UI 的排列方式：1 在上方，2-5 横向排列，6 在 3 的下方。
- 右侧：纵向列出 `DiceFaceLibrary.entries` 中的所有词条。
- 点击右侧词条会选中该词条。
- 点击左侧某个骰面，会把当前选中的词条装备到该骰面。
- 每个骰面显示自己的数字，以及已装备词条名称；如果未装备，则显示空槽状态。

## 场景与资源设置

新增资源文件夹：

- `Assets/Data/DiceFaces`
- `Assets/Data/BulletEvents`
- `Assets/Data/Libraries`

创建原型资源：

- `DiceFaceLibrary.asset`
- `BulletEventLibrary.asset`
- `ExtraShotOnFireEffect.asset`
- `ExplosionOnHitEffect.asset`
- `ForceFaceFourOnFireEndEffect.asset`
- 至少三个示例 `DiceFaceEntry` 资源，并让它们使用新事件效果。

场景接线时，只添加必要引用，不覆盖你已经在 Inspector 中调好的 `Player.prefab`、`AimRoot`、`ArmVisual`、`GunBody`、`Muzzle`、sorting layer，或 `DiceRevolverGun` 的射击/换弹调参字段。

## 错误处理

如果库为空或没有引用，页面仍然可以打开，只是右侧列表为空。

如果某个骰面没有装备词条，左轮会使用当前默认 projectile 行为和默认 projectile prefab。

如果某个事件效果缺少可选 prefab，则打印 warning 并跳过该效果。

## 测试与验证

自动化测试应覆盖：

- `DiceChamber` 的强制抽取和补回骰面行为。
- `DiceFaceLoadout` 对六个骰面槽位的装备与读取。
- 事件效果规则，尤其是额外发射不能递归，以及强制骰面 4 的行为。

实现后必须通过 Unity batchmode 编译。

手动验证：

- 按 `E` 可以打开和关闭构筑页面。
- 点击右侧词条，再点击左侧骰面，会更新该骰面的显示文本。
- 开火会使用该骰面装备词条中的属性。
- 三个原型事件会在正确时机触发。
