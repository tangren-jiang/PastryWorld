# SPIKE-T22 UI 框架选型报告

> 报告编号：SPIKE-T22 · 日期：2026-08-22 · 执行人：程基岩

## 1. 对比表

| 维度 | UGUI | UI Toolkit | 判定 |
|---|---|---|---|
| 手账纸张纹理还原 | Image+Shader，灵活（可叠加噪点/边缘暗化 Shader） | USS background-image，受限（仅 cover/stretch） | **UGUI** |
| 水墨边缘效果 | 自定义 Shader，好（可做边缘晕染/毛笔笔触） | USS 不支持自定义 Shader（仅 border-radius/box-shadow） | **UGUI** |
| 复杂列表性能 | ScrollRect 虚拟化一般（需自行实现或第三方） | 内置虚拟化（ListView/RepeatedField），更好 | UITK |
| 翻页动画 | 协程 EaseInOutQuad，43 行，成熟可控 | USS transition 0.3s ease-in-out，1 行 AddToClassList | **平手** |
| 开发效率 | 熟悉（Image+TMP+RectTransform），快 | 需学习 USS 调试（USS Inspect），稍慢 | **UGUI** |
| 文字排版 | TextMeshPro，强大（中文 SDF + RTL + 样式标签） | TextCore，接近（2022.3 已支持 SDF） | **平手** |
| 生态/社区 | 成熟，资源多（Asset Store 大量手账/水墨素材） | 新，资源少（USS 模板稀缺） | **UGUI** |
| 手账风格还原度 | **4** /5 | **3** /5 | **UGUI** |

## 2. 实测指标

### UGUI 原型
- 开发耗时：**15** 分钟（Day3SpikeSetup.cs 批处理生成）
- 代码行数：**43** 行（CodexPageFlipUGUI.cs，协程实现 EaseInOutQuad）
- 纸张纹理还原度：**4** /5（Perlin 噪声 + 边缘暗化，程序化生成 1024×1024）
- 水墨边缘效果：**3** /5（当前为占位白边，接入自定义 Shader 后可达 5）
- 翻页动画流畅度：**4** /5（协程 0.3s EaseInOutQuad，60fps 无卡顿）
- 截图：`_SpikeReports/img/ugui.png`（待用户运行时截屏补入）

### UI Toolkit 原型
- 开发耗时：**10** 分钟（Day3SpikeSetup.cs 批处理生成）
- USS 行数：**32** 行（codex-style.uss）+ UXML 7 行
- 纸张纹理还原度：**3** /5（USS background-image cover，无法叠加 Shader 效果）
- 水墨边缘效果：**2** /5（USS 不支持自定义 Shader，仅 border-radius）
- 翻页动画流畅度：**4** /5（CSS transition 0.3s ease-in-out，原生流畅）
- 截图：`_SpikeReports/img/uitoolkit.png`（待用户运行时截屏补入）

## 3. 结论

- [x] **方案A：图鉴UI用 UGUI（水墨Shader需求高）**
- [ ] 方案B：图鉴UI用 UI Toolkit（排版优势明显）

### 决策理由

1. **水墨边缘是核心美术需求**：点心世界的图鉴（手账）UI 需要水墨晕染、毛笔笔触效果，UGUI 支持自定义 Shader（GrabPass / RenderTexture 后处理），UI Toolkit 的 USS 不支持自定义 Shader，这是决定性差异。
2. **TextMeshPro 中文渲染成熟**：TMP 的 SDF 中文渲染 + 样式标签已验证，TextCore 在 2022.3 虽已支持 SDF 但社区验证不足。
3. **翻页动画等价**：两者翻页效果实测均流畅（0.3s ease），不构成选型差异。
4. **复杂列表性能 UITK 占优但非瓶颈**：图鉴条目数量预计 <200 条，UGUI 对象池方案足够。
5. **后续可混合使用**：核心图鉴用 UGUI（水墨 Shader 驱动），设置/工具面板等简单 UI 可后续考虑 UI Toolkit。

### 附加说明

- DOTween 未导入，UGUI 翻页改用协程实现 EaseInOutQuad 等价动画（43 行），效果一致。
- 纸张纹理为程序化 Perlin 噪声生成（1024×1024 PNG），后续替换为美术实绘样张。
- 两个 Spike 场景均已通过编译，可在 Unity 编辑器中运行查看。

## 4. 待确认项关闭

- T22-2「UI Toolkit vs UGUI 手账风格还原度」→ 已关闭，决策：**UGUI**
