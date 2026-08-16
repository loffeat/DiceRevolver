using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Event Library")]
    public sealed class BulletEventLibrary : ScriptableObject
    {
        [SerializeField] private BulletEventEffect[] effects = Array.Empty<BulletEventEffect>();

        public IReadOnlyList<BulletEventEffect> Effects => effects ?? Array.Empty<BulletEventEffect>();
    }
}
