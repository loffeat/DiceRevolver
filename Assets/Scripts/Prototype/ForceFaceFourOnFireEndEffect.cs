using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Bullet Events/Force Face Four On Fire End")]
    public sealed class ForceFaceFourOnFireEndEffect : BulletEventEffect
    {
        public override void Trigger(BulletEventContext context)
        {
            DiceChamber chamber = context.Chamber;
            if (chamber == null || chamber.ContainsFace(4))
            {
                return;
            }

            if (chamber.TryRefillFace(4))
            {
                chamber.TryForceNextFace(4);
            }
        }
    }
}
