using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceFaceLoadout : MonoBehaviour
    {
        [SerializeField] private DiceFaceEntry[] entries = new DiceFaceEntry[6];

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

        private void EnsureEntrySlots()
        {
            if (entries != null && entries.Length == 6)
            {
                return;
            }

            Array.Resize(ref entries, 6);
        }
    }
}
