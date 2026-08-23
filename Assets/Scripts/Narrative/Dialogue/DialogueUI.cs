using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PastryWorld.Core;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 对话 UI（T14）。运行时自建 UGUI Canvas（模式复用 MemoryClipPlayer / InkTransitionController）。
    /// 结构：全屏点击层（点击推进/跳过打字机）+ 底部墨色面板
    /// （立绘 + 说话人名 + 正文 Typewriter + 分支按钮 + 奉上按钮）。
    /// sortingOrder=600：记忆叠加(500)之上、记忆全屏(900)之下。
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Tooltip("对话 Canvas sortingOrder（500 记忆叠加之上、900 记忆全屏之下）")]
        [SerializeField] private int _sortingOrder = 600;

        /// <summary>点击推进（无分支节点）。打字中先跳过打字机。</summary>
        public event Action AdvanceClicked;

        /// <summary>点击"奉上"按钮。</summary>
        public event Action GiftClicked;

        /// <summary>点击分支选项。</summary>
        public event Action<int> ChoiceClicked;

        private Canvas _canvas;
        private CanvasGroup _group;
        private Image _portrait;
        private TMP_Text _speakerText;
        private TMP_Text _bodyText;
        private TypewriterEffect _typewriter;
        private RectTransform _choiceContainer;
        private Button _giftButton;
        private TMP_Text _giftButtonText;
        private DialogueNode _node;

        public bool IsTyping => _typewriter != null && _typewriter.IsTyping;

        /// <summary>显示一个节点。</summary>
        public void ShowNode(DialogueNode node)
        {
            _node = node;
            EnsureLayers();

            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;

            if (_portrait != null)
            {
                _portrait.sprite = node.portrait;
                _portrait.enabled = node.portrait != null;
            }

            _speakerText.text = node.speakerName;
            _typewriter.ShowText(node.text);
            RebuildButtons(node);
        }

        /// <summary>隐藏 UI（对话结束）。不销毁层级，供下次对话复用。</summary>
        public void Hide()
        {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _node = null;
            SetButtonsInteractive(false);
        }

        // ---- 内部 ----

        private void OnAdvanceClicked()
        {
            if (IsTyping)
            {
                _typewriter.SkipToEnd();
                return;
            }

            if (_node != null && _node.choices.Count > 0)
                return; // 分支节点必须点选项

            AdvanceClicked?.Invoke();
        }

        private void RebuildButtons(DialogueNode node)
        {
            // 清除旧选项按钮
            for (int i = _choiceContainer.childCount - 1; i >= 0; i--)
                Destroy(_choiceContainer.GetChild(i).gameObject);

            for (int i = 0; i < node.choices.Count; i++)
            {
                int index = i;
                string label = string.IsNullOrEmpty(node.choices[i].text)
                    ? "（沉默）"
                    : node.choices[i].text;
                var button = CreateButton(label, _choiceContainer,
                    new Color(0.15f, 0.12f, 0.1f, 0.95f), new Color(0.9f, 0.82f, 0.6f));
                button.onClick.AddListener(() =>
                {
                    if (IsTyping) return;
                    ChoiceClicked?.Invoke(index);
                });
            }

            // 奉上按钮（有 giftTrigger 且背包有货时显示）
            bool showGift = !string.IsNullOrEmpty(node.giftPastryId)
                && SimpleInventory.Has(node.giftPastryId);
            if (_giftButton != null)
            {
                _giftButton.gameObject.SetActive(showGift);
                if (showGift)
                {
                    var item = SimpleInventory.Get(node.giftPastryId);
                    _giftButtonText.text = $"奉上 · {item?.displayName ?? node.giftPastryId}";
                }
            }

            // 打字完成前选项不可点（TypewriterEffect.Completed 后激活）
            SetButtonsInteractive(false);
            _typewriter.Completed -= OnTypingCompleted;
            _typewriter.Completed += OnTypingCompleted;
        }

        private void OnTypingCompleted()
        {
            SetButtonsInteractive(true);
        }

        private void SetButtonsInteractive(bool interactive)
        {
            if (_choiceContainer == null) return;
            foreach (Transform child in _choiceContainer)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null)
                    btn.interactable = interactive;
            }

            if (_giftButton != null && _giftButton.gameObject.activeSelf)
                _giftButton.interactable = interactive;
        }

        private void EnsureLayers()
        {
            if (_canvas != null) return;

            EnsureEventSystem();

            var canvasGo = new GameObject("DialogueCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _sortingOrder;

            canvasGo.AddComponent<GraphicRaycaster>();
            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // ---- 全屏点击层（推进/跳过）----
            var catcherGo = new GameObject("ClickCatcher");
            catcherGo.transform.SetParent(canvasGo.transform, false);
            var catcherImage = catcherGo.AddComponent<Image>();
            catcherImage.color = new Color(0, 0, 0, 0); // 完全透明但可接收射线
            Stretch(catcherImage.rectTransform);
            var catcherButton = catcherGo.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(OnAdvanceClicked);

            // ---- 底部面板 ----
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.09f, 0.08f, 0.1f, 0.94f); // 墨色
            var panelRt = panelImage.rectTransform;
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(0f, 240f);
            panelRt.anchoredPosition = Vector2.zero;

            // 立绘（左）
            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(panelGo.transform, false);
            _portrait = portraitGo.AddComponent<Image>();
            _portrait.preserveAspect = true;
            _portrait.raycastTarget = false;
            var portraitRt = _portrait.rectTransform;
            portraitRt.anchorMin = new Vector2(0f, 0.5f);
            portraitRt.anchorMax = new Vector2(0f, 0.5f);
            portraitRt.pivot = new Vector2(0f, 0.5f);
            portraitRt.anchoredPosition = new Vector2(24f, 0f);
            portraitRt.sizeDelta = new Vector2(170f, 170f);

            // 说话人名
            _speakerText = CreateText("Speaker", panelGo.transform, fontSize: 32, bold: true);
            var speakerRt = _speakerText.rectTransform;
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.pivot = new Vector2(0f, 1f);
            speakerRt.offsetMin = new Vector2(220f, -52f);
            speakerRt.offsetMax = new Vector2(-24f, -12f);
            _speakerText.color = new Color(0.95f, 0.86f, 0.65f); // 暖金
            _speakerText.raycastTarget = false;

            // 正文（Typewriter）
            _bodyText = CreateText("Body", panelGo.transform, fontSize: 25, bold: false);
            var bodyRt = _bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(220f, 14f);
            bodyRt.offsetMax = new Vector2(-24f, -58f);
            _bodyText.color = new Color(0.92f, 0.9f, 0.86f);
            _bodyText.raycastTarget = false;
            _typewriter = _bodyText.gameObject.AddComponent<TypewriterEffect>();

            // 分支选项容器（右下垂直排列）
            var choicesGo = new GameObject("Choices");
            choicesGo.transform.SetParent(panelGo.transform, false);
            _choiceContainer = choicesGo.AddComponent<RectTransform>();
            var choiceRt = _choiceContainer;
            choiceRt.anchorMin = new Vector2(1f, 0f);
            choiceRt.anchorMax = new Vector2(1f, 0f);
            choiceRt.pivot = new Vector2(1f, 0f);
            choiceRt.anchoredPosition = new Vector2(-24f, 16f);
            choiceRt.sizeDelta = new Vector2(460f, 160f);
            var choiceLayout = choicesGo.AddComponent<VerticalLayoutGroup>();
            choiceLayout.spacing = 8f;
            choiceLayout.childAlignment = TextAnchor.UpperRight;
            choiceLayout.childControlWidth = true;
            choiceLayout.childControlHeight = true;
            choiceLayout.childForceExpandWidth = true;
            choiceLayout.childForceExpandHeight = false;

            // 奉上按钮（面板右上，区别于普通选项）
            var giftGo = new GameObject("GiftButton");
            giftGo.transform.SetParent(panelGo.transform, false);
            _giftButton = giftGo.AddComponent<Button>();
            var giftImage = giftGo.AddComponent<Image>();
            giftImage.color = new Color(0.45f, 0.3f, 0.14f, 0.98f); // 暖棕
            var giftRt = giftImage.rectTransform;
            giftRt.anchorMin = new Vector2(1f, 1f);
            giftRt.anchorMax = new Vector2(1f, 1f);
            giftRt.pivot = new Vector2(1f, 1f);
            giftRt.anchoredPosition = new Vector2(-24f, -60f);
            giftRt.sizeDelta = new Vector2(300f, 52f);
            _giftButtonText = CreateText("GiftLabel", giftGo.transform, fontSize: 24, bold: true);
            var giftTextRt = _giftButtonText.rectTransform;
            giftTextRt.anchorMin = Vector2.zero;
            giftTextRt.anchorMax = Vector2.one;
            giftTextRt.offsetMin = Vector2.zero;
            giftTextRt.offsetMax = Vector2.zero;
            _giftButtonText.alignment = TextAlignmentOptions.Center;
            _giftButtonText.color = new Color(1f, 0.94f, 0.8f);
            _giftButtonText.raycastTarget = false;
            _giftButton.onClick.AddListener(() =>
            {
                if (IsTyping) return;
                GiftClicked?.Invoke();
            });
            giftGo.SetActive(false);
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

        private Button CreateButton(string label, Transform parent, Color bg, Color textColor)
        {
            var go = new GameObject($"Choice_{label}");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = bg;

            var button = go.AddComponent<Button>();

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 44f;

            var text = CreateText("Label", go.transform, fontSize: 22, bold: false);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor;
            text.raycastTarget = false;
            text.text = label;

            return button;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>对话按钮需要 EventSystem。场景没有则创建（Input System UI 模块）。</summary>
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
