# 骰面四槽位配置设计

## 目标

把每个骰面从“装备一个包含若干阶段事件的复合词条”改为四个互不冲突的独立槽位：

1. 基础事件
2. 开火时事件
3. 命中时事件
4. 开火后事件

每个槽位可为空且最多装备一个词条。替换任意槽位时不得影响同一骰面的其他槽位。

## 数据模型

- `DiceFaceSlotType` 枚举定义 `Base`、`OnFire`、`OnHit`、`OnFireEnd` 四个阶段。
- `DiceFaceEntry` 继续负责词条显示信息，但只绑定一个槽位类型和一个 `BulletEventEffect`。
- `DiceFaceConfiguration` 保存一个骰面的四个 `DiceFaceEntry` 引用，并按槽位读取或替换。
- `DiceFaceConfigurationSnapshot` 在抽中骰面时复制四个槽位引用。该次激活、延迟弹丸和飞行中弹丸始终使用这份快照，不受之后构筑操作影响。
- `DiceFaceLoadout` 保存六个 `DiceFaceConfiguration`，提供按骰面和槽位装备、读取及快照接口。

## 兼容策略

现有 Player Prefab 中的 `entries` 和 `baseEffects` 保留为隐藏的旧数据，不重存 Player Prefab。`DiceFaceLoadout` 首次读取某面时把旧词条按其唯一事件阶段映射到对应新槽位，并把旧基础事件作为基础槽位后备值。

现有三个示例词条迁移为：

- `DoubleTap`：开火时槽位，效果为 `ExtraShotOnFireEffect`。
- `BlastRound`：命中时槽位，效果为 `ExplosionOnHitEffect`。
- `LoadedFour`：开火后槽位，效果为 `ForceFaceFourOnFireEndEffect`。

新增 `BasicShot` 基础词条，效果为 `FireBasicRevolverProjectile`。六个骰面原有的基础左轮子弹绑定保持有效。

## 运行时流程

抽中骰面后，`DiceRevolverGun` 获取该面的配置快照，并创建 `DiceFaceActivation`：

1. 广播 `FireStarted`。
2. 触发基础槽位。
3. 触发开火时槽位。
4. 广播 `FireEnded`。
5. 触发开火后槽位。
6. 带有命中事件资格的弹丸命中时，触发快照中的命中时槽位。

四个槽位共享现有单次激活事件预算和时间调度器。空槽位直接跳过；单个效果异常继续由现有异常隔离处理，不阻止其他低关联系统运行。

## 构筑页面

- 右侧词条库显示词条所属槽位类型。
- 先选择词条再点击骰面，只替换该词条对应的一个槽位。
- 左侧每个骰面显示基础、开火、命中、开火后四行摘要。
- 装备变化事件包含骰面编号和槽位类型，只刷新受影响骰面。

## 保护范围

- 不修改 Player Prefab 的 AimRoot、ArmVisual、GunBody、Muzzle Transform 或 Sorting Layer。
- 不修改 `DiceRevolverGun` 已有射速、换弹时间、弹速或其他用户调参。
- 不运行会重建完整场景或 Player Prefab 的生成器。
- 不把暂停中的测试机器人需求混入本次改动。

## 验证

- EditMode 数据模型测试：四槽位独立、同槽替换、越界保护、旧数据兼容、快照稳定。
- EditMode 运行时测试：四阶段触发顺序和命中快照。
- EditMode UI 测试：选择不同类型词条后可同时装备到同一骰面，并显示四行状态。
- 资源契约测试：四个示例词条类型正确，六面基础射击仍有效。
- 完整 EditMode 回归和项目上下文检查。

