# DiceRevolver 开发环境与跨设备准备

## 仓库硬性要求

- Unity Editor `6000.3.10f1`。
- Git；拉取美术和音频资源时需要 Git LFS。
- Windows PowerShell 5.1 或更高版本，用于上下文安装和检查脚本。

## 推荐配置

- 使用与 `Packages/manifest.json` 一致的 Unity 包版本。
- 在修改场景或 Prefab 前关闭其他设备上的 Unity 写入会话，避免 YAML 资源冲突。
- 功能开发使用独立分支或工作树。

## 首次克隆

1. 克隆仓库并执行 `git lfs install`。
2. 执行 `git lfs pull`，确认大文件不是指针占位内容。
3. 读取根目录 `AGENTS.md` 和 `.project-context/framework/PROTOCOL.md`。
4. 使用要求的 Unity 版本打开仓库根目录。
5. 运行上下文检查，再按可用环境运行 Unity 测试。

## 运行

在 Unity 中打开 `Assets/Scenes/TopDownShooterPrototype.unity` 并进入 Play Mode。主场景已经位于 Build Settings。

## 测试与构建

上下文检查：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
```

检查器验收测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/check-tests.ps1
```

安装器验收测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/tests/install-tests.ps1
```

Unity Editor 路径由每台设备自行发现并放入 `$UnityEditor`，然后运行：

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testResults .\Logs\editmode-results.xml -logFile .\Logs\editmode-tests.log
```

Unity Test Runner 完成后会自行退出，不要添加可能在测试开始前退出的 `-quit`。只有命令实际返回成功且结果文件无失败时，才记录为 `passed`。

## Git LFS

- 安装：`git lfs install`
- 拉取：`git lfs pull`
- 诊断：`git lfs env`
- 不要把 LFS 指针文本当成真实音频、图片或模型。

## 设备本地差异

- Unity、Git LFS 和 IDE 的绝对安装路径不写入共享资料。
- 使用 `$UnityEditor` 等任务专用变量保存本地路径。
- 本地日志和测试结果写入已忽略目录，不把设备缓存提交到 Git。

## 已知环境问题

- 当前设备执行部分全仓库 Git 检查时，Git LFS clean filter 对 `.git/lfs/tmp` 返回访问被拒绝。源代码和上下文文本读写不受影响，但不能据此声称整个工作区干净。
- 当前设备已确认可调用匹配项目版本的 Unity Editor `6000.3.10f1`；绝对安装路径属于设备本地信息，不写入共享资料。
- `2026-08-18` Unity 6000.3.10f1 已成功连接许可证服务；最近一次 EditMode 全量回归为模块化弹丸事件管线合入后的 `94/94`。
- 完整 PlayMode 战斗流程仍未运行。
