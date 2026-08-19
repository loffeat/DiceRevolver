using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace DiceRevolver.Tests
{
    public sealed class TestRobotBehaviorTreeTests
    {
        [Test]
        public void SequenceStopsAtFirstFailure()
        {
            List<string> calls = new List<string>();
            BehaviorSequence<List<string>> sequence = new BehaviorSequence<List<string>>(
                new BehaviorAction<List<string>>(context =>
                {
                    context.Add("first");
                    return BehaviorStatus.Success;
                }),
                new BehaviorAction<List<string>>(context =>
                {
                    context.Add("second");
                    return BehaviorStatus.Failure;
                }),
                new BehaviorAction<List<string>>(context =>
                {
                    context.Add("third");
                    return BehaviorStatus.Success;
                }));

            BehaviorStatus result = sequence.Tick(calls);

            Assert.That(result, Is.EqualTo(BehaviorStatus.Failure));
            Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void SelectorUsesFirstSuccessfulBranch()
        {
            List<string> calls = new List<string>();
            BehaviorSelector<List<string>> selector = new BehaviorSelector<List<string>>(
                new BehaviorAction<List<string>>(context =>
                {
                    context.Add("miss");
                    return BehaviorStatus.Failure;
                }),
                new BehaviorAction<List<string>>(context =>
                {
                    context.Add("hit");
                    return BehaviorStatus.Success;
                }),
                new BehaviorAction<List<string>>(context =>
                {
                    context.Add("unused");
                    return BehaviorStatus.Success;
                }));

            BehaviorStatus result = selector.Tick(calls);

            Assert.That(result, Is.EqualTo(BehaviorStatus.Success));
            Assert.That(calls, Is.EqualTo(new[] { "miss", "hit" }));
        }

        [Test]
        public void ParallelTicksAimFireAndMovementBranches()
        {
            List<string> calls = new List<string>();
            BehaviorParallel<List<string>> parallel = new BehaviorParallel<List<string>>(
                CreateRecordingAction("aim"),
                CreateRecordingAction("fire"),
                CreateRecordingAction("move"));

            BehaviorStatus result = parallel.Tick(calls);

            Assert.That(result, Is.EqualTo(BehaviorStatus.Success));
            Assert.That(calls, Is.EqualTo(new[] { "aim", "fire", "move" }));
        }

        [Test]
        public void CombatBrainApproachesWhenTargetIsTooFar()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f);

            TestRobotDecision decision = brain.Tick(Vector3.zero, new Vector3(0f, 0f, 10f), 0f);

            Assert.That(decision.MovementMode, Is.EqualTo(TestRobotMovementMode.Approach));
            Assert.That(decision.MoveInput, Is.EqualTo(Vector2.up).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void CombatBrainRetreatsWhenTargetIsTooClose()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f);

            TestRobotDecision decision = brain.Tick(Vector3.zero, new Vector3(0f, 0f, 2f), 0f);

            Assert.That(decision.MovementMode, Is.EqualTo(TestRobotMovementMode.Retreat));
            Assert.That(decision.MoveInput, Is.EqualTo(Vector2.down).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void CombatBrainStrafesPerpendicularInsideCombatBand()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f);

            TestRobotDecision decision = brain.Tick(Vector3.zero, new Vector3(0f, 0f, 6f), 0f);

            Assert.That(decision.MovementMode, Is.EqualTo(TestRobotMovementMode.Strafe));
            Assert.That(Vector2.Dot(decision.MoveInput, Vector2.up), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(decision.MoveInput.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void CombatBrainSwitchesStrafeDirectionForTheNextMovementBurst()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f, 0.7f, 1f);
            Vector3 target = new Vector3(0f, 0f, 6f);

            TestRobotDecision first = brain.Tick(Vector3.zero, target, 0f);
            TestRobotDecision holding = brain.Tick(Vector3.zero, target, 0.71f);
            TestRobotDecision nextBurst = brain.Tick(Vector3.zero, target, 1.72f);

            Assert.That(holding.MoveInput, Is.EqualTo(Vector2.zero)
                .Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(nextBurst.MoveInput, Is.EqualTo(-first.MoveInput)
                .Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void CombatBrainAlwaysAimsAndFiresWhileTargetExists()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f);
            Vector3 target = new Vector3(3f, 0f, 7f);

            TestRobotDecision decision = brain.Tick(Vector3.zero, target, 0f);

            Assert.That(decision.AimWorldPoint, Is.EqualTo(target));
            Assert.That(decision.FireHeld, Is.True);
        }

        [Test]
        public void CombatBrainMovesThenHoldsFireAndReevaluatesAfterThePause()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f, 0.7f, 1f);
            Vector3 farTarget = new Vector3(0f, 0f, 10f);
            Vector3 nearTarget = new Vector3(0f, 0f, 2f);

            TestRobotDecision moving = brain.Tick(Vector3.zero, farTarget, 0f);
            TestRobotDecision stillMoving = brain.Tick(Vector3.zero, farTarget, 0.69f);
            TestRobotDecision holding = brain.Tick(Vector3.zero, nearTarget, 0.71f);
            TestRobotDecision stillHolding = brain.Tick(Vector3.zero, nearTarget, 1.70f);
            TestRobotDecision movingAgain = brain.Tick(Vector3.zero, nearTarget, 1.72f);

            Assert.That(moving.LocomotionPhase, Is.EqualTo(TestRobotLocomotionPhase.Moving));
            Assert.That(moving.MoveInput, Is.EqualTo(Vector2.up)
                .Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(stillMoving.LocomotionPhase, Is.EqualTo(TestRobotLocomotionPhase.Moving));

            Assert.That(holding.LocomotionPhase, Is.EqualTo(TestRobotLocomotionPhase.Holding));
            Assert.That(holding.MoveInput, Is.EqualTo(Vector2.zero)
                .Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(holding.AimWorldPoint, Is.EqualTo(nearTarget));
            Assert.That(holding.FireHeld, Is.True);
            Assert.That(stillHolding.LocomotionPhase, Is.EqualTo(TestRobotLocomotionPhase.Holding));
            Assert.That(stillHolding.MoveInput, Is.EqualTo(Vector2.zero)
                .Using(Vector2ComparerWithEqualsOperator.Instance));

            Assert.That(movingAgain.LocomotionPhase, Is.EqualTo(TestRobotLocomotionPhase.Moving));
            Assert.That(movingAgain.MovementMode, Is.EqualTo(TestRobotMovementMode.Retreat));
            Assert.That(movingAgain.MoveInput, Is.EqualTo(Vector2.down)
                .Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        private static BehaviorAction<List<string>> CreateRecordingAction(string marker)
        {
            return new BehaviorAction<List<string>>(context =>
            {
                context.Add(marker);
                return BehaviorStatus.Success;
            });
        }
    }
}
