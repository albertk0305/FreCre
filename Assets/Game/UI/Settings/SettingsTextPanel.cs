using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.UI.Settings
{
    public sealed class SettingsTextPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private float titleHiddenVerticalExpansion = 30f;

        private bool subscribed;
        private bool layoutCached;
        private Vector2 titledScrollPosition;
        private Vector2 titledScrollSize;

        public bool IsOpen => gameObject.activeSelf;
        public TMP_Text TitleText => titleText;
        public TMP_Text ContentText => contentText;
        public ScrollRect ScrollRect => scrollRect;
        public bool IsTitleVisible => titleText != null
            && titleText.gameObject.activeSelf;

        private void Awake()
        {
            ConfigurePresentation();
            CacheTitledLayout();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            TMP_Text configuredTitleText,
            TMP_Text configuredContentText,
            ScrollRect configuredScrollRect,
            Button configuredCloseButton)
        {
            Unsubscribe();
            titleText = configuredTitleText
                ?? throw new ArgumentNullException(nameof(configuredTitleText));
            contentText = configuredContentText
                ?? throw new ArgumentNullException(nameof(configuredContentText));
            scrollRect = configuredScrollRect
                ?? throw new ArgumentNullException(nameof(configuredScrollRect));
            closeButton = configuredCloseButton
                ?? throw new ArgumentNullException(nameof(configuredCloseButton));

            layoutCached = false;
            ConfigurePresentation();
            CacheTitledLayout();
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Show(string title, string content)
        {
            Show(title, content, true);
        }

        public void Show(string title, string content, bool showTitle)
        {
            EnsureConfigured();

            titleText.text = title ?? string.Empty;
            contentText.text = content ?? string.Empty;
            ApplyTitleVisibility(showTitle);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ResetScrollToTop();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void ResetScrollToTop()
        {
            EnsureConfigured();

            contentText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentText.rectTransform);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void ConfigurePresentation()
        {
            if (contentText != null)
            {
                contentText.textWrappingMode = TextWrappingModes.Normal;
                contentText.richText = true;
                contentText.verticalAlignment = VerticalAlignmentOptions.Top;
            }

            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
        }

        private void CacheTitledLayout()
        {
            if (layoutCached || scrollRect == null)
                return;

            RectTransform viewport = GetViewportRect();
            if (viewport == null)
                return;

            titledScrollPosition = viewport.anchoredPosition;
            titledScrollSize = viewport.sizeDelta;
            layoutCached = true;
        }

        private void ApplyTitleVisibility(bool showTitle)
        {
            CacheTitledLayout();
            titleText.gameObject.SetActive(showTitle);
            if (!layoutCached)
                return;

            RectTransform viewport = GetViewportRect();
            float expansion = showTitle
                ? 0f
                : Mathf.Max(0f, titleHiddenVerticalExpansion);
            viewport.anchoredPosition = titledScrollPosition
                + new Vector2(0f, expansion * 0.5f);
            viewport.sizeDelta = titledScrollSize
                + new Vector2(0f, expansion);
        }

        private RectTransform GetViewportRect()
        {
            return scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
        }

        private void Subscribe()
        {
            if (subscribed || closeButton == null)
                return;

            closeButton.onClick.AddListener(Close);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            subscribed = false;
        }

        private void EnsureConfigured()
        {
            if (titleText == null
                || contentText == null
                || scrollRect == null
                || closeButton == null)
            {
                throw new InvalidOperationException(
                    "SettingsTextPanel is not configured.");
            }
        }
    }
}
