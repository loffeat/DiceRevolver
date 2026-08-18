using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class DiceFacePrototypeAssetBuilder
    {
        private const string RootFolder = "Assets/Resources/DiceFacePrototype";
        private const string BasicProjectileEffectPath =
            "Assets/Resources/DiceFacePrototype/BulletEvents/FireBasicRevolverProjectile.asset";

        [MenuItem("Dice Revolver/Setup Dice Face Build Prototype")]
        public static void SetupPrototypeAssets()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "DiceFacePrototype");
            EnsureFolder(RootFolder, "BulletEvents");
            EnsureFolder(RootFolder, "DiceFaces");

            ExtraShotOnFireEffect extraShot = LoadOrCreate<ExtraShotOnFireEffect>(
                $"{RootFolder}/BulletEvents/ExtraShotOnFireEffect.asset",
                out _);
            ExplosionOnHitEffect explosion = LoadOrCreate<ExplosionOnHitEffect>(
                $"{RootFolder}/BulletEvents/ExplosionOnHitEffect.asset",
                out _);
            ForceFaceFourOnFireEndEffect forceFour = LoadOrCreate<ForceFaceFourOnFireEndEffect>(
                $"{RootFolder}/BulletEvents/ForceFaceFourOnFireEndEffect.asset",
                out _);
            ProjectileSpawnEffect basicProjectile =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(BasicProjectileEffectPath);

            DiceFaceEntry basicShot = LoadOrCreate<DiceFaceEntry>(
                $"{RootFolder}/DiceFaces/BasicShot.asset",
                out bool basicShotCreated);
            if (basicShotCreated)
            {
                ConfigureEntry(
                    basicShot,
                    "基础射击",
                    "发射一发基础左轮子弹。",
                    new Color(0.82f, 0.86f, 0.90f, 1f),
                    DiceFaceSlotType.Base,
                    basicProjectile);
            }
            else
            {
                ConfigureMissingSlotMapping(basicShot, DiceFaceSlotType.Base, basicProjectile);
            }

            DiceFaceEntry doubleTap = LoadOrCreate<DiceFaceEntry>(
                $"{RootFolder}/DiceFaces/DoubleTap.asset",
                out bool doubleTapCreated);
            if (doubleTapCreated)
            {
                ConfigureEntry(
                    doubleTap,
                    "双重射击",
                    "开火时额外发射一次当前骰面。",
                    new Color(0.95f, 0.78f, 0.25f, 1f),
                    DiceFaceSlotType.OnFire,
                    extraShot);
            }
            else
            {
                ConfigureMissingSlotMapping(doubleTap, DiceFaceSlotType.OnFire, extraShot);
            }

            DiceFaceEntry blastRound = LoadOrCreate<DiceFaceEntry>(
                $"{RootFolder}/DiceFaces/BlastRound.asset",
                out bool blastRoundCreated);
            if (blastRoundCreated)
            {
                ConfigureEntry(
                    blastRound,
                    "爆炸弹",
                    "击中时生成已配置的爆炸弹幕。",
                    new Color(0.92f, 0.30f, 0.22f, 1f),
                    DiceFaceSlotType.OnHit,
                    explosion);
            }
            else
            {
                ConfigureMissingSlotMapping(blastRound, DiceFaceSlotType.OnHit, explosion);
            }

            DiceFaceEntry loadedFour = LoadOrCreate<DiceFaceEntry>(
                $"{RootFolder}/DiceFaces/LoadedFour.asset",
                out bool loadedFourCreated);
            if (loadedFourCreated)
            {
                ConfigureEntry(
                    loadedFour,
                    "强制四点",
                    "结束开火时填回骰面 4，并令下一次必定掷出 4。",
                    new Color(0.25f, 0.70f, 0.95f, 1f),
                    DiceFaceSlotType.OnFireEnd,
                    forceFour);
            }
            else
            {
                ConfigureMissingSlotMapping(loadedFour, DiceFaceSlotType.OnFireEnd, forceFour);
            }

            DiceFaceLibrary faceLibrary = LoadOrCreate<DiceFaceLibrary>(
                $"{RootFolder}/DiceFaceLibrary.asset",
                out bool faceLibraryCreated);
            if (faceLibraryCreated)
            {
                SetObjectArray(faceLibrary, "entries", basicShot, doubleTap, blastRound, loadedFour);
            }
            else
            {
                AppendMissingObjects(faceLibrary, "entries", basicShot, doubleTap, blastRound, loadedFour);
            }

            BulletEventLibrary eventLibrary = LoadOrCreate<BulletEventLibrary>(
                $"{RootFolder}/BulletEventLibrary.asset",
                out bool eventLibraryCreated);
            if (eventLibraryCreated)
            {
                SetObjectArray(eventLibrary, "effects", extraShot, explosion, forceFour);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dice face prototype assets are ready.");
        }

        private static void ConfigureEntry(
            DiceFaceEntry entry,
            string displayName,
            string description,
            Color displayColor,
            DiceFaceSlotType slotType,
            BulletEventEffect effect)
        {
            SerializedObject serialized = new(entry);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("displayColor").colorValue = displayColor;
            serialized.FindProperty("slotType").enumValueIndex = (int)slotType;
            serialized.FindProperty("effect").objectReferenceValue = effect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void ConfigureMissingSlotMapping(
            DiceFaceEntry entry,
            DiceFaceSlotType slotType,
            BulletEventEffect effect)
        {
            if (entry == null || entry.Effect != null || effect == null)
            {
                return;
            }

            SerializedObject serialized = new(entry);
            serialized.FindProperty("slotType").enumValueIndex = (int)slotType;
            serialized.FindProperty("effect").objectReferenceValue = effect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void AppendMissingObjects(Object target, string propertyName, params Object[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                Object value = values[valueIndex];
                if (value == null || Contains(property, value))
                {
                    continue;
                }

                int index = property.arraySize;
                property.arraySize++;
                property.GetArrayElementAtIndex(index).objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static bool Contains(SerializedProperty property, Object value)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetObjectArray(Object target, string propertyName, params Object[] values)
        {
            SerializedObject serialized = new(target);
            SetObjectArray(serialized, propertyName, values);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            values ??= System.Array.Empty<Object>();
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                created = false;
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
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
