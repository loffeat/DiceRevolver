using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Projectiles/Projectile Type")]
    public sealed class ProjectileTypeDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("显示名称")] private string displayName = "New Projectile Type";

        public string DisplayName => displayName;
    }
}
