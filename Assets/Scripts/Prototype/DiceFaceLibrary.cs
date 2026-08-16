using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Dice Face Library")]
    public sealed class DiceFaceLibrary : ScriptableObject
    {
        [SerializeField] private DiceFaceEntry[] entries = Array.Empty<DiceFaceEntry>();

        public IReadOnlyList<DiceFaceEntry> Entries => entries ?? Array.Empty<DiceFaceEntry>();
    }
}
