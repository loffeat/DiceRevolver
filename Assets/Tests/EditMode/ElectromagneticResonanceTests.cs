using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class ElectromagneticResonanceTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null)
                {
                    Object.DestroyImmediate(owned[index]);
                }
            }

            owned.Clear();
        }

        [Test]
        public void SelectionUsesAtMostThreeDistinctCandidatesWithoutReplacement()
        {
            List<ProjectileHandle> candidates = new List<ProjectileHandle>();
            for (int index = 0; index < 5; index++)
            {
                Projectile projectile = CreateProjectile($"Candidate {index}", Vector3.right * index);
                candidates.Add(new ProjectileHandle(projectile, default));
            }

            Queue<int> indices = new Queue<int>(new[] { 4, 0, 1 });
            IReadOnlyList<ProjectileHandle> selected =
                LightningChainTargetSelector.Select(
                    candidates,
                    3,
                    count => indices.Dequeue() % count);

            Assert.That(selected, Has.Count.EqualTo(3));
            Assert.That(selected[0].Projectile, Is.SameAs(candidates[4].Projectile));
            Assert.That(selected[1].Projectile, Is.SameAs(candidates[0].Projectile));
            Assert.That(selected[2].Projectile, Is.SameAs(candidates[2].Projectile));
        }

        private Projectile CreateProjectile(string name, Vector3 position)
        {
            GameObject owner = Own(new GameObject(name));
            owner.transform.position = position;
            return owner.AddComponent<Projectile>();
        }

        private T Own<T>(T target) where T : Object
        {
            owned.Add(target);
            return target;
        }
    }
}
