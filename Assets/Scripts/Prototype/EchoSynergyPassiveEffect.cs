using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(
        fileName = "EchoSynergyPassiveEffect",
        menuName = "Dice Revolver/Bullet Events/Passive/Echo Synergy")]
    public sealed class EchoSynergyPassiveEffect : PassiveEventEffect
    {
        [SerializeField, Min(1), InspectorName("每轮最大呼应次数")]
        private int maximumTriggersPerChamber = 4;
        [SerializeField, Min(0f), InspectorName("最大自然散布角度")]
        private float maximumSpreadAngle = 8f;
        [SerializeField, Min(0f), InspectorName("同帧最小角度间隔")]
        private float minimumSpreadSeparation = 2f;

        public int MaximumTriggersPerChamber => maximumTriggersPerChamber;
        public float MaximumSpreadAngle => maximumSpreadAngle;
        public float MinimumSpreadSeparation => minimumSpreadSeparation;

        public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
        {
            return new EchoRuntime(
                context,
                Mathf.Max(1, maximumTriggersPerChamber),
                Mathf.Max(0f, maximumSpreadAngle),
                Mathf.Max(0f, minimumSpreadSeparation));
        }

        private sealed class EchoRuntime : IDicePassiveEffectRuntime, IDiceProjectileHitObserver
        {
            private readonly PassiveBindingContext context;
            private readonly int maximumTriggers;
            private readonly float maximumSpreadAngle;
            private readonly float minimumSpreadSeparation;
            private int remainingTriggers;
            private bool active = true;

            public EchoRuntime(
                PassiveBindingContext context,
                int maximumTriggers,
                float maximumSpreadAngle,
                float minimumSpreadSeparation)
            {
                this.context = context;
                this.maximumTriggers = maximumTriggers;
                this.maximumSpreadAngle = maximumSpreadAngle;
                this.minimumSpreadSeparation = minimumSpreadSeparation;
                remainingTriggers = maximumTriggers;
            }

            public void OnProjectileHit(
                DiceRevolverShotContext shot,
                Collider hitCollider,
                Vector3 hitPosition)
            {
                ProjectileTypeDefinition ownerType = context.BaseProjectileType;
                if (!active ||
                    remainingTriggers <= 0 ||
                    shot?.Activation?.EventBudget == null ||
                    ownerType == null ||
                    shot.Stats.ProjectileTypeDefinition != ownerType)
                {
                    return;
                }

                if (context.RequestBonusActivation(
                    shot.Activation.EventBudget,
                    maximumSpreadAngle,
                    minimumSpreadSeparation,
                    shot.Activation))
                {
                    remainingTriggers--;
                }
            }

            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                return true;
            }

            public void OnReloadStarted()
            {
                ResetForChamber();
            }

            public void OnReloadCompleted()
            {
            }

            public void OnFaceConsumed(int face)
            {
                if (face == context.Face)
                {
                    active = false;
                }
            }

            public void Dispose()
            {
            }

            private void ResetForChamber()
            {
                remainingTriggers = maximumTriggers;
                active = true;
            }
        }
    }
}
