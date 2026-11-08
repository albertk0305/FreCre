using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Localization;
using ValorChronicle.Settings.Integration;
using ValorChronicle.UI.Settings;
using SettingsService = ValorChronicle.Settings.Services.SettingsService;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class SettingsTextPanelTests
    {
        private const float HiddenTitleExpansion = 30f;
        private const string MusicAttribution =
            "\"Arcadia\", \"Death and Axes\", \"Midnight Tale\"\n"
            + "Kevin MacLeod (incompetech.com)\n"
            + "Licensed under Creative Commons: By Attribution 4.0\n"
            + "http://creativecommons.org/licenses/by/4.0/";
        private const string PrefabPath =
            "Assets/Game/UI/Settings/SettingsPanel.prefab";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ShowAndClose_DisplayProvidedTextAndCanRepeat()
        {
            PanelFixture fixture = CreatePanelFixture();
            const string firstContent =
                "Line 01\nLine 02\nLine 03\nLine 04\nLine 05\n"
                + "Line 06\nLine 07\nLine 08\nLine 09\nLine 10\n"
                + "Line 11\nLine 12\nLine 13\nLine 14\nLine 15";
            const string secondContent =
                "Again 01\nAgain 02\nAgain 03\nAgain 04\nAgain 05\n"
                + "Again 06\nAgain 07\nAgain 08\nAgain 09\nAgain 10\n"
                + "Again 11\nAgain 12\nAgain 13\nAgain 14\nAgain 15";

            fixture.Panel.Show("First", firstContent);

            Assert.That(fixture.Panel.IsOpen, Is.True);
            Assert.That(fixture.Title.text, Is.EqualTo("First"));
            Assert.That(fixture.Content.text, Is.EqualTo(firstContent));
            Assert.That(fixture.Content.rectTransform.rect.height,
                Is.GreaterThan(fixture.Scroll.viewport.rect.height));
            Assert.That(fixture.Scroll.verticalNormalizedPosition, Is.EqualTo(1f));

            fixture.Scroll.verticalNormalizedPosition = 0f;
            fixture.CloseButton.onClick.Invoke();
            Assert.That(fixture.Panel.IsOpen, Is.False);

            fixture.Panel.Show("Second", secondContent);

            Assert.That(fixture.Scroll.verticalNormalizedPosition, Is.EqualTo(1f));
            fixture.CloseButton.onClick.Invoke();

            Assert.That(fixture.Panel.IsOpen, Is.False);
            Assert.That(fixture.Title.text, Is.EqualTo("Second"));
            Assert.That(fixture.Content.text, Is.EqualTo(secondContent));
        }

        [Test]
        public void Show_CanHideTitleAndExpandOnlyTheTopOfScrollArea()
        {
            PanelFixture fixture = CreatePanelFixture();
            RectTransform viewport = fixture.Scroll.GetComponent<RectTransform>();
            Vector2 titledPosition = viewport.anchoredPosition;
            Vector2 titledSize = viewport.sizeDelta;
            float titledBottom = titledPosition.y - titledSize.y * 0.5f;

            fixture.Panel.Show(string.Empty, "도움말 본문", false);

            Assert.That(fixture.Panel.IsTitleVisible, Is.False);
            Assert.That(viewport.sizeDelta.y,
                Is.EqualTo(titledSize.y + HiddenTitleExpansion).Within(0.001f));
            Assert.That(viewport.anchoredPosition.y,
                Is.EqualTo(titledPosition.y + HiddenTitleExpansion * 0.5f)
                    .Within(0.001f));
            Assert.That(viewport.anchoredPosition.y - viewport.sizeDelta.y * 0.5f,
                Is.EqualTo(titledBottom).Within(0.001f));

            fixture.Panel.Show("크레딧", "크레딧 본문");

            Assert.That(fixture.Panel.IsTitleVisible, Is.True);
            Assert.That(fixture.Title.text, Is.EqualTo("크레딧"));
            Assert.That(viewport.anchoredPosition, Is.EqualTo(titledPosition));
            Assert.That(viewport.sizeDelta, Is.EqualTo(titledSize));
        }

        [Test]
        public void LocalizedHelpContent_ContainsAllConfirmedRules()
        {
            LocalizationService localization = LoadLocalization();
            string english = localization.GetText(
                SettingsHelpContent.LocalizationKey,
                LocalizationService.EnglishLocale);
            string korean = localization.GetText(
                SettingsHelpContent.LocalizationKey,
                LocalizationCultureResolver.KoreanLocale);

            for (int section = 1; section <= 7; section++)
            {
                Assert.That(english, Does.Contain($"<b>{section}. "));
                Assert.That(korean, Does.Contain($"<b>{section}. "));
            }

            Assert.That(english, Does.Contain("<b>1. Game Objective</b>"));
            Assert.That(korean, Does.Contain("<b>1. 게임 목표</b>"));
            Assert.That(english, Does.Contain("FreCre"));
            Assert.That(korean, Does.Contain("FreCre"));
            Assert.That(english, Does.Not.Contain("Valor Chronicle"));
            Assert.That(korean, Does.Not.Contain("Valor Chronicle"));
            Assert.That(english, Does.Contain("Water is strong against Fire."));
            Assert.That(english, Does.Contain(
                "Light and Dark are strong against each other."));
            Assert.That(korean, Does.Contain("물은 불에 강합니다."));
            Assert.That(korean, Does.Contain("불은 풀에 강합니다."));
            Assert.That(korean, Does.Contain("풀은 물에 강합니다."));
            Assert.That(korean, Does.Contain("빛과 어둠은 서로에게 강합니다."));
        }

        [Test]
        public void LocalizedCreditsContent_PreservesConfirmedCreditsAndMusicLicense()
        {
            LocalizationService localization = LoadLocalization();
            string english = localization.GetText(
                SettingsCreditsContent.LocalizationKey,
                LocalizationService.EnglishLocale);
            string korean = localization.GetText(
                SettingsCreditsContent.LocalizationKey,
                LocalizationCultureResolver.KoreanLocale);

            Assert.That(english, Does.StartWith("<b>FreCre</b>"));
            Assert.That(korean, Does.StartWith("<b>FreCre</b>"));
            Assert.That(english, Does.Contain("Thank you for playing FreCre!"));
            Assert.That(korean, Does.Contain(
                "FreCre를 플레이해 주셔서 감사합니다!"));

            string[] sharedCredits =
            {
                "Neat3",
                "Unity",
                "Gemini & GPT Image",
                "Dragon Studio / Universfield",
                "Pretendard",
                MusicAttribution,
                "Neat3Develop",
                "neat3.itch.io/frecre",
                "albertk0305/FreCre",
                "neat3booze@gmail.com"
            };
            foreach (string expected in sharedCredits)
            {
                Assert.That(english, Does.Contain(expected));
                Assert.That(korean, Does.Contain(expected));
            }

            Assert.That(english, Does.Not.Contain("© 2026 Neat3"));
            Assert.That(korean, Does.Not.Contain("© 2026 Neat3"));
            Assert.That(english, Does.Not.Contain(
                "Third-party assets are used under their respective licenses."));
            Assert.That(korean, Does.Not.Contain(
                "Third-party assets are used under their respective licenses."));
        }

        [Test]
        public void SettingsPrefab_HelpAndCreditsUseSharedScrollableTextPanel()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            createdObjects.Add(instance);
            SettingsPanelView view = instance.GetComponent<SettingsPanelView>();
            Assert.That(view, Is.Not.Null);
            SettingsTextPanel panel = view.TextPanel;
            Assert.That(panel, Is.Not.Null);
            InvokeLifecycle(view, "OnEnable");
            InvokeLifecycle(panel, "OnEnable");
            var serializedPanel = new SerializedObject(panel);
            Assert.That(serializedPanel.FindProperty(
                    "titleHiddenVerticalExpansion").floatValue,
                Is.EqualTo(HiddenTitleExpansion));
            Button helpButton = instance.transform.Find("HelpButton")
                .GetComponent<Button>();
            Button creditsButton = instance.transform.Find("CreditsButton")
                .GetComponent<Button>();
            Button textPanelCloseButton = panel.transform.Find("CloseButton")
                .GetComponent<Button>();
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            settings.LoadOrCreate();
            LocalizationService localization = LoadLocalization();
            using var localeBinding = new SettingsLocaleBinding(
                settings,
                localization);
            using var presenter = new SettingsPanelPresenter(
                settings,
                view,
                null,
                localization);

            Assert.That(panel.IsOpen, Is.False);
            Assert.That(helpButton, Is.Not.Null);
            Assert.That(creditsButton, Is.Not.Null);
            Assert.That(textPanelCloseButton, Is.Not.Null);
            Assert.That(
                ((RectTransform)textPanelCloseButton.transform).anchoredPosition,
                Is.EqualTo(new Vector2(460f, 880f)));
            Assert.That(panel.ScrollRect.horizontal, Is.False);
            Assert.That(panel.ScrollRect.vertical, Is.True);
            Assert.That(panel.ScrollRect.viewport, Is.EqualTo(
                panel.ScrollRect.GetComponent<RectTransform>()));
            Assert.That(panel.ScrollRect.content, Is.EqualTo(
                panel.ContentText.rectTransform));
            Assert.That(panel.ScrollRect.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(
                panel.ContentText.GetComponent<ContentSizeFitter>().verticalFit,
                Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
            Assert.That(panel.ContentText.rectTransform.parent,
                Is.EqualTo(panel.ScrollRect.transform));

            settings.SetLocale(LocalizationCultureResolver.KoreanLocale);
            helpButton.onClick.Invoke();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.IsTitleVisible, Is.False);
            Assert.That(panel.ContentText.text,
                Is.EqualTo(localization.GetText(
                    SettingsHelpContent.LocalizationKey)));
            Assert.That(panel.ContentText.text,
                Does.StartWith("<b>1. 게임 목표</b>"));
            Assert.That(panel.ScrollRect.verticalNormalizedPosition,
                Is.EqualTo(1f));

            panel.Close();
            settings.SetLocale(LocalizationService.EnglishLocale);
            helpButton.onClick.Invoke();

            Assert.That(panel.ContentText.text,
                Does.StartWith("<b>1. Game Objective</b>"));
            Assert.That(panel.IsTitleVisible, Is.False);
            float englishHeight = panel.ContentText.rectTransform.rect.height;

            settings.SetLocale(LocalizationCultureResolver.KoreanLocale);

            Assert.That(panel.ContentText.text,
                Does.StartWith("<b>1. 게임 목표</b>"));
            float koreanHeight = panel.ContentText.rectTransform.rect.height;
            Assert.That(englishHeight, Is.GreaterThan(0f));
            Assert.That(koreanHeight, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(englishHeight - koreanHeight),
                Is.GreaterThan(0.01f));
            Assert.That(panel.ScrollRect.verticalNormalizedPosition,
                Is.EqualTo(1f));

            panel.Close();
            creditsButton.onClick.Invoke();

            Assert.That(view.IsCreditsVisible, Is.True);
            Assert.That(view.IsHelpVisible, Is.False);
            Assert.That(panel.IsTitleVisible, Is.False);
            Assert.That(panel.ContentText.text, Does.StartWith("<b>FreCre</b>"));
            Assert.That(panel.ContentText.text, Does.Contain(
                "FreCre를 플레이해 주셔서 감사합니다!"));
            Assert.That(panel.ContentText.text, Does.Contain(MusicAttribution));
            Assert.That(panel.ScrollRect.verticalNormalizedPosition,
                Is.EqualTo(1f));

            settings.SetLocale(LocalizationService.EnglishLocale);

            Assert.That(view.IsCreditsVisible, Is.True);
            Assert.That(panel.ContentText.text, Does.Contain(
                "Thank you for playing FreCre!"));
            Assert.That(panel.ContentText.text, Does.Not.Contain(
                "FreCre를 플레이해 주셔서 감사합니다!"));
            Assert.That(panel.ContentText.text, Does.Contain(MusicAttribution));

            textPanelCloseButton.onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False);
            helpButton.onClick.Invoke();
            Assert.That(view.IsHelpVisible, Is.True);
            Assert.That(panel.ContentText.text,
                Does.StartWith("<b>1. Game Objective</b>"));
            Assert.That(panel.ContentText.text, Does.Not.Contain("DEVELOPMENT"));

            textPanelCloseButton.onClick.Invoke();
            creditsButton.onClick.Invoke();
            Assert.That(view.IsCreditsVisible, Is.True);
            Assert.That(panel.ContentText.text, Does.StartWith("<b>FreCre</b>"));
            Assert.That(panel.ContentText.text,
                Does.Not.Contain("<b>1. Game Objective</b>"));
            Assert.That(panel.ScrollRect.verticalNormalizedPosition,
                Is.EqualTo(1f));
        }

        private static LocalizationService LoadLocalization()
        {
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog.CreateService();
        }

        private PanelFixture CreatePanelFixture()
        {
            var root = new GameObject("TextPanel", typeof(RectTransform));
            createdObjects.Add(root);
            var panel = root.AddComponent<SettingsTextPanel>();

            TMP_Text title = CreateChild<TextMeshProUGUI>(root, "TitleText");
            var scrollObject = new GameObject(
                "ScrollPanel",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(RectMask2D));
            scrollObject.transform.SetParent(root.transform, false);
            TMP_Text content = CreateChild<TextMeshProUGUI>(scrollObject, "Text");
            var viewport = scrollObject.GetComponent<RectTransform>();
            viewport.sizeDelta = new Vector2(300f, 100f);
            content.rectTransform.anchorMin = new Vector2(0f, 1f);
            content.rectTransform.anchorMax = new Vector2(1f, 1f);
            content.rectTransform.pivot = new Vector2(0.5f, 1f);
            content.rectTransform.sizeDelta = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            Button closeButton = CreateChild<Button>(root, "CloseButton");
            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = scrollObject.GetComponent<RectTransform>();
            scrollRect.content = content.rectTransform;

            panel.Configure(title, content, scrollRect, closeButton);
            return new PanelFixture(panel, title, content, scrollRect, closeButton);
        }

        private static void InvokeLifecycle(MonoBehaviour behaviour, string methodName)
        {
            MethodInfo method = behaviour.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(behaviour, null);
        }

        private static T CreateChild<T>(GameObject parent, string name)
            where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private sealed class PanelFixture
        {
            public PanelFixture(
                SettingsTextPanel panel,
                TMP_Text title,
                TMP_Text content,
                ScrollRect scroll,
                Button closeButton)
            {
                Panel = panel;
                Title = title;
                Content = content;
                Scroll = scroll;
                CloseButton = closeButton;
            }

            public SettingsTextPanel Panel { get; }
            public TMP_Text Title { get; }
            public TMP_Text Content { get; }
            public ScrollRect Scroll { get; }
            public Button CloseButton { get; }
        }
    }
}
