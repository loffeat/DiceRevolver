using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Projectiles/Projectile Tag")]
    public sealed class ProjectileTagDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("显示名称")] private string displayName = "New Projectile Tag";

        public string DisplayName => displayName;
    }
}
