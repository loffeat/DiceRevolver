using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class DiceFacePrototypeAssetBuilder
    {
        private const string RootFolder = "Assets/Resources/DiceFacePrototype";

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
                    new BulletEventEffect[] { extraShot },
                    null,
                    null);
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
                    null,
                    new BulletEventEffect[] { explosion },
                    null);
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
                    null,
                    null,
                    new BulletEventEffect[] { forceFour });
            }

            DiceFaceLibrary faceLibrary = LoadOrCreate<DiceFaceLibrary>(
                $"{RootFolder}/DiceFaceLibrary.asset",
                out bool faceLibraryCreated);
            if (faceLibraryCreated)
            {
                SetObjectArray(faceLibrary, "entries", doubleTap, blastRound, loadedFour);
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
            BulletEventEffect[] onFire,
            BulletEventEffect[] onHit,
            BulletEventEffect[] onFireEnd)
        {
            SerializedObject serialized = new(entry);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("displayColor").colorValue = displayColor;
            SetObjectArray(serialized, "onFireEffects", onFire);
            SetObjectArray(serialized, "onHitEffects", onHit);
            SetObjectArray(serialized, "onFireEndEffects", onFireEnd);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
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
