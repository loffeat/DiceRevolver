# 子弹事件时间系统设计

## 目标

为子弹事件提供一个轻量、可复用的游戏时间调度能力。首个使用者是“双重射击”：第一发正常生成，第二发默认在 `0.25` 秒后生成。后续事件可以通过相同的 `BulletEventContext` 接口安排延迟爆炸、延迟追加弹幕等行为，无需各自实现协程或依赖全局单例。

## 范围

- 新增由 `DiceRevolverGun` 持有的普通 C# 时间调度器。
- `BulletEventContext` 暴露统一的延迟安排接口。
- `ExtraShotOnFireEffect` 增加可序列化的延迟端口，默认 `0.25` 秒。
- 为核心战斗相关 Inspector 端口增加中文名称和必要说明。
- 保持既有序列化字段名和资源数值，除新增双重射击延迟字段外不重写 Prefab、场景或 SO 数据。

## 非目标

- 不制作全局时间管理器或跨场景单例。
- 不实现循环计时器、暂停单个任务、任务 ID、存档恢复或编辑器时间轴。
- 不改变射速、换弹、瞄准、sorting、Prefab Layer 或玩家 Transform。
- 不让延迟产生的第二发再次触发开火事件，避免递归双重射击。

## 架构

### BulletEventTimeScheduler

`BulletEventTimeScheduler` 是不继承 `MonoBehaviour` 的普通 C# 类。它只负责保存和执行定时任务，不直接读取 `UnityEngine.Time`。

公开职责：

- `Schedule(now, delay, callback)`：按当前游戏时间登记任务。
- `Tick(now, onException)`：执行所有到期任务。
- `Clear()`：释放尚未执行的任务。

调度规则：

- 负延迟按 `0` 处理。
- 使用到期时间排序；同一到期时间按登记顺序执行。
- 执行前先从队列移除，回调抛错也不会重复执行。
- 单个回调异常交给调用方记录，不阻止其他到期任务。
- `Tick` 期间新登记的任务留到下一次 `Tick`，避免零延迟事件形成同帧无限递归。

调度器接收显式 `now`，因此可用确定时间进行 EditMode 单元测试。`DiceRevolverGun` 传入 `Time.time`，所以 `Time.timeScale = 0` 时延迟自然暂停。

### DiceRevolverGun

左轮拥有一个调度器实例，并在 `LateUpdate` 中完成当前帧正常射击后调用 `Tick(Time.time)`。这样本帧新登记的零延迟事件可以在本帧末处理，而执行回调时继续登记的任务会安全地推迟到下一帧。

调度器只负责“何时调用”；弹丸创建仍统一经过 `SpawnConfiguredProjectile`。换弹不会清除已登记任务。左轮销毁时清空队列，避免延迟回调持有无效对象。

### BulletEventContext

上下文新增：

```csharp
bool Schedule(float delaySeconds, Action<BulletEventContext> callback)
```

调用事件只依赖上下文，不认识左轮的队列实现。上下文不可调度或回调为空时返回 `false`。执行时回调收到原事件上下文的副本，因此能够读取原骰面、射击方向、弹丸属性、命中位置和弹巢。

既有 `RequestAdditionalShot()` 保留，避免破坏已经存在的事件代码。

### ExtraShotOnFireEffect

新增字段：

```text
第二发延迟（秒） = 0.25
```

触发时不立即请求额外弹丸，而是调用 `context.Schedule`。到期后使用原 `DiceRevolverShotContext` 请求第二发，因此第二发保持第一发的：

- 骰面与词条
- 弹丸 Prefab 和运行时属性
- 发射位置
- 发射方向

玩家在延迟期间移动或转向不会修改已经登记的第二发。换弹不取消第二发。第二发由 `SpawnConfiguredProjectile(..., false)` 创建，不递归触发开火词条。

## Inspector 中文端口

使用 `[InspectorName]`、中文 `[Header]` 和必要的 `[Tooltip]`，不重命名 C# 字段，确保 Unity 序列化引用和值保持不变。

覆盖范围：

- `DiceRevolverGun`：引用、持枪、骰池、射速、换弹和换弹视觉参数。
- `DiceFaceEntry` 与扩展端口：显示信息、弹丸类型、Tag、伤害、距离、速度、穿透和三类事件。
- `Projectile`：默认速度与默认生命周期。
- `ExtraShotOnFireEffect`：第二发延迟。
- `ExplosionOnHitEffect`：爆炸弹丸 Prefab。
- `DiceFaceLibrary`、`BulletEventLibrary`、`DiceFaceLoadout`：库和六面装备数组。

不处理角色移动、瞄准、镜头、HUD 和构筑 UI 字段。

## 错误处理

- 空回调不进入队列。
- 负延迟归零。
- 一个回调异常时由左轮使用 `Debug.LogException` 记录，其他任务继续执行。
- 左轮或场景对象销毁后不再执行其队列中的任务。
- 缺少调度能力时，事件返回失败，不静默改成立即执行。

## 测试

EditMode 测试覆盖：

1. 到期前不执行，到期时只执行一次。
2. 同一时刻按登记顺序执行。
3. 一个任务抛错不阻止后续任务。
4. `Tick` 中新登记的任务推迟到下一次 `Tick`。
5. `BulletEventContext.Schedule` 正确传递延迟和上下文。
6. 双重射击默认提交 `0.25` 秒延迟，提交时不立即请求第二发，到期回调执行后只请求一次。
7. 既有额外射击递归保护继续通过。
8. 完整 EditMode 回归通过。

## 扩展方式

后续事件只需调用：

```csharp
context.Schedule(delaySeconds, delayedContext =>
{
    // 延迟后的子弹事件逻辑
});
```

时间系统不理解爆炸、弹幕或骰面规则，因此新增事件不会要求修改调度器。只有出现明确需求时才增加取消或循环能力。
