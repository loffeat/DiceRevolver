using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class ProjectileIdentityTests
    {
        [Test]
        public void RuntimeStatsExposeTypeIdentityAndMultipleTagIdentities()
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTagDefinition lightning = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            ProjectileTagDefinition elemental = ScriptableObject.CreateInstance<ProjectileTagDefinition>();

            try
            {
                SetField(definition, "projectileTypeDefinition", type);
                SetField(definition, "projectileTags", new[] { lightning, elemental });

                ProjectileRuntimeStats stats = definition.BuildRuntimeStats();

                Assert.That(stats.ProjectileTypeDefinition, Is.SameAs(type));
                Assert.That(stats.HasTag(lightning), Is.True);
                Assert.That(stats.HasTag(elemental), Is.True);
                Assert.That(stats.Tags.Count, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(elemental);
                Object.DestroyImmediate(lightning);
                Object.DestroyImmediate(type);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void EmptyIdentityLibrariesExposeEmptyCollections()
        {
            ProjectileTypeLibrary typeLibrary = ScriptableObject.CreateInstance<ProjectileTypeLibrary>();
            ProjectileTagLibrary tagLibrary = ScriptableObject.CreateInstance<ProjectileTagLibrary>();

            try
            {
                Assert.That(typeLibrary.Types, Is.Empty);
                Assert.That(tagLibrary.Tags, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(tagLibrary);
                Object.DestroyImmediate(typeLibrary);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }
    }
}
