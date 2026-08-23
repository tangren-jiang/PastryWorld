using TMPro;
using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// TMP 中文字体解析。TMP 默认字体（LiberationSans SDF）不含 CJK 字形，
    /// 运行时从系统字体动态创建 TMP_FontAsset（macOS: PingFang SC / Windows: 微软雅黑）。
    /// </summary>
    public static class DialogueFontResolver
    {
        private static TMP_FontAsset _cached;

        /// <summary>
        /// 可显示中文的 TMP 字体。找不到时返回 null（退回 TMP 默认字体，
        /// 流程可用但中文显示为方块——仅无中文字体的机器会出现）。
        /// </summary>
        public static TMP_FontAsset ChineseFont
        {
            get
            {
                if (_cached != null) return _cached;

                string[] candidates =
                {
                    "PingFang SC", "Hiragino Sans GB", "Heiti SC",      // macOS
                    "Microsoft YaHei", "SimHei", "SimSun",              // Windows
                    "Noto Sans CJK SC", "Source Han Sans SC",           // Linux/通用
                };

                foreach (var name in candidates)
                {
                    Font osFont = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (osFont == null || !osFont.HasCharacter('中')) continue;

                    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(osFont);
                    if (asset != null)
                    {
                        _cached = asset;
                        return _cached;
                    }
                }

                return null;
            }
        }
    }
}
