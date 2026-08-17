# DiceRevolver 项目状态

## 当前阶段

- 可玩原型阶段；骰面构筑主链路和可移植的 Codex 项目上下文系统均已完成当前计划范围。

## 全局有效状态

- `main` 的当前基线包含顶视角移动、骰子左轮、弹药 HUD、运行时构筑页和三个示例词条。
- `codex/portable-context-system` 已完成上下文系统实现，隔离工作树未修改 Unity 运行内容。
- 项目上下文框架位于 `.project-context/framework/`，项目实例资料位于 `.project-context/project/`。

## 活跃工作流

- 无

## 完成历史

- [2026-08-16 骰面构筑系统](workstreams/2026-08-16-dice-face-build-system/STATE.md)（`completed`）
- [2026-08-17 可移植项目上下文系统](workstreams/2026-08-17-portable-project-context-system/STATE.md)（`completed`）

## 已知缺口

- `ExplosionOnHitEffect.asset` 尚未配置爆炸弹丸 Prefab，触发时只输出 warning。
- 伤害和敌人穿透数已存入弹丸运行时属性，但没有完整敌人生命与穿透消费逻辑。
- 骰面构筑仅存在于当前运行期，没有存档。
- 尚未执行完整 PlayMode 战斗流程验证。
- 当前设备的 Git LFS clean filter 无法写入 `.git/lfs/tmp`，导致部分全仓库 Git 状态或 diff 检查失败。

## 最近项目级验证

- [passed] `2026-08-17`：检查器 10 项验收测试通过，包含 Unity、Web、Library、错误结构和默认根目录调用。
- [passed] `2026-08-17`：安装器 13 项验收测试通过，覆盖预览、幂等、安全合并、备份、重新绑定、升级和默认根目录调用。
- [passed] `2026-08-17`：`ExistingProject -Apply` 在当前仓库创建入口和项目资料后，结构检查返回成功。
- [passed] `2026-08-17`：真实仓库 `Repair` 预览返回 `no changes`，执行前后路径限定 Git 状态一致。
- [passed] `2026-08-17`：仅按 `AGENTS.md` 和上下文协议读取项目资料的冷启动演练，可以恢复项目结构、工作流、下一动作和已知限制。
- [not-run] `2026-08-17`：Unity EditMode 与 PlayMode 测试未运行，当前设备未发现项目文档所示的 Unity 可执行文件。
