using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PastryWorld.Core;
using PastryWorld.Exploration;
using PastryWorld.Narrative;

namespace PastryWorld.Codex
{
    /// <summary>
    /// 图鉴 UI（T22）。手账风格：米黄纸张 + 左栏条目列表 + 右栏详情页。
    /// 运行时自建 UGUI（模式复用 DialogueUI/MonologueUI）。
    /// 翻页动画：EaseInOutQuad 0.3s（Spike-T22 结论，协程实现）。
    /// 完成度百分比（T23）显示于顶部。sortingOrder=700（菜单层级，高于对话 600）。
    /// </summary>
    public class CodexUI : MonoBehaviour
    {
        [Tooltip("图鉴 Canvas sortingOrder（菜单层级，高于对话/独白 600）")]
        [SerializeField] private int _sortingOrder = 700;

        private Canvas _canvas;
        private CanvasGroup _group;
        private TMP_Text _completionText;
        private RectTransform _detailPanel;
        private RectTransform _detailPage;
        private TMP_Text _detailTitle;
        private TMP_Text _detailQuality;
        private TMP_Text _detailDesc;
        private TMP_Text _detailRelated;
        private Image _portraitFrame;

        private readonly Dictionary<string, Button> _entryButtons = new();
        private CodexManager _manager;
        private PlayerController _lockedPlayer;
        private string _selectedEntryId;
        private Coroutine _flipRoutine;

        private static readonly Color PaperColor = new(0.96f, 0.93f, 0.86f);
        private static readonly Color InkColor = new(0.28f, 0.24f, 0.2f);
        private static readonly Color FaintInkColor = new(0.55f, 0.5f, 0.44f);

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void OnEnable()
        {
            EventBus.Default.Subscribe<CodexEntryUnlockedEvent>(OnEntryUnlocked);
            EventBus.Default.Subscribe<CodexCompletionChangedEvent>(OnCompletionChanged);
        }

        private void OnDisable()
        {
            EventBus.Default.Unsubscribe<CodexEntryUnlockedEvent>(OnEntryUnlocked);
            EventBus.Default.Unsubscribe<CodexCompletionChangedEvent>(OnCompletionChanged);
        }

        private void OnEntryUnlocked(CodexEntryUnlockedEvent evt)
        {
            // 打开状态下实时刷新列表（未收录 → 名称浮现）
            if (IsOpen) RebuildList();
        }

        private void OnCompletionChanged(CodexCompletionChangedEvent evt)
        {
            if (IsOpen) RefreshCompletion();
        }

        // ---------------- 开关 ----------------

        public void Open()
        {
            EnsureLayers();
            _manager = CodexManager.Instance;

            _lockedPlayer = FindFirstObjectByType<PlayerController>();
            if (_lockedPlayer != null) _lockedPlayer.InputLocked = true;

            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;

            RebuildList();
            RefreshCompletion();
            if (string.IsNullOrEmpty(_selectedEntryId) && _manager != null && _manager.Entries.Count > 0)
            {
                SelectEntry(_manager.Entries[0].entryId, animate: false);
            }
        }

        public void Close()
        {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            if (_lockedPlayer != null)
            {
                _lockedPlayer.InputLocked = false;
                _lockedPlayer = null;
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        // ---------------- 条目选择 ----------------

        public void SelectEntry(string entryId, bool animate = true)
        {
            if (_manager == null || !_manager.TryGetEntry(entryId, out var entry)) return;

            _selectedEntryId = entryId;
            HighlightSelected();

            if (!animate)
            {
                FillDetail(entry);
                return;
            }

            // 翻页：旧页滑出 → 填充 → 新页滑入
            if (_flipRoutine != null) StopCoroutine(_flipRoutine);
            _flipRoutine = StartCoroutine(FlipToEntry(entry));
        }

        private IEnumerator FlipToEntry(CodexEntrySO entry)
        {
            // 滑出（向左）
            yield return SlidePage(Vector2.zero, new Vector2(-80f, 0f), 0.15f);
            FillDetail(entry);
            // 从右侧滑入
            _detailPage.anchoredPosition = new Vector2(80f, 0f);
            yield return SlidePage(new Vector2(80f, 0f), Vector2.zero, 0.15f);
            _flipRoutine = null;
        }

        private IEnumerator SlidePage(Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                _detailPage.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            _detailPage.anchoredPosition = to;
        }

        private void FillDetail(CodexEntrySO entry)
        {
            bool unlocked = _manager.IsUnlocked(entry.entryId);
            var quality = _manager.GetQuality(entry.entryId);

            _detailTitle.text = unlocked ? entry.displayName : "？？？";
            _detailTitle.color = unlocked ? InkColor : FaintInkColor;

            _detailQuality.text = unlocked ? QualityLabel(quality) : "未收录";
            _detailQuality.color = unlocked ? CodexEntrySO.QualityColor(quality) : FaintInkColor;

            _detailDesc.text = unlocked
                ? entry.description
                : "尚未收录。继续探索、制作与相遇，也许它会自己浮现。";
            _detailDesc.color = unlocked ? InkColor : FaintInkColor;

            // 交叉索引（仅解锁后显示；点击可跳转）
            _detailRelated.text = "";
            if (unlocked && entry.relatedEntryIds != null)
            {
                var parts = new List<string>();
                foreach (var id in entry.relatedEntryIds)
                {
                    if (_manager.TryGetEntry(id, out var rel))
                    {
                        parts.Add(_manager.IsUnlocked(id) ? rel.displayName : "？？？");
                    }
                }
                if (parts.Count > 0) _detailRelated.text = "关联： " + string.Join(" / ", parts);
            }

            // 插图框（占位色块，接入美术后换 Sprite）
            _portraitFrame.color = unlocked
                ? new Color(0.87f, 0.82f, 0.72f)
                : new Color(0.8f, 0.78f, 0.75f);
        }

        private static string QualityLabel(CodexQuality q)
        {
            return q switch
            {
                CodexQuality.Bronze => "铜",
                CodexQuality.Silver => "银",
                CodexQuality.Gold => "金",
                _ => q.ToString()
            };
        }

        // ---------------- 列表刷新 ----------------

        /// <summary>重建左栏列表（打开时 + 解锁事件时调用）。</summary>
        public void RebuildList()
        {
            if (_manager == null) _manager = CodexManager.Instance;
            if (_manager == null) return;

            var listContent = _listContent;
            if (listContent == null) return;

            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _entryButtons.Clear();

            // 按分类分组（保持 _entries 顺序内的分类首次出现序）
            var order = new List<CodexCategory>();
            var groups = new Dictionary<CodexCategory, List<CodexEntrySO>>();
            foreach (var entry in _manager.Entries)
            {
                if (entry == null) continue;
                if (!groups.ContainsKey(entry.category))
                {
                    groups[entry.category] = new List<CodexEntrySO>();
                    order.Add(entry.category);
                }
                groups[entry.category].Add(entry);
            }

            foreach (var cat in order)
            {
                CreateListHeader(listContent, CodexEntrySO.CategoryName(cat));
                foreach (var entry in groups[cat])
                {
                    CreateListButton(listContent, entry);
                }
            }

            HighlightSelected();
            RefreshCompletion();
        }

        private void CreateListHeader(Transform parent, string title)
        {
            var go = new GameObject($"Header_{title}");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.62f, 0.42f, 0.24f);
            text.text = "· " + title;
            text.raycastTarget = false;
            var font = DialogueFontResolver.ChineseFont;
            if (font != null) text.font = font;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 34f;
            le.preferredHeight = 34f;
        }

        private void CreateListButton(Transform parent, CodexEntrySO entry)
        {
            bool unlocked = _manager.IsUnlocked(entry.entryId);

            var go = new GameObject($"Entry_{entry.entryId}");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0); // 透明底，选中态由 HighlightSelected 上色

            var text = CreateChildText(go.transform, "Label", 19);
            text.text = unlocked ? entry.displayName : "？？？";
            text.color = unlocked ? InkColor : FaintInkColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            StretchWithPadding(text.rectTransform, 16f, 0f);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 34f;
            le.preferredHeight = 34f;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SelectEntry(entry.entryId));

            _entryButtons[entry.entryId] = button;
        }

        private void HighlightSelected()
        {
            foreach (var kv in _entryButtons)
            {
                var img = kv.Value.GetComponent<Image>();
                if (img == null) continue;
                img.color = kv.Key == _selectedEntryId
                    ? new Color(0.9f, 0.82f, 0.66f, 0.9f)
                    : new Color(0, 0, 0, 0);
            }
        }

        private void RefreshCompletion()
        {
            if (_manager == null) _manager = CodexManager.Instance;
            if (_manager == null || _completionText == null) return;
            _completionText.text = $"完成度 {_manager.CompletionPercent:F0}%（{_manager.UnlockedCount}/{_manager.TotalCount}）";
        }

        // ---------------- 层级构建 ----------------

        private RectTransform _listContent;

        private void EnsureLayers()
        {
            if (_canvas != null) return;

            EnsureEventSystem();
            _manager = CodexManager.Instance;

            var canvasGo = new GameObject("CodexCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _sortingOrder;
            canvasGo.AddComponent<GraphicRaycaster>();
            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            // 全屏暗化底
            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(canvasGo.transform, false);
            var dim = dimGo.AddComponent<Image>();
            dim.color = new Color(0.1f, 0.08f, 0.06f, 0.55f);
            Stretch(dim.rectTransform);

            // 纸张主面板
            var paperGo = new GameObject("Paper");
            paperGo.transform.SetParent(canvasGo.transform, false);
            var paper = paperGo.AddComponent<Image>();
            paper.color = PaperColor;
            var paperRt = paper.rectTransform;
            paperRt.anchorMin = paperRt.anchorMax = new Vector2(0.5f, 0.5f);
            paperRt.sizeDelta = new Vector2(980f, 640f);
            paperRt.anchoredPosition = Vector2.zero;

            // 顶部标题栏
            var title = CreateChildText(paperGo.transform, "Title", 30);
            title.text = "点 心 手 账";
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.5f, 0.32f, 0.16f);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(0.45f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.offsetMin = new Vector2(32f, -56f);
            titleRt.offsetMax = new Vector2(0f, -16f);
            title.alignment = TextAlignmentOptions.MidlineLeft;

            // 完成度（T23）
            _completionText = CreateChildText(paperGo.transform, "Completion", 22);
            _completionText.color = new Color(0.5f, 0.32f, 0.16f);
            var compRt = _completionText.rectTransform;
            compRt.anchorMin = new Vector2(0.45f, 1f);
            compRt.anchorMax = new Vector2(1f, 1f);
            compRt.pivot = new Vector2(1f, 1f);
            compRt.offsetMin = new Vector2(0f, -52f);
            compRt.offsetMax = new Vector2(-100f, -18f);
            _completionText.alignment = TextAlignmentOptions.MidlineRight;

            // 关闭按钮
            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(paperGo.transform, false);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.72f, 0.6f, 0.45f);
            var closeRt = closeImg.rectTransform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(44f, 44f);
            closeRt.anchoredPosition = new Vector2(-12f, -12f);
            var closeLabel = CreateChildText(closeGo.transform, "X", 24);
            closeLabel.text = "×";
            closeLabel.color = Color.white;
            Stretch(closeLabel.rectTransform);
            closeLabel.alignment = TextAlignmentOptions.Center;
            var closeButton = closeGo.AddComponent<Button>();
            closeButton.onClick.AddListener(Close);

            // 左栏列表（ScrollRect）
            var listGo = new GameObject("EntryList");
            listGo.transform.SetParent(paperGo.transform, false);
            var listRt = listGo.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0f);
            listRt.anchorMax = new Vector2(0.34f, 1f);
            listRt.offsetMin = new Vector2(24f, 24f);
            listRt.offsetMax = new Vector2(-8f, -72f);
            var listBg = listGo.AddComponent<Image>();
            listBg.color = new Color(0.92f, 0.88f, 0.79f);

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(listGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            Stretch(viewportRt);
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _listContent = contentGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = listGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = _listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 20f;

            // 右栏详情（子控件字段在 CreateDetailPanel 内赋值）
            _detailPanel = CreateDetailPanel(paperGo.transform);
        }

        private RectTransform CreateDetailPanel(Transform parent)
        {
            var panelGo = new GameObject("Detail");
            panelGo.transform.SetParent(parent, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.34f, 0f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.offsetMin = new Vector2(8f, 24f);
            panelRt.offsetMax = new Vector2(-24f, -72f);

            // 详情页（翻页动画作用体）
            var pageGo = new GameObject("Page");
            pageGo.transform.SetParent(panelGo.transform, false);
            _detailPage = pageGo.AddComponent<RectTransform>();
            Stretch(_detailPage);

            // 0: 插图框
            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(pageGo.transform, false);
            _portraitFrame = portraitGo.AddComponent<Image>();
            _portraitFrame.color = new Color(0.87f, 0.82f, 0.72f);
            var portraitRt = _portraitFrame.rectTransform;
            portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.5f, 1f);
            portraitRt.pivot = new Vector2(0.5f, 1f);
            portraitRt.sizeDelta = new Vector2(180f, 180f);
            portraitRt.anchoredPosition = new Vector2(0f, -20f);

            // 1: 标题
            _detailTitle = CreateChildText(pageGo.transform, "Title", 30);
            _detailTitle.fontStyle = FontStyles.Bold;
            var titleRt = _detailTitle.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 40f);
            titleRt.anchoredPosition = new Vector2(0f, -214f);
            _detailTitle.alignment = TextAlignmentOptions.Center;

            // 2: 品质徽标
            _detailQuality = CreateChildText(pageGo.transform, "Quality", 22);
            var qualityRt = _detailQuality.rectTransform;
            qualityRt.anchorMin = new Vector2(0f, 1f);
            qualityRt.anchorMax = new Vector2(1f, 1f);
            qualityRt.pivot = new Vector2(0.5f, 1f);
            qualityRt.sizeDelta = new Vector2(0f, 28f);
            qualityRt.anchoredPosition = new Vector2(0f, -258f);
            _detailQuality.alignment = TextAlignmentOptions.Center;

            // 3: 描述
            _detailDesc = CreateChildText(pageGo.transform, "Desc", 22);
            _detailDesc.enableWordWrapping = true;
            var descRt = _detailDesc.rectTransform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 1f);
            descRt.offsetMin = new Vector2(28f, 60f);
            descRt.offsetMax = new Vector2(-28f, -296f);

            // 4: 交叉索引
            _detailRelated = CreateChildText(pageGo.transform, "Related", 19);
            var relatedRt = _detailRelated.rectTransform;
            relatedRt.anchorMin = new Vector2(0f, 0f);
            relatedRt.anchorMax = new Vector2(1f, 0f);
            relatedRt.pivot = new Vector2(0.5f, 0f);
            relatedRt.sizeDelta = new Vector2(0f, 30f);
            relatedRt.anchoredPosition = new Vector2(0f, 18f);
            _detailRelated.alignment = TextAlignmentOptions.MidlineLeft;

            return panelRt;
        }

        private TMP_Text CreateChildText(Transform parent, string name, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.enableWordWrapping = false;
            text.color = InkColor;
            text.raycastTarget = false;
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

        private static void StretchWithPadding(RectTransform rt, float left, float right)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-right, 0f);
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
