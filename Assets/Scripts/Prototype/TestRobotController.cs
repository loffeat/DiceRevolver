using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class TestRobotController : TopDownCharacterController
    {
        [SerializeField, InspectorName("目标玩家")] private TopDownPlayerController target;
        [SerializeField, InspectorName("自动移动")] private bool autoMove;
        [SerializeField, InspectorName("最小战斗距离")] private float minimumCombatDistance = 4f;
        [SerializeField, InspectorName("最大战斗距离")] private float maximumCombatDistance = 8f;
        [SerializeField, InspectorName("横移换向间隔（秒）")] private float strafeDirectionInterval = 1f;
        [SerializeField, InspectorName("单次移动时长（秒）")] private float movementDuration = 0.7f;
        [SerializeField, InspectorName("站定攻击时长（秒）")] private float holdingDuration = 1f;

        private TestRobotCombatBrain brain;

        public TopDownPlayerController Target
        {
            get => target;
            set => target = value;
        }

        public override Vector3 AimWorldPoint { get; protected set; }
        public override Vector3 AimDirection { get; protected set; } = Vector3.forward;
        public override Vector2 MoveInput { get; protected set; }
        public override bool FireHeld { get; protected set; }
        public override bool ReloadPressedThisFrame { get; protected set; }

        protected override void Awake()
        {
            base.Awake();
            BuildBrain();
        }

        public override void RefreshControlIntent(float time)
        {
            if (target == null)
            {
                target = FindFirstObjectByType<TopDownPlayerController>();
            }

            if (target == null)
            {
                MoveInput = Vector2.zero;
                FireHeld = false;
                ReloadPressedThisFrame = false;
                return;
            }

            if (brain == null)
            {
                BuildBrain();
            }

            TestRobotDecision decision = brain.Tick(transform.position, target.transform.position, time);
            // 仅勾选"自动移动"后机器人才移动；瞄准与射击始终生效。
            MoveInput = autoMove ? decision.MoveInput : Vector2.zero;
            AimWorldPoint = decision.AimWorldPoint;
            FireHeld = decision.FireHeld;
            ReloadPressedThisFrame = false;

            Vector3 direction = AimWorldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                AimDirection = direction.normalized;
            }
        }

        private void BuildBrain()
        {
            brain = new TestRobotCombatBrain(
                minimumCombatDistance,
                maximumCombatDistance,
                strafeDirectionInterval,
                movementDuration,
                holdingDuration);
        }
    }
}
