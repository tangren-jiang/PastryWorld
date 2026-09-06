using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// TMP 中文字体解析。TMP 默认字体（LiberationSans SDF）不含 CJK 字形，
    /// 运行时从系统字体动态创建 TMP_FontAsset（macOS: PingFang SC / Windows: 微软雅黑）。
    ///
    /// 注意：Font.CreateDynamicFontFromOSFont 对不存在的字体名也会返回有效对象
    /// （渲染时 fallback 到系统默认字体，HasCharacter 假阳性），随后
    /// TMP_FontAsset.CreateFontAsset 打不开字体文件才失败并刷
    /// "Unable to load font face" 警告。因此先用 GetOSInstalledFontNames 过滤，
    /// 只对真实安装的字体创建，并对失败结果做负缓存。
    /// </summary>
    public static class DialogueFontResolver
    {
        private static TMP_FontAsset _cached;
        private static bool _attempted;

        /// <summary>
        /// 可显示中文的 TMP 字体。找不到时返回 null（退回 TMP 默认字体，
        /// 流程可用但中文显示为方块——仅无中文字体的机器会出现）。
        /// </summary>
        public static TMP_FontAsset ChineseFont
        {
            get
            {
                if (_cached != null) return _cached;
                if (_attempted) return null;
                _attempted = true;

                string[] candidates =
                {
                    "PingFang SC", "Hiragino Sans GB", "Heiti SC",      // macOS
                    "Microsoft YaHei", "SimHei", "SimSun",              // Windows
                    "Noto Sans CJK SC", "Source Han Sans SC",           // Linux/通用
                };

                var installed = new HashSet<string>(
                    Font.GetOSInstalledFontNames(), System.StringComparer.OrdinalIgnoreCase);

                foreach (var name in candidates)
                {
                    if (!installed.Contains(name)) continue;

                    Font osFont = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (osFont == null || !osFont.HasCharacter('中')) continue;

                    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(osFont);
                    if (asset != null)
                    {
                        _cached = asset;
                        Debug.Log($"[DialogueFontResolver] 使用系统中文字体: {name}");
                        return _cached;
                    }
                }

                Debug.LogError(
                    "[DialogueFontResolver] 未解析到可用中文字体，TMP 中文将显示为方块。" +
                    "排查方向：系统已安装 CJK 字体的名称是否在候选列表中（可在此处candidates里补上）");
                return null;
            }
        }
    }
}
