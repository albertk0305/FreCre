using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.UI;
using ValorChronicle.UI.Audio;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class InitSceneConnectionTests
    {
        private const string InitScenePath = "Assets/Scenes/Init.unity";

        [Test]
        public void InitSceneHasConnectedStartScreenAndNoMissingScripts()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    InitScenePath,
                    OpenSceneMode.Single);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                InitScreenController[] controllers =
                    Object.FindObjectsByType<InitScreenController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                GameBootstrapper[] bootstrappers =
                    Object.FindObjectsByType<GameBootstrapper>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                Assert.That(controllers, Has.Length.EqualTo(1));
                Assert.That(bootstrappers, Has.Length.EqualTo(1));
                Assert.That(
                    Object.FindObjectsByType<EventSystem>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Has.Length.EqualTo(1));
                Assert.That(
                    Object.FindObjectsByType<GraphicRaycaster>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Has.Length.EqualTo(1));

                InitScreenController controller = controllers[0];
                Assert.That(controller.IsConfigured, Is.True);
                Assert.That(controller.gameObject.name,
                    Is.EqualTo("InitBackgroundImage"));
                Assert.That(controller.StartButton,
                    Is.SameAs(controller.GetComponent<Button>()));
                Assert.That(controller.StartButton.interactable, Is.False);
                Assert.That(
                    Object.FindObjectsByType<ButtonPressSfx>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Has.Length.EqualTo(3));
                Assert.That(
                    Object.FindObjectsByType<Button>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Has.All.Matches<Button>(button =>
                        button.GetComponent<ButtonPressSfx>() != null));
                Assert.That(controller.StartButton.transition,
                    Is.EqualTo(Selectable.Transition.None));
                Assert.That(
                    controller.GetComponent<Image>().raycastTarget,
                    Is.True);
                Assert.That(controller.InitText.gameObject.activeSelf,
                    Is.False);
                Assert.That(controller.InitText.raycastTarget, Is.False);

                GameObject title = GameObject.Find("TitleImage");
                Assert.That(title, Is.Not.Null);
                Assert.That(title.GetComponent<Image>().raycastTarget,
                    Is.False);

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in
                        root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(
                            GameObjectUtility
                                .GetMonoBehavioursWithMissingScriptCount(
                                    child.gameObject),
                            Is.Zero,
                            $"Missing Script: {child.name}");
                    }
                }
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }
    }

    public sealed class GameViewportSceneConnectionTests
    {
        [TestCase("Assets/Scenes/Init.unity")]
        [TestCase("Assets/Scenes/Main.unity")]
        [TestCase("Assets/Scenes/Battle.unity")]
        [TestCase("Assets/Scenes/Party.unity")]
        [TestCase("Assets/Scenes/Character.unity")]
        [TestCase("Assets/Scenes/Gacha.unity")]
        public void BuildSceneUsesMaskedNineBySixteenGameViewport(
            string scenePath)
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                GameObject viewport = FindSingleByName(
                    scene,
                    "GamaeViewport");
                RectMask2D mask = viewport.GetComponent<RectMask2D>();
                AspectRatioFitter aspect =
                    viewport.GetComponent<AspectRatioFitter>();
                Assert.That(mask, Is.Not.Null);
                Assert.That(mask.enabled, Is.True);
                Assert.That(mask.padding, Is.EqualTo(Vector4.zero));
                Assert.That(mask.softness, Is.EqualTo(Vector2Int.zero));
                Assert.That(aspect, Is.Not.Null);
                Assert.That(
                    aspect.aspectMode,
                    Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
                Assert.That(
                    aspect.aspectRatio,
                    Is.EqualTo(9f / 16f).Within(0.000001f));

                Transform gameContent = viewport.transform.Find(
                    "GameContent");
                Assert.That(gameContent, Is.Not.Null);
                Assert.That(gameContent.parent, Is.SameAs(viewport.transform));

                Transform safeAreaRoot = viewport.transform.parent;
                Assert.That(safeAreaRoot, Is.Not.Null);
                Assert.That(safeAreaRoot.name, Is.EqualTo("SafeAreaRoot"));
                Assert.That(
                    safeAreaRoot.GetComponent<SafeAreaFitter>(),
                    Is.Not.Null);

                Canvas canvas = viewport.GetComponentInParent<Canvas>();
                Assert.That(canvas, Is.Not.Null);
                Assert.That(
                    canvas.renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.worldCamera, Is.Null);

                GameObject letterbox = FindSingleByName(
                    scene,
                    "LetterboxBackground");
                Assert.That(
                    letterbox.transform.parent,
                    Is.SameAs(canvas.transform));
                Assert.That(
                    letterbox.transform.IsChildOf(viewport.transform),
                    Is.False);
                Assert.That(
                    safeAreaRoot.parent,
                    Is.SameAs(canvas.transform));
                Assert.That(
                    letterbox.transform.GetSiblingIndex(),
                    Is.LessThan(safeAreaRoot.GetSiblingIndex()));
            }
            finally
            {
                RestoreSceneSetup(previousSetup);
            }
        }

        private static GameObject FindSingleByName(
            Scene scene,
            string objectName)
        {
            var matches = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms =
                    root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == objectName)
                    {
                        matches.Add(candidate.gameObject);
                    }
                }
            }

            Assert.That(
                matches,
                Has.Count.EqualTo(1),
                $"Expected one {objectName} in {scene.path}.");
            return matches[0];
        }

        private static void RestoreSceneSetup(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
        }
    }
}
