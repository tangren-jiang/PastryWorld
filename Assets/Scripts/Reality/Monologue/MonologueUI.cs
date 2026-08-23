using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PastryWorld.Narrative;

namespace PastryWorld.Reality
{
    /// <summary>
    /// 现实独白 UI（T17）。运行时自建 UGUI（模式复用 DialogueUI，T14）。
    /// 视觉区别于点心世界对话：无立绘、无选项、无奉上，
    /// 浅灰白半透明窄条 + 细字，打字机逐字 + 点击推进。
    /// sortingOrder=600（与对话同级，独白与对话互斥不会同时出现）。
    /// </summary>
    public class MonologueUI : MonoBehaviour
    {
        [Tooltip("独白 Canvas sortingOrder")]
        [SerializeField] private int _sortingOrder = 600;

        /// <summary>点击推进（打字中先跳过打字机）。</summary>
        public event Action AdvanceClicked;

        private Canvas _canvas;
        private CanvasGroup _group;
        private TMP_Text _speakerText;
        private TMP_Text _bodyText;
        private TypewriterEffect _typewriter;

        public bool IsTyping => _typewriter != null && _typewriter.IsTyping;

        /// <summary>显示一行独白。</summary>
        public void ShowLine(MonologueLine line)
        {
            EnsureLayers();

            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;

            _speakerText.text = line.speakerName;
            _typewriter.ShowText(line.text);
        }

        /// <summary>隐藏 UI（独白结束）。不销毁层级，供下次复用。</summary>
        public void Hide()
        {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void OnAdvanceClicked()
        {
            if (IsTyping)
            {
                _typewriter.SkipToEnd();
                return;
            }
            AdvanceClicked?.Invoke();
        }

        private void EnsureLayers()
        {
            if (_canvas != null) return;

            EnsureEventSystem();

            var canvasGo = new GameObject("MonologueCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _sortingOrder;

            canvasGo.AddComponent<GraphicRaycaster>();
            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // ---- 全屏点击层（推进/跳过打字机）----
            var catcherGo = new GameObject("ClickCatcher");
            catcherGo.transform.SetParent(canvasGo.transform, false);
            var catcherImage = catcherGo.AddComponent<Image>();
            catcherImage.color = new Color(0, 0, 0, 0);
            Stretch(catcherImage.rectTransform);
            var catcherButton = catcherGo.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(OnAdvanceClicked);

            // ---- 底部窄条（浅灰白，区别于对话的墨色面板）----
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.94f, 0.93f, 0.91f, 0.9f);
            var panelRt = panelImage.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(760f, 150f);
            panelRt.anchoredPosition = new Vector2(0f, 56f);

            // 说话人名（左上，小字，灰蓝）
            _speakerText = CreateText("Speaker", panelGo.transform, fontSize: 22, bold: true);
            var speakerRt = _speakerText.rectTransform;
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.pivot = new Vector2(0f, 1f);
            speakerRt.offsetMin = new Vector2(24f, -40f);
            speakerRt.offsetMax = new Vector2(-24f, -10f);
            _speakerText.color = new Color(0.42f, 0.45f, 0.52f);
            _speakerText.raycastTarget = false;

            // 正文（Typewriter，深灰）
            _bodyText = CreateText("Body", panelGo.transform, fontSize: 24, bold: false);
            var bodyRt = _bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(24f, 12f);
            bodyRt.offsetMax = new Vector2(-24f, -44f);
            _bodyText.color = new Color(0.25f, 0.26f, 0.3f);
            _bodyText.raycastTarget = false;
            _typewriter = _bodyText.gameObject.AddComponent<TypewriterEffect>();
        }

        private TMP_Text CreateText(string name, Transform parent, int fontSize, bool bold)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            if (bold) text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = true;

            var font = DialogueFontResolver.ChineseFont;
            if (font != null) text.font = font;

            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
