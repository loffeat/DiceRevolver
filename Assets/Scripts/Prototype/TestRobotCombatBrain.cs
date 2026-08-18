using UnityEngine;

namespace DiceRevolver.Prototype
{
    public enum TestRobotMovementMode
    {
        Approach,
        Retreat,
        Strafe
    }

    public readonly struct TestRobotDecision
    {
        public TestRobotDecision(
            Vector2 moveInput,
            Vector3 aimWorldPoint,
            bool fireHeld,
            TestRobotMovementMode movementMode)
        {
            MoveInput = moveInput;
            AimWorldPoint = aimWorldPoint;
            FireHeld = fireHeld;
            MovementMode = movementMode;
        }

        public Vector2 MoveInput { get; }
        public Vector3 AimWorldPoint { get; }
        public bool FireHeld { get; }
        public TestRobotMovementMode MovementMode { get; }
    }

    public sealed class TestRobotCombatBrain
    {
        private sealed class Context
        {
            public Vector3 SelfPosition;
            public Vector3 TargetPosition;
            public float Time;
            public Vector2 MoveInput;
            public Vector3 AimWorldPoint;
            public bool FireHeld;
            public TestRobotMovementMode MovementMode;

            public Vector2 ToTarget
            {
                get
                {
                    Vector3 delta = TargetPosition - SelfPosition;
                    return new Vector2(delta.x, delta.z);
                }
            }
        }

        private readonly float minimumCombatDistance;
        private readonly float maximumCombatDistance;
        private readonly float strafeDirectionInterval;
        private readonly Context context = new Context();
        private readonly IBehaviorNode<Context> root;

        private float nextStrafeDirectionTime;
        private float strafeDirection = 1f;
        private bool hasStrafeDirectionDeadline;

        public TestRobotCombatBrain(
            float minimumCombatDistance,
            float maximumCombatDistance,
            float strafeDirectionInterval)
        {
            this.minimumCombatDistance = Mathf.Max(0f, minimumCombatDistance);
            this.maximumCombatDistance = Mathf.Max(this.minimumCombatDistance, maximumCombatDistance);
            this.strafeDirectionInterval = Mathf.Max(0.05f, strafeDirectionInterval);

            IBehaviorNode<Context> movement = new BehaviorSelector<Context>(
                new BehaviorSequence<Context>(
                    new BehaviorCondition<Context>(IsTooFar),
                    new BehaviorAction<Context>(Approach)),
                new BehaviorSequence<Context>(
                    new BehaviorCondition<Context>(IsTooClose),
                    new BehaviorAction<Context>(Retreat)),
                new BehaviorAction<Context>(Strafe));

            root = new BehaviorParallel<Context>(
                new BehaviorAction<Context>(Aim),
                new BehaviorAction<Context>(Fire),
                movement);
        }

        public TestRobotDecision Tick(Vector3 selfPosition, Vector3 targetPosition, float time)
        {
            context.SelfPosition = selfPosition;
            context.TargetPosition = targetPosition;
            context.Time = time;
            context.MoveInput = Vector2.zero;
            context.AimWorldPoint = targetPosition;
            context.FireHeld = false;

            root.Tick(context);

            return new TestRobotDecision(
                context.MoveInput,
                context.AimWorldPoint,
                context.FireHeld,
                context.MovementMode);
        }

        private bool IsTooFar(Context current)
        {
            return current.ToTarget.magnitude > maximumCombatDistance;
        }

        private bool IsTooClose(Context current)
        {
            return current.ToTarget.magnitude < minimumCombatDistance;
        }

        private static BehaviorStatus Aim(Context current)
        {
            current.AimWorldPoint = current.TargetPosition;
            return BehaviorStatus.Success;
        }

        private static BehaviorStatus Fire(Context current)
        {
            current.FireHeld = true;
            return BehaviorStatus.Success;
        }

        private BehaviorStatus Approach(Context current)
        {
            ResetStrafeDeadline();
            current.MoveInput = current.ToTarget.normalized;
            current.MovementMode = TestRobotMovementMode.Approach;
            return BehaviorStatus.Success;
        }

        private BehaviorStatus Retreat(Context current)
        {
            ResetStrafeDeadline();
            current.MoveInput = -current.ToTarget.normalized;
            current.MovementMode = TestRobotMovementMode.Retreat;
            return BehaviorStatus.Success;
        }

        private BehaviorStatus Strafe(Context current)
        {
            if (!hasStrafeDirectionDeadline)
            {
                nextStrafeDirectionTime = current.Time + strafeDirectionInterval;
                hasStrafeDirectionDeadline = true;
            }
            else if (current.Time >= nextStrafeDirectionTime)
            {
                strafeDirection *= -1f;
                nextStrafeDirectionTime = current.Time + strafeDirectionInterval;
            }

            Vector2 toTarget = current.ToTarget.normalized;
            current.MoveInput = new Vector2(toTarget.y, -toTarget.x) * strafeDirection;
            current.MovementMode = TestRobotMovementMode.Strafe;
            return BehaviorStatus.Success;
        }

        private void ResetStrafeDeadline()
        {
            hasStrafeDirectionDeadline = false;
        }
    }
}
