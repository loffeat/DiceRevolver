# 骰面构筑系统

- ID: `2026-08-16-dice-face-build-system`
- Status: `completed`
- Branch: `main`
- Created: `2026-08-16`
- Updated: `2026-08-17`

## 目标

- 实现六面装备、数据驱动弹丸属性、三类事件效果和按 `E` 打开的构筑页原型。

## 非目标

- 不包含词条获取、存档、商店、完整敌人生命系统或正式美术。

## 已确认事实

- 实现文件、资源和 EditMode 测试均存在于仓库。
- 当前玩家 Prefab 使用 `DiceRevolverGun`；旧枪械脚本保留作历史参考。
- 构筑页通过运行时自举按需创建，并补齐 EventSystem；按 `E` 可打开或关闭。
- 手臂瞄准通过镜像虚拟枪口和解析式解算保持左右方向一致，近距离使用稳定距离避免角度扭曲。

## 已完成

- 骰面词条、词条库和六面装备组件。
- 弹丸运行时属性与开火、命中、开火结束事件扩展点。
- 双重射击、爆炸弹和强制四点示例词条。
- 运行时构筑 UI 与对应 EditMode 测试。
- 开火前同帧刷新瞄准姿态，Gizmo 射线显示实际弹丸方向。

## 当前正在进行

- 无

## 下一步

1. 由新的工作流处理已知产品缺口，不恢复本历史工作流。

## 阻塞

- 无

## 涉及文件

- `Assets/Scripts/Prototype/`
- `Assets/Tests/EditMode/`
- `Assets/Resources/DiceFacePrototype/`

## 验证记录

- [passed] `2026-08-17`：Unity `6000.3.10f1` 隔离临时工程 EditMode 测试 `29/29` 通过。
- [passed] `2026-08-17`：只读复审未发现 Critical 或 Important 问题。
- [not-run] `2026-08-17`：完整 PlayMode 战斗流程和当前场景最终视觉对齐未运行。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-16-dice-face-build-system-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-16-dice-face-build-system.md)
- [SDD 进度](../../../../.superpowers/sdd/2026-08-16-dice-face-build-system/progress.md)
