using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 逐字打字机效果（T16）。
    /// TMP maxVisibleCharacters 逐字递增。
    /// 标点停顿映射：逗号 0.1s / 句号·叹号·问号 0.3s / 省略号 0.5s / 破折号 0.2s。
    /// SkipToEnd() 立即显示全部文字（点击跳过）。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Tooltip("常规字符间隔（秒）")]
        [SerializeField, Range(0.005f, 0.2f)] private float _charInterval = 0.04f;

        [Tooltip("标点额外停顿（秒）。映射：，,、：: 0.1 / 。！？!? 0.3 / … 0.5 / — 0.2")]
        [SerializeField] private bool _punctuationPause = true;

        /// <summary>是否正在逐字显示中。</summary>
        public bool IsTyping { get; private set; }

        /// <summary>逐字显示完成（含 SkipToEnd 触发）。</summary>
        public event Action Completed;

        private TMP_Text _text;
        private Coroutine _routine;
        private string _content;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        void OnDisable()
        {
            // 场景切换等情况下终止协程
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
                IsTyping = false;
            }
        }

        /// <summary>开始逐字显示一段文字。</summary>
        public void ShowText(string content)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TypeRoutine(content ?? string.Empty));
        }

        /// <summary>跳过逐字，立即显示全部文字。</summary>
        public void SkipToEnd()
        {
            if (!IsTyping) return;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _text.text = _content;
            _text.maxVisibleCharacters = _content.Length;
            IsTyping = false;
            Completed?.Invoke();
        }

        private IEnumerator TypeRoutine(string content)
        {
            _content = content;
            _text.text = content;
            _text.maxVisibleCharacters = 0;
            _text.SetVerticesDirty();
            IsTyping = true;

            char prev = '\0';
            for (int i = 1; i <= content.Length; i++)
            {
                _text.maxVisibleCharacters = i;
                char c = content[i - 1];

                float pause = _punctuationPause ? GetPunctuationPause(c, prev) : 0f;
                yield return new WaitForSeconds(pause > 0f ? pause : _charInterval);

                prev = c;
            }

            IsTyping = false;
            _routine = null;
            Completed?.Invoke();
        }

        /// <summary>标点-停顿时长映射。省略号连续字符不重复停顿（……只停 0.5s）。</summary>
        private static float GetPunctuationPause(char c, char prev)
        {
            switch (c)
            {
                case '，':
                case ',':
                case '、':
                case '：':
                case ':':
                    return 0.1f;
                case '。':
                case '！':
                case '？':
                case '!':
                case '?':
                    return 0.3f;
                case '…':
                    return prev == '…' ? 0f : 0.5f;
                case '—':
                    return prev == '—' ? 0f : 0.2f;
                default:
                    return 0f;
            }
        }
    }
}
