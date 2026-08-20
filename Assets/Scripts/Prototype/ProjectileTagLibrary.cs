using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Projectiles/Projectile Tag Library")]
    public sealed class ProjectileTagLibrary : ScriptableObject
    {
        [SerializeField, InspectorName("弹幕标签库")]
        private ProjectileTagDefinition[] tags = Array.Empty<ProjectileTagDefinition>();

        public IReadOnlyList<ProjectileTagDefinition> Tags =>
            tags ?? Array.Empty<ProjectileTagDefinition>();
    }
}
