# 架构决定

## 每个骰面使用四个独立单事件槽位

- Date: `2026-08-19`
- Status: accepted

每个骰面使用基础、开火时、命中时、开火后四个互不冲突的槽位。`DiceFaceEntry` 只描述一个槽位和一个效果，`DiceFaceConfiguration` 组合四个词条。

单次抽面必须生成 `DiceFaceConfigurationSnapshot`。延迟弹丸和飞行中弹丸继续读取该快照，后续构筑操作只影响之后的抽面。

旧 `entries` 和 `baseEffects` 仅作为隐藏的序列化兼容来源，不再作为公开运行时接口。这样不需要重存 Player Prefab，也不会覆盖用户维护的角色、瞄准、sorting 或枪械调参。
