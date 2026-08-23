using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(menuName = "Dice Revolver/Relics/Loaded First Face")]
    public sealed class LoadedFirstFaceRelicDefinition : RelicDefinition
    {
        [SerializeField, Min(1), InspectorName("首抽强制骰面")]
        private int face = 1;

        public int Face
        {
            get => face;
            set => face = Mathf.Clamp(value, 1, DiceRevolverRules.FaceCount);
        }

        public override void ApplyAtRoundStart(RelicContext context)
        {
            if (context.Runtime == null || context.PassiveFaces == null)
            {
                return;
            }

            // 目标面为被动面时遗物不生效。
            for (int index = 0; index < context.PassiveFaces.Count; index++)
            {
                if (context.PassiveFaces[index] == face)
                {
                    return;
                }
            }

            context.Runtime.SetFirstDrawForce(face);
        }
    }
}
