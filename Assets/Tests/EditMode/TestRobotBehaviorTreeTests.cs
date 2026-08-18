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
        public void CombatBrainSwitchesStrafeDirectionAfterConfiguredInterval()
        {
            TestRobotCombatBrain brain = new TestRobotCombatBrain(4f, 8f, 1f);
            Vector3 target = new Vector3(0f, 0f, 6f);

            TestRobotDecision first = brain.Tick(Vector3.zero, target, 0f);
            TestRobotDecision beforeInterval = brain.Tick(Vector3.zero, target, 0.9f);
            TestRobotDecision afterInterval = brain.Tick(Vector3.zero, target, 1.1f);

            Assert.That(beforeInterval.MoveInput, Is.EqualTo(first.MoveInput).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(afterInterval.MoveInput, Is.EqualTo(-first.MoveInput).Using(Vector2ComparerWithEqualsOperator.Instance));
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
