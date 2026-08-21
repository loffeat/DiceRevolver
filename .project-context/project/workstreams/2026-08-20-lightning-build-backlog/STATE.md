# 雷电构筑系统

- ID: `2026-08-20-lightning-build-backlog`
- Status: `completed`
- Branch: `main`（功能分支已合并并清理）
- Created: `2026-08-20`
- Updated: `2026-08-21`

## 目标

- 将骰面扩展为基础、开火时、命中时、开火后、被动五个互不占用的槽位。
- 增加低耦合的被动 Runtime、弹丸类型/标签、穿透和弹丸归属查询能力。
- 实装雷电球、收尾者、电磁共鸣、特斯拉、呼应协同和链式反应。

## 已确认事实

- 已批准规格以五槽位、类型/标签 SO、每枪独立弹丸 Registry 和每面独立被动 Runtime 为扩展边界。
- 闪电链不属于攻击特效；呼应协同允许同帧奖励射击，但必须带自然散布。
- 链式反应只覆盖非空活动槽，不复制被动槽，并在触发后消耗自身开火后槽。

## 已完成

- `DiceFaceConfiguration` 与构筑 UI 已增加第五个被动槽，旧四槽位序列化数据保持兼容。
- 弹丸定义支持 `ProjectileTypeDefinition` 和多个 `ProjectileTagDefinition`；运行时仍保留旧字符串读取接口。
- `Projectile` 按不同 `IDamageReceiver` 计算穿透，同一目标的多个 Collider 不重复消耗穿透次数。
- `DicePassiveRuntime` 为每个 Gun、每个骰面创建独立被动实例，并隔离单个被动异常。
- 收尾者会把装备面保留到本弹夹最后；强制抽取收尾者时会等待其合法时机。
- 每把 Gun 拥有独立 `OwnedProjectileRegistry`，基础弹丸先生成并注册，再执行开火时事件。
- 雷电球默认伤害 `1`、速度 `5`、距离 `15`、穿透 `4`，带 Lightning 与 Elemental 标签，默认不是攻击特效。
- 特斯拉按同弹夹已发射的雷电弹丸为绑定面的基础弹幕提供每层 `5%` 加法伤害，换弹重置。
- 电磁共鸣从当前雷电球出发，在半径 `6` 内最多选择 `3` 个同枪拥有的雷电球生成闪电链；闪电链直接伤害为 `1`，不视为攻击特效，也不广播命中事件。
- 呼应协同按弹幕类型响应同类弹丸命中，同帧立即追加射击，最多 `4` 次；追加弹带自然散布并共享事件预算，绑定面正常射击后消耗并停止响应。
- 链式反应在开火后消耗自身，并把当前面的非空活动槽覆盖到下一次正常抽中的骰面；空槽不覆盖目标已有内容，被动槽永不复制，奖励射击不消费覆盖。
- 新词条和雷电资源只追加到资源库，没有自动装备到 Player 或 TestRobot。
- 定向 Builder 可幂等补齐缺失资源和空引用，不重写已有调参，也不加载保存受保护 Prefab 或场景。
- 功能分支已合并到 `main` 并清理；后续事件配置页面以该公开模块和资源状态为基线。

## 保护结果

- Player SHA256：`6E062B8470A5D07468FBCF9EDB85152E494DA6A7D2CF0006B1DE5A6A27B0E995`
- TargetDummy SHA256：`08BDFC2413576022A057C3E4A239DD490D3E6555D4E1D784DC3F481D5452FE33`
- TestRobot SHA256：`C6538FE8E900B51F70133654DD9DB64C47CC1637621334085F8AFDE8033E440B`
- Player 左轮参数保持：`holdDistance=0.85`、`holdHeight=0.72`、`shotsPerSecond=2`、`reloadDuration=2`。

## 验证记录

- [passed] 雷电资源、中文 Inspector、五槽位与 UI 联合 EditMode：`65/65`。
- [passed] 闪电链材质定向回归：`1/1`。
- [failed] 完整 EditMode：`233/234`，0 跳过；唯一失败仍为已获豁免的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`，原因是 Ground 为 `Y=-0.01` 而旧测试期望 `0`，没有新增失败。
- [passed] Builder 多次执行后，三个受保护 Prefab 哈希及四个 Player 左轮参数均与执行前一致。
- [not-run] 可见 PlayMode 中雷电球尺寸、闪电链表现、连锁射击散布和构筑组合手感尚未人工验收。

## 非目标

- 不自动修改 Player、TestRobot 的现有六面装备。
- 不修改 AimRoot、sorting layer、Ground 高度、角色美术和既有枪械调参。
- 不实现正式敌人生命/死亡、局内获取、存档或最终平衡。

## 当前正在进行

- 无

## 下一步

1. 在可见 PlayMode 中为 Player 手动装备新词条，验收雷电球、闪电链和同帧散布表现。
2. 根据手感调整新 ScriptableObject 上公开的半径、数量、伤害、持续时间和散布参数。
3. 后续构筑继续复用五槽位、类型/标签、被动 Runtime、归属 Registry 和共享事件预算接口。

## 阻塞

- 无。

## 涉及文件

- `Assets/Scripts/Prototype/`
- `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`
- `Assets/Tests/EditMode/`
- `Assets/Prefab/Projectiles/LightningOrb.prefab`
- `Assets/Prefab/Effects/LightningChain.prefab`
- `Assets/Resources/DiceFacePrototype/`
- `.project-context/project/`
- `docs/superpowers/`

## 相关资料

- [正式设计规格](../../../../docs/superpowers/specs/2026-08-20-lightning-build-system-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-21-lightning-build-system.md)
- [项目地图](../../PROJECT.md)
- [项目状态](../../STATUS.md)
- [左轮核心底层重构](../2026-08-20-dice-revolver-core-refactor/STATE.md)
