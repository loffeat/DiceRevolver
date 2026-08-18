using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Event Library")]
    public sealed class BulletEventLibrary : ScriptableObject
    {
        [SerializeField, InspectorName("子弹事件库")] private BulletEventEffect[] effects = Array.Empty<BulletEventEffect>();

        public IReadOnlyList<BulletEventEffect> Effects => effects ?? Array.Empty<BulletEventEffect>();
    }
}
