using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceFaceLoadout : MonoBehaviour
    {
        [SerializeField, InspectorName("六面五槽位配置")] private DiceFaceConfiguration[] faceConfigurations = new DiceFaceConfiguration[DiceRevolverRules.FaceCount];

        [SerializeField, HideInInspector, InspectorName("六面装备")] private DiceFaceEntry[] entries = new DiceFaceEntry[DiceRevolverRules.FaceCount];
        [SerializeField, HideInInspector, InspectorName("六面基础事件")] private BulletEventEffect[] baseEffects = new BulletEventEffect[DiceRevolverRules.FaceCount];

        public event Action<int, DiceFaceSlotType, DiceFaceEntry> SlotChanged;

        public void Equip(int face, DiceFaceEntry entry)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount)
            {
                return;
            }

            if (entry == null)
            {
                return;
            }

            DiceFaceConfiguration configuration = GetOrCreateConfiguration(face);
            configuration.Equip(entry);
            SlotChanged?.Invoke(face, entry.SlotType, entry);
        }

        public DiceFaceEntry GetEntry(int face, DiceFaceSlotType slotType)
        {
            DiceFaceConfiguration configuration = GetOrCreateConfiguration(face);
            return configuration?.GetEntry(slotType);
        }

        public DiceFaceConfigurationSnapshot GetSnapshot(int face)
        {
            DiceFaceConfiguration configuration = GetOrCreateConfiguration(face);
            if (configuration == null)
            {
                return default;
            }

            EnsureEntrySlots();
            return configuration.CreateSnapshot(baseEffects[face - 1]);
        }

        public void SetBaseEffect(int face, BulletEventEffect effect)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount)
            {
                return;
            }

            EnsureEntrySlots();
            baseEffects[face - 1] = effect;
        }

        public BulletEventEffect GetBaseEffect(int face)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount)
            {
                return null;
            }

            return GetSnapshot(face).GetEffect(DiceFaceSlotType.Base);
        }

        private DiceFaceConfiguration GetOrCreateConfiguration(int face)
        {
            if (face < 1 || face > DiceRevolverRules.FaceCount)
            {
                return null;
            }

            EnsureEntrySlots();
            int index = face - 1;
            DiceFaceConfiguration configuration = faceConfigurations[index];
            if (configuration == null)
            {
                configuration = new DiceFaceConfiguration();
                faceConfigurations[index] = configuration;
            }

            DiceFaceEntry legacyEntry = entries[index];
            if (legacyEntry != null && configuration.GetEntry(legacyEntry.SlotType) == null)
            {
                configuration.Equip(legacyEntry);
            }

            return configuration;
        }

        private void EnsureEntrySlots()
        {
            if (entries == null || entries.Length != DiceRevolverRules.FaceCount)
            {
                Array.Resize(ref entries, DiceRevolverRules.FaceCount);
            }

            if (baseEffects == null || baseEffects.Length != DiceRevolverRules.FaceCount)
            {
                Array.Resize(ref baseEffects, DiceRevolverRules.FaceCount);
            }

            if (faceConfigurations == null || faceConfigurations.Length != DiceRevolverRules.FaceCount)
            {
                Array.Resize(ref faceConfigurations, DiceRevolverRules.FaceCount);
            }
        }
    }
}
