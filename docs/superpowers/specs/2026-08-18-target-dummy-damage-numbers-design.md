# 测试靶与伤害飘字设计

## 目标

在当前顶视角原型中加入一个不会移动、不会攻击且拥有无限生命的测试靶。弹丸命中测试靶时，测试靶身旁生成独立的世界空间伤害数字，数字轻微上浮并淡出。

## 范围

- 建立可供未来正式敌人和其他伤害来源复用的受伤协议。
- 让 `Projectile` 在命中实现该协议的对象时提交伤害数据。
- 新增无限生命的 `TargetDummy`，只广播受伤结果，不维护或扣减生命值。
- 新增世界空间伤害数字生成器和单个飘字视图。
- 创建 `Assets/Prefab/TargetDummy.prefab` 并在当前原型场景放置一个实例。
- 使用专用、幂等的编辑器入口创建 Prefab 和场景实例。

## 非目标

- 不实现普通敌人的有限生命、死亡、移动、攻击、掉落或受击硬直。
- 不实现对象池、暴击样式、伤害类型抗性或伤害合并。
- 不修改左轮、骰面词条、玩家、瞄准、镜头、sorting layer 或现有 Inspector 数值。
- 不调用 `TopDownPrototypeSceneBuilder`，不重建当前场景。

## 架构

### DamageInfo 与 IDamageReceiver

`DamageInfo` 是只读伤害数据，首版包含数值、命中位置和来源对象。`IDamageReceiver.ReceiveDamage(DamageInfo)` 是弹丸和受击对象之间唯一的协议。

弹丸只负责从自身运行时属性构造 `DamageInfo`，并通过命中 Collider 的父级查找接收者。弹丸不认识测试靶、生命值或 UI。

### TargetDummy

`TargetDummy` 实现 `IDamageReceiver`。每次收到伤害时发布 `DamageReceived` 事件，并保存最近一次伤害供测试和调试读取。它没有当前生命字段，也不会因受伤销毁，因此语义上始终为无限生命。

### 世界空间飘字

`WorldDamageNumberSpawner` 订阅 `TargetDummy.DamageReceived`，每次命中创建一个独立 `WorldDamageNumber`。飘字使用世界空间 Canvas 与 UGUI `Text`，出生位置在测试靶身旁加入小范围随机偏移；随后按可调持续时间上浮、淡出并销毁。

显示规则：整数不显示小数；非整数最多保留一位小数。多个命中各自生成数字，互不覆盖。

### Prefab 与场景

`TargetDummy.prefab` 使用独立根对象、单个触发命中 Collider、静态测试靶外观和世界空间飘字生成器。专用编辑器方法只创建或更新该 Prefab，并在 `TopDownShooterPrototype.unity` 中不存在同名实例时添加一个实例；它不触碰其他场景对象。

## 测试

EditMode 测试覆盖：

1. `TargetDummy` 接收伤害后广播正确数据，连续受击时对象仍存在。
2. `Projectile` 将运行时伤害与命中位置提交给父级 `IDamageReceiver`。
3. 每次受伤都会生成独立飘字，并正确格式化整数和一位小数。
4. Prefab 包含受伤、Collider 和飘字组件，场景只包含一个测试靶实例。
5. 完整 EditMode 回归、上下文检查和受保护数据检查通过。

