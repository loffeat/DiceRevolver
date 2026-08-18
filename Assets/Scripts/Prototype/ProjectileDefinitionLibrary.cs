using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Projectiles/Projectile Definition Library")]
    public sealed class ProjectileDefinitionLibrary : ScriptableObject
    {
        [SerializeField, InspectorName("弹丸定义库")]
        private ProjectileDefinition[] definitions = Array.Empty<ProjectileDefinition>();

        public IReadOnlyList<ProjectileDefinition> Definitions =>
            definitions ?? Array.Empty<ProjectileDefinition>();
    }
}
