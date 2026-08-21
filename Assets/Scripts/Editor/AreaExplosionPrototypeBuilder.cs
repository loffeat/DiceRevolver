using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class AreaExplosionPrototypeBuilder
    {
        private const string PrefabPath = "Assets/Prefab/Projectiles/BlastExplosion.prefab";
        private const string MaterialPath = "Assets/Materials/ExplosionRing.mat";
        private const string DefinitionPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/BlastExplosion.asset";
        private const string RulePath =
            "Assets/Resources/DiceFacePrototype/EventRules/Core/BlastRoundRule.asset";
        private const string LibraryPath =
            "Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset";

        [MenuItem("Dice Revolver/Setup Area Explosion Prototype")]
        public static void Build()
        {
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Prefab", "Projectiles");
            EnsureFolder("Assets/Resources/DiceFacePrototype", "Projectiles");
            EnsureFolder("Assets/Resources/DiceFacePrototype", "BulletEvents");

            Material ringMaterial = LoadOrCreateRingMaterial();
            Projectile explosionPrefab = LoadOrCreateExplosionPrefab(ringMaterial);
            ProjectileDefinition definition = LoadOrCreateDefinition(explosionPrefab);
            BindRuleIfMissing(definition);
            AppendDefinitionIfMissing(definition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Area explosion prototype assets are ready.");
        }

        private static Material LoadOrCreateRingMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Sprites/Default") ??
                Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader)
            {
                name = "ExplosionRing"
            };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static Projectile LoadOrCreateExplosionPrefab(Material ringMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<Projectile>();
            }

            GameObject root = new GameObject("BlastExplosion");
            try
            {
                Projectile projectile = root.AddComponent<Projectile>();
                LineRenderer ring = root.AddComponent<LineRenderer>();
                ring.sharedMaterial = ringMaterial;
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.widthMultiplier = 0.18f;
                ring.positionCount = 64;
                ring.numCornerVertices = 2;
                ring.numCapVertices = 2;
                ring.sortingLayerName = "projectile";
                ring.sortingOrder = 0;

                AreaExplosionProjectile explosion = root.AddComponent<AreaExplosionProjectile>();
                SerializedObject serialized = new SerializedObject(explosion);
                serialized.FindProperty("radius").floatValue = 2.5f;
                serialized.FindProperty("targetLayers").intValue = ~0;
                serialized.FindProperty("ringRenderer").objectReferenceValue = ring;
                serialized.FindProperty("visualDuration").floatValue = 0.35f;
                serialized.FindProperty("ringColor").colorValue =
                    new Color(1f, 0.24f, 0.05f, 1f);
                serialized.FindProperty("ringWidth").floatValue = 0.18f;
                serialized.FindProperty("ringSegments").intValue = 64;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return saved.GetComponent<Projectile>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static ProjectileDefinition LoadOrCreateDefinition(Projectile explosionPrefab)
        {
            ProjectileDefinition definition =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(DefinitionPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("displayName").stringValue = "爆炸范围弹";
            serialized.FindProperty("projectilePrefab").objectReferenceValue = explosionPrefab;
            serialized.FindProperty("projectileType").stringValue = "Explosion";
            serialized.FindProperty("projectileTag").stringValue = "PlayerExplosion";
            serialized.FindProperty("damage").floatValue = 3f;
            serialized.FindProperty("flightDistance").floatValue = 0.0001f;
            serialized.FindProperty("flightSpeed").floatValue = 0.0001f;
            serialized.FindProperty("enemyPierceCount").intValue = 0;
            serialized.FindProperty("defaultAttackEffect").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void BindRuleIfMissing(ProjectileDefinition definition)
        {
            EventRuleDefinition rule =
                AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(RulePath);
            SpawnProjectileResultModule result = rule?.Results
                .Select(entry => entry.Result)
                .OfType<SpawnProjectileResultModule>()
                .FirstOrDefault();
            if (result == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(result);
            SerializedProperty property = serialized.FindProperty("projectileDefinition");
            if (property.objectReferenceValue != null)
            {
                return;
            }

            property.objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(result);
        }

        private static void AppendDefinitionIfMissing(ProjectileDefinition definition)
        {
            ProjectileDefinitionLibrary library =
                AssetDatabase.LoadAssetAtPath<ProjectileDefinitionLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ProjectileDefinitionLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty definitions = serialized.FindProperty("definitions");
            for (int i = 0; i < definitions.arraySize; i++)
            {
                if (definitions.GetArrayElementAtIndex(i).objectReferenceValue == definition)
                {
                    return;
                }
            }

            int index = definitions.arraySize;
            definitions.arraySize++;
            definitions.GetArrayElementAtIndex(index).objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
