using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Extra Shot On Fire")]
    public sealed class ExtraShotOnFireEffect : BulletEventEffect
    {
        public override void Trigger(BulletEventContext context)
        {
            context.RequestAdditionalShot();
        }
    }
}
