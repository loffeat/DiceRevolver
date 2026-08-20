using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Force Face Four On Fire End")]
    public sealed class ForceFaceFourOnFireEndEffect : BulletEventEffect
    {
        public override void Trigger(BulletEventContext context)
        {
            context.RequestRefillAndForceNextFace(4);
        }
    }
}
