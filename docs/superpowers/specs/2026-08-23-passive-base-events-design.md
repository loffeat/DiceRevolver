# 被动事件迁移为被动型基础事件 — 设计规格

- 日期：`2026-08-23`
- 状态：`draft`
- 关联工作流：`2026-08-23-passive-base-events`

## 目标

把当前"骰面第五被动槽"的被动事件模型，迁移为"被动型基础事件"：

1. 被动事件占据**整个骰面**（装在基础槽，带被动标志），该面**永不入抽池**。
2. 被动面数量 N 直接决定每轮总可抽取骰面数 = `6 − N`。
3. 被动事件与现状一致：**只要符合触发条件就随时生效**（弹丸生成/命中、抽面候选、换弹等信号）。

## 非目标

- 不新增新的被动触发信号或"敌人着火"等新条件类型（用户示例仅作为未来扩展方向，本版不实现）。
- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer 或 DiceRevolverGun 调参数值。
- 不实现构筑持久化存档。
- 不限制被动面数量（允许 0–6，含全被动）。

## 背景与现状

- `DiceFaceSlotType` 5 值（Base/OnFire/OnHit/OnFireEnd/Passive）；`DiceFaceConfiguration` 每面 5 槽。
- 被动执行有两条路径：旧 `PassiveEventEffect` → `DicePassiveRuntime`（抽面约束/优先级、弹丸观察、属性修正、奖励激活）；Rule 被动 → `DiceEventRuleRuntimeSet` 的 Passive 槽索引（特斯拉、呼应协同、收尾者）。
- `DiceRevolverRuntime` 换弹时把 1..6 全量入池；抽空自动换弹，`R` 手动换弹。
- 奖励射击（`ExecuteBonusShot`）**不消耗骰面**（只有正常抽面从弹巢移除），支持"触发指定骰面事件且不消耗骰面"。
- 10 个词条均为 Rule-backed；3 个被动词条资产（`Tesla.asset`/`EchoSynergy.asset`/`Finisher.asset`）`slotType: 4`。
- 受保护 Prefab 的 loadout 不含被动数据（TestRobot 的 `faceConfigurations` 全为空 4 槽、Player 无序列化配置）。
- **数据现状（重要）**：会话期间 3 个被动规则资产被编辑器改动，`allowedSlots` 从提交值 `16`（Passive）变为工作区值 `2/4/1`（Tesla=OnFire、EchoSynergy=OnHit、Finisher=Base）。本规格要求被动规则的 `allowedSlots` 必须包含 `基础(1)`（见迁移节），迁移会归一处理并校验。

## 已确认决策

| # | 决策 | 选择 |
|---|---|---|
| D1 | 被动槽去留 | **彻底删除第五被动槽**；被动只以"被动型基础事件"存在 |
| D2 | 被动面结构 | **纯被动面**：无活动槽、永不入抽池；被动基础事件本身可以是开火/命中类触发（如"敌人着火时触发骰面 4 事件且不消耗骰面"） |
| D3 | 被动面数量 | **不限（0–6）**；全被动给出警告（非错误） |
| D4 | 被动面标记方式 | **词条级标志**：`DiceFaceEntry.isPassiveBase` |
| D5 | 抽池排除实现 | **池级排除**：换弹填充时只把非被动面放入剩余池 |
| D6 | 构筑变更语义 | 构筑改变即**立即重建活动池**（重置本轮进度，原型接受） |

## 数据模型

1. **删除被动槽**：`DiceFaceConfiguration` 移除 `passiveEntry`，每面 4 槽。`DiceFaceSlotType.Passive` 从公共模型移除；序列化整数值 `4` 仅作旧资产兼容读取，迁移时转为"被动基础词条"。
2. **词条标志**：`DiceFaceEntry` 新增 `[SerializeField] bool isPassiveBase`（InspectorName"被动型基础"）。带标志的词条只能装进**基础槽**；装进其他活动槽 → 校验错误。同一规则既可作普通基础也可作被动基础（由词条标志决定）。
3. **规则掩码**：`DiceFaceSlotMask` 删除 `Passive` 位；规则"事件类型"只剩 基础/开火时/命中时/开火后/主动/全部。被动规则 allowedSlots 必须含 `基础(1)`。
4. **快照**：`DiceFaceConfigurationSnapshot` 删除 `GetPassiveEffect()`，新增 `IsPassiveFace`（由 baseEntry.isPassiveBase 推导）；`DiceFaceLoadout` 新增 `GetPassiveFaceSet()`。

## 运行时语义

1. **池级排除**：`DiceRevolverRuntime` 增加被动面集合；换弹/初始化只填充非被动面。`RemainingRounds` 与 HUD 弹药数 = `6 − N`；活动面抽空 → 自动换弹；`R` 手动换弹照旧；换弹只重置活动池，被动面永远留在弹巢；被动实例状态（如特斯拉层数）在换弹完成时重置（与现状一致）。
2. **被动监听绑定**：`DiceEventRuleRuntimeSet` 的被动服务改为遍历**被动面的 base 槽规则**（取代原 Passive 槽索引）；触发逻辑不变（信号匹配即生效）。
3. **抽面约束作用于活动池**：收尾者抽取优先级、`AllowsDraw` 过滤继续在活动面之间生效。
4. **构筑变更**：被动面集合变化 → 立即重建活动池（`DiceRevolverRuntime.RebuildActiveFaces(passiveFaces)`）。
5. **旧 legacy 被动路径**：`DicePassiveRuntime` 与 `PassiveEventEffect` 的绑定入口（gun/loadout 中被动槽 `RebuildFace`）删除；抽象类与隐藏 legacy 字段按既有兼容边界保留。

## 编辑器与构筑 UI

1. **规则编辑器**：左栏分类 5→4（删"被动"）；被动规则归入"基础"分类；"事件类型"掩码不再显示被动位。
2. **词条 Inspector**：`DiceFaceEntry` 新增"被动型基础"勾选；勾选后只能进基础槽。
3. **构筑页**：每面 5 行→4 行；被动面基础槽显示"被动"徽标，面卡片标注"该面不参与抽取"；选择被动基础词条时给出说明。
4. **校验器**：被动词条进非基础槽 → 错误；全被动（6/6）→ 警告；被动规则 allowedSlots 不含基础 → 错误。

## 迁移

1. **3 个被动词条资产**：`slotType: 4` → `slotType: 0` + `isPassiveBase: 1`；扩展 `EventRuleMigrationUtility` 实现，幂等，保留其余字段。
2. **3 个被动规则资产**（TeslaRule/EchoSynergyRule/FinisherRule）：`allowedSlots` 归一为 `基础(1)`（消除工作区 2/4/1 与提交值 16 的不一致；迁移后校验 `AllowsSlot(Base)` 成立）。
3. **`LightningBuildPrototypeBuilder`**：创建被动规则改用"基础槽 + 被动标志"新语义。
4. **受保护资产零改动**：迁移不触碰 Player/TestRobot/TargetDummy Prefab；迁移后 SHA256 门禁确认未改写。
5. **代码清理**：规则编辑器"被动"分类、`EventRuleValidator.PassiveStateSupported` 分支、迁移工具被动分支、`DiceFaceSlotTypeLabels` 被动标签、`EventRuleDefinition.ToMask` Passive 分支等移除。

## 测试策略

- **EditMode**：
  - 池级排除：N 被动 → 每轮池 = 6−N；抽空自动换弹；R 手动换弹；换弹重置活动池与被动实例状态。
  - `GetPassiveFaceSet` 计算正确。
  - 词条校验：被动词条只能进基础槽（错误）；全被动警告。
  - 被动规则仍按信号生效：特斯拉层数、呼应协同不消耗骰面奖励激活、收尾者优先级作用于活动池。
  - 迁移幂等：重复执行结果稳定；迁移后 `AllowsSlot(Base)` 成立。
  - 构筑改变即重建活动池。
- **静态门禁**：`DiceFaceSlotType.Passive` 在 Assets 零引用（脚本与资产）；`DiceFaceSlotMask.Passive` 位不再使用；10 个受保护文件 SHA256 与迁移前一致。
- **人工验收**：构筑页 4 行 + 被动徽标；HUD 弹药数 6−N；PlayMode 被动触发表现。

## 成功标准

- 被动只以"被动型基础事件"存在；被动面数量直接决定每轮可抽面数。
- 被动规则触发行为与迁移前一致（条件满足即生效，含不消耗骰面的奖励激活）。
- 受保护资产零改写；既有测试迁移后全绿（除既有豁免项）。
- 规则编辑器与构筑页不再出现"被动槽"概念。

## 风险与未决

- 用户工作区对 3 个被动规则的 `allowedSlots` 手动改动（2/4/1）与本规格目标（1）不一致 → 迁移归一并在评审中确认。
- 构筑改变即重置本轮进度（D6）可能影响手感 → 原型接受，人工验收时确认。
- 全被动（0 抽）场景只能靠被动间互动 → 设计者自担，仅警告。
