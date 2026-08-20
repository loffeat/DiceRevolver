# DiceRevolver 项目地图

## 项目目标

DiceRevolver 是一个 Unity 6 顶视角射击原型。当前核心验证目标是把六发左轮建模为不放回抽取的六面骰池，由骰面组合基础事件与构筑事件，并通过独立弹丸定义驱动生成、属性、命中和后续连锁。

## 当前玩法或业务循环

1. 玩家用 `WASD` 移动，鼠标指向地面完成瞄准。
2. 按住鼠标左键射击；左轮从剩余骰面中随机抽取一个面并移除。
3. 抽中的骰面生成四槽位配置快照，依次解析基础、开火时、命中时和开火后效果。
4. 六面耗尽后自动换弹并重置骰池；`R` 可以手动换弹。
5. `E` 打开构筑页，先选词条再选骰面以修改装备。

## 技术栈

- Unity `6000.3.10f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.18.0`
- UGUI `2.0.0`
- C#，EditMode 测试使用 Unity Test Framework `1.6.0`

## 运行入口与操作

- 构建入口场景：[TopDownShooterPrototype.unity](../../Assets/Scenes/TopDownShooterPrototype.unity)
- Unity 版本：[ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt)
- 构建场景配置：[EditorBuildSettings.asset](../../ProjectSettings/EditorBuildSettings.asset)
- 操作：`WASD` 移动、鼠标瞄准、左键射击、`R` 换弹、`E` 打开或关闭构筑页。

## 目录地图

- [Assets/Scripts/Prototype](../../Assets/Scripts/Prototype)：运行时玩家、左轮、弹丸、事件与 UI。
- [Assets/Scripts/Editor](../../Assets/Scripts/Editor)：原型场景和骰面资源生成工具。
- [Assets/Tests/EditMode](../../Assets/Tests/EditMode)：弹巢、装备、弹丸、效果、UI 和瞄准测试。
- [Assets/Resources/DiceFacePrototype](../../Assets/Resources/DiceFacePrototype)：四个示例词条、弹丸定义及事件资源。
- [docs/superpowers](../../docs/superpowers)：已批准的设计规格和实施计划。
- [.superpowers/sdd](../../.superpowers/sdd)：既有骰面构筑任务的执行与复核证据。

## 核心模块

- [TopDownPlayerController.cs](../../Assets/Scripts/Prototype/TopDownPlayerController.cs)：读取移动与鼠标输入，发布瞄准方向和世界坐标。
- [TopDownCharacterController.cs](../../Assets/Scripts/Prototype/TopDownCharacterController.cs)：共享角色运动模块，消费移动、瞄准、开火和换弹意图并负责平面约束与转向。
- [TestRobotController.cs](../../Assets/Scripts/Prototype/TestRobotController.cs)：把测试机器人战斗决策适配到共享角色控制接口。
- [BehaviorTree.cs](../../Assets/Scripts/Prototype/BehaviorTree.cs)：不依赖场景的 Sequence、Selector、Parallel、Condition 和 Action 行为树节点。
- [TestRobotCombatBrain.cs](../../Assets/Scripts/Prototype/TestRobotCombatBrain.cs)：按近/远阈值输出接近、后退或横移，在移动爆发与站定攻击间循环，并持续提供瞄准与开火意图。
- [TopDownAimHandRig.cs](../../Assets/Scripts/Prototype/TopDownAimHandRig.cs)：处理手臂镜像、枪口姿态和近距离稳定瞄准。
- [DiceChamber.cs](../../Assets/Scripts/Prototype/DiceChamber.cs)：维护剩余骰面、强制下次骰面和重置规则。
- [DiceRevolverGun.cs](../../Assets/Scripts/Prototype/DiceRevolverGun.cs)：协调射速、抽面、弹丸生成、事件和换弹。
- [DiceFaceLoadout.cs](../../Assets/Scripts/Prototype/DiceFaceLoadout.cs)：保存六个骰面的四槽位运行时装备，并兼容读取旧序列化数据。
- [DiceFaceConfiguration.cs](../../Assets/Scripts/Prototype/DiceFaceConfiguration.cs)：保存单面四槽位配置并生成单次激活快照。
- [DiceFaceEntry.cs](../../Assets/Scripts/Prototype/DiceFaceEntry.cs)：单槽位 ScriptableObject 构筑词条，绑定一个槽位类型和一个事件效果。
- [ProjectileDefinition.cs](../../Assets/Scripts/Prototype/ProjectileDefinition.cs)：拥有弹丸 Prefab、运行时属性、默认攻击特效与扩展端口。
- [DiceFaceActivation.cs](../../Assets/Scripts/Prototype/DiceFaceActivation.cs)：保存单次骰面激活快照、弹丸生成请求、命中关系和事件预算。
- [ProjectileSpawnEffect.cs](../../Assets/Scripts/Prototype/ProjectileSpawnEffect.cs)：按弹丸定义、延迟、主弹身份和攻击特效覆盖策略请求生成弹丸。
- [BulletEventEffect.cs](../../Assets/Scripts/Prototype/BulletEventEffect.cs)：开火、命中和开火结束效果的扩展基类。
- [BulletEventTimeScheduler.cs](../../Assets/Scripts/Prototype/BulletEventTimeScheduler.cs)：为子弹事件提供确定顺序、异常隔离的游戏时间延迟队列。
- [Projectile.cs](../../Assets/Scripts/Prototype/Projectile.cs)：应用运行时属性、移动、统一碰撞过滤和生命周期。
- [AreaExplosionProjectile.cs](../../Assets/Scripts/Prototype/AreaExplosionProjectile.cs)：在爆炸点对范围内受伤对象去重结算一次伤害，并驱动扩张淡出的圆环表现。
- [DamageInfo.cs](../../Assets/Scripts/Prototype/DamageInfo.cs)：跨伤害来源传递数值、命中点和来源的只读数据。
- [TargetDummy.cs](../../Assets/Scripts/Prototype/TargetDummy.cs)：无限生命测试靶，接收伤害并广播表现事件。
- [WorldDamageNumberSpawner.cs](../../Assets/Scripts/Prototype/WorldDamageNumberSpawner.cs)：把测试靶受击事件转换为独立世界空间飘字。
- [DiceBuildPageUI.cs](../../Assets/Scripts/Prototype/DiceBuildPageUI.cs)：编辑骰面装备。
- [DiceBuildRuntimeView.cs](../../Assets/Scripts/Prototype/DiceBuildRuntimeView.cs)：场景加载后按需创建构筑 UI 和装备组件。

## 关键数据流

```text
玩家输入 -> TopDownPlayerController -> TopDownCharacterController
目标位置 -> TestRobotCombatBrain -> TestRobotController -> TopDownCharacterController
TopDownCharacterController -> 移动/瞄准/动画/DiceRevolverGun
DiceChamber 抽面 -> DiceFaceLoadout -> DiceFaceConfigurationSnapshot 四槽位快照
ProjectileSpawnEffect -> ProjectileDefinition -> ProjectileRuntimeStats + 弹丸 Prefab
DiceFaceActivation -> 延迟生成、命中事件关系与连锁预算
DiceBuildPageUI -> DiceFaceLoadout.Equip -> 只替换词条所属槽位 -> 后续射击读取新快照
Projectile -> IDamageReceiver -> TargetDummy.DamageReceived -> WorldDamageNumberSpawner
ExplosionOnHitEffect -> BlastExplosion ProjectileDefinition -> AreaExplosionProjectile -> 范围 IDamageReceiver
```

额外射击通过事件上下文请求同属性弹丸，并禁止递归触发额外射击。命中事件由 `ProjectileHitReporter` 桥接回本次射击上下文。
需要延迟的事件通过 `BulletEventContext.Schedule` 登记回调，由所属左轮使用 `Time.time` 驱动；暂停游戏时延迟计时同步暂停。

## 明确非目标

- 当前只有通用伤害投递和无限生命测试靶，没有正式敌人的有限生命、死亡和穿透消费系统。
- 当前不实现词条获取、商店、奖励、局内持久化或正式美术表现。
- 上下文系统不修改 Unity 运行时代码、资源、Prefab、场景或项目设置。

## 术语

- 骰池：`DiceChamber` 中尚未被抽出的骰面集合。
- 骰面词条：绑定一个事件阶段、可装备到对应槽位的 `DiceFaceEntry` 资源。
- 四槽位：每个骰面独立拥有基础、开火时、命中时、开火后四个单事件槽位。
- 装备：`DiceFaceLoadout` 中六个面到四槽位配置的映射。
- 弹丸事件：由 `BulletEventEffect` 在开火、命中或开火结束时执行的扩展行为。
- 工作流：`.project-context/project/workstreams/` 下一个独立事项的状态和交接记录。
- 控制意图：共享角色控制器暴露的移动、瞄准、开火和换弹状态；来源可以是玩家输入或机器人 AI。
- 行动节奏：测试机器人默认移动 `0.7` 秒、站定攻击 `1.0` 秒，站定结束后重新判断战术移动方向。
- 范围爆炸：由命中事件在命中点生成的独立爆炸弹丸；伤害取自弹丸定义，半径和圆环表现取自爆炸 Prefab。
