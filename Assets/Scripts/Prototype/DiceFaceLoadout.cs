using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceFaceLoadout : MonoBehaviour
    {
        [SerializeField, InspectorName("六面装备")] private DiceFaceEntry[] entries = new DiceFaceEntry[6];
        [SerializeField, InspectorName("六面基础事件")] private BulletEventEffect[] baseEffects = new BulletEventEffect[6];

        public event Action<int, DiceFaceEntry> EntryChanged;

        public void Equip(int face, DiceFaceEntry entry)
        {
            if (face < 1 || face > 6)
            {
                return;
            }

            EnsureEntrySlots();
            entries[face - 1] = entry;
            EntryChanged?.Invoke(face, entry);
        }

        public DiceFaceEntry GetEntry(int face)
        {
            if (face < 1 || face > 6)
            {
                return null;
            }

            EnsureEntrySlots();
            return entries[face - 1];
        }

        public void SetBaseEffect(int face, BulletEventEffect effect)
        {
            if (face < 1 || face > 6)
            {
                return;
            }

            EnsureEntrySlots();
            baseEffects[face - 1] = effect;
        }

        public BulletEventEffect GetBaseEffect(int face)
        {
            if (face < 1 || face > 6)
            {
                return null;
            }

            EnsureEntrySlots();
            return baseEffects[face - 1];
        }

        private void EnsureEntrySlots()
        {
            if (entries == null || entries.Length != 6)
            {
                Array.Resize(ref entries, 6);
            }

            if (baseEffects == null || baseEffects.Length != 6)
            {
                Array.Resize(ref baseEffects, 6);
            }
        }
    }
}
