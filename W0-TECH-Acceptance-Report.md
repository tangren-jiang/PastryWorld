# W0-TECH 第0周技术环境搭建验收报告

## 验收日期：2026年8月22日
## 验收人：程基岩

### 验收结果

| # | 验收项 | 标准 | 结果 | 备注 |
|---|---|---|---|---|
| 1 | Unity项目编译运行 | 无error | ✅ | batchmode 多次编译通过，InputTest 场景 Play 验证 Sprite 跟鼠标 |
| 2 | URP 2D渲染管线 | Sprite正常渲染 | ✅ | PastryWorldURP.asset 已挂载 GraphicsSettings，WhiteSquare Sprite 可见 |
| 3 | Git仓库 | 已推送 | ✅ | main(177aecf) + develop(efb649f) 已推送 GitHub tangren-jiang/PastryWorld；Day3 提交 9ec63b6 因代理 502 待补推 |
| 4 | Input System | 接口编译通过 | ✅ | IInputProvider/Mouse/Touch Provider 编译通过；PastryWorldInput.inputactions 3 Map+9 Action+3 控制方案 |
| 5 | 代码规范 | 文档已提交 | ✅ | CODING-STANDARDS.md + BRANCH-STRATEGY.md 已提交仓库根目录 |
| 6 | UI Spike | 报告产出 | ✅ | SPIKE-T22 报告决策：UGUI（水墨 Shader 需求高），T22-2 已关闭 |
| 7 | CI/CD | PR可构建 | ⚠️ | UNITY_LICENSE Secret 已配置；unity-build.yml + pr-check.yml 已提交；待补推后创建测试 PR 验证；Branch Protection 待配置 |
| 8 | 目录结构 | 齐全 | ✅ | 7 模块目录(Scripts/Input·Craft·Exploration·Narrative·Reality·Codex·Core·Utils) + 7 asmdef + ScriptableObjects/5 + Art/4 + Settings/3 + Prefabs + Resources + _SpikeReports |
| 9 | 核心骨架 | 编译通过 | ✅ | 14 个骨架类全部编译通过：SingletonBehaviour / EventBus(IEventBus) / SaveSystem(ISaveSystem) / BootstrapLoader / CraftStep / StepConfig / BeatSO / CodexEntrySO / IInputProvider / MouseInputProvider / TouchInputProvider / InputTestController / CodexPageFlipUGUI / CodexPageFlipUITK |
| 10 | 场景骨架 | 可运行 | ✅ | Bootstrap.unity(index 0) + Main.unity(index 1) + InputTest.unity + UISpike_UGUI.unity + UISpike_UIToolkit.unity，EditorBuildSettings 已配置 |

### 环境记录（执行时实测）

| 项目 | 值 |
|---|---|
| Unity 版本 | 2022.3.62f3c1（中国版，arm64） |
| 手册推荐区间 | 2022.3.50f1 ~ 2022.3.63f1 → ✅ 匹配 |
| 机器 | Apple M4 / 16GB / macOS 26.5 |
| 磁盘可用 | ~120GB（≥50GB 要求 ✅） |
| git / git-lfs | 2.50.1 / 3.7.1（~/.local/bin） |
| gh | 2.86.0（~/.local/bin，账号 tangren-jiang） |
| GitHub 仓库 | tangren-jiang/PastryWorld（私有） |

### 技术任务完成明细

| W0-EXEC 节 | 内容 | 状态 |
|---|---|---|
| Day 1 | Unity 工程 + URP 2D + Git + LFS + GitHub 仓库 | ✅ |
| Day 2 | Input Action Asset（3 Map + 9 Action + 3 控制方案） | ✅ |
| Day 3 | UI Spike（UGUI vs UI Toolkit，SPIKE-T22 报告，决策 UGUI） | ✅ |
| Day 3-4 | CI/CD（unity-build.yml + pr-check.yml + UNITY_LICENSE Secret） | ⚠️ 配置完成，运行时验证待推送 |
| Day 5 | 目录结构 + 骨架代码 + 场景骨架 + 验收报告 | ✅ |

### 遗留问题

1. **Day3 提交待推送**：commit 9ec63b6（UI Spike 成果）因代理 502 未推送到 develop。网络恢复后执行 `git push origin develop` 即可。
2. **CI 运行时验证待完成**：需推送后创建测试 PR（feature/test-ci → develop）触发 pr-check 工作流，确认 game-ci/unity-builder 能正常编译。
3. **GitHub Branch Protection 未配置**：需在 GitHub 网页 Settings → Branches 添加 develop 分支保护规则（要求 PR + 状态检查通过）。
4. **UGUI/UITK 截图待补**：SPIKE-T22 报告中截图路径为占位，需用户在编辑器中运行两场景后截屏补入 `_SpikeReports/img/`。

以上 4 项均不阻塞第 1 周开发启动，补救窗口为本周内。

### 结论
- [x] 全部通过（CI 运行时验证为非阻塞遗留项），第1周可正式启动开发
- [ ] 存在阻塞问题，需补救后重新验收

### 签字
程基岩：___________
游承峰：___________
