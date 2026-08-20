# DiceRevolverGun 核心重构设计

## 目标

在不改变现有玩法的前提下，把集中在 `DiceRevolverGun` 中的左轮规则、骰面事件执行和 Unity 场景职责拆开，形成可直接测试、可继续承载雷电构筑的深模块。

本轮同时解决以下已确认问题：

- 项目领域固定为六面骰，但 Gun 暴露可配置 `faceCount`，其他模块又硬编码 `6`。
- Gun 同时管理输入、姿态、弹巢、射速、换弹、事件、弹丸和命中，职责过多。
- 射击集成测试大量通过反射访问 Gun 私有实现。
- `Projectile` 和 `ProjectileHitReporter` 分别处理同一次碰撞。
- 词条可通过 `BulletEventContext` 获取整个 Gun 和 Chamber，权限过宽。
- 事件预算固定为代码常量，无法从 Inspector 调整。

## 强制兼容原则

本轮是结构重构，不加入雷电玩法，也不改变以下可观察行为：

- 六面不放回随机抽取。
- 当前射速、手动换弹、空膛自动换弹和换弹时长。
- 基础、开火时、开火后、命中时四槽位规则及执行顺序。
- 配置快照、DoubleTap 延迟、主弹与附加弹命中资格。
- LoadedFour 补回骰面 4 并将其设为下一发的行为。
- BlastRound 的直击与范围爆炸行为。
- 单次激活共享事件预算、延迟回调和异常隔离。
- Player、TestRobot、HUD 和现有 ScriptableObject 资源的玩法表现。

## 领域规则

### 固定六面

新增 `DiceRevolverRules.FaceCount = 6` 作为唯一骰面数量来源。

- 删除 Gun Inspector 中的 `faceCount`。
- `DiceChamber`、`DiceFaceLoadout`、构筑 UI 和 Editor Builder 都读取统一规则。
- 不把骰面数量设计为运行时配置、构造参数或未来扩展端口。

### 事件预算

每把 `DiceRevolverGun` 暴露中文 Inspector 端口“单次骰面事件预算”：

- 默认值 `32`。
- 最小值 `1`。
- 每次成功抽面时，将当前值复制到新建的 `DiceFaceActivation`。
- 同一次激活的基础、开火时、开火后、命中、延迟和额外弹丸事件共用该预算。
- 修改 Gun 配置只影响之后创建的激活，不影响飞行中弹丸或已登记的延迟事件。
- 预算耗尽时，该激活只输出一次 warning，并停止后续事件消耗。

## 模块设计

### DiceRevolverRules

项目级固定规则模块。首个且当前唯一规则为 `FaceCount = 6`。

该模块不保存运行时状态，不读取 Unity 场景或资源。

### DiceRevolverRuntime

普通 C# 深模块，拥有一把左轮当前的机械状态：

- 剩余骰面集合。
- 下一发强制骰面。
- 射速冷却。
- 是否正在换弹及换弹起始时间。
- 手动换弹和空膛自动换弹规则。

它接收当前时间和枪械意图，返回抽面或换弹状态变化，不认识以下内容：

- GameObject、Transform、Prefab、Collider。
- 玩家、AI、枪口和动画。
- DiceFaceEntry、ProjectileDefinition 和具体词条。
- HUD 或其他表现订阅者。

Runtime 对外提供受限的弹巢操作，至少包含 LoadedFour 所需的“补回并强制指定下一骰面”。调用者不能取得内部可变集合或直接替换弹巢。

自动换弹判断必须发生在开火后事件完成之后。LoadedFour 可在本次抽面耗空弹巢后补回骰面 4；此时不得错误进入自动换弹。

### DiceShotPipeline

普通 C# 深模块，负责一次骰面激活的事件生命周期：

- 接收抽中的骰面、四槽位配置快照、枪口姿态和事件预算。
- 创建 `DiceFaceActivation`。
- 按现有顺序执行基础、开火时和开火后事件。
- 接收弹丸命中并执行符合资格的命中时事件。
- 管理事件预算、延迟调度、生成请求和异常隔离。

Pipeline 不实例化 GameObject。它通过生成请求让 Unity 适配器创建弹丸。

Pipeline 也不直接操作可变 Chamber。它只持有 Runtime 提供的受限能力，用于 LoadedFour 等已批准规则。

### DiceRevolverGun

保留为 MonoBehaviour 和 Unity 适配器，职责缩减为：

- 保存序列化引用和枪械调参。
- 读取 `TopDownCharacterController` 的开火、换弹和瞄准意图。
- 获取枪口位置与方向。
- 驱动 `DiceRevolverRuntime` 与 `DiceShotPipeline`。
- 根据生成请求实例化并配置 `Projectile`。
- 播放持枪姿态和换弹闪烁。
- 转发已有射击、命中和换弹事件。

以下公开观察接口保持可用：

- `FireStarted`
- `ProjectileHit`
- `FireEnded`
- `ReloadStarted`
- `ReloadCompleted`
- `RemainingRounds`
- `IsReloading`
- `ReloadDuration`

只被测试调用且没有生产引用的 `SpawnConfiguredProjectile` 删除。新测试通过 Runtime、Pipeline 或真实 Gun 行为验证，不再为测试保留生产接口。

### DiceFaceActivation 与 BulletEventContext

`DiceFaceActivation` 继续代表一次抽面产生的完整攻击链，并保存开火时的配置快照、姿态、主弹定义和剩余预算。

删除以下过宽访问：

- `DiceFaceActivation.Gun`
- `DiceFaceActivation.Chamber`
- `BulletEventContext.Gun`
- `BulletEventContext.Chamber`

事件上下文只提供词条实际需要的能力：

- 请求生成弹丸。
- 在指定位置请求生成弹丸。
- 安排延迟事件。
- 请求补回并强制指定下一骰面。

`ForceFaceFourOnFireEndEffect` 改用受限请求，不直接读取或修改 Chamber。

### Projectile

`Projectile` 统一负责：

- 移动和生命周期。
- 碰撞过滤。
- 直接伤害。
- 命中广播。
- 销毁。

`DiceRevolverGun` 在创建弹丸时直接订阅其命中事件，并将命中转交给 Pipeline。删除 `ProjectileHitReporter` 脚本、Prefab 组件和 Builder 接线。

命中顺序固定为：捕获命中、广播命中、派发 `ProjectileHit`、执行符合资格的 OnHit、提交直接伤害、销毁对象。广播必须提供 Collider 与实际命中位置；BlastRound 的直击与范围伤害结果不得改变。

## 开火数据流

1. Gun 收集当前时间、开火意图、换弹意图和枪口姿态。
2. Runtime 推进换弹状态；换弹未完成时拒绝开火。
3. Runtime 处理手动换弹请求和射速冷却。
4. Runtime 成功抽出一个骰面，并锁定下一次允许开火时间。
5. Gun 从 Loadout 获取该面的不可变四槽位快照。
6. Gun 将骰面、快照、姿态和当前事件预算交给 Pipeline。
7. Pipeline 创建激活并依次执行 `FireStarted`、基础、开火时、`FireEnded`、开火后。
8. Pipeline 的弹丸生成请求由 Gun 实例化为 Projectile。
9. 开火后事件完成后，Runtime 再判断弹巢是否为空并决定自动换弹。
10. Gun 根据 Runtime 的状态变化转发换弹事件并更新换弹表现。

允许同一帧在换弹完成后重新开火，保持现有行为。

## 命中数据流

1. Projectile 过滤其他弹丸和现有 Player 标签规则。
2. Projectile 捕获 Collider 与命中位置，并广播命中。
3. Gun 把命中及该弹丸的 ShotContext 转交 Pipeline。
4. Gun/Pipeline 保持现有 `ProjectileHit` 对外通知。
5. 若该弹丸允许触发攻击效果，Pipeline 执行激活快照中的命中时槽位。
6. Projectile 提交直接伤害并销毁。

BlastRound 继续在命中点请求独立范围爆炸；主弹和附加弹的命中资格保持不变。

## 序列化与资源迁移

允许精确修改 Player 与 TestRobot Prefab：

- 删除失效的 `faceCount`。
- 删除确认未使用的 `reloadDropDistance`。
- 新增 `eventBudgetPerActivation: 32`。

除上述键外，不修改以下内容：

- 射速、换弹时间、持枪距离和持枪高度。
- 玩家/AI控制器、视觉根节点、枪口、碰撞体和 Loadout 引用。
- AimRoot、ArmVisual、GunBody、Transform 和 Sorting Layer。

移除所有弹丸 Prefab 上的 `ProjectileHitReporter` 组件，并更新只负责定向创建资源的 Builder。不得运行会重建完整 Player 或场景的生成器。

现有 DiceFaceEntry、ProjectileDefinition、四槽位资源与序列化兼容字段保留。

## 错误处理

- 缺少控制器或枪口时安全跳过当前帧操作。
- 缺少弹丸定义或 Prefab 时跳过该生成请求并记录 warning。
- 空槽位直接跳过。
- 单个词条抛出异常时记录异常，不破坏 Runtime 或其他激活。
- 延迟回调继续使用创建时的激活与预算快照。
- Runtime 不直接写 Unity 日志；需要呈现的错误由 Gun 或 Pipeline 适配到 Unity 日志。
- 事件预算始终至少为 `1`，耗尽 warning 每次激活最多一次。

## 测试策略

### DiceRevolverRuntime 测试

- 固定六面且一轮内不重复抽面。
- 射速冷却。
- 手动换弹、空膛自动换弹和换弹完成。
- 换弹完成后恢复六面。
- LoadedFour 补回骰面 4 后不错误自动换弹。
- 测试只通过 Runtime 接口观察结果，不读取私有字段。

### DiceShotPipeline 测试

- 基础、开火时、开火后以及后续命中时阶段的执行顺序。
- 每次激活使用不可变配置快照。
- 主弹、附加弹和攻击特效的命中资格。
- 延迟事件继续使用原激活。
- Inspector 预算传入激活并在耗尽后终止连锁。
- 事件异常隔离。
- 测试通过 Pipeline 接口观察事件和生成请求，不反射其内部实现。

### Unity 集成和资源测试

- Gun 正确把玩家和 AI 意图交给 Runtime。
- 弹丸生成位置、方向、运行时属性和所有者正确。
- Projectile 命中广播驱动直接伤害与 OnHit。
- 生产脚本、Prefab 和 Builder 中不再存在 ProjectileHitReporter。
- Player/TestRobot 只发生允许的序列化键变化。
- UI、Loadout 和 Builder 使用统一 FaceCount。
- 现有资源与完整 EditMode 回归通过。

旧的私有反射测试在新接口测试覆盖相同行为后删除或降级为少量真实 Unity 接线测试，避免新旧两套测试叠加。

实施前记录当前 `139/139` EditMode 基线。每个模块按测试先行迁移，最后运行完整 EditMode 回归和上下文检查。PlayMode 最终战斗手感仍需人工验收。

## 非目标

- 不实现雷电球、收尾者、电磁共鸣、特斯拉、呼应协同或链式反应。
- 不新增全局事件总线、全局时间单例、对象池或正式阵营系统。
- 不重做构筑 UI、角色控制、AI、伤害系统或弹丸定义结构。
- 不把六面数量重新设计为可配置能力。
- 不为尚未实现的雷电规则增加空接口或占位字段。
