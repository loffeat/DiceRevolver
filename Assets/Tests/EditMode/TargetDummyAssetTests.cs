using System.Linq;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceRevolver.Tests
{
    public sealed class TargetDummyAssetTests
    {
        private const string PrefabPath = "Assets/Prefab/TargetDummy.prefab";
        private const string ScenePath = "Assets/Scenes/TopDownShooterPrototype.unity";

        [Test]
        public void PrefabContainsDamageTargetHitboxAndWorldSpaceNumberTemplate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<TargetDummy>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<WorldDamageNumberSpawner>(), Is.Not.Null);

            Collider hitbox = prefab.GetComponent<Collider>();
            Assert.That(hitbox, Is.Not.Null);
            Assert.That(hitbox.isTrigger, Is.True);

            Rigidbody body = prefab.GetComponent<Rigidbody>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(body.useGravity, Is.False);

            WorldDamageNumber template = prefab.GetComponentInChildren<WorldDamageNumber>(true);
            Assert.That(template, Is.Not.Null);
            Assert.That(template.gameObject.activeSelf, Is.False);
            Assert.That(template.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(template.GetComponentInChildren<Text>(true), Is.Not.Null);
        }

        [Test]
        public void PrototypeSceneContainsExactlyOneTargetDummyPrefabInstance()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject[] targets = scene.GetRootGameObjects()
                    .Where(root => root.GetComponent<TargetDummy>() != null)
                    .ToArray();

                Assert.That(targets, Has.Length.EqualTo(1));
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(targets[0]), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
