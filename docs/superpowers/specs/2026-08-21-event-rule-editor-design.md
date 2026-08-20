# 事件规则列表与 Unity 编辑器配置页设计

## 目标

建立一个 Unity 编辑器内的事件配置页面，按事件类型展示项目现有事件，并允许设计者组合：

- 事件触发类型；
- 规则级触发条件；
- 按顺序执行的结果；
- 每个结果自身的结果条件；
- 各模块公开的可调参数与 Debug 信息。

第一版采用规则列表，不实现任意节点图。所有条件默认按 AND 判定，但接口保留未来增加 OR 条件组的空间。

## 核心数据模型

### EventRuleDefinition

一个可装备事件对应一个 `EventRuleDefinition` ScriptableObject：

- 显示名称、描述、颜色、标签和稀有度等元数据；
- 一个 `EventTriggerModule`；
- 零到多个 `EventConditionModule`；
- 一个有序 `EventResultEntry` 列表；
- 每个 `EventResultEntry` 包含零到多个局部条件和一个 `EventResultModule`；
- 事件预算消耗、允许的槽位类型和递归策略。

模块作为规则资产的 SubAsset 保存。这样每条规则在 Project 中仍表现为一个主资产，同时保留 Unity Undo、序列化引用和模块类型检查，避免大量零散小文件。

```text
EventRuleDefinition
├─ Trigger: OnProjectileHit
├─ Conditions (全部满足)
│  ├─ ProjectileHasTag(Lightning)
│  └─ TargetIsEnemy
└─ Results (从上到下)
   ├─ [条件: NearbyOwnedProjectileCount >= 1] CreateLightningChain
   └─ [无局部条件] AddChamberStack(1)
```

## 模块接口

### 触发模块

`EventTriggerModule` 只描述“何时产生候选事件”，不执行玩法结果：

- 骰面基础；
- 开火时；
- 命中时；
- 开火后；
- 被动监听：弹丸生成、弹丸命中、换弹、骰面消耗、抽面过滤等。

触发器把运行时事实转换为不可变 `EventSignal`。Signal 携带 Gun、骰面、激活、弹丸、命中对象、位置、标签、事件预算和 Debug 因果作用域。

### 条件模块

`EventConditionModule` 接收 `EventEvaluationContext` 并返回结构化结果：

```csharp
EventConditionResult Evaluate(EventEvaluationContext context);
```

结果包含通过/失败、Debug 描述和可选失败原因。首批通用条件建议包括：

- 弹丸类型等于；
- 包含指定弹丸 Tag；
- 是否为攻击特效；
- 当前骰面/来源骰面；
- 弹夹剩余数量；
- 指定骰面是否仍在弹夹；
- 范围内同归属弹丸数量；
- 目标是否为敌人；
- 本轮触发次数或层数比较。

### 结果模块

`EventResultModule` 只负责一个结果，通过受限命令接口请求行为：

```csharp
EventResult Execute(EventExecutionContext context);
```

结果返回成功、跳过或失败及 Debug 描述。模块不能直接查找 Player、修改场景单例或绕过事件预算。首批结果建议包括：

- 生成指定弹丸；
- 延迟后执行结果列表；
- 触发指定骰面奖励射击；
- 造成范围伤害或生成爆炸定义；
- 生成闪电链；
- 检索/强制指定骰面；
- 覆盖下一骰面的非空活动槽；
- 增减本轮层数、次数或其他局部状态。

## 运行时结构

`EventRuleRuntime` 由规则资产为每把 Gun、每个装备面创建。它拥有可变状态，资产本身保持只读：

```text
DiceRevolverGun / DiceShotPipeline
        ↓ EventSignal
EventRuleRuntime
        ↓ Trigger 匹配
规则级 Conditions
        ↓ 全部通过
按顺序遍历 ResultEntry
        ↓ 局部结果条件通过
EventResultModule Runtime
        ↓ 受限命令
弹丸、枪膛、时间系统、闪电链等既有服务
```

每个模块运行时独立捕获异常。一个条件或结果出错只停止当前规则，不影响同骰面的其他槽位、其他规则或角色 3C。

所有延迟和连锁继续共享 `DiceEventBudget`，并复用 `CombatDebugTrace` 的 ChainId、父激活与递增序号。

## 编辑器页面

入口：`Window > Dice Revolver > 事件规则编辑器`。

页面使用三栏布局：

1. 左栏：事件类型分类、标签筛选、错误筛选。
2. 中栏：该分类下全部规则，支持搜索、新建、复制、重命名和定位资产。
3. 右栏：选中规则的完整配置，直接显示模块公开字段。

右栏从上到下显示：

- 基础信息与允许装备槽位；
- 触发类型模块；
- 规则级条件列表；
- 可拖拽排序的结果列表；
- 每个结果的局部结果条件；
- 实时校验和引用关系；
- Play Mode 最近触发记录。

模块添加菜单通过 `TypeCache.GetTypesDerivedFrom` 自动发现，不在窗口脚本中维护硬编码类型列表。资产列表通过 `AssetDatabase.FindAssets` 获取，因此页面能展示所有现有规则，不依赖手工更新 Library 才能被编辑器发现。

## 校验规则

编辑器必须即时提示但不擅自覆盖数据：

- 缺失触发模块或空结果列表；
- 规则允许槽位与触发类型冲突；
- 缺失弹丸、Tag、类型或 Prefab 引用；
- 延迟或奖励射击可能形成无预算递归；
- 被动规则包含不支持持久状态的模块；
- 同一 SubAsset 被错误挂到多个规则；
- 运行时服务需求未满足。

校验器输出错误、警告和提示三级结果。错误阻止保存为可装备状态，警告允许保存。

## 现有事件迁移

第一阶段保留 `BulletEventEffect` 和 `PassiveEventEffect`，通过兼容适配器逐个迁移，不一次性改写所有资源：

| 现有事件 | 新规则表达 |
|---|---|
| 基础射击 | 骰面基础触发 → 生成指定弹丸 |
| DoubleTap | 开火时 → 延迟 0.25 秒 → 生成当前基础弹丸 |
| BlastRound | 命中时 + 攻击特效条件 → 生成爆炸定义 |
| LoadedFour | 开火后 + 骰面 4 不在弹夹 → 填充并强制骰面 4 |
| 电磁共鸣 | 开火时 + Lightning 条件 + 范围弹丸条件 → 生成闪电链 |
| 特斯拉 | 弹丸生成被动 + Lightning 条件 → 增加层数；基础生成前读取层数修改伤害 |
| 呼应协同 | 同类弹丸命中被动 + 次数条件 → 触发绑定骰面奖励射击 |
| 链式反应 | 开火后 → 覆盖下一普通骰面非空活动槽 |
| 收尾者 | 抽面过滤被动 → 设置绑定面最低抽取优先级 |

迁移完成前，`DiceFaceEntry` 可引用旧 Effect 或新 Rule，但同一槽位只能使用其中一种，避免重复执行。

## Debug 集成

规则运行时向现有左上角 Debug 追踪器依次发布：

- 触发器收到 Signal；
- 每个规则级条件的通过或失败；
- 每个结果条件的通过或失败；
- 结果开始、成功、跳过或失败；
- 延迟任务实际执行；
- 奖励激活的父子因果关系。

默认 HUD 只显示已触发和已执行的高层记录。编辑器页面的 Play Mode 调试区可展开条件级记录，避免普通游戏画面被大量判定日志淹没。

## 测试策略

- 纯 C#：条件 AND、结果顺序、局部结果条件、预算、异常隔离和状态实例独立。
- 运行时集成：四活动槽、被动 Signal、延迟、命中、奖励射击和 Debug 因果链。
- 编辑器：类型自动发现、SubAsset 创建删除、Undo/Redo、拖拽排序、校验和资源重载。
- 迁移：每个旧事件迁移前后使用相同输入，断言产生相同可观察结果。
- 保护：Player/TestRobot 配置、枪械参数、AimRoot 和 sorting layer 不由迁移工具自动覆盖。

## 分阶段实施建议

1. 建立 Rule、Trigger、Condition、Result 接口和纯 C# 执行器。
2. 创建编辑器三栏页面、模块自动发现和校验器。
3. 增加旧 Effect 适配器，让新旧系统并存。
4. 迁移基础射击、DoubleTap、BlastRound、LoadedFour。
5. 增加带状态的被动模块并迁移雷电构筑。
6. 移除已无引用的旧具体 Effect 类，但长期保留兼容数据读取边界。

## 非目标

- 第一版不实现节点连线、循环节点、任意脚本表达式或运行时代码生成。
- 不允许配置模块直接访问任意场景对象或使用反射调用方法。
- 不在规则编辑器中修改角色 Prefab、枪械参数、Transform 或渲染层。
