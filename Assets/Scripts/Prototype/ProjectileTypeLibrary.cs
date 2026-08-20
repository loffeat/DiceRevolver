using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Projectiles/Projectile Type Library")]
    public sealed class ProjectileTypeLibrary : ScriptableObject
    {
        [SerializeField, InspectorName("弹幕类型库")]
        private ProjectileTypeDefinition[] types = Array.Empty<ProjectileTypeDefinition>();

        public IReadOnlyList<ProjectileTypeDefinition> Types =>
            types ?? Array.Empty<ProjectileTypeDefinition>();
    }
}
