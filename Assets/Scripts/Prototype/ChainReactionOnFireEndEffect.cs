using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(
        fileName = "ChainReactionOnFireEndEffect",
        menuName = "Dice Revolver/Bullet Events/On Fire End/Chain Reaction")]
    public sealed class ChainReactionOnFireEndEffect : BulletEventEffect
    {
        public override void Trigger(BulletEventContext context)
        {
            if (context.Activation == null)
            {
                return;
            }

            context.QueueNextShotOverlay(DiceFaceActiveOverlay.FromSnapshot(
                context.Activation.Configuration,
                true));
        }
    }
}
