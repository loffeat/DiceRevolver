using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRevolver.Tests
{
    public sealed class RenderingLayerContractTests
    {
        private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";

        [Test]
        public void PrototypeSceneUsesZeroHeightSpriteGroundAndEntities()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject ground = FindRoot(scene, "Ground");
                TopDownPlayerController player = FindRootComponent<TopDownPlayerController>(scene);
                TargetDummy targetDummy = FindPrefabRootComponent<TargetDummy>(
                    scene,
                    "Assets/Prefab/TargetDummy.prefab");

                Assert.That(ground, Is.Not.Null);
                Assert.That(player, Is.Not.Null);
                Assert.That(targetDummy, Is.Not.Null);
                Assert.That(ground.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(Mathf.Abs(targetDummy.transform.position.y), Is.LessThan(0.05f));
                Assert.That(ground.GetComponent<MeshRenderer>(), Is.Null);
                Assert.That(ground.GetComponent<Collider>(), Is.Null);

                SpriteRenderer groundRenderer = ground.GetComponent<SpriteRenderer>();
                Assert.That(groundRenderer, Is.Not.Null);
                Assert.That(groundRenderer.sprite, Is.Not.Null);
                Assert.That(groundRenderer.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(groundRenderer.sortingLayerName, Is.EqualTo("Background"));
                AssertUsesNamedSortingLayers(player.gameObject, "Player");
                AssertUsesNamedSortingLayers(targetDummy.gameObject, "TargetDummy");
            }
            finally
            {
                if (openedByTest && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static T FindRootComponent<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindPrefabRootComponent<T>(Scene scene, string prefabPath)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(roots[i]);
                if (source != null && AssetDatabase.GetAssetPath(source) == prefabPath)
                {
                    return roots[i].GetComponent<T>();
                }
            }

            return null;
        }

        private static void AssertUsesNamedSortingLayers(GameObject owner, string label)
        {
            SpriteRenderer[] renderers = owner.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers, Is.Not.Empty, label);
            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.That(
                    renderers[i].sortingLayerName,
                    Is.Not.EqualTo("Default"),
                    $"{label}/{renderers[i].name}");
            }
        }
    }
}
