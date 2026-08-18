# 骰面四槽位配置

- ID: `2026-08-19-dice-face-four-slots`
- Status: `completed`
- Branch: working tree
- Created: `2026-08-19`
- Updated: `2026-08-19`

## 目标

- 每个骰面拥有基础、开火时、命中时、开火后四个互不冲突的单事件槽位。
- 构筑页面能够分别装备和显示四个槽位。
- 单次骰面激活使用配置快照，保持延迟和命中事件稳定。

## 非目标

- 不实现测试机器人。
- 不修改角色瞄准、手臂、sorting 或左轮调参。
- 不实现构筑存档。

## 已确认事实

- 每个骰面必须同时容纳四个互不冲突的单事件槽位。
- 基础事件也是可装备槽位，不占用其他三个阶段槽位。
- 现有 Player Prefab 的六面基础左轮子弹绑定必须继续生效。
- 本事项不允许重存 Player Prefab 或覆盖用户维护的枪械数值。

## 当前正在进行

- 无

## 已完成

- 新增 `DiceFaceSlotType`、`DiceFaceConfiguration` 和不可变配置快照。
- `DiceFaceEntry` 改为单槽位单效果词条，保留隐藏旧数组用于兼容加载。
- `DiceFaceLoadout` 支持六面四槽位独立装备、替换、查询和快照。
- `DiceRevolverGun` 按基础、开火时、开火后分发单槽事件，并让命中弹丸读取同一次激活快照。
- 构筑页每面显示四行槽位摘要，右侧词条显示槽位类型。
- 新增 `BasicShot`，并迁移 DoubleTap、BlastRound、LoadedFour 的槽位映射。
- 未重存 Player Prefab，未修改瞄准、sorting 或左轮调参。

## 下一步

1. 在当前可见 Play Mode 按 `E` 打开构筑页，人工确认四行文本在目标分辨率下清晰可读。
2. 在同一骰面依次装备四类词条并射击，人工确认表现和预期一致。

## 阻塞

- 无。

## 涉及文件

- `Assets/Scripts/Prototype/DiceFaceSlotType.cs`
- `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`
- `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- `Assets/Scripts/Prototype/DiceBuildPageUI.cs`
- `Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs`
- `Assets/Scripts/Prototype/DiceBuildEntryButtonUI.cs`
- `Assets/Scripts/Prototype/DiceBuildRuntimeView.cs`
- `Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs`
- `Assets/Resources/DiceFacePrototype/DiceFaces/BasicShot.asset`
- `Assets/Resources/DiceFacePrototype/DiceFaces/DoubleTap.asset`
- `Assets/Resources/DiceFacePrototype/DiceFaces/BlastRound.asset`
- `Assets/Resources/DiceFacePrototype/DiceFaces/LoadedFour.asset`
- `Assets/Resources/DiceFacePrototype/DiceFaceLibrary.asset`
- `Assets/Tests/EditMode/`

## 验证记录

- [failed] `2026-08-19`：数据模型红灯编译失败，缺少 `DiceFaceSlotType`。
- [passed] `2026-08-19`：四槽位数据模型聚焦测试 `10/10`。
- [failed] `2026-08-19`：激活红灯编译失败，旧接口不接受配置快照。
- [passed] `2026-08-19`：激活快照 `7/7`，枪械四阶段触发 `13/13`。
- [failed] `2026-08-19`：UI 红灯编译失败，旧骰面块只接受一个词条标签。
- [passed] `2026-08-19`：构筑 UI 聚焦测试 `7/7`。
- [failed] `2026-08-19`：资源红灯 `0/1`，缺少 `BasicShot.asset`。
- [passed] `2026-08-19`：弹丸与四槽位资源契约 `8/8`。
- [passed] `2026-08-19`：隔离 Unity 完整 EditMode 最终回归 `107/107`。
- [passed] `2026-08-19`：Player Prefab SHA256、射速和换弹时间保持任务前数值。
- [not-run] `2026-08-19`：当前可见 Play Mode 的四槽位页面排版和操作手感未人工验收。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-19-dice-face-four-slots-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-19-dice-face-four-slots.md)
