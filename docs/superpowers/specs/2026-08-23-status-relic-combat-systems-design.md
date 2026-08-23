# 状态·遗物·收尾者·特斯拉·呼应协同 战斗系统设计

- 日期：`2026-08-23`
- 状态：`draft`
- 关联工作流：`2026-08-23-status-relic-combat-systems`

## 目标

为 DiceRevolver 实现用户预想 Build 所需的五组战斗能力，全部按已确认决策：

1. **负面效果框架**：通用敌人状态系统（`EnemyStatusHost` + 可配置状态定义），点燃（持续伤害）为第一个实例；敌人引入有限生命与死亡。
2. **遗物框架**：遗物 SO + 每轮开始钩子；"出千"为第一个实例（每轮首抽强制指定面，被动面不生效）。
3. **收尾者重做**：收尾者 = 基础事件（必定最后出现 + 使用时生成特殊穿甲左轮子弹）；抽牌评估覆盖普通面基础规则。
4. **特斯拉重做**：开火时按本轮雷电弹幕数增伤（buff 装备面基础事件伤害；基础无伤害则不生效）。
5. **呼应协同重做**：触发目标改为"与自己相邻的骰面"（按构筑 UI 布局），触发源为"敌人被点燃"。

## 非目标

- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer 或 DiceRevolverGun 调参数值（受保护资产）。
- 不做构筑页遗物 UI（遗物本版由 Inspector 配置）；不做遗物获取/商店。
- 不实现除点燃外的其他负面效果（框架预留，后续新增状态资产即可）。
- 不实现敌人 AI/掉落/击杀奖励。
- 被触发面执行**完整四阶段**（D5），不做"部分激活"模式。

## 已确认决策

| # | 决策 |
|---|---|
| D1 | 负面效果 = 通用框架；点燃为第一个实例 |
| D2 | 敌人有限生命 + 死亡 |
| D3 | 收尾者 = 基础事件（最后出现 + 穿甲弹）；抽牌评估覆盖普通面基础规则 |
| D4 | 呼应协同触发目标 = 严格相邻（面 3 → {1,2,4,6} 含面 4） |
| D5 | 被触发面完整激活（含开火后，零新机制） |
| D6 | 遗物 = 通用框架；出千为第一个实例 |

## 第 1 节：负面效果框架 + 敌人生命/死亡

### 敌人生命（D2）

- `EnemyHealth : MonoBehaviour, IDamageReceiver`：可配置 `maxHealth`；`ReceiveDamage` 扣血并广播 `DamageReceived`（飘字流程不变）；`Died` 事件；死亡行为可配置（禁用/销毁）。
- `TargetDummy` 迁移为有限血测试靶：可配置 HP，死亡后重置并继续可被打（保留测试语义）。
- 死亡时清空状态；重生后重新可施加。

### 状态框架（D1）

- `EnemyStatusHost : MonoBehaviour`：挂敌人，持有状态实例集合。
  - `EnemyStatusDefinition`（ScriptableObject）：ID/名称/描述/持续时间/每秒伤害/叠加策略（不可叠加刷新 / 可叠层）/视觉提示色。
  - 运行时实例按定义 ID 索引；`Update` 驱动 DoT 结算（每秒一跳，可配频率）；叠加按策略刷新或加层；到期移除；死亡清空。
  - 施加状态时广播 `EnemyStatusApplied`（供第 5 节信号与未来触发）。
- **通用模块**：
  - 结果模块 `ApplyEnemyStatusResultModule`：向命中目标施加指定状态（目标 = 命中对象上的 `EnemyStatusHost`）。
  - 条件模块 `HasEnemyStatusConditionModule`：检查目标是否处于指定状态。
- **点燃** `IgniteStatus`：第一个状态资产（持续时间 + 每秒伤害；伤害经 `IDamageReceiver` 投递，触发飘字）。

## 第 2 节：遗物框架 + 出千

- `RelicDefinition`（ScriptableObject）：名称/描述/效果参数；实现"每轮开始"效果接口 `ApplyAtRoundStart(RelicContext)`。
- `RelicRuntime`（挂枪械或独立组件）：持有遗物列表（Inspector 配置）；监听换弹完成 → 依次调用 `ApplyAtRoundStart`。
- **出千** `LoadedFirstFaceRelicDefinition`：参数=目标面号；换弹完成时调用 `DiceRevolverRuntime.SetFirstDrawForce(face)`（现有 `forcedNextFace` 机制，首抽必为该面）；**被动面校验**：目标面在被动面集合中 → 不生效。
- `DiceRevolverRuntime` 增加 `SetFirstDrawForce(face)`；**修复 `TryRefillAndForceNextFace` 与 `SetFirstDrawForce` 的被动面守卫**（强制面/填充面拒绝被动面，防止被动面被拉回抽池）。

## 第 3 节：抽牌评估扩展 + 收尾者新规则

- `DiceEventRuleRuntimeSet.FilterDrawCandidates` 的 DrawCandidate 评估从"只遍历被动面"改为"遍历**所有面**的基础槽规则"（普通面基础规则参与抽牌优先级/拒绝判定）；信号监听 `ExecutePassive` 仍只遍历被动面（互不干扰）。
- **收尾者 = 基础事件规则**（面 5 基础槽）：
  - 触发器 `SignalTypeTriggerModule` 掩码 = `DrawCandidate | Base`
  - 结果 1：`SetDrawPriorityResultModule` + 局部条件 `SignalTypeCondition(DrawCandidate)` → 面 5 必定最后出现
  - 结果 2：`SpawnProjectileResultModule`（primary）+ 局部条件 `SignalTypeCondition(Base)` → 使用时生成穿甲弹
- **穿甲弹资产**：新增 `ProjectileDefinition`（长型视觉、更高穿透上限/更多穿透对象；复用现有通用穿透机制）。
- 面 5：基础=收尾者、开火时=特斯拉、命中/开火后留空（普通面）。

## 第 4 节：特斯拉开火时增伤

- **运行时统计** `RoundProjectileStatistic`（每枪）：按弹丸 Tag/类型计数，弹丸生成时递增，换弹时重置。
- **特斯拉新规则**（面 5 开火时槽）：
  - 触发器 `SignalTypeTriggerModule` = `OnFire`
  - 结果：新增模块 **"按计数增加本次激活弹丸伤害"**（读取本轮雷电弹幕统计，给当前激活设置伤害倍率/加成）
  - 生效路径：激活内生成的弹丸（含基础槽收尾者的穿甲弹）携带加成；**本次激活未生成弹丸（基础无伤害）→ 加成无处施加 → 自然不生效**（用户语义）。
- 旧被动版特斯拉（ProjectileSpawned 计数 + BeforeProjectileStats 倍率）退役，迁移为开火时语义。

## 第 5 节：呼应协同相邻触发

- **新信号** `EnemyStatusApplied`：`EnemyStatusHost` 施加状态时广播，携带目标与状态；`EventSignal` 增加目标引用字段。
- **呼应协同新规则**（面 3 被动面基础槽）：
  - 触发器 `SignalTypeTriggerModule` = `EnemyStatusApplied`
  - 规则级条件：`HasEnemyStatusConditionModule`（点燃）
  - 结果：新增模块 **"触发相邻骰面"** `TriggerAdjacentFacesResultModule`——按装备面计算相邻面，逐面请求**完整激活**（`ExecuteBonusShot`，不消耗骰面，符合 D5）；带每轮触发次数上限（沿用计数键机制）。
- **相邻表** `DiceFaceAdjacency` 静态工具：按构筑 UI 布局（`DiceBuildPageUI.FacePositions` 网格）计算 8 向邻接；已验证面 3 → {1,2,4,6}。

## 编辑器/资产变更

- 新资产：点燃状态定义、穿甲弹 `ProjectileDefinition`、出千遗物定义、收尾者/特斯拉/呼应协同新规则（重构既有规则资产，保留幂等迁移）。
- 规则编辑器无需新 UI（新模块经 `TypeCache` 自动发现；新信号出现在触发器/条件掩码中）。

## 测试策略

- **EditMode**：
  - `EnemyHealth`：扣血/死亡/重置；`EnemyStatusHost`：施加/叠加刷新/叠层/到期/DoT 结算频率/死亡清空。
  - `ApplyEnemyStatusResultModule` / `HasEnemyStatusConditionModule`：目标解析、条件判定。
  - 遗物：出千在换弹完成时设置首抽强制面；被动面不生效；`TryRefillAndForceNextFace`/`SetFirstDrawForce` 拒绝被动面。
  - 抽牌评估：普通面基础规则（收尾者）参与 DrawCandidate 优先级；被动面信号监听不受影响。
  - 收尾者：DrawCandidate → 设置优先级；Base → 生成穿甲弹。
  - 特斯拉：开火时按统计增伤；无弹丸激活不生效；统计换弹重置。
  - 呼应协同：`EnemyStatusApplied` 点燃信号 → 触发相邻面完整激活（含面 4）、不消耗、次数上限。
  - 迁移幂等：重构规则资产重复执行稳定。
- **静态门禁**：受保护资产 SHA256 不变；`DiceFaceSlotMask.Passive` 等零引用保持。
- **人工验收**：构筑出千+收尾者+特斯拉+呼应协同 Build 的 PlayMode 循环表现。

## 成功标准

- 预想 Build 循环可运行：出千锁 4 → 面 4 雷电球/双发/点燃/链式反应 → 点燃触发呼应协同相邻激活（126 生成弹幕 + 面 4 再激活）→ 强制 4 点循环 → 收尾者最后抽到并受特斯拉增伤打出高伤害穿甲弹。
- 负面效果框架可扩展：新增状态只需新 `EnemyStatusDefinition` 资产。
- 遗物框架可扩展：新增遗物只需新 `RelicDefinition` 资产。
- 受保护资产零改写；EditMode 全绿（除既有豁免项）。

## 风险与未决

- 相邻触发包含面 4：面 4 被触发时链式反应排队覆盖可能造成额外循环——设计意图（D4/D5），PlayMode 手感待验收。
- 特斯拉增伤公式（每层倍率）与统计口径（按 Tag vs 按类型）参数化，默认值待设计资产时定。
- DoT 频率与每秒伤害数值为参数，待调优。
