using UnityEngine;

namespace DiceRevolver.Prototype
{
    public enum AttackEffectOverride
    {
        [InspectorName("使用弹丸默认值")]
        UseProjectileDefault,

        [InspectorName("强制启用")]
        ForceEnabled,

        [InspectorName("强制禁用")]
        ForceDisabled
    }
}
